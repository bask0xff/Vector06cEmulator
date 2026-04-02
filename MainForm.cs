using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Vector06cEmulator
{
    public class MainForm : Form
    {
        private PictureBox pictureBox;
        private Button runButton;
        private Button pauseButton;
        private Button testButton;
        private Label statusLabel;
        private Emulator emulator;
        private Timer screenTimer;
        private Bitmap displayBitmap;
        private TextBox debugTextBox;

        private bool isRunning = false;

        private int _tickCount = 0;

        public MainForm()
        {
            Text = "Вектор-06Ц Эмулятор";
            Width = 800;
            Height = 800;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            pictureBox = new PictureBox
            {
                Location = new Point(10, 10),
                Size = new Size(384, 384), // 256×256 увеличено в 1.5x
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle
            };

            runButton = new Button
            {
                Text = "Запуск",
                Location = new Point(10, 410),
                Size = new Size(100, 30)
            };
            runButton.Click += RunButton_Click;

            pauseButton = new Button
            {
                Text = "Пауза",
                Location = new Point(120, 410),
                Size = new Size(100, 30)
            };
            pauseButton.Click += PauseButton_Click;

            statusLabel = new Label
            {
                Text = "Статус: Остановлен",
                Location = new Point(10, 450),
                Size = new Size(380, 20)
            };

            testButton = new Button
            {
                Text = "Тест экрана",
                Location = new Point(230, 410),
                Size = new Size(120, 30)
            };
            testButton.Click += TestButton_Click;

            emulator = new Emulator();
            emulator.LogCallback = msg => DebugLog(msg);

            displayBitmap = new Bitmap(256, 256, PixelFormat.Format32bppArgb);
            pictureBox.Image = displayBitmap;
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;

            debugTextBox = new TextBox
            {
                Location = new Point(10, 480),
                Size = new Size(760, 250),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 9F),
                BackColor = Color.Black,
                ForeColor = Color.FromArgb(0, 255, 0)  // Зелёный как в терминале
            };

            Button loadRomButton = new Button
            {
                Text = "Загрузить ROM",
                Location = new Point(360, 410),
                Size = new Size(120, 30)
            };
            loadRomButton.Click += LoadRealRomButton_Click;

            Button resetButton = new Button
            {
                Text = "Сброс",
                Location = new Point(490, 410),
                Size = new Size(80, 30)
            };
            resetButton.Click += ResetButton_Click;

            Timer statusTimer = new Timer();
            statusTimer.Interval = 100; // Обновление каждые 100 мс
            statusTimer.Tick += (s, e) =>
            {
                if (isRunning)
                {
                    statusLabel.Text = $"Статус: Запущен | PC={emulator.Cpu.PC:X4} | A={emulator.Cpu.A:X2} | HALT={emulator.Cpu.Halted}";
                }
            };
            statusTimer.Start();

            Controls.Add(loadRomButton);
            Controls.Add(resetButton);

            Controls.Add(debugTextBox);
            Controls.Add(testButton);
            Controls.Add(pictureBox);
            Controls.Add(runButton);
            Controls.Add(pauseButton);
            Controls.Add(statusLabel);

            CreateTestGraphicsRom();


            // Таймер для обновления экрана (50 Гц)
            screenTimer = new Timer();
            screenTimer.Interval = 20; // 50 Гц
            screenTimer.Tick += ScreenTimer_Tick;
        }


        private void ResetButton_Click(object? sender, EventArgs e)
        {
            // Останавливаем эмулятор
            if (isRunning)
            {
                screenTimer.Stop();
                isRunning = false;
            }

            // Сбрасываем CPU
            emulator.Cpu.PC = 0x0100;
            emulator.Cpu.SP = 0xC000;
            emulator.Cpu.A = 0;
            emulator.Cpu.B = 0;
            emulator.Cpu.C = 0;
            emulator.Cpu.D = 0;
            emulator.Cpu.E = 0;
            emulator.Cpu.H = 0;
            emulator.Cpu.L = 0;
            emulator.Cpu.Halted = false;
            emulator.Cpu.IFF = false;

            // Сбрасываем видеоконтроллер
            emulator.IOBus.Out(0x00, 0x00);
            emulator.IOBus.Out(0x01, 0x01);
            emulator.IOBus.Out(0x10, 0x00);

            // Очищаем видеопамять
            for (int i = 0x1800; i <= 0x37FF; i++)
            {
                emulator.Memory.Write((ushort)i, 0);
            }

            // Обновляем экран
            emulator.Video.UpdateScreen();
            pictureBox.Image?.Dispose();
            pictureBox.Image = (Bitmap)emulator.Video.GetBitmap().Clone();

            statusLabel.Text = "Статус: Сброшен";
            runButton.Enabled = true;
            pauseButton.Enabled = false;

            DebugLog("Эмулятор сброшен");
        }

        private void CreateAnimationRom()
        {
            // Создаем ROM с простой анимацией
            byte[] romData = new byte[]
            {
                // Инициализация
                0x3E, 0x00, 0xD3, 0x00,  // MVI A,0 / OUT 0 - черный фон
                0x3E, 0x01, 0xD3, 0x01,  // MVI A,1 / OUT 1 - голубые пиксели
                0x3E, 0x00, 0xD3, 0x10,  // MVI A,0 / OUT 16 - скроллинг 0
        
                // Основной цикл
                0x21, 0x00, 0x18,        // Start: LXI H,1800h
                0x06, 0x20,              // MVI B,32
                0x3E, 0xFF,              // MVI A,FF
        
                0x77,                    // FillLoop: MOV M,A
                0x23,                    // INX H
                0x05,                    // DCR B
                0xC2, 0x08, 0x01,        // JNZ FillLoop
        
                // Задержка
                0x0E, 0xFF,              // Delay1: MVI C,FF
                0x0D,                    // Delay2: DCR C
                0xC2, 0x0D, 0x01,        // JNZ Delay2
                0x05,                    // DCR B
                0xC2, 0x0C, 0x01,        // JNZ Delay1
        
                // Очистка экрана
                0x21, 0x00, 0x18,        // LXI H,1800h
                0x06, 0x20,              // MVI B,32
                0x3E, 0x00,              // MVI A,0
        
                0x77,                    // ClearLoop: MOV M,A
                0x23,                    // INX H
                0x05,                    // DCR B
                0xC2, 0x1A, 0x01,        // JNZ ClearLoop
        
                // Задержка
                0x0E, 0xFF,              // Delay3: MVI C,FF
                0x0D,                    // Delay4: DCR C
                0xC2, 0x22, 0x01,        // JNZ Delay4
                0x05,                    // DCR B
                0xC2, 0x21, 0x01,        // JNZ Delay3
        
                // Меняем цвет
                0x3E, 0x02, 0xD3, 0x01,  // MVI A,2 / OUT 1 - зеленые пиксели
                0x0E, 0xFF,              // MVI C,FF (задержка)
        
                0xC3, 0x04, 0x01         // JMP Start
            };

            string path = Path.Combine(GetProjectPath(), "animation.rom");
            File.WriteAllBytes(path, romData);
            DebugLog($"[ROM] animation.rom создан ({romData.Length} байт)");
        }

        private void CreateTestGraphicsRom0()   
        {
            byte[] romData = new byte[]
            {
                // Ждём, пока монитор закончит инициализацию (примерно 0xF800+)
                0x21, 0x00, 0xF8,        // LXI H, F800
                0x7E,                    // MOV A,M
                0xFE, 0xC3,              // CPI C3   (обычно там JMP в мониторе)
                0xCA, 0x0A, 0x01,        // JZ WaitDone
                0xC3, 0x03, 0x01,        // JMP назад (простая задержка)

                // WaitDone:
                0x3E, 0x00, 0xD3, 0x00,  // MVI A,00h / OUT 00h   ← чёрная рамка
                0x3E, 0x01, 0xD3, 0x01,  // MVI A,01h / OUT 01h   ← голубой цвет линий !!!
                0x3E, 0x00, 0xD3, 0x10,  // MVI A,00h / OUT 10h   ← скролл = 0

                0x21, 0x00, 0x18,        // LXI H,1800h
                0x06, 0xFF,              // MVI B,FFh     (паттерн: все пиксели включены)
                0x0E, 0x00,              // MVI C,00h     (счётчик строк в группе)
                0x16, 0x00,              // MVI D,00h     (256 строк)

                // LineLoop:
                0x78,                    // MOV A,B
                0x1E, 0x20,              // MVI E,32      (32 байта на строку)
                // FillLoop:
                0x77,                    // MOV M,A
                0x23,                    // INX H
                0x1D,                    // DCR E
                0xC2, 0x25, 0x01,        // JNZ FillLoop

                0x0C,                    // INR C
                0x79, 0xFE, 0x04,        // MOV A,C / CPI 04
                0xC2, 0x38, 0x01,        // JNZ SkipToggle

                0x78, 0xEE, 0xFF, 0x47,  // MOV A,B / XRI FF / MOV B,A   ← переключаем паттерн
                0x0E, 0x00,              // MVI C,00

                // SkipToggle:
                0x15,                    // DCR D
                0xC2, 0x1F, 0x01,        // JNZ LineLoop

                0xC3, 0x3C, 0x01         // JMP $   (бесконечный цикл)
            };

            string path = Path.Combine(GetProjectPath(), "test_graphics.rom");
            File.WriteAllBytes(path, romData);
            DebugLog($"[ROM] test_graphics.rom обновлён ({romData.Length} байт) → {path}");
        }

        private void DebugLog(string message)
        {
            if (debugTextBox.InvokeRequired)
            {
                debugTextBox.Invoke(new Action(() => debugTextBox.AppendText(message + "\n")));
            }
            else
            {
                debugTextBox.AppendText(message + "\n");
            }
            debugTextBox.ScrollToCaret();
        }

        private void TestButton_Click(object? sender, EventArgs e)
        {
            // Заполняем видеопамять тестовым паттерном
            for (int y = 0; y < 256; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    ushort addr = (ushort)(0x1800 + y * 32 + x);
                    byte pattern = (byte)(((y / 8) % 2 == 0) ? 0xFF : 0x00);
                    emulator.Memory.Write(addr, pattern);
                }
            }

            // Устанавливаем цвета через IOBus (правильный способ)
            emulator.IOBus.Out(0x00, 0x09); // 06 - жёлтый фон
            emulator.IOBus.Out(0x01, 0x03); // 03 - голубые пиксели

            // Обновляем экран
            emulator.Video.UpdateScreen();
            pictureBox.Image?.Dispose();
            pictureBox.Image = (Bitmap)emulator.Video.GetBitmap().Clone();

            statusLabel.Text = "Тест: тестовый паттерн отображен!";

            DebugLog($"Видеопамять заполнена, фон=0x00, цвет=0x06");
            DebugLog($"Первые 8 байт VRAM: " +
                $"{emulator.Memory.Read(0x1800):X2} " +
                $"{emulator.Memory.Read(0x1801):X2} " +
                $"{emulator.Memory.Read(0x1802):X2} " +
                $"{emulator.Memory.Read(0x1803):X2}");
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;
            this.KeyUp += MainForm_KeyUp;
        }

        // Добавьте эти методы в класс MainForm
        private void LoadRealRomButton_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "ROM files (*.rom;*.bin)|*.rom;*.bin|All files (*.*)|*.*";
                openFileDialog.Title = "Выберите ROM файл для загрузки";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        byte[] romData = File.ReadAllBytes(openFileDialog.FileName);

                        // Для Вектор-06Ц программы обычно загружаются по адресу 0x0100
                        ushort loadAddress = 0x0100;

                        // Если файл имеет заголовок (например, .com или .exe), можно определить адрес
                        if (openFileDialog.FileName.EndsWith(".com"))
                            loadAddress = 0x0100;
                        else if (openFileDialog.FileName.EndsWith(".exe"))
                            loadAddress = 0x0100; // Для простоты пока так

                        emulator.Memory.Load(romData, loadAddress);

                        // Устанавливаем PC на точку входа
                        emulator.Cpu.PC = loadAddress;
                        emulator.Cpu.SP = 0xC000; // Стек в верхней части памяти

                        DebugLog($"Загружен ROM: {Path.GetFileName(openFileDialog.FileName)}");
                        DebugLog($"Размер: {romData.Length} байт, адрес: 0x{loadAddress:X4}");
                        DebugLog($"PC установлен на 0x{emulator.Cpu.PC:X4}, SP=0x{emulator.Cpu.SP:X4}");

                        statusLabel.Text = $"Загружен: {Path.GetFileName(openFileDialog.FileName)}";
                    }
                    catch (Exception ex)
                    {
                        DebugLog($"Ошибка загрузки ROM: {ex.Message}");
                        statusLabel.Text = "Ошибка загрузки ROM";
                    }
                }
            }
        }

        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            emulator.Keyboard.KeyDown(e.KeyCode);
        }

        private void MainForm_KeyUp(object? sender, KeyEventArgs e)
        {
            emulator.Keyboard.KeyUp(e.KeyCode);
        }

        private void UpdateBitmapPalette(Bitmap bitmap, VideoController video)
        {
            var paletteEntry = bitmap.Palette;

            // Стандартная палитра Вектор-06Ц
            Color[] colors = new Color[16]
            {
                Color.Black, Color.Blue, Color.Green, Color.Cyan,
                Color.Red, Color.Magenta, Color.LightYellow, Color.White,
                Color.DarkGray, Color.DarkBlue, Color.DarkGreen, Color.DarkCyan,
                Color.DarkRed, Color.DarkMagenta, Color.Yellow, Color.Gray
            };

            for (int i = 0; i < 16; i++)
            {
                paletteEntry.Entries[i] = Color.FromArgb(255, colors[i].R, colors[i].G, colors[i].B);
            }

            bitmap.Palette = paletteEntry;
        }

        private void RunButton_Click(object? sender, EventArgs e)
        {
            if (!isRunning)
            {
                //emulator.LoadMonitors(
                //    GetRomPath("Vector06C.rom"),
                //    GetRomPath("MonitorF.rom")
                //);
                emulator.LoadRom(GetRomPath("test_graphics.rom"), 0x0100);

                emulator.Cpu.PC = 0x0100;   // сразу на ROM
                emulator.Cpu.SP = 0xC000;

                emulator.Video.SetBorderColor(0x00);
                emulator.Video.SetPaletteColor(0x03);

                isRunning = true;
                screenTimer.Start();

                statusLabel.Text = "Статус: Запущен";
                runButton.Enabled = false;
                pauseButton.Enabled = true;
            }
        }

        private void PauseButton_Click(object? sender, EventArgs e)
        {
            isRunning = false;
            screenTimer.Stop();
            statusLabel.Text = "Статус: Пауза";
            runButton.Enabled = true;
            pauseButton.Enabled = false;
        }

        private void ScreenTimer_Tick(object? sender, EventArgs e)
        {
            // Выполняем несколько шагов CPU за каждый тик таймера
            for (int i = 0; i < 1200; i++) // ~60000 тактов / 50 Гц
            {
                if (emulator.Cpu.Halted)
                {
                    isRunning = false;
                    screenTimer.Stop();
                    statusLabel.Text = "Статус: HLT (остановлен)";
                    runButton.Enabled = true;
                    pauseButton.Enabled = false;
                    break;
                }
                emulator.Step();
            }

            // Обновляем экран
            emulator.Video.UpdateScreen();
            pictureBox.Image?.Dispose();
            pictureBox.Image = (Bitmap)emulator.Video.GetBitmap().Clone();

            if (++_tickCount % 50 == 0)   // каждую секунду
                emulator.PrintState();
        }


        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            screenTimer.Stop();
            emulator.Video.SaveScreenshot("final_screen.png");
            base.OnFormClosing(e);
        }

        private string GetProjectPath()
        {
            // Путь к папке проекта (где лежит .csproj)
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private string GetRomPath(string filename)
        {
            // Сначала пробуем в папке вывода
            string path1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
            if (File.Exists(path1))
                return path1;

            // Если не нашёл, пробуем в папке проекта (родитель папки bin)
            string projectPath = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.FullName;
            if (!string.IsNullOrEmpty(projectPath))
            {
                string path2 = Path.Combine(projectPath, filename);
                if (File.Exists(path2))
                    return path2;
            }

            // Возвращаем как есть (выдаст FileNotFoundException)
            return path1;
        }

        private void CreateTestGraphicsRom()
        {
            // Шахматная доска: клетки 8x8 пикселей
            // Строка = 32 байта = 256 пикселей
            // Паттерн строки чётного ряда:  AA AA AA AA ... (32 байта)
            // Паттерн строки нечётного ряда: 55 55 55 55 ... (32 байта)
            // Каждые 8 строк — инвертируем паттерн строки
            //
            // Загрузка по 0x0100
            // Адреса (вычислены вручную):
            //   0x0100  Init (12 байт)
            //   0x010C  DrawBoard
            //   0x010E    RowLoop
            //   0x0110      LineLoop
            //   0x0116        ByteLoop
            //   ...

            byte[] rom = new byte[]
            {
        // 0x0100 — Инициализация портов
        0x3E, 0x00, 0xD3, 0x00,    // MVI A,00 / OUT 00  (фон = чёрный)
        0x3E, 0x03, 0xD3, 0x01,    // MVI A,03 / OUT 01  (цвет = голубой)
        0x3E, 0x00, 0xD3, 0x10,    // MVI A,00 / OUT 10  (скролл = 0)

        // 0x010C — Начало DrawBoard
        0x21, 0x00, 0x18,          // LXI H, 0x1800   (H=начало VRAM)

        // B = счётчик рядов клеток (8 рядов по 8 строк + 8 рядов)
        // Используем D как "номер ряда" для выбора паттерна
        // D=0 → чётный ряд (AA/55), D=1 → нечётный (55/AA)
        0x16, 0x00,                // MVI D, 0   (номер ряда, чётность)
        0x06, 0x20,                // MVI B, 32  (32 ряда по 8 строк = 256 строк)

        // RowLoop: 0x0114
        // Выбираем паттерн: чётный ряд → C=0xAA, нечётный → C=0x55
        0x7A,                      // MOV A, D
        0xE6, 0x01,                // ANI 0x01   (проверяем бит 0)
        0xCA, 0x1C, 0x01,          // JZ EvenRow (0x011C)
        // Нечётный ряд:
        0x0E, 0x55,                // MVI C, 0x55
        0xC3, 0x1E, 0x01,          // JMP AfterPat (0x011E)
        // EvenRow: 0x011C
        0x0E, 0xAA,                // MVI C, 0xAA
        // AfterPat: 0x011E

        // Рисуем 8 строк текущего ряда
        0x1E, 0x08,                // MVI E, 8  (8 строк на ряд)

        // LineLoop: 0x0120
        // Рисуем одну строку (32 байта) с чередованием AA/55
        // Если чётная строка ряда → паттерн C, нечётная → инверсия C
        // Но для шахматки достаточно: все строки ряда одинаковы,
        // главное что соседний ряд инвертирован.
        // Внутри строки: байты чередуем AA 55 AA 55... (дают вертикальные полоски 4px)
        // Для клеток 8px используем: 0xFF на 4 байта, 0x00 на 4 байта (блоки 32px)
        // Проще: используем C как паттерн байта для всей строки

        0x79,                      // MOV A, C   (берём паттерн)
        0x26, 0x20,                // MVI H_temp... нет, просто заполним 32 байта

        // Заполняем 32 байта: чередуем C и ~C по 4 байта (клетки 32px)
        // PatInner: запишем 4 байта C, потом 4 байта ~C, повторить 4 раза

        // Используем регистр для счётчика внутри строки
        0x26, 0x04,                // ... 

        // УПРОЩЕНИЕ: для наглядности — вся строка одним байтом-паттерном
        // (чередование рядов даёт горизонтальные полосы, 
        //  чередование байт внутри строки даёт вертикальные)
        // Паттерн C меняем между строками тоже — для шахматки:
        0x79,                      // MOV A, C
        0x47,                      // MOV B_save... 

        // Перепишем проще — отдельные регистры:
        // Уберём всё лишнее
        0x76,                      // HLT — заглушка, перепишем ниже
            };

            // ↑ Этот подход стал запутанным. Пересчитаем с нуля чисто.
            DebugLog("[ROM] Генерирую шахматку...");
            CreateChessRom();
        }

        private void CreateChessRom()
        {
            // Шахматная доска 8x8 клеток, каждая клетка 32x32 пикселя
            // Строка VRAM = 32 байта = 256 пикселей
            // Клетка по X = 32 пикселя = 4 байта
            // Клетка по Y = 32 строки
            //
            // Ряд чётный  (0,2,4,6): байты FF FF FF FF 00 00 00 00 FF FF FF FF 00 00 00 00 ... (32 байта)
            // Ряд нечётный(1,3,5,7): байты 00 00 00 00 FF FF FF FF 00 00 00 00 FF FF FF FF ... (32 байта)
            //
            // Итого: 8 рядов * 32 строки = 256 строк ✓
            //
            // Алгоритм:
            //   B = ряд (0..7)
            //   для каждого ряда:
            //     C = 32 строки
            //     для каждой строки:
            //       записать 8 раз: [4 байта PAT1][4 байта PAT2]
            //       где PAT1=FF,PAT2=00 если ряд чётный; PAT1=00,PAT2=FF если нечётный
            //
            // Адреса (база 0x0100):
            // +0x00 = 0x0100: Init (12 байт)
            // +0x0C = 0x010C: LXI H,1800  (3)
            // +0x0F = 0x010F: MVI B,8     (2)
            // +0x11 = 0x0111: RowLoop     ← метка
            //   +0x00: MOV A,B            (1) 0x0111
            //   +0x01: ANI 01             (2) 0x0112
            //   +0x03: JZ EvenRow         (3) 0x0114  → 0x011A
            //   +0x06: MVI D,00 (PAT1=00) (2) 0x0117
            //   +0x08: MVI E,FF (PAT2=FF) (2) 0x0119
            //   +0x0A: JMP AfterPat       (3) 0x011B  → 0x011F
            // EvenRow: 0x011E (смещение +0x0E от RowLoop=0x0111 → 0x011F нет, пересчитаем)
            //
            // Лучше — посчитать байт за байтом:

            // Смещение от начала массива (= адрес - 0x0100):
            // [0]  3E 00 D3 00   MVI A,0 / OUT 0
            // [4]  3E 03 D3 01   MVI A,3 / OUT 1
            // [8]  3E 00 D3 10   MVI A,0 / OUT 0x10
            // [12] 21 00 18      LXI H, 0x1800
            // [15] 06 08         MVI B, 8         ; B = ряд (8 рядов)
            // RowLoop: смещение 17 → адрес 0x0111
            // [17] 78            MOV A, B
            // [18] E6 01         ANI 01
            // [20] CA 28 01      JZ 0x0128        ; чётный → PAT1=FF
            // [23] 16 00         MVI D, 0x00      ; нечётный PAT1=00
            // [25] 1E FF         MVI E, 0xFF      ; нечётный PAT2=FF
            // [27] C3 2A 01      JMP 0x012A
            // EvenRow: смещение 30 → 0x011E... нет, 0x0100+30=0x011E, но JZ выше → 0x0128?
            // Ошибка: 0x0100 + 0x28 = 0x0128, а смещение 30 = 0x1E → 0x0100+0x1E = 0x011E ≠ 0x0128
            // 
            // Нужно считать точно. Сделаем это программно:

            var code = new System.Collections.Generic.List<byte>();

            // Вспомогательная функция: текущий адрес
            ushort BaseAddr = 0x0100;
            ushort Addr() => (ushort)(BaseAddr + code.Count);

            void Emit(params byte[] bytes) { foreach (var b in bytes) code.Add(b); }
            void EmitWord(ushort w) { code.Add((byte)(w & 0xFF)); code.Add((byte)(w >> 8)); }

            // Плейсхолдеры для патчинга адресов
            int PlaceholderJZ() { var pos = code.Count; Emit(0xCA, 0x00, 0x00); return pos; }
            int PlaceholderJMP() { var pos = code.Count; Emit(0xC3, 0x00, 0x00); return pos; }
            int PlaceholderJNZ() { var pos = code.Count; Emit(0xC2, 0x00, 0x00); return pos; }
            void PatchAddr(int pos, ushort addr)
            {
                code[pos + 1] = (byte)(addr & 0xFF);
                code[pos + 2] = (byte)(addr >> 8);
            }

            // === Init ===
            Emit(0x3E, 0x00, 0xD3, 0x00);   // MVI A,0 / OUT 0
            Emit(0x3E, 0x03, 0xD3, 0x01);   // MVI A,3 / OUT 1
            Emit(0x3E, 0x00, 0xD3, 0x10);   // MVI A,0 / OUT 0x10

            // LXI H, 0x1800
            Emit(0x21, 0x00, 0x18);

            // MVI B, 8  (8 рядов клеток)
            Emit(0x06, 0x08);

            // RowLoop:
            ushort addrRowLoop = Addr();

            // MOV A,B / ANI 1
            Emit(0x78, 0xE6, 0x01);

            // JZ EvenRow (будем патчить)
            int jzEven = PlaceholderJZ();

            // Нечётный ряд: PAT1=0x00, PAT2=0xFF
            Emit(0x16, 0x00);   // MVI D, 0x00
            Emit(0x1E, 0xFF);   // MVI E, 0xFF
            int jmpAfterPat1 = PlaceholderJMP();

            // EvenRow:
            ushort addrEven = Addr();
            PatchAddr(jzEven, addrEven);

            // Чётный ряд: PAT1=0xFF, PAT2=0x00
            Emit(0x16, 0xFF);   // MVI D, 0xFF
            Emit(0x1E, 0x00);   // MVI E, 0x00

            // AfterPat:
            ushort addrAfterPat = Addr();
            PatchAddr(jmpAfterPat1, addrAfterPat);

            // MVI C, 32  (32 строки на ряд)
            Emit(0x0E, 0x20);

            // LineLoop:
            ushort addrLineLoop = Addr();

            // Рисуем одну строку: 8 блоков по 4 байта (PAT1 PAT1 PAT1 PAT1 PAT2 PAT2 PAT2 PAT2) x4
            // Используем стек для сохранения C (счётчик строк)
            // A-регистры: D=PAT1, E=PAT2
            // Будем использовать счётчик блоков в регистре (вложенный цикл)

            // PUSH B (сохраняем B=ряд, C=строки)
            Emit(0xC5);

            // MVI B, 4  (4 блока [PAT1×4 + PAT2×4] = 32 байта)
            Emit(0x06, 0x04);

            // BlockLoop:
            ushort addrBlockLoop = Addr();

            // Пишем 4 байта PAT1 (D)
            Emit(0x7A);           // MOV A, D
            Emit(0x77, 0x23);     // MOV M,A / INX H
            Emit(0x77, 0x23);     // MOV M,A / INX H
            Emit(0x77, 0x23);     // MOV M,A / INX H
            Emit(0x77, 0x23);     // MOV M,A / INX H

            // Пишем 4 байта PAT2 (E)
            Emit(0x7B);           // MOV A, E
            Emit(0x77, 0x23);     // MOV M,A / INX H
            Emit(0x77, 0x23);     // MOV M,A / INX H
            Emit(0x77, 0x23);     // MOV M,A / INX H
            Emit(0x77, 0x23);     // MOV M,A / INX H

            // DCR B / JNZ BlockLoop
            Emit(0x05);
            int jnzBlock = PlaceholderJNZ();
            PatchAddr(jnzBlock, addrBlockLoop);

            // POP B (восстанавливаем B=ряд, C=строки)
            Emit(0xC1);

            // DCR C / JNZ LineLoop
            Emit(0x0D);
            int jnzLine = PlaceholderJNZ();
            PatchAddr(jnzLine, addrLineLoop);

            // DCR B / JNZ RowLoop
            Emit(0x05);
            int jnzRow = PlaceholderJNZ();
            PatchAddr(jnzRow, addrRowLoop);

            // === Задержка ~1 сек ===
            ushort addrDelay = Addr();
            Emit(0x01, 0x00, 0x00);   // LXI B, 0  (65536 итераций)
            ushort addrDelayLoop = Addr();
            Emit(0x0B);               // DCX B
            Emit(0x78, 0xB1);         // MOV A,B / ORA C
            int jnzDelay = PlaceholderJNZ();
            PatchAddr(jnzDelay, addrDelayLoop);

            // === Инверсия VRAM ===
            Emit(0x21, 0x00, 0x18);   // LXI H, 0x1800
            Emit(0x11, 0x00, 0x20);   // LXI D, 0x2000 (8192)
            ushort addrInvLoop = Addr();
            Emit(0x7E);               // MOV A, M
            Emit(0xEE, 0xFF);         // XRI 0xFF
            Emit(0x77);               // MOV M, A
            Emit(0x23);               // INX H
            Emit(0x1B);               // DCX D
            Emit(0x7A, 0xB3);         // MOV A,D / ORA E
            int jnzInv = PlaceholderJNZ();
            PatchAddr(jnzInv, addrInvLoop);

            // JMP Delay (анимация)
            int jmpAnim = PlaceholderJMP();
            PatchAddr(jmpAnim, addrDelay);

            byte[] romData = code.ToArray();

            string path = System.IO.Path.Combine(GetProjectPath(), "test_graphics.rom");
            System.IO.File.WriteAllBytes(path, romData);
            DebugLog($"[ROM] Шахматная доска: {romData.Length} байт → {path}");
            DebugLog($"[ROM] RowLoop=0x{addrRowLoop:X4}, LineLoop=0x{addrLineLoop:X4}, DelayLoop=0x{addrDelayLoop:X4}");
        }

        private void CreateTestGraphicsRom3()
        {
            // Адреса при загрузке по 0x0100:
            // 0x0100 - начало программы
            // Метки вычислены вручную

            byte[] romData = new byte[]
            {
                    // === Инициализация видеорежима ===
                    // 0x0100
                    0x3E, 0x00, 0xD3, 0x00,  // MVI A,00 / OUT 00  - черный фон
                    0x3E, 0x05, 0xD3, 0x01,  // MVI A,06 / OUT 01  - желтые пиксели
                    0x3E, 0x00, 0xD3, 0x10,  // MVI A,00 / OUT 10  - скроллинг 0

                    // === Очистка VRAM (256*32 = 8192 байта) ===
                    // 0x010C
                    0x21, 0x00, 0x18,        // LXI H, 0x1800
                    0x11, 0x00, 0x20,        // LXI D, 0x2000  (счётчик 8192 = 0x2000)
                    0xAF,                    // XRA A  (A = 0)
                    // ClearLoop: 0x0115
                    0x77,                    // MOV M, A
                    0x23,                    // INX H
                    0x1B,                    // DCX D
                    0x7A, 0xB3,              // MOV A,D / ORA E
                    0xC2, 0x15, 0x01,        // JNZ ClearLoop (0x0115)

                    // === Рисуем горизонтальные полосы: 16 блоков по 16 строк ===
                    // Блок = 8 строк FF + 8 строк 00
                    // 0x011D
                    0x21, 0x00, 0x18,        // LXI H, 0x1800  (начало VRAM)
                    0x06, 0x10,              // MVI B, 16       (16 блоков)

                    // StripeLoop: 0x0123
                    // --- 8 строк пикселей (FF) ---
                    0x0E, 0x08,              // MVI C, 8        (8 строк)
                    // FillRowLoop: 0x0125
                    0x16, 0x20,              // MVI D, 32       (32 байта на строку)
                    0x3E, 0xFF,              // MVI A, FF
                    // FillByteLoop: 0x0129
                    0x77,                    // MOV M, A
                    0x23,                    // INX H
                    0x15,                    // DCR D
                    0xC2, 0x29, 0x01,        // JNZ FillByteLoop (0x0129)
                    0x0D,                    // DCR C
                    0xC2, 0x25, 0x01,        // JNZ FillRowLoop  (0x0125)

                    // --- 8 строк фона (00) ---
                    0x0E, 0x08,              // MVI C, 8        (8 строк)
                    // ClearRowLoop: 0x0136
                    0x16, 0x20,              // MVI D, 32       (32 байта на строку)
                    0x3E, 0x00,              // MVI A, 00
                    // ClearByteLoop: 0x013A
                    0x77,                    // MOV M, A
                    0x23,                    // INX H
                    0x15,                    // DCR D
                    0xC2, 0x3A, 0x01,        // JNZ ClearByteLoop (0x013A)
                    0x0D,                    // DCR C
                    0xC2, 0x36, 0x01,        // JNZ ClearRowLoop  (0x0136)

                    // --- следующий блок ---
                    0x05,                    // DCR B
                    0xC2, 0x23, 0x01,        // JNZ StripeLoop (0x0123)

                    // === Бесконечный цикл ===
                    // 0x0147
                    0xC3, 0x47, 0x01         // JMP 0x0147
            };

            string path = Path.Combine(GetProjectPath(), "test_graphics.rom");
            File.WriteAllBytes(path, romData);
            DebugLog($"[ROM] test_graphics.rom создан ({romData.Length} байт) → {path}");
        }

        private void CreateTestGraphicsRom1()
        {
            byte[] romData = new byte[]
            {
                0x3E, 0x00, 0xD3, 0x00,  // MVI A,00 / OUT 00  (border black)
                0x3E, 0x01, 0xD3, 0x01,  // MVI A,01 / OUT 01  (palette = голубой)
                0x3E, 0x00, 0xD3, 0x10,  // MVI A,00 / OUT 10  (scroll = 0)

                0x21, 0x00, 0x18,        // LXI H,1800
                0x06, 0xFF,              // MVI B,FF     (начальный паттерн)
                0x0E, 0x00,              // MVI C,00     (счётчик в блоке 4-х строк)
                0x16, 0x00,              // MVI D,00     (256 строк)

                // LineLoop
                0x78,                    // MOV A,B
                0x1E, 0x20,              // MVI E,32
                // FillLoop
                0x77, 0x23, 0x1D, 0xC2, 0x18, 0x01,  // MOV M,A / INX H / DCR E / JNZ FillLoop

                0x0C,                    // INR C
                0x79, 0xFE, 0x04,        // MOV A,C / CPI 04
                0xC2, 0x2B, 0x01,        // JNZ SkipToggle

                0x78, 0xEE, 0xFF, 0x47,  // MOV A,B / XRI FF / MOV B,A
                0x0E, 0x00,              // MVI C,00

                // SkipToggle
                0x15, 0xC2, 0x15, 0x01,  // DCR D / JNZ LineLoop

                0xC3, 0x2F, 0x01         // JMP $ (бесконечный цикл)
            };

            string path = Path.Combine(GetProjectPath(), "test_graphics.rom");
            File.WriteAllBytes(path, romData);
            DebugLog($"[ROM] test_graphics.rom создан ({romData.Length} байт) → {path}");
        }

        private void CreateTestGraphicsRom2()
        {
            // Простой тестовый ROM - рисует полосы и шахматку
            byte[] romData = new byte[]
            {
                // Инициализация видеорежима
                0x3E, 0x00, 0xD3, 0x00,  // MVI A,00 / OUT 00  - черный фон
                0x3E, 0x01, 0xD3, 0x01,  // MVI A,01 / OUT 01  - голубые пиксели
                0x3E, 0x00, 0xD3, 0x10,  // MVI A,00 / OUT 10  - скроллинг 0
        
                // Очистка видеопамяти
                0x21, 0x00, 0x18,        // LXI H,1800h - начало видеопамяти
                0x01, 0x00, 0x20,        // LXI B,2000h - 8192 байта (32*256)
                0x3E, 0x00,              // MVI A,00
                0x77,                    // FillLoop: MOV M,A
                0x23,                    // INX H
                0x0B,                    // DCX B
                0x78, 0xB1,              // MOV A,B / ORA C
                0xC2, 0x06, 0x01,        // JNZ FillLoop
        
                // Рисуем горизонтальные полосы
                0x21, 0x00, 0x18,        // LXI H,1800h
                0x06, 0x10,              // MVI B,16 (16 полос)
        
                0x3E, 0xFF,              // StripeLoop: MVI A,FF
                0x0E, 0x10,              // MVI C,16 (16 строк на полосу)
        
                0x16, 0x20,              // LineLoop: MVI D,32 (32 байта на строку)
                0x77,                    // ByteLoop: MOV M,A
                0x23,                    // INX H
                0x15,                    // DCR D
                0xC2, 0x14, 0x01,        // JNZ ByteLoop
        
                0x0D,                    // DCR C
                0xC2, 0x12, 0x01,        // JNZ LineLoop
        
                0x3E, 0x00,              // MVI A,00
                0x16, 0x20,              // MVI D,32
                0x77,                    // BlackLoop: MOV M,A
                0x23,                    // INX H
                0x15,                    // DCR D
                0xC2, 0x1D, 0x01,        // JNZ BlackLoop
        
                0x05,                    // DCR B
                0xC2, 0x10, 0x01,        // JNZ StripeLoop
        
                // Бесконечный цикл
                0xC3, 0x25, 0x01         // JMP $
            };

            string path = Path.Combine(GetProjectPath(), "test_graphics.rom");
            File.WriteAllBytes(path, romData);
            DebugLog($"[ROM] test_graphics.rom создан ({romData.Length} байт) → {path}");
        }
    }
}