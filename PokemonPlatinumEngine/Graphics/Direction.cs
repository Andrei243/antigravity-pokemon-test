using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>Which of the four signature shots a move takes (plan 25 · Y17; style guide, "Camera (G8)").</summary>
internal enum SignatureShot { None, ChargePush, FollowProjectile, ImpactPunch, Orbit }

/// <summary>
/// How a signature move is staged (plan 25 · Y17; style guide, "Move effects (G8)", "Signature moves"): a short
/// timeline laid over the move's own look. Every part is a pure function of the cue's (shown) age and seed, so a
/// direction holds no state and the same cue always plays the same way. GPU-free.
/// </summary>
/// <param name="Shot">The camera's shot through the move (<see cref="BattleCamera"/>).</param>
/// <param name="HitStop">How long the picture holds at the impact, seconds (scaled by <see cref="BattleCamera.Motion"/>).</param>
/// <param name="User">What the move does to its user's body at a shown age, beyond Y16's table (null: nothing).</param>
/// <param name="Target">What it does to its target's body at a shown age when it lands (null: a heavy hit's throw, or nothing).</param>
/// <param name="Light">A stage light laid over the move's own at its full (Y16's envelope eases it in and out).</param>
/// <param name="Effect">An effect laid over the move's own recipe (null: nothing).</param>
internal sealed record MoveDirection(
    SignatureShot Shot,
    float HitStop = 0f,
    Func<float, int, BodyMotion>? User = null,
    Func<float, int, BodyMotion>? Target = null,
    StageLight? Light = null,
    FxRecipe? Effect = null)
{
    /// <summary>Whether its target is thrown back at the impact (every move that stops the picture is heavy).</summary>
    public bool Heavy => HitStop > 0f;
}

/// <summary>The signature moves' directions, and the shared parts of them: the hit-stop's remap and a heavy hit's throw.</summary>
internal static class Directions
{
    private const float I = BattleAnimator.ImpactTime;

    /// <summary>A heavy hit's hold at the impact, seconds (shared with plan 25 · YD's Max Moves).</summary>
    public const float HeavyStop = 0.08f;

    /// <summary>When a signature move's camera lets go (an ordinary move's is <see cref="BattleCamera.MoveShotEnd"/>).</summary>
    public const float ShotEnd = 1.15f;

    /// <summary>How far a heavy hit throws its target back, in its height, and how flat it presses it.</summary>
    public const float ThrowReach = 0.12f, ThrowSquash = 0.08f;

    private static Color Rgb(int r, int g, int b, int a = 255) => new(r, g, b, a);

    private static float Smooth(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    /// <summary>The table, by move name. Y17a seeds it with six; Y17b on fill it.</summary>
    public static readonly IReadOnlyDictionary<string, MoveDirection> Table = new Dictionary<string, MoveDirection>
    {
        // A beam wound up and let go: the camera pushes in on the gathering, cuts to the blow; the user rocked back by it
        ["Hyper Beam"] = new(SignatureShot.ChargePush, HeavyStop, User: Recoil),
        // The same on its strike, and on its charge turn the push runs through the gathering of sunlight
        ["Solar Beam"] = new(SignatureShot.ChargePush, HeavyStop, User: Recoil),
        // A wave of time sent across: the camera rides it; the stage turns to the colour of old steel; rings of time on the target
        ["Roar of Time"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: new StageLight(new Vector3(0.16f, 0.3f, 0.42f), 0.35f), Effect: TimeRings),
        // A flurry of blows: a punch-in held on the hit
        ["Close Combat"] = new(SignatureShot.ImpactPunch, HeavyStop),
        // The dive from the sky: a punch-in on the landing
        ["Fly"] = new(SignatureShot.ImpactPunch, HeavyStop),
        // A cyclone round the target: the camera swings round it; the stage goes the grey-blue of a storm
        ["Hurricane"] = new(SignatureShot.Orbit, Light: new StageLight(new Vector3(0.3f, 0.36f, 0.46f), 0.4f, 0.25f)),

        // ---------------------------------------------------------------- Y17b: the legendaries' and mythicals' own
        // Sinnoh
        // Space torn open: a punch-in on the slash; the stage goes pearl and violet; a dark seam left hanging over the target
        ["Spacial Rend"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.42f, 0.22f, 0.5f, 0.4f), Effect: Rift),
        // Judgment from above: the camera pushes in on the gathering; a white-gold wash; the target pressed down under a halo
        ["Judgment"] = new(SignatureShot.ChargePush, HeavyStop, Target: Pressed, Light: Lit(1f, 0.92f, 0.62f, 0.18f), Effect: Halo),
        // Out of the shadow world: a punch-in on the blow; the stage nearly black
        ["Shadow Force"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.07f, 0.03f, 0.12f, 0.5f), Effect: Wisps),
        // A storm of lava round the target: the camera swings round it; the stage a molten red; embers left on the platform
        ["Magma Storm"] = new(SignatureShot.Orbit, Light: Lit(0.58f, 0.16f, 0.05f, 0.42f), Effect: Embers),
        // The giant's grip: a punch-in held on the squeeze (Y16 squeezes the body); the stage stone-grey
        ["Crush Grip"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: Held, Light: Lit(0.32f, 0.27f, 0.22f, 0.3f, 0.25f)),
        // A flare of seeds and light: the camera rides it; a pale spring green
        ["Seed Flare"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.72f, 0.95f, 0.62f, 0.15f), Effect: Leaves),
        // The dark void over the foes: the camera swings round (the move's own stage already goes black)
        ["Dark Void"] = new(SignatureShot.Orbit),
        // A dance under the moon on the user alone: its shot stays; the stage turns a night's blue
        ["Lunar Dance"] = new(SignatureShot.None, Light: Lit(0.26f, 0.32f, 0.62f, 0.42f)),
        // Kanto
        // The strike of a mind: the camera rides the lance; the stage a deep psychic violet; the target thrown
        ["Psystrike"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.5f, 0.18f, 0.55f, 0.42f)),
        // Johto
        // A blast of air from the deep: the camera rides it; sky-blue
        ["Aeroblast"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.55f, 0.76f, 0.96f, 0.32f)),
        // The sacred flame wound up and loosed: the push; a gold-red light; embers left after
        ["Sacred Fire"] = new(SignatureShot.ChargePush, HeavyStop, Light: Lit(0.9f, 0.52f, 0.18f, 0.24f), Effect: Embers),
        // Hoenn
        // The sea's first light: the camera rides the beams; the stage the deep blue of the ocean floor; spray left
        ["Origin Pulse"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.05f, 0.14f, 0.44f, 0.48f), Effect: Spray),
        // Blades of red-hot rock: a punch-in on the eruption; magma's red; the target tossed up, the ground cracked
        ["Precipice Blades"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: TossedUp, Light: Lit(0.52f, 0.12f, 0.04f, 0.46f), Effect: Cracks),
        // The dive from the sky: the user rises and hurtles in; a punch-in; green-gold
        ["Dragon Ascent"] = new(SignatureShot.ImpactPunch, HeavyStop, User: Dive, Light: Lit(0.36f, 0.62f, 0.3f, 0.3f)),
        // The full power of the mind: the push; violet; the user rocked back by its own blast
        ["Psycho Boost"] = new(SignatureShot.ChargePush, HeavyStop, User: Recoil, Light: Lit(0.46f, 0.2f, 0.62f, 0.42f)),
        // The Eon pair's balls of mist and light: the camera rides each, softly washed
        ["Mist Ball"] = new(SignatureShot.FollowProjectile, Light: Lit(0.86f, 0.6f, 0.76f, 0.16f)),
        ["Luster Purge"] = new(SignatureShot.FollowProjectile, Light: Lit(0.56f, 0.66f, 0.92f, 0.16f)),

        // ---------------------------------------------------------------- Y17b: the starters' finals
        // The three ultimate moves: wound up and let go, the user rocked back; what each leaves on the platform
        ["Blast Burn"] = new(SignatureShot.ChargePush, HeavyStop, User: Recoil, Light: Lit(0.6f, 0.2f, 0.05f, 0.42f), Effect: Embers),
        ["Hydro Cannon"] = new(SignatureShot.ChargePush, HeavyStop, User: Recoil, Light: Lit(0.1f, 0.3f, 0.6f, 0.36f), Effect: Spray),
        ["Frenzy Plant"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: TossedUp, Light: Lit(0.16f, 0.4f, 0.12f, 0.36f), Effect: Leaves),
        // Meteors one after another: the camera swings round the target; a night sky's indigo; craters cracked
        ["Draco Meteor"] = new(SignatureShot.Orbit, HeavyStop, Light: Lit(0.12f, 0.08f, 0.3f, 0.46f), Effect: Cracks),
        // Typhlosion's eruption and the fire starters' overheat: the push on the swelling; the air shimmering with heat
        ["Eruption"] = new(SignatureShot.ChargePush, HeavyStop, User: Heave, Light: Lit(0.62f, 0.26f, 0.08f, 0.36f), Effect: Shimmer),
        ["Overheat"] = new(SignatureShot.ChargePush, HeavyStop, User: Heave, Light: Lit(0.86f, 0.46f, 0.16f, 0.3f), Effect: Shimmer),
        // Blaziken's kick, the fire finals' charge, Torterra's hammer, Swampert's and Torterra's quake: punch-ins on the blow
        ["Blaze Kick"] = new(SignatureShot.ImpactPunch, HeavyStop, Effect: Embers),
        ["Flare Blitz"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.6f, 0.24f, 0.08f, 0.26f), Effect: Embers),
        ["Wood Hammer"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: Pressed, Effect: Leaves),
        ["Earthquake"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: Jolted, Light: Lit(0.36f, 0.28f, 0.18f, 0.24f, 0.2f), Effect: RingCracks),
        // Blastoise's and Empoleon's jet: the camera rides it
        ["Hydro Pump"] = new(SignatureShot.FollowProjectile, HeavyStop, Effect: Spray),
        // Sceptile's and Torterra's storm of leaves: the camera swings round it
        ["Leaf Storm"] = new(SignatureShot.Orbit, Light: Lit(0.2f, 0.44f, 0.16f, 0.3f), Effect: Leaves),

        // ---------------------------------------------------------------- Y17c: the pseudo-legendaries' moves
        // Dragonite's rage round its foe: the camera circles with it; no wash of its own (Y17d: the move's own red fade
        // already turns the whole stage brick red, and the signature's 0.16 only deepened it)
        ["Outrage"] = new(SignatureShot.Orbit, HeavyStop),
        // Tyranitar's blades of stone from under: a punch-in; the target tossed up, shards falling round it
        ["Stone Edge"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: TossedUp, Light: Lit(0.38f, 0.3f, 0.2f, 0.26f), Effect: Shards),
        // Its bite: a punch-in held on the jaws closing; the target only shudders in them
        ["Crunch"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: Held, Light: Lit(0.14f, 0.08f, 0.16f, 0.3f)),
        // Salamence's charge: the user lunges in to meet its target; a punch-in
        ["Dragon Rush"] = new(SignatureShot.ImpactPunch, HeavyStop, User: Lunge, Light: Lit(0.24f, 0.18f, 0.5f, 0.32f)),
        // Metagross's meteor of a fist: a punch-in; stars springing off the body
        ["Meteor Mash"] = new(SignatureShot.ImpactPunch, HeavyStop, Effect: Stars),
        // Garchomp's claws (its Earthquake is Y17b's): a punch-in on the slash; a dragon's teal
        ["Dragon Claw"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.12f, 0.34f, 0.4f, 0.26f)),
        // Hydreigon's pulse of darkness: the camera rides it; the dark vignette; wisps left rising
        ["Dark Pulse"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.06f, 0.03f, 0.08f, 0.4f), Effect: Wisps),
        // Goodra's pulse: the camera rides it; a soft violet
        ["Dragon Pulse"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.34f, 0.22f, 0.56f, 0.3f)),
        // Kommo-o's scales clashed: the camera swings round; gold rings of sound round the target
        ["Clanging Scales"] = new(SignatureShot.Orbit, HeavyStop, Light: Lit(0.56f, 0.42f, 0.16f, 0.28f), Effect: SoundRings),
        // Its soul's dance on its user alone: the shot stays; the rings stand round the user
        ["Clangorous Soul"] = new(SignatureShot.None, Light: Lit(0.6f, 0.46f, 0.18f, 0.3f), Effect: SoundRings),
        // Dragapult's darts: the camera rides each; a ghostly teal; light hits, no stop
        ["Dragon Darts"] = new(SignatureShot.FollowProjectile, Light: Lit(0.18f, 0.4f, 0.46f, 0.26f)),
        // Baxcalibur's charge with its glaive: the user lunges; a punch-in; frost left on the platform
        ["Glaive Rush"] = new(SignatureShot.ImpactPunch, HeavyStop, User: Lunge, Light: Lit(0.3f, 0.44f, 0.6f, 0.3f), Effect: Frost),

        // ---------------------------------------------------------------- Y17c: the starters' finals of Unova to Paldea
        // Emboar: its weight falling on the target, embers after; its head of stone, shards after
        ["Heat Crash"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: Pressed, Light: Lit(0.6f, 0.22f, 0.06f, 0.32f), Effect: Embers),
        ["Head Smash"] = new(SignatureShot.ImpactPunch, HeavyStop, Effect: Shards),
        // Samurott's shell blade, and the Hisuian one's edge leaving splinters
        ["Razor Shell"] = new(SignatureShot.ImpactPunch, HeavyStop, Effect: Spray),
        ["Ceaseless Edge"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.16f, 0.1f, 0.2f, 0.3f), Effect: Shards),
        // Serperior's blade (its Leaf Storm is Y17b's)
        ["Leaf Blade"] = new(SignatureShot.ImpactPunch, HeavyStop, Effect: Leaves),
        // Chesnaught's arm brought down: the target pressed
        ["Hammer Arm"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: Pressed),
        // Delphox's mystic fire: the camera rides it; violet and red; embers
        ["Mystical Fire"] = new(SignatureShot.FollowProjectile, Light: Lit(0.5f, 0.18f, 0.36f, 0.3f), Effect: Embers),
        // Greninja's stars of water: the camera rides them; spray
        ["Water Shuriken"] = new(SignatureShot.FollowProjectile, Effect: Spray),
        // Decidueye's arrow pinning a shadow, and the Hisuian one's three arrows
        ["Spirit Shackle"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.18f, 0.12f, 0.3f, 0.36f), Effect: Wisps),
        ["Triple Arrows"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.42f, 0.3f, 0.18f, 0.24f)),
        // Incineroar's lariat: a punch-in; a dark red
        ["Darkest Lariat"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.3f, 0.06f, 0.08f, 0.36f)),
        // Primarina's aria round the foes: the camera swings round; a soft sea light; spray
        ["Sparkling Aria"] = new(SignatureShot.Orbit, Light: Lit(0.5f, 0.66f, 0.82f, 0.18f), Effect: Spray),
        // Rillaboom's drum through the roots: a punch-in; the target jolted; leaves
        ["Drum Beating"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: Jolted, Light: Lit(0.18f, 0.36f, 0.12f, 0.3f), Effect: Leaves),
        // Cinderace's ball of fire: the camera rides it; embers
        ["Pyro Ball"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.68f, 0.3f, 0.08f, 0.32f), Effect: Embers),
        // Inteleon's shot: the camera rides it; spray
        ["Snipe Shot"] = new(SignatureShot.FollowProjectile, HeavyStop, Effect: Spray),
        // Meowscarada's bouquet: a punch-in on the burst; petals drifting down
        ["Flower Trick"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.5f, 0.2f, 0.34f, 0.26f), Effect: Petals),
        // Skeledirge's song of fire: the camera rides it; the user swelling as it sings; embers
        ["Torch Song"] = new(SignatureShot.FollowProjectile, HeavyStop, User: Heave, Light: Lit(0.66f, 0.32f, 0.1f, 0.3f), Effect: Embers),
        // Quaquaval's dancing step: the user lunges in; spray
        ["Aqua Step"] = new(SignatureShot.ImpactPunch, HeavyStop, User: Lunge, Effect: Spray),
        // The Hisuian Typhlosion's parade of spirit flames: the camera rides it; a ghostly violet; wisps
        ["Infernal Parade"] = new(SignatureShot.FollowProjectile, Light: Lit(0.3f, 0.14f, 0.4f, 0.32f), Effect: Wisps),

        // ---------------------------------------------------------------- Y17d: the legendaries' and mythicals' own, Unova to Paldea
        // Unova
        // Victini's V of flame: the user lunges in; a punch-in; fire's red-orange; embers
        ["V-create"] = new(SignatureShot.ImpactPunch, HeavyStop, User: Lunge, Light: Lit(0.66f, 0.24f, 0.06f, 0.34f), Effect: Embers),
        // Its ring of fire round the foes: the camera swings round
        ["Searing Shot"] = new(SignatureShot.Orbit, Light: Lit(0.62f, 0.3f, 0.08f, 0.28f), Effect: Embers),
        // The Swords of Justice's blade, and Keldeo's: punch-ins on the cut; steel pale, and a deep water blue
        ["Sacred Sword"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.7f, 0.74f, 0.8f, 0.16f), Effect: Blades),
        ["Secret Sword"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.22f, 0.32f, 0.56f, 0.28f)),
        // The forces of nature's storms (their Hisuian moves) round the foes: orbits, each its own colour and what it leaves
        ["Bleakwind Storm"] = new(SignatureShot.Orbit, Light: Lit(0.34f, 0.4f, 0.5f, 0.32f, 0.2f), Effect: Frost),
        ["Wildbolt Storm"] = new(SignatureShot.Orbit, Light: Lit(0.42f, 0.4f, 0.12f, 0.32f), Effect: Sparks),
        ["Sandsear Storm"] = new(SignatureShot.Orbit, Light: Lit(0.56f, 0.38f, 0.18f, 0.32f), Effect: Embers),
        // Reshiram's blue flare wound up and loosed, and its fusion flame followed; Zekrom's charge, and its fusion bolt followed
        ["Blue Flare"] = new(SignatureShot.ChargePush, HeavyStop, User: Recoil, Light: Lit(0.18f, 0.34f, 0.78f, 0.34f), Effect: Shimmer),
        ["Fusion Flare"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.86f, 0.5f, 0.3f, 0.22f), Effect: Embers),
        ["Bolt Strike"] = new(SignatureShot.ImpactPunch, HeavyStop, User: Lunge, Light: Lit(0.5f, 0.48f, 0.12f, 0.18f), Effect: Sparks),
        ["Fusion Bolt"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.24f, 0.3f, 0.6f, 0.3f), Effect: Sparks),
        // Kyurem's: the cold breath round the foes; its two charged moves wound up and let go
        ["Glaciate"] = new(SignatureShot.Orbit, Light: Lit(0.5f, 0.7f, 0.86f, 0.18f), Effect: Frost),
        ["Freeze Shock"] = new(SignatureShot.ChargePush, HeavyStop, Light: Lit(0.4f, 0.6f, 0.8f, 0.24f), Effect: Sparks),
        ["Ice Burn"] = new(SignatureShot.ChargePush, HeavyStop, Light: Lit(0.5f, 0.64f, 0.86f, 0.22f), Effect: Frost),
        // Meloetta's old song round the foes: rings of sound, a soft light
        ["Relic Song"] = new(SignatureShot.Orbit, Light: Lit(0.6f, 0.5f, 0.7f, 0.16f), Effect: SoundRings),
        // Genesect's blast: the camera rides it; a cold violet; sparks
        ["Techno Blast"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.38f, 0.28f, 0.54f, 0.28f), Effect: Sparks),
        // Kalos
        // Xerneas's gathering on itself: no shot of its own; glints standing round it
        ["Geomancy"] = new(SignatureShot.None, Light: Lit(0.6f, 0.5f, 0.75f, 0.2f), Effect: Glints),
        // Yveltal's wing of oblivion: the camera rides it; a deep red; wisps
        ["Oblivion Wing"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.38f, 0.05f, 0.1f, 0.4f), Effect: Wisps),
        // Zygarde's: arrows of earth from above pressing the target; waves of earth jolting it; its enforcer followed
        ["Thousand Arrows"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: Pressed, Light: Lit(0.3f, 0.36f, 0.18f, 0.3f), Effect: RingCracks),
        ["Thousand Waves"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: Jolted, Light: Lit(0.36f, 0.4f, 0.2f, 0.3f), Effect: RingCracks),
        ["Core Enforcer"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.2f, 0.44f, 0.24f, 0.3f), Effect: Glints),
        // Diancie's storm of diamonds round the foes: a pale, faint pink; glints
        ["Diamond Storm"] = new(SignatureShot.Orbit, Light: Lit(0.74f, 0.62f, 0.78f, 0.16f), Effect: Glints),
        // Hoopa's holes in space: punch-ins; the seam left hanging
        ["Hyperspace Hole"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.36f, 0.2f, 0.5f, 0.32f), Effect: Rift),
        ["Hyperspace Fury"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.2f, 0.08f, 0.24f, 0.38f), Effect: Rift),
        // Volcanion's steam: the push; the user heaving; spray
        ["Steam Eruption"] = new(SignatureShot.ChargePush, HeavyStop, User: Heave, Light: Lit(0.66f, 0.52f, 0.42f, 0.18f), Effect: Spray),
        // Alola
        // The island guardians' madness: the camera swings round; a muted violet
        ["Nature's Madness"] = new(SignatureShot.Orbit, Light: Lit(0.4f, 0.3f, 0.5f, 0.28f)),
        // Solgaleo's charge as the sun: the user lunges; gold; glints. Lunala's beam of the moon: followed; a night's violet; wisps
        ["Sunsteel Strike"] = new(SignatureShot.ImpactPunch, HeavyStop, User: Lunge, Light: Lit(0.86f, 0.6f, 0.24f, 0.22f), Effect: Glints),
        ["Moongeist Beam"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.2f, 0.12f, 0.42f, 0.4f), Effect: Wisps),
        // Necrozma's light wound up and loosed: the push, the user rocked back, glints; a pale gold and a prism's violet
        ["Photon Geyser"] = new(SignatureShot.ChargePush, HeavyStop, User: Recoil, Light: Lit(0.9f, 0.86f, 0.66f, 0.16f), Effect: Glints),
        ["Prismatic Laser"] = new(SignatureShot.ChargePush, HeavyStop, User: Recoil, Light: Lit(0.7f, 0.6f, 0.86f, 0.1f), Effect: Glints),
        // Magearna's cannon of flowers: the push, the user rocked back, petals
        ["Fleur Cannon"] = new(SignatureShot.ChargePush, HeavyStop, User: Recoil, Light: Lit(0.8f, 0.5f, 0.66f, 0.18f), Effect: Petals),
        // Marshadow's theft out of the shadow: a punch-in; near black; wisps
        ["Spectral Thief"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.1f, 0.06f, 0.14f, 0.4f), Effect: Wisps),
        // Zeraora's fists: a punch-in; sparks
        ["Plasma Fists"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.5f, 0.46f, 0.12f, 0.28f), Effect: Sparks),
        // Melmetal's two blows of its nut: a punch-in; the target pressed
        ["Double Iron Bash"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: Pressed, Light: Lit(0.36f, 0.36f, 0.4f, 0.24f, 0.2f)),
        // Galar
        // The heroes' charges, the sword and the shield: lunges; punch-ins; shards
        ["Behemoth Blade"] = new(SignatureShot.ImpactPunch, HeavyStop, User: Lunge, Light: Lit(0.3f, 0.4f, 0.6f, 0.28f), Effect: Shards),
        ["Behemoth Bash"] = new(SignatureShot.ImpactPunch, HeavyStop, User: Lunge, Light: Lit(0.5f, 0.2f, 0.2f, 0.28f), Effect: Shards),
        // Eternatus's beam wound up and loosed, and its cannon followed: a deep red
        ["Eternabeam"] = new(SignatureShot.ChargePush, HeavyStop, User: Recoil, Light: Lit(0.4f, 0.04f, 0.12f, 0.24f), Effect: Wisps),
        ["Dynamax Cannon"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.5f, 0.06f, 0.16f, 0.2f)),
        // Urshifu's: one blow of darkness held; three of water, light, no stop
        ["Wicked Blow"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.14f, 0.06f, 0.1f, 0.36f)),
        ["Surging Strikes"] = new(SignatureShot.ImpactPunch, Light: Lit(0.14f, 0.3f, 0.5f, 0.24f), Effect: Spray),
        // Zarude's healing in the jungle, on its side alone: no shot; leaves
        ["Jungle Healing"] = new(SignatureShot.None, Light: Lit(0.3f, 0.56f, 0.26f, 0.22f), Effect: Leaves),
        // Regieleki's cage round the foes; Regidrago's energy wound up and loosed
        ["Thunder Cage"] = new(SignatureShot.Orbit, Light: Lit(0.46f, 0.44f, 0.14f, 0.28f), Effect: Sparks),
        ["Dragon Energy"] = new(SignatureShot.ChargePush, HeavyStop, User: Recoil, Light: Lit(0.3f, 0.5f, 0.24f, 0.32f)),
        // Glastrier's lance from above, pressing; Spectrier's barrage round the foes
        ["Glacial Lance"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: Pressed, Light: Lit(0.5f, 0.68f, 0.86f, 0.22f), Effect: Frost),
        ["Astral Barrage"] = new(SignatureShot.Orbit, Light: Lit(0.16f, 0.1f, 0.3f, 0.4f), Effect: Wisps),
        // The Galarian birds': a dark wrath round the foes, a frozen glare followed, a kick of thunder punched in on
        ["Fiery Wrath"] = new(SignatureShot.Orbit, Light: Lit(0.24f, 0.06f, 0.14f, 0.38f), Effect: Wisps),
        ["Freezing Glare"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.36f, 0.3f, 0.56f, 0.28f), Effect: Frost),
        ["Thunderous Kick"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.4f, 0.32f, 0.12f, 0.28f), Effect: Sparks),
        // Hisui
        // Enamorus's storm of spring round the foes: petals
        ["Springtide Storm"] = new(SignatureShot.Orbit, Light: Lit(0.8f, 0.5f, 0.66f, 0.18f), Effect: Petals),
        // Paldea
        // The two paradox legends' charges: lunges; punch-ins; the ground cracked, and sparks
        ["Collision Course"] = new(SignatureShot.ImpactPunch, HeavyStop, User: Lunge, Light: Lit(0.5f, 0.2f, 0.08f, 0.32f), Effect: Cracks),
        ["Electro Drift"] = new(SignatureShot.ImpactPunch, HeavyStop, User: Lunge, Light: Lit(0.3f, 0.24f, 0.6f, 0.3f), Effect: Sparks),
        // The Treasures of Ruin's ruination round the foes: a dark red
        ["Ruination"] = new(SignatureShot.Orbit, Light: Lit(0.24f, 0.06f, 0.08f, 0.4f)),
        // Ogerpon's cudgel brought down: the target pressed; leaves
        ["Ivy Cudgel"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: Pressed, Light: Lit(0.18f, 0.38f, 0.14f, 0.26f), Effect: Leaves),
        // Terapagos's starstorm round the foes: a heavy orbit; stars
        ["Tera Starstorm"] = new(SignatureShot.Orbit, HeavyStop, Light: Lit(0.5f, 0.44f, 0.7f, 0.12f), Effect: Stars),
        // Pecharunt's chain: a punch-in held on it; the target only shudders; a poison's violet
        ["Malignant Chain"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: Held, Light: Lit(0.3f, 0.08f, 0.34f, 0.36f)),
        // The paradox legends' own: Walking Wake's steam followed, Iron Leaves' blade, Raging Bolt's clap, Iron Boulder's
        // cleave and Iron Crown's two cutters (light, no stop)
        ["Hydro Steam"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.5f, 0.6f, 0.7f, 0.18f), Effect: Spray),
        ["Psyblade"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.5f, 0.22f, 0.5f, 0.28f)),
        ["Thunderclap"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.5f, 0.48f, 0.14f, 0.3f), Effect: Sparks),
        ["Mighty Cleave"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.42f, 0.34f, 0.24f, 0.26f), Effect: Shards),
        ["Tachyon Cutter"] = new(SignatureShot.FollowProjectile, Light: Lit(0.4f, 0.44f, 0.56f, 0.24f), Effect: Glints),

        // ---------------------------------------------------------------- Y17e: the advanced and special moves
        // Wound up and let go: pushed in on, the user rocked back
        ["Sky Attack"] = new(SignatureShot.ChargePush, HeavyStop, User: Recoil, Light: Lit(0.66f, 0.6f, 0.36f, 0.2f)),
        ["Focus Blast"] = new(SignatureShot.ChargePush, HeavyStop, User: Recoil, Light: Lit(0.5f, 0.24f, 0.1f, 0.26f), Effect: Shards),
        // Bloodmoon Ursaluna's: a blood-red moon's light pushed in on, the user heaving as it roars it out
        ["Blood Moon"] = new(SignatureShot.ChargePush, HeavyStop, User: Heave, Light: Lit(0.42f, 0.04f, 0.06f, 0.4f), Effect: Embers),
        ["Chloroblast"] = new(SignatureShot.ChargePush, HeavyStop, User: Recoil, Light: Lit(0.3f, 0.56f, 0.2f, 0.1f), Effect: Leaves),
        // Balls and cannons followed across
        ["Shadow Ball"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.18f, 0.1f, 0.3f, 0.34f), Effect: Wisps),
        ["Flash Cannon"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.6f, 0.64f, 0.7f, 0.14f), Effect: Glints),
        ["Barb Barrage"] = new(SignatureShot.FollowProjectile, Light: Lit(0.3f, 0.12f, 0.36f, 0.26f), Effect: Shards),
        ["Bitter Malice"] = new(SignatureShot.FollowProjectile, Light: Lit(0.14f, 0.12f, 0.26f, 0.34f), Effect: Frost),
        ["Esper Wing"] = new(SignatureShot.FollowProjectile, Light: Lit(0.5f, 0.22f, 0.5f, 0.24f)),
        // A body flung in: lunges
        ["Brave Bird"] = new(SignatureShot.ImpactPunch, HeavyStop, User: Lunge, Light: Lit(0.5f, 0.6f, 0.76f, 0.22f)),
        ["Headlong Rush"] = new(SignatureShot.ImpactPunch, HeavyStop, User: Lunge, Light: Lit(0.38f, 0.28f, 0.16f, 0.24f), Effect: RingCracks),
        ["Wave Crash"] = new(SignatureShot.ImpactPunch, HeavyStop, User: Lunge, Light: Lit(0.14f, 0.32f, 0.56f, 0.26f), Effect: Spray),
        ["Psyshield Bash"] = new(SignatureShot.ImpactPunch, HeavyStop, User: Lunge, Light: Lit(0.5f, 0.24f, 0.5f, 0.22f), Effect: Glints),
        // Blows: punch-ins
        ["Superpower"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.5f, 0.24f, 0.1f, 0.24f)),
        ["Stone Axe"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.38f, 0.3f, 0.2f, 0.22f), Effect: Shards),
        ["Dire Claw"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.3f, 0.1f, 0.34f, 0.26f)),
        ["Mountain Gale"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: Pressed, Light: Lit(0.5f, 0.66f, 0.84f, 0.18f), Effect: Frost),
        // The two-turn moves' strikes: up from under for Dig and Dive, down from above for Bounce
        ["Dig"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: TossedUp, Effect: RingCracks),
        ["Dive"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: TossedUp, Effect: Spray),
        ["Bounce"] = new(SignatureShot.ImpactPunch, HeavyStop, User: Dive, Target: Pressed),
        // The Pledges: a pillar out of the ground under the target, punched in on, no stop
        ["Fire Pledge"] = new(SignatureShot.ImpactPunch, Target: TossedUp, Light: Lit(0.6f, 0.26f, 0.08f, 0.24f), Effect: Embers),
        ["Water Pledge"] = new(SignatureShot.ImpactPunch, Target: TossedUp, Light: Lit(0.12f, 0.3f, 0.56f, 0.24f), Effect: Spray),
        ["Grass Pledge"] = new(SignatureShot.ImpactPunch, Target: TossedUp, Light: Lit(0.18f, 0.4f, 0.14f, 0.24f), Effect: Leaves),
        // Round the foes: orbits
        ["Surf"] = new(SignatureShot.Orbit, Light: Lit(0.12f, 0.3f, 0.56f, 0.22f)),
        ["Blizzard"] = new(SignatureShot.Orbit, Light: Lit(0.5f, 0.66f, 0.84f, 0.2f, 0.2f), Effect: Frost),
        ["Heat Wave"] = new(SignatureShot.Orbit, Light: Lit(0.64f, 0.32f, 0.1f, 0.26f), Effect: Embers),
        ["Whirlpool"] = new(SignatureShot.Orbit, Light: Lit(0.1f, 0.26f, 0.5f, 0.24f), Effect: Spray),
        ["Thrash"] = new(SignatureShot.Orbit, HeavyStop),
        ["Matcha Gotcha"] = new(SignatureShot.Orbit, Light: Lit(0.3f, 0.5f, 0.2f, 0.22f), Effect: Leaves),
        ["Raging Fury"] = new(SignatureShot.Orbit, HeavyStop, Light: Lit(0.6f, 0.2f, 0.06f, 0.3f), Effect: Embers),
        // On its own side: no shot. A dance or a thought pulses an aura in the move's colour
        ["Swords Dance"] = new(SignatureShot.None, Effect: Aura),
        ["Nasty Plot"] = new(SignatureShot.None, Light: Lit(0.14f, 0.08f, 0.18f, 0.24f), Effect: Aura),
        ["Calm Mind"] = new(SignatureShot.None, Light: Lit(0.4f, 0.3f, 0.56f, 0.18f), Effect: Aura),
        ["Dragon Dance"] = new(SignatureShot.None, Light: Lit(0.24f, 0.2f, 0.5f, 0.22f), Effect: Aura),
        ["Victory Dance"] = new(SignatureShot.None, Light: Lit(0.5f, 0.3f, 0.16f, 0.18f), Effect: Aura),
        ["Take Heart"] = new(SignatureShot.None, Light: Lit(0.5f, 0.3f, 0.5f, 0.18f), Effect: Aura),
        // Cresselia's, left before: a night's lavender over its own side
        ["Lunar Blessing"] = new(SignatureShot.None, Light: Lit(0.4f, 0.34f, 0.62f, 0.3f), Effect: Glints),
        // Protections show what they are made of
        ["Spiky Shield"] = new(SignatureShot.None, Effect: Thorns),
        ["Burning Bulwark"] = new(SignatureShot.None, Light: Lit(0.6f, 0.24f, 0.06f, 0.22f), Effect: Embers),
        ["Shelter"] = new(SignatureShot.None, Effect: Glints),
        // Weather: a light alone; the weather's own look is Y1's
        ["Rain Dance"] = new(SignatureShot.None, Light: Lit(0.24f, 0.3f, 0.42f, 0.16f, 0.1f)),
        ["Sunny Day"] = new(SignatureShot.None, Light: Lit(0.9f, 0.7f, 0.36f, 0.05f)),

        // ---------------------------------------------------------------- Y17f: the moves of our own past level 100 (plan 06 · R31)
        // Each plays its type's template (no look of its own yet); the direction stages it. Ordinary strength (the
        // priority jabs, the drains, Rivet Shot, Root Snare, Pilfer Strike, Sap Lance, Swarm Chorus) and the debuffs
        // cast on the foes are left as ordinary moves, as Tackle is.
        // Normal: the tackle of a titan lunged in; the hymn rings round the foes
        ["Titan Tackle"] = new(SignatureShot.ImpactPunch, HeavyStop, User: Lunge, Light: Lit(0.5f, 0.44f, 0.36f, 0.2f)),
        ["Resonant Hymn"] = new(SignatureShot.Orbit, Light: Lit(0.66f, 0.56f, 0.36f, 0.16f), Effect: SoundRings),
        // Fighting: the kick and the mountain-breaking blow punched in on; the burst of ki followed
        ["Crescent Kick"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.5f, 0.3f, 0.14f, 0.22f)),
        ["Ki Burst"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.66f, 0.36f, 0.12f, 0.24f), Effect: Shards),
        ["Mountain Breaker"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: Pressed, Light: Lit(0.46f, 0.28f, 0.14f, 0.3f), Effect: RingCracks),
        // Flying: the talons and the plunge from the zenith rise and dive in; the jet stream followed
        ["Talon Dive"] = new(SignatureShot.ImpactPunch, HeavyStop, User: Dive, Light: Lit(0.5f, 0.6f, 0.76f, 0.2f)),
        ["Jet Stream"] = new(SignatureShot.FollowProjectile, Light: Lit(0.56f, 0.72f, 0.92f, 0.2f)),
        ["Zenith Plunge"] = new(SignatureShot.ImpactPunch, HeavyStop, User: Dive, Target: Pressed, Light: Lit(0.7f, 0.66f, 0.46f, 0.2f), Effect: RingCracks),
        // Poison: the lance punched in on; the torrent of miasma followed, a poison's violet, wisps rising
        ["Venom Lance"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.34f, 0.12f, 0.4f, 0.28f)),
        ["Miasma Torrent"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.3f, 0.1f, 0.36f, 0.32f), Effect: Wisps),
        // Ground: the fault and the rend jolt the target, cracks in a ring; the vein of magma bursts from under it
        ["Fault Line"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: Jolted, Light: Lit(0.38f, 0.3f, 0.18f, 0.24f, 0.2f), Effect: RingCracks),
        ["Magma Vein"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: TossedUp, Light: Lit(0.56f, 0.16f, 0.05f, 0.34f), Effect: Embers),
        ["Tectonic Rend"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: Jolted, Light: Lit(0.3f, 0.22f, 0.14f, 0.32f, 0.2f), Effect: RingCracks),
        // Rock: the boulder pressing, shards; the crystal lance followed, glints; stones falling from the sky round the foes
        ["Boulder Crush"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: Pressed, Light: Lit(0.38f, 0.3f, 0.2f, 0.24f), Effect: Shards),
        ["Crystal Lance"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.6f, 0.56f, 0.66f, 0.16f), Effect: Glints),
        ["Skyfall Stone"] = new(SignatureShot.Orbit, HeavyStop, Light: Lit(0.32f, 0.28f, 0.24f, 0.3f), Effect: Shards),
        // Bug: the mandibles close on the target, which only shudders in them
        ["Mandible Crush"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: Held, Light: Lit(0.3f, 0.4f, 0.12f, 0.24f)),
        // Ghost: the claw out of the shadow punched in on; the lantern's flame followed; wisps from both
        ["Spectral Claw"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.18f, 0.1f, 0.3f, 0.34f), Effect: Wisps),
        ["Phantom Lantern"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.24f, 0.14f, 0.42f, 0.32f), Effect: Wisps),
        // Steel: the forge's hammer brought down, sparks flying; the magnet's lance followed, glints
        ["Forge Hammer"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: Pressed, Light: Lit(0.5f, 0.32f, 0.14f, 0.24f), Effect: Sparks),
        ["Magnet Lance"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.44f, 0.48f, 0.58f, 0.2f), Effect: Glints),
        // Fire: the stomp of cinders pressing, embers; the pyre's lance followed; the blast of a forged sun wound up and
        // loosed, the user heaving with it, the air shimmering (its own fire is already bright: a low wash)
        ["Cinder Stomp"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: Pressed, Light: Lit(0.58f, 0.22f, 0.06f, 0.28f), Effect: Embers),
        ["Pyre Lance"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.64f, 0.28f, 0.08f, 0.28f), Effect: Embers),
        ["Sunforge Blast"] = new(SignatureShot.ChargePush, HeavyStop, User: Heave, Light: Lit(0.86f, 0.56f, 0.2f, 0.18f), Effect: Shimmer),
        // Water: the slam and the maelstrom's crash flung in, spray; the current of the abyss followed, a deep blue
        ["Riptide Slam"] = new(SignatureShot.ImpactPunch, HeavyStop, User: Lunge, Light: Lit(0.14f, 0.32f, 0.56f, 0.24f), Effect: Spray),
        ["Abyssal Current"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.05f, 0.14f, 0.4f, 0.4f), Effect: Spray),
        ["Maelstrom Crash"] = new(SignatureShot.ImpactPunch, HeavyStop, User: Lunge, Light: Lit(0.08f, 0.22f, 0.48f, 0.34f), Effect: Spray),
        // Grass: the lash of thorns punched in on, leaves
        ["Thornlash"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.18f, 0.38f, 0.14f, 0.26f), Effect: Leaves),
        // Electric: the lance of an arc flung in, sparks; the cascade of ions followed, sparks
        ["Arc Lance"] = new(SignatureShot.ImpactPunch, HeavyStop, User: Lunge, Light: Lit(0.46f, 0.44f, 0.14f, 0.22f), Effect: Sparks),
        ["Ion Cascade"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.42f, 0.4f, 0.16f, 0.22f), Effect: Sparks),
        // Psychic: the blade of the mind punched in on; the astral pulse followed, stars; the wave that breaks the mind
        // wound up and loosed, the user rocked back
        ["Mind Blade"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.5f, 0.22f, 0.5f, 0.26f)),
        ["Astral Pulse"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.3f, 0.2f, 0.52f, 0.3f), Effect: Stars),
        ["Mindbreak Wave"] = new(SignatureShot.ChargePush, HeavyStop, User: Recoil, Light: Lit(0.46f, 0.18f, 0.56f, 0.32f)),
        // Ice: the glacier rammed in, frost; the white-out round the foes; the lance of permafrost wound up and loosed
        ["Glacier Ram"] = new(SignatureShot.ImpactPunch, HeavyStop, User: Lunge, Light: Lit(0.42f, 0.6f, 0.8f, 0.22f), Effect: Frost),
        ["Whiteout Gale"] = new(SignatureShot.Orbit, Light: Lit(0.56f, 0.68f, 0.82f, 0.16f, 0.2f), Effect: Frost),
        ["Permafrost Lance"] = new(SignatureShot.ChargePush, HeavyStop, User: Recoil, Light: Lit(0.32f, 0.5f, 0.74f, 0.26f), Effect: Frost),
        // Dragon: the scales rent, a dragon's teal; the wyrm's flame followed, embers; the serpent's crash flung in, shards
        ["Scale Rend"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.12f, 0.34f, 0.4f, 0.26f)),
        ["Wyrmflare"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.4f, 0.16f, 0.4f, 0.3f), Effect: Embers),
        ["Serpent Crash"] = new(SignatureShot.ImpactPunch, HeavyStop, User: Lunge, Light: Lit(0.2f, 0.14f, 0.46f, 0.34f), Effect: Shards),
        // Dark: the fang held in the target; the umbral pulse followed, the dark vignette, wisps
        ["Rending Fang"] = new(SignatureShot.ImpactPunch, HeavyStop, Target: Held, Light: Lit(0.14f, 0.08f, 0.16f, 0.32f)),
        ["Umbral Pulse"] = new(SignatureShot.FollowProjectile, HeavyStop, Light: Lit(0.06f, 0.03f, 0.08f, 0.38f), Effect: Wisps),
        // Fairy: the fey strike punched in on, petals; the chorus of starlight round the foes, stars
        ["Fey Strike"] = new(SignatureShot.ImpactPunch, HeavyStop, Light: Lit(0.66f, 0.36f, 0.56f, 0.2f), Effect: Petals),
        ["Starlight Chorus"] = new(SignatureShot.Orbit, Light: Lit(0.56f, 0.5f, 0.74f, 0.16f), Effect: Stars),
        // On its own side, no shot: each setup pulses an aura in its type's colour; the plating glints, the overcharge sparks
        ["Rally Cry"] = new(SignatureShot.None, Light: Lit(0.6f, 0.5f, 0.36f, 0.14f), Effect: Aura),
        ["Centered Stance"] = new(SignatureShot.None, Light: Lit(0.5f, 0.3f, 0.14f, 0.16f), Effect: Aura),
        ["Updraft"] = new(SignatureShot.None, Light: Lit(0.56f, 0.7f, 0.9f, 0.14f), Effect: Aura),
        ["Monolith Pose"] = new(SignatureShot.None, Light: Lit(0.4f, 0.34f, 0.24f, 0.18f), Effect: Aura),
        ["Molt Renewal"] = new(SignatureShot.None, Light: Lit(0.4f, 0.5f, 0.16f, 0.16f), Effect: Aura),
        ["Alloy Plating"] = new(SignatureShot.None, Light: Lit(0.5f, 0.54f, 0.6f, 0.16f), Effect: Glints),
        ["Kindle"] = new(SignatureShot.None, Light: Lit(0.6f, 0.28f, 0.08f, 0.18f), Effect: Aura),
        ["Tidal Ward"] = new(SignatureShot.None, Light: Lit(0.14f, 0.32f, 0.56f, 0.18f), Effect: Aura),
        ["Verdant Renewal"] = new(SignatureShot.None, Light: Lit(0.26f, 0.5f, 0.2f, 0.16f), Effect: Aura),
        ["Overcharge"] = new(SignatureShot.None, Light: Lit(0.46f, 0.44f, 0.14f, 0.18f), Effect: Sparks),
        ["Clairvoyance"] = new(SignatureShot.None, Light: Lit(0.46f, 0.24f, 0.5f, 0.18f), Effect: Aura),
        ["Wyrm Dance"] = new(SignatureShot.None, Light: Lit(0.24f, 0.2f, 0.5f, 0.2f), Effect: Aura),
        ["Moonlit Grace"] = new(SignatureShot.None, Light: Lit(0.4f, 0.34f, 0.62f, 0.22f), Effect: Aura),
    };

    private static StageLight Lit(float r, float g, float b, float amount, float grey = 0f) => new(new Vector3(r, g, b), amount, grey);

    public static MoveDirection? Of(string move) => Table.TryGetValue(move, out var d) ? d : null;

    public static bool Has(string move) => Table.ContainsKey(move);

    /// <summary>Whether a cue is staged by its direction at all: a move's strike or its charge turn, nothing else.</summary>
    public static MoveDirection? For(EffectCue cue) =>
        cue.Kind == CueKind.Move && !cue.Arriving && !cue.ZMove && !cue.MaxMove ? Of(cue.Move) : null;

    /// <summary>A giant's growth's hold at its burst, seconds (plan 25 · YD1), under the motion number.</summary>
    public const float GiantStop = 0.12f;

    /// <summary>A Max Move's hold at its impact, seconds (plan 25 · YD2), under the motion number.</summary>
    public const float MaxStop = 0.12f;

    /// <summary>Whether a cue is a Max Move's finisher (plan 25 · YD2): a Max Move or G-Max Move that landed on another.</summary>
    public static bool IsMaxFinisher(EffectCue cue) =>
        cue.Kind == CueKind.Move && cue.MaxMove && !cue.OnSelf && !cue.Charging && !cue.Arriving && cue.Landed;

    /// <summary>How long a cue holds at its impact now: a landed strike of a heavy signature move or a Max Move (plan 25 · YD2), or a giant's growth at its burst (plan 25 · YD1), under the motion number.</summary>
    public static float StopOf(EffectCue cue)
    {
        if (cue.Kind == CueKind.Dynamax) return GiantStop * Math.Clamp(BattleCamera.Motion, 0f, 1f);
        if (IsMaxFinisher(cue)) return MaxStop * Math.Clamp(BattleCamera.Motion, 0f, 1f);
        if (cue.Charging || !cue.Landed || For(cue) is not { HitStop: > 0f } d) return 0f;
        return d.HitStop * Math.Clamp(BattleCamera.Motion, 0f, 1f);
    }

    /// <summary>How far a Max Move throws its target back, in its height, and how flat it presses it (plan 25 · YD2).</summary>
    public const float MaxThrowReach = 0.25f, MaxThrowSquash = 0.15f;

    /// <summary>
    /// A Max Move's target thrown (plan 25 · YD2; style guide, "The Max Move finisher"): back 0.25 of its height and
    /// pressed to 0.85 at the impact, then staggering home over 0.5 s with a lurch forward as it settles; a G-Max Move
    /// whose mark falls presses it down, one whose mark rises tosses it up. Still before the impact and after.
    /// </summary>
    public static BodyMotion MaxThrown(EffectCue cue, float age)
    {
        if (age < I || age >= I + 0.55f) return BodyMotion.Still;
        float t = age - I;
        // At its furthest from the impact on, so the hit-stop holds it there, and staggering home after
        float k = 1f - Smooth((t - 0.05f) / 0.5f);
        switch (MoveFx.GMaxWayOf(cue.Move))
        {
            case -1:
                return new BodyMotion(Sink: 0.06f * k, Squash: 0.18f * k);
            case 1:
            {
                float u = Math.Clamp(t / 0.55f, 0f, 1f);
                float up = 4f * u * (1f - u);
                return new BodyMotion(Up: 0.15f * up, Toward: -0.08f * up, Squash: 0.1f * k * (1f - up));
            }
        }
        // A lurch forward past home as it settles, a fifth of the throw
        float lurch = t > 0.3f ? 0.2f * MathF.Sin(Math.Clamp((t - 0.3f) / 0.25f, 0f, 1f) * MathF.PI) : 0f;
        return new BodyMotion(Toward: -MaxThrowReach * k + MaxThrowReach * lurch, Squash: MaxThrowSquash * k);
    }

    /// <summary>When a Dynamax's end shudders: from its start to <see cref="BattleAnimator.DynamaxFallStart"/> (plan 25 · YD3).</summary>
    public static BodyMotion Shudder(float age)
    {
        const float S = BattleAnimator.DynamaxFallStart;
        if (age < 0f || age >= S) return BodyMotion.Still;
        float k = 1f - age / S;
        return new BodyMotion(Toward: 0.03f * k * MathF.Sin(age * MathF.Tau * 14f));
    }

    /// <summary>Where a cue's hold falls: a move's impact, or a giant's burst after its trainer's call (plan 25 · YD1).</summary>
    public static float ImpactOf(EffectCue cue) =>
        cue.Kind == CueKind.Dynamax ? cue.Lead + BattleAnimator.DynamaxGrown : I;

    /// <summary>
    /// The age a cue is drawn at (the hit-stop): its own age up to the impact, held there for <paramref name="stop"/>,
    /// then caught up so that it ends at <paramref name="length"/> as it would have. With no stop it is the age itself.
    /// </summary>
    public static float Remap(float age, float stop, float length) => Remap(age, stop, length, I);

    /// <summary>The same, held at <paramref name="impact"/> instead of a move's impact (plan 25 · YD1: a giant's burst).</summary>
    public static float Remap(float age, float stop, float length, float impact)
    {
        if (stop <= 0f || age <= impact || length <= impact + stop) return age;
        if (age < impact + stop) return impact;
        if (age >= length) return age;
        return impact + (age - impact - stop) * (length - impact) / (length - impact - stop);
    }

    /// <summary>The shown age of a cue (<see cref="EffectCue.ShownAge"/>).</summary>
    public static float ShownAge(EffectCue cue)
    {
        float stop = StopOf(cue);
        return stop <= 0f ? cue.Age : Remap(cue.Age, stop, cue.Length, ImpactOf(cue));
    }

    /// <summary>A heavy hit's throw: back from the user and pressed a little at the impact, settling over 0.5 s.</summary>
    public static BodyMotion Thrown(float age)
    {
        if (age < I || age >= I + 0.55f) return BodyMotion.Still;
        float t = age - I;
        float k = t < 0.05f ? Smooth(t / 0.05f) : 1f - Smooth((t - 0.05f) / 0.5f);
        return new BodyMotion(Toward: -ThrowReach * k, Squash: ThrowSquash * k);
    }

    /// <summary>The user rocked back as a beam leaves it, and settling.</summary>
    private static BodyMotion Recoil(float age, int seed)
    {
        if (age < I - 0.12f || age >= I + 0.6f) return BodyMotion.Still;
        float t = age - (I - 0.12f);
        float k = t < 0.08f ? Smooth(t / 0.08f) : 1f - Smooth((t - 0.08f) / 0.55f);
        return new BodyMotion(Toward: -0.08f * k);
    }

    /// <summary>A beat of a target's reaction: in over 0.05 s at the impact, out over <paramref name="settle"/>.</summary>
    private static float Beat(float age, float settle)
    {
        if (age < I || age >= I + 0.05f + settle) return 0f;
        float t = age - I;
        return t < 0.05f ? Smooth(t / 0.05f) : 1f - Smooth((t - 0.05f) / settle);
    }

    /// <summary>A target pressed down from above (Judgment's light, Wood Hammer's blow): flattened and sunk a little.</summary>
    private static BodyMotion Pressed(float age, int seed)
    {
        float k = Beat(age, 0.5f);
        return k <= 0f ? BodyMotion.Still : new BodyMotion(Sink: 0.06f * k, Squash: 0.14f * k);
    }

    /// <summary>A target held in the grip (Y16 squeezes it): no throw, only a shudder as the grip tightens.</summary>
    private static BodyMotion Held(float age, int seed)
    {
        float k = Beat(age, 0.45f);
        return k <= 0f ? BodyMotion.Still : new BodyMotion(Spin: 0.08f * k * MathF.Sin((age - I) * 60f));
    }

    /// <summary>A target tossed up by what bursts under it (Precipice Blades, Frenzy Plant), coming down and settling.</summary>
    private static BodyMotion TossedUp(float age, int seed)
    {
        if (age < I || age >= I + 0.6f) return BodyMotion.Still;
        float t = (age - I) / 0.6f;
        float up = 4f * t * (1f - t);
        return new BodyMotion(Up: 0.22f * up, Toward: -0.05f * up, Squash: t > 0.8f ? 0.08f * (1f - t) / 0.2f : 0f);
    }

    /// <summary>A target shaken by the ground under it (Earthquake): three quick jolts up and down, settling.</summary>
    private static BodyMotion Jolted(float age, int seed)
    {
        float k = Beat(age, 0.55f);
        if (k <= 0f) return BodyMotion.Still;
        float jolt = MathF.Abs(MathF.Sin((age - I) * 26f));
        return new BodyMotion(Up: 0.06f * k * jolt, Squash: 0.06f * k * (1f - jolt));
    }

    /// <summary>
    /// Dragon Ascent's dive: the user rises over its wind-up, hurtles in to meet the target at the impact and goes
    /// home. Kept under a third of its height up, as Y16's rises are, so the player's own stays in the picture.
    /// </summary>
    private static BodyMotion Dive(float age, int seed)
    {
        const float start = I - 0.3f, end = I + 0.5f;
        if (age < start || age >= end) return BodyMotion.Still;
        if (age < I - 0.1f)
        {
            float k = Smooth((age - start) / 0.2f);
            return new BodyMotion(Up: 0.3f * k, Toward: -0.06f * k);
        }
        if (age < I)
        {
            float k = Smooth((age - (I - 0.1f)) / 0.1f);
            return new BodyMotion(Up: 0.3f * (1f - k), Toward: -0.06f + 0.66f * k, Narrow: 0.12f * k);
        }
        float back = Smooth((age - I) / (end - I));
        return new BodyMotion(Toward: 0.6f * (1f - back));
    }

    /// <summary>The user heaving as fire swells in it and bursts out (Eruption, Overheat): pressed down, then stretched up and settling.</summary>
    private static BodyMotion Heave(float age, int seed)
    {
        if (age < I - 0.25f || age >= I + 0.55f) return BodyMotion.Still;
        if (age < I)
        {
            float k = Smooth((age - (I - 0.25f)) / 0.25f);
            return new BodyMotion(Squash: 0.12f * k, Grow: 1f + 0.04f * k);
        }
        float s = 1f - Smooth((age - I) / 0.55f);
        return new BodyMotion(Squash: -0.1f * s, Toward: -0.05f * s);
    }

    /// <summary>
    /// A charge that meets its target (Dragon Rush, Glaive Rush, Aqua Step): drawn back a little, lunging in to the
    /// impact and going home over 0.45 s, with no rise.
    /// </summary>
    private static BodyMotion Lunge(float age, int seed)
    {
        const float start = I - 0.22f, end = I + 0.45f;
        if (age < start || age >= end) return BodyMotion.Still;
        if (age < I - 0.1f)
        {
            float k = Smooth((age - start) / 0.12f);
            return new BodyMotion(Toward: -0.06f * k, Squash: 0.05f * k);
        }
        if (age < I)
        {
            float k = Smooth((age - (I - 0.1f)) / 0.1f);
            return new BodyMotion(Toward: -0.06f + 0.61f * k, Narrow: 0.1f * k);
        }
        float back = Smooth((age - I) / (end - I));
        return new BodyMotion(Toward: 0.55f * (1f - back));
    }

    /// <summary>What a direction does to the user's body at a cue's shown age.</summary>
    public static BodyMotion UserBody(EffectCue cue)
    {
        if (cue.Charging || For(cue) is not { User: { } user }) return BodyMotion.Still;
        return user(cue.ShownAge, cue.Seed);
    }

    /// <summary>What a direction does to the target's body at a cue's shown age: its own, or a heavy hit's throw.</summary>
    public static BodyMotion TargetBody(EffectCue cue)
    {
        if (IsMaxFinisher(cue)) return MaxThrown(cue, cue.ShownAge);
        if (cue.Charging || cue.OnSelf || !cue.Landed || For(cue) is not { } d) return BodyMotion.Still;
        if (d.Target != null) return d.Target(cue.ShownAge, cue.Seed);
        return d.Heavy ? Thrown(cue.ShownAge) : BodyMotion.Still;
    }

    /// <summary>A direction's stage light at a cue's shown age (none for a charge turn).</summary>
    public static StageLight LightAt(EffectCue cue)
    {
        if (cue.Charging || For(cue) is not { Light: { } light }) return StageLight.None;
        return light.Scaled(StageLights.Envelope(cue.ShownAge, cue.Length));
    }

    /// <summary>Plays a direction's effect over the move's own recipe (nothing for a charge turn).</summary>
    public static void PlayEffect(in FxCtx c, FxColors col, FxList fx)
    {
        if (c.Cue.Charging || For(c.Cue) is not { Effect: { } effect }) return;
        effect(c, col, fx);
    }

    // ------------------------------------------------------------------ effects laid over

    /// <summary>Roar of Time: three rings of pale steel-blue standing round the target and slowing as they widen, after the impact.</summary>
    private static void TimeRings(in FxCtx c, FxColors col, FxList fx)
    {
        if (c.Cue.Missed) return;
        for (int i = 0; i < 3; i++)
        {
            float t0 = I + 0.04f + i * 0.12f;
            float k = (c.Age - t0) / 0.6f;
            if (k <= 0f || k >= 1f) continue;
            float grow = 1f - (1f - k) * (1f - k) * (1f - k);
            float alpha = 0.85f * (1f - k);
            fx.Sprite(FxShape.Ring, c.To, c.ToSize * (0.35f + 0.9f * grow), Fx.Fade(Rgb(170, 220, 255), alpha), FxBlend.Alpha);
        }
    }

    /// <summary>How far a target's reaction has come at a shown age: nought before the impact and after <paramref name="lasts"/>, a fade in between.</summary>
    private static float After(in FxCtx c, float delay, float lasts)
    {
        if (c.Cue.Missed) return -1f;
        float k = (c.Age - I - delay) / lasts;
        return k < 0f || k >= 1f ? -1f : k;
    }

    /// <summary>The ground's slant toward the battle's cameras, which stand on its +Z side: flat things lie on it and still read.</summary>
    private static readonly Vector3 GroundToLens = Vector3.Normalize(new Vector3(0f, 1f, 0.3f));

    /// <summary>A halo over a head tilted further toward the lens, so it reads as a ring and not a line.</summary>
    private static readonly Vector3 HaloTilt = Vector3.Normalize(new Vector3(0f, 1f, 0.7f));

    /// <summary>Embers left on the target's platform (the fire moves): six flecks glowing and lifting a little, gone in a second.</summary>
    private static void Embers(in FxCtx c, FxColors col, FxList fx)
    {
        float k = After(c, 0.05f, 0.85f);
        if (k < 0f) return;
        for (int i = 0; i < 6; i++)
        {
            float a = Fx.Rand(c.Seed, i, 171) * MathF.Tau;
            float r = c.ToSize * (0.35f + 0.35f * Fx.Rand(c.Seed, i, 172));
            var at = c.ToFeet + new Vector3(MathF.Cos(a) * r, c.ToSize * (0.04f + 0.18f * k * Fx.Rand(c.Seed, i, 173)), MathF.Sin(a) * r * 0.6f);
            float flicker = 0.75f + 0.25f * MathF.Sin(c.Age * 30f + i * 2.1f);
            fx.Sprite(FxShape.Ember, at, c.ToSize * 0.11f, Fx.Fade(Rgb(255, 150, 50), (1f - k) * flicker), FxBlend.Alpha);
        }
    }

    /// <summary>Cracks in the ground at the target's feet (Ground and the heavy blows), dark and fading in a second.</summary>
    private static void Cracks(in FxCtx c, FxColors col, FxList fx)
    {
        float k = After(c, 0f, 0.9f);
        if (k < 0f) return;
        float alpha = k < 0.1f ? k / 0.1f : 1f - (k - 0.1f) / 0.9f;
        for (int i = 0; i < 2; i++)
        {
            float off = (i == 0 ? -0.25f : 0.3f) * c.ToSize;
            var at = c.ToFeet + new Vector3(off, 0.02f, 0.55f * c.ToSize);
            fx.Flat(FxShape.Crack, at, GroundToLens, c.ToSize * (0.55f - 0.1f * i), Fx.Fade(Rgb(52, 40, 30), 0.9f * alpha), FxBlend.Alpha, Fx.Rand(c.Seed, i, 181) * MathF.Tau);
        }
    }

    /// <summary>Leaves settling on the target's platform (the Grass moves), drifting down and gone.</summary>
    private static void Leaves(in FxCtx c, FxColors col, FxList fx)
    {
        float k = After(c, 0.05f, 0.85f);
        if (k < 0f) return;
        for (int i = 0; i < 6; i++)
        {
            float x = Fx.Signed(c.Seed, i, 191) * c.ToSize * 0.7f;
            float fall = 1f - Fx.EaseOut(Math.Min(1f, k * 1.5f + Fx.Rand(c.Seed, i, 192) * 0.2f));
            var at = c.ToFeet + new Vector3(x + MathF.Sin(c.Age * 5f + i) * 0.08f * c.ToSize, c.ToSize * (0.05f + 0.75f * fall), 0.2f * c.ToSize);
            fx.Sprite(FxShape.Leaf, at, c.ToSize * 0.13f, Fx.Fade(col.Main, 1f - k * k), FxBlend.Alpha, c.Age * 4f + i);
        }
    }

    /// <summary>Spray falling back round the target (the Water moves): drops coming down after the splash.</summary>
    private static void Spray(in FxCtx c, FxColors col, FxList fx)
    {
        float k = After(c, 0.05f, 0.7f);
        if (k < 0f) return;
        for (int i = 0; i < 7; i++)
        {
            float a = Fx.Signed(c.Seed, i, 201);
            float rise = 4f * k * (1f - k);
            var at = c.To + new Vector3(a * c.ToSize * (0.4f + 0.5f * k), c.ToSize * (0.35f * rise - 0.4f * k), 0.2f * c.ToSize);
            fx.Sprite(FxShape.Drop, at, c.ToSize * 0.1f, Fx.Fade(col.Light, 1f - k), FxBlend.Alpha);
        }
    }

    /// <summary>Spacial Rend: a dark seam left hanging across the target where space was cut, stars in it, closing.</summary>
    private static void Rift(in FxCtx c, FxColors col, FxList fx)
    {
        float k = After(c, 0f, 0.8f);
        if (k < 0f) return;
        float open = k < 0.15f ? k / 0.15f : 1f - Smooth((k - 0.15f) / 0.85f);
        var a = c.To + new Vector3(-0.75f, 0.6f, 0.3f) * c.ToSize;
        var b = c.To + new Vector3(0.75f, -0.5f, 0.3f) * c.ToSize;
        var points = new Vector3[5];
        for (int i = 0; i < 5; i++)
        {
            float t = i / 4f;
            var p = Vector3.Lerp(a, b, t);
            float zig = (i % 2 == 0 ? 1f : -1f) * 0.06f * c.ToSize * (i == 0 || i == 4 ? 0f : 1f);
            points[i] = p + new Vector3(zig, zig, 0f);
        }
        fx.Ribbon(points, c.ToSize * 0.09f * open, Fx.Fade(Rgb(40, 16, 60), 0.9f * open), FxBlend.Alpha, depth: false);
        fx.Ribbon(points, c.ToSize * 0.03f * open, Fx.Fade(Rgb(255, 210, 250), 0.8f * open), FxBlend.Add, depth: false);
        for (int i = 0; i < 4; i++)
        {
            var at = Vector3.Lerp(a, b, 0.15f + 0.23f * i) + new Vector3(0f, 0.05f * c.ToSize * MathF.Sin(c.Age * 9f + i), 0f);
            fx.Sprite(FxShape.Star, at, c.ToSize * 0.07f * open, Fx.Fade(Rgb(255, 230, 255), open), FxBlend.Alpha, c.Age * 3f + i);
        }
    }

    /// <summary>Judgment: a gold halo over the target settling down onto it as the light presses it.</summary>
    private static void Halo(in FxCtx c, FxColors col, FxList fx)
    {
        float k = After(c, -0.1f, 0.9f);
        if (k < 0f) return;
        float down = Fx.EaseOut(Math.Min(1f, k * 2f));
        var at = c.ToFeet + Vector3.UnitY * c.ToSize * (1.25f - 0.35f * down);
        float alpha = k < 0.1f ? k / 0.1f : 1f - (k - 0.1f) / 0.9f;
        fx.Flat(FxShape.Ring, at, HaloTilt, c.ToSize * (0.9f - 0.2f * down), Fx.Fade(Rgb(255, 216, 110), 0.9f * alpha), FxBlend.Alpha, depth: false);
        fx.Flat(FxShape.Ring, at, HaloTilt, c.ToSize * (0.86f - 0.2f * down), Fx.Fade(Rgb(255, 250, 220), 0.6f * alpha), FxBlend.Add, depth: false);
    }

    /// <summary>Shadow Force: dark wisps rising off the target where the shadow struck it, gone in a second.</summary>
    private static void Wisps(in FxCtx c, FxColors col, FxList fx)
    {
        float k = After(c, 0.02f, 0.85f);
        if (k < 0f) return;
        for (int i = 0; i < 5; i++)
        {
            float x = Fx.Signed(c.Seed, i, 211) * c.ToHalfWidth * 1.2f;
            var at = c.ToFeet + new Vector3(x, c.ToSize * (0.2f + 0.9f * k + 0.1f * Fx.Rand(c.Seed, i, 212)), 0.15f * c.ToSize);
            fx.Sprite(FxShape.Wisp, at, c.ToSize * 0.2f, Fx.Fade(Rgb(52, 26, 78), 0.85f * (1f - k)), FxBlend.Alpha, Fx.Signed(c.Seed, i, 213) * 0.5f);
        }
    }

    /// <summary>Eruption, Overheat: the air over the target wavering with heat, pale bands rising and gone.</summary>
    private static void Shimmer(in FxCtx c, FxColors col, FxList fx)
    {
        float k = After(c, 0.05f, 0.85f);
        if (k < 0f) return;
        for (int i = 0; i < 3; i++)
        {
            float rise = (k + i / 3f) % 1f;
            var at = c.ToFeet + new Vector3(MathF.Sin(c.Age * 8f + i * 2f) * 0.08f * c.ToSize, c.ToSize * (0.3f + 1.0f * rise), 0.25f * c.ToSize);
            float alpha = 0.45f * MathF.Sin(rise * MathF.PI) * (1f - k);
            fx.Sprite(FxShape.Wave, at, c.ToSize * 0.9f, Fx.Fade(Rgb(255, 196, 140), alpha), FxBlend.Alpha);
        }
    }

    // ------------------------------------------------------------------ Y17c's reactions

    /// <summary>Shards of rock falling round the target's feet (Stone Edge, Head Smash, Ceaseless Edge's splinters), tumbling and gone.</summary>
    private static void Shards(in FxCtx c, FxColors col, FxList fx)
    {
        float k = After(c, 0.03f, 0.8f);
        if (k < 0f) return;
        for (int i = 0; i < 6; i++)
        {
            float x = Fx.Signed(c.Seed, i, 221) * (c.ToHalfWidth + 0.25f * c.ToSize);
            float fall = 1f - Fx.EaseOut(Math.Min(1f, k * 1.8f + Fx.Rand(c.Seed, i, 222) * 0.15f));
            var at = c.ToFeet + new Vector3(x, c.ToSize * (0.04f + 0.5f * fall), 0.3f * c.ToSize);
            float alpha = k < 0.75f ? 1f : (1f - k) / 0.25f;
            fx.Sprite(FxShape.Shard, at, c.ToSize * 0.12f, Fx.Fade(Rgb(150, 128, 100), 0.95f * alpha), FxBlend.Alpha, c.Age * 6f * (i % 2 == 0 ? 1f : -1f) + i);
        }
    }

    /// <summary>Meteor Mash: stars springing off the body in gold and white, arcing out and gone.</summary>
    private static void Stars(in FxCtx c, FxColors col, FxList fx)
    {
        float k = After(c, 0f, 0.7f);
        if (k < 0f) return;
        float out1 = Fx.EaseOut(k);
        for (int i = 0; i < 6; i++)
        {
            float a = (i / 6f + 0.08f * Fx.Signed(c.Seed, i, 231)) * MathF.Tau;
            var at = c.To + new Vector3(MathF.Cos(a) * c.ToSize * 0.75f * out1, MathF.Sin(a) * c.ToSize * 0.6f * out1 + c.ToSize * 0.1f * out1, 0.3f * c.ToSize);
            var tint = i % 2 == 0 ? Rgb(255, 222, 120) : Rgb(255, 250, 230);
            fx.Sprite(FxShape.Star, at, c.ToSize * 0.11f, Fx.Fade(tint, 1f - k * k), FxBlend.Alpha, c.Age * 5f + i);
        }
    }

    /// <summary>Clanging Scales, Clangorous Soul: gold rings of sound standing round the one it rings out on, widening and gone.</summary>
    private static void SoundRings(in FxCtx c, FxColors col, FxList fx)
    {
        if (c.Cue.Missed) return;
        for (int i = 0; i < 3; i++)
        {
            float t0 = I + i * 0.1f;
            float k = (c.Age - t0) / 0.5f;
            if (k <= 0f || k >= 1f) continue;
            float grow = Fx.EaseOut(k);
            fx.Sprite(FxShape.Ring, c.To, c.ToSize * (0.4f + 0.8f * grow), Fx.Fade(Rgb(255, 214, 120), 0.8f * (1f - k)), FxBlend.Alpha);
        }
    }

    /// <summary>Glaive Rush: frost glinting on the target's platform in snowflakes, melting away.</summary>
    private static void Frost(in FxCtx c, FxColors col, FxList fx)
    {
        float k = After(c, 0.05f, 0.85f);
        if (k < 0f) return;
        float alpha = k < 0.1f ? k / 0.1f : 1f - (k - 0.1f) / 0.9f;
        for (int i = 0; i < 7; i++)
        {
            float a = Fx.Rand(c.Seed, i, 241) * MathF.Tau;
            float r = c.ToSize * (0.3f + 0.4f * Fx.Rand(c.Seed, i, 242));
            var at = c.ToFeet + new Vector3(MathF.Cos(a) * r, c.ToSize * 0.05f, MathF.Sin(a) * r * 0.6f + 0.2f * c.ToSize);
            float glint = 0.7f + 0.3f * MathF.Sin(c.Age * 14f + i * 1.7f);
            fx.Sprite(FxShape.Snowflake, at, c.ToSize * 0.1f, Fx.Fade(Rgb(214, 238, 255), alpha * glint), FxBlend.Alpha, i);
        }
    }

    /// <summary>Flower Trick: petals drifting down round the target after the bouquet bursts.</summary>
    private static void Petals(in FxCtx c, FxColors col, FxList fx)
    {
        float k = After(c, 0.03f, 0.85f);
        if (k < 0f) return;
        for (int i = 0; i < 7; i++)
        {
            float x = Fx.Signed(c.Seed, i, 251) * c.ToSize * 0.75f;
            float fall = 1f - Fx.EaseOut(Math.Min(1f, k * 1.4f + Fx.Rand(c.Seed, i, 252) * 0.2f));
            var at = c.ToFeet + new Vector3(x + MathF.Sin(c.Age * 4f + i) * 0.1f * c.ToSize, c.ToSize * (0.08f + 0.9f * fall), 0.25f * c.ToSize);
            var tint = i % 2 == 0 ? Rgb(255, 150, 190) : Rgb(255, 214, 230);
            fx.Sprite(FxShape.Petal, at, c.ToSize * 0.12f, Fx.Fade(tint, 1f - k * k), FxBlend.Alpha, c.Age * 3f + i);
        }
    }

    // ------------------------------------------------------------------ Y17d's reactions

    /// <summary>Sparks crackling round the target after an electric blow (Bolt Strike, Thunder Cage, Electro Drift…), flickering out.</summary>
    private static void Sparks(in FxCtx c, FxColors col, FxList fx)
    {
        float k = After(c, 0.02f, 0.75f);
        if (k < 0f) return;
        for (int i = 0; i < 7; i++)
        {
            // Each spark jumps to a new place round the body every tenth of a second, on and off
            int hop = (int)(c.Age * 10f);
            float a = Fx.Rand(c.Seed, i * 31 + hop, 261) * MathF.Tau;
            float on = Fx.Rand(c.Seed, i * 31 + hop, 262) < 0.7f ? 1f : 0.25f;
            float r = c.ToHalfWidth * (0.8f + 0.5f * Fx.Rand(c.Seed, i, 263));
            var at = c.To + new Vector3(MathF.Cos(a) * r, MathF.Sin(a) * c.ToSize * 0.45f, 0.3f * c.ToSize);
            fx.Sprite(FxShape.Spark, at, c.ToSize * 0.13f, Fx.Fade(Rgb(255, 244, 150), (1f - k) * on), FxBlend.Alpha, a);
        }
    }

    /// <summary>Glints of crystal or light standing round the one it lands on (Diamond Storm, Necrozma's light, Geomancy), turning and gone.</summary>
    private static void Glints(in FxCtx c, FxColors col, FxList fx)
    {
        float k = After(c, 0f, 0.8f);
        if (k < 0f) return;
        float out1 = Fx.EaseOut(k);
        for (int i = 0; i < 6; i++)
        {
            float a = (i / 6f + 0.1f * Fx.Signed(c.Seed, i, 271)) * MathF.Tau + c.Age * 1.5f;
            float r = (c.ToHalfWidth + 0.15f * c.ToSize) * (0.9f + 0.4f * out1);
            var at = c.ToFeet + new Vector3(MathF.Cos(a) * r, c.ToSize * (0.25f + 0.55f * Fx.Rand(c.Seed, i, 272) + 0.15f * out1), 0.3f * c.ToSize);
            var tint = (i % 3) switch { 0 => Rgb(255, 236, 250), 1 => Rgb(214, 236, 255), _ => Rgb(255, 246, 214) };
            float alpha = k < 0.1f ? k / 0.1f : 1f - (k - 0.1f) / 0.9f;
            fx.Sprite(FxShape.Shard, at, c.ToSize * 0.1f, Fx.Fade(tint, 0.95f * alpha), FxBlend.Alpha, c.Age * 4f * (i % 2 == 0 ? 1f : -1f) + i);
        }
    }

    // ------------------------------------------------------------------ Y17e's reactions

    /// <summary>
    /// A setup move's aura (Swords Dance, Nasty Plot, Calm Mind, Dragon Dance…): two rings in the move's colour rising
    /// up the body it is on, tilted to the lens, and four motes lifting off it, pulsing once and gone.
    /// </summary>
    private static void Aura(in FxCtx c, FxColors col, FxList fx)
    {
        float k = After(c, -0.05f, 0.8f);
        if (k < 0f) return;
        float alpha = k < 0.15f ? k / 0.15f : 1f - (k - 0.15f) / 0.85f;
        for (int i = 0; i < 2; i++)
        {
            float rise = Math.Clamp(k * 1.3f - i * 0.3f, 0f, 1f);
            var at = c.ToFeet + Vector3.UnitY * c.ToSize * (0.1f + 0.9f * rise);
            float r = (c.ToHalfWidth + 0.2f * c.ToSize) * (1.1f - 0.25f * rise);
            fx.Flat(FxShape.Ring, at, HaloTilt, r, Fx.Fade(col.Main, 0.8f * alpha * (1f - 0.5f * rise)), FxBlend.Alpha, depth: false);
        }
        for (int i = 0; i < 4; i++)
        {
            float a = (i / 4f + 0.1f * Fx.Signed(c.Seed, i, 281)) * MathF.Tau;
            float r = c.ToHalfWidth + 0.1f * c.ToSize;
            var at = c.ToFeet + new Vector3(MathF.Cos(a) * r, c.ToSize * (0.2f + 0.9f * k + 0.15f * Fx.Rand(c.Seed, i, 282)), 0.3f * c.ToSize);
            fx.Sprite(FxShape.Glow, at, c.ToSize * 0.12f, Fx.Fade(col.Light, 0.9f * alpha), FxBlend.Alpha);
        }
    }

    /// <summary>Spiky Shield: green thorns standing up round its user's feet as the shield goes up, and gone.</summary>
    private static void Thorns(in FxCtx c, FxColors col, FxList fx)
    {
        float k = After(c, 0f, 0.75f);
        if (k < 0f) return;
        float up = Fx.EaseOut(Math.Min(1f, k * 3f));
        float alpha = k < 0.7f ? 1f : (1f - k) / 0.3f;
        for (int i = 0; i < 7; i++)
        {
            float a = (i / 7f) * MathF.PI + 0.1f * Fx.Signed(c.Seed, i, 291);
            float r = c.ToHalfWidth + 0.15f * c.ToSize;
            var at = c.ToFeet + new Vector3(-MathF.Cos(a) * r, c.ToSize * 0.12f * up, MathF.Sin(a) * r * 0.6f + 0.15f * c.ToSize);
            var tint = i % 2 == 0 ? Rgb(96, 170, 64) : Rgb(150, 210, 96);
            fx.Sprite(FxShape.Shard, at, c.ToSize * 0.14f * up, Fx.Fade(tint, 0.95f * alpha), FxBlend.Alpha, Fx.Signed(c.Seed, i, 292) * 0.4f);
        }
    }

    /// <summary>
    /// Cracks in a ring (Earthquake, Thousand Arrows, Thousand Waves, Dig, Headlong Rush): four dark cracks spread to both
    /// sides of the target and before it, past its half width, so the punch-in sees them round the body, not behind it.
    /// </summary>
    private static void RingCracks(in FxCtx c, FxColors col, FxList fx)
    {
        float k = After(c, 0f, 0.9f);
        if (k < 0f) return;
        float alpha = k < 0.1f ? k / 0.1f : 1f - (k - 0.1f) / 0.9f;
        float r = c.ToHalfWidth + 0.35f * c.ToSize;
        for (int i = 0; i < 4; i++)
        {
            // From the left side through the front to the right side: 200° to 340° of the circle, the lens at 270°
            float a = MathF.PI * (1.1f + 0.27f * i) + 0.08f * Fx.Signed(c.Seed, i, 301);
            var at = c.ToFeet + new Vector3(MathF.Cos(a) * r, 0.02f, -MathF.Sin(a) * r * 0.7f + 0.1f * c.ToSize);
            fx.Flat(FxShape.Crack, at, GroundToLens, c.ToSize * 0.5f, Fx.Fade(Rgb(52, 40, 30), 0.9f * alpha), FxBlend.Alpha, Fx.Rand(c.Seed, i, 302) * MathF.Tau);
        }
    }

    /// <summary>Sacred Sword's blades: shards of pale steel, larger than rock's, falling round the target with a white glint each.</summary>
    private static void Blades(in FxCtx c, FxColors col, FxList fx)
    {
        float k = After(c, 0.03f, 0.8f);
        if (k < 0f) return;
        float alpha = k < 0.75f ? 1f : (1f - k) / 0.25f;
        for (int i = 0; i < 5; i++)
        {
            float x = Fx.Signed(c.Seed, i, 311) * (c.ToHalfWidth + 0.3f * c.ToSize);
            float fall = 1f - Fx.EaseOut(Math.Min(1f, k * 1.6f + Fx.Rand(c.Seed, i, 312) * 0.15f));
            var at = c.ToFeet + new Vector3(x, c.ToSize * (0.06f + 0.6f * fall), 0.35f * c.ToSize);
            float spin = c.Age * 5f * (i % 2 == 0 ? 1f : -1f) + i;
            fx.Sprite(FxShape.Shard, at, c.ToSize * 0.18f, Fx.Fade(Rgb(206, 220, 240), alpha), FxBlend.Alpha, spin);
            if (i < 4) fx.Sprite(FxShape.Star, at, c.ToSize * 0.08f, Fx.Fade(Rgb(255, 255, 255), 0.8f * alpha * (1f - k)), FxBlend.Add, spin);
        }
    }
}
