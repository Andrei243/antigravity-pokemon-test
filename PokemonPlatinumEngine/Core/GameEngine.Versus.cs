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

    /// <summary>And in a battle of four (plan 13 · V21), in the players' order: each side's two.</summary>
    private static readonly string[] FourLooks = { "PLAYER", "DAWN", "ace_trainer_m", "ace_trainer_f" };

    /// <summary>
    /// The room's rules for VERSUS: level 50 under the modern rules (the builder's teams may hold any species), as many of
    /// each team as the smallest brings, no clauses (the teams are the players' own). Given four teams' sizes it is a
    /// double room, for a battle of four (plan 13 · V21).
    /// </summary>
    public static LinkRoom VersusRoom(params int[] sizes) => LinkRoom.Default with
    {
        Rules = RulesPreset.Modern,
        Format = sizes.Length == 4 ? BattleFormat.Double : BattleFormat.Single,
        Picks = System.Math.Max(1, sizes.Min()),
        SpeciesClause = false,
        ItemClause = false
    };

    /// <summary>What each player is called, in their order.</summary>
    internal static string VersusName(int player) => Text.Of(player switch { 0 => "versus.first", 1 => "versus.second", 2 => "versus.third", _ => "versus.fourth" });

    /// <summary>Two teams are two players' battle; four are two sides of two, the first two against the last two.</summary>
    private void BeginTheVersus(params BuiltTeam[] teams)
    {
        var players = teams.Select((t, i) => new LinkPlayer(VersusName(i), BuiltTeams.PartyOf(t).Members.Select(RecordedPokemon.Of).ToList())).ToArray();
        var room = VersusRoom(players.Select(p => p.Team.Count).ToArray());
        VersusMatch match;
        try
        {
            // Its own seed, drawn from the game's chance once; everything after it from the seed
            uint seed = (uint)Dice.Shared.Next();
            match = players.Length == 4
                ? VersusMatch.OfFour(players[0], players[1], players[2], players[3], room, seed, Settings.BattleScene, FourLooks)
                : new VersusMatch(players[0], players[1], room, seed, Settings.BattleScene, VersusLooks);
        }
        catch (LinkRefusedException e)
        {
            Log.Warn($"VERSUS refused its teams: {e.Message}");
            AudioManager.PlaySound("error");
            flow.BattleRoomScreen.AfterVersus(flow.BattleRoomScreen.Teams);
            return;
        }
        versus = match;
        ShowLinkBattle(match.Faces.SelectMany(f => f.LinkClient!.Own.Concat(f.LinkClient.PartnerOwn)), () => match.Face, KeepTheVersus, () =>
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

    /// <summary>
    /// Whose pad the next frame reads (plan 13 · V20, V21): in a battle of four the seat of whoever's turn it is (behind
    /// the curtain, whoever it waits for), else a friend's while a friend chooses, else the player's.
    /// </summary>
    private int SeatNow() => flow.State != GameState.Battle ? 0
        : versus is { Four: true } four ? four.Seat
        : flow.Battle?.FriendChoosing == true && versus == null ? 1 : 0;

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
