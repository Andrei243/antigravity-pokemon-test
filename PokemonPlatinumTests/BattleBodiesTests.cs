using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Bodies that answer (plan 25 · Y5; style guide, "Bodies that answer"): a miss dodged, tiredness in the red, a cheer
/// for a win, a catch or the other side's loss, a body gathered through a charge turn, and the eyes' order. The rules
/// say one thing more (<see cref="Charging"/> on a charge turn's line) and change nothing by it.
/// </summary>
public class BattleBodiesTests
{
    private const float Frame = 1f / 60f;

    private static readonly string[] ChargeMoves = { "Solar Beam", "Razor Wind", "Skull Bash", "Sky Attack", "Fly", "Dig", "Dive", "Bounce", "Shadow Force" };

    // ------------------------------------------------------------------ the rules' one event

    /// <summary>Plays a turn on the bare core and hands back everything it wrote.</summary>
    private static List<BattleEvent> TurnLog(BattleCore core, int mine = 0, int foe = 0)
    {
        var asked = Assert.IsType<ActionRequest>(core.Request).Places;
        var choices = new List<BattleChoice>();
        if (asked.Contains(Mine)) choices.Add(BattleChoice.Fight(Mine, mine));
        if (asked.Contains(Foe)) choices.Add(BattleChoice.Fight(Foe, foe));
        core.Submit(choices);
        return core.TakeLog();
    }

    private static IEnumerable<(Said Line, Charging Charge)> Charges(IEnumerable<BattleEvent> log) =>
        log.OfType<Said>().SelectMany(said => said.Shows.Concat(said.OnImpact).OfType<Charging>().Select(c => (said, c)));

    [Theory]
    [InlineData("Solar Beam")]
    [InlineData("Razor Wind")]
    [InlineData("Skull Bash")]
    [InlineData("Sky Attack")]
    [InlineData("Fly")]
    [InlineData("Dig")]
    [InlineData("Dive")]
    [InlineData("Bounce")]
    [InlineData("Shadow Force")]
    public void EveryChargeTurnCarriesItsChargingOnItsLine(string move)
    {
        var core = Wild(Mon("Giratina", 60, move), Idling(Mon("Blissey", 60)));
        var first = TurnLog(core, 0, 0);
        var charge = Assert.Single(Charges(first));
        Assert.Equal(new Charging(Mine, move), charge.Charge);
        // On the line that tells of the charge, beside the hop it stands in for, and on nothing else
        Assert.Contains(charge.Line.Shows, e => e is Lunged { Category: MoveCategory.Status } l && l.Place == Mine);
        Assert.DoesNotContain(first, e => e is Charging);

        // The turn the move is let go carries none
        var second = TurnLog(core, 0, 0);
        Assert.Empty(Charges(second));
        Assert.Contains(second.OfType<Said>(), s => s.Shows.Any(e => e is MoveShown shown && shown.Move == move));
    }

    [Fact]
    public void AChargeTheSunSkipsCarriesNone()
    {
        var core = Wild(Mon("Venusaur", 50, "Solar Beam"), Idling(Mon("Blissey", 50)), sky: BattleWeather.Sun);
        Assert.Empty(Charges(TurnLog(core, 0, 0)));
    }

    [Fact]
    public void ABattleWithAChargeReplaysTheSame()
    {
        static BattleCore Fresh(uint seed) => new(new CoreSetup
        {
            PlayerParty = PartyOf(Mon("Rhyhorn", 30, "Skull Bash", "Tackle")), WildPokemon = new List<Pokemon> { Mon("Bibarel", 30, "Tackle", "Rollout") },
            Random = new BattleRandom(seed), Rules = Ruleset.Platinum, PlayerName = "Lucas"
        });
        var core = Fresh(77);
        core.Start();
        for (int i = 0; i < 12 && core.Request is ActionRequest asked; i++)
            core.Submit(asked.Places.Select(p => BattleChoice.Fight(p, 0)).ToList());
        Assert.Contains(core.Log.OfType<Said>(), s => s.Shows.Any(e => e is Charging));

        var again = Fresh(77);
        again.Replay(core.Record!);
        Assert.Equal(Golden.BattleText.Written(core), Golden.BattleText.Written(again));
    }

    private static Party PartyOf(Pokemon p)
    {
        var party = new Party();
        party.Add(p);
        return party;
    }

    // ------------------------------------------------------------------ the dodge

    /// <summary>Reads a battle on a frame at a time, confirming each line, until <paramref name="until"/> holds.</summary>
    private static bool ReadOn(BattleEngine battle, Func<bool> until, int frames = 3000)
    {
        for (int i = 0; i < frames; i++)
        {
            if (until()) return true;
            if (battle.IsBattleOver) return until();
            if (battle.IsWaitingForConfirm && i % 20 == 19) battle.ConfirmMessage();
            battle.Update(Frame);
        }
        return until();
    }

    [Fact]
    public void AMissIsDodgedAwayFromItsPath()
    {
        var battle = Battle(Mon("Luxray", 40, "Stone Edge"), Idling(Mon("Bibarel", 30)), Steady().Force(RollKind.Accuracy, 99));
        battle.SelectMove(0);
        var foe = battle.Anim[BattleSide.Enemy, 0];
        Assert.True(ReadOn(battle, () => foe.DodgeAge >= 0f), "the foe never dodged");
        Assert.Equal((BattleSide.Player, 0), (foe.DodgeFromSide, foe.DodgeFromSlot));
        Assert.True(battle.Anim[BattleSide.Player, 0].DodgeAge < 0f);
        // It is over by its length, back where it stood
        for (int i = 0; i < (int)(BattleAnimator.DodgeTime * 60f) + 2; i++) battle.Anim.Update(Frame, (_, _) => null);
        Assert.True(foe.DodgeAge < 0f);
    }

    [Fact]
    public void AHitIsNotDodgedNorAMissWithTheSceneOff()
    {
        var hit = Battle(Mon("Luxray", 40, "Stone Edge"), Idling(Mon("Bibarel", 30)));
        hit.SelectMove(0);
        Assert.False(ReadOn(hit, () => hit.Anim[BattleSide.Enemy, 0].DodgeAge >= 0f, 400));

        var party = new Party();
        party.Add(Mon("Luxray", 40, "Stone Edge"));
        var off = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(), WildPokemon = new List<Pokemon> { Idling(Mon("Bibarel", 30)) },
            Random = Steady().Force(RollKind.Accuracy, 99), Rules = Ruleset.Platinum, Scene = false
        });
        Settle(off);
        off.SelectMove(0);
        Assert.False(ReadOn(off, () => off.Anim[BattleSide.Enemy, 0].DodgeAge >= 0f, 400));
    }

    [Fact]
    public void TheDodgeIsClearOfTheMissesPathAtTheImpact()
    {
        var places = new FxPlaces();
        var mineFeet = new Vector3(-3f, 0.2f, 6f);
        var foeFeet = new Vector3(4f, 0.4f, -5f);
        places.Set(BattleSide.Player, 0, mineFeet, 1.2f);
        places.Set(BattleSide.Enemy, 0, foeFeet, 2.2f);
        foreach (var (from, to, fromFeet, toFeet) in new[]
        {
            (BattleSide.Player, BattleSide.Enemy, mineFeet, foeFeet), (BattleSide.Enemy, BattleSide.Player, foeFeet, mineFeet)
        })
        {
            var cue = new EffectCue { Kind = CueKind.Move, Move = "Stone Edge", Missed = true, FromSide = from, ToSide = to };
            var c = new FxCtx(cue, places);
            var away = PokemonAnimation.DodgeAway(fromFeet, toFeet);
            // The miss goes wide on the other side
            Assert.True(Vector3.Dot(c.Aim - c.To, away) < 0f, $"{to}: the miss passes on the side it dodges to");
            foreach (var model in new[] { PokemonModels.Get("Riolu"), PokemonModels.Get("Starly"), PokemonModels.Sample(BodyPlan.Serpent), PokemonModels.Sample(BodyPlan.Fish) })
            {
                float h = c.ToSize;
                float aside = PokemonAnimation.DodgeAside(model, PokemonAnimation.DodgePeak);
                Assert.True(aside >= 0.19f, $"{model.Plan}: only {aside:0.00} of its height aside at the impact");
                Assert.Equal(aside, PokemonAnimation.DodgeAside(model, PokemonAnimation.DodgePeak), 5);
                float Gap(Vector3 body)
                {
                    // From the body to the miss's way, in the ground's plane
                    var a = new Vector2(c.From.X, c.From.Z);
                    var b = new Vector2(c.Aim.X, c.Aim.Z);
                    var p = new Vector2(body.X, body.Z);
                    var ab = b - a;
                    float t = Math.Clamp(Vector2.Dot(p - a, ab) / ab.LengthSquared(), 0f, 1f);
                    return Vector2.Distance(p, a + ab * t);
                }
                float standing = Gap(c.To), dodged = Gap(c.To + away * aside * h);
                Assert.True(dodged > standing + 0.18f * h, $"{model.Plan}: the gap only goes from {standing:0.00} to {dodged:0.00}");
            }
        }
        // Nothing aside at rest, at either end
        var riolu = PokemonModels.Get("Riolu");
        Assert.Equal(0f, PokemonAnimation.DodgeAside(riolu, 0f));
        Assert.Equal(0f, PokemonAnimation.DodgeAside(riolu, 1f));
        Assert.True(PokemonAnimation.DodgeAside(riolu, 0.02f) < 0.01f && PokemonAnimation.DodgeAside(riolu, 0.98f) < 0.01f);
    }

    // ------------------------------------------------------------------ tiredness

    [Fact]
    public void TirednessIsTakenFromTheBarNotFromTheHp()
    {
        var mon = Mon("Bidoof", 30, "Tackle");
        var anim = new BattleAnimator();
        anim.Appear(BattleSide.Player, mon);
        var view = anim[BattleSide.Player, 0];
        Assert.Equal(0f, view.Tired);

        // The rules' HP falls to one; the bar drains toward it, and tiredness waits for the bar
        mon.CurrentHP = 1;
        anim.Update(0.1f, (_, _) => mon);
        Assert.True(view.DisplayedHp > mon.MaxHP * BattleAnimator.LowHpRatio);
        Assert.Equal(0f, view.Tired);
        float waited = 0.1f;
        while (!BattleAnimator.InTheRed(view) && waited < 5f)
        {
            anim.Update(Frame, (_, _) => mon);
            waited += Frame;
        }
        Assert.True(BattleAnimator.InTheRed(view));
        Assert.InRange(view.Tired, 0f, 0.1f);
        for (int i = 0; i < 60; i++) anim.Update(Frame, (_, _) => mon);
        Assert.Equal(1f, view.Tired);

        // Healed, the bar climbs out of the red and tiredness goes
        mon.CurrentHP = mon.MaxHP;
        for (int i = 0; i < 180; i++) anim.Update(Frame, (_, _) => mon);
        Assert.Equal(0f, view.Tired);

        // One that comes in already in the red is tired from the start; one whose bar is at nought is not
        var low = Mon("Bidoof", 30, "Tackle");
        low.CurrentHP = 2;
        anim.SendOut(BattleSide.Enemy, low);
        Assert.Equal(1f, anim[BattleSide.Enemy, 0].Tired);
        Assert.False(BattleAnimator.InTheRed(new CombatantView()));
    }

    // ------------------------------------------------------------------ the cheer

    [Fact]
    public void ThePlayersPokemonCheerAWin()
    {
        var foe = Idling(Mon("Bidoof", 5));
        foe.CurrentHP = 1;
        var battle = Battle(Mon("Luxray", 40, "Spark"), foe);
        battle.SelectMove(0);
        Assert.True(ReadOn(battle, () => battle.Anim[BattleSide.Player, 0].CheerAge >= 0f), "the player's Pokémon never cheered");
        Assert.True(battle.Anim[BattleSide.Enemy, 0].CheerAge < 0f);
    }

    [Fact]
    public void TheFoesPokemonCheerThePlayersLoss()
    {
        var mine = Mon("Bidoof", 5, "Growl");
        mine.CurrentHP = 1;
        var battle = Battle(mine, Mon("Luxray", 40, "Spark"));
        battle.SelectMove(0);
        var foe = battle.Anim[BattleSide.Enemy, 0];
        Assert.True(ReadOn(battle, () => foe.CheerAge >= 0f), "the foe never cheered");
        Assert.True(battle.Anim[BattleSide.Player, 0].CheerAge < 0f);
        // On a line after the fall, not with it
        Assert.DoesNotContain("fainted", battle.CurrentMessage);
    }

    [Fact]
    public void NobodyCheersWhoIsDownOutOfSightOrGone()
    {
        var anim = new BattleAnimator();
        var standing = Mon("Bidoof", 5);
        anim.Appear(BattleSide.Player, standing);
        anim.Cheer(BattleSide.Player);
        Assert.True(anim[BattleSide.Player, 0].CheerAge >= 0f);

        var fainted = Mon("Bidoof", 5);
        fainted.CurrentHP = 0;
        fainted.Status = StatusCondition.Faint;
        anim.Appear(BattleSide.Enemy, fainted);
        anim.Cheer(BattleSide.Enemy);
        Assert.True(anim[BattleSide.Enemy, 0].CheerAge < 0f);
    }

    // ------------------------------------------------------------------ the charge

    [Fact]
    public void AChargeGathersTheBodyUntilTheMoveIsLetGo()
    {
        var foe = Idling(Mon("Blissey", 50));
        var battle = Battle(Mon("Rhyhorn", 40, "Skull Bash"), foe);
        battle.SelectMove(0);
        var mine = battle.Anim[BattleSide.Player, 0];
        Assert.True(ReadOn(battle, () => mine.ChargeAge >= 0f), "it never gathered itself");
        // In place of the hop its line began, with its aura
        Assert.True(mine.AttackAge < 0f);
        Assert.Contains(battle.Anim.Cues, c => c.Charging && c.Move == "Skull Bash" && c.OnSelf);
        battle.Anim.Update(BattleAnimator.ChargeInTime, (_, _) => null);
        Assert.Equal(1f, BattleAnimator.Gathered(mine));

        // Held through the menu, and gone as the blow is struck
        int hp = foe.MaxHP;
        Assert.True(ReadOn(battle, () => foe.CurrentHP < hp), "the blow never landed");
        Assert.True(mine.ChargeAge < 0f);
        Assert.Equal(0f, BattleAnimator.Gathered(mine));
    }

    [Fact]
    public void AChargeThatGoesUpOrUnderGathersNothing()
    {
        var battle = Battle(Mon("Staravia", 40, "Fly"), Idling(Mon("Blissey", 50)));
        battle.SelectMove(0);
        var mine = battle.Anim[BattleSide.Player, 0];
        Assert.True(ReadOn(battle, () => mine.Away));
        Assert.True(mine.ChargeAge < 0f);
        Assert.DoesNotContain(battle.Anim.Cues, c => c.Charging);
    }

    [Fact]
    public void AChargeCueCountsItsOwnSeeds()
    {
        var anim = new BattleAnimator();
        anim.Appear(BattleSide.Player, Mon("Rhyhorn", 40));
        var before = anim.Cue(new EffectCue { Kind = CueKind.Move, Move = "Tackle" });
        anim.Charge(BattleSide.Player, 0, "Skull Bash", PokemonType.Normal, MoveCategory.Physical);
        var after = anim.Cue(new EffectCue { Kind = CueKind.Move, Move = "Tackle" });
        var plain = new BattleAnimator();
        plain.Cue(new EffectCue { Kind = CueKind.Move, Move = "Tackle" });
        Assert.Equal(plain.Cue(new EffectCue { Kind = CueKind.Move, Move = "Tackle" }).Seed, after.Seed);
        Assert.NotEqual(before.Seed, after.Seed);
    }

    [Fact]
    public void AChargesAuraIsPureStaysUnderItsCapAndEnds()
    {
        var places = new FxPlaces();
        places.Set(BattleSide.Player, 0, new Vector3(-3f, 0.2f, 6f), 1.2f);
        var cue = new EffectCue { Kind = CueKind.Move, Charging = true, Move = "Skull Bash", Type = PokemonType.Normal, Seed = 4115 };
        string Drawn(float age)
        {
            cue.Age = age;
            var fx = new FxList();
            MoveFx.Play(cue, places, fx);
            Assert.True(fx.Count <= MoveFx.ChargeCap, $"{fx.Count} at {age}");
            Assert.Equal(0f, fx.Flash);
            Assert.Equal(0f, fx.Shake);
            return string.Join("\n", fx.Quads.Select(q => $"{q.Shape} {q.Center} {q.Size} {q.Color.A}"));
        }
        for (float age = 0f; age < BattleAnimator.EffectTime; age += 0.05f)
            Assert.Equal(Drawn(age), Drawn(age));
        Assert.NotEqual("", Drawn(0.5f));
        Assert.Equal("", Drawn(BattleAnimator.EffectTime));
    }

    // ------------------------------------------------------------------ a stat's change in the eyes

    [Fact]
    public void AStatsChangeShowsInTheEyesForAMoment()
    {
        var anim = new BattleAnimator();
        anim.Appear(BattleSide.Enemy, Mon("Bidoof", 5));
        var view = anim[BattleSide.Enemy, 0];
        anim.StatChange(BattleSide.Enemy, 0, up: false);
        Assert.Equal(-1f, BattleAnimator.StatShown(view));
        Assert.Equal(EyeState.Squeeze, PokemonAnimation.EyesOf(new PokePose { Stat = BattleAnimator.StatShown(view) }));
        anim.StatChange(BattleSide.Enemy, 0, up: true);
        Assert.Equal(EyeState.Fierce, PokemonAnimation.EyesOf(new PokePose { Stat = BattleAnimator.StatShown(view) }));
        anim.Update(BattleAnimator.StatEyesTime + 0.01f, (_, _) => null);
        Assert.Equal(0f, BattleAnimator.StatShown(view));
    }

    // ------------------------------------------------------------------ the eyes' order

    [Fact]
    public void TheEyesFollowTheStyleGuidesOrder()
    {
        EyeState Eyes(PokePose p) => PokemonAnimation.EyesOf(p);
        // 1, 2: a faint and sleep shut them over a cheer, a charge, a stat
        Assert.Equal(EyeState.Shut, Eyes(new PokePose { Faint = 0.5f, Cheer = 0.5f }));
        Assert.Equal(EyeState.Shut, Eyes(new PokePose { Asleep = true, Charge = 1f, Stat = 1f }));
        // 3: a hit squeezes them over a charge and a cheer
        Assert.Equal(EyeState.Squeeze, Eyes(new PokePose { Hurt = 0.3f, Charge = 1f, Cheer = 0.5f }));
        // 4: an attack or a charge is fierce, over a stat's fall
        Assert.Equal(EyeState.Fierce, Eyes(new PokePose { Charge = 1f, Stat = -1f }));
        Assert.Equal(EyeState.Open, Eyes(new PokePose { Charge = 0.2f }));
        // 6: a stat's fall squeezes, a rise is fierce, over frozen and a cheer
        Assert.Equal(EyeState.Squeeze, Eyes(new PokePose { Stat = -1f, Frozen = true, Cheer = 0.5f }));
        Assert.Equal(EyeState.Fierce, Eyes(new PokePose { Stat = 1f, Cheer = 0.5f }));
        // 7: frozen holds them open over a cheer and tiredness
        Assert.Equal(EyeState.Open, Eyes(new PokePose { Frozen = true, Cheer = 0.5f, Tired = 1f, Time = 0.1f }));
        // 8: a cheer smiles, over tiredness and a blink, only while it plays
        Assert.Equal(EyeState.Smile, Eyes(new PokePose { Cheer = 0.5f, Tired = 1f, Blink = 1f }));
        Assert.Equal(EyeState.Open, Eyes(new PokePose { Cheer = 0.99f }));
        // 9: tiredness blinks long and slow in place of the blink
        Assert.Equal(EyeState.Shut, Eyes(new PokePose { Tired = 1f, Time = 0.2f }));
        Assert.Equal(EyeState.Open, Eyes(new PokePose { Tired = 1f, Time = 1.3f, Blink = 1f }));
        Assert.Equal(EyeState.Shut, Eyes(new PokePose { Tired = 1f, Time = PokemonAnimation.TiredBlinkEvery + 0.4f }));
        // 10, 11
        Assert.Equal(EyeState.Shut, Eyes(new PokePose { Blink = 1f, Tired = 0.3f }));
        Assert.Equal(EyeState.Open, Eyes(new PokePose()));
    }

    [Fact]
    public void ASmileIsPaintedAndDiffersFromShut()
    {
        var m = PokemonModels.Get("Piplup");
        var smile = PokemonDecals.Paint(m, EyeState.Smile);
        var shut = PokemonDecals.Paint(m, EyeState.Shut);
        var open = PokemonDecals.Paint(m, EyeState.Open);
        int Opaque(PokemonPlatinumEngine.Graphics.PixelCanvas c, out int differ)
        {
            int n = 0;
            differ = 0;
            for (int d = 0; d < m.Decals.Count; d++)
            {
                if (!m.Decals[d].IsEye) continue;
                int x0 = d % m.AtlasColumns * PokemonDecals.Cell, y0 = d / m.AtlasColumns * PokemonDecals.Cell;
                for (int y = y0; y < y0 + PokemonDecals.Cell; y++)
                    for (int x = x0; x < x0 + PokemonDecals.Cell; x++)
                    {
                        if (c.Get(x, y).A >= 128) n++;
                        if ((c.Get(x, y).A >= 128) != (shut.Get(x, y).A >= 128)) differ++;
                    }
            }
            return n;
        }
        int smiling = Opaque(smile, out int fromShut), opened = Opaque(open, out _);
        Assert.InRange(smiling, 100, opened / 2);
        Assert.True(fromShut > 100, $"the smile differs from the shut eye by {fromShut} pixels");
    }

    // ------------------------------------------------------------------ the clips

    private static readonly string[] Bodies = { "Riolu", "Shinx", "Starly", "Turtwig", "Garchomp", "sample Serpent", "sample Fish", "sample Floating" };

    private static PokeModel ModelOf(string name) =>
        name.StartsWith("sample ") ? PokemonModels.Sample(Enum.Parse<BodyPlan>(name[7..])) : PokemonModels.Get(name);

    private static Matrix4x4[] Posed(PokeModel m, PokePose p)
    {
        m.Animate(p);
        return m.Skin.ToArray();
    }

    private static void Close(PokeModel m, Matrix4x4[] a, Matrix4x4[] b, float within, string what)
    {
        for (int i = 0; i < a.Length; i++)
            Assert.True(Vector3.Distance(Vector3.Transform(Vector3.One, a[i]), Vector3.Transform(Vector3.One, b[i])) < within * Math.Max(1f, m.Height), $"{what}: bone {i}");
    }

    [Theory]
    [MemberData(nameof(BodyNames))]
    public void EveryAnswerStartsAndEndsAtRestAndIsPure(string body)
    {
        var m = ModelOf(body);
        var rest = Posed(m, new PokePose { Time = 1.3f });
        foreach (float t in new[] { 0.001f, 0.999f })
        {
            Close(m, rest, Posed(m, new PokePose { Time = 1.3f, Dodge = t }), 0.01f, $"{body}'s dodge at {t}");
            Close(m, rest, Posed(m, new PokePose { Time = 1.3f, Cheer = t }), 0.01f, $"{body}'s cheer at {t}");
        }
        Close(m, rest, Posed(m, new PokePose { Time = 1.3f, Tired = 0f, Charge = 0f }), 1e-5f, $"{body} with nothing");
        // The same pose, the same body, however often it is asked
        foreach (var pose in new[] { new PokePose { Time = 0.7f, Dodge = 0.4f }, new PokePose { Time = 2.1f, Tired = 1f }, new PokePose { Time = 0.3f, Cheer = 0.3f }, new PokePose { Time = 0.9f, Charge = 0.7f } })
            Close(m, Posed(m, pose), Posed(m, pose), 1e-6f, $"{body} again");
        // And each moves it while it plays
        foreach (var pose in new[] { new PokePose { Time = 1.3f, Dodge = PokemonAnimation.DodgePeak }, new PokePose { Time = 1.3f, Tired = 1f }, new PokePose { Time = 1.3f, Cheer = 0.25f }, new PokePose { Time = 1.3f, Charge = 1f } })
            Assert.Contains(Enumerable.Range(0, rest.Length), i => Vector3.Distance(Vector3.Transform(Vector3.One, rest[i]), Vector3.Transform(Vector3.One, Posed(m, pose)[i])) > 0.01f * m.Height);
    }

    public static IEnumerable<object[]> BodyNames() => Bodies.Select(b => new object[] { b });

    [Theory]
    [InlineData("Riolu")]
    [InlineData("Shinx")]
    [InlineData("Turtwig")]
    public void FeetStayPlantedWhileTiredOrCharging(string species)
    {
        var m = PokemonModels.Get(species);
        foreach (var pose in new[] { new PokePose { Tired = 1f }, new PokePose { Charge = 1f }, new PokePose { Tired = 1f, Charge = 1f } })
            foreach (float t in new[] { 0.3f, 0.9f, 1.7f, 2.6f })
            {
                var p = pose;
                p.Time = t;
                m.Animate(p);
                for (int b = 0; b < m.Bones.Count; b++)
                {
                    if (m.Bones[b].Role != PokeRole.Leg) continue;
                    var foot = Centre(m, b);
                    Assert.True(Vector3.Distance(Vector3.Transform(foot, m.Skin[b]), foot) < 0.002f, $"{species}: a foot slides at {t} s");
                }
            }
    }

    private static Vector3 Centre(PokeModel m, int b)
    {
        var sum = Vector3.Zero;
        int n = 0;
        for (int v = 0; v < m.Mesh.VertexCount; v++)
            if (m.Mesh.WeightOf(v, b) > 0.9f) { sum += m.Mesh.Positions[v]; n++; }
        return n > 0 ? sum / n : m.Skeleton[b].Joint;
    }

    [Theory]
    [InlineData("Riolu")]
    [InlineData("Shinx")]
    [InlineData("Bibarel")]
    public void TirednessLowersTheHeadAndACheerRaisesIt(string species)
    {
        var m = PokemonModels.Get(species);
        int head = m.Find(PokeRole.Head);
        var rest = Centre(m, head);
        float Y(PokePose p)
        {
            m.Animate(p);
            return Vector3.Transform(rest, m.Skin[head]).Y;
        }
        float idle = Y(new PokePose { Time = 0f });
        Assert.True(Y(new PokePose { Time = 0f, Tired = 1f }) < idle - 0.02f * m.Height, $"{species}: tiredness leaves the head up");
        Assert.True(Y(new PokePose { Time = 0f, Charge = 1f }) < idle - 0.02f * m.Height, $"{species}: a charge leaves the head up");
        Assert.True(Y(new PokePose { Time = 0f, Cheer = 0.25f }) > idle + 0.05f * m.Height, $"{species}: a cheer's hop leaves the head down");
    }

    // ------------------------------------------------------------------ an imported model

    [Fact]
    public void AnImportedModelPlaysItsOwnAnswersOrItsBodyPlans()
    {
        var names = new[] { "Idle_Loop", "Evade01", "LowHP_Loop", "Victory01" };
        var clips = ImportedModels.MatchClips(names);
        Assert.Equal(1, clips[ClipRole.Dodge]);
        Assert.Equal(2, clips[ClipRole.Tired]);
        Assert.Equal(3, clips[ClipRole.Cheer]);
        var none = ImportedModels.MatchClips(new[] { "Idle", "Attack01" });
        Assert.False(none.ContainsKey(ClipRole.Dodge) || none.ContainsKey(ClipRole.Tired) || none.ContainsKey(ClipRole.Cheer));

        // Written out and read back, the model carries the three as clips of its own
        var gible = PokemonModels.Build("Gible");
        var asset = GltfReader.Parse(GltfWriter.Write(gible), null);
        var own = ImportedModels.FromAsset(asset, "Gible");
        Assert.True(own.Imported!.Has(ClipRole.Dodge) && own.Imported.Has(ClipRole.Tired) && own.Imported.Has(ClipRole.Cheer));
        var idle = Posed(own, new PokePose { Time = 0.5f });
        Assert.Contains(Enumerable.Range(0, idle.Length), i => Vector3.Distance(Vector3.Transform(Vector3.One, idle[i]), Vector3.Transform(Vector3.One, Posed(own, new PokePose { Time = 0.5f, Tired = 1f })[i])) > 0.005f);

        // Without them, the body plan's answers move it whole
        var bare = GltfReader.Parse(GltfWriter.Write(gible), null);
        bare.Animations.RemoveAll(a => a.Name is "dodge" or "tired" or "cheer");
        var plain = ImportedModels.FromAsset(bare, "Gible");
        Assert.False(plain.Imported!.Has(ClipRole.Tired));
        var still = Posed(plain, new PokePose { Time = 0.5f });
        Assert.Contains(Enumerable.Range(0, still.Length), i => Vector3.Distance(Vector3.Transform(Vector3.One, still[i]), Vector3.Transform(Vector3.One, Posed(plain, new PokePose { Time = 0.5f, Tired = 1f })[i])) > 0.005f);
        Assert.Contains(Enumerable.Range(0, still.Length), i => Vector3.Distance(Vector3.Transform(Vector3.One, still[i]), Vector3.Transform(Vector3.One, Posed(plain, new PokePose { Time = 0.5f, Cheer = 0.25f })[i])) > 0.005f);
    }
}
