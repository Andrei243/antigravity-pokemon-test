using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>Lighting setup for one scene: sun direction and colors of sunlight and of the sky/ground bounce.</summary>
internal readonly record struct SceneLighting(Vector3 SunDirection, Vector3 SunColor, Vector3 SkyAmbient, Vector3 GroundAmbient)
{
    public static SceneLighting Lerp(SceneLighting a, SceneLighting b, float t) => new(
        Vector3.Normalize(Vector3.Lerp(a.SunDirection, b.SunDirection, t)), Vector3.Lerp(a.SunColor, b.SunColor, t),
        Vector3.Lerp(a.SkyAmbient, b.SkyAmbient, t), Vector3.Lerp(a.GroundAmbient, b.GroundAmbient, t));
}

/// <summary>
/// GLSL programs for the 3D scenes. Everything is lit per pixel by one sun with soft (Poisson-disc) shadow maps,
/// a sky/ground ambient term and distance fog; vertex alpha drives wind sway for grass and leaves. The post
/// programs build the half-resolution blur, bloom and ambient-occlusion buffers and composite the frame.
/// </summary>
internal sealed class FieldShaders
{
    public const int ShadowMapSlot = 10;

    public Shader World { get; private set; }
    public Shader Depth { get; private set; }
    public Shader Character { get; private set; }
    public Shader Outline { get; private set; }
    public Shader Water { get; private set; }
    public Shader Sprite { get; private set; }
    public Shader Post { get; private set; }
    public Shader Down { get; private set; }
    public Shader Blur { get; private set; }
    public Shader Ssao { get; private set; }

    private Shader[] FieldPrograms => new[] { World, Depth, Character, Water, Sprite };
    private Shader[] LitPrograms => new[] { World, Character, Water, Sprite };

    // raylib's DrawMesh fills in mvp, matModel and matNormal for each mesh it draws
    private const string CommonVertex = @"#version 330
in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexNormal;
in vec4 vertexColor;
uniform mat4 mvp;
uniform mat4 matModel;
uniform mat4 matNormal;
uniform mat4 lightVP;
uniform float time;
out vec2 fragTexCoord;
out vec4 fragColor;
out vec3 fragNormal;
out vec3 fragWorld;
out vec4 fragLight;

// Vertex alpha below 1 marks geometry that sways in the wind (grass tips, leaves). Only static scenery sways,
// and scenery is drawn with an identity model matrix, so the world-space offset can be added in model space.
vec3 SwayOffset(vec3 p, float weight)
{
    if (weight <= 0.0) return vec3(0.0);
    float t = time * 1.6 + p.x * 0.63 + p.z * 0.41;
    return vec3(sin(t) * 0.07, 0.0, cos(t * 0.83) * 0.035) * weight;
}

void main()
{
    vec3 world = (matModel * vec4(vertexPosition, 1.0)).xyz;
    vec3 sway = SwayOffset(world, 1.0 - vertexColor.a);
    vec3 n = normalize((matNormal * vec4(vertexNormal, 0.0)).xyz);
    fragTexCoord = vertexTexCoord;
    fragColor = vec4(vertexColor.rgb, 1.0);
    fragNormal = n;
    fragWorld = world + sway;
    // Offsetting along the normal before projecting into light space avoids shadow acne
    fragLight = lightVP * vec4(fragWorld + n * 0.035, 1.0);
    gl_Position = mvp * vec4(vertexPosition + sway, 1.0);
}";

    private const string OutlineVertex = @"#version 330
in vec3 vertexPosition;
in vec4 vertexColor;
uniform mat4 mvp;
out vec4 fragColor;
void main()
{
    fragColor = vertexColor;
    gl_Position = mvp * vec4(vertexPosition, 1.0);
}";

    private const string OutlineFragment = @"#version 330
in vec4 fragColor;
out vec4 finalColor;
void main()
{
    finalColor = vec4(fragColor.rgb, 1.0);
}";

    // Percentage-closer filtering over a Poisson disc, rotated per pixel so the few taps blend into a soft
    // penumbra instead of banding. shadowTaps (4..16) comes from the quality preset.
    private const string ShadowFunctions = @"
uniform sampler2D shadowMap;
uniform float shadowTexel;
uniform float shadowSoftness;
uniform int shadowTaps;

const vec2 poisson[16] = vec2[](
    vec2(-0.9420, -0.3991), vec2(0.9456, -0.7689), vec2(-0.0942, -0.9294), vec2(0.3450, 0.2939),
    vec2(-0.9159, 0.4577), vec2(-0.8154, -0.8791), vec2(-0.3828, 0.2768), vec2(0.9748, 0.7565),
    vec2(0.4432, -0.9751), vec2(0.5374, -0.4737), vec2(-0.2650, -0.4189), vec2(0.7920, 0.1909),
    vec2(-0.2419, 0.9971), vec2(-0.8141, 0.9144), vec2(0.1998, 0.7864), vec2(0.1438, -0.1410));

float Shadow(vec4 lightPos)
{
    vec3 p = lightPos.xyz / lightPos.w * 0.5 + 0.5;
    if (p.x <= 0.0 || p.x >= 1.0 || p.y <= 0.0 || p.y >= 1.0 || p.z >= 1.0) return 1.0;
    float a = 6.2831853 * fract(52.9829189 * fract(dot(gl_FragCoord.xy, vec2(0.06711056, 0.00583715))));
    mat2 rot = mat2(cos(a), sin(a), -sin(a), cos(a));
    float lit = 0.0;
    for (int i = 0; i < shadowTaps; i++)
    {
        float d = texture(shadowMap, p.xy + rot * poisson[i] * shadowTexel * shadowSoftness).r;
        lit += (p.z - 0.0014 > d) ? 0.0 : 1.0;
    }
    return lit / float(shadowTaps);
}";

    // Aerial perspective: colour fades toward the fog colour with distance from the camera
    private const string FogFunctions = @"
uniform vec3 viewPos;
uniform vec3 fogColor;
uniform float fogAmount;
uniform float fogNear;
uniform float fogFar;

vec3 ApplyFog(vec3 color, vec3 world)
{
    float f = smoothstep(fogNear, fogFar, length(viewPos - world)) * fogAmount;
    return mix(color, fogColor, f);
}";

    private const string PostVertex = @"#version 330
in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec4 vertexColor;
uniform mat4 mvp;
out vec2 fragTexCoord;
out vec4 fragColor;
void main()
{
    fragTexCoord = vertexTexCoord;
    fragColor = vertexColor;
    gl_Position = mvp * vec4(vertexPosition, 1.0);
}";

    // Scenery. worldRamp blends smooth Lambert light toward a soft two-tone cel band (battle stages); glow lights
    // up bluish glass (windows, glass doors) with warm lamplight after dark.
    private const string WorldFragment = @"#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
in vec3 fragNormal;
in vec3 fragWorld;
in vec4 fragLight;
uniform sampler2D texture0;
uniform vec4 colDiffuse;
uniform vec3 sunDir;
uniform vec3 sunColor;
uniform vec3 skyAmbient;
uniform vec3 groundAmbient;
uniform float worldRamp;
uniform float glow;
out vec4 finalColor;
" + ShadowFunctions + FogFunctions + @"
void main()
{
    vec4 texel = texture(texture0, fragTexCoord);
    if (texel.a < 0.5) discard;
    vec3 albedo = texel.rgb * colDiffuse.rgb * fragColor.rgb;
    vec3 n = normalize(fragNormal);
    float ndl = dot(n, sunDir);
    float shadow = ndl > 0.0 ? Shadow(fragLight) : 0.0;
    vec3 ambient = mix(groundAmbient, skyAmbient, n.y * 0.5 + 0.5);
    float diffuse = mix(max(ndl, 0.0), smoothstep(0.0, 0.3, ndl) * 0.92 + max(ndl, 0.0) * 0.08, worldRamp);
    vec3 color = albedo * (ambient + sunColor * diffuse * shadow);
    if (glow > 0.0)
    {
        float glass = smoothstep(0.04, 0.16, texel.b - texel.r) * glow;
        color = mix(color, vec3(1.0, 0.8, 0.46) * (0.85 + 0.3 * texel.g), glass);
    }
    finalColor = vec4(ApplyFog(color, fragWorld), 1.0);
}";

    // HD-2D billboards: pixel-art sprites lit by the scene's sun and sky, without self-shadowing
    private const string SpriteFragment = @"#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
in vec3 fragWorld;
uniform sampler2D texture0;
uniform vec3 sunColor;
uniform vec3 skyAmbient;
out vec4 finalColor;
" + FogFunctions + @"
void main()
{
    vec4 texel = texture(texture0, fragTexCoord);
    if (texel.a < 0.5) discard;
    vec3 color = texel.rgb * fragColor.rgb * (skyAmbient * 0.62 + sunColor * 0.78);
    finalColor = vec4(ApplyFog(color, fragWorld), 1.0);
}";

    // Shadow pass: only depth matters, but cut-out texels must not cast shadows
    private const string DepthFragment = @"#version 330
in vec2 fragTexCoord;
uniform sampler2D texture0;
out vec4 finalColor;
void main()
{
    if (texture(texture0, fragTexCoord).a < 0.5) discard;
    finalColor = vec4(1.0);
}";

    // Characters and Pokémon: two-tone cel shading with a soft terminator, plus a rim light so they stand out.
    // shadowStrength 0 skips the shadow map (for models rendered outside a scene, e.g. sprite bakes).
    private const string CharacterFragment = @"#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
in vec3 fragNormal;
in vec3 fragWorld;
in vec4 fragLight;
uniform sampler2D texture0;
uniform vec4 colDiffuse;
uniform vec3 sunDir;
uniform vec3 sunColor;
uniform vec3 skyAmbient;
uniform vec3 groundAmbient;
uniform float shadowStrength;
uniform float rimStrength;
uniform vec4 flash;
out vec4 finalColor;
" + ShadowFunctions + FogFunctions + @"
void main()
{
    vec4 texel = texture(texture0, fragTexCoord);
    if (texel.a < 0.5) discard;
    vec3 albedo = texel.rgb * colDiffuse.rgb * fragColor.rgb;
    vec3 n = normalize(fragNormal);
    float ndl = dot(n, sunDir);
    float lit = smoothstep(0.0, 0.12, ndl);
    if (shadowStrength > 0.0) lit *= mix(1.0, Shadow(fragLight), shadowStrength);
    vec3 ambient = mix(groundAmbient, skyAmbient, n.y * 0.5 + 0.5);
    vec3 color = albedo * (ambient * 1.08 + sunColor * lit);
    vec3 v = normalize(viewPos - fragWorld);
    float rim = pow(1.0 - clamp(dot(n, v), 0.0, 1.0), 3.0);
    color += (albedo * 0.6 + vec3(0.1)) * rim * rimStrength;
    color = ApplyFog(color, fragWorld);
    finalColor = vec4(mix(color, flash.rgb, flash.a), 1.0);
}";

    // Two drifting layers of the water texture, rippling sun glints, a fresnel sky tint and shoreline foam
    // (vertex red channel carries how close each vertex is to the shore).
    private const string WaterFragment = @"#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
in vec3 fragWorld;
in vec4 fragLight;
uniform sampler2D texture0;
uniform vec3 sunDir;
uniform vec3 sunColor;
uniform vec3 skyAmbient;
uniform float time;
out vec4 finalColor;
" + ShadowFunctions + FogFunctions + @"
void main()
{
    vec2 uv1 = fragWorld.xz * 0.45 + vec2(time * 0.030, time * 0.017);
    vec2 uv2 = fragWorld.xz * 0.31 + vec2(-time * 0.021, time * 0.026);
    vec3 water = (texture(texture0, uv1).rgb + texture(texture0, uv2).rgb) * 0.5;

    float w1 = sin(fragWorld.x * 2.3 + time * 1.4) * cos(fragWorld.z * 1.9 - time * 1.1);
    float w2 = sin((fragWorld.x + fragWorld.z) * 3.1 - time * 2.1);
    vec3 n = normalize(vec3(w1 * 0.16, 1.0, w2 * 0.16));
    vec3 v = normalize(viewPos - fragWorld);
    vec3 h = normalize(v + sunDir);
    float shadow = Shadow(fragLight);
    float spec = pow(max(dot(n, h), 0.0), 120.0) * 1.6 * shadow * length(sunColor);
    float fresnel = pow(1.0 - max(dot(n, v), 0.0), 3.0);

    vec3 color = water * (skyAmbient + sunColor * 0.75 * shadow) + vec3(spec);
    color = mix(color, skyAmbient * 1.3, fresnel * 0.35);

    // Foam in a thin, broken band right at the water's edge (vertex red = closeness to the shore)
    float foamNoise = texture(texture0, fragWorld.xz * 1.3 + vec2(time * 0.05, 0.0)).g;
    float foam = smoothstep(0.45, 0.95, fragColor.r * (0.8 + foamNoise * 0.4));
    color = mix(color, (skyAmbient + sunColor) * 0.95, foam * 0.8);
    finalColor = vec4(ApplyFog(color, fragWorld), 1.0);
}";

    // Final composite: FXAA (single-sample presets), tilt-shift blur toward the top and bottom of the screen,
    // the wider depth-of-field blur, ambient occlusion, outlines where depth jumps (battle stages), bloom, then
    // grading and a vignette.
    private const string PostFragment = @"#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
uniform sampler2D texture0;
uniform sampler2D blurTex;
uniform sampler2D bloomTex;
uniform sampler2D aoTex;
uniform sampler2D depthTex;
uniform vec2 texel;
uniform float blurStrength;
uniform float dof;
uniform float focusBand;
uniform float bloomStrength;
uniform float aoStrength;
uniform float outlineStrength;
uniform float outlineWidth;
uniform float nearPlane;
uniform float farPlane;
uniform float fxaa;
uniform float saturation;
uniform float contrast;
uniform vec3 shadowTint;
uniform vec3 highlightTint;
uniform float vignette;
out vec4 finalColor;

const vec2 taps[12] = vec2[](
    vec2(-0.326, -0.406), vec2(-0.840, -0.074), vec2(-0.696, 0.457), vec2(-0.203, 0.621),
    vec2(0.962, -0.195), vec2(0.473, -0.480), vec2(0.519, 0.767), vec2(0.185, -0.893),
    vec2(0.507, 0.064), vec2(0.896, 0.412), vec2(-0.322, -0.933), vec2(-0.792, -0.598));

const vec3 lumaWeights = vec3(0.299, 0.587, 0.114);

// FXAA in the spirit of Timothy Lottes' console version: blend along the local edge direction
vec3 Fxaa(vec2 uv)
{
    vec3 rgbNW = texture(texture0, uv + vec2(-1.0, -1.0) * texel).rgb;
    vec3 rgbNE = texture(texture0, uv + vec2(1.0, -1.0) * texel).rgb;
    vec3 rgbSW = texture(texture0, uv + vec2(-1.0, 1.0) * texel).rgb;
    vec3 rgbSE = texture(texture0, uv + vec2(1.0, 1.0) * texel).rgb;
    vec3 rgbM = texture(texture0, uv).rgb;
    float lNW = dot(rgbNW, lumaWeights), lNE = dot(rgbNE, lumaWeights);
    float lSW = dot(rgbSW, lumaWeights), lSE = dot(rgbSE, lumaWeights), lM = dot(rgbM, lumaWeights);
    float lMin = min(lM, min(min(lNW, lNE), min(lSW, lSE)));
    float lMax = max(lM, max(max(lNW, lNE), max(lSW, lSE)));
    vec2 dir = vec2(-((lNW + lNE) - (lSW + lSE)), (lNW + lSW) - (lNE + lSE));
    float reduce = max((lNW + lNE + lSW + lSE) * 0.03125, 1.0 / 128.0);
    float rcpMin = 1.0 / (min(abs(dir.x), abs(dir.y)) + reduce);
    dir = clamp(dir * rcpMin, vec2(-8.0), vec2(8.0)) * texel;
    vec3 rgbA = 0.5 * (texture(texture0, uv + dir * (1.0 / 3.0 - 0.5)).rgb + texture(texture0, uv + dir * (2.0 / 3.0 - 0.5)).rgb);
    vec3 rgbB = rgbA * 0.5 + 0.25 * (texture(texture0, uv - dir * 0.5).rgb + texture(texture0, uv + dir * 0.5).rgb);
    float lB = dot(rgbB, lumaWeights);
    return (lB < lMin || lB > lMax) ? rgbA : rgbB;
}

float InverseDepth(vec2 uv)
{
    float z = texture(depthTex, uv).r * 2.0 - 1.0;
    return (farPlane + nearPlane - z * (farPlane - nearPlane)) / (2.0 * nearPlane * farPlane);
}

void main()
{
    vec2 uv = fragTexCoord;
    vec3 color = fxaa > 0.5 ? Fxaa(uv) : texture(texture0, uv).rgb;

    float band = abs(uv.y - 0.5) * 2.0;
    float edge = smoothstep(focusBand, 1.0, band);
    float radius = edge * blurStrength;
    if (radius > 0.05)
    {
        vec3 sum = color;
        for (int i = 0; i < 12; i++) sum += texture(texture0, uv + taps[i] * radius * texel).rgb;
        color = sum / 13.0;
    }
    if (dof > 0.0) color = mix(color, texture(blurTex, uv).rgb, edge * dof);
    if (aoStrength > 0.0) color *= mix(1.0, texture(aoTex, uv).r, aoStrength);

    // Outlines on the near side of depth steps. Inverse depth is linear across flat surfaces in screen space,
    // so its Laplacian is zero on the ground and only fires at silhouettes and creases.
    if (outlineStrength > 0.0)
    {
        vec2 o = texel * outlineWidth;
        float c = InverseDepth(uv);
        float around = InverseDepth(uv + vec2(o.x, 0.0)) + InverseDepth(uv - vec2(o.x, 0.0))
                     + InverseDepth(uv + vec2(0.0, o.y)) + InverseDepth(uv - vec2(0.0, o.y));
        float rise = (4.0 * c - around) / c;
        color *= 1.0 - smoothstep(0.06, 0.2, rise) * outlineStrength;
    }

    if (bloomStrength > 0.0) color += texture(bloomTex, uv).rgb * bloomStrength;

    float luma = dot(color, lumaWeights);
    color *= mix(shadowTint, highlightTint, smoothstep(0.15, 0.85, luma));
    color = mix(vec3(luma), color, saturation);
    color = (color - 0.5) * contrast + 0.5;

    vec2 d = (uv - 0.5) * vec2(1.25, 1.0);
    color *= mix(1.0 - vignette, 1.0, smoothstep(0.85, 0.3, length(d)));
    finalColor = vec4(clamp(color, 0.0, 1.0), 1.0);
}";

    // Half-resolution chains for bloom and depth of field: a 4-tap downsample (optionally keeping only what is
    // brighter than a threshold), then separable 9-tap Gaussian blurs
    private const string DownFragment = @"#version 330
in vec2 fragTexCoord;
uniform sampler2D texture0;
uniform vec2 texel;
uniform float threshold;
out vec4 finalColor;
void main()
{
    vec3 c = texture(texture0, fragTexCoord + vec2(-texel.x, -texel.y)).rgb
           + texture(texture0, fragTexCoord + vec2(texel.x, -texel.y)).rgb
           + texture(texture0, fragTexCoord + vec2(-texel.x, texel.y)).rgb
           + texture(texture0, fragTexCoord + vec2(texel.x, texel.y)).rgb;
    c *= 0.25;
    if (threshold < 1.0)
    {
        float luma = max(c.r, max(c.g, c.b));
        c *= smoothstep(threshold, threshold + 0.2, luma);
    }
    finalColor = vec4(c, 1.0);
}";

    private const string BlurFragment = @"#version 330
in vec2 fragTexCoord;
uniform sampler2D texture0;
uniform vec2 direction;
out vec4 finalColor;
void main()
{
    vec3 c = texture(texture0, fragTexCoord).rgb * 0.2270270270;
    c += texture(texture0, fragTexCoord + direction * 1.3846153846).rgb * 0.3162162162;
    c += texture(texture0, fragTexCoord - direction * 1.3846153846).rgb * 0.3162162162;
    c += texture(texture0, fragTexCoord + direction * 3.2307692308).rgb * 0.0702702703;
    c += texture(texture0, fragTexCoord - direction * 3.2307692308).rgb * 0.0702702703;
    finalColor = vec4(c, 1.0);
}";

    // Screen-space ambient occlusion from the depth buffer alone: view positions are rebuilt from depth, the
    // normal from the flatter pair of neighbours, and 12 samples in the normal's hemisphere are tested.
    private const string SsaoFragment = @"#version 330
in vec2 fragTexCoord;
uniform sampler2D texture0;
uniform vec2 depthTexel;
uniform float nearPlane;
uniform float farPlane;
uniform vec2 tanHalf;
uniform float radius;
out vec4 finalColor;

const vec3 kernel[12] = vec3[](
    vec3(0.10, 0.06, 0.08), vec3(-0.12, 0.10, 0.12), vec3(0.04, -0.18, 0.14), vec3(-0.22, -0.08, 0.16),
    vec3(0.28, 0.14, 0.10), vec3(-0.10, 0.34, 0.22), vec3(0.20, -0.36, 0.26), vec3(-0.44, 0.12, 0.30),
    vec3(0.50, 0.30, 0.34), vec3(-0.30, -0.58, 0.40), vec3(0.16, 0.74, 0.46), vec3(-0.78, -0.24, 0.52));

float LinearDepth(float d)
{
    float z = d * 2.0 - 1.0;
    return 2.0 * nearPlane * farPlane / (farPlane + nearPlane - z * (farPlane - nearPlane));
}

vec3 ViewPos(vec2 uv)
{
    float z = LinearDepth(texture(texture0, uv).r);
    return vec3((uv * 2.0 - 1.0) * tanHalf * z, -z);
}

void main()
{
    vec2 uv = fragTexCoord;
    if (texture(texture0, uv).r >= 0.99999) { finalColor = vec4(1.0); return; }
    vec3 p = ViewPos(uv);
    vec2 dx = vec2(depthTexel.x * 2.0, 0.0), dy = vec2(0.0, depthTexel.y * 2.0);
    vec3 pr = ViewPos(uv + dx), pl = ViewPos(uv - dx), pu = ViewPos(uv + dy), pd = ViewPos(uv - dy);
    vec3 ddx = abs(pr.z - p.z) < abs(p.z - pl.z) ? pr - p : p - pl;
    vec3 ddy = abs(pu.z - p.z) < abs(p.z - pd.z) ? pu - p : p - pd;
    vec3 n = normalize(cross(ddx, ddy));
    if (dot(n, p) > 0.0) n = -n;

    float a = 6.2831853 * fract(52.9829189 * fract(dot(gl_FragCoord.xy, vec2(0.06711056, 0.00583715))));
    vec3 rnd = vec3(cos(a), sin(a), 0.0);
    vec3 t = normalize(rnd - n * dot(rnd, n));
    mat3 tbn = mat3(t, cross(n, t), n);

    float occlusion = 0.0;
    for (int i = 0; i < 12; i++)
    {
        vec3 s = p + tbn * kernel[i] * radius;
        vec2 suv = (s.xy / -s.z) / tanHalf * 0.5 + 0.5;
        if (suv.x < 0.0 || suv.x > 1.0 || suv.y < 0.0 || suv.y > 1.0) continue;
        float sceneZ = -LinearDepth(texture(texture0, suv).r);
        float range = smoothstep(0.0, 1.0, radius / max(abs(p.z - sceneZ), 1e-4));
        occlusion += (sceneZ >= s.z + 0.03 * radius) ? range : 0.0;
    }
    finalColor = vec4(vec3(1.0 - occlusion / 12.0), 1.0);
}";

    public bool Loaded { get; private set; }

    public void Load()
    {
        if (Loaded) return;
        World = Raylib.LoadShaderFromMemory(CommonVertex, WorldFragment);
        Depth = Raylib.LoadShaderFromMemory(CommonVertex, DepthFragment);
        Character = Raylib.LoadShaderFromMemory(CommonVertex, CharacterFragment);
        Outline = Raylib.LoadShaderFromMemory(OutlineVertex, OutlineFragment);
        Water = Raylib.LoadShaderFromMemory(CommonVertex, WaterFragment);
        Sprite = Raylib.LoadShaderFromMemory(CommonVertex, SpriteFragment);
        Post = Raylib.LoadShaderFromMemory(PostVertex, PostFragment);
        Down = Raylib.LoadShaderFromMemory(PostVertex, DownFragment);
        Blur = Raylib.LoadShaderFromMemory(PostVertex, BlurFragment);
        Ssao = Raylib.LoadShaderFromMemory(PostVertex, SsaoFragment);

        foreach (var shader in new[] { World, Character, Water })
        {
            Set(shader, "shadowMap", ShadowMapSlot);
        }
        SetCharacterStyle(shadowStrength: 1f, rimStrength: 0.45f);
        SetFlash(default, 0f);
        SetGlow(0f);
        SetShadowQuality(16, 1.8f);
        SetFog(Vector3.One, 0f, 1000f, 2000f);
        Loaded = true;
    }

    public void Unload()
    {
        if (!Loaded) return;
        foreach (var shader in new[] { World, Depth, Character, Outline, Water, Sprite, Post, Down, Blur, Ssao })
        {
            Raylib.UnloadShader(shader);
        }
        Loaded = false;
    }

    /// <summary>Animation time (wind, water); set before the shadow pass so both passes sway together.</summary>
    public void SetTime(float time)
    {
        foreach (var shader in FieldPrograms)
        {
            Set(shader, "time", time);
        }
    }

    /// <summary>How strongly characters receive scene shadows, and how bright their rim light is.</summary>
    public void SetCharacterStyle(float shadowStrength, float rimStrength)
    {
        Set(Character, "shadowStrength", shadowStrength);
        Set(Character, "rimStrength", rimStrength);
    }

    /// <summary>Per-frame lighting values shared by the scene shaders, once the sun's shadow map is rendered.</summary>
    public void SetLighting(Matrix4x4 lightViewProjection, SceneLighting light, Vector3 viewPosition, float shadowTexel)
    {
        foreach (var shader in FieldPrograms)
        {
            SetMatrix(shader, "lightVP", lightViewProjection);
            Set(shader, "sunDir", light.SunDirection);
            Set(shader, "sunColor", light.SunColor);
            Set(shader, "skyAmbient", light.SkyAmbient);
            Set(shader, "groundAmbient", light.GroundAmbient);
            Set(shader, "viewPos", viewPosition);
            Set(shader, "shadowTexel", shadowTexel);
        }
    }

    /// <summary>Distance fog: colour, strength at the far distance, and where it starts and ends (world units from the camera).</summary>
    public void SetFog(Vector3 color, float amount, float near, float far)
    {
        foreach (var shader in LitPrograms)
        {
            Set(shader, "fogColor", color);
            Set(shader, "fogAmount", amount);
            Set(shader, "fogNear", near);
            Set(shader, "fogFar", far);
        }
    }

    /// <summary>Shadow filtering: taps from the quality preset, softness as the disc radius in shadow-map texels.</summary>
    public void SetShadowQuality(int taps, float softness)
    {
        foreach (var shader in new[] { World, Character, Water })
        {
            Set(shader, "shadowTaps", Math.Clamp(taps, 1, 16));
            Set(shader, "shadowSoftness", softness);
        }
    }

    /// <summary>Warm light behind glass (windows, glass doors); set around the drawing of glazed scenery.</summary>
    public void SetGlow(float amount) => Set(World, "glow", amount);

    public void SetPost(Vector2 texel, PostSettings post, DepthRange depth, bool fxaa, float outlineWidth)
    {
        Set(Post, "texel", texel);
        Set(Post, "blurStrength", post.TiltShift);
        Set(Post, "dof", post.Dof);
        Set(Post, "focusBand", post.FocusBand);
        Set(Post, "bloomStrength", post.BloomStrength);
        Set(Post, "aoStrength", post.AoStrength);
        Set(Post, "outlineStrength", post.OutlineStrength);
        Set(Post, "outlineWidth", outlineWidth);
        Set(Post, "nearPlane", depth.Near);
        Set(Post, "farPlane", depth.Far);
        Set(Post, "fxaa", fxaa ? 1f : 0f);
        Set(Post, "saturation", post.Saturation);
        Set(Post, "contrast", post.Contrast);
        Set(Post, "shadowTint", post.ShadowTint);
        Set(Post, "highlightTint", post.HighlightTint);
        Set(Post, "vignette", post.Vignette);
    }

    /// <summary>Binds the half-resolution buffers and the scene depth for the composite; call inside its shader mode.</summary>
    public void BindPostTextures(Texture2D blur, Texture2D bloom, Texture2D ao, Texture2D depth)
    {
        Raylib.SetShaderValueTexture(Post, Raylib.GetShaderLocation(Post, "blurTex"), blur);
        Raylib.SetShaderValueTexture(Post, Raylib.GetShaderLocation(Post, "bloomTex"), bloom);
        Raylib.SetShaderValueTexture(Post, Raylib.GetShaderLocation(Post, "aoTex"), ao);
        Raylib.SetShaderValueTexture(Post, Raylib.GetShaderLocation(Post, "depthTex"), depth);
    }

    public void SetDown(Vector2 texel, float threshold)
    {
        Set(Down, "texel", texel);
        Set(Down, "threshold", threshold);
    }

    public void SetBlur(Vector2 direction) => Set(Blur, "direction", direction);

    public void SetSsao(Vector2 depthTexel, DepthRange depth, float radius)
    {
        Set(Ssao, "depthTexel", depthTexel);
        Set(Ssao, "nearPlane", depth.Near);
        Set(Ssao, "farPlane", depth.Far);
        float tanHalf = MathF.Tan(depth.FovYDeg * MathF.PI / 360f);
        Set(Ssao, "tanHalf", new Vector2(tanHalf * depth.Aspect, tanHalf));
        Set(Ssao, "radius", radius);
    }

    /// <summary>How strongly scenery takes a cel-shaded light band (see <see cref="ArtLook.BattleRamp"/>).</summary>
    public void SetWorldRamp(float ramp) => Set(World, "worldRamp", ramp);

    /// <summary>Blends characters toward a flat colour (hit flashes, the dark silhouette of a wild Pokémon).</summary>
    public void SetFlash(Color color, float amount) =>
        Set(Character, "flash", new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, amount));

    private static void Set(Shader shader, string name, float value)
    {
        int loc = Raylib.GetShaderLocation(shader, name);
        if (loc >= 0) Raylib.SetShaderValue(shader, loc, value, ShaderUniformDataType.Float);
    }

    private static void Set(Shader shader, string name, int value)
    {
        int loc = Raylib.GetShaderLocation(shader, name);
        if (loc >= 0) Raylib.SetShaderValue(shader, loc, value, ShaderUniformDataType.Int);
    }

    private static void Set(Shader shader, string name, Vector2 value)
    {
        int loc = Raylib.GetShaderLocation(shader, name);
        if (loc >= 0) Raylib.SetShaderValue(shader, loc, value, ShaderUniformDataType.Vec2);
    }

    private static void Set(Shader shader, string name, Vector3 value)
    {
        int loc = Raylib.GetShaderLocation(shader, name);
        if (loc >= 0) Raylib.SetShaderValue(shader, loc, value, ShaderUniformDataType.Vec3);
    }

    private static void Set(Shader shader, string name, Vector4 value)
    {
        int loc = Raylib.GetShaderLocation(shader, name);
        if (loc >= 0) Raylib.SetShaderValue(shader, loc, value, ShaderUniformDataType.Vec4);
    }

    private static void SetMatrix(Shader shader, string name, Matrix4x4 value)
    {
        int loc = Raylib.GetShaderLocation(shader, name);
        if (loc >= 0) Raylib.SetShaderValueMatrix(shader, loc, value);
    }
}

/// <summary>The camera's clip planes and field of view, for passes that read the depth buffer.</summary>
internal readonly record struct DepthRange(float Near, float Far, float FovYDeg, float Aspect);
