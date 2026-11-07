using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Link;

namespace PokemonPlatinumEngine.Battle;

/// <summary>
/// Two people at one machine (plan 13 · V12, "Local versus"): a link battle with its server and both players in one
/// process, over two loopback links, so that it is fought exactly as a battle between two machines is (plan 28: the
/// server the only one who rolls, each player asked only their own places and shown only their own side). Each player
/// has a screen of their own (<see cref="BattleEngine.ForLink"/>); one is shown and answered at a time, the other played
/// along unseen (<see cref="BattleEngine.Unseen"/>, its lines read on by themselves). When the shown player has answered
/// and the other is asked, a curtain comes down between them (<see cref="Curtain"/>) and is lifted by the other player
/// (<see cref="PassTo"/>), so neither sees the other's choice. The battle is fought on copies at the room's level, so
/// neither team changes. GPU-free and with no clock or input: the game moves it on (<see cref="Frame"/>).
/// </summary>
public sealed class VersusMatch
{
    public LinkServer Server { get; }

    /// <summary>The two players' screens: the first player's (the core's player side) and the second's.</summary>
    public IReadOnlyList<BattleEngine> Faces => faces;

    /// <summary>The players' names, as each is greeted.</summary>
    public IReadOnlyList<string> Names { get; }

    /// <summary>Which player's screen is shown and answered (0 or 1).</summary>
    public int Shown { get; private set; }

    /// <summary>The screen shown.</summary>
    public BattleEngine Face => faces[Shown];

    /// <summary>The player the curtain waits for, while it is down; null while a screen is shown.</summary>
    public int? Curtain { get; private set; }

    /// <summary>How the battle ended, once the server says so.</summary>
    public LinkResult? Result => Server.Result;

    private readonly BattleEngine[] faces;
    private readonly LoopbackLink[] links = { new(), new() };

    /// <param name="first">The first player: their name and the team they bring, in its order.</param>
    /// <param name="second">The second player.</param>
    /// <param name="room">The room's rules (level, format, how many go in, the clauses).</param>
    /// <param name="seed">Where the battle's chance begins.</param>
    /// <param name="scene">The options' Battle Scene.</param>
    /// <param name="looks">Each player's look on their own platform and across the other's field.</param>
    public VersusMatch(LinkPlayer first, LinkPlayer second, LinkRoom room, uint seed, bool scene = true, IReadOnlyList<string?>? looks = null)
    {
        Names = new[] { first.Name, second.Name };
        Server = new LinkServer(LinkSetup.Bring(room, first, second), seed, links[0].Server, links[1].Server);
        var clients = new[]
        {
            new LinkClient(first.Name, first.Team, links[0].Player, new BroughtInOrder()) { AnswersByItself = false },
            new LinkClient(second.Name, second.Team, links[1].Player, new BroughtInOrder()) { AnswersByItself = false }
        };
        // Greeted, picked, and the battle begun: everything the loopback carries passed until nothing more is said
        for (int round = 0; round < 16; round++)
        {
            foreach (var c in clients) c.Pump();
            Server.Pump();
        }
        if (clients.Any(c => c.Hello == null)) throw new InvalidOperationException("The versus room never greeted both players");
        string? lookOf(int i) => looks != null && i < looks.Count ? looks[i] : null;
        faces = new[]
        {
            BattleEngine.ForLink(clients[0], scene, lookOf(0), lookOf(1)),
            BattleEngine.ForLink(clients[1], scene, lookOf(1), lookOf(0))
        };
        faces[1].Unseen = true;
    }

    /// <summary>Whether the battle is over and the shown screen has shown its end.</summary>
    public bool Over => Result != null && Face.IsBattleOver;

    /// <summary>
    /// A frame after the shown screen's own: the server answers what was sent, the unseen screen is played along, and
    /// the curtain comes down when the shown player waits and the other is asked.
    /// </summary>
    public void Frame(float dt)
    {
        Server.Pump();
        int other = 1 - Shown;
        // Its sounds and music are the shown screen's to make: the unseen one is played along in silence
        bool hushed = Core.AudioManager.Hushed;
        Core.AudioManager.Hushed = true;
        try { faces[other].Update(dt); }
        finally { Core.AudioManager.Hushed = hushed; }
        Server.Pump();
        if (Curtain == null && Result == null && Face.WaitingForOther && faces[other].LinkClient?.Asked != null)
            Curtain = other;
    }

    /// <summary>The curtain lifted by the player it waits for: their screen is the one shown and answered now.</summary>
    public void PassTo()
    {
        if (Curtain is not { } next) return;
        faces[Shown].Unseen = true;
        Shown = next;
        faces[Shown].Unseen = false;
        Curtain = null;
    }
}
