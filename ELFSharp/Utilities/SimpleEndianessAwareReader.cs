using System;
using System.IO;
using System.Net;

namespace ELFSharp.Utilities;
public sealed class SimpleEndianessAwareReader : IDisposable
{
    public Stream BaseStream => stream;

    private bool needsAdjusting;
    private bool beNonClosing;
    private Stream stream;

    public SimpleEndianessAwareReader(Stream stream, Endianess endianess, bool beNonClosing = false)
    {
        this.beNonClosing = beNonClosing;
        this.stream = stream;
        this.needsAdjusting = endianess == Endianess.LittleEndian ^ BitConverter.IsLittleEndian;
    }

    public void Dispose()
    {
        if (beNonClosing)
            return;
        else if (this.stream != null)
        {
            this.stream.Dispose();
            this.stream = null;
        }
    }

    public byte[] ReadBytes(int count) 
        => stream.ReadBytesOrThrow(count);

    public byte ReadByte()
    {
        var result = stream.ReadByte();
        if (result == -1) throw new EndOfStreamException("End of stream reached while trying to read one byte.");
        return (byte)result;
    }

    public short ReadInt16()
        => this.needsAdjusting
        ? IPAddress.NetworkToHostOrder(BitConverter.ToInt16(ReadBytes(sizeof(short)), 0))
        : BitConverter.ToInt16(ReadBytes(sizeof(short)), 0)
        ;

    public ushort ReadUInt16() => (ushort)ReadInt16();

    public int ReadInt32()
        => this.needsAdjusting
        ? IPAddress.NetworkToHostOrder(BitConverter.ToInt32(ReadBytes(sizeof(int)), 0))
        : BitConverter.ToInt32(ReadBytes(sizeof(int)), 0)
        ;

    public uint ReadUInt32() => (uint)ReadInt32();

    public long ReadInt64()
        => this.needsAdjusting
        ? IPAddress.NetworkToHostOrder(BitConverter.ToInt64(ReadBytes(sizeof(long)), 0))
        : BitConverter.ToInt64(ReadBytes(sizeof(long)), 0)
        ;

    public ulong ReadUInt64() => (ulong)ReadInt64();
}
