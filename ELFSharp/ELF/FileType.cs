namespace ELFSharp.ELF;
public enum FileType : ushort
{
	None = 0,
	Relocatable = 1,
	Executable = 2,
	SharedObject = 3,
	Core = 4,
}
