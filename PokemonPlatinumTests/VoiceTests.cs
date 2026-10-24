using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 26 · V1: the catalogue of every spoken line, the cast and the keys. The catalogue in <c>Data/voice/lines.json</c>
/// and the report in <c>docs/voice/coverage.md</c> are what the game gives (regenerate with
/// <c>dotnet run --project tools/VoiceTool -- catalogue --add-missing</c>), every speaker is cast, and no spoken text
/// holds a placeholder the player or the game fills.
/// </summary>
public sealed class VoiceTests
{
    private static string Path_(string relative) => Path.Combine(Repository.Root, relative.Replace('/', Path.DirectorySeparatorChar));

    private static readonly Lazy<(VoiceCatalogue.Catalogue Built, VoiceFile Previous)> Game = new(() =>
    {
        var previous = VoiceCatalogue.Parse(File.ReadAllText(Path_("PokemonPlatinumEngine/Data/voice/lines.json")));
        return (VoiceCatalogue.Build(VoiceCast.Default.File, previous), previous);
    });

    private static string LinesFile => Path_("PokemonPlatinumEngine/Data/voice/lines.json");

    [Fact]
    public void TheCatalogueIsWhatTheGameGives()
    {
        string onDisk = File.ReadAllText(LinesFile).Replace("\r\n", "\n");
        Assert.True(onDisk == VoiceCatalogue.Serialize(Game.Value.Built.Lines),
            "Data/voice/lines.json is not what the game gives: run `dotnet run --project tools/VoiceTool -- catalogue --add-missing`.");
        string report = File.ReadAllText(Path_("docs/voice/coverage.md")).Replace("\r\n", "\n");
        Assert.True(report == VoiceCatalogue.Report(Game.Value.Built, VoiceCast.Default.File), "docs/voice/coverage.md is out of step with the catalogue.");
    }

    [Fact]
    public void EverySpeakerIsCast()
    {
        var built = Game.Value.Built;
        Assert.True(built.Unlisted.Count == 0, "Speakers with no cast entry: " + string.Join("; ", built.Unlisted.Select(u => $"{u.Key} ({u.Value})")));
        Assert.True(built.UnknownIds.Count == 0, "Speaker ids the cast lacks: " + string.Join("; ", built.UnknownIds.Select(u => $"{u.Key} ({u.Value})")));
    }

    [Fact]
    public void EveryLineHasAVoiceOfTheCast()
    {
        var cast = VoiceCast.Default;
        foreach (var line in Game.Value.Built.Lines)
            Assert.True(line.Voice == VoiceCast.Self || cast.HasVoice(line.Voice), $"{line.Key} is spoken by {line.Voice}, which the cast lacks");
    }

    [Fact]
    public void TheCastPointsAtVoicesThatAreThere()
    {
        var cast = VoiceCast.Default;
        var file = cast.File;
        foreach (var (name, target) in file.Names.Concat(file.Looks).Concat(file.People))
            Assert.True(cast.HasVoice(target) || VoiceCast.IsArchetype(target), $"'{name}' is cast as {target}, which is no voice");
        foreach (var (id, voice) in file.Voices)
            foreach (string chosen in voice.Chooses ?? new List<string>())
                Assert.True(file.Voices.ContainsKey(chosen), $"{id} chooses {chosen}, which is no voice");
        Assert.NotNull(file.Voices[VoiceCast.Rival].Clone);
    }

    [Fact]
    public void KeysAreUniqueAndHashesAreTheEnglishs()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var key = new Regex(@"^(script:[^#]+#\d+\.\d+|(world|map):[^.]+\.[^.]+\.(dialog|before|after)\.\d+|intro:[a-z]+\.[A-Za-z]+\.0)$");
        foreach (var line in Game.Value.Built.Lines)
        {
            Assert.True(seen.Add(line.Key), $"two lines are keyed {line.Key}");
            Assert.Matches(key, line.Key);
            Assert.Equal(VoiceCatalogue.HashOf(line.En), line.Hash);
        }
    }

    [Fact]
    public void NoSpokenTextHoldsAFreePlaceholder()
    {
        foreach (var line in Game.Value.Built.Lines)
        {
            if (line.Status == VoiceLine.StatusShownOnly) { Assert.Null(line.Spoken); continue; }
            Assert.NotNull(line.Spoken);
            var left = SpokenText.PlaceholdersOf(line.Spoken!);
            if (left.Count == 0) { Assert.Null(line.Variants); continue; }
            // What stays is a placeholder with few values, and every value has its variant
            Assert.True(line.Variants is { Count: > 0 }, $"{line.Key} says {string.Join(" ", left)} with no variants");
            foreach (var v in line.Variants!)
                Assert.True(SpokenText.PlaceholdersOf(v.Spoken).Count == 0, $"{line.Key} {v.Key} still holds a placeholder");
        }
    }

    [Fact]
    public void ALineWhoseEnglishChangedIsStaleAndOneThatMovedIsRecast()
    {
        var (built, previous) = Game.Value;
        var old = new VoiceFile { Lines = previous.Lines.Select(l => new VoiceLine { Key = l.Key, Voice = l.Voice, Hash = l.Hash }).ToList() };
        old.Lines[0].Hash = "00000000";
        old.Lines[1].Voice = "somebody_else";
        old.Lines.Add(new VoiceLine { Key = "script:gone.Gone#0.0", Voice = "rival", Hash = "00000000" });
        old.Lines.RemoveAt(2);
        var changes = VoiceCatalogue.Compare(old, built);
        Assert.Equal(new[] { built.Lines[0].Key }, changes.Stale);
        Assert.Equal(new[] { built.Lines[1].Key }, changes.Recast);
        Assert.Equal(new[] { "script:gone.Gone#0.0" }, changes.Orphaned);
        Assert.Equal(new[] { built.Lines[2].Key }, changes.Added);
        Assert.False(VoiceCatalogue.Compare(previous, built).Any);
    }

    [Fact]
    public void AHandWrittenSpokenTextAndDirectionAreKeptWhileTheEnglishHolds()
    {
        var built = Game.Value.Built;
        var line = built.Lines.First(l => l.Status == VoiceLine.StatusShownOnly);
        var before = new VoiceFile { Lines = { new VoiceLine { Key = line.Key, Hash = line.Hash, Manual = true, Spoken = "Said by hand.", Direction = "shouting", Notes = "n" } } };
        var again = VoiceCatalogue.Build(VoiceCast.Default.File, before).Lines.First(l => l.Key == line.Key);
        Assert.Equal("Said by hand.", again.Spoken);
        Assert.Equal(VoiceLine.StatusSpoken, again.Status);
        Assert.Equal("shouting", again.Direction);

        before.Lines[0].Hash = "00000000";
        again = VoiceCatalogue.Build(VoiceCast.Default.File, before).Lines.First(l => l.Key == line.Key);
        Assert.Null(again.Spoken);
        Assert.Equal("shouting", again.Direction);
    }

    [Theory]
    [InlineData("Hey, {player}! Over here!", "Hey! Over here!")]
    [InlineData("Oh, it's you, {player}!", "Oh, it's you!")]
    [InlineData("{player}, look at this.", "Look at this.")]
    [InlineData("Well, {rival}, it's time.", "Well, it's time.")]
    [InlineData("Plain words.", "Plain words.")]
    [InlineData("Wait.{pause 0.4} Now.", "Wait. Now.")]
    [InlineData("It was {red:very} big.", "It was very big.")]
    public void ANameThePlayerTypedIsNeverSpoken(string shown, string spoken)
    {
        var made = SpokenText.Make(shown, _ => null);
        Assert.Equal(spoken, made.Spoken);
    }

    [Fact]
    public void ALineThatCannotLoseItsPlaceholderIsShownOnly()
    {
        Assert.Null(SpokenText.Make("You found {item} here.", _ => null).Spoken);
        Assert.Null(SpokenText.Make("{player}!", _ => null).Spoken);
    }

    [Fact]
    public void APlaceholderWithFewValuesIsVoicedOncePerValue()
    {
        var made = SpokenText.Make("I picked {starter}, {player}!", name => name == "starter" ? new[] { "Turtwig", "Chimchar", "Piplup" } : null);
        Assert.Equal("I picked {starter}!", made.Spoken);
        Assert.Equal(new[] { "~starter=Turtwig", "~starter=Chimchar", "~starter=Piplup" }, made.Variants.Select(v => v.Key));
        Assert.Equal("I picked Piplup!", made.Variants[2].Spoken);
    }

    [Fact]
    public void ASpeakerMayNameItsVoiceBeforeItsName()
    {
        var library = ScriptLibrary.FromSources(("test_town", "script Talk\n  speaker rowan \"Prof. Rowan\"\n  say \"Hello.\"\n  speaker none\n  speaker \"Mom\"\n  say \"Hi.\"\n  end\n"));
        var script = library.Find("Talk", "test_town")!;
        var speakers = script.Code.Where(i => i.Op == Op.Speaker).ToList();
        Assert.Equal("rowan", speakers[0].VoiceId);
        Assert.Equal("Prof. Rowan", speakers[0].Name);
        Assert.Equal("", speakers[2].VoiceId);
        // The numbering of the lines does not move for it
        Assert.Equal(new[] { 0, 1, 2, 3 }, script.Code.Where(i => i.SpeaksText).Select(i => i.TextIndex));

        var wrong = ScriptLibrary.FromSources(("test_town", "script Talk\n  speaker nobody_at_all \"X\"\n  say \"Hello.\"\n  end\n"));
        Assert.Contains(wrong.Problems(), p => p.Contains("nobody_at_all"));
        Assert.DoesNotContain(library.Problems(), p => p.Contains("rowan"));
    }

    [Fact]
    public void APersonIsGivenTheSameVoiceOnEveryRun()
    {
        var cast = VoiceCast.Default;
        string a = cast.ForPerson("twinleaf_town", "lass_1", "", null, "Lass", "lass", false);
        Assert.Equal(a, cast.ForPerson("twinleaf_town", "lass_1", "", null, "Lass", "lass", false));
        Assert.StartsWith("class:lass#", a);
        Assert.Equal(VoiceCast.Rival, cast.ForPerson("route_203", "rival", "", "Barry", "Rival", "rival", true));
        Assert.Equal("rowan", cast.ForPerson("anywhere", "x", "", null, null, "Rowan", false));
        Assert.Equal(2166136261u, VoiceCast.Hash32(""));
    }
}
