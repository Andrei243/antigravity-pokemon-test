using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models.StageShow;

/// <summary>
/// What a save keeps of the stage show (<see cref="Core.SaveData.StageShow"/>, plan 42 · PS1): the trunk, each Pokémon's last
/// dressing by its <see cref="Pokemon.Id"/>, the shows played and won, each show's best score, and the gifts the lobby's
/// fans wait with. Null while nothing is, so no save version was raised.
/// </summary>
public sealed class StageShowSave
{
    public bool TrunkGiven { get; set; }
    public Dictionary<string, int> Trunk { get; set; } = new();
    public Dictionary<string, List<string>> Dressings { get; set; } = new();
    public int Played { get; set; }
    public int Won { get; set; }
    public Dictionary<string, int> Best { get; set; } = new();
    /// <summary>A fan's key (<c>fan_1</c>, <c>fan_4</c>, <c>fan_18</c>) to the prop they wait to give.</summary>
    public Dictionary<string, string> Fans { get; set; } = new();
}

/// <summary>
/// The stage show's records (plan 42 · PS1, PS2; GPU-free): the trunk of props, the dressings by Pokémon, the shows
/// played and won and their bests, and the fans' gifts owed (<see cref="ShowFans"/>).
/// </summary>
public sealed class ShowRecords
{
    public PropTrunk Trunk { get; } = new();

    /// <summary>Whether the theatre's owner has given the trunk.</summary>
    public bool TrunkGiven { get; set; }

    private readonly Dictionary<Guid, Dressing> dressings = new();
    private readonly Dictionary<string, int> best = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> fans = new(StringComparer.Ordinal);

    public int Played { get; private set; }
    public int Won { get; private set; }

    public IReadOnlyDictionary<string, int> Best => best;

    /// <summary>The fans waiting with a gift, by their key, and the piece each holds.</summary>
    public IReadOnlyDictionary<string, string> Fans => fans;

    /// <summary>A Pokémon's last dressing (an empty one for a Pokémon never dressed, or one with no id).</summary>
    public Dressing DressingOf(Pokemon pokemon) =>
        pokemon.Id != Guid.Empty && dressings.TryGetValue(pokemon.Id, out var d) ? d.Copy() : new Dressing();

    /// <summary>Keeps a Pokémon's dressing; only pieces still in the trunk are kept.</summary>
    public void Keep(Pokemon pokemon, Dressing dressing)
    {
        if (pokemon.Id == Guid.Empty) return;
        var kept = Dressing.Of(dressing.Ids().Where(Trunk.Has));
        if (kept.Count == 0) dressings.Remove(pokemon.Id);
        else dressings[pokemon.Id] = kept;
    }

    /// <summary>A show played: its score, whether the player came first.</summary>
    public void Record(string show, int score, bool won)
    {
        Played++;
        if (won) Won++;
        if (!best.TryGetValue(show, out int b) || score > b) best[show] = score;
    }

    public int BestOf(string show) => best.TryGetValue(show, out int b) ? b : 0;

    public void Owe(string fan, string prop) => fans[fan] = prop;

    /// <summary>The piece a fan waits with, taken (null when they wait with none).</summary>
    public string? TakeFrom(string fan) => fans.Remove(fan, out var prop) ? prop : null;

    public string? WaitingWith(string fan) => fans.TryGetValue(fan, out var prop) ? prop : null;

    public void Clear()
    {
        Trunk.Clear();
        TrunkGiven = false;
        dressings.Clear();
        best.Clear();
        fans.Clear();
        Played = Won = 0;
    }

    public void Load(StageShowSave? save)
    {
        Clear();
        if (save == null) return;
        TrunkGiven = save.TrunkGiven;
        foreach (var (id, n) in save.Trunk) Trunk.Add(id, n);
        foreach (var (key, ids) in save.Dressings)
            if (Guid.TryParse(key, out var guid))
            {
                var d = Dressing.Of(ids);
                if (d.Count > 0) dressings[guid] = d;
            }
        Played = Math.Max(0, save.Played);
        Won = Math.Clamp(save.Won, 0, Played);
        foreach (var (show, score) in save.Best) best[show] = score;
        foreach (var (fan, prop) in save.Fans)
            if (ShowPropDatabase.Get(prop) != null) fans[fan] = prop;
    }

    /// <summary>What a save keeps: null while nothing is, so an older save and a new one read alike.</summary>
    public StageShowSave? ToSave()
    {
        if (!TrunkGiven && Trunk.Kinds == 0 && dressings.Count == 0 && Played == 0 && fans.Count == 0) return null;
        return new StageShowSave
        {
            TrunkGiven = TrunkGiven,
            Trunk = Trunk.Counts.Where(c => c.Value > 0).ToDictionary(c => c.Key, c => c.Value),
            Dressings = dressings.ToDictionary(d => d.Key.ToString(), d => d.Value.Ids()),
            Played = Played,
            Won = Won,
            Best = new Dictionary<string, int>(best),
            Fans = new Dictionary<string, string>(fans)
        };
    }

    /// <summary>The pieces the theatre's owner gives with the trunk: one of each mood's, a hat and a bow among them.</summary>
    public static readonly string[] FirstHandful = { "top_hat_black", "head_bow_pink", "scarf_yellow", "round_glasses", "short_cape_night", "folding_fan", "tail_bell" };

    /// <summary>The owner's gift: the trunk and <see cref="FirstHandful"/>, once. False if it was given already.</summary>
    public bool GiveTrunk()
    {
        if (TrunkGiven) return false;
        TrunkGiven = true;
        foreach (var id in FirstHandful) Trunk.Add(id);
        return true;
    }
}
