namespace Vector06cEmulator
{
    public class Cpu8080
    {
        public byte A, B, C, D, E, H, L;
        public ushort PC, SP;

        public bool Z, S, P, CY, AC;

        private Memory memory;

        public Cpu8080(Memory memory)
        {
            this.memory = memory;
        }

        public void Step()
        {
            ushort oldPC = PC;
            byte opcode = memory.Read(PC++);

            if ((opcode & 0xC0) == 0x40)
            {
                if (opcode == 0x76) // HLT
                    throw new Exception("HLT");

                int dst = (opcode >> 3) & 7;
                int src = opcode & 7;

                byte value = GetReg(src);
                SetReg(dst, value);
                return;
            }

            Console.WriteLine($"PC={oldPC:X4} OPCODE={opcode:X2}");

            Execute(opcode);
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

        private void Execute(byte opcode)
        {
            switch (opcode)
            {
                case 0x00: // NOP
                    break;

                case 0x3E: // MVI A, byte
                    A = memory.Read(PC++);
                    break;

                case 0x06: // MVI B, byte
                    B = memory.Read(PC++);
                    break;

                case 0x0E: // MVI C, byte
                    C = memory.Read(PC++);
                    break;

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
                case 0x03: // INX B
                    SetBC((ushort)(GetBC() + 1));
                    break;

                case 0x13: // INX D
                    SetDE((ushort)(GetDE() + 1));
                    break;

                case 0x23: // INX H
                    SetHL((ushort)(GetHL() + 1));
                    break;
                case 0x33: // INX SP
                    SP++;
                    break;

                // DCX (уменьшение)
                case 0x0B: SetBC((ushort)(GetBC() - 1)); break; // DCX B
                case 0x1B: SetDE((ushort)(GetDE() - 1)); break; // DCX D
                case 0x2B: SetHL((ushort)(GetHL() - 1)); break; // DCX H
                case 0x3B: SP--; break;

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

                // RET-условные
                case 0xC0: // RNZ
                    if (!Z) PC = Pop();
                    break;

                case 0xC8: // RZ
                    if (Z) PC = Pop();
                    break;



                case 0x30: // SIM
                           // пока игнорируем
                    break;

                case 0x76: // HLT
                    throw new Exception("HLT");

                default:
                    throw new NotImplementedException($"Opcode {opcode:X2} not implemented");
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
            SP--;
            memory.Write(SP, (byte)(value >> 8)); // high

            SP--;
            memory.Write(SP, (byte)(value & 0xFF)); // low
        }

        ushort Pop()
        {
            byte low = memory.Read(SP);
            SP++;

            byte high = memory.Read(SP);
            SP++;

            return (ushort)(low | (high << 8));
        }

        private ushort ReadWord()
        {
            byte low = memory.Read(PC++);
            byte high = memory.Read(PC++);
            return (ushort)(low | (high << 8));
        }
    }
}