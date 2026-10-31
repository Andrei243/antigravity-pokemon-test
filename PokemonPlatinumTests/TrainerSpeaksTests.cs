using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Trainers who speak in battle (plan 11 · C12): the moments the rules mark (<see cref="TrainerReacts"/>), the lines
/// of <c>trainer-lines.json</c>, the face that reads them out and the option that lets it.
/// </summary>
public class TrainerSpeaksTests
{
    // ------------------------------------------------------------------ the moments, on the rules alone

    /// <summary>Every trainer's reaction in a log, in the order it comes, with the event it came with (a line, or itself).</summary>
    private static List<(TrainerReacts Reacts, BattleEvent With, int At)> Reactions(IReadOnlyList<BattleEvent> log)
    {
        var found = new List<(TrainerReacts, BattleEvent, int)>();
        for (int i = 0; i < log.Count; i++)
        {
            if (log[i] is TrainerReacts alone) found.Add((alone, alone, i));
            if (log[i] is not Said said) continue;
            foreach (var r in said.Shows.Concat(said.OnImpact).OfType<TrainerReacts>()) found.Add((r, said, i));
        }
        return found;
    }

    /// <summary>A Pokémon that deals 40 HP whatever it faces, and nothing to chance.</summary>
    private static Pokemon Raging() => Mon("Gible", 50, "Dragon Rage");

    /// <summary>The foe's place is asked for: the Pokémon at this place in the trainer's party comes in.</summary>
    private static void FoeSends(BattleCore core, Place place, int partyIndex)
    {
        Assert.Equal(place, Assert.IsType<ReplacementRequest>(core.Request).Place);
        core.Submit(BattleChoice.Switch(place, partyIndex));
    }

    [Fact]
    public void ATrainerOfThreeReactsToEachMomentOnceAndNeverAfterTheEnd()
    {
        // Two Bidoof of level 5 (20 HP: 2 × 59 × 5 / 100 + 5 + 15) fall to one Dragon Rage each; the third, of
        // level 30, has 75 HP (2 × 59 × 30 / 100 + 5 + 40) and is left at 50, so a Dragon Rage leaves it at 10,
        // a fifth of 75 or under, and the next takes it down
        var core = Against(new[] { Raging() }, new[] { Mon("Bidoof", 5), Mon("Bidoof", 5), Mon("Bidoof", 30) });
        Turn(core);
        FoeSends(core, Foe, 1);
        Turn(core);
        FoeSends(core, Foe, 2);
        var last = core.At(Foe).Pokemon!;
        Assert.Equal(75, last.MaxHP);
        last.CurrentHP = 50;
        Turn(core);
        Assert.Equal(10, last.CurrentHP);
        Turn(core);
        Assert.Equal(BattleResult.PlayerVictory, core.Result);

        var reactions = Reactions(core.Log);
        Assert.Equal(new[] { TrainerCue.FirstDown, TrainerCue.LastPokemon, TrainerCue.LowHp }, reactions.Select(r => r.Reacts.Cue));
        Assert.All(reactions, r => Assert.Equal(Foe, r.Reacts.Place));
        // Each with the line that tells of it: the faint's, the send-out's; the red with the hit's
        Assert.Contains(((Said)reactions[0].With).Shows, e => e is Fainted);
        Assert.Contains(((Said)reactions[1].With).Shows, e => e is Entered);
        Assert.Contains(((Said)reactions[2].With).OnImpact, e => e is Struck { Place: var hit } && hit == Foe);
        // Nothing after the end
        int ended = core.Log.ToList().FindIndex(e => e is Ended);
        Assert.Equal(core.Log.Count - 1, ended);
        Assert.All(reactions, r => Assert.True(r.At < ended));
    }

    [Fact]
    public void ATrainerOfOneHasNoLastPokemonAndNoFirstDown()
    {
        var core = Against(new[] { Raging() }, new[] { Mon("Bidoof", 30) });
        core.At(Foe).Pokemon!.CurrentHP = 50;
        Turn(core);
        Turn(core);
        Assert.Equal(BattleResult.PlayerVictory, core.Result);
        Assert.Equal(new[] { TrainerCue.LowHp }, Reactions(core.Log).Select(r => r.Reacts.Cue));

        // Taken from above the red straight down to nothing, it never stood in the red: no word at all
        var swept = Against(new[] { Raging() }, new[] { Mon("Bidoof", 5) });
        Turn(swept);
        Assert.Equal(BattleResult.PlayerVictory, swept.Result);
        Assert.Empty(Reactions(swept.Log));
    }

    [Fact]
    public void NeitherAWildPokemonNorThePlayersSideReacts()
    {
        var wild = Wild(Raging(), Mon("Bidoof", 30));
        wild.At(Foe).Pokemon!.CurrentHP = 50;
        Turn(wild);
        Turn(wild);
        Assert.Empty(Reactions(wild.Log));

        // The player's own last Pokémon in the red and their first down say nothing either
        var core = Against(new[] { Mon("Bidoof", 5), Mon("Bidoof", 30) }, new[] { Raging() });
        Turn(core);
        core.Submit(BattleChoice.Switch(Mine, 1));
        core.At(Mine).Pokemon!.CurrentHP = 50;
        Turn(core);
        Assert.True(BattleCore.InTheRed(core.At(Mine).Pokemon!.CurrentHP, core.At(Mine).Pokemon!.MaxHP));
        Assert.Empty(Reactions(core.Log));
    }

    [Fact]
    public void ATagBattlesTwoTrainersEachHaveTheirOwnMoments()
    {
        var party = new Party();
        party.Add(Raging());
        party.Add(Raging());
        Trainer Of(string name)
        {
            var t = new Trainer { Id = "twin_" + name.ToLowerInvariant(), Name = name, TrainerClass = "Twins", PrizeMoney = 100 };
            t.Party.Add(Mon("Bidoof", 5));
            t.Party.Add(Mon("Bidoof", 5));
            return t;
        }
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, Trainers = new List<Trainer> { Of("Liv"), Of("Liz") }, Format = BattleFormat.Double, Random = Calm(),
            Rules = Ruleset.Platinum, PlayerController = null, EnemyController = null, PlayerName = "Lucas"
        });
        core.Start();
        DoubleTurn(core, (Mine, 0, Foe), (Mine2, 0, Foe2), (Foe, 0, null), (Foe2, 0, null));
        FoeSends(core, Foe, 1);
        FoeSends(core, Foe2, 1);

        var reactions = Reactions(core.Log);
        foreach (var place in new[] { Foe, Foe2 })
            Assert.Equal(new[] { TrainerCue.FirstDown, TrainerCue.LastPokemon }, reactions.Where(r => r.Reacts.Place == place).Select(r => r.Reacts.Cue));
    }

    [Fact]
    public void ABattleWithReactionsReplaysTheSame()
    {
        // Both sides chosen by the trainer AI from inside, so the seed is the whole battle; the trainer's team is
        // three, so they have moments to react to
        static BattleCore Fresh(uint seed)
        {
            var party = new Party();
            party.Add(Mon("Monferno", 30, "Flame Wheel", "Mach Punch"));
            var trainer = new Trainer { Id = "leader_roark", Name = "Roark", TrainerClass = "Leader", PrizeMoney = 1000 };
            foreach (var p in new[] { Mon("Geodude", 12, "Tackle"), Mon("Onix", 12, "Tackle"), Mon("Cranidos", 14, "Headbutt") }) trainer.Party.Add(p);
            return new BattleCore(new CoreSetup
            {
                PlayerParty = party, Trainers = new List<Trainer> { trainer }, Random = new BattleRandom(seed), Rules = Ruleset.Platinum,
                PlayerController = TrainerAi.Instance, PlayerName = "Lucas"
            });
        }
        var played = Fresh(20261012);
        played.Start();
        Assert.NotEqual(BattleResult.None, played.Result);
        Assert.NotEmpty(Reactions(played.Log));

        var again = Fresh(20261012);
        again.Replay(played.Record!);
        Assert.Equal(Golden.BattleText.Written(played), Golden.BattleText.Written(again));
    }

    [Fact]
    public void TheRulesRedIsTheHpBarsRed()
    {
        Assert.Equal(1f / BattleCore.RedShare, BattleAnimator.LowHpRatio);
        for (int max = 1; max <= 800; max++)
            for (int hp = 0; hp <= max; hp++)
                Assert.True(BattleCore.InTheRed(hp, max) == (hp > 0 && hp <= max * BattleAnimator.LowHpRatio), $"{hp} of {max}");
    }

    // ------------------------------------------------------------------ the face

    /// <summary>A battle against Roark with two Bidoof (20 HP and 75), the option on or off, played to the first choice.</summary>
    private static BattleEngine AgainstRoark(bool lines, Pokemon? mine = null, params Pokemon[] team)
    {
        var party = new Party();
        party.Add(mine ?? Raging());
        var roark = new Trainer { Id = "leader_roark", Name = "Roark", TrainerClass = "Leader", PrizeMoney = 1000 };
        foreach (var p in team.Length > 0 ? team : new[] { Mon("Bidoof", 5, "Growl"), Mon("Bidoof", 30, "Growl") }) roark.Party.Add(p);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(), Trainers = new List<Trainer> { roark },
            Random = Calm(), Rules = Ruleset.Platinum, TrainerLines = lines
        });
        Settle(battle);
        return battle;
    }

    /// <summary>Reads a battle on to its next menu or its end, keeping each message with who says it.</summary>
    private static List<(string Text, string? Speaker)> Read(BattleEngine battle, Action<BattleEngine>? onEach = null)
    {
        var said = new List<(string, string?)>();
        for (int i = 0; i < 400 && !battle.IsBattleOver; i++)
        {
            if (battle.HUD.MenuState is not (BattleMenuState.Message or BattleMenuState.LearnMove)) break;
            if (battle.IsWaitingForConfirm)
            {
                said.Add((battle.CurrentMessage, battle.CurrentSpeaker));
                onEach?.Invoke(battle);
            }
            battle.ConfirmMessage();
            battle.Update(1f / 60f);
        }
        return said;
    }

    [Fact]
    public void WithTheOptionOnTheTrainersLineIsReadAfterTheLineItCameWith()
    {
        var battle = AgainstRoark(lines: true);
        Assert.True(battle.SpeaksTrainerLines);
        battle.SelectMove(0);
        var said = Read(battle);
        string firstDown = TrainerLines.For("leader_roark", TrainerCue.FirstDown)!.Text;
        string lastOne = TrainerLines.For("leader_roark", TrainerCue.LastPokemon)!.Text;
        int fell = said.FindIndex(s => s.Text.EndsWith("Bidoof fainted!"));
        Assert.True(fell >= 0, string.Join(" / ", said.Select(s => s.Text)));
        // Straight after the faint, before the EXP; named on its pill
        Assert.Equal((firstDown, "Roark"), said[fell + 1]);
        int sent = said.FindIndex(s => s.Text == "Leader Roark sent out Bidoof!");
        Assert.Equal((lastOne, "Roark"), said[sent + 1]);
        // The battle's own lines name nobody
        Assert.All(said.Where(s => s.Text != firstDown && s.Text != lastOne), s => Assert.Null(s.Speaker));

        // Into the red: the line once the hit has been told, and the battle isn't over while it waits
        battle.EnemyPokemon.CurrentHP = 50;
        battle.SelectMove(0);
        string red = TrainerLines.For("leader_roark", TrainerCue.LowHp)!.Text;
        said = Read(battle, b =>
        {
            if (b.CurrentSpeaker != null) Assert.False(b.IsBattleOver);
        });
        Assert.Contains((red, "Roark"), said);
        battle.SelectMove(0);
        Read(battle);
        Assert.True(battle.IsBattleOver);
    }

    [Fact]
    public void WithTheOptionOffNothingIsSaid()
    {
        var on = AgainstRoark(lines: true);
        on.SelectMove(0);
        var spoken = Read(on);
        var off = AgainstRoark(lines: false);
        Assert.False(off.SpeaksTrainerLines);
        off.SelectMove(0);
        var quiet = Read(off);
        Assert.All(quiet, s => Assert.Null(s.Speaker));
        // The same battle, less the trainer's two lines
        Assert.Equal(spoken.Where(s => s.Speaker == null).Select(s => s.Text), quiet.Select(s => s.Text));
        Assert.Equal(2, spoken.Count - quiet.Count);
    }

    [Fact]
    public void TheLineIsSaidEvenAsTheBattleEnds()
    {
        // Explosion takes Roark's first Bidoof down and the player's only Pokémon with it: the trainer's line is read
        // after the faint, and the battle is over only once everything after it has been
        var battle = AgainstRoark(lines: true, Mon("Geodude", 50, "Explosion"), Mon("Bidoof", 5, "Growl"), Mon("Bidoof", 5, "Growl"));
        battle.SelectMove(0);
        var said = Read(battle, b =>
        {
            if (b.CurrentSpeaker != null) Assert.False(b.IsBattleOver);
        });
        Assert.Contains((TrainerLines.For("leader_roark", TrainerCue.FirstDown)!.Text, "Roark"), said);
        Assert.True(battle.IsBattleOver);
        Assert.Equal(BattleResult.PlayerDefeat, battle.Result);
    }

    [Fact]
    public void TrainersSpeakUnlessTurnedOff()
    {
        // Every game is played by the modern rules (plan 06 · R19), under which the trainers spoke: the row is on or off
        Assert.Equal(RulesDefault.On, new GameSettings().TrainerLines);
        var settings = new GameSettings();
        OptionsScreen.Change(settings, OptionRow.TrainerLines, 1);
        Assert.Equal(RulesDefault.Off, settings.TrainerLines);
        OptionsScreen.Change(settings, OptionRow.TrainerLines, 1);
        Assert.Equal(RulesDefault.On, settings.TrainerLines);
        settings.TrainerLines = RulesDefault.Rules;
        Assert.Equal(RulesDefault.On, settings.TrainerLines);
        Assert.True(settings.TrainerLines.Holds(RulesPreset.Modern));
    }

    // ------------------------------------------------------------------ the lines

    [Fact]
    public void EveryKeyOfTheLinesIsATrainerOrAStemOfSome()
    {
        var ids = TrainerDatabase.All.Select(t => t.Id).ToList();
        foreach (string key in TrainerLines.Keys)
        {
            if (TrainerLines.IsStem(key))
                Assert.True(ids.Any(id => id.StartsWith(key[..^1], StringComparison.OrdinalIgnoreCase)), $"No trainer's id begins with the stem {key}");
            else
                Assert.True(TrainerDatabase.Get(key) != null, $"No trainer is called {key}");
        }
    }

    [Fact]
    public void EveryLineFitsTwoLinesOfTheBox()
    {
        // The battle's message box wraps at 1634 units of Nunito ExtraBold 40, about seventy characters to a line;
        // measuring needs the font on the graphics card, so a line is held to a hundred characters with the
        // player's and the rival's names at their longest (seven), which two lines always hold
        string longest = new('W', PlayerIdentity.MaxNameLength);
        foreach (var (key, set) in TrainerLines.All)
            foreach (var line in set.Lines)
            {
                string cue = line == set.Giant ? "giant" : line == set.LowHp ? "lowHp" : line == set.FirstDown ? "firstDown" : "lastPokemon";
                string text = PlayerIdentity.Fill(line.Text, longest, PlayerLook.Boy, longest);
                Assert.False(string.IsNullOrWhiteSpace(text), $"{key} {cue}");
                Assert.True(text.Length <= 100, $"{key} {cue}: {text.Length} characters won't fit two lines");
                // The interface's font is Latin-1 only
                Assert.All(text, c => Assert.True(c <= 'ÿ', $"{key} {cue}: '{c}' can't be drawn"));
                Assert.DoesNotContain("{", text);
            }
    }

    [Fact]
    public void TheRivalRoarkAndTheBossesHaveTheirLines()
    {
        // Every battle the rival fights against the player (beside the player at Spear Pillar and in the Fight Area he says nothing)
        foreach (var record in TrainerDatabase.All.Where(t => t.Id.StartsWith("rival_") && !t.Id.Contains("spear_pillar") && !t.Id.Contains("fight_area")))
        {
            Assert.NotNull(TrainerLines.For(record.Id, TrainerCue.LowHp));
            // A team of one has no other moment
            if (record.Party.Count > 1)
                foreach (var cue in Enum.GetValues<TrainerCue>()) Assert.NotNull(TrainerLines.For(record.Id, cue));
        }
        string[] bosses =
        {
            "leader_roark", "leader_gardenia", "leader_maylene", "leader_wake", "leader_fantina", "leader_byron", "leader_candice", "leader_volkner",
            "commander_mars_valley_windworks", "commander_mars_lake_verity", "commander_mars_spear_pillar", "commander_mars_stark_mountain",
            "commander_jupiter_team_galactic_eterna_building", "commander_jupiter_spear_pillar", "commander_jupiter_stark_mountain",
            "commander_saturn_valor_cavern", "commander_saturn_galactic_hq",
            "galactic_boss_cyrus_celestic_town_ruins", "galactic_boss_cyrus_galactic_hq", "galactic_boss_cyrus_distortion_world",
            "elite_four_aaron", "elite_four_bertha", "elite_four_flint", "elite_four_lucian", "champion_cynthia"
        };
        foreach (string id in bosses)
        {
            Assert.NotNull(TrainerDatabase.Get(id));
            foreach (var cue in Enum.GetValues<TrainerCue>()) Assert.True(TrainerLines.For(id, cue) != null, $"{id} has nothing for {cue}");
        }
        // A rematch is a battle of its own, with nothing yet
        Assert.Null(TrainerLines.SetOf("leader_roark_rematch"));
    }

    // ------------------------------------------------------------------ back on the platform (plan 11 · C13)

    [Fact]
    public void ATrainerWhoSpeaksIsBackOnThePlatformWhileTheLineIsReadAndGoneAfter()
    {
        var battle = AgainstRoark(lines: true);
        // The opening played out: Roark has stepped off his platform, as he has by the time anyone chooses a move
        for (int i = 0; i < 90; i++) battle.Update(1f / 60f);
        Assert.Null(battle.Anim.EnemyTrainer);
        battle.SelectMove(0);
        var heard = new List<(PokemonPlatinumEngine.Overworld.Mood Mood, PokemonPlatinumEngine.Overworld.Gesture? Gesture)>();
        var said = Read(battle, b =>
        {
            if (b.CurrentSpeaker == null) return;
            var back = b.Anim.Speaking;
            Assert.NotNull(back);
            Assert.True(back!.LeaveAge < 0f);
            Assert.False(back.Second);
            // The look they stood on the platform in, kept after they stepped off it
            Assert.Null(b.Anim.EnemyTrainer);
            Assert.Equal(b.Anim.EnemyTrainerLook, back.Look);
            heard.Add((back.Mood, back.Gesture));
        });
        Assert.Equal(2, heard.Count);
        // Each with the face and the gesture of their own line
        var firstDown = TrainerLines.For("leader_roark", TrainerCue.FirstDown)!;
        var lastOne = TrainerLines.For("leader_roark", TrainerCue.LastPokemon)!;
        Assert.Equal(lastOne.Face, heard[1].Mood);
        Assert.Equal(lastOne.Gesture, heard[1].Gesture);
        Assert.Equal(firstDown.Face, heard[0].Mood);
        // Once the line is read they step off again, and are gone in the time it takes
        Assert.True(battle.Anim.Speaking == null || battle.Anim.Speaking.LeaveAge >= 0f);
        for (int i = 0; i < 30; i++) battle.Update(1f / 60f);
        Assert.Null(battle.Anim.Speaking);
    }

    [Fact]
    public void WithTheBattleSceneOffTheTrainerSpeaksFromOffThePlatform()
    {
        var battle = AgainstRoark(lines: true);
        battle.Scene = false;
        battle.SelectMove(0);
        var said = Read(battle, b => Assert.Null(b.Anim.Speaking));
        Assert.Contains(said, s => s.Speaker == "Roark");
    }

    [Fact]
    public void ATrainerStepsBackOnlyOnceTheyHaveSteppedOff()
    {
        var anim = new BattleAnimator();
        // Nobody to step back: no look kept
        anim.StepBack(false, PokemonPlatinumEngine.Overworld.Mood.Happy, null);
        Assert.Null(anim.Speaking);
        // Still standing there
        anim.EnemyTrainer = "hiker";
        anim.StepBack(false, PokemonPlatinumEngine.Overworld.Mood.Happy, null);
        Assert.Null(anim.Speaking);
        // Stepped off, and back
        anim.EnemyTrainer = null;
        Assert.Equal("hiker", anim.EnemyTrainerLook);
        anim.StepBack(false, PokemonPlatinumEngine.Overworld.Mood.Angry, PokemonPlatinumEngine.Overworld.Gesture.Point);
        Assert.Equal(0f, anim.Speaking!.Back);
        anim.Update(BattleAnimator.TrainerBackTime / 2f, null, null);
        Assert.Equal(0.5f, anim.Speaking.Back, 3);
        Assert.True(anim.Speaking.GestureAge < 0f);
        anim.Update(BattleAnimator.TrainerBackTime, null, null);
        Assert.Equal(1f, anim.Speaking.Back);
        Assert.True(anim.Speaking.GestureAge > 0f);
        // Sent off, and turned round half way: they walk on from where they were
        anim.SendOffSpeaker();
        anim.Update(BattleAnimator.TrainerBackTime * 0.25f, null, null);
        Assert.Equal(0.75f, anim.Speaking.Back, 3);
        anim.StepBack(false, PokemonPlatinumEngine.Overworld.Mood.Sad, null);
        Assert.Equal(0.75f, anim.Speaking.Back, 3);
        Assert.Equal(PokemonPlatinumEngine.Overworld.Mood.Sad, anim.Speaking.Mood);
        anim.SendOffSpeaker();
        anim.Update(BattleAnimator.TrainerBackTime, null, null);
        Assert.Null(anim.Speaking);
    }

    [Fact]
    public void TheSecondOfTwoTrainersStepsBackAloneToTheirOwnEdge()
    {
        var anim = new BattleAnimator { EnemyTrainer = "twin", EnemyTrainer2 = "twin_b" };
        anim.EnemyTrainer = anim.EnemyTrainer2 = null;
        anim.StepBack(true, PokemonPlatinumEngine.Overworld.Mood.Neutral, null);
        Assert.True(anim.Speaking!.Second);
        Assert.Equal("twin_b", anim.Speaking.Look);
        Assert.True(PokemonPlatinumEngine.Graphics.BattleStage.SpeakerSpot(true).X < 0f);
        Assert.True(PokemonPlatinumEngine.Graphics.BattleStage.SpeakerSpot(false).X > 0f);
    }

    [Fact]
    public void TheCameraFindsTheTrainerWhileTheySpeakAndLetsThemGo()
    {
        var anim = new BattleAnimator { EnemyTrainer = "hiker" };
        anim.EnemyTrainer = null;
        var places = new PokemonPlatinumEngine.Graphics.FxPlaces();
        anim.Update(3f, null, null);
        Assert.Equal(PokemonPlatinumEngine.Graphics.ShotKind.Overview, PokemonPlatinumEngine.Graphics.BattleCamera.Direct(anim, places).Kind);
        anim.StepBack(false, PokemonPlatinumEngine.Overworld.Mood.Happy, null);
        anim.Update(0.1f, null, null);
        var (shot, kind, _) = PokemonPlatinumEngine.Graphics.BattleCamera.Direct(anim, places);
        Assert.Equal(PokemonPlatinumEngine.Graphics.ShotKind.Trainer, kind);
        // A function of the animator's state: the same state, the same shot, framing the platform's edge they come to
        Assert.Equal(shot, PokemonPlatinumEngine.Graphics.BattleCamera.Direct(anim, places).Shot);
        Assert.True(shot.Target.X > PokemonPlatinumEngine.Graphics.BattleStage.EnemySpot.X);
        // While the player chooses, the overview
        Assert.Equal(PokemonPlatinumEngine.Graphics.ShotKind.Overview, PokemonPlatinumEngine.Graphics.BattleCamera.Direct(anim, places, choosing: true).Kind);
        anim.SendOffSpeaker();
        Assert.NotEqual(PokemonPlatinumEngine.Graphics.ShotKind.Trainer, PokemonPlatinumEngine.Graphics.BattleCamera.Direct(anim, places).Kind);
    }

    [Fact]
    public void EveryLinesFaceAndGestureAreNamesOfTheGame()
    {
        foreach (var (key, set) in TrainerLines.All)
            foreach (var line in new[] { set.FirstDown, set.LastPokemon, set.LowHp })
            {
                if (line == null) continue;
                Assert.True(line.Mood == null || Enum.TryParse<PokemonPlatinumEngine.Overworld.Mood>(line.Mood, true, out _), $"{key}: {line.Mood}");
                Assert.True(line.Act == null || line.Gesture != null, $"{key}: {line.Act}");
            }
        // Every boss's line has a face of its own
        Assert.All(TrainerLines.All.SelectMany(e => new[] { e.Value.FirstDown, e.Value.LastPokemon, e.Value.LowHp }).Where(l => l != null), l => Assert.NotNull(l!.Mood));
    }
}
