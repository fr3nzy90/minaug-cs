using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using minaug.DTOs.Configuration;
using minaug.Services;

namespace minaug.Extensions;

internal static class SetupExtensions
{
  extension(ILoggingBuilder instance)
  {
    public ILoggingBuilder Setup(LogLevel minimumLogLevel = LogLevel.Trace) =>
      instance
        .SetMinimumLevel(minimumLogLevel)
        .AddSimpleConsole(options =>
        {
          options.SingleLine = true;
          options.IncludeScopes = true;
          options.UseUtcTimestamp = true;
          options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
        });
  }

  extension(IServiceCollection instance)
  {
    public static IServiceCollection Create() =>
      new ServiceCollection()
        .AddOptions()
        .AddSingleton<MonthlyInflationCalculatorService>()
        .AddSingleton<DataAugmenterService>();

    public IServiceCollection SetupLogging(LogLevel minimumLogLevel) =>
      instance
        .AddLogging(builder => builder.Setup(minimumLogLevel));

    public IServiceCollection SetupConfiguration(IConfigurationRoot configuration) =>
      instance
        .Configure<GeneralConfig>(configuration.GetSection(nameof(GeneralConfig).Replace("Config", "")))
        .Configure<InflationDataConfig>(configuration.GetSection(nameof(InflationDataConfig).Replace("Config", "")))
        .Configure<AugmentDataConfig>(configuration.GetSection(nameof(AugmentDataConfig).Replace("Config", "")));
  }

  extension(IConfigurationRoot instance)
  {
    public static IConfigurationRoot Create(string filepath) =>
      new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile(filepath, false, true)
        .Build();
  }
}