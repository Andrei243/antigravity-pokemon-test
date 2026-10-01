using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Data;

/// <summary>An ability: its name, a short description (our own words) and what it does in battle.</summary>
public sealed class Ability
{
    public string Name { get; }
    public string Description { get; }

    /// <summary>What it does in battle; null while it isn't implemented yet (it then shows up but does nothing).</summary>
    public BattleEffect? Effect { get; }

    public bool IsImplemented => Effect != null;

    public Ability(string name, string description, BattleEffect? effect)
    {
        Name = name;
        Description = description;
        Effect = effect;
    }
}

/// <summary>
/// Abilities by name, and which ones each species can have. Effects follow Generation 4. The species table is a
/// stand-in until the species data files carry abilities (plan 03 · D1); <see cref="PokemonSpecies.Abilities"/>
/// wins when it is filled.
/// </summary>
public static class AbilityDatabase
{
    private static readonly Dictionary<string, Ability> Abilities = new(StringComparer.OrdinalIgnoreCase);

    static AbilityDatabase()
    {
        // Power in a pinch and other attack boosts
        Add("Overgrow", "Powers up Grass-type moves when the Pokémon is in trouble.", new PinchTypeBoost(PokemonType.Grass));
        Add("Blaze", "Powers up Fire-type moves when the Pokémon is in trouble.", new PinchTypeBoost(PokemonType.Fire));
        Add("Torrent", "Powers up Water-type moves when the Pokémon is in trouble.", new PinchTypeBoost(PokemonType.Water));
        Add("Swarm", "Powers up Bug-type moves when the Pokémon is in trouble.", new PinchTypeBoost(PokemonType.Bug));
        Add("Huge Power", "Doubles the Pokémon's Attack.", new AttackBoost(2f));
        Add("Pure Power", "Doubles the Pokémon's Attack.", new AttackBoost(2f));
        Add("Hustle", "Boosts Attack, but physical moves are less accurate.", new Hustle());
        Add("Guts", "Boosts Attack while the Pokémon has a status problem; burns don't weaken it.",
            new Guts());
        Add("Technician", "Powers up the Pokémon's weaker moves.", new TechnicianEffect());
        Add("Iron Fist", "Powers up punching moves.", new IronFist());
        Add("Reckless", "Powers up moves that hurt the user with recoil.", new Reckless());
        Add("Rivalry", "Hits harder against the same gender, softer against the opposite one.", new Rivalry());
        Add("Adaptability", "Moves of the Pokémon's own type get an even bigger boost.", new FlagEffect { Adaptability = true });
        Add("Tinted Lens", "Moves that are not very effective hit with double the power.", new TintedLens());
        Add("Sniper", "Critical hits do even more damage.", new Sniper());
        Add("Super Luck", "Raises the chance of critical hits.", new FlagEffect { CritBonus = 1 });
        Add("Compound Eyes", "Raises the accuracy of the Pokémon's moves.", new FlagEffect { Accuracy = 1.3f });
        Add("No Guard", "Moves used by or against the Pokémon always hit.", new NoGuard());
        Add("Serene Grace", "Doubles the chance of moves' side effects.", new FlagEffect { SideEffectMultiplier = 2 });
        Add("Rock Head", "Protects the Pokémon from recoil damage.", new FlagEffect { NoRecoil = true });
        Add("Mold Breaker", "The Pokémon's moves ignore the target's ability.", new FlagEffect { BreakAbilities = true });
        Add("Unaware", "Ignores the other Pokémon's stat changes when attacking or being attacked.", new FlagEffect { Unaware = true });
        Add("Scrappy", "Normal- and Fighting-type moves can hit Ghost types.", new Scrappy());

        // On entry
        Add("Intimidate", "Lowers the foes' Attack when the Pokémon enters battle.", new Intimidate());
        Add("Download", "Raises Attack or Sp. Atk, whichever the foes defend against worse, on entry.", new Download());

        // Defensive
        Add("Levitate", "Floats above the ground, out of reach of Ground-type moves.", new Levitate());
        Add("Volt Absorb", "Electric-type moves restore HP instead of doing damage.", new AbsorbType(PokemonType.Electric, "Volt Absorb"));
        Add("Water Absorb", "Water-type moves restore HP instead of doing damage.", new AbsorbType(PokemonType.Water, "Water Absorb"));
        Add("Flash Fire", "Takes in Fire-type moves to power up its own Fire moves.", new FlashFire());
        Add("Motor Drive", "Electric-type moves raise its Speed instead of doing damage.", new MotorDrive());
        Add("Wonder Guard", "Only super-effective moves can hurt it.", new WonderGuard());
        Add("Soundproof", "Unaffected by sound-based moves.", new Soundproof());
        Add("Thick Fat", "Halves damage from Fire- and Ice-type moves.",
            new DamageShield((m, _) => m.Type is PokemonType.Fire or PokemonType.Ice, 0.5f));
        Add("Heatproof", "Halves damage from Fire-type moves.", new DamageShield((m, _) => m.Type == PokemonType.Fire, 0.5f));
        Add("Filter", "Softens super-effective hits.", new DamageShield((_, e) => e > 1f, 0.75f));
        Add("Solid Rock", "Softens super-effective hits.", new DamageShield((_, e) => e > 1f, 0.75f));
        Add("Marvel Scale", "Boosts Defense while the Pokémon has a status problem.", new MarvelScale());
        Add("Battle Armor", "Hard armour protects it from critical hits.", new FlagEffect { NoCrits = true });
        Add("Shell Armor", "A hard shell protects it from critical hits.", new FlagEffect { NoCrits = true });
        Add("Shield Dust", "Protects it from the side effects of moves.", new ShieldDust());
        Add("Liquid Ooze", "Moves that drain its HP hurt the attacker instead.", new LiquidOoze());
        Add("Magic Guard", "Only direct attacks can hurt it.", new MagicGuard());
        Add("Tangled Feet", "Harder to hit while it is confused.", new TangledFeet());
        Add("Pressure", "Moves aimed at it use up more PP.", new FlagEffect { PpPressure = 1 });

        // Contact punishers
        Add("Static", "Contact with it may cause paralysis.", new ContactStatus(30, StatusCondition.Paralyze));
        Add("Flame Body", "Contact with it may cause a burn.", new ContactStatus(30, StatusCondition.Burn));
        Add("Poison Point", "Contact with it may poison the attacker.", new ContactStatus(30, StatusCondition.Poison));
        Add("Effect Spore", "Contact with it may cause poison, paralysis or sleep.",
            new ContactStatus(30, StatusCondition.Poison, StatusCondition.Paralyze, StatusCondition.Sleep));
        Add("Rough Skin", "Hurts attackers that touch it.", new RoughSkin());

        // Status and stat protection
        Add("Immunity", "Prevents poisoning.", new StatusImmunity(StatusCondition.Poison, StatusCondition.Toxic));
        Add("Insomnia", "Prevents sleep.", new StatusImmunity(StatusCondition.Sleep));
        Add("Vital Spirit", "Prevents sleep.", new StatusImmunity(StatusCondition.Sleep));
        Add("Limber", "Prevents paralysis.", new StatusImmunity(StatusCondition.Paralyze));
        Add("Water Veil", "Prevents burns.", new StatusImmunity(StatusCondition.Burn));
        Add("Magma Armor", "Prevents freezing.", new StatusImmunity(StatusCondition.Freeze));
        Add("Own Tempo", "Prevents confusion.", new FlagEffect { NoConfusion = true });
        Add("Inner Focus", "Its focus keeps it from flinching.", new FlagEffect { NoFlinch = true });
        Add("Clear Body", "Other Pokémon can't lower its stats.", new StatGuard());
        Add("White Smoke", "Other Pokémon can't lower its stats.", new StatGuard());
        Add("Hyper Cutter", "Other Pokémon can't lower its Attack.", new StatGuard(StatType.Attack));
        Add("Keen Eye", "Other Pokémon can't lower its accuracy.", new StatGuard(StatType.Accuracy));
        Add("Simple", "Stat changes on it are doubled.", new SimpleEffect());
        Add("Early Bird", "Wakes up from sleep twice as fast.", new FlagEffect { SleepRate = 2 });

        // Over time and reactions
        Add("Speed Boost", "Its Speed rises at the end of every turn.", new SpeedBoost());
        Add("Shed Skin", "May shed off a status problem at the end of a turn.", new ShedSkin());
        Add("Natural Cure", "Status problems heal when it is withdrawn.", new NaturalCure());
        Add("Quick Feet", "Boosts Speed while it has a status problem.", new QuickFeet());
        Add("Anger Point", "Maxes Attack after taking a critical hit.", new AngerPoint());
        Add("Steadfast", "Its Speed rises each time it flinches.", new Steadfast());
        Add("Synchronize", "Passes a burn, poison or paralysis back to the Pokémon that caused it.", new Synchronize());

        // Shown, but with no battle effect yet (weather, infatuation and field effects come later)
        foreach (var (name, text) in new[]
        {
            ("Sand Veil", "Harder to hit in a sandstorm."),
            ("Run Away", "Can always run from wild Pokémon."),
            ("Stench", "Its smell keeps wild Pokémon away."),
            ("Cute Charm", "Contact with it may cause infatuation."),
            ("Pickup", "May pick up items after battles."),
            ("Swift Swim", "Boosts Speed in the rain."),
            ("Chlorophyll", "Boosts Speed in sunshine."),
            ("Drizzle", "Makes it rain on entry."),
            ("Drought", "Brings out the sun on entry."),
            ("Sand Stream", "Whips up a sandstorm on entry."),
            ("Snow Warning", "Brings a hailstorm on entry."),
            ("Sturdy", "One-hit knockout moves don't work on it."),
            ("Oblivious", "Can't be infatuated."),
            ("Lightning Rod", "Draws Electric-type moves to itself."),
        })
        {
            Add(name, text, null);
        }
    }

    // The abilities of the species in today's Pokédex (Platinum's; there were no hidden abilities yet)
    private static readonly Dictionary<string, string[]> SpeciesAbilities = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Turtwig"] = new[] { "Overgrow" }, ["Grotle"] = new[] { "Overgrow" }, ["Torterra"] = new[] { "Overgrow" },
        ["Chimchar"] = new[] { "Blaze" }, ["Monferno"] = new[] { "Blaze" }, ["Infernape"] = new[] { "Blaze" },
        ["Piplup"] = new[] { "Torrent" }, ["Prinplup"] = new[] { "Torrent" }, ["Empoleon"] = new[] { "Torrent" },
        ["Starly"] = new[] { "Keen Eye" }, ["Staravia"] = new[] { "Intimidate" }, ["Staraptor"] = new[] { "Intimidate" },
        ["Bidoof"] = new[] { "Simple", "Unaware" }, ["Bibarel"] = new[] { "Simple", "Unaware" },
        ["Shinx"] = new[] { "Rivalry", "Intimidate" }, ["Luxio"] = new[] { "Rivalry", "Intimidate" }, ["Luxray"] = new[] { "Rivalry", "Intimidate" },
        ["Riolu"] = new[] { "Steadfast", "Inner Focus" }, ["Lucario"] = new[] { "Steadfast", "Inner Focus" },
        ["Gible"] = new[] { "Sand Veil" }, ["Gabite"] = new[] { "Sand Veil" }, ["Garchomp"] = new[] { "Sand Veil" },
        ["Giratina"] = new[] { "Pressure" },
    };

    public static void Initialize() { }

    private static void Add(string name, string description, BattleEffect? effect) =>
        Abilities[name] = new Ability(name, description, effect);

    public static Ability? Get(string? name) => name != null && Abilities.TryGetValue(name, out var a) ? a : null;

    public static IEnumerable<Ability> GetAll() => Abilities.Values;

    /// <summary>The abilities a species can have (one or two).</summary>
    public static IReadOnlyList<string> ForSpecies(PokemonSpecies species)
    {
        if (species.Abilities.Count > 0) return species.Abilities;
        return SpeciesAbilities.TryGetValue(species.Name, out var list) ? list : Array.Empty<string>();
    }

    /// <summary>Picks one of the species' abilities at random, as for a newly met Pokémon.</summary>
    public static string? PickFor(PokemonSpecies species, Random rng)
    {
        var list = ForSpecies(species);
        return list.Count == 0 ? null : list[rng.Next(list.Count)];
    }
}
