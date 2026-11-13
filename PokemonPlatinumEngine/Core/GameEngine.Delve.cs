using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Delve;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The delve in the game (plan 44 · SD8, SD9; style guide, "The delve"): a script's <c>delve go</c> sends the team's first
/// two who may go down a hollow as copies (<see cref="DelveRun"/>), the floor laid as the map <c>DelveFloor</c>
/// (<see cref="MapNamed"/>) with the leader drawn in the player's place (<see cref="Player.Avatar"/>) and the rest of the
/// team, the residents and the finds as Pokémon and balls of the map. A turn is the rules' at once; its lines are played
/// by <see cref="DelveScreen"/> before the next is read. The leader never walks the field's own steps, so no wild Pokémon,
/// warp or trigger is met. The run's end is settled at the camp (<see cref="DelveHome"/>): the bag kept, the recruits
/// home, the board's requests done. A game with no window plays the run at once by the plain policy.
/// </summary>
public partial class GameEngine
{
    /// <summary>The floor's map now drawn, and the people standing for the run on it, by the order the run gives each.</summary>
    private Map? delveMap;
    private readonly Dictionary<int, NPC> delvePeople = new();
    private readonly List<NPC> delveFinds = new();
    private int delveFloorShown = -1;

    /// <summary>A run begun (a script's <c>delve go</c>); false where the hollow isn't open or two can't go down.</summary>
    internal bool OpenDelve(string hollowKey) => OpenDelve(hollowKey, null);

    /// <summary>
    /// A run begun on a seed a tool gives (the harness's), or drawn from <see cref="Dice"/>. A game with no window plays
    /// it at once unless <paramref name="onTheFloor"/> asks for the floor laid as on screen (a test's key fuzzer).
    /// </summary>
    internal bool OpenDelve(string hollowKey, uint? seed, bool onTheFloor = false)
    {
        flow.ScriptAnswer = 0;
        if (DelveHollows.Get(hollowKey) is not { } hollow || !flow.DelveRecord.IsOpen(hollow) && hollow != DelveHollow.Test) return false;
        if (DelveHome.PairOf(flow.Party.Members) is not var (leader, partner)) return false;
        var randomiser = Randomizer.For(flow.Run);
        var run = DelveRun.Begin(hollow, leader, partner, seed ?? (uint)Dice.New().Next(), randomiser == null ? null : randomiser.Species);
        if (IsHeadless && !onTheFloor)
        {
            for (int n = 0; n < 20000 && run.Outcome == DelveOutcome.Going; n++) run.Act(DelvePolicy.Next(run));
            SettleTheDelve(run);
            return true;
        }
        flow.Delve = run;
        flow.DelveCamp = (flow.Map, flow.Player.GridX, flow.Player.GridY, flow.Player.Facing);
        flow.DelveScreen.Open(run);
        StartTransition(GameState.Delve, LayTheFloor);
        return true;
    }

    /// <summary>The run's floor laid as the map <c>DelveFloor</c>, the last floor's scene forgotten, the team put on it.</summary>
    private void LayTheFloor()
    {
        if (flow.Delve is not { } run) return;
        var file = run.Floor.ToFile();
        file.DisplayName = run.Hollow.Name ?? run.Hollow.Key;
        var map = file.ToMap();
        if (delveMap != null) world.Forget(delveMap, 0, 0, delveMap.Width, delveMap.Height);
        map.IsDark = run.Hollow.Dark;
        map.DarkReach = DarkReachInAHollow;
        delveMap = map;
        delvePeople.Clear();
        delveFinds.Clear();
        flow.Open = null;
        flow.Map = map;
        world.Life.Clear();
        delveFloorShown = run.Floor.Index;
        KeepTheFloor();
        PlayAreaMusic(map, run.Leader.X, run.Leader.Y);
        AnnounceLocation();
    }

    /// <summary>How far round the leader a dark hollow is lit (in tiles).</summary>
    public const float DarkReachInAHollow = 3.5f;

    /// <summary>The map made to stand as the run: everyone where the rules have them, the leader as the player.</summary>
    private void KeepTheFloor()
    {
        if (flow.Delve is not { } run || delveMap is not { } map) return;
        var standing = run.Team.Skip(1).Concat(run.Residents).Where(m => !m.Fainted).ToList();
        foreach (var gone in delvePeople.Keys.Where(k => standing.All(m => m.Order != k)).ToList())
        {
            map.NPCs.Remove(delvePeople[gone]);
            delvePeople.Remove(gone);
        }
        foreach (var m in standing)
        {
            if (!delvePeople.TryGetValue(m.Order, out var npc))
            {
                npc = new NPC { Name = "delve_" + m.Order, NpcType = NPC.PokemonType, Species = m.Pokemon.ModelName };
                delvePeople[m.Order] = npc;
                map.NPCs.Add(npc);
            }
            (npc.GridX, npc.GridY, npc.Facing) = (m.X, m.Y, m.Facing);
        }
        foreach (var ball in delveFinds) map.NPCs.Remove(ball);
        delveFinds.Clear();
        for (int i = 0; i < run.Finds.Count; i++)
        {
            var (x, y, item) = run.Finds[i];
            var ball = new NPC { Name = "delve_find_" + i, NpcType = NPC.ItemBallType, Item = item };
            (ball.GridX, ball.GridY) = (x, y);
            delveFinds.Add(ball);
            map.NPCs.Add(ball);
        }
        flow.Player.SetPosition(run.Leader.X, run.Leader.Y, run.Leader.Facing);
        flow.Player.Avatar = run.Leader.Pokemon.ModelName;
    }

    /// <summary>A frame of the delve: the buttons read when no line is playing, a turn handed over, the floor kept, the end settled.</summary>
    private void UpdateDelve(float dt)
    {
        var screen = flow.DelveScreen;
        if (flow.Delve is not { } run)
        {
            flow.State = GameState.Overworld;
            return;
        }
        screen.Advance(dt);
        world.Life.Advance(dt);
        if (!screen.Playing)
        {
            if (screen.Ended)
            {
                LeaveTheDelve();
                return;
            }
            ReadTheDelveButtons(screen);
        }
        else if (InputManager.IsActionPressed(GameAction.Confirm)) screen.Hurry();
        if (screen.TakeAction() is { } action && ActInTheDelve(action) == null && delveFloorShown != run.Floor.Index) return;
        KeepTheFloor();
    }

    /// <summary>An action handed to the rules through the screen; a new floor is laid behind a fade.</summary>
    private DelveRefusal? ActInTheDelve(DelveAction action)
    {
        if (flow.Delve is not { } run) return DelveRefusal.NotNow;
        var refused = flow.DelveScreen.Act(action);
        if (run.Floor.Index != delveFloorShown && run.Outcome == DelveOutcome.Going) StartTransition(GameState.Delve, LayTheFloor);
        else KeepTheFloor();
        return refused;
    }

    private static void ReadTheDelveButtons(DelveScreen screen)
    {
        bool turning = InputManager.IsActionDown(GameAction.Item);
        foreach (var (action, way) in new[] { (GameAction.Up, Direction.Up), (GameAction.Down, Direction.Down), (GameAction.Left, Direction.Left), (GameAction.Right, Direction.Right) })
        {
            // On the floor a way held steps on, a turn at a time; in a list it moves the cursor once a press
            bool field = screen.Menu == DelveMenu.Field && !turning;
            if (field ? InputManager.IsActionDown(action) : InputManager.IsActionPressed(action))
            {
                screen.Steer(way, turning);
                return;
            }
        }
        if (InputManager.IsActionPressed(GameAction.Confirm)) screen.Confirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) screen.Cancel();
        else if (InputManager.IsActionPressed(GameAction.Menu)) screen.OpenMoves();
        else if (InputManager.IsActionPressed(GameAction.PageRight)) screen.OpenBag();
        else if (InputManager.IsActionPressed(GameAction.PageLeft)) screen.Wait();
    }

    /// <summary>The run over: back to the camp through a fade, where its end is settled and the script hears it.</summary>
    private void LeaveTheDelve()
    {
        var run = flow.Delve;
        StartTransition(GameState.Overworld, () =>
        {
            if (run != null) SettleTheDelve(run);
            BackToTheCamp();
        });
    }

    /// <summary>The player back at the camp as they stood, the leader's look let go.</summary>
    private void BackToTheCamp()
    {
        flow.Player.Avatar = null;
        flow.Delve = null;
        flow.DelveScreen.Close();
        if (delveMap != null) world.Forget(delveMap, 0, 0, delveMap.Width, delveMap.Height);
        delveMap = null;
        delvePeople.Clear();
        delveFinds.Clear();
        delveFloorShown = -1;
        if (flow.DelveCamp is { } camp)
        {
            flow.Map = camp.Map;
            flow.Player.SetPosition(camp.X, camp.Y, camp.Facing);
            world.Life.Clear();
            PlayAreaMusic(camp.Map, camp.X, camp.Y);
        }
        flow.DelveCamp = null;
    }

    /// <summary>A run's end settled: the bag, the recruits home (ids, the Pokédex, the Nuzlocke's gifts clause), the record, the script's answer.</summary>
    private DelveSettled SettleTheDelve(DelveRun run)
    {
        string place = Nuzlocke.PlaceKeyOf(flow.Map, flow.Player.GridX, flow.Player.GridY);
        var settled = DelveHome.Settle(run, flow.DelveRecord, DelveBoardToday(), ApricornBox.DayOf(GameClock.Today), flow.Bag,
            p => !flow.Run.NoCatching && flow.Run.MayCarry(p, flow.Story.PlayerStarter),
            p =>
            {
                flow.Pokedex.RegisterSeen(p);
                flow.Pokedex.RegisterCaught(p);
                // The Nuzlocke's gifts clause: a recruit home is the camp's gift (plan 13 · V4)
                Nuzlocke.Gift(flow.Run, flow.RunState, place);
                if (!flow.Party.Add(p) && flow.Boxes.Store(p) == null) return false;
                return true;
            }, run.Hollow.Name ?? run.Hollow.Key);
        GiveIds();
        flow.ScriptAnswer = DelveHome.AnswerOf(run.Outcome);
        foreach (var p in settled.Home) ShowNotification(Text.Of("delve.home", ("pokemon", p.DisplayName)));
        return settled;
    }

    /// <summary>Today's board, drawn from the day's number over the hollows the camp's rank has opened.</summary>
    internal IReadOnlyList<DelveRequest> DelveBoardToday() => DelveBoard.Of(flow.Encounters.DailyNumber, flow.DelveRecord.RankOn(ApricornBox.DayOf(GameClock.Today)));

    /// <summary>The board opened (a script's <c>delve board</c>); with no window there is nothing to read and the script goes on.</summary>
    internal void OpenDelveBoard()
    {
        if (IsHeadless) return;
        var board = DelveBoardToday();
        int day = ApricornBox.DayOf(GameClock.Today);
        flow.DelveBoardScreen.Open(board, board.Select((_, i) => flow.DelveRecord.IsDone(i, day)).ToList(), flow.DelveRecord.Rank);
        flow.State = GameState.DelveBoard;
    }

    private void UpdateDelveBoard()
    {
        var screen = flow.DelveBoardScreen;
        if (InputManager.IsActionPressed(GameAction.Up)) screen.Move(-1);
        else if (InputManager.IsActionPressed(GameAction.Down)) screen.Move(1);
        else if (InputManager.IsActionPressed(GameAction.Confirm) || InputManager.IsActionPressed(GameAction.Cancel))
        {
            screen.Close();
            flow.State = GameState.Overworld;
        }
    }

    private void DrawDelve()
    {
        DrawTheField();
        ModernUi.DrawDelve(VirtualWidth, VirtualHeight, flow.DelveScreen);
    }

    private void DrawDelveBoard()
    {
        DrawTheField();
        ModernUi.DrawDelveBoard(VirtualWidth, VirtualHeight, flow.DelveBoardScreen);
    }

    internal sealed partial class Driver
    {
        /// <summary>The run under way, or null (plan 44 · SD8).</summary>
        public DelveRun? Delve => game.flow.Delve;

        /// <summary>The delve's screen.</summary>
        public DelveScreen DelveScreen => game.flow.DelveScreen;

        /// <summary>The delve's record (plan 44 · SD9).</summary>
        public DelveRecord DelveRecord => game.flow.DelveRecord;

        /// <summary>A run begun on a hollow, on a seed a tool gives; the test hollow is open to a tool.</summary>
        public bool OpenDelve(string hollow, uint seed) => game.OpenDelve(hollow, seed, onTheFloor: true);

        /// <summary>The Pokémon drawn in the player's place now (the delve's leader), or null for the player.</summary>
        public string? Avatar => game.flow.Player.Avatar;

        /// <summary>The floor's map while a run is laid, or null.</summary>
        public Map? DelveMap => game.delveMap;

        /// <summary>The camp's board's screen.</summary>
        public DelveBoardScreen DelveBoardScreen => game.flow.DelveBoardScreen;

        /// <summary>The camp's board on its screen.</summary>
        public void OpenDelveBoard() => game.OpenDelveBoard();

        /// <summary>The floor's map kept to the run at once (after a tool's own action).</summary>
        public void KeepTheFloor() => game.KeepTheFloor();

        /// <summary>An action of the leader's handed over as the buttons hand it (a new floor laid behind a fade).</summary>
        public Models.Delve.DelveRefusal? DelveAct(DelveAction action) => game.ActInTheDelve(action);
    }
}
