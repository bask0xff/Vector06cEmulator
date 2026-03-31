namespace Vector06cEmulator
{
    class Program
    {
        static void Main()
        {
            var emu = new Emulator();

            emu.LoadRom("test.bin"); 
            emu.Run();

            Console.ReadLine();
        }
    }
}
