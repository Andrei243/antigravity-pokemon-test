using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;
using Xunit;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 12 · Q24: the timing assist the rod's bite asks, the rumble's pulses and when they are asked for, and the two
/// rows. The rumble's strength and the device in hand are the process's, so the class runs with the other shared state.
/// </summary>
[Collection("Shared clocks and dice")]
public class TimingAndRumbleTests
{
    private static readonly WildEncounterEntry Magikarp = new() { SpeciesName = "Magikarp", MinLevel = 5, MaxLevel = 5 };

    /// <summary>A cast played at thirty frames a second until it says something, pressed at <paramref name="pressAfter"/> seconds into the bite (never if null).</summary>
    private static FishingStage Play(FishingRod rod, int seed, TimingAssist assist, float? pressAfter)
    {
        var cast = new FishingAttempt(rod, Magikarp, new Random(seed)) { Assist = assist };
        float sinceBite = 0f;
        for (int frame = 0; frame < 30 * 20 && !cast.Says; frame++)
        {
            bool press = cast.Stage == FishingStage.Hooked && pressAfter is { } after && sinceBite >= after;
            cast.Update(1f / 30f, press);
            if (cast.Stage == FishingStage.Hooked) sinceBite += 1f / 30f;
        }
        return cast.Stage;
    }

    // ------------------------------------------------------------------ the timing assist

    [Fact]
    public void UnderAutomaticEveryBiteIsLandedWithNoPress()
    {
        foreach (var rod in Enum.GetValues<FishingRod>())
            for (int seed = 0; seed < 50; seed++)
                Assert.Equal(FishingStage.Landed, Play(rod, seed, TimingAssist.Automatic, pressAfter: null));
    }

    [Fact]
    public void UnderAutomaticAPressTooSoonIsPassedOver()
    {
        var cast = new FishingAttempt(FishingRod.Good, Magikarp, new Random(1)) { Assist = TimingAssist.Automatic };
        cast.Update(FishingAttempt.CastSeconds, false);
        cast.Update(0.1f, true);
        Assert.Equal(FishingStage.Waiting, cast.Stage);
        // Under the other two it still comes up empty, as the original's
        var plain = new FishingAttempt(FishingRod.Good, Magikarp, new Random(1)) { Assist = TimingAssist.Generous };
        plain.Update(FishingAttempt.CastSeconds, false);
        plain.Update(0.1f, true);
        Assert.Equal(FishingStage.TooSoon, plain.Stage);
    }

    [Fact]
    public void UnderGenerousAPressAtOneAndAHalfTimesTheMomentLands()
    {
        foreach (var rod in Enum.GetValues<FishingRod>())
        {
            float late = FishingAttempt.HookSeconds(rod) * 1.5f;
            Assert.Equal(FishingStage.Landed, Play(rod, 7, TimingAssist.Generous, late));
            // With the row off the same press is too late
            Assert.Equal(FishingStage.GotAway, Play(rod, 7, TimingAssist.Off, late));
        }
    }

    [Fact]
    public void WithTheRowOffTheMomentsAreTheRulings()
    {
        // 45, 30 and 15 frames of the original's thirty a second (sRodTypeHookTimingWindow)
        foreach (var (rod, frames) in new[] { (FishingRod.Old, 45), (FishingRod.Good, 30), (FishingRod.Super, 15) })
        {
            var cast = new FishingAttempt(rod, Magikarp, new Random(1));
            Assert.Equal(frames / 30f, cast.Window);
            Assert.Equal(FishingStage.Landed, Play(rod, 1, TimingAssist.Off, frames / 30f - 0.1f));
        }
        Assert.Equal(TimingAssist.Off, new GameSettings().TimingAssist);
    }

    [Fact]
    public void AScoredPressIsNeverPlayedForThePlayer()
    {
        Assert.Equal(TimingAssist.Generous, TimingAssists.Scored(TimingAssist.Automatic));
        Assert.Equal(TimingAssist.Generous, TimingAssists.Scored(TimingAssist.Generous));
        Assert.Equal(TimingAssist.Off, TimingAssists.Scored(TimingAssist.Off));
        Assert.False(TimingAssists.Pressed(false, windowOpen: true, TimingAssists.Scored(TimingAssist.Automatic)));
        Assert.Equal(2f, TimingAssists.Window(1f, TimingAssist.Generous));
        Assert.Equal(1f, TimingAssists.Window(1f, TimingAssist.Off));
    }

    // ------------------------------------------------------------------ the rumble

    [Fact]
    public void EveryPulseIsNamedAfterASoundOrIsOneOfItsOwn()
    {
        Assert.All(Haptics.Pulses, p =>
        {
            Assert.True(p.Key == Haptics.Critical || SoundBank.Exists(p.Key), p.Key);
            Assert.InRange(p.Value.Left, 0f, 1f);
            Assert.InRange(p.Value.Right, 0f, 1f);
            Assert.InRange(p.Value.Seconds, 0.01f, 1f);
        });
        Assert.False(SoundBank.Exists(Haptics.Critical));
        // Light is half of Full
        var hit = Haptics.Pulses["hit_normal"];
        Assert.Equal(hit.Left / 2f, Haptics.Scaled(hit, Rumble.Light).Left);
        Assert.Equal(hit.Seconds, Haptics.Scaled(hit, Rumble.Light).Seconds);
    }

    /// <summary>Feels what <paramref name="play"/> asks of the pad with the row and the device given; puts both back.</summary>
    /// <summary>
    /// Feels what <paramref name="play"/> asks of the pad with the row and the device given (the list fills as it plays,
    /// so <paramref name="play"/> may look at it on the way); puts both back.
    /// </summary>
    private static List<string> Feel(Rumble strength, InputDevice device, Action<List<string>> play)
    {
        var (wasStrength, wasDevice) = (Haptics.Strength, InputManager.LastDevice);
        Haptics.Strength = strength;
        InputManager.LastDevice = device;
        var felt = Haptics.Listen();
        try
        {
            play(felt);
            return felt.ToList();
        }
        finally
        {
            Haptics.StopListening();
            Haptics.Strength = wasStrength;
            InputManager.LastDevice = wasDevice;
        }
    }

    [Fact]
    public void ASoundBringsItsPulseOnlyWithTheRowOnAndThePadInHand()
    {
        void Bite(List<string> _) => AudioManager.PlaySound("fish_bite");
        Assert.Equal(new[] { "fish_bite" }, Feel(Rumble.Full, InputDevice.Pad, Bite));
        Assert.Equal(new[] { "fish_bite" }, Feel(Rumble.Light, InputDevice.Pad, Bite));
        Assert.Empty(Feel(Rumble.Off, InputDevice.Pad, Bite));
        Assert.Empty(Feel(Rumble.Full, InputDevice.Keyboard, Bite));
        // A sound with no pulse brings none
        Assert.Empty(Feel(Rumble.Full, InputDevice.Pad, _ => AudioManager.PlaySound("cursor")));
    }

    [Fact]
    public void AHitsPulseComesAtItsImpactNotAtTheLunge()
    {
        var battle = Battle(Mon("Snorlax", 50, "Tackle"), Mon("Snorlax", 50));
        bool feltAtTheLine = true;
        var felt = Feel(Rumble.Full, InputDevice.Pad, sofar =>
        {
            battle.SelectMove(0);
            for (int i = 0; i < 400 && !(battle.IsWaitingForConfirm && battle.CurrentMessage.Contains("used Tackle")); i++)
            {
                battle.ConfirmMessage();
                battle.Update(1f / 60f);
            }
            Assert.Contains("used Tackle", battle.CurrentMessage);
            // The line is up and the lunge has begun: no hit is felt yet
            feltAtTheLine = sofar.Contains("hit_normal");
            // A third of a second on, it lands
            for (int i = 0; i < 30; i++) battle.Update(1f / 60f);
        });
        Assert.False(feltAtTheLine);
        Assert.Contains("hit_normal", felt);
        // With the row off a whole turn asks the pad for nothing
        Assert.Empty(Feel(Rumble.Off, InputDevice.Pad, _ => Turn(Battle(Mon("Snorlax", 50, "Tackle"), Mon("Snorlax", 50)))));
        // Nor with the keyboard in hand
        Assert.Empty(Feel(Rumble.Full, InputDevice.Keyboard, _ => Turn(Battle(Mon("Snorlax", 50, "Tackle"), Mon("Snorlax", 50)))));
    }


    [Fact]
    public void ACriticalHitHasItsOwnPulse()
    {
        var felt = Feel(Rumble.Full, InputDevice.Pad, _ =>
        {
            var rolls = new PokemonPlatinumEngine.Battle.Sim.BattleRandom(1)
                .Force(PokemonPlatinumEngine.Battle.Sim.RollKind.Critical, 0)
                .Force(PokemonPlatinumEngine.Battle.Sim.RollKind.Damage, 0)
                .Force(PokemonPlatinumEngine.Battle.Sim.RollKind.Accuracy, 0);
            Turn(Battle(Mon("Snorlax", 50, "Tackle"), Mon("Snorlax", 50), rolls));
        });
        Assert.Contains(Haptics.Critical, felt);
    }

    // ------------------------------------------------------------------ the rows

    [Fact]
    public void TheTwoRowsStandBeforeHelpAndTurnRound()
    {
        var rows = OptionsScreen.AllRows.ToArray();
        Assert.True(Array.IndexOf(rows, OptionRow.TimingAssist) < Array.IndexOf(rows, OptionRow.Help));
        Assert.True(Array.IndexOf(rows, OptionRow.Rumble) < Array.IndexOf(rows, OptionRow.Help));
        var s = new GameSettings();
        Assert.Equal((TimingAssist.Off, Rumble.Off), (s.TimingAssist, s.Rumble));
        var seen = new List<TimingAssist>();
        for (int i = 0; i < 3; i++) { OptionsScreen.Change(s, OptionRow.TimingAssist, 1); seen.Add(s.TimingAssist); }
        Assert.Equal(new[] { TimingAssist.Generous, TimingAssist.Automatic, TimingAssist.Off }, seen);
        OptionsScreen.Change(s, OptionRow.Rumble, -1);
        Assert.Equal(Rumble.Full, s.Rumble);
        foreach (var row in new[] { OptionRow.TimingAssist, OptionRow.Rumble })
        {
            var (label, value, help) = OptionsScreen.Describe(s, row);
            Assert.False(string.IsNullOrWhiteSpace(label));
            Assert.False(string.IsNullOrWhiteSpace(value));
            Assert.False(string.IsNullOrWhiteSpace(help));
        }
    }

    [Fact]
    public void TheOptionsFileKeepsBothRows()
    {
        string folder = Path.Combine(Path.GetTempPath(), "pp-q24-" + Guid.NewGuid().ToString("N"));
        try
        {
            string path = Path.Combine(folder, "settings.json");
            new GameSettings { TimingAssist = TimingAssist.Automatic, Rumble = Rumble.Light }.Save(path);
            var read = GameSettings.Load(path);
            Assert.Equal((TimingAssist.Automatic, Rumble.Light), (read.TimingAssist, read.Rumble));
            read.ResetToDefaults();
            Assert.Equal((TimingAssist.Off, Rumble.Off), (read.TimingAssist, read.Rumble));
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }
}
