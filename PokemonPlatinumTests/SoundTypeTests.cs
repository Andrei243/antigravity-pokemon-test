using System;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 46 · CP2: the Sound type, a nineteenth of our own. Its chart (decision 13), the games' sound moves left as they
/// are (decision 14), everything a type has, and what leaves it out so that nothing of the games' moves.
/// </summary>
public class SoundTypeTests
{
    private static float E(PokemonType a, PokemonType d, bool inverse = false) => TypeChart.GetEffectiveness(a, d, null, Ruleset.Modern, inverse);

    [Fact]
    public void SoundIsTheLastTypeWithThePlansChart()
    {
        Assert.Equal(PokemonType.Sound, Enum.GetValues<PokemonType>().Last());
        foreach (var d in new[] { PokemonType.Rock, PokemonType.Ice, PokemonType.Psychic }) Assert.Equal(2f, E(PokemonType.Sound, d));
        foreach (var d in new[] { PokemonType.Steel, PokemonType.Ground, PokemonType.Sound }) Assert.Equal(0.5f, E(PokemonType.Sound, d));
        foreach (var a in new[] { PokemonType.Dark, PokemonType.Electric, PokemonType.Ground }) Assert.Equal(2f, E(a, PokemonType.Sound));
        foreach (var a in new[] { PokemonType.Normal, PokemonType.Flying, PokemonType.Sound }) Assert.Equal(0.5f, E(a, PokemonType.Sound));
        // Nothing else is touched: no type is stopped by it or stops it, and the rest are plain
        int plain = Enum.GetValues<PokemonType>().Count(t => E(PokemonType.Sound, t) == 1f);
        Assert.Equal(19 - 6, plain);
        Assert.All(Enum.GetValues<PokemonType>(), t => Assert.NotEqual(0f, E(PokemonType.Sound, t)));
        Assert.All(Enum.GetValues<PokemonType>(), t => Assert.NotEqual(0f, E(t, PokemonType.Sound)));
        // An inverse battle turns its row round
        Assert.Equal(0.5f, E(PokemonType.Sound, PokemonType.Rock, inverse: true));
        Assert.Equal(2f, E(PokemonType.Normal, PokemonType.Sound, inverse: true));
        // The same under Platinum's rules: no species or move of the games has it, so nothing of theirs meets a row
        Assert.Equal(2f, TypeChart.GetEffectiveness(PokemonType.Sound, PokemonType.Rock, null, Ruleset.Platinum));
    }

    [Fact]
    public void NoSpeciesOrMoveOfTheGamesHasItAndTheirSoundMovesKeepTheirTypes()
    {
        Assert.DoesNotContain(PokemonDatabase.GetAll(), s => s.PrimaryType == PokemonType.Sound || s.SecondaryType == PokemonType.Sound
            || s.Forms?.Any(f => f.Types?.Contains(PokemonType.Sound) == true) == true);
        Assert.All(MoveDatabase.GetAll().Where(m => m.Type == PokemonType.Sound), m => Assert.Equal(0, m.Generation));
        Assert.Equal(PokemonType.Normal, MoveDatabase.Get("Hyper Voice").Type);
        Assert.Equal(PokemonType.Normal, MoveDatabase.Get("Boomburst").Type);
        // Every Sound move one can learn is a sound move; its Z-Move and Max Move carry no flags, as none do
        Assert.All(MoveDatabase.GetAll().Where(m => m.Type == PokemonType.Sound && m.Kind == MoveKind.Standard),
            m => Assert.True(m.Flags.HasFlag(MoveFlags.Sound), $"{m.Name} is no sound move"));
    }

    [Fact]
    public void SoundHasWhatEveryTypeHas()
    {
        Assert.Equal("Sound", Words.Type(PokemonType.Sound));
        Assert.NotEmpty(TypeSigns.Parts(PokemonType.Sound));
        Assert.NotEqual(Palette.GetTypeColor("NONE"), Palette.GetTypeColor("Sound"));
        Assert.Contains(TeraType.Of(PokemonType.Sound), TeraChange.Types);
        Assert.NotNull(ItemDatabase.Get("Sound Tera Shard"));
        Assert.Equal("GemSound", ItemDatabase.Get("Sound Gem")!.HoldEffect);
        Assert.Equal(PokemonType.Sound, HeldItemEffects.TypeBoostedBy(ItemDatabase.Get("Tuning Fork")!.HoldEffect!));
        Assert.Equal(20, ItemDatabase.Get("Tuning Fork")!.HoldParam);
        Assert.Equal(PokemonType.Sound, HeldItemEffects.TypeWeakenedBy(ItemDatabase.Get("Taci Berry")!.HoldEffect!));
        Assert.All(new[] { "Sound Gem", "Tuning Fork", "Taci Berry" }, n => Assert.True(HeldItemEffects.IsHoldable(ItemDatabase.Get(n)!), n));
        Assert.Equal(PokemonType.Sound, ItemDatabase.Get("Sonorium Z")!.ZCrystal!.Type);
        Assert.Equal(MoveKind.ZMove, MoveDatabase.Get("Resounding Finale").Kind);
        Assert.Equal(MoveKind.MaxMove, MoveDatabase.Get("Max Chorus").Kind);
        var song = new MoveData { Name = "Test Song", Type = PokemonType.Sound, Category = MoveCategory.Special, Power = 80 };
        Assert.Equal("Max Chorus", BattleCore.MaxMoveOf(PokemonDatabase.Get("Pikachu")!, song, false, null)!.Name);
        Assert.NotNull(MaterialTable.OfType(PokemonType.Sound, rare: false));
        Assert.NotNull(MaterialTable.OfType(PokemonType.Sound, rare: true));
    }

    [Fact]
    public void WhatGoesByTheGamesTypesLeavesItOut()
    {
        Assert.Equal(18, TypeChart.GamesTypes.Count);
        Assert.DoesNotContain(PokemonType.Sound, TypeChart.GamesTypes);
        // With no species of it in the data yet, a choice of a type offers none of it
        Assert.DoesNotContain(PokemonType.Sound, TypeChart.TypesWithSpecies);
        Assert.Equal(18, PokemonPlatinumEngine.UI.HelpScreen.Types.Length);
        // The randomiser's shuffle is the eighteen's: a Sound species keeps its type, the others are dealt as ever
        var random = Randomizer.Of(new RandomizerOptions { Seed = 12345, Types = true });
        Assert.Equal(PokemonType.Sound, random.TypeIn(10001, PokemonType.Sound));
        Assert.NotEqual(PokemonType.Sound, random.TypeIn(10001, PokemonType.Fire));
    }
}
