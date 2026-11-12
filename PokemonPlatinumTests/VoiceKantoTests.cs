using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Core.Autoplay;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 26, Kanto's session: the whole of FireRed's story voiced like Sinnoh's. Kanto has its own casting where a name is
/// another person than Sinnoh's (the rival, the mother, a trainer called Aaron), the way through a script decides who
/// speaks a line, a trainer's tag is left out of what is said, and a pack made on the developer's machine is held to the
/// first chapter of a new game of Kanto.
/// </summary>
[Collection("Shared clocks and dice")]
public sealed class VoiceKantoTests : IDisposable
{
    private readonly ITestOutputHelper output;

    public VoiceKantoTests(ITestOutputHelper output) => this.output = output;

    public void Dispose() => AudioManager.StopListening();

    private static string Lines => Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Data", "voice", "lines.json");

    private static readonly Lazy<Dictionary<string, VoiceLine>> Catalogue = new(() =>
        VoiceCatalogue.Parse(File.ReadAllText(Lines)).Lines.ToDictionary(l => l.Key, StringComparer.Ordinal));

    [Fact]
    public void KantoHasItsOwnRivalAndMotherAndEveryRegionalTargetIsAVoice()
    {
        var cast = VoiceCast.Default;
        var kanto = cast.File.Regions["kanto"];
        foreach (var (name, target) in kanto.Names.Concat(kanto.Looks).Concat(kanto.Classes))
            Assert.True(cast.HasVoice(target) || VoiceCast.IsArchetype(target), $"Kanto casts '{name}' as {target}, which is no voice");
        Assert.True(cast.HasVoice(kanto.Rival!));
        Assert.NotEqual(VoiceCast.Rival, kanto.Rival);

        // A script of Kanto's: its rival and its mother; Sinnoh's keep the voices they had
        Assert.Equal("kanto_rival", cast.ForSpeaker("", "{rival}", "pallet_town_professor_oaks_lab.OnEnter/{rival}"));
        Assert.Equal(VoiceCast.Rival, cast.ForSpeaker("", "{rival}", "route_203.Rival/{rival}"));
        Assert.Equal("kanto_mom", cast.ForSpeaker("", "Mom", "pallet_town_players_house_1f.Mom/Mom"));
        Assert.Equal("mom", cast.ForSpeaker("", "Mom", "twinleaf_town.Mom/Mom"));
        // Oak is one man: Kanto and Sinnoh share his voice
        Assert.Equal(cast.ForSpeaker("", "Prof. Oak", "eterna_city.Oak/Prof. Oak"), cast.ForSpeaker("", "Prof. Oak", "pallet_town.OakStops/Prof. Oak"));
        // Kanto's Champion is the rival
        Assert.Equal("kanto_rival", cast.ForPerson("pokemon_league_champions_room", "rival", "", "Terry", "Champion", null, false));
        Assert.Equal("kanto_rival", cast.ForPerson("pewter_city", "rival", "", "{rival}", "Rival Late", null, true));
        Assert.Equal(VoiceCast.Rival, cast.ForPerson("route_203", "rival", "", "Barry", "Rival", null, true));
    }

    [Fact]
    public void AKantoTrainerIsCastByTheirClassAndNotByANameSomeOtherRegionGaveAVoice()
    {
        var cast = VoiceCast.Default;
        const string name = "Aaron";
        Assert.Equal("aaron", cast.ForPerson("route_203", "youngster_1", "", name, "Youngster", "youngster", false));
        // In Sinnoh the name picks the voice; in Kanto the class does
        Assert.StartsWith("class:youngster#", cast.ForPerson("pewter_city", "youngster_1", "", name, "Youngster", "youngster", false));
        // ... but the Gym Leaders are themselves
        Assert.Equal("brock", cast.ForPerson("pewter_city_gym", "brock", "", "Brock", "Leader Brock", "leader", false));
        Assert.Equal("giovanni", cast.ForPerson("viridian_city_gym", "giovanni", "", "Giovanni", "Leader", null, false));
    }

    [Fact]
    public void ALineSaidAfterALabelIsSpokenByWhoeverTheWayThereLeavesSpeaking()
    {
        // Oak's script `ProfOak` holds {rival}'s lines further up the file; what follows its labels is Oak's by the way the runner goes
        Assert.Equal("prof_oak", Catalogue.Value["script:pallet_town_professor_oaks_lab.ProfOak#15.0"].Voice);
        Assert.Equal("kanto_rival", Catalogue.Value["script:pallet_town_professor_oaks_lab.ProfOak#8.0"].Voice);
        Assert.Equal("selphy", Catalogue.Value["script:five_island_resort_gorgeous_house.Selphy#11.0"].Voice);
    }

    [Fact]
    public void AKantoTrainersTagIsNotSpokenAndSinnohsIsAsItWas()
    {
        var kanto = Catalogue.Value["world:cinnabar_island_gym.dusty.before.0"];
        Assert.StartsWith("Burglar Dusty: ", kanto.En);
        Assert.Equal("No sneaking past this question! Battle me!", kanto.Spoken);
        var sinnoh = Catalogue.Value["world:route_202.youngster_tristan.before.0"];
        Assert.StartsWith("Youngster Tristan: ", sinnoh.Spoken);
    }

    [Fact]
    public void EveryVoiceThatSpeaksKantoIsDesigned()
    {
        var cast = VoiceCast.Default;
        var voices = Catalogue.Value.Values.Where(l => VoiceCatalogue.RegionOf(l.Key) == "kanto" && l.Voice != VoiceCast.Self).Select(l => l.Voice).Distinct().ToList();
        Assert.True(voices.Count > 150, $"only {voices.Count} voices speak Kanto");
        foreach (string voice in voices)
        {
            Assert.True(cast.HasVoice(voice), $"{voice} is no voice of the cast");
            if (!VoiceCast.IsArchetype(voice)) Assert.True(cast.File.Voices[voice].Design != null, $"{voice} speaks Kanto with no design");
            else Assert.True(cast.File.Voices.GetValueOrDefault(voice)?.Design != null, $"{voice} speaks Kanto with no design");
        }
    }

    // ------------------------------------------------------------------ the pack made on the developer's machine

    private static string? RealPackFolder()
    {
        if (Environment.GetEnvironmentVariable("VOICE_PACK") is { Length: > 0 } given) return given;
        string home = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "pokemon-voicegen", "voice", "en");
        return Directory.Exists(home) ? home : null;
    }

    /// <summary>The way through Kanto's first chapter: the bedroom, Oak's lab, the rival's challenge, Viridian's Mart and back to Oak for the Pokédex.</summary>
    private static readonly Leg[] Opening =
    {
        Leg.Enter("PalletTown_PlayersHouse_1F"),
        Leg.Trigger("Kanto", "OakStops", "pallet_town"),
        Leg.Talk("PalletTown_ProfessorOaksLab", "bulbasaur_ball"),
        Leg.Trigger("PalletTown_ProfessorOaksLab", "RivalChallenges"),
        Leg.Trigger("Kanto", "RoadBlocked", "viridian_city"),
        Leg.Enter("ViridianCity_Mart"),
        Leg.Enter("PalletTown_ProfessorOaksLab"),
        Leg.Talk("PalletTown_ProfessorOaksLab", "oaks_lab_prof_oak"),
    };

    [Fact]
    public void TheFirstChapterOfANewGameOfKantoResolvesEveryLineToATake()
    {
        string? where = RealPackFolder();
        var pack = where == null ? null : VoiceLibrary.Open(where);
        if (pack == null || !pack.Entries.Any(e => e.Voice == "kanto_rival"))
        {
            output.WriteLine("No voice pack with Kanto's voices here (~/.cache/pokemon-voicegen/voice/en or VOICE_PACK): nothing to hold the chapter to.");
            return;
        }
        List<string> heard = new();
        try
        {
            Headless.Play(engine =>
            {
                engine.Settings.Voices = VoiceMode.All;
                engine.VoicePackOverride = pack;
                engine.ApplySettings(window: false);
                heard = AudioManager.Listen();
                var bot = new Autoplayer(engine, seed: 1) { Itinerary = new Itinerary(Opening) };
                const float dt = 1f / 60f;
                for (double t = 0; t < 1800 && !bot.Stopped; t += dt)
                {
                    bot.Frame(dt);
                    if (bot.Itinerary!.Finished && t > 1) break;
                }
                bot.Release();
                output.WriteLine(bot.Report.ToMarkdown());
            }, region: "Kanto");
        }
        finally
        {
            pack.Settle();
        }

        int spoken = 0, nobodys = 0;
        var missing = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string entry in heard.Where(h => h.StartsWith("voice ", StringComparison.Ordinal) && h != "voice play" && h != "voice cut").Distinct())
        {
            string[] parts = entry.Split(' ');
            if (parts.Length < 3) continue;
            string key = parts[1], voice = parts[2];
            string lineKey = key.Contains('~') ? key[..key.IndexOf('~')] : key;
            if (voice == "-" || !Catalogue.Value.TryGetValue(lineKey, out var line) || line.Status != VoiceLine.StatusSpoken) { nobodys++; continue; }
            spoken++;
            // The blackout scene's two lines are "@self" lines of a script every region shares: the pack has them for Sinnoh's mother only (a Kanto one is owed, plan 26)
            if (lineKey.StartsWith("script:common.BlackOutHome", StringComparison.Ordinal)) continue;
            if (pack.StateOf(key, line.En, voice).State != VoiceState.Current) missing.Add($"{key} ({voice})");
        }
        output.WriteLine($"{spoken} lines of Kanto's first chapter asked for a voice that the catalogue speaks; {nobodys} are nobody's or shown only; {missing.Count} have no current take.");
        Assert.True(spoken >= 25, $"only {spoken} spoken lines were asked for: the chapter did not play with the voices on");
        Assert.True(missing.Count == 0, "no current take for:\n" + string.Join("\n", missing.Take(40)));
    }

    /// <summary>The keys of Kanto's lines whose takes the pack doesn't have yet, each a later plan's (see the test below).</summary>
    private static readonly string[] OwedTakes =
    {
        "script:two_island.CaseKanto", "script:vermilion_city.CaseKanto", "world:celadon_city.celadon_city.Festival_",
    };

    [Fact]
    public void EveryLineOfKantoTheCatalogueGivesAVoiceOfItsOwnHasATakeInThePackThatHasKantoInIt()
    {
        string? where = RealPackFolder();
        var pack = where == null ? null : VoiceLibrary.Open(where);
        if (pack == null || !pack.Entries.Any(e => e.Voice == "kanto_rival"))
        {
            output.WriteLine("No voice pack with Kanto's voices here: nothing to hold the catalogue to.");
            return;
        }
        try
        {
            var voices = VoiceCast.Default.File.Voices;
            var missing = new List<string>();
            int held = 0;
            foreach (var line in Catalogue.Value.Values)
            {
                if (VoiceCatalogue.RegionOf(line.Key) != "kanto" || line.Status != VoiceLine.StatusSpoken || line.Voice == VoiceCast.Self) continue;
                // Lines written after Kanto's pack was generated: the Open Door Agency's Kanto case (plan 41 · EG8: the
                // harbour keeper on Two Island, and the clerk, the fisherman and Dov on Vermilion's quay) and the festival
                // stall keepers (plan 40 · GE9). Their voices are designed; their takes are owed to the next run of
                // `VoiceTool generate` (plan 26), and this list goes when they are made
                if (OwedTakes.Any(owed => line.Key.StartsWith(owed, StringComparison.Ordinal))) continue;
                var keys = line.Variants is { Count: > 0 } v ? v.Select(x => line.Key + x.Key).ToList() : new List<string> { line.Key };
                var speakers = voices.TryGetValue(line.Voice, out var entry) && entry.Chooses is { Count: > 0 } chooses ? chooses : new List<string> { line.Voice };
                foreach (string speaker in speakers)
                    foreach (string key in keys)
                    {
                        held++;
                        if (pack.StateOf(key, line.En, speaker).State != VoiceState.Current) missing.Add($"{key} ({speaker})");
                    }
            }
            output.WriteLine($"{held} takes of Kanto's lines with a voice of their own; {missing.Count} are missing or stale.");
            Assert.True(missing.Count == 0, "no current take for:\n" + string.Join("\n", missing.Take(40)));
        }
        finally
        {
            pack.Settle();
        }
    }
}
