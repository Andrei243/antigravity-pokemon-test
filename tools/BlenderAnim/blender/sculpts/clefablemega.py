# Mega Clefable sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py),
# grown from Clefable's script (clefable.py, which builds as it is read: its plump column of a body, the front() of
# it, the carved open smile with its tongue and the dark eyes with their outer stroke are copied here).
#   blender --background --factory-startup --python sculpts/clefablemega.py -- <the export's Clefable-Mega.glb> <out.glb>
#
# The design, from the two pictures: Clefable paler and slimmer, hovering; the largest mass is a pair of great
# butterfly wings on each side, a long pink forewing up and out, a shorter pink lobe under it and a hindwing pink at
# the root and yellow outward, with small magenta points where they meet the back; a white hood over the crown with a
# round white bun on top; the ears hang from the hood's sides as short pink lobes that end in black megaphones, each
# with a pale blue lens in its mouth; short arms; short legs close together; small dark eyes, each with a stroke round
# its outer side; a wide open smile with a pink tongue; pink blush on the cheeks.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, lathe, hull, srgb, inside, smoothstep, unit, boxy, eye_look, args

PINK, WHITE, INK, MOUTH, TONGUE = "#F8DAD2", "#FBF9F7", "#1C1416", "#8A2638", "#EE7A86"
BLUSH, WINGP, WINGY, MAGENTA = "#F4A0B2", "#F590A4", "#F7E48A", "#E03C86"
HORN, LENS, RIM = "#2A262E", "#A8DEF0", "#45404A"
C = np.array([0, 0, 0.44])                # the body's middle
R = np.array([0.2, 0.178, 0.31])


def plump(V):
    """A rounded column, squared a little and fuller below its middle (Clefable's)."""
    V = boxy(2.3)(V)
    s = 1 + 0.06 * np.clip(-V[:, 2], 0, 1) - 0.06 * np.clip(V[:, 2], 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    return V


def front(x, z, lift=0.0):
    """About the point of the body's front at (x, z), and lift off it along -y."""
    zn = (z - C[2]) / R[2]
    q = max(0.0, 1 - zn * zn - (x / R[0]) ** 2)
    return np.array([x, -R[1] * math.sqrt(q) - lift, z])


def wing(k, name, d, length, width, colours, y, bone="wing{s}", root=(0.07, 0.115, 0.515)):
    """
    A flat lobe of a butterfly's wing from the root along d (on +X; mirrored), in the upright plane through d, its
    face to the front and the back: a long leaf, round at its end. colours: (colour, from, to) along its length, 0..1.
    """
    d = unit(*d)
    up = np.array([0, 0, 1.0]) - d * d[2]
    up /= np.linalg.norm(up)
    pts = []
    for t in np.linspace(0, 1, 26):
        w = width * math.sin(math.pi * min(1.0, 0.04 + t * 0.96) ** 0.85) ** 0.7
        pts.append((w + 0.02 * t * t, t))
    for t in np.linspace(1, 0, 26)[1:-1]:
        w = width * math.sin(math.pi * min(1.0, 0.04 + t * 0.96) ** 0.85) ** 0.7
        pts.append((-w * 0.85 + 0.02 * t * t, t))
    origin = np.array(root) + np.array([0, y, 0])
    for i, (col, lo, hi) in enumerate(colours):
        part = []
        for a, b in _clip(pts, lo, hi):
            part.append((a, b * length))
        if len(part) >= 3:
            # the outline's first coordinate is across the lobe (along up), its second along it (along d)
            k.add(f"{name}{i}", slab(part, origin, up, d, 0.02), col, bone, fuse=False, mirror=True)


def _clip(poly, lo, hi):
    def cut(P, keep, at):
        o, n = [], len(P)
        for i in range(n):
            p, q = P[i], P[(i + 1) % n]
            kp, kq = keep(p), keep(q)
            if kp:
                o.append(p)
            if kp != kq:
                t = (at - p[1]) / (q[1] - p[1])
                o.append((p[0] + (q[0] - p[0]) * t, at))
        return o
    return cut(cut(list(poly), lambda p: p[1] >= lo, lo), lambda p: p[1] <= hi, hi)


def clefable_mega(src, out_path):
    k = Kit(src, coat="fur")

    # One slim body, head and body one mass; the crown on the head's bone so the face moves with it
    k.add("body", ell(C, R, deform=plump), PINK, "body")
    k.add("crown", ell((0, -0.01, 0.63), (0.17, 0.152, 0.12)), PINK, "head")
    # The white hood over the crown, standing a little proud of the face, and a round bun on top with a small point
    k.add("hood", ell((0, 0.004, 0.665), (0.192, 0.17, 0.112)), WHITE, "head")
    k.add("bun", ell((0, 0.012, 0.79), (0.062, 0.056, 0.058)), WHITE, "head")
    k.add("bunpoint", ell((0.006, 0.01, 0.84), (0.02, 0.019, 0.019)), WHITE, "head")
    def hood(P):
        edge = 0.618 - 0.03 * smoothstep(0.08, 0.19, np.abs(P[:, 0])) - 0.05 * smoothstep(-0.05, 0.12, P[:, 1])
        return inside(edge - P[:, 2], 0.0025) * (P[:, 2] > 0.55)
    k.paint(hood, WHITE)

    # The ears: short pink lobes hanging out of the hood's sides, ending in black megaphones turned out to the sides,
    # a pale blue lens in each mouth
    k.add("lobe", tube([(0.13, 0.0, 0.625), (0.185, -0.012, 0.595), (0.222, -0.024, 0.575)], [0.05, 0.047, 0.044]),
          PINK, "ear{s}", mirror=True)
    MC0, MD = np.array([0.21, -0.022, 0.574]), unit(1, -0.38, -0.12)
    k.add("horn", lathe(MC0, MD, [(0, 0), (0.044, 0), (0.05, 0.05), (0.07, 0.115), (0.082, 0.15),
                                   (0.076, 0.158), (0.064, 0.146), (0, 0.146)], seg=40),
          HORN, "ear{s}", fuse=False, mirror=True, surface="shell")
    k.add("lens", lathe(MC0, MD, [(0, 0.136), (0.06, 0.136), (0.06, 0.15), (0, 0.15)], seg=36),
          LENS, "ear{s}", fuse=False, mirror=True, surface="shell")

    # The wings: a long pink forewing up and out and swept back, a shorter pink lobe under it, a hindwing pink at the
    # root and yellow outward, each lobe a little behind the one before
    el, sw = math.radians(24), math.radians(38)
    wing(k, "fore", (math.cos(el) * math.cos(sw), math.cos(el) * math.sin(sw), math.sin(el)), 0.62, 0.15,
         [(WINGP, 0, 1)], 0.0)
    el, sw = math.radians(-12), math.radians(32)
    wing(k, "mid", (math.cos(el) * math.cos(sw), math.cos(el) * math.sin(sw), math.sin(el)), 0.4, 0.085,
         [(WINGP, 0, 1)], 0.026, root=(0.07, 0.115, 0.49))
    el, sw = math.radians(-50), math.radians(22)
    wing(k, "hind", (math.cos(el) * math.cos(sw), math.cos(el) * math.sin(sw), math.sin(el)), 0.33, 0.09,
         [(WINGP, 0, 0.42), (WINGY, 0.42, 1)], 0.052, root=(0.07, 0.115, 0.47))
    # Small magenta points where the wings meet the back, one over the forewing, one under the middle lobe
    for i, (base, tip) in enumerate([((0.09, 0.085, 0.58), (0.21, 0.11, 0.69)),
                                     ((0.1, 0.09, 0.43), (0.23, 0.1, 0.35))]):
        base, tip = np.array(base), np.array(tip)
        a, b = np.array([0, 0, 1.0]) * 0.03, np.array([0, 1.0, 0]) * 0.018
        k.add(f"point{i}", hull([base + a, base - a, base + b, base - b, tip]), MAGENTA, "wing{s}", fuse=False,
              mirror=True)

    # Short arms, forward and a little down, a round hand on each
    k.add("arm", tube([(0.13, -0.03, 0.505), (0.17, -0.08, 0.478), (0.186, -0.118, 0.458)], [0.045, 0.04, 0.036]),
          PINK, "arm{s}", mirror=True)
    k.add("hand", ell((0.19, -0.13, 0.452), (0.043, 0.04, 0.042)), PINK, "arm{s}", mirror=True)

    # Short legs close together, small feet hanging (it hovers)
    k.add("leg", tube([(0.072, -0.01, 0.19), (0.076, -0.028, 0.115)], [0.058, 0.052]), PINK, "leg{s}", mirror=True)
    k.add("foot", ell((0.078, -0.048, 0.088), (0.052, 0.066, 0.036), rot=(-12, 0, 0)), PINK, "leg{s}", mirror=True)

    # The wide open smile, carved under the eyes, flat along its top, with a tongue at its bottom
    def d_shape(V):
        V = V.copy()
        V[:, 2] = np.minimum(V[:, 2], 0.3)
        return V
    MC = front(0, 0.505)
    k.add("mouth", ell(MC + (0, 0.004, -0.004), (0.046, 0.022, 0.032), deform=d_shape), MOUTH, "head", carve=True)
    k.add("tongue", ell(MC + (0, 0.012, -0.021), (0.028, 0.012, 0.01)), TONGUE, "head", fuse=False)

    # Pink blush on the cheeks
    db = unit(0.5, -0.86, 0)
    k.decal(front(0.118, 0.545), db, (0.026, 0.016), BLUSH, eps=0.003, rings=10, segs=40, mirror=True)

    # Small dark eyes with a white glint as Clefable's, a dark stroke curving round the outer side of each
    CP, CE = srgb(PINK), srgb(INK)
    def eye(u, v):
        c = np.tile(CP, (len(u), 1))
        c[((u + 0.12) / 0.46) ** 2 + (v / 0.78) ** 2 < 1] = CE
        c[((u - 0.0) / 0.2) ** 2 + ((v - 0.36) / 0.2) ** 2 < 1] = (1, 1, 1)
        r = np.sqrt(((u + 0.05) / 0.86) ** 2 + (v / 0.95) ** 2)
        c[(np.abs(r - 0.95) < 0.15) & (u > 0.25)] = CE
        return c
    de = unit(0.36, -0.92, 0.1)
    E = front(0.07, 0.58)
    def eyes(state):
        k.decal(E, de, (0.031, 0.036), eye_look(state, eye, CP, CE), eps=0.004, rings=20, segs=56, mirror=True)

    k.build(voxel=0.0062)
    k.eye_states(eyes)
    k.export(out_path)


if __name__ == "__main__":
    src, out = args()
    clefable_mega(src, out)
