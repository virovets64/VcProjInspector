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
      var inspector = new Inspector();
      inspector.AddLogger(new ConsoleLogger());
      inspector.Run(options);
      Environment.ExitCode = 0;
    }

    static void HandleParseError(IEnumerable<Error> errors)
    {
      Environment.ExitCode = 1;
    }
  }
}
