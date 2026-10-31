using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PokemonPlatinumEngine.Models;

/// <summary>One section of the title's credits: its heading and its paragraphs, each a line before it is wrapped.</summary>
public sealed record CreditsSection(string Heading, IReadOnlyList<string> Lines);

/// <summary>
/// Whose work is in the game, as the title's CREDITS shows it (plan 16 · T1; style guide, "Menu screens", "Credits"):
/// the two records the repository keeps, <c>docs/art/CREDITS.md</c> and <c>THIRD-PARTY-NOTICES.md</c>, copied beside the
/// executable into <c>Credits/</c> by the engine's project, read into sections and plain lines. GPU-free, so tests
/// read them as the game does. The roll after the Hall of Fame is another thing (<c>CreditsDatabase</c>, plan 02 · S14).
/// </summary>
public static class TitleCredits
{
    /// <summary>The two files, in the order the screen shows them.</summary>
    public static readonly string[] Files = { "CREDITS.md", "THIRD-PARTY-NOTICES.md" };

    /// <summary>Where the build puts them: <c>Credits/</c> beside the executable.</summary>
    public static string Folder => Path.Combine(AppContext.BaseDirectory, "Credits");

    /// <summary>
    /// The sections of both files, in order. A file that isn't there is passed over, so a build without them shows
    /// what the screen itself says and nothing else; it never throws.
    /// </summary>
    public static List<CreditsSection> Read(string? folder = null)
    {
        folder ??= Folder;
        var sections = new List<CreditsSection>();
        foreach (var name in Files)
        {
            string path = Path.Combine(folder, name);
            if (!File.Exists(path)) continue;
            try { sections.AddRange(Parse(File.ReadAllText(path))); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        return sections;
    }

    /// <summary>
    /// A Markdown file into sections: its first heading and every <c>## </c> heading begins one; a paragraph's lines are
    /// one line; a list's item is a line; a table's row is a line of its cells ("Header: value", joined by " · ") with
    /// its heading row and rule left out; a fenced block's paragraphs are lines as written. Emphasis, code marks and
    /// links are taken off (<see cref="Plain"/>). A section with nothing in it but its heading is kept only if it has a
    /// heading worth showing.
    /// </summary>
    public static List<CreditsSection> Parse(string markdown)
    {
        var sections = new List<CreditsSection>();
        string? heading = null;
        var lines = new List<string>();
        var paragraph = new StringBuilder();
        string[]? tableHeader = null;
        bool fenced = false;

        void EndParagraph()
        {
            if (paragraph.Length > 0) lines.Add(Plain(paragraph.ToString()));
            paragraph.Clear();
        }
        void EndSection()
        {
            EndParagraph();
            var kept = lines.Where(l => l.Length > 0).ToList();
            if (heading != null && (kept.Count > 0 || heading.Length > 0)) sections.Add(new CreditsSection(heading, kept));
            lines = new List<string>();
        }

        foreach (var raw in markdown.Replace("\r", "").Split('\n'))
        {
            string line = raw.TrimEnd();
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                EndParagraph();
                fenced = !fenced;
                continue;
            }
            if (fenced)
            {
                // A licence's text: its paragraphs as written, the line breaks inside one joined
                if (line.Trim().Length == 0) EndParagraph();
                else paragraph.Append(paragraph.Length > 0 ? " " : "").Append(line.Trim());
                continue;
            }
            if (line.StartsWith("# ", StringComparison.Ordinal) || line.StartsWith("## ", StringComparison.Ordinal))
            {
                EndSection();
                heading = Plain(line.TrimStart('#').Trim());
                tableHeader = null;
                continue;
            }
            if (line.StartsWith("#", StringComparison.Ordinal))
            {
                // A smaller heading is a line of its own in the section
                EndParagraph();
                lines.Add(Plain(line.TrimStart('#').Trim()));
                continue;
            }
            if (line.StartsWith("|", StringComparison.Ordinal))
            {
                EndParagraph();
                var cells = Cells(line);
                if (cells.All(c => c.Trim().Trim(':').Trim('-').Length == 0 && c.Contains('-'))) continue; // the rule under the heading row
                if (tableHeader == null)
                {
                    tableHeader = cells.Select(Plain).ToArray();
                    continue;
                }
                var parts = new List<string>();
                for (int i = 0; i < cells.Length; i++)
                {
                    string value = Plain(cells[i]);
                    if (value.Length == 0) continue;
                    string head = i < tableHeader.Length ? tableHeader[i] : "";
                    parts.Add(head.Length > 0 ? $"{head}: {value}" : value);
                }
                if (parts.Count > 0) lines.Add(string.Join(" · ", parts));
                continue;
            }
            tableHeader = null;
            if (line.Trim().Length == 0)
            {
                EndParagraph();
                continue;
            }
            string trimmed = line.TrimStart();
            if (trimmed.StartsWith("- ", StringComparison.Ordinal) || trimmed.StartsWith("* ", StringComparison.Ordinal))
            {
                EndParagraph();
                paragraph.Append(trimmed[2..].Trim());
                continue;
            }
            paragraph.Append(paragraph.Length > 0 ? " " : "").Append(trimmed);
        }
        EndSection();
        return sections;
    }

    // A table row's cells, its outer bars left off; "\|" stays a bar inside a cell
    private static string[] Cells(string row)
    {
        string inner = row.Trim();
        if (inner.StartsWith('|')) inner = inner[1..];
        if (inner.EndsWith('|') && !inner.EndsWith("\\|", StringComparison.Ordinal)) inner = inner[..^1];
        var cells = new List<string>();
        var cell = new StringBuilder();
        for (int i = 0; i < inner.Length; i++)
        {
            if (inner[i] == '\\' && i + 1 < inner.Length && inner[i + 1] == '|')
            {
                cell.Append('|');
                i++;
            }
            else if (inner[i] == '|')
            {
                cells.Add(cell.ToString());
                cell.Clear();
            }
            else cell.Append(inner[i]);
        }
        cells.Add(cell.ToString());
        return cells.ToArray();
    }

    private static readonly Regex Link = new(@"\[([^\]]*)\]\(([^)]*)\)", RegexOptions.Compiled);
    private static readonly Regex Angle = new(@"<(https?://[^>]+)>", RegexOptions.Compiled);
    private static readonly Regex Spaces = new(@"\s{2,}", RegexOptions.Compiled);

    /// <summary>A line of Markdown as plain text: links as their words, bare links as the address, no emphasis or code marks.</summary>
    public static string Plain(string text)
    {
        string s = Link.Replace(text, m => m.Groups[1].Value);
        s = Angle.Replace(s, m => m.Groups[1].Value);
        s = s.Replace("**", "").Replace("__", "").Replace("`", "");
        return Spaces.Replace(s, " ").Trim();
    }
}
