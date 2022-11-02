using DualBootBluetoothHelper.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace DualBootBluetoothHelper.API
{
    public class WindowsBluetooth
    {

        private ILogger _logger;

        private readonly string _bluetoothAdapterAQS = "System.Devices.InterfaceClassGuid:=\"{92383B0E-F90E-4AC9-8D44-8C2D0D0EBDA2}\"";
        private readonly string _bluetoothDeviceAQS = "System.Devices.DevObjectType:=5 AND System.Devices.Aep.ProtocolId:=\"{E0CBF06C-CD8B-4647-BB8A-263B43F0F974}\" AND (System.Devices.Aep.IsPaired:=System.StructuredQueryType.Boolean#True OR System.Devices.Aep.Bluetooth.IssueInquiry:=System.StructuredQueryType.Boolean#False)";
        private readonly string _bluetoothLEDeviceAQS = "System.Devices.DevObjectType:=5 AND System.Devices.Aep.ProtocolId:=\"{BB7BB05E-5972-42B5-94FC-76EAA7084D49}\" AND (System.Devices.Aep.IsPaired:=System.StructuredQueryType.Boolean#True OR System.Devices.Aep.Bluetooth.IssueInquiry:=System.StructuredQueryType.Boolean#False)";
        private readonly string _bluetoothDeviceAddressRegex = @"Bluetooth(?:LE)?#Bluetooth(?:LE)?(.*)-(.*)";
        private readonly string _bluetoothKeysRegistryKey = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\BTHPORT\Parameters\Keys";

        public WindowsBluetooth(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger("WindowsBluetooth");
        }

        public async Task<List<DBBHBluetoothAdapter>> ListBluetoothAdapters()
        {
            DeviceInformationCollection bluetoothAdaptersDeviceInfo = await DeviceInformation.FindAllAsync(_bluetoothAdapterAQS);

            var bluetoothAdapters = new List<DBBHBluetoothAdapter>();

            foreach (var bluetoothAdapterDeviceInfo in bluetoothAdaptersDeviceInfo)
            {
                var bluetoothAdapterInstance = await BluetoothAdapter.FromIdAsync(bluetoothAdapterDeviceInfo.Id);
                bluetoothAdapters.Add(new DBBHBluetoothAdapter(bluetoothAdapterDeviceInfo.Name, bluetoothAdapterInstance.BluetoothAddress));
            }

            return bluetoothAdapters;
        }

        public async Task<List<DBBHBluetoothDevice>> ListBluetoothDevices()
        {
            List<DBBHBluetoothDevice> bluetoothDevices = new();
            DeviceInformationCollection bluetoothDevicesInfo = await
                   DeviceInformation.FindAllAsync(_bluetoothDeviceAQS);
            DeviceInformationCollection bluetoothLEDevicesInfo = await
                   DeviceInformation.FindAllAsync(_bluetoothLEDeviceAQS);

            foreach (var bluetoothDeviceInfo in bluetoothDevicesInfo)
            {
                var matches = Regex.Match(bluetoothDeviceInfo.Id, _bluetoothDeviceAddressRegex);
                var btAddress = _btAddressStringToUlong(matches.Groups[2].Value);
                var btAdapterAddress = _btAddressStringToUlong(matches.Groups[1].Value);

                var bluetoothDeviceInstance = await BluetoothDevice.FromBluetoothAddressAsync(btAddress);
                if (bluetoothDeviceInstance != null)
                {
                    var btDevice = new DBBHBluetoothDevice(bluetoothDeviceInfo.Name, bluetoothDeviceInstance.BluetoothAddress, btAdapterAddress);
                    btDevice = GetFivePointOneDeviceKeysFromRegistry(btDevice);
                    bluetoothDevices.Add(btDevice);

                }
            }
            foreach (var bluetoothLEDeviceInfo in bluetoothLEDevicesInfo)
            {
                var matches = Regex.Match(bluetoothLEDeviceInfo.Id, _bluetoothDeviceAddressRegex);
                var btAddress = _btAddressStringToUlong(matches.Groups[2].Value);
                var btAdapterAddress = _btAddressStringToUlong(matches.Groups[1].Value);

                var bluetoothLEDeviceInstance = await BluetoothLEDevice.FromBluetoothAddressAsync(btAddress);
                if (bluetoothLEDeviceInstance != null)
                {
                    var btDevice = new DBBHBluetoothDevice(bluetoothLEDeviceInfo.Name, bluetoothLEDeviceInstance.BluetoothAddress, btAdapterAddress);
                    btDevice = GetFivePointOneDeviceKeysFromRegistry(btDevice);
                    bluetoothDevices.Add(btDevice);
                }
            }

            return bluetoothDevices;
        }

        private ulong _btAddressStringToUlong(string address)
        {
            address = address.Replace(":", "");
            return Convert.ToUInt64(address, 16);
        }

        public async Task<List<DBBHBluetoothAdapter>> ListBluetoothDevicesByAdapter()
        {
            var adapters = await ListBluetoothAdapters();
            var devices = await ListBluetoothDevices();
            devices = devices.OrderBy(o => o.Name).ToList();

            foreach (var adapter in adapters)
            {
                adapter.Devices = devices.FindAll(x => x.AdapterAddress.SequenceEqual(adapter.Address));
                devices = devices.Except(adapter.Devices).ToList();
            }

            adapters.Add(new DBBHBluetoothAdapter("Unknown adapter", 0) { Devices = devices });

            return adapters;
        }

        public DBBHBluetoothDevice GetFivePointOneDeviceKeysFromRegistry(DBBHBluetoothDevice device)
        {
            var regDeviceKey = _bluetoothKeysRegistryKey + "\\" + Convert.ToHexString(device.AdapterAddress) + "\\" + Convert.ToHexString(device.Address);
            _logger.LogDebug("Reading registry key: " + regDeviceKey);
            device.IRK = _convertToHex(Registry.GetValue(regDeviceKey, "IRK", null));
            _logger.LogDebug("Got IRK: " + device.IRK);
            device.LTK = _convertToHex(Registry.GetValue(regDeviceKey, "LTK", null));
            _logger.LogDebug("Got LTK: " + device.LTK);
            device.Rand = _reverseAndConvertToInt64(Registry.GetValue(regDeviceKey, "ERand", null));
            _logger.LogDebug("Got Rand: " + device.Rand);
            device.EDIV = _convertToHex(Registry.GetValue(regDeviceKey, "EDIV", null));
            _logger.LogDebug("Got EDIV: " + device.EDIV);
            return device;
        }

        private String _convertToHex(object? obj)
        {
            if (obj == null) return "";
            if (obj.GetType() == typeof(byte[]))
                return BitConverter.ToString((byte[])obj).Replace("-", string.Empty);
            UInt64 intObj = Convert.ToUInt64(obj);
            return intObj.ToString("X8");
        }
        private UInt64 _reverseAndConvertToInt64(object? obj)
        {
            if (obj == null) return 0;
            UInt64 intObj = (UInt64)(Int64)obj;
            UInt64 rev = _reverseBytes(intObj);
            return rev;
        }

        private static UInt64 _reverseBytes(UInt64 value)
        {
            return (value & 0x00000000000000FFUL) << 56 | (value & 0x000000000000FF00UL) << 40 |
                   (value & 0x0000000000FF0000UL) << 24 | (value & 0x00000000FF000000UL) << 8 |
                   (value & 0x000000FF00000000UL) >> 8 | (value & 0x0000FF0000000000UL) >> 24 |
                   (value & 0x00FF000000000000UL) >> 40 | (value & 0xFF00000000000000UL) >> 56;
        }

    }
}