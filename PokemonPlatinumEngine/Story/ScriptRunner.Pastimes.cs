using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The commands of plan 06 · R16b: <c>fossil</c> (the Oreburgh Mining Museum's revival), <c>gamecorner</c> (the Coin
/// Case, the machines, the prize exchange and its Hidden Power reader), <c>villa</c> (the catalogue and the room's
/// furniture) and <c>palpark</c> (the Catching Show). Each is on the original's script commands, named beside it.
/// </summary>
public sealed partial class ScriptRunner
{
    private int lastPrice, lastNumber;

    /// <summary>
    /// <c>fossil count</c> is how many Fossils the bag holds (<c>GetFossilCount</c>); <c>choose</c> picks the one to
    /// revive, asking which when the bag holds more than one kind (RESULT 1, or 0 when none is chosen; <c>{item}</c>
    /// names it and <c>{thing}</c> what it was); <c>revive</c> takes it and remembers the species
    /// (<c>GetSpeciesFromFossil</c>, <c>VAR_REVIVED_POKEMON_SPECIES</c>); <c>ready</c> is 1 while a Pokémon waits to be
    /// collected, and names it; <c>give</c> hands it over at level 20 (RESULT as <c>givepokemon</c>'s).
    /// </summary>
    private void Fossil(Instruction i)
    {
        var story = host.Story;
        switch (i.Name)
        {
            case "count":
                Result = Fossils.Count(host.Bag);
                break;
            case "choose":
            {
                var held = Fossils.Held(host.Bag);
                void Pick(string fossil)
                {
                    lastItem = fossil;
                    lastThing = Fossils.SpeciesOf(fossil) ?? "";
                    Result = 1;
                }
                if (held.Count == 0) Result = 0;
                else if (held.Count == 1) Pick(held[0]);                                       // FindFossilAtThreshold
                else
                {
                    host.Ask(speakerShown, Text.Of("scriptLines.whichFossil"), held.Select(LocalNames.Item).Append(Text.Of("scriptLines.cancel")).ToList(), cancel: held.Count);
                    afterBusy = () =>
                    {
                        if (host.Answer >= 0 && host.Answer < held.Count) Pick(held[host.Answer]);
                        else Result = 0;
                    };
                }
                break;
            }
            case "revive":
            {
                string? species = Fossils.SpeciesOf(lastItem);
                var item = ItemDatabase.Get(lastItem);
                if (species == null || item == null || !host.Bag.RemoveItem(item, 1)) { Result = 0; break; }
                story.SetVar(Fossils.RevivedVariable, PokemonDatabase.Get(species)!.DexNumber);
                lastThing = species;
                Result = 1;
                break;
            }
            case "ready":
                lastThing = Waiting(story) ?? "";
                Result = lastThing.Length > 0 ? 1 : 0;
                break;
            case "give":
            {
                string? species = Waiting(story);
                if (species == null) { Result = 0; break; }
                lastThing = species;
                Result = host.GivePokemon(new Pokemon(PokemonDatabase.Get(species)!, i.Number > 0 ? i.Number : Fossils.Level)) ? 1 : 2;
                story.SetVar(Fossils.RevivedVariable, 0);
                host.Pastimes.FossilsRevived++;
                break;
            }
        }
    }

    private static string? Waiting(StoryState story)
    {
        int number = story.Var(Fossils.RevivedVariable);
        return number == 0 ? null : Fossils.All.Select(f => f.Species).FirstOrDefault(s => PokemonDatabase.Get(s)?.DexNumber == number);
    }

    /// <summary>
    /// The Game Corner (<c>scrcmd_coins.c</c>, <c>scrcmd_game_corner_prize.c</c>, <c>ScrCmd_267</c>): <c>coins</c> is
    /// what the case holds; <c>room N</c> whether N more fit (<c>GoToIfCannotAddCoins</c>); <c>add N</c> and <c>take
    /// N</c> (all or none) change it; <c>slots N</c> sits the player at a machine and waits; <c>prize</c> asks which of
    /// the exchange's nineteen (RESULT 0 for none; <c>{item}</c>, <c>{price}</c>, and <c>{thing}</c> the move a TM
    /// holds); <c>buy</c> exchanges coins for it (RESULT 1, or 0 with too few). <c>hiddenpower</c> reads the Hidden
    /// Power of the Pokémon last chosen (<c>CalcHiddenPowerType</c>): RESULT 0 when none was chosen, 1 when it knows the
    /// move, 2 when it doesn't, 3 for an Egg; <c>{thing}</c> names the type. <c>cointrick</c> is the Coin Case's clown's
    /// toss: RESULT 0 or 1.
    /// </summary>
    private void GameCorner(Instruction i)
    {
        var coins = host.Pastimes.Coins;
        switch (i.Name)
        {
            case "coins":
                Result = coins.Coins;
                break;
            case "room":
                Result = coins.CanAdd(i.Number) ? 1 : 0;
                break;
            case "add":
                Result = coins.Add(i.Number) ? 1 : 0;
                break;
            case "take":
                Result = coins.Take(i.Number) ? 1 : 0;
                break;
            case "slots":
                Result = host.PastimeAct("slots", i.Number, Subject);
                break;
            case "prize":
            {
                var prizes = GameCornerPrizes.All;
                // A TM is listed with the move it holds
                var rows = prizes.Select(p => PrizeRow(p.Item, "scriptLines.priceCoins", "price", p.Coins.ToString("N0")))
                    .Append(Text.Of("scriptLines.noThanks")).ToList();
                host.Ask(speakerShown, Text.Of("scriptLines.whichPrize"), rows, cancel: prizes.Length);
                afterBusy = () =>
                {
                    if (host.Answer < 0 || host.Answer >= prizes.Length) { Result = 0; return; }
                    var (item, price) = prizes[host.Answer];
                    lastItem = item;
                    lastPrice = price;
                    lastThing = ItemDatabase.Get(item)?.TeachesMove ?? "";
                    Result = 1;
                };
                break;
            }
            case "buy":
            {
                var item = ItemDatabase.Get(lastItem);
                if (item == null || !coins.Take(lastPrice)) { Result = 0; break; }
                host.Bag.AddItem(item, 1);
                Result = 1;
                break;
            }
            case "cointrick":
                // The Coin Case's clown (scripts_veilstone_city_southeast_house.s): GetRandom 2, whatever hand was said
                Result = host.Chance.Next(2);
                break;
            case "hiddenpower":
            {
                var pokemon = Result >= 0 && Result < host.Party.Count ? host.Party.Members[Result] : null;
                if (pokemon == null) { Result = 0; break; }
                if (pokemon.IsEgg) { Result = 3; break; }
                lastThing = Battle.Sim.BattleCore.HiddenPowerType(pokemon).ToString();
                Result = pokemon.Moves.Any(m => m.Data.Name == "Hidden Power") ? 1 : 2;
                break;
            }
        }
    }

    /// <summary>
    /// The Villa (<c>scripts_villa.s</c>): <c>catalogue</c> asks which piece of what the catalogue lists now (RESULT 1
    /// for one to order, 2 for one already there, 0 for none; <c>{thing}</c> and <c>{price}</c>); <c>choose "Table"</c>
    /// names one without asking, and <c>is "Piano"</c> is 1 when that is the piece last chosen; <c>buy</c> pays for it and it is the player's (RESULT 0 with too little money);
    /// <c>furnish</c> stands what is owned in the room; <c>owned</c> counts it.
    /// </summary>
    private void Villa(Instruction i)
    {
        var story = host.Story;
        int Chosen(VillaFurniture piece)
        {
            lastThing = piece.Name;
            lastPrice = piece.Price;
            return VillaRules.Owns(story, piece) ? 2 : 1;
        }
        switch (i.Name)
        {
            case "catalogue":
            {
                var listed = VillaRules.Catalogue(story, host.VillaRecords);
                var rows = listed.Select(p => VillaRules.Owns(story, p) ? Text.Of("scriptLines.soldOut", ("name", p.Name))
                        : p.Price == 0 ? Text.Of("scriptLines.free", ("name", p.Name))
                        : Text.Of("scriptLines.priceDollars", ("name", p.Name), ("price", p.Price.ToString("N0"))))
                    .Append(Text.Of("scriptLines.cancel")).ToList();
                host.Ask(speakerShown, Text.Of("scriptLines.whichPiece"), rows, cancel: listed.Count);
                afterBusy = () => Result = host.Answer >= 0 && host.Answer < listed.Count ? Chosen(listed[host.Answer]) : 0;
                break;
            }
            case "choose":
                Result = Chosen(VillaRules.Find(i.Other)!);
                break;
            case "is":
                Result = lastThing == VillaRules.Find(i.Other)!.Name ? 1 : 0;
                break;
            case "buy":
            {
                var piece = VillaRules.Find(lastThing);
                if (piece == null || host.Money < piece.Price) { Result = 0; break; }
                host.Money -= piece.Price;
                story.Set(piece.Flag);
                Result = 1;
                break;
            }
            case "furnish":
                Result = host.PastimeAct("furnish", 0, Subject);
                break;
            case "owned":
                Result = VillaRules.Owned(story);
                break;
        }
    }

    /// <summary>
    /// Pal Park's Catching Show (<c>scrcmd_catching_show.c</c>, <c>scripts_pal_park_lobby.s</c>): <c>check</c> is 0
    /// when a show can begin, 1 when the boxes haven't six free places, 2 when six Pokémon don't wait in the park (with
    /// nothing waiting, or another day's six, it first lets the day's six visitors come, plan 03 · D12a:
    /// <see cref="PalParkState.Welcome"/>);
    /// <c>start</c> lets the six loose; <c>go</c> starts the clock; <c>active</c> is 1 while a show is on;
    /// <c>caught</c> how many are caught; <c>points catching|time|type|total</c> the score's parts
    /// (<c>CalcCatchingShowPoints</c>; <c>{number}</c> too), which stops the clock; <c>record</c> is 1 when the total
    /// beats the record, and keeps it; <c>prize</c> hands over the berry the score earns; <c>store</c> sends the six
    /// to the boxes (<c>MoveCatchingShowMonsToPCBoxes</c>); <c>end</c> ends the show and keeps nothing of it.
    /// </summary>
    private void PalPark(Instruction i)
    {
        var park = host.Pastimes.PalPark;
        switch (i.Name)
        {
            case "check":
                // Plan 03 · D12a: with nothing brought over, the day's six visitors come, by a fixed rotation of the
                // species Sinnoh has no other way to
                park.Welcome(SpeciesSources.Sinnoh.PalParkPool, Core.GameClock.Today, host.Chance);
                Result = host.PastimeAct("boxroom", 0, Subject) < Overworld.CatchingShow.Size ? 1 : park.Stocked ? 0 : 2;
                break;
            case "start":
                Result = park.Begin(host.Chance) ? 1 : 0;
                break;
            case "go":
                park.StartClock();
                break;
            case "active":
                Result = park.Show != null ? 1 : 0;
                break;
            case "caught":
                Result = park.Show?.Caught ?? 0;
                break;
            case "points":
            {
                park.Stop();
                var show = park.Show;
                Result = lastNumber = show == null ? 0 : i.Other switch
                {
                    "catching" => show.CatchingPoints,
                    "time" => show.TimePoints,
                    "type" => show.TypePoints,
                    _ => show.Score
                };
                break;
            }
            case "record":
            {
                int total = park.Show?.Score ?? 0;
                Result = total > host.Story.Var(PalParkState.RecordVariable) ? 1 : 0;
                if (Result == 1) host.Story.SetVar(PalParkState.RecordVariable, total);
                break;
            }
            case "prize":
            {
                var item = ItemDatabase.Get(PalParkState.Prize(park.Show?.Score ?? 0, host.Chance));
                if (item == null) { Result = 0; break; }
                host.Bag.AddItem(item, 1);
                lastItem = item.Name;
                host.Fanfare(FanfareFor(item));
                host.Say(null, new List<string> { Text.Of("found.received", ("n", 1), ("item", item.DisplayName)), Text.Of("found.putAway", ("n", 1), ("pocket", PocketName(item.Pocket))) });
                Result = 1;
                break;
            }
            case "store":
                Result = host.PastimeAct("store", 0, Subject);
                break;
            case "end":
                park.Abandon();
                break;
        }
    }
}
