using System;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The World Tournament in the game (plan 14 · B11): where its rules (<see cref="Models.Tournament"/>) meet the screen
/// of its events and bracket (<see cref="TournamentScreen"/>, <see cref="GameState.Tournament"/>). Everything else is
/// the Frontier's frame (<c>GameEngine.Frontier.cs</c>): the three are entered on the team's screen, a match is one of
/// the Frontier's battles with the tournament's own songs, and the opponent walks onto the stage in their own look.
/// </summary>
public partial class GameEngine
{
    public const string TournamentHallMap = "WorldTournamentHall";

    /// <summary>The events to choose from, or the bracket to look at; the script waits.</summary>
    private void OpenTournament(bool events)
    {
        flow.ScriptAnswer = 0;
        if (events) flow.TournamentScreen.OpenEvents(TournamentDatabase.Events, flow.Frontier.Tournament);
        else if (flow.Tournament is { } t) flow.TournamentScreen.OpenBracket(t);
        else return;
        flow.State = GameState.Tournament;
    }

    private void UpdateTournament(float dt)
    {
        flow.TournamentScreen.Update(dt);
        if (flow.TournamentScreen.IsActive) return;
        flow.ScriptAnswer = flow.TournamentScreen.Answer;
        flow.State = GameState.Overworld;
    }

    /// <summary>The round's opponent stands on the stage as the hall's <c>opponent</c>, in their own look and name.</summary>
    private void StandTournamentOpponent()
    {
        if (flow.Tournament?.Opponent is not { } opponent || flow.Map.FindPerson(TowerOpponentPeople[0]) is not { } person) return;
        person.NpcType = opponent.Look;
        person.Name = opponent.Title;
        KeepCharactersNear(trim: false);
    }

    private sealed partial class FieldHost
    {
        public Models.Tournament? Tournament { get => game.flow.Tournament; set => game.flow.Tournament = value; }
    }
}
