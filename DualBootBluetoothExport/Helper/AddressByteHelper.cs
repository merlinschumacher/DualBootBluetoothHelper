namespace DualBootBluetoothHelper.Helper
{
    internal static class AddressByteHelper
    {
        public static byte[] LeftPadAddress(byte[] address)
        {
            var paddedAddress = new byte[6];
            var startAt = paddedAddress.Length - address.Length;
            Array.Copy(address, 0, paddedAddress, startAt, address.Length);
            return paddedAddress;
        }
    }
}