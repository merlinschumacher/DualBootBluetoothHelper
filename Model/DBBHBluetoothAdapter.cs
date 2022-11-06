using DualBootBluetoothHelper.Helper;

namespace DualBootBluetoothHelper.Model
{
    public class DBBHBluetoothAdapter
    {
        public DBBHBluetoothAdapter(string name, string address)
        {
            Name = name;
            Address = address; 
        }

        public DBBHBluetoothAdapter(string name, ulong address)
        {
            Name = name;
            Address = AddressByteHelper.ulongToString(address);
        }

        public string Name { get; set; } = "";
        public string Address { get; set; }
        public List<DBBHBluetoothDevice> Devices { get; set; } = new();

        public override string ToString()
        {
            return $"{Name} ({Address})";
        }
    }
}