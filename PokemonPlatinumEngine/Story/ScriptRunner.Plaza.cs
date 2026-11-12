using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The commands of plan 41 · EG3 and EG6: <c>plaza</c>, Pennant Hall (<see cref="Plaza"/>), and <c>missions</c>, the
/// Week's Work (<see cref="MissionBoard"/>). Where the host keeps neither, both do nothing and hear 0.
/// </summary>
public sealed partial class ScriptRunner
{
    /// <summary>
    /// <c>open</c> opens the hall to its manager (RESULT the rank); <c>rank</c> is the rank, <c>next</c> the pennants still to
    /// earn for the next and <c>pitches</c> the pitches at the rank (each RESULT and <c>{number}</c>); <c>coins</c> the
    /// pennants held (RESULT, <c>{pennants}</c>), which stand over the field till the script ends; <c>pitch N</c> asks which
    /// stall stands at pitch N (the stalls offered, then EMPTY and LEAVE IT: RESULT 1 changed, 0 left, 2 for a pitch the
    /// rank hasn't, <c>{thing}</c> the stall); <c>give N</c> and <c>spend N</c> pay pennants in or out (RESULT 1, 0 when
    /// there aren't enough to spend); <c>guests</c> draws the day's guests on the first coming of a day (RESULT how many
    /// stand today). Said of a person of the hall: <c>stall</c> is the number in the catalogue of the stall the keeper keeps
    /// (0 for none, <c>{thing}</c> its name); <c>shop</c> opens its goods for pennants at the rank and waits; <c>greet</c>
    /// is a guest's first word of the day (RESULT the pennants it gave, 0 once greeted; <c>{foe}</c> their name);
    /// <c>battle</c> fights the guest, once a day, their team lifted to the furthest finished region's post-game level on a
    /// copy (RESULT 1 won, paid <see cref="PlazaFile.GuestWin"/>, 0 lost, 2 already beaten today, 3 for no guest).
    /// </summary>
    private void PlazaCommand(Instruction i)
    {
        if (host.Plaza is not { } plaza)
        {
            Result = 0;
            return;
        }
        var story = host.Story;
        switch (i.Name)
        {
            case "open":
                plaza.Opened = true;
                Result = lastNumber = plaza.Rank;
                break;
            case "rank":
                Result = lastNumber = plaza.Rank;
                break;
            case "next":
                Result = lastNumber = Plaza.ToNextRank(plaza.Earned);
                break;
            case "pitches":
                Result = lastNumber = Plaza.PitchesAt(plaza.Rank);
                break;
            case "coins":
                Result = plaza.Coins;
                host.ShowPennants(true);
                break;
            case "give":
                plaza.Give(i.Number);
                Result = 1;
                break;
            case "spend":
                Result = plaza.Spend(i.Number) ? 1 : 0;
                break;
            case "guests":
                if (plaza.GuestsFor(GameClock.Today, story, host.Chance))
                {
                    Plaza.KeepFlags(story, plaza);
                    host.PlazaChanged();
                }
                Result = lastNumber = plaza.Guests.Count;
                break;
            case "pitch":
            {
                int pitch = i.Number;
                lastThing = plaza.At(pitch) is { } standing ? StallName(standing) : "";
                if (pitch > Plaza.PitchesAt(plaza.Rank))
                {
                    Result = 2;
                    break;
                }
                var offered = Plaza.OfferedNow(story, plaza.Rank).Select(s => s.Id).ToList();
                var answers = offered.Select(StallName).ToList();
                answers.Add(Text.Of("hubPlaza.pitch.empty"));
                answers.Add(Text.Of("hubPlaza.pitch.leave"));
                host.Ask(speakerShown, Text.Of("hubPlaza.pitch.which", ("n", pitch)), answers, cancel: answers.Count - 1);
                afterBusy = () =>
                {
                    int a = host.Answer;
                    string? chosen = a >= 0 && a < offered.Count ? offered[a] : null;
                    bool clears = a == offered.Count;
                    if (chosen == null && !clears)
                    {
                        Result = 0;
                        return;
                    }
                    Result = plaza.Set(pitch, chosen, story) ? 1 : 0;
                    lastThing = chosen != null ? StallName(chosen) : "";
                    if (Result == 1)
                    {
                        Plaza.KeepFlags(story, plaza);
                        host.PlazaChanged();
                    }
                };
                break;
            }
            case "stall":
            {
                int pitch = Plaza.NumberIn(Subject?.Id, Plaza.KeeperPrefix);
                string? stall = plaza.At(pitch);
                lastThing = stall != null ? StallName(stall) : "";
                Result = lastNumber = Plaza.NumberOf(stall);
                break;
            }
            case "shop":
            {
                string? stall = plaza.At(Plaza.NumberIn(Subject?.Id, Plaza.KeeperPrefix));
                if (stall == null || Plaza.Stall(stall) is not { Goods.Count: > 0 })
                {
                    Result = 0;
                    break;
                }
                host.Open(ScriptScreen.Shop, Subject, PlazaShopCounter + stall);
                afterBusy = () => Result = 1;
                break;
            }
            case "greet":
            {
                int spot = Plaza.NumberIn(Subject?.Id, Plaza.GuestPrefix);
                lastFoe = plaza.GuestAt(spot) is { } guest ? GuestName(guest) : "";
                Result = lastNumber = plaza.Greet(spot);
                break;
            }
            case "battle":
            {
                if (NoPokemonToBattle()) break;
                int spot = Plaza.NumberIn(Subject?.Id, Plaza.GuestPrefix);
                if (Subject is not { } person || plaza.GuestAt(spot) is not { } guest)
                {
                    Result = 3;
                    break;
                }
                lastFoe = GuestName(guest);
                if (plaza.Battled.Contains(spot))
                {
                    Result = 2;
                    break;
                }
                // The guest's team on a copy, lifted to the journey's level, under an id of the hall's own: the trainer
                // of their region is never beaten by a battle here
                var team = new Trainer { Id = guest.Id };
                TrainerDatabase.Fill(team, guest);
                var lifted = LevelLift.To(team, Plaza.GuestLevel(story));
                lifted.Id = PlazaGuestTrainer + guest.Id;
                person.TrainerData = lifted;
                host.Battle(person, null, null, mayLose: true, first: false);
                afterBusy = () =>
                {
                    person.TrainerData = null;
                    if (host.Outcome == BattleOutcome.Won)
                    {
                        plaza.Battled.Add(spot);
                        plaza.Give(Plaza.File.GuestWin);
                        lastNumber = Plaza.File.GuestWin;
                        Result = 1;
                    }
                    else Result = 0;
                };
                break;
            }
        }
    }

    /// <summary>What a stall's goods counter is opened with: the counter's name begins so, and the stall's id follows.</summary>
    public const string PlazaShopCounter = "plaza:";

    /// <summary>What a guest's team is battled under: the hall's own id before the trainer's (plan 41 · EG3).</summary>
    public const string PlazaGuestTrainer = "plaza_guest_";

    private static string StallName(string id) => Text.Of($"hubPlaza.stall.{id}.name");

    private static string GuestName(TrainerRecord guest) => Text.Of("battle.fullTitle", ("class", Trainer.ClassName(guest.Class)), ("name", guest.Name));

    /// <summary>
    /// <c>read</c> reads the board: on a new week the week is drawn anew from the regions finished (RESULT how many missions
    /// it posts); <c>open</c> opens the board's screen and waits (RESULT the pennants it paid out); <c>begin N</c> begins
    /// the board's Nth mission where it is timed (RESULT 1, 0 where it can't be); <c>waiting</c> is how many rewards wait
    /// (RESULT, <c>{number}</c>); <c>claim</c> takes every reward waiting, its pennants to the plaza and its items to the
    /// bag (RESULT and <c>{number}</c> the pennants).
    /// </summary>
    private void MissionsCommand(Instruction i)
    {
        if (host.Missions is not { } board)
        {
            Result = 0;
            return;
        }
        switch (i.Name)
        {
            case "read":
                board.Refresh(GameClock.Today, Plaza.Finished(host.Story).Select(r => r.Id).ToList());
                Result = lastNumber = board.Missions.Count;
                break;
            case "open":
                board.Refresh(GameClock.Today, Plaza.Finished(host.Story).Select(r => r.Id).ToList());
                host.Open(ScriptScreen.MissionBoard, Subject);
                afterBusy = () => Result = lastNumber = host.Answer;
                break;
            case "begin":
                Result = board.Begin(i.Number - 1) ? 1 : 0;
                break;
            case "waiting":
                Result = lastNumber = board.Unclaimed;
                break;
            case "claim":
            {
                int pennants = 0;
                foreach (var reward in board.ClaimAll())
                {
                    pennants += reward.Pennants;
                    if (reward.Item != null && ItemDatabase.Get(reward.Item) is { } item)
                    {
                        host.Bag.AddItem(item, 1);
                        lastItem = item.Name;
                    }
                }
                host.Plaza?.Give(pennants);
                Result = lastNumber = pennants;
                break;
            }
        }
    }
}
