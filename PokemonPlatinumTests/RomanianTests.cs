using System.Text.Json;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Models;
using static PokemonPlatinumTests.Scenario;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 17 · N6, part a: the first translation, Romanian's <c>ui.json</c>. Its table has every key of English's and
/// no other, each entry takes only the placeholders its English has and the forms Romanian's plural rule asks for,
/// every character it writes is one the typeface has (with the comma below on ș and ț, never the cedilla), the
/// language is offered, and what is measured in English is measured in Romanian too: the battle's two lines, the
/// field's text box, the title's notices. Changing the language is the whole process's, so the class runs alone.
/// </summary>
[Collection("Shared clocks and dice")]
public class RomanianTests
{
    private const string Romanian = "ro";

    /// <summary>Entries whose placeholders differ from English's on purpose, with why.</summary>
    private static readonly Dictionary<string, string> OwnPlaceholders = new()
    {
        // Romanian has no article that changes with the next word, so the composed Pokédex entry drops it
        ["dexText.entry"] = "{article}",
        // A screen's line formatted with both of the team's forms: Romanian makes the team the subject and takes the second
        ["battle.theLuckyChantShielded"] = "{0} {1}",
    };

    private static Dictionary<string, JsonElement> Entries(string tag)
    {
        var table = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        string path = Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Data", "text", tag, "ui.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        void Walk(JsonElement element, string prefix)
        {
            foreach (var property in element.EnumerateObject())
            {
                string key = prefix.Length == 0 ? property.Name : prefix + "." + property.Name;
                var value = property.Value;
                bool forms = value.ValueKind == JsonValueKind.Object && value.EnumerateObject().Any()
                    && value.EnumerateObject().All(p => p.Value.ValueKind == JsonValueKind.String
                        && p.Name is "zero" or "one" or "two" or "few" or "many" or "other" or "boy" or "girl");
                if (value.ValueKind == JsonValueKind.String || forms) table[key] = value.Clone();
                else Walk(value, key);
            }
        }
        Walk(document.RootElement, "");
        return table;
    }

    private static IEnumerable<string> Strings(JsonElement entry) =>
        entry.ValueKind == JsonValueKind.String ? new[] { entry.GetString()! } : entry.EnumerateObject().Select(p => p.Value.GetString()!);

    private static HashSet<string> Holes(JsonElement entry) =>
        Strings(entry).SelectMany(s => Regex.Matches(s, @"\{[A-Za-z0-9]+\}").Select(m => m.Value)).ToHashSet();

    /// <summary>Every trainer of every region's files as (class, name), Master Trainers' classes left out (they name their species).</summary>
    private static List<(string Class, string Name)> EveryTrainer()
    {
        string data = Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Data");
        var files = new List<string> { Path.Combine(data, "trainers.json"), Path.Combine(data, "our-trainers.json") };
        files.AddRange(Directory.GetFiles(Path.Combine(data, "world"), "trainers.json", SearchOption.AllDirectories));
        var all = new List<(string, string)>();
        foreach (string file in files.Where(File.Exists))
            using (var document = JsonDocument.Parse(File.ReadAllText(file)))
                foreach (var t in document.RootElement.EnumerateArray())
                    if (t.TryGetProperty("class", out var c) && c.GetString() is { Length: > 0 } cls)
                        all.Add((cls, t.TryGetProperty("name", out var n) ? n.GetString() ?? "" : ""));
        return all;
    }

    [Fact]
    public void EveryTrainerClassOfEveryRegionHasANameInEnglishAndRomanian()
    {
        // Plan 17 · N6d: Kanto's and Johto's classes came out as the data wrote them ("Swimmer M", "Bird Keeper Gs").
        // The failures are listed as the lines to add to both tables, so a session that brings a class adds them at once.
        var english = Entries("en");
        var romanian = Entries(Romanian);
        var missing = EveryTrainer().Select(t => t.Class).Distinct().OrderBy(c => c, StringComparer.Ordinal)
            .Where(c => !c.EndsWith(" Master", StringComparison.Ordinal))
            .Select(c => (Class: c, Key: "classes." + PokemonPlatinumEngine.Models.Trainer.KeyOf(c)))
            .Where(c => !english.ContainsKey(c.Key) || !romanian.ContainsKey(c.Key))
            .Select(c => $"\"{c.Key["classes.".Length..]}\"   (class \"{c.Class}\"; en: {english.ContainsKey(c.Key)}, ro: {romanian.ContainsKey(c.Key)})")
            .ToList();
        Assert.True(missing.Count == 0, "Classes with no `classes.*` key in en/ui.json and ro/ui.json:\n" + string.Join("\n", missing));
    }

    [Fact]
    public void ARomanianClassAgreesWithTheEnglishOneItTranslatesAndNoClassIsStillRaw()
    {
        try
        {
            Text.Use(Romanian);
            Assert.Equal("Înotător", PokemonPlatinumEngine.Models.Trainer.ClassName("Swimmer M"));
            Assert.Equal("Tocilar", PokemonPlatinumEngine.Models.Trainer.ClassName("Super Nerd"));
            Assert.Equal("Călugăr", PokemonPlatinumEngine.Models.Trainer.ClassName("Sage"));
            Text.Use(Text.English);
            Assert.Equal("Swimmer", PokemonPlatinumEngine.Models.Trainer.ClassName("Swimmer M"));
            Assert.Equal("Bird Keeper", PokemonPlatinumEngine.Models.Trainer.ClassName("Bird Keeper Gs"));
            Assert.Equal("Rocket", PokemonPlatinumEngine.Models.Trainer.ClassName("Team Rocket"));
        }
        finally { Text.Use(Text.English); }
    }

    [Fact]
    public void ABattleTitleOfTheWidestClassAndNameFitsTheMessageBoxInBothLanguages()
    {
        var trainers = EveryTrainer();
        string widestName = trainers.Select(t => t.Name).Distinct().OrderByDescending(n => n.Length).First();
        var classes = trainers.Select(t => t.Class).Where(c => !c.EndsWith(" Master", StringComparison.Ordinal)).Distinct().ToList();
        var keys = Text.EnglishKeys.Where(k => k.StartsWith("battle.", StringComparison.Ordinal))
            .Where(k => Text.Of(k, ("n", 1L)).Contains("{trainer}")).OrderBy(k => k).ToList();
        Assert.NotEmpty(keys);
        var tooLong = new List<string>();
        try
        {
            foreach (string tag in new[] { "en", Romanian })
            {
                Text.Use(tag);
                var widest = classes.Select(PokemonPlatinumEngine.Models.Trainer.ClassName).Distinct()
                    .OrderByDescending(c => TextMetrics.Measure(c, ModernUi.MessageSize, UiWeight.ExtraBold)).Take(3).ToList();
                foreach (string cls in widest)
                    foreach (string key in keys)
                    {
                        string line = Text.Of(key, ("n", 1L)).Replace("{trainer}", cls + " " + widestName);
                        line = Regex.Replace(line, @"\{[A-Za-z0-9]+\}", "Kangaskhan");
                        if (TextMetrics.Wrap(line, ModernUi.MessageBoxWidth - ModernUi.MessageMargin, ModernUi.MessageSize, UiWeight.ExtraBold).Count > 2)
                            tooLong.Add($"{tag} {key}: {line}");
                    }
            }
        }
        finally { Text.Use(Text.English); }
        Assert.True(tooLong.Count == 0, "Titles that run past the box's two lines:\n" + string.Join("\n", tooLong));
    }

    [Fact]
    public void RomanianIsOfferedAndCallsItselfByItsOwnName()
    {
        var romanian = Assert.Single(Language.Offered, l => l.Tag == Romanian);
        Assert.Equal("Română", romanian.Name);
        Assert.True(romanian.HasTable);
        Assert.Empty(TextMetrics.Undrawable(romanian.Name, UiWeight.Black));
    }

    [Fact]
    public void RomaniansTableHasEveryKeyOfEnglishsAndNoOther()
    {
        var english = Text.EnglishKeys;
        var romanian = Text.KeysOf(Romanian);
        Assert.Empty(english.Except(romanian).OrderBy(k => k));
        // The places' names are the one section a language fills where English has nothing (plan 17 · N4, N6): an
        // area's English name is its file's, and places.<its letters> is what another language shows instead
        Assert.Empty(romanian.Except(english).Where(k => !k.StartsWith("places.", StringComparison.Ordinal)).OrderBy(k => k));
    }

    [Fact]
    public void EveryPlaceRomanianNamesIsAnAreaOfARegionAndShowsOnItsSign()
    {
        // Every region's areas (plan 17 · N6c: Kanto's and Johto's places are named too)
        var areas = Directory.GetDirectories(Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Data", "world"))
            .Select(region => Path.Combine(region, "areas")).Where(Directory.Exists)
            .SelectMany(folder => Directory.GetFiles(folder, "*.json"))
            .Select(f => JsonDocument.Parse(File.ReadAllText(f)).RootElement.GetProperty("name").GetString()!)
            .Select(PokemonPlatinumEngine.Models.Trainer.KeyOf).ToHashSet(StringComparer.Ordinal);
        var named = Text.KeysOf(Romanian).Where(k => k.StartsWith("places.", StringComparison.Ordinal)).Select(k => k["places.".Length..]).ToList();
        Assert.NotEmpty(named);
        // The Battle Zone is no area but the region the Fight, Survival and Resort Areas lie in, which lines name: its
        // Romanian name is kept here so every line names it alike (plan 17 · N6b, slice 3's decision)
        Assert.Empty(named.Where(n => !areas.Contains(n) && n != "BattleZone").OrderBy(n => n));
        try
        {
            Text.Use(Romanian);
            Assert.Equal("Orășelul Twinleaf", LocalNames.PlaceShown(null, "Twinleaf Town"));
            Assert.Equal("Ruta 201", LocalNames.PlaceShown(null, "Route 201"));
            Assert.Equal("Liga Pokémon", LocalNames.PlaceShown(null, "Pokémon League"));
            Assert.Equal("Ținutul Luptelor", LocalNames.PlaceShown(null, "Battle Zone"));
            Assert.Equal("Orășelul Pallet", LocalNames.PlaceShown(null, "Pallet Town"));
            Assert.Equal("Orașul Violet", LocalNames.PlaceShown(null, "Violet City"));
        }
        finally { Text.Use(Text.English); }
    }

    [Fact]
    public void TheHallOfFameCardNamesWhereAPokemonWasMetAsTheLanguageNamesThePlace()
    {
        // Plan 17 · N6c: the card said "Întâlnit în Route 201", the place a Pokémon keeps being the English identifier
        var starly = new Pokemon(PokemonDatabase.Get("Starly")!, 57);
        starly.Met("Route 201", new DateTime(2026, 5, 1));
        var eevee = new Pokemon(PokemonDatabase.Get("Eevee")!, 20);
        eevee.Met("Pallet Town", new DateTime(2026, 5, 2));
        eevee.MetLevel = 0;
        var scene = new HallOfFameCeremony();
        scene.Begin(new List<Pokemon> { starly, eevee }, "Ash", 4321, 3600, "PLAYER");
        Assert.Equal("Met at Route 201, at Lv. 57.", scene.CardOf(starly).Met);
        try
        {
            Text.Use(Romanian);
            Assert.Equal("Întâlnit: Ruta 201, la Nv. 57.", scene.CardOf(starly).Met);
            Assert.Equal("Eclozat: Orășelul Pallet.", scene.CardOf(eevee).Met);
        }
        finally { Text.Use(Text.English); }
        Assert.Equal("Hatched at Pallet Town.", scene.CardOf(eevee).Met);
    }

    [Fact]
    public void TheProfessorsNameOverTheIntroductionIsTheLanguagesOwn()
    {
        // Plan 17 · N6c: "Prof. Rowan" was English's abbreviation over every line of the introduction
        var intro = new IntroScreen();
        Assert.Equal("Prof. Rowan", intro.Professor);
        try
        {
            Text.Use(Romanian);
            Assert.Equal("Profesorul Rowan", intro.Professor);
        }
        finally { Text.Use(Text.English); }
    }

    [Fact]
    public void TheMoveAPokemonTriesToLearnIsNamedInRomanianInEveryLineOfTheQuestion()
    {
        // Plan 17 · N6b, slice 4: the lines round the question named the move in English
        try
        {
            Text.Use(Romanian);
            var turtwig = Mon("Turtwig", 8, "Tackle", "Withdraw", "Growl", "Leer");
            turtwig.CurrentExp = turtwig.ExpForNextLevel - 1;
            var foe = Mon("Magikarp", 5);
            foe.CurrentHP = 1;
            var battle = Battle(turtwig, foe);
            battle.SelectMove(0);
            var said = new List<string>();
            for (int i = 0; i < 2000 && !battle.IsBattleOver; i++)
            {
                if (battle.IsWaitingForConfirm || battle.IsLearningMove) said.Add(battle.CurrentMessage);
                battle.ConfirmMessage();
                battle.Update(1f / 60f);
            }
            Assert.Contains("Turtwig încearcă să învețe Absorbție.", said);
            Assert.DoesNotContain(said, line => line.Contains("Absorb ", StringComparison.Ordinal) || line.EndsWith("Absorb.", StringComparison.Ordinal));

            // With room for it the rules learn it themselves, and say so in the same words
            var roomy = Mon("Turtwig", 8, "Tackle", "Withdraw");
            roomy.CurrentExp = roomy.ExpForNextLevel - 1;
            var again = Mon("Magikarp", 5);
            again.CurrentHP = 1;
            var second = Battle(roomy, again);
            second.SelectMove(0);
            var heard = new List<string>();
            for (int i = 0; i < 2000 && !second.IsBattleOver; i++)
            {
                if (second.IsWaitingForConfirm) heard.Add(second.CurrentMessage);
                second.ConfirmMessage();
                second.Update(1f / 60f);
            }
            Assert.Contains("Turtwig a învățat Absorbție!", heard);
        }
        finally { Text.Use(Text.English); }
    }

    [Fact]
    public void TheIntroductionIsSaidInRomanianWithTheNamesFilledAfterTheLineIsFound()
    {
        try
        {
            Text.Use(Romanian);
            var intro = new IntroScreen();
            intro.Open();
            var said = new List<string>();
            for (int guard = 0; guard < 20000 && intro.Phase != IntroPhase.Done; guard++)
            {
                if (intro.Talking)
                {
                    if (intro.LineComplete) said.Add(intro.SpokenText);
                    intro.PressConfirm();
                }
                else if (intro.Phase is IntroPhase.ChooseLook or IntroPhase.ConfirmLook or IntroPhase.ConfirmName or IntroPhase.ConfirmRival)
                    intro.PressConfirm();
                else if (intro.Phase is IntroPhase.EnterName or IntroPhase.EnterRival)
                {
                    intro.PressStart();
                    intro.PressConfirm();
                }
                intro.Advance(1f / 60f);
            }
            Assert.Equal(IntroPhase.Done, intro.Phase);
            Assert.Contains("Bună! Mă bucur că ai putut veni.", said);
            // A line with a name in it is found by its English as the file writes it, {name} and all
            Assert.Contains("Lucas! Ce nume frumos.", said);
            Assert.Contains(said, line => line.StartsWith("Barry, desigur!", StringComparison.Ordinal));
            Assert.DoesNotContain(said, line => line.Contains("fine name", StringComparison.Ordinal));
        }
        finally { Text.Use(Text.English); }
    }

    [Fact]
    public void TheProfessorSpeaksRomanianFromTheTableBesideTheRegionFiles()
    {
        var table = Text.DataTable(Romanian);
        var lines = RegionDatabase.All.Select(r => r.Professor).Where(p => p != null)
            .SelectMany(p => new[] { p!.Lines.Hello, p.Lines.Welcome, p.Lines.MyName, p.Lines.World, p.Lines.OneOfThem, p.Lines.LiveAlongside,
                p.Lines.Study, p.Lines.Enough, p.Lines.BoyOrGirl, p.Lines.AskName, p.Lines.NextDoor, p.Lines.NeverStill, p.Lines.FineName,
                p.Lines.OfCourse, p.Lines.Story, p.Lines.OffYouGo })
            .ToHashSet(StringComparer.Ordinal);
        // The battle's trainer lines share the table (plan 17 · N6, wave 20), and so do the professors' names, which
        // stand over the introduction's lines (plan 17 · N6c)
        lines.UnionWith(TrainerLineTexts());
        lines.UnionWith(RegionDatabase.All.Select(r => r.Professor?.Name).OfType<string>());
        // And the Pokégear's radio (plan 17 · N6f)
        lines.UnionWith(RadioShows.AllLines);
        // Every line of the table is a line a region's file or trainer-lines.json has (a line rewritten in English leaves its entry behind)
        Assert.Empty(table.Keys.Where(k => !lines.Contains(k)).OrderBy(k => k));
        // Every line of Sinnoh's professor is in it, with the placeholders its English has and no character the faces lack
        var sinnoh = RegionDatabase.Get(RegionDatabase.Sinnoh)!.Professor!.Lines;
        foreach (string line in new[] { sinnoh.Hello, sinnoh.Story, sinnoh.FineName, sinnoh.OfCourse, sinnoh.OffYouGo })
            Assert.True(table.ContainsKey(line), line);
        foreach (var (en, ro) in table)
        {
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(ro, @"\{[a-z]+\}"))
                Assert.Contains(m.Value, en);
            Assert.Empty(TextMetrics.Undrawable(ro, UiWeight.Bold));
        }
        try
        {
            Text.Use(Romanian);
            Assert.Equal("Bună! Mă bucur că ai putut veni.", Text.OfData(sinnoh.Hello));
            Assert.Equal("A line no table has.", Text.OfData("A line no table has."));
        }
        finally { Text.Use(Text.English); }
        Assert.Equal(sinnoh.Hello, Text.OfData(sinnoh.Hello));
    }

    [Fact]
    public void ThePokegearsRadioSpeaksRomanianAndDrawsTheSameLineInEveryLanguage()
    {
        // Plan 17 · N6f: every station's line has a Romanian one in data.json, with the same placeholders
        var table = Text.DataTable(Romanian);
        var wrong = new List<string>();
        foreach (string en in RadioShows.AllLines)
        {
            if (!table.TryGetValue(en, out var ro) || ro.Length == 0) { wrong.Add($"no Romanian for: {en}"); continue; }
            var holes = Regex.Matches(en, @"\{[a-z]+\}").Select(m => m.Value).OrderBy(h => h).ToList();
            if (!holes.SequenceEqual(Regex.Matches(ro, @"\{[a-z]+\}").Select(m => m.Value).OrderBy(h => h))) wrong.Add($"placeholders differ: {ro}");
            if (TextMetrics.Undrawable(ro, UiWeight.ExtraBold).Any()) wrong.Add($"can't be drawn: {ro}");
        }
        Assert.True(wrong.Count == 0, string.Join("\n", wrong));
        // The dice draw the line from the English, so a seed gives the same line in both languages
        string english = RadioShows.Line(RadioStation.PokemonTalk, new Random(3), "Pidgey", "Route 29");
        try
        {
            Text.Use(Romanian);
            string romanian = RadioShows.Line(RadioStation.PokemonTalk, new Random(3), "Pidgey", "Ruta 29");
            Assert.NotEqual(english, romanian);
            Assert.Contains(romanian, RadioShows.Of(RadioStation.PokemonTalk).Select(l => table[l].Replace("{species}", "Pidgey").Replace("{place}", "Ruta 29")));
        }
        finally { Text.Use(Text.English); }
        Assert.Equal(english, RadioShows.Line(RadioStation.PokemonTalk, new Random(3), "Pidgey", "Route 29"));
    }

    /// <summary>Every line of <c>trainer-lines.json</c>, as its English is written.</summary>
    private static HashSet<string> TrainerLineTexts() =>
        TrainerLines.All.SelectMany(e => Enum.GetValues<TrainerCue>().Select(cue => e.Value.For(cue)?.Text))
            .Where(t => !string.IsNullOrEmpty(t)).Select(t => t!).ToHashSet(StringComparer.Ordinal);

    [Fact]
    public void EveryTrainerLineHasARomanianOneThatFitsTheBattlesTwoLines()
    {
        var table = Text.DataTable(Romanian);
        string longest = new('W', PlayerIdentity.MaxNameLength);
        var wrong = new List<string>();
        foreach (string en in TrainerLineTexts().OrderBy(t => t, StringComparer.Ordinal))
        {
            if (!table.TryGetValue(en, out var ro) || ro.Length == 0) { wrong.Add($"no Romanian for: {en}"); continue; }
            // The same placeholders as the English, no more and no fewer
            var holes = Regex.Matches(en, @"\{[a-z]+\}").Select(m => m.Value).OrderBy(h => h).ToList();
            if (!holes.SequenceEqual(Regex.Matches(ro, @"\{[a-z]+\}").Select(m => m.Value).OrderBy(h => h)))
                wrong.Add($"placeholders differ: {ro}");
            if (ro.Contains('ş') || ro.Contains('ţ') || ro.Contains('ã')) wrong.Add($"cedilla or ã: {ro}");
            string shown = PlayerIdentity.Fill(ro, longest, PlayerLook.Boy, longest);
            if (TextMetrics.Undrawable(shown, UiWeight.ExtraBold).Any()) wrong.Add($"can't be drawn: {ro}");
            var rows = TextMetrics.Wrap(shown, ModernUi.MessageBoxWidth - ModernUi.MessageMargin, ModernUi.MessageSize, UiWeight.ExtraBold);
            if (rows.Count > 2) wrong.Add($"runs past the box's two lines: {ro}");
        }
        Assert.True(wrong.Count == 0, string.Join("\n", wrong));
    }

    [Fact]
    public void ATrainerSpeaksRomanianInBattleAndEnglishStaysAsItWas()
    {
        string firstDown = TrainerLines.For("leader_roark", TrainerCue.FirstDown)!.Text;
        List<string> Said()
        {
            var party = new Party();
            party.Add(Mon("Gible", 50, "Dragon Rage"));
            var roark = new Trainer { Id = "leader_roark", Name = "Roark", TrainerClass = "Leader", PrizeMoney = 1000 };
            roark.Party.Add(Mon("Bidoof", 5, "Growl"));
            roark.Party.Add(Mon("Bidoof", 30, "Growl"));
            var battle = new BattleEngine(new BattleSetup
            {
                PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(), Trainers = new List<Trainer> { roark },
                Random = CoreScenario.Calm(), Rules = Ruleset.Platinum, TrainerLines = true
            });
            Settle(battle);
            battle.SelectMove(0);
            var said = new List<string>();
            for (int i = 0; i < 400 && !battle.IsBattleOver; i++)
            {
                if (battle.HUD.MenuState is not (BattleMenuState.Message or BattleMenuState.LearnMove)) break;
                if (battle.IsWaitingForConfirm && battle.CurrentSpeaker == "Roark") said.Add(battle.CurrentMessage);
                battle.ConfirmMessage();
                battle.Update(1f / 60f);
            }
            return said;
        }
        Assert.Contains(firstDown, Said());
        try
        {
            Text.Use(Romanian);
            var said = Said();
            Assert.Contains(Text.DataTable(Romanian)[firstDown], said);
            Assert.DoesNotContain(firstDown, said);
        }
        finally { Text.Use(Text.English); }
        Assert.Contains(firstDown, Said());
    }

    [Fact]
    public void EveryRomanianEntryTakesTheArgumentsItsEnglishTakesAndRomaniansForms()
    {
        var english = Entries(Text.English);
        var romanian = Entries(Romanian);
        var wrong = new List<string>();
        foreach (var (key, entry) in romanian)
        {
            // A place's name is Romanian's own (English shows the area's file), and takes no argument
            if (!english.TryGetValue(key, out var en))
            {
                if (!key.StartsWith("places.", StringComparison.Ordinal) || Holes(entry).Count > 0) wrong.Add($"{key}: no English entry");
                continue;
            }
            var holes = Holes(entry);
            var allowed = Holes(en);
            if (OwnPlaceholders.TryGetValue(key, out var own)) allowed.UnionWith(own.Split(' '));
            foreach (var hole in holes.Where(h => !allowed.Contains(h))) wrong.Add($"{key}: {hole} is no argument of the English");
            foreach (var hole in Holes(en).Where(h => !holes.Contains(h) && !(own?.Contains(h) ?? false)))
                wrong.Add($"{key}: {hole} of the English is left out");

            if (en.ValueKind != entry.ValueKind) { wrong.Add($"{key}: a string in one table and forms in the other"); continue; }
            if (en.ValueKind != JsonValueKind.Object) continue;
            var forms = entry.EnumerateObject().Select(p => p.Name).ToHashSet();
            // An entry of numbers takes Romanian's three forms; one of the player's look the same two as English
            var needed = en.TryGetProperty("boy", out _) ? new[] { "boy", "girl" } : new[] { "one", "few", "other" };
            foreach (var form in needed.Where(f => !forms.Contains(f))) wrong.Add($"{key}: no form \"{form}\"");
        }
        Assert.True(wrong.Count == 0, string.Join("\n", wrong));
    }

    [Fact]
    public void EveryRomanianStringIsDrawnWithTheRightLetters()
    {
        var wrong = new List<string>();
        foreach (var (key, entry) in Entries(Romanian))
            foreach (string text in Strings(entry))
            {
                // Romanian's ș and ț are the letters with the comma below (U+0219, U+021B), never the Turkish cedilla's
                if (text.IndexOfAny(new[] { 'Ş', 'ş', 'Ţ', 'ţ' }) >= 0) wrong.Add($"{key}: a cedilla in \"{text}\"");
                // ă, â and î are Romanian's too: an a with a breve is never written as the tilde's ã
                if (text.Contains('ã') || text.Contains('Ã')) wrong.Add($"{key}: ã in \"{text}\"");
                foreach (var weight in new[] { UiWeight.Bold, UiWeight.ExtraBold, UiWeight.Black })
                    if (TextMetrics.Undrawable(text, weight).Count > 0) wrong.Add($"{key}: \"{text}\" has a character the {weight} face lacks");
            }
        Assert.True(wrong.Count == 0, string.Join("\n", wrong.Distinct()));
    }

    /// <summary>What stands in for a placeholder of a Romanian line when it is measured: long names, as English's test has.</summary>
    private static string StandIn(string placeholder) => placeholder switch
    {
        "player" or "rival" => "Barbara",
        "trainer" or "trainer2" => "Crescător Pokémon Lorenzo",
        "move" or "newMove" or "bindingMove" or "forgotten" => "Extreme Speed",
        "item" => "Lagging Tail",
        "ability" => "Compound Eyes",
        "team" => "echipei adverse",
        "0" => "echipei adverse",
        "1" => "Echipa adversă",
        "stat" => "Apărarea Sp.",
        "condition" => "înghețare",
        "flavour" => "picant",
        "what" => "Apărare și Ap. Sp.",
        "money" => "65535",
        "exp" => "99999",
        "level" => "100",
        "n" or "count" or "hits" or "magnitude" or "stockpile" or "taken" or "amount" => "20",
        "species" => "Kangaskhan",
        "pick" => "Electric",
        "some" => "Kangaskhan inamic",
        "last" => "Kangaskhan inamic",
        "class" => "Crescător Pokémon",
        "name" => "Lorenzo",
        "stage" => "a scăzut drastic!",
        _ => "Kangaskhan inamic"
    };

    private static string Measured(string key, long n)
    {
        string line = Text.Of(key, ("n", n));
        foreach (Match hole in Regex.Matches(line, @"\{([A-Za-z0-9]+)\}"))
            line = line.Replace(hole.Value, StandIn(hole.Groups[1].Value));
        return line;
    }

    [Fact]
    public void EveryRomanianBattleLineFitsTheMessageBoxsTwoLines()
    {
        try
        {
            Text.Use(Romanian);
            var tooLong = new List<string>();
            foreach (var key in Text.EnglishKeys.Where(k => k.StartsWith("battle.", StringComparison.Ordinal)).OrderBy(k => k))
                foreach (long n in new long[] { 1, 10, 20 })
                {
                    string line = Measured(key, n);
                    var rows = TextMetrics.Wrap(line, ModernUi.MessageBoxWidth - ModernUi.MessageMargin, ModernUi.MessageSize, UiWeight.ExtraBold);
                    if (rows.Count > 2) tooLong.Add($"{key}: {line}");
                }
            Assert.True(tooLong.Count == 0, "Lines that run past the box's two lines:\n" + string.Join("\n", tooLong.Distinct()));
        }
        finally
        {
            Text.Use(Text.English);
        }
    }

    /// <summary>The sections whose lines the field's text box writes: the runner's own, the field's rules' and the tunnels'.</summary>
    private static readonly string[] SaidInTheTextBox =
        { "found.", "fieldMoves.", "fishing.", "cycling.", "aids.", "teaching.", "tunnels.", "belowGround.hiker.", "scriptLines.noPokemonToBattle",
          "scriptLines.which", "scriptLines.knowsFour", "hatching.hatched", "itemUse.", "engine.vsSeeker.", "engine.radar.", "engine.no" };

    [Fact]
    public void EveryRomanianLineOfTheFieldFitsTheTextBox()
    {
        try
        {
            Text.Use(Romanian);
            var tooLong = new List<string>();
            foreach (var key in Text.EnglishKeys.Where(k => SaidInTheTextBox.Any(s => k.StartsWith(s, StringComparison.Ordinal))).OrderBy(k => k))
                foreach (long n in new long[] { 1, 10, 20 })
                {
                    string line = Measured(key, n);
                    if (DialogueBox.RowsOf(line, GameEngine.VirtualWidth).Count > DialogueBox.Rows) tooLong.Add($"{key}: {line}");
                }
            Assert.True(tooLong.Count == 0, "Lines that run out of the text box:\n" + string.Join("\n", tooLong.Distinct()));
        }
        finally
        {
            Text.Use(Text.English);
        }
    }

    [Fact]
    public void TheTitlesNoticesAndQuestionsFitTheirPanelsInRomanian()
    {
        // As TitleScreenTests and DiagnosticsTests hold English: a panel 1040 wide with 56 at each side, its heading
        // 48 Black on one line, a notice's body 30 ExtraBold on three lines, a question's on two
        const float width = 1040 - 112;
        var wrong = new List<string>();
        void Fits(string heading, string body, int rows)
        {
            if (TextMetrics.Measure(heading, 48, UiWeight.Black) > width) wrong.Add($"heading: {heading}");
            int count = TextMetrics.Wrap(body, width, 30, UiWeight.ExtraBold).Count;
            if (count > rows) wrong.Add($"{count} rows: {body}");
        }
        try
        {
            Text.Use(Romanian);
            foreach (int slot in new[] { 1, 2, 3 })
            {
                foreach (var load in new[] { SaveLoad.Recovered, SaveLoad.Damaged })
                    Fits(TitleNotice.ForSlot(slot, load)!.Heading, TitleNotice.ForSlot(slot, load)!.Body, 3);
                Fits(TitleNotice.CouldNotDelete(slot).Heading, TitleNotice.CouldNotDelete(slot).Body, 3);
                var longest = new SaveData { PlayerName = "WWWWWWWW", PlayTimeSeconds = 999.99f * 3600f };
                Fits(TitleScreen.DeleteHeading(slot), TitleScreen.DeleteBlurb(longest, "Team Galactic Eterna Building"), 2);
                Fits(TitleScreen.DeleteHeading(slot), TitleScreen.DeleteBlurb(null, null), 2);
                foreach (int saves in new[] { 1, 2 }) Fits(Text.Of("title.newGame.heading"), TitleScreen.NewGameBlurb(slot, saves), 2);
                if (TextMetrics.Measure(SaveScreen.Question(slot), 44, UiWeight.Black) > 1080 - 112) wrong.Add(SaveScreen.Question(slot));
            }
            Fits(TitleNotice.NoFreeSlot.Heading, TitleNotice.NoFreeSlot.Body, 3);
            foreach (var move in new[] { OldSaveMove.NoRoom, OldSaveMove.Failed })
                Fits(TitleNotice.ForOldSave(new OldSaveOutcome(move))!.Heading, TitleNotice.ForOldSave(new OldSaveOutcome(move))!.Body, 3);
            var note = new CrashNote("logs/crash-20261231-235959-12.txt", true, new DateTime(2026, 6, 1, 12, 0, 0));
            foreach (var load in Enum.GetValues<SaveLoad>())
                foreach (bool kept in new[] { true, false })
                {
                    var notice = TitleNotice.ForCrash(note with { SaveKept = kept }, load)!;
                    Fits(notice.Heading, notice.Body, 3);
                }
        }
        finally
        {
            Text.Use(Text.English);
        }
        Assert.True(wrong.Count == 0, string.Join("\n", wrong));
    }

    [Fact]
    public void ARomanianStatChangeIsOneSentenceWithItsArticle()
    {
        try
        {
            Text.Use(Romanian);
            Assert.Equal("Atacul lui Bidoof a crescut brusc!", Text.Of("battle.statChange",
                ("pokemon", "Bidoof"), ("stat", BattleText.StatName(StatType.Attack)), ("stage", BattleText.StageChange(2))));
            // The battle's information panel names the stat without it
            Assert.Equal("Atac", PokemonPlatinumEngine.Battle.BattleInfo.StageName(StatType.Attack));
            Assert.Equal("Precizie", PokemonPlatinumEngine.Battle.BattleInfo.StageName(StatType.Accuracy));
            // Romanian counts one, a few and twenty or more apart
            Assert.Equal("Lovit o dată!", Text.Of("battle.hitTimes", ("n", 1)));
            Assert.Equal("Lovit de 3 ori!", Text.Of("battle.hitTimes", ("n", 3)));
            Assert.Equal("Lovit de 20 de ori!", Text.Of("battle.hitTimes", ("n", 20)));
            Assert.Equal("1,5", Text.Decimal(1.5, 1));
        }
        finally
        {
            Text.Use(Text.English);
        }
        Assert.Equal("Bidoof's Attack sharply rose!", Text.Of("battle.statChange",
            ("pokemon", "Bidoof"), ("stat", BattleText.StatName(StatType.Attack)), ("stage", BattleText.StageChange(2))));
        Assert.Equal("Attack", PokemonPlatinumEngine.Battle.BattleInfo.StageName(StatType.Attack));
    }
}
