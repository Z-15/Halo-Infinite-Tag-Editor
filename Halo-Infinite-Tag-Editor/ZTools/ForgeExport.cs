using InfiniteModuleEditor;
using InfiniteRuntimeTagViewer.Halo.TagObjects;
using Newtonsoft.Json;
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
    public class ForgeExport
    {
        class ForgeData
        {
            public string ManifestTag = "";
            public Dictionary<int, ForgeObjectData> Objects = new(); 
        }

        class ForgeObjectData
        {
            public string TagID = "";
            public string TagName = "";
            public string EntryName = "";
            public string EntryDescription = "";
            public string FolderID = "";
            public FolderInfo Folder = new();
        }

        class FolderInfo
        {
            public int CategoryIndex = 0;
            public string Title = "";
            public string Decription = "";
            public string CategoryID = "";
            public string CategoryName = "";
            public string ParentID = "";
            public string ParentName = "";
        }

        private static Dictionary<string, string> folderNames = new()
        {
            // ROOT FOLDERS
            { "3ACEAA9D", "recents" },
            { "751DF3FA", "prefabs" },
            { "EA3223EA", "accents" },
            { "9D24B701", "biomes" },
            { "957003CB", "blockers" },
            { "7416759B", "decals" },
            { "D45632B1", "fx" },
            { "58EAD6EF", "gameplay" },
            { "98D3445F", "halo design set" },
            { "F1CC100D", "lights" },
            { "EF548B2A", "primitives" },
            { "805A8ACF", "props" },
            { "AB5C78A5", "structures" },
            { "920A82B9", "Z_Null" },
            { "45E9333E", "Z_Unused" },
            { "F773D4F6", "kits" },
            // ACCENTS
            { "FCB0B577", "antennas" },
            { "D3E15E7E", "antennas mp" },
            { "051C51D3", "arena" },
            { "C83A0371", "barrels" },
            { "ACD5A535", "barrels mp" },
            { "BA860FA1", "bazaar" },
            { "A62122D8", "bazaar mp" },
            { "600A76CA", "bodies" },
            { "94C44886", "city props" },
            { "4A174B06", "city props mp" },
            { "48DF3BB5", "cover" },
            { "3C5C221E", "cover mp" },
            { "B8E84C1E", "crates" },
            { "4215C711", "crates mp" },
            { "094B5F3D", "destructibles" },
            { "2730E2EF", "destructibles mp" },
            { "374F00F1", "ducts" },
            { "F32489F1", "fences" },
            { "6C5E87BF", "fences mp" },
            { "B62245A9", "forerunner" },
            { "E908822C", "forerunner mp" },
            { "228BE1A4", "garbage" },
            { "9E83989D", "garbage mp" },
            { "77B82060", "glass" },
            { "56BA23BA", "missiles" },
            { "74D2A795", "missiles mp" },
            { "D7477F6A", "panels" },
            { "BDD4227F", "pipes" },
            { "69769AD1", "railings" },
            { "D1CD6834", "railings mp" },
            { "127F0160", "rubble" },
            { "300A7AB4", "sandbags" },
            { "A7DAD52E", "sandbags mp" },
            { "17864DE1", "signs" },
            { "4588194B", "supports" },
            { "070BAEA1", "tools mp" },
            { "935B215C", "unsc" },
            { "67927F10", "unsc mp" },
            { "FFC5EE8E", "vehicles" },
            { "53833E8C", "vehicles mp" },
            { "78F032BC", "wires" },
            { "2C664138", "wires mp" },
            { "06DFFE4B", "workstations" },
            { "F3F5375C", "workstations mp" },
            // BIOMES
            { "59D2C375", "bushes" },
            { "F7174DF2", "flora" },
            { "7E640351", "rocks - alpine" },
            { "F7D02A88", "rocks - burnt forest" },
            { "D16E4F08", "rocks - desert" },
            { "637D015B", "rocks - glacier" },
            { "ACE14020", "rocks - misc" },
            { "ADC73C23", "rocks - space" },
            { "32E488A5", "rocks - tidal" },
            { "A40F97C9", "rocks - wetlands" },
            { "3B167CF9", "stumps" },
            { "94C6B4EB", "terrain" },
            { "E857DAFE", "trees" },
            { "84A89220", "trees - logs" },
            { "5D98AB89", "trees - roots" },
            // BLOCKERS
            { "48FD8875", "one way blockers" },
            { "57FD7FFC", "player blockers" },
            { "0CAD6ADA", "projectile blockers" },
            { "94A72FF7", "team blockers" },
            { "DD740B6E", "vehicle blockers" },
            // DECALS
            { "68B715EC", "building signage" },
            { "3F6697AB", "letters" },
            { "945FB30E", "numbered symbols" },
            { "8FE9E9A6", "numbers" },
            { "1EFE9CB5", "unsc" },
            // FX
            { "67FE6E8C", "ambient life" },
            { "9F2A6BE7", "atmospherics" },
            { "04758E7F", "energy" },
            { "D96067BD", "explosions" },
            { "E84E2354", "fire" },
            { "05B5A5C8", "general" },
            { "9B475E78", "holograms" },
            { "04FEBB43", "liquids" },
            { "94CD949E", "smoke" },
            { "F24EA653", "sparks" },
            // GAMEPLAY
            { "3BD142A2", "audio" },
            { "0EA3DB6D", "equipment" },
            { "35328CB8", "game modes" },
            { "16A987BE", "launchers / lifts" },
            { "2EE94D70", "match flow" },
            { "82B4C1BA", "nav mesh" },
            { "466AD096", "player spawning" },
            { "D623245D", "sandbox" },
            { "A26A4C1A", "scripting" },
            { "9DAB4EF5", "teleporters" },
            { "D9026BD9", "vehicles" },
            { "0C5B1EFE", "volumes" },
            { "214E07D8", "weapon spawners" },
            { "AE54AEC7", "weapons" },
            // HALO DESIGN SET
            { "007F8C7D", "columns" },
            { "85FADED6", "columns mp" },
            { "6CB2C702", "cover" },
            { "931DAC45", "cover mp" },
            { "037F0DCC", "crates" },
            { "9E43E1BF", "crates mp" },
            { "4A50D3DE", "doorways" },
            { "891CD51E", "doorways mp" },
            { "B7D08EFA", "floors" },
            { "C3487ED6", "floors mp" },
            { "0F051460", "railings" },
            { "C7E7D7BE", "railings mp" },
            { "5A3E0F31", "ramps" },
            { "BD2FF5EF", "ramps mp" },
            { "51CB6FCF", "scale objects" },
            { "2E62784F", "walls" },
            { "8E06BFE6", "walls mp" },
            // LIGHTS
            { "4FA9B4B8", "forerunner light" },
            { "ED571C3C", "forerunner light mp" },
            { "545838D9", "forerunner no light" },
            { "DD37E7E2", "forerunner no light mp" },
            { "9C3D0A89", "generic light objects" },
            { "E019CC37", "unsc light" },
            { "6A72C8B2", "unsc light mp" },
            { "289FE80D", "unsc no light" },
            { "91964832", "unsc no light mp" },
            // PRIMITIVES
            { "F3228393", "blocks" },
            { "9A215954", "cones" },
            { "BD977D51", "cylinders" },
            { "0F2B2029", "pyramids" },
            { "B5A1024C", "rings" },
            { "15A30F2F", "spheres" },
            { "F6B1AB74", "trapezoids" },
            { "2C6FA031", "triangles" },
            // PROPS
            { "B33BA26D", "sports" },
            { "85274C4A", "summertime" },
            { "53087C47", "toys" },
            // STRUCTURES
            { "59E9E765", "beams" },
            { "6E0FC805", "bridges" },
            { "29DAE20F", "bridges mp" },
            { "13944678", "columns" },
            { "3D53832E", "cover" },
            { "0813C7E9", "doors" },
            { "FB42AFE8", "doors mp" },
            { "8C890378", "floors" },
            { "5589B3AD", "slopes" },
            { "4DE4BA8D", "walls" },
        };
        private static Dictionary<int, ForgeObjectData> forgeDataDict = new();
        private static Dictionary<string, FolderInfo> forgeFolderDict = new();
        private static int curDataBlockInd = 1;

        public static string ExtractForgeData(List<string> modulePaths)
        {
            // Get all food tag data can be done from the foom tag. Ez
            ForgeData fd = new();

            foreach (string path in modulePaths)
            {
                FileStream mStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                ModuleFile mFile = new ModuleFile();
                Module m = ModuleEditor.ReadModule(mStream);

                foreach (KeyValuePair<string, ModuleFile> mf in m.ModuleFiles)
                {
                    string TagPath = mf.Key.Replace("\0", String.Empty);

                    if (TagPath.EndsWith(".forgeobjectmanifest"))
                    {
                        curDataBlockInd = 1;

                        MemoryStream tStream = new();
                        tStream = ModuleEditor.GetTag(m, mStream, TagPath);
                        mf.Value.Tag = ModuleEditor.ReadTag(tStream, TagPath, mf.Value);
                        mf.Value.Tag.Name = TagPath;
                        mf.Value.Tag.ShortName = TagPath.Split("\\").Last();

                        fd.ManifestTag = TagPath;

                        Dictionary<long, TagLayouts.C> tagDefinitions = TagLayouts.Tags("foom");
                        GetForgeData(tagDefinitions, mf.Value, 0);
                    }
                }
            }

            // Set Folder Info
            foreach (ForgeObjectData fod in forgeDataDict.Values)
            {
                if (forgeFolderDict.ContainsKey(fod.FolderID))
                    fod.Folder = forgeFolderDict[fod.FolderID];
            }

            fd.Objects = forgeDataDict;

            return JsonConvert.SerializeObject(fd, Formatting.Indented).ToString();
        }

        private static void GetForgeData(Dictionary<long, TagLayouts.C> tagLayout, ModuleFile mf, long address, bool save = false, ForgeObjectData? fData = null, FolderInfo? folder = null)
        {
            byte[] data = mf.Tag.TagData;

            foreach (KeyValuePair<long, TagLayouts.C> entry in tagLayout)
            {
                try
                {
                    // Set the entries data address
                    entry.Value.MemoryAddress = address + entry.Key;

                    // Set Name, Type, and Raw Value
                    string name = "";
                    string type = "";
                    byte[] rawValue = GetDataFromByteArray((int)entry.Value.S, entry.Value.MemoryAddress, data);
                    if (entry.Value.N != null) name = entry.Value.N;
                    if (entry.Value.T != null) type = entry.Value.T;

                    if (save)
                    {
                        if (fData != null)
                        {
                            switch (name)
                            {
                                case "Forge Object":
                                    fData.TagID = ReverseHexString(BitConverter.ToUInt32(GetDataFromByteArray(4, 8, rawValue)).ToString("X"));
                                    TagInfo tag = GetTagInfo(fData.TagID);
                                    fData.TagName = tag.TagPath.Split("\\").Last().Split(".").First();
                                    break;
                                case "Name":
                                    fData.EntryName = ReverseHexString(BitConverter.ToUInt32(rawValue).ToString("X"));
                                    break;
                                case "Description":
                                    fData.EntryDescription = ReverseHexString(BitConverter.ToUInt32(rawValue).ToString("X"));
                                    break;
                                case "Keyword":
                                    fData.FolderID = ReverseHexString(BitConverter.ToUInt32(rawValue).ToString("X"));
                                    break;
                            }
                        }
                        
                        if (folder != null)
                        {
                            switch (name)
                            {
                                case "Title":
                                    folder.Title = ReverseHexString(BitConverter.ToUInt32(rawValue).ToString("X"));
                                    break;
                                case "Description":
                                    folder.Decription = ReverseHexString(BitConverter.ToUInt32(rawValue).ToString("X"));
                                    break;
                                case "Category ID":
                                    folder.CategoryID = ReverseHexString(BitConverter.ToUInt32(rawValue).ToString("X"));

                                    if (folderNames.ContainsKey(folder.CategoryID))
                                        folder.CategoryName = folderNames[folder.CategoryID];
                                    else
                                        folder.CategoryName = "Unknown";

                                    break;
                                case "Parent Category ID":
                                    folder.ParentID = ReverseHexString(BitConverter.ToUInt32(rawValue).ToString("X"));

                                    if (folderNames.ContainsKey(folder.ParentID))
                                        folder.ParentName = folderNames[folder.ParentID];
                                    else
                                        folder.ParentName = "Unknown";

                                    break;
                            }
                        }
                    }

                    // Handle tagblocks
                    switch (type)
                    {
                        case "Tagblock":
                            int childCount = BitConverter.ToInt32(GetDataFromByteArray(20, entry.Value.MemoryAddress, data), 16);
                            int curBlock = 0;
                            if (childCount > 0 && childCount < 100000 && entry.Value.N != "material constants")
                            {
                                curBlock = curDataBlockInd;
                                curDataBlockInd++;

                                for (int i = 0; i < childCount; i++)
                                {
                                    long newAddress = (long)mf.Tag.DataBlockArray[curBlock].Offset - (long)mf.Tag.DataBlockArray[0].Offset + (entry.Value.S * i);

                                    if (entry.Value.B != null)
                                    {
                                        if (name == "Forge Object Entries")
                                        {
                                            ForgeObjectData forgeData = new ForgeObjectData();
                                            GetForgeData(entry.Value.B, mf, newAddress, true, forgeData);
                                            forgeDataDict.Add(i + 1, forgeData);
                                        }
                                        else if (name == "Object Metadata" && fData != null)
                                        {
                                            GetForgeData(entry.Value.B, mf, newAddress, true, fData);
                                        }
                                        else if (name == "Category Entries")
                                        {
                                            FolderInfo folderInfo = new FolderInfo();
                                            folderInfo.CategoryIndex = i + 1;
                                            GetForgeData(entry.Value.B, mf, newAddress, true, null, folderInfo);
                                            forgeFolderDict.Add(folderInfo.CategoryID, folderInfo);
                                        }
                                        else
                                        {
                                            GetForgeData(entry.Value.B, mf, newAddress);
                                        }
                                    }
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
}
