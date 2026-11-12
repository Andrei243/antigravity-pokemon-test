using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    /// <summary>
    /// What Kalos's gate left owed (plan 30, "The gate opened"; not part of <c>all</c>; <c>kalosgrown</c>), in a new game of
    /// Kalos free and grown on its own file: the player before Viola stood back as she speaks (<c>kg01</c>, <c>kg02</c> the
    /// same frame cropped), her band (<c>kg_band_viola</c>), the Trainer Card's grown portrait, the designer's figure and
    /// the photo studio's (<c>kg10</c> to <c>kg13</c>), a grown skater pushing and gliding (<c>kg20</c> to <c>kg22</c>),
    /// fishing and surfing on a pond of the harness's own (<c>kg30</c> to <c>kg32</c>), Lumiose's avenue, its station and
    /// a restaurant with their crowds (<c>kg40</c> to <c>kg44</c>), a species of ours met in Route 2's grass (<c>kg50</c>),
    /// and plan 30's free hand-offs on Kalos's ground (<c>fw31</c> a ledge's hop, <c>fw34</c> Aquacorde's bridge,
    /// <c>fw35</c> a door walked into, <c>fw39</c> a script's walk). It prints what the field drew (live, impostors, sprites).
    /// </summary>
    public void KalosGrownMode()
    {
        engine.NewGameRegion = "Kalos";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        foreach (string species in new[] { "Froakie", "Fletchling", "Pikachu" })
            game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, 30));
        FieldRigs.Patient = true;
        Frames(10);
        Console.WriteLine($"kit {engine.KitHere}, movement {engine.MovementHere.Directions}, the player's look {PlayerIdentity.Character}");
        void Drew(string what) =>
            Console.WriteLine($"  {what}: live {game.World.LiveRigs.Live}, impostors {game.World.LiveRigs.Impostors}, sprites {game.World.LiveRigs.Sprites}, waiting {game.World.LiveRigs.Waiting}");

        // ---- talking to someone north of the player: the grown body stands back half a tile
        At("SantaluneCity_Gym", 6, 5, Direction.Up); Frames(4);
        Shot("kg00_before_viola");
        game.Interact();
        for (int i = 0; i < 120 && !game.Dialogue.IsActive; i++) Frames(1);
        game.Dialogue.FinishLine();
        Frames(30);
        Shot("kg01_talking_to_viola");
        ShotCrop("kg02_talking_to_viola_close", 760, 160, 400, 225, 2);
        Drew("the Gym");
        Console.WriteLine($"  talk: state {game.State}, script {engine.ScriptRunning}, facing {game.Player.Facing} at {game.Player.GridX},{game.Player.GridY}, ahead {game.Map.NPCs.At(game.Player.GridX, game.Player.GridY - 1)?.Name ?? "nobody"}, stood back {game.World.TalkBackShare:0.00}");
        for (int i = 0; i < 1500 && game.State != GameState.Battle && !BandLit(); i++)
        {
            if (game.Dialogue.IsActive && game.Dialogue.IsCurrentLineComplete && !game.Dialogue.IsQuestion) { game.Dialogue.Advance(); Frames(2); }
            else if (engine.Choice.IsOpen) { engine.Choice.Confirm(); Frames(2); }
            else Frames(1);
        }
        ShotBand("kg");
        for (int i = 0; i < 600 && game.State != GameState.Battle; i++) Frames(1);
        if (game.State == GameState.Battle) { Skip(1.0); ToMainMenu(game.Battle); Shot("kg03_violas_battle"); }
        // Back to the field, the battle let go of
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        foreach (string species in new[] { "Froakie", "Fletchling", "Pikachu" })
            game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, 30));
        Frames(10);

        // ---- the Trainer Card's portrait, the designer's figure and the photo studio's, in the grown kit
        At("Kalos", 340, 524, Direction.Down); game.LocationSign.Hide(); Frames(10);
        game.ChooseFromStartMenu(StartMenuChoice.Trainer);
        Frames(30);
        Shot("kg10_card_front");
        game.TrainerCard.AskForDesigner();
        Frames(40);
        Shot("kg11_card_designer");
        var d = game.CardDesigner;
        d.Move(2);
        for (int k = 0; k < 6 && d.Design.Pose != CardPoseKind.Sitting; k++) d.Change(1);
        Frames(40);
        Shot("kg12_card_designer_sitting");
        d.Cancel(); d.Cancel(); Frames(10);
        game.TrainerCard.Close(); Frames(10);
        // The studio stands in Jubilife's boutique; the kit is laid as Kalos's there for the shot
        engine.RegionKitOverride = CharacterKit.Grown;
        game.Story.Set(GameEngine.StudioFrameFlag);
        At("JubilifeBoutique", 7, 6, Direction.Right); game.LocationSign.Hide(); Frames(20);
        game.OpenPhotoStudio(); Frames(60);
        Shot("kg13_photo_studio");
        game.PhotoStudio.Cancel(); Frames(10);
        engine.RegionKitOverride = null;

        // ---- a grown skater on Vaniville's south lane: pushing, and gliding once let go
        game.Story.Set(SkateRules.Flag);
        At("Kalos", 246, 723, Direction.Right); game.LocationSign.Hide();
        engine.Steering = (Direction.Right, true);
        Frames(14);
        Console.WriteLine($"  skating: {game.Player.Mode}, moving {game.Player.IsMoving}");
        Shot("kg20_skating");
        ShotCrop("kg21_skating_close", 760, 400, 400, 225, 2);
        Frames(7);
        ShotCrop("kg21b_skating_close_later", 760, 400, 400, 225, 2);
        engine.Steering = null;
        Frames(3);
        Console.WriteLine($"  let go: {game.Player.Mode}, sliding {game.Player.IsSliding}");
        ShotCrop("kg22_skating_glide_close", 760, 400, 400, 225, 2);
        Frames(60);
        game.Story.Unset(SkateRules.Flag);

        // ---- fishing and surfing, grown, on a pond of the harness's own (the kit laid as grown on it)
        engine.RegionKitOverride = CharacterKit.Grown;
        var pond = BuildPond();
        game.Map = pond;
        game.Player.SetPosition(5, 5, Direction.Down);
        CharacterModels.Get(CharacterStyle.Holding(CharacterStyle.InKit(PlayerIdentity.Character, CharacterKit.Grown), HeldProp.Rod), game.RenderContext.Shaders);
        game.Player.Posed = PokemonPlatinumEngine.Overworld.Stance.Fish;
        Frames(3); Skip(0.6);
        Shot("kg30_fishing");
        ShotCrop("kg31_fishing_close", 560, 300, 800, 450, 2);
        game.Player.Posed = null;
        game.Player.SetPosition(7, 8, Direction.Left);
        game.Player.SetMode(TravelMode.Surfing);
        Frames(3); Skip(0.6);
        Shot("kg32_surfing");
        ShotCrop("kg33_surfing_close", 560, 300, 800, 450, 2);
        game.Player.SetMode(TravelMode.OnFoot);
        engine.RegionKitOverride = null;

        // ---- Lumiose with its crowds: the avenue, the station, a restaurant, the Pokémon Center
        At("Kalos", 360, 392, Direction.Up); game.LocationSign.Hide(); Frames(20);
        Shot("kg40_lumiose_avenue"); Drew("the avenue");
        At("Kalos", 346, 352, Direction.Up); game.LocationSign.Hide(); Frames(20);
        Shot("kg41_lumiose_boulevard"); Drew("the boulevard");
        foreach (var (name, map) in new[] { ("kg42_lumiose_station", "LumioseCity_Station"), ("kg43_lumiose_bistro", "LumioseCity_BistroDeux"), ("kg44_lumiose_center", "LumioseCity_PokemonCenter_1F") })
        {
            var m = MapDatabase.Get(map);
            var way = m.Warps.FirstOrDefault();
            int x = way != null ? way.SourceX : m.Width / 2, y = way != null ? Math.Max(1, way.SourceY - 1) : m.Height - 3;
            At(map, x, y, Direction.Up); game.LocationSign.Hide(); Frames(20);
            Shot(name);
            Drew(map);
            foreach (var npc in m.NPCs.Where(n => m.StanceOf(n) != PokemonPlatinumEngine.Overworld.Stance.Stand)) Console.WriteLine($"    {npc.Name}: {m.StanceOf(npc)} on {m.SeatUnder(npc)?.Type.ToString() ?? "the floor"}");
        }

        // ---- a species of ours met in Kalos's grass
        var ours = PokemonDatabase.Ours().Where(s => s.DexNumber >= 2660 && s.DexNumber < 2720).OrderBy(s => s.DexNumber).FirstOrDefault();
        if (ours != null)
        {
            At("Kalos", 255, 624, Direction.Up); game.LocationSign.Hide(); Frames(4);
            game.StartWildBattle(new WildEncounterEntry { SpeciesName = ours.Name, MinLevel = 120, MaxLevel = 120 });
            for (int i = 0; i < 600 && game.State != GameState.Battle; i++) Frames(1);
            if (game.State == GameState.Battle) { Skip(1.0); ToMainMenu(game.Battle); Shot("kg50_ours_in_kalos_grass"); Console.WriteLine($"  met {ours.Name} (No. {ours.DexNumber})"); }
            engine.StartNewGame();
            game.State = GameState.Overworld;
            game.LocationSign.Hide();
            Frames(10);
        }

        // ---- plan 30's free hand-offs on Kalos's ground
        FreeHandOffs();
        FieldRigs.Patient = false;
    }

    /// <summary>
    /// Plan 30's free hand-offs shot on Kalos's own ground (<c>fw31</c> to <c>fw39</c>, those Kalos has near its start): a
    /// ledge's hop, the bridge's deck, a door walked into from its front, a script's walk after centring.
    /// </summary>
    private void FreeHandOffs()
    {
        var kalos = MapDatabase.Get("Kalos");
        // A ledge south of the first towns: the first LedgeSouth with open ground north of it, round Route 2
        (int X, int Y)? ledge = null;
        for (int r = 0; r < 200 && ledge == null; r++)
            for (int y = 624 - r; y <= 624 + r && ledge == null; y++)
                for (int x = 255 - r; x <= 255 + r && ledge == null; x++)
                    if (Math.Max(Math.Abs(x - 255), Math.Abs(y - 624)) == r && kalos.BehaviourAt(x, y) is TileBehavior.LedgeSouth or TileBehavior.LongLedgeSouth && kalos.IsWalkable(x, y - 1) && kalos.IsWalkable(x, y + 1))
                        ledge = (x, y);
        if (ledge is var (lx, ly))
        {
            At("Kalos", lx, ly - 1, Direction.Down); game.LocationSign.Hide(); Frames(4);
            engine.Steering = (Direction.Down, false);
            for (int i = 0; i < 60 && game.Player.HopProgress <= 0.3f; i++) Frames(1);
            Console.WriteLine($"  fw31: the ledge at {lx},{ly}, hop {game.Player.HopProgress:0.00}");
            Shot("fw31_ledge_mid_hop");
            engine.Steering = null;
            Frames(30);
        }
        else Console.WriteLine("  !! no ledge near Route 2");

        // Aquacorde's bridge: on its deck, and walking off it
        At("Kalos", 255, 656, Direction.Up); game.LocationSign.Hide(); Frames(4);
        engine.Steering = (Direction.Up, false); Frames(20); engine.Steering = null; Frames(10);
        Console.WriteLine($"  fw34: on the bridge at {game.Player.GridX},{game.Player.GridY}, offset {game.Player.Offset}");
        Shot("fw34_bridge_deck");

        // A door walked into from its front: Santalune's Pokémon Center
        At("Kalos", 325, 526, Direction.Up); game.LocationSign.Hide(); Frames(4);
        engine.Steering = (Direction.Up, false);
        bool shot = false;
        for (int i = 0; i < 400 && game.Map.Name == "Kalos"; i++)
        {
            Frames(1);
            // A tile before the door's mat, still walking at it
            if (!shot && game.Map.GetWarpAt(game.Player.GridX, game.Player.GridY - 2) != null) { Shot("fw35_door_from_its_front"); shot = true; }
        }
        engine.Steering = null;
        Frames(20);
        Console.WriteLine($"  fw35: came in to {game.Map.Name} at {game.Player.GridX},{game.Player.GridY}");
        Shot("fw35b_door_came_in");
    }
}
