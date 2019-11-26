using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDL2;
using Chip8.emu; 

namespace Chip8
{
    class Program
    {
        static void Draw(Emu emu, IntPtr Renderer)
        {
            SDL.SDL_Rect rect = new SDL.SDL_Rect();
            rect.x = 0;
            rect.y = 0;
            rect.w = Emu.scale;
            rect.h = Emu.scale; 

            for (int x = 0; x < Emu.width; x++)
            {
                rect.x = x * Emu.scale; 
                for (int y = 0; y < Emu.height; y++)
                {
                    rect.y = y * Emu.scale; 
                    if (emu.GFX[x, y] == 1)
                    {
                        SDL.SDL_SetRenderDrawColor(Renderer, 255, 255, 255, 255); 
                        SDL.SDL_RenderFillRect(Renderer, ref rect); 
                    }
                    else
                    {
                        SDL.SDL_SetRenderDrawColor(Renderer, 0, 0, 0, 0);
                        SDL.SDL_RenderFillRect(Renderer, ref rect);
                    }
                    
                }
            }
            SDL.SDL_RenderPresent(Renderer);
        }

        static int TranslateKeyToIndex(SDL.SDL_Keycode KeyCode)
        {
            switch (KeyCode)
            {
                case SDL.SDL_Keycode.SDLK_1:
                    return 0x1;
                case SDL.SDL_Keycode.SDLK_2:
                    return 0x2;
                case SDL.SDL_Keycode.SDLK_3:
                    return 0x3;  
                case SDL.SDL_Keycode.SDLK_4:
                    return 0xC;
                case SDL.SDL_Keycode.SDLK_q:
                    return 0x4;
                case SDL.SDL_Keycode.SDLK_w:
                    return 0x5;
                case SDL.SDL_Keycode.SDLK_e:
                    return 0x6;
                case SDL.SDL_Keycode.SDLK_r:
                    return 0xD;
                case SDL.SDL_Keycode.SDLK_a:
                    return 0x7;
                case SDL.SDL_Keycode.SDLK_s:
                    return 0x8;
                case SDL.SDL_Keycode.SDLK_d:
                    return 0x9;
                case SDL.SDL_Keycode.SDLK_f:
                    return 0xE;
                case SDL.SDL_Keycode.SDLK_z:
                    return 0xA;
                case SDL.SDL_Keycode.SDLK_x:
                    return 0x0;
                case SDL.SDL_Keycode.SDLK_c:
                    return 0xB;
                case SDL.SDL_Keycode.SDLK_v:
                    return 0xF; 
                
            }
            return -1; 
        }

        static void Main(string[] args)
        {
            IntPtr Renderer;
            IntPtr Window; 

            SDL.SDL_SetHint(SDL.SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1");
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);

            SDL.SDL_Event e; 

            SDL.SDL_CreateWindowAndRenderer(Emu.width, Emu.height, SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE, out Window, out Renderer);
            SDL.SDL_SetRenderDrawColor(Renderer, 0, 0, 0, 0);
            SDL.SDL_RenderClear(Renderer);
            SDL.SDL_SetRenderDrawColor(Renderer, 255, 255, 255, 255);
            Emu emu = new Emu();
            emu.Initialize();
            while (!emu.finished)
            {
                while (SDL.SDL_PollEvent(out e) != 0) {
                    switch (e.type)
                    {
                        case SDL.SDL_EventType.SDL_QUIT:
                            emu.finished = true; 
                            break;
                        case SDL.SDL_EventType.SDL_KEYDOWN:
                            emu.Keys[TranslateKeyToIndex(e.key.keysym.sym)] = 1; 
                            break;
                        case SDL.SDL_EventType.SDL_KEYUP:
                            emu.Keys[TranslateKeyToIndex(e.key.keysym.sym)] = 0; 
                            break; 
                    }
                }

                if (!emu.DrawFlag)
                {
                    emu.ProcessCycle();
                }
                if (emu.DrawFlag)
                {
                    Draw(emu, Renderer);
                    emu.DrawFlag = false;
                }
            }

            SDL.SDL_Delay(5000);

            Console.WriteLine("Hello, World");
            Console.ReadKey();

            SDL.SDL_DestroyRenderer(Renderer); 
            SDL.SDL_DestroyWindow(Window);

            SDL.SDL_Quit();
        }
    }
}
