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
    KeyItems
}

public enum Direction
{
    Down,
    Up,
    Left,
    Right
}
