using Microsoft.Extensions.Logging;
using minaug.DTOs;
using System.CommandLine;
using System.Reflection;

namespace minaug.Utils;

internal static class CommandLineArgumentsFactory
{
  public static CommandLineArguments? Create(params string[] arguments)
  {
    CommandLineArguments? result = null;

    RootCommand rootCommand = new($"{AppDomain.CurrentDomain.FriendlyName} {Assembly.GetEntryAssembly()?.GetName().Version} console application");

    Option<string> configOption = new("--configuration", ["-c"])
    {
      Description = "Name of configuration file placed in same folder as executable",
      AllowMultipleArgumentsPerToken = false,
      Arity = ArgumentArity.ZeroOrOne,
      Required = false,
      DefaultValueFactory = _ => "config.json"
    };
    configOption.AcceptLegalFileNamesOnly();

    Option<bool> silentOption = new("--silent")
    {
      Description = "Log as little as possible",
      AllowMultipleArgumentsPerToken = false,
      Arity = ArgumentArity.ZeroOrOne,
      Required = false,
      DefaultValueFactory = _ => false
    };

    Option<bool> verboseOption = new("--verbose")
    {
      Description = "Log as much as possible",
      AllowMultipleArgumentsPerToken = false,
      Arity = ArgumentArity.ZeroOrOne,
      Required = false,
      DefaultValueFactory = _ => false
    };

    rootCommand.Options.Add(configOption);
    rootCommand.Options.Add(silentOption);
    rootCommand.Options.Add(verboseOption);

    rootCommand.SetAction(r =>
      result = new()
      {
        ConfigurationFilepath = r.GetValue(configOption)!,
        MinimumLogLevel = r.GetValue(silentOption) ? LogLevel.None : r.GetValue(verboseOption) ? LogLevel.Trace : LogLevel.Information
      });

    rootCommand
      .Parse(arguments)
      .Invoke();

    return result;
  }
}