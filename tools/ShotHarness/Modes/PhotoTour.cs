using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    // Plan 44 · SD5 (tu01 to tu08; not part of `all`): the photo tour's guide on Pastoria's observatory floor, a ride on the
    // Reed River from its opening, the viewfinder, a fruit thrown, the whistle, the closing and the judged panel, and the
    // best photograph kept in the photo book. The ride is played by the screen's own clock with its autopilot's hand on
    // the aim (`PhotoTourScreen.Autopilot`); the fruit and the whistle are pressed as a player presses them.
    public void TourMode()
    {
        game.Pokedex.UnlockNational();
        At("PastoriaCityObservatoryGate2F", 10, 5, Direction.Right);
        game.LocationSign.Hide();
        Frames(30);
        Shot("tu01_guide");

        game.OpenPhotoTour("reed_river");
        var s = game.PhotoTour;
        s.Autopilot = true;
        for (int i = 0; i < 1800 && !game.TourModelsReady; i++) Frames(1);
        Frames(30);
        Shot("tu02_ride_start");
        Frames(60 * 6);
        Shot("tu03_viewfinder");
        s.Throw();
        Frames(18);
        Shot("tu04_fruit");
        Frames(60 * 3);
        s.Whistle();
        Frames(10);
        Shot("tu05_whistle");
        var ride = s.Ride!;
        Console.WriteLine($"tour: weather {ride.Weather} hour {ride.Hour} seed {ride.Seed}");
        // On to the end of the rail (or the film), the closing caught half way
        for (int i = 0; i < 60 * 200 && s.Phase == TourPhase.Riding; i++) Frames(1);
        Frames(24);
        Shot("tu08_ride_end");
        for (int i = 0; i < 120 && s.Phase != TourPhase.Judged; i++) Frames(1);
        Frames(30);
        Shot("tu07_judged");
        Console.WriteLine($"tour: {ride.Photos.Count} photographs, total {s.Total}, rares {string.Join(", ", Enumerable.Range(0, ride.Course.Residents.Count).Where(ride.RareSeen).Select(i => ride.Course.Residents[i].Species))}");
        foreach (var b in s.Shown) Console.WriteLine($"  {b.Species} {b.Stars}★ {b.Points}");
        game.KeepTourPhoto();
        Frames(3);
        Shot("tu09_kept");
        Console.WriteLine($"book {game.PhotoBook.Count}: {string.Join(", ", game.PhotoBook.Prints.Select(x => x.File + "@" + x.How + " " + x.Lead))}; report {game.TourReport.Total}");
        s.Leave();
        Frames(10);
    }
}
