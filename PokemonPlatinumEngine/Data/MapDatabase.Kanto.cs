using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Data;

// Kanto, the first region in the chain (see RegionDatabase). Only a stand-in Pallet Town for now: the Kanto
// chapter builds the real maps.
public static partial class MapDatabase
{
    private static Map BuildPalletTown()
    {
        int w = 20, h = 21;
        const int SeaY = 15;
        var map = new Map(w, h)
        {
            Name = "PalletTown",
            DisplayName = "Pallet Town",
            BgmTrack = "Twinleaf",
            Trees = TreeStyle.Round
        };

        // Trees on three sides, the sea on the fourth; Route 1 to the north isn't built yet
        for (int x = 0; x < w; x++) map.SetGroundTile(x, 0, TileType.Tree, isSolid: true);
        for (int y = 0; y < SeaY; y++)
        {
            map.SetGroundTile(0, y, TileType.Tree, isSolid: true);
            map.SetGroundTile(w - 1, y, TileType.Tree, isSolid: true);
        }

        // The road from the north edge down to the pier
        for (int y = 1; y <= 14; y++)
        {
            map.SetGroundTile(9, y, TileType.Path);
            map.SetGroundTile(10, y, TileType.Path);
        }

        // The player's house, with a flower bed in front
        for (int x = 3; x <= 7; x++)
        {
            for (int y = 3; y <= 5; y++) map.SetGroundTile(x, y, TileType.RoofRed, isSolid: true);
            map.SetGroundTile(x, 6, TileType.Wall, isSolid: true);
            map.SetGroundTile(x, 7, TileType.FlowerGrass);
        }
        map.SetGroundTile(5, 6, TileType.Door);
        map.Warps.Add(new Warp { SourceX = 5, SourceY = 6, TargetMap = "PalletPlayerHouse", TargetX = 4, TargetY = 6, TargetFacing = Direction.Up });

        // The sea to the south, with a pier out to where the ferry calls
        for (int y = SeaY; y < h; y++)
            for (int x = 0; x < w; x++)
                map.SetGroundTile(x, y, TileType.Water, isSolid: true);
        for (int y = SeaY; y <= SeaY + 3; y++)
        {
            map.SetGroundTile(9, y, TileType.Path);
            map.SetGroundTile(10, y, TileType.Path);
        }

        // Signboards
        map.SetGroundTile(8, 9, TileType.Signpost, isSolid: true);
        map.Signboards[(8, 9)] = "Pallet Town\nA quiet town where every colour begins.";

        map.SetGroundTile(8, 2, TileType.Signpost, isSolid: true);
        map.Signboards[(8, 2)] = "Route 1 lies north, toward Viridian City.\nThe road is still being laid.";

        map.SetGroundTile(11, 14, TileType.Signpost, isSolid: true);
        map.Signboards[(11, 14)] = "Ferry Pier\nShips to Johto call here for Kanto's Champions.";

        // NPCs
        map.NPCs.Add(new NPC
        {
            Id = "PalletTown.Sailor",
            Name = "Sailor",
            NpcType = "Clerk",
            GridX = 9,
            GridY = SeaY + 3,
            Facing = Direction.Up,
            IsTransportAttendant = true
        });

        map.NPCs.Add(new NPC
        {
            Name = "Townsperson",
            NpcType = "Lady",
            GridX = 6,
            GridY = 11,
            Facing = Direction.Right,
            DialogLines = new()
            {
                "Hello! Everyone in Kanto starts out from a town like this one.",
                "Become Champion here, and the ferry at the pier will take you across the sea to Johto."
            }
        });

        return map;
    }

    private static Map BuildPalletPlayerHouse()
    {
        var map = BuildRoom("PalletPlayerHouse", "Home", InteriorStyle.House, 10, 9, doorX: 4);
        map.Warps.Add(new Warp { SourceX = 4, SourceY = 8, TargetMap = "PalletTown", TargetX = 5, TargetY = 7, TargetFacing = Direction.Down });

        map.AddProp(PropType.Fridge, 1, 2);
        map.AddProp(PropType.KitchenCounter, 2, 2);
        map.AddProp(PropType.Television, 6, 2);
        map.AddProp(PropType.Stairs, 8, 2, depth: 2);
        map.AddProp(PropType.Table, 4, 4, width: 2);
        map.AddProp(PropType.Plant, 1, 7);
        map.AddProp(PropType.Rug, 3, 6, width: 3, depth: 2);
        map.AddProp(PropType.Window, 4, 1, width: 2);

        map.NPCs.Add(new NPC
        {
            Name = "Mom",
            NpcType = "Mom",
            GridX = 6,
            GridY = 4,
            Facing = Direction.Left,
            IsHealingNurse = true,
            DialogLines = new()
            {
                "Mom: Back already? Have a rest, your Pokémon look tired.",
                "Mom: There, all better! Off you go, and say hello to Johto for me one day."
            }
        });

        return map;
    }
}
