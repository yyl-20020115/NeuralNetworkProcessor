using System.IO;
using ELFSharp.Utilities;

namespace ELFSharp.MachO;
public class Command
{
    protected SimpleEndianessAwareReader Reader;
    protected Stream Stream;
    internal Command(SimpleEndianessAwareReader reader, Stream stream)
    {
        Stream = stream;
        Reader = reader;
    }
}

