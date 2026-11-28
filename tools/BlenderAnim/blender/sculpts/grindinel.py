# Grindinel (No. 10112, Carpathia) sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py):
#   blender --background --factory-startup --python sculpts/grindinel.py -- <the export's Grindinel.glb> <out.glb>
#
# The design, from its picture (10112-Grindinel.jpg) and its brief (CP-dex-2b, "112. Grindinel: a hailstorm
# cluster"): a cluster of hailstones. In its middle a big smooth round stone, white with a blue shade under it, with
# two large eyes: dark navy sockets each holding a glowing cyan ring round a pale yellow heart. Round it, packed in a
# ring that hides the dark between them, nine rough lumpy stones of white ice shaded blue below (the biggest at the
# top left and on the right), a few small round ones between; little icicles hang under the lower stones. Crackling
# blue sparks (and two yellow ones) run in the dark gaps between the stones, binding them. Smaller chunks of ice
# orbit it in a loose tilted ring, higher on its right than its left (the brief's "like a loose ring"; the older
# sculpt's glowing hoop is not in the picture and is left out, the chunks alone ride the ring bone). The picture's
# loose flecks and frost specks are left out; the back is the same stones carried round.
#
# The skeleton: body (0, 0, 0.391), ring (0, -0.03, 0.391).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, inside, unit, eye_look, facets, args, hull, slab, axes

ICE, ICE_TOP, ICE_SHADE = "#D9E5F1", "#EEF4FA", "#A9C0DA"
BALL, BALL_SHADE = "#EAF2FA", "#BCD0E6"
GAP = "#243C5C"
SPARK, SPARK_YELLOW = "#3ECFFF", "#FFE24A"
ICICLE = "#BFE4F6"
NAVY, CYAN, HEART = srgb("#16223A"), srgb("#4EE0FF"), srgb("#F8F6B0")
INK = srgb("#1E2C44")

C = np.array([0.0, -0.045, 0.4])          # the middle stone, forward of the cluster
R = np.array([0.118, 0.105, 0.116])
CORE = np.array([0.0, 0.03, 0.4])         # the dark between the stones
CORE_R = np.array([0.17, 0.13, 0.19])
EYE_D = (0.42, -0.9, 0.02)
EYE_HALF = (0.034, 0.045)
VOXEL = 0.0055

# the ring of stones round it, in the front plane (x, z), and those behind: centre, radius, seed
STONES = [
    ((-0.09, 0.01, 0.585), 0.085, 1), ((0.135, 0.015, 0.53), 0.078, 2), ((0.175, 0.02, 0.33), 0.09, 3),
    ((0.045, 0.0, 0.24), 0.058, 4), ((-0.03, 0.01, 0.2), 0.056, 5), ((-0.12, 0.01, 0.255), 0.072, 6),
    ((-0.19, 0.02, 0.385), 0.068, 7), ((-0.165, 0.03, 0.5), 0.05, 8), ((0.2, 0.04, 0.43), 0.045, 9),
    ((0.0, 0.11, 0.53), 0.09, 10), ((0.0, 0.12, 0.3), 0.09, 11), ((0.12, 0.1, 0.42), 0.085, 12),
    ((-0.12, 0.1, 0.42), 0.085, 13), ((0.07, 0.13, 0.62), 0.06, 14), ((-0.07, 0.11, 0.2), 0.06, 15),
]
PEBBLES = [((0.04, -0.02, 0.585), 0.04), ((0.035, -0.06, 0.545), 0.026), ((0.2, -0.0, 0.25), 0.03),
           ((-0.2, -0.0, 0.47), 0.03)]
ICICLES = [((0.045, -0.02, 0.188), 0.04), ((-0.03, -0.01, 0.148), 0.035), ((-0.13, -0.01, 0.19), 0.04),
           ((0.18, 0.0, 0.245), 0.04), ((-0.05, 0.1, 0.145), 0.03)]
# sparks running in the gaps: a point on the dark core in (x, z), length, direction, colour
SPARKS = [((-0.125, 0.47), 0.09, (0.25, -1.0), SPARK), ((-0.13, 0.32), 0.07, (-0.3, -1.0), SPARK),
          ((0.125, 0.45), 0.1, (-0.15, -1.0), SPARK), ((0.11, 0.3), 0.07, (0.6, -0.8), SPARK_YELLOW),
          ((0.0, 0.535), 0.09, (1.0, 0.25), SPARK), ((-0.02, 0.27), 0.08, (1.0, -0.2), SPARK_YELLOW),
          ((0.06, 0.6), 0.06, (1.0, -0.6), SPARK)]


def on_ell(c, r, d):
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def stone(k, name, c, r, seed, bone="body"):
    """A rough hailstone: a lumpy faceted ball, white on top and shaded blue below."""
    k.add(name, ell(c, (r, r * 0.92, r * 0.95), rot=(seed * 23, seed * 41, 0),
                    deform=facets(14, 0.6, seed, depth=(0.8, 0.97))), ICE, bone)
    c = np.array(c, float)
    k.paint(lambda P, c=c, r=r: inside(P[:, 2] - (c[2] - 0.25 * r), 0.012), ICE_SHADE, on=[name])
    k.paint(lambda P, c=c, r=r: inside((c[2] + 0.45 * r) - P[:, 2], 0.012), ICE_TOP, on=[name])


def chunk(seed, c, size):
    """A small chunk of ice: the hull of a few points on a squashed ball, flat-faced."""
    rs = np.random.RandomState(seed)
    d = rs.normal(size=(11, 3)); d /= np.linalg.norm(d, axis=1, keepdims=True)
    return hull(np.array(c, float) + d * size * np.array([1.0, 0.8, 0.85]) * rs.uniform(0.75, 1.0, (11, 1)))


def zigzag(at, n, along, length, width, kinks=4):
    """A spark as a zigzag plate in the tangent plane at a point: its outline from a polyline offset either side."""
    n = unit(*n)
    t = np.array(along, float); t = t - n * (t @ n); t /= np.linalg.norm(t)
    s = np.cross(n, t)
    pts = [(i / kinks * length - length / 2, (0.012 if i % 2 else -0.012) * (0 < i < kinks)) for i in range(kinks + 1)]
    left = [(a, b + width / 2) for a, b in pts]; right = [(a, b - width / 2) for a, b in pts]
    return slab(left + right[::-1], np.array(at, float) + n * 0.004, t, s, 0.012)


def socket_eye(u, v):
    r = np.sqrt(u * u + v * v)
    c = np.tile(NAVY, (len(u), 1))
    c[r < 0.7] = CYAN
    c[r < 0.38] = HEART
    return c


def grindinel(src, out):
    k = Kit(src, coat="shell")
    k.add("gap", ell(CORE, CORE_R), GAP, "body")
    for i, (c, r, seed) in enumerate(STONES):
        stone(k, f"stone{i}", c, r, seed)
    for i, (c, r) in enumerate(PEBBLES):
        k.add(f"pebble{i}", ell(c, (r, r, r), sub=3), ICE_TOP, "body")
    k.add("ball", ell(C, R), BALL, "body")
    k.paint(lambda P: inside(P[:, 2] - (C[2] - 0.55 * R[2]), 0.02), BALL_SHADE, on=["ball"])
    for i, (c, length) in enumerate(ICICLES):
        c = np.array(c, float)
        pts = [c + (0.011, 0, 0.004), c + (-0.011, 0, 0.004), c + (0, 0.011, 0.004), c + (0, -0.011, 0.004),
               c + (0.003, 0, -length)]
        k.add(f"icicle{i}", hull(pts), ICICLE, "body", fuse=False)
    for i, ((x, z), length, d, col) in enumerate(SPARKS):
        y = CORE[1] - CORE_R[1] * math.sqrt(max(0.05, 1 - (x / CORE_R[0]) ** 2 - ((z - CORE[2]) / CORE_R[2]) ** 2))
        p, nn = on_ell(CORE, CORE_R, (x, y - CORE[1], z - CORE[2]))
        k.add(f"spark{i}", zigzag(p, nn, (d[0], 0, d[1]), length, 0.016), col, "body", fuse=False, glow=col)
    # the chunks orbiting it on a loose tilted ring, higher on its right
    for i, a in enumerate(np.linspace(0, 2 * math.pi, 8, endpoint=False) + 0.35):
        c = (0.3 * math.cos(a), -0.03 + 0.2 * math.sin(a), 0.38 + 0.075 * math.cos(a) + 0.015 * math.sin(3 * a) - 0.15 * max(0.0, -math.sin(a)))
        k.add(f"orbit{i}", chunk(20 + i, c, 0.033 + 0.014 * ((i * 5) % 3) / 2), ICE, "ring", fuse=False)
    k.build(voxel=VOXEL, smooth=3, band=0.006, wband=0.05)
    V = np.array([v.co[:] for v in k.ob.data.vertices])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    print("KIT: board: shows %.3f in a square of %.3f" % (np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l))))

    def eyes(state):
        for side in (1, -1):
            p, nrm = on_ell(C, R, (EYE_D[0] * side, EYE_D[1], EYE_D[2]))
            f = eye_look(state, socket_eye, srgb(BALL), INK, line=0.16)
            col = f if side > 0 else (lambda u, v, f=f: f(-u, v))
            k.decal(p, nrm, EYE_HALF, col, eps=0.003, rings=20, segs=56)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    grindinel(*args())
