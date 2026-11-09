using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using Xunit;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 45 · BC7: a recorded battle watched (the Vs. Recorder, plan 08 · P11) is filmed by the watcher's options. It has
/// no commands, so it runs no cycle; its opening, closing, pushes, reactions and splits are the face's as ever, and
/// nothing of the camera reaches the lines or their pace.
/// </summary>
[Collection("Shared clocks and dice")]
public class ReplayCameraTests
{
    private const float Frame = 1f / 60f;
    private static readonly DateTime Day = new(2026, 10, 20);

    private static BattleRecordFile Recorded()
    {
        var mine = new Party();
        mine.Add(Mon("Monferno", 20, "Ember", "Mach Punch", "Taunt"));
        mine.Add(Mon("Staravia", 18, "Wing Attack", "Quick Attack"));
        var trainer = new Trainer { Id = "replay_camera", Name = "Tristan", TrainerClass = "Youngster", PrizeMoney = 64, Ai = AiFlags.Basic };
        trainer.Party.Add(Mon("Shinx", 17, "Tackle", "Leer", "Spark"));
        trainer.Party.Add(Mon("Budew", 18, "Absorb", "Stun Spore", "Mega Drain"));
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = mine, Inventory = new Inventory(), Pokedex = new Pokedex(), Trainers = { trainer },
            Random = new BattleRandom(20261020), Rules = Ruleset.Platinum, PlayerName = "Lucas"
        });
        Settle(battle);
        for (int turn = 0; turn < 60 && !battle.IsBattleOver; turn++)
        {
            if (battle.IsChoosingReplacement)
                battle.SelectSwitch(Enumerable.Range(0, mine.Count).First(i => !mine.Members[i].IsFainted && battle.PlayerSlots.All(b => b.Pokemon != mine.Members[i])));
            else battle.SelectMove(0);
            Settle(battle);
        }
        Assert.True(battle.IsBattleOver);
        return battle.RecordFile(Day)!;
    }

    private static FxPlaces PlacesOf(BattleAnimator anim)
    {
        var places = new FxPlaces();
        foreach (var side in new[] { BattleSide.Player, BattleSide.Enemy })
            for (int slot = 0; slot < anim.SlotsOf(side); slot++)
                places.Set(side, slot, BattlePlaces.Spot(side, slot, anim.SlotsOf(side), anim.Spread(side)) + Vector3.UnitY * BattleStage.PlatformHeight,
                    1.2f * BattlePlaces.Fit(anim.SlotsOf(side)));
        return places;
    }

    /// <summary>Watches to the end, a director of a manner filming every frame (none for no camera): the lines shown, the frames taken, what the camera showed.</summary>
    private static (List<string> Lines, int Frames, HashSet<ShotKind> Kinds, bool Split, float MenuAge) Watch(BattleRecordFile file, CameraManner? manner)
    {
        var watched = BattleEngine.ForReplay(file);
        var camera = manner == null ? null : new BattleCamera { Manner = manner };
        var lines = new List<string>();
        var kinds = new HashSet<ShotKind>();
        bool waiting = false, split = false;
        int frames = 0;
        float menuAge = -1f;
        for (; frames < 60 * 60 * 20 && !watched.WatchOver; frames++)
        {
            watched.Watch(Frame, fast: false, leave: false);
            if (watched.IsWaitingForConfirm && !waiting) lines.Add(watched.CurrentMessage);
            waiting = watched.IsWaitingForConfirm;
            if (camera == null) continue;
            var places = PlacesOf(watched.Anim);
            camera.Update(Frame, watched.Anim, places, 0f, choosing: watched.HUD.MenuState != BattleMenuState.Message);
            kinds.Add(camera.Kind);
            split |= BattleCamera.SplitNow(watched.Anim, places, manner) != null;
            menuAge = Math.Max(menuAge, watched.Anim.MenuAge);
        }
        return (lines, frames, kinds, split, menuAge);
    }

    [Fact]
    public void ARecordingWatchedIsFilmedByTheWatchersMannerAndReadsTheSameWhateverItIs()
    {
        var file = Recorded();
        var plain = Watch(file, null);
        Assert.NotEmpty(plain.Lines);
        foreach (var manner in new[] { CameraManners.Classic, CameraManners.Find("sinnoh")!, CameraManners.Find("galar")! })
        {
            var filmed = Watch(file, manner);
            // The same lines at the same frames: the camera never held a line or hurried one
            Assert.Equal(plain.Lines, filmed.Lines);
            Assert.Equal(plain.Frames, filmed.Frames);
            // A watched battle has no commands: no cycle ever ran
            Assert.Equal(-1f, filmed.MenuAge);
            Assert.DoesNotContain(ShotKind.Idle, filmed.Kinds);
            Assert.DoesNotContain(ShotKind.OwnFront, filmed.Kinds);
        }
        // Classic films as it always has; Galar opens, closes and splits (a trainer's battle) and Sinnoh opens and closes
        var classic = Watch(file, CameraManners.Classic);
        Assert.DoesNotContain(ShotKind.Opening, classic.Kinds);
        Assert.False(classic.Split);
        var galar = Watch(file, CameraManners.Find("galar")!);
        Assert.Contains(ShotKind.Opening, galar.Kinds);
        Assert.True(galar.Split, "Galar's manner splits a recorded trainer's battle at its leads");
        var sinnoh = Watch(file, CameraManners.Find("sinnoh")!);
        Assert.Contains(ShotKind.Opening, sinnoh.Kinds);
    }

    [Fact]
    public void AWatchersReduceMotionFilmsARecordingAsClassic()
    {
        var file = Recorded();
        BattleCamera.Motion = 0f;
        try
        {
            var filmed = Watch(file, CameraManners.Find("galar")!);
            Assert.False(filmed.Split);
            Assert.DoesNotContain(ShotKind.Opening, filmed.Kinds);
            Assert.DoesNotContain(ShotKind.Closing, filmed.Kinds);
        }
        finally { BattleCamera.Motion = 1f; }
    }
}
