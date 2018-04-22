using Microsoft.Build.Construction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace InspectorCore
{
  public class InspectedEntity
  {
    public String FullPath { get; internal set; }
    public String PathFromBase { get; internal set; }
  }

  public class InspectedRelation
  {
    public InspectedEntity From { get; internal set; }
    public InspectedEntity To { get; internal set; }
  }

  public class InspectedSolution: InspectedEntity
  {
    public Microsoft.Build.Construction.SolutionFile Solution { get; internal set; }
    public bool Valid
    {
      get { return Solution != null; }
    }
  }

  public class InspectedProject: InspectedEntity
  {
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

    private Dictionary<String, InspectedEntity> entites = new Dictionary<string, InspectedEntity>(StringComparer.InvariantCultureIgnoreCase);
    private Dictionary<InspectedEntity, List<InspectedRelation>> outgoingRelations = new Dictionary<InspectedEntity, List<InspectedRelation>>();
    private Dictionary<InspectedEntity, List<InspectedRelation>> ingoingRelations = new Dictionary<InspectedEntity, List<InspectedRelation>>();
    private IContext Context { get; }

    public void AddEntity(InspectedEntity entity)
    {
      entites.Add(entity.FullPath, entity);
      outgoingRelations.Add(entity, new List<InspectedRelation>());
      ingoingRelations.Add(entity, new List<InspectedRelation>());
    }

    public void AddRelation(InspectedRelation relation)
    {
      outgoingRelations[relation.From].Add(relation);
      ingoingRelations[relation.To].Add(relation);
    }

    public IEnumerable<InspectedEntity> Entites()
    {
      return entites.Values;
    }

    public IEnumerable<InspectedRelation> OutgoingRelations(InspectedEntity entity)
    {
      return outgoingRelations[entity];
    }

    public IEnumerable<InspectedRelation> IngoingRelations(InspectedEntity entity)
    {
      return ingoingRelations[entity];
    }

    public InspectedEntity FindEntity(String path)
    {
      InspectedEntity entity = null;
      entites.TryGetValue(path, out entity);
      return entity;
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
      AddEntity(new InspectedSolution { Solution = solution, FullPath = filename, PathFromBase = Context.RemoveBase(filename) });
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
      AddEntity(new InspectedProject { Root = project, FullPath = filename, PathFromBase = Context.RemoveBase(filename) });
    }


  }
}
