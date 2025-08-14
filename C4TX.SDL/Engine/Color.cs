using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDL;

namespace C4TX.SDL.Engine
{
    public class Color
    {
        // Color definitions
        public static SDL_Color _bgColor = new SDL_Color() { r = 40, g = 40, b = 60, a = 255 };
        public static SDL_Color[] _laneColors = new SDL_Color[4]
        {
            new SDL_Color() { r = 255, g = 50, b = 50, a = 255 },
            new SDL_Color() { r = 50, g = 255, b = 50, a = 255 },
            new SDL_Color() { r = 50, g = 50, b = 255, a = 255 },
            new SDL_Color() { r = 255, g = 255, b = 50, a = 255 }
        };
        public static SDL_Color _textColor = new SDL_Color() { r = 255, g = 255, b = 255, a = 255 };
        public static SDL_Color _comboColor = new SDL_Color() { r = 255, g = 220, b = 100, a = 255 };

        // UI Theme colors
        public static SDL_Color _primaryColor = new SDL_Color() { r = 65, g = 105, b = 225, a = 255 }; // Royal blue
        public static SDL_Color _accentColor = new SDL_Color() { r = 255, g = 140, b = 0, a = 255 }; // Dark orange
        public static SDL_Color _panelBgColor = new SDL_Color() { r = 20, g = 20, b = 40, a = 230 }; // Semi-transparent dark blue
        public static SDL_Color _highlightColor = new SDL_Color() { r = 255, g = 215, b = 0, a = 255 }; // Gold
        public static SDL_Color _mutedTextColor = new SDL_Color() { r = 180, g = 180, b = 190, a = 255 }; // Light gray
        public static SDL_Color _errorColor = new SDL_Color() { r = 220, g = 50, b = 50, a = 255 }; // Red
        public static SDL_Color _successColor = new SDL_Color() { r = 50, g = 205, b = 50, a = 255 }; // Green
    }
}
