using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Overworld;

partial class Harness
{
    /// <summary>
    /// Starfall Street and Let's Go (plan 34 · PD5), after the `paldea` mode's Path of Legends: the street from its mouth
    /// (<c>pd99</c>) and Giacomo's gate (<c>pd99b</c>); Let's Go in the South Province's fields, the lead sent at a wild
    /// Pokémon put five metres ahead, running out, at the hits and the target fading (<c>l80_lets_go</c>, <c>l80b</c>,
    /// <c>l80c</c>); a raid on Giacomo's gate with the crew round the yard and the corner's count and clock
    /// (<c>l81_star_raid</c>) and a Let's Go in it (<c>l81b</c>); and Giacomo's battle at its menu on the base's own stage
    /// (<c>l82_star_boss</c>). It prints how each Let's Go came out and the raid's count.
    /// </summary>
    void PaldeaStarfall(OpenMap paldea)
    {
        ShootToward(paldea, "pd99_starfall_street", "starfall_mouth", 0, -470, 40);
        ShootToward(paldea, "pd99b_giacomo_gate", "giacomo_gate", -78, -430, 26);
    }

    /// <summary>
    /// Let's Go, a raid and a boss's stage played (plan 34 · PD5), last in the `paldea` mode and in a new game of its own,
    /// so the draws and frames they spend move no shot before them (<c>l80</c> to <c>l82</c>).
    /// </summary>
    void PaldeaLetsGoPlayed()
    {
        if (!Wanted("l80") && !Wanted("l81") && !Wanted("l82")) return;
        engine.NewGameRegion = "Paldea";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        game.Story.SetVar("VAR_PALDEA_STORY", 7);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Sprigatito")!, 12));
        Frames(10);
        var paldea = engine.OpenMapNamed("Paldea");
        if (paldea == null) { Console.WriteLine("!! no open map called Paldea"); return; }

        void PressLetsGo()
        {
            InputManager.Feed(ActionFrame.Press(GameAction.LetsGo));
            Frames(1);
            InputManager.Feed(null);
        }

        if (Wanted("l80"))
        {
            // A lead a good way above the field's Pokémon, first in the team
            var lead = new Pokemon(PokemonDatabase.Get("Pawmot")!, 30);
            game.Party.Add(lead);
            game.Party.Swap(0, game.Party.Count - 1);
            var zone = paldea.Zones.FirstOrDefault(z => z.Place == "south_province_area_one") ?? paldea.Zones[0];
            double cx = zone.Polygon.Average(p => p.X) / 1000.0, cz = zone.Polygon.Average(p => p.Z) / 1000.0;
            // The player facing the target off to the north-east, the camera looking east past them, so the lead's run
            // is seen from the side and not behind the player's back
            var at = OnLabGround(paldea, cx, cz + 3, 90, 0);
            PutInLab(paldea, at, 135, 95);
            game.OpenWild!.Clear();
            var target = OnLabGround(paldea, cx + 5, cz - 2, 90, 0);
            var slot = zone.Slots.First();
            game.OpenWild.Put(paldea, zone, slot, 5, target, OpenYaw.South);
            game.LocationSign.Hide();
            Frames(10);
            PressLetsGo();
            Frames(14);
            Shot("l80_lets_go");
            Frames(30);
            Shot("l80b_lets_go_hits");
            Frames(30);
            Shot("l80c_lets_go_fade");
            if (game.LastLetsGo is { } fought)
                Console.WriteLine($"  l80_lets_go: {lead.Species.Name} {lead.Level} at {slot.Species} in {zone.Name}: {fought.End} in {fought.Turns} turns, {fought.Exp} EXP");
            else Console.WriteLine("  !! l80_lets_go: no Let's Go battle was fought");
            Frames(60);
        }

        if (Wanted("l81"))
        {
            game.Story.SetVar("VAR_PALDEA_STARFALL", 1);
            if (paldea.Marker("giacomo_gate") is not { } gate) { Console.WriteLine("!! no marker giacomo_gate"); return; }
            PutInOpenMap(paldea, gate.Pos, gate.Yaw);
            game.LocationSign.Hide();
            game.BeginStarRaid("giacomo");
            Frames(40);
            Shot("l81_star_raid");
            PressLetsGo();
            Frames(36);
            Shot("l81b_star_raid_lets_go");
            if (game.StarRaid is { } raid)
                Console.WriteLine($"  l81_star_raid: {raid.Felled} of {StarRaid.Goal} felled, {raid.SecondsLeft} s left; standing {string.Join(", ", raid.Standing.Where(f => f != null).Select(f => $"{f!.Species} {f.Level}"))}");
            Frames(120);
        }

        if (Wanted("l82"))
        {
            var record = TrainerDatabase.Get("paldea_star_giacomo", RegionDatabase.Paldea);
            if (record == null) { Console.WriteLine("!! no trainer paldea_star_giacomo"); return; }
            var trainer = new Trainer { Id = record.Id };
            TrainerDatabase.Fill(trainer, record);
            game.StartTrainerBattle(new NPC { Name = trainer.Name, NpcType = "giacomo", TrainerData = trainer });
            for (int i = 0; i < 900 && game.State != GameState.Battle; i += 2) Frames(2);
            if (game.Battle is { } b)
            {
                ToMainMenu(b);
                Frames(2);
                Shot("l82_star_boss");
                Console.WriteLine($"  l82_star_boss: {string.Join(", ", b.Trainers[0].Party.Members.Select(p => $"{p.Species.Name} {p.Level}"))}");
                game.State = GameState.Overworld;
                Frames(10);
            }
        }
    }
}
