using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// Plan 40 · GE7 and GE8 (<c>docs/scripts.md</c>, "The gift box", "The trade board and blind trades"): the command
/// <c>gift</c>, which the Gift Runners say, the event Pokémon's own words of <c>givepokemon</c> and <c>giveegg</c>, and the
/// trade board's words of <c>trade</c>.
/// </summary>
public sealed partial class ScriptRunner
{
    /// <summary>The gift chosen on the box's screen (or the first due) for <c>gift give</c>; null for none.</summary>
    private Gift? chosenGift;

    /// <summary>
    /// <c>gift due</c>: RESULT how many gifts wait (and <c>{number}</c>); <c>gift waiting</c>: how many hold and wait for
    /// room on the team or for the run. <c>gift open</c>: the box's screen (the script waits): RESULT 1 with a gift chosen,
    /// 2 for TELL WORDS, 0 backed out of. <c>gift give</c>: the gift chosen (the first due where none was) handed over by
    /// its own script of <c>gifts.txt</c>, called as <c>call</c> calls, and noted taken: RESULT 1, or 0 with none.
    /// <c>gift words NAME</c>: the phrase kept under NAME told to the box: RESULT how many gifts it called up.
    /// <c>gift backdrop "Name"</c>: a backdrop into the Fashion Case with the item fanfare and its two lines (RESULT 0 for
    /// one the case holds already, and nothing said).
    /// </summary>
    private void GiftCommand(Instruction i)
    {
        var box = host.Gifts;
        var facts = host.GiftFacts;
        switch (i.Name)
        {
            case "due":
                Result = lastNumber = box?.Due(facts).Count ?? 0;
                break;
            case "waiting":
                Result = lastNumber = box?.Waiting(facts).Count ?? 0;
                break;
            case "open":
                chosenGift = null;
                host.Open(ScriptScreen.GiftBox, Subject);
                afterBusy = () =>
                {
                    var due = box?.Due(facts) ?? Array.Empty<Gift>();
                    int answer = host.Answer;
                    if (answer == GiftBoxAnswers.Words) Result = 2;
                    else if (answer >= 1 && answer <= due.Count)
                    {
                        chosenGift = due[answer - 1];
                        Result = 1;
                    }
                    else Result = 0;
                };
                break;
            case "give":
            {
                var due = box?.Due(facts) ?? Array.Empty<Gift>();
                var gift = chosenGift != null && due.Contains(chosenGift) ? chosenGift : due.FirstOrDefault();
                chosenGift = null;
                if (gift == null || box == null)
                {
                    Result = 0;
                    break;
                }
                var target = library.Find(GiftScripts + "." + gift.Script) ?? throw Wrong(i, $"there is no gift script '{gift.Script}' in {GiftScripts}.txt");
                box.Take(gift, facts.Today);
                Result = 1;
                if (calls.Count >= MostCalls) throw Wrong(i, $"scripts call one another more than {MostCalls} deep");
                calls.Push((script!, at));
                script = target;
                at = 0;
                break;
            }
            case "words":
            {
                var words = host.Story.PhraseOf(i.Other);
                Result = lastNumber = box?.Tell(words, facts).Count ?? 0;
                break;
            }
            case "backdrop":
            {
                int backdrop = SuperContestDatabase.Backdrop(i.Other);
                if (!host.FashionCase.AddBackdrop(backdrop))
                {
                    Result = 0;
                    break;
                }
                Result = 1;
                host.Fanfare(MusicRole.FanfareItem);
                host.Say(null, new List<string>
                {
                    Text.Of("found.received", ("n", 1), ("item", Text.Of("giftBox.backdrop", ("name", SuperContestDatabase.BackdropName(backdrop))))),
                    Text.Of("found.putInCase", ("n", 1))
                });
                break;
            }
        }
    }

    /// <summary>The file the gifts' scripts are in: <c>Data/scripts/gifts.txt</c>.</summary>
    public const string GiftScripts = "gifts";

    /// <summary>
    /// An event Pokémon's own (plan 40 · GE7): its ball, its ribbon and who sent it as its first trainer (a number of the
    /// sender's name, so it counts as another trainer's, as an event Pokémon does).
    /// </summary>
    private static void MarkAsAnEvent(Pokemon gift, Instruction i)
    {
        if (i.Ball.Length > 0) gift.Ball = i.Ball;
        if (i.Ribbon.Length > 0) RibbonRules.Give(gift, i.Ribbon);
        if (i.Sender.Length > 0) gift.OriginalTrainer = new TrainerMark(i.Sender, (int)(SplitMix.Fnv(i.Sender) % 65536), PlayerLook.Girl);
    }

    /// <summary>
    /// <c>trade board open</c>: the board's screen for the region the player is in (the script waits): RESULT the request
    /// chosen, 1 to 3, 4 for a blind trade, 0 backed out of. <c>trade board N</c> and <c>trade blind</c>: the Pokémon chosen
    /// last (<c>choosepokemon</c>'s RESULT) sent to request N or blind, and the one received through the game's own way in
    /// (its trade evolution plays): RESULT 1 done, 0 not the species or the level asked, 2 the request met already, 3 an
    /// Egg or the last able to fight, 4 a run that holds its Pokémon, 5 no blind trade left today.
    /// </summary>
    private void BoardTrade(Instruction i)
    {
        var board = host.Board;
        var today = GameClock.Today;
        if (i.Name == "board" && i.Other == "open")
        {
            host.Open(ScriptScreen.TradeBoard, Subject, host.Region);
            afterBusy = () => Result = host.Answer is >= 0 and <= TradeBoard.RequestsADay + 1 ? host.Answer : 0;
            return;
        }
        int slot = Result;
        if (board == null || slot < 0 || slot >= host.Party.Count)
        {
            Result = 0;
            return;
        }
        var given = host.Party.Members[slot];
        BoardRequest? request = null;
        if (i.Name == "board")
        {
            var requests = TradeBoard.Requests(today, host.Region, host.RegionsEntered);
            if (i.Number > requests.Count)
            {
                Result = 0;
                return;
            }
            request = requests[i.Number - 1];
        }
        string? why = board.WhyNot(today, request, given, host.Party, host.RunHoldsTrades);
        if (why != null)
        {
            Result = why switch { "met" => 2, "egg" or "last" => 3, "run" => 4, "spent" => 5, _ => 0 };
            return;
        }
        BoardTrainer trainer;
        string species;
        string? item = null;
        if (request != null)
        {
            (trainer, species, item) = (request.Trainer, request.Offers, request.Item);
            board.Meet(today, request);
        }
        else
        {
            (trainer, species) = TradeBoard.Blind(today, board.BlindMade, host.RegionsEntered);
            board.MakeBlind(today);
        }
        // A randomised run sends the species the gift would go to, as givepokemon gives (plan 13 · V6)
        if (Randomizer.For(host.Rules) is { MapsSpecies: true } random) species = PokemonDatabase.Get(random.Given(species))?.Name ?? species;
        var received = TradeBoard.Make(trainer, species, item, given.Level, today, host.Chance);
        host.Say(null, new List<string> { Text.Of("tradeBoard.sent", ("trainer", trainer.Name), ("given", given.DisplayName), ("received", received.DisplayName)) });
        host.ReceiveTraded(received, given);
        host.Story.Set(TradeBoard.FirstTradeFlag);
        Result = 1;
    }
}

/// <summary>The gift box's screen's answers past the gifts it lists (1 to n).</summary>
public static class GiftBoxAnswers
{
    /// <summary>TELL WORDS chosen.</summary>
    public const int Words = 255;
}
