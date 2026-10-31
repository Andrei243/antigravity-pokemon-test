using System;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;
using Raylib_cs;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>Photo mode in the field (plan 12 · Q16, its second half): the view's bounds, what it borrows and that it changes nothing.</summary>
public class PhotoModeTests
{
    private static PhotoView Outdoors() => new(new Vector2(100.5f, 200.5f), weatherAllowed: true);

    [Fact]
    public void TheCameraGoesNoFurtherThanItsReach()
    {
        var p = Outdoors();
        for (int i = 0; i < 100; i++) p.Pan(new Vector2(1, -1), 0.5f);
        Assert.Equal(PhotoView.ReachX, p.Offset.X);
        Assert.Equal(-PhotoView.ReachZ, p.Offset.Y);
        for (int i = 0; i < 100; i++) p.Pan(new Vector2(-1, 1), 0.5f);
        Assert.Equal(-PhotoView.ReachX, p.Offset.X);
        Assert.Equal(PhotoView.ReachZ, p.Offset.Y);
        Assert.Equal(p.Origin + p.Offset, p.Spot);
    }

    [Fact]
    public void ANudgeMovesAtThePansPace()
    {
        var p = Outdoors();
        p.Pan(new Vector2(1, 0), 0.5f);
        Assert.Equal(PhotoView.PanSpeed * 0.5f, p.Offset.X, 4);
        p.Pan(Vector2.Zero, 1f);
        Assert.Equal(PhotoView.PanSpeed * 0.5f, p.Offset.X, 4);
    }

    [Fact]
    public void TheZoomStaysWithinItsBoundsAndTheKitsAndStepsBackToWhereItWas()
    {
        Assert.True(PhotoView.WidestZoom >= CameraRig.WidestZoom && PhotoView.TightestZoom <= CameraRig.TightestZoom);
        var p = Outdoors();
        for (int i = 0; i < 20; i++) p.ZoomBy(1);
        Assert.Equal(PhotoView.TightestZoom, p.Zoom);
        for (int i = 0; i < 20; i++) p.ZoomBy(-1);
        Assert.Equal(PhotoView.WidestZoom, p.Zoom);
        var q = Outdoors();
        q.ZoomBy(2);
        q.ZoomBy(-2);
        Assert.Equal(1f, q.Zoom);
    }

    [Fact]
    public void TheViewsLensStaysWithinTheThreeCamerasBounds()
    {
        // Each of the field's three cameras, zoomed as far as a photo can, stays within what the camera kit allows it
        float[] lenses = { MapScene.OutdoorFovYDeg, MapScene.CaveFovYDeg, FieldView.Outdoor.FovYDeg };
        foreach (float fov in lenses)
            foreach (float zoom in new[] { PhotoView.WidestZoom, 1f, PhotoView.TightestZoom })
            {
                float lens = CameraRig.FovAt(fov, zoom);
                Assert.InRange(lens, CameraRig.FovAt(fov, CameraRig.TightestZoom), CameraRig.FovAt(fov, CameraRig.WidestZoom));
                Assert.InRange(lens, 1f, 89f);
            }
    }

    [Fact]
    public void ABorrowedHourIsTheMiddleOfItsPeriod()
    {
        var p = Outdoors();
        p.MoveRow(1);
        Assert.Equal(PhotoRow.Time, p.Row);
        Assert.Null(p.Hour);
        foreach (var period in new[] { TimeOfDay.Morning, TimeOfDay.Day, TimeOfDay.Twilight, TimeOfDay.Night, TimeOfDay.LateNight })
        {
            p.Change(1);
            Assert.Equal(period, p.Time);
            Assert.Equal(period, GameClock.ForHour((int)p.Hour!.Value));
        }
        p.Change(1);
        Assert.Null(p.Time);
        p.Change(-1);
        Assert.Equal(TimeOfDay.LateNight, p.Time);
    }

    [Fact]
    public void AWeatherIsBorrowedOnlyInTheOpen()
    {
        var p = Outdoors();
        p.MoveRow(2);
        Assert.Equal(PhotoRow.Weather, p.Row);
        Assert.Null(p.Weather);
        Assert.True(p.Change(1));
        Assert.Equal(FieldWeather.Clear, p.Weather);
        Assert.True(p.Change(1));
        Assert.Equal(FieldWeather.Rain, p.Weather);

        var room = new PhotoView(Vector2.Zero, weatherAllowed: false);
        room.MoveRow(2);
        Assert.False(room.Change(1));
        Assert.Null(room.Weather);
        Assert.Equal(PhotoWeather.Now, room.BorrowedWeather);
    }

    [Fact]
    public void TheRowsWrapAndEveryValueComesRound()
    {
        var p = Outdoors();
        p.MoveRow(-1);
        Assert.Equal(PhotoRow.Frame, p.Row);
        foreach (var frame in new[] { PhotoFrame.Mat, PhotoFrame.Cinema, PhotoFrame.None })
        {
            p.Change(1);
            Assert.Equal(frame, p.Frame);
        }
        p.MoveRow(-1);
        Assert.Equal(PhotoRow.Filter, p.Row);
        for (int i = 0; i < Enum.GetValues<PhotoFilter>().Length; i++) p.Change(1);
        Assert.Equal(PhotoFilter.None, p.Filter);
    }

    [Fact]
    public void APhotoChangesNothingOfTheClockTheDateOrThePlacesWeather()
    {
        var date = GameClock.FixedDate;
        var fixedTime = GameClock.Fixed;
        var map = new Map(4, 4);
        var before = map.WeatherAt(1, 1);
        var p = new PhotoView(new Vector2(1.5f, 1.5f), weatherAllowed: true);
        p.MoveRow(1);
        p.Change(4);
        p.MoveRow(1);
        p.Change(2);
        Assert.Equal(FieldWeather.Rain, p.Weather);
        Assert.Equal(date, GameClock.FixedDate);
        Assert.Equal(fixedTime, GameClock.Fixed);
        Assert.Equal(before, map.WeatherAt(1, 1));
    }

    [Fact]
    public void TheFiltersGradeAsTheStyleGuideSays()
    {
        var rig = ArtLook.FieldRig(13.5f, indoors: false);
        Assert.Equal(rig, PhotoFilters.Filtered(rig, PhotoFilter.None));
        var mono = PhotoFilters.Filtered(rig, PhotoFilter.Mono).Post;
        Assert.Equal(0f, mono.Saturation);
        Assert.Equal(Vector3.One, mono.ShadowTint);
        Assert.Equal(rig.Post.Contrast * 1.10f, mono.Contrast, 4);
        var vivid = PhotoFilters.Filtered(rig, PhotoFilter.Vivid).Post;
        Assert.Equal(rig.Post.Saturation * 1.25f, vivid.Saturation, 4);
        var sepia = PhotoFilters.Filtered(rig, PhotoFilter.Sepia).Post;
        Assert.Equal(new Vector3(1.24f, 1.00f, 0.68f), sepia.HighlightTint);
        var soft = PhotoFilters.Filtered(rig, PhotoFilter.Soft).Post;
        Assert.Equal(rig.Post.Vignette + 0.12f, soft.Vignette, 4);
        // Nothing but the grade changes
        Assert.Equal(rig.Light, PhotoFilters.Filtered(rig, PhotoFilter.Sepia).Light);
    }

    [Fact]
    public void ThePhotoKeyIsBoundInEveryPresetAndSharesNothing()
    {
        foreach (var mode in Enum.GetValues<ButtonMode>())
        {
            var bindings = InputBindings.For(mode);
            Assert.Equal(new[] { KeyboardKey.F9 }, bindings[GameAction.Photo].Keys);
            Assert.Equal(new[] { GamepadButton.MiddleLeft }, bindings[GameAction.Photo].Pad);
            Assert.DoesNotContain(bindings.Conflicts(), c => c.First == GameAction.Photo || c.Second == GameAction.Photo);
        }
        Assert.DoesNotContain(GameAction.Photo, InputManager.NotReadYet);
    }

    [Fact]
    public void EveryValueThePanelWritesHasItsWordsInEnglishAndRomanian()
    {
        var en = Text.KeysOf("en");
        var ro = Text.KeysOf("ro");
        string[] keys =
        {
            "controls.action.Photo", "photo.title", "photo.zoom", "photo.notHere", "photo.hint.compose", "photo.hint.panel", "photo.time.now"
        };
        var all = keys
            .Concat(Enum.GetValues<PhotoRow>().Select(r => $"photo.row.{r}"))
            .Concat(Enum.GetValues<PhotoWeather>().Select(w => $"photo.weather.{w}"))
            .Concat(Enum.GetValues<PhotoFilter>().Select(f => $"photo.filter.{f}"))
            .Concat(Enum.GetValues<PhotoFrame>().Select(f => $"photo.frame.{f}"))
            .Concat(new[] { TimeOfDay.Morning, TimeOfDay.Day, TimeOfDay.Twilight, TimeOfDay.Night, TimeOfDay.LateNight }.Select(t => $"photo.time.{t}"));
        foreach (var key in all)
        {
            Assert.Contains(key, en);
            Assert.Contains(key, ro);
        }
    }
}
