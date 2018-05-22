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
      CommandLine.Parser.Default.ParseArguments<InspectorOptions>(args)
        .WithParsed<InspectorOptions>(opts => RunOptionsAndReturnExitCode(opts))
        .WithNotParsed<InspectorOptions>((errs) => HandleParseError(errs));
    }

    static void RunOptionsAndReturnExitCode(InspectorOptions options)
    {
      using (var inspector = new Inspector())
      {
        inspector.AddLogger(new ConsoleLogger());
        inspector.Run(options);
      }
      Environment.ExitCode = 0;
    }

    static void HandleParseError(IEnumerable<Error> errors)
    {
      Environment.ExitCode = 1;
    }
  }
}
