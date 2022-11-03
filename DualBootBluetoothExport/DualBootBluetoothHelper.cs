// See https://aka.ms/new-console-template for more information

using DualBootBluetoothHelper.API;
using DualBootBluetoothHelper.Helper;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

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
                .SetMinimumLevel(LogLevel.Debug);
        });
ILogger logger = loggerFactory.CreateLogger<Program>();
logger.LogInformation("DualBootBluetoothHelper - This tool imports and exports bluetooth configurations.");

RequireAdministratorHelper.RequireAdministrator();

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    var windowsBluetooth = new WindowsBluetooth(loggerFactory);

    var windowsBluetoothAdapters = await windowsBluetooth.ListBluetoothDevicesByAdapter();
    logger.LogInformation("Found the following {count} Bluetooth adapters and their devices:", windowsBluetoothAdapters.Count); 
    foreach (var adapter in windowsBluetoothAdapters)
    {
        logger.LogInformation("====");
        logger.LogInformation(adapter.ToString());
        foreach (var device in adapter.Devices)
        {
            logger.LogInformation("----");
            logger.LogInformation(device.ToString());
            logger.LogDebug("LinkKey: {LinkKey}", device.LinkKey);
            logger.LogDebug("LTK: {LTK}", device.LTK);
            logger.LogDebug("IRK: {IRK}", device.IRK);
            logger.LogDebug("Rand: {Rand}", device.Rand);
            logger.LogDebug("EDIV: {EDIV}", device.EDIV);
        }
        logger.LogInformation("----");
    }
    logger.LogInformation("====");
}