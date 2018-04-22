using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InspectorCore;
using CsvHelper;
using CsvHelper.Configuration;
using System.IO;
using Microsoft.Build.Construction;

namespace CommonInspections
{
  [InspectionClass]
  class CsvReport : Inspection
  {

    sealed class SolutionMap : ClassMap<KeyValuePair<String, SolutionFile>>
    {
      public SolutionMap()
      {
        Map(m => m.Key).Name("Filename");
      }
    }


    sealed class ProjectMap : ClassMap<ProjectRootElement>
    {
      public ProjectMap()
      {
        Map(m => m.FullPath).Name("Filename");
      }
    }

    sealed class ProjectPropertyMap : ClassMap<ProjectPropertyElement>
    {
      public ProjectPropertyMap()
      {
        Map(m => m.ContainingProject.FullPath).Name("Project");
        Map(m => m.Name).Name("Name");
        Map(m => m.Parent.Label).Name("Label");
        Map(m => m.Parent.Condition).Name("Condition");
        Map(m => m.Value).Name("Value");
      }
    }


    protected override void run()
    {
      report(Context.Solutions.Where(x => x.Value != null), typeof(SolutionMap), "Solution.csv");
      report(Context.Projects.Values.Where(x => x != null), typeof(ProjectMap), "Project.csv");
      report(Context.Projects.Values.Where(x => x != null).SelectMany(x => x.Properties), typeof(ProjectPropertyMap), "Property.csv");
    }

    private void report<T>(IEnumerable<T> records, Type map, String filename)
    {
      using (var textWriter = new StreamWriter(Path.Combine(Context.Options.OutputDirectory, filename)))
      {
        using (var csvWriter = new CsvWriter(textWriter))
        {
          csvWriter.Configuration.RegisterClassMap(map);
          csvWriter.WriteRecords(records);
        }
      }
    }
  }
}
