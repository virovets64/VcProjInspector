using System;
using System.Collections.Generic;
using System.Text;

namespace VcProjInspector
{
  public abstract class Inspection
  {
    public IEngine Engine { get; internal set; }

    protected abstract void run();

    internal void Inspect()
    {
      run();
    }
  }

  [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
  public class InspectionClass : Attribute
  {
  }
}
