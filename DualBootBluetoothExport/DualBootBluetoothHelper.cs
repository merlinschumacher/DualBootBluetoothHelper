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

    //var windowsBluetoothAdapters = await windowsBluetooth.ListBluetoothAdapters();
    //logger.LogInformation("Found the following Bluetooth adapters:");
    //foreach (var adapter in windowsBluetoothAdapters)
    //    logger.LogInformation(adapter.ToString());

    //var windowsBluetoothDevices = await windowsBluetooth.ListBluetoothDevices();
    //logger.LogInformation("Found the following Bluetooth devices:");
    //foreach (var device in windowsBluetoothDevices)
    //    logger.LogInformation(device.ToString());

    var windowsBluetoothAdapters = await windowsBluetooth.ListBluetoothDevicesByAdapter();
    logger.LogInformation("Found the following Bluetooth adapters and devices:");
    foreach (var adapter in windowsBluetoothAdapters)
    {
        logger.LogInformation(adapter.ToString());
        foreach (var device in adapter.Devices)
        {
            logger.LogInformation(device.ToString());
            logger.LogDebug("LTK:" + device.LTK);
            logger.LogDebug("IRK:" + device.IRK);
            logger.LogDebug("ERand:" + device.Rand);
            logger.LogDebug("EDIV:" + device.EDIV);
        }
    }
}