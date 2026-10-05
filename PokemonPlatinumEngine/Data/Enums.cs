namespace PokemonPlatinumEngine.Data;

public enum PokemonType
{
    Normal,
    Fire,
    Water,
    Grass,
    Electric,
    Ice,
    Fighting,
    Poison,
    Ground,
    Flying,
    Psychic,
    Bug,
    Rock,
    Ghost,
    Dragon,
    Steel,
    Dark,
    Fairy
}

public enum MoveCategory
{
    Physical,
    Special,
    Status
}

public enum StatusCondition
{
    None,
    Poison,
    Toxic,
    Burn,
    Paralyze,
    Sleep,
    Freeze,
    Faint
}

public enum Nature
{
    Hardy,
    Lonely,
    Brave,
    Adamant,
    Naughty,
    Bold,
    Docile,
    Relaxed,
    Impish,
    Lax,
    Timid,
    Hasty,
    Serious,
    Jolly,
    Naive,
    Modest,
    Mild,
    Quiet,
    Bashful,
    Rash,
    Calm,
    Gentle,
    Sassy,
    Careful,
    Quirky
}

public enum StatType
{
    HP,
    Attack,
    Defense,
    SpAttack,
    SpDefense,
    Speed,
    Accuracy,
    Evasion
}

public enum GrowthRate
{
    Fast,
    MediumFast,
    MediumSlow,
    Slow,
    Erratic,
    Fluctuating
}

public enum Gender
{
    Male,
    Female,
    Genderless
}

public enum ItemPocket
{
    Items,
    Medicine,
    PokeBalls,
    TMsAndHMs,
    KeyItems,
    Berries,
    Mail,
    BattleItems
}

public enum Direction
{
    Down,
    Up,
    Left,
    Right
}

/// <summary>Which Pokémon a move can hit. In a single battle every "other" target is the one foe.</summary>
public enum MoveTarget
{
    /// <summary>One adjacent Pokémon the user picks (a foe by default).</summary>
    Selected,
    /// <summary>Both foes (Growl, Rock Slide); damage is cut to 3/4 when it hits more than one.</summary>
    AllFoes,
    /// <summary>Every other Pokémon on the field, the user's partner included (Earthquake, Surf).</summary>
    AllOthers,
    /// <summary>The user itself (Swords Dance, Synthesis).</summary>
    User,
    /// <summary>A random foe (Outrage, Thrash).</summary>
    RandomFoe,
    /// <summary>The user's side of the field (Reflect, Light Screen).</summary>
    UserSide,
    /// <summary>The foes' side of the field (Spikes, Stealth Rock).</summary>
    FoeSide,
    /// <summary>The whole field (Rain Dance, Trick Room).</summary>
    Field,
    /// <summary>The user's partner in a double battle (Helping Hand).</summary>
    Ally,
    /// <summary>The user or its partner (Acupressure).</summary>
    UserOrAlly,
    /// <summary>The user and its partner (Howl from Generation 8, Life Dew).</summary>
    UserAndAllies,
    /// <summary>The user's partners only (Coaching).</summary>
    Allies,
    /// <summary>Every Pokémon on the field, the user included (Perish Song, Flower Shield).</summary>
    AllPokemon
}

/// <summary>Properties of a move that abilities and items care about.</summary>
[System.Flags]
public enum MoveFlags
{
    None = 0,
    /// <summary>Touches the target: triggers Static, Rough Skin and the like.</summary>
    Contact = 1,
    /// <summary>A punching move (Iron Fist).</summary>
    Punch = 2,
    /// <summary>A sound-based move (Soundproof).</summary>
    Sound = 4,
    /// <summary>A biting move (Strong Jaw, from Generation 6).</summary>
    Bite = 8,
    /// <summary>An aura or pulse move (Mega Launcher, from Generation 6).</summary>
    Pulse = 16,
    /// <summary>A ball or bomb move (Bulletproof, from Generation 6).</summary>
    Ballistic = 32,
    /// <summary>A powder or spore move (Grass types and Overcoat are immune from Generation 6).</summary>
    Powder = 64,
    /// <summary>A dance (Dancer, from Generation 7).</summary>
    Dance = 128,
    /// <summary>Protect and Detect block it.</summary>
    Protect = 256,
    /// <summary>Magic Coat bounces it back.</summary>
    Reflectable = 512,
    /// <summary>Snatch steals it.</summary>
    Snatch = 1024,
    /// <summary>Mirror Move can copy it.</summary>
    Mirror = 2048,
    /// <summary>King's Rock and Razor Fang can make it flinch (Generation 4 kept a list; later games use every damaging move).</summary>
    KingsRock = 4096
}

/// <summary>How much of a move the engine runs. The data fields say what it does; <c>effect</c> names anything more.</summary>
public enum MoveEffectSupport
{
    /// <summary>Everything the move does is in its data fields.</summary>
    Full,
    /// <summary>The fields run (damage, a side effect), but the move's own effect is not in the engine yet.</summary>
    Partial,
    /// <summary>The move's effect is not in the engine yet and using it does nothing.</summary>
    None
}

/// <summary>What sets one of a species' forms apart from it (plan 03 · D11).</summary>
public enum FormKind
{
    /// <summary>Its looks, with the species' data or only other types (Arceus's plates): Unown's letters, Burmy's cloaks, Shellos's coasts.</summary>
    Look,
    /// <summary>Data of its own, kept outside battle: Rotom's appliances, Giratina's Origin Forme, Deoxys's and Shaymin's formes.</summary>
    Alternate,
    /// <summary>Another region's form, with its own types, stats, abilities and moves: Alolan, Galarian, Hisuian and Paldean.</summary>
    Regional,
    /// <summary>Taken during a battle and lost after it: Castform's weathers, Cherrim in the sun, Darmanitan's Zen Mode.</summary>
    Battle,
    Mega,
    Primal,
    Gigantamax
}

/// <summary>
/// How a species evolves. <c>Models/Evolution.cs</c> decides when each one happens; docs/mechanics/evolution.md
/// lists them with the games they come from and what this game does where the original needs something it lacks.
/// </summary>
public enum EvolutionMethod
{
    Level,
    Friendship,
    FriendshipDay,
    FriendshipNight,
    LevelDay,
    LevelNight,
    LevelMale,
    LevelFemale,
    /// <summary>Tyrogue's three ways: Attack above, equal to or below Defense.</summary>
    LevelAttackHigher,
    LevelAttackEqual,
    LevelDefenseHigher,
    /// <summary>Wurmple: decided by the Pokémon's personality value.</summary>
    LevelPersonalityLow,
    LevelPersonalityHigh,
    /// <summary>Nincada: becomes Ninjask, and Shedinja appears in a free party slot.</summary>
    LevelNinjask,
    LevelShedinja,
    LevelHoldingItemDay,
    LevelHoldingItemNight,
    LevelHoldingItem,
    LevelKnowsMove,
    LevelKnowsMoveType,
    LevelWithSpeciesInParty,
    LevelWithTypeInParty,
    /// <summary>On a map that has the place named in <c>location</c> (Moss Rock, Ice Rock, Magnetic Field).</summary>
    LevelAtLocation,
    LevelInRain,
    /// <summary>Inkay: the console held upside down. Here it is a plain level (see the rulings).</summary>
    LevelUpsideDown,
    Beauty,
    Affection,
    UseItem,
    UseItemMale,
    UseItemFemale,
    Trade,
    TradeHoldingItem,
    TradeWithSpecies,
    /// <summary>Anything the engine has no rule for; <c>note</c> says what. Nothing in the data uses it now.</summary>
    Other,
    /// <summary>Ursaring's Peat Block under a full moon: an item that only works at night.</summary>
    UseItemNight,
    /// <summary>Level up after walking <c>value</c> steps at the head of the party (Pawmo, Bramblin, Rellor).</summary>
    LevelAfterSteps,
    /// <summary>Level up after using <c>move</c> <c>value</c> times in battle (Primeape's Rage Fist, Stantler's Psyshield Bash).</summary>
    LevelAfterMoveUses,
    /// <summary>Level up after knocking out <c>value</c> Pokémon of <c>species</c> (Bisharp).</summary>
    LevelAfterDefeating,
    /// <summary>Level up with <c>value</c> of <c>item</c> in the bag, which are used up (Gimmighoul's coins, Meltan's candies).</summary>
    LevelWithItemsInBag,
    /// <summary>The player turns a full circle while it holds <c>item</c> (Milcery and its sweets).</summary>
    SpinHoldingItem,
    /// <summary>The level, at dusk: Platinum's evening, 17:00 to 19:59 (Own Tempo Rockruff into Dusk Form Lycanroc).</summary>
    LevelDusk,
    /// <summary>
    /// <c>value</c> critical hits landed in one battle, and the battle is over (Galarian Farfetch'd into Sirfetch'd):
    /// it evolves as the battle ends, without a level.
    /// </summary>
    CriticalHits,
    /// <summary>
    /// Level up where a map has the place named in <c>location</c> (the Stone Arch) after losing <c>value</c> HP to
    /// moves without fainting since (Galarian Yamask into Runerigus).
    /// </summary>
    LevelAfterDamage,
    /// <summary>Level up after losing <c>value</c> HP to its own moves' recoil without fainting since (White-Striped Basculin into Basculegion).</summary>
    LevelAfterRecoil
}
