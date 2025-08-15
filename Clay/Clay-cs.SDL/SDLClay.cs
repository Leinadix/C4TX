using System;
using System.Numerics;
using SDL2;

namespace Clay_cs.Example
{
    public static class Sdl2Clay
    {
        /// <summary>
        /// Muss vor RenderCommands einmalig gesetzt werden.
        /// </summary>
        public static IntPtr Renderer;

        /// <summary>
        /// Hier speichern: Sdl2Clay.Fonts[id] = SDL_ttf.TTF_OpenFont(path, size);
        /// </summary>
        public static IntPtr[] Fonts = new IntPtr[10];

        private static SDL.SDL_Color ToColor(Clay_Color color) => new SDL.SDL_Color
        {
            r = (byte)MathF.Round(color.r),
            g = (byte)MathF.Round(color.g),
            b = (byte)MathF.Round(color.b),
            a = (byte)MathF.Round(color.a),
        };

        public static unsafe Clay_Dimensions MeasureText(Clay_StringSlice slice, Clay_TextElementConfig* config, void* userData)
        {
            var text = slice.ToCSharpString();

            // Font-Index prüfen
            if (config->fontId < 0 || config->fontId >= Fonts.Length)
                return default;
            var font = Fonts[config->fontId];
            if (font == IntPtr.Zero)
                return default;

            // Textgröße mit SDL_ttf ermitteln
            if (SDL_ttf.TTF_SizeUTF8(font, text, out int w, out int h) != 0)
            {
                // Im Fehlerfall null zurückgeben
                return default;
            }

            return new Clay_Dimensions
            {
                width = w,
                height = h
            };
        }

        public static unsafe void RenderCommands(Clay_RenderCommandArray array)
        {
            if (Renderer == IntPtr.Zero)
                throw new InvalidOperationException("Sdl2Clay.Renderer muss gesetzt sein!");

            for (int i = 0; i < array.length; i++)
            {
                var cmd = Clay.RenderCommandArrayGet(array, i);
                var bb = cmd->boundingBox;
                int x = (int)bb.x;
                int y = (int)bb.y;
                int w = (int)bb.width;
                int h = (int)bb.height;

                switch (cmd->commandType)
                {
                    case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_NONE:
                        break;

                    case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_RECTANGLE:
                        {
                            var cfg = cmd->renderData.rectangle;
                            var col = ToColor(cfg.backgroundColor);
                            SDL.SDL_SetRenderDrawColor(Renderer, col.r, col.g, col.b, col.a);
                            var rect = new SDL.SDL_Rect { x = x, y = y, w = w, h = h };
                            SDL.SDL_RenderFillRect(Renderer, ref rect);
                            break;
                        }

                    case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_BORDER:
                        // erstmal ignoriert
                        break;

                    case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_TEXT:
                        {
                            var txt = cmd->renderData.text;
                            string str = txt.stringContents.ToCSharpString();
                            int fid = txt.fontId;
                            float ls = txt.letterSpacing;

                            if (fid < 0 || fid >= Fonts.Length) break;
                            var font = Fonts[fid];
                            if (font == IntPtr.Zero) break;

                            var col = ToColor(txt.textColor);
                            int penX = x, penY = y;

                            // Zeichenweise rendern, um LetterSpacing anzuwenden
                            foreach (char c in str)
                            {
                                if (c == '\n')
                                {
                                    penX = x;
                                    penY += h;
                                    continue;
                                }

                                IntPtr surf = SDL_ttf.TTF_RenderGlyph_Blended(font, c, col);
                                if (surf == IntPtr.Zero) continue;

                                IntPtr tex = SDL.SDL_CreateTextureFromSurface(Renderer, surf);
                                SDL.SDL_FreeSurface(surf);
                                SDL.SDL_QueryTexture(tex, out _, out _, out int gw, out int gh);

                                var dst = new SDL.SDL_Rect { x = penX, y = penY, w = gw, h = gh };
                                SDL.SDL_RenderCopy(Renderer, tex, IntPtr.Zero, ref dst);
                                SDL.SDL_DestroyTexture(tex);

                                penX += gw + (int)ls;
                            }
                            break;
                        }

                    case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_IMAGE:
                        {
                            // imageData direkt als SDL_Texture* erwartet
                            var texPtr = new IntPtr(cmd->renderData.image.imageData);
                            if (texPtr == IntPtr.Zero) break;

                            SDL.SDL_QueryTexture(texPtr, out _, out _, out int tw, out int th);
                            var dst = new SDL.SDL_Rect { x = x, y = y, w = tw, h = th };
                            SDL.SDL_RenderCopy(Renderer, texPtr, IntPtr.Zero, ref dst);
                            break;
                        }

                    case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_SCISSOR_START:
                        {
                            var clip = new SDL.SDL_Rect { x = x, y = y, w = w, h = h };
                            SDL.SDL_RenderSetClipRect(Renderer, ref clip);
                            break;
                        }

                    case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_SCISSOR_END:
                        SDL.SDL_RenderSetClipRect(Renderer, IntPtr.Zero);
                        break;

                    case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_CUSTOM:
                        // leer
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
