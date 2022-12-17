using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace HavokScriptToolsCommon
{
    public record HksStructure
    (
        HksHeader Header,
        HksTypeEnum TypeEnum,
        List<HksFunctionBlock> Functions,
        int Unk,
        List<HksStructBlock> Structs
    );

    public record HksHeader
    (
        byte[] Signature,
        byte Version,
        byte Format,
        HksEndianness Endianness,
        byte IntSize,
        byte Size_tSize,
        byte InstructionSize,
        byte NumberSize,
        HksNumberType NumberType,
        byte Flags,
        byte Unk
    );

    public record HksTypeEnum
    (
        uint Count,
        List<HksTypeEnumEntry> Entries
    );

    public record HksTypeEnumEntry
    (
        int Value,
        string Name
    );

    public record HksFunctionBlock
    (
        uint UpvalueCount,
        uint ParamCount,
        byte IsVararg,
        uint SlotCount,
        int Unk,
        uint InstructionCount,
        List<HksInstruction> Instructions,
        uint ConstantCount,
        List<HksValue> Constants,
        int HasDebugInfo,
        HksFunctionDebugInfo? DebugInfo,
        uint FunctionCount
    )
    {
        public int Address { get; set; }
        public bool Flag_Vararg_HasArg => (IsVararg & (uint)HksVarargFlags.HASARG) == 0;
        public bool Flag_Vararg_IsVararg => (IsVararg & (uint)HksVarargFlags.ISVARARG) == 0;
        public bool Flag_Vararg_NeedsArg => (IsVararg & (uint)HksVarargFlags.NEEDSARG) == 0;
    }

    public enum HksVarargFlags
    {
        HASARG   = 1 << 0,
        ISVARARG = 1 << 1,
        NEEDSARG = 1 << 2
    }

    public record HksFunctionDebugInfo
    (
        uint LineCount,
        uint LocalsCount,
        uint UpvalueCount,
        uint LineBegin,
        uint LineEnd,
        string Path,
        string Name,
        List<int> Lines,
        List<HksDebugLocal> Locals,
        List<string> Upvalues
    );

    public record HksDebugLocal
    (
        string Name,
        int Start,
        int End
    );

    public record HksInstruction
    (
        HksOpCode OpCode,
        List<HksOpArg> Args
    );

    public record HksOpArg
    (
        HksOpArgMode Mode,
        int Value
    );

    public record HksStructBlock
    (
        HksStructHeader Header,
        int MemberCount,
        int? ExtendCount,
        List<string>? ExtendedStructs,
        List<HksStructMember> Members
    );

    public record HksStructMember
    (
        HksStructHeader Header,
        int Index // index of the member in the backing array?
    );

    public record HksStructHeader
    (
        string Name,
        int Unk0,
        uint StructId,
        HksType Type,
        int Unk1,
        int Unk2
    );

    public record HksValue
    (
        HksType Type,
        object? Value
    );

    public enum HksType
    {
        TNIL,
        TBOOLEAN,
        TLIGHTUSERDATA,
        TNUMBER,
        TSTRING,
        TTABLE,
        TFUNCTION,
        TUSERDATA,
        TTHREAD,
        TIFUNCTION,
        TCFUNCTION,
        TUI64,
        TSTRUCT
    }

    public enum HksNumberType
    {
        FLOAT,
        INTEGER
    }

    public enum HksEndianness
    {
        BIG,
        LITTLE
    }
}
