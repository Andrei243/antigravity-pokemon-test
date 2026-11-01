using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    /// <summary>
    /// The Abandoned Ship, Artisan Cave and the last shut rooms of Hoenn (plan 21 · H12, part i), after Mirage Island's at
    /// the end of the <c>hoenn</c> mode (<c>HOENN_FROM=postgame</c>), or alone (<c>HOENN_FROM=ship</c>): the wreck from
    /// Route 108 (<c>h2e0</c>), its deck, corridors and cabins (<c>h2e1</c> to <c>h2e4</c>), the captain's office
    /// (<c>h2e5</c>), the storage room's door locked, opened by the Storage Key and the room behind it (<c>h2e6</c> to
    /// <c>h2e8</c>), the flooded cabin and the two passages under it (<c>h2e9</c> to <c>h2eb</c>), the hidden floor and a
    /// cabin's door opened by its key (<c>h2ec</c> to <c>h2ef</c>), Stern's trade on Slateport's quay (<c>h2eg</c>),
    /// Artisan Cave's two floors (<c>h2eh</c>, <c>h2ei</c>), Fortree's decoration shop and its desks (<c>h2ej</c>,
    /// <c>h2ek</c>), Mossdeep's game corner (<c>h2el</c>) and Sootopolis's house (<c>h2em</c>). It prints the doors' flags
    /// and where each dive and each way came out.
    /// </summary>
    void HoennAbandonedShipShots()
    {
        if (!HoennReady("the Abandoned Ship")) return;
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void ReadOn()
        {
            for (int i = 0; i < 1800 && (engine.ScriptRunning || game.State != GameState.Overworld); i++)
            {
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            Frames(6);
        }
        // Reads on through the lines said before a list of answers opens
        void ToChoice(string what)
        {
            for (int i = 0; i < 900 && !engine.Choice.IsOpen; i++)
            {
                if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            if (!engine.Choice.IsOpen) Console.WriteLine($"  !! never happened: {what}");
        }
        // Speaks to whoever stands on a tile (or reads it), and shoots the first line once it is whole
        void Read(string shot)
        {
            game.Interact(); Frames(3);
            Until(() => Box().IsActive && Box().IsCurrentLineComplete, $"a line for {shot}", 600);
            Shot(shot);
            ReadOn();
        }
        void Place(string map, int x, int y, Direction facing, TravelMode mode = TravelMode.OnFoot)
        {
            At(map, x, y, facing);
            if (mode != TravelMode.OnFoot) game.Player.SetMode(mode);
            game.LocationSign.Hide();
            Frames(20);
        }
        void Flags(string label, params string[] flags) =>
            Console.WriteLine($"  {label}: " + string.Join(", ", flags.Select(f => $"{f[5..]} {(game.Story.Has(f) ? "set" : "clear")}")));
        void Dive(string what)
        {
            game.Interact(); Frames(3);
            Until(() => Box().IsActive || engine.Choice.IsOpen, $"Dive's question at {what}", 300);
            ReadOn();
            Frames(30);
            game.LocationSign.Hide();
            Console.WriteLine($"  {what}: now {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY}), {game.Player.Mode}");
        }

        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
        var party = game.Party;
        party.Clear();
        var diver = new Pokemon(PokemonDatabase.Get("Swampert")!, 50);
        diver.Moves.Clear();
        foreach (string move in new[] { "Surf", "Dive", "Waterfall", "Strength" }) diver.Moves.Add(new Move(MoveDatabase.Get(move)!));
        party.Add(diver);
        party.Add(new Pokemon(PokemonDatabase.Get("Manectric")!, 50));
        game.Player.Moves = FieldMovement.MovesOf(party);
        game.Story.SetBadges("Hoenn", 0xFF);
        game.Story.Set("HoennHallOfFame");
        game.Story.Set("FLAG_SYS_POKENAV_GET");

        // ---- The wreck in Route 108's sea, and aboard
        Place("Hoenn", 169, 369, Direction.Up, TravelMode.Surfing);
        Shot("h2e0_route108_abandoned_ship");
        Place("AbandonedShip_Deck", 13, 13, Direction.Up);
        Shot("h2e1_deck");
        Place("AbandonedShip_Corridors_1F", 9, 10, Direction.Up);
        Shot("h2e2_corridors_1f");
        Place("AbandonedShip_Rooms_1F", 10, 13, Direction.Up);
        Shot("h2e3_cabins_1f_thalia");
        Place("AbandonedShip_Rooms2_1F", 6, 15, Direction.Up);
        Shot("h2e4_cabins_kira_and_dan");
        Place("AbandonedShip_CaptainsOffice", 4, 5, Direction.Left);
        Shot("h2e5_captains_office");

        // ---- The storage room's door, locked and opened
        Place("AbandonedShip_Corridors_B1F", 11, 5, Direction.Up);
        Read("h2e6_storage_door_locked");
        game.Bag.AddItem(ItemDatabase.Get("Storage Key")!, 1);
        Read("h2e7_storage_key_turned");
        Flags("the storage room", "FLAG_USED_STORAGE_KEY");
        Console.WriteLine($"  the way through: {game.Map.GetWarpAt(11, 4)?.TargetMap ?? "shut"}");
        Place("AbandonedShip_Room_B1F", 4, 6, Direction.Up);
        Shot("h2e8_storage_room");

        // ---- The flooded cabin and the passages under the ship
        Place("AbandonedShip_Rooms_B1F", 13, 6, Direction.Down, TravelMode.Surfing);
        Shot("h2e9_flooded_cabin");
        Place("AbandonedShip_Rooms_B1F", 13, 7, Direction.Down, TravelMode.Surfing);
        Dive("the flooded cabin");
        Shot("h2ea_flooded_passage_2");
        Place("AbandonedShip_Underwater1", 5, 4, Direction.Up, TravelMode.Diving);
        Shot("h2eb_flooded_passage_1");
        Dive("the first passage");
        Shot("h2ec_hidden_floor");

        // ---- A cabin of the hidden floor, locked and opened by its key
        Place("AbandonedShip_HiddenFloorCorridors", 3, 7, Direction.Down);
        Read("h2ed_room1_locked");
        game.Bag.AddItem(ItemDatabase.Get("Room 1 Key")!, 1);
        game.Interact(); Frames(3); ReadOn();
        Flags("Room 1", "FLAG_USED_ROOM_1_KEY");
        Shot("h2ee_room1_open");
        Place("AbandonedShip_HiddenFloorRooms", 6, 12, Direction.Up);
        Shot("h2ef_hidden_cabins");

        // ---- Stern's trade on Slateport's quay
        game.Bag.AddItem(ItemDatabase.Get("Scanner")!, 1);
        game.Story.Set("FLAG_TEAM_AQUA_ESCAPED_IN_SUBMARINE");
        game.Story.Unset("FLAG_HIDE_SLATEPORT_CITY_HARBOR_CAPTAIN_STERN");
        Place("SlateportCity_Harbor", 6, 14, Direction.Up);
        game.ArriveOnMap(); Frames(10); ReadOn();
        var stern = game.Map.NPCs.FirstOrDefault(n => n.Key == "slateport_harbor_capt_stern");
        if (stern == null) Console.WriteLine("  !! Captain Stern is not on the quay");
        else
        {
            Place("SlateportCity_Harbor", stern.GridX, stern.GridY + 1, Direction.Up);
            game.Interact(); Frames(3);
            ToChoice("Stern's choice");
            Frames(10);
            Shot("h2eg_stern_scanner_trade");
            ReadOn();
            Flags("the Scanner", "FLAG_EXCHANGED_SCANNER");
        }

        // ---- Artisan Cave, and the last rooms of three towns
        Place("ArtisanCave_1F", 10, 15, Direction.Up);
        Shot("h2eh_artisan_cave_1f");
        Place("ArtisanCave_B1F", 8, 46, Direction.Up);
        Shot("h2ei_artisan_cave_b1f");
        Place("FortreeCity_DecorationShop", 3, 4, Direction.Up);
        Shot("h2ej_fortree_decoration_shop");
        Place("FortreeCity_DecorationShop", 1, 4, Direction.Up);
        game.Interact(); Frames(3);
        ToChoice("the desks' list");
        Frames(10);
        Shot("h2ek_decoration_shop_desks");
        engine.Choice.Back(); Frames(4); ReadOn();
        Place("MossdeepCity_GameCorner_1F", 5, 7, Direction.Up);
        Shot("h2el_mossdeep_game_corner");
        Place("SootopolisCity_MysteryEventsHouse_1F", 4, 5, Direction.Up);
        Shot("h2em_sootopolis_mystery_events_house");
    }
}
