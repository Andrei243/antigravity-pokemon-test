# Silicobra sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/silicobra.py -- <the export's Silicobra.glb> <out.glb>
# Laid as ekans.py's line (imported: its Line, a tube a segment on its own bone through the chain of joints).
# From the references: a small sand snake whose largest mass by far is the sack of sand round its head, an olive hood
# like a teardrop leaning back, wider than tall, blotched with dark grey; out of its front comes a pale cream face like
# a skull: a short round snout with two big black nostrils on its front and a downturned mouth under them; on each
# side a big lime-green eye with a small black pupil under a heavy olive lid, the olive running down behind it like a
# tear. Below the hood a slim cream body drops to the ground and lies in a loop, the tail ending in a little curl,
# olive on its upper side toward the tip.
# The export's chain: body (0.12, 0.13, 0.03) behind at the right, seg1 (0.074, -0.081, 0.04) round the loop on the
# ground to seg8 (0.007, -0.06, 0.07), head (0, -0.06, 0.22) straight over it.
# Written to be grown from (sandaconda.py): the face, the nostrils, the eye and its lid are functions of a frame.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, polygon, args, sibling

e = sibling("ekans")

OLIVE, OLIVE_DARK, CREAM, FACE = "#A69E66", "#857D4C", "#E4DFC6", "#EFECDD"
SPOT, NOSTRIL, MOUTH = "#4C4A46", "#1E1C1C", "#6C6450"
EYE_GREEN, EYE_DEEP, PUPIL, INK = "#AEE23A", "#6FB01E", "#141414", "#3A3628"

VOXEL = 0.0036

LINE = [(0.142, 0.188, 0.072), (0.165, 0.205, 0.042), (0.152, 0.172, 0.026), (0.12, 0.13, 0.03), (0.105, 0.03, 0.035), (0.074, -0.081, 0.04),
        (-0.025, -0.1, 0.044), (-0.09, -0.032, 0.049), (-0.071, 0.054, 0.053), (0.005, 0.081, 0.057),
        (0.065, 0.037, 0.061), (0.06, -0.03, 0.068), (0.01, -0.055, 0.085), (0.0, -0.035, 0.14),
        (0.0, -0.02, 0.19)]
RADS = [0.009, 0.012, 0.016, 0.021, 0.025, 0.027, 0.028, 0.028, 0.028, 0.028, 0.028, 0.029, 0.031, 0.034, 0.036]
BONES = ["body", "body", "body", "body", "body", "seg1", "seg2", "seg3", "seg4", "seg5", "seg6", "seg7", "seg8", "head"]

HOOD = np.array([0.0, 0.005, 0.255])         # the sack's middle
HOOD_R = np.array([0.118, 0.106, 0.112])
HOOD_TILT = -12                               # its top leaning back


def teardrop(V):
    """The sack narrows below into the neck."""
    V = V.copy()
    t = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.62 * t ** 1.3
    V[:, 1] *= 1 - 0.55 * t ** 1.3
    V[:, 2] *= 1 + 0.35 * t                       # the lower half drawn out into a point at the neck
    V[:, 1] -= 0.25 * t ** 2                      # and the point carried forward over the rising body
    return V


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) along the direction d from its centre."""
    d = np.array(d, float)
    return np.array(c, float) + d / np.linalg.norm(d / np.array(r, float))


# --- the face: a skull-like cream snout out of the sack's front ----------------------------------------------------
class Face:
    """A face's frame: the skull's centre and radii, the snout's, and a size for the marks on it."""
    def __init__(self, skull, skull_r, snout, snout_r, size):
        self.skull, self.skull_r = np.array(skull, float), np.array(skull_r, float)
        self.snout, self.snout_r = np.array(snout, float), np.array(snout_r, float)
        self.size = size


def face(k, f, color, bone="head"):
    k.add("skull", ell(f.skull, f.skull_r, rot=(-4, 0, 0)), color, bone)
    k.add("snout", ell(f.snout, f.snout_r, rot=(-6, 0, 0)), color, bone)


def nostrils(k, f, color, ring=None, x=0.42, z=0.15, half=(0.27, 0.33), eps=0.002):
    """Two big round nostrils on the snout's front, a little apart, turned out a touch."""
    for s in (1, -1):
        d = unit(x * s, -1.0, z)
        c = on_ell(f.snout, f.snout_r, d)
        h = (f.size * half[0], f.size * half[1])
        if ring:
            k.decal(c, d, (h[0] * 1.3, h[1] * 1.25), ring, eps=eps, rings=12, segs=44)
        k.decal(c, d, h, color, eps=eps * 1.6, rings=12, segs=44)


def mouth(k, f, color, z=-0.55, width=0.62, eps=0.002):
    """A downturned mouth under the nostrils: one clean frowning bar."""
    d = unit(0.0, -1.0, z)
    c = on_ell(f.snout, f.snout_r, d)
    arc = [(-1.0, -0.3), (-0.6, 0.12), (0.0, 0.3), (0.6, 0.12), (1.0, -0.3), (0.95, -0.62), (0.55, -0.4),
           (0.0, -0.25), (-0.55, -0.4), (-0.95, -0.62)]
    k.decal(c, d, (f.snout_r[0] * width, f.size * 0.3), color, eps=eps, outline=polygon(arc),
            rings=12, segs=72, up=(0, 0, 1))


def eye_open(lid_edge=0.3, droop=0.12):
    """A big round green eye, a small black pupil toward the front, a dark rim, an olive lid over its top."""
    G, D, P, K, L = srgb(EYE_GREEN), srgb(EYE_DEEP), srgb(PUPIL), srgb(INK), srgb(OLIVE)
    def look(u, v):
        r2 = u * u + v * v
        c = np.tile(G, (len(u), 1))
        c[r2 > 0.62 ** 2] = D
        c[r2 > 0.86 ** 2] = K
        c[(u / 0.2) ** 2 + (v / 0.22) ** 2 < 1] = P
        c[((u + 0.4) / 0.13) ** 2 + ((v - 0.05) / 0.12) ** 2 < 1] = srgb("#FFFFFF")
        edge = lid_edge - droop * u
        c[v > edge] = L
        c[np.abs(v - edge) < 0.12] = K
        return c
    return look


def lay_eyes(k, f, d, half, lid_color, open_look, mirror=True, eps=0.002):
    """The eyes on the skull's sides in the game's five states; the lid shuts them in olive."""
    E = on_ell(f.skull, f.skull_r, d)
    lid, ink = srgb(lid_color), srgb(INK)
    def lay(state):
        look = eye_look(state, open_look, lid, ink, line=0.2)
        k.decal(E, d, half, look, eps=eps, rings=20, segs=56, mirror=mirror)
        if state == "Open":
            # the pupil a patch of its own, so its edge is round at any size
            k.decal(E, d, (half[0] * 0.22, half[1] * 0.24), PUPIL, eps=eps * 1.8, rings=6, segs=40, mirror=mirror)
    return lay


# --- Silicobra -----------------------------------------------------------------------------------------------------
FACE_F = Face(skull=(0.0, -0.088, 0.226), skull_r=(0.074, 0.066, 0.068),
              snout=(0.0, -0.14, 0.214), snout_r=(0.06, 0.054, 0.052), size=0.062)
EYEDIR = unit(0.72, -0.5, 0.28)


def hood(k):
    k.add("hood", ell(HOOD, HOOD_R, rot=(HOOD_TILT, 0, 0), deform=teardrop), OLIVE, "head", surface="fur")


def spots(k):
    """Dark grey blotches on the sack's sides and back, of uneven sizes."""
    for az, el, a, b, rot in ((70, 30, 0.026, 0.018, 20), (105, -8, 0.03, 0.016, -15), (150, 25, 0.022, 0.016, 40),
                              (200, -5, 0.028, 0.017, -25), (250, 28, 0.024, 0.016, 10), (285, -12, 0.03, 0.017, 30),
                              (130, 55, 0.02, 0.015, 0), (230, 55, 0.02, 0.015, 0)):
        t, p = math.radians(az), math.radians(el)
        d = np.array([math.sin(t) * math.cos(p), -math.cos(t) * math.cos(p), math.sin(p)])
        c = HOOD + d * HOOD_R * 0.98
        n = d / HOOD_R; n /= np.linalg.norm(n)
        up = np.array([math.cos(math.radians(rot)), 0, math.sin(math.radians(rot))])
        up = up - n * (up @ n)
        k.decal(c, n, (a * 1.05, b), SPOT, eps=0.0025, rings=10, segs=40, up=up)


def tail_marks(k, line):
    """Olive on the upper side of the tail toward its curled tip, and a blotch on the loop's back."""
    def mask(Q):
        j, s, d = line.place(Q)
        par = line.S[j]
        top = inside(-0.05 - d, 0.05)                      # facing away from the belly
        def span(a, b):
            return inside(a - par, 0.04) * inside(par - b, 0.04)
        return np.clip(top * (span(-1, 1.7) + span(2.5, 3.2) + span(7.8, 8.5)), 0, 1)
    k.paint(mask, OLIVE, on=["seg"])


def tear(k, f):
    """The lid's olive runs down behind the eye like a tear, and over the brow."""
    for s in (1, -1):
        d = unit(0.75 * s, -0.22, -0.05)
        c = on_ell(f.skull, f.skull_r, d)
        k.decal(c, d, (0.013, 0.02), OLIVE, eps=0.0016, rings=10, segs=36)


def silicobra(src, out_path):
    k = Kit(src, coat="scales")
    line = e.Line(LINE, RADS, rise=(0.09, 0.15))
    line.tubes(k, BONES, CREAM)
    hood(k)
    face(k, FACE_F, FACE)
    tail_marks(k, line)
    spots(k)
    tear(k, FACE_F)
    nostrils(k, FACE_F, NOSTRIL)
    mouth(k, FACE_F, MOUTH)
    k.build(voxel=VOXEL, smooth=4, band=0.004, wband=0.03)
    k.eye_states(lay_eyes(k, FACE_F, EYEDIR, (0.03, 0.03), OLIVE, eye_open()))
    k.export(out_path)


if __name__ == "__main__":
    silicobra(*args())
