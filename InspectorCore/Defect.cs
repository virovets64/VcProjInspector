﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace InspectorCore
{
  public enum DefectSeverity { Warning, Error, Internal };

  public class Defect
  {
    public Defect(String filename, int line, String description, Action fix = null)
    {
      Filename = filename;
      Line = line;
      Description = description;
      var defectClass = GetType().GetCustomAttribute(typeof(DefectClass)) as DefectClass;
      Severity = defectClass.Severity;
      Code = defectClass.Code;
      Fix = fix;
    }

    public String Filename { get; } = "";
    public int Line { get; } = 0;
    public String Description { get; } = "";
    public DefectSeverity Severity { get; }
    public String Code { get; }
    public Action Fix { get; protected set; }

    public override String ToString()
    {
      var sb = new StringBuilder();
      if(!String.IsNullOrEmpty(Filename))
      {
        sb.Append(Filename);
        if (Line != 0)
          sb.AppendFormat("({0})", Line);
        sb.Append(": ");
      }
      sb.Append(severityStrings[Severity]);
      sb.Append(" ");
      sb.Append(Code);
      sb.Append(": ");
      sb.Append(Description);
      return sb.ToString();
    }

    static Dictionary<DefectSeverity, String> severityStrings = new Dictionary<DefectSeverity, string>
    {
      { DefectSeverity.Warning, "warning" },
      { DefectSeverity.Error, "error" },
      { DefectSeverity.Internal, "internal error" }
    };
  }

  [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
  public class DefectClass : Attribute
  {
    public String Code { get; set; }
    public DefectSeverity Severity { get; set; }
  }

}
