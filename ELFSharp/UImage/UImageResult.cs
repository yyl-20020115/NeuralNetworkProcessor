namespace ELFSharp.UImage;
public enum UImageResult : uint
{
	OK = 0,
	NotUImage = 1,
	BadChecksum = 2,
	NotSupportedImageType = 3,
}

