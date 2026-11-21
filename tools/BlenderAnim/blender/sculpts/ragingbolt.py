# Raging Bolt sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ragingbolt.py -- <the export's Raging Bolt.glb> <out.glb>
#
# Raikou's ancient kin, built anew on raikou.py's strokes and eyes (through sibling("raikou")). From the references: a
# yellow long-necked dinosaur on four short thick legs, white hooves with red and orange flames over them. The front
# of the neck, the chest and the belly white, edged with a red line; black chevrons down the back of the neck, along
# the back and on the flanks and thighs. On top of the neck a broad purple storm cloud lies like a disc, puffed round
# its rim, a little tipped to the front, and on it the small black head with a beak of a face and a red eye, blue
# whiskers of lightning at its sides, a fan of white and grey feathers rising behind it. A blue zigzag of lightning
# runs along the back and becomes the tail, ending in a spiked blue star, out to +X.
# Reference ratios (3D picture, hoof to feathers 1.0): the cloud 0.58 across (built 0.52 of the height, a mass as
# large as the trunk), the head 0.13 tall (built 0.17), the neck 0.15 across at its root and 0.1 at its top, the
# trunk 0.29 deep, the legs 0.15 showing. The neck is shortened: the reference's whole height is 2.1 of the trunk's
# length, the build's about 1.4, so that the model fills its cell; the head stands 0.15 below its joint (z 1.12).
# Bones: body (0, 0, 0.38); neck (0, -0.16, 0.48); head (0, -0.28, 1.12); legs (+-0.09, -0.14, 0.34);
# hind legs (+-0.09, 0.16, 0.34); tail (0, 0.24, 0.42).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, slab, outward, srgb, inside, smoothstep, tri, unit, lumpy, args, sibling)

r = sibling("raikou")

YELLOW = "#F4CE3E"
WHITE = "#F2F2EC"
RED = "#E2462E"
ORANGE = "#F49A30"
BLACK = "#2A2830"
CLOUD = "#9A8ED0"
CLOUD_DARK = "#7A6EB4"
BLUE = "#4EB6DE"
FEATHER, FEATHER_GREY = "#F2F2F6", "#B6B4C6"
HOOF = "#F0EEE4"
EYE, IRIS, PUPIL = "#F25068", "#B4182E", "#200810"

LEG_X = 0.095
FORE_Y, HIND_Y = -0.15, 0.17
H = np.array([0.0, -0.32, 0.95])            # the head, sitting on the cloud's top at its front
HR = np.array([0.06, 0.085, 0.072])
C = np.array([0.0, -0.24, 0.86])            # the cloud's middle
NECK = np.array([(0, -0.1, 0.42), (0, -0.19, 0.55), (0, -0.25, 0.69), (0, -0.27, 0.82)])
NRAD = [0.1, 0.085, 0.072, 0.066]
EYE_DIR = unit(0.7, -0.55, 0.2)
EYE_R = 0.026


def kite(base, d, length, width, flat=0.55, up=(0, 0, 1.0), mid=0.45):
    b = np.array(base, float)
    d = unit(*d)
    a = np.cross(d, up)
    if np.linalg.norm(a) < 1e-3:
        a = np.cross(d, (1.0, 0, 0))
    a /= np.linalg.norm(a)
    n = np.cross(a, d)
    w = width
    m = b + d * length * mid
    pts = [b + a * w, b - a * w, b + n * w * flat, b - n * w * flat,
           m + a * w * 0.62, m - a * w * 0.62, m + n * w * flat * 0.6, m - n * w * flat * 0.6, b + d * length]
    return hull(pts)


# The neck's centre line, finely, for paint in its own frame
def _line():
    pts = []
    for i in range(len(NECK) - 1):
        for t in np.linspace(0, 1, 24, endpoint=False):
            pts.append(NECK[i] * (1 - t) + NECK[i + 1] * t)
    pts.append(NECK[-1])
    P = np.array(pts)
    T = np.gradient(P, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True)
    S = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(P, axis=0), axis=1))])
    return P, T, S


NC, NT, NS = _line()


def neck_frame(P):
    """For points near the neck: arc length along it, the angle from its front (0 front, pi back), the side."""
    j = ((P[:, None, :] - NC[None]) ** 2).sum(2).argmin(1)
    rad = P - NC[j]
    rad -= NT[j] * (rad * NT[j]).sum(1, keepdims=True)
    rad /= np.linalg.norm(rad, axis=1, keepdims=True) + 1e-9
    front = np.array([0, -1.0, 0])[None] - NT[j] * (NT[j] @ np.array([0, -1.0, 0]))[:, None]
    front /= np.linalg.norm(front, axis=1, keepdims=True)
    ang = np.arccos(np.clip((rad * front).sum(1), -1, 1))
    return NS[j], ang, np.sign(P[:, 0] + 1e-9), j


def add_trunk(k):
    k.add("chest", ell((0, -0.12, 0.4), (0.13, 0.13, 0.13)), YELLOW, "body")
    k.add("middle", ell((0, 0.03, 0.39), (0.125, 0.16, 0.125)), YELLOW, "body")
    k.add("rump", ell((0, 0.17, 0.41), (0.12, 0.11, 0.12)), YELLOW, "body")


def add_neck(k):
    k.add("necklow", tube(NECK[:2], NRAD[:2]), YELLOW, "body")
    k.add("neck", tube(NECK[1:], NRAD[1:]), YELLOW, "neck")
    # White down the front of the neck, the chest and the belly, edged with a red line
    def front(P, extra):
        s, ang, _, j = neck_frame(P)
        neck = inside(ang - (1.05 + extra), 0.02) * (P[:, 2] > 0.42) * (P[:, 2] < 0.84)
        chest = inside(P[:, 1] - (-0.15 - extra * 0.2 + 0.0), 0.004) * inside(P[:, 2] - 0.5, 0.004) \
            * inside(np.abs(P[:, 0]) - (0.09 + extra * 0.06), 0.004) * (P[:, 2] > 0.3)
        belly = inside(P[:, 2] - (0.33 + extra * 0.03), 0.003) * inside(np.abs(P[:, 0]) - (0.085 + extra * 0.06), 0.004) \
            * (P[:, 2] > 0.2) * (P[:, 1] < 0.12)
        return np.maximum(np.maximum(neck, chest), belly)
    k.paint(lambda P: front(P, 0.22), RED, on=["neck", "necklow", "chest", "middle", "rump"])
    k.paint(lambda P: front(P, 0.0), WHITE, on=["neck", "necklow", "chest", "middle", "rump"])
    # Black chevrons down the back of the neck, pointing down its length
    def chevrons(P):
        s, ang, _, _ = neck_frame(P)
        lat = (math.pi - ang)                       # 0 at the back, growing to either side
        m = np.zeros(len(P))
        for s0 in (0.12, 0.2, 0.28, 0.36, 0.44):
            d = np.abs((s - s0) - 0.05 * lat) - 0.014
            m = np.maximum(m, inside(d, 0.004) * (lat < 1.15))
        return m * (P[:, 2] > 0.48) * (P[:, 2] < 0.84)
    k.paint(chevrons, BLACK, on=["neck", "necklow"])


def add_legs(k):
    for name, bone, y, mass in (("fore", "leg{s}", FORE_Y, (0.085, 0.1, 0.13)), ("hind", "hind{s}", HIND_Y,
                                                                                (0.095, 0.12, 0.14))):
        x = LEG_X
        k.add(name + "mass", ell((x + 0.01, y, 0.29), mass), YELLOW, bone, mirror=True)
        k.add(name, tube([(x, y, 0.33), (x + 0.012, y - 0.005, 0.18), (x + 0.015, y - 0.01, 0.06)],
                         [0.075, 0.064, 0.06]), YELLOW, bone, mirror=True)
        k.add(name + "hoof", ell((x + 0.015, y - 0.02, 0.03), (0.07, 0.08, 0.032)), HOOF, bone, mirror=True)
    k.paint(lambda P: inside(P[:, 2] - 0.06, 0.003), HOOF, on=["fore", "hind"])
    # Red and orange flames over each hoof, standing up in front
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        for bone, y in ((f"leg{sd}", FORE_Y), (f"hind{sd}", HIND_Y)):
            x = (LEG_X + 0.015) * s
            for j, (dx, d, ln) in enumerate(((0.0, (0, -0.3, 1), 0.1), (0.04, (0.6, -0.2, 0.8), 0.08),
                                             (-0.04, (-0.6, -0.2, 0.8), 0.08))):
                b = np.array((x + dx * s, y - 0.06, 0.07))
                dd = (d[0] * s, d[1], d[2])
                k.add(f"flame{bone}{j}", kite(b, dd, ln, 0.026, flat=0.6, up=(0, 1.0, 0)), RED, bone, fuse=False,
                      surface="shell")
                k.add(f"flametip{bone}{j}", kite(b + unit(*dd) * ln * 0.45, dd, ln * 0.55, 0.016, flat=0.6,
                                                 up=(0, 1.0, 0)), ORANGE, bone, fuse=False, surface="shell")


def add_stripes(k):
    """Black chevrons on the flanks and thighs, and a row down the back."""
    for y, z in ((-0.06, 0.4), (0.07, 0.42)):
        r.vee(k, (0.12, y, z), (1, 0, 0.25), 0.13, BLACK, spread=0.8, w=0.024)
    r.vee(k, (LEG_X + 0.08, HIND_Y + 0.01, 0.3), (1, 0.15, 0), 0.11, BLACK, spread=0.7, w=0.022)
    r.vee(k, (LEG_X + 0.07, FORE_Y, 0.28), (1, -0.2, 0), 0.1, BLACK, spread=0.7, w=0.02)
    def back(P):
        m = np.zeros(len(P))
        for y0 in (-0.08, 0.02, 0.12, 0.22):
            d = np.abs((P[:, 1] - y0) + 0.6 * np.abs(P[:, 0])) - 0.014
            m = np.maximum(m, inside(d, 0.004) * inside(np.abs(P[:, 0]) - 0.07, 0.004))
        return m * (P[:, 2] > 0.47)
    k.paint(back, BLACK, on=["chest", "middle", "rump"])


def add_cloud(k):
    """The storm cloud: a broad thick disc tipped a little to the front, puffed round its rim, darker beneath."""
    k.add("cloud", ell(C, (0.22, 0.19, 0.05), rot=(12, 0, 0)), CLOUD, "head")
    for i, a in enumerate(np.radians(np.arange(0, 360, 30) + 15)):
        rr = 0.045 + 0.01 * (i % 2)
        x, y = math.sin(a) * 0.235, -math.cos(a) * 0.2
        p = C + np.array((x, y, -(-y) * math.sin(math.radians(12))))
        k.add(f"puff{i}", ell(p, (rr * 1.25, rr * 1.1, rr * 0.95)), CLOUD, "head")
    # the curls on its top, swirls of puffs
    for i, (dx, dy) in enumerate(((0.12, 0.02), (-0.12, 0.02), (0.07, 0.11), (-0.07, 0.11))):
        p = C + np.array((dx, dy, 0.035 + dy * math.sin(math.radians(12))))
        k.add(f"curl{i}", ell(p, (0.04, 0.04, 0.03)), CLOUD, "head")
    k.paint(lambda P: inside(P[:, 2] - (C[2] - 0.035 + 0.21 * (P[:, 1] - C[1])), 0.006)
            * (np.linalg.norm((P - C) / (0.32, 0.28, 0.12), axis=1) < 1), CLOUD_DARK, on=["cloud", "puff"])


def add_head(k):
    k.add("head", ell(H, HR), BLACK, "head")
    # the beak of a face, pointing forward and down
    k.add("beak", ell(H + (0, -0.075, -0.03), (0.036, 0.06, 0.04), rot=(25, 0, 0)), BLACK, "head")
    k.add("seat", ell(H + (0, 0.02, -0.06), (0.06, 0.07, 0.04)), BLACK, "head")
    # blue whiskers of lightning curling out from the face's sides
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        b = H + (0.045 * s, -0.06, -0.05)
        k.add(f"whisk{sd}0", kite(b, (s, -0.4, -0.2), 0.07, 0.016, flat=0.8), BLUE, "head", fuse=False,
              surface="shell")
        k.add(f"whisk{sd}1", kite(b + np.array((0.06 * s, -0.025, -0.012)), (s * 0.5, 0.2, 0.8), 0.05, 0.014,
                                  flat=0.8), BLUE, "head", fuse=False, surface="shell")
    # the fan of feathers rising up and back behind the head
    for j, (dx, d, ln, col) in enumerate(((0.0, (0, 0.35, 1), 0.16, FEATHER), (0.04, (0.45, 0.4, 0.9), 0.14, FEATHER),
                                         (-0.04, (-0.45, 0.4, 0.9), 0.14, FEATHER),
                                         (0.075, (0.8, 0.45, 0.5), 0.11, FEATHER_GREY),
                                         (-0.075, (-0.8, 0.45, 0.5), 0.11, FEATHER_GREY))):
        b = H + np.array((dx, 0.05, 0.03))
        k.add(f"feather{j}", kite(b, d, ln, 0.04, flat=0.45, up=(0, 1.0, 0), mid=0.55), col, "head", fuse=False,
              surface="fur")


def add_bolt(k):
    """The blue zigzag: up along the back from the shoulders, then the tail out to +X ending in a spiked star."""
    zig = [(-0.12, 0.49), (-0.07, 0.56), (-0.02, 0.5), (0.04, 0.57), (0.09, 0.51), (0.15, 0.57), (0.2, 0.5)]
    for i, (a, b) in enumerate(zip(zig, zig[1:])):
        a, b = np.array(a), np.array(b)
        d = (b - a) / np.linalg.norm(b - a); nn = np.array((-d[1], d[0]))
        a0, b0 = a - d * 0.01, b + d * 0.01
        outl = [a0 + nn * 0.016, b0 + nn * 0.016, b0 - nn * 0.016, a0 - nn * 0.016]
        k.add(f"backbolt{i}", slab(outl, (0, 0, 0), (0, 1.0, 0), (0, 0, 1.0), 0.032), BLUE,
              "body" if i < 4 else "tail", fuse=False, surface="shell")
    ex = unit(0.6, 0.8, 0)
    o = np.array((0.0, 0.25, 0.46))
    path = [(0.0, 0.0), (0.08, 0.14), (0.14, 0.02), (0.22, 0.16), (0.28, 0.06)]
    for i, (a, b) in enumerate(zip(path, path[1:])):
        a, b = np.array(a), np.array(b)
        d = (b - a) / np.linalg.norm(b - a); nn = np.array((-d[1], d[0]))
        a0, b0 = a - d * 0.012, b + d * 0.012
        outl = [a0 + nn * 0.018, b0 + nn * 0.018, b0 - nn * 0.018, a0 - nn * 0.018]
        k.add(f"tailbolt{i}", slab(outl, o, ex, (0, 0, 1), 0.03 + 0.002 * i), BLUE, "tail", fuse=False,
              surface="shell")
    tip = o + ex * path[-1][0] + np.array((0, 0, path[-1][1]))
    k.add("star", outward(ell(tip + ex * 0.03, (0.035, 0.035, 0.035), sub=3)), BLUE, "tail", fuse=False,
          surface="shell")
    rs = np.random.RandomState(3)
    for j in range(10):
        dd = rs.normal(size=3); dd /= np.linalg.norm(dd)
        k.add(f"starspike{j}", kite(tip + ex * 0.03 + dd * 0.02, dd, 0.06, 0.016, flat=0.8), BLUE, "tail",
              fuse=False, surface="shell")


SHUT_LINE = "#C8C6D2"


def eye_top(u):
    return 0.3 + 0.45 * u


def eyes(k, state):
    p, n = r.on_ell(H, HR, EYE_DIR)
    r.lay_eyes(k, state, p, n, EYE_R, r.eye_open(EYE, IRIS, PUPIL, iris_r=(0.62, 0.8)), BLACK, SHUT_LINE, eye_top)


if __name__ == "__main__":
    src, dst = args()
    k = Kit(src, coat="fur")
    add_trunk(k)
    add_legs(k)
    add_neck(k)
    add_stripes(k)
    add_cloud(k)
    add_head(k)
    add_bolt(k)
    k.build(voxel=0.008, band=0.004)
    k.eye_states(lambda s: eyes(k, s))
    k.export(dst)
