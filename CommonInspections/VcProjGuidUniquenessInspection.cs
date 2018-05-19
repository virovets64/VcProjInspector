using System;
using System.Collections.Generic;
using System.Linq;
using InspectorCore;

namespace CommonInspections
{
  [InspectionClass]
  class VcProjGuidUniquenessInspection: Inspection
  {
    [DefectClass(Code = "B4", Severity = DefectSeverity.Warning)]
    private class Defect_ProjectGuidIsDuplicated : Defect
    {
      public Defect_ProjectGuidIsDuplicated(String filename, int line, String targetProject, String guid) :
        base(filename, line, String.Format(SDefect.ProjectGuidDuplicate, guid, targetProject))
      { }
    }

    protected override void run()
    {
      var projectsByGuids = new Dictionary<string, VcProjectEntity>(StringComparer.OrdinalIgnoreCase);
      foreach (var project in Model.Entities<VcProjectEntity>().Where(x => !String.IsNullOrEmpty(x.Id)))
      {
        VcProjectEntity anotherProject;
        if (projectsByGuids.TryGetValue(project.Id, out anotherProject))
          Context.AddDefect(new Defect_ProjectGuidIsDuplicated(project.PathFromBase, project.IdLine, anotherProject.PathFromBase, project.Id));
        else
          projectsByGuids.Add(project.Id, project);
      }
    }
  }
}
