using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using CommandLine;
using Microsoft.Build.Construction;

namespace InspectorCore
{
  public class Inspector : IContext
  {
    public class Options
    {
      [Option('d', "dirs", Required = true, HelpText = "directories to scan.")]
      public IEnumerable<string> IncludeDirectories { get; set; }

      [Option('x', "exclude_dirs", Required = false, HelpText = "directories to exclude.")]
      public IEnumerable<string> ExcludeDirectories { get; set; }
    }

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
    private Dictionary<String, SolutionFile> solutions = new Dictionary<String, SolutionFile>();
    private Dictionary<String, ProjectRootElement> projects = new Dictionary<String, ProjectRootElement>();
    private List<ILogger> loggers = new List<ILogger>();

    private void collectInspections()
    {
      foreach(var plugin in plugins)
      {
        var types = plugin.GetTypes();
        foreach (var type in types)
        {
          var attribute = type.GetCustomAttribute<InspectionClass>();
          if (attribute != null)
          {
            var inspection = (Inspection)Activator.CreateInstance(type);
            inspection.Context = this;
            inspections.Add(inspection);
          }
        }
      }
    }

    private void collectFiles(Options options)
    {
      foreach (var dir in options.IncludeDirectories)
      {
        foreach (var filename in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
        {
          switch (Path.GetExtension(filename))
          {
            case ".sln":
              addSolution(filename);
              break;
            case ".vcxproj":
              addProject(filename);
              break;
          }
        }
      }
    }

    private void addSolution(String filename)
    {
      SolutionFile solution = null;
      try
      {
        solution = SolutionFile.Parse(filename);
      }
      catch (Exception e)
      {
        AddDefect(new Defect_SolutionOpenFailure(filename, e.Message));
      }
      solutions.Add(filename, solution);
    }

    private void addProject(string filename)
    {
      ProjectRootElement project = null;
      try
      {
        project = ProjectRootElement.Open(filename);
      }
      catch (Exception e)
      {
        AddDefect(new Defect_ProjectOpenFailure(filename, e.Message));
      }
      projects.Add(filename, project);
    }

    private void runInspections()
    {
      foreach (var inspection in inspections)
      {
        inspection.Inspect();
      }
    }

    private void loadPlugins()
    {
      var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Plugins");
      foreach(var filename in Directory.GetFiles(path, "*.dll"))
      {
        try
        {
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

    public IReadOnlyDictionary<String, SolutionFile> Solutions
    {
      get
      {
        return solutions;
      }
    }

    public IReadOnlyDictionary<String, ProjectRootElement> Projects
    {
      get
      {
        return projects;
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

    public void Run(Options options)
    {
      LogMessage(MessageImportance.High, SMessage.LoadingInspections);
      loadPlugins();
      collectInspections();

      LogMessage(MessageImportance.High, SMessage.LoadingProjects);
      collectFiles(options);

      LogMessage(MessageImportance.High, SMessage.RunningInspections);
      runInspections();
    }
  }
}
