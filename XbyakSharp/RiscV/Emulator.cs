using ELFSharp.ELF;
using ELFSharp.ELF.Sections;
using System.Text;

namespace XbyakSharp.RiscV;
public class Emulator
{
    public const ulong num_memory = 0x400000;
    public const ulong num_register = 32;
    public const ulong inst_size = sizeof(uint);

    public const ulong delta0 = 0xfffffffffffffffful;
    public const ulong delta6 = 0xfffffffffffffff0ul;
    public const ulong delta7 = 0xffffffffffffff00ul;
    public const ulong delta1 = 0xffffffff00000000ul;
    public const ulong delta2 = 0xffffffffffe00000ul;
    public const ulong delta3 = 0xfffffffffffff000ul;
    public const ulong delta5 = 0xffffffffffffe000ul;
    public const ulong delta4 = 0xfffffffffffffffeul;

    protected ulong[] regs = new ulong[num_register];
    protected byte[] memory = new byte[num_memory];
    protected IELF? elf = null;
    public IELF? Elf => this.elf;
    protected uint num_func = 0;
    protected uint num_ins = 0;
    protected ulong pc = 0;

    public TextWriter Writer { get; set; } = Console.Out;
    public TextReader Reader { get; set; } = Console.In;
    public ulong[] Regs => regs;
    public byte[] Memory => memory;

    public ulong Pc => pc;

    void lui(ulong ins)
    {
        ulong imm = (ins >> 5) << 12;
        ulong rd = (ins & ((1 << 5) - 1));
        regs[rd] = imm;
        if ((regs[rd] >> 31) == 1) regs[rd] += delta1;
    }

    void auipc(ulong ins)
    {
        ulong imm = (ins >> 5) << 12;
        if ((imm >> 31) == 1) imm += delta1;
        ulong rd = (ins & ((1 << 5) - 1));
        regs[rd] = imm + pc;
    }

    void jal(ulong ins)
    {
        ulong imm = 0;
        ulong rd = (ins & ((1 << 5) - 1));
        imm |= (ins >> 24) << 20;
        imm |= ((ins >> 5) & ((1 << 8) - 1)) << 12;
        imm |= ((ins >> 13) & 1) << 11;
        imm |= ((ins >> 14) & ((1 << 10) - 1)) << 1;
        if ((imm >> 20) == 1) imm += delta2;
        if (rd != 0) regs[rd] = pc + inst_size;
        pc += imm;
        if (imm == 0) { --num_func; return; }
        if (rd == 1) num_func++;
    }

    void jalr(ulong ins)
    {
        ulong imm = ins >> 13;
        ulong rs1 = (ins >> 8) & ((1 << 5) - 1);
        ulong rd = ins & ((1 << 5) - 1);
        if ((imm >> 11) == 1) imm += delta3;
        if (rd != 0) regs[rd] = pc + inst_size;
        pc = (imm + regs[rs1]) & delta4;
        if (rd == 1) num_func++;
        else if (rd == 0 && rs1 == 1) num_func--;
    }

    void beq(ulong rs1, ulong rs2, ulong imm)
    {
        if (regs[rs1] == regs[rs2])
            pc += imm;
        else
            pc += inst_size;
    }

    void bne(ulong rs1, ulong rs2, ulong imm)
    {
        if (regs[rs1] != regs[rs2])
            pc += imm;
        else
            pc += inst_size;
    }

    void blt(ulong rs1, ulong rs2, ulong imm)
    {
        // take the branch if rs1 is less than rs2, using signed comparison
        if ((long)regs[rs1] < (long)regs[rs2])
            pc += imm;
        else
            pc += inst_size;
    }

    void bge(ulong rs1, ulong rs2, ulong imm)
    {
        // take the branch if rs1 is greater than or equal to rs2, using signed comparison
        if ((long)regs[rs1] >= (long)regs[rs2]) pc += imm;
        else pc += inst_size;
    }

    void bltu(ulong rs1, ulong rs2, ulong imm)
    {
        // take the branch if rs1 is less than rs2, using unsigned comparison
        if (regs[rs1] < regs[rs2]) pc += imm;
        else pc += inst_size;
    }

    void bgeu(ulong rs1, ulong rs2, ulong imm)
    {
        // take the branch if rs1 is greater than or equal to rs2, using unsigned comparison
        if (regs[rs1] >= regs[rs2]) pc += imm;
        else pc += inst_size;
    }

    void _branch(ulong ins)
    {
        ulong imm = (ins >> 24) << 12;
        imm |= ((ins >> 18) & ((1 << 6) - 1)) << 5;
        ulong rs2 = (ins >> 13) & ((1 << 5) - 1);
        ulong rs1 = (ins >> 8) & ((1 << 5) - 1);
        ulong funct3 = (ins >> 5) & ((1 << 3) - 1);
        imm |= (ins & 1) << 11;
        imm |= ((ins >> 1) & ((1 << 4) - 1)) << 1;
        if ((imm >> 12) == 1) imm += delta5;
        switch (funct3)
        {
            case 0x0: beq(rs1, rs2, imm); break;     /* BEQ */
            case 0x1: bne(rs1, rs2, imm); break;     /* BNE */
            case 0x4: blt(rs1, rs2, imm); break;     /* BLT */
            case 0x5: bge(rs1, rs2, imm); break;     /* BGE */
            case 0x6: bltu(rs1, rs2, imm); break;     /* BLTU */
            case 0x7: bgeu(rs1, rs2, imm); break;     /* BGEU */
        }
    }

    void lb(ulong rd, ulong rs1, ulong imm)
    {
        // loads a 8-bit value from memory and sign-extends this to 64 bits
        ulong addr = regs[rs1] + imm;
        regs[rd] = 0;
        regs[rd] |= memory[addr];
        if ((regs[rd] >> 7) == 1) regs[rd] += delta6;
    }

    void lh(ulong rd, ulong rs1, ulong imm)
    {
        // loads a 16-bit value from memory and sign-extends this to 64 bits
        ulong addr = regs[rs1] + imm;
        regs[rd] = 0;
        for (int i = 1; i >= 0; --i)
        {
            regs[rd] <<= 8;
            regs[rd] |= memory[addr + (ulong)i];
        }
        if ((regs[rd] >> 15) == 1) regs[rd] += delta7;
    }

    void lw(ulong rd, ulong rs1, ulong imm)
    {
        // loads a 32-bit value from memory and sign-extends this to 64 bits
        ulong addr = regs[rs1] + imm;
        regs[rd] = 0;
        for (int i = 3; i >= 0; --i)
        {
            regs[rd] <<= 8;
            regs[rd] |= memory[addr + (ulong)i];
        }
        if ((regs[rd] >> 31) == 1) regs[rd] += delta1;
    }

    void ld(ulong rd, ulong rs1, ulong imm)
    {
        // loads a 64-bit value from memory
        ulong addr = regs[rs1] + imm;
        regs[rd] = 0;
        for (int i = 7; i >= 0; --i)
        {
            regs[rd] <<= 8;
            regs[rd] |= memory[addr + (ulong)i];
        }
    }

    void lbu(ulong rd, ulong rs1, ulong imm)
    {
        // loads a 8-bit value from memory and zero-extends this to 64 bits
        ulong addr = regs[rs1] + imm;
        regs[rd] = 0;
        regs[rd] |= memory[addr];
    }

    void lhu(ulong rd, ulong rs1, ulong imm)
    {
        // loads a 16-bit value from memory and zero-extends this to 64 bits
        ulong addr = regs[rs1] + imm;
        regs[rd] = 0;
        for (int i = 1; i >= 0; --i)
        {
            regs[rd] <<= 8;
            regs[rd] |= memory[addr + (ulong)i];
        }
    }

    void lwu(ulong rd, ulong rs1, ulong imm)
    {
        // loads a 32-bit value from memory and zero-extends this to 64 bits
        ulong addr = regs[rs1] + imm;
        regs[rd] = 0;
        for (int i = 3; i >= 0; --i)
        {
            regs[rd] <<= 8;
            regs[rd] |= memory[addr + (ulong)i];
        }
    }

    void _load(ulong ins)
    {
        ulong imm = ins >> 13;
        ulong rs1 = (ins >> 8) & ((1 << 5) - 1);
        ulong funct3 = (ins >> 5) & ((1 << 3) - 1);
        ulong rd = ins & ((1 << 5) - 1);
        if ((imm >> 11) == 1) imm += delta3;
        switch (funct3)
        {
            case 0x0: lb(rd, rs1, imm); break;       /* LB */
            case 0x1: lh(rd, rs1, imm); break;       /* LH */
            case 0x2: lw(rd, rs1, imm); break;       /* LW */
            case 0x3: ld(rd, rs1, imm); break;       /* LD */
            case 0x4: lbu(rd, rs1, imm); break;       /* LBU */
            case 0x5: lhu(rd, rs1, imm); break;       /* LHU */
            case 0x6: lwu(rd, rs1, imm); break;       /* LWU */
        }
    }

    void sb(ulong rs1, ulong rs2, ulong imm)
    {
        // store 8-bit values from the low bits of register rs2 to memory
        ulong addr = regs[rs1] + imm;
        memory[addr] = (byte)regs[rs2];
    }

    void sh(ulong rs1, ulong rs2, ulong imm)
    {
        // store 16-bit values from the low bits of register rs2 to memory
        ulong addr = regs[rs1] + imm;
        ulong r = regs[rs2];
        for (int i = 0; i <= 1; ++i)
        {
            memory[addr + (ulong)i] = (byte)r;
            r >>= 8;
        }
    }

    void sw(ulong rs1, ulong rs2, ulong imm)
    {
        // store 32-bit values from the low bits of register rs2 to memory
        ulong addr = regs[rs1] + imm;
        ulong r = regs[rs2];
        for (int i = 0; i <= 3; ++i)
        {
            memory[addr + (ulong)i] = (byte)r;
            r >>= 8;
        }
    }

    void sd(ulong rs1, ulong rs2, ulong imm)
    {
        // store 64-bit values from the low bits of register rs2 to memory
        ulong addr = regs[rs1] + imm;
        ulong r = regs[rs2];
        for (int i = 0; i <= 7; ++i)
        {
            memory[addr + (ulong)i] = (byte)r;
            r >>= 8;
        }
    }

    void _save(ulong ins)
    {
        ulong imm = (ins >> 18) << 5;
        ulong rs2 = (ins >> 13) & ((1 << 5) - 1);
        ulong rs1 = (ins >> 8) & ((1 << 5) - 1);
        ulong funct3 = (ins >> 5) & ((1 << 3) - 1);
        imm |= (ins & ((1 << 5) - 1));
        if ((imm >> 11) == 1) imm += delta3;
        switch (funct3)
        {
            case 0x0: sb(rs1, rs2, imm); break;       /* SB */
            case 0x1: sh(rs1, rs2, imm); break;       /* SH */
            case 0x2: sw(rs1, rs2, imm); break;       /* SW */
            case 0x3: sd(rs1, rs2, imm); break;       /* SD */
        }
    }

    void addi(ulong rd, ulong rs1, ulong imm)
    {
        // adds the sign-extended 12-bit immediate to register rs1
        // the result is simply the low 32-bits of the result
        regs[rd] = (ulong)((long)regs[rs1] + (long)imm);
    }

    void slli_(ulong rd, ulong rs1, int shamt)
    {
        // logical left shift
        regs[rd] = regs[rs1] << shamt;
    }

    void slli64(ulong rd, ulong rs1, int shamt)
    {
        shamt += 1 << 5;
        regs[rd] = regs[rs1] << shamt;
    }

    void slli(ulong funct7, ulong rd, ulong rs1, int shamt)
    {
        if (funct7 == 0x0) slli_(rd, rs1, shamt);
        else slli64(rd, rs1, shamt);
    }

    void slti(ulong rd, ulong rs1, ulong imm)
    {
        // writing 1 to rd if rs1 < imm(sign-extended), else 0 is written to rd
        // signed compares
        if ((long)regs[rs1] < (long)imm) regs[rd] = delta0;
        else regs[rd] = 0;

    }

    void sltiu(ulong rd, ulong rs1, ulong imm)
    {
        // unsigned compares
        if (regs[rs1] < imm) regs[rd] = delta0;
        else regs[rd] = 0;
    }

    void xori(ulong rd, ulong rs1, ulong imm)
    {
        regs[rd] = regs[rs1] ^ imm;
    }

    void srli(ulong rd, ulong rs1, int shamt)
    {
        // logical right shift
        regs[rd] = regs[rs1] >> shamt;
    }

    void srli64(ulong rd, ulong rs1, int shamt)
    {
        regs[rd] = regs[rs1] >> shamt;
    }

    void srai(ulong rd, ulong rs1, int shamt)
    {
        // arithmetic right shift
        regs[rd] = (ulong)(((long)regs[rs1]) >> shamt);
    }

    void srai64(ulong rd, ulong rs1, int shamt)
    {
        regs[rd] = (ulong)(((long)regs[rs1]) >> shamt);
    }

    void sr_i(ulong funct7, ulong rd, ulong rs1, int shamt)
    {
        if (funct7 == 0x0) srli(rd, rs1, shamt);
        else if (funct7 == 0x01) srli64(rd, rs1, shamt | (1 << 5));
        else if (funct7 == 0x20) srai(rd, rs1, shamt);
        else srai64(rd, rs1, shamt | (1 << 5));
    }

    void ori(ulong rd, ulong rs1, ulong imm)
    {
        regs[rd] = regs[rs1] | imm;
    }

    void andi(ulong rd, ulong rs1, ulong imm)
    {
        regs[rd] = regs[rs1] & imm;
    }

    void _imm_op(ulong ins)
    {
        ulong imm = ins >> 13;
        ulong rs1 = (ins >> 8) & ((1 << 5) - 1);
        ulong funct3 = (ins >> 5) & ((1 << 3) - 1);
        ulong rd = ins & ((1 << 5) - 1);
        int shamt = (int)(imm & ((1 << 5) - 1));
        ulong funct7 = imm >> 5;
        if ((imm >> 11) == 1) imm += delta3;
        switch (funct3)
        {
            case 0x0: addi(rd, rs1, imm); break;    /* ADDI */
            case 0x1: slli(funct7, rd, rs1, shamt); break;    /* SLLI SLLI64 */
            case 0x2: slti(rd, rs1, imm); break;    /* SLTI */
            case 0x3: sltiu(rd, rs1, imm); break;    /* SLTIU */
            case 0x4: xori(rd, rs1, imm); break;    /* XORI */
            case 0x5: sr_i(funct7, rd, rs1, shamt); break;    /* SRLI SRAI */
            case 0x6: ori(rd, rs1, imm); break;    /* ORI */
            case 0x7: andi(rd, rs1, imm); break;    /* ANDI */
        }
    }

    void add(ulong rd, ulong rs1, ulong rs2)
    {
        regs[rd] = regs[rs1] + regs[rs2];
    }

    void sub(ulong rd, ulong rs1, ulong rs2)
    {
        regs[rd] = regs[rs1] - regs[rs2];
    }

    void add_sub(ulong funct7, ulong rd, ulong rs1, ulong rs2)
    {
        if (funct7 == 0x0) add(rd, rs1, rs2);
        else sub(rd, rs1, rs2);
    }

    void sll(ulong rd, ulong rs1, ulong rs2)
    {
        regs[rs1] <<= (int)regs[rs2] & ((1 << 6) - 1);
    }

    void slt(ulong rd, ulong rs1, ulong rs2)
    {
        if ((long)regs[rs1] < (long)regs[rs2]) regs[rd] = delta0;
        else regs[rd] = 0;
    }

    void sltu(ulong rd, ulong rs1, ulong rs2)
    {
        if (regs[rs1] < regs[rs2]) regs[rd] = delta0;
        else regs[rd] = 0;
    }

    void xor(ulong rd, ulong rs1, ulong rs2)
    {
        regs[rd] = regs[rs1] ^ regs[rs2];
    }

    void srl(ulong rd, ulong rs1, ulong rs2)
    {
        regs[rs1] >>= (int)(regs[rs2] & ((1 << 6) - 1));
    }

    void sra(ulong rd, ulong rs1, ulong rs2)
    {
        regs[rs1] = (ulong)((long)regs[rs1] >> (int)(regs[rs2] & ((1 << 6) - 1)));
    }

    void sr_(ulong funct7, ulong rd, ulong rs1, ulong rs2)
    {
        if (funct7 == 0x0) srl(rd, rs1, rs2);
        else sra(rd, rs1, rs2);
    }

    void OR(ulong rd, ulong rs1, ulong rs2)
    {
        regs[rd] = regs[rs1] | regs[rs2];
    }

    void and(ulong rd, ulong rs1, ulong rs2)
    {
        regs[rd] = regs[rs1] & regs[rs2];
    }

    void _op(ulong ins)
    {
        ulong funct7 = ins >> 18;
        ulong rs2 = (ins >> 13) & ((1 << 5) - 1);
        ulong rs1 = (ins >> 8) & ((1 << 5) - 1);
        ulong funct3 = (ins >> 5) & ((1 << 3) - 1);
        ulong rd = ins & ((1 << 5) - 1);
        switch (funct3)
        {
            case 0x0: add_sub(funct7, rd, rs1, rs2); break;   /* ADD SUB */
            case 0x1: sll(rd, rs1, rs2); break;   /* SLL */
            case 0x2: slt(rd, rs1, rs2); break;   /* SLT */
            case 0x3: sltu(rd, rs1, rs2); break;   /* SLTU */
            case 0x4: xor(rd, rs1, rs2); break;   /* XOR */
            case 0x5: sr_(funct7, rd, rs1, rs2); break;   /* SRLI SRAI */
            case 0x6: OR(rd, rs1, rs2); break;   /* OR */
            case 0x7: and(rd, rs1, rs2); break;   /* AND */
        }
    }

    void addiw(ulong rd, ulong rs1, ulong imm)
    {
        if ((imm >> 11) == 1) imm += delta3;
        regs[rd] = regs[rs1] + imm;
        if ((regs[rd] >> 31) == 1) regs[rd] += delta1;
    }

    void slliw(ulong rd, ulong rs1, int shamt)
    {
        regs[rs1] <<= shamt;
        if ((regs[rd] >> 31) == 1) regs[rd] += delta1;
    }

    void srliw(ulong rd, ulong rs1, int shamt)
    {
        regs[rs1] >>= shamt;
        if ((regs[rd] >> 31) == 1) regs[rd] += delta1;
    }

    void sraiw(ulong rd, ulong rs1, int shamt)
    {
        regs[rs1] = (ulong)((long)regs[rs1] >> shamt);
        if ((regs[rd] >> 31) == 1) regs[rd] += delta1;
    }

    void sr_iw(ulong funct7, ulong rd, ulong rs1, int shamt)
    {
        if (funct7 == 0x0) srliw(rd, rs1, shamt);
        else sraiw(rd, rs1, shamt);
    }

    void _imm_op_64(ulong ins)
    {
        ulong imm = ins >> 13;
        ulong funct7 = imm >> 5;
        int shamt =(int)(imm & ((1 << 5) - 1));
        ulong rs1 = (ins >> 8) & ((1 << 5) - 1);
        ulong funct3 = (ins >> 5) & ((1 << 3) - 1);
        ulong rd = ins & ((1 << 5) - 1);
        switch (funct3)
        {
            case 0x0: addiw(rd, rs1, imm); break;   /* ADDIW */
            case 0x1: slliw(rd, rs1, shamt); break;   /* SLLIW */
            case 0x5: sr_iw(funct7, rd, rs1, shamt); break;   /* SRLIW SRAIW */
        }
    }

    void addw(ulong rd, ulong rs1, ulong rs2)
    {
        regs[rd] = regs[rs1] + regs[rs2];
        if ((regs[rd] >> 31) == 1) regs[rd] += delta1;
    }

    void subw(ulong rd, ulong rs1, ulong rs2)
    {
        regs[rd] = regs[rs1] - regs[rs2];
        if ((regs[rd] >> 31) == 1) regs[rd] += delta1;
    }

    void add_subw(ulong funct7, ulong rd, ulong rs1, ulong rs2)
    {
        if (funct7 == 0x0) addw(rd, rs1, rs2);
        else subw(rd, rs1, rs2);
    }

    void sllw(ulong rd, ulong rs1, ulong rs2)
    {
        regs[rs1] <<= (int)(regs[rs2] & ((1 << 5) - 1));
    }

    void srlw(ulong rd, ulong rs1, ulong rs2)
    {
        regs[rs1] >>= (int)(regs[rs2] & ((1 << 5) - 1));
    }

    void sraw(ulong rd, ulong rs1, ulong rs2)
    {
        regs[rs1] = (ulong)((long)regs[rs1] >> (int)(regs[rs2] & ((1 << 5) - 1)));
    }

    void sr_w(ulong funct7, ulong rd, ulong rs1, ulong rs2)
    {
        if (funct7 == 0x0) srlw(rd, rs1, rs2);
        else sraw(rd, rs1, rs2);
    }

    void read()
    {

    }

    void write()
    {
        var vs = new byte[regs[12]];
        for (ulong i = 0; i <= regs[12]; i++)
        {
            vs[regs[12] - 1 - i] = memory[regs[11] + i];
        }
        this.Writer?.Write(Encoding.Latin1.GetString(vs));

        regs[10] =(ulong) vs.Length;
    }

    void exit()
    {
    }

    void time()
    {
    }

    void scall(ulong ins)
    {
        if (regs[17] == 63) read();
        if (regs[17] == 64) write();
        if (regs[17] == 93) exit();
        if (regs[17] == 169) time();
    }

    void _op_64(ulong ins)
    {
        ulong funct7 = ins >> 18;
        ulong rs2 = (ins >> 13) & ((1 << 5) - 1);
        ulong rs1 = (ins >> 8) & ((1 << 5) - 1);
        ulong funct3 = (ins >> 5) & ((1 << 3) - 1);
        ulong rd = ins & ((1 << 5) - 1);
        switch (funct3)
        {
            case 0x0: add_subw(funct7, rd, rs1, rs2); break;  /* ADDW SUBW */
            case 0x1: sllw(rd, rs1, rs2); break;  /* SLLW */
            case 0x5: sr_w(funct7, rd, rs1, rs2); break;  /* SRLW SRAW */
        }
    }

    public void ProcessInstruction(ulong instruction)
    {
        System.Diagnostics.Debug.WriteLine("{0:X8} pc={1:X8}, ins={2:X8}",this.num_ins,this.pc, instruction);
        ulong opcode = instruction & ((1 << 7) - 1);
        ulong ins = instruction >> 7;
        switch (opcode)
        {
            case 0x37: lui(ins); pc += inst_size; break;     /* LUI */
            case 0x17: auipc(ins); pc += inst_size; break;     /* AUIPC */
            case 0x6F: jal(ins); break;     /* JAL */
            case 0x67: jalr(ins); break;     /* JALR */
            case 0x63: _branch(ins); break;     /* BEQ BNE BLT BGE BLTU BGEU */
            case 0x03: _load(ins); pc += inst_size; break;     /* LB LH LW LBU LHU LWU LD */
            case 0x23: _save(ins); pc += inst_size; break;     /* SB SH SW SD */
            case 0x13: _imm_op(ins); pc += inst_size; break;     /* ADDI SLTI SLTIU XORI ORI AND SLLI SRLI SRAI */
            case 0x33: _op(ins); pc += inst_size; break;     /* ADD SUB SLL SLT SLTU XOR SRL SRA OR AND */
            //        case 0x0F: process_syn( ins );     pc += inst_size;  break;     /* FENCE FENCE.I */
            //        case 0x73: process_sys( ins );     pc += inst_size;  break;     /* SCALL SBREAK RDCYCLE RDCYCLEN RDTIME RDTIMEH RDINSTRET RDINSTRETH*/
            case 0x1B: _imm_op_64(ins); pc += inst_size; break;     /* ADDIW SLLIW SRLIW SRAIW */
            case 0x3B: _op_64(ins); pc += inst_size; break;     /* ADDW SUBW SLLW SRLW SRAW */
            case 0x73: scall(ins); pc += inst_size; break;     /* SCALL */
            default:
                num_func = 0;
                break;
        }
    }
    public ulong GetInstruction()
    {
        ulong ins = 0;
        for (int i = 3; i >= 0; --i)
        {
            ins <<= 8;
            ins |= memory[pc + (ulong)i];
        }
        return ins;
    }
    public void Emulate()
    {
        this.num_func = 1;
        this.num_ins = 0;
        while (num_func != 0)
        {
            this.ProcessInstruction(this.GetInstruction());
            this.num_ins++;
        }
    }
    public bool Load(string filename) 
        => this.Load(File.OpenRead(filename));
    public bool Load(Stream stream)
    {
        if (ELFReader.TryLoad(stream, out var elf, true))
        {
            this.elf = elf;
            switch (elf._ElfClass)
            {
                case ElfClass.Bit32:
                    {
                        foreach (Section<uint> _section in elf.Sections)
                            if(_section.LoadAddress!=0)
                                this.Load(_section.GetContents(), _section.LoadAddress);
                        var sectionsToLoad = elf.GetSections<ProgBitsSection<uint>>().Where(x => x.LoadAddress != 0).ToList();
                        if (sectionsToLoad.FirstOrDefault() is Section<uint> section && elf is ELF<uint> elf32)
                        {
                            this.pc = elf32.EntryPoint;
                            regs[2] = 0xBFF0; // SP
                            return true;
                        }
                    }
                    break;
                case ElfClass.Bit64:
                    {
                        foreach (Section<ulong> _section in elf.Sections)
                            if (_section.LoadAddress != 0)
                                this.Load(_section.GetContents(), _section.LoadAddress);
                        var sectionsToLoad = elf.GetSections<ProgBitsSection<ulong>>().Where(x => x.LoadAddress != 0).ToList();
                        if (sectionsToLoad.FirstOrDefault() is Section<ulong> section && elf is ELF<ulong> elf64)
                        {
                            this.pc = elf64.EntryPoint;
                            this.regs[2] = 0xBFF0; // SP
                            return true;
                        }
                    }
                    break;
            }

        }
        return false;
    }
    public bool Load(byte[] vs, ulong loadAddress = 0ul)
    {
        if (vs != null && vs.Length > 0 && loadAddress + (ulong)vs.Length<num_memory)
        {
            Array.ConstrainedCopy(vs, 0, this.memory, (int)loadAddress, vs.Length);
            
            return true;
        }
        return false;
    }

}
