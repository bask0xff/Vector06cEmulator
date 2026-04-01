using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Vector06cEmulator
{
    public class VideoController
    {
        private const int ScreenWidth = 256;
        private const int ScreenHeight = 256;

        private readonly Memory _memory;

        private readonly byte[] videoRam = new byte[8192];
        private readonly Color[] palette = new Color[16];

        private int currentPaletteIndex = 0;   // цвет "включённых" пикселей (порт 0x01)
        private byte borderColor = 0;          // цвет "выключенных" пикселей (порт 0x00)
        private byte scrollOffset = 0;

        // Для отладки (тестовая кнопка)
        private int forcePaletteIndex = -1;
        private int forceBorderColor = -1;

        private Bitmap bitmap; // теперь всегда 32bppArgb

        public VideoController(Memory memory)
        {
            _memory = memory ?? throw new ArgumentNullException(nameof(memory));
            InitializePalette();
            bitmap = new Bitmap(ScreenWidth, ScreenHeight, PixelFormat.Format32bppArgb);
        }

        private void InitializePalette()
        {
            palette[0] = Color.Black;
            palette[1] = Color.Blue;
            palette[2] = Color.Green;
            palette[3] = Color.Cyan;
            palette[4] = Color.Red;
            palette[5] = Color.Magenta;
            palette[6] = Color.LightYellow;
            palette[7] = Color.White;
            palette[8] = Color.DarkGray;
            palette[9] = Color.DarkBlue;
            palette[10] = Color.DarkGreen;
            palette[11] = Color.DarkCyan;
            palette[12] = Color.DarkRed;
            palette[13] = Color.DarkMagenta;
            palette[14] = Color.Yellow;
            palette[15] = Color.Gray;
        }

        private uint[] BuildPalette32()
        {
            var p = new uint[16];
            for (int i = 0; i < 16; i++)
            {
                Color c = palette[i];
                p[i] = (uint)((0xFF << 24) | (c.R << 16) | (c.G << 8) | c.B);
            }
            return p;
        }

        public void SetBorderColor(byte value)
        {
            borderColor = (byte)(value & 0x0F);   // ← исправлено! не трогаем currentPaletteIndex
        }

        public void SetPaletteColor(byte value)
        {
            currentPaletteIndex = value & 0x0F;
        }

        public void SetScrollOffset(byte value)
        {
            scrollOffset = value;
        }

        // === Отладочные методы ===
        public int GetCurrentPaletteIndex() => currentPaletteIndex;
        public byte GetBorderColor() => borderColor;

        public int CountNonZeroVideoRam()
        {
            int count = 0;
            for (int i = 0; i < videoRam.Length; i++)
                if (videoRam[i] != 0) count++;
            return count;
        }

        public byte PeekVideoRam(int offset)
        {
            if (offset >= 0 && offset < videoRam.Length)
                return videoRam[offset];
            return 0;
        }

        public byte ReadVideoRam(ushort addr)
        {
            if (addr >= 0x1800 && addr <= 0x37FF)
                return _memory.Read(addr);   
            return 0;
        }

        public void WriteVideoRam(ushort addr, byte value)
        {
            if (addr >= 0x1800 && addr <= 0x37FF)
                _memory.Write(addr, value);  // пишем в общую память
        }

        public void ForcePalette(int paletteIdx, int borderIdx)
        {
            forcePaletteIndex = paletteIdx;
            forceBorderColor = borderIdx;
        }

        public void UpdateScreen()
        {
            UpdateScreenInternal(bitmap);
        }

        // Новый прямой метод (будет вызываться из Memory)
        public void DirectWriteVideoRam(ushort addr, byte value)
        {
            // Ничего не делаем дополнительно — данные уже в _memory
            // Можно добавить отладку при необходимости: DebugLog...
        }

        public void UpdateScreenInternal(Bitmap targetBitmap)
        {
            var bitmapData = targetBitmap.LockBits(
                new Rectangle(0, 0, ScreenWidth, ScreenHeight),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            unsafe
            {
                uint* ptr = (uint*)bitmapData.Scan0.ToPointer();
                uint[] palette32 = BuildPalette32();

                int effectivePalette = forcePaletteIndex >= 0 ? forcePaletteIndex : currentPaletteIndex;
                int effectiveBorder = forceBorderColor >= 0 ? forceBorderColor : borderColor;

                for (int y = 0; y < ScreenHeight; y++)
                {
                    int videoLine = (y + scrollOffset) % 256;
                    int lineBase = videoLine * 32;

                    for (int x = 0; x < ScreenWidth; x++)
                    {
                        int byteOffset = lineBase + (x / 8);
                        ushort addr = (ushort)(0x1800 + byteOffset);

                        byte pixelByte = _memory.Read(addr);        // ← теперь из общей памяти!

                        int bitPos = 7 - (x % 8);
                        bool pixelOn = (pixelByte & (1 << bitPos)) != 0;

                        int colorIndex = pixelOn ? effectivePalette : effectiveBorder;
                        uint color = palette32[colorIndex];

                        int pixelIndex = y * (bitmapData.Stride / 4) + x;
                        ptr[pixelIndex] = color;
                    }
                }
            }

            targetBitmap.UnlockBits(bitmapData);
        }

        public Bitmap GetBitmap() => bitmap;

        public void SaveScreenshot(string filename)
        {
            using var rgbBitmap = new Bitmap(ScreenWidth, ScreenHeight, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(rgbBitmap))
                g.DrawImage(bitmap, 0, 0);
            rgbBitmap.Save(filename, ImageFormat.Png);
        }

        public string GetDebugInfo()
        {
            return $"pal={currentPaletteIndex}, border={borderColor}, scroll={scrollOffset}, VRAM_nonzero={CountNonZeroVideoRam()}";
        }
    }
}