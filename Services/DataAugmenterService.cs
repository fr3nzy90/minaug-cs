using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinAug.DTOs;
using MinAug.DTOs.Configuration;
using MinAug.Extensions;

namespace MinAug.Services;

internal class DataAugmenterService
{
  private readonly ILogger _logger;
  private readonly MonthlyInflationCalculatorService _monthlyInflationCalculatorService;
  private readonly AugmentDataConfig _augmentDataConfig;

  public DataAugmenterService(ILogger<DataAugmenterService> logger,
    IOptionsSnapshot<AugmentDataConfig> augmentDataConfig,
    MonthlyInflationCalculatorService monthlyInflationCalculatorService)
  {
    _logger = logger;
    _augmentDataConfig = augmentDataConfig.Value;
    _monthlyInflationCalculatorService = monthlyInflationCalculatorService;
  }

  public void Run()
  {
    Csv csv = ReadCsv();
    Csv augmentedCsv = AugmentData(csv);
    WriteCsv(augmentedCsv);
  }

  private Csv ReadCsv()
  {
    try
    {
      Csv csv = Csv.ReadSimple(_augmentDataConfig.CsvFilePath, _augmentDataConfig.CsvDelimiter).Validate();

      if (csv.Header is null)
      {
        throw new InvalidDataException("Missing augmentation data header");
      }

      return csv;
    }
    catch (Exception e)
    {
      _logger.LogError("Error while reading data for augmentation. Error: {0}", e.Message);

      throw;
    }
  }

  private Csv AugmentData(Csv data)
  {
    try
    {
      int dateHeaderKeyIndex = data.GetHeaderKeyIndex(_augmentDataConfig.DateHeaderKey) ??
        throw new InvalidDataException("Missing date header key");
      int? amountHeaderKeyIndex = data.GetHeaderKeyIndex(_augmentDataConfig.AmountHeaderKey);

      string[] augmentedHeader = data.Header!
        .ConcatNonNull([
          _augmentDataConfig.AddInflationFactor ? _augmentDataConfig.InflationFactorHeaderKey : null,
          _augmentDataConfig.AddInflatedAmount ? _augmentDataConfig.InflationAdjustedAmountHeaderKey : null,
          _augmentDataConfig.AddReverseInflationFactor ? _augmentDataConfig.ReverseInflationFactorHeaderKey : null,
          _augmentDataConfig.AddReverseInflatedAmount ? _augmentDataConfig.ReverseInflationAdjustedAmountHeaderKey : null
        ])
        .ToArray();

      List<string[]> augmentedContent = data.Content
        .Select(lineItems =>
        {
          string inflationFactor = string.Empty;
          string inflatedAmount = string.Empty;
          string reverseInflationFactor = string.Empty;
          string reverseInflatedAmount = string.Empty;

          try
          {
            DateTime date = DateTime.Parse(lineItems[dateHeaderKeyIndex]);
            decimal inflationFactorValue = _monthlyInflationCalculatorService.Calculate(date);

            inflationFactor = ToString(inflationFactorValue, _augmentDataConfig.FactorDecimalPlaces);
            reverseInflationFactor = ToString(1 / inflationFactorValue, _augmentDataConfig.FactorDecimalPlaces);

            if (amountHeaderKeyIndex.HasValue)
            {
              decimal amount =  decimal.Parse(lineItems[amountHeaderKeyIndex.Value]);

              inflatedAmount = ToString(amount * inflationFactorValue, _augmentDataConfig.AmountDecimalPlaces);
              reverseInflatedAmount = ToString(amount / inflationFactorValue, _augmentDataConfig.AmountDecimalPlaces);
            }
          }
          catch (Exception e)
          {
            _logger.LogWarning("Problem while augmenting data line. Problem: {0}", e.Message);

          }

          return lineItems
            .ConcatNonNull([
              _augmentDataConfig.AddInflationFactor ? inflationFactor : null,
              _augmentDataConfig.AddInflatedAmount ? inflatedAmount : null,
              _augmentDataConfig.AddReverseInflationFactor ? reverseInflationFactor : null,
              _augmentDataConfig.AddReverseInflatedAmount ? reverseInflatedAmount : null
            ])
            .ToArray();
        })
        .ToList();

      return new Csv(augmentedHeader, augmentedContent);
    }
    catch (Exception e)
    {
      _logger.LogError("Error while augmenting data. Error: {0}", e.Message);

      throw;
    }
  }

  private void WriteCsv(Csv csv)
  {
    try
    {
      csv.WriteSimple(_augmentDataConfig.CsvFilePath, _augmentDataConfig.CsvDelimiter);
    }
    catch (Exception e)
    {
      _logger.LogError("Error while writing augmented data. Error: {0}", e.Message);

      throw;
    }
  }

  private static string ToString(decimal value, uint decimalPlaces) =>
    decimal.Round(value, (int)decimalPlaces).ToString();
}