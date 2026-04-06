using System;

namespace Vector06cEmulator
{
    public class Cpu8080
    {
        public byte A, B, C, D, E, H, L;
        public ushort PC, SP;
        public bool Z, S, P, CY, AC;

        private Memory memory;
        public bool Halted { get; set; } = false; 
        public bool IFF { get; set; } = false;

        private IOBus ioBus;

        public Memory GetMemory() => memory;

        public Cpu8080(Memory memory, IOBus ioBus)
        {
            this.memory = memory;
            this.ioBus = ioBus;
        }

        public void Step()
        {
            if (Halted) return;

            ushort oldPC = PC;

            //Disassembler.Disassemble(memory, oldPC); 

            byte opcode = memory.Read(PC++);

            Execute(opcode, oldPC);
        }

        // Вызвать аппаратное прерывание (RST 7 = вектор 0x0038)
        // Вектор-06Ц использует RST 7 от таймера
        public void Interrupt()
        {
            if (!IFF || Halted) return;
            IFF = false;
            Push(PC);
            PC = 0x0038;
        }

        private void Execute(byte opcode, ushort oldPC)
        {
            // MOV r1, r2 (0x40–0x7F, кроме 0x76 HLT)
            if (opcode != 0x76 && (opcode & 0xC0) == 0x40)
            {
                int dst = (opcode >> 3) & 7;
                int src = opcode & 7;
                SetReg(dst, GetReg(src));
                return;
            }

            switch (opcode)
            {
                case 0x00: // NOP
                    break;

                case 0x76: // HLT
                    Halted = true;
                    Console.WriteLine("CPU halted.");
                    break;

                // ── MVI r, byte ──────────────────────────────────────
                case 0x3E: A = memory.Read(PC++); break;
                case 0x06: B = memory.Read(PC++); break;
                case 0x0E: C = memory.Read(PC++); break;
                case 0x16: D = memory.Read(PC++); break;
                case 0x1E: E = memory.Read(PC++); break;
                case 0x26: H = memory.Read(PC++); break;
                case 0x2E: L = memory.Read(PC++); break;
                case 0x36: memory.Write(GetHL(), memory.Read(PC++)); break; // MVI M, byte

                // ── LXI rp, d16 ──────────────────────────────────────
                case 0x01: SetBC(ReadWord()); break;
                case 0x11: SetDE(ReadWord()); break;
                case 0x21: SetHL(ReadWord()); break;
                case 0x31: SP = ReadWord(); break;

                // ── LDA / STA / LHLD / SHLD ──────────────────────────
                case 0x3A: A = memory.Read(ReadWord()); break;  // LDA addr
                case 0x32: memory.Write(ReadWord(), A); break;  // STA addr
                case 0x2A:                                       // LHLD addr
                    {
                        ushort addr = ReadWord();
                        L = memory.Read(addr);
                        H = memory.Read((ushort)(addr + 1));
                        break;
                    }
                case 0x22:                                       // SHLD addr
                    {
                        ushort addr = ReadWord();
                        memory.Write(addr, L);
                        memory.Write((ushort)(addr + 1), H);
                        break;
                    }

                // ── LDAX / STAX ──────────────────────────────────────
                case 0x0A: A = memory.Read(GetBC()); break;
                case 0x1A: A = memory.Read(GetDE()); break;
                case 0x02: memory.Write(GetBC(), A); break;
                case 0x12: memory.Write(GetDE(), A); break;

                // ── INX ──────────────────────────────────────────────
                case 0x03: SetBC((ushort)(GetBC() + 1)); break;
                case 0x13: SetDE((ushort)(GetDE() + 1)); break;
                case 0x23: SetHL((ushort)(GetHL() + 1)); break;
                case 0x33: SP++; break;

                // ── DCX ──────────────────────────────────────────────
                case 0x0B: SetBC((ushort)(GetBC() - 1)); break;
                case 0x1B: SetDE((ushort)(GetDE() - 1)); break;
                case 0x2B: SetHL((ushort)(GetHL() - 1)); break;
                case 0x3B: SP--; break;

                // ── INR r ────────────────────────────────────────────
                case 0x04: B = Inr(B); break;
                case 0x0C: C = Inr(C); break;
                case 0x14: D = Inr(D); break;
                case 0x1C: E = Inr(E); break;
                case 0x24: H = Inr(H); break;
                case 0x2C: L = Inr(L); break;
                case 0x34:                          // INR M
                    {
                        ushort addr = GetHL();
                        memory.Write(addr, Inr(memory.Read(addr)));
                        break;
                    }
                case 0x3C: A = Inr(A); break;

                // ── DCR r ────────────────────────────────────────────
                case 0x05: B = Dcr(B); break;
                case 0x0D: C = Dcr(C); break;
                case 0x15: D = Dcr(D); break;
                case 0x1D: E = Dcr(E); break;
                case 0x25: H = Dcr(H); break;
                case 0x2D: L = Dcr(L); break;
                case 0x35:                          // DCR M
                    {
                        ushort addr = GetHL();
                        memory.Write(addr, Dcr(memory.Read(addr)));
                        break;
                    }
                case 0x3D: A = Dcr(A); break;

                // ── ADD r ────────────────────────────────────────────
                case 0x80: Add(B); break;
                case 0x81: Add(C); break;
                case 0x82: Add(D); break;
                case 0x83: Add(E); break;
                case 0x84: Add(H); break;
                case 0x85: Add(L); break;
                case 0x86: Add(memory.Read(GetHL())); break;
                case 0x87: Add(A); break;
                case 0xC6: Add(memory.Read(PC++)); break;  // ADI byte

                // ── ADC r ────────────────────────────────────────────
                case 0x88: Adc(B); break;
                case 0x89: Adc(C); break;
                case 0x8A: Adc(D); break;
                case 0x8B: Adc(E); break;
                case 0x8C: Adc(H); break;
                case 0x8D: Adc(L); break;
                case 0x8E: Adc(memory.Read(GetHL())); break;
                case 0x8F: Adc(A); break;
                case 0xCE: Adc(memory.Read(PC++)); break;  // ACI byte

                // ── SUB r ────────────────────────────────────────────
                case 0x90: Sub(B); break;
                case 0x91: Sub(C); break;
                case 0x92: Sub(D); break;
                case 0x93: Sub(E); break;
                case 0x94: Sub(H); break;
                case 0x95: Sub(L); break;
                case 0x96: Sub(memory.Read(GetHL())); break;
                case 0x97: Sub(A); break;
                case 0xD6: Sub(memory.Read(PC++)); break;  // SUI byte

                // ── SBB r ────────────────────────────────────────────
                case 0x98: Sbb(B); break;
                case 0x99: Sbb(C); break;
                case 0x9A: Sbb(D); break;
                case 0x9B: Sbb(E); break;
                case 0x9C: Sbb(H); break;
                case 0x9D: Sbb(L); break;
                case 0x9E: Sbb(memory.Read(GetHL())); break;
                case 0x9F: Sbb(A); break;
                case 0xDE: Sbb(memory.Read(PC++)); break;  // SBI byte

                // ── ANA / ANI ────────────────────────────────────────
                case 0xA0: Ana(B); break;
                case 0xA1: Ana(C); break;
                case 0xA2: Ana(D); break;
                case 0xA3: Ana(E); break;
                case 0xA4: Ana(H); break;
                case 0xA5: Ana(L); break;
                case 0xA6: Ana(memory.Read(GetHL())); break;
                case 0xA7: Ana(A); break;
                case 0xE6: Ana(memory.Read(PC++)); break;

                // ── XRA / XRI ────────────────────────────────────────
                case 0xA8: Xra(B); break;
                case 0xA9: Xra(C); break;
                case 0xAA: Xra(D); break;
                case 0xAB: Xra(E); break;
                case 0xAC: Xra(H); break;
                case 0xAD: Xra(L); break;
                case 0xAE: Xra(memory.Read(GetHL())); break;
                case 0xAF: Xra(A); break;
                case 0xEE: Xra(memory.Read(PC++)); break;

                // ── ORA / ORI ────────────────────────────────────────
                case 0xB0: Ora(B); break;
                case 0xB1: Ora(C); break;
                case 0xB2: Ora(D); break;
                case 0xB3: Ora(E); break;
                case 0xB4: Ora(H); break;
                case 0xB5: Ora(L); break;
                case 0xB6: Ora(memory.Read(GetHL())); break;
                case 0xB7: Ora(A); break;
                case 0xF6: Ora(memory.Read(PC++)); break;

                // ── CMP / CPI ────────────────────────────────────────
                case 0xB8: Cmp(B); break;
                case 0xB9: Cmp(C); break;
                case 0xBA: Cmp(D); break;
                case 0xBB: Cmp(E); break;
                case 0xBC: Cmp(H); break;
                case 0xBD: Cmp(L); break;
                case 0xBE: Cmp(memory.Read(GetHL())); break;
                case 0xBF: Cmp(A); break;
                case 0xFE: Cmp(memory.Read(PC++)); break;

                // ── DAD ──────────────────────────────────────────────
                case 0x09: Dad(GetBC()); break;
                case 0x19: Dad(GetDE()); break;
                case 0x29: Dad(GetHL()); break;
                case 0x39: Dad(SP); break;

                // ── Rotate ───────────────────────────────────────────
                case 0x07: // RLC
                    CY = (A & 0x80) != 0;
                    A = (byte)((A << 1) | (CY ? 1 : 0));
                    break;
                case 0x0F: // RRC
                    CY = (A & 0x01) != 0;
                    A = (byte)((A >> 1) | (CY ? 0x80 : 0));
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

                // ── DAA ──────────────────────────────────────────────
                case 0x27:
                    {
                        int correction = 0;
                        bool setCY = CY;
                        if ((A & 0x0F) > 9 || AC) correction += 0x06;
                        if ((A >> 4) > 9 || CY) { correction += 0x60; setCY = true; }
                        int res = A + correction;
                        AC = ((A ^ res) & 0x10) != 0;
                        A = (byte)(res & 0xFF);
                        CY = setCY;
                        SetFlagsZSP(A);
                        break;
                    }

                // ── CMA / STC / CMC ──────────────────────────────────
                case 0x2F: A = (byte)~A; break;          // CMA
                case 0x37: CY = true; break;              // STC
                case 0x3F: CY = !CY; break;               // CMC

                // ── JMP и условные переходы ──────────────────────────
                case 0xC3: PC = ReadWord(); break;
                case 0xC2: { ushort a = ReadWord(); if (!Z) PC = a; break; } // JNZ
                case 0xCA: { ushort a = ReadWord(); if (Z) PC = a; break; } // JZ
                case 0xD2: { ushort a = ReadWord(); if (!CY) PC = a; break; } // JNC
                case 0xDA: { ushort a = ReadWord(); if (CY) PC = a; break; } // JC
                case 0xE2: { ushort a = ReadWord(); if (!P) PC = a; break; } // JPO
                case 0xEA: { ushort a = ReadWord(); if (P) PC = a; break; } // JPE
                case 0xF2: { ushort a = ReadWord(); if (!S) PC = a; break; } // JP
                case 0xFA: { ushort a = ReadWord(); if (S) PC = a; break; } // JM

                // ── CALL и условные ──────────────────────────────────
                case 0xCD: { ushort a = ReadWord(); Push(PC); PC = a; break; }
                case 0xC4: { ushort a = ReadWord(); if (!Z) { Push(PC); PC = a; } break; }
                case 0xCC: { ushort a = ReadWord(); if (Z) { Push(PC); PC = a; } break; }
                case 0xD4: { ushort a = ReadWord(); if (!CY) { Push(PC); PC = a; } break; }
                case 0xDC: { ushort a = ReadWord(); if (CY) { Push(PC); PC = a; } break; }
                case 0xE4: { ushort a = ReadWord(); if (!P) { Push(PC); PC = a; } break; }
                case 0xEC: { ushort a = ReadWord(); if (P) { Push(PC); PC = a; } break; }
                case 0xF4: { ushort a = ReadWord(); if (!S) { Push(PC); PC = a; } break; }
                case 0xFC: { ushort a = ReadWord(); if (S) { Push(PC); PC = a; } break; }

                // ── RET и условные ───────────────────────────────────
                case 0xC9: PC = Pop(); break;
                case 0xC0: if (!Z) PC = Pop(); break;
                case 0xC8: if (Z) PC = Pop(); break;
                case 0xD0: if (!CY) PC = Pop(); break;
                case 0xD8: if (CY) PC = Pop(); break;
                case 0xE0: if (!P) PC = Pop(); break;
                case 0xE8: if (P) PC = Pop(); break;
                case 0xF0: if (!S) PC = Pop(); break;
                case 0xF8: if (S) PC = Pop(); break;

                // ── PUSH / POP ───────────────────────────────────────
                case 0xC5: Push(GetBC()); break;
                case 0xD5: Push(GetDE()); break;
                case 0xE5: Push(GetHL()); break;
                case 0xF5: Push((ushort)((A << 8) | GetFlags())); break;
                case 0xC1: SetBC(Pop()); break;
                case 0xD1: SetDE(Pop()); break;
                case 0xE1: SetHL(Pop()); break;
                case 0xF1:
                    {
                        ushort val = Pop();
                        A = (byte)(val >> 8);
                        SetFlags((byte)(val & 0xFF));
                        break;
                    }

                // ── RST ──────────────────────────────────────────────
                case 0xC7: Push(PC); PC = 0x0000; break;
                case 0xCF: Push(PC); PC = 0x0008; break;
                case 0xD7: Push(PC); PC = 0x0010; break;
                case 0xDF: Push(PC); PC = 0x0018; break;
                case 0xE7: Push(PC); PC = 0x0020; break;
                case 0xEF: Push(PC); PC = 0x0028; break;
                case 0xF7: Push(PC); PC = 0x0030; break;
                case 0xFF: Push(PC); PC = 0x0038; break;

                // ── XCHG / XTHL / SPHL / PCHL ────────────────────────
                case 0xEB: // XCHG
                    {
                        byte th = H, tl = L;
                        H = D; L = E;
                        D = th; E = tl;
                        break;
                    }
                case 0xE3: // XTHL
                    {
                        byte lo = memory.Read(SP);
                        byte hi = memory.Read((ushort)(SP + 1));
                        memory.Write(SP, L);
                        memory.Write((ushort)(SP + 1), H);
                        L = lo; H = hi;
                        break;
                    }
                case 0xF9: SP = GetHL(); break;  // SPHL
                case 0xE9: PC = GetHL(); break;  // PCHL

                // ── IN / OUT ─────────────────────────────────────────
                case 0xDB:  // IN port
                    {
                        byte port = memory.Read(PC++);
                        A = ioBus.In(port);
                        break;
                    }

                case 0xD3:  // OUT port
                    {
                        byte port = memory.Read(PC++);
                        ioBus.Out(port, A);
                        break;
                    }

                // ── EI / DI / RIM / SIM / NOP ────────────────────────
                case 0xFB: IFF = true; break;  // EI
                case 0xF3: IFF = false; break;  // DI
                case 0x20: break; // RIM (8085) — игнорируем
                case 0x30: break; // SIM (8085) — игнорируем

                default:
                    Console.WriteLine($"Warning: Opcode {opcode:X2} not implemented at PC={oldPC:X4}");
                    break;
            }
        }

        // ── Вспомогательные арифметические операции ──────────────────

        byte Inr(byte v)
        {
            AC = (v & 0x0F) == 0x0F;
            v++;
            SetFlagsZSP(v);
            return v;
        }

        byte Dcr(byte v)
        {
            AC = (v & 0x0F) == 0x00;
            v--;
            SetFlagsZSP(v);
            return v;
        }

        void Add(byte value)
        {
            int result = A + value;
            AC = ((A & 0x0F) + (value & 0x0F)) > 0x0F;
            CY = result > 0xFF;
            A = (byte)result;
            SetFlagsZSP(A);
        }

        void Adc(byte value)
        {
            int result = A + value + (CY ? 1 : 0);
            AC = ((A & 0x0F) + (value & 0x0F) + (CY ? 1 : 0)) > 0x0F;
            CY = result > 0xFF;
            A = (byte)result;
            SetFlagsZSP(A);
        }

        void Sub(byte value)
        {
            int result = A - value;
            AC = (A & 0x0F) < (value & 0x0F);
            CY = result < 0;
            A = (byte)(result & 0xFF);
            SetFlagsZSP(A);
        }

        void Sbb(byte value)
        {
            int result = A - value - (CY ? 1 : 0);
            AC = (A & 0x0F) < ((value & 0x0F) + (CY ? 1 : 0));
            CY = result < 0;
            A = (byte)(result & 0xFF);
            SetFlagsZSP(A);
        }

        void Ana(byte value)
        {
            AC = ((A | value) & 0x08) != 0;
            A &= value;
            CY = false;
            SetFlagsZSP(A);
        }

        void Xra(byte value)
        {
            A ^= value;
            CY = false;
            AC = false;
            SetFlagsZSP(A);
        }

        void Ora(byte value)
        {
            A |= value;
            CY = false;
            AC = false;
            SetFlagsZSP(A);
        }

        void Cmp(byte value)
        {
            int result = A - value;
            AC = (A & 0x0F) < (value & 0x0F);
            CY = result < 0;
            SetFlagsZSP((byte)(result & 0xFF));
        }

        void Dad(ushort value)
        {
            int result = GetHL() + value;
            CY = result > 0xFFFF;
            SetHL((ushort)result);
        }

        // ── Флаги ────────────────────────────────────────────────────

        byte GetFlags()
        {
            byte f = 0x02; // бит 1 всегда установлен на 8080
            if (S) f |= 0x80;
            if (Z) f |= 0x40;
            if (AC) f |= 0x10;
            if (P) f |= 0x04;
            if (CY) f |= 0x01;
            return f;
        }

        void SetFlags(byte f)
        {
            S = (f & 0x80) != 0;
            Z = (f & 0x40) != 0;
            AC = (f & 0x10) != 0;
            P = (f & 0x04) != 0;
            CY = (f & 0x01) != 0;
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

        // ── Стек ─────────────────────────────────────────────────────

        void Push(ushort value)
        {
            memory.Write(--SP, (byte)(value >> 8));
            memory.Write(--SP, (byte)(value & 0xFF));
        }

        ushort Pop()
        {
            byte low = memory.Read(SP++);
            byte high = memory.Read(SP++);
            return (ushort)((high << 8) | low);
        }

        // ── Регистровые пары ─────────────────────────────────────────

        byte GetReg(int code) => code switch
        {
            0 => B,
            1 => C,
            2 => D,
            3 => E,
            4 => H,
            5 => L,
            6 => memory.Read(GetHL()),
            7 => A,
            _ => 0
        };

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
                case 6: memory.Write(GetHL(), value); break;
                case 7: A = value; break;
            }
        }

        ushort GetBC() => (ushort)((B << 8) | C);
        void SetBC(ushort v) { B = (byte)(v >> 8); C = (byte)(v & 0xFF); }

        ushort GetDE() => (ushort)((D << 8) | E);
        void SetDE(ushort v) { D = (byte)(v >> 8); E = (byte)(v & 0xFF); }

        ushort GetHL() => (ushort)((H << 8) | L);
        void SetHL(ushort v) { H = (byte)(v >> 8); L = (byte)(v & 0xFF); }

        ushort ReadWord()
        {
            byte low = memory.Read(PC++);
            byte high = memory.Read(PC++);
            return (ushort)(low | (high << 8));
        }
    }
}