using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    // Plan 43 · LF6 (lf60 to lf66; LIFESTYLE_PART=phone): the travel phone in Unova, its CALLS, Bianca's call with her
    // face, Hugh ringing, the day's NEWS, POWERS with the Power Watch; then Kalos's MAP and Shauna's call with her face.
    // Everything through the game's own screens and calls.
    private void LifestylePhone()
    {
        game.Story.Set(TravelPhone.Flag);
        foreach (var region in new[] { RegionDatabase.Unova, RegionDatabase.Kalos })
            foreach (var c in PhoneBook.Of(region)) Pokegear.Register(game.Story, c.Id);
        if (ItemDatabase.Get(GameEngine.PowerWatchItem) is { } watch && game.Bag.GetQuantity(watch) == 0) game.Bag.AddItem(watch, 1);

        At("Unova", 48, 742, Direction.Down);
        game.LocationSign.Hide();
        Frames(20);
        game.OpenTravelPhone();
        Frames(30);
        var phone = game.TravelPhoneScreen;
        Console.WriteLine($"phone cards: {string.Join(", ", phone.Cards)}; numbers {string.Join(", ", phone.Contacts.Select(c => c.Id))}");
        phone.Move(0, 1);
        Frames(10);
        Shot("lf60_phone_calls");
        phone.Cancel();
        Frames(5);

        ShootACall(PhoneBook.Get("unova_bianca")!, incoming: false, "lf61_phone_call_face");
        ShootACall(PhoneBook.Get("unova_hugh")!, incoming: true, "lf62_phone_ringing");

        game.OpenTravelPhone(PhoneCard.News);
        Frames(30);
        Shot("lf63_phone_news");
        game.TravelPhoneScreen.Cancel();
        Frames(5);
        game.OpenTravelPhone(PhoneCard.Powers);
        Frames(30);
        Shot("lf65_phone_unova");
        game.TravelPhoneScreen.Cancel();
        Frames(5);

        At("Kalos", 242, 724, Direction.Down);
        game.LocationSign.Hide();
        Frames(20);
        game.OpenTravelPhone(PhoneCard.Map);
        Frames(30);
        Shot("lf64_phone_map_kalos");
        game.TravelPhoneScreen.Cancel();
        Frames(5);
        ShootACall(PhoneBook.Get("kalos_shauna")!, incoming: false, "lf66_phone_call_shauna");
    }

    // A call placed (or rung) and shot once its words are up and the caller's face is rendered, then read to its end
    private void ShootACall(PhoneContact contact, bool incoming, string name)
    {
        game.State = GameState.Overworld;
        game.PlaceCall(contact, incoming);
        for (int i = 0; i < 600 && !(game.CallFaceRendered && game.State == GameState.Dialogue); i++) Frames(1);
        Frames(40);
        Console.WriteLine($"{name}: {contact.Id} face {game.CallFaceLook ?? "none"} rendered {game.CallFaceRendered}");
        Shot(name);
        for (int i = 0; i < 40 && game.ScriptNow != null; i++)
        {
            if (game.Dialogue.IsActive) { game.Dialogue.FinishLine(); Frames(2); game.Dialogue.Advance(); }
            Frames(10);
        }
        Frames(10);
    }
}
