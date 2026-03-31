using System.Runtime.Intrinsics.X86;

namespace Vector06cEmulator
{
    public class Cpu8080
    {
        public byte A, B, C, D, E, H, L;
        public ushort PC, SP;

        public bool Z, S, P, CY, AC;

        private Memory memory;

        public bool Halted { get; private set; } = false;

        public Cpu8080(Memory memory)
        {
            this.memory = memory;
        }

        private const int MAX_STEPS = 100; // защита от бесконечных циклов
        private int stepCounter = 0;

        public void Step()
        {
            if (Halted) return;
            Console.WriteLine($"Step {stepCounter}: PC={PC:X4}");

            if (stepCounter++ > MAX_STEPS)
            {
                Console.WriteLine($"Maximum steps exceeded at PC={PC:X4}. Possible infinite loop.");
                Console.ReadLine();
            }

            ushort oldPC = PC;
            byte opcode = memory.Read(PC++);

            Console.WriteLine($"PC={oldPC:X4} OPCODE={opcode:X2}");

            switch (opcode)
            {
                case 0x76: // HLT
                    Halted = true;
                    Console.WriteLine("CPU halted.");
                    return;

                case 0x3E: // MVI A, byte
                    A = memory.Read(PC++);
                    return;

                case 0x32: // STA addr
                    {
                        ushort addr = memory.Read(PC++);
                        addr |= (ushort)(memory.Read(PC++) << 8);
                        memory.Write(addr, A);
                        return;
                    }

                case 0x33: // INX SP
                    SP++;
                    return;

                case 0x3B: // DCX SP
                    SP--;
                    return;

                case 0x20: // RIM
                    A = 0x00;
                    return;

                case 0x34: // INR M
                    {
                        ushort addr = GetHL();
                        byte val = memory.Read(addr);
                        val++;
                        memory.Write(addr, val);
                        return;
                    }

                // MOV (0x40-0x7F)
                default:
                    if ((opcode & 0xC0) == 0x40)
                    {
                        int dst = (opcode >> 3) & 7;
                        int src = opcode & 7;
                        SetReg(dst, GetReg(src));
                        return;
                    }

                    Console.WriteLine($"Warning: Opcode {opcode:X2} not implemented at PC={oldPC:X4}");
                    return;
            }
        }

        byte GetReg(int code)
        {
            return code switch
            {
                0 => B,
                1 => C,
                2 => D,
                3 => E,
                4 => H,
                5 => L,
                6 => memory.Read(GetHL()), // M
                7 => A,
                _ => 0
            };
        }

        void SetReg(int code, byte value)
        {
            switch (code)
            {
                case 0: B = value; break;
                case 1: C = value; break;
                case 2: D = value; break;
                case 3: E = value; break;
                case 4: H = value; break;
                case 5: L = value; break;
                case 6: memory.Write(GetHL(), value); break; // M
                case 7: A = value; break;
            }
        }

        ushort GetBC() => (ushort)((B << 8) | C);
        void SetBC(ushort val)
        {
            B = (byte)(val >> 8);
            C = (byte)(val & 0xFF);
        }

        ushort GetDE() => (ushort)((D << 8) | E);
        void SetDE(ushort val)
        {
            D = (byte)(val >> 8);
            E = (byte)(val & 0xFF);
        }

        ushort GetHL() => (ushort)((H << 8) | L);
        void SetHL(ushort val)
        {
            H = (byte)(val >> 8);
            L = (byte)(val & 0xFF);
        }

        private void Execute(byte opcode, ushort oldPC)
        {
            switch (opcode)
            {
                case 0x00: // HALT / NOP
                    Console.WriteLine("End of program");
                    break;

                case 0x3E: A = memory.Read(PC++); break; // MVI A, byte
                case 0x06: B = memory.Read(PC++); break; // MVI B, byte
                case 0x0E: C = memory.Read(PC++); break; // MVI C, byte

                case 0x3A: // LDA addr
                    {
                        ushort addr = ReadWord();
                        A = memory.Read(addr);
                        break;
                    }

                case 0xC3: // JMP addr
                    PC = ReadWord();
                    break;

                // Increment 03, 13, 23, 33
                case 0x03: SetBC((ushort)(GetBC() + 1)); break; // INX B
                case 0x13: SetDE((ushort)(GetDE() + 1)); break; // INX D
                case 0x23: SetHL((ushort)(GetHL() + 1)); break; // INX H
                case 0x33: SP++; break;                         // INX SP

                // DCX (уменьшение)
                case 0x0B: SetBC((ushort)(GetBC() - 1)); break; // DCX B
                case 0x1B: SetDE((ushort)(GetDE() - 1)); break; // DCX D
                case 0x2B: SetHL((ushort)(GetHL() - 1)); break; // DCX H
                case 0x3B: SP--; break;                         // DCX SP

                case 0x01: // LXI B, d16
                    SetBC(ReadWord());
                    break;

                case 0x11: // LXI D, d16
                    SetDE(ReadWord());
                    break;

                case 0x21: // LXI H, d16
                    SetHL(ReadWord());
                    break;

                case 0x31: // LXI SP, d16
                    SP = ReadWord();
                    break;

                // DAD — сложение 16-бит
                case 0x09: // DAD B
                    {
                        int result = GetHL() + GetBC();
                        CY = result > 0xFFFF;
                        SetHL((ushort)result);
                        break;
                    }

                case 0x19: // DAD D
                    {
                        int result = GetHL() + GetDE();
                        CY = result > 0xFFFF;
                        SetHL((ushort)result);
                        break;
                    }

                case 0x29: // DAD H
                    {
                        int result = GetHL() + GetHL();
                        CY = result > 0xFFFF;
                        SetHL((ushort)result);
                        break;
                    }

                case 0x39: // DAD SP
                    {
                        int result = GetHL() + SP;
                        CY = result > 0xFFFF;
                        SetHL((ushort)result);
                        break;
                    }


                // Побитовые / сдвиги (RLC / RRC / RAL / RAR)
                case 0x07: // RLC
                    A = (byte)((A << 1) | (A >> 7));
                    CY = (A & 0x01) != 0;
                    break;

                case 0x0F: // RRC
                    CY = (A & 0x01) != 0;
                    A = (byte)((A >> 1) | (A << 7));
                    break;

                case 0x17: // RAL
                    {
                        bool oldCY = CY;
                        CY = (A & 0x80) != 0;
                        A = (byte)((A << 1) | (oldCY ? 1 : 0));
                        break;
                    }

                case 0x1F: // RAR
                    {
                        bool oldCY = CY;
                        CY = (A & 0x01) != 0;
                        A = (byte)((A >> 1) | (oldCY ? 0x80 : 0));
                        break;
                    }

                // XRA / ORA / ANA (логика с флагами)
                case 0xA8: Xra(B); break;
                case 0xA9: Xra(C); break;
                case 0xAA: Xra(D); break;
                case 0xAB: Xra(E); break;
                case 0xAC: Xra(H); break;
                case 0xAD: Xra(L); break;
                case 0xAE: Xra(memory.Read(GetHL())); break;
                case 0xAF: Xra(A); break;

                case 0xEE: // XRI byte
                    Xra(memory.Read(PC++));
                    break;

                // INR / DCR (инкремент/декремент)
                case 0x04: B++; break;
                case 0x0C: C++; break;
                case 0x14: D++; break;
                case 0x1C: E++; break;
                case 0x24: H++; break;
                case 0x2C: L++; break;
                case 0x3C: // INR A
                    A++;
                    SetFlagsZSP(A);
                    break;

                case 0x05: B--; break;
                case 0x0D: C--; break;
                case 0x15: D--; break;
                case 0x1D: E--; break;
                case 0x25: H--; break;
                case 0x2D: L--; break;
                case 0x3D: A--; break;

                // ADD
                case 0x80: A += B; break;
                case 0x81: A += C; break;
                case 0x82: A += D; break;
                case 0x83: A += E; break;
                case 0x84: A += H; break;
                case 0x85: A += L; break;
                case 0x86: A += memory.Read(GetHL()); break;
                case 0x87: A += A; break;

                case 0x32: // STA
                    {
                        ushort addr = ReadWord();
                        memory.Write(addr, A);
                        break;
                    }

                case 0x34: // INR M
                    {
                        ushort addr = GetHL();
                        byte val = memory.Read(addr);
                        val++;
                        memory.Write(addr, val);
                        break;
                    }

                case 0x20: // RIM
                    A = 0x00; // все прерывания "выключены"
                    break;

                //PUSH / POP инструкции
                case 0xC5: // PUSH B
                    Push(GetBC());
                    break;

                case 0xD5: // PUSH D
                    Push(GetDE());
                    break;

                case 0xE5: // PUSH H
                    Push(GetHL());
                    break;

                case 0xF5: // PUSH PSW (A + flags)
                    Push((ushort)((A << 8) | GetFlags()));
                    break;

                case 0xC1: // POP B
                    SetBC(Pop());
                    break;

                case 0xD1: // POP D
                    SetDE(Pop());
                    break;

                case 0xE1: // POP H
                    SetHL(Pop());
                    break;

                case 0xF1: // POP PSW
                    {
                        ushort val = Pop();
                        A = (byte)(val >> 8);
                        SetFlags((byte)(val & 0xFF));
                        break;
                    }

                // условные переходы
                case 0xC2: // JNZ addr
                    {
                        ushort addr = ReadWord();
                        if (!Z)
                            PC = addr;
                        break;
                    }

                case 0xCA: // JZ addr
                    {
                        ushort addr = ReadWord();
                        if (Z)
                            PC = addr;
                        break;
                    }

                case 0xDA: // JC addr
                    {
                        ushort addr = ReadWord();
                        if (CY)
                            PC = addr;
                        break;
                    }

                case 0xD2: // JNC addr
                    {
                        ushort addr = ReadWord();
                        if (!CY)
                            PC = addr;
                        break;
                    }

                // CALL
                case 0xC4: // CNZ
                    {
                        ushort addr = ReadWord();
                        if (!Z)
                        {
                            Push(PC);
                            PC = addr;
                        }
                        break;
                    }

                case 0xCC: // CZ
                    {
                        ushort addr = ReadWord();
                        if (Z)
                        {
                            Push(PC);
                            PC = addr;
                        }
                        break;
                    }

                // Условные CALL и RET
                case 0xD4: // CNC addr
                    {
                        ushort addr = ReadWord();
                        if (!CY)
                        {
                            Push(PC);
                            PC = addr;
                        }
                        break;
                    }

                case 0xDC: // CC addr
                    {
                        ushort addr = ReadWord();
                        if (CY)
                        {
                            Push(PC);
                            PC = addr;
                        }
                        break;
                    }

                case 0xF4: // CP addr
                    {
                        ushort addr = ReadWord();
                        if (!P)
                        {
                            Push(PC);
                            PC = addr;
                        }
                        break;
                    }

                case 0xFC: // CP addr (parity even)
                    {
                        ushort addr = ReadWord();
                        if (P)
                        {
                            Push(PC);
                            PC = addr;
                        }
                        break;
                    }

                // RET
                case 0xD0: if (!CY) PC = Pop(); break; // RNC
                case 0xD8: if (CY) PC = Pop(); break;  // RC
                case 0xF0: if (!P) PC = Pop(); break;  // RPO
                case 0xF8: if (P) PC = Pop(); break;   // RPE

                // DAA — Decimal Adjust Accumulator
                case 0x27: // DAA
                    {
                        int correction = 0;
                        bool setCY = false;

                        if ((A & 0x0F) > 9 || AC) correction += 0x06;
                        if ((A >> 4) > 9 || CY) { correction += 0x60; setCY = true; }

                        int result = A + correction;
                        AC = ((A ^ result) & 0x10) != 0;
                        A = (byte)(result & 0xFF);
                        CY = setCY;
                        SetFlagsZSP(A);
                        break;
                    }

                // RST n (restart) — вызов подпрограммы по адресу n*8
                case 0xC7: // RST 0
                    Push(PC);
                    PC = 0x0000;
                    break;

                // Остальные RET/CALL без условий
                case 0xC9: // RET
                    PC = Pop();
                    break;

                case 0xCD: // CALL addr
                    {
                        ushort addr = ReadWord();
                        Push(PC);
                        PC = addr;
                        break;
                    }

                // EI / DI — Enable/Disable Interrupts
                case 0xFB: // EI
                    // пока игнорируем, прерывания не реализованы
                    break;

                // DI
                case 0xF3:
                    // пока игнорируем
                    break;

                // RST 1..7
                case 0xCF: // RST 1
                    Push(PC);
                    PC = 0x0008;
                    break;

                // RST 2..7
                case 0xD7: // RST 2
                    Push(PC);
                    PC = 0x0010;
                    break;

                // RST 3..7
                case 0xDF: // RST 3
                    Push(PC);
                    PC = 0x0018;
                    break;

                // RST 4..7
                case 0xE7: // RST 4
                    Push(PC);
                    PC = 0x0020;
                    break;

                // RST 5..7
                case 0xEF: // RST 5
                    Push(PC);
                    PC = 0x0028;
                    break;

                // RST 6..7
                case 0xF7: // RST 6
                    Push(PC);
                    PC = 0x0030;
                    break;

                // RST 7
                case 0xFF: // RST 7
                    Push(PC);
                    PC = 0x0038;
                    break;

                // IN / OUT (простая заглушка для Vector-06Ц)
                case 0xDB: // IN port
                    byte port = memory.Read(PC++);
                    A = 0xFF; // заглушка, клавиатура/порты пока не подключены
                    break;

                case 0xD3: // OUT port
                    port = memory.Read(PC++);
                    // заглушка — можно логировать, потом подключить дисплей/звук
                    break;


                // RET-условные
                case 0xC0: // RNZ
                    if (!Z) PC = Pop();
                    break;

                case 0xC8: // RZ
                    if (Z) PC = Pop();
                    break;

                // SUB r / SUI byte
                case 0x90: Sub(B); break;
                case 0x91: Sub(C); break;
                case 0x92: Sub(D); break;
                case 0x93: Sub(E); break;
                case 0x94: Sub(H); break;
                case 0x95: Sub(L); break;
                case 0x96: Sub(memory.Read(GetHL())); break;
                case 0x97: Sub(A); break;

                // CMP r / CPI byte
                case 0xB8: Cmp(B); break;
                case 0xB9: Cmp(C); break;
                case 0xBA: Cmp(D); break;
                case 0xBB: Cmp(E); break;
                case 0xBC: Cmp(H); break;
                case 0xBD: Cmp(L); break;
                case 0xBE: Cmp(memory.Read(GetHL())); break;
                case 0xBF: Cmp(A); break;

                // ANA r / ANI byte (AND)
                case 0xA0: Ana(B); break;
                case 0xA1: Ana(C); break;
                case 0xA2: Ana(D); break;
                case 0xA3: Ana(E); break;
                case 0xA4: Ana(H); break;
                case 0xA5: Ana(L); break;
                case 0xA6: Ana(memory.Read(GetHL())); break;
                case 0xA7: Ana(A); break;

                // ORA r / ORI byte (OR)
                case 0xB0: Ora(B); break;
                case 0xB1: Ora(C); break;
                case 0xB2: Ora(D); break;
                case 0xB3: Ora(E); break;
                case 0xB4: Ora(H); break;
                case 0xB5: Ora(L); break;
                case 0xB6: Ora(memory.Read(GetHL())); break;
                case 0xB7: Ora(A); break;

                case 0xF6: // ORI byte
                    Ora(memory.Read(PC++));
                    break;

                case 0xE6: // ANI byte
                    Ana(memory.Read(PC++));
                    break;

                case 0xFE: // CPI byte
                    Cmp(memory.Read(PC++));
                    break;

                case 0xD6: // SUI byte
                    Sub(memory.Read(PC++));
                    break;

                case 0x30: // SIM
                           // пока игнорируем
                    break;

                // LDAX / STAX — косвенные загрузка/сохранение
                case 0x0A: // LDAX B
                    A = memory.Read(GetBC());
                    break;

                case 0x1A: // LDAX D
                    A = memory.Read(GetDE());
                    break;

                case 0x02: // STAX B
                    memory.Write(GetBC(), A);
                    break;

                case 0x12: // STAX D
                    memory.Write(GetDE(), A);
                    break;


                case 0x76: // HLT
                    throw new Exception("HLT");

                default:
                    Console.WriteLine($"Warning: Opcode {opcode:X2} not implemented at PC={oldPC:X4}");
                    PC++;
                    break;
            }
        }

        byte GetFlags()
        {
            byte f = 0;
            if (Z) f |= 0x40;
            if (S) f |= 0x80;
            if (P) f |= 0x04;
            if (CY) f |= 0x01;
            if (AC) f |= 0x10;
            return f;
        }

        void SetFlags(byte f)
        {
            Z = (f & 0x40) != 0;
            S = (f & 0x80) != 0;
            P = (f & 0x04) != 0;
            CY = (f & 0x01) != 0;
            AC = (f & 0x10) != 0;
        }

        void SetFlagsZSP(byte value)
        {
            Z = value == 0;
            S = (value & 0x80) != 0;
            P = CountBits(value) % 2 == 0;
        }

        int CountBits(byte b)
        {
            int count = 0;
            for (int i = 0; i < 8; i++)
                if ((b & (1 << i)) != 0) count++;
            return count;
        }

        void Push(ushort value)
        {
            memory.Write(--SP, (byte)(value >> 8)); // high
            memory.Write(--SP, (byte)(value & 0xFF)); // low
        }

        ushort Pop()
        {
            byte low = memory.Read(SP++);
            byte high = memory.Read(SP++);
            return (ushort)((high << 8) | low);
        }

        void Add(byte value)
        {
            int result = A + value;
            bool carry = result > 0xFF;
            bool ac = AuxCarryAdd(A, value);
            A = (byte)result;
            SetFlagsAfterAddSub(A, carry, ac);
        }

        void SetFlagsAfterAdd(byte result, byte a, byte b, bool carry)
        {
            Z = result == 0;
            S = (result & 0x80) != 0;
            P = CountBits(result) % 2 == 0;
            CY = carry;
            AC = ((a & 0x0F) + (b & 0x0F)) > 0x0F;
        }

        void SetFlagsAfterSub(byte result, byte a, byte b, bool carry)
        {
            Z = result == 0;
            S = (result & 0x80) != 0;
            P = CountBits(result) % 2 == 0;
            CY = carry;
            AC = (a & 0x0F) < (b & 0x0F);
        }

        // вспомогательная функция для подсчета Auxiliary Carry
        bool AuxCarryAdd(byte a, byte b)
        {
            return ((a & 0x0F) + (b & 0x0F)) > 0x0F;
        }

        bool AuxCarrySub(byte a, byte b)
        {
            return (a & 0x0F) < (b & 0x0F);
        }

        // SUB r / SUI byte
        void Sub(byte value)
        {
            byte oldA = A;
            int result = A - value;
            bool ac = AuxCarrySub(A, value);
            bool cy = result < 0;

            A = (byte)(result & 0xFF);
            SetFlagsAfterAddSub(A, cy, ac);
        }

        // CMP r / CPI byte
        void Cmp(byte value)
        {
            int result = A - value;
            bool ac = AuxCarrySub(A, value);
            bool cy = result < 0;
            byte res = (byte)(result & 0xFF);
            SetFlagsAfterAddSub(res, cy, ac);
        }

        // ANA r / ANI byte (AND)
        void Ana(byte value)
        {
            AC = ((A | value) & 0x08) != 0; // точная установка Auxiliary Carry
            A &= value;
            CY = false;
            SetFlagsZSP(A);
        }

        // ORA r / ORI byte (OR)
        void Ora(byte value)
        {
            A |= value;
            CY = false;
            AC = false;
            SetFlagsZSP(A);
        }

        // XRA r / XRI byte (XOR)
        void Xra(byte value)
        {
            A ^= value;
            CY = false;
            AC = false;
            SetFlagsZSP(A);
        }

        void SetFlagsAfterAddSub(byte result, bool carry, bool ac)
        {
            Z = result == 0;
            S = (result & 0x80) != 0;
            P = CountBits(result) % 2 == 0;
            CY = carry;
            AC = ac;
        }

        private ushort ReadWord()
        {
            byte low = memory.Read(PC++);
            byte high = memory.Read(PC++);
            return (ushort)(low | (high << 8));
        }
    }
}