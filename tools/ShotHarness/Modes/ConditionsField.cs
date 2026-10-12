partial class Harness
{
    // What lies on the field (plan 25 · Y2), the last part of `conditions`: each look put there by its own move and
    // read on to the menu, where it stands whole. Spikes falling (96_hazards_falling), three layers of Spikes, two of
    // Toxic Spikes and Stealth Rock on the foe's side (96_hazards, with the foe's platform close up, 96_hazards_close),
    // blown away by Defog (96_hazards_blown, 96_hazards_gone), and the same on the player's side (96_hazards_mine,
    // 96_hazards_mine_close); Reflect and Light Screen on both sides (96_screens, 96_screens_close,
    // 96_screens_mine_close); Safeguard, Mist and Lucky Chant (96_veils); Tailwind (96_tailwind); Trick Room coming
    // up and standing (96_trick_room_rising, 96_trick_room); the stage before and under Gravity (96_gravity_before,
    // 96_gravity); a Wish over the player's Pokémon and a Future Sight over the foe (96_wish, with both close up); and
    // a double battle in a sandstorm with every hazard and screen on both sides (96_field_double). It prints what each
    // left on the field as the screen has it and whether that is what the rules have.
    void FieldLookMode()
    {
        BattleEngine Wild(string foe, int level, string foeMove, uint seed, BattleConditions conditions = null)
        {
            var enemy = new Pokemon(PokemonDatabase.Get(foe)!, level);
            enemy.Moves.Clear();
            enemy.Moves.Add(new Move(MoveDatabase.Get(foeMove)));
            var (name, x, y) = Place("Route201", -1, -1);
            game.Map = MapDatabase.Get(name);
            if (x >= 0) game.Player.SetPosition(x, y, Direction.Up);
            var b = new BattleEngine(new BattleSetup
            {
                PlayerParty = party, Inventory = inventory, Pokedex = pokedex, WildPokemon = new List<Pokemon> { enemy },
                Random = new BattleRandom(seed).Force(RollKind.Critical, 1).Force(RollKind.Damage, 0).Force(RollKind.Accuracy, 0),
                Conditions = conditions
            });
            game.BattleRenderer.SetArena(game.Map, x, y);
            game.Battle = b;
            game.State = GameState.Battle;
            ToMainMenu(b);
            return b;
        }

        // Reads on to the next menu, where the camera is back on the overview and what came in stands whole
        void ToMenu(BattleEngine b)
        {
            for (int i = 0; i < 60 && b.HUD.MenuState != BattleMenuState.Main && !b.IsBattleOver; i++)
            {
                Confirm(b);
                Skip(0.3);
            }
            Skip(1.2);
        }

        // Reads on to a line and lets what it starts play for a while: 0.3 s of it have passed as the line is found,
        // which is half of a look's coming in and three fifths of its fading
        void ReadTo(BattleEngine b, string line, double after)
        {
            for (int i = 0; i < 60 && b.CurrentMessage != line && !b.IsBattleOver; i++)
            {
                Confirm(b);
                Skip(0.3);
            }
            Skip(after);
        }

        // A turn: the player's move by its place, and the move the foe has for it
        void Use(BattleEngine b, int move, string foeMove = null)
        {
            if (foeMove != null)
            {
                b.EnemyPokemon.Moves.Clear();
                b.EnemyPokemon.Moves.Add(new Move(MoveDatabase.Get(foeMove)));
            }
            b.SelectMove(move);
        }

        void Left(BattleEngine b, string what)
        {
            var look = b.Anim.FieldLook;
            var standing = new List<string>();
            foreach (var side in new[] { BattleSide.Player, BattleSide.Enemy })
            {
                foreach (var kind in Enum.GetValues<SideCondition>())
                    if (look.Of(side, kind).Visible) standing.Add($"{side} {kind} {look.Of(side, kind).Layers}{(look.Of(side, kind).Up ? "" : " (fading)")}");
                for (int slot = 0; slot < b.Anim.Slots; slot++)
                    foreach (var kind in Enum.GetValues<PlaceCondition>())
                        if (look.Of(new Place(side, slot), kind).Visible) standing.Add($"{side} {slot} {kind}");
            }
            foreach (var kind in Enum.GetValues<FieldCondition>())
                if (look.Of(kind).Visible) standing.Add($"{kind}{(look.Of(kind).Up ? "" : " (fading)")}");
            // (While lines are still being read the rules are ahead of the screen, as they should be: only a menu holds the two together)
            string held = b.HUD.MenuState == BattleMenuState.Message ? "the rules are a turn ahead" : look.Difference(b.Core.Field) ?? "as the rules have it";
            Console.WriteLine($"  {what}: {(standing.Count == 0 ? "nothing on the field" : string.Join(", ", standing))}; {held}; {game.BattleRenderer.Effects.Count} quads");
        }

        Pokemon Mine(string species, params string[] moves)
        {
            var mine = new Pokemon(PokemonDatabase.Get(species)!, 40);
            mine.Moves.Clear();
            foreach (var m in moves) mine.Moves.Add(new Move(MoveDatabase.Get(m)));
            party.HealAll();
            party.Members.Insert(0, mine);
            return mine;
        }

        void Over(Pokemon mine)
        {
            party.Members.Remove(mine);
            game.State = GameState.Overworld;
        }

        // The two platforms at the screen's own resolution: what lies on them is small
        void Close(string name, bool foe, int up = 0) => ShotCrop(name, foe ? 1090 : 60, (foe ? 110 : 390) - up, 760, 480, 2);

        // ---- what lies in wait, on the foe's side
        var scatterer = Mine("Skarmory", "Spikes", "Toxic Spikes", "Stealth Rock", "Defog");
        var hb = Wild("Bidoof", 12, "Growl", 31);
        Use(hb, 0);
        ReadTo(hb, "Spikes were scattered all around the foe's team's feet!", 0);
        Shot("96_hazards_falling");
        ToMenu(hb);
        foreach (int move in new[] { 0, 0, 1, 1, 2 })
        {
            Use(hb, move);
            ToMenu(hb);
        }
        Left(hb, "the foe's side scattered");
        Shot("96_hazards");
        Close("96_hazards_close", foe: true);
        // Blown away by Defog: each line takes its own off
        Use(hb, 3);
        ReadTo(hb, "The spikes around the foe's team were blown away!", 0);
        Left(hb, "as Defog blows them away");
        Shot("96_hazards_blown");
        ToMenu(hb);
        Left(hb, "after Defog");
        Shot("96_hazards_gone");
        Over(scatterer);

        // ---- and on the player's own: a Skarmory across the field scatters them
        var target = Mine("Empoleon", "Growl");
        var mb = Wild("Skarmory", 30, "Spikes", 32);
        string[] scattered = { "Spikes", "Spikes", "Spikes", "Toxic Spikes", "Toxic Spikes", "Stealth Rock" };
        foreach (string move in scattered)
        {
            Use(mb, 0, move);
            ToMenu(mb);
        }
        Left(mb, "the player's side scattered");
        Shot("96_hazards_mine");
        Close("96_hazards_mine_close", foe: false);
        Over(target);

        // ---- the screens, on both sides
        var screener = Mine("Empoleon", "Reflect", "Light Screen", "Safeguard", "Mist");
        var sb = Wild("Bidoof", 12, "Reflect", 33);
        Use(sb, 0, "Reflect");
        ReadTo(sb, "Reflect raised your team's Defense!", 0);
        Shot("96_screens_rising");
        ToMenu(sb);
        Use(sb, 1, "Light Screen");
        ToMenu(sb);
        Left(sb, "both sides screened");
        Shot("96_screens");
        Close("96_screens_close", foe: true);
        Close("96_screens_mine_close", foe: false);
        Over(screener);

        // ---- the veils and the chant
        var veiler = Mine("Empoleon", "Safeguard", "Mist", "Lucky Chant");
        var vb = Wild("Bidoof", 12, "Safeguard", 34);
        Use(vb, 0, "Safeguard");
        ToMenu(vb);
        Use(vb, 1, "Mist");
        ToMenu(vb);
        Use(vb, 2, "Lucky Chant");
        ToMenu(vb);
        Left(vb, "both sides veiled");
        Shot("96_veils");
        Close("96_veils_close", foe: true);
        Close("96_veils_mine_close", foe: false);
        Over(veiler);

        // ---- Tailwind
        var blower = Mine("Empoleon", "Tailwind");
        var tb = Wild("Bidoof", 12, "Tailwind", 35);
        Use(tb, 0);
        ToMenu(tb);
        Left(tb, "a tailwind behind both");
        Shot("96_tailwind");
        Over(blower);

        // ---- Trick Room
        var twister = Mine("Empoleon", "Trick Room");
        var rb = Wild("Bidoof", 12, "Growl", 36);
        Use(rb, 0);
        ReadTo(rb, "Empoleon twisted the dimensions!", 0);
        Shot("96_trick_room_rising");
        ToMenu(rb);
        Left(rb, "Trick Room");
        Shot("96_trick_room");
        Over(twister);

        // ---- Gravity: the stage before it and under it
        var presser = Mine("Empoleon", "Gravity");
        var gb = Wild("Bidoof", 12, "Growl", 37);
        Skip(0.5);
        Shot("96_gravity_before");
        Use(gb, 0);
        ToMenu(gb);
        Left(gb, "Gravity");
        Shot("96_gravity");
        Over(presser);

        // ---- sent ahead: a Future Sight over the foe, then a Wish over the player's own
        var seer = Mine("Empoleon", "Future Sight", "Wish");
        var wb = Wild("Bidoof", 40, "Growl", 38);
        Use(wb, 0);
        ToMenu(wb);
        Use(wb, 1);
        ToMenu(wb);
        Left(wb, "a Wish and a Future Sight waiting");
        Shot("96_wish");
        Close("96_wish_close", foe: false, up: 150);
        Close("96_future_sight_close", foe: true, up: 70);
        // The turn after, both arrive and are gone
        Use(wb, 1);
        ToMenu(wb);
        Left(wb, "the turn they arrive");
        Over(seer);

        // ---- everything at once, in a double battle in a sandstorm
        var db = EverythingOnTheField();
        Skip(1.0);
        Left(db, "a double battle with everything");
        Shot("96_field_double");
        game.State = GameState.Overworld;
    }

    // A double battle in a sandstorm with every hazard and screen on both sides (plan 25 · Y2), at its menu: three
    // layers of Spikes, two of Toxic Spikes, Stealth Rock, Reflect, Light Screen, Safeguard, Mist, Tailwind and Lucky
    // Chant on each. Its teams and its chance are made from fixed numbers, never the game's seeded chance, so `profile`
    // times the same picture at every commit. The field is written straight into the rules and settled, as a menu
    // opening settles it: no line of this battle put it there.
    BattleEngine EverythingOnTheField()
    {
        var mine = new Party();
        mine.Add(new Pokemon(PokemonDatabase.Get("Empoleon")!, 50, new Random(21)));
        mine.Add(new Pokemon(PokemonDatabase.Get("Luxray")!, 50, new Random(22)));
        var one = new Trainer { Name = "Ana", TrainerClass = "Ace Trainer" };
        one.Party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, 50, new Random(23)));
        var two = new Trainer { Name = "Cal", TrainerClass = "Ace Trainer" };
        two.Party.Add(new Pokemon(PokemonDatabase.Get("Bronzong")!, 50, new Random(24)));
        game.Map = MapDatabase.Get("Sinnoh");
        game.BattleRenderer.SetArena(BattleArena.Grass);
        var b = new BattleEngine(new BattleSetup
        {
            PlayerParty = mine, Inventory = inventory, Pokedex = pokedex, Format = BattleFormat.Double,
            Trainers = new List<Trainer> { one, two }, WildPokemon = new List<Pokemon>(), Random = new BattleRandom(2502),
            Conditions = new BattleConditions { Weather = BattleWeather.Sandstorm }
        });
        game.Battle = b;
        game.State = GameState.Battle;
        Skip(130 / 60.0);
        for (int guard = 0; guard < 16 && b.HUD.MenuState == BattleMenuState.Message && !b.IsBattleOver; guard++)
        {
            b.ConfirmMessage();
            Frames(1);
            Skip(50 / 60.0);
        }
        foreach (var side in new[] { BattleSide.Player, BattleSide.Enemy })
        {
            var has = b.Core.Field.Side(side);
            has.Spikes = 3;
            has.ToxicSpikes = 2;
            has.StealthRock = true;
            has.ReflectTurns = has.LightScreenTurns = has.SafeguardTurns = has.MistTurns = has.TailwindTurns = has.LuckyChantTurns = 5;
        }
        b.Anim.FieldLook.Settle(b.Core.Field);
        Skip(0.8);
        return b;
    }
}
