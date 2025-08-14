using SDL;
using static SDL.SDL3;
using Clay_cs;
using System.Numerics;
using System.Runtime.InteropServices;
using static C4TX.SDL.LUI.Wrapper;
using System.Security.Cryptography;
using System;
using System.Drawing;
namespace C4TX.SDL.LUI
{
    public static unsafe class Wrapper
    {

        public static void DrawClayText(string text, ushort fontSize, Color textColor, ushort fontId,
            ushort letterSpacing, ushort lineHeight, Clay_TextAlignment textAlignment, Clay_TextElementConfigWrapMode wrapMode, Clay_ElementId parent)
        {
            
            var rectSize = new Clay_Dimensions(Clay.GetElementData(parent).boundingBox.width, Clay.GetElementData(parent).boundingBox.height);
            IntPtr dataPtr = Marshal.AllocHGlobal(Marshal.SizeOf<Clay_Dimensions>());
            Marshal.StructureToPtr(rectSize, dataPtr, false);

            Clay.OpenTextElement(text, new Clay_TextElementConfig
            {
                fontSize = fontSize,
                textColor = new(textColor.R, textColor.G, textColor.B, textColor.A),
                fontId = fontId,
                letterSpacing = letterSpacing,
                lineHeight = lineHeight,
                textAlignment = textAlignment,
                wrapMode = wrapMode,
                userData = (void*)dataPtr
            });
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UserData { public int w; public int h; }

        public static bool IsHovered(Clay_ElementId id, Vector2 mousePos)
            => Clay.GetElementData(id).boundingBox.x <= mousePos.X &&
                Clay.GetElementData(id).boundingBox.x + Clay.GetElementData(id).boundingBox.width >= mousePos.X &&
                Clay.GetElementData(id).boundingBox.y <= mousePos.Y &&
                Clay.GetElementData(id).boundingBox.y + Clay.GetElementData(id).boundingBox.height >= mousePos.Y;

        public static SDL_Renderer* Renderer;

        public static nint[] Fonts = new nint[10];

        private static SDL_Color ToColor(Clay_Color color) => new SDL_Color
        {
            r = (byte)MathF.Round(color.r),
            g = (byte)MathF.Round(color.g),
            b = (byte)MathF.Round(color.b),
            a = (byte)MathF.Round(color.a),
        };

        public static unsafe Clay_Dimensions MeasureText(Clay_StringSlice slice, Clay_TextElementConfig* config, void* userData)
        {
            if ((nint)userData == nint.Zero) return new();
            var data = *(Clay_Dimensions*)userData;

            return new Clay_Dimensions
            {
                width = data.width,
                height = data.height
            };
        }

        enum HAlign { Left, Center, Right }

        public static unsafe void RenderCommands(Clay_RenderCommandArray array)
        {
            if (Renderer == null)
                throw new InvalidOperationException("Sdl2Clay.Renderer is null!");

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

                            // Wir nehmen nur topLeft an – für echte Anwendungsfälle
                            // am besten min(topLeft, width/2, height/2) benutzen!
                            int radius = (int)cfg.cornerRadius.topLeft;

                            // Standardfüllung, wenn kein Radius
                            if (radius <= 0)
                            {
                                SDL_SetRenderDrawColor(Renderer, col.r, col.g, col.b, col.a);
                                var fullRect = new SDL_FRect { x = x, y = y, w = w, h = h };
                                SDL_RenderFillRect(Renderer, &fullRect);
                            }
                            else
                            {
                                SDL_SetRenderDrawColor(Renderer, col.r, col.g, col.b, col.a);

                                // 1) Mittelstück
                                var centerRect = new SDL_FRect
                                {
                                    x = x + radius,
                                    y = y,
                                    w = w - 2 * radius,
                                    h = h
                                };
                                SDL_RenderFillRect(Renderer, &centerRect);

                                // 2) Linker und rechter Steifen
                                var leftRect = new SDL_FRect
                                {
                                    x = x,
                                    y = y + radius,
                                    w = radius,
                                    h = h - 2 * radius
                                };
                                var rightRect = new SDL_FRect
                                {
                                    x = x + w - radius,
                                    y = y + radius,
                                    w = radius,
                                    h = h - 2 * radius
                                };
                                SDL_RenderFillRect(Renderer, &leftRect);
                                SDL_RenderFillRect(Renderer, &rightRect);

                                // 3) Die vier Ecken als Scanline-Kreise
                                DrawFilledCircle(Renderer, x + radius, y + radius, radius, col); // oben links
                                DrawFilledCircle(Renderer, x + w - radius, y + radius, radius, col); // oben rechts
                                DrawFilledCircle(Renderer, x + radius, y + h - radius, radius, col); // unten links
                                DrawFilledCircle(Renderer, x + w - radius, y + h - radius, radius, col); // unten rechts
                            }
                            break;
                        }



                    case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_BORDER:
                        break;

                    case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_TEXT:
                        {
                            var txt = cmd->renderData.text;
                            string str = txt.stringContents.ToCSharpString();
                            int fid = txt.fontId;
                            float ls = txt.letterSpacing;

                            if (fid < 0 || fid >= Fonts.Length) break;
                            var font = Fonts[fid];
                            if (font == nint.Zero) break;

                            var col = ToColor(txt.textColor);
                            var rect = new SDL_FRect { x = x, y = y, w = w, h = h };
                            HAlign align = HAlign.Center;
                            string[] lines = str.Split('\n');

                            int[] lineWidths = new int[lines.Length];
                            for (int ii = 0; ii < lines.Length; ii++)
                            {
                                string line = lines[ii];

                                for (int j = 0; j < line.Length; j++)
                                {
                                    char c = line[j];
                                    int _, tw;
                                    SDL3_ttf.TTF_GetStringSize((TTF_Font*)font, (byte*)&c, 1, &tw, &_);

                                    lineWidths[ii] = tw;
                                }
                            }

                            float penY = rect.y;

                            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                            {
                                string line = lines[lineIndex];
                                int totalW = lineWidths[lineIndex];

                                float penX;
                                switch (align)
                                {
                                    case HAlign.Center:
                                        penX = rect.x + w / 2;
                                        break;
                                    case HAlign.Right:
                                        penX = rect.x + w - totalW;
                                        break;
                                    default: // Left
                                        penX = rect.x;
                                        break;
                                }

                                foreach (char c in line)
                                {
                                    nint surf = (nint)SDL3_ttf.TTF_RenderGlyph_Blended((TTF_Font*)font, c, col);
                                    if (surf != nint.Zero)
                                    {
                                        nint tex = (nint)SDL_CreateTextureFromSurface(Renderer, (SDL_Surface*)surf);
                                        SDL_DestroySurface((SDL_Surface*)surf);
                                        float gw, gh;
                                        SDL_GetTextureSize((SDL_Texture*)tex, &gw, &gh);
                                        var dst = new SDL_FRect { x = penX, y = penY, w = gw, h = gh };
                                        SDL_RenderTexture(Renderer, (SDL_Texture*)tex, null, &dst);
                                        SDL_DestroyTexture((SDL_Texture*)tex);

                                        penX += gw + (int)ls;
                                    }
                                }

                                // move down one line-height:
                                penY += txt.lineHeight > 0
                                        ? txt.lineHeight
                                        : SDL3_ttf.TTF_GetFontHeight((TTF_Font*)font);
                            }

                            break;
                        }

                    case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_IMAGE:
                        {
                            var texPtr = new nint(cmd->renderData.image.imageData);
                            if (texPtr == nint.Zero) break;

                            float tw, th;

                            var userData = new nint(cmd->userData);
                            if (userData == nint.Zero) break;

                            UserData u = *(UserData*)userData;

                            SDL_GetTextureSize((SDL_Texture*)texPtr, &tw, &th);
                            var dst = new SDL_FRect
                            {
                                x = x,
                                y = y,
                                w = u.w,
                                h = u.h
                            };
                            SDL_RenderTexture(Renderer, (SDL_Texture*)texPtr, null, &dst);
                            Marshal.FreeHGlobal(userData);
                            break;
                        }

                    case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_SCISSOR_START:
                        {
                            var clip = new SDL_Rect { x = x, y = y, w = w, h = h };
                            SDL_SetRenderClipRect(Renderer, &clip);
                            break;
                        }

                    case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_SCISSOR_END:
                        SDL_SetRenderClipRect(Renderer, null);
                        break;

                    case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_CUSTOM:
                        // leer
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        private static void DrawFilledCircle(SDL_Renderer* renderer, int cx, int cy, int radius, SDL_Color col)
        {
            SDL_SetRenderDrawColor(renderer, col.r, col.g, col.b, col.a);

            for (int dy = -radius; dy <= radius; dy++)
            {
                int dx = (int)MathF.Floor(MathF.Sqrt(radius * radius - dy * dy));
                SDL_RenderLine(renderer,
                    cx - dx, cy + dy,
                    cx + dx, cy + dy
                );
            }
        }
    }
}
