using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The Pokémon people in the game trade for one of the player's (plan 06 · R12): Platinum's four, by the numbers of
/// its trade table (<c>res/npc_trades</c>, built by <c>NPCTrade_CreateMon</c>). Each wants one species and gives
/// another, at the level of the Pokémon given, with its own name, its first trainer's name, number and look, fixed
/// IVs and a fixed personality (so its nature, ability and gender are always the same), and the item it holds.
/// Platinum's table gives one number for all five of a Pokémon's contest condition (<see cref="Contest"/>); a
/// trade of the GBA games gives each of the five its own (<see cref="Conditions"/>: cool, beauty, cute, smart and
/// tough) and a sheen (plan 21 · H6, part b: Fortree City's Plusle). No drawing or input: a script asks for it with
/// <c>trade</c>.
/// </summary>
public sealed record NpcTrade(
    string Id, string Wants, string Gives, string Nickname, string TrainerName, int TrainerId, PlayerLook TrainerLook,
    uint Personality, string Item, int[] Ivs, Nature Nature, string Ability, Gender Gender, int Contest = 0, string? Language = null,
    int[]? Conditions = null, int Sheen = 0, string? Region = null, Gender? WantsGender = null)
{
    /// <summary>Where a traded Pokémon was met: the original writes "Link trade" whatever the map (met location 2001).</summary>
    public const string MetBy = "a trade";
}

public static class NpcTrades
{
    /// <summary>
    /// Every trade people of the game make (plan 40 · GE2), read from <c>trades.json</c>: Platinum's four, FireRed's nine,
    /// Emerald's four, HeartGold's six with a person and Black 2's six, written by <c>tools/DataImporter -- --trades</c>
    /// from the decompilations' tables (numbers and names, no text), and the rows of our own the importer adds
    /// (<c>Overrides/trades.json</c>). IVs are in the table's order: HP, Attack, Defense, Speed, Sp. Atk, Sp. Def.
    /// </summary>
    public static IReadOnlyList<NpcTrade> All { get; } = GameDataFiles.Load<List<NpcTrade>>(FileName);

    public const string FileName = "trades.json";

    public static NpcTrade? Get(string id) => All.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The Pokémon a trade gives for one of the given level: the species' moves at that level, its base
    /// friendship, a Poké Ball, its first trainer's mark (so it counts as another trainer's, for EXP and for
    /// obedience) and met "in a trade" today at that level.
    /// </summary>
    public static Pokemon Make(NpcTrade trade, int level, DateTime today)
    {
        var species = PokemonDatabase.Get(trade.Gives) ?? throw new InvalidOperationException($"No species {trade.Gives}.");
        var mon = new Pokemon(species, level, trade.Gender, trade.Nature, false)
        {
            Nickname = trade.Nickname,
            Personality = trade.Personality,
            AbilityName = trade.Ability,
            HeldItem = ItemDatabase.Get(trade.Item),
            Ball = "Poké Ball",
            OriginalTrainer = new TrainerMark(trade.TrainerName, trade.TrainerId, trade.TrainerLook),
            Language = trade.Language,
            // The table's cool, beauty, cute, smart and tough (NPCTrade_CreateMon: one number, and never the sheen),
            // or a GBA table's five and its sheen (CreateInGameTradePokemon)
            Cool = trade.Conditions?[0] ?? trade.Contest,
            Beauty = trade.Conditions?[1] ?? trade.Contest,
            Cute = trade.Conditions?[2] ?? trade.Contest,
            Smart = trade.Conditions?[3] ?? trade.Contest,
            Tough = trade.Conditions?[4] ?? trade.Contest,
            Sheen = trade.Sheen,
            IvHP = trade.Ivs[0],
            IvAttack = trade.Ivs[1],
            IvDefense = trade.Ivs[2],
            IvSpeed = trade.Ivs[3],
            IvSpAttack = trade.Ivs[4],
            IvSpDefense = trade.Ivs[5],
            Friendship = species.BaseFriendship
        };
        mon.RecalculateStats();
        mon.CurrentHP = mon.MaxHP;
        mon.Met(NpcTrade.MetBy, today);
        return mon;
    }

    /// <summary>
    /// Trades the team's Pokémon at <paramref name="slot"/> for the trade's: false, with nothing changed, when it isn't
    /// the species asked for. The one received takes the given one's place on the team, and nothing evolves: the
    /// original's trade with a person never asks for a trade evolution.
    /// </summary>
    public static Pokemon? Trade(NpcTrade trade, Party party, int slot, DateTime today)
    {
        if (slot < 0 || slot >= party.Count) return null;
        var given = party.Members[slot];
        // An Egg is no species anyone asks for (GetPartyMonSpecies reads nothing from one; plan 06 · R15)
        if (given.IsEgg || given.Species.Name != trade.Wants) return null;
        // HeartGold's girl in Blackthorn wants a female Dragonair, and takes no male one
        if (trade.WantsGender is { } wanted && given.Gender != wanted) return null;
        var received = Make(trade, given.Level, today);
        party.Members[slot] = received;
        return received;
    }
}
