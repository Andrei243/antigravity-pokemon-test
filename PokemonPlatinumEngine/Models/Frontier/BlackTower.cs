using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Frontier;

/// <summary>
/// Black City's Black Tower (plan 22 · U10, the last part; the rulings' "The Black Tower and N's Castle"): Black 2's climb
/// told with the Frontier's rules (plan 06 · R18) lent, as the Battle Subway lends them (<see cref="BattleSubway"/>).
/// Three of the player's Pokémon go in as copies at level 50 (<see cref="FrontierRules"/>), whole again before every
/// battle; five areas of three trainers of the Frontier's table, drawn by the Tower's rooms one area to a room; the
/// fifth area's third battle is Benga's once Unova's Hall of Fame is entered (before it, the room's boss of the table).
/// A climb lives in the story's own variables (<see cref="Vars"/>), so nothing new is saved and a game saved on the stairs
/// goes on at its battle.
/// </summary>
public static class BlackTower
{
    /// <summary>The areas of a climb and the battles of each.</summary>
    public const int Areas = 5, PerArea = 3, Battles = Areas * PerArea;

    /// <summary>How many Pokémon go in.</summary>
    public const int PartySize = 3;

    /// <summary>Benga's team, Black 2's own, which the data has among Unova's trainers.</summary>
    public const string BengaId = "unova_boss_trainer_benga";

    /// <summary>The story's variables a climb keeps its state in.</summary>
    public static class Vars
    {
        /// <summary>0 while no climb is under way, 1 while one is.</summary>
        public const string State = "VAR_UNOVA_BLACK_TOWER_STATE";
        /// <summary>The battles the climb has won.</summary>
        public const string Wins = "VAR_UNOVA_BLACK_TOWER_WINS";
        /// <summary>The places in the team of the Pokémon entered, one variable each (a place plus one; 0 for none).</summary>
        public static string Slot(int n) => $"VAR_UNOVA_BLACK_TOWER_SLOT_{n}";
        /// <summary>The most areas a climb has cleared.</summary>
        public const string Best = "VAR_UNOVA_BLACK_TOWER_BEST";
        /// <summary>How many times the top has been reached and its last battle won.</summary>
        public const string Cleared = "VAR_UNOVA_BLACK_TOWER_CLEARED";
    }

    /// <summary>The area the battle of that number (from 1) is in, from 1.</summary>
    public static int AreaOf(int battle) => Math.Clamp((battle - 1) / PerArea + 1, 1, Areas);

    /// <summary>Whether the battle of that number is the top's last, with Benga waiting at it after the Hall of Fame.</summary>
    public static bool IsTop(int battle) => battle == Battles;

    /// <summary>The Frontier's trainer for the battle of that number: the row of the Tower's room of its area, the area's last battle from the room's row of bosses.</summary>
    public static int TrainerFor(int battle, Func<int> next16)
    {
        int room = Math.Min(AreaOf(battle) - 1, 7), place = (battle - 1) % PerArea;
        int[] row = place == PerArea - 1 ? FrontierDatabase.TowerBossRooms[room] : FrontierDatabase.TowerRooms[room];
        return row[0] + (next16() & 0xFFFF) % (row[1] - row[0] + 1);
    }

    /// <summary>The trainer of the battle of that number with the team made for them, dressed as the Subway's riders of Unova's classes.</summary>
    public static List<FrontierOpponent> MakeOpponent(int battle, Func<int> next16)
    {
        int trainer = TrainerFor(battle, next16);
        return SubwayRiders.Dress(FrontierTrainers.ForTrainers(PartySize, trainer, trainer, false, next16));
    }

    /// <summary>Benga with his own team of the data at level 50, or null where the data has none.</summary>
    public static Trainer? Benga()
    {
        var record = TrainerDatabase.Get(BengaId);
        if (record == null) return null;
        var trainer = new Trainer
        {
            Id = record.Id,
            Name = record.Name,
            TrainerClass = record.Class,
            PrizeMoney = 0,
            Ai = Battle.Sim.Ai.AiFlagNames.Parse(record.Ai),
            Items = new List<string>()
        };
        foreach (var m in record.Party) trainer.Party.Add(TrainerDatabase.Build(m));
        return trainer.Relevelled(_ => FrontierRules.Level);
    }

    /// <summary>The Battle Points a climb earns as it ends: one for each area cleared, and ten more for the top.</summary>
    public static int PointsFor(int wins) => wins / PerArea + (wins >= Battles ? 10 : 0);

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
