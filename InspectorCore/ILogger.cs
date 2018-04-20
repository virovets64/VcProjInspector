using System;
using System.Collections.Generic;
using System.Text;

namespace InspectorCore
{
  public interface ILogger : IDisposable
  {
    void LogDefect(Defect defect);
    void LogMessage(MessageImportance importance, String text);
  }
}
