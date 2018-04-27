using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InspectorCore;

namespace CommonInspections
{
  [InspectionClass]
  class MissingProjectsInSolutionsInspection : Inspection
  {
    [DefectClass(Code = "B10", Severity = DefectSeverity.Error)]
    private class Defect_MissingProject : Defect
    {
      public Defect_MissingProject(String filename, String srcProj, String dstProj) :
        base(filename, 0, String.Format(SDefect.MissingProject, dstProj, srcProj))
      { }
    }

    protected override void run()
    {
      foreach (var solution in Model.Entities<SolutionEntity>().Where(x => x.Valid))
        foreach (var project in solution.EntitiesLinkedFrom<VcProjectReference, VcProjectEntity>().Where(x => x.Valid))
          foreach (var referencedProject in project.EntitiesLinkedFrom<VcProjectReference, VcProjectEntity>().Where(x => x.Valid))
            if (!solution.LinksFrom<VcProjectReference>().Any(x => x.To == referencedProject))
              Context.AddDefect(new Defect_MissingProject(solution.PathFromBase, project.PathFromBase, referencedProject.PathFromBase));
    }
  }
}
