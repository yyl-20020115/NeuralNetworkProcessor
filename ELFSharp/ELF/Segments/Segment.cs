using System;
using System.IO;
using ELFSharp.Utilities;

namespace ELFSharp.ELF.Segments;
public class Segment<T> : ISegment
{
    private long headerOffset;
    private ElfClass elfClass;
    private SimpleEndianessAwareReader reader;
    public SegmentType Type { get; private set; }
    public SegmentFlags Flags { get; private set; }
    public T Address { get; private set; }
    public T PhysicalAddress { get; private set; }
    public T Size { get; private set; }
    public T Alignment { get; private set; }
    public long FileSize { get; private set; }
    public long Offset { get; private set; }
    internal Segment(long headerOffset, ElfClass elfClass, SimpleEndianessAwareReader reader)
    {
        this.reader = reader;
        this.headerOffset = headerOffset;
        this.elfClass = elfClass;
        ReadHeader();
    }

    public static SegmentType ProbeType(SimpleEndianessAwareReader reader) => (SegmentType)reader.ReadUInt32();

    /// <summary>
    /// Returns content of the section as it is given in the file.
    /// Note that it may be an array of length 0.
    /// </summary>
    /// <returns>Segment contents as byte array.</returns>
    public byte[] GetFileContents()
    {
        if (FileSize == 0) return Array.Empty<byte>();
        SeekTo(Offset);
        var result = new byte[checked((int)FileSize)];
        var fileImage = reader.ReadBytes(result.Length);
        fileImage.CopyTo(result, 0);
        return result;
    }

    /// <summary>
    /// Returns content of the section, possibly padded or truncated to the memory size.
    /// Note that it may be an array of length 0.
    /// </summary>
    /// <returns>Segment image as a byte array.</returns>
    public byte[] GetMemoryContents()
    {
        var sizeAsInt = Size.To<int>();
        if (sizeAsInt == 0) return Array.Empty<byte>();
        SeekTo(Offset);
        var result = new byte[sizeAsInt];
        var fileImage = reader.ReadBytes(Math.Min(result.Length, checked((int)FileSize)));
        fileImage.CopyTo(result, 0);
        return result;
    }

    public byte[] GetRawHeader()
    {
        SeekTo(headerOffset);
        return reader.ReadBytes(elfClass == ElfClass.Bit32 ? 32 : 56);
    }

    public override string ToString() => string.Format("{2}: size {3}, @ 0x{0:X}", Address, PhysicalAddress, Type, Size);

    private void ReadHeader()
    {
        SeekTo(headerOffset);
        Type = (SegmentType)reader.ReadUInt32();
        if (elfClass == ElfClass.Bit64) Flags = (SegmentFlags)reader.ReadUInt32();
        // TODO: some functions?
        Offset = elfClass == ElfClass.Bit32 ? reader.ReadUInt32() : reader.ReadInt64();
        Address = (elfClass == ElfClass.Bit32 ? reader.ReadUInt32() : reader.ReadUInt64()).To<T>();
        PhysicalAddress = (elfClass == ElfClass.Bit32 ? reader.ReadUInt32() : reader.ReadUInt64()).To<T>();
        FileSize = elfClass == ElfClass.Bit32 ? reader.ReadInt32() : reader.ReadInt64();
        Size = (elfClass == ElfClass.Bit32 ? reader.ReadUInt32() : reader.ReadUInt64()).To<T>();
        if (elfClass == ElfClass.Bit32) Flags = (SegmentFlags)reader.ReadUInt32();
        Alignment = (elfClass == ElfClass.Bit32 ? reader.ReadUInt32() : reader.ReadUInt64()).To<T>();
    }

    private void SeekTo(long givenOffset) => reader.BaseStream.Seek(givenOffset, SeekOrigin.Begin);
}
