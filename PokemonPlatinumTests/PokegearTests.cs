using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The Pokégear (plan 20 · J3): numbers registered by a trainer's script, a rematch asked for on its day and hour
/// and fought, the calls that come on a step, the radio's dial and what its songs do to the wild Pokémon, and the map
/// card's region. Played with no screen, on maps of this class's own.
/// </summary>
public class PokegearTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private static readonly Lazy<World> Johto = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Johto));

    // Each test's own maps: a script beats trainers and moves people about
    private readonly Lazy<Dictionary<string, Map>> Maps = new(() => Johto.Value.BuildMaps().ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase));

    private static readonly DateTime Monday = new(2026, 10, 19), Tuesday = new(2026, 10, 20);

    private NPC Joey => Maps.Value["Johto"].Everyone.First(n => n.Key == "r30_gsboy2");

    private HeadlessScriptHost Play(Script script, StoryState story, NPC? subject, params int[] answers)
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Chikorita")!, 10));
        var host = new HeadlessScriptHost(story, party, new Inventory()) { Map = Maps.Value["Johto"], Fight = _ => BattleOutcome.Won };
        foreach (int a in answers) host.Answers.Enqueue(a);
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(script, subject);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    private HeadlessScriptHost TalkTo(NPC npc, StoryState story, params int[] answers) =>
        Play(Scripts.Find(FieldScripts.For(npc)!, npc.ScriptFile ?? Maps.Value["Johto"].ScriptFileAt(npc.GridX, npc.GridY))!, story, npc, answers);

    [Fact]
    public void ANumberRegisteredByATrainersScriptIsKeptByTheStory()
    {
        var story = new StoryState();
        var joey = Joey;
        Assert.Equal("Joey", FieldScripts.For(joey));
        // Not beaten yet: he battles, and asks nothing
        var first = TalkTo(joey, story);
        Assert.Contains(first.Log, l => l.StartsWith("battle johto_youngster_joey ", StringComparison.Ordinal));
        Assert.DoesNotContain(Pokegear.Contacts(story), c => c.Id == "joey");
        // Beaten: his number, refused and then agreed to
        TalkTo(joey, story, 1);
        Assert.False(story.Has(Pokegear.ContactFlag("joey")));
        var agreed = TalkTo(joey, story, 0);
        Assert.Contains("addcontact joey", agreed.Log);
        Assert.Contains(Pokegear.Contacts(story), c => c.Id == "joey");
        // A save keeps it: it is a flag of the story
        var saved = new StoryState();
        saved.Restore(story.Snapshot());
        Assert.Contains(Pokegear.Contacts(saved), c => c.Id == "joey");
    }

    [Fact]
    public void ARematchIsAskedForOnItsDayAndHourAndNotBefore()
    {
        var joey = PhoneBook.Get("joey")!;
        Assert.Equal("Monday", joey.RematchDay);
        Assert.Equal("Day", joey.RematchTime);
        Assert.Equal(new[] { "johto_youngster_joey_2", "johto_youngster_joey_3", "johto_youngster_joey_4" },
            TrainerDatabase.Get("johto_youngster_joey")!.Rematches!.Select(r => r!));
        var story = new StoryState();
        story.Set(Pokegear.ContactFlag("joey"));
        // Not beaten yet
        Assert.False(PhoneRematch.Wants(joey, story, Monday, TimeOfDay.Day));
        story.Defeat("johto_youngster_joey");
        // Beaten: on Mondays by day, not on Tuesday, not at night, not in the morning
        Assert.True(PhoneRematch.Wants(joey, story, Monday, TimeOfDay.Day));
        Assert.True(PhoneRematch.Wants(joey, story, Monday, TimeOfDay.Twilight));
        Assert.False(PhoneRematch.Wants(joey, story, Tuesday, TimeOfDay.Day));
        Assert.False(PhoneRematch.Wants(joey, story, Monday, TimeOfDay.Night));
        Assert.False(PhoneRematch.Wants(joey, story, Monday, TimeOfDay.Morning));
        // A call then asks once, and the rematch waits
        Assert.Equal(Pokegear.CallRematch, PhoneRematch.Call(joey, story, Monday, TimeOfDay.Day, incoming: false));
        Assert.Equal(Pokegear.CallWaiting, PhoneRematch.Call(joey, story, Monday, TimeOfDay.Day, incoming: true));
        // Someone not registered is never asked
        var wade = PhoneBook.Get("wade")!;
        story.Defeat("johto_bug_catcher_wade");
        Assert.False(PhoneRematch.Wants(wade, story, Tuesday, TimeOfDay.Night));
    }

    [Fact]
    public void ARematchIsCalledForAndFought()
    {
        var story = new StoryState();
        var joey = Joey;
        TalkTo(joey, story);
        TalkTo(joey, story, 0);
        // Rung on his day: his line for a rematch, and he waits on Route 30, spinning
        var contact = PhoneBook.Get("joey")!;
        story.SetVar(Pokegear.CallVar, PhoneRematch.Call(contact, story, Monday, TimeOfDay.Day, incoming: false));
        var call = Play(Scripts.Find(contact.Script, GameEngine.PhoneScripts)!, story, null);
        Assert.Contains(call.Transcript, l => l.Text.Contains("battle me again", StringComparison.OrdinalIgnoreCase));
        PhoneRematch.KeepWaiting(Maps.Value["Johto"].Everyone, story);
        Assert.True(joey.ReadyForRematch);
        // Spoken to, he brings the team of the first level the Badges have opened: none yet, so his own
        var fought = TalkTo(joey, story);
        Assert.Contains(fought.Log, l => l.StartsWith("battle johto_youngster_joey ", StringComparison.Ordinal));
        Assert.False(story.Has(PhoneRematch.WaitingFlag("joey")));
        Assert.False(joey.ReadyForRematch);
        // With two Johto Badges his second team comes
        story.GiveBadge(RegionDatabase.Johto, 0);
        story.GiveBadge(RegionDatabase.Johto, 1);
        story.SetVar(Pokegear.CallVar, PhoneRematch.Call(contact, story, Monday, TimeOfDay.Day, incoming: true));
        PhoneRematch.KeepWaiting(Maps.Value["Johto"].Everyone, story);
        var second = TalkTo(joey, story);
        Assert.Contains(second.Log, l => l.StartsWith("battle johto_youngster_joey_2 ", StringComparison.Ordinal));
        Assert.True(story.HasDefeated("johto_youngster_joey_2"));
    }

    [Fact]
    public void ACallComesFromTheSeedOnceTenMinutesHavePassed()
    {
        var story = new StoryState();
        foreach (string id in new[] { "mother", "prof_elm", "joey", "wade", "liz" }) Pokegear.Register(story, id);
        PhoneContact? Ring(int seed, float seconds, string? area)
        {
            var ringer = new PhoneRinger();
            ringer.Advance(seconds);
            return ringer.TryRing(story, area, new Random(seed));
        }
        // Not before ten minutes
        Assert.Null(Ring(1, PhoneRinger.WaitSeconds - 1, "new_bark"));
        // The same seed rings the same contact; Mom and Elm never ring on their own, nor anyone met where the player is
        var rang = Enumerable.Range(0, 60).Select(s => Ring(s, PhoneRinger.WaitSeconds, "route_30")).ToList();
        Assert.Equal(rang.Select(c => c?.Id), Enumerable.Range(0, 60).Select(s => Ring(s, PhoneRinger.WaitSeconds, "route_30")?.Id));
        Assert.Contains(rang, c => c == null);
        Assert.Contains(rang, c => c != null);
        Assert.DoesNotContain(rang, c => c is { Id: "mother" or "prof_elm" or "joey" });
        // A ring, or a miss, waits ten minutes again
        var again = new PhoneRinger();
        again.Advance(PhoneRinger.WaitSeconds);
        again.TryRing(story, null, new Random(3));
        Assert.Null(again.TryRing(story, null, new Random(3)));
    }

    [Fact]
    public void TheDialFindsEachStationOnlyWhereItIsReceived()
    {
        var dial = new RadioDial { Signal = RadioSignal.Johto };
        Assert.Null(dial.Tuned());
        foreach (var spot in RadioDial.SpotsOf(RadioSignal.Johto))
        {
            dial.Place(spot.X, spot.Y);
            Assert.Equal((spot, true), dial.Tuned());
            dial.Place(spot.X + spot.Clear + 2, spot.Y);
            Assert.Equal((spot, false), dial.Tuned());
        }
        Assert.Equal(RadioStation.PokemonMusic, RadioDial.StationOf(0, 9));
        Assert.Equal(RadioStation.PokemonTalk, RadioDial.StationOf(1, 9));
        Assert.Equal(RadioStation.TrainerProfiles, RadioDial.StationOf(2, 8));
        Assert.Equal(RadioStation.ThatTownThesePeople, RadioDial.StationOf(2, 9));
        Assert.Equal(RadioStation.SearchParty, RadioDial.StationOf(3, 9));
        Assert.Equal(RadioStation.RadioDrama, RadioDial.StationOf(3, 10));
        Assert.Equal(RadioStation.BuenasPassword, RadioDial.StationOf(3, 11));
        // Nothing in a cave, the Unown alone in the Ruins of Alph's halls
        dial.Signal = RadioDial.SignalAt("route_31", cave: true);
        Assert.Equal(RadioSignal.NoSignal, dial.Signal);
        dial.Place(112, 76);
        Assert.Null(dial.Tuned());
        Assert.Equal(RadioSignal.Alph, RadioDial.SignalAt("ruins_of_alph_underground_hall", cave: false));
        Assert.Equal(RadioSignal.Johto, RadioDial.SignalAt("violet", cave: false));
        // The cursor never leaves the dial
        var edge = new RadioDial();
        for (int i = 0; i < 40; i++) edge.Move(1, 0);
        Assert.True(edge.X - RadioDial.CenterX <= RadioDial.Reach);
        // Every talk station has words of its own
        foreach (var station in new[] { RadioStation.PokemonTalk, RadioStation.TrainerProfiles, RadioStation.ThatTownThesePeople, RadioStation.SearchParty, RadioStation.RadioDrama, RadioStation.BuenasPassword })
            Assert.NotEmpty(RadioShows.Of(station));
    }

    [Fact]
    public void TheMarchAndTheLullabyMoveTheRateByHeartGoldsNumbers()
    {
        Assert.Equal(25, Pokegear.RadioRateBonus(RadioSong.March));
        Assert.Equal(-25, Pokegear.RadioRateBonus(RadioSong.Lullaby));
        Assert.Equal(0, Pokegear.RadioRateBonus(RadioSong.HoennSound));
        Assert.Equal(0, Pokegear.RadioRateBonus(RadioSong.None));
        // Over many steps the March meets more than nothing does, and the Lullaby fewer
        int Met(int radio)
        {
            var steps = new EncounterSteps();
            var rng = new Random(7);
            return Enumerable.Range(0, 4000).Count(_ => steps.Meets(20, thick: false, rng, radio: radio));
        }
        int none = Met(0), march = Met(25), lullaby = Met(-25);
        Assert.True(march > none && none > lullaby, $"march {march}, none {none}, lullaby {lullaby}");
        // On foot the share is Platinum's 40, so the March makes it 65 and the Lullaby 15: about in proportion
        Assert.InRange(march / (double)none, 1.4, 1.9);
        Assert.InRange(lullaby / (double)none, 0.25, 0.5);
    }

    [Fact]
    public void TheSoundsSwapTheSlotsOnlyOnTheirDays()
    {
        Assert.Equal(RadioSong.March, RadioDial.MusicOn(DayOfWeek.Monday, nationalDex: true));
        Assert.Equal(RadioSong.Lullaby, RadioDial.MusicOn(DayOfWeek.Tuesday, nationalDex: true));
        Assert.Equal(RadioSong.HoennSound, RadioDial.MusicOn(DayOfWeek.Wednesday, nationalDex: true));
        Assert.Equal(RadioSong.March, RadioDial.MusicOn(DayOfWeek.Wednesday, nationalDex: false));
        Assert.Equal(RadioSong.SinnohSound, RadioDial.MusicOn(DayOfWeek.Thursday, nationalDex: true));
        Assert.Equal(RadioSong.Lullaby, RadioDial.MusicOn(DayOfWeek.Thursday, nationalDex: false));

        var area = Maps.Value["Johto"].Areas.First(a => a.Key == "route_29");
        Assert.Equal(2, area.HoennSounds.Count);
        Assert.Equal(2, area.SinnohSounds.Count);
        var plain = EncounterSlots.Grass(area, new EncounterMoment { Time = TimeOfDay.Morning });
        var march = EncounterSlots.Grass(area, new EncounterMoment { Time = TimeOfDay.Morning, Radio = RadioSong.March });
        Assert.Equal(plain.Select(s => s.SpeciesName), march.Select(s => s.SpeciesName));
        var hoenn = EncounterSlots.Grass(area, new EncounterMoment { Time = TimeOfDay.Morning, Radio = RadioSong.HoennSound });
        Assert.Equal(new[] { area.HoennSounds[0], area.HoennSounds[0], area.HoennSounds[1], area.HoennSounds[1] }, hoenn.Skip(2).Take(4).Select(s => s.SpeciesName));
        Assert.Equal(plain.Take(2).Concat(plain.Skip(6)).Select(s => s.SpeciesName), hoenn.Take(2).Concat(hoenn.Skip(6)).Select(s => s.SpeciesName));
        var sinnoh = EncounterSlots.Grass(area, new EncounterMoment { Time = TimeOfDay.Morning, Radio = RadioSong.SinnohSound });
        Assert.Equal(area.SinnohSounds[1], sinnoh[5].SpeciesName);
    }

    [Fact]
    public void TheMapCardNamesTheAreaUnderTheCursor()
    {
        var johto = RegionMap.Of("johto")!;
        Assert.Equal("Johto", johto.Map);
        Assert.Equal("new_bark", johto.BlockAt(21, 12)!.Area.Key);
        Assert.Equal("New Bark Town", johto.BlockAt(21, 12)!.Name);
        Assert.Equal("Route 29", johto.BlockAt(19, 12)!.Name);
        Assert.Equal("Violet City", johto.BlockAt(14, 7)!.Name);
        // Kanto, on the east of the same grid, is not on Johto's map card
        Assert.Null(johto.BlockAt(32, 11));
        Assert.False(johto.InView(32, 11));
        Assert.Same(johto, RegionMap.ForMap("Johto"));
    }

    [Fact]
    public void TheCardsAreJohtosAndComeWithTheirFlags()
    {
        var story = new StoryState();
        Assert.False(Pokegear.Has(story, RegionDatabase.Johto));
        story.Set(Pokegear.Flag);
        Assert.True(Pokegear.Has(story, RegionDatabase.Johto));
        Assert.False(Pokegear.Has(story, RegionDatabase.Sinnoh));
        Assert.Equal(new[] { PokegearCard.Phone }, Pokegear.Cards(story));
        story.Set(Pokegear.MapCardFlag);
        story.Set(Pokegear.RadioCardFlag);
        Assert.Equal(new[] { PokegearCard.Phone, PokegearCard.Map, PokegearCard.Radio }, Pokegear.Cards(story));
        // Every contact who rings on their own has a script of phone.txt, or the one for nobody
        Assert.NotNull(Scripts.Find("Nobody", GameEngine.PhoneScripts));
        foreach (string id in new[] { "mother", "prof_elm", "joey", "wade", "liz", "ralph" })
            Assert.NotNull(Scripts.Find(PhoneBook.Get(id)!.Script, GameEngine.PhoneScripts));
    }

    [Fact]
    public void ASaveFromBeforeThePhoneHasMomsAndElmsNumbers()
    {
        var story = new StoryState();
        story.Set(Pokegear.Flag);
        story.Set("FLAG_GOT_STARTER");
        StoryMigration.Upgrade(story, 21, Array.Empty<Pokemon>(), Scripts);
        Assert.Equal(new[] { "mother", "prof_elm" }, Pokegear.Contacts(story).Select(c => c.Id));
        // A game of Sinnoh has none
        var sinnoh = new StoryState();
        StoryMigration.Upgrade(sinnoh, 21, Array.Empty<Pokemon>(), Scripts);
        Assert.Empty(Pokegear.Contacts(sinnoh));
    }
}

/// <summary>
/// The week on Route 32 (plan 20, the stretch after J3): Frieda of Friday is there on Fridays alone, as HeartGold's map
/// script has her, and gives her Poison Barb once.
/// </summary>
public class RouteThirtyTwoWeekTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private static readonly Lazy<World> Johto = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Johto));
    private readonly Lazy<Map> map = new(() => Johto.Value.BuildMaps().First(m => m.Name == "Johto"));

    private HeadlessScriptHost Play(Script script, StoryState story, DayOfWeek day, NPC? subject = null)
    {
        var host = new HeadlessScriptHost(story, new Party(), new Inventory()) { Map = map.Value, Weekday = day };
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(script, subject);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        map.Value.ApplyPresence(story.Has);
        return host;
    }

    private bool FriedaThere(DayOfWeek day, StoryState story)
    {
        Play(Scripts.In("route_32", ScriptLibrary.OnEnter)!, story, day);
        return map.Value.NPCs.Any(n => n.Key == "r32_gsgirl1");
    }

    [Fact]
    public void FriedaIsOnRouteThirtyTwoOnFridaysAloneAndGivesHerBarbOnce()
    {
        var story = new StoryState();
        foreach (var day in Enum.GetValues<DayOfWeek>())
            Assert.Equal(day == DayOfWeek.Friday, FriedaThere(day, story));
        Assert.DoesNotContain(map.Value.NPCs, n => n.Key == "r32_gsmiddleman1_2");

        Assert.True(FriedaThere(DayOfWeek.Friday, story));
        var frieda = map.Value.NPCs.First(n => n.Key == "r32_gsgirl1");
        var first = Play(Scripts.Find(FieldScripts.For(frieda)!, "route_32")!, story, DayOfWeek.Friday, frieda);
        Assert.Contains(first.Transcript, l => l.Text.Contains("Poison Barb", StringComparison.Ordinal));
        Assert.True(story.Has("FLAG_GOT_POISON_BARB_FROM_FRIEDA"));
        Assert.Equal(1, story.Var("VAR_NUM_MET_WEEKDAY_SIBLINGS"));
        var again = Play(Scripts.Find(FieldScripts.For(frieda)!, "route_32")!, story, DayOfWeek.Friday, frieda);
        Assert.DoesNotContain(again.Transcript, l => l.Text.Contains("Poison Barb", StringComparison.Ordinal));
        Assert.Equal(1, story.Var("VAR_NUM_MET_WEEKDAY_SIBLINGS"));
    }

    [Fact]
    public void JohtosPokemonCentersHaveTheirPcAndTheCastTheirOwnLooks()
    {
        var maps = Johto.Value.BuildMaps().ToList();
        // The PC left of each Center's counter is read as every PC is (common.PC)
        foreach (var center in maps.Where(m => m.Areas.Any(a => a.Key.EndsWith("_pokecenter_1f", StringComparison.Ordinal))))
            Assert.Equal("PC", center.TileScripts[(4, 8)]);
        // Elm, the red-haired boy and Falkner stand as themselves, ahead of plan 20 · J14
        NPC Who(string key) => maps.SelectMany(m => m.Everyone).First(n => n.Key == key);
        Assert.Equal("elm", Who("t20r0101_doctor").NpcType, ignoreCase: true);
        Assert.Equal("silver", Who("t21_gsrivel").NpcType, ignoreCase: true);
        Assert.Equal("falkner", Who("t22gym0101_gsleader1").NpcType, ignoreCase: true);
    }

    [Fact]
    public void EveryContactOfEveryBookHasAName()
    {
        // (a name the string table lacks shows as its key in brackets: Juan, Flannery and Winona did, on the toast)
        foreach (var contact in PhoneBook.All.Concat(PhoneBook.Hoenn))
            Assert.False(contact.Name.StartsWith("["), $"{contact.Id} has no name: {contact.Name}");
    }
}
