using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    /// <summary>
    /// TR3, <c>peak</c> (tr20 to tr25; <c>TRAINING_PART=peak</c>): a Champion's game. The training club's clerk beside the
    /// Battle Frontier's south-east booth and her list for Battle Points (tr25), a Bottle Cap taken for 48 of 100 points; then
    /// in Sunyshore City's training hall the head coach's peak training on a level-50 Bidoof whose IVs are all ten, the
    /// question of its stats with their judge's words (tr20), Speed brought to its peak with that cap (tr21), SKILLS with the
    /// PEAK tag on Speed's row (tr22), the PC's judge with the same (tr23), and the clerk's stall (tr24). It prints the points,
    /// the bag's caps and the Speed before and after.
    /// </summary>
    void TrainingPeak()
    {
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        // Talks to whoever the player faces and reads on: each question answered by the answer the hand gives (by its
        // number in the question), the team's first chosen, the shot taken when a question or a line asks for one
        void Talk(Func<string, int> answer, Func<string, string?> shotAtQuestion, Func<string, string?> shotAtLine)
        {
            game.Interact();
            Frames(3);
            for (int i = 0; i < 120 && (Box().IsActive || engine.Choice.IsOpen || game.State is GameState.Dialogue or GameState.PartyMenu || engine.ScriptRunning); i++)
            {
                if (game.State == GameState.PartyMenu)
                {
                    game.PartyScreen.SelectedIndex = 0;
                    game.PartyScreen.Confirm(game.Party); Frames(3);
                    continue;
                }
                if (engine.Choice.IsOpen)
                {
                    string asked = Box().VisibleText;
                    int pick = answer(asked);
                    while (engine.Choice.Cursor < pick) engine.Choice.Move(1);
                    Frames(20);
                    if (shotAtQuestion(asked) is { } shot) Shot(shot);
                    engine.Choice.Confirm(); Frames(4);
                    continue;
                }
                if (!Box().IsActive) { Frames(1); continue; }
                Box().FinishLine(); Frames(2);
                if (shotAtLine(Box().VisibleText) is { } lineShot) { Frames(10); Shot(lineShot); }
                if (Box().VisibleText.Contains("traded") || Box().VisibleText.Contains("peak")) Console.WriteLine($"  said: {Box().VisibleText}");
                Box().Advance(); Frames(2);
            }
            Until(() => game.State == GameState.Overworld && !Box().IsActive && !engine.ScriptRunning, "the field again");
        }

        var cap = ItemDatabase.Get(HyperTraining.BottleCap)!;
        party.Clear();
        var bidoof = new Pokemon(PokemonDatabase.Get("Bidoof")!, 50)
        {
            IvHP = 10, IvAttack = 10, IvDefense = 10, IvSpAttack = 10, IvSpDefense = 10, IvSpeed = 10
        };
        bidoof.ReckonStats();
        party.Add(bidoof);
        game.Pokedex.RegisterSeen(bidoof.Species.DexNumber);
        game.Pokedex.RegisterCaught(bidoof.Species.DexNumber);
        game.Story.Set(RegionDatabase.Get(RegionDatabase.Sinnoh)!.StoryCompleteFlag);
        game.Bag.RemoveItem(cap, game.Bag.GetQuantity(cap));
        game.Frontier.GivePoints(100 - game.Frontier.BattlePoints);
        Console.WriteLine($"peak training: {game.Frontier.BattlePoints} BP, {game.Bag.GetQuantity(cap)} Bottle Caps, Bidoof's Speed {bidoof.Speed}");

        // ---- The training club's clerk beside the Exchange Service Corner's south-east booth: a Bottle Cap for 48 BP
        game.State = GameState.Overworld;
        At("BattleFrontier", 49, 45, Direction.Right);
        game.LocationSign.Hide();
        Frames(20);
        Talk(_ => 0, q => q.Contains("prize") ? "tr25_frontier_training_clerk" : null, _ => null);
        Console.WriteLine($"  after the clerk: {game.Frontier.BattlePoints} BP, {game.Bag.GetQuantity(cap)} Bottle Caps");

        // ---- Sunyshore City's hall: across the counter from the head coach, PEAK TRAINING, the team's first, SPEED
        At(GameEngine.SunyshoreTrainingHall, 11, 6, Direction.Up);
        game.LocationSign.Hide();
        Frames(30);
        Talk(q => q.Contains("peak") ? 5 : q.Contains("today") ? 1 : 0,
            q => q.Contains("peak") ? "tr20_peak_stats" : null,
            line => line.Contains("is at its peak now") ? "tr21_peak_done" : null);
        Console.WriteLine($"  after the coach: Speed at its peak {bidoof.IsHyperTrained(StatType.Speed)} (bits {bidoof.HyperTrained}), Speed {bidoof.Speed}, IV {bidoof.IvSpeed}; {game.Bag.GetQuantity(cap)} Bottle Caps");

        // ---- SKILLS with PEAK on Speed's row
        ShowSkills(bidoof, "tr22_skills_peak");

        // ---- The PC's judge on the same Bidoof, in a box of its own
        {
            var toolsBefore = engine.Settings.BoxTools;
            engine.Settings.BoxTools = RulesDefault.On;
            var boxesBefore = game.Boxes;
            var stored = new PcBoxes();
            var copy = bidoof.Clone();
            stored.Store(copy);
            game.Boxes = stored;
            game.State = GameState.PCStorage;
            var ps = game.PcScreen;
            ps.Tools = true;
            ps.Open(stored);
            for (int i = 0; i < 12 && ps.Under(party, stored) != copy; i++)
                ps.Move(i < 6 ? 0 : 1, i < 6 ? 1 : 0, party.Count);
            if (ps.Under(party, stored) != copy) Console.WriteLine("  !! the judge's Bidoof is not under the cursor");
            ps.Confirm(party, stored, engine.ShowNotification);
            ps.MenuIndex = ps.Menu!.ToList().IndexOf(PcAction.Judge);
            ps.Confirm(party, stored, engine.ShowNotification);
            Frames(12); Shot("tr23_judge_peak");
            ps.Close();
            game.Boxes = boxesBefore;
            engine.Settings.BoxTools = toolsBefore;
            game.State = GameState.Overworld;
        }

        // ---- The clerk's stall, for a Champion
        At(GameEngine.SunyshoreTrainingHall, 13, 6, Direction.Up);
        game.LocationSign.Hide();
        Frames(10);
        game.Interact();
        Frames(3);
        for (int i = 0; i < 40 && game.State != GameState.Shop; i++)
        {
            if (Box().IsActive) { Box().FinishLine(); Frames(2); Box().Advance(); }
            Frames(3);
        }
        if (game.State == GameState.Shop)
        {
            game.Shop.Confirm(game.Bag, 999999, engine.ShowNotification); Frames(30);
            Shot("tr24_stall");
            Console.WriteLine($"  the stall: {string.Join(", ", MartDatabase.Stock("training_hall", 8).Take(6).Select(i => $"{i.Name} {i.Price}"))}...");
            game.Shop.Close();
            Frames(10);
        }
        else Console.WriteLine("  !! the stall never opened");
        Until(() => !engine.ScriptRunning || Box().IsActive, "the clerk's end", 120);
        for (int i = 0; i < 20 && (Box().IsActive || engine.ScriptRunning); i++) { if (Box().IsActive) { Box().FinishLine(); Box().Advance(); } Frames(3); }
    }
}
