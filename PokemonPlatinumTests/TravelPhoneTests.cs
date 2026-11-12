using System;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// The travel phone (plan 43 · LF6): it works where its region's file says and nowhere else, the Pokégear and the
/// PokéNav untouched; a number is registered once; every call's script plays headless on every way through; the
/// ringer on a fixed clock; the phone owed to a save past its scene and to none before it; the day's news the same twice.
/// </summary>
public class TravelPhoneTests
{
    [Fact]
    public void ThePhoneWorksWhereItsRegionsFileSaysAndNowhereElse()
    {
        Assert.True(TravelPhone.WorksIn(RegionDatabase.Unova));
        Assert.True(TravelPhone.WorksIn(RegionDatabase.Kalos));
        foreach (var r in new[] { RegionDatabase.Sinnoh, RegionDatabase.Kanto, RegionDatabase.Johto, RegionDatabase.Hoenn })
            Assert.False(TravelPhone.WorksIn(r), r);
        var story = new StoryState();
        Assert.False(TravelPhone.Has(story, RegionDatabase.Unova));
        story.Set(TravelPhone.Flag);
        Assert.True(TravelPhone.Has(story, RegionDatabase.Unova));
        Assert.False(TravelPhone.Has(story, RegionDatabase.Johto));
        // The Pokégear and the PokéNav are their regions' alone, the phone's flag changes neither
        Assert.False(Pokegear.Has(story, RegionDatabase.Unova));
        Assert.False(PokeNav.Has(story, RegionDatabase.Kalos));
        Assert.Equal(PhoneBook.All, PhoneBook.Of(RegionDatabase.Johto));
        Assert.Equal(PhoneBook.Hoenn, PhoneBook.Of(RegionDatabase.Hoenn));
    }

    [Fact]
    public void EachBooksIdsAreItsOwnAndEveryCallHasItsScript()
    {
        var library = ScriptLibrary.Default;
        foreach (var region in new[] { RegionDatabase.Unova, RegionDatabase.Kalos, RegionDatabase.Alola, RegionDatabase.Galar })
        {
            var book = PhoneBook.Of(region);
            Assert.NotEmpty(book);
            foreach (var contact in book)
            {
                Assert.StartsWith(region.ToLowerInvariant() + "_", contact.Id, StringComparison.Ordinal);
                Assert.Same(contact, PhoneBook.Get(contact.Id));
                Assert.NotNull(library.Find(contact.Script, GameEngine.PhoneScripts));
                Assert.DoesNotContain("pokegear.people", contact.Name, StringComparison.Ordinal);
            }
        }
        var ids = PhoneBook.Every.Select(c => c.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void ANumberIsRegisteredOnce()
    {
        var story = new StoryState();
        Assert.True(Pokegear.Register(story, "unova_bianca"));
        Assert.False(Pokegear.Register(story, "unova_bianca"));
        Assert.Equal(new[] { "unova_bianca" }, TravelPhone.Contacts(story, RegionDatabase.Unova).Select(c => c.Id));
        Assert.Empty(TravelPhone.Contacts(story, RegionDatabase.Kalos));
        Assert.Empty(Pokegear.Contacts(story));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(8)]
    public void EveryCallPlaysHeadlessOnEveryWayThrough(int badges)
    {
        var library = ScriptLibrary.Default;
        foreach (var region in new[] { RegionDatabase.Unova, RegionDatabase.Kalos, RegionDatabase.Alola, RegionDatabase.Galar })
        {
            var names = RegionDatabase.Get(region)!.BadgeNames;
            foreach (var contact in PhoneBook.Of(region))
                for (int roll = 0; roll < 2; roll++)
                {
                    var host = new HeadlessScriptHost { Region = region };
                    host.Rolls.Enqueue(roll);
                    foreach (var badge in names.Take(badges)) host.Story.GiveBadge(region, badge);
                    var runner = new ScriptRunner(library, host);
                    runner.Start(library.Find(contact.Script, GameEngine.PhoneScripts)!);
                    runner.RunToEnd();
                    Assert.Empty(host.Problems);
                    Assert.NotEmpty(host.Transcript);
                }
        }
    }

    [Fact]
    public void TheRingerRingsAFriendOnAFixedClock()
    {
        var story = new StoryState();
        foreach (var c in PhoneBook.Of(RegionDatabase.Kalos)) Pokegear.Register(story, c.Id);
        var contacts = TravelPhone.Contacts(story, RegionDatabase.Kalos);
        var ringer = new PhoneRinger();
        Assert.Null(ringer.TryRing(contacts, "kalos_route_1", new Random(1)));
        ringer.Advance(PhoneRinger.WaitSeconds);
        PhoneContact? first = ringer.TryRing(contacts, "kalos_route_1", new Random(3));
        var again = new PhoneRinger();
        again.Advance(PhoneRinger.WaitSeconds);
        Assert.Equal(first?.Id, again.TryRing(contacts, "kalos_route_1", new Random(3))?.Id);
        // Mom and the professor never ring on their own
        for (int seed = 0; seed < 40; seed++)
        {
            var r = new PhoneRinger();
            r.Advance(PhoneRinger.WaitSeconds);
            if (r.TryRing(contacts, "kalos_route_1", new Random(seed)) is { } caller) Assert.True(caller.RingsOnItsOwn, caller.Id);
        }
    }

    [Fact]
    public void ThePhoneIsOwedToASavePastItsSceneAndToNoneBefore()
    {
        var before = new StoryState();
        before.EnterRegion(RegionDatabase.Unova);
        Assert.False(TravelPhone.Owed(before));
        var past = new StoryState();
        past.EnterRegion(RegionDatabase.Unova);
        past.Set(StoryState.PokedexFlag);
        Assert.True(TravelPhone.Owed(past));
        past.Set(TravelPhone.Flag);
        Assert.False(TravelPhone.Owed(past));

        var kalos = new StoryState();
        kalos.EnterRegion(RegionDatabase.Kalos);
        kalos.SetVar(TravelPhone.KalosStoryVar, TravelPhone.KalosGivenAt - 1);
        Assert.False(TravelPhone.Owed(kalos));
        kalos.SetVar(TravelPhone.KalosStoryVar, TravelPhone.KalosGivenAt);
        Assert.True(TravelPhone.Owed(kalos));

        // A Sinnoh game with its Pokédex, never in a region with the phone, is owed nothing
        var sinnoh = new StoryState();
        sinnoh.EnterRegion(RegionDatabase.Sinnoh);
        sinnoh.Set(StoryState.PokedexFlag);
        Assert.False(TravelPhone.Owed(sinnoh));
    }

    [Theory]
    [InlineData("UnovaNewGame", "Unova")]
    [InlineData("KalosNewGame", "Kalos")]
    [InlineData("GalarNewGame", "Galar")]
    public void ANewGameIsOwedNothingBeforeItsScene(string newGame, string region)
    {
        var host = new HeadlessScriptHost { Region = region };
        host.Story.EnterRegion(region);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(newGame, "common")!);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        // Galar has no scene of its own: entering it is enough
        Assert.Equal(region == RegionDatabase.Galar, TravelPhone.Owed(host.Story));
    }

    [Fact]
    public void APhoneGivenInUnovaTakesKalossNumbersOnceKalosIsPastItsScene()
    {
        var story = new StoryState();
        story.EnterRegion(RegionDatabase.Unova);
        story.Set(StoryState.PokedexFlag);
        story.Set(TravelPhone.Flag);
        foreach (var c in PhoneBook.Of(RegionDatabase.Unova)) story.Set(Pokegear.ContactFlag(c.Id));
        Assert.False(TravelPhone.Owed(story));
        Assert.False(TravelPhone.NumbersOwed(story, RegionDatabase.Unova));
        Assert.False(TravelPhone.NumbersOwed(story, RegionDatabase.Kalos));
        story.EnterRegion(RegionDatabase.Kalos);
        story.GiveBadge(RegionDatabase.Kalos, RegionDatabase.Get(RegionDatabase.Kalos)!.BadgeNames[0]);
        Assert.True(TravelPhone.NumbersOwed(story, RegionDatabase.Kalos));
        foreach (var c in PhoneBook.Of(RegionDatabase.Kalos)) story.Set(Pokegear.ContactFlag(c.Id));
        Assert.False(TravelPhone.NumbersOwed(story, RegionDatabase.Kalos));
        Assert.NotEmpty(TravelPhone.Contacts(story, RegionDatabase.Kalos));
    }

    [Fact]
    public void TheScenesThatGiveThePhoneRegisterTheirNumbers()
    {
        foreach (var (file, region) in new[] { ("aspertia_city", RegionDatabase.Unova), ("VanivilleTown_PlayersHouse_1F", RegionDatabase.Kalos) })
        {
            string text = System.IO.File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "Data", "scripts", file + ".txt"));
            Assert.Contains("setflag " + TravelPhone.Flag, text);
            foreach (var contact in PhoneBook.Of(region)) Assert.Contains("addcontact " + contact.Id, text);
        }
    }

    [Fact]
    public void TheDaysNewsIsTheSameTwiceAndTheScreenOpensWithoutAWindow()
    {
        var host = new HeadlessScriptHost();
        var facts = host.Broadcast();
        Assert.Equal(TravelPhone.News(facts, 17), TravelPhone.News(facts, 17));
        Assert.NotEmpty(TravelPhone.News(facts, 17));

        var screen = new PhoneScreen();
        var story = new StoryState();
        foreach (var c in PhoneBook.Of(RegionDatabase.Unova)) Pokegear.Register(story, c.Id);
        screen.Open(TravelPhone.Cards(RegionDatabase.Unova), TravelPhone.Contacts(story, RegionDatabase.Unova));
        Assert.Equal(PhoneCard.Calls, screen.Card);
        screen.Move(0, 1);
        Assert.Equal(1, screen.ContactIndex);
        screen.Move(1, 0);
        Assert.Equal(PhoneCard.News, screen.Card);
        screen.MoveCard(-1);
        screen.Confirm();
        Assert.Equal(PhoneRequest.Call, screen.TakeRequest());
        Assert.Equal(PhoneBook.Of(RegionDatabase.Unova)[1].Id, screen.Called!.Id);
        Assert.False(screen.IsActive);
    }

    [Fact]
    public void PowersIsTheLastCardOnceTheWatchIsHeldAndOpensThePanel()
    {
        Assert.DoesNotContain(PhoneCard.Powers, TravelPhone.Cards(RegionDatabase.Unova));
        var cards = TravelPhone.Cards(RegionDatabase.Unova, powers: true);
        Assert.Equal(PhoneCard.Powers, cards[^1]);
        var screen = new PhoneScreen();
        screen.Open(cards, Array.Empty<PhoneContact>(), PhoneCard.Powers);
        Assert.Equal(PhoneCard.Powers, screen.Card);
        screen.Confirm();
        Assert.Equal(PhoneRequest.Powers, screen.TakeRequest());
        Assert.False(screen.IsActive);
    }

    [Fact]
    public void AContactsOwnRowOfRematchesKeepsJohtosAndHoennsAnswersAndOpensALaterRegionsWithItsHallOfFame()
    {
        var story = new StoryState();
        // Johto's book is always open (its levels are the Badges'); Hoenn's leaders wait for its Hall of Fame
        Assert.All(PhoneBook.All, c => Assert.True(PhoneRematch.RowOpen(c, story), c.Id));
        foreach (var c in PhoneBook.Hoenn.Where(c => !PhoneRematch.IsHoennRouteTrainer(c))) Assert.False(PhoneRematch.RowOpen(c, story), c.Id);
        story.Set(RegionDatabase.Get(RegionDatabase.Hoenn)!.StoryCompleteFlag);
        foreach (var c in PhoneBook.Hoenn.Where(c => !PhoneRematch.IsHoennRouteTrainer(c))) Assert.True(PhoneRematch.RowOpen(c, story), c.Id);
        // A travel phone's contact once its region's Hall of Fame is entered
        var bianca = PhoneBook.Get("unova_bianca")!;
        Assert.False(PhoneRematch.RowOpen(bianca, story));
        story.Set(RegionDatabase.Get(RegionDatabase.Unova)!.StoryCompleteFlag);
        Assert.True(PhoneRematch.RowOpen(bianca, story));
    }

    [Fact]
    public void EveryTravelPhoneContactWithAFaceNamesALookOfTheTable()
    {
        foreach (var region in new[] { RegionDatabase.Unova, RegionDatabase.Kalos, RegionDatabase.Alola, RegionDatabase.Galar })
            foreach (var contact in PhoneBook.Of(region).Where(c => c.Look != null))
                Assert.True(CharacterStyles.Default.Has(contact.Look!), contact.Id + " " + contact.Look);
        // The Pokégear's and the PokéNav's show no face
        Assert.All(PhoneBook.All.Concat(PhoneBook.Hoenn), c => Assert.Null(c.Look));
        // A contact with no look begins no face
        var face = new CallPortrait();
        face.Begin(null);
        Assert.False(face.Active);
        face.Begin("bianca");
        Assert.True(face.Active);
        Assert.Null(face.Picture(false));
        face.End();
        Assert.False(face.Active);
    }
}
