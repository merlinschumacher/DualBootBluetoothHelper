using DualBootBluetoothHelper.Helper;

namespace DualBootBluetoothHelper.Model
{
    /// <summary>A Bluetooth device.</summary>
    public class DbbhBluetoothDevice
    {
        private string _adapterAddress = "";

        private string _address = "";

        /// <summary>Initializes a new instance of the <see cref="DbbhBluetoothDevice" /> class.</summary>
        /// <param name="name">The name of the device.</param>
        /// <param name="address">The bt address of the device.</param>
        /// <param name="adapterAddress">The adapters address to which the device is connected.</param>
        public DbbhBluetoothDevice(string name, string address, string adapterAddress)
        {
            Name = name;
            Address = address;
            AdapterAddress = adapterAddress;
        }

        /// <summary>Initializes a new instance of the <see cref="DbbhBluetoothDevice" /> class.</summary>
        /// <param name="name">The name of the device.</param>
        /// <param name="address">The bt address of the device.</param>
        /// <param name="adapterAddress">The adapters address to which the device is connected.</param>
        public DbbhBluetoothDevice(string name, ulong address, ulong adapterAddress)
        {
            Name = name;
            Address = AddressByteHelper.ulongToString(address);
            AdapterAddress = AddressByteHelper.ulongToString(adapterAddress);
        }

        /// <summary>Gets or sets the name of the device</summary>
        /// <value>The name as a string</value>
        public string Name { get; set; }
        /// <summary>Gets or sets the adapters address to which the device is connected.</summary>
        /// <value>The adapter address.</value>
        public string AdapterAddress { get => _adapterAddress; set => _adapterAddress = value.ToUpperInvariant(); }
        /// <summary>Gets or sets the devices address.</summary>
        /// <value>The devices address.</value>
        public string Address { get => _address; set => _address = value.ToUpperInvariant(); }
        /// <summary>Gets or sets the link key. The key is used to encrypt connections to older Bluetooth devices.</summary>
        /// <value>The link key.</value>
        public string LinkKey { get; set; } = "";
        public string EDIV { get; set; } = "";
        public string IRK { get; set; } = "";
        public string LTK { get; set; } = "";
        public UInt64? Rand { get; set; }
        public override string ToString()
        {
            return $"Address: {Address} - Adapter address: {AdapterAddress} - Name {Name}";
        }
    }
}