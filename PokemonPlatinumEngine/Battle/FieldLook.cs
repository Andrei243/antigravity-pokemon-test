using System;
using PokemonPlatinumEngine.Battle.Sim;

namespace PokemonPlatinumEngine.Battle;

/// <summary>
/// One thing drawn on the field for as long as it lasts (plan 25 · Y2): how much of it stands, how long since the
/// newest of it came, and what of it is fading away. What it looks like at these ages is
/// <see cref="Graphics.BattleFieldFx"/>, a pure function of them.
/// </summary>
public sealed class ConditionView
{
    /// <summary>How much of it stands: 0 for none, 1 for a screen, a room or something sent ahead, the layers of Spikes and Toxic Spikes.</summary>
    public int Layers { get; internal set; }

    /// <summary>Seconds since the newest layer came (it comes in over <see cref="BattleAnimator.FieldArriveTime"/>).</summary>
    public float Age { get; internal set; }

    /// <summary>The layers that are going, and for how long they have been (-1: nothing is).</summary>
    public int Fading { get; internal set; }
    public float FadingAge { get; internal set; } = -1f;

    /// <summary>From the animator's own count, never the battle's chance: varies where its pieces lie.</summary>
    public int Seed { get; internal set; }

    public bool Up => Layers > 0;

    /// <summary>
    /// The place a Max Move's blow landed on as the newest layer came (plan 25 · YD4), which it arrives from; null for
    /// one that simply came, or once it has settled with the rules. Only how it comes goes by it, never what lasts.
    /// </summary>
    public Place? From { get; internal set; }

    /// <summary>
    /// It is arriving from a blow now: a <see cref="From"/>, and its newest layer still within
    /// <see cref="BattleAnimator.BlowArrivalTime"/> (longer than its own coming in, so the Max Move's pull-back sees the flood run out).
    /// </summary>
    public bool ArrivingFromABlow => Up && From != null && Age < BattleAnimator.BlowArrivalTime;

    /// <summary>Something of it is on screen: standing, or still fading.</summary>
    public bool Visible => Up || FadingAge >= 0f;

    /// <summary>The layers to draw: those standing, or those fading once nothing stands.</summary>
    public int Shown => Up ? Layers : FadingAge >= 0f ? Fading : 0;

    /// <summary>How far the newest layer has come in, 0..1 (1 for what is fading: it came long ago).</summary>
    public float Arrival => Up ? Math.Clamp(Age / BattleAnimator.FieldArriveTime, 0f, 1f) : FadingAge >= 0f ? 1f : 0f;

    /// <summary>How much of it is still there, 0..1: whole while it stands, easing away over <see cref="BattleAnimator.FieldFadeTime"/>.</summary>
    public float Strength => Up ? 1f : FadingAge >= 0f ? Math.Clamp(1f - FadingAge / BattleAnimator.FieldFadeTime, 0f, 1f) : 0f;

    /// <summary>How far a look with no pieces of its own has come (the light under Gravity): in over the fade's time, and out again.</summary>
    public float Presence => Up ? Math.Clamp(Age / BattleAnimator.FieldFadeTime, 0f, 1f) : Strength;

    internal void Set(int layers, FieldLook owner, int salt, bool instantly, Place? from = null)
    {
        layers = Math.Max(0, layers);
        if (layers == Layers) return;
        if (layers == 0)
        {
            // What stood eases away; settled, it is simply gone
            Fading = instantly ? 0 : Layers;
            FadingAge = instantly ? -1f : 0f;
            Layers = 0;
            Age = 0f;
            From = null;
            return;
        }
        // The first layer brings its seed; a layer more keeps it, so what lies there already stays where it lay
        if (Layers == 0) Seed = owner.NextSeed(salt);
        Layers = layers;
        From = instantly ? null : from;
        Age = instantly ? BattleAnimator.FieldArriveTime : 0f;
        Fading = 0;
        FadingAge = -1f;
    }

    internal void Advance(float dt)
    {
        if (Up) Age += dt;
        if (FadingAge >= 0f && (FadingAge += dt) >= BattleAnimator.FieldFadeTime)
        {
            FadingAge = -1f;
            Fading = 0;
        }
    }
}

/// <summary>
/// What lies on the field while a battle is shown (plan 25 · Y2; style guide, "What lies on the field"): each side's
/// screens, veils and hazards, Trick Room and Gravity, and what waits for each place. It is the screen's own record,
/// set by the log's events as their lines are read (<see cref="SideConditionChanged"/>,
/// <see cref="FieldConditionChanged"/>, <see cref="PlaceConditionChanged"/>) and never read from the rules, which
/// are a turn ahead; <see cref="Settle"/> brings it level with them once a menu opens.
/// </summary>
public sealed class FieldLook
{
    private static readonly SideCondition[] SideKinds = Enum.GetValues<SideCondition>();
    private static readonly FieldCondition[] FieldKinds = Enum.GetValues<FieldCondition>();
    private static readonly PlaceCondition[] PlaceKinds = Enum.GetValues<PlaceCondition>();

    private readonly ConditionView[,] sides = new ConditionView[2, SideKinds.Length];
    private readonly ConditionView[] rooms = new ConditionView[FieldKinds.Length];
    // Every place a battle can have: a horde's five on the wild side (plan 14 · B4)
    private readonly ConditionView[,,] places = new ConditionView[2, BattleAnimator.MostSlots, PlaceKinds.Length];
    private int count;

    public FieldLook()
    {
        for (int side = 0; side < 2; side++)
        {
            for (int i = 0; i < SideKinds.Length; i++) sides[side, i] = new ConditionView();
            for (int slot = 0; slot < BattleAnimator.MostSlots; slot++)
                for (int i = 0; i < PlaceKinds.Length; i++) places[side, slot, i] = new ConditionView();
        }
        for (int i = 0; i < FieldKinds.Length; i++) rooms[i] = new ConditionView();
    }

    public ConditionView Of(BattleSide side, SideCondition condition) => sides[(int)side, (int)condition];
    public ConditionView Of(FieldCondition condition) => rooms[(int)condition];
    public ConditionView Of(Place place, PlaceCondition condition) => places[(int)place.Side, BattleAnimator.Within(place.Slot), (int)condition];

    /// <summary>Anything at all is drawn: standing or fading.</summary>
    public bool Any
    {
        get
        {
            foreach (var view in sides) if (view.Visible) return true;
            foreach (var view in rooms) if (view.Visible) return true;
            foreach (var view in places) if (view.Visible) return true;
            return false;
        }
    }

    // A seed of its own for each thing that comes up, from the order they came in: nothing here touches the battle's chance
    internal int NextSeed(int salt) => ++count * 6151 + salt;

    private static int Salt(BattleSide side, SideCondition condition) => (int)condition * 31 + (int)side;
    private static int Salt(FieldCondition condition) => (int)condition * 37 + 5;
    private static int Salt(Place place, PlaceCondition condition) => (int)condition * 41 + place.Number * 7 + 11;

    /// <summary>A side has this much of something now: more of it comes in, none of it fades away.</summary>
    /// <param name="from">The place a Max Move's blow landed on, which a new layer arrives from (plan 25 · YD4).</param>
    public void Set(BattleSide side, SideCondition condition, int layers, Place? from = null) =>
        Of(side, condition).Set(layers, this, Salt(side, condition), instantly: false, from);

    public void Set(FieldCondition condition, bool up, Place? from = null) => Of(condition).Set(up ? 1 : 0, this, Salt(condition), instantly: false, from);

    public void Set(Place place, PlaceCondition condition, bool up, Place? from = null) =>
        Of(place, condition).Set(up ? 1 : 0, this, Salt(place, condition), instantly: false, from);

    /// <summary>
    /// Takes what one of the log's events tells of the field; false for an event that tells nothing of it. This is
    /// the one way the look changes while a battle is shown: <see cref="BattleEngine"/> hands every event here as
    /// its line is read, and a test that follows a log does the same.
    /// </summary>
    /// <param name="from">
    /// The place a Max Move's blow landed on, when the event is one of that Max Move's lines (plan 25 · YD4): what it
    /// brings arrives from there. It changes how a thing comes and never what stands, nor any seed.
    /// </param>
    public bool Tell(BattleEvent happening, Place? from = null)
    {
        switch (happening)
        {
            case SideConditionChanged side:
                Set(side.Side, side.Condition, side.Layers, from);
                return true;
            case FieldConditionChanged room:
                Set(room.Condition, room.Up, from);
                return true;
            case PlaceConditionChanged waiting:
                Set(waiting.Place, waiting.Condition, waiting.Up, from);
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// The field as the rules have it once a menu opens, with nothing played: what the lines told is already there,
    /// and this only catches a battle the screen fell behind (a tool that changed the field between turns).
    /// </summary>
    public void Settle(FieldState rules)
    {
        for (int s = 0; s < 2; s++)
        {
            var side = (BattleSide)s;
            foreach (var kind in SideKinds)
                Of(side, kind).Set(rules.Side(side).Layers(kind), this, Salt(side, kind), instantly: true);
            for (int slot = 0; slot < BattleAnimator.MostSlots; slot++)
            {
                var place = new Place(side, slot);
                foreach (var kind in PlaceKinds)
                    Of(place, kind).Set(rules.At(place).Has(kind) ? 1 : 0, this, Salt(place, kind), instantly: true);
            }
        }
        foreach (var kind in FieldKinds) Of(kind).Set(rules.Has(kind) ? 1 : 0, this, Salt(kind), instantly: true);
    }

    /// <summary>Whether what stands here is what the rules have: every side's layers, both rooms, every place's wait.</summary>
    public bool Agrees(FieldState rules) => Difference(rules) == null;

    /// <summary>The first thing that stands here otherwise than the rules have it, or null when nothing does.</summary>
    public string? Difference(FieldState rules)
    {
        for (int s = 0; s < 2; s++)
        {
            var side = (BattleSide)s;
            foreach (var kind in SideKinds)
                if (Of(side, kind).Layers != rules.Side(side).Layers(kind))
                    return $"{side} {kind}: shown {Of(side, kind).Layers}, the rules have {rules.Side(side).Layers(kind)}";
            for (int slot = 0; slot < BattleAnimator.MostSlots; slot++)
            {
                var place = new Place(side, slot);
                foreach (var kind in PlaceKinds)
                    if (Of(place, kind).Up != rules.At(place).Has(kind))
                        return $"{side} {slot} {kind}: shown {Of(place, kind).Up}, the rules have {rules.At(place).Has(kind)}";
            }
        }
        foreach (var kind in FieldKinds)
            if (Of(kind).Up != rules.Has(kind)) return $"{kind}: shown {Of(kind).Up}, the rules have {rules.Has(kind)}";
        return null;
    }

    internal void Advance(float dt)
    {
        foreach (var view in sides) view.Advance(dt);
        foreach (var view in rooms) view.Advance(dt);
        foreach (var view in places) view.Advance(dt);
    }
}
