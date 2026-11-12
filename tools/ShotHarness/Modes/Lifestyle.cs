using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    // ---------------------------------------------------------------- the player's life (plan 43)

    // The card the player designs, the salon and the dye house, photographs (lf*; not part of "all"; LIFESTYLE_PART=card,
    // salon or photo runs one): LF1's card before and after its design and the designer's rows, pad and six (lf10 to lf17),
    // LF3's chair and counter and the player restyled in the field (lf30 to lf36), LF4's pose in photo mode, a spot's
    // plate and view and the book's page and opened photograph (lf40 to lf47). Everything through the game's own screens.
    public void LifestyleMode()
    {
        string part = Environment.GetEnvironmentVariable("LIFESTYLE_PART") ?? "";
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        if (game.Party.Count < 3)
            foreach (string species in new[] { "Staravia", "Luxio", "Buizel" })
                game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, 22));
        if (part is "" or "card") LifestyleCard();
        if (part is "" or "salon") LifestyleSalon();
        if (part is "" or "photo") LifestylePhoto();
        if (part is "studio") LifestyleStudio();
    }

    private void LifestyleCard()
    {
        At("Sinnoh", 116, 887, Direction.Down);
        game.LocationSign.Hide();
        Frames(10);
        game.ChooseFromStartMenu(StartMenuChoice.Trainer);
        Frames(30);
        Shot("lf10_card_front_plain");
        game.TrainerCard.AskForDesigner();
        Frames(20);
        var d = game.CardDesigner;
        Shot("lf11_card_designer");
        d.Change(1); d.Change(1);
        d.Move(1); d.Change(1); d.Change(1);
        d.Move(1); d.Change(1); d.Change(1); d.Change(1);
        d.Move(1); d.Change(1);
        Frames(40);
        Shot("lf12_card_pose");
        Console.WriteLine($"design: {d.Design.Background} {d.Design.Frame} {d.Design.Pose} {d.Design.Gesture} {d.Design.Mood}");
        d.Move(-3); d.Change(1);
        Frames(10);
        Shot("lf13_card_background");
        d.Move(5);
        d.Confirm();
        d.Move(1); d.Confirm();
        Frames(10);
        Shot("lf14_card_six");
        d.Cancel();
        game.Story.SetPhrase(CardDesign.GreetingPhrase, new[] { "feelings:happy", "small:together" });
        d.Greeting = PhraseGroups.Shown(game.Story.PhraseOf(CardDesign.GreetingPhrase)!);
        d.Move(2);
        d.Confirm();
        // A signature of three strokes
        d.PenTo(20, 30); d.Confirm();
        foreach (var (x, y) in new[] { (30, 10), (40, 34), (50, 12), (62, 32), (78, 18), (100, 26), (130, 20) }) d.PenTo(x, y);
        d.Confirm();
        Frames(10);
        Shot("lf15_card_signature");
        d.Cancel(); d.Cancel();
        Frames(30);
        Shot("lf16_card_front_designed");
        game.TrainerCard.Flip();
        Frames(30);
        Shot("lf17_card_back_signed");
        var face = game.CardNow.Design;
        Console.WriteLine($"face: {face?.Background?.Id} {face?.Frame?.Id} shown {face?.Shown.Count} greeting '{face?.Greeting}' signed {face?.Signature?.Count}");
        game.TrainerCard.Close();
        Frames(5);
    }

    private void LifestyleSalon()
    {
        game.Money = Math.Max(game.Money, 50000);
        var berry = ItemDatabase.Get("Lum Berry")!;
        game.Bag.AddItem(berry, 2);
        game.Wardrobe.Give("striped_tee");
        At("JubilifeBoutique", 2, 4, Direction.Left);
        game.LocationSign.Hide();
        Frames(10);
        game.OpenSalon(dye: false);
        Frames(30);
        var s = game.Salon;
        Shot("lf30_salon");
        while (s.Selected != "Ponytail") s.Move(1);
        Frames(40);
        Shot("lf31_salon_cut");
        s.Confirm(game.Money, _ => { }); game.Money += s.Confirm(game.Money, _ => { });
        s.MoveTab(1);
        while (s.Selected != "teal") s.Move(1);
        s.Confirm(game.Money, _ => { }); game.Money += s.Confirm(game.Money, _ => { });
        Frames(40);
        Shot("lf32_salon_colour");
        s.MoveTab(1);
        while (s.Selected != "green") s.Move(1);
        s.Confirm(game.Money, _ => { }); game.Money += s.Confirm(game.Money, _ => { });
        Frames(40);
        Shot("lf33_salon_eyes");
        s.Cancel();
        Frames(5);
        game.OpenSalon(dye: true);
        Frames(20);
        while (s.Selected != "striped_tee") s.Move(1);
        s.Confirm(game.Money, _ => { });
        while (s.Selected != "mint") s.Move(1);
        Frames(40);
        Shot("lf34_dye_counter");
        s.Confirm(game.Money, _ => { }); game.Money += s.Confirm(game.Money, _ => { });
        Frames(40);
        Shot("lf35_dyed");
        s.Cancel();
        Frames(10);
        Console.WriteLine($"worn: {game.Wardrobe.Worn.Key} money {game.Money} berries {game.Bag.GetQuantity(berry)}");
        At("Sinnoh", 140, 790, Direction.Down);
        game.LocationSign.Hide();
        Frames(60);
        Shot("lf36_restyled_in_the_field");
    }

    private void LifestylePhoto()
    {
        // The pose in photo mode, before the house
        At("Sinnoh", 113, 888, Direction.Down);
        game.LocationSign.Hide();
        Frames(20);
        if (game.OpenPhoto() && game.Photo is { } p)
        {
            p.PanelOpen = true;
            while (p.Row != PhotoRow.Pose) p.MoveRow(1);
            p.Change(3);
            p.MoveRow(1);
            p.Change(1);
            Frames(30);
            Shot("lf40_photo_pose");
            game.ClosePhoto();
            Frames(20);
        }
        // The plate by Twinleaf's pond, read: its view, and a photograph into the book
        Shot("lf42_spot_plate");
        game.Interact();
        Frames(40);
        Shot("lf43_spot_view");
        Console.WriteLine($"state {game.State}, photo {(game.Photo?.SpotId ?? "none")}");
        if (game.Photo != null)
        {
            game.TakePhoto();
            Frames(40);
        }
        Shot("lf44_spot_taken");
        Console.WriteLine($"book {game.PhotoBook.Count}: {string.Join(", ", game.PhotoBook.Prints.Select(x => x.File + "@" + x.Spot))}");
        game.OpenPhotoBook();
        Frames(30);
        Shot("lf45_book_page");
        game.PhotoBookScreen.Confirm();
        Frames(20);
        Shot("lf46_book_opened");
        game.PhotoBookScreen.Cancel();
        game.PhotoBookScreen.Cancel();
        Frames(5);
    }
}
