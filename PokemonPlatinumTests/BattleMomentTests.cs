using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// The turn's moments (plan 25 · Y3): each of the original's battle animations that is not a move rides on the line
/// that tells of it, the screen plays it before the line's HP and lets the HP land quietly, and every look is a pure
/// function of its cue's age and seed that draws and stops.
/// </summary>
public class BattleMomentTests
{
    public static IEnumerable<object[]> Moments => Enum.GetValues<BattleMoment>().Select(m => new object[] { m });

    // ------------------------------------------------------------------ the original's list is the checklist

    /// <summary>
    /// The original's battle animations that are not moves, in its order (<c>generated/battle_sub_animations.txt</c>
    /// at the pinned commit: the names alone), and what shows each here.
    /// </summary>
    private static readonly (string Original, string Shown)[] Checklist =
    {
        ("ASLEEP", "moment"), ("POISONED", "moment"), ("BURNED", "moment"), ("FROZEN", "moment"), ("PARALYZED", "moment"),
        ("CONFUSED", "moment"), ("INFATUATED", "moment"), ("LEVEL_UP", "moment"), ("BAG_ITEM", "moment"), ("HELD_ITEM", "moment"),
        ("SHINY", "CueKind.Shiny"), ("STAT_BOOST", "CueKind.StatUp"), ("STAT_DROP", "CueKind.StatDown"), ("RESTORE_HP", "CueKind.Heal"),
        ("SUB_OUT", "the doll"), ("SUB_IN", "the doll"), ("ITEM_ESCAPE", "moment"),
        ("WEATHER_FOG", "CueKind.Weather"), ("WEATHER_RAIN", "CueKind.Weather"), ("WEATHER_HAIL", "CueKind.Weather"),
        ("WEATHER_SAND", "CueKind.Weather"), ("WEATHER_SUN", "CueKind.Weather"),
        ("UNUSED_23", "unused"), ("UNUSED_24", "unused"), ("SUBSTITUTE_IN", "the doll"), ("SUBSTITUTE_OUT", "the doll"),
        ("HAPPY", "moment"), ("EATING", "moment"), ("ANGRY", "moment"),
        ("DAMAGE_CURSE", "moment"), ("DAMAGE_NIGHTMARE", "moment"), ("DAMAGE_LEECH_SEED", "moment"), ("DAMAGE_BIND", "moment"),
        ("DAMAGE_WRAP", "moment"), ("DAMAGE_FIRE_SPIN", "moment"), ("DAMAGE_MAGMA_STORM", "moment"), ("DAMAGE_CLAMP", "moment"),
        ("DAMAGE_WHIRLPOOL", "moment"), ("DAMAGE_SAND_TOMB", "moment"), ("DAMAGE_INGRAIN", "moment")
    };

    /// <summary>An original's name as a moment's: <c>DAMAGE_LEECH_SEED</c> is <c>LeechSeed</c>, <c>ITEM_ESCAPE</c> is <c>EscapeItem</c>.</summary>
    private static string AsMoment(string original)
    {
        if (original == "ITEM_ESCAPE") return nameof(BattleMoment.EscapeItem);
        var words = original.Split('_').Where(w => w != "DAMAGE");
        return string.Concat(words.Select(w => w[0] + w[1..].ToLowerInvariant()));
    }

    [Fact]
    public void EveryAnimationOfTheOriginalsListIsAccountedForAndEveryMomentIsOneOfThem()
    {
        Assert.Equal(40, Checklist.Length);
        var named = Checklist.Where(c => c.Shown == "moment").Select(c => AsMoment(c.Original)).ToList();
        Assert.Equal(Enum.GetNames<BattleMoment>().OrderBy(n => n), named.OrderBy(n => n));
        // The plan's twenty-four, and the held item's, which plan 12 · Q11's ItemShown turned out too wide to carry
        Assert.Equal(25, named.Count);
    }

    // ------------------------------------------------------------------ each moment rides on its line

    private static Pokemon Hurt(Pokemon p, int hp)
    {
        p.CurrentHP = hp;
        return p;
    }

    private static Pokemon Ailing(Pokemon p, StatusCondition status)
    {
        p.Status = status;
        if (status == StatusCondition.Sleep) p.SleepTurns = 4;
        return p;
    }

    /// <summary>The moments a battle's whole log carries, each with the line it rides on.</summary>
    private static List<(MomentShown Moment, Said Line)> MomentsOf(BattleCore core) =>
        core.Log.OfType<Said>().SelectMany(s => s.Shows.OfType<MomentShown>().Select(m => (m, s))).ToList();

    private static BattleCore Marsh(BattleRandom rolls)
    {
        var party = new Party();
        party.Add(Mon("Bidoof", 30, "Tackle"));
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { Mon("Carnivine", 25, "Bind") }, Kind = BattleKind.Safari, SpecialBalls = 30,
            Random = rolls, Rules = Ruleset.Platinum, PlayerController = null, PlayerName = "Lucas"
        });
        core.Start();
        return core;
    }

    /// <summary>A battle played until a moment's line has been said: the moment, the place it should be on, the other end, and words of its line.</summary>
    private static (BattleCore Core, Place On, Place? From, string Words) Scene(BattleMoment moment)
    {
        switch (moment)
        {
            case BattleMoment.Asleep:
            {
                var core = Wild(Mon("Snorlax", 50), Ailing(Mon("Snorlax", 50), StatusCondition.Sleep));
                Turn(core);
                return (core, Foe, null, "is fast asleep");
            }
            case BattleMoment.Poisoned:
            {
                var core = Wild(Mon("Snorlax", 50), Ailing(Mon("Snorlax", 50), StatusCondition.Poison));
                Turn(core);
                return (core, Foe, null, "is hurt by poison");
            }
            case BattleMoment.Burned:
            {
                var core = Wild(Mon("Snorlax", 50), Ailing(Mon("Snorlax", 50), StatusCondition.Burn));
                Turn(core);
                return (core, Foe, null, "is hurt by its burn");
            }
            case BattleMoment.Frozen:
            {
                var core = Wild(Mon("Snorlax", 50), Ailing(Mon("Snorlax", 50), StatusCondition.Freeze), Calm().Force(RollKind.Thaw, 1));
                Turn(core);
                return (core, Foe, null, "is frozen solid");
            }
            case BattleMoment.Paralyzed:
            {
                var core = Wild(Mon("Snorlax", 50), Ailing(Mon("Snorlax", 50), StatusCondition.Paralyze), Calm().Force(RollKind.FullParalysis, 0));
                Turn(core);
                return (core, Foe, null, "is paralyzed! It can't move");
            }
            case BattleMoment.Confused:
            {
                var core = Wild(Mon("Gengar", 50, "Confuse Ray"), Mon("Snorlax", 50), Calm().Force(RollKind.ConfusionTurns, 3).Force(RollKind.ConfusionSelfHit, 1));
                Turn(core);
                Turn(core);
                return (core, Foe, null, "is confused!");
            }
            case BattleMoment.Infatuated:
            {
                var core = Wild(Idling(Of(Gender.Female, "Clefairy", 50, "Attract")), Of(Gender.Male, "Snorlax", 50), Calm().Force(RollKind.Infatuation, 1));
                Turn(core);
                Turn(core, 1);
                return (core, Foe, null, "is in love with");
            }
            case BattleMoment.LevelUp:
            {
                var core = Wild(Mon("Bidoof", 5, "Tackle"), Hurt(Mon("Blissey", 60), 1));
                Turn(core);
                return (core, Mine, null, "grew to Lv.");
            }
            case BattleMoment.BagItem:
            {
                var core = Wild(Hurt(Mon("Snorlax", 50), 20), Mon("Snorlax", 50));
                core.Submit(BattleChoice.UseItem(Mine, "Potion"), BattleChoice.Fight(Foe, 0));
                return (core, Mine, null, "used the Potion");
            }
            case BattleMoment.EscapeItem:
            {
                var core = Wild(Mon("Snorlax", 50), Mon("Snorlax", 50));
                core.Submit(BattleChoice.UseItem(Mine, "Poké Doll"), BattleChoice.Fight(Foe, 0));
                return (core, Mine, null, "used the Poké Doll");
            }
            case BattleMoment.Happy:
            case BattleMoment.Eating:
            {
                var core = Marsh(Calm().Force(RollKind.Safari, 3, 254));
                core.Submit(BattleChoice.Bait(Mine));
                return (core, Foe, null, moment == BattleMoment.Happy ? "tossed some bait" : "munching away");
            }
            case BattleMoment.Angry:
            {
                var core = Marsh(Calm().Force(RollKind.Safari, 3, 254));
                core.Submit(BattleChoice.Mud(Mine));
                return (core, Foe, null, "is fuming");
            }
            case BattleMoment.Curse:
            {
                var core = Wild(Mon("Gengar", 50, "Curse"), Mon("Snorlax", 50));
                Turn(core);
                return (core, Foe, null, "is afflicted by the curse");
            }
            case BattleMoment.Nightmare:
            {
                var core = Wild(Mon("Gengar", 50, "Nightmare"), Ailing(Mon("Snorlax", 50), StatusCondition.Sleep));
                Turn(core);
                return (core, Foe, null, "is locked in a nightmare");
            }
            case BattleMoment.LeechSeed:
            {
                var core = Wild(Mon("Bulbasaur", 50, "Leech Seed"), Mon("Snorlax", 50));
                Turn(core);
                return (core, Foe, Mine, "health is sapped by Leech Seed");
            }
            case BattleMoment.Ingrain:
            {
                var core = Wild(Hurt(Mon("Tangela", 50, "Ingrain"), 20), Mon("Snorlax", 50));
                Turn(core);
                return (core, Mine, null, "absorbed nutrients with its roots");
            }
            case BattleMoment.HeldItem:
            {
                var core = Wild(Hurt(With(Mon("Snorlax", 50), item: "Leftovers"), 20), Mon("Snorlax", 50));
                Turn(core);
                return (core, Mine, null, "restored a little HP using its Leftovers");
            }
            default:
            {
                // The seven binds, each by its own move
                string move = moment switch
                {
                    BattleMoment.Bind => "Bind", BattleMoment.Wrap => "Wrap", BattleMoment.FireSpin => "Fire Spin", BattleMoment.MagmaStorm => "Magma Storm",
                    BattleMoment.Clamp => "Clamp", BattleMoment.Whirlpool => "Whirlpool", _ => "Sand Tomb"
                };
                var core = Wild(Mon("Mew", 50, move), Mon("Snorlax", 50), Calm().Force(RollKind.Duration, 1));
                Turn(core);
                return (core, Foe, null, $"is hurt by {move}");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Moments))]
    public void EachMomentRidesOnTheLineThatTellsOfIt(BattleMoment moment)
    {
        var (core, on, from, words) = Scene(moment);
        var line = core.Log.OfType<Said>().FirstOrDefault(s => s.Text.Contains(words));
        Assert.True(line != null, $"\"{words}\" isn't said in:\n{string.Join("\n", Lines(core.Log))}");
        Assert.Contains(new MomentShown(on, moment, from), line!.Shows);

        // A moment comes before the HP its line changes, so the screen plays it first
        int at = line.Shows.FindIndex(e => e is MomentShown);
        int hp = line.Shows.FindIndex(e => e is HpChanged);
        if (hp >= 0) Assert.True(at < hp, "the moment follows the line's HP");

        // And no line that doesn't tell of it carries it
        Assert.All(MomentsOf(core).Where(m => m.Moment.Moment == moment), m => Assert.Contains(words, m.Line.Text));
    }

    [Fact]
    public void AMomentThatHurtsOrHealsSharesItsLineWithTheHp()
    {
        foreach (var moment in new[] { BattleMoment.Poisoned, BattleMoment.Burned, BattleMoment.Curse, BattleMoment.Nightmare, BattleMoment.LeechSeed, BattleMoment.Ingrain,
                     BattleMoment.Bind, BattleMoment.Wrap, BattleMoment.FireSpin, BattleMoment.MagmaStorm, BattleMoment.Clamp, BattleMoment.Whirlpool, BattleMoment.SandTomb, BattleMoment.HeldItem })
        {
            var (core, on, _, words) = Scene(moment);
            var line = core.Log.OfType<Said>().First(s => s.Text.Contains(words));
            Assert.Contains(line.Shows, e => e is HpChanged hp && hp.Place == on && hp.Healed == (moment is BattleMoment.Ingrain or BattleMoment.HeldItem));
        }
    }

    [Fact]
    public void ALineThatGoesUnsaidTakesItsMomentWithIt()
    {
        // Magic Guard takes the poison's hurt, and its line, away: the moment must not ride on whatever is said next
        var core = Wild(Mon("Snorlax", 50), Ailing(With(Mon("Clefable", 50), ability: "Magic Guard"), StatusCondition.Poison));
        Turn(core);
        Turn(core);
        Assert.DoesNotContain(Lines(core.Log), s => s.Contains("hurt by poison"));
        Assert.Empty(MomentsOf(core));
        Assert.DoesNotContain(core.Log, e => e is MomentShown);
    }

    [Fact]
    public void ALineThatOnlyNamesAnItemCarriesNoMoment()
    {
        // Frisk names the item it finds (plan 12 · Q11's ItemShown) and nothing goes off
        var core = Wild(With(Mon("Banette", 50), ability: "Frisk"), With(Mon("Snorlax", 50), item: "Leftovers"));
        var frisked = core.Log.OfType<Said>().Single(s => s.Text.Contains("frisked"));
        Assert.Contains(frisked.Shows, e => e is ItemShown { Item: "Leftovers" });
        Assert.DoesNotContain(frisked.Shows, e => e is MomentShown);

        // The same Leftovers going off at the turn's end names itself and shows
        Hurt(core.At(Foe).Pokemon!, 20);
        Turn(core);
        var restored = core.Log.OfType<Said>().Single(s => s.Text.Contains("restored a little HP"));
        Assert.Contains(restored.Shows, e => e is ItemShown { Item: "Leftovers" } named && named.Place == Foe);
        Assert.Contains(new MomentShown(Foe, BattleMoment.HeldItem), restored.Shows);
    }

    [Fact]
    public void AnOrbsConditionShowsItsItemGoingOff()
    {
        // subscript_badly_poison with a held item's side effect plays the held item's animation first
        var core = Wild(With(Mon("Machamp", 50), item: "Toxic Orb"), Mon("Snorlax", 50));
        Turn(core);
        var poisoned = core.Log.OfType<Said>().SingleOrDefault(s => s.Text.Contains("badly poisoned"));
        Assert.True(poisoned != null, string.Join(" / ", Lines(core.Log)));
        Assert.Equal(new MomentShown(Mine, BattleMoment.HeldItem), poisoned!.Shows[0]);
        Assert.Contains(poisoned.Shows, e => e is StatusChanged { Status: StatusCondition.Toxic });

        // An orb that can't give its condition says nothing and shows nothing
        var steel = Wild(With(Mon("Steelix", 50), item: "Toxic Orb"), Mon("Snorlax", 50));
        Turn(steel);
        Assert.DoesNotContain(steel.Log, e => e is MomentShown);
        Assert.Empty(MomentsOf(steel));
    }

    [Fact]
    public void AQuickClawShowsWithNoLineOfItsOwn()
    {
        // subscript_check_quick_claw plays the held item's animation and says nothing for the claw: the moment stands
        // between lines, just before its holder's move, unless the holder is the last to act anyway
        var core = Wild(With(Mon("Snorlax", 50, "Tackle"), item: "Quick Claw"), Mon("Jolteon", 50), Calm().Force(RollKind.Speed, 0));
        Turn(core);
        int at = core.Log.ToList().FindIndex(e => e is MomentShown);
        Assert.True(at >= 0, string.Join(" / ", Lines(core.Log)));
        Assert.Equal(new MomentShown(Mine, BattleMoment.HeldItem), core.Log[at]);
        Assert.Equal("Snorlax used Tackle!", Assert.IsType<Said>(core.Log[at + 1]).Text);
        Assert.Empty(MomentsOf(core));

        // A claw that doesn't go off shows nothing
        var quiet = Wild(With(Mon("Snorlax", 50, "Tackle"), item: "Quick Claw"), Mon("Jolteon", 50), Calm().Force(RollKind.Speed, 1));
        Turn(quiet);
        Assert.DoesNotContain(quiet.Log, e => e is MomentShown);
    }

    [Fact]
    public void AnItemUsedOnTheBenchShowsNowhere()
    {
        // The Potion goes to the second of the team, who isn't on the field
        var core = Wild(Mon("Snorlax", 50), Mon("Snorlax", 50), bench: new[] { Hurt(Mon("Bidoof", 20), 5) });
        core.Submit(BattleChoice.UseItem(Mine, "Potion", onPartyMember: 1), BattleChoice.Fight(Foe, 0));
        Assert.Contains(Lines(core.Log), s => s.Contains("used the Potion"));
        Assert.Empty(MomentsOf(core));
    }

    [Fact]
    public void ALevelShowsOnlyOnAPokemonOnTheField()
    {
        // Whoever fought and was called back levels on the bench: its line carries no moment
        var core = Wild(Mon("Bidoof", 5, "Tackle"), Hurt(Mon("Blissey", 60), 1), bench: new[] { Mon("Machamp", 50, "Strength") });
        SwitchTo(core, 1);
        Turn(core);
        var levels = core.Log.OfType<Said>().Where(s => s.Text.Contains("grew to Lv.")).ToList();
        Assert.Contains(levels, s => s.Text.StartsWith("Bidoof"));
        Assert.All(levels.Where(s => s.Text.StartsWith("Bidoof")), s => Assert.DoesNotContain(s.Shows, e => e is MomentShown));
    }

    // ------------------------------------------------------------------ an attack sent ahead

    [Theory]
    [InlineData("Alakazam", "Future Sight")]
    [InlineData("Jirachi", "Doom Desire")]
    public void AnAttackSentAheadLandsWithItsOwnMovesCueFromWhereItWasSent(string sender, string move)
    {
        var core = Wild(Idling(Mon(sender, 50, move)), Mon("Snorlax", 50));
        Turn(core);
        Turn(core, 1);
        Assert.DoesNotContain(core.Log.OfType<Said>().SelectMany(s => s.Shows), e => e is AheadLanded);
        Turn(core, 1);

        var took = core.Log.OfType<Said>().Single(s => s.Text.Contains($"took the {move} attack"));
        var data = MoveDatabase.Get(move);
        Assert.Contains(new AheadLanded(move, data.Type, data.Category, Mine, Foe), took.Shows);
    }

    [Fact]
    public void AnAttackSentAheadThatFailsPlaysNothing()
    {
        // subscript_future_sight_damage goes past its animation when the attack doesn't hit
        var core = Wild(Idling(Mon("Alakazam", 50, "Future Sight")), Idling(Mon("Snorlax", 50, "Protect")));
        Turn(core, 0, 1);
        Turn(core, 1, 1);
        Turn(core, 1, 0);
        var said = Lines(core.Log);
        InOrder(said, "Foe Snorlax took the Future Sight attack!", "But it failed!");
        Assert.DoesNotContain(core.Log.OfType<Said>().SelectMany(s => s.Shows), e => e is AheadLanded);
    }

    // ------------------------------------------------------------------ the screen: a moment first, its HP quietly

    /// <summary>Reads a battle on to the line that holds <paramref name="words"/> and stops with it on screen.</summary>
    private static bool ReadTo(BattleEngine battle, string words)
    {
        for (int i = 0; i < 400 && !battle.IsBattleOver; i++)
        {
            if (battle.HUD.MenuState is not (BattleMenuState.Message or BattleMenuState.LearnMove)) return false;
            if (battle.IsWaitingForConfirm && battle.CurrentMessage.Contains(words)) return true;
            battle.ConfirmMessage();
            battle.Update(1f / 60f);
        }
        return false;
    }

    private static List<string> Hear(Action play)
    {
        var heard = AudioManager.Listen();
        try
        {
            play();
            return heard.ToList();
        }
        finally
        {
            AudioManager.StopListening();
        }
    }

    [Fact]
    public void APoisonTickIsAMomentAndNoHit()
    {
        var foe = Ailing(Mon("Snorlax", 50), StatusCondition.Poison);
        var battle = Battle(Mon("Snorlax", 50), foe);
        var view = battle.Anim[BattleSide.Enemy];
        int before = foe.CurrentHP;

        battle.SelectMove(0);
        var heard = Hear(() => Assert.True(ReadTo(battle, "is hurt by poison")));

        // The moment plays as its line appears; the body doesn't flinch, the camera doesn't shake and no blow is heard
        var cue = Assert.Single(battle.Anim.Cues, c => c.Kind == CueKind.Moment);
        Assert.Equal(BattleMoment.Poisoned, cue.Moment);
        Assert.Equal(BattleSide.Enemy, cue.ToSide);
        Assert.Equal(-1f, view.HitAge);
        Assert.Equal(-1f, battle.Anim.ShakeAge);
        Assert.Contains("status_poison", heard);
        Assert.DoesNotContain("hit_normal", heard);

        // Its HP lands with the moment, a third of a second in, and the bar drains from there
        Assert.Equal(before, foe.CurrentHP);
        heard = Hear(() => { for (int i = 0; i < 30; i++) battle.Update(1f / 60f); });
        Assert.Equal(before - before / 8, foe.CurrentHP);
        Assert.Equal(-1f, view.HitAge);
        Assert.DoesNotContain("hit_normal", heard);

        // Read on at once and the HP is there all the same
        var quick = Battle(Mon("Snorlax", 50), Ailing(Mon("Snorlax", 50), StatusCondition.Poison));
        Turn(quick);
        Assert.Equal(before - before / 8, quick.EnemyPokemon.CurrentHP);
    }

    [Fact]
    public void AHurtWithNoMomentStillLandsAsAHit()
    {
        // A sandstorm's buffeting keeps its blink in the original (subscript_weather_continues sets no skip), and here
        var battle = Battle(Mon("Snorlax", 50, "Sandstorm"), Mon("Snorlax", 50));
        battle.SelectMove(0);
        var heard = Hear(() => Assert.True(ReadTo(battle, "is buffeted by the sandstorm")));
        Assert.Contains("hit_normal", heard);
        Assert.True(battle.Anim.ShakeAge >= 0f);
    }

    [Fact]
    public void LeftoversGoesOffAsAHeldItemAndTheBarFillsQuietly()
    {
        var mine = Hurt(With(Mon("Snorlax", 50), item: "Leftovers"), 40);
        var battle = Battle(mine, Mon("Snorlax", 50));
        battle.SelectMove(0);
        var heard = Hear(() => Assert.True(ReadTo(battle, "restored a little HP")));

        var cue = Assert.Single(battle.Anim.Cues, c => c.Kind == CueKind.Moment);
        Assert.Equal(BattleMoment.HeldItem, cue.Moment);
        Assert.False(cue.Berry);
        Assert.DoesNotContain(battle.Anim.Cues, c => c.Kind == CueKind.Heal);
        Assert.Contains("moment_held_item", heard);
        Assert.DoesNotContain("heal", heard);

        Assert.Equal(40, mine.CurrentHP);
        Scenario.Settle(battle);
        Assert.Equal(40 + mine.MaxHP / 16, mine.CurrentHP);
    }

    [Fact]
    public void ABerryEatenShowsAsABerry()
    {
        // A Sitrus Berry at half HP or less: the line names it, and the screen draws the fruit
        var foe = Hurt(With(Mon("Snorlax", 50), item: "Sitrus Berry"), 112);
        var battle = Battle(Mon("Machamp", 50, "Strength"), foe);
        battle.SelectMove(0);
        Assert.True(ReadTo(battle, "restored its health using its Sitrus Berry"));
        var cue = Assert.Single(battle.Anim.Cues, c => c.Kind == CueKind.Moment);
        Assert.Equal(BattleMoment.HeldItem, cue.Moment);
        Assert.True(cue.Berry);
    }

    [Fact]
    public void BecomingConfusedAndBeingConfusedEachPlayTheMarkOnce()
    {
        var battle = Battle(Mon("Gengar", 50, "Confuse Ray"), Mon("Snorlax", 50),
            Steady().Force(RollKind.ConfusionTurns, 3).Force(RollKind.ConfusionSelfHit, 1));
        battle.SelectMove(0);
        var heard = Hear(() => Assert.True(ReadTo(battle, "became confused")));
        Assert.Single(battle.Anim.Cues, c => c.Kind == CueKind.Moment && c.Moment == BattleMoment.Confused);
        Assert.Single(heard, s => s == "status_confusion");

        Scenario.Settle(battle);
        battle.SelectMove(0);
        heard = Hear(() => Assert.True(ReadTo(battle, "is confused!")));
        Assert.Single(battle.Anim.Cues, c => c.Kind == CueKind.Moment && c.Moment == BattleMoment.Confused && c.Age < 0.03f);
        Assert.Single(heard, s => s == "status_confusion");
    }

    [Fact]
    public void TheMomentsCountForThemselvesSoAMovesSeedStaysWhatItWas()
    {
        // A battle's moves keep their looks whether or not a moment played before them
        var plain = new BattleAnimator();
        var marked = new BattleAnimator();
        marked.Moment(BattleSide.Enemy, 0, BattleMoment.Poisoned);
        marked.Moment(BattleSide.Player, 0, BattleMoment.HeldItem);
        Assert.Equal(plain.Cue(new EffectCue { Move = "Ember" }).Seed, marked.Cue(new EffectCue { Move = "Ember" }).Seed);
        // And two moments of a kind differ
        Assert.NotEqual(marked.Moment(BattleSide.Enemy, 0, BattleMoment.Poisoned).Seed, marked.Moment(BattleSide.Enemy, 0, BattleMoment.Poisoned).Seed);
    }

    [Fact]
    public void WhatShowsBehindADollHoldsForTheMoments()
    {
        // BattleSystem_ShouldShowStatusEffect: behind a Substitute or out of sight only what the original always shows
        // plays (the escape item among them); a level is played without the check; Leech Seed needs both its ends
        var anim = new BattleAnimator { Slots = 1 };
        anim.SubstituteUp(BattleSide.Enemy);
        foreach (var moment in Enum.GetValues<BattleMoment>())
        {
            var cue = anim.Moment(BattleSide.Enemy, 0, moment);
            bool always = moment is BattleMoment.EscapeItem or BattleMoment.LevelUp;
            Assert.Equal(always, anim.Cues.Contains(cue));
            Assert.Contains(anim.Moment(BattleSide.Player, 0, moment), anim.Cues);
        }

        // The seeded Pokémon can be seen and the seeder is behind its doll: nothing plays
        var sapped = anim.Moment(BattleSide.Player, 0, BattleMoment.LeechSeed, (BattleSide.Enemy, 0));
        Assert.DoesNotContain(sapped, anim.Cues);
        anim.SubstituteDown(BattleSide.Enemy);
        Assert.Contains(anim.Moment(BattleSide.Player, 0, BattleMoment.LeechSeed, (BattleSide.Enemy, 0)), anim.Cues);

        // Out of sight in the middle of a move
        anim[BattleSide.Player].Away = true;
        Assert.DoesNotContain(anim.Moment(BattleSide.Player, 0, BattleMoment.Burned), anim.Cues);
    }

    [Fact]
    public void AnAttackSentAheadIsWatchedWhereItLands()
    {
        var battle = Battle(Idling(Mon("Alakazam", 50, "Future Sight")), Mon("Snorlax", 50));
        Turn(battle);
        Turn(battle, 1);
        battle.SelectMove(1);
        var heard = Hear(() => Assert.True(ReadTo(battle, "took the Future Sight attack")));

        var cue = Assert.Single(battle.Anim.Cues, c => c.Arriving);
        Assert.True(cue.Arriving);
        Assert.Equal("Future Sight", cue.Move);
        Assert.Equal((BattleSide.Player, BattleSide.Enemy), (cue.FromSide, cue.ToSide));
        // Future Sight has a sound of its own since plan 25 · Y8a (its type's was heard before)
        Assert.Contains("use_future_sight", heard);

        // The camera goes to the target at once: nobody need stand where it was sent from
        var (_, kind, followed) = BattleCamera.Direct(battle.Anim, new FxPlaces());
        Assert.Equal(ShotKind.Target, kind);
        Assert.Same(cue, followed);
    }

    // ------------------------------------------------------------------ the looks

    private static readonly FxPlaces Places = new();

    private static FxList Look(BattleMoment moment, float age, int seed = 6007, bool berry = false, bool twoEnded = true)
    {
        bool between = moment == BattleMoment.LeechSeed && twoEnded;
        var cue = new EffectCue
        {
            Kind = CueKind.Moment, Moment = moment, Berry = berry, ToSide = BattleSide.Enemy,
            FromSide = between ? BattleSide.Player : BattleSide.Enemy, Seed = seed, Age = age
        };
        var fx = new FxList();
        MoveFx.Play(cue, Places, fx);
        return fx;
    }

    public static IEnumerable<object[]> Looks =>
        Enum.GetValues<BattleMoment>().Select(m => new object[] { m, false }).Append(new object[] { BattleMoment.HeldItem, true });

    [Theory]
    [MemberData(nameof(Looks))]
    public void EachLookDrawsStopsAndStaysUnderItsCap(BattleMoment moment, bool berry)
    {
        int most = 0;
        for (float age = 0.02f; age < BattleAnimator.MomentTime; age += 0.02f)
        {
            var a = Look(moment, age, berry: berry);
            // Pure in the cue's age and seed
            var b = Look(moment, age, berry: berry);
            Assert.Equal(a.Quads, b.Quads);
            Assert.Equal(a.Ribbons.Count, b.Ribbons.Count);
            Assert.All(a.Quads, q => Assert.True(float.IsFinite(q.Center.X + q.Center.Y + q.Center.Z + q.Size), $"{moment} at {age}"));
            Assert.True(a.Count <= MoveFx.MomentCap, $"{moment} draws {a.Count} at {age}");
            // A moment never flashes the screen or shakes the camera: the turn's end is many of them in a row
            Assert.Equal(0f, a.Flash);
            Assert.Equal(0f, a.Shake);
            most = Math.Max(most, a.Count);
        }
        Assert.True(most >= 4, $"{moment} draws at most {most}");

        // It is on screen as it lands, and gone when its time is up
        Assert.True(Look(moment, BattleAnimator.ImpactTime, berry: berry).Count > 0);
        Assert.Equal(0, Look(moment, BattleAnimator.MomentTime + 0.01f, berry: berry).Count);
        Assert.Equal(0, Look(moment, BattleAnimator.MomentTime + 0.5f, berry: berry).Count);
    }

    [Fact]
    public void TheLooksAreTheirOwn()
    {
        // No two moments draw the same picture as they land, and a berry isn't drawn as any other item
        var seen = new List<(string Name, List<FxQuad> Quads)>();
        foreach (var moment in Enum.GetValues<BattleMoment>())
            seen.Add((moment.ToString(), Look(moment, BattleAnimator.ImpactTime + 0.05f).Quads));
        seen.Add(("a berry", Look(BattleMoment.HeldItem, BattleAnimator.ImpactTime + 0.05f, berry: true).Quads));
        for (int i = 0; i < seen.Count; i++)
            for (int j = i + 1; j < seen.Count; j++)
                Assert.False(seen[i].Quads.SequenceEqual(seen[j].Quads), $"{seen[i].Name} and {seen[j].Name} look the same");
    }

    [Fact]
    public void LeechSeedTravelsFromTheSeededToTheSeeder()
    {
        var seeded = Places.Body(BattleSide.Enemy, 0);
        var seeder = Places.Body(BattleSide.Player, 0);
        float tall = Places.Height(BattleSide.Enemy, 0);

        // The motes are the small lights: the glow over the body and the glint where they arrive are bigger
        List<FxQuad> Motes(float age) => Look(BattleMoment.LeechSeed, age).Quads
            .Where(q => q.Shape == FxShape.Glow && q.Size < tall * 0.1f && q.Blend == FxBlend.Add).ToList();

        var leaving = Motes(0.16f);
        Assert.NotEmpty(leaving);
        Assert.All(leaving, q => Assert.True(Vector3.Distance(q.Center, seeded) < Vector3.Distance(q.Center, seeder)));

        var arriving = Motes(0.98f);
        Assert.NotEmpty(arriving);
        Assert.All(arriving, q => Assert.True(Vector3.Distance(q.Center, seeder) < Vector3.Distance(q.Center, seeded)));

        // On the way each is further along than it was
        float Along(float age) => Motes(age).Average(q => Vector3.Dot(q.Center - seeded, Vector3.Normalize(seeder - seeded)));
        Assert.True(Along(0.3f) < Along(0.5f) && Along(0.5f) < Along(0.7f));
    }

    [Theory]
    [InlineData("Future Sight", PokemonType.Psychic)]
    [InlineData("Doom Desire", PokemonType.Steel)]
    public void AnAttackSentAheadHasALandingOfItsOwn(string move, PokemonType type)
    {
        FxList At(float age, bool arriving)
        {
            var cue = new EffectCue
            {
                Kind = CueKind.Move, Move = move, Type = type, Category = MoveCategory.Special, FromSide = BattleSide.Player, ToSide = BattleSide.Enemy,
                Arriving = arriving, Seed = 7919, Age = age
            };
            var fx = new FxList();
            MoveFx.Play(cue, Places, fx);
            return fx;
        }

        // It isn't the move as it is used, it lands at the impact like any move, and it is over in a move's time
        Assert.NotEqual(At(0.2f, false).Quads, At(0.2f, true).Quads);
        Assert.Equal(At(0.2f, true).Quads, At(0.2f, true).Quads);
        Assert.Contains(At(BattleAnimator.ImpactTime + 0.03f, true).Quads, q => q.Shape == FxShape.Impact);
        Assert.Equal(0, At(BattleAnimator.EffectTime + 0.01f, true).Count);
        for (float age = 0.02f; age < BattleAnimator.EffectTime; age += 0.02f)
            Assert.True(At(age, true).Flash <= 0.5f);

        // Something shows in the place it was sent from as it sets off
        var sender = Places.Body(BattleSide.Player, 0);
        Assert.Contains(At(0.1f, true).Quads, q => Vector3.Distance(q.Center, sender) < 0.01f);
    }

    // ------------------------------------------------------------------ the sounds

    [Fact]
    public void EachMomentHasItsSound()
    {
        foreach (var moment in Enum.GetValues<BattleMoment>())
        {
            string? sound = SoundBank.MomentSound(moment);
            // A level's fanfare plays with it
            if (moment == BattleMoment.LevelUp)
            {
                Assert.Null(sound);
                continue;
            }
            Assert.True(sound != null && SoundBank.Exists(sound), moment.ToString());
        }
        // A condition as it plays on its turn sounds as it did when it was given
        Assert.Equal(SoundBank.StatusSound(StatusCondition.Poison), SoundBank.MomentSound(BattleMoment.Poisoned));
        Assert.Equal(SoundBank.StatusSound(StatusCondition.Sleep), SoundBank.MomentSound(BattleMoment.Asleep));
    }
}
