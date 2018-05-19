using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using CommandLine;

namespace InspectorCore
{
  public class InspectorOptions
  {
    [Option('d', "dirs", Required = true, HelpText = "directories to scan.")]
    public IEnumerable<string> IncludeDirectories { get; set; }

    [Option('x', "exclude_files", Required = false, HelpText = "files to exclude (regexp may be used).")]
    public IEnumerable<string> ExcludeFiles { get; set; }

    [Option('o', "output_dir", Default = ".", Required = false, HelpText = "output directory.")]
    public string OutputDirectory { get; set; }

    [Option('b', "base_dir", Default = "", Required = false, HelpText = "base directory.")]
    public string BaseDirectory { get; set; }

    [Option("vs_dir", Default = "", Required = false, HelpText = "Visual Studio directory.")]
    public string VSDirectory { get; set; }

    [Option("msbuild_dir", Default = "", Required = false, HelpText = "MSBuild directory.")]
    public string MSBuildDirectory { get; set; }

    [Option("tools_version", Default = "15.0", Required = false, HelpText = "Tools version.")]
    public string ToolsVersion { get; set; }

    [Option(Default = false, Required = false, HelpText = "Automatically fix defects.")]
    public bool Fix { get; set; }
  }

  public class Inspector : IContext, IDisposable
  {
    [DefectClass(Code = "A1", Severity = DefectSeverity.Internal)]
    private class Defect_PluginLoadFailure : Defect
    {
      public Defect_PluginLoadFailure(String filename, String errorMessage) :
        base(filename, 0, String.Format(SDefect.PluginLoadFailure, errorMessage))
      { }
    }

    [DefectClass(Code = "A11", Severity = DefectSeverity.Internal)]
    private class Defect_CodeDuplicate : Defect
    {
      public Defect_CodeDuplicate(Type class1, Type class2, String code) :
        base(class1.Assembly.Location, 0, String.Format(SDefect.DefectCodeDuplicate, class1.Name, class2.Name, code))
      { }
    }

    private DataModel model;
    private List<Assembly> plugins = new List<Assembly>();
    private List<Inspection> inspections = new List<Inspection>();
    private List<Defect> defects = new List<Defect>();
    private List<ILogger> loggers = new List<ILogger>();
    private InspectorOptions options;

    private void collectInspections()
    {
      foreach (var plugin in plugins)
      {
        var types = plugin.GetTypes();
        foreach (var type in types)
        {
          var attribute = type.GetCustomAttribute<InspectionClass>();
          if (attribute != null)
          {
            LogMessage(MessageImportance.Low, SMessage.CreatingInspection, type.Name);
            var inspection = (Inspection)Activator.CreateInstance(type);
            inspection.Context = this;
            inspection.Model = model;
            inspections.Add(inspection);
          }
        }
      }
    }

    private void checkDefectClasses()
    {
      var defectTypes = new Dictionary<String, Type>();
      Assembly[] thisAssembly = new Assembly[] { Assembly.GetExecutingAssembly() };
      foreach (var plugin in plugins.Union(thisAssembly))
      {
        var types = plugin.GetTypes();
        foreach (var type in types)
        {
          var defectClass = type.GetCustomAttribute<DefectClass>();
          if (defectClass != null)
          {
            Type anotherType;
            if(defectTypes.TryGetValue(defectClass.Code, out anotherType))
              AddDefect(new Defect_CodeDuplicate(type, anotherType, defectClass.Code));
            else
              defectTypes.Add(defectClass.Code, type);
          }
        }
      }
    }

    private void runInspections()
    {
      foreach (var inspection in inspections)
      {
        LogMessage(MessageImportance.Low, SMessage.RunningInspection, inspection.GetType().Name);
        inspection.Inspect();
      }
    }

    private int fixDefects()
    {
      int fixCount = 0;
      if (Options.Fix)
      {
        LogMessage(MessageImportance.High, SMessage.FixingDefects);
        foreach (var defect in defects.Where(x => x.Fix != null))
        {
          defect.Fix();
          fixCount++;
        }
        if (fixCount != 0)
        {
          LogMessage(MessageImportance.High, SMessage.NumberOfFixedDefects, fixCount);
          foreach (var entity in model.Entities<ProjectEntity>())
          {
            entity.Root.Save();
          }
        }
      }
      return fixCount;
    }

    private void loadPlugins()
    {
      var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Plugins");
      foreach (var filename in Directory.GetFiles(path, "*.dll"))
      {
        try
        {
          LogMessage(MessageImportance.Normal, SMessage.LoadingPlugin, filename);
          plugins.Add(Assembly.LoadFrom(filename));
        }
        catch (Exception e)
        {
          AddDefect(new Defect_PluginLoadFailure(filename, e.Message));
        }
      }
    }

    public IReadOnlyCollection<Defect> Defects
    {
      get
      {
        return defects.AsReadOnly();
      }
    }

    public void AddDefect(Defect defect)
    {
      defects.Add(defect);
      foreach (var logger in loggers)
        logger.LogDefect(defect);
    }

    public void LogMessage(MessageImportance importance, String format, params object[] args)
    {
      var text = String.Format(format, args);
      foreach (var logger in loggers)
        logger.LogMessage(importance, text);
    }

    public String RemoveBase(String path)
    {
      return Path.GetRelativePath(Options.BaseDirectory, path);
    }

    public InspectorOptions Options
    {
      get
      {
        return options;
      }
    }

    public void AddLogger(ILogger logger)
    {
      loggers.Add(logger);
    }

    public void RemoveLogger(ILogger logger)
    {
      loggers.Remove(logger);
    }

    public void Run(InspectorOptions options)
    {
      this.options = options;

      LogMessage(MessageImportance.High, SMessage.LoadingInspections);
      loadPlugins();

      LogMessage(MessageImportance.High, SMessage.LoadingProjects);
      model = new DataModel(this);

      collectInspections();
      checkDefectClasses();

      LogMessage(MessageImportance.High, SMessage.RunningInspections);
      runInspections();

      LogMessage(MessageImportance.Normal, SMessage.NameValue, "Plugins loaded: ", plugins.Count);
      LogMessage(MessageImportance.Normal, SMessage.NameValue, "Inspections run: ", inspections.Count);
      LogMessage(MessageImportance.Normal, SMessage.NameValue, "Solutions opened: ", model.Entities<SolutionEntity>().Where(x => x.Valid));
      LogMessage(MessageImportance.Normal, SMessage.NameValue, "Projects opened: ", model.Entities<VcProjectEntity>().Where(x => x.Valid).Count());
      LogMessage(MessageImportance.Normal, SMessage.NameValue, "Defects found: ", defects.Count);
      LogMessage(MessageImportance.Normal, SMessage.NameValue, "  Errors: ", defects.Count(x => x.Severity != DefectSeverity.Error));
      LogMessage(MessageImportance.Normal, SMessage.NameValue, "  Warnings: ", defects.Count(x => x.Severity != DefectSeverity.Warning));

      fixDefects();
    }

    public void Dispose()
    {
      foreach (var logger in loggers)
        logger.Dispose();
    }

  }
}
