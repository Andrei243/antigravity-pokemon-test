using PokemonPlatinumEngine.Battle.Sim.Link;

partial class Harness
{
    // ---------------------------------------------------------------- the battle's manners over the formats (plan 45 · BC7)

    // CAMERA_PART=formats (also `triple`, `rotation`, `royal`, `sos`, `photo`, `halls`, `link`, `replay` alone): for each
    // manner the format that matters to it: a triple's and a rotation's leads' split and their menu's cycle (bc_<manner>_
    // triple_*, _rotation_*), a Battle Royal's four quarters for the manners that have them (bc_<manner>_royal_split_*), an
    // SOS ally's hop (bc_<manner>_sos_*), a photo composed from an idle shot (bc_<manner>_photo_0_idle, _1_held, _2_turned),
    // the cycle over the Frontier's hall with a Brain and the Subway's street (bc_<manner>_hall_*, _subway_*), a link
    // battle's two screens in two manners at the same turn (bc_link_*), and a recorded battle watched (bc_<manner>_replay_*)
    // Not part of `all`.
    void CameraFormats(List<CameraManner> manners, Func<string, bool> part)
    {
        var savedParty = party.Members.ToList();
        void Quick()
        {
            FrameClock.Fixed = ++tick / 60.0;
            engine.Update(1f / 60f);
            engine.Draw();
        }
        void Half(string name)
        {
            if (!Wanted(name)) return;
            var img = Capture();
            Raylib.ImageResize(ref img, GameEngine.VirtualWidth / 2, GameEngine.VirtualHeight / 2);
            Save(img, name);
        }
        void ToMenu(BattleEngine b)
        {
            for (int i = 0; i < 120 && !b.IsBattleOver && b.HUD.MenuState != BattleMenuState.Main; i++) { Confirm(b); Skip(0.4); }
        }
        // A picture every `step` seconds, n of them, with the shot and the menu's open time printed
        void ReelEvery(BattleEngine b, string prefix, int n, double step)
        {
            for (int i = 0; i < n && !b.IsBattleOver; i++)
            {
                string name = $"{prefix}_{i:000}";
                Half(name);
                Console.WriteLine($"  {name}: {game.BattleRenderer.Shot}, the menu open {b.Anim.MenuAge:0.00} s");
                Skip(step);
            }
        }
        void Team(params string[] species)
        {
            party.Members.Clear();
            foreach (string s in species) party.Members.Add(new Pokemon(PokemonDatabase.Get(s)!, 50));
        }
        // A trainer's battle begun through the game's own StartTrainerBattle in Route 225's grass
        BattleEngine Begin(Trainer trainer, Func<NPC, BattleEngine> start = null)
        {
            game.Battle = null;
            game.State = GameState.Overworld;
            var (gx, gy) = GrassOf("route_225");
            At("Sinnoh", gx, gy, Direction.Up);
            game.EnterArea();
            game.LocationSign.Hide();
            Frames(4);
            var npc = new NPC { Name = trainer.Name, NpcType = "Trainer", TrainerData = trainer };
            game.StartTrainerBattle(npc);
            for (int i = 0; i < 900 && game.State != GameState.Battle; i++) Quick();
            if (game.State != GameState.Battle) { Console.WriteLine($"  !! never happened: {trainer.Name}'s battle"); return null; }
            return game.Battle;
        }
        Trainer Vera(string format, params string[] species)
        {
            var t = new Trainer
            {
                Id = "harness_camera_" + format, Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 0, Format = format,
                Ai = PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.Basic
            };
            foreach (string s in species) t.Party.Add(new Pokemon(PokemonDatabase.Get(s)!, 44));
            return t;
        }
        bool Splits(CameraManner m) => (m.Splits & CameraSplits.Leads) != 0;

        // ---- a triple and a rotation: the leads' split and the menu's cycle
        foreach (var (format, tag, species) in new[] { ("triple", "triple", new[] { "Staravia", "Kricketune", "Bibarel" }), ("rotation", "rotation", new[] { "Staravia", "Ponyta", "Bibarel" }) })
        {
            if (!part(tag)) continue;
            foreach (var manner in manners.Where(m => Splits(m) || m.Cycle.Count > 0))
            {
                engine.FixedCameraManner = manner;
                Console.WriteLine($"manner {manner.Name}: a {tag} battle");
                Team("Golem", "Rhyperior", "Steelix");
                var b = Begin(Vera(format, species));
                if (b == null) continue;
                Console.WriteLine($"  format {b.Format}; places {b.Anim.SlotsOf(BattleSide.Player)} against {b.Anim.SlotsOf(BattleSide.Enemy)}, turntables {b.Anim.Turntables}");
                if (Splits(manner)) SplitReel(b, $"bc_{manner.Name}_{tag}_split_leads");
                ToMenu(b);
                if (manner.Cycle.Count > 0 || manner.Drift > 0f)
                    ReelEvery(b, $"bc_{manner.Name}_{tag}_cycle", Math.Min(24, manner.Cycle.Count * 5 + 2), 1.0);
            }
        }

        // ---- the Battle Royal: four quarters for the manners that have them
        if (part("royal"))
            foreach (var manner in manners.Where(m => Splits(m)))
            {
                engine.FixedCameraManner = manner;
                bool quarters = (manner.Splits & CameraSplits.RoyalQuarters) != 0;
                Console.WriteLine($"manner {manner.Name}: a Battle Royal, quarters {quarters}");
                Team("Luxray", "Gyarados", "Golem");
                NPC Other(string name, params string[] species)
                {
                    var t = Vera("royal_" + name, species);
                    t.Id = "harness_camera_royal_" + name.ToLowerInvariant();
                    t.Name = name;
                    t.Format = null;
                    return new NPC { Name = name, NpcType = "Trainer", TrainerData = t };
                }
                game.Battle = null;
                game.State = GameState.Overworld;
                var (gx, gy) = GrassOf("route_225");
                At("Sinnoh", gx, gy, Direction.Up);
                game.EnterArea();
                game.LocationSign.Hide();
                Frames(4);
                game.StartBattleRoyal(Other("Ana", "Staravia", "Bibarel"), Other("Bo", "Shinx", "Buizel"), Other("Cy", "Machop", "Geodude"));
                for (int i = 0; i < 900 && game.State != GameState.Battle; i++) Quick();
                if (game.State != GameState.Battle) { Console.WriteLine("  !! never happened: the Battle Royal"); continue; }
                var royal = game.Battle;
                Console.WriteLine($"  format {royal.Format}; places {royal.Anim.SlotsOf(BattleSide.Player)} against {royal.Anim.SlotsOf(BattleSide.Enemy)}; royal {royal.Anim.Royal}");
                SplitReel(royal, $"bc_{manner.Name}_royal_split_leads");
                if (!quarters && game.BattleRenderer.Split != null) Console.WriteLine("  !! a manner without the quarters split a Royal");
            }

        // ---- an SOS battle: the ally's hop
        if (part("sos"))
            foreach (var manner in manners.Where(m => m.React))
            {
                engine.FixedCameraManner = manner;
                Console.WriteLine($"manner {manner.Name}: an SOS battle's ally");
                var golem = new Pokemon(PokemonDatabase.Get("Golem")!, 50);
                golem.Moves.Clear();
                foreach (string move in new[] { "Rock Throw", "Harden" }) golem.Moves.Add(new Move(MoveDatabase.Get(move)));
                party.Members.Clear();
                party.Members.Add(golem);
                game.Battle = null;
                game.State = GameState.Overworld;
                var (gx, gy) = GrassOf("route_225");
                At("Sinnoh", gx, gy, Direction.Up);
                game.EnterArea();
                game.LocationSign.Hide();
                Frames(4);
                game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Pikipek", MinLevel = 12, MaxLevel = 12, CallsForHelp = true });
                for (int i = 0; i < 600 && game.State != GameState.Battle; i++) Quick();
                if (game.State != GameState.Battle) { Console.WriteLine("  !! never happened: the SOS battle"); continue; }
                var sos = game.Battle;
                if (sos.Core.Random is not BattleRandom rolls) { Console.WriteLine("  !! the SOS battle's chance is no BattleRandom"); continue; }
                rolls.Force(RollKind.Call, 0);
                ToMainMenu(sos);
                ToMenu(sos);
                golem.CurrentHP = golem.MaxHP;
                sos.SelectMainMenuOption(0);
                sos.SelectMove(1);
                int taken = 0;
                for (int i = 0; i < 120 && !sos.IsBattleOver && taken < 14; i++)
                {
                    Skip(0.1);
                    bool landing = sos.Anim[BattleSide.Enemy, 1].ArriveAge >= 0f;
                    if (landing || (taken > 0 && taken < 14))
                    {
                        Half($"bc_{manner.Name}_sos_ally_{taken:00}");
                        Console.WriteLine($"  bc_{manner.Name}_sos_ally_{taken:00}: {game.BattleRenderer.Shot}, the ally's hop {sos.Anim[BattleSide.Enemy, 1].ArriveAge:0.00}");
                        taken++;
                    }
                    if (i % 6 == 5 && sos.HUD.MenuState == BattleMenuState.Message) Confirm(sos);
                }
                if (taken == 0) Console.WriteLine("  !! never seen: the ally's hop");
            }

        // ---- a photo composed from an idle shot
        if (part("photo"))
            foreach (var manner in manners.Where(m => m.Cycle.Count > 1))
            {
                engine.FixedCameraManner = manner;
                Console.WriteLine($"manner {manner.Name}: a photo over the cycle");
                party.HealAll();
                var b = StartBattle("Starly", 8);
                ToMainMenu(b);
                // The cycle to an idle shot that isn't the overview
                for (int i = 0; i < 200 && game.BattleRenderer.Shot is not (ShotKind.Idle or ShotKind.OwnFront); i++) Skip(0.25);
                Console.WriteLine($"  the camera has {game.BattleRenderer.Shot}, the menu open {b.Anim.MenuAge:0.00} s");
                Half($"bc_{manner.Name}_photo_0_idle");
                if (!game.OpenBattlePhoto()) { Console.WriteLine("  !! no photo was begun"); continue; }
                Frames(4);
                Half($"bc_{manner.Name}_photo_1_held");
                Console.WriteLine($"  held: {game.BattleRenderer.Shot}; zoom {game.BattlePhoto?.Zoom}");
                game.BattlePhoto?.Turn(new System.Numerics.Vector2(1f, 0f), 0.5f);
                Frames(4);
                Half($"bc_{manner.Name}_photo_2_turned");
                game.CloseBattlePhoto();
            }

        // ---- the Frontier's hall with a Brain, and the Subway's street: the cycle and the closing, inside walls
        if (part("halls"))
            foreach (var manner in manners.Where(m => m.Crane || m.Cycle.Count > 1))
                foreach (var (tag, map, cls) in new[] { ("hall", "BattleTowerBattleRoom", "Tower Tycoon"), ("subway", "BattleSubwayTrain", "Ace Trainer") })
                {
                    if (tag == "subway" && manner.Name != "galar") continue;
                    engine.FixedCameraManner = manner;
                    Console.WriteLine($"manner {manner.Name}: {tag} ({map})");
                    party.HealAll();
                    var brain = new Trainer { Name = tag == "hall" ? "Palmer" : "Ingo", TrainerClass = cls };
                    brain.Party.Add(new Pokemon(PokemonDatabase.Get("Staraptor")!, 50));
                    brain.Party.Add(new Pokemon(PokemonDatabase.Get("Luxray")!, 50));
                    var b = StartBattle(null, 0, brain, map);
                    Console.WriteLine($"  boss {b.Anim.BossBattle}");
                    ToMainMenu(b);
                    ReelEvery(b, $"bc_{manner.Name}_{tag}_cycle", Math.Min(32, manner.Cycle.Count * 5 + 2), 1.0);
                }

        // ---- a link battle on both screens, A in Galar's manner and B as Classic, at the same turn
        if (part("link"))
        {
            var galar = CameraManners.Find("galar");
            var room = LinkRoom.Default with { Picks = 3 };
            var lucas = new LinkPlayer("Lucas", LinkTeam(11, "Infernape", "Staraptor", "Luxray"));
            var dawn = new LinkPlayer("Dawn", LinkTeam(23, "Empoleon", "Roserade", "Garchomp"));
            var toLucas = new LoopbackLink();
            var toDawn = new LoopbackLink();
            linkServer = new LinkServer(LinkSetup.Bring(room, lucas, dawn), 2026, toLucas.Server, toDawn.Server);
            var a = new LinkClient(lucas.Name, lucas.Team, toLucas.Player, new InOrder()) { AnswersByItself = false };
            var b = new LinkClient(dawn.Name, dawn.Team, toDawn.Player, new InOrder()) { AnswersByItself = false };
            for (int i = 0; i < 8; i++) { a.Pump(); b.Pump(); linkServer.Pump(); }
            var faceA = BattleEngine.ForLink(a, playerLook: "PLAYER", foeLook: "DAWN");
            var faceB = BattleEngine.ForLink(b, playerLook: "DAWN", foeLook: "PLAYER");
            var (name, x, y) = Place("Route201", -1, -1);
            game.Map = MapDatabase.Get(name);
            if (x >= 0) game.Player.SetPosition(x, y, Direction.Up);
            game.BattleRenderer.SetArena(game.Map, x, y);
            ShowLink(faceA, faceB);
            game.State = GameState.Battle;
            Console.WriteLine("link: Lucas's screen in Galar's manner, Dawn's as Classic, the same turn of the same battle");
            // Each frame of both, to the commands (the leads' split passes on A's screen on the way)
            int n = 0;
            for (int i = 0; i < 4000 && (faceA.HUD.MenuState != BattleMenuState.Main || faceB.HUD.MenuState != BattleMenuState.Main); i++)
            {
                engine.FixedCameraManner = galar;
                Pass(0.1);
                if (i % 6 == 0 && game.BattleRenderer.Split != null && n < 6)
                {
                    Half($"bc_link_a_galar_split_{n:00}");
                    Console.WriteLine($"  Lucas's screen splits {game.BattleRenderer.Split?.In:0.00} in, the clock {faceA.Anim.Time:0.00}");
                    n++;
                }
                ReadOn(faceA);
                ReadOn(faceB);
            }
            // Five seconds into the commands on both: the two screens at the same moment
            Pass(5.0);
            foreach (var (shown, hidden, tag, manner) in new[] { (faceA, faceB, "a_galar", galar), (faceB, faceA, "b_classic", CameraManners.Classic) })
            {
                ShowLink(shown, hidden);
                engine.FixedCameraManner = manner;
                Pass(0.2);
                Half($"bc_link_{tag}_menu");
                Console.WriteLine($"  bc_link_{tag}_menu: {game.BattleRenderer.Shot}, the menu open {shown.Anim.MenuAge:0.00} s, the clock {shown.Anim.Time:0.00}");
            }
            game.Battle = null;
            linkHidden = null;
            linkServer = null;
            game.State = GameState.Overworld;
        }

        // ---- a recorded battle watched: filmed by the watcher's manner, no cycle (no commands)
        if (part("replay"))
            foreach (var manner in manners.Where(m => m.Name is "galar" or "sinnoh" or "kalos"))
            {
                engine.FixedCameraManner = manner;
                Console.WriteLine($"manner {manner.Name}: a recorded battle watched");
                var mine = new Party();
                mine.Add(new Pokemon(PokemonDatabase.Get("Monferno")!, 20));
                mine.Add(new Pokemon(PokemonDatabase.Get("Staravia")!, 18));
                var tristan = new Trainer { Id = "harness_replay", Name = "Tristan", TrainerClass = "Youngster", PrizeMoney = 64, Ai = PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.Basic };
                tristan.Party.Add(new Pokemon(PokemonDatabase.Get("Shinx")!, 17));
                tristan.Party.Add(new Pokemon(PokemonDatabase.Get("Budew")!, 18));
                var fought = new BattleEngine(new BattleSetup
                {
                    PlayerParty = mine, Inventory = inventory, Pokedex = pokedex, Trainers = new List<Trainer> { tristan },
                    Random = new BattleRandom(20261020), Rules = Ruleset.Platinum, PlayerName = "Lucas"
                });
                for (int i = 0; i < 40000 && !fought.IsBattleOver; i++)
                {
                    fought.Update(1f / 60f);
                    if (fought.IsLearningMove) fought.ChooseLearn(1);
                    else if (fought.IsWaitingForConfirm) fought.ConfirmMessage();
                    else if (fought.IsChoosingReplacement) fought.SelectSwitch(Enumerable.Range(0, mine.Count).First(j => !mine.Members[j].IsFainted && fought.PlayerSlots.All(s => s.Pokemon != mine.Members[j])));
                    else if (fought.HUD.MenuState == BattleMenuState.Main) fought.SelectMainMenuOption(0);
                    else if (fought.HUD.MenuState == BattleMenuState.Moves) fought.SelectMove(0);
                }
                var file = fought.RecordFile(new DateTime(2026, 10, 20));
                if (file == null) { Console.WriteLine("  !! the battle left no recording"); continue; }
                var watched = BattleEngine.ForReplay(file);
                game.Map = MapDatabase.Get("Sinnoh");
                game.BattleRenderer.SetArena(game.Map);
                game.Battle = watched;
                game.State = GameState.Battle;
                ReelEvery(watched, $"bc_{manner.Name}_replay", 40, 0.25);
            }

        engine.FixedCameraManner = null;
        party.Members.Clear();
        party.Members.AddRange(savedParty);
        game.Battle = null;
        game.State = GameState.Overworld;
    }
}
