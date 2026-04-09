using System;

namespace Vector06cEmulator
{
    public class Memory
    {
        private byte[] mem = new byte[65536];
        private VideoController _video;   // ← добавь это

        // Новый метод для установки ссылки
        public void SetVideoController(VideoController video)
        {
            _video = video;
        }

        public void Write(ushort addr, byte value)
        {
            if (addr >= 0x1800 && addr <= 0x180F) // Первые 16 байт видеопамяти
            {
                Console.WriteLine($"[MEMORY] WRITE 0x{addr:X4} = 0x{value:X2}");
            }

            mem[addr] = value;
        }

        public byte Read(ushort addr)
        {
            byte value = mem[addr];
            if (addr >= 0x1800 && addr <= 0x180F)
            {
                Console.WriteLine($"[MEMORY] READ 0x{addr:X4} = 0x{value:X2}");
            }
            return value;
        }

        public void Load(byte[] data, ushort start = 0)
        {
            Array.Copy(data, 0, mem, start, data.Length);
        }

        public byte[] GetDump()
        {
            byte[] dump = new byte[mem.Length];
            Array.Copy(mem, dump, mem.Length);
            return dump;
        }
    }
}