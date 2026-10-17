using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The command of plan 01 · M11, part d: <c>tutor</c>, what the move tutors and the Move Deleter do in their rooms
/// (<see cref="MoveTutors"/>), each word on the original's script command named beside it.
/// </summary>
public sealed partial class ScriptRunner
{
    private Pokemon? pupil;
    private TutorMove? tutorPrice;
    private string tutorMove = "", lastForgotten = "";
    private int forgetSlot = -1;

    /// <summary>
    /// All for the Pokémon RESULT names, after <c>choosepokemon</c>; <c>{wearer}</c> names it and <c>{thing}</c> the
    /// move. <c>moves "route_212"</c> asks which of the place's moves it is to be taught, each with its price in
    /// Shards (RESULT 1 one was picked, 0 none, 2 the tutor has nothing for it, 3 an Egg, 4 nobody chosen);
    /// <c>afford</c> is 1 when the bag holds the Shards the move picked costs and <c>pay</c> takes them.
    /// <c>special dracometeor</c> and <c>special ultimate</c> are Grandma Wilma's and the old man of Route 228's
    /// answers (RESULT 0 it may learn, 1 nobody chosen, 2 an Egg, 3 it can't, 4 it knows the move, 5 it doesn't trust
    /// its Trainer fully), and <c>dragons</c> is 1 when anyone of the team is a Dragon type. <c>learn</c> teaches the
    /// move settled on: at once to a Pokémon with room (RESULT 1), else it asks which move is to go (RESULT 3 learned
    /// in its place, with <c>{forgotten}</c> the move that went, 0 given up, 2 an HM's move was picked, which stays,
    /// and nothing is learned). <c>forget</c> is the
    /// Move Deleter's question (RESULT 1 a move was picked, 0 none, 2 it knows only one, 3 an Egg, 4 nobody chosen)
    /// and <c>delete</c> takes the move picked away for good, an HM's too.
    /// </summary>
    private void Tutor(Instruction i)
    {
        var team = host.Party.Members;
        Pokemon? Chosen()
        {
            var chosen = Result >= 0 && Result < team.Count ? team[Result] : null;
            if (chosen != null) lastWearer = chosen.DisplayName;
            return chosen;
        }
        switch (i.Name)
        {
            // CheckHasLearnableTutorMoves and ShowMoveTutorMoveSelectionMenu
            case "moves":
            {
                pupil = Chosen();
                tutorPrice = null;
                tutorMove = "";
                if (pupil == null) { Result = 4; break; }
                if (pupil.IsEgg) { Result = 3; break; }
                var moves = MoveTutors.Teachable(pupil, i.Other);
                if (moves.Count == 0) { Result = 2; break; }
                host.Ask(speakerShown, Text.Of("scriptLines.whichMove", ("pokemon", pupil.DisplayName)),
                    moves.Select(m => Text.Of("scriptLines.moveRow", ("move", LocalNames.Move(m.Move)), ("price", m.Price))).Append(Text.Of("scriptLines.cancel")).ToList(), cancel: moves.Count);
                afterBusy = () =>
                {
                    if (host.Answer < 0 || host.Answer >= moves.Count) { Result = 0; return; }
                    tutorPrice = moves[host.Answer];
                    lastThing = tutorMove = tutorPrice.Move;
                    Result = 1;
                };
                break;
            }
            // CheckCanAffordMove and PayShardCost
            case "afford":
                Result = tutorPrice != null && MoveTutors.CanAfford(host.Bag, tutorPrice) ? 1 : 0;
                break;
            case "pay":
                Result = tutorPrice != null && MoveTutors.Pay(host.Bag, tutorPrice) ? 1 : 0;
                break;
            // The checks of the two scripts that teach one move each: the type or the species, the move, the friendship
            case "special":
            {
                pupil = Chosen();
                tutorPrice = null;
                bool ultimate = i.Other == "ultimate";
                tutorMove = pupil == null || pupil.IsEgg ? "" : ultimate ? MoveTutors.UltimateMoveOf(pupil) ?? "" : MoveTutors.DracoMeteor;
                lastThing = tutorMove;
                Result = (int)(ultimate ? MoveTutors.ForUltimateMove(pupil) : MoveTutors.ForDracoMeteor(pupil));
                break;
            }
            // Route210GrandmaWilmaHouse_CheckCanAnyMonLearn
            case "dragons":
                Result = team.Any(p => !p.IsEgg && p.HasType(PokemonType.Dragon)) ? 1 : 0;
                break;
            // GetPartyMonMoveCount, the summary's choice of the move to go, and ResetMoveSlot
            case "learn":
            {
                if (pupil == null || tutorMove.Length == 0) { Result = 0; break; }
                lastForgotten = "";
                if (pupil.Moves.Count < 4)
                {
                    MoveTeaching.Learn(pupil, tutorMove);
                    Result = 1;
                    break;
                }
                var learner = pupil;
                string move = tutorMove;
                host.Ask(speakerShown, Text.Of("scriptLines.knowsFour", ("pokemon", learner.DisplayName), ("move", LocalNames.Move(move))),
                    learner.Moves.Select(m => m.DisplayName).Append(Text.Of("scriptLines.giveUp")).ToList(), cancel: learner.Moves.Count);
                afterBusy = () =>
                {
                    int slot = host.Answer;
                    if (slot < 0 || slot >= learner.Moves.Count) { Result = 0; return; }
                    lastForgotten = learner.Moves[slot].DisplayName;
                    if (MoveTeaching.WhyNotForget(learner, slot) != null) { Result = 2; return; }
                    MoveTeaching.Learn(learner, move, slot);
                    Result = 3;
                };
                break;
            }
            // The Move Deleter's SelectPartyMonMove
            case "forget":
            {
                pupil = Chosen();
                forgetSlot = -1;
                if (pupil == null) { Result = 4; break; }
                if (pupil.IsEgg) { Result = 3; break; }
                if (!MoveTutors.CanForget(pupil)) { Result = 2; break; }
                var learner = pupil;
                host.Ask(speakerShown, Text.Of("scriptLines.whichForgotten"), learner.Moves.Select(m => m.DisplayName).Append(Text.Of("scriptLines.cancel")).ToList(), cancel: learner.Moves.Count);
                afterBusy = () =>
                {
                    if (host.Answer < 0 || host.Answer >= learner.Moves.Count) { Result = 0; return; }
                    forgetSlot = host.Answer;
                    lastThing = learner.Moves[forgetSlot].DisplayName;
                    Result = 1;
                };
                break;
            }
            // ForgetMove
            case "delete":
                if (pupil == null || forgetSlot < 0 || forgetSlot >= pupil.Moves.Count || !MoveTutors.CanForget(pupil)) { Result = 0; break; }
                lastThing = MoveTutors.Forget(pupil, forgetSlot);
                forgetSlot = -1;
                Result = 1;
                break;
            default:
                throw Wrong(i, $"'tutor {i.Name}' is nothing a tutor does");
        }
    }
}
