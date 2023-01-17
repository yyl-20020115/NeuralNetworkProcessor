namespace ELFSharp.ELF.Sections;
public enum SpecialSectionType : uint
{
	Null = 0,
	ProgBits,
	NoBits,
	Shlib,
	ProcessorSpecific,
	UserSpecific
}