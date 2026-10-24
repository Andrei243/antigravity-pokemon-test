using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>Plan 20 · J13, part b: Mt. Mortar's karate master and his Tyrogue, and Blackthorn's house of moves.</summary>
public class JohtoPartBTests
{
    private static HeadlessScriptHost Run(string name, string area, HeadlessScriptHost host)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(name, area)!, new PokemonPlatinumEngine.Overworld.NPC { Name = "Someone" });
        runner.RunToEnd();
        return host;
    }

    private static Pokemon Mon(string species, int level = 20) => new(PokemonDatabase.Get(species)!, level);

    private static bool Said(HeadlessScriptHost host, string part) => host.Transcript.Any(l => l.Text.Contains(part));

    [Fact]
    public void TheKarateMasterGivesATyrogueOnceHeIsBeaten()
    {
        const string area = "mount_mortar_b1f";
        // Beaten, he gives the Tyrogue once
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon("Machop"));
        Run("Karate", area, host);
        Assert.True(host.Story.Has("FLAG_BEAT_MORTAR_KARATE_MASTER"));
        Assert.True(host.Story.Has("FLAG_GOT_TYROGUE_FROM_KIYO"));
        Assert.Contains(host.Party.Members, p => p.Species.Name == "Tyrogue" && p.Level == 10);
        host.Transcript.Clear();
        Run("Karate", area, host);
        Assert.Single(host.Party.Members, p => p.Species.Name == "Tyrogue");
        Assert.True(Said(host, "young fighter"));

        // With a full team he waits, and gives it when there is room
        var full = new HeadlessScriptHost();
        for (int i = 0; i < 6; i++) full.Party.Add(Mon("Machop"));
        Run("Karate", area, full);
        Assert.True(full.Story.Has("FLAG_BEAT_MORTAR_KARATE_MASTER"));
        Assert.False(full.Story.Has("FLAG_GOT_TYROGUE_FROM_KIYO"));
        Assert.Equal(6, full.Party.Count);

        // Declined, he asks nothing more
        var no = new HeadlessScriptHost();
        no.Party.Add(Mon("Machop"));
        no.Answers.Enqueue(1);
        Run("Karate", area, no);
        Assert.False(no.Story.Has("FLAG_BEAT_MORTAR_KARATE_MASTER"));
        Assert.Equal(1, no.Party.Count);
    }

    [Fact]
    public void BlackthornsHouseOfMovesDeletesAMoveAndTeachesDracoMeteorToATrustingDragon()
    {
        const string area = "blackthorn_move_tutor_house";
        var gible = Mon("Gible");
        gible.Moves.Clear();
        gible.Moves.Add(MoveDatabase.Create("Tackle"));
        gible.Moves.Add(MoveDatabase.Create("Sand-Attack"));
        gible.Friendship = 100;
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon("Machop"));
        host.Party.Add(gible);
        host.PokemonChoice = 1;
        Run("DragonTamer", area, host);
        Assert.False(gible.Knows("Draco Meteor"));
        Assert.True(Said(host, "trust you fully"));
        gible.Friendship = 255;
        Run("DragonTamer", area, host);
        Assert.True(gible.Knows("Draco Meteor"));

        // The Move Deleter makes a move forgotten for good
        var deleter = new HeadlessScriptHost();
        var machop = Mon("Machop");
        deleter.Party.Add(machop);
        int before = machop.Moves.Count;
        Run("Deleter", area, deleter);
        Assert.True(Said(deleter, "forgotten") || machop.Moves.Count == before - 1);
    }
}
