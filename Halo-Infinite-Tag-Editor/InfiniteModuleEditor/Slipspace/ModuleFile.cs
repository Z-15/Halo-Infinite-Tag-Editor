using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;

namespace InfiniteModuleEditor
{
    public class ModuleFile
    {
        public ModuleFileEntry FileEntry;
        public List<BlockInfo> Blocks = new List<BlockInfo>();
        public Tag Tag;
    }

    [StructLayout(LayoutKind.Explicit, Size = 88)]
    public struct ModuleFileEntry
    {
        [FieldOffset(0)]
        public int ResourceCount;

        [FieldOffset(4)]
        public int ParentIndex;

        [FieldOffset(10)]
        public short BlockCount;

        [FieldOffset(12)]
        public int BlockIndex;

        [FieldOffset(16)]
        public int ResourceIndex;

        [FieldOffset(20)]
        public int ClassId; //BigEndian

        [FieldOffset(24)]
        public uint DataOffset;

        [FieldOffset(32)]
        public uint TotalCompressedSize;
        [FieldOffset(36)]
        public uint TotalUncompressedSize;

        [FieldOffset(40)]
        public int GlobalTagId;

        [FieldOffset(44)]
        public uint UncompressedHeaderSize;

        [FieldOffset(48)]
        public uint UncompressedTagDataSize;

        [FieldOffset(52)]
        public uint UncompressedResourceDataSize;

        [FieldOffset(56)]
        public short HeaderBlockCount;

        [FieldOffset(58)]
        public short TagDataBlockCount;

        [FieldOffset(60)]
        public short ResourceBlockCount;

        [FieldOffset(64)]
        public int NameOffset;

        [FieldOffset(72)]
        public long AssetChecksum;

        [FieldOffset(80)]
        public long AssetId;

    }

    public class BlockInfo
    {
        public Block BlockData;
        public long ModuleOffset;
        public int BlockType;
    }

    public class Block
    {
        public uint CompressedOffset { get; set; }
        public uint CompressedSize { get; set; }
        public uint UncompressedOffset { get; set; }
        public uint UncompressedSize { get; set; }
        public bool Compressed { get; set; }
    }
}