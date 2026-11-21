# Braviary-Hisui sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown
# from Braviary's script (braviary.py, imported: its frame, beak, wing axes and outline, tail fan and taloned feet):
#   blender --background --factory-startup --python sculpts/braviaryhisui.py -- <the export's Braviary-Hisui.glb> <out.glb>
# From the references: Braviary's eagle in the air, its body a dark charcoal grey with a white streak down each flank;
# the head wholly under a great shaggy hood of white feathers flaring up and back like flames, the head's largest mass;
# over the brow a violet mask shaped like flames with a teal almond over each eye; a small teal-grey eye under it; the
# big hooked yellow beak open on a pink mouth; larger wings raised in a V, their inner half dark grey with a toothed
# edge over the white flight feathers; a white fan of a tail; shaggy dark thighs and yellow feet with dark talons.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, slab, srgb, inside, smoothstep, unit, eye_look, polygon, args, sibling

b_ = sibling("braviary")
p = b_.p
H, HR, SH = b_.H, b_.HR, b_.SH

DARK = "#4A4A52"
WHITE = "#F2F2F4"
PALE = "#D6D6DE"              # the white feathers' shade, toward their tips and in the tail
VIOLET = "#A660C8"
LILAC = "#D9A6E6"
TEAL = "#5ED2CC"
EYEC = "#5A8C92"
GROW = 1.15                    # the wings, larger than Braviary's


# --- the body: Braviary's, dark grey, a white streak down each flank under the wing --------------------------------
def body(k):
    k.add("body", ell((0, 0.01, 0.318), (0.105, 0.165, 0.112), rot=(-18, 0, 0)), DARK, "body")
    k.add("chest", ell((0, -0.1, 0.35), (0.09, 0.085, 0.095)), DARK, "body")
    k.add("neck", ell((0, -0.135, 0.41), (0.065, 0.07, 0.07), rot=(-25, 0, 0)), DARK, "head")
    k.add("head", ell(H, HR * 1.08), WHITE, "head")
    for s, b in ((1, "wingR"), (-1, "wingL")):
        k.add("shoulder", ell(SH * np.array([s, 1, 1]) + (-0.012 * s, 0.045, -0.015), (0.048, 0.065, 0.045)), DARK, b)
    # the white streak: along each flank from under the wing's root back toward the tail
    def streak(P):
        y = P[:, 1]
        zc = 0.33 - 0.12 * (y - 0.0)
        return (inside(np.abs(P[:, 2] - zc) - 0.016, 0.004) * smoothstep(0.075, 0.09, np.abs(P[:, 0]))
                * inside(-y, 0.01) * inside(y - 0.14, 0.01))
    k.paint(streak, WHITE, on=["body"])
    # the throat dark under the beak's line
    k.paint(lambda P: inside(P[:, 2] - (H[2] - 0.045 + 0.25 * (P[:, 1] - H[1])), 0.003) * inside(P[:, 1] - (H[1] + 0.02), 0.01),
            DARK, on=["head"])


def thighs(k):
    for s, b in ((1, "legR"), (-1, "legL")):
        k.add("thigh", ell((0.058 * s, -0.075, 0.25), (0.05, 0.06, 0.06), rot=(-30, 0, 0)), DARK, b)


# --- the hood: shaggy white feathers all round the head, flaring up and back ------------------------------------------
def fib(n):
    i = np.arange(n) + 0.5
    phi = np.arccos(1 - 2 * i / n); th = math.pi * (1 + 5 ** 0.5) * i
    return np.stack([np.cos(th) * np.sin(phi), np.sin(th) * np.sin(phi), np.cos(phi)], axis=1)


def hood(k):
    C = H + np.array([0, 0.02, 0.012])
    R = HR * 1.08
    rs = np.random.RandomState(7)
    for d in fib(70):
        # leave the face clear: nothing on the front below the brow
        if d[1] < -0.45 and d[2] < 0.55:
            continue
        if d[2] < -0.55:
            continue
        base = C + d * R * 0.92
        flow = unit(d[0] * 1.1, d[1] * 0.6 + 0.75, d[2] * 0.9 + 0.35)       # each swept up and back
        ln = 0.06 + 0.035 * max(0.0, d[2]) + 0.02 * rs.rand()
        if d[1] < -0.3:
            ln *= 0.7
        tip = base + flow * ln
        col = WHITE if rs.rand() < 0.65 else PALE
        k.add("hood", p.spike(base, tip, 0.05, 0.022, side=np.cross(flow, d), at=0.3), col, "head", fuse=False)
    # a few long flaring plumes over the crown, lilac at their roots in front
    for x, rise, ln in ((0.0, 0.9, 0.17), (0.04, 0.7, 0.15), (-0.04, 0.7, 0.15), (0.075, 0.4, 0.13), (-0.075, 0.4, 0.13)):
        base = C + np.array([x * 0.7, -0.01, R[2] * 0.85])
        tip = base + unit(x * 2.5, 0.85, rise) * ln
        k.add("plume", p.spike(base, tip, 0.06, 0.024, side=(1, 0, -x * 4), at=0.35), WHITE, "head", fuse=False)


# --- the mask: violet flames over the brow with a teal almond over each eye -----------------------------------------
FLAME = [(0.0, 1.0), (0.18, 0.55), (0.35, 0.95), (0.5, 0.45), (0.72, 0.75), (0.8, 0.25), (1.0, 0.1), (0.95, -0.25),
         (0.6, -0.45), (0.25, -0.2), (0.0, -0.45), (-0.25, -0.2), (-0.6, -0.45), (-0.95, -0.25), (-1.0, 0.1),
         (-0.8, 0.25), (-0.72, 0.75), (-0.5, 0.45), (-0.35, 0.95), (-0.18, 0.55)]
ALMOND = [(-1.0, 0.0), (-0.5, 0.55), (0.3, 0.8), (1.0, 0.35), (0.6, -0.3), (-0.2, -0.55)]


def mask(k):
    MC = H + np.array([0, -HR[1] * 1.02, HR[2] * 0.42])
    n = unit(0, -0.8, 0.55)
    k.decal(MC, n, (0.06, 0.05), VIOLET, outline=polygon(FLAME), up=unit(0, 0.55, 0.8), eps=0.0035, rings=22, segs=120)
    k.decal(MC + (0, 0, 0.004), n, (0.048, 0.037), LILAC, outline=polygon(FLAME), up=unit(0, 0.55, 0.8), eps=0.0048,
            rings=18, segs=120)
    for s in (1, -1):
        d = unit(0.5 * s, -0.8, 0.4)
        c = H + d / np.linalg.norm(d / (HR * 1.08))
        out = ALMOND if s > 0 else [(-a, b) for a, b in ALMOND]
        k.decal(c, d, (0.026, 0.014), TEAL, outline=polygon(out), up=unit(-0.25 * s, 0.3, 1), eps=0.0062, rings=10, segs=56)


# --- the wings: Braviary's, larger; white, the inner half dark with a toothed edge, on both faces --------------------
def coverts_outline():
    """The dark inner half by the root, under a white leading edge, its outer edge toothed over the white feathers."""
    pts = [(0.0, -0.008), (0.1, -0.018), (0.2, -0.012)]
    # the toothed edge from the leading edge's middle down and in to the trailing edge near the root
    for i, t in enumerate(np.linspace(0, 1, 9)[1:]):
        a, b = 0.2 - 0.1 * t, -0.012 + 0.16 * t
        pts.append((a + (0.022 if i % 2 == 0 else 0.0), b))
    pts.append((0.0, 0.125))
    return pts


def wings(k):
    d, c = b_.wing_axes()
    n = np.cross(d, c)
    root = SH - d * 0.03
    big = [(a * GROW, b * GROW) for a, b in b_.wing_outline()]
    cov = [(a * GROW, b * GROW) for a, b in coverts_outline()]
    k.add("wing", slab(big, root, d, c, 0.013), WHITE, "wing{s}", fuse=False, mirror=True)
    k.add("covert", slab(cov, root, d, c, 0.022), DARK, "wing{s}", fuse=False, mirror=True)


# --- the tail: Braviary's fan, white ------------------------------------------------------------------------------
def tail(k):
    base0 = np.array([0, 0.125, 0.27])
    pitch = math.radians(-22)
    for i, yaw in enumerate((-36, -18, 0, 18, 36)):
        y = math.radians(yaw)
        dd = np.array([math.sin(y) * math.cos(pitch), math.cos(y) * math.cos(pitch), math.sin(pitch)])
        side = np.array([math.cos(y), -math.sin(y), 0])
        r = math.radians(1.4 * yaw)
        side = side * math.cos(r) - np.array([0, 0, 1.0]) * math.sin(r)
        b = base0 + np.array([math.sin(y) * 0.02, 0, -0.008 * abs(yaw) / 18])
        ln, w = 0.26 - 0.02 * abs(yaw) / 18, 0.085
        k.add("tail", slab(b_.feather_part(ln, w, 0.0, 1.0), b, dd, side, 0.016), WHITE if i % 2 else PALE, "tail",
              fuse=False)


# --- the eyes: small, teal-grey, under the mask ----------------------------------------------------------------------
EYEDIR = unit(0.62, -0.74, 0.12)
EYE = (0.018, 0.015)
WHITEC, INKC = np.array([0.93, 0.94, 0.97]), srgb("#16161C")


def eye_open(u, v):
    c = np.tile(WHITEC, (len(u), 1))
    c[(u + 0.12) ** 2 + (v + 0.05) ** 2 < 0.62 ** 2] = srgb(EYEC)
    c[(u + 0.15) ** 2 + (v + 0.05) ** 2 < 0.3 ** 2] = INKC
    c[(u + 0.0) ** 2 + (v - 0.15) ** 2 < 0.12 ** 2] = (1, 1, 1)
    c[np.hypot(u, v) > 0.9] = INKC
    return c


def eyes(k):
    E = H + EYEDIR / np.linalg.norm(EYEDIR / (HR * 1.08))
    m = np.array([-1, 1, 1.0])
    LID = srgb(WHITE)
    def lay(state):
        look = eye_look(state, eye_open, LID, INKC, line=0.18)
        k.decal(E, EYEDIR, EYE, look, eps=0.004, outline=polygon(b_.EYEOUT), rings=20, segs=56)
        k.decal(E * m, EYEDIR * m, EYE, lambda u, v: look(-u, v), eps=0.004,
                outline=polygon([(-a, b) for a, b in b_.EYEOUT]), rings=20, segs=56)
    return lay


def braviary_hisui(src, out):
    k = Kit(src, coat="fur")
    body(k)
    thighs(k)
    mask(k)
    hood(k)
    b_.beak(k)
    wings(k)
    tail(k)
    b_.NAVY = DARK                     # the shaggy feathers over the shins
    b_.feet(k)
    k.build(voxel=b_.VOXEL, band=0.007)
    k.eye_states(eyes(k))
    k.export(out)


if __name__ == "__main__":
    braviary_hisui(*args())
