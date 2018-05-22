using System;
using System.Collections.Generic;
using System.Linq;
using InspectorCore;
using Microsoft.Build.Construction;

namespace CommonInspections
{
  [InspectionClass(Kind = InspectionKind.Report)]
  class Reports: Inspection
  {
    protected override void run()
    {
      new CsvReport<Defect>()
        .AddField("Code", x => x.Code)
        .AddField("Filename", x => x.Filename)
        .AddField("Line", x => x.Line.ToString())
        .AddField("Severity", x => x.Severity.ToString())
        .AddField("Description", x => x.Description)
        .AddField("State", x => x.State.ToString())
        .AddField("FixError", x => x.FixError)
        .SetRecords(Context.Defects)
        .Write("Defect.csv", Context);

      new CsvReport<SolutionEntity>()
        .AddField("Filename", x => x.PathFromBase)
        .SetRecords(Model.Entities<SolutionEntity>().Where(x => x.Valid))
        .Write("Solution.csv", Context);

      new CsvReport<VcProjectEntity>()
        .AddField("Filename", x => x.PathFromBase)
        .AddField("Id", x => x.Id.ToString())
        .SetRecords(Model.Entities<VcProjectEntity>().Where(x => x.Valid))
        .Write("Project.csv", Context);

      new CsvReport<VcProjectReference>()
        .AddField("Source", x => x.From.PathFromBase)
        .AddField("SourceType", x => x.From.TypeName)
        .AddField("Target", x => x.To.PathFromBase)
        .AddField("Line", x => x.Line.ToString())
        .SetRecords(Model.Entities<VcProjectEntity>().SelectMany(x => x.LinksTo<VcProjectReference>()))
        .Write("ProjectRef.csv", Context);

      new CsvReport<ProjectPropertyElement>()
        .AddField("Project", x => Context.RemoveBase(x.ContainingProject.FullPath))
        .AddField("Name", x => x.Name)
        .AddField("Label", x => x.Parent.Label)
        .AddField("Line", x => x.Location.Line.ToString())
        .AddField("Condition", x => x.Parent.Condition)
        .AddField("Value", x => x.Value)
        .SetRecords(Model.Entities<VcProjectEntity>().Where(x => x.Valid).SelectMany(x => x.Root.Properties))
        .Write("Property.csv", Context);

      new CsvReport<ImportLink>()
        .AddField("Project", x => x.From.PathFromBase)
        .AddField("Imports", x => x.To.PathFromBase)
        .AddField("Label", x => x.Label)
        .AddField("Line", x => x.Line.ToString())
        .SetRecords(Model.Entities<ProjectEntity>().SelectMany(x => x.LinksFrom<ImportLink>()))
        .Write("Import.csv", Context);
    }
  }
}
