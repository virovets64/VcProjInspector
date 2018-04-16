using System;
using System.Collections.Generic;
using System.Text;

namespace InspectorCore
{
  public enum DefectSeverity { Warning, Error };

  public class Defect
  {
    public DefectSeverity Severity { get; set; } = DefectSeverity.Error;
    public String Path { get; set; } = "";
    public int Line { get; set; } = 0;
    public String Description { get; set; } = "";
  }
}
