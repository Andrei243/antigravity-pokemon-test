# Diglett sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/diglett.py -- <the export's Diglett.glb> <out.glb>
#
# Written to be grown from (Dugtrio is three of these: sibling("diglett")). From the references: a brown peg with a
# round top, a little taller than wide, standing straight up out of a low ring of grey broken earth with loose pale
# stones on it, the ring about as wide as the whole is tall; two small black upright oval eyes set close together
# high on the face, each with a white glint at its top; a big pink oval nose, wider than tall, bulging forward a
# third of the way down, with a soft light spot on its upper side. No mouth, no limbs.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, srgb, unit, eye_look, args

BROWN, NOSE, SHINE = "#A9785C", "#E39AC2", "#EEB8D6"
EARTH, EARTH_DARK, STONE = "#8C8C92", "#74747A", "#B6B6BC"
INK = "#17110F"

# A Diglett's frame, as shares of its radius R (the peg's half width)
DEPTH = 0.94                        # front to back, a share of the width
EYE_X, EYE_Z = 0.29, 0.21           # the eyes' place: out from the middle, and above the dome's middle
EYE_HALF = (0.088, 0.18)            # an eye's half width and half height
NOSE_Z = -0.30                      # the nose's middle, under the dome's middle
NOSE_R = (0.40, 0.24, 0.235)        # its half sizes: across, forward, up


def peg(L):
    """A deform of the unit sphere: the upper half stays a dome, the lower half becomes an upright wall L deep with a
    flat foot (the peg's bottom, hidden in the earth)."""
    def f(V):
        V = V.copy()
        t = np.clip(-V[:, 2], 0, 1)                 # how far down the lower half a point is
        rxy = np.sqrt(V[:, 0] ** 2 + V[:, 1] ** 2)
        low = V[:, 2] < 0
        side = t < 0.8
        r = np.where(side, 1.0, rxy / 0.6) * (1 + 0.035 * t)
        z = np.where(side, -L * t / 0.8, -L)
        s = np.where(rxy > 1e-6, r / np.maximum(rxy, 1e-6), 0.0)
        V[low, 0] *= s[low]; V[low, 1] *= s[low]
        V[low, 2] = z[low]
        return V
    return f


def turn(off, yaw):
    """An offset (x, y, z) turned about the upright axis by yaw degrees (the front, -Y, toward +X for yaw > 0)."""
    a = math.radians(yaw)
    x, y, z = off
    return np.array([x * math.cos(a) - y * math.sin(a), x * math.sin(a) + y * math.cos(a), z])


def diglett(k, tag, foot, R, top, bone, yaw=0.0):
    """
    One Diglett on bone: its foot (x, y) on the ground, its half width R, its dome's top at height top, turned yaw
    degrees. Adds its peg, nose and the nose's shine; gives back where its eyes go, for eyes().
    """
    zc = top - R                                    # the dome's middle
    L = (zc - 0.03) / R                             # the wall ends in the earth, short of the ground
    base = np.array([foot[0], foot[1], 0.0])
    C = base + np.array([0, 0, zc])
    k.add(f"peg{tag}", ell(C, (R, R * DEPTH, R), rot=(0, 0, yaw), deform=peg(L)), BROWN, bone)
    # The nose: a pink oval bulging forward, fused so that its shine lies on it
    nz = zc + NOSE_Z * R
    N = base + turn((0, -R * DEPTH * 1.0, nz), yaw)
    k.add(f"nose{tag}", ell(N, (NOSE_R[0] * R, NOSE_R[1] * R, NOSE_R[2] * R), rot=(0, 0, yaw)), NOSE, bone,
          surface="shell")
    fwd = turn((0, -1, 0), yaw)
    sc = base + turn((-0.12 * R, -R * DEPTH - NOSE_R[1] * R * 0.9, nz + 0.08 * R), yaw)
    k.decal(sc, unit(*(fwd + np.array([0, 0, 0.55]))), (0.13 * R, 0.07 * R), SHINE, eps=0.003, rings=8, segs=32,
            up=(0, 0, 1))
    spots = []
    for side in (1, -1):
        d = np.array([EYE_X * side, -math.sqrt(1 - EYE_X ** 2 - EYE_Z ** 2), EYE_Z])
        at = base + turn((d[0] * R, d[1] * R * DEPTH, zc + d[2] * R), yaw)
        spots.append((at, turn((d[0], d[1], d[2] * 0.6), yaw), side))
    return dict(spots=spots, half=(EYE_HALF[0] * R, EYE_HALF[1] * R))


def burrow(k, rx, ry, count, seed, centre=(0.0, 0.0)):
    """The ring of broken earth: a low dark mound, lumps of earth round its edge, and loose pale stones on it."""
    rs = np.random.RandomState(seed)
    cx, cy = centre
    def flat(V):
        V = V.copy(); V[:, 2] = np.maximum(V[:, 2], -0.45); return V
    k.add("mound", ell((cx, cy, 0.03), (rx * 0.92, ry * 0.92, 0.075), deform=flat), EARTH_DARK, "root",
          surface="shell")
    for i in range(count):
        a = 2 * math.pi * (i + rs.uniform(-0.2, 0.2)) / count
        reach = rs.uniform(0.78, 0.92)
        s = rs.uniform(0.05, 0.068)
        at = (cx + math.cos(a) * rx * reach, cy + math.sin(a) * ry * reach, rs.uniform(0.045, 0.065))
        tone = EARTH if i % 3 else EARTH_DARK
        k.add(f"lump{i}", ell(at, (s * 1.3, s * 0.95, s * 0.85), rot=(rs.uniform(-15, 15), 0, math.degrees(a) + 90),
                              deform=flat), tone, "root", surface="shell")
    # Loose stones on the mound: chunky, flat-faced, pale, half sunk in the lumps
    n = count * 2 // 3
    for i in range(n):
        a = 2 * math.pi * (i + 0.5 + rs.uniform(-0.25, 0.25)) / n
        reach = rs.uniform(0.86, 1.0)
        s = rs.uniform(0.026, 0.036)
        lift = 0.1 - 0.12 * (reach - 0.86)
        c = np.array([cx + math.cos(a) * rx * reach, cy + math.sin(a) * ry * reach, lift + rs.uniform(0.0, 0.015)])
        d = rs.normal(size=(10, 3)); d /= np.linalg.norm(d, axis=1, keepdims=True)
        pts = d * rs.uniform(0.8, 1.1, size=(10, 1)) * np.array([s * 1.35, s, s * 0.7])
        pts = pts @ np.array([[math.cos(a), -math.sin(a), 0], [math.sin(a), math.cos(a), 0], [0, 0, 1]]).T
        k.add(f"stone{i}", hull(c + pts), STONE, "root", fuse=False, surface="shell")


WHITE = np.array([1.0, 1.0, 1.0])


def eye(u, v):
    """A black upright oval with a white glint on its top (u outward, v up)."""
    c = np.tile(srgb(INK), (len(u), 1))
    c[((u - 0.05) / 0.44) ** 2 + ((v - 0.44) / 0.27) ** 2 < 1] = WHITE
    return c


def eyes(k, specs, state):
    """The eyes of every Diglett in specs, in one state."""
    look = eye_look(state, eye, srgb(BROWN), srgb(INK), line=0.17)
    for sp in specs:
        for at, n, side in sp["spots"]:
            col = look if side > 0 else (lambda u, v, look=look: look(-u, v))
            k.decal(at, unit(*n), sp["half"], col, eps=0.005, rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")
    burrow(k, 0.215, 0.2, 13, 81)
    spec = diglett(k, "", (0.0, 0.0), 0.165, 0.5, "body")
    k.build(voxel=0.006, smooth=3, band=0.004)
    k.eye_states(lambda s: eyes(k, [spec], s))
    k.export(out)
