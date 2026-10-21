using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
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

    /// <summary>The named cast of the story's first half (plan 11 · C5): each a look of their own, under the name the world gives them.</summary>
    public static readonly string[] FirstHalfCast =
    {
        "roark", "poketch_president", "mars", "jupiter", "cheryl", "gardenia", "cynthia", "mira", "fantina", "bebe", "maylene", "crasher_wake"
    };

    [Fact]
    public void TheFirstHalfsNamedCastAreEachTheirOwnLook()
    {
        foreach (string look in FirstHalfCast)
        {
            Assert.True(Table.Has(look), $"{look} has no look");
            Assert.False(Table.Fallbacks.ContainsKey(look), $"{look} still falls back");
        }
        // Each is told from every other look by hair, hat and clothes (a skirt among them: the grunts of plan 11 · C2
        // wear one uniform, the women in a skirt)
        var seen = new Dictionary<(Color, HairCut, Headwear, Color, Color, Color, bool), string>();
        foreach (string look in Table.Names)
        {
            var s = Table.Get(look);
            var key = (s.HairColor, s.Hair, s.Hat, s.HatColor, s.Top, s.Bottom, s.Skirt);
            Assert.False(seen.TryGetValue(key, out var other), $"{look} looks just like {other}");
            seen[key] = look;
        }

        // The world's objects resolve to them by their own looks, and the people of the rooms by their type
        Assert.Equal("cheryl", WorldMapBuilder.CharacterFor("cheryl"));
        Assert.Equal("mira", WorldMapBuilder.CharacterFor("mira"));
        Assert.Equal("crasher_wake", WorldMapBuilder.CharacterFor("crasher_wake"));
        foreach (var (key, id, look) in new[] { ("eterna_forest", "cheryl", "cheryl"), ("wayward_cave_1f", "mira", "mira"), ("jubilife_city", "poketch_co_president", "poketch_president") })
        {
            var person = Sinnoh.Value.Overlay(key)!.People[id];
            string stands = person.NpcType ?? WorldMapBuilder.CharacterFor(Sinnoh.Value.Area(key)!.Objects.First(o => o.Id == id).Looks);
            Assert.Equal(look, stands);
        }
        foreach (var (map, name, look) in new[] { ("PastoriaGym", "Crasher Wake", "crasher_wake"), ("PoketchCompany", "President", "poketch_president") })
        {
            var file = JsonSerializer.Deserialize<MapFile>(File.ReadAllText(GameDataFiles.PathOf($"maps/{map}.json")), GameDataFiles.Json)!;
            Assert.Equal(look, file.Npcs.Single(n => n.Name == name).NpcType);
        }
    }

    /// <summary>
    /// The named cast of the story's second half and of the Battle Frontier (plan 11 · C6): each a look of their own,
    /// under the original's object name, named by the overlays and map files where they stand.
    /// </summary>
    public static readonly string[] SecondHalfCast =
    {
        "saturn", "riley", "byron", "candice", "volkner", "jasmine", "marley", "buck", "charon", "aaron", "bertha", "flint", "lucian",
        "palmer", "teala", "prof_oak", "dahlia", "argenta", "thorton", "darach", "caitlin"
    };

    [Fact]
    public void TheSecondHalfsNamedCastAreEachTheirOwnLook()
    {
        foreach (string look in SecondHalfCast)
        {
            Assert.True(Table.Has(look), $"{look} has no look");
            Assert.False(Table.Fallbacks.ContainsKey(look), $"{look} still falls back");
            Assert.Equal(look, WorldMapBuilder.CharacterFor(look));
        }
        // Each is told from every other look by hair, hat and clothes
        foreach (string look in SecondHalfCast)
        {
            var s = Table.Get(look);
            foreach (string other in Table.Names.Where(n => !n.Equals(look, StringComparison.OrdinalIgnoreCase)))
            {
                var o = Table.Get(other);
                Assert.False((s.HairColor, s.Hair, s.Hat, s.HatColor, s.Top, s.Bottom) == (o.HairColor, o.Hair, o.Hat, o.HatColor, o.Top, o.Bottom),
                    $"{look} looks just like {other}");
            }
        }

        // The world's people stand as their own looks: the overlays name no type for them, and their objects' looks are theirs
        foreach (var (key, id) in new[]
        {
            ("valor_cavern", "saturn"), ("iron_island", "riley"), ("iron_island_b2f_left_room", "riley"), ("resort_area", "byron"),
            ("snowpoint_city", "candice"), ("sunyshore_city", "flint"), ("sunyshore_city", "jasmine"), ("victory_road_1f_room_2", "marley"),
            ("fight_area", "buck"), ("fight_area", "flint"), ("fight_area", "volkner"), ("fight_area", "palmer"), ("route_227", "buck"),
            ("survival_area", "buck"), ("stark_mountain_outside", "buck"), ("stark_mountain_room_2", "buck"), ("stark_mountain_room_3", "buck"),
            ("stark_mountain_room_1", "charon"), ("stark_mountain_room_3", "charon")
        })
        {
            var person = Sinnoh.Value.Overlay(key)!.People![id];
            Assert.Null(person.NpcType);
            Assert.Equal(id, WorldMapBuilder.CharacterFor(Sinnoh.Value.Area(key)!.Objects.First(o => o.Id == id).Looks));
        }

        // The rooms' people by their type, under the ids the original's objects gave them
        foreach (var (map, id, look) in new[]
        {
            ("GalacticHQ1F", "saturn", "saturn"), ("GalacticHQControlRoom", "saturn", "saturn"), ("GalacticHQControlRoom", "charon", "charon"),
            ("CanalaveGym", "byron", "byron"), ("IronIslandHouse", "byron", "byron"), ("SnowpointGym", "candice", "candice"),
            ("SunyshoreGymRoom3", "volkner", "volkner"), ("VistaLighthouse", "volkner", "volkner"), ("CanalaveLibrary1F", "lucian", "lucian"),
            ("PokemonLeagueAaronRoom", "aaron", "aaron"), ("PokemonLeagueBerthaRoom", "bertha", "bertha"),
            ("PokemonLeagueFlintRoom", "flint", "flint"), ("PokemonLeagueLucianRoom", "lucian", "lucian"),
            ("RowanLab", "prof_oak", "prof_oak"), ("EternaCitySouthHouse", "prof_oak", "prof_oak"), ("PalParkLobby", "prof_oak", "prof_oak"),
            ("BattleHall", "visitor_professor", "prof_oak"), ("BattleHall", "visitor_jasmine", "jasmine"),
            ("BattleTower", "teala", "teala"), ("BattleTowerBattleSalon", "teala", "teala"), ("PokemonCenter2F", "teala_middle", "teala"),
            ("BattleTowerBattleSalon", "riley", "riley"), ("BattleTowerBattleSalon", "marley", "marley"), ("BattleTowerBattleSalon", "buck", "buck"),
            ("Battleground", "riley", "riley"), ("Battleground", "marley", "marley"), ("Battleground", "buck_guest", "buck"),
            ("Battleground", "byron", "byron"), ("Battleground", "candice", "candice"), ("Battleground", "volkner", "volkner"),
            ("ResortAreaVilla", "flint", "flint"), ("ResortAreaVilla", "crasher_wake", "crasher_wake"),
            ("BattleCastleBattleRoom", "lady", "caitlin")
        })
        {
            var file = JsonSerializer.Deserialize<MapFile>(File.ReadAllText(GameDataFiles.PathOf($"maps/{map}.json")), GameDataFiles.Json)!;
            Assert.Equal(look, file.Npcs.Single(n => n.Id == id).NpcType, ignoreCase: true);
        }
        // Every attendant upstairs and downstairs in a Pokémon Center is Teala
        foreach (string path in Directory.GetFiles(GameDataFiles.PathOf("maps"), "*PokemonCenter*.json"))
        {
            var file = JsonSerializer.Deserialize<MapFile>(File.ReadAllText(path), GameDataFiles.Json)!;
            Assert.All(file.Npcs.Where(n => n.Id?.StartsWith("teala", StringComparison.Ordinal) == true), n => Assert.Equal("teala", n.NpcType));
        }

        // The Frontier Brains walk into their battle rooms as themselves, and the Battle Salon's partners too
        foreach (var (brainClass, look) in new[] { ("Tower Tycoon", "palmer"), ("Factory Head", "thorton"), ("Castle Valet", "darach"), ("Hall Matron", "argenta"), ("Arcade Star", "dahlia") })
        {
            var brain = FrontierDatabase.Trainers.First(t => t.Class == brainClass);
            Assert.Equal(look, FrontierTrainers.LookOf(brain));
            Assert.Equal(look, Table.InBattle(brainClass, look));
        }
        Assert.All(BattleTowerChallenge.PartnerLooks, look => Assert.True(Table.Has(look), $"the partner {look} has no look"));
        Assert.Equal(new[] { "cheryl", "mira", "riley", "marley", "buck" }, FrontierDatabase.Trainers.Where(t => t.Class == "Pokémon Trainer").Select(FrontierTrainers.LookOf));
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
                // (a fallback counts too: the starter's briefcase is a thing, not a person, and the rival, the professor
                // and the nurse stand as their own looks by the names the game has always called them)
                if (!Table.Has(looks) && looks is not ("briefcase" or "barry" or "prof_rowan" or "pokecenter_nurse")) unmade.Add(looks);
            }
        }
        // Since plan 11 · C4 nobody of the open areas stands as a look they borrow
        Assert.Empty(unmade);
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
        // Plan 11 · C2 to C4 draw these: the count only falls (85 after plan 19 · K4 and plan 20 · J4 added the classes of Kanto and Johto, and K4's second part the Gamer and the Engineer of the sound map, which plan 11 · C13 and plan 20 · J14 will draw; 86 after plan 19 · K5 gave the Channeler a look and the sound map the Poké Maniac of FireRed's, "Pokemaniac"; 87 after K5's second part gave the Biker and the Cue Ball a look and the sound map FireRed's Rocker and Cooltrainer; 77 after plan 11 · C2 drew the first trainers of the open areas; 63 after C3 drew the rest; 41 after C4 drew the rest of Platinum's)
        Assert.True(plain.Count <= 41, $"{plain.Count} classes stand in the plain default: {string.Join(", ", plain)}");
        // What is left is the later regions' (their plans draw them): Platinum's classes all stand as looks of their own since C4
        Assert.Contains("Medium", plain);
        foreach (string platinum in TrainerDatabase.All.Select(t => t.Class).Distinct()) Assert.DoesNotContain(platinum, plain);
        // Plan 11 · C2 drew these
        Assert.DoesNotContain("Hiker", plain);
        Assert.DoesNotContain("Ace Trainer", plain);
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
        // ...a class plan 11 · C2 drew stands as its own look whatever the person wears...
        Assert.Equal("hiker", Table.InBattle("Hiker", "Gentleman"));
        Assert.Equal("grunt_f", Table.InBattle("Galactic", "grunt_f"));
        // ...and a class with no look yet stands in the plain default it always did, under its own name (Johto's Medium,
        // until plan 20 · J14 draws her; plan 11 · C4 drew the Sailor)
        Assert.Equal("sailor", Table.InBattle("Sailor", "Gentleman"));
        Assert.Equal("Medium", Table.InBattle("Medium", "Gentleman"));
        Assert.False(Table.Has("Medium"));
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
        for (int i = 0; i < 2400; i++)
        {
            if (done()) return true;
            System.Threading.Thread.Sleep(25);
        }
        return false;
    }

    /// <summary>The looks plan 11 · C2 made: the first trainers of the open areas, by the original's object names.</summary>
    public static readonly string[] FirstTrainers =
    {
        "school_kid_m", "school_kid_f", "bug_catcher", "camper", "picnicker", "twin", "kid_with_nds", "hiker", "worker", "grunt_m", "grunt_f",
        "cyclist_m", "cyclist_f", "battle_girl", "ace_trainer_m", "ace_trainer_f", "black_belt", "psychic", "ninja_boy", "ruin_maniac", "artist"
    };

    [Fact]
    public void TheFirstTrainersAreEachTheirOwnLook()
    {
        foreach (string look in FirstTrainers)
        {
            Assert.True(Table.Has(look), $"{look} has no look");
            Assert.False(Table.Fallbacks.ContainsKey(look), $"{look} still falls back");
            // A person of the world placed with this object look stands as it
            Assert.Equal(look, WorldMapBuilder.CharacterFor(look));
        }
        // Each class of theirs stands in battle as its own look, the woman's where there are two
        foreach (var (trainerClass, man, woman) in new[]
        {
            ("School Kid", "school_kid_m", "school_kid_f"), ("Cyclist", "cyclist_m", "cyclist_f"), ("Ace Trainer", "ace_trainer_m", "ace_trainer_f"),
            ("Galactic", "grunt", "grunt_f"), ("Hiker", "hiker", "hiker"), ("Worker", "worker", "worker"), ("Bug Catcher", "bug_catcher", "bug_catcher"),
            ("Camper", "camper", "camper"), ("Picnicker", "picnicker", "picnicker"), ("Battle Girl", "battle_girl", "battle_girl"),
            ("Psychic", "psychic", "psychic"), ("Ninja Boy", "ninja_boy", "ninja_boy"), ("Ruin Maniac", "ruin_maniac", "ruin_maniac"),
            ("Artist", "artist", "artist"), ("Black Belt", "blackbelt", "blackbelt")
        })
        {
            Assert.Equal(man, Table.InBattle(trainerClass, null));
            Assert.Equal(woman, Table.InBattle(trainerClass, woman));
        }
        // The new parts are worn: every cut, hat and the little build by someone
        var styles = FirstTrainers.Select(Table.Get).ToList();
        foreach (var cut in new[] { HairCut.Bowl, HairCut.Ponytail, HairCut.Curls, HairCut.Bald })
            Assert.Contains(styles, s => s.Hair == cut);
        foreach (var hat in new[] { Headwear.Helmet, Headwear.HardHat, Headwear.Beanie, Headwear.Bandana, Headwear.Headband })
            Assert.Contains(styles, s => s.Hat == hat);
        Assert.Contains(styles, s => s.Build == BodyBuild.Little);
        // The grunts carry the team's mark, and nobody else of them does
        Assert.Equal(new[] { "grunt_m", "grunt_f" }, FirstTrainers.Where(n => Table.Get(n).GalacticMark));
    }

    /// <summary>The mark on a grunt's chest is the mark's own yellow round its dark core, in the middle of the chest.</summary>
    [Fact]
    public void AGruntsUniformCarriesTheGalacticMark()
    {
        var rig = CharacterModels.Build("GRUNT_F");
        var mesh = rig.Mesh;
        static bool Near(Color a, Color b) => Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B) < 24;
        var yellow = Enumerable.Range(0, mesh.VertexCount).Where(v => Near(mesh.Colors[v], GalacticMark.Base)).Select(v => mesh.Positions[v]).ToList();
        Assert.True(yellow.Count > 20, $"only {yellow.Count} vertices of the mark");
        Assert.All(yellow, p => Assert.True(MathF.Abs(p.X) < 0.06f && p.Z > 0.05f, $"a yellow vertex at {p} is off the chest's middle"));
        Assert.Contains(Enumerable.Range(0, mesh.VertexCount), v => Near(mesh.Colors[v], GalacticMark.Ink));
        // A look without it has none
        var plain = CharacterModels.Build("LASS").Mesh;
        Assert.DoesNotContain(Enumerable.Range(0, plain.VertexCount), v => Near(plain.Colors[v], GalacticMark.Base));
    }

    /// <summary>The looks plan 11 · C3 made: the rest of the open areas' people, and three classes drawn only in battle.</summary>
    public static readonly string[] SecondTrainers =
    {
        "beauty", "collector", "expert_m", "expert_f", "middle_aged_man", "middle_aged_woman", "old_man", "old_woman", "pokefan_m", "pokefan_f",
        "pokemon_breeder_m", "pokemon_breeder_f", "tuber_m", "tuber_f", "parasol_lady", "fisherman", "swimmer_m", "swimmer_f", "scientist_m",
        "scientist_f", "guitarist", "cowgirl", "rancher", "receptionist", "cashier_m", "aroma_lady", "veteran", "poke_kid"
    };

    [Fact]
    public void TheSecondTrainersAreEachTheirOwnLook()
    {
        foreach (string look in SecondTrainers)
        {
            Assert.True(Table.Has(look), $"{look} has no look");
            Assert.False(Table.Fallbacks.ContainsKey(look), $"{look} still falls back");
            Assert.Equal(look, WorldMapBuilder.CharacterFor(look));
        }
        foreach (var (trainerClass, man, woman) in new[]
        {
            ("Pokéfan", "pokefan_m", "pokefan_f"), ("Pokémon Breeder", "pokemon_breeder_m", "pokemon_breeder_f"), ("Tuber", "tuber_m", "tuber_f"),
            ("Swimmer", "swimmer_m", "swimmer_f"), ("Scientist", "scientist", "scientist_f"), ("Beauty", "beauty", "beauty"),
            ("Collector", "collector", "collector"), ("Parasol Lady", "parasol_lady", "parasol_lady"), ("Fisherman", "fisherman", "fisherman"),
            ("Guitarist", "guitarist", "guitarist"), ("Cowgirl", "cowgirl", "cowgirl"), ("Rancher", "rancher", "rancher"),
            ("Aroma Lady", "aroma_lady", "aroma_lady"), ("Veteran", "veteran", "veteran"), ("Poké Kid", "poke_kid", "poke_kid")
        })
        {
            Assert.Equal(man, Table.InBattle(trainerClass, null));
            Assert.Equal(woman, Table.InBattle(trainerClass, woman));
        }
        // Every new part is worn by someone
        var styles = SecondTrainers.Select(Table.Get).ToList();
        Assert.Contains(styles, s => s.Overalls);
        Assert.Contains(styles, s => s.Robe);
        Assert.Contains(styles, s => s.Swimsuit);
        Assert.Contains(styles, s => s.Apron != null);
        Assert.Contains(styles, s => s.Vest != null);
        Assert.Contains(styles, s => s.Hat == Headwear.Cowboy);
        Assert.Contains(styles, s => s.Build == BodyBuild.Elder);
        foreach (var held in Enum.GetValues<HeldProp>().Where(h => h is not (HeldProp.None or HeldProp.Net or HeldProp.Pickaxe or HeldProp.Camera)))
            Assert.Contains(styles, s => s.Holds == held);
        // The net and the pickaxe went to two of C2's looks
        Assert.Equal(HeldProp.Net, Table.Get("bug_catcher").Holds);
        Assert.Equal(HeldProp.Pickaxe, Table.Get("ruin_maniac").Holds);
    }

    /// <summary>What a person holds is part of their body and moves with its bone: a rod with the right hand, a float ring with the hips.</summary>
    [Theory]
    [InlineData("FISHERMAN", "Rod")]
    [InlineData("TUBER_M", "FloatRing")]
    [InlineData("BUG_CATCHER", "Net")]
    public void WhatIsHeldMovesWithItsBone(string type, string holds)
    {
        var rig = CharacterModels.Build(type);
        var held = Enum.Parse<HeldProp>(holds);
        Assert.Equal(held, rig.Style.Holds);
        var mesh = rig.Mesh;
        int bone = held == HeldProp.FloatRing ? HumanBones.Hips : HumanBones.HandR;
        // Somewhere well out from the body, the surface follows that bone alone
        var far = Enumerable.Range(0, mesh.VertexCount)
            .Where(v => held == HeldProp.FloatRing
                // the ring before and behind the body, clear of the arms
                ? MathF.Abs(mesh.Positions[v].X) < 0.08f && MathF.Abs(mesh.Positions[v].Z) > 0.17f && mesh.Positions[v].Y < 0.5f
                // out ahead of the body below the face, on the right hand's side
                : mesh.Positions[v].Z > 0.3f && mesh.Positions[v].Y < 0.75f && mesh.Positions[v].X < 0f)
            .ToList();
        Assert.True(far.Count > 30, $"{type}: only {far.Count} vertices of what is held");
        Assert.All(far, v => Assert.True(mesh.WeightOf(v, bone) > 0.9f, $"{type}: a vertex at {mesh.Positions[v]} follows its bone {mesh.WeightOf(v, bone):F2} of the way"));
    }

    /// <summary>
    /// Every person among the original's object looks (its <c>OBJ_EVENT_GFX_*</c>, from the decompilation's
    /// <c>generated/object_events_gfx.txt</c>, written here by plan 11 · C4 since the importer's full run is not in the
    /// repository): the player's own variants, the unused and dummy entries, the <c>var_*</c> stand-ins, the Pokémon and
    /// the things are left out.
    /// </summary>
    public static readonly string[] OriginalsPeople =
    {
        "ninja_boy", "twin", "school_kid_m", "youngster", "bug_catcher", "lass", "battle_girl", "school_kid_f", "pokemon_breeder_m", "guitarist",
        "ace_trainer_m", "pokemon_breeder_f", "beauty", "ace_trainer_f", "pokefan_m", "pokefan_f", "expert_m", "expert_f", "collector", "hiker",
        "reporter", "cameraman", "cashier_m", "cashier_f", "pokecenter_nurse", "teala", "scientist_m", "scientist_f", "roughneck", "skier_m",
        "skier_f", "policeman", "idol", "gentleman", "socialite", "cyclist_m", "cyclist_f", "worker", "rancher", "cowgirl", "clown", "artist",
        "jogger", "swimmer_m", "swimmer_f", "tuber_f", "tuber_m", "ruin_maniac", "black_belt", "camper", "picnicker", "fisherman", "parasol_lady",
        "sailor", "waiter", "waitress", "rich_boy", "lady", "snowpoint_npc_m", "snowpoint_npc_f", "ace_trainer_snow_m", "ace_trainer_snow_f",
        "psychic", "baby_in_pram", "middle_aged_man", "middle_aged_woman", "prof_rowan", "cyrus", "mars", "saturn", "jupiter", "grunt_m", "grunt_f",
        "roark", "gardenia", "crasher_wake", "maylene", "fantina", "candice", "byron", "volkner", "aaron", "bertha", "flint", "lucian", "cynthia",
        "mom", "cheryl", "riley", "marley", "buck", "mira", "barry", "receptionist", "old_man", "old_woman", "prof_oak", "jasmine", "gym_guide",
        "palmer", "maid", "mystery_gift_deliveryman", "kid_with_nds", "looker", "charon", "thorton", "argenta", "darach", "dahlia", "caitlin",
        "frontier_single_attendant", "frontier_multi_attendant", "frontier_booth_attendant", "wifi_plaza_attendant_m", "wifi_plaza_attendant_f",
        "game_director"
    };

    [Fact]
    public void EveryPersonOfTheOriginalIsALookOfTheTable()
    {
        // None stands as another's look any more (plan 11 · C4): each is a look of the table or another name of one,
        // but the named cast whose objects the game has always called by the look's own name (the rival, the
        // professor, the nurse: what scripts and tests know them as)
        var named = new Dictionary<string, string> { ["barry"] = "Rival", ["prof_rowan"] = "Rowan", ["pokecenter_nurse"] = "Nurse" };
        foreach (string look in OriginalsPeople)
        {
            if (named.TryGetValue(look, out var whose))
            {
                Assert.Equal(whose, WorldMapBuilder.CharacterFor(look));
                continue;
            }
            Assert.True(Table.Has(look), $"{look} has no look");
            Assert.False(Table.Fallbacks.ContainsKey(look), $"{look} still falls back");
            Assert.Equal(look, WorldMapBuilder.CharacterFor(look));
        }
        // What else is left in the fallbacks is no person of the original's list: its stand-in and the briefcase, and
        // the GBA's and HeartGold's looks their plans give their own
        Assert.DoesNotContain(Table.Fallbacks.Keys, k => OriginalsPeople.Contains(k) && !named.ContainsKey(k));
    }

    [Fact]
    public void TheLastClassesStandAsTheirOwnLooks()
    {
        foreach (var (trainerClass, first, second) in new[]
        {
            ("Sailor", "sailor", "sailor"), ("Skier", "skier_m", "skier_f"), ("Policeman", "policeman", "policeman"), ("Roughneck", "roughneck", "roughneck"),
            ("Idol", "idol", "idol"), ("Socialite", "socialite", "socialite"), ("Rich Boy", "rich_boy", "rich_boy"), ("Waiter", "waiter", "waiter"),
            ("Waitress", "waitress", "waitress"), ("Maid", "maid", "maid"), ("Reporter", "reporter", "reporter"), ("Cameraman", "cameraman", "cameraman"),
            ("Jogger", "jogger", "jogger"), ("Dragon Tamer", "dragon_tamer", "dragon_tamer"), ("Pokémon Ranger", "ranger_m", "ranger_f"), ("PI", "pi", "pi"),
            ("Interviewers", "reporter", "cameraman"), ("Sis and Bro", "swimmer_f", "swimmer_m"), ("Belle & Pa", "lass", "middle_aged_man"),
            ("Double Team", "ace_trainer_f", "ace_trainer_m"), ("Young Couple", "ace_trainer_m", "ace_trainer_f")
        })
        {
            Assert.Equal(first, Table.InBattle(trainerClass, null));
            Assert.Equal(second, Table.InBattle(trainerClass, second));
        }
        // C4's parts are worn
        Assert.Equal(Headwear.Visor, Table.Get("jogger").Hat);
        Assert.Equal(Headwear.WideHat, Table.Get("socialite").Hat);
        Assert.Equal(HeldProp.Camera, Table.Get("cameraman").Holds);
    }
}
