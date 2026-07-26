using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinAug.DTOs;
using MinAug.Extensions;
using MinAug.Services;
using MinAug.Utils;

ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.Setup());
ILogger logger = loggerFactory.CreateLogger<Program>();

try
{
  CommandLineArguments? arguments = CommandLineArgumentsFactory.Create(args);

  if (arguments is null)
  {
    return;
  }

  loggerFactory.Dispose();
  loggerFactory = LoggerFactory.Create(builder => builder.Setup(arguments.MinimumLogLevel));
  logger = loggerFactory.CreateLogger<Program>();

  IConfigurationRoot configurationRoot = IConfigurationRoot.Create(arguments.ConfigurationFilepath);

  ServiceProvider serviceProvider = IServiceCollection.Create()
    .SetupLogging(arguments.MinimumLogLevel)
    .SetupConfiguration(configurationRoot)
    .BuildServiceProvider();

  serviceProvider
    .GetRequiredService<DataAugmenterService>()
    .Run();
}
catch (Exception e)
{
  logger.LogCritical("Error while executing program. Error: {0}", e.Message);
}
finally
{
  loggerFactory.Dispose();
}