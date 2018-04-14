using CommandLine;
using System;
using System.Collections.Generic;

namespace VcProjInspector
{
  class Program
  {
    public class Options
    {
      [Option('d', "dirs", Required = true, HelpText = "directories to scan.")]
      public IEnumerable<string> IncludeDirectories { get; set; }

      [Option('x', "exclude_dirs", Required = false, HelpText = "directories to exclude.")]
      public IEnumerable<string> ExcludeDirectories { get; set; }
    }

    static void Main(string[] args)
    {
      CommandLine.Parser.Default.ParseArguments<Options>(args)
        .WithParsed<Options>(opts => RunOptionsAndReturnExitCode(opts))
        .WithNotParsed<Options>((errs) => HandleParseError(errs));
    }

    static void RunOptionsAndReturnExitCode(Options options)
    {
      var engine = new Engine();
      engine.Run(options);
      Environment.ExitCode = 0;
    }

    static void HandleParseError(IEnumerable<Error> errors)
    {
      Environment.ExitCode = 1;
    }
  }
}
