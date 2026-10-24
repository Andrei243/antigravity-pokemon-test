partial class Harness
{
    /// <summary>
    /// Plan 20 · J13, part b, after the way to the League in the <c>johto</c> mode (so nothing before it moves;
    /// <c>JOHTO_FROM=partb</c> runs it alone in a new game of its own): Mt. Mortar's karate master asked for a battle, won
    /// and giving his Tyrogue (<c>jpb01</c> to <c>jpb04</c>), and Blackthorn's house of moves: the Move Deleter's question and
    /// the old man teaching Draco Meteor to a Gible that trusts its Trainer fully (<c>jpb10</c> to <c>jpb15</c>). It prints
    /// what each left behind (the flags, the team, the moves). Written, not run.
    /// </summary>
    void JohtoPartBShots()
    {
        game.State = GameState.Overworld;
        game.Player.SetMode(TravelMode.OnFoot);
        var box = game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void ReadTo(string? text, int most = 2400)
        {
            int idle = 0;
            for (int i = 0; i < most; i++)
            {
                if (text != null && box.IsActive && Translated.Saying(box.CurrentLine, text)) { Until(() => box.IsActive, "text"); box.FinishLine(); Frames(2); Console.WriteLine($"  said: {text}"); return; }
                if (text != null && !engine.ScriptRunning && !box.IsActive && game.State == GameState.Overworld) { if (++idle > 60) break; }
                else idle = 0;
                if (text == null && !engine.ScriptRunning && !box.IsActive && game.State == GameState.Overworld) return;
                if (game.State == GameState.Battle) { WinTheBattle(); continue; }
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (box.IsActive && box.IsCurrentLineComplete && !box.IsQuestion) { box.Advance(); Frames(2); }
                else Frames(1);
            }
            Console.WriteLine($"  !! never said: {text ?? "the script's end"}");
        }
        void WinTheBattle()
        {
            var fight = game.Battle;
            for (int guard = 0; guard < 400 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    foreach (var foe in fight.EnemyParty?.Members ?? new List<Pokemon>()) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    var lead = fight.PlayerPokemon;
                    if (lead != null) lead.CurrentHP = lead.MaxHP;
                    fight.SelectMainMenuOption(0);
                    fight.SelectMove(Math.Max(0, lead == null ? 0 : lead.Moves.FindIndex(m => m.Data.Category != MoveCategory.Status)));
                }
                else Confirm(fight);
                Skip(0.5);
            }
            Until(() => FieldAgain(), "the field again", 1500);
            Frames(10);
        }
        void TalkTo(string map, string key)
        {
            var npc = MapDatabase.Get(map).NPCs.First(n => n.Key == key);
            At(map, npc.GridX, npc.GridY + 1, Direction.Up);
            game.Interact();
        }

        game.Party.Members.Clear();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Machop")!, 30));
        // ---- Mt. Mortar: the karate master, the battle and the Tyrogue
        At("MtMortar_B1F", 41, 19, Direction.Up); Frames(10); Shot("jpb01_the_karate_master");
        TalkTo("MtMortar_B1F", "d38r0104_gsfighter");
        ReadTo("Will you fight me?"); Frames(10); Shot("jpb02_the_challenge");
        ReadTo("Well fought"); Frames(10); Shot("jpb03_beaten");
        ReadTo("received Tyrogue"); Frames(10); Shot("jpb04_the_tyrogue");
        ReadTo(null);
        Console.WriteLine($"  Mt. Mortar: beaten {game.Story.Has("FLAG_BEAT_MORTAR_KARATE_MASTER")}, gift {game.Story.Has("FLAG_GOT_TYROGUE_FROM_KIYO")}, "
            + $"team {string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} {p.Level}"))}");

        // ---- Blackthorn's house of moves: the Move Deleter, then Draco Meteor for a Gible that trusts its Trainer
        var gible = new Pokemon(PokemonDatabase.Get("Gible")!, 30) { Friendship = 255 };
        game.Party.Add(gible);
        At("Blackthorn_MoveTutorHouse", 4, 8, Direction.Up); Frames(10); Shot("jpb10_the_house_of_moves");
        TalkTo("Blackthorn_MoveTutorHouse", "t30r0601_gsmiddleman1");
        ReadTo("make a Pokémon forget"); Frames(10); Shot("jpb11_the_move_deleter");
        ReadTo("Shall I make one of your Pokémon forget"); Frames(10); engine.Choice.Back(); Frames(4);
        ReadTo(null);
        TalkTo("Blackthorn_MoveTutorHouse", "t30r0601_gsoldman1");
        ReadTo("Would you have me teach it"); Frames(10); Shot("jpb12_the_dragon_tamer");
        ReadTo("Choose the Pokémon"); Frames(10); Shot("jpb13_choose_the_pokemon");
        // (the line is read on to the team, where the Gible is chosen: waiting for a state other than the field's found
        // the line itself, and the team was never opened)
        box.Advance(); Frames(2);
        Until(() => game.State == GameState.PartyMenu, "the party screen", 300);
        Frames(20); Shot("jpb14_the_team_screen");
        game.PartyScreen.SelectedIndex = game.Party.Members.IndexOf(gible);
        game.PartyScreen.Confirm(game.Party); Frames(3);
        ReadTo("learned Draco Meteor"); Frames(10); Shot("jpb15_draco_meteor_learned");
        ReadTo(null);
        Console.WriteLine($"  Gible: {string.Join(", ", gible.Moves.Select(m => m.Name))}; knows Draco Meteor {gible.Knows("Draco Meteor")}");
    }
}
