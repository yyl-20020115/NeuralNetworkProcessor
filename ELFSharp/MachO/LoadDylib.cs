using System.IO;
using ELFSharp.Utilities;

namespace ELFSharp.MachO;
public class LoadDylib : Dylib
{
    public LoadDylib(SimpleEndianessAwareReader reader, Stream stream, uint commandSize) 
        : base(reader, stream, commandSize)
    {
    }
}