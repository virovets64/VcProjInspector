using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;

namespace InspectorCore
{
  class DataModel : IDataModel
  {
    public DataModel(IContext context)
    {
      Context = context;

      collectFiles();
      retrieveSolutionRefs();
      retrieveProjectRefs();
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

    [DefectClass(Code = "B1", Severity = DefectSeverity.Warning)]
    private class Defect_ProjectHasNoGuid : Defect
    {
      public Defect_ProjectHasNoGuid(String filename) :
        base(filename, 0, SDefect.ProjectHasNoGuid)
      { }
    }

    [DefectClass(Code = "B3", Severity = DefectSeverity.Error)]
    private class Defect_ProjectRefBroken : Defect
    {
      public Defect_ProjectRefBroken(ElementLocation location, String targetProject) :
        base(location.File, location.Line, String.Format(SDefect.ProjectRefBroken, targetProject))
      { }
    }

    [DefectClass(Code = "B4", Severity = DefectSeverity.Error)]
    private class Defect_ProjectRefDuplicate : Defect
    {
      public Defect_ProjectRefDuplicate(ElementLocation location, String targetProject) :
        base(location.File, location.Line, String.Format(SDefect.ProjectRefDuplicate, targetProject))
      { }
    }

    [DefectClass(Code = "B6", Severity = DefectSeverity.Error)]
    private class Defect_SolutiontRefBroken : Defect
    {
      public Defect_SolutiontRefBroken(String filename, String targetProject) :
        base(filename, 0, String.Format(SDefect.SolutionRefBroken, targetProject))
      { }
    }

    [DefectClass(Code = "B7", Severity = DefectSeverity.Error)]
    private class Defect_SolutionRefDuplicate : Defect
    {
      public Defect_SolutionRefDuplicate(String filename, String targetProject) :
        base(filename, 0, String.Format(SDefect.SolutionRefDuplicate, targetProject))
      { }
    }

    [DefectClass(Code = "B9", Severity = DefectSeverity.Error)]
    private class Defect_GuidStringInvalid : Defect
    {
      public Defect_GuidStringInvalid(String filename, int line, String guid) :
        base(filename, line, String.Format(SDefect.GuidStringInvalid, guid))
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
            addVcProject(filename);
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

    private void addVcProject(string filename)
    {
      var projectEntity = new VcProjectEntity { FullPath = filename, PathFromBase = Context.RemoveBase(filename)};

      Context.LogMessage(MessageImportance.Low, SMessage.OpeningProject, filename);

      try
      {
        projectEntity.Root = ProjectRootElement.Open(filename);
        var guidProperty = projectEntity.Root.Properties.FirstOrDefault(x => x.Name == "ProjectGuid");
        if (guidProperty == null)
        {
          Context.AddDefect(new Defect_ProjectHasNoGuid(projectEntity.PathFromBase));
        }
        else
        {
          projectEntity.Id = parseGuid(guidProperty.Value, projectEntity.PathFromBase, guidProperty.Location.Line);
          projectEntity.IdLine = guidProperty.Location.Line;
        }
      }
      catch (Exception e)
      {
        Context.AddDefect(new Defect_ProjectOpenFailure(filename, e.Message));
      }

      AddEntity(projectEntity);
    }

    private void retrieveSolutionRefs()
    {
      foreach (var solution in this.ValidSolutions())
      {
        foreach (var projectInSolution in solution.Solution.ProjectsInOrder)
        {
          if (projectInSolution.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
          {
            var refPath = projectInSolution.AbsolutePath;
            if (Utils.FileExtensionIs(refPath, ".vcxproj"))
            {
              var refProject = this.FindProject(refPath);
              if (refProject == null)
              {
                Context.AddDefect(new Defect_SolutiontRefBroken(solution.PathFromBase, refPath));
              }
              else if (OutgoingLinks(solution).Any(x => x.To == refProject))
              {
                Context.AddDefect(new Defect_SolutionRefDuplicate(solution.PathFromBase, refProject.PathFromBase));
              }
              else
              {
                var link = new VcProjectReference { From = solution, To = refProject };
                link.Id = parseGuid(projectInSolution.ProjectGuid, solution.PathFromBase);
                AddLink(link);
              }
            }
          }
        }
      }
    }

    private void retrieveProjectRefs()
    {
      foreach (var project in this.ValidProjects())
      {
        foreach (var reference in project.Root.Items.Where(x => x.ItemType == "ProjectReference"))
        {
          var refPath = Path.GetFullPath(Path.Combine(project.Root.DirectoryPath, reference.Include));
          var refProject = this.FindProject(refPath);
          if (refProject == null)
          {
            Context.AddDefect(new Defect_ProjectRefBroken(reference.Location, refPath));
          }
          else if (OutgoingLinks(project).Any(x => x.To == refProject))
          {
            Context.AddDefect(new Defect_ProjectRefDuplicate(reference.Location, refPath));
          }
          else
          {
            var link = new VcProjectReference { From = project, To = refProject };
            var refGuidElement = reference.Metadata.FirstOrDefault(x => x.Name == "Project");
            if(refGuidElement != null)
            {
              link.Id = parseGuid(refGuidElement.Value, project.PathFromBase, refGuidElement.Location.Line);
              link.Line = refGuidElement.Location.Line;
            }
            AddLink(link);
          }
        }
      }
    }


    private Guid? parseGuid(String input, String sourceFile, int line = 0)
    {
      Guid result;
      if (Guid.TryParse(input, out result))
        return result;
      Context.AddDefect(new Defect_GuidStringInvalid(sourceFile, line, input));
      return null;
    }
  }
}
