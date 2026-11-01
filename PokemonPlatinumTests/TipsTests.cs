using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 12 · Q23, the trainer tips: every tip is brought by a trigger in the code or a script and every trigger names a
/// tip, a tip shows once in a game, none shows while a script runs or a battle's log plays, and the row's default shows
/// them by the modern rules and none by Platinum's.
/// </summary>
public class TipsTests
{
    private static readonly Place Mine = new(BattleSide.Player, 0), Foe = new(BattleSide.Enemy, 0);

    // A line of each kind that brings a tip, as BattleEngine.Show meets it
    private static IEnumerable<BattleEvent> LinesThatBringTips()
    {
        var p = new Pokemon(PokemonDatabase.Get("Bidoof")!, 5);
        var spread = p.Stats();
        yield return new Entered(Foe, p, FromBall: false);
        yield return new TrainersStand("Youngster", null);
        yield return new Fainted(Mine);
        yield return new StatusChanged(Foe, StatusCondition.Poison);
        yield return new StatusChanged(Foe, StatusCondition.Toxic);
        yield return new StatusChanged(Foe, StatusCondition.Burn);
        yield return new StatusChanged(Foe, StatusCondition.Paralyze);
        yield return new StatusChanged(Foe, StatusCondition.Sleep);
        yield return new StatusChanged(Foe, StatusCondition.Freeze);
        yield return new StageChanged(Foe, StatType.Attack, -1, false);
        yield return new WeatherChanged(BattleWeather.Rain);
        yield return new SideConditionChanged(BattleSide.Enemy, SideCondition.Spikes, 1);
        yield return new Caught(p, "Poké Ball", false);
        yield return new Caught(p, "Poké Ball", true);
        yield return new LevelRose(p, 6, spread, spread);
        yield return new MoveWanted(p, "Tackle");
    }

    [Fact]
    public void EveryTipIsBroughtByATriggerAndEveryTriggerNamesATip()
    {
        // Each tip has its words in English and in Romanian
        foreach (var tip in Tips.All)
        {
            Assert.NotEqual($"tips.{tip.Id}.title", tip.Title);
            Assert.NotEqual($"tips.{tip.Id}.text", tip.Body);
        }
        Assert.InRange(Tips.All.Count, 24, 36);

        // The battle's lines
        var fromBattle = LinesThatBringTips().Select(Tips.ForBattle).ToList();
        Assert.All(fromBattle, id => Assert.NotNull(id));
        Assert.Equal(Tips.All.Where(t => t.Trigger == TipTrigger.Battle).Select(t => t.Id).OrderBy(s => s),
            fromBattle.Distinct().OrderBy(s => s)!);
        // ...and lines that bring none
        Assert.Null(Tips.ForBattle(new Entered(Mine, new Pokemon(PokemonDatabase.Get("Bidoof")!, 5), FromBall: true)));
        Assert.Null(Tips.ForBattle(new Fainted(Foe)));
        Assert.Null(Tips.ForBattle(new WeatherChanged(BattleWeather.None)));
        Assert.Null(Tips.ForBattle(new SideConditionChanged(BattleSide.Enemy, SideCondition.Spikes, 0)));

        // The scripts' tip commands: each names a tip, and every tip a script brings is named
        var fromScripts = ScriptLibrary.Default.All.SelectMany(s => s.Everything()).Where(i => i.Op == Op.Tip).Select(i => i.Name).ToHashSet();
        Assert.All(fromScripts, id => Assert.NotNull(Tips.Find(id)));
        Assert.Equal(Tips.All.Where(t => t.Trigger == TipTrigger.Script).Select(t => t.Id).OrderBy(s => s), fromScripts.OrderBy(s => s));

        // The field's own: each constant raised somewhere in the engine's code outside Tips.cs
        string engine = Path.Combine(Repository.Root, "PokemonPlatinumEngine");
        string code = string.Join("\n", Directory.EnumerateFiles(engine, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith("Tips.cs", StringComparison.Ordinal)).Select(File.ReadAllText));
        var named = new Dictionary<string, string> { [Tips.Ledge] = "Tips.Ledge", [Tips.Evolution] = "Tips.Evolution", [Tips.TeamFull] = "Tips.TeamFull" };
        Assert.Equal(Tips.All.Where(t => t.Trigger == TipTrigger.Field).Select(t => t.Id).OrderBy(s => s), named.Keys.OrderBy(s => s));
        foreach (var (id, constant) in named) Assert.Contains(constant, code);
    }

    [Fact]
    public void ATipIsRaisedOnceShownOnceAndKept()
    {
        var book = new TipBook();
        Assert.True(book.Raise("poison"));
        Assert.False(book.Raise("poison"));
        Assert.False(book.Raise("no such tip"));
        Assert.True(book.Raise("ledge"));
        Assert.Equal("poison", book.TakeNext()!.Id);
        Assert.False(book.Raise("poison"));
        Assert.Equal("ledge", book.TakeNext()!.Id);
        Assert.Null(book.TakeNext());
        // Kept in the list's order, and read back with a tip that is no longer one passed over
        Assert.Equal(new[] { "poison", "ledge" }, book.ToSave());
        var loaded = new TipBook();
        loaded.Load(new[] { "ledge", "gone", "poison" });
        Assert.Equal(new[] { "poison", "ledge" }, loaded.Seen.Select(t => t.Id));
        Assert.False(loaded.Raise("ledge"));
        Assert.Null(new TipBook().ToSave());
    }

    [Fact]
    public void TheRowShowsTipsByTheModernRulesAndNoneByPlatinums()
    {
        var s = new GameSettings();
        Assert.Equal(RulesDefault.Rules, s.Tips);
        Assert.True(s.Tips.Holds(RulesPreset.Modern));
        Assert.False(s.Tips.Holds(RulesPreset.Platinum));
        OptionsScreen.Change(s, OptionRow.Tips, 1);
        Assert.Equal(RulesDefault.On, s.Tips);
        Assert.True(s.Tips.Holds(RulesPreset.Platinum));
        OptionsScreen.Change(s, OptionRow.Tips, 1);
        Assert.False(s.Tips.Holds(RulesPreset.Modern));
        var (label, _, help) = OptionsScreen.Describe(s, OptionRow.Tips);
        Assert.Equal("Trainer tips", label);
        Assert.StartsWith("No tips are shown", help);
    }

    [Fact]
    public void TheParserKnowsTheTips()
    {
        var library = ScriptLibrary.FromSources(("t", "script T\n  tip cutTree\n  end\n"));
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(library, host);
        runner.Start(library.Find("t.T")!);
        runner.RunToEnd();
        Assert.Equal(new[] { "cutTree" }, host.TipsRaised);
        Assert.ThrowsAny<Exception>(() => ScriptLibrary.FromSources(("t", "script T\n  tip noSuchTip\n  end\n")));
    }

    [Fact]
    public void HelpsTipsPageListsWhatWasShown()
    {
        var help = new HelpScreen();
        var book = new TipBook();
        foreach (var id in new[] { "poison", "nurse", "cutTree" }) { book.Raise(id); book.TakeNext(); }
        help.TipsSeen = book.Seen;
        help.Open();
        help.Move(0, -1);
        help.Move(0, -1);
        for (int i = 0; i < 3; i++) help.Move(1, 0);
        Assert.Equal(HelpPage.Tips, help.Page);
        help.Move(0, 1);
        Assert.False(help.OnTabs);
        help.Move(0, 1);
        help.Move(0, 1);
        help.Move(0, 1);
        Assert.Equal(2, help.TipCursor);
        Assert.Equal("cutTree", help.TipsSeen[help.TipCursor].Id);
        // The page's rows are what its panel holds
        Assert.Equal(HelpScreen.VisibleTipRows, ModernUiRows());
    }

    private static int ModernUiRows() =>
        // The panel's height: from the tabs' foot (132 + 76 + 24) to the content's bottom (1032)
        (int)((1032 - (132 + 76 + 24) - 48 + (92 - 84)) / 92f);
}

/// <summary>Plan 12 · Q23 in a game with no window: the tip held back while a script runs and while a battle's log plays.</summary>
[Collection("Shared clocks and dice")]
public class TipsInAGameTests
{
    private const float Frame = 1f / 60f;

    [Fact]
    public void ATipWaitsForAQuietMomentAndShowsOnce()
    {
        var paths = SavePaths.Current;
        string folder = Path.Combine(Path.GetTempPath(), "tips-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(23);
            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            // A game with no window shows none unless asked
            Assert.False(engine.ShowsTips);
            engine.ShowsTips = true;
            engine.PeopleStayPut = true;
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            game.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 20));
            game.State = GameState.Overworld;
            game.Place(MapDatabase.Get("Sinnoh"), 116, 886, Direction.Down);
            game.TrainersLookOnArrival = false;
            Run(engine, ActionFrame.None);

            // Raised before a script, and one more from the script itself: neither shows while it runs
            engine.RaiseTip(Tips.Ledge);
            var library = ScriptLibrary.FromSources((game.Map.Name, "script Waits\n  tip pc\n  wait 1\n  end\n"));
            engine.StartScript(library.All.Single());
            for (int f = 0; f < 30; f++)
            {
                Run(engine, ActionFrame.None);
                Assert.False(engine.TipShown, "A tip came up while a script ran.");
            }
            Until(engine, () => !engine.ScriptRunning, "the script's end");
            Until(engine, () => engine.TipShown, "the first tip");
            Assert.Equal(Tips.Ledge, game.Flow.ShownTip!.Id);

            // Put away by a press (not before its quarter of a second), and the next comes up after it
            Run(engine, ActionFrame.Press(GameAction.Confirm));
            Assert.True(engine.TipShown, "A press as the tip came up put it away.");
            for (int f = 0; f < 20; f++) Run(engine, ActionFrame.None);
            Run(engine, ActionFrame.Press(GameAction.Confirm));
            Until(engine, () => engine.TipShown, "the script's tip");
            Assert.Equal("pc", game.Flow.ShownTip!.Id);
            for (int f = 0; f < 20; f++) Run(engine, ActionFrame.None);
            Run(engine, ActionFrame.Press(GameAction.Confirm));
            Run(engine, ActionFrame.None);
            Assert.False(engine.TipShown);

            // Raised again, it is not shown again; and the save keeps the two
            engine.RaiseTip(Tips.Ledge);
            for (int f = 0; f < 30; f++) Run(engine, ActionFrame.None);
            Assert.False(engine.TipShown);
            Assert.Equal(new[] { "ledge", "pc" }, game.BuildSave().TipsSeen!.OrderBy(s => s));

            // A battle: a tip comes up only at the main menu, never while its log plays; Toxic brings poison's
            var team = new Party();
            var lead = new Pokemon(PokemonDatabase.Get("Roserade")!, 40, new Random(51));
            lead.Moves.Clear();
            lead.Moves.Add(new Move(MoveDatabase.Get("Toxic")));
            team.Add(lead);
            var battle = new BattleEngine(new BattleSetup
            {
                PlayerParty = team, Inventory = new Inventory(), Pokedex = new Pokedex(),
                WildPokemon = new List<Pokemon> { new(PokemonDatabase.Get("Bibarel")!, 20, new Random(52)) },
                Random = new BattleRandom(15), Rules = Ruleset.Modern
            });
            game.Battle = battle;
            game.State = GameState.Battle;
            var shown = new List<string>();
            bool moved = false;
            for (int f = 0; f < 60 * 60 && !shown.Contains("poison"); f++)
            {
                if (engine.TipShown)
                {
                    Assert.True(battle.AtItsMenu, $"The tip \"{game.Flow.ShownTip!.Id}\" came up while the battle stood at {battle.HUD.MenuState}.");
                    if (!shown.Contains(game.Flow.ShownTip!.Id)) shown.Add(game.Flow.ShownTip.Id);
                }
                else if (battle.AtItsMenu && !moved)
                {
                    battle.SelectMove(0);
                    moved = true;
                }
                Run(engine, f % 20 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
            }
            Assert.Equal(new[] { "wild", "poison" }, shown);
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            PlayerIdentity.InRegion(RegionDatabase.First);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    private static void Run(GameEngine engine, ActionFrame frame)
    {
        InputManager.Feed(frame);
        engine.Update(Frame);
    }

    private static void Until(GameEngine engine, Func<bool> done, string what, int most = 60 * 30)
    {
        for (int f = 0; f < most; f++)
        {
            if (done()) return;
            Run(engine, ActionFrame.None);
        }
        Assert.Fail($"Waited {most / 60} seconds of game time for {what}; the game is in {engine.Drive.State}.");
    }
}
