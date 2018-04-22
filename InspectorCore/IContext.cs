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

  public class InspectedSolution
  {
    public String FullPath { get; internal set; }
    public String PathFromBase { get; internal set; }
    public Microsoft.Build.Construction.SolutionFile Solution { get; internal set; }
  }

  public class InspectedProject
  {
    public String FullPath { get; internal set; }
    public String PathFromBase { get; internal set; }
    public Microsoft.Build.Construction.ProjectRootElement Project { get; internal set; }
  }

  public interface IContext
  {
    IEnumerable<InspectedSolution> Solutions { get; }
    IEnumerable<InspectedProject> Projects { get; }
    InspectorOptions Options { get; }

    void AddDefect(Defect defect);
    void LogMessage(MessageImportance importance, String format, params object[] args);
    String RemoveBase(String path);
  }
}
