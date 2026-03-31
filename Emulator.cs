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
            try
            {
                while (true)
                {
                    Cpu.Step();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Stopped: {ex.Message}");
            }
        }
    }
}