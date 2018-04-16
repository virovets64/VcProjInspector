using System;
using System.Collections.Generic;
using System.Text;

namespace VcProjInspector
{
  public interface IEngine
  {
    void AddDefect(Defect defect);

    IReadOnlyDictionary<String, Microsoft.Build.Construction.SolutionFile> Solutions
    {
      get;
    }

    IReadOnlyDictionary<String, Microsoft.Build.Construction.ProjectRootElement> Projects
    {
      get;
    }
  }
}
