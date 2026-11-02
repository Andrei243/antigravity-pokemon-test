using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The command of plan 21, part l: <c>trainerhill</c>, a climb of Emerald's Trainer Hill on Route 111
/// (<see cref="TrainerHill"/>). A climb lives in the story's own variables, as a Battle Subway run does, so nothing of it
/// is kept by a host and nothing new is saved.
/// </summary>
public sealed partial class ScriptRunner
{
    private int PlaySeconds => (int)host.Broadcast().PlayTimeSeconds;

    /// <summary>
    /// At the counter: <c>begin normal|variety|unique|expert</c> starts a climb of that challenge with its clock
    /// running; <c>active</c> is the climb's state (0 none, 1 climbing, 2 lost, 3 at the roof); <c>mode</c> the
    /// challenge (0 to 3); <c>end</c> lets go of it. On a floor: <c>floor</c> is the floor (<c>{number}</c>, 5 the
    /// roof); <c>laid</c> is 1 when the floor's trainers stand on the map (0 on a floor a loaded game came back to,
    /// which is then laid again by a warp to it); <c>opponent</c> is 1 when whoever the script belongs to is a trainer
    /// of the floor not yet beaten (<c>{foe}</c>); <c>fight</c> is their battle on the player's own team, beside the
    /// floor's other trainer where that one is not yet beaten and sees the player too (RESULT 1 won, 0 lost);
    /// <c>lost</c> marks the climb lost; <c>climb</c> takes the stairs (RESULT 1 at the roof, where the clock stops);
    /// <c>down</c> goes a floor down (RESULT the floor come to, 0 the lobby). On the roof: <c>prize</c> hands over
    /// Emerald's prize for the time (RESULT 0 given, 2 given already this climb); <c>record</c> holds the time against the
    /// challenge's best (RESULT 0 a new record, 1 not, 2 held already). <c>{hill:TIME}</c> is the climb's time and
    /// <c>{hill:BEST}</c> the challenge's best.
    /// </summary>
    private void TrainerHillCommand(Instruction i)
    {
        var story = host.Story;
        int Floor() => Math.Clamp(story.Var(TrainerHill.Vars.Floor), 1, TrainerHill.Roof);
        switch (i.Name)
        {
            case "begin":
                TrainerHill.Begin(story, Enum.Parse<TrainerHillMode>(i.Other, ignoreCase: true), PlaySeconds);
                lastNumber = 1;
                break;
            case "active":
                Result = story.Var(TrainerHill.Vars.State);
                break;
            case "mode":
                Result = (int)TrainerHill.ModeOf(story);
                break;
            case "floor":
                Result = lastNumber = Floor();
                break;
            case "laid":
                Result = host.FindNpc(TrainerHillFloor.TrainerKey(0), TrainerHillFloor.MapName) != null ? 1 : 0;
                break;
            case "opponent":
            {
                int? t = TrainerHillFloor.TrainerOf(Subject);
                if (t is not { } trainer || Floor() > TrainerHill.Floors) { lastFoe = ""; Result = 0; break; }
                var record = TrainerHillDatabase.Floor((int)TrainerHill.ModeOf(story), Floor()).Trainers[trainer];
                lastFoe = $"{record.Class} {record.Name}";
                Result = TrainerHill.IsBeaten(story, Floor(), trainer) ? 0 : 1;
                break;
            }
            case "fight":
            {
                int t = TrainerHillFloor.TrainerOf(Subject) ?? throw Wrong(i, "'trainerhill fight' belongs to a trainer of the floor");
                var mode = TrainerHill.ModeOf(story);
                int floor = Floor();
                var record = TrainerHillDatabase.Floor((int)mode, floor);
                int level = TrainerHill.LevelFor(host.Party);
                var fighting = new List<int> { t };
                // FillHillTrainersParties: the floor's other trainer joins in when they too see the player and are not yet beaten
                int other = 1 - t;
                var (px, py) = host.TileOf(null);
                if (!TrainerHill.IsBeaten(story, floor, other) && TrainerHillFloor.Sees(record, record.Spots[other], px, py)) fighting.Add(other);
                var foes = fighting.Select(n => TrainerHill.TrainerOf(mode, floor, n, level)).ToList();
                var people = fighting.Select(n => host.FindNpc(TrainerHillFloor.TrainerKey(n), TrainerHillFloor.MapName)).ToList();
                host.FrontierBattle(host.Party, foes, null, doubles: foes.Count == 2);
                afterBusy = () =>
                {
                    Result = Outcome() == 1 ? 1 : 0;
                    if (Result != 1) return;
                    foreach (int n in fighting) TrainerHill.Beat(story, floor, n);
                    foreach (var person in people) if (person != null) person.HasBattled = true;
                };
                break;
            }
            case "lost":
                story.SetVar(TrainerHill.Vars.State, TrainerHill.States.Lost);
                break;
            case "climb":
                Result = TrainerHill.Climb(story, PlaySeconds) ? 1 : 0;
                lastNumber = Floor();
                break;
            case "down":
            {
                int floor = Math.Max(0, Floor() - 1);
                story.SetVar(TrainerHill.Vars.Floor, Math.Max(1, floor));
                Result = lastNumber = floor;
                break;
            }
            case "prize":
            {
                if (story.Var(TrainerHill.Vars.Prize) == 1) { Result = 2; break; }
                var item = ItemDatabase.Get(TrainerHill.PrizeFor(TrainerHill.ModeOf(story), story.Var(TrainerHill.Vars.Time)))
                    ?? throw Wrong(i, "the Trainer Hill's prize is no item");
                story.SetVar(TrainerHill.Vars.Prize, 1);
                host.Bag.AddItem(item, 1);
                lastItem = item.Name;
                host.Fanfare(FanfareFor(item));
                var lines = new List<string> { Text.Of("found.received", ("n", 1), ("item", item.DisplayName)) };
                if (!string.IsNullOrEmpty(item.TeachesMove)) lines.Add(Text.Of("found.holdsMove", ("item", item.DisplayName), ("move", LocalNames.Move(item.TeachesMove))));
                lines.Add(Text.Of("found.putAway", ("n", 1), ("pocket", PocketName(item.Pocket))));
                host.Say(null, lines);
                Result = 0;
                break;
            }
            case "record":
                Result = TrainerHill.CheckTime(story);
                break;
            case "end":
                story.SetVar(TrainerHill.Vars.State, TrainerHill.States.None);
                story.SetVar(TrainerHill.Vars.Floor, 0);
                break;
        }
    }

    /// <summary>The Trainer Hill's times as a line says them: the climb's, or the best of the challenge taken.</summary>
    private string HillPlaceholder(string argument)
    {
        var story = host.Story;
        if (argument == "TIME") return TrainerHill.TimeOf(TrainerHill.Frames(story, PlaySeconds));
        // BEST is the challenge taken; BEST_NORMAL and its kin a challenge by name, as the board downstairs reads them
        var mode = argument.StartsWith("BEST_") && Enum.TryParse<TrainerHillMode>(argument["BEST_".Length..], ignoreCase: true, out var named)
            ? named : TrainerHill.ModeOf(story);
        int best = story.Var(TrainerHill.Vars.Best(mode));
        return best == 0 ? Text.Of("trainerHill.none") : TrainerHill.TimeOf(best);
    }
}
