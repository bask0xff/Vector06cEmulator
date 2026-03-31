using System;
using System.IO;

namespace Vector06cEmulator
{
    public class Emulator
    {
        public readonly Memory Memory;
        public readonly VideoController Video;
        public readonly Keyboard Keyboard;
        public readonly IOBus IOBus;
        public readonly Cpu8080 Cpu;

        public Emulator()
        {
            Memory = new Memory();
            Video = new VideoController();
            Keyboard = new Keyboard();
            IOBus = new IOBus(Video, Keyboard);
            Cpu = new Cpu8080(Memory, IOBus);   // <-- передаём IOBus
        }

        public void LoadRom(string path)
        {
            var data = File.ReadAllBytes(path);
            Memory.Load(data, 0x0000);
            Cpu.PC = 0x0000;
            Console.WriteLine($"ROM загружен: {path} ({data.Length} байт)");
        }

        // Выполнить один шаг CPU — удобно для пошаговой отладки из UI
        public void Step()
        {
            Cpu.Step();
        }

        // Выполнить N тактов — удобно для таймера в WinForms
        public void RunCycles(int cycles)
        {
            for (int i = 0; i < cycles && !Cpu.Halted; i++)
                Cpu.Step();
        }

        // Полный запуск до HLT — для консольного режима
        public void Run()
        {
            while (!Cpu.Halted)
                Cpu.Step();

            PrintState();
        }

        public void PrintState()
        {
            Console.WriteLine("\n═══════════════════════════════");
            Console.WriteLine("        EXECUTION RESULTS      ");
            Console.WriteLine("═══════════════════════════════");
            Console.WriteLine("REGISTERS:");
            Console.WriteLine($"  A  = {Cpu.A:X2} ({Cpu.A})");
            Console.WriteLine($"  B  = {Cpu.B:X2}  C  = {Cpu.C:X2}");
            Console.WriteLine($"  D  = {Cpu.D:X2}  E  = {Cpu.E:X2}");
            Console.WriteLine($"  H  = {Cpu.H:X2}  L  = {Cpu.L:X2}");
            Console.WriteLine($"  SP = {Cpu.SP:X4}");
            Console.WriteLine($"  PC = {Cpu.PC:X4}");
            Console.WriteLine("───────────────────────────────");
            Console.WriteLine("FLAGS:");
            Console.WriteLine($"  Z={Cpu.Z}  S={Cpu.S}  CY={Cpu.CY}  P={Cpu.P}  AC={Cpu.AC}");
            Console.WriteLine("───────────────────────────────");
            Console.WriteLine("MEMORY [0x8000..0x8002]:");
            Console.WriteLine($"  {Memory.Read(0x8000):X2} {Memory.Read(0x8001):X2} {Memory.Read(0x8002):X2}");
            Console.WriteLine("═══════════════════════════════");
        }
    }
}
