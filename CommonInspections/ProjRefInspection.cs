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
    private Dictionary<Guid, String> projectsByGuid = new Dictionary<Guid, String>();
    private Dictionary<String, Guid> guidsByProjects = new Dictionary<String, Guid>();
    private Dictionary<String, List<String>> projectRefs = new Dictionary<String, List<String>>();
    private Dictionary<String, List<String>> solutionRefs = new Dictionary<String, List<String>>();

    protected override void run()
    {
      retrieveProjectGuids();
      retrieveProjectRefs();
      retrieveSolutionRefs();
    }

    private void retrieveProjectGuids()
    {
      foreach (var (projectPath, project) in Context.Projects.Where(x => x.Value != null))
      {
        var guidProperty = project.Properties.FirstOrDefault(x => x.Name == "ProjectGuid");
        if(guidProperty != null)
        {
          Guid? guid = parseGuid(guidProperty.Value, projectPath);
          if (guid.HasValue)
          {
            string anotherProjectPath;
            if(projectsByGuid.TryGetValue(guid.Value, out anotherProjectPath))
            {
              Context.AddDefect(new Defect
              {
                Severity = DefectSeverity.Error,
                Description = String.Format("Projects {0} and {1} have the same GUID", projectPath, anotherProjectPath)
              });
            }
            else
            {
              projectsByGuid[guid.Value] = projectPath;
            }
            guidsByProjects[projectPath] = guid.Value;
          }
        }
        else
        {
          Context.AddDefect(new Defect
          {
            Severity = DefectSeverity.Warning,
            Description = String.Format("Project {0} has no ProjectGuid property", projectPath)
          });
        }
      }
    }

    private void retrieveProjectRefs()
    {
      foreach (var (projectPath, project) in Context.Projects.Where(x => x.Value != null))
      {
        var thisProjectRefs = new List<string>();
        projectRefs[projectPath] = thisProjectRefs;
        foreach (var reference in project.Items.Where(x => x.ItemType == "ProjectReference"))
        {
          var refPath = Path.GetFullPath(Path.Combine(project.DirectoryPath, reference.Include));

          if (Context.Projects.ContainsKey(refPath))
          {
            thisProjectRefs.Add(refPath);
            var refGuidElement = reference.Metadata.FirstOrDefault(x => x.Name == "Project");
            Guid? refGuid = refGuidElement == null ? null : parseGuid(refGuidElement.Value, projectPath);
            Guid? projGuid = getProjectGuid(refPath);
            if (projGuid != refGuid)
            {
              Context.AddDefect(new Defect
              {
                Severity = DefectSeverity.Warning,
                Description = String.Format("GUID {0} in the reference from {1} doesn't match GUID {2} of project {3}",
                  refGuid.Value, projectPath, projGuid, refPath)
              });
            }
          }
          else
          {
            Context.AddDefect(new Defect
            {
              Severity = DefectSeverity.Error,
              Path = projectPath,
              Description = String.Format("Project {0} references project file {1} which doesn't exist", projectPath, refPath)
            });
          }
        }
      }
    }

    private void retrieveSolutionRefs()
    {
      foreach (var (solutionPath, solution) in Context.Solutions.Where(x => x.Value != null))
      {
        var thisSolutionRefs = new List<string>();
        solutionRefs[solutionPath] = thisSolutionRefs;

        foreach (var projectInSolution in solution.ProjectsInOrder)
        {
          if (projectInSolution.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
          {
            var refPath = Path.GetFullPath(projectInSolution.AbsolutePath);
            if(Path.GetExtension(refPath) == ".vcxproj")
            {
              if (Context.Projects.ContainsKey(refPath))
              {
                thisSolutionRefs.Add(refPath);
                Guid? refGuid = parseGuid(projectInSolution.ProjectGuid, solutionPath);
                Guid? projGuid = getProjectGuid(refPath);
                if (projGuid != refGuid)
                {
                  Context.AddDefect(new Defect
                  {
                    Severity = DefectSeverity.Warning,
                    Description = String.Format("GUID {0} in the reference from {1} doesn't match GUID {2} of project {3}",
                      refGuid.Value, solutionPath, projGuid, refPath)
                  });
                }
              }
              else
              {
                Context.AddDefect(new Defect
                {
                  Severity = DefectSeverity.Error,
                  Path = solutionPath,
                  Description = String.Format("Solution {0} references project file {1} which doesn't exist", solutionPath, refPath)
                });
              }
            }
          }
        }
      }
    }

    private Guid? getProjectGuid(String projectPath)
    {
      Guid projGuid;
      if (guidsByProjects.TryGetValue(projectPath, out projGuid))
        return projGuid;
      return null;
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
