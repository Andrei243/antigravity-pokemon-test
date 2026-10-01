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
    Berries
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
    RandomFoe
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
    Sound = 4
}
