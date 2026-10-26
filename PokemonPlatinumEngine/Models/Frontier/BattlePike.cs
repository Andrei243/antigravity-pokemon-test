using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Frontier;

/// <summary>What waits behind a door of the Battle Pike (Emerald's <c>PIKE_ROOM_*</c>, in its order).</summary>
public enum PikeRoom { SingleBattle, HealFull, Visitor, Status, HealPart, WildPokemon, HardBattle, DoubleBattle, Brain }

/// <summary>What the attendant of the three doors senses behind one of them (Emerald's <c>PIKE_HINT_*</c>, in its order).</summary>
public enum PikeHint { Nostalgia, Whispering, Pokemon, People, Brain }

/// <summary>
/// The Battle Pike's rooms of chance (plan 14 · B10; <c>docs/mechanics/rulings.md</c>, "The Battle Pike"), ported from
/// Emerald's <c>src/battle_pike.c</c> at the commit <c>Sources.EmeraldCommit</c> pins: the hint given before the doors
/// (<c>SetHintedRoom</c>), the room behind the door chosen (<c>GetNextRoomType</c>), a status from the room's Pokémon
/// (<c>TryInflictRandomStatus</c>), the healing rooms (<c>TryHealMons</c>) and the wild Pokémon of the round
/// (<c>sLvl50Mons</c>). Every draw is the sixteen-bit draw the caller hands over, taken in the original's order. No
/// drawing or input.
/// </summary>
public static class PikeRooms
{
    public const int Doors = 3;

    /// <summary>What the attendant senses of each room (<c>sRoomTypeHints</c>): two rooms to a hint, the Brain's alone.</summary>
    public static PikeHint HintOf(PikeRoom room) => room switch
    {
        PikeRoom.SingleBattle or PikeRoom.HealFull => PikeHint.People,
        PikeRoom.Visitor or PikeRoom.DoubleBattle => PikeHint.Whispering,
        PikeRoom.Status or PikeRoom.HealPart => PikeHint.Nostalgia,
        PikeRoom.WildPokemon or PikeRoom.HardBattle => PikeHint.Pokemon,
        _ => PikeHint.Brain
    };

    /// <summary>
    /// How many of the three a room touches, by the room of the round it is (1 to 14): one in the first four, two to
    /// the ninth, three after (<c>TryInflictRandomStatus</c>, <c>AtLeastOneHealthyMon</c>).
    /// </summary>
    public static int CountFor(int room) => room <= 4 ? 1 : room <= 9 ? 2 : 3;

    /// <summary>Fewer than two of the three have fainted (<c>AtLeastTwoAliveMons</c>, which counts the fallen).</summary>
    public static bool AtLeastTwoAlive(Party team) => team.Members.Take(3).Count(p => p.CurrentHP == 0) < 2;

    /// <summary>One of the first so many standing Pokémon of the three has no condition (<c>AtLeastOneHealthyMon</c>).</summary>
    public static bool AtLeastOneHealthy(Party team, int room)
    {
        int count = CountFor(room), healthy = 0;
        foreach (var p in team.Members.Take(3))
        {
            if (p.Status == StatusCondition.None && p.CurrentHP != 0) healthy++;
            if (healthy == count) break;
        }
        return healthy > 0;
    }

    /// <summary>Every one of the three at full HP, with no condition and every move's PP whole (<c>IsPartyFullHealed</c>).</summary>
    public static bool FullHealth(Party team) => team.Members.Take(3).All(p => p.CurrentHP >= p.MaxHP && p.Status == StatusCondition.None
        && p.Moves.All(m => m.CurrentPP >= m.MaxPP));

    /// <summary>
    /// The hint before the doors (<c>SetHintedRoom</c>): with the Pike Queen due every door leads to her, and the door
    /// number drawn (0 to 5) says how many of the three each door heals first; otherwise one door of three is drawn and
    /// the room behind it, from all eight (six when the healing rooms are off), a status room or a double battle the team
    /// couldn't feel becoming a visitor's.
    /// </summary>
    public static (int Door, PikeRoom Room) Hint(bool queen, bool healingOff, Party team, int room, Func<int> next16)
    {
        if (queen) return ((next16() & 0xFFFF) % 6, PikeRoom.Brain);
        int door = (next16() & 0xFFFF) % Doors;
        var candidates = Enumerable.Range(0, (int)PikeRoom.Brain).Select(i => (PikeRoom)i)
            .Where(r => !healingOff || r is not (PikeRoom.HealFull or PikeRoom.HealPart)).ToList();
        var hinted = candidates[(next16() & 0xFFFF) % candidates.Count];
        if (hinted == PikeRoom.Status && !AtLeastOneHealthy(team, room)) hinted = PikeRoom.Visitor;
        if (hinted == PikeRoom.DoubleBattle && !AtLeastTwoAlive(team)) hinted = PikeRoom.Visitor;
        return (door, hinted);
    }

    /// <summary>
    /// The room behind the door chosen (<c>GetNextRoomType</c>): the Brain's behind every door when she is due, the
    /// hinted room behind the hinted door, and behind the other two a room drawn from those whose hint isn't the hinted
    /// one's, less a double battle for a team with two fallen, a status room for a team with nobody to feel it and the
    /// healing rooms when they are off.
    /// </summary>
    public static PikeRoom Next(int door, int hintedDoor, PikeRoom hinted, bool healingOff, Party team, int room, Func<int> next16)
    {
        if (hinted == PikeRoom.Brain) return PikeRoom.Brain;
        if (door == hintedDoor) return hinted;
        var hint = HintOf(hinted);
        var candidates = Enumerable.Range(0, (int)PikeRoom.Brain).Select(i => (PikeRoom)i).Where(r => HintOf(r) != hint)
            .Where(r => r != PikeRoom.DoubleBattle || AtLeastTwoAlive(team))
            .Where(r => r != PikeRoom.Status || AtLeastOneHealthy(team, room))
            .Where(r => !healingOff || r is not (PikeRoom.HealFull or PikeRoom.HealPart)).ToList();
        return candidates[(next16() & 0xFFFF) % candidates.Count];
    }

    // The order the three are tried in: ten swaps of two places drawn (TryInflictRandomStatus, TryHealMons)
    private static int[] Shuffled(Func<int> next16)
    {
        int[] order = { 0, 1, 2 };
        for (int k = 0; k < 10; k++)
        {
            int i = (next16() & 0xFFFF) % 3, j = (next16() & 0xFFFF) % 3;
            (order[i], order[j]) = (order[j], order[i]);
        }
        return order;
    }

    /// <summary>A condition the Pokémon's own ability keeps off (<c>DoesAbilityPreventStatus</c>).</summary>
    public static bool AbilityPrevents(Pokemon p, StatusCondition status) => (status, p.AbilityName) switch
    {
        (StatusCondition.Freeze, "Magma Armor") or (StatusCondition.Burn, "Water Veil") or (StatusCondition.Paralyze, "Limber")
            or (StatusCondition.Sleep, "Insomnia" or "Vital Spirit") or (StatusCondition.Toxic, "Immunity") => true,
        _ => false
    };

    /// <summary>A condition its type keeps off (<c>DoesTypePreventStatus</c>: Ground keeps paralysis off, as Emerald has it).</summary>
    public static bool TypePrevents(Pokemon p, StatusCondition status) => status switch
    {
        StatusCondition.Toxic => p.HasType(PokemonType.Steel) || p.HasType(PokemonType.Poison),
        StatusCondition.Freeze => p.HasType(PokemonType.Ice),
        StatusCondition.Paralyze => p.HasType(PokemonType.Ground) || p.HasType(PokemonType.Electric),
        StatusCondition.Burn => p.HasType(PokemonType.Fire),
        _ => false
    };

    /// <summary>
    /// The status room's Pokémon at work (<c>TryInflictRandomStatus</c>): the three tried in a drawn order, a condition
    /// drawn (35 badly poisoned, 25 frozen, 20 paralysed, 10 asleep, 10 burned in a hundred) until one of the first so many
    /// standing and well can be given it, then given to each of those its ability and type don't keep it off. The
    /// Pokémon that gives it is a Dusclops for a freeze, either for a burn, a Kirlia for the rest. Null when nobody stands
    /// well to be given anything.
    /// </summary>
    public static (StatusCondition Status, string Giver, int Given)? InflictStatus(Party team, int room, Func<int> next16)
    {
        var order = Shuffled(next16);
        int count = CountFor(room);
        var mons = team.Members;
        bool Well(Pokemon p) => p.Status == StatusCondition.None && p.CurrentHP != 0;
        StatusCondition status = StatusCondition.None, drawn;
        bool chosen = false;
        for (int guard = 0; !chosen && guard < 1000; guard++)
        {
            int rand = (next16() & 0xFFFF) % 100;
            drawn = rand < 35 ? StatusCondition.Toxic : rand < 60 ? StatusCondition.Freeze : rand < 80 ? StatusCondition.Paralyze
                : rand < 90 ? StatusCondition.Sleep : StatusCondition.Burn;
            if (drawn == status) continue;
            status = drawn;
            int seen = 0;
            foreach (int i in order)
            {
                if (i >= mons.Count) continue;
                if (Well(mons[i]))
                {
                    seen++;
                    if (!TypePrevents(mons[i], status)) { chosen = true; break; }
                }
                if (seen == count) break;
            }
            if (seen == 0) return null;
        }
        if (!chosen) return null;
        string giver = status switch
        {
            StatusCondition.Freeze => "Dusclops",
            StatusCondition.Burn => (next16() & 0xFFFF) % 2 != 0 ? "Dusclops" : "Kirlia",
            _ => "Kirlia"
        };
        int given = 0, met = 0;
        foreach (int i in order)
        {
            if (i >= mons.Count) continue;
            var p = mons[i];
            if (Well(p))
            {
                met++;
                if (!AbilityPrevents(p, status) && !TypePrevents(p, status))
                {
                    p.Status = status;
                    if (status == StatusCondition.Sleep) p.SleepTurns = 1 + (next16() & 0xFFFF) % 3;
                    if (status == StatusCondition.Toxic) p.ToxicCounter = 0;
                    given++;
                }
            }
            if (met == count) break;
        }
        return (status, giver, given);
    }

    /// <summary>
    /// So many of the three healed in a drawn order, each that could be (<c>TryHealMons</c>, <c>HealMon</c>): HP whole even
    /// for one that fainted, the condition gone and the PP restored. The number healed.
    /// </summary>
    public static int Heal(Party team, int count, Func<int> next16)
    {
        if (count <= 0) return 0;
        var order = Shuffled(next16);
        int healed = 0;
        foreach (int i in order)
        {
            if (i >= team.Count) continue;
            var p = team.Members[i];
            if (p.CurrentHP >= p.MaxHP && p.Status == StatusCondition.None && p.Moves.All(m => m.CurrentPP >= m.MaxPP)) continue;
            HealOne(p);
            healed++;
            if (healed == count) break;
        }
        return healed;
    }

    /// <summary>One Pokémon healed as the Pike heals (<c>HealMon</c>): HP, condition and PP, its stat stages left as they are.</summary>
    public static void HealOne(Pokemon p)
    {
        p.CurrentHP = p.MaxHP;
        p.Status = StatusCondition.None;
        p.SleepTurns = 0;
        p.ToxicCounter = 0;
        foreach (var m in p.Moves) m.RestorePP();
    }

    /// <summary>How many of the three each door heals before the Pike Queen (<c>sNumMonsToHealBeforePikeQueen</c>), by the door number the hint drew.</summary>
    public static readonly int[,] HealBeforeQueen = { { 2, 1, 0 }, { 2, 0, 1 }, { 1, 2, 0 }, { 1, 0, 2 }, { 0, 2, 1 }, { 0, 1, 2 } };

    /// <summary>A wild Pokémon of the Pike's (<c>sLvl50Mons</c>): its species, how far under level 50 it is, its four moves.</summary>
    public sealed record WildMon(string Species, int Under, string[] Moves);

    private static readonly WildMon Seviper = new("Seviper", 4, new[] { "Toxic", "Glare", "Body Slam", "Sludge Bomb" });
    private static readonly WildMon Milotic = new("Milotic", 4, new[] { "Toxic", "Hypnosis", "Body Slam", "Surf" });

    /// <summary>The three of each of the four tables, a table for each twenty rounds won in a row (<c>sLvl50Mons</c>, <c>GetBattlePikeWildMonHeaderId</c>).</summary>
    public static IReadOnlyList<IReadOnlyList<WildMon>> WildTables => new IReadOnlyList<WildMon>[]
    {
        new[] { Seviper, Milotic, new WildMon("Dusclops", 5, new[] { "Will-O-Wisp", "Mean Look", "Toxic", "Shadow Punch" }) },
        new[] { Seviper, Milotic, new WildMon("Electrode", 5, new[] { "Explosion", "Self-Destruct", "Thunder", "Toxic" }) },
        new[] { Seviper, Milotic, new WildMon("Breloom", 5, new[] { "Spore", "Stun Spore", "Poison Powder", "Hidden Power" }) },
        new[] { Seviper, Milotic, new WildMon("Wobbuffet", 5, new[] { "Counter", "Mirror Coat", "Safeguard", "Destiny Bond" }) }
    };

    /// <summary>The table for so many rooms passed in a row: the first to 280 rooms (twenty rounds), the second to 560, the third to 840, the last after.</summary>
    public static int WildTableFor(int streak) => streak <= 20 * BattlePikeChallenge.RoundLength ? 0 : streak <= 40 * BattlePikeChallenge.RoundLength ? 1
        : streak <= 60 * BattlePikeChallenge.RoundLength ? 2 : 3;

    /// <summary>A wild Pokémon of the round's table, one of its three drawn, at fifty less its own count with its own four moves.</summary>
    public static Pokemon WildPokemon(int streak, Func<int> next16)
    {
        var table = WildTables[WildTableFor(streak)];
        var mon = table[(next16() & 0xFFFF) % table.Count];
        var p = new Pokemon(PokemonDatabase.Get(mon.Species)!, FrontierRules.Level - mon.Under, new Random(next16() & 0xFFFF));
        p.Moves.Clear();
        foreach (string name in mon.Moves)
            if (MoveDatabase.Get(name) is { } data) p.Moves.Add(new Move(data));
        return p;
    }
}

/// <summary>
/// A round of the Battle Pike (plan 14 · B10; <c>docs/mechanics/rulings.md</c>, "The Battle Pike"): Emerald's facility as a
/// venue of the Battle Frontier's annex. Three Pokémon go in as copies at level 50 (<see cref="FrontierRules"/>) and stay
/// as each room leaves them: nothing heals them but a room that does. Fourteen rooms make a round; before each the
/// attendant senses what is behind one of three doors (<see cref="PikeRooms.Hint"/>), the player chooses a door and the
/// room behind it is drawn (<see cref="PikeRooms.Next"/>). A round is lost when all three have fainted. The streak counts
/// rooms passed; the Pike Queen waits behind every door of the 28th room in a row, and of the 140th and every 56th after
/// with her other team. Every draw is on the sixteen-bit draw the caller hands over. No drawing or input.
/// </summary>
public sealed class BattlePikeChallenge
{
    public const int TeamSize = 3, RoundLength = 14;

    /// <summary>Our own trainers of the Pike Queen's two teams (<c>Data/our-trainers.json</c>).</summary>
    public const string QueenId = "pike_queen_lucy", QueenGoldId = "pike_queen_lucy_gold";

    /// <summary>The places in the team of the three entered, in the order they were entered.</summary>
    public IReadOnlyList<int> Slots { get; }

    /// <summary>The three as the round has them, copies at level 50 that every room leaves as it found them.</summary>
    public Party Team { get; }

    /// <summary>The rooms passed this round (0 to 14).</summary>
    public int Room { get; private set; }

    /// <summary>The rooms passed in a row before this round.</summary>
    public int StreakBefore { get; }

    /// <summary>The door the attendant's hint is of (0 to 2; 0 to 5 before the Pike Queen, a row of <see cref="PikeRooms.HealBeforeQueen"/>), and the room behind it.</summary>
    public int HintedDoor { get; private set; }
    public PikeRoom HintedRoom { get; private set; }

    /// <summary>The healing rooms are off: the three left the last room whole (<c>pikeHealingRoomsDisabled</c>).</summary>
    public bool HealingOff { get; private set; }

    /// <summary>The door chosen and the room behind it, while the player is in it.</summary>
    public int Door { get; private set; }
    public PikeRoom? Current { get; private set; }

    /// <summary>The trainers of the room (one, or two in a double battle), the wild Pokémon of a wild room, and what a status room or a healing room did.</summary>
    public List<FrontierOpponent> Foes { get; } = new();
    public Pokemon? Wild { get; private set; }
    public (StatusCondition Status, string Giver, int Given)? Inflicted { get; private set; }
    public int Healed { get; private set; }

    // The Frontier's trainers met this round, none twice (PrepareOneTrainer, PrepareTwoTrainers)
    private readonly HashSet<int> met = new();

    private BattlePikeChallenge(IReadOnlyList<int> slots, Party team, int streakBefore)
    {
        Slots = slots;
        Team = team;
        StreakBefore = streakBefore;
    }

    /// <summary>The Pike's part of the records: the rooms passed in a row and the most ever, and the rounds won.</summary>
    public static VenueRecord RecordOf(FrontierRecords records) => records.Pike;

    /// <summary>A round begun with the three entered: one walked out on ends the streak first.</summary>
    public static BattlePikeChallenge Begin(FrontierRecords records, Party party, IReadOnlyList<int> slots)
    {
        var record = records.Pike;
        if (record.InProgress) record.Streak = 0;
        record.InProgress = true;
        return new BattlePikeChallenge(slots.ToList(), FrontierRules.TeamFor(party, slots), record.Streak);
    }

    /// <summary>The room to come, 1 to 14.</summary>
    public int Next => Room + 1;

    /// <summary>The rooms passed in a row with the one to come.</summary>
    public int StreakAt(FrontierRecords records) => records.Pike.Streak + 1;

    /// <summary>Which of her two teams the Pike Queen brings to the room to come: none, silver (the 28th room in a row) or gold (the 140th and every 56th after).</summary>
    public static int QueenFor(int room) => room == 2 * RoundLength ? 1 : room == 10 * RoundLength || (room > 10 * RoundLength && (room - 10 * RoundLength) % (4 * RoundLength) == 0) ? 2 : 0;

    /// <summary>
    /// The attendant's hint for the room to come (<see cref="PikeRooms.Hint"/>): the hint word to say. It is drawn once a
    /// room; asked again before the door is chosen, it is the same.
    /// </summary>
    public PikeHint Hint(FrontierRecords records, Func<int> next16)
    {
        if (hintedFor != Next)
        {
            Current = null;
            (HintedDoor, HintedRoom) = PikeRooms.Hint(QueenFor(StreakAt(records)) != 0, HealingOff, Team, Next, next16);
            hintedFor = Next;
        }
        return PikeRooms.HintOf(HintedRoom);
    }

    // The room the hint was drawn for
    private int hintedFor;

    /// <summary>
    /// A door chosen (0 to 2): the room behind it is drawn and made ready (its trainers drawn from the Tower's rows, none
    /// met before this round; its wild Pokémon; a status given; the healing done) and named.
    /// </summary>
    public PikeRoom Enter(FrontierRecords records, int door, Func<int> next16)
    {
        Hint(records, next16);
        Door = Math.Clamp(door, 0, PikeRooms.Doors - 1);
        var room = PikeRooms.Next(Door, HintedDoor, HintedRoom, HealingOff, Team, Next, next16);
        Current = room;
        Foes.Clear();
        Wild = null;
        Inflicted = null;
        Healed = 0;
        int rounds = records.Pike.Streak / RoundLength;
        switch (room)
        {
            case PikeRoom.SingleBattle:
                Foes.Add(DrawTrainers(1, Math.Min(7, rounds), 3, next16)[0]);
                break;
            case PikeRoom.HardBattle:
                // The seventh battle's trainer (PrepareOneTrainer(TRUE)): a row further on
                Foes.Add(DrawTrainers(1, Math.Min(7, rounds + 1), 3, next16)[0]);
                break;
            case PikeRoom.DoubleBattle:
                Foes.AddRange(DrawTrainers(2, Math.Min(7, rounds), 2, next16));
                break;
            case PikeRoom.WildPokemon:
                Wild = PikeRooms.WildPokemon(records.Pike.Streak, next16);
                break;
            case PikeRoom.Status:
                Inflicted = PikeRooms.InflictStatus(Team, Next, next16);
                break;
            case PikeRoom.HealFull:
                foreach (var p in Team.Members) PikeRooms.HealOne(p);
                Healed = Team.Count;
                break;
            case PikeRoom.HealPart:
                Healed = PikeRooms.Heal(Team, (next16() & 0xFFFF) % 2 + 1, next16);
                break;
            case PikeRoom.Brain:
                Healed = PikeRooms.Heal(Team, PikeRooms.HealBeforeQueen[Math.Clamp(HintedDoor, 0, 5), Door], next16);
                break;
        }
        return room;
    }

    // Trainers of a Tower row the round hasn't met, each with so many of their own
    private List<FrontierOpponent> DrawTrainers(int count, int row, int each, Func<int> next16)
    {
        int[] range = FrontierDatabase.TowerRooms[row];
        var chosen = new List<int>();
        for (int guard = 0; chosen.Count < count && guard < 1000; guard++)
        {
            int trainer = range[0] + (next16() & 0xFFFF) % (range[1] - range[0] + 1);
            if (met.Contains(trainer) || chosen.Contains(trainer)) continue;
            chosen.Add(trainer);
        }
        foreach (int t in chosen) met.Add(t);
        return count == 1
            ? FrontierTrainers.ForTrainers(each, chosen[0], chosen[0], false, next16)
            : FrontierTrainers.ForTrainers(each * 2, chosen[0], chosen[^1], true, next16);
    }

    /// <summary>The room's trainers as the battle takes them, their Pokémon as copies.</summary>
    public List<Trainer> FoesAsTrainers() => Foes.Select(Copied).ToList();

    /// <summary>A trainer of the Frontier as a battle takes them, their Pokémon as copies.</summary>
    internal static Trainer Copied(FrontierOpponent o)
    {
        var party = new Party();
        foreach (var p in o.Pokemon) party.Add(FrontierRules.ForBattle(p));
        return new Trainer
        {
            Id = "frontier_" + o.Record.Id, Name = o.Record.Name, TrainerClass = o.Record.Class, PrizeMoney = 0,
            Ai = FrontierTrainers.Mind, Look = FrontierTrainers.LookOf(o.Record), Party = party
        };
    }

    /// <summary>The Pike Queen with the team the room calls for, at level 50.</summary>
    public Trainer Queen(FrontierRecords records)
    {
        var record = TrainerDatabase.Get(QueenFor(StreakAt(records)) == 2 ? QueenGoldId : QueenId)
            ?? throw new InvalidOperationException("The Pike Queen has no team in our-trainers.json.");
        return OurBrain(record, "lucy");
    }

    /// <summary>A Brain of ours as a battle takes them (<see cref="BattleSubway.Boss"/>'s way): their own team of our data, at level 50.</summary>
    internal static Trainer OurBrain(TrainerRecord record, string look)
    {
        var trainer = new Trainer
        {
            Id = record.Id, Name = record.Name, TrainerClass = record.Class, PrizeMoney = 0,
            Ai = FrontierTrainers.Mind, Look = CharacterStyles.Default.CharacterFor(look)
        };
        foreach (var m in record.Party) trainer.Party.Add(TrainerDatabase.Build(m));
        return trainer;
    }

    /// <summary>All three of the round have fainted: the round is lost.</summary>
    public bool AllDown => Team.Members.All(p => p.IsFainted);

    /// <summary>
    /// The room passed (<c>BattleFrontier_BattlePikeRoomNormal_EventScript_Exit</c>): the streak lengthens, the healing
    /// rooms are off for the next if the three leave it whole, and the round is won with the fourteenth.
    /// </summary>
    public bool Passed(FrontierRecords records)
    {
        var record = records.Pike;
        record.Streak = Math.Min(FrontierRecords.Most, record.Streak + 1);
        record.Best = Math.Max(record.Best, record.Streak);
        HealingOff = PikeRooms.FullHealth(Team);
        Room++;
        Current = null;
        return Room >= RoundLength;
    }

    /// <summary>The round is over, won or lost: the record is written, a loss ending the streak.</summary>
    public void Finish(FrontierRecords records, bool won)
    {
        var record = records.Pike;
        record.InProgress = false;
        if (!won)
        {
            record.Streak = 0;
            return;
        }
        record.Rounds = Math.Min(FrontierRecords.Most, record.Rounds + 1);
    }

    /// <summary>The Battle Points a round won earns: the Dome's rule of ours, by the rounds won in a row before it.</summary>
    public int PointsEarned => Room >= RoundLength ? BattleDomeChallenge.PointsFor(StreakBefore / RoundLength) : 0;
}
