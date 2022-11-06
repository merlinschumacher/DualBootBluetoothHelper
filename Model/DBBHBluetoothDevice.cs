using DualBootBluetoothHelper.Helper;

namespace DualBootBluetoothHelper.Model
{
    public class DBBHBluetoothDevice
    {
        public DBBHBluetoothDevice(string name, string address, string adapterAddress)
        {
            Name = name;
            Address = address;
            AdapterAddress = adapterAddress;
        }

        public DBBHBluetoothDevice(string name, ulong address, ulong adapterAddress)
        {
            Name = name;
            Address = AddressByteHelper.ulongToString(address);
            AdapterAddress = AddressByteHelper.ulongToString(adapterAddress);
        }

        public string Name { get; set; }

        private string _address = "";
        public string Address { get => _address; set => _address = value.ToUpperInvariant(); }
        private string _adapterAddress = "";
        public string AdapterAddress { get => _adapterAddress; set => _adapterAddress = value.ToUpperInvariant(); }
        public string LinkKey { get; set; } = "";
        public string IRK { get; set; } = "";
        public string LTK { get; set; } = "";
        public UInt64? Rand { get; set; }
        public string EDIV { get; set; } = "";


        public override string ToString()
        {
            return $"Address: {Address} - Adapter address: {AdapterAddress} - Name {Name}";
        }
    }
}