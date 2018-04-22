using System;
using System.Collections.Generic;
using System.Text;

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
    IEnumerable<InspectedSolution> Solutions { get; }
    IEnumerable<InspectedProject> Projects { get; }
    InspectorOptions Options { get; }
    
    void AddDefect(Defect defect);
    void LogMessage(MessageImportance importance, String format, params object[] args);
    String RemoveBase(String path);
    InspectedSolution FindSolution(String path);
    InspectedProject FindProject(String path);
  }
}
