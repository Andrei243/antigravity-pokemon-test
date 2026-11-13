using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models.Cabinets;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The arcade's cabinets in the game (plan 42 · PS6): a script's <c>gamecorner cabinet match</c> opens the next stage of
/// the match-three on the team's species, <c>gamecorner cabinet picture</c> the next picture of a species seen; what
/// either wins goes into the Coin Case and the records (<see cref="CabinetRecords"/>) as it closes.
/// </summary>
public partial class GameEngine
{
    /// <summary>The cabinets: 0 the match-three, 1 the picture-logic puzzle.</summary>
    public const int MatchCabinet = 0, PictureCabinet = 1;

    /// <summary>The cabinet open now (<see cref="MatchCabinet"/> or <see cref="PictureCabinet"/>).</summary>
    private int cabinetOpen;

    private void OpenCabinet(int which)
    {
        cabinetOpen = which;
        if (which == MatchCabinet)
        {
            var stage = flow.Cabinets.NextStage();
            if (stage == null) return;
            var species = CabinetDatabase.BoardSpecies(flow.Party.Members);
            if (!IsHeadless)
            {
                PokemonSprites.Request(stage.Foe);
                foreach (var s in species) PokemonSprites.Request(s);
            }
            flow.MatchThreeScreen.Open(stage, species);
        }
        else flow.PictureLogicScreen.Open(PicturesToOffer());
        flow.Journal.Tell(new Models.JournalEvent(Models.JournalEventKind.GameCorner));
        flow.State = GameState.Cabinet;
    }

    /// <summary>The species the picture cabinet looks through: every one the Pokédex has seen, in National order, but those solved already.</summary>
    internal IEnumerable<string> PicturesToOffer() =>
        flow.Pokedex.SeenSpecies.OrderBy(n => n)
            .Select(n => PokemonDatabase.GetByDex(n)?.Name)
            .Where(name => name != null && !flow.Cabinets.HasSolved(name))
            .Select(name => name!);

    private void UpdateCabinet(float dt)
    {
        var match = flow.MatchThreeScreen;
        var picture = flow.PictureLogicScreen;
        // A screen closed between frames (by a tool or the pointer) is taken up as one closed by its own update
        if (cabinetOpen == MatchCabinet)
        {
            match.Update(dt);
            if (match.IsActive) return;
            if (match.Played && match.Board is { } board)
            {
                flow.Cabinets.RecordStage(board.Stage.Id, board.Score, board.Cleared);
                if (match.Outcome > 0) flow.Pastimes.Coins.Add(match.Outcome);
            }
            flow.ScriptAnswer = match.Outcome;
        }
        else
        {
            picture.Update(dt);
            if (picture.IsActive) return;
            if (picture.Phase == UI.PicturePhase.Solved && picture.Puzzle is { } puzzle)
            {
                flow.Cabinets.RecordPicture(puzzle.Species, puzzle.Size);
                if (picture.Outcome > 0) flow.Pastimes.Coins.Add(picture.Outcome);
            }
            flow.ScriptAnswer = picture.Outcome;
        }
        flow.State = GameState.Overworld;
    }
}
