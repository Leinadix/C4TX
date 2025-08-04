using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SDL2.SDL;
using static C4TX.SDL.Engine.GameEngine;

namespace C4TX.SDL.KeyHandler
{
    public class PausedKeyhandler
    {
        public static void HandlePausedKeys(SDL_Scancode scancode)
        {

            if (scancode == SDL_Scancode.SDL_SCANCODE_F1)
            {
                TogglePause();
            }

            // Escape to stop
            if (scancode == SDL_Scancode.SDL_SCANCODE_ESCAPE)
            {
                Stop();
            }
        }
    }
}
