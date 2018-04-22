using Microsoft.Build.Construction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace InspectorCore
{
  public class InspectedSolution
  {
    public String FullPath { get; internal set; }
    public String PathFromBase { get; internal set; }
    public Microsoft.Build.Construction.SolutionFile Solution { get; internal set; }
    public bool Valid
    {
      get { return Solution != null; }
    }
  }

  public class InspectedProject
  {
    public String FullPath { get; internal set; }
    public String PathFromBase { get; internal set; }
    public Microsoft.Build.Construction.ProjectRootElement Root { get; internal set; }
    public bool Valid
    {
      get { return Root != null; }
    }
  }

  class DataModel
  {
    public DataModel(IContext context)
    {
      Context = context;

      collectFiles();
    }

    public Dictionary<String, InspectedSolution> solutions = new Dictionary<String, InspectedSolution>(StringComparer.InvariantCultureIgnoreCase);
    public Dictionary<String, InspectedProject> projects = new Dictionary<String, InspectedProject>(StringComparer.InvariantCultureIgnoreCase);
    private IContext Context { get; }

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

    private void collectFiles()
    {
      Context.LogMessage(MessageImportance.Normal, SMessage.CollectingFiles);
      foreach (var dir in Context.Options.IncludeDirectories)
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
        Context.LogMessage(MessageImportance.Low, SMessage.OpeningSolution, filename);
        solution = SolutionFile.Parse(filename);
      }
      catch (Exception e)
      {
        Context.AddDefect(new Defect_SolutionOpenFailure(filename, e.Message));
      }
      solutions.Add(filename, new InspectedSolution { Solution = solution, FullPath = filename, PathFromBase = Context.RemoveBase(filename) });
    }

    private void addProject(string filename)
    {
      ProjectRootElement project = null;
      try
      {
        Context.LogMessage(MessageImportance.Low, SMessage.OpeningProject, filename);
        project = ProjectRootElement.Open(filename);
      }
      catch (Exception e)
      {
        Context.AddDefect(new Defect_ProjectOpenFailure(filename, e.Message));
      }
      projects.Add(filename, new InspectedProject { Root = project, FullPath = filename, PathFromBase = Context.RemoveBase(filename) });
    }


  }
}
