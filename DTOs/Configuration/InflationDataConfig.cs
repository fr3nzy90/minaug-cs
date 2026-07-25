namespace minaug.DTOs.Configuration;

internal record InflationDataConfig
{
  public required string CsvFilePath { get; init; }
  public string CsvDelimiter { get; init; } = ";";
}