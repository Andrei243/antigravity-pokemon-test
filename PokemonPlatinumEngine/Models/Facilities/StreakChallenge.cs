using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.Models.Facilities;

/// <summary>What a win at a later battle place came to: the Battle Points it earned, whether it opened the hard room and whether it won the trophy.</summary>
public readonly record struct StreakWin(int Points, bool OpenedHard, bool Trophy, bool Host);

/// <summary>
/// A challenge of a later battle place under way (plan 37 · FC2; <c>docs/mechanics/rulings.md</c>, "The later games'
/// battle places"): the venues' round (<see cref="BattleVenueChallenge"/>) with a place's rules as values
/// (<see cref="StreakRoom"/>), a battle at a time. The entered go in as copies at the room's level, whole every battle
/// (<see cref="FrontierRules.TeamFor"/>); each trainer is drawn as their battle comes, of the later sets of the tiers the
/// streak has reached (<see cref="FacilityTrainers.Draw"/>), or is the room's host at <see cref="StreakRoom.HostAt"/>.
/// Every draw is on a generator of the challenge's own (<see cref="BattleRandom"/>, seeded as the challenge begins) whose
/// state before each trainer's draw is what a challenge put down keeps (<see cref="FacilityPause"/>), so the same trainer
/// comes when it is taken up again, the Tower's rule. No drawing or input.
/// </summary>
public sealed class StreakChallenge
{
    public StreakRoom Room { get; }

    /// <summary>The places in the team of those entered, in the order they were entered.</summary>
    public IReadOnlyList<int> Slots { get; }

    /// <summary>The ids of those entered, in the same order.</summary>
    public IReadOnlyList<Guid> Ids { get; }

    /// <summary>The streak as this sitting of the challenge began (begun, or taken up again).</summary>
    public int StreakBefore { get; }

    private readonly BattleRandom rng;
    private uint stateAtBattle;
    private long drawnAtBattle;
    private FrontierOpponent? opponent;
    private bool opponentIsHost;

    /// <summary>Their species, in the same order.</summary>
    public IReadOnlyList<string> Species { get; }

    private StreakChallenge(StreakRoom room, IReadOnlyList<int> slots, IReadOnlyList<Guid> ids, IReadOnlyList<string> species, BattleRandom rng, int streakBefore)
    {
        Room = room;
        Slots = slots;
        Ids = ids;
        Species = species;
        this.rng = rng;
        StreakBefore = streakBefore;
        stateAtBattle = rng.State;
        drawnAtBattle = rng.Drawn;
    }

    /// <summary>
    /// A challenge begun with those entered (<paramref name="slots"/>): one walked out on ends the streak first, a challenge
    /// put down is forgotten, and the challenge's generator is seeded from two of the caller's sixteen-bit draws.
    /// </summary>
    public static StreakChallenge Begin(FrontierRecords records, StreakRoom room, IReadOnlyList<int> slots, Party party, Func<int> next16)
    {
        var record = records.Facility(room.Place, room.Way, room.Hard);
        if (record.InProgress)
        {
            record.Latest = record.Streak;
            record.Streak = 0;
        }
        record.Paused = null;
        record.InProgress = true;
        uint seed = ((uint)next16() & 0xFFFF) | ((uint)next16() & 0xFFFF) << 16;
        return new StreakChallenge(room, slots.ToList(), IdsOf(party, slots),
            slots.Select(s => s >= 0 && s < party.Count ? party.Members[s].Species.Name : "").ToList(), new BattleRandom(seed), record.Streak);
    }

    private static List<Guid> IdsOf(Party party, IReadOnlyList<int> slots) =>
        slots.Select(s => s >= 0 && s < party.Count ? party.Members[s].Id : Guid.Empty).ToList();

    /// <summary>Whether a challenge put down in this room is waiting.</summary>
    public static bool IsPaused(FrontierRecords records, StreakRoom room) => records.PeekFacility(room.Place, room.Way, room.Hard)?.Paused != null;

    /// <summary>
    /// The challenge put down in this room taken up again, its generator where it stood before the next trainer was
    /// drawn; null when none is waiting, or the team no longer holds the same Pokémon in the same places (the pause is
    /// then given up and the streak ends, as a challenge walked out on).
    /// </summary>
    public static StreakChallenge? Resume(FrontierRecords records, StreakRoom room, Party party)
    {
        var record = records.PeekFacility(room.Place, room.Way, room.Hard);
        if (record?.Paused is not { } pause) return null;
        record.Paused = null;
        bool same = pause.Slots.Count == room.Entered && pause.Slots.Count == pause.Ids.Count && pause.Slots.Count == pause.Species.Count
                    && pause.Slots.Select((slot, n) => (slot, n)).All(p => p.slot >= 0 && p.slot < party.Count
                        && (pause.Ids[p.n] == Guid.Empty || party.Members[p.slot].Id == pause.Ids[p.n]) && party.Members[p.slot].Species.Name == pause.Species[p.n]);
        if (!same)
        {
            record.Latest = record.Streak;
            record.Streak = 0;
            return null;
        }
        record.InProgress = true;
        return new StreakChallenge(room, pause.Slots.ToList(), pause.Ids.ToList(), pause.Species.ToList(), BattleRandom.Resume(pause.Seed, pause.State, pause.Drawn), record.Streak);
    }

    private FacilityRecord Record(FrontierRecords records) => records.Facility(Room.Place, Room.Way, Room.Hard);

    /// <summary>The battle to come, counted from 1 in the streak.</summary>
    public int NextBattle(FrontierRecords records) => Record(records).Streak + 1;

    /// <summary>Whether the battle to come is the host's.</summary>
    public bool HostComes(FrontierRecords records) => Room.HostTrainer != null && NextBattle(records) == Room.HostAt;

    /// <summary>
    /// The trainer of the battle to come: the host at <see cref="StreakRoom.HostAt"/>, or a trainer drawn of the later
    /// sets the streak's tiers allow. Drawn once a battle, on the challenge's generator.
    /// </summary>
    public FrontierOpponent Opponent(FrontierRecords records)
    {
        if (opponent != null) return opponent;
        int streak = Record(records).Streak;
        stateAtBattle = rng.State;
        drawnAtBattle = rng.Drawn;
        if (HostComes(records))
        {
            var record = TrainerDatabase.Get(Room.HostTrainer) ?? throw new InvalidOperationException($"No trainer {Room.HostTrainer} for the {Room.Way} room's host");
            var team = TrainerDatabase.PartyOf(record);
            opponent = new FrontierOpponent
            {
                Number = 0, Pokemon = team.Members.Take(Room.Entered).Select(p => FrontierRules.ForBattle(p, Room.Level)).ToList(),
                Dress = new FrontierDress(record.Id, record.Class, record.Name, Room.Host?.Look ?? "Trainer")
            };
            opponentIsHost = true;
            return opponent;
        }
        var (low, high) = Room.Tiers(streak);
        var drawn = FacilityTrainers.Draw(Room.Entered, low, high, Room.Ivs(streak), rng.Next16, Room.Level);
        opponent = new FrontierOpponent
        {
            Number = 0, Pokemon = drawn.Pokemon, OtId = drawn.OtId, Personalities = drawn.Personalities,
            Dress = new FrontierDress(drawn.Id, drawn.Class, drawn.Name, drawn.Look)
        };
        opponentIsHost = false;
        return opponent;
    }

    /// <summary>The trainer of the battle to come as a battle takes them, with copies of their Pokémon; the host holds a Key Stone.</summary>
    public Trainer OpponentAsTrainer(FrontierRecords records)
    {
        var o = Opponent(records);
        var party = new Party();
        foreach (var p in o.Pokemon) party.Add(FrontierRules.ForBattle(p, Room.Level));
        var record = opponentIsHost ? TrainerDatabase.Get(Room.HostTrainer) : null;
        return new Trainer
        {
            Id = o.Dress?.Id ?? "facility", Name = o.ShownName, TrainerClass = o.ShownClass, PrizeMoney = 0,
            Ai = record != null ? Battle.Sim.Ai.AiFlagNames.Parse(record.Ai) : FrontierTrainers.Mind, Look = o.ShownLook, Party = party,
            Items = record?.Items.ToList() ?? new List<string>(),
            KeyStone = record?.KeyStone ?? false, ZRing = record?.ZRing ?? false
        };
    }

    /// <summary>The team the battle is fought with: copies of those entered at the room's level, whole, in their order.</summary>
    public Party TeamForBattle(Party party) => FrontierRules.TeamFor(party, Slots, Room.Level);

    /// <summary>What the room hands the battle: its format.</summary>
    public FrontierConditions Conditions => new(Format: Room.Format);

    /// <summary>Whether the battle is a double for the host's old flag (a double; a triple and a rotation go by the conditions' format).</summary>
    public bool Doubles => Room.Format == BattleFormat.Double;

    /// <summary>
    /// The battle was won: the streak lengthens and the next trainer is drawn when asked. The host beaten at a normal
    /// room's <see cref="StreakRoom.HostAt"/> opens its hard room; the hard room's host beaten for the first time wins its
    /// trophy (the caller keeps it in <see cref="StreakRoom.TrophyVariable"/>, which <paramref name="trophyHeld"/> says is set).
    /// </summary>
    public StreakWin Won(FrontierRecords records, bool trophyHeld)
    {
        var record = Record(records);
        bool host = opponentIsHost;
        record.Streak = Math.Min(FrontierRecords.Most, record.Streak + 1);
        record.Best = Math.Max(record.Best, record.Streak);
        bool opened = false, trophy = false;
        if (host && !Room.Hard)
        {
            var normal = records.Facility(Room.Place, Room.Way, false);
            opened = !normal.HardOpen;
            normal.HardOpen = true;
        }
        if (host && Room.Hard && Room.TrophyVariable != null && !trophyHeld) trophy = true;
        int points = Room.Points(record.Streak);
        opponent = null;
        opponentIsHost = false;
        return new StreakWin(points, opened, trophy, host);
    }

    /// <summary>The challenge is over, lost or given up: the streak ends and is kept as the latest.</summary>
    public void Finish(FrontierRecords records)
    {
        var record = Record(records);
        record.Latest = record.Streak;
        record.Streak = 0;
        record.InProgress = false;
        record.Paused = null;
    }

    /// <summary>
    /// The challenge put down between battles (to rest: the caller saves the game): the streak stays, and what the next
    /// trainer is drawn from is kept, so taking it up again meets the same one.
    /// </summary>
    public void Pause(FrontierRecords records)
    {
        var record = Record(records);
        record.InProgress = false;
        // The trainer to come is drawn again from where the generator stood before them (or stands now, if not yet drawn)
        uint state = opponent != null ? stateAtBattle : rng.State;
        long drawn = opponent != null ? drawnAtBattle : rng.Drawn;
        record.Paused = new FacilityPause { Slots = Slots.ToList(), Ids = Ids.ToList(), Species = Species.ToList(), Seed = rng.Seed, State = state, Drawn = drawn };
    }

    /// <summary>The room's record as it stands.</summary>
    public FacilityRecord RecordOf(FrontierRecords records) => Record(records);
}
