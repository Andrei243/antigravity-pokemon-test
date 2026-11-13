using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Frontier;

/// <summary>
/// Galar's Battle Tower in Rose Tower (plan 33 · GL7, part b; plan 33 · decision 5; the rulings' "Galar's Battle Tower
/// ladder"): a ranked ladder with the Frontier's rules lent, as the Battle Subway and the Black Tower lend them
/// (<see cref="BattleSubway"/>, <see cref="BlackTower"/>). Three of the player's Pokémon go in as copies at level 50
/// (<see cref="FrontierRules"/>), whole again before every battle, against trainers of the Frontier's table drawn by the
/// Tower's rooms, a room higher every rank or so. Ten ranks of ours: three wins at a rank take the player up to the next,
/// a loss ends the run and from the fifth rank on takes a win back (never a rank), and at the tenth, the Master Rank, the
/// third win is Leon's battle (his team of ours, its last three at level 50). A run and the ladder live in the story's own
/// variables (<see cref="Vars"/>), so nothing new is saved and no <see cref="FrontierFacility"/> or stat is added. No
/// drawing or input; every draw is on the sixteen-bit draw the caller hands over.
/// </summary>
public static class GalarTower
{
    /// <summary>The ranks of the ladder, the wins that take a rank up, and how many Pokémon go in.</summary>
    public const int Ranks = 10, WinsToRise = 3, PartySize = 3;

    /// <summary>The first rank (from 0) at which a loss takes a win back.</summary>
    public const int LossCostsFrom = 4;

    /// <summary>Leon's team of ours (plan 33 · GL7), whose last three he brings to the Master Rank's third win.</summary>
    public const string LeonId = "galar_leon_rematch";

    /// <summary>The story's variables the ladder keeps.</summary>
    public static class Vars
    {
        /// <summary>0 while no run is under way, 1 while one is.</summary>
        public const string State = "VAR_GALAR_TOWER_STATE";
        /// <summary>The rank the player stands at, from 0 (the first) to <see cref="Ranks"/> − 1 (the Master Rank).</summary>
        public const string Rank = "VAR_GALAR_TOWER_RANK";
        /// <summary>The wins at the rank so far, toward the next.</summary>
        public const string Wins = "VAR_GALAR_TOWER_WINS";
        /// <summary>The Battle Points the run has earned, handed over as it ends.</summary>
        public const string Earned = "VAR_GALAR_TOWER_EARNED";
        /// <summary>How many times Leon has been beaten on the ladder.</summary>
        public const string LeonBeaten = "VAR_GALAR_TOWER_LEON_BEATEN";
        /// <summary>The places in the team of the Pokémon entered, one variable each (a place plus one; 0 for none).</summary>
        public static string Slot(int n) => $"VAR_GALAR_TOWER_SLOT_{n}";
    }

    /// <summary>Whether the next battle at a rank with so many wins is Leon's: the Master Rank's third.</summary>
    public static bool LeonWaits(int rank, int wins) => rank >= Ranks - 1 && wins == WinsToRise - 1;

    /// <summary>The Tower's room a rank's trainers are drawn from: the first two ranks the first room, then a room higher every rank to the eighth.</summary>
    public static int RoomOf(int rank) => Math.Clamp(rank - 1, 0, 7);

    /// <summary>The Frontier's trainer for a battle: the room's row, the rank's third (its rising battle) from the room's row of bosses.</summary>
    public static int TrainerFor(int rank, int wins, Func<int> next16)
    {
        int room = RoomOf(rank);
        int[] row = wins == WinsToRise - 1 ? FrontierDatabase.TowerBossRooms[room] : FrontierDatabase.TowerRooms[room];
        return row[0] + (next16() & 0xFFFF) % (row[1] - row[0] + 1);
    }

    /// <summary>The trainer of the battle with the team made for them.</summary>
    public static List<FrontierOpponent> MakeOpponent(int rank, int wins, Func<int> next16)
    {
        int trainer = TrainerFor(rank, wins, next16);
        return FrontierTrainers.ForTrainers(PartySize, trainer, trainer, false, next16);
    }

    /// <summary>Leon with the last three of his team of ours at level 50, or null where the data has none.</summary>
    public static Trainer? Leon()
    {
        var record = TrainerDatabase.Get(LeonId, RegionDatabase.Galar);
        if (record == null) return null;
        var trainer = new Trainer { Id = record.Id };
        TrainerDatabase.Fill(trainer, record);
        var last = trainer.Party.Members.Skip(Math.Max(0, trainer.Party.Count - PartySize)).ToList();
        var three = new Trainer
        {
            Id = trainer.Id, Name = trainer.Name, TrainerClass = trainer.TrainerClass, PrizeMoney = 0, Ai = trainer.Ai,
            Items = new List<string>(), Look = trainer.Look, DynamaxBand = trainer.DynamaxBand
        };
        foreach (var p in last) three.Party.Add(p);
        return three.Relevelled(_ => FrontierRules.Level);
    }

    /// <summary>The Battle Points a win earns at a rank: one, and one more for every third rank.</summary>
    public static int PointsFor(int rank) => 1 + Math.Clamp(rank, 0, Ranks - 1) / 3;

    /// <summary>
    /// A win counted: its points kept for the run's end, and three at a rank take the player up (at the Master Rank the
    /// count starts again, Leon beaten). 1 when the rank rose, 2 when Leon was beaten, 0 otherwise.
    /// </summary>
    public static int Won(Story.StoryState story)
    {
        int rank = story.Var(Vars.Rank), wins = story.Var(Vars.Wins) + 1;
        story.SetVar(Vars.Earned, story.Var(Vars.Earned) + PointsFor(rank));
        if (wins < WinsToRise) { story.SetVar(Vars.Wins, wins); return 0; }
        story.SetVar(Vars.Wins, 0);
        if (rank >= Ranks - 1)
        {
            story.SetVar(Vars.LeonBeaten, story.Var(Vars.LeonBeaten) + 1);
            return 2;
        }
        story.SetVar(Vars.Rank, rank + 1);
        return 1;
    }

    /// <summary>A loss: from the fifth rank on it takes a win back, never a rank.</summary>
    public static void Lost(Story.StoryState story)
    {
        if (story.Var(Vars.Rank) >= LossCostsFrom) story.SetVar(Vars.Wins, Math.Max(0, story.Var(Vars.Wins) - 1));
    }

    /// <summary>The places in the team of the Pokémon entered.</summary>
    public static List<int> SlotsOf(Story.StoryState story)
    {
        var slots = new List<int>();
        for (int n = 0; n < PartySize; n++)
            if (story.Var(Vars.Slot(n)) > 0) slots.Add(story.Var(Vars.Slot(n)) - 1);
        return slots;
    }

    /// <summary>Keeps the Pokémon entered in the story's variables.</summary>
    public static void KeepSlots(Story.StoryState story, IReadOnlyList<int> slots)
    {
        for (int n = 0; n < PartySize; n++) story.SetVar(Vars.Slot(n), n < slots.Count ? slots[n] + 1 : 0);
    }
}
