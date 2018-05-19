using System;
using System.Linq;
using InspectorCore;
using Microsoft.Build.Construction;

namespace CommonInspections
{
  [InspectionClass]
  class GuidMismatchInspection : Inspection
  {
    [DefectClass(Code = "B1", Severity = DefectSeverity.Warning)]
    private class Defect_ProjectGuidMismatch : Defect
    {
      public Defect_ProjectGuidMismatch(VcProjectEntity project, VcProjectReference reference) :
        base(reference.From.PathFromBase, reference.Line, String.Format(SDefect.ProjectGuidMismatch, reference.Id, project.Id, project.PathFromBase))
      {
        var srcEntity = reference.From as VcProjectEntity;
        if(srcEntity != null && !String.IsNullOrEmpty(project.Id))
        {
          guidProperty = srcEntity.Root.Properties.First(x => x.Name == "ProjectGuid");
          targetGuid = project.Id;
          Fix = () =>
          {
            guidProperty.Value = targetGuid;
          };
        }
      }
      private ProjectPropertyElement guidProperty;
      private String targetGuid;
    }

    protected override void run()
    {
      foreach (var project in Model.Entities<VcProjectEntity>())
        foreach (var reference in project.LinksTo<VcProjectReference>())
          if (!reference.Id.Equals(project.Id, StringComparison.InvariantCultureIgnoreCase))
            Context.AddDefect(new Defect_ProjectGuidMismatch(project, reference));
    }
  }
}
