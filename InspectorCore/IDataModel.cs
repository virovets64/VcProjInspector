using System;
using System.Collections.Generic;
using System.Linq;

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
}
