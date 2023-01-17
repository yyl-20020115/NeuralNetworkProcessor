using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ELFSharp.ELF.Sections;
using ELFSharp.ELF.Segments;
using ELFSharp.Utilities;

namespace ELFSharp.ELF;
public sealed class ELF<T> : IELF where T : struct
{
    private const int SectionNameNotUniqueMarker = -1;

    private enum Stage : uint
    {
        Initalizing = 0,
        AfterSectionsAreRead = 1,
    }

    private enum GetSectionResult : uint
    {
        Success = 0,
        SectionNameNotUnique = 1,
        NoSectionsStringTable = 2,
        NoSuchSection = 3,
    }

    private readonly SimpleEndianessAwareReader reader;
    private readonly bool ownsStream;
    private long segmentHeaderOffset;
    private long sectionHeaderOffset;
    private ushort segmentHeaderEntrySize;
    private ushort segmentHeaderEntryCount;
    private ushort sectionHeaderEntrySize;
    private ushort sectionHeaderEntryCount;
    private ushort stringTableIndex;
    private List<Segment<T>> segments;
    private List<Section<T>> sections;
    private Dictionary<string, int> sectionIndicesByName;
    private List<SectionHeader> sectionHeaders;
    private StringTable<T> objectsStringTable;
    private StringTable<T> dynamicStringTable;
    private Stage currentStage;

    public Endianess Endianess { get; private set; }
    public ElfClass _ElfClass { get; private set; }
    public FileType Type { get; private set; }
    public Machine Machine { get; private set; }
    public T EntryPoint { get; private set; }
    public T MachineFlags { get; private set; }
    public bool HasSegmentHeader
        => segmentHeaderOffset != 0;
    public bool HasSectionHeader 
        => sectionHeaderOffset != 0;
    public bool HasSectionsStringTable 
        => stringTableIndex != 0;
    public IReadOnlyList<Segment<T>> Segments
        => segments.AsReadOnly();
    IReadOnlyList<ISegment> IELF.Segments 
        => Segments;
    public IStringTable SectionsStringTable { get; private set; }
    public IReadOnlyList<Section<T>> Sections 
        => sections.AsReadOnly();

    IEnumerable<TSectionType> IELF.GetSections<TSectionType>() 
        => Sections.Where(x => x is TSectionType).Cast<TSectionType>();

    public IEnumerable<TSection> GetSections<TSection>() where TSection : Section<T> 
        => Sections.Where(x => x is TSection).Cast<TSection>();

    IReadOnlyList<ISection> IELF.Sections 
        => Sections;

    public bool TryGetSection(string name, out Section<T> section)
        => TryGetSectionInner(name, out section) == GetSectionResult.Success;

    public Section<T> GetSection(string name)
    {
        switch (TryGetSectionInner(name, out Section<T> section))
        {
            case GetSectionResult.Success:
                return section;
            case GetSectionResult.SectionNameNotUnique:
                throw new InvalidOperationException("Given section name is not unique, order is ambigous.");
            case GetSectionResult.NoSectionsStringTable:
                throw new InvalidOperationException("Given ELF does not contain section header string table, therefore names of sections cannot be obtained.");
            case GetSectionResult.NoSuchSection:
                throw new KeyNotFoundException(string.Format("Given section {0} could not be found in the file.", name));
            default:
                throw new InvalidOperationException("Unhandled error.");
        }
    }

    bool IELF.TryGetSection(string name, out ISection section)
    {
        var result = TryGetSection(name, out var _section);
        section = _section;
        return result;
    }
    internal ELF(Stream stream, bool ownsStream)
    {
        this.ownsStream = ownsStream;
        reader = ObtainEndianessAwareReader(stream);
        ReadFields();
        ReadStringTable();
        ReadSections();
        ReadSegmentHeaders();
    }
    ISection IELF.GetSection(string name) 
        => GetSection(name);

    public Section<T> GetSection(int index)
    {
        var result = TryGetSectionInner(index, out var section);
        switch (result)
        {
            case GetSectionResult.Success:
                return section;
            case GetSectionResult.NoSuchSection:
                throw new IndexOutOfRangeException(
                    string.Format("Given section index {0} is out of range.", index));
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override string ToString() 
        => $"[ELF: Endianess={Endianess}, Class={_ElfClass}, Type={Type}, Machine={Machine}, EntryPoint=0x{EntryPoint:X}, NumberOfSections={sections.Count}, NumberOfSegments={segments.Count}]";

    bool IELF.TryGetSection(int index, out ISection section)
    {
        var result = TryGetSection(index, out var _section);
        section = _section;
        return result;
    }

    ISection IELF.GetSection(int index) 
        => GetSection(index);

    public void Dispose()
    {
        if (ownsStream) reader.BaseStream.Dispose();
    }

    private bool TryGetSection(int index, out Section<T> section) 
        => TryGetSectionInner(index, out section) == GetSectionResult.Success;

    private Section<T> GetSectionFromSectionHeader(SectionHeader header)
    {
        var returned = default(Section<T>);
        switch (header.Type)
        {
            case SectionType.Null:
                goto default;
            case SectionType.ProgBits:
                returned = new ProgBitsSection<T>(header, reader);
                break;
            case SectionType.SymbolTable:
                returned = new SymbolTable<T>(header, reader, objectsStringTable, this);
                break;
            case SectionType.StringTable:
                returned = new StringTable<T>(header, reader);
                break;
            case SectionType.RelocationAddends:
                goto default;
            case SectionType.HashTable:
                goto default;
            case SectionType.Dynamic:
                returned = new DynamicSection<T>(header, reader, this);
                break;
            case SectionType.Note:
                returned = new NoteSection<T>(header, reader);
                break;
            case SectionType.NoBits:
                goto default;
            case SectionType.Relocation:
                goto default;
            case SectionType.Shlib:
                goto default;
            case SectionType.DynamicSymbolTable:
                returned = new SymbolTable<T>(header, reader, dynamicStringTable, this);
                break;
            default:
                returned = new Section<T>(header, reader);
                break;
        }
        return returned;
    }

    private void ReadSegmentHeaders()
    {
        segments = new (segmentHeaderEntryCount);

        for (var i = 0u; i < segmentHeaderEntryCount; i++)
        {
            var seekTo = segmentHeaderOffset + i * segmentHeaderEntrySize;
            reader.BaseStream.Seek(seekTo, SeekOrigin.Begin);
            var segmentType = Segment<T>.ProbeType(reader);
            var segment = segmentType == SegmentType.Note
                ? new NoteSegment<T>(segmentHeaderOffset + i * segmentHeaderEntrySize, _ElfClass, reader)
                : new Segment<T>(segmentHeaderOffset + i * segmentHeaderEntrySize, _ElfClass, reader);
            segments.Add(segment);
        }
    }

    private void ReadSections()
    {
        sectionHeaders = new ();
        if (HasSectionsStringTable) sectionIndicesByName = new();
        for (var i = 0; i < sectionHeaderEntryCount; i++)
        {
            var header = ReadSectionHeader(i);
            sectionHeaders.Add(header);
            if (HasSectionsStringTable)
            {
                var name = header.Name;
                sectionIndicesByName[name] =
                    !sectionIndicesByName.ContainsKey(name) 
                    ? i 
                    : SectionNameNotUniqueMarker
                    ;
            }
        }
        sections = new (Enumerable.Repeat<Section<T>>(
            null,
            sectionHeaders.Count
        ));
        FindStringTables();
        for (var i = 0; i < sectionHeaders.Count; i++) TouchSection(i);
        sectionHeaders = null;
        currentStage = Stage.AfterSectionsAreRead;
    }

    private void TouchSection(int index)
    {
        if (currentStage != Stage.Initalizing)
            throw new InvalidOperationException("TouchSection invoked in improper state.");
        if (sections[index] != null) return;
        var section = GetSectionFromSectionHeader(sectionHeaders[index]);
        sections[index] = section;
    }

    private void FindStringTables()
    {
        TryGetSection(Consts.ObjectsStringTableName, out Section<T> section);
        objectsStringTable = (StringTable<T>)section;
        TryGetSection(Consts.DynamicStringTableName, out section);

        // It might happen that the section is not really available, represented as a NoBits one.
        dynamicStringTable = section as StringTable<T>;
    }

    private void ReadStringTable()
    {
        if (!HasSectionHeader || !HasSectionsStringTable) return;
        var header = ReadSectionHeader(stringTableIndex);
        if (header.Type != SectionType.StringTable)
            throw new InvalidOperationException("Given index of section header does not point at string table which was expected.");

        SectionsStringTable = new StringTable<T>(header, reader);
    }

    private SectionHeader ReadSectionHeader(int index)
    {
        if (index < 0 || index >= sectionHeaderEntryCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        reader.BaseStream.Seek(
            sectionHeaderOffset + index * sectionHeaderEntrySize,
            SeekOrigin.Begin
        );

        return new (reader, _ElfClass, SectionsStringTable);
    }

    private SimpleEndianessAwareReader ObtainEndianessAwareReader(Stream stream)
    {
        var reader = new BinaryReader(stream);
        reader.ReadBytes(4); // ELF magic
        var classByte = reader.ReadByte();

        _ElfClass = classByte switch
        {
            1 => ElfClass.Bit32,
            2 => ElfClass.Bit64,
            _ => throw new ArgumentException($"Given ELF file is of unknown class {classByte}."),
        };

        var endianessByte = reader.ReadByte();

        Endianess = endianessByte switch
        {
            1 => Endianess.LittleEndian,
            2 => Endianess.BigEndian,
            _ => throw new ArgumentException($"Given ELF file uses unknown endianess {endianessByte}."),
        };

        reader.ReadBytes(10); // padding bytes of section e_ident
        return new (stream, Endianess);
    }

    private void ReadFields()
    {
        Type = (FileType)reader.ReadUInt16();
        Machine = (Machine)reader.ReadUInt16();
        var version = reader.ReadUInt32();
        if (version != 1) throw new ArgumentException($"Given ELF file is of unknown version {version}.");
        EntryPoint = (_ElfClass == ElfClass.Bit32 ? reader.ReadUInt32() : reader.ReadUInt64()).To<T>();
        // TODO: assertions for (u)longs
        segmentHeaderOffset = _ElfClass == ElfClass.Bit32 ? reader.ReadUInt32() : reader.ReadInt64();
        sectionHeaderOffset = _ElfClass == ElfClass.Bit32 ? reader.ReadUInt32() : reader.ReadInt64();
        MachineFlags = reader.ReadUInt32().To<T>(); // TODO: always 32bit?
        var elfsize = reader.ReadUInt16(); // elf header size
        segmentHeaderEntrySize = reader.ReadUInt16();
        segmentHeaderEntryCount = reader.ReadUInt16();
        sectionHeaderEntrySize = reader.ReadUInt16();
        sectionHeaderEntryCount = reader.ReadUInt16();
        stringTableIndex = reader.ReadUInt16();
    }

    private GetSectionResult TryGetSectionInner(string name, out Section<T> section)
    {
        section = default;
        if (!HasSectionsStringTable) return GetSectionResult.NoSectionsStringTable;
        if (!sectionIndicesByName.TryGetValue(name, out int index)) return GetSectionResult.NoSuchSection;
        if (index == SectionNameNotUniqueMarker) return GetSectionResult.SectionNameNotUnique;
        return TryGetSectionInner(index, out section);
    }

    private GetSectionResult TryGetSectionInner(int index, out Section<T> section)
    {
        section = default;
        if (index >= sections.Count) return GetSectionResult.NoSuchSection;
        if (sections[index] != null)
        {
            section = sections[index];
            return GetSectionResult.Success;
        }
        if (currentStage != Stage.Initalizing)
            throw new InvalidOperationException("Assert not met: null section by proper index in not initializing stage.");
        TouchSection(index);
        section = sections[index];
        return GetSectionResult.Success;
    }
}
