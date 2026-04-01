using System;

namespace Vector06cEmulator
{
    class Program
    {
        static void Main()
        {
            var emu = new Emulator();

            // Сначала мониторы, потом программа
            emu.LoadMonitors("Monitor0.rom", "MonitorF.rom");
            emu.LoadRom("binland.rom");
            emu.Start();
            emu.Run(maxSteps: 10_000_000);

            Console.ReadLine();
        }
    }
}
