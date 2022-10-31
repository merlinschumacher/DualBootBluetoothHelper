using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace DualBootBluetoothHelper.Model
{
    public class DBBHBluetoothDevice
    {
        public DBBHBluetoothDevice(string name, byte[] address)
        {
            Name = name;
            Address = address;
        }

        public string Name { get; set; }

        public byte[] Address { get; set; }

        public override string ToString()
        {

            return Name + " - " + Convert.ToHexString(Address);
        }
    }
}
