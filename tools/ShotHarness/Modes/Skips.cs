partial class Harness
{
    // ---------------------------------------------------------------- scenes skipped (plan 12 · Q20)

    // What a scene left behind, for the side-by-side telling of a scene played and the same scene skipped: where the
    // player and everyone of the map stand and whether they are on it, the story, the team, the bag, and the rest
    // (the map, the game's state, the camera at rest or not, a script still running or not)
    sealed record SceneLeft(List<string> People, List<string> Flags, List<string> Variables, string Team, string Bag, string Else);

    SceneLeft WhatASceneLeft()
    {
        var map = game.Map;
        var me = game.Player;
        var on = map.NPCs.ToHashSet();
        // (On the map of Sinnoh, those within sight of the scene: its people are thousands)
        var people = new List<string> { $"player at {me.GridX},{me.GridY} facing {me.Facing}" };
        people.AddRange(map.Everyone.Where(n => !map.IsStreamed || (Math.Abs(n.GridX - me.GridX) <= 40 && Math.Abs(n.GridY - me.GridY) <= 40))
            .OrderBy(n => n.Key ?? n.Name, StringComparer.Ordinal).ThenBy(n => n.GridX).ThenBy(n => n.GridY)
            .Select(n => $"{n.Key ?? n.Name} at {n.GridX},{n.GridY} facing {n.Facing}, {(on.Contains(n) ? "on the map" : "off the map")}"));
        var told = game.Story.Snapshot();
        return new SceneLeft(people, told.Flags, told.Variables.Select(v => $"{v.Key} = {v.Value}").ToList(),
            string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} Lv{p.Level}")),
            string.Join(", ", inventory.AllItems.Where(i => i.Quantity > 0).OrderBy(i => i.Name, StringComparer.Ordinal).Select(i => $"{i.Name} x{i.Quantity}")),
            $"map {map.Name}, state {game.State}, camera at rest {game.World.Camera.AtRest}, script running {engine.ScriptRunning}, a question open {engine.Choice.IsOpen}");
    }

    // Side by side: a line begins with !! where the skipped scene left something else than the played one
    void TellBothWays(string scene, SceneLeft played, SceneLeft skipped)
    {
        void Compare(string what, IReadOnlyList<string> a, IReadOnlyList<string> b)
        {
            bool same = a.SequenceEqual(b);
            Console.WriteLine($"{(same ? "" : "!! ")}skip, {scene}, {what}: {a.Count} played, {b.Count} skipped, {(same ? "the same" : "DIFFERENT")}");
            foreach (string only in a.Except(b)) Console.WriteLine($"!!   played only : {only}");
            foreach (string only in b.Except(a)) Console.WriteLine($"!!   skipped only: {only}");
        }
        for (int i = 0; i < Math.Max(played.People.Count, skipped.People.Count); i++)
        {
            string a = i < played.People.Count ? played.People[i] : "-", b = i < skipped.People.Count ? skipped.People[i] : "-";
            Console.WriteLine($"{(a == b ? "" : "!! ")}skip, {scene}, played : {a}\n{(a == b ? "" : "!! ")}skip, {scene}, skipped: {b}");
        }
        Compare("flags", played.Flags, skipped.Flags);
        Compare("variables", played.Variables, skipped.Variables);
        Compare("the team", new[] { played.Team }, new[] { skipped.Team });
        Compare("the bag", new[] { played.Bag }, new[] { skipped.Bag });
        Compare("the rest", new[] { played.Else }, new[] { skipped.Else });
    }

    // Three of the later chapters' biggest scenes, each played to its end and then, from the same story, skipped by
    // the hold, with what each left told side by side: the speech in Team Galactic's hall (the camera kit, the roar's
    // shake, the ranks leaving), the meeting in Canalave's library (eleven walks, three people sent off, the
    // explosion's ripple and its lines over a black screen) and the torn pillar (the three of the lakes shown and
    // gone, sparkles, a ripple, the camera sent and brought back), which ends at Cynthia's question. No shots: a line
    // that begins with !! is something a skip left otherwise than the scene does. Not part of "all".
    public void SkipsMode()
    {
        var story = game.Story;
        game.State = GameState.Overworld;
        game.LocationSign.Hide();

        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        // Reads the scene's lines on, answering nothing, until something holds
        void ReadUntil(Func<bool> holds, string what, int most = 6000)
        {
            for (int i = 0; i < most && !holds(); i++)
            {
                if (Box().IsActive && !Box().IsCurrentLineComplete) Box().FinishLine();
                else if (Box().IsActive && !Box().IsQuestion) Box().Advance();
                Frames(1);
            }
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Come(string map, int x, int y, Direction facing)
        {
            At(map, x, y, facing);
            // (as a door brings the player: everyone back where the flags have them, and the place's own script)
            game.ArriveOnMap();
            Frames(3);
        }

        void BothWays(string scene, string map, Action given, Action come, Func<bool> over, Action? after = null)
        {
            given();
            var before = story.Snapshot();
            // Whoever a scene walks about a room stays where it left them until the room's own script puts them back,
            // so the second telling begins with everyone where the first found them: the story, and where each stands
            var stood = MapDatabase.Get(map).Everyone.Select(n => (Who: n, n.GridX, n.GridY, n.Facing)).ToList();
            engine.Settings.SkipScenes = SkipScenes.Off;
            int skips = game.ScenesSkipped;

            come();
            Until(() => game.Scene != null, $"{scene} beginning", 300);
            string? key = game.Scene;
            ReadUntil(over, $"the end of {scene}");
            Frames(60);
            var played = WhatASceneLeft();
            Console.WriteLine($"skip, {scene}: played, scene {key}, seen {key != null && story.HasSeenScene(key)}");
            after?.Invoke();
            ReadUntil(() => !engine.ScriptRunning && !Box().IsActive, $"{scene} played out");

            story.Restore(before);
            foreach (var (who, x, y, facing) in stood) (who.GridX, who.GridY, who.Facing) = (x, y, facing);
            engine.Settings.SkipScenes = SkipScenes.Any;
            come();
            Until(() => game.Scene != null, $"{scene} beginning again", 300);
            Frames(30);
            game.SkipHeld = true;
            Until(() => game.Skipping, $"the dark closing over {scene}", 240);
            Until(() => !game.Skipping, $"the dark opening after {scene}", 300);
            game.SkipHeld = false;
            ReadUntil(over, $"the end of {scene}, skipped");
            Frames(60);
            var skipped = WhatASceneLeft();
            Console.WriteLine($"skip, {scene}: skipped {game.ScenesSkipped - skips} time(s), seen {key != null && story.HasSeenScene(key)}");
            after?.Invoke();
            ReadUntil(() => !engine.ScriptRunning && !Box().IsActive, $"{scene} played out, skipped");
            engine.Settings.SkipScenes = SkipScenes.Off;
            TellBothWays(scene, played, skipped);
        }

        bool Quiet() => !engine.ScriptRunning && !Box().IsActive;

        // ---- Team Galactic's hall: Looker waves the player over, Cyrus's speech from the dais, the roar, the hall empty
        BothWays("the speech", "GalacticHQHall",
            () => story.SetVar("VAR_GALACTIC_HQ_HALL_STATE", 0),
            () =>
            {
                Come("GalacticHQHall", 24, 6, Direction.Down);
                ReadUntil(Quiet, "the hall's arrival");
                At("GalacticHQHall", 21, 12, Direction.Left);
                engine.Steering = (Direction.Left, false);
                Until(() => engine.ScriptRunning, "the step onto the hall's trigger", 120);
                engine.Steering = null;
            },
            Quiet);

        // ---- Canalave's library: the meeting at the table, the explosion, the three sent off
        BothWays("the meeting", "CanalaveLibrary3F",
            () =>
            {
                foreach (string flag in new[] { "FLAG_HIDE_CANALAVE_LIBRARY_RIVAL", "FLAG_HIDE_CANALAVE_LIBRARY_COUNTERPART", "FLAG_HIDE_CANALAVE_LIBRARY_ROWAN" }) story.Unset(flag);
                story.SetVar("VAR_CANALAVE_LIBRARY_STATE", 1);
            },
            () => Come("CanalaveLibrary3F", 3, 3, Direction.Left),
            Quiet);

        // ---- the torn pillar: Cynthia too late, the three of the lakes, and her question, where the scene ends
        BothWays("the torn pillar", "SpearPillarDistorted",
            () =>
            {
                foreach (string flag in new[] { "FLAG_HIDE_SPEAR_PILLAR_GRUNTS", "FLAG_HIDE_SPEAR_PILLAR_MARS_JUPITER", "FLAG_HIDE_SPEAR_PILLAR_CYRUS", "FLAG_UNLOCKED_VS_SEEKER_LVL_3", "FLAG_SPEAR_PILLAR_IS_DISTORTED" })
                    story.Set(flag);
                story.SetVar("VAR_SPEAR_PILLAR_STATE", 3);
                story.SetVar("VAR_SPEAR_PILLAR_DISTORTED_STATE", 1);
            },
            () => Come("SpearPillarDistorted", 30, 30, Direction.Up),
            () => engine.Choice.IsOpen,
            // "Are you ready?": not yet, so the scene's script ends where it stands
            () => { Frames(10); engine.Choice.Back(); Frames(4); });
    }
}
