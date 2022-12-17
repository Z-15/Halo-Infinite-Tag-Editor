using System;
using System.Collections.Generic;
using System.Text;

namespace HavokScriptToolsCommon
{
    public enum HksOpCode
    {
        GETFIELD,
        TEST,
        CALL_I,
        CALL_C,
        EQ,
        EQ_BK,
        GETGLOBAL,
        MOVE,
        SELF,
        RETURN,
        GETTABLE_S,
        GETTABLE_N,
        GETTABLE,
        LOADBOOL,
        TFORLOOP,
        SETFIELD,
        SETTABLE_S,
        SETTABLE_S_BK,
        SETTABLE_N,
        SETTABLE_N_BK,
        SETTABLE,
        SETTABLE_BK,
        TAILCALL_I,
        TAILCALL_C,
        TAILCALL_M,
        LOADK,
        LOADNIL,
        SETGLOBAL,
        JMP,
        CALL_M,
        CALL,
        INTRINSIC_INDEX,
        INTRINSIC_NEWINDEX,
        INTRINSIC_SELF,
        INTRINSIC_LITERAL,
        INTRINSIC_NEWINDEX_LITERAL,
        INTRINSIC_SELF_LITERAL,
        TAILCALL,
        GETUPVAL,
        SETUPVAL,
        ADD,
        ADD_BK,
        SUB,
        SUB_BK,
        MUL,
        MUL_BK,
        DIV,
        DIV_BK,
        MOD,
        MOD_BK,
        POW,
        POW_BK,
        NEWTABLE,
        UNM,
        NOT,
        LEN,
        LT,
        LT_BK,
        LE,
        LE_BK,
        CONCAT,
        TESTSET,
        FORPREP,
        FORLOOP,
        SETLIST,
        CLOSE,
        CLOSURE,
        VARARG,
        TAILCALL_I_R1,
        CALL_I_R1,
        SETUPVAL_R1,
        TEST_R1,
        NOT_R1,
        GETFIELD_R1,
        SETFIELD_R1,
        NEWSTRUCT,
        DATA,
        SETSLOTN,
        SETSLOTI,
        SETSLOT,
        SETSLOTS,
        SETSLOTMT,
        CHECKTYPE,
        CHECKTYPES,
        GETSLOT,
        GETSLOTMT,
        SELFSLOT,
        SELFSLOTMT,
        GETFIELD_MM,
        CHECKTYPE_D,
        GETSLOT_D,
        GETGLOBAL_MEM,
        NUM_OPCODES
    }

    enum HksOpMode
    {
        iABC,
        iABx,
        iAsBx
    }

    enum HksOpArgModeA
    {
        UNUSED,
        REG
    }

    enum HksOpArgModeBC
    {
        UNUSED,
        NUMBER,
        OFFSET,
        REG,
        REG_OR_CONST,
        CONST
    }

    public enum HksOpArgMode
    {
        NUMBER,
        REG,
        CONST
    }

    struct HksOpModes
    {
        public readonly HksOpCode opCode;
        public readonly HksOpMode opMode;
        public readonly HksOpArgModeA opArgModeA;
        public readonly HksOpArgModeBC opArgModeB;
        public readonly HksOpArgModeBC opArgModeC;
        public HksOpModes(HksOpCode opCode, HksOpMode opMode, HksOpArgModeA opArgModeA, HksOpArgModeBC opArgModeB, HksOpArgModeBC opArgModeC)
        {
            this.opCode = opCode;
            this.opMode = opMode;
            this.opArgModeA = opArgModeA;
            this.opArgModeB = opArgModeB;
            this.opArgModeC = opArgModeC;
        }
    }

    static class HksOpInfo
    {
        public static readonly HksOpModes[] opModes =
        {
            new HksOpModes(HksOpCode.GETFIELD,                   HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG,          HksOpArgModeBC.CONST),
            new HksOpModes(HksOpCode.TEST,                       HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.UNUSED,       HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.CALL_I,                     HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.CALL_C,                     HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.EQ,                         HksOpMode.iABC,  HksOpArgModeA.UNUSED, HksOpArgModeBC.REG_OR_CONST, HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.EQ_BK,                      HksOpMode.iABC,  HksOpArgModeA.UNUSED, HksOpArgModeBC.REG_OR_CONST, HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.GETGLOBAL,                  HksOpMode.iABx,  HksOpArgModeA.REG,    HksOpArgModeBC.CONST,        HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.MOVE,                       HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG,          HksOpArgModeBC.UNUSED),
            new HksOpModes(HksOpCode.SELF,                       HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG,          HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.RETURN,                     HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.UNUSED),
            new HksOpModes(HksOpCode.GETTABLE_S,                 HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG,          HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.GETTABLE_N,                 HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG,          HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.GETTABLE,                   HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG,          HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.LOADBOOL,                   HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.TFORLOOP,                   HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.UNUSED,       HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.SETFIELD,                   HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.CONST,        HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.SETTABLE_S,                 HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG_OR_CONST, HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.SETTABLE_S_BK,              HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG_OR_CONST, HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.SETTABLE_N,                 HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG_OR_CONST, HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.SETTABLE_N_BK,              HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG_OR_CONST, HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.SETTABLE,                   HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG_OR_CONST, HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.SETTABLE_BK,                HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG_OR_CONST, HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.TAILCALL_I,                 HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.TAILCALL_C,                 HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.TAILCALL_M,                 HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.LOADK,                      HksOpMode.iABx,  HksOpArgModeA.REG,    HksOpArgModeBC.CONST,        HksOpArgModeBC.UNUSED),
            new HksOpModes(HksOpCode.LOADNIL,                    HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG,          HksOpArgModeBC.UNUSED),
            new HksOpModes(HksOpCode.SETGLOBAL,                  HksOpMode.iABx,  HksOpArgModeA.REG,    HksOpArgModeBC.CONST,        HksOpArgModeBC.UNUSED),
            new HksOpModes(HksOpCode.JMP,                        HksOpMode.iAsBx, HksOpArgModeA.UNUSED, HksOpArgModeBC.OFFSET,       HksOpArgModeBC.UNUSED),
            new HksOpModes(HksOpCode.CALL_M,                     HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.CALL,                       HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.INTRINSIC_INDEX,            HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.INTRINSIC_NEWINDEX,         HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.INTRINSIC_SELF,             HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.INTRINSIC_LITERAL,          HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.INTRINSIC_NEWINDEX_LITERAL, HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.INTRINSIC_SELF_LITERAL,     HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.TAILCALL,                   HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.GETUPVAL,                   HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.UNUSED),
            new HksOpModes(HksOpCode.SETUPVAL,                   HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.UNUSED),
            new HksOpModes(HksOpCode.ADD,                        HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG_OR_CONST, HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.ADD_BK,                     HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG_OR_CONST, HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.SUB,                        HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG_OR_CONST, HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.SUB_BK,                     HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG_OR_CONST, HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.MUL,                        HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG_OR_CONST, HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.MUL_BK,                     HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG_OR_CONST, HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.DIV,                        HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG_OR_CONST, HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.DIV_BK,                     HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG_OR_CONST, HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.MOD,                        HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG_OR_CONST, HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.MOD_BK,                     HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG_OR_CONST, HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.POW,                        HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG_OR_CONST, HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.POW_BK,                     HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG_OR_CONST, HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.NEWTABLE,                   HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.UNM,                        HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG,          HksOpArgModeBC.UNUSED),
            new HksOpModes(HksOpCode.NOT,                        HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG,          HksOpArgModeBC.UNUSED),
            new HksOpModes(HksOpCode.LEN,                        HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG,          HksOpArgModeBC.UNUSED),
            new HksOpModes(HksOpCode.LT,                         HksOpMode.iABC,  HksOpArgModeA.UNUSED, HksOpArgModeBC.REG_OR_CONST, HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.LT_BK,                      HksOpMode.iABC,  HksOpArgModeA.UNUSED, HksOpArgModeBC.REG_OR_CONST, HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.LE,                         HksOpMode.iABC,  HksOpArgModeA.UNUSED, HksOpArgModeBC.REG_OR_CONST, HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.LE_BK,                      HksOpMode.iABC,  HksOpArgModeA.UNUSED, HksOpArgModeBC.REG_OR_CONST, HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.CONCAT,                     HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.TESTSET,                    HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG,          HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.FORPREP,                    HksOpMode.iAsBx, HksOpArgModeA.REG,    HksOpArgModeBC.OFFSET,       HksOpArgModeBC.UNUSED),
            new HksOpModes(HksOpCode.FORLOOP,                    HksOpMode.iAsBx, HksOpArgModeA.REG,    HksOpArgModeBC.OFFSET,       HksOpArgModeBC.UNUSED),
            new HksOpModes(HksOpCode.SETLIST,                    HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.OFFSET),
            new HksOpModes(HksOpCode.CLOSE,                      HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.UNUSED,       HksOpArgModeBC.UNUSED),
            new HksOpModes(HksOpCode.CLOSURE,                    HksOpMode.iABx,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.UNUSED),
            new HksOpModes(HksOpCode.VARARG,                     HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.UNUSED),
            new HksOpModes(HksOpCode.TAILCALL_I_R1,              HksOpMode.iABC,  HksOpArgModeA.UNUSED, HksOpArgModeBC.NUMBER,       HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.CALL_I_R1,                  HksOpMode.iABC,  HksOpArgModeA.UNUSED, HksOpArgModeBC.NUMBER,       HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.SETUPVAL_R1,                HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.UNUSED),
            new HksOpModes(HksOpCode.TEST_R1,                    HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.UNUSED,       HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.NOT_R1,                     HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG,          HksOpArgModeBC.UNUSED),
            new HksOpModes(HksOpCode.GETFIELD_R1,                HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG,          HksOpArgModeBC.CONST),
            new HksOpModes(HksOpCode.SETFIELD_R1,                HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.CONST,        HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.NEWSTRUCT,                  HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.DATA,                       HksOpMode.iABx,  HksOpArgModeA.UNUSED, HksOpArgModeBC.OFFSET,       HksOpArgModeBC.UNUSED),
            new HksOpModes(HksOpCode.SETSLOTN,                   HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.UNUSED,       HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.SETSLOTI,                   HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.SETSLOT,                    HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.SETSLOTS,                   HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.REG),
            new HksOpModes(HksOpCode.SETSLOTMT,                  HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.REG_OR_CONST),
            new HksOpModes(HksOpCode.CHECKTYPE,                  HksOpMode.iABx,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.UNUSED),
            new HksOpModes(HksOpCode.CHECKTYPES,                 HksOpMode.iABx,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.UNUSED),
            new HksOpModes(HksOpCode.GETSLOT,                    HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG,          HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.GETSLOTMT,                  HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG,          HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.SELFSLOT,                   HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG,          HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.SELFSLOTMT,                 HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG,          HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.GETFIELD_MM,                HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG,          HksOpArgModeBC.CONST),
            new HksOpModes(HksOpCode.CHECKTYPE_D,                HksOpMode.iABx,  HksOpArgModeA.REG,    HksOpArgModeBC.NUMBER,       HksOpArgModeBC.UNUSED),
            new HksOpModes(HksOpCode.GETSLOT_D,                  HksOpMode.iABC,  HksOpArgModeA.REG,    HksOpArgModeBC.REG,          HksOpArgModeBC.NUMBER),
            new HksOpModes(HksOpCode.GETGLOBAL_MEM,              HksOpMode.iABx,  HksOpArgModeA.REG,    HksOpArgModeBC.CONST,        HksOpArgModeBC.NUMBER)
        };
    }
}
