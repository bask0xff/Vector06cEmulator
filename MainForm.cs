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


            // Таймер для обновления экрана (50 Гц)
            screenTimer = new Timer();
            screenTimer.Interval = 20; // 50 Гц
            screenTimer.Tick += ScreenTimer_Tick;
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
            for (int y = 0; y < 256; y++)
            {
                int lineAddr = y * 32;
                for (int x = 0; x < 32; x++)
                {
                    int byteAddr = lineAddr + x;
                    byte pattern = ((y / 4) % 2 == 0) ? (byte)0xFF : (byte)0x00;
                    emulator.Video.WriteVideoRam((ushort)byteAddr, pattern);
                }
            }

            emulator.Video.SetPaletteColor(0x0E);
            emulator.Video.SetBorderColor(0x00);

            emulator.Video.UpdateScreenInternal(displayBitmap);
            pictureBox.Invalidate();

            statusLabel.Text = "Тест: жёлтые полосы нарисованы!";

            // ЗАМЕНИЛ Console.WriteLine НА DebugLog:
            DebugLog($"[ТЕСТ] pal={emulator.Video.GetCurrentPaletteIndex()}, VRAM={emulator.Video.CountNonZeroVideoRam()}");
            DebugLog($"Первые 8 байт VRAM: {emulator.Video.PeekVideoRam(0):X2} {emulator.Video.PeekVideoRam(1):X2} {emulator.Video.PeekVideoRam(2):X2} {emulator.Video.PeekVideoRam(3):X2}");
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
                    GetRomPath("Monitor0.rom"),
                    GetRomPath("MonitorF.rom")
                );
                emulator.LoadRom(GetRomPath("binland.rom"));
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
    }
}