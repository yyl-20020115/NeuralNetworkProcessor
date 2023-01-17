﻿using System;
using System.Diagnostics;

namespace ELFSharp.MachO;
[DebuggerDisplay("Section({segment.Name,nq},{Name,nq})")]
public sealed class Section
{
    private readonly Segment segment;
    public string Name { get; private set; }
    public string SegmentName { get; private set; }
    public ulong Address { get; private set; }
    public ulong Size { get; private set; }
    public uint Offset { get; private set; }
    public uint AlignExponent { get; private set; }
    public uint RelocOffset { get; private set; }
    public uint RelocCount { get; private set; }
    public uint Flags { get; private set; }

    public Section(string name, string segmentName, ulong address, ulong size, uint offset, uint alignExponent, uint relocOffset, uint numberOfReloc, uint flags, Segment segment)
    {
        Name = name;
        SegmentName = segmentName;
        Address = address;
        Size = size;
        Offset = offset;
        AlignExponent = alignExponent;
        RelocOffset = relocOffset;
        RelocCount = numberOfReloc;
        Flags = flags;
        this.segment = segment;
    }

    public byte[] GetData()
    {
        if (Offset < segment.FileOffset || Offset + Size > segment.FileOffset + segment.Size)
            return Array.Empty<byte>();
        var result = new byte[Size];
        Array.Copy(segment.GetData(), (int)(Offset - segment.FileOffset), result, 0, (int)Size);
        return result;
    }

}