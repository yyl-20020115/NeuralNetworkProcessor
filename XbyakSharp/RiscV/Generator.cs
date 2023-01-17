
namespace XbyakSharp.RiscV;

public class CodeGenerator : ICodeGenerator
{
    public BinaryWriter BinaryWriter { get; }
    public MemoryStream? Stream 
        => this.BinaryWriter.BaseStream as MemoryStream;
    public byte[]? Buffer 
        => this.Stream?.GetBuffer();
    public CodeGenerator(Stream? stream = null) 
        => this.BinaryWriter = new(stream ?? new MemoryStream());

    [Instruction]
    public long add(int rd, int rs1, int rs2)
        => this.EmitCodeFormatR(0b0110011, rd, 0b000, rs1, rs2, 0b0000000);

    [Instruction]
    public long sub(int rd, int rs1, int rs2)
        => this.EmitCodeFormatR(0b0110011, rd, 0b000, rs1, rs2, 0b0100000);

    [Instruction]
    public long sll(int rd, int rs1, int rs2)
        => this.EmitCodeFormatR(0b0110011, rd, 0b001, rs1, rs2, 0b0000000);

    [Instruction]
    public long slt(int rd, int rs1, int rs2)
        => this.EmitCodeFormatR(0b0110011, rd, 0b010, rs1, rs2, 0b0000000);

    [Instruction]
    public long sltu(int rd, int rs1, int rs2)
        => this.EmitCodeFormatR(0b0110011, rd, 0b011, rs1, rs2, 0b0000000);

    [Instruction]
    public long xor(int rd, int rs1, int rs2)
        => this.EmitCodeFormatR(0b0110011, rd, 0b100, rs1, rs2, 0b0000000);

    [Instruction]
    public long srl(int rd, int rs1, int rs2)
        => this.EmitCodeFormatR(0b0110011, rd, 0b101, rs1, rs2, 0b0000000);

    [Instruction]
    public long sra(int rd, int rs1, int rs2)
        => this.EmitCodeFormatR(0b0110011, rd, 0b101, rs1, rs2, 0b0100000);

    [Instruction]
    public long or(int rd, int rs1, int rs2)
        => this.EmitCodeFormatR(0b0110011, rd, 0b110, rs1, rs2, 0b0000000);

    [Instruction]
    public long and(int rd, int rs1, int rs2)
        => this.EmitCodeFormatR(0b0110011, rd, 0b111, rs1, rs2, 0b0000000);

    [Instruction]
    public long jal(int rd,int imm)
        => this.EmitCodeFormatJ(0x6f, rd, imm);

    [Instruction]
    public long beq(int rs1, int rs2, int imm)
        => this.EmitCodeFormatB(0x63, 0, rs1, rs2, imm);

    [Instruction]
    public long bne(int rs1, int rs2, int imm)
        => this.EmitCodeFormatB(0x63, 1, rs1, rs2, imm);
    [Instruction]
    public long blt(int rs1, int rs2, int imm)
        => this.EmitCodeFormatB(0x63, 4, rs1, rs2, imm);
    [Instruction]
    public long bge(int rs1, int rs2, int imm)
        => this.EmitCodeFormatB(0x63, 5, rs1, rs2, imm);
    [Instruction]
    public long bltu(int rs1, int rs2, int imm)
        => this.EmitCodeFormatB(0x63, 6, rs1, rs2, imm);
    [Instruction]
    public long bgeu(int rs1, int rs2, int imm)
        => this.EmitCodeFormatB(0x63, 7, rs1, rs2, imm);

    [Instruction]
    public long lb(int rd, int rs1, int imm)
        => this.EmitCodeFormatI(0x03,rd, 0, rs1, imm);

    [Instruction]
    public long lh(int rd, int rs1, int imm)
        => this.EmitCodeFormatI(0x03, rd, 1, rs1, imm);

    [Instruction]
    public long lw(int rd, int rs1, int imm)
        => this.EmitCodeFormatI(0x03, rd, 2, rs1, imm);

    [Instruction]
    public long ld(int rd, int rs1, int imm)
        => this.EmitCodeFormatI(0x03, rd, 3, rs1, imm);
    [Instruction]
    public long lbu(int rd, int rs1, int imm)
        => this.EmitCodeFormatI(0x03, rd, 4, rs1, imm);

    [Instruction]
    public long lhu(int rd, int rs1, int imm)
        => this.EmitCodeFormatI(0x03, rd, 5, rs1, imm);

    [Instruction]
    public long lwu(int rd, int rs1, int imm)
        => this.EmitCodeFormatI(0x03, rd, 6, rs1, imm);

    [Instruction]
    public long sb(int rs1, int rs2, int imm)
        => this.EmitCodeFormatS(0x23, 0, rs1, rs2, imm);

    [Instruction]
    public long sh(int rs1, int rs2, int imm)
        => this.EmitCodeFormatS(0x23, 1, rs1, rs2, imm);

    [Instruction]
    public long sw(int rs1, int rs2, int imm)
        => this.EmitCodeFormatS(0x23, 2, rs1, rs2, imm);

    [Instruction]
    public long sd(int rs1, int rs2, int imm)
        => this.EmitCodeFormatS(0x23, 3, rs1, rs2, imm);

    [Instruction]
    public long addi(int rd, int rs1, int imm)
        => this.EmitCodeFormatI(0x13, rd, 0b000, rs1, imm);
    [Instruction]
    public long slti(int rd, int rs1, int imm)
        => this.EmitCodeFormatI(0x13, rd, 0b010, rs1, imm);
    [Instruction]
    public long sltiu(int rd, int rs1, int imm)
        => this.EmitCodeFormatI(0x13, rd, 0b011, rs1, imm);
    [Instruction]
    public long xori(int rd, int rs1, int imm)
        => this.EmitCodeFormatI(0x13, rd, 0b100, rs1, imm);
    [Instruction]
    public long ori(int rd, int rs1, int imm)
        => this.EmitCodeFormatI(0x13, rd, 0b100, rs1, imm);

    [Instruction]
    public long andi(int rd, int rs1, int imm)
        => this.EmitCodeFormatI(0x13, rd, 0b111, rs1, imm);

    [Instruction]
    public long addiw(int rd, int rs1, int imm)
        => this.EmitCodeFormatI(0x1b, rd, 0b000, rs1, (imm & 0b111111111111));

    [Instruction]
    public long slliw(int rd, int rs1, int shamt)
        => this.EmitCodeFormatI(0x1b, rd, 0b001, rs1, (shamt & 0b11111));

    [Instruction]
    public long srliw(int rd, int rs1, int shamt)
        => this.EmitCodeFormatI(0x1b, rd, 0b101, rs1, (shamt & 0b11111));

    [Instruction]
    public long sraiw(int rd, int rs1, int shamt)
        => this.EmitCodeFormatI(0x1b, rd, 0b101, rs1, (shamt & 0b11111)|((0b0100000)<<5));

    [Instruction]
    public long addw(int rd, int rs1, int rs2)
        => this.EmitCodeFormatR(0x3b, rd, 0b000, rs1, rs2, 0b0000000);

    [Instruction]
    public long subw(int rd, int rs1, int rs2)
        => this.EmitCodeFormatR(0x3b, rd, 0b000, rs1, rs2, 0b0100000);

    [Instruction]
    public long sllw(int rd, int rs1, int rs2)
        => this.EmitCodeFormatR(0x3b, rd, 0b001, rs1, rs2, 0b0000000);

    [Instruction]
    public long srlw(int rd, int rs1, int rs2, int imm)
        => this.EmitCodeFormatR(0x3b, rd, 0b101, rs1, rs2, 0b0000000);

    [Instruction]
    public long sraw(int rd, int rs1, int rs2, int imm)
        => this.EmitCodeFormatR(0x3b, rd, 0b101, rs1, rs2, 0b0100000);

    [Instruction]
    public long jalr(int rd, int imm)
        => this.EmitCodeFormatJ(0x67, rd, imm);

    [Instruction]
    public long lui(int rd, int imm)
        => this.EmitCodeFormatU(0x37, rd, imm);

    [Instruction]
    public long auipc(int rd, int imm)
        => this.EmitCodeFormatU(0x6f, rd, imm);

    protected virtual long EmitCodeFormatR(int opcode, int rd, int funct3,int rs1,int rs2,int funct7)
        => this.EmitCode(
            (opcode & 0b1111111)
            | ((rd & 0b11111) << 7)
            | ((funct3 & 0b111)<<12)
            | ((rs1 & 0b11111)<<15)
            | ((rs2 & 0b11111)<<20)
            | ((funct7 & 0b1111111)<<25)
            );

    protected virtual long EmitCodeFormatI(int opcode, int rd, int funct3, int rs1, int imm)
        => this.EmitCode(
            (opcode & 0b1111111)
            | ((rd & 0b11111) << 7)
            | ((funct3 & 0b111) << 12)
            | ((rs1 & 0b11111) << 15)
            | (imm<<20)
            );

    protected virtual long EmitCodeFormatS(int opcode, int funct3, int rs1,int rs2, int imm)
        => this.EmitCode((opcode & 0b1111111)
            | ((imm & 0b11111) << 7)
            | ((funct3 & 0b111) <<12)
            | ((rs1 & 0b11111) << 15)
            | ((rs2 & 0b11111) << 20)
            | (((imm & 0b111111100000)>>5) << 25))
            ;

    protected virtual long EmitCodeFormatB(int opcode, int funct3, int rs1, int rs2, int imm)
        => this.EmitCode((opcode & 0b1111111)
            | (((imm & 0b100000000000)>>11) << 7)
            | ((imm & 0b11110) << 8)
            | ((funct3 & 0b111) << 12)
            | ((rs1 & 0b11111)<<15)
            | ((rs2 & 0b11111)<<20)
            | (((imm & 0b11111100000)>>5)<<25)
            | (((imm & 0b1000000000000)>>12)<<31)
            );

    protected virtual long EmitCodeFormatU(int opcode, int rd, int imm)
        => this.EmitCode((opcode&0b1111111) 
            | (int)(imm & 0b11111111111111111111000000000000)
            | ((rd & 0b11111) << 7)
            );

    protected virtual long EmitCodeFormatJ(int opcode, int rd, int imm)
        => this.EmitCode((opcode & 0b1111111)
            | ((rd & 0b11111) << 7)
            | ((imm & 0b11111111000000000000))
            | (((imm & 0b100000000000)>>11)<<20)
            | (((imm & 0b11111111110) >> 1) << 21)
            | (((imm & 0b100000000000000000000) >> 20) << 31)
            );

    protected virtual long EmitCode(int code)
    { 
        this.BinaryWriter.Write(code);
        return this.BinaryWriter.BaseStream.Position;
    }

    public CodeGenerator Generate(List<RiscVInstruction> instructions)
    {
        instructions.ForEach(instruction => instruction.Emit(this));
        return this;
    }
}
