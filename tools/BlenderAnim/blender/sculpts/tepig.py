# Tepig sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tepig.py -- <the export's Tepig.glb> <out.glb>
# Written to be grown from: pignite.py and emboar.py import its frame (sibling("tepig")): the colours, the snout, the
# eye, the hooves, the curly tail and the way the head's colours are painted.
#
# From the references: an orange piglet on four short legs, its head as big as its body and carried forward; a black
# cap over the crown and the back of the head; a yellow band across the forehead in front of the cap, down to the
# snout's bridge; a big flat oval pig's snout, red-pink, with two dark nostrils; big round eyes, white with a dark rim
# and a big black pupil looking forward, a white glint; a wide open mouth under the snout, dark inside with a pink
# tongue; two long dark ears standing up and back from the head's top, a V from the front; a black band round the back
# half of the body (the rump black above orange haunches); short orange legs with small dark hooves; a thin black tail
# curled into a coil and a half, a red ball at its end.
# Reference ratios (3D picture, feet to ear tips): ears above the cap / height 0.41; head (cap to chin) / height 0.32;
# head height / body depth 0.9; snout across / head across 0.45; belly over the ground / height 0.15 (the art runs).
# The export's joints: body (0, 0, 0.14), head (0, -0.08, 0.17), ears (+-0.04, -0.08, 0.24), legs (+-0.055, -0.06,
# 0.08), hind legs (+-0.055, 0.06, 0.08), tail (0, 0.11, 0.16).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, boxy, eye_look, args, outward

# --- colours (the line's) -----------------------------------------------------------------------------------------------
ORANGE = "#EE7A2E"
BLACK = "#3A2E2C"          # the dark of the cap, the ears, the band: a warm black-brown
YELLOW = "#F6D23C"
SNOUT = "#DE6A60"
NOSTRIL = "#5A2228"
HOOF = "#4A3A36"
BALL = "#DC4A3A"
MOUTH = "#6E2E44"
TONGUE = "#EE8C9C"
WHITE = "#FFFFFF"
INK = "#1E1618"

# --- the frame ----------------------------------------------------------------------------------------------------------
H = np.array([0.0, -0.122, 0.21])          # the head's middle
HR = np.array([0.103, 0.098, 0.093])
B = np.array([0.0, 0.035, 0.122])           # the body's middle
BR = np.array([0.09, 0.118, 0.08])
BAND = 0.03                                 # the black band's front edge along y
CAP_LIFT = 0.02                             # the cap's edge: rel z > CAP_LIFT - 0.6 rel y round the head's middle


def on_ell(x, z, c, r, lift=0.0):
    """The front point of an ellipsoid at (x, z) and its outward normal."""
    zn = (z - c[2]) / r[2]
    q = max(0.0, 1 - zn * zn - (x / r[0]) ** 2)
    p = np.array([x, c[1] - r[1] * math.sqrt(q), z])
    n = (p - c) / np.array(r) ** 2; n /= np.linalg.norm(n)
    return p + n * lift, n


def snout(k, c, r, bone, color=SNOUT, nostril=NOSTRIL, tilt=10, name="snout"):
    """A pig's snout: a flat round-ended oval standing out of the face, its front a little up; two dark nostrils."""
    c = np.array(c, float)
    k.add(name, ell(c, r, rot=(tilt, 0, 0), deform=boxy(2.6)), color, bone, surface="skin")
    d = unit(0, -math.cos(math.radians(tilt)), math.sin(math.radians(tilt)))
    front = c + d * r[1] * 0.92
    for s in (1, -1):
        k.decal(front + np.array([0.38 * r[0] * s, 0, 0.02 * r[2]]), d, (r[0] * 0.17, r[2] * 0.4), nostril, eps=0.004,
                rings=8, segs=28)
    return front, d


def round_eye(white=WHITE, ink=INK, rim=0.84, pupil=(-0.14, -0.02, 0.5, 0.6), glint=(-0.02, 0.22, 0.17)):
    """A big round eye: white with a dark rim, a big black pupil toward the front, a white glint on it."""
    W_, I_ = srgb(white), srgb(ink)
    pu, pv, pa, pb = pupil
    gu, gv, gr = glint
    def look(u, v):
        c = np.tile(W_, (len(u), 1))
        c[u * u + v * v > rim ** 2] = I_
        c[((u - pu) / pa) ** 2 + ((v - pv) / pb) ** 2 < 1] = I_
        c[((u - gu) / gr) ** 2 + ((v - gv) / gr) ** 2 < 1] = W_
        return c
    return look


def tail(k, root, size, ex, color=BLACK, r=0.0095, bone="tail", name="tail", end=None):
    """
    A thin curly tail: out of the rump along ex, a coil of a turn and a half in the plane of ex and up, then a stem up
    to its end, which is returned (where Tepig's ball or Pignite's puff goes).
    """
    root = np.array(root, float); ex = np.array(ex, float); ez = np.array([0, 0, 1.0])
    c = root + ex * size * 1.7 + ez * size * 1.25
    th = np.linspace(0, 2 * math.pi * 1.4, 64)
    rr = size * (1.0 - 0.15 * th / th[-1])
    ang = math.pi + th * -1.0                      # from the root's side round over the top and down again
    loop = c[None] + (np.cos(ang) * rr)[:, None] * ex[None] + (np.sin(ang) * rr)[:, None] * ez[None] \
        + (th / th[-1] * size * 0.5)[:, None] * ex[None]
    stem_top = loop[-1] + ez * size * 1.3 + ex * size * 0.15 if end is None else np.array(end, float)
    pts = np.vstack([root, root + ex * size * 0.5 + ez * size * 0.15, loop[2:], loop[-1] + (stem_top - loop[-1]) * 0.5,
                     stem_top])
    k.add(name, outward(tube(pts, [r] * len(pts), seg=14, per=3)), color, bone, fuse=False, surface="fur")
    return stem_top


def ball(k, c, r, color=BALL, bone="tail", name="ball"):
    """A round ball with a small pale glint on its upper front."""
    c = np.array(c, float)
    k.add(name, ell(c, (r, r, r), sub=4), color, bone, fuse=False, surface="shell")
    g = c + unit(-0.35, -0.65, 0.65) * r * 0.86
    k.add(name + "glint", ell(g, (r * 0.24, r * 0.16, r * 0.18), rot=(40, 0, -30), sub=2), "#F6A090", bone, fuse=False,
          surface="shell")


def hoof(k, c, r, bone, color=HOOF, mirror=True, name="hoof"):
    """A small dark hoof under a leg: a flat round cap, cleft by a notch in front."""
    k.add(name, ell(c, r, deform=boxy(2.4)), color, bone, mirror=mirror, surface="shell")


# --- Tepig ---------------------------------------------------------------------------------------------------------------
def add_body(k):
    def barrel(V):
        V = V.copy()
        V[:, 2] *= 1 + 0.08 * np.clip(V[:, 1], 0, 1)          # a little deeper toward the rump
        return V
    k.add("body", ell(B, BR, deform=barrel), ORANGE, "body")
    k.add("rump", ell(B + (0, 0.07, 0.008), (0.084, 0.07, 0.075)), ORANGE, "body")
    k.add("chest", ell((0, -0.06, 0.14), (0.08, 0.075, 0.075)), ORANGE, "body")


def add_legs(k):
    """Short thick legs: haunches fused into the flanks, a stub of shin, a dark hoof."""
    # front legs
    k.add("shoulder", ell((0.055, -0.06, 0.085), (0.048, 0.052, 0.055)), ORANGE, "leg{s}", mirror=True)
    k.add("leg", tube([(0.058, -0.065, 0.08), (0.06, -0.072, 0.034)], [0.039, 0.034]), ORANGE, "leg{s}", mirror=True)
    hoof(k, (0.06, -0.076, 0.016), (0.034, 0.037, 0.018), "leg{s}")
    # hind legs: round haunches out of the rump
    k.add("haunch", ell((0.058, 0.085, 0.09), (0.052, 0.064, 0.065)), ORANGE, "hind{s}", mirror=True)
    k.add("hindleg", tube([(0.06, 0.08, 0.075), (0.062, 0.07, 0.034)], [0.039, 0.034]), ORANGE, "hind{s}", mirror=True)
    hoof(k, (0.062, 0.066, 0.016), (0.034, 0.037, 0.018), "hind{s}")
    # a crisp line over each hoof
    k.paint(lambda P: inside(P[:, 2] - 0.03, 0.0025) * (np.abs(np.abs(P[:, 0]) - 0.06) < 0.045), HOOF,
            on=["hoof", "leg", "hindleg"])


def add_band(k):
    """The black band round the back half; the haunches stay orange."""
    def band(P):
        black = inside(BAND - P[:, 1], 0.003) * (P[:, 2] > 0.04)
        q = (P - np.array([0, 0.085, 0.085])) * np.array([1, 1, 1.0])
        thigh = np.hypot((np.abs(q[:, 0]) - 0.058) / 0.056, np.hypot(q[:, 1] / 0.07, q[:, 2] / 0.062))
        return black * (1 - inside(thigh - 1, 0.004))
    k.paint(band, BLACK, on=["body", "rump", "chest", "haunch"])


def add_head(k):
    k.add("head", ell(H, HR, deform=boxy(2.2)), ORANGE, "head")
    k.add("jowl", ell(H + (0, -0.03, -0.045), (0.08, 0.07, 0.05)), ORANGE, "head")
    k.add("neck", ell((0, -0.06, 0.17), (0.07, 0.065, 0.065)), ORANGE, "head")
    snout(k, H + (0, -0.104, -0.03), (0.047, 0.027, 0.033), "head")
    # the ears: long, dark, up and back from the head's top, a little out
    for side, s in (("R", 1), ("L", -1)):
        d = unit(0.32 * s, 0.55, 0.77)
        base = H + np.array((0.045 * s, 0.006, 0.078))
        side_dir = unit(1.0 * s, 0.25, 0.0)
        flat = np.cross(d, side_dir); flat /= np.linalg.norm(flat)
        pts = [base, base + d * 0.05, base + d * 0.1 + unit(0, 0.3, -0.1) * 0.006, base + d * 0.15, base + d * 0.19]
        k.add("ear" + side, blade(pts, [0.03, 0.04, 0.036, 0.022, 0.006], flat, 0.42), BLACK, "ear" + side)


def paint_head(k):
    """The yellow band over the brow and the black cap behind it, both crisp."""
    def yellow(P):
        rel = P - H
        a = np.abs(np.arctan2(rel[:, 0], -rel[:, 1]))
        return (inside(a - 0.48, 0.004) * inside(0.0 - rel[:, 2] + 0.6 * (rel[:, 1] + 0.09), 0.003)
                * (rel[:, 1] < -0.02) * (rel[:, 2] < 0.1))
    k.paint(yellow, YELLOW, on=["head", "jowl"])
    def cap(P):
        rel = P - H
        return inside(CAP_LIFT - 0.6 * rel[:, 1] - rel[:, 2], 0.003) * (rel[:, 1] < 0.11) * (P[:, 2] > 0.12)
    k.paint(cap, BLACK, on=["head", "jowl", "neck", "ear"])


def add_mouth(k):
    CM, CT = srgb(MOUTH), srgb(TONGUE)
    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[((u + 0.0) / 0.62) ** 2 + ((v + 0.62) / 0.48) ** 2 < 1] = CT
        return c
    k.decal(H + (0, -0.086, -0.07), unit(0, -1, -0.45), (0.056, 0.04), mouth, eps=0.004, rings=12, segs=56,
            outline=lambda th: 1.0 if math.sin(th) < 0 else 0.45)


EYE_X, EYE_Z, EYE_HALF = 0.067, 0.226, (0.032, 0.037)


def lay_eyes(k, state):
    p, n = on_ell(EYE_X, EYE_Z, H, HR)
    n = unit(n[0] * 1.15, n[1], n[2] * 0.6)
    look = eye_look(state, round_eye(), srgb(ORANGE), srgb(INK), line=0.17)
    k.decal(p, n, EYE_HALF, look, eps=0.004, rings=20, segs=56, mirror=True)


def tepig(src, out):
    k = Kit(src, coat="fur")
    add_body(k)
    add_legs(k)
    add_band(k)
    add_head(k)
    paint_head(k)
    add_mouth(k)
    end = tail(k, (0.0, 0.165, 0.155), 0.026, unit(0.5, 0.86, 0))
    ball(k, end + np.array([0, 0, 0.022]), 0.026)
    k.build(voxel=0.0045, band=0.005)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    tepig(*args())
