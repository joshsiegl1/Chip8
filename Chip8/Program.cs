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
            for (int x = 0; x < Emu.width; x++)
            {
                for (int y = 0; y < Emu.height; y++)
                {
                    if (emu.GFX[x, y] == 1)
                    {
                        SDL.SDL_RenderDrawPoint(Renderer, x, y); 
                    }
                    
                }
            }
            SDL.SDL_RenderPresent(Renderer);
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
                    }
                }

                emu.ProcessCycle();
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
