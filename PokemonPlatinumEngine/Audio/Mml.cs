using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PokemonPlatinumEngine.Audio;

/// <summary>
/// Reads songs written in a small Music Macro Language, the text format chiptune and DS-era composers used.
/// <c>docs/music-format.md</c> describes it; in short:
/// <code>
/// title Twinleaf Town
/// tempo 96
/// track lead flute night=ocarina vol=0.8 pan=-0.2
///   o5 l8 | L c d e4 g4. e | (c e g)2 r2 |
/// </code>
/// Notes are <c>c d e f g a b</c> with <c>+</c>/<c>#</c> or <c>-</c>, a length (4 = quarter) and dots; <c>r</c> rests;
/// <c>o</c> sets the octave and <c>&lt;</c>/<c>&gt;</c> move it; <c>l</c> sets the default length, <c>v</c> the velocity
/// (0–127), <c>q</c> how much of each note sounds (1–8); <c>^</c> or <c>&amp;</c> tie; <c>(c e g)</c> is a chord;
/// <c>[ … ]3</c> repeats; <c>L</c> marks the loop start; <c>|</c> checks that a bar ends there.
/// The drum kit's letters are b kick, s snare, h hi-hat, o open hat, c crash, t m f toms, x clap and k shaker.
/// </summary>
public static class Mml
{
    public static Song Parse(string id, string text)
    {
        string title = id;
        double tempo = 120, nightTempo = 0.95;
        int beatsPerBar = 4, beatUnit = 4;
        float reverb = 0.3f;
        var trackHeaders = new List<(string Header, StringBuilder Body, int Line)>();

        var lines = text.Replace("\r", "").Split('\n');
        for (int n = 0; n < lines.Length; n++)
        {
            string line = lines[n];
            int hash = line.IndexOf('#');
            if (hash >= 0) line = line[..hash];
            string trimmed = line.Trim();
            if (trimmed.Length == 0) continue;

            string word = FirstWord(trimmed, out string rest);
            if (word == "track")
            {
                trackHeaders.Add((rest, new StringBuilder(), n + 1));
                continue;
            }
            if (trackHeaders.Count > 0 && char.IsWhiteSpace(line[0]))
            {
                trackHeaders[^1].Body.Append(' ').Append(trimmed);
                continue;
            }

            switch (word)
            {
                case "title": title = rest; break;
                case "tempo": tempo = ParseDouble(rest, id, n); break;
                case "nighttempo": nightTempo = ParseDouble(rest, id, n); break;
                case "reverb": reverb = (float)ParseDouble(rest, id, n); break;
                case "meter":
                {
                    var parts = rest.Split('/');
                    if (parts.Length != 2 || !int.TryParse(parts[0], out beatsPerBar) || !int.TryParse(parts[1], out beatUnit))
                        throw Error(id, n, $"bad meter '{rest}'");
                    break;
                }
                default:
                    throw Error(id, n, $"unknown line '{trimmed}' (track bodies must be indented)");
            }
        }

        int ticksPerBar = Song.TicksPerWhole * beatsPerBar / beatUnit;
        var tracks = new List<(SongTrack Track, int End, int Loop)>();
        foreach (var (header, body, line) in trackHeaders)
        {
            var track = ParseTrackHeader(id, header, line);
            var (end, loop) = ParseBody(id, track, Expand(body.ToString(), id, track.Name), ticksPerBar);
            tracks.Add((track, end, loop));
        }

        if (tracks.Count == 0) throw new FormatException($"{id}: no tracks");
        int endTick = tracks[0].End;
        int loopTick = -1;
        foreach (var (track, end, loop) in tracks)
        {
            if (end != endTick)
                throw new FormatException($"{id}: track '{track.Name}' is {Bars(end, ticksPerBar)} bars long but '{tracks[0].Track.Name}' is {Bars(endTick, ticksPerBar)}");
            if (loop >= 0)
            {
                if (loopTick >= 0 && loop != loopTick)
                    throw new FormatException($"{id}: track '{track.Name}' loops at bar {Bars(loop, ticksPerBar) + 1}, another track elsewhere");
                loopTick = loop;
            }
        }

        var song = new Song
        {
            Id = id, Title = title, Tempo = tempo, NightTempo = nightTempo, TicksPerBar = ticksPerBar,
            LoopTick = loopTick, EndTick = endTick, Reverb = reverb
        };
        foreach (var t in tracks) song.Tracks.Add(t.Track);
        return song;
    }

    private static string Bars(int ticks, int ticksPerBar) => (ticks / (double)ticksPerBar).ToString("0.##", CultureInfo.InvariantCulture);

    private static SongTrack ParseTrackHeader(string id, string header, int line)
    {
        var parts = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) throw Error(id, line - 1, "a track needs a name and an instrument");
        var inst = InstrumentBank.Get(parts[1]) ?? throw Error(id, line - 1, $"unknown instrument '{parts[1]}'");
        Instrument? night = null;
        float vol = 0.8f, pan = 0f, rev = 0.3f;
        for (int i = 2; i < parts.Length; i++)
        {
            var kv = parts[i].Split('=');
            if (kv.Length != 2) throw Error(id, line - 1, $"bad track option '{parts[i]}'");
            switch (kv[0])
            {
                case "night": night = InstrumentBank.Get(kv[1]) ?? throw Error(id, line - 1, $"unknown instrument '{kv[1]}'"); break;
                case "vol": vol = (float)ParseDouble(kv[1], id, line - 1); break;
                case "pan": pan = (float)ParseDouble(kv[1], id, line - 1); break;
                case "rev": rev = (float)ParseDouble(kv[1], id, line - 1); break;
                default: throw Error(id, line - 1, $"unknown track option '{kv[0]}'");
            }
        }
        return new SongTrack { Name = parts[0], Instrument = inst, NightInstrument = night, Volume = vol, Pan = pan, ReverbSend = rev };
    }

    /// <summary>Writes out <c>[ … ]n</c> repeats, innermost first.</summary>
    private static string Expand(string body, string id, string track)
    {
        while (true)
        {
            int close = body.IndexOf(']');
            if (close < 0)
            {
                if (body.Contains('[')) throw new FormatException($"{id}/{track}: unclosed '['");
                return body;
            }
            int open = body.LastIndexOf('[', close);
            if (open < 0) throw new FormatException($"{id}/{track}: ']' without '['");
            int i = close + 1;
            while (i < body.Length && char.IsDigit(body[i])) i++;
            int count = i > close + 1 ? int.Parse(body[(close + 1)..i]) : 2;
            string inner = body[(open + 1)..close];
            var sb = new StringBuilder();
            for (int k = 0; k < count; k++) sb.Append(' ').Append(inner).Append(' ');
            body = body[..open] + sb + body[i..];
        }
    }

    private static (int End, int Loop) ParseBody(string id, SongTrack track, string s, int ticksPerBar)
    {
        bool kit = track.IsDrums;
        int pos = 0, tick = 0, octave = 4, defaultLength = Song.TicksPerQuarter, gate = 7, loop = -1;
        float velocity = 100f / 127f;

        FormatException Fail(string message) =>
            new($"{id}/{track.Name}: {message} near bar {tick / ticksPerBar + 1} ('{Context(s, pos)}')");

        while (true)
        {
            SkipSpace(s, ref pos);
            if (pos >= s.Length) break;
            char c = char.ToLowerInvariant(s[pos]);
            char raw = s[pos];

            if (raw == '|')
            {
                pos++;
                if (tick % ticksPerBar != 0) throw Fail($"bar line {tick % ticksPerBar} ticks into a bar");
                continue;
            }
            if (raw == 'L')
            {
                pos++;
                loop = tick;
                continue;
            }
            if (c == 'l')
            {
                pos++;
                defaultLength = ReadLength(s, ref pos, defaultLength, Fail);
                continue;
            }
            if (c == 'v')
            {
                pos++;
                velocity = Math.Clamp(ReadInt(s, ref pos, Fail), 0, 127) / 127f;
                continue;
            }
            if (c == 'q')
            {
                pos++;
                gate = Math.Clamp(ReadInt(s, ref pos, Fail), 1, 8);
                continue;
            }
            if (!kit && c == 'o')
            {
                pos++;
                octave = ReadInt(s, ref pos, Fail);
                continue;
            }
            if (!kit && raw == '>') { pos++; octave++; continue; }
            if (!kit && raw == '<') { pos++; octave--; continue; }

            if (raw == '(')
            {
                pos++;
                var keys = new List<int>();
                int chordOctave = octave;
                while (true)
                {
                    SkipSpace(s, ref pos);
                    if (pos >= s.Length) throw Fail("unclosed chord");
                    if (s[pos] == ')') { pos++; break; }
                    if (!kit && s[pos] == '>') { pos++; chordOctave++; continue; }
                    if (!kit && s[pos] == '<') { pos++; chordOctave--; continue; }
                    keys.Add(ReadKey(s, ref pos, chordOctave, kit, Fail));
                }
                int length = ReadNoteLength(s, ref pos, defaultLength, Fail);
                foreach (int key in keys)
                    track.Notes.Add(new NoteEvent(tick, length, GateTicks(length, gate), key, velocity));
                tick += length;
                continue;
            }

            if (c == 'r')
            {
                pos++;
                tick += ReadNoteLength(s, ref pos, defaultLength, Fail);
                continue;
            }

            int k = ReadKey(s, ref pos, octave, kit, Fail);
            int len = ReadNoteLength(s, ref pos, defaultLength, Fail);
            track.Notes.Add(new NoteEvent(tick, len, GateTicks(len, gate), k, velocity));
            tick += len;
        }

        return (tick, loop);
    }

    private static int GateTicks(int length, int gate) => gate >= 8 ? length : Math.Max(1, length * gate / 8);

    private static int ReadKey(string s, ref int pos, int octave, bool kit, Func<string, FormatException> fail)
    {
        char c = char.ToLowerInvariant(s[pos]);
        if (kit)
        {
            Drum? drum = c switch
            {
                'b' => Drum.Kick, 's' => Drum.Snare, 'h' => Drum.HiHat, 'o' => Drum.OpenHat, 'c' => Drum.Crash,
                't' => Drum.HighTom, 'm' => Drum.MidTom, 'f' => Drum.LowTom, 'x' => Drum.Clap, 'k' => Drum.Shaker,
                _ => null
            };
            if (drum == null) throw fail($"'{c}' is not a drum");
            pos++;
            return (int)drum.Value;
        }

        int pc = c switch { 'c' => 0, 'd' => 2, 'e' => 4, 'f' => 5, 'g' => 7, 'a' => 9, 'b' => 11, _ => -1 };
        if (pc < 0) throw fail($"unexpected '{s[pos]}'");
        pos++;
        while (pos < s.Length && (s[pos] == '+' || s[pos] == '#' || s[pos] == '-'))
        {
            pc += s[pos] == '-' ? -1 : 1;
            pos++;
        }
        return 12 * (octave + 1) + pc;
    }

    /// <summary>A length with its ties: <c>4.</c>, <c>4^16</c>, <c>2&amp;c8</c>.</summary>
    private static int ReadNoteLength(string s, ref int pos, int defaultLength, Func<string, FormatException> fail)
    {
        int length = ReadLength(s, ref pos, defaultLength, fail);
        while (pos < s.Length && (s[pos] == '^' || s[pos] == '&'))
        {
            pos++;
            SkipSpace(s, ref pos);
            // "&c8": the tied note's pitch is the same one, so only its length matters
            if (pos < s.Length && char.IsLetter(s[pos]))
            {
                pos++;
                while (pos < s.Length && (s[pos] == '+' || s[pos] == '#' || s[pos] == '-')) pos++;
            }
            length += ReadLength(s, ref pos, defaultLength, fail);
        }
        return length;
    }

    private static int ReadLength(string s, ref int pos, int defaultLength, Func<string, FormatException> fail)
    {
        int length = defaultLength;
        if (pos < s.Length && char.IsDigit(s[pos]))
        {
            int n = ReadInt(s, ref pos, fail);
            if (n <= 0 || Song.TicksPerWhole % n != 0) throw fail($"length {n} doesn't divide a whole note");
            length = Song.TicksPerWhole / n;
        }
        int add = length;
        while (pos < s.Length && s[pos] == '.')
        {
            pos++;
            add /= 2;
            length += add;
        }
        return length;
    }

    private static int ReadInt(string s, ref int pos, Func<string, FormatException> fail)
    {
        int start = pos;
        while (pos < s.Length && char.IsDigit(s[pos])) pos++;
        if (pos == start) throw fail("expected a number");
        return int.Parse(s[start..pos], CultureInfo.InvariantCulture);
    }

    private static void SkipSpace(string s, ref int pos)
    {
        while (pos < s.Length && char.IsWhiteSpace(s[pos])) pos++;
    }

    private static string Context(string s, int pos)
    {
        int from = Math.Max(0, pos - 12), to = Math.Min(s.Length, pos + 12);
        return s[from..to].Trim();
    }

    private static string FirstWord(string line, out string rest)
    {
        int space = line.IndexOfAny(new[] { ' ', '\t' });
        if (space < 0)
        {
            rest = "";
            return line.ToLowerInvariant();
        }
        rest = line[(space + 1)..].Trim();
        return line[..space].ToLowerInvariant();
    }

    private static double ParseDouble(string s, string id, int line) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : throw Error(id, line, $"bad number '{s}'");

    private static FormatException Error(string id, int line, string message) => new($"{id}, line {line + 1}: {message}");
}
