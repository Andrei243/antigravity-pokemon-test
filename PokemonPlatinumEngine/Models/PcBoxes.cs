using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The Pokémon Storage System as Platinum keeps it (plan 06 · R12; the original's <c>pc_boxes.c</c> and the box
/// application's rules): eighteen boxes of thirty places, each place empty or holding one Pokémon, each box with a
/// name of up to eight letters and a wallpaper, and the box the PC was last left on, where a Pokémon caught with a
/// full team goes first. No drawing or input: the PC's screen (<c>PCScreen</c>) asks these rules.
/// </summary>
public sealed class PcBoxes
{
    public const int BoxCount = 18, Columns = 6, Rows = 5, BoxSize = Columns * Rows;

    /// <summary>The longest name a box can be given (<c>BOX_NAME_LEN</c>).</summary>
    public const int NameLength = 8;

    /// <summary>
    /// The wallpapers, in the original's order: sixteen every game has, then eight a word game at Jubilife TV
    /// unlocks (<see cref="UnlockedWallpapers"/>).
    /// </summary>
    public static readonly IReadOnlyList<string> WallpaperNames = new[]
    {
        "Forest", "City", "Desert", "Savanna", "Crag", "Volcano", "Snow", "Cave", "Beach", "Seafloor", "River", "Sky",
        "Poké Center", "Machine", "Checks", "Simple",
        "Distortion", "Contest", "Nostalgic", "Croagunk", "Trio", "PikaPika", "Legend", "Team Galactic"
    };

    public const int OwnWallpapers = 16;

    /// <summary>
    /// The moves the field needs that a released Pokémon must not take with it (<c>sReleaseBlockingMoves</c>): it
    /// comes back if no other Pokémon of the team or the boxes knows one it knows.
    /// </summary>
    public static readonly IReadOnlyList<string> MovesTheFieldNeeds = new[] { "Surf", "Rock Climb", "Waterfall" };

    public sealed class Box
    {
        public string Name { get; set; } = "";
        public int Wallpaper { get; set; }
        public Pokemon?[] Slots { get; } = new Pokemon?[BoxSize];
        public int Count => Slots.Count(p => p != null);
    }

    private readonly Box[] boxes = new Box[BoxCount];

    public IReadOnlyList<Box> Boxes => boxes;

    /// <summary>The box the PC was last on: a Pokémon sent to the PC goes there first, then on to the next boxes.</summary>
    public int CurrentBox { get; set; }

    /// <summary>A bit for each of the eight wallpapers beyond the sixteen every game has.</summary>
    public int UnlockedWallpapers { get; set; }

    public PcBoxes()
    {
        // Box n is called "Box n" and has wallpaper n, the sixteen round again for the last two
        for (int i = 0; i < BoxCount; i++) boxes[i] = new Box { Name = $"Box {i + 1}", Wallpaper = i % OwnWallpapers };
    }

    public Pokemon? this[int box, int slot] => boxes[box].Slots[slot];

    /// <summary>Every stored Pokémon, box by box, place by place.</summary>
    public IEnumerable<Pokemon> All => boxes.SelectMany(b => b.Slots).OfType<Pokemon>();

    public int Count => boxes.Sum(b => b.Count);
    public bool IsFull => Count >= BoxCount * BoxSize;

    /// <summary>Whether a wallpaper may be put up: one of the sixteen, or one of the eight once unlocked.</summary>
    public bool HasWallpaper(int wallpaper) =>
        wallpaper >= 0 && wallpaper < WallpaperNames.Count && (wallpaper < OwnWallpapers || (UnlockedWallpapers & (1 << (wallpaper - OwnWallpapers))) != 0);

    /// <summary>Names a box: at most eight letters, and an empty name leaves it as it was.</summary>
    public void Rename(int box, string name)
    {
        name = name.Trim();
        if (name.Length == 0) return;
        boxes[box].Name = name.Length > NameLength ? name[..NameLength] : name;
    }

    /// <summary>
    /// Sends a Pokémon to the PC (a catch with a full team, a gift; <c>PCBoxes_TryStoreBoxMon</c>): into the first
    /// empty place of the current box, or of the boxes after it, round to the first. Returns where it went, or
    /// null with every box full.
    /// </summary>
    public (int Box, int Slot)? Store(Pokemon pokemon)
    {
        for (int i = 0; i < BoxCount; i++)
        {
            int box = (CurrentBox + i) % BoxCount;
            int slot = Array.IndexOf(boxes[box].Slots, null);
            if (slot < 0) continue;
            PutIn(box, slot, pokemon);
            return (box, slot);
        }
        return null;
    }

    /// <summary>Takes a Pokémon out of whichever place holds it (one given away in a trade); false if it isn't stored.</summary>
    public bool Remove(Pokemon pokemon)
    {
        foreach (var box in boxes)
            for (int i = 0; i < BoxSize; i++)
                if (box.Slots[i] == pokemon)
                {
                    box.Slots[i] = null;
                    return true;
                }
        return false;
    }

    /// <summary>Puts a Pokémon into an empty place, as storing it there does to it (<see cref="AsStored"/>).</summary>
    public void PutIn(int box, int slot, Pokemon pokemon)
    {
        if (boxes[box].Slots[slot] != null) throw new InvalidOperationException($"Box {box + 1} has a Pokémon at {slot} already.");
        AsStored(pokemon);
        boxes[box].Slots[slot] = pokemon;
    }

    /// <summary>Takes a Pokémon out of its place, leaving the place empty.</summary>
    public Pokemon? TakeOut(int box, int slot)
    {
        var pokemon = boxes[box].Slots[slot];
        boxes[box].Slots[slot] = null;
        return pokemon;
    }

    /// <summary>
    /// Moves what is in one place to another (the box's MOVE: put down, or swapped with what is there).
    /// </summary>
    public void Swap(int fromBox, int fromSlot, int toBox, int toSlot) =>
        (boxes[fromBox].Slots[fromSlot], boxes[toBox].Slots[toSlot]) = (boxes[toBox].Slots[toSlot], boxes[fromBox].Slots[fromSlot]);

    /// <summary>
    /// What going into a box does to a Pokémon (<c>BoxPokemon_RestorePP</c>, <c>BoxPokemon_SetShayminForm</c>): its
    /// PP come back and a Shaymin goes back to its Land Forme. It keeps what it holds.
    /// </summary>
    public static void AsStored(Pokemon pokemon)
    {
        foreach (var move in pokemon.Moves) move.CurrentPP = move.MaxPP;
        FormRules.BackToLand(pokemon);
    }

    /// <summary>
    /// What coming out of a box does to it (<c>Pokemon_FromBoxPokemon</c>, which works its stats out afresh): full
    /// HP and no condition.
    /// </summary>
    public static void AsWithdrawn(Pokemon pokemon)
    {
        pokemon.RecalculateStats();
        pokemon.CurrentHP = pokemon.MaxHP;
        pokemon.Status = StatusCondition.None;
        pokemon.SleepTurns = 0;
        pokemon.ToxicCounter = 0;
    }

    /// <summary>Whether a Pokémon of the team can still battle (the original counts those that aren't eggs and have HP).</summary>
    private static bool CanBattle(Pokemon p) => p.CurrentHP > 0;

    /// <summary>Whether it holds Mail, which keeps it out of the boxes and from being released (the original's checks).</summary>
    public static bool HoldsMail(Pokemon p) => p.HeldItem?.Pocket == ItemPocket.Mail;

    /// <summary>
    /// Why a member of the team can't go into a box, or null if it can (<c>BoxAppMan_OnLastAliveMon</c> and the
    /// Mail check): the last Pokémon able to battle stays, as does one holding Mail; and a full PC has no room.
    /// </summary>
    public string? WhyNotDeposit(Party party, int index)
    {
        var pokemon = party.Members[index];
        if (CanBattle(pokemon) && party.Members.Count(CanBattle) < 2) return "That's your last Pokémon able to battle!";
        if (HoldsMail(pokemon)) return "Please remove the Mail first.";
        if (IsFull) return "The boxes are full.";
        return null;
    }

    /// <summary>Puts a member of the team into a box's empty place.</summary>
    public void Deposit(Party party, int index, int box, int slot)
    {
        var pokemon = party.Members[index];
        party.RemoveAt(index);
        PutIn(box, slot, pokemon);
    }

    /// <summary>Takes a stored Pokémon onto the team: false when the team is full.</summary>
    public bool Withdraw(Party party, int box, int slot)
    {
        if (party.IsFull || boxes[box].Slots[slot] is not { } pokemon) return false;
        boxes[box].Slots[slot] = null;
        AsWithdrawn(pokemon);
        party.Add(pokemon);
        return true;
    }

    /// <summary>
    /// What releasing a Pokémon comes to, by the box application's checks: refused while it holds Mail or is the
    /// team's last that can battle; and if it knows a move the field needs (Surf, Rock Climb, Waterfall) that no
    /// other Pokémon of the team or the boxes knows, it comes back (<see cref="ReleaseOutcome.CameBack"/>).
    /// </summary>
    public ReleaseOutcome WhetherToRelease(Pokemon pokemon, Party party)
    {
        if (HoldsMail(pokemon)) return ReleaseOutcome.HoldsMail;
        if (party.Members.Contains(pokemon) && CanBattle(pokemon) && party.Members.Count(CanBattle) < 2) return ReleaseOutcome.LastOne;
        var others = party.Members.Concat(All).Where(p => p != pokemon).ToList();
        foreach (string move in MovesTheFieldNeeds)
            if (pokemon.Moves.Any(m => m.Name == move) && !others.Any(o => o.Moves.Any(m => m.Name == move)))
                return ReleaseOutcome.CameBack;
        return ReleaseOutcome.Released;
    }

    /// <summary>Lets a Pokémon go, from its place in a box or from the team, if <see cref="WhetherToRelease"/> allows it.</summary>
    public ReleaseOutcome Release(Pokemon pokemon, Party party)
    {
        var outcome = WhetherToRelease(pokemon, party);
        if (outcome != ReleaseOutcome.Released) return outcome;
        int at = party.Members.IndexOf(pokemon);
        if (at >= 0) party.RemoveAt(at);
        else
            foreach (var box in boxes)
                for (int i = 0; i < BoxSize; i++)
                    if (box.Slots[i] == pokemon) box.Slots[i] = null;
        return outcome;
    }

    /// <summary>
    /// The boxes of a save from before each Pokémon had a place of its own: the list laid out thirty to a box in
    /// the order it was kept, which is how the PC showed it.
    /// </summary>
    public static PcBoxes FromList(IEnumerable<Pokemon> stored)
    {
        var pc = new PcBoxes();
        int i = 0;
        foreach (var pokemon in stored)
        {
            if (i >= BoxCount * BoxSize) break;
            pc.boxes[i / BoxSize].Slots[i % BoxSize] = pokemon;
            i++;
        }
        return pc;
    }
}

/// <summary>What happened when a Pokémon was to be released.</summary>
public enum ReleaseOutcome { Released, CameBack, HoldsMail, LastOne }

/// <summary>The six marks the boxes put on a Pokémon (<c>MAX_POKEMON_MARKINGS</c>), a bit each in <see cref="Pokemon.Marks"/>.</summary>
[Flags]
public enum Markings { None = 0, Circle = 1, Triangle = 2, Square = 4, Heart = 8, Star = 16, Diamond = 32 }
