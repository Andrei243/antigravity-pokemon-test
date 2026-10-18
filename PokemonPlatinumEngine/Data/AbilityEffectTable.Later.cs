using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Data;

// The abilities of Generations 5 to 9 (plan 06 · R27), by Pokémon Showdown's rules (data/abilities.ts at the pinned
// commit). Where a bonus is inside the damage formula the entry is a bare FlagEffect and its code is a line of
// DamageCalculator.Abilities.cs, by name; what the core decides by name (the turn's order, a move's type, a move
// stopped before it lands, a stat lowered by another) is BattleCore.Abilities.cs. docs/mechanics/rulings.md, "The
// later abilities", lists where each is cut down.
public static partial class AbilityEffectTable
{
    private static void AddLater()
    {
        // In the damage formula, by name (DamageCalculator.Abilities.cs)
        foreach (var name in new[]
                 {
                     "Sheer Force", "Defeatist", "Toxic Boost", "Flare Boost", "Analytic", "Tough Claws", "Strong Jaw", "Mega Launcher", "Sharpness",
                     "Punk Rock", "Steelworker", "Steely Spirit", "Transistor", "Dragon's Maw", "Rocky Payload", "Fire Mane", "Fur Coat", "Grass Pelt",
                     "Ice Scales", "Fluffy", "Multiscale", "Shadow Shield", "Neuroforce", "Friend Guard", "Battery", "Power Spot", "Stakeout",
                     "Aura Guard", "Aerilate", "Pixilate", "Refrigerate", "Galvanize", "Dragonize", "Liquid Voice", "Long Reach",
                     "Infiltrator", "Unseen Fist", "Piercing Drill", "Merciless", "Parental Bond", "Corrosion", "Heavy Metal", "Light Metal",
                     "Victory Star", "Wonder Skin", "Prankster", "Gale Wings", "Triage", "Telepathy", "Dazzling", "Queenly Majesty", "Armor Tail",
                     "Good as Gold", "Magic Bounce", "Protean", "Libero", "Defiant", "Competitive", "Mirror Armor", "Propeller Tail", "Stalwart",
                     "Quick Draw", "Mycelium Might", "Soul-Heart", "Poison Puppeteer"
                 })
            Add(name, new FlagEffect());
        Add("Prism Armor", new DamageShield((_, e) => e > 1f, 0.75f));
        Add("Water Bubble", new NoStatus((_, s) => s == StatusCondition.Burn));
        Add("Purifying Salt", new NoStatus((_, s) => s != StatusCondition.None));
        Add("Gorilla Tactics", new GorillaTactics());
        Add("Guard Dog", new GuardDog());
        Add("Comatose", new Comatose());

        // Through the weather or the terrain
        Add("Sand Force", new SandForce());
        Add("Sand Rush", new FieldSpeed(BattleWeather.Sandstorm));
        Add("Slush Rush", new FieldSpeed(BattleWeather.Snow));
        Add("Surge Surfer", new FieldSpeed(null, FieldCondition.ElectricTerrain));
        Add("Electric Surge", new Surge(FieldCondition.ElectricTerrain));
        Add("Grassy Surge", new Surge(FieldCondition.GrassyTerrain));
        Add("Misty Surge", new Surge(FieldCondition.MistyTerrain));
        Add("Psychic Surge", new Surge(FieldCondition.PsychicTerrain));
        Add("Orichalcum Pulse", new Pulse(sun: true));
        Add("Hadron Engine", new Pulse(sun: false));
        Add("Protosynthesis", new Paradox(sun: true));
        Add("Quark Drive", new Paradox(sun: false));
        Add("Mimicry", new Mimicry());
        Add("Overcoat", new Proof(MoveFlags.Powder, weatherProof: true));
        Add("Bulletproof", new Proof(MoveFlags.Ballistic));

        // On entry
        Add("Turboblaze", new Announced("battle.turboblaze", breaksAbilities: true));
        Add("Teravolt", new Announced("battle.teravolt", breaksAbilities: true));
        Add("Dark Aura", new Announced("battle.darkAura"));
        Add("Fairy Aura", new Announced("battle.fairyAura"));
        Add("Aura Break", new Announced("battle.auraBreak"));
        Add("Vessel of Ruin", new Announced("battle.vesselOfRuin"));
        Add("Sword of Ruin", new Announced("battle.swordOfRuin"));
        Add("Tablets of Ruin", new Announced("battle.tabletsOfRuin"));
        Add("Beads of Ruin", new Announced("battle.beadsOfRuin"));
        Add("Intrepid Sword", new OnceABattle(StatType.Attack, 1));
        Add("Dauntless Shield", new OnceABattle(StatType.Defense, 1));
        Add("Supersweet Syrup", new OnceABattle(StatType.Evasion, -1, onFoes: true));
        Add("Screen Cleaner", new ScreenCleaner());
        Add("Curious Medicine", new CuriousMedicine());
        Add("Costar", new Costar());
        Add("Hospitality", new Hospitality());

        // Taking a move in
        Add("Sap Sipper", new AbsorbRaises(m => m.Type == PokemonType.Grass, StatType.Attack, 1));
        Add("Lightning Rod", new AbsorbRaises(m => m.Type == PokemonType.Electric, StatType.SpAttack, 1, PokemonType.Electric));
        Add("Storm Drain", new AbsorbRaises(m => m.Type == PokemonType.Water, StatType.SpAttack, 1, PokemonType.Water));
        Add("Well-Baked Body", new AbsorbRaises(m => m.Type == PokemonType.Fire, StatType.Defense, 2));
        Add("Wind Rider", new WindRider());
        Add("Earth Eater", new EarthEater());

        // Answering a hit
        Add("Justified", new HitRaises(m => m.Type == PokemonType.Dark, StatType.Attack, 1));
        Add("Rattled", new HitRaises(m => m.Type is PokemonType.Bug or PokemonType.Ghost or PokemonType.Dark, StatType.Speed, 1));
        Add("Stamina", new HitRaises(_ => true, StatType.Defense, 1));
        Add("Water Compaction", new HitRaises(m => m.Type == PokemonType.Water, StatType.Defense, 2));
        Add("Steam Engine", new HitRaises(m => m.Type is PokemonType.Fire or PokemonType.Water, StatType.Speed, 6));
        Add("Thermal Exchange", new HitRaises(m => m.Type == PokemonType.Fire, StatType.Attack, 1, burnProof: true));
        Add("Weak Armor", new WeakArmor());
        Add("Berserk", new HalfwayAnger(shell: false));
        Add("Anger Shell", new HalfwayAnger(shell: true));
        Add("Electromorphosis", new Charged(windOnly: false));
        Add("Wind Power", new Charged(windOnly: true));
        Add("Gooey", new Slows());
        Add("Tangling Hair", new Slows());
        Add("Iron Barbs", new RoughSkin());
        Add("Cotton Down", new CottonDown());
        Add("Sand Spit", new HitBrings(sand: true));
        Add("Seed Sower", new HitBrings(sand: false));
        Add("Spicy Spray", new SpicySpray());
        Add("Cursed Body", new CursedBody());
        Add("Mummy", new Catching(swaps: false));
        Add("Lingering Aroma", new Catching(swaps: false));
        Add("Wandering Spirit", new Catching(swaps: true));
        Add("Perish Body", new PerishBody());
        Add("Pickpocket", new Pickpocket());
        Add("Innards Out", new InnardsOut());
        Add("Toxic Debris", new ToxicDebris());

        // Attacking
        Add("Poison Touch", new Poisoner(chain: false));
        Add("Toxic Chain", new Poisoner(chain: true));
        Add("Magician", new Magician());
        Add("Moxie", new KnockOutRaises(StatType.Attack));
        Add("Chilling Neigh", new KnockOutRaises(StatType.Attack));
        Add("Grim Neigh", new KnockOutRaises(StatType.SpAttack));
        Add("Beast Boost", new KnockOutRaises(null));
        Add("Eelevate", new KnockOutRaises(null, floats: true));

        // Guarding itself
        Add("Contrary", new Contrary());
        Add("Big Pecks", new StatGuard(StatType.Defense));
        Add("Full Metal Body", new StatGuard());
        Add("Mind's Eye", new MindsEye());
        Add("Sweet Veil", new StatusImmunity(StatusCondition.Sleep));
        Add("Pastel Veil", new PastelVeil());
        Add("Shields Down", new NoStatus((b, s) => s != StatusCondition.None && b.Pokemon?.Form is { } form && form.EndsWith("-Meteor")
                                                    || s != StatusCondition.None && b.Pokemon is { Form: null, Species.Name: "Minior" }));

        // Over time, and leaving
        Add("Moody", new Moody());
        Add("Harvest", new Harvest());
        Add("Healer", new Healer());
        Add("Regenerator", new Regenerator());

        // The shapes that change in battle, which the core runs by the ability's name (plan 06 · R19b, R23)
        foreach (var name in new[]
                 {
                     "Zen Mode", "Stance Change", "Schooling", "Disguise", "Ice Face", "Gulp Missile", "Hunger Switch", "Zero to Hero",
                     "Embody Aspect", "Tera Shift", "Tera Shell", "Teraform Zero"
                 })
            Add(name, new FlagEffect());

        // The rest (plan 06 · R27b: AbilityEffectTable.Later2.cs)
        AddLater2();
    }
}
