# Crustle sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/crustle.py -- <the export's Crustle.glb> <out.glb>
#
# From the references: a red-orange crab carrying a great block of layered rock. The block is most of the height, a
# squared slab with bevelled corners leaning a little, its sides banded: dark brown at the bottom, light brown, a dark
# band, an orange band, light brown, a dark band and a light top, with small orange flecks. Under its front edge the
# crab's face peeks out: two round yellow eyes with black rims and black upright pupils on little bumps, close
# together, and a frowning mouth. Two big claws in front, low: a red-orange ball and a dark grey pincer of two hooked
# prongs pointing down. Three short pointed legs a side from under the block, red at the root and grey below.
#
# Grown from dwebble.py (its cone, stone chips and claw frame; sibling). Bones: root, body, head, arm{s}, leg{s},
# hind{s}, hind{s}.001.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, outward, hull, sibling

dw = sibling("dwebble")
cone, rotm = dw.cone, dw.rotm

RED, GREY, GREYDARK = "#D8603E", "#5E5652", "#3E3836"
YELLOW, RIM, PUPIL, MOUTH = "#F2C84A", "#2A201C", "#141010", "#5A2420"
ROCK = {"dark": "#4E423C", "brown": "#7E6A58", "band": "#3E3430", "tan": "#958068", "orange": "#E8A050",
        "top": "#9A8670"}
FLECK = "#E8A050"

# ---- the block: layers of rock, each its own hull on a bevelled footprint, leaning a little ------------------------
SLAB_C = np.array([0.0, 0.065, 0.36])          # its middle
SLAB_ROT = (4, -7, 0)                         # its lean: the front a little lower, tipped to one side
HALF_X, HALF_Y, HALF_Z, BEVEL = 0.215, 0.228, 0.15, 0.05
LAYERS = [  # (bottom, top) as shares of the height, colour, how far it stands out
    (0.00, 0.13, "dark", 0.0), (0.12, 0.33, "brown", 0.0), (0.32, 0.39, "band", 0.004),
    (0.38, 0.45, "tan", 0.0), (0.44, 0.50, "orange", 0.005), (0.49, 0.66, "tan", 0.0),
    (0.65, 0.71, "band", 0.004), (0.70, 1.0, "top", 0.0)]


def footprint(grow, seed):
    rs = np.random.RandomState(seed)
    hx, hy, b = HALF_X + grow, HALF_Y + grow, BEVEL
    pts = [(hx, -hy + b), (hx, hy - b), (hx - b, hy), (-hx + b, hy), (-hx, hy - b), (-hx, -hy + b),
           (-hx + b, -hy), (hx - b, -hy)]
    return [(x + 0.004 * rs.uniform(-1, 1), y + 0.004 * rs.uniform(-1, 1)) for x, y in pts]


def to_world(P):
    return np.asarray(P) @ rotm(SLAB_ROT).T + SLAB_C


def block(k):
    M = rotm(SLAB_ROT)
    for i, (z0, z1, col, grow) in enumerate(LAYERS):
        a, b = -HALF_Z + 2 * HALF_Z * z0, -HALF_Z + 2 * HALF_Z * z1
        pts = [(x, y, a) for x, y in footprint(grow, 3 * i + 1)] + [(x, y, b) for x, y in footprint(grow, 3 * i + 2)]
        k.add(f"layer{i}", hull(to_world(pts), merge=0.5), ROCK[col], "body", fuse=False, surface="shell")
    # small orange flecks: two on the top, one on the front, one on the +X side
    for i, (c, n, r) in enumerate((((0.07, -0.08, HALF_Z), (0, 0, 1), 0.02), ((-0.1, 0.1, HALF_Z), (0, 0, 1), 0.013),
                                   ((-0.12, -HALF_Y, 0.04), (0, -1, 0), 0.014),
                                   ((HALF_X, 0.08, -0.05), (1, 0, 0), 0.015))):
        c, n = np.array(c, float), np.array(n, float)
        a = np.cross(n, (0, 0, 1.0) if abs(n[2]) < 0.9 else (1.0, 0, 0)); a /= np.linalg.norm(a); bb = np.cross(n, a)
        ring = [c + n * 0.003 + r * f * (math.cos(t) * a + math.sin(t) * bb * 0.7)
                for t, f in zip(np.linspace(0, 2 * math.pi, 6, endpoint=False), (1.0, 0.75, 1.1, 0.7, 0.95, 0.85))]
        ring += [q - n * 0.006 for q in ring]
        k.add(f"fleck{i}", hull(to_world(ring), merge=0), FLECK, "body", fuse=False, surface="shell")


# ---- the crab under it -------------------------------------------------------------------------------------------
FACE_C, FACE_R = np.array([0.0, -0.165, 0.15]), np.array([0.085, 0.06, 0.06])
BUMP_C, BUMP_R = np.array([0.034, -0.2, 0.183]), np.array([0.03, 0.026, 0.028])


def body(k):
    k.add("body", ell((0.0, -0.04, 0.16), (0.15, 0.17, 0.075)), RED, "body")
    k.add("face", ell(FACE_C, FACE_R), RED, "head")
    k.add("bump", ell(BUMP_C, BUMP_R), RED, "head", mirror=True)


def open_eye(u, v):
    c = np.tile(srgb(RIM), (len(u), 1))
    r = np.sqrt(u * u + v * v)
    c[r < 0.8] = srgb(YELLOW)
    c[(u / 0.26) ** 2 + ((v + 0.02) / 0.5) ** 2 < 1] = srgb(PUPIL)
    return c


EYE_N = unit(0.25, -1, 0.3)


def eyes(k):
    p = BUMP_C + EYE_N * BUMP_R * 0.95
    def lay(state):
        k.decal(p, EYE_N, (0.0215, 0.0215), eye_look(state, open_eye, srgb(RED), srgb(RIM), line=0.17),
                eps=0.003, rings=20, segs=56, mirror=True)
    return lay


def mouth(k):
    def look(u, v):
        c = np.tile(srgb(RED), (len(u), 1))
        curve = 0.35 - 0.95 * u * u
        c[(np.abs(v - curve) < 0.22) & (np.abs(u) < 0.85)] = srgb(MOUTH)
        return c
    k.decal(FACE_C + np.array([0.0, -FACE_R[1] * 0.97, -0.012]), unit(0, -1, -0.15), (0.03, 0.016), look,
            eps=0.002, rings=12, segs=48)


# ---- the claws: a red ball, a dark grey hand and two hooked prongs ------------------------------------------------
CLAW_C, CLAW_R = np.array([0.175, -0.215, 0.105]), np.array([0.062, 0.058, 0.062])


def hook(root, mid, tip, r):
    """A hooked prong of two straight hulls: root to mid, then mid to the tip."""
    return cone(root, mid, r), cone(mid - unit(*(mid - root)) * r * 0.4, tip, r * 0.68)


def claws(k):
    k.add("arm", tube([(0.09, -0.13, 0.135), (0.14, -0.18, 0.115)], [0.034, 0.036]), RED, "arm{s}", mirror=True)
    k.add("claw", ell(CLAW_C, CLAW_R), RED, "arm{s}", mirror=True)
    k.add("hand", ell((0.18, -0.262, 0.085), (0.052, 0.04, 0.05), rot=(-20, 0, 0)), GREY, "arm{s}", mirror=True)
    for name, (root, mid, tip, r) in (("prong", ((0.205, -0.285, 0.075), (0.22, -0.32, 0.04), (0.205, -0.335, 0.0), 0.03)),
                                      ("prongin", ((0.15, -0.285, 0.07), (0.145, -0.315, 0.04), (0.125, -0.322, 0.008), 0.022))):
        a, b = hook(np.array(root), np.array(mid), np.array(tip), r)
        k.add(name + "a", a, GREY, "arm{s}", fuse=False, mirror=True, surface="shell")
        k.add(name + "b", b, GREYDARK, "arm{s}", fuse=False, mirror=True, surface="shell")


# ---- three short pointed legs a side ---------------------------------------------------------------------------------
LEGS = [("leg{s}", -0.06), ("hind{s}", 0.02), ("hind{s}.001", 0.1)]


def legs(k):
    for i, (bone, y) in enumerate(LEGS):
        k.add(f"legup{i}", outward(tube([(0.11, y, 0.13), (0.19, y - 0.01, 0.105)], [0.026, 0.023], seg=16, per=6)),
              RED, bone, fuse=False, mirror=True, surface="shell")
        k.add(f"legdn{i}", outward(tube([(0.19, y - 0.01, 0.105), (0.215, y - 0.018, 0.05), (0.222, y - 0.024, 0.0)],
                                        [0.022, 0.016, 0.005], seg=16, per=6)),
              GREY, bone, fuse=False, mirror=True, surface="shell")


def crustle(src, out):
    k = Kit(src, coat="shell")
    block(k)
    body(k)
    claws(k)
    legs(k)
    mouth(k)
    k.build(voxel=0.0055, smooth=3, band=0.006)
    k.eye_states(eyes(k))
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    crustle(src, out)
