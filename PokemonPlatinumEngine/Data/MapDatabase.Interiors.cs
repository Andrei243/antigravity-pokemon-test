using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Data;

public static partial class MapDatabase
{
    /// <summary>Walls round the edge (two rows deep at the back), floor inside, and an exit door in the front wall.</summary>
    private static Map BuildRoom(string name, string displayName, InteriorStyle style, int w, int h, int doorX)
    {
        var map = new Map(w, h)
        {
            Name = name,
            Interior = style,
            DisplayName = displayName,
            BgmTrack = "Twinleaf"
        };

        for (int x = 0; x < w; x++)
        {
            map.SetGroundTile(x, 0, TileType.Wall, isSolid: true);
            map.SetGroundTile(x, 1, TileType.Wall, isSolid: true);
            map.SetGroundTile(x, h - 1, TileType.Wall, isSolid: true);
        }
        for (int y = 0; y < h; y++)
        {
            map.SetGroundTile(0, y, TileType.Wall, isSolid: true);
            map.SetGroundTile(w - 1, y, TileType.Wall, isSolid: true);
        }
        for (int x = 1; x < w - 1; x++)
        {
            for (int y = 2; y < h - 1; y++) map.SetGroundTile(x, y, TileType.Floor);
        }

        // Exit door (must be walkable, it sits in the solid front wall)
        map.SetGroundTile(doorX, h - 1, TileType.Door, isSolid: false);
        return map;
    }

    private static Map BuildPlayerHouse()
    {
        var map = BuildRoom("PlayerHouse", "Lucas's Home", InteriorStyle.House, 10, 9, doorX: 4);
        map.Warps.Add(new Warp { SourceX = 4, SourceY = 8, TargetMap = "TwinleafTown", TargetX = 6, TargetY = 8, TargetFacing = Direction.Down });

        // Kitchen along the back wall, TV, stairs up to Lucas's room, and the dining table
        map.AddProp(PropType.Fridge, 1, 2);
        map.AddProp(PropType.KitchenCounter, 2, 2);
        map.AddProp(PropType.Stove, 3, 2);
        map.AddProp(PropType.Television, 6, 2);
        map.AddProp(PropType.Stairs, 8, 2, depth: 2);
        map.AddProp(PropType.Table, 4, 4, width: 2);
        map.AddProp(PropType.Chair, 3, 4);
        map.AddProp(PropType.Plant, 1, 7);
        map.AddProp(PropType.Plant, 8, 7);
        map.AddProp(PropType.Rug, 3, 6, width: 3, depth: 2);
        map.AddProp(PropType.Window, 4, 1, width: 2);
        map.AddProp(PropType.Clock, 7, 1);

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
                "Mom: Welcome home, honey! You and your Pokémon look like you need a rest.",
                "Mom: ...There! All your Pokémon are fully healed and ready for adventure!",
                "Mom: Don't forget your Running Shoes! Hold B, X or Shift to run anywhere!"
            }
        });

        return map;
    }

    private static Map BuildRivalHouse()
    {
        var map = BuildRoom("RivalHouse", "Barry's Home", InteriorStyle.House, 10, 9, doorX: 5);
        map.Warps.Add(new Warp { SourceX = 5, SourceY = 8, TargetMap = "TwinleafTown", TargetX = 17, TargetY = 8, TargetFacing = Direction.Down });

        map.AddProp(PropType.Bookshelf, 1, 2);
        map.AddProp(PropType.Bookshelf, 2, 2);
        map.AddProp(PropType.Television, 4, 2);
        map.AddProp(PropType.KitchenCounter, 6, 2);
        map.AddProp(PropType.Stove, 7, 2);
        map.AddProp(PropType.Fridge, 8, 2);
        map.AddProp(PropType.Table, 3, 4, width: 2);
        map.AddProp(PropType.Chair, 5, 4);
        map.AddProp(PropType.Sofa, 1, 5, depth: 2);
        map.AddProp(PropType.Plant, 1, 7);
        map.AddProp(PropType.Plant, 8, 7);
        map.AddProp(PropType.Rug, 4, 6, width: 3, depth: 2);
        map.AddProp(PropType.Painting, 3, 1);
        map.AddProp(PropType.Window, 5, 1, width: 2);

        map.NPCs.Add(new NPC
        {
            Name = "Barry's Mom",
            NpcType = "Lady",
            GridX = 7,
            GridY = 4,
            Facing = Direction.Down,
            DialogLines = new()
            {
                "Barry's Mom: Oh, Lucas! Barry just dashed out the door again.",
                "Barry's Mom: He said he'd fine you 10 million if you're late! That boy never slows down...",
                "Barry's Mom: Take care out there, and keep an eye on him for me!"
            }
        });

        return map;
    }

    private static Map BuildPokemonCenter() => BuildPokemonCenter("PokemonCenter", "SandgemTown", 6, 7);

    /// <summary>Every town's Pokémon Center has the same layout; only the street it opens onto differs.</summary>
    private static Map BuildPokemonCenter(string name, string town, int exitX, int exitY)
    {
        var map = BuildRoom(name, "Pokémon Center", InteriorStyle.PokemonCenter, 11, 9, doorX: 5);
        map.Warps.Add(new Warp { SourceX = 5, SourceY = 8, TargetMap = town, TargetX = exitX, TargetY = exitY, TargetFacing = Direction.Down });

        // Reception counter with the healing machine behind it; Nurse Joy is talked to across the counter
        map.AddProp(PropType.Counter, 3, 3, width: 5);
        map.AddProp(PropType.HealingMachine, 7, 2);
        map.AddProp(PropType.Bench, 1, 6, width: 2);
        map.AddProp(PropType.Bench, 8, 6, width: 2);
        map.AddProp(PropType.Plant, 1, 2);
        map.AddProp(PropType.Plant, 9, 7);
        map.AddProp(PropType.Rug, 4, 5, width: 3, depth: 2);
        map.AddProp(PropType.WallEmblem, 5, 1);
        map.AddProp(PropType.Window, 2, 1, width: 2);

        // PC terminal on the right
        map.SetGroundTile(9, 2, TileType.PC, isSolid: true);

        map.NPCs.Add(new NPC
        {
            Name = "Nurse Joy",
            NpcType = "Nurse",
            GridX = 5,
            GridY = 2,
            Facing = Direction.Down,
            IsHealingNurse = true,
            DialogLines = new()
            {
                "Nurse Joy: Hello, and welcome to the Pokémon Center!",
                "Nurse Joy: We restore your tired Pokémon to full health!",
                "Nurse Joy: ...Thank you for waiting. Your Pokémon are fully healed!",
                "Nurse Joy: We hope to see you again!"
            }
        });

        map.NPCs.Add(new NPC
        {
            Name = "PC Terminal",
            NpcType = "Trainer",
            GridX = 9,
            GridY = 2,
            Facing = Direction.Up,
            IsPCTerminal = true
        });

        return map;
    }

    private static Map BuildPokeMart() => BuildPokeMart("PokeMart", "SandgemTown", "Sandgem", 22, 7);

    /// <summary>Every town's Poké Mart has the same layout; the clerk greets you with the town's name.</summary>
    private static Map BuildPokeMart(string name, string town, string townName, int exitX, int exitY)
    {
        var map = BuildRoom(name, "Poké Mart", InteriorStyle.PokeMart, 9, 8, doorX: 4);
        map.Warps.Add(new Warp { SourceX = 4, SourceY = 7, TargetMap = town, TargetX = exitX, TargetY = exitY, TargetFacing = Direction.Down });

        // Cash counter on the left with the clerk behind it, shelves of goods on the right
        map.AddProp(PropType.Counter, 2, 3, depth: 2);
        map.AddProp(PropType.StoreShelf, 4, 2, width: 3);
        map.AddProp(PropType.Fridge, 7, 2);
        map.AddProp(PropType.StoreShelf, 5, 4, width: 2);
        map.AddProp(PropType.Plant, 7, 6);
        map.AddProp(PropType.Painting, 2, 1);

        map.NPCs.Add(new NPC
        {
            Name = "Clerk",
            NpcType = "Clerk",
            GridX = 1,
            GridY = 3,
            Facing = Direction.Right,
            IsPokeMartClerk = true,
            DialogLines = new()
            {
                $"Clerk: Welcome to the {townName} Poké Mart! How may I serve you today?"
            }
        });

        return map;
    }

    private static Map BuildRowanLab()
    {
        var map = BuildRoom("RowanLab", "Rowan's Pokémon Lab", InteriorStyle.Lab, 11, 10, doorX: 5);
        map.Warps.Add(new Warp { SourceX = 5, SourceY = 9, TargetMap = "SandgemTown", TargetX = 7, TargetY = 18, TargetFacing = Direction.Down });

        map.AddProp(PropType.LabDesk, 3, 3, width: 5);
        map.AddProp(PropType.Bookshelf, 1, 2);
        map.AddProp(PropType.Bookshelf, 2, 2);
        map.AddProp(PropType.Bookshelf, 8, 2);
        map.AddProp(PropType.Bookshelf, 9, 2);
        map.AddProp(PropType.LabMachine, 1, 5, depth: 2);
        map.AddProp(PropType.LabMachine, 9, 5, depth: 2);
        map.AddProp(PropType.Plant, 1, 8);
        map.AddProp(PropType.Plant, 9, 8);
        map.AddProp(PropType.Window, 3, 1, width: 2);
        map.AddProp(PropType.Window, 6, 1, width: 2);

        map.NPCs.Add(new NPC
        {
            Name = "Prof. Rowan",
            NpcType = "Rowan",
            GridX = 5,
            GridY = 2,
            Facing = Direction.Down,
            DialogLines = new()
            {
                "Prof. Rowan: Ah, Lucas! Welcome to my research facility.",
                "Prof. Rowan: The bond between trainers and Pokémon is truly limitless.",
                "Prof. Rowan: Explore Sinnoh, complete the Pokédex, and unravel the secrets of the Distortion World!"
            }
        });

        return map;
    }
}
