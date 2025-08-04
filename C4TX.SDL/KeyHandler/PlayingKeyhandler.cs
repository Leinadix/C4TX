using static SDL2.SDL;
using static C4TX.SDL.Engine.GameEngine;

namespace C4TX.SDL.KeyHandler
{
    public class PlayingKeyhandler
    {
        public static void HandlePlayingKeys(SDL_Scancode scancode)
        {

            // Game keys
            for (int i = 0; i < 4; i++)
            {
                if (scancode == _keyBindings[i])
                {
                    if (_keyStates[i] == 0) // Only register a press if the key wasn't already pressed
                    {
                        _keyStates[i] = 1;
                        CheckForHits(i);
                    }
                    return;
                }
            }

            // Escape to stop
            if (scancode == SDL_Scancode.SDL_SCANCODE_ESCAPE)
            {
                Stop();
            }

            // P to pause
            if (scancode == SDL_Scancode.SDL_SCANCODE_F1)
            {
                // Only allow pausing after the countdown
                if (_currentTime >= START_DELAY_MS)
                {
                    TogglePause();
                }
            }
        }
    }
}
