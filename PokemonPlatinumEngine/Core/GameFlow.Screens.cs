using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The screens' logic (plan 16 · T11c): the menus, the text box and the field's notices, each a screen whose logic
/// takes no window, held by the flow and drawn by the engine. The screens that render figures of their own offscreen
/// (the title, the introduction, the evolution scene, the wardrobe, plan 16 · T11f) make their pictures only when the
/// engine asks them to render, so they are made and steered here with no window too. A frame of actions reaches them
/// all through <see cref="Act"/>.
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

    /// <summary>The title, made afresh with the slots as the game starts and after a save is deleted.</summary>
    public TitleScreen TitleScreen { get; set; } = new(null);
    public IntroScreen IntroScreen { get; } = new();
    public EvolutionScreen EvolutionScreen { get; } = new();
    public WardrobeScreen WardrobeScreen { get; } = new();

    /// <summary>The screens of the game's other corners (plan 16 · T11g): each made its pictures, if any, only when drawn.</summary>
    public HallOfFameScreen HallOfFameScreen { get; } = new();
    public JournalScreen JournalScreen { get; } = new();
    public HatchScreen HatchScreen { get; } = new();
    public HallOfFameCeremony Ceremony { get; } = new();
    public CreditsScreen Credits { get; } = new();
    public SealCaseScreen SealCaseScreen { get; } = new();
    public SealShopScreen SealShopScreen { get; } = new();
    public FashionCaseScreen FashionCaseScreen { get; } = new();
    public PoffinCookingScreen PoffinCookingScreen { get; } = new();
    public PoffinCaseScreen PoffinCaseScreen { get; } = new();
    public PokegearScreen PokegearScreen { get; } = new();
    public SuperContestScreen SuperContestScreen { get; } = new();
    public SlotMachineScreen SlotMachineScreen { get; } = new();
    public MiningScreen MiningScreen { get; } = new();
    public UndergroundScreen UndergroundScreen { get; } = new();
    public DecorationScreen DecorationScreen { get; } = new();
    public FrontierRecordsScreen FrontierRecordsScreen { get; } = new();
    public FactoryScreen FactoryScreen { get; } = new();
    public HallBoardScreen HallBoardScreen { get; } = new();
    public CastleScreen CastleScreen { get; } = new();
    public ArcadeBoardScreen ArcadeBoardScreen { get; } = new();
    public PoketchView PoketchView { get; } = new();

    /// <summary>A question's answers, shown beside the text box once the question is written out.</summary>
    public ChoiceBox Choice { get; } = new();

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
