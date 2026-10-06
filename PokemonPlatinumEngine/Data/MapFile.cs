using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using TownArchitecture = PokemonPlatinumEngine.Overworld.Architecture;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// One map as stored in <c>Data/maps/&lt;Name&gt;.json</c>. The tile layers are rows of characters, one per tile
/// (see <see cref="TileCodes"/>); everything placed on the map is a list of plain records.
/// </summary>
public sealed class MapFile
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string BgmTrack { get; set; } = "";
    public InteriorStyle Interior { get; set; } = InteriorStyle.None;
    public TreeStyle Trees { get; set; } = TreeStyle.Round;

    /// <summary>How the town's houses are built; left out for the default, <see cref="TownArchitecture.Timber"/>.</summary>
    public TownArchitecture? Architecture { get; set; }

    /// <summary>The stage its battles are fought on; left out to let the map decide (see <see cref="Map.ArenaAt"/>).</summary>
    public BattleArena? BattleArena { get; set; }

    /// <summary>For a gym or a room of the League: the type its stage is themed on.</summary>
    public PokemonType? ArenaType { get; set; }

    /// <summary>Places some Pokémon evolve at (see <see cref="Map.EvolutionSites"/>); left out when there are none.</summary>
    public List<string>? EvolutionSites { get; set; }

    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>Ground tiles, one string per row, one <see cref="TileCodes"/> character per tile.</summary>
    public List<string> Ground { get; set; } = new();

    /// <summary>Blocked tiles: '#' is solid, '.' is open. Furniture props are already marked here.</summary>
    public List<string> Solid { get; set; } = new();

    /// <summary>Optional overhead tiles; '-' means none.</summary>
    public List<string>? Overhead { get; set; }

    public List<PropRecord> Props { get; set; } = new();

    /// <summary>
    /// Buildings whose kind can't be told from where their door leads: each names one tile of the building.
    /// Left out when there are none.
    /// </summary>
    public List<BuildingRecord>? Buildings { get; set; }

    public List<Warp> Warps { get; set; } = new();
    public List<SignRecord> Signboards { get; set; } = new();
    public List<NpcRecord> Npcs { get; set; } = new();

    /// <summary>Tiles that start a script when stepped on; left out when there are none.</summary>
    public List<TriggerRecord>? Triggers { get; set; }

    /// <summary>Items nobody can see, found by looking at their tile; left out when there are none.</summary>
    public List<HiddenItemRecord>? HiddenItems { get; set; }
    public List<WildEncounterEntry> WildEncounters { get; set; } = new();

    public sealed class PropRecord
    {
        public PropType Type { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; } = 1;
        public int Depth { get; set; } = 1;
    }

    public sealed class BuildingRecord
    {
        public int X { get; set; }
        public int Y { get; set; }
        public BuildingKind Kind { get; set; }
    }

    public sealed class SignRecord
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string Text { get; set; } = string.Empty;

        /// <summary>A script the sign runs instead of only being read (its text is what <c>sayown</c> says); left out for a plain sign.</summary>
        public string? Script { get; set; }
    }

    /// <summary>An item hidden in the ground: the tile, the item, how many (one when left out) and the story flag of its finding.</summary>
    public sealed class HiddenItemRecord
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string Item { get; set; } = string.Empty;
        public int? Count { get; set; }
        public string Flag { get; set; } = string.Empty;
    }

    /// <summary>A rectangle of tiles that starts a script when the player steps into it.</summary>
    public sealed class TriggerRecord
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; } = 1;
        public int Depth { get; set; } = 1;
        public string Script { get; set; } = string.Empty;

        /// <summary>The story variable it goes by, and the value it fires at; left out, it fires every time.</summary>
        public string? Variable { get; set; }
        public int? Value { get; set; }
    }

    public sealed class NpcRecord
    {
        /// <summary>Only for NPCs other data refers to; the rest get a fresh id when the map loads.</summary>
        public string? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NpcType { get; set; } = "Trainer";
        public int X { get; set; }
        public int Y { get; set; }
        public Direction Facing { get; set; } = Direction.Down;
        public List<string>? Dialog { get; set; }
        public bool? IsStarterBriefcase { get; set; }
        public bool? IsHealingNurse { get; set; }
        public bool? IsPokeMartClerk { get; set; }
        public bool? IsPCTerminal { get; set; }
        public bool? IsTransportAttendant { get; set; }

        /// <summary>The script talking to them runs, where it isn't the common one for what they are (docs/scripts.md).</summary>
        public string? Script { get; set; }

        /// <summary>A story flag that takes them off the map while it is set.</summary>
        public string? HiddenBy { get; set; }

        /// <summary>A story flag they wait for: they are on the map only while it is set.</summary>
        public string? ShownBy { get; set; }

        /// <summary>
        /// For an item lying in its ball (<c>npcType</c> "ItemBall"): the item and how many (one when left out).
        /// <see cref="HiddenBy"/> is then the flag set as it is picked up, which is what keeps it gone.
        /// </summary>
        public string? Item { get; set; }
        public int? Count { get; set; }
        public TrainerRecord? Trainer { get; set; }
    }

    public sealed class TrainerRecord
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string TrainerClass { get; set; } = "Trainer";
        public List<PartyMember> Party { get; set; } = new();
        public int PrizeMoney { get; set; } = 300;
        public string DialogueBefore { get; set; } = string.Empty;
        public string DialogueAfter { get; set; } = string.Empty;
        public int SightRange { get; set; } = 3;

        /// <summary>Two Pokémon at a time, when the player has two that can fight (twins, couples).</summary>
        public bool? DoubleBattle { get; set; }
    }

    public sealed class PartyMember
    {
        public string Species { get; set; } = string.Empty;
        public int Level { get; set; }

        /// <summary>The moves it knows, where the trainer chose them; left out, it knows what its level taught it.</summary>
        public List<string>? Moves { get; set; }
    }

    /// <summary>Builds a fresh, playable map: new NPC state and newly rolled trainer Pokémon each time.</summary>
    public Map ToMap()
    {
        var map = new Map(Width, Height)
        {
            Name = Name,
            DisplayName = DisplayName,
            BgmTrack = BgmTrack,
            Interior = Interior,
            Trees = Trees,
            Architecture = Architecture ?? TownArchitecture.Timber,
            Arena = BattleArena,
            ArenaType = ArenaType,
            EvolutionSites = EvolutionSites?.ToList() ?? new()
        };

        CheckRows(Ground, "ground");
        CheckRows(Solid, "solid");
        if (Overhead != null) CheckRows(Overhead, "overhead");

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                char solid = Solid[y][x];
                if (solid != '#' && solid != '.')
                    throw new InvalidDataException($"Map {Name}: solid row {y} has '{solid}' at column {x}; use '#' or '.'.");
                map.SetGroundTile(x, y, TileCodes.Parse(Ground[y][x], Name), isSolid: solid == '#');

                if (Overhead != null && Overhead[y][x] != TileCodes.None)
                    map.SetOverheadTile(x, y, TileCodes.Parse(Overhead[y][x], Name));
            }
        }

        // The solid grid already includes furniture, so props are listed without marking tiles again
        foreach (var p in Props)
            map.Props.Add(new Prop { Type = p.Type, X = p.X, Y = p.Y, Width = p.Width, Depth = p.Depth });

        foreach (var b in Buildings ?? new())
        {
            if (!map.InBounds(b.X, b.Y))
                throw new InvalidDataException($"Map {Name}: the {b.Kind} building at {b.X},{b.Y} is outside the map.");
            map.BuildingKinds[(b.X, b.Y)] = b.Kind;
        }

        foreach (var w in Warps)
            map.Warps.Add(new Warp { SourceX = w.SourceX, SourceY = w.SourceY, TargetMap = w.TargetMap, TargetX = w.TargetX, TargetY = w.TargetY, TargetFacing = w.TargetFacing });

        foreach (var s in Signboards)
        {
            map.Signboards[(s.X, s.Y)] = s.Text;
            if (!string.IsNullOrEmpty(s.Script)) map.SignScripts[(s.X, s.Y)] = s.Script;
        }

        foreach (var t in Triggers ?? new())
            map.Triggers.Add(new StepTrigger { X = t.X, Y = t.Y, Width = t.Width, Depth = t.Depth, Script = t.Script, Variable = t.Variable, Value = t.Value ?? 0 });

        foreach (var h in HiddenItems ?? new())
        {
            if (ItemDatabase.Get(h.Item) == null) throw new InvalidDataException($"Map {Name}: the hidden item at {h.X},{h.Y} is '{h.Item}', which is no item.");
            if (h.Flag.Length == 0) throw new InvalidDataException($"Map {Name}: the hidden {h.Item} at {h.X},{h.Y} has no flag, so it would be found again and again.");
            map.HiddenItems[(h.X, h.Y)] = new HiddenItem(h.Item, h.Count ?? 1, h.Flag);
        }

        foreach (var n in Npcs)
            map.Add(BuildNpc(n, Name));

        foreach (var e in WildEncounters)
            map.WildEncounters.Add(new WildEncounterEntry { SpeciesName = e.SpeciesName, MinLevel = e.MinLevel, MaxLevel = e.MaxLevel, Weight = e.Weight });

        return map;
    }

    /// <summary>A fresh person from their record: a trainer's Pokémon are rolled anew each time.</summary>
    internal static NPC BuildNpc(NpcRecord n, string mapName)
    {
        var npc = new NPC
        {
            Name = n.Name,
            NpcType = n.NpcType,
            GridX = n.X,
            GridY = n.Y,
            Facing = n.Facing,
            DialogLines = n.Dialog != null ? new List<string>(n.Dialog) : new(),
            IsStarterBriefcase = n.IsStarterBriefcase ?? false,
            IsHealingNurse = n.IsHealingNurse ?? false,
            IsPokeMartClerk = n.IsPokeMartClerk ?? false,
            IsPCTerminal = n.IsPCTerminal ?? false,
            IsTransportAttendant = n.IsTransportAttendant ?? false,
            Key = n.Id,
            Script = n.Script,
            HiddenBy = n.HiddenBy,
            ShownBy = n.ShownBy,
            Item = n.Item,
            ItemCount = n.Count ?? 1
        };
        if (n.Id != null) npc.Id = n.Id;
        if (npc.IsItemBall || npc.Item != null)
        {
            if (!npc.IsItemBall) throw new InvalidDataException($"Map {mapName}: {n.Name} holds an item and is no item ball (npcType \"{NPC.ItemBallType}\").");
            if (npc.Item == null || ItemDatabase.Get(npc.Item) == null) throw new InvalidDataException($"Map {mapName}: the item ball at {n.X},{n.Y} holds '{npc.Item}', which is no item.");
            if (string.IsNullOrEmpty(npc.HiddenBy)) throw new InvalidDataException($"Map {mapName}: the {npc.Item} at {n.X},{n.Y} has no hiddenBy flag, so it would come back.");
        }

        if (n.Trainer is { } t)
        {
            var party = new Party();
            foreach (var member in t.Party)
            {
                var species = PokemonDatabase.Get(member.Species)
                    ?? throw new InvalidDataException($"Map {mapName}: trainer {t.Id} has unknown species '{member.Species}'.");
                var pokemon = new Pokemon(species, member.Level);
                if (member.Moves is { Count: > 0 } chosen)
                {
                    pokemon.Moves.Clear();
                    foreach (string name in chosen)
                        pokemon.Moves.Add(new Move(MoveDatabase.Get(name)
                            ?? throw new InvalidDataException($"Map {mapName}: trainer {t.Id}'s {member.Species} has the unknown move '{name}'.")));
                }
                party.Add(pokemon);
            }

            npc.IsTrainer = true;
            npc.TrainerData = new Trainer
            {
                Id = t.Id,
                Name = t.Name,
                TrainerClass = t.TrainerClass,
                Party = party,
                PrizeMoney = t.PrizeMoney,
                DialogueBefore = t.DialogueBefore,
                DialogueAfter = t.DialogueAfter,
                SightRange = t.SightRange,
                DoubleBattle = t.DoubleBattle ?? false
            };
        }

        return npc;
    }

    private void CheckRows(List<string> rows, string layer)
    {
        if (rows.Count != Height)
            throw new InvalidDataException($"Map {Name}: {layer} has {rows.Count} rows, expected {Height}.");
        for (int y = 0; y < rows.Count; y++)
        {
            if (rows[y].Length != Width)
                throw new InvalidDataException($"Map {Name}: {layer} row {y} has {rows[y].Length} tiles, expected {Width}.");
        }
    }

    /// <summary>Captures a map as it stands, for writing back to a file.</summary>
    public static MapFile FromMap(Map map)
    {
        var file = new MapFile
        {
            Name = map.Name,
            DisplayName = map.DisplayName,
            BgmTrack = map.BgmTrack,
            Interior = map.Interior,
            Trees = map.Trees,
            Architecture = map.Architecture == TownArchitecture.Timber ? null : map.Architecture,
            BattleArena = map.Arena,
            ArenaType = map.ArenaType,
            EvolutionSites = map.EvolutionSites.Count > 0 ? map.EvolutionSites.ToList() : null,
            Width = map.Width,
            Height = map.Height
        };

        bool anyOverhead = false;
        var overhead = new List<string>();
        for (int y = 0; y < map.Height; y++)
        {
            var ground = new StringBuilder(map.Width);
            var solid = new StringBuilder(map.Width);
            var over = new StringBuilder(map.Width);
            for (int x = 0; x < map.Width; x++)
            {
                ground.Append(TileCodes.CodeOf(map.GetGroundTile(x, y)));
                solid.Append(map.IsSolid(x, y) ? '#' : '.');
                var top = map.GetOverheadTile(x, y);
                anyOverhead |= top.HasValue;
                over.Append(top.HasValue ? TileCodes.CodeOf(top.Value) : TileCodes.None);
            }
            file.Ground.Add(ground.ToString());
            file.Solid.Add(solid.ToString());
            overhead.Add(over.ToString());
        }
        if (anyOverhead) file.Overhead = overhead;

        file.Props = map.Props.Select(p => new PropRecord { Type = p.Type, X = p.X, Y = p.Y, Width = p.Width, Depth = p.Depth }).ToList();
        if (map.BuildingKinds.Count > 0)
            file.Buildings = map.BuildingKinds.Select(kv => new BuildingRecord { X = kv.Key.X, Y = kv.Key.Y, Kind = kv.Value }).ToList();
        file.Warps = map.Warps.ToList();
        file.Signboards = map.Signboards.Select(kv => new SignRecord { X = kv.Key.X, Y = kv.Key.Y, Text = kv.Value, Script = map.SignScripts.GetValueOrDefault(kv.Key) }).ToList();
        // Everyone the map has, whether or not the story has them on it just now
        file.Npcs = map.Everyone.OrderBy(n => n.Order < 0 ? int.MaxValue : n.Order).Select(ToRecord).ToList();
        if (map.Triggers.Count > 0)
            file.Triggers = map.Triggers.Select(t => new TriggerRecord
            {
                X = t.X, Y = t.Y, Width = t.Width, Depth = t.Depth, Script = t.Script,
                Variable = t.Variable, Value = t.Variable != null ? t.Value : null
            }).ToList();
        if (map.HiddenItems.Count > 0)
            file.HiddenItems = map.HiddenItems.Select(kv => new HiddenItemRecord
            {
                X = kv.Key.X, Y = kv.Key.Y, Item = kv.Value.Item, Count = kv.Value.Count > 1 ? kv.Value.Count : null, Flag = kv.Value.Flag
            }).ToList();
        file.WildEncounters = map.WildEncounters.ToList();
        return file;
    }

    private static NpcRecord ToRecord(NPC npc) => new()
    {
        Id = Guid.TryParse(npc.Id, out _) ? null : npc.Id,
        Name = npc.Name,
        NpcType = npc.NpcType,
        X = npc.GridX,
        Y = npc.GridY,
        Facing = npc.Facing,
        Dialog = npc.DialogLines.Count > 0 ? npc.DialogLines.ToList() : null,
        IsStarterBriefcase = npc.IsStarterBriefcase ? true : null,
        IsHealingNurse = npc.IsHealingNurse ? true : null,
        IsPokeMartClerk = npc.IsPokeMartClerk ? true : null,
        IsPCTerminal = npc.IsPCTerminal ? true : null,
        IsTransportAttendant = npc.IsTransportAttendant ? true : null,
        Script = npc.Script,
        HiddenBy = npc.HiddenBy,
        ShownBy = npc.ShownBy,
        Item = npc.Item,
        Count = npc.Item != null && npc.ItemCount > 1 ? npc.ItemCount : null,
        Trainer = npc.IsTrainer && npc.TrainerData is { } t ? new TrainerRecord
        {
            Id = t.Id,
            Name = t.Name,
            TrainerClass = t.TrainerClass,
            Party = t.Party.Members.Select(p => new PartyMember { Species = p.Species.Name, Level = p.Level, Moves = ChosenMoves(p) }).ToList(),
            PrizeMoney = t.PrizeMoney,
            DialogueBefore = t.DialogueBefore,
            DialogueAfter = t.DialogueAfter,
            SightRange = t.SightRange,
            DoubleBattle = t.DoubleBattle ? true : null
        } : null
    };

    /// <summary>A trainer's Pokémon's moves, where they are not simply what its level taught it.</summary>
    private static List<string>? ChosenMoves(Pokemon pokemon)
    {
        var known = pokemon.Moves.Select(m => m.Name).ToList();
        var taught = new Pokemon(pokemon.Species, pokemon.Level).Moves.Select(m => m.Name);
        return known.SequenceEqual(taught) ? null : known;
    }
}

/// <summary>The one-character codes for tiles in map files.</summary>
public static class TileCodes
{
    public const char None = '-';

    private static readonly (TileType Type, char Code)[] Table =
    {
        (TileType.Grass, '.'),
        (TileType.FlowerGrass, '*'),
        (TileType.TallGrass, 'w'),
        (TileType.Path, ':'),
        (TileType.Water, '~'),
        (TileType.LedgeDown, 'v'),
        (TileType.Tree, 'T'),
        (TileType.TreeTrunk, 't'),
        (TileType.RoofRed, 'r'),
        (TileType.RoofBlue, 'b'),
        (TileType.RoofGreen, 'g'),
        (TileType.Wall, '#'),
        (TileType.Door, 'D'),
        (TileType.Floor, '_'),
        (TileType.Signpost, 'S'),
        (TileType.PC, 'P'),
        (TileType.Sand, ','),
        (TileType.Dirt, ';'),
        (TileType.Snow, '^'),
        (TileType.CaveFloor, 'c'),
        (TileType.LedgeLeft, '<'),
        (TileType.LedgeRight, '>'),
        (TileType.Rock, 'R'),
        (TileType.Ice, 'i'),
        (TileType.Planks, '='),
        (TileType.Stairs, 's'),
        (TileType.Marsh, 'm'),
        (TileType.Paving, '+'),
        (TileType.Walkway, 'H'),
        (TileType.CaveWall, 'X'),
        (TileType.CaveMouth, 'M'),
        (TileType.ForestMouth, 'E'),
        (TileType.Puddle, 'p')
    };

    public static char CodeOf(TileType type)
    {
        foreach (var (t, c) in Table)
            if (t == type) return c;
        throw new ArgumentOutOfRangeException(nameof(type), type, "Tile type has no map file code.");
    }

    public static TileType Parse(char code, string mapName)
    {
        foreach (var (t, c) in Table)
            if (c == code) return t;
        throw new InvalidDataException($"Map {mapName}: unknown tile code '{code}'.");
    }

    public static IEnumerable<TileType> AllTypes => Table.Select(e => e.Type);
}
