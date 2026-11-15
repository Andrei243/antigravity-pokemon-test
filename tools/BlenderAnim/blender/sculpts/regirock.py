# Regirock sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/regirock.py -- <the export's Regirock.glb> <out.glb>
#
# The design, from the references: a golem of pale grey stones (warm grey, lavender in the shade), its trunk a cracked
# mass of flat-faced panels parted by dark seams, two of them orange (one on the chest, one low on the other side).
# No head of its own: the top of the trunk rises into a rounded block, and on its front the face, seven pale orange dots
# in an H. On each shoulder a great orange boulder standing higher than the head. Long arms of stacked flat stones
# hanging beside the trunk (carried a little forward here) to huge boulder fists level with the knees; short legs of
# two stones on flat wide feet. The eyes' five states are laid on the seven dots: open they shine, shut they dim with a
# dark line across (regigigas.py's gems).
# The brief expected brown and orange boulders: the pictures' stones are pale grey, the boulders alone orange.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, srgb, unit, boxy, eye_look, args, hull

# --- colours ----------------------------------------------------------------------------------------------------------
PALE, LAV, CORE = "#D4CCCA", "#BDB0BC", "#5E5262"
ORANGE, ORANGEDARK = "#E27A3A", "#C9652E"
DOT, DOTRIM = "#F8BC8C", "#B4683A"

# --- the frame --------------------------------------------------------------------------------------------------------
TRUNK_C, TRUNK_R, TAPER = np.array([0.0, 0.02, 0.5]), np.array([0.22, 0.165, 0.26]), 0.24

FACE_Y = -0.1265                    # the face plate's front, where the dots are cast
HEAD_B, HEAD_TOPFACE = 0.135, 0.88    # the head block's half depth, the top of its flat front
DOT_X, DOT_Z, DOT_HALF = 0.052, (0.848, 0.803, 0.758), 0.021
H_DOTS = [(-1, 0), (1, 0), (-1, 1), (0, 1), (1, 1), (-1, 2), (1, 2)]     # (column, row from the top): an H
GAP = 0.032                         # a seam's half width between panels, on the unit sphere


def shade(h, f):
    """A colour a little lighter or darker (f above or below 1), still written as on screen."""
    c = np.clip(np.array([int(h[i:i + 2], 16) for i in (1, 3, 5)]) * f, 0, 255).astype(int)
    return "#%02X%02X%02X" % tuple(c)


def taper(V):
    """The unit sphere squared up a little (its points keep their distance from the middle) and widened upward."""
    V = boxy(2.5)(V) * np.linalg.norm(V, axis=1, keepdims=True)
    V = V.copy()
    s = 1 + TAPER * V[:, 2]
    V[:, 0] *= s; V[:, 1] *= s
    return V


def on_trunk(p):
    """A point of the unit sphere's space taken onto the trunk (an egg wider at the shoulders)."""
    return TRUNK_C + taper(np.atleast_2d(p)) * TRUNK_R


def stone(c, half, rot=(0, 0, 0), seed=0, cham=0.38, jit=0.1):
    """A stone with flat faces: a box with its edges and corners cut back by chance, made a hull."""
    rs = np.random.RandomState(seed)
    pts = []
    for x in (-1, 0, 1):
        for y in (-1, 0, 1):
            for z in (-1, 0, 1):
                n = abs(x) + abs(y) + abs(z)
                if n == 0:
                    continue
                v = np.array([x, y, z], float) * (1 - cham * (n - 1) / 2)
                pts.append(v * (1 + jit * rs.randn(3)))
    P = np.array(pts) * np.array(half)
    if any(rot):
        m = np.array(Euler([math.radians(a) for a in rot]).to_matrix())
        P = P @ m.T
    return hull(P + np.array(c))


# --- the trunk: a dark core under flat panels, each a cell of a Voronoi pattern on the sphere ----------------------------
def panels(k):
    k.add("trunk", ell(TRUNK_C, TRUNK_R * 0.985, deform=taper), CORE, "body")
    N = 30
    i = np.arange(N) + 0.5
    ph = np.arccos(1 - 2 * i / N); th = math.pi * (1 + 5 ** 0.5) * i
    P = np.stack([np.cos(th) * np.sin(ph), np.sin(th) * np.sin(ph), np.cos(ph)], 1)
    rs = np.random.RandomState(377)
    P += rs.normal(size=P.shape) * 0.07
    P /= np.linalg.norm(P, axis=1, keepdims=True)
    # two orange panels: on the chest to the right of the middle, and low on the left side
    orange = {int(np.argmax(P @ unit(0.45, -0.85, 0.2))), int(np.argmax(P @ unit(-0.55, -0.7, -0.45)))}
    for n_, c in enumerate(P):
        if c[2] > 0.82 or c[2] < -0.9:
            continue                    # the top is the head's, the underside nobody sees
        t1 = np.cross(c, (0, 0, 1.0))
        if np.linalg.norm(t1) < 1e-3:
            t1 = np.cross(c, (1.0, 0, 0))
        t1 /= np.linalg.norm(t1); t2 = np.cross(c, t1)
        poly = [np.array(p) * 1.2 for p in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
        for m in P:
            if np.allclose(m, c):
                continue
            d = m - c
            a, b = t1 @ d, t2 @ d
            lim = (1 - c @ m) - GAP * math.hypot(a, b)
            new = []
            for j in range(len(poly)):
                p, q = poly[j], poly[(j + 1) % len(poly)]
                fp, fq = a * p[0] + b * p[1] - lim, a * q[0] + b * q[1] - lim
                if fp <= 0:
                    new.append(p)
                if fp * fq < 0:
                    new.append(p + (q - p) * (fp / (fp - fq)))
            poly = new
            if len(poly) < 3:
                break
        if len(poly) < 3:
            continue
        top = [c * 1.075 + t1 * x * 0.86 + t2 * y * 0.86 for x, y in poly]
        mid = [c * 1.04 + t1 * x + t2 * y for x, y in poly]
        low = [(c + t1 * x + t2 * y) / np.linalg.norm(c + t1 * x + t2 * y) * 0.9 for x, y in poly]
        W = on_trunk(np.vstack([top, mid, low]))
        if n_ in orange:
            col = shade(ORANGE, 0.95 + 0.1 * rs.rand())
        else:
            col = shade(LAV if rs.rand() < 0.25 else PALE, 0.93 + 0.1 * rs.rand())
        k.add(f"panel{n_}", hull(W), col, "body", fuse=False)


def octagon(a, b, c, z, y0=0.0):
    return [(sx * a, y0 + sy * (b - c), z) for sx in (-1, 1) for sy in (-1, 1)] + \
           [(sx * (a - c), y0 + sy * b, z) for sx in (-1, 1) for sy in (-1, 1)]


def head(k):
    """
    The trunk's top risen into a faceted block, its front one flat face from the shoulders to near its rounded top;
    the face is a thin fused plate just proud of that face (the dots' patches need a fused surface).
    """
    pts = (octagon(0.15, HEAD_B, 0.05, 0.62, 0.01) + octagon(0.148, HEAD_B, 0.05, HEAD_TOPFACE, 0.01)
           + octagon(0.118, 0.1, 0.045, 0.915, 0.01) + octagon(0.06, 0.05, 0.02, 0.95, 0.01))
    k.add("head", hull(pts), PALE, "head", fuse=False)
    def flat(V):
        return boxy(3.0)(V)
    k.add("face", ell((0, FACE_Y + 0.0125, 0.8), (0.094, 0.012, 0.088), deform=flat), PALE, "head")


def shoulders(k):
    """A great orange boulder on each shoulder, taller than the head, leaning a little in at its top."""
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"boulder{side}", stone((0.27 * s, 0.03, 0.79), (0.12, 0.13, 0.18), rot=(4, 8 * s, 6 * s), seed=11 + (s > 0)),
              ORANGE, f"arm{side}", fuse=False)
        k.add(f"boulderb{side}", stone((0.31 * s, 0.08, 0.68), (0.08, 0.09, 0.08), rot=(0, 0, 20 * s), seed=21 + (s > 0)),
              ORANGEDARK, f"arm{side}", fuse=False)


def arms(k):
    """Stacked flat stones hanging from under the boulder to a huge boulder fist, carried a little forward."""
    path = [((0.3, -0.0, 0.58), (0.08, 0.08, 0.055)),
            ((0.305, -0.035, 0.485), (0.085, 0.08, 0.05)),
            ((0.31, -0.065, 0.39), (0.08, 0.08, 0.05)),
            ((0.315, -0.09, 0.295), (0.085, 0.085, 0.05))]
    for s, side in ((1, "R"), (-1, "L")):
        for j, (c, h) in enumerate(path):
            col = shade(PALE if j % 2 == 0 else LAV, 0.96 + 0.04 * j)
            k.add(f"armstone{j}{side}", stone((c[0] * s, c[1], c[2]), h, rot=(0, 0, 15 * j * s), seed=30 + 4 * j + (s > 0)),
                  col, f"arm{side}", fuse=False)
        k.add(f"fist{side}", stone((0.32 * s, -0.115, 0.165), (0.115, 0.115, 0.1), rot=(-8, 6 * s, 20 * s), seed=50 + (s > 0),
                                   cham=0.42), PALE, f"arm{side}", fuse=False)


def legs(k):
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"hipstone{side}", stone((0.115 * s, 0.01, 0.255), (0.09, 0.085, 0.07), rot=(0, 0, 10 * s), seed=60 + (s > 0)),
              LAV, f"leg{side}", fuse=False)
        k.add(f"shin{side}", stone((0.125 * s, -0.01, 0.15), (0.08, 0.08, 0.06), rot=(0, 0, -12 * s), seed=62 + (s > 0)),
              PALE, f"leg{side}", fuse=False)
        k.add(f"foot{side}", stone((0.13 * s, -0.04, 0.045), (0.1, 0.125, 0.045), rot=(0, 0, 8 * s), seed=64 + (s > 0),
                                   cham=0.5), PALE, f"leg{side}", fuse=False)
    # a stone between the legs, under the trunk
    k.add("crotch", stone((0, 0.02, 0.27), (0.09, 0.08, 0.06), seed=70), PALE, "body", fuse=False)


# --- the dots, which are the eyes -------------------------------------------------------------------------------------
def dot_look():
    c0, rim, hi = srgb(DOT), srgb(DOTRIM), np.array([1.0, 0.97, 0.92])
    def look(u, v):
        c = np.tile(c0, (len(u), 1))
        c[u * u + v * v > 0.8 ** 2] = rim
        c[(u + 0.25) ** 2 + (v - 0.3) ** 2 < 0.24 ** 2] = hi
        return c
    return look


def dots(k, state):
    c0 = srgb(DOT)
    look = eye_look(state, dot_look(), c0 * 0.5, c0 * 0.15, line=0.2)
    for col, row in H_DOTS:
        x, z = col * DOT_X, DOT_Z[row]
        n = (0, -1.0, 0)
        k.decal((x, FACE_Y, z), n, (DOT_HALF, DOT_HALF), look, eps=0.003, rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    panels(k)
    head(k)
    shoulders(k)
    arms(k)
    legs(k)
    k.build(voxel=0.007, band=0.004)
    k.eye_states(lambda state: dots(k, state))
    k.export(out)
