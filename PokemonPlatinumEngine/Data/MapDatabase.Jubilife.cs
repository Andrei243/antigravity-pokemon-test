using System.Collections.Generic;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// Jubilife City, north of Route 202, and the buildings you can enter there (plan 02 · S5, first visit).
/// Routes 203, 204 and 218 aren't built yet, so someone stands in each of those roads for now.
/// </summary>
public static partial class MapDatabase
{
    private static void AddJubilifeMaps()
    {
        Maps["JubilifeCity"] = BuildJubilifeCity();
        Maps["TrainersSchool"] = BuildTrainersSchool();
        Maps["PoketchCompany"] = BuildPoketchCompany();
        Maps["JubilifePokemonCenter"] = BuildPokemonCenter("JubilifePokemonCenter", "JubilifeCity", 25, 27);
        Maps["JubilifePokeMart"] = BuildPokeMart("JubilifePokeMart", "JubilifeCity", "Jubilife", 32, 27);
    }

    /// <summary>A block of roof rows with a wall row in front; a door, when there is one, sits in the wall.</summary>
    private static void AddBuilding(Map map, int x0, int x1, int roofTop, int wallY, TileType roof, int? doorX = null)
    {
        for (int x = x0; x <= x1; x++)
        {
            for (int y = roofTop; y < wallY; y++) map.SetGroundTile(x, y, roof, isSolid: true);
            map.SetGroundTile(x, wallY, TileType.Wall, isSolid: true);
        }
        if (doorX.HasValue) map.SetGroundTile(doorX.Value, wallY, TileType.Door, isSolid: false);
    }

    private static void AddSign(Map map, int x, int y, string text)
    {
        map.SetGroundTile(x, y, TileType.Signpost, isSolid: true);
        map.Signboards[(x, y)] = text;
    }

    private static Map BuildJubilifeCity()
    {
        int w = 40, h = 34;
        var map = new Map(w, h)
        {
            Name = "JubilifeCity",
            DisplayName = "Jubilife City",
            BgmTrack = "Twinleaf"
        };

        // Trees all round, with a road out of each side: Route 204 north, Route 203 east, Route 218 west, Route 202 south
        for (int x = 0; x < w; x++)
        {
            map.SetGroundTile(x, 0, TileType.Tree, isSolid: true);
            map.SetGroundTile(x, h - 1, TileType.Tree, isSolid: true);
        }
        for (int y = 0; y < h; y++)
        {
            map.SetGroundTile(0, y, TileType.Tree, isSolid: true);
            map.SetGroundTile(w - 1, y, TileType.Tree, isSolid: true);
        }

        // The two avenues cross in the middle of the city and run out to every edge
        for (int y = 0; y < h; y++)
        {
            map.SetGroundTile(19, y, TileType.Path, isSolid: false);
            map.SetGroundTile(20, y, TileType.Path, isSolid: false);
        }
        for (int x = 0; x < w; x++)
        {
            map.SetGroundTile(x, 16, TileType.Path, isSolid: false);
            map.SetGroundTile(x, 17, TileType.Path, isSolid: false);
        }

        // Streets in front of the northern and southern blocks
        for (int x = 2; x <= 37; x++)
        {
            map.SetGroundTile(x, 8, TileType.Path);
            map.SetGroundTile(x, 9, TileType.Path);
            map.SetGroundTile(x, 27, TileType.Path);
            map.SetGroundTile(x, 28, TileType.Path);
        }

        // Northern block: Trainers' School, Pokétch Company and the TV station, which stands a storey taller
        AddBuilding(map, 3, 10, 4, 7, TileType.RoofGreen, doorX: 7);
        AddSign(map, 4, 7, "Trainers' School\nEvery new trainer is welcome to learn the basics here, free of charge!");

        AddBuilding(map, 23, 29, 4, 7, TileType.RoofBlue, doorX: 26);
        AddSign(map, 24, 7, "Pokétch Company\nWe make the Pokétch, the Pokémon Watch every trainer needs.");

        AddBuilding(map, 31, 37, 3, 7, TileType.RoofRed);
        AddSign(map, 34, 7, "Jubilife TV\nSinnoh's own broadcast station. Staff only beyond this point.");

        // Southern block: Global Terminal, condominiums, Pokémon Center and Poké Mart
        AddBuilding(map, 3, 10, 23, 26, TileType.RoofBlue);
        AddSign(map, 6, 26, "Global Terminal\nTrade with trainers far away. Opening soon!");

        AddBuilding(map, 12, 16, 23, 26, TileType.RoofGreen);
        AddSign(map, 13, 26, "Jubilife Condominiums");

        AddBuilding(map, 23, 27, 23, 26, TileType.RoofRed, doorX: 25);
        AddBuilding(map, 30, 34, 23, 26, TileType.RoofBlue, doorX: 32);

        // A fountain square between the school and the crossroads, and flower beds along the east side
        for (int x = 8; x <= 13; x++)
            for (int y = 10; y <= 14; y++) map.SetGroundTile(x, y, TileType.FlowerGrass);
        for (int x = 9; x <= 12; x++)
            for (int y = 11; y <= 13; y++) map.SetGroundTile(x, y, TileType.Water, isSolid: true);
        for (int x = 23; x <= 37; x++)
        {
            map.SetGroundTile(x, 13, TileType.FlowerGrass);
            map.SetGroundTile(x, 20, TileType.FlowerGrass);
        }

        AddSign(map, 18, 31, "Jubilife City\nSinnoh's busiest city, where people and new ideas meet.");

        // Roads out of town. A sign fills one lane and someone stands in the other until the route is built.
        AddSign(map, 19, 1, "Route 204\nNorth: Ravaged Path, Floaroma Town");
        AddSign(map, 38, 16, "Route 203\nEast: Oreburgh Gate, Oreburgh City");
        AddSign(map, 1, 16, "Route 218 Gate\nWest: the sea road to Canalave City");

        // Warps
        map.Warps.Add(new Warp { SourceX = 19, SourceY = 33, TargetMap = "Route202", TargetX = 14, TargetY = 1, TargetFacing = Direction.Down });
        map.Warps.Add(new Warp { SourceX = 20, SourceY = 33, TargetMap = "Route202", TargetX = 15, TargetY = 1, TargetFacing = Direction.Down });
        map.Warps.Add(new Warp { SourceX = 7, SourceY = 7, TargetMap = "TrainersSchool", TargetX = 6, TargetY = 9, TargetFacing = Direction.Up });
        map.Warps.Add(new Warp { SourceX = 26, SourceY = 7, TargetMap = "PoketchCompany", TargetX = 5, TargetY = 7, TargetFacing = Direction.Up });
        map.Warps.Add(new Warp { SourceX = 25, SourceY = 26, TargetMap = "JubilifePokemonCenter", TargetX = 5, TargetY = 7, TargetFacing = Direction.Up });
        map.Warps.Add(new Warp { SourceX = 32, SourceY = 26, TargetMap = "JubilifePokeMart", TargetX = 4, TargetY = 6, TargetFacing = Direction.Up });

        // Dawn meets you where Route 202 comes in
        map.NPCs.Add(new NPC
        {
            Name = "Dawn",
            NpcType = "Lass",
            GridX = 21,
            GridY = 30,
            Facing = Direction.Left,
            DialogLines = new()
            {
                "Dawn: Lucas! You made it through Route 202. Welcome to Jubilife City!",
                "Dawn: It's the biggest city in Sinnoh. There's even a TV station!",
                "Dawn: Barry ran on ahead to the Trainers' School. It's up the main street, on the left.",
                "Dawn: Have a look inside! The lessons there are a big help when you're starting out."
            }
        });

        // Someone keeping an eye on things from the crossroads
        map.NPCs.Add(new NPC
        {
            Name = "Looker",
            NpcType = "Looker",
            GridX = 18,
            GridY = 13,
            Facing = Direction.Right,
            DialogLines = new()
            {
                "???: ...! You did not see me. I am simply a man admiring the street.",
                "Looker: Very well. You may call me Looker. My work is... the international kind.",
                "Looker: A certain group is up to something in Sinnoh. I am here to find out what.",
                "Looker: If you notice anyone suspicious, you will remember my face, yes? Farewell!"
            }
        });

        // The Pokétch campaign: three clowns around town, each with one question
        map.NPCs.Add(new NPC
        {
            Name = "Clown",
            NpcType = "Clown",
            GridX = 35,
            GridY = 27,
            Facing = Direction.Left,
            DialogLines = new()
            {
                "Clown: Ta-da! Welcome to the Pokétch campaign! Here's my question...",
                "Clown: When a Pokémon wins a battle, does it earn Experience Points and grow stronger?",
                "Clown: ...Yes, exactly right! Find my two partners around the city for their questions!"
            }
        });

        map.NPCs.Add(new NPC
        {
            Name = "Clown",
            NpcType = "Clown",
            GridX = 28,
            GridY = 9,
            Facing = Direction.Down,
            DialogLines = new()
            {
                "Clown: Pokétch campaign quiz time! Pokémon have types like Fire or Water...",
                "Clown: ...but do their moves have types as well?",
                "Clown: ...Yes, they do! A move that matches its user's type hits harder, too!"
            }
        });

        map.NPCs.Add(new NPC
        {
            Name = "Clown",
            NpcType = "Clown",
            GridX = 34,
            GridY = 8,
            Facing = Direction.Down,
            DialogLines = new()
            {
                "Clown: Last question of the Pokétch campaign! Can a Pokémon carry an item?",
                "Clown: ...Correct, they can! Now go and tell the president of the Pokétch Company!"
            }
        });

        map.NPCs.Add(new NPC
        {
            Name = "Gentleman",
            NpcType = "Gentleman",
            GridX = 14,
            GridY = 9,
            Facing = Direction.Down,
            DialogLines = new()
            {
                "The fountain was built when the city was founded. Jubilife grew up around it.",
                "Everything new in Sinnoh seems to start in this city."
            }
        });

        map.NPCs.Add(new NPC
        {
            Name = "Lady",
            NpcType = "Lady",
            GridX = 29,
            GridY = 28,
            Facing = Direction.Up,
            DialogLines = new()
            {
                "The Poké Mart here stocks everything a trainer needs for the road ahead.",
                "Rest your team at the Pokémon Center next door before you set out!"
            }
        });

        // Standing in for the routes that aren't built yet
        map.NPCs.Add(new NPC
        {
            Name = "Worker",
            NpcType = "Clerk",
            GridX = 20,
            GridY = 1,
            Facing = Direction.Down,
            DialogLines = new()
            {
                "Worker: Sorry! The road north to Route 204 is closed while we lay new paving."
            }
        });

        map.NPCs.Add(new NPC
        {
            Name = "Worker",
            NpcType = "Clerk",
            GridX = 38,
            GridY = 17,
            Facing = Direction.Left,
            DialogLines = new()
            {
                "Worker: Route 203 is being cleared of fallen rocks. Nobody gets through until we're done!"
            }
        });

        map.NPCs.Add(new NPC
        {
            Name = "Fisherman",
            NpcType = "Youngster",
            GridX = 1,
            GridY = 17,
            Facing = Direction.Right,
            DialogLines = new()
            {
                "Past this gate, Route 218 crosses open water.",
                "Without a Pokémon that can Surf, there's no way over to the other side."
            }
        });

        return map;
    }

    private static Map BuildTrainersSchool()
    {
        var map = BuildRoom("TrainersSchool", "Trainers' School", InteriorStyle.Lab, 13, 11, doorX: 6);
        map.Warps.Add(new Warp { SourceX = 6, SourceY = 10, TargetMap = "JubilifeCity", TargetX = 7, TargetY = 8, TargetFacing = Direction.Down });

        // Bookshelves at the back, two rows of desks either side of the aisle to the teacher
        map.AddProp(PropType.Bookshelf, 1, 2);
        map.AddProp(PropType.Bookshelf, 2, 2);
        map.AddProp(PropType.Bookshelf, 10, 2);
        map.AddProp(PropType.Bookshelf, 11, 2);
        map.AddProp(PropType.Painting, 6, 1);
        map.AddProp(PropType.Window, 3, 1, width: 2);
        map.AddProp(PropType.Window, 8, 1, width: 2);
        map.AddProp(PropType.Table, 3, 5, width: 2);
        map.AddProp(PropType.Table, 7, 5, width: 2);
        map.AddProp(PropType.Table, 3, 7, width: 2);
        map.AddProp(PropType.Table, 7, 7, width: 2);
        map.AddProp(PropType.Plant, 1, 9);
        map.AddProp(PropType.Plant, 11, 9);

        map.NPCs.Add(new NPC
        {
            Name = "Teacher",
            NpcType = "Lady",
            GridX = 6,
            GridY = 3,
            Facing = Direction.Down,
            DialogLines = new()
            {
                "Teacher: Welcome to the Trainers' School! Today's lesson is about status conditions.",
                "Teacher: A poisoned or burned Pokémon keeps losing HP, even after the battle ends.",
                "Teacher: An Antidote or a Burn Heal sorts that out. Or rest at a Pokémon Center!",
                "Teacher: Try a practice battle with my students. They've been studying hard!"
            }
        });

        map.NPCs.Add(new NPC
        {
            Name = "Barry",
            NpcType = "Rival",
            GridX = 10,
            GridY = 4,
            Facing = Direction.Left,
            DialogLines = new()
            {
                "Barry: Lucas! What took you so long? I've already learned everything here!",
                "Barry: Next stop is Oreburgh City and its Gym. I'm getting my first Badge before you!",
                "Barry: Don't fall behind, or I'll fine you 10 million!"
            }
        });

        var coleParty = new Party();
        coleParty.Add(new Pokemon(PokemonDatabase.Get("Bidoof")!, 7));
        coleParty.Add(new Pokemon(PokemonDatabase.Get("Starly")!, 7));
        map.NPCs.Add(new NPC
        {
            Id = "trainer_cole",
            Name = "Cole",
            NpcType = "Youngster",
            GridX = 1,
            GridY = 4,
            Facing = Direction.Right,
            IsTrainer = true,
            TrainerData = new Trainer
            {
                Id = "trainer_cole",
                Name = "Cole",
                TrainerClass = "School Kid",
                Party = coleParty,
                PrizeMoney = 140,
                DialogueBefore = "School Kid Cole: I just aced my test on type matchups! Let me show you!",
                DialogueAfter = "School Kid Cole: Knowing the theory isn't enough, huh? Back to studying."
            }
        });

        var jennaParty = new Party();
        jennaParty.Add(new Pokemon(PokemonDatabase.Get("Shinx")!, 8));
        map.NPCs.Add(new NPC
        {
            Id = "trainer_jenna",
            Name = "Jenna",
            NpcType = "Lass",
            GridX = 11,
            GridY = 8,
            Facing = Direction.Left,
            IsTrainer = true,
            TrainerData = new Trainer
            {
                Id = "trainer_jenna",
                Name = "Jenna",
                TrainerClass = "School Kid",
                Party = jennaParty,
                PrizeMoney = 160,
                DialogueBefore = "School Kid Jenna: Practice battle! The teacher says real trainers never say no!",
                DialogueAfter = "School Kid Jenna: You're good! Did you study here too?"
            }
        });

        return map;
    }

    private static Map BuildPoketchCompany()
    {
        var map = BuildRoom("PoketchCompany", "Pokétch Company", InteriorStyle.Lab, 11, 9, doorX: 5);
        map.Warps.Add(new Warp { SourceX = 5, SourceY = 8, TargetMap = "JubilifeCity", TargetX = 26, TargetY = 8, TargetFacing = Direction.Down });

        // The president's desk faces the door; workbenches and prototypes line the walls
        map.AddProp(PropType.LabDesk, 3, 3, width: 5);
        map.AddProp(PropType.Bookshelf, 1, 2);
        map.AddProp(PropType.Bookshelf, 9, 2);
        map.AddProp(PropType.LabMachine, 1, 5, depth: 2);
        map.AddProp(PropType.LabMachine, 9, 5, depth: 2);
        map.AddProp(PropType.Plant, 1, 7);
        map.AddProp(PropType.Plant, 9, 7);
        map.AddProp(PropType.WallEmblem, 5, 1);
        map.AddProp(PropType.Window, 2, 1, width: 2);
        map.AddProp(PropType.Window, 7, 1, width: 2);

        map.NPCs.Add(new NPC
        {
            Name = "President",
            NpcType = "Gentleman",
            GridX = 5,
            GridY = 2,
            Facing = Direction.Down,
            DialogLines = new()
            {
                "President: Welcome to the Pokétch Company! I'm the president, and the inventor of the Pokétch.",
                "President: It's a watch made for trainers, and we're giving them away in a campaign!",
                "President: Three clowns around Jubilife each have a question for you. Answer them all!",
                "President: Do that, and a brand-new Pokétch is yours. I'll be waiting right here!"
            }
        });

        map.NPCs.Add(new NPC
        {
            Name = "Engineer",
            NpcType = "Clerk",
            GridX = 8,
            GridY = 6,
            Facing = Direction.Left,
            DialogLines = new()
            {
                "Engineer: Every Pokétch app starts as a sketch on that desk.",
                "Engineer: A clock, a calculator, a step counter... we're always dreaming up new ones!"
            }
        });

        return map;
    }
}
