using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PokemonPlatinumEngine.Battle.Sim.Link;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Online;

/// <summary>
/// The save a trade is applied to (plan 07 · O5), as the game will hand it to <see cref="OnlineClient.Apply"/>: whether
/// it holds a Pokémon of an id (the team, the boxes, the Day Care: <c>GameEngine.OwnedPokemon</c>), and the swap, which
/// takes the given one out, puts the received one in (the game's <c>ReceiveTradedPokemon</c>, with its trade evolution)
/// and writes the save before it returns.
/// </summary>
public interface ITradeKeeper
{
    bool Holds(Guid id);
    void Swap(Guid given, SavedPokemonData received);
}

/// <summary>What became of a receipt handed to <see cref="OnlineClient.Apply"/>.</summary>
public enum ReceiptOutcome
{
    /// <summary>Swapped and saved now, and acknowledged.</summary>
    Applied,
    /// <summary>The save holds the received Pokémon already (a receipt sent again after a crash): acknowledged, nothing changed.</summary>
    AlreadyApplied,
    /// <summary>The save holds neither Pokémon: it isn't the save that traded. Nothing changed and nothing acknowledged; the receipt waits.</summary>
    NotThisSave
}

/// <summary>The server said no (<see cref="Refused"/>), as an exception for a caller that awaited something else.</summary>
public sealed class OnlineRefusedException : Exception
{
    public Refused Refusal { get; }
    public OnlineRefusedException(Refused refused) : base($"{refused.Code}{(refused.Details != null ? ": " + refused.Details : "")}") => Refusal = refused;
}

/// <summary>
/// The game's end of the online server (plan 07 · O4 to O6), GPU-free and with no screen: a WebSocket to the server's
/// <see cref="OnlineRules.Path"/>, the <see cref="Hello"/> with the install token (<see cref="NewToken"/>, made once and
/// kept beside <c>settings.json</c> by the game), the friend code it is given, rooms by link code, the trade's offer,
/// confirmation and receipts (<see cref="Apply"/>), and a battle through plan 28's <see cref="LinkClient"/> over
/// <see cref="BattleChannel"/>. Messages come in on a reader of their own and wait in a queue: the game takes them a
/// frame at a time (<see cref="TryTake"/>), a test awaits the one it wants (<see cref="NextAsync{T}"/>). It reads no
/// clock; a wait is a <see cref="TimeSpan"/> given to the runtime.
/// </summary>
public sealed class OnlineClient : IAsyncDisposable
{
    private readonly Uri server;
    private readonly string token, name, dataVersion;
    private readonly ClientWebSocket socket = new();
    private readonly object sending = new();
    private Task lastSend = Task.CompletedTask;
    private readonly SemaphoreSlim arrived = new(0);
    private readonly List<OnlineMessage> inbox = new();
    private readonly ConcurrentQueue<string> battleInbox = new();
    private readonly CancellationTokenSource stop = new();
    private Task? reader;
    private volatile bool ended;

    /// <param name="server">The server's address, <c>wss://name.example/link</c> (the options' server row; <c>ws://127.0.0.1:5080/link</c> for a local server).</param>
    /// <param name="token">The install token (<see cref="NewToken"/>), the same every time this install connects.</param>
    /// <param name="name">The player's name, shown to the other player.</param>
    /// <param name="dataVersion">The game's data (<see cref="GameDataFiles.Version"/> unless a test says otherwise).</param>
    public OnlineClient(Uri server, string token, string name, string? dataVersion = null)
    {
        this.server = server;
        this.token = token;
        this.name = name;
        this.dataVersion = dataVersion ?? GameDataFiles.Version;
        BattleChannel = new Channel(this);
    }

    /// <summary>A new install token: 32 random bytes as hex, made once and kept.</summary>
    public static string NewToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    public Welcome? Welcome { get; private set; }
    public string? FriendCode => Welcome?.FriendCode;

    /// <summary>The room the client is in, once one is joined.</summary>
    public Joined? Room { get; private set; }

    /// <summary>Whether the connection is open (false once the server or the network dropped it).</summary>
    public bool Connected => socket.State == WebSocketState.Open && reader != null && !ended;

    /// <summary>Connects and says hello: the welcome, with the friend code and any receipts still to apply.</summary>
    public async Task<Welcome> ConnectAsync(TimeSpan wait)
    {
        using var timeout = new CancellationTokenSource(wait);
        await socket.ConnectAsync(server, timeout.Token);
        reader = Task.Run(ReadAsync);
        await SendAsync(new Hello(dataVersion, token, name));
        Welcome = await NextAsync<Welcome>(wait);
        return Welcome;
    }

    /// <summary>Sends a message after every one sent before it, in the order they were given (a battle's pick before its answers).</summary>
    public Task SendAsync(OnlineMessage message)
    {
        var bytes = Encoding.UTF8.GetBytes(OnlineProtocol.Write(message));
        lock (sending)
        {
            lastSend = lastSend.ContinueWith(async _ =>
                await socket.SendAsync(bytes, WebSocketMessageType.Text, true, stop.Token), TaskScheduler.Default).Unwrap();
            return lastSend;
        }
    }

    /// <summary>Sends without waiting (the game's frame can't): the send goes on by itself, and a lost one shows as a dropped connection.</summary>
    public void Send(OnlineMessage message) => _ = SendQuietly(message);

    private async Task SendQuietly(OnlineMessage message)
    {
        try { await SendAsync(message); }
        catch (Exception e) when (e is WebSocketException or ObjectDisposedException or OperationCanceledException or InvalidOperationException) { }
    }

    private async Task ReadAsync()
    {
        var buffer = new byte[8192];
        using var whole = new MemoryStream();
        try
        {
            while (!stop.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(buffer, stop.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    // The server closes (a shutdown, a refusal): the close is answered, and what came before it stays to be read
                    try { await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None); }
                    catch (Exception e) when (e is WebSocketException or ObjectDisposedException or InvalidOperationException) { }
                    break;
                }
                whole.Write(buffer, 0, result.Count);
                if (!result.EndOfMessage) continue;
                var text = Encoding.UTF8.GetString(whole.ToArray());
                whole.SetLength(0);
                OnlineMessage message;
                try { message = OnlineProtocol.Read(text); }
                catch (OnlineProtocolException) { continue; }
                if (message is BattleMessage battle)
                {
                    battleInbox.Enqueue(battle.Text);
                    continue;
                }
                lock (inbox)
                {
                    if (message is Joined joined) Room = joined;
                    if (message is RoomClosed) Room = null;
                    inbox.Add(message);
                }
                arrived.Release();
            }
        }
        catch (Exception e) when (e is WebSocketException or OperationCanceledException or ObjectDisposedException) { }
        finally
        {
            ended = true;
            arrived.Release();
        }
    }

    /// <summary>The next message waiting (not a battle's: those go to <see cref="BattleChannel"/>), if any.</summary>
    public bool TryTake(out OnlineMessage message)
    {
        lock (inbox)
        {
            if (inbox.Count == 0)
            {
                message = null!;
                return false;
            }
            message = inbox[0];
            inbox.RemoveAt(0);
            return true;
        }
    }

    /// <summary>Every message waiting, in order, without taking them.</summary>
    public IReadOnlyList<OnlineMessage> Waiting()
    {
        lock (inbox) return inbox.ToList();
    }

    /// <summary>
    /// Waits for the first message of type <typeparamref name="T"/>, taking it and leaving the others where they are; a
    /// <see cref="Refused"/> that comes first throws (unless <typeparamref name="T"/> is <see cref="Refused"/>), and so do a
    /// closed connection and the time running out.
    /// </summary>
    public Task<T> NextAsync<T>(TimeSpan wait) where T : OnlineMessage => NextAsync<T>(wait, _ => true);

    /// <summary>Waits for the first message of type <typeparamref name="T"/> that <paramref name="match"/> takes, as <see cref="NextAsync{T}(TimeSpan)"/>.</summary>
    public async Task<T> NextAsync<T>(TimeSpan wait, Func<T, bool> match) where T : OnlineMessage
    {
        using var timeout = new CancellationTokenSource(wait);
        while (true)
        {
            lock (inbox)
            {
                for (int i = 0; i < inbox.Count; i++)
                {
                    if (inbox[i] is T found && match(found))
                    {
                        inbox.RemoveAt(i);
                        return found;
                    }
                    if (inbox[i] is Refused refused && typeof(T) != typeof(Refused))
                    {
                        inbox.RemoveAt(i);
                        throw new OnlineRefusedException(refused);
                    }
                }
            }
            if (ended) throw new WebSocketException($"The connection closed before a {typeof(T).Name} came");
            try
            {
                await arrived.WaitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"No {typeof(T).Name} came");
            }
        }
    }

    // ---- Rooms ----

    public async Task<CodeMade> MakeCodeAsync(RoomKind kind, TimeSpan wait, LinkRoom? rules = null)
    {
        await SendAsync(new MakeCode(kind, rules));
        return await NextAsync<CodeMade>(wait);
    }

    public async Task<Joined> JoinAsync(string code, TimeSpan wait)
    {
        await SendAsync(new JoinCode(code));
        return await NextAsync<Joined>(wait);
    }

    /// <summary>Back into the room the welcome named (<see cref="Welcome.Room"/>) after a dropped connection.</summary>
    public async Task<Joined> RejoinAsync(string code, TimeSpan wait)
    {
        await SendAsync(new Rejoin(code));
        return await NextAsync<Joined>(wait);
    }

    // ---- Trading ----

    /// <summary>Offers a Pokémon (it must carry its id: <c>GameEngine.GiveIds</c> first), and waits for the server's check.</summary>
    public async Task<TradeOffered> OfferAsync(SavedPokemonData pokemon, TimeSpan wait)
    {
        await SendAsync(new TradeOffer(pokemon));
        return await NextAsync<TradeOffered>(wait, o => o.Mine);
    }

    /// <summary>
    /// Applies a receipt to the save, at most once whatever happens (plan 07's crash safety): a Pokémon already held is
    /// never added again, and the server is told only once the save is written.
    /// </summary>
    public async Task<ReceiptOutcome> Apply(TradeReceipt receipt, ITradeKeeper keeper)
    {
        var received = receipt.Received.Id ?? Guid.Empty;
        ReceiptOutcome outcome;
        if (keeper.Holds(received)) outcome = ReceiptOutcome.AlreadyApplied;
        else if (!keeper.Holds(receipt.Given)) return ReceiptOutcome.NotThisSave;
        else
        {
            keeper.Swap(receipt.Given, receipt.Received);
            outcome = ReceiptOutcome.Applied;
        }
        await SendAsync(new TradeAck(receipt.Receipt));
        return outcome;
    }

    // ---- Battles ----

    /// <summary>The battle's link: what <see cref="LinkClient"/> reads and writes plan 28's messages through.</summary>
    public ILinkChannel BattleChannel { get; }

    /// <summary>Brings a team to the battle room and makes the battle's client, which plays as the server's messages come (<see cref="LinkClient.Pump"/>).</summary>
    public LinkClient StartBattle(LinkPlayer team, ILinkChooser chooser)
    {
        var client = new LinkClient(team.Name, team.Team, BattleChannel, chooser);
        Send(new BattleTeam(team));
        return client;
    }

    /// <summary>A battle's link rebuilt after a reconnect (the server greets the player again and sends their view so far).</summary>
    public LinkClient ResumeBattle(LinkPlayer team, ILinkChooser chooser) => new(team.Name, team.Team, BattleChannel, chooser);

    /// <summary>Writes a battle's end into the save's record of link battles (plan 07 · O2, decision 9).</summary>
    public static void Record(LinkBattleRecord record, BattleRecorded recorded) => record.Record(recorded.OtherFriendCode, recorded.Outcome);

    private sealed class Channel : ILinkChannel
    {
        private readonly OnlineClient owner;
        public Channel(OnlineClient owner) => this.owner = owner;
        public void Send(string text) => owner.Send(new BattleMessage(text));
        public bool TryReceive(out string text) => owner.battleInbox.TryDequeue(out text!);
    }

    // ---- Closing ----

    /// <summary>Closes the connection as a game does on leaving the counter.</summary>
    public async Task CloseAsync()
    {
        try
        {
            if (socket.State == WebSocketState.Open)
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
        }
        catch (Exception e) when (e is WebSocketException or ObjectDisposedException) { }
        stop.Cancel();
        if (reader != null) await reader;
    }

    /// <summary>Drops the connection on the spot (a test's crash of the game, a network gone).</summary>
    public void Abort()
    {
        socket.Abort();
        stop.Cancel();
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
        socket.Dispose();
        stop.Dispose();
    }
}
