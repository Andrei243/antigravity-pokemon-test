using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 43 · LF1: the card the player designs. Its looks and what each waits for, the signature's pad, the six shown,
/// the designer's rows driven with no window, the greeting by key in English and Romanian, and a design kept by a save.
/// In the collection that runs alone, since the game's tests change the language and the clocks.
/// </summary>
[Collection("Shared clocks and dice")]
public class CardDesignTests
{
    [Fact]
    public void EveryLookIsWellFormedNamedAndInBothLanguages()
    {
        Assert.Equal(20, CardLooks.Backgrounds.Count);
        Assert.Equal(8, CardLooks.Frames.Count);
        Assert.Equal(CardLooks.Backgrounds.Count, CardLooks.Backgrounds.Select(b => b.Id).Distinct().Count());
        Assert.Equal(CardLooks.Frames.Count, CardLooks.Frames.Select(b => b.Id).Distinct().Count());
        foreach (var unlock in CardLooks.Backgrounds.Select(b => b.Unlock).Concat(CardLooks.Frames.Select(f => f.Unlock)))
            Assert.True(CardLooks.IsWellFormed(unlock), unlock);
        // A look's region is one the game has, and a medal category is one of the medals'
        foreach (var unlock in CardLooks.Backgrounds.Select(b => b.Unlock).Concat(CardLooks.Frames.Select(f => f.Unlock)).Where(u => u != CardLooks.Start))
        {
            var (kind, what) = (unlock[..unlock.IndexOf(':')], unlock[(unlock.IndexOf(':') + 1)..]);
            if (kind is "badges" or "champion" or "spots") Assert.NotNull(RegionDatabase.Get(what));
            if (kind == "medals") Assert.True(Enum.TryParse<MedalCategory>(what, out _), unlock);
        }
        // The first is the start's, so a card never designed has a look to begin on
        Assert.Equal(CardLooks.Start, CardLooks.Backgrounds[0].Unlock);
        Assert.Equal(CardLooks.Start, CardLooks.Frames[0].Unlock);
        var language = Text.Current;
        try
        {
            foreach (string tag in new[] { "en", "ro" })
            {
                Text.Use(tag);
                foreach (var b in CardLooks.Backgrounds) Assert.False(Text.Of($"cardDesign.background.{b.Id}").StartsWith("cardDesign", StringComparison.Ordinal), $"{tag} {b.Id}");
                foreach (var f in CardLooks.Frames) Assert.False(Text.Of($"cardDesign.frame.{f.Id}").StartsWith("cardDesign", StringComparison.Ordinal), $"{tag} {f.Id}");
            }
        }
        finally { Text.Use(language); }
    }

    [Fact]
    public void EachLookIsLockedUntilItsFact()
    {
        var none = CardFacts.None;
        Assert.Equal(CardLooks.Backgrounds.Count(b => b.Unlock == CardLooks.Start), CardLooks.BackgroundsOpen(none).Count);
        Assert.DoesNotContain("sinnoh_peaks", CardLooks.BackgroundsOpen(none));
        var sinnoh = new HashSet<string>(new[] { "sinnoh" }, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("sinnoh_peaks", CardLooks.BackgroundsOpen(new CardFacts { BadgesWhole = sinnoh }));
        Assert.DoesNotContain("sinnoh_aurora", CardLooks.BackgroundsOpen(new CardFacts { BadgesWhole = sinnoh }));
        Assert.Contains("sinnoh_aurora", CardLooks.BackgroundsOpen(new CardFacts { Champions = sinnoh }));
        Assert.Contains("laurel", CardLooks.FramesOpen(new CardFacts { Champions = sinnoh }));
        Assert.Contains("flare", CardLooks.BackgroundsOpen(new CardFacts { MedalsWhole = new HashSet<string> { "Battles" } }));
        Assert.Contains("gilt", CardLooks.FramesOpen(new CardFacts { Diplomas = new HashSet<string> { "Sinnoh" } }));
        // The Fashion Case's backdrops owned follow the file's, in number order
        var open = CardLooks.BackgroundsOpen(none, new[] { 7, 2 });
        Assert.Equal(new[] { "backdrop:2", "backdrop:7" }, open.TakeLast(2));
        Assert.Equal(7, CardLooks.BackdropOf("backdrop:7"));
        Assert.Null(CardLooks.BackdropOf("plain"));
    }

    [Fact]
    public void ThePadStrokesErasesClearsAndKeepsItsBits()
    {
        var pad = new SignaturePad();
        Assert.True(pad.IsEmpty);
        Assert.Null(pad.ToSaved());
        pad.Stroke(0, 0, 9, 0);
        Assert.Equal(10, pad.Count);
        pad.Stroke(0, 0, 4, 4);
        Assert.True(pad[2, 2] && pad[4, 4]);
        // Off the pad is passed over, never wrapped
        pad.Stroke(SignaturePad.Width - 2, 10, SignaturePad.Width + 5, 10);
        Assert.True(pad[SignaturePad.Width - 1, 10]);
        Assert.False(pad[0, 11]);
        int before = pad.Count;
        pad.Stroke(0, 0, 9, 0, erase: true);
        Assert.Equal(before - 10, pad.Count);
        Assert.Equal(SignaturePad.Width * SignaturePad.Height / 8, pad.ToBits().Length);
        var back = SignaturePad.FromSaved(pad.ToSaved());
        for (int y = 0; y < SignaturePad.Height; y++)
            for (int x = 0; x < SignaturePad.Width; x++)
                Assert.Equal(pad[x, y], back[x, y]);
        pad.Clear();
        Assert.True(pad.IsEmpty);
        Assert.True(SignaturePad.FromSaved("not base 64 at all!").IsEmpty);
    }

    [Fact]
    public void APokemonReleasedOrTradedDropsOutOfTheSix()
    {
        var a = new Pokemon(PokemonDatabase.Get("Bidoof")!, 5) { Id = Guid.NewGuid() };
        var b = new Pokemon(PokemonDatabase.Get("Starly")!, 5) { Id = Guid.NewGuid() };
        var owned = new Dictionary<Guid, Pokemon> { [a.Id] = a, [b.Id] = b };
        var design = new CardDesign { Shown = new List<Guid> { b.Id, a.Id, Guid.NewGuid() } };
        Assert.Equal(new[] { b, a }, design.ShownOf(new[] { a }, owned));
        owned.Remove(b.Id);
        design.Forget(owned.ContainsKey);
        Assert.Equal(new[] { a.Id }, design.Shown);
        // The team as it stands: its first six that are no Eggs
        Assert.Equal(new[] { a }, new CardDesign().ShownOf(new[] { a }, owned));
    }

    private static CardDesignScreen Designer(out List<CardCandidate> candidates)
    {
        candidates = Enumerable.Range(0, 8).Select(i =>
        {
            var p = new Pokemon(PokemonDatabase.Get("Bidoof")!, 5 + i) { Id = Guid.NewGuid() };
            return new CardCandidate(p.Id, "Bidoof", p.Level, p);
        }).ToList();
        var screen = new CardDesignScreen();
        screen.Open(null, CardLooks.BackgroundsOpen(CardFacts.None), CardLooks.FramesOpen(CardFacts.None),
            new[] { new CardTitleChoice(CardTitleKind.Champion, "Sinnoh", "Champion of Sinnoh") }, candidates, "");
        return screen;
    }

    [Fact]
    public void TheRowsAndTheCursorChangeTheDesign()
    {
        var screen = Designer(out var candidates);
        Assert.Equal(CardDesignRow.Background, screen.Row);
        string first = screen.Design.Background;
        screen.Change(1);
        Assert.NotEqual(first, screen.Design.Background);
        screen.Change(-1);
        Assert.Equal(first, screen.Design.Background);
        screen.Move(-1);
        Assert.Equal(CardDesignRow.Print, screen.Row);
        screen.Move(1);
        screen.Move(1);
        screen.Move(1);
        Assert.Equal(CardDesignRow.Pose, screen.Row);
        screen.Change(1);
        Assert.Equal(CardPoseKind.Sitting, screen.Design.Pose);
        screen.Change(1);
        Assert.Equal((CardPoseKind.Gesture, (Gesture?)Gesture.Wave), (screen.Design.Pose, screen.Design.Gesture));
        screen.Move(1);
        screen.Change(1);
        Assert.Equal(Mood.Happy, screen.Design.Mood);
        screen.Move(1);
        screen.Change(1);
        Assert.Equal((CardTitleKind.Champion, "Sinnoh"), (screen.Design.Title, screen.Design.TitleRegion));
        // SHOWN: the list, two taken out and one put back; never a seventh
        screen.Move(1);
        Assert.Equal(CardDesignRow.Shown, screen.Row);
        screen.Confirm();
        Assert.Equal(CardDesignMode.PickShown, screen.Mode);
        Assert.Equal(6, screen.Design.Shown!.Count);
        screen.Confirm();
        Assert.Equal(5, screen.Design.Shown.Count);
        screen.Move(1); screen.Move(1); screen.Move(1); screen.Move(1); screen.Move(1); screen.Move(1); screen.Move(1);
        screen.Confirm();
        Assert.Equal(candidates[7].Id, screen.Design.Shown[^1]);
        screen.Move(1);
        screen.Confirm();
        Assert.Equal(6, screen.Design.Shown.Count);
        Assert.False(screen.IsShown(candidates[0].Id) && screen.Design.Shown.Count > 6);
        screen.Cancel();
        Assert.Equal(CardDesignMode.Rows, screen.Mode);
        // GREETING asks the game for the picker, once
        screen.Move(1);
        screen.Confirm();
        Assert.True(screen.TakeGreetingRequest());
        Assert.False(screen.TakeGreetingRequest());
        // SIGNATURE: the pad, a stroke with the pen down, an eraser, a clear
        screen.Move(1);
        screen.Confirm();
        Assert.Equal(CardDesignMode.Pad, screen.Mode);
        screen.Confirm();
        screen.Change(1);
        screen.Move(1);
        Assert.True(screen.Pad.Count >= 3);
        screen.ToggleEraser();
        Assert.True(screen.Eraser);
        screen.ClearPad();
        Assert.True(screen.Pad.IsEmpty);
        screen.ToggleEraser();
        screen.Confirm();
        screen.Change(1);
        screen.Cancel();
        Assert.NotNull(screen.Design.Signature);
        // PRINT asks the game, once; leaving keeps everything
        screen.Move(1);
        screen.Confirm();
        Assert.True(screen.TakePrintRequest());
        screen.Cancel();
        Assert.False(screen.IsActive);
        Assert.NotNull(screen.Design.Signature);
    }

    [Fact]
    public void ThePointerFindsTheRows()
    {
        var screen = Designer(out _);
        var rows = ModernUi.CardDesignRows();
        Assert.Equal(Enum.GetValues<CardDesignRow>().Length, rows.Count);
        var middle = new System.Numerics.Vector2(rows[4].X + rows[4].Width / 2, rows[4].Y + rows[4].Height / 2);
        Assert.Equal(4, screen.HitTest(middle));
        screen.PointAt(4);
        Assert.Equal(CardDesignRow.Title, screen.Row);
    }

    [Fact]
    public void TheGreetingIsSaidByItsKeysInEitherLanguage()
    {
        var phrase = new[] { "feelings:happy", "small:together" };
        var language = Text.Current;
        try
        {
            Text.Use("en");
            string english = PhraseGroups.Shown(phrase);
            Text.Use("ro");
            string romanian = PhraseGroups.Shown(phrase);
            Assert.NotEqual(english, romanian);
            Assert.DoesNotContain(":", romanian);
        }
        finally { Text.Use(language); }
    }

    [Fact]
    public void AGameWithNoWindowDesignsTheCardAndASaveKeepsIt()
    {
        string folder = Path.Combine(Path.GetTempPath(), "card-design-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(5);
            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            game.Party.Add(new Pokemon(PokemonDatabase.Get("Starly")!, 8));
            game.State = GameState.Overworld;

            // A card never designed is today's front
            Assert.Null(game.CardDesign);
            Assert.Null(game.CardNow.Design);
            game.ChooseFromStartMenu(StartMenuChoice.Trainer);
            Assert.Equal(GameState.TrainerCard, game.State);
            // The item button opens the designer; nothing of it touches the graphics card
            game.TrainerCard.AskForDesigner();
            InputManager.Feed(ActionFrame.None);
            engine.Update(1f / 60f);
            Assert.Equal(GameState.CardDesign, game.State);
            var designer = game.CardDesigner;
            designer.Change(1);
            designer.Move(1);
            designer.Change(1);
            string background = designer.Design.Background, frame = designer.Design.Frame;
            // The greeting through the picker, back to the designer, and no script told
            designer.Move(1); designer.Move(1); designer.Move(1); designer.Move(1); designer.Move(1);
            Assert.Equal(CardDesignRow.Greeting, designer.Row);
            designer.Confirm();
            engine.Update(1f / 60f);
            Assert.Equal(GameState.Phrases, game.State);
            Assert.True(game.Phrases.Entry!.Choose(0, "feelings:happy"));
            Assert.True(game.Phrases.Entry.Choose(1, "small:together"));
            game.Phrases.Confirm();
            for (int f = 0; f < 30 && game.State == GameState.Phrases; f++) engine.Update(1f / 60f);
            Assert.Equal(GameState.CardDesign, game.State);
            Assert.Contains("happy", designer.Greeting, StringComparison.OrdinalIgnoreCase);
            // Signed, then out of the designer back to the card, with the face of the design
            designer.Move(1);
            designer.Confirm();
            designer.Confirm();
            designer.Change(1);
            designer.Change(1);
            designer.Cancel();
            designer.Cancel();
            engine.Update(1f / 60f);
            Assert.Equal(GameState.TrainerCard, game.State);
            var face = game.CardNow.Design;
            Assert.NotNull(face);
            Assert.Equal(background, face!.Background?.Id);
            Assert.Equal(frame, face.Frame?.Id);
            Assert.Single(face.Shown.Where(p => p.Species.Name == "Starly"));
            Assert.False(face.Signature!.IsEmpty);

            // A save keeps it, and a game loaded from it has it
            var save = game.BuildSave();
            Assert.NotNull(save.CardDesign);
            var read = SaveManager.Read(SaveManager.Write(save))!;
            game.CardDesign = null;
            game.ApplySave(read);
            Assert.Equal(background, game.CardDesign!.Background);
            Assert.Equal(save.CardDesign!.Signature, game.CardDesign.Signature);
            Assert.Equal(new[] { "feelings:happy", "small:together" }, game.Story.PhraseOf(CardDesign.GreetingPhrase));
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }
}
