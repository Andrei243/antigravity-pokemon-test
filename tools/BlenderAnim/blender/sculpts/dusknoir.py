# Dusknoir sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dusknoir.py -- <the export's Dusknoir.glb> <out.glb>
# Grown from Dusclops's script (and Duskull's before it): the eye made the same way, a red light with a pale core and
# a lens over it that glows, now in a dark slit low on the front of the head. A big dark grey ghost with no legs: a
# wide round body over a flat wisp of a tail, a pale grey head like a capped cylinder with a yellow band round it and a
# yellow aerial on top, pale angular plates standing round the head like a high collar, open at the front, arms in dark
# sleeves banded twice with yellow and cuffed in dark brown at the wrist, great pale hands with long fingers, and a
# face on the belly: two yellow pointed ovals for eyes and a yellow zigzag for a mouth.
# The skeleton: body (0, 0, 0.51), tail (0, 0, 0.35), head (0, 0, 0.65), arms (+-0.18, -0.02, 0.58).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, boxy, tri, args

src, out = args()
k = Kit(src, coat="fur")

GREY, PALE, YELLOW, CUFF, SLIT = "#585A55", "#BAC0AA", "#F2C832", "#3A302C", "#141214"
BC = np.array([0.0, 0.0, 0.47])
BR = np.array([0.25, 0.2, 0.22])

# --- the body: one wide round mass, and the wisp of a tail under it -------------------------------------------------
k.add("body", ell(BC, BR), GREY, "body")
k.add("tail", blade([(0.0, 0.01, 0.32), (0.01, 0.03, 0.2), (0.035, 0.07, 0.09), (0.06, 0.1, 0.02)],
                    [0.16, 0.1, 0.045, 0.004], (0, 1, 0), 0.45), GREY, "tail")

# --- the head: a pale capped cylinder on the shoulders ---------------------------------------------------------------
HC = np.array([0.0, 0.01, 0.765])
k.add("head", ell(HC, (0.105, 0.1, 0.15), deform=boxy(3.5)), PALE, "head")
# the yellow aerial: a short stalk and a flat disc, crisp, so loose
k.add("stalk", tube([(0, 0.01, 0.885), (0, 0.01, 0.94)], [0.02, 0.018]), YELLOW, "head", fuse=False, surface="shell")
k.add("disc", ell((0, 0.01, 0.948), (0.058, 0.058, 0.014)), YELLOW, "head", fuse=False, surface="shell")

# --- the collar: pale angular plates standing round the head, out and up, open at the front --------------------------
for i, (deg, rise, reach) in enumerate([(58, 0.2, 0.25), (98, 0.22, 0.27), (138, 0.19, 0.24), (180, 0.17, 0.22)]):
    for side in ((1, -1) if deg < 180 else (1,)):
        a = math.radians(deg) * side
        rad = np.array([math.sin(a), -math.cos(a), 0.0])
        base = np.array([0, 0.01, 0.64]) + rad * 0.12
        tip = np.array([0, 0.01, 0.64 + rise]) + rad * reach
        mid = base * 0.45 + tip * 0.55 + (0, 0, 0.02)
        k.add(f"plate{i}{'R' if side > 0 else 'L'}", blade([base, mid, tip], [0.075, 0.055, 0.008], rad, 0.2),
              PALE, "body", fuse=False)

# --- the arms: dark sleeves down and forward from the shoulders, a dark cuff, great pale hands ------------------------
SH, WR = np.array([0.18, -0.02, 0.6]), np.array([0.37, -0.1, 0.47])
k.add("arm", tube([(0.14, -0.01, 0.61), SH, (0.29, -0.065, 0.53), WR], [0.075, 0.075, 0.068, 0.062]), GREY,
      "arm{s}", mirror=True)
k.add("cuff", tube([(0.355, -0.093, 0.48), (0.41, -0.12, 0.44)], [0.074, 0.07]), CUFF, "arm{s}", mirror=True)
P0 = np.array([0.455, -0.145, 0.405])
k.add("hand", ell(P0, (0.06, 0.072, 0.042), rot=(0, -30, 0)), PALE, "arm{s}", mirror=True)
for j, dy in enumerate((-0.05, -0.017, 0.017, 0.05)):
    b0 = P0 + (0.035, dy, -0.01)
    k.add(f"finger{j}", tube([b0, b0 + (0.05, dy * 0.3, -0.035), b0 + (0.075, dy * 0.5, -0.085)],
                            [0.024, 0.02, 0.015]), PALE, "arm{s}", mirror=True)
k.add("thumb", tube([P0 + (-0.015, -0.05, 0.0), P0 + (-0.005, -0.1, -0.025), P0 + (0.005, -0.13, -0.06)],
                    [0.022, 0.019, 0.015]), PALE, "arm{s}", mirror=True)
# two yellow rings round each sleeve, crisp: by the place along the arm's line
def rings(P):
    m = np.zeros(len(P))
    for s in (1, -1):
        a, w = SH * (s, 1, 1), WR * (s, 1, 1)
        d = w - a; L = np.linalg.norm(d); d /= L
        t = (P - a) @ d
        off = np.linalg.norm((P - a) - np.outer(t, d), axis=1)
        near = (off < 0.11) & (P[:, 0] * s > 0.2)
        for f in (0.3, 0.58):
            m = np.maximum(m, near * inside(np.abs(t - f * L) - 0.016, 0.0045))
    return m
k.paint(rings, YELLOW)

# --- the yellow band round the head ---------------------------------------------------------------------------------
def headband(P):
    r = np.hypot(P[:, 0], P[:, 1] - 0.01)
    return (r < 0.14) * inside(np.abs(P[:, 2] - 0.82) - 0.016, 0.0045)
k.paint(headband, YELLOW)

# --- the face on the belly: a yellow zigzag for a mouth ---------------------------------------------------------------
def zigzag(P):
    phi = np.arctan2(P[:, 0], -P[:, 1])
    s = phi * 0.23
    zc = 0.405 + 0.028 * tri(s / 0.15 + 0.5) - 0.02 * (phi / 1.3) ** 2
    trunk = ((P[:, 0] / 0.27) ** 2 + (P[:, 1] / 0.22) ** 2 < 1.0) & (P[:, 1] < 0.0) & (np.abs(phi) < 1.35)
    return trunk * inside(np.abs(P[:, 2] - zc) - 0.016, 0.0045)
k.paint(zigzag, YELLOW)

# ... and two yellow pointed ovals for eyes
LENS = [(0.7 * math.sin(t) * abs(math.sin(t)) ** 0.8, math.cos(t)) for t in np.linspace(0, 2 * math.pi, 28, endpoint=False)]
for side in (1, -1):
    x, z = 0.1 * side, 0.52
    y = -BR[1] * math.sqrt(max(1 - (x / BR[0]) ** 2 - ((z - BC[2]) / BR[2]) ** 2, 0.0))
    n = unit(x / BR[0] ** 2, y / BR[1] ** 2, (z - BC[2]) / BR[2] ** 2)
    k.decal((x, y, z), n, (0.031, 0.042), YELLOW, eps=0.004, outline=polygon(LENS), rings=8, segs=40)

# --- the slit low on the head's front, dark, for the eye to sit in ----------------------------------------------------
SC = np.array([0.0, -0.1, 0.70])
SN = unit(0, -1, -0.05)
SLITO = [(-1, 0.25), (-0.8, 0.75), (-0.3, 0.95), (0.3, 0.95), (0.8, 0.75), (1, 0.25), (0.85, -0.45), (0.45, -0.9),
         (0, -1), (-0.45, -0.9), (-0.85, -0.45)]
k.decal(SC, SN, (0.07, 0.05), SLIT, eps=0.004, outline=polygon(SLITO), rings=12, segs=48)

k.build(voxel=0.007, smooth=3)

# --- the eye: a red light with a pale core in the slit, its lens glowing ---------------------------------------------
INK, LID = srgb("#8C8C84"), srgb(SLIT)
CORE, RED = srgb("#FFD2C8"), srgb("#E83A3A")
def light(u, v):
    r = np.sqrt(u * u + v * v)
    c = np.tile(RED, (len(u), 1))
    c[r < 0.42] = CORE
    return c
EC = np.array([0.0, -0.1, 0.70])
EH = 0.03
def eyes(state):
    k.decal(EC, SN, (EH, EH * 1.05), eye_look(state, light, LID, INK, line=0.16), eps=0.007, rings=20, segs=56)
    if state in ("Open", "Fierce"):
        hit = TREE.ray_cast(Vector(EC + SN * 0.35), Vector(-SN))[0]
        right = np.cross((0, 0, 1.0), SN); right /= np.linalg.norm(right)
        up = np.cross(SN, right)
        V, F = ell((0, 0, 0), (0.016, 0.016, 0.005))
        V = V @ np.stack([right, up, SN], axis=1).T + np.array(hit) + SN * 0.008
        k.add("light", (V, F), "#FF7A6E", "head", fuse=False, glow="#FF3A35", surface="skin")

from mathutils import Vector
from mathutils.bvhtree import BVHTree
_P, _F, _W = k.surfaces[k.name]
TREE = BVHTree.FromPolygons(_P.tolist(), _F)

k.eye_states(eyes)
k.export(out)
