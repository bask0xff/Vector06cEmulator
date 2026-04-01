using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Vector06cEmulator
{
    public class Keyboard
    {
        // Карта клавиш Вектор-06Ц
        private readonly Dictionary<Keys, byte> keyMap = new Dictionary<Keys, byte>
        {
            // Цифры
            { Keys.D0, 0x00 }, { Keys.D1, 0x01 }, { Keys.D2, 0x02 }, { Keys.D3, 0x03 },
            { Keys.D4, 0x04 }, { Keys.D5, 0x05 }, { Keys.D6, 0x06 }, { Keys.D7, 0x07 },
            { Keys.D8, 0x08 }, { Keys.D9, 0x09 },
            
            // Буквы
            { Keys.A, 0x0A }, { Keys.B, 0x0B }, { Keys.C, 0x0C }, { Keys.D, 0x0D },
            { Keys.E, 0x0E }, { Keys.F, 0x0F }, { Keys.G, 0x10 }, { Keys.H, 0x11 },
            { Keys.I, 0x12 }, { Keys.J, 0x13 }, { Keys.K, 0x14 }, { Keys.L, 0x15 },
            { Keys.M, 0x16 }, { Keys.N, 0x17 }, { Keys.O, 0x18 }, { Keys.P, 0x19 },
            { Keys.Q, 0x1A }, { Keys.R, 0x1B }, { Keys.S, 0x1C }, { Keys.T, 0x1D },
            { Keys.U, 0x1E }, { Keys.V, 0x1F }, { Keys.W, 0x20 }, { Keys.X, 0x21 },
            { Keys.Y, 0x22 }, { Keys.Z, 0x23 },
            
            // Специальные клавиши
            { Keys.Enter, 0x24 }, { Keys.Space, 0x25 }, { Keys.Back, 0x26 },
            { Keys.Escape, 0x27 }, { Keys.Tab, 0x28 }
        };

        private byte currentKeyState = 0xFF; // Все клавиши отпущены

        public Keyboard()
        {
            // Здесь можно добавить обработку клавиатуры формы
        }

        // Чтение порта клавиатуры
        public byte ReadPort(byte port)
        {
            // В Вектор-06Ц клавиатура читается через порты 0x10-0x1F
            // Возвращаем состояние клавиш
            return currentKeyState;
        }

        // Запись в порт клавиатуры (обычно не используется)
        public void WritePort(byte port, byte value)
        {
            // Клавиатура Вектор-06Ц обычно только читается
        }

        // Обработка нажатия клавиши
        public void KeyDown(Keys key)
        {
            if (keyMap.ContainsKey(key))
            {
                // Устанавливаем бит для нажатой клавиши
                byte bitMask = (byte)(1 << (keyMap[key] % 8));
                currentKeyState &= (byte)~bitMask;
            }
        }

        // Обработка отпускания клавиши
        public void KeyUp(Keys key)
        {
            if (keyMap.ContainsKey(key))
            {
                // Сбрасываем бит для отпущенной клавиши
                byte bitMask = (byte)(1 << (keyMap[key] % 8));
                currentKeyState |= bitMask;
            }
        }

        // Сброс всех клавиш
        public void Reset()
        {
            currentKeyState = 0xFF;
        }
    }
}