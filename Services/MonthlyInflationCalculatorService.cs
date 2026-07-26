using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinAug.DTOs;
using MinAug.DTOs.Configuration;
using MinAug.Extensions;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MinAug.Services;

internal class MonthlyInflationCalculatorService
{
  private readonly ILogger _logger;
  private readonly IDictionary<string, decimal> _aggregatedFactors;

  public MonthlyInflationCalculatorService(ILogger<MonthlyInflationCalculatorService> logger,
    IOptionsSnapshot<GeneralConfig> generalConfig,
    IOptionsSnapshot<InflationDataConfig> inflationDataConfig)
  {
    _logger = logger;

    IEnumerable<(string Date, decimal Factor)> data =
      ReadMonthlyInflationData(inflationDataConfig.Value.CsvFilePath, delimiter: inflationDataConfig.Value.CsvDelimiter);

    _aggregatedFactors = CreateMultiplers(data, generalConfig.Value.ReferenceDate);
  }

  public decimal Calculate(DateTime date)
  {
    string dateKey = GetDateKey(date);

    if (!_aggregatedFactors.TryGetValue(dateKey, out decimal result))
    {
      _logger.LogError("Given date {date} is out of range", date);
      throw new ArgumentOutOfRangeException(nameof(date));
    }

    return result;
  }

  private IEnumerable<(string DateKey, decimal Factor)> ReadMonthlyInflationData(string filepath, string delimiter)
  {
    try
    {
      Csv csv = Csv.ReadSimple(filepath, delimiter).Validate();
      int nameHeaderKeyIndex = csv.GetHeaderKeyIndex("Name") ?? throw new InvalidDataException("Missing name header key");
      int indexHeaderKeyIndex = csv.GetHeaderKeyIndex("Index") ?? throw new InvalidDataException("Missing index header key");

      return csv.Content
        .Select(items => new
        {
          Name = items[nameHeaderKeyIndex],
          Index = decimal.Parse(items[indexHeaderKeyIndex])
        })
        .Select(obj =>
        {
          if (!Regex.IsMatch(obj.Name, "\\d{4}M\\d{2}"))
          {
            _logger.LogError("Invalid {name} column format: {value}", nameof(obj.Name), obj.Name);
            throw new ArgumentException(nameof(obj.Name));
          }

          return (obj.Name, obj.Index / 100);
        });
    }
    catch (Exception e)
    {
      _logger.LogError("Error while reading monthly inflation data. Error: {0}", e.Message);
      throw;
    }
  }

  private IDictionary<string, decimal> CreateMultiplers(IEnumerable<(string DateKey, decimal Factor)> data, DateTime reference)
  {
    ArgumentNullException.ThrowIfNull(data);

    string referenceDateKey = GetDateKey(reference);

    // filter
    List<(string DateKey, decimal Factor)> filteredInflationDataEntries = data
      .Where(obj => !obj.DateKey.Equals(referenceDateKey) && obj.DateKey.CompareTo(referenceDateKey) < 1)
      .OrderByDescending(obj => obj.DateKey)
      .ToList();

    // validate
    string nextDateKey = GetDateKey(new DateTime(reference.Year, reference.Month, 1).AddMonths(-1));

    filteredInflationDataEntries
      .ForEach(entry =>
      {
        if (nextDateKey != entry.DateKey)
        {
          _logger.LogError("Missing inflation data for {dateKey}", nextDateKey);
          throw new InvalidDataException($"Missing inflation data for {nextDateKey}");
        }

        nextDateKey = GetDateKey(ParseDateKey(nextDateKey).AddMonths(-1));
      });

    // prepare
    decimal multiplier = 1.0m;

    return Array
      .Empty<(string, decimal)>()
      .Concat([(GetDateKey(reference), multiplier)])
      .Concat(filteredInflationDataEntries.Select(obj => (obj.DateKey, multiplier *= obj.Factor)))
      .ToDictionary(obj => obj.Item1, obj => obj.Item2);
  }

  private static string GetDateKey(DateTime date) =>
    date.ToString("yyyy\\MMM");

  private static DateTime ParseDateKey(string input)
  {
    if (!DateTime.TryParseExact(input, "yyyy\\MMM", null, DateTimeStyles.None, out DateTime result))
    {
      throw new FormatException("Invalid date key format");
    }

    return result;
  }
}