using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

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
      evaluateProjects();
      collectImportedProjects();
    }

    private Dictionary<String, Entity> entites = new Dictionary<string, Entity>(StringComparer.InvariantCultureIgnoreCase);
    private Dictionary<Entity, List<Link>> outgoingLinks = new Dictionary<Entity, List<Link>>();
    private Dictionary<Entity, List<Link>> ingoingLinks = new Dictionary<Entity, List<Link>>();
    private IContext Context { get; }

    private Dictionary<String, String> globalProperties;
    private ProjectCollection projectCollection;

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

    public IEnumerable<Link> LinksFrom(Entity entity)
    {
      return outgoingLinks[entity];
    }

    public IEnumerable<Link> LinksTo(Entity entity)
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

    [DefectClass(Code = "A4", Severity = DefectSeverity.Warning)]
    private class Defect_ProjectHasNoGuid : Defect
    {
      public Defect_ProjectHasNoGuid(String filename) :
        base(filename, 0, SDefect.ProjectHasNoGuid)
      { }
    }

    [DefectClass(Code = "A5", Severity = DefectSeverity.Error)]
    private class Defect_ProjectRefBroken : Defect
    {
      public Defect_ProjectRefBroken(ElementLocation location, String targetProject) :
        base(location.File, location.Line, String.Format(SDefect.ProjectRefBroken, targetProject))
      { }
    }

    [DefectClass(Code = "A6", Severity = DefectSeverity.Error)]
    private class Defect_ProjectRefDuplicate : Defect
    {
      public Defect_ProjectRefDuplicate(ElementLocation location, String targetProject) :
        base(location.File, location.Line, String.Format(SDefect.ProjectRefDuplicate, targetProject))
      { }
    }

    [DefectClass(Code = "A7", Severity = DefectSeverity.Error)]
    private class Defect_SolutiontRefBroken : Defect
    {
      public Defect_SolutiontRefBroken(String filename, String targetProject) :
        base(filename, 0, String.Format(SDefect.SolutionRefBroken, targetProject))
      { }
    }

    [DefectClass(Code = "A8", Severity = DefectSeverity.Error)]
    private class Defect_SolutionRefDuplicate : Defect
    {
      public Defect_SolutionRefDuplicate(String filename, String targetProject) :
        base(filename, 0, String.Format(SDefect.SolutionRefDuplicate, targetProject))
      { }
    }

    [DefectClass(Code = "A9", Severity = DefectSeverity.Error)]
    private class Defect_GuidStringInvalid : Defect
    {
      public Defect_GuidStringInvalid(String filename, int line, String guid) :
        base(filename, line, String.Format(SDefect.GuidStringInvalid, guid))
      { }
    }

    [DefectClass(Code = "A10", Severity = DefectSeverity.Error)]
    private class Defect_ProjectEvaluationFailure : Defect
    {
      public Defect_ProjectEvaluationFailure(String filename, String reason) :
        base(filename, 0, String.Format(SDefect.ProjectEvaluationFailure, reason))
      { }
    }

    private void collectFiles()
    {
      var excludeFilePatterns = Context.Options.ExcludeFiles
        .Select(x => new Regex(x, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace))
        .ToArray();

      Context.LogMessage(MessageImportance.Normal, SMessage.CollectingFiles);
      foreach (var dir in Context.Options.IncludeDirectories)
      {
        String fullDirName = Utils.GetActualFullPath(dir);
        foreach (var filename in Directory.GetFiles(fullDirName, "*", SearchOption.AllDirectories))
        {
          if (excludeFilePatterns.Length > 0)
          {
            var relativeName = Context.RemoveBase(filename);
            if (excludeFilePatterns.Any(x => x.IsMatch(relativeName)))
              continue;
          }

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
      var projectEntity = new VcProjectEntity { FullPath = filename, PathFromBase = Context.RemoveBase(filename) };

      projectEntity.Root = openProject(filename);

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

      AddEntity(projectEntity);
    }

    private void retrieveSolutionRefs()
    {
      foreach (var solution in this.Entities<SolutionEntity>().Where(x => x.Valid))
      {
        foreach (var projectInSolution in solution.Solution.ProjectsInOrder)
        {
          if (projectInSolution.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
          {
            var refPath = projectInSolution.AbsolutePath;
            if (Utils.FileExtensionIs(refPath, ".vcxproj"))
            {
              var refProject = this.FindEntity<VcProjectEntity>(refPath);
              if (refProject == null)
              {
                Context.AddDefect(new Defect_SolutiontRefBroken(solution.PathFromBase, refPath));
              }
              else if (LinksFrom(solution).Any(x => x.To == refProject))
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
      foreach (var project in this.Entities<VcProjectEntity>().Where(x => x.Valid))
      {
        foreach (var reference in project.Root.Items.Where(x => x.ItemType == "ProjectReference"))
        {
          var refPath = Path.GetFullPath(Path.Combine(project.Root.DirectoryPath, reference.Include));
          var refProject = this.FindEntity<VcProjectEntity>(refPath);
          if (refProject == null)
          {
            Context.AddDefect(new Defect_ProjectRefBroken(reference.Location, refPath));
          }
          else if (LinksFrom(project).Any(x => x.To == refProject))
          {
            Context.AddDefect(new Defect_ProjectRefDuplicate(reference.Location, refPath));
          }
          else
          {
            var link = new VcProjectReference { From = project, To = refProject };
            var refGuidElement = reference.Metadata.FirstOrDefault(x => x.Name == "Project");
            if (refGuidElement != null)
            {
              link.Id = parseGuid(refGuidElement.Value, project.PathFromBase, refGuidElement.Location.Line);
              link.Line = refGuidElement.Location.Line;
            }
            AddLink(link);
          }
        }
      }
    }

    private void evaluateProjects()
    {
      globalProperties = new Dictionary<String, String>();
      String vsDirectory = Context.Options.VSDirectory;
      if (String.IsNullOrEmpty(vsDirectory))
        vsDirectory = Environment.GetEnvironmentVariable("VSINSTALLDIR");
      if (String.IsNullOrEmpty(vsDirectory))
        throw new ArgumentException("Visual Studio directory is not specified");
      String msBuildDirectory = Context.Options.MSBuildDirectory;
      if (String.IsNullOrEmpty(msBuildDirectory))
        msBuildDirectory = Path.Combine(vsDirectory, "MSBuild");

      globalProperties.Add("VCTargetsPath", Path.Combine(vsDirectory, "Common7", "IDE", "VC", "VCTargets"));
      globalProperties.Add("MSBuildExtensionsPath", msBuildDirectory);

      var toolsVersion = Context.Options.ToolsVersion;
      projectCollection = new ProjectCollection();
      var toolset = new Toolset(toolsVersion, Path.Combine(msBuildDirectory, toolsVersion, "Bin"), projectCollection, string.Empty);
      projectCollection.AddToolset(toolset);

      foreach (var projectEntity in this.Entities<VcProjectEntity>().Where(x => x.Valid))
      {
        projectEntity.EvaluatedProject = evaluateProject(projectEntity.Root);
      }
    }

    private ProjectRootElement openProject(string filename)
    {
      Context.LogMessage(MessageImportance.Low, SMessage.OpeningProject, filename);
      try
      {
        return ProjectRootElement.Open(filename);
      }
      catch (Exception e)
      {
        Context.AddDefect(new Defect_ProjectOpenFailure(filename, e.Message));
      }
      return null;
    }


    private Project evaluateProject(ProjectRootElement root)
    {
      try
      {
        return new Project(root, globalProperties, "15.0", projectCollection);
      }
      catch (Exception e)
      {
        Context.AddDefect(new Defect_ProjectEvaluationFailure(Context.RemoveBase(root.FullPath), e.Message));
      }
      return null;
    }

    private void collectImportedProjects()
    {
      foreach (var projectEntity in this.Entities<ProjectEntity>().Where(x => x.EvaluatedProject != null).ToArray())
      {
        collectImportedProjects(projectEntity);
      }
    }

    private void collectImportedProjects(ProjectEntity projectEntity)
    {
      foreach (var import in projectEntity.EvaluatedProject.Imports)
      {
        var pathTo = import.ImportedProject.FullPath;
        var pathFrom = import.ImportingElement.ContainingProject.FullPath;

        var entityTo = FindEntity(pathTo);
        if (entityTo == null)
        {
          entityTo = new ImportedProjectEntity
          {
            FullPath = pathTo,
            PathFromBase = Context.RemoveBase(pathTo),
            Root = import.ImportedProject
          };
          AddEntity(entityTo);
        }

        var entityFrom = FindEntity(pathFrom);
        if (entityFrom == null)
        {
          entityFrom = new ImportedProjectEntity
          {
            FullPath = pathFrom,
            PathFromBase = Context.RemoveBase(pathFrom),
            Root = import.ImportingElement.ContainingProject
          };
          AddEntity(entityFrom);
        }

        AddLink(new ImportLink
        {
          From = entityFrom,
          To = entityTo,
          Label = import.ImportingElement.Label,
          Line = import.ImportingElement.Location.Line
        });
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
