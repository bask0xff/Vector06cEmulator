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
        private const int VideoRamSize = 8192; // 32 байта * 256 строк

        private readonly Memory _memory;
        private readonly Color[] palette = new Color[16];

        private byte borderColor = 0;      // Цвет фона (порт 0x00)
        private byte paletteIndex = 1;      // Цвет пикселей (порт 0x01)
        private byte scrollOffset = 0;      // Скроллинг (порт 0x10)

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
            // Стандартная палитра Вектор-06Ц
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

        // Обработка портов ввода/вывода
        public void OutPort(byte port, byte value)
        {
            switch (port)
            {
                case 0x00: // Цвет рамки/фона
                    borderColor = (byte)(value & 0x0F);
                    screenDirty = true;
                    break;

                case 0x01: // Цвет пикселей (палитра)
                    paletteIndex = (byte)(value & 0x0F);
                    screenDirty = true;
                    break;

                case 0x10: // Скроллинг
                    scrollOffset = value;
                    screenDirty = true;
                    break;

                default:
                    // Другие порты пока игнорируем
                    break;
            }
        }

        public byte InPort(byte port)
        {
            // В реальном Вектор-06Ц здесь читаются состояния
            switch (port)
            {
                case 0x00: // Чтение цвета фона
                    return borderColor;

                case 0x01: // Чтение цвета пикселей
                    return paletteIndex;

                case 0x10: // Чтение скроллинга
                    return scrollOffset;

                default:
                    return 0xFF;
            }
        }

        // Принудительное обновление экрана
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
                uint borderColor32 = (uint)ColorToArgb(palette[borderColor]);
                uint pixelColor32 = (uint)ColorToArgb(palette[paletteIndex]);

                for (int y = 0; y < ScreenHeight; y++)
                {
                    // Применяем скроллинг
                    int videoLine = (y + scrollOffset) % ScreenHeight;
                    int lineBase = videoLine * 32;

                    for (int x = 0; x < ScreenWidth; x++)
                    {
                        int byteOffset = lineBase + (x / 8);
                        //ushort addr = (ushort)(VideoRamStart + byteOffset);
                        ushort addr = (ushort)(0x1800 + videoLine * 32 + (x / 8));

                        byte pixelByte = _memory.Read(addr);
                        int bitPos = 7 - (x % 8);
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
            return (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
        }

        public Bitmap GetBitmap() => bitmap;

        // Для отладки
        public void FillTestPattern()
        {
            // Заполняем видеопамять тестовым паттерном
            for (int y = 0; y < ScreenHeight; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    ushort addr = (ushort)(VideoRamStart + y * 32 + x);
                    byte value = (byte)((y / 8) % 2 == 0 ? 0xFF : 0x00);
                    _memory.Write(addr, value);
                }
            }
            screenDirty = true;
        }

        public void SaveScreenshot(string filename)
        {
            UpdateScreen();
            bitmap.Save(filename, ImageFormat.Png);
        }

        // Методы для обратной совместимости со старым кодом
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

        public void ForcePalette(int paletteIdx, int borderIdx)
        {
            OutPort(0x01, (byte)paletteIdx);
            OutPort(0x00, (byte)borderIdx);
            screenDirty = true;
        }

        public void UpdateScreenInternal(Bitmap targetBitmap)
        {
            UpdateScreen();
            // Копируем внутренний bitmap в targetBitmap
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
    }
}