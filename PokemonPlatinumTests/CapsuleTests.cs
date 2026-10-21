using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.SuperContests;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Ball Capsules, Seals and ribbons (plan 06 · R17b): the Seal Case (<c>src/ball_seal_info.c</c>), a capsule changed
/// and put on a ball (<c>src/applications/capsule_menu/</c>), when and where its Seals burst (<c>ov12_02237E54.c</c>,
/// <c>ov12_02235E94.c</c>), where Seals come from (the scripts of Solaceon Town's east house and the Sunyshore
/// Market), Platinum's eighty ribbons and their givers (<c>src/ribbon.c</c>, <c>scrcmd_party.c</c>, the scripts of
/// Julia's and Dr. Footstep's houses and the Ribbon Syndicate), the Fashion Case's backdrops and the contests' records.
/// Every number is the original's, worked out beside it.
/// </summary>
[Collection("MapDatabase")]
public class CapsuleTests
{
    private static int Seal(string name) => SealDatabase.Seal(name);

    private static Pokemon Mon(string species = "Turtwig", int level = 20) =>
        new(PokemonDatabase.Get(species)!, level, Gender.Male, Nature.Hardy, false);

    private static Pokemon Form(string form)
    {
        var p = new Pokemon(PokemonDatabase.SpeciesOfForm(form)!, 20, Gender.Genderless, Nature.Hardy, false);
        p.ChangeForm(form);
        return p;
    }

    // ---------------------------------------------------------------- the data

    [Fact]
    public void TheEightySealsAreTheOriginalsWithTheirPrices()
    {
        // generated/seals.txt: eighty after the dummy; sealTypeValues' price is 50 or 100 for the forty-nine the market
        // sells and nought for the letters and the last three
        Assert.Equal(80, SealDatabase.Count);
        Assert.Equal((1, 6, 7), (Seal("Heart Seal A"), Seal("HEART_SEAL_F"), Seal("STAR_SEAL_A")));
        Assert.Equal((50, 100, 50, 100), (SealDatabase.Price(Seal("Heart Seal A")), SealDatabase.Price(Seal("Heart Seal E")), SealDatabase.Price(Seal("Star Seal A")), SealDatabase.Price(Seal("Line Seal D"))));
        Assert.Equal(49, Enumerable.Range(1, 80).Count(s => SealDatabase.Price(s) > 0));
        // The name of the sixteenth is the bank's seventy-seventh (its row's nameIndex is 0x4D)
        Assert.Equal("Line Seal D", SealDatabase.Name(16));
        // Twenty-eight show a letter: A to Z, the Shock Seal and the Mystery Seal (CapsuleMenu_IsAlphabetSeal)
        var letters = Enumerable.Range(1, 80).Where(SealDatabase.IsLetter).ToList();
        Assert.Equal(28, letters.Count);
        Assert.Equal((50, 77), (letters[0], letters[^1]));
        Assert.Equal(("A Seal", "Z Seal", "Shock Seal", "Mystery Seal"), (SealDatabase.Name(50), SealDatabase.Name(75), SealDatabase.Name(76), SealDatabase.Name(77)));
        Assert.False(SealDatabase.IsLetter(Seal("Liquid Seal")));
        Assert.Equal(0, Seal("No Such Seal"));
    }

    [Fact]
    public void TheMarketSellsSevenSealsADayAndEveryPricedSealInAWeek()
    {
        // include/data/mart_items.h, SunyshoreMarketStockMonday to Sunday
        var monday = SealDatabase.Market(DayOfWeek.Monday).Select(SealDatabase.Id).ToList();
        Assert.Equal(new[] { "HEART_SEAL_A", "STAR_SEAL_B", "FIRE_SEAL_A", "SONG_SEAL_A", "LINE_SEAL_C", "ELE_SEAL_B", "PARTY_SEAL_D" }, monday);
        Assert.Equal("STAR_SEAL_A", SealDatabase.Id(SealDatabase.Market(DayOfWeek.Sunday)[0]));
        var week = Enum.GetValues<DayOfWeek>().SelectMany(SealDatabase.Market).ToList();
        Assert.Equal(49, week.Count);
        Assert.Equal(49, week.Distinct().Count());
        Assert.All(week, s => Assert.True(SealDatabase.Price(s) > 0));
    }

    [Fact]
    public void TrainersPokemonComeOutOfTheOriginalsCapsules()
    {
        // res/trainers/ball_capsules.json: Flint's Magmortar names the thirteenth, eight Fire Seal B
        var flint = TrainerDatabase.Get("elite_four_flint")!;
        var record = flint.Party.Single(p => p.BallSeal != 0);
        Assert.Equal(("Magmortar", 13), (record.Species, record.BallSeal));
        var magmortar = TrainerDatabase.Build(record);
        // Pokemon_SetBallSeal: capsule number 1 and the trainers' capsule
        Assert.Equal(1, magmortar.CapsuleId);
        Assert.Equal(8, magmortar.Capsule!.Seals.Count);
        Assert.All(magmortar.Capsule.Seals, s => Assert.Equal("Fire Seal B", SealDatabase.Name(s.Seal)));
        Assert.All(magmortar.Capsule.Seals, s => Assert.True(s.OnTheBall));
        // The others' balls are plain
        Assert.All(flint.Party.Where(p => p.BallSeal == 0), p => Assert.Equal(0, TrainerDatabase.Build(p).CapsuleId));
        Assert.True(TrainerDatabase.All.SelectMany(t => t.Party).Count(p => p.BallSeal != 0) >= 100);
        Assert.All(TrainerDatabase.All.SelectMany(t => t.Party), p => Assert.InRange(p.BallSeal, 0, SealDatabase.TrainerCapsules));
    }

    // ---------------------------------------------------------------- the Seal Case

    [Fact]
    public void TheCaseKeepsNinetyNineOfAKindCountingThoseOnCapsules()
    {
        var seals = new SealCase();
        int heart = Seal("Heart Seal A");
        // GiveOrTakeSeal: all or none
        Assert.True(seals.Give(heart, 90));
        Assert.False(seals.Give(heart, 10));
        Assert.True(seals.Give(heart, 9));
        Assert.Equal((99, 0, 99), (seals.Count(heart), seals.InUse(heart), seals.Anywhere(heart)));
        Assert.False(seals.CanFit(heart));
        // One on a capsule is still one of the ninety-nine
        Assert.True(seals.Take(heart, 2));
        seals.SetCapsule(3, new BallCapsule(new[] { new BallSeal(heart, 190, 70), new BallSeal(heart, 200, 80) }));
        Assert.Equal((97, 2, 99), (seals.Count(heart), seals.InUse(heart), seals.Anywhere(heart)));
        Assert.False(seals.Give(heart));
        Assert.False(seals.Take(heart, 98));
        // SealCase_CountUniqueSeals and the bag's total (CalcTotalBallSeals)
        seals.Give(Seal("Q Seal"), 3);
        Assert.Equal((2, 102), (seals.Unique, seals.Total));
        Assert.Equal(new[] { heart, Seal("Q Seal") }, seals.Owned);
        Assert.Equal(96, seals.Room(Seal("Q Seal")));
    }

    [Fact]
    public void ACapsuleIsChangedSealBySealAndKeptOrGivenUp()
    {
        var seals = new SealCase();
        int heart = Seal("Heart Seal A"), star = Seal("Star Seal B");
        seals.Give(heart, 2);
        seals.Give(star, 9);
        var edit = new CapsuleEdit(seals, 0);

        // SealPlacement_GetTouchedSeal: out of the case onto the ball's middle, in front
        Assert.Equal(0, edit.Take(heart));
        Assert.Equal((1, new BallSeal(heart, 190, 70)), (seals.Count(heart), edit.Seals[0]));
        edit.Move(0, 150, 70);
        Assert.True(edit.Drop(0));
        Assert.Equal(0, edit.Take(star));
        Assert.Equal(new[] { star, heart }, edit.Seals.Select(s => s.Seal));

        // SealPlacement_SealIsValid: sixty dots from (190, 70) still sticks, sixty-one goes back into the case
        edit.Move(0, 250, 70);
        Assert.Equal(60, edit.Seals[0].Distance);
        Assert.True(edit.Drop(0));
        edit.Move(0, 251, 70);
        Assert.False(edit.Drop(0));
        Assert.Equal((9, 1), (seals.Count(star), edit.Seals.Count));
        // The distance is whole dots, what is past the point dropped (FX_Sqrt): 42² + 43² is 3613, whose root is 60.1
        Assert.Equal(60, CapsuleRules.Distance(190 + 42, 70 + 43, 190, 70));
        Assert.True(new BallSeal(star, 232, 113).OnTheBall);

        // Eight is all a capsule holds (SealPlacement_NotMaxSeals), and none is taken that the case hasn't
        Assert.Equal(-1, edit.Take(Seal("Q Seal")));
        for (int i = 0; i < 7; i++) Assert.Equal(0, edit.Take(star));
        Assert.True(edit.Full);
        Assert.Equal(-1, edit.Take(star));
        Assert.Equal(2, seals.Count(star));

        // Lifted to the front (SealPlacement_SetSealRenderPriorities)
        Assert.Equal(heart, edit.Seals[7].Seal);
        Assert.Equal(0, edit.Lift(7));
        Assert.Equal(heart, edit.Seals[0].Seal);

        // Given up: the case as it was, the capsule never written (CapsuleManager_CopyFromActiveCapsule, SetSealCounts)
        Assert.True(edit.Changed);
        edit.GiveUp();
        Assert.Equal((2, 9, true), (seals.Count(heart), seals.Count(star), seals.Capsule(0).IsEmpty));

        // Kept: written into the case (SealPlacement_LoadCapsuleSeals)
        var kept = new CapsuleEdit(seals, 0);
        kept.Take(heart);
        kept.Save();
        Assert.Equal((1, 1, 2), (seals.Count(heart), seals.InUse(heart), seals.Anywhere(heart)));
        Assert.False(kept.Changed);
        // Taken off again it goes back among the loose ones (GiveOrTakeSeal2)
        var again = new CapsuleEdit(seals, 0);
        again.Remove(0);
        again.Save();
        Assert.Equal((2, 0), (seals.Count(heart), seals.InUse(heart)));
    }

    [Fact]
    public void ACapsuleGoesOnABallAndComesOffAndTwoChangePlaces()
    {
        var seals = new SealCase();
        var party = new Party();
        var first = Mon();
        var second = Mon("Starly");
        var egg = Mon("Togepi");
        egg.IsEgg = true;
        party.Add(first);
        party.Add(second);
        party.Add(egg);
        seals.Give(Seal("Heart Seal A"), 5);
        var edit = new CapsuleEdit(seals, 2);
        edit.Take(Seal("Heart Seal A"));
        CapsuleRules.Keep(edit, party);

        // An empty capsule goes on nobody's ball, and no capsule on an Egg's
        Assert.False(CapsuleRules.Attach(seals, 0, first));
        Assert.False(CapsuleRules.Attach(seals, 2, egg));
        Assert.True(CapsuleRules.Attach(seals, 2, first));
        // MON_DATA_BALL_CAPSULE_ID is the capsule's number from 1, MON_DATA_BALL_CAPSULE a copy of its Seals
        Assert.Equal((3, seals.Capsule(2)), (first.CapsuleId, first.Capsule));
        Assert.Same(first, CapsuleRules.HolderOf(party, 2));
        Assert.Null(CapsuleRules.HolderOf(party, 0));

        // Changed and kept, the Pokémon whose ball it is on takes the new Seals
        var more = new CapsuleEdit(seals, 2);
        more.Take(Seal("Heart Seal A"));
        Assert.True(CapsuleRules.Keep(more, party));
        Assert.Equal(2, first.Capsule!.Seals.Count);

        // CapsuleGraphics_SwapCapsules: the Seals change places and the Pokémon keeps its capsule under its new number
        CapsuleRules.Swap(seals, party, 2, 7);
        Assert.Equal((8, true, 2), (first.CapsuleId, seals.Capsule(2).IsEmpty, seals.Capsule(7).Seals.Count));
        Assert.Same(first, CapsuleRules.HolderOf(party, 7));

        // Taken off, the capsule stays in the case (Pokemon_ClearBallCapsuleData)
        CapsuleRules.Free(party, 7);
        Assert.Equal((0, null, 2), (first.CapsuleId, first.Capsule, seals.Capsule(7).Seals.Count));
    }

    [Fact]
    public void ASealBurstsBySoFarAsItIsFromTheBallsMiddle()
    {
        int heart = Seal("Heart Seal A"), letter = Seal("K Seal");
        // ov12_02237EA0: (distance + 1) / 20 picks nought, eight or fourteen frames; from fifty-six dots on, twenty
        Assert.Equal(0, CapsuleRules.BurstDelay(new BallSeal(heart, 190, 70)));
        Assert.Equal(0, CapsuleRules.BurstDelay(new BallSeal(heart, 190 + 18, 70)));
        Assert.Equal(8, CapsuleRules.BurstDelay(new BallSeal(heart, 190 + 19, 70)));
        Assert.Equal(8, CapsuleRules.BurstDelay(new BallSeal(heart, 190, 70 - 38)));
        Assert.Equal(14, CapsuleRules.BurstDelay(new BallSeal(heart, 190 - 39, 70)));
        Assert.Equal(14, CapsuleRules.BurstDelay(new BallSeal(heart, 190 + 55, 70)));
        Assert.Equal(20, CapsuleRules.BurstDelay(new BallSeal(heart, 190 + 56, 70)));
        Assert.Equal(20, CapsuleRules.BurstDelay(new BallSeal(heart, 190, 70 + 60)));
        // A letter goes off after eight wherever it is
        Assert.Equal(8, CapsuleRules.BurstDelay(new BallSeal(letter, 190, 70)));
        Assert.Equal(8, CapsuleRules.BurstDelay(new BallSeal(letter, 250, 70)));
        // ov12_022365D4: as far from the Pokémon as from the ball's middle, and thirty dots higher
        Assert.Equal((-40, 50), CapsuleRules.BurstOffset(new BallSeal(heart, 150, 50)));
        Assert.Equal((0, 30), CapsuleRules.BurstOffset(new BallSeal(heart, 190, 70)));
    }

    // ---------------------------------------------------------------- where Seals come from

    [Fact]
    public void TheSealCaseComesWithFiveSealsOfFourKinds()
    {
        // SolaceonTownEastHouse_GiveRandomSeals: 1 + rand 6, 7 + rand 6 twice, 25 + rand 4, 43 + rand 7
        for (int seed = 0; seed < 40; seed++)
        {
            var given = CapsuleRules.FirstSeals(new Random(seed));
            Assert.Equal(5, given.Count);
            Assert.InRange(given[0], Seal("Heart Seal A"), Seal("Heart Seal F"));
            Assert.InRange(given[1], Seal("Star Seal A"), Seal("Star Seal F"));
            Assert.InRange(given[2], Seal("Star Seal A"), Seal("Star Seal F"));
            Assert.InRange(given[3], Seal("Foamy Seal A"), Seal("Foamy Seal D"));
            Assert.InRange(given[4], Seal("Song Seal A"), Seal("Song Seal G"));
        }
    }

    [Fact]
    public void AnUnownGivesTenSealsOfItsOwnShape()
    {
        // Fifty and the shape's number: A is the fiftieth Seal, "!" and "?" the Shock and Mystery Seals
        Assert.Equal("A Seal", SealDatabase.Name(CapsuleRules.UnownSeal(Mon("Unown"))));
        Assert.Equal("R Seal", SealDatabase.Name(CapsuleRules.UnownSeal(Form("Unown-R"))));
        Assert.Equal("Z Seal", SealDatabase.Name(CapsuleRules.UnownSeal(Form("Unown-Z"))));
        Assert.Equal("Shock Seal", SealDatabase.Name(CapsuleRules.UnownSeal(Form("Unown-Exclamation"))));
        Assert.Equal("Mystery Seal", SealDatabase.Name(CapsuleRules.UnownSeal(Form("Unown-Question"))));
        Assert.Equal(0, CapsuleRules.UnownSeal(Mon()));

        // Ten; from ninety on as many as bring the kind to ninety-nine; none once it is there
        var seals = new SealCase();
        int r = Seal("R Seal");
        Assert.Equal(10, CapsuleRules.UnownSeals(seals, r));
        seals.Give(r, 89);
        Assert.Equal(10, CapsuleRules.UnownSeals(seals, r));
        seals.Give(r, 4);
        Assert.Equal(6, CapsuleRules.UnownSeals(seals, r));
        seals.Give(r, 6);
        Assert.Equal(0, CapsuleRules.UnownSeals(seals, r));
    }

    private static HeadlessScriptHost Run(string name, string place, HeadlessScriptHost host)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(name, place)!, null);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    [Fact]
    public void TheMotherGivesTheSealCaseOnceAndHerBoySealsForAnUnown()
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon());
        // Without the case the boy sends the player to his mother
        Run("NinjaBoy", "SolaceonTownEastHouse", host);
        Assert.Equal(0, host.SealCase.Total);
        Run("PokefanF", "SolaceonTownEastHouse", host);
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Seal Case")!));
        Assert.True(host.Story.Has("FLAG_RECEIVED_SOLACEON_TOWN_EAST_HOUSE_SEAL_CASE"));
        Assert.Equal(5, host.SealCase.Total);
        // Spoken to again she only explains
        Run("PokefanF", "SolaceonTownEastHouse", host);
        Assert.Equal((1, 5), (host.Bag.GetQuantity(ItemDatabase.Get("Seal Case")!), host.SealCase.Total));

        // No Unown: nothing. An Unown: ten of its Seal. The same shape again this visit: nothing more
        Run("NinjaBoy", "SolaceonTownEastHouse", host);
        Assert.Equal(5, host.SealCase.Total);
        host.Party.Add(Form("Unown-K"));
        Run("NinjaBoy", "SolaceonTownEastHouse", host);
        Assert.Equal(10, host.SealCase.Count(Seal("K Seal")));
        Run("NinjaBoy", "SolaceonTownEastHouse", host);
        Assert.Equal(10, host.SealCase.Count(Seal("K Seal")));
        // Once the player has been away (the local variable is forgotten), the same shape gives ten more
        host.Story.ClearLocal();
        Run("NinjaBoy", "SolaceonTownEastHouse", host);
        Assert.Equal(20, host.SealCase.Count(Seal("K Seal")));
    }

    [Fact]
    public void TheMerchantOpensHisStallOnlyForSomeoneWithASealCase()
    {
        var host = new HeadlessScriptHost();
        Run("SealShop", "SunyshoreMarket", host);
        Assert.DoesNotContain("open SealShop", host.Log);
        host.Bag.AddItem(ItemDatabase.Get("Seal Case")!, 1);
        Run("SealShop", "SunyshoreMarket", host);
        Assert.Contains("open SealShop", host.Log);
    }

    [Fact]
    public void TheStallSellsWhatTheMoneyAndTheCaseAllow()
    {
        var seals = new SealCase();
        var shop = new SealShopScreen();
        shop.Open(SealDatabase.Market(DayOfWeek.Monday), seals);
        int money = 430;
        var said = new List<string>();
        // Heart Seal A is 50: eight for 430
        Assert.Equal(8, shop.MostOf(shop.Selected, money));
        money += shop.Confirm(money, said.Add);
        Assert.True(shop.ChoosingQuantity);
        shop.Move(1, 0, money);
        shop.Move(1, 0, money);
        money += shop.Confirm(money, said.Add);
        Assert.Equal((280, 3, 3), (money, seals.Count(Seal("Heart Seal A")), shop.Bought));
        // Up from one wraps to the most that can be bought
        shop.Confirm(money, said.Add);
        shop.Move(-1, 0, money);
        Assert.Equal(5, shop.Quantity);
        shop.Cancel();
        // A full kind is not sold, nor anything the money doesn't reach
        seals.Give(Seal("Heart Seal A"), 96);
        Assert.Equal(0, shop.Confirm(money, said.Add));
        Assert.False(shop.ChoosingQuantity);
        for (int i = 0; i < 4; i++) shop.Move(0, 1, money);
        Assert.Equal("Line Seal C", SealDatabase.Name(shop.Selected));
        Assert.Equal(0, shop.Confirm(30, said.Add));
        Assert.False(shop.ChoosingQuantity);
        shop.Cancel();
        Assert.False(shop.IsActive);
    }

    [Fact]
    public void ThePcOffersTheBallCapsulesToWhoeverHasASealCase()
    {
        // No Seal Case: the PC opens its boxes as ever
        var plain = new HeadlessScriptHost();
        Run("PC", "common", plain);
        Assert.Contains("open Pc", plain.Log);
        Assert.Empty(plain.Asked);

        // A case with no Seal in it has nothing to open (CommonScript_NoSeals)
        var empty = new HeadlessScriptHost();
        empty.Bag.AddItem(ItemDatabase.Get("Seal Case")!, 1);
        empty.Answers.Enqueue(1);
        Run("PC", "common", empty);
        Assert.DoesNotContain("open BallCapsules", empty.Log);
        Assert.DoesNotContain("open Pc", empty.Log);

        var host = new HeadlessScriptHost();
        host.Bag.AddItem(ItemDatabase.Get("Seal Case")!, 1);
        host.SealCase.Give(Seal("Heart Seal A"));
        host.Answers.Enqueue(1);
        Run("PC", "common", host);
        Assert.Contains("open BallCapsules", host.Log);
        host.Answers.Enqueue(0);
        Run("PC", "common", host);
        Assert.Contains("open Pc", host.Log);
    }

    // ---------------------------------------------------------------- the capsules' screen

    [Fact]
    public void TheCapsulesScreenChangesKeepsAttachesAndSwaps()
    {
        var seals = new SealCase();
        var party = new Party();
        var lead = Mon();
        party.Add(lead);
        party.Add(Mon("Starly"));
        foreach (string kind in new[] { "Heart Seal A", "Star Seal B", "Song Seal C" }) seals.Give(Seal(kind), 3);
        var screen = new SealCaseScreen();
        screen.Open(seals, party);

        // CapsuleManager_HandleCapsulePadMovement: left and right go round the twelve, up and down stop at the rows' ends
        screen.Move(-1, 0);
        Assert.Equal(11, screen.Cursor);
        screen.Move(1, 0);
        screen.Move(0, -1);
        Assert.Equal(0, screen.Cursor);
        screen.Move(0, 1);
        screen.Move(0, 1);
        screen.Move(0, 1);
        Assert.Equal(8, screen.Cursor);
        screen.Move(0, -1);
        screen.Move(0, -1);

        // An empty capsule: EDIT, SWAP, CANCEL
        screen.Confirm();
        Assert.Equal(new[] { CapsuleAction.Edit, CapsuleAction.Swap, CapsuleAction.Cancel }, screen.Menu);
        screen.Confirm();
        Assert.Equal(CapsuleStep.Seals, screen.Step);
        Assert.Equal(new[] { Seal("Heart Seal A"), Seal("Star Seal B"), Seal("Song Seal C") }, screen.PageKinds);

        // A Seal taken in hand, carried and stuck on
        screen.Confirm();
        Assert.Equal(CapsuleStep.Holding, screen.Step);
        screen.Move(1, 0);
        screen.Move(0, -1);
        Assert.Equal(new BallSeal(Seal("Heart Seal A"), 190 + SealCaseScreen.Nudge, 70 - SealCaseScreen.Nudge), screen.Edit!.Seals[0]);
        screen.Confirm();
        Assert.Equal((CapsuleStep.Seals, 2), (screen.Step, seals.Count(Seal("Heart Seal A"))));
        // Carried off the ball and let go, it is back in the case; it is never carried out of reach
        Assert.True(screen.PointAt(Seal("Star Seal B")));
        screen.Confirm();
        screen.Carry(-500, 0);
        Assert.Equal(190 - SealCaseScreen.Range, screen.Edit.Seals[0].X);
        screen.Confirm();
        Assert.Equal((1, 3), (screen.Edit.Seals.Count, seals.Count(Seal("Star Seal B"))));
        // Picked up again from the ball and put back with the cancel button
        screen.SwitchSide();
        Assert.Equal(CapsuleStep.OnBall, screen.Step);
        screen.Confirm();
        Assert.Equal(CapsuleStep.Holding, screen.Step);
        screen.Cancel();
        Assert.Equal((0, 3), (screen.Edit.Seals.Count, seals.Count(Seal("Heart Seal A"))));

        // Backing out of a changed capsule asks first; NO goes on, YES leaves everything as it was
        screen.Confirm();
        screen.Confirm();
        screen.Cancel();
        Assert.Equal(CapsuleStep.Quit, screen.Step);
        screen.Confirm();
        Assert.Equal(CapsuleStep.Seals, screen.Step);
        screen.Cancel();
        screen.Move(-1, 0);
        screen.Confirm();
        Assert.Equal((CapsuleStep.Capsules, 3, true), (screen.Step, seals.Count(Seal("Heart Seal A")), seals.Capsule(0).IsEmpty));

        // Kept: the capsule has its Seal, and now offers ATTACH first
        screen.Confirm();
        screen.Confirm();
        screen.Confirm();
        screen.Confirm();
        screen.Keep();
        Assert.Equal((CapsuleStep.Capsules, 1, 1), (screen.Step, seals.Capsule(0).Seals.Count, screen.Kept));
        screen.Confirm();
        Assert.Equal(CapsuleAction.Attach, screen.Menu[0]);
        screen.Confirm();
        Assert.Equal(CapsuleStep.Attach, screen.Step);
        screen.Confirm();
        Assert.Equal((1, CapsuleStep.Capsules), (lead.CapsuleId, screen.Step));
        // On a ball it offers DETACH first; swapped, the Pokémon carries the other number
        screen.Confirm();
        Assert.Equal(CapsuleAction.Detach, screen.Menu[0]);
        screen.Choose(CapsuleAction.Swap);
        screen.Move(1, 0);
        screen.Confirm();
        Assert.Equal((2, 1, true), (lead.CapsuleId, screen.Cursor, seals.Capsule(0).IsEmpty));
        screen.Confirm();
        screen.Confirm();
        Assert.Equal((0, null), (lead.CapsuleId, lead.Capsule));
        screen.Cancel();
        Assert.False(screen.IsActive);
    }

    [Fact]
    public void ThePagesOfSealsTurnPastTheirTopAndFoot()
    {
        var seals = new SealCase();
        for (int seal = 1; seal <= 20; seal++) seals.Give(seal);
        var screen = new SealCaseScreen();
        screen.Open(seals, new Party());
        screen.Confirm();
        screen.Confirm();
        Assert.Equal((3, 8), (screen.Pages, screen.PageKinds.Count));
        screen.Move(0, -1);
        Assert.Equal((2, 4, 2), (screen.Page, screen.PageKinds.Count, screen.SealCursor));
        // The last page has two rows: down from its second is the first page again
        screen.Move(0, 1);
        Assert.Equal((0, 0), (screen.Page, screen.SealCursor));
        screen.Move(0, 1);
        screen.Move(0, 1);
        screen.Move(0, 1);
        screen.Move(0, 1);
        Assert.Equal(1, screen.Page);
        Assert.True(screen.PointAt(20));
        Assert.Equal((2, 3, 20), (screen.Page, screen.SealCursor, screen.KindUnderCursor));
        Assert.False(screen.PointAt(21));
    }

    // ---------------------------------------------------------------- in battle

    [Fact]
    public void TheSealsBurstAsThePokemonIsSentOutAndTheRulesKnowNothingOfIt()
    {
        var capsule = new BallCapsule(new[] { new BallSeal(Seal("Heart Seal A"), 190, 70), new BallSeal(Seal("K Seal"), 150, 60) });
        Pokemon Mine(bool withSeals)
        {
            var p = new Pokemon(PokemonDatabase.Get("Turtwig")!, 20, new Random(4));
            if (withSeals)
            {
                p.CapsuleId = 5;
                p.Capsule = capsule;
            }
            return p;
        }
        List<BattleEvent> Play(bool withSeals, out BattleEngine battle)
        {
            var party = new Party();
            party.Add(Mine(withSeals));
            battle = new BattleEngine(new BattleSetup
            {
                PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(),
                WildPokemon = new List<Pokemon> { new(PokemonDatabase.Get("Starly")!, 5, new Random(9)) }, Random = new BattleRandom(77)
            });
            for (int i = 0; i < 40 && battle.HUD.MenuState == BattleMenuState.Message; i++)
            {
                battle.Update(0.5f);
                battle.ConfirmMessage();
            }
            battle.SelectMainMenuOption(0);
            battle.SelectMove(0);
            for (int i = 0; i < 60 && battle.HUD.MenuState == BattleMenuState.Message; i++)
            {
                battle.Update(0.5f);
                battle.ConfirmMessage();
            }
            return battle.Core.Log.ToList();
        }

        var plain = Play(false, out var without);
        var with = Play(true, out var sealedBattle);
        // The same battle, roll for roll: a capsule is the screen's alone
        Assert.Equal(plain.Select(e => e.GetType().Name + (e is Said said ? ":" + said.Text : "")), with.Select(e => e.GetType().Name + (e is Said said ? ":" + said.Text : "")));
        Assert.Equal(without.PlayerPokemon.CurrentHP, sealedBattle.PlayerPokemon.CurrentHP);
        // The rules' copy carries the capsule with it (Pokemon.Clone, CopyStateFrom)
        Assert.Equal(capsule, sealedBattle.Core.Log.SelectMany(e => e is Said line ? line.Shows : new List<BattleEvent> { e }).OfType<Entered>().First(e => e.Place.Side == BattleSide.Player).Pokemon.Capsule);

        // The animator: a cue for the Pokémon sent out with Seals, and none without
        var anim = new BattleAnimator();
        var cue = anim.SealBurst(BattleSide.Player, 0, capsule);
        Assert.Equal((CueKind.Seals, BattleSide.Player, BattleSide.Enemy), (cue.Kind, cue.FromSide, cue.ToSide));
        Assert.Equal(BattleAnimator.SealTime, cue.Length);
        Assert.True(BattleAnimator.SealTime > BattleAnimator.SendOutBallTime + 20 / CapsuleRules.FramesPerSecond);
    }

    [Fact]
    public void TheBurstIsAFunctionOfTheCapsuleItsAgeAndItsSeed()
    {
        var capsule = new BallCapsule(Enumerable.Range(0, 8).Select(i => new BallSeal(1 + i * 9, 150 + i * 10, 40 + i * 8)));
        List<SealMote> At(float age, int seed)
        {
            var motes = new List<SealMote>();
            SealFx.Motes(capsule, age, seed, motes);
            return motes;
        }
        Assert.Equal(At(0.6f, 3), At(0.6f, 3));
        Assert.NotEqual(At(0.6f, 3), At(0.6f, 4));
        Assert.NotEmpty(At(0.3f, 3));
        // Nothing before the ball opens, and nothing left once the last Seal has had its time
        Assert.Empty(At(-0.1f, 3));
        Assert.Empty(At(SealFx.Length + 0.01f, 3));
        // A Seal's pieces begin with its delay: one twenty frames out shows nothing at half a second
        var late = new BallCapsule(new[] { new BallSeal(Seal("Heart Seal A"), 190 + 58, 70) });
        var early = new List<SealMote>();
        SealFx.Motes(late, 0.5f, 1, early);
        Assert.Empty(early);
        SealFx.Motes(late, 20 / CapsuleRules.FramesPerSecond + 0.3f, 1, early);
        Assert.NotEmpty(early);

        // Every Seal has a sign, a colour and pieces of its own kind; a letter Seal spells its letter in dots
        for (int seal = 1; seal <= SealDatabase.Count; seal++)
        {
            var one = new List<SealMote>();
            SealFx.One(seal, default, 0.4f, seal, one);
            Assert.NotEmpty(one);
            Assert.All(one, m => Assert.True(m.Size > 0f && !float.IsNaN(m.At.X) && !float.IsNaN(m.At.Y)));
            Assert.Equal(SealDatabase.IsLetter(seal), SealFx.FamilyOf(seal) == SealFamily.Letter);
        }
        Assert.Equal(('A', 'Z', '!', '?'), (SealFx.LetterOf(50), SealFx.LetterOf(75), SealFx.LetterOf(76), SealFx.LetterOf(77)));
        Assert.All("ABCDEFGHIJKLMNOPQRSTUVWXYZ!?", letter => Assert.Equal(35, SealFx.GlyphOf(letter).Length));
        var k = new List<SealMote>();
        SealFx.One(Seal("K Seal"), default, 0.5f, 1, k);
        Assert.Equal(SealFx.GlyphOf('K').Count(c => c == 'X'), k.Count(m => m.Shape == FxShape.Dot));
        // The fourteen kinds are all there among the eighty
        Assert.Equal(Enum.GetValues<SealFamily>().Length, Enumerable.Range(1, 80).Select(SealFx.FamilyOf).Distinct().Count());

        // In a battle the cue plays nothing before the ball has opened, then the capsule's pieces
        var cue = new BattleAnimator().SealBurst(BattleSide.Player, 0, capsule);
        var places = new FxPlaces();
        places.Set(BattleSide.Player, 0, new System.Numerics.Vector3(0, 0, 2), 1f);
        places.Set(BattleSide.Enemy, 0, new System.Numerics.Vector3(0, 0, -2), 1f);
        var fx = new FxList();
        MoveFx.Play(cue, places, fx);
        Assert.Equal(0, fx.Count);
        typeof(EffectCue).GetProperty(nameof(EffectCue.Age))!.SetValue(cue, BattleAnimator.SendOutBallTime + 0.6f);
        MoveFx.Play(cue, places, fx);
        Assert.Equal(At(0.6f, cue.Seed).Count, fx.Count);
        // A Seal put to the right of the ball's middle bursts to the right of the player's Pokémon as the screen shows it
        var right = new BattleAnimator().SealBurst(BattleSide.Player, 0, new BallCapsule(new[] { new BallSeal(Seal("K Seal"), 190 + 50, 70) }));
        typeof(EffectCue).GetProperty(nameof(EffectCue.Age))!.SetValue(right, BattleAnimator.SendOutBallTime + 0.6f);
        fx.Clear();
        MoveFx.Play(right, places, fx);
        Assert.All(fx.Quads, q => Assert.True(q.Center.X > 0.3f));
    }

    // ---------------------------------------------------------------- saved, copied, and kept out of the boxes

    [Fact]
    public void ACapsuleIsSavedCopiedAndStaysOutOfABox()
    {
        var seals = new SealCase();
        seals.Give(Seal("Heart Seal A"), 7);
        seals.Give(Seal("Ele-Seal B"), 2);
        seals.SetCapsule(4, new BallCapsule(new[] { new BallSeal(Seal("Heart Seal A"), 180, 66), new BallSeal(Seal("Z Seal"), 200, 90) }));
        var p = Mon();
        CapsuleRules.Attach(seals, 4, p);

        var save = new SaveData { SealCase = seals.ToSave(), Party = new List<SavedPokemonData> { SavedPokemonData.FromPokemon(p), SavedPokemonData.FromPokemon(Mon("Starly")) } };
        var read = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(save))!;
        var back = new SealCase();
        back.Load(read.SealCase);
        Assert.Equal((7, 2, seals.Capsule(4)), (back.Count(Seal("Heart Seal A")), back.Count(Seal("Ele-Seal B")), back.Capsule(4)));
        Assert.Equal(new[] { "HEART_SEAL_A,180,66", "Z_SEAL,200,90" }, read.SealCase!.Capsules[4]);
        var loaded = read.Party[0].ToPokemon();
        Assert.Equal((5, seals.Capsule(4)), (loaded.CapsuleId, loaded.Capsule));
        Assert.Equal((0, null), (read.Party[1].ToPokemon().CapsuleId, read.Party[1].ToPokemon().Capsule));
        // A plain ball writes nothing of a capsule
        Assert.DoesNotContain("Capsule", JsonSerializer.Serialize(SavedPokemonData.FromPokemon(Mon("Starly")), new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }));

        // A battle's copy and what is taken back from it
        var clone = p.Clone();
        Assert.Equal((5, p.Capsule), (clone.CapsuleId, clone.Capsule));
        var other = Mon();
        other.CopyStateFrom(p);
        Assert.Equal((5, p.Capsule), (other.CapsuleId, other.Capsule));

        // BoxText_DetachBallCapsule: it goes into a box only with a plain ball
        var party = new Party();
        party.Add(p);
        party.Add(Mon("Starly"));
        var boxes = new PcBoxes();
        Assert.Equal("Please take the Ball Capsule off first.", boxes.WhyNotDeposit(party, 0));
        CapsuleRules.Detach(p);
        Assert.Null(boxes.WhyNotDeposit(party, 0));
    }

    // ---------------------------------------------------------------- ribbons

    [Fact]
    public void TheEightyRibbonsAreTheOriginalsInTheirOrder()
    {
        // generated/ribbons.txt and sRibbonDataTable (src/ribbon.c)
        var all = RibbonDatabase.All;
        Assert.Equal(80, all.Count);
        Assert.Equal(("RIBBON_HOENN_CHAMPION", "RIBBON_SINNOH_CHAMP", "RIBBON_COOL", "RIBBON_PREMIER"), (all[0].Id, all[32].Id, all[33].Id, all[79].Id));
        Assert.Equal(("Champion Ribbon", "Cool Ribbon Great", "Effort Ribbon", "Footprint Ribbon"),
            (RibbonDatabase.Name("RIBBON_HOENN_CHAMPION"), RibbonDatabase.Name("RIBBON_COOL_GREAT"), RibbonDatabase.Name(RibbonRules.Effort), RibbonDatabase.Name(RibbonRules.Footprint)));
        Assert.All(all, r => Assert.False(string.IsNullOrWhiteSpace(r.Name)));
        Assert.All(all, r => Assert.False(string.IsNullOrWhiteSpace(r.Art)));
        // Every ribbon a Super Contest gives is one of them, by the name R17a gave it
        foreach (var quality in Enum.GetValues<ContestStat>())
            foreach (var rank in Enum.GetValues<ContestRank>())
            {
                string ribbon = ContestRules.Ribbon(quality, rank);
                Assert.NotNull(RibbonDatabase.Get(ribbon));
                Assert.Equal((quality, (int)rank), RibbonRules.ContestOf(ribbon));
                Assert.Equal(RibbonKind.Contest, RibbonRules.Kind(ribbon));
            }
        // What each is for: forty for contests, eight for the towers, seven for the days, three bought
        Assert.Equal(20, all.Count(r => RibbonRules.Kind(r.Id) == RibbonKind.Contest));
        Assert.Equal(21, all.Count(r => RibbonRules.Kind(r.Id) == RibbonKind.HoennContest));
        Assert.Equal(8, all.Count(r => RibbonRules.Kind(r.Id) == RibbonKind.Tower));
        Assert.Equal(7, all.Count(r => RibbonRules.Kind(r.Id) == RibbonKind.Daily));
        Assert.Equal(3, all.Count(r => RibbonRules.Kind(r.Id) == RibbonKind.Syndicate));
        Assert.Equal(2, all.Count(r => RibbonRules.Kind(r.Id) == RibbonKind.Champion));
        // The ones the original marks as an event's are all of the kind nobody gives
        Assert.All(all.Where(r => r.Event), r => Assert.Equal(RibbonKind.Event, RibbonRules.Kind(r.Id)));
        // Every one has a line of its own to say what it is for
        Assert.All(all, r => Assert.NotEqual("A ribbon.", RibbonRules.Describe(r.Id)));
        // Those that can be had in this game already, and those that wait
        Assert.Equal(34, all.Count(r => RibbonRules.WaitsFor(r.Id) == null));
        // The Battle Tower gives four of its six (plan 06 · R18a); the two that take another player wait for one
        Assert.All(new[] { "RIBBON_ABILITY", "RIBBON_GREAT_ABILITY", "RIBBON_DOUBLE_ABILITY", "RIBBON_MULTI_ABILITY" }, r => Assert.Null(RibbonRules.WaitsFor(r)));
        Assert.Equal("a friend to battle beside in the Battle Tower", RibbonRules.WaitsFor("RIBBON_PAIR_ABILITY"));
        Assert.Equal("the Battle Tower's battles with the world", RibbonRules.WaitsFor("RIBBON_WORLD_ABILITY"));
        Assert.Equal("the Resort Area's Ribbon Syndicate", RibbonRules.WaitsFor(RibbonRules.Royal));
    }

    [Fact]
    public void TheTeamsRibbonsAreCountedEachKindOnce()
    {
        var party = new Party();
        var a = Mon();
        var b = Mon("Starly");
        var egg = Mon("Togepi");
        egg.IsEgg = true;
        party.Add(egg);
        party.Add(a);
        party.Add(b);
        // GetFirstNonEggInParty
        Assert.Equal(1, RibbonRules.FirstNonEgg(party));
        Assert.True(RibbonRules.Give(a, "RIBBON_COOL"));
        Assert.False(RibbonRules.Give(a, "RIBBON_COOL"));
        Assert.False(RibbonRules.Give(a, "RIBBON_NONE_SUCH"));
        RibbonRules.Give(b, "RIBBON_COOL");
        RibbonRules.Give(b, "RIBBON_ALERT");
        egg.Ribbons.Add("RIBBON_SMILE");
        // ScrCmd_CountPartyRibbons: different ribbons between the team's Pokémon, Eggs left out
        Assert.Equal(2, RibbonRules.CountPartyRibbons(party));
        // The summary lists them in the original's order
        RibbonRules.Give(a, "RIBBON_PREMIER");
        RibbonRules.Give(a, "RIBBON_HOENN_EFFORT");
        Assert.Equal(new[] { "RIBBON_HOENN_EFFORT", "RIBBON_COOL", "RIBBON_PREMIER" }, RibbonRules.Of(a).Select(r => r.Id));
        // Party_GiveChampionRibbons
        RibbonRules.GiveChampion(party);
        Assert.True(RibbonRules.Has(a, RibbonRules.SinnohChamp) && RibbonRules.Has(b, RibbonRules.SinnohChamp));
        Assert.False(egg.Ribbons.Contains(RibbonRules.SinnohChamp));
    }

    [Fact]
    public void EnteringTheHallOfFameGivesTheChampionRibbon()
    {
        var library = ScriptLibrary.FromSources(("test", "script Enter\n  halloffame\n"), ("common", ""));
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon());
        var runner = new ScriptRunner(library, host);
        runner.Start(library.All.First(), null);
        runner.RunToEnd();
        Assert.Contains("halloffame", host.Log);
        Assert.True(RibbonRules.Has(host.Party.Members[0], RibbonRules.SinnohChamp));
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday, "RIBBON_SMILE")]
    [InlineData(DayOfWeek.Monday, "RIBBON_ALERT")]
    [InlineData(DayOfWeek.Tuesday, "RIBBON_SHOCK")]
    [InlineData(DayOfWeek.Wednesday, "RIBBON_DOWNCAST")]
    [InlineData(DayOfWeek.Thursday, "RIBBON_CARELESS")]
    [InlineData(DayOfWeek.Friday, "RIBBON_RELAX")]
    [InlineData(DayOfWeek.Saturday, "RIBBON_SNOOZE")]
    public void JuliaGivesTheDaysRibbonForAStoryOnceADay(DayOfWeek day, string ribbon)
    {
        Assert.Equal(ribbon, RibbonRules.Daily(day));
        var host = new HeadlessScriptHost { Weekday = day };
        var lead = Mon();
        host.Party.Add(lead);
        // The first visit only makes the acquaintance, and she asks to be visited tomorrow
        Run("Julia", "SunyshoreCityNortheastHouse", host);
        Assert.Empty(lead.Ribbons);
        Run("Julia", "SunyshoreCityNortheastHouse", host);
        Assert.Empty(lead.Ribbons);
        // The next day (the daily flags cleared): no story, no ribbon
        host.Story.ClearDaily();
        host.Answers.Enqueue(1);
        Run("Julia", "SunyshoreCityNortheastHouse", host);
        Assert.Empty(lead.Ribbons);
        // A story: the day's ribbon, and no second story that day
        host.Answers.Enqueue(0);
        Run("Julia", "SunyshoreCityNortheastHouse", host);
        Assert.Equal(new[] { ribbon }, lead.Ribbons);
        Assert.Contains(host.Transcript, line => line.Text.Contains(RibbonDatabase.Name(ribbon)));
        int asked = host.Asked.Count;
        Run("Julia", "SunyshoreCityNortheastHouse", host);
        Assert.Equal(asked, host.Asked.Count);
        // On the same day of another week, the Pokémon has the ribbon already
        host.Story.ClearDaily();
        Run("Julia", "SunyshoreCityNortheastHouse", host);
        Assert.Equal(asked, host.Asked.Count);
        Assert.Single(lead.Ribbons);
    }

    [Fact]
    public void TheEffortRibbonIsForEffortValuesFilled()
    {
        var host = new HeadlessScriptHost();
        var lead = Mon();
        host.Party.Add(lead);
        // MAX_EVS_ALL_STATS is 510: 509 is not enough
        lead.EvHP = 252;
        lead.EvAttack = 252;
        lead.EvSpeed = 5;
        Assert.False(RibbonRules.EarnsEffort(lead));
        Run("EffortRibbonWoman", "SunyshoreMarket", host);
        Assert.Empty(lead.Ribbons);
        lead.EvSpeed = 6;
        Assert.Equal(510, RibbonRules.EffortTotal(lead));
        Run("EffortRibbonWoman", "SunyshoreMarket", host);
        Assert.Equal(new[] { RibbonRules.Effort }, lead.Ribbons);
        int lines = host.Transcript.Count;
        Run("EffortRibbonWoman", "SunyshoreMarket", host);
        Assert.Single(lead.Ribbons);
        Assert.True(host.Transcript.Count > lines);
    }

    [Fact]
    public void DrFootstepGivesHisRibbonForAllTheFriendshipThereIs()
    {
        // The script's steps: under 70, from 70, 150, 200, and 255
        var lead = Mon();
        foreach (var (friendship, level) in new[] { (0, 1), (69, 1), (70, 2), (149, 2), (150, 3), (199, 3), (200, 4), (254, 4), (255, 5) })
        {
            lead.Friendship = friendship;
            Assert.Equal(level, RibbonRules.FeelingLevel(lead));
        }
        var host = new HeadlessScriptHost();
        host.Party.Add(lead);
        lead.Friendship = 254;
        Run("DrFootstep", "FootstepHouse", host);
        Assert.Empty(lead.Ribbons);
        Assert.True(host.Story.Has("FLAG_TALKED_TO_DR_FOOTSTEP"));
        lead.Friendship = 255;
        Run("DrFootstep", "FootstepHouse", host);
        Assert.Equal(new[] { RibbonRules.Footprint }, lead.Ribbons);
        Run("DrFootstep", "FootstepHouse", host);
        Assert.Single(lead.Ribbons);
    }

    [Fact]
    public void TheSyndicateLetsInTenRibbonsAndSellsItsThreeInOrder()
    {
        var host = new HeadlessScriptHost();
        var lead = Mon();
        host.Party.Add(lead);
        foreach (var ribbon in RibbonDatabase.All.Take(9)) RibbonRules.Give(lead, ribbon.Id);
        // CountPartyRibbons under ten: shown out
        Run("RibbonSyndicateEntry", "common", host);
        Assert.False(host.Story.Has("FLAG_JOINED_RIBBON_SYNDICATE"));
        RibbonRules.Give(lead, "RIBBON_ALERT");
        Run("RibbonSyndicateEntry", "common", host);
        Assert.True(host.Story.Has("FLAG_JOINED_RIBBON_SYNDICATE"));
        Assert.Equal(1, host.Story.Var("VAR_RESORT_AREA_RIBBON_SYNDICATE_1F_STATE"));

        // The cashier: 10,000, then 100,000, then 999,999, each only with the money and a yes
        Assert.Equal((RibbonRules.Gorgeous, 10_000), RibbonRules.SyndicateOffer(lead));
        host.Money = 9_999;
        host.Answers.Enqueue(0);
        Run("RibbonSyndicateCashier", "common", host);
        Assert.False(RibbonRules.Has(lead, RibbonRules.Gorgeous));
        Assert.Equal(9_999, host.Money);
        host.Money = 1_200_000;
        host.Answers.Enqueue(1);
        Run("RibbonSyndicateCashier", "common", host);
        Assert.False(RibbonRules.Has(lead, RibbonRules.Gorgeous));
        host.Answers.Enqueue(0);
        Run("RibbonSyndicateCashier", "common", host);
        Assert.True(RibbonRules.Has(lead, RibbonRules.Gorgeous));
        Assert.Equal(1_190_000, host.Money);
        Assert.Equal((RibbonRules.Royal, 100_000), RibbonRules.SyndicateOffer(lead));
        host.Answers.Enqueue(0);
        Run("RibbonSyndicateCashier", "common", host);
        Assert.Equal((true, 1_090_000), (RibbonRules.Has(lead, RibbonRules.Royal), host.Money));
        Assert.Equal((RibbonRules.GorgeousRoyal, 999_999), RibbonRules.SyndicateOffer(lead));
        host.Answers.Enqueue(0);
        Run("RibbonSyndicateCashier", "common", host);
        Assert.Equal((true, 90_001), (RibbonRules.Has(lead, RibbonRules.GorgeousRoyal), host.Money));
        Assert.Null(RibbonRules.SyndicateOffer(lead));
        int asked = host.Asked.Count;
        Run("RibbonSyndicateCashier", "common", host);
        Assert.Equal(asked, host.Asked.Count);
    }

    [Fact]
    public void TheSummaryHasAPageOfRibbonsToLookThrough()
    {
        var party = new Party();
        var lead = Mon();
        party.Add(lead);
        var screen = new PartyScreen();
        screen.Open();
        screen.Confirm(party);
        screen.Confirm(party);
        Assert.True(screen.ShowSummary);
        Assert.Equal(new[] { PartyScreen.StatsPage, PartyScreen.MemoPage, PartyScreen.RibbonsPage }, screen.SummaryPages);
        // (The ribbons are the last page: one step back from the first wraps round to them)
        screen.TurnSummaryPage(-1);
        Assert.Equal(PartyScreen.RibbonsPage, screen.SummaryPage);
        // No ribbons: nothing to look through
        Assert.False(screen.SelectRibbons(lead));
        foreach (var ribbon in RibbonDatabase.All.Take(14)) RibbonRules.Give(lead, ribbon.Id);
        Assert.True(screen.SelectRibbons(lead));
        Assert.Equal(0, screen.RibbonCursor);
        screen.MoveRibbon(1, 0, lead.Ribbons.Count);
        screen.MoveRibbon(0, 1, lead.Ribbons.Count);
        Assert.Equal(1 + PartyScreen.RibbonColumns, screen.RibbonCursor);
        screen.LeaveRibbons();
        Assert.Equal(-1, screen.RibbonCursor);
        // With the Contest Hall visited the condition's page stands before the ribbons
        screen.ShowCondition = true;
        Assert.Equal(new[] { PartyScreen.StatsPage, PartyScreen.MemoPage, PartyScreen.ConditionPage, PartyScreen.RibbonsPage }, screen.SummaryPages);
        screen.TurnSummaryPage(1);
        Assert.Equal(PartyScreen.StatsPage, screen.SummaryPage);
        screen.TurnSummaryPage(-1);
        Assert.Equal(PartyScreen.RibbonsPage, screen.SummaryPage);
    }

    // ---------------------------------------------------------------- the Fashion Case and the contests' records

    [Fact]
    public void TheFashionCaseKeepsEachBackdropOnceAndComesWithThree()
    {
        // generated/backdrops.txt: eighteen
        Assert.Equal(18, SuperContestDatabase.Backdrops.Count);
        Assert.Equal((0, 17, -1), (SuperContestDatabase.Backdrop("Dress Up"), SuperContestDatabase.Backdrop("Theater"), SuperContestDatabase.Backdrop("Nowhere")));
        var fashion = new FashionCase();
        Assert.True(fashion.AddBackdrop(4));
        Assert.False(fashion.AddBackdrop(4));
        Assert.False(fashion.AddBackdrop(18));
        Assert.True(fashion.HasBackdrop(4));

        // JubilifeCity_GiveFashionCaseAndAccessories: five fluffs, five feathers or moustaches, Dress Up and two
        // different ones of the eight after it
        for (int seed = 0; seed < 30; seed++)
        {
            var given = new FashionCase();
            given.FillAsGiven(new Random(seed));
            Assert.Equal(10, given.Total);
            Assert.All(given.Held, h => Assert.InRange(h.Accessory, SuperContestDatabase.Accessory("White Fluff"), SuperContestDatabase.Accessory("White Fluff") + 5 + 100));
            Assert.Equal(5, given.Held.Where(h => h.Accessory < SuperContestDatabase.Accessory("White Fluff") + 6).Sum(h => h.Count));
            Assert.Equal(3, given.Backdrops.Count);
            Assert.Equal(0, given.Backdrops[0]);
            Assert.All(given.Backdrops.Skip(1), b => Assert.InRange(b, 1, 8));
        }

        // Saved by their names, in the order they came
        fashion.AddBackdrop(0);
        var save = new SaveData { FashionCase = fashion.ToSave(), Backdrops = fashion.BackdropsToSave() };
        var read = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(save))!;
        Assert.Equal(new[] { "Fiery", "Dress Up" }, read.Backdrops);
        var back = new FashionCase();
        back.Load(read.FashionCase, read.Backdrops);
        Assert.Equal(new[] { 4, 0 }, back.Backdrops);
    }

    [Fact]
    public void JubilifeTvsManGivesTheFashionCaseWithWhatGoesInIt()
    {
        var library = ScriptLibrary.FromSources(("test", "script Give\n  give \"Fashion Case\"\n  setflag FLAG_RECEIVED_FASHION_CASE\n  supercontest fashioncase\n"), ("common", ""));
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(library, host);
        runner.Start(library.All.First(), null);
        runner.RunToEnd();
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Fashion Case")!));
        Assert.Equal((10, 3), (host.FashionCase.Total, host.FashionCase.Backdrops.Count));
        // The game's own scene ends with it
        var scene = ScriptLibrary.Default.Find("TeamGalactic", "jubilife_city")!;
        Assert.Contains(scene.Everything(), i => i.Op == Op.SuperContest && i.Name == "fashioncase");
        Assert.Contains(scene.Everything(), i => i.Op == Op.Give && i.Name == "Fashion Case");
    }

    [Fact]
    public void AContestIsCountedAndItsWinnersPhotographHung()
    {
        var records = new ContestRecords();
        var fashion = new FashionCase();
        var mine = Mon();
        var today = new DateTime(2026, 10, 10);
        // A practice round counts nothing (Contest_EndContest)
        records.Count(ContestAwards.Conclude(mine, ContestStat.Cute, ContestRank.Normal, ContestMode.PracticeDance, 0, fashion), mine, Array.Empty<int>(), "Lucas", today);
        Assert.True(records.IsEmpty);
        // Entered and lost: one entered
        records.Count(ContestAwards.Conclude(mine, ContestStat.Cute, ContestRank.Normal, ContestMode.Official, 2, fashion), mine, Array.Empty<int>(), "Lucas", today);
        Assert.Equal((1, 0, 0), (records.Entered, records.Won, records.Ribbons));
        Assert.Null(records.PhotoOf(ContestStat.Cute));
        // Won: one won, a ribbon the Pokémon hadn't, and its photograph in the Cute Contest's frame
        int fluff = SuperContestDatabase.Accessory("Pink Fluff");
        records.Count(ContestAwards.Conclude(mine, ContestStat.Cute, ContestRank.Normal, ContestMode.Official, 0, fashion), mine, new[] { fluff, fluff }, "Lucas", today);
        Assert.Equal((2, 1, 1), (records.Entered, records.Won, records.Ribbons));
        var photo = records.PhotoOf(ContestStat.Cute)!;
        Assert.Equal(("Turtwig", ContestRank.Normal, "Lucas", today), (photo.Species, photo.Rank, photo.Trainer, photo.Date));
        Assert.Equal(new[] { "Pink Fluff", "Pink Fluff" }, photo.Accessories);
        Assert.Null(records.PhotoOf(ContestStat.Cool));
        // Won again with the ribbon already: no new ribbon, and the newer photograph takes the frame
        records.Count(ContestAwards.Conclude(mine, ContestStat.Cute, ContestRank.Normal, ContestMode.Official, 0, fashion), mine, Array.Empty<int>(), "Lucas", today.AddDays(1));
        Assert.Equal((3, 2, 1), (records.Entered, records.Won, records.Ribbons));
        Assert.Equal(today.AddDays(1), records.PhotoOf(ContestStat.Cute)!.Date);

        var save = new SaveData { ContestRecords = records };
        var read = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(save))!;
        var back = new ContestRecords();
        back.Load(read.ContestRecords);
        Assert.Equal((3, 2, 1, "Turtwig"), (back.Entered, back.Won, back.Ribbons, back.PhotoOf(ContestStat.Cute)!.Species));

        // The Contest Hall's frames: an empty one is only a frame; one with a photograph shows it
        var host = new HeadlessScriptHost();
        Run("FrameCool", "ContestHallLobby", host);
        Assert.DoesNotContain(host.Log, l => l.StartsWith("open ContestPhoto"));
        host.ContestRecords.Load(back);
        Run("FrameCute", "ContestHallLobby", host);
        Assert.Contains("open ContestPhoto", host.Log);
        // A contest held by a script counts as the game's does
        host.Party.Add(Mon());
        host.HoldContest(0, ContestStat.Tough, ContestRank.Normal, ContestMode.Official);
        Assert.Equal((4, 3), (host.ContestRecords.Entered, host.ContestRecords.Won));
        Assert.NotNull(host.ContestRecords.PhotoOf(ContestStat.Tough));
    }

    [Fact]
    public void AMasterRankWinIsTheTrainerCardsStarForContests()
    {
        // TrainerCase_CalculateTrainerCardLevel: one of the five steps is a Master Rank contest won in any quality
        Assert.Equal(0, TrainerCardRules.Level(false, 0, 0, false, 0));
        Assert.Equal(1, TrainerCardRules.Level(false, 0, 0, true, 0));
        Assert.Equal("FLAG_CONTEST_MASTER_SMART", GameEngine.ContestMasterFlag(ContestStat.Smart));
        Assert.Equal(2, TrainerScore.MadeCapsule);
    }

    [Fact]
    public void TheCasesAreOpenedFromTheBag()
    {
        var sealCase = ItemDatabase.Get("Seal Case")!;
        var fashionCase = ItemDatabase.Get("Fashion Case")!;
        Assert.Equal(BagAction.Open, BagScreen.ActionsFor(sealCase)[0]);
        Assert.Equal(BagAction.Open, BagScreen.ActionsFor(fashionCase)[0]);
        Assert.True(BagScreen.UsedInField(sealCase) && BagScreen.UsedInField(fashionCase));
        Assert.Contains(BagAction.Register, BagScreen.ActionsFor(sealCase));

        // The viewer: two tabs, the accessories' grid and the backdrops' list
        var fashion = new FashionCase();
        fashion.FillAsGiven(new Random(2));
        var viewer = new FashionCaseScreen();
        viewer.Open(fashion, new ContestRecords());
        Assert.Equal((0, fashion.Held.Count()), (viewer.Tab, viewer.Count));
        viewer.Move(1, 0);
        Assert.Equal(1, viewer.Cursor);
        viewer.MoveTab(1);
        Assert.Equal((1, 3, 0), (viewer.Tab, viewer.Count, viewer.Cursor));
        viewer.Move(0, 1);
        viewer.Move(0, 1);
        viewer.Move(0, 1);
        Assert.Equal(0, viewer.Cursor);
        viewer.Cancel();
        Assert.False(viewer.IsActive);
        // Opened on a photograph, either button closes it
        viewer.OpenPhoto(new ContestPhoto { Species = "Turtwig", Name = "Turtwig" });
        Assert.NotNull(viewer.Photo);
        viewer.Confirm();
        Assert.False(viewer.IsActive);
    }
}
