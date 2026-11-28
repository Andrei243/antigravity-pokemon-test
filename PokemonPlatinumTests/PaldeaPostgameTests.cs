using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Paldea after its Hall of Fame (plan 34 · PD9; docs/mechanics/rulings.md, "After Paldea's Hall of Fame"): the black
/// crystals of six and seven stars, the gym leaders' rematches, the Academy Ace Tournament on the academy's court, the
/// Treasures of Ruin behind their stakes, the partners' Mega Stones and the auction's oddities lot (plan 40 · GE11), and
/// the species of our own in the open map's zones (plan 03 · D15).
/// </summary>
public class PaldeaPostgameTests
{
    private static Region Region => RegionDatabase.Get(RegionDatabase.Paldea)!;
    private static readonly Lazy<OpenMap> Open = new(() => OpenRegions.Load(Region).Single());
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);

    private sealed class Game
    {
        public readonly StoryState Story = new();
        public readonly Party Party = new();
        public readonly Inventory Bag = new();

        public Game(string starter = "Fuecoco")
        {
            StoryMigration.BeginNewGame(Story, ScriptLibrary.Default);
            Party.Add(new Pokemon(PokemonDatabase.Get("Skeledirge")!, 80, new Random(0)));
            Story.ChooseStarter(starter);
            Story.SetVar("VAR_PALDEA_STORY", 7);
            Story.SetVar("VAR_PALDEA_VERSION", 1);
        }

        public void WayHome()
        {
            Story.Set("PaldeaHallOfFame");
            Story.Set("FLAG_PALDEA_WAY_HOME");
        }

        public HeadlessScriptHost Play(string file, string script, NPC? self = null, Func<NPC?, BattleOutcome>? fight = null, int[]? rolls = null, int[]? answers = null)
        {
            bool room = char.IsUpper(file[0]);
            var host = new HeadlessScriptHost(Story, Party, Bag)
            {
                Map = room ? Rooms.Value(file) : null,
                OpenMap = room ? null : Open.Value,
                OpenMapNamed = name => name == Open.Value.Name ? Open.Value : null,
                Region = RegionDatabase.Paldea,
                Fight = fight
            };
            foreach (int r in rolls ?? Array.Empty<int>()) host.Rolls.Enqueue(r);
            foreach (int a in answers ?? Array.Empty<int>()) host.Answers.Enqueue(a);
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find(script, file)!, self);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            return host;
        }
    }

    // ------------------------------------------------------------------ the black crystals

    [Fact]
    public void TheBlackCrystalsStandAfterTheHallOfFameAndNothingBeforeItMoves()
    {
        Assert.Equal(5, Raids.TeraMostStars(8, champion: false, aceTitle: false));
        Assert.Equal(6, Raids.TeraMostStars(8, champion: true, aceTitle: false));
        Assert.Equal(7, Raids.TeraMostStars(8, champion: true, aceTitle: true));
        foreach (var crystal in Raids.Paldea.Dens)
        {
            // Every crystal has its black rows, at the end of its table
            Assert.Contains(crystal.Rows, r => r.Stars == 6);
            Assert.Contains(crystal.Rows, r => r.Stars == 7);
            // Before the Hall of Fame each day's crystal is what PD7 drew
            for (uint day = 0; day < 60; day++)
                for (int badges = 0; badges <= 8; badges += 4)
                    Assert.Equal(Raids.CrystalToday(crystal, day, 1, badges), Raids.CrystalTodayUpTo(crystal, day, 1, Raids.TeraMostStars(badges)));
        }
        var all = Raids.Paldea.Dens.SelectMany(c => Enumerable.Range(0, 400).Select(d => Raids.CrystalTodayUpTo(c, (uint)d, 1, 7))).ToList();
        Assert.Contains(all, t => t.Stars == 6);
        Assert.Contains(all, t => t.Stars == 7);
        Assert.All(all.Where(t => t.Stars >= 6), t => Assert.InRange(t.Level, 80, 92));
        Assert.True(PokemonPlatinumEngine.Graphics.OpenWorld.DenBeams.IsBlack(6));
        Assert.False(PokemonPlatinumEngine.Graphics.OpenWorld.DenBeams.IsBlack(5));
        // The story's answer
        var game = new Game();
        game.Story.GiveBadge(RegionDatabase.Paldea, "bug");
        Assert.Equal(Raids.TeraMostStars(1), Raids.TeraMostStarsOf(game.Story));
        game.Story.Set("PaldeaHallOfFame");
        Assert.Equal(6, Raids.TeraMostStarsOf(game.Story));
        game.Story.Set(Raids.AceTitleFlag);
        Assert.Equal(7, Raids.TeraMostStarsOf(game.Story));
    }

    // ------------------------------------------------------------------ the leaders' rematches

    public static readonly TheoryData<string, string, string, string> Leaders = new()
    {
        { "Cortondo_GymTest", "katy", "bug", "KATY" },
        { "Artazon_GymTest", "brassius", "grass", "BRASSIUS" },
        { "Levincia_GymTest", "iono", "electric", "IONO" },
        { "Cascarrafa_GymTest", "kofu", "water", "KOFU" },
        { "Medali_GymTest", "larry", "normal", "LARRY" },
        { "Montenevera_GymTest", "ryme", "ghost", "RYME" },
        { "Alfornada_GymTest", "tulip", "psychic", "TULIP" },
        { "Glaseado_GymTest", "grusha", "ice", "GRUSHA" },
    };

    [Theory]
    [MemberData(nameof(Leaders))]
    public void EachLeaderAsksForARematchOnceADayAfterTheHallOfFame(string room, string who, string badge, string flagName)
    {
        var game = new Game();
        game.Story.GiveBadge(RegionDatabase.Paldea, badge);
        // Before the Hall of Fame: only talk
        var before = game.Play(room, "Leader");
        Assert.DoesNotContain(before.Log, l => l.StartsWith("battle "));
        game.Story.Set("PaldeaHallOfFame");
        var host = game.Play(room, "Leader");
        Assert.Contains(host.Log, l => l.StartsWith($"battle paldea_{who}_rematch"));
        Assert.True(game.Story.Has($"FLAG_DAILY_PALDEA_{flagName}_REMATCH"));
        // Again the same day: only talk
        var again = game.Play(room, "Leader");
        Assert.DoesNotContain(again.Log, l => l.StartsWith("battle "));
        var team = TrainerDatabase.Get($"paldea_{who}_rematch", RegionDatabase.Paldea)!;
        Assert.Equal(70, team.Party.Max(p => p.Level));
        Assert.True(TrainerDatabase.IsRematchTeam(team.Id));
    }

    // ------------------------------------------------------------------ the Academy Ace Tournament

    private static NPC Standing(Map court) => Assert.Single(court.NPCs, n => n.Id.StartsWith("ace_") && n.Id != "ace_referee");

    private static HeadlessScriptHost Arrive(Game game) => game.Play("Mesagoza_AceCourt", "OnEnter");

    [Theory]
    [InlineData(0, 0, 0, "Fuecoco", "paldea_katy_rematch", "paldea_rika_rematch", "paldea_geeta_rematch")]
    [InlineData(7, 3, 1, "Sprigatito", "paldea_grusha_rematch", "paldea_hassel_rematch", "paldea_nemona_8a_rematch")]
    [InlineData(4, 2, 1, "Quaxly", "paldea_larry_rematch", "paldea_larry_elite_rematch", "paldea_nemona_8c_rematch")]
    public void TheAceTournamentIsWonInThreeRoundsAndItsTitleLetsTheSevenStarCrystalsStand(int first, int second, int final, string starter, string one, string two, string three)
    {
        var game = new Game(starter);
        // Closed before the Way Home
        var closed = game.Play("Mesagoza_AcademyHall", "AceDesk");
        Assert.DoesNotContain(closed.Log, l => l.StartsWith("warp "));
        game.WayHome();
        var desk = game.Play("Mesagoza_AcademyHall", "AceDesk", rolls: new[] { first, second, final });
        Assert.Contains(desk.Log, l => l.StartsWith("warp Mesagoza_AceCourt"));
        Assert.Equal(1, game.Story.Var("VAR_PALDEA_ACE"));
        var court = Rooms.Value("Mesagoza_AceCourt");
        foreach (string team in new[] { one, two, three })
        {
            var host = new HeadlessScriptHost(game.Story, game.Party, game.Bag) { Map = court, Region = RegionDatabase.Paldea };
            var arrive = new ScriptRunner(ScriptLibrary.Default, host);
            arrive.Start(ScriptLibrary.Default.Find("OnEnter", "Mesagoza_AceCourt")!, null);
            arrive.RunToEnd();
            court.ApplyPresence(game.Story.Has);
            var self = Standing(court);
            var match = game.Play("Mesagoza_AceCourt", "Match", self);
            Assert.Contains(match.Log, l => l.StartsWith($"battle {team}"));
        }
        Assert.Equal(0, game.Story.Var("VAR_PALDEA_ACE"));
        Assert.Equal(1, game.Story.Var("VAR_PALDEA_ACE_TITLES"));
        Assert.True(game.Story.Has(Raids.AceTitleFlag));
        Assert.Equal(7, Raids.TeraMostStarsOf(game.Story));
    }

    [Fact]
    public void ALossEndsTheTournament()
    {
        var game = new Game();
        game.WayHome();
        game.Play("Mesagoza_AcademyHall", "AceDesk", rolls: new[] { 1, 0, 0 });
        var court = Rooms.Value("Mesagoza_AceCourt");
        Arrive(game);
        court.ApplyPresence(game.Story.Has);
        var host = game.Play("Mesagoza_AceCourt", "Match", Standing(court), fight: _ => BattleOutcome.Lost);
        Assert.Contains(host.Log, l => l.StartsWith("battle paldea_brassius_rematch"));
        Assert.Equal(0, game.Story.Var("VAR_PALDEA_ACE"));
        Assert.False(game.Story.Has(Raids.AceTitleFlag));
    }

    // ------------------------------------------------------------------ the Treasures of Ruin

    public static readonly TheoryData<string, string, string, string> Treasures = new()
    {
        { "purple", "Wo-Chien", "WO_CHIEN", "PaldeaShrineWoChien" },
        { "yellow", "Chien-Pao", "CHIEN_PAO", "PaldeaShrineChienPao" },
        { "green", "Ting-Lu", "TING_LU", "PaldeaShrineTingLu" },
        { "blue", "Chi-Yu", "CHI_YU", "PaldeaShrineChiYu" },
    };

    [Theory]
    [MemberData(nameof(Treasures))]
    public void EightStakesOpenTheirShrineAndItsTreasureWaitsUntilCaught(string colour, string treasure, string caughtName, string shrine)
    {
        var map = Open.Value;
        string Script(int i) => $"PaldeaStake{char.ToUpperInvariant(colour[0])}{colour[1..]}{i}";
        // Eight stakes of the colour stand as triggers of the open map, and the shrine
        for (int i = 1; i <= 8; i++) Assert.Single(map.Triggers, t => t.Script == Script(i));
        Assert.Single(map.Triggers, t => t.Script == shrine);
        var game = new Game();
        // Before the Way Home a stake says nothing and is not pulled
        var quiet = game.Play("cabo_poco", Script(1));
        Assert.Empty(quiet.Transcript);
        game.WayHome();
        for (int i = 1; i <= 7; i++) game.Play("cabo_poco", Script(i));
        var sealedShrine = game.Play("cabo_poco", shrine);
        Assert.DoesNotContain(sealedShrine.Log, l => l.StartsWith("wild"));
        game.Play("cabo_poco", Script(8));
        // Pulled twice counts once
        game.Play("cabo_poco", Script(8));
        Assert.Equal(8, game.Story.Var($"VAR_PALDEA_STAKES_{colour.ToUpperInvariant()}"));
        // Beaten, not caught: it waits
        game.Play("cabo_poco", shrine, fight: _ => BattleOutcome.Won);
        Assert.False(game.Story.Has($"FLAG_PALDEA_CAUGHT_{caughtName}"));
        var caught = game.Play("cabo_poco", shrine, fight: _ => BattleOutcome.Caught);
        Assert.Contains(caught.Log, l => l.Contains(treasure));
        Assert.True(game.Story.Has($"FLAG_PALDEA_CAUGHT_{caughtName}"));
    }

    // ------------------------------------------------------------------ the stones and the oddities lot

    [Fact]
    public void ClavellGivesThePartnersStonesOnceTheBagHoldsAKeyStone()
    {
        var game = new Game();
        game.WayHome();
        game.Play("Mesagoza_AcademyHall", "Clavell");
        Assert.False(game.Story.Has("FLAG_PALDEA_CLAVELL_MEGA_STONES"));
        game.Bag.AddItem(ItemDatabase.Get("Key Stone")!, 1);
        game.Play("Mesagoza_AcademyHall", "Clavell");
        foreach (string stone in new[] { "Meowscaradite", "Skeledirgite", "Quaquavalite" })
            Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get(stone)!));
        game.Play("Mesagoza_AcademyHall", "Clavell");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Meowscaradite")!));
    }

    [Fact]
    public void TheAuctioneerGivesTheOdditiesLotOnceAfterTheHallOfFame()
    {
        var game = new Game();
        game.Story.Set("FLAG_PALDEA_CASCARRAFA_TEST_DONE");
        game.Play("porto_marinada", "Auctioneer");
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Punching Glove")!));
        game.Story.Set("PaldeaHallOfFame");
        game.Play("porto_marinada", "Auctioneer");
        game.Play("porto_marinada", "Auctioneer");
        foreach (string item in new[] { "Auspicious Armor", "Malicious Armor", "Punching Glove" })
            Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get(item)!));
    }

    // ------------------------------------------------------------------ our species in the zones (plan 03 · D15)

    [Fact]
    public void EveryPaldeanSpeciesOfOursLivesInAZoneAtItsOwnLevelsBehindTheHallOfFame()
    {
        var rows = Open.Value.Zones.SelectMany(z => z.Slots.Select(s => (Zone: z, Slot: s))).Where(x => x.Slot.Ours).ToList();
        foreach (var (zone, slot) in rows)
        {
            var species = PokemonDatabase.Get(slot.Species);
            Assert.True(species != null && OursSlots.Placeable(species), $"{zone.Name}: {slot.Species}");
            Assert.Equal(RegionDatabase.Paldea, OursSlots.RegionOf(species!));
            Assert.InRange(slot.MinLevel, OursSlots.EvolutionLevel(species!), 260);
            Assert.InRange(slot.MaxLevel, slot.MinLevel, 260);
            Assert.Equal(1, slot.Weight);
        }
        var placed = rows.Select(r => r.Slot.Species).ToHashSet();
        var missing = PokemonDatabase.Ours().Where(OursSlots.Placeable).Where(s => OursSlots.RegionOf(s) == RegionDatabase.Paldea && !placed.Contains(s.Name)).Select(s => s.Name).ToList();
        Assert.True(missing.Count == 0, "Paldea's batch has no place for " + string.Join(", ", missing));
        // Shut, a row of ours is in no draw; open, it is
        var field = new OpenWildField();
        var ours = rows[0].Slot;
        Assert.False(field.InTheDraw(ours, FieldWeather.Clear));
        field.OursOpen = true;
        Assert.True(field.InTheDraw(ours, FieldWeather.Clear));
        // and the gate is Paldea's own Hall of Fame
        var story = new StoryState();
        Assert.False(OursSlots.Open(Region, story));
        story.Set("PaldeaHallOfFame");
        Assert.True(OursSlots.Open(Region, story));
    }
}
