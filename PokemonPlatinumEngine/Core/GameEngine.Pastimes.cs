using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Fossils, the Game Corner, the Villa and Pal Park in the game (plan 06 · R16b): where the rules of
/// <see cref="SlotMachine"/>, <see cref="VillaRules"/>, <see cref="PalParkState"/> and <see cref="CatchingShow"/>
/// meet the maps and the screens. A sitting at a machine is <see cref="GameState.SlotMachine"/>; the Villa's
/// furniture is stood in its room as the player comes in; a step in Pal Park may bring one of the six out of its
/// corner, met in a <see cref="BattleKind.PalPark"/> battle.
/// </summary>
public partial class GameEngine
{
    public const string VillaMap = VillaRules.Room, PalParkMap = "PalPark", PalParkLobbyMap = "PalParkLobby";
    /// <summary>The scripts the game itself starts in Pal Park (<c>pal_park.txt</c>).</summary>
    public const string ParkCaughtAllScript = "CaughtAll";

    /// <summary>The scripts the game itself starts here: what looking at a piece of the Villa's furniture says, and the Catching Show's end.</summary>
    public static IEnumerable<(string File, string Script)> PastimeScripts =>
        VillaRules.Furniture.Where(f => f.Places.Any(p => p.Blocks)).Select(f => (VillaMap, VillaLookScript(f))).Append(("pal_park", ParkCaughtAllScript));

    /// <summary>The flags and variables the game itself sets here, which scripts only read: the Villa's furniture, the day's visitor's words, the slot machines' and the Catching Show's records.</summary>
    public static IEnumerable<string> PastimeFlags => VillaRules.Furniture.Select(f => f.Flag);
    public static readonly string[] PastimeVariables = { GameCornerPrizes.StreakVariable, VillaRules.MessageVariable, VillaRules.VisitorVariable, PalParkState.RecordVariable };

    private readonly Pastimes pastimes = new();
    private readonly SlotMachineScreen slotScreen = new();
    private readonly List<Prop> villaProps = new();

    /// <summary>What the Villa's catalogue reads of the game's records (the Battle Frontier's are plan 06 · R18's to fill in).</summary>
    private VillaRecords VillaRecordsNow => new(FrontierEverywhere: false, FrontierPrint: false, hallOfFame.Total,
        story.Var(VillaRules.BattlegroundVariable), pastimes.BerriesPlanted, pastimes.EggsHatched, pastimes.Steps);

    private int PastimeAct(string what, int number, NPC? subject)
    {
        switch (what)
        {
            case "slots":
                OpenSlotMachine(number);
                return 1;
            case "furnish":
                FurnishVilla();
                // The delivery that leaves the catalogue nothing more to sell (Villa_HasAllFurniture)
                if (VillaRules.Owned(story) == VillaRules.Furniture.Count) trainerScore = TrainerScore.Add(trainerScore, TrainerScore.BoughtAllVillaFurniture);
                return 1;
            case "boxroom":
                return PcBoxes.BoxCount * PcBoxes.BoxSize - pcBoxStorage.Count;
            case "store":
                if (!pastimes.PalPark.Stocked) return 0;
                foreach (var pokemon in pastimes.PalPark.TakeSix("Pal Park", GameClock.Today))
                {
                    playerPokedex.RegisterCaught(pokemon.Species.DexNumber);
                    pcBoxStorage.Store(pokemon);
                }
                return 1;
            default:
                return 0;
        }
    }

    // ------------------------------------------------------------------ the Game Corner's machines

    // sub_0203E414: the coins go to the machine, whose mood is the day's deal for its number
    private void OpenSlotMachine(int machine)
    {
        slotScreen.Open(machine, pastimes.Coins.Coins, SlotMachine.MoodOf(machine, encounters.DailyNumber), fieldRandom);
        journal.Tell(new JournalEvent(JournalEventKind.GameCorner));
        currentState = GameState.SlotMachine;
    }

    // sub_0203E35C: the coins come back, and the longest run of bonus rounds is kept if it is the longest yet
    private void UpdateSlotMachine(float dt)
    {
        slotScreen.Update(dt);
        if (slotScreen.IsActive) return;
        CloseSlotMachine();
    }

    private void CloseSlotMachine()
    {
        if (slotScreen.Machine is { } machine)
        {
            pastimes.Coins.Set(machine.Coins);
            if (machine.MostRounds > story.Var(GameCornerPrizes.StreakVariable)) story.SetVar(GameCornerPrizes.StreakVariable, machine.MostRounds);
        }
        currentState = GameState.Overworld;
    }

    // ------------------------------------------------------------------ the Villa

    internal static PropType VillaProp(VillaFurniture piece) => piece.Name switch
    {
        "Table" => PropType.Table,
        "Big Sofa" or "Small Sofa" => PropType.Sofa,
        "Bed" => PropType.Bed,
        "Night Table" => PropType.NightTable,
        "TV" => PropType.Television,
        "Audio System" => PropType.AudioSystem,
        "Bookshelf" => PropType.Bookshelf,
        "Rack" => PropType.Rack,
        "Houseplant" => PropType.Plant,
        "PC Desk" => PropType.PcDesk,
        "Music Box" => PropType.MusicBox,
        "Pokémon Bust" => PropType.Bust,
        "Silver Pokémon Bust" => PropType.SilverBust,
        "Piano" => PropType.Piano,
        "Guest Set" => PropType.GuestSet,
        "Wall Clock" => PropType.Clock,
        "Masterpiece" => PropType.Painting,
        "Tea Set" => PropType.TeaSet,
        _ => PropType.Rug
    };

    /// <summary>The script that says what a piece of the Villa's furniture is when it is looked at (<c>ResortAreaVilla.txt</c>).</summary>
    public static string VillaLookScript(VillaFurniture piece) =>
        "Look" + string.Concat(piece.Flag["FLAG_VILLA_FURNITURE_".Length..].Split('_').Select(word => word[..1] + word[1..].ToLowerInvariant()));

    /// <summary>
    /// <c>Villa_DynamicMapFeaturesInit</c>: what the player owns stands in the room, each piece in its own place, and
    /// blocks the tiles the original's table gives it.
    /// </summary>
    private void FurnishVilla()
    {
        var room = MapDatabase.Get(VillaMap);
        if (room.Name != VillaMap) return;
        foreach (var prop in villaProps) room.Props.Remove(prop);
        villaProps.Clear();
        foreach (var piece in VillaRules.Furniture)
        {
            bool owned = VillaRules.Owns(story, piece);
            foreach (var place in piece.Places)
            {
                if (place.Blocks)
                    for (int y = place.Y; y < place.Y + place.Depth; y++)
                        for (int x = place.X; x < place.X + place.Width; x++)
                        {
                            room.SetSolid(x, y, owned);
                            if (owned) room.TileScripts[(x, y)] = VillaLookScript(piece);
                            else room.TileScripts.Remove((x, y));
                        }
                // The chandelier hangs out of the camera's sight and isn't drawn
                if (!owned || piece.Name == "Chandelier") continue;
                // A bookshelf is a tile wide: the piece is as many as its place holds
                if (piece.Name == "Bookshelf")
                    for (int x = place.X; x < place.X + place.Width; x++) AddVillaProp(room, new Prop { Type = PropType.Bookshelf, X = x, Y = place.Y });
                else AddVillaProp(room, new Prop { Type = VillaProp(piece), X = place.X, Y = place.Y, Width = place.Width, Depth = place.Depth });
            }
        }
        world?.Forget(room, 0, 0, room.Width, room.Height);
    }

    private void AddVillaProp(Map room, Prop prop)
    {
        room.Props.Add(prop);
        villaProps.Add(prop);
    }

    // ------------------------------------------------------------------ Pal Park

    private bool InPalPark => currentMap.Name == PalParkMap;

    /// <summary>The Catching Show's clock runs while the field is the player's.</summary>
    private void TickPalPark(float dt)
    {
        if (InPalPark && !ScriptRunning) pastimes.PalPark.Tick(dt);
    }

    /// <summary>
    /// A step in the park (<c>CatchingShow_CheckWildEncounter</c>): in the grass or on the water of one of the park's
    /// corners, one of the six that keep to it may come out.
    /// </summary>
    private bool PalParkStep()
    {
        var park = pastimes.PalPark;
        if (!InPalPark || park.Show is not { } show || !park.Running) return false;
        int x = player.GridX, y = player.GridY;
        int area = CatchingShow.AreaAt(x, y, currentMap.IsTallGrass(x, y), player.Mode == TravelMode.Surfing);
        int met = show.Step(area);
        if (met < 0) return false;
        fieldEncounter = false;
        MeetWildPokemon(show.Pokemon[met], BattleKind.PalPark);
        return true;
    }

    /// <summary>A battle in the park is over: a catch is the show's next, and the sixth ends it.</summary>
    private void AfterParkBattle()
    {
        if (battle?.Kind != BattleKind.PalPark || pastimes.PalPark.Show is not { } show) return;
        if (battle.Caught == null) return;
        show.CaughtCurrent();
        if (show.Caught >= CatchingShow.Size)
        {
            pastimes.PalPark.Stop();
            StartScript(ParkCaughtAllScript);
        }
    }

    private void DrawParkCount()
    {
        var park = pastimes.PalPark;
        if (InPalPark && park.Show is { } show) ModernUi.ParkCount(VirtualWidth, show.ParkBalls, (int)park.Seconds);
        // The Coin Case's count stands on the screen while someone of the Game Corner or its exchange is spoken to
        if (ScriptRunning && currentMap.Name is "VeilstoneGameCorner" or "VeilstonePrizeExchange" && playerInventory.GetQuantity(ItemDatabase.Get("Coin Case")!) > 0)
            ModernUi.CoinCount(pastimes.Coins.Coins, playerMoney);
    }

    private sealed partial class FieldHost
    {
        public Pastimes Pastimes => game.pastimes;
        public VillaRecords VillaRecords => game.VillaRecordsNow;
        public int PastimeAct(string what, int number, NPC? subject) => game.PastimeAct(what, number, subject);
    }
}
