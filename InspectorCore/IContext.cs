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
    void AddDefect(Defect defect);
    void LogMessage(MessageImportance importance, String format, params object[] args);
    String RemoveBase(String path);

    IReadOnlyDictionary<String, Microsoft.Build.Construction.SolutionFile> Solutions
    {
      get;
    }

    IReadOnlyDictionary<String, Microsoft.Build.Construction.ProjectRootElement> Projects
    {
      get;
    }

    InspectorOptions Options
    {
      get;
    }
  }
}
