using System;
using Raylib_cs;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine;

public static class Program
{
    public static void Main(string[] args)
    {
        // The window opens at the size and V-Sync choice saved in the options (sized for the monitor the first time)
        var settings = GameSettings.Load();
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | (settings.VSync ? ConfigFlags.VSyncHint : 0));
        Raylib.InitWindow(settings.WindowWidth, settings.WindowHeight, "Pokémon Platinum - Sinnoh Distortion Engine");
        Raylib.SetWindowMinSize(960, 540);
        Raylib.SetTargetFPS(60);
        Raylib.SetExitKey(KeyboardKey.Null); // Esc is "back" in the menus; the window closes with its own button
        WindowSettings.FitToMonitor(settings);
        WindowSettings.Apply(settings);

        // Initialize Engine
        GameEngine engine = new(settings);
        engine.Initialize();

        // Main Game Loop
        while (!Raylib.WindowShouldClose())
        {
            // Toggle full screen on F11 or Alt+Enter
            if (Raylib.IsKeyPressed(KeyboardKey.F11) || (Raylib.IsKeyDown(KeyboardKey.LeftAlt) && Raylib.IsKeyPressed(KeyboardKey.Enter)))
            {
                engine.ToggleFullscreen();
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
