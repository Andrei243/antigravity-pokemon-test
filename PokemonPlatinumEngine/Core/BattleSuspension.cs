using System.Text.Json;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// A battle left by the close button's SUSPEND AND QUIT (plan 12 · Q25): the game as it stood when the battle began
/// (<see cref="Save"/>, as <c>BuildSave</c> makes it), the battle as it began and every answer given in it since
/// (<see cref="Battle"/>, plan 08 · P10's file, closed on its record so far), and what the field needs to stand the
/// battle up again that the file doesn't hold (where it is fought, who of the map is fought). Kept beside its slot as
/// <c>suspend&lt;n&gt;.json</c> (<see cref="SavePaths.Suspend"/>) and deleted as it is taken up, so it is played on once and
/// rerolls nothing. No drawing, no input; every method takes the folder it works in.
/// </summary>
public sealed class BattleSuspension
{
    /// <summary>The shape of the file this game writes; a later one is refused.</summary>
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;

    /// <summary>The game as it stood as the battle began.</summary>
    public SaveData Save { get; set; } = new();

    /// <summary>The battle's file (<see cref="BattleRecordFile.Serialize"/>), its seed and its answers so far.</summary>
    public string Battle { get; set; } = "";

    /// <summary>The name of the place it is fought in (where a Pokémon caught in it was met).</summary>
    public string? Place { get; set; }

    /// <summary>The people of the map whose trainers are fought, by their keys: they are beaten or waiting once it ends.</summary>
    public List<string> TrainerPeople { get; set; } = new();

    /// <summary>A Pokémon hooked on a rod: the battle is fought on the water's stage.</summary>
    public bool Hooked { get; set; }

    /// <summary>The run's terms the battle was handed (plan 13 · V5), which the battle file keeps only in part.</summary>
    public RunTerms? Restrictions { get; set; }

    /// <summary>Whom it is against, as the title's card names it (<see cref="BattleRecordFile.Foe"/>).</summary>
    public string Foe { get; set; } = "";

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public string Serialize() => JsonSerializer.Serialize(this, Options);

    /// <summary>
    /// A suspension's text read back: the suspension and its battle, or why it can't be taken up (a file cut short, a
    /// later shape, or a battle of other data, <see cref="ReplayRefusal.OtherDataVersion"/>).
    /// </summary>
    public static (BattleSuspension? Suspension, BattleRecordFile? Battle, ReplayRefusal Refused) Read(string text, string? dataVersion = null)
    {
        BattleSuspension? read;
        try
        {
            read = JsonSerializer.Deserialize<BattleSuspension>(text, Options);
        }
        catch (JsonException)
        {
            return (null, null, ReplayRefusal.Damaged);
        }
        if (read == null || string.IsNullOrEmpty(read.Battle)) return (null, null, ReplayRefusal.Damaged);
        if (read.Version is > CurrentVersion or < 1) return (null, null, ReplayRefusal.OtherFileVersion);
        var battle = BattleRecordFile.Read(read.Battle, dataVersion);
        return battle.File == null ? (null, null, battle.Refused) : (read, battle.File, ReplayRefusal.None);
    }

    /// <summary>Whether a slot has a suspended battle beside it.</summary>
    public static bool Exists(SavePaths paths, int slot) => File.Exists(paths.Suspend(slot));

    /// <summary>A slot's suspension as <see cref="Read(string, string?)"/> reads it; nothing where there is none.</summary>
    public static (BattleSuspension? Suspension, BattleRecordFile? Battle, ReplayRefusal Refused) ReadFor(SavePaths paths, int slot)
    {
        try
        {
            return Read(File.ReadAllText(paths.Suspend(slot)));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return (null, null, ReplayRefusal.Damaged);
        }
    }

    /// <summary>Writes the suspension beside its slot, through a temporary file; false where it couldn't be written.</summary>
    public bool WriteFor(SavePaths paths, int slot)
    {
        string path = paths.Suspend(slot), temporary = SaveManager.TempOf(path);
        try
        {
            Directory.CreateDirectory(paths.Folder);
            File.WriteAllText(temporary, Serialize());
            File.Move(temporary, path, overwrite: true);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return false;
        }
    }

    /// <summary>Takes a slot's suspension away, and anything half written; true when nothing of it is left.</summary>
    public static bool Delete(SavePaths paths, int slot)
    {
        try
        {
            File.Delete(SaveManager.TempOf(paths.Suspend(slot)));
            File.Delete(paths.Suspend(slot));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // What couldn't be deleted is still there, and the answer says so
        }
        return !Exists(paths, slot);
    }
}

/// <summary>
/// Whether a battle lost may be fought again (plan 12 · Q25; rulings, "A lost battle fought again"): with the options'
/// RETRY BATTLES row on, after a battle the field or a scene began that the player lost and that nothing let be lost.
/// Never after a battle a scene says may be lost (its story goes on from the loss), the catching lesson, the Great Marsh,
/// Pal Park, the Bug-Catching Contest, the Battle Frontier or a battle watched, nor under a Nuzlocke or a run's
/// restrictions (a difficulty alone allows it), nor where no snapshot was kept as it began. No drawing, no input.
/// </summary>
public static class BattleRetry
{
    public static bool Offered(bool row, BattleResult result, BattleKind kind, bool snapshot, bool mayBeLost, bool frontier,
        bool watching, bool bugContest, bool nuzlocke, RunTerms? restrictions) =>
        row && result == BattleResult.PlayerDefeat && snapshot && !mayBeLost && !frontier && !watching && !bugContest && !nuzlocke
        && kind is BattleKind.Normal or BattleKind.Roamer && !Restricts(restrictions);

    /// <summary>Whether a run's terms are one of its challenge rules: any but the later mechanics barred, which is no challenge of plan 13's.</summary>
    public static bool Restricts(RunTerms? terms) => terms is { } t && (t.NoCatching || t.NoRunning || t.Monotype != null || t.NoItems);

    /// <summary>
    /// Whether the close button may offer SUSPEND AND QUIT for the battle on screen: a battle the field began that no
    /// script goes on from, not yet over, with its opening and its record kept (none for a battle a tool changed from
    /// outside), not under a Nuzlocke (whose catch the file doesn't keep), and with a saved game in its slot to go back to.
    /// </summary>
    public static bool Suspendable(bool snapshot, bool scriptRunning, BattleResult result, BattleKind kind, bool frontier,
        bool watching, bool bugContest, bool nuzlocke, bool recorded, bool slotSaved) =>
        snapshot && !scriptRunning && result == BattleResult.None && kind is BattleKind.Normal or BattleKind.Roamer && !frontier
        && !watching && !bugContest && !nuzlocke && recorded && slotSaved;
}
