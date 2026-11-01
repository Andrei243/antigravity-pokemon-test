using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    /// <summary>
    /// Hoenn's Battle Tents and the Lilycove Lady (plan 21 · H3's last part and H11, part c; not part of <c>all</c>;
    /// <c>ht*</c>): a new game in Hoenn with a team of three; for each tent its lobby before the attendant, its corridor and
    /// its battle room with a trainer of the round stood before the player (<c>ht01</c> to <c>ht09</c>: Verdanturf,
    /// Fallarbor, Slateport); Slateport's six rentals on the Factory's own screen (<c>ht10_slateport_rentals</c>); and the
    /// Favor Lady by the window of Lilycove's Pokémon Center (<c>ht11_lilycove_lady</c>). It prints each round's trainers
    /// and their level, and the rentals offered.
    /// </summary>
    public void HoennTentsMode()
    {
        engine.NewGameRegion = "Hoenn";
        engine.StartNewGame();
        game.Story.SetVar("VAR_LITTLEROOT_INTRO_STATE", 7);
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
        Frames(20);

        var team = game.Party;
        team.Clear();
        team.Add(new Pokemon(PokemonDatabase.Get("Combusken")!, 24));
        team.Add(new Pokemon(PokemonDatabase.Get("Lombre")!, 21));
        team.Add(new Pokemon(PokemonDatabase.Get("Breloom")!, 23));

        var random = new System.Random(21);
        int Draw() => random.Next(0x10000);
        int n = 1;
        foreach (var (tent, town, px, ox) in new[]
        {
            (BattleTent.Verdanturf, "VerdanturfTown", 2, 11),
            (BattleTent.Fallarbor, "FallarborTown", 4, 5),
            (BattleTent.Slateport, "SlateportCity", 4, 5)
        })
        {
            var round = BattleVenueChallenge.BeginTent(tent, new[] { 0, 1, 2 }, team, BattleVenueChallenge.TentMinimumLevel, Draw);
            Console.WriteLine($"{tent}: trainers at level {round.FoeLevel}: "
                + string.Join(", ", round.Foes.Select(f => $"{f.Record.Class} {f.Record.Name} ({string.Join("/", f.Pokemon.Select(p => p.Species.Name))})")));
            string key = tent.ToString().ToLowerInvariant();

            game.Story.Unset("FLAG_HOENN_TENT_OPPONENT");
            At(town + "_BattleTentLobby", 6, 6, Direction.Up);
            Frames(20);
            game.LocationSign.Hide();
            Shot($"ht{n++:00}_{key}_lobby");

            At(town + "_BattleTentCorridor", 2, 7, Direction.Up);
            Frames(20);
            Shot($"ht{n++:00}_{key}_corridor");

            // A trainer of the round walked down to face the player, as the room's arrival script leaves them
            // (dressed before the player comes in, so that the room's arrival makes their look ready)
            game.Story.Set("FLAG_HOENN_TENT_OPPONENT");
            var room = MapDatabase.Get(town + "_BattleTentBattleRoom");
            room.ApplyPresence(game.Story.Has);
            if (room.FindPerson("opponent") is { } opponent)
            {
                var foe = round.Foes[0].Record;
                opponent.NpcType = FrontierTrainers.LookOf(foe);
                opponent.Name = $"{foe.Class} {foe.Name}";
                opponent.GridX = ox;
                opponent.GridY = 5;
                opponent.Facing = Direction.Left;
            }
            else Console.WriteLine($"  !! nobody stands as the opponent in {town}'s battle room");
            At(town + "_BattleTentBattleRoom", px, 5, Direction.Right);
            Frames(30);
            Shot($"ht{n++:00}_{key}_battle_room");

            if (tent == BattleTent.Slateport)
            {
                Console.WriteLine("  rentals offered: " + string.Join(", ", round.Offer.Select(p => $"{p.Species.Name} {p.Level}")));
                foreach (var p in round.Offer) PokemonSprites.Request(p.Species.Name);
                game.FactoryRentals.OpenToRent(round.Offer, 3);
                game.State = GameState.FactoryRentals;
                for (int i = 0; i < 60; i++) Frames(1);
                Shot($"ht{n++:00}_slateport_rentals");
                game.FactoryRentals.Cancel();
                game.State = GameState.Overworld;
                Frames(4);
            }
        }
        game.Story.Unset("FLAG_HOENN_TENT_OPPONENT");

        // The Favor Lady by the window of Lilycove's Pokémon Center
        var center = MapDatabase.Get("LilycoveCity_PokemonCenter_1F");
        if (center.FindPerson("lilycove_lady") is { } lady)
        {
            At("LilycoveCity_PokemonCenter_1F", lady.GridX + 1, lady.GridY, Direction.Left);
            Frames(20);
            game.LocationSign.Hide();
            Shot($"ht{n++:00}_lilycove_lady");
        }
        else Console.WriteLine("  !! no Lilycove Lady in Lilycove's Pokémon Center");
    }
}
