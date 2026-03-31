using System.IO;

namespace Vector06cEmulator
{
    class Program
    {
        static void Main()
        {
            saveTests();

            var emu = new Emulator();

            //emu.LoadRom("counter_0_9.bin");
            //emu.LoadRom("counter_2.bin");
            //emu.LoadRom("counter_2.bin");
            emu.LoadRom("factorial2.bin");
            emu.Run();

            Console.ReadLine();
        }

        static void saveTests()
        {
            string path = "test.bin";
            if (!File.Exists(path))
            {
                byte[] testProgram = new byte[]
                {
                    0x3E, 0x05,        // MVI A, 0x05
                    0x21, 0x00, 0x80,  // LXI H, 0x8000
                    0x32, 0x00, 0x80,  // STA 0x8000
                    0x34,              // INR M
                    0x76               // HLT
                };
            }


            path = "counter_0_9.bin";
            if (!File.Exists(path))
            {
                // Счётчик от 0 до 9
                byte[] program = new byte[]
                {
                    0x3E, 0x00,        // MVI A, 0     ; A = 0
                    0x21, 0x00, 0x80,  // LXI H, 8000  ; HL = 0x8000 (ячейка памяти)
                    0x77,              // MOV M, A     ; mem[8000] = A
    
                    // loop:
                    0x3C,              // INR A        ; A++
                    0xFE, 0x0A,        // CPI 10       ; A == 10?
                    0xC2, 0x05, 0x00,  // JNZ loop     ; если нет — повторить (адрес 0x0005)
    
                    0x76               // HLT
                };

                File.WriteAllBytes(path, program);
            }

            path = "counter_1_5.bin";
            if (!File.Exists(path))
            {
                // Сумма чисел от 1 до 5
                byte[] program = new byte[]
                {
                    0x3E, 0x00,        // MVI A, 0     ; сумма = 0
                    0x06, 0x01,        // MVI B, 1     ; счётчик = 1
    
                    // loop:
                    0x80,              // ADD B         ; A += B
                    0x04,              // INR B         ; B++
                    0x78,              // MOV A, B      ; временно A = B для сравнения
                    0xFE, 0x06,        // CPI 6         ; B == 6?
                    0x78,              // MOV A, B      ; восстановим... 
    
                    // ^ неудобно — лучше через регистры по-другому
                    0x76               // HLT
                };

                File.WriteAllBytes(path, program);
            }

            path = "counter_2.bin";
            if (!File.Exists(path))
            {
                byte[] program = new byte[]
                {
                    0x3E, 0x00,        // MVI A, 0     ; A = сумма
                    0x06, 0x05,        // MVI B, 5     ; B = счётчик (5 итераций)

                    // loop (адрес 0x0004):
                    0x80,              // ADD B        ; A += B
                    0x05,              // DCR B        ; B--
                    0xC2, 0x04, 0x00,  // JNZ loop    ; пока B != 0
    
                    0x32, 0x00, 0x80,  // STA 0x8000  ; сохранить результат
                    0x76               // HLT
                };
                // A = 5+4+3+2+1 = 15 = 0x0F

                File.WriteAllBytes(path, program);
            }

            path = "factorial.bin";
            if (!File.Exists(path))
            {
                // Factorial
                byte[] program = new byte[]
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
                    0x7B,              // 000C: MOV A, E   ; <-- нет, нужно восстановить A...
                };

                File.WriteAllBytes(path, program);
            }

            path = "factorial2.bin";
            if (!File.Exists(path))
            {
                byte[] program = new byte[]
                {
                    // 5! через стек для сохранения накопителя
                    // B = multiplier, C = result, D = inner counter

                    0x06, 0x05,        // 0000: MVI B, 5
                    0x0E, 0x01,        // 0002: MVI C, 1

                    // outer_loop (0x0004):
                    0x59,              // 0004: MOV E, C    ; E = старый result
                    0x16, 0x00,        // 0005: MVI D, 0    ; D = inner counter
                    0x0E, 0x00,        // 0007: MVI C, 0    ; C = новый накопитель

                    // inner_loop (0x0009):
                    0x79,              // 0009: MOV A, C    ; A = накопитель
                    0x83,              // 000A: ADD E       ; A += E (старый result)
                    0x4F,              // 000B: MOV C, A    ; C = новый накопитель
                    0x14,              // 000C: INR D       ; D++
                    0x7A,              // 000D: MOV A, D    ; A = D
                    0xB8,              // 000E: CMP B       ; D == B?
                    0xC2, 0x09, 0x00,  // 000F: JNZ inner_loop (0x0009)

                    // C теперь = старый_C * B
                    0x05,              // 0012: DCR B       ; B--
                    0x3E, 0x01,        // 0013: MVI A, 1
                    0xB8,              // 0015: CMP B       ; B == 1?
                    0xC2, 0x04, 0x00,  // 0016: JNZ outer_loop (0x0004)

                    // Результат в C
                    0x79,              // 0019: MOV A, C
                    0x32, 0x00, 0x80,  // 001A: STA 0x8000  ; сохранить
                    0x76               // 001D: HLT
                };

                File.WriteAllBytes(path, program);

            }
        }
    }
}