namespace DualBootBluetoothHelper.Helper
{
    internal static class AddressByteHelper
    {
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

        /// <summary>Converts a ulong to an hexadecimal address string</summary>
        /// <param name="address">The address as ulong.</param>
        /// <returns>The address as a hexadecimal string</returns>
        public static string ulongToString(ulong address)
        {
            return Convert.ToHexString(LeftPadAddress(BitConverter.GetBytes(address).Reverse().SkipWhile(x => x == 0).ToArray()));
        }
    }
}