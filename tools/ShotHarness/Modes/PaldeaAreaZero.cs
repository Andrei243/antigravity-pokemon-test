using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    /// <summary>
    /// Area Zero and the Way Home (plan 34 · PD8, its second session; the <c>paldea</c> mode's very last part,
    /// <c>SHOTS_FILTER=pdz</c>): the Zero Gate with the three friends before it (<c>pdz00</c>), the crater from its second
    /// research station (<c>pdz01</c>), the Zero Lab with Sada's AI and the Koraidon the professor kept (<c>pdz02</c>), the
    /// AI's battle at its menu (<c>pdz03</c>), the legendary's battle form beside the player's own at its menu
    /// (<c>pdz04</c>), and the Way Home, Mom's welcome back in Cabo Poco (<c>pdz05</c>). A Scarlet game; it wins every
    /// battle it starts and leaves the game in the credits, so it comes after everything else.
    /// </summary>
    void PaldeaAreaZero(OpenMap paldea)
    {
        if (!Enumerable.Range(0, 6).Any(n => Wanted($"pdz{n:00}"))) return;
        var story = game.Story;
        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        void Win()
        {
            var fight = game.Battle;
            for (int guard = 0; guard < 600 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    foreach (var foe in fight.EnemyParty?.Members ?? new List<Pokemon>()) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    if (fight.EnemyPokemon is { } met) met.CurrentHP = Math.Min(met.CurrentHP, 1);
                    var lead = fight.PlayerPokemon;
                    if (lead != null) lead.CurrentHP = lead.MaxHP;
                    fight.SelectMainMenuOption(0);
                    if (lead != null) foreach (var m in lead.Moves) m.CurrentPP = m.MaxPP;
                    int hurts = lead == null ? 0 : lead.Moves.Select((m, i) => (m, i)).Where(p => p.m.Data.Category != MoveCategory.Status && p.m.Data.Type != PokemonType.Ghost)
                        .OrderByDescending(p => p.m.Data.Power).Select(p => p.i).DefaultIfEmpty(0).First();
                    fight.SelectMove(hurts);
                    if (fight.HUD.MenuState == BattleMenuState.SelectTarget) fight.SelectTarget(0);
                }
                else Confirm(fight);
                Skip(0.5);
            }
            for (int i = 0; i < 1500 && !FieldAgain(); i++) Frames(1);
            Frames(10);
        }
        // Reads on, shooting each battle at its menu by the next name given, until the player is home (the Way Home's
        // shot at Mom's first line) or the script ends
        void ReadOn(Queue<string> battleShots, int most = 9000)
        {
            int idle = 0;
            for (int i = 0; i < most; i++)
            {
                if (State() == GameState.Battle)
                {
                    if (battleShots.Count > 0)
                    {
                        string shot = battleShots.Dequeue();
                        Skip(1.0); ToMainMenu(game.Battle);
                        if (Wanted(shot)) Shot(shot);
                        Console.WriteLine($"  {shot}: {game.Battle.EnemyPokemon?.Species.Name}, beside {game.Battle.PlayerPokemon?.Species.Name}");
                    }
                    Win();
                    continue;
                }
                if (game.Map.Name == "CaboPoco_PlayersHouse_1F" && Box().IsActive && Box().IsCurrentLineComplete)
                {
                    Frames(20);
                    if (Wanted("pdz05_way_home")) Shot("pdz05_way_home");
                    Console.WriteLine($"  pdz05_way_home: FLAG_PALDEA_WAY_HOME {story.Has("FLAG_PALDEA_WAY_HOME")}, VAR_PALDEA_ZERO {story.Var("VAR_PALDEA_ZERO")}");
                    return;
                }
                idle = !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld ? idle + 1 : 0;
                if (idle > 30) return;
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
        }

        // A Scarlet Champion with the three paths behind them, and a strong team
        story.SetVar("VAR_PALDEA_STORY", 7);
        story.SetVar("VAR_PALDEA_VERSION", 1);
        story.SetVar("VAR_PALDEA_MOUNT", 1);
        story.SetBadges(RegionDatabase.Paldea, 0xFF);
        story.Set("PaldeaHallOfFame");
        story.SetVar("VAR_PALDEA_LEGENDS", 12);
        story.SetVar("VAR_PALDEA_STARFALL", 6);
        foreach (var (species, level) in new[] { ("Pawmot", 80), ("Skeledirge", 80), ("Garganacl", 80) })
            if (game.Party.Count < 5) game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, level));

        // The Zero Gate in the North Province, the friends waiting before it
        ShootToward(paldea, "pdz00_zero_gate", "zero_gate_front", -68, -779, 24);

        // The crater from its second station, looking back over the terrace and down
        story.SetVar("VAR_PALDEA_ZERO", 5);
        story.Set("FLAG_PALDEA_ZERO_GATE_OPEN");
        story.Set("FLAG_PALDEA_ZERO_HIDE_PARADOX_A_VIOLET");
        story.Set("FLAG_PALDEA_ZERO_HIDE_PARADOX_B_VIOLET");
        if (Wanted("pdz01_crater_station"))
        {
            At("AreaZero", 14, 14, Direction.Down);
            game.LocationSign.Hide();
            Frames(40);
            Shot("pdz01_crater_station");
            Console.WriteLine("  pdz01_crater_station: AreaZero at 14,14");
        }

        // The Zero Lab, as a Scarlet game finds it
        story.Set("FLAG_PALDEA_ZERO_HIDE_TURO");
        story.Set("FLAG_PALDEA_ZERO_HIDE_MIRAIDON");
        story.Unset("FLAG_PALDEA_ZERO_HIDE_SADA");
        story.Unset("FLAG_PALDEA_ZERO_HIDE_KORAIDON");
        if (Wanted("pdz02_zero_lab"))
        {
            At("AreaZero_ZeroLab", 7, 11, Direction.Up);
            game.LocationSign.Hide();
            Frames(40);
            Shot("pdz02_zero_lab");
            Console.WriteLine("  pdz02_zero_lab: AreaZero_ZeroLab at 7,11");
        }

        // The AI's battle, the legendary's battle form, and the Way Home
        if (!Enumerable.Range(3, 3).Any(n => Wanted($"pdz{n:00}"))) return;
        At("AreaZero_ZeroLab", 7, 6, Direction.Up);
        game.LocationSign.Hide();
        Frames(20);
        game.Interact();
        ReadOn(new Queue<string>(new[] { "pdz03_ai_battle", "pdz04_legend_battle" }));
    }
}
