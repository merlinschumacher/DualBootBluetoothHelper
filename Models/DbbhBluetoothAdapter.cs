using DualBootBluetoothHelper.Helpers;

namespace DualBootBluetoothHelper.Models
{
    /// <summary>A Bluetooth adapter</summary>
    public class DbbhBluetoothAdapter
    {
        /// <summary>Initializes a new instance of the <see cref="DbbhBluetoothAdapter" /> class.</summary>
        /// <param name="name">The name of the adapter.</param>
        /// <param name="address">The address of the adapter.</param>
        public DbbhBluetoothAdapter(string name, string address)
        {
            Name = name;
            Address = address;
        }

        /// <summary>Initializes a new instance of the <see cref="DbbhBluetoothAdapter" /> class.</summary>
        /// <param name="name">The name of the adapter.</param>
        /// <param name="address">The address of the adapter.</param>
        public DbbhBluetoothAdapter(string name, ulong address)
        {
            Name = name;
            Address = BtConversionHelper.ULongToString(address);
        }

        /// <summary>The user friendly name of the adapter.</summary>
        public string Name { get; set; }
        /// <summary>The address of the adapter.</summary>
        public string Address { get; set; }
        /// <summary>The list of devices associated with the adapter.</summary>
        public List<DbbhBluetoothDevice> Devices { get; set; } = new();
        public override string ToString()
        {
            return $"{Name} ({Address})";
        }
    }
}