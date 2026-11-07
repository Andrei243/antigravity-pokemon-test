using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim.Link;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Online;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Link battles in the game (plan 28 · I3's screen, plan 07 · O6's game half): a battle screen fed from a link client
/// (<see cref="BattleEngine.ForLink"/>) shown as the game's battle, with whatever keeps the link going beside it every
/// frame, and its end handed back to whoever began it, never to the field's own ending (no money, no faint, no
/// evolution: nothing of the save changes but what the one who began it writes).
/// </summary>
public partial class GameEngine
{
    // What keeps the link of the battle shown going each frame (the counter's session, a hot-seat's server), and what
    // the battle's end comes back to; null outside a link battle
    private Action? linkBeside;
    private Action? linkOver;
    private bool linkEnding;

    /// <summary>
    /// Shows a link client's battle as the game's battle behind a fade: its models made ready, the battle's music, an
    /// indoor stage; <paramref name="beside"/> runs every frame of it, <paramref name="over"/> once it has shown its end.
    /// </summary>
    private void ShowLinkBattle(LinkClient client, Action beside, Action over, string? foeLook = null)
    {
        var models = client.Own.Concat(client.FoeStandIns).Select(p => p.LookName).Distinct().ToList();
        foreach (string name in models) flow.WantModel(name);
        linkBeside = beside;
        linkOver = over;
        linkEnding = false;
        eyeThemePlaying = false;
        AudioManager.PlayMusic(MusicDirector.BattleRole(new[] { LinkSetup.PlayerClass }), immediate: true);
        StartTransition(GameState.Battle, () =>
        {
            AwaitModels(models);
            flow.Battle = BattleEngine.ForLink(client, Settings.BattleScene, PlayerIdentity.Character, foeLook);
            battleRenderer.SetArena(BattleArena.Indoors);
            flow.BattleTrainers.Clear();
        });
    }

    /// <summary>
    /// A frame of a link battle: the link kept going beside it, and its end (or a link that died under it) handed to
    /// whoever began it. True when the frame was a link battle's.
    /// </summary>
    private bool KeepTheLinkBattle()
    {
        if (flow.Battle is not { IsLink: true } face || linkOver == null) return false;
        linkBeside?.Invoke();
        if (linkEnding || !face.IsBattleOver && !LinkDied) return true;
        linkEnding = true;
        AudioManager.LowHp = false;
        AudioManager.Giant = false;
        var over = linkOver;
        over();
        return true;
    }

    /// <summary>The counter's link died under its battle: the battle can't go on, and the screen goes back to the counter.</summary>
    private bool LinkDied => flow.State == GameState.Battle && flow.GlobalLinkScreen.IsActive && flow.GlobalLinkScreen.Session.Phase == LinkPhase.Failed;

    /// <summary>Behind the fade back: the battle and its models let go, the place's music again.</summary>
    private void LeaveTheLinkBattle()
    {
        flow.Battle = null;
        linkBeside = null;
        linkOver = null;
        linkEnding = false;
        PokemonModels.Trim(flow.Party.Members.Select(p => p.LookName));
    }

    // ---------------------------------------------------------------- the Global Link counter's battle room (plan 07 · O6)

    // The record the counter took last, so each is written once
    private BattledRecord? recordTaken;
    private sealed record BattledRecord(BattleRecorded Recorded);

    /// <summary>
    /// A frame of the counter beside its battle room: once the room greets the player with the other's preview, the
    /// battle is shown; once the server counts it, the record is written into the save (and the save written).
    /// </summary>
    private void KeepTheCountersBattle()
    {
        var screen = flow.GlobalLinkScreen;
        var session = screen.Session;
        TakeTheBattleRecord();
        if (flow.State != GameState.GlobalLink || flow.Battle != null || linkOver != null) return;
        if (session.Phase == LinkPhase.Battling && session.Battle is { Hello: not null } client)
            ShowLinkBattle(client, beside: () =>
            {
                session.Update(0f);
                TakeTheBattleRecord();
            }, over: () =>
            {
                session.FinishBattle();
                StartTransition(GameState.GlobalLink, () =>
                {
                    LeaveTheLinkBattle();
                    PlayFieldMusic();
                });
            });
    }

    /// <summary>The server's count of the battle, written once into the save's record of link battles (plan 07 · O2, decision 9).</summary>
    private void TakeTheBattleRecord()
    {
        var screen = flow.GlobalLinkScreen;
        if (screen.Session.Recorded is not { } recorded || ReferenceEquals(recordTaken?.Recorded, recorded)) return;
        recordTaken = new BattledRecord(recorded);
        OnlineClient.Record(flow.LinkBattles, recorded);
        screen.Record = flow.LinkBattles.With(recorded.OtherFriendCode);
        bool saved = WriteSave();
        Log.Info($"A link battle at the Global Link counter: {recorded.Outcome} ({recorded.How}); {(saved ? "the game was saved" : "the game couldn't be saved")}.");
    }

    internal sealed partial class Driver
    {
        /// <summary>The link battle on screen's link kept going and its end handed back, as a frame of the game does (a tool driving a battle it shows by hand).</summary>
        public bool KeepTheLinkBattle() => game.KeepTheLinkBattle();
    }
}
