using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    // ---------------------------------------------------------------- Poké Jobs (plan 15 · E3)

    // Poké Jobs (Modes/Jobs.cs, not part of `all`): a Pokémon Center's PC asking which to open under the modern rules
    // (j01), the board empty (j02), a Bidoof of the boxes being sent to the orchard (j03), three away a few hours later
    // (j04), and the Bidoof brought back the next morning with what it earned (j05). The wall clock is the harness's
    // own, moved on by hand and put back after.
    public void JobsMode()
    {
        var fixedAt = WallClock.Fixed;
        DateTime now = WallClock.UtcNow;
        void At_(double hours)
        {
            WallClock.Fixed = fixedAt!.Value.AddHours(hours);
            now = WallClock.UtcNow;
        }

        // Three Pokémon in the boxes to send
        var boxes = game.Boxes;
        foreach (var (species, level) in new[] { ("Bidoof", 12), ("Starly", 10), ("Shinx", 11) })
        {
            var p = new Pokemon(PokemonDatabase.Get(species)!, level, Gender.Male, Nature.Hardy, false);
            boxes.Store(p);
            PokemonSprites.Request(p.LookName);
        }
        PokemonSprites.Flush(game.RenderContext);

        var center = MapDatabase.Get("PokemonCenter");
        var terminal = center.NPCs.First(n => n.IsPCTerminal);
        At("PokemonCenter", terminal.GridX, terminal.GridY + 1, Direction.Up);
        game.Interact(); Frames(3);
        for (int i = 0; i < 300 && !engine.Choice.IsOpen; i++)
        {
            if (game.Dialogue.IsActive && !game.Dialogue.IsQuestion) { game.Dialogue.FinishLine(); game.Dialogue.Advance(); }
            Frames(1);
        }
        // (where the region has a pasture the PC asks first whether to go there, plan 35 · BN5: USE THE PC)
        if (engine.Choice.Options.Contains("GO TO THE PASTURE"))
        {
            Frames(20); engine.Choice.Confirm(); Frames(3);
            for (int i = 0; i < 300 && !engine.Choice.IsOpen; i++) Frames(1);
        }
        Frames(20); Shot("j01_pc_menu");
        Console.WriteLine($"pc: rules {Ruleset.Current.Preset}, the choice open {engine.Choice.IsOpen}");

        engine.Choice.Move(1);
        engine.Choice.Confirm();
        Frames(30); Shot("j02_board");
        var screen = game.Jobs;
        Console.WriteLine($"board: state {game.State}, {screen.Board.Jobs.Count} away, {screen.Boxes.Count} in the boxes");

        // The Bidoof to the orchard for six hours
        screen.Confirm(now);
        Frames(4);
        screen.Confirm(now);
        screen.Sideways(1);
        screen.Move(1);
        for (int i = 0; i < 2; i++) screen.Sideways(-1);
        Frames(20); Shot("j03_posting");
        Console.WriteLine($"setting off: {screen.Chosen?.DisplayName} to {screen.Kind} for {screen.Hours} h");
        screen.Confirm(now);
        // The Starly on an errand of eight hours, the Shinx to the hot spring for three
        foreach (var (kind, hours) in new[] { (JobKind.Errand, 8), (JobKind.HotSpring, 3) })
        {
            screen.Move(1);
            screen.Confirm(now);
            screen.Confirm(now);
            while (screen.Kind != kind) screen.Sideways(1);
            screen.Move(1);
            while (screen.Hours != hours) screen.Sideways(Math.Sign(hours - screen.Hours));
            screen.Confirm(now);
        }
        Console.WriteLine($"away: {string.Join(", ", screen.Board.Jobs.Select(j => $"{j.Pokemon.SpeciesName} {j.Kind} {j.Hours} h"))}; boxes {screen.Boxes.Count}");

        // Four hours on: the hot spring's Shinx is back, the others under way
        At_(4);
        while (screen.Cursor != 1) screen.Move(1);
        Frames(20); Shot("j04_under_way");
        Console.WriteLine($"4 h on: {string.Join(", ", screen.Board.Jobs.Select(j => $"{j.Pokemon.SpeciesName} {j.HoursDone(now)} of {j.Hours}{(j.IsDone(now) ? " back" : "")}"))}");

        // The next morning: the Starly back from its errand, a level higher
        At_(20);
        screen.Confirm(now);
        Frames(30); Shot("j05_collected");
        if (screen.Result is { } back)
            Console.WriteLine($"collected: {back.Pokemon.DisplayName} Lv. {back.Pokemon.Level} (+{back.LevelsGained}), {back.Exp} EXP, {back.Effort} {back.EffortStat}, friendship +{back.Friendship}, berries {string.Join(", ", back.Berries.Select(b => $"{b.Item} x{b.Count}"))}");
        screen.Confirm(now);
        screen.Cancel();
        for (int i = 0; i < 200 && engine.ScriptRunning; i++) Frames(1);
        Console.WriteLine($"after: state {game.State}, script running {engine.ScriptRunning}, {game.JobBoard.Jobs.Count} still away");

        WallClock.Fixed = fixedAt;
    }
}
