using System;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Battle;

/// <summary>
/// One mark that lasts about a Pokémon (plan 09 · L2): whether it stands, how long it has been there, and how far
/// it has gone if it is going. What it looks like at these ages is <see cref="Graphics.MoveFx.Condition"/>, a pure
/// function of them.
/// </summary>
public sealed class LastingMark
{
    /// <summary>It stands: the Pokémon has what it shows.</summary>
    public bool Up { get; internal set; }

    /// <summary>
    /// Seconds since it began to show, which is what its loop is timed by. It goes on counting while the mark
    /// fades, so nothing jumps as it goes.
    /// </summary>
    public float Age { get; internal set; }

    /// <summary>Seconds since it began to go (-1: it isn't going).</summary>
    public float FadingAge { get; internal set; } = -1f;

    /// <summary>From the looks' own count, never the battle's chance: varies where its pieces are.</summary>
    public int Seed { get; internal set; }

    // How far it had come in when it began to go: what fades is what was there
    private float reached = 1f;

    /// <summary>Something of it is on screen: standing, or still fading.</summary>
    public bool Visible => Up || FadingAge >= 0f;

    /// <summary>
    /// How much of it is there, 0..1: in over <see cref="BattleAnimator.ConditionArriveTime"/> from the line that
    /// tells of it, away over <see cref="BattleAnimator.ConditionFadeTime"/> from the line that ends it.
    /// </summary>
    public float Strength => Up ? Math.Clamp(Age / BattleAnimator.ConditionArriveTime, 0f, 1f)
        : FadingAge >= 0f ? reached * Math.Clamp(1f - FadingAge / BattleAnimator.ConditionFadeTime, 0f, 1f) : 0f;

    internal void Begin(int seed, bool instantly)
    {
        Up = true;
        Age = instantly ? BattleAnimator.ConditionArriveTime : 0f;
        FadingAge = -1f;
        reached = 1f;
        Seed = seed;
    }

    internal void End(bool instantly)
    {
        if (!Up) return;
        reached = Strength;
        Up = false;
        FadingAge = instantly ? -1f : 0f;
    }

    internal void Clear()
    {
        Up = false;
        FadingAge = -1f;
        Age = 0f;
    }

    /// <summary>Takes on another mark as it stands (the condition that is going, kept while it fades).</summary>
    internal void Take(LastingMark other)
    {
        Up = other.Up;
        Age = other.Age;
        FadingAge = other.FadingAge;
        Seed = other.Seed;
        reached = other.reached;
    }

    internal void Advance(float dt)
    {
        if (Visible) Age += dt;
        if (FadingAge >= 0f && (FadingAge += dt) >= BattleAnimator.ConditionFadeTime) FadingAge = -1f;
    }
}

/// <summary>
/// What the Pokémon in one place shows for as long as it has it (plan 09 · L2; style guide, "Marks on a Pokémon",
/// "A condition that lasts"): its status condition, a confusion and a love, each a mark with its own ages. The
/// screen's own record, set by <see cref="ConditionLooks.Tell"/> from the log's events and never read from the
/// rules, which are a turn ahead.
/// </summary>
public sealed class ConditionLook
{
    /// <summary>The status condition shown: none, or one of the six (asleep, poisoned, badly poisoned, burned, paralysed, frozen).</summary>
    public StatusCondition Status { get; private set; }

    /// <summary>The mark of <see cref="Status"/>.</summary>
    public LastingMark Ailment { get; } = new();

    /// <summary>The condition that was cured or gave way to another, while its mark fades (none once it has gone).</summary>
    public StatusCondition GoingStatus => Going.Visible ? going : StatusCondition.None;
    public LastingMark Going { get; } = new();
    private StatusCondition going;

    public LastingMark Confusion { get; } = new();
    public LastingMark Love { get; } = new();

    public bool Confused => Confusion.Up;
    public bool InLove => Love.Up;

    /// <summary>Anything at all is shown: standing or fading.</summary>
    public bool Any => Ailment.Visible || Going.Visible || Confusion.Visible || Love.Visible;

    internal void SetStatus(StatusCondition status, ConditionLooks owner, int salt, bool instantly)
    {
        if (status == StatusCondition.Faint) status = StatusCondition.None;
        if (status == Status) return;
        // What it had gives way: its mark fades while the next comes in
        if (Ailment.Up && !instantly)
        {
            going = Status;
            Going.Take(Ailment);
            Going.End(instantly: false);
        }
        else if (instantly) Going.Clear();
        Status = status;
        if (status == StatusCondition.None) Ailment.Clear();
        else Ailment.Begin(owner.NextSeed(salt + (int)status * 17), instantly);
    }

    internal void SetPassing(bool confused, bool inLove, ConditionLooks owner, int salt, bool instantly)
    {
        Set(Confusion, confused, owner, salt + 101, instantly);
        Set(Love, inLove, owner, salt + 211, instantly);
    }

    private static void Set(LastingMark mark, bool up, ConditionLooks owner, int salt, bool instantly)
    {
        if (up == mark.Up) return;
        if (up) mark.Begin(owner.NextSeed(salt), instantly);
        else mark.End(instantly);
    }

    /// <summary>Nothing is shown, at once: the Pokémon is gone from its place, or another takes it.</summary>
    internal void Clear()
    {
        Status = StatusCondition.None;
        Ailment.Clear();
        Going.Clear();
        Confusion.Clear();
        Love.Clear();
    }

    internal void Advance(float dt)
    {
        Ailment.Advance(dt);
        Going.Advance(dt);
        Confusion.Advance(dt);
        Love.Advance(dt);
    }
}

/// <summary>
/// What each Pokémon on the field shows for as long as it has it (plan 09 · L2): a look for each of the four
/// places. It changes by one method, <see cref="Tell"/>, which <see cref="BattleEngine"/> hands every event of the
/// log as its line is read, and which a test that follows a log uses the same way; <see cref="Settle"/> brings it
/// level with the rules once a menu opens, with nothing played.
/// </summary>
public sealed class ConditionLooks
{
    private readonly ConditionLook[,] places = { { new(), new() }, { new(), new() } };
    private int count;

    public ConditionLook Of(BattleSide side, int slot) => places[(int)side, Math.Clamp(slot, 0, 1)];
    public ConditionLook Of(Place place) => Of(place.Side, place.Slot);

    /// <summary>Anything at all is shown on anyone.</summary>
    public bool Any
    {
        get
        {
            foreach (var look in places) if (look.Any) return true;
            return false;
        }
    }

    // A seed of its own for each mark that comes up, from the order they came in: nothing here touches the battle's chance
    internal int NextSeed(int salt) => ++count * 5591 + salt;

    private static int Salt(Place place) => place.Number * 29 + 7;

    /// <summary>
    /// Takes what one of the log's events tells of the Pokémon's conditions; false for an event that tells nothing
    /// of them. A Pokémon that takes a place shows nothing until its line says what it brings
    /// (<see cref="CameInWith"/>, <see cref="VolatileChanged"/>); a condition given or cured, a confusion or a love
    /// begun or ended change the place's look; a faint and a leaving clear it.
    /// </summary>
    public bool Tell(BattleEvent happening)
    {
        switch (happening)
        {
            case Entered entered:
                Of(entered.Place).Clear();
                return true;
            case CameInWith brought:
                Of(brought.Place).SetStatus(brought.Status, this, Salt(brought.Place), instantly: false);
                return true;
            case StatusChanged status:
                Of(status.Place).SetStatus(status.Status, this, Salt(status.Place), instantly: false);
                return true;
            case VolatileChanged passing:
                Of(passing.Place).SetPassing(passing.Confused, passing.InLove, this, Salt(passing.Place), instantly: false);
                return true;
            case Fainted fainted:
                Of(fainted.Place).Clear();
                return true;
            case Left left:
                Of(left.Place).Clear();
                return true;
            default:
                return false;
        }
    }

    /// <summary>What the rules have of the Pokémon in a place: nothing where nobody stands that can fight.</summary>
    public static (StatusCondition Status, bool Confused, bool InLove) RulesHave(Battler place) =>
        place.IsActive ? (place.Pokemon!.Status, place.IsConfused, place.Volatile.InLoveWith != null) : (StatusCondition.None, false, false);

    /// <summary>
    /// A place as the rules have it once a menu opens, with nothing played: what the lines told is already there,
    /// and this only catches a Pokémon changed from outside (a tool, a scripted scene). What is put there is there
    /// whole, and what is taken away is gone at once.
    /// </summary>
    public void Settle(Place place, (StatusCondition Status, bool Confused, bool InLove) rules)
    {
        var look = Of(place);
        look.SetStatus(rules.Status, this, Salt(place), instantly: true);
        look.SetPassing(rules.Confused, rules.InLove, this, Salt(place), instantly: true);
    }

    /// <summary>The first place that shows otherwise than the rules have it, or null when none does.</summary>
    public string? Difference(BattleCore rules)
    {
        foreach (var b in rules.AllBattlers)
        {
            var have = RulesHave(b);
            var look = Of(b.Place);
            if (look.Status != have.Status) return $"{b.Side} {b.Slot} ({b.Pokemon?.DisplayName}): shown {look.Status}, the rules have {have.Status}";
            if (look.Confused != have.Confused) return $"{b.Side} {b.Slot} ({b.Pokemon?.DisplayName}): shown {(look.Confused ? "confused" : "not confused")}, the rules have it {(have.Confused ? "confused" : "not")}";
            if (look.InLove != have.InLove) return $"{b.Side} {b.Slot} ({b.Pokemon?.DisplayName}): shown {(look.InLove ? "in love" : "not in love")}, the rules have it {(have.InLove ? "in love" : "not")}";
        }
        return null;
    }

    internal void Advance(float dt)
    {
        foreach (var look in places) look.Advance(dt);
    }
}
