using System;
using System.Collections.Generic;
using System.Text;

namespace InspectorCore
{
  public abstract class Inspection
  {
    public IContext Engine { get; internal set; }

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
