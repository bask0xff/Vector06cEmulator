using System;

namespace Vector06cEmulator
{
    public static class Disassembler
    {
        static string logText = "";

        public static void WriteLog(string log)
        {
            logText += log + Environment.NewLine;
        }

        public static string GetLog() => logText;

        public static int Disassemble(byte[] romData, Memory memory, ushort pc)
        {
            byte opcode = memory.Read(pc);

            WriteLog($"{pc:X4}: ");

            switch (opcode)
            {
                case 0x00:
                    WriteLog("NOP");
                    return 1;

                case 0x3E:
                    WriteLog($"MVI A, {memory.Read((ushort)(pc + 1)):X2}h");
                    return 2;

                case 0x06:
                    WriteLog($"MVI B, {memory.Read((ushort)(pc + 1)):X2}h");
                    return 2;

                case 0x0E:
                    WriteLog($"MVI C, {memory.Read((ushort)(pc + 1)):X2}h");
                    return 2;

                case 0x16:
                    WriteLog($"MVI D, {memory.Read((ushort)(pc + 1)):X2}h");
                    return 2;

                case 0x1E:
                    WriteLog($"MVI E, {memory.Read((ushort)(pc + 1)):X2}h");
                    return 2;

                case 0x21:
                    ushort addrHL = (ushort)(
                        memory.Read((ushort)(pc + 1)) |
                        (memory.Read((ushort)(pc + 2)) << 8)
                    );
                    WriteLog($"LXI H, {addrHL:X4}h");
                    return 3;

                case 0x32:
                    ushort addrSTA = (ushort)(
                        memory.Read((ushort)(pc + 1)) |
                        (memory.Read((ushort)(pc + 2)) << 8)
                    );
                    WriteLog($"STA {addrSTA:X4}h");
                    return 3;

                case 0x77:
                    WriteLog("MOV M, A");
                    return 1;

                case 0x3C:
                    WriteLog("INR A");
                    return 1;

                case 0x04:
                    WriteLog("INR B");
                    return 1;

                case 0x05:
                    WriteLog("DCR B");
                    return 1;

                case 0x80:
                    WriteLog("ADD B");
                    return 1;

                case 0x83:
                    WriteLog("ADD E");
                    return 1;

                case 0xFE:
                    WriteLog($"CPI {memory.Read((ushort)(pc + 1)):X2}h");
                    return 2;

                case 0xB8:
                    WriteLog("CMP B");
                    return 1;

                case 0xC2:
                    ushort jnz = (ushort)(
                        memory.Read((ushort)(pc + 1)) |
                        (memory.Read((ushort)(pc + 2)) << 8)
                    );
                    WriteLog($"JNZ {jnz:X4}h");
                    return 3;

                case 0xCA:
                    ushort jz = (ushort)(
                        memory.Read((ushort)(pc + 1)) |
                        (memory.Read((ushort)(pc + 2)) << 8)
                    );
                    WriteLog($"JZ {jz:X4}h");
                    return 3;

                case 0x76:
                    WriteLog("HLT");
                    return 1;

                default:
                    WriteLog($"DB {opcode:X2}h");
                    return 1;
            }
        }
    }
}