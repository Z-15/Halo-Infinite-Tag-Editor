using InfiniteModuleEditor;
using InfiniteRuntimeTagViewer.Halo.TagObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ZTools.ZCommon;

namespace ZTools
{
    public class HashExport
    {
        class HashTagInfo
        {
            public string TagID = "";
            public Dictionary<long, string> ReferenceLocations = new();
        }

        class HashInfo
        {
            public string HashID = "";
            public string HashName = "";
            public Dictionary<string, HashTagInfo> Tags = new();
        }

        private static Dictionary<string, HashInfo> foundHashes = new();
        private static Dictionary<string, string> hashNames = new();
        private static int curDataBlockInd = 1;
        private static byte[] data = new byte[0];

        public static List<string> DumpHashes(List<string> modulePaths)
        {
            foreach (string line in File.ReadLines(@".\Files\all_trimmed.txt"))
            {
                string trim = line.Trim();
                if (trim.Contains(":"))
                    if (!hashNames.ContainsKey(trim.Split(":").First()))
                        hashNames.Add(trim.Split(":").First(), trim.Split(":").Last());
            }

            foreach (string line in File.ReadLines(@".\Files\nocaps_3chars.txt"))
            {
                string trim = line.Trim();
                if (trim.Contains(":"))
                    if (!hashNames.ContainsKey(trim.Split(":").First()))
                        hashNames.Add(trim.Split(":").First(), trim.Split(":").Last());
            }

            foreach (string path in modulePaths)
            {
                FileStream mStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                Module m = ModuleEditor.ReadModule(mStream);

                foreach (KeyValuePair<string, ModuleFile> mf in m.ModuleFiles)
                {
                    try
                    {
                        string TagPath = mf.Key.Replace("\0", String.Empty);

                        if (!TagPath.EndsWith(".model_animation_graph") && !TagPath.EndsWith(".physics_model") && !TagPath.EndsWith(".decal_system") && !TagPath.EndsWith(".generator_system") && !TagPath.EndsWith(".composer_spawning_pattern"))
                        {
                            MemoryStream tStream = new();
                            tStream = ModuleEditor.GetTag(m, mStream, TagPath);
                            mf.Value.Tag = ModuleEditor.ReadTag(tStream, TagPath, mf.Value);
                            mf.Value.Tag.Name = TagPath;
                            mf.Value.Tag.ShortName = TagPath.Split("\\").Last();

                            string curTagGroup = mf.Key.Replace("\0", String.Empty).Split(".").Last();
                            string tagID = Convert.ToHexString(BitConverter.GetBytes(mf.Value.FileEntry.GlobalTagId));

                            if (tagGroups.ContainsKey(curTagGroup.Trim()))
                            {
                                Dictionary<long, TagLayouts.C> tagDefinitions = TagLayouts.Tags(tagGroups[curTagGroup]);
                                data = mf.Value.Tag.TagData;
                                ReadTagStructure(tagDefinitions, mf.Value);
                            }

                            tStream.Close();
                            curDataBlockInd = 1;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Dump Hashes Task: " + ex.Message);
                    }
                }
                mStream.Close();
            }

            return CreateLines();
        }

        private static void ReadTagStructure(Dictionary<long, TagLayouts.C> tagLayout, ModuleFile mf, long address = 0)
        {
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
                        byte[] rawValue = GetDataFromByteArray((int)entry.Value.S, entry.Value.MemoryAddress, data);

                        switch (type)
                        {
                            case "mmr3Hash":
                                string hash = BitConverter.ToUInt32(rawValue).ToString("X");
                                if (hash != "BCBCBCBC" && hash != "00000000" && !String.IsNullOrWhiteSpace(hash) && !String.IsNullOrEmpty(hash))
                                {
                                    string tagID = mf.FileEntry.GlobalTagId.ToString("X");
                                    if (!foundHashes.ContainsKey(hash))
                                    {
                                        HashInfo hashInfo = new();
                                        hashInfo.HashID = hash;

                                        if (hashNames.ContainsKey(hash))
                                            hashInfo.HashName = hashNames[hash];
                                        else
                                            hashInfo.HashName = "Unknown";

                                        HashTagInfo hashTagInfo = new();
                                        hashTagInfo.TagID = tagID;
                                        if (!hashTagInfo.ReferenceLocations.ContainsKey(entry.Value.MemoryAddress))
                                            hashTagInfo.ReferenceLocations.Add(entry.Value.MemoryAddress, name);

                                        hashInfo.Tags.Add(hashTagInfo.TagID, hashTagInfo);
                                        foundHashes.Add(hash, hashInfo);
                                    }
                                    else
                                    {
                                        HashInfo hashInfo = foundHashes[hash];

                                        if (!hashInfo.Tags.ContainsKey(tagID))
                                        {
                                            HashTagInfo hashTagInfo = new();
                                            hashTagInfo.TagID = tagID;
                                            if (!hashTagInfo.ReferenceLocations.ContainsKey(entry.Value.MemoryAddress))
                                                hashTagInfo.ReferenceLocations.Add(entry.Value.MemoryAddress, name);
                                            hashInfo.Tags.Add(hashTagInfo.TagID, hashTagInfo);
                                        }
                                        else
                                        {
                                            HashTagInfo hashTagInfo = hashInfo.Tags[tagID];
                                            if (!hashTagInfo.ReferenceLocations.ContainsKey(entry.Value.MemoryAddress))
                                                hashTagInfo.ReferenceLocations.Add(entry.Value.MemoryAddress, name);
                                        }
                                    }
                                }
                                break;
                            case "Tagblock":
                                int childCount = BitConverter.ToInt32(GetDataFromByteArray(20, entry.Value.MemoryAddress, data), 16);
                                int blockInd = 0;
                                if (childCount > 0 && childCount < 100000 && entry.Value.N != "material constants")
                                {
                                    blockInd = curDataBlockInd;
                                    curDataBlockInd++;
                                    for (int i = 0; i < childCount; i++)
                                    {
                                        long newAddress = (long)mf.Tag.DataBlockArray[blockInd].Offset - (long)mf.Tag.DataBlockArray[0].Offset + (entry.Value.S * i);
                                        ReadTagStructure(entry.Value.B, mf, newAddress);
                                    }
                                }
                                break;
                            case "FUNCTION":
                                int funcSize = BitConverter.ToInt32(GetDataFromByteArray(4, entry.Value.MemoryAddress + 20, data));
                                if (funcSize > 0)
                                {
                                    curDataBlockInd++;
                                }
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
            }
        }

        private static List<string> CreateLines()
        {
            List<string> lines = new();

            foreach (HashInfo hashInfo in foundHashes.Values)
            {
                string outLine = "";
                outLine = "Hash-" + hashInfo.HashID + "~Name-" + hashInfo.HashName + "~Tags{";
                int i = 0;
                foreach (HashTagInfo hashTagInfo in hashInfo.Tags.Values)
                {
                    outLine += hashTagInfo.TagID + "[";
                    int j = 0;
                    foreach (KeyValuePair<long, string> kvp in hashTagInfo.ReferenceLocations)
                    {
                        outLine += kvp.Value + ":" + kvp.Key.ToString("X");
                        if (j < hashTagInfo.ReferenceLocations.Count - 1)
                            outLine += ",";

                        j++;
                    }
                    outLine += "]";

                    if (i < hashInfo.Tags.Count - 1)
                        outLine += ",";

                    i++;
                }
                outLine += "}";

                lines.Add(outLine);
            }
            return lines;
        }
    }
}
