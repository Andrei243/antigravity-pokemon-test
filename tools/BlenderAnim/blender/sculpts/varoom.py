# Varoom sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/varoom.py -- <the export's Varoom.glb> <out.glb>
#
# From the references: a little single-cylinder engine sitting on a heap of dark purple faceted rocks (the rocks the
# largest mass). Over the rocks a pale grey steel cowl, its lower edge drooping over them; at its front a short pipe
# ending in an oval cup with a dark intake (the face's mouth), turned up and forward; a yellow eye with a black
# upright pupil on each side of the cowl, set in a grey socket; out of the cowl's top the cylinder leaning back, a
# square block with rounded corners, white banded black with a black cap; behind, the exhaust pipe for a tail, a grey
# tube running back and down, its end a purple bulb with purple spatter on the grey before it.
# The pictures overrule the task's text: the purple-ended tube is the exhaust at the back (the tail bone), not a
# tongue, and the face is the intake cup at the front (as the 3D picture and the game's sculptor have it).
#
# Written to be grown from (Revavroom keeps the rocks, the steel, the yellow eye and the spattered exhaust pipes):
# its makes are the functions below and its colours the constants.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, tube, hull, lathe, srgb, inside, unit, boxy, eye_look, args

STEEL, SOCKET, DARK = "#D3D5DA", "#9A9CA4", "#26262B"
ROCK, ROCKLIGHT = "#4F4459", "#5E526A"
STRIPE_W, STRIPE_K = "#ECEDEF", "#1E1E22"
PURPLE = "#8E4AA6"
YELLOW, PUPIL, INK, GLINT = "#F3C21C", "#151515", "#2A2A2E", "#FFFFFF"
VOXEL = 0.0034


# --- the line's makes ---------------------------------------------------------------------------------------------
def rock(k, name, bone, c, r, seed, n=26):
    """A faceted rock: the hull of points scattered over an ellipsoid, a loose piece with flat faces."""
    rs = np.random.RandomState(seed)
    d = rs.normal(size=(n, 3)); d /= np.linalg.norm(d, axis=1, keepdims=True)
    d *= (0.86 + 0.2 * rs.rand(n))[:, None]
    k.add(name, hull(np.array(c, float) + d * np.array(r, float), merge=6.0), ROCK, bone, fuse=False, surface="shell")


def exhaust(k, name, bone, path, radii, tip_r, seed, spots=7):
    """An exhaust pipe: a grey tube along path ending in a purple bulb, a ragged purple edge and spatter before it."""
    path = np.array(path, float)
    k.add(name, tube(path, radii), STEEL, bone)
    d = unit(*(path[-1] - path[-2]))
    tip = path[-1] + d * tip_r * 0.25
    k.add(name + "tip", ell(tip, (tip_r,) * 3), PURPLE, bone)
    # the purple running back from the bulb with a wavy edge, kept to this pipe's own neighbourhood
    near = path[-1] - d * tip_r * 1.7
    a, b = sculptkit.axes(d)
    def edge(P):
        q = P - near
        along = q @ d
        th = np.arctan2(q @ b, q @ a)
        wave = 0.22 * tip_r * np.sin(3 * th + 1.0)
        return inside(-(along - wave), 0.0012) * inside(np.linalg.norm(P - tip, axis=1) - tip_r * 3.0, 0.002)
    k.paint(edge, PURPLE)
    # spatter: round drops on the grey before the edge
    rs = np.random.RandomState(seed)
    for i in range(spots):
        t = 1.2 + 1.8 * rs.rand()
        th = 2 * math.pi * (i + 0.3 * rs.rand()) / spots
        c = near - d * tip_r * t * 0.85 + (math.cos(th) * a + math.sin(th) * b) * radii[-1]
        rr = tip_r * (0.3 + 0.16 * rs.rand())
        k.paint(lambda P, c=c, rr=rr: inside(np.linalg.norm(P - c, axis=1) - rr, 0.0012), PURPLE)


def eye_open(u, v):
    """A round yellow eye: a dark rim, an upright black pupil, a white glint."""
    c = np.tile(srgb(YELLOW), (len(u), 1))
    rr = u * u + v * v
    c[rr > 0.84 ** 2] = srgb(INK)
    c[(u / 0.34) ** 2 + (v / 0.6) ** 2 < 1] = srgb(PUPIL)
    c[((u + 0.12) / 0.13) ** 2 + ((v - 0.3) / 0.16) ** 2 < 1] = srgb(GLINT)
    return c


def lay_eye(k, state, at, n, half, mirror=True, up=(0, 0, 1)):
    k.decal(at, n, (half, half), eye_look(state, eye_open, srgb(SOCKET), srgb(INK), line=0.18), up=up,
            eps=0.0016, rings=20, segs=56, mirror=mirror)


def banded_block(k, name, bone, base, d, half, bands, cap):
    """A square block with rounded corners from base along d, banded: bands is (length, colour) from the base up,
    each a hull of its own, so every stripe keeps its edges; cap is the top's colour and length."""
    d = unit(*d)
    a = np.cross(d, (1.0, 0, 0)); a /= np.linalg.norm(a)
    b = np.cross(d, a)
    h, c = half, half * 0.72
    ring = [a * x + b * y for x, y in ((h, c), (c, h), (-c, h), (-h, c), (-h, -c), (-c, -h), (c, -h), (h, -c))]
    s = 0.0
    for i, (ln, col) in enumerate(bands + [cap]):
        p0 = np.array(base, float) + d * s
        p1 = p0 + d * (ln + 0.0006)
        pts = [p0 + q for q in ring] + [p1 + q * (0.9 if i == len(bands) else 1.0) for q in ring]
        k.add(f"{name}{i}", hull(pts), col, bone, fuse=False, surface="metal")
        s += ln
    return np.array(base, float) + d * s


# --- Varoom ---------------------------------------------------------------------------------------------------------
BC = np.array([0.0, 0.0, 0.185])           # the cowl's middle
BR = np.array([0.084, 0.104, 0.062])
EYE_AT = np.array([0.071, -0.052, 0.19])
EYE_N = unit(0.78, -0.55, 0.12)


def varoom(src, out):
    k = Kit(src, coat="metal")

    # The rocks: a heap under the cowl, one on each leg and two between
    for s, side in ((1, "R"), (-1, "L")):
        rock(k, f"rockfront{side}", f"leg{side}", (0.06 * s, -0.065, 0.075), (0.072, 0.07, 0.074), 11 if s > 0 else 12)
        rock(k, f"rockback{side}", f"hind{side}", (0.062 * s, 0.07, 0.07), (0.07, 0.072, 0.07), 13 if s > 0 else 14)
    rock(k, "rockmid", "body", (0, 0.0, 0.09), (0.07, 0.08, 0.075), 15)
    rock(k, "rockfore", "body", (0, -0.1, 0.07), (0.055, 0.05, 0.06), 16)

    # The steel cowl: a rounded block over the rocks, its lower edge drooping at the sides
    def cowl(V):
        V = boxy(2.6)(V)
        low = np.clip(-V[:, 2], 0, 1)
        V[:, 0] *= 1 + 0.12 * low
        return V
    k.add("cowl", ell(BC, BR, deform=cowl), STEEL, "body")
    k.add("brow", ell(BC + (0, -0.06, 0.012), (0.07, 0.06, 0.055)), STEEL, "head")
    # the eyes' grey sockets
    k.add("socket", ell(EYE_AT - EYE_N * 0.006, (0.024, 0.026, 0.028)), SOCKET, "head", mirror=True)
    # the face: a short pipe forward and up to the intake cup
    k.add("neck", tube([(0, -0.07, 0.2), (0, -0.115, 0.225), (0, -0.138, 0.247)], [0.026, 0.022, 0.02]), STEEL, "head")
    cd = unit(0, -0.55, 0.84)
    cb = np.array([0.0, -0.134, 0.24])
    cup = lathe(cb, cd, [(0, 0), (0.019, 0), (0.033, 0.016), (0.041, 0.032), (0.04, 0.038), (0.033, 0.039),
                         (0.029, 0.022), (0.029, 0.017), (0, 0.017)], seg=44)
    V = cup[0]; V = cb + (V - cb) * np.array([1.3, 1.0, 1.0])
    k.add("cup", (V, cup[1]), STEEL, "head", fuse=False, surface="metal")
    V, F = lathe(cb, cd, [(0, 0.0175), (0.0285, 0.0175), (0.0285, 0.0195), (0, 0.0195)], seg=44)
    k.add("intake", (cb + (V - cb) * np.array([1.3, 1.0, 1.0]), F), DARK, "head", fuse=False, surface="metal")

    # The cylinder: out of the cowl's top, leaning back, white banded black, a black cap
    W, K = STRIPE_W, STRIPE_K
    banded_block(k, "cylinder", "body", (0, 0.012, 0.205), (0, 0.6, 0.8), 0.047,
                 [(0.058, W), (0.016, K), (0.014, W), (0.016, K), (0.014, W), (0.016, K), (0.014, W)], (0.02, K))

    # The exhaust: back and down from the tail, out to the +X side, rising a little at its end
    exhaust(k, "exhaust", "tail", [(0, 0.075, 0.165), (0.018, 0.14, 0.128), (0.055, 0.205, 0.092), (0.095, 0.25, 0.085),
                                   (0.125, 0.275, 0.1)], [0.022, 0.02, 0.018, 0.018, 0.019], 0.03, seed=3)

    k.build(voxel=VOXEL, smooth=4, band=0.005, wband=0.03)
    k.eye_states(lambda state: lay_eye(k, state, EYE_AT, EYE_N, 0.021))
    k.export(out)


if __name__ == "__main__":
    varoom(*args())
