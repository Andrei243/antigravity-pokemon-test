partial class Harness
{
    // The field of the later games (plan 06 · R19b), the very last part of `conditions` (CONDITIONS_FROM=later runs it
    // alone): the four new weathers (96_later_snow, _harsh_sun, _heavy_rain, _strong_winds), the four terrains
    // (96_later_electric_terrain, _grassy_terrain, _misty_terrain, _psychic_terrain, each with its foe's platform close
    // up, _close), Aurora Veil and Sticky Web on both sides (96_later_veil, 96_later_web, with both platforms close up),
    // and a Mimikyu's disguise busted and an Aegislash's blade drawn (96_later_shapes). The moves that lay them are R24 to
    // R26's, so each is laid through the core's own entry point and the screen settled to it, as a menu opening settles
    // a field it fell behind; the shapes are the rules' own, played by a turn. It prints what the screen has of each.
    void LaterFieldMode()
    {
        BattleEngine Wild(string mine, string foe, uint seed, string? ability = null, string? foeAbility = null, params string[] moves)
        {
            var own = new Pokemon(PokemonDatabase.Get(mine)!, 50, new Random((int)seed));
            own.Moves.Clear();
            foreach (var m in moves.Length > 0 ? moves : new[] { "Growl" }) own.Moves.Add(new Move(MoveDatabase.Get(m)));
            if (ability != null) own.AbilityName = ability;
            var team = new Party();
            team.Add(own);
            var enemy = new Pokemon(PokemonDatabase.Get(foe)!, 50, new Random((int)seed + 1));
            enemy.Moves.Clear();
            enemy.Moves.Add(new Move(MoveDatabase.Get(foe == "Snorlax" ? "Tackle" : "Growl")));
            if (foeAbility != null) enemy.AbilityName = foeAbility;
            game.Map = MapDatabase.Get("Sinnoh");
            game.BattleRenderer.SetArena(BattleArena.Grass);
            var b = new BattleEngine(new BattleSetup
            {
                PlayerParty = team, Inventory = inventory, Pokedex = pokedex, WildPokemon = new List<Pokemon> { enemy },
                Random = new BattleRandom(seed).Force(RollKind.Critical, 1).Force(RollKind.Damage, 0).Force(RollKind.Accuracy, 0),
                Rules = Ruleset.Modern
            });
            game.Battle = b;
            game.State = GameState.Battle;
            ToMainMenu(b);
            return b;
        }

        // The screen brought level with what the rules were given, as a menu settles a field it fell behind
        void Settled(BattleEngine b)
        {
            b.Anim.FieldLook.Settle(b.Core.Field);
            b.Anim.SettleWeather(b.Core.Field.Weather);
            Skip(1.4);
        }

        void Left(BattleEngine b, string what)
        {
            var look = b.Anim.FieldLook;
            var standing = new List<string>();
            foreach (var side in new[] { BattleSide.Player, BattleSide.Enemy })
                foreach (var kind in Enum.GetValues<SideCondition>())
                    if (look.Of(side, kind).Visible) standing.Add($"{side} {kind}");
            foreach (var kind in Enum.GetValues<FieldCondition>())
                if (look.Of(kind).Visible) standing.Add(kind.ToString());
            Console.WriteLine($"  {what}: sky {b.Anim.Weather}, {(standing.Count == 0 ? "nothing on the field" : string.Join(", ", standing))}; " +
                $"{look.Difference(b.Core.Field) ?? "as the rules have it"}; {game.BattleRenderer.Effects.Count} quads");
        }

        void Close(string name, bool foe) => ShotCrop(name, foe ? 1090 : 60, foe ? 110 : 390, 760, 480, 2);

        // ---- the weathers
        foreach (var (weather, name) in new[]
        {
            (BattleWeather.Snow, "snow"), (BattleWeather.HarshSun, "harsh_sun"), (BattleWeather.HeavyRain, "heavy_rain"), (BattleWeather.StrongWinds, "strong_winds")
        })
        {
            var b = Wild("Empoleon", "Bidoof", 71);
            if (weather == BattleWeather.Snow) b.Core.SetWeather(BattleWeather.Hail, 5, "");
            else b.Core.SetStrongWeather(b.Core.At(new Place(BattleSide.Player, 0)), weather);
            Settled(b);
            Left(b, name);
            Shot($"96_later_{name}");
        }

        // ---- the terrains
        foreach (var (terrain, name) in new[]
        {
            (FieldCondition.ElectricTerrain, "electric_terrain"), (FieldCondition.GrassyTerrain, "grassy_terrain"),
            (FieldCondition.MistyTerrain, "misty_terrain"), (FieldCondition.PsychicTerrain, "psychic_terrain")
        })
        {
            var b = Wild("Empoleon", "Bidoof", 72);
            b.Core.SetTerrain(terrain, null);
            Settled(b);
            Left(b, name);
            Shot($"96_later_{name}");
            Close($"96_later_{name}_close", foe: true);
        }

        // ---- Aurora Veil and Sticky Web, on both sides
        var vb = Wild("Glalie", "Bidoof", 73);
        vb.Core.SetWeather(BattleWeather.Hail, 5, "");
        vb.Core.RaiseAuroraVeil(vb.Core.At(new Place(BattleSide.Player, 0)));
        vb.Core.RaiseAuroraVeil(vb.Core.At(new Place(BattleSide.Enemy, 0)));
        Settled(vb);
        Left(vb, "Aurora Veil on both sides");
        Shot("96_later_veil");
        Close("96_later_veil_close", foe: true);
        Close("96_later_veil_mine_close", foe: false);

        var wb = Wild("Empoleon", "Bidoof", 74);
        wb.Core.LayStickyWeb(BattleSide.Player);
        wb.Core.LayStickyWeb(BattleSide.Enemy);
        Settled(wb);
        Left(wb, "Sticky Web on both sides");
        Shot("96_later_web");
        Close("96_later_web_close", foe: true);
        Close("96_later_web_mine_close", foe: false);

        // ---- shapes: an Aegislash's blade drawn on the player's side, a Mimikyu's disguise busted on the foe's
        var sb = Wild("Aegislash", "Mimikyu", 75, "Stance Change", "Disguise", "Shadow Sneak");
        sb.SelectMove(0);
        for (int i = 0; i < 40 && sb.HUD.MenuState != BattleMenuState.Main && !sb.IsBattleOver; i++)
        {
            Confirm(sb);
            Skip(0.3);
        }
        Skip(1.0);
        Console.WriteLine($"  shapes: {sb.PlayerPokemon.Form ?? "-"} and {sb.EnemyPokemon.Form ?? "-"} on screen; the rules have " +
            $"{sb.Core.At(new Place(BattleSide.Player, 0)).Pokemon!.Form ?? "-"} and {sb.Core.At(new Place(BattleSide.Enemy, 0)).Pokemon!.Form ?? "-"}");
        Shot("96_later_shapes");
        game.State = GameState.Overworld;
    }
}
