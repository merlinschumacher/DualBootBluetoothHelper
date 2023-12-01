using DualBootBluetoothHelper.APIs;
using DualBootBluetoothHelper.Helpers;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.CommandLine;
using DualBootBluetoothHelper.Models;
using System.CommandLine.Invocation;

using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddFilter("DualBootBluetoothHelper.Program", LogLevel.Debug)
                .AddSimpleConsole(options =>
                {
                    options.IncludeScopes = false;
                    options.SingleLine = true;
                })
#if DEBUG
                .SetMinimumLevel(LogLevel.Debug)
#endif
;
        });
ILogger logger = loggerFactory.CreateLogger<Program>();


async Task<List<DbbhBluetoothAdapter>> WindowsGetDbbhBluetoothAdaptersAsync()
{

    var windowsBluetooth = new WindowsBluetooth(loggerFactory);

    var windowsBluetoothAdapters = await windowsBluetooth.ListBluetoothDevicesByAdapter();
    logger.LogInformation("Found the following {count} Bluetooth adapters and their devices:", windowsBluetoothAdapters.Count);
    foreach (var adapter in windowsBluetoothAdapters)
    {
        logger.LogInformation("====");
        logger.LogInformation("{adapter}", adapter.ToString());
        foreach (var device in adapter.Devices)
        {
            logger.LogInformation("{device}", device.ToString());
            logger.LogDebug("LinkKey: {LinkKey}", device.LinkKey);
            logger.LogDebug("LTK: {LTK}", device.LTK);
            logger.LogDebug("IRK: {IRK}", device.IRK);
            logger.LogDebug("Rand: {Rand}", device.Rand);
            logger.LogDebug("EDIV: {EDIV}", device.EDIV);
        }
    }
    logger.LogInformation("====");

    return windowsBluetoothAdapters;
}

void SaveAdaptersToJson(List<DbbhBluetoothAdapter> adapters, string filename)
{
    logger.LogInformation("Saving adapters to {outputFile}", filename);
    var json = JsonSerializer.Serialize(adapters, new JsonSerializerOptions { WriteIndented = true });
    logger.LogDebug("{json}", json);
    File.WriteAllText(filename, json);
}

#if !DEBUG
          // Check if we have administrative rights.
          try
          {
              RequireAdministratorHelper.RequireAdministrator();
          }
          catch (Exception ex)
          {
              if (ex is InvalidOperationException)
              {
                  logger.LogError("DualBootBluetoothHelper needs administrative/root priviledges to run! On Windows systems it needs to be run with 'psexec.exe -s' to get System access to work.");
                  System.Environment.Exit(1);
              }
              throw;
          }
#endif

var rootCommand = new RootCommand();


var exportFile = new Option<string>
    (name: "--export",
    getDefaultValue: () => "bt.json",
    description: "Export the Bluetooth keys to the given file.");
exportFile.AddAlias("-e");

rootCommand.AddOption(exportFile);

rootCommand.SetHandler((exportFileValue) =>
      {
          //logger.LogInformation("DualBootBluetoothHelper - This tool exports bluetooth configurations.");
          //logger.LogInformation("Pass a file name as the first argument to define an output file for the Bluetooth information.");


          Console.WriteLine($"--delay = {exportFileValue}");
          if (!string.IsNullOrEmpty(exportFileValue?.InvocationResult.ToString()))
          {
              var adapters = new List<DbbhBluetoothAdapter>();
              if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                  adapters = WindowsGetDbbhBluetoothAdaptersAsync().Result;
              SaveAdaptersToJson(adapters, exportFileValue.ParseResult.ToString());
          }

#if DEBUG
          Console.ReadKey();
#endif
      });

await rootCommand.InvokeAsync(args);

