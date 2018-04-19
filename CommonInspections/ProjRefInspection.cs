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

    private Dictionary<String, ProjectExtra> projectsByPath = new Dictionary<String, ProjectExtra>();
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
    }

    private void prepareExtras()
    {
      foreach (var (projectPath, project) in Context.Projects)
        projectsByPath[projectPath] = new ProjectExtra { Path = projectPath, RootElement = project };

      foreach (var (solutionPath, solution) in Context.Solutions)
        solutions.Add(new SolutionExtra { Path = solutionPath, Solution = solution });
    }

    private void retrieveProjectGuids()
    {
      foreach (var project in projectsByPath.Values)
      {
        var guidProperty = project.RootElement.Properties.FirstOrDefault(x => x.Name == "ProjectGuid");
        if(guidProperty == null)
        {
          Context.AddDefect(new Defect
          {
            Severity = DefectSeverity.Warning,
            Description = String.Format("Project {0} has no ProjectGuid property", project.Path)
          });
        }
        else
        {
          Guid? guid = parseGuid(guidProperty.Value, project.Path);
          if (guid.HasValue)
          {
            var anotherProject = findProjectById(guid.Value);
            if (anotherProject != null)
            {
              Context.AddDefect(new Defect
              {
                Severity = DefectSeverity.Error,
                Description = String.Format("Projects {0} and {1} have the same GUID", project.Path, anotherProject.Path)
              });
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
            Context.AddDefect(new Defect
            {
              Severity = DefectSeverity.Error,
              Path = project.Path,
              Description = String.Format("Project {0} references project file {1} which doesn't exist", project.Path, refPath)
            });
          }
          else if (project.References.Contains(refProject))
          {
            Context.AddDefect(new Defect
            {
              Severity = DefectSeverity.Error,
              Description = String.Format("Project {0} in referenced twice from project {1}",
                refProject.Path, project.Path)
            });
          }
          else
          {
            project.References.Add(refProject);
            var refGuidElement = reference.Metadata.FirstOrDefault(x => x.Name == "Project");
            Guid? refGuid = refGuidElement == null ? null : parseGuid(refGuidElement.Value, project.Path);
            if (refProject.Id != refGuid)
            {
              Context.AddDefect(new Defect
              {
                Severity = DefectSeverity.Warning,
                Description = String.Format("GUID {0} in the reference from {1} doesn't match GUID {2} of project {3}",
                  refGuid.Value, project.Path, refProject.Id, refProject.Path)
              });
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
            var refPath = Path.GetFullPath(projectInSolution.AbsolutePath);
            if (Path.GetExtension(refPath) == ".vcxproj")
            {
              var refProject = findProjectByPath(refPath);
              if (refProject == null)
              {
                Context.AddDefect(new Defect
                {
                  Severity = DefectSeverity.Error,
                  Path = solution.Path,
                  Description = String.Format("Solution {0} references project file {1} which doesn't exist", solution.Path, refPath)
                });
              }
              else if (solution.References.Contains(refProject))
              {
                Context.AddDefect(new Defect
                {
                  Severity = DefectSeverity.Error,
                  Description = String.Format("Project {0} in referenced twice from solution {1}",
                    refProject.Path, solution.Path)
                });
              }
              else
              {
                solution.References.Add(refProject);
                Guid? refGuid = parseGuid(projectInSolution.ProjectGuid, solution.Path);
                if (refProject.Id != refGuid)
                {
                  Context.AddDefect(new Defect
                  {
                    Severity = DefectSeverity.Warning,
                    Description = String.Format("GUID {0} in the reference from {1} doesn't match GUID {2} of project {3}",
                      refGuid.Value, solution.Path, refProject.Id, refPath)
                  });
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
      Context.AddDefect(new Defect
      {
        Severity = DefectSeverity.Error,
        Path = sourceFile,
        Description = String.Format("File {0} contains string {1} which is not a valid GUID format", sourceFile, input)
      });
      return null;
    }
  }
}
