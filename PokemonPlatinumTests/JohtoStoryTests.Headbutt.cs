using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>Plan 38 · EX4 in a game of Johto: Ilex Forest's man teaches Headbutt once, and a tree is shaken with it.</summary>
public partial class JohtoStoryTests
{
    [Fact]
    public void IlexForestsManTeachesHeadbuttOnceToAPokemonThatCanLearnIt()
    {
        var game = new Game();
        // Zen Headbutt's kin: a Slowpoke may learn it, by Platinum's tutor list or its own
        game.Party.Members.Clear();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Slowpoke")!, 20));
        Assert.Equal(TutorAnswer.Able, MoveTutors.ForKanto(game.Party.Members[0], "Headbutt"));
        game.Arrive("IlexForest", 36, 38);
        game.Answers.Enqueue(0);
        var taught = game.Talk("d36r0101_gsbigman");
        Assert.True(game.Party.Members[0].Knows("Headbutt"));
        Assert.True(game.Story.Has("FLAG_TUTOR_HEADBUTT"));
        Assert.Contains(taught.Transcript, l => l.Text.Contains("learned Headbutt"));
        // Once only: spoken to again, nobody is asked for
        var again = game.Talk("d36r0101_gsbigman");
        Assert.Empty(again.Asked);
    }

    [Fact]
    public void AHeadbuttTreeOnRouteTwentyNineIsShakenAndWhatFallsIsBattled()
    {
        var overworld = Johto.Value.BuildMaps().Single(m => m.Name == "Johto");
        var route = overworld.Areas.Single(a => a.Key == "route_29");
        Assert.NotNull(route.Headbutt);
        var (tx, ty) = route.Headbutt!.Tiles.Keys.First();
        Assert.Equal(TileBehavior.HeadbuttTree, overworld.BehaviourAt(tx, ty));

        var game = new Game();
        game.Party.Members.Clear();
        var knower = new Pokemon(PokemonDatabase.Get("Slowpoke")!, 20);
        knower.Moves.Clear();
        knower.Moves.Add(new Move(MoveDatabase.Get("Headbutt")!));
        game.Party.Add(knower);
        game.Arrive("Johto", tx, ty + 1);
        var host = new HeadlessScriptHost(game.Story, game.Party, game.Bag) { Map = game.Map, PlayerTile = game.Tile, Headbutted = ("Hoothoot", 3) };
        host.Answers.Enqueue(0);
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find("HeadbuttTree", "common")!);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.Contains(host.Log, l => l.StartsWith("usemove Headbutt"));
        Assert.Contains(host.Log, l => l.StartsWith("wildbattle Hoothoot 3"));

        // Nothing falls: the tree only shakes
        var empty = new HeadlessScriptHost(game.Story, game.Party, game.Bag) { Map = game.Map, PlayerTile = game.Tile };
        empty.Answers.Enqueue(0);
        runner = new ScriptRunner(Scripts, empty);
        runner.Start(Scripts.Find("HeadbuttTree", "common")!);
        runner.RunToEnd();
        Assert.DoesNotContain(empty.Log, l => l.StartsWith("wildbattle"));
        Assert.Contains(empty.Transcript, l => l.Text.Contains("nothing came down"));

        // A team that doesn't know it is asked nothing
        knower.Moves.Clear();
        knower.Moves.Add(new Move(MoveDatabase.Get("Tackle")!));
        var none = new HeadlessScriptHost(game.Story, game.Party, game.Bag) { Map = game.Map, PlayerTile = game.Tile };
        runner = new ScriptRunner(Scripts, none);
        runner.Start(Scripts.Find("HeadbuttTree", "common")!);
        runner.RunToEnd();
        Assert.Empty(none.Asked);
        Assert.Empty(none.Transcript);
    }
}
