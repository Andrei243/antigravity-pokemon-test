using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// What is left here (plan 12 · Q22): the counts of the items, trainers and species the area underfoot still holds, of
/// what can be had now from where the player stands.
/// </summary>
public class AreaChecklistTests
{
    /// <summary>
    /// A field of grass 12 by 12 cut in two by a wall down column 6, with a small tree in its one gap at (6, 5): the
    /// player stands at (2, 2), on the west side.
    /// </summary>
    private static Map Field()
    {
        var map = new Map(12, 12) { Name = "ChecklistField" };
        for (int y = 0; y < 12; y++)
            if (y != 5) map.SetGroundTile(6, y, TileType.Wall, isSolid: true);
        map.AddObstacle(PropType.CutTree, 6, 5);
        return map;
    }

    private static NPC Ball(int x, int y, string flag) => new()
    {
        Name = "ball_" + flag, NpcType = NPC.ItemBallType, GridX = x, GridY = y, HiddenBy = flag, Item = "Potion"
    };

    private static NPC Trainer(int x, int y, string id) => new()
    {
        Name = "trainer_" + x, GridX = x, GridY = y, IsTrainer = true,
        TrainerData = new Trainer { Id = id, Name = "Tom", TrainerClass = "Youngster" }
    };

    private static AreaCounts Count(Map map, ChecklistTerms terms) => AreaChecklist.Count(map, 2, 2, map.HeightAt(2, 2), TravelMode.OnFoot, terms);

    [Fact]
    public void ABallPickedUpLowersTheCount()
    {
        var map = Field();
        map.Add(Ball(3, 3, "FLAG_TEST_BALL"));
        var story = new StoryState();
        var terms = new ChecklistTerms { Story = story };
        Assert.Equal(1, Count(map, terms).Items);
        story.Set("FLAG_TEST_BALL");
        Assert.Equal(0, Count(map, terms).Items);
    }

    [Fact]
    public void AHiddenItemFoundLowersTheCount()
    {
        var map = Field();
        map.HiddenItems[(4, 4)] = new HiddenItem("Potion", 1, "FLAG_TEST_HIDDEN");
        var story = new StoryState();
        var terms = new ChecklistTerms { Story = story };
        Assert.Equal(1, Count(map, terms).Items);
        story.Set("FLAG_TEST_HIDDEN");
        Assert.Equal(0, Count(map, terms).Items);
    }

    [Fact]
    public void TwinsCountOnceAndABeatenTrainerNotAtAll()
    {
        var map = Field();
        map.Add(Trainer(3, 8, "twins_amy_and_may"));
        map.Add(Trainer(4, 8, "twins_amy_and_may"));
        map.Add(Trainer(2, 9, "youngster_tom"));
        var story = new StoryState();
        var terms = new ChecklistTerms { Story = story };
        Assert.Equal(2, Count(map, terms).Trainers);
        story.Defeat("twins_amy_and_may");
        Assert.Equal(1, Count(map, terms).Trainers);
    }

    [Fact]
    public void ABallBehindACutTreeCountsOnlyOnceCutCanBeUsed()
    {
        var map = Field();
        map.Add(Ball(9, 2, "FLAG_TEST_FAR_BALL"));
        var story = new StoryState();
        Assert.Equal(0, Count(map, new ChecklistTerms { Story = story }).Items);
        var cutting = new ChecklistTerms { Story = story, Clears = new HashSet<PropType> { PropType.CutTree } };
        Assert.Equal(1, Count(map, cutting).Items);
        // A rock smashed would open nothing here
        Assert.Equal(0, Count(map, new ChecklistTerms { Story = story, Clears = new HashSet<PropType> { PropType.CrackedRock } }).Items);
    }

    [Fact]
    public void ABallInTheWayIsPickedUpAndWhatLiesBeyondCounts()
    {
        var map = Field();
        // The gap is a ball now, not a tree: picked up, it is a way through
        map.NPCs.Remove(map.GetNpcAt(6, 5)!);
        map.Add(Ball(6, 5, "FLAG_TEST_GAP_BALL"));
        map.Add(Ball(9, 2, "FLAG_TEST_FAR_BALL"));
        Assert.Equal(2, Count(map, new ChecklistTerms { Story = new StoryState() }).Items);
    }

    [Fact]
    public void TheWatersSpeciesCountOnlyWithSurfAndARodsOnlyWithTheRod()
    {
        var map = Field();
        var area = new MapArea { Key = "checklist_field", DisplayName = "Field", Open = true };
        area.WildEncounters.Add(new WildEncounterEntry { SpeciesName = "Bidoof" });
        area.WaterEncounters.Add(new WildEncounterEntry { SpeciesName = "Psyduck" });
        area.RodEncounters[(int)FishingRod.Old].Add(new WildEncounterEntry { SpeciesName = "Magikarp" });
        area.RodEncounters[(int)FishingRod.Super].Add(new WildEncounterEntry { SpeciesName = "Gyarados" });
        map.SetArea(0, 0, area);
        var story = new StoryState();

        Assert.Equal(1, Count(map, new ChecklistTerms { Story = story }).Species);
        Assert.Equal(2, Count(map, new ChecklistTerms { Story = story, Moves = FieldMoves.Surf }).Species);
        Assert.Equal(2, Count(map, new ChecklistTerms { Story = story, Rods = new HashSet<FishingRod> { FishingRod.Old } }).Species);
        Assert.Equal(4, Count(map, new ChecklistTerms
        {
            Story = story, Moves = FieldMoves.Surf, Rods = new HashSet<FishingRod> { FishingRod.Old, FishingRod.Super }
        }).Species);

        // A species caught is no longer left
        int bidoof = PokemonDatabase.Get("Bidoof")!.DexNumber;
        Assert.Equal(0, Count(map, new ChecklistTerms { Story = story, Caught = n => n == bidoof }).Species);
    }

    [Fact]
    public void SomeoneHiddenOrNotYetShownIsNotCounted()
    {
        var map = Field();
        var waiting = Trainer(3, 3, "youngster_later");
        waiting.ShownBy = "FLAG_TEST_SHOWN";
        map.Add(waiting);
        var story = new StoryState();
        var terms = new ChecklistTerms { Story = story };
        Assert.Equal(0, Count(map, terms).Trainers);
        story.Set("FLAG_TEST_SHOWN");
        Assert.Equal(1, Count(map, terms).Trainers);
    }

    [Fact]
    public void TheCountsAreSaidAsOneLineAndNothingLeftSaysSo()
    {
        Assert.Equal("Items 2 · Trainers 3 · Pokémon 5", new AreaCounts(2, 3, 5).Line);
        Assert.Equal("Nothing left here", new AreaCounts(0, 0, 0).Line);
    }

    [Fact]
    public void WithTheRowOffTheSignIsTodays()
    {
        Assert.False(new GameSettings().AreaChecklist);
        var sign = new LocationSign();
        sign.Show("Route 202");
        Assert.Null(sign.Counts);
        Assert.Null(sign.SecondLine);
    }

    /// <summary>
    /// Done when (plan 12 · Q22): arriving on Route 202 at the start of the game tells how many items, trainers and species are
    /// left there, and each number falls as one is taken, beaten or caught.
    /// </summary>
    [Fact]
    public void Route202HasSomethingOfEachLeftAndEachFalls()
    {
        var map = MapDatabase.Get("Sinnoh");
        var b = map.AreaBounds("route_202")!.Value;
        var area = map.FindArea("route_202")!;
        // In from Sandgem Town's side: the lowest open tile of the route, nearest its middle
        (int X, int Y) start = (-1, -1);
        for (int y = b.Y + b.Height - 1; y >= b.Y && start.X < 0; y--)
            for (int dx = 0; dx < b.Width && start.X < 0; dx++)
                foreach (int x in new[] { b.X + b.Width / 2 + dx, b.X + b.Width / 2 - dx })
                    if (map.InBounds(x, y) && map.AreaAt(x, y) == area && !map.IsSolid(x, y) && map.GetNpcAt(x, y) == null
                        && FieldMovement.Step(map, x, y, Direction.Up, new Walker(TravelMode.OnFoot, map.HeightAt(x, y))).Moves)
                    { start = (x, y); break; }
        Assert.True(start.X >= 0);

        var story = new StoryState();
        StoryMigration.BeginNewGame(story, ScriptLibrary.Default);
        var caught = new HashSet<int>();
        var terms = new ChecklistTerms { Story = story, Caught = caught.Contains };
        AreaCounts Now() => AreaChecklist.Count(map, start.X, start.Y, map.HeightAt(start.X, start.Y), TravelMode.OnFoot, terms);

        var first = Now();
        Assert.True(first.Items > 0, $"items {first.Items}");
        Assert.True(first.Trainers > 0, $"trainers {first.Trainers}");
        Assert.True(first.Species > 0, $"species {first.Species}");

        // A trainer beaten
        var (reached, faced) = AreaChecklist.Reach(map, start.X, start.Y, map.HeightAt(start.X, start.Y), TravelMode.OnFoot, terms);
        var trainer = map.Everyone.First(n => n.IsTrainer && n.TrainerData != null && map.AreaAt(n.GridX, n.GridY) == area
            && AreaChecklist.Present(n, story) && (reached.Contains((n.GridX, n.GridY)) || faced.Contains((n.GridX, n.GridY))));
        story.Defeat(trainer.TrainerData!.Id);
        Assert.Equal(first.Trainers - 1, Now().Trainers);

        // An item taken: a ball's flag, or a hidden item's
        var ball = map.Everyone.FirstOrDefault(n => n.IsItemBall && n.HiddenBy != null && map.AreaAt(n.GridX, n.GridY) == area
            && AreaChecklist.Present(n, story) && (reached.Contains((n.GridX, n.GridY)) || faced.Contains((n.GridX, n.GridY))));
        string flag = ball?.HiddenBy ?? map.HiddenItems.First(h => map.AreaAt(h.Key.X, h.Key.Y) == area
            && !story.Has(h.Value.Flag) && (reached.Contains(h.Key) || faced.Contains(h.Key))).Value.Flag;
        story.Set(flag);
        Assert.Equal(first.Items - 1, Now().Items);

        // A species caught
        var species = area.WildEncounters.Select(e => PokemonDatabase.Get(e.SpeciesName)!.DexNumber).First();
        caught.Add(species);
        Assert.Equal(first.Species - 1, Now().Species);
    }
}
