using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
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
        private TextBox programTextBox;

        private bool isRunning = false;

        private int _tickCount = 0;

        public MainForm()
        {
            Text = "Вектор-06Ц Эмулятор";
            Width = 1024;
            Height = 920;
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
                Location = new Point(500, 10),
                Size = new Size(500, 750),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 9F),
                BackColor = Color.Black,
                ForeColor = Color.FromArgb(0, 255, 0)  // Зелёный как в терминале
            };

            programTextBox = new TextBox
            {
                Location = new Point(2, 520),
                Size = new Size(500, 230),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 9F),
                BackColor = Color.DarkBlue,
                ForeColor = Color.Yellow
            };

            Button loadRomButton = new Button
            {
                Text = "Загрузить ROM",
                Location = new Point(10, 470),
                Size = new Size(120, 30)
            };
            loadRomButton.Click += LoadRealRomButton_Click;

            Button resetButton = new Button
            {
                Text = "Сброс",
                Location = new Point(310, 470),
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

            Button diagnosticButton = new Button
            {
                Text = "Диагностика",
                Location = new Point(360, 410),
                Size = new Size(100, 30)
            };

            diagnosticButton.Click += CheckVideoMemoryButton_Click;
            Controls.Add(diagnosticButton);

            Controls.Add(loadRomButton);
            Controls.Add(resetButton);

            Controls.Add(debugTextBox);
            Controls.Add(programTextBox);
            Controls.Add(testButton);
            Controls.Add(pictureBox);
            Controls.Add(runButton);
            Controls.Add(pauseButton);
            Controls.Add(statusLabel);

            CreatePatternTestRom();
            CreateSimpleDebugRom();
            /*CreateDebugRom();
            CreateWorkingTestRom();
            CreateMinimalTestRom();
            CreateDiagnosticRom();
            CreateSimpleHelloWorldRom();
            
            CreateTestGraphicsRom();
            CreateFullBlueRom();
            CreateSnakeRom();*/

            statusTimer.Start();

            // Таймер для обновления экрана (50 Гц)
            screenTimer = new Timer();
            screenTimer.Interval = 20; // 50 Гц
            screenTimer.Tick += ScreenTimer_Tick;
        }

        private void CreateDiagnosticRom()
        {
            var code = new List<byte>();
            void Emit(params byte[] b) => code.AddRange(b);

            // Инициализация
            Emit(0x3E, 0x00, 0xD3, 0x00);     // Чёрный фон
            Emit(0x3E, 0x03, 0xD3, 0x01);     // Голубой цвет пикселей
            Emit(0x3E, 0x00, 0xD3, 0x10);     // Скроллинг 0

            // Тест 1: Рисуем вертикальную линию (X=100) через всю высоту
            Emit(0x21, 0x00, 0x18);           // LXI H, 1800h
            Emit(0x01, 0x00, 0x20);           // LXI B, 2000h (8192)
            Emit(0xAF);                       // XRA A (A=0)

            // Очистка
            ushort clearLoop = (ushort)(0x0100 + code.Count);
            Emit(0x77, 0x23, 0x0B, 0x78, 0xB1, 0xC2, (byte)clearLoop, (byte)(clearLoop >> 8));

            // Рисуем вертикальную линию в X=100
            // Адрес = 0x1800 + Y*32 + 12 (т.к. 100/8 = 12.5 -> байт 12, бит 4)
            Emit(0x06, 0x00);                 // MVI B, 0 (счётчик Y)
            ushort vertLoop = (ushort)(0x0100 + code.Count);
            Emit(0x78);                       // MOV A, B (Y)
            Emit(0x07, 0x07, 0x07);           // RLC x3 (умножаем на 8)
            Emit(0x80);                       // ADD B (Y*9?)
                                              // Упростим: используем HL = 0x1800 + Y*32
            Emit(0x21, 0x00, 0x18);           // LXI H, 1800h
            Emit(0x78, 0x07, 0x07, 0x07);     // MOV A,B / RLC x3
            Emit(0x85);                       // ADD L
            Emit(0x6F);                       // MOV L, A
                                              // Добавляем 12 (смещение байта для X=100)
            Emit(0x7D, 0xC6, 0x0C, 0x6F);     // MOV A,L / ADI 12 / MOV L, A

            // Устанавливаем бит 4 (16) в байте
            Emit(0x7E, 0xF6, 0x10, 0x77);     // MOV A,M / ORI 10h / MOV M,A

            Emit(0x04);                       // INR B
            Emit(0x78, 0xFE, 0x00, 0xC2);     // MOV A,B / CPI 0 / JNZ (B<256)
            Emit((byte)vertLoop, (byte)(vertLoop >> 8));

            // Бесконечный цикл
            Emit(0xC3, 0x00, 0x01);

            var romData = code.ToArray();
            string path = Path.Combine(GetProjectPath(), "diagnostic.rom");
            File.WriteAllBytes(path, romData);
            DebugLog($"Diagnostic ROM создан: {romData.Length} байт → {path}");
        }


        private void CreateSimpleHelloWorldRom()
        {
            var code = new List<byte>();
            void Emit(params byte[] b) => code.AddRange(b);

            // ============================================
            // ПРОСТАЯ ПРОГРАММА "HELLO WORLD!"
            // Рисует точки в видеопамяти, формируя текст
            // ============================================

            // Инициализация видеорежима
            Emit(0x3E, 0x00, 0xD3, 0x00);     // MVI A,00 / OUT 00 - чёрный фон
            Emit(0x3E, 0x03, 0xD3, 0x01);     // MVI A,03 / OUT 01 - голубой/циан цвет
            Emit(0x3E, 0x00, 0xD3, 0x10);     // MVI A,00 / OUT 10 - скроллинг 0

            // Очистка видеопамяти
            Emit(0x21, 0x00, 0x18);           // LXI H, 1800h
            Emit(0x01, 0x00, 0x20);           // LXI B, 2000h (8192 байта)
            Emit(0xAF);                       // XRA A (A=0)

            ushort clearLoop = (ushort)(0x0100 + code.Count);
            Emit(0x77);                       // MOV M, A
            Emit(0x23);                       // INX H
            Emit(0x0B);                       // DCX B
            Emit(0x78, 0xB1);                 // MOV A,B / ORA C
            Emit(0xC2, (byte)clearLoop, (byte)(clearLoop >> 8));

            // === РИСУЕМ ТЕКСТ "HELLO" (8x8 пикселей, простая матрица) ===

            // Буква H (позиция X=40, Y=100)
            // Адрес: 0x1800 + 100*32 + 40/8 = 0x1800 + 3200 + 5 = 0x1800 + 0xC80 + 5 = 0x2485
            Emit(0x21, 0x85, 0x24);           // LXI H, 0x2485 (адрес начала буквы H)
            Emit(0x36, 0x81);                 // MVI M, 81h (10000001)
            Emit(0x2C, 0x36, 0x81);           // INR L / MVI M, 81h (следующая строка)
            Emit(0x2C, 0x36, 0x81);           // INR L / MVI M, 81h
            Emit(0x2C, 0x36, 0xFF);           // INR L / MVI M, FFh (11111111)
            Emit(0x2C, 0x36, 0x81);           // INR L / MVI M, 81h
            Emit(0x2C, 0x36, 0x81);           // INR L / MVI M, 81h
            Emit(0x2C, 0x36, 0x81);           // INR L / MVI M, 81h
            Emit(0x2C, 0x36, 0x81);           // INR L / MVI M, 81h

            // Буква E (позиция X=56, Y=100)
            // Адрес: 0x1800 + 100*32 + 56/8 = 0x1800 + 3200 + 7 = 0x2487
            Emit(0x21, 0x87, 0x24);           // LXI H, 0x2487
            Emit(0x36, 0xFE);                 // MVI M, FEh (11111110)
            Emit(0x2C, 0x36, 0x80);           // INR L / MVI M, 80h
            Emit(0x2C, 0x36, 0x80);           // INR L / MVI M, 80h
            Emit(0x2C, 0x36, 0xF8);           // INR L / MVI M, F8h (11111000)
            Emit(0x2C, 0x36, 0x80);           // INR L / MVI M, 80h
            Emit(0x2C, 0x36, 0x80);           // INR L / MVI M, 80h
            Emit(0x2C, 0x36, 0xFE);           // INR L / MVI M, FEh
            Emit(0x2C, 0x36, 0x80);           // INR L / MVI M, 80h

            // Буква L (позиция X=72, Y=100)
            // Адрес: 0x1800 + 100*32 + 72/8 = 0x1800 + 3200 + 9 = 0x2489
            Emit(0x21, 0x89, 0x24);           // LXI H, 0x2489
            Emit(0x36, 0x80);                 // MVI M, 80h
            Emit(0x2C, 0x36, 0x80);           // INR L / MVI M, 80h
            Emit(0x2C, 0x36, 0x80);           // INR L / MVI M, 80h
            Emit(0x2C, 0x36, 0x80);           // INR L / MVI M, 80h
            Emit(0x2C, 0x36, 0x80);           // INR L / MVI M, 80h
            Emit(0x2C, 0x36, 0x80);           // INR L / MVI M, 80h
            Emit(0x2C, 0x36, 0xFE);           // INR L / MVI M, FEh
            Emit(0x2C, 0x36, 0x80);           // INR L / MVI M, 80h

            // Буква L (позиция X=88, Y=100) - вторая L
            // Адрес: 0x1800 + 100*32 + 88/8 = 0x1800 + 3200 + 11 = 0x248B
            Emit(0x21, 0x8B, 0x24);           // LXI H, 0x248B
            Emit(0x36, 0x80);                 // MVI M, 80h
            Emit(0x2C, 0x36, 0x80);           // INR L / MVI M, 80h
            Emit(0x2C, 0x36, 0x80);           // INR L / MVI M, 80h
            Emit(0x2C, 0x36, 0x80);           // INR L / MVI M, 80h
            Emit(0x2C, 0x36, 0x80);           // INR L / MVI M, 80h
            Emit(0x2C, 0x36, 0x80);           // INR L / MVI M, 80h
            Emit(0x2C, 0x36, 0xFE);           // INR L / MVI M, FEh
            Emit(0x2C, 0x36, 0x80);           // INR L / MVI M, 80h

            // Буква O (позиция X=104, Y=100)
            // Адрес: 0x1800 + 100*32 + 104/8 = 0x1800 + 3200 + 13 = 0x248D
            Emit(0x21, 0x8D, 0x24);           // LXI H, 0x248D
            Emit(0x36, 0x7C);                 // MVI M, 7Ch (01111100)
            Emit(0x2C, 0x36, 0x82);           // INR L / MVI M, 82h (10000010)
            Emit(0x2C, 0x36, 0x82);           // INR L / MVI M, 82h
            Emit(0x2C, 0x36, 0x82);           // INR L / MVI M, 82h
            Emit(0x2C, 0x36, 0x82);           // INR L / MVI M, 82h
            Emit(0x2C, 0x36, 0x82);           // INR L / MVI M, 82h
            Emit(0x2C, 0x36, 0x7C);           // INR L / MVI M, 7Ch
            Emit(0x2C, 0x36, 0x80);           // INR L / MVI M, 80h

            // === БЕСКОНЕЧНЫЙ ЦИКЛ ===
            Emit(0xC3, 0x00, 0x01);           // JMP 0x0100

            var romData = code.ToArray();
            string path = Path.Combine(GetProjectPath(), "hello_simple.rom");
            File.WriteAllBytes(path, romData);
            DebugLog($"Simple Hello ROM создан: {romData.Length} байт → {path}");

            // Выводим дамп для отладки
            DebugLog("ROM Contents:");
            for (int i = 0; i < romData.Length; i += 16)
            {
                string hex = BitConverter.ToString(romData, i, Math.Min(16, romData.Length - i)).Replace("-", " ");
                DebugLog($"0x{(0x0100 + i):X4}: {hex}");
            }
        }

        private void CreateSnakeRom()
        {
            var code = new List<byte>();

            void Emit(params byte[] b) => code.AddRange(b);

            // ==================== ИНИЦИАЛИЗАЦИЯ ====================
            Emit(0x3E, 0x00, 0xD3, 0x00);     // OUT 00 - чёрный фон
            Emit(0x3E, 0x01, 0xD3, 0x01);     // OUT 01 - голубой цвет
            Emit(0x3E, 0x00, 0xD3, 0x10);     // OUT 10 - скролл = 0

            // Очистка видеопамяти
            Emit(0x21, 0x00, 0x18);           // LXI H, 1800h
            Emit(0x11, 0x00, 0x20);           // LXI D, 2000h
            Emit(0xAF);                       // XRA A  (A = 0)
            ushort clearLoop = (ushort)(0x0100 + code.Count);
            Emit(0x77, 0x23, 0x1B, 0x7A, 0xB3);
            Emit(0xC2, (byte)clearLoop, (byte)(clearLoop >> 8)); // JNZ clearLoop

            // Начальная позиция головы
            Emit(0x3E, 0x40, 0x32, 0x00, 0xC0); // STA 0xC000  X = 64
            Emit(0x3E, 0x40, 0x32, 0x01, 0xC0); // STA 0xC001  Y = 64

            // ==================== ГЛАВНЫЙ ЦИКЛ ====================
            ushort mainLoop = (ushort)(0x0100 + code.Count);

            // Рисуем точку (голову)
            Emit(0x3A, 0x01, 0xC0);           // LDA 0xC001 (Y)
            Emit(0x47);                       // MOV B, A
            Emit(0x3A, 0x00, 0xC0);           // LDA 0xC000 (X)
            Emit(0x4F);                       // MOV C, A
            Emit(0xCD, 0x50, 0x01);           // CALL PutPixel (будет по адресу ~0x0150)

            // Двигаем вправо
            Emit(0x3A, 0x00, 0xC0);           // LDA X
            Emit(0x3C);                       // INR A
            Emit(0x32, 0x00, 0xC0);           // STA X

            // Простая задержка
            Emit(0x06, 0x10);                 // MVI B, 16
            ushort delay = (ushort)(0x0100 + code.Count);
            Emit(0x0E, 0x00);                 // MVI C, 0
            ushort delayInner = (ushort)(0x0100 + code.Count);
            Emit(0x0D);                       // DCR C
            Emit(0xC2, (byte)delayInner, (byte)(delayInner >> 8));
            Emit(0x05);                       // DCR B
            Emit(0xC2, (byte)delay, (byte)(delay >> 8));

            Emit(0xC3, (byte)mainLoop, (byte)(mainLoop >> 8)); // JMP mainLoop

            // ==================== PutPixel ====================
            ushort putPixelAddr = (ushort)(0x0100 + code.Count);

            Emit(0x78);                       // MOV A, B     ; Y
            Emit(0x07, 0x07, 0x07);           // RLC x3
            Emit(0x80);                       // ADD B        ; Y*8
            Emit(0x47);                       // MOV B, A
            Emit(0x79);                       // MOV A, C     ; X
            Emit(0x0F, 0x0F, 0x0F);           // RRC x3
            Emit(0xB0);                       // ORA B
            Emit(0xC6, 0x18);                 // ADI 0x18     ; грубо 0x1800
            Emit(0x6F);                       // MOV L, A
            Emit(0x26, 0x18);                 // MVI H, 0x18
            Emit(0x3E, 0xFF, 0x77);           // MVI A,FF / MOV M,A
            Emit(0xC9);                       // RET

            var romData = code.ToArray();

            string path = Path.Combine(GetProjectPath(), "snake.rom");
            File.WriteAllBytes(path, romData);

            DebugLog($"Snake ROM создан: {romData.Length} байт");
            DebugLog($"MainLoop = 0x{mainLoop:X4}, PutPixel = 0x{putPixelAddr:X4}");
        }

        private void CreateFullBlueRom()
        {
            byte[] romData = new byte[]
            {
                // 0x0100 — Инициализация
                0x3E, 0x00, 0xD3, 0x00,     // MVI A,00 / OUT 00  → чёрная рамка
                0x3E, 0x01, 0xD3, 0x01,     // MVI A,01 / OUT 01  → голубой цвет пикселей (индекс 1)
                0x3E, 0x00, 0xD3, 0x10,     // MVI A,00 / OUT 10  → скролл = 0

                // Заполняем всю видеопамять 0xFF (все пиксели включены)
                0x21, 0x00, 0x18,           // LXI H, 1800h
                0x11, 0x00, 0x20,           // LXI D, 2000h  (8192 байта = 0x2000)
                0x3E, 0xFF,                 // MVI A, FFh

                // FillLoop:
                0x77,                       // MOV M, A
                0x23,                       // INX H
                0x1B,                       // DCX D
                0x7A, 0xB3,                 // MOV A, D / ORA E
                0xC2, 0x0F, 0x01,           // JNZ FillLoop   (адрес 0x010F)

                // Бесконечный цикл
                0xC3, 0x1B, 0x01            // JMP $ (0x011B)
            };

            string path = Path.Combine(GetProjectPath(), "full_blue.rom");
            File.WriteAllBytes(path, romData);

            DebugLog($"[ROM] full_blue.rom создан ({romData.Length} байт) → {path}");
            DebugLog("Этот ROM должен залить весь экран голубым цветом");
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
            emulator.Video.UpdateScreenDebug();
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

        private void CreateWorkingTestRom()
        {
            var code = new List<byte>();
            void Emit(params byte[] b) => code.AddRange(b);

            // ============================================
            // ПРОСТАЯ ТЕСТОВАЯ ПРОГРАММА
            // Рисует горизонтальные полосы на экране
            // ============================================

            // 0x0100: Инициализация портов
            Emit(0x3E, 0x00);        // MVI A, 0
            Emit(0xD3, 0x00);        // OUT 0x00 - чёрный фон

            Emit(0x3E, 0x03);        // MVI A, 3
            Emit(0xD3, 0x01);        // OUT 0x01 - голубой цвет

            Emit(0x3E, 0x00);        // MVI A, 0
            Emit(0xD3, 0x10);        // OUT 0x10 - скроллинг 0

            // 0x010C: Заполняем видеопамять паттерном 0xFF (все пиксели включены)
            Emit(0x21, 0x00, 0x18);  // LXI H, 0x1800
            Emit(0x01, 0x00, 0x20);  // LXI B, 0x2000 (8192 байта)
            Emit(0x3E, 0xFF);        // MVI A, 0xFF

            ushort fillLoop = (ushort)(0x0100 + code.Count);
            Emit(0x77);              // MOV M, A
            Emit(0x23);              // INX H
            Emit(0x0B);              // DCX B
            Emit(0x78);              // MOV A, B
            Emit(0xB1);              // ORA C
            Emit(0xC2, (byte)fillLoop, (byte)(fillLoop >> 8)); // JNZ fillLoop

            // 0x011E: Бесконечный цикл
            Emit(0xC3, 0x1E, 0x01);  // JMP 0x011E

            var romData = code.ToArray();
            string path = Path.Combine(GetProjectPath(), "working_test.rom");
            File.WriteAllBytes(path, romData);

            DebugLog($"=== WORKING TEST ROM ===");
            DebugLog($"Size: {romData.Length} bytes");
            DebugLog($"Path: {path}");
            DebugLog("Expected: Full blue screen");

            // Выводим дамп для верификации
            DebugLog("\nROM dump:");
            for (int i = 0; i < romData.Length; i += 8)
            {
                string hex = BitConverter.ToString(romData, i, Math.Min(8, romData.Length - i)).Replace("-", " ");
                DebugLog($"0x{(0x0100 + i):X4}: {hex}");
            }
        }


        private void CreateMinimalTestRom()
        {
            var code = new List<byte>();
            void Emit(params byte[] b) => code.AddRange(b);

            // Минимальная программа - просто меняет цвет фона
            Emit(0x3E, 0x07);        // MVI A, 7 (белый)
            Emit(0xD3, 0x00);        // OUT 0x00 - белый фон

            Emit(0x3E, 0x04);        // MVI A, 4 (красный)
            Emit(0xD3, 0x01);        // OUT 0x01 - красные пиксели

            // Бесконечный цикл
            Emit(0xC3, 0x06, 0x01);  // JMP 0x0106

            var romData = code.ToArray();
            string path = Path.Combine(GetProjectPath(), "minimal_test.rom");
            File.WriteAllBytes(path, romData);

            DebugLog($"Minimal test ROM created: {path}");
            DebugLog("Expected: White background, red pixels (no video RAM writes)");
        }

        private void VerifyRomLoaded()
        {
            DebugLog("\n=== VERIFYING ROM LOADED ===");
            DebugLog($"PC = 0x{emulator.Cpu.PC:X4}");
            DebugLog($"SP = 0x{emulator.Cpu.SP:X4}");

            // Показываем первые 32 байта памяти по адресу 0x0100
            DebugLog("\nFirst 32 bytes at 0x0100:");
            for (int i = 0; i < 32; i += 16)
            {
                string hex = "";
                for (int j = 0; j < 16; j++)
                {
                    hex += $"{emulator.Memory.Read((ushort)(0x0100 + i + j)):X2} ";
                }
                DebugLog($"0x{(0x0100 + i):X4}: {hex}");
            }

            // Проверяем, что видеопамять чиста
            int nonZero = 0;
            for (int i = 0x1800; i <= 0x181F; i++)
            {
                if (emulator.Memory.Read((ushort)i) != 0)
                    nonZero++;
            }
            DebugLog($"\nNon-zero bytes in first 32 bytes of VRAM: {nonZero}");
        }


        private void DebugLog(string message)
        {
            if (debugTextBox.InvokeRequired)
            {
                debugTextBox.Invoke(new Action(() => debugTextBox.AppendText(message + "\r\n")));
            }
            else
            {
                debugTextBox.AppendText(message + "\r\n");
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
            emulator.Video.UpdateScreenDebug();
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
                    loadFileRom(openFileDialog.FileName);
                }
            }
        }

        private void loadFileRom(string fileName)
        {
            try
            {
                byte[] romData = File.ReadAllBytes(fileName);
                ushort loadAddress = 0x0100;

                // ОЧИЩАЕМ память перед загрузкой (опционально)
                // for (int i = 0; i < 0x1000; i++)
                //     emulator.Memory.Write((ushort)(loadAddress + i), 0);

                emulator.Memory.Load(romData, loadAddress);
                emulator.Cpu.PC = loadAddress;
                emulator.Cpu.SP = 0xC000;

                // Сбрасываем состояние CPU
                emulator.Cpu.Halted = false;
                emulator.Cpu.A = 0;
                emulator.Cpu.B = 0;
                emulator.Cpu.C = 0;
                emulator.Cpu.D = 0;
                emulator.Cpu.E = 0;
                emulator.Cpu.H = 0;
                emulator.Cpu.L = 0;

                DebugLog($"\n=== ROM LOADED ===");
                DebugLog($"File: {Path.GetFileName(fileName)}");
                DebugLog($"Size: {romData.Length} bytes");
                DebugLog($"Load address: 0x{loadAddress:X4}");
                DebugLog($"PC: 0x{emulator.Cpu.PC:X4}");
                DebugLog($"SP: 0x{emulator.Cpu.SP:X4}");

                // Показываем содержимое загруженного файла
                string hexDump = "";
                for (int i = 0; i < Math.Min(64, romData.Length); i += 16)
                {
                    string hex = BitConverter.ToString(romData, i, Math.Min(16, romData.Length - i)).Replace("-", " ");
                    hexDump += $"0x{loadAddress + i:X4}: {hex}\r\n";
                }
                programTextBox.Text = hexDump;

                VerifyRomLoaded(); // Вызываем верификацию

                statusLabel.Text = $"Загружен: {Path.GetFileName(fileName)}";
                Text = $"Вектор-06Ц Эмулятор - {Path.GetFileName(fileName)}";
            }
            catch (Exception ex)
            {
                DebugLog($"Ошибка загрузки ROM: {ex.Message}");
                statusLabel.Text = "Ошибка загрузки ROM";
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
                // НЕ загружаем ROM здесь, используем уже загруженный через Load ROM кнопку
                // Просто запускаем эмулятор с уже загруженной программой

                // Убеждаемся, что PC указывает на начало программы
                if (emulator.Cpu.PC == 0)
                    emulator.Cpu.PC = 0x0100;
                if (emulator.Cpu.SP == 0)
                    emulator.Cpu.SP = 0xC000;

                // Устанавливаем цвета
                emulator.Video.SetBorderColor(0x00);
                emulator.Video.SetPaletteColor(0x01);

                // Очищаем экран перед запуском
                for (int i = 0x1800; i <= 0x37FF; i++)
                    emulator.Memory.Write((ushort)i, 0);

                isRunning = true;
                screenTimer.Start();
                statusLabel.Text = "Статус: Запущен";
                runButton.Enabled = false;
                pauseButton.Enabled = true;

                DebugLog("=== ЭМУЛЯЦИЯ ЗАПУЩЕНА ===");
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
    for (int i = 0; i < 100; i++)
    {
        if (emulator.Cpu.Halted)
        {
            isRunning = false;
            screenTimer.Stop();
            statusLabel.Text = "Статус: HLT (остановлен)";
            runButton.Enabled = true;
            pauseButton.Enabled = false;
            DebugLog("CPU HALTED - остановка эмуляции");
            break;
        }
        emulator.Step();
    }

    // ПОСЛЕ выполнения, если CPU остановлен, проверяем пиксели
    if (emulator.Cpu.Halted)
    {
        DebugLog("\n=== PIXEL CHECK ===");
        emulator.Video.CheckPixel(0, 0);
        emulator.Video.CheckPixel(7, 0);
        emulator.Video.CheckPixel(8, 0);

        byte border = emulator.IOBus.In(0x00);
        byte color = emulator.IOBus.In(0x01);
        DebugLog($"Port 0x00 (border) = {border}");
        DebugLog($"Port 0x01 (color) = {color}");

        DebugLog("\n=== VRAM DUMP (first 16 bytes) ===");
        for (int i = 0; i < 16; i++)
        {
            byte val = emulator.Memory.Read((ushort)(0x1800 + i));
            DebugLog($"0x{0x1800 + i:X4}: 0x{val:X2}");
        }
    }

    // ВАЖНО: вызываем UpdateScreenDebug, а не UpdateScreen
    emulator.Video.UpdateScreenDebug();
    
    pictureBox.Image?.Dispose();
    pictureBox.Image = (Bitmap)emulator.Video.GetBitmap().Clone();
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
            DebugLog("Loading " + filename);
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
            //DebugLog("[ROM] Генерирую шахматку...");
            CreateChessRom();
            //CreateSpriteAnimationRom();
            CreateInteractiveSpriteRom();
        }


        private void CreateInteractiveSpriteRom()
        {
            var code = new List<byte>();
            void Emit(params byte[] b) => code.AddRange(b);

            ushort varX = 0xC000; // X (0-31)
            ushort varY = 0xC001; // Y (0-255)

            // 1. ИНИЦИАЛИЗАЦИЯ
            Emit(0x3E, 0x00, 0xD3, 0x00); // Фон черный
            Emit(0x3E, 0x01, 0xD3, 0x01); // Цвет синий/голубой
            Emit(0x3E, 0x00, 0xD3, 0x10); // Скролл 0

            // Очистка экрана (0x1800 - 0x37FF)
            Emit(0x21, 0x00, 0x18, 0x11, 0x00, 0x20, 0xAF);
            ushort cLoop = (ushort)(0x0100 + code.Count);
            Emit(0x77, 0x23, 0x1B, 0x7A, 0xB3, 0xC2, (byte)cLoop, (byte)(cLoop >> 8));

            // Начальные координаты
            Emit(0x3E, 0x0F, 0x32, (byte)varX, (byte)(varX >> 8)); // X = 15
            Emit(0x3E, 0x80, 0x32, (byte)varY, (byte)(varY >> 8)); // Y = 128

            // 2. ГЛАВНЫЙ ЦИКЛ
            ushort mainLoop = (ushort)(0x0100 + code.Count);

            // Стираем (C = 0)
            Emit(0x0E, 0x00);
            Emit(0xCD, 0x80, 0x01); // Вызов Draw (лежит ниже по 0x0180)

            // Опрос клавиатуры
            Emit(0x3E, 0xFE, 0xD3, 0x01, 0xDB, 0x01, 0x2F, 0x47); // Читаем в B

            // Проверка влево (бит 1)
            Emit(0x78, 0xE6, 0x02, 0xCA);
            int skipLeftIdx = code.Count; Emit(0, 0); // Заполнитель адреса
            Emit(0x3A, (byte)varX, (byte)(varX >> 8), 0x3D, 0xE6, 0x1F, 0x32, (byte)varX, (byte)(varX >> 8));
            ushort noLeft = (ushort)(0x0100 + code.Count);
            code[skipLeftIdx] = (byte)noLeft; code[skipLeftIdx + 1] = (byte)(noLeft >> 8);

            // Проверка вправо (бит 0)
            Emit(0x78, 0xE6, 0x01, 0xCA);
            int skipRightIdx = code.Count; Emit(0, 0);
            Emit(0x3A, (byte)varX, (byte)(varX >> 8), 0x3C, 0xE6, 0x1F, 0x32, (byte)varX, (byte)(varX >> 8));
            ushort noRight = (ushort)(0x0100 + code.Count);
            code[skipRightIdx] = (byte)noRight; code[skipRightIdx + 1] = (byte)(noRight >> 8);

            // Рисуем (C = FF)
            Emit(0x0E, 0xFF);
            Emit(0xCD, 0x80, 0x01);

            // Пауза
            Emit(0x11, 0xFF, 0x03);
            ushort dLoop = (ushort)(0x0100 + code.Count);
            Emit(0x1B, 0x7A, 0xB3, 0xC2, (byte)dLoop, (byte)(dLoop >> 8));

            Emit(0xC3, (byte)mainLoop, (byte)(mainLoop >> 8));

            // 3. ПОДПРОГРАММА ОТРИСОВКИ (строго по адресу 0x0180)
            while (code.Count < 0x80) code.Add(0x00);

            // Считаем HL = 0x1800 + Y*32 + X
            Emit(0x3A, (byte)varY, (byte)(varY >> 8)); // A = Y
            Emit(0x6F, 0x26, 0x00);           // L=A, H=0 (HL = Y)
            Emit(0x29, 0x29, 0x29, 0x29, 0x29); // HL = HL * 32 (5 сдвигов влево)

            Emit(0x11, (byte)varX, (byte)(varX >> 8)); // Используем DE как временный адрес переменной X
            Emit(0x1A);                       // LD A, (DE) -> читаем само значение X
            Emit(0x5F, 0x16, 0x00);           // E=A, D=0
            Emit(0x19);                       // HL = HL + DE (теперь HL = Y*32 + X)

            Emit(0x11, 0x00, 0x18);           // DE = 0x1800 (база VRAM)
            Emit(0x19);                       // HL = HL + DE (финальный адрес в VRAM)

            // Рисуем полоску 8 пикселей высотой
            Emit(0x06, 0x08);                 // Счетчик строк
            ushort drawLoop = (ushort)(0x0100 + code.Count);
            Emit(0x79, 0x77);                 // A = C, (HL) = A
            Emit(0x11, 0x20, 0x00, 0x19);     // DE = 32, HL = HL + DE (на след. строку)
            Emit(0x05, 0xC2, (byte)drawLoop, (byte)(drawLoop >> 8));
            Emit(0xC9);                       // RET

            File.WriteAllBytes(Path.Combine(GetProjectPath(), "interactive.rom"), code.ToArray());
        }


        private void CreateSpriteAnimationRom()
        {
            var code = new List<byte>();
            void Emit(params byte[] b) => code.AddRange(b);

            // Адреса переменных в ОЗУ (выше видеопамяти)
            ushort varX = 0xC000;

            // 1. ИНИЦИАЛИЗАЦИЯ (0x0100)
            Emit(0x3E, 0x00, 0xD3, 0x00);     // OUT 00 - черный фон
            Emit(0x3E, 0x01, 0xD3, 0x01);     // OUT 01 - голубой цвет пикселей
            Emit(0x3E, 0x00, 0xD3, 0x10);     // OUT 10 - скролл = 0

            // Очистка экрана (заливка 0x00 с 0x1800 по 0x37FF)
            Emit(0x21, 0x00, 0x18);           // LXI H, 1800h
            Emit(0x11, 0x00, 0x20);           // LXI D, 2000h (размер VRAM)
            Emit(0xAF);                       // XRA A (A = 0)
            ushort clearLoop = (ushort)(0x0100 + code.Count);
            Emit(0x77, 0x23, 0x1B, 0x7A, 0xB3, 0xC2, (byte)clearLoop, (byte)(clearLoop >> 8));

            // Инициализация координаты X = 0
            Emit(0xAF);                       // XRA A
            Emit(0x32, (byte)varX, (byte)(varX >> 8));

            // 2. ГЛАВНЫЙ ЦИКЛ (MainLoop)
            ushort mainLoop = (ushort)(0x0100 + code.Count);

            // --- Стираем старый спрайт (пишем 0x00) ---
            Emit(0x0E, 0x00);                 // MVI C, 00h (цвет стирания)
            Emit(0xCD, 0x50, 0x01);           // CALL DrawSprite (адрес подпрограммы ниже)

            // --- Двигаем координату ---
            Emit(0x3A, (byte)varX, (byte)(varX >> 8)); // LDA varX
            Emit(0x3C);                       // INR A (X++)
            Emit(0xE6, 0x1F);                 // ANI 1Fh (ограничение 32 байта, т.к. 256/8=32)
            Emit(0x32, (byte)varX, (byte)(varX >> 8)); // STA varX

            // --- Рисуем новый спрайт (пишем 0xFF) ---
            Emit(0x0E, 0xFF);                 // MVI C, FFh (закрашенный спрайт)
            Emit(0xCD, 0x50, 0x01);           // CALL DrawSprite

            // --- Большая задержка ---
            Emit(0x06, 0x40);                 // MVI B, 40h
            ushort delayOuter = (ushort)(0x0100 + code.Count);
            Emit(0x21, 0x00, 0x00);           // LXI H, 0000h
            ushort delayInner = (ushort)(0x0100 + code.Count);
            Emit(0x2B, 0x7C, 0xB5, 0xC2, (byte)delayInner, (byte)(delayInner >> 8)); // DCX H / MOV A,H / ORA L / JNZ
            Emit(0x05, 0xC2, (byte)delayOuter, (byte)(delayOuter >> 8)); // DCR B / JNZ

            Emit(0xC3, (byte)mainLoop, (byte)(mainLoop >> 8)); // JMP MainLoop

            // 3. ПОДПРОГРАММА DrawSprite (0x0150)
            // Рисует блок 8x8 по адресу: 0x1800 + (Y * 32) + X_byte
            // Для простоты возьмем Y = 120 (середина экрана)
            // 120 * 32 = 3840 (0x0F00). Адрес = 0x1800 + 0x0F00 = 0x2700
            while (code.Count < 0x50) code.Add(0x00); // Выравнивание до 0x0150

            Emit(0x3A, (byte)varX, (byte)(varX >> 8)); // LDA varX
            Emit(0x5F, 0x16, 0x27);           // MOV E,A / MVI D, 27h (DE = адрес в VRAM)
            Emit(0x06, 0x08);                 // MVI B, 8 (8 строк высоты)

            ushort drawLineLoop = (ushort)(0x0100 + code.Count);
            Emit(0x79);                       // MOV A, C (цвет из регистра C)
            Emit(0x12);                       // STAX D (запись в VRAM)
            Emit(0xEB, 0x21, 0x20, 0x00, 0x19, 0xEB); // XCHG / LXI H,32 / DAD D / XCHG (DE += 32)
            Emit(0x05, 0xC2, (byte)drawLineLoop, (byte)(drawLineLoop >> 8)); // DCR B / JNZ
            Emit(0xC9);                       // RET

            var romData = code.ToArray();
            string path = Path.Combine(GetProjectPath(), "sprite_anim.rom");
            File.WriteAllBytes(path, romData);
            DebugLog($"Анимация создана: {path}");
        }


        private void CreateChessRom()
        {
            var code = new List<byte>();

            void Emit(params byte[] bytes) => code.AddRange(bytes);

            // Инициализация
            Emit(0x3E, 0x02, 0xD3, 0x00);     // OUT 00 - чёрный фон
            Emit(0x3E, 0x05, 0xD3, 0x01);     // OUT 01 - голубой цвет (индекс 3 = Cyan)
            Emit(0x3E, 0x00, 0xD3, 0x10);     // OUT 10 - скролл = 0

            Emit(0x21, 0x00, 0x18);           // LXI H, 1800h

            // === Основной цикл: 256 строк ===
            Emit(0x16, 0x00);                 // D = 0  (счётчик строк, будет 256 итераций)

            ushort rowLoopAddr = (ushort)(0x0100 + code.Count);

            // Одна строка: 4 блока (FF FF FF FF 00 00 00 00) × 4 = 32 байта
            Emit(0x06, 0x04);                 // B = 4 блока

            ushort blockLoopAddr = (ushort)(0x0100 + code.Count);

            Emit(0x3E, 0xFF);                 // A = FFh
            for (int i = 0; i < 4; i++) Emit(0x77, 0x23);   // 4 раза (MOV M,A / INX H)

            Emit(0x3E, 0x00);                 // A = 00h
            for (int i = 0; i < 4; i++) Emit(0x77, 0x23);

            Emit(0x05);                       // DCR B
            Emit(0xC2, (byte)blockLoopAddr, (byte)(blockLoopAddr >> 8)); // JNZ blockLoop

            // Следующая строка — инвертируем паттерн (чередование рядов)
            Emit(0x7E);                       // MOV A,M     (берём первый байт текущей строки)
            Emit(0xEE, 0xFF);                 // XRI FFh
            Emit(0x32, 0x00, 0x18);           // STA 1800h   (сохраняем для следующей строки)

            Emit(0x15);                       // DCR D
            Emit(0xC2, (byte)rowLoopAddr, (byte)(rowLoopAddr >> 8)); // JNZ rowLoop

            // Бесконечный цикл
            Emit(0xC3, 0x2E, 0x01);           // JMP $  (0x012E примерно)

            var romData = code.ToArray();

            string path = Path.Combine(GetProjectPath(), "test_chess.rom");
            File.WriteAllBytes(path, romData);

            DebugLog($"Chess ROM создан: {romData.Length} байт");
            DebugLog($"rowLoopAddr = 0x{rowLoopAddr:X4}, blockLoopAddr = 0x{blockLoopAddr:X4}");
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


        private void CreatePatternTestRom()
        {
            var code = new List<byte>();
            void Emit(params byte[] b) => code.AddRange(b);

            // Устанавливаем красный фон и белые пиксели для контраста
            Emit(0x3E, 0x04);        // MVI A, 4 (красный)
            Emit(0xD3, 0x00);        // OUT 0x00

            Emit(0x3E, 0x07);        // MVI A, 7 (белый)
            Emit(0xD3, 0x01);        // OUT 0x01

            // Заполняем первые 16 байт видеопамяти паттерном 0xFF
            Emit(0x21, 0x00, 0x18);  // LXI H, 0x1800
            Emit(0x36, 0xFF);        // MVI M, 0xFF
            Emit(0x2C);              // INR L
            Emit(0x36, 0xFF);        // MVI M, 0xFF
            Emit(0x2C);              // INR L
            Emit(0x36, 0xFF);        // MVI M, 0xFF
            Emit(0x2C);              // INR L
            Emit(0x36, 0xFF);        // MVI M, 0xFF

            // HLT
            Emit(0x76);

            var romData = code.ToArray();
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pattern_test.rom");
            File.WriteAllBytes(path, romData);

            DebugLog($"\n=== PATTERN TEST ROM ===");
            for (int i = 0; i < romData.Length; i += 8)
            {
                string hex = BitConverter.ToString(romData, i, Math.Min(8, romData.Length - i)).Replace("-", " ");
                DebugLog($"0x{(0x0100 + i):X4}: {hex}");
            }
        }

        private void CreateSimpleDebugRom()
        {
            var code = new List<byte>();
            void Emit(params byte[] b) => code.AddRange(b);

            // Максимально простая программа

            // 0x0100: MVI A, 0x00
            Emit(0x3E, 0x00);

            // 0x0102: OUT 0x00
            Emit(0xD3, 0x00);

            // 0x0104: MVI A, 0x03
            Emit(0x3E, 0x03);

            // 0x0106: OUT 0x01
            Emit(0xD3, 0x01);

            // 0x0108: LXI H, 0x1800
            Emit(0x21, 0x00, 0x18);

            // 0x010B: MVI M, 0xFF
            Emit(0x36, 0xFF);

            // 0x010D: HLT
            Emit(0x76);

            var romData = code.ToArray();
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "simple_debug.rom");
            File.WriteAllBytes(path, romData);

            DebugLog($"\n=== SIMPLE DEBUG ROM ===");
            DebugLog($"Path: {path}");
            DebugLog($"Size: {romData.Length} bytes");

            // Выводим дамп
            for (int i = 0; i < romData.Length; i += 8)
            {
                string hex = BitConverter.ToString(romData, i, Math.Min(8, romData.Length - i)).Replace("-", " ");
                DebugLog($"0x{(0x0100 + i):X4}: {hex}");
            }
        }

        private void CreateDebugRom()
        {
            var code = new List<byte>();
            void Emit(params byte[] b) => code.AddRange(b);

            // ============================================
            // ПРОСТЕЙШАЯ ОТЛАДОЧНАЯ ПРОГРАММА
            // Без циклов, просто последовательные инструкции
            // ============================================

            // 0x0100: MVI A, 0x00
            Emit(0x3E, 0x00);        // MVI A, 0

            // 0x0102: OUT 0x00 - чёрный фон
            Emit(0xD3, 0x00);        // OUT 0x00

            // 0x0104: MVI A, 0x03
            Emit(0x3E, 0x03);        // MVI A, 3

            // 0x0106: OUT 0x01 - голубой цвет пикселей
            Emit(0xD3, 0x01);        // OUT 0x01

            // 0x0108: LXI H, 0x1800
            Emit(0x21, 0x00, 0x18);  // LXI H, 0x1800

            // 0x010B: MVI M, 0xFF - записать FF в видеопамять
            Emit(0x36, 0xFF);        // MVI M, 0xFF

            // 0x010D: MVI A, 0xAA - тестовое значение
            Emit(0x3E, 0xAA);        // MVI A, 0xAA

            // 0x010F: HLT - останов (чтобы видеть результат)
            Emit(0x76);              // HLT

            var romData = code.ToArray();
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.rom");
            File.WriteAllBytes(path, romData);

            DebugLog($"=== DEBUG ROM CREATED ===");
            DebugLog($"Path: {path}");
            DebugLog($"Size: {romData.Length} bytes");
            DebugLog($"Expected: Black background, blue pixel column at left edge, then HALT");

            // Выводим дамп ROM
            DebugLog("\nROM dump:");
            for (int i = 0; i < romData.Length; i += 8)
            {
                string hex = BitConverter.ToString(romData, i, Math.Min(8, romData.Length - i)).Replace("-", " ");
                DebugLog($"0x{(0x0100 + i):X4}: {hex}");
            }
        }

        private void CreateDebugRom_old()
        {
            var code = new List<byte>();
            void Emit(params byte[] b) => code.AddRange(b);

            // Самая простая программа - просто устанавливает цвет и заливает 1 строку
            Emit(0x3E, 0x00);        // MVI A, 0
            Emit(0xD3, 0x00);        // OUT 0x00 - чёрный фон

            Emit(0x3E, 0x03);        // MVI A, 3
            Emit(0xD3, 0x01);        // OUT 0x01 - голубой цвет

            // Записываем 0xFF в первый байт видеопамяти
            Emit(0x21, 0x00, 0x18);  // LXI H, 0x1800
            Emit(0x36, 0xFF);        // MVI M, 0xFF

            // Бесконечный цикл
            Emit(0xC3, 0x0C, 0x01);  // JMP 0x010C

            var romData = code.ToArray();
            string path = Path.Combine(GetProjectPath(), "debug.rom");
            File.WriteAllBytes(path, romData);

            DebugLog($"Debug ROM created: {path}");
            DebugLog("Expected: One pixel column (8 pixels wide) at left edge");
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


        private void CheckVideoMemoryButton_Click(object? sender, EventArgs e)
        {
            DebugLog("=== VIDEO MEMORY DIAGNOSTIC ===");

            // Проверяем адресацию видеопамяти
            DebugLog($"Video RAM range: 0x1800 - 0x37FF");

            // Проверяем, что можем записать и прочитать
            ushort testAddr = 0x1800;
            byte original = emulator.Memory.Read(testAddr);
            emulator.Memory.Write(testAddr, 0xAA);
            byte testRead = emulator.Memory.Read(testAddr);
            DebugLog($"Write/Read test at 0x{testAddr:X4}: wrote 0xAA, read 0x{testRead:X2} (OK: {testRead == 0xAA})");
            emulator.Memory.Write(testAddr, original);

            // Дамп первых строк видеопамяти
            emulator.Video.DumpVideoRam(0, 5);

            // Проверяем конкретные пиксели
            emulator.Video.CheckPixel(40, 100);
            emulator.Video.CheckPixel(41, 100);
            emulator.Video.CheckPixel(42, 100);

            // Проверяем состояние портов
            DebugLog($"Port 0x00 (border color): 0x{emulator.IOBus.In(0x00):X2}");
            DebugLog($"Port 0x01 (palette): 0x{emulator.IOBus.In(0x01):X2}");
            DebugLog($"Port 0x10 (scroll): 0x{emulator.IOBus.In(0x10):X2}");

            DebugLog("=== END DIAGNOSTIC ===");
        }

        private void DebugMemoryDump(ushort start, ushort count)
        {
            DebugLog($"\n=== MEMORY DUMP 0x{start:X4} - 0x{start + count:X4} ===");
            for (ushort addr = start; addr < start + count; addr += 16)
            {
                string hex = "";
                for (int i = 0; i < 16 && addr + i < start + count; i++)
                {
                    hex += $"{emulator.Memory.Read((ushort)(addr + i)):X2} ";
                }
                DebugLog($"0x{addr:X4}: {hex}");
            }
        }
    }
}