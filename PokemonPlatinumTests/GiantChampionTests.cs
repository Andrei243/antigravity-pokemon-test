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
/// The Champions with the Mega Max Band (plan 25 · YD6; style guide, "The Mega Max band", "The boss box", "The lines
/// wait for it", "The Champion's words"): the screen's alone, so the battles here are the rules' own, and only what is
/// shown is held.
/// </summary>
public class GiantChampionTests
{
    private const float Frame = 1f / 60f;

    private static readonly string[] MegaMaxTeams =
    {
        "champion_cynthia_megamax", "kanto_champion_megamax_squirtle", "kanto_champion_megamax_bulbasaur", "kanto_champion_megamax_charmander",
        "johto_champion_lance_megamax", "hoenn_steven_megamax", "unova_champion_iris_megamax"
    };

    private static BattleMeeting Meeting(TrainerRecord record, bool two = false) => new()
    {
        Trainers = new[] { new MetTrainer(record.Class, record.Name, record.Id) }, Double = two, Terrain = BattleTerrain.Plain, TheirLevel = 80, OwnLevel = 80
    };

    // ------------------------------------------------------------------ the band

    [Fact]
    public void TheSevenMegaMaxTeamsAreEveryOneOfOurs()
    {
        var ours = TrainerDatabase.Ours.Where(r => TrainerDatabase.IsMegaMaxTeam(r.Id)).Select(r => r.Id).OrderBy(i => i).ToList();
        Assert.Equal(MegaMaxTeams.OrderBy(i => i), ours);
        // Nothing else is told one: a rematch, a team beyond, a Champion's first battle
        foreach (string id in new[] { "champion_cynthia", "champion_cynthia_rematch", "hoenn_steven", "leader_roark", "" })
            Assert.False(TrainerDatabase.IsMegaMaxTeam(id), id);
    }

    [Fact]
    public void AMegaMaxTeamOpensOnTheLeaguesBandMadeMegaMaxAndNoOtherDoes()
    {
        foreach (string id in MegaMaxTeams)
        {
            var record = TrainerDatabase.Get(id)!;
            var meeting = Meeting(record);
            Assert.Equal(TransitionKind.EliteBand, SceneTransition.ForBattle(meeting));
            var band = SceneTransition.BandFor(meeting);
            Assert.True(band is { MegaMax: true, Class: "Champion" }, $"{id}: {band}");
            Assert.Equal(record.Name, band!.Value.Name);
            // Never the plain steel grey
            Assert.NotEqual(SceneTransition.PlainBand.Base, band.Value.Base);
        }
        // Cynthia keeps her own colours, Kanto's Champion the rival's purple
        Assert.Equal(SceneTransition.Bands.Last().Base, SceneTransition.BandFor(Meeting(TrainerDatabase.Get("champion_cynthia_megamax")!))!.Value.Base);
        Assert.Equal(SceneTransition.KantoBands.Last().Base, SceneTransition.BandFor(Meeting(TrainerDatabase.Get("kanto_champion_megamax_squirtle")!))!.Value.Base);

        // The same Champions' other teams keep their bands as they were, and Steven's first battle (the Rival's class) none
        var plain = SceneTransition.BandFor(Meeting(TrainerDatabase.Get("champion_cynthia")!));
        Assert.True(plain is { MegaMax: false });
        Assert.Equal(SceneTransition.Bands.Last(), plain!.Value);
        Assert.Null(SceneTransition.BandFor(Meeting(TrainerDatabase.Get("hoenn_steven")!)));
        // A double battle loses the band, a Mega Max team's too
        Assert.Null(SceneTransition.BandFor(Meeting(TrainerDatabase.Get("champion_cynthia_megamax")!, two: true)));
    }

    [Fact]
    public void TheRedRisesThroughTheBandFromWhenItsHalvesMeetToTheWhole()
    {
        var kind = TransitionKind.EliteBand;
        float from = SceneTransition.FlashSeconds;
        Assert.Equal(0f, SceneTransition.MegaMaxRise(kind, from - 0.01f));
        Assert.Equal(0f, SceneTransition.MegaMaxRise(kind, from + SceneTransition.MegaMaxRiseFrom));
        float last = 0f;
        for (float t = from; t < SceneTransition.OutSeconds(kind); t += 0.02f)
        {
            float rise = SceneTransition.MegaMaxRise(kind, t);
            Assert.InRange(rise, last, 1f);
            last = rise;
        }
        Assert.Equal(1f, SceneTransition.MegaMaxRise(kind, from + SceneTransition.MegaMaxRiseFrom + SceneTransition.MegaMaxRiseTakes + 0.01f), 3);
        Assert.Equal(0f, SceneTransition.MegaMaxRise(kind, SceneTransition.OutSeconds(kind)));
        // A Leader's band has no red
        Assert.Equal(0f, SceneTransition.MegaMaxRise(TransitionKind.LeaderBand, from + 1f));
    }

    // ------------------------------------------------------------------ the words

    [Fact]
    public void EachMegaMaxTeamHasItsLinesAndAWordAsItsAceGrows()
    {
        foreach (string id in MegaMaxTeams)
        {
            Assert.NotNull(TrainerLines.GiantFor(id));
            foreach (var cue in Enum.GetValues<TrainerCue>()) Assert.True(TrainerLines.For(id, cue) != null, $"{id} has nothing for {cue}");
            // Its ace is named in what it says as it grows: the Mega Dynamax ace of the team
            var record = TrainerDatabase.Get(id)!;
            var trainer = new Trainer { Id = id };
            TrainerDatabase.Fill(trainer, record);
            var ace = trainer.Party.Members.Single(p => p.HeldItem?.Name is { } item && item.EndsWith("ite", StringComparison.Ordinal) && p.Species.Name != "Eevee");
            Assert.Contains(ace.Species.Name, TrainerLines.GiantFor(id)!.Text);
        }
        // Nobody else has one yet
        Assert.Null(TrainerLines.GiantFor("champion_cynthia"));
    }

    // ------------------------------------------------------------------ the battle: the hold, the words, the boss box

    /// <summary>Cynthia's Mega Max team with her Garchomp first, against a Blissey that only Splashes.</summary>
    private static BattleEngine AgainstCynthia(bool scene = true, string id = "champion_cynthia_megamax")
    {
        var record = TrainerDatabase.Get(id)!;
        var cynthia = new Trainer { Id = record.Id };
        TrainerDatabase.Fill(cynthia, record);
        int ace = cynthia.Party.Members.FindIndex(p => p.Species.Name == "Garchomp");
        var g = cynthia.Party.Members[ace];
        cynthia.Party.Members.RemoveAt(ace);
        cynthia.Party.Members.Insert(0, g);
        var party = new Party();
        party.Add(Mon("Blissey", 100, "Splash"));
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(), Trainers = new List<Trainer> { cynthia },
            Random = Calm(), Rules = Ruleset.Modern, TrainerLines = true, Scene = scene
        });
        Settle(battle);
        return battle;
    }

    /// <summary>Splashes and reads on, a frame at a time, until the predicate holds; false if it never did.</summary>
    private static bool Until(BattleEngine battle, Func<bool> wanted, int frames = 3000)
    {
        for (int i = 0; i < frames && !battle.IsBattleOver; i++)
        {
            if (wanted()) return true;
            if (battle.HUD.MenuState == BattleMenuState.Main)
            {
                foreach (var p in battle.PlayerSlots.Where(s => s.Pokemon != null)) p.Pokemon!.CurrentHP = p.Pokemon.MaxHP;
                battle.SelectMainMenuOption(0);
                battle.SelectMove(0);
            }
            else if (battle.IsWaitingForConfirm) battle.ConfirmMessage();
            battle.Update(Frame);
        }
        return wanted();
    }

    [Fact]
    public void ALineReadWhileTheGrowthPlaysWaitsForItsEnd()
    {
        var battle = AgainstCynthia();
        Assert.True(Until(battle, () => battle.Anim.Cues.Any(c => c.Kind == CueKind.Dynamax) && battle.IsWaitingForConfirm), "no growth");
        var growth = battle.Anim.Cues.First(c => c.Kind == CueKind.Dynamax);
        string line = battle.CurrentMessage;
        Assert.True(battle.GrowthHold > 0.5f, $"the hold was {battle.GrowthHold}");
        battle.ConfirmMessage();
        // The line stays and nothing is asked for, until the growth is over
        Assert.False(battle.IsWaitingForConfirm);
        float waited = 0f;
        while (battle.Anim.Cues.Contains(growth) && waited < 5f)
        {
            Assert.Equal(line, battle.CurrentMessage);
            Assert.Equal(BattleMenuState.Message, battle.HUD.MenuState);
            battle.Update(Frame);
            waited += Frame;
        }
        Assert.True(waited > 0.5f, $"it waited {waited} s");
        Assert.False(battle.Anim.Cues.Contains(growth));
        battle.Update(Frame);
        Assert.NotEqual(line, battle.CurrentMessage);
    }

    [Fact]
    public void WithTheSceneOffNothingWaitsForAGrowth()
    {
        var battle = AgainstCynthia(scene: false);
        Assert.True(Until(battle, () => battle.Anim.Player != null && battle.Anim.Enemy.Dynamaxed && battle.IsWaitingForConfirm), "no Dynamax");
        Assert.Equal(0f, battle.GrowthHold);
        string line = battle.CurrentMessage;
        battle.ConfirmMessage();
        Assert.NotEqual(line, battle.CurrentMessage);
        // ...and the boss box is there at once: it is state of the giant standing
        Assert.Equal(1f, battle.BossShare(battle.Anim.Enemy));
    }

    [Fact]
    public void CynthiaSaysHerWordOnceTheGrowthIsOver()
    {
        var battle = AgainstCynthia();
        string said = PlayerIdentity.Fill(Text.OfData(TrainerLines.GiantFor("champion_cynthia_megamax")!.Text));
        EffectCue? growth = null;
        Assert.True(Until(battle, () =>
        {
            growth ??= battle.Anim.Cues.FirstOrDefault(c => c.Kind == CueKind.Dynamax);
            return battle.CurrentMessage == said;
        }), "Cynthia never said it");
        Assert.NotNull(growth);
        Assert.False(battle.Anim.Cues.Contains(growth!), "she spoke while it was still growing");
        Assert.Equal("Cynthia", battle.CurrentSpeaker);
        // Once a battle, as the ace grows only once
        Assert.True(Until(battle, () => battle.Anim.Enemy.Dynamaxed == false && battle.Anim.Cues.Any(c => c.Kind == CueKind.DynamaxEnd)));
    }

    [Fact]
    public void TheBossBoxWidensAtTheBurstHoldsTheBoxsHpAndNarrowsBackOverTheFall()
    {
        var battle = AgainstCynthia();
        var foe = battle.Anim.Enemy;
        Assert.Equal(0f, battle.BossShare(foe));
        // Through the Mega Evolution and the call nothing is widened
        Assert.True(Until(battle, () => battle.Anim.Cues.Any(c => c.Kind == CueKind.Dynamax)));
        var growth = battle.Anim.Cues.First(c => c.Kind == CueKind.Dynamax);
        bool widened = false;
        float last = 0f;
        while (battle.Anim.Cues.Contains(growth))
        {
            float share = battle.BossShare(foe);
            float burst = growth.Lead + BattleAnimator.DynamaxGrown;
            if (growth.Age < burst - 0.02f) Assert.Equal(0f, share);
            Assert.InRange(share, last, 1f);
            last = share;
            widened |= share >= 1f;
            // The bar holds exactly the share the box would, the raised HP's
            Assert.Equal(foe.DisplayedHp / Math.Max(1, foe.Shown!.MaxHP), ModernUi.BossBarShare(foe), 4);
            battle.Update(Frame);
        }
        Assert.True(widened, "never widened");
        Assert.Equal(1f, battle.BossShare(foe));
        Assert.True(foe.Shown!.MaxHP > 300, $"the box's most is {foe.Shown.MaxHP}");
        // The three pips: three lit as it begins
        Assert.Equal(3, ModernUi.BossPips(foe).Lit);
        // The fall: the end narrows the box back
        EffectCue? end = null;
        Assert.True(Until(battle, () => (end = battle.Anim.Cues.FirstOrDefault(c => c.Kind == CueKind.DynamaxEnd)) != null), "no end");
        float during = battle.BossShare(foe);
        Assert.InRange(during, 0.01f, 1f);
        while (battle.Anim.Cues.Contains(end!)) battle.Update(Frame);
        Assert.Equal(0f, battle.BossShare(foe));
        Assert.Equal(foe.DisplayedHp / Math.Max(1, foe.Shown!.MaxHP), ModernUi.BossBarShare(foe), 4);
    }

    [Fact]
    public void ATrainerWhoIsNoMegaMaxTeamHasNoBossBoxForTheSameMegaDynamax()
    {
        // The same team under another id: a Mega Dynamax all the same, a box of its own size
        var record = TrainerDatabase.Get("champion_cynthia_megamax")!;
        var vera = new Trainer { Id = "ace_vera" };
        TrainerDatabase.Fill(vera, record);
        vera.Id = "ace_vera";
        int ace = vera.Party.Members.FindIndex(p => p.Species.Name == "Garchomp");
        var g = vera.Party.Members[ace];
        vera.Party.Members.RemoveAt(ace);
        vera.Party.Members.Insert(0, g);
        var party = new Party();
        party.Add(Mon("Blissey", 100, "Splash"));
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(), Trainers = new List<Trainer> { vera },
            Random = Calm(), Rules = Ruleset.Modern
        });
        Settle(battle);
        Assert.True(Until(battle, () => battle.Anim.Enemy.Dynamaxed && battle.Anim.Enemy.MegaMax && battle.Anim.Enemy.GiantAge < 0f), "no Mega Dynamax");
        Assert.False(battle.AgainstAMegaMaxTeam);
        Assert.Equal(0f, battle.BossShare(battle.Anim.Enemy));
    }

    [Fact]
    public void ThePipsFollowTheTurnsAndTheOneGoingFades()
    {
        var view = new CombatantView { Dynamaxed = true, DynamaxTurnsLeft = 2, TickFrom = 3, TickAge = 0f };
        var (lit, going, alpha) = ModernUi.BossPips(view);
        Assert.Equal((2, 3), (lit, going));
        Assert.Equal(1f, alpha, 3);
        view.TickAge = BattleAnimator.TickTime;
        (lit, going, alpha) = ModernUi.BossPips(view);
        Assert.Equal(2, lit);
        Assert.True(going == -1 || alpha <= 0.001f);
        // The box at nought is the foe's own, at one the whole
        Assert.Equal(ModernUi.BossFrom, ModernUi.BossRect(0f));
        Assert.Equal(ModernUi.BossTo, ModernUi.BossRect(1f));
    }
}
