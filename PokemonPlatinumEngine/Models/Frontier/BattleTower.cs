using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Frontier;

/// <summary>
/// The ways the Battle Tower is taken, by the original's numbers (<c>BATTLE_TOWER_MODE_…</c>): alone with one
/// Pokémon out or with two, beside one of the Tower's own partners, beside a friend over a link, and against teams
/// from Wi-Fi. The last two need other players and are not held (decision 06 · 7).
/// </summary>
public enum TowerMode
{
    Single,
    Double,
    Multi,
    LinkMulti,
    Wifi
}

/// <summary>
/// Whoever battles beside the player in the Multi Battle Room (<c>BattleTowerPartnerData</c> and what is kept with
/// it): which of the five, the sets and personalities of their two Pokémon, the number those carry, and whether they
/// were handed the reserved items. Kept so that the same team comes back when a challenge is taken up again.
/// </summary>
public sealed class TowerPartner
{
    /// <summary>0 Cheryl, 1 Mira, 2 Riley, 3 Marley, 4 Buck (<c>BT_PARTNER_…</c>).</summary>
    public int Id { get; set; }
    public uint OtId { get; set; }
    public List<int> Sets { get; set; } = new();
    public List<uint> Personalities { get; set; } = new();
    public bool ReservedItems { get; set; }
}

/// <summary>
/// A Battle Tower challenge under way (plan 06 · R18a): the original's <c>BattleTower</c> and the functions that
/// work on it (<c>src/unk_02049D08.c</c>, <c>src/unk_0204AEE8.c</c>, <c>src/overlay104/ov104_0223A0C4.c</c>,
/// <c>ov104_022394A4.c</c>, <c>src/unk_0206B9D8.c</c>). Seven trainers are met one after another with the Pokémon
/// chosen at the counter; a set of seven won goes on to the next room, where the trainers are stronger; the streak
/// runs until a battle is lost or the challenge is given up. Every draw is on the Tower's own generator, a number
/// kept with the records, so a room's trainers and their teams are the same however often it is come back to. No
/// drawing or input.
/// </summary>
public sealed class BattleTowerChallenge
{
    /// <summary>The trainers of one set (<c>BT_OPPONENTS_COUNT</c>).</summary>
    public const int Opponents = 7;

    /// <summary>The Tower's five partners, in <see cref="TowerPartner.Id"/>'s order.</summary>
    public static readonly string[] PartnerTrainers =
        { "trainer_cheryl_cheryl", "trainer_mira_mira", "trainer_riley_riley", "trainer_marley_marley", "trainer_buck_buck" };

    /// <summary>The flag each partner waits for before they are in the Battle Salon: the story has them met first (<c>BattleTowerBattleSalon_OnTransition</c>).</summary>
    public static readonly string[] PartnerFlags =
    {
        "FLAG_TRAVELED_WITH_CHERYL", "FLAG_TRAVELED_WITH_MIRA", "FLAG_RECEIVED_RIOLU_EGG_FROM_RILEY", "FLAG_TRAVELED_WITH_MARLEY", "FLAG_BUCK_LEFT_BATTLEGROUND"
    };

    /// <summary>The flag that keeps each out of the Battle Salon (their own of the original's room).</summary>
    public static readonly string[] SalonFlags =
    {
        "FLAG_HIDE_BATTLE_SALON_CHERYL", "FLAG_HIDE_BATTLE_SALON_MIRA", "FLAG_HIDE_BATTLE_SALON_RILEY", "FLAG_HIDE_BATTLE_SALON_MARLEY", "FLAG_HIDE_BATTLE_SALON_BUCK"
    };

    public static readonly string[] PartnerNames = { "Cheryl", "Mira", "Riley", "Marley", "Buck" };

    /// <summary>How each stands in the field and in battle: their own look where this game has one, and a stand-in where it hasn't yet (plan 11).</summary>
    public static readonly string[] PartnerLooks = { "Cheryl", "Mira", "Clerk", "Lass", "Youngster" };

    public TowerMode Mode { get; }

    /// <summary>How many Pokémon are entered: three, four in the Double Battle Room, two beside a partner.</summary>
    public int PartySize => SizeFor(Mode);

    /// <summary>The trainer of the set about to be met, from 1 (<c>nextOpponentNum</c>).</summary>
    public int NextOpponent { get; private set; } = 1;

    /// <summary>The battles won in this set (<c>unk_0D</c>).</summary>
    public int Wins { get; private set; }

    /// <summary>The places in the team of the Pokémon entered, in the order chosen (<c>unk_2A</c>).</summary>
    public List<int> Slots { get; private set; } = new();

    /// <summary>The set's trainers by their numbers, drawn as the challenge begins: seven, or seven pairs beside a partner.</summary>
    public List<int> Trainers { get; private set; } = new();

    /// <summary>The state of the Tower's generator (<c>unk_08</c>).</summary>
    public uint Seed { get; private set; }

    /// <summary>The streak the challenge goes on from (<c>unk_1A</c>), and the room it is fought in (<c>roomNum</c>): the sets of seven won so far.</summary>
    public int StreakBefore { get; private set; }
    public int Room { get; private set; }

    /// <summary>The Tower Tycoon was beaten in this challenge: for the silver print, or for the gold one.</summary>
    public FrontierPrint BeatPalmer { get; set; }

    /// <summary>The streak has reached fifty in the Double or the Multi Battle Room (<c>unk_10_4</c>): the Pokémon entered are due their ribbon.</summary>
    public bool StreakRibbonDue { get; private set; }

    /// <summary>Whoever battles beside the player, once chosen in the Battle Salon.</summary>
    public TowerPartner? Partner { get; private set; }

    /// <summary>The five the Battle Salon offers, each with the team made for this challenge (<c>partnersDataDTO</c>); empty until they are made.</summary>
    public List<TowerPartner> Candidates { get; } = new();

    private bool defeatedSeven;
    // The species and items of the Pokémon entered (unk_2E, unk_36): a partner's team keeps clear of them
    private List<string> ownSpecies = new();
    private List<string?> ownItems = new();

    private BattleTowerChallenge(TowerMode mode) => Mode = mode;

    /// <summary><c>BattleTower_GetPartySizeForChallengeMode</c>.</summary>
    public static int SizeFor(TowerMode mode) => mode switch
    {
        TowerMode.Double => 4,
        TowerMode.Multi or TowerMode.LinkMulti => 2,
        _ => 3
    };

    public static FrontierStat RecordStat(TowerMode mode) => (FrontierStat)((int)mode * 2);
    public static FrontierStat LatestStat(TowerMode mode) => (FrontierStat)((int)mode * 2 + 1);

    /// <summary>The way of taking the Tower as a bit of <see cref="FrontierRecords.Running"/>.</summary>
    private static bool Running(FrontierRecords records, TowerMode mode) => records.IsRunning(FrontierFacility.Tower, (int)mode);

    // ------------------------------------------------------------------ beginning

    /// <summary>
    /// <c>BattleTower_Init</c> for a new challenge: nothing chosen yet, the first trainer next, and where a streak
    /// in this mode is still running, the wins it has and the room it has reached. Whatever challenge was put down
    /// is gone (<c>WifiBattleTowerSave_Init</c>).
    /// </summary>
    public static BattleTowerChallenge Begin(FrontierRecords records, TowerMode mode)
    {
        var challenge = new BattleTowerChallenge(mode);
        records.Tower.Paused = null;
        challenge.TakeUpStreak(records);
        return challenge;
    }

    /// <summary><c>BattleTower_Init</c> for a challenge taken up again: as it was put down (<see cref="Pause"/>); null when none was.</summary>
    public static BattleTowerChallenge? Resume(FrontierRecords records)
    {
        if (records.Tower.Paused is not { } paused) return null;
        var challenge = new BattleTowerChallenge(paused.Mode)
        {
            NextOpponent = paused.NextOpponent,
            Wins = paused.NextOpponent - 1,
            Slots = paused.Slots.ToList(),
            Trainers = paused.Trainers.ToList(),
            Seed = paused.Seed,
            Partner = paused.Partner
        };
        records.Tower.Paused = null;
        challenge.TakeUpStreak(records);
        return challenge;
    }

    private void TakeUpStreak(FrontierRecords records)
    {
        if (!Running(records, Mode)) return;
        StreakBefore = records.Get(LatestStat(Mode));
        Room = records.Tower.Rooms[(int)Mode];
    }

    /// <summary>The Pokémon chosen at the counter (<c>sub_0204A378</c>): their places in the team, in the order chosen.</summary>
    public void Enter(Party party, IEnumerable<int> slots)
    {
        Slots = slots.ToList();
        Note(party);
    }

    private void Note(Party party)
    {
        var entered = Entered(party);
        ownSpecies = entered.Select(p => p.Species.Name).ToList();
        ownItems = entered.Select(p => p.HeldItem?.Name).ToList();
    }

    /// <summary>The Pokémon entered, as they are in the team.</summary>
    public List<Pokemon> Entered(Party party) => Slots.Where(s => s >= 0 && s < party.Count).Select(s => party.Members[s]).ToList();

    /// <summary>
    /// Whether the Pokémon entered can still go on after a pause: each still in its place, allowed in, and no two alike
    /// or holding the same item. The original can't be walked away from between a pause and its end; this game can.
    /// </summary>
    public bool StillValid(Party party) =>
        Slots.Count == PartySize && Slots.Distinct().Count() == PartySize && Slots.All(s => s >= 0 && s < party.Count && FrontierRules.MayEnter(party.Members[s]))
        && FrontierRules.WhyNot(Entered(party)) == FrontierRefusal.None;

    // ------------------------------------------------------------------ the Tower's generator

    /// <summary><c>sub_0206BFF0</c>: the next state of the Tower's generator.</summary>
    public static uint Step(uint state) => unchecked(state * 48828125u + 1u);

    /// <summary><c>sub_0206BFFC</c>: the next state of the number a new streak starts from.</summary>
    public static uint NextStreak(uint state) => unchecked(state * 1566083941u + 1u);

    /// <summary><c>BattleTower_GetRandom</c>: the generator moves on, and its state over 65,535 is the draw, in sixteen bits.</summary>
    public int Random()
    {
        Seed = Step(Seed);
        return (int)((Seed / 65535u) & 0xFFFF);
    }

    /// <summary>
    /// <c>sub_0204ABF4</c>, as the challenge goes up from the lobby: a new streak moves the kept number on and starts
    /// from it (<c>sub_0206C02C</c>); a streak going on starts from the kept number moved on twenty-four times for
    /// every room come through (<c>sub_0206C068</c>), so that each room of a streak has its own trainers.
    /// </summary>
    public void TakeSeed(FrontierRecords records)
    {
        if (!Running(records, Mode))
        {
            records.Tower.RngState = NextStreak(records.Tower.RngState);
            Seed = Step(records.Tower.RngState);
            return;
        }
        uint state = Step(records.Tower.RngState);
        for (int i = 0, n = records.Tower.Rooms[(int)Mode] * 24; i < n; i++) state = Step(state);
        Seed = state;
    }

    // ------------------------------------------------------------------ the set's trainers

    /// <summary>
    /// <c>BattleTower_GetTrainerIDForRoomAndOpponentNum</c>: who stands in a room at a place of the seven. In the
    /// Single Battle Room the seventh of the third room is the Tower Tycoon and the seventh of the seventh the Tower
    /// Tycoon again; otherwise the trainer is drawn from the room's row, the seventh from the row of bosses, and
    /// every room from the eighth on from the last row. The room is counted in a byte, as the original passes it.
    /// </summary>
    public int TrainerFor(int room, int place)
    {
        room &= 0xFF;
        if (Mode == TowerMode.Single && place == Opponents - 1)
        {
            var (silver, gold) = FrontierBrains.Trainers(FrontierFacility.Tower);
            if (room == 2) return silver;
            if (room == 6) return gold;
        }
        int[] row = room >= 7 ? FrontierDatabase.TowerRooms[7]
            : place == Opponents - 1 ? FrontierDatabase.TowerBossRooms[room] : FrontierDatabase.TowerRooms[room];
        return row[0] + Random() % (row[1] - row[0] + 1);
    }

    /// <summary>
    /// <c>sub_0204A4C8</c>: the set's trainers are drawn, none twice: seven, or beside a partner fourteen, two for
    /// each battle.
    /// </summary>
    public void DrawTrainers()
    {
        bool pairs = Mode is TowerMode.Multi or TowerMode.LinkMulti;
        Trainers = new List<int>();
        for (int i = 0, n = pairs ? Opponents * 2 : Opponents; i < n; i++)
        {
            int trainer;
            do trainer = TrainerFor(Room, pairs ? i / 2 : i);
            while (Trainers.Contains(trainer));
            Trainers.Add(trainer);
        }
    }

    /// <summary>
    /// <c>BattleTower_CreateOpponentParties</c>: the team, or beside a partner the two teams, of the battle about to
    /// be fought, drawn afresh each time it is come to. The second trainer's Pokémon keep clear of the first's.
    /// </summary>
    public List<FrontierOpponent> MakeOpponents()
    {
        if (Mode is TowerMode.Multi or TowerMode.LinkMulti)
        {
            var first = MakeTeam(Trainers[(NextOpponent - 1) * 2], PartySize, null, null);
            var second = MakeTeam(Trainers[(NextOpponent - 1) * 2 + 1], PartySize,
                first.Pokemon.Select(p => p.Species.Name).ToList(), first.Pokemon.Select(p => p.HeldItem?.Name).ToList());
            return new List<FrontierOpponent> { first, second };
        }
        return new List<FrontierOpponent> { MakeTeam(Trainers[NextOpponent - 1], PartySize, null, null) };
    }

    /// <summary>
    /// <c>BattleTower_CreateRandomTrainerParty</c>: sets are drawn until the team is full, one of a species already
    /// in it (or in <paramref name="species"/>) drawn again, and one whose item is held already (or is among
    /// <paramref name="items"/>) drawn again too for the first fifty such, after which items are no longer looked at
    /// and the team is handed the reserved four. Then the number the Pokémon carry, and a personality for each.
    /// </summary>
    private FrontierOpponent MakeTeam(int trainer, int size, IReadOnlyList<string>? species, IReadOnlyList<string?>? items)
    {
        var record = FrontierDatabase.Trainer(trainer);
        var sets = new List<int>();
        int refused = 0;
        while (sets.Count < size)
        {
            int number = record.Sets[Random() % record.Sets.Count];
            var set = FrontierDatabase.Set(number);
            if (sets.Select(FrontierDatabase.Set).Any(s => s.Species == set.Species)) continue;
            if (species != null && species.Contains(set.Species)) continue;
            if (refused < 50)
            {
                if (sets.Select(FrontierDatabase.Set).Any(s => s.Item != null && s.Item == set.Item)) { refused++; continue; }
                if (items != null && set.Item != null && items.Contains(set.Item)) { refused++; continue; }
            }
            sets.Add(number);
        }
        uint otId = (uint)Random() | (uint)Random() << 16;
        bool reserved = refused >= 50;
        return Build(trainer, sets, otId, null, reserved);
    }

    // BattleTower_CopySetToPokemonDataDTO for each of a team: a personality drawn for it, or the one kept
    private FrontierOpponent Build(int trainer, List<int> sets, uint otId, IReadOnlyList<uint>? personalities, bool reserved)
    {
        int ivs = FrontierTrainers.IvsFor(trainer);
        var pokemon = new List<Pokemon>();
        var drawn = new List<uint>();
        for (int i = 0; i < sets.Count; i++)
        {
            var set = FrontierDatabase.Set(sets[i]);
            uint personality = personalities != null ? personalities[i] : FrontierTrainers.PersonalityFor(set.Nature, otId, Random);
            drawn.Add(personality);
            pokemon.Add(FrontierTrainers.Build(set, personality, ivs, FrontierRules.Level, reserved ? FrontierTrainers.ReservedItems[i % 4] : null));
        }
        return new FrontierOpponent { Number = trainer, Sets = sets, Pokemon = pokemon, OtId = otId, Personalities = drawn, ReservedItems = reserved };
    }

    // ------------------------------------------------------------------ the partners

    /// <summary>
    /// <c>sub_0204A97C</c>, as the Battle Salon is come into: each of the five is given two Pokémon for this
    /// challenge, clear of the species and the items of the two the player entered.
    /// </summary>
    public void MakePartners(Party party)
    {
        Note(party);
        Candidates.Clear();
        for (int id = 0; id < PartnerTrainers.Length; id++)
        {
            var team = MakeTeam(FrontierDatabase.NumberOf(PartnerTrainers[id]), PartySize, ownSpecies, ownItems);
            Candidates.Add(new TowerPartner { Id = id, OtId = team.OtId, Sets = team.Sets, Personalities = team.Personalities, ReservedItems = team.ReservedItems });
        }
    }

    /// <summary><c>BT_FUNC_SET_PARTNER_ID</c>.</summary>
    public void ChoosePartner(int id) => Partner = Candidates.FirstOrDefault(c => c.Id == id) ?? Partner;

    /// <summary>A partner's team as it was made (<c>sub_0204B404</c>): the same sets, number and personalities build the same Pokémon.</summary>
    public FrontierOpponent TeamOf(TowerPartner partner) =>
        Build(FrontierDatabase.NumberOf(PartnerTrainers[partner.Id]), partner.Sets, partner.OtId, partner.Personalities, partner.ReservedItems);

    /// <summary>What a partner says of one of their two (<c>GetBattleTowerPartnerSpeciesAndMove</c>): its species and the first of its moves.</summary>
    public static (string Species, string Move) Tells(TowerPartner partner, int which)
    {
        var set = FrontierDatabase.Set(partner.Sets[which]);
        return (set.Species, set.Moves.FirstOrDefault() ?? "");
    }

    // ------------------------------------------------------------------ battle by battle

    /// <summary>The streak as it stands (<c>ov104_022395D8</c>): what it was, and the battles won since.</summary>
    public int Streak => Math.Min(0xFFFF, StreakBefore + Wins);

    /// <summary>
    /// Whether the battle about to be fought is the Tower Tycoon's (the battle room's script: a Single streak of 20
    /// or of 48 as its seventh trainer is come to).
    /// </summary>
    public FrontierPrint PalmerNext => Mode != TowerMode.Single ? FrontierPrint.None : Streak switch
    {
        20 => FrontierPrint.Silver,
        48 => FrontierPrint.Gold,
        _ => FrontierPrint.None
    };

    /// <summary><c>ov104_022395B4</c>, after a battle won: one more win, the generator moved on, the next trainer.</summary>
    public int Won()
    {
        Wins++;
        Seed = Step(Seed);
        return ++NextOpponent;
    }

    /// <summary><c>BattleTower_HasDefeatedSevenTrainers</c>.</summary>
    public bool DefeatedSeven
    {
        get
        {
            if (NextOpponent > Opponents) defeatedSeven = true;
            return defeatedSeven;
        }
    }

    // ------------------------------------------------------------------ the end of a challenge

    /// <summary>
    /// <c>BattleTower_UpdateGameRecords</c>, when a battle is lost or the challenge given up: the streak ends where
    /// it stands. The record keeps it if it is the longest, the mode's streak is no longer running, and its room is
    /// the first again.
    /// </summary>
    public void EndLost(FrontierRecords records)
    {
        records.SetIfBetter(RecordStat(Mode), StreakBefore + Wins);
        records.Set(LatestStat(Mode), StreakBefore + Wins);
        records.SetRunning(FrontierFacility.Tower, (int)Mode, false);
        records.CountVictories(FrontierFacility.Tower, Wins);
        records.Tower.Rooms[(int)Mode] = 0;
        records.TowerChallenges++;
        CheckStreakRibbon();
    }

    /// <summary>
    /// <c>BattleTower_UpdateGameRecordsAndJournal</c>, when all seven are beaten: the streak goes on. It is kept as the
    /// mode's latest and as its record if it is the longest, it is running, and the next set is fought a room higher.
    /// The caller adds <see cref="SetScore"/> to the trainer's score.
    /// </summary>
    public void EndWon(FrontierRecords records)
    {
        int streak = records.Set(LatestStat(Mode), StreakBefore + Wins);
        records.SetRunning(FrontierFacility.Tower, (int)Mode, true);
        records.SetIfBetter(RecordStat(Mode), streak);
        records.CountVictories(FrontierFacility.Tower, Opponents);
        if (records.Tower.Rooms[(int)Mode] < 65534) records.Tower.Rooms[(int)Mode]++;
        records.TowerChallenges++;
        CheckStreakRibbon();
    }

    /// <summary>What a set of seven won adds to the Trainer Card's score (<c>TRAINER_SCORE_EVENT_UNK_14</c>).</summary>
    public const int SetScore = 7;

    // sub_0204ACC8
    private void CheckStreakRibbon()
    {
        if (Mode is TowerMode.Double or TowerMode.Multi or TowerMode.LinkMulti && StreakBefore + Wins >= 50) StreakRibbonDue = true;
    }

    /// <summary>The Battle Points a set of seven earns by the room it leaves the streak in (<c>soloBattlePoints</c>), and for beating the Tower Tycoon.</summary>
    public static readonly int[] PointsByRoom = { 0, 3, 3, 4, 4, 5, 5, 7 };
    public const int PalmerPoints = 20;

    /// <summary>
    /// <c>BattleTower_GiveBattlePointsReward</c>, after <see cref="EndWon"/>: twenty for a set that ended with the
    /// Tower Tycoon beaten, else by the rooms come through, seven from the seventh on. The points are the player's.
    /// </summary>
    public int GivePoints(FrontierRecords records)
    {
        int room = records.Tower.Rooms[(int)Mode];
        int points = BeatPalmer != FrontierPrint.None ? PalmerPoints : room >= 7 ? 7 : PointsByRoom[room];
        records.GivePoints(points);
        return points;
    }

    /// <summary>
    /// The ribbon for beating the Tower Tycoon (<c>sub_0204AB68</c>): the Ability Ribbon the first time and the Great
    /// Ability Ribbon the second, for each Pokémon entered that hasn't it. True when any was given.
    /// </summary>
    public bool GivePalmerRibbon(Party party)
    {
        if (Mode != TowerMode.Single || BeatPalmer == FrontierPrint.None) return false;
        return GiveRibbon(party, BeatPalmer == FrontierPrint.Silver ? "RIBBON_ABILITY" : "RIBBON_GREAT_ABILITY");
    }

    /// <summary>
    /// The ribbon for a streak of fifty (<c>sub_0204ABA0</c>): the Double Ability Ribbon in the Double Battle Room,
    /// the Multi Ability Ribbon beside one of the Tower's partners. True when any was given.
    /// </summary>
    public bool GiveStreakRibbon(Party party)
    {
        if (!StreakRibbonDue) return false;
        string? ribbon = Mode switch
        {
            TowerMode.Double => "RIBBON_DOUBLE_ABILITY",
            TowerMode.Multi => "RIBBON_MULTI_ABILITY",
            TowerMode.LinkMulti => "RIBBON_PAIR_ABILITY",
            TowerMode.Wifi => "RIBBON_WORLD_ABILITY",
            _ => null
        };
        return ribbon != null && GiveRibbon(party, ribbon);
    }

    // sub_0204AC54
    private bool GiveRibbon(Party party, string ribbon)
    {
        bool any = false;
        foreach (var p in Entered(party)) any |= p.Ribbons.Add(ribbon);
        return any;
    }

    /// <summary><c>sub_0204A8C8</c>: the challenge is put down between two battles, to be taken up where it stands.</summary>
    public void Pause(FrontierRecords records) => records.Tower.Paused = new TowerPause
    {
        Mode = Mode,
        NextOpponent = NextOpponent,
        Slots = Slots.ToList(),
        Trainers = Trainers.ToList(),
        Seed = Seed,
        Partner = Mode == TowerMode.Multi ? Partner : null
    };

    /// <summary>
    /// <c>sub_0204A064</c>: a challenge was left without being put down (the game was closed in the middle of it).
    /// The mode's streak is over: no longer running, its room the first again, its latest streak nought, and the
    /// number a new streak starts from moved on.
    /// </summary>
    public static void Forfeit(FrontierRecords records, TowerMode mode)
    {
        records.SetRunning(FrontierFacility.Tower, (int)mode, false);
        records.Tower.Rooms[(int)mode] = 0;
        records.Set(LatestStat(mode), 0);
        records.Tower.Paused = null;
        records.Tower.RngState = NextStreak(records.Tower.RngState);
    }

    // ------------------------------------------------------------------ the lobby's trophies

    /// <summary>The Single streaks the lobby's trophies are for, and the Underground's goods they are.</summary>
    public static readonly (int Streak, string Good)[] Trophies = { (20, "Bronze Trophy"), (50, "Silver Trophy"), (100, "Gold Trophy") };

    /// <summary>
    /// <c>sub_0206BDBC</c>: the trophy the Single record has earned and the lobby hasn't handed over, one at a time
    /// from the bronze: 1, 2 or 3 as it is given (it is marked given), 4 when the Underground's PC has no room for it
    /// (which is remembered), 0 when none is due.
    /// </summary>
    public static int TrophyDue(FrontierRecords records, Func<bool> roomInPc)
    {
        int streak = records.Get(FrontierStat.TowerRecordStreakSingle);
        var tower = records.Tower;
        if (streak < 20 || (tower.BronzeTrophy && tower.SilverTrophy && tower.GoldTrophy)) return 0;
        if (!tower.BronzeTrophy)
        {
            if (roomInPc()) { tower.BronzeTrophy = true; return 1; }
            tower.BronzeWaits = true;
            return 4;
        }
        if (streak < 50) return 0;
        if (!tower.SilverTrophy)
        {
            if (roomInPc()) { tower.SilverTrophy = true; return 2; }
            tower.SilverWaits = true;
            return 4;
        }
        if (streak < 100 || tower.GoldTrophy) return 0;
        if (roomInPc()) { tower.GoldTrophy = true; return 3; }
        tower.GoldWaits = true;
        return 4;
    }

    /// <summary>
    /// <c>sub_0206BF04</c>: whether someone waits in the lobby with a trophy: 0 for nobody; 1, 2 or 3 for one not yet
    /// offered; 4, 5 or 6 for one there was no room for.
    /// </summary>
    public static int TrophyWaiting(FrontierRecords records)
    {
        int streak = records.Get(FrontierStat.TowerRecordStreakSingle);
        var tower = records.Tower;
        if (streak < 20 || (tower.BronzeTrophy && tower.SilverTrophy && tower.GoldTrophy)) return 0;
        if (!tower.BronzeTrophy) return tower.BronzeWaits ? 4 : 1;
        if (streak < 50) return 0;
        if (!tower.SilverTrophy) return tower.SilverWaits ? 5 : 2;
        if (streak < 100 || tower.GoldTrophy) return 0;
        return tower.GoldWaits ? 6 : 3;
    }
}
