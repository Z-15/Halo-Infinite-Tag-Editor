using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Xml;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;
using OodleSharp;
using System.Diagnostics;

namespace InfiniteModuleEditor
{
   public class ModuleEditor
    {
        public static Module ReadModule(FileStream fileStream)
        {
            byte[] ModuleHeader = new byte[72];
            fileStream.Read(ModuleHeader, 0, 72);
            Module module = new Module
            {
                Head = Encoding.ASCII.GetString(ModuleHeader, 0, 4),
                Version = BitConverter.ToInt32(ModuleHeader, 4),
                ModuleId = BitConverter.ToInt64(ModuleHeader, 8),
                FileCount = BitConverter.ToInt32(ModuleHeader, 16),
                ManifestCount = BitConverter.ToInt32(ModuleHeader, 20),
                ResourceIndex = BitConverter.ToInt32(ModuleHeader, 32),
                StringsSize = BitConverter.ToInt32(ModuleHeader, 36),
                ResourceCount = BitConverter.ToInt32(ModuleHeader, 40),
                BlockCount = BitConverter.ToInt32(ModuleHeader, 44)
            };
            module.StringTableOffset = module.FileCount * 88 + 72; //72 is header size
            module.ResourceListOffset = module.StringTableOffset + module.StringsSize + 8; //Still dunno why these 8 bytes are here
            module.BlockListOffset = module.ResourceCount * 4 + module.ResourceListOffset;
            module.FileDataOffset = module.BlockCount * 20 + module.BlockListOffset; //inaccurate, need to skip past a bunch of 00s

            int FileEntriesSize = module.FileCount * 88;
            byte[] ModuleFileEntries = new byte[FileEntriesSize];
            fileStream.Read(ModuleFileEntries, 0, FileEntriesSize);
            fileStream.Seek(8, SeekOrigin.Current); //No idea what these bytes are for
            byte[] ModuleStrings = new byte[module.StringsSize];
            fileStream.Read(ModuleStrings, 0, module.StringsSize);

            //To fix the data offset
            fileStream.Seek(module.FileDataOffset, SeekOrigin.Begin);
            while (fileStream.ReadByte() == 0)
            {
                continue;
            }
            module.FileDataOffset = fileStream.Position - 1;

            Dictionary<int, string> StringList = new Dictionary<int, string>();

            for (int i = 0; i < FileEntriesSize; i += 88)
            {
                ModuleFileEntry moduleItem = new ModuleFileEntry
                {
                    ResourceCount = BitConverter.ToInt32(ModuleFileEntries, i),
                    ParentIndex = BitConverter.ToInt32(ModuleFileEntries, i + 4), //Seems to always be 0
                    //unknown int16 8
                    BlockCount = BitConverter.ToInt16(ModuleFileEntries, i + 10),
                    BlockIndex = BitConverter.ToInt32(ModuleFileEntries, i + 12),
                    ResourceIndex = BitConverter.ToInt32(ModuleFileEntries, i + 16),
                    ClassId = BitConverter.ToInt32(ModuleFileEntries, i + 20),
                    DataOffset = BitConverter.ToUInt32(ModuleFileEntries, i + 24), //some special stuff needs to be done here, check back later
                    //unknown int16 30
                    TotalCompressedSize = BitConverter.ToUInt32(ModuleFileEntries, i + 32),
                    TotalUncompressedSize = BitConverter.ToUInt32(ModuleFileEntries, i + 36),
                    GlobalTagId = BitConverter.ToInt32(ModuleFileEntries, i + 40),
                    UncompressedHeaderSize = BitConverter.ToUInt32(ModuleFileEntries, i + 44),
                    UncompressedTagDataSize = BitConverter.ToUInt32(ModuleFileEntries, i + 48),
                    UncompressedResourceDataSize = BitConverter.ToUInt32(ModuleFileEntries, i + 52),
                    HeaderBlockCount = BitConverter.ToInt16(ModuleFileEntries, i + 56),
                    TagDataBlockCount = BitConverter.ToInt16(ModuleFileEntries, i + 58),
                    ResourceBlockCount = BitConverter.ToInt16(ModuleFileEntries, i + 60),
                    //padding
                    NameOffset = BitConverter.ToInt32(ModuleFileEntries, i + 64),
                    //unknown int32 68 //Seems to always be -1
                    AssetChecksum = BitConverter.ToInt64(ModuleFileEntries, i + 72),
                    AssetId = BitConverter.ToInt64(ModuleFileEntries, i + 80)
                };
                if (moduleItem.GlobalTagId == -1)
                {
                    continue;
                }
                ModuleFileEntry moduleItemNext = new ModuleFileEntry();
                string TagName;
                if (i + 88 != FileEntriesSize)
                {
                    moduleItemNext.NameOffset = BitConverter.ToInt32(ModuleFileEntries, i + 88 + 64);
                    TagName = Encoding.ASCII.GetString(ModuleStrings, moduleItem.NameOffset, moduleItemNext.NameOffset - moduleItem.NameOffset);
                }
                else
                {
                    TagName = Encoding.ASCII.GetString(ModuleStrings, moduleItem.NameOffset, module.StringsSize - moduleItem.NameOffset);
                }
                StringList.Add(moduleItem.GlobalTagId, TagName);
                module.ModuleFiles.Add(TagName, new ModuleFile { FileEntry = moduleItem });
            }
            return module;
        }

        public static MemoryStream GetTag(Module module, FileStream fileStream, string SearchTerm)
        {
            foreach (KeyValuePair<string, ModuleFile> moduleFile in module.ModuleFiles)
            {
                if (!moduleFile.Key.Contains(SearchTerm))
                {
                    continue;
                }
                if (moduleFile.Value.FileEntry.TotalUncompressedSize == 0)
                {
                    continue;
                }
                ulong FirstBlockOffset = moduleFile.Value.FileEntry.DataOffset + (ulong)module.FileDataOffset;
                MemoryStream outputStream = new MemoryStream();
                if (moduleFile.Value.FileEntry.BlockCount != 0)
                {
                    for (int y = 0; y < moduleFile.Value.FileEntry.BlockCount; y++)
                    {
                        byte[] BlockBuffer = new byte[20];
                        fileStream.Seek((moduleFile.Value.FileEntry.BlockIndex * 20) + module.BlockListOffset + (y * 20), 0);
                        fileStream.Read(BlockBuffer, 0, 20);
                        Block block = new Block
                        {
                            CompressedOffset = BitConverter.ToUInt32(BlockBuffer, 0),
                            CompressedSize = BitConverter.ToUInt32(BlockBuffer, 4),
                            UncompressedOffset = BitConverter.ToUInt32(BlockBuffer, 8),
                            UncompressedSize = BitConverter.ToUInt32(BlockBuffer, 12),
                            Compressed = BitConverter.ToBoolean(BlockBuffer, 16)
                        };

                        //This is where it gets ugly-er
                        byte[] BlockFile = new byte[block.CompressedSize];
                        ulong BlockOffset = FirstBlockOffset + block.CompressedOffset;
                        fileStream.Seek((long)BlockOffset, 0);
                        moduleFile.Value.Blocks.Add(new BlockInfo { BlockData = block, ModuleOffset = fileStream.Position, BlockType = y });
                        fileStream.Read(BlockFile, 0, (int)block.CompressedSize);
                        if (block.Compressed)
                        {
                            byte[] DecompressedFile = Oodle.Decompress(BlockFile, BlockFile.Length, (int)block.UncompressedSize);
                            outputStream.Write(DecompressedFile, 0, DecompressedFile.Length);
                        }
                        else //if the block file is uncompressed
                        {
                            outputStream.Write(BlockFile, 0, BlockFile.Length);
                        }
                    }
                }
                else
                {
                    byte[] CompressedFile = new byte[moduleFile.Value.FileEntry.TotalCompressedSize];
                    fileStream.Seek((int)moduleFile.Value.FileEntry.DataOffset, 0);
                    fileStream.Read(CompressedFile, 0, (int)moduleFile.Value.FileEntry.TotalCompressedSize);
                    byte[] DecompressedFile = Oodle.Decompress(CompressedFile, (int)moduleFile.Value.FileEntry.TotalCompressedSize, (int)moduleFile.Value.FileEntry.TotalUncompressedSize);
                    outputStream.Write(DecompressedFile, 0, DecompressedFile.Length);
                }

                return outputStream;
            }
            return null;
        }

        public static Tag ReadTag(MemoryStream TagStream, string ShortTagName, ModuleFile ModuleFile)
        {

            Tag tag = new Tag();
            byte[] TagHeader = new byte[80];

            TagStream.Seek(0, SeekOrigin.Begin);
            TagStream.Read(TagHeader, 0, 80);


            GCHandle HeaderHandle = GCHandle.Alloc(TagHeader, GCHandleType.Pinned);
            tag.Header = (FileHeader)Marshal.PtrToStructure(HeaderHandle.AddrOfPinnedObject(), typeof(FileHeader)); //No idea how this magic bytes to structure stuff works, I just got this from github
            HeaderHandle.Free();

            tag.Name = ShortTagName;
            tag.TagDependencyArray = new TagDependency[tag.Header.DependencyCount];
            tag.DataBlockArray = new DataBlock[tag.Header.DataBlockCount];
            tag.TagStructArray = new TagStruct[tag.Header.TagStructCount];
            tag.DataReferenceArray = new DataReference[tag.Header.DataReferenceCount];
            tag.TagReferenceFixupArray = new TagReferenceFixup[tag.Header.TagReferenceCount];

            tag.StringTable = new byte[tag.Header.StringTableSize];

            for (long l = 0; l < tag.Header.DependencyCount; l++) //For each tag dependency, fill in its values
            {
                byte[] TagDependencyBytes = new byte[Marshal.SizeOf(tag.TagDependencyArray[l])];
                TagStream.Read(TagDependencyBytes, 0, Marshal.SizeOf(tag.TagDependencyArray[l]));
                GCHandle TagDependencyHandle = GCHandle.Alloc(TagDependencyBytes, GCHandleType.Pinned);
                tag.TagDependencyArray[l] = (TagDependency)Marshal.PtrToStructure(TagDependencyHandle.AddrOfPinnedObject(), typeof(TagDependency));
                TagDependencyHandle.Free();
            }

            if (ShortTagName.EndsWith(".biped"))
            {
                TagStream.Position += 128;
            }

            for (long l = 0; l < tag.Header.DataBlockCount; l++)
            {
                byte[] DataBlockBytes = new byte[Marshal.SizeOf(tag.DataBlockArray[l])];
                TagStream.Read(DataBlockBytes, 0, Marshal.SizeOf(tag.DataBlockArray[l]));
                GCHandle DataBlockHandle = GCHandle.Alloc(DataBlockBytes, GCHandleType.Pinned);
                tag.DataBlockArray[l] = (DataBlock)Marshal.PtrToStructure(DataBlockHandle.AddrOfPinnedObject(), typeof(DataBlock));
                DataBlockHandle.Free();
            }

            for (long l = 0; l < tag.Header.TagStructCount; l++)
            {
                byte[] TagStructBytes = new byte[Marshal.SizeOf(tag.TagStructArray[l])];
                TagStream.Read(TagStructBytes, 0, Marshal.SizeOf(tag.TagStructArray[l]));
                GCHandle TagStructHandle = GCHandle.Alloc(TagStructBytes, GCHandleType.Pinned);
                tag.TagStructArray[l] = (TagStruct)Marshal.PtrToStructure(TagStructHandle.AddrOfPinnedObject(), typeof(TagStruct));
                TagStructHandle.Free();
            }


            for (long l = 0; l < tag.Header.DataReferenceCount; l++)
            {
                byte[] DataReferenceBytes = new byte[Marshal.SizeOf(tag.DataReferenceArray[l])];
                TagStream.Read(DataReferenceBytes, 0, Marshal.SizeOf(tag.DataReferenceArray[l]));
                GCHandle DataReferenceHandle = GCHandle.Alloc(DataReferenceBytes, GCHandleType.Pinned);
                tag.DataReferenceArray[l] = (DataReference)Marshal.PtrToStructure(DataReferenceHandle.AddrOfPinnedObject(), typeof(DataReference));
                DataReferenceHandle.Free();
            }

            for (long l = 0; l < tag.Header.TagReferenceCount; l++)
            {
                byte[] TagReferenceBytes = new byte[Marshal.SizeOf(tag.TagReferenceFixupArray[l])];
                TagStream.Read(TagReferenceBytes, 0, Marshal.SizeOf(tag.TagReferenceFixupArray[l]));
                GCHandle TagReferenceHandle = GCHandle.Alloc(TagReferenceBytes, GCHandleType.Pinned);
                tag.TagReferenceFixupArray[l] = (TagReferenceFixup)Marshal.PtrToStructure(TagReferenceHandle.AddrOfPinnedObject(), typeof(TagReferenceFixup));
                TagReferenceHandle.Free();
            }

            TagStream.Read(tag.StringTable, 0, (int)tag.Header.StringTableSize); //better hope this never goes beyond sizeof(int)
            TagStream.Seek(tag.Header.ZoneSetDataSize, SeekOrigin.Current); //Data starts here after the "StringID" section which is probably something else

            tag.TrueDataOffset = (int)(tag.Header.HeaderSize + tag.DataBlockArray[0].Offset);
            TagStream.Seek(tag.TrueDataOffset, SeekOrigin.Begin);
            tag.TagData = new byte[tag.Header.DataSize];
            TagStream.Read(tag.TagData, 0, (int)tag.Header.DataSize);

            tag.MainStructSize = 0;
            tag.TotalTagBlockDataSize = 0;

            for (int i = 0; i < tag.DataBlockArray.Length; i++)
            {
                tag.TotalTagBlockDataSize += (int)tag.DataBlockArray[i].Size;
            }

            tag.MainStructSize = (int)(tag.Header.DataSize - tag.TotalTagBlockDataSize);

            return tag;
        }

        public static Tag ReadTag(FileStream TagStream, string ShortTagName)
        {

            Tag tag = new Tag();
            byte[] TagHeader = new byte[80];

            TagStream.Seek(0, SeekOrigin.Begin);
            TagStream.Read(TagHeader, 0, 80);

            GCHandle HeaderHandle = GCHandle.Alloc(TagHeader, GCHandleType.Pinned);
            tag.Header = (FileHeader)Marshal.PtrToStructure(HeaderHandle.AddrOfPinnedObject(), typeof(FileHeader)); //No idea how this magic bytes to structure stuff works, I just got this from github
            HeaderHandle.Free();

            tag.TagDependencyArray = new TagDependency[tag.Header.DependencyCount];
            tag.DataBlockArray = new DataBlock[tag.Header.DataBlockCount];
            tag.TagStructArray = new TagStruct[tag.Header.TagStructCount];
            tag.DataReferenceArray = new DataReference[tag.Header.DataReferenceCount];
            tag.TagReferenceFixupArray = new TagReferenceFixup[tag.Header.TagReferenceCount];

            tag.StringTable = new byte[tag.Header.StringTableSize];

            for (long l = 0; l < tag.Header.DependencyCount; l++) //For each tag dependency, fill in its values
            {
                byte[] TagDependencyBytes = new byte[Marshal.SizeOf(tag.TagDependencyArray[l])];
                TagStream.Read(TagDependencyBytes, 0, Marshal.SizeOf(tag.TagDependencyArray[l]));
                GCHandle TagDependencyHandle = GCHandle.Alloc(TagDependencyBytes, GCHandleType.Pinned);
                tag.TagDependencyArray[l] = (TagDependency)Marshal.PtrToStructure(TagDependencyHandle.AddrOfPinnedObject(), typeof(TagDependency));
                TagDependencyHandle.Free();
            }

            for (long l = 0; l < tag.Header.DataBlockCount; l++)
            {
                byte[] DataBlockBytes = new byte[Marshal.SizeOf(tag.DataBlockArray[l])];
                TagStream.Read(DataBlockBytes, 0, Marshal.SizeOf(tag.DataBlockArray[l]));
                GCHandle DataBlockHandle = GCHandle.Alloc(DataBlockBytes, GCHandleType.Pinned);
                tag.DataBlockArray[l] = (DataBlock)Marshal.PtrToStructure(DataBlockHandle.AddrOfPinnedObject(), typeof(DataBlock));
                DataBlockHandle.Free();
            }

            for (long l = 0; l < tag.Header.TagStructCount; l++)
            {
                byte[] TagStructBytes = new byte[Marshal.SizeOf(tag.TagStructArray[l])];
                TagStream.Read(TagStructBytes, 0, Marshal.SizeOf(tag.TagStructArray[l]));
                GCHandle TagStructHandle = GCHandle.Alloc(TagStructBytes, GCHandleType.Pinned);
                tag.TagStructArray[l] = (TagStruct)Marshal.PtrToStructure(TagStructHandle.AddrOfPinnedObject(), typeof(TagStruct));
                TagStructHandle.Free();
            }


            for (long l = 0; l < tag.Header.DataReferenceCount; l++)
            {
                byte[] DataReferenceBytes = new byte[Marshal.SizeOf(tag.DataReferenceArray[l])];
                TagStream.Read(DataReferenceBytes, 0, Marshal.SizeOf(tag.DataReferenceArray[l]));
                GCHandle DataReferenceHandle = GCHandle.Alloc(DataReferenceBytes, GCHandleType.Pinned);
                tag.DataReferenceArray[l] = (DataReference)Marshal.PtrToStructure(DataReferenceHandle.AddrOfPinnedObject(), typeof(DataReference));
                DataReferenceHandle.Free();
            }

            for (long l = 0; l < tag.Header.TagReferenceCount; l++)
            {
                byte[] TagReferenceBytes = new byte[Marshal.SizeOf(tag.TagReferenceFixupArray[l])];
                TagStream.Read(TagReferenceBytes, 0, Marshal.SizeOf(tag.TagReferenceFixupArray[l]));
                GCHandle TagReferenceHandle = GCHandle.Alloc(TagReferenceBytes, GCHandleType.Pinned);
                tag.TagReferenceFixupArray[l] = (TagReferenceFixup)Marshal.PtrToStructure(TagReferenceHandle.AddrOfPinnedObject(), typeof(TagReferenceFixup));
                TagReferenceHandle.Free();
            }

            TagStream.Read(tag.StringTable, 0, (int)tag.Header.StringTableSize); //better hope this never goes beyond sizeof(int)
            TagStream.Seek(tag.Header.ZoneSetDataSize, SeekOrigin.Current); //Data starts here after the "StringID" section which is probably something else

            tag.TagData = new byte[tag.Header.DataSize];
            TagStream.Read(tag.TagData, 0, (int)tag.Header.DataSize);

            tag.MainStructSize = 0;
            tag.TotalTagBlockDataSize = 0;

            for (int i = 0; i < tag.DataBlockArray.Length; i++)
            {
                tag.TotalTagBlockDataSize += (int)tag.DataBlockArray[i].Size;
            }

            tag.MainStructSize = (int)(tag.Header.DataSize - tag.TotalTagBlockDataSize);

            return tag;
        }

        public static bool WriteTag(ModuleFile ModuleFile, MemoryStream TagStream, FileStream ModuleStream)
        {
            for(int i = 0; i < ModuleFile.Blocks.Count; i++)
            {
                byte[] modifiedBlock = new byte[ModuleFile.Blocks[i].BlockData.UncompressedSize];

                TagStream.Seek(ModuleFile.Blocks[i].BlockData.UncompressedOffset, SeekOrigin.Begin);
                TagStream.Read(modifiedBlock, 0, modifiedBlock.Length);

                byte[] compressedBlock = Oodle.Compress(modifiedBlock, modifiedBlock.Length, OodleFormat.Kraken, OodleCompressionLevel.Optimal5);

                if (modifiedBlock.Length == ModuleFile.Blocks[i].BlockData.CompressedSize)
                {
                    compressedBlock = compressedBlock.Skip(2).ToArray();
                }

                if (compressedBlock.Length <= ModuleFile.Blocks[i].BlockData.CompressedSize)
                {
                    ModuleStream.Seek(ModuleFile.Blocks[i].ModuleOffset, SeekOrigin.Begin);
                    ModuleStream.Write(compressedBlock, 0, compressedBlock.Length);
                }
                else return false;
            }

            return true;            
        }
    }
}
