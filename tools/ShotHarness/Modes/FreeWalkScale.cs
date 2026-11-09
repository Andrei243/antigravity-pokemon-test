using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;

partial class Harness
{
    /// <summary>
    /// Plan 30 · FM6: scale in a region whose people are grown, the region's kit laid as grown (engine.RegionKitOverride:
    /// no region's file the harness's modes walk says so). fw47: Aquacorde's promenade with Pokémon of four sizes beside the
    /// player, as today (chibi, Platinum's camera) and grown (live rigs, the Pokémon at a grown region's scale, the Kalos
    /// camera); fw46: Roark's band with his portrait and the battle drawn in the grown kit; fw45: his battle with the grown
    /// trainers at the send-out's throw, at the menu and speaking. Prints the sizes, the cards, the camera and the looks.
    /// </summary>
    void FreeWalkScale()
    {
        if (!Wanted("fw45") && !Wanted("fw46") && !Wanted("fw47")) return;

        // ---- fw47: the field's scale
        if (Wanted("fw47"))
        {
            engine.NewGameRegion = "Kalos";
            engine.StartNewGame();
            game.State = GameState.Overworld;
            game.SaveSlot = 1;
            game.LocationSign.Hide();
            At("Kalos", CrowdX, CrowdZ, Direction.Down);
            Frames(20);
            var map = game.Map;
            var placed = new List<NPC>();
            // Four Pokémon of four sizes in a row south of the player, and two people beside them
            (string Species, int Dx)[] row = { ("Machop", -3), ("Pikachu", -2), ("Lucario", 2), ("Steelix", 4) };
            foreach (var (species, dx) in row)
            {
                int x = CrowdX + dx, z = CrowdZ + 2;
                if (!map.IsWalkable(x, z) || map.NPCs.At(x, z) != null) { Console.WriteLine($"  !! no room for {species} at ({x}, {z})"); continue; }
                var npc = new NPC { Name = $"fw47_{species}", NpcType = NPC.PokemonType, Species = species, GridX = x, GridY = z, Facing = Direction.Down };
                map.NPCs.Add(npc);
                placed.Add(npc);
            }
            foreach (var (look, dx) in new[] { ("CLERK", -1), ("BLACKBELT", 1) })
            {
                int x = CrowdX + dx, z = CrowdZ + 1;
                if (!map.IsWalkable(x, z) || map.NPCs.At(x, z) != null) continue;
                var npc = new NPC { Name = $"fw47_{look}", NpcType = look, GridX = x, GridY = z, Facing = Direction.Down };
                map.NPCs.Add(npc);
                placed.Add(npc);
            }
            Frames(30);
            Shot("fw47_scale_chibi");
            foreach (var (species, _) in row)
                Console.WriteLine($"  {species}: {FieldSprites.MetresOf(species):0.0} m, chibi {FieldSprites.FieldHeight(FieldSprites.MetresOf(species)):0.00} tiles on {FieldSprites.ClassOf(FieldSprites.FieldHeight(FieldSprites.MetresOf(species)))}, "
                    + $"grown {FieldSprites.GrownHeight(FieldSprites.MetresOf(species)):0.00} tiles on {FieldSprites.GrownClassOf(FieldSprites.GrownHeight(FieldSprites.MetresOf(species)))}");

            var camera = map.Camera;
            engine.RegionKitOverride = CharacterKit.Grown;
            map.Camera = FieldCamera.Kalos;
            game.World.ForgetAll();
            WaitForTheCrowd();
            Frames(30);
            Shot("fw47_scale_grown");
            TellTheCrowd("fw47_scale_grown");
            Console.WriteLine($"  the camera: {MapScene.ViewOf(map)} (Platinum's {MapScene.ViewOf(new Map(1, 1))})");
            ShotCrop("fw47_scale_grown_close", 560, 300, 800, 450, 2);
            Board("fw47_scale", new List<(string, string)> { ("fw47_scale_chibi", "today: chibi, 59°"), ("fw47_scale_grown", "grown: 0.85 × metres, 50°") },
                new Rectangle(0, 0, 1920, 1080), 1);

            map.Camera = camera;
            engine.RegionKitOverride = null;
            game.World.ForgetAll();
            foreach (var npc in placed) map.NPCs.Remove(npc);
            Frames(10);
            game.DeleteSave();
        }

        // ---- fw46 and fw45: a Gym Leader's band and battle drawn in the grown kit
        if (Wanted("fw45") || Wanted("fw46"))
        {
            engine.NewGameRegion = null;
            engine.StartNewGame();
            PastTheOpening();
            game.SaveSlot = 1;
            // A lead strong enough to fell his first Pokémon, so he speaks
            var lead = new Pokemon(PokemonDatabase.Get("Empoleon")!, 60);
            game.Party.Members.Insert(0, lead);
            engine.RegionKitOverride = CharacterKit.Grown;
            // The grown looks made before the band asks (a band never waits, and the first run meshes them)
            foreach (var look in new[] { "ROARK", PlayerIdentity.Character })
                CharacterModels.Get(CharacterStyle.InKit(look, CharacterKit.Grown), game.RenderContext.Shaders);
            At("OreburghGym", 5, 4, Direction.Up);
            game.ArriveOnMap();
            Frames(4);
            // Spoken to from before him, as the Gym's script plays it: his words read on until his band or his battle
            game.Interact();
            bool began = false;
            for (int i = 0; i < 2000 && !BandLit() && game.State != GameState.Battle; i++)
            {
                if (game.Dialogue.IsActive && game.Dialogue.IsCurrentLineComplete && !game.Dialogue.IsQuestion) { game.Dialogue.Advance(); Frames(2); }
                else if (engine.Choice.IsOpen) { engine.Choice.Confirm(); Frames(2); }
                else Frames(1);
                began = true;
            }
            if (!began || (!BandLit() && game.State != GameState.Battle)) Console.WriteLine("  !! Roark's battle never began");
            else
            {
                for (int i = 0; i < 900 && !BandLit() && game.State != GameState.Battle; i++) Frames(1);
                if (Wanted("fw46"))
                {
                    if (BandLit()) { Shot("fw46_band"); Console.WriteLine($"  the band: their portrait of {game.BandTheirLook}, the player's of {game.BandOwnLook ?? "nobody"}, {game.BandRendered} rendered"); }
                    else Console.WriteLine("  !! the band never lit");
                }
                for (int i = 0; i < 900 && game.State != GameState.Battle; i++) Frames(1);
                var b = game.Battle;
                if (b == null) Console.WriteLine("  !! the battle never began");
                else
                {
                    // His send-out: the throw at its height
                    for (int i = 0; i < 400 && !(b.Anim.EnemyTrainer != null && b.Anim.EnemyTrainerExit >= 0.05f); i++)
                    {
                        if (b.HUD.MenuState == BattleMenuState.Main) break;
                        Frames(1);
                    }
                    Shot("fw45_battle_throw");
                    ToMainMenu(b);
                    Skip(0.5);
                    Shot("fw45_battle_menu");
                    Console.WriteLine($"  the battle's looks: theirs {b.Anim.EnemyTrainerLook}, the player's {b.Anim.PlayerTrainerLook}; grown trainers {game.BattleRenderer.GrownTrainers}, drawn at {BattleRenderer.GrownBattleShare} of the battle's scale");
                    // Read on until he speaks at his first one's fall
                    for (int i = 0; i < 60 && b.CurrentSpeaker == null && !b.IsBattleOver; i++)
                    {
                        if (b.HUD.MenuState == BattleMenuState.Main) b.SelectMove(0);
                        else Confirm(b);
                        Skip(0.3);
                    }
                    Skip(0.8);
                    if (b.CurrentSpeaker != null) Shot("fw45_battle_speaking");
                    else Console.WriteLine("  !! Roark never spoke");
                    Console.WriteLine($"  speaker: {b.CurrentSpeaker ?? "nobody"}, line: {b.CurrentMessage}");
                }
            }
            engine.RegionKitOverride = null;
            game.DeleteSave();
        }
    }
}
