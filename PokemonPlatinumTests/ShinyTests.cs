using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;
using Raylib_cs;
using Xunit;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Shiny Pokémon (plan 09 · L1; style guide, "Shiny Pokémon"): a look of their own, named <c>X+s</c>, in a palette
/// worked out from the model and the species' number; their own signature, so no plain sprite is baked again; a
/// sparkle as they come out, which moves no seed of the battle's; a star by their name; and the Pokédex's record of the
/// species caught shiny.
/// </summary>
public class ShinyTests
{
    private const float Frame = 1f / 60f;

    // ------------------------------------------------------------------ the look's name

    [Fact]
    public void APokemonsLookIsItsModelAndShinyWhenItIsShiny()
    {
        var plain = Mon("Gyarados", 30);
        plain.IsShiny = false;
        Assert.Equal("Gyarados", plain.LookName);
        var shiny = Mon("Gyarados", 30);
        shiny.IsShiny = true;
        Assert.Equal("Gyarados+s", shiny.LookName);
        // A form's look keeps the form
        shiny.Form = "Gyarados-Mega";
        Assert.Equal("Gyarados-Mega+s", shiny.LookName);
        // An Egg shows no colours of its own
        shiny.Form = null;
        shiny.IsEgg = true;
        Assert.Equal("Gyarados", shiny.LookName);
    }

    [Theory]
    [InlineData("Gyarados")]
    [InlineData("Mr. Mime")]
    [InlineData("Charizard-Mega-X")]
    public void ALookNameGoesToItsModelAndBack(string model)
    {
        Assert.False(PokemonLook.IsShiny(model));
        Assert.True(PokemonLook.IsShiny(PokemonLook.Shiny(model)));
        Assert.Equal(model, PokemonLook.Base(PokemonLook.Shiny(model)));
        Assert.Equal(PokemonLook.Shiny(model), PokemonLook.Shiny(PokemonLook.Shiny(model)));
        Assert.Equal(model, PokemonLook.Of(model, false));
        Assert.Equal(model + "+s", PokemonLook.Of(model, true));
    }

    [Fact]
    public void AShinyLooksSignatureIsItsOwnAndThePlainOneIsUntouched()
    {
        string before = PokemonModels.Signature("Gyarados");
        string shiny = PokemonModels.Signature("Gyarados+s");
        Assert.NotEqual(before, shiny);
        Assert.Equal(before, PokemonModels.Signature("Gyarados"));
        Assert.Equal(shiny, PokemonModels.Signature("Gyarados+s"));
        Assert.NotEqual(PokemonModels.Signature("Magikarp+s"), shiny);
    }

    [Fact]
    public void AShinyModelIsTheSameMeshWithAPaletteAndThePlainOneHasNone()
    {
        var plain = PokemonModels.Build("Gyarados");
        var shiny = PokemonModels.Build("Gyarados+s");
        Assert.Null(plain.Palette);
        Assert.NotNull(shiny.Palette);
        // The mesh is the sculpt's, vertex for vertex: only the colours it is put on the GPU in differ
        Assert.Equal(plain.Mesh.Positions, shiny.Mesh.Positions);
        Assert.Equal(plain.Mesh.Colors, shiny.Mesh.Colors);
        Assert.Equal(plain.Decals.Count, shiny.Decals.Count);
    }

    // ------------------------------------------------------------------ the palettes

    private static IEnumerable<string> AllHandBuilt => PokemonModels.Species.Concat(PokemonModels.Forms);
    public static IEnumerable<object[]> HandBuilt => AllHandBuilt.Select(s => new object[] { s });

    [Theory]
    [MemberData(nameof(HandBuilt))]
    public void EveryHandBuiltShinyChangesItsMainColour(string model)
    {
        var colors = PokemonModels.Get(model).Mesh.Colors;
        var palette = ShinyPalettes.For(model, colors);
        var main = ShinyPalettes.MainColor(colors);
        var shined = palette.Map(main);
        Assert.False(shined.R == main.R && shined.G == main.G && shined.B == main.B, $"{model}'s main colour {main.R},{main.G},{main.B} stays as it is");
        // And the palette is a function: the same colour maps the same way every time
        Assert.Equal(shined, palette.Map(main));
    }

    [Fact]
    public void TheRuleTurnsTheHueByTheSpeciesAngleAndKeepsGreysAndDarks()
    {
        var blue = new Color(40, 90, 220, 255);
        var palette = new ShinyPalette("Test", 120f, blue);
        var (h, s, v) = ShinyPalette.ToHsv(blue);
        var (h2, s2, v2) = ShinyPalette.ToHsv(palette.Map(blue));
        Assert.InRange(Math.Abs(((h2 - h + 360f) % 360f) - 120f), 0f, 2f);
        Assert.InRange(Math.Abs(s2 - s), 0f, 0.02f);
        Assert.InRange(Math.Abs(v2 - v), 0f, 0.02f);
        // A white, a grey and a dark that aren't the body's main colour stay
        foreach (var kept in new[] { new Color(246, 246, 250, 255), new Color(128, 128, 132, 255), new Color(30, 24, 40, 255) })
            Assert.Equal(kept, palette.Map(kept));
        // A grey body is tinted
        var grey = new Color(150, 150, 156, 255);
        var tinted = new ShinyPalette("Test", 200f, grey).Map(grey);
        Assert.True(ShinyPalette.ToHsv(tinted).S >= ShinyPalette.GreyTint - 0.02f);
    }

    [Fact]
    public void TheAngleComesFromTheNumberAndNeverComesRoundToNothing()
    {
        for (int n = 0; n <= 1025; n++)
            Assert.InRange(ShinyPalettes.AngleOf(n), 60f, 300f);
        Assert.Equal(ShinyPalettes.AngleOf(130), ShinyPalettes.AngleOf(130));
        // A form takes its species' number
        Assert.Equal(ShinyPalettes.NumberOf("Charizard"), ShinyPalettes.NumberOf("Charizard-Mega-X"));
    }

    [Fact]
    public void AShinyGyaradosIsRed()
    {
        var colors = PokemonModels.Get("Gyarados").Mesh.Colors;
        var palette = ShinyPalettes.For("Gyarados", colors);
        Assert.True(palette.Picked);
        var (h, s, _) = ShinyPalette.ToHsv(palette.Map(ShinyPalettes.MainColor(colors)));
        Assert.True(s > 0.3f && (h > 330f || h < 20f), $"the main colour's hue is {h}");
    }

    [Fact]
    public void NoTwoPalettesOfALineAreTheSame()
    {
        int lines = 0;
        foreach (var name in PokemonModels.Species)
        {
            var species = PokemonDatabase.Get(name);
            if (species?.Evolutions == null) continue;
            foreach (var evolution in species.Evolutions)
            {
                if (PokemonDatabase.Get(evolution.TargetSpecies) is not { } into || into.Name == species.Name) continue;
                Assert.NotEqual(ShinyPalettes.Describe(species.Name), ShinyPalettes.Describe(into.Name));
                lines++;
            }
        }
        Assert.True(lines > 100);
    }

    [Fact]
    public void AMarksColourFollowsTheBodyAndTheEyesStay()
    {
        var palette = new ShinyPalette("Test", 150f, new Color(220, 60, 60, 255));
        var decal = new PokeDecal { Color = new Color(220, 200, 40, 255), Under = new Color(220, 60, 60, 255), Iris = new Color(40, 120, 220, 255), IsEye = true };
        var shined = decal.Shined(palette);
        Assert.NotEqual(decal.Color, shined.Color);
        Assert.NotEqual(decal.Under, shined.Under);
        Assert.Equal(decal.Iris, shined.Iris);
        // The model's own decal is never changed
        Assert.Equal(new Color(220, 200, 40, 255), decal.Color);
    }

    // ------------------------------------------------------------------ the sparkle

    private static FxList Sparkle(float age, int seed = 3911, bool fromBall = true)
    {
        var cue = new EffectCue
        {
            Kind = CueKind.Shiny, Lead = fromBall ? BattleAnimator.ShinyFromBall : BattleAnimator.ShinyFromAppearing,
            FromSide = BattleSide.Enemy, ToSide = BattleSide.Enemy, Seed = seed, Age = age
        };
        var fx = new FxList();
        MoveFx.Play(cue, new FxPlaces(), fx);
        return fx;
    }

    [Fact]
    public void TheSparkleIsTheSameForTheSameAgeAndSeedDrawsAndStops()
    {
        int most = 0;
        float lead = BattleAnimator.ShinyFromBall;
        for (float age = 0f; age < lead + BattleAnimator.ShinyTime + 0.2f; age += 0.02f)
        {
            var a = Sparkle(age);
            var b = Sparkle(age);
            Assert.Equal(a.Quads, b.Quads);
            Assert.True(a.Count <= MoveFx.ShinyCap, $"{a.Count} at {age}");
            Assert.Equal(0f, a.Flash);
            Assert.Equal(0f, a.Shake);
            most = Math.Max(most, a.Count);
        }
        Assert.True(most >= 8);
        // Nothing while the ball opens, and nothing once its time is up
        Assert.Equal(0, Sparkle(lead - 0.05f).Count);
        Assert.Equal(0, Sparkle(lead + BattleAnimator.ShinyTime + 0.05f).Count);
        Assert.True(Sparkle(lead + 0.2f).Count > 0);
        // Another seed scatters it otherwise
        Assert.False(Sparkle(lead + 0.2f).Quads.SequenceEqual(Sparkle(lead + 0.2f, seed: 7822).Quads));
    }

    private static BattleEngine Wild(Pokemon foe, bool scene = true)
    {
        var party = new Party();
        party.Add(Mon("Luxray", 40, "Tackle"));
        return new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(), WildPokemon = new List<Pokemon> { foe },
            Random = Steady(), Rules = Ruleset.Platinum, Scene = scene
        });
    }

    private static bool Sparkled(BattleEngine battle, int frames = 240)
    {
        for (int i = 0; i < frames; i++)
        {
            if (battle.Anim.Cues.Any(c => c.Kind == CueKind.Shiny)) return true;
            if (battle.IsWaitingForConfirm && i % 20 == 19) battle.ConfirmMessage();
            battle.Update(Frame);
        }
        return battle.Anim.Cues.Any(c => c.Kind == CueKind.Shiny);
    }

    [Fact]
    public void AShinyWildPokemonSparklesAsItAppearsAndAPlainOneDoesnt()
    {
        var shiny = Mon("Gyarados", 20);
        shiny.IsShiny = true;
        Assert.True(Sparkled(Wild(shiny)));
        var plain = Mon("Gyarados", 20);
        plain.IsShiny = false;
        Assert.False(Sparkled(Wild(plain)));
        // With the battle's scene off, as the original's animations that are no moves, it doesn't
        var again = Mon("Gyarados", 20);
        again.IsShiny = true;
        Assert.False(Sparkled(Wild(again, scene: false)));
    }

    [Fact]
    public void TheSparkleMovesNoSeedOfTheBattles()
    {
        // A move's cue is seeded by its own count: a sparkle before it leaves the seed what it was
        var anim = new BattleAnimator();
        var first = anim.Cue(new EffectCue { Kind = CueKind.Move, Move = "Tackle" });
        var other = new BattleAnimator();
        other.ShinySparkle(BattleSide.Enemy, 0, fromBall: false);
        var second = other.Cue(new EffectCue { Kind = CueKind.Move, Move = "Tackle" });
        Assert.Equal(first.Seed, second.Seed);
    }

    // ------------------------------------------------------------------ the star and the Pokédex

    [Fact]
    public void TheStarHasFivePointsStraightUpFirst()
    {
        var points = UiIcons.StarPoints(new Vector2(10, 10), 8f);
        Assert.Equal(10, points.Length);
        Assert.Equal(new Vector2(10, 2), points[0], new ApproximateVectors(0.01f));
        for (int i = 0; i < 10; i++)
            Assert.Equal(i % 2 == 0 ? 8f : 3.6f, Vector2.Distance(points[i], new Vector2(10, 10)), 3);
    }

    private sealed class ApproximateVectors(float tolerance) : IEqualityComparer<Vector2>
    {
        public bool Equals(Vector2 a, Vector2 b) => Vector2.Distance(a, b) <= tolerance;
        public int GetHashCode(Vector2 v) => 0;
    }

    [Fact]
    public void ThePokedexKeepsTheSpeciesCaughtShinyAndASaveKeepsThem()
    {
        var dex = new Pokedex();
        var plain = Mon("Magikarp", 5);
        plain.IsShiny = false;
        dex.RegisterCaught(plain);
        Assert.False(dex.IsCaughtShiny(plain.Species.DexNumber));
        var shiny = Mon("Gyarados", 30);
        shiny.IsShiny = true;
        dex.RegisterCaught(shiny);
        Assert.True(dex.IsCaughtShiny(shiny.Species.DexNumber));
        Assert.True(dex.IsCaught(shiny.Species.DexNumber));

        // Saved as numbers and read back
        var save = new SaveData { CaughtShiny = dex.CaughtShiny.OrderBy(n => n).ToList() };
        var json = System.Text.Json.JsonSerializer.Serialize(save);
        var read = System.Text.Json.JsonSerializer.Deserialize<SaveData>(json)!;
        var again = new Pokedex();
        foreach (int n in read.CaughtShiny) again.RegisterCaughtShiny(n);
        Assert.Equal(dex.CaughtShiny.OrderBy(n => n), again.CaughtShiny.OrderBy(n => n));
        // An older save has none
        Assert.Empty(System.Text.Json.JsonSerializer.Deserialize<SaveData>("{}")!.CaughtShiny);
        dex.Clear();
        Assert.Empty(dex.CaughtShiny);
    }
}
