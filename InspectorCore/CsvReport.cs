using System;
using System.Collections.Generic;
using System.IO;
using CsvHelper;

namespace InspectorCore
{
  public class CsvReport<T>
  {
    private class Field<U>
    {
      public String Name { get; set; }
      public Func<U, String> Getter { get; set; }
    }

    private List<Field<T>> fields = new List<Field<T>>();
    private IEnumerable<T> records;

    public CsvReport<T> AddField(String name, Func<T, String> getter)
    {
      fields.Add(new Field<T> { Name = name, Getter = getter });
      return this;
    }

    public CsvReport<T> SetRecords(IEnumerable<T> records)
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
