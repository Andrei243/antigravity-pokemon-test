using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>A flute played from the bag: the Black Flute halves the encounter rate, the White one adds half again.</summary>
public enum Flute { None, Black, White }

/// <summary>
/// What keeps wild Pokémon away or brings them on (plan 06 · R11), the original's <c>SpecialEncounter</c>: the
/// steps a Repel has left, and the flute played in this place. A Repel turns away every wild Pokémon of a lower
/// level than the team's first that can fight (<c>RepelPreventsEncounter</c>); a new one can't be used while one
/// lasts. A flute lasts until another place is come to (<c>field_map_change_flags.c</c>). No drawing or input.
/// </summary>
public sealed class EncounterAids
{
    /// <summary>The steps the Repel has left; 0 when none is working. Saved.</summary>
    public int RepelSteps { get; set; }

    public Flute Flute { get; set; }

    public bool RepelActive => RepelSteps > 0;

    /// <summary>Whether the item is one of these: the three Repels and the two flutes (their field use is <c>BagMessage</c>).</summary>
    public static bool IsAid(ItemData item) => item.Name is "Repel" or "Super Repel" or "Max Repel" or "Black Flute" or "White Flute";

    /// <summary>
    /// <c>UseItemInBag</c>: a flute plays (and stays in the bag); a Repel starts its steps, from the item's own
    /// number, unless one is still working. Returns what to tell the player and whether the item was used up.
    /// </summary>
    public (string Message, bool UsedUp) Use(ItemData item, string player)
    {
        switch (item.Name)
        {
            case "Black Flute":
                Flute = Flute.Black;
                return ($"{player} played the {item.Name}. Wild Pokémon will be less likely to appear.", false);
            case "White Flute":
                Flute = Flute.White;
                return ($"{player} played the {item.Name}. Wild Pokémon will be more likely to appear.", false);
        }
        if (RepelActive) return ("The effects of the last Repel are still lingering.", false);
        RepelSteps = item.EffectParam;
        return ($"{player} used the {item.Name}. Weak wild Pokémon will stay away.", true);
    }

    /// <summary><c>Repel_UpdateSteps</c>: a step taken. True on the step that ends the Repel, when the game says it wore off.</summary>
    public bool Step()
    {
        if (RepelSteps <= 0) return false;
        RepelSteps--;
        return RepelSteps == 0;
    }

    /// <summary>Another place is come to: the flute's tune is over.</summary>
    public void ChangePlace() => Flute = Flute.None;
}
