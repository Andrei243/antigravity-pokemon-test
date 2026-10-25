using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The command of plan 22 · U6, part d: <c>subway</c>, a Battle Subway run from the desk to the last car and back
/// (<see cref="BattleSubway"/>: the smallest honest version of plan 06 · R18's machinery). A run lives in the story's
/// own variables, so nothing about it is kept by a host and nothing new is saved. What the Subway shares with the
/// Frontier's facilities (Battle Points, saving) is <c>frontier</c>'s (<see cref="Frontier"/>).
/// </summary>
public sealed partial class ScriptRunner
{
    // The Subway Bosses of the battle made ready, where there are some (the trainers of the Frontier's table stand in towerFoes)
    private List<Trainer> subwayBosses = new();

    private SubwayTrain TrainOfTheRun => (SubwayTrain)Math.Clamp(host.Story.Var(BattleSubway.Vars.Train), 0, 2);

    /// <summary>
    /// <c>enough</c> is 1 when the team holds that many Pokémon (three, four in the Double Train, two in the Multi
    /// Train) that could go in together, of the train a run was begun on or <c>{number}</c> three before one is
    /// (<c>{banned}</c> lists the species of the banlist the Pokédex has seen); <c>begin single|double|multi</c> starts
    /// a run (none of the Pokémon chosen yet); <c>choose</c> has them chosen (RESULT 1, or 0 when backed out of) and
    /// <c>check</c> is 0, or 1 for two of a species and 2 for two holding the same item; <c>active</c> is 1 while a run
    /// is under way, <c>mode</c> is its train (0 single, 1 double, 2 multi) and <c>end</c> lets go of it. In the cars:<c>next</c> is the number of the battle to come
    /// (<c>{number}</c>); <c>boss</c> is 0, or 1 when Ingo waits at it and 2 when Emmet does; <c>opponent</c> makes the
    /// trainers of the car ready and stands them in it (<c>{foe}</c>; the bosses' own teams at a boss's car);
    /// <c>battle</c> is fought with the Pokémon entered (RESULT 1 won, 0 lost); <c>won</c> counts the win (RESULT 1
    /// once a set of seven is through); <c>streak</c> is the streak as it stands and <c>best</c> the longest of the
    /// train (<c>{number}</c>). Getting off: <c>finish won|lost</c> writes the best and ends the run; <c>points</c>
    /// hands over the Battle Points earned (RESULT and <c>{number}</c>).
    /// </summary>
    private void Subway(Instruction i)
    {
        var story = host.Story;
        int Draw() => host.Chance.Next(0x10000);
        switch (i.Name)
        {
            case "enough":
            {
                int needed = story.Var(BattleSubway.Vars.State) == 1 ? BattleSubway.PartySize(TrainOfTheRun) : 3;
                lastNumber = needed;
                lastBanned = Listed(FrontierRules.BannedSeen(species => Data.PokemonDatabase.Get(species) is { } s && host.FrontierAct("seen", s.DexNumber, Subject) == 1));
                Result = FrontierRules.EnoughToEnter(host.Party, needed) ? 1 : 0;
                break;
            }
            case "begin":
            {
                var train = Enum.Parse<SubwayTrain>(i.Other, ignoreCase: true);
                story.SetVar(BattleSubway.Vars.State, 1);
                story.SetVar(BattleSubway.Vars.Train, (int)train);
                story.SetVar(BattleSubway.Vars.Wins, 0);
                story.SetVar(BattleSubway.Vars.Seed, 1 + Draw());
                BattleSubway.KeepSlots(story, Array.Empty<int>());
                lastNumber = BattleSubway.PartySize(train);
                break;
            }
            case "choose":
            {
                int size = BattleSubway.PartySize(TrainOfTheRun);
                Result = host.FrontierAct("enter", size, Subject);
                afterBusy = () =>
                {
                    Result = host.Answer == 1 && host.Entered.Count == size ? 1 : 0;
                    if (Result == 1) BattleSubway.KeepSlots(story, host.Entered.ToList());
                };
                break;
            }
            case "check":
            {
                var chosen = BattleSubway.SlotsOf(story, TrainOfTheRun).Where(s => s < host.Party.Count).Select(s => host.Party.Members[s]).ToList();
                Result = (int)FrontierRules.WhyNot(chosen);
                break;
            }
            case "active":
                Result = story.Var(BattleSubway.Vars.State);
                break;
            case "mode":
                Result = (int)TrainOfTheRun;
                break;
            case "end":
                story.SetVar(BattleSubway.Vars.State, 0);
                story.SetVar(BattleSubway.Vars.Wins, 0);
                BattleSubway.KeepSlots(story, Array.Empty<int>());
                towerFoes = new List<FrontierOpponent>();
                subwayBosses = new List<Trainer>();
                break;

            case "next":
                Result = lastNumber = story.Var(BattleSubway.Vars.Wins) + 1;
                break;
            case "boss":
                Result = BattleSubway.BossAt(story.Var(BattleSubway.Vars.Wins) + 1);
                break;
            case "opponent":
            {
                var train = TrainOfTheRun;
                int battle = story.Var(BattleSubway.Vars.Wins) + 1;
                subwayBosses = BattleSubway.Bosses(train, battle);
                if (subwayBosses.Count > 0)
                {
                    towerFoes = new List<FrontierOpponent>();
                    lastFoe = string.Join(" and ", subwayBosses.Select(b => $"{b.TrainerClass} {b.Name}"));
                    Result = subwayBosses.Count;
                    break;
                }
                towerFoes = BattleSubway.MakeOpponents(train, battle, Draw);
                lastFoe = string.Join(" and ", towerFoes.Select(f => $"{f.Record.Class} {f.Record.Name}"));
                host.FrontierAct("opponents", 0, Subject);
                Result = towerFoes.Count;
                break;
            }
            case "foe":
            {
                // A trainer of the data stands in the car as the one to battle (the World Tournament's bracket): their own
                // team at level 50, fought as the Bosses are, with the Pokémon entered and nothing won but the match
                var record = Data.TrainerDatabase.Get(i.Other) ?? throw Wrong(i, $"there is no trainer '{i.Other}'");
                var trainer = new Trainer
                {
                    Id = record.Id,
                    Name = Data.TrainerDatabase.IsRival(record.Class) ? Core.PlayerIdentity.Fill("{rival}") : record.Name,
                    TrainerClass = Data.TrainerDatabase.IsRival(record.Class) ? Data.TrainerDatabase.RivalClass : record.Class,
                    PrizeMoney = 0,
                    Ai = Battle.Sim.Ai.AiFlagNames.Parse(record.Ai),
                    Items = new List<string>()
                };
                foreach (var m in record.Party) trainer.Party.Add(Data.TrainerDatabase.Build(m));
                subwayBosses = new List<Trainer> { trainer.Relevelled(_ => BattleSubway.Level) };
                towerFoes = new List<FrontierOpponent>();
                lastFoe = $"{trainer.TrainerClass} {trainer.Name}";
                Result = 1;
                break;
            }
            case "battle":
            {
                var train = TrainOfTheRun;
                int battle = story.Var(BattleSubway.Vars.Wins) + 1;
                var team = FrontierRules.TeamFor(host.Party, BattleSubway.SlotsOf(story, train));
                if (subwayBosses.Count == 0 && towerFoes.Count == 0)
                {
                    subwayBosses = BattleSubway.Bosses(train, battle);
                    if (subwayBosses.Count == 0) towerFoes = BattleSubway.MakeOpponents(train, battle, Draw);
                }
                var foes = subwayBosses.Count > 0 ? subwayBosses : towerFoes.Select(f => f.AsTrainer()).ToList();
                Trainer? partner = train == SubwayTrain.Multi
                    ? BattleSubway.MakePartner(story.Var(BattleSubway.Vars.Seed)).AsTrainer()
                    : null;
                subwayBosses = new List<Trainer>();
                towerFoes = new List<FrontierOpponent>();
                host.FrontierBattle(team, foes, partner, doubles: BattleSubway.Doubles(train));
                afterBusy = () => Result = host.Outcome == BattleOutcome.Won ? 1 : 0;
                break;
            }
            case "won":
            {
                int wins = story.Var(BattleSubway.Vars.Wins) + 1;
                story.SetVar(BattleSubway.Vars.Wins, wins);
                Result = wins % BattleSubway.Cars == 0 ? 1 : 0;
                break;
            }
            case "streak":
                Result = lastNumber = story.Var(BattleSubway.Vars.Wins);
                break;
            case "best":
                Result = lastNumber = story.Var(BattleSubway.Vars.Best(TrainOfTheRun));
                break;
            case "finish":
            {
                var train = TrainOfTheRun;
                int wins = story.Var(BattleSubway.Vars.Wins);
                lastNumber = wins;
                // A streak that is the longest of its train is kept, whether the run ended in a loss or by getting off; the
                // run itself is let go of by 'end', after the points are handed over
                if (wins > story.Var(BattleSubway.Vars.Best(train))) story.SetVar(BattleSubway.Vars.Best(train), wins);
                break;
            }
            case "points":
            {
                int points = BattleSubway.PointsFor(story.Var(BattleSubway.Vars.Wins));
                if (points > 0)
                {
                    host.Frontier.GivePoints(points);
                    host.FrontierAct("points", 1, Subject);
                }
                Result = lastNumber = points;
                break;
            }
        }
    }
}
