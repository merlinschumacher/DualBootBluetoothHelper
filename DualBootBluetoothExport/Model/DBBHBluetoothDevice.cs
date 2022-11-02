using DualBootBluetoothHelper.Helper;

namespace DualBootBluetoothHelper.Model
{
    public class DBBHBluetoothDevice
    {
        public enum DBBHBluetoothDeviceType
        {
            Classic,
            FivePointOne
        }

        public DBBHBluetoothDevice(string name, byte[] address, byte[] adapterAddress)
        {
            Name = name;
            Address = AddressByteHelper.LeftPadAddress(address);
            AdapterAddress = AddressByteHelper.LeftPadAddress(adapterAddress);
        }

        public DBBHBluetoothDevice(string name, ulong address, ulong adapterAddress)
        {
            Name = name;
            Address = AddressByteHelper.LeftPadAddress(BitConverter.GetBytes(address).Reverse().SkipWhile(x => x == 0).ToArray());
            AdapterAddress = AddressByteHelper.LeftPadAddress(BitConverter.GetBytes(adapterAddress).Reverse().SkipWhile(x => x == 0).ToArray());
        }

        public string Name { get; set; }

        public byte[] Address { get; set; }
        public byte[] AdapterAddress { get; set; }
        public DBBHBluetoothDeviceType DeviceType { get; set; }
        public string Key { get; set; } = "";
        public string IRK { get; set; } = "";
        public string LTK { get; set; } = "";
        public UInt64 Rand { get; set; }
        public string EDIV { get; set; } = "";


        public override string ToString()
        {
            return $"Address: {Convert.ToHexString(Address)} - Adapter address: {Convert.ToHexString(AdapterAddress)} - Name {Name}";
        }
    }
}