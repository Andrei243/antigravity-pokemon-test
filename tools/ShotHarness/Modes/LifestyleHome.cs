using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    // Plan 43 · LF7 (lf70 to lf77; LIFESTYLE_PART=home): the home's door at a tree of Sinnoh's with its way in drawn, the
    // home made with two pieces and its door moved to a spot of Unova's and of Kalos's with every piece where it was, the
    // six kinds' rooms, the bedrooms of Twinleaf Town and Vaniville Town decorated, and Johto's Mom asking to keep the
    // prize money and ringing to say what she bought. LIFESTYLE_PART=spots shoots every spot of the home and every photo
    // spot of the five regions (lf78_*, lf79_*) to look at where each stands.
    private void LifestyleHome()
    {
        foreach (string name in new[] { "Small Desk", "Pikachu Doll", "Ball Poster", "Red Plant", "Surf Mat" })
            SecretBases.Add(game.Decorations, DecorationDatabase.IdOf(name));

        // lf70: a tree of Sinnoh's with the home's door at it
        var sinnoh = MapDatabase.Get("Sinnoh");
        var spot = sinnoh.SecretBaseSpots.First();
        var (bx, by) = Before(sinnoh, spot.Key.X, spot.Key.Y);
        var layout = SecretBaseLayouts.Of(spot.Value)!;
        var home = new SecretBaseSave { Id = spot.Value, Map = "Sinnoh", X = spot.Key.X, Y = spot.Key.Y, OutX = bx, OutY = by };
        game.SecretBaseSet(home);
        // (a tile further off, so the way in isn't behind the player)
        At("Sinnoh", bx + (bx - spot.Key.X), by + (by - spot.Key.Y), FacingTo(bx, by, spot.Key.X, spot.Key.Y));
        game.LocationSign.Hide();
        game.KeepTheBaseEntrance();
        Frames(30);
        Console.WriteLine($"lf70: the home's door at {spot.Key.X},{spot.Key.Y} of Sinnoh, a {layout.Kind}, the player on {bx},{by}");
        Shot("lf70_spot_sinnoh");

        // Two pieces set down in the room, then the door moved to a spot of Unova's: the room as it was
        home.Placed.Add(new PlacedDecoration { Slot = 0, X = layout.PcFrontX + 1, Y = layout.PcFrontY + 1 });
        home.Placed.Add(new PlacedDecoration { Slot = 1, X = layout.PcFrontX - 1, Y = layout.PcFrontY + 1 });
        Bedrooms.MarkOut(game.Decorations, home, game.Bedrooms);
        MoveTheHomeTo("Unova", "lf71_home_opened_in_unova");
        MoveTheHomeTo("Kalos", "lf72_home_moved_kept");

        // lf73: the six kinds' rooms, each from inside its door
        foreach (var kind in new[] { SecretBaseKind.RedCave, SecretBaseKind.BrownCave, SecretBaseKind.BlueCave, SecretBaseKind.YellowCave, SecretBaseKind.Tree, SecretBaseKind.Shrub })
        {
            var room = SecretBaseLayouts.All.First(l => l.Kind == kind);
            var map = MapDatabase.Get(room.Map);
            var door = map.Warps.First();
            At(room.Map, door.SourceX, door.SourceY - 1, Direction.Up);
            game.LocationSign.Hide();
            Frames(30);
            Console.WriteLine($"lf73: {room.Map} ({map.Interior})");
            Shot($"lf73_home_kinds_{kind}");
        }

        // lf74, lf75: the bedrooms, decorated as their computers decorate them
        DecorateTheBedroom(RegionDatabase.Sinnoh, "PlayerHouse2F", new[] { 2, 3 }, "lf74_bedroom_sinnoh");
        DecorateTheBedroom(RegionDatabase.Kalos, "VanivilleTown_PlayersHouse_2F", new[] { 4 }, "lf75_bedroom_kalos");

        // lf76, lf77: Johto's Mom asked to keep a share, and her call once she has bought something
        At("NewBark_PlayersHouse_1F", 4, 5, Direction.Up);
        game.LocationSign.Hide();
        Frames(20);
        engine.StartScript("MomSavings", file: "new_bark_player_house_1f");
        for (int i = 0; i < 300 && !engine.Choice.IsOpen; i++)
        {
            if (game.Dialogue.IsActive) { game.Dialogue.FinishLine(); Frames(2); game.Dialogue.Advance(); }
            Frames(2);
        }
        Frames(20);
        Shot("lf76_mom_savings_ask");
        engine.Choice.Confirm();
        for (int i = 0; i < 60 && game.ScriptNow != null; i++)
        {
            if (game.Dialogue.IsActive) { game.Dialogue.FinishLine(); Frames(2); game.Dialogue.Advance(); }
            Frames(6);
        }
        game.Story.SetVar("VAR_JOHTO_MOM_SAVINGS", 3000);
        game.Story.Set("FLAG_JOHTO_MOM_SAVING");
        game.State = GameState.Overworld;
        if (PhoneBook.Get("mother") is { } mother)
        {
            Pokegear.Register(game.Story, mother.Id);
            // Rung until she has bought something with the savings, read to the line that says what
            bool shot = false;
            for (int call = 0; call < 8 && !shot; call++)
            {
                game.State = GameState.Overworld;
                game.PlaceCall(mother, incoming: true);
                for (int i = 0; i < 400 && game.ScriptNow != null && !shot; i++)
                {
                    if (game.Dialogue.IsActive && game.Dialogue.CurrentLine.Contains("savings", StringComparison.Ordinal))
                    {
                        game.Dialogue.FinishLine();
                        Frames(30);
                        Shot("lf77_mom_call_bought");
                        shot = true;
                    }
                    else if (game.Dialogue.IsActive) { game.Dialogue.FinishLine(); Frames(2); game.Dialogue.Advance(); }
                    Frames(4);
                }
                for (int i = 0; i < 60 && game.ScriptNow != null; i++)
                {
                    if (game.Dialogue.IsActive) { game.Dialogue.FinishLine(); Frames(2); game.Dialogue.Advance(); }
                    Frames(4);
                }
            }
            Console.WriteLine($"lf77: Mom holds {game.Story.Var("VAR_JOHTO_MOM_SAVINGS")} after her calls; the decoration PC holds {game.Decorations.Count}; shot {shot}");
        }
        game.SecretBaseSet(null);
    }

    // The door of the home moved to the first spot of a region's map through the game's own `openbase move`, and shot in the room
    private void MoveTheHomeTo(string region, string name)
    {
        var map = MapDatabase.Get(region);
        var spot = map.SecretBaseSpots.First();
        var (bx, by) = Before(map, spot.Key.X, spot.Key.Y);
        At(region, bx, by, FacingTo(bx, by, spot.Key.X, spot.Key.Y));
        game.LocationSign.Hide();
        Frames(10);
        engine.SecretBaseAct("move");
        for (int i = 0; i < 240 && !(SecretBaseLayouts.IsBaseMap(game.Map.Name) && game.State == GameState.Overworld); i++) Frames(1);
        game.LocationSign.Hide();
        Frames(30);
        Console.WriteLine($"{name}: door at {spot.Key.X},{spot.Key.Y} of {region}; in {game.Map.Name}; pieces {game.SecretBase!.Placed.Count}; props {game.Map.Props.Count(p => p.Type == PropType.Decoration)}");
        Shot(name);
    }

    private void DecorateTheBedroom(string region, string mapName, int[] slots, string name)
    {
        var room = MapDatabase.Get(mapName);
        var floor = Bedrooms.FloorOf(room, region);
        var free = floor.Floor.Concat(floor.Wall).Where(t => !floor.KeptClear.Contains(t)).OrderBy(t => t.Item2).ThenBy(t => t.Item1).ToList();
        var placed = new List<PlacedDecoration>();
        int at = 0;
        foreach (int slot in slots)
        {
            var d = DecorationDatabase.Get(game.Decorations[slot].Kind)!;
            while (at < free.Count && (!floor.Fits(d, free[at].Item1, free[at].Item2) || placed.Any(p => Math.Abs(p.X - free[at].Item1) < 2 && Math.Abs(p.Y - free[at].Item2) < 2))) at++;
            if (at >= free.Count) break;
            placed.Add(new PlacedDecoration { Slot = slot, X = free[at].Item1, Y = free[at].Item2 });
        }
        game.Bedrooms[region] = placed;
        Bedrooms.MarkOut(game.Decorations, game.SecretBase, game.Bedrooms);
        var start = RegionDatabase.Get(region)?.Start;
        At(mapName, start?.Map == mapName ? start.X : room.Warps[0].SourceX, start?.Map == mapName ? start.Y + 1 : room.Warps[0].SourceY - 1, Direction.Up);
        game.LocationSign.Hide();
        game.LayOutBedroom(game.Map);
        Frames(30);
        Console.WriteLine($"{name}: {string.Join(", ", placed.Select(p => $"{DecorationDatabase.Get(game.Decorations[p.Slot].Kind)!.Name} at {p.X},{p.Y}"))}; kept clear {floor.KeptClear.Count} of {floor.Floor.Count}");
        Shot(name);
        // and its computer's decorating opened over the room's plan
        if (game.OpenBedroomDecoration())
        {
            Frames(20);
            Shot(name + "_plan");
            game.Decoration.Close();
            Frames(5);
            game.State = GameState.Overworld;
        }
    }

    // Every spot of the home and every photo spot of the five regions, faced from the tile before it (lf78, lf79)
    private void LifestyleSpots()
    {
        foreach (string region in new[] { RegionDatabase.Sinnoh, RegionDatabase.Kanto, RegionDatabase.Johto, RegionDatabase.Unova, RegionDatabase.Kalos })
        {
            var map = MapDatabase.Get(region);
            int n = 0;
            foreach (var spot in map.SecretBaseSpots.OrderBy(s => s.Key.Y).ThenBy(s => s.Key.X))
            {
                var (bx, by) = Before(map, spot.Key.X, spot.Key.Y);
                game.SecretBaseSet(new SecretBaseSave { Id = spot.Value, Map = region, X = spot.Key.X, Y = spot.Key.Y, OutX = bx, OutY = by });
                At(region, bx + (bx - spot.Key.X), by + (by - spot.Key.Y), FacingTo(bx, by, spot.Key.X, spot.Key.Y));
                game.LocationSign.Hide();
                game.KeepTheBaseEntrance();
                Frames(20);
                string key = map.AreaAt(spot.Key.X, spot.Key.Y)?.Key ?? "?";
                Console.WriteLine($"lf78 {region} {n}: {key} spot {spot.Key.X},{spot.Key.Y} ({SecretBaseLayouts.Of(spot.Value)!.Kind}) tile {map.GetGroundTile(spot.Key.X, spot.Key.Y)}, faced from {bx},{by}");
                Shot($"lf78_base_{region.ToLowerInvariant()}_{n++}_{key}");
            }
        }
        game.SecretBaseSet(null);
        foreach (var photo in PhotoSpots.All.Where(p => p.Region != RegionDatabase.Hoenn))
        {
            var map = MapDatabase.Get(photo.Map);
            var (bx, by) = Before(map, photo.X, photo.Z);
            At(photo.Map, bx, by, FacingTo(bx, by, photo.X, photo.Z));
            game.LocationSign.Hide();
            Frames(20);
            Console.WriteLine($"lf79 {photo.Id}: plate {photo.X},{photo.Z} tile {map.GetGroundTile(photo.X, photo.Z)} solid {map.IsSolid(photo.X, photo.Z)}, faced from {bx},{by}");
            Shot($"lf79_photo_{photo.Id}");
        }
    }

    // The open tile the player faces a spot from: south first, as Hoenn's are faced
    private static (int X, int Y) Before(Map map, int x, int y)
    {
        foreach (var (dx, dy) in new[] { (0, 1), (0, -1), (-1, 0), (1, 0) })
            if (map.IsWalkable(x + dx, y + dy) && map.GetWarpAt(x + dx, y + dy) == null) return (x + dx, y + dy);
        return (x, y + 1);
    }

    private static Direction FacingTo(int fromX, int fromY, int x, int y) =>
        y < fromY ? Direction.Up : y > fromY ? Direction.Down : x < fromX ? Direction.Left : Direction.Right;
}
