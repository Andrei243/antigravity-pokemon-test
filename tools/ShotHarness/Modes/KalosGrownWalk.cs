using System;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Stance = PokemonPlatinumEngine.Overworld.Stance;

partial class Harness
{
    /// <summary>
    /// The rest of plan 30's free hand-offs on Kalos's own ground (<c>fw30</c> to <c>fw39</c> beside <c>fw31</c>, <c>fw34</c>
    /// and <c>fw35</c>), steered with the stick: the body along a corridor's wall, the player's stairs walked into
    /// slantwise, someone spoken to diagonally, the follower on its breadcrumbs, a script's walk after centring and, last
    /// since it ends in a battle, a trainer's "!" as the body crosses the lane (Kalos draws no ice: <c>fw32</c> is a line);
    /// then the grown figures the gate left owed (<c>kg60</c> to <c>kg66</c>): someone lying on a bed, a fisher of the map,
    /// a surfer under a bridge's deck, and the Mamoswine ridden on Route 17.
    /// </summary>
    private void KalosGrownWalk()
    {
        var team = game.Party;
        if (team.Count == 0)
            foreach (string species in new[] { "Froakie", "Fletchling", "Pikachu" })
                team.Add(new Pokemon(PokemonDatabase.Get(species)!, 30));
        var kalos = MapDatabase.Get("Kalos");
        void Free() { engine.Steering = null; engine.Stick = null; }
        void Stick(float x, float y, int frames)
        {
            engine.Steering = null;
            engine.Stick = (Vector2.Normalize(new Vector2(x, y)), false);
            for (int i = 0; i < frames && game.State == GameState.Overworld && !engine.ScriptRunning; i++) Frames(1);
        }
        string Where() => $"{game.Map.Name} {game.Player.GridX},{game.Player.GridY} offset {game.Player.Offset}, facing {game.Player.Facing}, gliding {game.Player.IsGliding}";
        bool Open(int x, int y) => kalos.InBounds(x, y) && kalos.IsWalkable(x, y) && kalos.BehaviourAt(x, y) != TileBehavior.TallGrass
            && kalos.NPCs.At(x, y) == null && kalos.GetWarpAt(x, y) == null;
        bool Shut(int x, int y) => kalos.InBounds(x, y) && !kalos.IsWalkable(x, y);
        (int X, int Y)? Near(int cx, int cy, int reach, Func<int, int, bool> holds)
        {
            for (int r = 0; r < reach; r++)
                for (int y = cy - r; y <= cy + r; y++)
                    for (int x = cx - r; x <= cx + r; x++)
                        if (Math.Max(Math.Abs(x - cx), Math.Abs(y - cy)) == r && holds(x, y)) return (x, y);
            return null;
        }

        // ---- fw30: a corridor one tile wide running north, walked slantwise so the body slides along its east wall
        var corridor = Near(300, 560, 260, (x, y) => Enumerable.Range(0, 5).All(k => Open(x, y - k) && Shut(x - 1, y - k) && Shut(x + 1, y - k)));
        if (corridor is var (cx, cy))
        {
            At("Kalos", cx, cy, Direction.Up); game.LocationSign.Hide(); Frames(4);
            Stick(0.55f, -1f, 34);
            Console.WriteLine($"  fw30: the corridor at {cx},{cy}..{cy - 4}; {Where()}");
            game.LocationSign.Hide(); Shot("fw30_corridor_wall");
            Free(); Frames(10);
        }
        else Console.WriteLine("  !! fw30: no corridor one tile wide in reach");

        // ---- fw32: ice (Kalos's drawings have no ice tile; Snowbelle's Gym is a puzzle that keeps the grid's steps)
        Console.WriteLine("  fw32: skipped, Kalos draws no ice (MapDraw's legend has none, Frost Cavern is cave floor)");

        // ---- fw33: the stairs of the player's house, walked into slantwise from the room's middle
        var house = MapDatabase.Get("VanivilleTown_PlayersHouse_1F");
        var stairs = house.Warps.FirstOrDefault(w => w.TargetMap.Contains("2F"));
        if (stairs != null)
        {
            At("VanivilleTown_PlayersHouse_1F", stairs.SourceX - 2, stairs.SourceY + 3, Direction.Up); game.LocationSign.Hide(); Frames(4);
            Stick(0.6f, -1f, 18);
            Console.WriteLine($"  fw33: the stairs' warp at {stairs.SourceX},{stairs.SourceY}; {Where()}");
            game.LocationSign.Hide(); Shot("fw33_stairs_slantwise");
            for (int i = 0; i < 120 && game.Map.Name == "VanivilleTown_PlayersHouse_1F"; i++) Frames(1);
            Free(); Frames(30);
            Console.WriteLine($"  fw33b: came up to {Where()}");
            game.LocationSign.Hide(); Shot("fw33b_stairs_came_up");
        }
        else Console.WriteLine("  !! fw33: no stairs in the player's house");

        // ---- fw36: someone in Aquacorde spoken to from the tile south-west of them, the body pressed in slantwise
        var talker = kalos.NPCs.Where(n => !n.IsThing && !n.IsTrainer && !n.IsPokemon && n.Movement == null
                && Math.Abs(n.GridX - 255) < 24 && Math.Abs(n.GridY - 660) < 24
                && Open(n.GridX - 1, n.GridY + 1) && Open(n.GridX - 1, n.GridY) && Open(n.GridX, n.GridY + 1))
            .OrderBy(n => Math.Abs(n.GridX - 255) + Math.Abs(n.GridY - 660)).FirstOrDefault();
        if (talker != null)
        {
            At("Kalos", talker.GridX - 1, talker.GridY + 1, Direction.Up); game.LocationSign.Hide(); Frames(4);
            Stick(1f, -1f, 10);
            Free(); Frames(2);
            game.Interact();
            for (int i = 0; i < 120 && !game.Dialogue.IsActive; i++) Frames(1);
            game.Dialogue.FinishLine();
            Frames(20);
            Console.WriteLine($"  fw36: {talker.Name} at {talker.GridX},{talker.GridY}; {Where()}; talking {game.Dialogue.IsActive}, they face {talker.Facing}");
            game.LocationSign.Hide(); Shot("fw36_diagonal_talk");
            for (int i = 0; i < 400 && (game.Dialogue.IsActive || engine.ScriptRunning); i++)
            {
                if (game.Dialogue.IsActive && game.Dialogue.IsCurrentLineComplete && !game.Dialogue.IsQuestion) game.Dialogue.Advance();
                else if (engine.Choice.IsOpen) engine.Choice.Confirm();
                Frames(2);
            }
        }
        else Console.WriteLine("  !! fw36: nobody in Aquacorde to speak to slantwise");

        // ---- fw38: the lead out of its ball on its breadcrumbs behind a slantwise walk in Aquacorde's square
        engine.Settings.Follower = true;
        At("Kalos", 248, 662, Direction.Right); game.LocationSign.Hide(); Frames(20);
        Stick(1f, 0f, 20);
        Stick(1f, 0.7f, 26);
        Console.WriteLine($"  fw38: {Where()}; lead {(game.Lead is { } lead ? $"{lead.Pokemon?.DisplayName} at {lead.Who.GridX},{lead.Who.GridY} drawn {lead.Who.DrawX:0.00},{lead.Who.DrawY:0.00}" : "none")}");
        game.LocationSign.Hide(); Shot("fw38_follower_breadcrumbs");
        Free(); Frames(40);
        game.LocationSign.Hide(); Shot("fw38b_follower_settled");
        engine.Settings.Follower = false;
        Frames(10);

        // ---- fw39: a script's walk begun off the tile's centre: the body eased onto it first, then walked north
        At("Kalos", 252, 664, Direction.Up); game.LocationSign.Hide(); Frames(4);
        Stick(1f, -0.4f, 7);
        Free(); Frames(1);
        Console.WriteLine($"  fw39: off the centre before the walk: {Where()}");
        engine.StartScript(ScriptParser.Parse("aquacorde_town", "script Test\n  walk player up 3\n  end\n")[0]);
        Frames(5);
        Console.WriteLine($"  fw39: centring: {Where()}");
        game.LocationSign.Hide(); Shot("fw39_script_walk_centring");
        Frames(14);
        game.LocationSign.Hide(); Shot("fw39b_script_walk_walking");
        for (int i = 0; i < 200 && engine.ScriptRunning; i++) Frames(1);
        Console.WriteLine($"  fw39: walked to {Where()}");

        // ---- the gate's owed figures, grown (kg60 on); the kit laid as grown on the harness's own rooms
        KalosGrownFigures();

        // ---- fw37 last: a trainer of Kalos sees the body as its centre crosses their lane (and the battle begins)
        NPC? seer = null;
        (int X, int Y) from = default, lane = default;
        foreach (var npc in kalos.NPCs.Where(n => n.IsTrainer && (n.TrainerData?.SightRange ?? 0) >= 3 && !n.IsThing)
                     .OrderBy(n => Math.Abs(n.GridX - 300) + Math.Abs(n.GridY - 560)))
        {
            var (dx, dy) = FieldMovement.Delta(npc.Facing);
            var l = (X: npc.GridX + dx * 2, Y: npc.GridY + dy * 2);
            // From two tiles to one side of the lane, the way between open and out of anyone's grass
            foreach (int side in new[] { 1, -1 })
            {
                var f = (X: l.X + dy * 2 * side, Y: l.Y + dx * 2 * side);
                var mid = (X: l.X + dy * side, Y: l.Y + dx * side);
                if (Open(l.X, l.Y) && Open(npc.GridX + dx, npc.GridY + dy) && Open(mid.X, mid.Y) && Open(f.X, f.Y)) { seer = npc; from = f; lane = l; break; }
            }
            if (seer != null) break;
        }
        if (seer != null)
        {
            At("Kalos", from.X, from.Y, Direction.Up); game.LocationSign.Hide(); Frames(4);
            var toward = Vector2.Normalize(new Vector2(lane.X - from.X, lane.Y - from.Y));
            // Slantwise across the lane: toward it and a little along it, away from the trainer
            var (fx, fy) = FieldMovement.Delta(seer.Facing);
            engine.Steering = null;
            engine.Stick = (Vector2.Normalize(toward + 0.6f * new Vector2(fx, fy)), false);
            for (int i = 0; i < 120 && !seer.HasSpottedPlayer; i++) Frames(1);
            Frames(4);
            Console.WriteLine($"  fw37: {seer.Name} at {seer.GridX},{seer.GridY} facing {seer.Facing}, lane {lane.X},{lane.Y}; spotted {seer.HasSpottedPlayer}, \"!\" {seer.ExclamationTimer:0.00}; {Where()}");
            game.LocationSign.Hide(); Shot("fw37_trainer_sees");
            Free();
        }
        else Console.WriteLine("  !! fw37: no trainer with an open lane in reach");
    }

    /// <summary>
    /// The grown figures plan 30's gate left owed, each shot on the harness's own ground with the kit laid as grown:
    /// someone lying along a bed (<c>kg60</c>, <c>kg61</c>), fishers of the map holding the rod (<c>kg62</c> to <c>kg63b</c>),
    /// the player under the terrain lab's bridge cut by its deck, grown and chibi on foot and grown surfing, and beside it
    /// (<c>kg64*</c>), and the Mamoswine of Route 17 ridden astride four ways (<c>kg65</c> to <c>kg69</c>).
    /// </summary>
    private void KalosGrownFigures()
    {
        var saved = (game.Map, game.Player.GridX, game.Player.GridY);
        engine.RegionKitOverride = CharacterKit.Grown;

        // Someone lying on a bed and the room's sitters, grown
        var room = BuildPosesRoom();
        game.Map = room;
        game.Player.SetPosition(6, 6, Direction.Up);
        game.Player.SetMode(TravelMode.OnFoot);
        Frames(6); Skip(0.4);
        var lier = room.NPCs.FirstOrDefault(n => room.StanceOf(n) == Stance.Lie);
        Console.WriteLine($"  kg60: {(lier != null ? $"{lier.Name} lies at {lier.GridX},{lier.GridY} facing {lier.Facing}" : "!! nobody lies")}; live {game.World.LiveRigs.Live}, sprites {game.World.LiveRigs.Sprites}");
        Shot("kg60_lying_on_a_bed");
        ShotCrop("kg61_lying_close", 1180, 240, 400, 225, 2);
        // The same with nobody live: the lier and the sitters as the impostors they are past the live twelve
        Impostors("kg60i_lying_impostor", ("kg61i_lying_impostor_close", 1180, 240, 400, 225));

        // Fishers of the map on the pond's shore, holding the rod: the fisherman (whose look carries one already) and a
        // youngster, whose look has none, so the rod is the grown kit's fishing look's
        var pond = BuildPond();
        var fisher = pond.NPCs.First(n => n.NpcType == "Fisherman");
        fisher.Posed = Stance.Fish;
        var youngster = new NPC { Name = "Youngster_fishing", NpcType = "Youngster", GridX = 12, GridY = 5, Facing = Direction.Left, Posed = Stance.Fish };
        pond.NPCs.Add(youngster);
        foreach (var who in new[] { fisher, youngster })
        {
            CharacterModels.Get(CharacterStyle.Holding(PlayerIdentity.CharacterFor(who.NpcType), HeldProp.Rod), game.RenderContext.Shaders);
            CharacterModels.Get(CharacterStyle.InKit(CharacterStyle.Holding(PlayerIdentity.CharacterFor(who.NpcType), HeldProp.Rod), CharacterKit.Grown), game.RenderContext.Shaders);
        }
        game.Map = pond;
        game.Player.SetPosition(4, 4, Direction.Right);
        Frames(6); Skip(0.6);
        Console.WriteLine($"  kg62: the fisher at {fisher.GridX},{fisher.GridY} {pond.StanceOf(fisher)}, the youngster at {youngster.GridX},{youngster.GridY} {pond.StanceOf(youngster)}; live {game.World.LiveRigs.Live}, sprites {game.World.LiveRigs.Sprites}");
        Shot("kg62_fisher_of_the_map");
        ShotCrop("kg63_fisher_close", 900, 450, 400, 225, 2);
        ShotCrop("kg63b_youngster_fishing_close", 1300, 380, 400, 225, 2);
        // The fishers and the pond's swimmers as impostors (nobody live)
        Impostors("kg62i_fisher_impostor", ("kg63i_fisher_impostor_close", 900, 450, 400, 225), ("kg63bi_youngster_impostor_close", 1300, 380, 400, 225));

        // Under the terrain lab's bridge: the grown player on foot, tall enough to cross the deck, cut by its boards as the
        // chibi sprite is (the kit laid grown, then Sinnoh's own chibi), and a grown surfer under it and beside it
        var lab = BuildTerrainLab();
        game.Map = lab;
        foreach (var (name, y, mode, kit) in new (string, int, TravelMode, CharacterKit?)[]
                 {
                     ("kg64_on_foot_under_the_deck", 18, TravelMode.OnFoot, CharacterKit.Grown),
                     ("kg64c_on_foot_under_the_deck_chibi", 18, TravelMode.OnFoot, null),
                     ("kg64e_surfing_under_the_deck", 18, TravelMode.Surfing, CharacterKit.Grown),
                     ("kg64g_surfing_beside_the_deck", 20, TravelMode.Surfing, CharacterKit.Grown)
                 })
        {
            engine.RegionKitOverride = kit;
            game.Player.SetPosition(23, y, Direction.Up);
            game.Player.SetMode(mode);
            if (mode == TravelMode.OnFoot) game.Player.SetHeight(0f);
            Frames(6); Skip(0.4);
            var at = new Vector3(game.Player.PixelX / 32f + 0.5f, 0f, game.Player.PixelY / 32f + 0.5f);
            Console.WriteLine($"  {name}: {mode} at 23,{y}, kit {engine.KitHere}, the deck over them {(Relief.DeckOver(lab, at.X, at.Z, Relief.At(lab, at.X, at.Z)) is { } d ? $"{d.X0:0.00}..{d.X1:0.00} at {d.Top:0.00}" : "none")}; live {game.World.LiveRigs.Live}, sprites {game.World.LiveRigs.Sprites}");
            Shot(name);
            ShotCrop(name.Replace("kg64", "kg64x") + "_close", 760, 240, 400, 225, 2);
        }
        game.Player.SetMode(TravelMode.OnFoot);
        engine.RegionKitOverride = null;

        // The Mamoswine of Route 17, ridden astride (Kalos is grown on its own file)
        var kalos = MapDatabase.Get("Kalos");
        var mamoswine = kalos.NPCs.FirstOrDefault(n => n.Name == "Mamoswine" && n.GridX == 532 && n.GridY == 204)
            ?? kalos.NPCs.FirstOrDefault(n => n.Name == "Mamoswine");
        if (mamoswine != null)
        {
            var (dx, dy) = FieldMovement.Delta(mamoswine.Facing);
            At("Kalos", mamoswine.GridX + dx, mamoswine.GridY + dy, Opposite(mamoswine.Facing)); game.LocationSign.Hide(); Frames(6);
            game.Interact();
            for (int i = 0; i < 900 && (game.Dialogue.IsActive || engine.ScriptRunning || engine.Choice.IsOpen); i++)
            {
                if (engine.Choice.IsOpen) { engine.Choice.Confirm(); Frames(2); }
                else if (game.Dialogue.IsActive && game.Dialogue.IsCurrentLineComplete && !game.Dialogue.IsQuestion) { game.Dialogue.Advance(); Frames(2); }
                else Frames(1);
            }
            Frames(30);
            Console.WriteLine($"  kg65: riding {game.Player.Mode} on {game.Player.Steed ?? "nothing"}; {game.Map.Name} {game.Player.GridX},{game.Player.GridY}");
            engine.Steering = (Direction.Right, false);
            Frames(12);
            var steedCard = FieldSprites.Get(FieldSprites.GrownName(game.Player.Steed ?? "Mamoswine"), CharacterSprites.FacingIndex(game.Player.Yaw));
            var riderRig = CharacterModels.Get(CharacterStyle.InKit(PlayerIdentity.Character, CharacterKit.Grown), game.RenderContext.Shaders);
            Console.WriteLine($"  kg65: the ride's card {steedCard?.Size} texels, {steedCard?.Rows} rows filled, a grown seat of {(steedCard != null ? WorldRenderer.GrownRideSeat(steedCard) : 0)} rows; the rider's saddle {GrownAnimation.SaddleOf(riderRig.Skeleton) * riderRig.Scale:0.00} tiles");
            Shot("kg65_ride_on_land");
            ShotCrop("kg66_ride_on_land_close", 770, 300, 400, 225, 2);
            engine.Steering = null;
            Frames(30);
            ShotCrop("kg66b_ride_on_land_standing_close", 770, 300, 400, 225, 2);
            // Ridden the other three ways, the card half a tile nearer the camera seen from the front and the back
            foreach (var (way, name) in new[] { (Direction.Down, "kg67_ride_facing_down"), (Direction.Left, "kg68_ride_facing_left"), (Direction.Up, "kg69_ride_facing_up") })
            {
                engine.Steering = (way, false);
                Frames(12);
                engine.Steering = null;
                Frames(20);
                Console.WriteLine($"  {name}: facing {game.Player.Facing} at {game.Player.GridX},{game.Player.GridY}");
                ShotCrop(name, 760, 260, 400, 250, 2);
            }
        }
        else Console.WriteLine("  !! kg65: no Mamoswine on Route 17");
        At(saved.Map.Name, saved.GridX, saved.GridY, Direction.Up);
        game.Player.SetMode(TravelMode.OnFoot);
        game.LocationSign.Hide(); Frames(10);
    }

    /// <summary>A shot of the place as it stands with nobody drawn live (<see cref="FieldRigChoice.LiveCap"/> 0), so every grown figure is its impostor, with crops; then live again.</summary>
    private void Impostors(string shot, params (string Name, int X, int Y, int W, int H)[] crops)
    {
        FieldRigChoice.LiveCap = 0;
        Frames(6);
        Console.WriteLine($"  {shot}: live {game.World.LiveRigs.Live}, impostors {game.World.LiveRigs.Impostors}, sprites {game.World.LiveRigs.Sprites}, looks baked {game.World.LiveRigs.LooksBaked}");
        Shot(shot);
        foreach (var c in crops) ShotCrop(c.Name, c.X, c.Y, c.W, c.H, 2);
        FieldRigChoice.LiveCap = null;
        Frames(2);
    }

    private static Direction Opposite(Direction d) => d switch
    {
        Direction.Up => Direction.Down,
        Direction.Down => Direction.Up,
        Direction.Left => Direction.Right,
        _ => Direction.Left
    };
}
