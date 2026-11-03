using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using PokemonPlatinumEngine.Online;

namespace PokemonPlatinum.Server;

/// <summary>
/// One game's WebSocket on the server: who it is (once its <see cref="Hello"/> is taken), the room it is in, and a
/// queue of what goes out to it, which one writer empties (a WebSocket takes one send at a time). What it sends in
/// goes to the hub, a message at a time.
/// </summary>
public sealed class Connection
{
    private readonly WebSocket socket;
    private readonly Channel<string> outgoing = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });

    public Connection(WebSocket socket, string ipHash, bool loopback)
    {
        this.socket = socket;
        IpHash = ipHash;
        Loopback = loopback;
    }

    public string IpHash { get; }
    public bool Loopback { get; }
    public Player? Player { get; set; }
    public string Name { get; set; } = "";
    public string DataVersion { get; set; } = "";
    public Room? Room { get; set; }
    public bool Closed { get; private set; }

    /// <summary>The messages it may still send this second, and when that second began (a bucket the hub refills).</summary>
    public int Allowance { get; set; } = Limits.MessagesPerSecond;
    public DateTimeOffset AllowanceSince { get; set; }

    public void Send(OnlineMessage message)
    {
        if (!Closed) outgoing.Writer.TryWrite(OnlineProtocol.Write(message));
    }

    private Task writer = Task.CompletedTask;

    /// <summary>Starts sending what is queued, until the queue is closed (<see cref="CloseAsync"/>) or the request ends.</summary>
    public void StartWriting(CancellationToken stop) => writer = WriteAsync(stop);

    /// <summary>Sends what is queued until the connection closes.</summary>
    private async Task WriteAsync(CancellationToken stop)
    {
        try
        {
            await foreach (var text in outgoing.Reader.ReadAllAsync(stop))
                await socket.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, stop);
        }
        catch (Exception e) when (e is OperationCanceledException or WebSocketException or ObjectDisposedException) { }
    }

    /// <summary>The next whole text message, or null once the socket is closed (or a message is too long or not text).</summary>
    public async Task<string?> ReadAsync(CancellationToken stop)
    {
        var buffer = new byte[8192];
        using var whole = new MemoryStream();
        try
        {
            while (true)
            {
                var result = await socket.ReceiveAsync(buffer, stop);
                if (result.MessageType == WebSocketMessageType.Close) return null;
                if (result.MessageType != WebSocketMessageType.Text) return null;
                whole.Write(buffer, 0, result.Count);
                if (whole.Length > OnlineRules.MostBytes) return null;
                if (result.EndOfMessage) return Encoding.UTF8.GetString(whole.ToArray());
            }
        }
        catch (Exception e) when (e is OperationCanceledException or WebSocketException or ObjectDisposedException)
        {
            return null;
        }
    }

    /// <summary>Closes after what is queued has gone (a graceful shutdown, a refusal that ends the connection).</summary>
    public async Task CloseAsync(string why)
    {
        if (Closed)
        {
            await writer;
            return;
        }
        Closed = true;
        outgoing.Writer.TryComplete();
        try
        {
            // What was queued goes first: the writer ends once the queue is empty
            await writer.WaitAsync(TimeSpan.FromSeconds(5));
            if (socket.State == WebSocketState.Open)
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, why, CancellationToken.None);
        }
        catch (Exception e) when (e is WebSocketException or ObjectDisposedException or OperationCanceledException or TimeoutException) { }
    }

    /// <summary>Drops the connection on the spot, nothing more sent (a killed server).</summary>
    public void Abort()
    {
        Closed = true;
        outgoing.Writer.TryComplete();
        socket.Abort();
    }
}
