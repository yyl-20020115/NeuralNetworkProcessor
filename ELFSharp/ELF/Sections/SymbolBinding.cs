namespace ELFSharp.ELF.Sections;
public enum SymbolBinding : uint
{
	Local = 0,
	Global,
	Weak,
	ProcessorSpecific
}