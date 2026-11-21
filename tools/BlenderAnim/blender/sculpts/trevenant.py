# Trevenant sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/trevenant.py -- <the export's Trevenant.glb> <out.glb>
#
# Phantump grown (phantump.py), in its bark, dark sockets and green leaves: a tall dead tree walking. A thick trunk
# with two jagged dark bands across it widens into a crown that is its head; under a peaked brow a dark hollow with one
# red-pink eye in it. Two great antler branches rise from the crown's corners, forking, with small leaves, and a tuft
# of spiky green leaves sits between them. Short branch arms (a dark band at each shoulder) end in great claw hands of
# three hooked fingers, a big clump of spiky leaves on each (the design's largest masses after the trunk); held
# forward and down here, where the art flings them wide. Six thick roots, three a side, arch out and down as its legs.
# The skeleton: body (0, 0, 0.38), armR/armL at (+-0.08, 0, 0.48), legR/legL at (+-0.06, 0, 0.2).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, ring_band, polygon, srgb, inside, unit, tri, eye_look, sibling, args

P = sibling("phantump")
BARK, BARKDARK, SOCKET, LEAF, LEAFDARK = P.BARK, P.BARKDARK, P.SOCKET, P.LEAF, P.LEAFDARK
BAND = "#2C3140"
EYERED, EYEPINK, EYELIGHT = "#C8324A", "#F48FA2", "#FDE0E6"

TRUNK_TOP = 0.5
CROWN = np.array([0.0, 0.004, 0.565]); CROWNR = np.array([0.104, 0.088, 0.1])
EYE = np.array([0.0, -0.088, 0.548])
HAND = np.array([0.19, -0.165, 0.38])


def spike(base, d, length, width):
    """A pointed leaf blade: a flat-faced hull from a lens-shaped foot to a point."""
    d = unit(*d)
    a = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a); b = np.cross(d, a)
    base = np.array(base, float)
    pts = [base + a * width, base - a * width, base + b * width * 0.55, base - b * width * 0.55,
           base + d * length * 0.45 + a * width * 0.7, base + d * length * 0.45 - a * width * 0.7, base + d * length]
    return hull(pts)


def tuft(k, centre, radii, bone, n, length, width, seed, up=(0, 0, 1)):
    """A clump of spiky leaves: a green dome fused in, and loose pointed blades fanned out of its upper side."""
    k.add("tuft", ell(centre, np.array(radii) * 0.85), LEAFDARK, bone, surface="leaf")
    rs = np.random.RandomState(seed)
    upv = unit(*up)
    for i in range(n):
        az = 2 * math.pi * (i + 0.5 * rs.rand()) / n
        el = -0.15 + 1.1 * rs.rand()
        d = np.array([math.cos(az) * math.cos(el), math.sin(az) * math.cos(el), math.sin(el)])
        d = unit(*(d + upv * 0.6))
        base = np.array(centre) + np.array(radii) * d * 0.75
        k.add(f"blade{bone}{seed}{i}", spike(base, d, length * (0.8 + 0.4 * rs.rand()), width),
              LEAF if i % 2 else LEAFDARK, bone, fuse=False, surface="leaf")


def trunk(k):
    k.add("trunk", tube([(0, 0.0, 0.17), (0, 0.0, 0.22), (0, 0.0, 0.33), (0, 0.0, 0.43), (0, 0.003, TRUNK_TOP)],
                        [0.066, 0.086, 0.074, 0.078, 0.086]), BARK, "body")
    k.add("crown", ell(CROWN, CROWNR, deform=P.cylish(3.0)), BARK, "body")
    # the peaked brow over the hollow
    k.add("brow", ell((0, -0.058, 0.6), (0.088, 0.04, 0.03), rot=(-12, 0, 0)), BARK, "body")
    # a little point under the trunk, among the roots
    k.add("foot", tube([(0, 0.0, 0.18), (0, 0.004, 0.12), (0, 0.008, 0.085)], [0.05, 0.03, 0.012]), BARK, "body")
    # the two jagged dark bands across the trunk, in its own frame (angle round it, height)
    def band(lo, hi, amp, n):
        def m(Pt):
            th = np.arctan2(Pt[:, 0], -Pt[:, 1])
            t = th * n / (2 * math.pi)
            top = hi + amp * tri(t)
            bot = lo + amp * tri(t + 0.5)
            near = inside(np.hypot(Pt[:, 0], Pt[:, 1]) - 0.11, 0.004)
            return inside(Pt[:, 2] - top, 0.003) * inside(bot - Pt[:, 2], 0.003) * near
        return m
    k.paint(band(0.415, 0.47, 0.013, 7), BAND, on=["trunk"])
    k.paint(band(0.3, 0.345, 0.012, 7), BAND, on=["trunk"])
    # dark cracks of bark
    for a, z0, z1 in ((0.9, 0.2, 0.29), (-0.7, 0.18, 0.28), (2.4, 0.2, 0.4), (-2.6, 0.22, 0.4), (3.1, 0.36, 0.52)):
        def crack(Pt, a=a, z0=z0, z1=z1):
            th = np.arctan2(Pt[:, 0], -Pt[:, 1])
            d = np.abs((th - a + math.pi) % (2 * math.pi) - math.pi) * 0.08
            return inside(d - 0.003, 0.0025) * (Pt[:, 2] > z0) * (Pt[:, 2] < z1)
        k.paint(crack, BARKDARK, on=["trunk"])


def antlers(k):
    for side, bone in ((1, "armR"), (-1, "armL")):
        m = np.array([side, 1, 1.0])
        main = [np.array(p) * m for p in ((0.05, 0.01, 0.6), (0.085, 0.012, 0.66), (0.115, 0.008, 0.71),
                                          (0.118, 0.0, 0.76), (0.104, -0.006, 0.795))]
        k.add(f"antler{side}", tube(main, [0.034, 0.028, 0.022, 0.017, 0.014]), BARK, "body")
        fork = [np.array(p) * m for p in ((0.112, 0.008, 0.705), (0.15, 0.004, 0.73), (0.18, 0.0, 0.765))]
        k.add(f"tine{side}", tube(fork, [0.02, 0.016, 0.013]), BARK, "body")
        P.leaves(k, main[-1], [(0.5 * side, -0.2, 1)], "body", size=1.4, tag=f"a{side}")
        P.leaves(k, fork[-1], [(0.8 * side, -0.2, 0.7)], "body", size=1.4, tag=f"f{side}")
        P.leaves(k, np.array([0.09, 0.0, 0.672]) * m, [(-0.4 * side, -0.3, 1)], "body", size=1.3, tag=f"i{side}")
    tuft(k, (0, 0.012, 0.655), (0.058, 0.05, 0.03), "body", 13, 0.07, 0.016, 3)


def arms(k):
    for side, bone in ((1, "armR"), (-1, "armL")):
        m = np.array([side, 1, 1.0])
        path = [np.array(p) * m for p in ((0.06, 0.0, 0.485), (0.12, -0.025, 0.47), (0.17, -0.08, 0.435),
                                          (0.19, -0.13, 0.4))]
        k.add(f"arm{side}", tube(path, [0.036, 0.034, 0.033, 0.034]), BARK, bone)
        k.add(f"shoulderband{side}", ring_band(np.array([0.112, -0.022, 0.471]) * m, np.array([0.06, -0.02, -0.012]) * m,
                                               0.033, 0.024, thick=0.007), BAND, bone, fuse=False)
        hand = HAND * m
        k.add(f"palm{side}", ell(hand, (0.064, 0.058, 0.052)), BARK, bone)
        # three great hooked fingers curling down and in from the hand's front
        for i, dx in enumerate((-0.046, 0.0, 0.046)):
            f0 = hand + np.array([dx * side, -0.03, 0.0])
            fl = np.array([dx * side * 0.35, 0, 0])
            pts = [f0, f0 + (0, -0.045, -0.004) + fl * 0.5, f0 + (0, -0.075, -0.045) + fl, f0 + (0, -0.066, -0.098) + fl,
                   f0 + (0, -0.03, -0.118) + fl * 0.8]
            r0 = 0.027 if i != 1 else 0.03
            k.add(f"finger{side}{i}", tube(pts, [r0, r0 * 0.95, r0 * 0.8, r0 * 0.58, r0 * 0.38]), BARK, bone)
        # the big clump of spiky leaves over the back of the hand
        tuft(k, hand + np.array([0.0, 0.03, 0.05]), (0.095, 0.085, 0.055), bone, 24, 0.085, 0.02, 11 + side,
             up=(0.4 * side, 0.3, 1))


def roots(k):
    for side, bone in ((1, "legR"), (-1, "legL")):
        for j, a in enumerate((55, 0, -50)):
            ar = math.radians(a)
            d = np.array([math.cos(ar) * side, -math.sin(ar), 0])
            rr = (0.035, 0.11, 0.19, 0.25, 0.275)
            zz = (0.245, 0.205, 0.15, 0.065, 0.02)
            pts = [d * r + (0, 0, z) for r, z in zip(rr, zz)]
            k.add(f"root{side}{j}", tube(pts, [0.042, 0.038, 0.031, 0.022, 0.016]), BARK, bone)


def face(k):
    # the dark hollow under the brow: a lens, flat on top
    lens = polygon([(-1, 0.25), (-0.55, 0.75), (0.55, 0.75), (1, 0.25), (0.6, -0.55), (0, -0.85), (-0.6, -0.55)])
    k.decal(EYE + (0, 0, 0.004), (0, -1, -0.05), (0.06, 0.042), SOCKET, eps=0.003, outline=lens, rings=16, segs=96)


def red_eye(u, v):
    r = np.hypot(u, v)
    c = np.tile(srgb(SOCKET), (len(u), 1))
    c[r < 0.92] = srgb(EYERED)
    c[r < 0.62] = srgb(EYEPINK)
    c[((u - 0.22) / 0.26) ** 2 + ((v - 0.28) / 0.22) ** 2 < 1] = srgb(EYELIGHT)
    return c


def eyes(k, state):
    k.decal(EYE, (0, -1, -0.05), (0.022, 0.022), eye_look(state, red_eye, srgb(SOCKET), srgb(EYERED), line=0.16),
            eps=0.0055, rings=20, segs=56)


def trevenant(src, out_path):
    k = Kit(src, coat="fur")
    trunk(k)
    antlers(k)
    arms(k)
    roots(k)
    face(k)
    k.build(voxel=0.0072, smooth=3, band=0.008, wband=0.05)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out_path)


if __name__ == "__main__":
    trevenant(*args())
