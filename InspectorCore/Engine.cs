using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using CommandLine;
using Microsoft.Build.Construction;

namespace InspectorCore
{
  public class Engine : IEngine
  {
    public class Options
    {
      [Option('d', "dirs", Required = true, HelpText = "directories to scan.")]
      public IEnumerable<string> IncludeDirectories { get; set; }

      [Option('x', "exclude_dirs", Required = false, HelpText = "directories to exclude.")]
      public IEnumerable<string> ExcludeDirectories { get; set; }
    }

    private List<Assembly> plugins = new List<Assembly>();
    private List<Inspection> inspections = new List<Inspection>();
    private List<Defect> defects = new List<Defect>();
    private Dictionary<String, SolutionFile> solutions = new Dictionary<String, SolutionFile>();
    private Dictionary<String, ProjectRootElement> projects = new Dictionary<String, ProjectRootElement>();

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
            inspection.Engine = this;
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
        AddDefect(new Defect
        {
          Path = filename,
          Description = String.Format("Can't open solution file {0}: {1}", filename, e.Message)
        });
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
        AddDefect(new Defect
        {
          Path = filename,
          Description = String.Format("Can't open project file {0}: {1}", filename, e.Message)
        });
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

    public void Run(Options options)
    {
      loadPlugins();
      collectInspections();
      collectFiles(options);
      runInspections();
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
          AddDefect(new Defect
          {
            Path = filename,
            Description = String.Format("Can't load plugin {0}: {1}", filename, e.Message)
          });
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
  }
}
