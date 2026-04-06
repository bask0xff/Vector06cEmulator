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

        public byte Read(ushort addr)
        {
            return mem[addr];
        }

        public void Write(ushort addr, byte value)
        {
            mem[addr] = value;

            // Зеркалирование в видеопамять
            if (addr >= 0x1800 && addr <= 0x37FF && _video != null)
            {
                //_video.DirectWriteVideoRam(addr, value);   // новый метод, см. ниже
            }
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