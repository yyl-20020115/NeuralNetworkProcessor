using System;
using System.IO;
using System.Text;
using ELFSharp.Utilities;

namespace ELFSharp.MachO;
public abstract class Dylib : Command
{
    public string Name { get; }
    public DateTime Timestamp { get; }
    public Version CurrentVersion { get; }
    public Version CompatibilityVersion { get; }

    internal Dylib(SimpleEndianessAwareReader reader, Stream stream, uint commandSize) : base(reader, stream)
    {
        var offset = reader.ReadUInt32();
        var timestamp = reader.ReadInt32();
        var currentVersion = reader.ReadUInt32();
        var compatibilityVersion = reader.ReadUInt32();
        Timestamp = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
        CurrentVersion = GetVersion(currentVersion);
        CompatibilityVersion = GetVersion(compatibilityVersion);
        Name = GetString(reader.ReadBytes((int)(commandSize - offset)));
    }

    private static Version GetVersion(uint version) => new ((int)(version >> 16), (int)((version >> 8) & 0xff), (int)(version & 0xff));

    private static string GetString(byte[] bytes)
    {
        var nullTerminatorIndex = Array.FindIndex(bytes, e => e == '\0');
        return Encoding.ASCII.GetString(bytes, index: 0, nullTerminatorIndex >= 0 ? nullTerminatorIndex : bytes.Length);
    }
}