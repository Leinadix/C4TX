# Clay-cs

c# binding for [clay.h](https://github.com/nicbarker/clay), an intimidate mode ui layout library with css-like styling. 
The binding has version parity up to 0.13.0

This README will focus on the c# syntax. for a more complete documentation pages I would recommend checking the clay GitHub. 

## Quickstart
Example using RayLib as renderer
The repo also contains a c# copy of the [video example](https://www.youtube.com/watch?v=DYWTw19_8r4) 
```cs
using ZeroElectric.Vinculum;

namespace Clay_cs.Example.Examples;

public class QuickStart
{
	public unsafe void Run()
	{
		// initialize raylib
		Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE | ConfigFlags.FLAG_WINDOW_HIGHDPI
			| ConfigFlags.FLAG_MSAA_4X_HINT | ConfigFlags.FLAG_VSYNC_HINT);
		Raylib.InitWindow(1024, 768, "C# Quickstart Clay");

		// initialize clay
		using var arena = Clay.CreateArena(Clay.MinMemorySize());
		Clay.Initialize(arena, new Clay_Dimensions(Raylib.GetScreenWidth(), Raylib.GetScreenHeight()), ErrorHandler);
		Clay.SetDebugModeEnabled(true);

		// setup text measuring and fonts
		Clay.SetMeasureTextFunction(RaylibClay.MeasureText);
		RaylibClay.Fonts[0] = Raylib.LoadFont("resources/Roboto-Regular.ttf");
		Raylib.SetTextureFilter(RaylibClay.Fonts[0].texture, TextureFilter.TEXTURE_FILTER_BILINEAR);

		while (Raylib.WindowShouldClose() == false)
		{
			// update these every frame
			Clay.SetLayoutDimensions(new Clay_Dimensions(Raylib.GetScreenWidth(), Raylib.GetScreenHeight()));
			Clay.SetPointerState(Raylib.GetMousePosition(), Raylib.IsMouseButtonDown(0));
			Clay.UpdateScrollContainers(true, Raylib.GetMouseWheelMoveV(), Raylib.GetFrameTime());

			// build your layout
			Clay.BeginLayout();

			using (Clay.Element(new()
			{
				id = Clay.Id("QuickStart"),
				backgroundColor = new Clay_Color(25, 0, 25),
				layout = new()
				{
					sizing = new Clay_Sizing(Clay_SizingAxis.Fixed(100), Clay_SizingAxis.Grow())
				}
			}))
			{
				Clay.OpenTextElement("Some text here", new()
				{
					fontSize = 16,
					textColor = new Clay_Color(255, 255, 255)
				});
			}

			var commands = Clay.EndLayout();

			// draw the layout
			Raylib.BeginDrawing();
			Raylib.ClearBackground(Raylib.BEIGE);
			RaylibClay.RenderCommands(commands);
			Raylib.EndDrawing();
		}
	}

	private static void ErrorHandler(Clay_ErrorData data)
	{
		Console.WriteLine($"{data.errorType}: {data.errorText.ToCSharpString()}");
	}
}
```

# Limitations

- Only dll has been build and tested.
- This package currently has no plans to fully implement any renderers. The example project contains a Raylib renderer (`RaylibClay.cs`). If you want to use this we recommend you to just copy it and modify as you need. 

# Install

## Nuget
`dotnet add package Orcolom.Clay_cs`

## Source / Git

- Clone this project locally and include all submodules.
- Add `Clay-cs.csproj` as your dependency

---

# Building Clay.dll

## Requisites
- ClangSharp needs to be installed as a global tool: https://github.com/dotnet/ClangSharp
- Zig needs to be part of your PATH: https://ziglang.org/download/

## Building
- Copy clay.h from the [clay repo](https://github.com/nicbarker/clay) to `Clay.Builder/src/clay.h`
- Ensure the presence off / Add back DLL_EXPORT at the top
```c 
  #ifdef CLAY_DLL
  #define CLAY_DLL_EXPORT() __declspec(dllexport) __stdcall
  #else
  #define CLAY_DLL_EXPORT(null)
  #endif
```
- Ensure the presence off / Add back CLAY_DLL_EXPORT
  - to all public methods under `// Public API functions ---`
  - to all internal methods under `// Internal API functions required by macros`. This is required so we can recreate the macros.
- open Clay.Builder.csproj
- run the project. 
- Clay.dll and the interop files will be copied to their needed destinations in `Clay-cs`

## Verify Clay.Dll

You can use a tool like [Dependencies](https://github.com/lucasg/Dependencies) to check if all needed c methods are listed in the Clay.dll
