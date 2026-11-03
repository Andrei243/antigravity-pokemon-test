using System.Collections.Generic;

namespace PokemonPlatinumEngine.Battle.Sim.Link;

/// <summary>
/// One end of a link between a player and the server (plan 28 · I3): text goes out and text comes in, nothing else.
/// The server holds one end for each player and each player the other. Plan 07 · O4 puts a WebSocket behind it; here
/// there is only the loopback, two queues in one process (<see cref="LoopbackLink"/>), which is all the tests need and
/// what a hot-seat battle on one machine (plan 13 · V12) could use.
/// </summary>
public interface ILinkChannel
{
    void Send(string text);

    /// <summary>The next text that came in, if any.</summary>
    bool TryReceive(out string text);
}

/// <summary>
/// A link with no network (plan 28 · I3): two ends whose sends are the other's receipts, in order. Every text that
/// crossed is kept, each way, so a test can read exactly what a player was sent.
/// </summary>
public sealed class LoopbackLink
{
    private readonly Queue<string> toServer = new(), toPlayer = new();
    private readonly List<string> sentToServer = new(), sentToPlayer = new();

    public LoopbackLink()
    {
        Server = new End(toPlayer, sentToPlayer, toServer);
        Player = new End(toServer, sentToServer, toPlayer);
    }

    /// <summary>The server's end.</summary>
    public ILinkChannel Server { get; }

    /// <summary>The player's end.</summary>
    public ILinkChannel Player { get; }

    /// <summary>Everything the server sent the player, in order.</summary>
    public IReadOnlyList<string> SentToPlayer => sentToPlayer;

    /// <summary>Everything the player sent the server, in order.</summary>
    public IReadOnlyList<string> SentToServer => sentToServer;

    private sealed class End : ILinkChannel
    {
        private readonly Queue<string> outbox, inbox;
        private readonly List<string> kept;

        public End(Queue<string> outbox, List<string> kept, Queue<string> inbox)
        {
            this.outbox = outbox;
            this.kept = kept;
            this.inbox = inbox;
        }

        public void Send(string text)
        {
            outbox.Enqueue(text);
            kept.Add(text);
        }

        public bool TryReceive(out string text) => inbox.TryDequeue(out text!);
    }
}
