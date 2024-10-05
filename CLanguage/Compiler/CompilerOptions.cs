// created by jay 0.7 (c) 1998 Axel.Schreiner@informatik.uni-osnabrueck.de

using CLanguage.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace CLanguage.Compiler;

public class CompilerOptions (MachineInfo machineInfo, Report report, IEnumerable<Document> documents)
{
    public readonly MachineInfo MachineInfo = machineInfo;
    public readonly Report Report = report;
    public readonly Document[] Documents = documents.ToArray ();

    public CompilerOptions (MachineInfo machineInfo)
        : this (machineInfo, new Report (), [])
    {
    }

    public CompilerOptions ()
        : this (new MachineInfo ())
    {
    }
}
