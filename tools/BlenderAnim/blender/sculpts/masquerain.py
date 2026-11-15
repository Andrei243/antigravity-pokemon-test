# Masquerain sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/masquerain.py -- <the export's Masquerain.glb> <out.glb>
# Surskit grown into a moth: a small pale blue body, a round head drawn up into a long horn that sweeps back and up,
# a salmon mask across the face with two small dark eyes at its upper corners; two great antennae from the head's
# sides, each a half disc of salmon with a pointed tip, a white ring and a maroon half eye at its middle, white dashes
# round its rim, like a mask's eyes; four pale clear wings, long diamonds, two to a side under the body.
# (The pictures overrule the brief: the antennae are salmon-orange, not pink; Surskit's yellow is gone.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, unit, eye_look, args

BLUE, MASK, SALMON, RING, EYESPOT = "#BCD9EE", "#EE9273", "#F29A7C", "#F7F4F2", "#A04E72"
WING, WINGLINE = "#D6E7F5", "#B3D0EA"
INK, WHITE = srgb("#18141C"), np.array([1.0, 1.0, 1.0])

H = np.array([0.0, -0.052, 0.17])                    # the head's centre, over its joint (0, -0.05, 0.146)
HR = np.array([0.06, 0.057, 0.058])
R = 0.06


def round_head(P):
    return np.arctan2(P[:, 0], -(P[:, 1] - H[1])) * R, P[:, 2]


def half_disc(r, c=0.0, n=40, a0=0.0):
    """A half disc of radius r on the chord, its middle at c along it: (along, out) points, the chord kept at out a0."""
    t = np.linspace(math.pi, 0, n)
    return [(c + r * math.cos(x), a0 + r * math.sin(x)) for x in t]


def half_ring(r0, r1, c, n=40, a0=0.004):
    t = np.linspace(math.pi, 0, n)
    out_ = [(c + r1 * math.cos(x), a0 + r1 * math.sin(x)) for x in t]
    in_ = [(c + r0 * math.cos(x), a0 + r0 * math.sin(x)) for x in t[::-1]]
    return out_ + in_


def antenna(k, s, side):
    """A half disc on its chord, the chord running from the root up and out to a pointed tip, its arc bulging outward."""
    root = np.array([0.04 * s, -0.052, 0.196])
    d = unit(0.62 * s, 0.12, 0.78)                      # the chord
    n = unit(0.3 * s, -1.0, 0.12)                       # the face, toward the front and a little out
    e = np.cross(n, d); e /= np.linalg.norm(e)
    if e[0] * s < 0:
        e = -e
    RD, C = 0.13, 0.13                                 # the disc's radius and its middle along the chord
    # salmon: the half disc and the pointed tip beyond its far end
    arc = half_disc(RD, C, n=48)
    tipped = arc[:-6] + [(C + RD + 0.085, -0.004), (C + RD * 0.6, -0.004)]
    body = [(0.0, -0.004)] + tipped[1:]
    k.add(f"sail{side}", slab(body, root, d, e, 0.012), SALMON, f"ear{side}", fuse=False)
    # the white ring and the maroon half eye in it, thicker so they show on both faces
    k.add(f"ring{side}", slab(half_ring(0.056, 0.086, C), root, d, e, 0.016), RING, f"ear{side}", fuse=False)
    k.add(f"spot{side}", slab(half_disc(0.056, C, a0=0.004), root, d, e, 0.019), EYESPOT, f"ear{side}", fuse=False)
    # white dashes round the rim, pointing at the middle
    for i in range(9):
        th = math.pi * (i + 0.6) / 9.2
        a, b = math.cos(th), math.sin(th)
        p, q = -b, a
        r0, r1, w = 0.098, 0.12, 0.0075
        pts = [(C + a * r0 + p * w, b * r0 + q * w), (C + a * r1 + p * w, b * r1 + q * w),
               (C + a * r1 - p * w, b * r1 - q * w), (C + a * r0 - p * w, b * r0 - q * w)]
        k.add(f"dash{side}{i}", slab(pts, root, d, e, 0.016), RING, f"ear{side}", fuse=False)
    # a short stalk from the head into the sail's root
    k.add(f"stalk{side}", tube([H + np.array([0.025 * s, 0.0, 0.03]), root + d * 0.02], [0.016, 0.012]), BLUE,
          f"ear{side}.001")


def wing(k, s, name, bone, along, lift, tilt, length):
    """A long clear diamond with a darker line along its middle, raised in a V and tilted to show its face."""
    root = np.array([0.03 * s, along * 0.05, 0.1])
    d = unit(s, along, lift)
    flat = np.cross((0, 0, 1.0), d); flat /= np.linalg.norm(flat)
    up = np.cross(d, flat); up /= np.linalg.norm(up)
    ez = flat * math.cos(math.radians(tilt)) + up * math.sin(math.radians(tilt))
    w = 0.045
    diamond = [(0.0, 0.0), (length * 0.32, w), (length, 0.0), (length * 0.32, -w)]
    k.add(name, slab(diamond, root, d, ez, 0.008), WING, bone, fuse=False, surface="leaf")
    line = [(0.0, 0.0), (length * 0.32, 0.0), (length * 0.97, -0.002), (length * 0.32, -w * 0.55)]
    k.add(name + "line", slab(line, root, d, ez, 0.011), WINGLINE, bone, fuse=False,
          surface="leaf")


def masquerain(src, out):
    k = Kit(src, coat="shell")
    k.add("head", ell(H, HR), BLUE, "head")
    # the horn: the head drawn up into a long point sweeping back and up
    k.add("horn", tube([H + np.array([0, 0.005, 0.02]), (0, -0.01, 0.25), (0, 0.045, 0.31), (0, 0.11, 0.345)],
                       [0.045, 0.028, 0.015, 0.005]), BLUE, "head")
    k.add("thorax", ell((0, -0.03, 0.105), (0.036, 0.034, 0.036)), BLUE, "body")
    k.add("abdomen", ell((0, -0.018, 0.064), (0.026, 0.026, 0.028)), BLUE, "body")
    # the salmon mask across the face, its top edge rising to the eyes at its corners
    def mask(P):
        a, z = round_head(P)
        top = 0.186 + 0.12 * np.abs(a)
        return inside(z - top, 0.002) * inside(0.13 - z + 0.4 * a * a, 0.002) * inside(np.abs(a) - 0.06, 0.002) \
            * (P[:, 1] < H[1])
    k.paint(mask, MASK, on=["head"])
    for s, side in ((1, "R"), (-1, "L")):
        antenna(k, s, side)
        wing(k, s, f"fore{side}", f"wing{side}", -0.5, 0.45, 30, 0.17)
        wing(k, s, f"hind{side}", f"wing{side}.001", 0.55, 0.35, -30, 0.15)
    k.build(voxel=0.0042, smooth=3)

    def eye(u, v):
        c = np.tile(WHITE, (len(u), 1))
        c[u * u + v * v > 0.8 ** 2] = INK
        c[((u + 0.08) / 0.5) ** 2 + ((v + 0.05) / 0.58) ** 2 < 1] = INK
        c[((u + 0.24) / 0.17) ** 2 + ((v - 0.2) / 0.17) ** 2 < 1] = WHITE
        return c

    ED = unit(0.5, -0.82, 0.3)
    E = H + ED / np.linalg.norm(ED / HR)

    def eyes(state):
        look = eye_look(state, eye, srgb(MASK), INK, line=0.2)
        k.decal(E, ED, (0.016, 0.018), look, eps=0.004, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    masquerain(*args())
