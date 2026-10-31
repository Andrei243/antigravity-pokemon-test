using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The commands of plan 06 · R18a: <c>frontier</c> (what every facility of the Battle Frontier shares: Battle Points,
/// the Exchange Service Corner, the records' board, the commemorative prints, the judge of a Pokémon's potential) and
/// <c>tower</c> (a Battle Tower challenge from the counter to the last battle and back). Each is on the original's
/// script commands and <c>CallBattleTowerFunction</c>'s functions, named beside it.
/// </summary>
public sealed partial class ScriptRunner
{
    // The opponent about to be met (or the two), what a partner tells of, and what the judge read
    private string lastFoe = "", lastBanned = "";
    private readonly string[] allies = { "", "" }, allyMoves = { "", "" };
    private List<FrontierOpponent> towerFoes = new();
    private int judgedTotal, judgedBest;

    /// <summary>
    /// <c>points</c> is the Battle Points held (<c>{points}</c> in a line says them); <c>give N</c> adds some;
    /// <c>prize items|machines</c> asks which of a booth's prizes (<c>PokeMartFrontier</c>; RESULT 1, or 0 for none;
    /// <c>{item}</c> and <c>{price}</c>, and <c>{thing}</c> the move a TM holds) and <c>buy</c> exchanges points for it
    /// (RESULT 1, or 0 with too few); <c>records tower</c> opens a facility's board and waits; <c>print tower</c> hands
    /// over the commemorative print that waits (RESULT 0 none, 1 silver, 2 gold); <c>judge</c> reads the potential of
    /// the Pokémon RESULT names (<c>JudgeStats</c>: RESULT 0 with none chosen, 3 for an Egg, else 1; <c>{thing}</c> is
    /// its best stat), and <c>judged total|best</c> are the two numbers read; <c>save</c> saves the game where the
    /// player stands, as a counter does before a challenge goes up.
    /// </summary>
    private void Frontier(Instruction i)
    {
        var records = host.Frontier;
        switch (i.Name)
        {
            case "points":
                Result = records.BattlePoints;
                break;
            case "give":
                Result = records.GivePoints(i.Number);
                break;
            case "prize":
            {
                var stock = FrontierExchange.Stock(machines: i.Other == "machines");
                var rows = stock.Select(p => PrizeRow(p.Item, "scriptLines.priceBp", "price", p.Price.ToString()))
                    .Append(Text.Of("scriptLines.noThanks")).ToList();
                host.FrontierAct("points", 1, Subject);
                host.Ask(speakerShown, Text.Of("scriptLines.whichPrize"), rows, cancel: stock.Count);
                afterBusy = () =>
                {
                    if (host.Answer < 0 || host.Answer >= stock.Count) { Result = 0; return; }
                    lastItem = stock[host.Answer].Item;
                    lastPrice = stock[host.Answer].Price;
                    lastThing = ItemDatabase.Get(lastItem)?.TeachesMove ?? "";
                    Result = 1;
                };
                break;
            }
            case "buy":
            {
                var item = ItemDatabase.Get(lastItem);
                if (item == null || FrontierExchange.PriceOf(lastItem) != lastPrice || !records.SpendPoints(lastPrice)) { Result = 0; break; }
                host.Bag.AddItem(item, 1);
                Result = 1;
                break;
            }
            case "records":
                Result = host.FrontierAct("records", (int)FacilityOf(i.Other), Subject);
                break;
            case "print":
            {
                var print = FrontierBrains.HandOver(host.Story, FacilityOf(i.Other), out bool allGold);
                // Common_CheckAllFrontierGoldPrintsObtained: the fifth gold print counts for the Trainer Card
                if (allGold) host.FrontierAct("score", FrontierBrains.AllGoldScore, Subject);
                Result = (int)print;
                break;
            }
            case "judge":
            {
                var pokemon = Result >= 0 && Result < host.Party.Count ? host.Party.Members[Result] : null;
                if (pokemon == null) { Result = 0; break; }
                if (pokemon.IsEgg) { Result = 3; break; }
                // JudgeStats: the six IVs added up, and the highest of them, the first of the highest in the stats' order
                (string Name, int Iv)[] ivs =
                {
                    ("HP", pokemon.IvHP), ("Attack", pokemon.IvAttack), ("Defense", pokemon.IvDefense),
                    ("Speed", pokemon.IvSpeed), ("Sp. Atk", pokemon.IvSpAttack), ("Sp. Def", pokemon.IvSpDefense)
                };
                judgedTotal = ivs.Sum(s => s.Iv);
                judgedBest = ivs.Max(s => s.Iv);
                lastThing = ivs.First(s => s.Iv == judgedBest).Name;
                lastWearer = pokemon.DisplayName;
                Result = 1;
                break;
            }
            case "judged":
                Result = i.Other == "total" ? judgedTotal : judgedBest;
                break;
            case "save":
                // Common_SaveGame, as a facility's counter saves before a challenge goes up and as it comes back
                Result = host.FrontierAct("save", 0, Subject);
                break;
            case "prints":
                // (ours, plan 21 · H12, part f) how many facilities have handed over a print, silver or gold: what the
                // event islands' passes are given for (Lilycove's ferry desk)
                Result = FrontierBrains.Printed.Count(f => host.Story.Var(FrontierBrains.PrintVariable(f)) >= 2);
                break;
            case "printed":
                // (ours, plan 21 · H12, part g) the print a facility has handed over: 0 none, 1 silver, 2 gold (Scott's
                // shields for the Tower's)
                Result = (int)FrontierBrains.PrintOf(host.Story, FacilityOf(i.Other));
                break;
        }
    }

    private static FrontierFacility FacilityOf(string word) => word switch
    {
        "factory" => FrontierFacility.Factory,
        "hall" => FrontierFacility.Hall,
        "castle" => FrontierFacility.Castle,
        "arcade" => FrontierFacility.Arcade,
        _ => FrontierFacility.Tower
    };

    /// <summary>
    /// The Battle Tower (<c>scripts_battle_tower.s</c>, <c>frontier_scripts_battle_tower_battle_room.s</c>,
    /// <c>CallBattleTowerFunction</c>). At the counter: <c>enough [N]</c> is 1 when the team holds that many Pokémon
    /// that could go in together (<c>BT_FUNC_CHECK_ENOUGH_VALID_POKEMON</c>; <c>{banned}</c> lists the species of the
    /// banlist the Pokédex has seen); <c>begin single|double|multi</c> starts a challenge (<c>InitBattleTower</c>);
    /// <c>choose</c> has its Pokémon chosen (RESULT 0 when backed out of); <c>check</c> is 0, or 1 for two of a species
    /// and 2 for two holding the same item; <c>enter</c> takes the generator's state and draws the set's trainers
    /// (<c>BT_FUNC_UNK_57</c> and <c>52</c>); <c>pause</c> puts the challenge down and <c>resume</c> takes it up
    /// (RESULT 0 when none waits or its Pokémon are no longer as they were, which ends it); <c>forfeit</c> ends a
    /// challenge that was walked out on; <c>end</c> lets go of it; <c>active</c> is 1 while one is under way,
    /// <c>mode</c> its mode. In the battle room: <c>next</c> is the number of the trainer to come (<c>{number}</c>);
    /// <c>opponent</c> makes their team and stands them in the room (<c>{foe}</c>); <c>battle</c> is fought (RESULT 1
    /// won, 0 lost); <c>won</c> counts the win (RESULT 1 once seven are beaten) and <c>sevens</c> asks it again;
    /// <c>streak</c> is the streak as it stands; <c>palmer</c> who comes next (0, 1 for the silver print, 2 for the
    /// gold); <c>beat silver|gold</c> notes the Tower Tycoon beaten; <c>finish won|lost</c> writes the records;
    /// <c>points</c> hands over the Battle Points earned (<c>{number}</c>). Back in the lobby: <c>beaten</c> is who was
    /// beaten (0, 1, 2); <c>ribbon palmer|streak</c> gives the ribbons due (RESULT 1 when any was given); <c>trophy</c>
    /// sends the trophy due to the Underground's PC (RESULT 0 none, 1 to 3 as given with <c>{thing}</c> and
    /// <c>{number}</c> the streak, 4 with no room) and <c>waiting</c> says whether one is due. In the Battle Salon:
    /// <c>partners</c> makes the five's teams, <c>tells N</c> fills <c>{ally:0}</c>, <c>{allymove:0}</c> and their
    /// second, <c>partner N</c> chooses one, <c>partnered</c> is who was chosen (-1 for nobody). <c>door</c> draws which
    /// of the corridor's four doors the attendant leads to (0 to 3), and <c>salon</c> lets into the Battle Salon
    /// whoever the story has had the player meet.
    /// </summary>
    private void Tower(Instruction i)
    {
        var records = host.Frontier;
        var tower = host.Tower;
        BattleTowerChallenge Challenge() => host.Tower ?? throw Wrong(i, $"'tower {i.Name}' needs a challenge under way");
        switch (i.Name)
        {
            case "enough":
            {
                int needed = i.Number > 0 ? i.Number : tower?.PartySize ?? 3;
                lastNumber = needed;
                lastBanned = Listed(FrontierRules.BannedSeen(species => PokemonDatabase.Get(species) is { } s && host.FrontierAct("seen", s.DexNumber, Subject) == 1));
                Result = FrontierRules.EnoughToEnter(host.Party, needed) ? 1 : 0;
                break;
            }
            case "begin":
                host.Tower = BattleTowerChallenge.Begin(records, Enum.Parse<TowerMode>(i.Other, ignoreCase: true));
                records.Tower.LastMode = host.Tower.Mode;
                break;
            case "choose":
            {
                var challenge = Challenge();
                Result = host.FrontierAct("enter", challenge.PartySize, Subject);
                afterBusy = () =>
                {
                    Result = host.Answer == 1 && host.Entered.Count == challenge.PartySize ? 1 : 0;
                    if (Result == 1) challenge.Enter(host.Party, host.Entered);
                };
                break;
            }
            case "check":
                Result = (int)FrontierRules.WhyNot(Challenge().Entered(host.Party));
                break;
            case "enter":
            {
                var challenge = Challenge();
                challenge.TakeSeed(records);
                challenge.DrawTrainers();
                records.ChallengesStarted++;
                host.FrontierAct("journal", 0, Subject);
                break;
            }
            case "pause":
                Challenge().Pause(records);
                host.Tower = null;
                break;
            case "resume":
            {
                var resumed = BattleTowerChallenge.Resume(records);
                if (resumed == null) { Result = 0; break; }
                if (!resumed.StillValid(host.Party))
                {
                    BattleTowerChallenge.Forfeit(records, resumed.Mode);
                    Result = 0;
                    break;
                }
                resumed.Enter(host.Party, resumed.Slots);
                host.Tower = resumed;
                Result = 1;
                break;
            }
            case "forfeit":
                BattleTowerChallenge.Forfeit(records, tower?.Mode ?? records.Tower.LastMode);
                host.Tower = null;
                break;
            case "end":
                host.Tower = null;
                break;
            case "active":
                Result = tower != null ? 1 : 0;
                break;
            case "paused":
                Result = records.Tower.Paused != null ? 1 : 0;
                break;
            case "mode":
                Result = tower != null ? (int)tower.Mode : -1;
                break;

            case "next":
                Result = lastNumber = Challenge().NextOpponent;
                break;
            case "opponent":
            {
                towerFoes = Challenge().MakeOpponents();
                lastFoe = string.Join(" and ", towerFoes.Select(f => $"{f.Record.Class} {f.Record.Name}"));
                host.FrontierAct("opponents", 0, Subject);
                Result = towerFoes.Count;
                break;
            }
            case "battle":
            {
                var challenge = Challenge();
                if (towerFoes.Count == 0) towerFoes = challenge.MakeOpponents();
                var team = FrontierRules.TeamFor(host.Party, challenge.Slots);
                Trainer? partner = null;
                if (challenge.Mode == TowerMode.Multi && challenge.Partner is { } beside)
                {
                    partner = challenge.TeamOf(beside).AsTrainer();
                    partner.Look = BattleTowerChallenge.PartnerLooks[beside.Id];
                }
                var foes = towerFoes.Select(f => f.AsTrainer()).ToList();
                towerFoes = new List<FrontierOpponent>();
                host.FrontierBattle(team, foes, partner, doubles: challenge.Mode != TowerMode.Single);
                afterBusy = () => Result = host.Outcome == BattleOutcome.Won ? 1 : 0;
                break;
            }
            case "won":
            {
                var challenge = Challenge();
                challenge.Won();
                Result = challenge.DefeatedSeven ? 1 : 0;
                break;
            }
            case "sevens":
                Result = tower is { DefeatedSeven: true } ? 1 : 0;
                break;
            case "streak":
                Result = lastNumber = Challenge().Streak;
                break;
            case "palmer":
                Result = (int)Challenge().PalmerNext;
                break;
            case "beat":
            {
                var which = i.Other == "gold" ? FrontierPrint.Gold : FrontierPrint.Silver;
                Challenge().BeatPalmer = which;
                FrontierBrains.Beaten(host.Story, FrontierFacility.Tower, which);
                break;
            }
            case "brain":
                // RECORD_UNK_059, counted as the Frontier Brain walks in
                records.BrainsMet++;
                break;
            case "finish":
            {
                var challenge = Challenge();
                if (i.Other == "won")
                {
                    challenge.EndWon(records);
                    host.FrontierAct("score", BattleTowerChallenge.SetScore, Subject);
                }
                else challenge.EndLost(records);
                break;
            }
            case "points":
                Result = lastNumber = Challenge().GivePoints(records);
                host.FrontierAct("points", 1, Subject);
                break;

            case "beaten":
                Result = tower != null ? (int)tower.BeatPalmer : 0;
                break;
            case "ribbon":
            {
                var challenge = Challenge();
                Result = (i.Other == "palmer" ? challenge.GivePalmerRibbon(host.Party) : challenge.GiveStreakRibbon(host.Party)) ? 1 : 0;
                break;
            }
            case "trophy":
            {
                var underground = host.Underground;
                int due = BattleTowerChallenge.TrophyDue(records, () => underground.PcGoods.Count < Models.Underground.UndergroundState.MaxPcGoods);
                if (due is >= 1 and <= 3)
                {
                    var (streak, good) = BattleTowerChallenge.Trophies[due - 1];
                    underground.TryAddPcGood(UndergroundDatabase.GoodId(good));
                    lastThing = good;
                    lastNumber = streak;
                }
                Result = due;
                break;
            }
            case "waiting":
                Result = BattleTowerChallenge.TrophyWaiting(records);
                break;

            case "salon":
                // BattleTowerBattleSalon_OnTransition: whoever the story has had the player meet is in the Battle Salon
                for (int id = 0; id < BattleTowerChallenge.PartnerFlags.Length; id++)
                    if (host.Story.Has(BattleTowerChallenge.PartnerFlags[id])) host.Story.Unset(BattleTowerChallenge.SalonFlags[id]);
                break;
            case "partners":
                Challenge().MakePartners(host.Party);
                break;
            case "tells":
            {
                var candidate = Challenge().Candidates.FirstOrDefault(c => c.Id == i.Number) ?? throw Wrong(i, "the Battle Salon's partners haven't been given their teams ('tower partners')");
                for (int n = 0; n < 2; n++) (allies[n], allyMoves[n]) = BattleTowerChallenge.Tells(candidate, n);
                break;
            }
            case "partner":
                Challenge().ChoosePartner(i.Number);
                break;
            case "partnered":
                Result = tower?.Partner?.Id ?? -1;
                // Whoever stands as the partner in the room takes the look of the one chosen
                if (Result >= 0) host.FrontierAct("partner", Result, Subject);
                break;
            case "door":
                // BattleTowerCorridor_OnFrame_EnterBattleRoom: GetRandom 4, which of the corridor's four doors
                Result = host.Chance.Next(4);
                break;
        }
    }

    /// <summary>The opponents the last <c>tower opponent</c> made: who a host stands in the battle room.</summary>
    public IReadOnlyList<FrontierOpponent> TowerFoes => towerFoes;

    // "Mew", "Mew and Lugia", "Mew, Lugia and Celebi"
    private static string Listed(IReadOnlyList<string> names) => names.Count switch
    {
        0 => "",
        1 => names[0],
        _ => string.Join(", ", names.Take(names.Count - 1)) + " and " + names[^1]
    };
}
