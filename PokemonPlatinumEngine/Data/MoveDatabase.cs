using System.Collections.Generic;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Data;

public static class MoveDatabase
{
    private static readonly Dictionary<string, MoveData> Moves = new(System.StringComparer.OrdinalIgnoreCase);

    public static void Initialize() { }

    static MoveDatabase()
    {
        Register(new MoveData
        {
            Id = 1,
            Name = "Tackle",
            Type = PokemonType.Normal,
            Category = MoveCategory.Physical,
            Power = 40,
            Accuracy = 100,
            MaxPP = 35,
            Description = "A physical attack in which the user charges and slams into the foe."
        });

        Register(new MoveData
        {
            Id = 2,
            Name = "Scratch",
            Type = PokemonType.Normal,
            Category = MoveCategory.Physical,
            Power = 40,
            Accuracy = 100,
            MaxPP = 35,
            Description = "Hard, pointed, sharp claws rake the target to inflict damage."
        });

        Register(new MoveData
        {
            Id = 3,
            Name = "Pound",
            Type = PokemonType.Normal,
            Category = MoveCategory.Physical,
            Power = 40,
            Accuracy = 100,
            MaxPP = 35,
            Description = "The target is physically pounded with a long tail or a foreleg, etc."
        });

        Register(new MoveData
        {
            Id = 4,
            Name = "Quick Attack",
            Type = PokemonType.Normal,
            Category = MoveCategory.Physical,
            Power = 40,
            Accuracy = 100,
            MaxPP = 30,
            Priority = 1,
            Description = "An almost invisibly fast attack that always goes first."
        });

        Register(new MoveData
        {
            Id = 5,
            Name = "Ember",
            Type = PokemonType.Fire,
            Category = MoveCategory.Special,
            Power = 40,
            Accuracy = 100,
            MaxPP = 25,
            InflictStatus = StatusCondition.Burn,
            StatusChancePercent = 10,
            Description = "The foe is attacked with small flames. It may also leave the target with a burn."
        });

        Register(new MoveData
        {
            Id = 6,
            Name = "Flame Wheel",
            Type = PokemonType.Fire,
            Category = MoveCategory.Physical,
            Power = 60,
            Accuracy = 100,
            MaxPP = 25,
            InflictStatus = StatusCondition.Burn,
            StatusChancePercent = 10,
            Description = "The user cloaks itself in fire and charges at the foe."
        });

        Register(new MoveData
        {
            Id = 7,
            Name = "Flamethrower",
            Type = PokemonType.Fire,
            Category = MoveCategory.Special,
            Power = 95,
            Accuracy = 100,
            MaxPP = 15,
            InflictStatus = StatusCondition.Burn,
            StatusChancePercent = 10,
            Description = "The foe is scorched with an intense blast of fire."
        });

        Register(new MoveData
        {
            Id = 8,
            Name = "Fire Blast",
            Type = PokemonType.Fire,
            Category = MoveCategory.Special,
            Power = 120,
            Accuracy = 85,
            MaxPP = 5,
            InflictStatus = StatusCondition.Burn,
            StatusChancePercent = 10,
            Description = "The target is attacked with an intense blast of all-consuming fire."
        });

        Register(new MoveData
        {
            Id = 9,
            Name = "Water Gun",
            Type = PokemonType.Water,
            Category = MoveCategory.Special,
            Power = 40,
            Accuracy = 100,
            MaxPP = 25,
            Description = "The target is blasted with a forceful shot of water."
        });

        Register(new MoveData
        {
            Id = 10,
            Name = "Bubble",
            Type = PokemonType.Water,
            Category = MoveCategory.Special,
            Power = 40,
            Accuracy = 100,
            MaxPP = 30,
            TargetStatChange = StatType.Speed,
            StatStageAmount = -1,
            StatChangeChancePercent = 10,
            Description = "A spray of bubbles is forcefully blown at the foe."
        });

        Register(new MoveData
        {
            Id = 11,
            Name = "Water Pulse",
            Type = PokemonType.Water,
            Category = MoveCategory.Special,
            Power = 60,
            Accuracy = 100,
            MaxPP = 20,
            Description = "The user attacks the target with a pulsing blast of water."
        });

        Register(new MoveData
        {
            Id = 12,
            Name = "Surf",
            Type = PokemonType.Water,
            Category = MoveCategory.Special,
            Power = 95,
            Accuracy = 100,
            MaxPP = 15,
            Description = "It swamps the entire area around the user with a giant wave."
        });

        Register(new MoveData
        {
            Id = 13,
            Name = "Hydro Pump",
            Type = PokemonType.Water,
            Category = MoveCategory.Special,
            Power = 120,
            Accuracy = 80,
            MaxPP = 5,
            Description = "The foe is blasted by a huge volume of water launched under great pressure."
        });

        Register(new MoveData
        {
            Id = 14,
            Name = "Razor Leaf",
            Type = PokemonType.Grass,
            Category = MoveCategory.Physical,
            Power = 55,
            Accuracy = 95,
            MaxPP = 25,
            CritStage = 1,
            Description = "Sharp-edged leaves are launched to slash at the opposing team."
        });

        Register(new MoveData
        {
            Id = 15,
            Name = "Vine Whip",
            Type = PokemonType.Grass,
            Category = MoveCategory.Physical,
            Power = 45,
            Accuracy = 100,
            MaxPP = 25,
            Description = "The target is struck with slender, whiplike vines to inflict damage."
        });

        Register(new MoveData
        {
            Id = 16,
            Name = "Mega Drain",
            Type = PokemonType.Grass,
            Category = MoveCategory.Special,
            Power = 40,
            Accuracy = 100,
            MaxPP = 15,
            DrainPercent = 50,
            Description = "A nutrient-draining attack. The user's HP is restored by half the damage taken by the target."
        });

        Register(new MoveData
        {
            Id = 17,
            Name = "Energy Ball",
            Type = PokemonType.Grass,
            Category = MoveCategory.Special,
            Power = 90,
            Accuracy = 100,
            MaxPP = 10,
            TargetStatChange = StatType.SpDefense,
            StatStageAmount = -1,
            StatChangeChancePercent = 10,
            Description = "The user draws power from nature and fires it at the foe."
        });

        Register(new MoveData
        {
            Id = 18,
            Name = "Wood Hammer",
            Type = PokemonType.Grass,
            Category = MoveCategory.Physical,
            Power = 120,
            Accuracy = 100,
            MaxPP = 15,
            RecoilPercent = 33,
            Description = "The user slams its rugged body into the foe. This also damages the user a little."
        });

        Register(new MoveData
        {
            Id = 19,
            Name = "Thunder Shock",
            Type = PokemonType.Electric,
            Category = MoveCategory.Special,
            Power = 40,
            Accuracy = 100,
            MaxPP = 30,
            InflictStatus = StatusCondition.Paralyze,
            StatusChancePercent = 10,
            Description = "A jolt of electricity is hurled at the foe to inflict damage."
        });

        Register(new MoveData
        {
            Id = 20,
            Name = "Spark",
            Type = PokemonType.Electric,
            Category = MoveCategory.Physical,
            Power = 65,
            Accuracy = 100,
            MaxPP = 20,
            InflictStatus = StatusCondition.Paralyze,
            StatusChancePercent = 30,
            Description = "The user throws an electrically charged tackle at the foe."
        });

        Register(new MoveData
        {
            Id = 21,
            Name = "Thunderbolt",
            Type = PokemonType.Electric,
            Category = MoveCategory.Special,
            Power = 95,
            Accuracy = 100,
            MaxPP = 15,
            InflictStatus = StatusCondition.Paralyze,
            StatusChancePercent = 10,
            Description = "A strong electric blast is loosed at the foe. It may also cause paralysis."
        });

        Register(new MoveData
        {
            Id = 22,
            Name = "Wing Attack",
            Type = PokemonType.Flying,
            Category = MoveCategory.Physical,
            Power = 60,
            Accuracy = 100,
            MaxPP = 35,
            Description = "The target is struck with large, imposing wings spread wide."
        });

        Register(new MoveData
        {
            Id = 23,
            Name = "Aerial Ace",
            Type = PokemonType.Flying,
            Category = MoveCategory.Physical,
            Power = 60,
            Accuracy = 0, // Never misses
            MaxPP = 20,
            Description = "An extremely fast attack that can never miss."
        });

        Register(new MoveData
        {
            Id = 24,
            Name = "Brave Bird",
            Type = PokemonType.Flying,
            Category = MoveCategory.Physical,
            Power = 120,
            Accuracy = 100,
            MaxPP = 15,
            RecoilPercent = 33,
            Description = "The user tucks in its wings and charges from a low altitude. This also damages the user."
        });

        Register(new MoveData
        {
            Id = 25,
            Name = "Bite",
            Type = PokemonType.Dark,
            Category = MoveCategory.Physical,
            Power = 60,
            Accuracy = 100,
            MaxPP = 25,
            Description = "The target is bitten with viciously sharp fangs."
        });

        Register(new MoveData
        {
            Id = 26,
            Name = "Crunch",
            Type = PokemonType.Dark,
            Category = MoveCategory.Physical,
            Power = 80,
            Accuracy = 100,
            MaxPP = 15,
            TargetStatChange = StatType.Defense,
            StatStageAmount = -1,
            StatChangeChancePercent = 20,
            Description = "The user crunches up the foe with sharp fangs. It may lower Defense."
        });

        Register(new MoveData
        {
            Id = 27,
            Name = "Mach Punch",
            Type = PokemonType.Fighting,
            Category = MoveCategory.Physical,
            Power = 40,
            Accuracy = 100,
            MaxPP = 30,
            Priority = 1,
            Description = "The user throws a punch at blinding speed. It always strikes first."
        });

        Register(new MoveData
        {
            Id = 28,
            Name = "Close Combat",
            Type = PokemonType.Fighting,
            Category = MoveCategory.Physical,
            Power = 120,
            Accuracy = 100,
            MaxPP = 5,
            StatChangeTargetSelf = true,
            TargetStatChange = StatType.Defense,
            StatStageAmount = -1,
            StatChangeChancePercent = 100,
            Description = "The user fights the foe up close without guarding itself. Lowers Defense and Sp. Def."
        });

        Register(new MoveData
        {
            Id = 29,
            Name = "Aura Sphere",
            Type = PokemonType.Fighting,
            Category = MoveCategory.Special,
            Power = 80,
            Accuracy = 0,
            MaxPP = 20,
            Description = "The user looses a blast of aura power from its deep within. Never misses."
        });

        Register(new MoveData
        {
            Id = 30,
            Name = "Dragon Claw",
            Type = PokemonType.Dragon,
            Category = MoveCategory.Physical,
            Power = 80,
            Accuracy = 100,
            MaxPP = 15,
            Description = "The user slashes the foe with huge, sharp claws."
        });

        Register(new MoveData
        {
            Id = 31,
            Name = "Dragon Pulse",
            Type = PokemonType.Dragon,
            Category = MoveCategory.Special,
            Power = 90,
            Accuracy = 100,
            MaxPP = 10,
            Description = "The target is attacked with a shock wave generated by the user's gaping mouth."
        });

        Register(new MoveData
        {
            Id = 32,
            Name = "Earthquake",
            Type = PokemonType.Ground,
            Category = MoveCategory.Physical,
            Power = 100,
            Accuracy = 100,
            MaxPP = 10,
            Description = "The user sets off an earthquake that hits all Pokémon in the area."
        });

        Register(new MoveData
        {
            Id = 33,
            Name = "Earth Power",
            Type = PokemonType.Ground,
            Category = MoveCategory.Special,
            Power = 90,
            Accuracy = 100,
            MaxPP = 10,
            TargetStatChange = StatType.SpDefense,
            StatStageAmount = -1,
            StatChangeChancePercent = 10,
            Description = "The user makes the ground under the foe erupt with power."
        });

        Register(new MoveData
        {
            Id = 34,
            Name = "Ice Beam",
            Type = PokemonType.Ice,
            Category = MoveCategory.Special,
            Power = 95,
            Accuracy = 100,
            MaxPP = 10,
            InflictStatus = StatusCondition.Freeze,
            StatusChancePercent = 10,
            Description = "The foe is struck with an icy-cold beam of energy."
        });

        Register(new MoveData
        {
            Id = 35,
            Name = "Shadow Ball",
            Type = PokemonType.Ghost,
            Category = MoveCategory.Special,
            Power = 80,
            Accuracy = 100,
            MaxPP = 15,
            TargetStatChange = StatType.SpDefense,
            StatStageAmount = -1,
            StatChangeChancePercent = 20,
            Description = "The user hurls a shadowy blob at the foe. May lower Sp. Def."
        });

        Register(new MoveData
        {
            Id = 36,
            Name = "Shadow Force",
            Type = PokemonType.Ghost,
            Category = MoveCategory.Physical,
            Power = 120,
            Accuracy = 100,
            MaxPP = 5,
            Description = "The user disappears, then strikes the foe on the next turn. Giratina's signature move."
        });

        // Status moves
        Register(new MoveData
        {
            Id = 37,
            Name = "Growl",
            Type = PokemonType.Normal,
            Category = MoveCategory.Status,
            Power = 0,
            Accuracy = 100,
            MaxPP = 40,
            TargetStatChange = StatType.Attack,
            StatStageAmount = -1,
            StatChangeChancePercent = 100,
            Description = "The user growls in an endearing way, making opposing Pokémon less wary."
        });

        Register(new MoveData
        {
            Id = 38,
            Name = "Leer",
            Type = PokemonType.Normal,
            Category = MoveCategory.Status,
            Power = 0,
            Accuracy = 100,
            MaxPP = 30,
            TargetStatChange = StatType.Defense,
            StatStageAmount = -1,
            StatChangeChancePercent = 100,
            Description = "The user gives opposing Pokémon an intimidating leer that lowers Defense."
        });

        Register(new MoveData
        {
            Id = 39,
            Name = "Tail Whip",
            Type = PokemonType.Normal,
            Category = MoveCategory.Status,
            Power = 0,
            Accuracy = 100,
            MaxPP = 30,
            TargetStatChange = StatType.Defense,
            StatStageAmount = -1,
            StatChangeChancePercent = 100,
            Description = "The user wags its tail cutely, making opposing Pokémon lower their Defense."
        });

        Register(new MoveData
        {
            Id = 40,
            Name = "Swords Dance",
            Type = PokemonType.Normal,
            Category = MoveCategory.Status,
            Power = 0,
            Accuracy = 0,
            MaxPP = 20,
            StatChangeTargetSelf = true,
            TargetStatChange = StatType.Attack,
            StatStageAmount = 2,
            StatChangeChancePercent = 100,
            Description = "A frenetic dance to uplift the fighting spirit. Sharply raises user's Attack."
        });

        Register(new MoveData
        {
            Id = 41,
            Name = "Hypnosis",
            Type = PokemonType.Psychic,
            Category = MoveCategory.Status,
            Power = 0,
            Accuracy = 60,
            MaxPP = 20,
            InflictStatus = StatusCondition.Sleep,
            StatusChancePercent = 100,
            Description = "The user employs hypnotic suggestion to make the target nod off into a deep sleep."
        });

        Register(new MoveData
        {
            Id = 42,
            Name = "Thunder Wave",
            Type = PokemonType.Electric,
            Category = MoveCategory.Status,
            Power = 0,
            Accuracy = 100,
            MaxPP = 20,
            InflictStatus = StatusCondition.Paralyze,
            StatusChancePercent = 100,
            Description = "A weak electric shock that paralyzes the target."
        });

        Register(new MoveData
        {
            Id = 43,
            Name = "Will-O-Wisp",
            Type = PokemonType.Fire,
            Category = MoveCategory.Status,
            Power = 0,
            Accuracy = 85,
            MaxPP = 15,
            InflictStatus = StatusCondition.Burn,
            StatusChancePercent = 100,
            Description = "The user shoots a sinister, bluish-white flame at the target to inflict a burn."
        });

        Register(new MoveData
        {
            Id = 44,
            Name = "Toxic",
            Type = PokemonType.Poison,
            Category = MoveCategory.Status,
            Power = 0,
            Accuracy = 90,
            MaxPP = 10,
            InflictStatus = StatusCondition.Toxic,
            StatusChancePercent = 100,
            Description = "A move that leaves the target badly poisoned. Its poison damage steadily increases."
        });

        Register(new MoveData
        {
            Id = 45,
            Name = "Withdraw",
            Type = PokemonType.Water,
            Category = MoveCategory.Status,
            Power = 0,
            Accuracy = 0,
            MaxPP = 40,
            StatChangeTargetSelf = true,
            TargetStatChange = StatType.Defense,
            StatStageAmount = 1,
            StatChangeChancePercent = 100,
            Description = "The user withdraws its body into its hard shell, raising its Defense."
        });
    }

    private static void Register(MoveData move)
    {
        Moves[move.Name] = move;
    }

    public static MoveData Get(string name)
    {
        if (Moves.TryGetValue(name, out var move))
            return move;
        return Moves["Tackle"];
    }

    public static Move Create(string name)
    {
        return new Move(Get(name));
    }
}
