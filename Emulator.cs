using System.Numerics;
using Raylib_cs;

namespace chip_8_emulator;

public class Emulator
{
    private byte[] chip8FontSet = new byte[80];
    
    private ushort opcode;
    private byte[] memory = new byte[4096];
    private byte[] registers = new byte[16];
    private ushort index;
    private ushort programCounter;
    private byte[] gfx = new byte[64 * 32];
    private byte delayTimer;
    private byte soundTimer;
    private ushort[] stack = new ushort[16];
    private ushort stackPointer;
    private byte[] keyPad  = new byte[16];
    private bool drawFlag = false;
    private Texture2D texture;

    public Emulator()
    {
        SetupGraphics();
        Initialize();
    }

    private void Initialize()
    {
        programCounter = 0x200;
        opcode = 0;
        index = 0;
        stackPointer = 0;
        
        ClearScreen();

        for (int i = 0; i < stack.Length; i++)
        {
            stack[i] = 0;
        }

        for (int i = 0; i < registers.Length; i++)
        {
            registers[i] = 0;
        }

        for (int i = 0; i < memory.Length; i++)
        {
            memory[i] = 0;
        }

        for (int i = 0; i < 80; i++)
        {
            memory[i] = chip8FontSet[i];
        }

        delayTimer = 0;
        soundTimer = 0;
    }

    private void SetupInput()
    {
        throw new NotImplementedException();
    }

    private void SetupGraphics()
    {
        Image image = Raylib.GenImageColor(64, 32, Color.Black);
        texture = Raylib.LoadTextureFromImage(image);
        Raylib.UnloadImage(image);
    }

    public void Load(string rom)
    {
        byte[] buffer = File.ReadAllBytes(rom);
        var bufferSize = buffer.Length;
        for (int i = 0; i < bufferSize; i++)
        {
            memory[i + 0x200] = buffer[i];
        }
    }
    public void Loop()
    {
        EmulateCycle();
        if (drawFlag)
        {
            DrawGraphics();
        }

        SetKeys();
    }

    private void SetKeys()
    {
        
    }

    private void DrawGraphics()
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
        Raylib.UpdateTexture(texture, pixelData);
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);
        Rectangle source = new Rectangle(0, 0, 64, 32);
        Rectangle target = new Rectangle(0, 0, 640, 320);
        Raylib.DrawTexturePro(texture, source, target, new Vector2(0.0f, 0.0f), 0.0f, Color.White);

        Raylib.EndDrawing();
    }

    private void EmulateCycle()
    {
        opcode = (ushort) (memory[programCounter] << 8 | memory[programCounter + 1]);
        switch (opcode & 0xF000)
        {
            case 0x0000:
                HandleNilXor();
                break;
            case 0x1000:
                ushort jumpTarget = GetTrailingThreeNibbles(opcode);
                JumpTo(jumpTarget);
                break;
            case 0x2000:
                ushort target = GetTrailingThreeNibbles(opcode);
                CallSubroutine(target);
                NextInstruction();
                break;
            case 0x3000:
                SkipInstructionIf(GetXValue(opcode) == GetTrailingByte(opcode));
                NextInstruction();
                break;
            case 0x4000:
                SkipInstructionIf(GetXValue(opcode) != GetTrailingByte(opcode));
                NextInstruction();
                break;
            case 0x5000:
                SkipInstructionIf(GetXValue(opcode) == GetYValue(opcode));
                NextInstruction();
                break;
            case 0x6000:
                SetXValue(opcode, (byte)GetTrailingByte(opcode));
                NextInstruction();
                break;
            case 0x7000:
                IncrementXValue(opcode, (byte)GetTrailingByte(opcode));
                NextInstruction();
                break;
            case 0x8000:
                HandleEightXor();
                break;
            case 0x9000:
                SkipInstructionIf(GetXValue(opcode) != GetYValue(opcode));
                NextInstruction();
                break;
            case 0xA000:
                index = GetTrailingThreeNibbles(opcode);
                NextInstruction();
                break;
            case 0xB000:
                JumpTo((ushort)((byte)GetTrailingByte(opcode) + GetXValue(0x0000)));
                NextInstruction();
                break;
            case 0xC000:
                var randomInBytes = (byte)Random.Shared.Next();
                SetXValue(opcode, (byte)(randomInBytes & GetTrailingByte(opcode)));
                NextInstruction();
                break;
            case 0xD000:
                Draw(GetXValue(opcode), GetYValue(opcode), GetTrailingNibble(opcode));
                NextInstruction();
                break;
            default:
                Console.WriteLine("Unknown opcode 0x" + opcode.ToString("X4"));
                break;
        }

        if (delayTimer > 0)
        {
            delayTimer--;
        }

        if (soundTimer > 0)
        {
            if (soundTimer == 1)
            {
                Console.WriteLine("BEEP");
            }

            soundTimer--;
        }
        
        
    }

    private void Draw(byte x, byte y, ushort numPixels)
    {
        SetVFValue(0);
        for (var i = 0; i < numPixels; i++)
        {
            var memoryPixel = memory[index + i];
            for (int j = 0; j < 8; j++)
            {
                var offset = x + 64 * y + j + i * 64;
                var bitmask = (1 >> j);
                byte masked = (byte)(memoryPixel & bitmask);
                var changed = gfx[offset] ^ masked;
                SetVFValue((byte)(GetVFValue() | changed));
                gfx[offset] = masked;
            }
        }

        drawFlag = true;
    }

    private void HandleEightXor()
    {
        switch (opcode & 0x000F)
        {
            case 0x0000:
                SetXValue(opcode, GetYValue(opcode));
                NextInstruction();
                break;
            case 0x1000:
                SetXValue(opcode, (byte)(GetXValue(opcode) | GetYValue(opcode)));
                NextInstruction();
                break;
            case 0x2000:
                SetXValue(opcode, (byte)(GetXValue(opcode) & GetYValue(opcode)));
                NextInstruction();
                break;
            case 0x3000:
                SetXValue(opcode, (byte)(GetXValue(opcode) ^ GetYValue(opcode)));
                NextInstruction();
                break;
            case 0x4000:
                SetXValue(opcode, (byte)(GetXValue(opcode) + GetYValue(opcode)));
                CheckCarry(GetXValue(opcode), GetYValue(opcode));
                NextInstruction();
                break;
            case 0x5000:
                SetXValue(opcode, (byte)(GetXValue(opcode) + GetYValue(opcode)));
                CheckUnderflow(GetXValue(opcode), GetYValue(opcode));
                NextInstruction();
                break;
            case 0x6000:
                SetXValue(opcode, (byte)(GetXValue(opcode) + GetYValue(opcode)));
                CheckUnderflow(GetXValue(opcode), GetYValue(opcode));
                NextInstruction();
                break;
            default:
                Console.WriteLine("Unknown opcode in Eighth Set 0x" + opcode.ToString("X4"));
                break;
        }
    }

    private void CheckUnderflow(byte a, byte b)
    {
        if (a >= b)
        {
            registers[15] = 1;
            return;
        }
        registers[15] = 0;
    }

    private void CheckCarry(byte a, byte b)
    {
        var xor = a ^ b;
        var and = a & b;
        if ( xor == 0 && and == 0xFF)
        {
            registers[15] = 1;
            return;
        }
        registers[15] = 0;
    }

    private void HandleNilXor()
    {
        switch(opcode & 0x000F)
        {
            case 0x0000: // 0x00E0: Clears the screen        
                ClearScreen();
                NextInstruction();
                break;
            case 0x000E: // 0x00EE: Returns from subroutine          
                Return();
                NextInstruction();
                break;
            default:
                Console.WriteLine("Unknown opcode [0x0000]: 0x" +  opcode.ToString("X4"));
                break;
        }
    }

    private void CallSubroutine(ushort N)
    {
        stack[stackPointer] = programCounter;
        stackPointer++;
        programCounter = N;
    }

    private void ClearScreen()
    {
        for (var i = 0; i < gfx.Length; i++)
        {
            gfx[i] = 0;
        }
    }

    private void JumpTo(ushort N)
    {
        programCounter = N;
    }

    private void Return()
    {
        stackPointer--;
        programCounter = stack[stackPointer];
    }

    private void NextInstruction()
    {
        programCounter += 2;
    }

    private void SkipInstructionIf(bool value)
    {
        if (value)
        {
            NextInstruction();
        }
    }
    
    private ushort GetTrailingThreeNibbles(ushort opcode)
    {
        return (ushort)(opcode & 0x0FFF);
    }
    
    private ushort GetTrailingByte(ushort opcode)
    {
        return (ushort)(opcode & 0x00FF);
    }

    private ushort GetTrailingNibble(ushort opcode)
    {
        return (ushort)(opcode & 0x000F);
    }
    
    private byte GetXValue(ushort opcode)
    {
        var index = MaskX(opcode);
        return registers[index];
    }

    private byte GetYValue(ushort opcode)
    {
        var index = MaskY(opcode);
        return registers[index];
    }

    private byte GetVFValue()
    {
        return registers[15];
    }

    private void SetXValue(ushort opcode, byte value)
    {
        var index = MaskX(opcode);
        registers[index] = value;
    }

    private void SetVFValue(byte value)
    {
        registers[15] = value;
    }

    private ushort MaskX(ushort opcode)
    {
        return (ushort)((opcode & 0x0F00) >> 8);
    }

    private ushort MaskY(ushort opcode)
    {
        return (ushort)((opcode & 0x00F0) >> 4);
    }

    private void IncrementXValue(ushort opcode, byte value)
    {
        SetXValue(opcode, (byte)(GetXValue(opcode) + value));
    }
}