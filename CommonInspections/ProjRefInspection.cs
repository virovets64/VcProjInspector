using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using Microsoft.Build.Construction;
using InspectorCore;

namespace CommonInspections
{
  [InspectionClass]
  class ProjRefInspection : Inspection
  {
    [DefectClass(Code = "B1", Severity = DefectSeverity.Warning)]
    private class Defect_ProjectHasNoGuid : Defect
    {
      public Defect_ProjectHasNoGuid(String filename) :
        base(filename, 0, SDefect.ProjectHasNoGuid)
      { }
    }

    [DefectClass(Code = "B2", Severity = DefectSeverity.Warning)]
    private class Defect_ProjectGuidIsDuplicated : Defect
    {
      public Defect_ProjectGuidIsDuplicated(ElementLocation location, String targetProject, Guid guid) :
        base(location.File, location.Line, String.Format(SDefect.ProjectGuidDuplicate, guid, targetProject))
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

    [DefectClass(Code = "B5", Severity = DefectSeverity.Error)]
    private class Defect_ProjectGuidMismatch : Defect
    {
      public Defect_ProjectGuidMismatch(String filename, int line, String targetProject, Guid? refGuid, Guid? targetGuid) :
        base(filename, line, String.Format(SDefect.ProjectGuidMismatch, refGuid, targetGuid, targetProject))
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

    [DefectClass(Code = "B8", Severity = DefectSeverity.Warning)]
    private class Defect_SolutionGuidMismatch : Defect
    {
      public Defect_SolutionGuidMismatch(String filename, String targetProject, Guid? refGuid, Guid? targetGuid) :
        base(filename, 0, String.Format(SDefect.SolutionGuidMismatch, refGuid, targetGuid, targetProject))
      { }
    }

    [DefectClass(Code = "B9", Severity = DefectSeverity.Error)]
    private class Defect_GuidStringInvalid : Defect
    {
      public Defect_GuidStringInvalid(String filename, String guid) :
        base(filename, 0, String.Format(SDefect.GuidStringInvalid, guid))
      { }
    }

    [DefectClass(Code = "B10", Severity = DefectSeverity.Error)]
    private class Defect_MissingProject : Defect
    {
      public Defect_MissingProject(String filename, String srcProj, String dstProj) :
        base(filename, 0, String.Format(SDefect.MissingProject, dstProj, srcProj))
      { }
    }

    class ProjectExtra
    {
      public String Path;
      public ProjectRootElement RootElement;
      public Guid? Id;
      public HashSet<ProjectExtra> References = new HashSet<ProjectExtra>();
    }

    class SolutionExtra
    {
      public String Path;
      public SolutionFile Solution;
      public HashSet<ProjectExtra> References = new HashSet<ProjectExtra>();
    }

    private Dictionary<String, ProjectExtra> projectsByPath = new Dictionary<String, ProjectExtra>(StringComparer.InvariantCultureIgnoreCase);
    private Dictionary<Guid, ProjectExtra> projectsById = new Dictionary<Guid, ProjectExtra>();
    private List<SolutionExtra> solutions = new List<SolutionExtra>();

    private ProjectExtra findProjectByPath(String path)
    {
      ProjectExtra result;
      if (projectsByPath.TryGetValue(path, out result))
        return result;
      return null;
    }

    private ProjectExtra findProjectById(Guid id)
    {
      ProjectExtra result;
      if (projectsById.TryGetValue(id, out result))
        return result;
      return null;
    }

    protected override void run()
    {
      prepareExtras();
      retrieveProjectGuids();
      retrieveProjectRefs();
      retrieveSolutionRefs();
      detectMissingProjectsInSolutions();
    }

    private void prepareExtras()
    {
      foreach (var inspectedProject in Context.Projects)
        projectsByPath[inspectedProject.FullPath] = new ProjectExtra { Path = inspectedProject.FullPath, RootElement = inspectedProject.Project };

      foreach (var inspectedSolution in Context.Solutions)
        solutions.Add(new SolutionExtra { Path = inspectedSolution.FullPath, Solution = inspectedSolution.Solution });
    }

    private void retrieveProjectGuids()
    {
      foreach (var project in projectsByPath.Values)
      {
        var guidProperty = project.RootElement.Properties.FirstOrDefault(x => x.Name == "ProjectGuid");
        if(guidProperty == null)
        {
          Context.AddDefect(new Defect_ProjectHasNoGuid(project.Path));
        }
        else
        {
          Guid? guid = parseGuid(guidProperty.Value, project.Path);
          if (guid.HasValue)
          {
            var anotherProject = findProjectById(guid.Value);
            if (anotherProject != null)
            {
              Context.AddDefect(new Defect_ProjectGuidIsDuplicated(guidProperty.Location, anotherProject.Path, guid.Value));
            }
            else
            {
              projectsById[guid.Value] = project;
            }
            project.Id = guid.Value;
          }
        }
      }
    }

    private void retrieveProjectRefs()
    {
      foreach (var project in projectsByPath.Values.Where(x => x.RootElement != null))
      {
        foreach (var reference in project.RootElement.Items.Where(x => x.ItemType == "ProjectReference"))
        {
          var refPath = Path.GetFullPath(Path.Combine(project.RootElement.DirectoryPath, reference.Include));
          var refProject = findProjectByPath(refPath);
          if (refProject == null)
          {
            Context.AddDefect(new Defect_ProjectRefBroken(reference.Location, refPath));
          }
          else if (project.References.Contains(refProject))
          {
            Context.AddDefect(new Defect_ProjectRefDuplicate(reference.Location, refPath));
          }
          else
          {
            project.References.Add(refProject);
            var refGuidElement = reference.Metadata.FirstOrDefault(x => x.Name == "Project");
            Guid? refGuid = refGuidElement == null ? null : parseGuid(refGuidElement.Value, project.Path);
            int line = refGuidElement == null ? 0 : refGuidElement.Location.Line;
            if (refProject.Id != refGuid)
            {
              Context.AddDefect(new Defect_ProjectGuidMismatch(project.Path, line, refProject.Path, refGuid, refProject.Id));
            }
          }
        }
      }
    }

    private void retrieveSolutionRefs()
    {
      foreach (var solution in solutions.Where(x => x.Solution != null))
      {
        foreach (var projectInSolution in solution.Solution.ProjectsInOrder)
        {
          if (projectInSolution.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
          {
            var refPath = projectInSolution.AbsolutePath;
            if (Utils.FileExtensionIs(refPath, ".vcxproj"))
            {
              var refProject = findProjectByPath(refPath);
              if (refProject == null)
              {
                Context.AddDefect(new Defect_SolutiontRefBroken(solution.Path, refPath));
              }
              else if (solution.References.Contains(refProject))
              {
                Context.AddDefect(new Defect_SolutionRefDuplicate(solution.Path, refProject.Path));
              }
              else
              {
                solution.References.Add(refProject);
                Guid? refGuid = parseGuid(projectInSolution.ProjectGuid, solution.Path);
                if (refProject.Id != refGuid)
                {
                  Context.AddDefect(new Defect_SolutionGuidMismatch(solution.Path, refPath, refGuid, refProject.Id));
                }
              }
            }
          }
        }
      }
    }

    private Guid? parseGuid(String input, String sourceFile)
    {
      Guid result;
      if(Guid.TryParse(input, out result))
        return result;
      Context.AddDefect(new Defect_GuidStringInvalid(sourceFile, input));
      return null;
    }

    private void detectMissingProjectsInSolutions()
    {
      foreach (var solution in solutions.Where(x => x.Solution != null))
        foreach (var project in solution.References.Where(x => x.RootElement != null))
          foreach (var referencedProject in project.References)
            if(!solution.References.Contains(referencedProject))
              Context.AddDefect(new Defect_MissingProject(solution.Path, project.Path, referencedProject.Path));
    }
  }
}
