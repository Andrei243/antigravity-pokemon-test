using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumTests;

/// <summary>
/// The boxes' tools (plan 12 · Q26; style guide, "PC boxes", "The boxes' tools"): the search, the sorts, a rectangle and a
/// search's results picked up and put down together, several released behind a hold, and the PC as plan 06 · R12 left it
/// with the row off. Driven without a window, as the game's keys would drive them.
/// </summary>
public class BoxToolsTests
{
    private static Pokemon Mon(string species, int level, Gender gender = Gender.Male, Nature nature = Nature.Hardy) =>
        new(PokemonDatabase.Get(species)!, level, gender, nature, false);

    private static Party PartyOf(params Pokemon[] members)
    {
        var party = new Party();
        foreach (var p in members) party.Add(p);
        return party;
    }

    private static PCScreen ToolsOn(PcBoxes pc)
    {
        var screen = new PCScreen { Tools = true };
        screen.Open(pc);
        return screen;
    }

    [Fact]
    public void EachFilterFindsWhatItAsksForAndNothingElse()
    {
        var pc = new PcBoxes();
        var starly = Mon("Starly", 10, Gender.Female, Nature.Jolly);
        var shinx = Mon("Shinx", 20);
        var bidoof = Mon("Bidoof", 30);
        bidoof.HeldItem = ItemDatabase.Get("Oran Berry");
        shinx.IsShiny = true;
        shinx.Marks = (int)(Markings.Heart | Markings.Star);
        var egg = Mon("Piplup", 1);
        egg.IsEgg = true;
        foreach (var p in new[] { starly, shinx, bidoof, egg }) pc.Store(p);

        List<Pokemon> Found(BoxFilter f) => BoxQuery.Find(pc, f).Select(at => pc[at.Box, at.Slot]!).ToList();

        Assert.Equal(new[] { starly, shinx, bidoof, egg }, Found(new BoxFilter()));
        Assert.Equal(new[] { starly }, Found(new BoxFilter { Species = "Starly" }));
        Assert.Equal(new[] { shinx }, Found(new BoxFilter { Type = PokemonType.Electric }));
        Assert.Equal(new[] { starly }, Found(new BoxFilter { Type = PokemonType.Flying }));
        var move = bidoof.Moves[0].Name;
        Assert.Contains(bidoof, Found(new BoxFilter { Move = move }));
        Assert.DoesNotContain(egg, Found(new BoxFilter { Move = move }));
        Assert.All(Found(new BoxFilter { Ability = starly.AbilityName }), p => Assert.Equal(starly.AbilityName, p.AbilityName));
        Assert.Equal(new[] { starly }, Found(new BoxFilter { Nature = Nature.Jolly }));
        Assert.Equal(new[] { shinx, bidoof }, Found(new BoxFilter { MinLevel = 15 }));
        Assert.Equal(new[] { starly, shinx }, Found(new BoxFilter { MaxLevel = 25 }));
        Assert.Equal(new[] { shinx }, Found(new BoxFilter { MinLevel = 15, MaxLevel = 25 }));
        Assert.Equal(new[] { bidoof }, Found(new BoxFilter { Item = BoxFilter.Anything }));
        Assert.Equal(new[] { starly, shinx }, Found(new BoxFilter { Item = BoxFilter.Nothing }));
        Assert.Equal(new[] { bidoof }, Found(new BoxFilter { Item = "Oran Berry" }));
        Assert.Equal(new[] { starly }, Found(new BoxFilter { Gender = Gender.Female }));
        Assert.Equal(new[] { shinx }, Found(new BoxFilter { Shiny = true }));
        Assert.Equal(new[] { shinx }, Found(new BoxFilter { Marks = Markings.Heart }));
        Assert.Empty(Found(new BoxFilter { Marks = Markings.Heart | Markings.Circle }));
    }

    [Fact]
    public void TheSearchPanelOffersOnlyWhatTheBoxesHold()
    {
        var pc = new PcBoxes();
        pc.Store(Mon("Starly", 4));
        pc.Store(Mon("Shinx", 9));
        var search = new BoxSearch(pc);
        Assert.Equal(new object?[] { null, "Starly", "Shinx" }, search.ChoicesOf(BoxSearchRow.Species));
        // Normal, Flying and Electric, in the chart's order
        Assert.Equal(new object?[] { null, PokemonType.Normal, PokemonType.Electric, PokemonType.Flying }.OrderBy(t => t is null ? -1 : (int)t),
            search.ChoicesOf(BoxSearchRow.Type));
        search.Move(1);
        Assert.True(search.Change(1));
        Assert.Equal(PokemonType.Normal, search.Filter.Type);
        Assert.True(search.Change(-1));
        Assert.True(search.Filter.IsEmpty);
    }

    [Fact]
    public void ASortKeepsTheTeamAndIsTheSameTwice()
    {
        var team = Mon("Turtwig", 12);
        var party = PartyOf(team);
        var pc = new PcBoxes();
        var names = new[] { "Shinx", "Bidoof", "Starly", "Kricketot", "Budew" };
        for (int i = 0; i < names.Length; i++) pc.PutIn(0, i * 2, Mon(names[i], 5 + i));
        pc.PutIn(1, 3, Mon("Abra", 40));

        foreach (BoxSort by in Enum.GetValues<BoxSort>())
        {
            BoxQuery.Sort(pc, 0, by);
            var once = Enumerable.Range(0, PcBoxes.BoxSize).Select(s => pc[0, s]).ToList();
            BoxQuery.Sort(pc, 0, by);
            Assert.Equal(once, Enumerable.Range(0, PcBoxes.BoxSize).Select(s => pc[0, s]).ToList());
            // Packed from the first place, the other box untouched, the team untouched
            Assert.All(once.Take(5), Assert.NotNull);
            Assert.All(once.Skip(5), Assert.Null);
            Assert.Equal("Abra", pc[1, 3]!.Species.Name);
            Assert.Equal(new[] { team }, party.Members);
        }
        BoxQuery.Sort(pc, 0, BoxSort.Number);
        Assert.Equal(new[] { "Starly", "Bidoof", "Kricketot", "Shinx", "Budew" }, Enumerable.Range(0, 5).Select(s => pc[0, s]!.Species.Name));
        BoxQuery.Sort(pc, 0, BoxSort.Level);
        Assert.Equal(new[] { 9, 8, 7, 6, 5 }, Enumerable.Range(0, 5).Select(s => pc[0, s]!.Level));

        // Every box at once: laid out from the first place on, the same twice
        BoxQuery.SortAll(pc, BoxSort.Number);
        var all = pc.Boxes.SelectMany(b => b.Slots).ToList();
        BoxQuery.SortAll(pc, BoxSort.Number);
        Assert.Equal(all, pc.Boxes.SelectMany(b => b.Slots).ToList());
        Assert.Equal("Abra", pc[0, 0]!.Species.Name);
        Assert.Equal(6, pc.Count);
        Assert.Null(pc[1, 3]);
        Assert.Equal(new[] { team }, party.Members);
    }

    [Fact]
    public void ARectanglePutDownInABoxWithRoomLandsInOrderAndWithoutRoomIsRefusedWhole()
    {
        var party = PartyOf(Mon("Turtwig", 5));
        var pc = new PcBoxes();
        var picked = new[] { Mon("Starly", 3), Mon("Bidoof", 3), Mon("Shinx", 3), Mon("Budew", 3) };
        // A rectangle of two by two at the box's top left
        pc.PutIn(0, 0, picked[0]); pc.PutIn(0, 1, picked[1]);
        pc.PutIn(0, PcBoxes.Columns, picked[2]); pc.PutIn(0, PcBoxes.Columns + 1, picked[3]);
        var notes = new List<string>();
        var screen = ToolsOn(pc);
        screen.Move(1, 0, party.Count);
        Assert.Equal((StorageZone.Box, 0), (screen.Zone, screen.Cell));
        screen.Confirm(party, pc, notes.Add);
        Assert.Contains(PcAction.Select, screen.Menu!);
        screen.Choose(PcAction.Select, party, pc, notes.Add);
        screen.Move(1, 0, party.Count);
        screen.Move(0, 1, party.Count);
        Assert.Equal(new[] { 0, 1, PcBoxes.Columns, PcBoxes.Columns + 1 }, screen.Rectangle);
        // The rectangle's corner stays in the box
        screen.Move(0, -1, party.Count); screen.Move(0, -1, party.Count);
        Assert.Equal((StorageZone.Box, 1), (screen.Zone, screen.Cell));
        screen.Move(0, 1, party.Count);
        screen.Confirm(party, pc, notes.Add);
        Assert.Equal(new[] { PcGroupAction.Move, PcGroupAction.Release, PcGroupAction.Cancel }, screen.GroupMenu);
        screen.Confirm(party, pc, notes.Add);
        Assert.Equal(picked, screen.Carried.Select(c => c.Pokemon));
        Assert.Equal(0, pc.Count);

        // A box with three places left refuses all four, and they stay carried
        for (int s = 0; s < PcBoxes.BoxSize - 3; s++) pc.PutIn(1, s, Mon("Kricketot", 2));
        screen.ChangeBox(1, party.Count);
        Assert.False(screen.PutDownMany(pc, notes.Add));
        Assert.Equal(4, screen.Carried.Count);
        Assert.Equal(PcBoxes.BoxSize - 3, pc.Boxes[1].Count);

        // Onto the team, never
        screen.ChangeBox(-1, party.Count);
        screen.Move(-1, 0, party.Count);
        screen.Move(-1, 0, party.Count);
        Assert.Equal(StorageZone.Party, screen.Zone);
        Assert.False(screen.PutDownMany(pc, notes.Add));
        Assert.Single(party.Members);

        // A box with room: from the cursor's place on, round to the first, in the order they were picked up
        screen.Move(1, 0, party.Count);
        Assert.Equal(StorageZone.Box, screen.Zone);
        pc.PutIn(0, 2, Mon("Abra", 9));
        screen.Cell = PcBoxes.BoxSize - 2;
        int from = screen.Cell;
        Assert.True(screen.PutDownMany(pc, notes.Add));
        Assert.Empty(screen.Carried);
        var expected = BoxQueryPlaces(from);
        for (int i = 0; i < 4; i++) Assert.Same(picked[i], pc[0, expected[i]]);
    }

    // The empty places of box 1 (all but place 2) from a place on, round to the first
    private static int[] BoxQueryPlaces(int from) =>
        Enumerable.Range(0, PcBoxes.BoxSize).Select(i => (from + i) % PcBoxes.BoxSize).Where(s => s != 2).Take(4).ToArray();

    [Fact]
    public void CarriedTogetherAndPutBackEachGoesWhereItCameFrom()
    {
        var party = PartyOf(Mon("Turtwig", 5));
        var pc = new PcBoxes();
        var a = Mon("Starly", 3);
        var b = Mon("Bidoof", 3);
        pc.PutIn(0, 4, a);
        pc.PutIn(2, 7, b);
        var screen = ToolsOn(pc);
        screen.PickUpMany(pc, new[] { new BoxPlace(0, 4), new BoxPlace(2, 7) });
        Assert.Equal(0, pc.Count);
        Assert.True(screen.Cancel(party, pc));
        Assert.Same(a, pc[0, 4]);
        Assert.Same(b, pc[2, 7]);
    }


    [Fact]
    public void WithTheRowOffThePcIsPlatinums()
    {
        var party = PartyOf(Mon("Turtwig", 5));
        var pc = new PcBoxes();
        pc.PutIn(0, 0, Mon("Starly", 3));
        var notes = new List<string>();
        var screen = new PCScreen();
        screen.Open(pc);
        screen.Move(1, 0, party.Count);
        screen.Confirm(party, pc, notes.Add);
        Assert.Equal(new[] { PcAction.Move, PcAction.Withdraw, PcAction.Summary, PcAction.Mark, PcAction.Release, PcAction.Cancel }, screen.Menu);
        screen.Cancel(party, pc);
        screen.Move(0, -1, party.Count);
        screen.Confirm(party, pc, notes.Add);
        Assert.Equal(new[] { PcBoxAction.Name, PcBoxAction.Wallpaper, PcBoxAction.Cancel }, screen.BoxMenu);

        // The row: by the rules unless chosen, which a Platinum game reads as off and the modern rules as on
        var settings = new GameSettings();
        Assert.Equal(RulesDefault.Rules, settings.BoxTools);
        Assert.False(FieldHelp.On(settings.BoxTools, RulesPreset.Platinum));
        Assert.True(FieldHelp.On(settings.BoxTools, RulesPreset.Modern));
        OptionsScreen.Change(settings, OptionRow.BoxTools, 1);
        Assert.Equal(RulesDefault.On, settings.BoxTools);
        OptionsScreen.Change(settings, OptionRow.BoxTools, 1);
        Assert.Equal(RulesDefault.Off, settings.BoxTools);
    }

    [Fact]
    public void EveryStarlyInEighteenBoxesIsFoundPickedUpTogetherAndPutDownInOneBox()
    {
        var party = PartyOf(Mon("Turtwig", 5));
        var pc = new PcBoxes();
        // A Starly and a Bidoof in each of the eighteen boxes, at a place of their own
        for (int box = 0; box < PcBoxes.BoxCount; box++)
        {
            pc.PutIn(box, box % PcBoxes.BoxSize, Mon("Starly", 2 + box));
            pc.PutIn(box, (box + 7) % PcBoxes.BoxSize, Mon("Bidoof", 2));
        }
        var notes = new List<string>();
        var screen = ToolsOn(pc);
        // The box's name, its menu, SEARCH
        screen.Move(1, 0, party.Count);
        screen.Move(0, -1, party.Count);
        screen.Confirm(party, pc, notes.Add);
        Assert.Equal(new[] { PcBoxAction.Name, PcBoxAction.Wallpaper, PcBoxAction.Search, PcBoxAction.Sort, PcBoxAction.Teams, PcBoxAction.Cancel }, screen.BoxMenu);
        screen.MenuIndex = 2;
        screen.Confirm(party, pc, notes.Add);
        var search = screen.Search!;
        Assert.True(search.Choose(BoxSearchRow.Species, "Starly"));
        while (BoxSearch.Rows[search.Row] != BoxSearchRow.Go) screen.Move(0, 1, party.Count);
        screen.Confirm(party, pc, notes.Add);
        Assert.Equal(18, screen.Results!.Count);
        // PICK UP ALL, then down in the first box from its first place, round its Bidoof
        screen.Confirm(party, pc, notes.Add);
        Assert.Equal(18, screen.Carried.Count);
        Assert.Null(screen.Search);
        Assert.Equal(0, pc.All.Count(p => p.Species.Name == "Starly"));
        screen.Move(0, 1, party.Count);
        Assert.Equal((StorageZone.Box, 0, 0), (screen.Zone, screen.Box, screen.Cell));
        screen.Confirm(party, pc, notes.Add);
        Assert.Empty(screen.Carried);
        var starlies = pc.Boxes[0].Slots.Where(p => p?.Species.Name == "Starly").ToList();
        Assert.Equal(18, starlies.Count);
        Assert.Equal(Enumerable.Range(2, 18), starlies.Select(p => p!.Level));
        Assert.Equal("Bidoof", pc[0, 7]!.Species.Name);
        Assert.Equal(18, pc.All.Count(p => p.Species.Name == "Bidoof"));
    }
}

/// <summary>
/// Releasing several at once (plan 12 · Q26) behind Q9's hold: the options' HOLD TO CONFIRM is the whole process's
/// (<see cref="ConfirmHold.Asked"/>), so these run with the other shared state.
/// </summary>
[Collection("Shared clocks and dice")]
public class BoxToolsReleaseTests : IDisposable
{
    public void Dispose() => ConfirmHold.Asked = false;

    private static Pokemon Mon(string species, int level) => new(PokemonDatabase.Get(species)!, level, Gender.Male, Nature.Hardy, false);

    // Three Bidoof in a rectangle, its RELEASE chosen: the question up
    private static (PCScreen Screen, Party Party, PcBoxes Pc, List<string> Notes) Asking()
    {
        var party = new Party();
        party.Add(Mon("Turtwig", 5));
        var pc = new PcBoxes();
        for (int s = 0; s < 3; s++) pc.PutIn(0, s, Mon("Bidoof", 3));
        var notes = new List<string>();
        var screen = new PCScreen { Tools = true };
        screen.Open(pc);
        screen.Move(1, 0, party.Count);
        screen.Choose(PcAction.Select, party, pc, notes.Add);
        screen.Move(1, 0, party.Count); screen.Move(1, 0, party.Count);
        screen.Confirm(party, pc, notes.Add);
        screen.MenuIndex = 1;
        screen.Confirm(party, pc, notes.Add);
        Assert.True(screen.AskingReleaseMany);
        Assert.Equal(3, screen.Releasing.Count);
        return (screen, party, pc, notes);
    }

    [Fact]
    public void ReleasingManyWaitsForTheHoldWhereTheOptionsAskForIt()
    {
        ConfirmHold.Asked = true;
        var (screen, party, pc, notes) = Asking();
        // A press only begins the hold; let go early, nothing goes and the question stands
        screen.Confirm(party, pc, notes.Add);
        Assert.True(screen.ReleaseHold.Holding);
        Assert.Equal(3, pc.Count);
        screen.HoldRelease(party, pc, notes.Add, 0.2f, true);
        screen.HoldRelease(party, pc, notes.Add, 0.05f, false);
        Assert.False(screen.ReleaseHold.Holding);
        Assert.True(screen.AskingReleaseMany);
        Assert.Equal(3, pc.Count);
        // Held to the end, all three go
        screen.Confirm(party, pc, notes.Add);
        screen.HoldRelease(party, pc, notes.Add, ConfirmHold.Seconds + 0.01f, true);
        Assert.False(screen.AskingReleaseMany);
        Assert.Equal(0, pc.Count);
        Assert.Single(party.Members);
    }

    [Fact]
    public void WithTheHoldOffTheYesReleasesThemAsTheSingleReleaseDoes()
    {
        ConfirmHold.Asked = false;
        var (screen, party, pc, notes) = Asking();
        screen.Confirm(party, pc, notes.Add);
        Assert.False(screen.AskingReleaseMany);
        Assert.Equal(0, pc.Count);
        // B on the question keeps them
        var (kept, keptParty, keptPc, _) = Asking();
        Assert.True(kept.Cancel(keptParty, keptPc));
        Assert.False(kept.AskingReleaseMany);
        Assert.Equal(3, keptPc.Count);
    }
}
