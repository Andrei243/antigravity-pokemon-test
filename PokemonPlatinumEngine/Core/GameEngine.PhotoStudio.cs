using System;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;
using Raylib_cs;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The photo studio (plan 43 · LF5; style guide, "The photo studio"): a script's <c>photo studio</c> opens the stage, the
/// player's figure is rendered offscreen only with a window, and TAKE draws the picture alone for one frame and keeps it
/// in the photo book at 1920 × 1080 with its small copy and its composition, as photo mode's pictures are kept. A
/// game with no window keeps a record with no picture.
/// </summary>
public partial class GameEngine
{
    /// <summary>The flag the clerk's first visit sets: the studio's first frame is given.</summary>
    public const string StudioFrameFlag = "FLAG_PHOTO_STUDIO_FRAME_GIVEN";

    private RenderTexture2D studioFigure;
    private bool studioFigureLoaded, studioFigureDrawn, studioCapture;

    /// <summary>The last studio photograph the book kept this run; null before any.</summary>
    public string? LastStudioPhoto { get; private set; }

    internal StudioFacts StudioFactsNow() => new() { FrameGiven = flow.Story.Has(StudioFrameFlag), Card = CardFactsNow() };

    /// <summary>The studio opened (a script's <c>photo studio</c>); false where it can't (a full book).</summary>
    internal bool OpenStudio()
    {
        if (flow.PhotoBook.IsFull)
        {
            ShowNotification(Text.Of("photoBook.full"));
            return false;
        }
        // A Pokémon caught since the last save is given its id first, so a photograph can name it
        GiveIds();
        var facts = StudioFactsNow();
        var team = flow.Party.Members.Select(p => new CardCandidate(p.Id, p.DisplayName, p.Level, p)).ToList();
        flow.StudioScreen.Open(flow.LastStudioShot, StudioLooks.BackdropsOpen(facts, flow.FashionCase.Backdrops), StudioLooks.FramesOpen(facts), team, flow.SealCase);
        flow.State = GameState.Studio;
        return true;
    }

    private Pokemon? StudioPokemonNow() =>
        flow.StudioScreen.Chosen?.Pokemon is { } p && StudioShot.MayStand(p) ? p : null;

    private void UpdateStudio(float dt)
    {
        var screen = flow.StudioScreen;
        if (studioCapture) return;
        screen.Update(dt);
        if (screen.TakeTakeRequest()) BeginStudioPhoto();
        if (!screen.IsActive)
        {
            flow.LastStudioShot = screen.Shot;
            ForgetStudioFigure();
            flow.State = GameState.Overworld;
        }
    }

    private PhotoPrint StudioPrint(string file) => new()
    {
        File = file,
        Place = PlaceName(),
        Region = Here.Id,
        Day = GameClock.Today,
        Lead = flow.StudioScreen.Chosen?.Pokemon.LookName,
        How = PhotoTaken.Studio,
        Studio = flow.StudioScreen.Shot with { Stickers = flow.StudioScreen.Shot.Stickers.ToList() }
    };

    /// <summary>TAKE pressed: the stage is drawn alone this frame and kept once drawn; with no window a record is kept at once.</summary>
    private void BeginStudioPhoto()
    {
        if (flow.PhotoBook.IsFull)
        {
            flow.Toast.Show(Text.Of("photoBook.full"));
            return;
        }
        if (IsHeadless)
        {
            flow.PhotoBook.Add(StudioPrint(""));
            flow.Toast.Show(Text.Of("photoBook.kept"));
            return;
        }
        flow.StudioScreen.Taking = true;
        studioCapture = true;
    }

    /// <summary>The stage's picture, once drawn alone: halved to 1920 × 1080 with a small copy, named by the wall clock, never over a file.</summary>
    private void TakeStudioPhoto()
    {
        if (!studioCapture || !flow.StudioScreen.Taking) return;
        studioCapture = false;
        flow.StudioScreen.Taking = false;
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
            flow.PhotoBook.Add(StudioPrint(Path.GetFileName(path)));
            LastStudioPhoto = path;
            flow.Toast.Show(Text.Of("photoBook.kept"));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log.Warn($"The studio photograph couldn't be kept: {e.Message}");
        }
    }

    private Texture2D? StudioFigureNow() => studioFigureLoaded && studioFigureDrawn ? studioFigure.Texture : null;

    /// <summary>The player as the field draws them, in the shot's pose and face, held still; a look not made yet is asked for and never waited for.</summary>
    private void RenderStudioFigure()
    {
        if (IsHeadless) return;
        var shot = flow.StudioScreen.Shot;
        if (!studioFigureLoaded)
        {
            studioFigure = Gpu.Target(ModernUi.StudioFigureWidth, ModernUi.StudioFigureHeight, "ui");
            Raylib.SetTextureFilter(studioFigure.Texture, TextureFilter.Bilinear);
            studioFigureLoaded = true;
        }
        string look = InTheKitHere(PlayerIdentity.Character);
        if (CharacterModels.TryGet(look, renderContext.Shaders) is not { } rig) return;
        // (a grown look framed by its own height, plan 30; a chibi one as it always was)
        var (viewHeight, lookY) = CharacterStudio.FigureOf(rig);
        var pose = new CharacterPose { Time = 0.5f, Expression = CharacterAnimation.ExpressionOf(shot.Mood) };
        if (shot.Pose == CardPoseKind.Sitting) pose.Stance = Overworld.Stance.SitGround;
        else if (shot.Pose == CardPoseKind.Gesture && shot.Gesture is { } gesture)
        {
            pose.Emote = CharacterAnimation.EmoteOf(gesture);
            pose.EmoteTime = CardDesign.HeldAt * CharacterAnimation.Duration(pose.Emote);
        }
        CharacterStudio.Render(renderContext, look, studioFigure, pose, yaw: 0.3f, viewHeight: viewHeight, lookY: lookY);
        studioFigureDrawn = true;
    }

    private void ForgetStudioFigure()
    {
        if (!studioFigureLoaded) return;
        Gpu.Unload(studioFigure);
        studioFigureLoaded = studioFigureDrawn = false;
    }

    internal sealed partial class Driver
    {
        /// <summary>The photo studio and its screen (plan 43 · LF5).</summary>
        public StudioScreen PhotoStudio => game.flow.StudioScreen;
        public bool OpenPhotoStudio() => game.OpenStudio();

        /// <summary>The Pokémon the studio shows, as the stage has it.</summary>
        public Pokemon? PhotoStudioPokemon => game.StudioPokemonNow();

        /// <summary>TAKE as a tool presses it: the next frame is the stage alone, kept in the book.</summary>
        public void TakePhotoStudioPhoto() => game.BeginStudioPhoto();

        /// <summary>The last studio photograph the book kept; null before any.</summary>
        public string? LastPhotoStudioPhoto => game.LastStudioPhoto;
    }
}
