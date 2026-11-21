# Kleavor sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/kleavor.py -- <the export's Kleavor.glb> <out.glb>
# Grown from scyther.py, whose frame is written again here (scyther.py builds as it is read, so it can't be imported):
# the named joints, the head ball grown about its joint, its muzzle, the eyes as narrow almond patches laid by
# `eye_frame`, a thin pale neck.
#
# From the references: a mantis of stone. A round gold head crusted with dark brown rock: a jagged mask of crags over
# its jaw and muzzle, crags round the sides and back of its head, a tall crag standing up over its brow, and a pale horn
# from the top of its head swept back; fierce white eyes with a black pupil under rock brows, a pale fang at each side
# of the jaw. A thin pale neck; the trunk is one gold shield pointed below; a great faceted gold block at each hip, thin
# pale legs bent out from under them to gold faceted boots and dark rock claws, three in front and one behind. Thin
# gold arms, each through the top of a great axe of dark stone: a slab with a ragged edge, the largest masses of the
# design. No wings show in either picture (the C# sculpt has small pale ones): none are built.
# The axes are held forward and down at the sides (the art raises one and lowers the other: a pose for a clip), their
# planes turned 55 degrees out from straight ahead, so both game cameras see a face. They are the exception to arms
# inside the body's width: the hands are at x +-0.185, y -0.15 (in front of the belly), the axes reach x +-0.376.
# The trunk, arms and head crouch 0.08 below their joints (DZ), as the pictures crouch them.
# Masses ranked from the pictures: the two axes, the head with its rock, the hip blocks, the shield, the boots.
# Ratios (3D picture): axe length / height 0.38 (built 0.39), head with rock height / height 0.26 (0.27), head width /
# height 0.34 (0.3), hip block / height 0.14 (0.15).
# Bones: body (0, 0, 0.5); head (0, -0.02, 0.61); arms (+-0.08, -0.02, 0.57); wings (+-0.04, 0.06, 0.56);
# legs (+-0.06, 0.04, 0.4).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, unit, eye_look, polygon, args

GOLD, GOLDDARK, PALE = "#D6AC5E", "#B88E46", "#DCD6C8"
ROCK, ROCK2, HORN, INK, WHITE = "#4A3632", "#5E4740", "#EEE8DA", "#15171C", "#F8F8F6"

# --- the frame (scyther.py's, on Kleavor's joints) -------------------------------------------------------------------
HJ, HS = np.array([0.0, -0.02, 0.61]), 1.45    # the head's joint, and how much the head is grown about it
DZ = -0.08                                      # the trunk, arms and head crouch this far below the joints
HDROP = np.array([0.0, -0.03, DZ])              # the head carried lower and forward


def hs(p):
    """A point of the head drawn at its first size, grown about the head's joint, carried down with the crouch."""
    return HJ + (np.array(p, float) - HJ) * HS + HDROP


def dz(*p):
    return np.array(p, float) + (0, 0, DZ)


H, HR = hs([0.0, -0.05, 0.69]), np.array([0.066, 0.068, 0.062]) * HS     # the head ball
MUZ_C, MUZ_R = hs([0.0, -0.1, 0.665]), np.array([0.042, 0.045, 0.034]) * HS
NECK = [dz(0, -0.015, 0.57), dz(0, -0.04, 0.64)]
SHOULDER = dz(0.085, -0.025, 0.565)
ELBOW = dz(0.14, -0.06, 0.49)
WRIST = dz(0.185, -0.15, 0.46)
HIP = dz(0.125, 0.03, 0.46)
KNEE = np.array([0.18, -0.01, 0.22])
ANKLE = np.array([0.165, 0.015, 0.12])
AXE_E1 = unit(0.82, -0.57, 0)                 # along the haft, turned 55 degrees out from straight ahead


def point_along(a, d, length, w, t, up=(0, 0, 1)):
    """A crag, spike or claw: a kite-section root at a narrowing to a point length along d (a hull)."""
    a = np.array(a, float); d = unit(*d)
    s = np.cross(d, up)
    if np.linalg.norm(s) < 1e-3:
        s = np.cross(d, (1.0, 0, 0))
    s /= np.linalg.norm(s)
    f = np.cross(s, d)
    return hull([a + s * w / 2, a - s * w / 2, a + f * t / 2, a - f * t / 2, a - d * 0.01,
                 a + d * length * 0.5 + s * w * 0.28, a + d * length * 0.5 - s * w * 0.28, a + d * length])


def rock(c, r, seed, n=14):
    """A lump of rock: the hull of points jittered round an ellipsoid."""
    rs = np.random.RandomState(seed)
    P = rs.normal(size=(n, 3)); P /= np.linalg.norm(P, axis=1, keepdims=True)
    P *= rs.uniform(0.8, 1.0, (n, 1))
    return hull(np.array(c, float) + P * np.array(r, float))


def add_body(k):
    k.add("thorax", ell(dz(0, -0.01, 0.55), (0.06, 0.055, 0.06)), GOLD, "body")
    k.add("neck", tube(NECK, [0.026, 0.026]), PALE, "head")
    # the shield: the trunk's front, one gold plate pointed below
    pts = []
    for s in (1, -1):
        pts += [(0.058 * s, -0.07, 0.585), (0.055 * s, 0.045, 0.585), (0.088 * s, -0.085, 0.5), (0.08 * s, 0.055, 0.5),
                (0.045 * s, -0.075, 0.42), (0.04 * s, 0.03, 0.43)]
    pts += [(0, -0.07, 0.345), (0, 0.0, 0.37), (0, -0.098, 0.5)]
    k.add("shield", hull(np.array(pts) + (0, 0, DZ)), GOLD, "body", fuse=False, surface="shell")
    # a great faceted gold block at each hip
    k.add("hipblock", rock(HIP, (0.072, 0.076, 0.08), 7, 16), GOLD, "leg{s}", fuse=False, mirror=True,
          surface="shell")


def add_legs(k):
    k.add("shank", tube([HIP + (0.01, 0, -0.05), KNEE, ANKLE + (0, 0, 0.03)], [0.024, 0.022, 0.022]), PALE,
          "leg{s}", mirror=True)
    k.add("boot", rock(ANKLE + (0, -0.005, -0.04), (0.048, 0.058, 0.055), 11, 14), GOLD, "leg{s}", fuse=False,
          mirror=True, surface="shell")
    foot = ANKLE + np.array([0, 0, -0.085])
    for j, dx in enumerate((-0.025, 0.0, 0.025)):
        k.add(f"claw{j}", point_along(foot + (dx, -0.04, 0.0), (dx * 4, -1, -0.45), 0.075, 0.03, 0.026), ROCK,
              "leg{s}", fuse=False, mirror=True, surface="shell")
    k.add("heel", point_along(foot + (0, 0.04, 0.0), (0, 1, -0.4), 0.05, 0.028, 0.024), ROCK, "leg{s}", fuse=False,
          mirror=True, surface="shell")


def add_arms(k):
    k.add("shoulder", ell(SHOULDER, (0.034, 0.034, 0.034)), GOLD, "arm{s}", mirror=True)
    k.add("upper", tube([SHOULDER, (SHOULDER + ELBOW) / 2 + (0.006, 0, 0), ELBOW], [0.024, 0.022, 0.023]), GOLD,
          "arm{s}", mirror=True)
    k.add("fore", tube([ELBOW, (ELBOW + WRIST) / 2, WRIST], [0.023, 0.024, 0.026]), GOLD, "arm{s}", mirror=True)


AXE = [(a * 0.88, b * 0.92) for a, b in [(-0.05, 0.07), (0.03, 0.09), (0.1, 0.06), (0.17, 0.075), (0.215, 0.01),
       (0.19, -0.07), (0.23, -0.15), (0.17, -0.25), (0.09, -0.21), (0.02, -0.27), (-0.05, -0.2), (-0.03, -0.11),
       (-0.08, -0.03)]]
FACE = [(a * 0.88, b * 0.92) for a, b in [(0.0, 0.03), (0.12, 0.02), (0.16, -0.1), (0.1, -0.17), (0.02, -0.14)]]


def add_axes(k):
    """A great axe of dark stone on each forearm: the haft through its top, the blade hanging below, its face out."""
    o = WRIST + AXE_E1 * 0.015 + np.array([0, 0, -0.01])
    k.add("axe", slab(AXE, o, AXE_E1, (0, 0, 1), 0.045), ROCK, "arm{s}", fuse=False, mirror=True, surface="shell")
    n = np.cross(AXE_E1, (0, 0, 1.0))           # the face's normal: forward and in
    for sgn in (1, -1):
        k.add(f"axeface{'f' if sgn > 0 else 'b'}", slab(FACE, o + n * sgn * 0.024, AXE_E1, (0, 0, 1), 0.01), ROCK2,
              "arm{s}", fuse=False, mirror=True, surface="shell")
    # the haft's end standing out of the axe's top
    k.add("haft", tube([WRIST, WRIST + AXE_E1 * 0.18 + (0, 0, 0.03)], [0.022, 0.02]), GOLD, "arm{s}", mirror=True)


def add_head(k):
    k.add("head", ell(H, HR), GOLD, "head")
    k.add("muzzle", ell(MUZ_C, MUZ_R), GOLD, "head")
    # the mask of crags over the jaw and muzzle, its lower edge jagged
    pts = []
    for s in (1, -1):
        pts += [hs((0.06 * s, -0.08, 0.668)), hs((0.05 * s, -0.12, 0.665)), hs((0.03 * s, -0.135, 0.648)),
                hs((0.055 * s, -0.07, 0.625)), hs((0.025 * s, -0.11, 0.618))]
    pts += [hs((0, -0.142, 0.66)), hs((0, -0.1, 0.61)), hs((0, -0.06, 0.64))]
    k.add("mask", hull(pts), ROCK, "head", fuse=False, surface="shell")
    for i, (x, z, d) in enumerate(((0.045, 0.625, (0.4, -0.5, -0.8)), (0.0, 0.615, (0, -0.4, -1)),
                                   (0.065, 0.645, (0.9, -0.3, -0.5)))):
        k.add(f"jag{i}", point_along(hs((x, -0.1, z)), d, 0.035, 0.034, 0.026), ROCK, "head", fuse=False,
              mirror=x > 0, surface="shell")
    # rock brows over the eyes, jutting forward and out
    k.add("brow", point_along(hs((0.025, -0.098, 0.722)), (0.75, -0.45, 0.22), 0.075, 0.05, 0.03), ROCK, "head",
          fuse=False, mirror=True, surface="shell")
    # chunky crusts of rock on the sides, the top and the back of the head, half sunk in it
    for i, (d, r, seed) in enumerate((((1, 0.1, 0.25), (0.045, 0.04, 0.05), 3), ((0.75, 0.6, 0.45), (0.05, 0.045, 0.045), 5),
                                      ((0.8, 0.3, -0.5), (0.04, 0.04, 0.04), 9), ((0.45, 0.4, 0.8), (0.04, 0.045, 0.035), 13))):
        p = H + unit(*d) * HR * 0.95
        k.add(f"crust{i}", rock(p, r, seed, 12), ROCK if i % 2 == 0 else ROCK2, "head", fuse=False, mirror=True,
              surface="shell")
    k.add("backcrust", rock(H + unit(0, 0.6, 0.8) * HR * 0.95, (0.045, 0.035, 0.035), 17, 12), ROCK2, "head",
          fuse=False, surface="shell")
    # a tall crag standing up over the brow, and a pale horn from the crown swept back
    k.add("crest", point_along(hs((0.0, -0.065, 0.74)), (0, -0.3, 1), 0.13, 0.095, 0.036, up=(0, -1, 0)), ROCK,
          "head", fuse=False, surface="shell")
    k.add("horn", point_along(hs((0.0, 0.0, 0.745)), (0, 0.55, 1), 0.085, 0.03, 0.026, up=(0, -1, 0)), HORN, "head",
          fuse=False, surface="shell")
    # a pale fang at each side of the jaw
    k.add("fang", point_along(hs((0.04, -0.1, 0.63)), (0.2, -0.3, -1), 0.04, 0.022, 0.018), HORN, "head",
          fuse=False, mirror=True, surface="shell")


# --- the eyes: scyther.py's almonds, fierce, under the rock brows --------------------------------------------------------
EYE_HALF = (0.026 * HS, 0.018 * HS)
EYE_SHAPE = [(-1.0, -0.15), (-0.55, 0.2), (0.0, 0.5), (0.55, 0.7), (1.0, 0.6), (0.95, 0.1), (0.55, -0.55),
             (0.0, -0.7), (-0.6, -0.5)]


def eye_frame(side, d=(0.6, -0.76, 0.18)):
    de = unit(side * d[0], d[1], d[2])
    p = H + de / math.sqrt(((de / HR) ** 2).sum())
    n = (p - H) / HR ** 2
    return p, n / np.linalg.norm(n)


def eye_open(sclera, pupil):
    CW, CK = srgb(sclera), srgb(pupil)
    def eye(u, v):
        c = np.tile(CW, (len(u), 1))
        c[((u + 0.12) / 0.42) ** 2 + ((v + 0.02) / 0.7) ** 2 < 1] = CK
        c[((u + 0.0) / 0.15) ** 2 + ((v - 0.25) / 0.18) ** 2 < 1] = (1, 1, 1)
        return c
    return eye


def add_eyes(k, state, lid=GOLD, sclera=WHITE, pupil=INK):
    look = eye_look(state, eye_open(sclera, pupil), srgb(lid), srgb(INK), line=0.17)
    for side in (1, -1):
        c, n = eye_frame(side)
        col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
        shape = EYE_SHAPE if side == 1 else [(-a, b) for a, b in EYE_SHAPE]
        k.decal(c, n, EYE_HALF, col, eps=0.002, rings=20, segs=56, outline=polygon(shape))


def kleavor(src, out):
    k = Kit(src, coat="shell")
    add_body(k)
    add_legs(k)
    add_arms(k)
    add_axes(k)
    add_head(k)
    k.build(voxel=0.0055, smooth=4, band=0.005)
    k.eye_states(lambda s: add_eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    kleavor(*args())
