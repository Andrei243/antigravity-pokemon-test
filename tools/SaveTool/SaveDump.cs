using System.Globalization;
using System.Text;
using PokemonPlatinumEngine.Core;

namespace SaveTool;

/// <summary>A save written out for a person to read (plan 12 · Q3): who, where, how far, the team, the boxes, the bag.</summary>
public static class SaveDump
{
    public static string Describe(SaveData save, bool everything = false)
    {
        var text = new StringBuilder();
        var badges = save.RegionalBadges();
        text.AppendLine($"{save.PlayerName} ({save.Look}), rival {save.RivalName}, ${save.Money}, trainer {save.TrainerId}");
        text.AppendLine($"played {Time(save.PlayTimeSeconds)}; began {save.Started?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "(unknown)"} in {save.BeganInOrHere()}; {save.Rules} rules");
        text.AppendLine($"versions: world {save.WorldVersion}/{SaveData.CurrentWorld}, story {save.StoryVersion}/{PokemonPlatinumEngine.Story.StoryState.CurrentVersion}, save {save.SaveVersion}/{SaveData.CurrentSave}");
        text.AppendLine($"at {save.CurrentMapName} {save.PlayerGridX},{save.PlayerGridY} facing {save.PlayerFacing}, {save.Travel}");
        text.AppendLine("badges: " + (badges.Count == 0 ? "none" : string.Join(", ", badges.OrderBy(b => b.Key, StringComparer.Ordinal).Select(b => $"{b.Key} {System.Numerics.BitOperations.PopCount((uint)b.Value)}"))));
        text.AppendLine($"pokédex: {save.SeenSpecies.Count} seen, {save.CaughtSpecies.Count} caught{(save.NationalPokedex ? ", national" : "")}");
        if (save.AutosaveSlot is { } autosaved) text.AppendLine($"an autosave of slot {autosaved}");

        text.AppendLine();
        text.AppendLine($"team ({save.Party.Count})");
        foreach (var p in save.Party) text.AppendLine("  " + Line(p));
        int boxed = save.Boxes?.Boxes.Sum(b => b.Pokemon.Count) ?? 0;
        text.AppendLine($"boxes: {save.Boxes?.Boxes.Count ?? 0} boxes, {boxed + save.BoxStorage.Count} Pokémon");
        if (everything && save.Boxes != null)
            for (int b = 0; b < save.Boxes.Boxes.Count; b++)
                foreach (var entry in save.Boxes.Boxes[b].Pokemon.OrderBy(e => e.Slot))
                    text.AppendLine($"  box {b + 1}.{entry.Slot + 1}  {Line(entry.Pokemon)}");
        if (save.DayCare != null)
            foreach (var place in save.DayCare.Places.Where(p => p?.Pokemon != null))
                text.AppendLine($"  Day Care  {Line(place!.Pokemon!)}");

        text.AppendLine();
        text.AppendLine($"bag ({save.Inventory.Count} items)");
        foreach (var item in save.Inventory) text.AppendLine($"  {item.ItemName} x{item.Quantity}");

        text.AppendLine();
        text.AppendLine($"story: {save.StoryFlags.Count} flags, {save.StoryVariables.Count} variables, {save.DefeatedTrainers.Count} trainers beaten, {save.TakenItems.Count} items taken");
        if (everything)
        {
            text.AppendLine("  flags: " + string.Join(", ", save.StoryFlags.Order(StringComparer.Ordinal)));
            text.AppendLine("  variables: " + string.Join(", ", save.StoryVariables.OrderBy(v => v.Key, StringComparer.Ordinal).Select(v => $"{v.Key}={v.Value}")));
        }
        return text.ToString();
    }

    private static string Line(SavedPokemonData p)
    {
        string name = string.IsNullOrEmpty(p.Nickname) || p.Nickname == p.SpeciesName ? p.SpeciesName : $"{p.Nickname} ({p.SpeciesName})";
        string form = p.Form != null ? $" [{p.Form}]" : "";
        string egg = p.IsEgg ? " (Egg)" : "";
        string moves = string.Join(", ", p.Moves.Select(m => $"{m.MoveName} {m.CurrentPP}"));
        string held = p.HeldItem != null ? $" holding {p.HeldItem}" : "";
        return $"{name}{form}{egg} L{p.Level} {p.Gender}{(p.IsShiny ? " shiny" : "")} {p.Nature}, HP {p.CurrentHP}, IV {p.IvHP}/{p.IvAttack}/{p.IvDefense}/{p.IvSpAttack}/{p.IvSpDefense}/{p.IvSpeed}, EV {p.EvHP}/{p.EvAttack}/{p.EvDefense}/{p.EvSpAttack}/{p.EvSpDefense}/{p.EvSpeed}{held}; {moves}";
    }

    private static string Time(float seconds)
    {
        int minutes = (int)(Math.Max(0f, seconds) / 60f);
        return $"{minutes / 60}:{minutes % 60:D2}";
    }
}
