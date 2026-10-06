namespace PokemonPlatinumEngine.Models;

/// <summary>What a Pokémon that won't obey does instead of the move it was told to use.</summary>
public enum Disobedience
{
    /// <summary>It obeys.</summary>
    None,
    /// <summary>It does nothing: loafs, turns away, pretends not to notice (<c>subscript_disobey_do_nothing</c>).</summary>
    Nothing,
    /// <summary>Asleep, it ignores an order to Snore or Sleep Talk (<c>subscript_disobey_while_asleep</c>).</summary>
    WhileAsleep,
    /// <summary>It uses another of its moves (<c>subscript_disobey_orders</c>).</summary>
    OtherMove,
    /// <summary>It lies down for a nap: it falls asleep (<c>subscript_disobey_sleep</c>).</summary>
    Naps,
    /// <summary>It hurts itself as a confused Pokémon would (<c>subscript_disobey_hit_self</c>).</summary>
    HitsItself
}

/// <summary>
/// Whether a Pokémon obeys its trainer, by Platinum's rule (plan 06 · R10; <c>BattleControllerPlayer_CheckObedience</c>).
/// Only one whose original trainer is someone else can disobey, and only above the level the player's badges let
/// them command: 10 with fewer than two badges, 30 from two, 50 from four, 70 from six, any level with all eight.
/// No drawing or input; the battle core asks it before a move and rolls on its own generator.
/// </summary>
public static class Obedience
{
    /// <summary>The highest level the player's badges let a traded Pokémon be commanded at; null with all eight.</summary>
    public static int? LevelCap(int badges) => badges >= 8 ? null : badges >= 6 ? 70 : badges >= 4 ? 50 : badges >= 2 ? 30 : 10;

    /// <summary>Whether a Pokémon is someone else's, by its original trainer (<c>BattleSystem_TrainerIsOT</c>).</summary>
    public static bool IsOutsider(Pokemon p, TrainerMark? player) =>
        p.OriginalTrainer is { } mark && (player == null || !mark.Is(player.Name, player.Id, player.Look));

    /// <summary>
    /// The level difference's first test (<c>(rand & 0xFF) * (level + cap) >> 8 &lt; cap</c>): a roll of 256 against
    /// the Pokémon's level and the cap together. A Pokémon at or under the cap always obeys.
    /// </summary>
    public static bool Obeys(int level, int cap, int roll256) => level <= cap || (roll256 * (level + cap)) >> 8 < cap;
}
