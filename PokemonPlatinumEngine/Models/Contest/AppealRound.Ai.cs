using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Contest;

/// <summary>
/// How Emerald's contestants think (plan 21 · H11; <c>data/contest_ai_scripts.s</c>, <c>src/contest_ai.c</c>): each of
/// the four moves starts at 100, the routines every contestant has (a bad move, a combo, a boring repeat, the crowd's
/// excitement, the order, a good move and the erratic one) add and take away, and the highest score wins, a tie by a
/// draw. The routines are written out as Emerald's scripts read; the one place it differs is
/// <c>if_user_doesnt_have_move</c>, which the original compares with a move's number where the script means its
/// effect (its own comment calls it a bug): here it asks the effect, as the script means.
/// </summary>
public sealed partial class AppealRound
{
    private bool Roll(int below) => rng.Next(256) < below;

    private bool LastAppeal => AppealNumber == Appeals - 1;

    /// <summary>The index of the move contestant <paramref name="user"/> chooses (<c>ContestAI_GetActionToUse</c>).</summary>
    private int ChooseFor(int user)
    {
        var a = Appealers[user];
        var scores = new int[4];
        for (int i = 0; i < 4; i++) scores[i] = i < a.Moves.Count ? 100 : 0;
        for (int i = 0; i < a.Moves.Count; i++)
        {
            int score = 100;
            var move = a.Moves[i];
            score += CheckBadMove(user, move);
            score += CheckCombo(user, move);
            score += CheckBoring(user, move);
            score += CheckExcitement(user, move);
            score += CheckOrder(user, move);
            score += CheckGoodMove(user, move);
            score += Erratic();
            scores[i] = Math.Clamp(score, 0, 255);
        }
        // A move picked at random among the best (the original draws a move and takes it if none is better)
        while (true)
        {
            int index = rng.Next(4);
            int best = scores[index];
            if (index >= a.Moves.Count) continue;
            if (scores.All(s => s <= best)) return index;
        }
    }

    // ---- what the AI's routines ask

    private int UserOrder(int user) => turnOrder[user];
    private bool CanParticipateAt(int turn) => !status[ContestantAtTurn(turn)].TurnDisabled;
    private int UsedCount(int user, AppealMove move) =>
        status[user].PrevMove != null && status[user].PrevMove!.Name == move.Name ? status[user].MoveRepeatCount + 1 : 0;
    private static int EffectOf(AppealMove move) => move.Contest.Effect;
    private static ContestEffectType TypeOf(AppealMove move) => HoennContestDatabase.Effect(move.Contest.Effect).Type;
    private int Excitement(AppealMove move) => AppealRules.Excitement(Category, move.Contest.Category);

    private bool WouldFinishCombo(int user, AppealMove move) =>
        status[user].PrevMove != null && HoennContestDatabase.AreCombo(status[user].PrevMove!.Contest, move.Contest);

    private bool IsComboStarter(int user, AppealMove move) =>
        Appealers[user].Moves.Any(other => HoennContestDatabase.AreCombo(move.Contest, other.Contest));

    private bool IsComboFinisher(int user, AppealMove move) =>
        Appealers[user].Moves.Any(other => HoennContestDatabase.AreCombo(other.Contest, move.Contest));

    private bool UserHasExcitingMove(int user) => Appealers[user].Moves.Any(m => Excitement(m) == 1);

    private bool UserHasEffect(int user, int effect) => Appealers[user].Moves.Any(m => m.Contest.Effect == effect);

    private int UserStars(int user) => status[user].Condition / 10;

    // Emerald's effect numbers the routines name
    private const int EGreatAppealButNoMoreMoves = 2, ERepetitionNotBoring = 3, EAvoidStartleOnce = 4, EAvoidStartle = 5, EAvoidStartleSlightly = 6,
        EStartleFrontMon = 8, ESlightlyStartlePrevMons = 9, EStartlePrevMon = 10, EStartlePrevMons = 11, EBadlyStartleFrontMon = 12,
        EBadlyStartlePrevMons = 13, EStartlePrevMon2 = 14, EStartlePrevMons2 = 15, EShiftJudgeAttention = 16, EStartleMonWithJudgesAttention = 17,
        EJamsOthersButMissOneTurn = 18, EStartleMonsSameType = 19, EStartleCool = 20, EStartleBeauty = 21, EStartleCute = 22, EStartleSmart = 23,
        EStartleTough = 24, EMakeFollowingMonNervous = 25, EMakeFollowingMonsNervous = 26, EWorsenConditionOfPrevMons = 27,
        EBadlyStartlesMonsInGoodCondition = 28, EBetterIfFirst = 29, EBetterIfLast = 30, EAppealAsGoodAsPrevOnes = 31, EAppealAsGoodAsPrevOne = 32,
        EBetterWhenLater = 33, EBetterIfSameType = 35, EBetterIfDiffType = 36, EAffectedByPrevAppeal = 37, EImproveCondition = 38,
        EBetterWithGoodCondition = 39, ENextAppealEarlier = 40, ENextAppealLater = 41, EBadlyStartleMonsWithGoodAppeals = 45,
        EBetterWhenAudienceExcited = 46, EDontExciteAudience = 47, EUserMoreEasilyStartled = 1;

    // ---- AI_CheckBadMove

    private int CheckBadMove(int user, AppealMove move)
    {
        int effect = EffectOf(move);
        int order = UserOrder(user);
        switch (effect)
        {
            case EStartleFrontMon: case EStartlePrevMon: case EBadlyStartleFrontMon: case EStartlePrevMon2: case EAppealAsGoodAsPrevOne:
            case EBetterIfSameType: case EBetterIfDiffType: case EAffectedByPrevAppeal:
                // If the one before is skipping its turn (or the user is first), discourage the move
                return order == 0 ? -10 : CanParticipateAt(order - 1) ? 0 : -10;
            case ESlightlyStartlePrevMons: case EStartlePrevMons: case EBadlyStartlePrevMons: case EStartlePrevMons2:
            case EStartleMonWithJudgesAttention: case EShiftJudgeAttention: case EJamsOthersButMissOneTurn: case EStartleMonsSameType:
            case EBadlyStartleMonsWithGoodAppeals: case EStartleCool: case EStartleBeauty: case EStartleCute: case EStartleSmart: case EStartleTough:
            case EBadlyStartlesMonsInGoodCondition: case EWorsenConditionOfPrevMons: case EAppealAsGoodAsPrevOnes:
                // If those before are all skipping their turns (or the user is first)
                if (order == 0) return -20;
                for (int t = 0; t < order; t++) if (CanParticipateAt(t)) return 0;
                return -15;
            case EMakeFollowingMonNervous:
                if (order == 3) return -10;
                return CanParticipateAt(order + 1) ? 0 : -10;
            case EMakeFollowingMonsNervous: case EDontExciteAudience:
                if (order == 3) return -10;
                for (int t = order + 1; t < Contestants; t++) if (CanParticipateAt(t)) return 0;
                return -10;
            case EImproveCondition:
                return UserStars(user) < 3 ? 0 : -20;
            case EAvoidStartleOnce: case EAvoidStartle: case EAvoidStartleSlightly:
                if (order == 3) return -10;
                for (int t = order + 1; t < Contestants; t++) if (CanParticipateAt(t)) return 0;
                return -10;
            case EGreatAppealButNoMoreMoves:
                if (LastAppeal) return Roll(20) ? 0 : 20;
                int limit = (AppealNumber + 1) * 20;
                return Roll(limit) ? 0 : -15;
            default:
                return 0;
        }
    }

    // ---- AI_CheckCombo

    private int CheckCombo(int user, AppealMove move)
    {
        if (WouldFinishCombo(user, move)) return 25;
        int score = 0;
        if (UsedCount(user, move) == 0 && IsComboStarter(user, move))
        {
            bool onLast = LastAppeal;
            switch (UserOrder(user))
            {
                case 0: score += onLast ? (Roll(125) ? 0 : -15) : (Roll(150) ? 0 : 10); break;
                case 1: score += onLast ? (Roll(125) ? 0 : -15) : (Roll(125) ? 0 : 10); break;
                case 2: score += onLast ? (Roll(125) ? 0 : -15) : (Roll(50) ? 0 : 10); break;
                default: score += onLast ? (Roll(125) ? 0 : -15) : 10; break;
            }
        }
        if (IsComboFinisher(user, move)) score -= 10;
        return score;
    }

    // ---- AI_CheckBoring

    private int CheckBoring(int user, AppealMove move)
    {
        if (EffectOf(move) == ERepetitionNotBoring) return 0;
        return UsedCount(user, move) switch { 1 => -5, 2 => -15, 3 => -20, 4 => -25, _ => 0 };
    }

    // ---- AI_CheckExcitement

    private int CheckExcitement(int user, AppealMove move)
    {
        int excitement = Excitement(move);
        int order = UserOrder(user);
        if (excitement < 0)
        {
            if (ApplauseLevel == 4)
            {
                if (order != 0) return Roll(127) ? 0 : -10;
                return Roll(51) ? 0 : 20;
            }
            if (ApplauseLevel == 3)
            {
                if (order != 0) return Roll(127) ? 0 : 10;
                if (LastAppeal) return 15;
                return Roll(51) ? 0 : 10;
            }
            return UserHasExcitingMove(user) ? 0 : 15;
        }
        if (excitement == 0) return Roll(127) ? 0 : 10;
        // Positive
        bool repeat = UsedCount(user, move) > 0;
        if (repeat && EffectOf(move) != ERepetitionNotBoring) return 0;
        if (order == 0 && ApplauseLevel == 4) return 30;
        return Roll(100) ? 0 : 10;
    }

    // ---- AI_CheckOrder

    private int CheckOrder(int user, AppealMove move)
    {
        int effect = EffectOf(move);
        var type = TypeOf(move);
        switch (UserOrder(user))
        {
            case 0:
                if (effect == EBetterIfFirst) return 15;
                if (effect == EBetterWhenLater) return -15;
                if (type == ContestEffectType.AvoidStartle) return Roll(100) ? 0 : 10;
                return 0;
            case 1:
                if (effect == EBetterWhenLater) return -5;
                if (type == ContestEffectType.AvoidStartle) return Roll(125) ? 0 : 10;
                return 0;
            case 2:
                return effect is EBetterWhenLater or EAppealAsGoodAsPrevOnes or EUserMoreEasilyStartled ? 5 : 0;
            default:
                if (effect is EBetterWhenLater or EBetterIfLast or EAppealAsGoodAsPrevOnes or EUserMoreEasilyStartled) return 15;
                if (effect == EJamsOthersButMissOneTurn) return 5;
                if (type == ContestEffectType.AvoidStartle) return -10;
                if (type == ContestEffectType.StartleMons) return Roll(125) ? 0 : 10;
                return 0;
        }
    }

    // ---- AI_CheckGoodMove

    private int CheckGoodMove(int user, AppealMove move)
    {
        int effect = EffectOf(move);
        int order = UserOrder(user);
        switch (effect)
        {
            case EBetterWithGoodCondition:
                return UserStars(user) switch { 3 => 20, 2 => Roll(125) ? 0 : 15, 1 => Roll(125) ? 0 : 5, _ => -20 };
            case ENextAppealEarlier:
                if (!UserHasEffect(user, EBetterIfFirst)) return 0;
                return Roll(50) ? 0 : 20;
            case ENextAppealLater:
                if (!UserHasEffect(user, EBetterIfLast)) return 0;
                return Roll(50) ? 0 : 20;
            case ERepetitionNotBoring:
                if (order != 3) return 0;
                return Roll(50) ? 0 : 15;
            case EImproveCondition:
                if (!UserHasEffect(user, EBetterWithGoodCondition))
                {
                    if (LastAppeal) return -10;
                    if (AppealNumber == 0) return Roll(100) ? 0 : 10;
                    if (UsedCount(user, move) == 1) return 0;
                    return Roll(125) ? 0 : 10;
                }
                if (UserStars(user) == 3) return -10;
                return Roll(50) ? 0 : 15;
            case EDontExciteAudience:
                if (UsedCount(user, move) == 1) return 0;
                if (order <= 1) return Roll(127) ? 0 : 10;
                if (!LastAppeal) return 0;
                if (UserHasExcitingMove(user)) return 0;
                if (ApplauseLevel < 1) return 0;
                return 10;
            case EAppealAsGoodAsPrevOnes:
                return order switch { 1 => 5, 2 => 15, 3 => 20, _ => 0 };
            case EAppealAsGoodAsPrevOne:
            {
                if (order == 0) return -10;
                int score = 0;
                for (int t = 0; t < order; t++) if (CanParticipateAt(t)) score += 5;
                return score;
            }
            case EBetterWhenAudienceExcited:
                // Emerald's bug kept: the first-up check looks at "not the first appeal", where the script means the first
                if (order == 0)
                {
                    if (AppealNumber != 0) return Roll(125) ? 0 : -15;
                    if (ApplauseLevel == 4) return Roll(125) ? 0 : 20;
                    if (ApplauseLevel == 3) return Roll(125) ? 0 : 15;
                    return 0;
                }
                return Roll(178) ? 0 : 10;
            case EWorsenConditionOfPrevMons:
            {
                if (order == 0) return 0;
                int score = 0;
                for (int t = 0; t < order && t < 3; t++)
                {
                    int who = ContestantAtTurn(t);
                    if (status[who].TurnDisabled) continue;
                    int stars = status[who].Condition / 10;
                    if (stars == 0) continue;
                    if (!Roll(125)) score += stars * 5;
                }
                return score;
            }
            case EShiftJudgeAttention:
            case EStartleMonWithJudgesAttention:
            {
                // Emerald's bug kept: it asks whether the one before has NOT used a combo starter
                if (order == 0) return 0;
                int score = 0;
                for (int t = 0; t < order && t < 3; t++)
                {
                    int who = ContestantAtTurn(t);
                    if (status[who].TurnDisabled) continue;
                    bool usedStarter = CanCombo(who) && status[who].PrevMove != null && status[who].PrevMove!.Contest.ComboStarter != 0;
                    if (usedStarter) continue;
                    if (Roll(125)) continue;
                    score += 2;
                    if (status[who].CompletedComboFlag) score += 8;
                }
                return score;
            }
            case EMakeFollowingMonsNervous:
            {
                if (order == 3) return 0;
                int score = 0;
                for (int t = Contestants - 1; t > order; t--)
                {
                    int who = ContestantAtTurn(t);
                    if (status[who].TurnDisabled) continue;
                    bool usedStarter = CanCombo(who) && status[who].PrevMove != null && status[who].PrevMove!.Contest.ComboStarter != 0;
                    if (!usedStarter) continue;
                    score += 5;
                    if (!Roll(125)) score += 5;
                }
                return score;
            }
            case EJamsOthersButMissOneTurn:
            {
                int score = LastAppeal ? 5 : 0;
                switch (order)
                {
                    case 0: score -= 15; break;
                    case 1: if (!Roll(125)) score -= 10; break;
                    case 2: if (!Roll(125)) score += 5; break;
                    default: if (!Roll(125)) score += 15; break;
                }
                return score;
            }
            default:
                return 0;
        }
    }

    // ---- AI_Erratic

    private int Erratic() =>
        Category is ContestStat.Cute or ContestStat.Smart or ContestStat.Tough ? (Roll(125) ? 0 : 10) : 0;
}
