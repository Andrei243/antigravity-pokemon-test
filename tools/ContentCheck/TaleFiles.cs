using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace ContentCheck;

/// <summary>
/// The Tales' file (plan 46 · CP11): <c>Data/tales.json</c> held to its rules (read strictly; turns above nought; an id
/// of letters, once; its four words in the table, <c>basme.&lt;id&gt;.name</c>, <c>.told</c>, <c>.rule</c> and <c>.story</c>;
/// a rule that does something; a bonus above nothing; an accuracy of 1 to 100; a sky of three colours of 0 to 255; and every
/// Tale Form a form of its species).
/// </summary>
public static class TaleFiles
{
    public static void Check(GameContent content, List<Finding> found)
    {
        string path = Path.Combine(content.Root, "tales.json");
        if (!File.Exists(path)) return;
        string file = content.Relative(path);
        TalesFile data;
        try
        {
            data = GameDataFiles.ReadStrict<TalesFile>(File.ReadAllText(path), file);
        }
        catch (InvalidDataException e)
        {
            found.Add(new Finding(Kind.Unreadable, file, "tales", e.Message));
            return;
        }

        void Add(string name, string what) => found.Add(new Finding(Kind.Data, file, name, what));
        if (data.Turns < 1) Add("turns", "is under one: a tale would hold for no turn");
        foreach (var group in data.Tales.GroupBy(t => t.Id).Where(g => g.Count() > 1)) Add(group.Key, "is listed twice");
        foreach (var tale in data.Tales)
        {
            if (tale.Id.Length == 0 || !tale.Id.All(char.IsAsciiLetter))
                Add(tale.Id, "is no id of letters (its words are basme.<id>.name)");
            foreach (string word in new[] { "name", "told", "rule", "story" })
                if (!Text.Has($"basme.{tale.Id}.{word}")) Add(tale.Id, $"has no {word} (basme.{tale.Id}.{word} in ui.json)");
            if (tale.Boost == null && tale.Weather == null && tale.Accuracy == 100 && !tale.NoNight && tale.Forms.Count == 0)
                Add(tale.Id, "does nothing: no bonus, weather, accuracy, sun or form");
            if (tale.Boost is { } boost)
            {
                if (boost.Percent <= 0) Add(tale.Id, "has a bonus of nothing or less");
                if (boost.Types.Count == 0 && boost.Flags.Count == 0) Add(tale.Id, "has a bonus for no type and no flag");
            }
            if (tale.Accuracy is < 1 or > 100) Add(tale.Id, "has an accuracy outside 1 to 100");
            if (tale.Sky is { } sky && (sky.Color.Length != 3 || sky.Color.Any(c => c is < 0 or > 255) || sky.Amount is < 0 or > 1 || sky.Grey is < 0 or > 1))
                Add(tale.Id, "has a sky that is not three colours of 0 to 255 with an amount and a grey of 0 to 1");
            foreach (var bound in tale.Forms)
            {
                var species = PokemonDatabase.Get(bound.Species);
                if (species == null) Add(tale.Id, $"binds {bound.Species}, which is no species");
                else if (species.Form(bound.Form) == null) Add(tale.Id, $"wakes {bound.Species} into {bound.Form}, which is no form of it");
            }
        }
    }
}
