using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim.Link;

/// <summary>What a link battle's room levels its teams to (plan 28 · I4): everyone to 50 or to 100, or each as it is.</summary>
public enum LinkLevel { FlatFifty, FlatHundred, AsIs }

/// <summary>
/// The rules of a room two players battle in (plan 28 · I4): whose rules (Platinum's by default), the level, the
/// format, how many of the six brought go in (6, or 3 for "pick three of six"), and the clauses checked as the teams
/// are brought. Plain data: both players and the server read the same.
/// </summary>
public sealed record LinkRoom(RulesPreset Rules = RulesPreset.Platinum, LinkLevel Level = LinkLevel.FlatFifty,
    BattleFormat Format = BattleFormat.Single, int Picks = 6, bool SpeciesClause = true, bool ItemClause = true)
{
    /// <summary>A Flat 50 room under Platinum's rules, six against six: the default, as plan 28's design has it.</summary>
    public static LinkRoom Default => new();
}

/// <summary>
/// One of the six a player brought, as the team preview shows it to the other (plan 28 · I2): species by number, form,
/// level, gender and whether it is shiny. No moves, no item, no ability, no stats.
/// </summary>
public sealed record PreviewEntry(int Dex, string? Form, int Level, Gender Gender, bool Shiny);

/// <summary>
/// What one place may answer with (plan 28 · I3), worked out by the server from the rules (<see cref="BattleCore.WhyNot"/>)
/// for its own side alone: the moves by position, whether only Struggle is left, and the party members it may switch to
/// or send in.
/// </summary>
public sealed record PlaceOptions(Place Place, IReadOnlyList<int> Moves, bool Struggle, IReadOnlyList<int> Switches, bool MustSendIn);

/// <summary>How a link battle came to its end (plan 28 · I4).</summary>
public enum LinkEnding { KnockedOut, Forfeit, Timer, Disconnect }

/// <summary>A link battle's result (plan 28 · I4): who won (none for a match given up before it began), after how many turns, and how it ended. What plan 07 · O6 records against two friend codes.</summary>
public sealed record LinkResult(BattleSide? Winner, int Turns, LinkEnding How);

/// <summary>A message of a link battle (plan 28 · I3), on the wire through <see cref="BattleWire.WriteMessage"/>.</summary>
public abstract record LinkMessage;

/// <summary>
/// The server to each player as a room fills: which side they are, the room, the other's name and the other's six as
/// the preview shows them; to a player brought back after a drop, also the order they picked (null before they have).
/// </summary>
public sealed record LinkHello(int Version, BattleSide Side, string FoeName, LinkRoom Room, IReadOnlyList<PreviewEntry> FoePreview,
    IReadOnlyList<int>? Picked = null) : LinkMessage;

/// <summary>A player to the server: the positions of their own six that go in, in the order they go in.</summary>
public sealed record LinkPick(IReadOnlyList<int> Order) : LinkMessage;

/// <summary>The server to a player: what their screen shows of the turn, once every answer it waited for is in (<see cref="SideView"/>).</summary>
public sealed record LinkTurn(IReadOnlyList<BattleEvent> Events) : LinkMessage;

/// <summary>
/// The server to a player: the question for their own places alone (<paramref name="Request"/> names no place of the
/// other side), numbered so that an answer names what it answers, with what each place may answer.
/// </summary>
public sealed record LinkAsk(int Number, BattleRequest Request, IReadOnlyList<PlaceOptions> Options) : LinkMessage;

/// <summary>A player to the server: the answer to question <paramref name="Number"/>, one choice for each place asked.</summary>
public sealed record LinkAnswer(int Number, IReadOnlyList<BattleChoice> Choices) : LinkMessage;

/// <summary>A player to the server: they give the match up.</summary>
public sealed record LinkForfeit : LinkMessage;

/// <summary>The server to a player: what they sent can't be taken, and why; the question stands.</summary>
public sealed record LinkRefused(string Reason) : LinkMessage;

/// <summary>The server to both players: the battle is over.</summary>
public sealed record LinkOver(LinkResult Result) : LinkMessage;
