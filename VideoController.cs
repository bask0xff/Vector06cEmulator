using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Vector06cEmulator
{
    public class VideoController
    {
        private const int ScreenWidth = 256;
        private const int ScreenHeight = 256;

        private readonly byte[] videoRam = new byte[8192];
        private readonly Color[] palette = new Color[16];

        private int currentPaletteIndex = 0;
        private byte borderColor = 0;
        private byte scrollOffset = 0;

        private Bitmap bitmap;

        public VideoController()
        {
            InitializePalette();
            bitmap = new Bitmap(ScreenWidth, ScreenHeight, PixelFormat.Format8bppIndexed);
            UpdatePalette();
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

        private void UpdatePalette()
        {
            var paletteEntry = bitmap.Palette;
            for (int i = 0; i < 16; i++)
            {
                paletteEntry.Entries[i] = Color.FromArgb(
                    palette[i].R, palette[i].G, palette[i].B);
            }
            bitmap.Palette = paletteEntry;
        }

        public void SetBorderColor(byte value)
        {
            borderColor = value;
            currentPaletteIndex = value & 0x0F;
        }

        public void SetPaletteColor(byte value)
        {
            currentPaletteIndex = value & 0x0F;
        }

        public void SetScrollOffset(byte value)
        {
            scrollOffset = value;
        }

        // ПУБЛИЧНЫЕ МЕТОДЫ ДЛЯ ОТЛАДКИ
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
                return videoRam[addr - 0x1800];
            return 0;
        }

        public void WriteVideoRam(ushort addr, byte value)
        {
            if (addr >= 0x1800 && addr <= 0x37FF)
                videoRam[addr - 0x1800] = value;
        }

        // Обновление экрана для общего использования
        public void UpdateScreen()
        {
            UpdateScreenInternal(bitmap);
        }

        // ПУБЛИЧНЫЙ метод для обновления внешнего Bitmap
        // В VideoController.cs
        public void UpdateScreenInternal(Bitmap targetBitmap)
        {
            var bitmapData = targetBitmap.LockBits(
                new Rectangle(0, 0, ScreenWidth, ScreenHeight),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);  // 32 бита вместо 8!

            unsafe
            {
                uint* ptr = (uint*)bitmapData.Scan0.ToPointer();  // uint* вместо byte*

                // Палитра как массив 32-битных цветов (ARGB)
                uint[] palette32 = new uint[16]
                {
            0xFF000000,  // 0: чёрный
            0xFF0000FF,  // 1: синий
            0xFF00FF00,  // 2: зелёный
            0xFF00FFFF,  // 3: циан
            0xFFFF0000,  // 4: красный
            0xFFFF00FF,  // 5: маджента
            0xFFFFFF00,  // 6: жёлтый
            0xFFFFFFFF,  // 7: белый
            0xFF808080,  // 8: тёмно-серый
            0xFF000080,  // 9: тёмно-синий
            0xFF008000,  // 10: тёмно-зелёный
            0xFF008080,  // 11: тёмно-циан
            0xFF800000,  // 12: тёмно-красный
            0xFF800080,  // 13: тёмно-маджента
            0xFFFFFF00,  // 14: жёлтый (ярко)
            0xFFC0C0C0   // 15: серый
                };

                for (int y = 0; y < ScreenHeight; y++)
                {
                    int videoLine = (y + scrollOffset) % 256;
                    int videoAddr = videoLine * 32;

                    for (int x = 0; x < ScreenWidth; x++)
                    {
                        int byteAddr = videoAddr + (x / 8);
                        int bitPos = 7 - (x % 8);

                        byte pixelByte = (byteAddr < videoRam.Length) ? videoRam[byteAddr] : (byte)0;
                        bool pixelOn = (pixelByte & (1 << bitPos)) != 0;

                        int colorIndex = pixelOn ? currentPaletteIndex : (borderColor & 0x0F);
                        uint color = palette32[colorIndex];

                        int pixelIndex = y * (bitmapData.Stride / 4) + x;  //_stride в uint, а не в byte!
                        ptr[pixelIndex] = color;
                    }
                }
            }

            targetBitmap.UnlockBits(bitmapData);
        }

        public Bitmap GetBitmap() => bitmap;

        public void SaveScreenshot(string filename)
        {
            var rgbBitmap = new Bitmap(ScreenWidth, ScreenHeight, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(rgbBitmap))
            {
                g.DrawImage(bitmap, 0, 0);
            }
            rgbBitmap.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
            Console.WriteLine($"Скриншот: {filename}");
        }

        public string GetDebugInfo()
        {
            return $"pal={currentPaletteIndex}, border={borderColor}, scroll={scrollOffset}, VRAM_nonzero={CountNonZeroVideoRam()}";
        }

        // В VideoController.cs
        /*public void UpdateScreenInternal(Bitmap targetBitmap)  // <-- Должно быть public
        {
            var bitmapData = targetBitmap.LockBits(
                new Rectangle(0, 0, ScreenWidth, ScreenHeight),
                ImageLockMode.WriteOnly,
                PixelFormat.Format8bppIndexed);

            unsafe
            {
                byte* ptr = (byte*)bitmapData.Scan0.ToPointer();

                for (int y = 0; y < ScreenHeight; y++)
                {
                    int videoLine = (y + scrollOffset) % 256;
                    int videoAddr = videoLine * 32;

                    for (int x = 0; x < ScreenWidth; x++)
                    {
                        int byteAddr = videoAddr + (x / 8);
                        int bitPos = 7 - (x % 8);

                        byte pixelByte = (byteAddr < videoRam.Length) ? videoRam[byteAddr] : (byte)0;
                        bool pixelOn = (pixelByte & (1 << bitPos)) != 0;

                        int colorIndex = pixelOn ? currentPaletteIndex : (borderColor & 0x0F);
                        int pixelIndex = y * bitmapData.Stride + x;

                        ptr[pixelIndex] = (byte)colorIndex;
                    }
                }
            }

            targetBitmap.UnlockBits(bitmapData);
        }*/
    }
}