partial class Harness
{
    /// <summary>
    /// Z5's story scenes on screen (the <c>alolastory</c> mode, not part of <c>all</c>; plan 23 · Z5, "Melemele's last door
    /// and Z5's shots"): a new game of Alola taken to <c>VAR_ALOLA_STORY</c> 7 as <c>AlolaIslandsTests</c> does, then each of
    /// the chapter's scenes from the marina to Lana's trial started as the game starts it (a step onto its trigger, or a
    /// word to its person), each shot at its first line (<c>_a</c>), its fourth (<c>_b</c>), its battle's menu where it
    /// has one (<c>_battle</c>) and the field after it (<c>_after</c>): <c>was30</c> the marina's grunts and Ilima,
    /// <c>was31</c> the festival, <c>was32</c> Ilima before Verdant Cavern, <c>was33</c> Nebby in the meadow, <c>was34</c>
    /// Hau on Route 3, <c>was35</c> Hala's grand trial, <c>was36</c> the boat to Akala, <c>was37</c> Hau in Paniola,
    /// <c>was38</c> Mallow at the ranch, <c>was39</c> Gladion on Route 5, <c>was40</c> Lana and her Lapras,
    /// <c>was41</c> the Lapras ride across Brooklet Hill and <c>was42</c> the totem Wishiwashi. The totem Gumshoos is
    /// <c>akala</c>'s <c>wz29</c> to <c>wz35</c>. Every battle is won with the foes left a single HP. Each scene prints
    /// the story's count after it.
    /// </summary>
    public void AlolaStoryMode()
    {
        var story = game.Story;
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Win()
        {
            var fight = game.Battle;
            for (int guard = 0; guard < 400 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    foreach (var foe in fight.EnemyParty?.Members ?? new List<Pokemon>()) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    if (fight.EnemyPokemon is { } met) met.CurrentHP = Math.Min(met.CurrentHP, 1);
                    foreach (var slot in fight.Core.EnemySlots) if (slot.Pokemon is { } p) p.CurrentHP = Math.Min(p.CurrentHP, 1);
                    var lead = fight.PlayerPokemon;
                    if (lead != null) lead.CurrentHP = lead.MaxHP;
                    fight.SelectMainMenuOption(0);
                    int hurts = lead == null ? 0 : Math.Max(0, lead.Moves.FindIndex(m => m.Data.Category != MoveCategory.Status));
                    fight.SelectMove(hurts);
                    if (fight.HUD.MenuState == BattleMenuState.SelectTarget) fight.SelectTarget(0);
                }
                else Confirm(fight);
                Skip(0.5);
            }
            Until(() => FieldAgain(), "the field again", 1500);
            Frames(10);
        }
        // Reads a scene to its end, shooting its first and fourth lines and its battle's menu
        void Scene(string name)
        {
            int lines = 0;
            bool battled = false;
            int idle = 0;
            for (int i = 0; i < 6000; i++)
            {
                if (game.State == GameState.Battle)
                {
                    Skip(1.0);
                    ToMainMenu(game.Battle);
                    if (!battled) Shot($"{name}_battle");
                    battled = true;
                    Win();
                    continue;
                }
                idle = !engine.ScriptRunning && !Box().IsActive && game.State == GameState.Overworld ? idle + 1 : 0;
                if (idle > 30) break;
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion)
                {
                    lines++;
                    if (lines == 1) { Frames(6); Shot($"{name}_a"); }
                    if (lines == 4) { Frames(6); Shot($"{name}_b"); }
                    Box().Advance(); Frames(2);
                }
                else if (game.State is not GameState.Overworld and not GameState.Dialogue and not GameState.Battle && !engine.ScriptRunning)
                {
                    // A screen a scene opens (the Z-Ring's, a ride's) is put away
                    InputManager.Feed(ActionFrame.Press(GameAction.Cancel)); Frames(1); InputManager.Feed(null); Frames(10);
                }
                else Frames(1);
            }
            game.LocationSign.Hide();
            Frames(10);
            Shot($"{name}_after");
            Console.WriteLine($"  {name}: {lines} lines, battle {battled}, VAR_ALOLA_STORY {story.Var("VAR_ALOLA_STORY")}, at {game.Map.Name} ({game.Player.GridX}, {game.Player.GridY})");
        }
        // Steps onto a scene's trigger from the open side, as the game finds it
        void StepOnto(string map, string script, string name)
        {
            var m = MapDatabase.Get(map);
            var t = m.Triggers.FirstOrDefault(t => t.Script == script);
            if (t == null) { Console.WriteLine($"  !! {map} has no trigger {script}"); return; }
            int cx = t.X + t.Width / 2, cy = t.Y + t.Depth / 2;
            (int Dx, int Dy, Direction Way)[] ways = t.Depth > t.Width
                ? new[] { (-1, 0, Direction.Right), (1, 0, Direction.Left), (0, 1, Direction.Up), (0, -1, Direction.Down) }
                : new[] { (0, 1, Direction.Up), (0, -1, Direction.Down), (-1, 0, Direction.Right), (1, 0, Direction.Left) };
            foreach (var (dx, dy, way) in ways)
            {
                int fx = (way is Direction.Right or Direction.Left) ? (way == Direction.Right ? t.X - 1 : t.X + t.Width) : cx;
                int fy = (way is Direction.Up or Direction.Down) ? (way == Direction.Up ? t.Y + t.Depth : t.Y - 1) : cy;
                if (!m.InBounds(fx, fy) || !m.IsWalkable(fx, fy)) continue;
                At(map, fx, fy, way);
                game.EnterArea();
                Frames(2);
                engine.Steering = (way, false);
                Until(() => engine.ScriptRunning, $"the scene {script}", 120);
                engine.Steering = null;
                Scene(name);
                return;
            }
            Console.WriteLine($"  !! no open side before {script}");
        }
        // Speaks to someone of a place
        void Talk(string map, string key, string place, string name)
        {
            var m = MapDatabase.Get(map);
            var npc = m.FindPerson(key, place);
            if (npc == null) { Console.WriteLine($"  !! {place} has nobody called {key}"); return; }
            foreach (var (dx, dy, facing) in new[] { (0, 1, Direction.Up), (0, -1, Direction.Down), (-1, 0, Direction.Right), (1, 0, Direction.Left) })
            {
                int x = npc.GridX + dx, y = npc.GridY + dy;
                if (!m.InBounds(x, y) || !m.IsWalkable(x, y)) continue;
                At(map, x, y, facing);
                Frames(2);
                game.Interact();
                Frames(2);
                Scene(name);
                return;
            }
            Console.WriteLine($"  !! nowhere to stand before {key}");
        }

        engine.NewGameRegion = "Alola";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(30);
        var lead = new Pokemon(PokemonDatabase.Get("Litten")!, 18);
        game.Party.Members.Insert(0, lead);
        story.ChooseStarter("Litten");
        story.SetVar("VAR_ALOLA_STORY", 7);
        // What the chapter's first scenes leave behind: the kahuna and Hau home in Iki Town since the starters
        story.Unset("FLAG_ALOLA_HIDE_HALA_IKI");
        story.Unset("FLAG_ALOLA_HIDE_HAU_IKI");

        StepOnto("Melemele", "Marina", "was30_marina");
        StepOnto("Melemele", "Festival", "was31_festival");
        Talk("Melemele", "ilima", "alola_route_2", "was32_ilima_at_the_cavern");
        // The trial itself is the akala mode's (wz29 to wz35): its dens and totem are played here without a shot
        foreach (string flag in new[] { "FLAG_ALOLA_VERDANT_DEN_1", "FLAG_ALOLA_VERDANT_DEN_2", "FLAG_ALOLA_VERDANT_DEN_3" }) story.Set(flag);
        Talk("VerdantCavern", "totem", "verdant_cavern", "was32z_verdant_totem");
        StepOnto("Melemele", "Nebby", "was33_nebby_in_the_meadow");
        StepOnto("Melemele", "HauOnRoute3", "was34_hau_on_route3");
        StepOnto("Melemele", "GrandTrial", "was35_grand_trial");
        StepOnto("Melemele", "SailToAkala", "was36_sail_to_akala");
        StepOnto("Akala", "HauInPaniola", "was37_hau_in_paniola");
        StepOnto("Akala", "Mallow", "was38_mallow_at_the_ranch");
        StepOnto("Akala", "Gladion", "was39_gladion_on_route5");
        Talk("Akala", "lana", "brooklet_hill", "was40_lana");
        Talk("Akala", "splash", "brooklet_hill", "was41_brooklet_splash");
        Talk("Akala", "splash_2", "brooklet_hill", "was41b_brooklet_splash");
        Talk("Akala", "totem", "brooklet_hill", "was42_totem_wishiwashi");
        game.DeleteSave();
    }
}
