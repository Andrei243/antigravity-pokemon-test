using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Facilities;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// <c>facility stadium &lt;word&gt;</c>, the cups and the Leaders' round in the stadium off the Battle Frontier annex's lobby
/// (plan 37 · FC7, <see cref="Cups"/>, <see cref="LeadersRound"/>). A run lives in the story's own variables and, for the
/// three chosen, in the one script that plays it at the desk.
/// </summary>
public sealed partial class ScriptRunner
{
    /// <summary>The stadium's word under <c>facility</c>.</summary>
    public const string StadiumPlace = "stadium";

    // The three going in (the player's own copies or the rentals), as the desk's script has them
    private Party? stadiumTeam;
    private IReadOnlyList<int> stadiumSlots = Array.Empty<int>();

    /// <summary>
    /// The cups: <c>cup &lt;id&gt;</c> chooses one; <c>choose</c> has three of the team chosen (RESULT 1, 0 when backed out of);
    /// <c>cupcheck</c> is why they may not enter (0 they may, else a <see cref="CupRefusal"/>); <c>rent</c> lends three rentals
    /// instead; <c>cupbegin</c> begins the run of eight; <c>cupnext</c> is the battle to come (<c>{number}</c>) and stands its
    /// trainer (<c>{foe}</c>); <c>cupbattle</c> fights it (RESULT 1 won, 0 lost); <c>cupwon</c> counts the win (RESULT 1 when it
    /// was the eighth); <c>cupprize</c> hands over the cup's points and, the first time, its prize (RESULT 1 the prize too);
    /// <c>cupend</c> ends the run. The round: <c>region &lt;name&gt;</c> chooses one (RESULT 1 when its round is open);
    /// <c>roundbegin</c> begins it with the three chosen; <c>roundnext</c> stands its next trainer (<c>{foe}</c>, <c>{number}</c>
    /// their place from 1); <c>roundbattle</c> fights them at 100 (RESULT 1 won); <c>roundwon</c> counts it (RESULT 1 when the
    /// round is cleared); <c>roundend</c> ends it.
    /// </summary>
    private void StadiumCommand(Instruction i)
    {
        var story = host.Story;
        CupRecord Cup() => Cups.All.ElementAtOrDefault(story.Var(Cups.CupVariable) - 1) ?? throw Wrong(i, "no cup is chosen ('facility stadium cup ...')");
        string Region() => LeadersRound.Regions.ElementAtOrDefault(story.Var(LeadersRound.RegionVariable) - 1) ?? throw Wrong(i, "no region's round is chosen");
        switch (i.Other)
        {
            case "cup":
            {
                int index = Cups.All.ToList().FindIndex(c => c.Id == i.Move);
                if (index < 0) throw Wrong(i, $"there is no cup '{i.Move}'");
                story.SetVar(Cups.CupVariable, index + 1);
                story.SetVar(Cups.RentedVariable, 0);
                stadiumTeam = null;
                Result = 1;
                break;
            }
            case "choose":
            {
                Result = host.FrontierAct("enter", Cups.Entered, Subject);
                afterBusy = () =>
                {
                    stadiumSlots = host.Entered.ToList();
                    Result = host.Answer == 1 && stadiumSlots.Count == Cups.Entered ? 1 : 0;
                };
                break;
            }
            case "cupcheck":
            {
                var chosen = stadiumSlots.Where(s => s >= 0 && s < host.Party.Count).Select(s => host.Party.Members[s]).ToList();
                Result = (int)Cups.WhyNot(chosen, Cup());
                break;
            }
            case "rent":
            {
                var cup = Cup();
                int seed = host.Chance.Next(0x10000);
                stadiumTeam = new Party();
                foreach (var p in Cups.Rentals(cup, Cups.DrawsFor(seed, 0))) stadiumTeam.Add(p);
                story.SetVar(Cups.RentedVariable, 1);
                Result = 1;
                break;
            }
            case "cupbegin":
            {
                var cup = Cup();
                if (story.Var(Cups.RentedVariable) == 0)
                {
                    if (stadiumSlots.Count != Cups.Entered) throw Wrong(i, "'facility stadium cupbegin' needs three chosen or rented");
                    stadiumTeam = FrontierRules.TeamFor(host.Party, stadiumSlots, cup.MaxLevel);
                }
                story.SetVar(Cups.WonVariable, 0);
                story.SetVar(Cups.SeedVariable, host.Chance.Next(0x10000));
                host.Frontier.ChallengesStarted++;
                Result = 1;
                break;
            }
            case "cupnext":
            {
                var cup = Cup();
                int battle = story.Var(Cups.WonVariable) + 1;
                var foe = Cups.Foe(cup, battle, Cups.DrawsFor(story.Var(Cups.SeedVariable), battle));
                StandFacilityFoe(foe);
                Result = lastNumber = battle;
                break;
            }
            case "cupbattle":
            {
                var cup = Cup();
                if (stadiumTeam == null) throw Wrong(i, "'facility stadium cupbattle' needs a run begun");
                int battle = story.Var(Cups.WonVariable) + 1;
                var foe = Cups.Foe(cup, battle, Cups.DrawsFor(story.Var(Cups.SeedVariable), battle)).AsTrainer();
                // Whole again every battle, as every place's copies are
                var team = new Party();
                foreach (var p in stadiumTeam.Members) team.Add(FrontierRules.ForBattle(p, cup.MaxLevel));
                towerFoes = new List<FrontierOpponent>();
                host.FrontierBattle(team, new List<Trainer> { foe }, null, doubles: false);
                afterBusy = () => Result = Outcome() == 1 ? 1 : 0;
                break;
            }
            case "cupwon":
            {
                int won = story.Var(Cups.WonVariable) + 1;
                story.SetVar(Cups.WonVariable, won);
                Result = won >= Cups.Battles ? 1 : 0;
                lastNumber = won;
                break;
            }
            case "cupprize":
            {
                var cup = Cup();
                host.Frontier.GivePoints(cup.Points);
                host.FrontierAct("points", 1, Subject);
                lastNumber = cup.Points;
                Result = 0;
                if (story.Var(Cups.WonCupVariable(cup)) == 0 && ItemDatabase.Get(cup.Prize) is { } item)
                {
                    story.SetVar(Cups.WonCupVariable(cup), 1);
                    host.Bag.AddItem(item, 1);
                    lastItem = item.Name;
                    Result = 1;
                }
                break;
            }
            case "cupend":
                story.SetVar(Cups.CupVariable, 0);
                story.SetVar(Cups.WonVariable, 0);
                story.SetVar(Cups.RentedVariable, 0);
                stadiumTeam = null;
                stadiumSlots = Array.Empty<int>();
                towerFoes = new List<FrontierOpponent>();
                break;
            case "region":
            {
                int index = LeadersRound.Regions.ToList().IndexOf(i.Move);
                if (index < 0) throw Wrong(i, $"there is no Leaders' round of '{i.Move}'");
                story.SetVar(LeadersRound.RegionVariable, index + 1);
                Result = LeadersRound.Open(i.Move, story.Has) ? 1 : 0;
                break;
            }
            case "roundbegin":
                if (stadiumSlots.Count != Cups.Entered) throw Wrong(i, "'facility stadium roundbegin' needs three chosen");
                stadiumTeam = LeadersRound.TeamFor(host.Party, stadiumSlots);
                story.SetVar(LeadersRound.BeatenVariable, 0);
                host.Frontier.ChallengesStarted++;
                Result = 1;
                break;
            case "roundnext":
            {
                var order = LeadersRound.TrainersOf(Region());
                int at = Math.Clamp(story.Var(LeadersRound.BeatenVariable), 0, order.Count - 1);
                var t = LeadersRound.TrainerOf(order[at]);
                StandFacilityFoe(new FacilityOpponent { Id = t.Id, Class = t.TrainerClass, Name = t.Name, Look = t.Look ?? "Trainer", Pokemon = t.Party.Members.ToList() });
                Result = lastNumber = at + 1;
                break;
            }
            case "roundbattle":
            {
                if (stadiumTeam == null) throw Wrong(i, "'facility stadium roundbattle' needs a round begun");
                var order = LeadersRound.TrainersOf(Region());
                var foe = LeadersRound.TrainerOf(order[Math.Clamp(story.Var(LeadersRound.BeatenVariable), 0, order.Count - 1)]);
                var team = new Party();
                foreach (var p in stadiumTeam.Members) team.Add(FrontierRules.ForBattle(p, LeadersRound.Level));
                towerFoes = new List<FrontierOpponent>();
                host.FrontierBattle(team, new List<Trainer> { foe }, null, doubles: false);
                afterBusy = () => Result = Outcome() == 1 ? 1 : 0;
                break;
            }
            case "roundwon":
            {
                string region = Region();
                int beaten = story.Var(LeadersRound.BeatenVariable) + 1;
                story.SetVar(LeadersRound.BeatenVariable, beaten);
                lastNumber = beaten;
                Result = beaten >= LeadersRound.TrainersOf(region).Count ? 1 : 0;
                if (Result == 1) story.SetVar(LeadersRound.ClearedVariable(region), 1);
                break;
            }
            case "roundend":
                story.SetVar(LeadersRound.RegionVariable, 0);
                story.SetVar(LeadersRound.BeatenVariable, 0);
                stadiumTeam = null;
                stadiumSlots = Array.Empty<int>();
                towerFoes = new List<FrontierOpponent>();
                break;
        }
    }

    // A trainer of the stadium stood as the room's `opponent` (where it has one) and named for {foe}
    private void StandFacilityFoe(FacilityOpponent foe)
    {
        var dressed = new FrontierOpponent { Number = 0, Pokemon = foe.Pokemon, Dress = new FrontierDress(foe.Id, foe.Class, foe.Name, foe.Look) };
        towerFoes = new List<FrontierOpponent> { dressed };
        lastFoe = $"{dressed.ShownClass} {dressed.ShownName}";
        host.FrontierAct("opponents", 0, Subject);
    }
}
