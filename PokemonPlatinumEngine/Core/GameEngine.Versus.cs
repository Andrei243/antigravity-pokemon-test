using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Link;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// VERSUS, two people at one machine (plan 13 · V12; rulings, "Two people at one machine"): the battle room's row, two
/// of its teams, and a <see cref="VersusMatch"/> shown as the game's battle with a curtain between the players. Nothing
/// is read from or written to any save: the teams are the room's, fought as copies at level 50.
/// </summary>
public partial class GameEngine
{
    /// <summary>The hot-seat battle under way; null outside one.</summary>
    private VersusMatch? versus;

    /// <summary>The look each player stands in: the boy's and the girl's of the introduction, as the battle room has no game.</summary>
    private static readonly string[] VersusLooks = { "PLAYER", "DAWN" };

    /// <summary>The room's rules for VERSUS: level 50 under the modern rules (the builder's teams may hold any species), as many of each team as the smaller brings, no clauses (the teams are the players' own).</summary>
    public static LinkRoom VersusRoom(int first, int second) => LinkRoom.Default with
    {
        Rules = RulesPreset.Modern,
        Picks = System.Math.Max(1, System.Math.Min(first, second)),
        SpeciesClause = false,
        ItemClause = false
    };

    private void BeginTheVersus(BuiltTeam first, BuiltTeam second)
    {
        var a = new LinkPlayer(Text.Of("versus.first"), BuiltTeams.PartyOf(first).Members.Select(RecordedPokemon.Of).ToList());
        var b = new LinkPlayer(Text.Of("versus.second"), BuiltTeams.PartyOf(second).Members.Select(RecordedPokemon.Of).ToList());
        VersusMatch match;
        try
        {
            // Its own seed, drawn from the game's chance once; everything after it from the seed
            match = new VersusMatch(a, b, VersusRoom(a.Team.Count, b.Team.Count), (uint)Dice.Shared.Next(), Settings.BattleScene, VersusLooks);
        }
        catch (LinkRefusedException e)
        {
            Log.Warn($"VERSUS refused its teams: {e.Message}");
            AudioManager.PlaySound("error");
            flow.BattleRoomScreen.AfterVersus(flow.BattleRoomScreen.Teams);
            return;
        }
        versus = match;
        ShowLinkBattle(match.Faces.SelectMany(f => f.LinkClient!.Own), () => match.Face, KeepTheVersus, () =>
        {
            StartTransition(GameState.BattleRoom, () =>
            {
                LeaveTheLinkBattle();
                versus = null;
                AudioManager.PlayMusic(MusicRole.Title);
                flow.BattleRoomScreen.AfterVersus(BuiltTeams.Read(TeamsFile).Teams);
            });
        });
    }

    /// <summary>A frame of VERSUS beside the shown screen: the other played along, and the curtain lifted by a press.</summary>
    private void KeepTheVersus(float dt)
    {
        if (versus is not { } match) return;
        match.Frame(dt);
        if (match.Curtain != null && InputManager.IsActionPressed(GameAction.Confirm)) PassTheVersus();
    }

    /// <summary>The curtain lifted: the player it waited for has the screen.</summary>
    private void PassTheVersus()
    {
        if (versus is not { Curtain: not null } match) return;
        match.PassTo();
        flow.Battle = match.Face;
        AudioManager.PlaySound("select");
    }

    /// <summary>The curtain over the battle while it is down (style guide, "Battle panels", "The curtain").</summary>
    private void DrawTheVersusCurtain()
    {
        if (versus is { Curtain: { } next } match)
            ModernUi.VersusCurtain(VirtualWidth, VirtualHeight, match.Names[next], match.Names[1 - next]);
    }

    internal sealed partial class Driver
    {
        /// <summary>The hot-seat battle under way (plan 13 · V12).</summary>
        public VersusMatch? Versus => game.versus;

        /// <summary>The curtain lifted as a press of A lifts it.</summary>
        public void PassTheVersus() => game.PassTheVersus();
    }
}
