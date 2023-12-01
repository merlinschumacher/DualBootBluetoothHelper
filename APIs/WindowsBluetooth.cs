using DualBootBluetoothHelper.Helpers;
using DualBootBluetoothHelper.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace DualBootBluetoothHelper.APIs
{
    /// <summary>A class to access the Windows Bluetooth API and registry data</summary>
    public class WindowsBluetooth
    {
        // This AQS selects all the Bluetooth adapters.
        private readonly string _bluetoothAdapterAQS = "System.Devices.InterfaceClassGuid:=\"{92383B0E-F90E-4AC9-8D44-8C2D0D0EBDA2}\"";

        // A regex to extract Bluetooth device addresses from a AQS device string. 
        private readonly string _bluetoothDeviceAddressRegex = @"Bluetooth(?:LE)?#Bluetooth(?:LE)?(.*)-(.*)";
        // This AQS selects all the Bluetooth devices.
        private readonly string _bluetoothDeviceAQS = "System.Devices.DevObjectType:=5 AND System.Devices.Aep.ProtocolId:=\"{E0CBF06C-CD8B-4647-BB8A-263B43F0F974}\" AND (System.Devices.Aep.IsPaired:=System.StructuredQueryType.Boolean#True OR System.Devices.Aep.Bluetooth.IssueInquiry:=System.StructuredQueryType.Boolean#False)";
        // The registry key where the Bluetooth connection keys are stored.
        private readonly string _bluetoothKeysRegistryKey = @"SYSTEM\CurrentControlSet\Services\BTHPORT\Parameters\Keys";

        // This AQS selects all the Bluetooth LE devices.
        private readonly string _bluetoothLEDeviceAQS = "System.Devices.DevObjectType:=5 AND System.Devices.Aep.ProtocolId:=\"{BB7BB05E-5972-42B5-94FC-76EAA7084D49}\" AND (System.Devices.Aep.IsPaired:=System.StructuredQueryType.Boolean#True OR System.Devices.Aep.Bluetooth.IssueInquiry:=System.StructuredQueryType.Boolean#False)";
        private readonly ILogger _logger;

        /// <summary>Initializes a new instance of the <see cref="WindowsBluetooth" /> class.</summary>
        /// <param name="loggerFactory">A logger factory.</param>
        /// <exception cref="System.InvalidOperationException">When the API is called outside of a Windows OS</exception>
        public WindowsBluetooth(ILoggerFactory loggerFactory)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new InvalidOperationException("This is not a Windows OS. You can't call this API from another OS.");
            }
            _logger = loggerFactory.CreateLogger("WindowsBluetooth");
        }

        /// <summary>Retrieves all Bluetooth adapters and their devices from the Windows Bluetooth API and registry.</summary>
        /// <returns>A List of DbbhBluetoothAdapter</returns>
        public async Task<List<DbbhBluetoothAdapter>> ListBluetoothDevicesByAdapter()
        {
            var adaptersDeviceManager = await ListBluetoothAdaptersFromDeviceManager();
            var adaptersRegistry = ListBluetoothAdaptersFromRegistry();
            var adapters = adaptersDeviceManager.UnionBy(adaptersRegistry, a => a.Address).ToList();
            var devices = await ListBluetoothDevicesFromDeviceManager();

            devices = devices.OrderBy(o => o.Name).ToList();

            foreach (var adapter in adapters)
            {
                var devicesRegistry = ListBluetoothDevicesFromRegistryByAdapter(adapter.Address);
                adapter.Devices = adapter.Devices.Concat(devicesRegistry).ToList();
                adapter.Devices = devices.FindAll(x => x.AdapterAddress == adapter.Address);
                adapter.Devices = adapter.Devices.DistinctBy(d => d.Address).ToList();
                for (int i = 0; i < adapter.Devices.Count; i++)
                {
                    adapter.Devices[i] = GetClassicDeviceKeysFromRegistry(adapter.Devices[i]);
                    adapter.Devices[i] = GetFivePointOneDeviceKeysFromRegistry(adapter.Devices[i]);
                }
                devices = devices.Except(adapter.Devices).ToList();
            }

            adapters.RemoveAll(a => a.Devices.Count == 0);

            if (devices.Count > 0)
                adapters.Add(new DbbhBluetoothAdapter("Devices that can't be matched to any adapter:", 0) { Devices = devices });

            return adapters;
        }

        /// <summary>Retrieves all Bluetooth devices from the Windows Bluetooth API.</summary>
        /// <returns>A List of DbbhBluetoothDevice</returns>
        public async Task<List<DbbhBluetoothDevice>> ListBluetoothDevicesFromDeviceManager()
        {
            List<DbbhBluetoothDevice> bluetoothDevices = new();
            DeviceInformationCollection bluetoothDevicesInfo = await
                   DeviceInformation.FindAllAsync(_bluetoothDeviceAQS);
            DeviceInformationCollection bluetoothLEDevicesInfo = await
                   DeviceInformation.FindAllAsync(_bluetoothLEDeviceAQS);

            var allBluetoothDevices = bluetoothDevicesInfo.Concat(bluetoothLEDevicesInfo);

            foreach (var bluetoothDeviceInfo in allBluetoothDevices)
            {
                var matches = Regex.Match(bluetoothDeviceInfo.Id, _bluetoothDeviceAddressRegex);
                var btAddress = BtConversionHelper.AddressStringToUlong(matches.Groups[2].Value);
                var btAdapterAddress = BtConversionHelper.AddressStringToUlong(matches.Groups[1].Value);

                var bluetoothDeviceInstance = await BluetoothDevice.FromBluetoothAddressAsync(btAddress);
                if (bluetoothDeviceInstance != null)
                {
                    var btDevice = new DbbhBluetoothDevice(bluetoothDeviceInfo.Name, bluetoothDeviceInstance.BluetoothAddress, btAdapterAddress);
                    bluetoothDevices.Add(btDevice);
                }
            }

            return bluetoothDevices;
        }

        /// <summary>Retrieves all Bluetooth devices from the Windows registry by adapter.</summary>
        /// <returns>A List of DbbhBluetoothDevice</returns>
        public List<DbbhBluetoothDevice> ListBluetoothDevicesFromRegistryByAdapter(string btAdapterAddress)
        {
            var bluetoothDevicesRegistryKeys = Registry.LocalMachine.OpenSubKey(_bluetoothKeysRegistryKey + "\\" + btAdapterAddress)?.GetSubKeyNames() ?? Array.Empty<string>();
            var bluetoothDevicesRegistryValues = Registry.LocalMachine.OpenSubKey(_bluetoothKeysRegistryKey + "\\" + btAdapterAddress)?.GetValueNames() ?? Array.Empty<string>();

            var btDevices = new List<DbbhBluetoothDevice>();

            foreach (var key in bluetoothDevicesRegistryKeys)
            {
                _logger.LogDebug("Adding device from key: {key}", key);
                btDevices.Add(new DbbhBluetoothDevice($"Unknown device({key.ToUpperInvariant()})", key, btAdapterAddress));
            }
            foreach (var value in bluetoothDevicesRegistryValues)
            {
                _logger.LogDebug("Found value: {value}", value);
                var btDevice = btDevices.Find(b => b.Address.ToUpperInvariant() == value.ToUpperInvariant());
                if (btDevice == null)
                {
                    _logger.LogDebug("Adding device from value: {value}", value);
                    btDevices.Add(new DbbhBluetoothDevice($"Unknown device({value.ToUpperInvariant()})", value, btAdapterAddress));
                }
            }

            return btDevices;
        }

        /// <summary>Gets the classic device keys from registry.</summary>
        /// <param name="device">The device.</param>
        private DbbhBluetoothDevice GetClassicDeviceKeysFromRegistry(DbbhBluetoothDevice device)
        {
            var regAdapterKey = Registry.LocalMachine.OpenSubKey(_bluetoothKeysRegistryKey + "\\" + device.AdapterAddress);
            if (regAdapterKey == null)
                return device;
            _logger.LogDebug("Reading registry key: {key}", regAdapterKey);
            device.LinkKey = BtConversionHelper.ConvertToHex(regAdapterKey.GetValue(device.Address, null));
            _logger.LogDebug("Got LinkKey: {LinkKey}", device.LinkKey);
            return device;
        }

        /// <summary>Gets the Bluetooth 5.1+ device keys from registry.</summary>
        /// <param name="device">The device.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        private DbbhBluetoothDevice GetFivePointOneDeviceKeysFromRegistry(DbbhBluetoothDevice device)
        {
            var deviceKey = Registry.LocalMachine.OpenSubKey(_bluetoothKeysRegistryKey + "\\" + device.AdapterAddress + "\\" + device.Address);
            if (deviceKey == null)
                return device;
            _logger.LogDebug("Reading registry key: {key}", deviceKey);
            device.IRK = BtConversionHelper.ConvertToHex(deviceKey.GetValue("IRK", null));
            _logger.LogDebug("Got IRK: {IRK}", device.IRK);
            device.LTK = BtConversionHelper.ConvertToHex(deviceKey.GetValue("LTK", null));
            _logger.LogDebug("Got LTK: {LTK}", device.LTK);
            device.Rand = BtConversionHelper.ReverseAndConvertToInt64(deviceKey.GetValue("ERand", null));
            _logger.LogDebug("Got Rand: {Rand}", device.Rand);
            device.EDIV = BtConversionHelper.ConvertToHex(deviceKey.GetValue("EDIV", null));
            _logger.LogDebug("Got EDIV: {EDIV}", device.EDIV);
            return device;
        }

        /// <summary>Lists the bluetooth adapters from device manager.</summary>
        /// <returns>
        ///   <br />
        /// </returns>
        private async Task<List<DbbhBluetoothAdapter>> ListBluetoothAdaptersFromDeviceManager()
        {
            DeviceInformationCollection bluetoothAdaptersDeviceInfo = await DeviceInformation.FindAllAsync(_bluetoothAdapterAQS);

            var bluetoothAdapters = new List<DbbhBluetoothAdapter>();

            foreach (var bluetoothAdapterDeviceInfo in bluetoothAdaptersDeviceInfo)
            {
                var bluetoothAdapterInstance = await BluetoothAdapter.FromIdAsync(bluetoothAdapterDeviceInfo.Id);
                bluetoothAdapters.Add(new DbbhBluetoothAdapter(bluetoothAdapterDeviceInfo.Name, bluetoothAdapterInstance.BluetoothAddress));
            }

            return bluetoothAdapters;
        }

        /// <summary>Lists the bluetooth adapters found in the registry.</summary>
        /// <returns>
        ///   <br />
        /// </returns>
        private List<DbbhBluetoothAdapter> ListBluetoothAdaptersFromRegistry()
        {
            var bluetoothAdapterRegistryKeys = Registry.LocalMachine.OpenSubKey(_bluetoothKeysRegistryKey)?.GetSubKeyNames() ?? Array.Empty<string>();

            var bluetoothAdapters = new List<DbbhBluetoothAdapter>();

            foreach (var bluetoothAdapterRegistryKey in bluetoothAdapterRegistryKeys)
            {
                bluetoothAdapters.Add(new DbbhBluetoothAdapter($"Unknown adapter({bluetoothAdapterRegistryKey.ToUpper()})", bluetoothAdapterRegistryKey));
            }

            return bluetoothAdapters;
        }
    }
}