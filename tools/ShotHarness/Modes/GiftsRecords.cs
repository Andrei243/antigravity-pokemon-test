using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

partial class Harness
{
    // ---------------------------------------------------------------- records mixed (plan 40 · GE10)

    // Records mixed between one's own saves (gf90 to gf92), in a new game of its own past the opening, with another save
    // written by hand into slot 3 of the out dir (a girl called Mira, a Pikachu, a secret base at Route 118's tree with a
    // desk and a doll, an interview) and taken away at the end:
    //   gf90_record_desk: the record desk upstairs in Jubilife City's Pokémon Center, its line once the records are mixed;
    //   gf91_mixed_in_the_hall: the friends' hall come to again, Mira standing on its first spot;
    //   gf92_mixed_base: Route 118's tree gone into, Mira's base with her desk, her doll and herself by the PC.
    // It prints what was mixed, who stands in the hall and the base laid out.
    void GiftsRecords()
    {
        GiftsNewGame();
        var hoenn = MapDatabase.Get("Hoenn");
        var spot = hoenn.SecretBaseSpots.First(s => s.Value == 171).Key;
        var decorations = new List<StoredDecoration>();
        SecretBases.Add(decorations, DecorationDatabase.IdOf("Small Desk"));
        SecretBases.Add(decorations, DecorationDatabase.IdOf("Pikachu Doll"));
        var other = new SaveData
        {
            PlayerName = "Mira",
            Look = PlayerLook.Girl,
            TrainerId = 4242,
            Started = new DateTime(2026, 3, 14),
            Party =
            {
                new SavedPokemonData
                {
                    SpeciesName = "Pikachu", Level = 42, Nature = Nature.Jolly,
                    Moves = new List<SavedMoveData> { new() { MoveName = "Thunder Shock" }, new() { MoveName = "Quick Attack" } }
                }
            },
            Decorations = decorations,
            SecretBase = new SecretBaseSave
            {
                Id = 171, Map = "Hoenn", X = spot.X, Y = spot.Y, OutX = spot.X, OutY = spot.Y + 1,
                Placed = { new PlacedDecoration { Slot = 0, X = 2, Y = 8 }, new PlacedDecoration { Slot = 1, X = 5, Y = 8 } }
            }
        };
        other.StoryPhrases[StoryState.InterviewPhrase] = new List<string> { PhraseGroups.Key(PhraseGroup.Pokemon, "Pikachu"), PhraseGroups.Key(PhraseGroup.Types, "Electric") };
        SaveSlots.Write(SavePaths.Current, 3, other);
        try
        {
            // The desk, across her counter from the tile south of it
            At(FriendsHall.Room, 3, 6, Direction.Up);
            Frames(20);
            game.Interact();
            Frames(3);
            GiftsUntil(() => GiftsSaying("All done"), "the records mixed");
            game.Dialogue.FinishLine(); Frames(20);
            Shot("gf90_record_desk");
            GiftsUntil(() => !engine.ScriptRunning && !game.Dialogue.IsActive && game.State == GameState.Overworld, "the desk's goodbye");
            foreach (var r in game.Records.Records)
                Console.WriteLine($"  mixed: {r.Name} ({r.Key}), team {string.Join(", ", r.Team?.Select(b => $"{b.Species} {b.Level} [{string.Join("/", b.Moves)}]") ?? new[] { "none" })}, base {(r.Base == null ? "none" : $"{r.Base.Id} at {r.Base.X},{r.Base.Y} with {r.Base.Standing.Count}")}, interview {(r.Interview == null ? "none" : string.Join(" ", r.Interview))}");

            // The hall come to again, from the floor south of the spots
            At(FriendsHall.Room, 9, 11, Direction.Up);
            // (the room is the one the desk stands in, so it is come to again by hand: the hall is laid out as it is come to)
            game.ArriveOnMap();
            Frames(30);
            Shot("gf91_mixed_in_the_hall");
            Console.WriteLine($"  in the hall: {string.Join(", ", game.Map.NPCs.Where(FriendsHall.IsFriend).Select(n => $"{n.Name} at {n.GridX},{n.GridY}"))}");

            // Route 118's tree, faced from the tile under it: Mira's base
            At("Hoenn", spot.X, spot.Y + 1, Direction.Up);
            Frames(20);
            game.TrainersLookOnArrival = false;
            game.Interact();
            Frames(3);
            GiftsUntil(() => game.Map.Name == "SecretBase_Tree2" && game.State == GameState.Overworld && !engine.ScriptRunning, "Mira's base");
            Frames(40);
            Shot("gf92_mixed_base");
            Console.WriteLine($"  in the base: {string.Join(", ", game.Map.Props.Where(p => p.Type == PropType.Decoration).Select(p => $"{p.Model} at {p.X},{p.Y}"))}; " +
                $"{string.Join(", ", game.Map.NPCs.Where(FriendsHall.IsFriend).Select(n => $"{n.Name} at {n.GridX},{n.GridY}"))}; visiting {game.Records.Visiting?.Name ?? "nobody"}");
        }
        finally
        {
            SaveSlots.Delete(SavePaths.Current, 3);
        }
    }
}
