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
    int[]? Conditions = null, int Sheen = 0)
{
    /// <summary>Where a traded Pokémon was met: the original writes "Link trade" whatever the map (met location 2001).</summary>
    public const string MetBy = "a trade";
}

public static class NpcTrades
{
    // IVs in the table's order: HP, Attack, Defense, Speed, Sp. Atk, Sp. Def. The nature, ability and gender are what
    // the fixed personality gives (Pokemon_InitWith: nature PID % 25, ability by bit 0, gender by the low byte)
    public static readonly IReadOnlyList<NpcTrade> All = new[]
    {
        new NpcTrade("kazza", "Machop", "Abra", "Kazza", "Hilary", 25643, PlayerLook.Girl, 142, "Oran Berry",
            new[] { 15, 15, 15, 20, 25, 25 }, Nature.Quiet, "Synchronize", Gender.Male),
        new NpcTrade("charap", "Buizel", "Chatot", "Charap", "Norton", 44142, PlayerLook.Boy, 2151, "Leppa Berry",
            new[] { 15, 20, 15, 25, 25, 15 }, Nature.Lonely, "Tangled Feet", Gender.Female, Contest: 20),
        new NpcTrade("gaspar", "Medicham", "Haunter", "Gaspar", "Mindy", 19248, PlayerLook.Girl, 136, "Everstone",
            new[] { 20, 25, 15, 25, 15, 15 }, Nature.Hasty, "Levitate", Gender.Male),
        new NpcTrade("foppa", "Finneon", "Magikarp", "Foppa", "Meister", 53277, PlayerLook.Boy, 1116, "Lum Berry",
            new[] { 15, 25, 15, 20, 25, 15 }, Nature.Mild, "Swift Swim", Gender.Female, Language: "German"),
        // Emerald's INGAME_TRADE_PLUSLE (src/data/trade.h): the boy of Fortree City's first house wants a Volbeat.
        // Its personality 0x6F gives a Hasty female with its first ability; the IVs are in the table's order, which
        // is the one above. Emerald's Plusle holds Wood Mail, which the item table hasn't got: it holds Grass Mail
        new NpcTrade("pluses", "Volbeat", "Plusle", "Pluses", "Roman", 73996, PlayerLook.Boy, 0x6F, "Grass Mail",
            new[] { 4, 4, 4, 5, 5, 4 }, Nature.Hasty, "Plus", Gender.Female,
            Conditions: new[] { 5, 5, 30, 5, 5 }, Sheen: 10),
        // Emerald's INGAME_TRADE_HORSEA (plan 21 · H8, part d): the man of Pacifidlog Town's third house wants a Bagon.
        // Its personality 0x7F gives a Brave male with its first ability. Emerald's Horsea holds Wave Mail, which the
        // item table hasn't got: it holds Bubble Mail, the sea's
        new NpcTrade("seasor", "Bagon", "Horsea", "Seasor", "Skylar", 46285, PlayerLook.Boy, 0x7F, "Bubble Mail",
            new[] { 5, 4, 4, 4, 5, 4 }, Nature.Brave, "Swift Swim", Gender.Male,
            Conditions: new[] { 5, 5, 5, 5, 30 }, Sheen: 10)
    };

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
        var received = Make(trade, given.Level, today);
        party.Members[slot] = received;
        return received;
    }
}
