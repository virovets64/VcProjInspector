using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Build.Construction;

namespace VcProjInspector
{
  internal class Engine : IEngine
  {
    private List<Inspection> inspections = new List<Inspection>();
    private List<Defect> defects = new List<Defect>();
    private Dictionary<String, SolutionFile> solutions = new Dictionary<String, SolutionFile>();
    private Dictionary<String, ProjectRootElement> projects = new Dictionary<String, ProjectRootElement>();

    private void collectInspections()
    {
      var types = Assembly.GetExecutingAssembly().GetTypes();
      foreach (var type in types)
      {
        var attribute = type.GetCustomAttribute<InspectionClass>();
        if (attribute != null)
          inspections.Add((Inspection)Activator.CreateInstance(type));
      }
    }

    private void collectFiles(Program.Options options)
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
        AddDefect(new Defect { Path = filename, Description = "Can't open solution: " + e.Message });
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
        AddDefect(new Defect { Path = filename, Description = "Can't open project: " + e.Message });
      }
      projects.Add(filename, project);
    }

    private void runInspections()
    {
      foreach (var inspection in inspections)
      {
        inspection.Inspect(this);
      }
    }

    public void Run(Program.Options options)
    {
      collectInspections();
      collectFiles(options);
      runInspections();
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
