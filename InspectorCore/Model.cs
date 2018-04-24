using Microsoft.Build.Construction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace InspectorCore
{
  public class Entity
  {
    public IDataModel Model { get; internal set; }
    public String FullPath { get; internal set; }
    public String PathFromBase { get; internal set; }
  }

  public class Link
  {
    public Entity From { get; internal set; }
    public Entity To { get; internal set; }
  }

  public class SolutionEntity: Entity
  {
    public Microsoft.Build.Construction.SolutionFile Solution { get; internal set; }
    public bool Valid
    {
      get { return Solution != null; }
    }
  }

  public class ProjectEntity: Entity
  {
    public Microsoft.Build.Construction.ProjectRootElement Root { get; internal set; }
    public bool Valid
    {
      get { return Root != null; }
    }
  }

  public interface IDataModel
  {
    IEnumerable<Entity> Entites();
    IEnumerable<Link> OutgoingLinks(Entity entity);
    IEnumerable<Link> IngoingLinks(Entity entity);
    Entity FindEntity(String path);
    void AddEntity(Entity entity);
    void AddLink(Link link);
  }


  public static class ModelExtensions
  {
    public static IEnumerable<SolutionEntity> Solutions(this IDataModel model)
    {
      return model.Entites().Select(x => x as SolutionEntity).Where(x => x != null);
    }
    public static IEnumerable<SolutionEntity> ValidSolutions(this IDataModel model)
    {
      return Solutions(model).Where(x => x.Valid);
    }
    public static IEnumerable<ProjectEntity> Projects(this IDataModel model)
    {
      return model.Entites().Select(x => x as ProjectEntity).Where(x => x != null);
    }
    public static IEnumerable<ProjectEntity> ValidProjects(this IDataModel model)
    {
      return Projects(model).Where(x => x.Valid);
    }
    public static SolutionEntity FindSolution(this IDataModel model, String path)
    {
      return model.FindEntity(path) as SolutionEntity;
    }
    public static ProjectEntity FindProject(this IDataModel model, String path)
    {
      return model.FindEntity(path) as ProjectEntity;
    }
  }

  class DataModel: IDataModel
  {
    public DataModel(IContext context)
    {
      Context = context;

      collectFiles();
    }

    private Dictionary<String, Entity> entites = new Dictionary<string, Entity>(StringComparer.InvariantCultureIgnoreCase);
    private Dictionary<Entity, List<Link>> outgoingLinks = new Dictionary<Entity, List<Link>>();
    private Dictionary<Entity, List<Link>> ingoingLinks = new Dictionary<Entity, List<Link>>();
    private IContext Context { get; }

    public void AddEntity(Entity entity)
    {
      entity.Model = this;
      entites.Add(entity.FullPath, entity);
      outgoingLinks.Add(entity, new List<Link>());
      ingoingLinks.Add(entity, new List<Link>());
    }

    public void AddLink(Link link)
    {
      outgoingLinks[link.From].Add(link);
      ingoingLinks[link.To].Add(link);
    }

    public IEnumerable<Entity> Entites()
    {
      return entites.Values;
    }

    public IEnumerable<Link> OutgoingLinks(Entity entity)
    {
      return outgoingLinks[entity];
    }

    public IEnumerable<Link> IngoingLinks(Entity entity)
    {
      return ingoingLinks[entity];
    }

    public Entity FindEntity(String path)
    {
      Entity entity = null;
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
      AddEntity(new SolutionEntity { Solution = solution, FullPath = filename, PathFromBase = Context.RemoveBase(filename) });
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
      AddEntity(new ProjectEntity { Root = project, FullPath = filename, PathFromBase = Context.RemoveBase(filename) });
    }


  }
}
