using System.Text.Json;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>The imported world's trainers are the table's (plan 08 · P7): an overlay gives their lines and nothing else.</summary>
public class OverlayTrainerTests
{
    private static readonly Lazy<World> Sinnoh = new(() => World.LoadAll().Single(w => w.Index.Region == "Sinnoh"));

    /// <summary>The table is the only copy of who a trainer is: no overlay writes a team, a class, a name or an id beside it.</summary>
    [Fact]
    public void NoOverlayCarriesWhatTheTableHas()
    {
        string folder = GameDataFiles.PathOf(Path.Combine("world", "sinnoh", "overlays"));
        int trainers = 0;
        foreach (string path in Directory.GetFiles(folder, "*.json"))
        {
            string key = Path.GetFileNameWithoutExtension(path);
            string text = Regex.Replace(File.ReadAllText(path), @"^\s*//.*$", "", RegexOptions.Multiline);
            using var doc = JsonDocument.Parse(text, new JsonDocumentOptions { AllowTrailingCommas = true });
            if (!doc.RootElement.TryGetProperty("people", out var people)) continue;
            var objects = Sinnoh.Value.Area(key)?.Objects.ToDictionary(o => o.Id) ?? new();
            foreach (var person in people.EnumerateObject())
            {
                if (!person.Value.TryGetProperty("trainer", out var trainer)) continue;
                trainers++;
                foreach (var field in trainer.EnumerateObject())
                {
                    if (field.Name is "dialogueBefore" or "dialogueAfter") continue;
                    // An id only for someone whose object names no trainer of its own, whom a script battles
                    bool scripted = field.Name == "id" && TrainerDatabase.Get(objects[person.Name].Script) == null;
                    Assert.True(scripted, $"{key}: {person.Name}'s trainer block carries '{field.Name}', which is the table's");
                }
            }
        }
        Assert.True(trainers >= 241, $"{trainers} trainers in the overlays");
    }

    /// <summary>Every trainer standing in the world is the table's, with its id, class, name, team, items and moves.</summary>
    [Fact]
    public void EveryTrainerOfTheWorldIsTheTables()
    {
        MapDatabase.Initialize();
        int seen = 0;
        foreach (var map in MapDatabase.MapNames.Select(MapDatabase.Get).Where(m => m.IsStreamed))
        {
            foreach (var npc in map.Everyone.Where(n => n.IsTrainer))
            {
                var t = npc.TrainerData!;
                var record = TrainerDatabase.Get(t.Id);
                Assert.True(record != null, $"{map.Name}: {npc.Name}'s trainer '{t.Id}' is not the table's");
                Assert.Equal(record!.Class, t.TrainerClass);
                Assert.Equal(record.Class == TrainerDatabase.RivalClass ? "{rival}" : record.Name, t.Name);
                Assert.Equal(record.Items, t.Items);
                Assert.Equal(record.Party.Select(p => (p.Species, p.Level)), t.Party.Members.Select(p => (p.Species.Name, p.Level)));
                foreach (var (mine, theirs) in t.Party.Members.Zip(record.Party))
                    if (theirs.Moves is { Count: > 0 } chosen) Assert.Equal(chosen, mine.Moves.Select(m => m.Name));
                Assert.True(t.FromPlatinum);
                seen++;
            }
        }
        Assert.True(seen >= 241, $"{seen} trainers in the world");
    }

    /// <summary>A save that beat Tristan under the id the south-west's overlay once made up has beaten the table's Tristan.</summary>
    [Fact]
    public void AnOlderSaveKeepsTheTrainersItBeat()
    {
        var story = new StoryState();
        story.Defeat("trainer_tristan");
        story.Defeat("trainer_liv_and_liz");
        StoryMigration.Upgrade(story, 5, Array.Empty<Pokemon>(), ScriptLibrary.Default, new Inventory());
        Assert.True(story.HasDefeated("youngster_tristan"));
        Assert.True(story.HasDefeated("twins_liv_and_liz"));
        Assert.False(story.HasDefeated("lass_natalie"));

        // A save of today's version is left alone
        var today = new StoryState();
        today.Defeat("trainer_tristan");
        StoryMigration.Upgrade(today, StoryState.CurrentVersion, Array.Empty<Pokemon>(), ScriptLibrary.Default, new Inventory());
        Assert.False(today.HasDefeated("youngster_tristan"));
    }

    /// <summary>A hand-made map may still type a team of its own for one of the table's trainers, and it wins.</summary>
    [Fact]
    public void AHandTypedTeamOnAHandMadeMapStillWins()
    {
        var npc = MapFile.BuildNpc(new MapFile.NpcRecord
        {
            Name = "Tristan", X = 1, Y = 1,
            Trainer = new MapFile.TrainerRecord
            {
                Id = "youngster_tristan", Name = "Tristan", TrainerClass = "Youngster", PrizeMoney = 123,
                Party = { new MapFile.PartyMember { Species = "Bidoof", Level = 9 } }
            }
        }, "T");
        Assert.Equal(("Bidoof", 9), (npc.TrainerData!.Party.Members.Single().Species.Name, npc.TrainerData.Party.Members[0].Level));
        Assert.Equal(123, npc.TrainerData.PrizeMoney);
        Assert.False(npc.TrainerData.FromPlatinum);
    }
}
