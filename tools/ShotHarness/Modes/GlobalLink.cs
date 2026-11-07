using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using PokemonPlatinum.Server;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Online;
using PokemonPlatinumEngine.Overworld;

partial class Harness
{
    // ---------------------------------------------------------------- The Global Link counter (plan 07 · O5, O7)

    // The Global Link counter (Modes/GlobalLink.cs, not part of `all`): the counter with no server named (gl00), then the
    // online server started in-process on localhost (a database in the out dir, nothing beyond 127.0.0.1), the game's
    // options pointed at it, and a friend's visit beside the game's own: the menu with the friend code (gl01), the keypad
    // half typed (gl02), a code made and waiting (gl03), the room with both offers (gl04), agreed (gl05), the trade done
    // (gl06), and an impossible Pokémon refused with its faults (gl07). The friend code and the link code are the
    // server's own draws, so those texts differ from run to run; everything else is the game's.
    public void GlobalLinkMode()
    {
        At("PokemonCenter2F", 7, 6, Direction.Up);
        Frames(10);

        engine.Settings.OnlineServer = null;
        game.OpenGlobalLink();
        Frames(30); Shot("gl00_no_server");
        var screen = game.GlobalLink;
        Console.WriteLine($"no server: {screen.Phase} '{screen.Session.Line}'");
        screen.Cancel();
        Frames(5);

        string folder = Path.Combine(outDir, "globallink");
        Directory.CreateDirectory(folder);
        var app = LinkHost.Build(new ServerOptions
        {
            Local = true,
            Port = 0,
            Database = Path.Combine(folder, $"link-{Guid.NewGuid():N}.db"),
            IpHashSecret = "the-harness-secret",
            Ticking = false
        });
        app.StartAsync().GetAwaiter().GetResult();
        try
        {
            var address = LinkHost.Address(app);
            engine.Settings.OnlineServer = address.ToString();

            // The friend: a visit of its own on a save of its own, holding a Shinx
            var shinx = new Pokemon(PokemonDatabase.Get("Shinx")!, 12, new BattleRandom(702)) { Id = Guid.Parse("000002be-504b-8001-8000-0000000002be") };
            var friendSave = new HarnessKeeper(SavedPokemonData.FromPokemon(shinx));
            var friend = new GlobalLinkSession(() => new OnlineClient(address, OnlineClient.NewToken(), "Dawn"), friendSave);

            void Pump(Func<bool> done, string what)
            {
                for (int i = 0; i < 1200 && !done(); i++)
                {
                    friend.Update(1f / 60f);
                    Frames(1);
                    Thread.Sleep(5);
                }
                if (!done()) Console.WriteLine($"!! {what} never came: game {screen.Phase} '{screen.Session.Line}', friend {friend.Phase} '{friend.Line}'");
            }

            game.OpenGlobalLink();
            Pump(() => screen.Phase == LinkPhase.Menu && friend.Phase == LinkPhase.Menu, "the welcome");
            Frames(20); Shot("gl01_menu");
            Console.WriteLine($"menu: friend code {screen.Session.FriendCode}, server {screen.ServerShown}");

            // The keypad, four digits in and taken back out again
            screen.Move(1);
            screen.Confirm();
            // (to a key by its row, then along it: a step sideways stays in its row)
            foreach (int key in new[] { 0, 1, 2, 3 })
            {
                while (screen.Key / GlobalLinkScreen.KeypadColumns != key / GlobalLinkScreen.KeypadColumns) screen.Move(1);
                while (screen.Key != key) screen.Sideways(1);
                screen.Confirm();
            }
            Frames(20); Shot("gl02_keypad");
            Console.WriteLine($"typed: '{screen.Session.Typed}'");
            screen.Cancel(); screen.Cancel(); screen.Cancel(); screen.Cancel(); screen.Cancel();
            Pump(() => screen.Phase == LinkPhase.Menu, "the menu again");

            // A code made by the game, typed by the friend
            while (screen.MenuRow != GlobalLinkRow.MakeCode) screen.Move(1);
            screen.Confirm();
            Pump(() => screen.Phase == LinkPhase.WaitingForOther, "a code");
            Frames(20); Shot("gl03_code_made");
            friend.EnterCode();
            foreach (char c in screen.Session.Code!) friend.TypeDigit(c - '0');
            friend.SubmitCode();
            Pump(() => screen.Phase == LinkPhase.Room && friend.Phase == LinkPhase.Room, "the room");

            // Both offer: the game its Turtwig (the first of the team), the friend its Shinx
            screen.Confirm();
            friend.Offer(friendSave.Held.Values.First());
            Pump(() => screen.Session.MayConfirm, "both offers");
            Frames(30); Shot("gl04_offers");
            friend.ConfirmTrade();
            Pump(() => screen.Session.TheirsConfirmed, "the friend's word");
            Frames(10); Shot("gl05_they_agree");
            screen.Confirm();
            Pump(() => screen.Phase == LinkPhase.Traded || game.State != GameState.GlobalLink, "the trade");
            // A trade evolution would play over the counter first; the Shinx doesn't evolve by trade
            Frames(30); Shot("gl06_traded");
            Console.WriteLine($"traded: the team {string.Join(", ", game.Party.Members.Select(p => p.DisplayName))}; the friend holds {string.Join(", ", friendSave.Held.Values.Select(p => p.SpeciesName))}");
            screen.Confirm();
            Frames(5);

            // An impossible Pokémon refused, with its faults
            var bad = game.Party.Members[0];
            int effort = bad.EvAttack;
            bad.EvAttack = 255;
            screen.Confirm();
            Pump(() => !screen.Session.Checking, "the check");
            Frames(20); Shot("gl07_refused");
            Console.WriteLine($"refused: '{screen.Session.Line}' {screen.Session.Details?.Replace('\n', ';')}");
            bad.EvAttack = effort;

            screen.Cancel();
            Pump(() => screen.Phase == LinkPhase.Menu, "the menu after leaving");

            // The battle room (plan 07 · O6): a battle code made by the game, typed by the friend, six brought each side
            BattleRoomShots(screen, friend, Pump);

            screen.Cancel();
            friend.Close();
            Frames(10);
        }
        finally
        {
            engine.Settings.OnlineServer = null;
            try { app.StopAsync().GetAwaiter().GetResult(); } catch (OperationCanceledException) { }
            app.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        // The trade wrote the slot: the harness's save is taken away again, as every mode that saves does
        game.DeleteSave();
    }

    /// <summary>The friend's save as a trade sees it.</summary>
    private sealed class HarnessKeeper : ITradeKeeper
    {
        public Dictionary<Guid, SavedPokemonData> Held { get; } = new();
        public HarnessKeeper(params SavedPokemonData[] pokemon) { foreach (var p in pokemon) Held[p.Id!.Value] = p; }
        public bool Holds(Guid id) => Held.ContainsKey(id);
        public void Swap(Guid given, SavedPokemonData received) { Held.Remove(given); Held[received.Id!.Value] = received; }
    }
}
