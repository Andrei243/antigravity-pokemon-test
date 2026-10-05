using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Battle.Sim;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// What each ability does in battle, by name (Platinum's rules; plan 06 · R7 completed the 123). <see cref="AbilityDatabase"/>
/// pairs these with the abilities in <c>abilities.json</c>; an ability missing here shows up but does nothing yet.
/// Kept apart from the database so tools can ask which abilities run without loading the data files. A bonus inside
/// the damage formula is applied by <c>DamageCalculator</c> by the ability's name, in the original's place; an
/// ability that works only in the field (Pickup, Stench…) has a bare entry here and its code in the field, by name.
/// </summary>
public static class AbilityEffectTable
{
    private static readonly Dictionary<string, BattleEffect> Effects = new(StringComparer.OrdinalIgnoreCase);

    static AbilityEffectTable()
    {
        // In the damage formula, by name: a power or a stat in the original's own place
        foreach (var name in new[] { "Overgrow", "Blaze", "Torrent", "Swarm", "Huge Power", "Pure Power", "Technician", "Iron Fist", "Reckless", "Rivalry",
                     "Thick Fat", "Heatproof", "Marvel Scale", "Plus", "Minus" })
            Add(name, new FlagEffect());
        Add("Hustle", new Hustle());
        Add("Guts", new Guts());
        Add("Adaptability", new FlagEffect { Adaptability = true });
        Add("Tinted Lens", new TintedLens());
        Add("Sniper", new Sniper());
        Add("Super Luck", new FlagEffect { CritBonus = 1 });
        Add("Compound Eyes", new FlagEffect { Accuracy = 1.3f });
        Add("No Guard", new NoGuard());
        Add("Serene Grace", new FlagEffect { SideEffectMultiplier = 2 });
        Add("Rock Head", new FlagEffect { NoRecoil = true });
        Add("Mold Breaker", new FlagEffect { BreakAbilities = true, EntryLine = "{0} breaks the mold!" });
        Add("Unaware", new FlagEffect { Unaware = true });
        Add("Run Away", new FlagEffect { Escapes = true });
        Add("Scrappy", new Scrappy());
        Add("Skill Link", new FlagEffect { FiveHits = true });
        Add("Sticky Hold", new FlagEffect { KeepsHeldItem = true });
        Add("Normalize", new FlagEffect { Normalizes = true });
        Add("Klutz", new FlagEffect { Klutz = true });
        Add("Stall", new FlagEffect { Stalls = true });
        Add("Truant", new FlagEffect { Loafs = true });
        Add("Slow Start", new SlowStart());
        Add("Unburden", new Unburden());
        Add("Gluttony", new FlagEffect { EatsEarly = true });

        // On entry
        Add("Intimidate", new Intimidate());
        Add("Download", new Download());
        Add("Trace", new Trace());
        Add("Anticipation", new Anticipation());
        Add("Forewarn", new Forewarn());
        Add("Frisk", new Frisk());
        Add("Pressure", new FlagEffect { PpPressure = 1, EntryLine = "{0} is exerting its Pressure!" });

        // Defensive
        Add("Levitate", new Levitate());
        Add("Volt Absorb", new AbsorbType(PokemonType.Electric, "Volt Absorb"));
        Add("Water Absorb", new AbsorbType(PokemonType.Water, "Water Absorb"));
        Add("Flash Fire", new FlashFire());
        Add("Motor Drive", new MotorDrive());
        Add("Wonder Guard", new WonderGuard());
        Add("Soundproof", new Soundproof());
        Add("Filter", new DamageShield((_, e) => e > 1f, 0.75f));
        Add("Solid Rock", new DamageShield((_, e) => e > 1f, 0.75f));
        Add("Battle Armor", new FlagEffect { NoCrits = true });
        Add("Shell Armor", new FlagEffect { NoCrits = true });
        Add("Shield Dust", new ShieldDust());
        Add("Liquid Ooze", new LiquidOoze());
        Add("Magic Guard", new MagicGuard());
        Add("Tangled Feet", new TangledFeet());
        Add("Lightning Rod", new FlagEffect { Draws = PokemonType.Electric });
        Add("Storm Drain", new FlagEffect { Draws = PokemonType.Water });
        // Read by name where they matter: Sturdy against a one-hit knockout, Damp against Explosion and Aftermath
        Add("Sturdy", new FlagEffect());
        Add("Damp", new FlagEffect());

        // Answering a hit
        Add("Static", new ContactStatus(30, StatusCondition.Paralyze));
        Add("Flame Body", new ContactStatus(30, StatusCondition.Burn));
        Add("Poison Point", new ContactStatus(30, StatusCondition.Poison));
        Add("Effect Spore", new ContactStatus(30, StatusCondition.Poison, StatusCondition.Paralyze, StatusCondition.Sleep));
        Add("Rough Skin", new RoughSkin());
        Add("Color Change", new ColorChange());
        Add("Aftermath", new Aftermath());
        Add("Cute Charm", new CuteCharm());

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
        Add("Poison Heal", new FlagEffect { PoisonHeals = true });

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

        // Shapes (plan 06 · R7)
        Add("Forecast", new Forecast());
        Add("Flower Gift", new FlowerGift());
        Add("Multitype", new Multitype());

        // Holding a foe on the field, and holding one's own ground
        Add("Shadow Tag", new Trapper("Shadow Tag"));
        Add("Arena Trap", new Trapper("Arena Trap"));
        Add("Magnet Pull", new Trapper("Magnet Pull"));
        Add("Suction Cups", new FlagEffect { Anchored = true });
        Add("Oblivious", new FlagEffect { NoRomance = true });
        // Read by the end of a turn itself, for every sleeper across the field
        Add("Bad Dreams", new FlagEffect());

        // In the field alone (plan 06 · R7): the wild Pokémon met, and what the party picks up after a battle, go by these names
        foreach (var name in new[] { "Stench", "Illuminate", "Pickup", "Honey Gather" }) Add(name, new FlagEffect());
    }

    private static void Add(string name, BattleEffect effect) => Effects[name] = effect;

    public static BattleEffect? Get(string name) => Effects.GetValueOrDefault(name);

    /// <summary>The names of the abilities that have a battle effect.</summary>
    public static IEnumerable<string> Names => Effects.Keys;
}
