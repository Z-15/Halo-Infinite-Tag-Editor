using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InfiniteRuntimeTagViewer;

namespace ZTools
{
    public class ZCommon
    {
        public static Dictionary<string, string> tagGroups = new Dictionary<string, string>();
        public static Dictionary<string, TagInfo> inhaledTags = new Dictionary<string, TagInfo>();

        public class TagInfo
        {
            public string TagID = "";
            public string AssetID = "";
            public string TagPath = "";
            public string ModulePath = "";
        }

        public static TagInfo GetTagInfo(string tagID)
        {
            TagInfo result = new TagInfo();

            if (inhaledTags.ContainsKey(tagID))
                result = inhaledTags[tagID];
            else
            {
                result.TagID = tagID;
                result.AssetID = "Unknown";
                result.TagPath = "Unknown";
            }

            return result;
        }

        public static byte[] GetDataFromByteArray(int size, long offset, byte[] data)
        {
            byte[] result = data.Skip((int)offset).Take(((int)offset + size) - (int)offset).ToArray();
            return result;
        }

        public static Dictionary<string, bool> GetFlagsFromBits(int amountOfBytes, int maxBit, byte[] data, Dictionary<int, string>? descriptions = null)
        {
            Dictionary<string, bool> values = new();

            if (maxBit == 0)
            {
                maxBit = maxBit = amountOfBytes * 8;
            }

            int maxAmountOfBytes = Math.Clamp((int)Math.Ceiling((double)maxBit / 8), 0, amountOfBytes);
            int bitsLeft = maxBit - 1; // -1 to start at 

            for (int @byte = 0; @byte < maxAmountOfBytes; @byte++)
            {
                if (bitsLeft < 0)
                {
                    continue;
                }

                int amountOfBits = @byte * 8 > maxBit ? ((@byte * 8) - maxBit) : 8;
                byte flags_value = (byte)data[@byte];

                for (int bit = 0; bit < amountOfBits; bit++)
                {
                    int currentBitIndex = (@byte * 8) + bit;
                    if (bitsLeft < 0)
                    {
                        continue;
                    }

                    int _byte = @byte, _bit = bit;

                    if (descriptions != null && descriptions.ContainsKey(currentBitIndex))
                    {
                        bool value = flags_value.GetBit(bit);
                        string description = descriptions[(@byte * 8) + bit];
                        values.Add(description, value);
                    }
                    bitsLeft--;
                }
            }

            return values;
        }

        public static string ReverseString(string myStr)
        {
            char[] myArr = myStr.ToCharArray();
            Array.Reverse(myArr);
            return new string(myArr);
        }
    }
}
