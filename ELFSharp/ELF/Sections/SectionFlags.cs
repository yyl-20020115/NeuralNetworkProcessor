using System;

namespace ELFSharp.ELF.Sections;

[Flags]
public enum SectionFlags : uint
{
    Writable = 1,
    Allocatable = 2,
    Executable = 4
}
