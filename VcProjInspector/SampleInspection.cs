using System;
using System.Collections.Generic;
using System.Text;

namespace VcProjInspector
{
  [InspectionClass]
  class SampleInspection : Inspection
  {
    protected override void run(IEngine engine)
    {
      Console.WriteLine("SampleInspection");
    }
  }
}
