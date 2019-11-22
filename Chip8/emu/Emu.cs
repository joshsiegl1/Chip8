using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8.emu
{
    public class Emu
    {
        //throwaway
        public bool finished = false;

        public bool DrawFlag = false;

        //These two variables pertain to the Fx0A instruction - Input is handled in Program.cs
        bool StopAndWaitForKeyPress = false;
        ushort KeyPressedRegister; 

        public const int width = 64;
        public const int height = 32;
        public const int scale = 1; 

        //current opcode
        ushort opcode;

        //CPU registers
        private ushort[] V = new ushort[16];

        //image pointer
        ushort I;

        //program counter
        ushort pc; 

        //4K memory
        private byte[] memory = new byte[4096];

        byte[] fontset = new byte[80]
        {
            0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
            0x20, 0x60, 0x20, 0x20, 0x70, // 1
            0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
            0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
            0x90, 0x90, 0xF0, 0x10, 0x10, // 4
            0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
            0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
            0xF0, 0x10, 0x20, 0x40, 0x40, // 7
            0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
            0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
            0xF0, 0x90, 0xF0, 0x90, 0x90, // A
            0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
            0xF0, 0x80, 0x80, 0x80, 0xF0, // C
            0xE0, 0x90, 0x90, 0x90, 0xE0, // D
            0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
            0xF0, 0x80, 0xF0, 0x80, 0x80  // F
        }; 

        //pixels (black and white only)
        byte[,] gfx = new byte[width, height];
        public byte[,] GFX { get { return gfx; } }

        public byte[] Keys = new byte[16]; 

        ushort delayTimer;
        ushort soundTimer;

        ushort[] stack = new ushort[16]; 
        byte sp;

        Random r;

        public Emu()
        {
            Loader loader = new Loader();
            byte[] romData = loader.LoadFile(); 

            for (int i = 0; i < romData.Length; i++)
            {
                memory[i + 0x200] = romData[i];
                Console.Write(memory[i + 0x200].ToString()); 
            }

            r = new Random(); 
        }

        public void Initialize()
        {
            pc = 0x200;
            opcode = 0;
            I = 0;
            sp = 0; 

            for (int i = 0; i < 80; i++)
            {
                memory[i] = fontset[i]; 
            }
        }

        public void ProcessCycle()
        {
            if (pc >= memory.Length - 2)
            {
                finished = true;
                return; 
            }

            //Fx0A calls for the stopage of the program until a key is pressed, 
            //that logic is implemented here. We simply don't run the processesor until 
            //we detect that a key was placed in the Keys array
            if (StopAndWaitForKeyPress)
            {
                for (int i = 0; i < Keys.Length; i++)
                {
                    if (Keys[i] == 1)
                    {
                        StopAndWaitForKeyPress = false;
                        V[KeyPressedRegister] = (ushort)i;
                        pc += 2; 
                        break; 
                    }
                }
                return;
            }

            //This simply takes two bytes and stores them in an unsigned short, the decimal value of that new value is irrelavent
            //We're only concerned with the bits
            opcode = (ushort)(memory[pc] << 8 | memory[pc + 1]);
            Console.Write(opcode); 

            //for a decimal like 224 (first opcode in the Paddles.ch8 rom), mapped to a 16 bit variable
            //would be 0000 0000 1110 0000 
            //so 0xF000 masks the first nibble in the first byte to check for zero,
            //In the Chip-8 EMU's case, the first nibble tells us what instruction subset we'll use
            // (0000 0000 1110 0000 & 1111 0000 0000 0000) masks to (0000 0000 0000 0000) or simply 0x0000 or 0
            switch(opcode & 0xF000)
            {
                //In the case of 224, a zero is found
                case 0x0000:
                    {
                        //Do a second mask on the opcode to get the second byte in the ushort
                        //There's only two values we care about E0 and EE (CLS and RET)
                        //This masks the last bit
                        var op = (opcode & 0x000F);
                        switch (op)
                        {
                            //So unltimately, in the case of 224, the first opcode in our ROM (Hex: 00E0)
                            //it maps to a CLS command, which is performed here
                            //We come to this conclusion by masking the first bit and the last bit which are both 0
                            //If you check the Chip-8 Instruction set, you'll see that CLS is the only opcode which leads and trails with a zero
                            case 0x0000:
                                //Clear Screen
                                for (int x = 0; x < width; x++)
                                {
                                    for (int y = 0; y < height; y++)
                                    {
                                        gfx[x, y] = 0; 
                                    }
                                }
                                DrawFlag = true; 
                                pc += 2;
                                break;
                            case 0x000E:
                                //Return from a subroutine
                                pc = stack[--sp];
                                break;
                            default:
                                //0nnn - ignored for modern implementations, see here: http://devernay.free.fr/hacks/chip8/C8TECH10.HTM#0nnn
                                break;
                        }
                        break;
                    }
                case 0x1000:
                    pc = (ushort)(opcode & 0x0FFF); 
                    break;
                case 0x2000:
                    stack[sp] = pc;
                    sp++; 
                    pc = (ushort)(opcode & 0x0FFF); 
                    break;
                case 0x4000:
                    {
                        ushort regIndex_X = (ushort)((opcode & 0x0F00) >> 8);
                        ushort kk = (ushort)(opcode & 0x00FF);
                        if (V[regIndex_X] == kk)
                        {
                            pc += 4;
                        }
                        else
                        {
                            pc += 2; 
                        }
                        break;
                    }
                case 0x6000:
                    {
                        ushort regIndex = (ushort)((opcode & 0x0F00) >> 8);
                        ushort val = (ushort)(opcode & 0x00FF);
                        V[regIndex] = val;
                        pc += 2;
                        break;
                    }
                case 0x7000:
                    {
                        ushort regIndex = (ushort)((opcode & 0x0F00) >> 8);
                        ushort val = (ushort)(opcode & 0x00FF);
                        V[regIndex] += val;
                        pc += 2;
                        break;
                    }
                case 0x8000:
                    {
                        ushort op = (ushort)(opcode & 0x000F);
                        switch (op)
                        {
                            case 0x0000:
                                {
                                    ushort regIndex_X = (ushort)((opcode & 0x0F00) >> 8);
                                    ushort regIndex_Y = (ushort)((opcode & 0x00F0) >> 4);
                                    V[regIndex_X] = V[regIndex_Y];
                                    pc += 2;
                                    break;
                                }
                            case 0x0001:
                                {
                                    ushort regIndex_X = (ushort)((opcode & 0x0F00) >> 8);
                                    ushort regIndex_Y = (ushort)((opcode & 0x00F0) >> 4);
                                    //This might not be right
                                    V[regIndex_X] = (ushort)(regIndex_X | regIndex_Y); 
                                    pc += 2; 
                                    break; 
                                }
                            case 0x0002:
                                {
                                    ushort regIndex_X = (ushort)((opcode & 0x0F00) >> 8);
                                    ushort regIndex_Y = (ushort)((opcode & 0x00F0) >> 4);
                                    V[regIndex_X] = (ushort)(regIndex_X & regIndex_Y); 
                                    pc += 2; 
                                    break; 
                                }
                            case 0x0003:
                                {
                                    ushort regIndex_X = (ushort)((opcode & 0x0F00) >> 8);
                                    ushort regIndex_Y = (ushort)((opcode & 0x00F0) >> 4);
                                    V[regIndex_X] = (ushort)(regIndex_X ^ regIndex_Y);
                                    pc += 2;
                                    break; 
                                }
                            case 0x0004:
                                {
                                    ushort regIndex_X = (ushort)((opcode & 0x0F00) >> 8);
                                    ushort regIndex_Y = (ushort)((opcode & 0x00F0) >> 4);
                                    var result = V[regIndex_X] + V[regIndex_Y]; 
                                    if (result > 255)
                                    {
                                        V[16] = 1; 
                                    }
                                    else
                                    {
                                        V[16] = 0; 
                                    }
                                    V[regIndex_X] = (ushort)(result & 0x00FF); 
                                    break; 
                                }
                            case 0x0005:
                                {

                                    break; 
                                }
                            case 0x0006:
                                {
                                    break; 
                                }
                            case 0x0007:
                                {
                                    break; 
                                }
                            case 0x000E:
                                {
                                    break; 
                                }
                        }
                        break; 
                    }
                case 0x9000:
                    {
                        ushort regIndex_X = (ushort)((opcode & 0x0F00) >> 8);
                        ushort regIndex_Y = (ushort)((opcode & 0x00F0) >> 4);
                        if (V[regIndex_X] != V[regIndex_Y])
                        {
                            pc += 4; 
                        }
                        else
                        {
                            pc += 2; 
                        }
                        break;
                    }
                case 0xA000:
                    I = (ushort)(opcode & 0x0FFF);
                    pc += 2; 
                    break;
                case 0xB000:
                    pc = (ushort)(opcode & 0x0FFF);
                    pc = (ushort)(pc + V[0]); 
                    break;
                case 0xC000:
                    {
                        ushort regIndex_X = (ushort)((opcode & 0x0F00) >> 8);
                        byte rnd = (byte)r.Next(255);
                        byte kk = (byte)(opcode & 0x00FF);
                        V[regIndex_X] = (ushort)(rnd & kk);
                        pc += 2; 
                        break;
                    }
                case 0xD000:
                    {
                        V[0xF] = 0;
                        ushort X = V[(opcode & 0x0F00) >> 8];
                        ushort Y = V[(opcode & 0x00F0) >> 4];
                        byte nBytes = (byte)(opcode & 0x000F);

                        for (int y = 0; y < nBytes; y++)
                        {
                            ushort sprite = memory[I + y]; 
                            for (int x = 0; x < 8; x++)
                            {
                                //loop through scale here, adding s to x and i
                                //for (int s = 0; s < scale; s++)
                                //Also multiply X and Y by scale

                                byte posX = (byte)((x + X) % width);
                                byte posY = (byte)((y + Y) % height); 

                                if ((sprite & (0x80 >> x)) != 0)
                                {
                                    if (gfx[X + x, Y + y] == 1)
                                        V[0xF] = 1;

                                    gfx[X + x, Y + y] ^= 1;
                                }
                            }
                        }

                        DrawFlag = true; 
                        pc += 2; 
                        break; 
                    }
                case 0xE000:
                    {
                        ushort op = (ushort)(opcode & 0x000F); 
                        switch (op)
                        {
                            case 0x000E:
                                {
                                    ushort regIndex_X = (ushort)((opcode & 0x0F00) >> 8); 
                                    if (Keys[V[regIndex_X]] == 1)
                                    {
                                        pc += 4; 
                                    }
                                    else
                                    {
                                        pc += 2; 
                                    }
                                    break; 
                                }
                            case 0x0001:
                                {
                                    ushort regIndex_X = (ushort)((opcode & 0x0F00) >> 8);
                                    if (Keys[V[regIndex_X]] == 0)
                                    {
                                        pc += 4;
                                    }
                                    else
                                    {
                                        pc += 2;
                                    }
                                    break; 
                                }
                        }
                        break; 
                    } 
                case 0xF000:
                    {
                        var op = (opcode & 0x00FF); 
                        switch (op)
                        {
                            case 0x0007:
                                {
                                    ushort regIndex_X = (ushort)((opcode & 0x0F00) >> 8);
                                    V[regIndex_X] = delayTimer;
                                    pc += 2;  
                                    break; 
                                }
                            case 0x000A:
                                {
                                    //Stop the proccessor and wait for a key press
                                    StopAndWaitForKeyPress = true;
                                    KeyPressedRegister = (ushort)((opcode & 0x0F00) >> 8); 
                                    break; 
                                }
                            case 0x0015:
                                {
                                    ushort regIndex_X = (ushort)((opcode & 0x0F00) >> 8);
                                    delayTimer = V[regIndex_X];
                                    pc += 2; 
                                    break; 
                                }
                            case 0X0018:
                                {
                                    ushort regIndex_X = (ushort)((opcode & 0x0F00) >> 8);
                                    soundTimer = V[regIndex_X];
                                    pc += 2; 
                                    break; 
                                }
                            case 0x001E:
                                {
                                    ushort regIndex_X = (ushort)((opcode & 0x0F00) >> 8);
                                    I = (ushort)(I + V[regIndex_X]);
                                    pc += 2; 
                                    break; 
                                }
                            case 0x0029:
                                {
                                    ushort regIndex_X = (ushort)((opcode & 0x0F00) >> 8);
                                    I = V[regIndex_X];
                                    pc += 2; 
                                    break; 
                                }
                            case 0x0033:
                                {

                                    break; 
                                }
                            case 0x0055:
                                {
                                    ushort maxIndex_X = (ushort)((opcode & 0x0F00) >> 8); 
                                    for (int i = 0; i < maxIndex_X; i++)
                                    {
                                        memory[I + i] = (byte)V[i]; 
                                    }
                                    pc += 2; 
                                    break; 
                                }
                            case 0x0065:
                                {
                                    ushort maxIndex_X = (ushort)((opcode & 0x0F00) >> 8);
                                    for (int i = 0; i < maxIndex_X; i++)
                                    {
                                        V[i] = memory[I + i]; 
                                    }
                                    pc += 2; 
                                    break; 
                                }
                        }
                        break; 
                    }
                default:
                    Console.Write(" NOT FOUND");
                    pc += 2; 
                    break; 
            }

            Console.Write("\n"); 
        }

    }
}
