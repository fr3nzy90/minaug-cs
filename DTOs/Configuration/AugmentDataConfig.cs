namespace minaug.DTOs.Configuration;

internal class AugmentDataConfig
{
  public required string CsvFilePath { get; init; }
  public string CsvDelimiter { get; init; } = ";";
  public string DateHeaderKey { get; init; } = "Date";
  public string AmountHeaderKey { get; init; } = "Amount";
  public uint AmountDecimalPlaces { get; init; } = 2;
  public uint FactorDecimalPlaces { get; init; } = 5;
  public bool AddInflationFactor { get; init; } = true;
  public string InflationFactorHeaderKey { get; init; } = "Inflation factor";
  public bool AddInflatedAmount { get; init; } = true;
  public string InflationAdjustedAmountHeaderKey { get; init; } = "Inflated amount";
  public bool AddReverseInflationFactor { get; init; } = true;
  public string ReverseInflationFactorHeaderKey { get; init; } = "Reverse inflation factor";
  public bool AddReverseInflatedAmount { get; init; } = true;
  public string ReverseInflationAdjustedAmountHeaderKey { get; init; } = "Reverse inflated amount";
}