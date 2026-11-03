using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim.Link;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Online;

namespace PokemonPlatinum.Server;

/// <summary>
/// One of a room's two players, bound to it as they joined (plan 07's identity): their friend code with the hashes of
/// their install token and address. A dropped player may come back within <see cref="OnlineRules.RejoinSeconds"/> only
/// with the same friend code and the same token or address.
/// </summary>
public sealed class Member
{
    public Member(Connection c)
    {
        FriendCode = c.Player!.FriendCode;
        TokenHash = c.Player.TokenHash;
        IpHash = c.IpHash;
        Name = c.Name;
        Connection = c;
    }

    public string FriendCode { get; }
    public string? TokenHash { get; }
    public string IpHash { get; }
    public string Name { get; }
    public Connection? Connection { get; set; }
    public DateTimeOffset? AwaySince { get; set; }

    public bool Binds(Connection c) =>
        c.Player!.FriendCode == FriendCode && (c.Player.TokenHash != null && c.Player.TokenHash == TokenHash || c.IpHash == IpHash);

    public void Send(OnlineMessage message) => Connection?.Send(message);
}

/// <summary>
/// A link-code room (plan 07 · O4): its code, which works once and for <see cref="OnlineRules.CodeSeconds"/>, its kind,
/// its two players, and what goes on in it: a trade's offers and confirmations (O5) or a battle (O6).
/// </summary>
public sealed class Room
{
    public Room(string code, RoomKind kind, LinkRoom? rules, Member maker, DateTimeOffset expires)
    {
        Code = code;
        Kind = kind;
        Rules = rules;
        Members[0] = maker;
        Expires = expires;
    }

    public string Code { get; }
    public RoomKind Kind { get; }
    public LinkRoom? Rules { get; }
    public Member?[] Members { get; } = new Member?[2];
    public DateTimeOffset Expires { get; }
    public bool Full => Members[1] != null;

    public int IndexOf(Connection c) => Members[0]?.Connection == c ? 0 : Members[1]?.Connection == c ? 1 : -1;
    public Member Other(int index) => Members[1 - index]!;

    // ---- A trade (O5) ----
    public SavedPokemonData?[] Offers { get; } = new SavedPokemonData?[2];
    public bool[] Confirmed { get; } = new bool[2];

    public void ResetTrade()
    {
        Offers[0] = Offers[1] = null;
        Confirmed[0] = Confirmed[1] = false;
    }

    // ---- A battle (O6) ----
    public LinkPlayer?[] Teams { get; } = new LinkPlayer?[2];
    public LinkServer? Battle { get; set; }
    public uint Seed { get; set; }
    public bool Recorded { get; set; }

    /// <summary>The turn timer: the question each side was last timed on and when its time runs out.</summary>
    public int[] TimedQuestion { get; } = { -1, -1 };
    public DateTimeOffset[] Deadline { get; } = new DateTimeOffset[2];

    public static BattleSide SideOf(int index) => index == 0 ? BattleSide.Player : BattleSide.Enemy;
}

/// <summary>
/// The battle host's end of a link (plan 07 · O6): <see cref="LinkServer"/>'s <see cref="ILinkChannel"/> for one side,
/// which wraps each of its texts in a <see cref="BattleMessage"/> to the member's connection, and sends nothing while the
/// member is away (<see cref="LinkServer.Rejoin"/> sends them their whole view again). What the member sends comes in
/// through <see cref="LinkServer.Receive"/>, never through <see cref="TryReceive"/>.
/// </summary>
public sealed class SocketLink : ILinkChannel
{
    private readonly Member member;

    public SocketLink(Member member) => this.member = member;

    public void Send(string text) => member.Send(new BattleMessage(text));

    public bool TryReceive(out string text)
    {
        text = "";
        return false;
    }
}
