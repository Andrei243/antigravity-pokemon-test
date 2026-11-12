using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Locked hearts in the game (plan 44 · SD1; <see cref="HeartLock"/>, <c>docs/mechanics/rulings.md</c>, "Locked hearts"):
/// the Reclaimer carried into a trainer's battle, the locked Pokémon reclaimed in it taken into the team or the PC as it
/// ends, those knocked out noted as missed (the courier carries them back), and the team's own locks lowered by what they
/// do: a battle and its knock-outs, the steps, the camp, the spring. A heart whose gauge has emptied opens once the field
/// is free (<see cref="KeepTheHearts"/>).
/// </summary>
public partial class GameEngine
{
    /// <summary>The bag holds the Reclaimer.</summary>
    internal bool CarriesTheReclaimer => ItemDatabase.Get(HeartLock.ReclaimerItem) is { } item && flow.Bag.GetQuantity(item) > 0;

    /// <summary>The steps toward the next fall of the team's locks.</summary>
    private int lockSteps;

    /// <summary>The openings since the game began or loaded, for whoever shows them (the opening's scene, a test).</summary>
    internal List<HeartOpening> HeartsOpened { get; } = new();

    /// <summary>
    /// What a battle leaves of Lockwork's locked Pokémon: each reclaimed comes home (an id, the Pokédex, the team or
    /// the PC, the Nuzlocke's gifts clause), each knocked out is missed, and each locked Pokémon of the team that fought
    /// has its gauge lowered for the battle and its knock-outs.
    /// </summary>
    private void HeartsAfterBattle(BattleEngine battle)
    {
        foreach (var p in battle.Reclaimed) TakeInTheReclaimed(p);
        foreach (var trainer in battle.Trainers)
            foreach (var member in trainer.Party.Members)
                if (member.Lock is { } missed && member.IsFainted && !battle.Reclaimed.Contains(member)) flow.Hearts.Miss(missed.Id);
        foreach (var (p, fall) in battle.LockFalls()) LowerTheLock(p, fall);
    }

    /// <summary>A locked Pokémon reclaimed comes home: given an id, seen and caught, met here, onto the team or into the PC.</summary>
    private void TakeInTheReclaimed(Pokemon p)
    {
        if (p.Lock is { } locked) flow.Hearts.Reclaim(locked.Id);
        p.CurrentHP = System.Math.Max(1, p.CurrentHP);
        if (p.Status == StatusCondition.Faint) p.Status = StatusCondition.None;
        flow.Pokedex.RegisterSeen(p);
        flow.Pokedex.RegisterCaught(p);
        // The Nuzlocke's gifts clause: a reclaim is the place's catch (plan 13 · V4)
        Nuzlocke.Gift(flow.Run, flow.RunState, Nuzlocke.PlaceKeyOf(flow.Map, flow.Player.GridX, flow.Player.GridY));
        if (!flow.Party.Add(p)) flow.Boxes.Store(p);
        GiveIds();
    }

    /// <summary>Lowers a locked Pokémon's gauge; an emptied one waits for the field to be free to open.</summary>
    internal void LowerTheLock(Pokemon p, int by)
    {
        HeartLock.Lower(p, by);
    }

    /// <summary>Every 128 steps the team's locked Pokémon have their gauges lowered (plan 44 · SD1).</summary>
    private void HeartsOnStep()
    {
        if (!flow.Party.Members.Any(p => p.Lock != null)) return;
        if (++lockSteps < HeartLock.StepsPerFall) return;
        lockSteps = 0;
        foreach (var p in flow.Party.Members.Where(p => p.Lock != null).ToList()) LowerTheLock(p, HeartLock.Fall.Steps);
    }

    /// <summary>The spring's rite (a script's <c>heart spring</c>): once a day, every locked heart of the team is lowered. False when done already today.</summary>
    internal bool SpringRite()
    {
        int today = ApricornBox.DayOf(GameClock.Today);
        if (flow.Hearts.SpringDay == today) return false;
        flow.Hearts.SpringDay = today;
        foreach (var p in flow.Party.Members.Where(p => p.Lock != null).ToList()) LowerTheLock(p, HeartLock.Fall.Spring);
        return true;
    }

    /// <summary>
    /// The hearts whose gauges have emptied open once the field is free, one at a time: the lock taken off, the held EXP
    /// given up to the level cap, the record kept. True while one is being opened.
    /// </summary>
    private bool KeepTheHearts()
    {
        if (!CanSaveNow) return false;
        var p = flow.Party.Members.FirstOrDefault(p => p.Lock is { IsOpen: true });
        if (p == null) return false;
        OpenTheHeart(p);
        return true;
    }

    /// <summary>A heart opens: what <see cref="HeartLock.Open"/> gives, written down, and the opening shown.</summary>
    internal HeartOpening OpenTheHeart(Pokemon p)
    {
        var opening = HeartLock.Open(p, CurrentLevelCap());
        flow.Hearts.Open(opening.LockId);
        HeartsOpened.Add(opening);
        ShowTheOpening(opening);
        return opening;
    }

    /// <summary>
    /// The opening shown (plan 44 · SD2): the field fades into its scene, the Pokémon's model made while it is dark; a game
    /// with no screen only says so.
    /// </summary>
    private void ShowTheOpening(HeartOpening opening)
    {
        if (IsHeadless)
        {
            ShowNotification(Text.Of("hearts.opened", ("pokemon", opening.Pokemon.DisplayName)));
            return;
        }
        var p = opening.Pokemon;
        string look = PokemonLook.Of(p.ModelName, p.Gender == Gender.Female, p.IsShiny);
        flow.WantModel(look);
        StartTransition(GameState.HeartOpening, () =>
        {
            AwaitModels(new[] { look });
            flow.HeartOpeningScreen.Begin(opening);
        });
    }

    /// <summary>The opening's scene is over: the field comes back, and a level's moves are asked as the bag's Rare Candy asks them.</summary>
    private void FinishHeartOpening()
    {
        var opening = flow.HeartOpeningScreen.Opening;
        StartTransition(GameState.Overworld, PlayFieldMusic);
        if (opening != null && opening.Wanted.Count > 0) ShowNotification(Text.Of("hearts.movesWanted", ("pokemon", opening.Pokemon.DisplayName)));
    }
}
