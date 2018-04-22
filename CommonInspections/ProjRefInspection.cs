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
      public Guid? Id;
      public HashSet<InspectedProject> References = new HashSet<InspectedProject>();
    }

    class SolutionExtra
    {
      public HashSet<InspectedProject> References = new HashSet<InspectedProject>();
    }

    private Dictionary<InspectedSolution, SolutionExtra> solutionExtras = new Dictionary<InspectedSolution, SolutionExtra>();
    private Dictionary<InspectedProject, ProjectExtra> projectExtras = new Dictionary<InspectedProject, ProjectExtra>();
    private Dictionary<Guid, InspectedProject> projectsById = new Dictionary<Guid, InspectedProject>();


    private InspectedProject findProjectById(Guid id)
    {
      InspectedProject result;
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
      foreach (var inspectedProject in Context.Projects.Where(x => x.Valid))
        projectExtras.Add(inspectedProject, new ProjectExtra());

      foreach (var inspectedSolution in Context.Solutions.Where(x => x.Valid))
        solutionExtras.Add(inspectedSolution, new SolutionExtra());
    }

    private void retrieveProjectGuids()
    {
      foreach (var project in Context.Projects.Where(x => x.Valid))
      {
        var guidProperty = project.Root.Properties.FirstOrDefault(x => x.Name == "ProjectGuid");
        if(guidProperty == null)
        {
          Context.AddDefect(new Defect_ProjectHasNoGuid(project.PathFromBase));
        }
        else
        {
          Guid? guid = parseGuid(guidProperty.Value, project.PathFromBase);
          if (guid.HasValue)
          {
            var anotherProject = findProjectById(guid.Value);
            if (anotherProject != null)
            {
              Context.AddDefect(new Defect_ProjectGuidIsDuplicated(guidProperty.Location, anotherProject.PathFromBase, guid.Value));
            }
            else
            {
              projectsById[guid.Value] = project;
            }
            projectExtras[project].Id = guid.Value;
          }
        }
      }
    }

    private void retrieveProjectRefs()
    {
      foreach (var project in Context.Projects.Where(x => x.Valid))
      {
        var extra = projectExtras[project];
        foreach (var reference in project.Root.Items.Where(x => x.ItemType == "ProjectReference"))
        {
          var refPath = Path.GetFullPath(Path.Combine(project.Root.DirectoryPath, reference.Include));
          var refProject = Context.FindProject(refPath);
          if (refProject == null)
          {
            Context.AddDefect(new Defect_ProjectRefBroken(reference.Location, refPath));
          }
          else if (extra.References.Contains(refProject))
          {
            Context.AddDefect(new Defect_ProjectRefDuplicate(reference.Location, refPath));
          }
          else
          {
            extra.References.Add(refProject);
            var refGuidElement = reference.Metadata.FirstOrDefault(x => x.Name == "Project");
            Guid? refGuid = refGuidElement == null ? null : parseGuid(refGuidElement.Value, project.PathFromBase);
            int line = refGuidElement == null ? 0 : refGuidElement.Location.Line;
            var projGuid = projectExtras[refProject].Id;
            if (projGuid != refGuid)
            {
              Context.AddDefect(new Defect_ProjectGuidMismatch(project.PathFromBase, line, refProject.PathFromBase, refGuid, projGuid));
            }
          }
        }
      }
    }

    private void retrieveSolutionRefs()
    {
      foreach (var (solution, extra) in solutionExtras.Where(x => x.Key.Solution != null))
      {
        foreach (var projectInSolution in solution.Solution.ProjectsInOrder)
        {
          if (projectInSolution.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
          {
            var refPath = projectInSolution.AbsolutePath;
            if (Utils.FileExtensionIs(refPath, ".vcxproj"))
            {
              var refProject = Context.FindProject(refPath);
              if (refProject == null)
              {
                Context.AddDefect(new Defect_SolutiontRefBroken(solution.PathFromBase, refPath));
              }
              else if (extra.References.Contains(refProject))
              {
                Context.AddDefect(new Defect_SolutionRefDuplicate(solution.PathFromBase, refProject.PathFromBase));
              }
              else
              {
                extra.References.Add(refProject);
                Guid? refGuid = parseGuid(projectInSolution.ProjectGuid, solution.PathFromBase);
                var projGuid = projectExtras[refProject].Id;
                if (projGuid != refGuid)
                {
                  Context.AddDefect(new Defect_SolutionGuidMismatch(solution.PathFromBase, refPath, refGuid, projGuid));
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
      foreach (var solution in Context.Solutions.Where(x => x.Valid))
        foreach (var project in solutionExtras[solution].References.Where(x => x.Valid))
          foreach (var referencedProject in projectExtras[project].References)
            if (!solutionExtras[solution].References.Contains(referencedProject))
              Context.AddDefect(new Defect_MissingProject(solution.PathFromBase, project.PathFromBase, referencedProject.PathFromBase));
    }
  }
}
