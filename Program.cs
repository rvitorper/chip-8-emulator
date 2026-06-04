using chip_8_emulator;
using Raylib_cs;

Raylib.InitWindow(640, 320, "Chip 8 Emulator");
var emulator = new Emulator();
emulator.Load("/Users/ramon/Documents/chip-8-emulator/3-corax+.ch8");
while (!Raylib.WindowShouldClose())
{
    emulator.Loop();
}
Raylib.CloseWindow();