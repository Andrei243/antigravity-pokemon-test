# Spiridel (No. 10027, Carpathia) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/spiridel.py -- <the export's Spiridel.glb> <out.glb>
#
# From its picture (docs/art/reference/gemini/carpathia/10027-Spiridel.jpg) and its brief (CP-dex-1-plateau.md, 27):
# a tiny mischievous house sprite. Largest first: a red knitted cap whose turned-up cuff sits low over the brow and
# whose top flops over to the viewer's left (-X) down to ear height; a big round tan head; a wooden spoon bigger than
# itself held across the body in both hands, its bowl up by the head on +X; two big pointed ears out sideways and a
# little up, pink inside; a long drooping pinkish nose; bright green almond eyes under brows that slope to the nose;
# a sly lopsided smile and pink cheeks; a pear of a body with a paler belly; short stubby legs; a thin tail curling
# out on -X to a little candle's flame.
#
# Written to be grown from (spiriduke.py imports it with sibling("spiridel")): the palette, on_head, the ear, the cap
# and its cuff, the nose, the almond eyes, the smile and the flame are functions here; the calls under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, lathe, axes, outward, srgb, inside, smoothstep, unit, polygon, eye_look, args)

# --- the line's colours -----------------------------------------------------------------------------------------
SKIN, BELLY, NOSE, EARIN = "#D8AA7C", "#CDBE8C", "#E59A7A", "#EC9E92"
CAP, CUFF = "#C8513B", "#B4442F"
WOOD, WOODIN = "#A87646", "#7A522E"
BROW, EYE, EYERIM, PUPIL, GLINT = "#8E6444", "#4EF0A4", "#0F8E58", "#0C2016", "#F2FFF8"
MOUTH, BLUSH = "#5E3026", "#E8907A"
FLAME, FLAMEIN = "#FF8C3C", "#FFE592"

# --- Spiridel's frame (the skeleton: body 0.17, head 0.27, ears 0.38, arms (+-0.09, -0.02, 0.22), legs 0.09) ----
HC = np.array([0.0, -0.01, 0.318])
HR = np.array([0.132, 0.118, 0.122])
VOXEL = 0.0045


def on_head(a, e, c=HC, r=HR):
    """The point of an ellipsoid at azimuth a (radians from the front, + toward +X) and elevation e, and its normal."""
    q = np.array([math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)])
    p = np.array(c, float) + q * np.array(r, float)
    n = q / np.array(r, float)
    return p, n / np.linalg.norm(n)


def frame(d, n):
    """A right-handed frame (across, normal, d) for a part along d whose face looks along n."""
    d = unit(*d)
    n = np.array(n, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    return np.cross(n, d), n, d


def ell_frame(c, radii, ex, ey, ez, deform=None, sub=4):
    """An ellipsoid with radii along a frame of its own (ex, ey, ez), wound outward."""
    V, F = ell((0, 0, 0), radii, deform=deform, sub=sub)
    M = np.stack([np.array(ex, float), np.array(ey, float), np.array(ez, float)])
    return outward((V @ M + np.array(c, float), F))


def ear(k, name, base, d, n, length, width, thick, colour, lining, bone, cup=0.45, curl=0.03, droop=0.0):
    """A big pointed elf's ear from base along d, its hollow looking along n, a pink lining as a second loose piece
    standing out of the hollow. droop bends the tip down by that share of the length."""
    x, nn, dd = frame(d, n)

    def shape(t_scale, w_scale, lift):
        def deform(V):
            V = V.copy()
            t = (V[:, 2] + 1) / 2
            w = np.sqrt(np.clip(t * 2.2, 0, 1)) * (1 - np.clip(t, 0, 1) ** 1.4) * 1.2
            V[:, 0] *= w
            V[:, 1] *= 0.45 + 0.55 * w
            return V
        V, F = ell((0, 0, 0), (width * w_scale, thick, length / 2 * t_scale), deform=deform, sub=4)
        V[:, 2] += length / 2 * t_scale
        t = np.clip(V[:, 2] / length, 0, 1)
        V[:, 1] += cup * (V[:, 0] / width) ** 2 * width * 0.5 + lift - curl * length * t ** 2
        W = V[:, :1] * x + V[:, 1:2] * nn + V[:, 2:3] * dd + np.array(base, float)
        W[:, 2] -= droop * length * t ** 2
        return outward((W, F))

    k.add(name, shape(1.0, 1.0, 0.0), colour, bone, fuse=False)
    k.add(name + "lining", shape(0.78, 0.62, thick * 0.8), lining, bone, fuse=False)


def ring(c, axis, r_in, r_out, below, above, squash=1.0, seg=56):
    """A thick round band about an axis (a knitted cap's turned-up cuff): its section a rounded box from below to
    above along the axis, r_in to r_out from it; squash narrows it front to back."""
    prof = [(r_in, below), (r_out - 0.006, below), (r_out, below + 0.008), (r_out + 0.003, (below + above) / 2),
            (r_out, above - 0.008), (r_out - 0.006, above), (r_in, above), (r_in, below)]
    V, F = lathe(c, axis, prof, seg=seg)
    V = np.array(c) + (V - np.array(c)) * np.array([1.0, squash, 1.0])
    return outward((V, F))


def torus(c, axis, R, r, seg=48, nr=14):
    prof = [(R + r * math.cos(t), r * math.sin(t)) for t in np.linspace(0, 2 * math.pi, nr + 1)]
    return lathe(c, axis, prof, seg=seg)


def teardrop(V):
    """The unit sphere drawn up into a flame's point at +z."""
    V = V.copy()
    z = V[:, 2]
    w = np.where(z < 0, 1.0, np.clip(1 - z, 0, 1) ** 0.85)
    V[:, 0] *= w; V[:, 1] *= w
    return V


def flame(k, name, base, height, width, bone, lean=(0.0, 0.0)):
    """A small candle's flame: an orange teardrop with a yellow heart standing in its front, both giving light."""
    b = np.array(base, float)
    ez = unit(lean[0], lean[1], 1.0)
    ex, ey = axes(ez)
    k.add(name, ell_frame(b + ez * height * 0.45, (width, width * 0.85, height * 0.55), ex, ey, ez, teardrop, sub=3),
          FLAME, bone, fuse=False, glow=FLAME)
    k.add(name + "in", ell_frame(b + ez * height * 0.36 - ey * width * 0.4 - ex * width * 0.05,
                                 (width * 0.55, width * 0.5, height * 0.34), ex, ey, ez, teardrop, sub=3),
          FLAMEIN, bone, fuse=False, glow=FLAMEIN)


def cap(k, top, top_r, flop, flop_r, cuff_c, cuff_axis, r_in, r_out, below, above, squash, cut, bone="head"):
    """The knitted cap: a fused dome over the head and a tube flopping over, under a loose turned-up cuff. Paint
    makes the line where the cap meets the head crisp, under the cuff (cut along the cuff's axis)."""
    k.add("capdome", ell(top, top_r), CAP, bone, surface="cloth")
    k.add("capflop", tube(flop, flop_r, seg=20), CAP, bone, surface="cloth")
    k.add("cuff", ring(cuff_c, cuff_axis, r_in, r_out, below, above, squash), CUFF, bone, fuse=False, surface="cloth")
    A = unit(*cuff_axis); C0 = np.array(cuff_c, float)
    k.paint(lambda P: inside(cut - (P - C0) @ A, 0.002), CAP, on=["head", "cap"])


def nose(k, pts, radii, bone="head"):
    """A long drooping nose fused into the face."""
    k.add("nose", tube(pts, radii, seg=20), NOSE, bone)


def almond(c, r, a, e, half, slant=0.4):
    """The almond eye's outline (u outward, v up) whose top line slopes down toward the nose."""
    return [(-1.0, -0.08), (-0.6, 0.34), (0.05, 0.62 + 0.18 * slant), (0.7, 0.8), (1.0, 0.38),
            (0.82, -0.32), (0.25, -0.72), (-0.45, -0.58)]


def radial(outline):
    ol = polygon(outline)
    def r(u, v):
        th = np.arctan2(v, u)
        return np.hypot(u, v) / np.array([ol(t) for t in th])
    return r


def lay_eyes(k, c, r, ea, ee, half, outline, lid, ink, pupil_r=0.36, pupil_dv=-0.04, glint=True,
             brow=None, eps=0.0022):
    """
    Bright green almond eyes: the white is all iris, a darker green rim a sixth of the eye, a solid dark pupil as a
    patch of its own and a glint over it (Open). Laid by hand on both sides (the outline is not symmetric), the left
    one's outline turned round. brow: (lift over the eye, half sizes, colour) of a slanting brow mark, in every state.
    """
    ep, en = on_head(ea, ee, c, r)
    CE, CR_, CL, CI = srgb(EYE), srgb(EYERIM), srgb(lid), srgb(ink)
    rad = radial(outline)

    def open_look(u, v):
        col = np.tile(CE, (len(u), 1))
        col[rad(u, v) > 0.8] = CR_
        return col
    up = np.array([0, 0, 1.0]); right = np.cross(up, en); right /= np.linalg.norm(right); vup = np.cross(en, right)
    mx = np.array([-1, 1, 1.0])
    flip = [(-a, b) for a, b in outline]

    def lay(state):
        look = eye_look(state, open_look, CL, CI, line=0.15)
        k.decal(ep, en, half, look, eps=eps, rings=20, segs=56, outline=polygon(outline))
        k.decal(ep * mx, en * mx, half, lambda u, v: look(-u, v), eps=eps, rings=20, segs=56,
                outline=polygon(flip))
        if state in ("Open", "Fierce"):
            dv = pupil_dv + (-0.22 if state == "Fierce" else 0.0)
            pc = ep + vup * dv * half[1] - right * 0.08 * half[0]
            pr = pupil_r * half[0]
            k.decal(pc, en, (pr * 0.85, pr * 1.05), srgb(PUPIL), eps=eps + 0.0015, rings=8, segs=40, mirror=True)
            if glint and state == "Open":
                gc = pc + vup * 0.42 * pr + right * 0.38 * pr
                k.decal(gc, en, (pr * 0.36, pr * 0.36), srgb(GLINT), eps=eps + 0.003, rings=5, segs=28,
                        mirror=True)
    return lay, ep, en, right, vup


def brows(k, ep, en, right, vup, half, lift, colour, tilt=22.0, inward=0.0):
    """A brow over each eye sloping down to the nose by tilt degrees: a bar laid on the face (every state), its
    inner end thinner. inward moves it toward the nose by that share of its half length."""
    mx = np.array([-1, 1, 1.0])
    a = math.radians(tilt)
    out = [(-1.0, -0.55), (1.0, -0.85), (1.0, 1.0), (-1.0, 0.55)]
    for s in (1, -1):
        e, n = (ep, en) if s > 0 else (ep * mx, en * mx)
        r_ = np.cross((0, 0, 1.0), n); r_ /= np.linalg.norm(r_); vu = np.cross(n, r_)
        up = vu * math.cos(a) - s * r_ * math.sin(a)
        bc = e + vu * lift - s * r_ * inward * half[0]
        o = out if s > 0 else [(-p, q) for p, q in out]
        k.decal(bc, n, half, srgb(colour), up=up, eps=0.0024, rings=6, segs=40, outline=polygon(o))


def smile(k, centre, normal, half, up=(0, 0, 1)):
    """A sly lopsided smile, one dark curve rising to +X, on a patch of skin."""
    CS, CM = srgb(SKIN), srgb(MOUTH)
    def look(u, v):
        col = np.tile(CS, (len(u), 1))
        curve = -0.42 + 0.75 * u * u + 0.28 * u
        col[(np.abs(v - curve) < 0.24 + 0.1 * (u > 0.6)) & (np.abs(u) < 0.92)] = CM
        return col
    k.decal(centre, normal, half, look, up=up, eps=0.0022, rings=14, segs=56)


def spiridel(src, out_path):
    k = Kit(src, coat="skin")

    # ---- the body: a pear, a paler belly; a short neck into the big head
    BC, BR = np.array([0.0, 0.004, 0.142]), np.array([0.106, 0.096, 0.108])
    def pear(V):
        V = V.copy(); z = V[:, 2]
        s = 1 + 0.12 * np.clip(-z, 0, 1) - 0.12 * np.clip(z, 0, 1) ** 2
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell(BC, BR, deform=pear), SKIN, "body")
    k.add("neck", tube([(0, 0.0, 0.2), (0, -0.006, 0.252)], [0.062, 0.06]), SKIN, "head")
    k.paint(lambda P: inside(((P[:, 0] / 0.074) ** 2 + ((P[:, 2] - 0.125) / 0.085) ** 2) - 1, 0.04)
            * (P[:, 1] < -0.02) * (P[:, 2] < 0.215), BELLY, on=["body"])

    # ---- the legs: short and stubby, the thighs fused into the body, flat little feet
    k.add("thigh", tube([(0.05, 0.0, 0.1), (0.056, -0.012, 0.036)], [0.036, 0.031]), SKIN, "leg{s}", mirror=True)
    k.add("foot", ell((0.058, -0.03, 0.019), (0.034, 0.049, 0.021)), SKIN, "leg{s}", mirror=True)

    # ---- the head: a big round ball, the cap over its top
    k.add("head", ell(HC, HR), SKIN, "head")
    cap(k, top=(0.0, 0.016, 0.455), top_r=(0.13, 0.122, 0.088),
        flop=[(0.02, 0.02, 0.475), (-0.07, 0.03, 0.515), (-0.17, 0.04, 0.485), (-0.25, 0.045, 0.405), (-0.285, 0.035, 0.33)],
        flop_r=[0.085, 0.074, 0.06, 0.045, 0.033],
        cuff_c=(0.0, 0.004, 0.41), cuff_axis=(0.04, -0.22, 1.0), r_in=0.098, r_out=0.143, below=-0.038,
        above=0.046, squash=0.93, cut=-0.012)

    # ---- the ears: big and pointed, out sideways and a little up, pink inside
    for s, side in ((1, "R"), (-1, "L")):
        base, nrm = on_head(s * 1.32, 0.12)
        ear(k, f"ear{side}", base - nrm * 0.016, (s * 0.86, 0.08, 0.48), (s * 0.2, -1.0, 0.16), 0.2, 0.06,
            0.0095, SKIN, EARIN, f"ear{side}", cup=0.45, curl=0.03)

    # ---- the nose: long, drooping forward and down
    nose(k, [(0.0, -0.1, 0.292), (0.004, -0.148, 0.283), (0.01, -0.186, 0.264), (0.013, -0.204, 0.25)],
         [0.021, 0.019, 0.017, 0.017])

    # ---- the spoon: bigger than itself, held across the body, the bowl up by the head on +X
    P0 = np.array([-0.175, -0.152, 0.036]); P1 = np.array([0.255, -0.152, 0.284])
    d = unit(*(P1 - P0))
    neck_at = P0 + d * (np.linalg.norm(P1 - P0) - 0.082)
    k.add("handle", outward(tube([P0, (P0 + neck_at) / 2, neck_at], [0.0145, 0.0135, 0.0125], seg=18)),
          WOOD, "armR", fuse=False, surface="shell")
    nb = unit(0.0, -1.0, 0.12); nb = nb - d * (nb @ d); nb /= np.linalg.norm(nb)
    xb = np.cross(nb, d)
    k.add("bowl", ell_frame(P1, (0.056, 0.086, 0.021), xb, d, nb),
          WOOD, "armR", fuse=False, surface="shell")
    k.add("bowlin", ell_frame(P1 + nb * 0.011, (0.044, 0.07, 0.012), xb, d, nb), WOODIN, "armR", fuse=False,
          surface="shell")

    # ---- the arms: forward and down to the handle, fists round it
    grips = {1: P0 + d * 0.365, -1: P0 + d * 0.17}
    for s, side in ((1, "R"), (-1, "L")):
        G = grips[s]
        SH = np.array([0.084 * s, -0.012, 0.214])
        EL = np.array([0.126, -0.07, 0.186]) if s > 0 else np.array([-0.104, -0.085, 0.152])
        k.add(f"upper{side}", tube([SH, EL], [0.023, 0.021]), SKIN, f"arm{side}")
        k.add(f"fore{side}", tube([EL, G + np.array([0.0, 0.012, 0.004])], [0.021, 0.02]), SKIN, f"arm{side}")
        k.add(f"fist{side}", ell(G + np.array([0.0, 0.002, 0.003]), (0.026, 0.024, 0.028)), SKIN, f"arm{side}")

    # ---- the tail: thin, curling out on -X to a candle's flame
    TP = [(0.0, 0.085, 0.085), (-0.07, 0.12, 0.08), (-0.165, 0.1, 0.13), (-0.21, 0.055, 0.215)]
    k.add("tail", tube(TP, [0.012, 0.011, 0.0105, 0.01]), SKIN, "tail")
    flame(k, "flame", (-0.212, 0.052, 0.222), 0.09, 0.032, "tail", lean=(-0.15, -0.05))

    # ---- the face: brows, pink cheeks and the sly smile, in every state
    lay, ep, en, right, vup = lay_eyes(k, HC, HR, 0.46, -0.06, (0.029, 0.024), almond(HC, HR, 0.46, -0.06, None),
                                       SKIN, BROW)
    brows(k, ep, en, right, vup, (0.034, 0.0085), 0.021, BROW, tilt=24, inward=0.12)
    for s in (1, -1):
        p, n = on_head(s * 0.8, -0.34)
        k.decal(p, unit(s * 0.45, -1.0, 0.05), (0.024, 0.016), srgb(BLUSH), eps=0.002, rings=8, segs=40)
    p, n = on_head(0.06, -0.52)
    smile(k, p, unit(0.12, -1.0, -0.62), (0.05, 0.02))

    k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.03)
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    spiridel(*args())
