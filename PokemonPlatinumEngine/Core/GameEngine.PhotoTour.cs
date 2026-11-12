using System;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using Raylib_cs;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The photo tour (plan 44 · SD5; style guide, "The photo tour"): a script's <c>tour ride</c> opens a course, its
/// residents' models asked for behind the opening's dark and never waited for by a frame; the ride's photographs are
/// judged into the report as it is left, the script hearing the ride's total. KEEP draws the ride's best photograph
/// alone for one frame and keeps it in the photo book, as the photo studio keeps its pictures. A game with no window
/// plays the ride at once by the autopilot.
/// </summary>
public partial class GameEngine
{
    private CourseRenderer? courseRenderer;
    private bool tourCapture;

    /// <summary>The last tour photograph the book kept this run; null before any.</summary>
    public string? LastTourPhoto { get; private set; }

    private CourseRenderer Course => courseRenderer ??= new CourseRenderer(renderContext);

    /// <summary>A ride on a course opened (a script's <c>tour ride</c>); false where the course isn't there or isn't open yet.</summary>
    internal bool OpenPhotoTour(string courseId)
    {
        flow.ScriptAnswer = 0;
        if (PhotoTourCourses.Get(courseId) is not { } course || !flow.PhotoTour.IsOpen(course)) return false;
        uint seed = (uint)Dice.New().Next();
        var weather = TourRide.WeatherFor(course, seed);
        int hour = (int)GameClock.Hour;
        if (IsHeadless)
        {
            var ride = new TourRide(course, seed, hour, weather);
            TourAutopilot.Play(ride);
            flow.ScriptAnswer = flow.PhotoTour.Take(ride);
            return true;
        }
        foreach (var r in course.Residents) PokemonModels.Request(r.Species);
        flow.PhotoTourScreen.Open(course, seed, hour, weather);
        flow.State = GameState.PhotoTour;
        return true;
    }

    /// <summary>Whether every resident of the ride's course has its model made (what the opening waits for).</summary>
    private bool TourModelsReady() =>
        flow.PhotoTourScreen.Ride is not { } ride || ride.Course.Residents.All(r => PokemonModels.TryGet(r.Species, out _));

    private bool tourJudged;

    private void UpdatePhotoTour(float dt)
    {
        var screen = flow.PhotoTourScreen;
        if (tourCapture) return;
        screen.Update(dt, TourModelsReady());
        // Judged into the report once, as the panel comes up
        if (screen.Phase == TourPhase.Judged && !tourJudged && screen.Ride is { } ride)
        {
            tourJudged = true;
            foreach (var b in screen.Bests) flow.PhotoTour.Keep(b);
            flow.PhotoTour.KeepRide(ride.Course.Id, screen.Total);
            flow.ScriptAnswer = screen.Total;
        }
        if (screen.TakeKeepRequest()) BeginTourKeep();
        if (!screen.IsActive)
        {
            tourJudged = false;
            screen.Close();
            courseRenderer?.Unload();
            flow.State = GameState.Overworld;
        }
        else if (screen.Phase != TourPhase.Judged) tourJudged = false;
    }

    /// <summary>The course rendered offscreen: the ride now, or the best photograph alone while it is being kept.</summary>
    private void RenderPhotoTour()
    {
        var screen = flow.PhotoTourScreen;
        if (screen.Ride is not { } ride) return;
        if (screen.Taking && screen.BestPhoto is { } best) Course.Render(best.Frame, ride.Course, best.Frame.Time);
        else if (screen.Phase != TourPhase.Judged) Course.Render(ride.FrameNow(), ride.Course, ride.Time);
    }

    private void DrawPhotoTour()
    {
        var screen = flow.PhotoTourScreen;
        if (screen.Taking || screen.Phase != TourPhase.Judged) Course.DrawToScreen();
        ModernUi.DrawPhotoTour(VirtualWidth, VirtualHeight, screen);
    }

    private PhotoPrint TourPrint(string file, TourBest best) => new()
    {
        File = file,
        Place = PlaceName(),
        Region = Here.Id,
        Day = GameClock.Today,
        Lead = best.Species,
        How = PhotoTaken.Tour
    };

    /// <summary>KEEP pressed: the best photograph drawn alone this frame and kept once drawn; with no window a record is kept at once.</summary>
    private void BeginTourKeep()
    {
        var screen = flow.PhotoTourScreen;
        if (screen.BestPhoto is not { } best) return;
        if (flow.PhotoBook.IsFull)
        {
            flow.Toast.Show(Text.Of("photoBook.full"));
            return;
        }
        if (IsHeadless)
        {
            flow.PhotoBook.Add(TourPrint("", best));
            screen.Kept = true;
            return;
        }
        screen.Taking = true;
        tourCapture = true;
    }

    /// <summary>The best photograph's picture, once drawn alone: halved to 1920 × 1080 with a small copy, never over a file.</summary>
    private void TakeTourPhoto()
    {
        var screen = flow.PhotoTourScreen;
        if (!tourCapture || !screen.Taking) return;
        tourCapture = false;
        screen.Taking = false;
        if (screen.BestPhoto is not { } best) return;
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
            flow.PhotoBook.Add(TourPrint(Path.GetFileName(path), best));
            screen.Kept = true;
            LastTourPhoto = path;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log.Warn($"The tour photograph couldn't be kept: {e.Message}");
        }
    }

    internal sealed partial class Driver
    {
        /// <summary>The photo tour's screen and report (plan 44 · SD5).</summary>
        public PhotoTourScreen PhotoTour => game.flow.PhotoTourScreen;
        public TourReport TourReport => game.flow.PhotoTour;
        public bool OpenPhotoTour(string course) => game.OpenPhotoTour(course);

        /// <summary>Whether every resident's model of the ride is made.</summary>
        public bool TourModelsReady => game.TourModelsReady();

        /// <summary>KEEP as a tool presses it: the next frame is the best photograph alone, kept in the book.</summary>
        public void KeepTourPhoto() => game.BeginTourKeep();

        public string? LastTourPhoto => game.LastTourPhoto;
    }
}
