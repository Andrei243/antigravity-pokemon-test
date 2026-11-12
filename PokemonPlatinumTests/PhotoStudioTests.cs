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
/// Plan 43 · LF5: the photo studio. Its looks and what each waits for, the composition's rules (a Seal not owned refused
/// and none used up, an Egg refused, at most twelve stickers inside the picture), the screen's rows and sticker cursor
/// driven with no window, the composition kept with its print through a save, and the photographer's script.
/// </summary>
[Collection("Shared clocks and dice")]
public class PhotoStudioTests
{
    private static SealCase Seals(params int[] owned)
    {
        var seals = new SealCase();
        foreach (int s in owned) Assert.True(seals.Give(s));
        return seals;
    }

    [Fact]
    public void EveryLookIsWellFormedNamedAndInBothLanguages()
    {
        Assert.Equal(StudioLooks.Backdrops.Count, StudioLooks.Backdrops.Select(b => b.Id).Distinct().Count());
        Assert.Equal(StudioLooks.Frames.Count, StudioLooks.Frames.Select(f => f.Id).Distinct().Count());
        Assert.Equal(CardLooks.Start, StudioLooks.Backdrops[0].Unlock);
        Assert.Equal(CardLooks.Start, StudioLooks.Frames[0].Unlock);
        foreach (string unlock in StudioLooks.Backdrops.Select(b => b.Unlock).Concat(StudioLooks.Frames.Select(f => f.Unlock)))
            Assert.True(StudioLooks.IsWellFormed(unlock), unlock);
        var language = Text.Current;
        try
        {
            foreach (string tag in new[] { "en", "ro" })
            {
                Text.Use(tag);
                foreach (var b in StudioLooks.Backdrops) Assert.False(Text.Of($"photoStudio.backdrop.{b.Id}").StartsWith("photoStudio", StringComparison.Ordinal), $"{tag} {b.Id}");
                foreach (var f in StudioLooks.Frames) Assert.False(Text.Of($"photoStudio.frame.{f.Id}").StartsWith("photoStudio", StringComparison.Ordinal), $"{tag} {f.Id}");
                foreach (var row in Enum.GetValues<StudioRow>()) Assert.False(Text.Of($"photoStudio.row.{row}").StartsWith("photoStudio", StringComparison.Ordinal), $"{tag} {row}");
                foreach (string why in new[] { "notOwned", "full", "outside", "noSeals" }) Assert.False(Text.Of($"photoStudio.refusal.{why}").StartsWith("photoStudio", StringComparison.Ordinal), $"{tag} {why}");
            }
        }
        finally { Text.Use(language); }
    }

    [Fact]
    public void ALookWaitsForItsFactAndTheGiftedFrameForTheClerksGift()
    {
        var none = new StudioFacts();
        Assert.DoesNotContain(StudioLooks.GiftFrame, StudioLooks.FramesOpen(none));
        Assert.Contains(StudioLooks.GiftFrame, StudioLooks.FramesOpen(new StudioFacts { FrameGiven = true }));
        Assert.DoesNotContain("studio_peaks", StudioLooks.BackdropsOpen(none));
        var badges = new StudioFacts { Card = new CardFacts { BadgesWhole = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "sinnoh" } } };
        Assert.Contains("studio_peaks", StudioLooks.BackdropsOpen(badges));
        // The Fashion Case's backdrops owned are the studio's too
        Assert.Contains("backdrop:3", StudioLooks.BackdropsOpen(none, new[] { 3 }));
    }

    [Fact]
    public void ASealNotOwnedIsRefusedAndNoneIsUsedUp()
    {
        int heart = SealDatabase.Seal("HEART_SEAL_A");
        Assert.NotEqual(0, heart);
        var seals = Seals(heart);
        var shot = new StudioShot();
        Assert.Equal("notOwned", shot.WhyNotSticker(SealDatabase.Seal("STAR_SEAL_A"), 0.5f, 0.5f, seals));
        Assert.Empty(shot.WithSticker(SealDatabase.Seal("STAR_SEAL_A"), 0.5f, 0.5f, seals).Stickers);
        var one = shot.WithSticker(heart, 0.25f, 0.75f, seals);
        Assert.Single(one.Stickers);
        Assert.Equal(1, seals.Count(heart));
        // Off the picture is refused
        Assert.Equal("outside", shot.WhyNotSticker(heart, 1.2f, 0.5f, seals));
    }

    [Fact]
    public void AtMostTwelveStickersStandOnAPicture()
    {
        int heart = SealDatabase.Seal("HEART_SEAL_A");
        var seals = Seals(heart);
        var shot = new StudioShot();
        for (int i = 0; i < StudioShot.MostStickers + 3; i++) shot = shot.WithSticker(heart, 0.1f + i * 0.05f, 0.5f, seals);
        Assert.Equal(StudioShot.MostStickers, shot.Stickers.Count);
        Assert.Equal("full", shot.WhyNotSticker(heart, 0.5f, 0.5f, seals));
        Assert.Equal(StudioShot.MostStickers - 1, shot.WithoutSticker(0).Stickers.Count);
    }

    [Fact]
    public void AnEggMayNotStandOnTheStage()
    {
        var egg = new Pokemon(PokemonDatabase.Get("Bidoof")!, 5) { Id = Guid.NewGuid(), IsEgg = true };
        var bidoof = new Pokemon(PokemonDatabase.Get("Bidoof")!, 5) { Id = Guid.NewGuid() };
        Assert.False(StudioShot.MayStand(egg));
        Assert.True(StudioShot.MayStand(bidoof));
        var screen = new StudioScreen();
        screen.Open(null, new[] { "plain" }, new[] { "plain" },
            new[] { new CardCandidate(egg.Id, "Egg", 1, egg), new CardCandidate(bidoof.Id, "Bidoof", 5, bidoof) }, Seals());
        Assert.Single(screen.Candidates);
    }

    [Fact]
    public void TheRowsAndTheStickersCursorAreDrivenWithNoWindow()
    {
        int heart = SealDatabase.Seal("HEART_SEAL_A");
        var bidoof = new Pokemon(PokemonDatabase.Get("Bidoof")!, 5) { Id = Guid.NewGuid() };
        var seals = Seals(heart);
        var screen = new StudioScreen();
        screen.Open(null, new[] { "plain", "studio_sky" }, new[] { "plain", "studio_double" }, new[] { new CardCandidate(bidoof.Id, "Bidoof", 5, bidoof) }, seals);
        Assert.Equal(StudioRow.Backdrop, screen.Row);
        screen.Change(1);
        Assert.Equal("studio_sky", screen.Shot.Backdrop);
        screen.Move(1); screen.Change(1);
        Assert.Equal(1, screen.Shot.PoseIndex);
        Assert.Equal(CardPoseKind.Sitting, screen.Shot.Pose);
        screen.Move(1); screen.Change(1);
        Assert.Equal(Mood.Happy, screen.Shot.Mood);
        screen.Move(1); screen.Change(1);
        Assert.Equal(bidoof.Id, screen.Shot.PokemonId);
        screen.Move(1); screen.Change(1);
        Assert.Equal(StudioPlace.Left, screen.Shot.Place);
        // The stickers: A opens the case, A carries the Seal, the cursor moves, A puts it down, B steps back
        screen.Move(1);
        Assert.Equal(StudioRow.Stickers, screen.Row);
        screen.Confirm();
        Assert.Equal(StudioMode.PickSeal, screen.Mode);
        screen.Confirm();
        Assert.Equal(StudioMode.PlaceSeal, screen.Mode);
        screen.Change(1); screen.Move(-1);
        Assert.Equal(0.5f + StudioScreen.CursorStep, screen.CursorX, 3);
        screen.Place();
        Assert.Single(screen.Shot.Stickers);
        screen.Cancel(); screen.Cancel();
        Assert.Equal(StudioMode.Rows, screen.Mode);
        // Left on STICKERS takes the last one off
        screen.Change(-1);
        Assert.Empty(screen.Shot.Stickers);
        // The frame, then TAKE asks for the picture once
        screen.Move(1); screen.Change(1);
        Assert.Equal("studio_double", screen.Shot.Frame);
        screen.Move(1);
        Assert.Equal(StudioRow.Take, screen.Row);
        Assert.False(screen.TakeTakeRequest());
        screen.Confirm();
        Assert.True(screen.TakeTakeRequest());
        Assert.False(screen.TakeTakeRequest());
    }

    [Fact]
    public void ThePointerChoosesARowASealAndAPointOfTheStage()
    {
        int heart = SealDatabase.Seal("HEART_SEAL_A");
        var screen = new StudioScreen();
        screen.Open(null, new[] { "plain" }, new[] { "plain" }, Array.Empty<CardCandidate>(), Seals(heart));
        // A row, from the rectangle it is drawn in
        var frameRow = ModernUi.StudioRowRect(GameEngine.VirtualWidth, StudioRow.Frame);
        int? hit = screen.HitTest(new System.Numerics.Vector2(frameRow.X + 10, frameRow.Y + 10));
        Assert.Equal((int)StudioRow.Frame, hit);
        screen.PointAt(hit!.Value);
        Assert.Equal(StudioRow.Frame, screen.Row);
        Assert.Null(screen.HitTest(new System.Numerics.Vector2(2, 2)));
        // The case's first Seal
        screen.PointAt((int)StudioRow.Stickers);
        screen.Confirm();
        Assert.Equal(StudioMode.PickSeal, screen.Mode);
        Assert.Equal(100, screen.HitTest(ModernUi.StudioSealSpot(0)));
        screen.Confirm();
        // A point of the stage carries the Seal there, and A puts it down
        var stage = ModernUi.StudioStage;
        var at = new System.Numerics.Vector2(stage.X + stage.Width * 0.25f, stage.Y + stage.Height * 0.75f);
        screen.PointAt(screen.HitTest(at)!.Value);
        Assert.Equal(0.25f, screen.CursorX, 2);
        Assert.Equal(0.75f, screen.CursorY, 2);
        screen.Confirm();
        var sticker = Assert.Single(screen.Shot.Stickers);
        Assert.Equal(0.25f, sticker.X, 2);
    }

    [Fact]
    public void TheLastCompositionIsSavedAndWhatIsGoneFallsBack()
    {
        int heart = SealDatabase.Seal("HEART_SEAL_A");
        var seals = Seals(heart);
        var gone = Guid.NewGuid();
        var shot = new StudioShot { Backdrop = "studio_sky", Frame = "studio_double", PokemonId = gone }.WithSticker(heart, 0.4f, 0.4f, seals);
        var back = SaveManager.Read(SaveManager.Write(new SaveData { LastStudioShot = shot })!)!;
        Assert.NotNull(back.LastStudioShot);
        Assert.Equal("studio_sky", back.LastStudioShot!.Backdrop);
        Assert.Single(back.LastStudioShot.Stickers);
        // Opened with the Pokémon gone and the Seal no longer owned: both fall away, the rest stays
        var screen = new StudioScreen();
        screen.Open(back.LastStudioShot, new[] { "plain", "studio_sky" }, new[] { "plain", "studio_double" }, Array.Empty<CardCandidate>(), Seals());
        Assert.Equal("studio_sky", screen.Shot.Backdrop);
        Assert.Equal(Guid.Empty, screen.Shot.PokemonId);
        Assert.Empty(screen.Shot.Stickers);
        // A save that never left the studio writes nothing of it
        Assert.DoesNotContain("lastStudioShot", SaveManager.Write(new SaveData())!, StringComparison.Ordinal);
    }

    [Fact]
    public void NoSealsMeansTheStickersRowSaysSo()
    {
        var screen = new StudioScreen();
        screen.Open(null, new[] { "plain" }, new[] { "plain" }, Array.Empty<CardCandidate>(), Seals());
        for (int i = 0; i < 5; i++) screen.Move(1);
        Assert.Equal(StudioRow.Stickers, screen.Row);
        screen.Confirm();
        Assert.Equal(StudioMode.Rows, screen.Mode);
        Assert.Equal("noSeals", screen.Refusal);
    }

    [Fact]
    public void ACompositionIsKeptWithItsPrintThroughASave()
    {
        int heart = SealDatabase.Seal("HEART_SEAL_A");
        var seals = Seals(heart);
        var shot = new StudioShot { Backdrop = "studio_sky", Frame = "studio_double", Mood = Mood.Happy, PokemonId = Guid.NewGuid(), Place = StudioPlace.Left }
            .WithPose(4).WithSticker(heart, 0.3f, 0.6f, seals).WithSticker(heart, 0.8f, 0.2f, seals);
        var book = new PhotoBook();
        book.Add(new PhotoPrint { File = "photo-x.png", Place = "Jubilife Boutique", Region = "sinnoh", Day = new DateTime(2026, 11, 12), How = PhotoTaken.Studio, Studio = shot });
        var data = new SaveData { PhotoBook = book.ToSave() };
        var back = SaveManager.Read(SaveManager.Write(data)!)!;
        var again = new PhotoBook();
        again.Load(back.PhotoBook);
        var kept = Assert.Single(again.Prints);
        Assert.Equal(PhotoTaken.Studio, kept.How);
        Assert.Equal(shot.Backdrop, kept.Studio!.Backdrop);
        Assert.Equal(shot.Frame, kept.Studio.Frame);
        Assert.Equal(shot.PokemonId, kept.Studio.PokemonId);
        Assert.Equal(shot.Pose, kept.Studio.Pose);
        Assert.Equal(shot.Gesture, kept.Studio.Gesture);
        Assert.Equal(2, kept.Studio.Stickers.Count);
        Assert.Equal(0.3f, kept.Studio.Stickers[0].X, 3);
        // A photograph of photo mode has none
        Assert.Null(new PhotoPrint().Studio);
    }

    [Fact]
    public void ThePhotographersFirstVisitGivesTheFrameAndOpensTheStudio()
    {
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("Photographer", "JubilifeBoutique")!);
        runner.RunToEnd();
        Assert.Contains(host.Log, l => l == "open PhotoStudio");
        Assert.True(host.Story.Has(GameEngine.StudioFrameFlag));
        Assert.DoesNotContain(ScriptLibrary.Default.Problems(), p => p.Contains("Photographer", StringComparison.Ordinal));
    }

    [Fact]
    public void AGameWithNoWindowOpensTheStudioTakesAPictureAndTouchesNoGraphicsCard()
    {
        string folder = Path.Combine(Path.GetTempPath(), "photo-studio-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(7);
            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            game.Party.Add(new Pokemon(PokemonDatabase.Get("Starly")!, 8));
            game.State = GameState.Overworld;
            game.SealCase.Give(SealDatabase.Seal("HEART_SEAL_A"));

            Assert.True(game.OpenPhotoStudio());
            Assert.Equal(GameState.Studio, game.State);
            var studio = game.PhotoStudio;
            // The team's Starly (the starter's first, so one of the Pokémon) is offered, and the frame of the gift is not yet
            Assert.NotEmpty(studio.Candidates);
            Assert.DoesNotContain(StudioLooks.GiftFrame, studio.Frames);
            while (studio.Row != StudioRow.Take) studio.Move(1);
            studio.Confirm();
            InputManager.Feed(ActionFrame.None);
            engine.Update(1f / 60f);
            Assert.Equal(1, game.PhotoBook.Count);
            Assert.Equal(PhotoTaken.Studio, game.PhotoBook.Prints[0].How);
            Assert.NotNull(game.PhotoBook.Prints[0].Studio);
            // Out of the studio and back to the field
            studio.Cancel();
            engine.Update(1f / 60f);
            Assert.Equal(GameState.Overworld, game.State);
        }
        finally
        {
            SavePaths.Current = paths;
            try { Directory.Delete(folder, true); } catch (IOException) { }
        }
    }
}
