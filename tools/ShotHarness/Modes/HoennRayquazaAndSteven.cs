using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    /// <summary>
    /// Hoenn's post-game, part c (plan 21 · H12), after part b's shots at the end of the <c>hoenn</c> mode
    /// (<c>HOENN_FROM=postgame</c>): Sky Pillar's second floor laid clean and cracked all over once Rayquaza has calmed
    /// Sootopolis (<c>h280</c>, <c>h281</c>), the top with Rayquaza waiting and its battle (<c>h282</c>, <c>h283</c>),
    /// Meteor Falls' first room before and after the Hall of Fame opens Steven's cave (<c>h284</c>, <c>h285</c>), the cave,
    /// Steven's words and his battle (<c>h286</c> to <c>h288</c>). It prints the swaps laid and the flags each scene left.
    /// Written, not run, by the session that built it.
    /// </summary>
    void HoennRayquazaAndStevenShots()
    {
        if (!HoennReady("Rayquaza and Steven")) return;
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void ReadOn()
        {
            for (int i = 0; i < 1800 && (engine.ScriptRunning || game.State != GameState.Overworld); i++)
            {
                if (game.State == GameState.Battle) return;
                if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            Frames(6);
        }
        void Flags(string label, params string[] flags) =>
            Console.WriteLine($"  {label}: " + string.Join(", ", flags.Select(f => $"{f} {(game.Story.Has(f) ? "set" : "clear")}")));
        void Swapped(string map) =>
            Console.WriteLine($"  {map}: " + string.Join(", ", PokemonPlatinumEngine.Data.MapDatabase.Get(map).SwappedPlaces.Select(p => $"{p.Flag} {(p.Swapped ? "laid" : "not laid")}")));

        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        engine.Settings.TimeOfDay = TimeOfDay.Day;

        // ---- Sky Pillar's second floor, clean and then cracked
        game.Story.Unset("FLAG_HOENN_SKY_PILLAR_CRACKED");
        At("SkyPillar_2F", 10, 2, Direction.Down); Frames(20);
        Swapped("SkyPillar_2F");
        Shot("h280_sky_pillar_2f_clean");
        game.Story.Set("FLAG_HOENN_SKY_PILLAR_CRACKED");
        game.Story.SetVar("VAR_SKY_PILLAR_STATE", 2);
        game.Story.SetVar("VAR_SKY_PILLAR_RAYQUAZA_CRY_DONE", 1);
        game.Story.Set("FLAG_HIDE_SKY_PILLAR_TOP_RAYQUAZA");
        At("SkyPillar_2F", 10, 2, Direction.Down); Frames(20);
        Swapped("SkyPillar_2F");
        Shot("h281_sky_pillar_2f_cracked");

        // ---- The top: Rayquaza back, and its battle
        game.Story.Unset("FLAG_DEFEATED_RAYQUAZA");
        At("SkyPillar_Top", 14, 8, Direction.Up);
        game.ArriveOnMap(); Frames(20);
        ReadOn();
        Shot("h282_sky_pillar_top_rayquaza");
        At("SkyPillar_Top", 14, 7, Direction.Up);
        game.Interact(); Frames(3);
        ReadOn();
        Until(() => game.State == GameState.Battle, "Rayquaza's battle", 600);
        if (game.State == GameState.Battle) { ToMainMenu(game.Battle); Shot("h283_rayquaza_battle"); }
        Flags("Rayquaza", "FLAG_HIDE_SKY_PILLAR_TOP_RAYQUAZA_STILL", "FLAG_DEFEATED_RAYQUAZA");

        // ---- Meteor Falls' first room, before and after the Hall of Fame opens Steven's cave
        game.State = GameState.Overworld;
        game.Story.Unset("HoennHallOfFame");
        At("MeteorFalls_1F_1R", 4, 4, Direction.Up); Frames(20);
        Swapped("MeteorFalls_1F_1R");
        Shot("h284_meteor_falls_shut");
        game.Story.Set("HoennHallOfFame");
        At("MeteorFalls_1F_1R", 4, 4, Direction.Up); Frames(20);
        Swapped("MeteorFalls_1F_1R");
        Shot("h285_meteor_falls_stevens_cave_open");

        // ---- Steven's cave, his words and his battle
        At("MeteorFalls_StevensCave", 10, 28, Direction.Up); Frames(20);
        Shot("h286_stevens_cave");
        game.Story.Unset("FLAG_DEFEATED_METEOR_FALLS_STEVEN");
        At("MeteorFalls_StevensCave", 19, 4, Direction.Up);
        game.Interact(); Frames(3);
        Until(() => Box().IsActive, "Steven's words"); Frames(40); Box().FinishLine(); Frames(2);
        Shot("h287_steven");
        ReadOn();
        Until(() => game.State == GameState.Battle, "Steven's battle", 600);
        if (game.State == GameState.Battle) { ToMainMenu(game.Battle); Shot("h288_steven_battle"); }
        Flags("Steven", "FLAG_DEFEATED_METEOR_FALLS_STEVEN");
    }
}
