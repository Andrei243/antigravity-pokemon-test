using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The event legendaries of plan 29 · LS11 outside Sinnoh (docs/plans/decisions/LS11-proposals-event-legendaries.md;
/// docs/mechanics/rulings.md, "Celebi and Jirachi"): Kurt's GS Ball laid on Ilex Forest's shrine brings Celebi (LS11b),
/// and the humming stone in Steven's cave wakes Jirachi at night (LS11c). Each scene starts as the game starts it, on
/// maps of this class's own.
/// </summary>
public class EventLegendaryTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private static readonly Lazy<Dictionary<string, World>> Worlds = new(() => World.LoadAll().ToDictionary(w => w.Index.Region));

    private sealed class Game
    {
        private readonly Dictionary<string, Map> maps = new(StringComparer.OrdinalIgnoreCase);
        public StoryState Story { get; } = new();
        public Party Party { get; } = new();
        public Inventory Bag { get; } = new();
        public Map Map { get; private set; } = null!;
        public (int X, int Y) Tile { get; set; }
        public BattleOutcome Fight { get; set; } = BattleOutcome.Won;
        public bool Night { get; set; }
        public HeadlessScriptHost Last { get; private set; } = null!;

        public Game(string region, string map, int x, int y)
        {
            foreach (var m in Worlds.Value[region].BuildMaps()) maps[m.Name] = m;
            StoryMigration.BeginNewGame(Story, Scripts);
            Party.Add(new Pokemon(PokemonDatabase.Get("Swampert")!, 70, new Random(1)));
            Arrive(map, x, y);
        }

        public void Arrive(string name, int x, int y)
        {
            Map = maps[name];
            Map.ForgetForced();
            Map.ApplyPresence(Story.Has);
            Wandering.SendHome(Map);
            Tile = (x, y);
        }

        public NPC? Present(string key) => Map.NPCs.FirstOrDefault(n => n.Key == key);

        public HeadlessScriptHost Talk(string key, params int[] answers)
        {
            var npc = Map.NPCs.First(n => n.Key == key);
            Tile = (npc.GridX, npc.GridY + 1);
            return Play(Scripts.Find(FieldScripts.For(npc)!, npc.ScriptFile ?? Map.ScriptFileAt(npc.GridX, npc.GridY))!, npc, null, answers);
        }

        public HeadlessScriptHost Read(int x, int y, params int[] answers)
        {
            Tile = (x, y + 1);
            string script = Map.SignScripts[(x, y)];
            return Play(Scripts.Find(script, Map.ScriptFileAt(x, y))!, null, new[] { Map.GetSignboardAt(x, y)! }, answers);
        }

        private HeadlessScriptHost Play(Script script, NPC? subject, IReadOnlyList<string>? own, int[] answers)
        {
            var host = new HeadlessScriptHost(Story, Party, Bag)
            {
                Map = Map,
                PlayerTile = Tile,
                MapNamed = name => maps.GetValueOrDefault(name),
                Fight = _ => Fight,
                NeedsPokemon = true,
                TimeOfDay = Night ? TimeOfDay.Night : TimeOfDay.Day,
                Hour = Night ? 23 : 12
            };
            foreach (int answer in answers) host.Answers.Enqueue(answer);
            var runner = new ScriptRunner(Scripts, host);
            runner.Start(script, subject, own);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            Map.ApplyPresence(Story.Has);
            Tile = host.PlayerTile;
            return Last = host;
        }
    }

    [Fact]
    public void KurtHandsAChampionTheGsBallOnce()
    {
        var game = new Game(RegionDatabase.Johto, "Azalea_KurtsHouse", 4, 5);
        game.Story.Set("FLAG_BEAT_AZALEA_ROCKETS");
        // Before the Hall of Fame he only talks of Apricorns
        game.Talk("t23r0501_gantetsu");
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("GS Ball")!));

        game.Story.Set("JohtoHallOfFame");
        game.Talk("t23r0501_gantetsu");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("GS Ball")!));
        Assert.True(game.Story.Has("FLAG_JOHTO_GOT_GS_BALL"));
        game.Talk("t23r0501_gantetsu");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("GS Ball")!));
    }

    [Fact]
    public void TheGsBallOnIlexForestsShrineBringsCelebiUntilItIsMet()
    {
        var game = new Game(RegionDatabase.Johto, "IlexForest", 16, 56);
        Assert.Null(game.Present("ilex_forest_celebi"));
        // Without the ball the shrine's sign is only read
        game.Read(16, 55);
        Assert.DoesNotContain(game.Last.Log, l => l.StartsWith("wildbattle"));

        // Laid there and run from, the ball stays and Celebi comes again at the next reading, with no question
        game.Story.Set("JohtoHallOfFame");
        game.Bag.AddItem(ItemDatabase.Get("GS Ball")!);
        game.Fight = BattleOutcome.Fled;
        game.Read(16, 55, 0);
        Assert.Contains("wildbattle Celebi 50 Fled", game.Last.Log);
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("GS Ball")!));
        Assert.True(game.Story.Has("FLAG_JOHTO_GS_BALL_ON_SHRINE"));
        Assert.Null(game.Present("ilex_forest_celebi"));

        game.Fight = BattleOutcome.Caught;
        game.Read(16, 55);
        Assert.Contains("wildbattle Celebi 50 Caught", game.Last.Log);
        Assert.True(game.Story.Has("FLAG_JOHTO_CELEBI_MET"));
        game.Read(16, 55);
        Assert.DoesNotContain(game.Last.Log, l => l.StartsWith("wildbattle"));
    }

    [Fact]
    public void TheHummingStoneWakesJirachiAtNightOnceCozmoHasHisMeteorite()
    {
        var game = new Game(RegionDatabase.Hoenn, "MeteorFalls_StevensCave", 19, 4);
        game.Story.Set("HoennHallOfFame");
        game.Story.Set("FLAG_DEFEATED_METEOR_FALLS_STEVEN");
        Assert.Null(game.Present("meteor_falls_jirachi"));
        // Without Cozmo's word, or by day, nothing wakes
        game.Night = true;
        game.Talk("meteor_falls_steven");
        Assert.DoesNotContain(game.Last.Log, l => l.StartsWith("wildbattle"));
        game.Story.Set("FLAG_RECEIVED_TM_RETURN");
        game.Night = false;
        game.Talk("meteor_falls_steven");
        Assert.DoesNotContain(game.Last.Log, l => l.StartsWith("wildbattle"));

        // At night it does; run from, it is back the next night
        game.Night = true;
        game.Fight = BattleOutcome.Fled;
        game.Talk("meteor_falls_steven");
        Assert.Contains("wildbattle Jirachi 50 Fled", game.Last.Log);
        Assert.Null(game.Present("meteor_falls_jirachi"));
        game.Arrive("MeteorFalls_StevensCave", 19, 4);
        game.Fight = BattleOutcome.Won;
        game.Talk("meteor_falls_steven");
        Assert.Contains("wildbattle Jirachi 50 Won", game.Last.Log);
        Assert.True(game.Story.Has("FLAG_HOENN_JIRACHI_MET"));
        game.Talk("meteor_falls_steven");
        Assert.DoesNotContain(game.Last.Log, l => l.StartsWith("wildbattle"));
    }

    [Fact]
    public void CelebiAndJirachiAreOnTheirRegionsAreaPages()
    {
        Assert.Contains(Habitats.For("johto")!.Of("Celebi"), h => h.Name == "Ilex Forest" && h.Ways.HasFlag(HabitatWays.Special));
        Assert.Contains(Habitats.For("hoenn")!.Of("Jirachi"), h => h.Name == "Meteor Falls" && h.Ways.HasFlag(HabitatWays.Special));
    }
}
