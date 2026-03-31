using System.IO;

namespace Vector06cEmulator
{
    class Program
    {
        static void Main()
        {
            SaveTests();

            var emu = new Emulator();

            //emu.LoadRom("counter_0_9.bin");
            //emu.LoadRom("counter_2.bin");
            //emu.LoadRom("counter_2.bin");
            //emu.LoadRom("factorial2.bin");
            emu.LoadRom("binland.rom");
            emu.Run();

            Console.ReadLine();
        }

        static void SaveTests()
        {
            var tests = new Dictionary<string, byte[]>
            {
                ["test.bin"] = new byte[]
                {
                    0x3E, 0x05,        // MVI A, 05h
                    0x21, 0x00, 0x80,  // LXI H, 8000h
                    0x32, 0x00, 0x80,  // STA 8000h
                    0x34,              // INR M
                    0x76               // HLT
                },

                // Счётчик от 0 до 9
                ["counter_0_9.bin"] = new byte[]
                {
                    0x3E, 0x00,        // MVI A, 0
                    0x21, 0x00, 0x80,  // LXI H, 8000h
                    0x77,              // MOV M, A

                    // loop:
                    0x3C,              // INR A
                    0xFE, 0x0A,        // CPI 0Ah
                    0xC2, 0x05, 0x00,  // JNZ 0005h

                    0x76               // HLT
                },

                // Сумма чисел от 1 до 5
                ["counter_2.bin"] = new byte[]
                {
                    0x3E, 0x00,        // MVI A, 0       ; A = сумма
                    0x06, 0x05,        // MVI B, 5       ; B = счётчик

                    // loop (0004h):
                    0x80,              // ADD B          ; A += B
                    0x05,              // DCR B          ; B--
                    0xC2, 0x04, 0x00,  // JNZ 0004h      ; пока B != 0

                    0x32, 0x00, 0x80,  // STA 8000h      ; сохранить результат
                    0x76               // HLT
                },

                ["factorial.bin"] = new byte[]
                { 
                    // Вычисляем 5! = 5×4×3×2×1 = 120 = 0x78
                    // Алгоритм:
                    //   result = 1
                    //   multiplier = 5
                    //   пока multiplier > 1:
                    //       result = result * multiplier  (через сложение)
                    //       multiplier--

                    // Регистры:
                    //   B = multiplier (5,4,3,2,1)
                    //   C = result (текущий)
                    //   A = аккумулятор для умножения
                    //   D = счётчик внутреннего цикла

                    0x06, 0x05,        // 0000: MVI B, 5   ; multiplier = 5
                    0x0E, 0x01,        // 0002: MVI C, 1   ; result = 1

                    // outer_loop (адрес 0x0004):
                    // Умножаем C * B → результат в A
                    // inner: A = 0; repeat B times: A += C
                    0x3E, 0x00,        // 0004: MVI A, 0   ; A = 0 (накопитель)
                    0x16, 0x00,        // 0006: MVI D, 0   ; D = счётчик = 0

                    // inner_loop (адрес 0x0008):
                    0x81,              // 0008: ADD C      ; A += C
                    0x14,              // 0009: INR D      ; D++
                    0x7A,              // 000A: MOV A, D   ; временно A = D для сравнения с B
                    0xB8,              // 000B: CMP B      ; D == B?
                    0x7B               // 000C: MOV A, E   ; <-- нет, нужно восстановить A...
                },

                ["factorial2.bin"] = new byte[]
                {
                    // 5! = 120 (78h)
                    // B = multiplier
                    // C = result
                    // D = inner counter
                    // E = временное хранение

                    0x06, 0x05,        // 0000: MVI B, 5
                    0x0E, 0x01,        // 0002: MVI C, 1

                    // outer_loop (0004h):
                    0x59,              // 0004: MOV E, C      ; сохранить старый result
                    0x16, 0x00,        // 0005: MVI D, 0      ; D = 0
                    0x0E, 0x00,        // 0007: MVI C, 0      ; новый result

                    // inner_loop (0009h):
                    0x79,              // 0009: MOV A, C
                    0x83,              // 000A: ADD E         ; A += старый result
                    0x4F,              // 000B: MOV C, A      ; сохранить
                    0x14,              // 000C: INR D
                    0x7A,              // 000D: MOV A, D
                    0xB8,              // 000E: CMP B
                    0xC2, 0x09, 0x00,  // 000F: JNZ 0009h

                    // завершили умножение
                    0x05,              // 0012: DCR B
                    0x3E, 0x01,        // 0013: MVI A, 1
                    0xB8,              // 0015: CMP B
                    0xC2, 0x04, 0x00,  // 0016: JNZ 0004h

                    // результат в C
                    0x79,              // 0019: MOV A, C
                    0x32, 0x00, 0x80,  // 001A: STA 8000h
                    0x76               // 001D: HLT
                }
            };

            foreach (var test in tests)
            {
                SaveTest(test.Key, test.Value);
            }
        }

        static void SaveTest(string fileName, byte[] data)
        {
            if (File.Exists(fileName))
                return;

            File.WriteAllBytes(fileName, data);
        }

    }
}