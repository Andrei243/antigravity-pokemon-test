using System;
using System.IO;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;

partial class Harness
{
    // ---------------------------------------------------------------- photo mode in battle (plan 12 · Q17)

    // A wild battle on Route 201 through photo mode (23i_battle_photo_*, the last part of `photo`): the battle stilled at
    // its menu from the overview with the interface gone, the panel, the camera turned round to the side and down low,
    // a move's attacker shot from behind the player's own Pokémon held mid-lunge, a stepped pose close up, the night
    // borrowed, a filter and the two frames, a picture written, and the battle going on afterwards. It prints the
    // animator's clock, the line and both HP before and after the frames a photo takes, which must not move.
    void PhotoBattle()
    {
        var b = StartBattle("Bidoof", 3);
        ToMainMenu(b);
        Console.WriteLine($"battle: menu {b.HUD.MenuState}, camera {game.BattleRenderer.Shot}");

        if (!game.OpenBattlePhoto()) Console.WriteLine("!! photo mode would not open in battle");
        var p = game.BattlePhoto!;
        float clock = b.Anim.Time;
        string line = b.CurrentMessage;
        int mine = b.PlayerPokemon.CurrentHP, theirs = b.EnemyPokemon.CurrentHP;
        Frames(4); Shot("23i_battle_photo_overview");
        Console.WriteLine($"held: camera {game.BattleRenderer.Shot}");

        p.PanelOpen = true;
        Frames(2); Shot("23i_battle_photo_panel");
        p.PanelOpen = false;

        p.Turn(new Vector2(1f, 0f), 0.6f);
        p.ZoomBy(-2);
        p.Turn(new Vector2(0f, 1f), 0.15f);
        Frames(2); Shot("23i_battle_photo_turned");
        Console.WriteLine($"turned: yaw {p.Yaw:0.00}, pitch {p.Pitch * 180f / MathF.PI:0.0}°, camera at {p.Shot.Position.X:0.0}, {p.Shot.Position.Y:0.0}, {p.Shot.Position.Z:0.0}");

        Console.WriteLine($"stilled: clock {clock:0.000} -> {b.Anim.Time:0.000}, line \"{line}\" -> \"{b.CurrentMessage}\", HP {mine}/{theirs} -> {b.PlayerPokemon.CurrentHP}/{b.EnemyPokemon.CurrentHP}");
        if (b.Anim.Time != clock || b.CurrentMessage != line) Console.WriteLine("!! the photo moved the battle on");
        game.CloseBattlePhoto();
        Frames(20);

        // A move: the director over the shoulder of the player's own Pokémon as it winds up, then held there
        b.SelectMove(0);
        Skip(8 / 60.0);
        Console.WriteLine($"move: \"{b.CurrentMessage}\", camera {game.BattleRenderer.Shot}");
        game.OpenBattlePhoto();
        p = game.BattlePhoto!;
        clock = b.Anim.Time;
        Frames(4); Shot("23i_battle_photo_from_behind");

        // A stepped pose, close
        p.ZoomBy(3);
        SetBattleRow(p, BattlePhotoRow.Pose, 0);
        Frames(2); Shot("23i_battle_photo_pose_0");
        SetBattleRow(p, BattlePhotoRow.Pose, 6);
        p.PanelOpen = true;
        Frames(2); Shot("23i_battle_photo_pose_6");
        p.PanelOpen = false;
        Console.WriteLine($"pose: step {p.PoseStep:0.0} s, the animator's clock {b.Anim.Time:0.000}");
        p.ZoomBy(-3);

        // The night borrowed, a filter, the frames
        SetBattleRow(p, BattlePhotoRow.Time, 4);
        Frames(2); Shot("23i_battle_photo_night");
        Console.WriteLine($"night: the stage's hour {p.Hour:0.00}, the clock's {GameClock.Hour:0.00}");
        SetBattleRow(p, BattlePhotoRow.Time, -4);
        SetBattleRow(p, BattlePhotoRow.Filter, 2);
        SetBattleRow(p, BattlePhotoRow.Frame, 2);
        Frames(2); Shot("23i_battle_photo_mono_cinema");
        SetBattleRow(p, BattlePhotoRow.Filter, -2);
        SetBattleRow(p, BattlePhotoRow.Frame, -1);
        Frames(2); Shot("23i_battle_photo_mat");
        SetBattleRow(p, BattlePhotoRow.Frame, -1);

        // A picture: the screenshot key's capture, with nothing of the interface in it
        game.PressScreenshot();
        Frames(1);
        string? picture = game.LastScreenshot;
        Console.WriteLine(picture == null ? "!! no photo of the battle was written" : $"the photo: {Path.GetFileName(picture)}, {new FileInfo(picture).Length} bytes");
        if (picture != null) File.Copy(picture, Path.Combine(outDir, "23i_battle_photo_written.png"), overwrite: true);
        if (b.Anim.Time != clock) Console.WriteLine("!! the photo moved the battle's clock on");

        // Let go: the move goes on from where it stood
        game.CloseBattlePhoto();
        Skip(14 / 60.0); Shot("23i_battle_photo_after");
        Console.WriteLine($"after: clock {b.Anim.Time:0.000}, camera {game.BattleRenderer.Shot}, line \"{b.CurrentMessage}\"");
    }

    // Takes a row of the battle photo's panel a number of values on (or back)
    static void SetBattleRow(BattlePhotoView p, BattlePhotoRow row, int by)
    {
        while (p.Row != row) p.MoveRow(1);
        int step = Math.Sign(by);
        for (int i = 0; i < Math.Abs(by); i++) p.Change(step);
    }
}
