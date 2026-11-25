using System;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    /// <summary>
    /// Tera Raids and picnics (plan 34 · PD7; the <c>paldea</c> mode, <c>SHOTS_FILTER=ft72,27g</c>): a Tera crystal lit today
    /// on the open map by day and after dark (<c>ft72b_tera_crystal</c>, <c>ft72c_tera_crystal_night</c>); a Tera Raid beside
    /// three of the academy's students, the boss Terastallized as it opens (<c>ft72a_tera_raid_begins</c>) and at the menu
    /// with its crystal shield up (<c>ft72_tera_raid</c>); and a picnic on the South Province's grass: the team
    /// (<c>27g_picnic</c>), a sandwich's makings on the table (<c>27g_picnic_table</c>) and the sandwich served with its power
    /// (<c>27g_picnic_sandwich</c>). It prints the crystal, the raid's shields and clock, and what the sandwich started.
    /// </summary>
    void PaldeaRaidsAndPicnics(OpenMap paldea)
    {
        // A crystal lit today, stood nine metres south of it and facing it
        uint day = game.Flow.Encounters.DailyNumber;
        int version = Math.Max(1, game.Story.Var("VAR_PALDEA_VERSION"));
        int badges = game.Story.BadgeCount(RegionDatabase.Paldea);
        var lit = Raids.Paldea.Dens.FirstOrDefault(c => Raids.CrystalStands(c, day, version, badges, felled: false));
        if (lit != null && paldea.Triggers.FirstOrDefault(t => t.Name == "crystal_" + lit.Id) is { } trigger)
        {
            double cx = trigger.Shape.Base.X / 1000.0, cz = trigger.Shape.Base.Z / 1000.0;
            var today = Raids.CrystalToday(lit, day, version, badges);
            foreach (var (shot, night) in new[] { ("ft72b_tera_crystal", false), ("ft72c_tera_crystal_night", true) })
            {
                if (!Wanted(shot)) continue;
                var hour = engine.Settings.TimeOfDay;
                if (night)
                {
                    engine.Settings.TimeOfDay = TimeOfDay.Night;
                    engine.ApplySettings(window: false);
                }
                var at = OnLabGround(paldea, cx, cz + 9, 90, 0);
                PutInLab(paldea, at, 180, 180);
                game.LocationSign.Hide();
                Frames(40);
                Shot(shot);
                Console.WriteLine($"  {shot}: crystal {lit.Id} at ({cx}, {cz}) holds a {today.Species} of {today.Stars} stars, Tera {today.Tera}");
                if (night)
                {
                    engine.Settings.TimeOfDay = hour;
                    engine.ApplySettings(window: false);
                }
            }
        }
        else Console.WriteLine($"  !! no crystal lit on day {day}");

        PaldeaTeraRaid(paldea);
        PaldeaPicnic(paldea);
    }

    /// <summary>A Tera Raid begun as a crystal's script begins it, fought with the first move until the crystal shield is up.</summary>
    void PaldeaTeraRaid(OpenMap paldea)
    {
        if (!Wanted("ft72_tera_raid") && !Wanted("ft72a_tera_raid_begins")) return;
        void Quick()
        {
            FrameClock.Fixed = ++tick / 60.0;
            engine.Update(1f / 60f);
            engine.Draw();
        }
        if (paldea.Marker("area_two_west") is { } m) PutInOpenMap(paldea, m.Pos, m.Yaw);
        game.LocationSign.Hide();
        game.Party.Clear();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Skeledirge")!, 62));
        game.Bag.AddItem(ItemDatabase.Get(BattleCore.TeraOrbItem)!, 1);
        Frames(4);
        var crystal = Raids.Crystal("glaseado_1")!;
        var boss = new Pokemon(PokemonDatabase.Get("Baxcalibur")!, 52);
        var tera = TeraType.Of(PokemonType.Dragon);
        boss.TeraType = tera;
        var students = Raids.Allies(crystal, 3, 1, 50, Raids.Paldea);
        engine.Drive.StartRaid(boss, new RaidTerms(4, students, Tera: tera));
        for (int i = 0; i < 900 && game.State != GameState.Battle; i++) Quick();
        if (game.State != GameState.Battle) { Console.WriteLine("  !! never happened: the Tera Raid"); return; }
        var raid = game.Battle!;
        bool beginsShot = false, shieldShot = false;
        for (int guard = 0; guard < 900 && !raid.IsBattleOver && !shieldShot; guard++)
        {
            if (raid.HUD.MenuState == BattleMenuState.Main)
            {
                if (raid.Core.RaidShields > 0)
                {
                    Skip(0.6);
                    Shot("ft72_tera_raid");
                    shieldShot = true;
                    Console.WriteLine($"  ft72_tera_raid: turn {raid.Core.Turn}, shields {raid.Core.RaidShields}, turns left {raid.Core.TeraTurnsLeft}, "
                        + $"boss {raid.Core.EnemySlots[0].Pokemon!.CurrentHP}/{raid.Core.EnemySlots[0].Pokemon!.MaxHP}, orb {raid.Core.OrbChargeOf(raid.Core.PlayerSlots[0])}");
                    break;
                }
                raid.SelectMainMenuOption(0);
                if (raid.CanTerastallizeNow && !raid.TeraArmed) raid.ToggleTera();
                raid.SelectMove(0);
            }
            else
            {
                if (!beginsShot && raid.CurrentMessage?.Contains("Tera energy") == true)
                {
                    Skip(0.8);
                    Shot("ft72a_tera_raid_begins");
                    beginsShot = true;
                }
                Confirm(raid);
            }
            Skip(0.4);
        }
        if (!shieldShot) Console.WriteLine($"  !! never happened: the crystal shield (turn {raid.Core.Turn}, over {raid.IsBattleOver})");
        game.Battle = null;
        game.State = GameState.Overworld;
    }

    /// <summary>A picnic on the South Province's grass: the team, the table with a sandwich's makings, the sandwich served.</summary>
    void PaldeaPicnic(OpenMap paldea)
    {
        if (!Wanted("27g_picnic") && !Wanted("27g_picnic_table") && !Wanted("27g_picnic_sandwich")) return;
        if (paldea.Marker("area_two_west") is { } m) PutInOpenMap(paldea, m.Pos, m.Yaw);
        game.LocationSign.Hide();
        game.State = GameState.Overworld;
        game.Party.Clear();
        foreach (var (species, level) in new[] { ("Sprigatito", 16), ("Lechonk", 14), ("Pawmi", 15), ("Smoliv", 13) })
        {
            var p = new Pokemon(PokemonDatabase.Get(species)!, level);
            p.CurrentHP = Math.Max(1, p.MaxHP * 2 / 3);
            game.Party.Add(p);
        }
        game.Crafting.Pouch.Clear();
        foreach (var (id, count) in new[] { ("butter", 2), ("cheese", 2), ("tomatoes", 2), ("onions", 1), ("spices", 1) })
            game.Crafting.Pouch.Add(id, count);
        game.Bag.AddItem(ItemDatabase.Get("Oran Berry")!, 2);
        game.Bag.AddItem(ItemDatabase.Get("Honey")!, 1);
        Frames(10);
        Console.WriteLine($"picnic offered: {engine.Drive.HasCamp}");
        engine.Drive.ChooseFromStartMenu(StartMenuChoice.Camp);
        Frames(40);
        var camp = game.Camp;
        Shot("27g_picnic");
        camp.AskToCook();
        Frames(8);
        foreach (var id in new[] { "butter", "Oran Berry" })
        {
            int at = camp.OnHand.FindIndex(i => i.Id == id);
            if (at < 0) { Console.WriteLine($"  !! {id} not on the table: {string.Join(", ", camp.OnHand.Select(i => i.Id))}"); return; }
            while (camp.PantryCursor != at) camp.PantryMove(1);
            camp.PantryConfirm();
        }
        Frames(10);
        Shot("27g_picnic_table");
        Console.WriteLine($"  table: {string.Join(", ", camp.Pot.Select(i => i.Id))} makes {Meals.NameOf(camp.Expected)} (sandwiches: {camp.Supplies?.Sandwiches})");
        while (!camp.OnCook) camp.PantryMove(1);
        camp.PantryConfirm();
        for (int guard = 0; guard < 1500 && camp.Phase == CampPhase.Stirring; guard++)
        {
            if (camp.Stirring is { WindowOpen: true }) camp.Stir();
            Frames(1);
        }
        Frames(30);
        Shot("27g_picnic_sandwich");
        var served = camp.Served;
        Console.WriteLine($"  served: {(served == null ? "(none)" : $"{Meals.NameOf(served.Dish)} {served.Grade}, power {served.Power?.Kind} L{served.Power?.Level} {served.Power?.Type} {served.Power?.SecondsLeft}s")}");
        camp.ServedConfirm();
        Frames(10);
        camp.Cancel();
        Frames(10);
        game.State = GameState.Overworld;
    }
}
