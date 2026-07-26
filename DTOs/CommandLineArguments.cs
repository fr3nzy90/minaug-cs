using Microsoft.Extensions.Logging;

namespace MinAug.DTOs;

internal record CommandLineArguments
{
  public required string ConfigurationFilepath { get; init; }
  public required LogLevel MinimumLogLevel { get; init; }
}