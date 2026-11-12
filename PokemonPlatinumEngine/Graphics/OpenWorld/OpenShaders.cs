using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics.OpenWorld;

/// <summary>
/// The open world's own programs (plan 32 · OW8; style guide, "Open world"): the ground, its cover chosen texel by
/// texel from the atlas and lit through three shadow maps; the water, the field water's pixel art banded by depth; and
/// the sky, a pass over the whole picture by the angle over the horizon. The grid's programs (<see cref="FieldShaders"/>)
/// are never changed for them: the player's rig is drawn with the field's character program, lit by the first map.
/// Every uniform's location is asked of <see cref="FieldShaders.Location"/>, which keeps it.
/// </summary>
internal sealed class OpenShaders
{
    public Shader Terrain { get; private set; }
    public Shader Water { get; private set; }
    public Shader Sky { get; private set; }

    /// <summary>What stands on the ground, drawn as it was built (plan 32 · OW9): the buildings, lit through the three maps, its lit glass glowing after dark.</summary>
    public Shader Scenery { get; private set; }

    /// <summary>The same, a mesh drawn many times by a transform each (the near trees, the grass's tufts): <c>Raylib.DrawMeshInstanced</c>.</summary>
    public Shader Instanced { get; private set; }

    /// <summary>The instanced meshes' depth, for the shadow maps.</summary>
    public Shader InstancedDepth { get; private set; }

    /// <summary>The far trees and anyone drawn far off: cards turned to the camera by the program, each showing the one of its eight views nearest the camera's side.</summary>
    public Shader Impostor { get; private set; }

    public bool Loaded { get; private set; }

    // ------------------------------------------------------------------ the ground

    private const string TerrainVertex = @"#version 330
in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexNormal;
in vec4 vertexColor;
uniform mat4 mvp;
uniform mat4 matModel;
out vec3 fragWorld;
out vec3 fragNormal;
out vec3 fragBary;
flat out ivec3 fragSlots;
void main()
{
    fragWorld = (matModel * vec4(vertexPosition, 1.0)).xyz;
    fragNormal = vertexNormal;
    fragBary = vertexColor.rgb;
    int bc = int(vertexTexCoord.y + 0.5);
    fragSlots = ivec3(int(vertexTexCoord.x + 0.5), bc / 16, bc - (bc / 16) * 16);
    gl_Position = mvp * vec4(vertexPosition, 1.0);
}";

    // Three maps from the sun, each fitted round its slice of the view: the slice a point falls in by its depth
    // along the view, the last 15 % of each blended into the next. Where the card compares for itself
    // (HARDWARE_PCF) each tap is its filtered comparison, as the field's map's are
    private const string CascadeFunctions = @"
uniform mat4 lightVP0;
uniform mat4 lightVP1;
uniform mat4 lightVP2;
uniform vec3 cascadeEnds;
uniform vec3 cascadeOffset;
uniform vec3 cascadeBias;
uniform vec3 cascadeTexel;
uniform vec3 viewForward;
uniform int shadowTaps;
const float CascadeBlend = 0.15;
#ifdef HARDWARE_PCF
uniform sampler2DShadow shadow0;
uniform sampler2DShadow shadow1;
uniform sampler2DShadow shadow2;
float Taps(sampler2DShadow map, vec3 p, float texel)
{
    if (shadowTaps < 4) return texture(map, p);
    if (shadowTaps < 9)
    {
        float o = texel * 0.5;
        return 0.25 * (texture(map, vec3(p.x - o, p.y - o, p.z)) + texture(map, vec3(p.x + o, p.y - o, p.z))
                     + texture(map, vec3(p.x - o, p.y + o, p.z)) + texture(map, vec3(p.x + o, p.y + o, p.z)));
    }
    float lit = 0.0;
    for (int j = -1; j <= 1; j++)
        for (int i = -1; i <= 1; i++)
            lit += texture(map, vec3(p.xy + vec2(i, j) * texel, p.z));
    return lit / 9.0;
}
#else
uniform sampler2D shadow0;
uniform sampler2D shadow1;
uniform sampler2D shadow2;
float Taps(sampler2D map, vec3 p, float texel)
{
    float lit = 0.0;
    int n = shadowTaps < 4 ? 1 : 3;
    for (int j = 0; j < n; j++)
        for (int i = 0; i < n; i++)
        {
            vec2 o = (vec2(i, j) - float(n - 1) * 0.5) * texel;
            lit += p.z > texture(map, p.xy + o).r ? 0.0 : 1.0;
        }
    return lit / float(n * n);
}
#endif

float InMap(int i, vec3 world, vec3 n)
{
    vec3 at = world + n * cascadeOffset[i];
    vec4 lp = (i == 0 ? lightVP0 : i == 1 ? lightVP1 : lightVP2) * vec4(at, 1.0);
    vec3 p = lp.xyz / lp.w * 0.5 + 0.5;
    if (p.x <= 0.0 || p.x >= 1.0 || p.y <= 0.0 || p.y >= 1.0 || p.z >= 1.0) return 1.0;
    p.z -= cascadeBias[i];
    if (i == 0) return Taps(shadow0, p, cascadeTexel.x);
    if (i == 1) return Taps(shadow1, p, cascadeTexel.y);
    return Taps(shadow2, p, cascadeTexel.z);
}

float CascadeShadow(vec3 world, vec3 n)
{
    float d = dot(world - viewPos, viewForward);
    if (d >= cascadeEnds.z) return 1.0;
    int i = d < cascadeEnds.x ? 0 : d < cascadeEnds.y ? 1 : 2;
    float end = cascadeEnds[i];
    float start = end * (1.0 - CascadeBlend);
    float s = InMap(i, world, n);
    if (d > start)
    {
        float next = i < 2 ? InMap(i + 1, world, n) : 1.0;
        s = mix(s, next, (d - start) / (end - start));
    }
    return s;
}";

    // The fog, the cloud shade and the mirror's cut, as the field's programs have them (stageWash and stageGrey stay
    // nought: no battle's move lights the open world)
    private const string Shared = @"
uniform vec3 viewPos;
uniform vec3 fogColor;
uniform float fogAmount;
uniform float fogNear;
uniform float fogFar;
uniform float cloudShade;
uniform float cloudTime;
uniform vec2 mirrorClip;
uniform vec4 lamp;
uniform vec3 lampColour;
uniform float lampReach;

vec3 ApplyFog(vec3 color, vec3 world)
{
    // The air between: thickening with the distance as haze does (never a wall of it), lying thicker low down, and
    // brighter and warmer looking toward the sun
    vec3 to = world - viewPos;
    float d = length(to);
    float low = 1.0 + 0.6 * clamp(1.0 - (world.y - viewPos.y + 20.0) / 80.0, 0.0, 1.0);
    float f = (1.0 - exp(-max(d - fogNear * 0.25, 0.0) * low / (fogFar * 0.62))) * fogAmount;
    float toSun = pow(max(dot(to / max(d, 0.001), sunDir), 0.0), 6.0);
    return mix(color, fogColor + sunColor * (0.16 * toSun), f);
}

float CloudLight(vec3 world)
{
    if (cloudShade <= 0.0) return 1.0;
    vec2 p = world.xz * 0.012 + vec2(cloudTime * 0.016, cloudTime * 0.009);
    float n = sin(p.x * 3.1 + sin(p.y * 2.3) * 1.7) * 0.5 + sin(p.y * 2.7 + sin(p.x * 1.9) * 1.3) * 0.5;
    return 1.0 - cloudShade * smoothstep(-0.2, 0.7, n);
}

// The light about the player under rock: falling to nothing at its reach, by its square
vec3 Lamp(vec3 world, vec3 n)
{
    if (lamp.w <= 0.0) return vec3(0.0);
    vec3 l = lamp.xyz - world;
    float d = length(l);
    float fall = clamp(1.0 - d / lampReach, 0.0, 1.0);
    fall *= fall;
    float facing = max(dot(n, l / max(d, 0.001)), 0.0) * 0.75 + 0.25;
    return lampColour * fall * facing * lamp.w;
}

float Hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
float Hash3(vec3 p) { return fract(sin(dot(p, vec3(127.1, 311.7, 74.7))) * 43758.5453); }";

    // The ground (style guide, "Open world", "Ground"): each cover a material worked out from the noise sheet read at
    // three sizes in the world (so nothing repeats to the eye), laid by the face's own axes and blended between them,
    // the covers of a triangle's corners meeting along a ragged, soft border, and the surface's grain felt by the light.
    // The slots are the atlas's order (OpenCoverArt.Order): 0 to 3 the grasses and the highland, 4 mud, 5 the path,
    // 6 sand, 7 the cave's floor, 8 and 9 the rocks, 10 snow, 11 climbable rock
    private const string TerrainFragment = @"#version 330
in vec3 fragWorld;
in vec3 fragNormal;
in vec3 fragBary;
flat in ivec3 fragSlots;
uniform sampler2D texture0;
uniform vec3 coverBase[16];
uniform vec3 coverPatch[16];
uniform vec3 coverDark[16];
uniform vec3 coverLight[16];
uniform vec3 sunDir;
uniform vec3 sunColor;
uniform vec3 skyAmbient;
uniform vec3 groundAmbient;
uniform float worldRamp;
out vec4 finalColor;
" + Shared + CascadeFunctions + @"
// How many metres the sheet covers at each of its three sizes: no two a whole multiple of one another
const float Fine = 1.0 / 3.1;
const float Mid = 1.0 / 23.0;
const float Macro = 1.0 / 190.0;

// The sheet by the face's own axes. Level ground (most of any picture) reads it once, from above; only a face that
// leans reads the two sides too, with the slopes worked out beforehand (no slope may be asked inside a branch)
vec4 Sheet(vec3 p, vec3 w, float s, vec3 dx, vec3 dy)
{
    vec4 top = textureGrad(texture0, p.xz * s, dx.xz * s, dy.xz * s);
    if (w.y > 0.985) return top;
    return top * w.y + textureGrad(texture0, p.zy * s, dx.zy * s, dy.zy * s) * w.x
                     + textureGrad(texture0, p.xy * s, dx.xy * s, dy.xy * s) * w.z;
}

// One cover's colour at a point, and how rough it is to the light
vec3 Cover(int slot, vec4 fine, vec4 mid, vec4 macro, float steep, vec3 p, out float rough)
{
    vec3 base = coverBase[slot], patch = coverPatch[slot], dark = coverDark[slot], light = coverLight[slot];
    vec3 c = mix(base, patch, smoothstep(0.40, 0.62, mid.r * 0.7 + fine.g * 0.3));
    if (slot <= 3)
    {
        // Grass: clumps with shade between them and lit tips, and sweeps of drier grass over tens of metres
        c = mix(c, dark, smoothstep(0.30, 0.70, fine.b) * 0.34);
        c = mix(c, light, smoothstep(0.70, 0.95, fine.a) * 0.24);
        c = mix(c, c * vec3(1.12, 1.03, 0.78), smoothstep(0.50, 0.85, macro.g) * 0.45);
        c *= 0.90 + 0.20 * macro.r;
        rough = 0.45;
    }
    else if (slot == 6)
    {
        // Sand: ripples across the wind where it lies level, and a slow change of tone
        float ripple = sin((p.x * 0.55 + p.z * 0.83) * 7.0 + mid.r * 9.0 + fine.g * 1.5) * 0.5 + 0.5;
        c = mix(c, dark, smoothstep(0.55, 0.95, ripple) * 0.30 * (1.0 - steep));
        c = mix(c, light, smoothstep(0.80, 0.97, fine.a) * 0.20);
        c *= 0.93 + 0.14 * macro.r;
        rough = 0.30;
    }
    else if (slot == 10)
    {
        // Snow: drifts with blue hollows, and a glint here and there
        c = mix(base, patch, smoothstep(0.30, 0.70, mid.r));
        c = mix(c, dark, smoothstep(0.55, 0.90, mid.a * 0.7 + fine.r * 0.3) * 0.45);
        c = mix(c, vec3(0.94, 0.96, 1.0), (1.0 - smoothstep(0.015, 0.04, fine.b)) * 0.45);
        rough = 0.22;
    }
    else if (slot >= 8)
    {
        // Rock: on a face, strata of their own tone with a crease under each; cracks and chips everywhere
        float beds = slot == 11 ? 2.6 : 1.3;
        float s = p.y * beds + mid.r * 2.2 + macro.g * 4.0;
        float f = fract(s);
        float tone = Hash(vec2(floor(s), float(slot)));
        c = mix(c, mix(base * 0.84, patch * 1.06, tone), steep * 0.85);
        c = mix(c, dark, (1.0 - smoothstep(0.0, 0.10, f)) * steep * 0.65);
        c = mix(c, light, smoothstep(0.90, 1.0, f) * steep * (slot == 11 ? 0.5 : 0.2));
        c = mix(c, dark, smoothstep(0.84, 0.99, fine.a) * smoothstep(0.35, 0.65, mid.g) * 0.34);
        c = mix(c, light, smoothstep(0.76, 0.95, fine.r) * 0.26);
        c *= 0.86 + 0.28 * macro.r;
        rough = 1.0;
    }
    else
    {
        // The path, mud and a cave's floor: pebbles strewn in drifts, worn darker here and there; mud lies wet in its hollows
        float pebble = (1.0 - smoothstep(0.10, 0.20, fine.b)) * smoothstep(0.42, 0.58, mid.g * 0.7 + fine.r * 0.3);
        c = mix(c, mix(dark, light, smoothstep(0.3, 0.7, fine.r)), pebble * 0.7);
        c = mix(c, dark, smoothstep(0.62, 0.92, slot == 4 ? mid.a : fine.a) * (slot == 4 ? 0.55 : 0.16));
        c *= 0.91 + 0.18 * macro.r;
        rough = slot == 4 ? 0.40 : 0.65;
    }
    return c;
}

// How far a cover reaches into its neighbours, from the noise by its slot: the same on every triangle
float Push(int slot, vec4 fine, vec4 mid)
{
    float k = fract(float(slot) * 0.618);
    return (mix(fine.g, 1.0 - fine.b, k) - 0.5) * 0.5 + (mid.a - 0.5) * 0.3;
}

void main()
{
    if (mirrorClip.x > 0.5 && fragWorld.y < mirrorClip.y) discard;
    vec3 n = normalize(fragNormal);
    vec3 w = pow(abs(n), vec3(4.0));
    w /= (w.x + w.y + w.z);
    vec3 wdx = dFdx(fragWorld), wdy = dFdy(fragWorld);
    // (what the water mirrors is seen bent and small: it needs the land's colours, not its grain)
    bool mirrored = mirrorClip.x > 0.5;
    vec4 fine = mirrored ? vec4(0.5) : Sheet(fragWorld, w, Fine, wdx, wdy);
    vec4 mid = Sheet(fragWorld, w, Mid, wdx, wdy);
    vec4 macro = texture(texture0, fragWorld.xz * Macro);
    float steep = 1.0 - w.y;

    // The corners' covers, those of one cover summed, each pushed by the noise and the greatest winning softly
    ivec3 c = fragSlots;
    vec3 b = fragBary;
    if (c.y == c.x) { b.x += b.y; b.y = -9.0; }
    if (c.z == c.x) { b.x += b.z; b.z = -9.0; }
    else if (c.z == c.y && b.y > -1.0) { b.y += b.z; b.z = -9.0; }
    b += vec3(Push(c.x, fine, mid), Push(c.y, fine, mid), Push(c.z, fine, mid));
    float most = max(b.x, max(b.y, b.z));
    vec3 share = max(b - most + 0.16, 0.0);
    share /= (share.x + share.y + share.z);

    vec3 albedo = vec3(0.0);
    float rough = 0.0, r;
    if (share.x > 0.0) { albedo += Cover(c.x, fine, mid, macro, steep, fragWorld, r) * share.x; rough += r * share.x; }
    if (share.y > 0.0) { albedo += Cover(c.y, fine, mid, macro, steep, fragWorld, r) * share.y; rough += r * share.y; }
    if (share.z > 0.0) { albedo += Cover(c.z, fine, mid, macro, steep, fragWorld, r) * share.z; rough += r * share.z; }

    // The grain as the light feels it: the surface's height from the noise, its slope across the picture turned
    // into a lean of the normal (far off the sheet's mip levels flatten it, so the distance is smooth)
    float h = fine.r * 0.5 + (1.0 - fine.b) * 0.3 + mid.r * 0.2;
    vec3 sx = dFdx(fragWorld), sy = dFdy(fragWorld);
    vec3 r1 = cross(sy, n), r2 = cross(n, sx);
    float det = dot(sx, r1);
    vec3 lean = sign(det) * (dFdx(h) * r1 + dFdy(h) * r2);
    vec3 nb = mirrored ? n : normalize(abs(det) * n - lean * (0.09 * rough));
    albedo *= 1.0 - rough * 0.16 * (0.5 - h);

    float ndl = dot(n, sunDir);
    float shadow = ndl > 0.0 ? CascadeShadow(fragWorld, n) : 0.0;
    vec3 ambient = mix(groundAmbient, skyAmbient, nb.y * 0.5 + 0.5);
    float diffuse = mix(max(ndl, 0.0), smoothstep(0.0, 0.3, ndl) * 0.92 + max(ndl, 0.0) * 0.08, worldRamp);
    diffuse *= clamp(1.0 + (dot(nb, sunDir) - ndl) * 1.5, 0.5, 1.4);
    vec3 light = ambient + sunColor * diffuse * shadow * CloudLight(fragWorld) + Lamp(fragWorld, nb);
    finalColor = vec4(ApplyFog(albedo * light, fragWorld), 1.0);
}";

    // ------------------------------------------------------------------ the water

    private const string WaterVertex = @"#version 330
in vec3 vertexPosition;
in vec2 vertexTexCoord;
uniform mat4 mvp;
uniform mat4 matModel;
out vec3 fragWorld;
out float fragDepth;
void main()
{
    fragWorld = (matModel * vec4(vertexPosition, 1.0)).xyz;
    fragDepth = vertexTexCoord.x;
    gl_Position = mvp * vec4(vertexPosition, 1.0);
}";

    // The water (style guide, "Open world", "Water"): its colour by the depth under it, eased from the shallows to the
    // deep; two layers of ripples from the noise sheet drifting across one another, which bend what it mirrors, catch
    // the sun and take the sky's colour toward the horizon; foam breaking along the shore
    private const string WaterFragment = @"#version 330
in vec3 fragWorld;
in float fragDepth;
uniform sampler2D texture0;
uniform sampler2D texture1;
uniform mat4 mirrorVP;
uniform vec4 mirror;
uniform vec3 sunDir;
uniform vec3 sunColor;
uniform vec3 skyAmbient;
uniform float time;
out vec4 finalColor;
" + Shared + CascadeFunctions + @"
const vec3 deep = vec3(58.0, 118.0, 190.0) / 255.0;
const vec3 middle = vec3(76.0, 146.0, 214.0) / 255.0;
const vec3 shallow = vec3(116.0, 184.0, 232.0) / 255.0;
const vec3 foam = vec3(236.0, 246.0, 255.0) / 255.0;

float Swell(vec2 p)
{
    return texture(texture0, p * 0.085 + vec2(time * 0.011, time * 0.006)).r * 0.6
         + texture(texture0, p * 0.23 - vec2(time * 0.016, -time * 0.012)).g * 0.4;
}

void main()
{
    float depth = fragDepth;
    if (depth < 0.0) discard;
    vec2 p = fragWorld.xz;

    // The surface's lean from the swell's height a little to the east and to the south
    float h = Swell(p);
    vec3 n = normalize(vec3((h - Swell(p + vec2(0.18, 0.0))) * 2.2, 1.0, (h - Swell(p + vec2(0.0, 0.18))) * 2.2));
    vec4 grain = texture(texture0, p * 0.31 + vec2(time * 0.02, 0.0));

    vec3 color = mix(mix(shallow, middle, smoothstep(0.05, 0.7, depth)), deep, smoothstep(0.6, 2.4, depth));
    color *= 0.94 + 0.12 * h;

    float shadow = CascadeShadow(fragWorld, vec3(0.0, 1.0, 0.0));
    float sun = max(sunDir.y, 0.0) * shadow * CloudLight(fragWorld);
    const vec3 luma = vec3(0.3, 0.5, 0.2);
    float daylight = smoothstep(0.6, 0.95, dot(skyAmbient + sunColor * max(sunDir.y, 0.0), luma));

    // Foam: breaking in and out along the shore, torn by the noise
    float reach = 0.10 + 0.05 * sin(time * 1.3 + grain.a * 6.0);
    float shore = (1.0 - smoothstep(reach * 0.4, reach, depth)) * smoothstep(0.25, 0.55, grain.b + grain.r * 0.4);
    shore = max(shore, (1.0 - smoothstep(0.0, 0.035, depth)) * 0.9);
    shore += (1.0 - smoothstep(0.0, 0.05, abs(depth - 0.26 - 0.06 * sin(time * 0.8 + h * 5.0)))) * smoothstep(0.55, 0.75, grain.g) * 0.45;
    color = mix(color, foam, clamp(shore, 0.0, 1.0));

    // After dark the water is darker and greyer, as the field's
    color = mix(mix(vec3(dot(color, luma)), color, 0.55) * 0.62, color, daylight);
    color *= skyAmbient + sunColor * sun + Lamp(fragWorld, vec3(0.0, 1.0, 0.0));

    vec3 toEye = normalize(viewPos - fragWorld);
    float glance = pow(1.0 - max(dot(n, toEye), 0.0), 4.0);
    float open = 1.0 - clamp(shore, 0.0, 1.0);

    // What stands over the water, mirrored and bent by the ripples: in the water at the mirror's height alone,
    // fading out from 0.1 m off it to 0.25 m, so a sloping river shows no line where its mirror ends
    float off = abs(fragWorld.y - mirror.y);
    if (mirror.x > 0.5)
    {
        vec3 at = vec3(fragWorld.x + n.x * 1.6, mirror.y, fragWorld.z + n.z * 1.6);
        vec4 m = mirrorVP * vec4(at, 1.0);
        vec2 uv = m.xy / m.w * 0.5 + 0.5;
        if (m.w > 0.0 && uv.x >= 0.0 && uv.y >= 0.0 && uv.x <= 1.0 && uv.y <= 1.0)
        {
            vec4 seen = texture(texture1, uv);
            float much = seen.a * (0.30 + 0.45 * glance) * (1.0 - smoothstep(0.1, 0.25, off)) * smoothstep(0.05, 0.4, depth) * open;
            color = mix(color, seen.rgb * 0.9, much);
        }
    }

    // The sky's own colour toward the horizon, and the sun caught on the ripples
    color = mix(color, fogColor, glance * 0.45 * open);
    vec3 halfway = normalize(sunDir + toEye);
    color += sunColor * pow(max(dot(n, halfway), 0.0), 220.0) * sun * daylight * open * 0.9;
    finalColor = vec4(ApplyFog(color, fragWorld), 1.0);
}";

    // ------------------------------------------------------------------ the sky

    private const string SkyVertex = @"#version 330
in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec4 vertexColor;
uniform mat4 mvp;
void main()
{
    gl_Position = mvp * vec4(vertexPosition, 1.0);
}";

    private const string SkyFragment = @"#version 330
uniform mat4 invViewProj;
uniform vec2 viewport;
uniform vec3 eye;
uniform vec3 zenith;
uniform vec3 middle;
uniform vec3 horizon;
uniform vec3 cloudTint;
uniform vec3 sunDir;
uniform vec3 sunColor;
uniform float stars;
uniform float time;
out vec4 finalColor;

float Hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
float Noise(vec2 p)
{
    vec2 i = floor(p), f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = Hash(i), b = Hash(i + vec2(1.0, 0.0)), c = Hash(i + vec2(0.0, 1.0)), d = Hash(i + vec2(1.0, 1.0));
    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

void main()
{
    vec2 ndc = gl_FragCoord.xy / viewport * 2.0 - 1.0;
    vec4 far = invViewProj * vec4(ndc, 1.0, 1.0);
    vec3 dir = normalize(far.xyz / far.w - eye);
    float e = dir.y;
    // The horizon's colour at the horizon (and under it: the fog's), the middle by 10°, the zenith from 50°
    vec3 color = mix(horizon, middle, smoothstep(0.0, 0.1736, e));
    color = mix(color, zenith, smoothstep(0.1736, 0.766, e));

    // Stars after dark: one cell in 60 of a grid of half a degree, a texel or two each, twinkling, from 1° up
    if (stars > 0.0 && e > 0.015)
    {
        float az = atan(dir.x, dir.z) * 57.29578, el = asin(clamp(e, -1.0, 1.0)) * 57.29578;
        vec2 cell = floor(vec2(az, el) / 0.5);
        float h = Hash(cell);
        if (h > 1.0 - 1.0 / 60.0)
        {
            vec2 at = (cell + vec2(Hash(cell + 3.0), Hash(cell + 9.0)) * 0.8 + 0.1) * 0.5;
            if (abs(az - at.x) * cos(el / 57.29578) < 0.06 && abs(el - at.y) < 0.06)
            {
                float twinkle = 0.65 + 0.35 * sin(time * (1.5 + Hash(cell + 21.0) * 2.0) + h * 40.0);
                color = mix(color, vec3(1.0, 0.98, 0.92), twinkle * stars * smoothstep(0.015, 0.05, e));
            }
        }
    }

    // The sun (after dark, the moon): its disc, the glare close round it and a wide glow in the air toward it
    float toSun = max(dot(dir, sunDir), 0.0);
    float bright = max(sunColor.r, max(sunColor.g, sunColor.b));
    vec3 sunTint = sunColor / max(bright, 0.001);
    float up = smoothstep(-0.02, 0.05, sunDir.y);
    color += sunTint * (pow(toSun, 8.0) * 0.10 + pow(toSun, 90.0) * 0.30) * min(bright, 1.0) * up;
    // (a sun the weather has taken shows no disc)
    float disc = smoothstep(0.99955, 0.99975, toSun) * up * smoothstep(0.3, 0.7, bright);

    // Clouds: a layer 600 m up, five octaves of value noise pulled about by another, soft-edged, their thick middles
    // shaded underneath and their edges toward the sun lit, drifting east, thinning into the haze near the horizon
    float cover = 0.0;
    if (e > 0.005)
    {
        float reach = (600.0 - eye.y) / e;
        vec2 p = eye.xz + dir.xz * reach;
        vec2 q = (p + vec2(time * 4.0, 0.0)) * 0.0018;
        q += (vec2(Noise(q * 1.7 + 5.0), Noise(q * 1.7 + 31.0)) - 0.5) * 0.55;
        float n = Noise(q) * 0.50 + Noise(q * 2.03 + 11.0) * 0.26 + Noise(q * 4.1 + 23.0) * 0.13
                + Noise(q * 8.3 + 47.0) * 0.07 + Noise(q * 16.9 + 71.0) * 0.04;
        // (the same a little toward the sun: where it is thinner there, this side is lit)
        vec2 s = q + normalize(sunDir.xz + vec2(0.0001, 0.0)) * 0.09;
        float toward = Noise(s) * 0.50 + Noise(s * 2.03 + 11.0) * 0.26 + Noise(s * 4.1 + 23.0) * 0.13 + 0.055;
        float cloud = smoothstep(0.50, 0.72, n) * smoothstep(0.0, 0.10, e);
        float lit = clamp(0.62 + (n - toward) * 3.2, 0.0, 1.0);
        vec3 shade = mix(cloudTint, middle, 0.45) * 0.86;
        vec3 body = mix(shade, cloudTint, lit) + sunTint * pow(toSun, 12.0) * 0.18 * (1.0 - smoothstep(0.55, 0.8, n)) * up;
        cover = cloud * 0.94;
        color = mix(color, body, cover);
    }
    color = mix(color, sunTint * 1.6 + 0.2, disc * (1.0 - cover));
    finalColor = vec4(color, 1.0);
}";

    // ------------------------------------------------------------------ what stands on the ground (plan 32 · OW9)

    // Static scenery is drawn with an identity model (its places are in its vertices), and a mesh drawn many times is
    // given a transform each (raylib's DrawMeshInstanced binds them to instanceTransform, whose place is pinned so every
    // program agrees on it). Vertex alpha under 1 sways in the wind, and what sways fully (a tuft's tips) parts round
    // whoever walks through it, as the field's grass does; both offsets are worked out in the world
    private const string SceneryVertex = @"#version 330
in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexNormal;
in vec4 vertexColor;
#ifdef INSTANCED
layout(location = 9) in mat4 instanceTransform;
#endif
uniform mat4 mvp;
uniform mat4 matModel;
uniform float time;
uniform float wind;
uniform float still;
uniform vec4 walkers[8];
uniform int walkerCount;
out vec2 fragTexCoord;
out vec4 fragColor;
out vec3 fragNormal;
out vec3 fragWorld;

vec3 SwayOffset(vec3 p, float weight)
{
    if (weight <= 0.0 || still >= 1.0) return vec3(0.0);
    float t = time * 1.6 + p.x * 0.42 + p.z * 0.27;
    float gust = 0.5 + 0.5 * sin(time * 0.52 - p.x * 0.07 - p.z * 0.03);
    gust *= gust;
    float strength = 0.75 + 0.6 * gust + wind * (0.6 + gust);
    return vec3(sin(t) * 0.07 * strength + 0.05 * gust * (0.5 + wind), 0.0, cos(t * 0.83) * 0.035 * strength) * weight;
}

vec3 PartOffset(vec3 p, float weight)
{
    vec3 offset = vec3(0.0);
    float bend = smoothstep(0.7, 1.0, weight);
    if (bend <= 0.0) return offset;
    for (int i = 0; i < walkerCount; i++)
    {
        vec2 d = p.xz - walkers[i].xz;
        float k = 1.0 - smoothstep(0.25, 0.75, length(d));
        if (k <= 0.0) continue;
        vec2 away = normalize(d + vec2(0.0001, 0.0));
        offset += vec3(away.x * 0.22, -0.16, away.y * 0.22) * k * bend;
    }
    return offset;
}

void main()
{
#ifdef INSTANCED
    mat4 model = instanceTransform;
#else
    mat4 model = matModel;
#endif
    vec3 world = (model * vec4(vertexPosition, 1.0)).xyz;
    float weight = 1.0 - vertexColor.a;
    vec3 sway = SwayOffset(world, weight) + PartOffset(world, weight);
    fragTexCoord = vertexTexCoord;
    fragColor = vec4(vertexColor.rgb, 1.0);
    fragNormal = normalize(mat3(model) * vertexNormal);
    fragWorld = world + sway;
#ifdef INSTANCED
    gl_Position = mvp * vec4(fragWorld, 1.0);
#else
    gl_Position = mvp * vec4(vertexPosition + sway, 1.0);
#endif
}";

    // Lit as the ground is (the battle scenery's band, the three maps' shadows, the cloud shade, the light about the
    // player under rock, the fog), and glass marked lit in the art's alpha glowing after dark as the field's does
    private const string SceneryFragment = @"#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
in vec3 fragNormal;
in vec3 fragWorld;
uniform sampler2D texture0;
#ifdef GRAIN
uniform sampler2D texture1;
#endif
uniform vec4 colDiffuse;
uniform vec3 sunDir;
uniform vec3 sunColor;
uniform vec3 skyAmbient;
uniform vec3 groundAmbient;
uniform float worldRamp;
uniform float glowPublic;
uniform float glowHome;
uniform vec3 glowColor;
out vec4 finalColor;
" + Shared + CascadeFunctions + @"
void main()
{
    if (mirrorClip.x > 0.5 && fragWorld.y < mirrorClip.y) discard;
    vec4 texel = texture(texture0, fragTexCoord);
    if (texel.a < 0.5) discard;
    vec3 albedo = texel.rgb * colDiffuse.rgb * fragColor.rgb;
    vec3 n = normalize(fragNormal);
#ifdef TWO_SIDED
    // A building is seen from every side: the underside of an eave, seen from below, is lit as facing down
    if (!gl_FrontFacing) n = -n;
#endif
#ifdef GRAIN
    // A built surface is no flat paint: the grain of its stone, plaster or tiles from the noise sheet laid by the
    // face's own axes, rain's streaks down a wall, and that grain felt by the light
    vec3 gw = pow(abs(n), vec3(4.0));
    gw /= (gw.x + gw.y + gw.z);
    vec4 grit = texture(texture1, fragWorld.xz * 0.43) * gw.y + texture(texture1, fragWorld.zy * 0.43) * gw.x + texture(texture1, fragWorld.xy * 0.43) * gw.z;
    vec4 wear = texture(texture1, fragWorld.xz * 0.06) * gw.y + texture(texture1, vec2(fragWorld.z * 0.7, fragWorld.y * 0.045)) * gw.x + texture(texture1, vec2(fragWorld.x * 0.7, fragWorld.y * 0.045)) * gw.z;
    albedo *= (0.89 + 0.14 * grit.r + 0.07 * (1.0 - grit.b)) * (0.93 + 0.12 * wear.g);
    albedo = mix(albedo, albedo * vec3(0.72, 0.70, 0.66), smoothstep(0.70, 0.95, wear.a) * (1.0 - gw.y) * 0.22);
    float gh = grit.r * 0.6 + (1.0 - grit.b) * 0.4;
    vec3 gsx = dFdx(fragWorld), gsy = dFdy(fragWorld);
    vec3 gr1 = cross(gsy, n), gr2 = cross(n, gsx);
    float gdet = dot(gsx, gr1);
    n = normalize(abs(gdet) * n - sign(gdet) * (dFdx(gh) * gr1 + dFdy(gh) * gr2) * 0.035);
#endif
    float ndl = dot(n, sunDir);
    float shadow = ndl > 0.0 ? CascadeShadow(fragWorld, n) : 0.0;
    vec3 ambient = mix(groundAmbient, skyAmbient, n.y * 0.5 + 0.5);
    float diffuse = mix(max(ndl, 0.0), smoothstep(0.0, 0.3, ndl) * 0.92 + max(ndl, 0.0) * 0.08, worldRamp);
#ifdef LEAVES
    // Leaves and blades let light through and stand among one another: the sun wraps round them, a third of it
    // comes through whichever way they face, and their own shade on one another is never black
    diffuse = max(ndl * 0.6 + 0.4, 0.0) * 0.72 + 0.28;
    shadow = mix(0.42, 1.0, CascadeShadow(fragWorld, n));
#endif
    vec3 color = albedo * (ambient + sunColor * diffuse * shadow * CloudLight(fragWorld) + Lamp(fragWorld, n));
    if (texel.a < 0.95)
    {
        float amount = texel.a > 0.7 ? glowPublic : glowHome;
        float tone = dot(texel.rgb, vec3(0.3, 0.5, 0.2));
        color = mix(color, glowColor * (0.62 + 0.5 * tone), amount);
    }
    finalColor = vec4(ApplyFog(color, fragWorld), 1.0);
}";

    // The instanced meshes into the shadow maps: their depth alone, with what is cut out of the art cut out
    private const string DepthFragment = @"#version 330
in vec2 fragTexCoord;
uniform sampler2D texture0;
out vec4 finalColor;
void main()
{
    if (texture(texture0, fragTexCoord).a < 0.5) discard;
    finalColor = vec4(1.0);
}";

    // A card standing on its foot (vertexPosition) and turned to the camera by the program: its corner across and up in
    // the first texture coordinate, its width, height and row of the atlas in the normal, the way the thing faces in the
    // colour's alpha (a whole turn in 255ths: a tree's 255 is as good as nought). It shows the one of eight views (a turn
    // of an eighth each, as they were baked, round the thing facing south) nearest the camera's side of it, and nothing
    // nearer the camera than reach.x, where the thing itself is drawn
    private const string ImpostorVertex = @"#version 330
in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexNormal;
in vec4 vertexColor;
uniform mat4 mvp;
uniform vec3 eye;
uniform vec2 reach;
uniform vec2 atlasCells;
out vec2 fragTexCoord;
out vec3 fragWorld;
out vec4 fragColor;
out float fragShow;
void main()
{
    vec3 foot = vertexPosition;
    vec2 to = eye.xz - foot.xz;
    float d = length(to);
    vec2 t = d > 0.001 ? to / d : vec2(0.0, 1.0);
    vec3 right = vec3(t.y, 0.0, -t.x);
    float way = vertexColor.a * 6.2831853;
    float view = mod(floor((atan(t.x, t.y) - way) / 0.78539816 + 0.5) + 16.0, 8.0);
    float w = vertexNormal.x, h = vertexNormal.y, row = vertexNormal.z;
    fragTexCoord = vec2((view + vertexTexCoord.x) / atlasCells.x, (row + 1.0 - vertexTexCoord.y) / atlasCells.y);
    vec3 p = foot + right * ((vertexTexCoord.x - 0.5) * w) + vec3(0.0, vertexTexCoord.y * h, 0.0);
    fragShow = d >= reach.x ? 1.0 : 0.0;
    fragWorld = p;
    fragColor = vec4(vertexColor.rgb, 1.0);
    gl_Position = mvp * vec4(fragShow > 0.5 ? p : foot, 1.0);
}";

    // Lit as the field's sprites are (the sky's light and the sun's, no shade of their own: their bake has it), and fogged
    private const string ImpostorFragment = @"#version 330
in vec2 fragTexCoord;
in vec3 fragWorld;
in vec4 fragColor;
in float fragShow;
uniform sampler2D texture0;
uniform vec3 sunDir;
uniform vec3 sunColor;
uniform vec3 skyAmbient;
out vec4 finalColor;
" + Shared + @"
void main()
{
    if (fragShow < 0.5) discard;
    vec4 texel = texture(texture0, fragTexCoord);
    if (texel.a < 0.5) discard;
    float sun = max(sunDir.y, 0.0);
    vec3 light = skyAmbient * 0.7 + sunColor * (0.35 + 0.45 * sun) * CloudLight(fragWorld);
    finalColor = vec4(ApplyFog(texel.rgb * fragColor.rgb * light, fragWorld), 1.0);
}";

    /// <summary>A program that reads the shadow maps, compiled for the way this machine filters them (as the field's are).</summary>
    private static string Shadowed(string fragment) =>
        FieldShaders.HardwareShadows ? fragment.Replace("#version 330", "#version 330\n#define HARDWARE_PCF") : fragment;

    /// <summary>The vertex program of a mesh drawn many times, a transform to each.</summary>
    private static string InstancedOf(string vertex) => vertex.Replace("#version 330", "#version 330\n#define INSTANCED");

    public void Load()
    {
        if (Loaded) return;
        Terrain = Gpu.ShaderFrom(TerrainVertex, Shadowed(TerrainFragment), "shader");
        Water = Gpu.ShaderFrom(WaterVertex, Shadowed(WaterFragment), "shader");
        Sky = Gpu.ShaderFrom(SkyVertex, SkyFragment, "shader");
        Scenery = Gpu.ShaderFrom(SceneryVertex, Shadowed(SceneryFragment).Replace("#version 330", "#version 330\n#define TWO_SIDED\n#define GRAIN"), "shader");
        Instanced = Gpu.ShaderFrom(InstancedOf(SceneryVertex), Shadowed(SceneryFragment).Replace("#version 330", "#version 330\n#define LEAVES"), "shader");
        InstancedDepth = Gpu.ShaderFrom(InstancedOf(SceneryVertex), DepthFragment, "shader");
        Impostor = Gpu.ShaderFrom(ImpostorVertex, ImpostorFragment, "shader");
        foreach (var shader in new[] { Terrain, Water, Scenery, Instanced })
            for (int i = 0; i < OpenCascades.Count; i++) SetInt(shader, $"shadow{i}", OpenCascades.Slots[i]);
        // The covers' colours, which the ground's program keeps (they never change)
        var (bases, patches, darks, lights) = OpenGroundArt.Palette();
        SetColours("coverBase", bases);
        SetColours("coverPatch", patches);
        SetColours("coverDark", darks);
        SetColours("coverLight", lights);
        Loaded = true;
    }

    public void Unload()
    {
        if (!Loaded) return;
        Gpu.Unload(Terrain);
        Gpu.Unload(Water);
        Gpu.Unload(Sky);
        Gpu.Unload(Scenery);
        Gpu.Unload(Instanced);
        Gpu.Unload(InstancedDepth);
        Gpu.Unload(Impostor);
        Loaded = false;
    }

    private Shader[] Lit => new[] { Terrain, Water, Scenery, Instanced, Impostor };

    /// <summary>What sways and parts (plan 32 · OW9): the time, the wind, the options' REDUCE MOTION and up to eight walkers' feet.</summary>
    public void SetMotion(float time, float wind, bool still, System.Collections.Generic.IReadOnlyList<Vector3> walkers)
    {
        int count = System.Math.Min(walkerBuffer.Length, walkers.Count);
        for (int i = 0; i < count; i++) walkerBuffer[i] = new Vector4(walkers[i], 1f);
        foreach (var shader in new[] { Scenery, Instanced, InstancedDepth })
        {
            Set(shader, "time", time);
            Set(shader, "wind", wind);
            Set(shader, "still", still ? 1f : 0f);
            SetInt(shader, "walkerCount", count);
            int loc = FieldShaders.Location(shader, "walkers");
            if (loc >= 0 && count > 0) Raylib.SetShaderValueV(shader, loc, walkerBuffer, ShaderUniformDataType.Vec4, count);
        }
    }

    private readonly Vector4[] walkerBuffer = new Vector4[8];

    /// <summary>The glow of lit glass after dark (plan 32 · OW9): lights that burn all night, the windows of homes, and their colour.</summary>
    public void SetGlow(float publicLevel, float homeLevel, Vector3 colour)
    {
        Set(Scenery, "glowPublic", publicLevel);
        Set(Scenery, "glowHome", homeLevel);
        Set(Scenery, "glowColor", colour);
        Set(Instanced, "glowPublic", 0f);
        Set(Instanced, "glowHome", 0f);
    }

    /// <summary>Where the camera is, how near it the impostors give way to the things themselves, and how many views and rows their atlas has.</summary>
    public void SetImpostors(Vector3 eye, float reach, int views, int rows)
    {
        Set(Impostor, "eye", eye);
        Set(Impostor, "reach", new Vector2(reach, 0f));
        Set(Impostor, "atlasCells", new Vector2(views, rows));
    }

    /// <summary>The sun and the sky's light, the camera's place and way, and the ground's band (the battle scenery's).</summary>
    public void SetLight(SceneLighting light, Vector3 eye, Vector3 forward, float worldRamp)
    {
        foreach (var shader in Lit)
        {
            Set(shader, "sunDir", light.SunDirection);
            Set(shader, "sunColor", light.SunColor);
            Set(shader, "skyAmbient", light.SkyAmbient);
            Set(shader, "groundAmbient", light.GroundAmbient);
            Set(shader, "viewPos", eye);
            Set(shader, "viewForward", Vector3.Normalize(forward));
        }
        foreach (var shader in new[] { Terrain, Scenery, Instanced }) Set(shader, "worldRamp", worldRamp);
    }

    public void SetFog(Vector3 colour, float amount, float near, float far, float cloudShade, float time)
    {
        foreach (var shader in Lit)
        {
            Set(shader, "fogColor", colour);
            Set(shader, "fogAmount", amount);
            Set(shader, "fogNear", near);
            Set(shader, "fogFar", far);
            Set(shader, "cloudShade", cloudShade);
            Set(shader, "cloudTime", time);
        }
        Set(Water, "time", time);
    }

    /// <summary>The three maps' cameras, their slices' ends, their offsets and biases in their own depth, their texels, and the preset's taps.</summary>
    public void SetCascades(Matrix4x4[] lightVP, Vector3 ends, Vector3 offsets, Vector3 biases, Vector3 texels, int taps)
    {
        foreach (var shader in Lit)
        {
            for (int i = 0; i < OpenCascades.Count; i++) SetMatrix(shader, $"lightVP{i}", lightVP[i]);
            Set(shader, "cascadeEnds", ends);
            Set(shader, "cascadeOffset", offsets);
            Set(shader, "cascadeBias", biases);
            Set(shader, "cascadeTexel", texels);
            SetInt(shader, "shadowTaps", taps);
        }
    }

    /// <summary>The light about the player under rock (strength nought for none).</summary>
    public void SetLamp(Vector3 at, float strength, Vector3 colour, float reach)
    {
        foreach (var shader in Lit)
        {
            Set(shader, "lamp", new Vector4(at, strength));
            Set(shader, "lampColour", colour);
            Set(shader, "lampReach", reach);
        }
    }

    /// <summary>The mirror pass's cut: nothing under the plane is drawn while it runs (off: nought).</summary>
    public void SetMirrorClip(bool on, float plane)
    {
        foreach (var shader in new[] { Terrain, Scenery, Instanced }) Set(shader, "mirrorClip", on ? new Vector2(1f, plane) : Vector2.Zero);
    }

    /// <summary>What the water reads of the mirror: whether it mirrors, the plane and the mirrored camera's view and projection.</summary>
    public void SetWaterMirror(bool on, float plane, Matrix4x4 mirrorVP)
    {
        Set(Water, "mirror", new Vector4(on ? 1f : 0f, plane, 0f, 0f));
        if (on) SetMatrix(Water, "mirrorVP", mirrorVP);
    }

    public void SetSky(Matrix4x4 inverseViewProjection, Vector2 viewport, Vector3 eye, SkyColors sky, SceneLighting light, float time)
    {
        SetMatrix(Sky, "invViewProj", inverseViewProjection);
        Set(Sky, "viewport", viewport);
        Set(Sky, "eye", eye);
        Set(Sky, "zenith", Of(sky.Zenith));
        Set(Sky, "middle", Of(sky.Middle));
        Set(Sky, "horizon", Of(sky.Horizon));
        Set(Sky, "cloudTint", Of(sky.CloudTint));
        Set(Sky, "stars", sky.Stars);
        Set(Sky, "sunDir", light.SunDirection);
        Set(Sky, "sunColor", light.SunColor);
        Set(Sky, "time", time);
    }

    private static Vector3 Of(Color c) => new(c.R / 255f, c.G / 255f, c.B / 255f);

    private void SetColours(string name, float[] colours)
    {
        int loc = FieldShaders.Location(Terrain, name);
        if (loc >= 0) Raylib.SetShaderValueV(Terrain, loc, colours, ShaderUniformDataType.Vec3, colours.Length / 3);
    }

    private static void Set(Shader shader, string name, float value)
    {
        int loc = FieldShaders.Location(shader, name);
        if (loc >= 0) Raylib.SetShaderValue(shader, loc, value, ShaderUniformDataType.Float);
    }

    private static void SetInt(Shader shader, string name, int value)
    {
        int loc = FieldShaders.Location(shader, name);
        if (loc >= 0) Raylib.SetShaderValue(shader, loc, value, ShaderUniformDataType.Int);
    }

    private static void Set(Shader shader, string name, Vector2 value)
    {
        int loc = FieldShaders.Location(shader, name);
        if (loc >= 0) Raylib.SetShaderValue(shader, loc, value, ShaderUniformDataType.Vec2);
    }

    private static void Set(Shader shader, string name, Vector3 value)
    {
        int loc = FieldShaders.Location(shader, name);
        if (loc >= 0) Raylib.SetShaderValue(shader, loc, value, ShaderUniformDataType.Vec3);
    }

    private static void Set(Shader shader, string name, Vector4 value)
    {
        int loc = FieldShaders.Location(shader, name);
        if (loc >= 0) Raylib.SetShaderValue(shader, loc, value, ShaderUniformDataType.Vec4);
    }

    private static void SetMatrix(Shader shader, string name, Matrix4x4 value)
    {
        int loc = FieldShaders.Location(shader, name);
        if (loc >= 0) Raylib.SetShaderValueMatrix(shader, loc, value);
    }
}
