using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Footprints (plan 09 · L9; style guide, "Menu screens (G10)", the Pokédex): every walking species' print made from
/// its own model, none for those that don't walk, the Pokédex asking for one only on an opened entry, and Dr.
/// Footstep's question of the lead's friendship.
/// </summary>
public class FootprintTests
{
    public static IEnumerable<object[]> HandBuiltSpecies => PokemonModels.Species.Select(s => new object[] { s });

    [Theory]
    [MemberData(nameof(HandBuiltSpecies))]
    public void EveryWalkerLeavesAPrintAndEveryFishAndFloaterNone(string species)
    {
        var model = PokemonModels.Build(species);
        var print = Footprints.Make(model);
        if (model.Plan is BodyPlan.Biped or BodyPlan.Quadruped or BodyPlan.Bird)
        {
            Assert.NotNull(print);
            Assert.True(print!.Count >= Footprints.Least, $"{species} ({model.Plan}) leaves a print of {print.Count} texels");
            // The same model gives the same print
            Assert.Equal(print.Encode(), Footprints.Make(model)!.Encode());
        }
        else if (model.Plan is BodyPlan.Fish or BodyPlan.Floating)
            Assert.Null(print);
    }

    [Fact]
    public void AModelBuiltTwiceLeavesTheSamePrint()
    {
        foreach (var species in new[] { "Turtwig", "Starly", "Machop", "Shinx" })
        {
            var first = Footprints.Make(PokemonModels.Build(species));
            var second = Footprints.Make(PokemonModels.Build(species));
            Assert.NotNull(first);
            Assert.Equal(first!.Encode(), second!.Encode());
        }
    }

    [Fact]
    public void TheSamplesOfTheBodiesThatDontWalkLeaveNone()
    {
        Assert.Null(Footprints.Make(PokemonModels.Sample(BodyPlan.Fish)));
        Assert.Null(Footprints.Make(PokemonModels.Sample(BodyPlan.Floating)));
        Assert.Null(Footprints.Make(PokemonModels.Sample(BodyPlan.Serpent)));
        Assert.True(Footprints.Make(PokemonModels.Sample(BodyPlan.Quadruped))!.Count >= Footprints.Least);
    }

    [Fact]
    public void APrintIsKeptAsItsOwnTextAndReadBack()
    {
        var paw = Footprints.Paw();
        Assert.True(paw.Count >= Footprints.Least);
        var back = Footprint.Decode(paw.Encode());
        Assert.Equal(paw, back);
        Assert.Null(Footprint.Decode("none"));
        Assert.Null(Footprint.Decode(new string('#', Footprint.Size)));
        // The picture is white where inked and clear elsewhere
        var canvas = paw.ToCanvas();
        for (int y = 0; y < Footprint.Size; y++)
            for (int x = 0; x < Footprint.Size; x++)
                Assert.Equal(paw[x, y], canvas.Get(x, y).A > 0);
    }

    [Fact]
    public void AFootIsSeenToesUp()
    {
        // A quadruped's front left foot: its print is no wider than the square and lies inside its margin
        var print = Footprints.Make(PokemonModels.Build("Shinx"))!;
        for (int y = 0; y < Footprint.Size; y++)
            for (int x = 0; x < Footprint.Size; x++)
                if (print[x, y]) Assert.InRange(x, 1, Footprint.Size - 2);
    }

    /// <summary>
    /// The opened entry asks for a print (which may build a model in the background); the list and the INFO page
    /// beside it only show one already made.
    /// </summary>
    [Fact]
    public void OnlyTheOpenedEntryAsksForAPrint()
    {
        string engine = Path.Combine(Repository.Root, "PokemonPlatinumEngine");
        var asks = new Regex(@"Footprints\.Icon\(");
        var shows = new Regex(@"Footprints\.HeldIcon\(");
        var method = new Regex(@"static\s+[\w<>\[\]?,. ]+\s+(\w+)\s*\(");
        var asked = new List<string>();
        var shown = new List<string>();
        foreach (var file in Directory.GetFiles(engine, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")) continue;
            string name = Path.GetFileName(file);
            if (name == "Footprints.cs") continue;
            string? inMethod = null;
            foreach (var line in File.ReadLines(file))
            {
                var m = method.Match(line);
                if (m.Success) inMethod = m.Groups[1].Value;
                if (asks.IsMatch(line))
                {
                    asked.Add($"{name}:{inMethod}");
                    // ...and only where the entry is open on its own
                    Assert.Contains("opening >= 0", line);
                }
                if (shows.IsMatch(line)) shown.Add($"{name}:{inMethod}");
            }
        }
        Assert.Equal(new[] { "ModernUi.Pokedex.cs:DexInfo" }, asked);
        Assert.Contains("ModernUi.Pokedex.cs:DexList", shown);
        Assert.All(shown, s => Assert.StartsWith("ModernUi.Pokedex.cs:", s));
    }

    // ------------------------------------------------------------------ Dr. Footstep's question

    private static Pokemon Mon(string species = "Turtwig") => new(PokemonDatabase.Get(species)!, 20, Gender.Male, Nature.Hardy, false);

    [Fact]
    public void AnIfAsksTheFriendshipOfTheFirstThatIsNoEgg()
    {
        var host = new HeadlessScriptHost();
        var script = ScriptParser.Parse("test", """
            script Asks
              if friendship >= 200 setflag FLAG_CLOSE
              if friendship < 70 setflag FLAG_UNSURE
              end
            """)[0];
        void Run()
        {
            host.Story.Unset("FLAG_CLOSE");
            host.Story.Unset("FLAG_UNSURE");
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(script, null);
            runner.RunToEnd();
        }
        // Nobody: neither holds
        Run();
        Assert.False(host.Story.Has("FLAG_CLOSE"));
        Assert.False(host.Story.Has("FLAG_UNSURE"));
        // An Egg first is passed over for the Pokémon behind it
        var egg = Mon();
        egg.IsEgg = true;
        egg.Friendship = 0;
        var lead = Mon();
        lead.Friendship = 220;
        host.Party.Add(egg);
        host.Party.Add(lead);
        Run();
        Assert.True(host.Story.Has("FLAG_CLOSE"));
        Assert.False(host.Story.Has("FLAG_UNSURE"));
        lead.Friendship = 10;
        Run();
        Assert.False(host.Story.Has("FLAG_CLOSE"));
        Assert.True(host.Story.Has("FLAG_UNSURE"));
    }

    [Theory]
    [InlineData(0, "make of this Trainer yet")]
    [InlineData(70, "all right, I suppose")]
    [InlineData(150, "a good, steady bond")]
    [InlineData(199, "a good, steady bond")]
    [InlineData(200, "close indeed")]
    [InlineData(255, "seldom read a heart so open")]
    public void DrFootstepReadsTheStepsOfFriendship(int friendship, string heard)
    {
        var host = new HeadlessScriptHost();
        var lead = Mon();
        lead.Friendship = friendship;
        host.Party.Add(lead);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("DrFootstep", "FootstepHouse")!, null);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.Contains(host.Transcript, line => line.Text.Contains(heard, StringComparison.OrdinalIgnoreCase));
    }
}
