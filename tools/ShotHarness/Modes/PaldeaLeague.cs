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
    /// Paldea's Pokémon League (plan 34 · PD8, its first session; the <c>paldea</c> mode's last part,
    /// <c>SHOTS_FILTER=pdg</c>): the League's building on its terrace under Mesagoza's south wall (<c>pdg00</c>,
    /// <c>pdg01</c>), the lobby (<c>pdg02</c>), Rika's interview at its first question (<c>pdg03</c>), the court with Poppy
    /// due and her battle (<c>pdg04</c>, <c>pdg05</c>), Geeta on the court and her battle (<c>pdg06</c>, <c>pdg07</c>), and
    /// after Nemona the Hall of Fame's scene at its first card (<c>pdg08</c>). It wins every battle it starts. Last in the
    /// mode, since it leaves the game in the Hall of Fame's scene.
    /// </summary>
    void PaldeaLeague(OpenMap paldea)
    {
        if (!Enumerable.Range(0, 9).Any(n => Wanted($"pdg{n:00}"))) return;
        var story = game.Story;
        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
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
            Until(() => FieldAgain(), "the field again", 1500);
            Frames(10);
        }
        // Reads on, answering every question with its first answer, until a battle begins (shot at its menu) or the script ends
        void ReadOn(string? battleShot, int most = 2400)
        {
            int idle = 0;
            for (int i = 0; i < most; i++)
            {
                if (State() == GameState.Battle)
                {
                    if (battleShot != null) { Skip(1.0); ToMainMenu(game.Battle); Shot(battleShot); Console.WriteLine($"  {battleShot}: {game.Battle.EnemyPokemon?.Species.Name}"); battleShot = null; }
                    Win();
                    continue;
                }
                if (State() == GameState.HallOfFameCeremony) return;
                idle = !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld ? idle + 1 : 0;
                if (idle > 30) return;
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
        }
        void Court(int due)
        {
            story.SetVar("VAR_PALDEA_LEAGUE", due);
            At("PaldeaLeague_Court", 7, 6, Direction.Up);
            game.ArriveOnMap();
            game.LocationSign.Hide();
            Frames(40);
        }

        // Outside: from the terrace before the door, and from the road with Mesagoza's wall behind
        ShootToward(paldea, "pdg00_league_outside", "league_front", 46, -290, 26);
        ShootToward(paldea, "pdg01_league_from_the_road", "area_one_north", 46, -290, 40);
        ShootRoom("pdg02_league_lobby", "PaldeaLeague_Lobby");

        // A Champion of eight badges, with a team strong enough to be shot at the menus
        story.SetVar("VAR_PALDEA_STORY", 7);
        story.SetBadges(RegionDatabase.Paldea, 0xFF);
        foreach (var (species, level) in new[] { ("Pawmot", 70), ("Skeledirge", 70), ("Garganacl", 70) })
            if (game.Party.Count < 6) game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, level));

        // The interview at its first question
        At("PaldeaLeague_Lobby", 3, 8, Direction.Left);
        game.LocationSign.Hide();
        Frames(10);
        game.Interact();
        for (int i = 0; i < 1200 && !engine.Choice.IsOpen; i++)
        {
            if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
            else Frames(1);
        }
        Frames(20);
        if (Wanted("pdg03_the_interview")) Shot("pdg03_the_interview");
        ReadOn(null);
        Console.WriteLine($"  the interview: VAR_PALDEA_LEAGUE {story.Var("VAR_PALDEA_LEAGUE")}");

        // Poppy due on the court, and her battle
        Court(2);
        if (Wanted("pdg04_court_poppy")) Shot("pdg04_court_poppy");
        game.Interact();
        ReadOn(Wanted("pdg05_poppy_battle") ? "pdg05_poppy_battle" : null);

        // Geeta, and her battle
        Court(5);
        if (Wanted("pdg06_court_geeta")) Shot("pdg06_court_geeta");
        game.Interact();
        ReadOn(Wanted("pdg07_geeta_battle") ? "pdg07_geeta_battle" : null);
        Console.WriteLine($"  after Geeta: VAR_PALDEA_LEAGUE {story.Var("VAR_PALDEA_LEAGUE")}");

        // Nemona, then the Hall of Fame's scene at its first card
        if (!Wanted("pdg08_hall_of_fame")) return;
        Court(6);
        game.Interact();
        ReadOn(null, 6000);
        Until(() => State() == GameState.HallOfFameCeremony, "the Hall of Fame's scene", 1200);
        var ceremony = game.Ceremony;
        Until(() => ceremony.Phase == CeremonyPhase.Show && ceremony.PartsShown == HallOfFameCeremony.CardParts, "the first card", 900);
        Frames(20);
        Shot("pdg08_hall_of_fame");
        Console.WriteLine($"  pdg08_hall_of_fame: PaldeaHallOfFame {story.Has("PaldeaHallOfFame")}, VAR_PALDEA_LEAGUE {story.Var("VAR_PALDEA_LEAGUE")}");
    }
}
