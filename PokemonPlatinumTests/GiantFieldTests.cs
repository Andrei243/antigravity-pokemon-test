using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using Raylib_cs;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Max Moves on the field and Mega Dynamax's double change (plan 25 · YD4; style guide, "Max Moves on the field", "A
/// Mega Dynamax" and "Camera (G8)", "A Mega Dynamax"). What a Max Move brings arrives from its blow and lasts as it
/// always did; a Mega Dynamax is told by its form's kind and shows both halves; the camera never goes back to the
/// overview between its Mega's light and its growth. None of it moves a rule.
/// </summary>
public class GiantFieldTests
{
    private const float Frame = 1f / 60f;

    private static readonly FieldCondition[] Terrains =
        { FieldCondition.ElectricTerrain, FieldCondition.GrassyTerrain, FieldCondition.MistyTerrain, FieldCondition.PsychicTerrain };

    private static readonly SideCondition[] GMaxSides =
        { SideCondition.GMaxSteelsurge, SideCondition.GMaxWildfire, SideCondition.GMaxCannonade, SideCondition.GMaxVineLash, SideCondition.GMaxVolcalith };

    private static readonly Place Foe = new(BattleSide.Enemy, 0), Mine = new(BattleSide.Player, 0);

    // ------------------------------------------------------------------ what lasts

    [Fact]
    public void WhatAMaxMoveBringsLastsAsItWouldHaveAndOnlyComesOtherwise()
    {
        var places = new FxPlaces();
        foreach (var from in new[] { Foe, Mine })
        {
            foreach (var terrain in Terrains)
            {
                var plain = new FieldLook();
                var blown = new FieldLook();
                plain.Tell(new FieldConditionChanged(terrain, true));
                blown.Tell(new FieldConditionChanged(terrain, true), from);
                Assert.Equal(from, blown.Of(terrain).From);
                Assert.True(blown.Of(terrain).ArrivingFromABlow);
                Assert.Equal(plain.Of(terrain).Seed, blown.Of(terrain).Seed);
                // Once it has arrived, the two are drawn alike, quad for quad
                plain.Advance(BattleAnimator.BlowArrivalTime + 0.01f);
                blown.Advance(BattleAnimator.BlowArrivalTime + 0.01f);
                Assert.False(blown.Of(terrain).ArrivingFromABlow);
                Same(Draw(fx => BattleFieldFx.Field(terrain, plain.Of(terrain), 3.2f, places, fx)),
                    Draw(fx => BattleFieldFx.Field(terrain, blown.Of(terrain), 3.2f, places, fx)), $"{terrain} from {from}");
            }
            foreach (var kind in GMaxSides)
            {
                var plain = new FieldLook();
                var blown = new FieldLook();
                var side = from.Side == BattleSide.Player ? BattleSide.Enemy : BattleSide.Player;
                plain.Tell(new SideConditionChanged(side, kind, 1));
                blown.Tell(new SideConditionChanged(side, kind, 1), from);
                plain.Advance(BattleAnimator.BlowArrivalTime + 0.01f);
                blown.Advance(BattleAnimator.BlowArrivalTime + 0.01f);
                Same(Draw(fx => BattleFieldFx.Side(side, kind, plain.Of(side, kind), 3.2f, places, fx)),
                    Draw(fx => BattleFieldFx.Side(side, kind, blown.Of(side, kind), 3.2f, places, fx)), $"{kind} from {from}");
            }
        }
        // Settled with the rules, nothing arrives from anywhere
        var settled = new FieldLook();
        settled.Tell(new FieldConditionChanged(FieldCondition.ElectricTerrain, true), Foe);
        settled.Settle(new FieldState());
        Assert.Null(settled.Of(FieldCondition.ElectricTerrain).From);
    }

    [Fact]
    public void AnArrivalFromABlowIsPureCappedAndOnTheGround()
    {
        var places = new FxPlaces();
        foreach (var from in new[] { Foe, Mine })
        {
            void Check(string name, BattleEvent told, int cap, Action<FieldLook, FxList> draw)
            {
                var plain = new FieldLook();
                var blown = new FieldLook();
                plain.Tell(told);
                blown.Tell(told, from);
                var feet = places.Feet(from.Side, from.Slot);
                bool reachedOut = false;
                for (float age = 0f; age < BattleAnimator.BlowArrivalTime + 0.1f; age += 0.01f)
                {
                    var b = Draw(fx => draw(blown, fx));
                    var again = Draw(fx => draw(blown, fx));
                    Assert.Equal(b.Count, again.Count);
                    Assert.True(b.Count <= cap + BattleFieldFx.ArrivalCap, $"{name} from {from}: {b.Count} quads at {age:0.00}");
                    // It comes out across the ground from the struck Pokémon's feet: a ring lying there
                    reachedOut |= b.Quads.Any(q => q.Shape == FxShape.Ring && q.Normal == Vector3.UnitY
                        && Vector2.Distance(new Vector2(q.Center.X, q.Center.Z), new Vector2(feet.X, feet.Z)) < 0.01f && MathF.Abs(q.Center.Y - feet.Y) < 0.1f);
                    plain.Advance(0.01f);
                    blown.Advance(0.01f);
                }
                Assert.True(reachedOut, $"{name} from {from} drew no arrival");
                // After it, nothing more
                Assert.Equal(Draw(fx => draw(plain, fx)).Count, Draw(fx => draw(blown, fx)).Count);
            }
            foreach (var terrain in Terrains)
                Check(terrain.ToString(), new FieldConditionChanged(terrain, true), BattleFieldFx.Cap(terrain), (look, fx) => BattleFieldFx.Field(terrain, look.Of(terrain), 1f, places, fx));
            foreach (var kind in GMaxSides)
            {
                var side = from.Side == BattleSide.Player ? BattleSide.Enemy : BattleSide.Player;
                Check(kind.ToString(), new SideConditionChanged(side, kind, 1), BattleFieldFx.Cap(kind), (look, fx) => BattleFieldFx.Side(side, kind, look.Of(side, kind), 1f, places, fx));
            }
        }
    }

    [Fact]
    public void SteelsurgesSplintersAreThrownFromTheBlowToTheirPlaces()
    {
        var places = new FxPlaces();
        var look = new FieldLook();
        look.Tell(new SideConditionChanged(BattleSide.Enemy, SideCondition.GMaxSteelsurge, 1), Foe);
        look.Advance(0.05f);
        var early = Draw(fx => BattleFieldFx.Side(BattleSide.Enemy, SideCondition.GMaxSteelsurge, look.Of(BattleSide.Enemy, SideCondition.GMaxSteelsurge), 1f, places, fx));
        var body = places.Body(Foe.Side, Foe.Slot);
        var shards = early.Quads.Where(q => q.Shape == FxShape.Shard).ToList();
        Assert.NotEmpty(shards);
        // Near the impact as they set off, not already over the rim
        Assert.All(shards, q => Assert.True(Vector3.Distance(q.Center, body) < BattleStage.EnemyPlatformRadius * 0.9f, $"a splinter at {q.Center} far from the blow at {body}"));
    }

    [Fact]
    public void AWeatherFromABlowRingsOutFromTheTargetUnderTheCapsAndPure()
    {
        var places = new FxPlaces();
        foreach (var weather in Enum.GetValues<BattleWeather>().Where(w => w != BattleWeather.None))
            foreach (var from in new[] { Foe, Mine })
            {
                var anim = new BattleAnimator();
                var cue = anim.WeatherBegins(weather, from);
                Assert.True(cue.FromBlow);
                Assert.Equal(from.Side, cue.ToSide);
                var plainCue = new BattleAnimator().WeatherBegins(weather);
                Assert.False(plainCue.FromBlow);
                Assert.Equal(cue.Seed, plainCue.Seed);
                bool rang = false;
                for (float age = 0f; age < BattleAnimator.WeatherCueTime; age += 0.02f)
                {
                    cue.Age = plainCue.Age = age;
                    var fx = Draw(f => MoveFx.Play(cue, places, f));
                    var again = Draw(f => MoveFx.Play(cue, places, f));
                    Assert.Equal(fx.Count, again.Count);
                    Assert.True(fx.Count <= MoveFx.MaxQuads, $"{weather}: {fx.Count} quads at {age:0.00}");
                    Assert.InRange(fx.Flash, 0f, 0.35f);
                    rang |= fx.Quads.Any(q => q.Shape == FxShape.Ring && q.Normal == Vector3.UnitY && q.Size > 3f
                        && Vector2.Distance(new Vector2(q.Center.X, q.Center.Z), new Vector2(places.Feet(from.Side, 0).X, places.Feet(from.Side, 0).Z)) < 0.01f);
                    if (age >= ArenaFx.WeatherArrivalTime)
                    {
                        var plain = Draw(f => MoveFx.Play(plainCue, places, f));
                        Assert.True(fx.Count <= plain.Count, $"{weather}: the arrival outlasted {ArenaFx.WeatherArrivalTime} s");
                    }
                }
                Assert.True(rang, $"{weather} from {from}: no ring ran out from the target");
            }
    }

    [Fact]
    public void OnScreenMaxFlaresSunComesFromItsBlow()
    {
        var charizard = Mon("Charizard", 50, "Flamethrower");
        charizard.DynamaxLevel = 10;
        var battle = Battle(charizard, "Snorlax");
        battle.ToggleLaterMechanic();
        battle.SelectMove(0);
        EffectCue? sun = null;
        for (int i = 0; i < 1500 && sun == null; i++)
        {
            if (battle.IsWaitingForConfirm) battle.ConfirmMessage();
            battle.Update(Frame);
            sun = battle.Anim.Cues.FirstOrDefault(c => c.Kind == CueKind.Weather);
        }
        Assert.NotNull(sun);
        Assert.True(sun!.FromBlow, "Max Flare's sun was not brought from its blow");
        Assert.Equal(BattleSide.Enemy, sun.ToSide);
    }

    // ------------------------------------------------------------------ Mega Dynamax

    [Fact]
    public void AMegaDynamaxGrowsInTheMegasFourColoursAndAPlainOneInRed()
    {
        var places = new FxPlaces();
        foreach (bool mega in new[] { false, true })
            foreach (var side in new[] { BattleSide.Player, BattleSide.Enemy })
            {
                var cue = new BattleAnimator().Cue(new EffectCue { Kind = CueKind.Dynamax, MegaMax = mega, FromSide = side, ToSide = side, Move = "Transform" });
                var colours = new List<Color>();
                for (float age = 0f; age < cue.Length; age += 0.01f)
                {
                    cue.Age = age;
                    var fx = Draw(f => MoveFx.Play(cue, places, f));
                    Assert.True(fx.Count <= MoveFx.MaxQuads, $"{fx.Count} quads at {age:0.00}");
                    Assert.InRange(fx.Flash, 0f, 0.35f);
                    Assert.Equal(0f, fx.Shake);
                    colours.AddRange(fx.Quads.Where(q => q.Color.A > 40).Select(q => q.Color));
                }
                // Violet and cyan are the Mega's: a plain Dynamax has neither, a Mega Dynamax both
                bool violet = colours.Any(c => c.B > 200 && c.R > 150 && c.G < 140);
                bool cyan = colours.Any(c => c.B > 200 && c.G > 180 && c.R < 160);
                Assert.Equal(mega, violet);
                Assert.Equal(mega, cyan);
                // The mark: a white ring on the body for the Mega Dynamax alone
                cue.Age = BattleAnimator.DynamaxGrown + 0.05f;
                var at = Draw(f => MoveFx.Play(cue, places, f));
                Assert.Equal(mega, at.Quads.Any(q => q.Shape == FxShape.Ring && q.Normal == Vector3.Zero && q.Color.R > 240 && q.Color.G > 240 && q.Color.B > 240));
            }
    }

    [Fact]
    public void AMegaDynamaxsWreathAndEndAreInTheMegasColours()
    {
        var places = new FxPlaces();
        foreach (bool mega in new[] { false, true })
        {
            var anim = new BattleAnimator();
            anim.Appear(BattleSide.Enemy, Mon("Garchomp", 50));
            anim.Dynamax(BattleSide.Enemy, 0, gigantamax: false, played: false, megaMax: mega);
            Assert.Equal(mega, anim[BattleSide.Enemy, 0].MegaMax);
            var fx = Draw(f => MoveFx.Giants(anim, places, f));
            Assert.True(fx.Count <= MoveFx.GiantCap);
            Assert.Equal(mega, fx.Quads.Any(q => q.Color.B > 200 && q.Color.G > 180 && q.Color.R < 160 || q.Color.B > 150 && q.Color.R > 120 && q.Color.G < 90));
            var end = anim.Undynamax(BattleSide.Enemy, 0)!;
            Assert.Equal(mega, end.MegaMax);
            Assert.False(anim[BattleSide.Enemy, 0].MegaMax);
        }
    }

    [Fact]
    public void TheCameraNeverGoesBackToTheOverviewBetweenAMegasLightAndItsGrowth()
    {
        float was = BattleCamera.Motion;
        try
        {
            BattleCamera.Motion = 1f;
            foreach (var side in new[] { BattleSide.Enemy, BattleSide.Player })
            {
                var anim = new BattleAnimator();
                anim.Appear(BattleSide.Player, Mon("Blissey", 50));
                anim.Appear(BattleSide.Enemy, Mon("Blissey", 50));
                anim.Appear(side, Mon("Garchomp", 50));
                anim.Update(2f, (_, _) => null);
                var places = new FxPlaces();
                anim.Change(side, 0, CueKind.Mega, megaMax: true);
                // The Mega's light, then the lines between (however long they are read), then the growth
                for (float t = 0f; t < BattleAnimator.MegaTime + 2.5f; t += 0.05f)
                {
                    Assert.Equal(ShotKind.Giant, BattleCamera.Direct(anim, places).Kind);
                    anim.Update(0.05f, (_, _) => null);
                }
                var growth = anim.Dynamax(side, 0, gigantamax: false, megaMax: true)!;
                Assert.True(growth.MegaMax);
                Assert.Null(anim.MegaMaxHold);
                Assert.Equal(ShotKind.Giant, BattleCamera.Direct(anim, places).Kind);
            }
            // A plain Mega Evolution keeps its own shots, and a menu lets a held Mega go
            var plain = new BattleAnimator();
            plain.Appear(BattleSide.Enemy, Mon("Garchomp", 50));
            plain.Update(2f, (_, _) => null);
            plain.Change(BattleSide.Enemy, 0, CueKind.Mega);
            Assert.Null(plain.MegaMaxHold);
            Assert.NotEqual(ShotKind.Giant, BattleCamera.Direct(plain, new FxPlaces()).Kind);
            var held = new BattleAnimator();
            held.Appear(BattleSide.Enemy, Mon("Garchomp", 50));
            held.Change(BattleSide.Enemy, 0, CueKind.Mega, megaMax: true);
            held.LetGoOfMegaMax();
            held.Update(BattleAnimator.MegaTime + 0.1f, (_, _) => null);
            held.Update(1f, (_, _) => null);
            Assert.Equal(ShotKind.Overview, BattleCamera.Direct(held, new FxPlaces()).Kind);
        }
        finally
        {
            BattleCamera.Motion = was;
        }
    }

    [Fact]
    public void OnScreenAMegaDynamaxIsToldByItsFormAndItsMegaHoldsTheGiantShot()
    {
        var garchomp = Mon("Garchomp", 50, "Dragon Claw");
        garchomp.HeldItem = ItemDatabase.Get("Garchompite");
        garchomp.DynamaxLevel = 10;
        var party = new Party();
        party.Add(garchomp);
        var bag = new Inventory();
        bag.AddItem(ItemDatabase.Get(BattleCore.KeyStoneItem)!);
        bag.AddItem(ItemDatabase.Get(BattleCore.DynamaxBandItem)!);
        bag.AddItem(ItemDatabase.Get(BattleCore.MegaMaxBandItem)!);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = bag, Pokedex = new Pokedex(),
            WildPokemon = new List<Pokemon> { Mon("Snorlax", 50, "Splash") }, Random = Calm(), Rules = Ruleset.Modern
        });
        Settle(battle);
        battle.SelectMainMenuOption(0);
        Assert.True(battle.ToggleLaterMechanic());
        Assert.True(battle.ToggleLaterMechanic());
        Assert.True(battle.MegaDynamaxArmed, "the Mega Dynamax was not armed");
        battle.SelectMove(0);
        bool sawHold = false;
        EffectCue? growth = null;
        for (int i = 0; i < 1500 && growth == null; i++)
        {
            if (battle.IsWaitingForConfirm) battle.ConfirmMessage();
            battle.Update(Frame);
            sawHold |= battle.Anim.MegaMaxHold != null;
            growth = battle.Anim.Cues.FirstOrDefault(c => c.Kind == CueKind.Dynamax);
        }
        Assert.True(sawHold, "the Mega was never told it would Mega Dynamax");
        Assert.NotNull(growth);
        Assert.True(growth!.MegaMax, "the growth was not told a Mega Dynamax");
        Assert.True(battle.Anim[BattleSide.Player, 0].MegaMax);
    }

    // ------------------------------------------------------------------ helpers

    private static BattleEngine Battle(Pokemon mine, string foe)
    {
        var party = new Party();
        party.Add(mine);
        var bag = new Inventory();
        bag.AddItem(ItemDatabase.Get(BattleCore.DynamaxBandItem)!);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = bag, Pokedex = new Pokedex(),
            WildPokemon = new List<Pokemon> { Mon(foe, 50, "Splash") }, Random = Calm(), Rules = Ruleset.Modern
        });
        Settle(battle);
        battle.SelectMainMenuOption(0);
        return battle;
    }

    private static FxList Draw(Action<FxList> draw)
    {
        var fx = new FxList();
        draw(fx);
        return fx;
    }

    private static void Same(FxList a, FxList b, string what)
    {
        Assert.Equal(a.Count, b.Count);
        for (int i = 0; i < a.Quads.Count; i++)
        {
            Assert.True(Vector3.Distance(a.Quads[i].Center, b.Quads[i].Center) < 1e-4f, $"{what}: quad {i} stands elsewhere");
            Assert.Equal(a.Quads[i].Color.A, b.Quads[i].Color.A);
        }
    }
}
