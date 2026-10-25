using System.Collections.Generic;

namespace PokemonPlatinumEngine.Data;

/// <summary>Whose rules an adventure is played by. Chosen when it begins, kept in its save and never changed after.</summary>
public enum RulesPreset
{
    /// <summary>Platinum's own rules: the default, and what the story is balanced for.</summary>
    Platinum,
    /// <summary>The newest games' rules, wherever they changed something Platinum has.</summary>
    Modern
}

/// <summary>
/// Every number and rule that differs between the generations (plan 06, decision 1). Code that runs such a rule
/// asks the rules for it instead of knowing the number itself, so the two presets can't drift apart in a dozen
/// places. <c>docs/mechanics/rulings.md</c> lists what each preset says, including the differences that wait for
/// the session that writes their rule: add those here when their code is written, never as a constant beside it.
/// </summary>
public sealed class Ruleset
{
    public RulesPreset Preset { get; }

    /// <summary>The name the game shows for the preset.</summary>
    public string Name => Preset == RulesPreset.Modern ? "Modern rules" : "Platinum rules";

    /// <summary>A critical hit is one in this many, by critical-hit stage (0 to 4).</summary>
    public IReadOnlyList<int> CriticalOdds { get; private init; } = new[] { 16, 8, 4, 3, 2 };

    /// <summary>What a critical hit multiplies the damage by. Sniper raises it by half again.</summary>
    public float CriticalMultiplier { get; private init; } = 2f;

    /// <summary>A burn takes one part in this many of the Pokémon's HP at the end of each turn.</summary>
    public int BurnDamageDivisor { get; private init; } = 8;

    /// <summary>Paralysis divides Speed by this.</summary>
    public int ParalysisSpeedDivisor { get; private init; } = 4;

    /// <summary>What paralysis multiplies Speed by.</summary>
    public float ParalysisSpeed => 1f / ParalysisSpeedDivisor;

    /// <summary>Electric types can't be paralysed (from Generation 6).</summary>
    public bool ElectricTypesCantBeParalyzed { get; private init; }

    /// <summary>How many lengths a sleep can have, each as likely as the others.</summary>
    public int SleepLengths { get; private init; } = 4;

    /// <summary>A confused Pokémon hurts itself one time in this many.</summary>
    public int ConfusionSelfHitOdds { get; private init; } = 2;

    /// <summary>Steel resists Ghost and Dark, as it did until Generation 6.</summary>
    public bool SteelResistsGhostAndDark { get; private init; } = true;

    /// <summary>
    /// Moves have the power, accuracy and PP of the newest games (Tackle is 40 and never weaker for missing)
    /// instead of Platinum's (35, 95% accurate): <see cref="Models.MoveData.Modern"/>.
    /// </summary>
    public bool ModernMoveValues { get; private init; }

    /// <summary>
    /// Species and their forms have the types and base stats of the newest games (Clefairy is a Fairy type, Pikachu's
    /// Defense is 40) instead of Platinum's: <see cref="Models.PokemonSpecies.Modern"/>.
    /// </summary>
    public bool ModernSpeciesValues { get; private init; }

    // ---- Conditions and the field (plan 06 · R3). A count is of turn ends still to come unless it says otherwise.

    /// <summary>
    /// A sleep's counter starts at this plus one of <see cref="SleepLengths"/>. It drops by one each time the
    /// sleeper's turn comes, and the turn it reaches nothing the Pokémon wakes and moves.
    /// </summary>
    public int SleepCounterBase => 2;

    /// <summary>Weather brought by an ability (Drizzle, Sand Stream…) lasts this many turns; 0 is for as long as the battle.</summary>
    public int AbilityWeatherTurns { get; private init; }

    /// <summary>How long a Taunt lasts, from the shortest to the longest (each as likely).</summary>
    public (int Min, int Max) TauntTurns { get; private init; } = (3, 5);

    /// <summary>Encore's and Disable's counters: each lasts one turn end more than it says.</summary>
    public (int Min, int Max) EncoreTurns { get; private init; } = (3, 7);
    public (int Min, int Max) DisableTurns { get; private init; } = (3, 6);

    public int TailwindTurns { get; private init; } = 3;
    public (int Min, int Max) UproarTurns { get; private init; } = (3, 6);

    /// <summary>A binding move (Wrap, Fire Spin) holds for this many turn ends, the last of them the one that frees.</summary>
    public (int Min, int Max) BindTurns { get; private init; } = (3, 6);

    /// <summary>The same count when the binder holds a Grip Claw.</summary>
    public int GripClawBindTurns { get; private init; } = 6;

    /// <summary>Each turn end it takes one part in this many of the bound Pokémon's HP.</summary>
    public int BindDamageDivisor { get; private init; } = 16;

    /// <summary>
    /// Protect, Detect and Endure work when a number drawn from 65,536 is no greater than this, by how many times
    /// in a row they have worked already (the last entry for every time after).
    /// </summary>
    public IReadOnlyList<int> ProtectRates { get; private init; } = new[] { 0xFFFF, 0x7FFF, 0x3FFF, 0x1FFF };

    /// <summary>Stages of evasion Minimize gives.</summary>
    public int MinimizeStages { get; private init; } = 1;

    /// <summary>Powder and spore moves do nothing to Grass types (from Generation 6).</summary>
    public bool GrassTypesIgnorePowder { get; private init; }

    /// <summary>Ghost types can always switch out and run (from Generation 6).</summary>
    public bool GhostTypesCantBeTrapped { get; private init; }

    /// <summary>Toxic never misses when a Poison type uses it (from Generation 6).</summary>
    public bool PoisonTypesNeverMissToxic { get; private init; }

    // ---- The move families (plan 06 · R4)

    /// <summary>A move of two to five hits draws its count out of a hundred (35, 35, 15 and 15 for 2 to 5) instead of Platinum's two draws of four.</summary>
    public bool MultiHitByHundredths { get; private init; }

    /// <summary>Explosion and Self-Destruct halve the target's Defense (until Generation 5).</summary>
    public bool ExplosionHalvesDefense { get; private init; } = true;

    /// <summary>Hidden Power's power: 0 for what the IVs make it (30 to 70), or the one number the newest games give it.</summary>
    public int HiddenPowerPower { get; private init; }

    // ---- The unique effects (plan 06 · R5)

    /// <summary>
    /// Chatter's chance of confusing, in hundredths. Platinum's went by the loudness of the cry recorded through
    /// the microphone (nothing, 10 or 30): this game has no microphone and takes the loudest. The newest games
    /// confuse every time.
    /// </summary>
    public int ChatterConfusionChance { get; private init; } = 30;

    /// <summary>Knock Off is half as strong again against a target holding an item it can knock off (Generation 6 on).</summary>
    public bool KnockOffStrongerOnItem { get; private init; }

    /// <summary>
    /// The opponents' Pokémon can take or swap the player's items with Thief, Covet, Trick and Switcheroo. In
    /// Platinum they can't outside the Battle Frontier and link battles (<c>BtlCmd_TryStealItem</c>); from
    /// Generation 5 on they can.
    /// </summary>
    public bool FoesCanTakeItems { get; private init; }

    // ---- Abilities (plan 06 · R7)

    /// <summary>
    /// Simple doubles each change to the holder's stat stages (Generation 5 on). In Platinum the stages change as
    /// anyone's do and count double wherever they are read: the damage formula, the accuracy check, the turn order.
    /// </summary>
    public bool SimpleDoublesChanges { get; private init; }

    // ---- After the battle (plan 06 · R10)

    /// <summary>
    /// EXP by the newest games' formula (Generation 7 on): the foe's base EXP and level over 5, scaled by the two
    /// levels (<c>((2L + 10) / (L + Lp + 10))^2.5</c>), plus one, and given to the whole party, the Pokémon that
    /// didn't fight getting half; no bonus for a trainer's Pokémon, a fifth more for one past the level it would
    /// evolve at. In Platinum the foe's base EXP times its level over 7 is shared among those who fought it, and
    /// an Exp. Share's holder takes half.
    /// </summary>
    public bool ScaledExp { get; private init; }

    /// <summary>The most effort one stat can hold: 255 in Platinum, 252 from Generation 6.</summary>
    public int EvStatCap { get; private init; } = 255;

    /// <summary>How far a vitamin raises a stat's effort: 100 in Platinum, the stat's cap from Generation 8.</summary>
    public int VitaminLimit { get; private init; } = 100;

    /// <summary>Whether a Pokémon at the level cap still gains effort from a battle (Generation 5 on); in Platinum it gains nothing.</summary>
    public bool EvsAtTheCap { get; private init; }

    /// <summary>
    /// The highest level any rules let a Pokémon reach (plan 18 · W4, the user's ruling of 2026-10-21): 260, the study's
    /// fair climb under the modern EXP, so that a team carried from Sinnoh to Paldea stays a step ahead of every
    /// region's Champion. Whatever level a Pokémon is made or loaded at is held between 1 and this.
    /// </summary>
    public const int HighestLevel = 260;

    /// <summary>
    /// The level no Pokémon grows past under these rules (plan 18 · W4): 100 in Platinum, <see cref="HighestLevel"/> by
    /// the modern rules, under which each later region's levels climb past 100 (the region file's <c>levels</c>,
    /// <see cref="LevelMapping"/>). A battle's EXP, the Rare Candy, the EXP Candies and the Day Care stop there.
    /// </summary>
    public int LevelCap { get; private init; } = 100;

    /// <summary>One wild or new Pokémon in how many is shiny: 8,192 in Platinum, 4,096 from Generation 6.</summary>
    public int ShinyOdds { get; private init; } = 8192;

    /// <summary>
    /// Whether poison hurts a Pokémon outside battle, a hit point every fourth step (plan 06 · R13,
    /// <c>Field_UpdatePoison</c>): in Platinum, and no longer from Generation 5.
    /// </summary>
    public bool PoisonInTheField { get; private init; } = true;

    /// <summary>
    /// Whether an Everstone always hands its holder's nature down, whichever parent holds it (plan 06 · R15; from Black 2
    /// and White 2). In Platinum only the mother's or Ditto's does, one time in two (<c>Daycare_GetParentToInheritNature</c>).
    /// </summary>
    public bool EverstoneAlwaysPassesNature { get; private init; }

    /// <summary>
    /// Whether an Egg's IVs are handed down the later way (plan 06 · R15): three different stats, five with a Destiny
    /// Knot (Generation 6), a Power item's stat from its holder (from HeartGold and SoulSilver). Platinum's three picks
    /// can fall on the same stat, and no item changes them (<c>Egg_InheritIVs</c>).
    /// </summary>
    public bool ModernInheritance { get; private init; }

    /// <summary>
    /// How many more times an Egg's personality is drawn for a shiny one when its parents come from games of different
    /// languages (the Masuda method, plan 06 · R15): four in Platinum (<c>Egg_SetInitialData</c>), five from Generation 5.
    /// </summary>
    public int MasudaRerolls { get; private init; } = 4;

    /// <summary>
    /// Hail is snow (Generation 9, plan 06 · R19b): whatever would whip up a hailstorm (the move Hail, Snow Warning,
    /// a snowy place's sky) brings snow instead, which hurts nobody and raises an Ice type's Defense by half
    /// (<see cref="Battle.Sim.BattleWeather.Snow"/>); everything else hail does, snow does.
    /// </summary>
    public bool SnowInPlaceOfHail { get; private init; }

    /// <summary>
    /// The reach the later games gave two of Platinum's moves (plan 06 · R25's leftovers; Pokémon Showdown's
    /// <c>defog</c> and <c>feint</c>): Defog clears the hazards from its user's own side too and lifts any terrain
    /// (Generations 6 and 8), and Feint strikes whether or not its target protects itself, breaking the side's guards
    /// as it breaks Protect (Generation 5). In Platinum Defog clears the target's side alone and Feint fails on a
    /// target that isn't protecting.
    /// </summary>
    public bool LaterMoveReach { get; private init; }

    /// <summary>
    /// The damage formula of Generation 5 on (plan 06 · R19c; Pokémon Showdown's <c>getDamage</c> and <c>modifyDamage</c>):
    /// the bonuses to the power, the stats and the damage chained in 4096ths and rounded half down, the weather and a
    /// spread hit after the + 2, a burn after the roll, the same-type bonus and the type chart, and the screens, a Life
    /// Orb, an Expert Belt, Filter and Tinted Lens as final modifiers (<see cref="Battle.DamageCalculator"/>). In
    /// Platinum every bonus is a step of its own rounded down, and the burn, the screens and the weather come inside the
    /// base damage, before the + 2 (<c>BattleSystem_CalcMoveDamage</c>).
    /// </summary>
    public bool ModernDamageFormula { get; private init; }

    /// <summary>
    /// Mega Evolution and Primal Reversion (plan 06 · R20; Pokémon Showdown's <c>canMegaEvo</c>, <c>runMegaEvo</c> and
    /// the orbs' <c>onSwitchIn</c>): a trainer with a Key Stone may Mega Evolve one Pokémon holding its own Mega Stone
    /// (or a Rayquaza that knows Dragon Ascent) once a battle, before the turn's moves; Kyogre and Groudon holding
    /// their orbs revert as they come in; a Mega Stone or an orb never leaves its own species' hands. Platinum has none
    /// of it, and where it is handed over is each region's (Kalos's for the Key Stone).
    /// </summary>
    public bool MegaEvolution { get; private init; }

    /// <summary>
    /// Z-Moves and Ultra Burst (plan 06 · R21; Pokémon Showdown's <c>canZMove</c>, <c>getActiveZMove</c>,
    /// <c>runZPower</c> and <c>canUltraBurst</c>): a trainer with a Z-Ring may turn one move a battle into its Z-Move
    /// through the Z-Crystal its Pokémon holds, and a Necrozma fused with Solgaleo or Lunala holding Ultranecrozium Z
    /// may Ultra Burst once; a Z-Crystal never leaves the hands it is in. Platinum has none of it, and the Z-Ring is
    /// Alola's to hand over.
    /// </summary>
    public bool ZMoves { get; private init; }

    /// <summary>
    /// Dynamax and Gigantamax (plan 06 · R22; Pokémon Showdown's <c>dynamax</c> condition, <c>getMaxMove</c>,
    /// <c>getActiveMaxMove</c> and the Max Moves' and G-Max Moves' own effects): a trainer with a Dynamax Band may
    /// Dynamax one Pokémon a battle for three turns, its HP raised by its Dynamax Level and its moves become Max Moves
    /// (G-Max Moves for one with the Gigantamax Factor). Platinum has none of it, and the band is Galar's to hand over.
    /// </summary>
    public bool Dynamax { get; private init; }

    /// <summary>
    /// Terastallization (plan 06 · R23; Pokémon Showdown's <c>terastallize</c>, <c>canTerastallize</c>, the same-type
    /// bonus of <c>modifyDamage</c> and the power of 60 in <c>getDamage</c>): a trainer with a Tera Orb may Terastallize
    /// one Pokémon a battle, which takes its Tera Type for defence until the battle ends. Platinum has none of it, and
    /// the orb is Paldea's to hand over.
    /// </summary>
    public bool Terastallization { get; private init; }

    /// <summary>
    /// The balls of the later games (plan 06 · R28a; <c>docs/mechanics/rulings.md</c>, "The later games' held items,
    /// medicine and balls"): each ball's bonus as the newest games give it (a Quick Ball's five times, a Net Ball's three
    /// and a half, a Timer Ball's 1229/4096 a turn), and the balls Platinum doesn't have (Johto's, the Dream, Beast and
    /// Hisui's). Platinum's own balls keep Platinum's bonuses without it (<see cref="Battle.Sim.Formulas.BallTenths"/>).
    /// </summary>
    public bool LaterBalls { get; private init; }

    /// <summary>
    /// Hordes (plan 14 · B3; X and Y's): a wild battle asked for as a <see cref="Battle.BattleFormat.Horde"/> is fought
    /// one against five. Platinum has none, and under its rules such a battle falls back to a single against the first
    /// (<see cref="Battle.BattleFormats.FallBack"/>), so <c>battles-platinum.txt</c> never moves for it.
    /// </summary>
    public bool Hordes { get; private init; }

    /// <summary>
    /// Calls for help (plan 14 · B5; Sun and Moon's SOS battles): a wild battle asked for as a <see cref="Battle.BattleFormat.Sos"/>
    /// keeps a second place on the wild side for an ally its Pokémon may call. Platinum has none, and under its rules such
    /// a battle is the single it began as (<see cref="Battle.BattleFormats.FallBack"/>), so <c>battles-platinum.txt</c> never moves for it.
    /// </summary>
    public bool SosCalls { get; private init; }

    /// <summary>
    /// Triple battles (plan 14 · B6; Black and White's): a battle asked for as a <see cref="Battle.BattleFormat.Triple"/> is
    /// fought three against three, reach by position. Platinum has none, and under its rules such a battle is the double
    /// or single the sides can field (<see cref="Battle.BattleFormats.FallBack"/>), so <c>battles-platinum.txt</c> never moves for it.
    /// </summary>
    public bool Triples { get; private init; }

    /// <summary>
    /// Rotation battles (plan 14 · B7; Black and White's): a battle asked for as a <see cref="Battle.BattleFormat.Rotation"/>
    /// is fought with three on each side's turntable, the front one fighting. Platinum has none, and under its rules such a
    /// battle is a single (<see cref="Battle.BattleFormats.FallBack"/>), so <c>battles-platinum.txt</c> never moves for it.
    /// </summary>
    public bool Rotations { get; private init; }

    /// <summary>The weather these rules make of one asked for: snow for hail where <see cref="SnowInPlaceOfHail"/> holds.</summary>
    public Battle.Sim.BattleWeather Sky(Battle.Sim.BattleWeather weather) =>
        SnowInPlaceOfHail && weather == Battle.Sim.BattleWeather.Hail ? Battle.Sim.BattleWeather.Snow : weather;

    private Ruleset(RulesPreset preset) => Preset = preset;

    public static Ruleset Platinum { get; } = new(RulesPreset.Platinum);

    public static Ruleset Modern { get; } = new(RulesPreset.Modern)
    {
        CriticalOdds = new[] { 24, 8, 2, 1, 1 },
        CriticalMultiplier = 1.5f,
        BurnDamageDivisor = 16,
        ParalysisSpeedDivisor = 2,
        ElectricTypesCantBeParalyzed = true,
        SleepLengths = 3,
        ConfusionSelfHitOdds = 3,
        SteelResistsGhostAndDark = false,
        ModernMoveValues = true,
        ModernSpeciesValues = true,
        AbilityWeatherTurns = 5,
        TauntTurns = (3, 3),
        EncoreTurns = (2, 2),
        DisableTurns = (4, 4),
        TailwindTurns = 4,
        UproarTurns = (3, 3),
        BindTurns = (5, 6),
        GripClawBindTurns = 8,
        BindDamageDivisor = 8,
        // A third as likely each time, down to 1 in 729
        ProtectRates = new[] { 0xFFFF, 21845, 7281, 2427, 809, 269, 89 },
        MinimizeStages = 2,
        GrassTypesIgnorePowder = true,
        GhostTypesCantBeTrapped = true,
        PoisonTypesNeverMissToxic = true,
        MultiHitByHundredths = true,
        ExplosionHalvesDefense = false,
        HiddenPowerPower = 60,
        ChatterConfusionChance = 100,
        KnockOffStrongerOnItem = true,
        FoesCanTakeItems = true,
        SimpleDoublesChanges = true,
        ScaledExp = true,
        EvStatCap = 252,
        VitaminLimit = 252,
        EvsAtTheCap = true,
        LevelCap = HighestLevel,
        ShinyOdds = 4096,
        PoisonInTheField = false,
        EverstoneAlwaysPassesNature = true,
        ModernInheritance = true,
        MasudaRerolls = 5,
        SnowInPlaceOfHail = true,
        LaterMoveReach = true,
        ModernDamageFormula = true,
        MegaEvolution = true,
        ZMoves = true,
        Dynamax = true,
        Terastallization = true,
        LaterBalls = true,
        Hordes = true,
        SosCalls = true,
        Triples = true,
        Rotations = true
    };

    public static Ruleset Of(RulesPreset preset) => preset == RulesPreset.Modern ? Modern : Platinum;

    /// <summary>
    /// The rules of the game in progress: what a battle uses when it isn't handed rules of its own, and what the
    /// moves' and the species' values follow. Static, like <see cref="Core.PlayerIdentity"/>, and for the same reason tests must
    /// not change it (they run side by side): a test of the modern rules hands them to its battle
    /// (<see cref="Battle.BattleSetup.Rules"/>) and asks a move for its values under them
    /// (<see cref="Models.MoveData.Under"/>).
    /// </summary>
    public static Ruleset Current { get; private set; } = Platinum;

    /// <summary>
    /// Makes a preset the rules of the game in progress. The engine calls it once, as a new game begins or a save
    /// is loaded, before any of that game's Pokémon are made.
    /// </summary>
    public static void Use(RulesPreset preset)
    {
        Current = Of(preset);
        MoveDatabase.UseRules(Current);
        PokemonDatabase.UseRules(Current);
    }
}
