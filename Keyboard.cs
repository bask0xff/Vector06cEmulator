namespace Vector06cEmulator
{
    // Клавиатура Вектор-06Ц — матрица 8 строк × 8 столбцов
    // OUT port 0x02 — выбор строки (номер строки в битах)
    // IN  port 0x02 — чтение столбцов (0 = нажата, 1 = отпущена)
    public class Keyboard
    {
        // matrix[row] — битовая маска нажатых клавиш в строке
        // бит = 0 означает "клавиша нажата" (активный низкий уровень)
        private readonly byte[] matrix = new byte[8];

        private int selectedRow = 0;

        public Keyboard()
        {
            // Все клавиши отпущены — все биты = 1
            for (int i = 0; i < 8; i++)
                matrix[i] = 0xFF;
        }

        // CPU пишет в порт 0x02 — выбирает строку матрицы
        public void SelectRow(byte value)
        {
            // Вектор-06Ц: строка задаётся номером (0–7) в младших битах
            selectedRow = value & 0x07;
        }

        // CPU читает из порта 0x02 — получает состояние столбцов выбранной строки
        public byte ReadColumn()
        {
            return matrix[selectedRow];
        }

        // Нажать клавишу (row, col) — вызывается из UI
        public void KeyDown(int row, int col)
        {
            matrix[row] &= (byte)~(1 << col);  // Сбрасываем бит (активный низкий)
        }

        // Отпустить клавишу (row, col) — вызывается из UI
        public void KeyUp(int row, int col)
        {
            matrix[row] |= (byte)(1 << col);   // Устанавливаем бит
        }

        // Отпустить все клавиши
        public void Reset()
        {
            for (int i = 0; i < 8; i++)
                matrix[i] = 0xFF;
        }
    }
}
