using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using minaug.DTOs;
using minaug.DTOs.Configuration;
using minaug.Extensions;
using System.Text.RegularExpressions;

namespace minaug.Services;

internal class MonthlyInflationCalculatorService
{
  private readonly ILogger _logger;
  private readonly IDictionary<(DateTime RangeStart, DateTime RangeEnd), decimal> _aggregatedFactors;

  public MonthlyInflationCalculatorService(ILogger<MonthlyInflationCalculatorService> logger,
    IOptionsSnapshot<GeneralConfig> generalConfig,
    IOptionsSnapshot<InflationDataConfig> inflationDataConfig)
  {
    _logger = logger;

    IEnumerable<(int Year, int Month, decimal Factor)> data =
      ReadMonthlyInflationData(inflationDataConfig.Value.CsvFilePath, delimiter: inflationDataConfig.Value.CsvDelimiter);

    _aggregatedFactors = CreateMultiplers(data, generalConfig.Value.ReferenceDate);
  }

  public decimal Calculate(DateTime date)
  {
    (DateTime RangeStart, DateTime RangeEnd) range = GetRange(date.Year, date.Month);

    if (!_aggregatedFactors.TryGetValue(range, out decimal result))
    {
      _logger.LogError("Given date {date} is out of range", date);
      throw new ArgumentOutOfRangeException(nameof(date));
    }

    return result;
  }

  private IEnumerable<(int Year, int Month, decimal Factor)> ReadMonthlyInflationData(string filepath, string delimiter)
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

          return (int.Parse(obj.Name.Substring(0, 4)),
            int.Parse(obj.Name.Substring(5, 2)),
            obj.Index / 100);
        });
    }
    catch (Exception e)
    {
      _logger.LogError("Error while reading monthly inflation data. Error: {0}", e.Message);
      throw;
    }
  }

  private IDictionary<(DateTime RangeStart, DateTime RangeEnd), decimal> CreateMultiplers(
    IEnumerable<(int Year, int Month, decimal Factor)> data, DateTime reference)
  {
    ArgumentNullException.ThrowIfNull(data);

    // filter
    List<(int Year, int Month, decimal Factor)> filteredInflationDataEntries = data
      .Where(obj => !(obj.Year, obj.Month).Equals((reference.Year, reference.Month)) &&
        (obj.Year, obj.Month).CompareTo((reference.Year, reference.Month)) < 1)
      .OrderByDescending(obj => (obj.Year, obj.Month))
      .ToList();

    // validate
    (int Year, int Month) next = (reference.Year, reference.Month - 1);

    filteredInflationDataEntries
      .ForEach(entry =>
      {
        if (next != (entry.Year, entry.Month))
        {
          _logger.LogError("Missing inflation data for {name1}={value1} {name2}={value2}", nameof(next.Year), next.Year,
            nameof(next.Month), next.Month);
          throw new InvalidDataException($"Missing inflation data for {nameof(next.Year)}={next.Year} {nameof(next.Month)}={next.Month}");
        }
        next = entry.Month == 1 ? (entry.Year - 1, 12) : (entry.Year, entry.Month - 1);
      });

    // prepare
    decimal multiplier = 1.0m;

    return Array
      .Empty<((DateTime, DateTime), decimal)>()
      .Concat([(GetRange(reference.Year, reference.Month), multiplier)])
      .Concat(filteredInflationDataEntries.Select(obj => (GetRange(obj.Year, obj.Month), multiplier *= obj.Factor)))
      .ToDictionary(obj => obj.Item1, obj => obj.Item2);
  }

  private static (DateTime RangeStart, DateTime RangeEnd) GetRange(int year, int month)
  {
    (int endYear, int endMonth) = month == 12 ? (year + 1, 1) : (year, month + 1);

    return (new DateTime(year, month, 1), new DateTime(endYear, endMonth, 1));
  }
}