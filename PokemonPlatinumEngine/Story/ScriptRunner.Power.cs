using System;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The command of plan 36 · TR8: <c>power</c>, the timed powers (<see cref="PowerState"/>). <c>power level</c> is the level a
/// teacher teaches at now, by the Halls of Fame entered (<c>RESULT</c> 0 to 3, <see cref="PowerTable.TeachingLevel"/>);
/// <c>power learn</c> teaches the kinds of the host's region (<c>Data/powers.json</c>'s <c>teachers</c>) at that level
/// (<c>RESULT</c> how many were learned or raised, <c>{thing}</c> their names and <c>{number}</c> the level; 0 when nothing
/// changed); <c>power grant exp 2 10</c> puts a kind in force at a level for so many minutes of play through the one door
/// (<see cref="PowerState.Grant"/>; <c>RESULT</c> 1, or 0 when the host keeps no powers). A host with no powers learns nothing.
/// </summary>
public sealed partial class ScriptRunner
{
    private void PowerCommand(Instruction i)
    {
        Result = 0;
        var powers = host.Powers;
        int halls = RegionDatabase.All.Count(r => host.Story.Has(r.StoryCompleteFlag));
        int level = PowerTable.TeachingLevel(halls);
        switch (i.Name)
        {
            case "level":
                Result = level;
                return;

            case "learn":
            {
                if (powers == null || level == 0) return;
                var learned = PowerTable.TaughtIn(host.Region).Where(kind => powers.Learn(kind, level)).ToList();
                lastThing = string.Join(Core.Text.Of("crafting.leftSeparator"), learned.Select(PowerTable.NameOf));
                lastNumber = level;
                Result = learned.Count;
                return;
            }

            case "grant":
                if (powers == null || !Enum.TryParse<PowerKind>(i.Other, true, out var kind)) return;
                Result = powers.Grant(kind, i.Number, i.Partner is { } minutes && int.TryParse(minutes, out int m) ? m * 60 : PowerTable.Seconds) ? 1 : 0;
                return;
        }
    }
}
