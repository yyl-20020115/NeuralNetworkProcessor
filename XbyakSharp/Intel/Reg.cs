﻿/*!
 * --- original library license
 * 
	@file xbyak.h
	@brief Xbyak ; JIT assembler for x86(IA32)/x64 by C++
	@author herumi
	@url https://github.com/herumi/xbyak, http://homepage1.nifty.com/herumi/soft/xbyak_e.html
	@note modified new BSD license
	http://opensource.org/licenses/BSD-3-Clause
*/

/*
 * Reg.cs
 * Author: mes
 * License: new BSD license http://opensource.org/licenses/BSD-3-Clause
 */

namespace XbyakSharp.Intel;
public class Reg : Operand
{
    public Reg() { }
    public Reg(int idx, KindType kind)
        : this(idx, kind, 0, 0) { }

    public Reg(int idx, KindType kind, int bit)
        : this(idx, kind, bit, 0) { }

    public Reg(int idx, KindType kind, int bit, int ext8Bit)
        : base(idx, kind, bit, ext8Bit) { }

    public bool HasRex() => this.IsExt8Bit() || this.IsREG(64) || IsExtIdx();

    public Reg ChangeBit(int bit) => new Reg(IDX, Kind, bit, this.IsExt8Bit() ? 1 : 0);

    public bool IsExtIdx() => IDX > 7;

    public int GetRex() => GetRex(new Reg());

    public int GetRex(Reg baseReg)
    {
        int result = 0;
        if (HasRex() || baseReg.HasRex())
        {
            result = (0x40 | ((this.IsREG(64) || baseReg.IsREG(64)) ? 8 : 0) | (IsExtIdx() ? 4 : 0) | (baseReg.IsExtIdx() ? 1 : 0));
        }
        return result;
    }

    public virtual Reg Copy() => new(IDX, Kind, Bit, Ext8Bit);

    public override Reg ToReg() => this;
}

public class Reg8 : Reg
{
    public Reg8(int idx)
        : this(idx, 0) { }

    public Reg8(int idx, int ext8Bit)
        : base(idx, KindType.REG, 8, ext8Bit) { }

    public override Reg Copy() => new Reg8(IDX, Ext8Bit);
}

public class Reg16 : Reg
{
    public Reg16(int idx)
        : base(idx, KindType.REG, 16) { }

    public override Reg Copy() => new Reg16(IDX);
}

public class Mmx : Reg
{
    public Mmx(int idx)
        : this(idx, KindType.MMX, 64) { }

    public Mmx(int idx, KindType kind)
        : this(idx, kind, 64) { }

    public Mmx(int idx, KindType kind, int bit)
        : base(idx, kind, bit) { }

    public override Reg Copy() => new Mmx(IDX, Kind, Bit);
}

public class Xmm : Mmx
{
    public Xmm(int idx)
        : this(idx, KindType.XMM, 128) { }

    public Xmm(int idx, KindType kind)
        : this(idx, kind, 128) { }

    public Xmm(int idx, KindType kind, int bit)
        : base(idx, kind, bit) { }

    public override Reg Copy() => new Xmm(IDX, Kind, Bit);
}

public class Ymm : Xmm
{
    public Ymm(int idx)
        : base(idx, KindType.YMM, 256) { }

    public override Reg Copy() => new Ymm(IDX);
}

public class Fpu : Reg
{
    public Fpu(int idx)
        : base(idx, KindType.FPU, 32) { }

    public override Reg Copy() => new Fpu(IDX);
}

public class Reg32e : Reg
{
    public Reg32e(int idx, int bit)
        : base(idx, KindType.REG, bit)
    {
        Index = new Reg();
        Scale = 0;
        Disp = 0;
    }

    public Reg32e(Reg baseReg, Reg index, int scale, uint disp)
        : base(baseReg.IDX, baseReg.Kind, baseReg.Bit, baseReg.Ext8Bit)
    {
        if (scale != 0 && scale != 1 && scale != 2 && scale != 4 && scale != 8)
        {
            throw new ArgumentException("scale is one of 0, 1, 2, 4 and 8", "scale");
        }
        if (!baseReg.IsNone() && !index.IsNone() && baseReg.Bit != index.Bit)
        {
            throw new ArgumentException("not equals baseReg.Bit and index.Bit", "index");
        }
        if (index.IDX == (int)Code.ESP)
        {
            throw new ArgumentException("esp cant be index", "index");
        }
        Index = index;
        Scale = scale;
        Disp = disp;
    }

    public Reg Index { get; private set; }

    public int Scale { get; private set; }

    public uint Disp { get; private set; }

    public Reg32e Optimize()
    {
        if (this.IsNone() && !Index.IsNone() && Scale == 2)
        {
            Reg index = new (Index.IDX, KindType.REG, Index.Bit);
            return new (index, index, 1, Disp);
        }
        return this;
    }

    public override Reg Copy() => new Reg32e(this, Index, Scale, Disp);

    public static Reg32e operator +(Reg32e a, Reg32e b)
    {
        if (a.Scale == 0)
        {
            if (b.Scale == 0)
            {
                if (b.IDX == (int)Code.ESP)
                {
                    return new Reg32e(b, a, 1, a.Disp + b.Disp);
                }
                return new Reg32e(a, b, 1, a.Disp + b.Disp);
            }
            else if (b.IsNone())
            {
                return new Reg32e(a, b.Index, b.Scale, a.Disp + b.Disp);
            }
        }
        throw new InvalidOperationException("bad adressing");
    }

    public static Reg32e operator +(Reg32e r, uint disp) => new(r, r.Index, r.Scale, r.Disp + disp);

    public static Reg32e operator -(Reg32e r, uint disp) => new(r, r.Index, r.Scale, r.Disp - disp);

    public static Reg32e operator *(Reg32e r, int scale)
    {
        if (r.Scale == 0)
        {
            if (scale == 1)
            {
                return r;
            }
            else if (scale == 2 || scale == 4 || scale == 8)
            {
                return new Reg32e(new Reg(), r, scale, r.Disp);
            }
        }
        throw new InvalidOperationException("bad scale");
    }
}

public class Reg32 : Reg32e
{
    public Reg32(int idx)
        : base(idx, 32) { }

    public override Reg Copy() => new Reg32(IDX);
}
#region for x64

public class Reg64 : Reg32e
{
    public Reg64(int idx)
        : base(idx, 64) { }

    public override Reg Copy() => new Reg64(IDX);
}

public class RegRip
{
    public RegRip()
        : this(0) { }
    public RegRip(uint disp) => Disp = disp;

    public uint Disp { get; private set; }

    public static RegRip operator +(RegRip r, uint disp) => new (r.Disp + disp);

    public static RegRip operator -(RegRip r, uint disp) => new (r.Disp - disp);
}
#endregion for x64

