using System;
using System.Diagnostics;

namespace Vector06cEmulator
{
    public static class Disassembler
    {
        private static string logText = "";

        public static void WriteLog(string log)
        {
            logText += log + Environment.NewLine;
        }

        public static string GetLog() => logText;

        public static void ClearLog() => logText = "";

        /// <summary>
        /// Дизассемблирует одну инструкцию по адресу pc.
        /// Возвращает количество байт инструкции.
        /// </summary>
        public static int Disassemble(byte[] romData, Memory memory, ushort pc)
        {
            byte op = memory.Read(pc);
            byte b1 = memory.Read((ushort)(pc + 1));
            byte b2 = memory.Read((ushort)(pc + 2));
            ushort imm16 = (ushort)(b1 | (b2 << 8));

            // Префикс: адрес + байты
            string bytes1 = $"{op:X2}";
            string mnem;
            int len;

            // MOV r1, r2  (0x40–0x7F, кроме 0x76 HLT)
            if (op != 0x76 && (op & 0xC0) == 0x40)
            {
                int dst = (op >> 3) & 7;
                int src = op & 7;
                mnem = $"MOV {RegName(dst)}, {RegName(src)}";
                len = 1;
            }
            // ADD r  (0x80–0x87)
            else if ((op & 0xF8) == 0x80)
            {
                mnem = $"ADD {RegName(op & 7)}";
                len = 1;
            }
            // ADC r  (0x88–0x8F)
            else if ((op & 0xF8) == 0x88)
            {
                mnem = $"ADC {RegName(op & 7)}";
                len = 1;
            }
            // SUB r  (0x90–0x97)
            else if ((op & 0xF8) == 0x90)
            {
                mnem = $"SUB {RegName(op & 7)}";
                len = 1;
            }
            // SBB r  (0x98–0x9F)
            else if ((op & 0xF8) == 0x98)
            {
                mnem = $"SBB {RegName(op & 7)}";
                len = 1;
            }
            // ANA r  (0xA0–0xA7)
            else if ((op & 0xF8) == 0xA0)
            {
                mnem = $"ANA {RegName(op & 7)}";
                len = 1;
            }
            // XRA r  (0xA8–0xAF)
            else if ((op & 0xF8) == 0xA8)
            {
                mnem = $"XRA {RegName(op & 7)}";
                len = 1;
            }
            // ORA r  (0xB0–0xB7)
            else if ((op & 0xF8) == 0xB0)
            {
                mnem = $"ORA {RegName(op & 7)}";
                len = 1;
            }
            // CMP r  (0xB8–0xBF)
            else if ((op & 0xF8) == 0xB8)
            {
                mnem = $"CMP {RegName(op & 7)}";
                len = 1;
            }
            else
            {
                switch (op)
                {
                    // ── NOP / HLT ────────────────────────────────────
                    case 0x00: mnem = "NOP"; len = 1; break;
                    case 0x76: mnem = "HLT"; len = 1; break;

                    // ── MVI r, byte ──────────────────────────────────
                    case 0x3E: mnem = $"MVI A, {b1:X2}h"; len = 2; break;
                    case 0x06: mnem = $"MVI B, {b1:X2}h"; len = 2; break;
                    case 0x0E: mnem = $"MVI C, {b1:X2}h"; len = 2; break;
                    case 0x16: mnem = $"MVI D, {b1:X2}h"; len = 2; break;
                    case 0x1E: mnem = $"MVI E, {b1:X2}h"; len = 2; break;
                    case 0x26: mnem = $"MVI H, {b1:X2}h"; len = 2; break;
                    case 0x2E: mnem = $"MVI L, {b1:X2}h"; len = 2; break;
                    case 0x36: mnem = $"MVI M, {b1:X2}h"; len = 2; break;

                    // ── LXI rp, d16 ──────────────────────────────────
                    case 0x01: mnem = $"LXI B, {imm16:X4}h"; len = 3; break;
                    case 0x11: mnem = $"LXI D, {imm16:X4}h"; len = 3; break;
                    case 0x21: mnem = $"LXI H, {imm16:X4}h"; len = 3; break;
                    case 0x31: mnem = $"LXI SP, {imm16:X4}h"; len = 3; break;

                    // ── LDA / STA / LHLD / SHLD ──────────────────────
                    case 0x3A: mnem = $"LDA {imm16:X4}h"; len = 3; break;
                    case 0x32: mnem = $"STA {imm16:X4}h"; len = 3; break;
                    case 0x2A: mnem = $"LHLD {imm16:X4}h"; len = 3; break;
                    case 0x22: mnem = $"SHLD {imm16:X4}h"; len = 3; break;

                    // ── LDAX / STAX ──────────────────────────────────
                    case 0x0A: mnem = "LDAX B"; len = 1; break;
                    case 0x1A: mnem = "LDAX D"; len = 1; break;
                    case 0x02: mnem = "STAX B"; len = 1; break;
                    case 0x12: mnem = "STAX D"; len = 1; break;

                    // ── INX ──────────────────────────────────────────
                    case 0x03: mnem = "INX B"; len = 1; break;
                    case 0x13: mnem = "INX D"; len = 1; break;
                    case 0x23: mnem = "INX H"; len = 1; break;
                    case 0x33: mnem = "INX SP"; len = 1; break;

                    // ── DCX ──────────────────────────────────────────
                    case 0x0B: mnem = "DCX B"; len = 1; break;
                    case 0x1B: mnem = "DCX D"; len = 1; break;
                    case 0x2B: mnem = "DCX H"; len = 1; break;
                    case 0x3B: mnem = "DCX SP"; len = 1; break;

                    // ── INR r ────────────────────────────────────────
                    case 0x04: mnem = "INR B"; len = 1; break;
                    case 0x0C: mnem = "INR C"; len = 1; break;
                    case 0x14: mnem = "INR D"; len = 1; break;
                    case 0x1C: mnem = "INR E"; len = 1; break;
                    case 0x24: mnem = "INR H"; len = 1; break;
                    case 0x2C: mnem = "INR L"; len = 1; break;
                    case 0x34: mnem = "INR M"; len = 1; break;
                    case 0x3C: mnem = "INR A"; len = 1; break;

                    // ── DCR r ────────────────────────────────────────
                    case 0x05: mnem = "DCR B"; len = 1; break;
                    case 0x0D: mnem = "DCR C"; len = 1; break;
                    case 0x15: mnem = "DCR D"; len = 1; break;
                    case 0x1D: mnem = "DCR E"; len = 1; break;
                    case 0x25: mnem = "DCR H"; len = 1; break;
                    case 0x2D: mnem = "DCR L"; len = 1; break;
                    case 0x35: mnem = "DCR M"; len = 1; break;
                    case 0x3D: mnem = "DCR A"; len = 1; break;

                    // ── Immediate arithmetic ──────────────────────────
                    case 0xC6: mnem = $"ADI {b1:X2}h"; len = 2; break;
                    case 0xCE: mnem = $"ACI {b1:X2}h"; len = 2; break;
                    case 0xD6: mnem = $"SUI {b1:X2}h"; len = 2; break;
                    case 0xDE: mnem = $"SBI {b1:X2}h"; len = 2; break;
                    case 0xE6: mnem = $"ANI {b1:X2}h"; len = 2; break;
                    case 0xEE: mnem = $"XRI {b1:X2}h"; len = 2; break;
                    case 0xF6: mnem = $"ORI {b1:X2}h"; len = 2; break;
                    case 0xFE: mnem = $"CPI {b1:X2}h"; len = 2; break;

                    // ── DAD ──────────────────────────────────────────
                    case 0x09: mnem = "DAD B"; len = 1; break;
                    case 0x19: mnem = "DAD D"; len = 1; break;
                    case 0x29: mnem = "DAD H"; len = 1; break;
                    case 0x39: mnem = "DAD SP"; len = 1; break;

                    // ── Rotate ───────────────────────────────────────
                    case 0x07: mnem = "RLC"; len = 1; break;
                    case 0x0F: mnem = "RRC"; len = 1; break;
                    case 0x17: mnem = "RAL"; len = 1; break;
                    case 0x1F: mnem = "RAR"; len = 1; break;

                    // ── DAA / CMA / STC / CMC ─────────────────────────
                    case 0x27: mnem = "DAA"; len = 1; break;
                    case 0x2F: mnem = "CMA"; len = 1; break;
                    case 0x37: mnem = "STC"; len = 1; break;
                    case 0x3F: mnem = "CMC"; len = 1; break;

                    // ── JMP и условные ───────────────────────────────
                    case 0xC3: mnem = $"JMP {imm16:X4}h"; len = 3; break;
                    case 0xC2: mnem = $"JNZ {imm16:X4}h"; len = 3; break;
                    case 0xCA: mnem = $"JZ  {imm16:X4}h"; len = 3; break;
                    case 0xD2: mnem = $"JNC {imm16:X4}h"; len = 3; break;
                    case 0xDA: mnem = $"JC  {imm16:X4}h"; len = 3; break;
                    case 0xE2: mnem = $"JPO {imm16:X4}h"; len = 3; break;
                    case 0xEA: mnem = $"JPE {imm16:X4}h"; len = 3; break;
                    case 0xF2: mnem = $"JP  {imm16:X4}h"; len = 3; break;
                    case 0xFA: mnem = $"JM  {imm16:X4}h"; len = 3; break;

                    // ── CALL и условные ──────────────────────────────
                    case 0xCD: mnem = $"CALL {imm16:X4}h"; len = 3; break;
                    case 0xC4: mnem = $"CNZ  {imm16:X4}h"; len = 3; break;
                    case 0xCC: mnem = $"CZ   {imm16:X4}h"; len = 3; break;
                    case 0xD4: mnem = $"CNC  {imm16:X4}h"; len = 3; break;
                    case 0xDC: mnem = $"CC   {imm16:X4}h"; len = 3; break;
                    case 0xE4: mnem = $"CPO  {imm16:X4}h"; len = 3; break;
                    case 0xEC: mnem = $"CPE  {imm16:X4}h"; len = 3; break;
                    case 0xF4: mnem = $"CP   {imm16:X4}h"; len = 3; break;
                    case 0xFC: mnem = $"CM   {imm16:X4}h"; len = 3; break;

                    // ── RET и условные ───────────────────────────────
                    case 0xC9: mnem = "RET"; len = 1; break;
                    case 0xC0: mnem = "RNZ"; len = 1; break;
                    case 0xC8: mnem = "RZ"; len = 1; break;
                    case 0xD0: mnem = "RNC"; len = 1; break;
                    case 0xD8: mnem = "RC"; len = 1; break;
                    case 0xE0: mnem = "RPO"; len = 1; break;
                    case 0xE8: mnem = "RPE"; len = 1; break;
                    case 0xF0: mnem = "RP"; len = 1; break;
                    case 0xF8: mnem = "RM"; len = 1; break;

                    // ── PUSH / POP ───────────────────────────────────
                    case 0xC5: mnem = "PUSH B"; len = 1; break;
                    case 0xD5: mnem = "PUSH D"; len = 1; break;
                    case 0xE5: mnem = "PUSH H"; len = 1; break;
                    case 0xF5: mnem = "PUSH PSW"; len = 1; break;
                    case 0xC1: mnem = "POP B"; len = 1; break;
                    case 0xD1: mnem = "POP D"; len = 1; break;
                    case 0xE1: mnem = "POP H"; len = 1; break;
                    case 0xF1: mnem = "POP PSW"; len = 1; break;

                    // ── RST ──────────────────────────────────────────
                    case 0xC7: mnem = "RST 0"; len = 1; break;
                    case 0xCF: mnem = "RST 1"; len = 1; break;
                    case 0xD7: mnem = "RST 2"; len = 1; break;
                    case 0xDF: mnem = "RST 3"; len = 1; break;
                    case 0xE7: mnem = "RST 4"; len = 1; break;
                    case 0xEF: mnem = "RST 5"; len = 1; break;
                    case 0xF7: mnem = "RST 6"; len = 1; break;
                    case 0xFF: mnem = "RST 7"; len = 1; break;

                    // ── XCHG / XTHL / SPHL / PCHL ────────────────────
                    case 0xEB: mnem = "XCHG"; len = 1; break;
                    case 0xE3: mnem = "XTHL"; len = 1; break;
                    case 0xF9: mnem = "SPHL"; len = 1; break;
                    case 0xE9: mnem = "PCHL"; len = 1; break;

                    // ── IN / OUT ─────────────────────────────────────
                    case 0xDB: mnem = $"IN  {b1:X2}h"; len = 2; break;
                    case 0xD3: mnem = $"OUT {b1:X2}h"; len = 2; break;

                    // ── EI / DI ──────────────────────────────────────
                    case 0xFB: mnem = "EI"; len = 1; break;
                    case 0xF3: mnem = "DI"; len = 1; break;

                    // ── 8085 (игнорируем как NOP) ─────────────────────
                    case 0x20: mnem = "RIM (NOP)"; len = 1; break;
                    case 0x30: mnem = "SIM (NOP)"; len = 1; break;

                    default:
                        mnem = $"DB {op:X2}h  ; ???";
                        len = 1;
                        break;
                }
            }

            // Формируем строку байтов для отображения
            string bytesStr = op.ToString("X2");
            if (len >= 2) bytesStr += $" {b1:X2}";
            if (len >= 3) bytesStr += $" {b2:X2}";

            if(false)
                Debug.WriteLine($"{pc:X4}:  {bytesStr,-8}  {mnem}");
            //WriteLog($"{pc:X4}:  {bytesStr,-8}  {mnem}");
            return len;
        }

        /// <summary>
        /// Дизассемблирует диапазон адресов [from, to).
        /// </summary>
        public static void DisassembleRange(Memory memory, ushort from, ushort to)
        {
            ushort pc = from;
            while (pc < to)
            {
                int len = Disassemble(null, memory, pc);
                pc = (ushort)(pc + len);
            }
        }

        /// <summary>
        /// Дизассемблирует count инструкций начиная с адреса from.
        /// </summary>
        public static void DisassembleCount(Memory memory, ushort from, int count)
        {
            ushort pc = from;
            for (int i = 0; i < count; i++)
            {
                int len = Disassemble(null, memory, pc);
                pc = (ushort)(pc + len);
            }
        }

        // Имена регистров по коду 0-7
        private static string RegName(int code) => code switch
        {
            0 => "B",
            1 => "C",
            2 => "D",
            3 => "E",
            4 => "H",
            5 => "L",
            6 => "M",
            7 => "A",
            _ => "?"
        };
    }
}