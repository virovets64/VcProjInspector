using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InspectorCore;

namespace CommonInspections
{
  [InspectionClass]
  class OrphanedProjectsInspection : Inspection
  {
    [DefectClass(Code = "B3", Severity = DefectSeverity.Warning)]
    private class Defect_ProjectIsOrphan : Defect
    {
      public Defect_ProjectIsOrphan(String filename) :
        base(filename, 0, SDefect.ProjectIsOrphan)
      { }
    }

    protected override void run()
    {
      foreach (var project in Model.Entities<VcProjectEntity>())
        if(!project.LinksTo<VcProjectReference>().Any())
          Context.AddDefect(new Defect_ProjectIsOrphan(project.PathFromBase));
    }
  }
}
