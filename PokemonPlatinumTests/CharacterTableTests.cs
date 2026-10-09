using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using Raylib_cs;

namespace PokemonPlatinumTests;

/// <summary>The table of looks (plan 11 · C1): one name for a person in the field, in battle and in the files.</summary>
public class CharacterTableTests
{
    private static readonly Lazy<World> Sinnoh = new(() => World.LoadAll().Single(w => w.Index.Region == "Sinnoh"));

    private static CharacterStyles Table => CharacterStyles.Default;

    /// <summary>The boy and the girl are what every game is drawn as: moving them into the file must not move a texel of either.</summary>
    [Fact]
    public void ThePlayersLooksAreWhatTheyWere()
    {
        var boy = CharacterStyle.For("PLAYER");
        Assert.Equal(new Color(252, 218, 184, 255), boy.Skin);
        Assert.Equal(new Color(70, 56, 78, 255), boy.HairColor);
        Assert.Equal(HairCut.Short, boy.Hair);
        Assert.Equal(Headwear.Beret, boy.Hat);
        Assert.Equal(new Color(220, 56, 60, 255), boy.HatColor);
        Assert.Equal(new Color(246, 246, 250, 255), boy.HatBand);
        Assert.Equal(new Color(58, 78, 138, 255), boy.Top);
        Assert.Equal(new Color(236, 70, 70, 255), boy.Accent);
        Assert.Equal(new Color(48, 50, 70, 255), boy.Bottom);
        Assert.Equal(new Color(200, 70, 60, 255), boy.Shoes);
        Assert.Equal(new Color(40, 36, 60, 255), boy.Eyes);
        Assert.Equal(new Color(240, 236, 228, 255), boy.Sole);
        Assert.Equal(new Color(242, 196, 70, 255), boy.Bag);
        Assert.Equal((false, false, false, false, false, false), (boy.Skirt, boy.Shorts, boy.Mustache, boy.Stripes, boy.Coat, boy.ShortSleeves));
        Assert.Equal((false, false, true, true), (boy.BushyBrows, boy.Lashes, boy.Blush, boy.Scarf));
        Assert.Equal((BodyBuild.Kid, 1f), (boy.Build, boy.Height));

        var girl = CharacterStyle.For("DAWN");
        Assert.Equal(new Color(252, 218, 184, 255), girl.Skin);
        Assert.Equal(new Color(54, 62, 104, 255), girl.HairColor);
        Assert.Equal(HairCut.Long, girl.Hair);
        Assert.Equal(Headwear.Beret, girl.Hat);
        Assert.Equal(new Color(248, 248, 252, 255), girl.HatColor);
        Assert.Equal(new Color(238, 124, 156, 255), girl.HatBand);
        Assert.Equal(new Color(226, 84, 118, 255), girl.Top);
        Assert.Equal(new Color(248, 248, 252, 255), girl.Accent);
        Assert.Equal(new Color(226, 84, 118, 255), girl.Bottom);
        Assert.Equal(new Color(238, 124, 156, 255), girl.Shoes);
        Assert.Equal(new Color(70, 86, 140, 255), girl.Eyes);
        Assert.Equal(new Color(244, 240, 236, 255), girl.Sole);
        Assert.Equal(new Color(250, 246, 236, 255), girl.Bag);
        Assert.Equal((true, false, false, false, true, false), (girl.Skirt, girl.Shorts, girl.Mustache, girl.Stripes, girl.Coat, girl.ShortSleeves));
        Assert.Equal((false, true, true, true), (girl.BushyBrows, girl.Lashes, girl.Blush, girl.Scarf));
        Assert.Equal((BodyBuild.Kid, 1f), (girl.Build, girl.Height));

        // The names that are the boy's, and a name nobody has: the plain default
        Assert.Equal(boy.Top, CharacterStyle.For("TRAINER").Top);
        Assert.Equal(boy.Top, CharacterStyle.For("Lucas").Top);
        Assert.Equal(new CharacterStyle().Top, CharacterStyle.For("someone_nobody_drew").Top);
    }

    [Fact]
    public void EveryLookIsReadAndNoNameIsTwoLooks()
    {
        Assert.True(Table.Names.Count() >= 25, "the looks of plan 02's chapters so far");
        foreach (string look in new[] { "player", "dawn", "rival", "rowan", "nurse", "cyrus", "roark", "grunt", "cynthia", "gentleman" })
            Assert.True(Table.Has(look), $"{look} is in the table");
        // A name matches without regard to case: a map's "Lass" is the file's "lass"
        Assert.True(Table.Has("Lass") && Table.Has("LASS"));
        Assert.Equal(Table.Names.Count(), Table.Names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        foreach (var (alias, of) in Table.Aliases)
        {
            Assert.DoesNotContain(alias, Table.Names, StringComparer.OrdinalIgnoreCase);
            Assert.Contains(of, Table.Names, StringComparer.OrdinalIgnoreCase);
        }
        // A fallback stands as a look of the table (or the starter's briefcase, which is a thing, not a person), and
        // never folds away an object look that has its own
        foreach (var (looks, stands) in Table.Fallbacks)
        {
            Assert.False(Table.Has(looks), $"{looks} is a look of its own, and the fallback would never be read");
            Assert.True(Table.Has(stands) || stands == "StarterBriefcase", $"{looks} stands as '{stands}', which is no look");
        }
        // A copy is handed out: changing it changes no one else
        Table.Get("lass").Top = Color.Black;
        Assert.NotEqual(Color.Black, Table.Get("lass").Top);
    }

    [Fact]
    public void AFieldOfALookTheKitDoesntHaveIsAnErrorNotAPlainLook()
    {
        Assert.ThrowsAny<JsonException>(() => CharacterStyles.Parse("""{ "looks": { "hiker": { "hairColour": "#402010" } } }"""));
        Assert.ThrowsAny<JsonException>(() => CharacterStyles.Parse("""{ "looks": { "hiker": { "top": "brown" } } }"""));
        Assert.ThrowsAny<JsonException>(() => CharacterStyles.Parse("""{ "looks": {}, "same": { "lucas": "nobody" } }"""));
        var table = CharacterStyles.Parse("""{ "looks": { "hiker": { "top": "#804020", "hair": "Spiky", "build": "Adult", "height": 1.1 } } }""");
        var hiker = table.Get("Hiker");
        Assert.Equal((new Color(128, 64, 32, 255), HairCut.Spiky, BodyBuild.Adult, 1.1f), (hiker.Top, hiker.Hair, hiker.Build, hiker.Height));
    }

    /// <summary>
    /// Every person of the open areas resolves through the table: as a look of their own, or as the look their
    /// object stands as for now. Those who stand as the boy because nobody has drawn them yet are counted, and plan 11
    /// · C2 to C4 take them off this list as they draw them.
    /// </summary>
    [Fact]
    public void EveryPersonOfTheOpenAreasResolves()
    {
        var unmade = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string key in Sinnoh.Value.Index.Areas)
        {
            var file = Sinnoh.Value.Area(key)!;
            var overlay = Sinnoh.Value.Overlay(key);
            foreach (var (id, person) in overlay?.People ?? new())
            {
                if (person.NpcType != null) continue;
                string looks = file.Objects.First(o => o.Id == id).Looks;
                if (person.Trainer == null && WorldMapBuilder.SpeciesFor(looks) != null) continue;
                string stands = WorldMapBuilder.CharacterFor(looks);
                Assert.True(Table.Has(stands) || stands == "StarterBriefcase", $"{key}: {id} ({looks}) stands as '{stands}', which is no look");
                if (!Table.Has(looks) && !Table.Fallbacks.ContainsKey(looks)) unmade.Add(looks);
            }
        }
        // The sea's, the snow's and the east's people (plan 01 · M7 and M8): plan 11 · C3 and C4's
        Assert.Equal(new[]
        {
            "ace_trainer_snow_f", "ace_trainer_snow_m", "policeman", "roughneck", "sailor", "skier_f", "skier_m",
            "snowpoint_npc_f", "snowpoint_npc_m", "swimmer_f", "swimmer_m", "tuber_f", "tuber_m"
        }, unmade);
    }

    /// <summary>
    /// Every trainer class of the data and of the sound map stands on the battle's platform as something: a look of
    /// the table, the person's own (a class that names nobody in particular), or, until plan 11 · C2 to C4 draw it,
    /// the plain default it always stood in.
    /// </summary>
    [Fact]
    public void EveryClassStandsInBattleAsSomething()
    {
        var classes = TrainerDatabase.All.Select(t => t.Class).ToHashSet();
        using (var sound = JsonDocument.Parse(File.ReadAllText(GameDataFiles.PathOf("audio/sound-map.json"))))
            foreach (string block in new[] { "eyeThemes", "battleThemes" })
                foreach (var entry in sound.RootElement.GetProperty(block).EnumerateObject()) classes.Add(entry.Name);

        foreach (var (name, looks) in Table.Classes)
            foreach (string look in looks ?? Array.Empty<string>())
                Assert.True(Table.Has(look), $"the class {name} is drawn as '{look}', which is no look");

        var plain = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string trainerClass in classes)
        {
            string? look = Table.OfClass(trainerClass, null);
            if (look == null) Assert.Equal("Roark", Table.InBattle(trainerClass, "Roark"));
            else if (!Table.Has(look)) plain.Add(trainerClass);
        }
        // Plan 11 · C2 to C4 draw these: the count only falls
        Assert.True(plain.Count <= 56, $"{plain.Count} classes stand in the plain default: {string.Join(", ", plain)}");
        Assert.Contains("Hiker", plain);
        Assert.DoesNotContain("Lass", plain);
    }

    [Fact]
    public void AClassIsDrawnAsItselfAndTheLeaderAsThePerson()
    {
        // The game's own table: the class wins over the look the person borrows in the field...
        Assert.Equal("lass", Table.InBattle("Lass", "Lady"));
        Assert.Equal("grunt", Table.InBattle("Galactic", "Grunt"));
        // ...a Leader, a Commander and the Champion are who they are...
        Assert.Equal("Roark", Table.InBattle("Leader", "Roark"));
        Assert.Equal("Mars", Table.InBattle("Commander", "Mars"));
        Assert.Equal("Cynthia", Table.InBattle("Champion", "Cynthia"));
        // ...and a class with no look yet stands in the plain default it always did: a Hiker stands as a Gentleman in
        // the field until plan 11 · C2 draws him
        Assert.Equal("Hiker", Table.InBattle("Hiker", "Gentleman"));
        Assert.False(Table.Has("Hiker"));
        // A trainer nobody carries (a test's, the harness's) stands as the class
        Assert.Equal("Leader", Table.InBattle("Leader", null));

        // A class of two looks: the person's own tells which, and the first stands for anyone else
        var table = CharacterStyles.Parse("""
            { "looks": { "ace_trainer_m": {}, "ace_trainer_f": {} },
              "classes": { "Ace Trainer": [ "ace_trainer_m", "ace_trainer_f" ], "Leader": null } }
            """);
        Assert.Equal("ace_trainer_f", table.OfClass("Ace Trainer", "Ace_Trainer_F"));
        Assert.Equal("ace_trainer_m", table.OfClass("Ace Trainer", "ace_trainer_m"));
        Assert.Equal("ace_trainer_m", table.OfClass("Ace Trainer", "lass"));
        Assert.Null(table.OfClass("Leader", "roark"));
    }

    /// <summary>The look of the person carrying a trainer reaches the battle's platform, through the copy the rules are given.</summary>
    [Fact]
    public void ALeadersBattleStandsThePersonOnThePlatform()
    {
        var leader = new Trainer { Name = "Roark", TrainerClass = "Leader", Look = "Roark" };
        leader.Party.Add(new Pokemon(PokemonDatabase.Get("Geodude")!, 12));
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 14));
        var battle = new BattleEngine(party, leader.Party.Members[0], new Inventory(), new Pokedex(), leader);
        Assert.Equal("Roark", battle.Anim.EnemyTrainer);
        Assert.Equal("Roark", new BattleMirror().Copy(leader).Look);
    }

    [Fact]
    public void TrimKeepsWhatItIsToldToKeep()
    {
        CharacterModels.Preload(new[] { "StarterBriefcase", "Rift" });
        // A build under way is left to finish, so the trim waits for both
        Assert.True(SpinWait(() => CharacterModels.Built("StarterBriefcase") && CharacterModels.Built("Rift")));
        CharacterModels.Trim(new[] { "rift" });
        Assert.Contains("Rift", CharacterModels.Held, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("StarterBriefcase", CharacterModels.Held, StringComparer.OrdinalIgnoreCase);
    }

    private static bool SpinWait(Func<bool> done)
    {
        for (int i = 0; i < 400; i++)
        {
            if (done()) return true;
            System.Threading.Thread.Sleep(25);
        }
        return false;
    }
}
