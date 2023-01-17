namespace ELFSharp.MachO;
public enum MachOResult : uint
{
    OK = 0,
    NotMachO = 1,
    FatMachO = 2,
}
