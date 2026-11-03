using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The command of plan 13 · V23: <c>friend</c>, said of the person the script was started with, one of the friends'
/// trainers the friends' hall stood (<see cref="FriendsHall"/>). <c>beaten</c> is 1 when they were beaten today (and for
/// someone who is no friend's trainer, who has nothing to battle); <c>before</c> and <c>after</c> say the lines their file
/// chose, under their name; <c>battle</c> fights them at level 50 on copies of both teams, the Frontier's battle rules
/// (nothing won, paid, learnt or used up), RESULT 1 won and 0 lost, and a win beats them for the day.
/// </summary>
public sealed partial class ScriptRunner
{
    private void FriendCommand(Instruction i)
    {
        var file = host.FriendOf(Subject);
        var hall = host.FriendsHall;
        switch (i.Name)
        {
            case "beaten":
                Result = file == null || hall == null || hall.BeatenOn(file.Checksum, GameClock.Today) ? 1 : 0;
                break;
            case "before" or "after":
                if (file == null) break;
                int line = i.Name == "before" ? file.Before : file.After;
                host.Say(file.Name, new List<string> { Text.Of($"friends.line.{line}") });
                break;
            case "battle":
            {
                if (file == null || hall == null)
                {
                    Result = 0;
                    break;
                }
                // Both teams at level 50, copies: the player's whole team but its Eggs, the friend's made from their file
                var team = LevelScaling.TeamAt(host.Party.Members.Where(p => !p.IsEgg), TrainerFile.FlatLevel);
                var foe = file.ToTrainer();
                host.FrontierBattle(team, new[] { foe }, null, doubles: false);
                afterBusy = () =>
                {
                    Result = host.Outcome == BattleOutcome.Won ? 1 : 0;
                    if (Result == 1)
                    {
                        hall.Beat(file.Checksum, GameClock.Today);
                        host.FriendWon();
                    }
                };
                break;
            }
        }
    }
}
