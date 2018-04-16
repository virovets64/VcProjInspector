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
      CommandLine.Parser.Default.ParseArguments<Inspector.Options>(args)
        .WithParsed<Inspector.Options>(opts => RunOptionsAndReturnExitCode(opts))
        .WithNotParsed<Inspector.Options>((errs) => HandleParseError(errs));
    }

    static void RunOptionsAndReturnExitCode(Inspector.Options options)
    {
      var engine = new Inspector();
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
