namespace ELFSharp.ELF.Sections;
public enum SymbolType : uint
{
	NotSpecified = 0,
	Object,
	Function,
	Section,
	File,
	ProcessorSpecific
}