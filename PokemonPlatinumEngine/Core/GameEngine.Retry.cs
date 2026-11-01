using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// A lost battle fought again, and a battle suspended (plan 12 · Q25; rulings, "A lost battle fought again, and a
/// battle suspended"). As a battle the field or a scene begins is made, the game keeps what it stood as
/// (<see cref="BuildSave"/>) beside the setup the battle was made from, and the battle itself keeps how it began
/// (<see cref="BattleEngine.Opening"/>, plan 08 · P10's file). After a loss, with the options' RETRY BATTLES on, the
/// question asks TRY AGAIN or GIVE UP before anything of the loss happens: TRY AGAIN puts the battle's own Pokémon, the
/// bag and the money back as they went in and fights the same foes, made afresh from the file, on a fresh seed from
/// <see cref="Dice"/>; a scene's battle is fought again while its script waits on the outcome. The close button's
/// SUSPEND AND QUIT writes the game as the battle began, the file and the record so far beside the slot
/// (<see cref="BattleSuspension"/>); CONTINUE on the slot loads it, stands the battle up again on its own seed, plays
/// the record back at once with nothing shown (<see cref="BattleEngine.CatchUp"/>) and deletes the file.
/// </summary>
public partial class GameEngine
{
    private readonly RetryPrompt retryPrompt = new();

    // The battle in hand as it began: the setup it was made from, the game as it stood, and a rod's hook
    private BattleSetup? battleSetup;
    private SaveData? battleSnapshot;
    private bool battleHooked;
    // The question was asked of the battle in hand (and answered GIVE UP, if it is still over)
    private bool retryAsked;

    // The slots' suspended battles as the title read them, by whom each is against
    private Dictionary<int, string> titleBattles = new();

    // The battle the start above is of: a battle made any other way (the Frontier's, one watched) has none
    private BattleEngine? keptFor;

    /// <summary>
    /// Makes the battle the field or a scene begins, keeping what it begins from (plan 12 · Q25): its setup, and the game
    /// as it stands. The battle keeps its own opening; nothing is written.
    /// </summary>
    private void MakeTheBattle(BattleSetup setup)
    {
        battleSetup = setup;
        battleSnapshot = BuildSave();
        battleHooked = flow.Hooked;
        retryAsked = false;
        flow.Battle = keptFor = new BattleEngine(setup);
    }

    /// <summary>Forgets the battle in hand's start: a game entered, the field back.</summary>
    private void ForgetTheBattlesStart()
    {
        battleSetup = null;
        battleSnapshot = null;
        keptFor = null;
        retryAsked = false;
    }

    // Whether what is kept is the battle on screen's
    private bool KeptForThis(BattleEngine battle) => battleSnapshot != null && battleSetup != null && keptFor == battle;

    /// <summary>Whether the battle just lost may be fought again (<see cref="BattleRetry.Offered"/>).</summary>
    private bool RetryOffered(BattleEngine battle) => BattleRetry.Offered(Settings.RetryBattles, battle.Result, battle.Kind,
        KeptForThis(battle), flow.Runner.IsRunning && flow.BattleMayBeLost, battle.IsFrontier, battle.Watching,
        battle.OnlyBall != null, flow.Run.Nuzlocke || battleSetup?.Nuzlocke != null, battleSetup?.Restrictions);

    /// <summary>
    /// A battle over: asks TRY AGAIN or GIVE UP where it may be fought again and hasn't been asked (true: the question is
    /// up, and the battle's end waits for it).
    /// </summary>
    private bool AskToTryAgain(BattleEngine battle)
    {
        if (retryAsked || !RetryOffered(battle)) return false;
        retryAsked = true;
        retryPrompt.Open(ShownFoe(battle.Opening));
        return true;
    }

    /// <summary>The question answered: the battle fought again, or the loss gone on with as always.</summary>
    internal void AnswerRetry(RetryChoice choice)
    {
        if (choice == RetryChoice.TryAgain) TryTheBattleAgain();
        // GIVE UP: the battle's end, which waited, goes on next frame as a loss (retryAsked keeps it from asking again)
    }

    /// <summary>
    /// The battle fought again from its start: behind the dark, the battle's own Pokémon, the bag and the money as the
    /// battle began, and a new battle of the same foes made afresh from its file, on a fresh seed.
    /// </summary>
    private void TryTheBattleAgain()
    {
        if (flow.Battle is not { } lost || battleSetup is not { } setup || battleSnapshot is not { } snapshot) return;
        var opening = lost.Opening;
        flow.Statistics.Count(Statistics.Retries);
        Log.Info($"The battle against {opening.Foe} was lost and is fought again.");
        StartTransition(GameState.Battle, () =>
        {
            PutBackAsTheBattleBegan(setup, opening, snapshot);
            var fresh = opening.ToSetup();
            // The trainers' looks on the platform are the people's who carry them
            foreach (var (made, was) in fresh.Trainers.Zip(setup.Trainers)) made.Look = was.Look;
            var again = setup.Again(fresh.WildPokemon, fresh.Trainers, fresh.Partner ?? setup.Partner, fresh.FirstTrainerPokemon, random: null);
            AwaitModels(PrepareModels(again.WildPokemon.Concat(again.Trainers.SelectMany(t => t.Party.Members))));
            if (again.Trainers.Count > 0) AudioManager.PlayMusic(MusicDirector.BattleRole(again.Trainers.Select(t => t.TrainerClass)), immediate: true);
            else if (again.WildPokemon.Count > 0) AudioManager.PlayWildBattleMusic(again.WildPokemon[0].Species);
            flow.Battle = keptFor = new BattleEngine(again);
            battleSetup = again;
            retryAsked = false;
        });
    }

    /// <summary>
    /// What a battle may have changed, put back as it went in: each Pokémon the battle fought with as the battle's file
    /// has it (the same objects, by their ids, so whoever holds one still does), in the order it went in; the bag and
    /// its order; the money.
    /// </summary>
    private void PutBackAsTheBattleBegan(BattleSetup setup, BattleRecordFile opening, SaveData snapshot)
    {
        var rules = Ruleset.Of(opening.Rules);
        var side = setup.PlayerParty;
        var before = side.Members.ToList();
        var back = new List<Pokemon>();
        for (int i = 0; i < opening.Player.Count; i++)
        {
            var was = opening.Player[i];
            var p = before.FirstOrDefault(m => m.Id != Guid.Empty && m.Id == (was.Pokemon.Id ?? Guid.Empty)) ?? (i < before.Count ? before[i] : null);
            if (p == null) continue;
            p.CopyStateFrom(was.ToPokemon(rules));
            back.Add(p);
        }
        if (back.Count == before.Count && back.Distinct().Count() == back.Count)
        {
            int limit = side.Limit;
            side.Clear();
            foreach (var p in back) side.Add(p);
            side.Limit = limit;
        }

        flow.Bag.Clear();
        foreach (var item in snapshot.Inventory)
            if (ItemDatabase.Get(item.ItemName) is { } data) flow.Bag.AddItem(data, item.Quantity);
        foreach (var (pocketName, orderName) in snapshot.BagOrder)
            if (Enum.TryParse<ItemPocket>(pocketName, out var pocket) && Enum.TryParse<BagOrder>(orderName, out var order)) flow.Bag.SetOrder(pocket, order);
        flow.Money = snapshot.Money;
    }

    /// <summary>Whom a battle is against as the question and the title's card name them: the first trainer, or the first wild Pokémon in the language shown.</summary>
    private static string ShownFoe(BattleRecordFile file) => file.Trainers.Count > 0 ? file.Foe
        : file.Wild.Count > 0 ? LocalNames.Species(file.Wild[0].Pokemon.SpeciesName) : "";

    // ------------------------------------------------------------------ suspending

    /// <summary>Whether the close button may offer SUSPEND AND QUIT now (<see cref="BattleRetry.Suspendable"/>).</summary>
    private bool CanSuspendNow => gameStarted && flow.State == GameState.Battle && flow.Battle is { } b && !retryPrompt.IsOpen
        && BattleRetry.Suspendable(KeptForThis(b), flow.Runner.IsRunning, b.Result, b.Kind, b.IsFrontier, b.Watching,
            b.OnlyBall != null, flow.Run.Nuzlocke || battleSetup?.Nuzlocke != null, b.RecordFile(GameClock.Today) != null,
            System.IO.File.Exists(SavePaths.Current.Slot(flow.SaveSlot)));

    /// <summary>
    /// SUSPEND AND QUIT: the game as the battle began, the battle's file closed on its record so far, and what the field
    /// needs to stand it up again, written beside the slot. False where it couldn't be (the game then stays open).
    /// </summary>
    private bool SuspendTheBattle()
    {
        if (flow.Battle is not { } battle || battleSnapshot is not { } snapshot || battleSetup is not { } setup) return false;
        if (battle.RecordFile(GameClock.Today) is not { } file) return false;
        var suspension = new BattleSuspension
        {
            Save = snapshot,
            Battle = file.Serialize(),
            Place = setup.Place,
            TrainerPeople = flow.BattleTrainers.Select(n => n.Key).OfType<string>().ToList(),
            Hooked = battleHooked,
            Restrictions = setup.Restrictions,
            Foe = ShownFoe(file)
        };
        bool written = suspension.WriteFor(SavePaths.Current, flow.SaveSlot);
        if (written) Log.Info($"The battle against {file.Foe} was suspended beside slot {flow.SaveSlot}, after {file.Answers.Count} answers.");
        else Log.Warn($"The battle couldn't be suspended beside slot {flow.SaveSlot}.");
        return written;
    }

    /// <summary>
    /// The slots' suspended battles, read as the title reads the slots: each that can be taken up by whom it is against,
    /// and a notice (and the file taken away) for each that can't (another data version, cut short, or a slot with no
    /// save to go back to).
    /// </summary>
    private List<TitleNotice?> ReadTheSuspensions(SavePaths paths)
    {
        titleBattles = new Dictionary<int, string>();
        var notices = new List<TitleNotice?>();
        foreach (var slot in titleSlots)
        {
            if (!BattleSuspension.Exists(paths, slot.Number)) continue;
            var (suspension, _, refused) = BattleSuspension.ReadFor(paths, slot.Number);
            if (suspension != null && slot.Save != null)
            {
                titleBattles[slot.Number] = suspension.Foe;
                continue;
            }
            Log.Warn($"The battle suspended beside slot {slot.Number} can't be taken up ({refused}); it was taken away.");
            BattleSuspension.Delete(paths, slot.Number);
            notices.Add(TitleNotice.ForSuspension(slot.Number));
        }
        return notices;
    }

    /// <summary>
    /// CONTINUE on a slot with a battle beside it: the game as the battle began, the battle stood up again from its file
    /// on its own seed and caught up with its record at once, the file deleted. False where there is none, or it can't
    /// be taken up (the slot's own save is then loaded as ever).
    /// </summary>
    private bool TakeUpTheBattle(int slot)
    {
        var paths = SavePaths.Current;
        if (!BattleSuspension.Exists(paths, slot)) return false;
        var (suspension, file, refused) = BattleSuspension.ReadFor(paths, slot);
        // Played on once: the file goes whatever comes of it, so nothing is rolled twice
        BattleSuspension.Delete(paths, slot);
        titleBattles.Remove(slot);
        if (suspension == null || file == null)
        {
            Log.Warn($"The battle suspended beside slot {slot} couldn't be taken up ({refused}).");
            return false;
        }

        flow.SaveSlot = slot;
        ApplySaveData(suspension.Save);
        flow.Journal.TakenUp(GameClock.Today, PlaceName());
        EnterGame();

        var rules = file.ToSetup();
        // The player's side: the team the save brought back, each as the battle's file has it (a sleep's turns, a bad
        // poisoning's count, which a save doesn't keep); a battle of one Pokémon put forward is that one alone
        var side = flow.Party;
        var ids = file.Player.Select(r => r.Pokemon.Id ?? Guid.Empty).ToList();
        if (ids.Count != flow.Party.Count || !ids.SequenceEqual(flow.Party.Members.Select(p => p.Id)))
        {
            side = new Party();
            foreach (var id in ids) if (flow.Party.Members.FirstOrDefault(p => p.Id == id) is { } member) side.Add(member);
        }
        for (int i = 0; i < file.Player.Count && i < side.Count; i++) side.Members[i].CopyStateFrom(file.Player[i].ToPokemon(rules.Rules!));

        var setup = new BattleSetup
        {
            PlayerParty = side,
            Inventory = flow.Bag,
            Pokedex = flow.Pokedex,
            PcStorage = flow.Boxes,
            Place = suspension.Place,
            Format = file.Format,
            WildPokemon = rules.WildPokemon,
            Trainers = rules.Trainers,
            FirstTrainerPokemon = rules.FirstTrainerPokemon,
            Random = rules.Random,
            Rules = rules.Rules,
            Conditions = rules.Conditions,
            Kind = file.Kind,
            Partner = rules.Partner,
            CannotFlee = file.CannotFlee,
            FirstBattle = file.FirstBattle,
            SpecialBalls = file.SpecialBalls,
            MoveHints = MoveHintsOn,
            TrainerLines = TrainerLinesOn,
            NoBattleItems = file.NoBattleItems,
            OffersSwitch = file.OffersSwitch,
            MegaMaxDen = file.MegaMaxDen,
            Difficulty = file.Difficulty,
            RunLevelCap = file.RunLevelCap,
            CrossedExp = file.CrossedExp,
            Restrictions = suspension.Restrictions,
            Scene = Settings.BattleScene
        };
        AwaitModels(PrepareModels(setup.WildPokemon.Concat(setup.Trainers.SelectMany(t => t.Party.Members))
            .Concat(setup.Partner?.Party.Members ?? Enumerable.Empty<Pokemon>())));
        battleSetup = setup;
        battleSnapshot = suspension.Save;
        battleHooked = suspension.Hooked;
        retryAsked = false;
        var battle = new BattleEngine(setup);
        if (!battle.CatchUp(file.Record.Answers))
        {
            Log.Warn($"The battle suspended beside slot {slot} didn't play back as it was fought; the game goes on from where it began.");
            ForgetTheBattlesStart();
            ShowNotification(Text.Of("title.notice.suspension.heading", ("slot", slot)));
            return true;
        }
        flow.Battle = keptFor = battle;
        flow.BattleTrainers.Clear();
        foreach (var key in suspension.TrainerPeople)
            if (flow.Map.Everyone.FirstOrDefault(n => n.Key == key && n.TrainerData != null) is { } person) flow.BattleTrainers.Add(person);
        eyeThemePlaying = false;
        if (setup.Trainers.Count > 0) AudioManager.PlayMusic(MusicDirector.BattleRole(setup.Trainers.Select(t => t.TrainerClass)), immediate: true);
        else if (setup.WildPokemon.Count > 0) AudioManager.PlayWildBattleMusic(setup.WildPokemon[0].Species);
        if (suspension.Hooked) battleRenderer.SetArena(BattleArena.Water, trees: flow.Map.TreesAt(flow.Player.GridX, flow.Player.GridY));
        else battleRenderer.SetArena(flow.Map, flow.Player.GridX, flow.Player.GridY);
        flow.OpenOn(GameState.Battle);
        Log.Info($"The battle against {file.Foe} was taken up from slot {slot}, {file.Answers.Count} answers played back.");
        return true;
    }
}
