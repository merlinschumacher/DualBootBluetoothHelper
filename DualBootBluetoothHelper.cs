using DualBootBluetoothHelper.API;
using DualBootBluetoothHelper.Helper;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Text.Json;

var jsonOutputFile = "bt-out.json";

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
logger.LogInformation("DualBootBluetoothHelper - This tool exports bluetooth configurations.");
logger.LogInformation("Pass a file name as the first argument to define an output file for the Bluetooth information.");

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
}


// Set the output file to the first argument, if the argument ist given.
if (args.Length > 0 && !String.IsNullOrEmpty(args[0]))
{
    jsonOutputFile = args[0];
}

// Retrieve all Windows Bluetooth adapters and devices.
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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

    logger.LogInformation("dumping to {outputFile}", jsonOutputFile);
    var json = JsonSerializer.Serialize(windowsBluetoothAdapters, new JsonSerializerOptions { WriteIndented = true });
    logger.LogDebug("{json}", json);
    await File.WriteAllTextAsync(jsonOutputFile, json);
}