using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Globalization;

namespace HavokScriptToolsCommon
{
    public class HksAssembler
    {
        string filename;
        HksHeader? globalHeader;

        public HksAssembler(string filename)
        {
            this.filename = filename;
        }

        public byte[] Assemble(string outfile)
        {
            byte[] result = Assemble();
            using (var bw = new BinaryWriter(File.Open(outfile, FileMode.Create)))
            {
                bw.Write(result);
            }
            return result;
        }

        public byte[] Assemble()
        {
            var lex = Lex();
            var structure = Parse(lex);

            var bw = new MyBinaryWriter();

            // header
            bw.WriteString("\x1bLua");
            bw.WriteInt8(0x51);
            bw.WriteInt8(14);
            bw.WriteUInt8((byte)structure.Header.Endianness);
            bw.WriteUInt8(structure.Header.IntSize);
            bw.WriteUInt8(structure.Header.Size_tSize);
            bw.WriteUInt8(structure.Header.InstructionSize);
            bw.WriteUInt8(structure.Header.NumberSize);
            bw.WriteUInt8((byte)structure.Header.NumberType);
            bw.WriteUInt8(structure.Header.Flags);
            bw.WriteUInt8(structure.Header.Unk);
            bw.SetLittleEndian(structure.Header.Endianness == HksEndianness.LITTLE);

            // type enum
            bw.WriteInt32(structure.TypeEnum.Entries.Count);
            foreach (var typeEnumEntry in structure.TypeEnum.Entries)
            {
                bw.WriteInt32(typeEnumEntry.Value);
                bw.WriteInt32(typeEnumEntry.Name.Length + 1);
                bw.WriteString(typeEnumEntry.Name + "\0");
            }

            // functions
            foreach (var function in structure.Functions)
            {
                bw.WriteUInt32(function.UpvalueCount);
                bw.WriteUInt32(function.ParamCount);
                bw.WriteUInt8(function.IsVararg);
                bw.WriteUInt32(function.SlotCount);
                bw.WriteInt32(function.Unk);
                bw.WriteInt32(function.Instructions.Count);
                bw.Pad(4, 0x5f);

                foreach (var instruction in function.Instructions)
                {
                    bw.WriteUInt32(AssembleInstruction(instruction));
                }

                bw.WriteInt32(function.Constants.Count);
                foreach (var constant in function.Constants)
                {
                    AssembleValue(constant, bw);
                }

                bw.WriteInt32(function.HasDebugInfo);
                if (function.HasDebugInfo == 1)
                {
                    bw.WriteInt32(function.DebugInfo!.Lines.Count);
                    bw.WriteInt32(function.DebugInfo!.Locals.Count);
                    bw.WriteInt32(function.DebugInfo!.Upvalues.Count);
                    bw.WriteUInt32(function.DebugInfo!.LineBegin);
                    bw.WriteUInt32(function.DebugInfo!.LineEnd);
                    AssembleString(function.DebugInfo!.Path, bw, false);
                    if (Regex.IsMatch(function.DebugInfo!.Name, "FUNC_[0-9A-F]{8}"))
                    {
                        // discard auto-generated function names since the address might change
                        AssembleString("", bw, false);
                    }
                    else
                    {
                        AssembleString(function.DebugInfo!.Name, bw, false);
                    }
                    foreach (int line in function.DebugInfo!.Lines)
                    {
                        bw.WriteInt32(line);
                    }
                    foreach (HksDebugLocal local in function.DebugInfo!.Locals)
                    {
                        AssembleString(local.Name, bw);
                        bw.WriteInt32(local.Start);
                        bw.WriteInt32(local.End);
                    }
                    foreach (string upvalue in function.DebugInfo!.Upvalues)
                    {
                        AssembleString(upvalue, bw);
                    }
                }

                bw.WriteUInt32(function.FunctionCount);
            }

            // unk
            bw.WriteInt32(1);

            // structs
            foreach (HksStructBlock struct_ in structure.Structs)
            {
                AssembleStructHeader(struct_.Header, bw);
                bw.WriteInt32(struct_.Members.Count);
                if (struct_.ExtendedStructs is List<string> extendedStructs)
                {
                    bw.WriteInt32(struct_.ExtendedStructs.Count);
                    foreach (string extendedStruct in extendedStructs)
                    {
                        AssembleString(extendedStruct, bw);
                    }
                }
                foreach (HksStructMember member in struct_.Members)
                {
                    AssembleStructHeader(member.Header, bw);
                    bw.WriteInt32(member.Index);
                }
            }

            bw.WriteInt64(0);

            return bw.GetData().ToArray();
        }

        private void AssembleStructHeader(HksStructHeader header, MyBinaryWriter bw)
        {
            AssembleString(header.Name, bw);
            bw.WriteInt32(header.Unk0);
            bw.WriteUInt32(header.StructId);
            bw.WriteInt32((int)header.Type);
            bw.WriteInt32(header.Unk1);
            bw.WriteInt32(header.Unk2);
        }

        private uint AssembleInstruction(HksInstruction instruction)
        {
            uint raw = (uint)instruction.OpCode << 25;
            var opModes = HksOpInfo.opModes[(int)instruction.OpCode];
            int currentArg = 0;

            // argument A
            raw |= (uint)instruction.Args[currentArg++].Value;

            if (opModes.opMode == HksOpMode.iABC)
            {
                if (opModes.opArgModeB != HksOpArgModeBC.UNUSED)
                {
                    int value = instruction.Args[currentArg].Value;
                    if (opModes.opArgModeB == HksOpArgModeBC.OFFSET)
                    {
                        HksAssemblyException.Assert(value <= 0x1ff && value >= 0, "immediate value out of range");
                    }
                    else
                    {
                        HksAssemblyException.Assert(value <= 0xff && value >= 0, "immediate value out of range");
                    }

                    if (opModes.opArgModeB == HksOpArgModeBC.REG_OR_CONST && instruction.Args[currentArg].Mode == HksOpArgMode.CONST)
                    {
                        value |= 0x100;
                    }
                    raw |= (uint)value << 17;
                    currentArg++;
                }

                if (opModes.opArgModeC != HksOpArgModeBC.UNUSED)
                {
                    int value = instruction.Args[currentArg].Value;
                    if (opModes.opArgModeC == HksOpArgModeBC.OFFSET)
                    {
                        HksAssemblyException.Assert(value <= 0x1ff && value >= 0, "immediate value out of range");
                    }
                    else
                    {
                        HksAssemblyException.Assert(value <= 0xff && value >= 0, "immediate value out of range");
                    }

                    if (opModes.opArgModeC == HksOpArgModeBC.REG_OR_CONST && instruction.Args[currentArg].Mode == HksOpArgMode.CONST)
                    {
                        value |= 0x100;
                    }
                    raw |= (uint)value << 8;
                    currentArg++;
                }
            }
            else
            {
                if (opModes.opArgModeB != HksOpArgModeBC.UNUSED)
                {
                    int value = instruction.Args[currentArg].Value;
                    if (opModes.opMode == HksOpMode.iAsBx)
                    {
                        value += 0xffff;
                    }
                    HksAssemblyException.Assert(value >= 0 && value <= 0x1ffff, "immediate value out of range");
                    raw |= (uint)value << 8;
                    currentArg++;
                }
            }

            return raw;
        }

        private void AssembleValue(HksValue value, MyBinaryWriter bw)
        {
            bw.WriteUInt8((byte)value.Type);
            switch (value.Type)
            {
                case HksType.TNIL:
                    // no value
                    break;
                case HksType.TBOOLEAN:
                    bw.WriteBool((bool)value.Value!);
                    break;
                case HksType.TSTRING:
                    AssembleString((string)value.Value!, bw);
                    break;
                case HksType.TNUMBER:
                    AssembleNumber(value.Value!, bw);
                    break;
                case HksType.TLIGHTUSERDATA:
                    bw.WriteInt64((long)value.Value!);
                    break;
                default:
                    throw new HksDisassemblyException("type not implemented: " + value.Type);
            }
        }

        private void AssembleString(string value, MyBinaryWriter bw, bool addNullTerminatorIfEmpty=true)
        {
            if (value.Length != 0 || addNullTerminatorIfEmpty)
            {
                value += '\0';
            }
            if (globalHeader!.Size_tSize == 4)
            {
                bw.WriteInt32(value.Length);
            }
            else
            {
                bw.WriteInt64(value.Length);
            }

            bw.WriteString(value);
        }

        private void AssembleNumber(object value, MyBinaryWriter bw)
        {
            if (value is int intValue)
            {
                bw.WriteInt32(intValue);
            }
            else if (value is long longValue)
            {
                bw.WriteInt64(longValue);
            }
            else if (value is float floatValue)
            {
                bw.WriteFloat(floatValue);
            }
            else if (value is double doubleValue)
            {
                bw.WriteDouble(doubleValue);
            }
            else
            {
                throw new HksDisassemblyException("this shouldn't happen");
            }
        }

        private List<List<string>> Lex()
        {
            // language=regex
            string instructionExp = "([RK]\\()|([,);[\\]])| "; // register/constant | separators | spaces
            // language=regex
            string directiveExp = "(\"[^\"]*\")|(\\([^)]*\\))|([,;])| "; // string | parentheses | separators | spaces

            var lex = new List<List<string>>();
            foreach (string fileLine in File.ReadLines(filename))
            {
                string line = fileLine.Trim();
                string[] split;
                if (line.StartsWith("."))
                {
                    split = Regex.Split(line, directiveExp);
                }
                else
                {
                    split = Regex.Split(line, instructionExp);
                }

                List<string> lineLex = new();
                foreach (string token in split)
                {
                    if (token == ";")
                    {
                        break;
                    }
                    else if (token != "")
                    {
                        lineLex.Add(token);
                    }
                }
                if (lineLex.Count > 0)
                {
                    lex.Add(lineLex);
                }
            }
            return lex;
        }


        private HksStructure Parse(List<List<string>> lex)
        {
            int cursor = 0;
            globalHeader = ParseHeader(lex, ref cursor);
            var functions = new List<HksFunctionBlock>();
            for (uint functionCount = 1; functions.Count < functionCount;)
            {
                HksFunctionBlock function = ParseFunction(lex, ref cursor);
                functions.Add(function);
                functionCount += function.FunctionCount;
            }
            var structs = new List<HksStructBlock>();
            while (cursor < lex.Count)
            {
                structs.Add(ParseStruct(lex, ref cursor));
            }
            
            return new HksStructure(globalHeader, GetTypeEnum(), functions, 1, structs);
        }

        private HksHeader ParseHeader(List<List<string>> lex, ref int cursor)
        {
            var foundDirectives = new Dictionary<string, string[]>();
            for (int i = 0; i < 7; i++, cursor++)
            {
                var line = ParseDirective(lex[cursor]);
                string directive = line[0];
                string[] args = line.Skip(1).ToArray();
                HksAssemblyException.Assert(!foundDirectives.ContainsKey(directive), "Duplicate directive: " + directive);
                foundDirectives[directive] = args;
            }

            // TODO: error handling
            HksEndianness endianness = (HksEndianness)Enum.Parse(typeof(HksEndianness), foundDirectives[".endianness"][0], true);
            byte intSize = byte.Parse(foundDirectives[".int_size"][0]);
            byte size_tSize = byte.Parse(foundDirectives[".size_t_size"][0]);
            byte instructionSize = byte.Parse(foundDirectives[".instruction_size"][0]);
            byte numberSize = byte.Parse(foundDirectives[".number_size"][0]);
            HksNumberType numberType = (HksNumberType)Enum.Parse(typeof(HksNumberType), foundDirectives[".number_type"][0], true);
            byte flags = byte.Parse(foundDirectives[".flags"][0]);

            byte[] signature = { 0x1b, 0x4c, 0x75, 0x61 }; // "\x1bLua"
            return new HksHeader(signature, 0x51, 14, endianness, intSize, size_tSize, instructionSize, numberSize, numberType, flags, 0);
        }

        private HksFunctionBlock ParseFunction(List<List<string>> lex, ref int cursor)
        {
            var constants = new List<HksValue>();
            var upvalues = new List<string>();
            var locals = new List<HksDebugLocal>();
            var foundDirectives = new Dictionary<string, string[]>();
            for (var line = lex[cursor]; line[0].StartsWith("."); line = lex[++cursor])
            {
                line = ParseDirective(line);
                if (line[0] == ".constant")
                {
                    constants.Add(ParseValue(line[1]));
                }
                else if (line[0] == ".upvalue")
                {
                    upvalues.Add(line[1]);
                }
                else if (line[0] == ".local")
                {
                    locals.Add(new HksDebugLocal(line[1], int.Parse(line[2]), int.Parse(line[3])));
                }
                else
                {
                    foundDirectives.Add(line[0], line.Skip(1).ToArray());
                }
            }

            string functionName = string.Join(' ', foundDirectives[".function"]);
            uint upvalueCount = uint.Parse(foundDirectives[".upvalue_count"][0]);
            uint paramCount = uint.Parse(foundDirectives[".param_count"][0]);
            byte isVararg = byte.Parse(foundDirectives[".is_vararg"][0]);
            uint slotCount = uint.Parse(foundDirectives[".slot_count"][0]);
            uint functionCount = uint.Parse(foundDirectives[".function_count"][0]);


            var instructions = new List<HksInstruction>();
            var lineNumbers = new List<int>();
            for (; cursor < lex.Count; cursor++)
            {
                var line = lex[cursor];
                if (line[0].StartsWith("."))
                {
                    break;
                }
                instructions.Add(ParseInstruction(line, ref lineNumbers));
            }

            int hasDebugInfo = 0;
            HksFunctionDebugInfo? debugInfo = null;
            if (foundDirectives.ContainsKey(".debug_info"))
            {
                hasDebugInfo = 1;
                string path = "";
                if (foundDirectives.ContainsKey(".path"))
                {
                    path = string.Join(' ', foundDirectives[".path"]);
                }
                uint lineBegin = uint.Parse(foundDirectives[".line_begin"][0]);
                uint lineEnd = uint.Parse(foundDirectives[".line_end"][0]);

                debugInfo = new HksFunctionDebugInfo((uint)lineNumbers.Count, (uint)locals.Count, (uint)upvalues.Count, lineBegin, lineEnd, path, functionName, lineNumbers, locals, upvalues);
            }

            return new HksFunctionBlock(upvalueCount, paramCount, isVararg, slotCount, 0, (uint)instructions.Count, instructions, (uint)constants.Count, constants, hasDebugInfo, debugInfo, functionCount);
        }

        private HksInstruction ParseInstruction(List<string> line, ref List<int> lineNumbers)
        {
            if (line[0] == "[")
            {
                lineNumbers.Add(int.Parse(line[1]));
                HksAssemblyException.Assert(line[2] == "]", "malformed line number");
                line = line.Skip(3).ToList();
            }
            else
            {
                lineNumbers.Add(lineNumbers.Count > 0 ? lineNumbers.Last() : 0);
            }

            HksOpCode opCode = (HksOpCode)Enum.Parse(typeof(HksOpCode), line[0]);
            var args = new List<HksOpArg>();
            foreach (List<string> argLex in SplitList(line.Skip(1).ToList(), ","))
            {
                if (argLex.Count == 3 && argLex[0] == "R(" && argLex[2] == ")")
                {
                    args.Add(new HksOpArg(HksOpArgMode.REG, int.Parse(argLex[1])));
                }
                else if (argLex.Count == 3 && argLex[0] == "K(" && argLex[2] == ")")
                {
                    args.Add(new HksOpArg(HksOpArgMode.CONST, int.Parse(argLex[1])));
                }
                else if (argLex.Count == 1)
                {
                    args.Add(new HksOpArg(HksOpArgMode.NUMBER, int.Parse(argLex[0])));
                }
                else
                {
                    throw new HksAssemblyException("malformed instruction argument");
                }
            }
            return new HksInstruction(opCode, args);
        }

        private List<List<string>> SplitList(List<string> list, string delimiter)
        {
            var result = new List<List<string>>();
            var group = new List<string>();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == delimiter)
                {
                    result.Add(group);
                    group = new List<string>();
                }
                else
                {
                    group.Add(list[i]);
                }
            }
            if (list.Count != 0)
            {
                result.Add(group);
            }
            return result;
        }

        // just removes commas when there are multiple arguments
        private List<string> ParseDirective(List<string> line)
        {
            var result = new List<string>();
            result.Add(line[0]);
            for (int i = 1; i < line.Count; i++)
            {
                if (i % 2 != 0)
                {
                    result.Add(line[i]);
                }
                else
                {
                    HksAssemblyException.Assert(line[i] == ",", "malformed directive argument list");
                }
            }
            return result;
        }

        private bool ParseString(string instring, out string outstring)
        {
            if ((instring.StartsWith('"') && instring.EndsWith('"')) || (instring.StartsWith('\'') && instring.EndsWith('\'')))
            {
                outstring = instring[1..(instring.Length - 1)];
                return true;
            }
            else
            {
                outstring = "";
                return false;
            }
        }

        private HksValue ParseValue(string str)
        {
            HksType type;
            object? value;
            if (ParseString(str, out string stringResult))
            {
                type = HksType.TSTRING;
                value = stringResult;
            }
            else if (bool.TryParse(str, out bool boolResult))
            {
                type = HksType.TBOOLEAN;
                value = boolResult;
            }
            else if (str == "nil")
            {
                type = HksType.TNIL;
                value = null;
            }
            else if (globalHeader!.NumberSize == 4 && globalHeader!.NumberType == HksNumberType.INTEGER && int.TryParse(str, out int intResult))
            {
                type = HksType.TNUMBER;
                value = intResult;
            }
            else if (globalHeader!.NumberSize == 8 && globalHeader!.NumberType == HksNumberType.INTEGER && long.TryParse(str, out long longResult))
            {
                type = HksType.TNUMBER;
                value = longResult;
            }
            else if (globalHeader!.NumberSize == 4 && globalHeader!.NumberType == HksNumberType.FLOAT && float.TryParse(str, out float floatResult))
            {
                type = HksType.TNUMBER;
                value = floatResult;
            }
            else if (globalHeader!.NumberSize == 8 && globalHeader!.NumberType == HksNumberType.FLOAT && double.TryParse(str, out double doubleResult))
            {
                type = HksType.TNUMBER;
                value = doubleResult;
            }
            else if (str.StartsWith(HksType.TLIGHTUSERDATA.ToString() + "(") && str.EndsWith(")"))
            {
                type = HksType.TLIGHTUSERDATA;
                int start = HksType.TLIGHTUSERDATA.ToString().Length + 1;
                int end = str.Length - 1;
                value = long.Parse(str[start..end]);
            }
            else
            {
                throw new HksAssemblyException("failed to parse value: " + str);
            }

            return new HksValue(type, value);
        }

        private HksStructBlock ParseStruct(List<List<string>> lex, ref int cursor)
        {
            List<string> structDirective = ParseDirective(lex[cursor++]);
            HksAssemblyException.Assert(structDirective[0] == ".struct", "malformed struct");
            string structName = structDirective[1];
            uint structId = uint.Parse(structDirective[2]);

            List<string>? extendList = null;
            int? extendCount = null;
            if (globalHeader!.Flags == 3)
            {
                extendList = new List<string>();
                while (lex[cursor][0] == ".extends")
                {
                    extendList.Add(lex[cursor++][1]);
                }
                extendCount = extendList.Count;
            }

            var memberList = new List<HksStructMember>();
            int index = 0;
            while (cursor < lex.Count && lex[cursor][0] == ".member")
            {
                List<string> memberDirective = ParseDirective(lex[cursor++]);
                string memberName = memberDirective[1];
                HksType memberType = (HksType)Enum.Parse(typeof(HksType), memberDirective[2]);
                uint memberStructId = 0xffff;
                if (memberType == HksType.TSTRUCT)
                {
                    memberStructId = uint.Parse(memberDirective[3]);
                }

                int unk = 0;
                if (memberName == "__testdummy")
                {
                    memberStructId = 0;
                    unk = 3;

                }
                else if (memberName == "__structmeta")
                {
                    memberStructId = 0;
                    unk = 1;
                }
                else if (memberName == "__structbacking")
                {
                    unk = 2;
                }

                var header = new HksStructHeader(memberName, 0, memberStructId, memberType, unk, 0);
                memberList.Add(new HksStructMember(header, index));

                index++;
                if (index == 1 || index % 8 == 0)
                {
                    index++;
                }
            }
            HksStructHeader structHeader = new HksStructHeader(structName, 0, structId, 0, 0, 0);
            return new HksStructBlock(structHeader, memberList.Count, extendCount, extendList, memberList);
        }

        private HksTypeEnum GetTypeEnum()
        {
            var typeEnumEntries = new List<HksTypeEnumEntry>();
            foreach (HksType type in Enum.GetValues(typeof(HksType)))
            {
                typeEnumEntries.Add(new HksTypeEnumEntry((int)type, type.ToString()));
            }
            return new HksTypeEnum((uint)typeEnumEntries.Count, typeEnumEntries);
        }
    }

    public class HksAssemblyException : Exception
    {
        public HksAssemblyException() { }
        public HksAssemblyException(string message) : base(message) { }
        public HksAssemblyException(string message, Exception innerException) : base(message, innerException) { }
        public static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new HksAssemblyException(message);
            }
        }
    }
}
