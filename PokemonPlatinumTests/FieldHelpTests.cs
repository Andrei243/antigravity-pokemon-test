using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 12 · Q18: field moves without a party member who knows them. The grants are worked out from the row, the rules,
/// the Badge and the HM in the bag, never stored; the obstacles' scripts ask them beside <c>knows</c>.
/// </summary>
[Collection("Shared clocks and dice")]
public class FieldHelpTests
{
    private static Pokemon Knowing(params string[] moves)
    {
        var pokemon = new Pokemon(PokemonDatabase.Get("Bibarel")!, 30);
        pokemon.Moves.Clear();
        foreach (string move in moves) pokemon.Moves.Add(new Move(MoveDatabase.Get(move)!));
        return pokemon;
    }

    private static void GiveHm(HeadlessScriptHost host, FieldMove move) => host.Bag.AddItem(FieldHelp.HmOf(move)!);

    private static (HeadlessScriptHost Host, Map Map, NPC Thing) Face(PropType kind, RulesDefault row, string? badge, bool hm, FieldMove move)
    {
        var map = new Map(5, 5) { Name = "Obstacle" };
        var thing = map.AddObstacle(kind, 2, 1);
        thing.HiddenBy = "FLAG_MAP_LOCAL_HIDE_OBSTACLE_1_TEST";
        var host = new HeadlessScriptHost { Map = map, PlayerTile = (2, 2), PlayerFacing = Direction.Up, FieldHelpers = row };
        host.Party.Add(Knowing("Tackle"));
        if (badge is { } b) host.Story.GiveBadge("Sinnoh", b);
        if (hm) GiveHm(host, move);
        host.Answers.Enqueue(0);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.For(thing)!)!, thing);
        runner.RunToEnd();
        map.ApplyPresence(host.Story.Has);
        return (host, map, thing);
    }

    [Theory]
    [InlineData(PropType.CutTree, FieldMove.Cut, "Forest")]
    [InlineData(PropType.CrackedRock, FieldMove.RockSmash, "Coal")]
    public void WithTheRowOffEveryObstacleAnswersAsItDid(PropType kind, FieldMove move, string badge)
    {
        var (host, map, thing) = Face(kind, RulesDefault.Off, badge, hm: true, move);
        Assert.Empty(host.Asked);
        Assert.Contains(thing, map.NPCs);
        Assert.DoesNotContain(host.Log, l => l.StartsWith("usemove"));
    }

    [Theory]
    [InlineData(PropType.CutTree, FieldMove.Cut, "Forest")]
    [InlineData(PropType.CrackedRock, FieldMove.RockSmash, "Coal")]
    public void ATreeFallsToATeamWithNoKnowerOnceTheBadgeAndTheHmAreBothHeld(PropType kind, FieldMove move, string badge)
    {
        // Neither alone is enough
        foreach (var (has, hm) in new[] { ((string?)badge, false), ((string?)null, true) })
        {
            var (h, m, t) = Face(kind, RulesDefault.On, has, hm, move);
            Assert.Empty(h.Asked);
            Assert.Contains(t, m.NPCs);
        }

        var (host, map, thing) = Face(kind, RulesDefault.On, badge, hm: true, move);
        Assert.Single(host.Asked);
        string name = FieldMoveRules.MoveName(move);
        Assert.Contains(host.Log, l => l.StartsWith($"usemove {name} "));
        Assert.True(host.Story.Has(thing.HiddenBy!));
        Assert.DoesNotContain(thing, map.NPCs);
        // A wild helper did it, in the game's own line
        Assert.Contains(host.Transcript, t => t.Text.StartsWith("A wild ") && t.Text.EndsWith($"used {name}!"));
    }

    [Fact]
    public void ARowOnTheRulesGivesHelpersToTheModernRulesAndNotToPlatinums()
    {
        var story = new StoryState();
        story.GiveBadge("Sinnoh", "Forest");
        var bag = new Inventory();
        bag.AddItem(FieldHelp.HmOf(FieldMove.Cut)!);
        Assert.Equal(new[] { FieldMove.Cut }, FieldHelp.Granted(RulesDefault.Rules, RulesPreset.Modern, story, bag, "Sinnoh"));
        Assert.Empty(FieldHelp.Granted(RulesDefault.Rules, RulesPreset.Platinum, story, bag, "Sinnoh"));
        Assert.Equal(new[] { FieldMove.Cut }, FieldHelp.Granted(RulesDefault.On, RulesPreset.Platinum, story, bag, "Sinnoh"));
        Assert.Empty(FieldHelp.Granted(RulesDefault.Off, RulesPreset.Modern, story, bag, "Sinnoh"));
    }

    [Fact]
    public void FlashTeleportDigAndSweetScentStayWithThePokemonThatKnowThem()
    {
        Assert.DoesNotContain(FieldMove.Dig, FieldHelp.Helped);
        Assert.DoesNotContain(FieldMove.Flash, FieldHelp.Helped);
        Assert.DoesNotContain(FieldMove.Teleport, FieldHelp.Helped);
        Assert.DoesNotContain(FieldMove.SweetScent, FieldHelp.Helped);
        Assert.Equal(8, FieldHelp.Helped.Count);
        foreach (var move in FieldHelp.Helped) Assert.NotNull(FieldHelp.HmOf(move));
    }

    [Fact]
    public void SurfAndWaterfallTakeThePlayerOutAndUpThroughTheMovementRules()
    {
        var story = new StoryState();
        story.GiveBadge("Sinnoh", "Fen");
        story.GiveBadge("Sinnoh", "Beacon");
        var bag = new Inventory();
        bag.AddItem(FieldHelp.HmOf(FieldMove.Surf)!);
        bag.AddItem(FieldHelp.HmOf(FieldMove.Waterfall)!);
        var moves = FieldHelp.Moves(FieldHelp.Granted(RulesDefault.On, RulesPreset.Modern, story, bag, "Sinnoh"));
        Assert.True(moves.HasFlag(FieldMoves.Surf));
        Assert.True(moves.HasFlag(FieldMoves.Waterfall));
        Assert.False(moves.HasFlag(FieldMoves.RockClimb));
    }

    [Fact]
    public void MakingAHelperDrawsNothingFromDiceAndKnowsTheMove()
    {
        Dice.Seed(1234);
        int expected = Dice.New().Next(1000);
        Dice.Seed(1234);
        var helper = FieldHelp.HelperFor(FieldMove.Surf);
        Assert.Equal(expected, Dice.New().Next(1000));
        Assert.Contains(helper.Moves, m => m.Name == "Surf");
        Assert.Equal(FieldHelp.HelperFor(FieldMove.Surf).Personality, helper.Personality);
        Dice.Seed(null);
    }

    [Fact]
    public void ASaveHoldsNothingOfTheGrantsAndTheRowIsKept()
    {
        var settings = new GameSettings();
        Assert.Equal(RulesDefault.Rules, settings.FieldHelpers);
        OptionsScreen.Change(settings, OptionRow.FieldHelpers, 1);
        Assert.Equal(RulesDefault.On, settings.FieldHelpers);
        OptionsScreen.Change(settings, OptionRow.FieldHelpers, 1);
        Assert.Equal(RulesDefault.Off, settings.FieldHelpers);
        // Nothing of the grants is a field of the save
        Assert.DoesNotContain(typeof(SaveData).GetProperties(), p => p.Name.Contains("Granted") || p.Name.Contains("Helper"));
    }
}
