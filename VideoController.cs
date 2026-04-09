using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Vector06cEmulator
{
    public class VideoController
    {
        private const int ScreenWidth = 256;
        private const int ScreenHeight = 256;
        private const int VideoRamStart = 0x1800;
        private const int VideoRamEnd = 0x37FF;
        private const int VideoRamSize = 8192;

        private readonly Memory _memory;
        private readonly Color[] palette = new Color[16];

        private byte borderColor = 0;
        private byte paletteIndex = 1;
        private byte scrollOffset = 0;

        private Bitmap bitmap;
        private bool screenDirty = true;

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
            palette[6] = Color.Yellow;
            palette[7] = Color.White;
            palette[8] = Color.DarkGray;
            palette[9] = Color.DarkBlue;
            palette[10] = Color.DarkGreen;
            palette[11] = Color.DarkCyan;
            palette[12] = Color.DarkRed;
            palette[13] = Color.DarkMagenta;
            palette[14] = Color.Gold;
            palette[15] = Color.LightGray;
        }

        public void OutPort(byte port, byte value)
        {
            switch (port)
            {
                case 0x00:
                    borderColor = (byte)(value & 0x0F);
                    screenDirty = true;
                    break;
                case 0x01:
                    paletteIndex = (byte)(value & 0x0F);
                    screenDirty = true;
                    break;
                case 0x10:
                    scrollOffset = value;
                    screenDirty = true;
                    break;
            }
        }

        public byte InPort(byte port)
        {
            switch (port)
            {
                case 0x00: return borderColor;
                case 0x01: return paletteIndex;
                case 0x10: return scrollOffset;
                default: return 0xFF;
            }
        }

        // Временный метод для отладки - использует яркие цвета для проверки
        public void UpdateScreenDebug()
        {
            UpdateScreen();
            Console.WriteLine("\n=== UpdateScreenDebug CALLED ===");

            // Создаём новый bitmap для теста
            var testBitmap = new Bitmap(ScreenWidth, ScreenHeight, PixelFormat.Format32bppArgb);

            var bitmapData = testBitmap.LockBits(
                new Rectangle(0, 0, ScreenWidth, ScreenHeight),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            unsafe
            {
                uint* ptr = (uint*)bitmapData.Scan0.ToPointer();

                // Используем простые цвета для теста (НЕ из палитры)
                uint borderColor32 = 0xFFFF0000; // КРАСНЫЙ (Alpha=FF, Red=FF, Green=00, Blue=00)
                uint pixelColor32 = 0xFF00FF00;   // ЗЕЛЁНЫЙ (Alpha=FF, Red=00, Green=FF, Blue=00)

                Console.WriteLine($"Using border color: RED (0x{borderColor32:X8})");
                Console.WriteLine($"Using pixel color: GREEN (0x{pixelColor32:X8})");

                // Читаем первый байт видеопамяти
                byte vram0 = _memory.Read(0x1800);
                Console.WriteLine($"VRAM[0x1800] = 0x{vram0:X2}");

                int pixelsOn = 0;

                // Рисуем ВСЕ строки для теста
                for (int y = 0; y < ScreenHeight; y++)
                {
                    int videoLine = (y + scrollOffset) % ScreenHeight;

                    for (int x = 0; x < ScreenWidth; x++)
                    {
                        int byteOffset = x / 8;
                        int bitPos = 7 - (x % 8);
                        ushort addr = (ushort)(VideoRamStart + videoLine * 32 + byteOffset);
                        byte pixelByte = _memory.Read(addr);
                        bool pixelOn = (pixelByte & (1 << bitPos)) != 0;

                        if (pixelOn) pixelsOn++;

                        uint color = pixelOn ? pixelColor32 : borderColor32;
                        ptr[y * ScreenWidth + x] = color;
                    }
                }

                Console.WriteLine($"Pixels ON: {pixelsOn} out of {ScreenWidth * ScreenHeight}");
                Console.WriteLine("Screen filled: RED background, GREEN pixels where VRAM=1");
            }

            testBitmap.UnlockBits(bitmapData);

            // Заменяем текущий bitmap
            var oldBitmap = bitmap;
            bitmap = testBitmap;
            if (oldBitmap != null) oldBitmap.Dispose();

            screenDirty = false;
            Console.WriteLine("=== UpdateScreenDebug FINISHED ===\n");
        }

        public void UpdateScreen()
        {
            if (!screenDirty) return;

            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, ScreenWidth, ScreenHeight),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            unsafe
            {
                uint* ptr = (uint*)bitmapData.Scan0.ToPointer();

                // ПРАВИЛЬНОЕ преобразование цветов палитры в ARGB
                uint GetColorFromPalette(int index)
                {
                    Color c = palette[index & 0x0F];
                    return (uint)((c.A << 24) | (c.R << 16) | (c.G << 8) | c.B);
                }

                uint borderColor32 = GetColorFromPalette(borderColor);
                uint pixelColor32 = GetColorFromPalette(paletteIndex);

                for (int y = 0; y < ScreenHeight; y++)
                {
                    int videoLine = (y + scrollOffset) % ScreenHeight;

                    for (int x = 0; x < ScreenWidth; x++)
                    {
                        int byteOffset = x / 8;
                        int bitPos = 7 - (x % 8);
                        ushort addr = (ushort)(VideoRamStart + videoLine * 32 + byteOffset);
                        byte pixelByte = _memory.Read(addr);
                        bool pixelOn = (pixelByte & (1 << bitPos)) != 0;

                        uint color = pixelOn ? pixelColor32 : borderColor32;
                        ptr[y * ScreenWidth + x] = color;
                    }
                }
            }

            bitmap.UnlockBits(bitmapData);
            screenDirty = false;
        }

        private int ColorToArgb(Color color)
        {
            // Убедитесь, что альфа-канал = 255 (полностью непрозрачный)
            return (255 << 24) | (color.R << 16) | (color.G << 8) | color.B;
        }

        public Bitmap GetBitmap() => bitmap;

        // === ДОБАВЛЕННЫЕ МЕТОДЫ ДЛЯ СОВМЕСТИМОСТИ ===

        public void SetBorderColor(byte value)
        {
            OutPort(0x00, value);
        }

        public void SetPaletteColor(byte value)
        {
            OutPort(0x01, value);
        }

        public void SetScrollOffset(byte value)
        {
            OutPort(0x10, value);
        }

        public void SaveScreenshot(string filename)
        {
            UpdateScreen();
            bitmap.Save(filename, ImageFormat.Png);
        }

        public void ForcePalette(int paletteIdx, int borderIdx)
        {
            OutPort(0x01, (byte)paletteIdx);
            OutPort(0x00, (byte)borderIdx);
            screenDirty = true;
        }

        public void UpdateScreenInternal(Bitmap targetBitmap)
        {
            UpdateScreen();
            using (var g = Graphics.FromImage(targetBitmap))
            {
                g.DrawImage(bitmap, 0, 0);
            }
        }

        public int GetCurrentPaletteIndex() => paletteIndex;

        public int CountNonZeroVideoRam()
        {
            int count = 0;
            for (int i = VideoRamStart; i <= VideoRamEnd; i++)
            {
                if (_memory.Read((ushort)i) != 0)
                    count++;
            }
            return count;
        }

        public byte PeekVideoRam(int offset)
        {
            if (offset >= 0 && offset < VideoRamSize)
                return _memory.Read((ushort)(VideoRamStart + offset));
            return 0;
        }

        public void WriteVideoRam(ushort addr, byte value)
        {
            if (addr >= VideoRamStart && addr <= VideoRamEnd)
                _memory.Write(addr, value);
        }

        public void DumpVideoRam(int startLine = 0, int lines = 10)
        {
            Console.WriteLine($"=== VIDEO RAM DUMP (lines {startLine}-{startLine + lines}) ===");
            for (int line = startLine; line < startLine + lines && line < 256; line++)
            {
                int baseAddr = VideoRamStart + line * 32;
                Console.Write($"Line {line:3} (0x{baseAddr:X4}): ");
                for (int x = 0; x < 32; x++)
                {
                    byte val = _memory.Read((ushort)(baseAddr + x));
                    Console.Write($"{val:X2} ");
                }
                Console.WriteLine();
            }
        }

        public void CheckPixel(int x, int y)
        {
            if (x < 0 || x >= 256 || y < 0 || y >= 256)
            {
                Console.WriteLine($"Pixel ({x},{y}) out of range");
                return;
            }

            int lineBase = VideoRamStart + y * 32;
            int byteOffset = x / 8;
            int bitPos = 7 - (x % 8);
            ushort addr = (ushort)(lineBase + byteOffset);
            byte pixelByte = _memory.Read(addr);
            bool pixelOn = (pixelByte & (1 << bitPos)) != 0;

            Console.WriteLine($"Pixel ({x},{y}): addr=0x{addr:X4}, byte=0x{pixelByte:X2}, bit={bitPos}, on={pixelOn}");
        }
    }
}