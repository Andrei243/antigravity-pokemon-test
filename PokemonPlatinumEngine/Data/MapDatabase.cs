using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Data;

public static class MapDatabase
{
    private static readonly Dictionary<string, Map> Maps = new(System.StringComparer.OrdinalIgnoreCase);

    public static void Initialize()
    {
        Maps.Clear();
        Maps["TwinleafTown"] = BuildTwinleafTown();
        Maps["Route201"] = BuildRoute201();
        Maps["LakeVerity"] = BuildLakeVerity();
        Maps["SandgemTown"] = BuildSandgemTown();
        Maps["Route202"] = BuildRoute202();
        Maps["PlayerHouse"] = BuildPlayerHouse();
        Maps["PokemonCenter"] = BuildPokemonCenter();
        Maps["PokeMart"] = BuildPokeMart();
        Maps["RowanLab"] = BuildRowanLab();
    }

    public static Map Get(string name)
    {
        if (Maps.Count == 0) Initialize();
        if (Maps.TryGetValue(name, out var map)) return map;
        return Maps["TwinleafTown"];
    }

    private static Map BuildTwinleafTown()
    {
        int w = 24, h = 20;
        var map = new Map(w, h)
        {
            Name = "TwinleafTown",
            DisplayName = "Twinleaf Town",
            BgmTrack = "Twinleaf"
        };

        // Surrounding border trees (leave North road (11,0) and (12,0) open!)
        for (int x = 0; x < w; x++)
        {
            if (x != 11 && x != 12)
            {
                map.SetGroundTile(x, 0, TileType.Tree, isSolid: true);
            }
            map.SetGroundTile(x, h - 1, TileType.Tree, isSolid: true);
        }
        for (int y = 0; y < h; y++)
        {
            map.SetGroundTile(0, y, TileType.Tree, isSolid: true);
            map.SetGroundTile(w - 1, y, TileType.Tree, isSolid: true);
        }

        // Main North-South road (Open all the way to y = 0!)
        for (int y = 0; y < h - 1; y++)
        {
            map.SetGroundTile(11, y, TileType.Path, isSolid: false);
            map.SetGroundTile(12, y, TileType.Path, isSolid: false);
        }

        // East-West crossroad
        for (int x = 3; x < 21; x++)
        {
            map.SetGroundTile(x, 10, TileType.Path, isSolid: false);
            map.SetGroundTile(x, 11, TileType.Path, isSolid: false);
        }

        // Player's House (Top Left: (4, 4) to (8, 7))
        for (int x = 4; x <= 8; x++)
        {
            for (int y = 4; y <= 6; y++)
            {
                map.SetGroundTile(x, y, TileType.RoofRed, isSolid: true);
            }
            map.SetGroundTile(x, 7, TileType.Wall, isSolid: true);
        }
        map.SetGroundTile(6, 7, TileType.Door, isSolid: false); // Front door (walkable!)

        // Rival's House (Top Right: (15, 4) to (19, 7))
        for (int x = 15; x <= 19; x++)
        {
            for (int y = 4; y <= 6; y++)
            {
                map.SetGroundTile(x, y, TileType.RoofBlue, isSolid: true);
            }
            map.SetGroundTile(x, 7, TileType.Wall, isSolid: true);
        }
        map.SetGroundTile(17, 7, TileType.Door, isSolid: false);

        // Flower beds
        for (int x = 4; x <= 8; x++) map.SetGroundTile(x, 8, TileType.FlowerGrass);
        for (int x = 15; x <= 19; x++) map.SetGroundTile(x, 8, TileType.FlowerGrass);

        // Pond in bottom left
        for (int x = 3; x <= 7; x++)
        {
            for (int y = 14; y <= 17; y++)
            {
                map.SetGroundTile(x, y, TileType.Water, isSolid: true);
            }
        }

        // Signboards
        map.SetGroundTile(10, 11, TileType.Signpost, isSolid: true);
        map.Signboards[(10, 11)] = "Twinleaf Town\nFresh and free! The town where journeys begin.";

        map.SetGroundTile(5, 7, TileType.Signpost, isSolid: true);
        map.Signboards[(5, 7)] = "Lucas's House";

        // Warps
        // House door
        map.Warps.Add(new Warp { SourceX = 6, SourceY = 7, TargetMap = "PlayerHouse", TargetX = 4, TargetY = 6, TargetFacing = Direction.Up });

        // North exit to Route 201 (Walkable at y = 0)
        map.Warps.Add(new Warp { SourceX = 11, SourceY = 0, TargetMap = "Route201", TargetX = 14, TargetY = 20, TargetFacing = Direction.Up });
        map.Warps.Add(new Warp { SourceX = 12, SourceY = 0, TargetMap = "Route201", TargetX = 15, TargetY = 20, TargetFacing = Direction.Up });

        // NPCs
        map.NPCs.Add(new NPC
        {
            Name = "Barry",
            NpcType = "Rival",
            GridX = 12,
            GridY = 6,
            Facing = Direction.Down,
            DialogLines = new()
            {
                "Barry: Hey, Lucas! You're finally ready!",
                "Barry: Professor Rowan is waiting at Lake Verity just past Route 201 to the north-west!",
                "Barry: Let's hurry to Lake Verity right now! Walk north up the road to exit the town!"
            }
        });

        map.NPCs.Add(new NPC
        {
            Name = "Town Lass",
            NpcType = "Lass",
            GridX = 14,
            GridY = 12,
            Facing = Direction.Left,
            DialogLines = new()
            {
                "Technology is incredible! You can save your adventure anytime from the Start Menu!",
                "If you press Enter or Tab, you can check your Pokémon, Bag, and Pokédex."
            }
        });

        return map;
    }

    private static Map BuildRoute201()
    {
        int w = 36, h = 22;
        var map = new Map(w, h)
        {
            Name = "Route201",
            DisplayName = "Route 201",
            BgmTrack = "Route201"
        };

        // Surrounding trees
        for (int x = 0; x < w; x++)
        {
            map.SetGroundTile(x, 0, TileType.Tree, isSolid: true);
            // Leave south entrance (14, 21) and (15, 21) open to Twinleaf!
            if (x != 14 && x != 15)
            {
                map.SetGroundTile(x, h - 1, TileType.Tree, isSolid: true);
            }
        }
        for (int y = 0; y < h; y++)
        {
            // Leave West exit (0, 9) and (0, 10) open to Lake Verity!
            if (y != 9 && y != 10)
            {
                map.SetGroundTile(0, y, TileType.Tree, isSolid: true);
            }
            // Leave East exit (w-1, 9) and (w-1, 10) open to Sandgem Town!
            if (y != 9 && y != 10)
            {
                map.SetGroundTile(w - 1, y, TileType.Tree, isSolid: true);
            }
        }

        // East-West main road (Open from x = 0 to x = w - 1!)
        for (int x = 0; x < w; x++)
        {
            map.SetGroundTile(x, 9, TileType.Path, isSolid: false);
            map.SetGroundTile(x, 10, TileType.Path, isSolid: false);
        }

        // South road to Twinleaf (Open to y = h - 1!)
        for (int y = 10; y < h; y++)
        {
            map.SetGroundTile(14, y, TileType.Path, isSolid: false);
            map.SetGroundTile(15, y, TileType.Path, isSolid: false);
        }

        // Tall grass patches
        for (int x = 3; x <= 11; x++)
        {
            for (int y = 3; y <= 7; y++) map.SetGroundTile(x, y, TileType.TallGrass);
            for (int y = 13; y <= 18; y++) map.SetGroundTile(x, y, TileType.TallGrass);
        }

        for (int x = 18; x <= 30; x++)
        {
            for (int y = 3; y <= 7; y++) map.SetGroundTile(x, y, TileType.TallGrass);
            for (int y = 13; y <= 18; y++) map.SetGroundTile(x, y, TileType.TallGrass);
        }

        // Ledges
        for (int x = 18; x <= 26; x++) map.SetGroundTile(x, 12, TileType.LedgeDown);

        // Signboards
        map.SetGroundTile(13, 11, TileType.Signpost, isSolid: true);
        map.Signboards[(13, 11)] = "Route 201\nWest: Lake Verity | East: Sandgem Town | South: Twinleaf Town";

        // Wild Encounters
        map.WildEncounters.Add(new() { SpeciesName = "Bidoof", MinLevel = 2, MaxLevel = 4, Weight = 45 });
        map.WildEncounters.Add(new() { SpeciesName = "Starly", MinLevel = 2, MaxLevel = 4, Weight = 45 });
        map.WildEncounters.Add(new() { SpeciesName = "Shinx", MinLevel = 3, MaxLevel = 4, Weight = 10 });

        // Warps
        // South to Twinleaf
        map.Warps.Add(new Warp { SourceX = 14, SourceY = 21, TargetMap = "TwinleafTown", TargetX = 11, TargetY = 1, TargetFacing = Direction.Down });
        map.Warps.Add(new Warp { SourceX = 15, SourceY = 21, TargetMap = "TwinleafTown", TargetX = 12, TargetY = 1, TargetFacing = Direction.Down });

        // West to Lake Verity
        map.Warps.Add(new Warp { SourceX = 0, SourceY = 9, TargetMap = "LakeVerity", TargetX = 26, TargetY = 9, TargetFacing = Direction.Left });
        map.Warps.Add(new Warp { SourceX = 0, SourceY = 10, TargetMap = "LakeVerity", TargetX = 26, TargetY = 10, TargetFacing = Direction.Left });

        // East to Sandgem Town
        map.Warps.Add(new Warp { SourceX = 35, SourceY = 9, TargetMap = "SandgemTown", TargetX = 1, TargetY = 9, TargetFacing = Direction.Right });
        map.Warps.Add(new Warp { SourceX = 35, SourceY = 10, TargetMap = "SandgemTown", TargetX = 1, TargetY = 10, TargetFacing = Direction.Right });

        // Trainer
        var youngsterParty = new Party();
        youngsterParty.Add(new Pokemon(PokemonDatabase.Get("Starly")!, 4));
        map.NPCs.Add(new NPC
        {
            Id = "trainer_tristan",
            Name = "Tristan",
            NpcType = "Youngster",
            GridX = 24,
            GridY = 7,
            Facing = Direction.Down,
            IsTrainer = true,
            TrainerData = new Trainer
            {
                Id = "trainer_tristan",
                Name = "Tristan",
                TrainerClass = "Youngster",
                PrizeMoney = 160,
                DialogueBefore = "Youngster Tristan: Our eyes met! That means we have to battle!",
                DialogueAfter = "Youngster Tristan: Aww, my Starly fought hard! You're really tough!"
            }
        });

        return map;
    }

    private static Map BuildLakeVerity()
    {
        int w = 28, h = 22;
        var map = new Map(w, h)
        {
            Name = "LakeVerity",
            DisplayName = "Lake Verity",
            BgmTrack = "Twinleaf"
        };

        // Surrounding trees
        for (int x = 0; x < w; x++)
        {
            map.SetGroundTile(x, 0, TileType.Tree, isSolid: true);
            map.SetGroundTile(x, h - 1, TileType.Tree, isSolid: true);
        }
        for (int y = 0; y < h; y++)
        {
            map.SetGroundTile(0, y, TileType.Tree, isSolid: true);
            // Leave East exit (w-1, 9) and (w-1, 10) open to Route 201!
            if (y != 9 && y != 10)
            {
                map.SetGroundTile(w - 1, y, TileType.Tree, isSolid: true);
            }
        }

        // Great Lake Water in north
        for (int x = 2; x <= 22; x++)
        {
            for (int y = 2; y <= 8; y++)
            {
                map.SetGroundTile(x, y, TileType.Water, isSolid: true);
            }
        }

        // Shore path
        for (int x = 1; x < w; x++)
        {
            map.SetGroundTile(x, 9, TileType.Path, isSolid: false);
            map.SetGroundTile(x, 10, TileType.Path, isSolid: false);
        }

        // Wild grass
        for (int x = 4; x <= 20; x++)
        {
            for (int y = 13; y <= 18; y++) map.SetGroundTile(x, y, TileType.TallGrass);
        }

        // Signboard
        map.SetGroundTile(21, 10, TileType.Signpost, isSolid: true);
        map.Signboards[(21, 10)] = "Lake Verity\nAccording to legend, a legendary Pokémon of emotion rests in this deep lake.";

        // Wild encounters
        map.WildEncounters.Add(new() { SpeciesName = "Bidoof", MinLevel = 3, MaxLevel = 5, Weight = 40 });
        map.WildEncounters.Add(new() { SpeciesName = "Starly", MinLevel = 3, MaxLevel = 5, Weight = 40 });
        map.WildEncounters.Add(new() { SpeciesName = "Shinx", MinLevel = 4, MaxLevel = 5, Weight = 20 });

        // Warps (East back to Route 201)
        map.Warps.Add(new Warp { SourceX = 27, SourceY = 9, TargetMap = "Route201", TargetX = 1, TargetY = 9, TargetFacing = Direction.Right });
        map.Warps.Add(new Warp { SourceX = 27, SourceY = 10, TargetMap = "Route201", TargetX = 1, TargetY = 10, TargetFacing = Direction.Right });

        // Professor Rowan & Starter Briefcase!
        map.NPCs.Add(new NPC
        {
            Name = "Prof. Rowan",
            NpcType = "Rowan",
            GridX = 12,
            GridY = 9,
            Facing = Direction.Down,
            DialogLines = new()
            {
                "Prof. Rowan: Hmm... The energy radiating from this lake is fascinating...",
                "Prof. Rowan: Oh, young trainer! I left my research briefcase by the shore.",
                "Prof. Rowan: Inside are three Poké Balls containing rare Sinnoh Pokémon.",
                "Prof. Rowan: Go ahead and examine the briefcase to choose your partner!"
            }
        });

        // Starter briefcase interactable
        map.NPCs.Add(new NPC
        {
            Id = "starter_briefcase",
            Name = "Professor's Briefcase",
            NpcType = "StarterBriefcase",
            GridX = 13,
            GridY = 9,
            Facing = Direction.Down,
            IsStarterBriefcase = true
        });

        return map;
    }

    private static Map BuildSandgemTown()
    {
        int w = 30, h = 24;
        var map = new Map(w, h)
        {
            Name = "SandgemTown",
            DisplayName = "Sandgem Town",
            BgmTrack = "Twinleaf"
        };

        // Surrounding trees / seaside border
        for (int x = 0; x < w; x++)
        {
            // Leave North exit (14, 0) and (15, 0) open to Route 202!
            if (x != 14 && x != 15)
            {
                map.SetGroundTile(x, 0, TileType.Tree, isSolid: true);
            }
            map.SetGroundTile(x, h - 1, TileType.Water, isSolid: true);
        }
        for (int y = 0; y < h; y++)
        {
            // Leave West exit (0, 9) and (0, 10) open to Route 201!
            if (y != 9 && y != 10)
            {
                map.SetGroundTile(0, y, TileType.Tree, isSolid: true);
            }
            map.SetGroundTile(w - 1, y, TileType.Tree, isSolid: true);
        }

        // Main Crossroad (Open to edges!)
        for (int x = 0; x < w; x++)
        {
            map.SetGroundTile(x, 9, TileType.Path, isSolid: false);
            map.SetGroundTile(x, 10, TileType.Path, isSolid: false);
        }
        for (int y = 0; y < h - 1; y++)
        {
            map.SetGroundTile(14, y, TileType.Path, isSolid: false);
            map.SetGroundTile(15, y, TileType.Path, isSolid: false);
        }

        // Pokémon Center (Top Left: (4, 3) to (8, 6))
        for (int x = 4; x <= 8; x++)
        {
            for (int y = 3; y <= 5; y++) map.SetGroundTile(x, y, TileType.RoofRed, isSolid: true);
            map.SetGroundTile(x, 6, TileType.Wall, isSolid: true);
        }
        map.SetGroundTile(6, 6, TileType.Door, isSolid: false);

        // Poké Mart (Top Right: (20, 3) to (24, 6))
        for (int x = 20; x <= 24; x++)
        {
            for (int y = 3; y <= 5; y++) map.SetGroundTile(x, y, TileType.RoofBlue, isSolid: true);
            map.SetGroundTile(x, 6, TileType.Wall, isSolid: true);
        }
        map.SetGroundTile(22, 6, TileType.Door, isSolid: false);

        // Professor Rowan's Pokémon Lab (Bottom: (4, 14) to (10, 18))
        for (int x = 4; x <= 10; x++)
        {
            for (int y = 14; y <= 16; y++) map.SetGroundTile(x, y, TileType.RoofBlue, isSolid: true);
            map.SetGroundTile(x, 17, TileType.Wall, isSolid: true);
        }
        map.SetGroundTile(7, 17, TileType.Door, isSolid: false);

        // Signboards
        map.SetGroundTile(13, 10, TileType.Signpost, isSolid: true);
        map.Signboards[(13, 10)] = "Sandgem Town\nTown of Sand and Sparkle!\nNorth: Route 202 | West: Route 201";

        // Warps
        map.Warps.Add(new Warp { SourceX = 6, SourceY = 6, TargetMap = "PokemonCenter", TargetX = 5, TargetY = 7, TargetFacing = Direction.Up });
        map.Warps.Add(new Warp { SourceX = 22, SourceY = 6, TargetMap = "PokeMart", TargetX = 4, TargetY = 6, TargetFacing = Direction.Up });
        map.Warps.Add(new Warp { SourceX = 7, SourceY = 17, TargetMap = "RowanLab", TargetX = 5, TargetY = 8, TargetFacing = Direction.Up });

        // West to Route 201
        map.Warps.Add(new Warp { SourceX = 0, SourceY = 9, TargetMap = "Route201", TargetX = 34, TargetY = 9, TargetFacing = Direction.Left });
        map.Warps.Add(new Warp { SourceX = 0, SourceY = 10, TargetMap = "Route201", TargetX = 34, TargetY = 10, TargetFacing = Direction.Left });

        // North to Route 202
        map.Warps.Add(new Warp { SourceX = 14, SourceY = 0, TargetMap = "Route202", TargetX = 14, TargetY = 24, TargetFacing = Direction.Up });
        map.Warps.Add(new Warp { SourceX = 15, SourceY = 0, TargetMap = "Route202", TargetX = 15, TargetY = 24, TargetFacing = Direction.Up });

        // NPCs
        map.NPCs.Add(new NPC
        {
            Name = "Dawn",
            NpcType = "Lass",
            GridX = 17,
            GridY = 10,
            Facing = Direction.Left,
            DialogLines = new()
            {
                "Dawn: Hi, Lucas! Welcome to Sandgem Town!",
                "Dawn: The Pokémon Center on the top-left heals your Pokémon for free!",
                "Dawn: The Poké Mart on the top-right sells Poké Balls and Potions!",
                "Dawn: Professor Rowan's Lab is on the bottom-left."
            }
        });

        return map;
    }

    private static Map BuildRoute202()
    {
        int w = 30, h = 26;
        var map = new Map(w, h)
        {
            Name = "Route202",
            DisplayName = "Route 202 & Distortion Gateway",
            BgmTrack = "Route201"
        };

        // Surrounding trees
        for (int x = 0; x < w; x++)
        {
            map.SetGroundTile(x, 0, TileType.Tree, isSolid: true);
            // Leave south entrance (14, 25) and (15, 25) open to Sandgem!
            if (x != 14 && x != 15)
            {
                map.SetGroundTile(x, h - 1, TileType.Tree, isSolid: true);
            }
        }
        for (int y = 0; y < h; y++)
        {
            map.SetGroundTile(0, y, TileType.Tree, isSolid: true);
            map.SetGroundTile(w - 1, y, TileType.Tree, isSolid: true);
        }

        // Main North-South road (Open all the way to y = h - 1!)
        for (int y = 0; y < h; y++)
        {
            map.SetGroundTile(14, y, TileType.Path, isSolid: false);
            map.SetGroundTile(15, y, TileType.Path, isSolid: false);
        }

        // Dense wild grass
        for (int y = 4; y <= 20; y++)
        {
            for (int x = 3; x <= 11; x++) map.SetGroundTile(x, y, TileType.TallGrass);
            for (int x = 18; x <= 26; x++) map.SetGroundTile(x, y, TileType.TallGrass);
        }

        // Ledges
        for (int x = 4; x <= 11; x++) map.SetGroundTile(x, 12, TileType.LedgeDown);
        for (int x = 18; x <= 25; x++) map.SetGroundTile(x, 12, TileType.LedgeDown);

        // Distortion Shrine in northern clearing (Tile (14, 3))
        map.SetGroundTile(14, 3, TileType.Floor);
        map.SetGroundTile(15, 3, TileType.Floor);

        // Wild encounters with Sinnoh favorites!
        map.WildEncounters.Add(new() { SpeciesName = "Shinx", MinLevel = 5, MaxLevel = 7, Weight = 35 });
        map.WildEncounters.Add(new() { SpeciesName = "Starly", MinLevel = 5, MaxLevel = 7, Weight = 25 });
        map.WildEncounters.Add(new() { SpeciesName = "Bidoof", MinLevel = 5, MaxLevel = 6, Weight = 20 });
        map.WildEncounters.Add(new() { SpeciesName = "Riolu", MinLevel = 6, MaxLevel = 8, Weight = 10 });
        map.WildEncounters.Add(new() { SpeciesName = "Gible", MinLevel = 7, MaxLevel = 9, Weight = 8 });
        map.WildEncounters.Add(new() { SpeciesName = "Giratina", MinLevel = 50, MaxLevel = 50, Weight = 2 });

        // Warps (South back to Sandgem Town)
        map.Warps.Add(new Warp { SourceX = 14, SourceY = 25, TargetMap = "SandgemTown", TargetX = 14, TargetY = 1, TargetFacing = Direction.Down });
        map.Warps.Add(new Warp { SourceX = 15, SourceY = 25, TargetMap = "SandgemTown", TargetX = 15, TargetY = 1, TargetFacing = Direction.Down });

        // Trainers on Route 202
        var lassParty = new Party();
        lassParty.Add(new Pokemon(PokemonDatabase.Get("Shinx")!, 6));
        map.NPCs.Add(new NPC
        {
            Id = "trainer_natalie",
            Name = "Natalie",
            NpcType = "Lass",
            GridX = 18,
            GridY = 7,
            Facing = Direction.Left,
            IsTrainer = true,
            TrainerData = new Trainer
            {
                Id = "trainer_natalie",
                Name = "Natalie",
                TrainerClass = "Lass",
                PrizeMoney = 240,
                DialogueBefore = "Lass Natalie: My cute Shinx is fully charged! Prepare for a shock!",
                DialogueAfter = "Lass Natalie: Wow, you're strong! Keep going toward the Distortion Gateway!"
            }
        });

        var youngsterParty2 = new Party();
        youngsterParty2.Add(new Pokemon(PokemonDatabase.Get("Bidoof")!, 6));
        youngsterParty2.Add(new Pokemon(PokemonDatabase.Get("Starly")!, 7));
        map.NPCs.Add(new NPC
        {
            Id = "trainer_logan",
            Name = "Logan",
            NpcType = "Youngster",
            GridX = 11,
            GridY = 16,
            Facing = Direction.Right,
            IsTrainer = true,
            TrainerData = new Trainer
            {
                Id = "trainer_logan",
                Name = "Logan",
                TrainerClass = "Youngster",
                PrizeMoney = 280,
                DialogueBefore = "Youngster Logan: I've caught two Pokémon already! Let's see your team!",
                DialogueAfter = "Youngster Logan: No way! You've got an amazing team balance!"
            }
        });

        // Giratina Distortion Rift Entity
        map.NPCs.Add(new NPC
        {
            Name = "Distortion Rift",
            NpcType = "Trainer",
            GridX = 14,
            GridY = 2,
            Facing = Direction.Down,
            DialogLines = new()
            {
                "A shadowy spatial rift trembles before you...",
                "You feel the chilling gaze of the Renegade Pokémon, GIRATINA, observing you from the Distortion World!"
            }
        });

        return map;
    }

    private static Map BuildPlayerHouse()
    {
        int w = 10, h = 9;
        var map = new Map(w, h)
        {
            Name = "PlayerHouse",
            DisplayName = "Lucas's Home",
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

        map.SetGroundTile(2, 2, TileType.PC, isSolid: true);
        map.SetGroundTile(4, 4, TileType.Wall, isSolid: true);
        map.SetGroundTile(5, 4, TileType.Wall, isSolid: true);

        // Exit door (must be walkable, it sits in the solid bottom wall)
        map.SetGroundTile(4, 7, TileType.Floor);
        map.SetGroundTile(4, 8, TileType.Door, isSolid: false);
        map.Warps.Add(new Warp { SourceX = 4, SourceY = 8, TargetMap = "TwinleafTown", TargetX = 6, TargetY = 8, TargetFacing = Direction.Down });

        // Mom NPC
        map.NPCs.Add(new NPC
        {
            Name = "Mom",
            NpcType = "Nurse",
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

    private static Map BuildPokemonCenter()
    {
        int w = 11, h = 9;
        var map = new Map(w, h)
        {
            Name = "PokemonCenter",
            DisplayName = "Pokémon Center",
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

        // Nurse Counter
        for (int x = 3; x <= 7; x++) map.SetGroundTile(x, 3, TileType.Wall, isSolid: true);

        // PC Terminal on right
        map.SetGroundTile(9, 2, TileType.PC, isSolid: true);

        // Warp Door
        map.SetGroundTile(5, 8, TileType.Door, isSolid: false);
        map.Warps.Add(new Warp { SourceX = 5, SourceY = 8, TargetMap = "SandgemTown", TargetX = 6, TargetY = 7, TargetFacing = Direction.Down });

        // Nurse Joy
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

        // PC Terminal NPC
        map.NPCs.Add(new NPC
        {
            Name = "PC Terminal",
            NpcType = "Trainer",
            GridX = 9,
            GridY = 3,
            Facing = Direction.Up,
            IsPCTerminal = true
        });

        return map;
    }

    private static Map BuildPokeMart()
    {
        int w = 9, h = 8;
        var map = new Map(w, h)
        {
            Name = "PokeMart",
            DisplayName = "Poké Mart",
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

        for (int y = 2; y <= 5; y++) map.SetGroundTile(2, y, TileType.Wall, isSolid: true);

        // Warp Door
        map.SetGroundTile(4, 7, TileType.Door, isSolid: false);
        map.Warps.Add(new Warp { SourceX = 4, SourceY = 7, TargetMap = "SandgemTown", TargetX = 22, TargetY = 7, TargetFacing = Direction.Down });

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
                "Clerk: Welcome to the Sandgem Poké Mart! How may I serve you today?"
            }
        });

        return map;
    }

    private static Map BuildRowanLab()
    {
        int w = 11, h = 10;
        var map = new Map(w, h)
        {
            Name = "RowanLab",
            DisplayName = "Rowan's Pokémon Lab",
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

        for (int x = 3; x <= 7; x++) map.SetGroundTile(x, 3, TileType.Wall, isSolid: true);

        // Warp Door
        map.SetGroundTile(5, 9, TileType.Door, isSolid: false);
        map.Warps.Add(new Warp { SourceX = 5, SourceY = 9, TargetMap = "SandgemTown", TargetX = 7, TargetY = 18, TargetFacing = Direction.Down });

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
