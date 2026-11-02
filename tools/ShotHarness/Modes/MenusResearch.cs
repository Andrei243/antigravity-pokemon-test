using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    // ---------------------------------------------------------------- the Pokédex's research (plan 15 · E1, E2)

    // The last of `menus`: a wild battle with nothing drawn, which tells the Pokédex's research what it shows (a
    // Starly met, its move used, it beaten), then the TASKS page of that record (26zg), the list with a level on its
    // rows (26zh), a record whole at level 10 (26zi) and the research certificate (26zj). The research is put back
    // as it was after.
    void MenusResearch()
    {
        var pokedex = game.Pokedex;
        var research = pokedex.Research;
        var kept = research.ToSaved();
        bool keptComplete = research.Complete;
        var starly = PokemonDatabase.Get("Starly")!;
        string move = ResearchTasks.TwoMoves(starly)[0];

        // The battle, fought headless on the game's own Pokédex
        var party = new Party();
        var mine = new Pokemon(starly, 40, Gender.Male, Nature.Hardy, false);
        mine.Moves.Clear();
        mine.Moves.Add(new Move(MoveDatabase.Get(move)));
        party.Add(mine);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = pokedex, Rules = Ruleset.Current,
            WildPokemon = new List<Pokemon> { new(starly, 2, Gender.Female, Nature.Hardy, false) }, Random = new BattleRandom(7)
        });
        void ReadOn()
        {
            for (int i = 0; i < 400 && !battle.IsBattleOver; i++)
            {
                if (battle.HUD.MenuState is not (BattleMenuState.Message or BattleMenuState.LearnMove)) break;
                battle.ConfirmMessage();
                battle.Update(1f / 60f);
            }
        }
        ReadOn();
        if (!battle.IsBattleOver) { battle.SelectMove(0); ReadOn(); }
        var record = research.RecordOf(starly.DexNumber);
        Console.WriteLine($"research after a battle: Starly met {record.CountOf("met")}, {move} {record.CountOf("move:" + move)}, beaten {record.CountOf("defeated")}, level {research.LevelOf(starly)}, points {research.Points(starly)}");

        // A few more of the record, as a game would come to, so the page shows rungs reached
        research.Tell(starly, ResearchKind.Met, n: 6);
        research.Tell(starly, ResearchKind.Defeated, n: 4);
        research.Tell(starly, ResearchKind.Caught, n: 2);
        research.Tell(starly, ResearchKind.Leveled, n: 3);
        foreach (var s in new[] { "Bidoof", "Shinx", "Turtwig" }.Select(n => PokemonDatabase.Get(n)!)) research.Tell(s, ResearchKind.Met, n: 25);

        var dexScreen = game.PokedexScreen;
        void Pick(string name) => dexScreen.SelectedIndex = dexScreen.Rows.ToList().FindIndex(r => r.Species.Name == name);
        foreach (int seen in pokedex.SeenSpecies.Take(40)) PokemonSprites.Request(PokemonDatabase.GetByDex(seen)!.Name);
        PokemonSprites.Flush(game.RenderContext);
        game.State = GameState.PokedexMenu;
        dexScreen.Open(pokedex, game.World.Portrait(PlayerIdentity.Character));
        Pick("Starly");
        dexScreen.Confirm();
        while (dexScreen.Page != PokedexPage.Tasks) dexScreen.Sideways(1);
        Frames(20); Shot("26zg_pokedex_tasks_starly");
        Console.WriteLine($"  Starly: level {research.LevelOf(starly)}, points {research.Points(starly)}, tasks {string.Join(", ", ResearchTasks.For(starly).Select(t => $"{t.Key} {research.RecordOf(starly.DexNumber).CountOf(t.Key)}"))}");
        dexScreen.Cancel();
        Pick("Starly");
        Frames(10); Shot("26zh_pokedex_list_levels");

        // A record whole: the disc turns Gold
        foreach (var task in ResearchTasks.For(starly)) research.Tell(starly, task.Kind, task.Move, task.Rungs[^1]);
        dexScreen.Confirm();
        while (dexScreen.Page != PokedexPage.Tasks) dexScreen.Sideways(1);
        Frames(10); Shot("26zi_pokedex_tasks_complete");
        dexScreen.Close();

        // Every species of the Sinnoh Pokédex at level 10: the certificate as the Pokédex opens
        foreach (var s in Pokedex.Entries(Pokedex.Sinnoh).Select(e => e.Species))
            foreach (var task in ResearchTasks.For(s)) research.Tell(s, task.Kind, task.Move, task.Rungs[^1]);
        research.Complete = false;
        dexScreen.Open(pokedex, game.World.Portrait(PlayerIdentity.Character));
        while (dexScreen.Focus == PokedexFocus.Diploma && dexScreen.ShownDiploma != Research.CertificateMode) dexScreen.Confirm();
        Frames(20); Shot("26zj_pokedex_research_certificate");
        Console.WriteLine($"  certificate shown {dexScreen.ShownDiploma == Research.CertificateMode}, complete {research.Complete}, Sinnoh {research.SinnohProgress()}");
        dexScreen.Close();

        research.Restore(kept, keptComplete);
        research.TakeRisen();
        game.State = GameState.Overworld;
    }
}
