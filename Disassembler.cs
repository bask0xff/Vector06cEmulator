using System;

namespace Vector06cEmulator
{
    public static class Disassembler
    {
        public static int Disassemble(Memory memory, ushort pc)
        {
            byte opcode = memory.Read(pc);

            Console.Write($"{pc:X4}: ");

            switch (opcode)
            {
                case 0x00:
                    Console.WriteLine("NOP");
                    return 1;

                case 0x3E:
                    Console.WriteLine($"MVI A, {memory.Read((ushort)(pc + 1)):X2}h");
                    return 2;

                case 0x06:
                    Console.WriteLine($"MVI B, {memory.Read((ushort)(pc + 1)):X2}h");
                    return 2;

                case 0x0E:
                    Console.WriteLine($"MVI C, {memory.Read((ushort)(pc + 1)):X2}h");
                    return 2;

                case 0x16:
                    Console.WriteLine($"MVI D, {memory.Read((ushort)(pc + 1)):X2}h");
                    return 2;

                case 0x1E:
                    Console.WriteLine($"MVI E, {memory.Read((ushort)(pc + 1)):X2}h");
                    return 2;

                case 0x21:
                    ushort addrHL = (ushort)(
                        memory.Read((ushort)(pc + 1)) |
                        (memory.Read((ushort)(pc + 2)) << 8)
                    );
                    Console.WriteLine($"LXI H, {addrHL:X4}h");
                    return 3;

                case 0x32:
                    ushort addrSTA = (ushort)(
                        memory.Read((ushort)(pc + 1)) |
                        (memory.Read((ushort)(pc + 2)) << 8)
                    );
                    Console.WriteLine($"STA {addrSTA:X4}h");
                    return 3;

                case 0x77:
                    Console.WriteLine("MOV M, A");
                    return 1;

                case 0x3C:
                    Console.WriteLine("INR A");
                    return 1;

                case 0x04:
                    Console.WriteLine("INR B");
                    return 1;

                case 0x05:
                    Console.WriteLine("DCR B");
                    return 1;

                case 0x80:
                    Console.WriteLine("ADD B");
                    return 1;

                case 0x83:
                    Console.WriteLine("ADD E");
                    return 1;

                case 0xFE:
                    Console.WriteLine($"CPI {memory.Read((ushort)(pc + 1)):X2}h");
                    return 2;

                case 0xB8:
                    Console.WriteLine("CMP B");
                    return 1;

                case 0xC2:
                    ushort jnz = (ushort)(
                        memory.Read((ushort)(pc + 1)) |
                        (memory.Read((ushort)(pc + 2)) << 8)
                    );
                    Console.WriteLine($"JNZ {jnz:X4}h");
                    return 3;

                case 0xCA:
                    ushort jz = (ushort)(
                        memory.Read((ushort)(pc + 1)) |
                        (memory.Read((ushort)(pc + 2)) << 8)
                    );
                    Console.WriteLine($"JZ {jz:X4}h");
                    return 3;

                case 0x76:
                    Console.WriteLine("HLT");
                    return 1;

                default:
                    Console.WriteLine($"DB {opcode:X2}h");
                    return 1;
            }
        }
    }
}