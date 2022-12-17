using System;
using System.Collections.Generic;
using System.Text;

namespace HavokScriptToolsCommon
{
    class Util
    {
        public static bool ArraysEqual<T>(T[] arr1, T[] arr2)
        {
            if (arr1.Length != arr2.Length)
            {
                return false;
            }

            for (int i = 0; i < arr1.Length; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(arr1[i], arr2[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static void SetBit(ref byte value, int index, bool on)
        {
            if (on)
            {
                value |= (byte)(1 << index);
            }
            else
            {
                value &= (byte)~(1 << index);
            }
        }

        public static bool GetBit(byte value, int index)
        {
            return (value & (1 << index)) != 0;
        }
    }

    public class Box<T>
    {
        public T V { get; }
        public Box(T value)
        {
            V = value;
        }
    }
}
