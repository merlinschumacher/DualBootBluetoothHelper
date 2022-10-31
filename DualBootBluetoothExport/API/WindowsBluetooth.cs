using DualBootBluetoothHelper.Model;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace DualBootBluetoothHelper.API
{
    public class WindowsBluetooth
    {

        private readonly string _bluetoothInterfaceClassGuidAQS = "System.Devices.InterfaceClassGuid:=\"{92383B0E-F90E-4AC9-8D44-8C2D0D0EBDA2}\""
        public WindowsBluetooth()
        {
        }

        public async Task<List<DBBHBluetoothAdapter>> ListBluetoothAdapters()
        {
            DeviceInformationCollection bluetoothAdaptersDeviceInfo = await DeviceInformation.FindAllAsync();

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
            List<DBBHBluetoothDevice> devices = new();
            //Paired bluetooth devices
            DeviceInformationCollection PairedBluetoothDevices = await
                   DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelector());

            foreach (var dev in PairedBluetoothDevices)
            {

                devices.Add(new DBBHBluetoothDevice(dev.Name, null));
            }

            return devices;

        }
    }
}
