using System;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 13 · V22: teams and trainers as text. A team in Showdown's paste format (held against a paste written here), a
/// trainer file and its code, a changed character or another data version refused, an illegal team refused with its
/// reason, the folder read in the order of its names, and the friends' page that reads and sends them.
/// </summary>
public class FriendsTests
{
    private static BuiltTeam Team() => new()
    {
        Name = "TEST",
        Members =
        {
            new BuiltPokemon { Species = "Garchomp", Level = 50, Nature = Nature.Jolly, Ability = "Sand Veil", Item = "Leftovers", Moves = { "Earthquake", "Dragon Claw", "Fire Fang", "Swords Dance" } },
            new BuiltPokemon { Species = "Lucario", Level = 50, Nature = Nature.Adamant, Ability = "Inner Focus", Moves = { "Close Combat", "Extreme Speed" } }
        }
    };

    private const string Paste =
        "Garchomp @ Leftovers\nAbility: Sand Veil\nLevel: 50\nJolly Nature\n- Earthquake\n- Dragon Claw\n- Fire Fang\n- Swords Dance\n\n" +
        "Lucario\nAbility: Inner Focus\nLevel: 50\nAdamant Nature\n- Close Combat\n- Extreme Speed";

    private sealed class Clipboard : IFriendsHost
    {
        public string? Text { get; set; }
        public string? ReadClipboard() => Text;
        public void WriteClipboard(string text) => Text = text;
    }

    private static string Folder()
    {
        string folder = Path.Combine(Path.GetTempPath(), "friends-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    // ------------------------------------------------------------------ the paste

    [Fact]
    public void ATeamIsWrittenAsTheTestsPasteSays()
    {
        Assert.Equal(Paste, ShowdownPaste.Of(Team()));
    }

    [Fact]
    public void APasteIsReadBackToTheSameTeamAndWrittenTheSame()
    {
        var read = ShowdownPaste.Read(Paste);
        Assert.True(read.Ok);
        Assert.Equal(2, read.Members.Count);
        var garchomp = read.Members[0];
        Assert.Equal(("Garchomp", null, 50, Nature.Jolly, "Sand Veil", "Leftovers"), (garchomp.Species, garchomp.Form, garchomp.Level, garchomp.Nature, garchomp.Ability, garchomp.Item));
        Assert.Equal(new[] { "Earthquake", "Dragon Claw", "Fire Fang", "Swords Dance" }, garchomp.Moves);
        Assert.Equal(Paste, ShowdownPaste.Of(new BuiltTeam { Members = read.Members }));
        Assert.Null(BuiltTeams.WhyNot(new BuiltTeam { Members = read.Members }));
    }

    [Fact]
    public void ShowdownsOwnExportIsReadWhateverItHoldsThatABuiltPokemonHasNot()
    {
        // A nickname, a gender, effort, IVs, a shiny, a header and a Tera Type are checked for their shape and let go
        string exported = "=== [gen9ou] Mine ===\n\nChomper (Garchomp) (F) @ Leftovers\nAbility: Sand Veil\nLevel: 50\nShiny: Yes\nTera Type: Dragon\nEVs: 252 Atk / 4 SpD / 252 Spe\nJolly Nature\nIVs: 0 SpA\n- Earthquake\n- Swords Dance";
        var read = ShowdownPaste.Read(exported);
        Assert.True(read.Ok, string.Join(", ", read.Problems));
        Assert.Equal("Garchomp", read.Members[0].Species);
        Assert.Equal(new[] { "Earthquake", "Swords Dance" }, read.Members[0].Moves);

        // No Level line is Showdown's 100
        Assert.Equal(100, ShowdownPaste.Read("Garchomp\nAdamant Nature\n- Earthquake").Members[0].Level);
    }

    [Fact]
    public void AFormIsReadAndWrittenByShowdownsName()
    {
        Assert.True(ShowdownPaste.Resolve("Rotom-Wash", out var species, out var form));
        Assert.Equal("Rotom", species!.Name);
        Assert.Equal("Rotom-Wash", form!.Name);
        Assert.True(ShowdownPaste.Resolve("Meowstic-F", out _, out var female));
        Assert.Equal("Meowstic-Female", female!.Name);
        var built = new BuiltPokemon { Species = "Rotom", Form = "Rotom-Wash", Moves = { "Hydro Pump" } };
        Assert.StartsWith("Rotom-Wash", ShowdownPaste.Of(built));
        Assert.False(ShowdownPaste.Resolve("Notamon", out _, out _));
    }

    [Fact]
    public void ALineThatCantBeReadIsNamedByNumberAndNeverQuoted()
    {
        var read = ShowdownPaste.Read("Garchomp\nLevel: 500\nJolly Nature\n- Not A Real Move\nWeird: thing\n\nNotamon\n- Tackle");
        Assert.False(read.Ok);
        Assert.Contains(new PasteProblem(2, "paste.badLevel"), read.Problems);
        Assert.Contains(new PasteProblem(4, "paste.unknownMove"), read.Problems);
        Assert.Contains(new PasteProblem(5, "paste.unknownLine"), read.Problems);
        Assert.Contains(new PasteProblem(7, "paste.unknownSpecies"), read.Problems);
        Assert.Empty(read.Members);
        Assert.Contains("paste.empty", ShowdownPaste.Read("  ").Problems.Select(p => p.Key));
        Assert.Contains("paste.tooLong", ShowdownPaste.Read(new string('x', ShowdownPaste.LongestText + 1)).Problems.Select(p => p.Key));
        // Seven Pokémon are one too many
        string seven = string.Join("\n\n", Enumerable.Repeat("Garchomp\nLevel: 50\n- Earthquake", 7));
        Assert.Contains("paste.tooMany", ShowdownPaste.Read(seven).Problems.Select(p => p.Key));
    }

    [Fact]
    public void ATeamFromAPasteIsHeldToLegality()
    {
        var (team, _, illegal) = ShowdownPaste.Team("Pasted", "Garchomp\nLevel: 50\n- Thunderbolt");
        Assert.Null(team);
        Assert.Equal("battleRoom.illegal.move", illegal!.Value.Key);
        var (ok, problems, none) = ShowdownPaste.Team("Pasted", Paste);
        Assert.NotNull(ok);
        Assert.Empty(problems);
        Assert.Null(none);
    }

    // ------------------------------------------------------------------ a trainer file and its code

    private static TrainerFile Friend(string name = "Ash") => TrainerFile.Of(name, PlayerLook.Girl, "Veteran", 3, 7, Team());

    [Fact]
    public void ATrainerFileIsReadBackWhole()
    {
        var file = Friend();
        var (read, refused) = TrainerFile.Read(file.ToText());
        Assert.Null(refused);
        Assert.Equal(("Ash", PlayerLook.Girl, "Veteran", 3, 7), (read!.Name, read.Look, read.Class, read.Before, read.After));
        Assert.Equal(file.Checksum, read.Checksum);
        Assert.Equal(Paste, read.Paste);
        Assert.Equal(2, read.Team()!.Members.Count);
        // The file is plain text of a few lines, in a shape a person can read
        Assert.StartsWith("PPTRAINER 1\nName: Ash\nLook: Girl\nClass: Veteran\nLine: 3\nLine: 7\nData: ", file.ToText());
    }

    [Fact]
    public void ACodeIsReadBackToTheSameFile()
    {
        var file = Friend();
        string code = file.ToCode();
        Assert.True(code.Length < 1500, $"{code.Length} characters");
        Assert.All(code, c => Assert.True(char.IsUpper(c) || char.IsDigit(c) || c is '-' or '\n'));
        var (read, refused) = TrainerFile.ReadCode(code);
        Assert.Null(refused);
        Assert.Equal(file.ToText(), read!.ToText());
        // Lower case, separators and line breaks are let go
        Assert.Null(TrainerFile.ReadCode(code.ToLowerInvariant().Replace("-", " ")).Refused);
    }

    [Fact]
    public void OneChangedCharacterFailsTheChecksum()
    {
        string text = Friend().ToText();
        foreach (var changed in new[] { text.Replace("Name: Ash", "Name: Asi"), text.Replace("Garchomp", "Garchomq"), text.Replace("Jolly", "Jollz"), text.Replace("Line: 3", "Line: 4"), text + "x" })
        {
            var (file, refused) = TrainerFile.Read(changed);
            Assert.Null(file);
            Assert.NotNull(refused);
        }
        Assert.Equal("trainerFile.refused.checksum", TrainerFile.Read(text.Replace("Name: Ash", "Name: Asi")).Refused!.Key);
        // A code with a letter changed is refused too, however it fails
        string code = Friend().ToCode();
        int at = code.IndexOf('-') + 2;
        char other = code[at] == 'A' ? 'B' : 'A';
        var (broken, why) = TrainerFile.ReadCode(code[..at] + other + code[(at + 1)..]);
        Assert.Null(broken);
        Assert.NotNull(why);
    }

    [Fact]
    public void AnotherDataVersionIsRefused()
    {
        var file = Friend();
        file.DataVersion = "0000000000000000";
        file.Checksum = "";
        var (read, refused) = TrainerFile.Read(file.ToText());
        Assert.Equal("trainerFile.refused.version", refused!.Key);
        Assert.Null(TrainerFiles.In(WriteOne(file)).Single().File);
        _ = read;
    }

    private static string WriteOne(TrainerFile file)
    {
        string folder = Folder();
        File.WriteAllText(Path.Combine(folder, "x" + TrainerFile.Ending), file.ToText());
        return folder;
    }

    [Fact]
    public void AnIllegalTeamIsRefusedWithItsReason()
    {
        var team = Team();
        team.Members[0].Moves[0] = "Thunderbolt";
        var (_, refused) = TrainerFile.Read(TrainerFile.Of("Ash", PlayerLook.Boy, "Veteran", 0, 1, team).ToText());
        Assert.Equal("battleRoom.illegal.move", refused!.Key);
        Assert.Equal(LocalNames.Move("Thunderbolt"), refused.Value);

        // A name that is no keyboard's, a class that is not on the list and a line that is not one of ours
        Assert.Equal("trainerFile.refused.name", TrainerFile.Read(TrainerFile.Of("../x", PlayerLook.Boy, "Veteran", 0, 1, Team()).ToText()).Refused!.Key);
        Assert.Equal("trainerFile.refused.class", TrainerFile.Read(TrainerFile.Of("Ash", PlayerLook.Boy, "Rival", 0, 1, Team()).ToText()).Refused!.Key);
        Assert.Equal("trainerFile.refused.line", TrainerFile.Read(TrainerFile.Of("Ash", PlayerLook.Boy, "Veteran", 0, 99, Team()).ToText()).Refused!.Key);
        Assert.Equal("trainerFile.refused.shape", TrainerFile.Read("hello").Refused!.Key);
        Assert.Equal("trainerFile.refused.tooBig", TrainerFile.Read(new string('x', TrainerFile.LongestText + 1)).Refused!.Key);
    }

    [Fact]
    public void EveryClassAndLineAFileMayNameIsThereInTheData()
    {
        var classes = TrainerDatabase.All.Select(t => t.Class).ToHashSet();
        Assert.All(TrainerFile.Classes, c => Assert.Contains(c, classes));
        Assert.All(Enumerable.Range(0, TrainerFile.Phrases), i => Assert.False(string.IsNullOrWhiteSpace(Text.Of($"friends.line.{i}"))));
    }

    [Fact]
    public void FilesAreReadInTheOrderOfTheirNamesAndWhatIsNotATrainersIsLeftAlone()
    {
        string folder = Folder();
        try
        {
            foreach (string name in new[] { "bob", "Cy", "ann" }) TrainerFiles.Write(folder, Friend(name));
            File.WriteAllText(Path.Combine(folder, "notes.txt"), "not a trainer");
            File.WriteAllText(Path.Combine(folder, "broken" + TrainerFile.Ending), "PPTRAINER 1\nnonsense");
            File.WriteAllText(Path.Combine(folder, "huge" + TrainerFile.Ending), new string('x', TrainerFile.LongestText * 5));
            var entries = TrainerFiles.In(folder);
            // Ordinal: capitals first, the same on every machine
            Assert.Equal(new[] { "Cy.trainer.txt", "ann.trainer.txt", "bob.trainer.txt", "broken.trainer.txt", "huge.trainer.txt" }, entries.Select(e => e.FileName));
            Assert.Equal(new[] { true, true, true, false, false }, entries.Select(e => e.File != null));
            Assert.Equal("trainerFile.refused.shape", entries[3].Refused!.Key);
            Assert.Equal("trainerFile.refused.tooBig", entries[4].Refused!.Key);
            Assert.Empty(TrainerFiles.In(Path.Combine(folder, "nowhere")));
        }
        finally { Directory.Delete(folder, true); }
    }

    [Fact]
    public void AFriendsTrainerBattlesAtFiftyWithEveryRoutineOfTheAi()
    {
        var trainer = Friend().ToTrainer();
        Assert.Equal(2, trainer.Party.Members.Count);
        Assert.All(trainer.Party.Members, p => Assert.Equal(TrainerFile.FlatLevel, p.Level));
        Assert.Equal(0, trainer.PrizeMoney);
        Assert.Equal("Veteran", trainer.TrainerClass);
        foreach (var flag in new[] { AiFlags.Basic, AiFlags.EvalAttack, AiFlags.Expert, AiFlags.SetupFirstTurn, AiFlags.Risky, AiFlags.PrioritizeExtremes })
            Assert.True(trainer.Ai.HasFlag(flag), flag.ToString());
    }

    // ------------------------------------------------------------------ the page

    [Fact]
    public void ACodeFromOneMachineIsPastedIntoAnotherAndFought()
    {
        string mine = Folder(), theirs = Folder();
        try
        {
            // The sender exports a built team: a file in their folder and a code on the clipboard
            var clipboard = new Clipboard();
            var sender = new FriendsScreen();
            sender.Open(new BuiltTeamsFile { Teams = { Team() } }, mine, clipboard);
            while (sender.RowAtCursor.Kind != FriendsRowKind.Export) sender.Move(1);
            sender.Confirm();
            Assert.True(sender.Notice!.Value.Good);
            Assert.NotNull(sender.Code);
            Assert.Equal(sender.Code, clipboard.Text);
            Assert.Single(Directory.GetFiles(mine, "*" + TrainerFile.Ending));

            // The receiver reads the code from their clipboard, and it stands in their list, ready
            var receiver = new FriendsScreen();
            receiver.Open(new BuiltTeamsFile(), theirs, clipboard);
            while (receiver.RowAtCursor.Kind != FriendsRowKind.ReadCode) receiver.Move(1);
            receiver.Confirm();
            Assert.True(receiver.Notice!.Value.Good, receiver.Notice.Value.Key);
            Assert.Equal(1, receiver.Files.Count);
            Assert.NotNull(receiver.Files[0].File);
            Assert.Equal(FriendsRowKind.Trainer, receiver.RowAtCursor.Kind);

            // ... and fights it
            receiver.Confirm();
            Assert.False(receiver.IsActive);
            var chosen = receiver.TakeChosen();
            Assert.Equal("TEST", chosen!.Name);
            Assert.Null(receiver.TakeChosen());
            Assert.Equal(2, chosen.ToTrainer().Party.Members.Count);
        }
        finally { Directory.Delete(mine, true); Directory.Delete(theirs, true); }
    }

    [Fact]
    public void ABadCodeOrPasteIsRefusedWithAReasonAndKeepsNothing()
    {
        string folder = Folder();
        try
        {
            var clipboard = new Clipboard { Text = "NOT-A-CODE-AT-ALL!" };
            var page = new FriendsScreen();
            page.Open(new BuiltTeamsFile(), folder, clipboard);
            page.ReadCode(clipboard.Text);
            Assert.False(page.Notice!.Value.Good);
            Assert.Equal("trainerFile.refused.code", page.Notice.Value.Key);
            Assert.Empty(Directory.GetFiles(folder));

            page.PasteTeam("Garchomp\nLevel: 50\n- Not A Real Move");
            Assert.Equal(("paste.unknownMove", "3", false), page.Notice!.Value);
            Assert.False(page.Changed);
            page.PasteTeam("Garchomp\nLevel: 50\n- Thunderbolt");
            Assert.Equal("battleRoom.illegal.move", page.Notice!.Value.Key);
            Assert.Empty(page.Teams.Teams);

            page.PasteTeam(Paste);
            Assert.True(page.Notice!.Value.Good);
            Assert.True(page.Changed);
            Assert.Equal(2, page.Teams.Teams[0].Members.Count);
            // A refused file in the list cannot be fought
            File.WriteAllText(Path.Combine(folder, "bad" + TrainerFile.Ending), "PPTRAINER 1\nnope");
            page.Resume();
            while (page.RowAtCursor.Kind != FriendsRowKind.Trainer) page.Move(1);
            page.Confirm();
            Assert.Null(page.Chosen);
            Assert.True(page.IsActive);
        }
        finally { Directory.Delete(folder, true); }
    }

    [Fact]
    public void TheRoomHasAFriendsRowAndEveryWordTheseScreensAskForIsInBothLanguages()
    {
        Assert.Contains(RoomEntry.Friends, BattleRoomScreen.Entries);
        var room = new BattleRoomScreen();
        room.Open(new[] { Team() }, Array.Empty<PuzzleFile>(), new BattleRoomRecords());
        while (BattleRoomScreen.Entries[room.Cursor] != RoomEntry.Friends) room.Move(1);
        room.Confirm();
        Assert.IsType<OpenFriends>(room.TakeRequest());
        room.PickForFriend(new[] { Team() });
        Assert.Equal(RoomChallenge.Friend, room.Picking);
        room.Confirm();
        var begin = Assert.IsType<BeginRun>(room.TakeRequest());
        Assert.Equal((RoomChallenge.Friend, 0), (begin.Kind, begin.Team));
    }
}
