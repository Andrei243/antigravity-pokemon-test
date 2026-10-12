using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>A place on the field: a side and one of its slots.</summary>
public readonly record struct Place(BattleSide Side, int Slot)
{
    /// <summary>The place's number as the original counts them: the player's first, the foe's first, then the second of each.</summary>
    public int Number => Slot * 2 + (Side == BattleSide.Enemy ? 1 : 0);
}

/// <summary>
/// One thing a battle's log says happened (plan 06 · R2). The core works a whole turn out at once and writes what
/// happened here, in order; whoever shows the battle plays the log back at its own pace. It is the same split the
/// original has between its battle logic and the "controller" it sends messages to, and what lets a battle run
/// with no screen at all (tests, a server, a replay).
/// <para>
/// Pokémon in events are the core's own (<see cref="BattleCore"/> works on copies); a place is where on the field.
/// </para>
/// </summary>
public abstract record BattleEvent;

/// <summary>
/// A line of text. Whatever is attached happens as the line appears (<see cref="Shows"/>) or at the moment a move
/// lands (<see cref="OnImpact"/>); an event that stands in the log by itself happens once the line before it has
/// been read.
/// </summary>
public sealed record Said(string Text) : BattleEvent
{
    public List<BattleEvent> Shows { get; } = new();
    public List<BattleEvent> OnImpact { get; } = new();

    public Said With(BattleEvent happening)
    {
        Shows.Add(happening);
        return this;
    }

    public Said AtImpact(BattleEvent happening)
    {
        OnImpact.Add(happening);
        return this;
    }
}

/// <summary>The trainers standing across the field as the battle opens.</summary>
public sealed record TrainersStand(string First, string? Second) : BattleEvent;

/// <summary>A species has been met (it goes in the Pokédex as seen).</summary>
public sealed record Seen(PokemonSpecies Species) : BattleEvent;

/// <summary>A Pokémon takes a place: thrown out of a ball, or already there when a wild battle opens.</summary>
public sealed record Entered(Place Place, Pokemon Pokemon, bool FromBall) : BattleEvent;

/// <summary>A Pokémon is called back (the animation); <see cref="Left"/> is when it is gone.</summary>
public sealed record Recalled(Place Place) : BattleEvent;
/// <param name="Ran">A wild Pokémon that ran away (a roamer, one of the Great Marsh's), not one called back.</param>
public sealed record Left(Place Place, bool Ran = false) : BattleEvent;

/// <summary>A Pokémon starts its move.</summary>
public sealed record Lunged(Place Place, MoveCategory Category) : BattleEvent;

/// <summary>A move's effect travels from its user to one target, and how it came out there.</summary>
public sealed record MoveShown(string Move, PokemonType Type, MoveCategory Category, Place From, Place To,
    bool Missed, bool Blocked, bool Critical, bool SuperEffective) : BattleEvent;

/// <summary>A move's damage lands: the HP its target is left with.</summary>
public sealed record Struck(Place Place, int Hp, bool Hard) : BattleEvent;

/// <summary>The sound of the hits of one move landing: super effective on any target, or not very effective on all.</summary>
public sealed record HitSounded(bool SuperEffective, bool NotVeryEffective = false) : BattleEvent;

/// <summary>A Pokémon is confused: it has just become so, or its confusion holds as it tries to move.</summary>
public sealed record Confused(Place Place) : BattleEvent;

/// <summary>HP restored, or lost to something that isn't a move's hit (poison, recoil): the HP it is left with.</summary>
public sealed record HpChanged(Place Place, int Hp, bool Healed) : BattleEvent;

public sealed record StageChanged(Place Place, StatType Stat, int Stage, bool Rose) : BattleEvent;

/// <summary>A status condition given, or cured (<see cref="StatusCondition.None"/>).</summary>
public sealed record StatusChanged(Place Place, StatusCondition Status) : BattleEvent;

public sealed record Fainted(Place Place) : BattleEvent;

/// <summary>The player has got away from the battle.</summary>
public sealed record GotAway : BattleEvent;

/// <summary>A Pokémon goes where it can't be seen (into the air, under the ground), and comes back.</summary>
public sealed record Vanished(Place Place) : BattleEvent;
public sealed record Reappeared(Place Place) : BattleEvent;

/// <summary>A Pokémon has taken another's shape (Transform): the screen shows it as the rules now have it.</summary>
public sealed record Reshaped(Place Place) : BattleEvent;

/// <summary>A Substitute is put up in a place, or is gone from it.</summary>
public sealed record SubstituteChanged(Place Place, bool Up) : BattleEvent;

/// <summary>
/// A line names the ability of the Pokémon in a place (plan 12 · Q11): the original's message tag for a battler's
/// ability (<c>BattleMessage_AbilityTag</c>), which tells the trainer AI of it (<c>BattleAI_SetAbility</c>). Whoever
/// reads the log learns it from this, never from the line's words: the trainer AI's memory, and the screen's
/// information panel, which says nothing the player hasn't been told.
/// </summary>
public sealed record AbilityShown(Place Place, string Ability) : BattleEvent;

/// <summary>A line names the held item of the Pokémon in a place (<c>BattleMessage_ItemTag</c>, <c>BattleAI_SetHeldItem</c>): a berry eaten, Leftovers, a Focus Sash, a Frisk.</summary>
public sealed record ItemShown(Place Place, string Item) : BattleEvent;

/// <summary>The weather over the battle is now this (none: it cleared).</summary>
public sealed record WeatherChanged(BattleWeather Weather) : BattleEvent;

/// <summary>
/// The weather goes on at the end of a turn (plan 25 · Y1): on the turn's weather line ("Rain continues to fall."), as the
/// original plays the weather's animation on each turn it lasts. The rules change nothing by it; the screen plays the weather's cue again.
/// </summary>
public sealed record WeatherContinues(BattleWeather Weather) : BattleEvent;

/// <summary>
/// A side of the field has more or less of something than it had (plan 25 · Y2): a screen, a veil, Tailwind or Lucky
/// Chant up (1) or gone (0), Spikes at 0 to 3 layers, Toxic Spikes at 0 to 2, Stealth Rock there or not. It rides on
/// the line that tells of it: the move that set it, the turn's end that wears it off, Brick Break, Defog, Rapid Spin,
/// a Poison type taking the Toxic Spikes up. The rules change nothing by it; the screen draws what lasts from it.
/// </summary>
public sealed record SideConditionChanged(BattleSide Side, SideCondition Condition, int Layers) : BattleEvent;

/// <summary>Trick Room or Gravity begins or ends, on its line (a battle that opens under Trick Room says so too).</summary>
public sealed record FieldConditionChanged(FieldCondition Condition, bool Up) : BattleEvent;

/// <summary>
/// Something begins to wait for a place, or waits no longer: a Wish, or a Future Sight or Doom Desire on its way.
/// It rides on the line that tells of it; where the rules let one go with no line (its turn comes and nobody stands
/// there), it stands in the log by itself.
/// </summary>
public sealed record PlaceConditionChanged(Place Place, PlaceCondition Condition, bool Up) : BattleEvent;
public sealed record ExpGained(Pokemon Pokemon, int Amount) : BattleEvent;

/// <summary>
/// A Pokémon's EXP took it to a level (through every level it reached at once). <paramref name="Before"/> and
/// <paramref name="After"/> are its six stats as they stood before the EXP and as they stand at the new level (plan
/// 08 · P5; the original's level-up summary takes its two pages from the same two readings,
/// <c>SEQ_GET_EXP_LEVEL_UP_SUMMARY_PRINT_DIFF</c> and <c>_PRINT_TRUE</c>). Whoever shows the level takes its numbers
/// from here: the rules' copy has grown already, and the Pokémon the screen holds is behind it.
/// </summary>
public sealed record LevelRose(Pokemon Pokemon, int Level, StatSpread Before, StatSpread After) : BattleEvent;

/// <summary>
/// A Pokémon that knows four moves reached a level with a move to learn (plan 06 · R10): the screen asks the
/// player which move, if any, to forget for it (the original's <c>SEQ_GET_EXP_WANTS_TO_LEARN_MOVE</c>). The rules
/// have changed nothing; whatever is chosen is given to both the screen's Pokémon and the rules' copy.
/// </summary>
public sealed record MoveWanted(Pokemon Pokemon, string Move) : BattleEvent;

/// <summary>A ball is thrown at the foe in a slot and shakes this many times (4: it holds).</summary>
public sealed record BallThrown(string Ball, int Slot, int Shakes) : BattleEvent;

/// <summary>The wild Pokémon is the player's: in the party, or sent to the PC.</summary>
public sealed record Caught(Pokemon Pokemon, string Ball, bool ToBox) : BattleEvent;

/// <summary>The moments a trainer of the other side speaks at in battle (plan 11 · C12).</summary>
public enum TrainerCue
{
    /// <summary>The last of their Pokémon able to fight comes out (never the opening's send-out).</summary>
    LastPokemon,

    /// <summary>The first of their Pokémon falls, with others still able to fight.</summary>
    FirstDown,

    /// <summary>Their last Pokémon is in the red: a fifth of its HP or under (<see cref="BattleCore.RedShare"/>).</summary>
    LowHp
}

/// <summary>
/// A trainer of the other side reacts to a moment of the battle (plan 11 · C12): the trainer of the place named.
/// The rules write it whatever the options say, once per trainer and moment, with no roll and never after the end;
/// what the trainer says, if anything, is the face's own (<see cref="Data.TrainerLines"/>).
/// </summary>
public sealed record TrainerReacts(Place Place, TrainerCue Cue) : BattleEvent;

/// <summary>The other side has nobody left (the victory music starts before the last lines are read).</summary>
public sealed record Won : BattleEvent;

public sealed record Ended(BattleResult Result) : BattleEvent;

/// <summary>
/// The turn's moments (plan 25 · Y3): the original's battle animations that are not moves
/// (<c>generated/battle_sub_animations.txt</c>), but those drawn by other events (a stat's boost and drop, HP
/// restored, a condition given, the weathers, the doll's swaps) and those of plan 09 (shiny; a condition that lasts
/// on the model). Each is named after its original, <c>BATTLE_ANIMATION_…</c>, and written where the original plays
/// it (<c>BtlCmd_PlayBattleAnimation</c> in a subscript of <c>res/battle/scripts/subscripts</c>, named beside each).
/// </summary>
public enum BattleMoment
{
    /// <summary><c>subscript_sleeping</c>: asleep as it would have moved.</summary>
    Asleep,

    /// <summary><c>subscript_poison_damage</c>: poison biting at the turn's end.</summary>
    Poisoned,

    /// <summary><c>subscript_burn_damage</c>.</summary>
    Burned,

    /// <summary><c>subscript_frozen</c>: frozen solid as it would have moved.</summary>
    Frozen,

    /// <summary><c>subscript_fully_paralyzed</c>.</summary>
    Paralyzed,

    /// <summary><c>subscript_confused</c>, <c>subscript_hurt_self_in_confusion</c>: confused as it tries to move.</summary>
    Confused,

    /// <summary><c>subscript_infatuated</c>: in love as it tries to move.</summary>
    Infatuated,

    /// <summary><c>Task_GetExp</c>, <c>SEQ_GET_EXP_CHECK_LEVEL_UP</c>: a Pokémon on the field reaches a level.</summary>
    LevelUp,

    /// <summary>An item from the bag used on a Pokémon on the field (<c>BtlCmd_UseBagItem</c>).</summary>
    BagItem,

    /// <summary><c>subscript_escape_item</c>: a Poké Doll or a Fluffy Tail gets the player away.</summary>
    EscapeItem,

    /// <summary><c>subscript_safari_throw_bait</c>: the Great Marsh's Pokémon as the bait lands.</summary>
    Happy,

    /// <summary><c>subscript_safari_throw_bait</c>: and as it eats.</summary>
    Eating,

    /// <summary><c>subscript_safari_throw_rock</c>: the Great Marsh's Pokémon as the mud lands.</summary>
    Angry,

    /// <summary><c>subscript_curse_damage</c>.</summary>
    Curse,

    /// <summary><c>subscript_nightmare_effect</c>.</summary>
    Nightmare,

    /// <summary><c>subscript_leech_seed_effect</c>: between the seeded Pokémon and the place the seed was sown from.</summary>
    LeechSeed,

    /// <summary><c>subscript_ingrain_heal</c>.</summary>
    Ingrain,

    /// <summary><c>subscript_bind_effect</c>, by the move that binds: Bind, and any move it has no animation of its own for.</summary>
    Bind,
    Wrap,
    FireSpin,
    MagmaStorm,
    Clamp,
    Whirlpool,
    SandTomb,

    /// <summary>
    /// The original's <c>BATTLE_ANIMATION_HELD_ITEM</c>: a held item goes off (Leftovers, a berry eaten, a Focus Sash,
    /// a Quick Claw…). A line that only names an item (Frisk, Knock Off, Trick) carries its <see cref="ItemShown"/>
    /// and no moment, which is why this is one: plan 12 · Q11's event is wider than the animation.
    /// </summary>
    HeldItem
}

/// <summary>
/// A moment of the turn plays on the Pokémon in a place (plan 25 · Y3), on the line that tells of it. Where the line
/// also changes that Pokémon's HP, the moment comes first and the bar then drains or fills with no flinch and no
/// sound of a hit, as the original skips the sprite's blink (<c>SYSCTL_SKIP_SPRITE_BLINK</c>). The rules change
/// nothing by it and never ask whether it can be seen: the screen decides that
/// (<c>BattleSystem_ShouldShowStatusEffect</c>).
/// </summary>
/// <param name="From">
/// The other end of a moment between two places: for Leech Seed, the place the seed was sown from, which is where
/// the sapped health goes. Null for a moment on one Pokémon.
/// </param>
public sealed record MomentShown(Place Place, BattleMoment Moment, Place? From = null) : BattleEvent;

/// <summary>
/// An attack sent ahead lands (Future Sight, Doom Desire; plan 25 · Y3): its move's own effect in its landing half,
/// from the place it was sent from to the place it was aimed at, on the line that says the attack was taken, once
/// its accuracy has been rolled (the original's <c>PlayMoveAnimationOnMons</c> in
/// <c>subscript_future_sight_damage</c>, which plays nothing when the attack fails).
/// </summary>
public sealed record AheadLanded(string Move, PokemonType Type, MoveCategory Category, Place From, Place To) : BattleEvent;

/// <summary>
/// The two passing conditions a Pokémon shows for as long as it has them (plan 09 · L2), as they stand from this
/// line on: whether the Pokémon in a place is confused, and whether it is in love. It rides on every line where
/// either begins or ends: the move, the ability or the berry that confuses, Attract and Cute Charm, a Destiny Knot,
/// the herb, the berries and the bag's items that cure them, a confusion running out, and the send-out of a Pokémon
/// a confusion was passed to by Baton Pass. Where the rules end a love with no line (the one loved is called back),
/// it stands in the log by itself. The rules change nothing by it; the screen draws what lasts from it, and
/// <see cref="Confused"/> and the moments go on telling of what is played as each happens.
/// </summary>
public sealed record VolatileChanged(Place Place, bool Confused, bool InLove) : BattleEvent;

/// <summary>
/// A Pokémon takes its place with a status condition it already had (plan 09 · L2): asleep on the bench, poisoned
/// before the battle began. It rides on the line that sends it out (or stands beside a wild Pokémon's
/// <see cref="Entered"/>), so whoever shows the battle knows what the newcomer brings without asking the rules,
/// which are a turn ahead. Nothing is given by it and nothing is played for it: <see cref="StatusChanged"/> is a
/// condition given or cured on the field.
/// </summary>
public sealed record CameInWith(Place Place, StatusCondition Status) : BattleEvent;
