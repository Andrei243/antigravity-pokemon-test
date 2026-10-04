using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The kind of body a generated Pokémon has (plan 03 · D5): its Pokédex shape, refined by the words of its
/// category ("Mouse", "River Crab", "Cocoon"). Each kind is sculpted on one of the body plans.
/// </summary>
internal enum BodyKind
{
    Quadruped, Upright, Humanoid, Legs, Bird, Bat, Insect, Serpent, Crawler, Arthropod, Fish,
    Ball, Armed, Blob, Cluster, Cocoon, Jelly, Tentacled
}

/// <summary>How a four- or two-legged body stands: ordinary, long in the leg and neck (hoofed), low and long (reptiles, turtles) or heavy.</summary>
internal enum Stance { Normal, Long, Low, Stocky }

internal enum EarKind { None, Round, Pointed, Long, Floppy, Fin, Tuft }

internal enum TailKind { None, Stub, Thin, Bushy, Thick, Bolt, Paddle, Fin, Curl, Wisp }

/// <summary>What the end of a tail carries.</summary>
internal enum TipKind { None, Tuft, Flame, Leaf, Ball, Star, Spike }

/// <summary>What grows on top of the head.</summary>
internal enum TopKind { None, Horn, Horns, Antlers, Crest, Sprout, Flower, Flame, Antennae, Tuft, Gem, Cap, Crown }

/// <summary>What the back carries.</summary>
internal enum BackKind { None, Shell, Bulb, Leaves, Flower, Spikes, Plates, Mane, Flames, Mushroom, Fin, Crystals }

internal enum MouthKind { None, Muzzle, Snout, Beak, Bill, Jaw, Trunk, Mandibles }

internal enum PatternKind { None, Belly, Spots, Stripes, Mask, Socks, TwoTone, ChestMark }

internal enum WingKind { None, Feather, Bat, Insect, Butterfly, Dragon }

internal enum HandKind { Paw, Fist, Claw, Blade, Pincer, Leaf, Flipper }

/// <summary>The head's proportions: round, wide, tall, long in the face, or flat.</summary>
internal enum HeadShape { Round, Wide, Tall, Long, Flat }

/// <summary>A few category words call for a body of their own within a kind (a star, a candle, a seashell).</summary>
internal enum Special { None, Star, Candle, Mound, Bivalve, Pumpkin, Cone, Crescent, Sun, Bell, Balloon, Gear, Box, Sword, Spiral, Cotton, Tree }

/// <summary>
/// Everything a generated model is made of (plan 03 · D5), worked out from the species' data alone: its body kind
/// and plan, proportions, colours, materials and parts. Choices that should run through an evolutionary line (the
/// ears, the tail, the eyes, the shade of its colour) come from a seed shared by the family; the rest from the
/// species' own seed, so every species differs. No GPU calls.
/// </summary>
internal sealed class PokeGenome
{
    public string Species = "";
    public string Category = "";
    public int Dex;

    /// <summary>0 for a basic species, 1 and 2 for its evolutions; -1 for a baby.</summary>
    public int Stage;
    public bool Legendary;

    public BodyKind Kind;
    public BodyPlan Plan;
    public Stance Stance;
    public Special Special;
    public bool Hovers;

    /// <summary>Share of the sprite frame the model fills, from the species' height.</summary>
    public float Fill;

    // Proportions, each about 1 for an ordinary build
    public float HeadScale = 1f, LegScale = 1f, BodyLength = 1f, Girth = 1f, NeckLength;

    public PokemonType Primary;
    public PokemonType? Secondary;

    // Colours
    public Color Main, Second, Belly, Dark, Eye;
    public bool IrisEye, ScleraEye;
    public float EyeScale = 1f;
    public SurfaceMaterial Coat = SurfaceMaterial.Fur;

    // Parts
    public EarKind Ears;
    public TailKind Tail;
    public TipKind TailTip;
    public int Tails = 1;
    public TopKind Top;
    public BackKind Back;
    public MouthKind Mouth;
    public PatternKind Pattern;
    public WingKind Wings;
    public HandKind Hands;
    public bool Cheeks, Claws, Fangs, Rocky, Icy, Metallic, Robot, OneEye, Whiskers;
    public HeadShape HeadShape;

    /// <summary>Ear and tail tips in a colour of their own; a fluffy ruff round the neck; hands and feet in another colour; a saddle of colour over the back.</summary>
    public bool Tips, Ruff, Gloves, Saddle;

    /// <summary>How many: spikes on the back, heads in a cluster, legs of a crawler or a spider, tentacles, segments.</summary>
    public int Spikes, Heads = 1, LegPairs = 2, Tentacles, Segments;

    /// <summary>A number the sculptor draws its own small variations from.</summary>
    public uint Seed;

    /// <summary>All of it as text: two species with the same text would look the same (tests compare them).</summary>
    public string Describe()
    {
        var sb = new StringBuilder();
        sb.Append(Kind).Append(',').Append(Plan).Append(',').Append(Stance).Append(',').Append(Special).Append(',').Append(Hovers).Append(',').Append(Stage).Append(',');
        foreach (float f in new[] { Fill, HeadScale, LegScale, BodyLength, Girth, NeckLength, EyeScale })
            sb.Append(f.ToString("F3")).Append(',');
        foreach (var c in new[] { Main, Second, Belly, Dark, Eye }) sb.Append(c.R).Append('.').Append(c.G).Append('.').Append(c.B).Append(',');
        sb.Append(IrisEye).Append(ScleraEye).Append(Coat).Append(',').Append(Ears).Append(Tail).Append(TailTip).Append(Tails).Append(',')
            .Append(Top).Append(Back).Append(Mouth).Append(Pattern).Append(Wings).Append(Hands).Append(',')
            .Append(Cheeks).Append(Claws).Append(Fangs).Append(Rocky).Append(Icy).Append(Metallic).Append(Robot).Append(OneEye).Append(Whiskers).Append(',')
            .Append(HeadShape).Append(Tips).Append(Ruff).Append(Gloves).Append(Saddle).Append(',')
            .Append(Spikes).Append(',').Append(Heads).Append(',').Append(LegPairs).Append(',').Append(Tentacles).Append(',').Append(Segments).Append(',').Append(Seed);
        return sb.ToString();
    }
}

/// <summary>
/// A small deterministic random generator (SplitMix32), so a species' model is the same on every run and every
/// version of .NET (the mesh cache is keyed by the model).
/// </summary>
internal sealed class GenomeRandom
{
    private uint state;

    public GenomeRandom(uint seed) => state = seed;

    public uint Next()
    {
        uint z = state += 0x9E3779B9u;
        z = (z ^ (z >> 16)) * 0x85EBCA6Bu;
        z = (z ^ (z >> 13)) * 0xC2B2AE35u;
        return z ^ (z >> 16);
    }

    /// <summary>0 (inclusive) to 1 (exclusive).</summary>
    public float Float() => (Next() >> 8) * (1f / 16777216f);

    public float Range(float a, float b) => a + (b - a) * Float();

    public int Int(int n) => n <= 1 ? 0 : (int)(Next() % (uint)n);

    public bool Chance(float p) => Float() < p;

    public T Pick<T>(params T[] items) => items[Int(items.Length)];

    public static uint Hash(int value, uint salt)
    {
        uint h = (uint)value * 0x27D4EB2Du ^ salt * 0x165667B1u;
        h ^= h >> 15;
        h *= 0x2C1B3C6Du;
        h ^= h >> 12;
        return h;
    }
}

/// <summary>Works out the <see cref="PokeGenome"/> of any species from its data (plan 03 · D5).</summary>
internal static class PokemonGenomes
{
    private static readonly Dictionary<string, PokeGenome> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object Gate = new();
    private static Dictionary<string, string>? preEvolution;

    /// <summary>The genome of a species (null for a name that isn't one).</summary>
    public static PokeGenome? For(string species)
    {
        lock (Gate)
        {
            if (Cache.TryGetValue(species, out var g)) return g;
        }
        var data = PokemonDatabase.Get(species);
        if (data == null) return null;
        var made = Make(data);
        lock (Gate) Cache[species] = made;
        return made;
    }

    // ------------------------------------------------------------------ families

    /// <summary>The species each species evolves from (none for the first of a line).</summary>
    private static Dictionary<string, string> PreEvolutions()
    {
        lock (Gate)
        {
            if (preEvolution != null) return preEvolution;
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in PokemonDatabase.GetAll().OrderBy(s => s.DexNumber))
                foreach (var e in s.Evolutions ?? new List<EvolutionData>())
                    if (!string.IsNullOrEmpty(e.TargetSpecies) && !map.ContainsKey(e.TargetSpecies) && !e.TargetSpecies.Equals(s.Name, StringComparison.OrdinalIgnoreCase))
                        map[e.TargetSpecies] = s.Name;
            return preEvolution = map;
        }
    }

    /// <summary>The first species of the line and how many evolutions it took to get here.</summary>
    internal static (PokemonSpecies Root, int Steps) Family(PokemonSpecies s)
    {
        var map = PreEvolutions();
        var root = s;
        int steps = 0;
        while (steps < 4 && map.TryGetValue(root.Name, out var before) && PokemonDatabase.Get(before) is { } prev)
        {
            root = prev;
            steps++;
        }
        return (root, steps);
    }

    // ------------------------------------------------------------------ words

    /// <summary>
    /// Whether the category (or the name) has one of these words: whole words, or the end of a run-together word
    /// for longer ones ("EleSquirrel" has "squirrel"), so "cat" never matches "Catcher".
    /// </summary>
    private sealed class Words
    {
        private readonly string[] words;
        private readonly string name;

        public Words(PokemonSpecies s)
        {
            words = s.Category.ToLowerInvariant().Split(new[] { ' ', '-', '\'' }, StringSplitOptions.RemoveEmptyEntries);
            name = s.Name.ToLowerInvariant();
        }

        public bool Any(params string[] keys)
        {
            foreach (var k in keys)
                foreach (var w in words)
                    if (w == k || w == k + "s" || (k.Length >= 4 && w.EndsWith(k, StringComparison.Ordinal))) return true;
            return false;
        }

        public bool Name(params string[] parts)
        {
            foreach (var p in parts)
                if (name.Contains(p, StringComparison.Ordinal)) return true;
            return false;
        }
    }

    private static readonly string[] Felines = { "cat", "kitten", "lion", "tiger", "lynx", "leopard", "panther", "cheetah", "feline", "lioness" };
    private static readonly string[] Canines = { "dog", "puppy", "wolf", "fox", "jackal", "hound", "coyote", "canine", "foxling", "pup" };
    private static readonly string[] Rodents = { "mouse", "rat", "hamster", "chinchilla", "squirrel", "rodent", "mole", "shrew" };
    private static readonly string[] Bears = { "bear", "panda", "koala", "cub" };
    private static readonly string[] Rabbits = { "rabbit", "bunny", "hare" };
    private static readonly string[] Hoofed = { "horse", "pony", "unicorn", "deer", "reindeer", "moose", "elk", "fawn", "stallion", "colt", "goat", "sheep", "ram", "llama", "bull", "ox", "cattle", "bison", "buffalo", "cow" };
    private static readonly string[] Pigs = { "pig", "boar", "hog", "swine", "piglet" };
    private static readonly string[] Reptiles = { "lizard", "gecko", "iguana", "chameleon", "salamander", "croc", "crocodile", "alligator", "gator", "dinosaur", "raptor", "monitor", "reptile" };
    private static readonly string[] Turtles = { "turtle", "tortoise", "terrapin", "shell", "shellfish", "armadillo", "snail" };
    private static readonly string[] Birds = { "bird", "chick", "duck", "owl", "penguin", "eagle", "hawk", "crow", "raven", "swan", "pelican", "parrot", "heron", "crane", "sparrow", "starling", "robin", "swallow", "pigeon", "vulture", "flamingo", "songbird", "rooster", "hen", "beak", "falcon", "stork", "gull", "seagull", "albatross", "cardinal", "puffin", "kingfisher", "condor" };
    private static readonly string[] Bats = { "bat", "vampire" };
    private static readonly string[] Crabs = { "crab", "lobster", "crawfish", "crayfish", "shrimp", "prawn", "pincer", "barnacle", "hermit", "ruffian", "rogue" };
    private static readonly string[] Spiders = { "spider", "scorpion", "tarantula", "tick", "mite" };
    private static readonly string[] Larvae = { "worm", "larva", "caterpillar", "centipede", "megapede", "millipede", "curlipede", "grub", "sewing", "scatterdust", "hairy", "bagworm", "maggot" };
    private static readonly string[] Monkeys = { "monkey", "chimp", "ape", "gorilla", "baboon", "mandrill", "primate", "orangutan" };
    private static readonly string[] Frogs = { "frog", "toad", "tadpole", "axolotl", "newt" };
    private static readonly string[] Mustelids = { "seal", "lion", "walrus", "otter", "weasel", "ferret", "mink", "beaver", "platypus", "sea" };
    private static readonly string[] Plants = { "seed", "leaf", "flower", "bloom", "blossom", "bud", "bouquet", "bulb", "weed", "grass", "tree", "root", "vine", "cactus", "berry", "fruit", "apple", "cherry", "acorn", "nut", "olive", "petal", "rose", "lily", "garden", "bonsai", "sprout", "moss", "grove", "flowering", "posy", "bramble" };
    private static readonly string[] Rocks = { "rock", "stone", "boulder", "pebble", "ore", "mineral", "crystal", "jewel", "gem", "coal", "salt", "megaton", "mantle", "compressed", "meteorite", "meteor" };
    private static readonly string[] Blades = { "sword", "blade", "axe", "scythe", "katana", "sickle", "edge", "mantis", "slash" };
    private static readonly string[] Fighters = { "punch", "punching", "kick", "kicking", "boxing", "fist", "muscle", "muscular", "superpower", "karate", "judo", "sumo", "wrestler", "brawler", "martial", "jujitsu", "tantrum", "strong", "gripper", "pummel" };
    private static readonly string[] Spiny = { "spiny", "spike", "spikes", "pin", "quill", "thorn", "needle", "urchin", "hedgehog", "porcupine", "prickly" };
    private static readonly string[] Floaty = { "gas", "balloon", "blimp", "meteor", "meteorite", "cloud", "wisp", "floating", "drifter", "jellyfish", "magnet", "plasma", "cell", "mitosis", "multiplying", "nebula", "protostar", "cotton", "spirit", "chime", "lamp", "luring", "puppet", "screech", "magical", "requiem", "weather", "prism", "parasite", "bronze" };

    // ------------------------------------------------------------------ the genome

    private static PokeGenome Make(PokemonSpecies s)
    {
        var w = new Words(s);
        var (root, steps) = Family(s);
        var fr = new GenomeRandom(GenomeRandom.Hash(root.DexNumber, 0xFA11u));
        var r = new GenomeRandom(GenomeRandom.Hash(s.DexNumber, 0x5EEDu));
        var types = new List<PokemonType> { s.PrimaryType };
        if (s.SecondaryType is { } second && second != s.PrimaryType) types.Add(second);
        bool Is(PokemonType t) => types.Contains(t);
        var eggs = s.EggGroups ?? new List<string>();
        bool Egg(string e) => eggs.Any(x => x.Equals(e, StringComparison.OrdinalIgnoreCase));
        bool undiscovered = Egg("Undiscovered");
        bool evolves = s.Evolutions is { Count: > 0 };

        var g = new PokeGenome
        {
            Species = s.Name, Category = s.Category, Dex = s.DexNumber, Primary = s.PrimaryType, Secondary = types.Count > 1 ? types[1] : null,
            Seed = GenomeRandom.Hash(s.DexNumber, 0xB0D1u),
            Stage = undiscovered && evolves && steps == 0 && s.Height <= 0.6f ? -1 : steps,
            Legendary = undiscovered && !evolves && steps == 0 && s.BaseHP + s.BaseAttack + s.BaseDefense + s.BaseSpAttack + s.BaseSpDefense + s.BaseSpeed >= 570
        };
        g.Robot = s.Name.StartsWith("Iron ", StringComparison.Ordinal) || w.Any("virtual", "gear", "robot", "machine", "cyl", "engine");
        g.Metallic = Is(PokemonType.Steel) || g.Robot;
        g.Rocky = Is(PokemonType.Rock) || (Egg("Mineral") && !g.Metallic && !Is(PokemonType.Ice));
        g.Icy = Is(PokemonType.Ice);

        g.Kind = KindOf(s, w, Is);
        g.Special = SpecialOf(g.Kind, w);
        g.Plan = g.Kind switch
        {
            BodyKind.Quadruped or BodyKind.Arthropod => BodyPlan.Quadruped,
            BodyKind.Upright or BodyKind.Humanoid or BodyKind.Legs => BodyPlan.Biped,
            BodyKind.Bird or BodyKind.Bat or BodyKind.Insect => BodyPlan.Bird,
            BodyKind.Serpent or BodyKind.Crawler => BodyPlan.Serpent,
            BodyKind.Fish => BodyPlan.Fish,
            _ => BodyPlan.Floating
        };
        g.Hovers = g.Plan is BodyPlan.Bird or BodyPlan.Fish || g.Kind == BodyKind.Jelly
            || (g.Plan == BodyPlan.Floating && g.Kind != BodyKind.Cocoon && g.Special is not (Special.Mound or Special.Candle or Special.Tree)
                && (Is(PokemonType.Ghost) || Is(PokemonType.Psychic) || Is(PokemonType.Flying) || s.Abilities.Contains("Levitate") || w.Any(Floaty)));

        // Small species fill little of their sprite frame (and so of their platform in battle), big ones all of it
        g.Fill = Math.Clamp(0.6f + 0.17f * MathF.Log2(Math.Max(0.05f, s.Height) / 0.5f), 0.45f, 1f);

        // Proportions: babies and basic species are chibi, final stages longer in the leg and smaller in the head
        float st = Math.Clamp(g.Stage, -1, 2);
        g.HeadScale = (1.08f - 0.12f * st - (g.Legendary ? 0.12f : 0f)) * fr.Range(0.92f, 1.08f) * r.Range(0.96f, 1.04f);
        g.LegScale = (0.9f + 0.18f * st + (g.Legendary ? 0.15f : 0f)) * fr.Range(0.85f, 1.15f) * r.Range(0.95f, 1.05f);
        g.BodyLength = fr.Range(0.85f, 1.2f) * r.Range(0.95f, 1.05f);
        float heft = s.Weight / Math.Max(0.01f, s.Height * s.Height);
        g.Girth = fr.Range(0.85f, 1.18f) * r.Range(0.95f, 1.05f) * (heft > 60f ? 1.12f : 1f);
        g.EyeScale = (g.Stage <= 0 ? 1.08f : 0.95f) * fr.Range(0.9f, 1.12f);

        bool reptile = w.Any(Reptiles) || ((Egg("Monster") || Egg("Dragon")) && !Egg("Field"));
        g.Stance = w.Any(Hoofed) ? Stance.Long
            : w.Any(Turtles) || (reptile && g.Kind == BodyKind.Quadruped) ? Stance.Low
            : w.Any(Bears) || w.Any(Pigs) || heft > 85f ? Stance.Stocky
            : Stance.Normal;
        if (g.Stance == Stance.Long) g.NeckLength = fr.Range(0.04f, 0.12f);

        Colours(g, s, w, Is, fr, r);
        Parts(g, s, w, Is, Egg, fr, r);
        return g;
    }

    private static BodyKind KindOf(PokemonSpecies s, Words w, Func<PokemonType, bool> Is)
    {
        switch (s.Shape)
        {
            case "Quadruped":
                return w.Any(Crabs) || w.Any(Spiders) ? BodyKind.Arthropod : BodyKind.Quadruped;
            case "Upright":
                return BodyKind.Upright;
            case "Humanoid":
                return BodyKind.Humanoid;
            case "Legs":
                return BodyKind.Legs;
            case "Wings":
                return w.Any(Bats) || (!w.Any(Birds) && (Is(PokemonType.Dark) || Is(PokemonType.Poison)) && !Is(PokemonType.Normal)) ? BodyKind.Bat : BodyKind.Bird;
            case "Bug Wings":
                return w.Any(Bats) ? BodyKind.Bat : BodyKind.Insect;
            case "Squiggle":
                return w.Any("cocoon", "hard") ? BodyKind.Cocoon : BodyKind.Serpent;
            case "Fish":
                return BodyKind.Fish;
            case "Ball":
                return w.Any("cocoon") ? BodyKind.Cocoon : BodyKind.Ball;
            case "Arms":
                return BodyKind.Armed;
            case "Blob":
                return BodyKind.Blob;
            case "Heads":
                return BodyKind.Cluster;
            case "Tentacles":
                return w.Any("jellyfish", "floating", "parasite") || (Is(PokemonType.Ghost) && !Is(PokemonType.Grass)) ? BodyKind.Jelly : BodyKind.Tentacled;
            case "Armor":
                return w.Any(Larvae) ? BodyKind.Crawler : BodyKind.Arthropod;
        }
        return BodyKind.Upright;
    }

    private static Special SpecialOf(BodyKind kind, Words w)
    {
        if (w.Any("star") && kind is BodyKind.Blob or BodyKind.Ball) return Special.Star;
        if (w.Any("mysterious", "brutal") && kind == BodyKind.Blob) return Special.Star;
        if (w.Any("candle", "lamp")) return Special.Candle;
        if (w.Any("mole", "eel") && kind is BodyKind.Blob or BodyKind.Cluster) return Special.Mound;
        if (w.Any("bivalve", "clam")) return Special.Bivalve;
        if (w.Any("pumpkin")) return Special.Pumpkin;
        if (w.Any("snow", "snowstorm") && kind is BodyKind.Blob or BodyKind.Cluster) return Special.Cone;
        if (w.Any("meteorite") && w.Name("luna")) return Special.Crescent;
        if (w.Any("meteorite")) return Special.Sun;
        if (w.Any("bell", "chime", "bronze")) return Special.Bell;
        if (w.Any("balloon", "blimp")) return Special.Balloon;
        if (w.Any("gear")) return Special.Gear;
        if (w.Any("coffin", "virtual", "chest")) return Special.Box;
        if (w.Any("sword") && kind is BodyKind.Blob or BodyKind.Cluster) return Special.Sword;
        if (w.Any("spiral")) return Special.Spiral;
        if (w.Any("cotton", "puff") && kind is BodyKind.Ball or BodyKind.Armed or BodyKind.Blob) return Special.Cotton;
        if (w.Any("coconut", "bonsai", "tree", "stump") && kind is BodyKind.Legs or BodyKind.Armed or BodyKind.Tentacled) return Special.Tree;
        return Special.None;
    }

    // ------------------------------------------------------------------ colours

    /// <summary>The body colours the Pokédex sorts by, as the toon shader likes them: saturated middle tones.</summary>
    private static Color BodyColor(string? name) => (name ?? "").ToLowerInvariant() switch
    {
        "black" => new Color(62, 60, 76, 255),
        "blue" => new Color(88, 150, 226, 255),
        "brown" => new Color(172, 118, 72, 255),
        "gray" => new Color(152, 152, 166, 255),
        "green" => new Color(112, 186, 96, 255),
        "pink" => new Color(242, 156, 188, 255),
        "purple" => new Color(152, 108, 198, 255),
        "red" => new Color(222, 78, 68, 255),
        "white" => new Color(238, 238, 244, 255),
        "yellow" => new Color(248, 212, 78, 255),
        _ => new Color(168, 168, 120, 255)
    };

    /// <summary>A type's accent colour and an alternative for when the first is too close to the body.</summary>
    internal static (Color A, Color B) TypeColors(PokemonType t) => t switch
    {
        PokemonType.Normal => (C(240, 224, 186), C(150, 108, 74)),
        PokemonType.Fire => (C(248, 138, 50), C(252, 214, 84)),
        PokemonType.Water => (C(130, 198, 244), C(244, 232, 196)),
        PokemonType.Electric => (C(252, 216, 64), C(60, 58, 70)),
        PokemonType.Grass => (C(96, 188, 82), C(244, 140, 170)),
        PokemonType.Ice => (C(190, 232, 246), C(244, 248, 252)),
        PokemonType.Fighting => (C(196, 92, 64), C(226, 196, 150)),
        PokemonType.Poison => (C(168, 96, 196), C(214, 90, 170)),
        PokemonType.Ground => (C(214, 174, 112), C(122, 84, 54)),
        PokemonType.Flying => (C(240, 240, 246), C(172, 206, 240)),
        PokemonType.Psychic => (C(244, 126, 176), C(190, 150, 230)),
        PokemonType.Bug => (C(176, 204, 72), C(240, 208, 80)),
        PokemonType.Rock => (C(176, 156, 124), C(140, 136, 128)),
        PokemonType.Ghost => (C(116, 88, 170), C(64, 52, 88)),
        PokemonType.Dragon => (C(92, 104, 204), C(214, 72, 64)),
        PokemonType.Dark => (C(70, 66, 82), C(206, 56, 60)),
        PokemonType.Steel => (C(178, 186, 202), C(232, 196, 84)),
        PokemonType.Fairy => (C(248, 176, 206), C(250, 244, 248)),
        _ => (C(220, 220, 220), C(120, 120, 120))
    };

    private static Color C(int r, int g, int b) => new(r, g, b, 255);

    private static float Distance(Color a, Color b)
    {
        float dr = a.R - b.R, dg = a.G - b.G, db = a.B - b.B;
        return MathF.Sqrt(dr * dr * 0.3f + dg * dg * 0.59f + db * db * 0.11f) * 1.7f;
    }

    internal static float Luma(Color c) => 0.3f * c.R + 0.59f * c.G + 0.11f * c.B;

    /// <summary>Turns a colour's hue by <paramref name="hue"/> degrees and scales its saturation and value.</summary>
    internal static Color Shift(Color c, float hue, float sat, float val)
    {
        float r = c.R / 255f, g = c.G / 255f, b = c.B / 255f;
        float max = MathF.Max(r, MathF.Max(g, b)), min = MathF.Min(r, MathF.Min(g, b));
        float h = 0f, d = max - min;
        if (d > 1e-5f)
        {
            if (max == r) h = 60f * (((g - b) / d) % 6f);
            else if (max == g) h = 60f * ((b - r) / d + 2f);
            else h = 60f * ((r - g) / d + 4f);
        }
        float s = max <= 0f ? 0f : d / max;
        h = (h + hue + 720f) % 360f;
        s = Math.Clamp(s * sat, 0f, 1f);
        float v = Math.Clamp(max * val, 0f, 1f);
        float cc = v * s, x = cc * (1f - MathF.Abs(h / 60f % 2f - 1f)), m = v - cc;
        var (rr, gg, bb) = h switch
        {
            < 60f => (cc, x, 0f),
            < 120f => (x, cc, 0f),
            < 180f => (0f, cc, x),
            < 240f => (0f, x, cc),
            < 300f => (x, 0f, cc),
            _ => (cc, 0f, x)
        };
        return new Color((int)MathF.Round((rr + m) * 255f), (int)MathF.Round((gg + m) * 255f), (int)MathF.Round((bb + m) * 255f), 255);
    }

    private static void Colours(PokeGenome g, PokemonSpecies s, Words w, Func<PokemonType, bool> Is, GenomeRandom fr, GenomeRandom r)
    {
        // A shade of its colour shared by the family, nudged a little for each species
        var main = BodyColor(s.Color);
        bool grey = s.Color is "Gray" or "White" or "Black";
        main = Shift(main, grey ? 0f : fr.Range(-9f, 9f) + r.Range(-3f, 3f), fr.Range(0.86f, 1.1f), fr.Range(0.9f, 1.06f) * r.Range(0.97f, 1.03f));
        g.Main = main;

        // The accent: its second type's colour, else its first type's, else whatever stands apart from the body
        var candidates = new List<Color>();
        foreach (var t in new[] { g.Secondary, (PokemonType?)g.Primary })
        {
            if (t is not { } type) continue;
            var (a, b) = TypeColors(type);
            candidates.Add(a);
            candidates.Add(b);
        }
        candidates.Add(new Color(244, 230, 196, 255));
        candidates.Add(PixelCanvas.Shadow(main, 0.45f));
        var cream = new Color(246, 236, 210, 255);
        var second = candidates.FirstOrDefault(c => Distance(c, main) > 75f, cream);
        g.Second = Shift(second, r.Range(-4f, 4f), 1f, r.Range(0.97f, 1.03f));

        g.Belly = Luma(main) < 95f ? new Color(232, 218, 186, 255) : PixelCanvas.Mix(main, cream, s.Color == "White" ? 0.3f : 0.62f);
        g.Dark = Luma(main) < 80f ? new Color(36, 34, 46, 255) : PixelCanvas.Shadow(main, 0.48f);

        // Eyes: plain dark ones, or with an iris in a colour that suits the type
        g.IrisEye = fr.Chance(0.5f) || Is(PokemonType.Dark) || g.Legendary;
        var irises = g.Primary switch
        {
            PokemonType.Fire or PokemonType.Fighting or PokemonType.Dark => new[] { C(214, 64, 58), C(248, 182, 60) },
            PokemonType.Water or PokemonType.Ice or PokemonType.Flying => new[] { C(70, 132, 222), C(90, 190, 220) },
            PokemonType.Grass or PokemonType.Bug => new[] { C(96, 176, 84), C(150, 104, 62) },
            PokemonType.Electric or PokemonType.Ground or PokemonType.Rock => new[] { C(248, 200, 60), C(150, 104, 62) },
            PokemonType.Psychic or PokemonType.Fairy or PokemonType.Ghost or PokemonType.Poison => new[] { C(176, 92, 210), C(220, 70, 110) },
            PokemonType.Dragon or PokemonType.Steel => new[] { C(248, 200, 60), C(214, 64, 58), C(70, 132, 222) },
            _ => new[] { C(150, 104, 62), C(90, 70, 60) }
        };
        g.Eye = irises[fr.Int(irises.Length)];
        float sclera = g.Kind switch
        {
            BodyKind.Fish => 0.65f,
            BodyKind.Ball or BodyKind.Blob or BodyKind.Armed or BodyKind.Cluster or BodyKind.Jelly => 0.4f,
            BodyKind.Bird => 0.3f,
            BodyKind.Insect or BodyKind.Arthropod or BodyKind.Crawler => 0.12f,
            _ => 0.22f
        };
        if (Is(PokemonType.Fairy) || Is(PokemonType.Water)) sclera += 0.15f;
        if (g.Stage < 0) sclera += 0.2f;
        g.ScleraEye = !g.IrisEye && fr.Chance(sclera);

        // What the surface is made of
        g.Coat = g.Metallic ? SurfaceMaterial.Metal
            : g.Kind is BodyKind.Fish or BodyKind.Serpent or BodyKind.Jelly ? SurfaceMaterial.Scales
            : g.Kind is BodyKind.Insect or BodyKind.Arthropod or BodyKind.Crawler or BodyKind.Cocoon ? SurfaceMaterial.Shell
            : g.Rocky || g.Icy ? SurfaceMaterial.Scales
            : Is(PokemonType.Water) || Is(PokemonType.Dragon) || Is(PokemonType.Poison) || Is(PokemonType.Ghost) || w.Any(Reptiles) || w.Any(Frogs) ? SurfaceMaterial.Scales
            : SurfaceMaterial.Fur;
    }

    // ------------------------------------------------------------------ parts

    private static void Parts(PokeGenome g, PokemonSpecies s, Words w, Func<PokemonType, bool> Is, Func<string, bool> Egg, GenomeRandom fr, GenomeRandom r)
    {
        int stage = Math.Max(0, g.Stage);
        bool furry = g.Kind is BodyKind.Quadruped or BodyKind.Upright or BodyKind.Humanoid or BodyKind.Legs;
        bool mammal = furry && (Egg("Field") || w.Any(Felines) || w.Any(Canines) || w.Any(Rodents) || w.Any(Bears) || w.Any(Rabbits) || w.Any(Monkeys))
            && !w.Any(Reptiles) && !w.Any(Birds);
        bool reptile = furry && (w.Any(Reptiles) || w.Any("dragon", "monster") || Egg("Monster") || Egg("Dragon")) && !mammal;
        bool birdlike = w.Any(Birds) && g.Kind is not (BodyKind.Bird);

        // Ears
        g.Ears = EarKind.None;
        if (furry || g.Kind is BodyKind.Bat or BodyKind.Ball or BodyKind.Cluster)
        {
            if (w.Any(Rabbits)) g.Ears = EarKind.Long;
            else if (w.Any(Felines) || w.Any(Canines) || w.Any(Bats) || g.Kind == BodyKind.Bat) g.Ears = EarKind.Pointed;
            else if (w.Any(Rodents) || w.Any(Bears) || w.Any(Pigs) || w.Any("koala", "chinchilla")) g.Ears = EarKind.Round;
            else if (w.Any("puppy", "dog", "elephant", "nose", "basset")) g.Ears = EarKind.Floppy;
            else if (mammal) g.Ears = fr.Pick(EarKind.Round, EarKind.Pointed, EarKind.Pointed, EarKind.Tuft);
            else if (reptile && Is(PokemonType.Water)) g.Ears = fr.Chance(0.4f) ? EarKind.Fin : EarKind.None;
            else if (g.Kind is BodyKind.Ball or BodyKind.Cluster && (Egg("Field") || Egg("Fairy")) && fr.Chance(0.6f)) g.Ears = fr.Pick(EarKind.Round, EarKind.Pointed);
            else if (furry && !reptile && !birdlike && fr.Chance(0.35f)) g.Ears = fr.Pick(EarKind.Round, EarKind.Pointed, EarKind.Tuft);
        }
        if (g.Kind == BodyKind.Serpent && (Is(PokemonType.Dragon) || Is(PokemonType.Water)) && fr.Chance(0.5f)) g.Ears = EarKind.Fin;

        // Mouth and snout
        g.Mouth = MouthKind.None;
        if (g.Kind == BodyKind.Bird || birdlike || w.Any("beak", "chick", "duck", "penguin", "owl")) g.Mouth = w.Any("duck", "platypus", "bill") ? MouthKind.Bill : MouthKind.Beak;
        else if (w.Any("elephant", "nose", "trunk", "tapir")) g.Mouth = MouthKind.Trunk;
        else if (w.Any(Pigs) || w.Any("mole")) g.Mouth = MouthKind.Snout;
        else if (reptile || w.Any("shark", "jaw", "bite", "fang", "croc", "chomp", "maw")) g.Mouth = MouthKind.Jaw;
        else if (mammal || (furry && fr.Chance(0.3f))) g.Mouth = MouthKind.Muzzle;
        else if (g.Kind is BodyKind.Arthropod or BodyKind.Crawler && fr.Chance(0.45f)) g.Mouth = MouthKind.Mandibles;
        if (g.Kind == BodyKind.Serpent) g.Mouth = MouthKind.Jaw;

        // Tail
        g.Tail = TailKind.None;
        g.TailTip = TipKind.None;
        if (g.Kind is BodyKind.Quadruped or BodyKind.Upright or BodyKind.Bat || (g.Kind == BodyKind.Legs && fr.Chance(0.4f)))
        {
            if (w.Any("fox")) g.Tail = TailKind.Bushy;
            else if (w.Any("squirrel")) g.Tail = TailKind.Bushy;
            else if (w.Any(Rodents)) g.Tail = Is(PokemonType.Electric) ? TailKind.Bolt : TailKind.Thin;
            else if (w.Any(Felines)) g.Tail = TailKind.Thin;
            else if (w.Any("beaver", "platypus")) g.Tail = TailKind.Paddle;
            else if (w.Any(Pigs)) g.Tail = TailKind.Curl;
            else if (w.Any(Bears) || w.Any(Turtles) || w.Any(Rabbits)) g.Tail = TailKind.Stub;
            else if (reptile) g.Tail = TailKind.Thick;
            else if (w.Any(Canines) || w.Any(Hoofed)) g.Tail = w.Any(Hoofed) ? TailKind.Thin : TailKind.Bushy;
            else if (Is(PokemonType.Water) && !mammal && fr.Chance(0.5f)) g.Tail = TailKind.Fin;
            else if (g.Kind == BodyKind.Bat) g.Tail = fr.Chance(0.4f) ? TailKind.Thin : TailKind.None;
            else g.Tail = fr.Pick(TailKind.Thin, TailKind.Thick, TailKind.Bushy, TailKind.Stub);
            if (Is(PokemonType.Electric) && g.Tail is TailKind.Thin && fr.Chance(0.6f)) g.Tail = TailKind.Bolt;
        }
        if (g.Kind is BodyKind.Armed or BodyKind.Ball or BodyKind.Blob && g.Hovers && Is(PokemonType.Ghost)) g.Tail = TailKind.Wisp;
        if (g.Tail is not (TailKind.None or TailKind.Stub or TailKind.Curl or TailKind.Wisp))
        {
            if (Is(PokemonType.Fire) && g.Tail is TailKind.Thin or TailKind.Thick && fr.Chance(0.75f)) g.TailTip = TipKind.Flame;
            else if (Is(PokemonType.Grass) && fr.Chance(0.5f)) g.TailTip = TipKind.Leaf;
            else if (Is(PokemonType.Electric) && g.Tail == TailKind.Thin) g.TailTip = TipKind.Star;
            else if (Is(PokemonType.Poison) || w.Any("scorpion", "sting")) g.TailTip = TipKind.Spike;
            else if (g.Tail == TailKind.Thin && fr.Chance(0.5f)) g.TailTip = TipKind.Tuft;
            else if (g.Tail == TailKind.Thin && fr.Chance(0.25f)) g.TailTip = TipKind.Ball;
        }
        if (w.Any("fox") && stage >= 1) g.Tails = stage >= 1 && fr.Chance(0.7f) ? 3 : 1;
        if (w.Any("nine")) g.Tails = 5;

        // Hands
        g.Hands = HandKind.Paw;
        if (w.Any(Blades)) g.Hands = HandKind.Blade;
        else if (w.Any(Crabs) || w.Any("scorpion", "claw")) g.Hands = HandKind.Pincer;
        else if (w.Any(Fighters) || (Is(PokemonType.Fighting) && g.Kind == BodyKind.Humanoid)) g.Hands = HandKind.Fist;
        else if (Is(PokemonType.Grass) && g.Kind is BodyKind.Humanoid or BodyKind.Armed or BodyKind.Blob && fr.Chance(0.5f)) g.Hands = HandKind.Leaf;
        else if (Is(PokemonType.Water) && w.Any(Mustelids)) g.Hands = HandKind.Flipper;
        else if (Is(PokemonType.Ground) || Is(PokemonType.Dragon) || Is(PokemonType.Dark) || w.Any("scratch", "mole", "claw", "slash")) g.Hands = HandKind.Claw;
        g.Claws = g.Hands == HandKind.Claw || (reptile && fr.Chance(0.5f));

        // On top of the head
        g.Top = TopKind.None;
        if (w.Any("unicorn", "drill", "rhino", "horn", "single") && g.Kind is BodyKind.Quadruped or BodyKind.Upright or BodyKind.Humanoid) g.Top = TopKind.Horn;
        else if (w.Any("deer", "reindeer", "moose", "elk", "antler")) g.Top = TopKind.Antlers;
        else if (w.Any("bull", "goat", "ram", "ox", "bison", "buffalo", "cattle", "devil", "demon", "ogre")) g.Top = TopKind.Horns;
        else if (w.Any("mushroom", "fungus")) g.Top = TopKind.Cap;
        else if (w.Any("flower", "bloom", "blossom", "bouquet", "petal", "posy", "flowering", "garden")) g.Top = TopKind.Flower;
        else if (w.Any("king", "queen", "emperor", "royal", "regal", "crown", "monarch")) g.Top = TopKind.Crown;
        else if (Is(PokemonType.Grass) && stage == 0 && g.Kind is not (BodyKind.Insect or BodyKind.Serpent or BodyKind.Fish) && fr.Chance(0.75f)) g.Top = TopKind.Sprout;
        else if (Is(PokemonType.Grass) && g.Kind is BodyKind.Humanoid or BodyKind.Legs or BodyKind.Blob or BodyKind.Ball && fr.Chance(0.5f)) g.Top = fr.Chance(0.5f) ? TopKind.Flower : TopKind.Sprout;
        else if (Is(PokemonType.Fire) && g.Kind is BodyKind.Humanoid or BodyKind.Ball or BodyKind.Armed or BodyKind.Blob or BodyKind.Legs && fr.Chance(0.6f)) g.Top = TopKind.Flame;
        else if (g.Kind is BodyKind.Insect or BodyKind.Crawler || (Is(PokemonType.Bug) && g.Kind is not (BodyKind.Arthropod or BodyKind.Cocoon) && fr.Chance(0.6f))) g.Top = TopKind.Antennae;
        else if (Is(PokemonType.Psychic) && fr.Chance(0.45f) || (Is(PokemonType.Fairy) && fr.Chance(0.25f))) g.Top = TopKind.Gem;
        else if ((Is(PokemonType.Dragon) || Is(PokemonType.Dark) || Is(PokemonType.Rock) || Is(PokemonType.Steel) || Is(PokemonType.Ground)) && g.Kind is not (BodyKind.Insect or BodyKind.Fish)
                 && fr.Chance(0.55f + 0.12f * stage)) g.Top = fr.Chance(0.6f) ? TopKind.Horns : TopKind.Horn;
        else if (g.Kind == BodyKind.Bird && fr.Chance(0.55f + 0.15f * stage)) g.Top = TopKind.Crest;
        else if ((Is(PokemonType.Water) || Is(PokemonType.Ice)) && g.Kind is BodyKind.Upright or BodyKind.Humanoid or BodyKind.Quadruped && fr.Chance(0.35f)) g.Top = TopKind.Crest;
        else if (furry && fr.Chance(0.3f)) g.Top = TopKind.Tuft;
        if (g.Legendary && g.Top == TopKind.None) g.Top = fr.Pick(TopKind.Crest, TopKind.Horns, TopKind.Gem, TopKind.Crown);

        // On the back
        g.Back = BackKind.None;
        if (g.Kind == BodyKind.Quadruped || g.Kind == BodyKind.Upright || g.Kind == BodyKind.Arthropod || g.Kind == BodyKind.Humanoid)
        {
            if (w.Any(Turtles) || w.Any("armor", "shield", "hermit")) g.Back = BackKind.Shell;
            else if (w.Any("mushroom")) g.Back = BackKind.Mushroom;
            else if (Is(PokemonType.Grass) && g.Kind == BodyKind.Quadruped) g.Back = stage >= 2 ? BackKind.Flower : (fr.Chance(0.7f) ? BackKind.Bulb : BackKind.Leaves);
            else if (w.Any("lion", "mane") || (Is(PokemonType.Electric) && stage >= 1 && g.Kind == BodyKind.Quadruped && fr.Chance(0.6f))) g.Back = BackKind.Mane;
            else if (Is(PokemonType.Fire) && g.Kind == BodyKind.Quadruped && (stage >= 1 || fr.Chance(0.3f))) g.Back = BackKind.Flames;
            else if (w.Any(Spiny)) g.Back = BackKind.Spikes;
            else if (g.Icy && fr.Chance(0.5f)) g.Back = BackKind.Crystals;
            else if (g.Rocky || Is(PokemonType.Steel) || (Is(PokemonType.Dragon) && fr.Chance(0.5f))) g.Back = fr.Chance(0.5f) ? BackKind.Plates : BackKind.Spikes;
            else if ((Is(PokemonType.Water) && !mammal && fr.Chance(0.4f)) || w.Any("shark")) g.Back = BackKind.Fin;
            else if ((Is(PokemonType.Poison) || Is(PokemonType.Dark) || Is(PokemonType.Ground)) && fr.Chance(0.35f)) g.Back = BackKind.Spikes;
        }
        g.Spikes = g.Back is BackKind.Spikes or BackKind.Plates or BackKind.Crystals ? (w.Any(Spiny) ? 6 + 2 * stage : 3 + stage + fr.Int(2)) : 0;
        if (g.Kind == BodyKind.Ball && (w.Any(Spiny) || g.Icy || w.Any("sun"))) g.Spikes = 6 + 2 * stage;

        // Wings: birds, bats and insects always; anything else that flies grows a pair on its back
        g.Wings = g.Kind switch
        {
            BodyKind.Bird => Is(PokemonType.Dragon) ? WingKind.Dragon : WingKind.Feather,
            BodyKind.Bat => WingKind.Bat,
            BodyKind.Insect => w.Any("butterfly", "moth", "scale", "silk") ? WingKind.Butterfly : WingKind.Insect,
            BodyKind.Upright or BodyKind.Humanoid or BodyKind.Quadruped when Is(PokemonType.Flying) =>
                Is(PokemonType.Dragon) || Is(PokemonType.Fire) || Is(PokemonType.Dark) ? WingKind.Dragon : Is(PokemonType.Bug) ? WingKind.Insect : WingKind.Feather,
            BodyKind.Humanoid when Is(PokemonType.Fairy) && fr.Chance(0.25f) => WingKind.Insect,
            _ => WingKind.None
        };

        // Patterns and small marks
        g.Pattern = PatternKind.Belly;
        if (Is(PokemonType.Poison) && fr.Chance(0.55f)) g.Pattern = PatternKind.Spots;
        else if (w.Any("tiger", "zebra", "bee", "hornet", "wasp", "striped", "stripe") || (g.Kind is BodyKind.Insect or BodyKind.Crawler && fr.Chance(0.5f))) g.Pattern = PatternKind.Stripes;
        else if (g.Kind == BodyKind.Ball && (Is(PokemonType.Electric) || w.Any("ball")) && fr.Chance(0.7f)) g.Pattern = PatternKind.TwoTone;
        else if ((Is(PokemonType.Dark) || w.Any("bandit", "ninja", "mask", "thief", "raccoon")) && fr.Chance(0.6f)) g.Pattern = PatternKind.Mask;
        else if (Is(PokemonType.Psychic) && fr.Chance(0.35f)) g.Pattern = PatternKind.ChestMark;
        else if (furry && fr.Chance(0.25f)) g.Pattern = PatternKind.Socks;
        else if (fr.Chance(0.15f)) g.Pattern = PatternKind.Spots;
        g.Cheeks = Is(PokemonType.Electric) && (w.Any(Rodents) || g.Kind is BodyKind.Quadruped or BodyKind.Upright or BodyKind.Humanoid) && fr.Chance(0.7f);
        g.Fangs = (Is(PokemonType.Dark) || g.Kind == BodyKind.Bat || w.Any("fang", "vampire", "bite")) && g.Mouth is MouthKind.None or MouthKind.Muzzle or MouthKind.Jaw;
        g.Whiskers = w.Any("catfish", "whisker", "carp", "barbel") || (g.Kind == BodyKind.Fish && fr.Chance(0.15f));
        g.OneEye = w.Any("magnet", "eyeball", "requiem", "gripper") && g.Kind is BodyKind.Armed or BodyKind.Ball or BodyKind.Cluster or BodyKind.Insect;

        // Smaller touches that tell species of the same kind apart
        g.HeadShape = reptile || g.Mouth == MouthKind.Jaw ? HeadShape.Long
            : g.Kind is BodyKind.Bird or BodyKind.Fish or BodyKind.Insect ? HeadShape.Round
            : fr.Pick(HeadShape.Round, HeadShape.Round, HeadShape.Wide, HeadShape.Tall, HeadShape.Flat);
        g.Tips = furry && (fr.Chance(0.4f) || (w.Any(Rodents) && Is(PokemonType.Electric)));
        g.Ruff = furry && g.Back != BackKind.Mane && (w.Any(Canines) || w.Any("sheep", "wool", "fleece", "fluffy", "evolution", "fox") ? fr.Chance(0.75f) : mammal && fr.Chance(0.15f));
        g.Gloves = g.Kind is BodyKind.Humanoid or BodyKind.Upright && (fr.Chance(0.35f) || (Is(PokemonType.Fighting) && fr.Chance(0.5f)));
        g.Saddle = g.Kind == BodyKind.Quadruped && g.Back is BackKind.None or BackKind.Spikes && fr.Chance(0.25f);
        if (g.Kind == BodyKind.Humanoid && g.Ears == EarKind.None)
        {
            if (Is(PokemonType.Psychic) && fr.Chance(0.5f)) g.Ears = fr.Pick(EarKind.Pointed, EarKind.Long);
            else if (Is(PokemonType.Fairy) && fr.Chance(0.5f)) g.Ears = EarKind.Round;
        }
        if (g.Kind == BodyKind.Humanoid && g.Top == TopKind.None)
            g.Top = Is(PokemonType.Fighting) ? fr.Pick(TopKind.Tuft, TopKind.Crest, TopKind.None)
                : Is(PokemonType.Ice) ? TopKind.Crest
                : fr.Chance(0.35f) ? fr.Pick(TopKind.Tuft, TopKind.Crest, TopKind.Horn) : TopKind.None;

        // Counts
        g.Heads = g.Kind == BodyKind.Cluster ? (w.Any("triple", "trio", "family", "formation", "collective") || w.Name("trio", "tri") || stage >= 2 ? 3 : 2) : 1;
        if (g.Kind == BodyKind.Legs && w.Any("twin")) g.Heads = 2;
        if (g.Kind == BodyKind.Legs && w.Any("triple", "coconut")) g.Heads = 3;
        g.LegPairs = g.Kind switch
        {
            BodyKind.Arthropod => w.Any(Spiders) || w.Any("ant", "beetle", "mold", "skater") ? 3 : 2,
            BodyKind.Insect => 2,
            _ => 2
        };
        g.Tentacles = g.Kind switch
        {
            BodyKind.Jelly => 4 + 2 * fr.Int(2),
            BodyKind.Tentacled => w.Any("octopus", "jujitsu", "tantrum", "jet") ? 6 : 4,
            _ => 0
        };
        g.Segments = g.Kind switch
        {
            BodyKind.Serpent => w.Any("slug", "lava", "tissue", "cucumber", "worm", "heap", "castle", "disguise", "apple", "lingering") ? 2 : 4 + Math.Min(2, stage),
            BodyKind.Crawler => 3 + Math.Min(2, stage) + (w.Any("centipede", "megapede") ? 1 : 0),
            _ => 0
        };
    }
}
