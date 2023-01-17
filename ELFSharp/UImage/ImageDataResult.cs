namespace ELFSharp.UImage;
public enum ImageDataResult : uint
{
	OK = 0,
	BadChecksum = 1,
	UnsupportedCompressionFormat = 2,
}
