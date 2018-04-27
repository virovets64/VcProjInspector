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

  public class VcProjectEntity : ProjectEntity
  {
    public Guid? Id { get; internal set; }
    public int IdLine { get; internal set; }
  }

  public class VcProjectReference: Link
  {
    public Guid? Id { get; internal set; }
    public int Line { get; internal set; }
  }

  public interface IDataModel
  {
    IEnumerable<Entity> Entites();
    IEnumerable<Link> LinksFrom(Entity entity);
    IEnumerable<Link> LinksTo(Entity entity);
    Entity FindEntity(String path);
    void AddEntity(Entity entity);
    void AddLink(Link link);
  }


  public static class ModelExtensions
  {
    public static IEnumerable<EntityType> Entities<EntityType>(this IDataModel model) where EntityType : Entity
    {
      return model.Entites().Select(x => x as EntityType).Where(x => x != null);
    }
    public static EntityType FindEntity<EntityType>(this IDataModel model, String path) where EntityType : Entity
    {
      return model.FindEntity(path) as EntityType;
    }
    public static IEnumerable<LinkType> LinksFrom<LinkType>(this Entity entity) where LinkType : Link
    {
      return entity.Model.LinksFrom(entity).Select(x => x as LinkType).Where(x => x != null);
    }
    public static IEnumerable<LinkType> LinksTo<LinkType>(this Entity entity) where LinkType : Link
    {
      return entity.Model.LinksTo(entity).Select(x => x as LinkType).Where(x => x != null);
    }
    public static IEnumerable<EntityType> EntitiesLinkedFrom<LinkType, EntityType>(this Entity entity) 
      where LinkType : Link where EntityType : Entity
    {
      return entity.LinksFrom<LinkType>().Select(x => x.To as EntityType).Where(x => x != null);
    }
    public static IEnumerable<EntityType> EntitiesLinkedTo<LinkType, EntityType>(this Entity entity)
      where LinkType : Link where EntityType : Entity
    {
      return entity.LinksTo<LinkType>().Select(x => x.To as EntityType).Where(x => x != null);
    }
  }
}
