using Microsoft.Extensions.Logging;

namespace minaug.DTOs;

internal record CommandLineArguments
{
  public required string ConfigurationFilepath { get; init; }
  public required LogLevel MinimumLogLevel { get; init; }
}