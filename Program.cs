using System;

namespace Vector06cEmulator
{
    class Program
    {
        static void Main()
        {
            var emu = new Emulator();

            // Сначала мониторы, потом программа
            emu.LoadMonitors("Monitor0.rom", "MonitorF.rom"); // взято из /Emulator3000/"ЮТ-88"
            //emu.LoadMonitors("Vector06C.rom", "BootTIMSoft.rom"); // из папки /Emulator3000/Vector06C
            emu.LoadRom("binland.rom");
            emu.Start();
            emu.Run(maxSteps: 10_000_000);

            Console.ReadLine();
        }
    }
}
