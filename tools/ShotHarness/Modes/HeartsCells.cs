using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

partial class Harness
{
    // ---------------------------------------------------------------- Lockwork's cells (plan 44 · SD3, SD4)

    // Lockwork's cells and its tower (lh10 to lh32; HEARTS_PART=cells runs them alone): Looker with the Reclaimer in the
    // Survival Area (lh10), the spring's water at Sendoff Spring (lh11), and for each of the five regions its port with the
    // doorman and the agents, the cell's room and its admin's battle at the menu (lh12 to lh26); the tower's second floor
    // with the head and his battle (lh27, lh28), the courier in the lobby once the machine has stopped (lh29), and a
    // locked heart following the player in the lock's grey beside an open one (lh30, lh31). It prints who stands where.
    public void HeartsCells()
    {
        party.Clear();
        party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, 100));
        foreach (string region in new[] { "Sinnoh", "Kanto", "Johto", "Hoenn", "Unova" }) game.Story.Set($"{region}HallOfFame");

        // ---- Looker in the Survival Area and the spring
        At("Sinnoh", 658, 344, Direction.Up);
        game.LocationSign.Hide();
        Frames(30);
        Shot("lh10_survival_looker");
        At("SendoffSpring", 30, 33, Direction.Up);
        game.LocationSign.Hide();
        Frames(30);
        Shot("lh11_spring");

        int n = 12;
        foreach (var (region, map, x, y) in new[] { ("Sinnoh", "Sinnoh", 651, 442), ("Kanto", "Kanto", 282, 226), ("Johto", "Johto", 266, 268), ("Hoenn", "Hoenn", 513, 104) })
        {
            At(map, x, y + 3, Direction.Up);
            game.LocationSign.Hide();
            Frames(30);
            Shot($"lh{n++}_{region.ToLowerInvariant()}_port");
            Console.WriteLine($"{region}'s port: {string.Join(", ", game.Map.NPCs.Where(p => p.Key?.StartsWith("lockwork_") == true).Select(p => $"{p.Key} {p.GridX},{p.GridY}"))}");
            At("LockworkCell" + region, 6, 8, Direction.Up);
            game.LocationSign.Hide();
            Frames(30);
            Shot($"lh{n++}_{region.ToLowerInvariant()}_cell");
            AdminBattle("lockwork_admin", $"lockwork_{region.ToLowerInvariant()}_admin", $"lh{n++}_{region.ToLowerInvariant()}_admin");
        }

        // ---- Unova: the tower's door on Castelia's avenue, its lobby and the admin
        At("CasteliaCity", 54, 8, Direction.Up);
        game.LocationSign.Hide();
        Frames(30);
        Shot("lh24_castelia_tower_door");
        At("LockworkTower1F", 6, 8, Direction.Up);
        game.LocationSign.Hide();
        Frames(30);
        Shot("lh25_tower_lobby");
        AdminBattle("lockwork_admin", "lockwork_unova_admin", "lh26_unova_admin");

        // ---- the head on the second floor
        game.Story.Set("FLAG_LOCKWORK_UNOVA_ADMIN_BEATEN");
        At("LockworkTower2F", 6, 6, Direction.Up);
        game.LocationSign.Hide();
        Frames(30);
        Shot("lh27_tower_head");
        AdminBattle("lockwork_head", "lockwork_head", "lh28_head_battle");

        // ---- the machine stopped: the courier in the lobby
        game.Story.Set("FLAG_LOCKWORK_MACHINE_STOPPED");
        At("LockworkTower1F", 6, 8, Direction.Up);
        game.LocationSign.Hide();
        Frames(30);
        Shot("lh29_courier");
        Console.WriteLine($"lobby: {string.Join(", ", game.Map.NPCs.Select(p => $"{p.Key} {p.GridX},{p.GridY}"))}");

        // ---- a locked heart following the player in the lock's grey, and the same once its heart is open
        party.Clear();
        var locked = TrainerDatabase.Build(new TrainerPokemonRecord { Species = "Gible", Level = 30, IvScale = 100, Personality = 4242, Lock = "lock_sinnoh_gible" });
        party.Add(locked);
        engine.Settings.Follower = true;
        At("Sinnoh", 106, 887, Direction.Right);
        for (int i = 0; i < 2 && game.State == GameState.Overworld; i++)
        {
            engine.Steering = (Direction.Right, false);
            for (int f = 0; f < 30 && !game.Player.IsMoving; f++) Frames(1);
            engine.Steering = null;
            for (int f = 0; f < 90 && game.Player.IsMoving; f++) Frames(1);
        }
        for (int f = 0; f < 240 && game.Lead?.Moving == true; f++) Frames(1);
        Frames(60);
        Shot("lh30_follower_locked");
        Console.WriteLine($"the follower: {game.Lead?.Pokemon.DisplayName}, locked {locked.Lock != null}");
        locked.Lock = null;
        Frames(30);
        Shot("lh31_follower_open");
        engine.Settings.Follower = false;
        Frames(10);
    }

    // A person of the place battled as a team of the table, at the battle's menu, shot and put down again
    void AdminBattle(string key, string trainerId, string shot)
    {
        var who = game.Map.Everyone.FirstOrDefault(p => p.Key == key);
        if (who == null) { Console.WriteLine($"!! nobody {key} on {game.Map.Name}"); return; }
        var team = new Trainer { Id = trainerId };
        TrainerDatabase.Fill(team, TrainerDatabase.Get(trainerId)!);
        who.TrainerData = team;
        game.StartTrainerBattle(who);
        for (int i = 0; i < 600 && game.State != GameState.Battle; i++) Frames(1);
        if (game.Battle is not { } b) { Console.WriteLine($"!! {trainerId}'s battle didn't start"); return; }
        ToMainMenu(b);
        Skip(1.2);
        Shot(shot);
        Console.WriteLine($"{trainerId}: {string.Join(", ", (b.EnemyParty?.Members ?? new()).Select(p => $"{p.DisplayName} {p.Level}{(p.Lock != null ? " locked" : "")}"))}");
        game.Battle = null;
        game.State = GameState.Overworld;
        Frames(10);
    }
}
