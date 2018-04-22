using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using CommandLine;
using Microsoft.Build.Construction;

namespace InspectorCore
{
  public class InspectorOptions
  {
    [Option('d', "dirs", Required = true, HelpText = "directories to scan.")]
    public IEnumerable<string> IncludeDirectories { get; set; }

    [Option('x', "exclude_dirs", Required = false, HelpText = "directories to exclude.")]
    public IEnumerable<string> ExcludeDirectories { get; set; }

    [Option('o', "output_dir", Default = ".", Required = false, HelpText = "output directory.")]
    public string OutputDirectory { get; set; }

    [Option('b', "base_dir", Default = "", Required = false, HelpText = "base directory.")]
    public string BaseDirectory { get; set; }
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

    [DefectClass(Code = "A2", Severity = DefectSeverity.Error)]
    private class Defect_SolutionOpenFailure : Defect
    {
      public Defect_SolutionOpenFailure(String filename, String errorMessage) :
        base(filename, 0, String.Format(SDefect.SolutionOpenFailure, errorMessage))
      { }
    }

    [DefectClass(Code = "A3", Severity = DefectSeverity.Error)]
    private class Defect_ProjectOpenFailure : Defect
    {
      public Defect_ProjectOpenFailure(String filename, String errorMessage) :
        base(filename, 0, String.Format(SDefect.ProjectOpenFailure, errorMessage))
      { }
    }

    private List<Assembly> plugins = new List<Assembly>();
    private List<Inspection> inspections = new List<Inspection>();
    private List<Defect> defects = new List<Defect>();
    private Dictionary<String, InspectedSolution> solutions = new Dictionary<String, InspectedSolution>(StringComparer.InvariantCultureIgnoreCase);
    private Dictionary<String, InspectedProject> projects = new Dictionary<String, InspectedProject>(StringComparer.InvariantCultureIgnoreCase);
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
            inspections.Add(inspection);
          }
        }
      }
    }

    private void collectFiles()
    {
      LogMessage(MessageImportance.Normal, SMessage.CollectingFiles);
      foreach (var dir in options.IncludeDirectories)
      {
        String fullDirName = Utils.GetActualFullPath(dir);
        foreach (var filename in Directory.GetFiles(fullDirName, "*", SearchOption.AllDirectories))
        {
          if (Utils.FileExtensionIs(filename, ".sln"))
            addSolution(filename);
          else if (Utils.FileExtensionIs(filename, ".vcxproj"))
            addProject(filename);
        }
      }
    }

    private void addSolution(String filename)
    {
      SolutionFile solution = null;
      try
      {
        LogMessage(MessageImportance.Low, SMessage.OpeningSolution, filename);
        solution = SolutionFile.Parse(filename);
      }
      catch (Exception e)
      {
        AddDefect(new Defect_SolutionOpenFailure(filename, e.Message));
      }
      solutions.Add(filename, new InspectedSolution { Solution = solution, FullPath = filename, PathFromBase = RemoveBase(filename) });
    }

    private void addProject(string filename)
    {
      ProjectRootElement project = null;
      try
      {
        LogMessage(MessageImportance.Low, SMessage.OpeningProject, filename);
        project = ProjectRootElement.Open(filename);
      }
      catch (Exception e)
      {
        AddDefect(new Defect_ProjectOpenFailure(filename, e.Message));
      }
      projects.Add(filename, new InspectedProject { Project = project, FullPath = filename, PathFromBase = RemoveBase(filename) });
    }

    private void runInspections()
    {
      foreach (var inspection in inspections)
      {
        LogMessage(MessageImportance.Low, SMessage.RunningInspection, inspection.GetType().Name);
        inspection.Inspect();
      }
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

    public IEnumerable<InspectedSolution> Solutions
    {
      get
      {
        return solutions.Values;
      }
    }

    public IEnumerable<InspectedProject> Projects
    {
      get
      {
        return projects.Values;
      }
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
      collectInspections();

      LogMessage(MessageImportance.High, SMessage.LoadingProjects);
      collectFiles();

      LogMessage(MessageImportance.High, SMessage.RunningInspections);
      runInspections();

      LogMessage(MessageImportance.Normal, SMessage.NameValue, "Plugins loaded: ", plugins.Count);
      LogMessage(MessageImportance.Normal, SMessage.NameValue, "Inspections run: ", inspections.Count);
      LogMessage(MessageImportance.Normal, SMessage.NameValue, "Solutions opened: ", solutions.Count(x => x.Value != null));
      LogMessage(MessageImportance.Normal, SMessage.NameValue, "Projects opened: ", projects.Count(x => x.Value != null));
      LogMessage(MessageImportance.Normal, SMessage.NameValue, "Defects found: ", defects.Count);
      LogMessage(MessageImportance.Normal, SMessage.NameValue, "  Errors: ", defects.Count(x => x.Severity != DefectSeverity.Error));
      LogMessage(MessageImportance.Normal, SMessage.NameValue, "  Warnings: ", defects.Count(x => x.Severity != DefectSeverity.Warning));
    }

    public void Dispose()
    {
      foreach (var logger in loggers)
        logger.Dispose();
    }

  }
}
