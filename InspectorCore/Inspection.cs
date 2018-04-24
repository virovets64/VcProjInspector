﻿using System;

namespace InspectorCore
{
  public abstract class Inspection
  {
    public IContext Context { get; internal set; }
    public IDataModel Model { get; internal set; }

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
