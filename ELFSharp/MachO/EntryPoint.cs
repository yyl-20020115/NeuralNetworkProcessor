using System.IO;
using ELFSharp.Utilities;

namespace ELFSharp.MachO;
public class EntryPoint : Command
{
    public long Value { get; private set; }
    public long StackSize { get; private set; }
    public EntryPoint(SimpleEndianessAwareReader reader, Stream stream) : base(reader, stream)
    {
        Value = Reader.ReadInt64();
        StackSize = Reader.ReadInt64();
    }
}
