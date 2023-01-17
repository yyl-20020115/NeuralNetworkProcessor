using System.IO;
using System.Collections.Generic;
using System.Linq;
using ELFSharp.Utilities;

namespace ELFSharp.MachO;
public sealed class MachO
{
    private bool is64;
    private Command[] commands;

    internal const int Architecture64 = 0x1000000;
    public Machine Machine { get; private set; }
    public FileType FileType { get; private set; }
    public HeaderFlags Flags { get; private set; }
    public bool Is64 => is64;

    internal MachO(Stream stream, bool is64, Endianess endianess, bool ownsStream)
    {
        this.is64 = is64;

        using var reader = new SimpleEndianessAwareReader(stream, endianess, ownsStream);

        Machine = (Machine)reader.ReadInt32();
        reader.ReadBytes(4); // we don't support the cpu subtype now
        FileType = (FileType)reader.ReadUInt32();
        var noOfCommands = reader.ReadInt32();
        reader.ReadInt32(); // size of commands
        Flags = (HeaderFlags)reader.ReadUInt32();
        if (is64) reader.ReadBytes(4); // reserved
        commands = new Command[noOfCommands];
        ReadCommands(noOfCommands, stream, reader);
    }

    public IEnumerable<T> GetCommandsOfType<T>() => commands.Where(x => x != null).OfType<T>();

    private void ReadCommands(int noOfCommands, Stream stream, SimpleEndianessAwareReader reader)
    {
        for (var i = 0; i < noOfCommands; i++)
        {
            var loadCommandType = reader.ReadUInt32();
            var commandSize = reader.ReadUInt32();
            switch ((CommandType)loadCommandType)
            {
                case CommandType.SymbolTable:
                    commands[i] = new SymbolTable(reader, stream, is64);
                    break;
                case CommandType.IdDylib:
                    commands[i] = new IdDylib(reader, stream, commandSize);
                    break;
                case CommandType.LoadDylib:
                    commands[i] = new LoadDylib(reader, stream, commandSize);
                    break;
                case CommandType.LoadWeakDylib:
                    commands[i] = new LoadWeakDylib(reader, stream, commandSize);
                    break;
                case CommandType.ReexportDylib:
                    commands[i] = new ReexportDylib(reader, stream, commandSize);
                    break;
                case CommandType.Main:
                    commands[i] = new EntryPoint(reader, stream);
                    break;
                case CommandType.Segment:
                case CommandType.Segment64:
                    commands[i] = new Segment(reader, stream, this);
                    break;
                default:
                    reader.ReadBytes((int)commandSize - 8); // 8 bytes is the size of the common command header
                    break;
            }
        }
    }
}
