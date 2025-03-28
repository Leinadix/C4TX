using C4TX.SDL.Models;
using static C4TX.SDL.Engine.GameEngine;
using static SDL2.SDL;

namespace C4TX.SDL.Engine
{
    public class ResultsKeyhandler
    {
        public static void HandleResultsKeys(SDL_Scancode scancode)
        {
            if (scancode == SDL_Scancode.SDL_SCANCODE_RETURN)
            {
                _currentState = GameState.Menu;
            }
            else if (scancode == SDL_Scancode.SDL_SCANCODE_SPACE)
            {
                Start();
            }
            else if (scancode == SDL_Scancode.SDL_SCANCODE_LEFT)
            {
                // Cycle to previous accuracy model
                int modelCount = Enum.GetValues(typeof(AccuracyModel)).Length;
                _resultScreenAccuracyModel = (AccuracyModel)((_resultScreenAccuracyModel == 0) ?
                    modelCount - 1 : (int)_resultScreenAccuracyModel - 1);
            }
            else if (scancode == SDL_Scancode.SDL_SCANCODE_RIGHT)
            {
                // Cycle to next accuracy model
                int modelCount = Enum.GetValues(typeof(AccuracyModel)).Length;
                _resultScreenAccuracyModel = (AccuracyModel)(((int)_resultScreenAccuracyModel + 1) % modelCount);
            }
        }
    }
}
