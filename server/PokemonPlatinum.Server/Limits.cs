using PokemonPlatinumEngine.Online;

namespace PokemonPlatinum.Server;

/// <summary>
/// How much one address may do (plan 07 · O4): wrong link codes (ten in ten minutes stop it joining for ten minutes,
/// the plan's rule), codes made, connections open at once and messages a second on one connection (limits of ours,
/// generous for friends and tight for a script). Kept in memory by the address's hash: a restart forgets them, which
/// costs a guesser nothing it didn't have.
/// </summary>
public sealed class Limits
{
    public const int MessagesPerSecond = 200;
    public const int ConnectionsPerAddress = 16;
    public const int CodesPerWindow = 30;

    private readonly Dictionary<string, List<DateTimeOffset>> wrong = new(), made = new();
    private readonly Dictionary<string, DateTimeOffset> blockedUntil = new();
    private readonly Dictionary<string, int> open = new();

    private static readonly TimeSpan Window = TimeSpan.FromSeconds(OnlineRules.GuessWindowSeconds);

    /// <summary>Whether the address may try a code now.</summary>
    public bool MayGuess(string ip, DateTimeOffset now) => !blockedUntil.TryGetValue(ip, out var until) || now >= until;

    /// <summary>A wrong code from the address: whether it is blocked by it.</summary>
    public bool WrongGuess(string ip, DateTimeOffset now)
    {
        var list = Recent(wrong, ip, now);
        list.Add(now);
        if (list.Count < OnlineRules.WrongGuesses) return false;
        blockedUntil[ip] = now + Window;
        list.Clear();
        return true;
    }

    /// <summary>Whether the address may make another code now (and counts it).</summary>
    public bool MayMake(string ip, DateTimeOffset now)
    {
        var list = Recent(made, ip, now);
        if (list.Count >= CodesPerWindow) return false;
        list.Add(now);
        return true;
    }

    /// <summary>A connection opens from the address: whether it may.</summary>
    public bool Open(string ip)
    {
        open.TryGetValue(ip, out int count);
        if (count >= ConnectionsPerAddress) return false;
        open[ip] = count + 1;
        return true;
    }

    public void Close(string ip)
    {
        if (open.TryGetValue(ip, out int count)) open[ip] = Math.Max(0, count - 1);
    }

    /// <summary>A message on a connection: whether it is within the connection's allowance this second.</summary>
    public static bool Allow(Connection c, DateTimeOffset now)
    {
        if (now - c.AllowanceSince >= TimeSpan.FromSeconds(1))
        {
            c.AllowanceSince = now;
            c.Allowance = MessagesPerSecond;
        }
        if (c.Allowance <= 0) return false;
        c.Allowance--;
        return true;
    }

    private static List<DateTimeOffset> Recent(Dictionary<string, List<DateTimeOffset>> by, string ip, DateTimeOffset now)
    {
        if (!by.TryGetValue(ip, out var list)) by[ip] = list = new List<DateTimeOffset>();
        list.RemoveAll(t => now - t >= Window);
        return list;
    }
}
