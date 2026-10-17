using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The moves of their own (plan 25 · Y6 on): every move's look lands with its damage, is over when its cue is, draws
/// no more than <see cref="MoveFx.MaxQuads"/>, and is a function of its cue's age and seed alone; the batches cover the
/// story's moves in the order it shows them (<see cref="MoveSightings"/>), each with a look and a sound of its own.
/// </summary>
public class MovePresentationTests
{
    private readonly ITestOutputHelper output;

    public MovePresentationTests(ITestOutputHelper output) => this.output = output;

    private const float Frame = 1f / 60f;
    private static readonly FxPlaces Places = new();

    private static IEnumerable<MoveData> PlatinumMoves => MoveDatabase.GetAll().Where(m => m.Id <= 467);

    private static EffectCue Cue(MoveData move, bool critical = false, bool superEffective = false, bool missed = false, BattleTerrain terrain = BattleTerrain.Plain)
    {
        // A move on the user itself plays on the user (Swords Dance); everything else crosses to the foe
        bool self = IsOnSelf(move);
        return new BattleAnimator().Cue(new EffectCue
        {
            Kind = CueKind.Move, Move = move.Name, Type = move.Type, Category = move.Category,
            FromSide = BattleSide.Player, ToSide = self ? BattleSide.Player : BattleSide.Enemy,
            Critical = critical, SuperEffective = superEffective, Missed = missed, Terrain = terrain
        });
    }

    private static bool IsOnSelf(MoveData move) => move.Target is MoveTarget.User or MoveTarget.UserSide or MoveTarget.UserAndAllies;

    private static FxList Draw(EffectCue cue, float age)
    {
        cue.Age = age;
        var fx = new FxList();
        MoveFx.Play(cue, Places, fx);
        return fx;
    }

    private static string Picture(FxList fx) =>
        string.Join(";", fx.Quads.Select(q => $"{q.Shape}{q.Center.X:0.000},{q.Center.Y:0.000},{q.Center.Z:0.000}/{q.Size:0.000}/{q.Color.R},{q.Color.A}"))
        + "|" + string.Join(";", fx.Ribbons.Select(r => $"{r.Width:0.000}/{r.Points.Length}")) + $"|{fx.Flash:0.000}|{fx.Shake:0.000}";

    // ------------------------------------------------------------------ every recipe

    [Fact]
    public void EveryMoveLandsWithItsDamageWithinAFrame()
    {
        var late = new List<string>();
        foreach (var move in PlatinumMoves)
        {
            var fx = Draw(Cue(move), BattleAnimator.ImpactTime);
            if (Math.Abs(fx.Landed - BattleAnimator.ImpactTime) > Frame) late.Add($"{move.Name} lands at {fx.Landed:0.000}");
        }
        Assert.Empty(late);
    }

    [Fact]
    public void EveryMoveIsOverByTheEndOfItsCue()
    {
        foreach (var move in PlatinumMoves)
            foreach (var cue in new[] { Cue(move), Cue(move, critical: true, superEffective: true), Cue(move, missed: true) })
            {
                var fx = Draw(cue, BattleAnimator.EffectTime + Frame);
                Assert.True(fx.Count == 0 && fx.Flash == 0f && fx.Shake == 0f, $"{move.Name} still draws {fx.Count} after its cue");
            }
    }

    [Fact]
    public void NoMoveDrawsMoreThanTheCapAtAnyAge()
    {
        var over = new List<string>();
        foreach (var move in PlatinumMoves)
        {
            var cue = Cue(move, critical: true, superEffective: true);
            for (float age = 0f; age < BattleAnimator.EffectTime; age += Frame)
            {
                int n = Draw(cue, age).Count;
                if (n > MoveFx.MaxQuads) over.Add($"{move.Name} draws {n} at {age:0.00}");
            }
        }
        Assert.Empty(over);
    }

    [Fact]
    public void TheCapIsTheHeaviestOfTheLooksBeforeTheBatches()
    {
        // G8's 146, as they stood when Y6 measured them: the cap is theirs, and no batch raises it
        int heaviest = 0;
        string which = "";
        foreach (var move in PlatinumMoves.Where(m => MoveFx.HasOwnEffect(m.Name) && MoveFx.BatchOf(m.Name) == null))
        {
            var cue = Cue(move, critical: true, superEffective: true);
            for (float age = 0f; age < BattleAnimator.EffectTime; age += Frame)
            {
                int n = Draw(cue, age).Count;
                if (n > heaviest) (heaviest, which) = (n, move.Name);
            }
        }
        output.WriteLine($"heaviest before the batches: {which}, {heaviest}");
        Assert.InRange(heaviest, MoveFx.MaxQuads - 15, MoveFx.MaxQuads);
    }

    [Fact]
    public void EveryLookIsTheSameForTheSameAgeAndSeed()
    {
        foreach (var move in PlatinumMoves.Where(m => MoveFx.BatchOf(m.Name) != null))
        {
            var a = Cue(move, critical: true);
            var b = Cue(move, critical: true);
            b.Seed = a.Seed;
            foreach (float age in new[] { 0.1f, BattleAnimator.ImpactTime, 0.5f, 0.9f })
                Assert.Equal(Picture(Draw(a, age)), Picture(Draw(b, age)));
        }
    }

    [Fact]
    public void NoBatchFlashesHarderThanTheStyleGuideAllows()
    {
        foreach (var move in PlatinumMoves.Where(m => MoveFx.BatchOf(m.Name) != null))
        {
            var cue = Cue(move, critical: true, superEffective: true);
            for (float age = 0f; age < BattleAnimator.EffectTime; age += 0.02f)
            {
                var fx = Draw(cue, age);
                Assert.InRange(fx.Flash, 0f, 0.5f);
                Assert.InRange(fx.Shake, 0f, 1f);
            }
        }
    }

    [Fact]
    public void ABatchsLookIsItsOwnAndNotItsTemplate()
    {
        foreach (var move in PlatinumMoves.Where(m => MoveFx.BatchOf(m.Name) != null))
        {
            var cue = Cue(move);
            var template = MoveFx.Template(move.Type, move.Category, cue.OnSelf);
            int differ = 0;
            foreach (float age in new[] { 0.15f, 0.3f, BattleAnimator.ImpactTime + 0.05f, 0.6f, 0.9f })
            {
                cue.Age = age;
                var own = new FxList();
                MoveFx.Play(cue, Places, own);
                var plain = new FxList();
                template(new FxCtx(cue, Places), MoveFx.ColorsOf(move.Type), plain);
                if (Picture(own) != Picture(plain)) differ++;
                Assert.True(own.Count + (own.Flash > 0 ? 1 : 0) >= 0);
            }
            Assert.True(differ > 0, $"{move.Name} looks just like its template");
            // And it draws something
            Assert.True(new[] { 0.1f, 0.3f, 0.45f, 0.7f }.Sum(t => Draw(Cue(move), t).Count) > 0, $"{move.Name} draws nothing");
        }
    }

    [Fact]
    public void SecretPowerGoesByTheGround()
    {
        var move = MoveDatabase.Get("Secret Power")!;
        var looks = Enum.GetValues<BattleTerrain>().Select(t =>
        {
            var cue = Cue(move, terrain: t);
            cue.Seed = 7;
            return Picture(Draw(cue, 0.2f));
        }).Distinct().Count();
        Assert.True(looks >= 6, $"Secret Power has only {looks} looks");
        var camo = MoveDatabase.Get("Camouflage")!;
        Assert.NotEqual(Picture(Draw(Cue(camo, terrain: BattleTerrain.Grass), 0.3f)).Split('|')[0].Split(';')[0], "");
    }

    // ------------------------------------------------------------------ the batches and the story

    [Fact]
    public void TheFirstChaptersMovesAllHaveALookAndASoundOfTheirOwn()
    {
        var first = MoveSightings.FirstChapter;
        output.WriteLine($"{first.Count} moves by the end of the first chapter: {string.Join(", ", first)}");
        Assert.InRange(first.Count, 30, 60);
        Assert.All(first, move => Assert.Equal("Y6", MoveFx.BatchOf(move)));
        Assert.All(first, move => Assert.True(SoundBank.CryModesOf(move) != null || SoundBank.Exists("use_" + SoundBank.MoveKey(move)), move));
    }

    [Fact]
    public void TheSeventhBatchsFirstPartAllHaveALookAndASoundOfTheirOwn()
    {
        // Plan 25 · Y7, its first part: Route 204 to Team Galactic's Eterna Building
        var part = MoveSightings.SeventhBatchFirstPart;
        output.WriteLine($"{part.Count} moves in Y7's first part: {string.Join(", ", part)}");
        Assert.Equal(67, part.Count);
        Assert.All(part, move => Assert.Equal("Y7", MoveFx.BatchOf(move)));
        Assert.All(part, move => Assert.True(SoundBank.Exists("use_" + SoundBank.MoveKey(move)), move));
        // And nothing of the second part has run ahead of it
        Assert.All(MoveFx.Batches.Where(b => b.Value == "Y7"), b => Assert.Contains(b.Key, part, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void TheSeventhBatchsSecondPartAllHaveALookAndASoundOfTheirOwn()
    {
        // Plan 25 · Y7b: Route 206 to Route 208
        var part = MoveSightings.SeventhBatchSecondPart;
        output.WriteLine($"{part.Count} moves in Y7's second part: {string.Join(", ", part)}");
        Assert.Equal(39, part.Count);
        Assert.All(part, move => Assert.Equal("Y7b", MoveFx.BatchOf(move)));
        // Roar is the user's voice and has the look alone
        Assert.All(part, move => Assert.True(SoundBank.CryModesOf(move) != null || SoundBank.Exists("use_" + SoundBank.MoveKey(move)), move));
        Assert.Contains("Roar", part);
        Assert.False(SoundBank.Exists("use_roar"));
        // And nothing of either part has run into the other
        Assert.All(MoveFx.Batches.Where(b => b.Value == "Y7b"), b => Assert.Contains(b.Key, part, StringComparer.OrdinalIgnoreCase));
        Assert.Empty(part.Intersect(MoveSightings.SeventhBatchFirstPart, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void TheEighthBatchsFirstPartAllHaveALookAndASoundOfTheirOwn()
    {
        // Plan 25 · Y8a: Hearthome City's gate to the Café Cabin on Route 210
        var part = MoveSightings.EighthBatchFirstPart;
        output.WriteLine($"{part.Count} moves in Y8's first part: {string.Join(", ", part)}");
        Assert.Equal(51, part.Count);
        Assert.All(part, move => Assert.Equal("Y8", MoveFx.BatchOf(move)));
        // Chatter is the user's voice and has the look alone
        Assert.All(part, move => Assert.True(SoundBank.CryModesOf(move) != null || SoundBank.Exists("use_" + SoundBank.MoveKey(move)), move));
        Assert.Contains("Chatter", part);
        Assert.False(SoundBank.Exists("use_chatter"));
        // And nothing outside the part has run into the batch, nor the part into the ones before it
        Assert.All(MoveFx.Batches.Where(b => b.Value == "Y8"), b => Assert.Contains(b.Key, part, StringComparer.OrdinalIgnoreCase));
        Assert.Empty(part.Intersect(MoveSightings.SeventhBatchFirstPart.Concat(MoveSightings.SeventhBatchSecondPart), StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void TheEighthBatchsMovesThatShareAParticleFileOrAKindLookDifferentHere()
    {
        // Metal Claw and Shadow Claw, Fury Attack and Pin Missile, Haze and Defog, Aqua Ring and Light Screen
        foreach (var (a, b) in new[] { ("Metal Claw", "Shadow Claw"), ("Fury Attack", "Pin Missile"), ("Haze", "Defog"), ("Light Screen", "Aqua Ring"), ("Thunder Shock", "Shock Wave"), ("Stockpile", "Swallow") })
        {
            var ma = MoveDatabase.Get(a)!;
            var mb = MoveDatabase.Get(b)!;
            int same = 0;
            foreach (float age in new[] { 0.15f, 0.3f, 0.5f, 0.8f })
            {
                var ca = Cue(ma);
                var cb = Cue(mb);
                cb.Seed = ca.Seed;
                if (Picture(Draw(ca, age)) == Picture(Draw(cb, age))) same++;
            }
            Assert.True(same < 4, $"{a} and {b} look alike");
        }
    }

    [Fact]
    public void TheEighthBatchsSecondPartAllHaveALookAndASoundOfTheirOwn()
    {
        // Plan 25 · Y8b: Route 210's northern half to the Seven Stars Restaurant
        var part = MoveSightings.EighthBatchSecondPart;
        output.WriteLine($"{part.Count} moves in Y8's second part: {string.Join(", ", part)}");
        Assert.Equal(55, part.Count);
        Assert.All(part, move => Assert.Equal("Y8b", MoveFx.BatchOf(move)));
        Assert.All(part, move => Assert.True(SoundBank.Exists("use_" + SoundBank.MoveKey(move)), move));
        // And nothing outside the part has run into the batch, nor the part into the ones before it
        Assert.All(MoveFx.Batches.Where(b => b.Value == "Y8b"), b => Assert.Contains(b.Key, part, StringComparer.OrdinalIgnoreCase));
        Assert.Empty(part.Intersect(MoveSightings.EighthBatchFirstPart.Concat(MoveSightings.SeventhBatchFirstPart).Concat(MoveSightings.SeventhBatchSecondPart), StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void TheEighthBatchsSecondPartsMovesThatShareAParticleFileOrAKindLookDifferentHere()
    {
        // The fangs, the punches, the poison moves, the airs, the mists and the whips
        foreach (var (a, b) in new[] { ("Hyper Fang", "Fire Fang"), ("Fire Fang", "Poison Fang"), ("Drain Punch", "Ice Punch"), ("Poison Jab", "Poison Fang"),
            ("Air Slash", "Air Cutter"), ("Mist", "Haze"), ("Sludge Bomb", "Mud Bomb"), ("Vine Whip", "Aqua Tail"), ("Strength", "Vital Throw"), ("Psychic", "Psycho Cut") })
        {
            var ma = MoveDatabase.Get(a)!;
            var mb = MoveDatabase.Get(b)!;
            int same = 0;
            foreach (float age in new[] { 0.15f, 0.3f, 0.5f, 0.8f })
            {
                var ca = Cue(ma);
                var cb = Cue(mb);
                cb.Seed = ca.Seed;
                if (Picture(Draw(ca, age)) == Picture(Draw(cb, age))) same++;
            }
            Assert.True(same < 4, $"{a} and {b} look alike");
        }
    }

    [Fact]
    public void TheNinthBatchsFirstPartAllHaveALookAndASoundOfTheirOwn()
    {
        // Plan 25 · Y9a: Lake Valor to Route 222
        var part = MoveSightings.NinthBatchFirstPart;
        output.WriteLine($"{part.Count} moves in Y9's first part: {string.Join(", ", part)}");
        Assert.Equal(54, part.Count);
        Assert.All(part, move => Assert.Equal("Y9", MoveFx.BatchOf(move)));
        Assert.All(part, move => Assert.True(SoundBank.Exists("use_" + SoundBank.MoveKey(move)), move));
        // And nothing outside the part has run into the batch, nor the part into the ones before it
        Assert.All(MoveFx.Batches.Where(b => b.Value == "Y9"), b => Assert.Contains(b.Key, part, StringComparer.OrdinalIgnoreCase));
        Assert.Empty(part.Intersect(MoveSightings.EighthBatchSecondPart.Concat(MoveSightings.EighthBatchFirstPart), StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void TheNinthBatchsFirstPartsMovesThatShareAParticleFileOrAKindLookDifferentHere()
    {
        // The fists, the beams, the cold moves, the fire, the water and the tail and hammer moves
        foreach (var (a, b) in new[] { ("Thunder Punch", "Thunder Fang"), ("Thunder", "Discharge"), ("Fire Blast", "Flamethrower"), ("Icy Wind", "Blizzard"),
            ("Hydro Pump", "Muddy Water"), ("Iron Tail", "Wood Hammer"), ("Shadow Punch", "Dizzy Punch"), ("Cross Chop", "Close Combat"),
            ("Mud Shot", "Brine"), ("Avalanche", "Hail"), ("Flash Cannon", "Focus Blast"), ("Psywave", "Aurora Beam"), ("Double-Edge", "Thrash"),
            ("Iron Tail", "Double Hit"), ("Shadow Punch", "Mach Punch"), ("Dragon Breath", "Flamethrower"), ("Bounce", "High Jump Kick") })
        {
            var ma = MoveDatabase.Get(a)!;
            var mb = MoveDatabase.Get(b)!;
            int same = 0;
            foreach (float age in new[] { 0.15f, 0.3f, 0.5f, 0.8f })
            {
                var ca = Cue(ma);
                var cb = Cue(mb);
                cb.Seed = ca.Seed;
                if (Picture(Draw(ca, age)) == Picture(Draw(cb, age))) same++;
            }
            Assert.True(same < 4, $"{a} and {b} look alike");
        }
    }
    [Fact]
    public void MovesThatShareAParticleFileInTheOriginalLookDifferentHere()
    {
        // Block and Imprison, Self-Destruct and Explosion, Snore and Rest, and two more that are alike in kind
        foreach (var (a, b) in new[] { ("Block", "Imprison"), ("Self-Destruct", "Explosion"), ("Snore", "Rest"), ("Bug Bite", "Ice Fang"), ("Mind Reader", "Detect") })
        {
            var ma = MoveDatabase.Get(a)!;
            var mb = MoveDatabase.Get(b)!;
            int same = 0;
            foreach (float age in new[] { 0.15f, 0.3f, 0.5f, 0.8f })
            {
                var ca = Cue(ma);
                var cb = Cue(mb);
                cb.Seed = ca.Seed;
                if (Picture(Draw(ca, age)) == Picture(Draw(cb, age))) same++;
            }
            Assert.True(same < 4, $"{a} and {b} look alike");
        }
    }

    [Fact]
    public void MagnitudesStrengthIsTheSeedsAndShowsInTheShake()
    {
        var move = MoveDatabase.Get("Magnitude")!;
        var shakes = Enumerable.Range(1, 40).Select(seed =>
        {
            var cue = Cue(move);
            cue.Seed = seed;
            return Enumerable.Range(0, 30).Max(i => Draw(cue, 0.05f + i * 0.03f).Shake);
        }).ToList();
        Assert.InRange(shakes.Min(), 0.4f, 0.7f);
        Assert.InRange(shakes.Max(), 0.8f, 1.01f);
    }

    [Fact]
    public void TheNewShapesOfTheSeventhBatchAreDrawn()
    {
        // Y7's five cells of the atlas: each covers its middle and leaves its edge empty
        foreach (var shape in new[] { FxShape.Hex, FxShape.Target, FxShape.Beak, FxShape.Horn, FxShape.Seed })
        {
            int covered = 0;
            for (float y = -1f; y <= 1f; y += 0.05f)
                for (float x = -1f; x <= 1f; x += 0.05f)
                    if (FxTextures.Coverage(shape, new System.Numerics.Vector2(x, y)) > 0.5f) covered++;
            Assert.InRange(covered, 80, 1200);
            Assert.True(FxTextures.Coverage(shape, new System.Numerics.Vector2(1.15f, 1.15f)) < 0.01f, $"{shape} reaches its cell's corner");
        }
    }

    [Fact]
    public void TheOrderOfTheBatchesIsTheStorys()
    {
        // The order Y7 to Y10 take (written into plan 25 from this list; MOVE_SIGHTINGS_OUT names a file to write it to)
        var all = MoveSightings.All;
        Assert.True(all.Count > 350, $"only {all.Count} of Platinum's moves are seen anywhere");
        Assert.Equal(all.Count, all.Select(s => s.Move).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.True(all.Zip(all.Skip(1)).All(p => p.First.Rank <= p.Second.Rank));
        if (Environment.GetEnvironmentVariable("MOVE_SIGHTINGS_OUT") is { } path)
            System.IO.File.WriteAllLines(path, all.Select(s => $"{s.Rank}\t{s.Place}\t{s.Move}\t{s.How}"));
    }

    [Fact]
    public void EveryBatchMoveHasItsOwnSoundAndTheRestTheirTypes()
    {
        foreach (var move in PlatinumMoves)
        {
            string sound = SoundBank.MoveSound(move.Name, move.Type);
            if (MoveFx.BatchOf(move.Name) == null)
                Assert.Equal(SoundBank.MoveSound(move.Type), sound);
            else if (SoundBank.CryModesOf(move.Name) == null)
                Assert.Equal("use_" + SoundBank.MoveKey(move.Name), sound);
        }
        // Every own sound belongs to a batch move
        Assert.All(SoundBank.MovesWithOwnSounds, move => Assert.NotNull(MoveFx.BatchOf(move)));
        Assert.Equal("mud_slap", SoundBank.MoveKey("Mud-Slap"));
        Assert.Equal("will_o_wisp", SoundBank.MoveKey("Will-O-Wisp"));
    }

    [Fact]
    public void TheFloorsOfMovesOfTheirOwnOnlyRise()
    {
        // Raised by each batch (plan 25 · Y6 to Y10): Platinum's moves with a look and a sound of their own
        int own = PlatinumMoves.Count(m => MoveFx.BatchOf(m.Name) != null &&
            (SoundBank.CryModesOf(m.Name) != null || SoundBank.Exists("use_" + SoundBank.MoveKey(m.Name))));
        output.WriteLine($"{own} of Platinum's 467 have a look and a sound of their own");
        Assert.True(own >= 313, $"only {own}");
    }

    [Fact]
    public void TheVoicesOfTheMovesAreModesOfTheirOwn()
    {
        var raw = Cries.Make(CryVoice.Of(PokemonDatabase.Get("Bidoof")!));
        var modes = new[] { CryMode.Uproar1, CryMode.Uproar2, CryMode.Howl1, CryMode.Howl2, CryMode.HyperVoice1, CryMode.HyperVoice2, CryMode.MidMove };
        var made = modes.Select(m => Cries.Play(raw, m)).ToList();
        foreach (var (s, mode) in made.Zip(modes))
        {
            Assert.All(s, x => Assert.True(float.IsFinite(x)));
            Assert.True(MathF.Abs(s[0]) < 0.02f && MathF.Abs(s[^1]) < 0.02f, $"{mode} clicks");
            Assert.True(s.Max(MathF.Abs) <= Cries.Peak + 0.01f, $"{mode} peaks over the cry's peak");
            Assert.True(Loudness.Of(s) <= Loudness.CryCeiling + 0.05, $"{mode} is too loud");
            Assert.InRange(s.Length / (double)Synthesizer.SampleRate, 0.2, 2.0);
        }
        Assert.Equal(modes.Length, made.Select(s => $"{s.Length}:{s[s.Length / 3]:0.0000}").Distinct().Count());
        foreach (var move in new[] { "Growl", "Roar", "Hyper Voice", "Howl", "Chatter" }) Assert.NotNull(SoundBank.CryModesOf(move));
    }
}
