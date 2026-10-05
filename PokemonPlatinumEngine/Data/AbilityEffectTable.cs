using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Battle.Sim;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// What each implemented ability does in battle, by name (Generation 4 rules). <see cref="AbilityDatabase"/> pairs
/// these with the abilities in <c>abilities.json</c>; an ability missing here shows up but does nothing yet. Kept
/// apart from the database so tools can ask which abilities run without loading the data files.
/// </summary>
public static class AbilityEffectTable
{
    private static readonly Dictionary<string, BattleEffect> Effects = new(StringComparer.OrdinalIgnoreCase);

    static AbilityEffectTable()
    {
        // Power in a pinch and other attack boosts
        Add("Overgrow", new PinchTypeBoost(PokemonType.Grass));
        Add("Blaze", new PinchTypeBoost(PokemonType.Fire));
        Add("Torrent", new PinchTypeBoost(PokemonType.Water));
        Add("Swarm", new PinchTypeBoost(PokemonType.Bug));
        Add("Huge Power", new AttackBoost(2f));
        Add("Pure Power", new AttackBoost(2f));
        Add("Hustle", new Hustle());
        Add("Guts", new Guts());
        Add("Technician", new TechnicianEffect());
        Add("Iron Fist", new IronFist());
        Add("Reckless", new Reckless());
        Add("Rivalry", new Rivalry());
        Add("Adaptability", new FlagEffect { Adaptability = true });
        Add("Tinted Lens", new TintedLens());
        Add("Sniper", new Sniper());
        Add("Super Luck", new FlagEffect { CritBonus = 1 });
        Add("Compound Eyes", new FlagEffect { Accuracy = 1.3f });
        Add("No Guard", new NoGuard());
        Add("Serene Grace", new FlagEffect { SideEffectMultiplier = 2 });
        Add("Rock Head", new FlagEffect { NoRecoil = true });
        Add("Mold Breaker", new FlagEffect { BreakAbilities = true });
        Add("Unaware", new FlagEffect { Unaware = true });
        Add("Run Away", new FlagEffect { Escapes = true });
        Add("Scrappy", new Scrappy());
        Add("Skill Link", new FlagEffect { FiveHits = true });

        // On entry
        Add("Intimidate", new Intimidate());
        Add("Download", new Download());

        // Defensive
        Add("Levitate", new Levitate());
        Add("Volt Absorb", new AbsorbType(PokemonType.Electric, "Volt Absorb"));
        Add("Water Absorb", new AbsorbType(PokemonType.Water, "Water Absorb"));
        Add("Flash Fire", new FlashFire());
        Add("Motor Drive", new MotorDrive());
        Add("Wonder Guard", new WonderGuard());
        Add("Soundproof", new Soundproof());
        Add("Thick Fat", new DamageShield((m, _) => m.Type is PokemonType.Fire or PokemonType.Ice, 0.5f));
        Add("Heatproof", new DamageShield((m, _) => m.Type == PokemonType.Fire, 0.5f));
        Add("Filter", new DamageShield((_, e) => e > 1f, 0.75f));
        Add("Solid Rock", new DamageShield((_, e) => e > 1f, 0.75f));
        Add("Marvel Scale", new MarvelScale());
        Add("Battle Armor", new FlagEffect { NoCrits = true });
        Add("Shell Armor", new FlagEffect { NoCrits = true });
        Add("Shield Dust", new ShieldDust());
        Add("Liquid Ooze", new LiquidOoze());
        Add("Magic Guard", new MagicGuard());
        Add("Tangled Feet", new TangledFeet());
        Add("Pressure", new FlagEffect { PpPressure = 1 });

        // Contact punishers
        Add("Static", new ContactStatus(30, StatusCondition.Paralyze));
        Add("Flame Body", new ContactStatus(30, StatusCondition.Burn));
        Add("Poison Point", new ContactStatus(30, StatusCondition.Poison));
        Add("Effect Spore", new ContactStatus(30, StatusCondition.Poison, StatusCondition.Paralyze, StatusCondition.Sleep));
        Add("Rough Skin", new RoughSkin());

        // Status and stat protection
        Add("Immunity", new StatusImmunity(StatusCondition.Poison, StatusCondition.Toxic));
        Add("Insomnia", new StatusImmunity(StatusCondition.Sleep));
        Add("Vital Spirit", new StatusImmunity(StatusCondition.Sleep));
        Add("Limber", new StatusImmunity(StatusCondition.Paralyze));
        Add("Water Veil", new StatusImmunity(StatusCondition.Burn));
        Add("Magma Armor", new StatusImmunity(StatusCondition.Freeze));
        Add("Own Tempo", new FlagEffect { NoConfusion = true });
        Add("Inner Focus", new FlagEffect { NoFlinch = true });
        Add("Clear Body", new StatGuard());
        Add("White Smoke", new StatGuard());
        Add("Hyper Cutter", new StatGuard(StatType.Attack));
        Add("Keen Eye", new StatGuard(StatType.Accuracy));
        Add("Simple", new SimpleEffect());
        Add("Early Bird", new FlagEffect { SleepRate = 2 });

        // Over time and reactions
        Add("Speed Boost", new SpeedBoost());
        Add("Shed Skin", new ShedSkin());
        Add("Natural Cure", new NaturalCure());
        Add("Quick Feet", new QuickFeet());
        Add("Anger Point", new AngerPoint());
        Add("Steadfast", new Steadfast());
        Add("Synchronize", new Synchronize());

        // The weather (plan 06 · R3)
        Add("Drizzle", new WeatherBringer(BattleWeather.Rain, "Rain", "{0}'s Drizzle brought the rain!"));
        Add("Drought", new WeatherBringer(BattleWeather.Sun, "Sun", "{0}'s Drought turned the sunlight harsh!"));
        Add("Sand Stream", new WeatherBringer(BattleWeather.Sandstorm, "Sandstorm", "{0}'s Sand Stream whipped up a sandstorm!"));
        Add("Snow Warning", new WeatherBringer(BattleWeather.Hail, "Hail", "{0}'s Snow Warning whipped up a hailstorm!"));
        Add("Cloud Nine", new WeatherBlind());
        Add("Air Lock", new WeatherBlind());
        Add("Swift Swim", new WeatherSpeed(BattleWeather.Rain));
        Add("Chlorophyll", new WeatherSpeed(BattleWeather.Sun));
        Add("Sand Veil", new WeatherCloak(BattleWeather.Sandstorm));
        Add("Snow Cloak", new WeatherCloak(BattleWeather.Hail));
        Add("Rain Dish", new WeatherHealer(BattleWeather.Rain, "Rain Dish"));
        Add("Ice Body", new WeatherHealer(BattleWeather.Hail, "Ice Body"));
        Add("Dry Skin", new DrySkin());
        Add("Solar Power", new SolarPower());
        Add("Hydration", new Hydration());
        Add("Leaf Guard", new LeafGuard());

        // Holding a foe on the field, and holding one's own ground
        Add("Shadow Tag", new Trapper("Shadow Tag"));
        Add("Arena Trap", new Trapper("Arena Trap"));
        Add("Magnet Pull", new Trapper("Magnet Pull"));
        Add("Suction Cups", new FlagEffect { Anchored = true });
        Add("Oblivious", new FlagEffect { NoRomance = true });
        // Read by the end of a turn itself, for every sleeper across the field
        Add("Bad Dreams", new FlagEffect());
    }

    private static void Add(string name, BattleEffect effect) => Effects[name] = effect;

    public static BattleEffect? Get(string name) => Effects.GetValueOrDefault(name);

    /// <summary>The names of the abilities that have a battle effect.</summary>
    public static IEnumerable<string> Names => Effects.Keys;
}
