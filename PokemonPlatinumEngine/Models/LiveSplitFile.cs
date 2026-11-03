using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// A LiveSplit splits file of the run (plan 13 · V24; rulings, "The live feed, the overlay and the splits file"): the
/// <c>.lss</c> LiveSplit opens, with a segment for every split point the category counts and, for each one the run has
/// taken, its time in the Personal Best column, so LiveSplit's own comparison is this run so far. The game's real time
/// is LiveSplit's Real Time and the game's own the Game Time. The plan sent splits to LiveSplit Server over a socket;
/// this writes the file the program also reads, and so opens no connection. Written whole; never read back. GPU-free.
/// </summary>
public static class LiveSplitFile
{
    /// <summary>The ending of the file, and the version of LiveSplit's format it is written in.</summary>
    public const string Ending = ".lss", FormatVersion = "1.7.0";

    /// <summary>The file of a timer under a category (a run with the row off is written as the Badges').</summary>
    public static string Of(RunTimer timer, TimerCategory category, string game)
    {
        if (category == TimerCategory.Off) category = TimerCategory.Badges;
        var taken = timer.In(category).ToDictionary(p => p.Point.Id, p => p.Split);
        var settings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false), OmitXmlDeclaration = false };
        var text = new StringBuilder();
        using (var xml = XmlWriter.Create(new Utf8Writer(text), settings))
        {
            xml.WriteStartDocument();
            xml.WriteStartElement("Run");
            xml.WriteAttributeString("version", FormatVersion);
            xml.WriteElementString("GameIcon", "");
            xml.WriteElementString("GameName", game);
            xml.WriteElementString("CategoryName", category switch
            {
                TimerCategory.HallOfFame => "Hall of Fame",
                TimerCategory.Story => "Story",
                _ => "Badges"
            });
            xml.WriteStartElement("Metadata");
            xml.WriteStartElement("Run");
            xml.WriteAttributeString("id", "");
            xml.WriteEndElement();
            xml.WriteStartElement("Platform");
            xml.WriteAttributeString("usesEmulator", "False");
            xml.WriteString("PC");
            xml.WriteEndElement();
            xml.WriteElementString("Region", "");
            xml.WriteElementString("Variables", "");
            xml.WriteEndElement();
            xml.WriteElementString("Offset", "00:00:00");
            xml.WriteElementString("AttemptCount", "1");
            xml.WriteElementString("AttemptHistory", "");
            xml.WriteStartElement("Segments");
            foreach (var point in RunTimer.Points.Where(p => p.CountsIn(category)))
            {
                xml.WriteStartElement("Segment");
                xml.WriteElementString("Name", point.Shown);
                xml.WriteElementString("Icon", "");
                xml.WriteStartElement("SplitTimes");
                xml.WriteStartElement("SplitTime");
                xml.WriteAttributeString("name", "Personal Best");
                if (taken.TryGetValue(point.Id, out var split))
                {
                    xml.WriteElementString("RealTime", Time(split.RealSeconds));
                    xml.WriteElementString("GameTime", Time(split.GameSeconds));
                }
                xml.WriteEndElement();
                xml.WriteEndElement();
                xml.WriteElementString("BestSegmentTime", "");
                xml.WriteElementString("SegmentHistory", "");
                xml.WriteEndElement();
            }
            xml.WriteEndElement();
            xml.WriteElementString("AutoSplitterSettings", "");
            xml.WriteEndElement();
            xml.WriteEndDocument();
        }
        return text.ToString();
    }

    /// <summary>A time as LiveSplit writes one: <c>HH:MM:SS.fffffff</c>, hours running past a day.</summary>
    public static string Time(double seconds)
    {
        if (seconds < 0 || double.IsNaN(seconds)) seconds = 0;
        var span = TimeSpan.FromSeconds(Math.Round(seconds, 3));
        long hours = (long)span.TotalHours;
        return string.Create(CultureInfo.InvariantCulture, $"{hours:D2}:{span.Minutes:D2}:{span.Seconds:D2}.{span.Milliseconds * 10000:D7}");
    }

    // A StringWriter that says it is UTF-8, so the declaration says what the file is
    private sealed class Utf8Writer : StringWriter
    {
        public Utf8Writer(StringBuilder builder) : base(builder, CultureInfo.InvariantCulture) { }
        public override Encoding Encoding => new UTF8Encoding(false);
    }
}
