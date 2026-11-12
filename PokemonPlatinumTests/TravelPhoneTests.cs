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
        foreach (var region in new[] { RegionDatabase.Unova, RegionDatabase.Kalos })
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
        var ids = PhoneBook.All.Concat(PhoneBook.Hoenn).Concat(PhoneBook.Of(RegionDatabase.Unova)).Concat(PhoneBook.Of(RegionDatabase.Kalos)).Select(c => c.Id).ToList();
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
        foreach (var region in new[] { RegionDatabase.Unova, RegionDatabase.Kalos })
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
}
