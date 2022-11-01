using DualBootBluetoothHelper.Model;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace DualBootBluetoothHelper.API
{
    public class WindowsBluetooth
    {

        private readonly string _bluetoothAdapterAQS = "System.Devices.InterfaceClassGuid:=\"{92383B0E-F90E-4AC9-8D44-8C2D0D0EBDA2}\"";
        //private readonly string _bluetoothDeviceAQS = "System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\"";
        private readonly string _bluetoothDeviceAQS = "System.Devices.DevObjectType:=5 AND System.Devices.Aep.ProtocolId:=\"{E0CBF06C-CD8B-4647-BB8A-263B43F0F974}\" AND (System.Devices.Aep.IsPaired:=System.StructuredQueryType.Boolean#True OR System.Devices.Aep.Bluetooth.IssueInquiry:=System.StructuredQueryType.Boolean#False)";

        private readonly string _bluetoothLEDeviceAQS = "System.Devices.DevObjectType:=5 AND System.Devices.Aep.ProtocolId:=\"{BB7BB05E-5972-42B5-94FC-76EAA7084D49}\" AND (System.Devices.Aep.IsPaired:=System.StructuredQueryType.Boolean#True OR System.Devices.Aep.Bluetooth.IssueInquiry:=System.StructuredQueryType.Boolean#False)";
        private const string _deviceAddressRegex = @"Bluetooth#Bluetooth(.*)-(.*)";
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
            //Paired bluetooth devices
            DeviceInformationCollection bluetoothDevicesInfo = await
                   DeviceInformation.FindAllAsync(_bluetoothDeviceAQS);
            DeviceInformationCollection bluetoothLEDevicesInfo = await
                   DeviceInformation.FindAllAsync(_bluetoothDeviceAQS);
            var btRegex = new Regex(_deviceAddressRegex);

            var devices = await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelector());
            foreach (var bluetoothDeviceInfo in bluetoothDevicesInfo)
            {

                var matches = Regex.Match(bluetoothDeviceInfo.Id, _deviceAddressRegex);
                var btAddressString = matches.Groups[2].Value;
                var btAdapterAddressString = matches.Groups[1].Value;
                btAddressString = btAddressString.Replace(":", "");
                var btAddress = Convert.ToUInt64(btAddressString,16);


                var bluetoothDeviceInstance = await BluetoothDevice.FromBluetoothAddressAsync(btAddress);
                bluetoothDevices.Add(new DBBHBluetoothDevice(bluetoothDeviceInfo.Name, bluetoothDeviceInstance.BluetoothAddress));

            }

            return bluetoothDevices;

        }
    }
}
