namespace DualBootBluetoothHelper.Helpers
{
    internal static class BtConversionHelper
    {
        /// <summary>Converts a Bluetooth address from string to uLong</summary>
        /// <param name="address">The address.</param>
        /// <returns>ulong of the address</returns>
        public static ulong AddressStringToUlong(string address)
        {
            address = address.Replace(":", "");
            return Convert.ToUInt64(address, 16);
        }

        public static String ConvertToHex(object? obj)
        {
            if (obj == null) return "";
            if (obj.GetType() == typeof(byte[]))
                return BitConverter.ToString((byte[])obj).Replace("-", string.Empty).ToUpperInvariant();
            UInt64 intObj = Convert.ToUInt64(obj);
            return intObj.ToString("X8").ToUpperInvariant();
        }

        /// <summary>Left pads the address</summary>
        /// <param name="address">The address to be padded.</param>
        /// <returns>The address left padded with zeros.</returns>
        public static byte[] LeftPadAddress(byte[] address)
        {
            var paddedAddress = new byte[6];
            var startAt = paddedAddress.Length - address.Length;
            Array.Copy(address, 0, paddedAddress, startAt, address.Length);
            return paddedAddress;
        }

        public static UInt64? ReverseAndConvertToInt64(object? obj)
        {
            if (obj == null) return null;
            UInt64 intObj = (UInt64)(Int64)obj;
            UInt64 rev = ReverseBytes(intObj);
            return rev;
        }

        /// <summary>Converts a ulong to an hexadecimal address string</summary>
        /// <param name="address">The address as ulong.</param>
        /// <returns>The address as a hexadecimal string</returns>
        public static string ULongToString(ulong address)
        {
            return Convert.ToHexString(LeftPadAddress(BitConverter.GetBytes(address).Reverse().SkipWhile(x => x == 0).ToArray()));
        }
        private static UInt64 ReverseBytes(UInt64 value)
        {
            return (value & 0x00000000000000FFUL) << 56 | (value & 0x000000000000FF00UL) << 40 |
                   (value & 0x0000000000FF0000UL) << 24 | (value & 0x00000000FF000000UL) << 8 |
                   (value & 0x000000FF00000000UL) >> 8 | (value & 0x0000FF0000000000UL) >> 24 |
                   (value & 0x00FF000000000000UL) >> 40 | (value & 0xFF00000000000000UL) >> 56;
        }

    }
}