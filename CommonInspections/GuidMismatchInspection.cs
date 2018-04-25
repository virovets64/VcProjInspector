using System;
using InspectorCore;

namespace CommonInspections
{
  [InspectionClass]
  class GuidMismatchInspection : Inspection
  {
    [DefectClass(Code = "B5", Severity = DefectSeverity.Error)]
    private class Defect_ProjectGuidMismatch : Defect
    {
      public Defect_ProjectGuidMismatch(String filename, int line, String targetProject, Guid? refGuid, Guid? targetGuid) :
        base(filename, line, String.Format(SDefect.ProjectGuidMismatch, refGuid, targetGuid, targetProject))
      { }
    }

    protected override void run()
    {
      foreach (var project in Model.Entities<VcProjectEntity>())
        foreach (var reference in project.LinksTo<VcProjectReference>())
          if (reference.Id != project.Id)
            Context.AddDefect(new Defect_ProjectGuidMismatch(reference.From.PathFromBase, reference.Line, project.PathFromBase, reference.Id, project.Id));
    }
  }
}
