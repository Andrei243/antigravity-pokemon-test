using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// The species of our own that evolve past level 100 (plan 03 Â· D14), batch 1: Kanto's, in our numbers' order
// (2000 + the forebear's National number). Each is sculpted from its forebear's own sculpt as the look of
// docs/plans/D14-proposals-kanto.md says: the forebear's build is taken whole where the body stays (renamed,
// stretched, its colours changed, a part taken away) and the new features are added to it, or, where the body
// changes its plan, it is built anew from the forebear's parts and colours. The forebears' own sculpts are never
// touched.
internal static partial class PokemonModels
{
    /// <summary>The species of our own of batch 1 with a sculpt of their own, by name in capitals (null for any other).</summary>
    private static PokeBuilder? CreateOurs(string species) => species.ToUpperInvariant() switch
    {
        "RATCHISEL" => Ratchisel(),
        "LANCEROW" => Lancerow(),
        "NAJAROK" => Najarok(),
        "SANDSPIRE" => Sandspire(),
        "KITSUNOX" => Kitsunox(),
        "CORDYHUSK" => Cordyhusk(),
        "SOMNIMOTH" => Somnimoth(),
        "MOLEMOUNT" => Molemount(),
        "REGALYNX" => Regalynx(),
        "PSYDUKE" => Psyduke(),
        "KIRININE" => Kirinine(),
        "MEDUSARCH" => Medusarch(),
        "SOLARION" => Solarion(),
        "SLOWMANTLE" => Slowmantle(),
        "PORRIGARD" => Porrigard(),
        "TRIPLUMA" => Tripluma(),
        "GLACITUSK" => Glacitusk(),
        "SLUDGEMIRE" => Sludgemire(),
        "CLAMSPIRE" => Clamspire(),
        "MESMERYX" => Mesmeryx(),
        "PINCEREX" => Pincerex(),
        "DYNAMORB" => Dynamorb(),
        "PALMAGUS" => Palmagus(),
        "OSSUGARD" => Ossugard(),
        "SAVATEUR" => Savateur(),
        "PUGILORD" => Pugilord(),
        "SMOGLOM" => Smoglom(),
        "MATRIROO" => Matriroo(),
        "SABREFIN" => Sabrefin(),
        "STELLAMIR" => Stellamir(),
        "MIMESTRO" => Mimestro(),
        "AURORELLE" => Aurorelle(),
        "MANDIBRON" => Mandibron(),
        "BOVARCH" => Bovarch(),
        "TYPHONARCH" => Typhonarch(),
        "FERRISLE" => Ferrisle(),
        "NAIADEON" => Naiadeon(),
        "FULGUREON" => Fulgureon(),
        "PYREON" => Pyreon(),
        "AMMONARCH" => Ammonarch(),
        "EURYPTEROX" => Eurypterox(),
        "AZHDARCH" => Azhdarch(),
        "DOZEMOUNT" => Dozemount(),
        "GLACIRUNE" => Glacirune(),
        "FULGURIS" => Fulguris(),
        "PYREVANT" => Pyrevant(),
        // Batch 3, Hoenn (PokemonModels.Ours.Hoenn.cs)
        "HYAENARCH" => Hyaenarch(),
        "STREAKOON" => Streakoon(),
        "SWIFTARROW" => Swiftarrow(),
        "PELICAPTAIN" => Pelicaptain(),
        "OCELLWING" => Ocellwing(),
        "SPOREPUGIL" => Sporepugil(),
        "SHINOBUZZ" => Shinobuzz(),
        "YOKOZAN" => Yokozan(),
        "FELINELLE" => Felinelle(),
        "GEMWRAITH" => Gemwraith(),
        "DECEIVORE" => Deceivore(),
        "ASCETRANCE" => Ascetrance(),
        "FULMENHOUND" => Fulmenhound(),
        "ANODION" => Anodion(),
        "CATHODION" => Cathodion(),
        "PHOTURION" => Photurion(),
        "LAMPYRIA" => Lampyria(),
        "GULPGORGE" => Gulpgorge(),
        "MEGALODAX" => Megalodax(),
        "LEVIAWHALE" => Leviawhale(),
        "VOLCAMEL" => Volcamel(),
        "SMELTORTOISE" => Smeltortoise(),
        "BOARCANE" => Boarcane(),
        "WHIRLPANDA" => Whirlpanda(),
        "SAGUAROX" => Saguarox(),
        "NIMBUSONG" => Nimbusong(),
        "RAVAGOOSE" => Ravagoose(),
        "SCIMITAIL" => Scimitail(),
        "SELENOLITH" => Selenolith(),
        "HELIOLITH" => Heliolith(),
        "NAMAZURO" => Namazuro(),
        "CRAYRONIN" => Crayronin(),
        "DOGUNARCH" => Dogunarch(),
        "CRINOLITH" => Crinolith(),
        "ANOMALOREX" => Anomalorex(),
        "PRISMARIA" => Prismaria(),
        "CHROMELEON" => Chromeleon(),
        "EFFIGRIM" => Effigrim(),
        "MUSADON" => Musadon(),
        "CARILLONE" => Carillone(),
        "OMENBLADE" => Omenblade(),
        "BERGMAW" => Bergmaw(),
        "GULPEREEL" => Gulpereel(),
        "SIRENYSS" => Sirenyss(),
        "AEONFIN" => Aeonfin(),
        "HEARTREEF" => Heartreef(),
        "REGIMONOLITH" => Regimonolith(),
        "REGIGLACIER" => Regiglacier(),
        "REGIFERRUM" => Regiferrum(),
        "AURELIAS" => Aurelias(),
        "AURELIOS" => Aurelios(),
        // Batch 5, Unova (PokemonModels.Ours.Unova.cs)
        "BEACONWATCH" => Beaconwatch(),
        "UMBRAPARD" => Umbrapard(),
        "SIMISYLVAN" => Simisylvan(),
        "SIMISCORCH" => Simiscorch(),
        "SIMITORRENT" => Simitorrent(),
        "SOMNARNA" => Somnarna(),
        "STORMSTRIKA" => Stormstrika(),
        "AMORABAT" => Amorabat(),
        "EXCAVARCH" => Excavarch(),
        "REMEDINO" => Remedino(),
        "THROJUTSU" => Throjutsu(),
        "SAWKENDO" => Sawkendo(),
        "COTTONIMBUS" => Cottonimbus(),
        "BALLERILLY" => Ballerilly(),
        "BASCURAGE" => Bascurage(),
        "DARUMANDALA" => Darumandala(),
        "MARIACHACTUS" => Mariachactus(),
        "CRUSTADEL" => Crustadel(),
        "BRAWLHOOD" => Brawlhood(),
        "GLYPHORACLE" => Glyphoracle(),
        "PHARAURUM" => Pharaurum(),
        "TORTUGARCH" => Tortugarch(),
        "PRIMAVIAN" => Primavian(),
        "SCRAPODOR" => Scrapodor(),
        "ZOROMIRAGE" => Zoromirage(),
        "CHINCHILLUX" => Chinchillux(),
        "CYGNELLE" => Cygnelle(),
        "SEASONSTAG" => Seasonstag(),
        "NIMBOLGA" => Nimbolga(),
        "LANCELIER" => Lancelier(),
        "SPOREMONARCH" => Sporemonarch(),
        "JELLICROWN" => Jellicrown(),
        "ALOMATRON" => Alomatron(),
        "VOLTARANTULA" => Voltarantula(),
        "FERROSPIRE" => Ferrospire(),
        "COSMOGREY" => Cosmogrey(),
        "BEARTUNDRA" => Beartundra(),
        "CRYOHEDRON" => Cryohedron(),
        "VELOCIGOR" => Velocigor(),
        "FLUXFLOUNDER" => Fluxflounder(),
        "MIENSHIFU" => Mienshifu(),
        "DRUDRAKON" => Drudrakon(),
        "COLOSSURK" => Colossurk(),
        "BOUFFALANX" => Bouffalanx(),
        "IMPERAVIA" => Imperavia(),
        "CARRIONQUEEN" => Carrionqueen(),
        "PYROVORE" => Pyrovore(),
        "FORMICARCH" => Formicarch(),
        "HELIOPTERA" => Helioptera(),
        "COBALTARCH" => Cobaltarch(),
        "TERRAKHAN" => Terrakhan(),
        "VIRIZENITH" => Virizenith(),
        // Batch 4, Sinnoh's (PokemonModels.Ours.Sinnoh.cs)
        "DAMBAREL" => Dambarel(),
        "MAESTRIKKET" => Maestrikket(),
        "CRANIALITH" => Cranialith(),
        "BASTIONSAUR" => Bastionsaur(),
        "NOCTURMOTH" => Nocturmoth(),
        "HIVEMPRESS" => Hivempress(),
        "VOLTGLIDER" => Voltglider(),
        "TIDESTOAT" => Tidestoat(),
        "SAKURABLOOM" => Sakurabloom(),
        "GASTROMIRE" => Gastromire(),
        "QUADRIPOM" => Quadripom(),
        "DIRIGEIST" => Dirigeist(),
        "LAPINOVA" => Lapinova(),
        "HEXMAGISTRA" => Hexmagistra(),
        "CORVICAPO" => Corvicapo(),
        "BRUTABBY" => Brutabby(),
        "MEPHITARCH" => Mephitarch(),
        "DOTAKURON" => Dotakuron(),
        "CANTATOT" => Cantatot(),
        "OSSUARION" => Ossuarion(),
        "AURASAGE" => Aurasage(),
        "BEHEMOWDON" => Behemowdon(),
        "DREADPION" => Dreadpion(),
        "VENOMSENSEI" => Venomsensei(),
        "FLYTRAPEX" => Flytrapex(),
        "LUMIFATHOM" => Lumifathom(),
        "GLACIARBOR" => Glaciarbor(),
        "RIMERIPPER" => Rimeripper(),
        "LICKOLOSSUS" => Lickolossus(),
        "TANGLEWOOD" => Tanglewood(),
        "MEGANEURON" => Meganeuron(),
        "DRYADEON" => Dryadeon(),
        "BOREALEON" => Borealeon(),
        "FANGLISCOR" => Fangliscor(),
        "LODESTARCH" => Lodestarch(),
        "FROSTBRIDE" => Frostbride(),
        "UXINOUS" => Uxinous(),
        "MESPATHOS" => Mespathos(),
        "AZELTHYMOS" => Azelthymos(),
        // Batch 2, Johto (PokemonModels.Ours.Johto.cs)
        "ERMILONG" => Ermilong(),
        "NOCTAUGUR" => Noctaugur(),
        "COCCINOVA" => Coccinova(),
        "ORBWEAVIX" => Orbweavix(),
        "ABYSSOLUX" => Abyssolux(),
        "TOTEMSIGHT" => Totemsight(),
        "MIMICEDAR" => Mimicedar(),
        "HELIANTHRA" => Helianthra(),
        "MIRELORD" => Mirelord(),
        "ZENITHEON" => Zenitheon(),
        "ECLIPSEON" => Eclipseon(),
        "SLOWMONARCH" => Slowmonarch(),
        "WOBBULWARK" => Wobbulwark(),
        "PINECITADEL" => Pinecitadel(),
        "IRONWYRM" => Ironwyrm(),
        "MASTIFFANG" => Mastiffang(),
        "FUGUMACE" => Fugumace(),
        "GUILLOTYNE" => Guillotyne(),
        "FERMENTOISE" => Fermentoise(),
        "ATLASCROSS" => Atlascross(),
        "CALDERON" => Calderon(),
        "REEFCROWN" => Reefcrown(),
        "KRAKANNON" => Krakannon(),
        "SLEIGHWING" => Sleighwing(),
        "RAYSAIL" => Raysail(),
        "RAPIERWING" => Rapierwing(),
        "CERBERHOUND" => Cerberhound(),
        "SIEGEPHANT" => Siegephant(),
        "MURALIST" => Muralist(),
        "GYRODRILL" => Gyrodrill(),
        "PASTORINE" => Pastorine(),
        "RAITENKO" => Raitenko(),
        "ENKAIOU" => Enkaiou(),
        "SUISEIREN" => Suiseiren(),
        // Batch 6 (Kalos; PokemonModels.Ours.Kalos.cs)
        "EXCAVABBIT" => Excavabbit(),
        "BLAZEMONARCH" => Blazemonarch(),
        "ALPICORN" => Alpicorn(),
        "BAMBRAWLER" => Bambrawler(),
        "MAJESTHOUND" => Majesthound(),
        "OCCULYNX" => Occulynx(),
        "PERFUMELLE" => Perfumelle(),
        "SOUFFLAIR" => Soufflair(),
        "MESMERQUID" => Mesmerquid(),
        "REEFCOLONY" => Reefcolony(),
        "KELPDRAKE" => Kelpdrake(),
        "SALVOCLAW" => Salvoclaw(),
        "CORONAFRILL" => Coronafrill(),
        "DESPOTITAN" => Despotitan(),
        "BOREALOSAUR" => Borealosaur(),
        "FAERIEON" => Faerieon(),
        "LUCHACONDOR" => Luchacondor(),
        "SIGNALMUS" => Signalmus(),
        "DRUZARION" => Druzarion(),
        "CHATELKEY" => Chatelkey(),
        "SYLVANSHADE" => Sylvanshade(),
        "LANTERNHUSK" => Lanternhusk(),
        "GLACITADEL" => Glacitadel(),
        "SONARWING" => Sonarwing(),
        "ECOSENTINEL" => Ecosentinel(),
        // Batch 7 (Alola; PokemonModels.Ours.Alola.cs)
        "INSPECTOOSE" => Inspectoose(),
        "GLACIPINCER" => Glacipincer(),
        "POLLENOBLE" => Pollenoble(),
        "LUPINACLE" => Lupinacle(),
        "VENOMREEF" => Venomreef(),
        "CLAYSTEED" => Claysteed(),
        "DIVEWEAVER" => Diveweaver(),
        "ORCHIMANTIS" => Orchimantis(),
        "LUMISHROOM" => Lumishroom(),
        "SMOLDERESS" => Smolderess(),
        "HUGGRIZZLE" => Huggrizzle(),
        "WREATHFEY" => Wreathfey(),
        "ARBORSAGE" => Arborsage(),
        "SCRUMMIAN" => Scrummian(),
        "SHOGUNPOD" => Shogunpod(),
        "SANDCITADEL" => Sandcitadel(),
        "CUCUMBRAWL" => Cucumbrawl(),
        "SOMNIKOALA" => Somnikoala(),
        "CALDERTOISE" => Caldertoise(),
        "ROLLOCUPINE" => Rollocupine(),
        "MASQUERADOLL" => Masqueradoll(),
        "CHROMAGNASH" => Chromagnash(),
        "ELDERDRAKE" => Elderdrake(),
        "GALLEONGHAST" => Galleonghast(),
        "KOKONUI" => Kokonui(),
        "LELENUI" => Lelenui(),
        "BULUNUI" => Bulunui(),
        "FININUI" => Fininui(),
        "NIHILIAD" => Nihiliad(),
        "BUZZCOLOSSUS" => Buzzcolossus(),
        "PHEROMUSE" => Pheromuse(),
        "XURKIGROVE" => Xurkigrove(),
        "CELESTRATA" => Celestrata(),
        "ORIGAMURAI" => Origamurai(),
        "DEVOURMAW" => Devourmaw(),
        "NAGANAPEX" => Naganapex(),
        "ZIGGURAK" => Ziggurak(),
        "PYROCEPHALON" => Pyrocephalon(),
        _ => null
    };

    // ------------------------------------------------------------------ building on a forebear's sculpt

    /// <summary>
    /// Takes a forebear's finished build as the start of one of ours: renamed (the model and its description), and
    /// filling <paramref name="fill"/> of the sprite frame.
    /// </summary>
    private static PokeBuilder Become(PokeBuilder b, string species, float fill)
    {
        b.Model.Species = species;
        b.Model.Fill = fill;
        b.Sdf.Name = "pokemon " + species;
        return b;
    }

    /// <summary>Changes every shape and mark of one colour to another (exact colours, as the forebear's code names them).</summary>
    private static void Recolour(PokeBuilder b, params (Color from, Color to)[] map)
    {
        static bool Same(Color a, Color c) => a.R == c.R && a.G == c.G && a.B == c.B;
        foreach (var s in b.Sdf.Shapes)
            foreach (var (from, to) in map)
                if (Same(s.Color, from)) { s.Color = to; break; }
        foreach (var d in b.Model.Decals)
            foreach (var (from, to) in map)
            {
                if (Same(d.Color, from)) d.Color = to;
                if (d.Iris is { } iris && Same(iris, from)) d.Iris = to;
            }
        b.Sdf.Changed();
    }

    /// <summary>
    /// Stretches the whole sculpt about the origin by <paramref name="k"/> (taller and leaner, longer and lower):
    /// every shape's place and size, the joints and the marks. A shape turned askew is sized by how much each of its
    /// own axes is stretched, which is near enough for the soft shapes of a sculpt.
    /// </summary>
    private static void Stretch(PokeBuilder b, Vector3 k)
    {
        Grow(b, k, Vector3.Zero, (SdfPrimitive _) => true, _ => true);
        b.Model.Skeleton.Scale(k);
    }

    /// <summary>
    /// Stretches the shapes of the bones <paramref name="bones"/> says, and their marks, by <paramref name="k"/> about
    /// <paramref name="about"/> (a wing broadened from its shoulder, a shell grown round its middle); the joints stay.
    /// </summary>
    private static void Grow(PokeBuilder b, Vector3 k, Vector3 about, Func<int, bool> bones) =>
        Grow(b, k, about, s => bones(s.Bone), bones);

    /// <summary>Stretches the shapes <paramref name="which"/> picks by <paramref name="k"/> about <paramref name="about"/>, and the marks of the bones <paramref name="marks"/> picks.</summary>
    private static void Grow(PokeBuilder b, Vector3 k, Vector3 about, Func<SdfPrimitive, bool> which, Func<int, bool> marks)
    {
        float Along(Vector3 axis) => (k * Vector3.Normalize(axis)).Length();
        float Across(Vector3 axis)
        {
            var d = Vector3.Normalize(axis);
            return (k.X * (1f - d.X * d.X) + k.Y * (1f - d.Y * d.Y) + k.Z * (1f - d.Z * d.Z)) / 2f;
        }
        float mean = (k.X + k.Y + k.Z) / 3f;
        foreach (var s in b.Sdf.Shapes)
        {
            if (!which(s)) continue;
            var q = s.Rotation;
            var ax = Vector3.Transform(Vector3.UnitX, q);
            var ay = Vector3.Transform(Vector3.UnitY, q);
            var az = Vector3.Transform(Vector3.UnitZ, q);
            switch (s.Shape)
            {
                case SdfShape.RoundCone:
                    float across = s.B == s.A ? mean : Across(s.B - s.A);
                    s.Size = new Vector3(s.Size.X * across, s.Size.Y * across, s.Size.Z);
                    break;
                case SdfShape.Sphere:
                    s.Size = new Vector3(s.Size.X * mean, s.Size.Y, s.Size.Z);
                    break;
                case SdfShape.Torus:
                    s.Size = new Vector3(s.Size.X * Across(ay), s.Size.Y * mean, s.Size.Z);
                    break;
                case SdfShape.Cylinder:
                    s.Size = new Vector3(s.Size.X * Across(ay), s.Size.Y * Along(ay), s.Size.Z);
                    break;
                default:
                    s.Size = new Vector3(s.Size.X * Along(ax), s.Size.Y * Along(ay), s.Size.Z * Along(az));
                    break;
            }
            s.A = about + (s.A - about) * k;
            s.B = about + (s.B - about) * k;
            s.Blend *= mean;
        }
        b.Sdf.Changed();
        foreach (var d in b.Model.Decals)
        {
            if (!marks(d.Bone)) continue;
            d.Center = about + (d.Center - about) * k;
            d.Normal = Vector3.Normalize(d.Normal / k);
        }
    }

    /// <summary>Moves the shapes and marks of the bones <paramref name="bones"/> picks by <paramref name="d"/> (a head carried up on a longer neck); the joints stay.</summary>
    private static void Move(PokeBuilder b, Func<int, bool> bones, Vector3 d)
    {
        foreach (var s in b.Sdf.Shapes)
            if (bones(s.Bone))
            {
                s.A += d;
                s.B += d;
            }
        b.Sdf.Changed();
        foreach (var m in b.Model.Decals)
            if (bones(m.Bone)) m.Center += d;
    }

    /// <summary>Paints a colour and a material together over the surface of <paramref name="bone"/> inside an ellipsoid (steel over a claw).</summary>
    private static void PaintAs(PokeBuilder b, int bone, Vector3 center, Vector3 radii, Color color, SurfaceMaterial mat, Vector3 rotationDeg = default)
    {
        var rot = Quaternion.CreateFromYawPitchRoll(rotationDeg.Y * Degree, rotationDeg.X * Degree, rotationDeg.Z * Degree);
        b.Sdf.Paint(b.Sdf.Ellipsoid(center, radii, color, bone, PokeBuilder.PaintEdge, mat, rot), true, bone);
    }

    /// <summary>A bone of the build by its name (it must be there).</summary>
    private static int BoneOf(PokeBuilder b, string name)
    {
        int i = b.Model.Skeleton.Find(name);
        if (i < 0) throw new InvalidOperationException($"{b.Model.Species} has no bone {name}");
        return i;
    }

    private static readonly Color Steel = Rgb(170, 176, 188);

    // ------------------------------------------------------------------ 2020 Ratchisel, from Raticate

    /// <summary>
    /// Ratchisel: Raticate down on all fours, lower and longer, its brown fur grizzled grey along the back, its
    /// whiskers kept, its buck teeth grown into square chisels of steel and its scaly tail thickened with three
    /// steel rings.
    /// </summary>
    /// <param name="female">A female keeps Raticate's: shorter whiskers.</param>
    private static PokeBuilder Ratchisel(bool female = false)
    {
        var b = new PokeBuilder("Ratchisel", 0.84f, BodyPlan.Quadruped, V(0, 0.25f, -0.04f)) { Coat = Fur };
        var fur = Rgb(150, 116, 86);
        var grey = Rgb(138, 136, 132);
        var cream = Rgb(184, 150, 116);
        var tailColor = Rgb(170, 134, 98);
        foreach (var (z, front) in new[] { (0.14f, true), (-0.22f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.1f * s, 0.2f, z), front);
                if (front) b.Limb(leg, V(0.1f * s, 0.22f, z), V(0.11f * s, 0.05f, z + 0.02f), 0.05f, 0.036f, fur);
                else
                {
                    // Haunches as heavy as Raticate's, folded over the hind feet
                    b.Ell(leg, V(0.11f * s, 0.17f, z), V(0.07f, 0.1f, 0.1f), fur);
                    b.Limb(leg, V(0.11f * s, 0.12f, z - 0.02f), V(0.115f * s, 0.05f, z + 0.01f), 0.04f, 0.034f, fur);
                }
                b.Ell(leg, V(0.115f * s, 0.024f, z + 0.05f), V(0.042f, 0.024f, 0.07f), PixelCanvas.Shadow(fur, 0.15f));
                Claws(b, leg, V(0.115f * s, 0.02f, z + 0.11f), V(0.014f, 0, 0), V(0, -0.2f, 1f), 0.024f, 0.008f);
            });
        b.Ell(Body, V(0, 0.25f, -0.04f), V(0.16f, 0.14f, 0.28f), fur);
        b.PaintEll(Body, V(0, 0.16f, 0.0f), V(0.12f, 0.07f, 0.22f), cream);
        // Grizzled grey shag over the whole of its back, from the crown of its head to its rump
        b.PaintEll(Body, V(0, 0.36f, -0.06f), V(0.15f, 0.08f, 0.3f), grey);
        Patches(b, Body, grey, (V(0.12f, 0.31f, 0.1f), 0.04f), (V(-0.13f, 0.3f, -0.04f), 0.04f), (V(0.13f, 0.3f, -0.18f), 0.035f), (V(-0.1f, 0.32f, -0.2f), 0.035f));
        for (int i = 0; i < 9; i++)
        {
            float z = 0.14f - i * 0.05f;
            float x = (i % 3 - 1) * 0.05f;
            b.Spike(Body, V(x, 0.37f, z), V(x * 1.3f, 0.42f, z - 0.05f), 0.026f, grey, 0.5f, blend: 0.012f);
        }
        // A long thick tail lying out behind and curling to a fine point, banded with four rings of steel
        int tail = b.Tail(V(0, 0.2f, -0.3f));
        var path = Smooth(3, V(0, 0.2f, -0.3f), V(0.05f, 0.1f, -0.46f), V(0.18f, 0.05f, -0.6f), V(0.34f, 0.06f, -0.66f), V(0.46f, 0.1f, -0.6f), V(0.5f, 0.06f, -0.48f));
        b.Tube(tail, path, 0.05f, 0.012f, tailColor, blend: 0f);
        for (int i = 2; i < path.Length - 2; i += 2)
            b.PaintTorus(tail, path[i], 0.046f - 0.0024f * i, 0.005f, PixelCanvas.Shadow(tailColor, 0.2f), Euler(path[i + 1] - path[i - 1]));
        foreach (int i in new[] { 3, 5, 7, 9 })
        {
            float t = i / (float)(path.Length - 1);
            b.Torus(tail, path[i], 0.05f - 0.038f * t + 0.004f, 0.012f, Steel, Euler(path[i + 1] - path[i - 1]), mat: Metal, blend: 0.004f);
        }
        b.Limb(Body, V(0, 0.3f, 0.16f), V(0, 0.32f, 0.24f), 0.09f, 0.08f, fur);
        int head = b.Head(V(0, 0.32f, 0.22f));
        var c = V(0, 0.36f, 0.3f);
        var r = V(0.12f, 0.105f, 0.12f);
        b.Ell(head, c, r, fur);
        b.PaintEll(head, V(0, 0.4f, 0.24f), V(0.08f, 0.05f, 0.1f), grey);
        b.Ell(head, V(0, 0.32f, 0.4f), V(0.075f, 0.06f, 0.075f), fur);
        b.PaintEll(head, V(0, 0.3f, 0.42f), V(0.07f, 0.045f, 0.06f), cream);
        b.Ell(head, V(0, 0.345f, 0.47f), V(0.022f, 0.018f, 0.016f), Rgb(60, 40, 40), blend: 0.006f);
        // The chisels: two broad teeth of steel, cut square at their ends
        PokeBuilder.Both(s => b.Box(head, V(0.024f * s, 0.22f, 0.45f), V(0.022f, 0.07f, 0.012f), 0.003f, Steel, V(-6f, 0, 0), Metal, 0.004f));
        Whiskers(b, head, V(0.05f, 0.32f, 0.44f), female ? 0.1f : 0.17f, Rgb(70, 60, 54), 0.006f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.08f * s, 0.43f, 0.24f));
            b.Ell(ear, V(0.1f * s, 0.455f, 0.235f), V(0.032f, 0.034f, 0.016f), fur, V(0, -10f * s, -30f * s));
            b.PaintEll(ear, V(0.1f * s, 0.455f, 0.245f), V(0.02f, 0.022f, 0.012f), PixelCanvas.Shadow(fur, 0.3f), V(0, -10f * s, -30f * s));
        });
        foreach (var (x, h) in new[] { (-0.03f, 0.04f), (0.0f, 0.05f), (0.03f, 0.04f) })
            b.Spike(head, V(x, 0.44f, 0.22f), V(x * 1.4f, 0.44f + h, 0.15f), 0.024f, grey, 0.5f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.055f * s, 0.39f), V(0.55f * s, 0.15f, 1f), 0.026f, glare: true));
        return b;
    }

    // ------------------------------------------------------------------ 2038 Kitsunox, from Ninetales

    /// <summary>
    /// Kitsunox: Ninetales, its gold paled to cream, its nine tails longer and wispier, each ending in a pale violet
    /// flame, its ruff grown into a mantle and a small pale flame floating at its brow on a thin strand.
    /// </summary>
    private static PokeBuilder Kitsunox()
    {
        var b = Become(NinetalesBuild(false), "Kitsunox", 0.96f);
        var cream = Rgb(244, 216, 146);
        var tailCream = Rgb(242, 220, 160);
        var wispy = Rgb(222, 210, 240);
        var violet = Rgb(176, 136, 238);
        var pale = Rgb(240, 230, 255);
        var mane = Rgb(248, 232, 184);
        Recolour(b, (Rgb(246, 228, 164), cream), (Rgb(248, 232, 170), tailCream), (Rgb(246, 156, 72), wispy),
            (Rgb(252, 246, 222), mane), (Rgb(214, 50, 56), Rgb(150, 92, 220)));
        // Each tail runs on past its end, thinning to a wisp, and burns at its tip in pale violet
        int tail = BoneOf(b, "tail");
        for (int i = 0; i < 9; i++)
        {
            float a = (-72f + 18f * i) * Degree;
            var flat = Vector3.Normalize(V(MathF.Sin(a), 0, -MathF.Cos(a) * 0.9f));
            var root = V(0, 0.41f, -0.22f);
            float len = 0.5f - 0.08f * MathF.Abs(MathF.Sin(a));
            var end = root + flat * len + V(0, 0.04f, 0);
            var wisp = end + flat * 0.12f + V(0, 0.18f, 0);
            b.Tube(tail, Smooth(3, end - flat * 0.04f, end + flat * 0.08f + V(0, 0.05f, 0), wisp), 0.034f, 0.016f, wispy, blend: 0f);
            FlameTongue(b, tail, wisp - flat * 0.01f, wisp + flat * 0.05f + V(0, 0.09f, 0), 0.026f, violet, pale);
        }
        // The ruff grown into a mantle over the shoulders, falling in flame-like locks down the breast and back
        b.Ell(Body, V(0, 0.45f, 0.07f), V(0.13f, 0.075f, 0.12f), mane, blend: 0.03f);
        PokeBuilder.Both(s => b.Ell(Body, V(0.09f * s, 0.4f, 0.1f), V(0.06f, 0.09f, 0.07f), mane, blend: 0.03f));
        foreach (float x in new[] { -0.06f, -0.02f, 0.02f, 0.06f })
            b.Spike(Body, V(x, 0.36f, 0.17f), V(x * 1.3f, 0.22f, 0.2f), 0.026f, mane, 0.5f);
        PokeBuilder.Both(s =>
        {
            foreach (var (y, z) in new[] { (0.5f, 0.02f), (0.46f, -0.06f), (0.42f, -0.13f) })
            {
                var root = V(0.09f * s, y, z);
                b.Tube(Body, Smooth(3, root, root + V(0.06f * s, 0.06f, -0.06f), root + V(0.08f * s, 0.12f, -0.14f)), 0.026f, 0.008f, mane, blend: 0.01f);
            }
        });
        // A pale flame at the brow on a thin strand
        int head = BoneOf(b, "head");
        b.Tube(head, Smooth(3, V(0, 0.665f, 0.215f), V(0, 0.72f, 0.26f), V(0, 0.77f, 0.25f)), 0.007f, 0.005f, cream, blend: 0.004f);
        FlameTongue(b, head, V(0, 0.765f, 0.25f), V(0, 0.84f, 0.24f), 0.022f, violet, pale);
        return b;
    }

    // ------------------------------------------------------------------ 2083 Porrigard, from Farfetch'd

    /// <summary>
    /// Porrigard: Farfetch'd grown taller and leaner, a cape of white feathers over its shoulders, a few grey
    /// feathers in its brows, and its leek grown into a long staff tipped with a bundle of green leaves.
    /// </summary>
    private static PokeBuilder Porrigard()
    {
        var b = Become(FarfetchdBuild(false), "Porrigard", 0.86f);
        // The leek goes: the staff takes its place in the same wing
        int grip = BoneOf(b, "wingL");
        var leekWhite = Rgb(242, 244, 232);
        var leekGreen = Rgb(96, 170, 72);
        b.Sdf.RemoveAll(s => s.Bone == grip && (s.Color.Equals(leekWhite) || s.Color.Equals(leekGreen)));
        Stretch(b, V(0.94f, 1.14f, 0.95f));
        var white = Rgb(246, 244, 238);
        var green = Rgb(80, 156, 64);
        // The staff, from the ground beside its foot up past its head, a sage stalk greening toward its top, bound
        // with a cord under its bundle of leaves
        var foot = V(-0.12f, 0.016f, 0.13f);
        var top = V(-0.19f, 0.86f, 0.0f);
        var d = top - foot;
        var up = Vector3.Normalize(d);
        // A great leek for a staff: a thick white stalk greening toward its top, a sheaf of long leaves fanning from it
        Recolour(b, (Rgb(178, 158, 140), Rgb(188, 166, 126)), (Rgb(116, 96, 84), Rgb(150, 128, 98)));
        b.Limb(grip, foot, foot + d * 0.62f, 0.028f, 0.027f, Rgb(238, 240, 224), Leaf, 0.008f);
        b.Limb(grip, foot + d * 0.6f, top, 0.027f, 0.026f, Rgb(184, 214, 150), Leaf, 0.012f);
        var side = Vector3.Normalize(Vector3.Cross(d, V(0, 0, 1f)));
        foreach (var (t, z, len) in new[] { (-1.6f, 0.03f, 0.26f), (-1.1f, -0.04f, 0.32f), (-0.6f, 0.05f, 0.36f), (-0.2f, -0.05f, 0.38f), (0.2f, 0.04f, 0.37f), (0.6f, -0.03f, 0.34f), (1.1f, 0.05f, 0.3f), (1.6f, -0.02f, 0.25f), (0f, 0.07f, 0.33f) })
        {
            var root = top - up * 0.02f;
            var mid = root + up * len * 0.5f + side * t * 0.03f + V(0, 0, z * 0.5f);
            var tip = root + up * len + side * t * 0.1f + V(0, -MathF.Abs(t) * 0.04f, z);
            Frond(b, grip, root, mid, 0.026f, green, V(0, 0, 1f), 0.45f, Leaf);
            Frond(b, grip, mid - (mid - root) * 0.15f, tip, 0.024f, PixelCanvas.Shadow(green, 0.12f), V(0, 0, 1f), 0.45f, Leaf);
        }
        // A cape of white feathers over its shoulders and down its back, its edges showing in front
        b.Ell(Body, V(0, 0.46f, -0.04f), V(0.14f, 0.06f, 0.12f), white, V(14f, 0, 0), blend: 0.02f);
        PokeBuilder.Both(s => b.Ell(Body, V(0.1f * s, 0.43f, 0.03f), V(0.055f, 0.06f, 0.05f), white, blend: 0.02f));
        for (int i = 0; i < 5; i++)
        {
            float x = -0.08f + 0.04f * i;
            Blade(b, Body, V(x, 0.46f, -0.12f), V(x * 1.35f, 0.22f - MathF.Abs(x) * 0.4f, -0.2f), 0.05f, white, V(0, 0.2f, -1f), 0.25f);
        }
        // Grey feathers grown into its brows
        int head = BoneOf(b, "head");
        PokeBuilder.Both(s => b.Spike(head, V(0.05f * s, 0.685f, 0.05f), V(0.1f * s, 0.73f, 0.02f), 0.012f, Rgb(176, 176, 182), 0.5f));
        return b;
    }

    // ------------------------------------------------------------------ 2099 Pincerex, from Kingler

    /// <summary>
    /// Pincerex: Kingler with its great claw grown greater still and tempered to steel rimmed in red, the small claw
    /// a little bigger, its shell a darker red with spikes along its edge, the crown of five spikes on its head kept.
    /// </summary>
    private static PokeBuilder Pincerex()
    {
        var red = Rgb(124, 34, 44);
        var grey = Rgb(132, 138, 150);
        var spike = Rgb(140, 112, 116);
        var b = CrabBuild("Pincerex", 0.92f, 0.085f, 0.18f, 5, red, grey);
        // Legs and underside armoured in steel
        Recolour(b, (Rgb(214, 210, 204), grey));
        foreach (var s in b.Sdf.Shapes.Where(s => s.Color.Equals(grey))) s.Material = Metal;
        // A broader, flatter shell
        Grow(b, V(1.25f, 1f, 1.2f), V(0, 0.17f, 0), s => s.Bone == Body && s.Shape == SdfShape.Ellipsoid && s.Color.Equals(red) && s.Size.X > 0.1f, _ => false);
        // The great claw, steel with a red rim at the wrist and down its inner edge
        int arm = BoneOf(b, "armR");
        var elbow = V(0.22f, 0.24f, 0.08f);
        var d = Vector3.Normalize(V(0.15f, 1f, 0.35f));
        PaintAs(b, arm, elbow + d * 0.18f * 1.4f, V(0.2f, 0.26f, 0.2f), Steel, Metal, Euler(d));
        b.Torus(arm, elbow + d * 0.18f * 0.35f, 0.075f, 0.016f, red, Euler(d), mat: Shell, blend: 0.006f);
        b.Torus(arm, elbow + d * 0.18f * 0.85f, 0.11f, 0.012f, Rgb(100, 104, 116), Euler(d), mat: Metal, blend: 0.006f);
        // Spikes along the rim of its shell, behind and at the sides, and up its back
        for (int i = 0; i < 11; i++)
        {
            float a = (195f + 150f * i / 10f) * Degree;
            var at = V(0.2f * MathF.Cos(a), 0.18f, 0.144f * MathF.Sin(a));
            var outward = Vector3.Normalize(V(MathF.Cos(a), 0.35f, MathF.Sin(a) * 0.9f));
            b.Spike(Body, at - outward * 0.01f, at + outward * 0.07f, 0.017f, spike, mat: Shell);
        }
        foreach (var (x, z) in new[] { (-0.07f, -0.04f), (0.07f, -0.04f), (0f, -0.08f), (-0.1f, 0.02f), (0.1f, 0.02f) })
            b.Spike(Body, V(x, 0.235f, z), V(x * 1.3f, 0.31f, z - 0.03f), 0.016f, spike, mat: Shell);
        return b;
    }

    /// <summary>How far a forebear's build was moved after it was sculpted (a flier lifted off the ground): where the joint of <paramref name="bone"/> is, less where it was sculpted.</summary>
    private static Vector3 Shifted(PokeBuilder b, string bone, Vector3 sculptedAt) => b.Model.Skeleton[BoneOf(b, bone)].Joint - sculptedAt;

    /// <summary>The bone an Arbok-built coil's point <paramref name="i"/> lies on (a bone for every three points, <see cref="Coils"/>).</summary>
    private static int CoilBone(PokeBuilder b, int i, int per = 3) => i < per ? Body : BoneOf(b, "seg" + i / per);

    // ------------------------------------------------------------------ 2024 Najarok, from Arbok

    /// <summary>
    /// Najarok: Arbok longer and heavier, its purple deepened toward violet with thin gold bands down its body, its
    /// hood widened into a crowned frill, and the pattern on the hood woken into a second face whose eyes open and
    /// blink.
    /// </summary>
    private static PokeBuilder Najarok()
    {
        var b = Become(Arbok(), "Najarok", 0.98f);
        var violet = Rgb(86, 58, 128);
        var gold = Rgb(214, 176, 96);
        Recolour(b, (Rgb(150, 116, 184), violet), (Rgb(214, 198, 160), Rgb(118, 92, 164)), (Rgb(36, 30, 40), Rgb(52, 32, 78)));
        // Gold chevrons down the paler belly of its neck
        int neckBone = b.Sdf.Shapes.First(s => s.Color.Equals(Rgb(118, 92, 164))).Bone;
        for (int i = 0; i < 6; i++)
        {
            var at = V(0, 0.45f + i * 0.04f, 0.155f - i * 0.002f);
            PokeBuilder.Both(s => b.PaintEll(neckBone, at + V(0.022f * s, -0.008f, 0), V(0.026f, 0.005f, 0.03f), gold, V(0, 0, 28f * s)));
        }
        // A small crown of points on its head
        int skull = BoneOf(b, "head");
        foreach (var (x, h) in new[] { (-0.03f, 0.04f), (0f, 0.06f), (0.03f, 0.04f) })
            b.Spike(skull, V(x, 0.815f, 0.07f), V(x * 1.3f, 0.82f + h, 0.04f), 0.014f, violet, 0.5f);
        // Thin gold bands round the body, a pair of points apart, from the tail to the neck
        var coil = Spiral3(0.28f, 0.15f, 0.07f, 0.17f, 2.2f, 1.3f, 20);
        var points = new[] { V(0.34f, 0.05f, -0.36f), V(0.3f, 0.06f, -0.3f) }.Concat(coil)
            .Concat(new[] { V(0.0f, 0.28f, 0.08f), V(0, 0.44f, 0.1f), V(0, 0.58f, 0.08f), V(0, 0.72f, 0.06f) }).ToArray();
        for (int i = 3; i < points.Length - 3; i += 2)
        {
            float t = i / (float)(points.Length - 1);
            float radius = 0.022f + 0.05f * MathF.Min(1f, t * 2.5f);
            b.PaintTorus(CoilBone(b, i), points[i], radius, 0.007f, gold, Euler(points[i + 1] - points[i - 1]));
        }
        // The hood widens into a frill crowned with points
        int hood = BoneOf(b, "hood");
        var hoodAt = V(0, 0.6f, 0.04f);
        Grow(b, V(1.35f, 1.2f, 1f), hoodAt, bone => bone == hood);
        // Its rim burns into points like a dark flame, all the way round its sides and crown
        for (int i = 0; i < 13; i++)
        {
            float a = (-110f + 220f * i / 12f) * Degree;
            var rim = V(0, 0.62f, 0.02f) + V(MathF.Sin(a) * 0.25f, MathF.Cos(a) * 0.215f, 0);
            var outward = Vector3.Normalize(V(MathF.Sin(a), MathF.Cos(a) * 1.1f + 0.35f, -0.05f));
            b.Spike(hood, rim - outward * 0.025f, rim + outward * (i % 2 == 0 ? 0.11f : 0.07f), 0.03f, violet, 0.4f);
        }
        // The face on the hood wakes: its two rings become pale violet eyes with slit pupils that open and blink
        var rings = b.Model.Decals.Where(d => d.Bone == hood && d.Shape == MarkShape.Disc).ToList();
        var eyes = rings.Where(d => d.Color.R > 230).Select(d => (d.Center, d.Normal)).ToList();
        b.Model.Decals.RemoveAll(d => rings.Contains(d));
        foreach (var (at, normal) in eyes)
        {
            b.Mark(hood, at, normal, 0.07f, 0.05f, Rgb(46, 28, 70), MarkShape.Disc);
            b.Eye(hood, at, normal, 0.044f, sclera: true, white: Rgb(206, 186, 238), pupil: Rgb(40, 22, 62));
        }
        Stretch(b, V(1.08f, 1.06f, 1.08f));
        return b;
    }

    // ------------------------------------------------------------------ 2047 Cordyhusk, from Parasect

    /// <summary>
    /// Cordyhusk: Parasect after the mushroom has taken its host whole: the cap swollen into a broad parasol hanging
    /// over the body like a cloak, its spots glowing pale, threads of white mycelium hanging from its rim, and the bug
    /// beneath a pale hollow husk with its eyes painted out.
    /// </summary>
    private static PokeBuilder Cordyhusk()
    {
        var b = Become(Parasect(), "Cordyhusk", 0.88f);
        var husk = Rgb(210, 206, 192);
        var red = Rgb(164, 58, 52);
        var pale = Rgb(170, 240, 226);
        var thread = Rgb(236, 236, 228);
        Recolour(b, (Rgb(242, 150, 84), husk), (Rgb(214, 122, 66), Rgb(178, 172, 154)), (Rgb(210, 104, 150), red), (Rgb(246, 220, 186), Rgb(150, 92, 82)));
        // The spots glow
        foreach (var s in b.Sdf.Shapes.Where(s => s.Op == SdfOp.Paint && s.Color.Equals(Rgb(250, 202, 96))))
        {
            s.Color = pale;
            s.Material = Glow;
            s.PaintsMaterial = true;
        }
        // The cap swells into a parasol that hangs over the body
        int mushroom = BoneOf(b, "mushroom");
        var top = V(0, 0.37f, -0.08f);
        Grow(b, V(1.3f, 1.12f, 1.3f), top, bone => bone == mushroom);
        // Raised up over the husk on a pale stalk grown out of its back, so the parasol hangs above it
        var raise = V(0, 0.12f, 0.03f);
        Move(b, bone => bone == mushroom, raise);
        top += raise;
        b.Limb(mushroom, V(0, 0.22f, -0.1f), top + V(0, -0.04f, 0), 0.06f, 0.075f, Rgb(214, 206, 188), blend: 0.03f);
        var turn = V(-12f, 0, 0);
        var capR = V(0.27f, 0.17f, 0.29f) * V(1.3f, 1.12f, 1.3f);
        var q = Quaternion.CreateFromYawPitchRoll(0, turn.X * Degree, 0);
        // Its rim droops into a cloak
        b.Ell(mushroom, top + V(0, -0.06f, 0), V(capR.X * 1.02f, 0.08f, capR.Z * 1.02f), red, turn, blend: 0.03f);
        // Threads of mycelium hanging from the rim, all round but before its face
        for (int i = 0; i < 14; i++)
        {
            float a = (40f + i * (280f / 13f)) * Degree;
            var rim = top + Vector3.Transform(V(MathF.Sin(a) * capR.X * 0.97f, -0.11f, MathF.Cos(a) * capR.Z * 0.97f), q);
            float drop = 0.11f + 0.05f * ((i * 7) % 3);
            b.Tube(mushroom, new[] { rim + V(0, 0.02f, 0), rim - V(0, drop * 0.5f, 0) + V(MathF.Sin(i * 1.3f) * 0.01f, 0, 0), rim - V(0, drop, 0) }, 0.008f, 0.004f, thread, blend: 0.004f);
            b.Ell(mushroom, rim - V(0, drop, 0), V(0.008f, 0.01f, 0.008f), thread, blend: 0.003f);
        }
        // The husk's eyes painted out
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
        {
            d.White = Rgb(208, 194, 168);
            d.Pupil = Rgb(186, 172, 150);
        }
        return b;
    }

    // ------------------------------------------------------------------ 2078 Solarion, from Rapidash

    /// <summary>
    /// Solarion: Rapidash taller and more muscular, its horn longer and spiralled, two wings of flame spread from its
    /// shoulders, its mane and tail streaming further and flames at its hooves.
    /// </summary>
    private static PokeBuilder Solarion()
    {
        var b = Become(Horse(true), "Solarion", 0.98f);
        const float k = 1.12f;
        int head = BoneOf(b, "head");
        // A coat of burnished gold-bronze, dark hooves
        Recolour(b, (Rgb(252, 240, 192), Rgb(196, 140, 66)), (Rgb(140, 130, 170), Rgb(66, 44, 38)));
        // Heavier in the chest and the haunches
        Grow(b, V(1.12f, 1.1f, 1.04f), V(0, 0.52f, -0.02f), s => s.Bone == Body && s.Shape == SdfShape.Ellipsoid && s.Color.Equals(Rgb(196, 140, 66)), _ => false);
        // A longer horn, a band of gold spiralling up it
        var ivory = Rgb(250, 246, 236);
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.Equals(ivory));
        var hornFoot = V(0, 0.9f, 0.36f * k);
        var hornTip = V(0, 1.2f, 0.54f * k);
        b.Spike(head, hornFoot, hornTip, 0.04f, ivory, mat: Shell);
        var axis = Vector3.Normalize(hornTip - hornFoot);
        var u = Vector3.Normalize(Vector3.Cross(axis, V(1f, 0, 0)));
        var w = Vector3.Cross(axis, u);
        var helix = Enumerable.Range(0, 25).Select(i =>
        {
            float t = i / 24f;
            float rad = 0.034f * (1f - t) + 0.006f;
            float a = t * MathF.Tau * 2.5f;
            return hornFoot + axis * Vector3.Distance(hornFoot, hornTip) * t * 0.92f + (u * MathF.Cos(a) + w * MathF.Sin(a)) * rad;
        }).ToArray();
        b.Tube(head, helix, 0.008f, 0.004f, Rgb(244, 196, 96), Shell, 0.003f);
        // Two wings of flame from its shoulders
        var rim = Rgb(248, 128, 48);
        var heart = Rgb(255, 226, 120);
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.08f * s, 0.6f, 0.06f);
            int wing = b.Wing(s, shoulder);
            // A feathered wing of flame, its long feathers rising and swept back, licking into tongues at their ends
            var wrist = V(0.24f * s, 0.86f, -0.04f);
            OpenWing(b, wing, s, shoulder, wrist, 9, 112f, 10f, 0.44f, 0.06f, rim, Rgb(255, 196, 84), heart, 0.7f);
            foreach (var (tip, r) in new[] { (V(0.22f * s, 1.3f, -0.12f), 0.04f), (V(0.4f * s, 1.22f, -0.2f), 0.04f), (V(0.54f * s, 1.04f, -0.3f), 0.036f) })
                FlameTongue(b, wing, wrist, tip, r, rim, heart);
        });
        // The mane and the tail streaming further, flames at the hooves
        int tail = BoneOf(b, "tail");
        Blaze(b, tail, V(0, 0.58f, -0.26f * k), V(0, 0.3f, -1f), 0.5f * k, 0.06f * k);
        Blaze(b, tail, V(0, 0.58f, -0.26f * k), V(-0.2f, 0.45f, -1f), 0.42f * k, 0.05f * k);
        int mane = BoneOf(b, "mane");
        Blaze(b, mane, V(0, 0.8f, 0.22f * k), V(0, 0.55f, -1f), 0.4f * k, 0.065f * k);
        foreach (var (z, front) in new[] { (0.17f, true), (-0.21f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = BoneOf(b, (front ? "leg" : "hind") + (s < 0 ? "L" : "R"));
                Blaze(b, leg, V(0.085f * s, 0.04f, (z + 0.0f) * k), V(0.3f * s, 0.25f, -1f), 0.11f, 0.026f);
            });
        // The wings widen the sculpt and coarsen its mesh, where the nostrils' small marks lose the triangles under them:
        // they are painted instead
        b.Model.Decals.RemoveAll(d => !d.IsEye && d.Bone == head);
        PokeBuilder.Both(s => b.PaintEll(head, V(0.028f * s, 0.83f, 0.48f * k), V(0.011f, 0.009f, 0.014f), Rgb(120, 100, 90), soft: 0.006f));
        Stretch(b, V(1.08f, 1.08f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2105 Ossugard, from Marowak

    /// <summary>
    /// Ossugard: Marowak taller, its skull helmet grown into a great horned skull, a pale blue flame burning in its
    /// eyes, its club grown into a long staff of bone with a skull at its head.
    /// </summary>
    private static PokeBuilder Ossugard()
    {
        var b = Become(SkullBuild(true, false), "Ossugard", 0.86f);
        var skull = Rgb(232, 230, 224);
        var blue = Rgb(120, 200, 255);
        var paleBlue = Rgb(232, 248, 255);
        int grip = BoneOf(b, "armL");
        int head = BoneOf(b, "head");
        b.Sdf.RemoveAll(s => s.Bone == grip && s.Color.Equals(skull));
        // Dark scaled hide where Marowak's was tan
        var hide = Rgb(86, 78, 64);
        Recolour(b, (Rgb(198, 160, 112), hide), (Rgb(242, 226, 184), Rgb(112, 102, 82)));
        // The horns curl back and round like a ram's
        var horn = Rgb(196, 182, 150);
        b.Sdf.RemoveAll(s => s.Bone == head && s.Shape == SdfShape.RoundCone && s.Color.Equals(skull));
        float h = 1.18f;
        var c = V(0, 0.43f * h, 0.04f);
        var r = V(0.12f, 0.1f, 0.13f);
        PokeBuilder.Both(s => RamHorn(b, head, c + V(0.08f * s, 0.06f, -0.04f), s, 0.11f, 0.034f, horn));
        // Blue fire in the eye holes, rising over the brow
        Recolour(b, (Black, blue));
        PokeBuilder.Both(s =>
        {
            var socket = On(c, r, 0.055f * s, c.Y - 0.005f);
            FlameTongue(b, head, socket + V(0.005f * s, 0.035f, -0.01f), socket + V(0.03f * s, 0.11f, -0.05f), 0.016f, blue, paleBlue);
        });
        // The staff, a great bone knobbed at its foot and bound with rags, a ram's skull at its head
        var bone = Rgb(214, 202, 170);
        // carried over its shoulder from the raised hand
        var bottom = V(-0.32f, 0.3f, 0.1f);
        var top = V(0.24f, 0.86f, -0.16f);
        b.Limb(grip, bottom, top, 0.03f, 0.028f, bone, Shell, 0.01f);
        foreach (float s in new[] { -1f, 1f })
            b.Ell(grip, bottom + V(0.025f * s, 0.006f, 0), V(0.034f, 0.032f, 0.034f), bone, mat: Shell, blend: 0.01f);
        var rag = Rgb(150, 152, 132);
        foreach (float t in new[] { 0.62f, 0.84f })
            b.Torus(grip, Vector3.Lerp(bottom, top, t), 0.034f, 0.009f, rag, Euler(top - bottom), blend: 0.004f);
        var sk = top + V(-0.01f, 0.05f, 0.01f);
        b.Ell(grip, sk, V(0.06f, 0.055f, 0.062f), skull, mat: Shell, blend: 0.012f);
        b.Ell(grip, sk + V(0, -0.03f, 0.055f), V(0.036f, 0.028f, 0.045f), skull, mat: Shell, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(grip, sk + V(0.024f * s, 0.005f, 0.054f), V(0.015f, 0.014f, 0.014f), Rgb(60, 52, 64));
            RamHorn(b, grip, sk + V(0.04f * s, 0.025f, -0.02f), s, 0.045f, 0.016f, horn);
        });
        FlameTongue(b, grip, sk + V(0, 0.04f, 0), sk + V(0, 0.12f, -0.02f), 0.022f, blue, paleBlue);
        // Rags bound round its wrists and ankles
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var wrist = s < 0 ? V(-0.15f, 0.37f * h, 0.02f) : V(0.155f, 0.19f * h, 0.075f);
            b.Torus(arm, wrist, 0.032f, 0.01f, rag, V(0, 0, 70f * s), blend: 0.004f);
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            b.Torus(leg, V(0.08f * s, 0.05f, 0.02f), 0.04f, 0.01f, rag, blend: 0.004f);
        });
        // A longer tail, a ridge of bone along it
        int tail = BoneOf(b, "tail");
        var tailPath = Smooth(3, V(0, 0.12f * h, -0.08f), V(0, 0.06f, -0.24f), V(0.06f, 0.05f, -0.38f), V(0.14f, 0.12f, -0.46f), V(0.18f, 0.24f, -0.48f));
        b.Tube(tail, tailPath, 0.04f, 0.012f, hide, blend: 0.01f);
        for (int i = 1; i < tailPath.Length - 1; i += 2)
        {
            var dir = Vector3.Normalize(tailPath[i + 1] - tailPath[i - 1]);
            var up = Vector3.Normalize(Vector3.Cross(Vector3.Cross(dir, V(0, 1f, 0)), dir));
            if (up.Y < 0) up = -up;
            float rad = 0.04f - 0.028f * i / (tailPath.Length - 1);
            b.Spike(tail, tailPath[i] + up * rad * 0.6f, tailPath[i] + up * (rad + 0.03f) - dir * 0.015f, 0.012f, bone, mat: Shell, blend: 0.004f);
        }
        Stretch(b, V(0.88f, 1.22f, 0.95f));
        return b;
    }

    /// <summary>A ram's horn from <paramref name="root"/> on side <paramref name="s"/>: back, down and round toward the front, <paramref name="r"/> across its curl.</summary>
    private static void RamHorn(PokeBuilder b, int bone, Vector3 root, float s, float r, float thick, Color color)
    {
        var centre = root + V(0.03f * s, -r * 0.55f, -r * 0.55f);
        var points = Enumerable.Range(0, 16).Select(i =>
        {
            float t = i / 15f;
            float a = MathF.PI * 0.75f - t * MathF.PI * 1.55f;
            float rad = r * (1f - 0.35f * t);
            return centre + V(s * (0.03f * t + 0.012f), MathF.Sin(a) * rad, -MathF.Cos(a) * rad);
        }).ToArray();
        points[0] = root;
        b.Tube(bone, points, thick, thick * 0.35f, color, Shell, 0.006f);
        for (int i = 3; i < points.Length - 2; i += 3)
            b.PaintTorus(bone, points[i], thick * (1f - 0.6f * i / 15f) * 1.02f, 0.003f, PixelCanvas.Shadow(color, 0.2f), Euler(points[i + 1] - points[i - 1]));
    }

    // ------------------------------------------------------------------ 2131 Ferrisle, from Lapras

    /// <summary>
    /// Ferrisle: Lapras much larger, its knobbled shell grown into a wide deck with a low crest of ice along its
    /// middle and small pines of ice on it, its neck and its horn longer.
    /// </summary>
    private static PokeBuilder Ferrisle()
    {
        var b = Become(LaprasBuild(false), "Ferrisle", 1f);
        var shell = Rgb(170, 170, 178);
        var blue = Rgb(92, 170, 230);
        var cream = Rgb(244, 236, 200);
        var ice = Rgb(204, 236, 250);
        // The knobs go, and the shell flattens and widens into a deck
        b.Sdf.RemoveAll(s => s.Bone == Body && s.Shape == SdfShape.RoundCone && s.Color.Equals(shell));
        var deckShape = b.Sdf.Shapes.First(s => s.Bone == Body && s.Shape == SdfShape.Ellipsoid && s.Color.Equals(shell));
        var dc = V(0, 0.38f, -0.08f);
        var dr = V(0.32f, 0.1f, 0.37f);
        // The point on the deck's top over (x, z)
        Vector3 Top(float x, float z) => V(x, dc.Y + dr.Y * MathF.Sqrt(MathF.Max(0f, 1f - x * x / (dr.X * dr.X) - (z - dc.Z) * (z - dc.Z) / (dr.Z * dr.Z))), z);
        deckShape.Color = Rgb(150, 152, 158);
        deckShape.A = dc;
        deckShape.Size = dr;
        b.Sdf.Changed();
        b.PaintTorus(Body, dc + V(0, -0.02f, 0), 0.33f, 0.014f, Rgb(118, 118, 126), default, sz: 0.37f / 0.32f);
        b.PaintEll(Body, dc + V(0, 0.08f, 0), V(0.3f, 0.04f, 0.35f), Rgb(228, 236, 244));
        // A low crest of ice down its middle
        for (int i = 0; i < 7; i++)
        {
            float z = 0.14f - i * 0.065f;
            var at = Top(0, z);
            b.Spike(Body, at - V(0, 0.015f, 0), at + V(0, 0.05f + 0.015f * (i % 2), -0.015f), 0.03f, Rgb(176, 222, 248), 0.5f, Shell);
        }
        // Small pines of ice standing on the deck
        foreach (var (x, z, size) in new[] { (0.16f, 0.06f, 1.4f), (-0.17f, -0.04f, 1.6f), (0.15f, -0.22f, 1.2f), (-0.12f, -0.28f, 1.1f), (0.21f, -0.1f, 1f), (-0.2f, 0.08f, 1.1f), (0.04f, -0.34f, 1.2f), (-0.06f, 0.1f, 0.9f), (0.24f, -0.3f, 0.9f) })
        {
            var foot = Top(x, z) - V(0, 0.012f, 0);
            for (int t = 0; t < 3; t++)
                b.Spike(Body, foot + V(0, 0.035f * t * size, 0), foot + V(0, (0.08f + 0.035f * t) * size, 0), (0.036f - 0.008f * t) * size, ice, mat: Shell, blend: 0.006f);
        }
        // The neck rises further, carrying the head, and the horn grows long
        int neck = BoneOf(b, "neck");
        int head = BoneOf(b, "head");
        var d = V(0, 0.11f, 0.03f);
        Move(b, bone => bone == head, d);
        b.Model.Skeleton.Shift(d, first: head);
        var spine = Smooth(3, V(0, 0.75f, 0.31f), V(0, 0.84f, 0.33f), V(0, 0.79f, 0.3f) + d);
        b.Tube(neck, spine, 0.056f, 0.055f, blue, blend: 0f);
        b.PaintEll(neck, V(0, 0.82f, 0.38f), V(0.04f, 0.07f, 0.035f), cream, Euler(V(0, 1f, 0.1f)));
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == head && s.Shape == SdfShape.RoundCone && s.Color.Equals(blue) && s.B.Y > s.A.Y + 0.05f && MathF.Abs(s.A.X) < 0.02f).ToList())
        {
            s.B = s.A + (s.B - s.A) * 2.4f;
            s.Color = Rgb(214, 238, 250);
            s.Material = Shell;
        }
        // Deep teal hide, pale as ice beneath; a frill of ice down the back of its neck
        Recolour(b, (blue, Rgb(46, 136, 150)), (cream, Rgb(206, 232, 242)));
        for (int i = 1; i < spine.Length - 1; i += 2)
        {
            var dirN = Vector3.Normalize(spine[i + 1] - spine[i - 1]);
            var backN = Vector3.Normalize(V(0, 0, -1f) - dirN * dirN.Z);
            b.Spike(neck, spine[i] + backN * 0.045f, spine[i] + backN * 0.09f + dirN * 0.01f, 0.018f, Rgb(214, 238, 250), 0.4f, Shell, 0.006f);
        }
        b.Sdf.Changed();
        return b;
    }

    // ------------------------------------------------------------------ 2139 Ammonarch, from Omastar

    /// <summary>
    /// Ammonarch: Omastar with its spiral shell grown much larger and ridged, spikes along its outer whorl, and its
    /// tentacles longer and more of them.
    /// </summary>
    private static PokeBuilder Ammonarch()
    {
        var b = Become(Omastar(), "Ammonarch", 0.92f);
        // Stone-brown shell banded slate blue, dark teal tentacles
        Recolour(b, (Rgb(232, 220, 182), Rgb(150, 132, 106)), (Rgb(172, 152, 108), Rgb(84, 104, 120)), (Rgb(98, 186, 220), Rgb(40, 100, 116)));
        var shell = Rgb(150, 132, 106);
        var blue = Rgb(40, 100, 116);
        var lift = Shifted(b, "head", V(0, 0.26f, 0.06f));
        var sc = V(0, 0.4f, -0.06f) + lift;
        // The shell grows and sits further back over its face
        Grow(b, V(1.35f, 1.35f, 1.35f), sc, bone => bone == Body);
        var back = V(0, 0.06f, -0.08f);
        Move(b, bone => bone == Body, back);
        sc += back;
        var sr = V(0.18f, 0.22f, 0.21f) * 1.35f;
        // A keel round its outer whorl, and spikes along it
        b.Torus(Body, sc, sr.Y * 1.0f, 0.02f, shell, V(0, 0, 90f), sz: sr.Z / sr.Y, mat: Shell, blend: 0.01f);
        for (int i = 0; i < 7; i++)
        {
            float a = (-20f + i * 32f) * Degree;
            var dir = V(0, MathF.Cos(a), -MathF.Sin(a));
            var at = Out(sc, sr, default, dir);
            var n = Outward(sc, sr, at);
            b.Spike(Body, at - n * 0.01f, at + n * 0.1f, 0.032f, PixelCanvas.Mix(shell, Rgb(170, 140, 100), 0.4f), mat: Shell);
        }
        // More tentacles, longer, between the old ones
        int head = BoneOf(b, "head");
        var c = V(0, 0.25f, 0.12f) + lift;
        var r = V(0.17f, 0.12f, 0.1f);
        for (int i = 0; i < 7; i++)
        {
            float a = (-100f + (i + 0.5f) * (200f / 7f)) * Degree;
            Tentacle(b, head, c + V(MathF.Sin(a) * r.X * 0.8f, -r.Y * 0.5f, MathF.Cos(a) * r.Z * 0.7f), V(MathF.Sin(a) * 1.3f, 0, MathF.Cos(a)), 0.34f, 0.03f, blue);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2141 Eurypterox, from Kabutops

    /// <summary>
    /// Eurypterox: Kabutops longer and lower, with a jointed tail ending in a flat paddle, its two scythes bigger and a
    /// second, smaller pair of claws below them.
    /// </summary>
    private static PokeBuilder Eurypterox()
    {
        // Kabutops gone down onto many legs as a sea scorpion: a long low body of slate plates, its two scythes held up
        // over its head, a smaller pair of claws before them, and a jointed tail arching up behind to a leaf-shaped paddle
        var b = new PokeBuilder("Eurypterox", 0.98f, BodyPlan.Quadruped, V(0, 0.2f, 0)) { Coat = Shell };
        var slate = Rgb(70, 92, 108);
        var plate = Rgb(92, 104, 112);
        var under = Rgb(64, 118, 150);
        var blade = Rgb(108, 120, 130);
        // The body, plates overlapping from the head back
        var spine = new[] { V(0, 0.2f, 0.16f), V(0, 0.21f, 0.06f), V(0, 0.21f, -0.04f), V(0, 0.2f, -0.14f), V(0, 0.19f, -0.24f) };
        for (int i = 0; i < spine.Length; i++)
        {
            float w = 0.14f - 0.012f * i;
            b.Ell(Body, spine[i], V(w, 0.075f, 0.075f), i % 2 == 0 ? slate : plate, mat: Shell, blend: 0.02f);
            b.Ell(Body, spine[i] + V(0, 0.05f, -0.01f), V(w * 0.95f, 0.04f, 0.07f), plate, V(-12f, 0, 0), Shell, 0.012f);
            b.PaintEll(Body, spine[i] - V(0, 0.06f, 0), V(w * 0.85f, 0.03f, 0.06f), under);
        }
        // Eight legs down its sides
        for (int i = 0; i < 4; i++)
            PokeBuilder.Both(s =>
            {
                float z = 0.12f - i * 0.1f;
                var hip = V(0.11f * s, 0.16f, z);
                int leg = i == 0 ? b.Leg(s, hip, true) : i == 3 ? b.Leg(s, hip, false) : b.Part($"leg{i}" + (s < 0 ? "L" : "R"), Body, hip, PokeRole.Leg, 0.4f * i, s);
                var knee = V(0.2f * s, 0.12f, z + 0.02f);
                var foot = V(0.24f * s, 0.01f, z + 0.04f - i * 0.01f);
                b.Limb(leg, hip, knee, 0.022f, 0.018f, under, Shell);
                b.Spike(leg, knee, foot, 0.016f, slate, mat: Shell, blend: 0.004f);
            });
        // A low head with a ridge of studs and pale eyes
        int head = b.Head(V(0, 0.2f, 0.22f));
        var hc = V(0, 0.2f, 0.27f);
        var hr = V(0.09f, 0.06f, 0.07f);
        b.Ell(head, hc, hr, slate, mat: Shell);
        foreach (var (x, z) in new[] { (0f, 0.0f), (-0.04f, -0.02f), (0.04f, -0.02f), (0f, -0.04f) })
            b.Spike(head, hc + V(x, 0.05f, z), hc + V(x * 1.2f, 0.08f, z - 0.01f), 0.012f, plate, mat: Shell, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            b.Eye(head, On(hc, hr, 0.04f * s, hc.Y + 0.01f), V(0.5f * s, 0.2f, 1f), 0.016f, sclera: true, white: Rgb(196, 246, 240), pupil: Rgb(220, 255, 250), glare: true);
            b.Spike(head, hc + V(0.025f * s, -0.04f, 0.05f), hc + V(0.02f * s, -0.09f, 0.07f), 0.01f, plate, mat: Shell, blend: 0.004f);
            // A small pair of claws before its face
            int claw = b.Part(s < 0 ? "clawL" : "clawR", Body, V(0.07f * s, 0.17f, 0.2f), PokeRole.Arm, s, s);
            var elbow = V(0.14f * s, 0.12f, 0.3f);
            var hand = V(0.12f * s, 0.08f, 0.4f);
            b.Limb(claw, V(0.07f * s, 0.17f, 0.2f), elbow, 0.02f, 0.018f, under, Shell);
            b.Limb(claw, elbow, hand, 0.018f, 0.022f, slate, Shell);
            foreach (float t in new[] { -1f, 1f })
                b.Spike(claw, hand, hand + V(0.02f * t * s, -0.01f, 0.05f), 0.012f, plate, mat: Shell, blend: 0.004f);
            // The two great scythes, raised high over the body
            int arm = b.Arm(s, V(0.1f * s, 0.23f, 0.12f));
            var shoulder = V(0.1f * s, 0.23f, 0.12f);
            var bend = V(0.2f * s, 0.46f, 0.1f);
            b.Limb(arm, shoulder, bend, 0.03f, 0.026f, under, Shell);
            b.Ell(arm, bend, V(0.03f, 0.03f, 0.03f), slate, mat: Shell);
            Blade(b, arm, bend, bend + V(0.06f * s, 0.04f, 0.3f), 0.06f, blade, V(s, 0, 0), 0.25f, Shell);
            b.Spike(arm, bend + V(0.02f * s, 0.02f, 0.18f), bend + V(0.03f * s, -0.04f, 0.16f), 0.01f, plate, mat: Shell, blend: 0.003f);
        });
        // The tail, jointed in plates, arching up behind to a leaf-shaped paddle with two small fins
        int tail = b.Tail(V(0, 0.2f, -0.28f));
        var path = Smooth(3, V(0, 0.19f, -0.28f), V(0, 0.2f, -0.44f), V(0, 0.3f, -0.56f), V(0, 0.48f, -0.6f), V(0, 0.62f, -0.54f));
        for (int i = 0; i < path.Length - 1; i++)
        {
            float t = i / (float)(path.Length - 1);
            var mid = (path[i] + path[i + 1]) / 2f;
            float len = Vector3.Distance(path[i], path[i + 1]);
            b.Ell(tail, mid, V(0.07f - 0.035f * t, len * 0.62f, 0.055f - 0.025f * t), i % 2 == 0 ? slate : plate, Euler(path[i + 1] - path[i]), Shell, 0.006f);
        }
        var end = path[^1];
        var up = Vector3.Normalize(path[^1] - path[^2]);
        Blade(b, tail, end - up * 0.02f, end + up * 0.22f + V(0, 0, 0.04f), 0.1f, Rgb(84, 116, 130), V(1f, 0, 0), 0.22f, Shell);
        PokeBuilder.Both(s => Blade(b, tail, end - up * 0.02f, end + V(0.1f * s, 0.06f, -0.02f), 0.035f, Rgb(84, 116, 130), V(0, 0.3f, 1f), 0.3f, Shell));
        return b;
    }

    // ------------------------------------------------------------------ 2142 Azhdarch, from Aerodactyl

    /// <summary>
    /// Azhdarch: Aerodactyl grown into a giant of the skies: its head carried high on a long stiff neck under a great
    /// crest, its wings broader and a deeper purple, and a ridge of amber stones down its back and tail.
    /// </summary>
    private static PokeBuilder Azhdarch()
    {
        var b = Become(AerodactylBuild(false), "Azhdarch", 1f);
        var grey = Rgb(110, 104, 92);
        var amber = Rgb(236, 160, 56);
        var lift = Shifted(b, "head", V(0, 0.6f, 0.06f));
        // A hide of weathered stone-grey, wings of deep purple
        Recolour(b, (Rgb(156, 126, 196), Rgb(84, 58, 128)), (Rgb(182, 174, 206), grey), (Rgb(74, 70, 84), Rgb(64, 60, 56)));
        // Broader wings
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            Grow(b, V(1.3f, 1.12f, 1f), V(0.08f * s, 0.56f, 0) + lift, bone => bone == wing);
        });
        // The head carried up and forward on a long stiff neck, which turns from its foot
        int head = BoneOf(b, "head");
        int jaw = BoneOf(b, "jaw");
        var d = V(0, 0.2f, 0.11f);
        Move(b, bone => bone == head || bone == jaw, d);
        b.Model.Skeleton.Shift(d, first: jaw);
        b.Limb(head, V(0, 0.56f, 0.02f) + lift, V(0, 0.6f, 0.04f) + d + lift, 0.056f, 0.05f, grey);
        // A great crest swept back from its skull
        var c = V(0, 0.68f, 0.12f) + d + lift;
        Blade(b, head, c + V(0, 0.04f, -0.01f), c + V(0, 0.26f, -0.2f), 0.12f, grey, V(1f, 0, 0), 0.3f);
        // Amber crystals set along its crest
        foreach (var (at, way) in new[] { (V(0, 0.1f, -0.04f), V(0, 1f, 0.2f)), (V(0.015f, 0.16f, -0.1f), V(0.3f, 1f, -0.2f)), (V(-0.015f, 0.2f, -0.15f), V(-0.3f, 1f, -0.4f)), (V(0, 0.24f, -0.19f), V(0, 0.8f, -0.8f)), (V(0.02f, 0.07f, 0.0f), V(0.6f, 0.6f, 0.4f)), (V(-0.02f, 0.07f, 0.0f), V(-0.6f, 0.6f, 0.4f)) })
            b.Spike(head, c + at, c + at + Vector3.Normalize(way) * 0.06f, 0.018f, amber, 0.6f, Shell, 0.004f);
        // A ridge of amber stones down its back and along its tail
        var bc = V(0, 0.48f, 0) + lift;
        var br = V(0.1f, 0.13f, 0.11f);
        foreach (var dir in new[] { V(0, 0.85f, -0.4f), V(0, 0.75f, -0.6f), V(0, 0.55f, -0.8f), V(0, 0.4f, -0.9f), V(0, 0.2f, -1f), V(0, 0.05f, -1f) })
        {
            var at = Out(bc, br, V(-20f, 0, 0), dir);
            b.Spike(Body, at - dir * 0.01f, at + Vector3.Normalize(dir + V(0, 0.6f, 0)) * 0.07f, 0.024f, amber, 0.6f, Shell, 0.004f);
        }
        int tail = BoneOf(b, "tail");
        var path = Smooth(3, V(0, 0.42f, -0.07f), V(0, 0.36f, -0.25f), V(0.02f, 0.33f, -0.44f));
        for (int i = 2; i < path.Length - 1; i += 2)
        {
            float t = i / (float)(path.Length - 1);
            float rad = 0.04f + (0.014f - 0.04f) * t;
            b.Ell(tail, path[i] + lift + V(0, rad * 0.9f, 0), V(0.016f, 0.013f, 0.02f), amber, mat: Shell, blend: 0.005f);
        }
        return Lift(b);
    }

    /// <summary>A thin band spiralling up from <paramref name="foot"/> to <paramref name="tip"/> round a horn <paramref name="r"/> wide at its foot (a narwhal's tusk, a unicorn's horn).</summary>
    private static void Helix(PokeBuilder b, int bone, Vector3 foot, Vector3 tip, float r, float turns, float thick, Color color)
    {
        var axis = Vector3.Normalize(tip - foot);
        var u = Vector3.Normalize(Vector3.Cross(axis, MathF.Abs(axis.X) < 0.9f ? V(1f, 0, 0) : V(0, 1f, 0)));
        var w = Vector3.Cross(axis, u);
        float length = Vector3.Distance(foot, tip);
        var points = Enumerable.Range(0, 25).Select(i =>
        {
            float t = i / 24f;
            float rad = r * (1f - t) * 0.95f + thick * 0.5f;
            float a = t * MathF.Tau * turns;
            return foot + axis * length * t * 0.92f + (u * MathF.Cos(a) + w * MathF.Sin(a)) * rad;
        }).ToArray();
        b.Tube(bone, points, thick, thick * 0.5f, color, Shell, 0.003f);
    }

    // ------------------------------------------------------------------ 2022 Lancerow, from Fearow

    /// <summary>
    /// Lancerow: Fearow with its long beak grown into a lance half again as long as its head, a ring of hard shell
    /// where it meets the face, its red crest swept back into three long plumes, and broader wings barred pale.
    /// </summary>
    private static PokeBuilder Lancerow()
    {
        var b = Become(Fearow(), "Lancerow", 0.97f);
        var lift = Shifted(b, "head", V(0, 0.7f, 0.15f));
        int head = BoneOf(b, "head");
        var red = Rgb(224, 74, 64);
        var crimson = Rgb(176, 30, 52);
        var bone = Rgb(226, 214, 184);
        Recolour(b, (Rgb(170, 112, 66), Rgb(164, 134, 92)), (Rgb(222, 186, 132), Rgb(240, 226, 192)), (Rgb(226, 160, 150), Rgb(156, 126, 96)));
        // The beak grows into a lance near as long as the bird, ringed where it leaves the face with a knob of shell
        var beak = b.Sdf.Shapes.First(s => s.Bone == head && s.Color.Equals(Rgb(232, 170, 164)));
        var dir = Vector3.Normalize(beak.B - beak.A);
        beak.B = beak.A + (beak.B - beak.A) * 2.3f;
        beak.Size = new Vector3(beak.Size.X * 1.25f, beak.Size.Y, beak.Size.Z);
        beak.Color = bone;
        b.Torus(head, beak.A + dir * 0.02f, 0.042f, 0.018f, Rgb(198, 186, 160), Euler(dir), mat: Shell, blend: 0.004f);
        b.Ell(head, beak.A + dir * 0.005f, V(0.05f, 0.05f, 0.035f), Rgb(208, 196, 168), Euler(dir), Shell, 0.01f);
        // The crest swept back into three long crimson plumes streaming behind the head
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.Equals(red));
        var c = V(0, 0.77f, 0.16f) + lift;
        foreach (var (x, rise, len) in new[] { (0f, 0.1f, 0.5f), (-0.03f, 0.03f, 0.44f), (0.03f, 0.05f, 0.47f) })
        {
            var root = c + V(x, 0.05f, 0.03f);
            var path = Smooth(3, root, root + V(x * 2f, 0.1f, -0.14f), root + V(x * 4f, 0.08f + rise, -len * 0.7f), root + V(x * 6f, rise, -len));
            Ribbon(b, head, path, 0.05f, crimson, V(x * 8f, 1f, 0), 0.012f);
        }
        // Wings spread wide: tan coverts, dark flight feathers barred with a band of cream
        var dark = Rgb(72, 58, 44);
        var tan = Rgb(164, 134, 92);
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            b.Sdf.RemoveAll(sh => sh.Bone == wing);
            var shoulder = V(0.12f * s, 0.48f, 0.06f) + lift;
            var wrist = V(0.34f * s, 0.64f, -0.02f) + lift;
            OpenWing(b, wing, s, shoulder, wrist, 8, 70f, -70f, 0.42f, 0.055f, dark, tan, Rgb(214, 200, 164), 0.35f);
        });
        b.Sdf.Changed();
        return Lift(b);
    }

    /// <summary>
    /// A bird's wing spread open on the bone <paramref name="wing"/> (a forebear's own wing bone, its folded wing taken
    /// away): an arm of <paramref name="coverts"/> from shoulder to wrist and <paramref name="count"/> flight feathers
    /// fanned from along it, the first <paramref name="from"/> degrees up from straight out and the last
    /// <paramref name="to"/>, each crossed by a band of <paramref name="bar"/>.
    /// </summary>
    private static void OpenWing(PokeBuilder b, int wing, float s, Vector3 shoulder, Vector3 wrist, int count, float from, float to,
        float length, float width, Color color, Color coverts, Color bar, float back = 0.3f)
    {
        b.Limb(wing, shoulder, wrist, width * 1.1f, width * 0.7f, coverts);
        var span = wrist - shoulder;
        b.Ell(wing, shoulder + span * 0.45f + V(0, -0.02f, -0.01f), V(span.Length() * 0.5f, width * 1.5f, width * 0.5f), coverts, V(0, 0, MathF.Atan2(span.Y, span.X * s) / Degree * s), blend: 0.02f);
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 1f : i / (count - 1f);
            float a = (from + (to - from) * t) * Degree;
            var root = Vector3.Lerp(Vector3.Lerp(shoulder, wrist, 0.25f), wrist, t);
            var dir = Vector3.Normalize(V(MathF.Cos(a) * s, MathF.Sin(a), -back));
            float len = length * (0.65f + 0.35f * MathF.Sin(MathF.PI * (0.2f + 0.7f * t)));
            var end = root + dir * len;
            Frond(b, wing, root, end, width, color, V(0, 0, 1f), 0.3f, Fur, 0.012f);
            if (bar != color) b.PaintEll(wing, root + (end - root) * 0.5f, V(width * 1.3f, len * 0.04f, width * 1.3f), bar, Euler(dir));
        }
    }

    // ------------------------------------------------------------------ 2028 Sandspire, from Sandslash

    /// <summary>
    /// Sandspire: Sandslash with its quills grown into fewer, taller spines of banded sandstone, tallest over its
    /// shoulders, its claws thickened into picks and a plate of rock over its brow.
    /// </summary>
    private static PokeBuilder Sandspire()
    {
        var b = Become(Sandslash(), "Sandspire", 0.9f);
        var quill = Rgb(146, 96, 58);
        var tipColor = Rgb(200, 156, 104);
        var stone = Rgb(196, 150, 100);
        var band = Rgb(132, 82, 52);
        var rock = Rgb(126, 98, 74);
        Recolour(b, (Rgb(236, 198, 106), Rgb(212, 170, 120)), (Rgb(248, 234, 196), Rgb(230, 196, 150)));
        b.Sdf.RemoveAll(s => s.Bone == Body && (s.Color.Equals(quill) || s.Color.Equals(tipColor)));
        var c = V(0, 0.36f, -0.02f);
        var r = V(0.16f, 0.2f, 0.15f);
        for (int row = 0; row < 3; row++)
        {
            int cols = row == 0 ? 3 : 4;
            for (int col = 0; col < cols; col++)
            {
                float elev = (66f - row * 24f) * Degree;
                float phi = ((col - (cols - 1) / 2f) * 34f) * Degree;
                var dir = Vector3.Normalize(V(MathF.Sin(phi) * MathF.Cos(elev), MathF.Sin(elev), -MathF.Cos(phi) * MathF.Cos(elev)));
                var root = c + dir * r * 0.8f;
                float len = (row == 0 ? 0.34f : 0.28f - row * 0.03f) * (1f - MathF.Abs(phi) * 0.25f);
                var to = root + Vector3.Normalize(dir + V(0, 0.35f, -0.25f)) * len;
                b.Spike(Body, root, to, 0.065f, stone, 0.6f, Shell, 0.012f);
                b.PaintEll(Body, Vector3.Lerp(root, to, 0.3f), V(0.07f, 0.022f, 0.07f), band, Euler(to - root));
                b.PaintEll(Body, Vector3.Lerp(root, to, 0.55f), V(0.06f, 0.02f, 0.06f), Rgb(170, 96, 64), Euler(to - root));
                b.PaintEll(Body, Vector3.Lerp(root, to, 0.78f), V(0.05f, 0.016f, 0.05f), band, Euler(to - root));
            }
        }
        // Plates of banded rock over its shoulders
        PokeBuilder.Both(s =>
        {
            var at = V(0.15f * s, 0.47f, 0.02f);
            b.Ell(Body, at, V(0.075f, 0.06f, 0.07f), stone, V(0, 0, -25f * s), Shell, 0.015f);
            b.PaintEll(Body, at + V(0.01f * s, 0.01f, 0), V(0.08f, 0.012f, 0.075f), band, V(0, 0, -25f * s));
        });
        // Claws thickened into great dark picks
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            Grow(b, V(1.5f, 1.5f, 1.5f), V(0.2f * s, 0.33f, 0.18f), sh => sh.Bone == arm && sh.Color.Equals(White), _ => false);
        });
        Recolour(b, (White, Rgb(96, 94, 90)));
        foreach (var s in b.Sdf.Shapes.Where(s => s.Color.Equals(Rgb(96, 94, 90)))) s.Material = Shell;
        // A helmet of rough rock over its brow and the crown of its head
        int head = BoneOf(b, "head");
        b.Ell(head, V(0, 0.6f, 0.11f), V(0.11f, 0.065f, 0.11f), rock, V(-15f, 0, 0), Shell, 0.015f);
        b.Ell(head, V(0, 0.585f, 0.19f), V(0.085f, 0.03f, 0.05f), rock, V(-25f, 0, 0), Shell, 0.012f);
        // A tail of banded plates
        int tail = BoneOf(b, "tail");
        foreach (var (at, size) in new[] { (V(0, 0.2f, -0.2f), 0.05f), (V(0, 0.15f, -0.26f), 0.042f), (V(0, 0.11f, -0.31f), 0.034f) })
            b.Spike(tail, at, at + V(0, 0.07f, -0.06f), size, stone, 0.5f, Shell);
        return b;
    }

    // ------------------------------------------------------------------ 2049 Somnimoth, from Venomoth

    /// <summary>
    /// Somnimoth: Venomoth with its four wings grown wider and paled to lilac, the lower pair trailing long tails like
    /// a luna moth's, an eyespot on each upper wing, its fuzzy body thicker and its feelers branched like ferns.
    /// </summary>
    private static PokeBuilder Somnimoth()
    {
        var b = Become(Venomoth(), "Somnimoth", 0.9f);
        var lift = Shifted(b, "head", V(0, 0.5f, 0.02f));
        var lilac = Rgb(234, 222, 250);
        var vein = Rgb(182, 160, 222);
        Recolour(b, (Rgb(214, 196, 240), lilac), (Rgb(160, 130, 206), vein), (Rgb(144, 112, 196), Rgb(190, 164, 212)));
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
        {
            d.White = Rgb(178, 54, 120);
            d.Pupil = Rgb(86, 20, 62);
        }
        Grow(b, V(1.25f, 1.25f, 1.25f), V(0, 0.42f, 0) + lift, bone => bone == Body);
        // A fluffy pale collar where the head meets the body, and the abdomen striped in pink
        var collar = V(0, 0.49f, 0.03f) + lift;
        b.Ell(Body, collar, V(0.085f, 0.05f, 0.075f), Rgb(244, 238, 248), blend: 0.025f);
        FurTufts(b, Body, collar, V(0.085f, 0.05f, 0.075f), 16, 0.03f, 0.016f, Rgb(244, 238, 248), 0.4f, -0.8f, 0.6f);
        foreach (float y in new[] { 0.3f, 0.35f, 0.4f })
            b.PaintTorus(Body, V(0, y, -0.02f) + lift, 0.07f, 0.012f, Rgb(232, 176, 206), V(10f, 0, 0));
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            var shoulder = V(0.04f * s, 0.46f, -0.03f) + lift;
            var k = V(1.3f, 1.18f, 1f);
            Grow(b, k, shoulder, bone => bone == wing);
            // A long tail trailing from each lower wing
            var lower = shoulder + (V(0.17f * s, 0.34f, -0.04f) + lift - shoulder) * k;
            var foot = lower + V(0.05f * s, -0.07f, 0);
            Blade(b, wing, foot, foot + V(0.07f * s, -0.24f, -0.03f), 0.05f, lilac, V(0, 0, 1f), 0.25f);
            b.PaintEll(wing, foot + V(0.065f * s, -0.22f, -0.03f), V(0.03f, 0.04f, 0.02f), vein);
            // A band of cream across each upper wing, inside its dark border
            var upper = shoulder + (V(0.26f * s, 0.6f, -0.02f) + lift - shoulder) * k;
            var way = upper - shoulder;
            b.PaintEll(wing, shoulder + way * 1.3f, V(0.12f, 0.12f, 0.08f), Rgb(120, 92, 170));
            b.PaintEll(wing, shoulder + way * 1.0f, V(0.08f, 0.08f, 0.08f), Rgb(246, 236, 196));
            // An eyespot on each lower wing, rings of purple and pink
            var spot = lower + V(0.01f * s, 0.03f, 0);
            b.Mark(wing, spot, V(0, 0, 1f), 0.04f, 0.04f, Rgb(84, 60, 140), MarkShape.Disc);
            b.Mark(wing, spot, V(0, 0, 1f), 0.03f, 0.03f, Rgb(238, 160, 196), MarkShape.Disc);
            b.Mark(wing, spot, V(0, 0, 1f), 0.016f, 0.016f, Rgb(84, 60, 140), MarkShape.Disc);
        });
        // Feelers branched like ferns
        PokeBuilder.Both(s =>
        {
            int ear = BoneOf(b, s < 0 ? "earL" : "earR");
            var from = V(0.02f * s, 0.59f, 0.02f) + lift;
            var to = V(0.06f * s, 0.72f, 0.0f) + lift;
            foreach (float t in new[] { 0.3f, 0.5f, 0.7f, 0.88f })
            {
                var at = Vector3.Lerp(from, to, t);
                foreach (float z in new[] { -1f, 1f })
                    b.Spike(ear, at, at + V(0.03f * s, 0.018f, 0.03f * z) * (1.2f - t * 0.6f), 0.007f, Rgb(144, 112, 196), blend: 0.003f);
            }
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2051 Molemount, from Dugtrio

    /// <summary>
    /// Molemount: five of Dugtrio's heads in a ring, the middle one tallest, standing out of the wide mound of earth
    /// the three never showed, boulders set in its slopes.
    /// </summary>
    private static PokeBuilder Molemount()
    {
        var b = new PokeBuilder("Molemount", 0.84f, BodyPlan.Biped, V(0, 0.17f, 0)) { Coat = Scales };
        var brown = Rgb(104, 84, 72);
        var nose = Rgb(226, 132, 140);
        var earth = Rgb(122, 90, 60);
        Burrow(b, 0.44f, 0.38f, 20, Rgb(150, 148, 150), 2051u);
        // The mound, dark earth with paler dirt on its slopes, boulders set in it
        b.Ell(Root, V(0, 0.0f, 0), V(0.46f, 0.34f, 0.4f), earth, blend: 0.03f);
        foreach (var (x, z, sz) in new[] { (0.2f, 0.16f, 0.06f), (-0.24f, 0.05f, 0.07f), (0.06f, -0.24f, 0.08f), (-0.12f, 0.22f, 0.05f), (0.27f, -0.12f, 0.06f) })
            b.PaintEll(Root, V(x * 1.2f, 0.24f, z * 1.2f), V(sz * 1.8f, 0.08f, sz * 1.8f), Rgb(146, 108, 74));
        foreach (var (x, z, k) in new[] { (0.33f, 0.1f, 0.08f), (-0.3f, -0.14f, 0.085f), (-0.12f, 0.33f, 0.07f), (0.2f, -0.3f, 0.07f), (-0.36f, 0.12f, 0.065f), (0.12f, 0.3f, 0.06f), (-0.2f, 0.24f, 0.05f), (0.36f, -0.08f, 0.055f) })
        {
            float y = 0.34f * MathF.Sqrt(MathF.Max(0f, 1f - x * x / 0.2116f - z * z / 0.16f));
            b.Box(Root, V(x, y - k * 0.2f, z), V(k * 1.0f, k * 0.8f, k * 0.85f), k * 0.3f, Rgb(140, 138, 142), V(15f, x * 200f, 20f), Shell, 0.008f);
        }
        // Five heads: the tallest in the middle, four round it, each bobbing on its own
        DiglettDome(b, Body, V(0, 0.27f, -0.05f), 1.1f, brown, nose);
        Grow(b, V(1.08f, 0.8f, 1.08f), V(0, 0.27f, -0.05f), bone => bone == Body);
        foreach (var (at, k, i) in new[] { (V(-0.12f, 0.2f, 0.15f), 0.76f, 0), (V(0.13f, 0.2f, 0.15f), 0.76f, 1), (V(-0.22f, 0.22f, -0.06f), 0.72f, 2), (V(0.22f, 0.22f, -0.05f), 0.7f, 3) })
        {
            int part = b.Part("head" + i, Root, at + V(0, 0.05f, 0), PokeRole.Head, 0.7f + 1.3f * i, at.X < 0 ? -1f : 1f);
            DiglettDome(b, part, at, k, brown, nose);
            Grow(b, V(1.08f, 0.78f, 1.08f), at, bone => bone == part);
        }
        b.CutBox(Root, V(0, -0.3f, 0), V(0.8f, 0.3f, 0.8f), Quaternion.Identity);
        return b;
    }

    // ------------------------------------------------------------------ 2053 Regalynx, from Persian

    /// <summary>
    /// Regalynx: Persian longer and leaner, its cream coat brushed with gold at the paws and the tail, a mane of soft
    /// fur at its chest, its brow gem grown into a crown of three gems in a gold setting and its curled tail ending in
    /// a tuft.
    /// </summary>
    private static PokeBuilder Regalynx()
    {
        var b = Become(Persian(), "Regalynx", 0.88f);
        var fur = Rgb(246, 232, 194);
        var gold = Rgb(234, 188, 90);
        var gem = Rgb(222, 48, 62);
        int head = BoneOf(b, "head");
        // No whiskers; ears of gold, pink inside, tall and tufted like a lynx's
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.Equals(Rgb(236, 236, 240)));
        Recolour(b, (Rgb(60, 52, 58), Rgb(228, 186, 104)), (Rgb(150, 120, 116), Rgb(240, 196, 196)));
        PokeBuilder.Both(s =>
        {
            int ear = BoneOf(b, s < 0 ? "earL" : "earR");
            Grow(b, V(0.9f, 1.3f, 1f), V(0.06f * s, 0.51f, 0.19f), bone => bone == ear);
            var tip = V(0.06f * s, 0.51f, 0.19f) + (V(0.11f * s, 0.62f, 0.17f) - V(0.06f * s, 0.51f, 0.19f)) * V(0.9f, 1.3f, 1f);
            b.Spike(ear, tip - V(0.01f * s, 0.02f, 0), tip + V(0.02f * s, 0.07f, -0.01f), 0.01f, Rgb(196, 150, 70), 0.6f);
        });
        // The crown of three gems
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.Equals(gem));
        var brow = V(0, 0.515f, 0.279f);
        b.Ell(head, brow + V(0, 0.004f, -0.004f), V(0.056f, 0.014f, 0.02f), gold, V(-40f, 0, 0), Metal, 0.004f);
        b.Ell(head, brow + V(0, 0.026f, 0.0f), V(0.02f, 0.026f, 0.013f), gem, V(-40f, 0, 0), Metal, 0.004f);
        PokeBuilder.Both(s => b.Ell(head, brow + V(0.042f * s, 0.008f, -0.01f), V(0.013f, 0.017f, 0.01f), gem, V(-40f, 0, 0), Metal, 0.004f));
        // Its legs shade into gold toward the paws
        foreach (var (z, front) in new[] { (0.14f, true), (-0.15f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = BoneOf(b, (front ? "leg" : "hind") + (s < 0 ? "L" : "R"));
                b.PaintEll(leg, V(0.066f * s, 0.07f, z + (front ? 0.03f : 0.0f)), V(0.05f, 0.1f, 0.06f), Rgb(238, 210, 140));
                b.PaintEll(leg, V(0.066f * s, 0.03f, z + (front ? 0.04f : 0.01f)), V(0.042f, 0.045f, 0.058f), gold);
            });
        // A great mane of soft pale fur round its neck and chest
        var mane = Rgb(248, 226, 230);
        var mc = V(0, 0.36f, 0.15f);
        var mr = V(0.105f, 0.11f, 0.08f);
        b.Ell(Body, mc, mr, mane, blend: 0.03f);
        FurTufts(b, Body, mc, mr, 30, 0.06f, 0.026f, mane, 0.2f, -0.9f, 0.9f);
        // The tail's curl ends in a gold tuft
        int tail = BoneOf(b, "tail");
        var dir = Vector3.Normalize(V(0.2f, 0.7f, -1f));
        var up = Vector3.Normalize(Vector3.Cross(Vector3.Cross(dir, V(0, 1f, 0)), dir));
        if (up.Y < 0f) up = -up;
        var end = V(0, 0.33f, -0.21f) + dir * 0.3f + up * 0.3f * 0.15f;
        var tuft = end + up * 0.06f * 2f;
        b.Ell(tail, tuft, V(0.055f, 0.05f, 0.055f), gold, blend: 0.012f);
        FurTufts(b, tail, tuft, V(0.055f, 0.05f, 0.055f), 18, 0.05f, 0.022f, gold, 2f, -0.9f, 0.2f);
        Stretch(b, V(0.95f, 1.04f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2055 Psyduke, from Golduck

    /// <summary>
    /// Psyduke: Golduck taller and leaner, its blue deepened, its bill longer, the gem on its brow grown into a third
    /// eye that glows, webbed fins spread from its forearms and a pale ring of light floating round its head.
    /// </summary>
    private static PokeBuilder Psyduke()
    {
        var b = Become(Golduck(), "Psyduke", 0.94f);
        var oldBlue = Rgb(92, 152, 222);
        var blue = Rgb(42, 52, 112);
        var bill = Rgb(246, 222, 140);
        var fin = Rgb(158, 214, 214);
        Recolour(b, (oldBlue, blue), (PixelCanvas.Light1(oldBlue, 0.18f), Rgb(58, 70, 140)), (bill, Rgb(226, 190, 104)));
        bill = Rgb(226, 190, 104);
        foreach (var d in b.Model.Decals.Where(d => d.IsEye)) d.Iris = Rgb(236, 214, 80);
        int head = BoneOf(b, "head");
        // A longer, narrower bill
        Grow(b, V(0.8f, 0.9f, 1.4f), V(0, 0.8f, 0.06f), s => s.Bone == head && s.Color.Equals(bill), _ => false);
        // A longer tail, ending in a pale fin
        int tail = BoneOf(b, "tail");
        b.Tube(tail, Smooth(3, V(0, 0.22f, -0.38f), V(0.04f, 0.14f, -0.56f), V(0.1f, 0.16f, -0.72f), V(0.16f, 0.24f, -0.84f)), 0.04f, 0.012f, blue, blend: 0.01f);
        Blade(b, tail, V(0.08f, 0.17f, -0.66f), V(0.12f, 0.3f, -0.74f), 0.04f, fin, V(1f, 0, 0), 0.25f);
        // The third eye, wide and glowing
        var gem = b.Sdf.Shapes.First(s => s.Bone == head && s.Color.Equals(Rgb(220, 56, 60)) && s.Material == Glow);
        gem.Size = V(0.04f, 0.03f, 0.024f);
        gem.Color = Rgb(250, 110, 130);
        b.PaintEll(head, gem.A + V(0, 0, 0.018f), V(0.016f, 0.016f, 0.012f), Rgb(255, 222, 226));
        // Webbed fins from its forearms
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            Blade(b, arm, V(0.24f * s, 0.6f, 0.08f), V(0.36f * s, 0.62f, -0.06f), 0.07f, fin, V(s, 0.3f, 0), 0.22f);
            Blade(b, arm, V(0.27f * s, 0.56f, 0.13f), V(0.38f * s, 0.5f, 0.0f), 0.05f, fin, V(s, 0.3f, 0), 0.22f);
        });
        // A ring of light floating round its head on thin strands
        var ring = V(0, 1.0f, -0.02f);
        b.Torus(head, ring, 0.17f, 0.011f, Rgb(250, 222, 240), mat: Glow, blend: 0.004f);
        PokeBuilder.Both(s => b.Tube(head, new[] { V(0.09f * s, 0.93f, -0.01f), V(0.17f * s, 1.0f, -0.02f) }, 0.005f, 0.004f, Rgb(250, 222, 240), Glow, 0.003f));
        b.Tube(head, new[] { V(0, 0.93f, -0.1f), V(0, 1.0f, -0.19f) }, 0.005f, 0.004f, Rgb(250, 222, 240), Glow, 0.003f);
        Stretch(b, V(0.82f, 1.12f, 0.86f));
        return b;
    }

    // ------------------------------------------------------------------ 2059 Kirinine, from Arcanine

    /// <summary>
    /// Kirinine: Arcanine larger, its mane flowing longer like a lion dance's, two short horns swept back from its
    /// brow and a long beard of cream fur.
    /// </summary>
    private static PokeBuilder Kirinine()
    {
        var b = Become(Arcanine(), "Kirinine", 0.98f);
        const float lift = 0.09f;
        var mane = Rgb(248, 228, 184);
        var c = V(0, 0.64f + lift, 0.25f);
        // A deeper red-orange coat
        var red = Rgb(204, 84, 46);
        Recolour(b, (Rgb(244, 150, 72), red), (Rgb(250, 214, 170), Rgb(236, 170, 130)));
        // A longer mane, flame-red with streaks of cream
        Grow(b, V(1.2f, 1.35f, 1.25f), V(0, 0.5f + lift, 0.16f), s => s.Bone == Body && s.Color.Equals(mane), _ => false);
        // The mane flame-red over the head, neck and back, cream down the chest
        var flame = Rgb(200, 72, 44);
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == Body && s.Color.Equals(mane) && s.Op == SdfOp.Add))
            s.Color = flame;
        b.Sdf.Changed();
        b.PaintEll(Body, V(0, 0.42f + lift, 0.3f), V(0.13f, 0.17f, 0.1f), mane);
        int tailBone = BoneOf(b, "tail");
        b.PaintEll(tailBone, V(0, 0.62f + lift, -0.36f), V(0.1f, 0.08f, 0.12f), flame);
        int head = BoneOf(b, "head");
        // Two horns swept back from its brow, curving up at their ends
        PokeBuilder.Both(s =>
        {
            var root = c + V(0.045f * s, 0.07f, 0.02f);
            b.Tube(head, Smooth(3, root, root + V(0.03f * s, 0.08f, -0.08f), root + V(0.05f * s, 0.17f, -0.12f)), 0.024f, 0.008f, Rgb(214, 190, 150), Shell, 0.008f);
        });
        // A long beard of cream fur
        foreach (float x in new[] { -0.025f, 0f, 0.025f })
            b.Spike(head, V(x, c.Y - 0.075f, 0.33f), V(x * 1.4f, c.Y - 0.2f, 0.34f), 0.026f, mane, 0.6f);
        // Its stripes curled into scrolls on its flanks
        PokeBuilder.Both(s =>
        {
            foreach (float z in new[] { 0.06f, -0.12f })
                b.Mark(Body, Out(V(0, 0.38f + lift, -0.02f), V(0.13f, 0.12f, 0.27f), default, V(s, 0.15f, z * 3f)), V(s, 0.15f, z * 3f), 0.03f, 0.03f, Rgb(40, 38, 48), MarkShape.Ring);
        });
        return b;
    }

    // ------------------------------------------------------------------ 2073 Medusarch, from Tentacruel

    /// <summary>
    /// Medusarch: Tentacruel with its bell raised into a tall sail-like float, blue paled to violet, its three red gems
    /// kept, and its tentacles doubled in number and length, trailing in a cluster beneath.
    /// </summary>
    private static PokeBuilder Medusarch()
    {
        var b = Become(Tentacruel(), "Medusarch", 0.98f);
        var lift = Shifted(b, "body", V(0, 0.66f, 0));
        var violet = Rgb(96, 104, 206);
        var sail = Rgb(120, 146, 226);
        var grey = Rgb(132, 120, 110);
        var navy = Rgb(52, 42, 104);
        Recolour(b, (Rgb(72, 172, 232), violet));
        var c = V(0, 0.68f, -0.02f) + lift;
        // The sail rising high from the float and sweeping back, blue paling to lilac at its ridged edge
        var crest = Smooth(3, c + V(0, 0.06f, 0.04f), c + V(0, 0.3f, 0.02f), c + V(0, 0.46f, -0.1f), c + V(0, 0.42f, -0.28f), c + V(0, 0.26f, -0.44f));
        As(b, Shell, () => Ribbon(b, Body, crest, 0.42f, sail, V(1f, 0, 0), 0.04f));
        for (int i = 2; i < crest.Length - 1; i += 2)
            b.PaintEll(Body, crest[i] + V(0, 0.06f, 0.02f), V(0.05f, 0.05f, 0.08f), Rgb(196, 176, 240), Euler(crest[i + 1] - crest[i - 1]));
        // As many tentacles again, longer, between the old ones
        for (int i = 0; i < 10; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / 10f + 0.31f;
            var out1 = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = V(0, 0.55f, -0.02f) + out1 * 0.1f + lift;
            int arm = b.Part("tentacleB" + i, Body, root, PokeRole.Leaf, i * 0.6f + 0.3f);
            float sag = 0.05f * (i % 3);
            b.Tube(arm, Smooth(3, root, root + out1 * 0.05f + V(0, -0.2f, 0), root + out1 * 0.12f + V(0, -0.38f - sag, 0), root + out1 * 0.2f + V(0, -0.52f - sag, 0)), 0.018f, 0.01f, grey);
        }
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone > Body && s.Color.Equals(grey) && s.Shape == SdfShape.RoundCone).ToList())
            if (b.Model.Skeleton[s.Bone].Name.StartsWith("tentacle") && !b.Model.Skeleton[s.Bone].Name.StartsWith("tentacleB"))
            {
                var root = b.Model.Skeleton[s.Bone].Joint;
                s.A = root + (s.A - root) * V(1f, 1.15f, 1f);
                s.B = root + (s.B - root) * V(1f, 1.15f, 1f);
            }
        // The tentacles darkened to navy, every third one violet
        foreach (var s in b.Sdf.Shapes.Where(s => s.Color.Equals(grey)))
            s.Color = b.Model.Skeleton[s.Bone].Name.EndsWith("3") || b.Model.Skeleton[s.Bone].Name.EndsWith("6") || b.Model.Skeleton[s.Bone].Name.EndsWith("9") ? Rgb(142, 70, 186) : navy;
        b.Sdf.Changed();
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2080 Slowmantle, from Slowbro

    /// <summary>
    /// Slowmantle: Slowbro sat back in the Shellder of its tail grown into a great spiral shell over its back like a
    /// hermit crab's house, grey lined in pink, a pearl gleaming in the shell's mouth.
    /// </summary>
    private static PokeBuilder Slowmantle()
    {
        var b = Become(Slowbro(), "Slowmantle", 0.94f);
        var pink = Rgb(240, 160, 184);
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail && !s.Color.Equals(pink));
        b.Model.Decals.RemoveAll(d => d.Bone == tail);
        var grey = Rgb(160, 160, 166);
        var line = Rgb(232, 140, 136);
        Recolour(b, (pink, Rgb(230, 130, 116)));
        var sc = V(0, 0.5f, -0.2f);
        var sr = V(0.27f, 0.32f, 0.3f);
        Ammonite(b, Body, sc, sr, grey, line, 20f, -10f, -40f, -70f, -100f, -130f, -160f);
        // Broad bands of pink over its back, following its whorl
        foreach (float deg in new[] { 35f, 5f, -25f, -55f, -85f, -115f, -145f })
        {
            float a = deg * Degree;
            var at = Out(sc, sr, default, V(0, MathF.Cos(a), MathF.Sin(a)));
            b.PaintEll(Body, at, V(sr.X * 0.95f, 0.016f, sr.Y * 0.6f), Rgb(224, 150, 150), V(deg + 90f, 0, 0), 0.008f);
        }
        // The shell's mantle reaching forward over its shoulders and the back of its head
        b.Ell(Body, sc + V(0, 0.16f, 0.08f), V(0.25f, 0.12f, 0.16f), grey, V(-20f, 0, 0), Shell, 0.03f);
        b.PaintEll(Body, sc + V(0, 0.22f, 0.12f), V(0.27f, 0.03f, 0.18f), line, V(-20f, 0, 0));
        // The pearl in the shell's dark mouth over its tail
        var mouth = Out(sc, sr, default, V(0, -0.7f, -0.7f));
        b.Ell(Body, mouth + V(0, 0.02f, -0.01f), V(0.1f, 0.08f, 0.04f), Rgb(70, 62, 70), V(-45f, 0, 0), Shell, 0.01f);
        b.Ell(Body, mouth + V(0, 0.03f, 0.0f), V(0.06f, 0.06f, 0.06f), Rgb(246, 246, 252), mat: Shell, blend: 0.006f);
        return b;
    }

    // ------------------------------------------------------------------ 2085 Tripluma, from Dodrio

    /// <summary>
    /// Tripluma: Dodrio with a crest of plumes on each head in its own colour (red, gold and white), a fan of tail
    /// feathers raised like a peacock's, and thicker legs; the female's beige necks kept.
    /// </summary>
    /// <param name="female">A female keeps Dodrio's: beige necks.</param>
    private static PokeBuilder Tripluma(bool female = false)
    {
        var b = Become(Dodrio(female), "Tripluma", 0.97f);
        var plume = Rgb(232, 132, 128);
        var fur = Rgb(192, 136, 96);
        var brown = Rgb(166, 118, 62);
        var neck = female ? Rgb(226, 204, 168) : Rgb(204, 176, 136);
        var oldNeck = female ? Rgb(214, 186, 146) : Rgb(44, 40, 44);
        var beakColor = Rgb(200, 162, 112);
        // Brown feathers, its necks and heads one pale tan, short hooked beaks
        var heads = new[] { BoneOf(b, "headL"), BoneOf(b, "headM"), BoneOf(b, "headR") };
        foreach (var s in b.Sdf.Shapes.ToList())
        {
            if (heads.Contains(s.Bone) && (s.Color.Equals(fur) || s.Color.Equals(oldNeck))) s.Color = neck;
            else if (s.Color.Equals(oldNeck)) s.Color = neck;
            if (heads.Contains(s.Bone) && s.Color.Equals(Rgb(232, 212, 180)) && s.Shape == SdfShape.RoundCone)
            {
                s.B = s.A + (s.B - s.A) * 0.62f;
                s.Size = new Vector3(s.Size.X * 1.35f, s.Size.Y, s.Size.Z);
                var d = Vector3.Normalize(s.B - s.A);
                b.Spike(s.Bone, s.B - d * 0.012f, s.B + V(0, -0.03f, 0) + d * 0.004f, s.Size.X * 0.45f, beakColor, mat: Shell, blend: 0.004f);
            }
        }
        Recolour(b, (Rgb(232, 212, 180), beakColor), (fur, brown), (Rgb(58, 50, 52), Rgb(128, 90, 50)), (Rgb(224, 198, 160), Rgb(112, 98, 74)));
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        // A fan of tail feathers raised high like a peacock's, red, gold and white in turn, each with an eye at its end
        var fan = new[] { Rgb(206, 52, 56), Rgb(226, 186, 72), Rgb(240, 232, 214) };
        var root = V(0, 0.47f, -0.12f);
        for (int i = 0; i < 11; i++)
        {
            float a = (-70f + 140f * i / 10f) * Degree;
            var dir = Vector3.Normalize(V(MathF.Sin(a) * 1.1f, 1.1f * MathF.Cos(a) + 0.2f, -1.0f));
            var tip = root + dir * 0.4f;
            Frond(b, tail, root, tip, 0.055f, fan[i % 3], V(0, 0.4f, -1f), 0.3f, Fur, 0.01f);
            b.PaintEll(tail, root + dir * 0.34f, V(0.035f, 0.045f, 0.035f), fan[(i + 1) % 3], Euler(dir));
        }
        // A great crest of plumes on each head, each in its own colour
        foreach (var (name, at, color) in new[] { ("headL", V(-0.17f, 0.79f, 0.04f), Rgb(184, 30, 50)), ("headM", V(0.0f, 0.9f, -0.02f), Rgb(222, 184, 64)), ("headR", V(0.16f, 0.82f, 0.05f), Rgb(240, 240, 238)) })
        {
            int head = BoneOf(b, name);
            foreach (float x in new[] { -1.5f, -0.75f, 0f, 0.75f, 1.5f })
                b.Spike(head, at + V(0.015f * x, 0.05f, -0.01f), at + V(0.04f * x, 0.24f - MathF.Abs(x) * 0.04f, -0.13f + MathF.Abs(x) * 0.02f), 0.018f, color, 0.5f);
            b.Spike(head, at + V(0, 0.03f, -0.03f), at + V(0, 0.05f, -0.2f), 0.02f, color, 0.5f);
        }
        // Thick, scaled legs
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            Grow(b, V(1.7f, 1f, 1.7f), V(0.055f * s, 0.2f, 0.01f), sh => sh.Bone == leg && sh.Shape == SdfShape.RoundCone && sh.Size.X > 0.012f, _ => false);
        });
        return b;
    }

    // ------------------------------------------------------------------ 2087 Glacitusk, from Dewgong

    /// <summary>
    /// Glacitusk: Dewgong bigger and heavier, its flippers lightly blue, its horn grown into a long spiralled tusk of
    /// ivory, two short tusks below its mouth and a mantle of thick fur at its neck.
    /// </summary>
    private static PokeBuilder Glacitusk()
    {
        var b = Become(Dewgong(), "Glacitusk", 0.96f);
        var lift = Shifted(b, "head", V(0, 0.44f, 0.1f));
        var white = Rgb(238, 242, 250);
        var ivory = Rgb(244, 234, 206);
        int head = BoneOf(b, "head");
        Recolour(b, (Rgb(226, 234, 246), Rgb(84, 142, 160)));
        // The horn grown into a long spiral tusk
        b.Sdf.RemoveAll(s => s.Bone == head && s.Shape == SdfShape.RoundCone && s.Color.Equals(white));
        var foot = V(0, 0.6f, 0.17f) + lift;
        var tip = V(0, 0.8f, 0.42f) + lift;
        b.Spike(head, foot, tip, 0.032f, ivory, mat: Shell);
        Helix(b, head, foot, tip, 0.032f, 3f, 0.007f, Rgb(214, 198, 160));
        // Its small tusks grown longer
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == head && s.Shape == SdfShape.RoundCone && s.Color.Equals(White)).ToList())
        {
            s.B = s.A + (s.B - s.A) * 1.9f;
            s.Size = new Vector3(s.Size.X * 1.3f, s.Size.Y, s.Size.Z);
            s.Color = ivory;
        }
        // A mantle of thick fur round its neck
        var mc = V(0, 0.43f, 0.11f) + lift;
        var mr = V(0.15f, 0.08f, 0.13f);
        b.Ell(Body, mc, mr, white, blend: 0.03f);
        FurTufts(b, Body, mc, mr, 28, 0.045f, 0.024f, white, 0.5f, -0.7f, 0.7f);
        // Shards of ice caught in the mantle
        foreach (var (d, size) in new[] { (V(0.8f, 0.5f, -0.2f), 0.035f), (V(-0.7f, 0.6f, -0.3f), 0.03f), (V(0.4f, 0.7f, -0.6f), 0.028f), (V(-0.9f, 0.1f, 0.2f), 0.026f) })
            b.Box(Body, Out(mc, mr, default, d), V(size, size * 0.7f, size * 0.9f), size * 0.15f, Rgb(206, 236, 250), V(20f, d.X * 40f, 30f), Shell, 0.004f);
        b.Sdf.Changed();
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2089 Sludgemire, from Muk

    /// <summary>
    /// Sludgemire: Muk thickened into tar, black-purple with a gloss and bubbles on its surface, bones and rocks half
    /// sunk in it, its arms thicker and dripping.
    /// </summary>
    private static PokeBuilder Sludgemire()
    {
        var b = Become(Muk(), "Sludgemire", 0.92f);
        const float k = 1.35f;
        var tar = Rgb(46, 38, 58);
        Recolour(b, (Rgb(162, 122, 182), tar), (Rgb(116, 84, 136), Rgb(30, 24, 40)), (Rgb(84, 92, 120), Rgb(24, 18, 28)));
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
        {
            d.White = Rgb(250, 206, 64);
            d.Pupil = Rgb(70, 36, 10);
        }
        var hc = V(0, 0.26f, 0.02f);
        var hr = V(0.26f, 0.21f, 0.21f);
        // Bubbles and blisters on its surface, thickest over its shoulders
        foreach (var (d, size) in new[] { (V(0.6f, 0.6f, -0.3f), 0.03f), (V(-0.5f, 0.8f, -0.2f), 0.024f), (V(0.2f, 0.9f, -0.5f), 0.02f), (V(-0.8f, 0.2f, -0.4f), 0.032f), (V(0.8f, 0.1f, -0.5f), 0.022f),
            (V(-0.3f, 0.9f, -0.3f), 0.026f), (V(0.4f, 0.85f, -0.1f), 0.022f), (V(0f, 0.8f, -0.6f), 0.028f), (V(-0.6f, 0.6f, -0.5f), 0.024f) })
        {
            var at = Out(hc, hr, V(-8f, 0, 0), d);
            b.Ell(Body, at, V(size, size, size), Rgb(70, 58, 92), mat: Scales, blend: 0.006f);
        }
        // Fangs of old bone round its gaping mouth
        var mouth = Out(hc, hr, V(-8f, 0, 0), V(0, -0.05f, 1f));
        for (int i = 0; i < 6; i++)
        {
            float a = -1.1f + i * 0.44f;
            var at = mouth + V(MathF.Sin(a) * 0.07f, (i % 2 == 0 ? 0.1f : -0.1f), -0.02f) * 1.35f;
            b.Spike(Body, at, at + V(0, i % 2 == 0 ? -0.06f : 0.06f, 0.05f), 0.016f, Rgb(214, 196, 150), mat: Shell, blend: 0.008f);
        }
        // A bone and rocks half sunk in the puddle round it
        Club(b, Body, V(0.2f, 0.13f, 0.17f), V(0.33f, 0.1f, 0.03f), 0.018f, Rgb(232, 226, 210));
        foreach (var (at, size) in new[] { (V(-0.28f, 0.1f, 0.1f), 0.05f), (V(-0.15f, 0.12f, -0.22f), 0.045f), (V(0.28f, 0.1f, -0.12f), 0.04f) })
            b.Ell(Body, at, V(size * 1.2f, size * 0.8f, size), Rgb(120, 116, 122), V(0, at.X * 300f, 12f), Shell, 0.008f);
        // Thick arms hanging from heavy shoulders down to the ground, dripping, their hands clawed
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            b.Sdf.RemoveAll(sh => sh.Bone == arm);
            var shoulder = V(0.2f * s, 0.25f, 0.06f) * k;
            var elbow = V(0.3f * s, 0.16f, 0.16f) * k;
            var hand = V(0.33f * s, 0.06f, 0.22f) * k;
            b.Ell(arm, shoulder, V(0.08f, 0.08f, 0.08f) * k, tar, blend: 0.03f);
            b.Tube(arm, Smooth(3, shoulder, elbow, hand), 0.06f * k, 0.045f * k, tar, blend: 0.03f);
            b.Ell(arm, hand, V(0.07f, 0.04f, 0.07f) * k, tar, blend: 0.02f);
            for (int i = -1; i <= 1; i++)
                b.Spike(arm, hand + V(0.035f * i, -0.01f, 0.05f) * k, hand + V(0.05f * i, -0.04f, 0.1f) * k, 0.016f * k, tar, 0.7f, blend: 0.01f);
            foreach (float t in new[] { 0.3f, 0.55f })
            {
                var at = Vector3.Lerp(Vector3.Lerp(shoulder, elbow, t * 2f), Vector3.Lerp(elbow, hand, t * 2f - 1f), t > 0.5f ? 1f : 0f) - V(0, 0.035f, 0) * k;
                b.Spike(arm, at + V(0, 0.02f, 0) * k, at - V(0, 0.05f, 0) * k, 0.018f * k, tar, blend: 0.012f);
            }
        });
        Stretch(b, V(1f, 1.25f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2091 Clamspire, from Cloyster

    /// <summary>
    /// Clamspire: Cloyster's spiked shell grown into a spire of shells grown together like a reef, smaller shells on
    /// its slopes, pale violet with white spikes; its pearl face kept, and a second, smaller face in a side shell.
    /// </summary>
    private static PokeBuilder Clamspire()
    {
        var b = Become(Cloyster(), "Clamspire", 0.96f);
        var lift = Shifted(b, "body", V(0, 0.42f, 0));
        var violet = Rgb(188, 170, 222);
        var spike = Rgb(238, 236, 248);
        var pearl = Rgb(44, 40, 74);
        Recolour(b, (Rgb(142, 132, 180), violet));
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == Body && s.Shape == SdfShape.RoundCone && s.Color.Equals(violet)))
            s.Color = spike;
        var c = V(0, 0.42f, -0.04f) + lift;
        // The spire of shells grown together over it
        foreach (var (at, r, n) in new[] { (V(0, 0.33f, -0.1f), V(0.19f, 0.14f, 0.16f), 5), (V(0, 0.5f, -0.14f), V(0.12f, 0.11f, 0.11f), 4), (V(0, 0.64f, -0.16f), V(0.07f, 0.07f, 0.07f), 3) })
        {
            b.Ell(Body, c + at, r, violet, mat: Shell, blend: 0.03f);
            for (int i = 0; i < n; i++)
            {
                float a = i * MathF.Tau / n + 0.4f;
                var d = Vector3.Normalize(V(MathF.Sin(a), 0.5f, MathF.Cos(a) * 0.8f - 0.3f));
                var root = Out(c + at, r, default, d);
                b.Spike(Body, root - d * 0.02f, root + d * (0.05f + r.X * 0.4f), 0.03f, spike, mat: Shell);
            }
        }
        b.Spike(Body, c + V(0, 0.68f, -0.16f), c + V(0, 0.82f, -0.2f), 0.04f, spike, mat: Shell);
        // A broad foot of shells grown together round its base, the reef it stands on
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f + 0.2f;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            var at = c + V(0, -0.24f, -0.06f) + d * 0.25f;
            var tone = i % 2 == 0 ? PixelCanvas.Light1(violet, 0.12f) : Rgb(176, 184, 226);
            b.Ell(Body, at, V(0.14f, 0.1f, 0.12f), tone, V(0, a / Degree, 20f), Shell, 0.03f);
            b.Spike(Body, at + d * 0.08f + V(0, 0.03f, 0), at + d * 0.18f + V(0, 0.08f, 0), 0.026f, spike, mat: Shell);
        }
        // Small shells on its slopes, one of them open on a second, smaller face
        foreach (var (at, size) in new[] { (V(-0.27f, 0.12f, 0.02f), 0.07f), (V(0.2f, -0.2f, 0.1f), 0.06f), (V(-0.18f, 0.28f, -0.12f), 0.055f) })
            b.Ell(Body, c + at, V(size * 1.2f, size, size), PixelCanvas.Light1(violet, 0.12f), V(0, 0, 20f), Shell, 0.015f);
        var side = c + V(0.28f, 0.12f, 0.06f);
        b.Ell(Body, side, V(0.09f, 0.085f, 0.08f), PixelCanvas.Light1(violet, 0.12f), mat: Shell, blend: 0.02f);
        var face = side + V(0.01f, 0, 0.05f);
        var faceR = V(0.065f, 0.06f, 0.05f);
        b.Ell(Body, face, faceR, pearl, mat: Shell, blend: 0.006f);
        PokeBuilder.Both(s => b.Eye(Body, On(face, faceR, face.X + 0.024f * s, face.Y + 0.008f), V(0.3f * s, 0.05f, 1f), 0.018f, sclera: true, pupil: Black));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2097 Mesmeryx, from Hypno

    /// <summary>
    /// Mesmeryx: Hypno taller and thinner, its yellow paled, its collar grown into a high ruff (the female's fuller
    /// still), and its pendulum become a ring of three coins circling on a thin chain round its hand.
    /// </summary>
    /// <param name="female">A female keeps Hypno's: a fuller ruff.</param>
    private static PokeBuilder Mesmeryx(bool female = false)
    {
        var b = Become(Hypno(female), "Mesmeryx", 0.96f);
        var pale = Rgb(240, 222, 150);
        var ruff = Rgb(246, 246, 240);
        Recolour(b, (Rgb(250, 214, 82), pale));
        // A great ruff of pale lilac-white fluff round the back of its head, a cloud of it rising behind
        var rc = V(0, 0.64f, 0);
        float f = female ? 1.2f : 1f;
        var cloud = Rgb(240, 236, 248);
        b.Ell(Body, rc + V(0, 0.12f, -0.07f), V(0.17f, 0.17f, 0.09f) * f, cloud, blend: 0.03f);
        FurTufts(b, Body, rc + V(0, 0.12f, -0.07f), V(0.17f, 0.17f, 0.09f) * f, 30, 0.05f, 0.026f, cloud, 0.1f, -0.9f, 0.4f);
        for (int i = 0; i < 9; i++)
        {
            float a = (-120f + i * 30f) * Degree;
            var foot = rc + V(MathF.Sin(a) * 0.13f * f, 0.02f, -MathF.Cos(a) * 0.1f * f);
            var outward = Vector3.Normalize(V(MathF.Sin(a), 0, -MathF.Cos(a)));
            Blade(b, Body, foot, foot + outward * 0.08f * f + V(0, 0.26f * f - MathF.Abs(MathF.Sin(a)) * 0.05f, 0), 0.06f * f, ruff, outward, 0.24f);
        }
        // Forearms and hands gone dark, as if gloved in shadow, and a dark streak down its flanks
        var shadow = Rgb(46, 42, 74);
        PokeBuilder.Both(s =>
        {
            int a = BoneOf(b, s < 0 ? "armL" : "armR");
            var hand = s > 0 ? V(0.27f, 0.74f, 0.06f) : V(-0.3f, 0.66f, 0.08f);
            var elbow = V(0.23f * s, 0.6f, 0.02f);
            b.PaintEll(a, (elbow + hand) / 2f + V(0.02f * s, 0, 0), V(0.06f, 0.09f, 0.06f), shadow, Euler(hand - elbow));
            b.PaintEll(a, hand, V(0.05f, 0.05f, 0.05f), shadow);
            b.PaintEll(Body, V(0.1f * s, 0.46f, -0.02f), V(0.04f, 0.13f, 0.08f), shadow);
        });
        // Its ears grown tall, tipped in shadow
        PokeBuilder.Both(s =>
        {
            int ear = BoneOf(b, s < 0 ? "earL" : "earR");
            Grow(b, V(1f, 1.35f, 1f), V(0.06f * s, 0.85f, 0), bone => bone == ear);
            b.PaintEll(ear, V(0.115f * s, 1.03f, -0.025f), V(0.04f, 0.05f, 0.04f), shadow);
        });
        // A long thin tail ending in a dark tuft
        int tail = b.Tail(V(0, 0.4f, -0.08f));
        var tailPath = Smooth(3, V(0, 0.4f, -0.08f), V(0.05f, 0.22f, -0.2f), V(0.12f, 0.1f, -0.3f), V(0.2f, 0.18f, -0.4f), V(0.22f, 0.34f, -0.44f));
        b.Tube(tail, tailPath, 0.022f, 0.014f, pale, blend: 0.01f);
        FurTufts(b, tail, tailPath[^1] + V(0, 0.03f, 0), V(0.03f, 0.04f, 0.03f), 10, 0.04f, 0.016f, shadow, 2f, -0.6f, 0.2f);
        b.Ell(tail, tailPath[^1] + V(0, 0.03f, 0), V(0.03f, 0.04f, 0.03f), shadow, blend: 0.01f);
        // The ring of three dark coins on a thin chain round its hand
        int arm = BoneOf(b, "armR");
        b.Sdf.RemoveAll(s => s.Bone == arm && (s.Color.Equals(Rgb(200, 200, 210)) || s.Color.Equals(Rgb(200, 204, 216))));
        var grip = V(0.27f, 0.74f, 0.06f);
        var ring = grip - V(0, 0.13f, 0);
        var chain = Rgb(96, 90, 112);
        b.Tube(arm, new[] { grip - V(0, 0.03f, -0.01f), ring + V(0, 0.005f, 0.08f) }, 0.008f, 0.008f, chain, Metal, 0f);
        b.Torus(arm, ring, 0.08f, 0.008f, chain, mat: Metal, blend: 0f);
        for (int i = 0; i < 3; i++)
        {
            float a = (90f + i * 120f) * Degree;
            var at = ring + V(MathF.Cos(a) * 0.08f, 0, MathF.Sin(a) * 0.08f);
            b.Ell(arm, at, V(0.032f, 0.032f, 0.009f), Rgb(84, 84, 96), V(0, -a / Degree + 90f, 0), Metal, 0.004f);
        }
        Stretch(b, V(0.84f, 1.12f, 0.88f));
        return b;
    }

    /// <summary>Gives the shapes <paramref name="add"/> lays down the material <paramref name="mat"/> (a ribbon of light, made by a helper that lays fur).</summary>
    private static void As(PokeBuilder b, SurfaceMaterial mat, Action add)
    {
        int before = b.Sdf.Shapes.Count;
        add();
        for (int i = before; i < b.Sdf.Shapes.Count; i++) b.Sdf.Shapes[i].Material = mat;
    }

    // ------------------------------------------------------------------ 2101 Dynamorb, from Electrode

    /// <summary>
    /// Dynamorb: Electrode larger, its red and white halves divided by a band of steel set with three studs, two arcs of
    /// current circling it on thin strands.
    /// </summary>
    private static PokeBuilder Dynamorb()
    {
        var b = Become(Electrode(), "Dynamorb", 0.78f);
        var c = V(0, 0.3f, 0);
        // Its halves turned about: red above, where the glowing eyes are, white below; no grin
        var red = Rgb(196, 62, 70);
        Recolour(b, (Rgb(242, 242, 246), red), (Rgb(226, 66, 74), Rgb(236, 236, 240)));
        b.Sdf.RemoveAll(s => s.Color.Equals(Rgb(150, 40, 56)) || s.Op == SdfOp.Cut);
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
        {
            d.White = Rgb(226, 250, 120);
            d.Pupil = Rgb(246, 255, 200);
        }
        // A heavy band of dark steel round its middle, bolted, and vents in the red above it
        var band = Rgb(104, 106, 114);
        b.Torus(Body, c, 0.3f, 0.034f, band, mat: Metal, blend: 0.008f);
        for (int i = 0; i < 8; i++)
        {
            float r = (i * 45f + 22.5f) * Degree;
            b.Ell(Body, c + V(MathF.Sin(r) * 0.335f, 0, MathF.Cos(r) * 0.335f), V(0.024f, 0.024f, 0.024f), Rgb(140, 142, 150), mat: Metal, blend: 0.004f);
        }
        foreach (float a in new[] { -40f, 40f, 140f, -140f })
        {
            var n = V(MathF.Sin(a * Degree), 0.45f, MathF.Cos(a * Degree));
            b.Mark(Body, Out(c, V(0.3f, 0.3f, 0.3f), default, n), n, 0.035f, 0.016f, Rgb(70, 66, 74), MarkShape.Bar);
        }
        // Two rings of grey wire circling it, held off the ball by two strands, sparks of current along them
        var wire = Rgb(150, 154, 164);
        var current = Rgb(250, 242, 150);
        foreach (var turn in new[] { V(28f, 0, 24f), V(-28f, 0, -24f) })
        {
            var q = Quaternion.CreateFromYawPitchRoll(turn.Y * Degree, turn.X * Degree, turn.Z * Degree);
            b.Torus(Body, c, 0.42f, 0.011f, wire, turn, mat: Metal, blend: 0f);
            foreach (var local in new[] { V(0.42f, 0, 0), V(-0.42f, 0, 0) })
            {
                var p = c + Vector3.Transform(local, q);
                b.Tube(Body, new[] { c + (p - c) * 0.7f, p }, 0.007f, 0.007f, wire, Metal, 0f);
            }
            foreach (float a in new[] { 0.5f, 2.2f, 3.9f, 5.4f })
                b.Ell(Body, c + Vector3.Transform(V(MathF.Cos(a) * 0.42f, 0, MathF.Sin(a) * 0.42f), q), V(0.022f, 0.022f, 0.022f), current, mat: Glow, blend: 0.004f);
        }
        return b;
    }

    // ------------------------------------------------------------------ 2103 Palmagus, from Exeggutor

    /// <summary>
    /// Palmagus: Exeggutor grown into a grove: two trunks rise from one root, each with its crown of leaves and three
    /// coconut heads, and a fourth head grows low at the join.
    /// </summary>
    private static PokeBuilder Palmagus()
    {
        var b = new PokeBuilder("Palmagus", 0.97f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Leaf };
        var bark = Rgb(124, 116, 84);
        var ring = PixelCanvas.Mix(bark, Rgb(60, 70, 40), 0.35f);
        var shell = Rgb(150, 108, 70);
        var leaf = Rgb(46, 126, 104);
        var cream = Rgb(150, 140, 104);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.2f, 0));
            b.Limb(leg, V(0.1f * s, 0.21f, 0), V(0.12f * s, 0.06f, 0.03f), 0.075f, 0.065f, bark);
            b.Ell(leg, V(0.125f * s, 0.035f, 0.06f), V(0.075f, 0.035f, 0.085f), cream);
            for (int i = -1; i <= 1; i++)
                b.Spike(leg, V(0.125f * s + 0.035f * i, 0.025f, 0.125f), V(0.125f * s + 0.045f * i, 0.016f, 0.16f), 0.014f, White, mat: Shell, blend: 0.004f);
        });
        b.Ell(Body, V(0, 0.32f, 0), V(0.18f, 0.18f, 0.15f), bark);
        foreach (float y in new[] { 0.22f, 0.29f, 0.36f })
            b.PaintTorus(Body, V(0, y, 0), 0.18f * MathF.Sqrt(MathF.Max(0.05f, 1f - MathF.Pow((y - 0.32f) / 0.18f, 2))), 0.016f, ring, sz: 0.83f);
        int tail = b.Tail(V(0, 0.2f, -0.11f));
        b.Limb(tail, V(0, 0.2f, -0.1f), V(0, 0.08f, -0.26f), 0.055f, 0.03f, bark);
        // The fourth head, low at the join
        Coconut(b, Body, V(0, 0.42f, 0.12f), 0.085f, shell, V(0, 0, 1f), true, false, false);
        // Two trunks, each with its crown
        PokeBuilder.Both(s =>
        {
            int trunk = b.Part(s < 0 ? "trunkL" : "trunkR", Body, V(0.06f * s, 0.42f, -0.02f), PokeRole.Segment, s < 0 ? 0.2f : 1.4f, s);
            var spine = Smooth(3, V(0.05f * s, 0.4f, -0.02f), V(0.1f * s, 0.58f, -0.03f), V(0.16f * s, 0.74f, -0.02f), V(0.2f * s, 0.86f, -0.01f));
            b.Tube(trunk, spine, 0.065f, 0.05f, bark, blend: 0f);
            for (int i = 2; i < spine.Length - 1; i += 3)
                b.PaintTorus(trunk, spine[i], 0.062f - 0.002f * i, 0.012f, ring, Euler(spine[i + 1] - spine[i - 1]));
            var top = V(0.2f * s, 0.88f, -0.01f);
            int crown = b.Part(s < 0 ? "crownL" : "crownR", trunk, top, PokeRole.Head, s < 0 ? 0.6f : 2.2f, s);
            b.Ell(crown, top, V(0.09f, 0.07f, 0.08f), bark, blend: 0.04f);
            Coconut(b, crown, top + V(-0.08f, 0.05f, 0.05f), 0.085f, shell, V(-0.4f + 0.5f * s, 0, 1f), s < 0, s > 0, false);
            Coconut(b, crown, top + V(0.08f, 0.06f, 0.04f), 0.085f, shell, V(0.4f + 0.5f * s, 0, 1f), false, false, false);
            Coconut(b, crown, top + V(0.0f, 0.07f, -0.11f), 0.078f, shell, V(0.85f * s, 0.15f, 0.3f), false, false, s > 0);
            var root0 = top + V(0, 0.15f, -0.03f);
            int leaves = b.Part(s < 0 ? "leavesL" : "leavesR", crown, root0, PokeRole.Leaf, s);
            b.Ell(leaves, root0, V(0.065f, 0.055f, 0.065f), leaf);
            for (int i = 0; i < 9; i++)
            {
                float a = i * MathF.Tau / 9f + 0.2f;
                var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
                float front = MathF.Max(0f, dir.Z);
                float reach = 0.4f - 0.15f * front + 0.03f * (i % 3), rise = 0.3f + 0.1f * front;
                var root = root0 + dir * 0.03f;
                LongLeaf(b, leaves, root, root + dir * reach * 0.35f + V(0, rise * 0.8f, 0), root + dir * reach * 1.1f + V(0, rise * (0.1f - 0.15f * (i % 2)), 0), 0.05f, i % 3 == 1 ? Rgb(112, 82, 136) : leaf, 0.012f);
            }
        });
        // Its heads' eyes glow violet
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
        {
            d.White = Rgb(214, 150, 250);
            d.Pupil = Rgb(244, 220, 255);
            if (d.Iris is { }) d.Iris = Rgb(214, 150, 250);
        }
        // The low head wears a long beard of fibre
        foreach (float x in new[] { -0.035f, 0f, 0.035f })
            b.Spike(Body, V(x, 0.38f, 0.18f), V(x * 1.3f, 0.24f, 0.2f), 0.03f, Rgb(122, 92, 62), 0.6f, blend: 0.012f);
        // Twisting moss on its trunks, and roots spreading from its feet
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            foreach (var (dx, dz) in new[] { (0.1f, 0.06f), (0.04f, -0.08f), (-0.02f, 0.1f) })
                b.Tube(leg, Smooth(3, V(0.12f * s, 0.1f, 0.03f), V((0.12f + dx * 0.6f) * s, 0.04f, 0.03f + dz * 0.6f), V((0.12f + dx * 1.3f) * s, 0.012f, 0.03f + dz * 1.3f)), 0.034f, 0.014f, bark, blend: 0.01f);
            int trunk = BoneOf(b, s < 0 ? "trunkL" : "trunkR");
            b.PaintEll(trunk, V(0.11f * s, 0.6f, 0.04f), V(0.05f, 0.06f, 0.05f), Rgb(96, 132, 62));
        });
        return b;
    }

    // ------------------------------------------------------------------ 2106 Savateur, from Hitmonlee

    /// <summary>Savateur: Hitmonlee taller still, its legs longer and springier, a short cape of brown hide at its shoulders and wraps round its ankles.</summary>
    private static PokeBuilder Savateur()
    {
        var b = Become(Hitmonlee(), "Savateur", 0.95f);
        var hide = Rgb(124, 86, 58);
        var wrap = Rgb(240, 228, 196);
        var gold = Rgb(206, 156, 86);
        // A golden hide, its springs of the same skin banded darker
        Recolour(b, (Rgb(182, 148, 120), gold), (Rgb(236, 220, 170), Rgb(178, 124, 66)));
        // A lean torso narrowing to the waist
        Grow(b, V(0.78f, 1f, 0.8f), V(0, 0.64f, 0), s => s.Bone == Body, bone => bone == Body);
        // Arms hanging long at its sides, clawed
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            b.Sdf.RemoveAll(sh => sh.Bone == arm);
            var shoulder = V(0.11f * s, 0.62f, 0);
            var elbow = V(0.17f * s, 0.46f, 0.02f);
            var hand = V(0.2f * s, 0.32f, 0.05f);
            b.Limb(arm, shoulder, elbow, 0.034f, 0.028f, gold);
            b.Limb(arm, elbow, hand, 0.028f, 0.024f, gold);
            b.Ell(arm, hand, V(0.03f, 0.035f, 0.028f), gold);
            Claws(b, arm, hand + V(0, -0.025f, 0.01f), V(0.012f, 0, 0), V(0.1f * s, -1f, 0.3f), 0.04f, 0.008f);
        });
        // A crest of long golden plumes swept up and back from its head
        foreach (var (x, h, back) in new[] { (0f, 0.22f, 0.1f), (-0.04f, 0.17f, 0.12f), (0.04f, 0.17f, 0.12f), (-0.07f, 0.1f, 0.1f), (0.07f, 0.1f, 0.1f) })
            Blade(b, Body, V(x * 0.6f, 0.8f, 0.01f), V(x * 1.4f, 0.8f + h, -back), 0.045f, PixelCanvas.Light1(gold, 0.1f), V(0, 0.2f, 1f), 0.3f);
        // The cape of brown feathers over its shoulders, falling down its back and sides
        b.Ell(Body, V(0, 0.72f, -0.02f), V(0.16f, 0.05f, 0.11f), hide, V(10f, 0, 0), blend: 0.02f);
        for (int i = 0; i < 7; i++)
        {
            float a = (-80f + 160f * i / 6f) * Degree;
            var root = V(MathF.Sin(a) * 0.13f, 0.72f, -MathF.Cos(a) * 0.09f);
            var outward = Vector3.Normalize(V(MathF.Sin(a), 0, -MathF.Cos(a)));
            Blade(b, Body, root, root + outward * 0.06f + V(0, -0.26f + MathF.Abs(MathF.Sin(a)) * 0.06f, 0), 0.07f, hide, outward, 0.22f);
        }
        // Thick wraps of pale cloth round its ankles, fur at their tops
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            var ankle = V(0.11f * s, 0.07f, 0.03f);
            var dir = Vector3.Normalize(V(0.06f * s, 0.47f, 0) - ankle);
            b.Limb(leg, ankle + dir * 0.0f, ankle + dir * 0.13f, 0.05f, 0.048f, wrap, blend: 0.008f);
            b.Torus(leg, ankle + dir * 0.13f, 0.048f, 0.016f, Rgb(250, 244, 226), Euler(dir), blend: 0.006f);
            foreach (float t in new[] { 0.03f, 0.07f, 0.11f })
                b.PaintTorus(leg, ankle + dir * t, 0.05f, 0.005f, Rgb(214, 200, 168), Euler(dir + V(0.2f * s, 0, 0)));
        });
        Stretch(b, V(1f, 1.1f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2107 Pugilord, from Hitmonchan

    /// <summary>Pugilord: Hitmonchan broader in the chest, its red gloves become gauntlets of steel banded red, a gold champion's belt at its waist and a high collar.</summary>
    private static PokeBuilder Pugilord()
    {
        var b = Become(Hitmonchan(), "Pugilord", 0.92f);
        var glove = Rgb(222, 64, 64);
        var gold = Rgb(232, 186, 66);
        var tunic = Rgb(214, 210, 236);
        var armour = Rgb(138, 146, 160);
        var red = Rgb(196, 52, 54);
        var skin = Rgb(190, 156, 122);
        int head = BoneOf(b, "head");
        // The crest of points goes under a helmet
        b.Sdf.RemoveAll(s => s.Bone == head && s.Shape == SdfShape.RoundCone && s.Color.Equals(skin));
        Grow(b, V(1.22f, 1.04f, 1.15f), V(0, 0.55f, 0), s => s.Bone == Body && s.Color.Equals(tunic), _ => false);
        // Armour of steel trimmed red and gold where the tunic was, gauntlets of steel banded red for gloves
        Recolour(b, (tunic, armour), (Rgb(198, 194, 234), armour), (glove, Steel), (White, red));
        foreach (var s in b.Sdf.Shapes.Where(s => s.Color.Equals(Steel) || s.Color.Equals(armour))) s.Material = Metal;
        b.PaintEll(Body, V(0, 0.63f, 0.09f), V(0.14f, 0.03f, 0.05f), red, V(10f, 0, 0));
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var fist = V(0.17f * s, 0.67f, 0.16f);
            var elbow = V(0.21f * s, 0.53f, 0.05f);
            Grow(b, V(1.45f, 1.45f, 1.45f), fist, sh => sh.Bone == arm && sh.Color.Equals(Steel), _ => false);
            var d = Vector3.Normalize(fist - elbow);
            foreach (float t in new[] { -0.07f, -0.03f })
                b.Torus(arm, fist + d * t, 0.07f, 0.013f, red, Euler(d), mat: Shell, blend: 0.004f);
            // Gold knuckle plates
            for (int k = -1; k <= 1; k++)
                b.Box(arm, fist + V(0.022f * k, 0.035f, 0.095f), V(0.016f, 0.014f, 0.012f), 0.004f, gold, default, Metal, 0.004f);
            // Plates over the shoulders, red at their edge, and red pads on its knees
            b.Ell(Body, V(0.13f * s, 0.66f, 0), V(0.06f, 0.035f, 0.06f), armour, V(0, 0, -25f * s), Metal, 0.012f);
            b.PaintEll(Body, V(0.16f * s, 0.65f, 0), V(0.03f, 0.04f, 0.065f), red);
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            b.PaintEll(leg, V(0.095f * s, 0.12f, 0.02f), V(0.05f, 0.08f, 0.05f), armour);
            b.Ell(leg, V(0.1f * s, 0.22f, 0.06f), V(0.034f, 0.03f, 0.02f), red, mat: Shell, blend: 0.008f);
        });
        // The champion's belt, broad, its plate in front
        b.Torus(Body, V(0, 0.46f, 0), 0.15f, 0.034f, gold, sz: 0.8f, mat: Metal, blend: 0.006f);
        b.Ell(Body, V(0, 0.46f, 0.13f), V(0.06f, 0.045f, 0.02f), gold, mat: Metal, blend: 0.004f);
        b.Ell(Body, V(0, 0.46f, 0.145f), V(0.028f, 0.022f, 0.008f), Rgb(250, 214, 110), mat: Metal, blend: 0.002f);
        // A high collar
        b.Torus(Body, V(0, 0.665f, -0.005f), 0.08f, 0.024f, armour, V(-12f, 0, 0), sz: 0.9f, mat: Metal, blend: 0.01f);
        b.Ell(Body, V(0, 0.69f, -0.06f), V(0.085f, 0.045f, 0.035f), armour, mat: Metal, blend: 0.015f);
        // A helmet of steel, a visor of gold over its eyes and a crest of red and gold
        var c = V(0, 0.79f, 0.02f);
        b.Ell(head, c + V(0, 0.035f, -0.012f), V(0.1f, 0.085f, 0.09f), armour, mat: Metal, blend: 0.01f);
        b.Ell(head, c + V(0, 0.045f, 0.07f), V(0.075f, 0.016f, 0.03f), gold, V(-10f, 0, 0), Metal, 0.006f);
        b.Ell(head, c + V(0, 0.11f, -0.01f), V(0.022f, 0.05f, 0.08f), red, mat: Shell, blend: 0.01f);
        b.Ell(head, c + V(0, 0.13f, 0.03f), V(0.024f, 0.035f, 0.03f), gold, mat: Metal, blend: 0.008f);
        return b;
    }

    // ------------------------------------------------------------------ 2110 Smoglom, from Weezing

    /// <summary>
    /// Smoglom: Weezing joined by a third, smallest ball, all three ringed with a thin band of fire at their seams,
    /// smoke rising from vents on their tops.
    /// </summary>
    private static PokeBuilder Smoglom()
    {
        var b = Become(Weezing(), "Smoglom", 0.88f);
        var lift = Shifted(b, "small", V(0.13f, 0.42f, -0.04f));
        var purple = Rgb(68, 62, 76);
        var smoke = Rgb(112, 92, 124);
        Recolour(b, (Rgb(126, 106, 168), purple), (Rgb(214, 206, 150), smoke), (PixelCanvas.Mix(Rgb(126, 106, 168), Rgb(30, 24, 40), 0.25f), Rgb(48, 44, 54)));
        // Glowing ember eyes
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
        {
            d.White = Rgb(255, 196, 70);
            d.Pupil = Rgb(250, 130, 40);
        }
        // Its skin rough as cinder, pocked with paler lumps
        foreach (var (d, size) in new[] { (V(0.5f, 0.7f, 0.3f), 0.03f), (V(-0.6f, 0.5f, 0.2f), 0.028f), (V(0.2f, -0.6f, 0.6f), 0.03f), (V(-0.3f, 0.9f, -0.2f), 0.032f), (V(0.8f, 0.2f, -0.4f), 0.03f), (V(-0.8f, -0.3f, -0.3f), 0.028f) })
            b.Ell(Body, Out(V(-0.08f, 0.32f, 0) + lift, V(0.2f, 0.19f, 0.19f), default, d), V(size, size * 0.7f, size), Rgb(86, 80, 96), mat: Shell, blend: 0.012f);
        var cBig = V(-0.08f, 0.32f, 0) + lift;
        var cSmall = V(0.19f, 0.42f, -0.04f) + lift;
        var cThird = V(-0.26f, 0.52f, -0.08f) + lift;
        var rThird = V(0.095f, 0.095f, 0.09f);
        int third = b.Part("third", Body, cThird + V(0.05f, -0.04f, 0), PokeRole.Head, 2.6f, -1f);
        b.Ell(third, cThird, rThird, purple);
        b.Ell(Body, (cBig + cThird) * 0.5f, V(0.06f, 0.06f, 0.06f), purple, blend: 0.05f);
        PokeBuilder.Both(s => b.Eye(third, Out(cThird, rThird, default, V(-0.2f + 0.4f * s, 0.15f, 1f)), V(-0.2f + 0.4f * s, 0.15f, 1f), 0.02f, sclera: true, white: Rgb(255, 196, 70), pupil: Rgb(250, 130, 40), glare: true));
        b.Torus(third, Out(cThird, rThird, default, V(-0.1f, -0.35f, 1f)), 0.014f, 0.006f, Rgb(90, 84, 100), Euler(V(-0.1f, -0.35f, 1f)));
        // Bands of fire at the seams
        var fire = Rgb(250, 140, 50);
        foreach (var (a, z, bone) in new[] { (cBig, cSmall, Body), (cBig, cThird, Body) })
        {
            var d = Vector3.Normalize(z - a);
            var mid = a + (z - a) * 0.52f;
            b.Torus(bone, mid, 0.1f, 0.014f, fire, Euler(d), mat: Glow, blend: 0.008f);
            // Cracks of lava running out from the seam over the bigger ball
            foreach (var (off, len, roll) in new[] { (V(0, 0.06f, 0.06f), 0.07f, 20f), (V(0, -0.06f, 0.07f), 0.06f, -25f), (V(0, 0.02f, 0.1f), 0.05f, 60f) })
                PaintAs(b, Body, a + (z - a) * 0.38f + off, V(0.012f, len, 0.03f), fire, Glow, V(0, 0, roll));
        }
        // Vents on their tops, smoke rising from them
        foreach (var (at, size, bone) in new[] { (cBig + V(0.02f, 0.19f, -0.02f), 0.03f, Body), (cSmall + V(0.01f, 0.13f, -0.02f), 0.024f, BoneOf(b, "small")), (cThird + V(0, 0.09f, -0.01f), 0.02f, third) })
        {
            b.Limb(bone, at - V(0, 0.02f, 0), at + V(0, 0.03f, 0), size, size * 0.9f, Rgb(70, 52, 80));
            GasPuff(b, bone, at + V(0, 0.03f + size * 0.5f, 0), V(0.2f, 1f, 0), size * 1.2f, smoke);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2115 Matriroo, from Kangaskhan

    /// <summary>Matriroo: Kangaskhan taller, its helmet grown into a crest, and the child in its pouch older and wearing a helmet of its own.</summary>
    private static PokeBuilder Matriroo()
    {
        var b = Become(Kangaskhan(), "Matriroo", 0.97f);
        // A warmer brown hide, its helmet and crest pale as bone
        var dark = Rgb(226, 212, 178);
        Recolour(b, (Rgb(92, 70, 62), dark), (Rgb(160, 124, 104), Rgb(176, 136, 88)));
        int head = BoneOf(b, "head");
        var c = V(0, 0.9f, 0.08f);
        Blade(b, head, c + V(0, 0.07f, 0.04f), c + V(0, 0.2f, -0.17f), 0.11f, dark, V(1f, 0, 0), 0.25f, Shell);
        // The child grown older, its own helmet on
        int child = BoneOf(b, "baby");
        var bc = V(0, 0.53f, 0.2f);
        Grow(b, V(1.3f, 1.3f, 1.3f), bc - V(0, 0.04f, 0), bone => bone == child);
        var nc = bc - V(0, 0.04f, 0) + V(0, 0.04f, 0) * 1.3f;
        b.Ell(child, nc + V(0, 0.05f, -0.004f), V(0.088f, 0.04f, 0.082f), dark, mat: Shell, blend: 0.012f);
        Stretch(b, V(1f, 1.06f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2119 Sabrefin, from Seaking

    /// <summary>
    /// Sabrefin: Seaking longer and sleeker, its horn grown into a straight sword's blade, patched like a koi, its fins
    /// flowing longer; the female's shorter horn kept as a shorter blade.
    /// </summary>
    /// <param name="female">A female keeps Seaking's: a shorter blade.</param>
    private static PokeBuilder Sabrefin(bool female = false)
    {
        var b = Become(Seaking(female), "Sabrefin", 0.9f);
        var lift = Shifted(b, "head", V(0, 0.45f, 0.15f));
        int head = BoneOf(b, "head");
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.Equals(Rgb(250, 236, 200)));
        // A pearl-white koi, its fins long, flowing and peach-pink
        var pearl = Rgb(248, 240, 244);
        var peach = Rgb(248, 212, 200);
        var navy = Rgb(40, 52, 86);
        Recolour(b, (Rgb(238, 100, 52), pearl), (Rgb(244, 242, 240), peach), (Rgb(50, 44, 50), navy));
        foreach (var name in new[] { "dorsal", "tailFin", "finL", "finR" })
        {
            int finBone = BoneOf(b, name);
            b.Model.Decals.RemoveAll(d => d.Bone == finBone);
        }
        // Its horn grown into a long spiralled lance of ivory
        var foot = V(0, 0.6f, 0.22f) + lift;
        var tip = (female ? V(0, 0.8f, 0.4f) : V(0, 0.92f, 0.52f)) + lift;
        b.Spike(head, foot, tip, 0.04f, Rgb(244, 238, 224), mat: Shell);
        Helix(b, head, foot, tip, 0.04f, 4f, 0.007f, Rgb(214, 202, 180));
        // Patched like a koi, orange and navy over its back and head
        foreach (var (at, size, color) in new[] { (V(0.1f, 0.52f, 0.1f), 0.07f, Rgb(240, 132, 64)), (V(-0.1f, 0.54f, -0.08f), 0.08f, Rgb(240, 132, 64)), (V(0.09f, 0.46f, -0.2f), 0.06f, Rgb(240, 132, 64)), (V(0f, 0.62f, 0.14f), 0.06f, Rgb(240, 132, 64)),
            (V(0.1f, 0.42f, -0.06f), 0.03f, navy), (V(-0.09f, 0.48f, 0.08f), 0.028f, navy), (V(0.06f, 0.6f, -0.02f), 0.025f, navy), (V(-0.08f, 0.38f, -0.2f), 0.026f, navy) })
            b.PaintEll(Body, at + lift, V(size * 0.6f, size, size * 1.2f), color);
        // Long flowing fins like a veil, peach paling to blue at their ends
        int dorsal = BoneOf(b, "dorsal");
        int fin = BoneOf(b, "tailFin");
        int finL = BoneOf(b, "finL"), finR = BoneOf(b, "finR");
        b.Sdf.RemoveAll(s => s.Bone == dorsal || s.Bone == fin || s.Bone == finL || s.Bone == finR);
        var veil = Rgb(248, 214, 204);
        var veilEnd = Rgb(214, 228, 246);
        void Veil(int bone, Vector3[] path, float width, Vector3 facing)
        {
            var p = path.Select(q => q + lift).ToArray();
            Ribbon(b, bone, p, width, veil, facing, 0.014f);
            b.PaintEll(bone, p[^2], V(width * 1.2f, width * 1.2f, width * 1.2f), veilEnd);
        }
        Veil(dorsal, Smooth(3, V(0, 0.62f, 0.06f), V(0, 0.72f, -0.06f), V(0, 0.76f, -0.2f), V(0, 0.7f, -0.34f)), 0.1f, V(1f, 0, 0));
        foreach (var (up, len) in new[] { (0.22f, 0.5f), (0.02f, 0.46f), (-0.2f, 0.5f) })
            Veil(fin, Smooth(3, V(0, 0.45f, -0.3f), V(0, 0.45f + up * 0.5f, -0.3f - len * 0.4f), V(0, 0.45f + up, -0.3f - len * 0.75f), V(0, 0.45f + up * 1.3f, -0.3f - len)), 0.1f, V(1f, 0, 0));
        PokeBuilder.Both(s =>
        {
            int side = s < 0 ? finL : finR;
            Veil(side, Smooth(3, V(0.1f * s, 0.36f, 0.06f), V(0.16f * s, 0.28f, 0.0f), V(0.2f * s, 0.18f, -0.08f), V(0.22f * s, 0.08f, -0.14f)), 0.07f, V(s, 0.3f, 0));
            Veil(side, Smooth(3, V(0.06f * s, 0.32f, -0.12f), V(0.08f * s, 0.24f, -0.2f), V(0.1f * s, 0.16f, -0.28f)), 0.05f, V(s, 0.2f, 0));
        });
        Stretch(b, V(0.92f, 1f, 1.15f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2121 Stellamir, from Starmie

    /// <summary>Stellamir: Starmie with a third, smaller star turning between its two, all three round the core, its gem holding a swirl of light.</summary>
    private static PokeBuilder Stellamir()
    {
        var b = Become(Starmie(), "Stellamir", 0.9f);
        var lift = Shifted(b, "body", V(0, 0.42f, 0));
        var c = V(0, 0.42f, 0) + lift;
        // Deep blue-violet stars, a third of glowing teal turning between them like webbing
        var purple = Rgb(132, 118, 190);
        Recolour(b, (purple, Rgb(110, 84, 196)), (PixelCanvas.Mix(purple, Rgb(60, 50, 110), 0.25f), Rgb(52, 58, 140)));
        int star = b.Part("star3", Body, c, PokeRole.Leaf, 1.2f);
        StarPoints(b, star, c - V(0, 0, 0.015f), 0.44f, 0.12f, 18f, 0.42f, Rgb(70, 192, 196));
        b.Ell(star, c - V(0, 0, 0.015f), V(0.1f, 0.1f, 0.05f), Rgb(70, 192, 196), blend: 0.02f);
        // The gem's swirl of light, pink and violet
        Recolour(b, (Rgb(214, 48, 78), Rgb(236, 120, 200)));
        var gem = c + V(0, 0, 0.09f + 0.068f * 0.15f + 0.03f);
        for (int i = 0; i < 4; i++)
        {
            float a = i * 1.6f;
            float rr = 0.012f + 0.008f * i;
            b.PaintEll(Body, gem + V(MathF.Cos(a) * rr, MathF.Sin(a) * rr, 0), V(0.012f, 0.012f, 0.02f), Rgb(170, 110, 240), soft: 0.006f);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2122 Mimestro, from Mr. Mime

    /// <summary>
    /// Mimestro: Mr. Mime taller, in a tall hat and a conductor's tailcoat of pink and white, a pane of light held in
    /// its hand.
    /// </summary>
    private static PokeBuilder Mimestro()
    {
        var b = Become(MrMime(), "Mimestro", 0.92f);
        var pink = Rgb(238, 98, 120);
        var magenta = Rgb(176, 42, 102);
        var navy = Rgb(246, 206, 222);
        // Magenta where Mr. Mime is blue: its curled hair and shoes
        Recolour(b, (Rgb(44, 92, 172), magenta));
        int head = BoneOf(b, "head");
        // A tall pale pink hat, banded magenta
        b.Ell(head, V(0, 0.965f, 0.01f), V(0.12f, 0.014f, 0.11f), navy, mat: Shell, blend: 0.01f);
        b.Limb(head, V(0, 0.96f, 0.01f), V(0, 1.16f, 0.0f), 0.07f, 0.08f, navy, Shell, 0.004f);
        b.PaintTorus(head, V(0, 0.995f, 0.008f), 0.072f, 0.016f, magenta, V(-3f, 0, 0));
        // Striped magenta trousers
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            b.PaintEll(leg, V(0.115f * s, 0.35f, 0.015f), V(0.05f, 0.14f, 0.05f), magenta, Euler(V(0.05f * s, -0.22f, 0.03f)));
            b.PaintEll(leg, V(0.145f * s, 0.15f, 0.015f), V(0.045f, 0.11f, 0.045f), magenta);
        });
        // The tailcoat's tails behind, its lapels in front
        foreach (float x in new[] { -1f, 1f })
            Blade(b, Body, V(0.05f * x, 0.58f, -0.09f), V(0.12f * x, 0.16f, -0.22f), 0.065f, pink, V(x * 0.3f, 0, -1f), 0.25f);
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.07f * s, 0.68f, 0.09f), V(0.035f, 0.07f, 0.03f), pink, V(0, 0, -20f * s)));
        // A pane of light held in its right hand
        b.Box(BoneOf(b, "armR"), V(0.5f, 0.72f, 0.12f), V(0.07f, 0.11f, 0.006f), 0.004f, Rgb(190, 228, 255), V(0, 0, -12f), Glow, 0.006f);
        // A softer pink for the pads and the coat, a white waistcoat between its lapels
        Recolour(b, (pink, Rgb(232, 150, 182)));
        b.PaintEll(Body, V(0, 0.64f, 0.1f), V(0.05f, 0.09f, 0.03f), Rgb(246, 244, 246));
        Stretch(b, V(1f, 1.06f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2124 Aurorelle, from Jynx

    /// <summary>
    /// Aurorelle: a new figure out of Jynx, tall and slender in a gown of ice crystals, its long hair pale blue-white
    /// and lit along its length by the green and violet bands of an aurora, its face a pale lilac mask with large eyes.
    /// </summary>
    private static PokeBuilder Aurorelle()
    {
        var b = Become(Jynx(), "Aurorelle", 0.92f);
        var gown = Rgb(170, 208, 240);
        var ice = Rgb(224, 242, 255);
        Recolour(b, (Rgb(214, 72, 66), gown), (Rgb(244, 226, 156), Rgb(226, 236, 250)), (Rgb(112, 92, 168), Rgb(206, 188, 232)), (Rgb(242, 200, 84), Rgb(214, 226, 244)));
        // Crystals of ice on the gown, round its hem and at its shoulders
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            var at = d * 0.23f + V(0, 0.07f, 0);
            b.Spike(Body, at - d * 0.02f, at + d * 0.05f + V(0, 0.06f, 0), 0.025f, ice, mat: Shell, blend: 0.006f);
        }
        PokeBuilder.Both(s => b.Spike(Body, V(0.09f * s, 0.56f, 0.0f), V(0.14f * s, 0.65f, -0.02f), 0.024f, ice, mat: Shell, blend: 0.006f));
        // The aurora along its hair
        int head = BoneOf(b, "head");
        foreach (var (x, color) in new[] { (-0.07f, Rgb(120, 240, 190)), (-0.025f, Rgb(190, 140, 250)), (0.025f, Rgb(120, 240, 190)), (0.07f, Rgb(190, 140, 250)) })
            PaintAs(b, head, V(x, 0.58f, -0.11f), V(0.016f, 0.2f, 0.06f), color, Glow);
        // Its hair streams out in long locks flaring wide to the ground, lit by the aurora along their length
        var hairPale = Rgb(204, 222, 248);
        Recolour(b, (Rgb(244, 156, 176), Rgb(204, 176, 226)));
        PokeBuilder.Both(s =>
        {
            foreach (var (x, back, flare) in new[] { (0.08f, -0.07f, 0.34f), (0.1f, -0.03f, 0.26f), (0.05f, -0.1f, 0.2f) })
            {
                var path = Smooth(3, V(x * s, 0.72f, back), V((x + 0.08f) * s, 0.5f, back - 0.04f), V((x + flare * 0.6f) * s, 0.24f, back - 0.02f), V((x + flare) * s, 0.06f, back + 0.04f), V((x + flare + 0.06f) * s, 0.04f, back + 0.12f));
                b.Tube(head, path, 0.05f, 0.016f, hairPale, blend: 0.02f);
                for (int i = 2; i < path.Length - 1; i += 3)
                    PaintAs(b, head, path[i], V(0.05f, 0.07f, 0.05f), i % 2 == 0 ? Rgb(120, 240, 190) : Rgb(190, 140, 250), Glow);
            }
        });
        // A crown of ice
        foreach (var (x, h) in new[] { (-0.06f, 0.05f), (-0.03f, 0.07f), (0f, 0.09f), (0.03f, 0.07f), (0.06f, 0.05f) })
            b.Spike(head, V(x, 0.85f, 0.04f), V(x * 1.2f, 0.85f + h, 0.03f), 0.014f, Rgb(214, 238, 255), mat: Shell, blend: 0.006f);
        // Larger eyes
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
        {
            d.Size *= 1.25f;
            d.Half *= 1.25f;
        }
        Stretch(b, V(0.92f, 1.12f, 0.92f));
        return b;
    }

    // ------------------------------------------------------------------ 2127 Mandibron, from Pinsir

    /// <summary>
    /// Mandibron: Pinsir darkened to brown-black, its horns grown longer and near black with an edge of red teeth, and
    /// a smooth carapace closed over its back.
    /// </summary>
    private static PokeBuilder Mandibron()
    {
        // Pinsir gone down on six legs into a great beetle: its body lies along the ground under a closed carapace, and
        // its two horns reach forward from its head as mandibles, near black, toothed in red along their insides
        var b = new PokeBuilder("Mandibron", 0.95f, BodyPlan.Quadruped, V(0, 0.26f, -0.04f)) { Coat = Shell };
        var brown = Rgb(58, 46, 44);
        var dark = Rgb(38, 30, 32);
        var horn = Rgb(44, 36, 40);
        var red = Rgb(170, 40, 46);
        // Six jointed legs, spurred, each foot two claws
        foreach (var (z, front, name) in new[] { (0.12f, true, (string?)null), (-0.04f, false, "mid"), (-0.2f, false, (string?)null) })
            PokeBuilder.Both(s =>
            {
                var hip = V(0.12f * s, 0.2f, z);
                int leg = name == null ? b.Leg(s, hip, front) : b.Part(name + (s < 0 ? "L" : "R"), Body, hip, PokeRole.Leg, 0.5f, s);
                var knee = V(0.25f * s, 0.17f, z + (front ? 0.06f : -0.02f));
                var foot = V(0.29f * s, 0.02f, z + (front ? 0.1f : -0.04f));
                b.Limb(leg, hip, knee, 0.032f, 0.026f, brown, Shell);
                b.Limb(leg, knee, foot, 0.026f, 0.018f, brown, Shell);
                b.Spike(leg, Vector3.Lerp(knee, foot, 0.4f), Vector3.Lerp(knee, foot, 0.4f) + V(0.03f * s, 0.02f, 0), 0.008f, dark, mat: Shell, blend: 0.003f);
                foreach (float t in new[] { -1f, 1f })
                    b.Spike(leg, foot + V(0, 0.01f, 0), foot + V(0.015f * s + 0.012f * t, -0.008f, 0.03f * (front ? 1f : -0.3f) + 0.012f * t), 0.009f, dark, mat: Shell, blend: 0.003f);
            });
        // The body under a closed carapace, its two wing cases parted by a seam, bands on the abdomen beneath
        var c = V(0, 0.24f, -0.08f);
        var r = V(0.2f, 0.13f, 0.28f);
        b.Ell(Body, c, r, brown);
        b.Ell(Body, c + V(0, 0.07f, -0.03f), V(0.23f, 0.16f, 0.29f), dark, mat: Shell, blend: 0.02f);
        b.PaintEll(Body, c + V(0, 0.23f, -0.03f), V(0.006f, 0.05f, 0.29f), Rgb(90, 64, 60), soft: 0.006f);
        b.PaintEll(Body, c + V(0, 0.1f, 0.18f), V(0.18f, 0.06f, 0.008f), Rgb(80, 58, 56), soft: 0.006f);
        foreach (float z in new[] { -0.16f, -0.08f, 0.0f, 0.08f })
            b.PaintEll(Body, c + V(0, -0.1f, z), V(0.15f, 0.04f, 0.008f), Rgb(84, 66, 60), soft: 0.006f);
        // A broad thorax and a low head between the mandibles, its eyes burning orange
        int head = b.Head(V(0, 0.26f, 0.18f));
        var hc = V(0, 0.25f, 0.24f);
        var hr = V(0.1f, 0.075f, 0.08f);
        b.Ell(head, hc, hr, dark, mat: Shell);
        b.Ell(head, hc + V(0, 0.03f, -0.04f), V(0.13f, 0.06f, 0.07f), dark, mat: Shell, blend: 0.03f);
        PokeBuilder.Both(s =>
        {
            b.Eye(head, On(hc, hr, 0.05f * s, hc.Y + 0.015f), V(0.6f * s, 0.15f, 1f), 0.018f, sclera: true, white: Rgb(250, 120, 60), pupil: Rgb(255, 200, 120), glare: true);
            // Small hooked mouthparts and feelers
            b.Spike(head, hc + V(0.03f * s, -0.05f, 0.06f), hc + V(0.02f * s, -0.09f, 0.08f), 0.012f, Rgb(110, 70, 66), mat: Shell, blend: 0.004f);
            b.Tube(head, new[] { hc + V(0.05f * s, -0.02f, 0.07f), hc + V(0.1f * s, -0.04f, 0.12f), hc + V(0.13f * s, -0.02f, 0.15f) }, 0.004f, 0.003f, dark, blend: 0f);
        });
        // The two great mandibles, reaching forward and up, toothed in red along their insides
        PokeBuilder.Both(s =>
        {
            var path = Smooth(3, hc + V(0.06f * s, 0.04f, 0.02f), hc + V(0.13f * s, 0.12f, 0.14f), hc + V(0.14f * s, 0.26f, 0.26f), hc + V(0.06f * s, 0.36f, 0.36f));
            b.Tube(head, path, 0.05f, 0.012f, horn, Shell, 0f);
            for (int i = 2; i < path.Length - 1; i++)
            {
                float t = i / (float)(path.Length - 1);
                var p = path[i];
                var d = Vector3.Normalize(path[i + 1] - path[i - 1]);
                var inward = Vector3.Normalize(V(-s, 0, 0) - d * Vector3.Dot(V(-s, 0, 0), d));
                float rr = 0.05f + (0.012f - 0.05f) * t;
                b.Spike(head, p + inward * rr * 0.5f, p + inward * (rr + 0.028f) + d * 0.01f, 0.011f, red, mat: Shell, blend: 0.004f);
            }
        });
        return b;
    }

    // ------------------------------------------------------------------ 2128 Bovarch, from Tauros

    /// <summary>
    /// Bovarch: Tauros heavier in the shoulders, its horns grown wider and ringed, its mane longer and darker, and a
    /// crest of rock-like hide on its brow where the three studs were.
    /// </summary>
    private static PokeBuilder Bovarch()
    {
        var b = Become(Tauros(), "Bovarch", 1f);
        var oldMane = Rgb(116, 106, 100);
        var mane = Rgb(54, 48, 40);
        // A dun hide, a near black mane, pale grey hooves; horns of old ivory
        Recolour(b, (oldMane, mane), (Rgb(206, 158, 98), Rgb(150, 132, 104)), (Rgb(112, 78, 62), Rgb(118, 100, 80)), (Rgb(118, 124, 160), Rgb(96, 90, 82)), (Rgb(96, 98, 140), mane));
        Grow(b, V(1.12f, 1.15f, 1.15f), V(0, 0.5f, 0.15f), s => s.Bone == Body && s.Color.Equals(mane), _ => false);
        int head = BoneOf(b, "head");
        var horn = Rgb(150, 152, 170);
        // The studs give way to a crest of rock-like hide
        b.Sdf.RemoveAll(s => s.Bone == head && s.Shape == SdfShape.Ellipsoid && s.Color.Equals(horn));
        var c = V(0, 0.52f, 0.37f);
        var r = V(0.085f, 0.08f, 0.09f);
        var brow = On(c, r, 0, 0.555f);
        b.Ell(head, brow + V(0, 0.01f, -0.015f), V(0.06f, 0.03f, 0.04f), Rgb(150, 146, 140), V(-20f, 0, 0), Shell, 0.012f);
        foreach (float x in new[] { -0.03f, 0f, 0.03f })
            b.Spike(head, brow + V(x, 0.02f, -0.02f), brow + V(x * 1.3f, 0.06f, -0.05f), 0.016f, Rgb(150, 146, 140), mat: Shell, blend: 0.006f);
        // Wider horns, ringed
        Grow(b, V(1.4f, 1.08f, 1f), V(0, 0.56f, 0.35f), s => s.Bone == head && s.Color.Equals(horn), _ => false);
        PokeBuilder.Both(s =>
        {
            var path = Smooth(3, V(0.06f * s, 0.56f, 0.35f), V(0.15f * s, 0.585f, 0.36f), V(0.2f * s, 0.65f, 0.36f), V(0.18f * s, 0.74f, 0.38f));
            for (int i = 2; i < path.Length - 2; i += 2)
            {
                var p = V(0, 0.56f, 0.35f) + (path[i] - V(0, 0.56f, 0.35f)) * V(1.4f, 1.08f, 1f);
                var q = V(0, 0.56f, 0.35f) + (path[i + 1] - V(0, 0.56f, 0.35f)) * V(1.4f, 1.08f, 1f);
                float t = i / (float)(path.Length - 1);
                b.PaintTorus(head, p, 0.03f + (0.012f - 0.03f) * t, 0.005f, Rgb(96, 96, 112), Euler(q - p));
            }
        });
        Recolour(b, (horn, Rgb(196, 180, 146)), (Rgb(96, 96, 112), Rgb(132, 116, 92)));
        return b;
    }

    // ------------------------------------------------------------------ 2130 Typhonarch, from Gyarados

    /// <summary>
    /// Typhonarch: Gyarados grown into the dragon it has always looked like: two short forelegs with clawed hands, its
    /// crest grown into a great fan of fins and its whiskers long and flowing (the female's white).
    /// </summary>
    /// <param name="female">A female keeps Gyarados's: white whiskers.</param>
    private static PokeBuilder Typhonarch(bool female = false)
    {
        var b = Become(Gyarados(female), "Typhonarch", 1f);
        var blue = Rgb(58, 150, 226);
        var cream = Rgb(244, 226, 156);
        var navy = Rgb(36, 70, 160);
        var white = Rgb(236, 242, 248);
        const float hy = 0.94f;
        int head = BoneOf(b, "head");
        // The crest grown into a great fan of fins
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.Equals(navy));
        for (int i = 0; i < 7; i++)
        {
            float a = (-66f + i * 22f) * Degree;
            var root = V(MathF.Sin(a) * 0.04f, hy + 0.08f, 0.1f);
            Blade(b, head, root, root + V(MathF.Sin(a) * 0.3f, MathF.Cos(a) * 0.3f, -0.14f), 0.075f, navy, V(0, 0.15f, 1f), 0.25f);
        }
        // Long flowing whiskers, from its snout out and back on the wind (the female's paler still)
        var whisker = female ? white : Rgb(200, 232, 236);
        b.Sdf.RemoveAll(s => s.Bone == head && s.Shape == SdfShape.RoundCone && s.Size.X < 0.012f && (s.Color.Equals(white) || s.Color.Equals(Rgb(96, 172, 236))));
        PokeBuilder.Both(s => b.Tube(head, Smooth(3, V(0.08f * s, hy - 0.02f, 0.38f), V(0.22f * s, hy - 0.08f, 0.36f), V(0.34f * s, hy - 0.02f, 0.24f), V(0.4f * s, hy - 0.16f, 0.12f), V(0.46f * s, hy - 0.1f, 0.0f)), 0.01f, 0.004f, whisker, blend: 0f));
        // A fan of fins down its whole back
        for (int i = 1; i <= 6; i++)
        {
            int segBone = BoneOf(b, "seg" + i);
            b.Model.Decals.RemoveAll(d => d.Bone == segBone && !d.IsEye);
        }
        // Two short forelegs with clawed hands, from where it rears up
        int seg = BoneOf(b, "seg4");
        var mid = (V(0.16f, 0.44f, 0.05f) + V(0.05f, 0.62f, -0.03f)) / 2f;
        PokeBuilder.Both(s =>
        {
            int arm = b.Part(s < 0 ? "armL" : "armR", seg, mid + V(0.1f * s, -0.03f, 0.04f), PokeRole.Arm, s, s);
            var shoulder = mid + V(0.08f * s, -0.02f, 0.05f);
            var elbow = mid + V(0.26f * s, -0.1f, 0.14f);
            var hand = mid + V(0.22f * s, -0.16f, 0.3f);
            b.Limb(arm, shoulder, elbow, 0.062f, 0.05f, blue);
            b.Ell(arm, elbow, V(0.05f, 0.05f, 0.05f), blue);
            b.Limb(arm, elbow, hand, 0.05f, 0.042f, blue);
            b.PaintEll(arm, (elbow + hand) / 2f + V(0, -0.025f, 0.01f), V(0.04f, 0.07f, 0.04f), cream, Euler(hand - elbow));
            b.Ell(arm, hand, V(0.045f, 0.04f, 0.045f), blue);
            Claws(b, arm, hand + V(0, -0.01f, 0.035f), V(0.024f, 0, 0), V(0, -0.4f, 1f), 0.05f, 0.014f);
        });
        // Teal scales, a pale belly, fins of sea green
        Recolour(b, (blue, Rgb(38, 112, 128)), (cream, Rgb(150, 214, 204)), (navy, Rgb(70, 160, 158)), (Rgb(250, 214, 74), Rgb(100, 186, 180)),
            (Rgb(236, 242, 248), Rgb(168, 226, 216)), (Rgb(170, 216, 244), Rgb(150, 220, 210)));
        return b;
    }

    // ------------------------------------------------------------------ 2134 Naiadeon, from Vaporeon

    /// <summary>
    /// Naiadeon: Vaporeon longer, its ruff grown into a flowing mane of fins, its tail fin wider and pale at its edge,
    /// pale wave lines painted down its body.
    /// </summary>
    private static PokeBuilder Naiadeon()
    {
        var b = Become(Vaporeon(), "Naiadeon", 0.84f);
        const float h = 0.17f;
        var teal = Rgb(44, 116, 128);
        var cream = Rgb(126, 200, 190);
        var pale = Rgb(206, 238, 252);
        // Deep teal, its frill and fins sea green
        Recolour(b, (Rgb(92, 190, 232), teal), (Rgb(40, 96, 168), Rgb(30, 80, 96)), (Rgb(248, 240, 200), cream));
        // The mane of fins flowing back down its neck and over its shoulders
        for (int i = 0; i < 13; i++)
        {
            float a = -2.0f + i * (4f / 12f);
            var at = V(MathF.Sin(a) * 0.05f, h + 0.17f + MathF.Cos(a) * 0.02f, 0.12f + MathF.Cos(a) * 0.035f);
            var tip = at + V(MathF.Sin(a) * 0.14f, 0.02f - MathF.Abs(MathF.Sin(a)) * 0.12f, -0.3f);
            Ribbon(b, Body, Smooth(3, at, (at + tip) / 2f + V(0, 0.07f, 0), tip), 0.07f, i % 2 == 0 ? cream : Rgb(96, 176, 170), V(MathF.Sin(a), 1f, 0), 0.012f);
        }
        // A wider tail fin, pale at its edge
        int tail = BoneOf(b, "tail");
        var end = V(0.15f, h - 0.03f, -0.4f);
        Grow(b, V(1.6f, 1.6f, 1.6f), end, s => s.Bone == tail && Vector3.Distance(s.A, end) < 0.01f, _ => false);
        b.PaintEll(tail, end + V(0.16f, 0.09f, -0.07f), V(0.06f, 0.06f, 0.06f), pale);
        b.PaintEll(tail, end + V(0.14f, -0.015f, 0.12f), V(0.06f, 0.06f, 0.06f), pale);
        // Wave lines down its body
        PokeBuilder.Both(s =>
        {
            foreach (float z in new[] { 0.05f, -0.07f })
            {
                var n = V(s, 0.35f, z * 4f);
                b.Mark(Body, Out(V(0, h + 0.02f, -0.025f), V(0.075f, 0.07f, 0.15f), default, n), n, 0.05f, 0.014f, pale, MarkShape.Wave);
            }
        });
        // A long fin down its tail
        int tailBone = BoneOf(b, "tail");
        Ribbon(b, tailBone, Smooth(3, V(0, h + 0.07f, -0.16f), V(0.04f, h + 0.08f, -0.28f), V(0.12f, h + 0.02f, -0.38f)), 0.05f, cream, V(0, 1f, 0), 0.012f);
        Stretch(b, V(1f, 1.04f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2135 Fulgureon, from Jolteon

    /// <summary>Fulgureon: Jolteon with its spikes grown long and swept back into bolts of lightning, white at their tips, and its ruff stood up in a crest.</summary>
    private static PokeBuilder Fulgureon()
    {
        var b = Become(Jolteon(), "Fulgureon", 0.8f);
        var yellow = Rgb(250, 214, 56);
        int tail = BoneOf(b, "tail");
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == tail && s.Color.Equals(yellow) && s.Shape == SdfShape.RoundCone).ToList())
        {
            var d = s.B - s.A;
            s.B = s.A + d * 1.6f + V(0, 0, -0.05f);
            var dir = Vector3.Normalize(s.B - s.A);
            var kink = Vector3.Normalize(dir + Vector3.Normalize(Vector3.Cross(dir, V(0, 0, 1f)) + V(0.001f, 0, 0)) * 0.9f);
            b.Spike(tail, s.B - dir * 0.015f, s.B + kink * 0.09f, 0.026f, Rgb(255, 240, 150), mat: Glow, blend: 0.006f);
        }
        // The ruff stood up in a crest, dark as a thundercloud
        var navy = Rgb(44, 52, 76);
        var cloud = Rgb(96, 102, 118);
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == Body && s.Color.Equals(White) && s.Shape == SdfShape.RoundCone).ToList())
        {
            var d = s.B - s.A;
            s.B = s.A + Vector3.Normalize(d + V(0, 0.16f, 0)) * d.Length() * 1.4f;
            s.Color = navy;
        }
        const float h = 0.24f;
        // Storm cloud over its crown and down the back of its neck
        int head = BoneOf(b, "head");
        foreach (var (at, rr) in new[] { (V(0, h + 0.27f, 0.06f), 0.05f), (V(-0.04f, h + 0.25f, 0.03f), 0.04f), (V(0.04f, h + 0.25f, 0.03f), 0.04f), (V(0, h + 0.24f, 0.0f), 0.045f) })
            b.Ell(head, at + V(0, 0.01f, -0.02f), V(rr, rr, rr) * 1.3f, cloud, blend: 0.02f);
        // Dark patches through its coat, jagged like the space between bolts
        foreach (var (at, size) in new[] { (V(0.06f, h + 0.02f, 0.06f), 0.045f), (V(-0.06f, h + 0.02f, 0.06f), 0.045f), (V(0.07f, h - 0.02f, -0.1f), 0.04f), (V(-0.07f, h - 0.02f, -0.1f), 0.04f) })
            b.PaintEll(Body, at, V(size * 0.7f, size * 1.2f, size), navy, V(0, 0, at.X > 0 ? 25f : -25f));
        foreach (var leg in new[] { "legL", "legR", "hindL", "hindR" })
        {
            int bone = BoneOf(b, leg);
            float sx = leg.EndsWith("L") ? -1f : 1f;
            float z = leg.StartsWith("leg") ? 0.09f : -0.11f;
            b.PaintEll(bone, V(0.06f * sx, h * 0.55f, z), V(0.035f, 0.06f, 0.035f), navy);
        }
        // Long bolts swept back over its back from its shoulders
        foreach (var (x, len) in new[] { (0.04f, 0.4f), (-0.04f, 0.4f), (0.08f, 0.3f), (-0.08f, 0.3f) })
        {
            var root = V(x, h + 0.08f, 0.04f);
            var mid = root + V(x * 0.8f, 0.1f, -len * 0.5f);
            var tip = root + V(x * 1.4f, 0.26f, -len);
            b.Spike(Body, root, mid + V(0, -0.04f, 0), 0.04f, Rgb(250, 214, 56), 0.35f);
            b.Spike(Body, mid - V(0, 0.06f, 0), tip, 0.036f, Rgb(250, 236, 140), 0.35f);
        }
        b.Sdf.Changed();
        return b;
    }

    // ------------------------------------------------------------------ 2136 Pyreon, from Flareon

    /// <summary>Pyreon: Flareon with its ruff and tail fuller, small flames burning at the ruff's tips, and the tuft on its brow streaming back.</summary>
    private static PokeBuilder Pyreon()
    {
        var b = Become(Flareon(), "Pyreon", 0.82f);
        const float h = 0.2f;
        var cream = Rgb(250, 230, 150);
        var about = V(0, h + 0.11f, 0.11f);
        Grow(b, V(1.25f, 1.25f, 1.25f), about, s => s.Bone == Body && s.Color.Equals(cream), _ => false);
        int tail = BoneOf(b, "tail");
        Grow(b, V(1.25f, 1.3f, 1.25f), V(0, h + 0.1f, -0.2f), s => s.Bone == tail && s.Color.Equals(cream), _ => false);
        for (int i = 0; i < 9; i++)
        {
            float a = -1.6f + i * 0.4f;
            var n = Vector3.Normalize(V(MathF.Sin(a) * 0.9f, 0.4f, MathF.Cos(a) * 0.9f));
            var at = Out(about, V(0.106f, 0.094f, 0.081f), default, n);
            FlameTongue(b, Body, at - n * 0.012f, at + n * 0.04f + V(0, 0.045f, 0), 0.016f, Rgb(244, 104, 44), Rgb(255, 220, 100));
        }
        int head = BoneOf(b, "head");
        // A deeper orange, its tail a great plume of flame-orange and its legs darkening to soot at the paws
        var orange = Rgb(214, 104, 46);
        var flame = Rgb(236, 132, 54);
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == tail && s.Color.Equals(cream))) s.Color = flame;
        Recolour(b, (Rgb(242, 128, 58), orange));
        foreach (var leg in new[] { "legL", "legR", "hindL", "hindR" })
        {
            int bone = BoneOf(b, leg);
            float sx = leg.EndsWith("L") ? -1f : 1f;
            float z = leg.StartsWith("leg") ? 0.09f : -0.12f;
            b.PaintEll(bone, V(0.055f * sx, 0.03f, z), V(0.035f, 0.05f, 0.045f), Rgb(70, 40, 30));
        }
        // Flames licking up from its tail's end
        foreach (var (dx, dy) in new[] { (0f, 0.14f), (0.06f, 0.1f), (-0.06f, 0.1f) })
            FlameTongue(b, tail, V(dx * 0.5f, h + 0.28f, -0.3f), V(dx, h + 0.28f + dy, -0.34f), 0.03f, Rgb(244, 104, 44), Rgb(255, 220, 100));
        // Its brow tuft streams back as a long lock
        b.Tube(head, Smooth(3, V(0, h + 0.3f, 0.15f), V(0, h + 0.33f, 0.06f), V(0.03f, h + 0.33f, -0.04f), V(0.08f, h + 0.36f, -0.1f)), 0.026f, 0.008f, orange, blend: 0.01f);
        return b;
    }

    // ------------------------------------------------------------------ 2143 Dozemount, from Snorlax

    /// <summary>
    /// Dozemount: Snorlax asleep so long the woods grew on it: a mossy mantle on its back and shoulders with small trees
    /// and flowers, and a bird's nest on its head.
    /// </summary>
    private static PokeBuilder Dozemount()
    {
        var b = Become(Snorlax(), "Dozemount", 1f);
        // A bear's dark brown hide where Snorlax is teal, its face and belly still cream
        Recolour(b, (Rgb(40, 104, 132), Rgb(84, 70, 60)));
        var moss = Rgb(98, 150, 72);
        var deep = Rgb(70, 120, 58);
        // The mantle of moss over its back and shoulders
        b.Ell(Body, V(0, 0.52f, -0.1f), V(0.34f, 0.2f, 0.24f), moss, V(-10f, 0, 0), Leaf, 0.04f);
        foreach (var (at, size) in new[] { (V(0.24f, 0.56f, 0.0f), 0.1f), (V(-0.24f, 0.56f, 0.0f), 0.1f), (V(0.12f, 0.66f, -0.14f), 0.09f), (V(-0.14f, 0.64f, -0.18f), 0.08f), (V(0.0f, 0.5f, -0.3f), 0.1f) })
            b.Ell(Body, at, V(size, size * 0.7f, size), moss, mat: Leaf, blend: 0.03f);
        // A mound of moss rising high over its back, small trees on it, flowers in the moss, roots hanging down its sides
        b.Ell(Body, V(0.04f, 0.66f, -0.16f), V(0.3f, 0.16f, 0.22f), moss, V(-10f, 0, 0), Leaf, 0.04f);
        foreach (var (foot, r) in new[] { (V(0.18f, 0.78f, -0.14f), 0.09f), (V(-0.16f, 0.74f, -0.16f), 0.06f), (V(0.02f, 0.72f, -0.3f), 0.055f), (V(0.28f, 0.64f, -0.02f), 0.05f) })
            Foliage(b, Body, foot, foot + V(0, r * 1.8f, 0), r, Rgb(120, 86, 54), deep);
        foreach (var (x, z) in new[] { (0.3f, 0.06f), (-0.3f, 0.04f), (0.26f, -0.18f), (-0.24f, -0.2f) })
            b.Tube(Body, new[] { V(x, 0.56f, z), V(x * 1.06f, 0.46f, z + 0.01f), V(x * 1.02f, 0.38f, z) }, 0.008f, 0.005f, Rgb(110, 80, 52), blend: 0.004f);
        foreach (var (at, color) in new[] { (V(0.26f, 0.62f, 0.06f), Rgb(246, 200, 220)), (V(-0.26f, 0.62f, 0.04f), Rgb(250, 230, 120)), (V(0.06f, 0.7f, -0.08f), Rgb(246, 246, 250)), (V(-0.08f, 0.68f, -0.06f), Rgb(246, 200, 220)) })
            b.Ell(Body, at, V(0.02f, 0.014f, 0.02f), color, mat: Leaf, blend: 0.004f);
        // A bird's nest on its head
        int head = BoneOf(b, "head");
        b.Torus(head, V(0, 0.9f, -0.01f), 0.065f, 0.028f, Rgb(140, 100, 62), mat: Leaf, blend: 0.01f);
        b.Ell(head, V(0, 0.9f, -0.01f), V(0.06f, 0.02f, 0.06f), Rgb(120, 86, 54), mat: Leaf, blend: 0.01f);
        foreach (float x in new[] { -0.02f, 0.02f })
            b.Ell(head, V(x, 0.925f, -0.01f + x), V(0.016f, 0.02f, 0.016f), Rgb(226, 236, 246), mat: Shell, blend: 0.004f);
        return b;
    }

    // ------------------------------------------------------------------ 2144 Glacirune, from Articuno

    /// <summary>
    /// Glacirune: Articuno grown, its two tail streamers become a long veil of aurora, green and violet, and crystals
    /// of ice at its wing tips.
    /// </summary>
    private static PokeBuilder Glacirune()
    {
        var b = Become(Articuno(), "Glacirune", 1f);
        var lift = Shifted(b, "head", V(0, 0.7f, 0.04f));
        // Paler, icier blues
        Recolour(b, (Rgb(116, 178, 234), Rgb(170, 216, 232)), (Rgb(206, 232, 250), Rgb(226, 244, 248)), (Rgb(54, 114, 198), Rgb(120, 184, 210)), (Rgb(150, 150, 164), Rgb(60, 80, 120)));
        int tail = BoneOf(b, "tail");
        var green = Rgb(100, 230, 170);
        var violet = Rgb(120, 80, 190);
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == tail))
        {
            s.Color = s.A.X < 0 ? green : violet;
            s.Material = Glow;
        }
        // A broad veil of aurora flowing out behind it, violet with green bands
        As(b, Glow, () =>
        {
            foreach (var (sx, w, color) in new[] { (0f, 0.24f, violet), (-0.5f, 0.12f, green), (0.5f, 0.12f, green) })
                Ribbon(b, tail, Smooth(3, V(0.01f * sx, 0.46f, -0.1f), V(0.06f * sx, 0.32f, -0.32f), V(0.12f * sx, 0.18f, -0.52f), V(0.16f * sx + 0.04f, 0.12f, -0.66f), V(0.22f * sx + 0.08f, 0.2f, -0.78f)).Select(p => p + lift).ToArray(),
                    w, color, V(1f, 0.15f, 0.2f), 0.014f);
        });
        // Crystals of ice at the ends of its long feathers
        var ice = Rgb(220, 242, 255);
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            var shoulder = V(0.08f * s, 0.64f, -0.02f) + lift;
            var wrist = V(0.24f * s, 0.8f, -0.06f) + lift;
            for (int i = 2; i < 6; i++)
            {
                float t = i / 5f;
                float a = (112f + (28f - 112f) * t) * Degree;
                var root = Vector3.Lerp(Vector3.Lerp(shoulder, wrist, 0.3f), wrist, t);
                var dir = Vector3.Normalize(V(MathF.Cos(a) * s, MathF.Sin(a), -0.25f));
                float len = 0.38f * (0.7f + 0.3f * MathF.Sin(MathF.PI * (0.25f + 0.75f * t)));
                var end = root + dir * len;
                b.Spike(wing, end - dir * 0.03f, end + dir * 0.06f, 0.022f, ice, mat: Shell, blend: 0.006f);
            }
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2145 Fulguris, from Zapdos

    /// <summary>Fulguris: Zapdos grown, its jagged feathers longer and darker at their roots, a crown of storm cloud at its head.</summary>
    private static PokeBuilder Fulguris()
    {
        var b = Become(Zapdos(), "Fulguris", 1f);
        var lift = Shifted(b, "head", V(0, 0.66f, 0.05f));
        var yellow = Rgb(246, 214, 70);
        var slate = Rgb(52, 60, 76);
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == Body && s.Color.Equals(yellow) && s.Shape == SdfShape.RoundCone))
            s.B = s.A + (s.B - s.A) * 1.4f;
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            var shoulder = V(0.08f * s, 0.62f, -0.02f) + lift;
            Grow(b, V(1.22f, 1.22f, 1.1f), shoulder, bone => bone == wing);
        });
        // Its feathers dark as a storm, lightning-yellow only at their ends; eyes that glow yellow
        Recolour(b, (yellow, slate), (Rgb(40, 36, 44), yellow));
        foreach (var w in new[] { "wingL", "wingR" })
        {
            int wing = BoneOf(b, w);
            float s = w.EndsWith("L") ? -1f : 1f;
            var shoulder = V(0.08f * s, 0.62f, -0.02f) + lift;
            foreach (var sh in b.Sdf.Shapes.Where(sh => sh.Bone == wing && sh.Op == SdfOp.Add && sh.Shape == SdfShape.RoundCone && sh.Color.Equals(slate)).ToList())
                b.PaintEll(wing, Vector3.Lerp(sh.A, sh.B, 0.82f), V(0.03f, Vector3.Distance(sh.A, sh.B) * 0.22f, 0.03f), yellow, Euler(sh.B - sh.A));
        }
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
        {
            d.White = Rgb(250, 240, 120);
            d.Pupil = Rgb(255, 255, 220);
        }
        // A crown of storm cloud at its head
        int head = BoneOf(b, "head");
        var hc = V(0, 0.75f, 0.08f) + lift;
        var cloud = Rgb(140, 144, 158);
        foreach (var (at, r) in new[] { (V(0, 0.07f, -0.04f), 0.05f), (V(-0.05f, 0.06f, -0.06f), 0.04f), (V(0.05f, 0.06f, -0.06f), 0.04f), (V(0, 0.08f, -0.1f), 0.045f), (V(-0.035f, 0.1f, -0.02f), 0.032f), (V(0.035f, 0.1f, -0.02f), 0.032f) })
            b.Ell(head, hc + at * 1.4f + V(0, 0.01f, 0), V(r, r * 0.8f, r) * 1.5f, cloud, blend: 0.02f);
        b.Sdf.Changed();
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2146 Pyrevant, from Moltres

    /// <summary>Pyrevant: Moltres with its fire whitened at the core, a tail of three long flame streamers and a halo of embers at its crest.</summary>
    private static PokeBuilder Pyrevant()
    {
        var b = Become(Moltres(), "Pyrevant", 1f);
        var lift = Shifted(b, "head", V(0, 0.64f, 0.1f));
        var rim = Rgb(214, 70, 40);
        var heart = Rgb(255, 246, 216);
        // A body of ash grey veined in orange, wings of fire white at the heart and red at the rim
        Recolour(b, (Rgb(255, 214, 90), heart), (PixelCanvas.Mix(Rgb(244, 112, 50), Rgb(255, 214, 90), 0.5f), PixelCanvas.Mix(rim, heart, 0.5f)), (Rgb(244, 112, 50), rim), (Rgb(244, 198, 84), Rgb(96, 92, 96)), (Rgb(170, 140, 116), Rgb(120, 116, 118)));
        b.PaintEll(Body, V(0, 0.56f, 0.09f) + lift, V(0.012f, 0.08f, 0.04f), Rgb(240, 140, 50), V(-25f, 0, 0), 0.006f);
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.04f * s, 0.6f, 0.08f) + lift, V(0.008f, 0.05f, 0.03f), Rgb(240, 140, 50), V(-25f, 0, 30f * s), 0.006f));
        // The wings burn white-gold at their hearts
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            b.PaintEll(wing, V(0.3f * s, 0.8f, -0.1f) + lift, V(0.16f, 0.12f, 0.12f), Rgb(255, 236, 190));
            b.PaintEll(wing, V(0.22f * s, 0.74f, -0.12f) + lift, V(0.12f, 0.09f, 0.1f), Rgb(255, 250, 236));
        });
        // Three long streamers of flame for a tail
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        foreach (float dx in new[] { -1f, 0f, 1f })
        {
            var path = Smooth(3, V(0, 0.5f, -0.14f), V(dx * 0.06f, 0.48f, -0.36f), V(dx * 0.14f, 0.42f, -0.58f), V(dx * 0.2f, 0.38f + 0.04f * MathF.Abs(dx), -0.8f)).Select(p => p + lift).ToArray();
            b.Tube(tail, path, 0.045f, 0.018f, Rgb(248, 168, 72), Glow, 0f);
            for (int i = 1; i < path.Length - 1; i += 2)
                b.PaintEll(tail, path[i], V(0.02f, 0.04f, 0.02f), heart, Euler(path[i + 1] - path[i - 1]));
            FlameTongue(b, tail, path[^1] - Vector3.Normalize(path[^1] - path[^2]) * 0.02f, path[^1] + Vector3.Normalize(path[^1] - path[^2]) * 0.12f + V(0, 0.04f, 0), 0.03f, rim, heart);
        }
        // A halo of embers round its crest, on a thread of light
        int head = BoneOf(b, "head");
        var c = V(0, 0.73f, 0.17f) + lift;
        var ring = c + V(0, 0.12f, -0.05f);
        b.Torus(head, ring, 0.08f, 0.013f, Rgb(255, 200, 110), mat: Glow, blend: 0.004f);
        b.Tube(head, new[] { c + V(0, 0.09f, -0.02f), ring + V(0, 0, 0.08f) }, 0.013f, 0.013f, Rgb(255, 200, 110), Glow, 0.004f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + 0.3f;
            b.Ell(head, ring + V(MathF.Cos(a) * 0.08f, 0, MathF.Sin(a) * 0.08f), V(0.022f, 0.022f, 0.022f), Rgb(255, 170, 70), mat: Glow, blend: 0.006f);
        }
        return Lift(b);
    }
}
using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// The species of our own that evolve past level 100 (plan 03 Â· D14), batch 1: Kanto's, in our numbers' order
// (2000 + the forebear's National number). Each is sculpted from its forebear's own sculpt as the look of
// docs/plans/D14-proposals-kanto.md says: the forebear's build is taken whole where the body stays (renamed,
// stretched, its colours changed, a part taken away) and the new features are added to it, or, where the body
// changes its plan, it is built anew from the forebear's parts and colours. The forebears' own sculpts are never
// touched.
internal static partial class PokemonModels
{
    /// <summary>The species of our own of batch 1 with a sculpt of their own, by name in capitals (null for any other).</summary>
    private static PokeBuilder? CreateOurs(string species) => species.ToUpperInvariant() switch
    {
        "RATCHISEL" => Ratchisel(),
        "LANCEROW" => Lancerow(),
        "NAJAROK" => Najarok(),
        "SANDSPIRE" => Sandspire(),
        "KITSUNOX" => Kitsunox(),
        "CORDYHUSK" => Cordyhusk(),
        "SOMNIMOTH" => Somnimoth(),
        "MOLEMOUNT" => Molemount(),
        "REGALYNX" => Regalynx(),
        "PSYDUKE" => Psyduke(),
        "KIRININE" => Kirinine(),
        "MEDUSARCH" => Medusarch(),
        "SOLARION" => Solarion(),
        "SLOWMANTLE" => Slowmantle(),
        "PORRIGARD" => Porrigard(),
        "TRIPLUMA" => Tripluma(),
        "GLACITUSK" => Glacitusk(),
        "SLUDGEMIRE" => Sludgemire(),
        "CLAMSPIRE" => Clamspire(),
        "MESMERYX" => Mesmeryx(),
        "PINCEREX" => Pincerex(),
        "DYNAMORB" => Dynamorb(),
        "PALMAGUS" => Palmagus(),
        "OSSUGARD" => Ossugard(),
        "SAVATEUR" => Savateur(),
        "PUGILORD" => Pugilord(),
        "SMOGLOM" => Smoglom(),
        "MATRIROO" => Matriroo(),
        "SABREFIN" => Sabrefin(),
        "STELLAMIR" => Stellamir(),
        "MIMESTRO" => Mimestro(),
        "AURORELLE" => Aurorelle(),
        "MANDIBRON" => Mandibron(),
        "BOVARCH" => Bovarch(),
        "TYPHONARCH" => Typhonarch(),
        "FERRISLE" => Ferrisle(),
        "NAIADEON" => Naiadeon(),
        "FULGUREON" => Fulgureon(),
        "PYREON" => Pyreon(),
        "AMMONARCH" => Ammonarch(),
        "EURYPTEROX" => Eurypterox(),
        "AZHDARCH" => Azhdarch(),
        "DOZEMOUNT" => Dozemount(),
        "GLACIRUNE" => Glacirune(),
        "FULGURIS" => Fulguris(),
        "PYREVANT" => Pyrevant(),
        // Batch 3, Hoenn (PokemonModels.Ours.Hoenn.cs)
        "HYAENARCH" => Hyaenarch(),
        "STREAKOON" => Streakoon(),
        "SWIFTARROW" => Swiftarrow(),
        "PELICAPTAIN" => Pelicaptain(),
        "OCELLWING" => Ocellwing(),
        "SPOREPUGIL" => Sporepugil(),
        "SHINOBUZZ" => Shinobuzz(),
        "YOKOZAN" => Yokozan(),
        "FELINELLE" => Felinelle(),
        "GEMWRAITH" => Gemwraith(),
        "DECEIVORE" => Deceivore(),
        "ASCETRANCE" => Ascetrance(),
        "FULMENHOUND" => Fulmenhound(),
        "ANODION" => Anodion(),
        "CATHODION" => Cathodion(),
        "PHOTURION" => Photurion(),
        "LAMPYRIA" => Lampyria(),
        "GULPGORGE" => Gulpgorge(),
        "MEGALODAX" => Megalodax(),
        "LEVIAWHALE" => Leviawhale(),
        "VOLCAMEL" => Volcamel(),
        "SMELTORTOISE" => Smeltortoise(),
        "BOARCANE" => Boarcane(),
        "WHIRLPANDA" => Whirlpanda(),
        "SAGUAROX" => Saguarox(),
        "NIMBUSONG" => Nimbusong(),
        "RAVAGOOSE" => Ravagoose(),
        "SCIMITAIL" => Scimitail(),
        "SELENOLITH" => Selenolith(),
        "HELIOLITH" => Heliolith(),
        "NAMAZURO" => Namazuro(),
        "CRAYRONIN" => Crayronin(),
        "DOGUNARCH" => Dogunarch(),
        "CRINOLITH" => Crinolith(),
        "ANOMALOREX" => Anomalorex(),
        "PRISMARIA" => Prismaria(),
        "CHROMELEON" => Chromeleon(),
        "EFFIGRIM" => Effigrim(),
        "MUSADON" => Musadon(),
        "CARILLONE" => Carillone(),
        "OMENBLADE" => Omenblade(),
        "BERGMAW" => Bergmaw(),
        "GULPEREEL" => Gulpereel(),
        "SIRENYSS" => Sirenyss(),
        "AEONFIN" => Aeonfin(),
        "HEARTREEF" => Heartreef(),
        "REGIMONOLITH" => Regimonolith(),
        "REGIGLACIER" => Regiglacier(),
        "REGIFERRUM" => Regiferrum(),
        "AURELIAS" => Aurelias(),
        "AURELIOS" => Aurelios(),
        // Batch 5, Unova (PokemonModels.Ours.Unova.cs)
        "BEACONWATCH" => Beaconwatch(),
        "UMBRAPARD" => Umbrapard(),
        "SIMISYLVAN" => Simisylvan(),
        "SIMISCORCH" => Simiscorch(),
        "SIMITORRENT" => Simitorrent(),
        "SOMNARNA" => Somnarna(),
        "STORMSTRIKA" => Stormstrika(),
        "AMORABAT" => Amorabat(),
        "EXCAVARCH" => Excavarch(),
        "REMEDINO" => Remedino(),
        "THROJUTSU" => Throjutsu(),
        "SAWKENDO" => Sawkendo(),
        "COTTONIMBUS" => Cottonimbus(),
        "BALLERILLY" => Ballerilly(),
        "BASCURAGE" => Bascurage(),
        "DARUMANDALA" => Darumandala(),
        "MARIACHACTUS" => Mariachactus(),
        "CRUSTADEL" => Crustadel(),
        "BRAWLHOOD" => Brawlhood(),
        "GLYPHORACLE" => Glyphoracle(),
        "PHARAURUM" => Pharaurum(),
        "TORTUGARCH" => Tortugarch(),
        "PRIMAVIAN" => Primavian(),
        "SCRAPODOR" => Scrapodor(),
        "ZOROMIRAGE" => Zoromirage(),
        "CHINCHILLUX" => Chinchillux(),
        "CYGNELLE" => Cygnelle(),
        "SEASONSTAG" => Seasonstag(),
        "NIMBOLGA" => Nimbolga(),
        "LANCELIER" => Lancelier(),
        "SPOREMONARCH" => Sporemonarch(),
        "JELLICROWN" => Jellicrown(),
        "ALOMATRON" => Alomatron(),
        "VOLTARANTULA" => Voltarantula(),
        "FERROSPIRE" => Ferrospire(),
        "COSMOGREY" => Cosmogrey(),
        "BEARTUNDRA" => Beartundra(),
        "CRYOHEDRON" => Cryohedron(),
        "VELOCIGOR" => Velocigor(),
        "FLUXFLOUNDER" => Fluxflounder(),
        "MIENSHIFU" => Mienshifu(),
        "DRUDRAKON" => Drudrakon(),
        "COLOSSURK" => Colossurk(),
        "BOUFFALANX" => Bouffalanx(),
        "IMPERAVIA" => Imperavia(),
        "CARRIONQUEEN" => Carrionqueen(),
        "PYROVORE" => Pyrovore(),
        "FORMICARCH" => Formicarch(),
        "HELIOPTERA" => Helioptera(),
        "COBALTARCH" => Cobaltarch(),
        "TERRAKHAN" => Terrakhan(),
        "VIRIZENITH" => Virizenith(),
        // Batch 4, Sinnoh's (PokemonModels.Ours.Sinnoh.cs)
        "DAMBAREL" => Dambarel(),
        "MAESTRIKKET" => Maestrikket(),
        "CRANIALITH" => Cranialith(),
        "BASTIONSAUR" => Bastionsaur(),
        "NOCTURMOTH" => Nocturmoth(),
        "HIVEMPRESS" => Hivempress(),
        "VOLTGLIDER" => Voltglider(),
        "TIDESTOAT" => Tidestoat(),
        "SAKURABLOOM" => Sakurabloom(),
        "GASTROMIRE" => Gastromire(),
        "QUADRIPOM" => Quadripom(),
        "DIRIGEIST" => Dirigeist(),
        "LAPINOVA" => Lapinova(),
        "HEXMAGISTRA" => Hexmagistra(),
        "CORVICAPO" => Corvicapo(),
        "BRUTABBY" => Brutabby(),
        "MEPHITARCH" => Mephitarch(),
        "DOTAKURON" => Dotakuron(),
        "CANTATOT" => Cantatot(),
        "OSSUARION" => Ossuarion(),
        "AURASAGE" => Aurasage(),
        "BEHEMOWDON" => Behemowdon(),
        "DREADPION" => Dreadpion(),
        "VENOMSENSEI" => Venomsensei(),
        "FLYTRAPEX" => Flytrapex(),
        "LUMIFATHOM" => Lumifathom(),
        "GLACIARBOR" => Glaciarbor(),
        "RIMERIPPER" => Rimeripper(),
        "LICKOLOSSUS" => Lickolossus(),
        "TANGLEWOOD" => Tanglewood(),
        "MEGANEURON" => Meganeuron(),
        "DRYADEON" => Dryadeon(),
        "BOREALEON" => Borealeon(),
        "FANGLISCOR" => Fangliscor(),
        "LODESTARCH" => Lodestarch(),
        "FROSTBRIDE" => Frostbride(),
        "UXINOUS" => Uxinous(),
        "MESPATHOS" => Mespathos(),
        "AZELTHYMOS" => Azelthymos(),
        // Batch 2, Johto (PokemonModels.Ours.Johto.cs)
        "ERMILONG" => Ermilong(),
        "NOCTAUGUR" => Noctaugur(),
        "COCCINOVA" => Coccinova(),
        "ORBWEAVIX" => Orbweavix(),
        "ABYSSOLUX" => Abyssolux(),
        "TOTEMSIGHT" => Totemsight(),
        "MIMICEDAR" => Mimicedar(),
        "HELIANTHRA" => Helianthra(),
        "MIRELORD" => Mirelord(),
        "ZENITHEON" => Zenitheon(),
        "ECLIPSEON" => Eclipseon(),
        "SLOWMONARCH" => Slowmonarch(),
        "WOBBULWARK" => Wobbulwark(),
        "PINECITADEL" => Pinecitadel(),
        "IRONWYRM" => Ironwyrm(),
        "MASTIFFANG" => Mastiffang(),
        "FUGUMACE" => Fugumace(),
        "GUILLOTYNE" => Guillotyne(),
        "FERMENTOISE" => Fermentoise(),
        "ATLASCROSS" => Atlascross(),
        "CALDERON" => Calderon(),
        "REEFCROWN" => Reefcrown(),
        "KRAKANNON" => Krakannon(),
        "SLEIGHWING" => Sleighwing(),
        "RAYSAIL" => Raysail(),
        "RAPIERWING" => Rapierwing(),
        "CERBERHOUND" => Cerberhound(),
        "SIEGEPHANT" => Siegephant(),
        "MURALIST" => Muralist(),
        "GYRODRILL" => Gyrodrill(),
        "PASTORINE" => Pastorine(),
        "RAITENKO" => Raitenko(),
        "ENKAIOU" => Enkaiou(),
        "SUISEIREN" => Suiseiren(),
        // Batch 8, Galar and Hisui (PokemonModels.Ours.Galar.cs)
        "LARDEROUS" => Larderous(),
        "REYNARDINE" => Reynardine(),
        "THISTLEWISP" => Thistlewisp(),
        "RAMSWORTH" => Ramsworth(),
        "JUGGERNAW" => Juggernaw(),
        "VOLTCOURSER" => Voltcourser(),
        "PIPPINWYRM" => Pippinwyrm(),
        "CRUMBLEDRAKE" => Crumbledrake(),
        "DUNECONDA" => Duneconda(),
        "GANNETGULP" => Gannetgulp(),
        "RAPIERFIN" => Rapierfin(),
        "AMPLIPUNK" => Amplipunk(),
        "FURNAPEDE" => Furnapede(),
        "KRAKETSU" => Kraketsu(),
        "URNGEIST" => Urngeist(),
        "JARLKATT" => Jarlkatt(),
        "WRAITHREEF" => Wraithreef(),
        "PALADRAKE" => Paladrake(),
        "RUNEDOLMEN" => Runedolmen(),
        "PATISSERENE" => Patisserene(),
        "PHALANXION" => Phalanxion(),
        "VOLTECHINUS" => Voltechinus(),
        "AURORAMOTH" => Auroramoth(),
        "HENJOURNER" => Henjourner(),
        "FLOEGUIN" => Floeguin(),
        "MAJORDOMEE" => Majordomee(),
        "GORGEHOG" => Gorgehog(),
        "VERDIGRAND" => Verdigrand(),
        "FULGUREX" => Fulgurex(),
        "GLACIVOLT" => Glacivolt(),
        "ABYSSAURUS" => Abyssaurus(),
        "GLACICHTHYS" => Glacichthys(),
        "REGIFULGUR" => Regifulgur(),
        "REGIDRACONIS" => Regidraconis(),
        "HOARSTEED" => Hoarsteed(),
        "SHADESTEED" => Shadesteed(),
        "WYRDSTAG" => Wyrdstag(),
        "HALBERDON" => Halberdon(),
        "SHOALWRAITH" => Shoalwraith(),
        "CLIFFLASHER" => Clifflasher(),
        "SPINARMADA" => Spinarmada(),
        "ENAMORATA" => Enamorata(),
        "URSENSEI" => Ursensei(),
        "GLIMMANE" => Glimmane(),
        "SLOWSLINGER" => Slowslinger(),
        "SLOWMAGUS" => Slowmagus(),
        "PURIFUME" => Purifume(),
        "FROSTRUMA" => Frostruma(),
        "SNAREFLAT" => Snareflat(),
        "HYPNOSKYE" => Hypnoskye(),
        "STRIDESTORM" => Stridestorm(),
        "DUSKPHOENIX" => Duskphoenix(),
        "OBSIDHOUND" => Obsidhound(),
        "APRICHARGE" => Apricharge(),
        "FANDANGLEAF" => Fandangleaf(),
        "SPECTREVIX" => Spectrevix(),
        "AUGURHAWK" => Augurhawk(),
        "BERGBREAKER" => Bergbreaker(),
        _ => null
    };

    // ------------------------------------------------------------------ building on a forebear's sculpt

    /// <summary>
    /// Takes a forebear's finished build as the start of one of ours: renamed (the model and its description), and
    /// filling <paramref name="fill"/> of the sprite frame.
    /// </summary>
    private static PokeBuilder Become(PokeBuilder b, string species, float fill)
    {
        b.Model.Species = species;
        b.Model.Fill = fill;
        b.Sdf.Name = "pokemon " + species;
        return b;
    }

    /// <summary>Changes every shape and mark of one colour to another (exact colours, as the forebear's code names them).</summary>
    private static void Recolour(PokeBuilder b, params (Color from, Color to)[] map)
    {
        static bool Same(Color a, Color c) => a.R == c.R && a.G == c.G && a.B == c.B;
        foreach (var s in b.Sdf.Shapes)
            foreach (var (from, to) in map)
                if (Same(s.Color, from)) { s.Color = to; break; }
        foreach (var d in b.Model.Decals)
            foreach (var (from, to) in map)
            {
                if (Same(d.Color, from)) d.Color = to;
                if (d.Iris is { } iris && Same(iris, from)) d.Iris = to;
            }
        b.Sdf.Changed();
    }

    /// <summary>
    /// Stretches the whole sculpt about the origin by <paramref name="k"/> (taller and leaner, longer and lower):
    /// every shape's place and size, the joints and the marks. A shape turned askew is sized by how much each of its
    /// own axes is stretched, which is near enough for the soft shapes of a sculpt.
    /// </summary>
    private static void Stretch(PokeBuilder b, Vector3 k)
    {
        Grow(b, k, Vector3.Zero, (SdfPrimitive _) => true, _ => true);
        b.Model.Skeleton.Scale(k);
    }

    /// <summary>
    /// Stretches the shapes of the bones <paramref name="bones"/> says, and their marks, by <paramref name="k"/> about
    /// <paramref name="about"/> (a wing broadened from its shoulder, a shell grown round its middle); the joints stay.
    /// </summary>
    private static void Grow(PokeBuilder b, Vector3 k, Vector3 about, Func<int, bool> bones) =>
        Grow(b, k, about, s => bones(s.Bone), bones);

    /// <summary>Stretches the shapes <paramref name="which"/> picks by <paramref name="k"/> about <paramref name="about"/>, and the marks of the bones <paramref name="marks"/> picks.</summary>
    private static void Grow(PokeBuilder b, Vector3 k, Vector3 about, Func<SdfPrimitive, bool> which, Func<int, bool> marks)
    {
        float Along(Vector3 axis) => (k * Vector3.Normalize(axis)).Length();
        float Across(Vector3 axis)
        {
            var d = Vector3.Normalize(axis);
            return (k.X * (1f - d.X * d.X) + k.Y * (1f - d.Y * d.Y) + k.Z * (1f - d.Z * d.Z)) / 2f;
        }
        float mean = (k.X + k.Y + k.Z) / 3f;
        foreach (var s in b.Sdf.Shapes)
        {
            if (!which(s)) continue;
            var q = s.Rotation;
            var ax = Vector3.Transform(Vector3.UnitX, q);
            var ay = Vector3.Transform(Vector3.UnitY, q);
            var az = Vector3.Transform(Vector3.UnitZ, q);
            switch (s.Shape)
            {
                case SdfShape.RoundCone:
                    float across = s.B == s.A ? mean : Across(s.B - s.A);
                    s.Size = new Vector3(s.Size.X * across, s.Size.Y * across, s.Size.Z);
                    break;
                case SdfShape.Sphere:
                    s.Size = new Vector3(s.Size.X * mean, s.Size.Y, s.Size.Z);
                    break;
                case SdfShape.Torus:
                    s.Size = new Vector3(s.Size.X * Across(ay), s.Size.Y * mean, s.Size.Z);
                    break;
                case SdfShape.Cylinder:
                    s.Size = new Vector3(s.Size.X * Across(ay), s.Size.Y * Along(ay), s.Size.Z);
                    break;
                default:
                    s.Size = new Vector3(s.Size.X * Along(ax), s.Size.Y * Along(ay), s.Size.Z * Along(az));
                    break;
            }
            s.A = about + (s.A - about) * k;
            s.B = about + (s.B - about) * k;
            s.Blend *= mean;
        }
        b.Sdf.Changed();
        foreach (var d in b.Model.Decals)
        {
            if (!marks(d.Bone)) continue;
            d.Center = about + (d.Center - about) * k;
            d.Normal = Vector3.Normalize(d.Normal / k);
        }
    }

    /// <summary>Moves the shapes and marks of the bones <paramref name="bones"/> picks by <paramref name="d"/> (a head carried up on a longer neck); the joints stay.</summary>
    private static void Move(PokeBuilder b, Func<int, bool> bones, Vector3 d)
    {
        foreach (var s in b.Sdf.Shapes)
            if (bones(s.Bone))
            {
                s.A += d;
                s.B += d;
            }
        b.Sdf.Changed();
        foreach (var m in b.Model.Decals)
            if (bones(m.Bone)) m.Center += d;
    }

    /// <summary>Paints a colour and a material together over the surface of <paramref name="bone"/> inside an ellipsoid (steel over a claw).</summary>
    private static void PaintAs(PokeBuilder b, int bone, Vector3 center, Vector3 radii, Color color, SurfaceMaterial mat, Vector3 rotationDeg = default)
    {
        var rot = Quaternion.CreateFromYawPitchRoll(rotationDeg.Y * Degree, rotationDeg.X * Degree, rotationDeg.Z * Degree);
        b.Sdf.Paint(b.Sdf.Ellipsoid(center, radii, color, bone, PokeBuilder.PaintEdge, mat, rot), true, bone);
    }

    /// <summary>A bone of the build by its name (it must be there).</summary>
    private static int BoneOf(PokeBuilder b, string name)
    {
        int i = b.Model.Skeleton.Find(name);
        if (i < 0) throw new InvalidOperationException($"{b.Model.Species} has no bone {name}");
        return i;
    }

    private static readonly Color Steel = Rgb(170, 176, 188);

    // ------------------------------------------------------------------ 2020 Ratchisel, from Raticate

    /// <summary>
    /// Ratchisel: Raticate down on all fours, lower and longer, its brown fur grizzled grey along the back, its
    /// whiskers kept, its buck teeth grown into square chisels of steel and its scaly tail thickened with three
    /// steel rings.
    /// </summary>
    /// <param name="female">A female keeps Raticate's: shorter whiskers.</param>
    private static PokeBuilder Ratchisel(bool female = false)
    {
        var b = new PokeBuilder("Ratchisel", 0.84f, BodyPlan.Quadruped, V(0, 0.25f, -0.04f)) { Coat = Fur };
        var fur = Rgb(150, 116, 86);
        var grey = Rgb(138, 136, 132);
        var cream = Rgb(184, 150, 116);
        var tailColor = Rgb(170, 134, 98);
        foreach (var (z, front) in new[] { (0.14f, true), (-0.22f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.1f * s, 0.2f, z), front);
                if (front) b.Limb(leg, V(0.1f * s, 0.22f, z), V(0.11f * s, 0.05f, z + 0.02f), 0.05f, 0.036f, fur);
                else
                {
                    // Haunches as heavy as Raticate's, folded over the hind feet
                    b.Ell(leg, V(0.11f * s, 0.17f, z), V(0.07f, 0.1f, 0.1f), fur);
                    b.Limb(leg, V(0.11f * s, 0.12f, z - 0.02f), V(0.115f * s, 0.05f, z + 0.01f), 0.04f, 0.034f, fur);
                }
                b.Ell(leg, V(0.115f * s, 0.024f, z + 0.05f), V(0.042f, 0.024f, 0.07f), PixelCanvas.Shadow(fur, 0.15f));
                Claws(b, leg, V(0.115f * s, 0.02f, z + 0.11f), V(0.014f, 0, 0), V(0, -0.2f, 1f), 0.024f, 0.008f);
            });
        b.Ell(Body, V(0, 0.25f, -0.04f), V(0.16f, 0.14f, 0.28f), fur);
        b.PaintEll(Body, V(0, 0.16f, 0.0f), V(0.12f, 0.07f, 0.22f), cream);
        // Grizzled grey shag over the whole of its back, from the crown of its head to its rump
        b.PaintEll(Body, V(0, 0.36f, -0.06f), V(0.15f, 0.08f, 0.3f), grey);
        Patches(b, Body, grey, (V(0.12f, 0.31f, 0.1f), 0.04f), (V(-0.13f, 0.3f, -0.04f), 0.04f), (V(0.13f, 0.3f, -0.18f), 0.035f), (V(-0.1f, 0.32f, -0.2f), 0.035f));
        for (int i = 0; i < 9; i++)
        {
            float z = 0.14f - i * 0.05f;
            float x = (i % 3 - 1) * 0.05f;
            b.Spike(Body, V(x, 0.37f, z), V(x * 1.3f, 0.42f, z - 0.05f), 0.026f, grey, 0.5f, blend: 0.012f);
        }
        // A long thick tail lying out behind and curling to a fine point, banded with four rings of steel
        int tail = b.Tail(V(0, 0.2f, -0.3f));
        var path = Smooth(3, V(0, 0.2f, -0.3f), V(0.05f, 0.1f, -0.46f), V(0.18f, 0.05f, -0.6f), V(0.34f, 0.06f, -0.66f), V(0.46f, 0.1f, -0.6f), V(0.5f, 0.06f, -0.48f));
        b.Tube(tail, path, 0.05f, 0.012f, tailColor, blend: 0f);
        for (int i = 2; i < path.Length - 2; i += 2)
            b.PaintTorus(tail, path[i], 0.046f - 0.0024f * i, 0.005f, PixelCanvas.Shadow(tailColor, 0.2f), Euler(path[i + 1] - path[i - 1]));
        foreach (int i in new[] { 3, 5, 7, 9 })
        {
            float t = i / (float)(path.Length - 1);
            b.Torus(tail, path[i], 0.05f - 0.038f * t + 0.004f, 0.012f, Steel, Euler(path[i + 1] - path[i - 1]), mat: Metal, blend: 0.004f);
        }
        b.Limb(Body, V(0, 0.3f, 0.16f), V(0, 0.32f, 0.24f), 0.09f, 0.08f, fur);
        int head = b.Head(V(0, 0.32f, 0.22f));
        var c = V(0, 0.36f, 0.3f);
        var r = V(0.12f, 0.105f, 0.12f);
        b.Ell(head, c, r, fur);
        b.PaintEll(head, V(0, 0.4f, 0.24f), V(0.08f, 0.05f, 0.1f), grey);
        b.Ell(head, V(0, 0.32f, 0.4f), V(0.075f, 0.06f, 0.075f), fur);
        b.PaintEll(head, V(0, 0.3f, 0.42f), V(0.07f, 0.045f, 0.06f), cream);
        b.Ell(head, V(0, 0.345f, 0.47f), V(0.022f, 0.018f, 0.016f), Rgb(60, 40, 40), blend: 0.006f);
        // The chisels: two broad teeth of steel, cut square at their ends
        PokeBuilder.Both(s => b.Box(head, V(0.024f * s, 0.22f, 0.45f), V(0.022f, 0.07f, 0.012f), 0.003f, Steel, V(-6f, 0, 0), Metal, 0.004f));
        Whiskers(b, head, V(0.05f, 0.32f, 0.44f), female ? 0.1f : 0.17f, Rgb(70, 60, 54), 0.006f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.08f * s, 0.43f, 0.24f));
            b.Ell(ear, V(0.1f * s, 0.455f, 0.235f), V(0.032f, 0.034f, 0.016f), fur, V(0, -10f * s, -30f * s));
            b.PaintEll(ear, V(0.1f * s, 0.455f, 0.245f), V(0.02f, 0.022f, 0.012f), PixelCanvas.Shadow(fur, 0.3f), V(0, -10f * s, -30f * s));
        });
        foreach (var (x, h) in new[] { (-0.03f, 0.04f), (0.0f, 0.05f), (0.03f, 0.04f) })
            b.Spike(head, V(x, 0.44f, 0.22f), V(x * 1.4f, 0.44f + h, 0.15f), 0.024f, grey, 0.5f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.055f * s, 0.39f), V(0.55f * s, 0.15f, 1f), 0.026f, glare: true));
        return b;
    }

    // ------------------------------------------------------------------ 2038 Kitsunox, from Ninetales

    /// <summary>
    /// Kitsunox: Ninetales, its gold paled to cream, its nine tails longer and wispier, each ending in a pale violet
    /// flame, its ruff grown into a mantle and a small pale flame floating at its brow on a thin strand.
    /// </summary>
    private static PokeBuilder Kitsunox()
    {
        var b = Become(NinetalesBuild(false), "Kitsunox", 0.96f);
        var cream = Rgb(244, 216, 146);
        var tailCream = Rgb(242, 220, 160);
        var wispy = Rgb(222, 210, 240);
        var violet = Rgb(176, 136, 238);
        var pale = Rgb(240, 230, 255);
        var mane = Rgb(248, 232, 184);
        Recolour(b, (Rgb(246, 228, 164), cream), (Rgb(248, 232, 170), tailCream), (Rgb(246, 156, 72), wispy),
            (Rgb(252, 246, 222), mane), (Rgb(214, 50, 56), Rgb(150, 92, 220)));
        // Each tail runs on past its end, thinning to a wisp, and burns at its tip in pale violet
        int tail = BoneOf(b, "tail");
        for (int i = 0; i < 9; i++)
        {
            float a = (-72f + 18f * i) * Degree;
            var flat = Vector3.Normalize(V(MathF.Sin(a), 0, -MathF.Cos(a) * 0.9f));
            var root = V(0, 0.41f, -0.22f);
            float len = 0.5f - 0.08f * MathF.Abs(MathF.Sin(a));
            var end = root + flat * len + V(0, 0.04f, 0);
            var wisp = end + flat * 0.12f + V(0, 0.18f, 0);
            b.Tube(tail, Smooth(3, end - flat * 0.04f, end + flat * 0.08f + V(0, 0.05f, 0), wisp), 0.034f, 0.016f, wispy, blend: 0f);
            FlameTongue(b, tail, wisp - flat * 0.01f, wisp + flat * 0.05f + V(0, 0.09f, 0), 0.026f, violet, pale);
        }
        // The ruff grown into a mantle over the shoulders, falling in flame-like locks down the breast and back
        b.Ell(Body, V(0, 0.45f, 0.07f), V(0.13f, 0.075f, 0.12f), mane, blend: 0.03f);
        PokeBuilder.Both(s => b.Ell(Body, V(0.09f * s, 0.4f, 0.1f), V(0.06f, 0.09f, 0.07f), mane, blend: 0.03f));
        foreach (float x in new[] { -0.06f, -0.02f, 0.02f, 0.06f })
            b.Spike(Body, V(x, 0.36f, 0.17f), V(x * 1.3f, 0.22f, 0.2f), 0.026f, mane, 0.5f);
        PokeBuilder.Both(s =>
        {
            foreach (var (y, z) in new[] { (0.5f, 0.02f), (0.46f, -0.06f), (0.42f, -0.13f) })
            {
                var root = V(0.09f * s, y, z);
                b.Tube(Body, Smooth(3, root, root + V(0.06f * s, 0.06f, -0.06f), root + V(0.08f * s, 0.12f, -0.14f)), 0.026f, 0.008f, mane, blend: 0.01f);
            }
        });
        // A pale flame at the brow on a thin strand
        int head = BoneOf(b, "head");
        b.Tube(head, Smooth(3, V(0, 0.665f, 0.215f), V(0, 0.72f, 0.26f), V(0, 0.77f, 0.25f)), 0.007f, 0.005f, cream, blend: 0.004f);
        FlameTongue(b, head, V(0, 0.765f, 0.25f), V(0, 0.84f, 0.24f), 0.022f, violet, pale);
        return b;
    }

    // ------------------------------------------------------------------ 2083 Porrigard, from Farfetch'd

    /// <summary>
    /// Porrigard: Farfetch'd grown taller and leaner, a cape of white feathers over its shoulders, a few grey
    /// feathers in its brows, and its leek grown into a long staff tipped with a bundle of green leaves.
    /// </summary>
    private static PokeBuilder Porrigard()
    {
        var b = Become(FarfetchdBuild(false), "Porrigard", 0.86f);
        // The leek goes: the staff takes its place in the same wing
        int grip = BoneOf(b, "wingL");
        var leekWhite = Rgb(242, 244, 232);
        var leekGreen = Rgb(96, 170, 72);
        b.Sdf.RemoveAll(s => s.Bone == grip && (s.Color.Equals(leekWhite) || s.Color.Equals(leekGreen)));
        Stretch(b, V(0.94f, 1.14f, 0.95f));
        var white = Rgb(246, 244, 238);
        var green = Rgb(80, 156, 64);
        // The staff, from the ground beside its foot up past its head, a sage stalk greening toward its top, bound
        // with a cord under its bundle of leaves
        var foot = V(-0.12f, 0.016f, 0.13f);
        var top = V(-0.19f, 0.86f, 0.0f);
        var d = top - foot;
        var up = Vector3.Normalize(d);
        // A great leek for a staff: a thick white stalk greening toward its top, a sheaf of long leaves fanning from it
        Recolour(b, (Rgb(178, 158, 140), Rgb(188, 166, 126)), (Rgb(116, 96, 84), Rgb(150, 128, 98)));
        b.Limb(grip, foot, foot + d * 0.62f, 0.028f, 0.027f, Rgb(238, 240, 224), Leaf, 0.008f);
        b.Limb(grip, foot + d * 0.6f, top, 0.027f, 0.026f, Rgb(184, 214, 150), Leaf, 0.012f);
        var side = Vector3.Normalize(Vector3.Cross(d, V(0, 0, 1f)));
        foreach (var (t, z, len) in new[] { (-1.6f, 0.03f, 0.26f), (-1.1f, -0.04f, 0.32f), (-0.6f, 0.05f, 0.36f), (-0.2f, -0.05f, 0.38f), (0.2f, 0.04f, 0.37f), (0.6f, -0.03f, 0.34f), (1.1f, 0.05f, 0.3f), (1.6f, -0.02f, 0.25f), (0f, 0.07f, 0.33f) })
        {
            var root = top - up * 0.02f;
            var mid = root + up * len * 0.5f + side * t * 0.03f + V(0, 0, z * 0.5f);
            var tip = root + up * len + side * t * 0.1f + V(0, -MathF.Abs(t) * 0.04f, z);
            Frond(b, grip, root, mid, 0.026f, green, V(0, 0, 1f), 0.45f, Leaf);
            Frond(b, grip, mid - (mid - root) * 0.15f, tip, 0.024f, PixelCanvas.Shadow(green, 0.12f), V(0, 0, 1f), 0.45f, Leaf);
        }
        // A cape of white feathers over its shoulders and down its back, its edges showing in front
        b.Ell(Body, V(0, 0.46f, -0.04f), V(0.14f, 0.06f, 0.12f), white, V(14f, 0, 0), blend: 0.02f);
        PokeBuilder.Both(s => b.Ell(Body, V(0.1f * s, 0.43f, 0.03f), V(0.055f, 0.06f, 0.05f), white, blend: 0.02f));
        for (int i = 0; i < 5; i++)
        {
            float x = -0.08f + 0.04f * i;
            Blade(b, Body, V(x, 0.46f, -0.12f), V(x * 1.35f, 0.22f - MathF.Abs(x) * 0.4f, -0.2f), 0.05f, white, V(0, 0.2f, -1f), 0.25f);
        }
        // Grey feathers grown into its brows
        int head = BoneOf(b, "head");
        PokeBuilder.Both(s => b.Spike(head, V(0.05f * s, 0.685f, 0.05f), V(0.1f * s, 0.73f, 0.02f), 0.012f, Rgb(176, 176, 182), 0.5f));
        return b;
    }

    // ------------------------------------------------------------------ 2099 Pincerex, from Kingler

    /// <summary>
    /// Pincerex: Kingler with its great claw grown greater still and tempered to steel rimmed in red, the small claw
    /// a little bigger, its shell a darker red with spikes along its edge, the crown of five spikes on its head kept.
    /// </summary>
    private static PokeBuilder Pincerex()
    {
        var red = Rgb(124, 34, 44);
        var grey = Rgb(132, 138, 150);
        var spike = Rgb(140, 112, 116);
        var b = CrabBuild("Pincerex", 0.92f, 0.085f, 0.18f, 5, red, grey);
        // Legs and underside armoured in steel
        Recolour(b, (Rgb(214, 210, 204), grey));
        foreach (var s in b.Sdf.Shapes.Where(s => s.Color.Equals(grey))) s.Material = Metal;
        // A broader, flatter shell
        Grow(b, V(1.25f, 1f, 1.2f), V(0, 0.17f, 0), s => s.Bone == Body && s.Shape == SdfShape.Ellipsoid && s.Color.Equals(red) && s.Size.X > 0.1f, _ => false);
        // The great claw, steel with a red rim at the wrist and down its inner edge
        int arm = BoneOf(b, "armR");
        var elbow = V(0.22f, 0.24f, 0.08f);
        var d = Vector3.Normalize(V(0.15f, 1f, 0.35f));
        PaintAs(b, arm, elbow + d * 0.18f * 1.4f, V(0.2f, 0.26f, 0.2f), Steel, Metal, Euler(d));
        b.Torus(arm, elbow + d * 0.18f * 0.35f, 0.075f, 0.016f, red, Euler(d), mat: Shell, blend: 0.006f);
        b.Torus(arm, elbow + d * 0.18f * 0.85f, 0.11f, 0.012f, Rgb(100, 104, 116), Euler(d), mat: Metal, blend: 0.006f);
        // Spikes along the rim of its shell, behind and at the sides, and up its back
        for (int i = 0; i < 11; i++)
        {
            float a = (195f + 150f * i / 10f) * Degree;
            var at = V(0.2f * MathF.Cos(a), 0.18f, 0.144f * MathF.Sin(a));
            var outward = Vector3.Normalize(V(MathF.Cos(a), 0.35f, MathF.Sin(a) * 0.9f));
            b.Spike(Body, at - outward * 0.01f, at + outward * 0.07f, 0.017f, spike, mat: Shell);
        }
        foreach (var (x, z) in new[] { (-0.07f, -0.04f), (0.07f, -0.04f), (0f, -0.08f), (-0.1f, 0.02f), (0.1f, 0.02f) })
            b.Spike(Body, V(x, 0.235f, z), V(x * 1.3f, 0.31f, z - 0.03f), 0.016f, spike, mat: Shell);
        return b;
    }

    /// <summary>How far a forebear's build was moved after it was sculpted (a flier lifted off the ground): where the joint of <paramref name="bone"/> is, less where it was sculpted.</summary>
    private static Vector3 Shifted(PokeBuilder b, string bone, Vector3 sculptedAt) => b.Model.Skeleton[BoneOf(b, bone)].Joint - sculptedAt;

    /// <summary>The bone an Arbok-built coil's point <paramref name="i"/> lies on (a bone for every three points, <see cref="Coils"/>).</summary>
    private static int CoilBone(PokeBuilder b, int i, int per = 3) => i < per ? Body : BoneOf(b, "seg" + i / per);

    // ------------------------------------------------------------------ 2024 Najarok, from Arbok

    /// <summary>
    /// Najarok: Arbok longer and heavier, its purple deepened toward violet with thin gold bands down its body, its
    /// hood widened into a crowned frill, and the pattern on the hood woken into a second face whose eyes open and
    /// blink.
    /// </summary>
    private static PokeBuilder Najarok()
    {
        var b = Become(Arbok(), "Najarok", 0.98f);
        var violet = Rgb(86, 58, 128);
        var gold = Rgb(214, 176, 96);
        Recolour(b, (Rgb(150, 116, 184), violet), (Rgb(214, 198, 160), Rgb(118, 92, 164)), (Rgb(36, 30, 40), Rgb(52, 32, 78)));
        // Gold chevrons down the paler belly of its neck
        int neckBone = b.Sdf.Shapes.First(s => s.Color.Equals(Rgb(118, 92, 164))).Bone;
        for (int i = 0; i < 6; i++)
        {
            var at = V(0, 0.45f + i * 0.04f, 0.155f - i * 0.002f);
            PokeBuilder.Both(s => b.PaintEll(neckBone, at + V(0.022f * s, -0.008f, 0), V(0.026f, 0.005f, 0.03f), gold, V(0, 0, 28f * s)));
        }
        // A small crown of points on its head
        int skull = BoneOf(b, "head");
        foreach (var (x, h) in new[] { (-0.03f, 0.04f), (0f, 0.06f), (0.03f, 0.04f) })
            b.Spike(skull, V(x, 0.815f, 0.07f), V(x * 1.3f, 0.82f + h, 0.04f), 0.014f, violet, 0.5f);
        // Thin gold bands round the body, a pair of points apart, from the tail to the neck
        var coil = Spiral3(0.28f, 0.15f, 0.07f, 0.17f, 2.2f, 1.3f, 20);
        var points = new[] { V(0.34f, 0.05f, -0.36f), V(0.3f, 0.06f, -0.3f) }.Concat(coil)
            .Concat(new[] { V(0.0f, 0.28f, 0.08f), V(0, 0.44f, 0.1f), V(0, 0.58f, 0.08f), V(0, 0.72f, 0.06f) }).ToArray();
        for (int i = 3; i < points.Length - 3; i += 2)
        {
            float t = i / (float)(points.Length - 1);
            float radius = 0.022f + 0.05f * MathF.Min(1f, t * 2.5f);
            b.PaintTorus(CoilBone(b, i), points[i], radius, 0.007f, gold, Euler(points[i + 1] - points[i - 1]));
        }
        // The hood widens into a frill crowned with points
        int hood = BoneOf(b, "hood");
        var hoodAt = V(0, 0.6f, 0.04f);
        Grow(b, V(1.35f, 1.2f, 1f), hoodAt, bone => bone == hood);
        // Its rim burns into points like a dark flame, all the way round its sides and crown
        for (int i = 0; i < 13; i++)
        {
            float a = (-110f + 220f * i / 12f) * Degree;
            var rim = V(0, 0.62f, 0.02f) + V(MathF.Sin(a) * 0.25f, MathF.Cos(a) * 0.215f, 0);
            var outward = Vector3.Normalize(V(MathF.Sin(a), MathF.Cos(a) * 1.1f + 0.35f, -0.05f));
            b.Spike(hood, rim - outward * 0.025f, rim + outward * (i % 2 == 0 ? 0.11f : 0.07f), 0.03f, violet, 0.4f);
        }
        // The face on the hood wakes: its two rings become pale violet eyes with slit pupils that open and blink
        var rings = b.Model.Decals.Where(d => d.Bone == hood && d.Shape == MarkShape.Disc).ToList();
        var eyes = rings.Where(d => d.Color.R > 230).Select(d => (d.Center, d.Normal)).ToList();
        b.Model.Decals.RemoveAll(d => rings.Contains(d));
        foreach (var (at, normal) in eyes)
        {
            b.Mark(hood, at, normal, 0.07f, 0.05f, Rgb(46, 28, 70), MarkShape.Disc);
            b.Eye(hood, at, normal, 0.044f, sclera: true, white: Rgb(206, 186, 238), pupil: Rgb(40, 22, 62));
        }
        Stretch(b, V(1.08f, 1.06f, 1.08f));
        return b;
    }

    // ------------------------------------------------------------------ 2047 Cordyhusk, from Parasect

    /// <summary>
    /// Cordyhusk: Parasect after the mushroom has taken its host whole: the cap swollen into a broad parasol hanging
    /// over the body like a cloak, its spots glowing pale, threads of white mycelium hanging from its rim, and the bug
    /// beneath a pale hollow husk with its eyes painted out.
    /// </summary>
    private static PokeBuilder Cordyhusk()
    {
        var b = Become(Parasect(), "Cordyhusk", 0.88f);
        var husk = Rgb(210, 206, 192);
        var red = Rgb(164, 58, 52);
        var pale = Rgb(170, 240, 226);
        var thread = Rgb(236, 236, 228);
        Recolour(b, (Rgb(242, 150, 84), husk), (Rgb(214, 122, 66), Rgb(178, 172, 154)), (Rgb(210, 104, 150), red), (Rgb(246, 220, 186), Rgb(150, 92, 82)));
        // The spots glow
        foreach (var s in b.Sdf.Shapes.Where(s => s.Op == SdfOp.Paint && s.Color.Equals(Rgb(250, 202, 96))))
        {
            s.Color = pale;
            s.Material = Glow;
            s.PaintsMaterial = true;
        }
        // The cap swells into a parasol that hangs over the body
        int mushroom = BoneOf(b, "mushroom");
        var top = V(0, 0.37f, -0.08f);
        Grow(b, V(1.3f, 1.12f, 1.3f), top, bone => bone == mushroom);
        // Raised up over the husk on a pale stalk grown out of its back, so the parasol hangs above it
        var raise = V(0, 0.12f, 0.03f);
        Move(b, bone => bone == mushroom, raise);
        top += raise;
        b.Limb(mushroom, V(0, 0.22f, -0.1f), top + V(0, -0.04f, 0), 0.06f, 0.075f, Rgb(214, 206, 188), blend: 0.03f);
        var turn = V(-12f, 0, 0);
        var capR = V(0.27f, 0.17f, 0.29f) * V(1.3f, 1.12f, 1.3f);
        var q = Quaternion.CreateFromYawPitchRoll(0, turn.X * Degree, 0);
        // Its rim droops into a cloak
        b.Ell(mushroom, top + V(0, -0.06f, 0), V(capR.X * 1.02f, 0.08f, capR.Z * 1.02f), red, turn, blend: 0.03f);
        // Threads of mycelium hanging from the rim, all round but before its face
        for (int i = 0; i < 14; i++)
        {
            float a = (40f + i * (280f / 13f)) * Degree;
            var rim = top + Vector3.Transform(V(MathF.Sin(a) * capR.X * 0.97f, -0.11f, MathF.Cos(a) * capR.Z * 0.97f), q);
            float drop = 0.11f + 0.05f * ((i * 7) % 3);
            b.Tube(mushroom, new[] { rim + V(0, 0.02f, 0), rim - V(0, drop * 0.5f, 0) + V(MathF.Sin(i * 1.3f) * 0.01f, 0, 0), rim - V(0, drop, 0) }, 0.008f, 0.004f, thread, blend: 0.004f);
            b.Ell(mushroom, rim - V(0, drop, 0), V(0.008f, 0.01f, 0.008f), thread, blend: 0.003f);
        }
        // The husk's eyes painted out
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
        {
            d.White = Rgb(208, 194, 168);
            d.Pupil = Rgb(186, 172, 150);
        }
        return b;
    }

    // ------------------------------------------------------------------ 2078 Solarion, from Rapidash

    /// <summary>
    /// Solarion: Rapidash taller and more muscular, its horn longer and spiralled, two wings of flame spread from its
    /// shoulders, its mane and tail streaming further and flames at its hooves.
    /// </summary>
    private static PokeBuilder Solarion()
    {
        var b = Become(Horse(true), "Solarion", 0.98f);
        const float k = 1.12f;
        int head = BoneOf(b, "head");
        // A coat of burnished gold-bronze, dark hooves
        Recolour(b, (Rgb(252, 240, 192), Rgb(196, 140, 66)), (Rgb(140, 130, 170), Rgb(66, 44, 38)));
        // Heavier in the chest and the haunches
        Grow(b, V(1.12f, 1.1f, 1.04f), V(0, 0.52f, -0.02f), s => s.Bone == Body && s.Shape == SdfShape.Ellipsoid && s.Color.Equals(Rgb(196, 140, 66)), _ => false);
        // A longer horn, a band of gold spiralling up it
        var ivory = Rgb(250, 246, 236);
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.Equals(ivory));
        var hornFoot = V(0, 0.9f, 0.36f * k);
        var hornTip = V(0, 1.2f, 0.54f * k);
        b.Spike(head, hornFoot, hornTip, 0.04f, ivory, mat: Shell);
        var axis = Vector3.Normalize(hornTip - hornFoot);
        var u = Vector3.Normalize(Vector3.Cross(axis, V(1f, 0, 0)));
        var w = Vector3.Cross(axis, u);
        var helix = Enumerable.Range(0, 25).Select(i =>
        {
            float t = i / 24f;
            float rad = 0.034f * (1f - t) + 0.006f;
            float a = t * MathF.Tau * 2.5f;
            return hornFoot + axis * Vector3.Distance(hornFoot, hornTip) * t * 0.92f + (u * MathF.Cos(a) + w * MathF.Sin(a)) * rad;
        }).ToArray();
        b.Tube(head, helix, 0.008f, 0.004f, Rgb(244, 196, 96), Shell, 0.003f);
        // Two wings of flame from its shoulders
        var rim = Rgb(248, 128, 48);
        var heart = Rgb(255, 226, 120);
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.08f * s, 0.6f, 0.06f);
            int wing = b.Wing(s, shoulder);
            // A feathered wing of flame, its long feathers rising and swept back, licking into tongues at their ends
            var wrist = V(0.24f * s, 0.86f, -0.04f);
            OpenWing(b, wing, s, shoulder, wrist, 9, 112f, 10f, 0.44f, 0.06f, rim, Rgb(255, 196, 84), heart, 0.7f);
            foreach (var (tip, r) in new[] { (V(0.22f * s, 1.3f, -0.12f), 0.04f), (V(0.4f * s, 1.22f, -0.2f), 0.04f), (V(0.54f * s, 1.04f, -0.3f), 0.036f) })
                FlameTongue(b, wing, wrist, tip, r, rim, heart);
        });
        // The mane and the tail streaming further, flames at the hooves
        int tail = BoneOf(b, "tail");
        Blaze(b, tail, V(0, 0.58f, -0.26f * k), V(0, 0.3f, -1f), 0.5f * k, 0.06f * k);
        Blaze(b, tail, V(0, 0.58f, -0.26f * k), V(-0.2f, 0.45f, -1f), 0.42f * k, 0.05f * k);
        int mane = BoneOf(b, "mane");
        Blaze(b, mane, V(0, 0.8f, 0.22f * k), V(0, 0.55f, -1f), 0.4f * k, 0.065f * k);
        foreach (var (z, front) in new[] { (0.17f, true), (-0.21f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = BoneOf(b, (front ? "leg" : "hind") + (s < 0 ? "L" : "R"));
                Blaze(b, leg, V(0.085f * s, 0.04f, (z + 0.0f) * k), V(0.3f * s, 0.25f, -1f), 0.11f, 0.026f);
            });
        // The wings widen the sculpt and coarsen its mesh, where the nostrils' small marks lose the triangles under them:
        // they are painted instead
        b.Model.Decals.RemoveAll(d => !d.IsEye && d.Bone == head);
        PokeBuilder.Both(s => b.PaintEll(head, V(0.028f * s, 0.83f, 0.48f * k), V(0.011f, 0.009f, 0.014f), Rgb(120, 100, 90), soft: 0.006f));
        Stretch(b, V(1.08f, 1.08f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2105 Ossugard, from Marowak

    /// <summary>
    /// Ossugard: Marowak taller, its skull helmet grown into a great horned skull, a pale blue flame burning in its
    /// eyes, its club grown into a long staff of bone with a skull at its head.
    /// </summary>
    private static PokeBuilder Ossugard()
    {
        var b = Become(SkullBuild(true, false), "Ossugard", 0.86f);
        var skull = Rgb(232, 230, 224);
        var blue = Rgb(120, 200, 255);
        var paleBlue = Rgb(232, 248, 255);
        int grip = BoneOf(b, "armL");
        int head = BoneOf(b, "head");
        b.Sdf.RemoveAll(s => s.Bone == grip && s.Color.Equals(skull));
        // Dark scaled hide where Marowak's was tan
        var hide = Rgb(86, 78, 64);
        Recolour(b, (Rgb(198, 160, 112), hide), (Rgb(242, 226, 184), Rgb(112, 102, 82)));
        // The horns curl back and round like a ram's
        var horn = Rgb(196, 182, 150);
        b.Sdf.RemoveAll(s => s.Bone == head && s.Shape == SdfShape.RoundCone && s.Color.Equals(skull));
        float h = 1.18f;
        var c = V(0, 0.43f * h, 0.04f);
        var r = V(0.12f, 0.1f, 0.13f);
        PokeBuilder.Both(s => RamHorn(b, head, c + V(0.08f * s, 0.06f, -0.04f), s, 0.11f, 0.034f, horn));
        // Blue fire in the eye holes, rising over the brow
        Recolour(b, (Black, blue));
        PokeBuilder.Both(s =>
        {
            var socket = On(c, r, 0.055f * s, c.Y - 0.005f);
            FlameTongue(b, head, socket + V(0.005f * s, 0.035f, -0.01f), socket + V(0.03f * s, 0.11f, -0.05f), 0.016f, blue, paleBlue);
        });
        // The staff, a great bone knobbed at its foot and bound with rags, a ram's skull at its head
        var bone = Rgb(214, 202, 170);
        // carried over its shoulder from the raised hand
        var bottom = V(-0.32f, 0.3f, 0.1f);
        var top = V(0.24f, 0.86f, -0.16f);
        b.Limb(grip, bottom, top, 0.03f, 0.028f, bone, Shell, 0.01f);
        foreach (float s in new[] { -1f, 1f })
            b.Ell(grip, bottom + V(0.025f * s, 0.006f, 0), V(0.034f, 0.032f, 0.034f), bone, mat: Shell, blend: 0.01f);
        var rag = Rgb(150, 152, 132);
        foreach (float t in new[] { 0.62f, 0.84f })
            b.Torus(grip, Vector3.Lerp(bottom, top, t), 0.034f, 0.009f, rag, Euler(top - bottom), blend: 0.004f);
        var sk = top + V(-0.01f, 0.05f, 0.01f);
        b.Ell(grip, sk, V(0.06f, 0.055f, 0.062f), skull, mat: Shell, blend: 0.012f);
        b.Ell(grip, sk + V(0, -0.03f, 0.055f), V(0.036f, 0.028f, 0.045f), skull, mat: Shell, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(grip, sk + V(0.024f * s, 0.005f, 0.054f), V(0.015f, 0.014f, 0.014f), Rgb(60, 52, 64));
            RamHorn(b, grip, sk + V(0.04f * s, 0.025f, -0.02f), s, 0.045f, 0.016f, horn);
        });
        FlameTongue(b, grip, sk + V(0, 0.04f, 0), sk + V(0, 0.12f, -0.02f), 0.022f, blue, paleBlue);
        // Rags bound round its wrists and ankles
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var wrist = s < 0 ? V(-0.15f, 0.37f * h, 0.02f) : V(0.155f, 0.19f * h, 0.075f);
            b.Torus(arm, wrist, 0.032f, 0.01f, rag, V(0, 0, 70f * s), blend: 0.004f);
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            b.Torus(leg, V(0.08f * s, 0.05f, 0.02f), 0.04f, 0.01f, rag, blend: 0.004f);
        });
        // A longer tail, a ridge of bone along it
        int tail = BoneOf(b, "tail");
        var tailPath = Smooth(3, V(0, 0.12f * h, -0.08f), V(0, 0.06f, -0.24f), V(0.06f, 0.05f, -0.38f), V(0.14f, 0.12f, -0.46f), V(0.18f, 0.24f, -0.48f));
        b.Tube(tail, tailPath, 0.04f, 0.012f, hide, blend: 0.01f);
        for (int i = 1; i < tailPath.Length - 1; i += 2)
        {
            var dir = Vector3.Normalize(tailPath[i + 1] - tailPath[i - 1]);
            var up = Vector3.Normalize(Vector3.Cross(Vector3.Cross(dir, V(0, 1f, 0)), dir));
            if (up.Y < 0) up = -up;
            float rad = 0.04f - 0.028f * i / (tailPath.Length - 1);
            b.Spike(tail, tailPath[i] + up * rad * 0.6f, tailPath[i] + up * (rad + 0.03f) - dir * 0.015f, 0.012f, bone, mat: Shell, blend: 0.004f);
        }
        Stretch(b, V(0.88f, 1.22f, 0.95f));
        return b;
    }

    /// <summary>A ram's horn from <paramref name="root"/> on side <paramref name="s"/>: back, down and round toward the front, <paramref name="r"/> across its curl.</summary>
    private static void RamHorn(PokeBuilder b, int bone, Vector3 root, float s, float r, float thick, Color color)
    {
        var centre = root + V(0.03f * s, -r * 0.55f, -r * 0.55f);
        var points = Enumerable.Range(0, 16).Select(i =>
        {
            float t = i / 15f;
            float a = MathF.PI * 0.75f - t * MathF.PI * 1.55f;
            float rad = r * (1f - 0.35f * t);
            return centre + V(s * (0.03f * t + 0.012f), MathF.Sin(a) * rad, -MathF.Cos(a) * rad);
        }).ToArray();
        points[0] = root;
        b.Tube(bone, points, thick, thick * 0.35f, color, Shell, 0.006f);
        for (int i = 3; i < points.Length - 2; i += 3)
            b.PaintTorus(bone, points[i], thick * (1f - 0.6f * i / 15f) * 1.02f, 0.003f, PixelCanvas.Shadow(color, 0.2f), Euler(points[i + 1] - points[i - 1]));
    }

    // ------------------------------------------------------------------ 2131 Ferrisle, from Lapras

    /// <summary>
    /// Ferrisle: Lapras much larger, its knobbled shell grown into a wide deck with a low crest of ice along its
    /// middle and small pines of ice on it, its neck and its horn longer.
    /// </summary>
    private static PokeBuilder Ferrisle()
    {
        var b = Become(LaprasBuild(false), "Ferrisle", 1f);
        var shell = Rgb(170, 170, 178);
        var blue = Rgb(92, 170, 230);
        var cream = Rgb(244, 236, 200);
        var ice = Rgb(204, 236, 250);
        // The knobs go, and the shell flattens and widens into a deck
        b.Sdf.RemoveAll(s => s.Bone == Body && s.Shape == SdfShape.RoundCone && s.Color.Equals(shell));
        var deckShape = b.Sdf.Shapes.First(s => s.Bone == Body && s.Shape == SdfShape.Ellipsoid && s.Color.Equals(shell));
        var dc = V(0, 0.38f, -0.08f);
        var dr = V(0.32f, 0.1f, 0.37f);
        // The point on the deck's top over (x, z)
        Vector3 Top(float x, float z) => V(x, dc.Y + dr.Y * MathF.Sqrt(MathF.Max(0f, 1f - x * x / (dr.X * dr.X) - (z - dc.Z) * (z - dc.Z) / (dr.Z * dr.Z))), z);
        deckShape.Color = Rgb(150, 152, 158);
        deckShape.A = dc;
        deckShape.Size = dr;
        b.Sdf.Changed();
        b.PaintTorus(Body, dc + V(0, -0.02f, 0), 0.33f, 0.014f, Rgb(118, 118, 126), default, sz: 0.37f / 0.32f);
        b.PaintEll(Body, dc + V(0, 0.08f, 0), V(0.3f, 0.04f, 0.35f), Rgb(228, 236, 244));
        // A low crest of ice down its middle
        for (int i = 0; i < 7; i++)
        {
            float z = 0.14f - i * 0.065f;
            var at = Top(0, z);
            b.Spike(Body, at - V(0, 0.015f, 0), at + V(0, 0.05f + 0.015f * (i % 2), -0.015f), 0.03f, Rgb(176, 222, 248), 0.5f, Shell);
        }
        // Small pines of ice standing on the deck
        foreach (var (x, z, size) in new[] { (0.16f, 0.06f, 1.4f), (-0.17f, -0.04f, 1.6f), (0.15f, -0.22f, 1.2f), (-0.12f, -0.28f, 1.1f), (0.21f, -0.1f, 1f), (-0.2f, 0.08f, 1.1f), (0.04f, -0.34f, 1.2f), (-0.06f, 0.1f, 0.9f), (0.24f, -0.3f, 0.9f) })
        {
            var foot = Top(x, z) - V(0, 0.012f, 0);
            for (int t = 0; t < 3; t++)
                b.Spike(Body, foot + V(0, 0.035f * t * size, 0), foot + V(0, (0.08f + 0.035f * t) * size, 0), (0.036f - 0.008f * t) * size, ice, mat: Shell, blend: 0.006f);
        }
        // The neck rises further, carrying the head, and the horn grows long
        int neck = BoneOf(b, "neck");
        int head = BoneOf(b, "head");
        var d = V(0, 0.11f, 0.03f);
        Move(b, bone => bone == head, d);
        b.Model.Skeleton.Shift(d, first: head);
        var spine = Smooth(3, V(0, 0.75f, 0.31f), V(0, 0.84f, 0.33f), V(0, 0.79f, 0.3f) + d);
        b.Tube(neck, spine, 0.056f, 0.055f, blue, blend: 0f);
        b.PaintEll(neck, V(0, 0.82f, 0.38f), V(0.04f, 0.07f, 0.035f), cream, Euler(V(0, 1f, 0.1f)));
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == head && s.Shape == SdfShape.RoundCone && s.Color.Equals(blue) && s.B.Y > s.A.Y + 0.05f && MathF.Abs(s.A.X) < 0.02f).ToList())
        {
            s.B = s.A + (s.B - s.A) * 2.4f;
            s.Color = Rgb(214, 238, 250);
            s.Material = Shell;
        }
        // Deep teal hide, pale as ice beneath; a frill of ice down the back of its neck
        Recolour(b, (blue, Rgb(46, 136, 150)), (cream, Rgb(206, 232, 242)));
        for (int i = 1; i < spine.Length - 1; i += 2)
        {
            var dirN = Vector3.Normalize(spine[i + 1] - spine[i - 1]);
            var backN = Vector3.Normalize(V(0, 0, -1f) - dirN * dirN.Z);
            b.Spike(neck, spine[i] + backN * 0.045f, spine[i] + backN * 0.09f + dirN * 0.01f, 0.018f, Rgb(214, 238, 250), 0.4f, Shell, 0.006f);
        }
        b.Sdf.Changed();
        return b;
    }

    // ------------------------------------------------------------------ 2139 Ammonarch, from Omastar

    /// <summary>
    /// Ammonarch: Omastar with its spiral shell grown much larger and ridged, spikes along its outer whorl, and its
    /// tentacles longer and more of them.
    /// </summary>
    private static PokeBuilder Ammonarch()
    {
        var b = Become(Omastar(), "Ammonarch", 0.92f);
        // Stone-brown shell banded slate blue, dark teal tentacles
        Recolour(b, (Rgb(232, 220, 182), Rgb(150, 132, 106)), (Rgb(172, 152, 108), Rgb(84, 104, 120)), (Rgb(98, 186, 220), Rgb(40, 100, 116)));
        var shell = Rgb(150, 132, 106);
        var blue = Rgb(40, 100, 116);
        var lift = Shifted(b, "head", V(0, 0.26f, 0.06f));
        var sc = V(0, 0.4f, -0.06f) + lift;
        // The shell grows and sits further back over its face
        Grow(b, V(1.35f, 1.35f, 1.35f), sc, bone => bone == Body);
        var back = V(0, 0.06f, -0.08f);
        Move(b, bone => bone == Body, back);
        sc += back;
        var sr = V(0.18f, 0.22f, 0.21f) * 1.35f;
        // A keel round its outer whorl, and spikes along it
        b.Torus(Body, sc, sr.Y * 1.0f, 0.02f, shell, V(0, 0, 90f), sz: sr.Z / sr.Y, mat: Shell, blend: 0.01f);
        for (int i = 0; i < 7; i++)
        {
            float a = (-20f + i * 32f) * Degree;
            var dir = V(0, MathF.Cos(a), -MathF.Sin(a));
            var at = Out(sc, sr, default, dir);
            var n = Outward(sc, sr, at);
            b.Spike(Body, at - n * 0.01f, at + n * 0.1f, 0.032f, PixelCanvas.Mix(shell, Rgb(170, 140, 100), 0.4f), mat: Shell);
        }
        // More tentacles, longer, between the old ones
        int head = BoneOf(b, "head");
        var c = V(0, 0.25f, 0.12f) + lift;
        var r = V(0.17f, 0.12f, 0.1f);
        for (int i = 0; i < 7; i++)
        {
            float a = (-100f + (i + 0.5f) * (200f / 7f)) * Degree;
            Tentacle(b, head, c + V(MathF.Sin(a) * r.X * 0.8f, -r.Y * 0.5f, MathF.Cos(a) * r.Z * 0.7f), V(MathF.Sin(a) * 1.3f, 0, MathF.Cos(a)), 0.34f, 0.03f, blue);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2141 Eurypterox, from Kabutops

    /// <summary>
    /// Eurypterox: Kabutops longer and lower, with a jointed tail ending in a flat paddle, its two scythes bigger and a
    /// second, smaller pair of claws below them.
    /// </summary>
    private static PokeBuilder Eurypterox()
    {
        // Kabutops gone down onto many legs as a sea scorpion: a long low body of slate plates, its two scythes held up
        // over its head, a smaller pair of claws before them, and a jointed tail arching up behind to a leaf-shaped paddle
        var b = new PokeBuilder("Eurypterox", 0.98f, BodyPlan.Quadruped, V(0, 0.2f, 0)) { Coat = Shell };
        var slate = Rgb(70, 92, 108);
        var plate = Rgb(92, 104, 112);
        var under = Rgb(64, 118, 150);
        var blade = Rgb(108, 120, 130);
        // The body, plates overlapping from the head back
        var spine = new[] { V(0, 0.2f, 0.16f), V(0, 0.21f, 0.06f), V(0, 0.21f, -0.04f), V(0, 0.2f, -0.14f), V(0, 0.19f, -0.24f) };
        for (int i = 0; i < spine.Length; i++)
        {
            float w = 0.14f - 0.012f * i;
            b.Ell(Body, spine[i], V(w, 0.075f, 0.075f), i % 2 == 0 ? slate : plate, mat: Shell, blend: 0.02f);
            b.Ell(Body, spine[i] + V(0, 0.05f, -0.01f), V(w * 0.95f, 0.04f, 0.07f), plate, V(-12f, 0, 0), Shell, 0.012f);
            b.PaintEll(Body, spine[i] - V(0, 0.06f, 0), V(w * 0.85f, 0.03f, 0.06f), under);
        }
        // Eight legs down its sides
        for (int i = 0; i < 4; i++)
            PokeBuilder.Both(s =>
            {
                float z = 0.12f - i * 0.1f;
                var hip = V(0.11f * s, 0.16f, z);
                int leg = i == 0 ? b.Leg(s, hip, true) : i == 3 ? b.Leg(s, hip, false) : b.Part($"leg{i}" + (s < 0 ? "L" : "R"), Body, hip, PokeRole.Leg, 0.4f * i, s);
                var knee = V(0.2f * s, 0.12f, z + 0.02f);
                var foot = V(0.24f * s, 0.01f, z + 0.04f - i * 0.01f);
                b.Limb(leg, hip, knee, 0.022f, 0.018f, under, Shell);
                b.Spike(leg, knee, foot, 0.016f, slate, mat: Shell, blend: 0.004f);
            });
        // A low head with a ridge of studs and pale eyes
        int head = b.Head(V(0, 0.2f, 0.22f));
        var hc = V(0, 0.2f, 0.27f);
        var hr = V(0.09f, 0.06f, 0.07f);
        b.Ell(head, hc, hr, slate, mat: Shell);
        foreach (var (x, z) in new[] { (0f, 0.0f), (-0.04f, -0.02f), (0.04f, -0.02f), (0f, -0.04f) })
            b.Spike(head, hc + V(x, 0.05f, z), hc + V(x * 1.2f, 0.08f, z - 0.01f), 0.012f, plate, mat: Shell, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            b.Eye(head, On(hc, hr, 0.04f * s, hc.Y + 0.01f), V(0.5f * s, 0.2f, 1f), 0.016f, sclera: true, white: Rgb(196, 246, 240), pupil: Rgb(220, 255, 250), glare: true);
            b.Spike(head, hc + V(0.025f * s, -0.04f, 0.05f), hc + V(0.02f * s, -0.09f, 0.07f), 0.01f, plate, mat: Shell, blend: 0.004f);
            // A small pair of claws before its face
            int claw = b.Part(s < 0 ? "clawL" : "clawR", Body, V(0.07f * s, 0.17f, 0.2f), PokeRole.Arm, s, s);
            var elbow = V(0.14f * s, 0.12f, 0.3f);
            var hand = V(0.12f * s, 0.08f, 0.4f);
            b.Limb(claw, V(0.07f * s, 0.17f, 0.2f), elbow, 0.02f, 0.018f, under, Shell);
            b.Limb(claw, elbow, hand, 0.018f, 0.022f, slate, Shell);
            foreach (float t in new[] { -1f, 1f })
                b.Spike(claw, hand, hand + V(0.02f * t * s, -0.01f, 0.05f), 0.012f, plate, mat: Shell, blend: 0.004f);
            // The two great scythes, raised high over the body
            int arm = b.Arm(s, V(0.1f * s, 0.23f, 0.12f));
            var shoulder = V(0.1f * s, 0.23f, 0.12f);
            var bend = V(0.2f * s, 0.46f, 0.1f);
            b.Limb(arm, shoulder, bend, 0.03f, 0.026f, under, Shell);
            b.Ell(arm, bend, V(0.03f, 0.03f, 0.03f), slate, mat: Shell);
            Blade(b, arm, bend, bend + V(0.06f * s, 0.04f, 0.3f), 0.06f, blade, V(s, 0, 0), 0.25f, Shell);
            b.Spike(arm, bend + V(0.02f * s, 0.02f, 0.18f), bend + V(0.03f * s, -0.04f, 0.16f), 0.01f, plate, mat: Shell, blend: 0.003f);
        });
        // The tail, jointed in plates, arching up behind to a leaf-shaped paddle with two small fins
        int tail = b.Tail(V(0, 0.2f, -0.28f));
        var path = Smooth(3, V(0, 0.19f, -0.28f), V(0, 0.2f, -0.44f), V(0, 0.3f, -0.56f), V(0, 0.48f, -0.6f), V(0, 0.62f, -0.54f));
        for (int i = 0; i < path.Length - 1; i++)
        {
            float t = i / (float)(path.Length - 1);
            var mid = (path[i] + path[i + 1]) / 2f;
            float len = Vector3.Distance(path[i], path[i + 1]);
            b.Ell(tail, mid, V(0.07f - 0.035f * t, len * 0.62f, 0.055f - 0.025f * t), i % 2 == 0 ? slate : plate, Euler(path[i + 1] - path[i]), Shell, 0.006f);
        }
        var end = path[^1];
        var up = Vector3.Normalize(path[^1] - path[^2]);
        Blade(b, tail, end - up * 0.02f, end + up * 0.22f + V(0, 0, 0.04f), 0.1f, Rgb(84, 116, 130), V(1f, 0, 0), 0.22f, Shell);
        PokeBuilder.Both(s => Blade(b, tail, end - up * 0.02f, end + V(0.1f * s, 0.06f, -0.02f), 0.035f, Rgb(84, 116, 130), V(0, 0.3f, 1f), 0.3f, Shell));
        return b;
    }

    // ------------------------------------------------------------------ 2142 Azhdarch, from Aerodactyl

    /// <summary>
    /// Azhdarch: Aerodactyl grown into a giant of the skies: its head carried high on a long stiff neck under a great
    /// crest, its wings broader and a deeper purple, and a ridge of amber stones down its back and tail.
    /// </summary>
    private static PokeBuilder Azhdarch()
    {
        var b = Become(AerodactylBuild(false), "Azhdarch", 1f);
        var grey = Rgb(110, 104, 92);
        var amber = Rgb(236, 160, 56);
        var lift = Shifted(b, "head", V(0, 0.6f, 0.06f));
        // A hide of weathered stone-grey, wings of deep purple
        Recolour(b, (Rgb(156, 126, 196), Rgb(84, 58, 128)), (Rgb(182, 174, 206), grey), (Rgb(74, 70, 84), Rgb(64, 60, 56)));
        // Broader wings
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            Grow(b, V(1.3f, 1.12f, 1f), V(0.08f * s, 0.56f, 0) + lift, bone => bone == wing);
        });
        // The head carried up and forward on a long stiff neck, which turns from its foot
        int head = BoneOf(b, "head");
        int jaw = BoneOf(b, "jaw");
        var d = V(0, 0.2f, 0.11f);
        Move(b, bone => bone == head || bone == jaw, d);
        b.Model.Skeleton.Shift(d, first: jaw);
        b.Limb(head, V(0, 0.56f, 0.02f) + lift, V(0, 0.6f, 0.04f) + d + lift, 0.056f, 0.05f, grey);
        // A great crest swept back from its skull
        var c = V(0, 0.68f, 0.12f) + d + lift;
        Blade(b, head, c + V(0, 0.04f, -0.01f), c + V(0, 0.26f, -0.2f), 0.12f, grey, V(1f, 0, 0), 0.3f);
        // Amber crystals set along its crest
        foreach (var (at, way) in new[] { (V(0, 0.1f, -0.04f), V(0, 1f, 0.2f)), (V(0.015f, 0.16f, -0.1f), V(0.3f, 1f, -0.2f)), (V(-0.015f, 0.2f, -0.15f), V(-0.3f, 1f, -0.4f)), (V(0, 0.24f, -0.19f), V(0, 0.8f, -0.8f)), (V(0.02f, 0.07f, 0.0f), V(0.6f, 0.6f, 0.4f)), (V(-0.02f, 0.07f, 0.0f), V(-0.6f, 0.6f, 0.4f)) })
            b.Spike(head, c + at, c + at + Vector3.Normalize(way) * 0.06f, 0.018f, amber, 0.6f, Shell, 0.004f);
        // A ridge of amber stones down its back and along its tail
        var bc = V(0, 0.48f, 0) + lift;
        var br = V(0.1f, 0.13f, 0.11f);
        foreach (var dir in new[] { V(0, 0.85f, -0.4f), V(0, 0.75f, -0.6f), V(0, 0.55f, -0.8f), V(0, 0.4f, -0.9f), V(0, 0.2f, -1f), V(0, 0.05f, -1f) })
        {
            var at = Out(bc, br, V(-20f, 0, 0), dir);
            b.Spike(Body, at - dir * 0.01f, at + Vector3.Normalize(dir + V(0, 0.6f, 0)) * 0.07f, 0.024f, amber, 0.6f, Shell, 0.004f);
        }
        int tail = BoneOf(b, "tail");
        var path = Smooth(3, V(0, 0.42f, -0.07f), V(0, 0.36f, -0.25f), V(0.02f, 0.33f, -0.44f));
        for (int i = 2; i < path.Length - 1; i += 2)
        {
            float t = i / (float)(path.Length - 1);
            float rad = 0.04f + (0.014f - 0.04f) * t;
            b.Ell(tail, path[i] + lift + V(0, rad * 0.9f, 0), V(0.016f, 0.013f, 0.02f), amber, mat: Shell, blend: 0.005f);
        }
        return Lift(b);
    }

    /// <summary>A thin band spiralling up from <paramref name="foot"/> to <paramref name="tip"/> round a horn <paramref name="r"/> wide at its foot (a narwhal's tusk, a unicorn's horn).</summary>
    private static void Helix(PokeBuilder b, int bone, Vector3 foot, Vector3 tip, float r, float turns, float thick, Color color)
    {
        var axis = Vector3.Normalize(tip - foot);
        var u = Vector3.Normalize(Vector3.Cross(axis, MathF.Abs(axis.X) < 0.9f ? V(1f, 0, 0) : V(0, 1f, 0)));
        var w = Vector3.Cross(axis, u);
        float length = Vector3.Distance(foot, tip);
        var points = Enumerable.Range(0, 25).Select(i =>
        {
            float t = i / 24f;
            float rad = r * (1f - t) * 0.95f + thick * 0.5f;
            float a = t * MathF.Tau * turns;
            return foot + axis * length * t * 0.92f + (u * MathF.Cos(a) + w * MathF.Sin(a)) * rad;
        }).ToArray();
        b.Tube(bone, points, thick, thick * 0.5f, color, Shell, 0.003f);
    }

    // ------------------------------------------------------------------ 2022 Lancerow, from Fearow

    /// <summary>
    /// Lancerow: Fearow with its long beak grown into a lance half again as long as its head, a ring of hard shell
    /// where it meets the face, its red crest swept back into three long plumes, and broader wings barred pale.
    /// </summary>
    private static PokeBuilder Lancerow()
    {
        var b = Become(Fearow(), "Lancerow", 0.97f);
        var lift = Shifted(b, "head", V(0, 0.7f, 0.15f));
        int head = BoneOf(b, "head");
        var red = Rgb(224, 74, 64);
        var crimson = Rgb(176, 30, 52);
        var bone = Rgb(226, 214, 184);
        Recolour(b, (Rgb(170, 112, 66), Rgb(164, 134, 92)), (Rgb(222, 186, 132), Rgb(240, 226, 192)), (Rgb(226, 160, 150), Rgb(156, 126, 96)));
        // The beak grows into a lance near as long as the bird, ringed where it leaves the face with a knob of shell
        var beak = b.Sdf.Shapes.First(s => s.Bone == head && s.Color.Equals(Rgb(232, 170, 164)));
        var dir = Vector3.Normalize(beak.B - beak.A);
        beak.B = beak.A + (beak.B - beak.A) * 2.3f;
        beak.Size = new Vector3(beak.Size.X * 1.25f, beak.Size.Y, beak.Size.Z);
        beak.Color = bone;
        b.Torus(head, beak.A + dir * 0.02f, 0.042f, 0.018f, Rgb(198, 186, 160), Euler(dir), mat: Shell, blend: 0.004f);
        b.Ell(head, beak.A + dir * 0.005f, V(0.05f, 0.05f, 0.035f), Rgb(208, 196, 168), Euler(dir), Shell, 0.01f);
        // The crest swept back into three long crimson plumes streaming behind the head
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.Equals(red));
        var c = V(0, 0.77f, 0.16f) + lift;
        foreach (var (x, rise, len) in new[] { (0f, 0.1f, 0.5f), (-0.03f, 0.03f, 0.44f), (0.03f, 0.05f, 0.47f) })
        {
            var root = c + V(x, 0.05f, 0.03f);
            var path = Smooth(3, root, root + V(x * 2f, 0.1f, -0.14f), root + V(x * 4f, 0.08f + rise, -len * 0.7f), root + V(x * 6f, rise, -len));
            Ribbon(b, head, path, 0.05f, crimson, V(x * 8f, 1f, 0), 0.012f);
        }
        // Wings spread wide: tan coverts, dark flight feathers barred with a band of cream
        var dark = Rgb(72, 58, 44);
        var tan = Rgb(164, 134, 92);
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            b.Sdf.RemoveAll(sh => sh.Bone == wing);
            var shoulder = V(0.12f * s, 0.48f, 0.06f) + lift;
            var wrist = V(0.34f * s, 0.64f, -0.02f) + lift;
            OpenWing(b, wing, s, shoulder, wrist, 8, 70f, -70f, 0.42f, 0.055f, dark, tan, Rgb(214, 200, 164), 0.35f);
        });
        b.Sdf.Changed();
        return Lift(b);
    }

    /// <summary>
    /// A bird's wing spread open on the bone <paramref name="wing"/> (a forebear's own wing bone, its folded wing taken
    /// away): an arm of <paramref name="coverts"/> from shoulder to wrist and <paramref name="count"/> flight feathers
    /// fanned from along it, the first <paramref name="from"/> degrees up from straight out and the last
    /// <paramref name="to"/>, each crossed by a band of <paramref name="bar"/>.
    /// </summary>
    private static void OpenWing(PokeBuilder b, int wing, float s, Vector3 shoulder, Vector3 wrist, int count, float from, float to,
        float length, float width, Color color, Color coverts, Color bar, float back = 0.3f)
    {
        b.Limb(wing, shoulder, wrist, width * 1.1f, width * 0.7f, coverts);
        var span = wrist - shoulder;
        b.Ell(wing, shoulder + span * 0.45f + V(0, -0.02f, -0.01f), V(span.Length() * 0.5f, width * 1.5f, width * 0.5f), coverts, V(0, 0, MathF.Atan2(span.Y, span.X * s) / Degree * s), blend: 0.02f);
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 1f : i / (count - 1f);
            float a = (from + (to - from) * t) * Degree;
            var root = Vector3.Lerp(Vector3.Lerp(shoulder, wrist, 0.25f), wrist, t);
            var dir = Vector3.Normalize(V(MathF.Cos(a) * s, MathF.Sin(a), -back));
            float len = length * (0.65f + 0.35f * MathF.Sin(MathF.PI * (0.2f + 0.7f * t)));
            var end = root + dir * len;
            Frond(b, wing, root, end, width, color, V(0, 0, 1f), 0.3f, Fur, 0.012f);
            if (bar != color) b.PaintEll(wing, root + (end - root) * 0.5f, V(width * 1.3f, len * 0.04f, width * 1.3f), bar, Euler(dir));
        }
    }

    // ------------------------------------------------------------------ 2028 Sandspire, from Sandslash

    /// <summary>
    /// Sandspire: Sandslash with its quills grown into fewer, taller spines of banded sandstone, tallest over its
    /// shoulders, its claws thickened into picks and a plate of rock over its brow.
    /// </summary>
    private static PokeBuilder Sandspire()
    {
        var b = Become(Sandslash(), "Sandspire", 0.9f);
        var quill = Rgb(146, 96, 58);
        var tipColor = Rgb(200, 156, 104);
        var stone = Rgb(196, 150, 100);
        var band = Rgb(132, 82, 52);
        var rock = Rgb(126, 98, 74);
        Recolour(b, (Rgb(236, 198, 106), Rgb(212, 170, 120)), (Rgb(248, 234, 196), Rgb(230, 196, 150)));
        b.Sdf.RemoveAll(s => s.Bone == Body && (s.Color.Equals(quill) || s.Color.Equals(tipColor)));
        var c = V(0, 0.36f, -0.02f);
        var r = V(0.16f, 0.2f, 0.15f);
        for (int row = 0; row < 3; row++)
        {
            int cols = row == 0 ? 3 : 4;
            for (int col = 0; col < cols; col++)
            {
                float elev = (66f - row * 24f) * Degree;
                float phi = ((col - (cols - 1) / 2f) * 34f) * Degree;
                var dir = Vector3.Normalize(V(MathF.Sin(phi) * MathF.Cos(elev), MathF.Sin(elev), -MathF.Cos(phi) * MathF.Cos(elev)));
                var root = c + dir * r * 0.8f;
                float len = (row == 0 ? 0.34f : 0.28f - row * 0.03f) * (1f - MathF.Abs(phi) * 0.25f);
                var to = root + Vector3.Normalize(dir + V(0, 0.35f, -0.25f)) * len;
                b.Spike(Body, root, to, 0.065f, stone, 0.6f, Shell, 0.012f);
                b.PaintEll(Body, Vector3.Lerp(root, to, 0.3f), V(0.07f, 0.022f, 0.07f), band, Euler(to - root));
                b.PaintEll(Body, Vector3.Lerp(root, to, 0.55f), V(0.06f, 0.02f, 0.06f), Rgb(170, 96, 64), Euler(to - root));
                b.PaintEll(Body, Vector3.Lerp(root, to, 0.78f), V(0.05f, 0.016f, 0.05f), band, Euler(to - root));
            }
        }
        // Plates of banded rock over its shoulders
        PokeBuilder.Both(s =>
        {
            var at = V(0.15f * s, 0.47f, 0.02f);
            b.Ell(Body, at, V(0.075f, 0.06f, 0.07f), stone, V(0, 0, -25f * s), Shell, 0.015f);
            b.PaintEll(Body, at + V(0.01f * s, 0.01f, 0), V(0.08f, 0.012f, 0.075f), band, V(0, 0, -25f * s));
        });
        // Claws thickened into great dark picks
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            Grow(b, V(1.5f, 1.5f, 1.5f), V(0.2f * s, 0.33f, 0.18f), sh => sh.Bone == arm && sh.Color.Equals(White), _ => false);
        });
        Recolour(b, (White, Rgb(96, 94, 90)));
        foreach (var s in b.Sdf.Shapes.Where(s => s.Color.Equals(Rgb(96, 94, 90)))) s.Material = Shell;
        // A helmet of rough rock over its brow and the crown of its head
        int head = BoneOf(b, "head");
        b.Ell(head, V(0, 0.6f, 0.11f), V(0.11f, 0.065f, 0.11f), rock, V(-15f, 0, 0), Shell, 0.015f);
        b.Ell(head, V(0, 0.585f, 0.19f), V(0.085f, 0.03f, 0.05f), rock, V(-25f, 0, 0), Shell, 0.012f);
        // A tail of banded plates
        int tail = BoneOf(b, "tail");
        foreach (var (at, size) in new[] { (V(0, 0.2f, -0.2f), 0.05f), (V(0, 0.15f, -0.26f), 0.042f), (V(0, 0.11f, -0.31f), 0.034f) })
            b.Spike(tail, at, at + V(0, 0.07f, -0.06f), size, stone, 0.5f, Shell);
        return b;
    }

    // ------------------------------------------------------------------ 2049 Somnimoth, from Venomoth

    /// <summary>
    /// Somnimoth: Venomoth with its four wings grown wider and paled to lilac, the lower pair trailing long tails like
    /// a luna moth's, an eyespot on each upper wing, its fuzzy body thicker and its feelers branched like ferns.
    /// </summary>
    private static PokeBuilder Somnimoth()
    {
        var b = Become(Venomoth(), "Somnimoth", 0.9f);
        var lift = Shifted(b, "head", V(0, 0.5f, 0.02f));
        var lilac = Rgb(234, 222, 250);
        var vein = Rgb(182, 160, 222);
        Recolour(b, (Rgb(214, 196, 240), lilac), (Rgb(160, 130, 206), vein), (Rgb(144, 112, 196), Rgb(190, 164, 212)));
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
        {
            d.White = Rgb(178, 54, 120);
            d.Pupil = Rgb(86, 20, 62);
        }
        Grow(b, V(1.25f, 1.25f, 1.25f), V(0, 0.42f, 0) + lift, bone => bone == Body);
        // A fluffy pale collar where the head meets the body, and the abdomen striped in pink
        var collar = V(0, 0.49f, 0.03f) + lift;
        b.Ell(Body, collar, V(0.085f, 0.05f, 0.075f), Rgb(244, 238, 248), blend: 0.025f);
        FurTufts(b, Body, collar, V(0.085f, 0.05f, 0.075f), 16, 0.03f, 0.016f, Rgb(244, 238, 248), 0.4f, -0.8f, 0.6f);
        foreach (float y in new[] { 0.3f, 0.35f, 0.4f })
            b.PaintTorus(Body, V(0, y, -0.02f) + lift, 0.07f, 0.012f, Rgb(232, 176, 206), V(10f, 0, 0));
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            var shoulder = V(0.04f * s, 0.46f, -0.03f) + lift;
            var k = V(1.3f, 1.18f, 1f);
            Grow(b, k, shoulder, bone => bone == wing);
            // A long tail trailing from each lower wing
            var lower = shoulder + (V(0.17f * s, 0.34f, -0.04f) + lift - shoulder) * k;
            var foot = lower + V(0.05f * s, -0.07f, 0);
            Blade(b, wing, foot, foot + V(0.07f * s, -0.24f, -0.03f), 0.05f, lilac, V(0, 0, 1f), 0.25f);
            b.PaintEll(wing, foot + V(0.065f * s, -0.22f, -0.03f), V(0.03f, 0.04f, 0.02f), vein);
            // A band of cream across each upper wing, inside its dark border
            var upper = shoulder + (V(0.26f * s, 0.6f, -0.02f) + lift - shoulder) * k;
            var way = upper - shoulder;
            b.PaintEll(wing, shoulder + way * 1.3f, V(0.12f, 0.12f, 0.08f), Rgb(120, 92, 170));
            b.PaintEll(wing, shoulder + way * 1.0f, V(0.08f, 0.08f, 0.08f), Rgb(246, 236, 196));
            // An eyespot on each lower wing, rings of purple and pink
            var spot = lower + V(0.01f * s, 0.03f, 0);
            b.Mark(wing, spot, V(0, 0, 1f), 0.04f, 0.04f, Rgb(84, 60, 140), MarkShape.Disc);
            b.Mark(wing, spot, V(0, 0, 1f), 0.03f, 0.03f, Rgb(238, 160, 196), MarkShape.Disc);
            b.Mark(wing, spot, V(0, 0, 1f), 0.016f, 0.016f, Rgb(84, 60, 140), MarkShape.Disc);
        });
        // Feelers branched like ferns
        PokeBuilder.Both(s =>
        {
            int ear = BoneOf(b, s < 0 ? "earL" : "earR");
            var from = V(0.02f * s, 0.59f, 0.02f) + lift;
            var to = V(0.06f * s, 0.72f, 0.0f) + lift;
            foreach (float t in new[] { 0.3f, 0.5f, 0.7f, 0.88f })
            {
                var at = Vector3.Lerp(from, to, t);
                foreach (float z in new[] { -1f, 1f })
                    b.Spike(ear, at, at + V(0.03f * s, 0.018f, 0.03f * z) * (1.2f - t * 0.6f), 0.007f, Rgb(144, 112, 196), blend: 0.003f);
            }
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2051 Molemount, from Dugtrio

    /// <summary>
    /// Molemount: five of Dugtrio's heads in a ring, the middle one tallest, standing out of the wide mound of earth
    /// the three never showed, boulders set in its slopes.
    /// </summary>
    private static PokeBuilder Molemount()
    {
        var b = new PokeBuilder("Molemount", 0.84f, BodyPlan.Biped, V(0, 0.17f, 0)) { Coat = Scales };
        var brown = Rgb(104, 84, 72);
        var nose = Rgb(226, 132, 140);
        var earth = Rgb(122, 90, 60);
        Burrow(b, 0.44f, 0.38f, 20, Rgb(150, 148, 150), 2051u);
        // The mound, dark earth with paler dirt on its slopes, boulders set in it
        b.Ell(Root, V(0, 0.0f, 0), V(0.46f, 0.34f, 0.4f), earth, blend: 0.03f);
        foreach (var (x, z, sz) in new[] { (0.2f, 0.16f, 0.06f), (-0.24f, 0.05f, 0.07f), (0.06f, -0.24f, 0.08f), (-0.12f, 0.22f, 0.05f), (0.27f, -0.12f, 0.06f) })
            b.PaintEll(Root, V(x * 1.2f, 0.24f, z * 1.2f), V(sz * 1.8f, 0.08f, sz * 1.8f), Rgb(146, 108, 74));
        foreach (var (x, z, k) in new[] { (0.33f, 0.1f, 0.08f), (-0.3f, -0.14f, 0.085f), (-0.12f, 0.33f, 0.07f), (0.2f, -0.3f, 0.07f), (-0.36f, 0.12f, 0.065f), (0.12f, 0.3f, 0.06f), (-0.2f, 0.24f, 0.05f), (0.36f, -0.08f, 0.055f) })
        {
            float y = 0.34f * MathF.Sqrt(MathF.Max(0f, 1f - x * x / 0.2116f - z * z / 0.16f));
            b.Box(Root, V(x, y - k * 0.2f, z), V(k * 1.0f, k * 0.8f, k * 0.85f), k * 0.3f, Rgb(140, 138, 142), V(15f, x * 200f, 20f), Shell, 0.008f);
        }
        // Five heads: the tallest in the middle, four round it, each bobbing on its own
        DiglettDome(b, Body, V(0, 0.27f, -0.05f), 1.1f, brown, nose);
        Grow(b, V(1.08f, 0.8f, 1.08f), V(0, 0.27f, -0.05f), bone => bone == Body);
        foreach (var (at, k, i) in new[] { (V(-0.12f, 0.2f, 0.15f), 0.76f, 0), (V(0.13f, 0.2f, 0.15f), 0.76f, 1), (V(-0.22f, 0.22f, -0.06f), 0.72f, 2), (V(0.22f, 0.22f, -0.05f), 0.7f, 3) })
        {
            int part = b.Part("head" + i, Root, at + V(0, 0.05f, 0), PokeRole.Head, 0.7f + 1.3f * i, at.X < 0 ? -1f : 1f);
            DiglettDome(b, part, at, k, brown, nose);
            Grow(b, V(1.08f, 0.78f, 1.08f), at, bone => bone == part);
        }
        b.CutBox(Root, V(0, -0.3f, 0), V(0.8f, 0.3f, 0.8f), Quaternion.Identity);
        return b;
    }

    // ------------------------------------------------------------------ 2053 Regalynx, from Persian

    /// <summary>
    /// Regalynx: Persian longer and leaner, its cream coat brushed with gold at the paws and the tail, a mane of soft
    /// fur at its chest, its brow gem grown into a crown of three gems in a gold setting and its curled tail ending in
    /// a tuft.
    /// </summary>
    private static PokeBuilder Regalynx()
    {
        var b = Become(Persian(), "Regalynx", 0.88f);
        var fur = Rgb(246, 232, 194);
        var gold = Rgb(234, 188, 90);
        var gem = Rgb(222, 48, 62);
        int head = BoneOf(b, "head");
        // No whiskers; ears of gold, pink inside, tall and tufted like a lynx's
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.Equals(Rgb(236, 236, 240)));
        Recolour(b, (Rgb(60, 52, 58), Rgb(228, 186, 104)), (Rgb(150, 120, 116), Rgb(240, 196, 196)));
        PokeBuilder.Both(s =>
        {
            int ear = BoneOf(b, s < 0 ? "earL" : "earR");
            Grow(b, V(0.9f, 1.3f, 1f), V(0.06f * s, 0.51f, 0.19f), bone => bone == ear);
            var tip = V(0.06f * s, 0.51f, 0.19f) + (V(0.11f * s, 0.62f, 0.17f) - V(0.06f * s, 0.51f, 0.19f)) * V(0.9f, 1.3f, 1f);
            b.Spike(ear, tip - V(0.01f * s, 0.02f, 0), tip + V(0.02f * s, 0.07f, -0.01f), 0.01f, Rgb(196, 150, 70), 0.6f);
        });
        // The crown of three gems
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.Equals(gem));
        var brow = V(0, 0.515f, 0.279f);
        b.Ell(head, brow + V(0, 0.004f, -0.004f), V(0.056f, 0.014f, 0.02f), gold, V(-40f, 0, 0), Metal, 0.004f);
        b.Ell(head, brow + V(0, 0.026f, 0.0f), V(0.02f, 0.026f, 0.013f), gem, V(-40f, 0, 0), Metal, 0.004f);
        PokeBuilder.Both(s => b.Ell(head, brow + V(0.042f * s, 0.008f, -0.01f), V(0.013f, 0.017f, 0.01f), gem, V(-40f, 0, 0), Metal, 0.004f));
        // Its legs shade into gold toward the paws
        foreach (var (z, front) in new[] { (0.14f, true), (-0.15f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = BoneOf(b, (front ? "leg" : "hind") + (s < 0 ? "L" : "R"));
                b.PaintEll(leg, V(0.066f * s, 0.07f, z + (front ? 0.03f : 0.0f)), V(0.05f, 0.1f, 0.06f), Rgb(238, 210, 140));
                b.PaintEll(leg, V(0.066f * s, 0.03f, z + (front ? 0.04f : 0.01f)), V(0.042f, 0.045f, 0.058f), gold);
            });
        // A great mane of soft pale fur round its neck and chest
        var mane = Rgb(248, 226, 230);
        var mc = V(0, 0.36f, 0.15f);
        var mr = V(0.105f, 0.11f, 0.08f);
        b.Ell(Body, mc, mr, mane, blend: 0.03f);
        FurTufts(b, Body, mc, mr, 30, 0.06f, 0.026f, mane, 0.2f, -0.9f, 0.9f);
        // The tail's curl ends in a gold tuft
        int tail = BoneOf(b, "tail");
        var dir = Vector3.Normalize(V(0.2f, 0.7f, -1f));
        var up = Vector3.Normalize(Vector3.Cross(Vector3.Cross(dir, V(0, 1f, 0)), dir));
        if (up.Y < 0f) up = -up;
        var end = V(0, 0.33f, -0.21f) + dir * 0.3f + up * 0.3f * 0.15f;
        var tuft = end + up * 0.06f * 2f;
        b.Ell(tail, tuft, V(0.055f, 0.05f, 0.055f), gold, blend: 0.012f);
        FurTufts(b, tail, tuft, V(0.055f, 0.05f, 0.055f), 18, 0.05f, 0.022f, gold, 2f, -0.9f, 0.2f);
        Stretch(b, V(0.95f, 1.04f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2055 Psyduke, from Golduck

    /// <summary>
    /// Psyduke: Golduck taller and leaner, its blue deepened, its bill longer, the gem on its brow grown into a third
    /// eye that glows, webbed fins spread from its forearms and a pale ring of light floating round its head.
    /// </summary>
    private static PokeBuilder Psyduke()
    {
        var b = Become(Golduck(), "Psyduke", 0.94f);
        var oldBlue = Rgb(92, 152, 222);
        var blue = Rgb(42, 52, 112);
        var bill = Rgb(246, 222, 140);
        var fin = Rgb(158, 214, 214);
        Recolour(b, (oldBlue, blue), (PixelCanvas.Light1(oldBlue, 0.18f), Rgb(58, 70, 140)), (bill, Rgb(226, 190, 104)));
        bill = Rgb(226, 190, 104);
        foreach (var d in b.Model.Decals.Where(d => d.IsEye)) d.Iris = Rgb(236, 214, 80);
        int head = BoneOf(b, "head");
        // A longer, narrower bill
        Grow(b, V(0.8f, 0.9f, 1.4f), V(0, 0.8f, 0.06f), s => s.Bone == head && s.Color.Equals(bill), _ => false);
        // A longer tail, ending in a pale fin
        int tail = BoneOf(b, "tail");
        b.Tube(tail, Smooth(3, V(0, 0.22f, -0.38f), V(0.04f, 0.14f, -0.56f), V(0.1f, 0.16f, -0.72f), V(0.16f, 0.24f, -0.84f)), 0.04f, 0.012f, blue, blend: 0.01f);
        Blade(b, tail, V(0.08f, 0.17f, -0.66f), V(0.12f, 0.3f, -0.74f), 0.04f, fin, V(1f, 0, 0), 0.25f);
        // The third eye, wide and glowing
        var gem = b.Sdf.Shapes.First(s => s.Bone == head && s.Color.Equals(Rgb(220, 56, 60)) && s.Material == Glow);
        gem.Size = V(0.04f, 0.03f, 0.024f);
        gem.Color = Rgb(250, 110, 130);
        b.PaintEll(head, gem.A + V(0, 0, 0.018f), V(0.016f, 0.016f, 0.012f), Rgb(255, 222, 226));
        // Webbed fins from its forearms
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            Blade(b, arm, V(0.24f * s, 0.6f, 0.08f), V(0.36f * s, 0.62f, -0.06f), 0.07f, fin, V(s, 0.3f, 0), 0.22f);
            Blade(b, arm, V(0.27f * s, 0.56f, 0.13f), V(0.38f * s, 0.5f, 0.0f), 0.05f, fin, V(s, 0.3f, 0), 0.22f);
        });
        // A ring of light floating round its head on thin strands
        var ring = V(0, 1.0f, -0.02f);
        b.Torus(head, ring, 0.17f, 0.011f, Rgb(250, 222, 240), mat: Glow, blend: 0.004f);
        PokeBuilder.Both(s => b.Tube(head, new[] { V(0.09f * s, 0.93f, -0.01f), V(0.17f * s, 1.0f, -0.02f) }, 0.005f, 0.004f, Rgb(250, 222, 240), Glow, 0.003f));
        b.Tube(head, new[] { V(0, 0.93f, -0.1f), V(0, 1.0f, -0.19f) }, 0.005f, 0.004f, Rgb(250, 222, 240), Glow, 0.003f);
        Stretch(b, V(0.82f, 1.12f, 0.86f));
        return b;
    }

    // ------------------------------------------------------------------ 2059 Kirinine, from Arcanine

    /// <summary>
    /// Kirinine: Arcanine larger, its mane flowing longer like a lion dance's, two short horns swept back from its
    /// brow and a long beard of cream fur.
    /// </summary>
    private static PokeBuilder Kirinine()
    {
        var b = Become(Arcanine(), "Kirinine", 0.98f);
        const float lift = 0.09f;
        var mane = Rgb(248, 228, 184);
        var c = V(0, 0.64f + lift, 0.25f);
        // A deeper red-orange coat
        var red = Rgb(204, 84, 46);
        Recolour(b, (Rgb(244, 150, 72), red), (Rgb(250, 214, 170), Rgb(236, 170, 130)));
        // A longer mane, flame-red with streaks of cream
        Grow(b, V(1.2f, 1.35f, 1.25f), V(0, 0.5f + lift, 0.16f), s => s.Bone == Body && s.Color.Equals(mane), _ => false);
        // The mane flame-red over the head, neck and back, cream down the chest
        var flame = Rgb(200, 72, 44);
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == Body && s.Color.Equals(mane) && s.Op == SdfOp.Add))
            s.Color = flame;
        b.Sdf.Changed();
        b.PaintEll(Body, V(0, 0.42f + lift, 0.3f), V(0.13f, 0.17f, 0.1f), mane);
        int tailBone = BoneOf(b, "tail");
        b.PaintEll(tailBone, V(0, 0.62f + lift, -0.36f), V(0.1f, 0.08f, 0.12f), flame);
        int head = BoneOf(b, "head");
        // Two horns swept back from its brow, curving up at their ends
        PokeBuilder.Both(s =>
        {
            var root = c + V(0.045f * s, 0.07f, 0.02f);
            b.Tube(head, Smooth(3, root, root + V(0.03f * s, 0.08f, -0.08f), root + V(0.05f * s, 0.17f, -0.12f)), 0.024f, 0.008f, Rgb(214, 190, 150), Shell, 0.008f);
        });
        // A long beard of cream fur
        foreach (float x in new[] { -0.025f, 0f, 0.025f })
            b.Spike(head, V(x, c.Y - 0.075f, 0.33f), V(x * 1.4f, c.Y - 0.2f, 0.34f), 0.026f, mane, 0.6f);
        // Its stripes curled into scrolls on its flanks
        PokeBuilder.Both(s =>
        {
            foreach (float z in new[] { 0.06f, -0.12f })
                b.Mark(Body, Out(V(0, 0.38f + lift, -0.02f), V(0.13f, 0.12f, 0.27f), default, V(s, 0.15f, z * 3f)), V(s, 0.15f, z * 3f), 0.03f, 0.03f, Rgb(40, 38, 48), MarkShape.Ring);
        });
        return b;
    }

    // ------------------------------------------------------------------ 2073 Medusarch, from Tentacruel

    /// <summary>
    /// Medusarch: Tentacruel with its bell raised into a tall sail-like float, blue paled to violet, its three red gems
    /// kept, and its tentacles doubled in number and length, trailing in a cluster beneath.
    /// </summary>
    private static PokeBuilder Medusarch()
    {
        var b = Become(Tentacruel(), "Medusarch", 0.98f);
        var lift = Shifted(b, "body", V(0, 0.66f, 0));
        var violet = Rgb(96, 104, 206);
        var sail = Rgb(120, 146, 226);
        var grey = Rgb(132, 120, 110);
        var navy = Rgb(52, 42, 104);
        Recolour(b, (Rgb(72, 172, 232), violet));
        var c = V(0, 0.68f, -0.02f) + lift;
        // The sail rising high from the float and sweeping back, blue paling to lilac at its ridged edge
        var crest = Smooth(3, c + V(0, 0.06f, 0.04f), c + V(0, 0.3f, 0.02f), c + V(0, 0.46f, -0.1f), c + V(0, 0.42f, -0.28f), c + V(0, 0.26f, -0.44f));
        As(b, Shell, () => Ribbon(b, Body, crest, 0.42f, sail, V(1f, 0, 0), 0.04f));
        for (int i = 2; i < crest.Length - 1; i += 2)
            b.PaintEll(Body, crest[i] + V(0, 0.06f, 0.02f), V(0.05f, 0.05f, 0.08f), Rgb(196, 176, 240), Euler(crest[i + 1] - crest[i - 1]));
        // As many tentacles again, longer, between the old ones
        for (int i = 0; i < 10; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / 10f + 0.31f;
            var out1 = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = V(0, 0.55f, -0.02f) + out1 * 0.1f + lift;
            int arm = b.Part("tentacleB" + i, Body, root, PokeRole.Leaf, i * 0.6f + 0.3f);
            float sag = 0.05f * (i % 3);
            b.Tube(arm, Smooth(3, root, root + out1 * 0.05f + V(0, -0.2f, 0), root + out1 * 0.12f + V(0, -0.38f - sag, 0), root + out1 * 0.2f + V(0, -0.52f - sag, 0)), 0.018f, 0.01f, grey);
        }
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone > Body && s.Color.Equals(grey) && s.Shape == SdfShape.RoundCone).ToList())
            if (b.Model.Skeleton[s.Bone].Name.StartsWith("tentacle") && !b.Model.Skeleton[s.Bone].Name.StartsWith("tentacleB"))
            {
                var root = b.Model.Skeleton[s.Bone].Joint;
                s.A = root + (s.A - root) * V(1f, 1.15f, 1f);
                s.B = root + (s.B - root) * V(1f, 1.15f, 1f);
            }
        // The tentacles darkened to navy, every third one violet
        foreach (var s in b.Sdf.Shapes.Where(s => s.Color.Equals(grey)))
            s.Color = b.Model.Skeleton[s.Bone].Name.EndsWith("3") || b.Model.Skeleton[s.Bone].Name.EndsWith("6") || b.Model.Skeleton[s.Bone].Name.EndsWith("9") ? Rgb(142, 70, 186) : navy;
        b.Sdf.Changed();
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2080 Slowmantle, from Slowbro

    /// <summary>
    /// Slowmantle: Slowbro sat back in the Shellder of its tail grown into a great spiral shell over its back like a
    /// hermit crab's house, grey lined in pink, a pearl gleaming in the shell's mouth.
    /// </summary>
    private static PokeBuilder Slowmantle()
    {
        var b = Become(Slowbro(), "Slowmantle", 0.94f);
        var pink = Rgb(240, 160, 184);
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail && !s.Color.Equals(pink));
        b.Model.Decals.RemoveAll(d => d.Bone == tail);
        var grey = Rgb(160, 160, 166);
        var line = Rgb(232, 140, 136);
        Recolour(b, (pink, Rgb(230, 130, 116)));
        var sc = V(0, 0.5f, -0.2f);
        var sr = V(0.27f, 0.32f, 0.3f);
        Ammonite(b, Body, sc, sr, grey, line, 20f, -10f, -40f, -70f, -100f, -130f, -160f);
        // Broad bands of pink over its back, following its whorl
        foreach (float deg in new[] { 35f, 5f, -25f, -55f, -85f, -115f, -145f })
        {
            float a = deg * Degree;
            var at = Out(sc, sr, default, V(0, MathF.Cos(a), MathF.Sin(a)));
            b.PaintEll(Body, at, V(sr.X * 0.95f, 0.016f, sr.Y * 0.6f), Rgb(224, 150, 150), V(deg + 90f, 0, 0), 0.008f);
        }
        // The shell's mantle reaching forward over its shoulders and the back of its head
        b.Ell(Body, sc + V(0, 0.16f, 0.08f), V(0.25f, 0.12f, 0.16f), grey, V(-20f, 0, 0), Shell, 0.03f);
        b.PaintEll(Body, sc + V(0, 0.22f, 0.12f), V(0.27f, 0.03f, 0.18f), line, V(-20f, 0, 0));
        // The pearl in the shell's dark mouth over its tail
        var mouth = Out(sc, sr, default, V(0, -0.7f, -0.7f));
        b.Ell(Body, mouth + V(0, 0.02f, -0.01f), V(0.1f, 0.08f, 0.04f), Rgb(70, 62, 70), V(-45f, 0, 0), Shell, 0.01f);
        b.Ell(Body, mouth + V(0, 0.03f, 0.0f), V(0.06f, 0.06f, 0.06f), Rgb(246, 246, 252), mat: Shell, blend: 0.006f);
        return b;
    }

    // ------------------------------------------------------------------ 2085 Tripluma, from Dodrio

    /// <summary>
    /// Tripluma: Dodrio with a crest of plumes on each head in its own colour (red, gold and white), a fan of tail
    /// feathers raised like a peacock's, and thicker legs; the female's beige necks kept.
    /// </summary>
    /// <param name="female">A female keeps Dodrio's: beige necks.</param>
    private static PokeBuilder Tripluma(bool female = false)
    {
        var b = Become(Dodrio(female), "Tripluma", 0.97f);
        var plume = Rgb(232, 132, 128);
        var fur = Rgb(192, 136, 96);
        var brown = Rgb(166, 118, 62);
        var neck = female ? Rgb(226, 204, 168) : Rgb(204, 176, 136);
        var oldNeck = female ? Rgb(214, 186, 146) : Rgb(44, 40, 44);
        var beakColor = Rgb(200, 162, 112);
        // Brown feathers, its necks and heads one pale tan, short hooked beaks
        var heads = new[] { BoneOf(b, "headL"), BoneOf(b, "headM"), BoneOf(b, "headR") };
        foreach (var s in b.Sdf.Shapes.ToList())
        {
            if (heads.Contains(s.Bone) && (s.Color.Equals(fur) || s.Color.Equals(oldNeck))) s.Color = neck;
            else if (s.Color.Equals(oldNeck)) s.Color = neck;
            if (heads.Contains(s.Bone) && s.Color.Equals(Rgb(232, 212, 180)) && s.Shape == SdfShape.RoundCone)
            {
                s.B = s.A + (s.B - s.A) * 0.62f;
                s.Size = new Vector3(s.Size.X * 1.35f, s.Size.Y, s.Size.Z);
                var d = Vector3.Normalize(s.B - s.A);
                b.Spike(s.Bone, s.B - d * 0.012f, s.B + V(0, -0.03f, 0) + d * 0.004f, s.Size.X * 0.45f, beakColor, mat: Shell, blend: 0.004f);
            }
        }
        Recolour(b, (Rgb(232, 212, 180), beakColor), (fur, brown), (Rgb(58, 50, 52), Rgb(128, 90, 50)), (Rgb(224, 198, 160), Rgb(112, 98, 74)));
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        // A fan of tail feathers raised high like a peacock's, red, gold and white in turn, each with an eye at its end
        var fan = new[] { Rgb(206, 52, 56), Rgb(226, 186, 72), Rgb(240, 232, 214) };
        var root = V(0, 0.47f, -0.12f);
        for (int i = 0; i < 11; i++)
        {
            float a = (-70f + 140f * i / 10f) * Degree;
            var dir = Vector3.Normalize(V(MathF.Sin(a) * 1.1f, 1.1f * MathF.Cos(a) + 0.2f, -1.0f));
            var tip = root + dir * 0.4f;
            Frond(b, tail, root, tip, 0.055f, fan[i % 3], V(0, 0.4f, -1f), 0.3f, Fur, 0.01f);
            b.PaintEll(tail, root + dir * 0.34f, V(0.035f, 0.045f, 0.035f), fan[(i + 1) % 3], Euler(dir));
        }
        // A great crest of plumes on each head, each in its own colour
        foreach (var (name, at, color) in new[] { ("headL", V(-0.17f, 0.79f, 0.04f), Rgb(184, 30, 50)), ("headM", V(0.0f, 0.9f, -0.02f), Rgb(222, 184, 64)), ("headR", V(0.16f, 0.82f, 0.05f), Rgb(240, 240, 238)) })
        {
            int head = BoneOf(b, name);
            foreach (float x in new[] { -1.5f, -0.75f, 0f, 0.75f, 1.5f })
                b.Spike(head, at + V(0.015f * x, 0.05f, -0.01f), at + V(0.04f * x, 0.24f - MathF.Abs(x) * 0.04f, -0.13f + MathF.Abs(x) * 0.02f), 0.018f, color, 0.5f);
            b.Spike(head, at + V(0, 0.03f, -0.03f), at + V(0, 0.05f, -0.2f), 0.02f, color, 0.5f);
        }
        // Thick, scaled legs
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            Grow(b, V(1.7f, 1f, 1.7f), V(0.055f * s, 0.2f, 0.01f), sh => sh.Bone == leg && sh.Shape == SdfShape.RoundCone && sh.Size.X > 0.012f, _ => false);
        });
        return b;
    }

    // ------------------------------------------------------------------ 2087 Glacitusk, from Dewgong

    /// <summary>
    /// Glacitusk: Dewgong bigger and heavier, its flippers lightly blue, its horn grown into a long spiralled tusk of
    /// ivory, two short tusks below its mouth and a mantle of thick fur at its neck.
    /// </summary>
    private static PokeBuilder Glacitusk()
    {
        var b = Become(Dewgong(), "Glacitusk", 0.96f);
        var lift = Shifted(b, "head", V(0, 0.44f, 0.1f));
        var white = Rgb(238, 242, 250);
        var ivory = Rgb(244, 234, 206);
        int head = BoneOf(b, "head");
        Recolour(b, (Rgb(226, 234, 246), Rgb(84, 142, 160)));
        // The horn grown into a long spiral tusk
        b.Sdf.RemoveAll(s => s.Bone == head && s.Shape == SdfShape.RoundCone && s.Color.Equals(white));
        var foot = V(0, 0.6f, 0.17f) + lift;
        var tip = V(0, 0.8f, 0.42f) + lift;
        b.Spike(head, foot, tip, 0.032f, ivory, mat: Shell);
        Helix(b, head, foot, tip, 0.032f, 3f, 0.007f, Rgb(214, 198, 160));
        // Its small tusks grown longer
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == head && s.Shape == SdfShape.RoundCone && s.Color.Equals(White)).ToList())
        {
            s.B = s.A + (s.B - s.A) * 1.9f;
            s.Size = new Vector3(s.Size.X * 1.3f, s.Size.Y, s.Size.Z);
            s.Color = ivory;
        }
        // A mantle of thick fur round its neck
        var mc = V(0, 0.43f, 0.11f) + lift;
        var mr = V(0.15f, 0.08f, 0.13f);
        b.Ell(Body, mc, mr, white, blend: 0.03f);
        FurTufts(b, Body, mc, mr, 28, 0.045f, 0.024f, white, 0.5f, -0.7f, 0.7f);
        // Shards of ice caught in the mantle
        foreach (var (d, size) in new[] { (V(0.8f, 0.5f, -0.2f), 0.035f), (V(-0.7f, 0.6f, -0.3f), 0.03f), (V(0.4f, 0.7f, -0.6f), 0.028f), (V(-0.9f, 0.1f, 0.2f), 0.026f) })
            b.Box(Body, Out(mc, mr, default, d), V(size, size * 0.7f, size * 0.9f), size * 0.15f, Rgb(206, 236, 250), V(20f, d.X * 40f, 30f), Shell, 0.004f);
        b.Sdf.Changed();
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2089 Sludgemire, from Muk

    /// <summary>
    /// Sludgemire: Muk thickened into tar, black-purple with a gloss and bubbles on its surface, bones and rocks half
    /// sunk in it, its arms thicker and dripping.
    /// </summary>
    private static PokeBuilder Sludgemire()
    {
        var b = Become(Muk(), "Sludgemire", 0.92f);
        const float k = 1.35f;
        var tar = Rgb(46, 38, 58);
        Recolour(b, (Rgb(162, 122, 182), tar), (Rgb(116, 84, 136), Rgb(30, 24, 40)), (Rgb(84, 92, 120), Rgb(24, 18, 28)));
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
        {
            d.White = Rgb(250, 206, 64);
            d.Pupil = Rgb(70, 36, 10);
        }
        var hc = V(0, 0.26f, 0.02f);
        var hr = V(0.26f, 0.21f, 0.21f);
        // Bubbles and blisters on its surface, thickest over its shoulders
        foreach (var (d, size) in new[] { (V(0.6f, 0.6f, -0.3f), 0.03f), (V(-0.5f, 0.8f, -0.2f), 0.024f), (V(0.2f, 0.9f, -0.5f), 0.02f), (V(-0.8f, 0.2f, -0.4f), 0.032f), (V(0.8f, 0.1f, -0.5f), 0.022f),
            (V(-0.3f, 0.9f, -0.3f), 0.026f), (V(0.4f, 0.85f, -0.1f), 0.022f), (V(0f, 0.8f, -0.6f), 0.028f), (V(-0.6f, 0.6f, -0.5f), 0.024f) })
        {
            var at = Out(hc, hr, V(-8f, 0, 0), d);
            b.Ell(Body, at, V(size, size, size), Rgb(70, 58, 92), mat: Scales, blend: 0.006f);
        }
        // Fangs of old bone round its gaping mouth
        var mouth = Out(hc, hr, V(-8f, 0, 0), V(0, -0.05f, 1f));
        for (int i = 0; i < 6; i++)
        {
            float a = -1.1f + i * 0.44f;
            var at = mouth + V(MathF.Sin(a) * 0.07f, (i % 2 == 0 ? 0.1f : -0.1f), -0.02f) * 1.35f;
            b.Spike(Body, at, at + V(0, i % 2 == 0 ? -0.06f : 0.06f, 0.05f), 0.016f, Rgb(214, 196, 150), mat: Shell, blend: 0.008f);
        }
        // A bone and rocks half sunk in the puddle round it
        Club(b, Body, V(0.2f, 0.13f, 0.17f), V(0.33f, 0.1f, 0.03f), 0.018f, Rgb(232, 226, 210));
        foreach (var (at, size) in new[] { (V(-0.28f, 0.1f, 0.1f), 0.05f), (V(-0.15f, 0.12f, -0.22f), 0.045f), (V(0.28f, 0.1f, -0.12f), 0.04f) })
            b.Ell(Body, at, V(size * 1.2f, size * 0.8f, size), Rgb(120, 116, 122), V(0, at.X * 300f, 12f), Shell, 0.008f);
        // Thick arms hanging from heavy shoulders down to the ground, dripping, their hands clawed
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            b.Sdf.RemoveAll(sh => sh.Bone == arm);
            var shoulder = V(0.2f * s, 0.25f, 0.06f) * k;
            var elbow = V(0.3f * s, 0.16f, 0.16f) * k;
            var hand = V(0.33f * s, 0.06f, 0.22f) * k;
            b.Ell(arm, shoulder, V(0.08f, 0.08f, 0.08f) * k, tar, blend: 0.03f);
            b.Tube(arm, Smooth(3, shoulder, elbow, hand), 0.06f * k, 0.045f * k, tar, blend: 0.03f);
            b.Ell(arm, hand, V(0.07f, 0.04f, 0.07f) * k, tar, blend: 0.02f);
            for (int i = -1; i <= 1; i++)
                b.Spike(arm, hand + V(0.035f * i, -0.01f, 0.05f) * k, hand + V(0.05f * i, -0.04f, 0.1f) * k, 0.016f * k, tar, 0.7f, blend: 0.01f);
            foreach (float t in new[] { 0.3f, 0.55f })
            {
                var at = Vector3.Lerp(Vector3.Lerp(shoulder, elbow, t * 2f), Vector3.Lerp(elbow, hand, t * 2f - 1f), t > 0.5f ? 1f : 0f) - V(0, 0.035f, 0) * k;
                b.Spike(arm, at + V(0, 0.02f, 0) * k, at - V(0, 0.05f, 0) * k, 0.018f * k, tar, blend: 0.012f);
            }
        });
        Stretch(b, V(1f, 1.25f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2091 Clamspire, from Cloyster

    /// <summary>
    /// Clamspire: Cloyster's spiked shell grown into a spire of shells grown together like a reef, smaller shells on
    /// its slopes, pale violet with white spikes; its pearl face kept, and a second, smaller face in a side shell.
    /// </summary>
    private static PokeBuilder Clamspire()
    {
        var b = Become(Cloyster(), "Clamspire", 0.96f);
        var lift = Shifted(b, "body", V(0, 0.42f, 0));
        var violet = Rgb(188, 170, 222);
        var spike = Rgb(238, 236, 248);
        var pearl = Rgb(44, 40, 74);
        Recolour(b, (Rgb(142, 132, 180), violet));
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == Body && s.Shape == SdfShape.RoundCone && s.Color.Equals(violet)))
            s.Color = spike;
        var c = V(0, 0.42f, -0.04f) + lift;
        // The spire of shells grown together over it
        foreach (var (at, r, n) in new[] { (V(0, 0.33f, -0.1f), V(0.19f, 0.14f, 0.16f), 5), (V(0, 0.5f, -0.14f), V(0.12f, 0.11f, 0.11f), 4), (V(0, 0.64f, -0.16f), V(0.07f, 0.07f, 0.07f), 3) })
        {
            b.Ell(Body, c + at, r, violet, mat: Shell, blend: 0.03f);
            for (int i = 0; i < n; i++)
            {
                float a = i * MathF.Tau / n + 0.4f;
                var d = Vector3.Normalize(V(MathF.Sin(a), 0.5f, MathF.Cos(a) * 0.8f - 0.3f));
                var root = Out(c + at, r, default, d);
                b.Spike(Body, root - d * 0.02f, root + d * (0.05f + r.X * 0.4f), 0.03f, spike, mat: Shell);
            }
        }
        b.Spike(Body, c + V(0, 0.68f, -0.16f), c + V(0, 0.82f, -0.2f), 0.04f, spike, mat: Shell);
        // A broad foot of shells grown together round its base, the reef it stands on
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f + 0.2f;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            var at = c + V(0, -0.24f, -0.06f) + d * 0.25f;
            var tone = i % 2 == 0 ? PixelCanvas.Light1(violet, 0.12f) : Rgb(176, 184, 226);
            b.Ell(Body, at, V(0.14f, 0.1f, 0.12f), tone, V(0, a / Degree, 20f), Shell, 0.03f);
            b.Spike(Body, at + d * 0.08f + V(0, 0.03f, 0), at + d * 0.18f + V(0, 0.08f, 0), 0.026f, spike, mat: Shell);
        }
        // Small shells on its slopes, one of them open on a second, smaller face
        foreach (var (at, size) in new[] { (V(-0.27f, 0.12f, 0.02f), 0.07f), (V(0.2f, -0.2f, 0.1f), 0.06f), (V(-0.18f, 0.28f, -0.12f), 0.055f) })
            b.Ell(Body, c + at, V(size * 1.2f, size, size), PixelCanvas.Light1(violet, 0.12f), V(0, 0, 20f), Shell, 0.015f);
        var side = c + V(0.28f, 0.12f, 0.06f);
        b.Ell(Body, side, V(0.09f, 0.085f, 0.08f), PixelCanvas.Light1(violet, 0.12f), mat: Shell, blend: 0.02f);
        var face = side + V(0.01f, 0, 0.05f);
        var faceR = V(0.065f, 0.06f, 0.05f);
        b.Ell(Body, face, faceR, pearl, mat: Shell, blend: 0.006f);
        PokeBuilder.Both(s => b.Eye(Body, On(face, faceR, face.X + 0.024f * s, face.Y + 0.008f), V(0.3f * s, 0.05f, 1f), 0.018f, sclera: true, pupil: Black));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2097 Mesmeryx, from Hypno

    /// <summary>
    /// Mesmeryx: Hypno taller and thinner, its yellow paled, its collar grown into a high ruff (the female's fuller
    /// still), and its pendulum become a ring of three coins circling on a thin chain round its hand.
    /// </summary>
    /// <param name="female">A female keeps Hypno's: a fuller ruff.</param>
    private static PokeBuilder Mesmeryx(bool female = false)
    {
        var b = Become(Hypno(female), "Mesmeryx", 0.96f);
        var pale = Rgb(240, 222, 150);
        var ruff = Rgb(246, 246, 240);
        Recolour(b, (Rgb(250, 214, 82), pale));
        // A great ruff of pale lilac-white fluff round the back of its head, a cloud of it rising behind
        var rc = V(0, 0.64f, 0);
        float f = female ? 1.2f : 1f;
        var cloud = Rgb(240, 236, 248);
        b.Ell(Body, rc + V(0, 0.12f, -0.07f), V(0.17f, 0.17f, 0.09f) * f, cloud, blend: 0.03f);
        FurTufts(b, Body, rc + V(0, 0.12f, -0.07f), V(0.17f, 0.17f, 0.09f) * f, 30, 0.05f, 0.026f, cloud, 0.1f, -0.9f, 0.4f);
        for (int i = 0; i < 9; i++)
        {
            float a = (-120f + i * 30f) * Degree;
            var foot = rc + V(MathF.Sin(a) * 0.13f * f, 0.02f, -MathF.Cos(a) * 0.1f * f);
            var outward = Vector3.Normalize(V(MathF.Sin(a), 0, -MathF.Cos(a)));
            Blade(b, Body, foot, foot + outward * 0.08f * f + V(0, 0.26f * f - MathF.Abs(MathF.Sin(a)) * 0.05f, 0), 0.06f * f, ruff, outward, 0.24f);
        }
        // Forearms and hands gone dark, as if gloved in shadow, and a dark streak down its flanks
        var shadow = Rgb(46, 42, 74);
        PokeBuilder.Both(s =>
        {
            int a = BoneOf(b, s < 0 ? "armL" : "armR");
            var hand = s > 0 ? V(0.27f, 0.74f, 0.06f) : V(-0.3f, 0.66f, 0.08f);
            var elbow = V(0.23f * s, 0.6f, 0.02f);
            b.PaintEll(a, (elbow + hand) / 2f + V(0.02f * s, 0, 0), V(0.06f, 0.09f, 0.06f), shadow, Euler(hand - elbow));
            b.PaintEll(a, hand, V(0.05f, 0.05f, 0.05f), shadow);
            b.PaintEll(Body, V(0.1f * s, 0.46f, -0.02f), V(0.04f, 0.13f, 0.08f), shadow);
        });
        // Its ears grown tall, tipped in shadow
        PokeBuilder.Both(s =>
        {
            int ear = BoneOf(b, s < 0 ? "earL" : "earR");
            Grow(b, V(1f, 1.35f, 1f), V(0.06f * s, 0.85f, 0), bone => bone == ear);
            b.PaintEll(ear, V(0.115f * s, 1.03f, -0.025f), V(0.04f, 0.05f, 0.04f), shadow);
        });
        // A long thin tail ending in a dark tuft
        int tail = b.Tail(V(0, 0.4f, -0.08f));
        var tailPath = Smooth(3, V(0, 0.4f, -0.08f), V(0.05f, 0.22f, -0.2f), V(0.12f, 0.1f, -0.3f), V(0.2f, 0.18f, -0.4f), V(0.22f, 0.34f, -0.44f));
        b.Tube(tail, tailPath, 0.022f, 0.014f, pale, blend: 0.01f);
        FurTufts(b, tail, tailPath[^1] + V(0, 0.03f, 0), V(0.03f, 0.04f, 0.03f), 10, 0.04f, 0.016f, shadow, 2f, -0.6f, 0.2f);
        b.Ell(tail, tailPath[^1] + V(0, 0.03f, 0), V(0.03f, 0.04f, 0.03f), shadow, blend: 0.01f);
        // The ring of three dark coins on a thin chain round its hand
        int arm = BoneOf(b, "armR");
        b.Sdf.RemoveAll(s => s.Bone == arm && (s.Color.Equals(Rgb(200, 200, 210)) || s.Color.Equals(Rgb(200, 204, 216))));
        var grip = V(0.27f, 0.74f, 0.06f);
        var ring = grip - V(0, 0.13f, 0);
        var chain = Rgb(96, 90, 112);
        b.Tube(arm, new[] { grip - V(0, 0.03f, -0.01f), ring + V(0, 0.005f, 0.08f) }, 0.008f, 0.008f, chain, Metal, 0f);
        b.Torus(arm, ring, 0.08f, 0.008f, chain, mat: Metal, blend: 0f);
        for (int i = 0; i < 3; i++)
        {
            float a = (90f + i * 120f) * Degree;
            var at = ring + V(MathF.Cos(a) * 0.08f, 0, MathF.Sin(a) * 0.08f);
            b.Ell(arm, at, V(0.032f, 0.032f, 0.009f), Rgb(84, 84, 96), V(0, -a / Degree + 90f, 0), Metal, 0.004f);
        }
        Stretch(b, V(0.84f, 1.12f, 0.88f));
        return b;
    }

    /// <summary>Gives the shapes <paramref name="add"/> lays down the material <paramref name="mat"/> (a ribbon of light, made by a helper that lays fur).</summary>
    private static void As(PokeBuilder b, SurfaceMaterial mat, Action add)
    {
        int before = b.Sdf.Shapes.Count;
        add();
        for (int i = before; i < b.Sdf.Shapes.Count; i++) b.Sdf.Shapes[i].Material = mat;
    }

    // ------------------------------------------------------------------ 2101 Dynamorb, from Electrode

    /// <summary>
    /// Dynamorb: Electrode larger, its red and white halves divided by a band of steel set with three studs, two arcs of
    /// current circling it on thin strands.
    /// </summary>
    private static PokeBuilder Dynamorb()
    {
        var b = Become(Electrode(), "Dynamorb", 0.78f);
        var c = V(0, 0.3f, 0);
        // Its halves turned about: red above, where the glowing eyes are, white below; no grin
        var red = Rgb(196, 62, 70);
        Recolour(b, (Rgb(242, 242, 246), red), (Rgb(226, 66, 74), Rgb(236, 236, 240)));
        b.Sdf.RemoveAll(s => s.Color.Equals(Rgb(150, 40, 56)) || s.Op == SdfOp.Cut);
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
        {
            d.White = Rgb(226, 250, 120);
            d.Pupil = Rgb(246, 255, 200);
        }
        // A heavy band of dark steel round its middle, bolted, and vents in the red above it
        var band = Rgb(104, 106, 114);
        b.Torus(Body, c, 0.3f, 0.034f, band, mat: Metal, blend: 0.008f);
        for (int i = 0; i < 8; i++)
        {
            float r = (i * 45f + 22.5f) * Degree;
            b.Ell(Body, c + V(MathF.Sin(r) * 0.335f, 0, MathF.Cos(r) * 0.335f), V(0.024f, 0.024f, 0.024f), Rgb(140, 142, 150), mat: Metal, blend: 0.004f);
        }
        foreach (float a in new[] { -40f, 40f, 140f, -140f })
        {
            var n = V(MathF.Sin(a * Degree), 0.45f, MathF.Cos(a * Degree));
            b.Mark(Body, Out(c, V(0.3f, 0.3f, 0.3f), default, n), n, 0.035f, 0.016f, Rgb(70, 66, 74), MarkShape.Bar);
        }
        // Two rings of grey wire circling it, held off the ball by two strands, sparks of current along them
        var wire = Rgb(150, 154, 164);
        var current = Rgb(250, 242, 150);
        foreach (var turn in new[] { V(28f, 0, 24f), V(-28f, 0, -24f) })
        {
            var q = Quaternion.CreateFromYawPitchRoll(turn.Y * Degree, turn.X * Degree, turn.Z * Degree);
            b.Torus(Body, c, 0.42f, 0.011f, wire, turn, mat: Metal, blend: 0f);
            foreach (var local in new[] { V(0.42f, 0, 0), V(-0.42f, 0, 0) })
            {
                var p = c + Vector3.Transform(local, q);
                b.Tube(Body, new[] { c + (p - c) * 0.7f, p }, 0.007f, 0.007f, wire, Metal, 0f);
            }
            foreach (float a in new[] { 0.5f, 2.2f, 3.9f, 5.4f })
                b.Ell(Body, c + Vector3.Transform(V(MathF.Cos(a) * 0.42f, 0, MathF.Sin(a) * 0.42f), q), V(0.022f, 0.022f, 0.022f), current, mat: Glow, blend: 0.004f);
        }
        return b;
    }

    // ------------------------------------------------------------------ 2103 Palmagus, from Exeggutor

    /// <summary>
    /// Palmagus: Exeggutor grown into a grove: two trunks rise from one root, each with its crown of leaves and three
    /// coconut heads, and a fourth head grows low at the join.
    /// </summary>
    private static PokeBuilder Palmagus()
    {
        var b = new PokeBuilder("Palmagus", 0.97f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Leaf };
        var bark = Rgb(124, 116, 84);
        var ring = PixelCanvas.Mix(bark, Rgb(60, 70, 40), 0.35f);
        var shell = Rgb(150, 108, 70);
        var leaf = Rgb(46, 126, 104);
        var cream = Rgb(150, 140, 104);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.2f, 0));
            b.Limb(leg, V(0.1f * s, 0.21f, 0), V(0.12f * s, 0.06f, 0.03f), 0.075f, 0.065f, bark);
            b.Ell(leg, V(0.125f * s, 0.035f, 0.06f), V(0.075f, 0.035f, 0.085f), cream);
            for (int i = -1; i <= 1; i++)
                b.Spike(leg, V(0.125f * s + 0.035f * i, 0.025f, 0.125f), V(0.125f * s + 0.045f * i, 0.016f, 0.16f), 0.014f, White, mat: Shell, blend: 0.004f);
        });
        b.Ell(Body, V(0, 0.32f, 0), V(0.18f, 0.18f, 0.15f), bark);
        foreach (float y in new[] { 0.22f, 0.29f, 0.36f })
            b.PaintTorus(Body, V(0, y, 0), 0.18f * MathF.Sqrt(MathF.Max(0.05f, 1f - MathF.Pow((y - 0.32f) / 0.18f, 2))), 0.016f, ring, sz: 0.83f);
        int tail = b.Tail(V(0, 0.2f, -0.11f));
        b.Limb(tail, V(0, 0.2f, -0.1f), V(0, 0.08f, -0.26f), 0.055f, 0.03f, bark);
        // The fourth head, low at the join
        Coconut(b, Body, V(0, 0.42f, 0.12f), 0.085f, shell, V(0, 0, 1f), true, false, false);
        // Two trunks, each with its crown
        PokeBuilder.Both(s =>
        {
            int trunk = b.Part(s < 0 ? "trunkL" : "trunkR", Body, V(0.06f * s, 0.42f, -0.02f), PokeRole.Segment, s < 0 ? 0.2f : 1.4f, s);
            var spine = Smooth(3, V(0.05f * s, 0.4f, -0.02f), V(0.1f * s, 0.58f, -0.03f), V(0.16f * s, 0.74f, -0.02f), V(0.2f * s, 0.86f, -0.01f));
            b.Tube(trunk, spine, 0.065f, 0.05f, bark, blend: 0f);
            for (int i = 2; i < spine.Length - 1; i += 3)
                b.PaintTorus(trunk, spine[i], 0.062f - 0.002f * i, 0.012f, ring, Euler(spine[i + 1] - spine[i - 1]));
            var top = V(0.2f * s, 0.88f, -0.01f);
            int crown = b.Part(s < 0 ? "crownL" : "crownR", trunk, top, PokeRole.Head, s < 0 ? 0.6f : 2.2f, s);
            b.Ell(crown, top, V(0.09f, 0.07f, 0.08f), bark, blend: 0.04f);
            Coconut(b, crown, top + V(-0.08f, 0.05f, 0.05f), 0.085f, shell, V(-0.4f + 0.5f * s, 0, 1f), s < 0, s > 0, false);
            Coconut(b, crown, top + V(0.08f, 0.06f, 0.04f), 0.085f, shell, V(0.4f + 0.5f * s, 0, 1f), false, false, false);
            Coconut(b, crown, top + V(0.0f, 0.07f, -0.11f), 0.078f, shell, V(0.85f * s, 0.15f, 0.3f), false, false, s > 0);
            var root0 = top + V(0, 0.15f, -0.03f);
            int leaves = b.Part(s < 0 ? "leavesL" : "leavesR", crown, root0, PokeRole.Leaf, s);
            b.Ell(leaves, root0, V(0.065f, 0.055f, 0.065f), leaf);
            for (int i = 0; i < 9; i++)
            {
                float a = i * MathF.Tau / 9f + 0.2f;
                var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
                float front = MathF.Max(0f, dir.Z);
                float reach = 0.4f - 0.15f * front + 0.03f * (i % 3), rise = 0.3f + 0.1f * front;
                var root = root0 + dir * 0.03f;
                LongLeaf(b, leaves, root, root + dir * reach * 0.35f + V(0, rise * 0.8f, 0), root + dir * reach * 1.1f + V(0, rise * (0.1f - 0.15f * (i % 2)), 0), 0.05f, i % 3 == 1 ? Rgb(112, 82, 136) : leaf, 0.012f);
            }
        });
        // Its heads' eyes glow violet
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
        {
            d.White = Rgb(214, 150, 250);
            d.Pupil = Rgb(244, 220, 255);
            if (d.Iris is { }) d.Iris = Rgb(214, 150, 250);
        }
        // The low head wears a long beard of fibre
        foreach (float x in new[] { -0.035f, 0f, 0.035f })
            b.Spike(Body, V(x, 0.38f, 0.18f), V(x * 1.3f, 0.24f, 0.2f), 0.03f, Rgb(122, 92, 62), 0.6f, blend: 0.012f);
        // Twisting moss on its trunks, and roots spreading from its feet
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            foreach (var (dx, dz) in new[] { (0.1f, 0.06f), (0.04f, -0.08f), (-0.02f, 0.1f) })
                b.Tube(leg, Smooth(3, V(0.12f * s, 0.1f, 0.03f), V((0.12f + dx * 0.6f) * s, 0.04f, 0.03f + dz * 0.6f), V((0.12f + dx * 1.3f) * s, 0.012f, 0.03f + dz * 1.3f)), 0.034f, 0.014f, bark, blend: 0.01f);
            int trunk = BoneOf(b, s < 0 ? "trunkL" : "trunkR");
            b.PaintEll(trunk, V(0.11f * s, 0.6f, 0.04f), V(0.05f, 0.06f, 0.05f), Rgb(96, 132, 62));
        });
        return b;
    }

    // ------------------------------------------------------------------ 2106 Savateur, from Hitmonlee

    /// <summary>Savateur: Hitmonlee taller still, its legs longer and springier, a short cape of brown hide at its shoulders and wraps round its ankles.</summary>
    private static PokeBuilder Savateur()
    {
        var b = Become(Hitmonlee(), "Savateur", 0.95f);
        var hide = Rgb(124, 86, 58);
        var wrap = Rgb(240, 228, 196);
        var gold = Rgb(206, 156, 86);
        // A golden hide, its springs of the same skin banded darker
        Recolour(b, (Rgb(182, 148, 120), gold), (Rgb(236, 220, 170), Rgb(178, 124, 66)));
        // A lean torso narrowing to the waist
        Grow(b, V(0.78f, 1f, 0.8f), V(0, 0.64f, 0), s => s.Bone == Body, bone => bone == Body);
        // Arms hanging long at its sides, clawed
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            b.Sdf.RemoveAll(sh => sh.Bone == arm);
            var shoulder = V(0.11f * s, 0.62f, 0);
            var elbow = V(0.17f * s, 0.46f, 0.02f);
            var hand = V(0.2f * s, 0.32f, 0.05f);
            b.Limb(arm, shoulder, elbow, 0.034f, 0.028f, gold);
            b.Limb(arm, elbow, hand, 0.028f, 0.024f, gold);
            b.Ell(arm, hand, V(0.03f, 0.035f, 0.028f), gold);
            Claws(b, arm, hand + V(0, -0.025f, 0.01f), V(0.012f, 0, 0), V(0.1f * s, -1f, 0.3f), 0.04f, 0.008f);
        });
        // A crest of long golden plumes swept up and back from its head
        foreach (var (x, h, back) in new[] { (0f, 0.22f, 0.1f), (-0.04f, 0.17f, 0.12f), (0.04f, 0.17f, 0.12f), (-0.07f, 0.1f, 0.1f), (0.07f, 0.1f, 0.1f) })
            Blade(b, Body, V(x * 0.6f, 0.8f, 0.01f), V(x * 1.4f, 0.8f + h, -back), 0.045f, PixelCanvas.Light1(gold, 0.1f), V(0, 0.2f, 1f), 0.3f);
        // The cape of brown feathers over its shoulders, falling down its back and sides
        b.Ell(Body, V(0, 0.72f, -0.02f), V(0.16f, 0.05f, 0.11f), hide, V(10f, 0, 0), blend: 0.02f);
        for (int i = 0; i < 7; i++)
        {
            float a = (-80f + 160f * i / 6f) * Degree;
            var root = V(MathF.Sin(a) * 0.13f, 0.72f, -MathF.Cos(a) * 0.09f);
            var outward = Vector3.Normalize(V(MathF.Sin(a), 0, -MathF.Cos(a)));
            Blade(b, Body, root, root + outward * 0.06f + V(0, -0.26f + MathF.Abs(MathF.Sin(a)) * 0.06f, 0), 0.07f, hide, outward, 0.22f);
        }
        // Thick wraps of pale cloth round its ankles, fur at their tops
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            var ankle = V(0.11f * s, 0.07f, 0.03f);
            var dir = Vector3.Normalize(V(0.06f * s, 0.47f, 0) - ankle);
            b.Limb(leg, ankle + dir * 0.0f, ankle + dir * 0.13f, 0.05f, 0.048f, wrap, blend: 0.008f);
            b.Torus(leg, ankle + dir * 0.13f, 0.048f, 0.016f, Rgb(250, 244, 226), Euler(dir), blend: 0.006f);
            foreach (float t in new[] { 0.03f, 0.07f, 0.11f })
                b.PaintTorus(leg, ankle + dir * t, 0.05f, 0.005f, Rgb(214, 200, 168), Euler(dir + V(0.2f * s, 0, 0)));
        });
        Stretch(b, V(1f, 1.1f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2107 Pugilord, from Hitmonchan

    /// <summary>Pugilord: Hitmonchan broader in the chest, its red gloves become gauntlets of steel banded red, a gold champion's belt at its waist and a high collar.</summary>
    private static PokeBuilder Pugilord()
    {
        var b = Become(Hitmonchan(), "Pugilord", 0.92f);
        var glove = Rgb(222, 64, 64);
        var gold = Rgb(232, 186, 66);
        var tunic = Rgb(214, 210, 236);
        var armour = Rgb(138, 146, 160);
        var red = Rgb(196, 52, 54);
        var skin = Rgb(190, 156, 122);
        int head = BoneOf(b, "head");
        // The crest of points goes under a helmet
        b.Sdf.RemoveAll(s => s.Bone == head && s.Shape == SdfShape.RoundCone && s.Color.Equals(skin));
        Grow(b, V(1.22f, 1.04f, 1.15f), V(0, 0.55f, 0), s => s.Bone == Body && s.Color.Equals(tunic), _ => false);
        // Armour of steel trimmed red and gold where the tunic was, gauntlets of steel banded red for gloves
        Recolour(b, (tunic, armour), (Rgb(198, 194, 234), armour), (glove, Steel), (White, red));
        foreach (var s in b.Sdf.Shapes.Where(s => s.Color.Equals(Steel) || s.Color.Equals(armour))) s.Material = Metal;
        b.PaintEll(Body, V(0, 0.63f, 0.09f), V(0.14f, 0.03f, 0.05f), red, V(10f, 0, 0));
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var fist = V(0.17f * s, 0.67f, 0.16f);
            var elbow = V(0.21f * s, 0.53f, 0.05f);
            Grow(b, V(1.45f, 1.45f, 1.45f), fist, sh => sh.Bone == arm && sh.Color.Equals(Steel), _ => false);
            var d = Vector3.Normalize(fist - elbow);
            foreach (float t in new[] { -0.07f, -0.03f })
                b.Torus(arm, fist + d * t, 0.07f, 0.013f, red, Euler(d), mat: Shell, blend: 0.004f);
            // Gold knuckle plates
            for (int k = -1; k <= 1; k++)
                b.Box(arm, fist + V(0.022f * k, 0.035f, 0.095f), V(0.016f, 0.014f, 0.012f), 0.004f, gold, default, Metal, 0.004f);
            // Plates over the shoulders, red at their edge, and red pads on its knees
            b.Ell(Body, V(0.13f * s, 0.66f, 0), V(0.06f, 0.035f, 0.06f), armour, V(0, 0, -25f * s), Metal, 0.012f);
            b.PaintEll(Body, V(0.16f * s, 0.65f, 0), V(0.03f, 0.04f, 0.065f), red);
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            b.PaintEll(leg, V(0.095f * s, 0.12f, 0.02f), V(0.05f, 0.08f, 0.05f), armour);
            b.Ell(leg, V(0.1f * s, 0.22f, 0.06f), V(0.034f, 0.03f, 0.02f), red, mat: Shell, blend: 0.008f);
        });
        // The champion's belt, broad, its plate in front
        b.Torus(Body, V(0, 0.46f, 0), 0.15f, 0.034f, gold, sz: 0.8f, mat: Metal, blend: 0.006f);
        b.Ell(Body, V(0, 0.46f, 0.13f), V(0.06f, 0.045f, 0.02f), gold, mat: Metal, blend: 0.004f);
        b.Ell(Body, V(0, 0.46f, 0.145f), V(0.028f, 0.022f, 0.008f), Rgb(250, 214, 110), mat: Metal, blend: 0.002f);
        // A high collar
        b.Torus(Body, V(0, 0.665f, -0.005f), 0.08f, 0.024f, armour, V(-12f, 0, 0), sz: 0.9f, mat: Metal, blend: 0.01f);
        b.Ell(Body, V(0, 0.69f, -0.06f), V(0.085f, 0.045f, 0.035f), armour, mat: Metal, blend: 0.015f);
        // A helmet of steel, a visor of gold over its eyes and a crest of red and gold
        var c = V(0, 0.79f, 0.02f);
        b.Ell(head, c + V(0, 0.035f, -0.012f), V(0.1f, 0.085f, 0.09f), armour, mat: Metal, blend: 0.01f);
        b.Ell(head, c + V(0, 0.045f, 0.07f), V(0.075f, 0.016f, 0.03f), gold, V(-10f, 0, 0), Metal, 0.006f);
        b.Ell(head, c + V(0, 0.11f, -0.01f), V(0.022f, 0.05f, 0.08f), red, mat: Shell, blend: 0.01f);
        b.Ell(head, c + V(0, 0.13f, 0.03f), V(0.024f, 0.035f, 0.03f), gold, mat: Metal, blend: 0.008f);
        return b;
    }

    // ------------------------------------------------------------------ 2110 Smoglom, from Weezing

    /// <summary>
    /// Smoglom: Weezing joined by a third, smallest ball, all three ringed with a thin band of fire at their seams,
    /// smoke rising from vents on their tops.
    /// </summary>
    private static PokeBuilder Smoglom()
    {
        var b = Become(Weezing(), "Smoglom", 0.88f);
        var lift = Shifted(b, "small", V(0.13f, 0.42f, -0.04f));
        var purple = Rgb(68, 62, 76);
        var smoke = Rgb(112, 92, 124);
        Recolour(b, (Rgb(126, 106, 168), purple), (Rgb(214, 206, 150), smoke), (PixelCanvas.Mix(Rgb(126, 106, 168), Rgb(30, 24, 40), 0.25f), Rgb(48, 44, 54)));
        // Glowing ember eyes
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
        {
            d.White = Rgb(255, 196, 70);
            d.Pupil = Rgb(250, 130, 40);
        }
        // Its skin rough as cinder, pocked with paler lumps
        foreach (var (d, size) in new[] { (V(0.5f, 0.7f, 0.3f), 0.03f), (V(-0.6f, 0.5f, 0.2f), 0.028f), (V(0.2f, -0.6f, 0.6f), 0.03f), (V(-0.3f, 0.9f, -0.2f), 0.032f), (V(0.8f, 0.2f, -0.4f), 0.03f), (V(-0.8f, -0.3f, -0.3f), 0.028f) })
            b.Ell(Body, Out(V(-0.08f, 0.32f, 0) + lift, V(0.2f, 0.19f, 0.19f), default, d), V(size, size * 0.7f, size), Rgb(86, 80, 96), mat: Shell, blend: 0.012f);
        var cBig = V(-0.08f, 0.32f, 0) + lift;
        var cSmall = V(0.19f, 0.42f, -0.04f) + lift;
        var cThird = V(-0.26f, 0.52f, -0.08f) + lift;
        var rThird = V(0.095f, 0.095f, 0.09f);
        int third = b.Part("third", Body, cThird + V(0.05f, -0.04f, 0), PokeRole.Head, 2.6f, -1f);
        b.Ell(third, cThird, rThird, purple);
        b.Ell(Body, (cBig + cThird) * 0.5f, V(0.06f, 0.06f, 0.06f), purple, blend: 0.05f);
        PokeBuilder.Both(s => b.Eye(third, Out(cThird, rThird, default, V(-0.2f + 0.4f * s, 0.15f, 1f)), V(-0.2f + 0.4f * s, 0.15f, 1f), 0.02f, sclera: true, white: Rgb(255, 196, 70), pupil: Rgb(250, 130, 40), glare: true));
        b.Torus(third, Out(cThird, rThird, default, V(-0.1f, -0.35f, 1f)), 0.014f, 0.006f, Rgb(90, 84, 100), Euler(V(-0.1f, -0.35f, 1f)));
        // Bands of fire at the seams
        var fire = Rgb(250, 140, 50);
        foreach (var (a, z, bone) in new[] { (cBig, cSmall, Body), (cBig, cThird, Body) })
        {
            var d = Vector3.Normalize(z - a);
            var mid = a + (z - a) * 0.52f;
            b.Torus(bone, mid, 0.1f, 0.014f, fire, Euler(d), mat: Glow, blend: 0.008f);
            // Cracks of lava running out from the seam over the bigger ball
            foreach (var (off, len, roll) in new[] { (V(0, 0.06f, 0.06f), 0.07f, 20f), (V(0, -0.06f, 0.07f), 0.06f, -25f), (V(0, 0.02f, 0.1f), 0.05f, 60f) })
                PaintAs(b, Body, a + (z - a) * 0.38f + off, V(0.012f, len, 0.03f), fire, Glow, V(0, 0, roll));
        }
        // Vents on their tops, smoke rising from them
        foreach (var (at, size, bone) in new[] { (cBig + V(0.02f, 0.19f, -0.02f), 0.03f, Body), (cSmall + V(0.01f, 0.13f, -0.02f), 0.024f, BoneOf(b, "small")), (cThird + V(0, 0.09f, -0.01f), 0.02f, third) })
        {
            b.Limb(bone, at - V(0, 0.02f, 0), at + V(0, 0.03f, 0), size, size * 0.9f, Rgb(70, 52, 80));
            GasPuff(b, bone, at + V(0, 0.03f + size * 0.5f, 0), V(0.2f, 1f, 0), size * 1.2f, smoke);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2115 Matriroo, from Kangaskhan

    /// <summary>Matriroo: Kangaskhan taller, its helmet grown into a crest, and the child in its pouch older and wearing a helmet of its own.</summary>
    private static PokeBuilder Matriroo()
    {
        var b = Become(Kangaskhan(), "Matriroo", 0.97f);
        // A warmer brown hide, its helmet and crest pale as bone
        var dark = Rgb(226, 212, 178);
        Recolour(b, (Rgb(92, 70, 62), dark), (Rgb(160, 124, 104), Rgb(176, 136, 88)));
        int head = BoneOf(b, "head");
        var c = V(0, 0.9f, 0.08f);
        Blade(b, head, c + V(0, 0.07f, 0.04f), c + V(0, 0.2f, -0.17f), 0.11f, dark, V(1f, 0, 0), 0.25f, Shell);
        // The child grown older, its own helmet on
        int child = BoneOf(b, "baby");
        var bc = V(0, 0.53f, 0.2f);
        Grow(b, V(1.3f, 1.3f, 1.3f), bc - V(0, 0.04f, 0), bone => bone == child);
        var nc = bc - V(0, 0.04f, 0) + V(0, 0.04f, 0) * 1.3f;
        b.Ell(child, nc + V(0, 0.05f, -0.004f), V(0.088f, 0.04f, 0.082f), dark, mat: Shell, blend: 0.012f);
        Stretch(b, V(1f, 1.06f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2119 Sabrefin, from Seaking

    /// <summary>
    /// Sabrefin: Seaking longer and sleeker, its horn grown into a straight sword's blade, patched like a koi, its fins
    /// flowing longer; the female's shorter horn kept as a shorter blade.
    /// </summary>
    /// <param name="female">A female keeps Seaking's: a shorter blade.</param>
    private static PokeBuilder Sabrefin(bool female = false)
    {
        var b = Become(Seaking(female), "Sabrefin", 0.9f);
        var lift = Shifted(b, "head", V(0, 0.45f, 0.15f));
        int head = BoneOf(b, "head");
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.Equals(Rgb(250, 236, 200)));
        // A pearl-white koi, its fins long, flowing and peach-pink
        var pearl = Rgb(248, 240, 244);
        var peach = Rgb(248, 212, 200);
        var navy = Rgb(40, 52, 86);
        Recolour(b, (Rgb(238, 100, 52), pearl), (Rgb(244, 242, 240), peach), (Rgb(50, 44, 50), navy));
        foreach (var name in new[] { "dorsal", "tailFin", "finL", "finR" })
        {
            int finBone = BoneOf(b, name);
            b.Model.Decals.RemoveAll(d => d.Bone == finBone);
        }
        // Its horn grown into a long spiralled lance of ivory
        var foot = V(0, 0.6f, 0.22f) + lift;
        var tip = (female ? V(0, 0.8f, 0.4f) : V(0, 0.92f, 0.52f)) + lift;
        b.Spike(head, foot, tip, 0.04f, Rgb(244, 238, 224), mat: Shell);
        Helix(b, head, foot, tip, 0.04f, 4f, 0.007f, Rgb(214, 202, 180));
        // Patched like a koi, orange and navy over its back and head
        foreach (var (at, size, color) in new[] { (V(0.1f, 0.52f, 0.1f), 0.07f, Rgb(240, 132, 64)), (V(-0.1f, 0.54f, -0.08f), 0.08f, Rgb(240, 132, 64)), (V(0.09f, 0.46f, -0.2f), 0.06f, Rgb(240, 132, 64)), (V(0f, 0.62f, 0.14f), 0.06f, Rgb(240, 132, 64)),
            (V(0.1f, 0.42f, -0.06f), 0.03f, navy), (V(-0.09f, 0.48f, 0.08f), 0.028f, navy), (V(0.06f, 0.6f, -0.02f), 0.025f, navy), (V(-0.08f, 0.38f, -0.2f), 0.026f, navy) })
            b.PaintEll(Body, at + lift, V(size * 0.6f, size, size * 1.2f), color);
        // Long flowing fins like a veil, peach paling to blue at their ends
        int dorsal = BoneOf(b, "dorsal");
        int fin = BoneOf(b, "tailFin");
        int finL = BoneOf(b, "finL"), finR = BoneOf(b, "finR");
        b.Sdf.RemoveAll(s => s.Bone == dorsal || s.Bone == fin || s.Bone == finL || s.Bone == finR);
        var veil = Rgb(248, 214, 204);
        var veilEnd = Rgb(214, 228, 246);
        void Veil(int bone, Vector3[] path, float width, Vector3 facing)
        {
            var p = path.Select(q => q + lift).ToArray();
            Ribbon(b, bone, p, width, veil, facing, 0.014f);
            b.PaintEll(bone, p[^2], V(width * 1.2f, width * 1.2f, width * 1.2f), veilEnd);
        }
        Veil(dorsal, Smooth(3, V(0, 0.62f, 0.06f), V(0, 0.72f, -0.06f), V(0, 0.76f, -0.2f), V(0, 0.7f, -0.34f)), 0.1f, V(1f, 0, 0));
        foreach (var (up, len) in new[] { (0.22f, 0.5f), (0.02f, 0.46f), (-0.2f, 0.5f) })
            Veil(fin, Smooth(3, V(0, 0.45f, -0.3f), V(0, 0.45f + up * 0.5f, -0.3f - len * 0.4f), V(0, 0.45f + up, -0.3f - len * 0.75f), V(0, 0.45f + up * 1.3f, -0.3f - len)), 0.1f, V(1f, 0, 0));
        PokeBuilder.Both(s =>
        {
            int side = s < 0 ? finL : finR;
            Veil(side, Smooth(3, V(0.1f * s, 0.36f, 0.06f), V(0.16f * s, 0.28f, 0.0f), V(0.2f * s, 0.18f, -0.08f), V(0.22f * s, 0.08f, -0.14f)), 0.07f, V(s, 0.3f, 0));
            Veil(side, Smooth(3, V(0.06f * s, 0.32f, -0.12f), V(0.08f * s, 0.24f, -0.2f), V(0.1f * s, 0.16f, -0.28f)), 0.05f, V(s, 0.2f, 0));
        });
        Stretch(b, V(0.92f, 1f, 1.15f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2121 Stellamir, from Starmie

    /// <summary>Stellamir: Starmie with a third, smaller star turning between its two, all three round the core, its gem holding a swirl of light.</summary>
    private static PokeBuilder Stellamir()
    {
        var b = Become(Starmie(), "Stellamir", 0.9f);
        var lift = Shifted(b, "body", V(0, 0.42f, 0));
        var c = V(0, 0.42f, 0) + lift;
        // Deep blue-violet stars, a third of glowing teal turning between them like webbing
        var purple = Rgb(132, 118, 190);
        Recolour(b, (purple, Rgb(110, 84, 196)), (PixelCanvas.Mix(purple, Rgb(60, 50, 110), 0.25f), Rgb(52, 58, 140)));
        int star = b.Part("star3", Body, c, PokeRole.Leaf, 1.2f);
        StarPoints(b, star, c - V(0, 0, 0.015f), 0.44f, 0.12f, 18f, 0.42f, Rgb(70, 192, 196));
        b.Ell(star, c - V(0, 0, 0.015f), V(0.1f, 0.1f, 0.05f), Rgb(70, 192, 196), blend: 0.02f);
        // The gem's swirl of light, pink and violet
        Recolour(b, (Rgb(214, 48, 78), Rgb(236, 120, 200)));
        var gem = c + V(0, 0, 0.09f + 0.068f * 0.15f + 0.03f);
        for (int i = 0; i < 4; i++)
        {
            float a = i * 1.6f;
            float rr = 0.012f + 0.008f * i;
            b.PaintEll(Body, gem + V(MathF.Cos(a) * rr, MathF.Sin(a) * rr, 0), V(0.012f, 0.012f, 0.02f), Rgb(170, 110, 240), soft: 0.006f);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2122 Mimestro, from Mr. Mime

    /// <summary>
    /// Mimestro: Mr. Mime taller, in a tall hat and a conductor's tailcoat of pink and white, a pane of light held in
    /// its hand.
    /// </summary>
    private static PokeBuilder Mimestro()
    {
        var b = Become(MrMime(), "Mimestro", 0.92f);
        var pink = Rgb(238, 98, 120);
        var magenta = Rgb(176, 42, 102);
        var navy = Rgb(246, 206, 222);
        // Magenta where Mr. Mime is blue: its curled hair and shoes
        Recolour(b, (Rgb(44, 92, 172), magenta));
        int head = BoneOf(b, "head");
        // A tall pale pink hat, banded magenta
        b.Ell(head, V(0, 0.965f, 0.01f), V(0.12f, 0.014f, 0.11f), navy, mat: Shell, blend: 0.01f);
        b.Limb(head, V(0, 0.96f, 0.01f), V(0, 1.16f, 0.0f), 0.07f, 0.08f, navy, Shell, 0.004f);
        b.PaintTorus(head, V(0, 0.995f, 0.008f), 0.072f, 0.016f, magenta, V(-3f, 0, 0));
        // Striped magenta trousers
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            b.PaintEll(leg, V(0.115f * s, 0.35f, 0.015f), V(0.05f, 0.14f, 0.05f), magenta, Euler(V(0.05f * s, -0.22f, 0.03f)));
            b.PaintEll(leg, V(0.145f * s, 0.15f, 0.015f), V(0.045f, 0.11f, 0.045f), magenta);
        });
        // The tailcoat's tails behind, its lapels in front
        foreach (float x in new[] { -1f, 1f })
            Blade(b, Body, V(0.05f * x, 0.58f, -0.09f), V(0.12f * x, 0.16f, -0.22f), 0.065f, pink, V(x * 0.3f, 0, -1f), 0.25f);
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.07f * s, 0.68f, 0.09f), V(0.035f, 0.07f, 0.03f), pink, V(0, 0, -20f * s)));
        // A pane of light held in its right hand
        b.Box(BoneOf(b, "armR"), V(0.5f, 0.72f, 0.12f), V(0.07f, 0.11f, 0.006f), 0.004f, Rgb(190, 228, 255), V(0, 0, -12f), Glow, 0.006f);
        // A softer pink for the pads and the coat, a white waistcoat between its lapels
        Recolour(b, (pink, Rgb(232, 150, 182)));
        b.PaintEll(Body, V(0, 0.64f, 0.1f), V(0.05f, 0.09f, 0.03f), Rgb(246, 244, 246));
        Stretch(b, V(1f, 1.06f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2124 Aurorelle, from Jynx

    /// <summary>
    /// Aurorelle: a new figure out of Jynx, tall and slender in a gown of ice crystals, its long hair pale blue-white
    /// and lit along its length by the green and violet bands of an aurora, its face a pale lilac mask with large eyes.
    /// </summary>
    private static PokeBuilder Aurorelle()
    {
        var b = Become(Jynx(), "Aurorelle", 0.92f);
        var gown = Rgb(170, 208, 240);
        var ice = Rgb(224, 242, 255);
        Recolour(b, (Rgb(214, 72, 66), gown), (Rgb(244, 226, 156), Rgb(226, 236, 250)), (Rgb(112, 92, 168), Rgb(206, 188, 232)), (Rgb(242, 200, 84), Rgb(214, 226, 244)));
        // Crystals of ice on the gown, round its hem and at its shoulders
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            var at = d * 0.23f + V(0, 0.07f, 0);
            b.Spike(Body, at - d * 0.02f, at + d * 0.05f + V(0, 0.06f, 0), 0.025f, ice, mat: Shell, blend: 0.006f);
        }
        PokeBuilder.Both(s => b.Spike(Body, V(0.09f * s, 0.56f, 0.0f), V(0.14f * s, 0.65f, -0.02f), 0.024f, ice, mat: Shell, blend: 0.006f));
        // The aurora along its hair
        int head = BoneOf(b, "head");
        foreach (var (x, color) in new[] { (-0.07f, Rgb(120, 240, 190)), (-0.025f, Rgb(190, 140, 250)), (0.025f, Rgb(120, 240, 190)), (0.07f, Rgb(190, 140, 250)) })
            PaintAs(b, head, V(x, 0.58f, -0.11f), V(0.016f, 0.2f, 0.06f), color, Glow);
        // Its hair streams out in long locks flaring wide to the ground, lit by the aurora along their length
        var hairPale = Rgb(204, 222, 248);
        Recolour(b, (Rgb(244, 156, 176), Rgb(204, 176, 226)));
        PokeBuilder.Both(s =>
        {
            foreach (var (x, back, flare) in new[] { (0.08f, -0.07f, 0.34f), (0.1f, -0.03f, 0.26f), (0.05f, -0.1f, 0.2f) })
            {
                var path = Smooth(3, V(x * s, 0.72f, back), V((x + 0.08f) * s, 0.5f, back - 0.04f), V((x + flare * 0.6f) * s, 0.24f, back - 0.02f), V((x + flare) * s, 0.06f, back + 0.04f), V((x + flare + 0.06f) * s, 0.04f, back + 0.12f));
                b.Tube(head, path, 0.05f, 0.016f, hairPale, blend: 0.02f);
                for (int i = 2; i < path.Length - 1; i += 3)
                    PaintAs(b, head, path[i], V(0.05f, 0.07f, 0.05f), i % 2 == 0 ? Rgb(120, 240, 190) : Rgb(190, 140, 250), Glow);
            }
        });
        // A crown of ice
        foreach (var (x, h) in new[] { (-0.06f, 0.05f), (-0.03f, 0.07f), (0f, 0.09f), (0.03f, 0.07f), (0.06f, 0.05f) })
            b.Spike(head, V(x, 0.85f, 0.04f), V(x * 1.2f, 0.85f + h, 0.03f), 0.014f, Rgb(214, 238, 255), mat: Shell, blend: 0.006f);
        // Larger eyes
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
        {
            d.Size *= 1.25f;
            d.Half *= 1.25f;
        }
        Stretch(b, V(0.92f, 1.12f, 0.92f));
        return b;
    }

    // ------------------------------------------------------------------ 2127 Mandibron, from Pinsir

    /// <summary>
    /// Mandibron: Pinsir darkened to brown-black, its horns grown longer and near black with an edge of red teeth, and
    /// a smooth carapace closed over its back.
    /// </summary>
    private static PokeBuilder Mandibron()
    {
        // Pinsir gone down on six legs into a great beetle: its body lies along the ground under a closed carapace, and
        // its two horns reach forward from its head as mandibles, near black, toothed in red along their insides
        var b = new PokeBuilder("Mandibron", 0.95f, BodyPlan.Quadruped, V(0, 0.26f, -0.04f)) { Coat = Shell };
        var brown = Rgb(58, 46, 44);
        var dark = Rgb(38, 30, 32);
        var horn = Rgb(44, 36, 40);
        var red = Rgb(170, 40, 46);
        // Six jointed legs, spurred, each foot two claws
        foreach (var (z, front, name) in new[] { (0.12f, true, (string?)null), (-0.04f, false, "mid"), (-0.2f, false, (string?)null) })
            PokeBuilder.Both(s =>
            {
                var hip = V(0.12f * s, 0.2f, z);
                int leg = name == null ? b.Leg(s, hip, front) : b.Part(name + (s < 0 ? "L" : "R"), Body, hip, PokeRole.Leg, 0.5f, s);
                var knee = V(0.25f * s, 0.17f, z + (front ? 0.06f : -0.02f));
                var foot = V(0.29f * s, 0.02f, z + (front ? 0.1f : -0.04f));
                b.Limb(leg, hip, knee, 0.032f, 0.026f, brown, Shell);
                b.Limb(leg, knee, foot, 0.026f, 0.018f, brown, Shell);
                b.Spike(leg, Vector3.Lerp(knee, foot, 0.4f), Vector3.Lerp(knee, foot, 0.4f) + V(0.03f * s, 0.02f, 0), 0.008f, dark, mat: Shell, blend: 0.003f);
                foreach (float t in new[] { -1f, 1f })
                    b.Spike(leg, foot + V(0, 0.01f, 0), foot + V(0.015f * s + 0.012f * t, -0.008f, 0.03f * (front ? 1f : -0.3f) + 0.012f * t), 0.009f, dark, mat: Shell, blend: 0.003f);
            });
        // The body under a closed carapace, its two wing cases parted by a seam, bands on the abdomen beneath
        var c = V(0, 0.24f, -0.08f);
        var r = V(0.2f, 0.13f, 0.28f);
        b.Ell(Body, c, r, brown);
        b.Ell(Body, c + V(0, 0.07f, -0.03f), V(0.23f, 0.16f, 0.29f), dark, mat: Shell, blend: 0.02f);
        b.PaintEll(Body, c + V(0, 0.23f, -0.03f), V(0.006f, 0.05f, 0.29f), Rgb(90, 64, 60), soft: 0.006f);
        b.PaintEll(Body, c + V(0, 0.1f, 0.18f), V(0.18f, 0.06f, 0.008f), Rgb(80, 58, 56), soft: 0.006f);
        foreach (float z in new[] { -0.16f, -0.08f, 0.0f, 0.08f })
            b.PaintEll(Body, c + V(0, -0.1f, z), V(0.15f, 0.04f, 0.008f), Rgb(84, 66, 60), soft: 0.006f);
        // A broad thorax and a low head between the mandibles, its eyes burning orange
        int head = b.Head(V(0, 0.26f, 0.18f));
        var hc = V(0, 0.25f, 0.24f);
        var hr = V(0.1f, 0.075f, 0.08f);
        b.Ell(head, hc, hr, dark, mat: Shell);
        b.Ell(head, hc + V(0, 0.03f, -0.04f), V(0.13f, 0.06f, 0.07f), dark, mat: Shell, blend: 0.03f);
        PokeBuilder.Both(s =>
        {
            b.Eye(head, On(hc, hr, 0.05f * s, hc.Y + 0.015f), V(0.6f * s, 0.15f, 1f), 0.018f, sclera: true, white: Rgb(250, 120, 60), pupil: Rgb(255, 200, 120), glare: true);
            // Small hooked mouthparts and feelers
            b.Spike(head, hc + V(0.03f * s, -0.05f, 0.06f), hc + V(0.02f * s, -0.09f, 0.08f), 0.012f, Rgb(110, 70, 66), mat: Shell, blend: 0.004f);
            b.Tube(head, new[] { hc + V(0.05f * s, -0.02f, 0.07f), hc + V(0.1f * s, -0.04f, 0.12f), hc + V(0.13f * s, -0.02f, 0.15f) }, 0.004f, 0.003f, dark, blend: 0f);
        });
        // The two great mandibles, reaching forward and up, toothed in red along their insides
        PokeBuilder.Both(s =>
        {
            var path = Smooth(3, hc + V(0.06f * s, 0.04f, 0.02f), hc + V(0.13f * s, 0.12f, 0.14f), hc + V(0.14f * s, 0.26f, 0.26f), hc + V(0.06f * s, 0.36f, 0.36f));
            b.Tube(head, path, 0.05f, 0.012f, horn, Shell, 0f);
            for (int i = 2; i < path.Length - 1; i++)
            {
                float t = i / (float)(path.Length - 1);
                var p = path[i];
                var d = Vector3.Normalize(path[i + 1] - path[i - 1]);
                var inward = Vector3.Normalize(V(-s, 0, 0) - d * Vector3.Dot(V(-s, 0, 0), d));
                float rr = 0.05f + (0.012f - 0.05f) * t;
                b.Spike(head, p + inward * rr * 0.5f, p + inward * (rr + 0.028f) + d * 0.01f, 0.011f, red, mat: Shell, blend: 0.004f);
            }
        });
        return b;
    }

    // ------------------------------------------------------------------ 2128 Bovarch, from Tauros

    /// <summary>
    /// Bovarch: Tauros heavier in the shoulders, its horns grown wider and ringed, its mane longer and darker, and a
    /// crest of rock-like hide on its brow where the three studs were.
    /// </summary>
    private static PokeBuilder Bovarch()
    {
        var b = Become(Tauros(), "Bovarch", 1f);
        var oldMane = Rgb(116, 106, 100);
        var mane = Rgb(54, 48, 40);
        // A dun hide, a near black mane, pale grey hooves; horns of old ivory
        Recolour(b, (oldMane, mane), (Rgb(206, 158, 98), Rgb(150, 132, 104)), (Rgb(112, 78, 62), Rgb(118, 100, 80)), (Rgb(118, 124, 160), Rgb(96, 90, 82)), (Rgb(96, 98, 140), mane));
        Grow(b, V(1.12f, 1.15f, 1.15f), V(0, 0.5f, 0.15f), s => s.Bone == Body && s.Color.Equals(mane), _ => false);
        int head = BoneOf(b, "head");
        var horn = Rgb(150, 152, 170);
        // The studs give way to a crest of rock-like hide
        b.Sdf.RemoveAll(s => s.Bone == head && s.Shape == SdfShape.Ellipsoid && s.Color.Equals(horn));
        var c = V(0, 0.52f, 0.37f);
        var r = V(0.085f, 0.08f, 0.09f);
        var brow = On(c, r, 0, 0.555f);
        b.Ell(head, brow + V(0, 0.01f, -0.015f), V(0.06f, 0.03f, 0.04f), Rgb(150, 146, 140), V(-20f, 0, 0), Shell, 0.012f);
        foreach (float x in new[] { -0.03f, 0f, 0.03f })
            b.Spike(head, brow + V(x, 0.02f, -0.02f), brow + V(x * 1.3f, 0.06f, -0.05f), 0.016f, Rgb(150, 146, 140), mat: Shell, blend: 0.006f);
        // Wider horns, ringed
        Grow(b, V(1.4f, 1.08f, 1f), V(0, 0.56f, 0.35f), s => s.Bone == head && s.Color.Equals(horn), _ => false);
        PokeBuilder.Both(s =>
        {
            var path = Smooth(3, V(0.06f * s, 0.56f, 0.35f), V(0.15f * s, 0.585f, 0.36f), V(0.2f * s, 0.65f, 0.36f), V(0.18f * s, 0.74f, 0.38f));
            for (int i = 2; i < path.Length - 2; i += 2)
            {
                var p = V(0, 0.56f, 0.35f) + (path[i] - V(0, 0.56f, 0.35f)) * V(1.4f, 1.08f, 1f);
                var q = V(0, 0.56f, 0.35f) + (path[i + 1] - V(0, 0.56f, 0.35f)) * V(1.4f, 1.08f, 1f);
                float t = i / (float)(path.Length - 1);
                b.PaintTorus(head, p, 0.03f + (0.012f - 0.03f) * t, 0.005f, Rgb(96, 96, 112), Euler(q - p));
            }
        });
        Recolour(b, (horn, Rgb(196, 180, 146)), (Rgb(96, 96, 112), Rgb(132, 116, 92)));
        return b;
    }

    // ------------------------------------------------------------------ 2130 Typhonarch, from Gyarados

    /// <summary>
    /// Typhonarch: Gyarados grown into the dragon it has always looked like: two short forelegs with clawed hands, its
    /// crest grown into a great fan of fins and its whiskers long and flowing (the female's white).
    /// </summary>
    /// <param name="female">A female keeps Gyarados's: white whiskers.</param>
    private static PokeBuilder Typhonarch(bool female = false)
    {
        var b = Become(Gyarados(female), "Typhonarch", 1f);
        var blue = Rgb(58, 150, 226);
        var cream = Rgb(244, 226, 156);
        var navy = Rgb(36, 70, 160);
        var white = Rgb(236, 242, 248);
        const float hy = 0.94f;
        int head = BoneOf(b, "head");
        // The crest grown into a great fan of fins
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.Equals(navy));
        for (int i = 0; i < 7; i++)
        {
            float a = (-66f + i * 22f) * Degree;
            var root = V(MathF.Sin(a) * 0.04f, hy + 0.08f, 0.1f);
            Blade(b, head, root, root + V(MathF.Sin(a) * 0.3f, MathF.Cos(a) * 0.3f, -0.14f), 0.075f, navy, V(0, 0.15f, 1f), 0.25f);
        }
        // Long flowing whiskers, from its snout out and back on the wind (the female's paler still)
        var whisker = female ? white : Rgb(200, 232, 236);
        b.Sdf.RemoveAll(s => s.Bone == head && s.Shape == SdfShape.RoundCone && s.Size.X < 0.012f && (s.Color.Equals(white) || s.Color.Equals(Rgb(96, 172, 236))));
        PokeBuilder.Both(s => b.Tube(head, Smooth(3, V(0.08f * s, hy - 0.02f, 0.38f), V(0.22f * s, hy - 0.08f, 0.36f), V(0.34f * s, hy - 0.02f, 0.24f), V(0.4f * s, hy - 0.16f, 0.12f), V(0.46f * s, hy - 0.1f, 0.0f)), 0.01f, 0.004f, whisker, blend: 0f));
        // A fan of fins down its whole back
        for (int i = 1; i <= 6; i++)
        {
            int segBone = BoneOf(b, "seg" + i);
            b.Model.Decals.RemoveAll(d => d.Bone == segBone && !d.IsEye);
        }
        // Two short forelegs with clawed hands, from where it rears up
        int seg = BoneOf(b, "seg4");
        var mid = (V(0.16f, 0.44f, 0.05f) + V(0.05f, 0.62f, -0.03f)) / 2f;
        PokeBuilder.Both(s =>
        {
            int arm = b.Part(s < 0 ? "armL" : "armR", seg, mid + V(0.1f * s, -0.03f, 0.04f), PokeRole.Arm, s, s);
            var shoulder = mid + V(0.08f * s, -0.02f, 0.05f);
            var elbow = mid + V(0.26f * s, -0.1f, 0.14f);
            var hand = mid + V(0.22f * s, -0.16f, 0.3f);
            b.Limb(arm, shoulder, elbow, 0.062f, 0.05f, blue);
            b.Ell(arm, elbow, V(0.05f, 0.05f, 0.05f), blue);
            b.Limb(arm, elbow, hand, 0.05f, 0.042f, blue);
            b.PaintEll(arm, (elbow + hand) / 2f + V(0, -0.025f, 0.01f), V(0.04f, 0.07f, 0.04f), cream, Euler(hand - elbow));
            b.Ell(arm, hand, V(0.045f, 0.04f, 0.045f), blue);
            Claws(b, arm, hand + V(0, -0.01f, 0.035f), V(0.024f, 0, 0), V(0, -0.4f, 1f), 0.05f, 0.014f);
        });
        // Teal scales, a pale belly, fins of sea green
        Recolour(b, (blue, Rgb(38, 112, 128)), (cream, Rgb(150, 214, 204)), (navy, Rgb(70, 160, 158)), (Rgb(250, 214, 74), Rgb(100, 186, 180)),
            (Rgb(236, 242, 248), Rgb(168, 226, 216)), (Rgb(170, 216, 244), Rgb(150, 220, 210)));
        return b;
    }

    // ------------------------------------------------------------------ 2134 Naiadeon, from Vaporeon

    /// <summary>
    /// Naiadeon: Vaporeon longer, its ruff grown into a flowing mane of fins, its tail fin wider and pale at its edge,
    /// pale wave lines painted down its body.
    /// </summary>
    private static PokeBuilder Naiadeon()
    {
        var b = Become(Vaporeon(), "Naiadeon", 0.84f);
        const float h = 0.17f;
        var teal = Rgb(44, 116, 128);
        var cream = Rgb(126, 200, 190);
        var pale = Rgb(206, 238, 252);
        // Deep teal, its frill and fins sea green
        Recolour(b, (Rgb(92, 190, 232), teal), (Rgb(40, 96, 168), Rgb(30, 80, 96)), (Rgb(248, 240, 200), cream));
        // The mane of fins flowing back down its neck and over its shoulders
        for (int i = 0; i < 13; i++)
        {
            float a = -2.0f + i * (4f / 12f);
            var at = V(MathF.Sin(a) * 0.05f, h + 0.17f + MathF.Cos(a) * 0.02f, 0.12f + MathF.Cos(a) * 0.035f);
            var tip = at + V(MathF.Sin(a) * 0.14f, 0.02f - MathF.Abs(MathF.Sin(a)) * 0.12f, -0.3f);
            Ribbon(b, Body, Smooth(3, at, (at + tip) / 2f + V(0, 0.07f, 0), tip), 0.07f, i % 2 == 0 ? cream : Rgb(96, 176, 170), V(MathF.Sin(a), 1f, 0), 0.012f);
        }
        // A wider tail fin, pale at its edge
        int tail = BoneOf(b, "tail");
        var end = V(0.15f, h - 0.03f, -0.4f);
        Grow(b, V(1.6f, 1.6f, 1.6f), end, s => s.Bone == tail && Vector3.Distance(s.A, end) < 0.01f, _ => false);
        b.PaintEll(tail, end + V(0.16f, 0.09f, -0.07f), V(0.06f, 0.06f, 0.06f), pale);
        b.PaintEll(tail, end + V(0.14f, -0.015f, 0.12f), V(0.06f, 0.06f, 0.06f), pale);
        // Wave lines down its body
        PokeBuilder.Both(s =>
        {
            foreach (float z in new[] { 0.05f, -0.07f })
            {
                var n = V(s, 0.35f, z * 4f);
                b.Mark(Body, Out(V(0, h + 0.02f, -0.025f), V(0.075f, 0.07f, 0.15f), default, n), n, 0.05f, 0.014f, pale, MarkShape.Wave);
            }
        });
        // A long fin down its tail
        int tailBone = BoneOf(b, "tail");
        Ribbon(b, tailBone, Smooth(3, V(0, h + 0.07f, -0.16f), V(0.04f, h + 0.08f, -0.28f), V(0.12f, h + 0.02f, -0.38f)), 0.05f, cream, V(0, 1f, 0), 0.012f);
        Stretch(b, V(1f, 1.04f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2135 Fulgureon, from Jolteon

    /// <summary>Fulgureon: Jolteon with its spikes grown long and swept back into bolts of lightning, white at their tips, and its ruff stood up in a crest.</summary>
    private static PokeBuilder Fulgureon()
    {
        var b = Become(Jolteon(), "Fulgureon", 0.8f);
        var yellow = Rgb(250, 214, 56);
        int tail = BoneOf(b, "tail");
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == tail && s.Color.Equals(yellow) && s.Shape == SdfShape.RoundCone).ToList())
        {
            var d = s.B - s.A;
            s.B = s.A + d * 1.6f + V(0, 0, -0.05f);
            var dir = Vector3.Normalize(s.B - s.A);
            var kink = Vector3.Normalize(dir + Vector3.Normalize(Vector3.Cross(dir, V(0, 0, 1f)) + V(0.001f, 0, 0)) * 0.9f);
            b.Spike(tail, s.B - dir * 0.015f, s.B + kink * 0.09f, 0.026f, Rgb(255, 240, 150), mat: Glow, blend: 0.006f);
        }
        // The ruff stood up in a crest, dark as a thundercloud
        var navy = Rgb(44, 52, 76);
        var cloud = Rgb(96, 102, 118);
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == Body && s.Color.Equals(White) && s.Shape == SdfShape.RoundCone).ToList())
        {
            var d = s.B - s.A;
            s.B = s.A + Vector3.Normalize(d + V(0, 0.16f, 0)) * d.Length() * 1.4f;
            s.Color = navy;
        }
        const float h = 0.24f;
        // Storm cloud over its crown and down the back of its neck
        int head = BoneOf(b, "head");
        foreach (var (at, rr) in new[] { (V(0, h + 0.27f, 0.06f), 0.05f), (V(-0.04f, h + 0.25f, 0.03f), 0.04f), (V(0.04f, h + 0.25f, 0.03f), 0.04f), (V(0, h + 0.24f, 0.0f), 0.045f) })
            b.Ell(head, at + V(0, 0.01f, -0.02f), V(rr, rr, rr) * 1.3f, cloud, blend: 0.02f);
        // Dark patches through its coat, jagged like the space between bolts
        foreach (var (at, size) in new[] { (V(0.06f, h + 0.02f, 0.06f), 0.045f), (V(-0.06f, h + 0.02f, 0.06f), 0.045f), (V(0.07f, h - 0.02f, -0.1f), 0.04f), (V(-0.07f, h - 0.02f, -0.1f), 0.04f) })
            b.PaintEll(Body, at, V(size * 0.7f, size * 1.2f, size), navy, V(0, 0, at.X > 0 ? 25f : -25f));
        foreach (var leg in new[] { "legL", "legR", "hindL", "hindR" })
        {
            int bone = BoneOf(b, leg);
            float sx = leg.EndsWith("L") ? -1f : 1f;
            float z = leg.StartsWith("leg") ? 0.09f : -0.11f;
            b.PaintEll(bone, V(0.06f * sx, h * 0.55f, z), V(0.035f, 0.06f, 0.035f), navy);
        }
        // Long bolts swept back over its back from its shoulders
        foreach (var (x, len) in new[] { (0.04f, 0.4f), (-0.04f, 0.4f), (0.08f, 0.3f), (-0.08f, 0.3f) })
        {
            var root = V(x, h + 0.08f, 0.04f);
            var mid = root + V(x * 0.8f, 0.1f, -len * 0.5f);
            var tip = root + V(x * 1.4f, 0.26f, -len);
            b.Spike(Body, root, mid + V(0, -0.04f, 0), 0.04f, Rgb(250, 214, 56), 0.35f);
            b.Spike(Body, mid - V(0, 0.06f, 0), tip, 0.036f, Rgb(250, 236, 140), 0.35f);
        }
        b.Sdf.Changed();
        return b;
    }

    // ------------------------------------------------------------------ 2136 Pyreon, from Flareon

    /// <summary>Pyreon: Flareon with its ruff and tail fuller, small flames burning at the ruff's tips, and the tuft on its brow streaming back.</summary>
    private static PokeBuilder Pyreon()
    {
        var b = Become(Flareon(), "Pyreon", 0.82f);
        const float h = 0.2f;
        var cream = Rgb(250, 230, 150);
        var about = V(0, h + 0.11f, 0.11f);
        Grow(b, V(1.25f, 1.25f, 1.25f), about, s => s.Bone == Body && s.Color.Equals(cream), _ => false);
        int tail = BoneOf(b, "tail");
        Grow(b, V(1.25f, 1.3f, 1.25f), V(0, h + 0.1f, -0.2f), s => s.Bone == tail && s.Color.Equals(cream), _ => false);
        for (int i = 0; i < 9; i++)
        {
            float a = -1.6f + i * 0.4f;
            var n = Vector3.Normalize(V(MathF.Sin(a) * 0.9f, 0.4f, MathF.Cos(a) * 0.9f));
            var at = Out(about, V(0.106f, 0.094f, 0.081f), default, n);
            FlameTongue(b, Body, at - n * 0.012f, at + n * 0.04f + V(0, 0.045f, 0), 0.016f, Rgb(244, 104, 44), Rgb(255, 220, 100));
        }
        int head = BoneOf(b, "head");
        // A deeper orange, its tail a great plume of flame-orange and its legs darkening to soot at the paws
        var orange = Rgb(214, 104, 46);
        var flame = Rgb(236, 132, 54);
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == tail && s.Color.Equals(cream))) s.Color = flame;
        Recolour(b, (Rgb(242, 128, 58), orange));
        foreach (var leg in new[] { "legL", "legR", "hindL", "hindR" })
        {
            int bone = BoneOf(b, leg);
            float sx = leg.EndsWith("L") ? -1f : 1f;
            float z = leg.StartsWith("leg") ? 0.09f : -0.12f;
            b.PaintEll(bone, V(0.055f * sx, 0.03f, z), V(0.035f, 0.05f, 0.045f), Rgb(70, 40, 30));
        }
        // Flames licking up from its tail's end
        foreach (var (dx, dy) in new[] { (0f, 0.14f), (0.06f, 0.1f), (-0.06f, 0.1f) })
            FlameTongue(b, tail, V(dx * 0.5f, h + 0.28f, -0.3f), V(dx, h + 0.28f + dy, -0.34f), 0.03f, Rgb(244, 104, 44), Rgb(255, 220, 100));
        // Its brow tuft streams back as a long lock
        b.Tube(head, Smooth(3, V(0, h + 0.3f, 0.15f), V(0, h + 0.33f, 0.06f), V(0.03f, h + 0.33f, -0.04f), V(0.08f, h + 0.36f, -0.1f)), 0.026f, 0.008f, orange, blend: 0.01f);
        return b;
    }

    // ------------------------------------------------------------------ 2143 Dozemount, from Snorlax

    /// <summary>
    /// Dozemount: Snorlax asleep so long the woods grew on it: a mossy mantle on its back and shoulders with small trees
    /// and flowers, and a bird's nest on its head.
    /// </summary>
    private static PokeBuilder Dozemount()
    {
        var b = Become(Snorlax(), "Dozemount", 1f);
        // A bear's dark brown hide where Snorlax is teal, its face and belly still cream
        Recolour(b, (Rgb(40, 104, 132), Rgb(84, 70, 60)));
        var moss = Rgb(98, 150, 72);
        var deep = Rgb(70, 120, 58);
        // The mantle of moss over its back and shoulders
        b.Ell(Body, V(0, 0.52f, -0.1f), V(0.34f, 0.2f, 0.24f), moss, V(-10f, 0, 0), Leaf, 0.04f);
        foreach (var (at, size) in new[] { (V(0.24f, 0.56f, 0.0f), 0.1f), (V(-0.24f, 0.56f, 0.0f), 0.1f), (V(0.12f, 0.66f, -0.14f), 0.09f), (V(-0.14f, 0.64f, -0.18f), 0.08f), (V(0.0f, 0.5f, -0.3f), 0.1f) })
            b.Ell(Body, at, V(size, size * 0.7f, size), moss, mat: Leaf, blend: 0.03f);
        // A mound of moss rising high over its back, small trees on it, flowers in the moss, roots hanging down its sides
        b.Ell(Body, V(0.04f, 0.66f, -0.16f), V(0.3f, 0.16f, 0.22f), moss, V(-10f, 0, 0), Leaf, 0.04f);
        foreach (var (foot, r) in new[] { (V(0.18f, 0.78f, -0.14f), 0.09f), (V(-0.16f, 0.74f, -0.16f), 0.06f), (V(0.02f, 0.72f, -0.3f), 0.055f), (V(0.28f, 0.64f, -0.02f), 0.05f) })
            Foliage(b, Body, foot, foot + V(0, r * 1.8f, 0), r, Rgb(120, 86, 54), deep);
        foreach (var (x, z) in new[] { (0.3f, 0.06f), (-0.3f, 0.04f), (0.26f, -0.18f), (-0.24f, -0.2f) })
            b.Tube(Body, new[] { V(x, 0.56f, z), V(x * 1.06f, 0.46f, z + 0.01f), V(x * 1.02f, 0.38f, z) }, 0.008f, 0.005f, Rgb(110, 80, 52), blend: 0.004f);
        foreach (var (at, color) in new[] { (V(0.26f, 0.62f, 0.06f), Rgb(246, 200, 220)), (V(-0.26f, 0.62f, 0.04f), Rgb(250, 230, 120)), (V(0.06f, 0.7f, -0.08f), Rgb(246, 246, 250)), (V(-0.08f, 0.68f, -0.06f), Rgb(246, 200, 220)) })
            b.Ell(Body, at, V(0.02f, 0.014f, 0.02f), color, mat: Leaf, blend: 0.004f);
        // A bird's nest on its head
        int head = BoneOf(b, "head");
        b.Torus(head, V(0, 0.9f, -0.01f), 0.065f, 0.028f, Rgb(140, 100, 62), mat: Leaf, blend: 0.01f);
        b.Ell(head, V(0, 0.9f, -0.01f), V(0.06f, 0.02f, 0.06f), Rgb(120, 86, 54), mat: Leaf, blend: 0.01f);
        foreach (float x in new[] { -0.02f, 0.02f })
            b.Ell(head, V(x, 0.925f, -0.01f + x), V(0.016f, 0.02f, 0.016f), Rgb(226, 236, 246), mat: Shell, blend: 0.004f);
        return b;
    }

    // ------------------------------------------------------------------ 2144 Glacirune, from Articuno

    /// <summary>
    /// Glacirune: Articuno grown, its two tail streamers become a long veil of aurora, green and violet, and crystals
    /// of ice at its wing tips.
    /// </summary>
    private static PokeBuilder Glacirune()
    {
        var b = Become(Articuno(), "Glacirune", 1f);
        var lift = Shifted(b, "head", V(0, 0.7f, 0.04f));
        // Paler, icier blues
        Recolour(b, (Rgb(116, 178, 234), Rgb(170, 216, 232)), (Rgb(206, 232, 250), Rgb(226, 244, 248)), (Rgb(54, 114, 198), Rgb(120, 184, 210)), (Rgb(150, 150, 164), Rgb(60, 80, 120)));
        int tail = BoneOf(b, "tail");
        var green = Rgb(100, 230, 170);
        var violet = Rgb(120, 80, 190);
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == tail))
        {
            s.Color = s.A.X < 0 ? green : violet;
            s.Material = Glow;
        }
        // A broad veil of aurora flowing out behind it, violet with green bands
        As(b, Glow, () =>
        {
            foreach (var (sx, w, color) in new[] { (0f, 0.24f, violet), (-0.5f, 0.12f, green), (0.5f, 0.12f, green) })
                Ribbon(b, tail, Smooth(3, V(0.01f * sx, 0.46f, -0.1f), V(0.06f * sx, 0.32f, -0.32f), V(0.12f * sx, 0.18f, -0.52f), V(0.16f * sx + 0.04f, 0.12f, -0.66f), V(0.22f * sx + 0.08f, 0.2f, -0.78f)).Select(p => p + lift).ToArray(),
                    w, color, V(1f, 0.15f, 0.2f), 0.014f);
        });
        // Crystals of ice at the ends of its long feathers
        var ice = Rgb(220, 242, 255);
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            var shoulder = V(0.08f * s, 0.64f, -0.02f) + lift;
            var wrist = V(0.24f * s, 0.8f, -0.06f) + lift;
            for (int i = 2; i < 6; i++)
            {
                float t = i / 5f;
                float a = (112f + (28f - 112f) * t) * Degree;
                var root = Vector3.Lerp(Vector3.Lerp(shoulder, wrist, 0.3f), wrist, t);
                var dir = Vector3.Normalize(V(MathF.Cos(a) * s, MathF.Sin(a), -0.25f));
                float len = 0.38f * (0.7f + 0.3f * MathF.Sin(MathF.PI * (0.25f + 0.75f * t)));
                var end = root + dir * len;
                b.Spike(wing, end - dir * 0.03f, end + dir * 0.06f, 0.022f, ice, mat: Shell, blend: 0.006f);
            }
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2145 Fulguris, from Zapdos

    /// <summary>Fulguris: Zapdos grown, its jagged feathers longer and darker at their roots, a crown of storm cloud at its head.</summary>
    private static PokeBuilder Fulguris()
    {
        var b = Become(Zapdos(), "Fulguris", 1f);
        var lift = Shifted(b, "head", V(0, 0.66f, 0.05f));
        var yellow = Rgb(246, 214, 70);
        var slate = Rgb(52, 60, 76);
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == Body && s.Color.Equals(yellow) && s.Shape == SdfShape.RoundCone))
            s.B = s.A + (s.B - s.A) * 1.4f;
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            var shoulder = V(0.08f * s, 0.62f, -0.02f) + lift;
            Grow(b, V(1.22f, 1.22f, 1.1f), shoulder, bone => bone == wing);
        });
        // Its feathers dark as a storm, lightning-yellow only at their ends; eyes that glow yellow
        Recolour(b, (yellow, slate), (Rgb(40, 36, 44), yellow));
        foreach (var w in new[] { "wingL", "wingR" })
        {
            int wing = BoneOf(b, w);
            float s = w.EndsWith("L") ? -1f : 1f;
            var shoulder = V(0.08f * s, 0.62f, -0.02f) + lift;
            foreach (var sh in b.Sdf.Shapes.Where(sh => sh.Bone == wing && sh.Op == SdfOp.Add && sh.Shape == SdfShape.RoundCone && sh.Color.Equals(slate)).ToList())
                b.PaintEll(wing, Vector3.Lerp(sh.A, sh.B, 0.82f), V(0.03f, Vector3.Distance(sh.A, sh.B) * 0.22f, 0.03f), yellow, Euler(sh.B - sh.A));
        }
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
        {
            d.White = Rgb(250, 240, 120);
            d.Pupil = Rgb(255, 255, 220);
        }
        // A crown of storm cloud at its head
        int head = BoneOf(b, "head");
        var hc = V(0, 0.75f, 0.08f) + lift;
        var cloud = Rgb(140, 144, 158);
        foreach (var (at, r) in new[] { (V(0, 0.07f, -0.04f), 0.05f), (V(-0.05f, 0.06f, -0.06f), 0.04f), (V(0.05f, 0.06f, -0.06f), 0.04f), (V(0, 0.08f, -0.1f), 0.045f), (V(-0.035f, 0.1f, -0.02f), 0.032f), (V(0.035f, 0.1f, -0.02f), 0.032f) })
            b.Ell(head, hc + at * 1.4f + V(0, 0.01f, 0), V(r, r * 0.8f, r) * 1.5f, cloud, blend: 0.02f);
        b.Sdf.Changed();
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2146 Pyrevant, from Moltres

    /// <summary>Pyrevant: Moltres with its fire whitened at the core, a tail of three long flame streamers and a halo of embers at its crest.</summary>
    private static PokeBuilder Pyrevant()
    {
        var b = Become(Moltres(), "Pyrevant", 1f);
        var lift = Shifted(b, "head", V(0, 0.64f, 0.1f));
        var rim = Rgb(214, 70, 40);
        var heart = Rgb(255, 246, 216);
        // A body of ash grey veined in orange, wings of fire white at the heart and red at the rim
        Recolour(b, (Rgb(255, 214, 90), heart), (PixelCanvas.Mix(Rgb(244, 112, 50), Rgb(255, 214, 90), 0.5f), PixelCanvas.Mix(rim, heart, 0.5f)), (Rgb(244, 112, 50), rim), (Rgb(244, 198, 84), Rgb(96, 92, 96)), (Rgb(170, 140, 116), Rgb(120, 116, 118)));
        b.PaintEll(Body, V(0, 0.56f, 0.09f) + lift, V(0.012f, 0.08f, 0.04f), Rgb(240, 140, 50), V(-25f, 0, 0), 0.006f);
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.04f * s, 0.6f, 0.08f) + lift, V(0.008f, 0.05f, 0.03f), Rgb(240, 140, 50), V(-25f, 0, 30f * s), 0.006f));
        // The wings burn white-gold at their hearts
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            b.PaintEll(wing, V(0.3f * s, 0.8f, -0.1f) + lift, V(0.16f, 0.12f, 0.12f), Rgb(255, 236, 190));
            b.PaintEll(wing, V(0.22f * s, 0.74f, -0.12f) + lift, V(0.12f, 0.09f, 0.1f), Rgb(255, 250, 236));
        });
        // Three long streamers of flame for a tail
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        foreach (float dx in new[] { -1f, 0f, 1f })
        {
            var path = Smooth(3, V(0, 0.5f, -0.14f), V(dx * 0.06f, 0.48f, -0.36f), V(dx * 0.14f, 0.42f, -0.58f), V(dx * 0.2f, 0.38f + 0.04f * MathF.Abs(dx), -0.8f)).Select(p => p + lift).ToArray();
            b.Tube(tail, path, 0.045f, 0.018f, Rgb(248, 168, 72), Glow, 0f);
            for (int i = 1; i < path.Length - 1; i += 2)
                b.PaintEll(tail, path[i], V(0.02f, 0.04f, 0.02f), heart, Euler(path[i + 1] - path[i - 1]));
            FlameTongue(b, tail, path[^1] - Vector3.Normalize(path[^1] - path[^2]) * 0.02f, path[^1] + Vector3.Normalize(path[^1] - path[^2]) * 0.12f + V(0, 0.04f, 0), 0.03f, rim, heart);
        }
        // A halo of embers round its crest, on a thread of light
        int head = BoneOf(b, "head");
        var c = V(0, 0.73f, 0.17f) + lift;
        var ring = c + V(0, 0.12f, -0.05f);
        b.Torus(head, ring, 0.08f, 0.013f, Rgb(255, 200, 110), mat: Glow, blend: 0.004f);
        b.Tube(head, new[] { c + V(0, 0.09f, -0.02f), ring + V(0, 0, 0.08f) }, 0.013f, 0.013f, Rgb(255, 200, 110), Glow, 0.004f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + 0.3f;
            b.Ell(head, ring + V(MathF.Cos(a) * 0.08f, 0, MathF.Sin(a) * 0.08f), V(0.022f, 0.022f, 0.022f), Rgb(255, 170, 70), mat: Glow, blend: 0.006f);
        }
        return Lift(b);
    }
}
