namespace Vector06cEmulator
{
    // Шина ввода-вывода Вектор-06Ц
    // Маршрутизирует IN/OUT команды процессора к нужным устройствам
    public class IOBus
    {
        private readonly VideoController video;
        private readonly Keyboard keyboard;

        public IOBus(VideoController video, Keyboard keyboard)
        {
            this.video = video;
            this.keyboard = keyboard;
        }

        public byte In(byte port)
        {
            switch (port)
            {
                case 0x02: return keyboard.ReadColumn();
                case 0x07: return 0xFF;
                case 0x12: return 0x00;
                case 0xA0: return keyboard.ReadPortA();
                case 0xA1: return keyboard.ReadPortB();
                case 0xA2: return keyboard.ReadPortC();
                default: return 0xFF;  // тихая заглушка
            }
        }

        public void Out(byte port, byte value)
        {
            switch (port)
            {
                case 0x00: video.SetBorderColor(value); break;   // Цвет рамки
                case 0x01: video.SetPaletteColor(value); break;  // Цвет палитры
                case 0x02: keyboard.SelectRow(value); break;     // Клавиатура: выбираем строку матрицы
                case 0x03: /* звук — позже */ break;
                case 0x08: /* сброс — позже */ break;
                case 0x0C: /* управление памятью — позже */ break;
                case 0x0B: /* управление памятью — позже */ break;
                case 0x10: video.SetScrollOffset(value); break;  // Прокрутка экрана
                case 0x12: /* таймер — позже */ break;
                case 0x21: /* расширенный регистр — позже */ break;
                case 0x3A: /* расширенный регистр — позже */ break;
                default:
                    Console.WriteLine($"[IOBus] OUT {port:X2} = {value:X2} (не реализован)");
                    break;
            }
        }
    }
}
