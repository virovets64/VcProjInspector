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

    protected override void run()
    {
      foreach (var solution in Engine.Solutions)
      {
        foreach (var projectInSolution in solution.Value.ProjectsInOrder)
        {
          if (projectInSolution.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
          {
            var projectPath = Path.GetFullPath(projectInSolution.AbsolutePath);
            inspectProjectInSolution(solution.Key, solution.Value, projectPath, projectInSolution);
          }
        }
      }
    }

    private void inspectProjectInSolution(String solutionPath, SolutionFile solution,
                                          String projectPath, ProjectInSolution projectInSolution)
    {
      if (Path.GetExtension(projectPath) == ".vcxproj")
      {
        ProjectRootElement project = findReference(solutionPath, projectPath, parseGuid(projectInSolution.ProjectGuid, solutionPath));
        if(project == null)
        {
          Engine.AddDefect(new Defect
          {
            Severity = DefectSeverity.Error,
            Path = solutionPath,
            Description = String.Format("Solution {0} references project file {1} which doesn't exist", solutionPath, projectPath)
          });
        }
        else
        { 
          foreach (var reference in project.Items.Where(x => x.ItemType == "ProjectReference"))
          {
            var refPath = Path.GetFullPath(Path.Combine(project.DirectoryPath, reference.Include));
            var projectGuidElement = reference.Metadata.FirstOrDefault(x => x.Name == "Project");
            Guid? guid = projectGuidElement == null ? null : parseGuid(projectGuidElement.Value, projectPath);
            ProjectRootElement refProject = findReference(projectPath, refPath, guid);
            if(refProject == null)
            {
              Engine.AddDefect(new Defect
              {
                Severity = DefectSeverity.Error,
                Path = projectPath,
                Description = String.Format("Project {0} references project file {1} which doesn't exist", projectPath, refPath)
              });
            }
          }
        }
      }
    }

    private ProjectRootElement findReference(String sourceFile, String refPath, Guid? sourceGuid)
    {
      ProjectRootElement project;
      if (Engine.Projects.TryGetValue(refPath, out project))
      {
        if (project != null)
        {
          var guidProperty = project.Properties.FirstOrDefault(x => x.Name == "ProjectGuid");
          Guid? projGuid = guidProperty == null ? null : parseGuid(guidProperty.Value, refPath);
          if (sourceGuid.HasValue && projGuid.HasValue)
          {
            if (sourceGuid != projGuid)
            {
              Engine.AddDefect(new Defect
              {
                Severity = DefectSeverity.Warning,
                Path = sourceFile,
                Description = String.Format("GUID {0} in the reference from {1} doesn't match GUID {2} of project {3}",
                  sourceGuid, sourceFile, projGuid, refPath)
              });
            }
          }
        }
      }
      return project;
    }

    private Guid? parseGuid(String input, String sourceFile)
    {
      Guid result;
      if(Guid.TryParse(input, out result))
        return result;
      Engine.AddDefect(new Defect
      {
        Severity = DefectSeverity.Error,
        Path = sourceFile,
        Description = String.Format("File {0} contains string {1} which is not a valid GUID format", sourceFile, input)
      });
      return null;
    }

  }
}
