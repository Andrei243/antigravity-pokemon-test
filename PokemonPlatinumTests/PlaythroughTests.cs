using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Core.Autoplay;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 16 · T13: the end-to-end test. The playthrough bot (plan 16 · T7) plays a new game with no window from the
/// bedroom, on an itinerary of the story's own triggers and people (<see cref="Itineraries"/>), finding its own way
/// between them by the field's rules and playing every scene, question and battle as it comes, to the Coal Badge.
/// Both run with the suite (the whole way to Roark is about twelve minutes of game time and twenty seconds), and fail
/// on an exception, a softlock, a script gone wrong, a value out of range or a leg given up. <c>PLAYTHROUGH_SEED=n</c>
/// plays them on other dice (the nightly's).
/// </summary>
[Collection("Shared clocks and dice")]
public class PlaythroughTests
{
    private readonly ITestOutputHelper output;

    public PlaythroughTests(ITestOutputHelper output) => this.output = output;

    private static int Seed => int.TryParse(Environment.GetEnvironmentVariable("PLAYTHROUGH_SEED"), out int s) ? s : 12;

    [Fact]
    public void TheFirstChapterPlaysThroughFromTheBedroomWithNoWindow()
    {
        Headless.Play(engine =>
        {
            var game = engine.Drive;
            var bot = new Autoplayer(engine, seed: 1) { Itinerary = new Itinerary(Itineraries.FirstChapter) };
            Run(bot, 1800);
            var report = bot.Report;
            Assert.True(bot.Itinerary!.Finished, report.ToMarkdown());
            Assert.Empty(report.LegsGivenUp);
            Assert.True(report.Clean, report.ToMarkdown());
            Assert.True(game.Story.Has(StoryState.PokedexFlag), report.ToMarkdown());
            Assert.True(game.Story.Has(StoryState.RunningShoesFlag), report.ToMarkdown());
            Assert.Equal(5, game.Bag.GetQuantity(ItemDatabase.Get("Poké Ball")!));
        }, Seed);
    }

    [Fact]
    public void TwinleafToCoalBadge()
    {

        Headless.Play(engine =>
        {
            var game = engine.Drive;
            var bot = new Autoplayer(engine, seed: 1) { Itinerary = new Itinerary(Itineraries.TwinleafToCoalBadge) };
            Run(bot, 4 * 3600);
            var report = bot.Report;
            Assert.True(report.Clean, report.ToMarkdown());
            Assert.Empty(report.LegsGivenUp);
            Assert.True(game.Story.HasBadge("Sinnoh", "Coal"), report.ToMarkdown());
        }, Seed);
    }

    [Fact]
    public void KalossLastChaptersPlayThroughFromCoumarineToTheHallOfFame()
    {
        // Plan 23 · Z4: a Kalos game brought to where the Tower of Mastery leaves it (three Badges, in Coumarine City),
        // with a team strong enough for the League, played by the bot through every scene, Gym and battle to the Hall of Fame
        // (the team a few levels higher since Kalos's last places joined Kalos's Gyms and water: the dice the maps draw had
        // moved, and the bot lost to Diantha)
        Headless.Play(engine =>
        {
            var game = engine.Drive;
            var story = game.Story;
            story.ChooseStarter("Froakie");
            story.Set(StoryState.PokedexFlag);
            story.Set(StoryState.RunningShoesFlag);
            foreach (string badge in new[] { "Bug", "Cliff", "Rumble" }) story.GiveBadge(RegionDatabase.Kalos, badge);
            story.SetVar("VAR_KALOS_STORY", 22);
            story.SetVar("VAR_KALOS_LUMIOSE_ROADS", 1);
            foreach (var (species, level) in new[] { ("Greninja", 82), ("Talonflame", 80), ("Venusaur", 80), ("Lucario", 80), ("Garchomp", 80), ("Gardevoir", 80) })
                game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, level));
            game.Bag.AddItem(ItemDatabase.Get("Poké Ball")!, 10);
            // (a Repel that lasts the whole way: the wild Pokémon are all weaker than the lead, and only the story is tried here)
            game.Flow.EncounterAids.RepelSteps = 1_000_000;
            game.Place(MapDatabase.Get("Kalos"), 243, 474, Direction.Down);
            game.State = GameState.Overworld;
            Assert.Equal(MovementDirections.Free, engine.MovementHere.Directions);
            var bot = new Autoplayer(engine, seed: 1) { Itinerary = new Itinerary(Itineraries.KalosLastChapters) };
            Run(bot, 6 * 3600);
            var report = bot.Report;
            Assert.True(report.Clean, report.ToMarkdown());
            Assert.Empty(report.LegsGivenUp);
            Assert.True(story.Has("KalosHallOfFame"), report.ToMarkdown());
            Assert.Equal(8, story.BadgeCount(RegionDatabase.Kalos));
        }, Seed, RegionDatabase.Kalos);
    }

    [Fact]
    public void AKalosGamePlaysWholeFromTheBedroomInVanivilleToTheHallOfFame()
    {
        // Plan 23 · Z4's "done when": the bot plays the opening, the middle chapters and the last ones in one itinerary.
        // Five strong Pokémon are on the team before the first Pokémon arrives (the starter is the sixth), so the Gyms
        // and the League are the story's to be got through and the bot has no training to do
        Headless.Play(engine =>
        {
            var game = engine.Drive;
            var story = game.Story;
            foreach (var (species, level) in new[] { ("Talonflame", 80), ("Venusaur", 80), ("Lucario", 80), ("Garchomp", 80), ("Gardevoir", 80) })
                game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, level));
            game.Bag.AddItem(ItemDatabase.Get("Poké Ball")!, 10);
            game.Flow.EncounterAids.RepelSteps = 1_000_000;
            // Kalos moves freely since plan 30's gate (2026-11-12), so the bot steers a stick the whole way
            Assert.Equal(MovementDirections.Free, engine.MovementHere.Directions);
            var bot = new Autoplayer(engine, seed: 1) { Itinerary = new Itinerary(Itineraries.KalosWhole) };
            Run(bot, 10 * 3600);
            var report = bot.Report;
            Assert.True(report.Clean, report.ToMarkdown());
            Assert.Empty(report.LegsGivenUp);
            Assert.True(bot.Itinerary!.Finished, report.ToMarkdown());
            Assert.True(story.Has("KalosHallOfFame"), report.ToMarkdown());
            Assert.Equal(8, story.BadgeCount(RegionDatabase.Kalos));
        }, Seed, RegionDatabase.Kalos);
    }

    [Fact]
    public void AlolasFirstChapterPlaysFreelyWithAStickFromTheNewHomeToTheMarina()
    {
        // Alola moves freely and grown since the user's switch of 2026-11-12, on its own file: the bot steers a stick
        // from Mom's word to Hau'oli City's marina, playing the professor's door, the trail's bridge, the kahuna's gift,
        // Hau's battle, the Pokédex and Team Skull's grunts as they come
        Headless.Play(engine =>
        {
            var game = engine.Drive;
            Assert.Null(engine.RegionMovementOverride);
            Assert.Equal(MovementDirections.Free, engine.MovementHere.Directions);
            Assert.Equal(CharacterKit.Grown, engine.KitHere);
            // (a strong Pokémon on the team before the first arrives, so the marina's battles are the story's to be got
            // through, not a Rowlet of level 5's: the starter's own battle with Hau is won either way)
            game.Party.Add(new Pokemon(PokemonDatabase.Get("Lycanroc")!, 40));
            output.WriteLine($"begun on {game.Map.Name} at {game.Player.GridX},{game.Player.GridY}, VAR_ALOLA_STORY {game.Story.Var("VAR_ALOLA_STORY")}");
            var bot = new Autoplayer(engine, seed: 1) { Itinerary = new Itinerary(Itineraries.AlolaFirstChapter) };
            Run(bot, 3600);
            output.WriteLine($"ended on {game.Map.Name} at {game.Player.GridX},{game.Player.GridY}, VAR_ALOLA_STORY {game.Story.Var("VAR_ALOLA_STORY")}");
            var report = bot.Report;
            Assert.True(report.Clean, report.ToMarkdown());
            Assert.Empty(report.LegsGivenUp);
            Assert.True(bot.Itinerary!.Finished, report.ToMarkdown());
            Assert.True(game.Story.Has(StoryState.PokedexFlag), report.ToMarkdown());
            Assert.True(game.Story.Var("VAR_ALOLA_STORY") >= 8, $"VAR_ALOLA_STORY {game.Story.Var("VAR_ALOLA_STORY")}\n{report.ToMarkdown()}");
        }, Seed, RegionDatabase.Alola);
    }

    [Fact]
    public void APaldeaGamePlaysFromTheBedroomInCaboPocoToTheAcademysDoor()
    {
        // Plan 34 · PD1's "done when": the bot steers a stick on the open map and walks the rooms, from Mom and the director
        // downstairs through the terrace, the beach, the grotto, the lighthouse and Mesagoza's gate to the academy's welcome
        // (a strong Pokémon on the team before the first arrives, so Nemona's lesson and the students are the story's to be
        // got through, as Alola's run has it; the starter's battle on the terrace is won either way)
        Headless.Play(engine =>
        {
            var game = engine.Drive;
            Assert.Equal(MovementDirections.Free, engine.MovementHere.Directions);
            game.Party.Add(new Pokemon(PokemonDatabase.Get("Pawmot")!, 40));
            game.Flow.EncounterAids.RepelSteps = 1_000_000;
            output.WriteLine($"begun on {game.Map.Name} at {game.Player.GridX},{game.Player.GridY}, VAR_PALDEA_STORY {game.Story.Var("VAR_PALDEA_STORY")}");
            var bot = new Autoplayer(engine, seed: 1) { Itinerary = new Itinerary(Itineraries.PaldeaToTheAcademy) };
            Run(bot, 3600);
            output.WriteLine($"ended on {game.Map.Name}, VAR_PALDEA_STORY {game.Story.Var("VAR_PALDEA_STORY")}");
            var report = bot.Report;
            Assert.True(report.Clean, report.ToMarkdown());
            Assert.Empty(report.LegsGivenUp);
            Assert.True(bot.Itinerary!.Finished, report.ToMarkdown());
            Assert.Equal(7, game.Story.Var("VAR_PALDEA_STORY"));
            Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Tera Orb")!));
        }, Seed, RegionDatabase.Paldea);
    }

    [Fact]
    public void APaldeaGamePlaysFromTheBedroomInCaboPocoToTheThirdBadge()
    {
        // Plan 34 · PD3's second session: on from the academy's door through the first three gyms in the academy's order,
        // Katy's olive roll in her gym's room and Brassius's and Iono's searches on the open map, the team seen to before
        // each leader; strong Pokémon on the team before the first arrives, as Galar's run has it, so the leaders are the
        // story's to be got through
        Headless.Play(engine =>
        {
            var game = engine.Drive;
            foreach (var (species, level) in new[] { ("Pawmot", 60), ("Skeledirge", 60), ("Garganacl", 60) })
                game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, level));
            game.Flow.EncounterAids.RepelSteps = 1_000_000;
            var bot = new Autoplayer(engine, seed: 1) { Itinerary = new Itinerary(Itineraries.PaldeaToTheThirdBadge) };
            Run(bot, 3 * 3600);
            output.WriteLine($"ended on {game.Map.Name}, VAR_PALDEA_STORY {game.Story.Var("VAR_PALDEA_STORY")}, badges {game.Story.BadgeCount(RegionDatabase.Paldea)}");
            var report = bot.Report;
            Assert.True(report.Clean, report.ToMarkdown());
            Assert.Empty(report.LegsGivenUp);
            Assert.True(bot.Itinerary!.Finished, report.ToMarkdown());
            Assert.Equal(3, game.Story.BadgeCount(RegionDatabase.Paldea));
            foreach (string badge in new[] { "Bug", "Grass", "Electric" }) Assert.True(game.Story.HasBadge(RegionDatabase.Paldea, badge), badge);
            // Crossing into the towns opened them to Fly on Paldea's Town Map
            foreach (string town in new[] { "CORTONDO", "ARTAZON", "LEVINCIA" }) Assert.True(game.Story.Has("FLAG_FIRST_ARRIVAL_" + town), town);
        }, Seed, RegionDatabase.Paldea);
    }

    [Fact]
    public void APaldeaGamePlaysFromTheBedroomInCaboPocoToTheFifthBadge()
    {
        // Plan 34 · PD6, owed by its first session: on from the third badge through the West Province's two gyms in the
        // academy's order, Kofu's errand to the auction at Porto Marinada and Larry's secret order at the Treasure Eatery
        Headless.Play(engine =>
        {
            var game = engine.Drive;
            foreach (var (species, level) in new[] { ("Pawmot", 60), ("Skeledirge", 60), ("Garganacl", 60) })
                game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, level));
            game.Flow.EncounterAids.RepelSteps = 1_000_000;
            var bot = new Autoplayer(engine, seed: 1) { Itinerary = new Itinerary(Itineraries.PaldeaToTheFifthBadge) };
            Run(bot, 5 * 3600);
            output.WriteLine($"ended on {game.Map.Name}, badges {game.Story.BadgeCount(RegionDatabase.Paldea)}");
            var report = bot.Report;
            Assert.True(report.Clean, report.ToMarkdown());
            Assert.Empty(report.LegsGivenUp);
            Assert.True(bot.Itinerary!.Finished, report.ToMarkdown());
            Assert.Equal(5, game.Story.BadgeCount(RegionDatabase.Paldea));
            foreach (string badge in new[] { "Bug", "Grass", "Electric", "Water", "Normal" }) Assert.True(game.Story.HasBadge(RegionDatabase.Paldea, badge), badge);
        }, Seed, RegionDatabase.Paldea);
    }

    [Fact]
    public void APaldeaGameWinsTheLastThreeBadgesFromMedali()
    {
        // Plan 34 · PD6, its second session: a game with the first five badges put down on Medali's square, the bot
        // walking to Montenevera for Ryme's rap battle, through Alfornada Cavern for Tulip's photographs, and up Glaseado
        // for Grusha's run, down through its gates and back up to the gym; a strong team on hand, as the gyms' runs have it
        Headless.Play(engine =>
        {
            var game = engine.Drive;
            foreach (var (species, level) in new[] { ("Pawmot", 75), ("Skeledirge", 75), ("Garganacl", 75) })
                game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, level));
            game.Flow.EncounterAids.RepelSteps = 1_000_000;
            game.Story.SetVar("VAR_PALDEA_STORY", 7);
            foreach (string badge in new[] { "Bug", "Grass", "Electric", "Water", "Normal" }) game.Story.GiveBadge(RegionDatabase.Paldea, badge);
            game.PlaceOpen(engine.OpenMapNamed("Paldea")!, "medali_square");
            var bot = new Autoplayer(engine, seed: 1) { Itinerary = new Itinerary(Itineraries.PaldeaNorthBadges) };
            Run(bot, 5 * 3600);
            output.WriteLine($"ended on {game.Map.Name}, badges {game.Story.BadgeCount(RegionDatabase.Paldea)}");
            var report = bot.Report;
            Assert.True(report.Clean, report.ToMarkdown());
            Assert.Empty(report.LegsGivenUp);
            Assert.True(bot.Itinerary!.Finished, report.ToMarkdown());
            Assert.Equal(8, game.Story.BadgeCount(RegionDatabase.Paldea));
            // Crossing into the new towns opened them to Fly on Paldea's Town Map
            foreach (string town in new[] { "MONTENEVERA", "ALFORNADA", "GLASEADO_MOUNTAIN" }) Assert.True(game.Story.Has("FLAG_FIRST_ARRIVAL_" + town), town);
        }, Seed, RegionDatabase.Paldea);
    }

    [Fact]
    public void APaldeaGameIsRiddenFromCaboPocoToGlaseadosTop()
    {
        // Plan 34 · PD6's done-when, headless: the region crossed on the mount from Cabo Poco's lane to the top of Glaseado
        // Mountain by the bot, mounted again whenever a battle or a door put it down (the harness's `paldea` mode times the
        // same ride drawn, the chunks streamed as it goes, `pdn90`)
        Headless.Play(engine =>
        {
            var game = engine.Drive;
            foreach (var (species, level) in new[] { ("Pawmot", 75), ("Skeledirge", 75), ("Garganacl", 75) })
                game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, level));
            game.Flow.EncounterAids.RepelSteps = 1_000_000;
            game.Story.SetVar("VAR_PALDEA_STORY", 7);
            game.Story.SetVar(PokemonPlatinumEngine.OpenWorld.MountRules.MountVariable, PokemonPlatinumEngine.OpenWorld.MountRules.Koraidon);
            game.Story.Set("FLAG_PALDEA_MOUNT_DASH");
            game.PlaceOpen(engine.OpenMapNamed("Paldea")!, "cabo_poco_lane");
            var bot = new Autoplayer(engine, seed: 1) { Itinerary = new Itinerary(Itineraries.PaldeaToGlaseadosTop) };
            const float dt = 1f / 60f;
            int ridden = 0, frames = 0;
            for (double t = 0; t < 2 * 3600 && !bot.Stopped && !bot.Itinerary.Finished; t += dt)
            {
                if (engine.InOpenField && game.State == PokemonPlatinumEngine.Core.GameState.Overworld && !game.OpenMounted && !engine.ScriptRunning
                    && game.WhyNotMount == PokemonPlatinumEngine.OpenWorld.MountRefusal.None)
                    game.ToggleMount();
                bot.Frame(dt);
                frames++;
                if (game.OpenMounted) ridden++;
            }
            bot.Release();
            output.WriteLine($"{bot.Report.Seconds:0} s of game time, ridden {100.0 * ridden / Math.Max(1, frames):0}% of the frames, ended at {game.OpenPlayer?.Pos}");
            output.WriteLine(bot.Report.ToMarkdown());
            var report = bot.Report;
            Assert.True(report.Clean, report.ToMarkdown());
            Assert.Empty(report.LegsGivenUp);
            Assert.True(bot.Itinerary!.Finished, report.ToMarkdown());
            Assert.Equal("glaseado_mountain", engine.OpenMapNamed("Paldea")!.AreaAt(game.OpenPlayer!.Pos)?.Key);
            Assert.True(game.OpenPlayer.Pos.MetresY > 45, $"ended at {game.OpenPlayer.Pos}");
            Assert.True(ridden > frames / 2, $"ridden {ridden} of {frames} frames");
        }, Seed, RegionDatabase.Paldea);
    }

    [Fact]
    public void APaldeaGameRunsFromTheLeagueThroughAreaZeroToTheWayHome()
    {
        // Plan 34 · PD8's done-when, its last leg (the legs before it are the runs above: the bedroom to the academy's
        // door, the badges, the Path of Legends, the ride to Glaseado's top): a game with the eight badges, the five
        // Titans and the five bases behind it put down before the League's door; the bot through Rika's interview, the
        // Elite Four, Geeta and Nemona to the Hall of Fame and home to Cabo Poco, then riding north across the region to
        // the Zero Gate (mounted again whenever a battle or a door put it down), down Area Zero past its four stations to
        // the Zero Lab, the professor's AI, the legendary's battle form beside the player, and the Way Home
        Headless.Play(engine =>
        {
            var game = engine.Drive;
            foreach (var (species, level) in new[] { ("Pawmot", 100), ("Skeledirge", 100), ("Garganacl", 100), ("Meowscarada", 100), ("Kingambit", 100) })
                game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, level));
            game.Flow.EncounterAids.RepelSteps = 1_000_000;
            game.Story.SetVar("VAR_PALDEA_STORY", 7);
            game.Story.SetVar("VAR_PALDEA_VERSION", 1);
            game.Story.SetVar(PokemonPlatinumEngine.OpenWorld.MountRules.MountVariable, PokemonPlatinumEngine.OpenWorld.MountRules.Koraidon);
            game.Story.Set("FLAG_PALDEA_MOUNT_DASH");
            game.Story.SetVar("VAR_PALDEA_LEGENDS", Itineraries.PaldeaLegendsEnd);
            game.Story.SetVar("VAR_PALDEA_STARFALL", 6);
            foreach (string badge in new[] { "Bug", "Grass", "Electric", "Water", "Normal", "Ghost", "Psychic", "Ice" }) game.Story.GiveBadge(RegionDatabase.Paldea, badge);
            game.PlaceOpen(engine.OpenMapNamed("Paldea")!, "league_front");
            var bot = new Autoplayer(engine, seed: 1) { Itinerary = new Itinerary(Itineraries.PaldeaLeagueToTheWayHome) };
            const float dt = 1f / 60f;
            int ridden = 0, open = 0;
            var started = DateTime.UtcNow;
            for (double t = 0; t < 3 * 3600 && !bot.Stopped; t += dt)
            {
                if (engine.InOpenField && game.State == PokemonPlatinumEngine.Core.GameState.Overworld && !game.OpenMounted && !engine.ScriptRunning
                    && game.WhyNotMount == PokemonPlatinumEngine.OpenWorld.MountRefusal.None)
                    game.ToggleMount();
                bot.Frame(dt);
                if (engine.InOpenField) { open++; if (game.OpenMounted) ridden++; }
                if (bot.Itinerary!.Finished && game.State == PokemonPlatinumEngine.Core.GameState.Overworld) break;
            }
            bot.Release();
            output.WriteLine($"{bot.Report.Seconds:0} s of game time in {(DateTime.UtcNow - started).TotalSeconds:0} s; ridden {100.0 * ridden / Math.Max(1, open):0}% of the open map's frames; ended on {game.Map.Name}; VAR_PALDEA_ZERO {game.Story.Var("VAR_PALDEA_ZERO")}, WAY_HOME {game.Story.Has("FLAG_PALDEA_WAY_HOME")}, LEAGUE {game.Story.Var("VAR_PALDEA_LEAGUE")}");
            output.WriteLine(bot.Report.ToMarkdown());
            var report = bot.Report;
            Assert.True(report.Clean, report.ToMarkdown());
            Assert.Empty(report.LegsGivenUp);
            Assert.True(bot.Itinerary!.Finished, report.ToMarkdown());
            Assert.True(game.Story.Has("PaldeaHallOfFame"));
            Assert.True(game.Story.Has("FLAG_PALDEA_WAY_HOME"));
            Assert.Equal(8, game.Story.Var("VAR_PALDEA_ZERO"));
            Assert.Contains(game.Party.Members, p => p.Species.Name == "Koraidon");
            Assert.True(ridden > open / 2, $"ridden {ridden} of {open} open frames");
        }, Seed, RegionDatabase.Paldea);
    }

    [Fact]
    public void APaldeaGameWalksThePathOfLegendsFromArvensWordToHisBattle()
    {
        // Plan 34 · PD4: a game past the academy's welcome put down at the lighthouse's foot, the bot walking the Path of
        // Legends on foot: Arven spoken to, each Titan met by its place's trigger, chased to its den and beaten there, and
        // Arven's battle at the lighthouse; a strong team on hand, as the gyms' run has it
        Headless.Play(engine =>
        {
            var game = engine.Drive;
            foreach (var (species, level) in new[] { ("Pawmot", 70), ("Skeledirge", 70), ("Garganacl", 70) })
                game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, level));
            game.Flow.EncounterAids.RepelSteps = 1_000_000;
            game.Story.SetVar("VAR_PALDEA_STORY", 7);
            game.Story.Unset("FLAG_PALDEA_HIDE_ARVEN_LIGHTHOUSE");
            game.Story.SetVar("VAR_PALDEA_MOUNT", 1);
            game.PlaceOpen(engine.OpenMapNamed("Paldea")!, "lighthouse_foot");
            var bot = new Autoplayer(engine, seed: 1) { Itinerary = new Itinerary(Itineraries.PaldeaPathOfLegends) };
            Run(bot, 2 * 3600);
            output.WriteLine($"ended at VAR_PALDEA_LEGENDS {game.Story.Var("VAR_PALDEA_LEGENDS")}");
            var report = bot.Report;
            Assert.True(report.Clean, report.ToMarkdown());
            Assert.Empty(report.LegsGivenUp);
            Assert.True(bot.Itinerary!.Finished, report.ToMarkdown());
            Assert.Equal(Itineraries.PaldeaLegendsEnd, game.Story.Var("VAR_PALDEA_LEGENDS"));
            Assert.Equal(PokemonPlatinumEngine.OpenWorld.MovementGrants.Dash | PokemonPlatinumEngine.OpenWorld.MovementGrants.Swim | PokemonPlatinumEngine.OpenWorld.MovementGrants.HighJump
                | PokemonPlatinumEngine.OpenWorld.MovementGrants.Glide | PokemonPlatinumEngine.OpenWorld.MovementGrants.Climb, PokemonPlatinumEngine.OpenWorld.MountRules.Grants(game.Story));
        }, Seed, RegionDatabase.Paldea);
    }

    [Fact]
    public void TheWayIsFoundAcrossMapsAndAlongAMapWithNoBound()
    {
        // Across maps, by their warps: down the stairs and out of the house; into the mine and down a floor
        Assert.Equal("PlayerHouse", Itinerary.NextMap("PlayerHouse2F", "Sinnoh", MapDatabase.Get));
        Assert.Equal("OreburghMineB1F", Itinerary.NextMap("Sinnoh", "OreburghMineB2F", MapDatabase.Get));
        Assert.Null(Itinerary.NextMap("Sinnoh", "Sinnoh", MapDatabase.Get));

        // Along a map, further than the bot's own goals look: Twinleaf Town to Sandgem Town's Pokémon Center door,
        // every step one the field takes, ending on the door
        var sinnoh = MapDatabase.Get("Sinnoh");
        var door = sinnoh.Warps.First(w => w.TargetMap == "PokemonCenter");
        var steps = Policy.Route(sinnoh, 116, 886, new Walker(), (x, y) => (x, y) == (door.SourceX, door.SourceY), door.SourceX, door.SourceY);
        Assert.NotNull(steps);
        Assert.True(steps!.Count > Policy.Reach, $"{steps.Count} steps");
        int px = 116, py = 886;
        float h = sinnoh.HeightAt(px, py);
        foreach (var way in steps)
        {
            var step = FieldMovement.Step(sinnoh, px, py, way, new Walker() with { Height = h });
            Assert.True(step.Moves, $"blocked at {px},{py} going {way}: {step.Obstacle}");
            (px, py, h) = (step.X, step.Y, step.Height);
        }
        Assert.Equal((door.SourceX, door.SourceY), (px, py));

        // An itinerary goes leg by leg, keeping what it did and what it gave up
        var it = new Itinerary(new[] { Leg.Enter("PlayerHouse"), Leg.Reach("Sinnoh", 1, 1) });
        Assert.Equal(LegKind.Enter, it.Current!.Kind);
        it.Advance();
        it.Advance(givenUp: true);
        Assert.True(it.Finished);
        Assert.Equal(new[] { "Enter PlayerHouse" }, it.Done);
        Assert.Equal(new[] { "Reach Sinnoh 1,1" }, it.GivenUp);
    }

    // Plays until the itinerary is done (and the field is the player's again), the run stops or the time is up
    private void Run(Autoplayer bot, double seconds)
    {
        var started = DateTime.UtcNow;
        const float dt = 1f / 60f;
        for (double t = 0; t < seconds && !bot.Stopped; t += dt)
        {
            bot.Frame(dt);
            if (bot.Itinerary!.Finished && t > 1) break;
        }
        bot.Release();
        output.WriteLine($"{bot.Report.Seconds:0} s of game time in {(DateTime.UtcNow - started).TotalSeconds:0} s");
        output.WriteLine("team: " + string.Join(", ", bot.Team));
        output.WriteLine(bot.Report.ToMarkdown());
    }
}

/// <summary>A game with no window for the playthrough tests (plan 16 · T7, T13), begun in Sinnoh's bedroom in a save folder of its own.</summary>
internal static class Headless
{
    public static void Play(Action<GameEngine> play, int dice = 12, string region = "Sinnoh")
    {
        string folder = Path.Combine(Path.GetTempPath(), "playthrough-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(dice);
            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            engine.NewGameRegion = region;
            engine.StartNewGame();
            engine.Drive.State = GameState.Overworld;
            play(engine);
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            PlayerIdentity.InRegion(RegionDatabase.First);
            PlayerIdentity.SetRival(null);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    /// <summary>Past the first chapter as the harness takes it, with a lead strong enough for any grass of the open world's south.</summary>
    public static void PastTheOpening(GameEngine engine, int level = 30)
    {
        var game = engine.Drive;
        StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, level));
        game.Bag.AddItem(ItemDatabase.Get("Poké Ball")!, 10);
        game.Place(MapDatabase.Get("Sinnoh"), 116, 886, Direction.Down);
        game.TrainersLookOnArrival = false;
        game.State = GameState.Overworld;
    }
}
