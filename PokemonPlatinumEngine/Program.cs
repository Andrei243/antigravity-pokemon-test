using System;
using Raylib_cs;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine;

public static class Program
{
    public static void Main(string[] args)
    {
        // 1920x1080 Full HD Native Window Setup
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.VSyncHint);
        Raylib.InitWindow(1920, 1080, "Pokémon Platinum - Sinnoh Distortion Engine (Full HD)");
        Raylib.SetWindowMinSize(960, 540);
        Raylib.SetTargetFPS(60);

        // Initialize Engine
        GameEngine engine = new();
        engine.Initialize();

        // Main Game Loop
        while (!Raylib.WindowShouldClose())
        {
            // Toggle Fullscreen on F11 or Alt+Enter
            if (Raylib.IsKeyPressed(KeyboardKey.F11) || (Raylib.IsKeyDown(KeyboardKey.LeftAlt) && Raylib.IsKeyPressed(KeyboardKey.Enter)))
            {
                Raylib.ToggleFullscreen();
            }

            float dt = Raylib.GetFrameTime();
            if (dt > 0.1f) dt = 0.1f; // Prevent spiral of death on lag spike

            engine.Update(dt);
            engine.Draw();
        }

        // Cleanup
        engine.Close();
        Raylib.CloseWindow();
    }
}
