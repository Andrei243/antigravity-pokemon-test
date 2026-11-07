using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using PokemonPlatinumEngine.Battle.Sim.Link;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Online;

namespace PokemonPlatinum.Server;

/// <summary>A player as the server knows them: a friend code, and the hashes it recognises them by.</summary>
public sealed record Player(string FriendCode, string? TokenHash, string IpHash);

/// <summary>A trade written down (the write-ahead trade log): who gave what to whom, and which of the two has applied it.</summary>
public sealed record TradeRow(Guid Receipt, string CodeA, string CodeB, SavedPokemonData FromA, SavedPokemonData FromB, bool AckA, bool AckB, bool Done)
{
    /// <summary>What the player of <paramref name="code"/> still has to apply, if anything.</summary>
    public TradeReceipt? ReceiptFor(string code) =>
        Done ? null
        : code == CodeA && !AckA ? new TradeReceipt(Receipt, FromA.Id!.Value, FromB, CodeB)
        : code == CodeB && !AckB ? new TradeReceipt(Receipt, FromB.Id!.Value, FromA, CodeA)
        : null;
}

/// <summary>
/// The server's SQLite store (plan 07 · O4 to O6): the players (friend codes by the keyed hashes of their install
/// tokens and IP addresses, never the addresses themselves), the trade log, whose row is written before any receipt
/// goes out, who each traded Pokémon belongs to now (so the same Pokémon can't be traded away twice), and the battles'
/// results with their records. Every write is one transaction, journalled (WAL, synchronous FULL), so a killed server
/// finds either all of a step or none of it. One connection, used under the hub's lock.
/// </summary>
public sealed class Store : IDisposable
{
    private const int SchemaVersion = 1;

    /// <summary>A player not seen for this long is forgotten (plan 07's privacy).</summary>
    public static readonly TimeSpan ForgetAfter = TimeSpan.FromDays(365);

    private readonly SqliteConnection db;
    // The one connection is used under the hub's lock and also by a caller that only reads (a test polling OpenTrades):
    // a statement from another thread while a transaction is pending throws, so every statement and every transaction
    // holds this lock (reentrant, so Ack and Recover may read inside theirs).
    private readonly object sync = new();
    private readonly byte[] secret;

    public Store(string path, string secret)
    {
        this.secret = Encoding.UTF8.GetBytes(secret);
        db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString());
        db.Open();
        Run("PRAGMA journal_mode=WAL;");
        Run("PRAGMA synchronous=FULL;");
        Run("PRAGMA foreign_keys=ON;");
        Migrate();
    }

    public void Dispose() => db.Dispose();

    private void Migrate()
    {
        long version = Scalar<long>("PRAGMA user_version;");
        if (version >= SchemaVersion) return;
        using var tx = db.BeginTransaction();
        Run("""
            CREATE TABLE IF NOT EXISTS players (
              code TEXT PRIMARY KEY, token TEXT UNIQUE, ip TEXT NOT NULL, first_seen INTEGER NOT NULL, last_seen INTEGER NOT NULL);
            CREATE INDEX IF NOT EXISTS players_ip ON players(ip);
            CREATE TABLE IF NOT EXISTS trades (
              receipt TEXT PRIMARY KEY, code_a TEXT NOT NULL, code_b TEXT NOT NULL, id_a TEXT NOT NULL, id_b TEXT NOT NULL,
              pokemon_a TEXT NOT NULL, pokemon_b TEXT NOT NULL, data_version TEXT NOT NULL, at INTEGER NOT NULL,
              ack_a INTEGER NOT NULL DEFAULT 0, ack_b INTEGER NOT NULL DEFAULT 0, done INTEGER NOT NULL DEFAULT 0);
            CREATE INDEX IF NOT EXISTS trades_open ON trades(done);
            CREATE TABLE IF NOT EXISTS owners (id TEXT PRIMARY KEY, code TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS battles (
              id INTEGER PRIMARY KEY AUTOINCREMENT, code_a TEXT NOT NULL, code_b TEXT NOT NULL, winner TEXT, turns INTEGER NOT NULL,
              how TEXT NOT NULL, seed INTEGER NOT NULL, data_version TEXT NOT NULL, record TEXT, at INTEGER NOT NULL);
            """, tx);
        Run($"PRAGMA user_version={SchemaVersion};", tx);
        tx.Commit();
    }

    /// <summary>The keyed hash of a value (HMAC-SHA256 with the server's secret), hex: an address or a token is never stored as it is.</summary>
    public string Hash(string kind, string value)
    {
        using var mac = new HMACSHA256(secret);
        return Convert.ToHexString(mac.ComputeHash(Encoding.UTF8.GetBytes(kind + ":" + value))).ToLowerInvariant();
    }

    // ---- Players ----

    /// <summary>
    /// Who connects (plan 07's identity): a known install token keeps its friend code, which moves to the address it
    /// comes from now; a new token is a new player, whether its address is known or not (two players behind one
    /// router); a game that sends no token is the player of its address who has none, made on the address's first visit.
    /// </summary>
    public Player Recognise(string? tokenHash, string ipHash, DateTimeOffset now)
    {
        long t = now.ToUnixTimeSeconds();
        string? code = tokenHash != null
            ? Scalar<string?>("SELECT code FROM players WHERE token=$t", ("$t", tokenHash))
            : Scalar<string?>("SELECT code FROM players WHERE token IS NULL AND ip=$ip", ("$ip", ipHash));
        if (code != null)
        {
            Run("UPDATE players SET ip=$ip, last_seen=$now WHERE code=$c", null, ("$ip", ipHash), ("$now", t), ("$c", code));
            return new Player(code, tokenHash, ipHash);
        }
        do code = NewFriendCode(); while (Scalar<long>("SELECT COUNT(*) FROM players WHERE code=$c", ("$c", code)) > 0);
        Run("INSERT INTO players(code, token, ip, first_seen, last_seen) VALUES($c, $t, $ip, $now, $now)", null,
            ("$c", code), ("$t", (object?)tokenHash ?? DBNull.Value), ("$ip", ipHash), ("$now", t));
        return new Player(code, tokenHash, ipHash);
    }

    public int PlayerCount => (int)Scalar<long>("SELECT COUNT(*) FROM players");

    /// <summary>Forgets every player not seen for <see cref="ForgetAfter"/> who has no trade still waiting: how many.</summary>
    public int ForgetOld(DateTimeOffset now)
    {
        long before = (now - ForgetAfter).ToUnixTimeSeconds();
        return Run("""
            DELETE FROM players WHERE last_seen < $before
              AND code NOT IN (SELECT code_a FROM trades WHERE done=0) AND code NOT IN (SELECT code_b FROM trades WHERE done=0)
            """, null, ("$before", before));
    }

    private static string NewFriendCode()
    {
        Span<char> digits = stackalloc char[OnlineRules.FriendCodeDigits];
        for (int i = 0; i < digits.Length; i++) digits[i] = (char)('0' + RandomNumberGenerator.GetInt32(10));
        return $"{digits[..4]}-{digits[4..8]}-{digits[8..]}";
    }

    // ---- Trades ----

    /// <summary>Who a traded Pokémon belongs to now, by the last trade it was in; null for one never traded here.</summary>
    public string? OwnerOf(Guid id) => Scalar<string?>("SELECT code FROM owners WHERE id=$id", ("$id", id.ToString()));

    /// <summary>Whether a Pokémon is in a trade not yet applied by both.</summary>
    public bool InOpenTrade(Guid id) =>
        Scalar<long>("SELECT COUNT(*) FROM trades WHERE done=0 AND (id_a=$id OR id_b=$id)", ("$id", id.ToString())) > 0;

    /// <summary>
    /// The trade written down, once and for good, before anything is sent: the row, and each Pokémon's new owner, in
    /// one transaction. From here the trade goes forward only (a receipt for each side until each acknowledges it).
    /// </summary>
    public Guid Commit(string codeA, SavedPokemonData fromA, string codeB, SavedPokemonData fromB, string dataVersion, DateTimeOffset now)
    {
        var receipt = Guid.NewGuid();
        lock (sync)
        {
            using var tx = db.BeginTransaction();
            Run("""
                INSERT INTO trades(receipt, code_a, code_b, id_a, id_b, pokemon_a, pokemon_b, data_version, at)
                VALUES($r, $ca, $cb, $ia, $ib, $pa, $pb, $dv, $at)
                """, tx, ("$r", receipt.ToString()), ("$ca", codeA), ("$cb", codeB), ("$ia", fromA.Id!.Value.ToString()), ("$ib", fromB.Id!.Value.ToString()),
                ("$pa", Json(fromA)), ("$pb", Json(fromB)), ("$dv", dataVersion), ("$at", now.ToUnixTimeSeconds()));
            Run("INSERT OR REPLACE INTO owners(id, code) VALUES($id, $c)", tx, ("$id", fromA.Id.Value.ToString()), ("$c", codeB));
            Run("INSERT OR REPLACE INTO owners(id, code) VALUES($id, $c)", tx, ("$id", fromB.Id.Value.ToString()), ("$c", codeA));
            tx.Commit();
        }
        return receipt;
    }

    public TradeRow? Trade(Guid receipt) => Trades("WHERE receipt=$r", ("$r", receipt.ToString())).FirstOrDefault();

    /// <summary>Every trade that waits for the player of <paramref name="code"/> to apply it.</summary>
    public List<TradeReceipt> PendingFor(string code) =>
        Trades("WHERE done=0 AND ((code_a=$c AND ack_a=0) OR (code_b=$c AND ack_b=0)) ORDER BY at, receipt", ("$c", code))
            .Select(t => t.ReceiptFor(code)!).ToList();

    public List<TradeRow> OpenTrades() => Trades("WHERE done=0 ORDER BY at, receipt");

    /// <summary>The player of <paramref name="code"/> has applied the trade: whether it is now done for both. Nothing for a stranger to it.</summary>
    public (bool Known, bool Done) Ack(Guid receipt, string code)
    {
        lock (sync)
        {
            var trade = Trade(receipt);
            if (trade == null || code != trade.CodeA && code != trade.CodeB) return (false, false);
            using var tx = db.BeginTransaction();
            Run(code == trade.CodeA ? "UPDATE trades SET ack_a=1 WHERE receipt=$r" : "UPDATE trades SET ack_b=1 WHERE receipt=$r", tx, ("$r", receipt.ToString()));
            Run("UPDATE trades SET done=1 WHERE receipt=$r AND ack_a=1 AND ack_b=1", tx, ("$r", receipt.ToString()));
            tx.Commit();
            return (true, Trade(receipt)!.Done);
        }
    }

    /// <summary>
    /// The recovery pass, as the server starts: every trade still open keeps its owners as its row says (written in the
    /// same transaction as the row, so this changes nothing unless the file was mended by hand), and how many wait.
    /// </summary>
    public int Recover()
    {
        lock (sync)
        {
            var open = OpenTrades();
            if (open.Count == 0) return 0;
            using var tx = db.BeginTransaction();
            foreach (var t in open)
            {
                Run("INSERT OR REPLACE INTO owners(id, code) VALUES($id, $c)", tx, ("$id", t.FromA.Id!.Value.ToString()), ("$c", t.CodeB));
                Run("INSERT OR REPLACE INTO owners(id, code) VALUES($id, $c)", tx, ("$id", t.FromB.Id!.Value.ToString()), ("$c", t.CodeA));
            }
            tx.Commit();
            return open.Count;
        }
    }

    private List<TradeRow> Trades(string where, params (string, object)[] args)
    {
        lock (sync)
        {
            using var cmd = Command($"SELECT receipt, code_a, code_b, pokemon_a, pokemon_b, ack_a, ack_b, done FROM trades {where}", null, args);
            using var r = cmd.ExecuteReader();
            var rows = new List<TradeRow>();
            while (r.Read())
                rows.Add(new TradeRow(Guid.Parse(r.GetString(0)), r.GetString(1), r.GetString(2), Saved(r.GetString(3)), Saved(r.GetString(4)),
                    r.GetInt64(5) != 0, r.GetInt64(6) != 0, r.GetInt64(7) != 0));
            return rows;
        }
    }

    // ---- Battles ----

    /// <summary>A link battle's end, counted against the two friend codes, with its record for a bug report.</summary>
    public void RecordBattle(string codeA, string codeB, LinkResult result, uint seed, string dataVersion, string? record, DateTimeOffset now) =>
        Run("""
            INSERT INTO battles(code_a, code_b, winner, turns, how, seed, data_version, record, at)
            VALUES($a, $b, $w, $turns, $how, $seed, $dv, $rec, $at)
            """, null, ("$a", codeA), ("$b", codeB),
            ("$w", result.Winner switch { PokemonPlatinumEngine.Battle.BattleSide.Player => (object)"a", PokemonPlatinumEngine.Battle.BattleSide.Enemy => "b", _ => DBNull.Value }),
            ("$turns", result.Turns), ("$how", result.How.ToString()), ("$seed", (long)seed), ("$dv", dataVersion),
            ("$rec", (object?)record ?? DBNull.Value), ("$at", now.ToUnixTimeSeconds()));

    /// <summary>Wins, losses and draws of the player of <paramref name="code"/> against the player of <paramref name="other"/>.</summary>
    public (int Wins, int Losses, int Draws) Battles(string code, string other)
    {
        lock (sync)
        {
            int wins = 0, losses = 0, draws = 0;
            using var cmd = Command("SELECT code_a, winner FROM battles WHERE (code_a=$c AND code_b=$o) OR (code_a=$o AND code_b=$c)", null, ("$c", code), ("$o", other));
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                if (r.IsDBNull(1)) { draws++; continue; }
                bool aWon = r.GetString(1) == "a";
                if (aWon == (r.GetString(0) == code)) wins++; else losses++;
            }
            return (wins, losses, draws);
        }
    }

    // ---- Plumbing ----

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
    private static string Json(SavedPokemonData p) => System.Text.Json.JsonSerializer.Serialize(p, JsonOptions);
    private static SavedPokemonData Saved(string json) => System.Text.Json.JsonSerializer.Deserialize<SavedPokemonData>(json, JsonOptions)!;

    private SqliteCommand Command(string sql, SqliteTransaction? tx, params (string, object)[] args)
    {
        var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = tx;
        foreach (var (name, value) in args) cmd.Parameters.AddWithValue(name, value);
        return cmd;
    }

    private int Run(string sql, SqliteTransaction? tx = null, params (string, object)[] args)
    {
        lock (sync)
        {
            using var cmd = Command(sql, tx, args);
            return cmd.ExecuteNonQuery();
        }
    }

    private T Scalar<T>(string sql, params (string, object)[] args)
    {
        lock (sync)
        {
            using var cmd = Command(sql, null, args);
            var value = cmd.ExecuteScalar();
            return value is null or DBNull ? default! : (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
        }
    }
}
