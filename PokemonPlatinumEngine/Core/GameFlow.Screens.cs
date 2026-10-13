using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The screens' logic (plan 16 · T11c): the menus, the text box and the field's notices, each a screen whose logic
/// takes no window, held by the flow and drawn by the engine. The screens that render figures of their own offscreen
/// (the title, the introduction, the evolution scene, the wardrobe) stay the engine's until their logic is parted
/// from their pictures. A frame of actions reaches them all through <see cref="Act"/>.
/// </summary>
public sealed partial class GameFlow
{
    public StartMenu StartMenu { get; } = new();
    public PartyScreen PartyScreen { get; } = new();
    public BagScreen BagScreen { get; } = new();
    public PokedexScreen PokedexScreen { get; } = new();
    public TrainerCardScreen TrainerCardScreen { get; } = new();
    public StarterSelectScreen StarterSelectScreen { get; } = new();
    public ShopScreen ShopScreen { get; } = new();
    public PCScreen PCScreen { get; } = new();
    public OptionsScreen OptionsScreen { get; } = new();
    public SaveScreen SaveScreen { get; } = new();
    public TownMapScreen TownMapScreen { get; } = new();

    /// <summary>The text box; a tool may hand the game another.</summary>
    public DialogueManager Dialogue { get; set; } = new();

    /// <summary>The place's name as the player comes into it.</summary>
    public LocationSign LocationSign { get; } = new();

    /// <summary>A line over the field that goes by itself ("Game saved.").</summary>
    public Toast Toast { get; } = new();

    /// <summary>
    /// Hands this thread's reads the frame's actions in place of the keys (<see cref="InputManager.Feed"/>), for a
    /// game steered with no window: the screens, the field and the battle read them as they read the keys. Null gives
    /// the keys back.
    /// </summary>
    public void Act(ActionFrame? actions) => InputManager.Feed(actions);
}
