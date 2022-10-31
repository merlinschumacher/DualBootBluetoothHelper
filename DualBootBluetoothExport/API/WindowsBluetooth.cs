using DualBootBluetoothHelper.Model;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace DualBootBluetoothHelper.API
{
    public class WindowsBluetooth
    {

        private readonly string _bluetoothInterfaceClassGuidAQS = "System.Devices.InterfaceClassGuid:=\"{92383B0E-F90E-4AC9-8D44-8C2D0D0EBDA2}\"";
        public WindowsBluetooth()
        {
        }

        public async Task<List<DBBHBluetoothAdapter>> ListBluetoothAdapters()
        {
            DeviceInformationCollection bluetoothAdaptersDeviceInfo = await DeviceInformation.FindAllAsync(_bluetoothInterfaceClassGuidAQS);

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
                   DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelector());

            foreach (var bluetoothDeviceInfo in bluetoothDevicesInfo)
            {
                var bluetoothDeviceInstance = await BluetoothDevice.FromIdAsync(bluetoothDeviceInfo.Id);
                bluetoothDevices.Add(new DBBHBluetoothDevice(bluetoothDeviceInfo.Name, bluetoothDeviceInstance.BluetoothAddress));

            }

            return bluetoothDevices;

        }
    }
}
