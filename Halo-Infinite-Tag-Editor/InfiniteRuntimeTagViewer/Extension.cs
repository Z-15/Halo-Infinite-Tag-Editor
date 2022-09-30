using System.Windows;

namespace InfiniteRuntimeTagViewer
{
    public static class Extension
    {
        public static bool GetBit(this byte b, int bitNumber)
        {
            return (b & (1 << bitNumber)) != 0;
        }

        public static void UpdateBit(ref this byte aByte, int pos, bool value)
        {
            if (value)
            {
                //left-shift 1, then bitwise OR
                aByte = (byte) (aByte | (1 << pos));
            }
            else
            {
                //left-shift 1, then take complement, then bitwise AND
                aByte = (byte) (aByte & ~(1 << pos));
            }
        }
    }
}
