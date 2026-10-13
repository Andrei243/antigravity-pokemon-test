using PokemonPlatinumEngine.Core;
using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Hall of Fame's ceremony and the credits (plan 02 · S14; style guide, "The Hall of Fame's ceremony" and "The
/// credits"): the hall the team is shown in, a Pokémon's card, the Champion among the team under falling paper, and
/// the credits' night, title card, roll and last card.
/// </summary>
internal static partial class ModernUi
{
    private static readonly Color CeremonyGold = new(255, 214, 130, 255), CeremonyGoldDeep = new(186, 124, 30, 255);
    private static readonly Color CeremonyCream = new(255, 244, 224, 255), CreditNoteBlue = new(178, 196, 236, 255);
    // The paper's five colours: the kit's gold, red, blue and green, and cream (written out: another file's colours may
    // not be made yet when this one's are)
    private static readonly Color[] Slips = { new(240, 176, 56, 255), new(232, 72, 76, 255), new(70, 136, 232, 255), new(56, 182, 104, 255), new(255, 244, 224, 255) };

    /// <summary>A number in [0, 1) that is always the same for the same two numbers: where a slip of paper or a mote begins.</summary>
    private static float Scatter(int i, int salt)
    {
        unchecked
        {
            uint h = (uint)i * 374761393u + (uint)salt * 668265263u;
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xFFFFFF) / (float)0x1000000;
        }
    }

    private static void Picture(Texture2D picture, Vector2 c, float width, float height, float alpha = 1f)
    {
        if (width < 1f || alpha <= 0.004f) return;
        Raylib.DrawTexturePro(picture, new Rectangle(0, 0, picture.Width, -picture.Height),
            new Rectangle(c.X - width / 2f, c.Y - height / 2f, width, height), Vector2.Zero, 0f, new Color(255, 255, 255, (int)(255 * Math.Clamp(alpha, 0f, 1f))));
    }

    private static void Centred(string text, float cx, float y, float size, Color color, UiWeight weight) =>
        UiFonts.Draw(text, cx - UiFonts.Measure(text, size, weight) / 2f, y, size, color, weight);

    // ------------------------------------------------------------------ the ceremony

    /// <summary>
    /// The Hall of Fame's stage: night blue falling to wine, a pool of warm light on the floor under <paramref name="c"/>,
    /// and two spotlights from the top corners sweeping across it.
    /// </summary>
    public static void CeremonyStage(int sw, int sh, Vector2 c, float time)
    {
        Raylib.DrawRectangleGradientV(0, 0, sw, sh, new Color(10, 12, 34, 255), new Color(58, 18, 34, 255));

        Raylib.BeginBlendMode(BlendMode.Additive);
        SoftLight(c + new Vector2(0, 300), 1700, 250, Lit(CeremonyGold, 0.30f));
        SoftLight(c + new Vector2(0, 300), 820, 110, Lit(CeremonyGold, 0.34f));
        SoftLight(c, 1100, 1000, Lit(new Color(255, 190, 120, 255), 0.10f));

        // The spotlights: a long soft beam from each top corner to a point that drifts to and fro over the pool
        foreach (int side in new[] { -1, 1 })
        {
            var from = new Vector2(sw / 2f + side * (sw / 2f - 120f), -140f);
            var to = c + new Vector2(side * 60f + MathF.Sin(time * 0.55f + side * 1.1f) * 300f, 250f);
            var along = to - from;
            float length = along.Length() * 1.25f;
            float degrees = MathF.Atan2(along.Y, along.X) * 180f / MathF.PI;
            SoftLight((from + to) / 2f, length, 300f, Lit(CeremonyGold, 0.13f), degrees);
            SoftLight((from + to) / 2f, length, 110f, Lit(Color.White, 0.07f), degrees);
        }
        Raylib.EndBlendMode();
    }

    /// <summary>A soft shadow on the stage's floor under whoever stands at <paramref name="foot"/>.</summary>
    private static void StageShadow(Vector2 foot, float width, float alpha = 1f) =>
        SoftLight(foot, width, width * 0.17f, new Color(6, 2, 14, (int)(170 * Math.Clamp(alpha, 0f, 1f))));

    public static void DrawCeremony(int sw, int sh, HallOfFameCeremony scene, HallOfFameCeremony.Look look, float time,
        Func<int, Texture2D?> pictureOf, Texture2D? champion)
    {
        bool together = scene.Phase is CeremonyPhase.Champion or CeremonyPhase.Out;
        var centre = together ? new Vector2(sw / 2f, 520f) : new Vector2(sw * 0.31f, 500f);
        CeremonyStage(sw, sh, centre, time);

        if (!together && scene.Team.Count > 0)
        {
            // The Pokémon comes in from the left onto the pool, its card from the right
            var pokemon = scene.Team[scene.Member];
            var at = centre + new Vector2(-(1f - look.Slide) * (centre.X + 420f), 0f);
            StageShadow(at + new Vector2(0, 280), 470, look.Slide);
            if (pictureOf(scene.Member) is { } picture) Picture(picture, at, 640, 640);
            CeremonyCardPanel(new Rectangle(1040 + (1f - look.Slide) * 920f, 268, 760, 470), scene.CardOf(pokemon), scene.PartsShown,
                $"{scene.Member + 1} / {scene.Team.Count}");
        }
        else if (together)
        {
            // The team either side, the outermost first so that those nearer the Champion stand before them
            for (int i = scene.Team.Count - 1; i >= 0; i--)
            {
                float slide = scene.TeamSlide(i);
                if (slide <= 0.01f || pictureOf(i) is not { } picture) continue;
                int side = HallOfFameCeremony.SideOf(i), sign = Math.Sign(side), rank = Math.Abs(side) - 1;
                var at = new Vector2(centre.X + sign * (270f + rank * 215f) + sign * (1f - slide) * 1100f, 650f - rank * 16f);
                StageShadow(at + new Vector2(0, 126), 230, slide);
                Picture(picture, at, 300, 300);
            }
            if (champion is { } figure)
            {
                StageShadow(new Vector2(centre.X, 788), 300, look.Figure);
                // Their figure is 600 tall, and rises a little as it comes
                Picture(figure, new Vector2(centre.X, 520f + (1f - look.Figure) * 60f), 600f * figure.Width / figure.Height, 600f, look.Figure);
            }

            if (look.Title > 0.01f)
            {
                string title = Text.Of("ceremony.title");
                const float size = 116f, spacing = 10f;
                float width = UiFonts.MeasureDisplay(title, size, spacing), x = (sw - width) / 2f, y = 54f - (1f - look.Title) * 24f;
                UiFonts.DrawDisplay(title, x + 4, y + 6, size, spacing, new Color(10, 6, 24, (int)(200 * look.Title)));
                float sweep = time % 5.5f;
                UiFonts.DrawDisplayGradient(title, x, y, size, spacing, new Color(255, 236, 170, 255), new Color(226, 160, 60, 255), look.Title,
                    sweep < 1.6f ? x - 200f + (width + 500f) * (sweep / 1.6f) : float.NaN);
                Centred(Text.Of("ceremony.congratulations"), sw / 2f, y + size * 1.0f, 44, CeremonyCream with { A = (byte)(255 * look.Title) }, UiWeight.Black);

                // Who the Champion is: the name, the card's number and the time played
                var pill = new Rectangle(sw / 2f - 520, 936 + (1f - look.Title) * 30f, 1040, 84);
                byte a = (byte)(255 * look.Title);
                UiShapes.Shape(pill, 42, new Color(16, 16, 44, (int)(226 * look.Title)), new Color(30, 14, 40, (int)(226 * look.Title)), CeremonyGold with { A = a }, 3);
                float mid = pill.Y + pill.Height / 2f;
                UiFonts.DrawCentered(scene.Champion, pill.X + 48, mid, 40, Color.White with { A = a }, UiWeight.Black);
                string id = Text.Of("ceremony.id", ("id", scene.ChampionId)), played = Text.Of("ceremony.time", ("time", scene.TimePlayed));
                UiFonts.DrawCentered(id, pill.X + pill.Width / 2f - UiFonts.Measure(id, 30, UiWeight.ExtraBold) / 2f + 40, mid, 30, CeremonyGold with { A = a }, UiWeight.ExtraBold);
                UiFonts.DrawCentered(played, pill.X + pill.Width - 48 - UiFonts.Measure(played, 30, UiWeight.ExtraBold), mid, 30, CeremonyCream with { A = a }, UiWeight.ExtraBold);
            }

            if (look.Confetti >= 0f) Confetti(sw, sh, look.Confetti);
            if (look.Arrow) AdvanceArrow(sw - 150, sh - 96);
        }

        if (look.Black > 0.004f) Raylib.DrawRectangle(0, 0, sw, sh, new Color(0, 0, 0, (int)(255 * Math.Clamp(look.Black, 0f, 1f))));
    }

    /// <summary>
    /// A Pokémon's card in the ceremony: the welcome, its name and level; its species and types; and, under a rule,
    /// who first trained it and where it came from. <paramref name="parts"/> of those three show.
    /// </summary>
    private static void CeremonyCardPanel(Rectangle r, CeremonyCard card, int parts, string count)
    {
        Panel(r, 34, frame: CeremonyGoldDeep);
        float x = r.X + 52, right = r.X + r.Width - 52;
        // The welcome is on the card as it comes; the rest is written onto it part by part
        UiFonts.Draw(Text.Of("ceremony.welcome"), x, r.Y + 36, 22, CeremonyGoldDeep, UiWeight.Black);
        if (parts >= 1)
        {
            // A long nickname gives way to the level beside it
            float size = 64f;
            while (size > 40f && UiFonts.Measure(card.Name, size, UiWeight.Black) > r.Width - 330f) size -= 4f;
            UiFonts.DrawCentered(card.Name, x, r.Y + 122, size, Ink, UiWeight.Black);
            if (card.Gender is Gender.Male or Gender.Female)
                UiIcons.GenderMark(new Vector2(x + UiFonts.Measure(card.Name, size, UiWeight.Black) + size * 0.5f, r.Y + 122), size * 0.7f, card.Gender);
            Level(right, r.Y + 122, card.Level, 46);
        }
        if (parts >= 2)
        {
            UiFonts.DrawCentered(card.Species, x, r.Y + 196, 34, Muted, UiWeight.ExtraBold);
            float px = x;
            foreach (var type in card.Types) px += TypePill(px, r.Y + 228, type, 42, 150) + 12;
        }
        if (parts >= 3)
        {
            UiShapes.Fill(new Rectangle(x, r.Y + 296, r.Width - 104, 3), 1.5f, Rule);
            Label(Text.Of("ceremony.trainer"), x, r.Y + 316);
            UiFonts.Draw(card.Trainer, x, r.Y + 340, 36, Ink, UiWeight.Black);
            string id = Text.Of("ceremony.id", ("id", card.TrainerId));
            UiFonts.Draw(id, right - UiFonts.Measure(id, 28, UiWeight.ExtraBold), r.Y + 348, 28, Muted, UiWeight.ExtraBold);
            DrawWrapped(card.Met, x, r.Y + 398, r.Width - 220, 28, Ink, 34, UiWeight.ExtraBold);
        }
        UiFonts.Draw(count, right - UiFonts.Measure(count, 22, UiWeight.Black), r.Y + r.Height - 46, 22, Muted, UiWeight.Black);
    }

    /// <summary>
    /// Slips of paper falling: ninety, each a function of its number and of how long the paper has been falling, so the
    /// picture is the same every run.
    /// </summary>
    private static void Confetti(int sw, int sh, float age)
    {
        const int slips = 90;
        float span = sh + 80f;
        for (int i = 0; i < slips; i++)
        {
            float speed = 140f + Scatter(i, 1) * 120f;
            float fallen = age * speed - Scatter(i, 2) * span;
            if (fallen < 0f) continue;
            float y = fallen % span - 40f;
            float x = Scatter(i, 3) * sw + MathF.Sin(age * (0.9f + Scatter(i, 4)) + i) * 28f;
            float turn = age * (90f + Scatter(i, 5) * 220f) + i * 40f;
            Raylib.DrawRectanglePro(new Rectangle(x, y, 6, 14), new Vector2(3, 7), turn, Slips[i % Slips.Length]);
        }
    }

    // ------------------------------------------------------------------ the credits

    /// <summary>
    /// Breaks a note of the credits into lines of about the same width, no wider than <paramref name="maxWidth"/>: as
    /// few lines as <see cref="Wrap"/> needs, but never a last line of one word under a full one.
    /// </summary>
    public static List<string> WrapEvenly(string text, float maxWidth, float size, UiWeight weight = UiWeight.Bold)
    {
        var lines = Wrap(text, maxWidth, size, weight);
        if (lines.Count < 2) return lines;
        // The narrowest column that still takes no more lines
        float low = maxWidth / lines.Count, high = maxWidth;
        for (int i = 0; i < 12; i++)
        {
            float middle = (low + high) / 2f;
            if (Wrap(text, middle, size, weight).Count <= lines.Count) high = middle;
            else low = middle;
        }
        return Wrap(text, high, size, weight);
    }

    /// <summary>The credits' night: the dark hall with motes of soft light drifting upward.</summary>
    public static void CreditsBackdrop(int sw, int sh, float time)
    {
        Raylib.DrawRectangleGradientV(0, 0, sw, sh, new Color(8, 10, 30, 255), new Color(22, 30, 78, 255));
        Raylib.BeginBlendMode(BlendMode.Additive);
        float span = sh + 160f;
        for (int i = 0; i < 40; i++)
        {
            float size = 22f + Scatter(i, 11) * 64f;
            float y = sh + 80f - (time * (10f + Scatter(i, 12) * 26f) + Scatter(i, 13) * span) % span;
            float x = Scatter(i, 14) * sw + MathF.Sin(time * 0.3f + i) * 22f;
            SoftLight(new Vector2(x, y), size, size, Lit(EvolutionLight, 0.06f + Scatter(i, 15) * 0.10f));
        }
        Raylib.EndBlendMode();
    }

    /// <summary>The title's own lettering, centred with its top at <paramref name="y"/> (as the title screen draws it).</summary>
    private static void TitleLettering(int sw, float y, float alpha, float time)
    {
        if (alpha <= 0.01f) return;
        const string word = "PLATINUM", series = "Pokémon", sub = "A  FAN  REMAKE";
        const float size = 196f, spacing = 16f, seriesSize = 78f;
        float width = UiFonts.MeasureDisplay(word, size, spacing), x = (sw - width) / 2f;
        UiFonts.DrawDisplay(series, x + 9, y - seriesSize * 0.78f + 4, seriesSize, 2f, new Color(10, 6, 24, (int)(170 * alpha)));
        UiFonts.DrawDisplay(series, x + 6, y - seriesSize * 0.78f, seriesSize, 2f, CeremonyCream with { A = (byte)(255 * alpha) });
        UiFonts.DrawDisplay(word, x + 5, y + 8, size, spacing, new Color(10, 6, 24, (int)(200 * alpha)));
        float sweep = time % 5.5f;
        UiFonts.DrawDisplayGradient(word, x, y, size, spacing, new Color(255, 255, 255, 255), new Color(150, 162, 196, 255), alpha,
            sweep < 1.6f ? x - 200f + (width + 500f) * (sweep / 1.6f) : float.NaN);
        var silver = new Color(214, 222, 240, (int)(230 * alpha));
        float ruleY = y + size * 0.98f;
        UiShapes.Fill(new Rectangle(x + 4, ruleY, width - 8, 4), 2, silver);
        Centred(sub, sw / 2f, ruleY + 22, 38, silver, UiWeight.Black);
    }

    public static void DrawCredits(int sw, int sh, CreditsScreen credits, CreditsScreen.Look look, Texture2D? member, float time)
    {
        CreditsBackdrop(sw, sh, time);
        TitleLettering(sw, 360f, look.Title, time);

        if (credits.Phase == CreditsPhase.Roll)
        {
            // Whoever of the team stands beside the roll, on a pool of light, with its name under it
            if (look.Member >= 0 && look.MemberAlpha > 0.01f)
            {
                var at = new Vector2(sw * 0.25f, 480f);
                Raylib.BeginBlendMode(BlendMode.Additive);
                SoftLight(at + new Vector2(0, 250), 900, 150, Lit(EvolutionLight, 0.26f * look.MemberAlpha));
                SoftLight(at + new Vector2(0, 250), 460, 70, Lit(Color.White, 0.22f * look.MemberAlpha));
                Raylib.EndBlendMode();
                if (member is { } picture) Picture(picture, at, 520, 520, look.MemberAlpha);
                string name = credits.Team[look.Member].DisplayName;
                float w = UiFonts.Measure(name, 30, UiWeight.Black) + 64;
                byte a = (byte)(255 * look.MemberAlpha);
                UiShapes.Fill(new Rectangle(at.X - w / 2f, 790, w, 56), 28, new Color(10, 16, 48, (int)(190 * look.MemberAlpha)));
                Centred(name, at.X, 801, 30, Color.White with { A = a }, UiWeight.Black);
            }

            float cx = sw * 0.64f;
            foreach (var line in credits.Lines)
            {
                float y = sh - credits.Scroll + line.Y;
                if (y < -60f || y > sh + 20f) continue;
                byte a = (byte)(255 * CreditsScreen.LineAlpha(y + 18f));
                if (a == 0) continue;
                switch (line.Kind)
                {
                    case CreditLineKind.Heading:
                        Centred(line.Text, cx, y, CreditsRoll.HeadingSize, Gold with { A = a }, UiWeight.Black);
                        UiShapes.Fill(new Rectangle(cx - 60, y + 40, 120, 3), 1.5f, Gold with { A = (byte)(a * 0.7f) });
                        break;
                    case CreditLineKind.Name:
                        Centred(line.Text, cx, y, CreditsRoll.NameSize, Color.White with { A = a }, UiWeight.ExtraBold);
                        break;
                    default:
                        Centred(line.Text, cx, y, CreditsRoll.NoteSize, CreditNoteBlue with { A = a }, UiWeight.Bold);
                        break;
                }
            }
        }

        if (look.End > 0.01f)
        {
            string end = Data.CreditsDatabase.End;
            const float size = 170f, spacing = 14f;
            float width = UiFonts.MeasureDisplay(end, size, spacing), x = (sw - width) / 2f, y = 360f - (1f - look.End) * 20f;
            UiFonts.DrawDisplay(end, x + 5, y + 8, size, spacing, new Color(10, 6, 24, (int)(200 * look.End)));
            UiFonts.DrawDisplayGradient(end, x, y, size, spacing, new Color(255, 255, 255, 255), new Color(150, 162, 196, 255), look.End);
            Centred(Data.CreditsDatabase.Farewell, sw / 2f, y + size + 40, 44, CeremonyCream with { A = (byte)(255 * look.End) }, UiWeight.Black);
            if (look.Arrow) AdvanceArrow(sw - 150, sh - 96);
        }

        if (look.Black > 0.004f) Raylib.DrawRectangle(0, 0, sw, sh, new Color(0, 0, 0, (int)(255 * Math.Clamp(look.Black, 0f, 1f))));
    }
}
