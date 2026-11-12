using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// <c>facility trainertower &lt;word&gt;</c>, a climb of FireRed's Trainer Tower on Seven Island (plan 37 · FC6,
/// <see cref="TrainerTower"/>). A climb lives in the story's own variables, as the Trainer Hill's does.
/// </summary>
public sealed partial class ScriptRunner
{
    /// <summary>The Trainer Tower's word under <c>facility</c>.</summary>
    public const string TrainerTowerPlace = "trainertower";

    /// <summary>
    /// At the counter: <c>challenge single|double|knockout|mixed</c> starts a climb with its clock running; <c>climbing</c> is
    /// the climb's state (0 none, 1 climbing, 2 lost, 3 at the roof). On a floor: <c>floor</c> is the floor (<c>{number}</c>,
    /// 9 the roof); <c>kind</c> its kind (0 single, 1 double, 2 knockout); <c>opponent</c> stands the battle's trainers as
    /// <c>opponent</c> and <c>opponent_2</c> (<c>{foe}</c>; RESULT how many); <c>fight</c> is their battle on the player's own
    /// team (RESULT 1 won, 0 lost), and a win counts toward the floor (<c>cleared</c> is then 1 once all of it is beaten);
    /// <c>lost</c> marks the climb lost; <c>climb</c> takes the stairs (RESULT 1 at the roof, where the clock stops). On the
    /// roof: <c>prize</c> hands over the challenge's prize (RESULT 0 given, 2 given already); <c>record</c> holds the time
    /// against the challenge's best (RESULT 0 a new record, 1 not, 2 held already); <c>end</c> lets the climb go.
    /// <c>{tower:TIME}</c> is the climb's time and <c>{tower:BEST}</c> the challenge's best (<c>{tower:BEST_SINGLE}</c> one by name).
    /// </summary>
    private void TrainerTowerCommand(Instruction i)
    {
        var story = host.Story;
        switch (i.Other)
        {
            case "challenge":
                TrainerTower.Begin(story, Enum.Parse<TrainerTowerChallenge>(i.Move, ignoreCase: true), PlaySeconds);
                Result = 1;
                break;
            case "climbing":
                Result = story.Var(TrainerTower.Vars.State);
                break;
            case "floor":
                Result = lastNumber = TrainerTower.FloorOf(story);
                break;
            case "kind":
                Result = TrainerTower.FloorOf(story) > TrainerTower.Floors ? 3 : TrainerTower.KindOf(TrainerTower.ChallengeOf(story), TrainerTower.FloorOf(story));
                break;
            case "opponent":
            {
                int floor = TrainerTower.FloorOf(story);
                if (floor > TrainerTower.Floors || story.Var(TrainerTower.Vars.State) != TrainerTower.States.Climbing) { Result = 0; break; }
                var foes = TrainerTower.OpponentsOf(TrainerTower.ChallengeOf(story), floor, story.Var(TrainerTower.Vars.Beaten), TrainerHill.LevelFor(host.Party));
                towerFoes = foes.Select(TrainerTower.DressOf).ToList();
                lastFoe = string.Join(" and ", towerFoes.Select(o => $"{o.ShownClass} {o.ShownName}"));
                host.FrontierAct("opponents", 0, Subject);
                Result = foes.Count;
                break;
            }
            case "fight":
            {
                int floor = TrainerTower.FloorOf(story);
                var foes = TrainerTower.OpponentsOf(TrainerTower.ChallengeOf(story), floor, story.Var(TrainerTower.Vars.Beaten), TrainerHill.LevelFor(host.Party));
                towerFoes = new List<FrontierOpponent>();
                // A double floor's two trainers bring one Pokémon each and battle as a pair
                host.FrontierBattle(host.Party, foes, null, doubles: foes.Count == 2);
                afterBusy = () =>
                {
                    Result = Outcome() == 1 ? 1 : 0;
                    if (Result == 1) TrainerTower.Won(story);
                };
                break;
            }
            case "cleared":
            {
                var challenge = TrainerTower.ChallengeOf(story);
                int floor = TrainerTower.FloorOf(story);
                Result = floor > TrainerTower.Floors || story.Var(TrainerTower.Vars.Beaten) >= TrainerTower.TrainersOn(challenge, floor) ? 1 : 0;
                break;
            }
            case "lost":
                story.SetVar(TrainerTower.Vars.State, TrainerTower.States.Lost);
                break;
            case "climb":
                Result = TrainerTower.Climb(story, PlaySeconds) ? 1 : 0;
                lastNumber = TrainerTower.FloorOf(story);
                break;
            case "prize":
            {
                var challenge = TrainerTower.ChallengeOf(story);
                if (story.Var(TrainerTower.Vars.Prize(challenge)) == 1) { Result = 2; break; }
                var item = ItemDatabase.Get(TrainerTower.PrizeOf(challenge)) ?? throw Wrong(i, "the Trainer Tower's prize is no item");
                story.SetVar(TrainerTower.Vars.Prize(challenge), 1);
                host.Bag.AddItem(item, 1);
                lastItem = item.Name;
                host.Fanfare(FanfareFor(item));
                host.Say(null, new List<string>
                {
                    Text.Of("found.received", ("n", 1), ("item", item.DisplayName)),
                    Text.Of("found.putAway", ("n", 1), ("pocket", PocketName(item.Pocket)))
                });
                Result = 0;
                break;
            }
            case "record":
                Result = TrainerTower.CheckTime(story);
                break;
            case "end":
                story.SetVar(TrainerTower.Vars.State, TrainerTower.States.None);
                story.SetVar(TrainerTower.Vars.Floor, 0);
                story.SetVar(TrainerTower.Vars.Beaten, 0);
                towerFoes = new List<FrontierOpponent>();
                break;
        }
    }

    /// <summary>The Trainer Tower's times as a line says them: the climb's, or the best of the challenge taken (or named).</summary>
    private string TowerPlaceholder(string argument)
    {
        var story = host.Story;
        if (argument == "TIME") return TrainerHill.TimeOf(TrainerTower.Frames(story, PlaySeconds));
        var challenge = argument.StartsWith("BEST_", StringComparison.Ordinal) && Enum.TryParse<TrainerTowerChallenge>(argument["BEST_".Length..], ignoreCase: true, out var named)
            ? named : TrainerTower.ChallengeOf(story);
        int best = story.Var(TrainerTower.Vars.Best(challenge));
        return best == 0 ? Text.Of("trainerHill.none") : TrainerHill.TimeOf(best);
    }
}
