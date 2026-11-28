# Urslet (Carpathia No. 10053) sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py):
#   blender --background --factory-startup --python sculpts/urslet.py -- <the export's Urslet.glb> <out.glb>
# Written to be grown from: ursabrun.py imports its helpers and palette (sibling("urslet")): the round ear with its
# dark lining, the bear's eye, the claw, the honey crescent painted round the body's axis, the board check.
#
# The design, from the project's picture (docs/art/reference/gemini/carpathia/10053-Urslet.jpg): a round brown bear cub
# standing; a big round head some two fifths of its height, round ears on its corners with a darker lining, a small
# tuft on the crown; a pale honey-cream muzzle low on the face with a nose glazed in honey that drips; dark round eyes
# with a glint under small worried brows; a small mouth; a pale cream crescent on the chest under the chin, its horns
# up; a round pear of a body on short thick legs and broad feet; the short arms hugging a broken honeycomb to its belly,
# honey dripping off it; a little round tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, boxy, eye_look, args

BROWN, DARK, LINING, CREAM, MOON = "#8B5A3A", "#5E3B27", "#5A3622", "#F1DCAE", "#F0D9A6"
HONEY, COMB, CELL, INK, GLINT, MOUTH = "#E2952A", "#E3A640", "#B06A1C", "#1B120E", "#FFFFFF", "#3E2418"


# --- the line's helpers ----------------------------------------------------------------------------------------------
def hit(shape, o, d):
    """Where a ray from o along d first meets a part's own mesh: a place on a part before the body is fused."""
    V, F = shape
    h = BVHTree.FromPolygons(V.tolist(), F).ray_cast(Vector(o), Vector(unit(*d)))
    return np.array(h[0])


def round_about(P, c, r):
    """(the arc round an upright axis through c, at radius r; z): coordinates for paint round a head or a body."""
    return np.arctan2(P[:, 0] - c[0], -(P[:, 1] - c[1])) * r, P[:, 2]


def crescent(c, r, zc, R, bite, lift, front_y, edge=0.0025):
    """A pale crescent with its horns up, painted round the axis through c (radius r): a disc of radius R at height zc
    less a disc of radius R * bite raised by lift."""
    def mask(P):
        s, z = round_about(P, c, r)
        whole = inside(np.hypot(s, z - zc) - R, edge)
        cut = inside(np.hypot(s, z - zc - lift) - R * bite, edge)
        return whole * (1 - cut) * (P[:, 1] < front_y)
    return mask


def claw(k, name, base, d, length, width, bone, color, curl=0.0):
    """A claw: a flat-faced wedge from base along d, its tip bent down by curl."""
    base, d = np.array(base, float), unit(*d)
    a = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a)
    b = np.cross(a, d)
    tip = base + d * length - b * (width * 0.25 + curl * length)
    mid = base + d * length * 0.55 - b * curl * length * 0.3
    k.add(name, hull([base + a * width + b * width * 0.8, base - a * width + b * width * 0.8,
                      base + a * width - b * width * 0.7, base - a * width - b * width * 0.7,
                      base - d * width, mid + a * width * 0.6 + b * width * 0.3, mid - a * width * 0.6 + b * width * 0.3,
                      mid - b * width * 0.5, tip]), color, bone, fuse=False, surface="shell")


def round_ear(k, c, r, n, bone, color, lining, half, eps=0.003):
    """A round ear, a flattened ball fused into the crown, a dark oval laid on its front (mirrored)."""
    c, n = np.array(c, float), unit(*n)
    k.add("ear", ell(c, r), color, bone, mirror=True)
    k.decal(hit(ell(c, r), c + n * 0.3, -n), n, half, lining, eps=eps, rings=10, segs=48, mirror=True)


def tuft(k, name, base, d, length, width, bone, color):
    base, d = np.array(base, float), unit(*d)
    a = np.cross(d, (0, 1.0, 0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a)
    b = np.cross(a, d)
    k.add(name, hull([base + a * width + b * width * 0.6, base - a * width + b * width * 0.6,
                      base + a * width - b * width * 0.6, base - a * width - b * width * 0.6,
                      base - d * width * 0.5, base + d * length]), color, bone, fuse=False, surface="fur")


def bear_eye(ink=INK, glint=GLINT, iris=None):
    """A round dark eye, two glints (u outward, v up); with iris, a ring of that colour round a dark pupil."""
    K, G = srgb(ink), srgb(glint)
    I = srgb(iris) if iris else None
    def look(u, v):
        c = np.tile(K, (len(u), 1))
        if I is not None:
            c[(u / 0.55) ** 2 + (v / 0.6) ** 2 > 1] = I
        c[((u - 0.28) / 0.3) ** 2 + ((v - 0.34) / 0.3) ** 2 < 1] = G
        c[((u + 0.3) / 0.12) ** 2 + ((v + 0.32) / 0.12) ** 2 < 1] = G
        return c
    return look


def board(ob):
    """The game's board: a square of 1.3 x max(height, 0.85 x max(width, length)); four corners show 0.853 w + 0.522 l."""
    V = np.array([v.co[:] for v in ob.data.vertices])
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    sq = 1.3 * max(h, 0.85 * max(w, l))
    s = 0.853 * w + 0.522 * l
    print(f"BOARD: height {h:.3f} width {w:.3f} length {l:.3f}; corners {s:.3f} of square {sq:.3f} = {s / sq:.2f}")


def hexagon(c, n, r, depth, turn=0.0):
    """A hexagonal prism: centre c of its front face, axis n into the comb, circumradius r."""
    c, n = np.array(c, float), unit(*n)
    a = np.cross(n, (0, 0, 1.0)); a /= np.linalg.norm(a)
    b = np.cross(n, a)
    P = []
    for i in range(6):
        t = math.pi / 3 * i + turn
        e = a * math.cos(t) * r + b * math.sin(t) * r
        P += [c + e, c + e + n * depth]
    return hull(P)


# --- Urslet ----------------------------------------------------------------------------------------------------------
H = np.array([0.0, -0.032, 0.402])          # the head's middle (its bone at 0, -0.02, 0.32)
HR = np.array([0.136, 0.118, 0.118])
B = np.array([0.0, 0.004, 0.172])           # the body's middle
BR = np.array([0.13, 0.116, 0.138])
CN = unit(0, -1, 0.18)                      # the honeycomb's face looks this way
CC = np.array([0.008, -0.126, 0.165])       # the comb's middle


def urslet(src, out):
    k = Kit(src, coat="fur")

    # The body: a round pear, fullest low; the thighs fused into its bottom; a little round tail
    def pear(V):
        V = V.copy()
        s = 1 + 0.08 * np.clip(-V[:, 2], 0, 1) - 0.1 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell(B, BR, deform=pear), BROWN, "body")
    k.add("chest", ell((0, -0.02, 0.27), (0.1, 0.09, 0.06)), BROWN, "body")
    k.add("tail", ell((0, 0.118, 0.098), (0.036, 0.034, 0.034)), BROWN, "body")

    # The head: big and round, a little squared, a muzzle standing forward low on the face
    HEAD = ell(H, HR, deform=boxy(2.25))
    k.add("head", HEAD, BROWN, "head")
    MUZ = ell(H + (0, -0.094, -0.046), (0.056, 0.044, 0.042))
    k.add("muzzle", MUZ, CREAM, "head")
    k.paint(lambda P: inside(np.linalg.norm((P - (H + (0, -0.094, -0.046))) / (0.06, 0.05, 0.047), axis=1) - 1, 0.002)
            * (P[:, 1] < H[1] - 0.05), CREAM, on=["muzzle", "head"])

    # Round ears on the crown's corners, the lining darker; a small tuft of three points on the crown
    round_ear(k, (0.102, -0.018, 0.49), (0.042, 0.024, 0.04), (0.2, -1, 0.12), "ear{s}", BROWN, LINING, (0.023, 0.023))
    for i, (x, d) in enumerate(((0.0, (0, -0.1, 1)), (0.018, (0.4, 0.1, 1)), (-0.016, (-0.35, 0.15, 1)))):
        tuft(k, f"tuft{i}", H + (x, -0.03, HR[2] - 0.018), d, 0.032, 0.012, "head", BROWN)

    # The honey nose, glazed and dripping
    NC = hit(MUZ, H + (0, -0.4, -0.022), (0, 1, 0))
    k.add("nose", ell(NC + (0, 0.005, 0), (0.021, 0.014, 0.015)), HONEY, "head", fuse=False, surface="plastic")
    for i, (dx, l) in enumerate(((0.011, 0.01),)):
        top = NC + (dx, -0.004, -0.008)
        k.add(f"drip{i}", tube([top, top + (0, -0.004, -l)], [0.0095, 0.0085]), HONEY, "head", fuse=False,
              surface="plastic")
        k.add(f"drop{i}", ell(top + (0, -0.004, -l - 0.004), (0.011, 0.011, 0.012)), HONEY, "head", fuse=False,
              surface="plastic")
    # A small mouth under it: one little arc bowed up in the middle
    def mouth(u, v):
        c = np.tile(srgb(CREAM), (len(u), 1))
        c[(np.abs(v - (0.35 - 0.8 * u * u)) < 0.3) & (np.abs(u) < 0.86)] = srgb(MOUTH)
        c[(np.abs(u) < 0.16) & (v > 0.3)] = srgb(MOUTH)
        return c
    MC = hit(MUZ, H + (0, -0.4, -0.068), (0, 1, 0))
    k.decal(MC, unit(0, -1, -0.3), (0.022, 0.014), mouth, eps=0.003, rings=10, segs=48)

    # The pale crescent on the chest under the chin, its horns up
    k.paint(crescent(B, 0.12, 0.262, 0.05, 0.86, 0.022, B[1] - 0.04), MOON, on=["body", "chest"])

    # The broken honeycomb hugged to the belly: chunky hexagonal cells, each with a darker hollow, honey dripping off
    a = np.cross(CN, (0, 0, 1.0)); a /= np.linalg.norm(a)
    b = -np.cross(CN, a)                        # up the comb
    r = 0.0205
    cells = [(0, 0), (1, 0), (-1, 0), (0.5, 0.87), (-0.5, 0.87), (1.5, 0.87), (-1.5, 0.87), (0.5, -0.87),
             (-0.5, -0.87), (0, -1.74), (1, 1.74), (-1, 1.74)]
    for i, (p, q) in enumerate(cells):
        c = CC + (a * p + b * q) * r * 1.78 - CN * 0.004 * ((i * 7) % 3)
        k.add(f"comb{i}", hexagon(c, -CN, r, 0.026, turn=math.pi / 6), COMB, "body", fuse=False, surface="plastic")
        k.add(f"cell{i}", hexagon(c + CN * 0.002, -CN, r * 0.62, 0.006, turn=math.pi / 6), CELL, "body",
              fuse=False, surface="plastic")
    for i, (p, l) in enumerate(((0.25, 0.016), (-0.7, 0.01))):
        top = CC + a * p * r * 1.78 - b * 2.05 * r * 1.78 - CN * 0.004
        k.add(f"combdrip{i}", tube([top + (0, 0, 0.012), top - (0, 0, l)], [0.0095, 0.0085]), HONEY, "body",
              fuse=False, surface="plastic")
        k.add(f"combdrop{i}", ell(top - (0, 0, l + 0.005), (0.012, 0.012, 0.013)), HONEY, "body", fuse=False,
              surface="plastic")

    # Short thick arms round the comb, the paws holding its sides
    for s, nm, paw in ((1, "R", (0.058, -0.158, 0.135)), (-1, "L", (-0.062, -0.156, 0.205))):
        k.add("arm" + nm, tube([(0.098 * s, -0.03, 0.275), (0.122 * s, -0.08, 0.225), np.array(paw) * (0.95, 1, 1)],
                               [0.044, 0.04, 0.036]), BROWN, "arm" + nm)
        k.add("paw" + nm, ell(paw, (0.036, 0.032, 0.034)), BROWN, "arm" + nm)

    # Short thick legs fused into the body, broad feet
    for s, nm in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add("thigh" + nm, ell(np.array((0.07, -0.004, 0.09)) * m, (0.062, 0.066, 0.07)), BROWN, "leg" + nm)
        k.add("shin" + nm, tube([np.array(p) * m for p in [(0.074, -0.01, 0.065), (0.077, -0.018, 0.05)]],
                                [0.047, 0.044]), BROWN, "leg" + nm)
        k.add("foot" + nm, ell(np.array((0.078, -0.038, 0.023)) * m, (0.048, 0.062, 0.024)), BROWN, "leg" + nm)

    # Small worried brows over the eyes, their inner ends raised (in every state)
    for s in (1, -1):
        BC = hit(HEAD, H + (0.052 * s, -0.4, 0.046), (0, 1, 0))
        k.decal(BC, unit(0.42 * s, -0.9, 0.25), (0.019, 0.0075), DARK, up=unit(0.32 * s, 0, 1), eps=0.003, rings=8,
                segs=40)

    # Round dark eyes with two glints, on the brown above the muzzle
    de = unit(0.42, -0.88, 0.1)
    E = hit(HEAD, H + (0.05, -0.4, 0.008), (0, 1, 0))
    look = bear_eye(INK, GLINT, iris="#4A2C1C")
    def lay(state):
        k.decal(E, de, (0.02, 0.023), eye_look(state, look, srgb(BROWN), srgb(INK), line=0.2), eps=0.003, rings=20,
                segs=56, mirror=True)

    k.build(voxel=0.0052, band=0.005, wband=0.045)
    board(k.ob)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    urslet(*args())
