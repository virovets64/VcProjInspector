using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using InspectorCore;
using Microsoft.Build.Construction;

namespace CommonInspections
{
  [InspectionClass]
  class MissingItemsInspection : Inspection
  {
    [DefectClass(Code = "B7", Severity = DefectSeverity.Error)]
    private class Defect_ItemNotFound : Defect
    {
      public Defect_ItemNotFound(ProjectEntity project, ProjectItemElement item, String path) :
        base(project.PathFromBase, item.Location.Line, String.Format(SDefect.ItemNotFound, path))
      {
        Fix = () =>
        {
          project.Root.Items.Remove(item);
        };
      }
   }

    protected override void run()
    {
      var excludeTypes = new HashSet<String>{ "ProjectConfiguration" };
      foreach (var project in Model.Entities<VcProjectEntity>().Where(x => x.Valid))
      {
        foreach (var item in project.Root.Items)
        {
          if (excludeTypes.Contains(item.ItemType))
            continue;
          var itemPath = Path.Combine(project.Root.DirectoryPath, item.Include);
          if (!File.Exists(itemPath))
            Context.AddDefect(new Defect_ItemNotFound(project, item, itemPath));
        }
      }
    }
  }
}
