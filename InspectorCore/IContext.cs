using System;
using System.Collections.Generic;

namespace InspectorCore
{
  public enum MessageImportance
  {
    Low,
    Normal,
    High
  }

  public interface IContext
  {
    InspectorOptions Options { get; }
    
    void AddDefect(Defect defect);
    void LogMessage(MessageImportance importance, String format, params object[] args);
    String RemoveBase(String path);
    IReadOnlyCollection<Defect> Defects { get; }
  }
}
