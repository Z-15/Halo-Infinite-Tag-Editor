using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;

namespace InfiniteModuleEditor
{
    public class Tag
    {
        public FileHeader Header { get; set; }
        public TagDependency[] TagDependencyArray { get; set; }
        public DataBlock[] DataBlockArray { get; set; }
        public TagStruct[] TagStructArray { get; set; }
        public DataReference[] DataReferenceArray { get; set; }
        public TagReferenceFixup[] TagReferenceFixupArray { get; set; }
        public StringID[] StringIDArray { get; set; }
        public byte[] StringTable { get; set; }
        public ZoneSetInformationHeader ZoneSetInfoHeader { get; set; }
        public ZoneSetEntry[] ZoneSetEntryArray { get; set; }
        public ZoneSetTag[] ZoneSetTagArray { get; set; }
        public byte[] TagData { get; set; }
        public byte[] ResourceData { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public int MainStructSize { get; set; }
        public int TotalTagBlockDataSize { get; set; }
        public int StringTableOffset { get; set; }
        public int TrueDataOffset { get; set; }
    }

    [StructLayout(LayoutKind.Explicit, Size = 80)]
    public struct FileHeader
    {
        [FieldOffset(0)]
        public int Magic;

        [FieldOffset(4)]
        public int Version;

        [FieldOffset(8)]
        public ulong UnknownProperty;

        [FieldOffset(16)]
        public ulong AssetChecksum;

        [FieldOffset(24)]
        public int DependencyCount;

        [FieldOffset(28)]
        public int DataBlockCount;

        [FieldOffset(32)]
        public int TagStructCount;

        [FieldOffset(36)]
        public int DataReferenceCount;

        [FieldOffset(40)]
        public int TagReferenceCount;

        [FieldOffset(44)]
        public uint StringTableSize;

        [FieldOffset(48)]
        public uint ZoneSetDataSize;

        [FieldOffset(52)]
        public uint UnknownProperty2;

        [FieldOffset(56)]
        public uint HeaderSize;

        [FieldOffset(60)]
        public uint DataSize;

        [FieldOffset(64)]
        public uint ResourceDataSize;

        [FieldOffset(68)]
        public int UnknownProperty3;

        [FieldOffset(72)]
        public byte HeaderAlignment;

        [FieldOffset(73)]
        public byte TagDataAlightment;

        [FieldOffset(74)]
        public byte ResourceDataAligment;

        [FieldOffset(75)]
        public byte UnknownProperty4;

        [FieldOffset(76)]
        public int UnknownProperty5;
    }

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public struct TagDependency
    {
        [FieldOffset(0)]
        public int GroupTag;

        [FieldOffset(4)]
        public uint NameOffset;

        [FieldOffset(8)]
        public long AssetID;

        [FieldOffset(16)]
        public int GlobalID;

        [FieldOffset(20)]
        public int UnknownProperty;
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct DataBlock
    {
        [FieldOffset(0)]
        public uint Size;

        [FieldOffset(4)]
        public short UnknownProperty;

        [FieldOffset(6)]
        public short Section;

        [FieldOffset(8)]
        public ulong Offset;
    }

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct TagStruct
    {
        [FieldOffset(0)]
        public long GUID1;

        [FieldOffset(8)]
        public long GUID2;

        [FieldOffset(16)]
        public short Type;

        [FieldOffset(18)]
        public short UnknownPropety;

        [FieldOffset(20)]
        public int TargetIndex;

        [FieldOffset(24)]
        public int FieldBlock;

        [FieldOffset(28)]
        public uint FieldOffset;
    }

    [StructLayout(LayoutKind.Explicit, Size = 20)]
    public struct DataReference
    {
        [FieldOffset(0)]
        public int ParentStructIndex;

        [FieldOffset(4)]
        public int UnknownProperty;

        [FieldOffset(8)]
        public int TargetIndex;

        [FieldOffset(12)]
        public int FieldBlock;

        [FieldOffset(16)]
        public uint FieldOffset;
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct TagReferenceFixup
    {
        [FieldOffset(0)]
        public int FieldBlock;

        [FieldOffset(4)]
        public uint FieldOffset;

        [FieldOffset(8)]
        public uint NameOffset;

        [FieldOffset(12)]
        public int DepdencyIndex;
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct StringID
    {
        [FieldOffset(0)]
        public uint UnknownProperty;

        [FieldOffset(4)]
        public uint StringOffset;
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct ZoneSetInformationHeader
    {
        [FieldOffset(0)]
        public long ZoneSetCount;

        [FieldOffset(8)]
        public long ZoneSetListOffset;
    }

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    public struct ZoneSetEntry
    {
        [FieldOffset(0)]
        public int StringID;

        [FieldOffset(4)]
        public int UnknownProperty;

        [FieldOffset(8)]
        public long UnknownProperty2;

        [FieldOffset(16)]
        public long UnknownProperty3;

        [FieldOffset(24)]
        public long TagCount;

        [FieldOffset(32)]
        public ulong TagListOffset;
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct ZoneSetTag
    {
        [FieldOffset(0)]
        public uint GlobalID;

        [FieldOffset(4)]
        public int StringID;
    }

    ///
    ///Tag Data
    ///

    [StructLayout(LayoutKind.Explicit, Size = 28)]
    public struct TagBlock
    {
        [FieldOffset(0)]
        public ulong Elements;

        [FieldOffset(8)]
        public ulong TypeInfo;

        [FieldOffset(16)]
        public int Count;

        [FieldOffset(20)]
        public int UnknownProperty;

        [FieldOffset(24)]
        public int UnknownProperty2;
    }

    [StructLayout(LayoutKind.Explicit, Size = 28)]
    public struct TagReference
    {
        [FieldOffset(0)]
        public ulong TypeInfo;

        [FieldOffset(8)]
        public int GlobalID;

        [FieldOffset(12)]
        public long AssetID;

        [FieldOffset(20)]
        public int GroupTag;

        [FieldOffset(24)]
        public int LocalHandle;

        public override string ToString()
        {
            return GlobalID + " " + AssetID + " " + GroupTag;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public struct DataReferenceField
    {
        [FieldOffset(0)]
        public ulong Data;

        [FieldOffset(8)]
        public ulong TypeInfo;

        [FieldOffset(16)]
        public int UnknownProperty;

        [FieldOffset(20)]
        public uint Size;

        public override string ToString()
        {
            return "Data Size:" + Size;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct PageableResource
    {
        [FieldOffset(0)]
        public ulong TypeInfo;

        [FieldOffset(8)]
        public uint Handle;

        [FieldOffset(12)]
        public int UnknownProperty;
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct RealBounds
    {
        [FieldOffset(0)]
        public float MinBound;

        [FieldOffset(4)]
        public float MaxBound;

        public override string ToString()
        {
            return MinBound + " " + MaxBound;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 12)]
    public struct RealVector3D
    {
        [FieldOffset(0)]
        public float I;

        [FieldOffset(4)]
        public float J;

        [FieldOffset(8)]
        public float K;

        public override string ToString()
        {
            return I + " " + J + " " + K;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct ShortBounds
    {
        [FieldOffset(0)]
        public short MinBound;

        [FieldOffset(2)]
        public short MaxBound;

        public override string ToString()
        {
            return MinBound + " " + MaxBound;
        }
    }

    [Flags]
    public enum Flags8 : byte
    {
        Bit0 = 1,
        Bit1 = 2,
        Bit2 = 4,
        Bit3 = 8,
        Bit4 = 16,
        Bit5 = 32,
        Bit6 = 64,
        Bit7 = 128
    }

    [Flags]
    public enum Flags16 : ushort
    {
        Bit0 = 1,
        Bit1 = 2,
        Bit2 = 4,
        Bit3 = 8,
        Bit4 = 16,
        Bit5 = 32,
        Bit6 = 64,
        Bit7 = 128,
        Bit8 = 256,
        Bit9 = 512,
        Bit10 = 1024,
        Bit11 = 2048,
        Bit12 = 4096,
        Bit13 = 8192,
        Bit14 = 16384,
        Bit15 = 32768
    }

    [Flags]
    public enum Flags32 : uint
    {
        Bit0 = 1,
        Bit1 = 2,
        Bit2 = 4,
        Bit3 = 8,
        Bit4 = 16,
        Bit5 = 32,
        Bit6 = 64,
        Bit7 = 128,
        Bit8 = 256,
        Bit9 = 512,
        Bit10 = 1024,
        Bit11 = 2048,
        Bit12 = 4096,
        Bit13 = 8192,
        Bit14 = 16384,
        Bit15 = 32768,
        Bit16 = 65536,
        Bit17 = 131072,
        Bit18 = 262144,
        Bit19 = 524288,
        Bit20 = 1048576,
        Bit21 = 2097152,
        Bit22 = 4194304,
        Bit23 = 8388608,
        Bit24 = 16777216,
        Bit25 = 33554432,
        Bit26 = 67108864,
        Bit27 = 134217728,
        Bit28 = 268435456,
        Bit29 = 536870912,
        Bit30 = 1073741824,
        Bit31 = 2147483648,
    }
}
