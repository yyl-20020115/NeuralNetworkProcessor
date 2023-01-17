using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SharpLua
{
    public struct LuaFileHeader
    {
        public const int HeaderSize = 12;

        public const int SignatureBytes = 0x1B4C7561;
        public const byte Lua51Version = 0x51;

        public string signature;        // should be ".Lua" or SignatureBytes
        public byte version;            // 0x51 (81) for Lua 5.1
        public byte format;             // 0 for official Lua version
        public bool isLittleEndian;
        public byte intSize;            // in bytes. default 4
        public byte size_tSize;         // in bytes. default 4
        public byte instructionSize;    // in bytes. default 4
        public byte lua_NumberSize;     // in bytes. default 8
        public bool isIntegral;         // true = integral number type, false = floating point

        // default header bytes on x86:
        // 1B4C7561 51000104 04040800

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.AppendFormat("signature:       {0}\n", this.signature);
            builder.AppendFormat("version:         {0}\n", this.version);
            builder.AppendFormat("format:          {0}\n", this.format);
            builder.AppendFormat("isLittleEndian:  {0}\n", this.isLittleEndian);
            builder.AppendFormat("intSize:         {0}\n", this.intSize);
            builder.AppendFormat("size_tSize:      {0}\n", this.size_tSize);
            builder.AppendFormat("instructionSize: {0}\n", this.instructionSize);
            builder.AppendFormat("lua_NumberSize:  {0}\n", this.lua_NumberSize);
            builder.AppendFormat("isIntegral:      {0}\n", this.isIntegral);

            return builder.ToString();
        }
    }

    public class LuaFileReader : IDisposable
    {
        private string file = null;
        private FileStream stream = null;
        private BinaryReader reader = null;
        private LuaFileHeader header = default;
        public LuaFileHeader Header => header;

        public LuaFileReader() { }
 
        public Function ReadNextFunction() 
            => (this.stream.Length - this.stream.Position - 1) <= 0 ? null : new()
        {
            sourceName = this.ReadString(),
            lineNumber = this.ReadInteger(this.header.intSize),
            lastLineNumber = this.ReadInteger(this.header.intSize),
            numUpvalues = this.reader.ReadByte(),
            numParameters = this.reader.ReadByte(),
            varArgFlag = (Function.VarArg)this.reader.ReadByte(),
            maxStackSize = this.reader.ReadByte(),
            instructions = this.ReadInstructions(),
            constants = this.ReadConstants(),
            functions = this.ReadFunctions(),
            sourceLinePositions = this.ReadLineNumbers(),
            locals = this.ReadLocals(),
            upvalues = this.ReadUpvalues()
        };

        public void Dispose()
        {
            this.reader?.Dispose();
            this.stream?.Dispose();
            this.reader = null;
            this.stream = null;
        }
        public LuaFileReader WithInit(string file)
        {
            this.Init(file);
            return this;
        }
        public bool Init(string file)
        {
            try
            {
                this.stream = new FileStream(this.file = file, FileMode.Open);
                this.reader = new BinaryReader(this.stream, Encoding.ASCII);
                this.header = new LuaFileHeader();
                var bytes = this.reader.ReadBytes(12);
                char[] sig = { (char)bytes[0], (char)bytes[1], (char)bytes[2], (char)bytes[3] };
                this.header.signature = new string(sig);
                if (this.header.signature != (char)27 + "Lua")
                    throw new InvalidDataException("File does not appear to be a Lua bytecode file.");

                this.header.version = bytes[4];
                if (this.header.version != LuaFileHeader.Lua51Version)
                    throw new NotImplementedException("Only Lua 5.1 is supported.");
                this.header.format = bytes[5];
                this.header.isLittleEndian = bytes[6] != 0;
                this.header.intSize = bytes[7];
                this.header.size_tSize = bytes[8];
                this.header.instructionSize = bytes[9];
                this.header.lua_NumberSize = bytes[10];
                this.header.isIntegral = bytes[11] != 0;
                return true;
            }
            catch (FileNotFoundException)
            {
                //Console.WriteLine("File " + file + " does not exist: " + fnfe);
                return false;
            }
        }

        private List<LvmInstruction> ReadInstructions()
        {
            var numInstrs = ReadInteger(header.intSize);
            var instrs = new List<LvmInstruction>(numInstrs);
            for (var i = 0; i < numInstrs; ++i)
                instrs.Add(new LvmInstruction(
                    ReadInteger(header.instructionSize)));
            return instrs;
        }

        private List<Constant> ReadConstants()
        {
            var numConsts = ReadInteger(this.header.intSize);
            var consts = new List<Constant>(numConsts);
            for (var i = 0; i < numConsts; ++i)
            {
                var type = (LuaType)this.reader.ReadByte();
                switch (type)
                {
                    case LuaType.Nil:
                        consts.Add(new NilConstant());
                        break;
                    case LuaType.Bool:
                        consts.Add(new BoolConstant(this.reader.ReadBoolean()));
                        break;
                    case LuaType.Number:
                        consts.Add(new NumberConstant(this.ReadNumber(this.header.lua_NumberSize)));
                        break;
                    case LuaType.String:
                        consts.Add(new StringConstant(this.ReadString()));
                        break;
                    default:
                        break;
                }
            }

            return consts;
        }

        private List<Function> ReadFunctions()
        {
            var numFuncs = this.ReadInteger(header.intSize);
            var funcs = new List<Function>(numFuncs);
            for (var i = 0; i < numFuncs; ++i)
                funcs.Add(this.ReadNextFunction());
            return funcs;
        }

        private List<int> ReadLineNumbers()
        {
            var numLinePos = this.ReadInteger(this.header.intSize);
            var linePos = new List<int>(numLinePos);
            for (var i = 0; i < numLinePos; ++i)
                // subtract 1 to index from 0, not 1
                linePos.Add(this.ReadInteger(this.header.intSize) - 1);
            return linePos;
        }

        private List<Local> ReadLocals()
        {
            var numLocals = this.ReadInteger(header.intSize);
            var locals = new List<Local>(numLocals);
            for (var i = 0; i < numLocals; ++i)
                locals.Add(new Local(this.ReadString(), 
                    this.ReadInteger(this.header.intSize), this.ReadInteger(this.header.intSize)));
            return locals;
        }

        private List<string> ReadUpvalues()
        {
            var numUpvalues = this.ReadInteger(this.header.intSize);
            var upvalues = new List<string>(numUpvalues);
            for (var i = 0; i < numUpvalues; ++i)
                upvalues.Add(this.ReadString());
            return upvalues;
        }

        private string ReadString() 
            => Encoding.ASCII.GetString(
                this.reader.ReadBytes(this.ReadInteger(this.header.size_tSize)));

        private int ReadInteger(byte intSize)
        {
            var bytes = this.reader.ReadBytes(intSize);
            var ret = 0;
            if (this.header.isLittleEndian)
                for (var i = 0; i < intSize; ++i)
                    ret += (this.header.isLittleEndian) 
                        ? (bytes[i] << (i * 8))
                        : (bytes[i] >> (i * 8)) 
                        ;
            return ret;
        }

        private double ReadNumber(byte numSize)
        {
            var bytes = reader.ReadBytes(numSize);
            var value = 0.0;
            if (numSize == 8)
            {
                value = BitConverter.ToDouble(bytes, 0);
            }
            else if (numSize == 4)
            {
                value = BitConverter.ToSingle(bytes, 0);
            }
            else
            {
                //throw new NotImplementedException("Uhm...");
                value = 0.0;
            }

            return value;
        }
    }
}
