using System;
using System.Windows.Forms;

namespace Vector06cEmulator
{
    public class IOBus
    {
        private readonly VideoController _video;
        private readonly Keyboard _keyboard;

        public IOBus(VideoController video, Keyboard keyboard)
        {
            _video = video ?? throw new ArgumentNullException(nameof(video));
            _keyboard = keyboard ?? throw new ArgumentNullException(nameof(keyboard));
        }

        public byte In(byte port)
        {
            // В Вектор-06Ц порты:
            // 0x00-0x0F - видеорегистры
            // 0x10-0x1F - клавиатура и системные регистры
            if (port >= 0x00 && port <= 0x0F)
            {
                return _video.InPort(port);
            }
            else if (port >= 0x10 && port <= 0x1F)
            {
                return _keyboard.ReadPort(port);
            }

            return 0xFF; // Неподключенные порты возвращают 0xFF
        }

        public void Out(byte port, byte value)
        {
            Console.WriteLine($"[IOBUS] OUT port=0x{port:X2}, value=0x{value:X2}");

            // Ваш существующий код обработки портов
            if (port == 0x00 || port == 0x01 || port == 0x10)
            {
                _video.OutPort(port, value);
            }
            // ... остальной код
        }
    }
}