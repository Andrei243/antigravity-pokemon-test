using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    // ---------------------------------------------------------------- locked hearts (plan 44 · SD2)

    // Lockwork's locked hearts on screen (lh*; not part of "all"): a trainer's locked Gible at the menu with its haze
    // (lh01) and close up (lh02), a ball thrown at it with the Reclaimer (lh03) and the reclaim's line (lh04), the
    // reclaimed Gible's summary with its gauge and its sealed moves (lh05, lh05b), a locked Pokémon of the player's
    // breaking loose (lh06) and CALM where RUN stands, then calmed (lh07), and its heart opening on the evolution's stage
    // (lh08) with the level panel for the EXP its lock held (lh09). It prints the lock at each step.
    public void HeartsMode()
    {
        if (Environment.GetEnvironmentVariable("HEARTS_PART") == "cells") { HeartsCells(); return; }
        var rules = Ruleset.Modern;
        // ---- a Lockwork agent's locked Gible, the player carrying the Reclaimer
        var (name, x, y) = Place("Route201", -1, -1);
        game.Map = MapDatabase.Get(name);
        if (x >= 0) game.Player.SetPosition(x, y, Direction.Up);
        var mine = new Party();
        var luxray = new Pokemon(PokemonDatabase.Get("Luxray")!, 40);
        mine.Add(luxray);
        var gible = TrainerDatabase.Build(new TrainerPokemonRecord { Species = "Gible", Level = 22, IvScale = 100, Personality = 4242, Lock = "lock_sinnoh_gible" });
        var agent = new Trainer { Id = "lockwork_agent", Name = "Ivo", TrainerClass = "Ace Trainer", PrizeMoney = 880 };
        agent.Party.Add(gible);
        inventory.AddItem(ItemDatabase.Get("Poké Ball")!, 5);
        var b = new BattleEngine(new BattleSetup
        {
            PlayerParty = mine, Inventory = inventory, Pokedex = pokedex, Trainers = new List<Trainer> { agent }, Rules = rules,
            Random = new BattleRandom(44).Force(RollKind.CatchShake, 0).Force(RollKind.CriticalCapture, 255), Reclaimer = true
        });
        game.BattleRenderer.SetArena(game.Map, x, y);
        game.Battle = b;
        game.State = GameState.Battle;
        ToMainMenu(b);
        Skip(1.2);
        Shot("lh01_locked_foe");
        ShotCrop("lh02_locked_foe_close", 1180, 160, 640, 520, 2);
        Console.WriteLine($"foe's lock: {gible.Lock}, the haze up {b.Anim.Conditions.Of(new Place(BattleSide.Enemy, 0)).Haze.Up}");

        // ---- the ball thrown with the Reclaimer
        b.UseBagItem("Poké Ball");
        Skip(0.6);
        Shot("lh03_reclaim_throw");
        for (int i = 0; i < 40 && !b.CurrentMessage.Contains("reclaimed"); i++) { Confirm(b); Skip(0.5); }
        Skip(0.4);
        Shot("lh04_reclaimed");
        Console.WriteLine($"reclaimed: {string.Join(", ", b.Reclaimed.Select(p => p.DisplayName))}, line: {b.CurrentMessage}");

        // ---- the reclaimed Gible's summary: its gauge, the EXP held and its sealed moves
        party.Clear();
        var reclaimed = b.Reclaimed.FirstOrDefault() ?? gible;
        reclaimed.Lock = (reclaimed.Lock ?? HeartLock.Locked("lock_sinnoh_gible")).Lower(30).Hold(1840);
        party.Add(reclaimed);
        party.Add(new Pokemon(PokemonDatabase.Get("Luxray")!, 40));
        game.Battle = null;
        var partyScreen = game.PartyScreen;
        game.State = GameState.PartyMenu;
        partyScreen.Open();
        Frames(40);
        partyScreen.ShowSummary = true;
        Frames(30);
        Shot("lh05_summary_gauge");
        partyScreen.TurnSummaryPage(1);
        partyScreen.TurnSummaryPage(1);
        Frames(30);
        Shot("lh05b_summary_sealed");
        partyScreen.ShowSummary = false;
        Console.WriteLine($"summary: {reclaimed.Lock}, moves back {reclaimed.Lock!.MovesBack} of {reclaimed.Moves.Count}");

        // ---- a locked Pokémon of the player's breaks loose, and is calmed
        var locked = TrainerDatabase.Build(new TrainerPokemonRecord { Species = "Gible", Level = 30, IvScale = 100, Personality = 4242, Lock = "lock_sinnoh_gible", Moves = new() { "Tackle" } });
        var loose = new Party();
        loose.Add(locked);
        var wild = new Pokemon(PokemonDatabase.Get("Bidoof")!, 28);
        var c = new BattleEngine(new BattleSetup
        {
            PlayerParty = loose, Inventory = inventory, Pokedex = pokedex, WildPokemon = new List<Pokemon> { wild }, Rules = rules,
            Random = new BattleRandom(45).Force(RollKind.Lock, 0, 0).Force(RollKind.Critical, 1).Force(RollKind.Accuracy, 0)
        });
        game.Battle = c;
        game.State = GameState.Battle;
        ToMainMenu(c);
        c.SelectMainMenuOption(0);
        Frames(2);
        c.SelectMove(0);
        for (int i = 0; i < 30 && !c.CurrentMessage.Contains("broke loose"); i++) { Skip(0.3); if (!c.CurrentMessage.Contains("broke loose")) Confirm(c); }
        Skip(0.6);
        Shot("lh06_breaking_loose");
        for (int i = 0; i < 20 && c.HUD.MenuState == BattleMenuState.Message && !c.IsBattleOver; i++) { Confirm(c); Skip(0.6); }
        Skip(0.5);
        Shot("lh07_calm_offered");
        Console.WriteLine($"CALM offered: {c.OffersCalm}");
        if (c.OffersCalm) c.SelectCalm();
        for (int i = 0; i < 30 && !c.CurrentMessage.Contains("calmed"); i++) { Skip(0.3); if (!c.CurrentMessage.Contains("calmed")) Confirm(c); }
        Skip(0.4);
        Shot("lh07_calm");
        Console.WriteLine($"after the calm: {locked.Lock}");

        // ---- the heart opens on the evolution's stage, its held EXP brought a level
        game.Battle = null;
        party.Clear();
        locked.Lock = locked.Lock! with { Gauge = 0, HeldExp = 9000 };
        party.Add(locked);
        game.State = GameState.Overworld;
        var opening = game.OpenHeart(locked);
        for (int i = 0; i < 200 && game.State != GameState.HeartOpening; i++) Frames(1);
        Frames(50);
        Shot("lh08_opening");
        var screen = game.HeartOpening;
        for (int i = 0; i < 600 && screen.Phase != HeartOpeningPhase.Announce; i++) Frames(1);
        Frames(90);
        screen.PressConfirm();
        Frames(30);
        Shot("lh09_opening_levels");
        Console.WriteLine($"opened: {opening.LockId}, level {opening.LevelBefore} -> {opening.LevelAfter}, phase {screen.Phase}");
        screen.PressConfirm();
        screen.PressConfirm();
        Frames(60);
        HeartsCells();
    }
}
