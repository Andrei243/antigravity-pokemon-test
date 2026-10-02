using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Faces, drawn two ways (style guide, "Characters"): for the 3D models in battle, smooth anti-aliased textures
/// laid over the front of the head (eyes with two highlights, brows, a mouth; cut out where there's nothing, so
/// the skin shows through); for the field's pixel sprites, small hand-placed pixel faces stamped onto each baked
/// frame, because a smooth face shrunk to sprite size turns to mush. The painting runs without GPU calls.
/// </summary>
internal static class CharacterFaces
{
    public const int TextureSize = 128;
    private static readonly Color Ink = new(34, 26, 40, 255);

    /// <summary>Uploads a face texture for every expression, eyes open and shut, as skinned character materials.</summary>
    public static Material[] Materials(CharacterRig rig, FieldShaders shaders)
    {
        var result = new Material[Enum.GetValues<Expression>().Length * 2];
        foreach (var expression in Enum.GetValues<Expression>())
            foreach (bool blink in new[] { false, true })
            {
                var tex = Paint(rig.Style, rig.Face, expression, blink).ToTexture();
                Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
                Raylib.SetTextureWrap(tex, TextureWrap.Clamp);
                result[(int)expression * 2 + (blink ? 1 : 0)] = RenderContext.MaterialFor(shaders.CharacterSkinned, tex);
            }
        return result;
    }

    // ------------------------------------------------------------------ smooth faces (battle)

    /// <summary>
    /// Paints a face texture. It covers the square <see cref="FaceLayout.TextureCenter"/> ± half its size on the
    /// front of the head, in model units; features are placed from the face's anchor between the eyes.
    /// </summary>
    public static PixelCanvas Paint(CharacterStyle s, FaceLayout face, Expression expression, bool blink)
    {
        const int S = TextureSize;
        var canvas = new PixelCanvas(S, S);
        float k = S / (2f * face.TextureHalfSize);           // pixels per model unit
        var c = face.TextureCenter;
        Vector2 Px(float x, float y) => new(S * 0.5f + (x - c.X) * k, S * 0.5f - (y - c.Y) * k);

        bool adult = s.Build == BodyBuild.Adult;
        float eyeW = (adult ? 0.029f : 0.034f) * k, eyeH = (adult ? 0.043f : 0.05f) * k;
        float eyeY = face.Anchor.Y;
        var dark = PixelCanvas.Mix(s.Eyes, Ink, 0.55f);
        var iris = s.Eyes;
        var irisLight = PixelCanvas.Light1(s.Eyes, 0.45f);
        var pupil = PixelCanvas.Mix(s.Eyes, Ink, 0.8f);
        var lip = new Color(150, 64, 70, 255);

        foreach (float side in new[] { -1f, 1f })
        {
            var e = Px(side * face.EyeHalfSpacing, eyeY);
            if (blink)
            {
                // Shut: a lid line curving down, with the lashes at its outer end
                AaPaint.Arc(canvas, e + new Vector2(0, eyeH * 0.1f), eyeW * 1.05f, eyeH * 0.45f, MathF.PI * 0.08f, MathF.PI * 0.92f, 1.6f, dark);
                continue;
            }
            float w = eyeW, h = eyeH;
            if (expression == Expression.Surprised) { w *= 1.08f; h *= 1.12f; }
            if (expression == Expression.Happy) h *= 0.82f;

            // Dark rim, the iris lighter toward its bottom, the pupil, then two highlights
            AaPaint.Ellipse(canvas, e, w, h, dark);
            for (int y = (int)(e.Y - h); y <= (int)(e.Y + h) + 1; y++)
            {
                float t = Math.Clamp((y - (e.Y - h * 0.4f)) / (h * 1.4f), 0f, 1f);
                var col = PixelCanvas.Mix(iris, irisLight, t * 0.8f);
                AaPaint.EllipseRow(canvas, e + new Vector2(0, h * 0.12f), w * 0.74f, h * 0.76f, y, col);
            }
            float pupilScale = expression == Expression.Surprised ? 0.6f : 1f;
            AaPaint.Ellipse(canvas, e + new Vector2(0, h * 0.12f), w * 0.38f * pupilScale, h * 0.42f * pupilScale, pupil);
            AaPaint.Ellipse(canvas, e + new Vector2(-w * 0.3f, -h * 0.36f), w * 0.34f, w * 0.34f, Color.White);
            AaPaint.Ellipse(canvas, e + new Vector2(w * 0.32f, h * 0.38f), w * 0.15f, w * 0.15f, Color.White);

            // The upper lid line, heavier than the rim, with a flick at the outer corner for long lashes
            AaPaint.Arc(canvas, e, w * 1.08f, h * 1.02f, MathF.PI * 1.08f, MathF.PI * 1.92f, adult ? 1.8f : 2.2f, Ink);
            if (s.Lashes)
                AaPaint.Line(canvas, e + new Vector2(side * w * 0.95f, -h * 0.45f), e + new Vector2(side * w * 1.45f, -h * 0.75f), 1.5f, Ink);
            if (expression == Expression.Angry)
            {
                // A lid pressed down at the inner corner
                AaPaint.Line(canvas, e + new Vector2(-side * w * 1.1f, -h * 0.55f), e + new Vector2(side * w * 1.1f, -h * 1.05f), 2.4f, Ink);
            }
        }

        // Brows: only where the bangs leave the brow bare; the professor's are sculpted
        if (!s.BushyBrows)
        {
            var brow = PixelCanvas.Shadow(s.HairColor, 0.35f);
            foreach (float side in new[] { -1f, 1f })
            {
                var b = Px(side * face.EyeHalfSpacing, eyeY + (adult ? 0.068f : 0.078f));
                float rise = expression switch { Expression.Surprised => -0.25f * eyeH, Expression.Sad => 0f, _ => 0f };
                float tiltIn = expression switch { Expression.Angry => 0.35f * eyeH, Expression.Sad => -0.35f * eyeH, _ => 0f };
                AaPaint.Line(canvas, b + new Vector2(-side * eyeW * 0.9f, rise + tiltIn), b + new Vector2(side * eyeW * 0.9f, rise - tiltIn * 0.3f), 1.8f, brow);
            }
        }

        // Mouth (under the moustache, if there is one)
        var m = Px(0, eyeY - (adult ? 0.085f : 0.095f) - (s.Mustache ? 0.035f : 0f));
        float mw = (adult ? 0.026f : 0.03f) * k;
        switch (expression)
        {
            case Expression.Happy:
                // An open smile
                AaPaint.HalfDisc(canvas, m - new Vector2(0, mw * 0.15f), mw * 1.2f, mw * 0.9f, lip);
                AaPaint.Ellipse(canvas, m + new Vector2(0, mw * 0.42f), mw * 0.6f, mw * 0.3f, new Color(232, 120, 132, 255));
                break;
            case Expression.Surprised:
                AaPaint.Ellipse(canvas, m, mw * 0.55f, mw * 0.7f, lip);
                break;
            case Expression.Sad:
                AaPaint.Arc(canvas, m + new Vector2(0, mw * 0.6f), mw * 0.9f, mw * 0.5f, MathF.PI * 1.15f, MathF.PI * 1.85f, 1.6f, lip);
                break;
            case Expression.Angry:
                AaPaint.Line(canvas, m + new Vector2(-mw * 0.8f, 0), m + new Vector2(mw * 0.8f, 0), 1.7f, lip);
                break;
            default:
                if (s.Mustache) break;
                AaPaint.Arc(canvas, m - new Vector2(0, mw * 0.4f), mw * 0.8f, mw * 0.5f, MathF.PI * 0.15f, MathF.PI * 0.85f, 1.6f, lip);
                break;
        }
        return canvas;
    }

    // ------------------------------------------------------------------ pixel faces (field sprites)

    /// <summary>
    /// Stamps a pixel face onto a baked sprite. <paramref name="anchor"/> is where the point between the eyes
    /// landed in the sprite (pixels, y down); <paramref name="facing"/> is 0 front, 1 facing right, 2 back,
    /// 3 facing left. Eyes are two texels wide and three tall with a highlight on the upper left; the mouth is
    /// a texel or three.
    /// </summary>
    public static void Stamp(PixelCanvas sprite, Vector2 anchor, int facing, CharacterStyle s, Expression expression, bool blink)
    {
        if (facing == 2) return;
        var lash = PixelCanvas.Mix(s.Eyes, Ink, 0.6f);
        var iris = PixelCanvas.Light1(s.Eyes, 0.15f);
        var lip = new Color(150, 64, 70, 255);
        int cx = (int)MathF.Floor(anchor.X);
        int ey = (int)MathF.Round(anchor.Y) - 1;
        int mouthY = ey + 5;

        void Eye(int x0, bool highlightLeft)
        {
            if (blink)
            {
                sprite.SetRaw(x0, ey + 1, lash);
                sprite.SetRaw(x0 + 1, ey + 1, lash);
                return;
            }
            sprite.SetRaw(x0, ey, lash);
            sprite.SetRaw(x0 + 1, ey, lash);
            if (expression == Expression.Happy)
            {
                // Squinting with joy: the lower half is just iris
                sprite.SetRaw(x0, ey + 1, iris);
                sprite.SetRaw(x0 + 1, ey + 1, iris);
                return;
            }
            sprite.SetRaw(highlightLeft ? x0 : x0 + 1, ey + 1, Color.White);
            sprite.SetRaw(highlightLeft ? x0 + 1 : x0, ey + 1, iris);
            sprite.SetRaw(x0, ey + 2, iris);
            sprite.SetRaw(x0 + 1, ey + 2, iris);
        }

        if (facing == 0)
        {
            Eye(cx - 3, true);
            Eye(cx + 2, true);
            if (s.Lashes && !blink)
            {
                sprite.SetRaw(cx - 4, ey, lash);
                sprite.SetRaw(cx + 4, ey, lash);
            }
            if (s.Mustache) return;
            switch (expression)
            {
                case Expression.Happy:
                    sprite.SetRaw(cx - 1, mouthY, lip);
                    sprite.SetRaw(cx + 1, mouthY, lip);
                    sprite.SetRaw(cx, mouthY + 1, lip);
                    break;
                case Expression.Surprised:
                    sprite.SetRaw(cx, mouthY, lip);
                    sprite.SetRaw(cx, mouthY + 1, lip);
                    break;
                case Expression.Sad:
                    sprite.SetRaw(cx - 1, mouthY + 1, lip);
                    sprite.SetRaw(cx, mouthY, lip);
                    sprite.SetRaw(cx + 1, mouthY + 1, lip);
                    break;
                default:
                    sprite.SetRaw(cx, mouthY, lip);
                    break;
            }
            return;
        }

        // In profile: one eye a little behind the front of the face, the mouth just under it
        bool right = facing == 1;
        int eyeX = right ? cx - 3 : cx + 2;
        Eye(eyeX, !right);
        if (s.Lashes && !blink) sprite.SetRaw(right ? eyeX - 1 : eyeX + 2, ey, lash);
        if (!s.Mustache) sprite.SetRaw(right ? cx - 1 : cx + 1, mouthY, lip);
    }
}
