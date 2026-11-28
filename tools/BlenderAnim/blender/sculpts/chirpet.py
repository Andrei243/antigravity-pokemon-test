# Chirpet sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/chirpet.py -- <the export's Chirpet.glb> <out.glb>
#
# The design, from the project's picture (10015-Chirpet.jpg) and brief (CP-dex-1-plateau.md, 15): a small green field
# cricket. Its first mass is a great round green head, wider than tall, as large as the rest of the body above the
# ground; two big dark brown eyes with two white glints each and no white round them, a small dark green brow mark over
# each, yellow blushes under them, a small open smile with a pink tongue. Two long thin pale green antennae rise from
# the crown and arc back over the body. A short thorax, yellow-green in front, and a long abdomen behind, green above
# and yellow-green beneath with segment lines. Over the back two leaf-shaped wing cases rise in a V, darker green,
# ridged like a file and edged yellow along the lower side. Six legs: the front pair held as small arms before the
# chest (rubbing its wings, the brief says; carried forward here), the middle pair thin and standing, and the great
# hind legs, fat drumstick thighs fused to the abdomen's flanks, knees above the back, long spined shins to the ground.
#
# Written to be grown from (fiddlecrick.py imports it): the colours, on_ell, the eye, the mouth, the wing case, the
# antenna and the leg are functions and constants; the build is under __main__.
# Skeleton: body (0, 0.02, 0.13), head (0, -0.06, 0.17), leg{s} (+-0.04, -0.06, 0.11), hind{s} (+-0.05, 0.07, 0.12).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, hull, srgb, inside, unit, eye_look, outward, args

GREEN, GREEN_D, BELLY, LINE = "#8CCB48", "#5E9E36", "#CBDD58", "#9DB53A"
WING, RIDGE, RIM, ANT = "#5FA43A", "#3E7C2A", "#DCD34C", "#86C64C"
BROWN, BROWN_L, WHITE = "#3A2218", "#74503A", "#FFFFFF"
MOUTH, TONGUE, BLUSH = "#A22C36", "#F28C92", "#E6E466"


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) in direction d from its centre, and its outward normal."""
    c, r, d = np.array(c, float), np.array(r, float), unit(*d)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


# --- the line's eye, mouth and marks ---------------------------------------------------------------------------------
def eye_open(sclera=None, iris_r=1.0, lift=0.0):
    """The line's eye: a dark brown iris with a lighter crescent low in it, a big white glint up and outward and a
    small one low and inward; a pale sclera round the iris when given (Fiddlecrick)."""
    CI, CL, CW = srgb(BROWN), srgb(BROWN_L), srgb(WHITE)
    CS = srgb(sclera) if sclera else None
    def eye(u, v):
        c = np.tile(CI, (len(u), 1))
        vv = v - lift
        r = np.sqrt(u * u + vv * vv) / iris_r
        c[(r < 0.92) & (vv < -0.32) & (r > 0.45)] = CL
        if CS is not None:
            c[r > 1.0] = CS
        c[((u - 0.26 * iris_r) / 0.3) ** 2 + ((vv - 0.36 * iris_r) / 0.3) ** 2 < iris_r ** 2] = CW
        c[((u + 0.3 * iris_r) / 0.14) ** 2 + ((vv + 0.34 * iris_r) / 0.14) ** 2 < iris_r ** 2] = CW
        return c
    return eye


def mouth_look(u, v):
    """An open smile: dark red, a pink tongue in its lower part."""
    c = np.tile(srgb(MOUTH), (len(u), 1))
    c[(v < -0.2) & (np.abs(u) < 0.62)] = srgb(TONGUE)
    return c


def smile_outline(th):
    """A D laid flat: a nearly straight top, a round bottom."""
    return 1.0 if math.sin(th) < 0 else 0.42 + 0.58 * abs(math.cos(th)) ** 6


def antenna(k, pts, r0, r1, bone="head", color=ANT, name="antenna"):
    """A thin antenna through pts (written on +X), tapering, loose; mirrored."""
    n = len(pts)
    k.add(name, tube(pts, list(np.linspace(r0, r1, n)), seg=12), color, bone, fuse=False, mirror=True)


def wing_case(k, name, root, d, n0, L, W, thick, bone, ridges=3, rim_lo=True, colours=(WING, RIM, RIDGE), sub=False):
    """A leaf-shaped wing case from root along d (L long, W half wide), its face turned toward n0: a plate, a yellow
    strip along its lower edge and darker ridges across it (the file), each thicker than the one under it so it shows
    on both faces. Written on +X and mirrored."""
    ex = unit(*d)
    ez = np.cross(unit(*n0), ex); ez /= np.linalg.norm(ez)
    low = -1.0 if ez[2] > 0 else 1.0           # the side of the plate that hangs lower
    t = np.linspace(0, 1, 21)
    half = W * np.sin(np.pi * np.clip(t, 0, 1)) ** 0.75 * (1 - 0.25 * t)
    half[0] = half[-1] = 0.002
    up = [(a * L, h) for a, h in zip(t, half)]
    lo = [(a * L, -h) for a, h in zip(t, half)][::-1]
    plate, strip, bars = colours
    k.add(name, slab(up + lo, root, ex, ez, thick), plate, bone, fuse=False, mirror=True)
    # the yellow edge: between the lower outline and 0.62 of it
    tt = np.linspace(0.06, 0.94, 18)
    hh = W * np.sin(np.pi * tt) ** 0.75 * (1 - 0.25 * tt)
    outer = [(a * L, low * h * 1.0) for a, h in zip(tt, hh)]
    inner = [(a * L, low * h * 0.6) for a, h in zip(tt, hh)][::-1]
    k.add(name + "rim", slab(outer + inner, root, ex, ez, thick + 0.0024), strip, bone, fuse=False, mirror=True)
    # the ridges: slanting bars across the plate's upper part
    for i in range(ridges):
        a0 = (0.28 + 0.2 * i) * L
        h0 = W * np.sin(np.pi * a0 / L) ** 0.75 * (1 - 0.25 * a0 / L)
        w = 0.0065
        p0, p1 = (a0, low * 0.45 * h0), (a0 + 0.1 * L, -low * 0.82 * h0)
        e = np.array(p1) - np.array(p0); e /= np.linalg.norm(e)
        o = np.array([-e[1], e[0]]) * w
        bar = [tuple(np.array(p0) + o), tuple(np.array(p1) + o), tuple(np.array(p1) - o), tuple(np.array(p0) - o)]
        k.add(f"{name}ridge{i}", slab(bar, root, ex, ez, thick + 0.003), bars, bone, fuse=False, mirror=True)


def leg(k, name, pts, radii, bone, color=GREEN, mirror=True):
    """A fused leg through pts (written on +X)."""
    k.add(name, tube(pts, radii), color, bone, mirror=mirror)


def spines(k, name, a, b, n, size, bone, color=GREEN_D, back=(0, 1, -0.4)):
    """n small hard spines along the shin from a to b, pointing back: loose hulls; mirrored."""
    a, b = np.array(a, float), np.array(b, float)
    d = unit(*back)
    for i in range(n):
        p = a + (b - a) * (i + 1) / (n + 1)
        ax = unit(*(b - a))
        s1 = np.cross(ax, d); s1 /= np.linalg.norm(s1)
        base = [p + ax * size * 0.6, p - ax * size * 0.6, p + s1 * size * 0.5, p - s1 * size * 0.5]
        k.add(f"{name}{i}", hull(base + [p + d * size * 2.2]), color, bone, fuse=False, mirror=True)


# --- Chirpet -------------------------------------------------------------------------------------------------------
HC, HR = np.array([0.0, -0.1, 0.245]), np.array([0.092, 0.082, 0.08])      # the head
E = 0.002                                                                   # a crisp paint's edge
EYE_D, EYE_HALF = (0.42, -0.86, 0.17), (0.031, 0.036)


def lay_eyes(k, state):
    look = eye_look(state, eye_open(), srgb(GREEN), srgb(BROWN), line=0.16)
    for s in (1, -1):
        p, n = on_ell(HC, HR, (EYE_D[0] * s, EYE_D[1], EYE_D[2]))
        col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
        k.decal(p, n, EYE_HALF, col, up=(-0.12 * s, 0, 1), eps=0.0016, rings=20, segs=56)


def chirpet(src, out):
    k = Kit(src, coat="shell")
    # ---- the head, round, a little wider than tall
    k.add("head", ell(HC, HR, sub=5), GREEN, "head")
    # ---- the thorax and the long abdomen behind it
    k.add("thorax", ell((0, -0.035, 0.168), (0.052, 0.056, 0.056)), GREEN, "body")
    k.add("abdomen", tube([(0, -0.01, 0.152), (0, 0.06, 0.142), (0, 0.13, 0.128), (0, 0.175, 0.118)],
                          [0.056, 0.058, 0.048, 0.028]), GREEN, "body")
    def belly(P):
        zc = 0.152 - (P[:, 1] + 0.01) * 0.18                       # the abdomen's centre line, falling back
        return inside(P[:, 2] - (zc - 0.016), E) * (P[:, 1] > -0.055)
    k.paint(belly, BELLY, on=["abdomen", "thorax"])
    k.paint(lambda P: inside(P[:, 1] + 0.055, E) * inside(P[:, 2] - 0.19, E), BELLY, on=["thorax"])
    def segments(P):
        m = np.zeros(len(P))
        for y in (0.035, 0.075, 0.115):
            m = np.maximum(m, inside(np.abs(P[:, 1] - y) - 0.0018, 0.0008))
        zc = 0.152 - (P[:, 1] + 0.01) * 0.18
        return m * inside(P[:, 2] - (zc - 0.02), E)
    k.paint(segments, LINE, on=["abdomen"])
    # ---- the front pair as small arms held before the chest
    leg(k, "arm", [(0.036, -0.07, 0.172), (0.062, -0.108, 0.15), (0.04, -0.128, 0.176)], [0.0095, 0.009, 0.0085],
        "body")
    k.add("hand", ell((0.034, -0.132, 0.182), (0.012, 0.011, 0.012)), GREEN, "body", mirror=True)
    # ---- the middle pair, thin and standing, knees up and out
    leg(k, "midleg", [(0.03, -0.05, 0.13), (0.075, -0.085, 0.13), (0.092, -0.105, 0.06), (0.097, -0.112, 0.016)],
        [0.011, 0.0098, 0.0092, 0.0088], "leg{s}")
    k.add("midfoot", ell((0.1, -0.124, 0.009), (0.013, 0.021, 0.008)), GREEN_D, "leg{s}", mirror=True)
    # ---- the great hind legs: drumstick thighs on the flanks, knees above the back, long spined shins
    leg(k, "thigh", [(0.036, 0.05, 0.13), (0.066, 0.11, 0.182), (0.08, 0.168, 0.228)], [0.034, 0.032, 0.015],
        "hind{s}")
    leg(k, "shin", [(0.08, 0.17, 0.228), (0.088, 0.19, 0.12), (0.093, 0.205, 0.02)], [0.012, 0.011, 0.0095],
        "hind{s}")
    k.add("hindfoot", ell((0.094, 0.23, 0.009), (0.012, 0.024, 0.008)), GREEN_D, "hind{s}", mirror=True)
    spines(k, "spine", (0.08, 0.17, 0.228), (0.093, 0.205, 0.02), 2, 0.007, "hind{s}")
    # ---- the wing cases, raised in a V over the back, ridged and edged yellow
    wing_case(k, "case", (0.02, -0.01, 0.198), (0.36, 0.6, 0.72), (0.85, -0.3, 0.42), 0.19, 0.062, 0.009, "body")
    # ---- the antennae: thin, rising from the crown and arcing back over the body
    antenna(k, [(0.024, -0.118, 0.31), (0.032, -0.118, 0.36), (0.055, -0.07, 0.405), (0.085, 0.02, 0.418),
                (0.115, 0.11, 0.396)], 0.0078, 0.0064)
    # ---- the face: brows, blushes, the open smile (before the build: they are in every eye state)
    for s in (1, -1):
        p, n = on_ell(HC, HR, (0.43 * s, -0.62, 0.72))
        k.decal(p, n, (0.016, 0.0055), GREEN_D, up=(0.22 * s, 0, 1), eps=0.0016, rings=6, segs=36)
        p, n = on_ell(HC, HR, (0.74 * s, -0.6, -0.2))
        k.decal(p, n, (0.017, 0.011), BLUSH, eps=0.0014, rings=8, segs=40)
    p, n = on_ell(HC, HR, (0, -1, -0.36))
    k.decal(p, n, (0.021, 0.014), mouth_look, eps=0.0016, outline=smile_outline, rings=12, segs=56)
    k.build(voxel=0.0042, smooth=3, band=0.005, wband=0.03)
    k.eye_states(lambda state: lay_eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    chirpet(*args())
