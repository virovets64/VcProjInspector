using System;
using System.Collections.Generic;
using System.Text;
using InspectorCore;
using CsvHelper;
using System.IO;
using CsvHelper.Configuration;

namespace VcProjInspector
{
  class CsvLogger : ILogger
  {
    public sealed class DefectMap : ClassMap<Defect>
    {
      public DefectMap()
      {
        Map(m => m.Code);
        Map(m => m.Filename);
        Map(m => m.Line);
        Map(m => m.Description);
      }
    }
    private CsvWriter csvWriter;
    private TextWriter textWriter;

    public CsvLogger(String filename)
    {
      textWriter = new StreamWriter(filename);
      csvWriter = new CsvWriter(textWriter);
      csvWriter.Configuration.RegisterClassMap<DefectMap>();
      csvWriter.WriteHeader<Defect>();
      csvWriter.NextRecord();
    }

    public void Dispose()
    {
      csvWriter.Dispose();
      textWriter.Dispose();
    }

    public void LogDefect(Defect defect)
    {
      csvWriter.WriteRecord(defect);
      csvWriter.NextRecord();
    }

    public void LogMessage(MessageImportance importance, String text)
    { }
  }
}
