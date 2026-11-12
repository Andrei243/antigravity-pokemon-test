using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The command of plan 44 · SD2: <c>heart</c>, Lockwork's locked hearts. <c>has</c> asks whether the Reclaimer is in the
/// bag (<c>RESULT</c> 1 or 0); <c>give</c> puts it in the bag once and sets <c>FLAG_GOT_RECLAIMER</c> (<c>RESULT</c> 1, or 0
/// when it was held already); <c>spring</c> is the spring's rite (<c>RESULT</c> 1 when the team's locked hearts were
/// lowered, 2 when it was done today already, 0 with no locked heart in the team); <c>open</c> opens every heart of the team
/// whose gauge has emptied (<c>RESULT</c> how many); <c>locked</c> counts the team's locked hearts; <c>count</c>,
/// <c>missed</c> and <c>opened</c> count the locks reclaimed, missed and opened since the game began. A host that keeps no
/// record answers 0.
/// </summary>
public sealed partial class ScriptRunner
{
    /// <summary>The flag the Reclaimer's giving sets.</summary>
    public const string ReclaimerFlag = "FLAG_GOT_RECLAIMER";

    private void HeartCommand(Instruction i)
    {
        var device = ItemDatabase.Get(HeartLock.ReclaimerItem);
        bool held = device != null && host.Bag.GetQuantity(device) > 0;
        var record = host.Hearts;
        switch (i.Name)
        {
            case "has":
                Result = held ? 1 : 0;
                return;
            case "give":
                Result = 0;
                if (device == null || held) return;
                host.Bag.AddItem(device, 1);
                host.Story.Set(ReclaimerFlag);
                lastItem = device.Name;
                Result = 1;
                return;
            case "spring":
            {
                var locked = host.Party.Members.Where(p => p.Lock != null).ToList();
                if (locked.Count == 0 || record == null)
                {
                    Result = 0;
                    return;
                }
                int today = ApricornBox.DayOf(host.Today);
                if (record.SpringDay == today)
                {
                    Result = 2;
                    return;
                }
                record.SpringDay = today;
                foreach (var p in locked) HeartLock.Lower(p, HeartLock.Fall.Spring);
                Result = 1;
                return;
            }
            case "open":
            {
                int opened = 0;
                foreach (var p in host.Party.Members.Where(p => p.Lock is { IsOpen: true }).ToList())
                {
                    host.OpenHeart(p);
                    opened++;
                }
                Result = opened;
                return;
            }
            case "locked":
                Result = host.Party.Members.Count(p => p.Lock != null);
                return;
            case "count":
                Result = record?.Reclaimed.Count ?? 0;
                return;
            case "missed":
                Result = record?.Missed.Count ?? 0;
                return;
            case "opened":
                Result = record?.Opened.Count ?? 0;
                return;
        }
    }
}
