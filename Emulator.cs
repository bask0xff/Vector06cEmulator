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

        // Вектор-06Ц: тактовая частота ~3 МГц, прерывание 50 Гц (PAL)
        // 3_000_000 / 50 = 60_000 тактов между прерываниями
        // Пока у нас нет подсчёта тактов — считаем инструкции (приближение)
        private const int CyclesPerFrame = 60_000;
        private int cycleCounter = 0;

        public Emulator()
        {
            Memory = new Memory();
            Video = new VideoController(Memory);
            Memory.SetVideoController(Video);   // ← важно! связываем их

            Keyboard = new Keyboard();
            IOBus = new IOBus(Video, Keyboard);
            Cpu = new Cpu8080(Memory, IOBus);
        }

        public void LoadMonitors(string monitor0Path, string monitorFPath)
        {
            if (File.Exists(monitor0Path))
            {
                var data = File.ReadAllBytes(monitor0Path);
                Memory.Load(data, 0x0000);
                Console.WriteLine($"Monitor0 loaded: {monitor0Path} ({data.Length} bytes) -> 0x0000");
            }
            else Console.WriteLine($"ATTENTION: {monitor0Path} not found");

            if (File.Exists(monitorFPath))
            {
                var data = File.ReadAllBytes(monitorFPath);
                Memory.Load(data, 0xF800);
                Console.WriteLine($"MonitorF loaded: {monitorFPath} ({data.Length} bytes) -> 0xF800");
            }
            else Console.WriteLine($"ATTENTION: {monitorFPath} not found");
        }

        public void LoadRom(string path, ushort address = 0x0100)
        {
            var data = File.ReadAllBytes(path);
            Memory.Load(data, address);
            Console.WriteLine($"ROM loaded: {path} ({data.Length} bytes) -> 0x{address:X4}");
        }

        public void Start()
        {
            Cpu.PC = 0x0000;
            Cpu.SP = 0xC000;
            cycleCounter = 0;
            Console.WriteLine("CPU started from 0x0000");
        }

        // Один шаг CPU + обработка прерывания если пора
        public void Step()
        {
            Cpu.Step();
            cycleCounter++;
            //Console.WriteLine($"[STEP] counter={cycleCounter}");  // временно
            if (cycleCounter >= CyclesPerFrame)
            {
                cycleCounter = 0;
                Cpu.Interrupt();
            }
        }

        // Запуск на maxSteps инструкций — для отладки
        public void Run(int maxSteps = 2_000_000)
        {
            int steps = 0;
            while (!Cpu.Halted && steps < maxSteps)
            {
                Step();
                steps++;
            }

            if (steps >= maxSteps)
                Console.WriteLine($"\nStopped by limit ({maxSteps} steps), PC={Cpu.PC:X4}");

            PrintState();
        }

        public void PrintState()
        {
            Console.WriteLine("\n═══════════════════════════════");
            Console.WriteLine("        EXECUTION RESULTS      ");
            Console.WriteLine("═══════════════════════════════");
            Console.WriteLine($"  A={Cpu.A:X2}  B={Cpu.B:X2}  C={Cpu.C:X2}");
            Console.WriteLine($"  D={Cpu.D:X2}  E={Cpu.E:X2}");
            Console.WriteLine($"  H={Cpu.H:X2}  L={Cpu.L:X2}");
            Console.WriteLine($"  SP={Cpu.SP:X4}  PC={Cpu.PC:X4}");
            Console.WriteLine($"  Z={Cpu.Z} S={Cpu.S} CY={Cpu.CY} P={Cpu.P} AC={Cpu.AC}");
            Console.WriteLine("═══════════════════════════════");
        }
    }
}