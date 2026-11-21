# Snom sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/snom.py -- <the export's Snom.glb> <out.glb>
#
# The design, from the references: a tiny larva lying on the ground. Its front is a big smooth white head like a
# rounded hood, as tall as the whole body, its lower front a soft two-lobed puff of a mouth; two small black eyes low
# on the head's sides, just above the ground. Behind and over the back of the head is its body, a lump of pale blue
# ice as tall as the head and a little wider, bristling with short pointed crystals over its top, sides and back.
# The pictures make it shorter than the old sculpt: about one and a half times as long as it is tall.
# Written to be grown from (frosmoth.py imports it): the colours, a crystal, the eye.
# Skeleton: body (0, 0.13, 0.04), seg1 (0, 0.05, 0.045), seg2 (0, -0.03, 0.055), head (0, -0.08, 0.08).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, srgb, unit, eye_look, args

WHITE, ICE, CRYSTAL, INK = "#F2F4F8", "#B8D8F0", "#CDE5F6", "#141418"
HEAD_C = np.array([0.0, -0.05, 0.072])
HEAD_R = np.array([0.064, 0.07, 0.072])
ICE_C = np.array([0.0, 0.04, 0.072])
ICE_R = np.array([0.064, 0.07, 0.066])
VOXEL = 0.0019


def on(c, r, az, el):
    """An ellipsoid's surface at azimuth az (from the front toward +X) and elevation el, and its outward way."""
    a, e = math.radians(az), math.radians(el)
    d = np.array([math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)])
    k = 1 / math.sqrt(((d / r) ** 2).sum())
    p = np.array(c) + d * k
    n = (p - c) / np.array(r) ** 2
    return p, n / np.linalg.norm(n)


def crystal(k, name, p, d, length, radius, bone, colour=CRYSTAL, sides=4, seed=0):
    """A short pointed crystal of ice: a pyramid from a base sunk in the surface at p out along d."""
    d = unit(*d)
    a = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a)
    b = np.cross(d, a)
    base = p - d * radius * 0.9
    pts = [base + radius * (math.cos(t) * a + math.sin(t) * b)
           for t in np.linspace(0, 2 * math.pi, sides, endpoint=False) + seed]
    pts.append(base - d * radius * 0.4)
    pts.append(p + d * length)
    k.add(name, hull(pts), colour, bone, fuse=False, surface="shell")


# (azimuth, elevation, length, base radius) on the ice: broad crystals in rings, which make its jagged outline; none
# on the front low down, where the head is
CRYSTALS = ([(az, 72, 0.04, 0.03) for az in (-30, 40, 130, -130)]
            + [(az, 38, 0.041, 0.031) for az in (-62, 64, 105, -100, 145, -150, 180)]
            + [(az, 4, 0.038, 0.03) for az in (-92, 90, 125, -125, 160, -162)]
            + [(az, -26, 0.034, 0.026) for az in (-110, 112, 150, -150)]
            + [(0, 52, 0.04, 0.028)])


def bone_at(y):
    return "seg2" if y < 0.0 else ("seg1" if y < 0.08 else "body")


def eye_open(u, v):
    c = np.tile(srgb(INK), (len(u), 1))
    c[((u + 0.3) / 0.3) ** 2 + ((v - 0.38) / 0.3) ** 2 < 1] = srgb("#FFFFFF")
    return c


EYE_AZ, EYE_EL, EYE_HALF = 50, -26, (0.0125, 0.015)


def snom(src, out):
    k = Kit(src, coat="fur")
    # the head and the puff of its mouth
    k.add("head", ell(HEAD_C, HEAD_R), WHITE, "head")
    for sx in (1, -1):
        k.add(f"mouth{sx}", ell((0.021 * sx, -0.105, 0.03), (0.028, 0.026, 0.025)), WHITE, "head")
    # the ice: one lump over the back of the head
    k.add("ice", ell(ICE_C, ICE_R), ICE, "seg1", surface="shell")
    for i, (az, el, ln, rd) in enumerate(CRYSTALS):
        p, n = on(ICE_C, ICE_R, az, el)
        if p[1] < HEAD_C[1] + 0.03:        # never on the head's side of the lump
            continue
        crystal(k, f"crystal{i}", p, n + np.array([0, 0.35, 0.1]), ln, rd, bone_at(p[1]), sides=5, seed=i * 0.7)
    k.build(voxel=VOXEL, smooth=3, band=0.003, wband=0.03)

    def lay(state):
        look = eye_look(state, eye_open, srgb(WHITE), srgb(INK), line=0.2)
        for sx in (1, -1):
            p, n = on(HEAD_C, HEAD_R, sx * EYE_AZ, EYE_EL)
            k.decal(p, n, EYE_HALF, look if sx > 0 else (lambda u, v, g=look: g(-u, v)), eps=0.0012,
                    rings=20, segs=56)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    snom(*args())
