# Toucannon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/toucannon.py -- <the export's Toucannon.glb> <out.glb>
# Pikipek's line grown (its frame's names, feet, tail and way of making the eyes, imported from pikipek.py): from the
# references, a toucan standing upright, a black egg of a body; a great bill bigger than the head, curving down, its
# upper half banded yellow at the root, then pale orange, orange and red-orange to a black tip, its lower half red-
# orange; a black ring like a muzzle on each side of the bill's root; a small black head with a yellow face round the
# eye and down the cheek; a half-lidded blue eye under a dark lid with a grey arc over it; a long white chest from the
# throat down to a grey zigzag over the black belly; black wings folded at the sides; red-orange feathers at the hips;
# a long black tail hanging behind to the ground; short bright blue legs with black claws.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, blade, lathe, sheet, outward, srgb, inside, tri, unit, args, sibling

p = sibling("pikipek")
g = p.g
BLACK, WHITE, YELLOWF, ZIG, HIP = "#2E2F36", "#F4F4F1", "#F6DC3A", "#9EA0A8", "#E2482A"
BEAKBANDS = [(0.03, 0.3, "#F8D93C"), (0.3, 0.46, "#F8AE3C"), (0.46, 0.62, "#F17C30"), (0.62, 0.8, "#E34B27")]
LOWER, LEGC, CLAW, LIDC, ARC = "#E44A28", "#3CB2E6", "#1E1E22", "#2B3B70", "#A2A4AE"


def frame():
    F = p.frame()
    F.H = np.array([0, -0.042, 0.326])              # the head's centre, over its joint (0, -0.048, 0.256)
    F.HR = np.array([0.048, 0.052, 0.048])
    F.BODY = ((0, 0.016, 0.182), (0.076, 0.08, 0.106), (10, 0, 0))
    F.NECK = ((0, -0.026, 0.27), (0.056, 0.056, 0.05))
    F.LEGX, F.LEGTOP, F.ANKLE, F.LEGY = 0.04, 0.095, 0.017, 0.0
    F.LEGR = (0.0098, 0.0088)
    F.TOE, F.HEEL, F.TOER = 0.048, 0.03, 0.0066
    # the upper bill: an ellipsoid (centre, radii, tilt) bent down and tapered by beak_shape
    F.BEAK = (np.array([0, -0.18, 0.279]), np.array([0.03, 0.122, 0.05]), 25)
    F.LOWER = ((0, -0.15, 0.265), (0.019, 0.098, 0.019), 25)
    F.NOSTRIL = (0.12, 64)                          # where along the bill (its share) and how far round from below
    F.EYEDIR = unit(0.66, -0.68, 0.14)
    F.EYE = (0.0175, 0.018)
    F.VOXEL = 0.0042
    F.BAND, F.WBAND = 0.005, 0.045
    F.FACE = [(-0.075, 0.004), (-0.03, 0.016), (0.0, 0.022), (0.022, 0.012), (0.032, -0.02), (0.026, -0.064),
              (-0.1, -0.064), (-0.1, 0.0)]
    F.CHESTTOP, F.CHESTEDGE = 0.31, 0.118           # the white chest's top and its lower zigzag's mean height
    F.WING = ([(0.062, -0.018, 0.25), (0.078, 0.04, 0.19), (0.066, 0.098, 0.12), (0.036, 0.13, 0.085)],
              [0.03, 0.05, 0.04, 0.01], 0.36)
    F.HIPS = [((0.05, 0.05, 0.11), (0.082, 0.1, 0.072)), ((0.046, 0.068, 0.12), (0.07, 0.12, 0.096)),
              ((0.054, 0.04, 0.1), (0.088, 0.066, 0.064))]
    F.TAIL = (np.array([0, 0.074, 0.12]), unit(0.22, 0.5, -0.84), 0.13, 0.062, 15)
    return F


def beak_shape(V):
    """The unit sphere tapered to the tip (y -1) and bent down, the root (y +1) inside the face."""
    V = V.copy()
    s = np.clip((1 - V[:, 1]) / 2, 0, 1)
    V[:, 0] *= 1 - 0.55 * s
    V[:, 2] *= 1 - 0.45 * s
    V[:, 2] -= 0.75 * s ** 2
    return V


def place(F, V):
    c, r, tilt = F.BEAK
    m = np.array(Euler([math.radians(tilt), 0, 0]).to_matrix())
    return (V * r) @ m.T + c


def beak_point(F, s, phi, grow=1.0):
    """A point of the bill's surface by its share s from the root and its angle phi round from below."""
    vy = 1 - 2 * s
    rho = np.sqrt(np.clip(1 - vy * vy, 0, 1))
    V = np.stack([rho * np.sin(phi), vy, -rho * np.cos(phi)], axis=-1).reshape(-1, 3)
    return place(F, beak_shape(V) * grow)


def beak(k, F, base=BLACK, bands=BEAKBANDS, lower=LOWER):
    """The upper bill black, its bands rings a hair over it (seams underneath, under the lower bill)."""
    k.add("beak", outward((place(F, beak_shape(ell((0, 0, 0), (1, 1, 1), sub=4)[0]))
                           , ell((0, 0, 0), (1, 1, 1), sub=4)[1])), base, "head", fuse=False, surface="shell")
    for i, (s0, s1, col) in enumerate(bands):
        def at(u, v, s0=s0, s1=s1):
            phi = 0.02 + 2 * math.pi * 0.99 * u
            s = s0 + (s1 - s0) * v
            return beak_point(F, s.ravel(), phi.ravel(), grow=1.03).reshape(u.shape + (3,))
        k.add(f"beakband{i}", outward(sheet(at, 64, 8, 0.0025)), col, "head", fuse=False, surface="shell")
    c, r, tilt = F.LOWER
    k.add("beaklow", ell(c, r, rot=(tilt, 0, 0), deform=g.pointed), lower, "head", fuse=False, surface="shell")


def nostrils(k, F, ring=BLACK, inner=None):
    """A black ring like a muzzle on each side of the bill's root."""
    s, a = F.NOSTRIL
    c = beak_point(F, np.array([s]), np.array([math.radians(a)]), grow=1.0)[0]
    c2 = beak_point(F, np.array([s]), np.array([math.radians(a)]), grow=1.2)[0]
    n = (c2 - c) / np.linalg.norm(c2 - c)
    k.add("nostril", lathe(c - n * 0.003, n, [(0, 0), (0.012, 0), (0.012, 0.005), (0.0075, 0.005), (0.0075, 0.004),
                                                  (0, 0.004)], seg=32), ring, "head", fuse=False, mirror=True,
          surface="shell")
    k.add("nostrilin", lathe(c - n * 0.003, n, [(0, 0), (0.0076, 0), (0.0076, 0.0042), (0, 0.0042)], seg=32),
          bands_colour(), "head", fuse=False, mirror=True, surface="shell")


def bands_colour():
    return BEAKBANDS[0][2]


def chest(k, F, colour=WHITE, zig=ZIG):
    """The long white chest down to a grey zigzag over the black belly, on the front alone."""
    edge = lambda P: F.CHESTEDGE + 0.012 * tri(P[:, 0] / 0.034)
    front = lambda P: inside(P[:, 1] - 0.0 + 0.4 * np.abs(P[:, 0]), 0.004) * inside(np.abs(P[:, 0]) - 0.066, 0.004)
    k.paint(lambda P: front(P) * inside(edge(P) - 0.026 - P[:, 2], 0.0018) * inside(P[:, 2] - F.CHESTTOP, 0.004), zig)
    k.paint(lambda P: front(P) * inside(edge(P) - P[:, 2], 0.0018) * inside(P[:, 2] - F.CHESTTOP, 0.004), colour)


def face(k, F, colour=YELLOWF):
    k.paint(lambda P: p.side_mask(F, P, F.FACE), colour)


def wings_folded(k, F, colour=BLACK):
    pts, radii, f = F.WING
    k.add("wing", blade(pts, radii, (1, 0, 0), f), colour, "wing{s}", mirror=True)


def hips(k, F, colour=HIP):
    for i, (b, t) in enumerate(F.HIPS):
        k.add(f"hip{i}", g.spike(b, t, 0.026, 0.011, side=(0, 0, 1)), colour, "body", fuse=False, mirror=True)


INK, IRIS, GLINT = p.INK, srgb("#4AB4EA"), np.array([1.0, 1.0, 1.0])


def eye_open(u, v):
    """A half-lidded blue eye: a dark blue lid over its top, a black pupil, a grey arc over the top of its rim."""
    r = np.hypot(u, v)
    c = np.tile(IRIS, (len(u), 1))
    c[np.hypot(u + 0.06, v + 0.12) < 0.42] = INK
    c[np.hypot(u + 0.26, v + 0.28) < 0.14] = GLINT
    c[v > 0.12 - 0.12 * u] = srgb(LIDC)
    c[r > 0.8] = INK
    c[(r > 0.8) & (v > 0.05)] = srgb(ARC)
    return c


def toucannon(src, out_path):
    F = frame()
    k = Kit(src, coat="fur")
    p.body(k, F, body=BLACK, head=BLACK, neck=BLACK)
    chest(k, F)
    face(k, F)
    wings_folded(k, F)
    beak(k, F)
    nostrils(k, F)
    hips(k, F)
    p.tail(k, F, colour=BLACK, thick=0.011)
    p.legs(k, F, foot=LEGC, claw=CLAW)
    k.build(voxel=F.VOXEL, band=F.BAND, wband=F.WBAND)
    k.eye_states(p.eyes(k, F, lid=YELLOWF, open_look=eye_open))
    k.export(out_path)


if __name__ == "__main__":
    toucannon(*args())
