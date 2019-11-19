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

        const int width = 64;
        const int height = 32;
        const int scale = 1; 

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

        //pixels (black and white only)
        byte[,] gfx = new byte[width, height];

        char delayTimer;
        char soundTimer;

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
        }

        public void ProcessCycle()
        {
            if (pc >= memory.Length - 2)
            {
                finished = true;
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
                    stack[sp++] = pc;  
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
                        byte nBytes = (byte)(opcode & 0x000F);
                        for (int i = 0; i < nBytes; i++)
                        {
                            int sprite = memory[I + i]; 
                            for (int x = 0; x < 8; x++)
                            {
                                //loop through scale here, adding s to x and i
                                //for (int s = 0; s < scale; s++)
                                //Also multiply X and Y by scale
                                ushort X = (ushort)(V[((opcode & 0x0F00) >> 8) + x] % width);
                                ushort Y = (ushort)(V[((opcode & 0x00F0) >> 4) + i] % height);
                                if ((sprite & (0x80 >> i)) != 0)
                                {
                                    if (gfx[X, Y] == 1)
                                        V[0xF] = 1;

                                    if (gfx[X, Y] == 1)
                                        gfx[X, Y] = 0;
                                    else
                                        gfx[X, Y] = 1; 
                                }
                            }
                        }

                        pc += 2; 
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
