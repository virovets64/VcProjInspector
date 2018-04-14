using System;
using System.Collections.Generic;
using System.Text;

namespace VcProjInspector
{
  public abstract class Inspection
  {
    protected abstract void run(IEngine engine);

    internal void Inspect(IEngine engine)
    {
      run(engine);
    }
  }

  [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
  public class InspectionClass : Attribute
  {
  }
}
