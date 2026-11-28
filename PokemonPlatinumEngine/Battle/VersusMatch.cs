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
/// <para>
/// Four at one machine (plan 13 · V21, <see cref="OfFour"/>): two trainers a side with a place each. A side still has
/// one screen, which asks its two trainers for their own places one after the other (as a friend beside the player is
/// asked, V20), so the curtain comes down between the sides and never between partners.
/// </para>
/// </summary>
public sealed class VersusMatch
{
    public LinkServer Server { get; }

    /// <summary>The two sides' screens: the first side's (the core's player side) and the second's.</summary>
    public IReadOnlyList<BattleEngine> Faces => faces;

    /// <summary>Whom the curtain names for each side: the player's name, or both of a side's two trainers'.</summary>
    public IReadOnlyList<string> Names { get; }

    /// <summary>Four play: two trainers a side (plan 13 · V21).</summary>
    public bool Four => Server.Setup.Multi;

    /// <summary>Which side's screen is shown and answered (0 or 1).</summary>
    public int Shown { get; private set; }

    /// <summary>The screen shown.</summary>
    public BattleEngine Face => faces[Shown];

    /// <summary>The side the curtain waits for, while it is down; null while a screen is shown.</summary>
    public int? Curtain { get; private set; }

    /// <summary>
    /// Whose controller is wanted, of four (plan 13 · V21): 0 and 1 the first side's two trainers, 2 and 3 the second's.
    /// Behind the curtain, the first trainer of the side it waits for. With two players, always 0: they pass one round.
    /// </summary>
    public int Seat => !Four ? 0 : Curtain is { } next ? next * 2 : Shown * 2 + (Face.FriendChoosing ? 1 : 0);

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
        : this(new[] { first, second }, room, seed, scene, looks)
    {
    }

    /// <summary>A battle of four (plan 13 · V21): the first side's two trainers against the second's.</summary>
    /// <param name="looks">The four players' looks, in the order of the players.</param>
    public static VersusMatch OfFour(LinkPlayer first, LinkPlayer firstPartner, LinkPlayer second, LinkPlayer secondPartner,
        LinkRoom room, uint seed, bool scene = true, IReadOnlyList<string?>? looks = null) =>
        new(new[] { first, firstPartner, second, secondPartner }, room, seed, scene, looks);

    private VersusMatch(LinkPlayer[] players, LinkRoom room, uint seed, bool scene, IReadOnlyList<string?>? looks)
    {
        bool four = players.Length == 4;
        // Each brings only what goes in, the first of their team in its order: what a side is shown of the other before
        // the battle and what its view names are then the same Pokémon
        players = players.Select(p => p with { Team = p.Team.Take(room.Picks).ToList() }).ToArray();
        // Each side's player: the one greeted, whose screen it is
        var leads = four ? new[] { players[0], players[2] } : players;
        var setup = four
            ? LinkSetup.BringMulti(room, players[0], players[1], players[2], players[3])
            : LinkSetup.Bring(room, players[0], players[1]);
        Names = four
            ? new[] { BattleCore.JoinNames(new[] { players[0].Name, players[1].Name }), BattleCore.JoinNames(new[] { players[2].Name, players[3].Name }) }
            : new[] { players[0].Name, players[1].Name };
        Server = new LinkServer(setup, seed, links[0].Server, links[1].Server);
        var ends = new[] { links[0].Player, links[1].Player };
        var clients = Enumerable.Range(0, 2).Select(side => new LinkClient(leads[side].Name, leads[side].Team, ends[side], new BroughtInOrder())
        {
            AnswersByItself = false,
            Seats = !four ? null : new LinkSeats(players[side * 2 + 1], players[(1 - side) * 2 + 1].Name,
                (side == 0 ? setup.TeamB : setup.TeamA).Count)
        }).ToArray();
        // Greeted, picked, and the battle begun: everything the loopback carries passed until nothing more is said
        for (int round = 0; round < 16; round++)
        {
            foreach (var c in clients) c.Pump();
            Server.Pump();
        }
        if (clients.Any(c => c.Hello == null)) throw new InvalidOperationException("The versus room never greeted both players");
        string? LookOf(int i) => looks != null && i < looks.Count ? looks[i] : null;
        faces = Enumerable.Range(0, 2).Select(side =>
        {
            int other = 1 - side;
            return four
                ? BattleEngine.ForLink(clients[side], scene, LookOf(side * 2), LookOf(other * 2), LookOf(side * 2 + 1), LookOf(other * 2 + 1))
                : BattleEngine.ForLink(clients[side], scene, LookOf(side), LookOf(other));
        }).ToArray();
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
