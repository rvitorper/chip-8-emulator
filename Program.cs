using System.Numerics;
using chip_8_emulator;
using Raylib_cs;

void Draw(byte[] gfx, Texture2D texture2D)
{
    Color[] pixelData = new Color[64 * 32];
    for (int i = 0; i < gfx.Length; i++)
    {
        pixelData[i] = Color.Black;
        if (gfx[i] > 0)
        {
            pixelData[i] = Color.White;
        }
    }
    Raylib.UpdateTexture(texture2D, pixelData);
    Raylib.BeginDrawing();
    Raylib.ClearBackground(Color.Black);
    Rectangle source = new Rectangle(0, 0, 64, 32);
    Rectangle target = new Rectangle(0, 0, 640, 320);
    Raylib.DrawTexturePro(texture2D, source, target, new Vector2(0.0f, 0.0f), 0.0f, Color.White);

    Raylib.EndDrawing();
}

Raylib.InitWindow(640, 320, "Chip 8 Emulator");
Raylib.SetTargetFPS(60);
var emulator = new Emulator();
emulator.Load("/Users/ramon/Documents/chip-8-emulator/5-quirks.ch8");
Image image = Raylib.GenImageColor(64, 32, Color.Black);
var texture = Raylib.LoadTextureFromImage(image);
Raylib.UnloadImage(image);

void SetKeys(Emulator emulator)
{
    var keyBindings =
        new List<(KeyboardKey, ushort)>{
            (KeyboardKey.One, 0x1),
            (KeyboardKey.Two, 0x2),
            (KeyboardKey.Three, 0x3),
            (KeyboardKey.Four, 0xC),
            (KeyboardKey.Q, 0x4),
            (KeyboardKey.W, 0x5),
            (KeyboardKey.E, 0x6),
            (KeyboardKey.R, 0xD),
            (KeyboardKey.A, 0x7),
            (KeyboardKey.S, 0x8),
            (KeyboardKey.D, 0x9),
            (KeyboardKey.F, 0xE),
            (KeyboardKey.Z, 0xA),
            (KeyboardKey.X, 0x0),
            (KeyboardKey.C, 0xB),
            (KeyboardKey.V, 0xF),
        };
    foreach (var keyBinding in keyBindings)
    {
        if (Raylib.IsKeyDown(keyBinding.Item1))
        {
            emulator.SetKey(keyBinding.Item2, 1);
        }
        else
        {
            emulator.SetKey(keyBinding.Item2, 0);
        }
    }
}

while (!Raylib.WindowShouldClose())
{
    for (int i = 0; i < 20; i++)
    {
        emulator.EmulateCycle();
    }
    emulator.DecreaseTimers();
    if (emulator.GetDrawFlag())
    {
        Draw(emulator.GetGraphics(), texture);
    }

    SetKeys(emulator);
}
Raylib.CloseWindow();