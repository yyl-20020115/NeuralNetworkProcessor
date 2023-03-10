using System;
using System.IO;
using System.Text;
using ELFSharp.Utilities;

namespace ELFSharp.ELF.Sections;

internal class NoteData
{
    private readonly SimpleEndianessAwareReader reader;
    internal string Name { get; private set; }
    internal byte[] Description { get; private set; }
    internal ulong Type { get; private set; }
    private int FieldSize => sizeof(uint);
    internal NoteData(ulong sectionOffset, ulong sectionSize, SimpleEndianessAwareReader reader)
    {
        this.reader = reader;
        var sectionEnd = (long)(sectionOffset + sectionSize);
        reader.BaseStream.Seek((long)sectionOffset, SeekOrigin.Begin);
        var nameSize = ReadSize();
        var descriptionSize = ReadSize();
        Type = ReadField();
        int remainder;
        var fields = Math.DivRem(nameSize, FieldSize, out remainder);
        var alignedNameSize = FieldSize * (remainder > 0 ? fields + 1 : fields);

        // We encountered binaries where nameSize and descriptionSize are
        // invalid (i.e. significantly larger than the size of the binary itself).
        // To avoid throwing on such binaries, we only read in name and description
        // if the sizes are within range of the containing section.
        if (reader.BaseStream.Position + alignedNameSize <= sectionEnd)
        {
            var name = reader.ReadBytes(alignedNameSize);
            if (nameSize > 0)
            {
                Name = Encoding.UTF8.GetString(name, 0, nameSize - 1); // minus one to omit terminating NUL
            }
            if (reader.BaseStream.Position + descriptionSize <= sectionEnd)
            {
                Description = descriptionSize > 0 ? reader.ReadBytes(descriptionSize) : new byte[0];
            }
        }
    }

    /*
    * According to some versions of ELF64 specfication, in 64-bit ELF files words, of which
    * such section consists, should have 8 byte length. However, this is not the case in
    * some other specifications (some of theme contradicts with themselves like the 64bit MIPS
    * one). In real life scenarios I also observed that note sections are identical in both
    * ELF classes. There is also only one structure (i.e. Elf_External_Note) in existing and
    * well tested GNU tools.
    *
    * Nevertheless I leave here the whole machinery as it is already written and may be useful
    * some day.
    */
    private int ReadSize() => reader.ReadInt32();
    private ulong ReadField() => reader.ReadUInt32();
}
