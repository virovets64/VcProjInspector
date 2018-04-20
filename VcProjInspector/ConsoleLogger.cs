using System;
using System.Collections.Generic;
using System.Text;
using InspectorCore;

namespace VcProjInspector
{
  class ConsoleLogger: ILogger
  {
    static Dictionary<MessageImportance, ConsoleColor> messageColors = new Dictionary<MessageImportance, ConsoleColor>
    {
      { MessageImportance.Low, ConsoleColor.DarkGray },
      { MessageImportance.Normal, ConsoleColor.Gray },
      { MessageImportance.High, ConsoleColor.White }
    };

    static Dictionary<DefectSeverity, ConsoleColor> defectColors = new Dictionary<DefectSeverity, ConsoleColor>
    {
      { DefectSeverity.Warning, ConsoleColor.Yellow },
      { DefectSeverity.Error, ConsoleColor.Red },
      { DefectSeverity.Internal, ConsoleColor.Magenta }
    };

    public void LogDefect(Defect defect)
    {
      var oldColor = Console.ForegroundColor;
      Console.ForegroundColor = defectColors[defect.Severity];
      Console.WriteLine(defect.ToString());
      Console.ForegroundColor = oldColor;
    }

    public void LogMessage(MessageImportance importance, String text)
    {
      var oldColor = Console.ForegroundColor;
      Console.ForegroundColor = messageColors[importance];
      Console.WriteLine(text);
      Console.ForegroundColor = oldColor;
    }

  }
}
