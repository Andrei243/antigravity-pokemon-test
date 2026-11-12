using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.StageShow;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Plan 42's pastimes in the game (PS1 to PS3): the theatre's dressing room and stage, where their rules
/// (<see cref="ShowRecords"/>, <see cref="ShowRun"/>, GPU-free) meet their screens, and the card-flip tables of the Game
/// Corners HeartGold played them at (<see cref="CardFlipGame"/>).
/// </summary>
public partial class GameEngine
{
    private void OpenDressingRoom()
    {
        flow.ScriptAnswer = 0;
        // The slots a body offers once its model is built (every slot while it is being made)
        flow.DressingRoomScreen.SlotsOf = pokemon => PokemonModels.TryGet(pokemon.LookName, out var model) ? PropAnchors.SlotsOf(model) : null;
        foreach (var pokemon in flow.Party.Members.Where(p => !p.IsEgg)) PokemonModels.Request(pokemon.LookName);
        flow.DressingRoomScreen.Open(flow.Party.Members, flow.StageShow);
        if (flow.DressingRoomScreen.IsActive) flow.State = GameState.DressingRoom;
    }

    private void UpdateDressingRoom(float dt)
    {
        flow.DressingRoomScreen.Update(dt);
        if (flow.DressingRoomScreen.IsActive) return;
        flow.ScriptAnswer = flow.DressingRoomScreen.Outcome;
        if (!IsHeadless) flow.DressingRoomScreen.Free();
        flow.State = GameState.Overworld;
    }

    private void OpenStageShow()
    {
        flow.ScriptAnswer = 0;
        foreach (var show in StageShowDatabase.All)
            foreach (var member in show.Cast) PokemonModels.Request(member.Species);
        foreach (var pokemon in flow.Party.Members.Where(p => !p.IsEgg)) PokemonModels.Request(pokemon.LookName);
        flow.StageShowScreen.Open(StageShowDatabase.All, flow.Party.Members, flow.StageShow, fieldRandom.Next(), Settings.TimingAssist);
        if (flow.StageShowScreen.IsActive) flow.State = GameState.StageShow;
    }

    private void UpdateStageShow(float dt)
    {
        var screen = flow.StageShowScreen;
        screen.Update(dt);
        if (screen.IsActive) return;
        flow.ScriptAnswer = screen.Outcome;
        if (!IsHeadless) screen.Free();
        flow.State = GameState.Overworld;
        // The show's song gives way to the lobby's
        if (screen.Run != null) PlayFieldMusic();
    }

    // ------------------------------------------------------------------ the card-flip tables (plan 42 · PS3)

    private void OpenCardFlip()
    {
        flow.CardFlipScreen.Open(flow.Pastimes.Coins.Coins, fieldRandom);
        flow.State = GameState.CardFlip;
    }

    private void UpdateCardFlip(float dt)
    {
        flow.CardFlipScreen.Update(dt);
        if (flow.CardFlipScreen.IsActive) return;
        // The coins come back to the case, whatever the sitting did with them
        if (flow.CardFlipScreen.Game != null) flow.Pastimes.Coins.Set(flow.CardFlipScreen.Coins);
        flow.State = GameState.Overworld;
    }
}
