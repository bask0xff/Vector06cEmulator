namespace Vector06cEmulator
{
    // Видеоконтроллер Вектор-06Ц
    // Вектор-06Ц: 256×256 пикселей, 4 бита на пиксель (16 цветов из палитры)
    // Видеопамять: 0xC000–0xFFFF (4 плоскости по 0x1000 байт каждая)
    // Каждая плоскость отвечает за один бит цвета
    public class VideoController
    {
        public const int ScreenWidth  = 256;
        public const int ScreenHeight = 256;

        // Палитра: 16 цветов, каждый — RGB
        // Вектор-06Ц кодирует цвет как RGB в портах управления
        private readonly int[] palette = new int[16];

        private byte borderColor  = 0;
        private byte scrollOffset = 0;

        // Индекс записываемого цвета палитры
        // OUT 0x01 записывает цвет по текущему индексу, потом индекс растёт
        private int paletteIndex = 0;

        public VideoController()
        {
            InitDefaultPalette();
        }

        // OUT port 0x00 — цвет рамки и сброс индекса палитры
        public void SetBorderColor(byte value)
        {
            borderColor  = value;
            paletteIndex = 0;   // Запись в порт 0x00 сбрасывает счётчик палитры
        }

        // OUT port 0x01 — запись очередного цвета в палитру
        public void SetPaletteColor(byte value)
        {
            // Вектор-06Ц: цвет хранится как 3-bit RGB (по 1 биту на канал)
            // биты: 0=B, 1=G, 2=R, 3=яркость (упрощённо)
            int r = ((value >> 2) & 1) * 0xAA + ((value >> 5) & 1) * 0x55;
            int g = ((value >> 1) & 1) * 0xAA + ((value >> 4) & 1) * 0x55;
            int b = ((value >> 0) & 1) * 0xAA + ((value >> 3) & 1) * 0x55;
            palette[paletteIndex & 0x0F] = (r << 16) | (g << 8) | b;
            paletteIndex++;
        }

        // OUT port 0x10 — прокрутка экрана
        public void SetScrollOffset(byte value)
        {
            scrollOffset = value;
        }

        // Рендерим кадр из видеопамяти в массив ARGB-пикселей
        // Вызывается перед отрисовкой каждого кадра
        public void Render(Memory memory, int[] pixels)
        {
            // Вектор-06Ц: видеопамять начинается с 0xC000
            // 4 плоскости по 256*256/8 = 8192 байт (0x2000)
            // plane0: 0xC000, plane1: 0xE000 — упрощённая модель
            // Реальная адресация сложнее, пока делаем плоскую модель

            const ushort videoBase = 0xC000;

            for (int y = 0; y < ScreenHeight; y++)
            {
                int screenY = (y + scrollOffset) & 0xFF;

                for (int x = 0; x < ScreenWidth; x += 8)
                {
                    int byteIndex = screenY * 32 + x / 8;

                    byte plane0 = memory.Read((ushort)(videoBase + 0x0000 + byteIndex));
                    byte plane1 = memory.Read((ushort)(videoBase + 0x2000 + byteIndex));

                    for (int bit = 7; bit >= 0; bit--)
                    {
                        int colorIndex =
                            (((plane0 >> bit) & 1) << 0) |
                            (((plane1 >> bit) & 1) << 1);

                        int pixelX = x + (7 - bit);
                        int pixelY = y;
                        pixels[pixelY * ScreenWidth + pixelX] = unchecked((int)0xFF000000) | palette[colorIndex];
                    }
                }
            }
        }

        private void InitDefaultPalette()
        {
            // Стандартная палитра Вектор-06Ц (упрощённая)
            palette[0]  = 0x000000; // Чёрный
            palette[1]  = 0x0000AA; // Синий
            palette[2]  = 0x00AA00; // Зелёный
            palette[3]  = 0x00AAAA; // Голубой
            palette[4]  = 0xAA0000; // Красный
            palette[5]  = 0xAA00AA; // Пурпурный
            palette[6]  = 0xAA5500; // Коричневый
            palette[7]  = 0xAAAAAA; // Светло-серый
            palette[8]  = 0x555555; // Тёмно-серый
            palette[9]  = 0x5555FF; // Ярко-синий
            palette[10] = 0x55FF55; // Ярко-зелёный
            palette[11] = 0x55FFFF; // Ярко-голубой
            palette[12] = 0xFF5555; // Ярко-красный
            palette[13] = 0xFF55FF; // Ярко-пурпурный
            palette[14] = 0xFFFF55; // Жёлтый
            palette[15] = 0xFFFFFF; // Белый
        }
    }
}
