using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Facilities;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// <c>facility titles &lt;word&gt;</c>, the house of titles on Kalos's Route 7 (plan 37 · FC4, <see cref="TitlesHouse"/>):
/// its own words under the one command of the later places. The battles are the field's (<see cref="IScriptHost.Battle"/>,
/// <c>StartTrainerBattle</c>): the player's own team at its own levels, paid EXP and money.
/// </summary>
public sealed partial class ScriptRunner
{
    /// <summary>
    /// <c>member</c> battles the member spoken to with three of the later sets at the title's level over the player's
    /// highest (RESULT 1 won, 0 lost; a win adds its points to <c>VAR_KALOS_CHATEAU_POINTS</c>, <c>{number}</c>);
    /// <c>level</c> is that level (<c>{number}</c>); <c>visitor</c> draws the day's visitor of another finished region by the
    /// day's seed into <c>VAR_KALOS_CHATEAU_VISITOR</c> (RESULT 1, 0 for none); <c>dress</c> stands the day's visitor as the
    /// hall's <c>opponent</c> (<c>{foe}</c>; RESULT 1, 0 for none); <c>visit</c> battles them (RESULT and points as
    /// <c>member</c>'s).
    /// </summary>
    private void TitlesCommand(Instruction i)
    {
        int title = host.Story.Var(TitlesHouse.TitleVariable);
        int seed = host.Story.Var(TitlesHouse.SeedVariable);
        int highest = host.Party.Members.Where(p => !p.IsEgg).Select(p => p.Level).DefaultIfEmpty(1).Max();
        TreeCastRecord? Visitor() => host.Story.Var(TitlesHouse.VisitorVariable) is var n and > 0 && n <= PartnersTree.Cast.Count ? PartnersTree.Cast[n - 1] : null;
        switch (i.Other)
        {
            case "level":
                Result = lastNumber = TitlesHouse.LevelFor(title, highest);
                break;
            case "visitor":
            {
                int number = TitlesHouse.VisitorOfTheDay(title, host.Story.Has, seed);
                host.Story.SetVar(TitlesHouse.VisitorVariable, number);
                Result = number > 0 ? 1 : 0;
                break;
            }
            case "dress":
            {
                Result = 0;
                if (Visitor() is not { } visitor) break;
                var dressed = TitlesHouse.VisitorDress(visitor);
                towerFoes = new List<FrontierOpponent> { dressed };
                lastFoe = $"{dressed.ShownClass} {dressed.ShownName}";
                host.FrontierAct("opponents", 0, Subject);
                Result = 1;
                break;
            }
            case "member":
            case "visit":
            {
                if (NoPokemonToBattle()) break;
                if (Subject is not { } person) throw Wrong(i, $"'facility titles {i.Other}' is said by someone of the house");
                Trainer team;
                int points;
                if (i.Other == "member")
                {
                    if (!TitlesHouse.Members.TryGetValue(person.Key, out var member)) throw Wrong(i, $"{person.Key} is no member of the house of titles");
                    team = TitlesHouse.MemberTeam(person.Key, title, highest, seed);
                    points = TitlesHouse.PointsFor(member.East ? TitlesGuestKind.East : TitlesGuestKind.West);
                }
                else
                {
                    if (Visitor() is not { } visitor)
                    {
                        // Nobody visits today (no other region finished): nothing to battle
                        Result = 0;
                        break;
                    }
                    // Their own team, lifted to the journey's post-game level on a copy under an id of the house's own, so
                    // the trainer of their region is never beaten by a battle here (the plaza's rule, plan 41 · EG3)
                    var own = TitlesHouse.VisitorTeam(visitor);
                    team = LevelLift.To(own, System.Math.Max(LevelLift.PostGameLevel(host.Story), own.Party.Members.Max(p => p.Level)));
                    team.Id = TitlesHouse.VisitorTrainer + visitor.Trainer;
                    points = TitlesHouse.PointsFor(TitlesGuestKind.Visitor, visitor.Role);
                }
                var before = person.TrainerData;
                person.TrainerData = team;
                host.Battle(person, null, null, mayLose: true, first: false);
                afterBusy = () =>
                {
                    person.TrainerData = before;
                    if (host.Outcome == BattleOutcome.Won)
                    {
                        host.Story.SetVar(TitlesHouse.PointsVariable, host.Story.Var(TitlesHouse.PointsVariable) + points);
                        lastNumber = points;
                        Result = 1;
                    }
                    else Result = 0;
                };
                break;
            }
        }
    }
}
