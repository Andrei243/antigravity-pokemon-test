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
    float f = smoothstep(fogNear, fogFar, length(viewPos - world)) * fogAmount;
    return mix(color, fogColor, f);
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

    private const string TerrainFragment = @"#version 330
in vec3 fragWorld;
in vec3 fragNormal;
in vec3 fragBary;
flat in ivec3 fragSlots;
uniform sampler2D texture0;
uniform vec3 sunDir;
uniform vec3 sunColor;
uniform vec3 skyAmbient;
uniform vec3 groundAmbient;
uniform float worldRamp;
out vec4 finalColor;
" + Shared + CascadeFunctions + @"
const float Texels = 16.0;
const float TileMetres = 2.0;
const float Slots = 16.0;
const float Tile = 32.0;
const float Pad = 16.0;
const float Cell = 64.0;

vec2 Project(vec3 v, int axis) { return axis == 1 ? v.xz : axis == 0 ? vec2(v.z, -v.y) : vec2(v.x, -v.y); }

void main()
{
    if (mirrorClip.x > 0.5 && fragWorld.y < mirrorClip.y) discard;
    vec3 n = normalize(fragNormal);
    // Derivatives first, while every fragment of the quad takes them
    vec3 wx = dFdx(fragWorld), wy = dFdy(fragWorld);
    vec3 bx = dFdx(fragBary), by = dFdy(fragBary);

    // The face's own axis for this 1/16 m cube: top, east-west or north-south, by the normal's weights to the fourth power
    vec3 w = pow(abs(n), vec3(4.0));
    w /= (w.x + w.y + w.z);
    float pick = Hash3(floor(fragWorld * Texels) + vec3(0.5, 7.0, 13.0));
    int axis = pick < w.y ? 1 : (pick < w.y + w.x ? 0 : 2);
    vec2 plane = Project(fragWorld, axis);
    vec2 texel = floor(plane * Texels);
    vec2 centre = (texel + 0.5) / Texels;

    // The corners' weights at the texel's middle: the triangle's weights move linearly over its plane
    vec2 px = Project(wx, axis), py = Project(wy, axis);
    float det = px.x * py.y - py.x * px.y;
    vec3 b = fragBary;
    if (abs(det) > 1e-14)
    {
        vec2 d = centre - plane;
        float s = (d.x * py.y - py.x * d.y) / det;
        float t = (px.x * d.y - d.x * px.y) / det;
        b += s * bx + t * by;
    }

    // The cover of the corner whose weight is greatest, the weights of corners of one cover summed, jittered per texel
    ivec3 c = fragSlots;
    if (c.y == c.x) { b.x += b.y; b.y = -9.0; }
    if (c.z == c.x) { b.x += b.z; b.z = -9.0; }
    else if (c.z == c.y && b.y > -1.0) { b.y += b.z; b.z = -9.0; }
    float salt = float(axis) * 31.0;
    b += (vec3(Hash(texel + salt), Hash(texel + salt + 17.3), Hash(texel + salt + 41.7)) - 0.5) * 0.56;
    int slot = b.x >= b.y && b.x >= b.z ? c.x : (b.y >= b.z ? c.y : c.z);

    // The tile's texel there, in the middle of its cell: whole texels near, between mip levels far (the cell's pad is
    // the tile's own, so a far level reads nothing of another cover)
    vec2 tile = fract(centre / TileMetres);
    vec2 atlas = vec2((Pad + tile.x * Tile) / Cell, (float(slot) * Cell + Pad + tile.y * Tile) / (Cell * Slots));
    vec2 scale = vec2(Tile / Cell, Tile / (Cell * Slots)) / TileMetres;
    vec3 albedo = textureGrad(texture0, atlas, px * scale, py * scale).rgb;

    float ndl = dot(n, sunDir);
    float shadow = ndl > 0.0 ? CascadeShadow(fragWorld, n) : 0.0;
    vec3 ambient = mix(groundAmbient, skyAmbient, n.y * 0.5 + 0.5);
    float diffuse = mix(max(ndl, 0.0), smoothstep(0.0, 0.3, ndl) * 0.92 + max(ndl, 0.0) * 0.08, worldRamp);
    vec3 light = ambient + sunColor * diffuse * shadow * CloudLight(fragWorld) + Lamp(fragWorld, n);
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

    private const string WaterFragment = @"#version 330
in vec3 fragWorld;
in float fragDepth;
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
const vec3 foamThin = vec3(176.0, 218.0, 248.0) / 255.0;
const vec3 waveLight = vec3(150.0, 206.0, 246.0) / 255.0;
const vec3 waveDark = vec3(46.0, 104.0, 186.0) / 255.0;
const float Texels = 16.0;

void main()
{
    vec3 wx = dFdx(fragWorld), wy = dFdy(fragWorld);
    float dx = dFdx(fragDepth), dy = dFdy(fragDepth);
    vec2 t = floor(fragWorld.xz * Texels);
    vec2 centre = (t + 0.5) / Texels;

    // The depth at the texel's middle, so every band is whole texels
    float depth = fragDepth;
    float det = wx.x * wy.z - wy.x * wx.z;
    if (abs(det) > 1e-14)
    {
        vec2 d = centre - fragWorld.xz;
        float s = (d.x * wy.z - wy.x * d.y) / det;
        float u = (wx.x * d.y - d.x * wx.z) / det;
        depth += s * dx + u * dy;
    }
    if (depth < 0.0) discard;

    // Depth bands whose edges breathe by a texel
    float breathe = sin(time * 0.9 + t.x * 0.11 + t.y * 0.17) * 0.04;
    vec3 color = depth + breathe < 0.3 ? shallow : depth + breathe < 1.0 ? middle : deep;
    vec3 band = color;

    // Wave marks: a light dash with dipped ends and a dark line under it, one to a cell, drifting east
    vec2 p = vec2(t.x - floor(time * 2.0), t.y);
    vec2 c = floor(p / vec2(22.0, 11.0));
    vec2 f = p - c * vec2(22.0, 11.0);
    float h1 = Hash(c), h2 = Hash(c + 17.0), h3 = Hash(c + 43.0);
    float len = 4.0 + floor(h2 * 5.0);
    vec2 o = vec2(floor(h1 * (22.0 - len)), 1.0 + floor(h3 * 7.0));
    if (h2 > 0.3 && fract(h1 + time * 0.09) < 0.72 && depth > 0.5 && f.x >= o.x && f.x < o.x + len)
    {
        float dip = (f.x == o.x || f.x == o.x + len - 1.0) ? 1.0 : 0.0;
        if (f.y == o.y + dip) color = waveLight;
        else if (f.y == o.y + dip + 1.0) color = waveDark;
    }

    // Foam: lapping where it is shallowest, and a broken line a little further out
    float lap = 0.035 + 0.025 * sin(time * 1.3 + (t.x + t.y) * 0.09);
    float ring = 0.15 + 0.05 * sin(time * 0.8 + (t.x - t.y) * 0.07);
    if (depth < lap) color = foam;
    else if (abs(depth - ring) < 0.012 && Hash(floor(t / 3.0) + 5.0) > 0.42) color = foamThin;

    float shadow = CascadeShadow(fragWorld, vec3(0.0, 1.0, 0.0));
    float sun = max(sunDir.y, 0.0) * shadow * CloudLight(fragWorld);
    const vec3 luma = vec3(0.3, 0.5, 0.2);
    float daylight = smoothstep(0.6, 0.95, dot(skyAmbient + sunColor * max(sunDir.y, 0.0), luma));

    // Sparkles: a plus sign that flashes for a moment, where the sun reaches
    vec2 sc = floor(t / 30.0);
    vec2 sf = t - sc * 30.0 - (floor(vec2(Hash(sc + 3.0), Hash(sc + 7.0)) * 24.0) + 3.0);
    float life = fract(time * 0.4 + Hash(sc + 91.0));
    if (life < 0.2 && depth > 0.3 && sun > 0.3 && daylight > 0.5)
    {
        float arm = (life > 0.05 && life < 0.14) ? 2.0 : 1.0;
        if ((sf.x == 0.0 && abs(sf.y) <= arm) || (sf.y == 0.0 && abs(sf.x) <= arm)) color = vec3(1.0);
    }

    bool plain = color == band;
    color = mix(mix(vec3(dot(color, luma)), color, 0.55) * 0.62, color, daylight);
    color *= skyAmbient + sunColor * sun + Lamp(fragWorld, vec3(0.0, 1.0, 0.0));

    // What stands over the water, mirrored: in the mid and deep bands of the water at the mirror's height alone, one
    // colour per texel read at its middle, each row pushed a texel east or west on a slow swell, broken into runs
    if (mirror.x > 0.5 && plain && depth + breathe >= 0.3 && abs(fragWorld.y - mirror.y) < 0.25)
    {
        float swell = floor(1.2 * sin(time * 1.5 + t.y * 0.9) + 0.5);
        float run = floor((t.x - floor(time * 2.0)) / 6.0);
        if (Hash(vec2(run, t.y) + 71.0) > 0.16)
        {
            vec3 at = vec3((t.x + swell + 0.5) / Texels, mirror.y, (t.y + 0.5) / Texels);
            vec4 m = mirrorVP * vec4(at, 1.0);
            vec2 uv = m.xy / m.w * 0.5 + 0.5;
            if (m.w > 0.0 && uv.x >= 0.0 && uv.y >= 0.0 && uv.x <= 1.0 && uv.y <= 1.0)
            {
                vec4 seen = texture(texture1, uv);
                color = mix(color, mix(seen.rgb, color, 0.35) * 0.82, seen.a * 0.7);
            }
        }
    }
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

    // Clouds: a layer 600 m up, three octaves of value noise, soft-edged, lit in the cloud tint and shaded
    // toward the sky's middle underneath, drifting east, thinning to nothing within 4° of the horizon
    if (e > 0.005)
    {
        float reach = (600.0 - eye.y) / e;
        vec2 p = eye.xz + dir.xz * reach;
        vec2 q = (p + vec2(time * 4.0, 0.0)) * 0.0018;
        float n = Noise(q) * 0.55 + Noise(q * 2.03 + 11.0) * 0.3 + Noise(q * 4.1 + 23.0) * 0.15;
        float cloud = smoothstep(0.52, 0.70, n) * smoothstep(0.0, 0.07, e);
        vec3 shade = mix(cloudTint, middle, 0.22);
        vec3 lit = mix(shade, cloudTint, smoothstep(0.55, 0.85, n));
        color = mix(color, lit, cloud * 0.92);
    }
    finalColor = vec4(color, 1.0);
}";

    /// <summary>A program that reads the shadow maps, compiled for the way this machine filters them (as the field's are).</summary>
    private static string Shadowed(string fragment) =>
        FieldShaders.HardwareShadows ? fragment.Replace("#version 330", "#version 330\n#define HARDWARE_PCF") : fragment;

    public void Load()
    {
        if (Loaded) return;
        Terrain = Gpu.ShaderFrom(TerrainVertex, Shadowed(TerrainFragment), "shader");
        Water = Gpu.ShaderFrom(WaterVertex, Shadowed(WaterFragment), "shader");
        Sky = Gpu.ShaderFrom(SkyVertex, SkyFragment, "shader");
        foreach (var shader in new[] { Terrain, Water })
            for (int i = 0; i < OpenCascades.Count; i++) SetInt(shader, $"shadow{i}", OpenCascades.Slots[i]);
        Loaded = true;
    }

    public void Unload()
    {
        if (!Loaded) return;
        Gpu.Unload(Terrain);
        Gpu.Unload(Water);
        Gpu.Unload(Sky);
        Loaded = false;
    }

    private Shader[] Lit => new[] { Terrain, Water };

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
        Set(Terrain, "worldRamp", worldRamp);
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
    public void SetMirrorClip(bool on, float plane) => Set(Terrain, "mirrorClip", on ? new Vector2(1f, plane) : Vector2.Zero);

    /// <summary>What the water reads of the mirror: whether it mirrors, the plane and the mirrored camera's view and projection.</summary>
    public void SetWaterMirror(bool on, float plane, Matrix4x4 mirrorVP)
    {
        Set(Water, "mirror", new Vector4(on ? 1f : 0f, plane, 0f, 0f));
        if (on) SetMatrix(Water, "mirrorVP", mirrorVP);
    }

    public void SetSky(Matrix4x4 inverseViewProjection, Vector2 viewport, Vector3 eye, SkyColors sky, float time)
    {
        SetMatrix(Sky, "invViewProj", inverseViewProjection);
        Set(Sky, "viewport", viewport);
        Set(Sky, "eye", eye);
        Set(Sky, "zenith", Of(sky.Zenith));
        Set(Sky, "middle", Of(sky.Middle));
        Set(Sky, "horizon", Of(sky.Horizon));
        Set(Sky, "cloudTint", Of(sky.CloudTint));
        Set(Sky, "stars", sky.Stars);
        Set(Sky, "time", time);
    }

    private static Vector3 Of(Color c) => new(c.R / 255f, c.G / 255f, c.B / 255f);

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
