/*!
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
 * CodeGeneratorOpCodes.cs
 * Author: mes
 * License: new BSD license http://opensource.org/licenses/BSD-3-Clause
 */
//1513 instructions
namespace XbyakSharp.Intel;
public partial class CodeGenerator
{
    [Instruction(IsPseudoCode =true)]
    public void L(string labelName)
    {
        label.Define(labelName, Size, GetCurrentPointer());
    }

    [Instruction(IsPseudoCode = true)]
    public void InLocalLabel()
    {
        label.EnterLocal();
    }

    [Instruction(IsPseudoCode = true)]
    public void OutLocalLabel()
    {
        label.LeaveLocal();
    }

    [Instruction]
    public void jmp(string labelName, LabelType type = LabelType.Auto)
    {
        OpJmp(labelName, type, (byte)0b11101011, (byte)0b11101001, 0);
    }

    [Instruction]
    public void jmp(IntPtr addr, LabelType type = LabelType.Auto)
    {
        OpJmpAbs(addr, type, (byte)0b11101011, (byte)0b11101001);
    }

    [Instruction]
    public void jmp(IOperand op)
    {
        OpR_ModM(op, (int)BIT, 4, 0xFF, None, None, true);
    }

    [Instruction]
    public void call(IOperand op)
    {
        OpR_ModM(op, 16 | (int)i32e, 2, 0xFF, None, None, true);
    }

    [Instruction]
    public void call(string labelName)
    {
        OpJmp(labelName, LabelType.Near, 0, (byte)0b11101000, 0);
    }

    [Instruction]
    public void call(IntPtr addr)
    {
        OpJmpAbs(addr, LabelType.Near, 0, (byte)0b11101000);
    }

    [Instruction]
    public void test(IOperand op, Reg reg)
    {
        OpModRM(reg, op, op.IsREG() && (op.Kind == reg.Kind), op.IsMEM(), (int)0b10000100);
    }

    [Instruction]
    public void test(IOperand op, uint imm)
    {
        VerifyMemHasSize(op);
        if (op.IsREG() && op.IDX == 0)
        {
            Rex(op);
            Db((int)0b10101000 | (op.IsBit(8) ? 0 : 1));
        }
        else
        {
            OpR_ModM(op, 0, 0, (int)0b11110110);
        }
        Db(imm, Math.Min(op.Bit / 8, 4));
    }

    [Instruction]
    public void ret(int imm = 0)
    {
        if (imm != 0)
        {
            Db((int)0b11000010);
            Dw(unchecked((uint)imm));
        }
        else
        {
            Db((int)0b11000011);
        }
    }

    [Instruction]
    public void imul(Reg reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG() && (reg.Kind == op.Kind), op.IsMEM(), 0x0F, (int)0b10101111);
    }

    [Instruction]
    public void imul(Reg reg, IOperand op, int imm)
    {
        int s = Util.IsInDisp8(unchecked((uint)imm)) ? 1 : 0;
        OpModRM(reg, op, op.IsREG() && (reg.Kind == op.Kind), op.IsMEM(), (int)0b01101001 | (s << 1));
        int size = s != 0 ? 1 : reg.IsREG(16) ? 2 : 4;
        Db(unchecked((uint)imm), size);
    }

    [Instruction]
    public void pop(IOperand op)
    {
        OpPushPop(op, (int)0b10001111, 0, (int)0b01011000);
    }

    [Instruction]
    public void push(IOperand op)
    {
        OpPushPop(op, (int)0b11111111, 6, (int)0b01010000);
    }

    [Instruction]
    public void push(AddressFrame af, uint imm)
    {
        if (af.Bit == 8 && Util.IsInDisp8(imm))
        {
            Db((int)0b01101010);
            Db(imm);
        }
        else if (af.Bit == 16 && IsInDisp16(imm))
        {
            Db(0x66);
            Db((int)0b01101000);
            Dw(imm);
        }
        else
        {
            Db((int)0b01101000);
            Dd(imm);
        }
    }

    [Instruction]
    public void push(uint imm)
    {
        push(Util.IsInDisp8(imm) ? @byte : dword, imm);
    }

    [Instruction]
    public void bswap(Reg32e reg)
    {
        OpModR(new Reg32(1), reg, 0x0F);
    }

    [Instruction]
    public void mov(IOperand reg1, IOperand reg2)
    {
        Reg r = null;
        Address addr = null;
        byte code = 0;
        if (reg1.IsREG() && reg1.IDX == 0 && reg2.IsMEM())
        {
            r = reg1.ToReg();
            addr = (Address)reg2;
            code = (byte)0b10100000;
        }
        else if (reg1.IsMEM() && reg2.IsREG() && reg2.IDX == 0)
        {
            r = reg2.ToReg();
            addr = (Address)reg1;
            code = (byte)0b10100010;
        }
        if (Environment.Is64BitProcess)
        {
            if (addr != null && addr.Is64BitDisp)
            {
                if (code != 0)
                {
                    Rex(r);
                    Db(reg1.IsREG(8) ? 0xA0 : reg1.IsREG() ? 0xA1 : reg2.IsREG(8) ? 0xA2 : 0xA3);
                    Db(addr.Disp, 8);
                }
                else
                {
                    throw new ArgumentException("reg1 and reg2 are bad combination");
                }
            }
            else
            {
                OpRM_RM(reg1, reg2, (int)0b10001000);
            }
        }
        else
        {
            if (code != 0 && addr.IsOnlyDisp)
            {
                Rex(r, addr);
                Db(code | (r.IsBit(8) ? 0 : 1));
                Dd(unchecked((uint)addr.Disp));
            }
            else
            {
                OpRM_RM(reg1, reg2, (int)0b10001000);
            }
        }
    }

    [Instruction]
    public void mov(IOperand op, ulong imm, bool opti = true)
    {
        if (!Environment.Is64BitProcess && imm > uint.MaxValue)
        {
            throw new ArgumentException("imm is must be less than uint.MaxValue in 32bit process");
        }
        VerifyMemHasSize(op);
        if (op.IsREG())
        {
            Rex(op);
            int code = 0;
            int size = 0;
            bool notOpti = !(Environment.Is64BitProcess && (opti && op.IsBit(64) && Util.IsInInt32(imm)));
            if (!notOpti)
            {
                Db((int)0b11000111);
                code = (int)0b11000000;
                size = 4;
            }
            else
            {
                code = (int)0b10110000 | ((op.IsBit(8) ? 0 : 1) << 3);
                size = op.Bit / 8;
            }
            Db(code | (op.IDX & 7));
            Db(imm, size);
        }
        else if (op.IsMEM())
        {
            OpModM((Address)op, new Reg(0, Operand.KindType.REG, op.Bit), (int)0b11000110);
            int size = Math.Min(op.Bit / 8, 4);
            Db(unchecked((uint)imm), size);
        }
        else
        {
            throw new ArgumentException("op and imm are bad combination");
        }
    }

    [Instruction]
    public void mov(Reg32e reg, string labelName)
    {
        if ((Environment.Is64BitProcess && reg.GetType() != typeof(Reg64)) || (!Environment.Is64BitProcess && reg.GetType() != typeof(Reg32)))
        {
            throw new ArgumentException("invalid register type", "reg");
        }
        if (labelName == null)
        {
            mov(reg, 0, true);
            return;
        }

        int jmpSize = IntPtr.Size;
        ulong dummyAddr = 0x12345678;
        if (Environment.Is64BitProcess)
        {
            dummyAddr = 0x1122334455667788UL;
        }
        if (IsAutoGrow && Size + 16 >= MaxSize)
        {
            GrowMemory();
        }
        ulong offset = 0;
        if (label.GetOffset(out offset, labelName))
        {
            if (IsAutoGrow)
            {
                mov(reg, dummyAddr);
                Save((int)Size - jmpSize, offset, jmpSize, LabelMode.LaddTop);
            }
            else
            {
                mov(reg, Top + offset, false);
            }
            return;
        }
        mov(reg, dummyAddr);
        JmpLabel jmp = new ()
        {
            EndOfJmp = Size,
            JmpSize = jmpSize,
            Mode = IsAutoGrow ? LabelMode.LaddTop : LabelMode.Labs
        };
        label.AddUndefinedLabel(labelName, jmp);
    }

    [Instruction]
    public void cmpxchg8b(Address addr)
    {
        OpModM(addr, new Reg32(1), 0x0F, (int)0b11000111);
    }

    [Instruction]
    public void cmpxchg16b(Address addr)
    {
        OpModM(addr, new Reg64(1), 0x0F, (int)0b11000111);
    }

    [Instruction]
    public void xadd(IOperand op, Reg reg)
    {
        OpModRM(reg, op, op.IsREG() && reg.IsREG() && op.Bit == reg.Bit, op.IsMEM(), 0x0F, (int)0b11000000 | (reg.IsBit(8) ? 0 : 1));
    }

    [Instruction]
    public void xchg(IOperand op1, IOperand op2)
    {
        if (op1.IsMEM() || (op2.IsREG(16 | i32e) && op2.IDX == 0))
        {
            Util.Swap(ref op1, ref op2);
        }
        if (op1.IsMEM())
        {
            throw new ArgumentException("op1 and op2 are bad combination");
        }
        if (op2.IsREG() && (op1.IsREG(16 | i32e) && op1.IDX == 0) && (!Environment.Is64BitProcess || (op2.IDX != 0 || !op1.IsREG(32))))
        {
            Rex(op2, op1);
            Db(0x90 | (op2.IDX & 7));
            return;
        }
        OpModRM(op1, op2, op1.IsREG() && op2.IsREG() && op1.Bit == op2.Bit, op2.IsMEM(), (int)0b10000110 | (op1.IsBit(8) ? 0 : 1));
    }

    [Instruction]
    public void movd(Address addr, Mmx mmx)
    {
        if (mmx.IsXMM())
        {
            Db(0x66);
        }
        OpModM(addr, mmx, 0x0F, (int)0b01111110);
    }

    [Instruction]
    public void movd(Reg32 reg, Mmx mmx)
    {
        if (mmx.IsXMM())
        {
            Db(0x66);
        }
        OpModR(mmx, reg, 0x0F, (int)0b01111110);
    }

    [Instruction]
    public void movd(Mmx mmx, Address addr)
    {
        if (mmx.IsXMM())
        {
            Db(0x66);
        }
        OpModM(addr, mmx, 0x0F, (int)0b01101110);
    }

    [Instruction]
    public void movd(Mmx mmx, Reg32 reg)
    {
        if (mmx.IsXMM())
        {
            Db(0x66);
        }
        OpModR(mmx, reg, 0x0F, (int)0b01101110);
    }

    [Instruction]
    public void movq2dq(Xmm xmm, Mmx mmx)
    {
        Db(0xF3);
        OpModR(xmm, mmx, 0x0F, (int)0b11010110);
    }

    [Instruction]
    public void movdq2q(Mmx mmx, Xmm xmm)
    {
        Db(0xF2);
        OpModR(mmx, xmm, 0x0F, (int)0b11010110);
    }

    [Instruction]
    public void movq(Mmx mmx, IOperand op)
    {
        if (mmx.IsXMM())
        {
            Db(0xF3);
        }
        OpModRM(mmx, op, mmx.Kind == op.Kind, op.IsMEM(), 0x0F, (int)(mmx.IsXMM() ? 0b01111110 : 0b01101111));
    }

    [Instruction]
    public void movq(Address addr, Mmx mmx)
    {
        if (mmx.IsXMM())
        {
            Db(0x66);
        }
        OpModM(addr, mmx, 0x0F, (int)(mmx.IsXMM() ? 0b11010110 : 0b01111111));
    }

    [Instruction]
    public void pextrw(IOperand op, Mmx xmm, byte imm)
    {
        OpExt(op, xmm, 0x15, imm, true);
    }

    [Instruction]
    public void pextrb(IOperand op, Xmm xmm, byte imm)
    {
        OpExt(op, xmm, 0x14, imm);
    }

    [Instruction]
    public void pextrd(IOperand op, Xmm xmm, byte imm)
    {
        OpExt(op, xmm, 0x16, imm);
    }

    [Instruction]
    public void extractps(IOperand op, Xmm xmm, byte imm)
    {
        OpExt(op, xmm, 0x17, imm);
    }

    [Instruction]
    public void pinsrw(Mmx mmx, IOperand op, int imm)
    {
        if (!op.IsREG(32) && !op.IsMEM())
        {
            throw new ArgumentException("bad combination", "op");
        }
        OpGen(mmx, op, (int)0b11000100, mmx.IsXMM() ? 0x66 : None, null, imm);
    }

    [Instruction]
    public void insertps(Xmm xmm, IOperand op, byte imm)
    {
        OpGen(xmm, op, 0x21, 0x66, IsXMM_XMMorMEM, imm, (int)0b00111010);
    }

    [Instruction]
    public void pinsrb(Xmm xmm, IOperand op, byte imm)
    {
        OpGen(xmm, op, 0x20, 0x66, IsXMM_REG32orMEM, imm, (int)0b00111010);
    }

    [Instruction]
    public void pinsrd(Xmm xmm, IOperand op, byte imm)
    {
        OpGen(xmm, op, 0x22, 0x66, IsXMM_REG32orMEM, imm, (int)0b00111010);
    }

    [Instruction]
    public void pmovmskb(Reg32e reg, Mmx mmx)
    {
        if (mmx.IsXMM())
        {
            Db(0x66);
        }
        OpModR(reg, mmx, 0x0F, (int)0b11010111);
    }

    [Instruction]
    public void maskmovq(Mmx reg1, Mmx reg2)
    {
        if (!reg1.IsMMX() || !reg2.IsMMX())
        {
            throw new ArgumentException("reg1 and reg2 are bad combination");
        }
        OpModR(reg1, reg2, 0x0F, (int)0b11110111);
    }

    [Instruction]
    public void lea(Reg32e reg, Address addr)
    {
        OpModM(addr, reg, (int)0b10001101);
    }

    [Instruction]
    public void movmskps(Reg32e reg, Xmm xmm)
    {
        OpModR(reg, xmm, 0x0F, (int)0b01010000);
    }

    [Instruction]
    public void movmskpd(Reg32e reg, Xmm xmm)
    {
        Db(0x66);
        movmskps(reg, xmm);
    }

    [Instruction]
    public void movntps(Address addr, Xmm xmm)
    {
        OpModM(addr, new Mmx(xmm.IDX), 0x0F, (int)0b00101011);
    }

    [Instruction]
    public void movntdqa(Xmm xmm, Address addr)
    {
        Db(0x66);
        OpModM(addr, xmm, 0x0F, 0x38, 0x2A);
    }

    [Instruction]
    public void lddqu(Xmm xmm, Address addr)
    {
        Db(0xF2);
        OpModM(addr, xmm, 0x0F, (int)0b11110000);
    }

    [Instruction]
    public void movnti(Address addr, Reg32e reg)
    {
        OpModM(addr, reg, 0x0F, (int)0b11000011);
    }

    [Instruction]
    public void movntq(Address addr, Mmx mmx)
    {
        if (!mmx.IsMMX())
        {
            throw new ArgumentException("bad combination", "mmx");
        }
        OpModM(addr, mmx, 0x0F, (int)0b11100111);
    }

    [Instruction]
    public void popcnt(Reg reg, IOperand op)
    {
        bool is16Bit = reg.IsREG(16) && (op.IsREG(16) || op.IsMEM());
        if (!is16Bit && !(reg.IsREG(i32e) && (op.IsREG(i32e) || op.IsMEM())))
        {
            throw new ArgumentException("reg and op are bad combination");
        }
        if (is16Bit)
        {
            Db(0x66);
        }
        Db(0xF3);
        OpModRM(reg.ChangeBit(i32e == 32 ? 32 : reg.Bit), op, op.IsREG(), true, 0x0F, 0xB8);
    }

    [Instruction]
    public void crc32(Reg32e reg, IOperand op)
    {
        if (reg.IsBit(32) && op.IsBit(16))
        {
            Db(0x66);
        }
        Db(0xF2);
        OpModRM(reg, op, op.IsREG(), op.IsMEM(), 0x0F, 0x38, 0xF0 | (op.IsBit(8) ? 0 : 1));
    }

    [Instruction]
    public void packssdw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x6B);
    }

    [Instruction]
    public void packsswb(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x63);
    }

    [Instruction]
    public void packuswb(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x67);
    }

    [Instruction]
    public void pand(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xDB);
    }

    [Instruction]
    public void pandn(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xDF);
    }

    [Instruction]
    public void pmaddwd(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xF5);
    }

    [Instruction]
    public void pmulhuw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xE4);
    }

    [Instruction]
    public void pmulhw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xE5);
    }

    [Instruction]
    public void pmullw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xD5);
    }

    [Instruction]
    public void por(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xEB);
    }

    [Instruction]
    public void punpckhbw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x68);
    }

    [Instruction]
    public void punpckhwd(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x69);
    }

    [Instruction]
    public void punpckhdq(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x6A);
    }

    [Instruction]
    public void punpcklbw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x60);
    }

    [Instruction]
    public void punpcklwd(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x61);
    }

    [Instruction]
    public void punpckldq(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x62);
    }

    [Instruction]
    public void pxor(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xEF);
    }

    [Instruction]
    public void pavgb(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xE0);
    }

    [Instruction]
    public void pavgw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xE3);
    }

    [Instruction]
    public void pmaxsw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xEE);
    }

    [Instruction]
    public void pmaxub(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xDE);
    }

    [Instruction]
    public void pminsw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xEA);
    }

    [Instruction]
    public void pminub(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xDA);
    }

    [Instruction]
    public void psadbw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xF6);
    }

    [Instruction]
    public void paddq(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xD4);
    }

    [Instruction]
    public void pmuludq(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xF4);
    }

    [Instruction]
    public void psubq(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xFB);
    }

    [Instruction]
    public void paddb(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xFC);
    }

    [Instruction]
    public void paddw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xFD);
    }

    [Instruction]
    public void paddd(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xFE);
    }

    [Instruction]
    public void paddsb(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xEC);
    }

    [Instruction]
    public void paddsw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xED);
    }

    [Instruction]
    public void paddusb(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xDC);
    }

    [Instruction]
    public void paddusw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xDD);
    }

    [Instruction]
    public void pcmpeqb(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x74);
    }

    [Instruction]
    public void pcmpeqw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x75);
    }

    [Instruction]
    public void pcmpeqd(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x76);
    }

    [Instruction]
    public void pcmpgtb(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x64);
    }

    [Instruction]
    public void pcmpgtw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x65);
    }

    [Instruction]
    public void pcmpgtd(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x66);
    }

    [Instruction]
    public void psllw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xF1);
    }

    [Instruction]
    public void pslld(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xF2);
    }

    [Instruction]
    public void psllq(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xF3);
    }

    [Instruction]
    public void psraw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xE1);
    }

    [Instruction]
    public void psrad(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xE2);
    }

    [Instruction]
    public void psrlw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xD1);
    }

    [Instruction]
    public void psrld(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xD2);
    }

    [Instruction]
    public void psrlq(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xD3);
    }

    [Instruction]
    public void psubb(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xF8);
    }

    [Instruction]
    public void psubw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xF9);
    }

    [Instruction]
    public void psubd(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xFA);
    }

    [Instruction]
    public void psubsb(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xE8);
    }

    [Instruction]
    public void psubsw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xE9);
    }

    [Instruction]
    public void psubusb(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xD8);
    }

    [Instruction]
    public void psubusw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0xD9);
    }

    [Instruction]
    public void psllw(Mmx mmx, int imm8)
    {
        OpMMX_IMM(mmx, imm8, 0x71, 6);
    }

    [Instruction]
    public void pslld(Mmx mmx, int imm8)
    {
        OpMMX_IMM(mmx, imm8, 0x72, 6);
    }

    [Instruction]
    public void psllq(Mmx mmx, int imm8)
    {
        OpMMX_IMM(mmx, imm8, 0x73, 6);
    }

    [Instruction]
    public void psraw(Mmx mmx, int imm8)
    {
        OpMMX_IMM(mmx, imm8, 0x71, 4);
    }

    [Instruction]
    public void psrad(Mmx mmx, int imm8)
    {
        OpMMX_IMM(mmx, imm8, 0x72, 4);
    }

    [Instruction]
    public void psrlw(Mmx mmx, int imm8)
    {
        OpMMX_IMM(mmx, imm8, 0x71, 2);
    }

    [Instruction]
    public void psrld(Mmx mmx, int imm8)
    {
        OpMMX_IMM(mmx, imm8, 0x72, 2);
    }

    [Instruction]
    public void psrlq(Mmx mmx, int imm8)
    {
        OpMMX_IMM(mmx, imm8, 0x73, 2);
    }

    [Instruction]
    public void pslldq(Xmm xmm, int imm8)
    {
        OpMMX_IMM(xmm, imm8, 0x73, 7);
    }

    [Instruction]
    public void psrldq(Xmm xmm, int imm8)
    {
        OpMMX_IMM(xmm, imm8, 0x73, 3);
    }

    [Instruction]
    public void pshufw(Mmx mmx, IOperand op, byte imm8)
    {
        OpMMX(mmx, op, 0x70, 0x00, imm8);
    }

    [Instruction]
    public void pshuflw(Mmx mmx, IOperand op, byte imm8)
    {
        OpMMX(mmx, op, 0x70, 0xF2, imm8);
    }

    [Instruction]
    public void pshufhw(Mmx mmx, IOperand op, byte imm8)
    {
        OpMMX(mmx, op, 0x70, 0xF3, imm8);
    }

    [Instruction]
    public void pshufd(Mmx mmx, IOperand op, byte imm8)
    {
        OpMMX(mmx, op, 0x70, 0x66, imm8);
    }

    [Instruction]
    public void movdqa(Xmm xmm, IOperand op)
    {
        OpMMX(xmm, op, 0x6F, 0x66);
    }

    [Instruction]
    public void movdqa(Address addr, Xmm xmm)
    {
        Db(0x66);
        OpModM(addr, xmm, 0x0F, 0x7F);
    }

    [Instruction]
    public void movdqu(Xmm xmm, IOperand op)
    {
        OpMMX(xmm, op, 0x6F, 0xF3);
    }

    [Instruction]
    public void movdqu(Address addr, Xmm xmm)
    {
        Db(0xF3);
        OpModM(addr, xmm, 0x0F, 0x7F);
    }

    [Instruction]
    public void movaps(Xmm xmm, IOperand op)
    {
        OpMMX(xmm, op, 0x28, 0x100);
    }

    [Instruction]
    public void movaps(Address addr, Xmm xmm)
    {
        OpModM(addr, xmm, 0x0F, 0x29);
    }

    [Instruction]
    public void movss(Xmm xmm, IOperand op)
    {
        OpMMX(xmm, op, 0x10, 0xF3);
    }

    [Instruction]
    public void movss(Address addr, Xmm xmm)
    {
        Db(0xF3);
        OpModM(addr, xmm, 0x0F, 0x11);
    }

    [Instruction]
    public void movups(Xmm xmm, IOperand op)
    {
        OpMMX(xmm, op, 0x10, 0x100);
    }

    [Instruction]
    public void movups(Address addr, Xmm xmm)
    {
        OpModM(addr, xmm, 0x0F, 0x11);
    }

    [Instruction]
    public void movapd(Xmm xmm, IOperand op)
    {
        OpMMX(xmm, op, 0x28, 0x66);
    }

    [Instruction]
    public void movapd(Address addr, Xmm xmm)
    {
        Db(0x66);
        OpModM(addr, xmm, 0x0F, 0x29);
    }

    [Instruction]
    public void movsd(Xmm xmm, IOperand op)
    {
        OpMMX(xmm, op, 0x10, 0xF2);
    }

    [Instruction]
    public void movsd(Address addr, Xmm xmm)
    {
        Db(0xF2);
        OpModM(addr, xmm, 0x0F, 0x11);
    }

    [Instruction]
    public void movupd(Xmm xmm, IOperand op)
    {
        OpMMX(xmm, op, 0x10, 0x66);
    }

    [Instruction]
    public void movupd(Address addr, Xmm xmm)
    {
        Db(0x66);
        OpModM(addr, xmm, 0x0F, 0x11);
    }

    [Instruction]
    public void addps(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x58, 0x100, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void addss(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x58, 0xF3, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void addpd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x58, 0x66, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void addsd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x58, 0xF2, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void andnps(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x55, 0x100, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void andnpd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x55, 0x66, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void andps(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x54, 0x100, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void andpd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x54, 0x66, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void cmpps(Xmm xmm, IOperand op, byte imm8)
    {
        OpGen(xmm, op, 0xC2, 0x100, IsXMM_XMMorMEM, imm8);
    }

    [Instruction]
    public void cmpss(Xmm xmm, IOperand op, byte imm8)
    {
        OpGen(xmm, op, 0xC2, 0xF3, IsXMM_XMMorMEM, imm8);
    }

    [Instruction]
    public void cmppd(Xmm xmm, IOperand op, byte imm8)
    {
        OpGen(xmm, op, 0xC2, 0x66, IsXMM_XMMorMEM, imm8);
    }

    [Instruction]
    public void cmpsd(Xmm xmm, IOperand op, byte imm8)
    {
        OpGen(xmm, op, 0xC2, 0xF2, IsXMM_XMMorMEM, imm8);
    }

    [Instruction]
    public void divps(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x5E, 0x100, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void divss(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x5E, 0xF3, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void divpd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x5E, 0x66, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void divsd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x5E, 0xF2, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void maxps(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x5F, 0x100, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void maxss(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x5F, 0xF3, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void maxpd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x5F, 0x66, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void maxsd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x5F, 0xF2, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void minps(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x5D, 0x100, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void minss(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x5D, 0xF3, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void minpd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x5D, 0x66, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void minsd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x5D, 0xF2, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void mulps(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x59, 0x100, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void mulss(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x59, 0xF3, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void mulpd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x59, 0x66, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void mulsd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x59, 0xF2, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void orps(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x56, 0x100, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void orpd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x56, 0x66, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void rcpps(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x53, 0x100, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void rcpss(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x53, 0xF3, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void rsqrtps(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x52, 0x100, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void rsqrtss(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x52, 0xF3, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void shufps(Xmm xmm, IOperand op, byte imm8)
    {
        OpGen(xmm, op, 0xC6, 0x100, IsXMM_XMMorMEM, imm8);
    }

    [Instruction]
    public void shufpd(Xmm xmm, IOperand op, byte imm8)
    {
        OpGen(xmm, op, 0xC6, 0x66, IsXMM_XMMorMEM, imm8);
    }

    [Instruction]
    public void sqrtps(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x51, 0x100, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void sqrtss(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x51, 0xF3, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void sqrtpd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x51, 0x66, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void sqrtsd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x51, 0xF2, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void subps(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x5C, 0x100, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void subss(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x5C, 0xF3, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void subpd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x5C, 0x66, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void subsd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x5C, 0xF2, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void unpckhps(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x15, 0x100, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void unpckhpd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x15, 0x66, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void unpcklps(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x14, 0x100, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void unpcklpd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x14, 0x66, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void xorps(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x57, 0x100, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void xorpd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x57, 0x66, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void maskmovdqu(Xmm reg1, Xmm reg2)
    {
        Db(0x66);
        OpModR(reg1, reg2, 0x0F, 0xF7);
    }

    [Instruction]
    public void movhlps(Xmm reg1, Xmm reg2)
    {
        OpModR(reg1, reg2, 0x0F, 0x12);
    }

    [Instruction]
    public void movlhps(Xmm reg1, Xmm reg2)
    {
        OpModR(reg1, reg2, 0x0F, 0x16);
    }

    [Instruction]
    public void punpckhqdq(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x6D, 0x66, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void punpcklqdq(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x6C, 0x66, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void comiss(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x2F, 0x100, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void ucomiss(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x2E, 0x100, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void comisd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x2F, 0x66, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void ucomisd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x2E, 0x66, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void cvtpd2ps(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x5A, 0x66, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void cvtps2pd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x5A, 0x100, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void cvtsd2ss(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x5A, 0xF2, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void cvtss2sd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x5A, 0xF3, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void cvtpd2dq(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0xE6, 0xF2, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void cvttpd2dq(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0xE6, 0x66, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void cvtdq2pd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0xE6, 0xF3, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void cvtps2dq(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x5B, 0x66, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void cvttps2dq(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x5B, 0xF3, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void cvtdq2ps(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x5B, 0x100, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void addsubpd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0xD0, 0x66, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void addsubps(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0xD0, 0xF2, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void haddpd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x7C, 0x66, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void haddps(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x7C, 0xF2, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void hsubpd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x7D, 0x66, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void hsubps(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x7D, 0xF2, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void movddup(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x12, 0xF2, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void movshdup(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x16, 0xF3, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void movsldup(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x12, 0xF3, IsXMM_XMMorMEM);
    }

    [Instruction]
    public void cvtpi2ps(IOperand reg, IOperand op)
    {
        OpGen(reg, op, 0x2A, 0x100, IsXMM_MMXorMEM);
    }

    [Instruction]
    public void cvtps2pi(IOperand reg, IOperand op)
    {
        OpGen(reg, op, 0x2D, 0x100, IsMMX_XMMorMEM);
    }

    [Instruction]
    public void cvtsi2ss(IOperand reg, IOperand op)
    {
        OpGen(reg, op, 0x2A, 0xF3, IsXMM_REG32orMEM);
    }

    [Instruction]
    public void cvtss2si(IOperand reg, IOperand op)
    {
        OpGen(reg, op, 0x2D, 0xF3, IsREG32_XMMorMEM);
    }

    [Instruction]
    public void cvttps2pi(IOperand reg, IOperand op)
    {
        OpGen(reg, op, 0x2C, 0x100, IsMMX_XMMorMEM);
    }

    [Instruction]
    public void cvttss2si(IOperand reg, IOperand op)
    {
        OpGen(reg, op, 0x2C, 0xF3, IsREG32_XMMorMEM);
    }

    [Instruction]
    public void cvtpi2pd(IOperand reg, IOperand op)
    {
        OpGen(reg, op, 0x2A, 0x66, IsXMM_MMXorMEM);
    }

    [Instruction]
    public void cvtpd2pi(IOperand reg, IOperand op)
    {
        OpGen(reg, op, 0x2D, 0x66, IsMMX_XMMorMEM);
    }

    [Instruction]
    public void cvtsi2sd(IOperand reg, IOperand op)
    {
        OpGen(reg, op, 0x2A, 0xF2, IsXMM_REG32orMEM);
    }

    [Instruction]
    public void cvtsd2si(IOperand reg, IOperand op)
    {
        OpGen(reg, op, 0x2D, 0xF2, IsREG32_XMMorMEM);
    }

    [Instruction]
    public void cvttpd2pi(IOperand reg, IOperand op)
    {
        OpGen(reg, op, 0x2C, 0x66, IsMMX_XMMorMEM);
    }

    [Instruction]
    public void cvttsd2si(IOperand reg, IOperand op)
    {
        OpGen(reg, op, 0x2C, 0xF2, IsREG32_XMMorMEM);
    }

    [Instruction]
    public void prefetcht0(Address addr)
    {
        OpModM(addr, new Reg32(1), 0x0F, (int)0b00011000);
    }

    [Instruction]
    public void prefetcht1(Address addr)
    {
        OpModM(addr, new Reg32(2), 0x0F, (int)0b00011000);
    }

    [Instruction]
    public void prefetcht2(Address addr)
    {
        OpModM(addr, new Reg32(3), 0x0F, (int)0b00011000);
    }

    [Instruction]
    public void prefetchnta(Address addr)
    {
        OpModM(addr, new Reg32(0), 0x0F, (int)0b00011000);
    }

    [Instruction]
    public void movhps(IOperand op1, IOperand op2)
    {
        OpMovXMM(op1, op2, 0x16, 0x100);
    }

    [Instruction]
    public void movlps(IOperand op1, IOperand op2)
    {
        OpMovXMM(op1, op2, 0x12, 0x100);
    }

    [Instruction]
    public void movhpd(IOperand op1, IOperand op2)
    {
        OpMovXMM(op1, op2, 0x16, 0x66);
    }

    [Instruction]
    public void movlpd(IOperand op1, IOperand op2)
    {
        OpMovXMM(op1, op2, 0x12, 0x66);
    }

    [Instruction]
    public void cmovo(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 0);
    }

    [Instruction]
    public void jo(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x70, 0x80, 0x0F);
    }

    [Instruction]
    public void seto(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 0);
    }

    [Instruction]
    public void cmovno(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 1);
    }

    [Instruction]
    public void jno(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x71, 0x81, 0x0F);
    }

    [Instruction]
    public void setno(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 1);
    }

    [Instruction]
    public void cmovb(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 2);
    }

    [Instruction]
    public void jb(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x72, 0x82, 0x0F);
    }

    [Instruction]
    public void setb(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 2);
    }

    [Instruction]
    public void cmovc(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 2);
    }

    [Instruction]
    public void jc(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x72, 0x82, 0x0F);
    }

    [Instruction]
    public void setc(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 2);
    }

    [Instruction]
    public void cmovnae(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 2);
    }

    [Instruction]
    public void jnae(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x72, 0x82, 0x0F);
    }

    [Instruction]
    public void setnae(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 2);
    }

    [Instruction]
    public void cmovnb(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 3);
    }

    [Instruction]
    public void jnb(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x73, 0x83, 0x0F);
    }

    [Instruction]
    public void setnb(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 3);
    }

    [Instruction]
    public void cmovae(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 3);
    }

    [Instruction]
    public void jae(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x73, 0x83, 0x0F);
    }

    [Instruction]
    public void setae(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 3);
    }

    [Instruction]
    public void cmovnc(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 3);
    }

    [Instruction]
    public void jnc(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x73, 0x83, 0x0F);
    }

    [Instruction]
    public void setnc(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 3);
    }

    [Instruction]
    public void cmove(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 4);
    }

    [Instruction]
    public void je(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x74, 0x84, 0x0F);
    }

    [Instruction]
    public void sete(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 4);
    }

    [Instruction]
    public void cmovz(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 4);
    }

    [Instruction]
    public void jz(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x74, 0x84, 0x0F);
    }

    [Instruction]
    public void setz(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 4);
    }

    [Instruction]
    public void cmovne(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 5);
    }

    [Instruction]
    public void jne(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x75, 0x85, 0x0F);
    }

    [Instruction]
    public void setne(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 5);
    }

    [Instruction]
    public void cmovnz(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 5);
    }

    [Instruction]
    public void jnz(string labelName, LabelType type = LabelType.Auto)
    {
        OpJmp(labelName, type, 0x75, 0x85, 0x0F);
    }

    [Instruction]
    public void setnz(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 5);
    }

    [Instruction]
    public void cmovbe(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 6);
    }

    [Instruction]
    public void jbe(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x76, 0x86, 0x0F);
    }

    [Instruction]
    public void setbe(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 6);
    }

    [Instruction]
    public void cmovna(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 6);
    }

    [Instruction]
    public void jna(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x76, 0x86, 0x0F);
    }

    [Instruction]
    public void setna(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 6);
    }

    [Instruction]
    public void cmovnbe(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 7);
    }

    [Instruction]
    public void jnbe(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x77, 0x87, 0x0F);
    }

    [Instruction]
    public void setnbe(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 7);
    }

    [Instruction]
    public void cmova(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 7);
    }

    [Instruction]
    public void ja(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x77, 0x87, 0x0F);
    }

    [Instruction]
    public void seta(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 7);
    }

    [Instruction]
    public void cmovs(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 8);
    }

    [Instruction]
    public void js(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x78, 0x88, 0x0F);
    }

    [Instruction]
    public void sets(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 8);
    }

    [Instruction]
    public void cmovns(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 9);
    }

    [Instruction]
    public void jns(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x79, 0x89, 0x0F);
    }

    [Instruction]
    public void setns(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 9);
    }

    [Instruction]
    public void cmovp(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 10);
    }

    [Instruction]
    public void jp(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x7A, 0x8A, 0x0F);
    }

    [Instruction]
    public void setp(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 10);
    }

    [Instruction]
    public void cmovpe(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 10);
    }

    [Instruction]
    public void jpe(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x7A, 0x8A, 0x0F);
    }

    [Instruction]
    public void setpe(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 10);
    }

    [Instruction]
    public void cmovnp(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 11);
    }

    [Instruction]
    public void jnp(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x7B, 0x8B, 0x0F);
    }

    [Instruction]
    public void setnp(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 11);
    }

    [Instruction]
    public void cmovpo(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 11);
    }

    [Instruction]
    public void jpo(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x7B, 0x8B, 0x0F);
    }

    [Instruction]
    public void setpo(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 11);
    }

    [Instruction]
    public void cmovl(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 12);
    }

    [Instruction]
    public void jl(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x7C, 0x8C, 0x0F);
    }

    [Instruction]
    public void setl(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 12);
    }

    [Instruction]
    public void cmovnge(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 12);
    }

    [Instruction]
    public void jnge(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x7C, 0x8C, 0x0F);
    }

    [Instruction]
    public void setnge(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 12);
    }

    [Instruction]
    public void cmovnl(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 13);
    }

    [Instruction]
    public void jnl(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x7D, 0x8D, 0x0F);
    }

    [Instruction]
    public void setnl(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 13);
    }

    [Instruction]
    public void cmovge(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 13);
    }

    [Instruction]
    public void jge(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x7D, 0x8D, 0x0F);
    }

    [Instruction]
    public void setge(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 13);
    }

    [Instruction]
    public void cmovle(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 14);
    }

    [Instruction]
    public void jle(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x7E, 0x8E, 0x0F);
    }

    [Instruction]
    public void setle(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 14);
    }

    [Instruction]
    public void cmovng(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 14);
    }

    [Instruction]
    public void jng(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x7E, 0x8E, 0x0F);
    }

    [Instruction]
    public void setng(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 14);
    }

    [Instruction]
    public void cmovnle(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 15);
    }

    [Instruction]
    public void jnle(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x7F, 0x8F, 0x0F);
    }

    [Instruction]
    public void setnle(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 15);
    }

    [Instruction]
    public void cmovg(Reg32e reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(i32e), op.IsMEM(), 0x0F, (int)0b01000000 | 15);
    }

    [Instruction]
    public void jg(string label, LabelType type = LabelType.Auto)
    {
        OpJmp(label, type, 0x7F, 0x8F, 0x0F);
    }

    [Instruction]
    public void setg(IOperand op)
    {
        OpR_ModM(op, 8, 0, 0x0F, (int)0b10010000 | 15);
    }

    [Instruction]
    public void jcxz(string label)
    {
        if (Environment.Is64BitProcess)
        {
            throw new InvalidOperationException("cant use in x64 process");
        }
        Db(0x67);
        OpJmp(label, LabelType.Short, 0xe3, 0, 0);
    }

    [Instruction]
    public void jecxz(string label)
    {
        if (Environment.Is64BitProcess)
        {
            Db(0x67);
        }
        OpJmp(label, LabelType.Short, 0xe3, 0, 0);
    }

    [Instruction]
    public void jrcxz(string label)
    {
        if (!Environment.Is64BitProcess)
        {
            throw new InvalidOperationException("cant use in x86 process");
        }
        OpJmp(label, LabelType.Short, 0xe3, 0, 0);
    }

    #region for x64

    [Instruction]
    public void cdqe()
    {
        if (!Environment.Is64BitProcess)
        {
            throw new InvalidOperationException("cant use in x86 process");
        }
        Db(0x48);
        Db(0x98);
    }

    [Instruction]
    public void cqo()
    {
        if (!Environment.Is64BitProcess)
        {
            throw new InvalidOperationException("cant use in x86 process");
        }
        Db(0x48);
        Db(0x99);
    }

    #endregion for x64

    #region for x86

    [Instruction]
    public void aaa()
    {
        if (Environment.Is64BitProcess)
        {
            throw new InvalidOperationException("cant use in x64 process");
        }
        Db(0x37);
    }

    [Instruction]
    public void aad()
    {
        if (Environment.Is64BitProcess)
        {
            throw new InvalidOperationException("cant use in x64 process");
        }
        Db(0xD5);
        Db(0x0A);
    }

    [Instruction]
    public void aam()
    {
        if (Environment.Is64BitProcess)
        {
            throw new InvalidOperationException("cant use in x64 process");
        }
        Db(0xD4);
        Db(0x0A);
    }

    [Instruction]
    public void aas()
    {
        if (Environment.Is64BitProcess)
        {
            throw new InvalidOperationException("cant use in x64 process");
        }
        Db(0x3F);
    }

    [Instruction]
    public void daa()
    {
        if (Environment.Is64BitProcess)
        {
            throw new InvalidOperationException("cant use in x64 process");
        }
        Db(0x27);
    }

    [Instruction]
    public void das()
    {
        if (Environment.Is64BitProcess)
        {
            throw new InvalidOperationException("cant use in x64 process");
        }
        Db(0x2F);
    }

    [Instruction]
    public void popad()
    {
        if (Environment.Is64BitProcess)
        {
            throw new InvalidOperationException("cant use in x64 process");
        }
        Db(0x61);
    }

    [Instruction]
    public void popfd()
    {
        if (Environment.Is64BitProcess)
        {
            throw new InvalidOperationException("cant use in x64 process");
        }
        Db(0x9D);
    }

    [Instruction]
    public void pusha()
    {
        if (Environment.Is64BitProcess)
        {
            throw new InvalidOperationException("cant use in x64 process");
        }
        Db(0x60);
    }

    [Instruction]
    public void pushad()
    {
        if (Environment.Is64BitProcess)
        {
            throw new InvalidOperationException("cant use in x64 process");
        }
        Db(0x60);
    }

    [Instruction]
    public void pushfd()
    {
        if (Environment.Is64BitProcess)
        {
            throw new InvalidOperationException("cant use in x64 process");
        }
        Db(0x9C);
    }

    [Instruction]
    public void popa()
    {
        if (Environment.Is64BitProcess)
        {
            throw new InvalidOperationException("cant use in x64 process");
        }
        Db(0x61);
    }

    #endregion for x86

    [Instruction]
    public void cbw()
    {
        Db(0x66);
        Db(0x98);
    }

    [Instruction]
    public void cdq()
    {
        Db(0x99);
    }

    [Instruction]
    public void clc()
    {
        Db(0xF8);
    }

    [Instruction]
    public void cld()
    {
        Db(0xFC);
    }

    [Instruction]
    public void cli()
    {
        Db(0xFA);
    }

    [Instruction]
    public void cmc()
    {
        Db(0xF5);
    }

    [Instruction]
    public void cpuid()
    {
        Db(0x0F);
        Db(0xA2);
    }

    [Instruction]
    public void cwd()
    {
        Db(0x66);
        Db(0x99);
    }

    [Instruction]
    public void cwde()
    {
        Db(0x98);
    }

    [Instruction]
    public void lahf()
    {
        Db(0x9F);
    }

    [Instruction]
    public void @lock()
    {
        Db(0xF0);
    }

    [Instruction]
    public void nop()
    {
        Db(0x90);
    }

    [Instruction]
    public void sahf()
    {
        Db(0x9E);
    }

    [Instruction]
    public void stc()
    {
        Db(0xF9);
    }

    [Instruction]
    public void std()
    {
        Db(0xFD);
    }

    [Instruction]
    public void sti()
    {
        Db(0xFB);
    }

    [Instruction]
    public void emms()
    {
        Db(0x0F);
        Db(0x77);
    }

    [Instruction]
    public void pause()
    {
        Db(0xF3);
        Db(0x90);
    }

    [Instruction]
    public void sfence()
    {
        Db(0x0F);
        Db(0xAE);
        Db(0xF8);
    }

    [Instruction]
    public void lfence()
    {
        Db(0x0F);
        Db(0xAE);
        Db(0xE8);
    }

    [Instruction]
    public void mfence()
    {
        Db(0x0F);
        Db(0xAE);
        Db(0xF0);
    }

    [Instruction]
    public void monitor()
    {
        Db(0x0F);
        Db(0x01);
        Db(0xC8);
    }

    [Instruction]
    public void mwait()
    {
        Db(0x0F);
        Db(0x01);
        Db(0xC9);
    }

    [Instruction]
    public void rdmsr()
    {
        Db(0x0F);
        Db(0x32);
    }

    [Instruction]
    public void rdpmc()
    {
        Db(0x0F);
        Db(0x33);
    }

    [Instruction]
    public void rdtsc()
    {
        Db(0x0F);
        Db(0x31);
    }

    [Instruction]
    public void rdtscp()
    {
        Db(0x0F);
        Db(0x01);
        Db(0xF9);
    }

    [Instruction]
    public void ud2()
    {
        Db(0x0F);
        Db(0x0B);
    }

    [Instruction]
    public void wait()
    {
        Db(0x9B);
    }

    [Instruction]
    public void fwait()
    {
        Db(0x9B);
    }

    [Instruction]
    public void wbinvd()
    {
        Db(0x0F);
        Db(0x09);
    }

    [Instruction]
    public void wrmsr()
    {
        Db(0x0F);
        Db(0x30);
    }

    [Instruction]
    public void xlatb()
    {
        Db(0xD7);
    }

    [Instruction]
    public void popf()
    {
        Db(0x9D);
    }

    [Instruction]
    public void pushf()
    {
        Db(0x9C);
    }

    [Instruction]
    public void vzeroall()
    {
        Db(0xC5);
        Db(0xFC);
        Db(0x77);
    }

    [Instruction]
    public void vzeroupper()
    {
        Db(0xC5);
        Db(0xF8);
        Db(0x77);
    }

    [Instruction]
    public void xgetbv()
    {
        Db(0x0F);
        Db(0x01);
        Db(0xD0);
    }

    [Instruction]
    public void f2xm1()
    {
        Db(0xD9);
        Db(0xF0);
    }

    [Instruction]
    public void fabs()
    {
        Db(0xD9);
        Db(0xE1);
    }

    [Instruction]
    public void faddp()
    {
        Db(0xDE);
        Db(0xC1);
    }

    [Instruction]
    public void fchs()
    {
        Db(0xD9);
        Db(0xE0);
    }

    [Instruction]
    public void fcom()
    {
        Db(0xD8);
        Db(0xD1);
    }

    [Instruction]
    public void fcomp()
    {
        Db(0xD8);
        Db(0xD9);
    }

    [Instruction]
    public void fcompp()
    {
        Db(0xDE);
        Db(0xD9);
    }

    [Instruction]
    public void fcos()
    {
        Db(0xD9);
        Db(0xFF);
    }

    [Instruction]
    public void fdecstp()
    {
        Db(0xD9);
        Db(0xF6);
    }

    [Instruction]
    public void fdivp()
    {
        Db(0xDE);
        Db(0xF9);
    }

    [Instruction]
    public void fdivrp()
    {
        Db(0xDE);
        Db(0xF1);
    }

    [Instruction]
    public void fincstp()
    {
        Db(0xD9);
        Db(0xF7);
    }

    [Instruction]
    public void finit()
    {
        Db(0x9B);
        Db(0xDB);
        Db(0xE3);
    }

    [Instruction]
    public void fninit()
    {
        Db(0xDB);
        Db(0xE3);
    }

    [Instruction]
    public void fld1()
    {
        Db(0xD9);
        Db(0xE8);
    }

    [Instruction]
    public void fldl2t()
    {
        Db(0xD9);
        Db(0xE9);
    }

    [Instruction]
    public void fldl2e()
    {
        Db(0xD9);
        Db(0xEA);
    }

    [Instruction]
    public void fldpi()
    {
        Db(0xD9);
        Db(0xEB);
    }

    [Instruction]
    public void fldlg2()
    {
        Db(0xD9);
        Db(0xEC);
    }

    [Instruction]
    public void fldln2()
    {
        Db(0xD9);
        Db(0xED);
    }

    [Instruction]
    public void fldz()
    {
        Db(0xD9);
        Db(0xEE);
    }

    [Instruction]
    public void fmulp()
    {
        Db(0xDE);
        Db(0xC9);
    }

    [Instruction]
    public void fnop()
    {
        Db(0xD9);
        Db(0xD0);
    }

    [Instruction]
    public void fpatan()
    {
        Db(0xD9);
        Db(0xF3);
    }

    [Instruction]
    public void fprem()
    {
        Db(0xD9);
        Db(0xF8);
    }

    [Instruction]
    public void fprem1()
    {
        Db(0xD9);
        Db(0xF5);
    }

    [Instruction]
    public void fptan()
    {
        Db(0xD9);
        Db(0xF2);
    }

    [Instruction]
    public void frndint()
    {
        Db(0xD9);
        Db(0xFC);
    }

    [Instruction]
    public void fscale()
    {
        Db(0xD9);
        Db(0xFD);
    }

    [Instruction]
    public void fsin()
    {
        Db(0xD9);
        Db(0xFE);
    }

    [Instruction]
    public void fsincos()
    {
        Db(0xD9);
        Db(0xFB);
    }

    [Instruction]
    public void fsqrt()
    {
        Db(0xD9);
        Db(0xFA);
    }

    [Instruction]
    public void fsubp()
    {
        Db(0xDE);
        Db(0xE9);
    }

    [Instruction]
    public void fsubrp()
    {
        Db(0xDE);
        Db(0xE1);
    }

    [Instruction]
    public void ftst()
    {
        Db(0xD9);
        Db(0xE4);
    }

    [Instruction]
    public void fucom()
    {
        Db(0xDD);
        Db(0xE1);
    }

    [Instruction]
    public void fucomp()
    {
        Db(0xDD);
        Db(0xE9);
    }

    [Instruction]
    public void fucompp()
    {
        Db(0xDA);
        Db(0xE9);
    }

    [Instruction]
    public void fxam()
    {
        Db(0xD9);
        Db(0xE5);
    }

    [Instruction]
    public void fxch()
    {
        Db(0xD9);
        Db(0xC9);
    }

    [Instruction]
    public void fxtract()
    {
        Db(0xD9);
        Db(0xF4);
    }

    [Instruction]
    public void fyl2x()
    {
        Db(0xD9);
        Db(0xF1);
    }

    [Instruction]
    public void fyl2xp1()
    {
        Db(0xD9);
        Db(0xF9);
    }

    [Instruction]
    public void adc(IOperand op1, IOperand op2)
    {
        OpRM_RM(op1, op2, 0x10);
    }

    [Instruction]
    public void adc(IOperand op, uint imm)
    {
        OpRM_I(op, imm, 0x10, 2);
    }

    [Instruction]
    public void add(IOperand op1, IOperand op2)
    {
        OpRM_RM(op1, op2, 0x00);
    }

    [Instruction]
    public void add(IOperand op, uint imm)
    {
        OpRM_I(op, imm, 0x00, 0);
    }

    [Instruction]
    public void and(IOperand op1, IOperand op2)
    {
        OpRM_RM(op1, op2, 0x20);
    }

    [Instruction]
    public void and(IOperand op, uint imm)
    {
        OpRM_I(op, imm, 0x20, 4);
    }

    [Instruction]
    public void cmp(IOperand op1, IOperand op2)
    {
        OpRM_RM(op1, op2, 0x38);
    }

    [Instruction]
    public void cmp(IOperand op, uint imm)
    {
        OpRM_I(op, imm, 0x38, 7);
    }

    [Instruction]
    public void or(IOperand op1, IOperand op2)
    {
        OpRM_RM(op1, op2, 0x08);
    }

    [Instruction]
    public void or(IOperand op, uint imm)
    {
        OpRM_I(op, imm, 0x08, 1);
    }

    [Instruction]
    public void sbb(IOperand op1, IOperand op2)
    {
        OpRM_RM(op1, op2, 0x18);
    }

    [Instruction]
    public void sbb(IOperand op, uint imm)
    {
        OpRM_I(op, imm, 0x18, 3);
    }

    [Instruction]
    public void sub(IOperand op1, IOperand op2)
    {
        OpRM_RM(op1, op2, 0x28);
    }

    [Instruction]
    public void sub(IOperand op, uint imm)
    {
        OpRM_I(op, imm, 0x28, 5);
    }

    [Instruction]
    public void xor(IOperand op1, IOperand op2)
    {
        OpRM_RM(op1, op2, 0x30);
    }

    [Instruction]
    public void xor(IOperand op, uint imm)
    {
        OpRM_I(op, imm, 0x30, 6);
    }

    [Instruction]
    public void dec(IOperand op)
    {
        OpIncDec(op, 0x48, 1);
    }

    [Instruction]
    public void inc(IOperand op)
    {
        OpIncDec(op, 0x40, 0);
    }

    [Instruction]
    public void div(IOperand op)
    {
        OpR_ModM(op, 0, 6, 0xF6);
    }

    [Instruction]
    public void idiv(IOperand op)
    {
        OpR_ModM(op, 0, 7, 0xF6);
    }

    [Instruction]
    public void imul(IOperand op)
    {
        OpR_ModM(op, 0, 5, 0xF6);
    }

    [Instruction]
    public void mul(IOperand op)
    {
        OpR_ModM(op, 0, 4, 0xF6);
    }

    [Instruction]
    public void neg(IOperand op)
    {
        OpR_ModM(op, 0, 3, 0xF6);
    }

    [Instruction]
    public void not(IOperand op)
    {
        OpR_ModM(op, 0, 2, 0xF6);
    }

    [Instruction]
    public void rcl(IOperand op, int imm)
    {
        OpShift(op, imm, 2);
    }

    [Instruction]
    public void rcl(IOperand op, Reg8 cl)
    {
        OpShift(op, cl, 2);
    }

    [Instruction]
    public void rcr(IOperand op, int imm)
    {
        OpShift(op, imm, 3);
    }

    [Instruction]
    public void rcr(IOperand op, Reg8 cl)
    {
        OpShift(op, cl, 3);
    }

    [Instruction]
    public void rol(IOperand op, int imm)
    {
        OpShift(op, imm, 0);
    }

    [Instruction]
    public void rol(IOperand op, Reg8 cl)
    {
        OpShift(op, cl, 0);
    }

    [Instruction]
    public void ror(IOperand op, int imm)
    {
        OpShift(op, imm, 1);
    }

    [Instruction]
    public void ror(IOperand op, Reg8 cl)
    {
        OpShift(op, cl, 1);
    }

    [Instruction]
    public void sar(IOperand op, int imm)
    {
        OpShift(op, imm, 7);
    }

    [Instruction]
    public void sar(IOperand op, Reg8 cl)
    {
        OpShift(op, cl, 7);
    }

    [Instruction]
    public void shl(IOperand op, int imm)
    {
        OpShift(op, imm, 4);
    }

    [Instruction]
    public void shl(IOperand op, Reg8 cl)
    {
        OpShift(op, cl, 4);
    }

    [Instruction]
    public void shr(IOperand op, int imm)
    {
        OpShift(op, imm, 5);
    }

    [Instruction]
    public void shr(IOperand op, Reg8 cl)
    {
        OpShift(op, cl, 5);
    }

    [Instruction]
    public void sal(IOperand op, int imm)
    {
        OpShift(op, imm, 4);
    }

    [Instruction]
    public void sal(IOperand op, Reg8 cl)
    {
        OpShift(op, cl, 4);
    }

    [Instruction]
    public void shld(IOperand op, Reg reg, byte imm)
    {
        OpShxd(op, reg, imm, 0xA4);
    }

    [Instruction]
    public void shld(IOperand op, Reg reg, Reg8 cl)
    {
        OpShxd(op, reg, 0, 0xA4, cl);
    }

    [Instruction]
    public void shrd(IOperand op, Reg reg, byte imm)
    {
        OpShxd(op, reg, imm, 0xAC);
    }

    [Instruction]
    public void shrd(IOperand op, Reg reg, Reg8 cl)
    {
        OpShxd(op, reg, 0, 0xAC, cl);
    }

    [Instruction]
    public void bsf(Reg reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(16 | i32e), op.IsMEM(), 0x0F, 0xBC);
    }

    [Instruction]
    public void bsr(Reg reg, IOperand op)
    {
        OpModRM(reg, op, op.IsREG(16 | i32e), op.IsMEM(), 0x0F, 0xBD);
    }

    [Instruction]
    public void pshufb(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x00, 0x66, None, 0x38);
    }

    [Instruction]
    public void phaddw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x01, 0x66, None, 0x38);
    }

    [Instruction]
    public void phaddd(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x02, 0x66, None, 0x38);
    }

    [Instruction]
    public void phaddsw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x03, 0x66, None, 0x38);
    }

    [Instruction]
    public void pmaddubsw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x04, 0x66, None, 0x38);
    }

    [Instruction]
    public void phsubw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x05, 0x66, None, 0x38);
    }

    [Instruction]
    public void phsubd(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x06, 0x66, None, 0x38);
    }

    [Instruction]
    public void phsubsw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x07, 0x66, None, 0x38);
    }

    [Instruction]
    public void psignb(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x08, 0x66, None, 0x38);
    }

    [Instruction]
    public void psignw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x09, 0x66, None, 0x38);
    }

    [Instruction]
    public void psignd(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x0A, 0x66, None, 0x38);
    }

    [Instruction]
    public void pmulhrsw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x0B, 0x66, None, 0x38);
    }

    [Instruction]
    public void pabsb(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x1C, 0x66, None, 0x38);
    }

    [Instruction]
    public void pabsw(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x1D, 0x66, None, 0x38);
    }

    [Instruction]
    public void pabsd(Mmx mmx, IOperand op)
    {
        OpMMX(mmx, op, 0x1E, 0x66, None, 0x38);
    }

    [Instruction]
    public void palignr(Mmx mmx, IOperand op, int imm)
    {
        OpMMX(mmx, op, 0x0f, 0x66, unchecked((byte)imm), 0x3a);
    }

    [Instruction]
    public void blendvpd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x15, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void blendvps(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x14, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void packusdw(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x2B, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void pblendvb(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x10, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void pcmpeqq(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x29, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void ptest(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x17, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void pmovsxbw(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x20, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void pmovsxbd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x21, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void pmovsxbq(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x22, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void pmovsxwd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x23, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void pmovsxwq(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x24, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void pmovsxdq(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x25, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void pmovzxbw(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x30, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void pmovzxbd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x31, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void pmovzxbq(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x32, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void pmovzxwd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x33, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void pmovzxwq(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x34, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void pmovzxdq(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x35, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void pminsb(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x38, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void pminsd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x39, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void pminuw(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x3A, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void pminud(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x3B, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void pmaxsb(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x3C, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void pmaxsd(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x3D, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void pmaxuw(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x3E, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void pmaxud(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x3F, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void pmuldq(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x28, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void pmulld(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x40, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void phminposuw(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x41, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void pcmpgtq(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0x37, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void aesdec(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0xDE, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void aesdeclast(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0xDF, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void aesenc(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0xDC, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void aesenclast(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0xDD, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void aesimc(Xmm xmm, IOperand op)
    {
        OpGen(xmm, op, 0xDB, 0x66, IsXMM_XMMorMEM, None, 0x38);
    }

    [Instruction]
    public void blendpd(Xmm xmm, IOperand op, int imm)
    {
        OpGen(xmm, op, 0x0D, 0x66, IsXMM_XMMorMEM, unchecked((byte)imm), 0x3A);
    }

    [Instruction]
    public void blendps(Xmm xmm, IOperand op, int imm)
    {
        OpGen(xmm, op, 0x0C, 0x66, IsXMM_XMMorMEM, unchecked((byte)imm), 0x3A);
    }

    [Instruction]
    public void dppd(Xmm xmm, IOperand op, int imm)
    {
        OpGen(xmm, op, 0x41, 0x66, IsXMM_XMMorMEM, unchecked((byte)imm), 0x3A);
    }

    [Instruction]
    public void dpps(Xmm xmm, IOperand op, int imm)
    {
        OpGen(xmm, op, 0x40, 0x66, IsXMM_XMMorMEM, unchecked((byte)imm), 0x3A);
    }

    [Instruction]
    public void mpsadbw(Xmm xmm, IOperand op, int imm)
    {
        OpGen(xmm, op, 0x42, 0x66, IsXMM_XMMorMEM, unchecked((byte)imm), 0x3A);
    }

    [Instruction]
    public void pblendw(Xmm xmm, IOperand op, int imm)
    {
        OpGen(xmm, op, 0x0E, 0x66, IsXMM_XMMorMEM, unchecked((byte)imm), 0x3A);
    }

    [Instruction]
    public void roundps(Xmm xmm, IOperand op, int imm)
    {
        OpGen(xmm, op, 0x08, 0x66, IsXMM_XMMorMEM, unchecked((byte)imm), 0x3A);
    }

    [Instruction]
    public void roundpd(Xmm xmm, IOperand op, int imm)
    {
        OpGen(xmm, op, 0x09, 0x66, IsXMM_XMMorMEM, unchecked((byte)imm), 0x3A);
    }

    [Instruction]
    public void roundss(Xmm xmm, IOperand op, int imm)
    {
        OpGen(xmm, op, 0x0A, 0x66, IsXMM_XMMorMEM, unchecked((byte)imm), 0x3A);
    }

    [Instruction]
    public void roundsd(Xmm xmm, IOperand op, int imm)
    {
        OpGen(xmm, op, 0x0B, 0x66, IsXMM_XMMorMEM, unchecked((byte)imm), 0x3A);
    }

    [Instruction]
    public void pcmpestrm(Xmm xmm, IOperand op, int imm)
    {
        OpGen(xmm, op, 0x60, 0x66, IsXMM_XMMorMEM, unchecked((byte)imm), 0x3A);
    }

    [Instruction]
    public void pcmpestri(Xmm xmm, IOperand op, int imm)
    {
        OpGen(xmm, op, 0x61, 0x66, IsXMM_XMMorMEM, unchecked((byte)imm), 0x3A);
    }

    [Instruction]
    public void pcmpistrm(Xmm xmm, IOperand op, int imm)
    {
        OpGen(xmm, op, 0x62, 0x66, IsXMM_XMMorMEM, unchecked((byte)imm), 0x3A);
    }

    [Instruction]
    public void pcmpistri(Xmm xmm, IOperand op, int imm)
    {
        OpGen(xmm, op, 0x63, 0x66, IsXMM_XMMorMEM, unchecked((byte)imm), 0x3A);
    }

    [Instruction]
    public void pclmulqdq(Xmm xmm, IOperand op, int imm)
    {
        OpGen(xmm, op, 0x44, 0x66, IsXMM_XMMorMEM, unchecked((byte)imm), 0x3A);
    }

    [Instruction]
    public void aeskeygenassist(Xmm xmm, IOperand op, int imm)
    {
        OpGen(xmm, op, 0xDF, 0x66, IsXMM_XMMorMEM, unchecked((byte)imm), 0x3A);
    }

    [Instruction]
    public void pclmullqlqdq(Xmm xmm, IOperand op)
    {
        pclmulqdq(xmm, op, 0x00);
    }

    [Instruction]
    public void pclmulhqlqdq(Xmm xmm, IOperand op)
    {
        pclmulqdq(xmm, op, 0x01);
    }

    [Instruction]
    public void pclmullqhdq(Xmm xmm, IOperand op)
    {
        pclmulqdq(xmm, op, 0x10);
    }

    [Instruction]
    public void pclmulhqhdq(Xmm xmm, IOperand op)
    {
        pclmulqdq(xmm, op, 0x11);
    }

    [Instruction]
    public void ldmxcsr(Address addr)
    {
        OpModM(addr, new Reg32(2), 0x0F, 0xAE);
    }

    [Instruction]
    public void stmxcsr(Address addr)
    {
        OpModM(addr, new Reg32(3), 0x0F, 0xAE);
    }

    [Instruction]
    public void clflush(Address addr)
    {
        OpModM(addr, new Reg32(7), 0x0F, 0xAE);
    }

    [Instruction]
    public void fldcw(Address addr)
    {
        OpModM(addr, new Reg32(5), 0xD9, 0x100);
    }

    [Instruction]
    public void fstcw(Address addr)
    {
        Db(0x9B);
        OpModM(addr, new Reg32(7), 0xD9, None);
    }

    [Instruction]
    public void movntpd(Address addr, Xmm reg)
    {
        OpModM(addr, new Reg16(reg.IDX), 0x0F, 0x2B);
    }

    [Instruction]
    public void movntdq(Address addr, Xmm reg)
    {
        OpModM(addr, new Reg16(reg.IDX), 0x0F, 0xE7);
    }

    [Instruction]
    public void movsx(Reg reg, IOperand op)
    {
        OpMovxx(reg, op, 0xBE);
    }

    [Instruction]
    public void movzx(Reg reg, IOperand op)
    {
        OpMovxx(reg, op, 0xB6);
    }

    [Instruction]
    public void fadd(Address addr)
    {
        OpFpuMem(addr, 0x00, 0xD8, 0xDC, 0, 0);
    }

    [Instruction]
    public void fiadd(Address addr)
    {
        OpFpuMem(addr, 0xDE, 0xDA, 0x00, 0, 0);
    }

    [Instruction]
    public void fcom(Address addr)
    {
        OpFpuMem(addr, 0x00, 0xD8, 0xDC, 2, 0);
    }

    [Instruction]
    public void fcomp(Address addr)
    {
        OpFpuMem(addr, 0x00, 0xD8, 0xDC, 3, 0);
    }

    [Instruction]
    public void fdiv(Address addr)
    {
        OpFpuMem(addr, 0x00, 0xD8, 0xDC, 6, 0);
    }

    [Instruction]
    public void fidiv(Address addr)
    {
        OpFpuMem(addr, 0xDE, 0xDA, 0x00, 6, 0);
    }

    [Instruction]
    public void fdivr(Address addr)
    {
        OpFpuMem(addr, 0x00, 0xD8, 0xDC, 7, 0);
    }

    [Instruction]
    public void fidivr(Address addr)
    {
        OpFpuMem(addr, 0xDE, 0xDA, 0x00, 7, 0);
    }

    [Instruction]
    public void ficom(Address addr)
    {
        OpFpuMem(addr, 0xDE, 0xDA, 0x00, 2, 0);
    }

    [Instruction]
    public void ficomp(Address addr)
    {
        OpFpuMem(addr, 0xDE, 0xDA, 0x00, 3, 0);
    }

    [Instruction]
    public void fild(Address addr)
    {
        OpFpuMem(addr, 0xDF, 0xDB, 0xDF, 0, 5);
    }

    [Instruction]
    public void fist(Address addr)
    {
        OpFpuMem(addr, 0xDF, 0xDB, 0x00, 2, 0);
    }

    [Instruction]
    public void fistp(Address addr)
    {
        OpFpuMem(addr, 0xDF, 0xDB, 0xDF, 3, 7);
    }

    [Instruction]
    public void fisttp(Address addr)
    {
        OpFpuMem(addr, 0xDF, 0xDB, 0xDD, 1, 0);
    }

    [Instruction]
    public void fld(Address addr)
    {
        OpFpuMem(addr, 0x00, 0xD9, 0xDD, 0, 0);
    }

    [Instruction]
    public void fmul(Address addr)
    {
        OpFpuMem(addr, 0x00, 0xD8, 0xDC, 1, 0);
    }

    [Instruction]
    public void fimul(Address addr)
    {
        OpFpuMem(addr, 0xDE, 0xDA, 0x00, 1, 0);
    }

    [Instruction]
    public void fst(Address addr)
    {
        OpFpuMem(addr, 0x00, 0xD9, 0xDD, 2, 0);
    }

    [Instruction]
    public void fstp(Address addr)
    {
        OpFpuMem(addr, 0x00, 0xD9, 0xDD, 3, 0);
    }

    [Instruction]
    public void fsub(Address addr)
    {
        OpFpuMem(addr, 0x00, 0xD8, 0xDC, 4, 0);
    }

    [Instruction]
    public void fisub(Address addr)
    {
        OpFpuMem(addr, 0xDE, 0xDA, 0x00, 4, 0);
    }

    [Instruction]
    public void fsubr(Address addr)
    {
        OpFpuMem(addr, 0x00, 0xD8, 0xDC, 5, 0);
    }

    [Instruction]
    public void fisubr(Address addr)
    {
        OpFpuMem(addr, 0xDE, 0xDA, 0x00, 5, 0);
    }

    [Instruction]
    public void fadd(Fpu reg1, Fpu reg2)
    {
        OpFpuFpu(reg1, reg2, 0xD8C0, 0xDCC0);
    }

    [Instruction]
    public void fadd(Fpu reg1)
    {
        OpFpuFpu(st0, reg1, 0xD8C0, 0xDCC0);
    }

    [Instruction]
    public void faddp(Fpu reg1, Fpu reg2)
    {
        OpFpuFpu(reg1, reg2, 0x0000, 0xDEC0);
    }

    [Instruction]
    public void faddp(Fpu reg1)
    {
        OpFpuFpu(reg1, st0, 0x0000, 0xDEC0);
    }

    [Instruction]
    public void fcmovb(Fpu reg1, Fpu reg2)
    {
        OpFpuFpu(reg1, reg2, 0xDAC0, 0x00C0);
    }

    [Instruction]
    public void fcmovb(Fpu reg1)
    {
        OpFpuFpu(st0, reg1, 0xDAC0, 0x00C0);
    }

    [Instruction]
    public void fcmove(Fpu reg1, Fpu reg2)
    {
        OpFpuFpu(reg1, reg2, 0xDAC8, 0x00C8);
    }

    [Instruction]
    public void fcmove(Fpu reg1)
    {
        OpFpuFpu(st0, reg1, 0xDAC8, 0x00C8);
    }

    [Instruction]
    public void fcmovbe(Fpu reg1, Fpu reg2)
    {
        OpFpuFpu(reg1, reg2, 0xDAD0, 0x00D0);
    }

    [Instruction]
    public void fcmovbe(Fpu reg1)
    {
        OpFpuFpu(st0, reg1, 0xDAD0, 0x00D0);
    }

    [Instruction]
    public void fcmovu(Fpu reg1, Fpu reg2)
    {
        OpFpuFpu(reg1, reg2, 0xDAD8, 0x00D8);
    }

    [Instruction]
    public void fcmovu(Fpu reg1)
    {
        OpFpuFpu(st0, reg1, 0xDAD8, 0x00D8);
    }

    [Instruction]
    public void fcmovnb(Fpu reg1, Fpu reg2)
    {
        OpFpuFpu(reg1, reg2, 0xDBC0, 0x00C0);
    }

    [Instruction]
    public void fcmovnb(Fpu reg1)
    {
        OpFpuFpu(st0, reg1, 0xDBC0, 0x00C0);
    }

    [Instruction]
    public void fcmovne(Fpu reg1, Fpu reg2)
    {
        OpFpuFpu(reg1, reg2, 0xDBC8, 0x00C8);
    }

    [Instruction]
    public void fcmovne(Fpu reg1)
    {
        OpFpuFpu(st0, reg1, 0xDBC8, 0x00C8);
    }

    [Instruction]
    public void fcmovnbe(Fpu reg1, Fpu reg2)
    {
        OpFpuFpu(reg1, reg2, 0xDBD0, 0x00D0);
    }

    [Instruction]
    public void fcmovnbe(Fpu reg1)
    {
        OpFpuFpu(st0, reg1, 0xDBD0, 0x00D0);
    }

    [Instruction]
    public void fcmovnu(Fpu reg1, Fpu reg2)
    {
        OpFpuFpu(reg1, reg2, 0xDBD8, 0x00D8);
    }

    [Instruction]
    public void fcmovnu(Fpu reg1)
    {
        OpFpuFpu(st0, reg1, 0xDBD8, 0x00D8);
    }

    [Instruction]
    public void fcomi(Fpu reg1, Fpu reg2)
    {
        OpFpuFpu(reg1, reg2, 0xDBF0, 0x00F0);
    }

    [Instruction]
    public void fcomi(Fpu reg1)
    {
        OpFpuFpu(st0, reg1, 0xDBF0, 0x00F0);
    }

    [Instruction]
    public void fcomip(Fpu reg1, Fpu reg2)
    {
        OpFpuFpu(reg1, reg2, 0xDFF0, 0x00F0);
    }

    [Instruction]
    public void fcomip(Fpu reg1)
    {
        OpFpuFpu(st0, reg1, 0xDFF0, 0x00F0);
    }

    [Instruction]
    public void fucomi(Fpu reg1, Fpu reg2)
    {
        OpFpuFpu(reg1, reg2, 0xDBE8, 0x00E8);
    }

    [Instruction]
    public void fucomi(Fpu reg1)
    {
        OpFpuFpu(st0, reg1, 0xDBE8, 0x00E8);
    }

    [Instruction]
    public void fucomip(Fpu reg1, Fpu reg2)
    {
        OpFpuFpu(reg1, reg2, 0xDFE8, 0x00E8);
    }

    [Instruction]
    public void fucomip(Fpu reg1)
    {
        OpFpuFpu(st0, reg1, 0xDFE8, 0x00E8);
    }

    [Instruction]
    public void fdiv(Fpu reg1, Fpu reg2)
    {
        OpFpuFpu(reg1, reg2, 0xD8F0, 0xDCF8);
    }

    [Instruction]
    public void fdiv(Fpu reg1)
    {
        OpFpuFpu(st0, reg1, 0xD8F0, 0xDCF8);
    }

    [Instruction]
    public void fdivp(Fpu reg1, Fpu reg2)
    {
        OpFpuFpu(reg1, reg2, 0x0000, 0xDEF8);
    }

    [Instruction]
    public void fdivp(Fpu reg1)
    {
        OpFpuFpu(reg1, st0, 0x0000, 0xDEF8);
    }

    [Instruction]
    public void fdivr(Fpu reg1, Fpu reg2)
    {
        OpFpuFpu(reg1, reg2, 0xD8F8, 0xDCF0);
    }

    [Instruction]
    public void fdivr(Fpu reg1)
    {
        OpFpuFpu(st0, reg1, 0xD8F8, 0xDCF0);
    }

    [Instruction]
    public void fdivrp(Fpu reg1, Fpu reg2)
    {
        OpFpuFpu(reg1, reg2, 0x0000, 0xDEF0);
    }

    [Instruction]
    public void fdivrp(Fpu reg1)
    {
        OpFpuFpu(reg1, st0, 0x0000, 0xDEF0);
    }

    [Instruction]
    public void fmul(Fpu reg1, Fpu reg2)
    {
        OpFpuFpu(reg1, reg2, 0xD8C8, 0xDCC8);
    }

    [Instruction]
    public void fmul(Fpu reg1)
    {
        OpFpuFpu(st0, reg1, 0xD8C8, 0xDCC8);
    }

    [Instruction]
    public void fmulp(Fpu reg1, Fpu reg2)
    {
        OpFpuFpu(reg1, reg2, 0x0000, 0xDEC8);
    }

    [Instruction]
    public void fmulp(Fpu reg1)
    {
        OpFpuFpu(reg1, st0, 0x0000, 0xDEC8);
    }

    [Instruction]
    public void fsub(Fpu reg1, Fpu reg2)
    {
        OpFpuFpu(reg1, reg2, 0xD8E0, 0xDCE8);
    }

    [Instruction]
    public void fsub(Fpu reg1)
    {
        OpFpuFpu(st0, reg1, 0xD8E0, 0xDCE8);
    }

    [Instruction]
    public void fsubp(Fpu reg1, Fpu reg2)
    {
        OpFpuFpu(reg1, reg2, 0x0000, 0xDEE8);
    }

    [Instruction]
    public void fsubp(Fpu reg1)
    {
        OpFpuFpu(reg1, st0, 0x0000, 0xDEE8);
    }

    [Instruction]
    public void fsubr(Fpu reg1, Fpu reg2)
    {
        OpFpuFpu(reg1, reg2, 0xD8E8, 0xDCE0);
    }

    [Instruction]
    public void fsubr(Fpu reg1)
    {
        OpFpuFpu(st0, reg1, 0xD8E8, 0xDCE0);
    }

    [Instruction]
    public void fsubrp(Fpu reg1, Fpu reg2)
    {
        OpFpuFpu(reg1, reg2, 0x0000, 0xDEE0);
    }

    [Instruction]
    public void fsubrp(Fpu reg1)
    {
        OpFpuFpu(reg1, st0, 0x0000, 0xDEE0);
    }

    [Instruction]
    public void fcom(Fpu reg)
    {
        OpFpu(reg, 0xD8, 0xD0);
    }

    [Instruction]
    public void fcomp(Fpu reg)
    {
        OpFpu(reg, 0xD8, 0xD8);
    }

    [Instruction]
    public void ffree(Fpu reg)
    {
        OpFpu(reg, 0xDD, 0xC0);
    }

    [Instruction]
    public void fld(Fpu reg)
    {
        OpFpu(reg, 0xD9, 0xC0);
    }

    [Instruction]
    public void fst(Fpu reg)
    {
        OpFpu(reg, 0xDD, 0xD0);
    }

    [Instruction]
    public void fstp(Fpu reg)
    {
        OpFpu(reg, 0xDD, 0xD8);
    }

    [Instruction]
    public void fucom(Fpu reg)
    {
        OpFpu(reg, 0xDD, 0xE0);
    }

    [Instruction]
    public void fucomp(Fpu reg)
    {
        OpFpu(reg, 0xDD, 0xE8);
    }

    [Instruction]
    public void fxch(Fpu reg)
    {
        OpFpu(reg, 0xD9, 0xC8);
    }

    [Instruction]
    public void vaddpd(Xmm xmm, IOperand op1)
    {
        vaddpd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vaddpd(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_66, 0x58, true);
    }

    [Instruction]
    public void vaddps(Xmm xmm, IOperand op1)
    {
        vaddps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vaddps(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F, 0x58, true);
    }

    [Instruction]
    public void vaddsd(Xmm xmm, IOperand op1)
    {
        vaddsd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vaddsd(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_F2, 0x58, false);
    }

    [Instruction]
    public void vaddss(Xmm xmm, IOperand op1)
    {
        vaddss(xmm, op1, new Operand());
    }

    [Instruction]
    public void vaddss(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_F3, 0x58, false);
    }

    [Instruction]
    public void vsubpd(Xmm xmm, IOperand op1)
    {
        vsubpd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vsubpd(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_66, 0x5C, true);
    }

    [Instruction]
    public void vsubps(Xmm xmm, IOperand op1)
    {
        vsubps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vsubps(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F, 0x5C, true);
    }

    [Instruction]
    public void vsubsd(Xmm xmm, IOperand op1)
    {
        vsubsd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vsubsd(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_F2, 0x5C, false);
    }

    [Instruction]
    public void vsubss(Xmm xmm, IOperand op1)
    {
        vsubss(xmm, op1, new Operand());
    }

    [Instruction]
    public void vsubss(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_F3, 0x5C, false);
    }

    [Instruction]
    public void vmulpd(Xmm xmm, IOperand op1)
    {
        vmulpd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vmulpd(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_66, 0x59, true);
    }

    [Instruction]
    public void vmulps(Xmm xmm, IOperand op1)
    {
        vmulps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vmulps(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F, 0x59, true);
    }

    [Instruction]
    public void vmulsd(Xmm xmm, IOperand op1)
    {
        vmulsd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vmulsd(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_F2, 0x59, false);
    }

    [Instruction]
    public void vmulss(Xmm xmm, IOperand op1)
    {
        vmulss(xmm, op1, new Operand());
    }

    [Instruction]
    public void vmulss(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_F3, 0x59, false);
    }

    [Instruction]
    public void vdivpd(Xmm xmm, IOperand op1)
    {
        vdivpd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vdivpd(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_66, 0x5E, true);
    }

    [Instruction]
    public void vdivps(Xmm xmm, IOperand op1)
    {
        vdivps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vdivps(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F, 0x5E, true);
    }

    [Instruction]
    public void vdivsd(Xmm xmm, IOperand op1)
    {
        vdivsd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vdivsd(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_F2, 0x5E, false);
    }

    [Instruction]
    public void vdivss(Xmm xmm, IOperand op1)
    {
        vdivss(xmm, op1, new Operand());
    }

    [Instruction]
    public void vdivss(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_F3, 0x5E, false);
    }

    [Instruction]
    public void vmaxpd(Xmm xmm, IOperand op1)
    {
        vmaxpd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vmaxpd(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_66, 0x5F, true);
    }

    [Instruction]
    public void vmaxps(Xmm xmm, IOperand op1)
    {
        vmaxps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vmaxps(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F, 0x5F, true);
    }

    [Instruction]
    public void vmaxsd(Xmm xmm, IOperand op1)
    {
        vmaxsd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vmaxsd(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_F2, 0x5F, false);
    }

    [Instruction]
    public void vmaxss(Xmm xmm, IOperand op1)
    {
        vmaxss(xmm, op1, new Operand());
    }

    [Instruction]
    public void vmaxss(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_F3, 0x5F, false);
    }

    [Instruction]
    public void vminpd(Xmm xmm, IOperand op1)
    {
        vminpd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vminpd(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_66, 0x5D, true);
    }

    [Instruction]
    public void vminps(Xmm xmm, IOperand op1)
    {
        vminps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vminps(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F, 0x5D, true);
    }

    [Instruction]
    public void vminsd(Xmm xmm, IOperand op1)
    {
        vminsd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vminsd(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_F2, 0x5D, false);
    }

    [Instruction]
    public void vminss(Xmm xmm, IOperand op1)
    {
        vminss(xmm, op1, new Operand());
    }

    [Instruction]
    public void vminss(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_F3, 0x5D, false);
    }

    [Instruction]
    public void vandpd(Xmm xmm, IOperand op1)
    {
        vandpd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vandpd(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_66, 0x54, true);
    }

    [Instruction]
    public void vandps(Xmm xmm, IOperand op1)
    {
        vandps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vandps(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F, 0x54, true);
    }

    [Instruction]
    public void vandnpd(Xmm xmm, IOperand op1)
    {
        vandnpd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vandnpd(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_66, 0x55, true);
    }

    [Instruction]
    public void vandnps(Xmm xmm, IOperand op1)
    {
        vandnps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vandnps(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F, 0x55, true);
    }

    [Instruction]
    public void vorpd(Xmm xmm, IOperand op1)
    {
        vorpd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vorpd(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_66, 0x56, true);
    }

    [Instruction]
    public void vorps(Xmm xmm, IOperand op1)
    {
        vorps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vorps(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F, 0x56, true);
    }

    [Instruction]
    public void vxorpd(Xmm xmm, IOperand op1)
    {
        vxorpd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vxorpd(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_66, 0x57, true);
    }

    [Instruction]
    public void vxorps(Xmm xmm, IOperand op1)
    {
        vxorps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vxorps(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F, 0x57, true);
    }

    [Instruction]
    public void vblendpd(Xmm xm1, Xmm xm2, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x0D, true, 0);
        Db(imm);
    }

    [Instruction]
    public void vblendpd(Xmm xmm, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x0D, true, 0);
        Db(imm);
    }

    [Instruction]
    public void vblendps(Xmm xm1, Xmm xm2, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x0C, true, 0);
        Db(imm);
    }

    [Instruction]
    public void vblendps(Xmm xmm, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x0C, true, 0);
        Db(imm);
    }

    [Instruction]
    public void vdppd(Xmm xm1, Xmm xm2, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x41, false, 0);
        Db(imm);
    }

    [Instruction]
    public void vdppd(Xmm xmm, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x41, false, 0);
        Db(imm);
    }

    [Instruction]
    public void vdpps(Xmm xm1, Xmm xm2, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x40, true, 0);
        Db(imm);
    }

    [Instruction]
    public void vdpps(Xmm xmm, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x40, true, 0);
        Db(imm);
    }

    [Instruction]
    public void vmpsadbw(Xmm xm1, Xmm xm2, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x42, false, 0);
        Db(imm);
    }

    [Instruction]
    public void vmpsadbw(Xmm xmm, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x42, false, 0);
        Db(imm);
    }

    [Instruction]
    public void vpblendw(Xmm xm1, Xmm xm2, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x0E, false, 0);
        Db(imm);
    }

    [Instruction]
    public void vpblendw(Xmm xmm, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x0E, false, 0);
        Db(imm);
    }

    [Instruction]
    public void vroundsd(Xmm xm1, Xmm xm2, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x0B, false, 0);
        Db(imm);
    }

    [Instruction]
    public void vroundsd(Xmm xmm, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x0B, false, 0);
        Db(imm);
    }

    [Instruction]
    public void vroundss(Xmm xm1, Xmm xm2, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x0A, false, 0);
        Db(imm);
    }

    [Instruction]
    public void vroundss(Xmm xmm, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x0A, false, 0);
        Db(imm);
    }

    [Instruction]
    public void vpclmulqdq(Xmm xm1, Xmm xm2, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x44, false, 0);
        Db(imm);
    }

    [Instruction]
    public void vpclmulqdq(Xmm xmm, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x44, false, 0);
        Db(imm);
    }

    [Instruction]
    public void vpermilps(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x0C, true, 0);
    }

    [Instruction]
    public void vpermilpd(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x0D, true, 0);
    }

    [Instruction]
    public void vcmppd(Xmm xm1, Xmm xm2, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xC2, true, -1);
        Db(imm);
    }

    [Instruction]
    public void vcmppd(Xmm xmm, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xC2, true, -1);
        Db(imm);
    }

    [Instruction]
    public void vcmpps(Xmm xm1, Xmm xm2, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F, 0xC2, true, -1);
        Db(imm);
    }

    [Instruction]
    public void vcmpps(Xmm xmm, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F, 0xC2, true, -1);
        Db(imm);
    }

    [Instruction]
    public void vcmpsd(Xmm xm1, Xmm xm2, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_F2, 0xC2, false, -1);
        Db(imm);
    }

    [Instruction]
    public void vcmpsd(Xmm xmm, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_F2, 0xC2, false, -1);
        Db(imm);
    }

    [Instruction]
    public void vcmpss(Xmm xm1, Xmm xm2, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_F3, 0xC2, false, -1);
        Db(imm);
    }

    [Instruction]
    public void vcmpss(Xmm xmm, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_F3, 0xC2, false, -1);
        Db(imm);
    }

    [Instruction]
    public void vcvtsd2ss(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_F2, 0x5A, false, -1);
    }

    [Instruction]
    public void vcvtsd2ss(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_F2, 0x5A, false, -1);
    }

    [Instruction]
    public void vcvtss2sd(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_F3, 0x5A, false, -1);
    }

    [Instruction]
    public void vcvtss2sd(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_F3, 0x5A, false, -1);
    }

    [Instruction]
    public void vinsertps(Xmm xm1, Xmm xm2, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x21, false, 0);
        Db(imm);
    }

    [Instruction]
    public void vinsertps(Xmm xmm, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x21, false, 0);
        Db(imm);
    }

    [Instruction]
    public void vpacksswb(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0x63, false, -1);
    }

    [Instruction]
    public void vpacksswb(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x63, false, -1);
    }

    [Instruction]
    public void vpackssdw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0x6B, false, -1);
    }

    [Instruction]
    public void vpackssdw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x6B, false, -1);
    }

    [Instruction]
    public void vpackuswb(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0x67, false, -1);
    }

    [Instruction]
    public void vpackuswb(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x67, false, -1);
    }

    [Instruction]
    public void vpackusdw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x2B, false, -1);
    }

    [Instruction]
    public void vpackusdw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x2B, false, -1);
    }

    [Instruction]
    public void vpaddb(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xFC, false, -1);
    }

    [Instruction]
    public void vpaddb(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xFC, false, -1);
    }

    [Instruction]
    public void vpaddw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xFD, false, -1);
    }

    [Instruction]
    public void vpaddw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xFD, false, -1);
    }

    [Instruction]
    public void vpaddd(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xFE, false, -1);
    }

    [Instruction]
    public void vpaddd(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xFE, false, -1);
    }

    [Instruction]
    public void vpaddq(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xD4, false, -1);
    }

    [Instruction]
    public void vpaddq(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xD4, false, -1);
    }

    [Instruction]
    public void vpaddsb(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xEC, false, -1);
    }

    [Instruction]
    public void vpaddsb(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xEC, false, -1);
    }

    [Instruction]
    public void vpaddsw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xED, false, -1);
    }

    [Instruction]
    public void vpaddsw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xED, false, -1);
    }

    [Instruction]
    public void vpaddusb(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xDC, false, -1);
    }

    [Instruction]
    public void vpaddusb(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xDC, false, -1);
    }

    [Instruction]
    public void vpaddusw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xDD, false, -1);
    }

    [Instruction]
    public void vpaddusw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xDD, false, -1);
    }

    [Instruction]
    public void vpalignr(Xmm xm1, Xmm xm2, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x0F, false, -1);
        Db(imm);
    }

    [Instruction]
    public void vpalignr(Xmm xmm, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x0F, false, -1);
        Db(imm);
    }

    [Instruction]
    public void vpand(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xDB, false, -1);
    }

    [Instruction]
    public void vpand(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xDB, false, -1);
    }

    [Instruction]
    public void vpandn(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xDF, false, -1);
    }

    [Instruction]
    public void vpandn(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xDF, false, -1);
    }

    [Instruction]
    public void vpavgb(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xE0, false, -1);
    }

    [Instruction]
    public void vpavgb(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xE0, false, -1);
    }

    [Instruction]
    public void vpavgw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xE3, false, -1);
    }

    [Instruction]
    public void vpavgw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xE3, false, -1);
    }

    [Instruction]
    public void vpcmpeqb(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0x74, false, -1);
    }

    [Instruction]
    public void vpcmpeqb(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x74, false, -1);
    }

    [Instruction]
    public void vpcmpeqw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0x75, false, -1);
    }

    [Instruction]
    public void vpcmpeqw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x75, false, -1);
    }

    [Instruction]
    public void vpcmpeqd(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0x76, false, -1);
    }

    [Instruction]
    public void vpcmpeqd(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x76, false, -1);
    }

    [Instruction]
    public void vpcmpeqq(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x29, false, -1);
    }

    [Instruction]
    public void vpcmpeqq(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x29, false, -1);
    }

    [Instruction]
    public void vpcmpgtb(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0x64, false, -1);
    }

    [Instruction]
    public void vpcmpgtb(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x64, false, -1);
    }

    [Instruction]
    public void vpcmpgtw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0x65, false, -1);
    }

    [Instruction]
    public void vpcmpgtw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x65, false, -1);
    }

    [Instruction]
    public void vpcmpgtd(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0x66, false, -1);
    }

    [Instruction]
    public void vpcmpgtd(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x66, false, -1);
    }

    [Instruction]
    public void vpcmpgtq(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x37, false, -1);
    }

    [Instruction]
    public void vpcmpgtq(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x37, false, -1);
    }

    [Instruction]
    public void vphaddw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x01, false, -1);
    }

    [Instruction]
    public void vphaddw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x01, false, -1);
    }

    [Instruction]
    public void vphaddd(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x02, false, -1);
    }

    [Instruction]
    public void vphaddd(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x02, false, -1);
    }

    [Instruction]
    public void vphaddsw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x03, false, -1);
    }

    [Instruction]
    public void vphaddsw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x03, false, -1);
    }

    [Instruction]
    public void vphsubw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x05, false, -1);
    }

    [Instruction]
    public void vphsubw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x05, false, -1);
    }

    [Instruction]
    public void vphsubd(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x06, false, -1);
    }

    [Instruction]
    public void vphsubd(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x06, false, -1);
    }

    [Instruction]
    public void vphsubsw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x07, false, -1);
    }

    [Instruction]
    public void vphsubsw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x07, false, -1);
    }

    [Instruction]
    public void vpmaddwd(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xF5, false, -1);
    }

    [Instruction]
    public void vpmaddwd(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xF5, false, -1);
    }

    [Instruction]
    public void vpmaddubsw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x04, false, -1);
    }

    [Instruction]
    public void vpmaddubsw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x04, false, -1);
    }

    [Instruction]
    public void vpmaxsb(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x3C, false, -1);
    }

    [Instruction]
    public void vpmaxsb(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x3C, false, -1);
    }

    [Instruction]
    public void vpmaxsw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xEE, false, -1);
    }

    [Instruction]
    public void vpmaxsw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xEE, false, -1);
    }

    [Instruction]
    public void vpmaxsd(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x3D, false, -1);
    }

    [Instruction]
    public void vpmaxsd(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x3D, false, -1);
    }

    [Instruction]
    public void vpmaxub(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xDE, false, -1);
    }

    [Instruction]
    public void vpmaxub(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xDE, false, -1);
    }

    [Instruction]
    public void vpmaxuw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x3E, false, -1);
    }

    [Instruction]
    public void vpmaxuw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x3E, false, -1);
    }

    [Instruction]
    public void vpmaxud(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x3F, false, -1);
    }

    [Instruction]
    public void vpmaxud(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x3F, false, -1);
    }

    [Instruction]
    public void vpminsb(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x38, false, -1);
    }

    [Instruction]
    public void vpminsb(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x38, false, -1);
    }

    [Instruction]
    public void vpminsw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xEA, false, -1);
    }

    [Instruction]
    public void vpminsw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xEA, false, -1);
    }

    [Instruction]
    public void vpminsd(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x39, false, -1);
    }

    [Instruction]
    public void vpminsd(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x39, false, -1);
    }

    [Instruction]
    public void vpminub(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xDA, false, -1);
    }

    [Instruction]
    public void vpminub(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xDA, false, -1);
    }

    [Instruction]
    public void vpminuw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x3A, false, -1);
    }

    [Instruction]
    public void vpminuw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x3A, false, -1);
    }

    [Instruction]
    public void vpminud(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x3B, false, -1);
    }

    [Instruction]
    public void vpminud(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x3B, false, -1);
    }

    [Instruction]
    public void vpmulhuw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xE4, false, -1);
    }

    [Instruction]
    public void vpmulhuw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xE4, false, -1);
    }

    [Instruction]
    public void vpmulhrsw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x0B, false, -1);
    }

    [Instruction]
    public void vpmulhrsw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x0B, false, -1);
    }

    [Instruction]
    public void vpmulhw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xE5, false, -1);
    }

    [Instruction]
    public void vpmulhw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xE5, false, -1);
    }

    [Instruction]
    public void vpmullw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xD5, false, -1);
    }

    [Instruction]
    public void vpmullw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xD5, false, -1);
    }

    [Instruction]
    public void vpmulld(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x40, false, -1);
    }

    [Instruction]
    public void vpmulld(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x40, false, -1);
    }

    [Instruction]
    public void vpmuludq(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xF4, false, -1);
    }

    [Instruction]
    public void vpmuludq(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xF4, false, -1);
    }

    [Instruction]
    public void vpmuldq(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x28, false, -1);
    }

    [Instruction]
    public void vpmuldq(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x28, false, -1);
    }

    [Instruction]
    public void vpor(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xEB, false, -1);
    }

    [Instruction]
    public void vpor(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xEB, false, -1);
    }

    [Instruction]
    public void vpsadbw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xF6, false, -1);
    }

    [Instruction]
    public void vpsadbw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xF6, false, -1);
    }

    [Instruction]
    public void vpshufb(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x00, false, -1);
    }

    [Instruction]
    public void vpsignb(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x08, false, -1);
    }

    [Instruction]
    public void vpsignb(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x08, false, -1);
    }

    [Instruction]
    public void vpsignw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x09, false, -1);
    }

    [Instruction]
    public void vpsignw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x09, false, -1);
    }

    [Instruction]
    public void vpsignd(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x0A, false, -1);
    }

    [Instruction]
    public void vpsignd(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x0A, false, -1);
    }

    [Instruction]
    public void vpsllw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xF1, false, -1);
    }

    [Instruction]
    public void vpsllw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xF1, false, -1);
    }

    [Instruction]
    public void vpslld(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xF2, false, -1);
    }

    [Instruction]
    public void vpslld(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xF2, false, -1);
    }

    [Instruction]
    public void vpsllq(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xF3, false, -1);
    }

    [Instruction]
    public void vpsllq(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xF3, false, -1);
    }

    [Instruction]
    public void vpsraw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xE1, false, -1);
    }

    [Instruction]
    public void vpsraw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xE1, false, -1);
    }

    [Instruction]
    public void vpsrad(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xE2, false, -1);
    }

    [Instruction]
    public void vpsrad(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xE2, false, -1);
    }

    [Instruction]
    public void vpsrlw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xD1, false, -1);
    }

    [Instruction]
    public void vpsrlw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xD1, false, -1);
    }

    [Instruction]
    public void vpsrld(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xD2, false, -1);
    }

    [Instruction]
    public void vpsrld(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xD2, false, -1);
    }

    [Instruction]
    public void vpsrlq(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xD3, false, -1);
    }

    [Instruction]
    public void vpsrlq(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xD3, false, -1);
    }

    [Instruction]
    public void vpsubb(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xF8, false, -1);
    }

    [Instruction]
    public void vpsubb(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xF8, false, -1);
    }

    [Instruction]
    public void vpsubw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xF9, false, -1);
    }

    [Instruction]
    public void vpsubw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xF9, false, -1);
    }

    [Instruction]
    public void vpsubd(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xFA, false, -1);
    }

    [Instruction]
    public void vpsubd(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xFA, false, -1);
    }

    [Instruction]
    public void vpsubq(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xFB, false, -1);
    }

    [Instruction]
    public void vpsubq(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xFB, false, -1);
    }

    [Instruction]
    public void vpsubsb(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xE8, false, -1);
    }

    [Instruction]
    public void vpsubsb(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xE8, false, -1);
    }

    [Instruction]
    public void vpsubsw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xE9, false, -1);
    }

    [Instruction]
    public void vpsubsw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xE9, false, -1);
    }

    [Instruction]
    public void vpsubusb(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xD8, false, -1);
    }

    [Instruction]
    public void vpsubusb(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xD8, false, -1);
    }

    [Instruction]
    public void vpsubusw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xD9, false, -1);
    }

    [Instruction]
    public void vpsubusw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xD9, false, -1);
    }

    [Instruction]
    public void vpunpckhbw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0x68, false, -1);
    }

    [Instruction]
    public void vpunpckhbw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x68, false, -1);
    }

    [Instruction]
    public void vpunpckhwd(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0x69, false, -1);
    }

    [Instruction]
    public void vpunpckhwd(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x69, false, -1);
    }

    [Instruction]
    public void vpunpckhdq(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0x6A, false, -1);
    }

    [Instruction]
    public void vpunpckhdq(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x6A, false, -1);
    }

    [Instruction]
    public void vpunpckhqdq(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0x6D, false, -1);
    }

    [Instruction]
    public void vpunpckhqdq(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x6D, false, -1);
    }

    [Instruction]
    public void vpunpcklbw(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0x60, false, -1);
    }

    [Instruction]
    public void vpunpcklbw(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x60, false, -1);
    }

    [Instruction]
    public void vpunpcklwd(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0x61, false, -1);
    }

    [Instruction]
    public void vpunpcklwd(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x61, false, -1);
    }

    [Instruction]
    public void vpunpckldq(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0x62, false, -1);
    }

    [Instruction]
    public void vpunpckldq(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x62, false, -1);
    }

    [Instruction]
    public void vpunpcklqdq(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0x6C, false, -1);
    }

    [Instruction]
    public void vpunpcklqdq(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x6C, false, -1);
    }

    [Instruction]
    public void vpxor(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xEF, false, -1);
    }

    [Instruction]
    public void vpxor(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xEF, false, -1);
    }

    [Instruction]
    public void vrcpss(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_F3, 0x53, false, -1);
    }

    [Instruction]
    public void vrcpss(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_F3, 0x53, false, -1);
    }

    [Instruction]
    public void vrsqrtss(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_F3, 0x52, false, -1);
    }

    [Instruction]
    public void vrsqrtss(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_F3, 0x52, false, -1);
    }

    [Instruction]
    public void vshufpd(Xmm xm1, Xmm xm2, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0xC6, true, -1);
        Db(imm);
    }

    [Instruction]
    public void vshufpd(Xmm xmm, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0xC6, true, -1);
        Db(imm);
    }

    [Instruction]
    public void vshufps(Xmm xm1, Xmm xm2, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F, 0xC6, true, -1);
        Db(imm);
    }

    [Instruction]
    public void vshufps(Xmm xmm, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F, 0xC6, true, -1);
        Db(imm);
    }

    [Instruction]
    public void vsqrtsd(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_F2, 0x51, false, -1);
    }

    [Instruction]
    public void vsqrtsd(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_F2, 0x51, false, -1);
    }

    [Instruction]
    public void vsqrtss(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_F3, 0x51, false, -1);
    }

    [Instruction]
    public void vsqrtss(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_F3, 0x51, false, -1);
    }

    [Instruction]
    public void vunpckhpd(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0x15, true, -1);
    }

    [Instruction]
    public void vunpckhpd(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x15, true, -1);
    }

    [Instruction]
    public void vunpckhps(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F, 0x15, true, -1);
    }

    [Instruction]
    public void vunpckhps(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F, 0x15, true, -1);
    }

    [Instruction]
    public void vunpcklpd(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F | AVXType.Pp_66, 0x14, true, -1);
    }

    [Instruction]
    public void vunpcklpd(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x14, true, -1);
    }

    [Instruction]
    public void vunpcklps(Xmm xm1, Xmm xm2, IOperand op)
    {
        OpAVX_X_X_XM(xm1, xm2, op, AVXType.Mm_0F, 0x14, true, -1);
    }

    [Instruction]
    public void vunpcklps(Xmm xmm, IOperand op)
    {
        OpAVX_X_X_XM(xmm, xmm, op, AVXType.Mm_0F, 0x14, true, -1);
    }

    [Instruction]
    public void vaeskeygenassist(Xmm xm, IOperand op, byte imm)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0xDF, false, 0, imm);
    }

    [Instruction]
    public void vroundpd(Xmm xm, IOperand op, byte imm)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x09, true, 0, imm);
    }

    [Instruction]
    public void vroundps(Xmm xm, IOperand op, byte imm)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x08, true, 0, imm);
    }

    [Instruction]
    public void vpermilpd(Xmm xm, IOperand op, byte imm)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x05, true, 0, imm);
    }

    [Instruction]
    public void vpermilps(Xmm xm, IOperand op, byte imm)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x04, true, 0, imm);
    }

    [Instruction]
    public void vpcmpestri(Xmm xm, IOperand op, byte imm)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x61, false, 0, imm);
    }

    [Instruction]
    public void vpcmpestrm(Xmm xm, IOperand op, byte imm)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x60, false, 0, imm);
    }

    [Instruction]
    public void vpcmpistri(Xmm xm, IOperand op, byte imm)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x63, false, 0, imm);
    }

    [Instruction]
    public void vpcmpistrm(Xmm xm, IOperand op, byte imm)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x62, false, 0, imm);
    }

    [Instruction]
    public void vtestps(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x0E, true, 0);
    }

    [Instruction]
    public void vtestpd(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x0F, true, 0);
    }

    [Instruction]
    public void vcomisd(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x2F, false, -1);
    }

    [Instruction]
    public void vcomiss(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F, 0x2F, false, -1);
    }

    [Instruction]
    public void vcvtdq2ps(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F, 0x5B, true, -1);
    }

    [Instruction]
    public void vcvtps2dq(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x5B, true, -1);
    }

    [Instruction]
    public void vcvttps2dq(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F | AVXType.Pp_F3, 0x5B, true, -1);
    }

    [Instruction]
    public void vmovapd(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x28, true, -1);
    }

    [Instruction]
    public void vmovaps(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F, 0x28, true, -1);
    }

    [Instruction]
    public void vmovddup(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F | AVXType.Pp_F2, 0x12, true, -1);
    }

    [Instruction]
    public void vmovdqa(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x6F, true, -1);
    }

    [Instruction]
    public void vmovdqu(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F | AVXType.Pp_F3, 0x6F, true, -1);
    }

    [Instruction]
    public void vmovshdup(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F | AVXType.Pp_F3, 0x16, true, -1);
    }

    [Instruction]
    public void vmovsldup(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F | AVXType.Pp_F3, 0x12, true, -1);
    }

    [Instruction]
    public void vmovupd(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x10, true, -1);
    }

    [Instruction]
    public void vmovups(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F, 0x10, true, -1);
    }

    [Instruction]
    public void vpabsb(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x1C, false, -1);
    }

    [Instruction]
    public void vpabsw(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x1D, false, -1);
    }

    [Instruction]
    public void vpabsd(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x1E, false, -1);
    }

    [Instruction]
    public void vphminposuw(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x41, false, -1);
    }

    [Instruction]
    public void vpmovsxbw(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x20, false, -1);
    }

    [Instruction]
    public void vpmovsxbd(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x21, false, -1);
    }

    [Instruction]
    public void vpmovsxbq(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x22, false, -1);
    }

    [Instruction]
    public void vpmovsxwd(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x23, false, -1);
    }

    [Instruction]
    public void vpmovsxwq(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x24, false, -1);
    }

    [Instruction]
    public void vpmovsxdq(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x25, false, -1);
    }

    [Instruction]
    public void vpmovzxbw(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x30, false, -1);
    }

    [Instruction]
    public void vpmovzxbd(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x31, false, -1);
    }

    [Instruction]
    public void vpmovzxbq(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x32, false, -1);
    }

    [Instruction]
    public void vpmovzxwd(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x33, false, -1);
    }

    [Instruction]
    public void vpmovzxwq(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x34, false, -1);
    }

    [Instruction]
    public void vpmovzxdq(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x35, false, -1);
    }

    [Instruction]
    public void vpshufd(Xmm xm, IOperand op, byte imm)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x70, false, -1, imm);
    }

    [Instruction]
    public void vpshufhw(Xmm xm, IOperand op, byte imm)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F | AVXType.Pp_F3, 0x70, false, -1, imm);
    }

    [Instruction]
    public void vpshuflw(Xmm xm, IOperand op, byte imm)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F | AVXType.Pp_F2, 0x70, false, -1, imm);
    }

    [Instruction]
    public void vptest(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0x17, false, -1);
    }

    [Instruction]
    public void vrcpps(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F, 0x53, true, -1);
    }

    [Instruction]
    public void vrsqrtps(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F, 0x52, true, -1);
    }

    [Instruction]
    public void vsqrtpd(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x51, true, -1);
    }

    [Instruction]
    public void vsqrtps(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F, 0x51, true, -1);
    }

    [Instruction]
    public void vucomisd(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F | AVXType.Pp_66, 0x2E, false, -1);
    }

    [Instruction]
    public void vucomiss(Xmm xm, IOperand op)
    {
        OpAVX_X_XM_IMM(xm, op, AVXType.Mm_0F, 0x2E, false, -1);
    }

    [Instruction]
    public void vmovapd(Address addr, Xmm xmm)
    {
        OpAVX_X_XM_IMM(xmm, addr, AVXType.Mm_0F | AVXType.Pp_66, 0x29, true, -1);
    }

    [Instruction]
    public void vmovaps(Address addr, Xmm xmm)
    {
        OpAVX_X_XM_IMM(xmm, addr, AVXType.Mm_0F, 0x29, true, -1);
    }

    [Instruction]
    public void vmovdqa(Address addr, Xmm xmm)
    {
        OpAVX_X_XM_IMM(xmm, addr, AVXType.Mm_0F | AVXType.Pp_66, 0x7F, true, -1);
    }

    [Instruction]
    public void vmovdqu(Address addr, Xmm xmm)
    {
        OpAVX_X_XM_IMM(xmm, addr, AVXType.Mm_0F | AVXType.Pp_F3, 0x7F, true, -1);
    }

    [Instruction]
    public void vmovupd(Address addr, Xmm xmm)
    {
        OpAVX_X_XM_IMM(xmm, addr, AVXType.Mm_0F | AVXType.Pp_66, 0x11, true, -1);
    }

    [Instruction]
    public void vmovups(Address addr, Xmm xmm)
    {
        OpAVX_X_XM_IMM(xmm, addr, AVXType.Mm_0F, 0x11, true, -1);
    }

    [Instruction]
    public void vaddsubpd(Xmm xmm, IOperand op1)
    {
        vaddsubpd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vaddsubpd(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_66, 0xD0, true, -1);
    }

    [Instruction]
    public void vaddsubps(Xmm xmm, IOperand op1)
    {
        vaddsubps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vaddsubps(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_F2, 0xD0, true, -1);
    }

    [Instruction]
    public void vhaddpd(Xmm xmm, IOperand op1)
    {
        vhaddpd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vhaddpd(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_66, 0x7C, true, -1);
    }

    [Instruction]
    public void vhaddps(Xmm xmm, IOperand op1)
    {
        vhaddps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vhaddps(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_F2, 0x7C, true, -1);
    }

    [Instruction]
    public void vhsubpd(Xmm xmm, IOperand op1)
    {
        vhsubpd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vhsubpd(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_66, 0x7D, true, -1);
    }

    [Instruction]
    public void vhsubps(Xmm xmm, IOperand op1)
    {
        vhsubps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vhsubps(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F | AVXType.Pp_F2, 0x7D, true, -1);
    }

    [Instruction]
    public void vaesenc(Xmm xmm, IOperand op1)
    {
        vaesenc(xmm, op1, new Operand());
    }

    [Instruction]
    public void vaesenc(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xDC, false, 0);
    }

    [Instruction]
    public void vaesenclast(Xmm xmm, IOperand op1)
    {
        vaesenclast(xmm, op1, new Operand());
    }

    [Instruction]
    public void vaesenclast(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xDD, false, 0);
    }

    [Instruction]
    public void vaesdec(Xmm xmm, IOperand op1)
    {
        vaesdec(xmm, op1, new Operand());
    }

    [Instruction]
    public void vaesdec(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xDE, false, 0);
    }

    [Instruction]
    public void vaesdeclast(Xmm xmm, IOperand op1)
    {
        vaesdeclast(xmm, op1, new Operand());
    }

    [Instruction]
    public void vaesdeclast(Xmm xmm, IOperand op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xDF, false, 0);
    }

    [Instruction]
    public void vmaskmovps(Xmm xm1, Xmm xm2, Address addr)
    {
        OpAVX_X_X_XM(xm1, xm2, addr, AVXType.Mm_0F38 | AVXType.Pp_66, 0x2C, true, 0);
    }

    [Instruction]
    public void vmaskmovps(Address addr, Xmm xm1, Xmm xm2)
    {
        OpAVX_X_X_XM(xm2, xm1, addr, AVXType.Mm_0F38 | AVXType.Pp_66, 0x2E, true, 0);
    }

    [Instruction]
    public void vmaskmovpd(Xmm xm1, Xmm xm2, Address addr)
    {
        OpAVX_X_X_XM(xm1, xm2, addr, AVXType.Mm_0F38 | AVXType.Pp_66, 0x2D, true, 0);
    }

    [Instruction]
    public void vmaskmovpd(Address addr, Xmm xm1, Xmm xm2)
    {
        OpAVX_X_X_XM(xm2, xm1, addr, AVXType.Mm_0F38 | AVXType.Pp_66, 0x2F, true, 0);
    }

    [Instruction]
    public void cmpeqpd(Xmm x, IOperand op)
    {
        cmppd(x, op, 0);
    }

    [Instruction]
    public void vcmpeqpd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 0);
    }

    [Instruction]
    public void vcmpeqpd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 0);
    }

    [Instruction]
    public void cmpltpd(Xmm x, IOperand op)
    {
        cmppd(x, op, 1);
    }

    [Instruction]
    public void vcmpltpd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 1);
    }

    [Instruction]
    public void vcmpltpd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 1);
    }

    [Instruction]
    public void cmplepd(Xmm x, IOperand op)
    {
        cmppd(x, op, 2);
    }

    [Instruction]
    public void vcmplepd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 2);
    }

    [Instruction]
    public void vcmplepd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 2);
    }

    [Instruction]
    public void cmpunordpd(Xmm x, IOperand op)
    {
        cmppd(x, op, 3);
    }

    [Instruction]
    public void vcmpunordpd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 3);
    }

    [Instruction]
    public void vcmpunordpd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 3);
    }

    [Instruction]
    public void cmpneqpd(Xmm x, IOperand op)
    {
        cmppd(x, op, 4);
    }

    [Instruction]
    public void vcmpneqpd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 4);
    }

    [Instruction]
    public void vcmpneqpd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 4);
    }

    [Instruction]
    public void cmpnltpd(Xmm x, IOperand op)
    {
        cmppd(x, op, 5);
    }

    [Instruction]
    public void vcmpnltpd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 5);
    }

    [Instruction]
    public void vcmpnltpd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 5);
    }

    [Instruction]
    public void cmpnlepd(Xmm x, IOperand op)
    {
        cmppd(x, op, 6);
    }

    [Instruction]
    public void vcmpnlepd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 6);
    }

    [Instruction]
    public void vcmpnlepd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 6);
    }

    [Instruction]
    public void cmpordpd(Xmm x, IOperand op)
    {
        cmppd(x, op, 7);
    }

    [Instruction]
    public void vcmpordpd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 7);
    }

    [Instruction]
    public void vcmpordpd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 7);
    }

    [Instruction]
    public void vcmpeq_uqpd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 8);
    }

    [Instruction]
    public void vcmpeq_uqpd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 8);
    }

    [Instruction]
    public void vcmpngepd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 9);
    }

    [Instruction]
    public void vcmpngepd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 9);
    }

    [Instruction]
    public void vcmpngtpd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 10);
    }

    [Instruction]
    public void vcmpngtpd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 10);
    }

    [Instruction]
    public void vcmpfalsepd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 11);
    }

    [Instruction]
    public void vcmpfalsepd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 11);
    }

    [Instruction]
    public void vcmpneq_oqpd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 12);
    }

    [Instruction]
    public void vcmpneq_oqpd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 12);
    }

    [Instruction]
    public void vcmpgepd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 13);
    }

    [Instruction]
    public void vcmpgepd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 13);
    }

    [Instruction]
    public void vcmpgtpd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 14);
    }

    [Instruction]
    public void vcmpgtpd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 14);
    }

    [Instruction]
    public void vcmptruepd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 15);
    }

    [Instruction]
    public void vcmptruepd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 15);
    }

    [Instruction]
    public void vcmpeq_ospd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 16);
    }

    [Instruction]
    public void vcmpeq_ospd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 16);
    }

    [Instruction]
    public void vcmplt_oqpd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 17);
    }

    [Instruction]
    public void vcmplt_oqpd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 17);
    }

    [Instruction]
    public void vcmple_oqpd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 18);
    }

    [Instruction]
    public void vcmple_oqpd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 18);
    }

    [Instruction]
    public void vcmpunord_spd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 19);
    }

    [Instruction]
    public void vcmpunord_spd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 19);
    }

    [Instruction]
    public void vcmpneq_uspd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 20);
    }

    [Instruction]
    public void vcmpneq_uspd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 20);
    }

    [Instruction]
    public void vcmpnlt_uqpd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 21);
    }

    [Instruction]
    public void vcmpnlt_uqpd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 21);
    }

    [Instruction]
    public void vcmpnle_uqpd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 22);
    }

    [Instruction]
    public void vcmpnle_uqpd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 22);
    }

    [Instruction]
    public void vcmpord_spd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 23);
    }

    [Instruction]
    public void vcmpord_spd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 23);
    }

    [Instruction]
    public void vcmpeq_uspd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 24);
    }

    [Instruction]
    public void vcmpeq_uspd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 24);
    }

    [Instruction]
    public void vcmpnge_uqpd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 25);
    }

    [Instruction]
    public void vcmpnge_uqpd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 25);
    }

    [Instruction]
    public void vcmpngt_uqpd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 26);
    }

    [Instruction]
    public void vcmpngt_uqpd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 26);
    }

    [Instruction]
    public void vcmpfalse_ospd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 27);
    }

    [Instruction]
    public void vcmpfalse_ospd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 27);
    }

    [Instruction]
    public void vcmpneq_ospd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 28);
    }

    [Instruction]
    public void vcmpneq_ospd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 28);
    }

    [Instruction]
    public void vcmpge_oqpd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 29);
    }

    [Instruction]
    public void vcmpge_oqpd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 29);
    }

    [Instruction]
    public void vcmpgt_oqpd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 30);
    }

    [Instruction]
    public void vcmpgt_oqpd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 30);
    }

    [Instruction]
    public void vcmptrue_uspd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmppd(x1, x2, op, 31);
    }

    [Instruction]
    public void vcmptrue_uspd(Xmm x, IOperand op)
    {
        vcmppd(x, op, 31);
    }

    [Instruction]
    public void cmpeqps(Xmm x, IOperand op)
    {
        cmpps(x, op, 0);
    }

    [Instruction]
    public void vcmpeqps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 0);
    }

    [Instruction]
    public void vcmpeqps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 0);
    }

    [Instruction]
    public void cmpltps(Xmm x, IOperand op)
    {
        cmpps(x, op, 1);
    }

    [Instruction]
    public void vcmpltps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 1);
    }

    [Instruction]
    public void vcmpltps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 1);
    }

    [Instruction]
    public void cmpleps(Xmm x, IOperand op)
    {
        cmpps(x, op, 2);
    }

    [Instruction]
    public void vcmpleps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 2);
    }

    [Instruction]
    public void vcmpleps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 2);
    }

    [Instruction]
    public void cmpunordps(Xmm x, IOperand op)
    {
        cmpps(x, op, 3);
    }

    [Instruction]
    public void vcmpunordps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 3);
    }

    [Instruction]
    public void vcmpunordps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 3);
    }

    [Instruction]
    public void cmpneqps(Xmm x, IOperand op)
    {
        cmpps(x, op, 4);
    }

    [Instruction]
    public void vcmpneqps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 4);
    }

    [Instruction]
    public void vcmpneqps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 4);
    }

    [Instruction]
    public void cmpnltps(Xmm x, IOperand op)
    {
        cmpps(x, op, 5);
    }

    [Instruction]
    public void vcmpnltps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 5);
    }

    [Instruction]
    public void vcmpnltps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 5);
    }

    [Instruction]
    public void cmpnleps(Xmm x, IOperand op)
    {
        cmpps(x, op, 6);
    }

    [Instruction]
    public void vcmpnleps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 6);
    }

    [Instruction]
    public void vcmpnleps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 6);
    }

    [Instruction]
    public void cmpordps(Xmm x, IOperand op)
    {
        cmpps(x, op, 7);
    }

    [Instruction]
    public void vcmpordps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 7);
    }

    [Instruction]
    public void vcmpordps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 7);
    }

    [Instruction]
    public void vcmpeq_uqps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 8);
    }

    [Instruction]
    public void vcmpeq_uqps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 8);
    }

    [Instruction]
    public void vcmpngeps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 9);
    }

    [Instruction]
    public void vcmpngeps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 9);
    }

    [Instruction]
    public void vcmpngtps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 10);
    }

    [Instruction]
    public void vcmpngtps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 10);
    }

    [Instruction]
    public void vcmpfalseps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 11);
    }

    [Instruction]
    public void vcmpfalseps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 11);
    }

    [Instruction]
    public void vcmpneq_oqps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 12);
    }

    [Instruction]
    public void vcmpneq_oqps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 12);
    }

    [Instruction]
    public void vcmpgeps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 13);
    }

    [Instruction]
    public void vcmpgeps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 13);
    }

    [Instruction]
    public void vcmpgtps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 14);
    }

    [Instruction]
    public void vcmpgtps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 14);
    }

    [Instruction]
    public void vcmptrueps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 15);
    }

    [Instruction]
    public void vcmptrueps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 15);
    }

    [Instruction]
    public void vcmpeq_osps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 16);
    }

    [Instruction]
    public void vcmpeq_osps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 16);
    }

    [Instruction]
    public void vcmplt_oqps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 17);
    }

    [Instruction]
    public void vcmplt_oqps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 17);
    }

    [Instruction]
    public void vcmple_oqps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 18);
    }

    [Instruction]
    public void vcmple_oqps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 18);
    }

    [Instruction]
    public void vcmpunord_sps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 19);
    }

    [Instruction]
    public void vcmpunord_sps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 19);
    }

    [Instruction]
    public void vcmpneq_usps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 20);
    }

    [Instruction]
    public void vcmpneq_usps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 20);
    }

    [Instruction]
    public void vcmpnlt_uqps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 21);
    }

    [Instruction]
    public void vcmpnlt_uqps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 21);
    }

    [Instruction]
    public void vcmpnle_uqps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 22);
    }

    [Instruction]
    public void vcmpnle_uqps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 22);
    }

    [Instruction]
    public void vcmpord_sps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 23);
    }

    [Instruction]
    public void vcmpord_sps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 23);
    }

    [Instruction]
    public void vcmpeq_usps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 24);
    }

    [Instruction]
    public void vcmpeq_usps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 24);
    }

    [Instruction]
    public void vcmpnge_uqps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 25);
    }

    [Instruction]
    public void vcmpnge_uqps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 25);
    }

    [Instruction]
    public void vcmpngt_uqps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 26);
    }

    [Instruction]
    public void vcmpngt_uqps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 26);
    }

    [Instruction]
    public void vcmpfalse_osps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 27);
    }

    [Instruction]
    public void vcmpfalse_osps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 27);
    }

    [Instruction]
    public void vcmpneq_osps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 28);
    }

    [Instruction]
    public void vcmpneq_osps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 28);
    }

    [Instruction]
    public void vcmpge_oqps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 29);
    }

    [Instruction]
    public void vcmpge_oqps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 29);
    }

    [Instruction]
    public void vcmpgt_oqps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 30);
    }

    [Instruction]
    public void vcmpgt_oqps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 30);
    }

    [Instruction]
    public void vcmptrue_usps(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpps(x1, x2, op, 31);
    }

    [Instruction]
    public void vcmptrue_usps(Xmm x, IOperand op)
    {
        vcmpps(x, op, 31);
    }

    [Instruction]
    public void cmpeqsd(Xmm x, IOperand op)
    {
        cmpsd(x, op, 0);
    }

    [Instruction]
    public void vcmpeqsd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 0);
    }

    [Instruction]
    public void vcmpeqsd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 0);
    }

    [Instruction]
    public void cmpltsd(Xmm x, IOperand op)
    {
        cmpsd(x, op, 1);
    }

    [Instruction]
    public void vcmpltsd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 1);
    }

    [Instruction]
    public void vcmpltsd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 1);
    }

    [Instruction]
    public void cmplesd(Xmm x, IOperand op)
    {
        cmpsd(x, op, 2);
    }

    [Instruction]
    public void vcmplesd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 2);
    }

    [Instruction]
    public void vcmplesd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 2);
    }

    [Instruction]
    public void cmpunordsd(Xmm x, IOperand op)
    {
        cmpsd(x, op, 3);
    }

    [Instruction]
    public void vcmpunordsd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 3);
    }

    [Instruction]
    public void vcmpunordsd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 3);
    }

    [Instruction]
    public void cmpneqsd(Xmm x, IOperand op)
    {
        cmpsd(x, op, 4);
    }

    [Instruction]
    public void vcmpneqsd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 4);
    }

    [Instruction]
    public void vcmpneqsd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 4);
    }

    [Instruction]
    public void cmpnltsd(Xmm x, IOperand op)
    {
        cmpsd(x, op, 5);
    }

    [Instruction]
    public void vcmpnltsd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 5);
    }

    [Instruction]
    public void vcmpnltsd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 5);
    }

    [Instruction]
    public void cmpnlesd(Xmm x, IOperand op)
    {
        cmpsd(x, op, 6);
    }

    [Instruction]
    public void vcmpnlesd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 6);
    }

    [Instruction]
    public void vcmpnlesd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 6);
    }

    [Instruction]
    public void cmpordsd(Xmm x, IOperand op)
    {
        cmpsd(x, op, 7);
    }

    [Instruction]
    public void vcmpordsd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 7);
    }

    [Instruction]
    public void vcmpordsd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 7);
    }

    [Instruction]
    public void vcmpeq_uqsd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 8);
    }

    [Instruction]
    public void vcmpeq_uqsd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 8);
    }

    [Instruction]
    public void vcmpngesd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 9);
    }

    [Instruction]
    public void vcmpngesd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 9);
    }

    [Instruction]
    public void vcmpngtsd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 10);
    }

    [Instruction]
    public void vcmpngtsd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 10);
    }

    [Instruction]
    public void vcmpfalsesd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 11);
    }

    [Instruction]
    public void vcmpfalsesd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 11);
    }

    [Instruction]
    public void vcmpneq_oqsd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 12);
    }

    [Instruction]
    public void vcmpneq_oqsd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 12);
    }

    [Instruction]
    public void vcmpgesd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 13);
    }

    [Instruction]
    public void vcmpgesd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 13);
    }

    [Instruction]
    public void vcmpgtsd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 14);
    }

    [Instruction]
    public void vcmpgtsd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 14);
    }

    [Instruction]
    public void vcmptruesd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 15);
    }

    [Instruction]
    public void vcmptruesd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 15);
    }

    [Instruction]
    public void vcmpeq_ossd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 16);
    }

    [Instruction]
    public void vcmpeq_ossd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 16);
    }

    [Instruction]
    public void vcmplt_oqsd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 17);
    }

    [Instruction]
    public void vcmplt_oqsd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 17);
    }

    [Instruction]
    public void vcmple_oqsd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 18);
    }

    [Instruction]
    public void vcmple_oqsd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 18);
    }

    [Instruction]
    public void vcmpunord_ssd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 19);
    }

    [Instruction]
    public void vcmpunord_ssd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 19);
    }

    [Instruction]
    public void vcmpneq_ussd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 20);
    }

    [Instruction]
    public void vcmpneq_ussd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 20);
    }

    [Instruction]
    public void vcmpnlt_uqsd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 21);
    }

    [Instruction]
    public void vcmpnlt_uqsd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 21);
    }

    [Instruction]
    public void vcmpnle_uqsd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 22);
    }

    [Instruction]
    public void vcmpnle_uqsd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 22);
    }

    [Instruction]
    public void vcmpord_ssd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 23);
    }

    [Instruction]
    public void vcmpord_ssd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 23);
    }

    [Instruction]
    public void vcmpeq_ussd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 24);
    }

    [Instruction]
    public void vcmpeq_ussd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 24);
    }

    [Instruction]
    public void vcmpnge_uqsd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 25);
    }

    [Instruction]
    public void vcmpnge_uqsd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 25);
    }

    [Instruction]
    public void vcmpngt_uqsd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 26);
    }

    [Instruction]
    public void vcmpngt_uqsd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 26);
    }

    [Instruction]
    public void vcmpfalse_ossd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 27);
    }

    [Instruction]
    public void vcmpfalse_ossd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 27);
    }

    [Instruction]
    public void vcmpneq_ossd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 28);
    }

    [Instruction]
    public void vcmpneq_ossd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 28);
    }

    [Instruction]
    public void vcmpge_oqsd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 29);
    }

    [Instruction]
    public void vcmpge_oqsd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 29);
    }

    [Instruction]
    public void vcmpgt_oqsd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 30);
    }

    [Instruction]
    public void vcmpgt_oqsd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 30);
    }

    [Instruction]
    public void vcmptrue_ussd(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpsd(x1, x2, op, 31);
    }

    [Instruction]
    public void vcmptrue_ussd(Xmm x, IOperand op)
    {
        vcmpsd(x, op, 31);
    }

    [Instruction]
    public void cmpeqss(Xmm x, IOperand op)
    {
        cmpss(x, op, 0);
    }

    [Instruction]
    public void vcmpeqss(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 0);
    }

    [Instruction]
    public void vcmpeqss(Xmm x, IOperand op)
    {
        vcmpss(x, op, 0);
    }

    [Instruction]
    public void cmpltss(Xmm x, IOperand op)
    {
        cmpss(x, op, 1);
    }

    [Instruction]
    public void vcmpltss(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 1);
    }

    [Instruction]
    public void vcmpltss(Xmm x, IOperand op)
    {
        vcmpss(x, op, 1);
    }

    [Instruction]
    public void cmpless(Xmm x, IOperand op)
    {
        cmpss(x, op, 2);
    }

    [Instruction]
    public void vcmpless(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 2);
    }

    [Instruction]
    public void vcmpless(Xmm x, IOperand op)
    {
        vcmpss(x, op, 2);
    }

    [Instruction]
    public void cmpunordss(Xmm x, IOperand op)
    {
        cmpss(x, op, 3);
    }

    [Instruction]
    public void vcmpunordss(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 3);
    }

    [Instruction]
    public void vcmpunordss(Xmm x, IOperand op)
    {
        vcmpss(x, op, 3);
    }

    [Instruction]
    public void cmpneqss(Xmm x, IOperand op)
    {
        cmpss(x, op, 4);
    }

    [Instruction]
    public void vcmpneqss(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 4);
    }

    [Instruction]
    public void vcmpneqss(Xmm x, IOperand op)
    {
        vcmpss(x, op, 4);
    }

    [Instruction]
    public void cmpnltss(Xmm x, IOperand op)
    {
        cmpss(x, op, 5);
    }

    [Instruction]
    public void vcmpnltss(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 5);
    }

    [Instruction]
    public void vcmpnltss(Xmm x, IOperand op)
    {
        vcmpss(x, op, 5);
    }

    [Instruction]
    public void cmpnless(Xmm x, IOperand op)
    {
        cmpss(x, op, 6);
    }

    [Instruction]
    public void vcmpnless(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 6);
    }

    [Instruction]
    public void vcmpnless(Xmm x, IOperand op)
    {
        vcmpss(x, op, 6);
    }

    [Instruction]
    public void cmpordss(Xmm x, IOperand op)
    {
        cmpss(x, op, 7);
    }

    [Instruction]
    public void vcmpordss(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 7);
    }

    [Instruction]
    public void vcmpordss(Xmm x, IOperand op)
    {
        vcmpss(x, op, 7);
    }

    [Instruction]
    public void vcmpeq_uqss(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 8);
    }

    [Instruction]
    public void vcmpeq_uqss(Xmm x, IOperand op)
    {
        vcmpss(x, op, 8);
    }

    [Instruction]
    public void vcmpngess(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 9);
    }

    [Instruction]
    public void vcmpngess(Xmm x, IOperand op)
    {
        vcmpss(x, op, 9);
    }

    [Instruction]
    public void vcmpngtss(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 10);
    }

    [Instruction]
    public void vcmpngtss(Xmm x, IOperand op)
    {
        vcmpss(x, op, 10);
    }

    [Instruction]
    public void vcmpfalsess(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 11);
    }

    [Instruction]
    public void vcmpfalsess(Xmm x, IOperand op)
    {
        vcmpss(x, op, 11);
    }

    [Instruction]
    public void vcmpneq_oqss(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 12);
    }

    [Instruction]
    public void vcmpneq_oqss(Xmm x, IOperand op)
    {
        vcmpss(x, op, 12);
    }

    [Instruction]
    public void vcmpgess(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 13);
    }

    [Instruction]
    public void vcmpgess(Xmm x, IOperand op)
    {
        vcmpss(x, op, 13);
    }

    [Instruction]
    public void vcmpgtss(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 14);
    }

    [Instruction]
    public void vcmpgtss(Xmm x, IOperand op)
    {
        vcmpss(x, op, 14);
    }

    [Instruction]
    public void vcmptruess(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 15);
    }

    [Instruction]
    public void vcmptruess(Xmm x, IOperand op)
    {
        vcmpss(x, op, 15);
    }

    [Instruction]
    public void vcmpeq_osss(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 16);
    }

    [Instruction]
    public void vcmpeq_osss(Xmm x, IOperand op)
    {
        vcmpss(x, op, 16);
    }

    [Instruction]
    public void vcmplt_oqss(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 17);
    }

    [Instruction]
    public void vcmplt_oqss(Xmm x, IOperand op)
    {
        vcmpss(x, op, 17);
    }

    [Instruction]
    public void vcmple_oqss(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 18);
    }

    [Instruction]
    public void vcmple_oqss(Xmm x, IOperand op)
    {
        vcmpss(x, op, 18);
    }

    [Instruction]
    public void vcmpunord_sss(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 19);
    }

    [Instruction]
    public void vcmpunord_sss(Xmm x, IOperand op)
    {
        vcmpss(x, op, 19);
    }

    [Instruction]
    public void vcmpneq_usss(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 20);
    }

    [Instruction]
    public void vcmpneq_usss(Xmm x, IOperand op)
    {
        vcmpss(x, op, 20);
    }

    [Instruction]
    public void vcmpnlt_uqss(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 21);
    }

    [Instruction]
    public void vcmpnlt_uqss(Xmm x, IOperand op)
    {
        vcmpss(x, op, 21);
    }

    [Instruction]
    public void vcmpnle_uqss(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 22);
    }

    [Instruction]
    public void vcmpnle_uqss(Xmm x, IOperand op)
    {
        vcmpss(x, op, 22);
    }

    [Instruction]
    public void vcmpord_sss(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 23);
    }

    [Instruction]
    public void vcmpord_sss(Xmm x, IOperand op)
    {
        vcmpss(x, op, 23);
    }

    [Instruction]
    public void vcmpeq_usss(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 24);
    }

    [Instruction]
    public void vcmpeq_usss(Xmm x, IOperand op)
    {
        vcmpss(x, op, 24);
    }

    [Instruction]
    public void vcmpnge_uqss(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 25);
    }

    [Instruction]
    public void vcmpnge_uqss(Xmm x, IOperand op)
    {
        vcmpss(x, op, 25);
    }

    [Instruction]
    public void vcmpngt_uqss(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 26);
    }

    [Instruction]
    public void vcmpngt_uqss(Xmm x, IOperand op)
    {
        vcmpss(x, op, 26);
    }

    [Instruction]
    public void vcmpfalse_osss(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 27);
    }

    [Instruction]
    public void vcmpfalse_osss(Xmm x, IOperand op)
    {
        vcmpss(x, op, 27);
    }

    [Instruction]
    public void vcmpneq_osss(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 28);
    }

    [Instruction]
    public void vcmpneq_osss(Xmm x, IOperand op)
    {
        vcmpss(x, op, 28);
    }

    [Instruction]
    public void vcmpge_oqss(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 29);
    }

    [Instruction]
    public void vcmpge_oqss(Xmm x, IOperand op)
    {
        vcmpss(x, op, 29);
    }

    [Instruction]
    public void vcmpgt_oqss(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 30);
    }

    [Instruction]
    public void vcmpgt_oqss(Xmm x, IOperand op)
    {
        vcmpss(x, op, 30);
    }

    [Instruction]
    public void vcmptrue_usss(Xmm x1, Xmm x2, IOperand op)
    {
        vcmpss(x1, x2, op, 31);
    }

    [Instruction]
    public void vcmptrue_usss(Xmm x, IOperand op)
    {
        vcmpss(x, op, 31);
    }

    [Instruction]
    public void vmovhpd(Xmm x, IOperand op1)
    {
        vmovhpd(x, op1, new Operand());
    }

    [Instruction]
    public void vmovhpd(Xmm x, IOperand op1, IOperand op2)
    {
        if (!op2.IsNone() && !op2.IsMEM())
        {
            throw new ArgumentException("bad combination", "op2");
        }
        OpAVX_X_X_XM(x, op1, op2, AVXType.Mm_0F | AVXType.Pp_66, 0x16, false);
    }

    [Instruction]
    public void vmovhpd(Address addr, Xmm x)
    {
        OpAVX_X_X_XM(x, xmm0, addr, AVXType.Mm_0F | AVXType.Pp_66, 0x17, false);
    }

    [Instruction]
    public void vmovhps(Xmm x, IOperand op1)
    {
        vmovhps(x, op1, new Operand());
    }

    [Instruction]
    public void vmovhps(Xmm x, IOperand op1, IOperand op2)
    {
        if (!op2.IsNone() && !op2.IsMEM())
        {
            throw new ArgumentException("bad combination", "op2");
        }
        OpAVX_X_X_XM(x, op1, op2, AVXType.Mm_0F, 0x16, false);
    }

    [Instruction]
    public void vmovhps(Address addr, Xmm x)
    {
        OpAVX_X_X_XM(x, xmm0, addr, AVXType.Mm_0F, 0x17, false);
    }

    [Instruction]
    public void vmovlpd(Xmm x, IOperand op1)
    {
        vmovlpd(x, op1, new Operand());
    }

    [Instruction]
    public void vmovlpd(Xmm x, IOperand op1, IOperand op2)
    {
        if (!op2.IsNone() && !op2.IsMEM())
        {
            throw new ArgumentException("bad combination", "op2");
        }
        OpAVX_X_X_XM(x, op1, op2, AVXType.Mm_0F | AVXType.Pp_66, 0x12, false);
    }

    [Instruction]
    public void vmovlpd(Address addr, Xmm x)
    {
        OpAVX_X_X_XM(x, xmm0, addr, AVXType.Mm_0F | AVXType.Pp_66, 0x13, false);
    }

    [Instruction]
    public void vmovlps(Xmm x, IOperand op1)
    {
        vmovlps(x, op1, new Operand());
    }

    [Instruction]
    public void vmovlps(Xmm x, IOperand op1, IOperand op2)
    {
        if (!op2.IsNone() && !op2.IsMEM())
        {
            throw new ArgumentException("bad combination", "op2");
        }
        OpAVX_X_X_XM(x, op1, op2, AVXType.Mm_0F, 0x12, false);
    }

    [Instruction]
    public void vmovlps(Address addr, Xmm x)
    {
        OpAVX_X_X_XM(x, xmm0, addr, AVXType.Mm_0F, 0x13, false);
    }

    [Instruction]
    public void vfmadd132pd(Xmm xmm, Xmm op1)
    {
        vfmadd132pd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmadd132pd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0x98, true, 1);
    }

    [Instruction]
    public void vfmadd213pd(Xmm xmm, Xmm op1)
    {
        vfmadd213pd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmadd213pd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xA8, true, 1);
    }

    [Instruction]
    public void vfmadd231pd(Xmm xmm, Xmm op1)
    {
        vfmadd231pd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmadd231pd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xB8, true, 1);
    }

    [Instruction]
    public void vfmadd132ps(Xmm xmm, Xmm op1)
    {
        vfmadd132ps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmadd132ps(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0x98, true, 0);
    }

    [Instruction]
    public void vfmadd213ps(Xmm xmm, Xmm op1)
    {
        vfmadd213ps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmadd213ps(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xA8, true, 0);
    }

    [Instruction]
    public void vfmadd231ps(Xmm xmm, Xmm op1)
    {
        vfmadd231ps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmadd231ps(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xB8, true, 0);
    }

    [Instruction]
    public void vfmadd132sd(Xmm xmm, Xmm op1)
    {
        vfmadd132sd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmadd132sd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0x99, false, 1);
    }

    [Instruction]
    public void vfmadd213sd(Xmm xmm, Xmm op1)
    {
        vfmadd213sd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmadd213sd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xA9, false, 1);
    }

    [Instruction]
    public void vfmadd231sd(Xmm xmm, Xmm op1)
    {
        vfmadd231sd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmadd231sd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xB9, false, 1);
    }

    [Instruction]
    public void vfmadd132ss(Xmm xmm, Xmm op1)
    {
        vfmadd132ss(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmadd132ss(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0x99, false, 0);
    }

    [Instruction]
    public void vfmadd213ss(Xmm xmm, Xmm op1)
    {
        vfmadd213ss(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmadd213ss(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xA9, false, 0);
    }

    [Instruction]
    public void vfmadd231ss(Xmm xmm, Xmm op1)
    {
        vfmadd231ss(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmadd231ss(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xB9, false, 0);
    }

    [Instruction]
    public void vfmaddsub132pd(Xmm xmm, Xmm op1)
    {
        vfmaddsub132pd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmaddsub132pd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0x96, true, 1);
    }

    [Instruction]
    public void vfmaddsub213pd(Xmm xmm, Xmm op1)
    {
        vfmaddsub213pd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmaddsub213pd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xA6, true, 1);
    }

    [Instruction]
    public void vfmaddsub231pd(Xmm xmm, Xmm op1)
    {
        vfmaddsub231pd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmaddsub231pd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xB6, true, 1);
    }

    [Instruction]
    public void vfmaddsub132ps(Xmm xmm, Xmm op1)
    {
        vfmaddsub132ps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmaddsub132ps(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0x96, true, 0);
    }

    [Instruction]
    public void vfmaddsub213ps(Xmm xmm, Xmm op1)
    {
        vfmaddsub213ps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmaddsub213ps(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xA6, true, 0);
    }

    [Instruction]
    public void vfmaddsub231ps(Xmm xmm, Xmm op1)
    {
        vfmaddsub231ps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmaddsub231ps(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xB6, true, 0);
    }

    [Instruction]
    public void vfmsubadd132pd(Xmm xmm, Xmm op1)
    {
        vfmsubadd132pd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmsubadd132pd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0x97, true, 1);
    }

    [Instruction]
    public void vfmsubadd213pd(Xmm xmm, Xmm op1)
    {
        vfmsubadd213pd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmsubadd213pd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xA7, true, 1);
    }

    [Instruction]
    public void vfmsubadd231pd(Xmm xmm, Xmm op1)
    {
        vfmsubadd231pd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmsubadd231pd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xB7, true, 1);
    }

    [Instruction]
    public void vfmsubadd132ps(Xmm xmm, Xmm op1)
    {
        vfmsubadd132ps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmsubadd132ps(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0x97, true, 0);
    }

    [Instruction]
    public void vfmsubadd213ps(Xmm xmm, Xmm op1)
    {
        vfmsubadd213ps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmsubadd213ps(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xA7, true, 0);
    }

    [Instruction]
    public void vfmsubadd231ps(Xmm xmm, Xmm op1)
    {
        vfmsubadd231ps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmsubadd231ps(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xB7, true, 0);
    }

    [Instruction]
    public void vfmsub132pd(Xmm xmm, Xmm op1)
    {
        vfmsub132pd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmsub132pd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0x9A, true, 1);
    }

    [Instruction]
    public void vfmsub213pd(Xmm xmm, Xmm op1)
    {
        vfmsub213pd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmsub213pd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xAA, true, 1);
    }

    [Instruction]
    public void vfmsub231pd(Xmm xmm, Xmm op1)
    {
        vfmsub231pd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmsub231pd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xBA, true, 1);
    }

    [Instruction]
    public void vfmsub132ps(Xmm xmm, Xmm op1)
    {
        vfmsub132ps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmsub132ps(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0x9A, true, 0);
    }

    [Instruction]
    public void vfmsub213ps(Xmm xmm, Xmm op1)
    {
        vfmsub213ps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmsub213ps(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xAA, true, 0);
    }

    [Instruction]
    public void vfmsub231ps(Xmm xmm, Xmm op1)
    {
        vfmsub231ps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmsub231ps(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xBA, true, 0);
    }

    [Instruction]
    public void vfmsub132sd(Xmm xmm, Xmm op1)
    {
        vfmsub132sd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmsub132sd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0x9B, false, 1);
    }

    [Instruction]
    public void vfmsub213sd(Xmm xmm, Xmm op1)
    {
        vfmsub213sd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmsub213sd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xAB, false, 1);
    }

    [Instruction]
    public void vfmsub231sd(Xmm xmm, Xmm op1)
    {
        vfmsub231sd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmsub231sd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xBB, false, 1);
    }

    [Instruction]
    public void vfmsub132ss(Xmm xmm, Xmm op1)
    {
        vfmsub132ss(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmsub132ss(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0x9B, false, 0);
    }

    [Instruction]
    public void vfmsub213ss(Xmm xmm, Xmm op1)
    {
        vfmsub213ss(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmsub213ss(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xAB, false, 0);
    }

    [Instruction]
    public void vfmsub231ss(Xmm xmm, Xmm op1)
    {
        vfmsub231ss(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfmsub231ss(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xBB, false, 0);
    }

    [Instruction]
    public void vfnmadd132pd(Xmm xmm, Xmm op1)
    {
        vfnmadd132pd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfnmadd132pd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0x9C, true, 1);
    }

    [Instruction]
    public void vfnmadd213pd(Xmm xmm, Xmm op1)
    {
        vfnmadd213pd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfnmadd213pd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xAC, true, 1);
    }

    [Instruction]
    public void vfnmadd231pd(Xmm xmm, Xmm op1)
    {
        vfnmadd231pd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfnmadd231pd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xBC, true, 1);
    }

    [Instruction]
    public void vfnmadd132ps(Xmm xmm, Xmm op1)
    {
        vfnmadd132ps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfnmadd132ps(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0x9C, true, 0);
    }

    [Instruction]
    public void vfnmadd213ps(Xmm xmm, Xmm op1)
    {
        vfnmadd213ps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfnmadd213ps(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xAC, true, 0);
    }

    [Instruction]
    public void vfnmadd231ps(Xmm xmm, Xmm op1)
    {
        vfnmadd231ps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfnmadd231ps(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xBC, true, 0);
    }

    [Instruction]
    public void vfnmadd132sd(Xmm xmm, Xmm op1)
    {
        vfnmadd132sd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfnmadd132sd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0x9D, false, 1);
    }

    [Instruction]
    public void vfnmadd213sd(Xmm xmm, Xmm op1)
    {
        vfnmadd213sd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfnmadd213sd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xAD, false, 1);
    }

    [Instruction]
    public void vfnmadd231sd(Xmm xmm, Xmm op1)
    {
        vfnmadd231sd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfnmadd231sd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xBD, false, 1);
    }

    [Instruction]
    public void vfnmadd132ss(Xmm xmm, Xmm op1)
    {
        vfnmadd132ss(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfnmadd132ss(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0x9D, false, 0);
    }

    [Instruction]
    public void vfnmadd213ss(Xmm xmm, Xmm op1)
    {
        vfnmadd213ss(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfnmadd213ss(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xAD, false, 0);
    }

    [Instruction]
    public void vfnmadd231ss(Xmm xmm, Xmm op1)
    {
        vfnmadd231ss(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfnmadd231ss(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xBD, false, 0);
    }

    [Instruction]
    public void vfnmsub132pd(Xmm xmm, Xmm op1)
    {
        vfnmsub132pd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfnmsub132pd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0x9E, true, 1);
    }

    [Instruction]
    public void vfnmsub213pd(Xmm xmm, Xmm op1)
    {
        vfnmsub213pd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfnmsub213pd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xAE, true, 1);
    }

    [Instruction]
    public void vfnmsub231pd(Xmm xmm, Xmm op1)
    {
        vfnmsub231pd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfnmsub231pd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xBE, true, 1);
    }

    [Instruction]
    public void vfnmsub132ps(Xmm xmm, Xmm op1)
    {
        vfnmsub132ps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfnmsub132ps(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0x9E, true, 0);
    }

    [Instruction]
    public void vfnmsub213ps(Xmm xmm, Xmm op1)
    {
        vfnmsub213ps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfnmsub213ps(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xAE, true, 0);
    }

    [Instruction]
    public void vfnmsub231ps(Xmm xmm, Xmm op1)
    {
        vfnmsub231ps(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfnmsub231ps(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xBE, true, 0);
    }

    [Instruction]
    public void vfnmsub132sd(Xmm xmm, Xmm op1)
    {
        vfnmsub132sd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfnmsub132sd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0x9F, false, 1);
    }

    [Instruction]
    public void vfnmsub213sd(Xmm xmm, Xmm op1)
    {
        vfnmsub213sd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfnmsub213sd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xAF, false, 1);
    }

    [Instruction]
    public void vfnmsub231sd(Xmm xmm, Xmm op1)
    {
        vfnmsub231sd(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfnmsub231sd(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xBF, false, 1);
    }

    [Instruction]
    public void vfnmsub132ss(Xmm xmm, Xmm op1)
    {
        vfnmsub132ss(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfnmsub132ss(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0x9F, false, 0);
    }

    [Instruction]
    public void vfnmsub213ss(Xmm xmm, Xmm op1)
    {
        vfnmsub213ss(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfnmsub213ss(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xAF, false, 0);
    }

    [Instruction]
    public void vfnmsub231ss(Xmm xmm, Xmm op1)
    {
        vfnmsub231ss(xmm, op1, new Operand());
    }

    [Instruction]
    public void vfnmsub231ss(Xmm xmm, Xmm op1, IOperand op2)
    {
        OpAVX_X_X_XM(xmm, op1, op2, AVXType.Mm_0F38 | AVXType.Pp_66, 0xBF, false, 0);
    }

    [Instruction]
    public void vaesimc(Xmm x, IOperand op)
    {
        OpAVX_X_XM_IMM(x, op, AVXType.Mm_0F38 | AVXType.Pp_66, 0xDB, false, 0);
    }

    [Instruction]
    public void vbroadcastf128(Ymm y, Address addr)
    {
        OpAVX_X_XM_IMM(y, addr, AVXType.Mm_0F38 | AVXType.Pp_66, 0x1A, true, 0);
    }

    [Instruction]
    public void vbroadcastsd(Ymm y, Address addr)
    {
        OpAVX_X_XM_IMM(y, addr, AVXType.Mm_0F38 | AVXType.Pp_66, 0x19, true, 0);
    }

    [Instruction]
    public void vbroadcastss(Xmm x, Address addr)
    {
        OpAVX_X_XM_IMM(x, addr, AVXType.Mm_0F38 | AVXType.Pp_66, 0x18, true, 0);
    }

    [Instruction]
    public void vextractf128(IOperand op, Ymm y, byte imm)
    {
        OpAVX_X_X_XMcvt(y, y.IsXMM() ? xmm0 : ymm0, op, op.IsXMM(), Operand.KindType.YMM, AVXType.Mm_0F3A | AVXType.Pp_66, 0x19, true, 0);
        Db(imm);
    }

    [Instruction]
    public void vextractps(IOperand op, Xmm x, byte imm)
    {
        if (!(op.IsREG(32) || op.IsMEM()) || x.IsYMM())
        {
            throw new ArgumentException("bad combination");
        }
        OpAVX_X_X_XMcvt(x, x.IsXMM() ? xmm0 : ymm0, op, op.IsREG(), Operand.KindType.XMM, AVXType.Mm_0F3A | AVXType.Pp_66, 0x17, false, 0);
        Db(imm);
    }

    [Instruction]
    public void vinsertf128(Ymm y1, Ymm y2, IOperand op, byte imm)
    {
        OpAVX_X_X_XMcvt(y1, y2, op, op.IsXMM(), Operand.KindType.YMM, AVXType.Mm_0F3A | AVXType.Pp_66, 0x18, true, 0);
        Db(imm);
    }

    [Instruction]
    public void vperm2f128(Ymm y1, Ymm y2, IOperand op, byte imm)
    {
        OpAVX_X_X_XM(y1, y2, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x06, true, 0);
        Db(imm);
    }

    [Instruction]
    public void vlddqu(Xmm x, Address addr)
    {
        OpAVX_X_X_XM(x, x.IsXMM() ? xmm0 : ymm0, addr, AVXType.Mm_0F | AVXType.Pp_F2, 0xF0, true, 0);
    }

    [Instruction]
    public void vldmxcsr(Address addr)
    {
        OpAVX_X_X_XM(xmm2, xmm0, addr, AVXType.Mm_0F, 0xAE, false, -1);
    }

    [Instruction]
    public void vstmxcsr(Address addr)
    {
        OpAVX_X_X_XM(xmm3, xmm0, addr, AVXType.Mm_0F, 0xAE, false, -1);
    }

    [Instruction]
    public void vmaskmovdqu(Xmm x1, Xmm x2)
    {
        OpAVX_X_X_XM(x1, xmm0, x2, AVXType.Mm_0F | AVXType.Pp_66, 0xF7, false, -1);
    }

    [Instruction]
    public void vpextrb(IOperand op, Xmm x, byte imm)
    {
        if (!op.IsREG(i32e) && !op.IsMEM())
        {
            throw new ArgumentException("bad combination");
        }
        OpAVX_X_X_XMcvt(x, xmm0, op, !op.IsMEM(), Operand.KindType.XMM, AVXType.Mm_0F3A | AVXType.Pp_66, 0x14, false);
        Db(imm);
    }

    [Instruction]
    public void vpextrw(Reg r, Xmm x, byte imm)
    {
        OpAVX_X_X_XM(new Xmm(r.IDX), xmm0, x, AVXType.Mm_0F | AVXType.Pp_66, 0xC5, false, r.IsBit(64) ? 1 : 0);
        Db(imm);
    }

    [Instruction]
    public void vpextrw(Address addr, Xmm x, byte imm)
    {
        OpAVX_X_X_XM(x, xmm0, addr, AVXType.Mm_0F3A | AVXType.Pp_66, 0x15, false);
        Db(imm);
    }

    [Instruction]
    public void vpextrd(IOperand op, Xmm x, byte imm)
    {
        if (!op.IsREG(32) && !op.IsMEM())
        {
            throw new ArgumentException("bad combination");
        }
        OpAVX_X_X_XMcvt(x, xmm0, op, !op.IsMEM(), Operand.KindType.XMM, AVXType.Mm_0F3A | AVXType.Pp_66, 0x16, false, 0);
        Db(imm);
    }

    [Instruction]
    public void vpinsrb(Xmm x1, Xmm x2, IOperand op, byte imm)
    {
        if (!op.IsREG(32) && !op.IsMEM())
        {
            throw new ArgumentException("bad combination");
        }
        OpAVX_X_X_XMcvt(x1, x2, op, !op.IsMEM(), Operand.KindType.XMM, AVXType.Mm_0F3A | AVXType.Pp_66, 0x20, false);
        Db(imm);
    }

    [Instruction]
    public void vpinsrb(Xmm x, IOperand op, byte imm)
    {
        if (!op.IsREG(32) && !op.IsMEM())
        {
            throw new ArgumentException("bad combination");
        }
        OpAVX_X_X_XMcvt(x, x, op, !op.IsMEM(), Operand.KindType.XMM, AVXType.Mm_0F3A | AVXType.Pp_66, 0x20, false);
        Db(imm);
    }

    [Instruction]
    public void vpinsrw(Xmm x1, Xmm x2, IOperand op, byte imm)
    {
        if (!op.IsREG(32) && !op.IsMEM())
        {
            throw new ArgumentException("bad combination");
        }
        OpAVX_X_X_XMcvt(x1, x2, op, !op.IsMEM(), Operand.KindType.XMM, AVXType.Mm_0F | AVXType.Pp_66, 0xC4, false);
        Db(imm);
    }

    [Instruction]
    public void vpinsrw(Xmm x, IOperand op, byte imm)
    {
        if (!op.IsREG(32) && !op.IsMEM())
        {
            throw new ArgumentException("bad combination");
        }
        OpAVX_X_X_XMcvt(x, x, op, !op.IsMEM(), Operand.KindType.XMM, AVXType.Mm_0F | AVXType.Pp_66, 0xC4, false);
        Db(imm);
    }

    [Instruction]
    public void vpinsrd(Xmm x1, Xmm x2, IOperand op, byte imm)
    {
        if (!op.IsREG(32) && !op.IsMEM())
        {
            throw new ArgumentException("bad combination");
        }
        OpAVX_X_X_XMcvt(x1, x2, op, !op.IsMEM(), Operand.KindType.XMM, AVXType.Mm_0F3A | AVXType.Pp_66, 0x22, false, 0);
        Db(imm);
    }

    [Instruction]
    public void vpinsrd(Xmm x, IOperand op, byte imm)
    {
        if (!op.IsREG(32) && !op.IsMEM())
        {
            throw new ArgumentException("bad combination");
        }
        OpAVX_X_X_XMcvt(x, x, op, !op.IsMEM(), Operand.KindType.XMM, AVXType.Mm_0F3A | AVXType.Pp_66, 0x22, false, 0);
        Db(imm);
    }

    [Instruction]
    public void vpmovmskb(Reg32e r, Xmm x)
    {
        if (x.IsYMM())
        {
            throw new ArgumentException("bad combination");
        }
        OpAVX_X_X_XM(new Xmm(r.IDX), xmm0, x, AVXType.Mm_0F | AVXType.Pp_66, 0xD7, false);
    }

    [Instruction]
    public void vpslldq(Xmm x1, Xmm x2, byte imm)
    {
        OpAVX_X_X_XM(xmm7, x1, x2, AVXType.Mm_0F | AVXType.Pp_66, 0x73, false);
        Db(imm);
    }

    [Instruction]
    public void vpslldq(Xmm x, byte imm)
    {
        OpAVX_X_X_XM(xmm7, x, x, AVXType.Mm_0F | AVXType.Pp_66, 0x73, false);
        Db(imm);
    }

    [Instruction]
    public void vpsrldq(Xmm x1, Xmm x2, byte imm)
    {
        OpAVX_X_X_XM(xmm3, x1, x2, AVXType.Mm_0F | AVXType.Pp_66, 0x73, false);
        Db(imm);
    }

    [Instruction]
    public void vpsrldq(Xmm x, byte imm)
    {
        OpAVX_X_X_XM(xmm3, x, x, AVXType.Mm_0F | AVXType.Pp_66, 0x73, false);
        Db(imm);
    }

    [Instruction]
    public void vpsllw(Xmm x1, Xmm x2, byte imm)
    {
        OpAVX_X_X_XM(xmm6, x1, x2, AVXType.Mm_0F | AVXType.Pp_66, 0x71, false);
        Db(imm);
    }

    [Instruction]
    public void vpsllw(Xmm x, byte imm)
    {
        OpAVX_X_X_XM(xmm6, x, x, AVXType.Mm_0F | AVXType.Pp_66, 0x71, false);
        Db(imm);
    }

    [Instruction]
    public void vpslld(Xmm x1, Xmm x2, byte imm)
    {
        OpAVX_X_X_XM(xmm6, x1, x2, AVXType.Mm_0F | AVXType.Pp_66, 0x72, false);
        Db(imm);
    }

    [Instruction]
    public void vpslld(Xmm x, byte imm)
    {
        OpAVX_X_X_XM(xmm6, x, x, AVXType.Mm_0F | AVXType.Pp_66, 0x72, false);
        Db(imm);
    }

    [Instruction]
    public void vpsllq(Xmm x1, Xmm x2, byte imm)
    {
        OpAVX_X_X_XM(xmm6, x1, x2, AVXType.Mm_0F | AVXType.Pp_66, 0x73, false);
        Db(imm);
    }

    [Instruction]
    public void vpsllq(Xmm x, byte imm)
    {
        OpAVX_X_X_XM(xmm6, x, x, AVXType.Mm_0F | AVXType.Pp_66, 0x73, false);
        Db(imm);
    }

    [Instruction]
    public void vpsraw(Xmm x1, Xmm x2, byte imm)
    {
        OpAVX_X_X_XM(xmm4, x1, x2, AVXType.Mm_0F | AVXType.Pp_66, 0x71, false);
        Db(imm);
    }

    [Instruction]
    public void vpsraw(Xmm x, byte imm)
    {
        OpAVX_X_X_XM(xmm4, x, x, AVXType.Mm_0F | AVXType.Pp_66, 0x71, false);
        Db(imm);
    }

    [Instruction]
    public void vpsrad(Xmm x1, Xmm x2, byte imm)
    {
        OpAVX_X_X_XM(xmm4, x1, x2, AVXType.Mm_0F | AVXType.Pp_66, 0x72, false);
        Db(imm);
    }

    [Instruction]
    public void vpsrad(Xmm x, byte imm)
    {
        OpAVX_X_X_XM(xmm4, x, x, AVXType.Mm_0F | AVXType.Pp_66, 0x72, false);
        Db(imm);
    }

    [Instruction]
    public void vpsrlw(Xmm x1, Xmm x2, byte imm)
    {
        OpAVX_X_X_XM(xmm2, x1, x2, AVXType.Mm_0F | AVXType.Pp_66, 0x71, false);
        Db(imm);
    }

    [Instruction]
    public void vpsrlw(Xmm x, byte imm)
    {
        OpAVX_X_X_XM(xmm2, x, x, AVXType.Mm_0F | AVXType.Pp_66, 0x71, false);
        Db(imm);
    }

    [Instruction]
    public void vpsrld(Xmm x1, Xmm x2, byte imm)
    {
        OpAVX_X_X_XM(xmm2, x1, x2, AVXType.Mm_0F | AVXType.Pp_66, 0x72, false);
        Db(imm);
    }

    [Instruction]
    public void vpsrld(Xmm x, byte imm)
    {
        OpAVX_X_X_XM(xmm2, x, x, AVXType.Mm_0F | AVXType.Pp_66, 0x72, false);
        Db(imm);
    }

    [Instruction]
    public void vpsrlq(Xmm x1, Xmm x2, byte imm)
    {
        OpAVX_X_X_XM(xmm2, x1, x2, AVXType.Mm_0F | AVXType.Pp_66, 0x73, false);
        Db(imm);
    }

    [Instruction]
    public void vpsrlq(Xmm x, byte imm)
    {
        OpAVX_X_X_XM(xmm2, x, x, AVXType.Mm_0F | AVXType.Pp_66, 0x73, false);
        Db(imm);
    }

    [Instruction]
    public void vblendvpd(Xmm x1, Xmm x2, IOperand op, Xmm x4)
    {
        OpAVX_X_X_XM(x1, x2, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x4B, true);
        Db(x4.IDX << 4);
    }

    [Instruction]
    public void vblendvpd(Xmm x1, IOperand op, Xmm x4)
    {
        OpAVX_X_X_XM(x1, x1, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x4B, true);
        Db(x4.IDX << 4);
    }

    [Instruction]
    public void vblendvps(Xmm x1, Xmm x2, IOperand op, Xmm x4)
    {
        OpAVX_X_X_XM(x1, x2, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x4A, true);
        Db(x4.IDX << 4);
    }

    [Instruction]
    public void vblendvps(Xmm x1, IOperand op, Xmm x4)
    {
        OpAVX_X_X_XM(x1, x1, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x4A, true);
        Db(x4.IDX << 4);
    }

    [Instruction]
    public void vpblendvb(Xmm x1, Xmm x2, IOperand op, Xmm x4)
    {
        OpAVX_X_X_XM(x1, x2, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x4C, false);
        Db(x4.IDX << 4);
    }

    [Instruction]
    public void vpblendvb(Xmm x1, IOperand op, Xmm x4)
    {
        OpAVX_X_X_XM(x1, x1, op, AVXType.Mm_0F3A | AVXType.Pp_66, 0x4C, false);
        Db(x4.IDX << 4);
    }

    [Instruction]
    public void vmovd(Xmm x, Reg32 reg)
    {
        OpAVX_X_X_XM(x, xmm0, new Xmm(reg.IDX), AVXType.Mm_0F | AVXType.Pp_66, 0x6E, false, 0);
    }

    [Instruction]
    public void vmovd(Xmm x, Address addr)
    {
        OpAVX_X_X_XM(x, xmm0, addr, AVXType.Mm_0F | AVXType.Pp_66, 0x6E, false, 0);
    }

    [Instruction]
    public void vmovd(Reg32 reg, Xmm x)
    {
        OpAVX_X_X_XM(x, xmm0, new Xmm(reg.IDX), AVXType.Mm_0F | AVXType.Pp_66, 0x7E, false, 0);
    }

    [Instruction]
    public void vmovd(Address addr, Xmm x)
    {
        OpAVX_X_X_XM(x, xmm0, addr, AVXType.Mm_0F | AVXType.Pp_66, 0x7E, false, 0);
    }

    [Instruction]
    public void vmovq(Xmm x, Address addr)
    {
        OpAVX_X_X_XM(x, xmm0, addr, AVXType.Mm_0F | AVXType.Pp_F3, 0x7E, false, -1);
    }

    [Instruction]
    public void vmovq(Address addr, Xmm x)
    {
        OpAVX_X_X_XM(x, xmm0, addr, AVXType.Mm_0F | AVXType.Pp_66, 0xD6, false, -1);
    }

    [Instruction]
    public void vmovq(Xmm x1, Xmm x2)
    {
        OpAVX_X_X_XM(x1, xmm0, x2, AVXType.Mm_0F | AVXType.Pp_F3, 0x7E, false, -1);
    }

    [Instruction]
    public void vmovhlps(Xmm x1, Xmm x2)
    {
        vmovhlps(x1, x2, new Operand());
    }

    [Instruction]
    public void vmovhlps(Xmm x1, Xmm x2, Operand op)
    {
        if (!op.IsNone() && !op.IsXMM())
        {
            throw new ArgumentException("bad combination");
        }
        OpAVX_X_X_XM(x1, x2, op, AVXType.Mm_0F, 0x12, false);
    }

    [Instruction]
    public void vmovlhps(Xmm x1, Xmm x2)
    {
        vmovlhps(x1, x2, new Operand());
    }

    [Instruction]
    public void vmovlhps(Xmm x1, Xmm x2, Operand op)
    {
        if (!op.IsNone() && !op.IsXMM())
        {
            throw new ArgumentException("bad combination");
        }
        OpAVX_X_X_XM(x1, x2, op, AVXType.Mm_0F, 0x16, false);
    }

    [Instruction]
    public void vmovmskpd(Reg r, Xmm x)
    {
        if (!r.IsBit(i32e))
        {
            throw new ArgumentException("bad combination");
        }
        OpAVX_X_X_XM(x.IsXMM() ? new Xmm(r.IDX) : new Ymm(r.IDX), x.IsXMM() ? xmm0 : ymm0, x, AVXType.Mm_0F | AVXType.Pp_66, 0x50, true, 0);
    }

    [Instruction]
    public void vmovmskps(Reg r, Xmm x)
    {
        if (!r.IsBit(i32e))
        {
            throw new ArgumentException("bad combination");
        }
        OpAVX_X_X_XM(x.IsXMM() ? new Xmm(r.IDX) : new Ymm(r.IDX), x.IsXMM() ? xmm0 : ymm0, x, AVXType.Mm_0F, 0x50, true, 0);
    }

    [Instruction]
    public void vmovntdq(Address addr, Xmm x)
    {
        OpAVX_X_X_XM(x, x.IsXMM() ? xmm0 : ymm0, addr, AVXType.Mm_0F | AVXType.Pp_66, 0xE7, true);
    }

    [Instruction]
    public void vmovntpd(Address addr, Xmm x)
    {
        OpAVX_X_X_XM(x, x.IsXMM() ? xmm0 : ymm0, addr, AVXType.Mm_0F | AVXType.Pp_66, 0x2B, true);
    }

    [Instruction]
    public void vmovntps(Address addr, Xmm x)
    {
        OpAVX_X_X_XM(x, x.IsXMM() ? xmm0 : ymm0, addr, AVXType.Mm_0F, 0x2B, true);
    }

    [Instruction]
    public void vmovntdqa(Xmm x, Address addr)
    {
        OpAVX_X_X_XM(x, xmm0, addr, AVXType.Mm_0F38 | AVXType.Pp_66, 0x2A, false);
    }

    [Instruction]
    public void vmovsd(Xmm x1, Xmm x2)
    {
        vmovsd(x1, x2, new Operand());
    }

    [Instruction]
    public void vmovsd(Xmm x1, Xmm x2, Operand op)
    {
        if (!op.IsNone() && !op.IsXMM())
        {
            throw new ArgumentException("bad combination");
        }
        OpAVX_X_X_XM(x1, x2, op, AVXType.Mm_0F | AVXType.Pp_F2, 0x10, false);
    }

    [Instruction]
    public void vmovsd(Xmm x, Address addr)
    {
        OpAVX_X_X_XM(x, xmm0, addr, AVXType.Mm_0F | AVXType.Pp_F2, 0x10, false);
    }

    [Instruction]
    public void vmovsd(Address addr, Xmm x)
    {
        OpAVX_X_X_XM(x, xmm0, addr, AVXType.Mm_0F | AVXType.Pp_F2, 0x11, false);
    }

    [Instruction]
    public void vmovss(Xmm x1, Xmm x2)
    {
        vmovss(x1, x2, new Operand());
    }

    [Instruction]
    public void vmovss(Xmm x1, Xmm x2, Operand op)
    {
        if (!op.IsNone() && !op.IsXMM())
        {
            throw new ArgumentException("bad combination");
        }
        OpAVX_X_X_XM(x1, x2, op, AVXType.Mm_0F | AVXType.Pp_F3, 0x10, false);
    }

    [Instruction]
    public void vmovss(Xmm x, Address addr)
    {
        OpAVX_X_X_XM(x, xmm0, addr, AVXType.Mm_0F | AVXType.Pp_F3, 0x10, false);
    }

    [Instruction]
    public void vmovss(Address addr, Xmm x)
    {
        OpAVX_X_X_XM(x, xmm0, addr, AVXType.Mm_0F | AVXType.Pp_F3, 0x11, false);
    }

    [Instruction]
    public void vcvtss2si(Reg32 r, IOperand op)
    {
        OpAVX_X_X_XM(new Xmm(r.IDX), xmm0, op, AVXType.Mm_0F | AVXType.Pp_F3, 0x2D, false, 0);
    }

    [Instruction]
    public void vcvttss2si(Reg32 r, IOperand op)
    {
        OpAVX_X_X_XM(new Xmm(r.IDX), xmm0, op, AVXType.Mm_0F | AVXType.Pp_F3, 0x2C, false, 0);
    }

    [Instruction]
    public void vcvtsd2si(Reg32 r, IOperand op)
    {
        OpAVX_X_X_XM(new Xmm(r.IDX), xmm0, op, AVXType.Mm_0F | AVXType.Pp_F2, 0x2D, false, 0);
    }

    [Instruction]
    public void vcvttsd2si(Reg32 r, IOperand op)
    {
        OpAVX_X_X_XM(new Xmm(r.IDX), xmm0, op, AVXType.Mm_0F | AVXType.Pp_F2, 0x2C, false, 0);
    }

    [Instruction]
    public void vcvtsi2ss(Xmm x, IOperand op1)
    {
        vcvtsi2ss(x, op1, new Operand());
    }

    [Instruction]
    public void vcvtsi2ss(Xmm x, IOperand op1, IOperand op2)
    {
        if (!op2.IsNone() && !(op2.IsREG(i32e) || op2.IsMEM()))
        {
            throw new ArgumentException("bad combination");
        }
        OpAVX_X_X_XMcvt(x, op1, op2, op2.IsREG(), Operand.KindType.XMM, AVXType.Mm_0F | AVXType.Pp_F3, 0x2A, false, (op1.IsMEM() || op2.IsMEM()) ? -1 : (op1.IsREG(32) || op2.IsREG(32)) ? 0 : 1);
    }

    [Instruction]
    public void vcvtsi2sd(Xmm x, IOperand op1)
    {
        vcvtsi2sd(x, op1, new Operand());
    }

    [Instruction]
    public void vcvtsi2sd(Xmm x, IOperand op1, IOperand op2)
    {
        if (!op2.IsNone() && !(op2.IsREG(i32e) || op2.IsMEM()))
        {
            throw new ArgumentException("bad combination");
        }
        OpAVX_X_X_XMcvt(x, op1, op2, op2.IsREG(), Operand.KindType.XMM, AVXType.Mm_0F | AVXType.Pp_F2, 0x2A, false, (op1.IsMEM() || op2.IsMEM()) ? -1 : (op1.IsREG(32) || op2.IsREG(32)) ? 0 : 1);
    }

    [Instruction]
    public void vcvtps2pd(Xmm x, IOperand op)
    {
        if (!op.IsMEM() && !op.IsXMM())
        {
            throw new ArgumentException("bad combination");
        }
        OpAVX_X_X_XMcvt(x, x.IsXMM() ? xmm0 : ymm0, op, !op.IsMEM(), x.IsXMM() ? Operand.KindType.XMM : Operand.KindType.YMM, AVXType.Mm_0F, 0x5A, true);
    }

    [Instruction]
    public void vcvtdq2pd(Xmm x, IOperand op)
    {
        if (!op.IsMEM() && !op.IsXMM())
        {
            throw new ArgumentException("bad combination");
        }
        OpAVX_X_X_XMcvt(x, x.IsXMM() ? xmm0 : ymm0, op, !op.IsMEM(), x.IsXMM() ? Operand.KindType.XMM : Operand.KindType.YMM, AVXType.Mm_0F | AVXType.Pp_F3, 0xE6, true);
    }

    [Instruction]
    public void vcvtpd2ps(Xmm x, IOperand op)
    {
        if (x.IsYMM())
        {
            throw new ArgumentException("bad combination");
        }
        OpAVX_X_X_XM(op.IsYMM() ? new Ymm(x.IDX) : x, op.IsYMM() ? ymm0 : xmm0, op, AVXType.Mm_0F | AVXType.Pp_66, 0x5A, true);
    }

    [Instruction]
    public void vcvtpd2dq(Xmm x, IOperand op)
    {
        if (x.IsYMM())
        {
            throw new ArgumentException("bad combination");
        }
        OpAVX_X_X_XM(op.IsYMM() ? new Ymm(x.IDX) : x, op.IsYMM() ? ymm0 : xmm0, op, AVXType.Mm_0F | AVXType.Pp_F2, 0xE6, true);
    }

    [Instruction]
    public void vcvttpd2dq(Xmm x, IOperand op)
    {
        if (x.IsYMM())
        {
            throw new ArgumentException("bad combination");
        }
        OpAVX_X_X_XM(op.IsYMM() ? new Ymm(x.IDX) : x, op.IsYMM() ? ymm0 : xmm0, op, AVXType.Mm_0F | AVXType.Pp_66, 0xE6, true);
    }

    #region for x64

    [Instruction]
    public void vmovq(Xmm x, Reg64 reg)
    {
        OpAVX_X_X_XM(x, xmm0, new Xmm(reg.IDX), AVXType.Mm_0F | AVXType.Pp_66, 0x6E, false, 1);
    }

    [Instruction]
    public void vmovq(Reg64 reg, Xmm x)
    {
        OpAVX_X_X_XM(x, xmm0, new Xmm(reg.IDX), AVXType.Mm_0F | AVXType.Pp_66, 0x7E, false, 1);
    }

    [Instruction]
    public void vpextrq(IOperand op, Xmm x, byte imm)
    {
        if (!op.IsREG(64) && !op.IsMEM())
        {
            throw new ArgumentException("bad combination");
        }
        OpAVX_X_X_XMcvt(x, xmm0, op, !op.IsMEM(), Operand.KindType.XMM, AVXType.Mm_0F3A | AVXType.Pp_66, 0x16, false, 1);
        Db(imm);
    }

    [Instruction]
    public void vpinsrq(Xmm x1, Xmm x2, IOperand op, byte imm)
    {
        if (!op.IsREG(64) && !op.IsMEM())
        {
            throw new ArgumentException("bad combination");
        }
        OpAVX_X_X_XMcvt(x1, x2, op, !op.IsMEM(), Operand.KindType.XMM, AVXType.Mm_0F3A | AVXType.Pp_66, 0x22, false, 1);
        Db(imm);
    }

    [Instruction]
    public void vpinsrq(Xmm x, IOperand op, byte imm)
    {
        if (!op.IsREG(64) && !op.IsMEM())
        {
            throw new ArgumentException("bad combination");
        }
        OpAVX_X_X_XMcvt(x, x, op, !op.IsMEM(), Operand.KindType.XMM, AVXType.Mm_0F3A | AVXType.Pp_66, 0x22, false, 1);
        Db(imm);
    }

    [Instruction]
    public void vcvtss2si(Reg64 r, IOperand op)
    {
        OpAVX_X_X_XM(new Xmm(r.IDX), xmm0, op, AVXType.Mm_0F | AVXType.Pp_F3, 0x2D, false, 1);
    }

    [Instruction]
    public void vcvttss2si(Reg64 r, IOperand op)
    {
        OpAVX_X_X_XM(new Xmm(r.IDX), xmm0, op, AVXType.Mm_0F | AVXType.Pp_F3, 0x2C, false, 1);
    }

    [Instruction]
    public void vcvtsd2si(Reg64 r, IOperand op)
    {
        OpAVX_X_X_XM(new Xmm(r.IDX), xmm0, op, AVXType.Mm_0F | AVXType.Pp_F2, 0x2D, false, 1);
    }

    [Instruction]
    public void vcvttsd2si(Reg64 r, IOperand op)
    {
        OpAVX_X_X_XM(new Xmm(r.IDX), xmm0, op, AVXType.Mm_0F | AVXType.Pp_F2, 0x2C, false, 1);
    }

    [Instruction]
    public void movq(Reg64 reg, Mmx mmx)
    {
        if (mmx.IsXMM())
        {
            Db(0x66);
        }
        OpModR(mmx, reg, 0x0F, (int)0b01111110);
    }

    [Instruction]
    public void movq(Mmx mmx, Reg64 reg)
    {
        if (mmx.IsXMM())
        {
            Db(0x66);
        }
        OpModR(mmx, reg, 0x0F, (int)0b01101110);
    }

    [Instruction]
    public void pextrq(IOperand op, Xmm xmm, byte imm)
    {
        if (!op.IsREG(64) && !op.IsMEM())
        {
            throw new ArgumentException("bad combination", "op");
        }
        OpGen(new Reg64(xmm.IDX), op, 0x16, 0x66, null, imm, (int)0b00111010);
    }

    [Instruction]
    public void pinsrq(Xmm xmm, IOperand op, byte imm)
    {
        if (!op.IsREG(64) && !op.IsMEM())
        {
            throw new ArgumentException("bad combination", "op");
        }
        OpGen(new Reg64(xmm.IDX), op, 0x22, 0x66, null, imm, (int)0b00111010);
    }

    [Instruction]
    public void movsxd(Reg64 reg, IOperand op)
    {
        if (!op.IsBit(32))
        {
            throw new ArgumentException("bad combination", "op");
        }
        OpModRM(reg, op, op.IsREG(), op.IsMEM(), 0x63);
    }

    #endregion for x64
}