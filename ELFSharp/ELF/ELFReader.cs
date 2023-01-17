using System;
using System.IO;
using System.Text;

namespace ELFSharp.ELF;
public static class ELFReader
{
	private static readonly byte[] Magic =
	{
		0x7F,
		0x45,
		0x4C,
		0x46
	}; // 0x7F 'E' 'L' 'F'

	private const string NotELFMessage = "Given stream is not a proper ELF file.";

	public static IELF Load(Stream stream, bool shouldOwnStream = true)
	{
		if (!TryLoad(stream, out IELF elf, shouldOwnStream))
			throw new ArgumentException(NotELFMessage);

		return elf;
	}

    public static IELF Load(string fileName) => Load(File.OpenRead(fileName), true);

    public static bool TryLoad(Stream stream, out IELF elf, bool shouldOwnStream = true)
	{
		switch (CheckELFType(stream))
		{
			case ElfClass.Bit32:
				elf = new ELF<uint>(stream, shouldOwnStream);
				return true;
			case ElfClass.Bit64:
				elf = new ELF<ulong>(stream, shouldOwnStream);
				return true;
			default:
				elf = null;
				return false;
		}
	}

    public static bool TryLoad(string fileName, out IELF elf) 
		=> TryLoad(File.OpenRead(fileName), out elf, true);

	public static ElfClass CheckELFType(Stream stream)
	{
		var currentStreamPosition = stream.Position;
		if (stream.Length < Consts.MinimalELFSize) return ElfClass.NotELF;
		using var reader = new BinaryReader(stream, Encoding.Latin1, true);
		var magic = reader.ReadBytes(4);
		for (var i = 0; i < 4; i++)
			if (magic[i] != Magic[i]) return ElfClass.NotELF;
		var value = reader.ReadByte();
		stream.Position = currentStreamPosition;
		return value == 1 ? ElfClass.Bit32 : value==2 ? ElfClass.Bit64 : ElfClass.NotELF;
	}

	public static ElfClass CheckELFType(string fileName)
	{
		using var stream = File.OpenRead(fileName);
		return CheckELFType(stream);
	}

	public static ELF<T> Load<T>(Stream stream, bool shouldOwnStream = true) where T : struct
	{
		if (CheckELFType(stream) == ElfClass.NotELF) throw new ArgumentException(NotELFMessage);
		return new (stream, shouldOwnStream);
	}

    public static ELF<T> Load<T>(string fileName) where T : struct 
		=> Load<T>(File.OpenRead(fileName), true);

    public static bool TryLoad<T>(Stream stream, out ELF<T> elf, bool shouldOwnStream = true) where T : struct
	{
		switch (CheckELFType(stream))
		{
			case ElfClass.Bit32:
			case ElfClass.Bit64:
				elf = new ELF<T>(stream, shouldOwnStream);
				return true;
			default:
				elf = null;
				return false;
		}
	}

    public static bool TryLoad<T>(string fileName, out ELF<T> elf) where T : struct 
		=> TryLoad(File.OpenRead(fileName), out elf, true);

}