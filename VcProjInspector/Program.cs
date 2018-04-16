using CommandLine;
using System;
using System.Collections.Generic;
using InspectorCore;

namespace VcProjInspector
{
  class Program
  {
    static void Main(string[] args)
    {
      CommandLine.Parser.Default.ParseArguments<Engine.Options>(args)
        .WithParsed<Engine.Options>(opts => RunOptionsAndReturnExitCode(opts))
        .WithNotParsed<Engine.Options>((errs) => HandleParseError(errs));
    }

    static void RunOptionsAndReturnExitCode(Engine.Options options)
    {
      var engine = new Engine();
      engine.Run(options);
      foreach(var defect in engine.Defects)
        Console.WriteLine(defect.Description);
      Environment.ExitCode = 0;
    }

    static void HandleParseError(IEnumerable<Error> errors)
    {
      Environment.ExitCode = 1;
    }
  }
}
