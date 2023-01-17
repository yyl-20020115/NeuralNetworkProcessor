using ELFSharp.Utilities;

namespace ELFSharp.ELF.Sections;
public sealed class NoteSection<T> : Section<T>, INoteSection where T : struct
{
    private NoteData data;
    public string NoteName => data.Name;
    public byte[] Description => data.Description;
    public T NoteType=> data.Type.To<T>();
    internal NoteSection(SectionHeader header, SimpleEndianessAwareReader reader) 
        : base(header, reader)
    {
        data = new (header.Offset, header.Size, reader);
    }
    public override string ToString() => string.Format("{0}: {2}, Type={1}", Name, NoteType, Type);
}
