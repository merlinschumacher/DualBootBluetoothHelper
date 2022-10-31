using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace DualBootBluetoothHelper.Model
{
    public class DBBHBluetoothAdapter
    {
        public DBBHBluetoothAdapter(string name, byte[] address)
        {
            Name = name;
            Address = address;
        }
        public DBBHBluetoothAdapter(string name, ulong address)
        {
            Name = name;
            Address = BitConverter.GetBytes(address).Reverse().SkipWhile(x => x == 0).ToArray();
        }

        public string Name { get; set; } = "";
        public byte[] Address { get; set; }
        public List<DBBHBluetoothDevice> Devices { get; set; } = new();

        public override string ToString()
        {

            return Name + " - " + Convert.ToHexString(Address);
        }

    }
}
