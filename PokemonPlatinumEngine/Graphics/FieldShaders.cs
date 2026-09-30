using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>Lighting setup for one scene: sun direction and colors of sunlight and of the sky/ground bounce.</summary>
internal readonly record struct SceneLighting(Vector3 SunDirection, Vector3 SunColor, Vector3 SkyAmbient, Vector3 GroundAmbient);

/// <summary>
/// GLSL programs for the 3D field. Everything is lit per pixel by one sun with PCF-filtered shadow maps and a
/// sky/ground ambient term; vertex alpha drives wind sway for grass and leaves.
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

    private Shader[] FieldPrograms => new[] { World, Depth, Character, Water, Sprite };

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

    private const string ShadowFunctions = @"
uniform sampler2D shadowMap;
uniform float shadowTexel;

float Shadow(vec4 lightPos)
{
    vec3 p = lightPos.xyz / lightPos.w * 0.5 + 0.5;
    if (p.x <= 0.0 || p.x >= 1.0 || p.y <= 0.0 || p.y >= 1.0 || p.z >= 1.0) return 1.0;
    float lit = 0.0;
    for (int x = -1; x <= 1; x++)
    {
        for (int y = -1; y <= 1; y++)
        {
            float d = texture(shadowMap, p.xy + vec2(x, y) * shadowTexel * 1.3).r;
            lit += (p.z - 0.0012 > d) ? 0.0 : 1.0;
        }
    }
    return lit / 9.0;
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
out vec4 finalColor;
" + ShadowFunctions + @"
void main()
{
    vec4 texel = texture(texture0, fragTexCoord);
    if (texel.a < 0.5) discard;
    vec3 albedo = texel.rgb * colDiffuse.rgb * fragColor.rgb;
    vec3 n = normalize(fragNormal);
    float ndl = dot(n, sunDir);
    float shadow = ndl > 0.0 ? Shadow(fragLight) : 0.0;
    vec3 ambient = mix(groundAmbient, skyAmbient, n.y * 0.5 + 0.5);
    // worldRamp blends smooth Lambert light toward a soft two-tone cel band
    float diffuse = mix(max(ndl, 0.0), smoothstep(0.0, 0.3, ndl) * 0.92 + max(ndl, 0.0) * 0.08, worldRamp);
    vec3 color = albedo * (ambient + sunColor * diffuse * shadow);
    finalColor = vec4(color, 1.0);
}";

    // HD-2D billboards: pixel-art sprites lit by the scene's sun and sky, without self-shadowing
    private const string SpriteFragment = @"#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
uniform sampler2D texture0;
uniform vec3 sunColor;
uniform vec3 skyAmbient;
out vec4 finalColor;
void main()
{
    vec4 texel = texture(texture0, fragTexCoord);
    if (texel.a < 0.5) discard;
    finalColor = vec4(texel.rgb * fragColor.rgb * (skyAmbient * 0.62 + sunColor * 0.78), 1.0);
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
    // shadowStrength 0 skips the shadow map (for models rendered outside the field, e.g. battle sprites).
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
uniform vec3 viewPos;
uniform float shadowStrength;
uniform float rimStrength;
uniform vec4 flash;
out vec4 finalColor;
" + ShadowFunctions + @"
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
uniform vec3 viewPos;
uniform float time;
out vec4 finalColor;
" + ShadowFunctions + @"
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
    float spec = pow(max(dot(n, h), 0.0), 120.0) * 1.6 * shadow;
    float fresnel = pow(1.0 - max(dot(n, v), 0.0), 3.0);

    vec3 color = water * (skyAmbient + sunColor * 0.75 * shadow) + vec3(spec);
    color = mix(color, vec3(0.78, 0.9, 1.0), fresnel * 0.35);

    // Foam in a thin, broken band right at the water's edge (vertex red = closeness to the shore)
    float foamNoise = texture(texture0, fragWorld.xz * 1.3 + vec2(time * 0.05, 0.0)).g;
    float foam = smoothstep(0.45, 0.95, fragColor.r * (0.8 + foamNoise * 0.4));
    color = mix(color, vec3(0.94, 0.98, 1.0), foam * 0.8);
    finalColor = vec4(color, 1.0);
}";

    // Final composite: tilt-shift blur toward the top and bottom of the screen (the diorama look), an optional
    // wider depth-of-field blur and bloom from the half-resolution chains, then grading and a vignette
    private const string PostFragment = @"#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
uniform sampler2D texture0;
uniform sampler2D blurTex;
uniform sampler2D bloomTex;
uniform vec2 texel;
uniform float blurStrength;
uniform float dof;
uniform float focusBand;
uniform float bloomStrength;
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

void main()
{
    vec2 uv = fragTexCoord;
    vec3 color = texture(texture0, uv).rgb;

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
    if (bloomStrength > 0.0) color += texture(bloomTex, uv).rgb * bloomStrength;

    float luma = dot(color, vec3(0.299, 0.587, 0.114));
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

        foreach (var shader in new[] { World, Character, Water })
        {
            Set(shader, "shadowMap", ShadowMapSlot);
        }
        SetCharacterStyle(shadowStrength: 1f, rimStrength: 0.45f);
        SetFlash(default, 0f);
        Loaded = true;
    }

    public void Unload()
    {
        if (!Loaded) return;
        foreach (var shader in new[] { World, Depth, Character, Outline, Water, Sprite, Post, Down, Blur })
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

    /// <summary>How strongly characters receive field shadows, and how bright their rim light is.</summary>
    public void SetCharacterStyle(float shadowStrength, float rimStrength)
    {
        Set(Character, "shadowStrength", shadowStrength);
        Set(Character, "rimStrength", rimStrength);
    }

    /// <summary>Per-frame lighting values shared by the field shaders, once the sun's shadow map is rendered.</summary>
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

    public void SetPost(Vector2 texel, PostSettings post)
    {
        Set(Post, "texel", texel);
        Set(Post, "blurStrength", post.TiltShift);
        Set(Post, "dof", post.Dof);
        Set(Post, "focusBand", post.FocusBand);
        Set(Post, "bloomStrength", post.BloomStrength);
        Set(Post, "saturation", post.Saturation);
        Set(Post, "contrast", post.Contrast);
        Set(Post, "shadowTint", post.ShadowTint);
        Set(Post, "highlightTint", post.HighlightTint);
        Set(Post, "vignette", post.Vignette);
    }

    /// <summary>Binds the half-resolution blur and bloom textures for the composite; call inside its shader mode.</summary>
    public void BindPostTextures(Texture2D blur, Texture2D bloom)
    {
        Raylib.SetShaderValueTexture(Post, Raylib.GetShaderLocation(Post, "blurTex"), blur);
        Raylib.SetShaderValueTexture(Post, Raylib.GetShaderLocation(Post, "bloomTex"), bloom);
    }

    public void SetDown(Vector2 texel, float threshold)
    {
        Set(Down, "texel", texel);
        Set(Down, "threshold", threshold);
    }

    public void SetBlur(Vector2 direction) => Set(Blur, "direction", direction);

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
