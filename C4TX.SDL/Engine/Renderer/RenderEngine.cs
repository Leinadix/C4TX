using C4TX.SDL.KeyHandler;
using C4TX.SDL.LUI;
using C4TX.SDL.Models;
using C4TX.SDL.Services;
using Clay_cs;
using SDL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static C4TX.SDL.Engine.GameEngine;
using static SDL.SDL3;
using static System.Formats.Asn1.AsnWriter;

namespace C4TX.SDL.Engine.Renderer
{
    public partial class RenderEngine
    {
        public static unsafe void Render()
        {
            // Begin frame timing
            double frameStartTime = SDL_GetTicks();

            if (_currentState != GameState.Playing) Clay.BeginLayout();

            // Clear screen with background color
            SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, Color._bgColor.r, Color._bgColor.g, Color._bgColor.b, Color._bgColor.a);
            SDL_RenderClear((SDL_Renderer*)_renderer);

            // Render different content based on game state
            switch (_currentState)
            {
                case GameState.ProfileSelect:
                    RenderProfileSelection();
                    break;
                case GameState.Menu:
                    RenderMenu();
                    break;
                case GameState.Playing:
                    RenderGameplay();
                    break;
                case GameState.Paused:
                    RenderGameplay();
                    RenderPauseOverlay();
                    break;
                case GameState.Results:
                    RenderResults();
                    break;
                case GameState.Settings:
                    RenderSettings();
                    break;
            }

            if (_currentState != GameState.Playing)
            {

                var commands = Clay.EndLayout();

                Wrapper.RenderCommands(commands);
            }

            // Always render volume indicator if needed
            if (_showVolumeIndicator)
            {
                RenderVolumeIndicator();
            }

            // Always render rate indicator if needed
            if (_showRateIndicator)
            {
                RenderRateIndicator();
            }

            // Render update notification if available
            if (_showUpdateNotification)
            {
                RenderUpdateNotification();
            }

            // Draw FPS counter in top right corner if in menu or gameplay
            if (_currentState == GameState.Menu ||
                _currentState == GameState.Playing ||
                _currentState == GameState.Paused)
            {
                DrawFpsCounter();
            }

            

            // Present the rendered frame
            SDL_RenderPresent((SDL_Renderer*)_renderer);

            // Update FPS counter
            _frameCount++;
            double currentTime = SDL_GetTicks();
            _currentFrameTime = currentTime - frameStartTime;

            // Update FPS calculation every second
            if (currentTime - _lastFpsUpdateTime >= _fpsUpdateInterval)
            {
                _currentFps = _frameCount / ((currentTime - _lastFpsUpdateTime) / 1000.0);
                _lastFpsUpdateTime = currentTime;
                _frameCount = 0;
            }
        }
    }
}
