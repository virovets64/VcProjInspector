﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InspectorCore;
using CsvHelper;
using System.IO;
using Microsoft.Build.Construction;

namespace CommonInspections
{
  [InspectionClass]
  class CsvReport : Inspection
  {
    protected override void run()
    {
      new Report<InspectedSolution>()
        .AddField("Filename", x => x.PathFromBase)
        .SetRecords(Context.Solutions.Where(x => x.Solution != null))
        .Write("Solution.csv", Context);

      new Report<InspectedProject>()
        .AddField("Filename", x => x.PathFromBase)
        .SetRecords(Context.Projects.Where(x => x.Project != null))
        .Write("Project.csv", Context);

      new Report<ProjectPropertyElement>()
        .AddField("Project", x => Context.RemoveBase(x.ContainingProject.FullPath))
        .AddField("Name", x => x.Name)
        .AddField("Label", x => x.Parent.Label)
        .AddField("Condition", x => x.Parent.Condition)
        .AddField("Value", x => x.Value)
        .SetRecords(Context.Projects.Where(x => x.Project != null).SelectMany(x => x.Project.Properties))
        .Write("Property.csv", Context);
    }


    private class Report<T>
    {
      private class Field<U>
      {
        public String Name { get; set; }
        public Func<U, String> Getter { get; set; }
      }

      private List<Field<T>> fields = new List<Field<T>>();
      private IEnumerable<T> records;

      public Report<T> AddField(String name, Func<T, String> getter)
      {
        fields.Add(new Field<T> { Name = name, Getter = getter });
        return this;
      }

      public Report<T> SetRecords(IEnumerable<T> records)
      {
        this.records = records;
        return this;
      }
      
      public void Write(String filename, IContext context)
      {
        using (var textWriter = new StreamWriter(Path.Combine(context.Options.OutputDirectory, filename)))
        {
          using (var csvWriter = new CsvWriter(textWriter))
          {
            foreach (var field in fields)
              csvWriter.WriteField(field.Name);

            csvWriter.NextRecord();

            foreach (var record in records)
            {
              foreach (var field in fields)
                csvWriter.WriteField(field.Getter(record));

              csvWriter.NextRecord();
            }
          }
        }
      }
    }
  }
}
