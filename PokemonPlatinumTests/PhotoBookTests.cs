using System;
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
/// Plan 43 · LF4: the photo book, its pages and its round trip; photo mode's POSE and FACE rows; and the photo spots, held
/// to their maps, their plates and their scripts, each played with no window.
/// </summary>
[Collection("Shared clocks and dice")]
public class PhotoBookTests
{
    private static PhotoPrint Print(int n, string region = "Sinnoh", string? spot = null) =>
        new() { File = $"photo-{n:000}.png", Place = "Sinnoh", Region = region, Day = new DateTime(2026, 11, 1).AddDays(n), Spot = spot };

    [Fact]
    public void TheBookHoldsAHundredInOrderAndRoundTrips()
    {
        var book = new PhotoBook();
        for (int i = 0; i < PhotoBook.Capacity; i++) Assert.True(book.Add(Print(i)));
        Assert.True(book.IsFull);
        Assert.False(book.Add(Print(999)));
        Assert.Equal("photo-000.png", book.Prints[0].File);
        Assert.Equal("photo-000-small.png", book.Prints[0].SmallFile);
        var json = System.Text.Json.JsonSerializer.Serialize(book.ToSave());
        var back = new PhotoBook();
        back.Load(System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<PhotoPrint>>(json));
        Assert.Equal(book.Prints, back.Prints);
        Assert.Null(new PhotoBook().ToSave());
    }

    [Fact]
    public void NamesSortByTimeAndNeverOverwrite()
    {
        var t = new DateTime(2026, 11, 12, 9, 30, 0, 123);
        Assert.True(string.CompareOrdinal(PhotoBook.NameFor(t, 0), PhotoBook.NameFor(t.AddMilliseconds(1), 0)) < 0);
        string folder = "x";
        var taken = new System.Collections.Generic.HashSet<string> { Path.Combine(folder, PhotoBook.NameFor(t, 0)) };
        Assert.Equal(Path.Combine(folder, PhotoBook.NameFor(t, 1)), PhotoBook.PathFor(folder, t, taken.Contains));
    }

    [Fact]
    public void ThePagesTabsAndThrowingAway()
    {
        var book = new PhotoBook();
        for (int i = 0; i < 8; i++) book.Add(Print(i, i < 5 ? "Sinnoh" : "Kanto"));
        var screen = new PhotoBookScreen();
        screen.Open(book);
        Assert.Equal(7, screen.Index);
        Assert.Equal(1, screen.Page);
        Assert.Equal(2, screen.Pages);
        Assert.Equal(new[] { "Sinnoh", "Kanto" }, screen.Tabs);
        screen.ChangeTab(1);
        Assert.Equal(5, screen.Shown.Count);
        screen.ChangeTab(1);
        Assert.Equal(3, screen.Shown.Count);
        screen.Move(1, 0);
        screen.Confirm();
        Assert.True(screen.Opened);
        screen.AskThrow();
        Assert.True(screen.Asking);
        // NO first: a press of A keeps it
        screen.Confirm();
        Assert.Equal(8, book.Count);
        screen.AskThrow();
        screen.Move(1, 0);
        screen.Confirm();
        Assert.Equal(7, book.Count);
        Assert.Equal("photo-006.png", screen.TakeThrown()!.File);
        Assert.Null(screen.TakeThrown());
    }

    [Fact]
    public void ThePoseRowsChangeThePictureAndNothingOfTheGame()
    {
        var view = new PhotoView(new System.Numerics.Vector2(10, 10), weatherAllowed: true);
        while (view.Row != PhotoRow.Pose) view.MoveRow(1);
        Assert.True(view.Change(1));
        Assert.Equal(Gesture.Wave, view.Pose);
        view.MoveRow(1);
        Assert.Equal(PhotoRow.Face, view.Row);
        view.Change(1);
        Assert.Equal(Mood.Happy, view.Face);
        // A spot's view offers POSE and FACE alone
        var spot = new PhotoView(new System.Numerics.Vector2(0, 0), weatherAllowed: false) { SpotId = "sinnoh_twinleaf_pond" };
        spot.MoveRow(0);
        Assert.Equal(PhotoRow.Pose, spot.Row);
        spot.MoveRow(1);
        Assert.Equal(PhotoRow.Face, spot.Row);
        spot.MoveRow(1);
        Assert.Equal(PhotoRow.Pose, spot.Row);
        // A pose held is the player's gesture at its height, and let go leaves nothing
        var player = new Player(0, 0);
        player.HoldGesture(Gesture.Cheer, PhotoView.PoseHeldAt);
        Assert.Equal(Gesture.Cheer, player.Gesture);
        Assert.Equal(PhotoView.PoseHeldAt * Acting.Length(Gesture.Cheer), player.GestureAge, 3);
        player.HoldGesture(null, 0f);
        Assert.Null(player.Gesture);
    }

    [Fact]
    public void EverySpotStandsOnItsMapWithItsPlateAndScript()
    {
        Assert.Equal(5, PhotoSpots.All.Count(s => s.Region == RegionDatabase.Sinnoh));
        var library = ScriptLibrary.Default;
        foreach (var spot in PhotoSpots.All)
        {
            var map = MapDatabase.Get(spot.Map);
            Assert.True(map.InBounds(spot.X, spot.Z) && map.InBounds(spot.LookX, spot.LookZ), spot.Id);
            Assert.Contains(map.Props, p => p.Type == PropType.PhotoSpot && p.Covers(spot.X, spot.Z));
            Assert.Null(map.GetWarpAt(spot.X, spot.Z));
            // Read from a tile beside it a player can stand on
            Assert.True(new[] { (0, 1), (0, -1), (1, 0), (-1, 0) }.Any(d => map.IsWalkable(spot.X + d.Item1, spot.Z + d.Item2)), $"{spot.Id}: nothing to read it from");
            Assert.True(map.TileScripts.TryGetValue((spot.X, spot.Z), out var script) && script.EndsWith("PhotoSpot", StringComparison.Ordinal), $"{spot.Id}: no script on its plate");
            string area = map.AreaAt(spot.X, spot.Z)?.Key ?? "";
            var photo = library.Find("PhotoSpot", area);
            Assert.NotNull(photo);
            Assert.Contains(photo!.Code, i => i.Op == Op.Photo && i.Name == "spot" && i.Other == spot.Id);
            foreach (string tag in new[] { "en", "ro" })
            {
                string language = Text.Current;
                try { Text.Use(tag); Assert.False(Text.Of($"photoBook.spot.{spot.Id}").StartsWith("photoBook", StringComparison.Ordinal)); }
                finally { Text.Use(language); }
            }
        }
    }

    [Fact]
    public void EachSpotsScriptKeepsARecordWithNoWindowAndARegionsFiveUnlockItsBackground()
    {
        var host = new HeadlessScriptHost();
        foreach (var spot in PhotoSpots.All.Where(s => s.Region == RegionDatabase.Sinnoh))
        {
            var area = MapDatabase.Get(spot.Map).AreaAt(spot.X, spot.Z)!.Key;
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find("PhotoSpot", area)!);
            runner.RunToEnd();
            Assert.Contains($"photo spot {spot.Id}", host.Log);
        }
        Assert.Equal(5, host.PhotoBook!.Count);
        Assert.Contains("Sinnoh", host.PhotoBook.RegionsWhole());
        var facts = new CardFacts { SpotsTaken = host.PhotoBook.RegionsWhole().ToHashSet(StringComparer.OrdinalIgnoreCase) };
        Assert.Contains("sinnoh_snapshot", CardLooks.BackgroundsOpen(facts));
        Assert.DoesNotContain("sinnoh_snapshot", CardLooks.BackgroundsOpen(CardFacts.None));
    }
}
