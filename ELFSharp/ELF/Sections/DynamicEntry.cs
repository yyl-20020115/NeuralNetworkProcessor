namespace ELFSharp.ELF.Sections;
/// <summary>
/// Dynamic table entries are made up of a 32 bit or 64 bit "tag"
/// and a 32 bit or 64 bit union (val/pointer in 64 bit, val/pointer/offset in 32 bit).
/// 
/// See LLVM elf.h file for the C/C++ version.
/// </summary>
public class DynamicEntry<T> : IDynamicEntry
{
    public DynamicTag Tag { get; private set; }
    public T Value { get; private set; }
    public DynamicEntry(T tagValue, T value)
    {
        this.Tag = (DynamicTag)tagValue.To<ulong>();
        this.Value = value;
    }

    public override string ToString() => string.Format("{0} \t 0x{1:X}", Tag, Value);
}