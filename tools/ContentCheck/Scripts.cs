using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Story;

namespace ContentCheck;

/// <summary>
/// The scripts (<c>scripts/&lt;place&gt;.txt</c>): what <see cref="ScriptLibrary.Problems"/> says of them (a script called
/// that doesn't exist, an item, a species, a trainer, a map, a song or a sound nobody has heard of), that each file is
/// named for a place the game has, and that every line they say fits the text box and can be drawn.
/// </summary>
public static class Scripts
{
    public static void Check(GameContent content, List<Finding> found)
    {
        var scripts = content.Scripts;
        string FileOf(string file) => $"{ScriptLibrary.Folder}/{file}.txt";

        // "file.txt(12): what."
        foreach (string problem in scripts.Problems(mapExists: name => content.MapNamed(name) != null, songExists: MusicLibrary.Exists, soundExists: AudioManager.SoundNames.Contains))
        {
            int bracket = problem.IndexOf(".txt(", StringComparison.Ordinal), close = problem.IndexOf("): ", StringComparison.Ordinal);
            found.Add(bracket > 0 && close > bracket
                ? new Finding(Kind.ScriptProblem, FileOf(problem[..bracket]), "line " + problem[(bracket + 5)..close], problem[(close + 3)..].TrimEnd('.'))
                : new Finding(Kind.ScriptProblem, ScriptLibrary.Folder, "scripts", problem));
        }

        // A file is looked in for the place it is named for: an open area, or a room. One named for nowhere is never read
        var open = content.Regions.SelectMany(r => r.World.Index.Areas).ToHashSet(StringComparer.OrdinalIgnoreCase);
        // (the Pokégear's calls are a file of their own, which the game starts by its name, plan 20 · J3)
        foreach (string file in scripts.Files.Where(f => f != ScriptLibrary.Common && f != PokemonPlatinumEngine.Core.GameEngine.PhoneScripts).OrderBy(f => f, StringComparer.Ordinal))
            if (!open.Contains(file) && content.MapNamed(file) == null)
                found.Add(new Finding(Kind.Misplaced, FileOf(file), file, "is named for no open area and no map: nothing starts its scripts"));

        foreach (var script in scripts.All.OrderBy(s => s.File, StringComparer.Ordinal).ThenBy(s => s.Line))
        {
            // Who the box may name as a line of the script is said (a line that begins with the speaker's name drops it)
            string?[] speakers = script.Everything().Where(i => i.Op == Op.Speaker && i.Name.Length > 0).Select(i => (string?)i.Name).Distinct().ToArray();
            foreach (var i in script.Everything())
            {
                string name = $"line {i.Line} ({script.Name})";
                switch (i.Op)
                {
                    case Op.Say or Op.Text:
                        foreach (string line in i.Lines) Lines.Check(found, FileOf(script.File), name, line, speakers);
                        break;
                    case Op.Ask or Op.Choose:
                        Lines.Check(found, FileOf(script.File), name, i.Lines[0], speakers);
                        foreach (string answer in i.Lines.Skip(1)) Lines.CheckAnswer(found, FileOf(script.File), name, answer);
                        break;
                    case Op.Speaker when i.Name.Length > 0:
                        Lines.CheckName(found, FileOf(script.File), name, i.Name);
                        break;
                }
            }
        }
    }
}
