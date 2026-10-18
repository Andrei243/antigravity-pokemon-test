using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Data;

// The last 23 abilities of Generations 5 to 9 (plan 06 · R27b), by Pokémon Showdown's rules (data/abilities.ts and
// data/conditions.ts at the pinned commit, fetched into the importer's cache). As in R27, a bare FlagEffect is an
// ability the core reads by name where Showdown's event has it (BattleCore.Abilities2.cs; Neutralizing Gas in
// Battler.Ability, RKS System in Battler's types, Mega Sol in the damage formula's weather). docs/mechanics/rulings.md,
// "The later abilities", lists where each is cut down.
public static partial class AbilityEffectTable
{
    private static void AddLater2()
    {
        // Read by name in the core (BattleCore.Abilities2.cs), Battler and the damage formula
        foreach (var name in new[]
                 {
                     "Aroma Veil", "Flower Veil", "Ripen", "Symbiosis", "Receiver", "Power of Alchemy", "Wimp Out", "Emergency Exit",
                     "Dancer", "RKS System", "Ball Fetch", "Opportunist", "Mega Sol", "Cheek Pouch"
                 })
            Add(name, new FlagEffect());

        // Berries kept from the foes
        Add("Unnerve", new Unnerve());
        Add("As One", new AsOne());

        // As it comes in
        Add("Illusion", new Illusion());
        Add("Imposter", new Imposter());
        Add("Neutralizing Gas", new NeutralizingGas());
        Add("Commander", new FlagEffect());
        Add("Supreme Overlord", new SupremeOverlord());

        // A berry's own course
        Add("Cud Chew", new CudChew());

        // Forms in battle (R29's forms are the data's: Greninja's bond, Zygarde's cells)
        Add("Battle Bond", new BattleBond());
        Add("Power Construct", new PowerConstruct());
    }
}
