using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>Why a trainer file or code is refused: the key of the words that say it, and what they name (the game's own words, never the file's).</summary>
public sealed record TrainerRefusal(string Key, string Value = "");

/// <summary>
/// A friend's trainer (plan 13 · V22; rulings, "Teams and trainers as text"): a name, a look, a class, two lines and a team,
/// as a text file or a code anyone can paste. It is untrusted input from its first byte, so it is read whole or refused:
/// a size limit, its shape, a checksum, the version of the data it was made with, and the legality of its team
/// (<see cref="BuiltTeams.WhyNot(BuiltTeam)"/>); no path in it is ever followed; and nothing of its own is shown but a name held
/// to the keyboard's rules. Its lines are two of a list of ours (<see cref="Phrases"/>), chosen by number, and its class is
/// one of a short list (<see cref="Classes"/>), so no text a stranger wrote reaches the screen but the name. GPU-free.
/// </summary>
public sealed class TrainerFile
{
    /// <summary>The first line of a file, and the format it says.</summary>
    public const string Magic = "PPTRAINER 1";

    /// <summary>The longest file or code read, in characters.</summary>
    public const int LongestText = 6000;

    /// <summary>The longest name: a trainer's keyboard takes ten (<see cref="BuiltTeams.NameLength"/>).</summary>
    public const int NameLength = BuiltTeams.NameLength;

    /// <summary>The folder beside the save the files are kept in, and the ending a file has.</summary>
    public const string FolderName = "trainers", Ending = ".trainer.txt";

    /// <summary>How many lines (<c>friends.line.N</c> in the table) a trainer may choose two from.</summary>
    public const int Phrases = 12;

    /// <summary>The classes a friend's trainer may be (all of them classes of <c>trainers.json</c>), by their English names.</summary>
    public static readonly IReadOnlyList<string> Classes = new[]
    {
        "Ace Trainer", "Youngster", "Lass", "Veteran", "Black Belt", "Psychic", "Collector", "Gentleman", "Beauty", "Hiker"
    };

    public string Name { get; set; } = "";
    public PlayerLook Look { get; set; }
    public string Class { get; set; } = Classes[0];

    /// <summary>The numbers of the two lines said before and after the battle.</summary>
    public int Before { get; set; }
    public int After { get; set; } = 1;

    /// <summary>The data version the team was made under (<see cref="GameDataFiles.Version"/>).</summary>
    public string DataVersion { get; set; } = "";

    /// <summary>The team, as a paste.</summary>
    public string Paste { get; set; } = "";

    /// <summary>The checksum the file states (read) or that was worked out (written), eight hexadecimal digits.</summary>
    public string Checksum { get; set; } = "";

    // ------------------------------------------------------------------ making one

    /// <summary>A file of a team under this game's data: the checksum worked out.</summary>
    public static TrainerFile Of(string name, PlayerLook look, string trainerClass, int before, int after, BuiltTeam team)
    {
        var file = new TrainerFile
        {
            Name = PlayerIdentity.Clean(name, NameLength), Look = look, Class = trainerClass, Before = before, After = after,
            DataVersion = GameDataFiles.Version, Paste = ShowdownPaste.Of(team)
        };
        file.Checksum = ChecksumOf(file.Body());
        return file;
    }

    /// <summary>The text of the file, the checksum in its line.</summary>
    public string ToText() => Body("Checksum: " + (Checksum.Length == 8 ? Checksum : ChecksumOf(Body())));

    private const string ZeroSum = "Checksum: 00000000";

    /// <summary>Whether a name is made of what the keyboard has: letters, digits and . - ' and the space.</summary>
    public static bool OnTheKeyboard(string name) => name.All(c => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '.' or '-' or '\'' or ' ');

    // The file with its checksum line zeroed (or given): what the checksum is of
    private string Body(string sumLine = ZeroSum)
    {
        var text = new StringBuilder();
        text.Append(Magic).Append('\n');
        text.Append("Name: ").Append(Name).Append('\n');
        text.Append("Look: ").Append(Look).Append('\n');
        text.Append("Class: ").Append(Class).Append('\n');
        text.Append("Line: ").Append(Before).Append('\n');
        text.Append("Line: ").Append(After).Append('\n');
        text.Append("Data: ").Append(DataVersion).Append('\n');
        text.Append(sumLine).Append('\n');
        text.Append("---\n");
        text.Append(Paste.Replace("\r\n", "\n").Trim());
        return text.ToString();
    }

    /// <summary>FNV-1a of the text as bytes, eight hexadecimal digits.</summary>
    public static string ChecksumOf(string text)
    {
        uint hash = 2166136261;
        foreach (byte b in Encoding.UTF8.GetBytes(text)) { hash ^= b; hash *= 16777619; }
        return hash.ToString("X8");
    }

    // ------------------------------------------------------------------ reading one

    /// <summary>Reads the text of a file; the file, or why it is refused. The team is checked as well (<see cref="Refuse"/>).</summary>
    public static (TrainerFile? File, TrainerRefusal? Refused) Read(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return (null, new("trainerFile.refused.empty"));
        if (text.Length > LongestText) return (null, new("trainerFile.refused.tooBig"));
        text = text.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        if (text.Any(c => c < ' ' && c != '\n' && c != '\t')) return (null, new("trainerFile.refused.shape"));

        int rule = text.IndexOf("\n---\n", StringComparison.Ordinal);
        if (rule < 0) return (null, new("trainerFile.refused.shape"));
        var head = text[..rule].Split('\n');
        if (head.Length != 8 || head[0] != Magic) return (null, new("trainerFile.refused.shape"));

        // The header is exactly these lines in this order; anything else is another file
        string? Field(int i, string key) => head[i].StartsWith(key + ": ", StringComparison.Ordinal) ? head[i][(key.Length + 2)..] : null;
        var file = new TrainerFile { Paste = text[(rule + 5)..] };
        string? name = Field(1, "Name"), look = Field(2, "Look"), cls = Field(3, "Class"), l1 = Field(4, "Line"), l2 = Field(5, "Line"), data = Field(6, "Data"), sum = Field(7, "Checksum");
        if (name == null || look == null || cls == null || l1 == null || l2 == null || data == null || sum == null) return (null, new("trainerFile.refused.shape"));
        if (sum.Length != 8 || !sum.All(Uri.IsHexDigit)) return (null, new("trainerFile.refused.shape"));

        // The checksum is of the file as it stands, so a changed character anywhere is found before anything else is believed
        string zeroed = string.Join("\n", head.Take(7).Append(ZeroSum)) + text[rule..];
        if (!string.Equals(ChecksumOf(zeroed), sum, StringComparison.OrdinalIgnoreCase)) return (null, new("trainerFile.refused.checksum"));

        file.Checksum = sum.ToUpperInvariant();
        file.Name = name;
        if (!Enum.TryParse<PlayerLook>(look, out var l) || !Enum.IsDefined(l)) return (null, new("trainerFile.refused.shape"));
        file.Look = l;
        file.Class = cls;
        if (!int.TryParse(l1, out int before) || !int.TryParse(l2, out int after)) return (null, new("trainerFile.refused.shape"));
        file.Before = before;
        file.After = after;
        file.DataVersion = data;
        return (file, Refuse(file));
    }

    /// <summary>Why a file read can't be used; null for one that can. In the order a friend would fix them.</summary>
    public static TrainerRefusal? Refuse(TrainerFile file)
    {
        if (file.Name.Length == 0 || file.Name != PlayerIdentity.Clean(file.Name, NameLength) || !OnTheKeyboard(file.Name))
            return new("trainerFile.refused.name");
        if (!Classes.Contains(file.Class)) return new("trainerFile.refused.class");
        if (file.Before is < 0 or >= Phrases || file.After is < 0 or >= Phrases) return new("trainerFile.refused.line");
        if (file.DataVersion != GameDataFiles.Version) return new("trainerFile.refused.version");
        var (team, problems, illegal) = ShowdownPaste.Team(file.Name, file.Paste);
        if (team == null)
        {
            if (problems.Count > 0) return new("trainerFile.refused.paste", problems[0].Line.ToString());
            return illegal is { } why ? new(why.Key, why.Value) : new("trainerFile.refused.shape");
        }
        return null;
    }

    /// <summary>The team of a file that was read, whole; null if it can't be made.</summary>
    public BuiltTeam? Team() => ShowdownPaste.Team(Name, Paste).Team;

    // ------------------------------------------------------------------ as a battle's opponent

    /// <summary>The level a friend's trainer and the player both fight at.</summary>
    public const int FlatLevel = 50;

    /// <summary>
    /// The trainer as an opponent: their Pokémon made afresh at <see cref="FlatLevel"/> on copies (IVs 31, no effort), the
    /// class's name, no prize and every routine of the trainer AI (<see cref="AiFlags"/> but the ones a battle adds itself).
    /// </summary>
    public Trainer ToTrainer()
    {
        var team = Team() ?? throw new InvalidOperationException("A file that can't battle is made no trainer.");
        var trainer = new Trainer
        {
            Id = "friend_" + Checksum, Name = Name, TrainerClass = Class, PrizeMoney = 0,
            Ai = AiFlags.Basic | AiFlags.EvalAttack | AiFlags.Expert | AiFlags.SetupFirstTurn | AiFlags.Risky | AiFlags.PrioritizeExtremes
                | AiFlags.BatonPass | AiFlags.CheckHp | AiFlags.Weather | AiFlags.Harassment,
            Party = LevelScaling.TeamAt(BuiltTeams.PartyOf(team).Members, FlatLevel)
        };
        return trainer;
    }

    // ------------------------------------------------------------------ as a code

    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>The file packed (deflated) and written in base 32, in groups of five letters, to be pasted anywhere.</summary>
    public string ToCode()
    {
        var raw = Encoding.UTF8.GetBytes(ToText());
        using var packed = new MemoryStream();
        using (var deflate = new DeflateStream(packed, CompressionLevel.SmallestSize, true)) deflate.Write(raw, 0, raw.Length);
        var chars = new StringBuilder();
        int buffer = 0, bits = 0;
        foreach (byte b in packed.ToArray())
        {
            buffer = (buffer << 8) | b;
            bits += 8;
            while (bits >= 5) { chars.Append(Alphabet[(buffer >> (bits - 5)) & 31]); bits -= 5; }
            buffer &= (1 << bits) - 1;
        }
        if (bits > 0) chars.Append(Alphabet[(buffer << (5 - bits)) & 31]);
        var grouped = new StringBuilder();
        for (int i = 0; i < chars.Length; i++)
        {
            if (i > 0) grouped.Append(i % 40 == 0 ? '\n' : i % 5 == 0 ? '-' : "");
            grouped.Append(chars[i]);
        }
        return grouped.ToString();
    }

    /// <summary>Reads a code (separators and case are let go): the file it holds, or why it is refused.</summary>
    public static (TrainerFile? File, TrainerRefusal? Refused) ReadCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return (null, new("trainerFile.refused.empty"));
        if (code.Length > LongestText) return (null, new("trainerFile.refused.tooBig"));
        var bytes = new List<byte>();
        int buffer = 0, bits = 0;
        foreach (char raw in code)
        {
            if (raw is '-' or '\n' or '\r' or ' ' or '\t') continue;
            int v = Alphabet.IndexOf(char.ToUpperInvariant(raw));
            if (v < 0) return (null, new("trainerFile.refused.code"));
            buffer = (buffer << 5) | v;
            bits += 5;
            if (bits >= 8) { bytes.Add((byte)((buffer >> (bits - 8)) & 255)); bits -= 8; buffer &= (1 << bits) - 1; }
        }
        try
        {
            using var inflate = new DeflateStream(new MemoryStream(bytes.ToArray()), CompressionMode.Decompress);
            // Never believe how big the file says it is: read one more than the most and refuse a longer one
            var buffer2 = new byte[LongestText + 1];
            int total = 0, got;
            while (total < buffer2.Length && (got = inflate.Read(buffer2, total, buffer2.Length - total)) > 0) total += got;
            if (total > LongestText) return (null, new("trainerFile.refused.tooBig"));
            return Read(new UTF8Encoding(false, true).GetString(buffer2, 0, total));
        }
        catch (Exception e) when (e is InvalidDataException or ArgumentException or IOException)
        {
            return (null, new("trainerFile.refused.code"));
        }
    }
}

/// <summary>
/// The friends' trainers beside the save (plan 13 · V22): <c>trainers/*.trainer.txt</c> read in the order of their names,
/// never in the order the file system lists them, which differs between machines. Each is read whole or refused, a file
/// larger than a trainer file can be is refused before it is read, and a file the folder holds that isn't a trainer's
/// is left alone. GPU-free.
/// </summary>
public static class TrainerFiles
{
    /// <summary>The most files read from the folder.</summary>
    public const int Most = 40;

    /// <summary>One file of the folder: its name as the folder has it, the trainer if it is one, or why it is refused.</summary>
    public sealed record Entry(string FileName, TrainerFile? File, TrainerRefusal? Refused);

    /// <summary>The folder's files in the order of their names (ordinal, so the same on every machine).</summary>
    public static IReadOnlyList<Entry> In(string folder)
    {
        var list = new List<Entry>();
        if (!Directory.Exists(folder)) return list;
        string[] paths;
        try { paths = Directory.GetFiles(folder, "*" + TrainerFile.Ending); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return list; }
        foreach (string path in paths.OrderBy(Path.GetFileName, StringComparer.Ordinal).Take(Most))
        {
            string name = Path.GetFileName(path);
            try
            {
                if (new FileInfo(path).Length > TrainerFile.LongestText * 4L) { list.Add(new(name, null, new("trainerFile.refused.tooBig"))); continue; }
                var (file, refused) = TrainerFile.Read(File.ReadAllText(path, new UTF8Encoding(false, false)));
                list.Add(new(name, refused == null ? file : null, refused));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                list.Add(new(name, null, new("trainerFile.refused.empty")));
            }
        }
        return list;
    }

    /// <summary>Writes a trainer's file into the folder, whole and beside first; the path it went to (named for the trainer, never for anything the file says).</summary>
    public static string Write(string folder, TrainerFile file)
    {
        Directory.CreateDirectory(folder);
        string stem = new string(file.Name.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
        string path = Path.Combine(folder, stem + TrainerFile.Ending);
        string temp = path + ".tmp";
        File.WriteAllText(temp, file.ToText(), new UTF8Encoding(false));
        File.Move(temp, path, overwrite: true);
        return path;
    }
}
