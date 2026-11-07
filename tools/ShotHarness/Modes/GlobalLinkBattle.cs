using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Link;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Online;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    // The counter's battle room (plan 07 · O6), the last of the globallink mode: a battle code made (gl10), the room
    // with the team to bring (gl11), the battle on the game's screen at its commands (gl12), waiting for the friend
    // (gl13), its last line (gl14) and the counter after it with the record (gl15). The friend answers through a screen
    // of its own that is never drawn, the first move it may use each turn.
    void BattleRoomShots(GlobalLinkScreen screen, GlobalLinkSession friend, Action<Func<bool>, string> pump)
    {
        // A room picks six: the team is made six for it, and put back afterwards
        var kept = game.Party.Members.ToList();
        foreach (var (name, i) in new[] { "Staraptor", "Luxray", "Floatzel", "Roserade", "Lucario", "Garchomp" }.Select((n, i) => (n, i)))
            if (game.Party.Count < 6) game.Party.Add(new Pokemon(PokemonDatabase.Get(name)!, 40, new BattleRandom(800u + (uint)i)));

        // The friend reads its trade's end and is back at its menu
        friend.Dismiss();
        pump(() => friend.Phase == LinkPhase.Menu, "the friend's menu");
        while (screen.MenuRow != GlobalLinkRow.BattleCode) screen.Move(1);
        screen.Confirm();
        pump(() => screen.Phase == LinkPhase.WaitingForOther, "a battle code");
        Frames(20); Shot("gl10_battle_code");
        friend.EnterCode();
        foreach (char c in screen.Session.Code!) friend.TypeDigit(c - '0');
        friend.SubmitCode();
        pump(() => screen.Phase == LinkPhase.BattleRoom && friend.Phase == LinkPhase.BattleRoom, "the battle room");
        Frames(20); Shot("gl11_battle_room");

        // Both bring their six
        screen.Confirm();
        var friendsTeam = new[] { "Empoleon", "Rapidash", "Gengar", "Tangrowth", "Mamoswine", "Togekiss" }
            .Select((s, i) => RecordedPokemon.Of(new Pokemon(PokemonDatabase.Get(s)!, 40, new BattleRandom(900u + (uint)i)))).ToList();
        friend.BringTeam(new LinkPlayer("Dawn", friendsTeam));
        BattleEngine friendFace = null;
        var friendLines = new List<string>();
        void FriendFrame()
        {
            if (friendFace == null && friend.Battle?.Hello != null) friendFace = BattleEngine.ForLink(friend.Battle);
            if (friendFace == null) return;
            friendFace.Update(1f / 60f);
            Answer(friendFace, friendLines);
        }

        // Until the game's battle opens at its commands; the friend holds off its first answer so the wait is seen
        bool friendWaits = true;
        pump(() =>
        {
            if (!friendWaits) FriendFrame();
            else if (friendFace == null && friend.Battle?.Hello != null) friendFace = BattleEngine.ForLink(friend.Battle);
            else friendFace?.Update(1f / 60f);
            // (the game's own lines read on as a player reads them)
            if (game.Battle is { IsLink: true } reading && reading.IsWaitingForConfirm && !reading.IsLearningMove) reading.ConfirmMessage();
            return game.State == GameState.Battle && game.Battle is { IsLink: true } b && b.AtItsMenu;
        }, "the battle's commands");
        Skip(0.8);
        Shot("gl12_battle_commands");
        if (game.Battle is not { IsLink: true } face)
        {
            Console.WriteLine("!! the battle room's battle never showed");
            return;
        }
        face.SelectMainMenuOption(0);
        face.SelectMove(Strongest(face, face.LinkClient!.Asked!.Options[0].Moves));
        Skip(0.5);
        Shot("gl13_battle_waiting");
        Console.WriteLine($"battle room: waiting '{face.WaitingLine}', the friend's screen {(friendFace == null ? "not yet greeted" : "greeted")}");

        // On to the end: both answer the first move they may use
        friendWaits = false;
        var lines = new List<string>();
        pump(() =>
        {
            FriendFrame();
            if (game.Battle is { IsLink: true } shown && OnLastLine(shown)) return true;
            if (game.Battle is { IsLink: true } going) Answer(going, lines);
            return game.State != GameState.Battle;
        }, "the battle's last line");
        Skip(0.4);
        Shot("gl14_battle_end");
        Console.WriteLine("battle room: the game read " + string.Join(" | ", lines.Take(16)));

        // Read on: back onto the counter, how it went and the record
        pump(() =>
        {
            FriendFrame();
            if (game.Battle is { IsLink: true } shown && shown.IsWaitingForConfirm) shown.ConfirmMessage();
            return game.State == GameState.GlobalLink && screen.Phase == LinkPhase.Battled && screen.Record != null;
        }, "the counter after the battle");
        Frames(30); Shot("gl15_battled");
        Console.WriteLine($"battled: '{screen.Session.LineText}', record {screen.Record?.Wins}-{screen.Record?.Losses}-{screen.Record?.Draws}, saved link record {game.LinkBattles.Wins}-{game.LinkBattles.Losses}");
        screen.Confirm();
        pump(() => screen.Phase == LinkPhase.Menu, "the menu after the battle");

        game.Party.Members.Clear();
        foreach (var p in kept) game.Party.Add(p);
    }
}
