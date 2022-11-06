using DualBootBluetoothHelper.API;
using DualBootBluetoothHelper.Helper;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Text.Json;

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
                .SetMinimumLevel(LogLevel.Debug);
#else
;
#endif
        });
ILogger logger = loggerFactory.CreateLogger<Program>();
logger.LogInformation("DualBootBluetoothHelper - This tool imports and exports bluetooth configurations.");

try
{
    RequireAdministratorHelper.RequireAdministrator();
} catch (Exception ex)
{
    if (ex is InvalidOperationException)
    {
        logger.LogError("DualBootBluetoothHelper needs administrative/root priviledges to run! On Windows systems it needs to be run with psexec -s to work.");
    }
}

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    var windowsBluetooth = new WindowsBluetooth(loggerFactory);

    var windowsBluetoothAdapters = await windowsBluetooth.ListBluetoothDevicesByAdapter();
    logger.LogInformation("Found the following {count} Bluetooth adapters and their devices:", windowsBluetoothAdapters.Count);
    foreach (var adapter in windowsBluetoothAdapters)
    {
        logger.LogInformation("====");
        logger.LogInformation("{adapter}",adapter.ToString());
        foreach (var device in adapter.Devices)
        {
            logger.LogInformation("{device}",device.ToString());
            logger.LogDebug("LinkKey: {LinkKey}", device.LinkKey);
            logger.LogDebug("LTK: {LTK}", device.LTK);
            logger.LogDebug("IRK: {IRK}", device.IRK);
            logger.LogDebug("Rand: {Rand}", device.Rand);
            logger.LogDebug("EDIV: {EDIV}", device.EDIV);
        }
    }
    logger.LogInformation("====");

    logger.LogInformation("dumping to bt.json");
    var json = JsonSerializer.Serialize(windowsBluetoothAdapters, new JsonSerializerOptions { WriteIndented = true });
    logger.LogDebug("{json}",json);
    await File.WriteAllTextAsync("bt.json", json);

}