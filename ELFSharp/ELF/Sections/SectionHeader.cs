using ELFSharp.Utilities;

namespace ELFSharp.ELF.Sections;
internal sealed class SectionHeader
{
    private SimpleEndianessAwareReader reader;
    private IStringTable table;
    private ElfClass elfClass;
    internal string Name { get; private set; }
    internal uint NameIndex { get; private set; }
    internal SectionType Type { get; private set; }
    internal SectionFlags Flags { get; private set; }
    internal ulong RawFlags { get; private set; }
    internal ulong LoadAddress { get; private set; }
    internal ulong Alignment { get; private set; }
    internal ulong EntrySize { get; private set; }
    internal ulong Size { get; private set; }
    internal ulong Offset { get; private set; }
    internal uint Link { get; private set; }
    internal uint Info { get; private set; }

    // TODO: make elf consts file with things like SHT_LOUSER
    internal SectionHeader(SimpleEndianessAwareReader reader, ElfClass elfClass, IStringTable table = null)
    {
        this.reader = reader;
        this.table = table;
        this.elfClass = elfClass;
        ReadSectionHeader();
    }

    public override string ToString()
        => string.Format("{0}: {2}, load @0x{4:X}, {5} bytes long", Name, NameIndex, Type, RawFlags, LoadAddress, Size);

    private void ReadSectionHeader()
    {
        NameIndex = reader.ReadUInt32();
        if (table != null) Name = table[NameIndex];
        Type = (SectionType)reader.ReadUInt32();
        RawFlags = ReadAddress();
        Flags = unchecked((SectionFlags)RawFlags);
        LoadAddress = ReadAddress();
        Offset = ReadOffset();
        Size = ReadOffset();
        Link = reader.ReadUInt32();
        Info = reader.ReadUInt32();
        Alignment = ReadAddress();
        EntrySize = ReadAddress();
    }

    private ulong ReadAddress() => elfClass == ElfClass.Bit32 ? reader.ReadUInt32() : reader.ReadUInt64();
    private ulong ReadOffset() => elfClass == ElfClass.Bit32 ? reader.ReadUInt32() : reader.ReadUInt64();
}