namespace XbyakSharp.RiscV;
 //* NAME.EXT
 //* NAME
 //* NAME rd
 //* NAME rd, imm
 //* NAME rd, rs1
 //* NAME rd, rs1, rs2
 //* NAME rd, rs1, shamt
 //* NAME rd, rs1, imm
 //* NAME rd, csr, rs1
 //* NAME rd, csr, imm
 //* NAME rd, rs1, rs2, rs3
 //*
 //* x0 zero
 //* x1 ra
 //* x2 sp
 //* x3 gp
 //* x4 tp
 //* x5-7 t0-2
 //* x8 s0/fp
 //* x9 s1
 //* x10-11 a0-1
 //* x12-17 a2-7
 //* x18-27 s2-11
 //* x28-31 t3-t6
 //* f0-7 ft0-7
 //* f8-9 fs0-1
 //* f10-11 fa0-1
 //* f12-17 fa2-7
 //* f18-27 fs2-11
 //* f28-31 ft8-11

public class Parser
{
    public static readonly Dictionary<string, string> RegisterMap = new()
    {
        ["zero"] = "x0",
        ["ra"] = "x1",
        ["sp"] = "x2",
        ["gp"] = "x3",
        ["tp"] = "x4",
        ["t0"] = "x5",
        ["t1"] = "x6",
        ["t2"] = "x7",
        ["s0"] = "x8",
        ["fp"] = "x8",
        ["a0"] = "x10",
        ["a1"] = "x11",
        ["a2"] = "x12",
        ["a3"] = "x13",
        ["a4"] = "x14",
        ["a5"] = "x15",
        ["a6"] = "x16",
        ["a7"] = "x17",
        ["s2"] = "x18",
        ["s3"] = "x19",
        ["s4"] = "x20",
        ["s5"] = "x21",
        ["s6"] = "x22",
        ["s7"] = "x23",
        ["s8"] = "x24",
        ["s9"] = "x25",
        ["s10"] = "x26",
        ["s11"] = "x27",
        ["t3"] = "x28",
        ["t4"] = "x29",
        ["t5"] = "x30",
        ["t6"] = "x31",
    };

    public virtual List<RiscVInstruction> Parse(TextReader reader)
    {
        var insts = new List<RiscVInstruction>();
        var line = "";
        while((line=reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (line.StartsWith('#')) continue;
            if (ParseLine(line) is RiscVInstruction inst) insts.Add(inst);
        }
        return insts;
    }

    public virtual RiscVInstruction ParseLine(string line)
    {
        if (line.LastIndexOf(';') is int i && i >= 0) line = line.Substring(0, i);
        var parts = line.Split(' ');
        if (parts.Length == 0) 
            return null;
        else if (parts.Length == 1)
        {
            var candidates = RiscVInstruction.Instructions.Where(
                i => i.Name.Equals(parts[0], 
                    StringComparison.InvariantCultureIgnoreCase)
                ).ToList();
            if (candidates.Count == 1) return candidates[0] with { };
        }
        else if(parts.Length == 2)
        {
            var candidates = RiscVInstruction.Instructions.Where(
                i => i.Name.Equals(
                    parts[0], 
                    StringComparison.InvariantCultureIgnoreCase)
                ).ToList();
            if (candidates.Any() && parts[1].Split(',') is string[] args && args.Length > 0)
            {
                var cls = args.Select(a => this.ClassifyArgument(a.Trim())).ToList();
                var types = cls.Select(c => (c.type, c.name)).ToList();
                var cads = candidates.Select(
                    c => (c, s: c.Arguments.Select(a => (a.type, a.name)).ToList())).ToList();
                foreach (var cad in cads)
                {
                    if (cad.s.Count == cls.Count)
                    {
                        var f = true;
                        for (int j = 0; j < cls.Count; j++)
                        {
                            if (cad.s[j].type != cls[j].type) { f = false; break; }
                            if (cad.s[j].name == cls[j].name) continue;
                            if (!cad.s[j].name.StartsWith(cls[j].name)) { f = false; break; };
                        }
                        if (f)
                        {
                            var d = cad.c with { };
                            for (int k = 0; k < cls.Count; k++)
                                d.Arguments[k] =
                                    (d.Arguments[k].name, cls[k].type, cls[k].value);
                            return d;
                        }
                    }
                }
            }
        }
        return null;
    }
    protected (string name, Type type,object value) ClassifyArgument(string arg)
    {
        if (string.IsNullOrEmpty(arg))
            return ("", typeof(string), "");
        else if (int.TryParse(arg, out var i))
            return ("imm", typeof(int), i);
        else if ((arg.StartsWith("0x") || arg.StartsWith("0X")) && arg.Length>2 
            && int.TryParse(arg[2..], System.Globalization.NumberStyles.HexNumber, null, out var v))
            return ("imm", typeof(int), v);
        if (RegisterMap.TryGetValue(arg.ToLower(), out var reg)) arg = reg;
        return arg.ToLower().StartsWith("x") && int.TryParse(arg[1..],out var p) && p>=0 && p<=31 
            ? ("r", typeof(int), p)
            : ("", typeof(string), "")
            ;
    }
}
