using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using InfiniteModuleEditor;
using InfiniteRuntimeTagViewer.Halo.TagObjects;
using Microsoft.Win32;
using Newtonsoft.Json;
using static ZTools.ZCommon;

namespace ZTools
{
    public class JsonExport
    {
        class TagJsonData
        {
            public string TagID = "";
            public string TagName = "";
            public string DataOffset = "";
            public List<object> Data = new();
        }

        class BasicValue
        {
            public string Name = "";
            public string Type = "";
            public string Offset = "";
            public long Size = 0;
            public string Bytes = "";
            public object Value = new();
        }

        class Bounds
        {
            public object Min = 0;
            public object Max = 0;
        }

        class RGBValue
        {
            public RGBValue(float r, float g, float b)
            {
                R = r;
                G = g;
                B = b;
                HexColor = "#" + ((byte)(r * 255)).ToString("X2") + ((byte)(g * 255)).ToString("X2") + ((byte)(b * 255)).ToString("X2");
            }

            public string HexColor = "";
            public float R = 0;
            public float G = 0;
            public float B = 0;
        }

        class ARGBValue
        {
            public ARGBValue(float a, float r, float g, float b)
            {
                A = a;
                R = r;
                G = g;
                B = b;
                HexColor = "#" + ((byte)(a * 255)).ToString("X2") + ((byte)(r * 255)).ToString("X2") + ((byte)(g * 255)).ToString("X2") + ((byte)(b * 255)).ToString("X2");
            }

            public string HexColor = "";
            public float A = 0;
            public float R = 0;
            public float G = 0;
            public float B = 0;
        }

        class TagBlockValue
        {
            public int Count = 0;
            public int BlockIndex = 0;
            public string Address = "";
            public Dictionary<string, object> Data = new();
        }

        class TagRefValue
        {
            public string TagID = "";
            public string AssetID = "";
            public string TagGroup = "";
            public string TagPath = "";
            public string ModulePath = "";
        }

        class EnumValue
        {
            public int RawValue = 0;
            public string Selection = "";
            public Dictionary<int, string> Options = new();
        }

        class FlagValue
        {
            public int RawValue = 0;
            public Dictionary<string, bool> Flags = new();
        }

        private static ModuleFile mf = new();
        private static Dictionary<long, TagLayouts.C> tagDef = new();
        private static byte[] data = new byte[0];
        private static int curDataBlockInd = 1;

        public static string? ExportTagToJson(Dictionary<long, TagLayouts.C> tagLayout, TagInfo tag, ModuleFile? moduleFile)
        {
            if (moduleFile != null)
            {
                try
                {
                    // Set Values
                    mf = moduleFile;
                    tagDef = tagLayout;
                    data = mf.Tag.TagData;
                    curDataBlockInd = 1;

                    // Create Json Data Class and Get Data
                    TagJsonData tjd = new();
                    tjd.TagID = tag.TagID;
                    tjd.TagName = tag.TagPath.Replace("\0", "");
                    tjd.DataOffset = "0x" + mf.Tag.TrueDataOffset.ToString("X");
                    tjd.Data = ReadTagStructure(tagDef);

                    return JsonConvert.SerializeObject(tjd, Formatting.Indented).ToString();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            return null;
        }

        private static List<object> ReadTagStructure(Dictionary<long, TagLayouts.C> tagLayout, long address = 0)
        {
            List<object> Data = new();

            foreach (KeyValuePair<long, TagLayouts.C> entry in tagLayout)
            {
                entry.Value.MemoryAddress = address + entry.Key;

                string name = "";
                string type = "";

                if (entry.Value.N != null)
                    name = entry.Value.N;

                if (entry.Value.T != null)
                    type = entry.Value.T;

                if (!name.Contains("generated_pad"))
                {
                    try
                    {
                        if (type == "Comment")
                        {
                            Data.Add(name);
                        }
                        else
                        {
                            byte[] rawValue = GetDataFromByteArray((int)entry.Value.S, entry.Value.MemoryAddress, data);
                            string bytes = "";
                            foreach (byte b in rawValue)
                                bytes += b.ToString("X2") + " ";

                            BasicValue v = new()
                            {
                                Name = name,
                                Type = type,
                                Offset = "0x" + entry.Value.MemoryAddress.ToString("X"),
                                Size = entry.Value.S,
                                Bytes = bytes.Trim(),
                                Value = "Null"
                            };

                            switch (type)
                            {
                                case "Byte":
                                    v.Value = rawValue[0];
                                    break;
                                case "2Byte":
                                    v.Value = BitConverter.ToInt16(rawValue);
                                    break;
                                case "4Byte":
                                    v.Value = BitConverter.ToInt32(rawValue);
                                    break;
                                case "Float":
                                    v.Value = BitConverter.ToSingle(rawValue);
                                    break;
                                case "Pointer":
                                    v.Value = "0x" + BitConverter.ToUInt64(rawValue).ToString("X");
                                    break;
                                case "mmr3Hash":
                                    v.Value = BitConverter.ToUInt32(rawValue).ToString("X");
                                    break;
                                case "String":
                                    v.Value = Encoding.UTF8.GetString(rawValue).Split('\0').First();
                                    break;
                                case "EnumGroup":
                                    TagLayouts.EnumGroup fg3 = (TagLayouts.EnumGroup)entry.Value;
                                    v.Name = fg3.N;
                                    EnumValue eVal = new();
                                    eVal.Options = fg3.STR;

                                    if (entry.Value.S == 1)
                                        eVal.RawValue = rawValue[0];
                                    else if (entry.Value.S == 2)
                                        eVal.RawValue = BitConverter.ToInt16(rawValue);
                                    else if (entry.Value.S == 4)
                                        eVal.RawValue = BitConverter.ToInt32(rawValue);

                                    if (eVal.Options.ContainsKey(eVal.RawValue))
                                        eVal.Selection = eVal.Options[eVal.RawValue];
                                    else
                                        eVal.Selection = "Error";

                                    v.Value = eVal;
                                    break;
                                case "FlagGroup":
                                    TagLayouts.FlagGroup? fg = (TagLayouts.FlagGroup)entry.Value;
                                    v.Name = fg.N;
                                    FlagValue fVal = new();

                                    if (entry.Value.S == 1)
                                        fVal.RawValue = rawValue[0];
                                    else if (entry.Value.S == 2)
                                        fVal.RawValue = BitConverter.ToInt16(rawValue);
                                    else if (entry.Value.S == 4)
                                        fVal.RawValue = BitConverter.ToInt32(rawValue);

                                    fVal.Flags = GetFlagsFromBits(fg.A, fg.MB, GetDataFromByteArray(fg.A, entry.Value.MemoryAddress, data), fg.STR);
                                    v.Value = fVal;
                                    break;
                                case "BoundsFloat":
                                    v.Value = new Bounds()
                                    {
                                        Min = BitConverter.ToSingle(GetDataFromByteArray(8, entry.Value.MemoryAddress, data), 0),
                                        Max = BitConverter.ToSingle(GetDataFromByteArray(8, entry.Value.MemoryAddress, data), 4)
                                    };
                                    break;
                                case "Bounds2Byte":
                                    v.Value = new Bounds()
                                    {
                                        Min = BitConverter.ToSingle(GetDataFromByteArray(8, entry.Value.MemoryAddress, data), 0),
                                        Max = BitConverter.ToSingle(GetDataFromByteArray(8, entry.Value.MemoryAddress, data), 4)
                                    };
                                    break;
                                case "2DPoint_Float":
                                    v.Value = new Vector2()
                                    {
                                        X = BitConverter.ToSingle(GetDataFromByteArray(8, entry.Value.MemoryAddress, data), 0),
                                        Y = BitConverter.ToSingle(GetDataFromByteArray(8, entry.Value.MemoryAddress, data), 4)
                                    };
                                    break;
                                case "2DPoint_2Byte":
                                    v.Value = new Vector2()
                                    {
                                        X = BitConverter.ToInt16(GetDataFromByteArray(4, entry.Value.MemoryAddress, data), 0),
                                        Y = BitConverter.ToInt16(GetDataFromByteArray(4, entry.Value.MemoryAddress, data), 2)
                                    };
                                    break;
                                case "3DPoint":
                                    v.Value = new Vector3()
                                    {
                                        X = BitConverter.ToSingle(GetDataFromByteArray(12, entry.Value.MemoryAddress, data), 0),
                                        Y = BitConverter.ToSingle(GetDataFromByteArray(12, entry.Value.MemoryAddress, data), 4),
                                        Z = BitConverter.ToSingle(GetDataFromByteArray(12, entry.Value.MemoryAddress, data), 8)
                                    };
                                    break;
                                case "Quanternion":
                                    v.Value = new Quaternion()
                                    {
                                        W = BitConverter.ToSingle(GetDataFromByteArray(16, entry.Value.MemoryAddress, data), 0),
                                        X = BitConverter.ToSingle(GetDataFromByteArray(16, entry.Value.MemoryAddress, data), 4),
                                        Y = BitConverter.ToSingle(GetDataFromByteArray(16, entry.Value.MemoryAddress, data), 8),
                                        Z = BitConverter.ToSingle(GetDataFromByteArray(16, entry.Value.MemoryAddress, data), 12)
                                    };
                                    break;
                                case "3DPlane":
                                    v.Value = new Quaternion()
                                    {
                                        W = BitConverter.ToSingle(GetDataFromByteArray(16, entry.Value.MemoryAddress, data), 0),
                                        X = BitConverter.ToSingle(GetDataFromByteArray(16, entry.Value.MemoryAddress, data), 4),
                                        Y = BitConverter.ToSingle(GetDataFromByteArray(16, entry.Value.MemoryAddress, data), 8),
                                        Z = BitConverter.ToSingle(GetDataFromByteArray(16, entry.Value.MemoryAddress, data), 12)
                                    };
                                    break;
                                case "RGB":
                                    v.Value = new RGBValue(BitConverter.ToSingle(GetDataFromByteArray(12, entry.Value.MemoryAddress, data), 0),
                                        BitConverter.ToSingle(GetDataFromByteArray(12, entry.Value.MemoryAddress, data), 4),
                                        BitConverter.ToSingle(GetDataFromByteArray(12, entry.Value.MemoryAddress, data), 8));
                                    break;
                                case "ARGB":
                                    v.Value = new ARGBValue(BitConverter.ToSingle(GetDataFromByteArray(16, entry.Value.MemoryAddress, data), 0),
                                        BitConverter.ToSingle(GetDataFromByteArray(16, entry.Value.MemoryAddress, data), 4),
                                        BitConverter.ToSingle(GetDataFromByteArray(16, entry.Value.MemoryAddress, data), 8),
                                        BitConverter.ToSingle(GetDataFromByteArray(16, entry.Value.MemoryAddress, data), 12));
                                    break;
                                case "TagRef":
                                    string tagId = Convert.ToHexString(GetDataFromByteArray(4, entry.Value.MemoryAddress + 8, data));
                                    byte[] tagGroupData = GetDataFromByteArray(4, entry.Value.MemoryAddress + 20, data);
                                    string tagGroup = Convert.ToHexString(tagGroupData);
                                    if (tagGroup != "FFFFFFFF")
                                        tagGroup = ReverseString(Encoding.UTF8.GetString(tagGroupData));

                                    if (tagId != "FFFFFFFF" && tagGroup != "FFFFFFFF")
                                    {
                                        TagRefValue value = new();
                                        value.TagID = tagId;
                                        value.TagGroup = tagGroup;

                                        TagInfo tag = GetTagInfo(tagId);
                                        value.AssetID = tag.AssetID;
                                        value.TagPath = tag.TagPath;
                                        value.ModulePath = tag.ModulePath;

                                        v.Value = value;
                                    }
                                    break;
                                case "Tagblock":
                                    int childCount = BitConverter.ToInt32(GetDataFromByteArray(20, entry.Value.MemoryAddress, data), 16);

                                    TagBlockValue tbv = new();
                                    if (childCount > 0 && childCount < 100000)
                                    {
                                        tbv.Count = childCount;
                                        tbv.BlockIndex = curDataBlockInd;
                                        tbv.Address = "0x" + mf.Tag.DataBlockArray[tbv.BlockIndex].Offset.ToString("X");
                                        curDataBlockInd++;
                                        for (int i = 0; i < childCount; i++)
                                        {
                                            long newAddress = (long)mf.Tag.DataBlockArray[tbv.BlockIndex].Offset - (long)mf.Tag.DataBlockArray[0].Offset + (entry.Value.S * i);
                                            tbv.Data.Add("Index " + (i + 1), ReadTagStructure(entry.Value.B, newAddress));
                                        }
                                        v.Value = tbv;
                                    }

                                    break;
                                case "FUNCTION":
                                    int funcSize = BitConverter.ToInt32(GetDataFromByteArray(4, entry.Value.MemoryAddress + 20, data));
                                    TagBlockValue funcV = new();
                                    if (funcSize > 0)
                                    {
                                        funcV.Count = funcSize;
                                        funcV.BlockIndex = curDataBlockInd;
                                        curDataBlockInd++;

                                        v.Value = funcV;
                                    }
                                    break;
                            }

                            Data.Add(v);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
            }
            return Data;
        }
    }
}
