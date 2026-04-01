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

        private void CreateTestGraphicsRom0()   // ← обнови эту функцию
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
            emulator.IOBus.Out(0x00, 0x00); // Черный фон
            emulator.IOBus.Out(0x01, 0x06); // Желтые пиксели

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
                emulator.LoadMonitors(
                    GetRomPath("Vector06C.rom"),
                    GetRomPath("MonitorF.rom")
                );
                emulator.LoadRom(GetRomPath("test_graphics.rom"));

                emulator.Video.SetBorderColor(0x00);
                emulator.Video.SetPaletteColor(0x01);   // голубой

                emulator.Start();

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

            // Показываем изображение в PictureBox
            pictureBox.Image?.Dispose();
            pictureBox.Image = (Bitmap)emulator.Video.GetBitmap().Clone();

            statusLabel.Text = $"Статус: Запущен, PC={emulator.Cpu.PC:X4}";
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

        private void CreateTestGraphicsRom()
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