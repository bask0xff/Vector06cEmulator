namespace Vector06cEmulator
{
    class Program
    {
        static void Main()
        {
            var emu = new Emulator();

            emu.LoadRom("factorial2.bin"); 
            emu.Run();

            Console.ReadLine();
        }
    }
}
