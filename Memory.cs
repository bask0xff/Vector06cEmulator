namespace Vector06cEmulator
{
    public class Memory
    {
        private byte[] mem = new byte[65536];

        public byte Read(ushort addr)
        {
            return mem[addr];
        }

        public void Write(ushort addr, byte value)
        {
            mem[addr] = value;
        }

        public void Load(byte[] data, ushort start = 0)
        {
            Array.Copy(data, 0, mem, start, data.Length);
        }
    }
}