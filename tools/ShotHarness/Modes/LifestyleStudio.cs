using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    // Plan 43 · LF5 (lf50 to lf56; LIFESTYLE_PART=studio): the photographer's corner of the Jubilife Boutique, the studio's
    // stage with a backdrop, a pose, a Pokémon and two stickers of Seals owned, a frame, the picture taken and its print
    // in the photo book. Everything through the game's own screens.
    private void LifestyleStudio()
    {
        game.Story.Set(GameEngine.StudioFrameFlag);
        game.SealCase.Give(SealDatabase.Seal("HEART_SEAL_A"), 2);
        game.SealCase.Give(SealDatabase.Seal("STAR_SEAL_A"), 1);
        At("JubilifeBoutique", 7, 6, Direction.Right);
        game.LocationSign.Hide();
        Frames(20);
        Shot("lf50_studio_room");
        game.OpenPhotoStudio();
        Frames(40);
        var s = game.PhotoStudio;
        void GoTo(StudioRow row) { while (s.Row != row) s.Move(1); }
        s.Change(2);
        Frames(30);
        Shot("lf51_studio_backdrop");
        GoTo(StudioRow.Pose); s.Change(4);
        GoTo(StudioRow.Face); s.Change(1);
        GoTo(StudioRow.Pokemon); s.Change(1);
        Frames(40);
        Shot("lf52_studio_pose");
        // Two Seals stuck on: the first where the cursor starts, the second a little up and to the right
        GoTo(StudioRow.Stickers);
        s.Confirm(); s.Confirm(); s.Place();
        s.Change(6); s.Move(-1); s.Move(-1); s.Place();
        s.Cancel(); s.Cancel();
        Frames(30);
        Shot("lf53_studio_stickers");
        GoTo(StudioRow.Frame);
        while (s.Shot.Frame != "studio_gilt") s.Change(1);
        Frames(30);
        Shot("lf54_studio_frame");
        game.TakePhotoStudioPhoto();
        Frames(3);
        Shot("lf55_studio_taken");
        Frames(30);
        Console.WriteLine($"book {game.PhotoBook.Count}: {string.Join(", ", game.PhotoBook.Prints.Select(x => x.File + "@" + x.How + " stickers " + x.Studio?.Stickers.Count))}");
        s.Cancel();
        Frames(5);
        game.OpenPhotoBook();
        Frames(30);
        Shot("lf56_book_studio_print");
        game.PhotoBookScreen.Cancel();
        Frames(5);
    }
}
