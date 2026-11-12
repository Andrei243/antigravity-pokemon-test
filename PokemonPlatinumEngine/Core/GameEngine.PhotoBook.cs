using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;
using Raylib_cs;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The photo book and the photo spots (plan 43 · LF4; style guide, "Field menus and notices", "Photo mode", and "Menu
/// screens", "The photo book"): photo mode's A keeps its 4K picture in the screenshots folder as ever and puts a copy
/// at 1920 × 1080 with a small one for its page into <c>photos/</c> beside the saves while the book has room; a spot's
/// plate opens photo mode on the spot's view with POSE and FACE alone; the book's pages load their pictures only while
/// they are in view. A pose is the player's gesture held and a face, put back as photo mode is left.
/// </summary>
public partial class GameEngine
{
    // A copy for the book is wanted from the frame the screenshot key takes
    private bool bookPhotoWanted;

    // The face the player had before a photo's, and whether a spot's photograph was taken
    private Mood moodBeforePhoto;
    private bool posing, spotPhotoTaken;
    private Direction? followerFacing;

    // The book's pictures loaded for the page in view, by file
    private readonly Dictionary<string, Texture2D> bookPictures = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The last photograph the book kept this run; null before any.</summary>
    public string? LastBookPhoto { get; private set; }

    /// <summary>The player posed as the photo has it, and a follower turned to the camera; photo mode runs no field, so it holds.</summary>
    private void PoseForThePhoto(PhotoView p)
    {
        if (!posing)
        {
            moodBeforePhoto = flow.Player.Mood;
            if (flow.Along?.Who is { } who) followerFacing = who.Facing;
            posing = true;
        }
        if (flow.Player.Gesture != p.Pose) flow.Player.HoldGesture(p.Pose, PhotoView.PoseHeldAt);
        flow.Player.Mood = p.Face;
        if (flow.Along?.Who is { } along) along.Facing = Direction.Down;
    }

    /// <summary>The pose let go as photo mode is left: the face the player had, no gesture, the follower as it faced; a spot's script hears whether a photograph was taken.</summary>
    private void LetGoOfThePose(PhotoView p)
    {
        if (posing)
        {
            flow.Player.HoldGesture(null, 0f);
            flow.Player.Mood = moodBeforePhoto;
            if (flow.Along?.Who is { } along && followerFacing is { } facing) along.Facing = facing;
            posing = false;
            followerFacing = null;
        }
        if (p.SpotId != null) flow.ScriptAnswer = spotPhotoTaken ? 1 : 0;
        spotPhotoTaken = false;
    }

    /// <summary>
    /// A photo spot read (<c>photo spot "&lt;id&gt;"</c>): photo mode opened looking at the spot's point at its zoom, with
    /// POSE and FACE alone. False where it can't open (no such spot, no window, the field not free, a full book).
    /// </summary>
    internal bool OpenPhotoAtSpot(string id)
    {
        if (PhotoSpots.Get(id) is not { } spot) return false;
        if (IsHeadless)
        {
            // A game with no window keeps a record with no picture, as the headless host does
            flow.ScriptAnswer = flow.PhotoBook.Add(PrintHere(spot, "")) ? 1 : 0;
            return true;
        }
        if (flow.PhotoBook.IsFull)
        {
            ShowNotification(Text.Of("photoBook.full"));
            return false;
        }
        var view = new PhotoView(new Vector2(spot.LookX + 0.5f, spot.LookZ + 0.5f), weatherAllowed: false) { SpotId = spot.Id, PanelOpen = true };
        view.MoveRow(0);
        view.SetZoom(spot.Zoom);
        photo = view;
        barsWanted = false;
        zoomAsked = world.Camera.Zoom;
        world.Photo = view;
        world.Camera.Pan(view.Origin.X, view.Origin.Y, 0.3f);
        spotPhotoTaken = false;
        flow.State = GameState.Photo;
        AudioManager.PlaySound("menu_open");
        return true;
    }

    private PhotoPrint PrintHere(PhotoSpot? spot, string file) => new()
    {
        File = file,
        Place = spot?.Map ?? PlaceName(),
        Region = spot?.Region ?? Here.Id,
        Day = GameClock.Today,
        Lead = flow.Party.Members.FirstOrDefault(p => !p.IsEgg)?.LookName,
        Spot = spot?.Id,
        How = spot != null ? PhotoTaken.Spot : PhotoTaken.PhotoMode
    };

    /// <summary>
    /// The book's copy of the frame just drawn, once the screenshot key has taken it: the picture halved to 1920 × 1080
    /// and a small one at 480 × 270, named by the wall clock and never written over a file. A spot's photograph closes
    /// photo mode after it.
    /// </summary>
    private void TakeBookPhoto()
    {
        if (!bookPhotoWanted) return;
        bookPhotoWanted = false;
        var spot = photo?.SpotId is { } id ? PhotoSpots.Get(id) : null;
        if (flow.PhotoBook.IsFull)
        {
            flow.Toast.Show(Text.Of("photoBook.full"));
            return;
        }
        try
        {
            string folder = SavePaths.Current.Photos;
            Directory.CreateDirectory(folder);
            string path = PhotoBook.PathFor(folder, WallClock.Local, File.Exists);
            var image = Raylib.LoadImageFromTexture(virtualScreen.Texture);
            Raylib.ImageFlipVertical(ref image);
            Raylib.ImageResize(ref image, PhotoBook.Width, PhotoBook.Height);
            Raylib.ExportImage(image, path);
            Raylib.ImageResize(ref image, PhotoBook.SmallWidth, PhotoBook.SmallHeight);
            Raylib.ExportImage(image, PhotoPrint.SmallOf(path));
            Raylib.UnloadImage(image);
            flow.PhotoBook.Add(PrintHere(spot, Path.GetFileName(path)));
            LastBookPhoto = path;
            flow.Toast.Show(Text.Of("photoBook.kept"));
            if (spot != null)
            {
                spotPhotoTaken = true;
                ClosePhoto();
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log.Warn($"The photograph couldn't be kept: {e.Message}");
        }
    }

    /// <summary>The book opened (a script's <c>photo book</c>).</summary>
    internal void OpenPhotoBook()
    {
        flow.PhotoBookScreen.Open(flow.PhotoBook);
        flow.State = GameState.PhotoBook;
    }

    private void UpdatePhotoBook(float dt)
    {
        var screen = flow.PhotoBookScreen;
        screen.Update(dt);
        if (screen.TakeThrown() is { } thrown) DeletePhotoFiles(thrown);
        if (!screen.IsActive)
        {
            ForgetBookPictures();
            flow.State = GameState.Overworld;
        }
    }

    /// <summary>A photograph thrown away: the book's own two files deleted, never anything else.</summary>
    private static void DeletePhotoFiles(PhotoPrint print)
    {
        if (print.File.Length == 0 || Path.GetFileName(print.File) != print.File) return;
        foreach (string name in new[] { print.File, print.SmallFile })
        {
            string path = Path.Combine(SavePaths.Current.Photos, name);
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Log.Warn($"A photograph's file couldn't be deleted: {e.Message}"); }
        }
    }

    /// <summary>The page's small pictures (and the one opened large) loaded, every other let go: the book holds no more than a page on the card.</summary>
    private void KeepThePageLoaded()
    {
        var screen = flow.PhotoBookScreen;
        var wanted = screen.PageNow.Select(p => p.SmallFile).Where(f => f.Length > 0).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (screen.Opened && screen.Selected is { File.Length: > 0 } open) wanted.Add(open.File);
        foreach (string file in bookPictures.Keys.Where(f => !wanted.Contains(f)).ToList())
        {
            Gpu.Unload(bookPictures[file]);
            bookPictures.Remove(file);
        }
        foreach (string file in wanted.Where(f => !bookPictures.ContainsKey(f)))
        {
            string path = Path.Combine(SavePaths.Current.Photos, file);
            if (!File.Exists(path)) continue;
            var texture = Gpu.TextureFromFile(path, "photobook");
            Raylib.SetTextureFilter(texture, TextureFilter.Bilinear);
            bookPictures[file] = texture;
        }
    }

    private Texture2D? BookPicture(string file) => bookPictures.TryGetValue(file, out var t) ? t : null;

    private void ForgetBookPictures()
    {
        foreach (var t in bookPictures.Values) Gpu.Unload(t);
        bookPictures.Clear();
    }

    internal sealed partial class Driver
    {
        /// <summary>The photo book and its screen (plan 43 · LF4).</summary>
        public PhotoBook PhotoBook => game.flow.PhotoBook;
        public PhotoBookScreen PhotoBookScreen => game.flow.PhotoBookScreen;
        public void OpenPhotoBook() => game.OpenPhotoBook();

        /// <summary>A photo spot read by a tool, as its plate's script reads it.</summary>
        public bool OpenPhotoAtSpot(string id) => game.OpenPhotoAtSpot(id);

        /// <summary>The last photograph the book kept; null before any.</summary>
        public string? LastBookPhoto => game.LastBookPhoto;
    }
}
