using ELFSharp.ELF.Sections;
using ELFSharp.Utilities;

namespace ELFSharp.ELF.Segments;
public sealed class NoteSegment<T> : Segment<T>, INoteSegment
{
    public string NoteName => data.Name;
    public ulong NoteType => data.Type;
    public byte[] NoteDescription => data.Description;

    private readonly NoteData data;
    internal NoteSegment(long headerOffset, ElfClass elfClass, SimpleEndianessAwareReader reader)
        : base(headerOffset, elfClass, reader)
    {
        data = new NoteData((ulong)base.Offset, (ulong)base.FileSize, reader);
    }
}