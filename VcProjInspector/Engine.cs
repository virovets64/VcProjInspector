using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace VcProjInspector
{
  internal class Engine : IEngine
  {
    private List<Inspection> inspections = new List<Inspection>();
    private List<Defect> defects = new List<Defect>();

    private void collectInspections()
    {
      var types = Assembly.GetExecutingAssembly().GetTypes();
      foreach (var type in types)
      {
        var attribute = type.GetCustomAttribute<InspectionClass>();
        if (attribute != null)
          inspections.Add((Inspection)Activator.CreateInstance(type));
      }
    }

    private void runInspections()
    {
      foreach (var inspection in inspections)
      {
        inspection.Inspect(this);
      }
    }

    public void Run(Program.Options options)
    {
      collectInspections();
      runInspections();
    }

    public IReadOnlyCollection<Defect> Defects
    {
      get
      {
        return defects.AsReadOnly();
      }
    }

    public void AddDefect(Defect defect)
    {
      defects.Add(defect);
    }

  }
}
