namespace PokemonPlatinum.Server;

/// <summary>
/// The server's settings (plan 07 · O4), read from the configuration section <c>LinkServer</c> (so the environment's
/// <c>LinkServer__IpHashSecret</c>, <c>LinkServer__Database</c> and <c>LinkServer__TrustedProxy</c>, as the setup
/// guide's service file sets them) and from the command line's <c>--local</c>, <c>--port</c> and <c>--database</c>.
/// </summary>
public sealed class ServerOptions
{
    /// <summary>The secret players' IP addresses (and install tokens) are hashed with: made once, never changed, never shared.</summary>
    public string? IpHashSecret { get; set; }

    /// <summary>The SQLite file.</summary>
    public string Database { get; set; } = "link.db";

    /// <summary>The address of the proxy whose <c>X-Forwarded-For</c> is believed (Caddy, on the same machine); none in local mode.</summary>
    public string? TrustedProxy { get; set; }

    /// <summary>Local mode: on localhost alone, no proxy, a secret made for the run if none is set (two game windows on one machine).</summary>
    public bool Local { get; set; }

    /// <summary>The port local mode listens on (0: any free one).</summary>
    public int Port { get; set; } = 5080;

    /// <summary>The server's clock: the system's, or a test's.</summary>
    public TimeProvider Clock { get; set; } = TimeProvider.System;

    /// <summary>
    /// For the tests alone: the step of a trade at which the server "dies" (<see cref="TradeSteps"/>): it stops on the
    /// spot, writes nothing more and drops every connection, as a killed process would.
    /// </summary>
    public string? CrashAt { get; set; }

    /// <summary>Whether the server keeps its own clock ticking (rooms' expiry, turn timers); the tests tick it by hand.</summary>
    public bool Ticking { get; set; } = true;
}

/// <summary>The steps of a trade on the server, in order, which <see cref="ServerOptions.CrashAt"/> names.</summary>
public static class TradeSteps
{
    public const string BeforeCommit = "before-commit";
    public const string AfterCommit = "after-commit";
    public const string AfterReceiptA = "after-receipt-a";
    public const string AfterReceiptB = "after-receipt-b";
    public const string AfterFirstAck = "after-ack-1";
    public const string AfterSecondAck = "after-ack-2";

    public static readonly string[] All = { BeforeCommit, AfterCommit, AfterReceiptA, AfterReceiptB, AfterFirstAck, AfterSecondAck };
}

/// <summary>Thrown where <see cref="ServerOptions.CrashAt"/> says: the server dies there.</summary>
public sealed class SimulatedCrash : Exception
{
    public SimulatedCrash(string step) : base($"The server was killed at {step}") { }
}
