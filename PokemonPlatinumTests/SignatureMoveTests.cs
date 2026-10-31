using System;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Signature moves (plan 25 · Y17a): the direction table names real moves, the hit-stop is a shown age that holds at
/// the impact and ends on time, what a direction lays over a look stays under the caps, the four shots keep clear of
/// the player's platform and come home, a heavy hit throws its target, and nothing of it changes an ordinary move.
/// </summary>
public class SignatureMoveTests
{
    private const float I = BattleAnimator.ImpactTime;

    private static EffectCue Cue(string move, float age, BattleSide from = BattleSide.Player, bool missed = false, bool charging = false)
    {
        var data = MoveDatabase.Get(move)!;
        var to = from == BattleSide.Player ? BattleSide.Enemy : BattleSide.Player;
        return new EffectCue
        {
            Kind = CueKind.Move, Move = move, Type = data.Type, Category = data.Category, FromSide = from,
            ToSide = charging ? from : to, Missed = missed, Charging = charging, Seed = 7, Age = age
        };
    }

    private static BattleAnimator Playing(EffectCue cue)
    {
        var anim = new BattleAnimator();
        float age = cue.Age;
        cue.Age = 0f;
        anim.Cue(cue);
        cue.Age = age;
        return anim;
    }

    public static TheoryData<string> Signatures()
    {
        var data = new TheoryData<string>();
        foreach (var move in Directions.Table.Keys) data.Add(move);
        return data;
    }

    // ------------------------------------------------------------------ the table

    [Fact]
    public void EveryMoveOfTheTableIsAMoveOfTheData()
    {
        Assert.DoesNotContain(Directions.Table.Keys, m => MoveDatabase.Get(m) == null);
        Assert.True(Directions.Table.Count >= 6);
        // Each of the four shots is shown by at least one move
        foreach (var shot in new[] { SignatureShot.ChargePush, SignatureShot.FollowProjectile, SignatureShot.ImpactPunch, SignatureShot.Orbit })
            Assert.Contains(Directions.Table.Values, d => d.Shot == shot);
    }

    /// <summary>Y17b's batch: the legendaries' and mythicals' own moves of Sinnoh, Kanto, Johto and Hoenn, and the starters' finals'.</summary>
    public static readonly string[] Y17b =
    {
        "Spacial Rend", "Judgment", "Shadow Force", "Magma Storm", "Crush Grip", "Seed Flare", "Dark Void", "Lunar Dance",
        "Psystrike", "Aeroblast", "Sacred Fire", "Origin Pulse", "Precipice Blades", "Dragon Ascent", "Psycho Boost",
        "Mist Ball", "Luster Purge",
        "Blast Burn", "Hydro Cannon", "Frenzy Plant", "Draco Meteor", "Eruption", "Overheat", "Blaze Kick", "Flare Blitz",
        "Wood Hammer", "Earthquake", "Hydro Pump", "Leaf Storm",
    };

    [Fact]
    public void Y17bsBatchIsInTheTable()
    {
        Assert.DoesNotContain(Y17b, m => !Directions.Has(m));
        Assert.True(Directions.Table.Count >= 35);
    }

    [Theory]
    [MemberData(nameof(Signatures))]
    public void WhatADirectionLaysOverIsOverByTheCuesEnd(string move)
    {
        // Nothing before the impact but on a charge-push's own look, and nothing left once the cue is done
        var gone = new FxList();
        Directions.PlayEffect(new FxCtx(Cue(move, BattleAnimator.EffectTime - 0.01f), new FxPlaces()), MoveFx.ColorsOf(MoveDatabase.Get(move)!.Type), gone);
        Assert.Empty(gone.Quads);
        Assert.Empty(gone.Ribbons);
        var missed = new FxList();
        Directions.PlayEffect(new FxCtx(Cue(move, I + 0.2f, missed: true), new FxPlaces()), MoveFx.ColorsOf(MoveDatabase.Get(move)!.Type), missed);
        Assert.Empty(missed.Quads);
        // Every body motion is still again by the cue's end
        Assert.False(Directions.TargetBody(Cue(move, 1.29f)).Moves);
        Assert.False(Directions.UserBody(Cue(move, 1.29f)).Moves);
    }

    [Theory]
    [MemberData(nameof(Signatures))]
    public void EveryBodyADirectionMovesStaysInBounds(string move)
    {
        for (float age = 0f; age < 1.3f; age += 0.01f)
            foreach (var body in new[] { Directions.UserBody(Cue(move, age)), Directions.TargetBody(Cue(move, age)) })
            {
                Assert.InRange(body.Up, -0.01f, 0.32f);
                Assert.InRange(body.Toward, -0.2f, BodyMotions.RushReach);
                Assert.InRange(body.Sink, 0f, 0.1f);
                Assert.InRange(MathF.Abs(body.Squash), 0f, 0.16f);
                Assert.InRange(body.Grow, 0.95f, 1.06f);
                Assert.True(float.IsFinite(body.Spin));
            }
    }

    [Fact]
    public void Y17bsTargetsAnswerTheirBlows()
    {
        // Precipice Blades tosses its target up; Earthquake jolts it; Judgment presses it down; Crush Grip only shudders it
        Assert.True(Directions.TargetBody(Cue("Precipice Blades", I + 0.3f)).Up > 0.1f);
        Assert.True(Directions.TargetBody(Cue("Earthquake", I + 0.12f)).Moves);
        var pressed = Directions.TargetBody(Cue("Judgment", I + 0.1f));
        Assert.True(pressed.Squash > 0f && pressed.Sink > 0f);
        Assert.Equal(0f, Directions.TargetBody(Cue("Crush Grip", I + 0.1f)).Toward);
        // Dragon Ascent's user rises before the impact and meets the target at it
        Assert.True(Directions.UserBody(Cue("Dragon Ascent", I - 0.12f)).Up > 0.2f);
        Assert.True(Directions.UserBody(Cue("Dragon Ascent", I)).Toward > 0.5f);
    }

    /// <summary>Y17c's batch: the pseudo-legendaries' moves and the starters' finals' of Unova to Paldea (Hisui's three among them).</summary>
    public static readonly string[] Y17c =
    {
        "Outrage", "Stone Edge", "Crunch", "Dragon Rush", "Meteor Mash", "Dragon Claw", "Dark Pulse", "Dragon Pulse",
        "Clanging Scales", "Clangorous Soul", "Dragon Darts", "Glaive Rush",
        "Heat Crash", "Head Smash", "Razor Shell", "Ceaseless Edge", "Leaf Blade", "Hammer Arm", "Mystical Fire",
        "Water Shuriken", "Spirit Shackle", "Triple Arrows", "Darkest Lariat", "Sparkling Aria", "Drum Beating",
        "Pyro Ball", "Snipe Shot", "Flower Trick", "Torch Song", "Aqua Step", "Infernal Parade",
    };

    [Fact]
    public void Y17csBatchIsInTheTable()
    {
        Assert.DoesNotContain(Y17c, m => !Directions.Has(m));
        Assert.Empty(Y17c.Intersect(Y17b));
        Assert.True(Directions.Table.Count >= 35 + Y17c.Length);
    }

    [Fact]
    public void Y17csBodiesAnswer()
    {
        // The three lunges meet their targets at the impact and are home by the cue's end, never rising
        foreach (var move in new[] { "Dragon Rush", "Glaive Rush", "Aqua Step" })
        {
            Assert.True(Directions.UserBody(Cue(move, I)).Toward > 0.5f, move);
            Assert.True(Directions.UserBody(Cue(move, I - 0.15f)).Toward < 0f, move);
            for (float age = 0f; age < 1.3f; age += 0.01f) Assert.Equal(0f, Directions.UserBody(Cue(move, age)).Up);
        }
        // Stone Edge tosses its target; Crunch only shudders it; Heat Crash and Hammer Arm press it; Drum Beating jolts it
        Assert.True(Directions.TargetBody(Cue("Stone Edge", I + 0.3f)).Up > 0.1f);
        Assert.Equal(0f, Directions.TargetBody(Cue("Crunch", I + 0.1f)).Toward);
        foreach (var move in new[] { "Heat Crash", "Hammer Arm" })
            Assert.True(Directions.TargetBody(Cue(move, I + 0.1f)).Squash > 0f, move);
        Assert.True(Directions.TargetBody(Cue("Drum Beating", I + 0.12f)).Moves);
        // Clangorous Soul is on its user alone: no shot of its own, no throw
        Assert.Equal(SignatureShot.None, Directions.Of("Clangorous Soul")!.Shot);
    }

    [Theory]
    [InlineData("Stone Edge"), InlineData("Meteor Mash"), InlineData("Clanging Scales"), InlineData("Glaive Rush"), InlineData("Flower Trick")]
    public void Y17csReactionsComeAfterTheImpactAndGo(string move)
    {
        var col = MoveFx.ColorsOf(MoveDatabase.Get(move)!.Type);
        var before = new FxList();
        Directions.PlayEffect(new FxCtx(Cue(move, I - 0.02f), new FxPlaces()), col, before);
        Assert.Empty(before.Quads);
        var during = new FxList();
        Directions.PlayEffect(new FxCtx(Cue(move, I + 0.2f), new FxPlaces()), col, during);
        Assert.InRange(during.Quads.Count + during.Ribbons.Count, 1, 10);
    }

    /// <summary>Y17d's batch: the legendaries' and mythicals' own moves of Unova, Kalos, Alola, Galar, Hisui and Paldea.</summary>
    public static readonly string[] Y17d =
    {
        "V-create", "Searing Shot", "Sacred Sword", "Secret Sword", "Bleakwind Storm", "Wildbolt Storm", "Sandsear Storm",
        "Blue Flare", "Fusion Flare", "Bolt Strike", "Fusion Bolt", "Glaciate", "Freeze Shock", "Ice Burn", "Relic Song", "Techno Blast",
        "Geomancy", "Oblivion Wing", "Thousand Arrows", "Thousand Waves", "Core Enforcer", "Diamond Storm", "Hyperspace Hole",
        "Hyperspace Fury", "Steam Eruption",
        "Nature's Madness", "Sunsteel Strike", "Moongeist Beam", "Photon Geyser", "Prismatic Laser", "Fleur Cannon", "Spectral Thief",
        "Plasma Fists", "Double Iron Bash",
        "Behemoth Blade", "Behemoth Bash", "Eternabeam", "Dynamax Cannon", "Wicked Blow", "Surging Strikes", "Jungle Healing",
        "Thunder Cage", "Dragon Energy", "Glacial Lance", "Astral Barrage", "Fiery Wrath", "Freezing Glare", "Thunderous Kick",
        "Springtide Storm",
        "Collision Course", "Electro Drift", "Ruination", "Ivy Cudgel", "Tera Starstorm", "Malignant Chain", "Hydro Steam",
        "Psyblade", "Thunderclap", "Mighty Cleave", "Tachyon Cutter",
    };

    [Fact]
    public void Y17dsBatchIsInTheTable()
    {
        Assert.DoesNotContain(Y17d, m => !Directions.Has(m));
        Assert.Empty(Y17d.Intersect(Y17b));
        Assert.Empty(Y17d.Intersect(Y17c));
        Assert.Equal(Y17d.Length, Y17d.Distinct().Count());
        Assert.True(Directions.Table.Count >= 35 + Y17c.Length + Y17d.Length);
    }

    [Fact]
    public void Y17dsBodiesAnswer()
    {
        // The charges meet their targets at the impact and never rise
        foreach (var move in new[] { "V-create", "Bolt Strike", "Sunsteel Strike", "Behemoth Blade", "Behemoth Bash", "Collision Course", "Electro Drift" })
        {
            Assert.True(Directions.UserBody(Cue(move, I)).Toward > 0.5f, move);
            for (float age = 0f; age < 1.3f; age += 0.01f) Assert.Equal(0f, Directions.UserBody(Cue(move, age)).Up);
        }
        // The lances and arrows from above press; the waves of earth jolt; the chain only shudders
        foreach (var move in new[] { "Thousand Arrows", "Glacial Lance", "Double Iron Bash", "Ivy Cudgel" })
            Assert.True(Directions.TargetBody(Cue(move, I + 0.1f)).Squash > 0f, move);
        Assert.True(Directions.TargetBody(Cue("Thousand Waves", I + 0.12f)).Moves);
        Assert.Equal(0f, Directions.TargetBody(Cue("Malignant Chain", I + 0.1f)).Toward);
        // The beams wound up rock their users back
        foreach (var move in new[] { "Eternabeam", "Photon Geyser", "Prismatic Laser", "Fleur Cannon", "Blue Flare", "Dragon Energy" })
            Assert.True(Directions.UserBody(Cue(move, I)).Toward < 0f, move);
        // Geomancy and Jungle Healing are on their own side: no shot of their own
        Assert.Equal(SignatureShot.None, Directions.Of("Geomancy")!.Shot);
        Assert.Equal(SignatureShot.None, Directions.Of("Jungle Healing")!.Shot);
        // The light multi-hits hold nothing
        Assert.False(Directions.Of("Surging Strikes")!.Heavy);
        Assert.False(Directions.Of("Tachyon Cutter")!.Heavy);
    }

    [Theory]
    [InlineData("Bolt Strike"), InlineData("Thunder Cage"), InlineData("Diamond Storm"), InlineData("Prismatic Laser"), InlineData("Geomancy")]
    public void Y17dsReactionsComeAfterTheImpactAndGo(string move)
    {
        var col = MoveFx.ColorsOf(MoveDatabase.Get(move)!.Type);
        var before = new FxList();
        Directions.PlayEffect(new FxCtx(Cue(move, I - 0.02f), new FxPlaces()), col, before);
        Assert.Empty(before.Quads);
        var during = new FxList();
        Directions.PlayEffect(new FxCtx(Cue(move, I + 0.2f), new FxPlaces()), col, during);
        Assert.InRange(during.Quads.Count + during.Ribbons.Count, 1, 10);
    }

    /// <summary>Y17e's batch: the advanced and special moves, the Pledges, the protections and what was left before.</summary>
    public static readonly string[] Y17e =
    {
        "Sky Attack", "Focus Blast", "Blood Moon", "Chloroblast",
        "Shadow Ball", "Flash Cannon", "Barb Barrage", "Bitter Malice", "Esper Wing",
        "Brave Bird", "Headlong Rush", "Wave Crash", "Psyshield Bash",
        "Superpower", "Stone Axe", "Dire Claw", "Mountain Gale", "Dig", "Dive", "Bounce",
        "Fire Pledge", "Water Pledge", "Grass Pledge",
        "Surf", "Blizzard", "Heat Wave", "Whirlpool", "Thrash", "Matcha Gotcha", "Raging Fury",
        "Swords Dance", "Nasty Plot", "Calm Mind", "Dragon Dance", "Victory Dance", "Take Heart", "Lunar Blessing",
        "Spiky Shield", "Burning Bulwark", "Shelter", "Rain Dance", "Sunny Day",
    };

    [Fact]
    public void Y17esBatchIsInTheTable()
    {
        Assert.DoesNotContain(Y17e, m => !Directions.Has(m));
        Assert.Empty(Y17e.Intersect(Y17b));
        Assert.Empty(Y17e.Intersect(Y17c));
        Assert.Empty(Y17e.Intersect(Y17d));
        Assert.Equal(Y17e.Length, Y17e.Distinct().Count());
        Assert.True(Directions.Table.Count >= 35 + Y17c.Length + Y17d.Length + Y17e.Length);
    }

    [Fact]
    public void Y17esBodiesAnswer()
    {
        // The bodies flung in meet their targets and never rise
        foreach (var move in new[] { "Brave Bird", "Headlong Rush", "Wave Crash", "Psyshield Bash" })
        {
            Assert.True(Directions.UserBody(Cue(move, I)).Toward > 0.5f, move);
            for (float age = 0f; age < 1.3f; age += 0.01f) Assert.Equal(0f, Directions.UserBody(Cue(move, age)).Up);
        }
        // Bounce's user rises before the impact and comes down onto its target, which is pressed
        Assert.True(Directions.UserBody(Cue("Bounce", I - 0.12f)).Up > 0.2f);
        Assert.True(Directions.TargetBody(Cue("Bounce", I + 0.1f)).Squash > 0f);
        // Dig, Dive and the Pledges toss their targets up from under them
        foreach (var move in new[] { "Dig", "Dive", "Fire Pledge", "Water Pledge", "Grass Pledge" })
            Assert.True(Directions.TargetBody(Cue(move, I + 0.3f)).Up > 0.1f, move);
        // The Pledges hold nothing; the wound-up blasts rock their users back
        foreach (var move in new[] { "Fire Pledge", "Water Pledge", "Grass Pledge" })
            Assert.False(Directions.Of(move)!.Heavy, move);
        foreach (var move in new[] { "Sky Attack", "Focus Blast", "Chloroblast" })
            Assert.True(Directions.UserBody(Cue(move, I)).Toward < 0f, move);
        // Moves on their own side have no shot
        foreach (var move in new[] { "Swords Dance", "Nasty Plot", "Calm Mind", "Dragon Dance", "Victory Dance", "Take Heart",
                     "Lunar Blessing", "Spiky Shield", "Burning Bulwark", "Shelter", "Rain Dance", "Sunny Day" })
            Assert.Equal(SignatureShot.None, Directions.Of(move)!.Shot);
    }

    [Theory]
    [InlineData("Swords Dance"), InlineData("Dragon Dance"), InlineData("Spiky Shield"), InlineData("Earthquake"),
     InlineData("Thousand Arrows"), InlineData("Sacred Sword"), InlineData("Headlong Rush")]
    public void Y17esReactionsComeAfterTheImpactAndGo(string move)
    {
        var col = MoveFx.ColorsOf(MoveDatabase.Get(move)!.Type);
        var before = new FxList();
        Directions.PlayEffect(new FxCtx(Cue(move, I - 0.08f), new FxPlaces()), col, before);
        Assert.Empty(before.Quads);
        var during = new FxList();
        Directions.PlayEffect(new FxCtx(Cue(move, I + 0.2f), new FxPlaces()), col, during);
        Assert.InRange(during.Quads.Count + during.Ribbons.Count, 1, 10);
    }

    [Fact]
    public void RingCracksStandBesideTheTargetNotBehindIt()
    {
        // Every crack of the ring lies past the target's half width to one side, or before its feet toward the lens
        var places = new FxPlaces();
        var c = new FxCtx(Cue("Thousand Waves", I + 0.2f), places);
        var fx = new FxList();
        Directions.PlayEffect(c, MoveFx.ColorsOf(MoveDatabase.Get("Thousand Waves")!.Type), fx);
        Assert.Equal(4, fx.Quads.Count);
        foreach (var q in fx.Quads)
        {
            var off = q.Center - c.ToFeet;
            Assert.True(MathF.Abs(off.X) > c.ToHalfWidth * 0.9f || off.Z > 0.2f * c.ToSize, $"crack at {off}");
            Assert.True(off.Z >= -0.05f * c.ToSize, $"crack behind at {off}");
        }
    }

    /// <summary>Y17f's batch: the moves of our own past level 100 (plan 06 · R31) that are worth staging.</summary>
    public static readonly string[] Y17f =
    {
        "Titan Tackle", "Resonant Hymn", "Crescent Kick", "Ki Burst", "Mountain Breaker", "Talon Dive", "Jet Stream",
        "Zenith Plunge", "Venom Lance", "Miasma Torrent", "Fault Line", "Magma Vein", "Tectonic Rend", "Boulder Crush",
        "Crystal Lance", "Skyfall Stone", "Mandible Crush", "Spectral Claw", "Phantom Lantern", "Forge Hammer", "Magnet Lance",
        "Cinder Stomp", "Pyre Lance", "Sunforge Blast", "Riptide Slam", "Abyssal Current", "Maelstrom Crash", "Thornlash",
        "Arc Lance", "Ion Cascade", "Mind Blade", "Astral Pulse", "Mindbreak Wave", "Glacier Ram", "Whiteout Gale",
        "Permafrost Lance", "Scale Rend", "Wyrmflare", "Serpent Crash", "Rending Fang", "Umbral Pulse", "Fey Strike",
        "Starlight Chorus",
        "Rally Cry", "Centered Stance", "Updraft", "Monolith Pose", "Molt Renewal", "Alloy Plating", "Kindle", "Tidal Ward",
        "Verdant Renewal", "Overcharge", "Clairvoyance", "Wyrm Dance", "Moonlit Grace",
    };

    /// <summary>The setups of our own on their own side, which take no shot.</summary>
    private static readonly string[] Y17fSetups =
    {
        "Rally Cry", "Centered Stance", "Updraft", "Monolith Pose", "Molt Renewal", "Alloy Plating", "Kindle", "Tidal Ward",
        "Verdant Renewal", "Overcharge", "Clairvoyance", "Wyrm Dance", "Moonlit Grace",
    };

    [Fact]
    public void Y17fsBatchIsInTheTable()
    {
        Assert.DoesNotContain(Y17f, m => !Directions.Has(m));
        // Every one a move of our own (generation 0, ids 3001 to 3072), none of an earlier batch, none twice
        Assert.All(Y17f, m => Assert.InRange(MoveDatabase.Get(m)!.Id, 3001, 3072));
        Assert.Empty(Y17f.Intersect(Y17b).Concat(Y17f.Intersect(Y17c)).Concat(Y17f.Intersect(Y17d)).Concat(Y17f.Intersect(Y17e)));
        Assert.Equal(Y17f.Length, Y17f.Distinct().Count());
        // Every move of ours of 90 power or more is staged, and every one on its own side
        var ours = MoveDatabase.GetAll().Where(m => m.Id is >= 3001 and <= 3072).ToList();
        Assert.DoesNotContain(ours, m => m.Power >= 90 && !Directions.Has(m.Name));
        Assert.DoesNotContain(ours, m => m.Category == MoveCategory.Status && m.Target is MoveTarget.User or MoveTarget.UserAndAllies &&!Directions.Has(m.Name));
        Assert.True(Directions.Table.Count >= 35 + Y17c.Length + Y17d.Length + Y17e.Length + Y17f.Length);
    }

    [Fact]
    public void Y17fsBodiesAnswer()
    {
        // The bodies flung in meet their targets and never rise
        foreach (var move in new[] { "Titan Tackle", "Riptide Slam", "Maelstrom Crash", "Arc Lance", "Glacier Ram", "Serpent Crash" })
        {
            Assert.True(Directions.UserBody(Cue(move, I)).Toward > 0.5f, move);
            for (float age = 0f; age < 1.3f; age += 0.01f) Assert.Equal(0f, Directions.UserBody(Cue(move, age)).Up);
        }
        // The talons and the plunge rise over their wind-up and come down on the target
        foreach (var move in new[] { "Talon Dive", "Zenith Plunge" })
            Assert.True(Directions.UserBody(Cue(move, I - 0.12f)).Up > 0.2f, move);
        // Blows from above press; the ground's jolt; the magma's vein tosses; the jaws and the fang only shudder
        foreach (var move in new[] { "Mountain Breaker", "Zenith Plunge", "Boulder Crush", "Forge Hammer", "Cinder Stomp" })
            Assert.True(Directions.TargetBody(Cue(move, I + 0.1f)).Squash > 0f, move);
        foreach (var move in new[] { "Fault Line", "Tectonic Rend" })
            Assert.True(Directions.TargetBody(Cue(move, I + 0.12f)).Moves, move);
        Assert.True(Directions.TargetBody(Cue("Magma Vein", I + 0.3f)).Up > 0.1f);
        foreach (var move in new[] { "Mandible Crush", "Rending Fang" })
            Assert.Equal(0f, Directions.TargetBody(Cue(move, I + 0.1f)).Toward);
        // The blasts wound up: Sunforge Blast heaves, Mindbreak Wave and Permafrost Lance rock their users back
        Assert.True(Directions.UserBody(Cue("Sunforge Blast", I - 0.1f)).Squash > 0f);
        foreach (var move in new[] { "Mindbreak Wave", "Permafrost Lance" })
            Assert.True(Directions.UserBody(Cue(move, I)).Toward < 0f, move);
        // The setups are on their own side: no shot, nothing held
        foreach (var move in Y17fSetups)
        {
            Assert.Equal(SignatureShot.None, Directions.Of(move)!.Shot);
            Assert.False(Directions.Of(move)!.Heavy, move);
        }
    }

    [Theory]
    [InlineData("Mountain Breaker"), InlineData("Zenith Plunge"), InlineData("Skyfall Stone"), InlineData("Forge Hammer"),
     InlineData("Sunforge Blast"), InlineData("Astral Pulse"), InlineData("Rally Cry"), InlineData("Alloy Plating"), InlineData("Overcharge")]
    public void Y17fsReactionsComeAfterTheImpactAndGo(string move)
    {
        var col = MoveFx.ColorsOf(MoveDatabase.Get(move)!.Type);
        var before = new FxList();
        Directions.PlayEffect(new FxCtx(Cue(move, I - 0.08f), new FxPlaces()), col, before);
        Assert.Empty(before.Quads);
        var during = new FxList();
        Directions.PlayEffect(new FxCtx(Cue(move, I + 0.2f), new FxPlaces()), col, during);
        Assert.InRange(during.Quads.Count + during.Ribbons.Count, 1, 10);
    }

    // ------------------------------------------------------------------ the hit-stop

    [Theory]
    [InlineData(BattleAnimator.EffectTime), InlineData(BattleAnimator.MaxMoveTime)]
    public void TheRemapHoldsAtTheImpactAndEndsOnTime(float length)
    {
        const float stop = Directions.HeavyStop;
        float last = -1f;
        for (float age = 0f; age <= length + 0.2f; age += 0.005f)
        {
            float shown = Directions.Remap(age, stop, length);
            Assert.True(shown >= last - 1e-5f, $"backward at {age}");
            last = shown;
            if (age <= I) Assert.Equal(age, shown);
            else if (age < I + stop) Assert.Equal(I, shown);
            Assert.InRange(shown, 0f, Math.Max(age, length) + 1e-4f);
        }
        Assert.Equal(length, Directions.Remap(length, stop, length), 4);
        // No stop: the age itself, to the bit
        Assert.Equal(0.731f, Directions.Remap(0.731f, 0f, length));
    }

    [Fact]
    public void AHeavySignatureHitHoldsThePictureAtItsImpact()
    {
        var cue = Cue("Hyper Beam", I + 0.04f);
        Assert.Equal(I, cue.ShownAge);
        Assert.Equal(0.2f, Cue("Hyper Beam", 0.2f).ShownAge);
        Assert.True(Cue("Hyper Beam", 0.8f).ShownAge > 0.8f - Directions.HeavyStop);
    }

    [Theory]
    [InlineData("Tackle", false, false), InlineData("Ice Beam", false, false), InlineData("Hyper Beam", true, false), InlineData("Solar Beam", false, true)]
    [InlineData("Hurricane", false, false)]
    public void NothingButALandedHeavyStrikeIsHeld(string move, bool missed, bool charging)
    {
        // An ordinary move, a miss, a charge turn and a signature without a stop all draw at their own age
        for (float age = 0f; age < 1.3f; age += 0.05f)
            Assert.Equal(age, Cue(move, age, missed: missed, charging: charging).ShownAge);
    }

    // ------------------------------------------------------------------ the looks laid over

    [Theory]
    [MemberData(nameof(Signatures))]
    public void WhatADirectionLaysOverALookStaysUnderTheCaps(string move)
    {
        foreach (var from in new[] { BattleSide.Player, BattleSide.Enemy })
            for (float age = 0f; age < 1.4f; age += 0.01f)
            {
                var cue = Cue(move, age, from);
                var all = new FxList();
                MoveFx.Play(cue, new FxPlaces(), all);
                Assert.True(all.Count <= MoveFx.MaxQuads, $"{move} at {age:0.00}: {all.Count}");

                var layer = new FxList();
                Directions.PlayEffect(new FxCtx(cue, new FxPlaces()), MoveFx.ColorsOf(cue.Type), layer);
                Assert.True(layer.Flash <= 0.35f, $"{move}'s flash {layer.Flash}");
                Assert.True(layer.Shake <= 0.5f, $"{move}'s shake {layer.Shake}");
            }
    }

    [Theory]
    [MemberData(nameof(Signatures))]
    public void ASignatureMoveIsAPureFunctionOfItsAgeAndSeed(string move)
    {
        foreach (float age in new[] { 0.1f, I + 0.03f, 0.7f, 1.1f })
        {
            var a = new FxList();
            var b = new FxList();
            MoveFx.Play(Cue(move, age), new FxPlaces(), a);
            MoveFx.Play(Cue(move, age), new FxPlaces(), b);
            Assert.Equal(a.Count, b.Count);
            for (int i = 0; i < a.Quads.Count; i++) Assert.Equal(a.Quads[i].Center, b.Quads[i].Center);
            Assert.True(BodyMotions.Of(Playing(Cue(move, age)), BattleSide.Enemy, 0)
                == BodyMotions.Of(Playing(Cue(move, age)), BattleSide.Enemy, 0));
        }
    }

    [Fact]
    public void RoarOfTimesRingsComeAfterItsImpactAndAreGone()
    {
        var before = new FxList();
        Directions.PlayEffect(new FxCtx(Cue("Roar of Time", 0.2f), new FxPlaces()), MoveFx.ColorsOf(PokemonType.Dragon), before);
        Assert.Empty(before.Quads);
        var after = new FxList();
        Directions.PlayEffect(new FxCtx(Cue("Roar of Time", 0.6f), new FxPlaces()), MoveFx.ColorsOf(PokemonType.Dragon), after);
        Assert.NotEmpty(after.Quads);
        var gone = new FxList();
        Directions.PlayEffect(new FxCtx(Cue("Roar of Time", 1.29f), new FxPlaces()), MoveFx.ColorsOf(PokemonType.Dragon), gone);
        Assert.Empty(gone.Quads);
    }

    // ------------------------------------------------------------------ the bodies and the light

    [Fact]
    public void AHeavyHitThrowsItsTargetBackAndItSettles()
    {
        Assert.False(BodyMotions.Of(Playing(Cue("Close Combat", 0.2f)), BattleSide.Enemy, 0).Moves);
        var thrown = BodyMotions.Of(Playing(Cue("Close Combat", I + 0.12f)), BattleSide.Enemy, 0);
        Assert.True(thrown.Toward < 0f && thrown.Toward >= -Directions.ThrowReach);
        Assert.True(thrown.Squash > 0f && thrown.Squash <= Directions.ThrowSquash);
        // Back where it stood once the throw is over, and never by a miss
        Assert.False(BodyMotions.Of(Playing(Cue("Close Combat", 1.2f)), BattleSide.Enemy, 0).Moves);
        Assert.False(BodyMotions.Of(Playing(Cue("Close Combat", I + 0.12f, missed: true)), BattleSide.Enemy, 0).Moves);
        // An ordinary hit throws nobody
        Assert.False(BodyMotions.Of(Playing(Cue("Ice Beam", I + 0.12f)), BattleSide.Enemy, 0).Moves);
    }

    [Fact]
    public void ABeamRocksItsUserBack()
    {
        var user = BodyMotions.Of(Playing(Cue("Hyper Beam", I)), BattleSide.Player, 0);
        Assert.True(user.Toward < 0f);
        Assert.False(BodyMotions.Of(Playing(Cue("Hyper Beam", 1.25f)), BattleSide.Player, 0).Moves);
    }

    [Fact]
    public void ASignatureLightComesOverTheMovesOwnAndGoes()
    {
        Assert.False(Directions.LightAt(Cue("Hurricane", 0f)).Any);
        Assert.True(Directions.LightAt(Cue("Hurricane", 0.6f)).Any);
        Assert.False(Directions.LightAt(Cue("Hurricane", 1.29f)).Any);
        // Roar of Time's own darkening and its signature's steel-blue, together never darker than the cap allows
        var both = StageLights.Of(Playing(Cue("Roar of Time", 0.6f)));
        Assert.True(both.Any && both.Amount <= StageLights.MostDark + 1e-4f);
        Assert.False(Directions.LightAt(Cue("Solar Beam", 0.5f, charging: true)).Any);
    }

    // ------------------------------------------------------------------ the camera

    private static bool Finite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);

    [Theory]
    [MemberData(nameof(Signatures))]
    public void EverySignatureShotKeepsClearOfThePlayersPlatform(string move)
    {
        var platform = BattleStage.PlayerSpot + new Vector3(0, 1.2f, 0);
        foreach (var from in new[] { BattleSide.Player, BattleSide.Enemy })
            for (float age = 0f; age < Directions.ShotEnd; age += 0.02f)
            {
                var cue = Cue(move, age, from);
                var anim = Playing(cue);
                var (shot, kind, _) = BattleCamera.Direct(anim, new FxPlaces());
                Assert.True(Finite(shot.Position) && Finite(shot.Target), $"{move} at {age:0.00}");
                Assert.InRange(shot.Fov, 5f, BattleCamera.WidestFov + 0.01f);
                // The player's own Pokémon's close shots stand behind it, as every move's attacker shot does; any other
                // shot of the four stays out of the platform's way
                if (kind is ShotKind.Follow or ShotKind.Orbit or ShotKind.Punch)
                    Assert.True(Vector3.Distance(shot.Position, platform) >= BattleCamera.Clearance, $"{move} {kind} from {from} at {age:0.00}: {shot.Position}");
            }
    }

    [Fact]
    public void EachShotIsTheOneItsMoveAsks()
    {
        ShotKind At(string move, float age) => BattleCamera.Direct(Playing(Cue(move, age)), new FxPlaces()).Kind;
        Assert.Equal(ShotKind.Push, At("Hyper Beam", 0.1f));
        Assert.Equal(ShotKind.Target, At("Hyper Beam", 0.5f));
        Assert.Equal(ShotKind.Follow, At("Roar of Time", 0.2f));
        Assert.Equal(ShotKind.Punch, At("Close Combat", I + 0.06f));
        Assert.Equal(ShotKind.Target, At("Close Combat", 1.1f));
        Assert.Equal(ShotKind.Orbit, At("Hurricane", 0.6f));
        // Held past an ordinary move's end, so the pull-back is seen
        Assert.NotEqual(ShotKind.Overview, At("Close Combat", BattleCamera.MoveShotEnd + 0.05f));
        Assert.Contains(At("Close Combat", Directions.ShotEnd + 0.01f), new[] { ShotKind.Intro, ShotKind.Overview });
        // A charge turn pushes in on its user
        Assert.Equal(ShotKind.Push, BattleCamera.Direct(Playing(Cue("Solar Beam", 0.5f, charging: true)), new FxPlaces()).Kind);
        // An ordinary move keeps its own shots
        Assert.Equal(ShotKind.Attacker, At("Ice Beam", 0.1f));
    }

    [Fact]
    public void ThePushCloses_ThePunchIsTighter_AndTheOrbitComesHome()
    {
        var places = new FxPlaces();
        var platform = places.Body(BattleSide.Enemy, 0);
        float Distance(string move, float age, BattleSide from = BattleSide.Player)
        {
            var (shot, _, _) = BattleCamera.Direct(Playing(Cue(move, age, from)), places);
            return Vector3.Distance(shot.Position, shot.Target);
        }
        Assert.True(Distance("Hyper Beam", 0.2f, BattleSide.Enemy) < Distance("Hyper Beam", 0.01f, BattleSide.Enemy));
        Assert.True(Distance("Close Combat", I + 0.1f) < Distance("Close Combat", 0.3f));
        // The orbit's swing is a sine: round and back to the plain target shot by its end
        var (start, _, _) = BattleCamera.Direct(Playing(Cue("Hurricane", BattleCamera.CutTime)), places);
        var (mid, _, _) = BattleCamera.Direct(Playing(Cue("Hurricane", 0.7f)), places);
        var (end, _, _) = BattleCamera.Direct(Playing(Cue("Hurricane", Directions.ShotEnd - 0.001f)), places);
        Assert.True(Vector3.Distance(mid.Position, start.Position) > 3f);
        Assert.True(Vector3.Distance(end.Position, start.Position) < 0.2f);
        // Swung round the foe, still looking at it
        Assert.True(Vector3.Distance(mid.Target, platform) < 0.5f);
    }
}

/// <summary>The motion number turned down (plan 12 · Q9's reduce motion): a static, so these run alone.</summary>
[Collection("Shared clocks and dice")]
public class SignatureMotionTests
{
    private static EffectCue Cue(string move, float age) => new()
    {
        Kind = CueKind.Move, Move = move, Type = PokemonType.Normal, Category = MoveCategory.Special,
        FromSide = BattleSide.Player, ToSide = BattleSide.Enemy, Seed = 3, Age = age
    };

    private static BattleAnimator Playing(EffectCue cue)
    {
        var anim = new BattleAnimator();
        float age = cue.Age;
        cue.Age = 0f;
        anim.Cue(cue);
        cue.Age = age;
        return anim;
    }

    [Fact]
    public void WithNoMotionASignatureMovePlaysAsAnOrdinaryOne()
    {
        float was = BattleCamera.Motion;
        try
        {
            BattleCamera.Motion = 0f;
            Assert.Equal(BattleAnimator.ImpactTime + 0.04f, Cue("Hyper Beam", BattleAnimator.ImpactTime + 0.04f).ShownAge);
            foreach (float age in new[] { 0.1f, 0.3f, 0.6f, 0.9f, 1.05f })
            {
                var signature = BattleCamera.Direct(Playing(Cue("Hyper Beam", age)), new FxPlaces());
                var plain = BattleCamera.Direct(Playing(Cue("Ice Beam", age)), new FxPlaces());
                Assert.Equal(plain.Kind, signature.Kind);
                Assert.Equal(plain.Shot, signature.Shot);
            }
        }
        finally { BattleCamera.Motion = was; }
    }
}
