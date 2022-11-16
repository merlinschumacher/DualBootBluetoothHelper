using DualBootBluetoothHelper.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace DualBootBluetoothHelper.API
{
    /// <summary>A class to access the Windows Bluetooth API and registry data</summary>
    public class WindowsBluetooth
    {
        private readonly string _bluetoothAdapterAQS = "System.Devices.InterfaceClassGuid:=\"{92383B0E-F90E-4AC9-8D44-8C2D0D0EBDA2}\"";
        private readonly string _bluetoothDeviceAddressRegex = @"Bluetooth(?:LE)?#Bluetooth(?:LE)?(.*)-(.*)";
        private readonly string _bluetoothDeviceAQS = "System.Devices.DevObjectType:=5 AND System.Devices.Aep.ProtocolId:=\"{E0CBF06C-CD8B-4647-BB8A-263B43F0F974}\" AND (System.Devices.Aep.IsPaired:=System.StructuredQueryType.Boolean#True OR System.Devices.Aep.Bluetooth.IssueInquiry:=System.StructuredQueryType.Boolean#False)";
        private readonly string _bluetoothKeysRegistryKey = @"SYSTEM\CurrentControlSet\Services\BTHPORT\Parameters\Keys";
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
#pragma warning disable S4143 // Collection elements should not be replaced unconditionally
                    adapter.Devices[i] = GetFivePointOneDeviceKeysFromRegistry(adapter.Devices[i]);
#pragma warning restore S4143 // Collection elements should not be replaced unconditionally
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

            foreach (var bluetoothDeviceInfo in bluetoothDevicesInfo)
            {
                var matches = Regex.Match(bluetoothDeviceInfo.Id, _bluetoothDeviceAddressRegex);
                var btAddress = BtAddressStringToUlong(matches.Groups[2].Value);
                var btAdapterAddress = BtAddressStringToUlong(matches.Groups[1].Value);

                var bluetoothDeviceInstance = await BluetoothDevice.FromBluetoothAddressAsync(btAddress);
                if (bluetoothDeviceInstance != null)
                {
                    var btDevice = new DbbhBluetoothDevice(bluetoothDeviceInfo.Name, bluetoothDeviceInstance.BluetoothAddress, btAdapterAddress);
                    bluetoothDevices.Add(btDevice);
                }
            }
            foreach (var bluetoothLEDeviceInfo in bluetoothLEDevicesInfo)
            {
                var matches = Regex.Match(bluetoothLEDeviceInfo.Id, _bluetoothDeviceAddressRegex);
                var btAddress = BtAddressStringToUlong(matches.Groups[2].Value);
                var btAdapterAddress = BtAddressStringToUlong(matches.Groups[1].Value);

                var bluetoothLEDeviceInstance = await BluetoothLEDevice.FromBluetoothAddressAsync(btAddress);
                if (bluetoothLEDeviceInstance != null)
                {
                    var btDevice = new DbbhBluetoothDevice(bluetoothLEDeviceInfo.Name, bluetoothLEDeviceInstance.BluetoothAddress, btAdapterAddress);
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

        private static ulong BtAddressStringToUlong(string address)
        {
            address = address.Replace(":", "");
            return Convert.ToUInt64(address, 16);
        }

        private static String ConvertToHex(object? obj)
        {
            if (obj == null) return "";
            if (obj.GetType() == typeof(byte[]))
                return BitConverter.ToString((byte[])obj).Replace("-", string.Empty).ToUpperInvariant();
            UInt64 intObj = Convert.ToUInt64(obj);
            return intObj.ToString("X8").ToUpperInvariant();
        }

        private static UInt64? ReverseAndConvertToInt64(object? obj)
        {
            if (obj == null) return null;
            UInt64 intObj = (UInt64)(Int64)obj;
            UInt64 rev = ReverseBytes(intObj);
            return rev;
        }

        private static UInt64 ReverseBytes(UInt64 value)
        {
            return (value & 0x00000000000000FFUL) << 56 | (value & 0x000000000000FF00UL) << 40 |
                   (value & 0x0000000000FF0000UL) << 24 | (value & 0x00000000FF000000UL) << 8 |
                   (value & 0x000000FF00000000UL) >> 8 | (value & 0x0000FF0000000000UL) >> 24 |
                   (value & 0x00FF000000000000UL) >> 40 | (value & 0xFF00000000000000UL) >> 56;
        }

        private DbbhBluetoothDevice GetClassicDeviceKeysFromRegistry(DbbhBluetoothDevice device)
        {
            var regAdapterKey = Registry.LocalMachine.OpenSubKey(_bluetoothKeysRegistryKey + "\\" + device.AdapterAddress);
            if (regAdapterKey == null)
                return device;
            _logger.LogDebug("Reading registry key: {key}", regAdapterKey);
            device.LinkKey = ConvertToHex(regAdapterKey.GetValue(device.Address, null));
            _logger.LogDebug("Got LinkKey: {LinkKey}", device.LinkKey);
            return device;
        }

        private DbbhBluetoothDevice GetFivePointOneDeviceKeysFromRegistry(DbbhBluetoothDevice device)
        {
            var deviceKey = Registry.LocalMachine.OpenSubKey(_bluetoothKeysRegistryKey + "\\" + device.AdapterAddress + "\\" + device.Address);
            if (deviceKey == null)
                return device;
            _logger.LogDebug("Reading registry key: {key}", deviceKey);
            device.IRK = ConvertToHex(deviceKey.GetValue("IRK", null));
            _logger.LogDebug("Got IRK: {IRK}", device.IRK);
            device.LTK = ConvertToHex(deviceKey.GetValue("LTK", null));
            _logger.LogDebug("Got LTK: {LTK}", device.LTK);
            device.Rand = ReverseAndConvertToInt64(deviceKey.GetValue("ERand", null));
            _logger.LogDebug("Got Rand: {Rand}", device.Rand);
            device.EDIV = ConvertToHex(deviceKey.GetValue("EDIV", null));
            _logger.LogDebug("Got EDIV: {EDIV}", device.EDIV);
            return device;
        }

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

        private List<DbbhBluetoothAdapter> ListBluetoothAdaptersFromRegistry()
        {
            var bluetoothAdapterRegistryKeys = Registry.LocalMachine.OpenSubKey(_bluetoothKeysRegistryKey)?.GetSubKeyNames() ?? Array.Empty<string>();

            var bluetoothAdapters = new List<DbbhBluetoothAdapter>();

            foreach (var bluetoothAdapterRegistryKey in bluetoothAdapterRegistryKeys)
            {
                bluetoothAdapters.Add(new DbbhBluetoothAdapter($"Unknown adapter({bluetoothAdapterRegistryKey.ToUpper()})", BtAddressStringToUlong(bluetoothAdapterRegistryKey)));
            }

            return bluetoothAdapters;
        }
    }
}