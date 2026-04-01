using System;

namespace Vector06cEmulator
{
    public class Memory
    {
        private byte[] mem = new byte[65536];
        private VideoController videoController;  // <-- Добавьте поле

        public void SetVideoController(VideoController video)
        {
            videoController = video;
        }

        public byte Read(ushort addr)
        {
            // Перехватываем чтение из видеопамяти
            if (videoController != null && addr >= 0x1800 && addr <= 0x37FF)
            {
                return videoController.ReadVideoRam(addr);
            }
            return mem[addr];
        }

        public void Write(ushort addr, byte value)
        {
            // Перехватываем запись в видеопамять
            if (videoController != null && addr >= 0x1800 && addr <= 0x37FF)
            {
                videoController.WriteVideoRam(addr, value);
                return;  // Не сохраняем в mem, т.к. уже в videoRam
            }
            mem[addr] = value;
        }

        public void Load(byte[] data, ushort start = 0)
        {
            Array.Copy(data, 0, mem, start, data.Length);
        }
    }
}