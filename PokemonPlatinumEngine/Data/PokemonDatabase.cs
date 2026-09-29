using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Data;

public static class PokemonDatabase
{
    private static readonly Dictionary<string, PokemonSpecies> SpeciesByName = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<int, PokemonSpecies> SpeciesByDex = new();

    public static void Initialize() { }

    static PokemonDatabase()
    {
        // 1. Turtwig -> Grotle -> Torterra
        Register(new PokemonSpecies
        {
            DexNumber = 387,
            Name = "Turtwig",
            Category = "Tiny Leaf",
            PrimaryType = PokemonType.Grass,
            BaseHP = 55, BaseAttack = 68, BaseDefense = 64, BaseSpAttack = 45, BaseSpDefense = 55, BaseSpeed = 31,
            CatchRate = 45, BaseExpYield = 64, GrowthRate = GrowthRate.MediumSlow,
            Height = 0.4f, Weight = 10.2f,
            DexEntry = "Made from soil, the shell on its back hardens when it drinks water. It lives along lakes.",
            Learnset = new()
            {
                new() { Level = 1, MoveName = "Tackle" },
                new() { Level = 5, MoveName = "Withdraw" },
                new() { Level = 9, MoveName = "Razor Leaf" },
                new() { Level = 13, MoveName = "Bite" },
                new() { Level = 17, MoveName = "Mega Drain" },
                new() { Level = 25, MoveName = "Synthesis" }
            },
            Evolution = new() { Level = 18, TargetSpecies = "Grotle" }
        });

        Register(new PokemonSpecies
        {
            DexNumber = 388,
            Name = "Grotle",
            Category = "Grove",
            PrimaryType = PokemonType.Grass,
            BaseHP = 75, BaseAttack = 89, BaseDefense = 85, BaseSpAttack = 55, BaseSpDefense = 65, BaseSpeed = 36,
            CatchRate = 45, BaseExpYield = 142, GrowthRate = GrowthRate.MediumSlow,
            Height = 1.1f, Weight = 97.0f,
            DexEntry = "It lives along water in forests. In the daytime, it leaves the forest to sunbathe its treed shell.",
            Learnset = new()
            {
                new() { Level = 1, MoveName = "Tackle" },
                new() { Level = 9, MoveName = "Razor Leaf" },
                new() { Level = 13, MoveName = "Bite" },
                new() { Level = 22, MoveName = "Mega Drain" },
                new() { Level = 27, MoveName = "Earthquake" }
            },
            Evolution = new() { Level = 32, TargetSpecies = "Torterra" }
        });

        Register(new PokemonSpecies
        {
            DexNumber = 389,
            Name = "Torterra",
            Category = "Continent",
            PrimaryType = PokemonType.Grass,
            SecondaryType = PokemonType.Ground,
            BaseHP = 95, BaseAttack = 109, BaseDefense = 105, BaseSpAttack = 75, BaseSpDefense = 85, BaseSpeed = 56,
            CatchRate = 45, BaseExpYield = 236, GrowthRate = GrowthRate.MediumSlow,
            Height = 2.2f, Weight = 310.0f,
            DexEntry = "Some Pokémon are born on a Torterra's back and spend their entire life there.",
            Learnset = new()
            {
                new() { Level = 1, MoveName = "Wood Hammer" },
                new() { Level = 32, MoveName = "Earthquake" },
                new() { Level = 39, MoveName = "Crunch" },
                new() { Level = 45, MoveName = "Energy Ball" }
            }
        });

        // 2. Chimchar -> Monferno -> Infernape
        Register(new PokemonSpecies
        {
            DexNumber = 390,
            Name = "Chimchar",
            Category = "Chimp",
            PrimaryType = PokemonType.Fire,
            BaseHP = 44, BaseAttack = 58, BaseDefense = 44, BaseSpAttack = 58, BaseSpDefense = 44, BaseSpeed = 61,
            CatchRate = 45, BaseExpYield = 62, GrowthRate = GrowthRate.MediumSlow,
            Height = 0.5f, Weight = 6.2f,
            DexEntry = "It agilely scales sheer cliffs to live atop craggy mountains. Its fire is fueled by gas in its belly.",
            Learnset = new()
            {
                new() { Level = 1, MoveName = "Scratch" },
                new() { Level = 1, MoveName = "Leer" },
                new() { Level = 7, MoveName = "Ember" },
                new() { Level = 12, MoveName = "Quick Attack" },
                new() { Level = 15, MoveName = "Mach Punch" },
                new() { Level = 19, MoveName = "Flame Wheel" }
            },
            Evolution = new() { Level = 14, TargetSpecies = "Monferno" }
        });

        Register(new PokemonSpecies
        {
            DexNumber = 391,
            Name = "Monferno",
            Category = "Playful",
            PrimaryType = PokemonType.Fire,
            SecondaryType = PokemonType.Fighting,
            BaseHP = 64, BaseAttack = 78, BaseDefense = 52, BaseSpAttack = 78, BaseSpDefense = 52, BaseSpeed = 81,
            CatchRate = 45, BaseExpYield = 142, GrowthRate = GrowthRate.MediumSlow,
            Height = 0.9f, Weight = 22.0f,
            DexEntry = "It uses ceilings and walls by bouncing off them with fiery acrobatics.",
            Learnset = new()
            {
                new() { Level = 1, MoveName = "Scratch" },
                new() { Level = 14, MoveName = "Mach Punch" },
                new() { Level = 19, MoveName = "Flame Wheel" },
                new() { Level = 26, MoveName = "Close Combat" },
                new() { Level = 33, MoveName = "Flamethrower" }
            },
            Evolution = new() { Level = 36, TargetSpecies = "Infernape" }
        });

        Register(new PokemonSpecies
        {
            DexNumber = 392,
            Name = "Infernape",
            Category = "Flame",
            PrimaryType = PokemonType.Fire,
            SecondaryType = PokemonType.Fighting,
            BaseHP = 76, BaseAttack = 104, BaseDefense = 71, BaseSpAttack = 104, BaseSpDefense = 71, BaseSpeed = 108,
            CatchRate = 45, BaseExpYield = 240, GrowthRate = GrowthRate.MediumSlow,
            Height = 1.2f, Weight = 35.0f,
            DexEntry = "Its crown of fire is indicative of its fiery nature. It is beaten by none in terms of quickness.",
            Learnset = new()
            {
                new() { Level = 1, MoveName = "Mach Punch" },
                new() { Level = 36, MoveName = "Close Combat" },
                new() { Level = 41, MoveName = "Flamethrower" },
                new() { Level = 53, MoveName = "Fire Blast" }
            }
        });

        // 3. Piplup -> Prinplup -> Empoleon
        Register(new PokemonSpecies
        {
            DexNumber = 393,
            Name = "Piplup",
            Category = "Penguin",
            PrimaryType = PokemonType.Water,
            BaseHP = 53, BaseAttack = 51, BaseDefense = 53, BaseSpAttack = 61, BaseSpDefense = 56, BaseSpeed = 40,
            CatchRate = 45, BaseExpYield = 63, GrowthRate = GrowthRate.MediumSlow,
            Height = 0.4f, Weight = 5.2f,
            DexEntry = "Because it is very proud, it hates accepting food from people. Its thick down guards it from cold.",
            Learnset = new()
            {
                new() { Level = 1, MoveName = "Pound" },
                new() { Level = 4, MoveName = "Growl" },
                new() { Level = 8, MoveName = "Bubble" },
                new() { Level = 11, MoveName = "Water Gun" },
                new() { Level = 15, MoveName = "Water Pulse" },
                new() { Level = 22, MoveName = "Bite" }
            },
            Evolution = new() { Level = 16, TargetSpecies = "Prinplup" }
        });

        Register(new PokemonSpecies
        {
            DexNumber = 394,
            Name = "Prinplup",
            Category = "Penguin",
            PrimaryType = PokemonType.Water,
            BaseHP = 64, BaseAttack = 66, BaseDefense = 68, BaseSpAttack = 81, BaseSpDefense = 76, BaseSpeed = 50,
            CatchRate = 45, BaseExpYield = 142, GrowthRate = GrowthRate.MediumSlow,
            Height = 0.8f, Weight = 23.0f,
            DexEntry = "It lives alone, away from others. Apparently, every one of them believes it is the most important.",
            Learnset = new()
            {
                new() { Level = 1, MoveName = "Pound" },
                new() { Level = 11, MoveName = "Water Gun" },
                new() { Level = 16, MoveName = "Water Pulse" },
                new() { Level = 24, MoveName = "Surf" },
                new() { Level = 31, MoveName = "Hydro Pump" }
            },
            Evolution = new() { Level = 36, TargetSpecies = "Empoleon" }
        });

        Register(new PokemonSpecies
        {
            DexNumber = 395,
            Name = "Empoleon",
            Category = "Emperor",
            PrimaryType = PokemonType.Water,
            SecondaryType = PokemonType.Steel,
            BaseHP = 84, BaseAttack = 86, BaseDefense = 88, BaseSpAttack = 111, BaseSpDefense = 101, BaseSpeed = 60,
            CatchRate = 45, BaseExpYield = 239, GrowthRate = GrowthRate.MediumSlow,
            Height = 1.7f, Weight = 84.5f,
            DexEntry = "The three horns that extend from its beak attest to its power. The leader has the largest horns.",
            Learnset = new()
            {
                new() { Level = 1, MoveName = "Aqua Jet" },
                new() { Level = 36, MoveName = "Surf" },
                new() { Level = 42, MoveName = "Ice Beam" },
                new() { Level = 52, MoveName = "Hydro Pump" }
            }
        });

        // 4. Starly -> Staravia -> Staraptor
        Register(new PokemonSpecies
        {
            DexNumber = 396,
            Name = "Starly",
            Category = "Starling",
            PrimaryType = PokemonType.Normal,
            SecondaryType = PokemonType.Flying,
            BaseHP = 40, BaseAttack = 55, BaseDefense = 30, BaseSpAttack = 30, BaseSpDefense = 30, BaseSpeed = 60,
            CatchRate = 255, BaseExpYield = 49, GrowthRate = GrowthRate.MediumSlow,
            Height = 0.3f, Weight = 2.0f,
            DexEntry = "They flock in great numbers. Though small, they flap their wings with great power.",
            Learnset = new()
            {
                new() { Level = 1, MoveName = "Tackle" },
                new() { Level = 1, MoveName = "Growl" },
                new() { Level = 5, MoveName = "Quick Attack" },
                new() { Level = 9, MoveName = "Wing Attack" },
                new() { Level = 13, MoveName = "Aerial Ace" }
            },
            Evolution = new() { Level = 14, TargetSpecies = "Staravia" }
        });

        Register(new PokemonSpecies
        {
            DexNumber = 397,
            Name = "Staravia",
            Category = "Starling",
            PrimaryType = PokemonType.Normal,
            SecondaryType = PokemonType.Flying,
            BaseHP = 55, BaseAttack = 75, BaseDefense = 50, BaseSpAttack = 40, BaseSpDefense = 40, BaseSpeed = 80,
            CatchRate = 120, BaseExpYield = 119, GrowthRate = GrowthRate.MediumSlow,
            Height = 0.6f, Weight = 15.5f,
            DexEntry = "Recognizing their own weakness, they maintain huge flocks. Fierce scuffles break out when flocks meet.",
            Learnset = new()
            {
                new() { Level = 1, MoveName = "Quick Attack" },
                new() { Level = 9, MoveName = "Wing Attack" },
                new() { Level = 18, MoveName = "Aerial Ace" },
                new() { Level = 28, MoveName = "Brave Bird" }
            },
            Evolution = new() { Level = 34, TargetSpecies = "Staraptor" }
        });

        Register(new PokemonSpecies
        {
            DexNumber = 398,
            Name = "Staraptor",
            Category = "Predator",
            PrimaryType = PokemonType.Normal,
            SecondaryType = PokemonType.Flying,
            BaseHP = 85, BaseAttack = 120, BaseDefense = 70, BaseSpAttack = 50, BaseSpDefense = 60, BaseSpeed = 100,
            CatchRate = 45, BaseExpYield = 218, GrowthRate = GrowthRate.MediumSlow,
            Height = 1.2f, Weight = 24.9f,
            DexEntry = "It has a savage nature. It will courageously challenge any foe, no matter how much bigger it is.",
            Learnset = new()
            {
                new() { Level = 1, MoveName = "Close Combat" },
                new() { Level = 34, MoveName = "Close Combat" },
                new() { Level = 40, MoveName = "Brave Bird" }
            }
        });

        // 5. Bidoof -> Bibarel
        Register(new PokemonSpecies
        {
            DexNumber = 399,
            Name = "Bidoof",
            Category = "Plump Mouse",
            PrimaryType = PokemonType.Normal,
            BaseHP = 59, BaseAttack = 45, BaseDefense = 40, BaseSpAttack = 35, BaseSpDefense = 40, BaseSpeed = 31,
            CatchRate = 255, BaseExpYield = 50, GrowthRate = GrowthRate.MediumFast,
            Height = 0.5f, Weight = 20.0f,
            DexEntry = "With nerves of steel, nothing can perturb it. It is more agile and active than it appears.",
            Learnset = new()
            {
                new() { Level = 1, MoveName = "Tackle" },
                new() { Level = 1, MoveName = "Growl" },
                new() { Level = 9, MoveName = "Bite" },
                new() { Level = 13, MoveName = "Water Gun" },
                new() { Level = 17, MoveName = "Headbutt" }
            },
            Evolution = new() { Level = 15, TargetSpecies = "Bibarel" }
        });

        Register(new PokemonSpecies
        {
            DexNumber = 400,
            Name = "Bibarel",
            Category = "Beaver",
            PrimaryType = PokemonType.Normal,
            SecondaryType = PokemonType.Water,
            BaseHP = 79, BaseAttack = 85, BaseDefense = 60, BaseSpAttack = 55, BaseSpDefense = 60, BaseSpeed = 71,
            CatchRate = 127, BaseExpYield = 144, GrowthRate = GrowthRate.MediumFast,
            Height = 1.0f, Weight = 31.5f,
            DexEntry = "It makes its nest by damming streams with bark and mud. A river dammed by Bibarel never overflows.",
            Learnset = new()
            {
                new() { Level = 1, MoveName = "Water Gun" },
                new() { Level = 15, MoveName = "Water Pulse" },
                new() { Level = 23, MoveName = "Crunch" },
                new() { Level = 30, MoveName = "Surf" }
            }
        });

        // 6. Shinx -> Luxio -> Luxray
        Register(new PokemonSpecies
        {
            DexNumber = 403,
            Name = "Shinx",
            Category = "Flash",
            PrimaryType = PokemonType.Electric,
            BaseHP = 45, BaseAttack = 65, BaseDefense = 34, BaseSpAttack = 40, BaseSpDefense = 34, BaseSpeed = 45,
            CatchRate = 235, BaseExpYield = 53, GrowthRate = GrowthRate.MediumSlow,
            Height = 0.5f, Weight = 9.5f,
            DexEntry = "All of its fur dazzles if danger approaches. It flees while the foe is blinded by the glare.",
            Learnset = new()
            {
                new() { Level = 1, MoveName = "Tackle" },
                new() { Level = 5, MoveName = "Leer" },
                new() { Level = 9, MoveName = "Thunder Shock" },
                new() { Level = 13, MoveName = "Bite" },
                new() { Level = 17, MoveName = "Spark" }
            },
            Evolution = new() { Level = 15, TargetSpecies = "Luxio" }
        });

        Register(new PokemonSpecies
        {
            DexNumber = 404,
            Name = "Luxio",
            Category = "Spark",
            PrimaryType = PokemonType.Electric,
            BaseHP = 60, BaseAttack = 85, BaseDefense = 49, BaseSpAttack = 60, BaseSpDefense = 49, BaseSpeed = 60,
            CatchRate = 120, BaseExpYield = 127, GrowthRate = GrowthRate.MediumSlow,
            Height = 0.9f, Weight = 30.5f,
            DexEntry = "Strong electricity courses through the tips of its sharp claws. A light scratch causes fainting.",
            Learnset = new()
            {
                new() { Level = 1, MoveName = "Thunder Shock" },
                new() { Level = 13, MoveName = "Bite" },
                new() { Level = 18, MoveName = "Spark" },
                new() { Level = 28, MoveName = "Crunch" },
                new() { Level = 38, MoveName = "Thunderbolt" }
            },
            Evolution = new() { Level = 30, TargetSpecies = "Luxray" }
        });

        Register(new PokemonSpecies
        {
            DexNumber = 405,
            Name = "Luxray",
            Category = "Gleam Eyes",
            PrimaryType = PokemonType.Electric,
            BaseHP = 80, BaseAttack = 120, BaseDefense = 79, BaseSpAttack = 95, BaseSpDefense = 79, BaseSpeed = 70,
            CatchRate = 45, BaseExpYield = 235, GrowthRate = GrowthRate.MediumSlow,
            Height = 1.4f, Weight = 42.0f,
            DexEntry = "It has eyes that can see through anything. It spots and captures prey hiding behind walls.",
            Learnset = new()
            {
                new() { Level = 1, MoveName = "Spark" },
                new() { Level = 30, MoveName = "Crunch" },
                new() { Level = 42, MoveName = "Thunderbolt" }
            }
        });

        // 7. Riolu -> Lucario
        Register(new PokemonSpecies
        {
            DexNumber = 447,
            Name = "Riolu",
            Category = "Emanation",
            PrimaryType = PokemonType.Fighting,
            BaseHP = 40, BaseAttack = 70, BaseDefense = 40, BaseSpAttack = 35, BaseSpDefense = 40, BaseSpeed = 60,
            CatchRate = 75, BaseExpYield = 57, GrowthRate = GrowthRate.MediumSlow,
            Height = 0.7f, Weight = 20.2f,
            DexEntry = "Its body is lithe yet strong. It can crest three mountains and cross two canyons in one night.",
            Learnset = new()
            {
                new() { Level = 1, MoveName = "Quick Attack" },
                new() { Level = 6, MoveName = "Bite" },
                new() { Level = 15, MoveName = "Mach Punch" },
                new() { Level = 24, MoveName = "Swords Dance" }
            },
            Evolution = new() { Level = 25, TargetSpecies = "Lucario" }
        });

        Register(new PokemonSpecies
        {
            DexNumber = 448,
            Name = "Lucario",
            Category = "Aura",
            PrimaryType = PokemonType.Fighting,
            SecondaryType = PokemonType.Steel,
            BaseHP = 70, BaseAttack = 110, BaseDefense = 70, BaseSpAttack = 115, BaseSpDefense = 70, BaseSpeed = 90,
            CatchRate = 45, BaseExpYield = 184, GrowthRate = GrowthRate.MediumSlow,
            Height = 1.2f, Weight = 54.0f,
            DexEntry = "A well-trained one can sense auras to identify and take in the feelings of creatures a mile away.",
            Learnset = new()
            {
                new() { Level = 1, MoveName = "Mach Punch" },
                new() { Level = 25, MoveName = "Aura Sphere" },
                new() { Level = 34, MoveName = "Close Combat" },
                new() { Level = 42, MoveName = "Dragon Pulse" },
                new() { Level = 51, MoveName = "Earthquake" }
            }
        });

        // 8. Gible -> Gabite -> Garchomp
        Register(new PokemonSpecies
        {
            DexNumber = 443,
            Name = "Gible",
            Category = "Land Shark",
            PrimaryType = PokemonType.Dragon,
            SecondaryType = PokemonType.Ground,
            BaseHP = 58, BaseAttack = 70, BaseDefense = 45, BaseSpAttack = 40, BaseSpDefense = 45, BaseSpeed = 42,
            CatchRate = 45, BaseExpYield = 60, GrowthRate = GrowthRate.Slow,
            Height = 0.7f, Weight = 20.5f,
            DexEntry = "It nests in small, horizontal holes in cave walls. It pounces to catch prey that stray by.",
            Learnset = new()
            {
                new() { Level = 1, MoveName = "Tackle" },
                new() { Level = 7, MoveName = "Dragon Claw" },
                new() { Level = 15, MoveName = "Bite" },
                new() { Level = 22, MoveName = "Earth Power" }
            },
            Evolution = new() { Level = 24, TargetSpecies = "Gabite" }
        });

        Register(new PokemonSpecies
        {
            DexNumber = 444,
            Name = "Gabite",
            Category = "Cave",
            PrimaryType = PokemonType.Dragon,
            SecondaryType = PokemonType.Ground,
            BaseHP = 68, BaseAttack = 90, BaseDefense = 65, BaseSpAttack = 50, BaseSpDefense = 55, BaseSpeed = 82,
            CatchRate = 45, BaseExpYield = 144, GrowthRate = GrowthRate.Slow,
            Height = 1.4f, Weight = 56.0f,
            DexEntry = "It loves shiny things. It searches for gems to stockpile in its nest in dark caves.",
            Learnset = new()
            {
                new() { Level = 1, MoveName = "Dragon Claw" },
                new() { Level = 24, MoveName = "Earthquake" },
                new() { Level = 33, MoveName = "Crunch" }
            },
            Evolution = new() { Level = 48, TargetSpecies = "Garchomp" }
        });

        Register(new PokemonSpecies
        {
            DexNumber = 445,
            Name = "Garchomp",
            Category = "Mach",
            PrimaryType = PokemonType.Dragon,
            SecondaryType = PokemonType.Ground,
            BaseHP = 108, BaseAttack = 130, BaseDefense = 95, BaseSpAttack = 80, BaseSpDefense = 85, BaseSpeed = 102,
            CatchRate = 45, BaseExpYield = 270, GrowthRate = GrowthRate.Slow,
            Height = 1.9f, Weight = 95.0f,
            DexEntry = "When it folds up its body and extends its wings, it looks like a jet plane. It flies at sonic speed.",
            Learnset = new()
            {
                new() { Level = 1, MoveName = "Dragon Claw" },
                new() { Level = 48, MoveName = "Earthquake" },
                new() { Level = 55, MoveName = "Fire Blast" },
                new() { Level = 64, MoveName = "Swords Dance" }
            }
        });

        // 9. Giratina (Platinum Mascot!)
        Register(new PokemonSpecies
        {
            DexNumber = 487,
            Name = "Giratina",
            Category = "Renegade",
            PrimaryType = PokemonType.Ghost,
            SecondaryType = PokemonType.Dragon,
            BaseHP = 150, BaseAttack = 120, BaseDefense = 100, BaseSpAttack = 120, BaseSpDefense = 100, BaseSpeed = 90,
            CatchRate = 3, BaseExpYield = 306, GrowthRate = GrowthRate.Slow,
            Height = 4.5f, Weight = 750.0f,
            DexEntry = "It was banished for its violence. It silently gazed upon the old world from the Distortion World.",
            Learnset = new()
            {
                new() { Level = 1, MoveName = "Dragon Claw" },
                new() { Level = 1, MoveName = "Shadow Ball" },
                new() { Level = 50, MoveName = "Earth Power" },
                new() { Level = 60, MoveName = "Aura Sphere" },
                new() { Level = 70, MoveName = "Shadow Force" }
            }
        });
    }

    private static void Register(PokemonSpecies species)
    {
        SpeciesByName[species.Name] = species;
        SpeciesByDex[species.DexNumber] = species;
    }

    public static PokemonSpecies? Get(string name)
    {
        SpeciesByName.TryGetValue(name, out var species);
        return species;
    }

    public static PokemonSpecies? GetByDex(int dexNumber)
    {
        SpeciesByDex.TryGetValue(dexNumber, out var species);
        return species;
    }

    public static IEnumerable<PokemonSpecies> GetAll() => SpeciesByName.Values;
}
