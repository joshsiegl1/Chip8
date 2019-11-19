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
        static void Main(string[] args)
        {
            Emu emu = new Emu();
            emu.Initialize();
            while (!emu.finished)
            {
                emu.ProcessCycle();
            }

            SDL.SDL_SetHint(SDL.SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1");
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);

            var window = IntPtr.Zero;
            window = SDL.SDL_CreateWindow("CHIP 8 EMU",
                SDL.SDL_WINDOWPOS_CENTERED,
                SDL.SDL_WINDOWPOS_CENTERED,
                64,
                32,
                SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE
            );

            SDL.SDL_Delay(5000);
            SDL.SDL_DestroyWindow(window);

            SDL.SDL_Quit(); 

            Console.WriteLine("Hello, World");
            Console.ReadKey(); 
        }
    }
}
