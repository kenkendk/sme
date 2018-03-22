using System;
using SME;

namespace SimpleMIPS
{
    public enum Opcodes : int
    {
        Rformat,
        ignore_1,
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
        ignore_16,
        floating = 17,
        ignore_18,
        ignore_19,
        ignore_20,
        ignore_21,
        ignore_22,
        ignore_23,
        ignore_24,
        ignore_25,
        ignore_26,
        ignore_27,
        ignore_28,
        ignore_29,
        ignore_30,
        ignore_31,
        lb = 32,
        lh,
        lwl,
        lw = 35,
        lbu,
        lhu,
        lwr,
        ignore_39,
        sb = 40,
        sh,
        swl,
        sw = 43,
        ignore_44,
        ignore_45,
        swr = 46,
        cache,
        ll,
        lwc1,
        lwc2,
        pref,
        ignore_52,
        ldc1 = 53,
        ldc2,
        ignore_55,
        sc = 56,
        swc1,
        swc2,
        ignore_59,
        ignore_60,
        sdc1 = 61,
        sdc2 = 62,
        terminate = 63, // e.g. When instruction is 0xFFFFFFFF //ignore_63,
    }

    public enum Funcs
    {
        sll,
        ignore_1,
        srl = 2,
        sra,
        sllv,
        ignore_5,
        srlv = 6,
        srav,
        jr,
        jalr,
        movz,
        movn,
        syscall,
        bbreak, // break is a keyword
        ignore_14,
        sync = 15,
        mfhi,
        mthi,
        mflo,
        mtlo,
        ignore_20,
        ignore_21,
        ignore_22,
        ignore_23,
        mult = 24,
        multu,
        div,
        divu,
        ignore_28,
        ignore_29,
        ignore_30,
        ignore_31,
        add = 32,
        addu,
        sub,
        subu,
        and,
        or,
        xor,
        nor,
        ignore_40,
        ignore_41,
        slt = 42,
        sltu,
        ignore_44,
        ignore_45,
        ignore_46,
        ignore_47,
        tge = 48,
        tgeu,
        tlt,
        tltu,
        teq,
        ignore_53,
        tne = 54,
        ignore_55,
        ignore_56,
        ignore_57,
        ignore_58,
        ignore_59,
        ignore_60,
        ignore_61,
        ignore_62,
        ignore_63,
    };

    [ClockedProcess]
    public class CPU : StateProcess
    {
        public CPU() 
        {
            /* 
            // In case of the simple program, hardcode registers 1 and 2
            registers[1] = 5;
            registers[2] = 2;
            */

            // Set the stack pointer to the maximum memory address
            registers[29] = (uint)MemoryConstants.max_addr;    
        }

        [InputBus]
        MemoryOutput memout = Scope.CreateOrLoadBus<MemoryOutput>();

        [OutputBus]
        MemoryInput memin = Scope.CreateOrLoadBus<MemoryInput>();

        [OutputBus]
        Terminate terminate = Scope.CreateOrLoadBus<Terminate>();

        uint iptr = 0;
        uint instruction = 0;
        uint[] registers = new uint[32];

        protected async override System.Threading.Tasks.Task OnTickAsync()
        {
            /*await ClockAsync();
            while (true) {*/
                // Debug print registers
                //Console.Write("["); for (int i = 0; i < 13; i++) Console.Write("{0}, ", registers[i]); Console.WriteLine("]");

                // Fetch instruction
                memin.ena = true;
                memin.addr = iptr;
                memin.wrena = false;
                memin.wrdata = 0;
                await ClockAsync();
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
                                registers[rd] = (uint) ((int)registers[rs] < (int)registers[rt] ? 1 : 0); break;
                            case Funcs.sltu:
                                registers[rd] = (uint) (registers[rs] < registers[rt] ? 1 : 0); break;
                            default:
                                throw new Exception($"Funct not found: {funct}");
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
                        await ClockAsync();
                        registers[rt] = memout.rddata;
                        break;
                    case Opcodes.sw:
                        memin.ena = true;
                        memin.addr = (uint)((int)registers[rs] + ext) >> 2; // Right shift because memory is word array not byte
                        memin.wrena = true;
                        memin.wrdata = registers[rt];
                        await ClockAsync();
                        break;
                    case Opcodes.terminate:
                        terminate.flg = true;
                        return;
                    default:
                        throw new Exception($"Opcode not found: {opcode}");
                }
            //}
        }
    }
}
