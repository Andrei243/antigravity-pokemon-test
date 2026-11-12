using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Delve;

/// <summary>How a run stands.</summary>
public enum DelveOutcome
{
    Going,
    /// <summary>The last floor's stairs reached: the bag kept, the recruits there may come home.</summary>
    Cleared,
    /// <summary>The leader fell with no Revive in the bag: nothing found is kept.</summary>
    Fallen,
    /// <summary>Left by an Escape Rope: the bag kept, no recruit comes home.</summary>
    Escaped
}

/// <summary>Why a Pokémon may not go down, or an action may not be taken.</summary>
public enum DelveRefusal
{
    Egg,
    Fainted,
    Locked,
    /// <summary>The leader and the partner are the same Pokémon.</summary>
    Same,
    /// <summary>The run is over, or waits for a recruit's answer.</summary>
    NotNow,
    /// <summary>A step onto a wall, a pool or a resident.</summary>
    Blocked,
    NoMove,
    NoPP,
    /// <summary>A move whose doing is a battle's (a switch, a weather, the field, a power the battle works out).</summary>
    NeedsABattle,
    NotInTheBag,
    /// <summary>An item that would do nothing now.</summary>
    WouldNotHelp,
    /// <summary>A recruit's answer when nobody asks.</summary>
    NobodyAsks
}

/// <summary>What the leader is told to do on a turn.</summary>
public enum DelveActionKind { Step, Strike, Move, Item, Wait, Answer }

/// <summary>One action of the leader (or, for <see cref="DelveActionKind.Item"/>, an item given to a teammate).</summary>
public readonly record struct DelveAction(DelveActionKind Kind, Direction Way = Direction.Down, int Move = 0, string? Item = null, int On = 0, bool Yes = false)
{
    public static DelveAction Step(Direction way) => new(DelveActionKind.Step, way);
    public static DelveAction Strike(Direction way) => new(DelveActionKind.Strike, way);
    public static DelveAction UseMove(int move, Direction way) => new(DelveActionKind.Move, way, move);
    public static DelveAction Use(string item, int on = 0) => new(DelveActionKind.Item, Item: item, On: on);
    public static DelveAction Wait => new(DelveActionKind.Wait);
    public static DelveAction Answer(bool yes) => new(DelveActionKind.Answer, Yes: yes);
}

/// <summary>What happened, for the screen's log (SD8) and the tests: plain data, never words.</summary>
public enum DelveEventKind
{
    Stepped, Struck, UsedMove, Missed, Damaged, Fainted, StatusGiven, StatusFailed, Confused, Snapped, StatChanged, Healed,
    Recoil, Drained, Flinched, LostTurn, HurtItself, Woke, Noticed, Fled, Snare, Found, BagFull, Ate, Cured, Starving,
    Descended, AsksToJoin, Joined, Declined, Revived, Cleared, Fell, Escaped, Bitten,
    /// <summary>The leader on the last floor's stairs with the guardian still standing (SD9).</summary>
    Guarded
}

/// <summary>An event of the run: who, at whom, with what (a move's, an item's or a status's name) and how much.</summary>
public sealed record DelveEvent(DelveEventKind Kind, string Who, string? Target = null, string? What = null, int Amount = 0);

/// <summary>
/// A run down a hollow (plan 44 · SD7; <c>docs/mechanics/rulings.md</c>, "The delve"): the player stays at the camp; a
/// leader and a partner of the team go down as copies at the hollow's level (<see cref="LevelScaling.At"/>, holding
/// nothing), with up to two recruits met on the way. Every action of the leader is a turn, after which everyone else acts
/// once (<see cref="DelveTurns"/>). Everything is drawn from the run's one <see cref="DelveRandom"/>, a floor from its own
/// seed, so a seed and the actions are the whole run. Nothing it does reaches the player's own Pokémon. No drawing or input.
/// </summary>
public sealed class DelveRun
{
    /// <summary>The hunger a run begins with, and the most it holds.</summary>
    public const int FullHunger = 100;

    /// <summary>The turns between two points of hunger lost.</summary>
    public const int HungerEvery = 10;

    /// <summary>The turns between two mends of the team's HP while it isn't starving, and between two bites of a poison or a burn.</summary>
    public const int MendEvery = 5;

    /// <summary>How many items the run's bag holds.</summary>
    public const int BagSize = 16;

    /// <summary>The team's most: the leader, the partner and two recruits.</summary>
    public const int MostOnTheTeam = 4;

    public DelveHollow Hollow { get; }
    public uint Seed { get; }
    public DelveFloor Floor { get; private set; } = null!;
    public List<DelveMon> Team { get; } = new();
    public List<DelveMon> Residents { get; } = new();
    public List<string> Bag { get; } = new();
    public List<(int X, int Y, string Item)> Finds { get; } = new();
    public List<(int X, int Y, SnareKind Kind)> Snares { get; } = new();
    public int Hunger { get; internal set; } = FullHunger;
    public int Turn { get; private set; }
    public DelveOutcome Outcome { get; internal set; }
    public List<DelveEvent> Events { get; } = new();

    /// <summary>A resident knocked out that asks to join, waiting for <see cref="DelveAction.Answer"/>.</summary>
    public DelveMon? Asking { get; internal set; }

    /// <summary>The recruits that reached the hollow's end with the team (SD9 brings them home).</summary>
    public List<Pokemon> RecruitsAtTheEnd { get; } = new();

    /// <summary>A test's hand on the recruit's roll: the draw of a hundred it comes out as, with no draw made.</summary>
    public int? FixedRecruitRoll { get; set; }

    internal DelveRandom Rng { get; }

    /// <summary>The deepest floor the team has stood on, from one.</summary>
    public int DeepestFloor { get; private set; }

    /// <summary>The hollow's guardian on its last floor, or null (SD9).</summary>
    public DelveMon? Guardian { get; private set; }

    /// <summary>How a resident's species is met (the randomiser's map, plan 13 · V6); null is the hollow's own.</summary>
    public Func<string, string>? MapSpecies { get; init; }
    private int nextOrder;

    public DelveMon Leader => Team[0];

    private DelveRun(DelveHollow hollow, uint seed)
    {
        Hollow = hollow;
        Seed = seed;
        Rng = new DelveRandom(seed);
    }

    /// <summary>Why a Pokémon may not go down, or null.</summary>
    public static DelveRefusal? WhyNot(Pokemon? pokemon)
    {
        if (pokemon == null || pokemon.IsEgg) return DelveRefusal.Egg;
        if (pokemon.IsFainted) return DelveRefusal.Fainted;
        if (HeartLock.Refuses(pokemon, LockedUse.Delve)) return DelveRefusal.Locked;
        return null;
    }

    /// <summary>Why a leader and a partner may not go down together, or null.</summary>
    public static DelveRefusal? WhyNot(Pokemon leader, Pokemon partner) =>
        WhyNot(leader) ?? WhyNot(partner) ?? (ReferenceEquals(leader, partner) ? DelveRefusal.Same : null);

    /// <summary>A run begun on its first floor, the two going down as copies at the hollow's level holding nothing.</summary>
    public static DelveRun Begin(DelveHollow hollow, Pokemon leader, Pokemon partner, uint seed, Func<string, string>? mapSpecies = null)
    {
        if (WhyNot(leader, partner) is { } refused) throw new InvalidOperationException($"Can't go down: {refused}.");
        var run = new DelveRun(hollow, seed) { MapSpecies = mapSpecies };
        run.Team.Add(run.Copy(leader, true));
        run.Team.Add(run.Copy(partner, true));
        run.EnterFloor(0);
        return run;
    }

    private DelveMon Copy(Pokemon pokemon, bool onTeam)
    {
        var copy = LevelScaling.At(pokemon, Hollow.Level);
        copy.HeldItem = null;
        return new DelveMon(copy, onTeam, nextOrder++);
    }

    /// <summary>The seed of a floor of this run.</summary>
    public uint FloorSeed(int index) => DelveRandom.Mix(Seed, index + 1);

    internal void EnterFloor(int index)
    {
        Floor = DelveFloor.Generate(Hollow, index, FloorSeed(index));
        DeepestFloor = Math.Max(DeepestFloor, index + 1);
        Residents.Clear();
        Guardian = null;
        Finds.Clear();
        Finds.AddRange(Floor.Finds);
        Snares.Clear();
        Snares.AddRange(Floor.Snares);
        var made = new DelveRandom(DelveRandom.Mix(Floor.Seed, 0x5EED));
        foreach (var spawn in Floor.Residents)
        {
            string name = MapSpecies?.Invoke(spawn.Species) ?? spawn.Species;
            var species = PokemonDatabase.Get(name) ?? throw new InvalidOperationException($"No species {name}.");
            var pokemon = new Pokemon(species, Hollow.Level, made) { HeldItem = null };
            Residents.Add(new DelveMon(pokemon, false, nextOrder++) { X = spawn.X, Y = spawn.Y, Dormant = spawn.Asleep, Shy = spawn.Shy, Facing = (Direction)made.Next(4) });
        }
        // The team comes down round the way in, the leader on it, the others on the nearest open tiles in a fixed order
        Leader.X = Floor.Entrance.X;
        Leader.Y = Floor.Entrance.Y;
        var near = Floor.Grid.Reachable(Floor.Entrance, new HashSet<(int, int)>())
            .Where(t => t != Floor.Entrance && !Residents.Any(r => r.X == t.X && r.Y == t.Y))
            .OrderBy(t => Math.Abs(t.X - Floor.Entrance.X) + Math.Abs(t.Y - Floor.Entrance.Y)).ThenBy(t => t.Y).ThenBy(t => t.X).ToList();
        for (int i = 1; i < Team.Count; i++)
        {
            Team[i].X = near[i - 1].X;
            Team[i].Y = near[i - 1].Y;
            Team[i].Facing = Leader.Facing;
        }
        // The guardian stands on the open tile nearest the last floor's stairs, wakeful, never shy and never asking to join (SD9)
        if (Floor.IsLast && Hollow.Guardian is { } guardian) PlaceGuardian(guardian, made);
    }

    private void PlaceGuardian(string guardian, DelveRandom made)
    {
        string name = MapSpecies?.Invoke(guardian) ?? guardian;
        var species = PokemonDatabase.Get(name) ?? throw new InvalidOperationException($"No species {name}.");
        var stairs = Floor.Stairs;
        var at = Floor.Grid.Reachable(Floor.Entrance, new HashSet<(int, int)>())
            .Where(t => t != stairs && t != Floor.Entrance && At(t.X, t.Y) == null && Team.All(m => (m.X, m.Y) != t))
            .OrderBy(t => Math.Abs(t.X - stairs.X) + Math.Abs(t.Y - stairs.Y)).ThenBy(t => t.Y).ThenBy(t => t.X).First();
        var pokemon = new Pokemon(species, Hollow.Level + DelveHollow.GuardianOver, made) { HeldItem = null };
        Guardian = new DelveMon(pokemon, false, nextOrder++) { X = at.X, Y = at.Y, IsGuardian = true };
        Residents.Add(Guardian);
    }

    internal void Say(DelveEventKind kind, string who, string? target = null, string? what = null, int amount = 0) =>
        Events.Add(new DelveEvent(kind, who, target, what, amount));

    /// <summary>The leader turned on the spot, which is no turn (SD8: a way held with the turn button).</summary>
    public void Face(Direction way)
    {
        if (Outcome == DelveOutcome.Going) Leader.Facing = way;
    }

    /// <summary>Whoever stands on a tile, of the team or the floor, or null.</summary>
    public DelveMon? At(int x, int y) =>
        Team.FirstOrDefault(m => !m.Fainted && m.X == x && m.Y == y) ?? Residents.FirstOrDefault(m => !m.Fainted && m.X == x && m.Y == y);

    /// <summary>Why the leader may not do an action now, or null.</summary>
    public DelveRefusal? WhyNot(DelveAction action)
    {
        if (Outcome != DelveOutcome.Going) return DelveRefusal.NotNow;
        if (action.Kind == DelveActionKind.Answer) return Asking == null ? DelveRefusal.NobodyAsks : null;
        if (Asking != null) return DelveRefusal.NotNow;
        switch (action.Kind)
        {
            case DelveActionKind.Step:
            {
                var (x, y) = DelveTurns.Ahead(Leader.X, Leader.Y, action.Way);
                if (!Floor.Passable(x, y, Leader.Swims)) return DelveRefusal.Blocked;
                var there = At(x, y);
                return there != null && !there.OnTeam ? DelveRefusal.Blocked : null;
            }
            case DelveActionKind.Move: return DelveMoves.WhyNot(Leader, action.Move);
            case DelveActionKind.Item:
                if (!Bag.Contains(action.Item ?? "")) return DelveRefusal.NotInTheBag;
                if (action.On < 0 || action.On >= Team.Count || Team[action.On].Fainted) return DelveRefusal.WouldNotHelp;
                return DelveItems.Helps(this, Team[action.On], action.Item!) ? null : DelveRefusal.WouldNotHelp;
            default: return null;
        }
    }

    /// <summary>
    /// The leader's action and, if it was a turn, everyone else's after it. Returns the refusal and changes nothing where
    /// the action may not be taken. A recruit's answer is no turn.
    /// </summary>
    public DelveRefusal? Act(DelveAction action)
    {
        if (WhyNot(action) is { } refused) return refused;
        if (action.Kind == DelveActionKind.Answer)
        {
            Recruits.Answer(this, action.Yes);
            return null;
        }
        int floor = Floor.Index;
        if (DelveTurns.MayAct(this, Leader))
        {
            switch (action.Kind)
            {
                case DelveActionKind.Step: StepLeader(action.Way); break;
                case DelveActionKind.Strike:
                    Leader.Facing = action.Way;
                    DelveMoves.Strike(this, Leader, action.Way);
                    break;
                case DelveActionKind.Move: DelveMoves.Use(this, Leader, action.Move, action.Way); break;
                case DelveActionKind.Item: DelveItems.Use(this, Team[action.On], action.Item!); break;
            }
        }
        // A new floor begins with the team's arrival: nobody there acts on the turn that brought it down
        if (Outcome == DelveOutcome.Going && Floor.Index == floor) DelveTurns.Round(this);
        EndOfTurn();
        return null;
    }

    private void StepLeader(Direction way)
    {
        Leader.Facing = way;
        var (x, y) = DelveTurns.Ahead(Leader.X, Leader.Y, way);
        // A teammate in the way changes places with the leader
        if (At(x, y) is { OnTeam: true } mate)
        {
            mate.X = Leader.X;
            mate.Y = Leader.Y;
        }
        Leader.X = x;
        Leader.Y = y;
        Say(DelveEventKind.Stepped, Leader.Name, amount: (int)way);
        LeaderArrives();
    }

    /// <summary>What the leader's tile does: a find picked up, a snare sprung, the stairs taken.</summary>
    internal void LeaderArrives()
    {
        int find = Finds.FindIndex(f => f.X == Leader.X && f.Y == Leader.Y);
        if (find >= 0)
        {
            if (Bag.Count >= BagSize) Say(DelveEventKind.BagFull, Leader.Name, what: Finds[find].Item);
            else
            {
                Bag.Add(Finds[find].Item);
                Say(DelveEventKind.Found, Leader.Name, what: Finds[find].Item);
                Finds.RemoveAt(find);
            }
        }
        int snare = Snares.FindIndex(s => s.X == Leader.X && s.Y == Leader.Y);
        if (snare >= 0)
        {
            var kind = Snares[snare].Kind;
            Snares.RemoveAt(snare);
            Say(DelveEventKind.Snare, Leader.Name, what: kind.ToString());
            switch (kind)
            {
                case SnareKind.Sleep: DelveMoves.GiveStatus(this, Leader, Leader, StatusCondition.Sleep, sleepTurns: 3); break;
                case SnareKind.Poison: DelveMoves.GiveStatus(this, Leader, Leader, StatusCondition.Poison); break;
                case SnareKind.Drop: GoDown(); return;
            }
        }
        if ((Leader.X, Leader.Y) == Floor.Stairs) GoDown();
    }

    private void GoDown()
    {
        if (Floor.IsLast)
        {
            // The guardian keeps the way up until it is knocked out
            if (Guardian is { Fainted: false })
            {
                Say(DelveEventKind.Guarded, Leader.Name, Guardian.Name);
                return;
            }
            Outcome = DelveOutcome.Cleared;
            foreach (var m in Team.Where(m => m.Recruit && !m.Fainted)) RecruitsAtTheEnd.Add(m.Pokemon);
            Say(DelveEventKind.Cleared, Leader.Name, amount: Floor.Index + 1);
            return;
        }
        // A recruit's question unanswered as the team goes down is a no
        if (Asking != null) Recruits.Answer(this, false);
        EnterFloor(Floor.Index + 1);
        Say(DelveEventKind.Descended, Leader.Name, amount: Floor.Index + 1);
    }

    private void EndOfTurn()
    {
        if (Outcome != DelveOutcome.Going) return;
        Turn++;
        if (Turn % HungerEvery == 0 && Hunger > 0) Hunger--;
        if (Hunger == 0)
        {
            Leader.Pokemon.CurrentHP = Math.Max(0, Leader.Pokemon.CurrentHP - 1);
            Say(DelveEventKind.Starving, Leader.Name, amount: 1);
        }
        if (Turn % MendEvery == 0)
        {
            foreach (var m in Team.Concat(Residents).Where(m => !m.Fainted).ToList())
            {
                var status = m.Pokemon.Status;
                if (status is StatusCondition.Poison or StatusCondition.Toxic or StatusCondition.Burn)
                {
                    int bite = Math.Max(1, m.Pokemon.MaxHP / 16);
                    m.Pokemon.CurrentHP = Math.Max(0, m.Pokemon.CurrentHP - bite);
                    Say(DelveEventKind.Bitten, m.Name, what: status.ToString(), amount: bite);
                    if (m.Fainted) DelveMoves.Faint(this, null, m);
                }
                else if (m.OnTeam && Hunger > 0 && m.Pokemon.CurrentHP < m.Pokemon.MaxHP)
                    m.Pokemon.CurrentHP = Math.Min(m.Pokemon.MaxHP, m.Pokemon.CurrentHP + Math.Max(1, m.Pokemon.MaxHP / 20));
            }
        }
        LeaderFalls();
    }

    /// <summary>A fallen leader: a Revive in the bag is used at once, or the run is lost with all that was found.</summary>
    internal void LeaderFalls()
    {
        if (Outcome != DelveOutcome.Going || !Leader.Fainted) return;
        int revive = Bag.IndexOf("Revive");
        if (revive >= 0)
        {
            Bag.RemoveAt(revive);
            Leader.Pokemon.CurrentHP = Math.Max(1, Leader.Pokemon.MaxHP / 2);
            Leader.Pokemon.Status = StatusCondition.None;
            Say(DelveEventKind.Revived, Leader.Name, what: "Revive", amount: Leader.Pokemon.CurrentHP);
            return;
        }
        Outcome = DelveOutcome.Fallen;
        Bag.Clear();
        Say(DelveEventKind.Fell, Leader.Name);
    }

    /// <summary>The run left by an Escape Rope.</summary>
    internal void Escape()
    {
        Outcome = DelveOutcome.Escaped;
        Say(DelveEventKind.Escaped, Leader.Name, what: "Escape Rope");
    }
}
