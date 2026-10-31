using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// The Max Move finisher (plan 25 · YD2) and Max Guard, the countdown and the fall (plan 25 · YD3); style guide, "The
/// Max Move finisher", "Max Guard", "Its end, the fall", "The three turns seen" and "Camera (G8)", "A Max Move". None of
/// it moves a rule: what is checked is the screen's alone.
/// </summary>
public class GiantFinisherTests
{
    private const float Frame = 1f / 60f;
    private const float I = BattleAnimator.ImpactTime;

    private static IEnumerable<MoveData> MaxMoves => MoveDatabase.GetAll().Where(m => m.Kind is MoveKind.MaxMove or MoveKind.GMaxMove);

    private static EffectCue MaxCue(MoveData move, BattleSide from = BattleSide.Player, bool missed = false, bool blocked = false, bool onGuard = false)
    {
        bool self = move.Name == "Max Guard";
        var to = self ? from : from == BattleSide.Player ? BattleSide.Enemy : BattleSide.Player;
        return new BattleAnimator().Cue(new EffectCue
        {
            Kind = CueKind.Move, Move = move.Name, Type = move.Type, Category = move.Category, MaxMove = true,
            FromSide = from, ToSide = to, Missed = missed, Blocked = blocked, OnGuard = onGuard
        });
    }

    private static FxList Draw(EffectCue cue, float age, FxPlaces? places = null)
    {
        cue.Age = age;
        var fx = new FxList();
        MoveFx.Play(cue, places ?? new FxPlaces(), fx);
        return fx;
    }

    private static BattleAnimator Standing()
    {
        var anim = new BattleAnimator();
        anim.Appear(BattleSide.Player, Mon("Charizard", 50, "Flamethrower"));
        anim.Appear(BattleSide.Enemy, Mon("Snorlax", 50, "Body Slam"));
        anim.Update(2f, (_, _) => null);
        return anim;
    }

    // ------------------------------------------------------------------ YD2: the hit-stop

    [Fact]
    public void ALandedMaxMoveHoldsAtItsImpactAndStillEndsOnTime()
    {
        var flare = MoveDatabase.Get("Max Flare")!;
        var cue = MaxCue(flare);
        Assert.Equal(Directions.MaxStop, Directions.StopOf(cue));
        float last = -1f;
        for (float age = 0f; age <= cue.Length; age += 0.005f)
        {
            cue.Age = age;
            float shown = cue.ShownAge;
            Assert.True(shown >= last - 1e-5f, $"backward at {age}");
            last = shown;
            if (age <= I) Assert.Equal(age, shown);
            else if (age < I + Directions.MaxStop) Assert.Equal(I, shown);
        }
        cue.Age = cue.Length;
        Assert.Equal(cue.Length, cue.ShownAge, 4);
        // A miss, a blow that came to nothing and Max Guard hold nothing
        Assert.Equal(0f, Directions.StopOf(MaxCue(flare, missed: true)));
        Assert.Equal(0f, Directions.StopOf(MaxCue(flare, blocked: true)));
        Assert.Equal(0f, Directions.StopOf(MaxCue(MoveDatabase.Get("Max Guard")!)));
    }

    // ------------------------------------------------------------------ YD2: the shatter and the shake

    [Fact]
    public void EveryMaxMoveWithItsFinisherIsUnderTheCapsAtEveryAge()
    {
        var over = new List<string>();
        foreach (var move in MaxMoves)
            foreach (var from in new[] { BattleSide.Player, BattleSide.Enemy })
            {
                var cue = MaxCue(move, from);
                for (float age = 0f; age < cue.Length; age += Frame)
                {
                    var fx = Draw(cue, age);
                    if (fx.Count > MoveFx.MaxQuads) over.Add($"{move.Name} draws {fx.Count} at {age:0.00}");
                    if (fx.Flash > 0.35f) over.Add($"{move.Name} flashes {fx.Flash:0.00}");
                    if (fx.Shake > 0.5f + 1e-4f) over.Add($"{move.Name} shakes {fx.Shake:0.00}");
                }
                // The same blow on a guard is under the caps too
                var guarded = MaxCue(move, from, blocked: true, onGuard: true);
                for (float age = 0f; age < guarded.Length; age += Frame)
                    if (Draw(guarded, age).Count > MoveFx.MaxQuads) over.Add($"{move.Name} on a guard draws too much at {age:0.00}");
            }
        Assert.Empty(over);
    }

    [Fact]
    public void TheFinisherShakesAPhysicalMoveToTheCapAndThrowsItsPieces()
    {
        EffectCue Knuckle(MoveCategory category) => new BattleAnimator().Cue(new EffectCue
        {
            Kind = CueKind.Move, Move = "Max Knuckle", Type = PokemonType.Fighting, Category = category, MaxMove = true,
            FromSide = BattleSide.Player, ToSide = BattleSide.Enemy
        });
        var physical = Knuckle(MoveCategory.Physical);
        var special = Knuckle(MoveCategory.Special);
        float Peak(EffectCue c) => Enumerable.Range(0, 90).Max(i => Draw(c, i * Frame).Shake);
        Assert.Equal(MoveFx.MaxShakePhysical, Peak(physical), 2);
        Assert.Equal(MoveFx.MaxShakeSpecial, Peak(special), 2);
        // The shatter is the type's own shape (a fist for Fighting): none before the impact, twelve after, gone 0.75 s on
        int Fists(float age) => Draw(physical, age).Quads.Count(q => q.Shape == FxShape.Fist);
        Assert.Equal(0, Fists(I - 0.05f));
        Assert.True(Fists(I + Directions.MaxStop + 0.2f) >= MoveFx.ShatterPieces, $"{Fists(I + 0.3f)} fists");
        Assert.True(Fists(I + Directions.MaxStop + 0.85f) <= 1);
    }

    // ------------------------------------------------------------------ YD2: the target thrown

    [Fact]
    public void AMaxMovesTargetIsThrownAndExactlyStillBeforeAndAfter()
    {
        var anim = Standing();
        var cue = anim.Cue(new EffectCue
        {
            Kind = CueKind.Move, Move = "Max Flare", Type = PokemonType.Fire, Category = MoveCategory.Special, MaxMove = true,
            FromSide = BattleSide.Player, ToSide = BattleSide.Enemy
        });
        Assert.Equal(BodyMotion.Still, BodyMotions.Of(anim, BattleSide.Enemy, 0));
        anim.Update(I + 0.06f, (_, _) => null);
        var thrown = BodyMotions.Of(anim, BattleSide.Enemy, 0);
        Assert.InRange(thrown.Toward, -Directions.MaxThrowReach - 0.001f, -Directions.MaxThrowReach * 0.9f);
        Assert.InRange(thrown.Shape.Height, 0.84f, 0.87f);
        // The user is not thrown
        Assert.Equal(0f, BodyMotions.Of(anim, BattleSide.Player, 0).Toward);
        anim.Update(Directions.MaxStop + 0.6f, (_, _) => null);
        Assert.Equal(BodyMotion.Still, BodyMotions.Of(anim, BattleSide.Enemy, 0));
        // A G-Max Move whose mark falls presses it down; one whose mark rises tosses it up
        var falling = Directions.MaxThrown(new EffectCue { Kind = CueKind.Move, Move = "G-Max Volt Crash", MaxMove = true }, I + 0.1f);
        Assert.True(falling.Sink > 0f && falling.Toward == 0f);
        var rising = Directions.MaxThrown(new EffectCue { Kind = CueKind.Move, Move = "G-Max Cuddle", MaxMove = true }, I + 0.2f);
        Assert.True(rising.Up > 0f);
    }

    // ------------------------------------------------------------------ YD2: the box punched

    [Fact]
    public void TheGhostBarNeverShowsMoreThanTheBoxHadAndDrainsAfterAMoment()
    {
        var anim = Standing();
        var v = anim[BattleSide.Enemy, 0];
        var foe = v.Shown!;
        float before = v.DisplayedHp;
        anim.Punch(BattleSide.Enemy, 0);
        foe.CurrentHP = foe.MaxHP / 3;
        float held = -1f;
        for (float t = 0f; t < 3f; t += Frame)
        {
            anim.Update(Frame, (_, _) => null);
            if (v.PunchAge < 0f) break;
            Assert.InRange(v.GhostHp, v.DisplayedHp, before);
            var (_, ghost, _) = ModernUi.PunchOf(v);
            Assert.True(ghost <= before / foe.MaxHP + 1e-4f);
            if (v.PunchAge < BattleAnimator.GhostHold) Assert.Equal(before, v.GhostHp);
            else if (held < 0f) held = t;
        }
        Assert.Equal(-1f, v.PunchAge);
        Assert.Equal(-1f, ModernUi.PunchOf(v).Ghost);
        Assert.Equal(Vector2.Zero, ModernUi.PunchOf(v).Jolt);
    }

    [Fact]
    public void ALandedMaxMovePunchesTheBoxOnScreenAndAnOrdinaryMoveDoesNot()
    {
        var charizard = Mon("Charizard", 50, "Flamethrower");
        charizard.DynamaxLevel = 10;
        var battle = Battle(charizard, "Snorlax");
        battle.ToggleLaterMechanic();
        battle.SelectMove(0);
        bool punched = false;
        for (int i = 0; i < 900 && !punched; i++)
        {
            if (battle.IsWaitingForConfirm) battle.ConfirmMessage();
            battle.Update(Frame);
            punched |= battle.Anim.Enemy.PunchAge >= 0f;
        }
        Assert.True(punched, "the foe's box was never punched");
    }

    private static BattleEngine Battle(Pokemon mine, string foe, string foeMove = "Splash")
    {
        var party = new Party();
        party.Add(mine);
        var bag = new Inventory();
        bag.AddItem(ItemDatabase.Get(BattleCore.DynamaxBandItem)!);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = bag, Pokedex = new Pokedex(),
            WildPokemon = new List<Pokemon> { Mon(foe, 50, foeMove) }, Random = Calm(), Rules = Ruleset.Modern
        });
        Settle(battle);
        battle.SelectMainMenuOption(0);
        return battle;
    }

    // ------------------------------------------------------------------ YD3: Max Guard struck

    [Fact]
    public void ABlowOnMaxGuardStopsAtTheDomeAndRingsIt()
    {
        var places = new FxPlaces();
        var tackle = MoveDatabase.Get("Tackle")!;
        EffectCue Blow(bool onGuard) => new BattleAnimator().Cue(new EffectCue
        {
            Kind = CueKind.Move, Move = tackle.Name, Type = tackle.Type, Category = tackle.Category,
            FromSide = BattleSide.Enemy, ToSide = BattleSide.Player, Blocked = true, OnGuard = onGuard
        });
        var plain = Blow(false);
        var guarded = Blow(true);
        guarded.Seed = plain.Seed;
        var c = new FxCtx(guarded, places);
        float stop = places.HalfWidth(BattleSide.Player, 0) + places.Height(BattleSide.Player, 0) * 0.2f;
        Assert.Equal(stop, Vector3.Distance(c.Aim, c.To), 3);
        Assert.True(Vector3.Distance(c.Aim, c.From) < Vector3.Distance(c.To, c.From), "the blow stops short, toward its user");
        // Nothing of the dome before the impact; the dome and its sparks at it
        Assert.Equal(Draw(plain, I - 0.05f).Count, Draw(guarded, I - 0.05f).Count);
        Assert.True(Draw(guarded, I + 0.1f).Count >= Draw(plain, I + 0.1f).Count + 14);
        Assert.Equal(Draw(plain, I + 0.7f).Count, Draw(guarded, I + 0.7f).Count);
        Assert.True(SoundBank.Exists("max_guard_hit") && SoundBank.IsSetPiece("max_guard_hit"));
    }

    [Fact]
    public void TheDomeRingsOnScreenForABlowItStops()
    {
        // The player's giant puts up Max Guard (from a status move), the foe's Tackle comes to nothing on it
        var charizard = Mon("Charizard", 50, "Growl");
        charizard.DynamaxLevel = 10;
        var battle = Battle(charizard, "Snorlax", "Tackle");
        battle.ToggleLaterMechanic();
        battle.SelectMove(0);
        EffectCue? rung = null;
        bool guardUp = false;
        for (int i = 0; i < 1200 && rung == null; i++)
        {
            if (battle.IsWaitingForConfirm) battle.ConfirmMessage();
            battle.Update(Frame);
            guardUp |= battle.Anim.Player.MaxGuarded;
            rung ??= battle.Anim.Cues.FirstOrDefault(c => c.OnGuard);
        }
        Assert.True(guardUp, "Max Guard never went up");
        Assert.NotNull(rung);
        Assert.Equal("Tackle", rung!.Move);
        Assert.True(rung.Blocked && !rung.Missed);
        // Down again as the menu settles
        for (int i = 0; i < 1200 && battle.HUD.MenuState != BattleMenuState.Main; i++)
        {
            if (battle.IsWaitingForConfirm) battle.ConfirmMessage();
            battle.Update(Frame);
        }
        Assert.False(battle.Anim.Player.MaxGuarded);
    }

    // ------------------------------------------------------------------ YD3: the three turns

    [Fact]
    public void ThePillTicksDownOnceATurnAndNeverUp()
    {
        var anim = Standing();
        anim.Dynamax(BattleSide.Enemy, 0, gigantamax: false, played: false);
        var v = anim[BattleSide.Enemy, 0];
        Assert.Equal(3, v.DynamaxTurnsLeft);
        Assert.Equal(-1f, v.TickAge);
        anim.SettleDynamax(BattleSide.Enemy, 0, true, 3, false);
        Assert.Equal(-1f, v.TickAge);
        foreach (int turns in new[] { 2, 1 })
        {
            anim.SettleDynamax(BattleSide.Enemy, 0, true, turns, false);
            Assert.Equal(0f, v.TickAge);
            Assert.Equal(turns + 1, v.TickFrom);
            var (from, to, k) = ModernUi.TickOf(v);
            Assert.Equal((turns + 1, turns, 0f), (from, to, k));
            anim.Update(BattleAnimator.TickTime + Frame, (_, _) => null);
            Assert.Equal(-1f, v.TickAge);
            Assert.Equal((turns, turns, 1f), ModernUi.TickOf(v));
            // The same count settled again ticks nothing
            anim.SettleDynamax(BattleSide.Enemy, 0, true, turns, false);
            Assert.Equal(-1f, v.TickAge);
        }
        Assert.True(anim.GiantOnItsLastTurn);
        // Never up
        anim.SettleDynamax(BattleSide.Enemy, 0, true, 3, false);
        Assert.Equal(-1f, v.TickAge);
        anim.SettleDynamax(BattleSide.Enemy, 0, false, 0, false);
        Assert.False(anim.GiantOnItsLastTurn);
    }

    [Fact]
    public void TheWreathThinsAndQuickensWithEachTurnUnderItsCap()
    {
        Assert.Equal(new[] { 6, 5, 4 }, new[] { 3, 2, 1 }.Select(MoveFx.WreathClouds));
        Assert.Equal(new[] { 1.2f, 1.6f, 2.2f }, new[] { 3, 2, 1 }.Select(MoveFx.PulseHz));
        var anim = Standing();
        anim.Dynamax(BattleSide.Enemy, 0, gigantamax: true, played: false);
        int last = int.MaxValue;
        foreach (int turns in new[] { 3, 2, 1 })
        {
            anim.SettleDynamax(BattleSide.Enemy, 0, true, turns, true);
            int most = 0;
            for (float t = 0f; t < 1f; t += 0.05f)
            {
                anim.Update(0.05f, (_, _) => null);
                var fx = new FxList();
                MoveFx.Giants(anim, new FxPlaces(), fx);
                Assert.InRange(fx.Count, 1, MoveFx.GiantCap);
                most = Math.Max(most, fx.Count);
            }
            Assert.True(most < last, $"{turns} turns left draws {most}, no fewer than before");
            last = most;
        }
        Assert.True(Ambience.Entries.Any(e => e.Bed == AmbienceBed.Giant));
    }

    // ------------------------------------------------------------------ YD3: the fall

    [Fact]
    public void AnEndShuddersFallsInAndIsItsOwnSizeOnTimeUnderTheCaps()
    {
        var anim = Standing();
        anim.Dynamax(BattleSide.Enemy, 0, gigantamax: false, played: false);
        var v = anim[BattleSide.Enemy, 0];
        var cue = anim.Undynamax(BattleSide.Enemy, 0)!;
        Assert.False(cue.Fell);
        Assert.Equal(BattleAnimator.DynamaxScaleFoe, BattleAnimator.GiantOf(v, false));
        anim.Update(0.15f, (_, _) => null);
        Assert.NotEqual(0f, BodyMotions.Of(anim, BattleSide.Enemy, 0).Toward);
        Assert.Equal(BattleAnimator.DynamaxScaleFoe, BattleAnimator.GiantOf(v, false));
        anim.Update(BattleAnimator.DynamaxShrunk - 0.15f, (_, _) => null);
        Assert.Equal(BattleAnimator.DynamaxFallLow, BattleAnimator.GiantOf(v, false), 2);
        Assert.Equal(0f, BodyMotions.Of(anim, BattleSide.Enemy, 0).Toward);
        anim.Update(BattleAnimator.DynamaxShrinkTime - BattleAnimator.DynamaxShrunk + Frame, (_, _) => null);
        Assert.Equal(1f, BattleAnimator.GiantOf(v, false));
        // Every age of the fall and of a faint's under the caps and pure, with no flash and no shake
        foreach (bool fell in new[] { false, true })
        {
            var c = new BattleAnimator().Cue(new EffectCue { Kind = CueKind.DynamaxEnd, Fell = fell, FromSide = BattleSide.Enemy, ToSide = BattleSide.Enemy });
            for (float age = 0f; age < c.Length; age += Frame)
            {
                var fx = Draw(c, age);
                Assert.InRange(fx.Count, 0, MoveFx.MaxQuads);
                Assert.Equal(0f, fx.Flash);
                Assert.Equal(0f, fx.Shake);
                Assert.Equal(fx.Count, Draw(c, age).Count);
            }
        }
        Assert.True(SoundBank.Exists("giant_fall") && SoundBank.IsSetPiece("giant_fall"));
    }

    [Fact]
    public void AGiantThatFaintsSinksIntoItsCloudsBeforeTheFaintsShot()
    {
        var anim = Standing();
        anim.Dynamax(BattleSide.Enemy, 0, gigantamax: false, played: false);
        var v = anim[BattleSide.Enemy, 0];
        anim.Faint(BattleSide.Enemy, 0.15f, 0);
        // The fall is its end: the rules' line of the end, coming after, plays nothing more
        var cue = Assert.Single(anim.Cues, c => c.Kind == CueKind.DynamaxEnd);
        Assert.True(cue.Fell);
        Assert.False(v.Dynamaxed);
        Assert.Null(anim.Undynamax(BattleSide.Enemy, 0));
        Assert.Equal(0.15f + BattleAnimator.GiantFallTime, v.FaintDelay, 4);
        var places = new FxPlaces();
        float faintShotAt = -1f;
        for (float t = 0f; t < 3f && v.Present; t += Frame)
        {
            var kind = BattleCamera.Direct(anim, places).Kind;
            if (kind == ShotKind.Faint && faintShotAt < 0f) faintShotAt = t;
            if (t < 0.15f + BattleAnimator.GiantFallTime) Assert.NotEqual(ShotKind.Faint, kind);
            anim.Update(Frame, (_, _) => null);
        }
        // It sank before the faint's own shot came, and the faint ended
        Assert.True(faintShotAt >= 0.15f + BattleAnimator.GiantFallTime - Frame, $"the faint's shot at {faintShotAt:0.00}");
        Assert.False(v.Present);
        // A recall shrinks it with nothing played
        var other = Standing();
        other.Dynamax(BattleSide.Enemy, 0, gigantamax: false, played: false);
        other.Recall(BattleSide.Enemy);
        Assert.Null(other.Undynamax(BattleSide.Enemy, 0));
        // With the scene off a faint does not wait
        var off = Standing();
        off.Dynamax(BattleSide.Enemy, 0, gigantamax: false, played: false);
        off.FallsAsGiant = false;
        off.Faint(BattleSide.Enemy, 0.15f, 0);
        Assert.Equal(0.15f, off[BattleSide.Enemy, 0].FaintDelay, 4);
    }
}

/// <summary>The Max Move's camera with the motion number at its two ends: a static, so these run alone.</summary>
[Collection("Shared clocks and dice")]
public class GiantFinisherCameraTests
{
    private const float Step = 0.01f;

    private static BattleAnimator Standing(float ownHeight, float foeHeight, FxPlaces places)
    {
        var anim = new BattleAnimator();
        anim.Appear(BattleSide.Player, Scenario.Mon("Charizard", 50, "Flamethrower"));
        anim.Appear(BattleSide.Enemy, Scenario.Mon("Snorlax", 50, "Body Slam"));
        anim.Update(2f, (_, _) => null);
        places.Set(BattleSide.Player, 0, BattleStage.PlayerSpot + new Vector3(0, BattleStage.PlatformHeight, 0), ownHeight);
        places.Set(BattleSide.Enemy, 0, BattleStage.EnemySpot + new Vector3(0, BattleStage.PlatformHeight, 0), foeHeight);
        return anim;
    }

    [Theory]
    [InlineData(BattleSide.Player, 0.6f, 2.2f), InlineData(BattleSide.Player, 1.6f, 4.4f), InlineData(BattleSide.Player, 3.0f, 1.2f)]
    [InlineData(BattleSide.Enemy, 0.6f, 2.2f), InlineData(BattleSide.Enemy, 1.6f, 4.4f), InlineData(BattleSide.Enemy, 3.0f, 1.2f)]
    public void TheMaxMoveShotNeverCrossesNorNearsThePlayersPlatform(BattleSide from, float own, float foe)
    {
        float was = BattleCamera.Motion;
        try
        {
            BattleCamera.Motion = 1f;
            foreach (bool missed in new[] { false, true })
            {
                var places = new FxPlaces();
                var anim = Standing(own, foe, places);
                var cue = anim.Cue(new EffectCue
                {
                    Kind = CueKind.Move, Move = "Max Strike", Type = PokemonType.Normal, Category = MoveCategory.Physical, MaxMove = true,
                    FromSide = from, ToSide = from == BattleSide.Player ? BattleSide.Enemy : BattleSide.Player, Missed = missed
                });
                Vector3? last = null;
                bool sawWhip = false;
                for (float age = 0f; age < cue.Length; age += Step)
                {
                    var (shot, kind, _) = BattleCamera.Direct(anim, places);
                    if (kind == ShotKind.MaxMove)
                    {
                        Assert.True(float.IsFinite(shot.Position.X + shot.Position.Y + shot.Position.Z + shot.Fov));
                        Assert.InRange(shot.Fov, 5f, BattleCamera.WidestFov + 0.01f);
                        Assert.True(shot.Position.Y > BattleStage.PlatformHeight, $"under the ground at {age:0.00}");
                        // A frame that stays where it was or moves a little never passes the player's platform
                        if (last is { } p && Vector3.Distance(p, shot.Position) < 0.5f)
                            Assert.False(BattleCamera.CrossesPlayerSide(p, shot.Position), $"by the player's platform at {age:0.00} ({from}, missed {missed})");
                        if (age > BattleCamera.CutTime && age < I) sawWhip = true;
                        last = shot.Position;
                    }
                    else last = null;
                    anim.Update(Step, (_, _) => null);
                }
                Assert.True(sawWhip);
            }
        }
        finally { BattleCamera.Motion = was; }
    }

    private const float I = BattleAnimator.ImpactTime;

    [Fact]
    public void TheMaxMoveShotHoldsToItsLetGoAndTheOrdinaryShotsHaveItUnderNoMotion()
    {
        float was = BattleCamera.Motion;
        try
        {
            var places = new FxPlaces();
            BattleCamera.Motion = 1f;
            var anim = Standing(1.6f, 2.2f, places);
            anim.Cue(new EffectCue
            {
                Kind = CueKind.Move, Move = "Max Flare", Type = PokemonType.Fire, Category = MoveCategory.Special, MaxMove = true,
                FromSide = BattleSide.Player, ToSide = BattleSide.Enemy
            });
            anim.Update(0.1f, (_, _) => null);
            Assert.Equal(ShotKind.MaxMove, BattleCamera.Direct(anim, places).Kind);
            // Held through the punch-in and the pull-back, past an ordinary move's end
            anim.Update(1.1f, (_, _) => null);
            Assert.Equal(ShotKind.MaxMove, BattleCamera.Direct(anim, places).Kind);
            anim.Update(BattleCamera.MaxLetGo - 1.2f + 0.05f, (_, _) => null);
            Assert.Equal(ShotKind.Overview, BattleCamera.Direct(anim, places).Kind);

            // Under no motion: the ordinary move's shots, no hold, and no jolt of the box
            BattleCamera.Motion = 0f;
            var still = Standing(1.6f, 2.2f, places);
            var cue = still.Cue(new EffectCue
            {
                Kind = CueKind.Move, Move = "Max Flare", Type = PokemonType.Fire, Category = MoveCategory.Special, MaxMove = true,
                FromSide = BattleSide.Player, ToSide = BattleSide.Enemy
            });
            Assert.Equal(0f, Directions.StopOf(cue));
            still.Update(0.1f, (_, _) => null);
            Assert.Equal(ShotKind.Attacker, BattleCamera.Direct(still, places).Kind);
            still.Update(1.0f, (_, _) => null);
            Assert.Equal(ShotKind.Overview, BattleCamera.Direct(still, places).Kind);
            still.Punch(BattleSide.Enemy, 0);
            still.Update(0.05f, (_, _) => null);
            Assert.Equal(Vector2.Zero, ModernUi.PunchOf(still[BattleSide.Enemy, 0]).Jolt);
        }
        finally { BattleCamera.Motion = was; }
    }
}
