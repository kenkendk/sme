using System;
using SME;

namespace SimpleMIPS
{
    public enum Opcodes : int
    {
        Rformat,
        j = 2,
        jal,
        beq,
        bne,
        blez,
        bgtz,
        addi = 8,
        addiu,
        slti,
        sltiu,
        andi,
        ori,
        xori,
        lui,
        floating = 17,
        lb = 32,
        lh,
        lwl,
        lw = 35,
        lbu,
        lhu,
        lwr,
        sb = 40,
        sh,
        swl,
        sw = 43,
        swr = 46,
        cache,
        ll,
        lwc1,
        lwc2,
        pref,
        ldc1 = 53,
        ldc2,
        sc = 56,
        swc1,
        swc2,
        sdc1 = 61,
        sdc2 = 62,
        terminate = 63, // e.g. When instruction is 0xFFFFFFFF
    }

    public enum Funcs
    {
        sll,
        srl = 2,
        sra,
        sllv,
        srlv = 6,
        srav,
        jr,
        jalr,
        movz,
        movn,
        syscall,
        bbreak, // break is a keyword
        sync = 15,
        mfhi,
        mthi,
        mflo,
        mtlo,
        mult = 24,
        multu,
        div,
        divu,
        add = 32,
        addu,
        sub,
        subu,
        and,
        or,
        xor,
        nor,
        slt = 42,
        sltu,
        tge = 48,
        tgeu,
        tlt,
        tltu,
        teq,
        tne = 54,
    };

    [ClockedProcess]
    public class CPU : StateProcess
    {
        public CPU()
        {
            // Set the stack pointer to the maximum memory address
            registers[29] = ((uint)MemoryConstants.max_addr) << 2;
        }

        [InputBus]
        public MemoryOutput memout;

        [OutputBus]
        public MemoryInput memin = Scope.CreateBus<MemoryInput>();

        [OutputBus]
        public Terminate terminate = Scope.CreateBus<Terminate>();

        uint iptr = 0;
        uint instruction = 0;
        uint[] registers = new uint[32];

        protected async override System.Threading.Tasks.Task OnTickAsync()
        {
            // Fetch instruction
            memin.ena = true;
            memin.addr = iptr;
            memin.wrena = false;
            memin.wrdata = 0;
            await ClockAsync();
            memin.ena = false;
            await ClockAsync();
            iptr++;

            // Extract fields from the instruction
            instruction = memout.rddata;
            Opcodes opcode = (Opcodes)((instruction >> 26) & 0x3F);
            byte rs     = (byte)((instruction >> 21) & 0x1F);
            byte rt     = (byte)((instruction >> 16) & 0x1F);
            byte rd     = (byte)((instruction >> 11) & 0x1F);
            byte shamt  = (byte)((instruction >> 6)  & 0x1F);
            Funcs funct = (Funcs)(instruction        & 0x3F);
            uint jaddr  = (uint) (instruction        & 0x03FFFFFF);
            short imm   = (short)(instruction        & 0xFFFF);
            int ext   = (int)imm;
            uint zext = (uint) (0x0 | imm);
            await ClockAsync();

            // Run the instruction
            switch (opcode)
            {
                case Opcodes.j:
                    // ignore top bits from pc
                    iptr = jaddr;
                    break;
                case Opcodes.jal:
                    registers[31] = iptr;
                    iptr = jaddr;
                    break;
                case Opcodes.Rformat:
                    switch (funct)
                    {
                        case Funcs.sll:
                            registers[rd] = registers[rt] << shamt; break;
                        case Funcs.srl:
                            registers[rd] = registers[rt] >> shamt; break;
                        case Funcs.jr:
                            iptr = registers[rs]; break;
                        case Funcs.add:
                            registers[rd] = (uint)((int)registers[rs] + (int)registers[rt]); break;
                        case Funcs.addu:
                            registers[rd] = registers[rs] + registers[rt]; break;
                        case Funcs.sub:
                            registers[rd] = (uint)((int)registers[rs] - (int)registers[rt]); break;
                        case Funcs.subu:
                            registers[rd] = registers[rs] - registers[rt]; break;
                        case Funcs.and:
                            registers[rd] = registers[rs] & registers[rt]; break;
                        case Funcs.or:
                            registers[rd] = registers[rs] | registers[rt]; break;
                        case Funcs.slt:
                            if ((int)registers[rs] < (int)registers[rt])
                                registers[rd] = 1;
                            else
                                registers[rd] = 0;
                            break;
                        case Funcs.sltu:
                            if (registers[rs] < registers[rt])
                                registers[rd] = 1;
                            else
                                registers[rd] = 0;
                            break;
                        default:
                            SimulationOnly(() => throw new Exception($"Funct not found: {funct}"));
                            break;
                    }
                    break;
                case Opcodes.beq:
                    if (registers[rs] == registers[rt])
                    {
                        iptr = (uint)((int)iptr + ext);
                    }
                    break;
                case Opcodes.bne:
                    if (registers[rs] != registers[rt])
                    {
                        iptr = (uint)((int)iptr + ext);
                    }
                    break;
                case Opcodes.addi:
                    registers[rt] = (uint) ((int)registers[rs] + ext);
                    break;
                case Opcodes.andi:
                    registers[rt] = registers[rs] & zext;
                    break;
                case Opcodes.ori:
                    registers[rt] = registers[rs] | zext;
                    break;
                case Opcodes.lw:
                    memin.ena = true;
                    memin.addr = (uint)((int)registers[rs] + ext) >> 2; // Right shift because memory is word array not byte
                    memin.wrena = false;
                    memin.wrdata = 0;
                    await ClockAsync();
                    memin.ena = false;
                    await ClockAsync();
                    registers[rt] = memout.rddata;
                    break;
                case Opcodes.sw:
                    memin.ena = true;
                    memin.addr = (uint)((int)registers[rs] + ext) >> 2; // Right shift because memory is word array not byte
                    memin.wrena = true;
                    memin.wrdata = registers[rt];
                    await ClockAsync();
                    memin.ena = false;
                    memin.wrena = false;
                    break;
                case Opcodes.terminate: // Not quite MIPS standard :)
                    terminate.flg = true;
                    return;
                default:
                    SimulationOnly(() => throw new Exception($"Opcode not found: {opcode}"));
                    break;
            }
        }
    }
}
