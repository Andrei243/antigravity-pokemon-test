# Impidimp sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/impidimp.py -- <the export's Impidimp.glb> <out.glb>
#
# From the references: a little pink imp that is mostly a great round head (half its height); a dark navy fringe of
# hair over the front of the face, its lower edge in scallops, the two big round white eyes with small black pupils set
# in it; a small red nose between the eyes; a wide open grin under it with a fang at each corner and a long dark
# tongue hanging out over the chest; two big cupped ears rising up and out from the head's upper sides, red inside; a
# small thin pink body; thin arms (flung wide in the art, carried forward and down here) with three long fingers and
# a thumb; thin legs on big flat oval feet; a thin dark tail curling up behind to a pointed blade beside the head.
# The pictures overrule the old C# sculpt: no horns, a pink (not dark) body, the fringe only on the face's front.
#
# Reference ratios (3D picture): head height / total 0.49 (built 0.50 and its radii a tenth over the first lay-out),
# head width / body width 3.7 (built 2.9, the chest made thicker than the picture's for the thickness floor), ear length / head height 0.55 (built 0.56), foot length / total 0.2.
#
# The line's frame (Morgrem and Grimmsnarl import it): on_head (a point and normal on the head by its angles), ear
# (a pointed cup with its lining as a second loose piece), fang, claw, eye_patch and the lid colours.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, outward, srgb, inside, smoothstep, unit, polygon, eye_look, args)

PINK, FRINGE, RED, LINING = "#E46AA6", "#2E2660", "#E0344E", "#E8404E"
WHITE, PUPIL, MOUTH, TONGUE = "#F6F4F2", "#141018", "#7A1A30", "#33285A"
FANG, LIDLINE = "#F4F0EA", "#F6B4D2"

H = np.array([0.0, -0.012, 0.292])            # the head's centre and radii
HR = np.array([0.105, 0.097, 0.099])


def on_head(a, e, c=H, r=HR):
    """The point of an ellipsoid head at azimuth a (radians from the front, + toward +X) and elevation e, and the
    surface's normal there."""
    q = np.array([math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)])
    p = np.array(c, float) + q * np.array(r, float)
    n = q / np.array(r, float)
    return p, n / np.linalg.norm(n)


def head_angles(P, c=H, r=HR):
    """Each point's azimuth and elevation about an ellipsoid head's centre."""
    q = (P - np.array(c)) / np.array(r)
    a = np.arctan2(q[:, 0], -q[:, 1])
    e = np.arctan2(q[:, 2], np.hypot(q[:, 0], q[:, 1]))
    return a, e


def frame(d, n):
    """A right-handed frame (across, normal, d) for a part along d whose face looks along n."""
    d = unit(*d)
    n = np.array(n, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    x = np.cross(n, d)
    return x, n, d


def ear(k, name, base, d, n, length, width, thick, colour, lining, bone, cup=0.35, curl=0.0, surface=None):
    """
    A pointed cupped ear from base along d, its hollow looking along n: a flat ellipsoid narrowing to its tip and
    bent into a cup, with a lining of the other colour as a second loose piece standing out of the hollow's face.
    curl bends the tip toward the back of the hollow (-n) by that share of the length.
    """
    x, nn, dd = frame(d, n)

    def shape(t_scale, w_scale, lift):
        def deform(V):
            V = V.copy()
            t = (V[:, 2] + 1) / 2
            w = np.sqrt(np.clip(t * 1.6, 0, 1)) * (1 - np.clip(t, 0, 1) ** 1.8) * 1.15
            V[:, 0] *= w
            V[:, 1] *= 0.4 + 0.6 * w
            return V
        V, F = ell((0, 0, 0), (width * w_scale, thick, length / 2 * t_scale), deform=deform, sub=4)
        V[:, 2] += length / 2 * t_scale
        # the cup: the edges come forward, the middle stays back; the tip curls back
        t = np.clip(V[:, 2] / length, 0, 1)
        V[:, 1] += cup * (V[:, 0] / width) ** 2 * width * 0.5 + lift - curl * length * t ** 2
        return outward((V[:, :1] * x + V[:, 1:2] * nn + V[:, 2:3] * dd + np.array(base, float), F))

    k.add(name, shape(1.0, 1.0, 0.0), colour, bone, fuse=False, surface=surface)
    k.add(name + "lining", shape(0.8, 0.7, thick * 0.75), lining, bone, fuse=False, surface=surface)


def fang(base, d, length, width, thick, toward=(0, -1, 0)):
    """A tooth or fang: a flat-faced kite at its root to a point along d, its flat face looking along toward."""
    x, n, dd = frame(d, toward)
    base = np.array(base, float)
    mid = base + dd * length * 0.4
    return hull([base + x * width / 2, base - x * width / 2, base + n * thick / 2, base - n * thick / 2,
                 mid + x * width * 0.32, mid - x * width * 0.32, mid + n * thick * 0.3, mid - n * thick * 0.3,
                 base + dd * length])


def claw(base, d, length, width, thick, up=(0, 0, 1)):
    """A claw or finger's point: like a fang, its flat face looking along up."""
    return fang(base, d, length, width, thick, toward=up)


def eye_patch(white, pupil, pupil_at=(0.0, -0.05), pupil_r=0.34):
    """The open eye's colour function: a white disc with a solid round pupil."""
    CW, CP = srgb(white), srgb(pupil)

    def look(u, v):
        c = np.tile(CW, (len(u), 1))
        c[(u - pupil_at[0]) ** 2 + ((v - pupil_at[1]) * 0.95) ** 2 < pupil_r ** 2] = CP
        return c
    return look


EYE_A, EYE_E, EYE_HALF = 0.49, -0.05, (0.034, 0.04)


def lay_eyes(k, c, r, ea, ee, half, white, pupil, lid, lidline, pupil_r=0.3, pupil_dv=-0.12, tall=1.0,
             outline=None, pupil_du=-0.06, eps=0.002):
    """
    The line's eyes: a white patch on the head at azimuth ea and elevation ee, and a solid round pupil as a patch of
    its own over it (a dot drawn inside the white's colour function comes out a star); shut states drawn on the lid.
    In Fierce the pupil drops under the slanting lid. outline: the white's corners (u outward, v up) for an eye that
    is no ellipse (an almond slanting to the nose); the two eyes are then laid by hand, the left one's turned round.
    """
    ep, en = on_head(ea, ee, c, r)
    CW, CL, CI = srgb(white), srgb(lid), srgb(lidline)
    plain = lambda u, v: np.tile(CW, (len(u), 1))
    # the white's own frame, as the kit lays it: u across (outward on +X), v up
    up = np.array([0, 0, 1.0]); right = np.cross(up, en); right /= np.linalg.norm(right); vup = np.cross(en, right)
    mx = np.array([-1, 1, 1.0])

    def lay(state):
        look = eye_look(state, plain, CL, CI, line=0.15)
        if outline is None:
            k.decal(ep, en, half, look, eps=eps, rings=20, segs=56, mirror=True)
        else:
            k.decal(ep, en, half, look, eps=eps, rings=20, segs=56, outline=polygon(outline))
            k.decal(ep * mx, en * mx, half, lambda u, v: look(-u, v), eps=eps, rings=20, segs=56,
                    outline=polygon([(-a, b) for a, b in outline]))
        if state in ("Open", "Fierce"):
            dv = pupil_dv + (-0.2 if state == "Fierce" else 0.0)
            pc = ep + vup * dv * half[1] + right * pupil_du * half[0] - en * 0.001
            pr = pupil_r * half[0]
            k.decal(pc, en, (pr, pr * tall), srgb(pupil), eps=eps + 0.0015, rings=8, segs=40, mirror=True)
    return lay


def impidimp(src, out_path):
    k = Kit(src, coat="fur")

    # --- the head: a great ball, a little wider than deep ------------------------------------------------------
    k.add("head", ell(H, HR), PINK, "head")
    k.add("neck", tube([(0, 0.0, 0.165), (0, -0.004, 0.215)], [0.024, 0.028]), PINK, "head")

    # --- the body: small and thin, a chest over a narrow waist and small hips ----------------------------------
    k.add("chest", ell((0, 0.004, 0.172), (0.036, 0.03, 0.04)), PINK, "body")
    k.add("hips", ell((0, 0.008, 0.128), (0.034, 0.029, 0.03)), PINK, "body")

    # --- the legs: thin, the knees bent forward, big flat oval feet ---------------------------------------------
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", tube(np.array([(0.026, 0.004, 0.128), (0.048, -0.028, 0.075)]) * m, [0.018, 0.015]),
              PINK, f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.048, -0.028, 0.075), (0.054, 0.0, 0.03)]) * m, [0.015, 0.0135]),
              PINK, f"leg{side}")
        k.add(f"foot{side}", ell(np.array([0.056, -0.03, 0.0125]) * m, (0.027, 0.045, 0.0125), rot=(0, 0, -8 * s)),
              PINK, f"leg{side}")

    # --- the arms: thin, carried forward and a little down, elbows bent; three long fingers and a thumb --------
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        SH, EL, WR = np.array([0.03, 0.0, 0.185]), np.array([0.074, -0.032, 0.158]), np.array([0.078, -0.08, 0.14])
        k.add(f"upper{side}", tube(np.array([SH, EL]) * m, [0.0125, 0.0115]), PINK, f"arm{side}")
        k.add(f"fore{side}", tube(np.array([EL, WR]) * m, [0.0115, 0.011]), PINK, f"arm{side}")
        PALM = WR + np.array([0.003, -0.012, -0.004])
        k.add(f"palm{side}", ell(PALM * m, (0.017, 0.015, 0.011)), PINK, f"arm{side}")
        for j, d in enumerate([(0.55, -0.7, -0.35), (0.05, -0.85, -0.4), (-0.5, -0.7, -0.4)]):
            d = unit(*d)
            a = PALM + d * 0.01
            b = a + d * 0.022
            c_ = b + d * 0.012 + np.array([0, 0, -0.006])
            k.add(f"finger{side}{j}", tube(np.array([a, b, c_]) * m, [0.0078, 0.0075, 0.007]), PINK, f"arm{side}")
        th = PALM + np.array([-0.012, -0.004, 0.006])
        k.add(f"thumb{side}", tube(np.array([th, th + (-0.014, -0.012, 0.004)]) * m, [0.0075, 0.007]), PINK,
              f"arm{side}")

    # --- the tail: thin and dark, curling up behind on +X to a pointed blade beside the head ---------------------
    TP = [(0.0, 0.03, 0.125), (0.03, 0.075, 0.13), (0.075, 0.1, 0.17), (0.1, 0.1, 0.225)]
    k.add("tail", tube(TP, [0.0085, 0.0078, 0.0072, 0.0068]), FRINGE, "tail")
    tip = np.array(TP[-1])
    d = unit(0.25, -0.05, 1)
    k.add("tailblade", hull([tip + (-0.006, 0.0, -0.01), tip + (0.006, 0.0, -0.01), tip + (0, 0.0045, -0.004),
                             tip + (0, -0.0045, -0.004), tip + (-0.018, 0.0, 0.02), tip + (0.016, 0.0, 0.016),
                             tip + (0.0, 0.004, 0.022), tip + (0.0, -0.004, 0.022), tip + d * 0.058]),
          FRINGE, "tail", fuse=False, surface="skin")

    # --- the ears: big cups up and out from the head's upper sides, red inside ----------------------------------
    for s, side in ((1, "R"), (-1, "L")):
        base, nrm = on_head(s * 1.12, 0.42)
        ear(k, f"ear{side}", base - nrm * 0.02, (s * 0.72, 0.06, 0.7), (s * 0.35, -0.94, 0.05), 0.125, 0.05, 0.011,
            PINK, LINING, "head", cup=0.5, curl=0.06)

    # --- the fringe: dark over the face's front, its top an arc, its lower edge in scallops ------------------------
    def fringe(P):
        a, e = head_angles(P)
        A = np.abs(a)
        top = 0.74 - 0.42 * (A / 1.2) ** 2
        bottom = -0.56 + 0.2 * np.abs(np.sin(A * math.pi / 0.44))
        bottom = np.where(A < 0.13, -0.5, bottom)                  # the point down between the eyes, to the nose
        side = 1.22 - 0.1 * np.clip(e, 0, 1)
        front = (P[:, 1] < H[1] + 0.03)
        return (inside(e - top, 0.012) * inside(bottom - e, 0.012) * inside(A - side, 0.015) * front
                * (P[:, 2] > H[2] - 0.09))
    k.paint(fringe, FRINGE, on=["head"])

    # --- the nose: a small red triangle on the fringe's point between the eyes ---------------------------------
    p, n = on_head(0.0, -0.3)
    k.decal(p, n, (0.012, 0.011), srgb(RED), eps=0.0025, rings=6, segs=36,
            outline=polygon([(-1, 0.7), (1, 0.7), (0.0, -1.0)]))

    # --- the mouth: a wide open grin, dark red, with a fang at each upper corner ----------------------------------
    p, n = on_head(0.0, -0.66)
    k.decal(p, unit(0, -1, -0.35), (0.06, 0.026), srgb(MOUTH), eps=0.0025, rings=12, segs=72,
            outline=polygon([(-1, 0.75), (-0.5, 0.45), (0, 0.38), (0.5, 0.45), (1, 0.75), (0.72, -0.15),
                             (0.38, -0.7), (0, -0.88), (-0.38, -0.7), (-0.72, -0.15)]))
    for s, side in ((1, "R"), (-1, "L")):
        fp, fn = on_head(s * 0.4, -0.6)
        k.add(f"fang{side}", fang(fp - fn * 0.004 + (0, 0, 0.002), (s * 0.05, -0.15, -1), 0.024, 0.013, 0.008,
                                  toward=fn), FANG, "head", fuse=False, surface="shell")
    # the tongue: long and dark, hanging out of the grin over the chest
    mp, mn = on_head(0.0, -0.74)
    T0 = mp - mn * 0.008
    k.add("tongue", outward(blade([T0, T0 + (0, -0.012, -0.022), T0 + (0, -0.014, -0.048), T0 + (0, -0.008, -0.068)],
                                  [0.013, 0.0145, 0.013, 0.008], (0, 1, 0.25), 0.5)),
          TONGUE, "head", fuse=False, surface="skin")

    k.build(voxel=0.004, smooth=4, band=0.006, wband=0.03)

    # --- the eyes: big round whites set in the fringe, a small solid pupil; the lid the fringe's dark ------------
    lay = lay_eyes(k, H, HR, EYE_A, EYE_E, EYE_HALF, WHITE, PUPIL, FRINGE, LIDLINE)
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    impidimp(*args())
