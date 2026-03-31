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

                case 0x32: // STA addr
                    ushort addr = ReadWord();
                    memory.Write(addr, A);
                    break;

                case 0x3A: // LDA addr
                    addr = ReadWord();
                    A = memory.Read(addr);
                    break;

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
                case 0x3C: A++; break;

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


                case 0x76: // HLT
                    throw new Exception("HLT");

                default:
                    throw new NotImplementedException($"Opcode {opcode:X2} not implemented");
            }
        }

        private ushort ReadWord()
        {
            byte low = memory.Read(PC++);
            byte high = memory.Read(PC++);
            return (ushort)(low | (high << 8));
        }
    }
}