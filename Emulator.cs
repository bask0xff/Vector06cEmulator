using System;
using System.IO;

namespace Vector06cEmulator
{
    public class Emulator
    {
        public readonly Memory          Memory;
        public readonly VideoController Video;
        public readonly Keyboard        Keyboard;
        public readonly IOBus           IOBus;
        public readonly Cpu8080         Cpu;

        public Emulator()
        {
            Memory   = new Memory();
            Video    = new VideoController();
            Keyboard = new Keyboard();
            IOBus    = new IOBus(Video, Keyboard);
            Cpu      = new Cpu8080(Memory, IOBus);
        }

        // Загружаем оба монитора — вызывать ДО LoadRom
        public void LoadMonitors(string monitor0Path, string monitorFPath)
        {
            if (File.Exists(monitor0Path))
            {
                var data = File.ReadAllBytes(monitor0Path);
                Memory.Load(data, 0x0000);
                Console.WriteLine($"Monitor0 загружен: {monitor0Path} ({data.Length} байт) -> 0x0000");
            }
            else
            {
                Console.WriteLine($"ВНИМАНИЕ: {monitor0Path} не найден");
            }

            if (File.Exists(monitorFPath))
            {
                var data = File.ReadAllBytes(monitorFPath);
                Memory.Load(data, 0xF800);
                Console.WriteLine($"MonitorF загружен: {monitorFPath} ({data.Length} байт) -> 0xF800");
            }
            else
            {
                Console.WriteLine($"ВНИМАНИЕ: {monitorFPath} не найден");
            }
        }

        // Загружаем ROM программы — после LoadMonitors
        // Вектор-06Ц загружает программы в RAM начиная с 0x0100
        public void LoadRom(string path, ushort address = 0x0100)
        {
            var data = File.ReadAllBytes(path);
            Memory.Load(data, address);
            Console.WriteLine($"ROM загружен: {path} ({data.Length} байт) -> 0x{address:X4}");
        }

        // Старт с адреса монитора — нормальный старт Вектор-06Ц
        public void Start()
        {
            Cpu.PC = 0x0000;
            Cpu.SP = 0xC000;  // Типичное начальное значение стека
            Console.WriteLine("CPU запущен с 0x0000");
        }

        // Выполнить N шагов
        public void RunCycles(int cycles)
        {
            for (int i = 0; i < cycles && !Cpu.Halted; i++)
                Cpu.Step();
        }

        // Полный запуск до HLT — для отладки
        public void Run(int maxSteps = 500_000)
        {
            int steps = 0;
            while (!Cpu.Halted && steps < maxSteps)
            {
                Cpu.Step();
                steps++;
            }

            if (steps >= maxSteps)
                Console.WriteLine($"\nОстановлен по лимиту шагов ({maxSteps}), PC={Cpu.PC:X4}");

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
            Console.WriteLine("═══════════════════════════════");
        }
    }
}
