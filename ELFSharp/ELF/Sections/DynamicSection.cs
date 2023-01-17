using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ELFSharp.Utilities;

namespace ELFSharp.ELF.Sections;
public sealed class DynamicSection<T> : Section<T>, IDynamicSection where T : struct
{
    private List<DynamicEntry<T>> entries;
    private readonly ELF<T> elf;
    public IEnumerable<DynamicEntry<T>> Entries => new ReadOnlyCollection<DynamicEntry<T>>(entries);
    IEnumerable<IDynamicEntry> IDynamicSection.Entries => entries.Cast<IDynamicEntry>();
    internal DynamicSection(SectionHeader header, SimpleEndianessAwareReader reader, ELF<T> elf) : base(header, reader)
    {
        this.elf = elf;
        this.ReadEntries();
    }

    public override string ToString() => string.Format("{0}: {2}, load @0x{4:X}, {5} entries", Name, NameIndex, Type, RawFlags, LoadAddress, Entries.Count());

    private void ReadEntries()
    {
        /// "Kind-of" Bug:
        /// So, this winds up with "extra" DT_NULL entries for some executables.  The issue
        /// is basically that sometimes the .dynamic section's size (and # of entries) per the 
        /// header is higher than the actual # of entries.  The extra space gets filled with null
        /// entries in all of the ELF files I tested, so we shouldn't end up with any 'incorrect' entries 
        /// here unless someone is messing with the ELF structure.

        SeekToSectionBeginning();
        var entryCount = elf._ElfClass == ElfClass.Bit32 ? Header.Size / 8 : Header.Size / 16;
        entries = new List<DynamicEntry<T>>();
        for (ulong i = 0; i < entryCount; i++)
        {
            if (elf._ElfClass == ElfClass.Bit32)
            {
                entries.Add(new(Reader.ReadUInt32().To<T>(), Reader.ReadUInt32().To<T>()));
            }
            else if (elf._ElfClass == ElfClass.Bit64)
            {
                entries.Add(new(Reader.ReadUInt64().To<T>(), Reader.ReadUInt64().To<T>()));
            }
        }
    }
}
