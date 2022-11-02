using DualBootBluetoothHelper.Model;
using System.Text.RegularExpressions;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace DualBootBluetoothHelper.API
{
    public class WindowsBluetooth
    {
        private readonly string _bluetoothAdapterAQS = "System.Devices.InterfaceClassGuid:=\"{92383B0E-F90E-4AC9-8D44-8C2D0D0EBDA2}\"";
        private readonly string _bluetoothDeviceAQS = "System.Devices.DevObjectType:=5 AND System.Devices.Aep.ProtocolId:=\"{E0CBF06C-CD8B-4647-BB8A-263B43F0F974}\" AND (System.Devices.Aep.IsPaired:=System.StructuredQueryType.Boolean#True OR System.Devices.Aep.Bluetooth.IssueInquiry:=System.StructuredQueryType.Boolean#False)";
        private readonly string _bluetoothLEDeviceAQS = "System.Devices.DevObjectType:=5 AND System.Devices.Aep.ProtocolId:=\"{BB7BB05E-5972-42B5-94FC-76EAA7084D49}\" AND (System.Devices.Aep.IsPaired:=System.StructuredQueryType.Boolean#True OR System.Devices.Aep.Bluetooth.IssueInquiry:=System.StructuredQueryType.Boolean#False)";
        private const string _bluetoothDeviceAddressRegex = @"Bluetooth(?:LE)?#Bluetooth(?:LE)?(.*)-(.*)";

        public WindowsBluetooth()
        {
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
                    bluetoothDevices.Add(new DBBHBluetoothDevice(bluetoothDeviceInfo.Name, bluetoothDeviceInstance.BluetoothAddress, btAdapterAddress));
            }
            foreach (var bluetoothLEDeviceInfo in bluetoothLEDevicesInfo)
            {
                var matches = Regex.Match(bluetoothLEDeviceInfo.Id, _bluetoothDeviceAddressRegex);
                var btAddress = _btAddressStringToUlong(matches.Groups[2].Value);
                var btAdapterAddress = _btAddressStringToUlong(matches.Groups[1].Value);

                var bluetoothLEDeviceInstance = await BluetoothLEDevice.FromBluetoothAddressAsync(btAddress);
                if (bluetoothLEDeviceInstance != null)
                    bluetoothDevices.Add(new DBBHBluetoothDevice(bluetoothLEDeviceInfo.Name, bluetoothLEDeviceInstance.BluetoothAddress, btAdapterAddress));
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
    }
}