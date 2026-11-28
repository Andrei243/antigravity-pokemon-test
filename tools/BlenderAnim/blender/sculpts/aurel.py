# Aurel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/aurel.py -- <the export's Aurel.glb> <out.glb>
#
# From its picture (docs/art/reference/gemini/carpathia/10108-Aurel.jpg) and its brief ("108. Aurel"): a lumpy gold
# nugget like an egg leaning a little to its right (+X), shiny, its lumps lit pale gold and its creases a darker gold;
# a shy face, two big dark eyes with brown irises and white glints, each under a creased socket whose brow rises to
# the nose (a worried look); two little lumps of hands held together under the eyes, as if at its chin; two stubby
# feet. A few drops of stream water cling to it. The pebble it peeks out from behind is scenery and is left out.
#
# Written to be grown from: aurifer.py imports it (sibling("aurel")) for the line's gold.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, inside, unit, eye_look, polygon, args, lumpy, facets

# --- the line's colours -------------------------------------------------------------------------------------------
GOLD, GOLD_LT, GOLD_DK = "#DBAE3C", "#F4D768", "#A87E22"   # the nugget, its lit lumps, its creases
SOCKET = "#B98E2C"                                          # the creased socket round each eye
INK, IRIS, GLEAM = "#1C140E", "#6E4C24", "#FFFFFF"
DROP, DROP_LT = "#C8DCE6", "#F4FAFC"                        # the stream's drops

# --- the frame ----------------------------------------------------------------------------------------------------
C = np.array([0.0, 0.0, 0.192])                 # the nugget's centre, over the body's joint (0, 0, 0.206)
R = np.array([0.144, 0.124, 0.17])
TILT = (0, 9, 0)                                # leaning a little to its right
EYE_X, EYE_Z = 0.05, 0.218
EYE = (0.031, 0.034)
VOXEL = 0.0036


def nugget_shape(V):
    """The unit sphere made a nugget: soft lumps, a few broad facets, a flatter underside."""
    V = V * (1 + 0.055 * np.sin(3.1 * V[:, 0] + 1.7 * V[:, 2] + 0.4) * np.sin(2.6 * V[:, 1] - 2.2 * V[:, 2] + 1.1)
             + 0.035 * np.sin(4.3 * V[:, 2] + 2.0 * V[:, 0]))[:, None]
    V = facets(10, 0.35, 108, keep_front=0.5, depth=(0.9, 1.0))(V)
    V[:, 2] = np.maximum(V[:, 2], -0.86)
    return V


def lumps(P):
    """A field over the surface: high on the lumps, low in the creases."""
    x, y, z = P[:, 0] * 42, P[:, 1] * 42, P[:, 2] * 42
    return np.sin(x * 0.9 + z * 0.5) + np.sin(y * 1.1 - z * 0.7 + 1.3) + np.sin(z * 1.3 + x * 0.4 + 2.1)


def body(k):
    k.add("nugget", ell(C, R, rot=TILT, deform=nugget_shape, sub=5), GOLD, "body", surface="metal")
    # the face's front a little flatter, so the eyes lie on it
    k.add("face", ell(C + np.array([0, -0.06, 0.012]), (0.12, 0.07, 0.11)), GOLD, "body", surface="metal")
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"hand{side}", ell((0.05 * s, -0.114, 0.124), (0.036, 0.026, 0.024), rot=(0, 0, -12 * s),
                                 deform=lumpy(3, 2, 0.1)), GOLD, f"arm{side}", surface="metal")
        k.add(f"foot{side}", ell((0.062 * s, -0.024, 0.03), (0.03, 0.036, 0.031), deform=lumpy(3, 2, 0.08)),
              GOLD, f"leg{side}", surface="metal")


def shine(k, on=("nugget", "face", "hand", "foot")):
    """The lumps lit pale gold, the creases darker."""
    k.paint(lambda P: inside(1.0 - lumps(P), 0.25) * inside(-(P[:, 1] + 0.11), 0.01), GOLD_LT, on=list(on))
    k.paint(lambda P: inside(lumps(P) + 1.75, 0.15) * 0.75, GOLD_DK, on=list(on))
    # the face round the eyes kept plain
    k.paint(lambda P: inside(np.hypot(P[:, 0] / 0.1, (P[:, 2] - EYE_Z) / 0.06) - 1, 0.02) * (P[:, 1] < -0.08),
            GOLD, on=list(on))


def front(x, z):
    """Where a ray from the front meets the face part (near enough for a patch's centre)."""
    c, r = C + np.array([0, -0.06, 0.012]), np.array([0.12, 0.07, 0.11])
    q = 1 - (x / r[0]) ** 2 - ((z - c[2]) / r[2]) ** 2
    return np.array([x, c[1] - r[1] * math.sqrt(max(q, 0.0)), z])


# the socket's crease over the eye, u outward: a brow rising from the outer side to the nose
SOCKET_OUT = [(-1.0, 0.25), (-0.7, 0.95), (-0.1, 0.85), (0.5, 0.6), (1.0, 0.25), (1.0, -0.15), (0.0, -0.05),
              (-1.0, 0.0)]


def sockets(k):
    m = np.array([-1, 1, 1.0])
    for s in (1, -1):
        at = front(EYE_X * s, EYE_Z + 0.008)
        nrm = unit(0.3 * s, -1, 0.1)
        out = SOCKET_OUT if s > 0 else [(-a, b) for a, b in SOCKET_OUT]
        k.decal(at, nrm, (EYE[0] * 1.35, EYE[1] * 1.35), SOCKET, eps=0.0016, outline=polygon(out), rings=8, segs=48)


def drops(k):
    """A few drops of the stream, each a pale teardrop with a glint."""
    def look(u, v):
        c = np.tile(srgb(DROP), (len(u), 1))
        c[np.hypot(u + 0.25, v - 0.1) < 0.35] = srgb(DROP_LT)
        return c
    tear = polygon([(0.0, 1.0), (0.45, 0.1), (0.6, -0.4), (0.3, -0.85), (0.0, -0.95), (-0.3, -0.85), (-0.6, -0.4),
                    (-0.45, 0.1)])
    for n in ((0.75, -0.55, 0.45), (-0.85, -0.45, -0.05), (0.8, -0.45, -0.3), (-0.25, -0.5, 0.75),
              (0.5, 0.75, -0.2), (-0.6, 0.6, 0.35)):
        n = unit(*n)
        k.decal(C + n * R, n, (0.01, 0.015), look, eps=0.0016, outline=tear, rings=6, segs=24)


def eye_open(u, v):
    """A big dark eye: a brown iris low in it, a large white glint up toward the nose and a small one below."""
    c = np.tile(srgb(INK), (len(u), 1))
    c[(np.hypot(u, v + 0.05) < 0.78) & (v < -0.05)] = srgb(IRIS)
    c[np.hypot(u, v + 0.05) < 0.4] = srgb(INK)
    c[np.hypot(u + 0.28, v - 0.32) < 0.26] = srgb(GLEAM)
    c[np.hypot(u - 0.32, v + 0.42) < 0.11] = srgb(GLEAM)
    return c


def eyes(k, lid=GOLD):
    LID, LINE = srgb(lid), srgb(GOLD_DK)
    m = np.array([-1, 1, 1.0])
    at, nrm = front(EYE_X, EYE_Z), unit(0.3, -1, 0.05)
    def lay(state):
        look = eye_look(state, eye_open, LID, srgb(INK), line=0.18)
        k.decal(at, nrm, EYE, look, eps=0.0028, rings=20, segs=56)
        k.decal(at * m, nrm * m, EYE, lambda u, v, look=look: look(-u, v), eps=0.0028, rings=20, segs=56)
    return lay


def board(ob):
    V = np.array([v.co[:] for v in ob.data.vertices])
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    print("KIT: board: corners %.3f (0.853 w + 0.522 l), shows %.3f, in a square of %.3f"
          % (0.853 * w + 0.522 * l, np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l))))


def aurel(src, out_path):
    k = Kit(src, coat="metal")
    body(k)
    shine(k)
    sockets(k)
    drops(k)
    ob = k.build(voxel=VOXEL, smooth=3, band=0.006)
    board(ob)
    k.eye_states(eyes(k))
    k.export(out_path)


if __name__ == "__main__":
    aurel(*args())
