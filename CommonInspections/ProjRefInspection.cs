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
    [DefectClass(Code = "B5", Severity = DefectSeverity.Error)]
    private class Defect_ProjectGuidMismatch : Defect
    {
      public Defect_ProjectGuidMismatch(String filename, int line, String targetProject, Guid? refGuid, Guid? targetGuid) :
        base(filename, line, String.Format(SDefect.ProjectGuidMismatch, refGuid, targetGuid, targetProject))
      { }
    }

    [DefectClass(Code = "B10", Severity = DefectSeverity.Error)]
    private class Defect_MissingProject : Defect
    {
      public Defect_MissingProject(String filename, String srcProj, String dstProj) :
        base(filename, 0, String.Format(SDefect.MissingProject, dstProj, srcProj))
      { }
    }

    protected override void run()
    {
      checkReferenceGuids();
      detectMissingProjectsInSolutions();
    }

    private void checkReferenceGuids()
    {
      foreach (var project in Model.Projects().Select(x => x as VcProjectEntity).Where(x => x != null))
        foreach(var reference in Model.IngoingLinks(project).Select(x => x as VcProjectReference).Where(x => x != null))
          if (reference.Id != project.Id)
            Context.AddDefect(new Defect_ProjectGuidMismatch(reference.From.PathFromBase, reference.Line, project.PathFromBase, reference.Id, project.Id));
    }

    private void detectMissingProjectsInSolutions()
    {
      foreach (var solution in Model.ValidSolutions())
        foreach (var project in Model.OutgoingLinks(solution).Select(x => x.To as VcProjectEntity).Where(x => x != null && x.Valid))
          foreach (var referencedProject in Model.OutgoingLinks(project).Select(x => x.To as VcProjectEntity).Where(x => x != null && x.Valid))
            if (!Model.OutgoingLinks(solution).Any(x => x.To == referencedProject))
              Context.AddDefect(new Defect_MissingProject(solution.PathFromBase, project.PathFromBase, referencedProject.PathFromBase));
    }
  }
}
