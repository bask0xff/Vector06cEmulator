using System;

namespace Vector06cEmulator
{
    public class Emulator
    {
        public Cpu8080 Cpu;
        public Memory Memory;

        public Emulator()
        {
            Memory = new Memory();
            Cpu = new Cpu8080(Memory);
        }

        public void LoadRom(string path)
        {
            var data = File.ReadAllBytes(path);
            Memory.Load(data, 0x0000);
            Cpu.PC = 0x0000;
        }

        public void Run()
        {
            while (!Cpu.Halted)
                Cpu.Step();

            Console.WriteLine("\n═══════════════════════════════");
            Console.WriteLine("        EXECUTION RESULTS ");
            Console.WriteLine("═══════════════════════════════");
            Console.WriteLine("REGISTERS:");
            Console.WriteLine($"A  = {Cpu.A:X2} ({Cpu.A})");
            Console.WriteLine($"B  = {Cpu.B:X2} ({Cpu.B})");
            Console.WriteLine($"C  = {Cpu.C:X2} ({Cpu.C})");
            Console.WriteLine($"D  = {Cpu.D:X2} ({Cpu.D})");
            Console.WriteLine($"H  = {Cpu.H:X2} ({Cpu.H})");
            Console.WriteLine($"L  = {Cpu.L:X2} ({Cpu.L})");
            Console.WriteLine($"SP = {Cpu.SP:X4}");
            Console.WriteLine($"PC = {Cpu.PC:X4}");
            Console.WriteLine("───────────────────────────────");
            Console.WriteLine("FLAGS:");
            Console.WriteLine($"Z={Cpu.Z} S={Cpu.S} CY={Cpu.CY} P={Cpu.P} AC={Cpu.AC}");
            Console.WriteLine("───────────────────────────────");
            Console.WriteLine("MEMORY:");
            Console.WriteLine($"  mem[8000] = {Memory.Read(0x8000):X2} ({Memory.Read(0x8000)})");
            Console.WriteLine($"  mem[8001] = {Memory.Read(0x8001):X2} ({Memory.Read(0x8001)})");
            Console.WriteLine($"  mem[8002] = {Memory.Read(0x8002):X2} ({Memory.Read(0x8002)})");
            Console.WriteLine("═══════════════════════════════");
            
        }
    }
}