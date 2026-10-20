using System;
using System.IO;
using System.Linq;
using System.Text;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using Xunit;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// The Vs. Recorder's file (plan 08 · P10; design "Recorded battles", risk "A replay goes stale"): a battle written
/// down as its two sides, its trainers, its rules and conditions, its seed and its answers, read back, and fought again
/// with no screen to the same lines and the same end; a file of other data refused rather than played wrong.
/// </summary>
public class BattleRecordFileTests
{
    private static readonly DateTime Day = new(2026, 10, 20);

    // A trainer's battle of two Pokémon a side, the game's own (its screen, its menus) with nothing fixed but the seed:
    // the trainer thinks with its own flags, the player answers with moves and a switch
    private static (BattleEngine Battle, Party Mine) Fight(uint seed = 20261020)
    {
        var mine = new Party();
        mine.Add(Mon("Monferno", 20, "Ember", "Mach Punch", "Taunt"));
        mine.Add(Mon("Staravia", 18, "Wing Attack", "Quick Attack"));
        var trainer = new Trainer { Id = "record_test", Name = "Tristan", TrainerClass = "Youngster", PrizeMoney = 64, Ai = AiFlags.Basic };
        trainer.Party.Add(Mon("Shinx", 17, "Tackle", "Leer", "Spark"));
        trainer.Party.Add(Mon("Budew", 18, "Absorb", "Stun Spore", "Mega Drain"));
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = mine, Inventory = new Inventory(), Pokedex = new Pokedex(), Trainers = { trainer },
            Random = new BattleRandom(seed), Rules = Ruleset.Platinum, PlayerName = "Lucas"
        });
        Settle(battle);
        for (int turn = 0; turn < 60 && !battle.IsBattleOver; turn++)
        {
            int bench = Enumerable.Range(0, mine.Count).FirstOrDefault(i => !mine.Members[i].IsFainted && battle.PlayerSlots.All(b => b.Pokemon != mine.Members[i]), -1);
            if (battle.IsChoosingReplacement) battle.SelectSwitch(bench);
            else if (turn == 1 && bench >= 0) battle.SelectSwitch(bench);
            else battle.SelectMove(0);
            Settle(battle);
        }
        Assert.True(battle.IsBattleOver);
        return (battle, mine);
    }

    [Fact]
    public void AFileReadsBackAsItWasWritten()
    {
        var (battle, _) = Fight();
        var file = battle.RecordFile(Day)!;
        Assert.NotNull(file);
        Assert.Equal(BattleRecordFile.CurrentFileVersion, file.FileVersion);
        Assert.Equal(GameDataFiles.Version, file.DataVersion);
        Assert.Equal(RulesPreset.Platinum, file.Rules);
        Assert.Equal(("Lucas", Day), (file.PlayerName, file.Day!.Value));
        Assert.Equal(new[] { "Monferno", "Staravia" }, file.Player.Select(p => p.Pokemon.SpeciesName));
        Assert.Equal(new[] { "Shinx", "Budew" }, file.Trainers.Single().Party.Select(p => p.Pokemon.SpeciesName));
        Assert.Equal((battle.Result, battle.Core.Turn), (file.Result, file.Turns));
        Assert.Equal(battle.Core.Record!.Seed, file.Seed);
        Assert.Equal(battle.Core.Record!.Answers.SelectMany(a => a), file.Record.Answers.SelectMany(a => a));

        // Written and read back, it is the same file to the character
        string text = file.Serialize();
        var reading = BattleRecordFile.Read(text);
        Assert.Equal(ReplayRefusal.None, reading.Refused);
        Assert.Equal(text, reading.File!.Serialize());

        // The file keeps the battle as it began: the player's Pokémon at their full HP, not as the battle left them
        Assert.All(file.Player, p => Assert.Equal(p.Pokemon.ToPokemon(Ruleset.Platinum).MaxHP, p.Pokemon.ToPokemon(Ruleset.Platinum).CurrentHP));
    }

    [Fact]
    public void ABattleReplayedFromItsFileEndsTheSameWithTheSameLines()
    {
        var (battle, _) = Fight();
        var file = battle.RecordFile(Day)!;

        // From the file in memory
        var again = file.Replay();
        Assert.Equal(battle.Result, again.Result);
        Assert.Equal(Golden.BattleText.Written(battle.Core), Golden.BattleText.Written(again));
        Assert.Null(again.Request);

        // And from the disk, under the name the game gives it, in a folder of its own
        string folder = Path.Combine(Path.GetTempPath(), "replays-" + Guid.NewGuid().ToString("N"));
        try
        {
            string path = file.WriteTo(folder);
            Assert.Equal("20261020-YoungsterTristan.json", Path.GetFileName(path));
            Assert.False(File.Exists(path + ".tmp"));
            var reading = BattleRecordFile.ReadFile(path);
            Assert.Equal(ReplayRefusal.None, reading.Refused);
            var fromDisk = reading.File!.Replay();
            Assert.Equal(battle.Result, fromDisk.Result);
            Assert.Equal(Golden.BattleText.Written(battle.Core), Golden.BattleText.Written(fromDisk));

            // A second battle against the same trainer the same day is kept beside the first, not over it
            string second = file.WriteTo(folder);
            Assert.Equal("20261020-YoungsterTristan-2.json", Path.GetFileName(second));
            Assert.True(File.Exists(path));
        }
        finally
        {
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    [Fact]
    public void AFileOfAnotherDataVersionIsRefused()
    {
        var (battle, _) = Fight();
        var file = battle.RecordFile(Day)!;
        string text = file.Serialize();

        // Read under other data, it is refused, never played
        var reading = BattleRecordFile.Read(text, "0123456789abcdef");
        Assert.Null(reading.File);
        Assert.Equal(ReplayRefusal.OtherDataVersion, reading.Refused);

        // And so is a file whose own version is another, under the game's data
        file.DataVersion = "0123456789abcdef";
        Assert.Equal(ReplayRefusal.OtherDataVersion, BattleRecordFile.Read(file.Serialize()).Refused);
        Assert.False(string.IsNullOrWhiteSpace(BattleRecordFile.Notice(ReplayRefusal.OtherDataVersion)));
    }

    [Fact]
    public void ADamagedFileOrOneOfANewerGameIsRefused()
    {
        var (battle, _) = Fight();
        var file = battle.RecordFile(Day)!;

        Assert.Equal(ReplayRefusal.Damaged, BattleRecordFile.Read("{ not a battle").Refused);
        Assert.Equal(ReplayRefusal.Damaged, BattleRecordFile.Read("{}").Refused);
        file.FileVersion = BattleRecordFile.CurrentFileVersion + 1;
        Assert.Equal(ReplayRefusal.OtherFileVersion, BattleRecordFile.Read(file.Serialize()).Refused);
        Assert.Equal(ReplayRefusal.Damaged, BattleRecordFile.ReadFile(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json")).Refused);
        foreach (var refused in new[] { ReplayRefusal.Damaged, ReplayRefusal.OtherFileVersion })
            Assert.False(string.IsNullOrWhiteSpace(BattleRecordFile.Notice(refused)));
    }

    [Fact]
    public void ABattleWhoseChanceIsNoBattleRandomIsNotRecorded()
    {
        // A test's own generator can't be seeded again, so nothing would come out the same
        var mine = new Party();
        mine.Add(Mon("Monferno", 20, "Ember"));
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = mine, Inventory = new Inventory(), Pokedex = new Pokedex(), WildPokemon = { Mon("Bidoof", 3, "Tackle") },
            Random = new Random(5), Rules = Ruleset.Platinum
        });
        Assert.Null(battle.RecordFile(Day));
    }

    [Fact]
    public void TheDataVersionIsAHashOfTheDataFiles()
    {
        byte[] a = Encoding.UTF8.GetBytes("{\n  \"name\": \"Bulbasaur\"\n}\n"), b = Encoding.UTF8.GetBytes("[]\n");
        string version = GameDataFiles.VersionOf(new[] { a, b });
        Assert.Matches("^[0-9a-f]{16}$", version);
        Assert.Equal(version, GameDataFiles.VersionOf(new[] { a, b }));
        // Windows' line endings are the same data
        Assert.Equal(version, GameDataFiles.VersionOf(new[] { Encoding.UTF8.GetBytes("{\r\n  \"name\": \"Bulbasaur\"\r\n}\r\n"), b }));
        // A letter changed, the files in another order, or one file's end moved into the next, is another version
        Assert.NotEqual(version, GameDataFiles.VersionOf(new[] { Encoding.UTF8.GetBytes("{\n  \"name\": \"Bulbasaun\"\n}\n"), b }));
        Assert.NotEqual(version, GameDataFiles.VersionOf(new[] { b, a }));
        Assert.NotEqual(GameDataFiles.VersionOf(new[] { new byte[] { 1, 2 }, new byte[] { 3 } }), GameDataFiles.VersionOf(new[] { new byte[] { 1 }, new byte[] { 2, 3 } }));

        // The game's own is of its five files of rules, and the same every time it is asked
        Assert.Matches("^[0-9a-f]{16}$", GameDataFiles.Version);
        Assert.Equal(GameDataFiles.VersionOf(GameDataFiles.VersionedFiles.Select(n => File.ReadAllBytes(GameDataFiles.PathOf(n)))), GameDataFiles.Version);
    }

    [Fact]
    public void ARecordingIsNamedByTheDayAndTheFoe()
    {
        Assert.Equal("20261020-YoungsterTristan.json", BattleRecordFile.NameFor(Day, "Youngster Tristan", _ => false));
        Assert.Equal("20261020-PokemonTrainerCheryl.json", BattleRecordFile.NameFor(Day, "Pokémon Trainer Cheryl", _ => false));
        Assert.Equal("20261020-YoungsterTristan-3.json",
            BattleRecordFile.NameFor(Day, "Youngster Tristan", n => n is "20261020-YoungsterTristan.json" or "20261020-YoungsterTristan-2.json"));
        Assert.Equal("20261020-Battle.json", BattleRecordFile.NameFor(Day, "", _ => false));
        // The folder is beside the saves, never the working directory
        var paths = SavePaths.In(Path.Combine(Path.GetTempPath(), "somewhere"));
        Assert.Equal(Path.Combine(paths.Folder, "replays"), paths.Replays);
    }
}
