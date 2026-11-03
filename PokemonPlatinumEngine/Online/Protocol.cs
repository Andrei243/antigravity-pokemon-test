using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using PokemonPlatinumEngine.Battle.Sim.Link;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Online;

// The online protocol (plan 07 · O4): every message the game and the server send each other, as plain records, and
// the one way they are written and read. The server (server/PokemonPlatinum.Server) and the game's client
// (OnlineClient) both reference this file, which plan 07 · O1 moves into the Core library with the rest of the rules.
// A message is a JSON object of its fields in camel case with its kind in "type", inside an envelope that carries the
// protocol's version: {"v":1,"msg":{"type":"hello",...}}. A battle's own messages (plan 28's LinkMessage) cross inside
// a BattleMessage, as BattleWire wrote them, so the battle's wire format stays the one plan 28 holds to its tests.

/// <summary>What kind of room a link code opens: a trade, or a battle.</summary>
public enum RoomKind { Trade, Battle }

/// <summary>Why the server refuses something (<see cref="Refused"/>): what the game shows a line for.</summary>
public enum RefusalCode
{
    /// <summary>The game speaks another version of the protocol: it must be updated.</summary>
    Outdated,
    /// <summary>The game's data isn't the server's (<see cref="GameDataFiles.Version"/>): it can't trade or battle until updated.</summary>
    DataVersion,
    /// <summary>The message was not understood, or not asked for now.</summary>
    BadMessage,
    /// <summary>Too many messages, codes or connections from one place in too little time.</summary>
    TooFast,
    /// <summary>Too many wrong link codes from one address: joining is stopped for a while.</summary>
    TooManyGuesses,
    /// <summary>No open room has that code (never made, used already, or expired).</summary>
    NoSuchRoom,
    /// <summary>The room has its two players.</summary>
    RoomFull,
    /// <summary>The player is in a room already.</summary>
    InARoom,
    /// <summary>The Pokémon or the team can't be (<see cref="PokemonCheck"/>, a room's clauses): the details say why.</summary>
    Illegal,
    /// <summary>A Pokémon offered is one the server knows has been traded to someone else, or is in a trade not yet finished.</summary>
    Duplicate,
    /// <summary>The server is shutting down.</summary>
    Closing
}

/// <summary>A message of the online protocol (plan 07 · O4), each kind with its name on the wire.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Hello), "hello")]
[JsonDerivedType(typeof(Welcome), "welcome")]
[JsonDerivedType(typeof(Refused), "refused")]
[JsonDerivedType(typeof(MakeCode), "makeCode")]
[JsonDerivedType(typeof(CodeMade), "codeMade")]
[JsonDerivedType(typeof(JoinCode), "joinCode")]
[JsonDerivedType(typeof(Joined), "joined")]
[JsonDerivedType(typeof(Rejoin), "rejoin")]
[JsonDerivedType(typeof(LeaveRoom), "leaveRoom")]
[JsonDerivedType(typeof(RoomClosed), "roomClosed")]
[JsonDerivedType(typeof(OtherAway), "otherAway")]
[JsonDerivedType(typeof(OtherBack), "otherBack")]
[JsonDerivedType(typeof(TradeOffer), "tradeOffer")]
[JsonDerivedType(typeof(TradeOffered), "tradeOffered")]
[JsonDerivedType(typeof(TradeConfirm), "tradeConfirm")]
[JsonDerivedType(typeof(TradeConfirmed), "tradeConfirmed")]
[JsonDerivedType(typeof(TradeCancel), "tradeCancel")]
[JsonDerivedType(typeof(TradeCancelled), "tradeCancelled")]
[JsonDerivedType(typeof(TradeReceipt), "tradeReceipt")]
[JsonDerivedType(typeof(TradeAck), "tradeAck")]
[JsonDerivedType(typeof(TradeDone), "tradeDone")]
[JsonDerivedType(typeof(BattleTeam), "battleTeam")]
[JsonDerivedType(typeof(BattleMessage), "battle")]
[JsonDerivedType(typeof(BattleRecorded), "battleRecorded")]
[JsonDerivedType(typeof(ServerClosing), "serverClosing")]
public abstract record OnlineMessage;

/// <summary>
/// The game to the server, first on every connection: what it is, the data it was built with, the install token it
/// made once and keeps beside <c>settings.json</c> (never shown, never typed: plan 07's identity), and the player's name.
/// </summary>
public sealed record Hello(string DataVersion, string Token, string Name) : OnlineMessage;

/// <summary>
/// The server to the game: its friend code (made on its first visit), the server's protocol and data, the trades that
/// still wait for this player's receipt to be applied (<see cref="TradeReceipt"/>, plan 07's crash safety), and the
/// code of a room it may come back to (a battle it dropped out of, within <see cref="OnlineRules.RejoinSeconds"/>).
/// </summary>
public sealed record Welcome(string FriendCode, int Protocol, string DataVersion, IReadOnlyList<TradeReceipt> Pending, string? Room) : OnlineMessage;

/// <summary>The server to the game: what was asked can't be done, why, and the details where there are some (a Pokémon's faults, a line each).</summary>
public sealed record Refused(RefusalCode Code, string? Details = null) : OnlineMessage;

/// <summary>The game: make a link code for a room of this kind; for a battle, by these rules (null: <see cref="LinkRoom.Default"/>).</summary>
public sealed record MakeCode(RoomKind Kind, LinkRoom? Rules = null) : OnlineMessage;

/// <summary>The server: the room's code, which works once, for <see cref="OnlineRules.CodeSeconds"/>.</summary>
public sealed record CodeMade(string Code, RoomKind Kind, int Seconds) : OnlineMessage;

/// <summary>The game: join the room of this code.</summary>
public sealed record JoinCode(string Code) : OnlineMessage;

/// <summary>The server to both players as a room fills: its code and kind, the battle's rules, who the other is.</summary>
public sealed record Joined(string Code, RoomKind Kind, LinkRoom? Rules, string OtherFriendCode, string OtherName) : OnlineMessage;

/// <summary>The game, after a dropped connection: back into the room of this code (only from the same address or with the same token).</summary>
public sealed record Rejoin(string Code) : OnlineMessage;

/// <summary>The game: out of the room. A battle under way is given up; a trade not yet confirmed by both is called off.</summary>
public sealed record LeaveRoom : OnlineMessage;

/// <summary>The server: the room is gone (the other left, gave up for good, or the code expired).</summary>
public sealed record RoomClosed(string Code) : OnlineMessage;

/// <summary>The server: the other player's connection dropped; they have <paramref name="Seconds"/> to come back.</summary>
public sealed record OtherAway(int Seconds) : OnlineMessage;

/// <summary>The server: the other player is back.</summary>
public sealed record OtherBack : OnlineMessage;

/// <summary>The game: the Pokémon it offers, as a save keeps it, with its own id (<c>Pokemon.Id</c>). A new offer replaces the last and undoes both confirmations.</summary>
public sealed record TradeOffer(Core.SavedPokemonData Pokemon) : OnlineMessage;

/// <summary>The server, to both: a Pokémon has been offered and passed the check; <paramref name="Mine"/> says whose.</summary>
public sealed record TradeOffered(bool Mine, Core.SavedPokemonData Pokemon) : OnlineMessage;

/// <summary>The game: it confirms the trade as offered now.</summary>
public sealed record TradeConfirm : OnlineMessage;

/// <summary>The server, to both: one side has confirmed.</summary>
public sealed record TradeConfirmed(bool Mine) : OnlineMessage;

/// <summary>The game: calls the trade off (until both have confirmed).</summary>
public sealed record TradeCancel : OnlineMessage;

/// <summary>The server, to both: the trade was called off, or an offer was changed; both choose again.</summary>
public sealed record TradeCancelled : OnlineMessage;

/// <summary>
/// The server: the trade is done on the server (written down before this was sent). The game takes
/// <paramref name="Given"/> out, puts <paramref name="Received"/> in, saves at once and answers with
/// <see cref="TradeAck"/>; a receipt sent again (after a crash on either end) changes nothing a second time, since the
/// game refuses an id it holds.
/// </summary>
public sealed record TradeReceipt(Guid Receipt, Guid Given, Core.SavedPokemonData Received, string OtherFriendCode) : OnlineMessage;

/// <summary>The game: the receipt is applied and saved.</summary>
public sealed record TradeAck(Guid Receipt) : OnlineMessage;

/// <summary>The server: this player's side of the trade is finished.</summary>
public sealed record TradeDone(Guid Receipt) : OnlineMessage;

/// <summary>The game, in a battle room: the name and the team it brings (as a battle's record keeps a team).</summary>
public sealed record BattleTeam(LinkPlayer Team) : OnlineMessage;

/// <summary>A battle's own message (plan 28's <see cref="LinkMessage"/>), as <see cref="BattleWire"/> wrote it, either way.</summary>
public sealed record BattleMessage(string Text) : OnlineMessage;

/// <summary>The server: the battle is over and counted against the two friend codes; what the game writes into its <c>LinkBattleRecord</c>.</summary>
public sealed record BattleRecorded(string OtherFriendCode, Models.LinkOutcome Outcome, LinkEnding How) : OnlineMessage;

/// <summary>The server: it is shutting down; a trade finishes by its receipt at the next connection, a battle under way is lost.</summary>
public sealed record ServerClosing : OnlineMessage;

/// <summary>The fixed numbers of the online rules (plan 07's design), which the server keeps and the game shows.</summary>
public static class OnlineRules
{
    /// <summary>How long a link code waits for the second player.</summary>
    public const int CodeSeconds = 600;

    /// <summary>How long a dropped player has to come back into a room.</summary>
    public const int RejoinSeconds = 60;

    /// <summary>A battle's turn timer.</summary>
    public const int TurnSeconds = 60;

    /// <summary>Wrong codes from one address within <see cref="GuessWindowSeconds"/> that stop it joining for as long.</summary>
    public const int WrongGuesses = 10;
    public const int GuessWindowSeconds = 600;

    /// <summary>A link code's digits and a friend code's.</summary>
    public const int CodeDigits = 8;
    public const int FriendCodeDigits = 12;

    /// <summary>The largest message either end takes, in bytes.</summary>
    public const int MostBytes = 64 * 1024;

    /// <summary>The path of the WebSocket on the server.</summary>
    public const string Path = "/link";
}

/// <summary>The one way an online message is written and read (plan 07 · O4).</summary>
public static class OnlineProtocol
{
    /// <summary>The protocol's version: a game that speaks another is told to update.</summary>
    public const int Version = 1;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        AllowOutOfOrderMetadataProperties = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private sealed record Envelope(int V, OnlineMessage? Msg);

    public static string Write(OnlineMessage message) => JsonSerializer.Serialize(new Envelope(Version, message), Json);

    /// <summary>A message's text, or why it can't be read (another version, not a message).</summary>
    public static OnlineMessage Read(string text)
    {
        Envelope? envelope;
        try
        {
            using var doc = JsonDocument.Parse(text);
            if (!doc.RootElement.TryGetProperty("v", out var v) || v.ValueKind != JsonValueKind.Number || v.GetInt32() != Version)
                throw new OnlineProtocolException(RefusalCode.Outdated, "another version of the protocol");
            envelope = doc.RootElement.Deserialize<Envelope>(Json);
        }
        catch (JsonException e)
        {
            throw new OnlineProtocolException(RefusalCode.BadMessage, e.Message);
        }
        catch (NotSupportedException e)
        {
            throw new OnlineProtocolException(RefusalCode.BadMessage, e.Message);
        }
        catch (InvalidOperationException e)
        {
            throw new OnlineProtocolException(RefusalCode.BadMessage, e.Message);
        }
        return envelope?.Msg ?? throw new OnlineProtocolException(RefusalCode.BadMessage, "no message");
    }
}

/// <summary>A text that is no message of this protocol.</summary>
public sealed class OnlineProtocolException : Exception
{
    public RefusalCode Code { get; }
    public OnlineProtocolException(RefusalCode code, string message) : base(message) => Code = code;
}
