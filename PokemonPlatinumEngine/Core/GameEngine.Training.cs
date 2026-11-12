namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The training hall in the game (plan 36 · TR2; <c>docs/mechanics/rulings.md</c>, "The training hall"): a coach's
/// <c>train drill</c> opens <see cref="UI.TrainingScreen"/> (<see cref="GameState.Training"/>) on the Pokémon the script
/// chose, with the drills' records (<c>GameFlow.Training</c>, saved as <c>SaveData.Training</c>), and the script hears
/// the grade won once it closes. The drills' rules are GPU-free in <c>Models/TrainingDrills.cs</c>, the records' in
/// <c>Models/TrainingRecords.cs</c>.
/// </summary>
public partial class GameEngine
{
    /// <summary>Sinnoh's hall: a room of ours behind Sunyshore City's alley between its north houses (plan 36 · TR2).</summary>
    public const string SunyshoreTrainingHall = "SunyshoreTrainingHall";

    /// <summary>Opens the drills on a member of the team (by its place); nothing happens for a place nobody holds.</summary>
    private void OpenTraining(int place)
    {
        if (place < 0 || place >= flow.Party.Count) return;
        flow.State = GameState.Training;
        flow.TrainingScreen.HoldsTeraOrb = HoldsTeraOrb;
        flow.TrainingScreen.Open(flow.Party.Members[place], flow.Training, fieldRandom);
    }

    /// <summary>The key item of Terastallization (plan 36 · TR5): what the Tera keeper, the drills' shards and the summary's Tera pill wait for.</summary>
    public const string TeraOrbItem = "Tera Orb";

    /// <summary>Whether the bag holds the Tera Orb.</summary>
    internal bool HoldsTeraOrb => Data.ItemDatabase.Get(TeraOrbItem) is { } orb && flow.Bag.GetQuantity(orb) > 0;

    private void UpdateTraining(float dt)
    {
        flow.TrainingScreen.Update(dt);
        if (flow.TrainingScreen.IsActive) return;
        flow.ScriptAnswer = flow.TrainingScreen.Outcome;
        flow.State = GameState.Overworld;
    }
}
