# Milotic sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/milotic.py -- <the export's Milotic.glb> <out.glb>
# Feebas grown beautiful: a long cream sea serpent coiled on the ground, its neck rising from the front of the coil to a
# small pointed head with a tall cream crest, red eyes, two thin red feelers curling up and out over it like a heart,
# and long pink fins falling from behind the eyes like hair, ragged at their ends. From the back of the coil to the tail
# it is blue, netted in black like scales, pink scales under the tail, which rises to a fan of four blue fins with pink
# hearts. The body is laid as giratinaorigin.py's: one smooth line through the chain of joints, a tube a segment on its
# own bone, the scales painted by arc length and the angle round the line. The fan is loose flat plates (feebas.py).
# The export's chain: body (0.05, 0.28, 0.1), seg1 (0.04, 0.33) round the back, seg2 (-0.17, 0.16), seg3 (-0.12,
# -0.08), seg4 (0.09, -0.12) round the front, seg5 to seg7 up the neck, head (0, -0.06, 0.72); tail and tailFin both
# on (0.24, 0.28, 0.08).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, tri, polygon, args

src, out = args()
k = Kit(src, coat="scales")

CREAM, BLUE, NET, PINK, HAIR, FEELER = "#F3E4BC", "#3EA4E4", "#1A2030", "#E2526C", "#EE6A80", "#DE4664"

# --- the line of the body, from the tail's tip to the head ----------------------------------------------------------
LINE = np.array([(0.4, 0.215, 0.49), (0.395, 0.235, 0.33), (0.345, 0.265, 0.155), (0.24, 0.28, 0.08),
                 (0.04, 0.33, 0.09), (-0.17, 0.16, 0.1), (-0.12, -0.08, 0.11), (0.09, -0.12, 0.13),
                 (0.13, -0.02, 0.28), (0.05, 0.02, 0.47), (0.0, -0.02, 0.62), (0.0, -0.06, 0.72)])
RADS = np.array([0.03, 0.036, 0.046, 0.056, 0.07, 0.084, 0.094, 0.09, 0.075, 0.058, 0.048, 0.044])
BONES = ["tailFin", "tail", "tail", "body", "seg1", "seg2", "seg3", "seg4", "seg5", "seg6", "seg7"]


def spline(pts, per=12):
    pts = np.array(pts, float)
    ext = np.vstack([2 * pts[0] - pts[1], pts, 2 * pts[-1] - pts[-2]])
    C, S = [], []
    for i in range(len(pts) - 1):
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        for j in range(per):
            t = j / per
            C.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                            + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
            S.append(i + t)
    C.append(pts[-1]); S.append(len(pts) - 1)
    return np.array(C), np.array(S)


CRV, SEG = spline(LINE, per=14)
RAD = np.interp(SEG, np.arange(len(LINE)), RADS)
for i, bone in enumerate(BONES):
    sel = (SEG >= i - 0.15) & (SEG <= i + 1.15)
    k.add(f"seg{i}", tube(CRV[sel][::2], RAD[sel][::2], seg=20, per=5), CREAM, bone)
# the neck carried up into the back of the head
HC = np.array([0.0, -0.088, 0.77])
k.add("nape", tube([LINE[-2], LINE[-1], (0, -0.075, 0.775)], [0.048, 0.046, 0.05]), CREAM, "head")

# --- the scales: a fine copy of the line with arc length and a frame carried along it -------------------------------
FC, FS = spline(LINE, per=80)
CUM = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(FC, axis=0), axis=1))])
FT = np.gradient(FC, axis=0); FT /= np.linalg.norm(FT, axis=1, keepdims=True)
FN = np.zeros_like(FC)
n = np.array([1.0, 0, 0]); n -= FT[0] * (n @ FT[0]); n /= np.linalg.norm(n)
for i in range(len(FC)):
    n = n - FT[i] * (n @ FT[i]); n /= np.linalg.norm(n)
    FN[i] = n
FB = np.cross(FT, FN)


def along(Q):
    j = np.concatenate([((Q[a:a + 3000, None, :] - FC[None]) ** 2).sum(2).argmin(1) for a in range(0, len(Q), 3000)])
    rad = Q - FC[j]
    t = (rad * FT[j]).sum(1)                    # the arc length between the samples, or the lines step
    rad = rad - FT[j] * t[:, None]
    rad /= np.linalg.norm(rad, axis=1, keepdims=True) + 1e-9
    th = np.arctan2((rad * FB[j]).sum(1), (rad * FN[j]).sum(1))
    return j, rad, th, CUM[j] + t


AROUND = 5                       # diamonds round the body
def lattice(Q):
    j, rad, th, s = along(Q)
    u = s / 0.072
    v = th * AROUND / (2 * math.pi)
    return j, rad, u, v


def blue(Q):
    j, rad, u, v = lattice(Q)
    edge = 5.25 + 0.3 * tri(v / 2)                  # the edge of the blue follows the scales' zigzag
    return inside((FS[j] - edge) * 0.06, 0.003) * (FS[j] < 10.5)


def pinks(Q):
    j, rad, u, v = lattice(Q)
    under = rad @ unit(0, -0.35, -1)
    # whole diamonds: the cell's own middle decides, so the pink stops at the black lines
    cu, cv = np.floor(u + v), np.floor(u - v)
    mid = (cu - cv) / 2 % AROUND
    return (under > 0.15) * (FS[j] < 3.9) * (((cu + cv) % 3) != 0) * 1.0


def net(Q):
    j, rad, u, v = lattice(Q)
    a, b = (u + v) % 1.0, (u - v) % 1.0
    d = np.minimum(np.minimum(a, 1 - a), np.minimum(b, 1 - b))
    return inside((d - 0.115) * 0.07, 0.0015)


ON = ["seg"]
k.paint(blue, BLUE, on=ON)
k.paint(lambda Q: pinks(Q) * blue(Q), PINK, on=ON)
k.paint(lambda Q: net(Q) * blue(Q), NET, on=ON)

# --- the tail's fan: four blue fins opening upward, a pink heart in each ----------------------------------------------
def lens(a0, a1, width, bulge=0.5, n=24):
    a0, a1 = np.array(a0, float), np.array(a1, float)
    d = a1 - a0; L = np.linalg.norm(d); d /= L
    side = np.array([-d[1], d[0]])
    pts = []
    for i in range(n):
        t = i / (n - 1)
        r = width * np.sin(np.pi * t ** (math.log(0.5) / math.log(bulge))) ** 0.7
        pts.append(a0 + d * L * t + side * r)
    for i in range(n - 2, 0, -1):
        t = i / (n - 1)
        r = width * np.sin(np.pi * t ** (math.log(0.5) / math.log(bulge))) ** 0.7
        pts.append(a0 + d * L * t - side * r)
    return np.array(pts)


def area(p):
    return 0.5 * np.sum(p[:, 0] * np.roll(p[:, 1], -1) - np.roll(p[:, 0], -1) * p[:, 1])


def plate(outline, origin, thick):
    """A flat plate in the plane facing the front (x out, z up) with a fan of triangles from its middle."""
    p = np.array(outline, float)
    if area(p) < 0:
        p = p[::-1]
    o = np.array(origin, float)
    W = o + np.stack([p[:, 0], np.zeros(len(p)), p[:, 1]], axis=1)
    c = W.mean(0)
    m = len(p)
    nrm = np.array([0, -1.0, 0])
    V = np.vstack([W + nrm * thick / 2, W - nrm * thick / 2, [c + nrm * thick / 2, c - nrm * thick / 2]])
    F = []
    for i in range(m):
        j = (i + 1) % m
        F.append([2 * m, j, i])
        F.append([2 * m + 1, m + i, m + j])
        F.append([i, j, m + j, m + i])
    return V, F


TIP = LINE[0]
for n_, a in enumerate([-42, -15, 12, 39]):
    d = np.array([math.sin(math.radians(a)), math.cos(math.radians(a))])
    o = TIP + np.array([0, 0.006 * (n_ % 2) - 0.003, -0.01])
    k.add(f"fan{n_}", plate(lens(d * 0.0, d * 0.165, 0.042, bulge=0.5), o, 0.008), BLUE, "tailFin", fuse=False)
    k.add(f"heart{n_}", plate(lens(d * 0.045, d * 0.128, 0.016, bulge=0.45), o, 0.014), PINK, "tailFin", fuse=False)

# --- the head: small, pointed in front, a tall crest ---------------------------------------------------------------
def snout(V):
    V = V.copy()
    t = np.clip(-V[:, 1], 0, 1)
    V[:, 0] *= 1 - 0.38 * t
    V[:, 2] *= 1 - 0.3 * t
    V[:, 2] -= 0.18 * t ** 2                    # the muzzle dips a little
    return V
k.add("skull", ell(HC, (0.066, 0.1, 0.064), deform=snout), CREAM, "head")
k.add("crest", tube([(0, -0.075, 0.81), (0, -0.065, 0.87), (0, -0.055, 0.925), (0, -0.05, 0.968)],
                    [0.038, 0.025, 0.012, 0.003]), CREAM, "head")
# pink behind the eyes, where the long fins grow
EB = np.array([0.045, -0.105, 0.795])
k.paint(lambda Q: inside(-((Q - EB * np.array([0, 1, 1])) @ unit(0, 1, -0.45)), 0.002)
        * inside(0.03 - np.abs(Q[:, 0]), 0.002) * inside(0.715 - Q[:, 2], 0.002)
        * inside(Q[:, 2] - 0.835, 0.002), HAIR)

# Thin red feelers rising from the brow, curling out and back down like the halves of a heart
k.add("feeler", tube([(0.022, -0.14, 0.81), (0.045, -0.145, 0.865), (0.085, -0.125, 0.915), (0.135, -0.095, 0.93),
                      (0.175, -0.065, 0.895), (0.188, -0.045, 0.835)], [0.0075, 0.007, 0.0065, 0.006, 0.0055, 0.005]),
      FEELER, "head", fuse=False, mirror=True)

# Long pink fins falling from behind the eyes like hair, ragged at their ends
FACE = unit(0.45, -1, 0)
pts = np.array([(0.045, -0.07, 0.785), (0.07, -0.07, 0.72), (0.1, -0.09, 0.62), (0.135, -0.11, 0.52),
                (0.165, -0.125, 0.43), (0.185, -0.132, 0.37)])
k.add("hair", blade(pts, [0.024, 0.04, 0.05, 0.055, 0.056, 0.054], FACE, 0.22), HAIR, "head", fuse=False,
      mirror=True)
# the end torn into three short points
e0 = pts[-1]
for n_, (dx, dz) in enumerate([(-0.026, -0.065), (0.03, -0.045)]):
    e = e0 + np.array([dx * 1.25, -0.004 * n_, dz])
    k.add(f"hairtip{n_}", blade([e0 + np.array([dx, 0, 0.03]), e], [0.03, 0.004], FACE, 0.22), HAIR, "head",
          fuse=False, mirror=True)

# --- the eyes: red almonds under the brow, a dark pupil and a glint -------------------------------------------------
LID, INK = srgb(CREAM), srgb("#3A0C1A")
IRIS, DEEP, WHITE = srgb("#C81E4C"), srgb("#8E1236"), srgb("#FFFFFF")
ALMOND = polygon([(-1, -0.15), (-0.55, 0.5), (0.25, 0.8), (1, 0.45), (0.75, -0.35), (0.05, -0.75), (-0.6, -0.55)])
def eye(u, v):
    c = np.tile(IRIS, (len(u), 1))
    c[v > 0.35] = DEEP
    c[((u - 0.05) / 0.32) ** 2 + ((v - 0.05) / 0.55) ** 2 < 1] = INK
    c[((u + 0.25) / 0.2) ** 2 + ((v - 0.35) / 0.22) ** 2 < 1] = WHITE
    return c
de = unit(0.78, -0.58, 0.18)
def eyes(state):
    k.decal((0.04, -0.128, 0.79), de, (0.028, 0.023), eye_look(state, eye, LID, INK, line=0.2), eps=0.006,
            outline=ALMOND, rings=20, segs=56, mirror=True)

k.build(voxel=0.0068, smooth=4)
k.eye_states(eyes)
k.export(out)
