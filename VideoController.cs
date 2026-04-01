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

        public void UpdateScreen()
        {
            var bitmapData = bitmap.LockBits(
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

            bitmap.UnlockBits(bitmapData);
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
            Console.WriteLine($"Screenshot: {filename}");
        }
    }
}