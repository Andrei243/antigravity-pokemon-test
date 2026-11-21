# Mega Gyarados sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gyaradosmega.py -- <the export's Gyarados-Mega.glb> <out.glb>
# Copied from gyarados.py (which builds as it is read) and changed as the two pictures change it: the same coiled,
# rearing blue body and great head with its gaping maw, now with a dark slate underside edged by a red line and red
# ovals on the flanks; its largest new mass a great pair of pale yellow sails along the back of the rearing neck, each
# held on five blue ribs and scalloped between them, spread in a V; a long dark horn swept up and back off the brow
# with two shorter ones beside it; big yellow fins zigzagging back from the sides of the head and blue spines round
# the cheeks and jaw; long white whiskers trailing back from the lips; blue spines along the coil's flanks, small
# yellow fins on the lower back and the tail, and the tail's fan blue with a yellow edge.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, blade, sheet, hull, outward, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

BLUE, DARK, SEAMC, RED, YELLOW, NAVY = "#2F92DA", "#3C3B4A", "#2A2934", "#D62A3C", "#F2E8A2", "#2A5BA8"
HORN, MOUTH, RIM, TOOTH, WHISKER = "#3E4150", "#E2808A", "#F4E49C", "#FAFAFA", "#F2F4F8"


# --- flat plates (as gyarados.py's) ---------------------------------------------------------------------------------
def area(p):
    return 0.5 * np.sum(p[:, 0] * np.roll(p[:, 1], -1) - np.roll(p[:, 0], -1) * p[:, 1])


def earclip(p):
    idx = list(range(len(p)))
    tris = []
    def cross(o, a, b):
        return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])
    guard = 0
    while len(idx) > 3 and guard < 100000:
        guard += 1
        m = len(idx)
        cut = False
        for i in range(m):
            a, b, c = idx[i - 1], idx[i], idx[(i + 1) % m]
            if cross(p[a], p[b], p[c]) <= 1e-12:
                continue
            if any(cross(p[a], p[b], p[j]) >= 0 and cross(p[b], p[c], p[j]) >= 0 and cross(p[c], p[a], p[j]) >= 0
                   for j in idx if j not in (a, b, c)):
                continue
            tris.append((a, b, c)); idx.pop(i); cut = True
            break
        if not cut:
            tris.append((idx[-1], idx[0], idx[1])); idx.pop(0)
    tris.append(tuple(idx))
    return tris


def flat(outline, origin, ex, ez, thick, taper=None):
    """A flat plate of a polygon (a, b in the plane of ex, ez about origin); taper(b) scales its thickness."""
    ex, ez = unit(*ex), unit(*ez)
    nrm = np.cross(ex, ez); nrm /= np.linalg.norm(nrm)
    p = np.array(outline, float)
    if area(p) < 0:
        p = p[::-1]
    W = np.array(origin, float) + p[:, :1] * ex + p[:, 1:] * ez
    t = np.full(len(p), thick / 2) if taper is None else thick / 2 * taper(p[:, 1])
    m = len(p)
    front, back = W + nrm * t[:, None], W - nrm * t[:, None]
    F = []
    for a, b, c in earclip(p):
        F.append([a, b, c]); F.append([m + c, m + b, m + a])
    rim = []
    for i in range(m):
        j = (i + 1) % m
        rim.extend([front[i], back[i], back[j], front[j]])
        F.append([2 * m + 4 * i, 2 * m + 4 * i + 1, 2 * m + 4 * i + 2, 2 * m + 4 * i + 3])
    return outward((np.vstack([front, back, np.array(rim)]), F))


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


def spine(base, d, length, r, flat_=0.5, up=(0, 0, 1.0)):
    """A solid spine with flat faces: a diamond of points round base, flattened across up, and a tip."""
    d = unit(*d); up = np.array(up, float)
    a = np.cross(d, up)
    if np.linalg.norm(a) < 1e-3:
        a = np.cross(d, (1.0, 0, 0))
    a /= np.linalg.norm(a); b = np.cross(a, d)
    base = np.array(base, float)
    return hull([base + a * r, base - a * r, base + b * r * flat_, base - b * r * flat_, base - d * r * 0.5,
                 base + d * length])


# --- the body: as gyarados.py's, one smooth line from the tail's tip through every joint to the head ----------------
FIN = np.array([0.22, 0.44, 0.12]); BODY = np.array([0.04, 0.3, 0.14])
TIP = FIN + (FIN - BODY) * 0.22
P = np.array([TIP, FIN, BODY, (-0.18, 0.14, 0.15), (-0.14, -0.12, 0.18), (0.1, -0.17, 0.26), (0.16, -0.05, 0.44),
              (0.05, 0.03, 0.62), (-0.01, -0.03, 0.77), (0.0, -0.1, 0.86)])
R = np.array([0.036, 0.05, 0.088, 0.11, 0.12, 0.125, 0.12, 0.11, 0.1, 0.095])
BONES = ["tailFin", "tail", "body", "seg1", "seg2", "seg3", "seg4", "seg5", "seg6"]

CRV, SEG = spline(P, per=14)
RAD = np.interp(SEG, np.arange(len(P)), R)
for i, bone in enumerate(BONES):
    sel = (SEG >= i - 0.15) & (SEG <= i + 1.15)
    k.add(f"seg{i}", tube(CRV[sel][::2], RAD[sel][::2], seg=20, per=5), BLUE, bone)

FCRV, FSEG = spline(P, per=80)
CUM = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(FCRV, axis=0), axis=1))])
TAN = np.gradient(FCRV, axis=0); TAN /= np.linalg.norm(TAN, axis=1, keepdims=True)
w = smoothstep(0.3, 0.52, FCRV[:, 2])[:, None]
D = (1 - w) * np.array([0, 0, -1.0]) + w * np.array([0, -1.0, 0])
BEL = D - TAN * (D * TAN).sum(1, keepdims=True); BEL /= np.linalg.norm(BEL, axis=1, keepdims=True)


def nearest(Q):
    return np.concatenate([((Q[a:a + 3000, None, :] - FCRV[None]) ** 2).sum(2).argmin(1) for a in range(0, len(Q), 3000)])


def facing(Q):
    j = nearest(Q)
    rad = Q - FCRV[j]; rad /= np.linalg.norm(rad, axis=1, keepdims=True) + 1e-9
    return j, (rad * BEL[j]).sum(1)


SPAN = 8.25


def width(j):
    return 0.2 - 0.3 * smoothstep(0.3, 0.52, FCRV[j, 2])


def belly(Q):
    j, d = facing(Q)
    return inside(width(j) - d, 0.01) * (FSEG[j] < SPAN) * (FSEG[j] > 0.35)


PER = 0.078


def plates(Q):
    j, d = facing(Q)
    f = (CUM[j] / PER) % 1.0
    th = width(j)
    return inside((np.abs(f - 0.5) - 0.45) * -PER, 0.003) * smoothstep(th + 0.03, th + 0.12, d) * \
        (FSEG[j] < SPAN - 0.2) * (FSEG[j] > 0.8)


def redline(Q):
    """The red line along the edge of the dark underside, as the pictures draw it."""
    j, d = facing(Q)
    return inside(np.abs(d - width(j) - 0.07) - 0.05, 0.008) * (FSEG[j] < SPAN - 0.1) * (FSEG[j] > 0.6)


SPER = 0.2


def spots(Q):
    j, d = facing(Q)
    f = (CUM[j] / SPER) % 1.0
    u = (f - 0.5) * SPER / 0.05
    v = (d - width(j) + 0.42) / 0.2
    return inside(np.abs(u) + np.abs(v) * 0.8 - 1, 0.08) * (FSEG[j] < SPAN - 0.5) * (FSEG[j] > 1.0)


k.paint(belly, DARK, on=["seg"])
k.paint(plates, SEAMC, on=["seg"])
k.paint(redline, RED, on=["seg"])
k.paint(spots, RED, on=["seg"])


def at(s):
    i = int(np.argmin(np.abs(FSEG - s)))
    return FCRV[i], TAN[i], BEL[i], float(np.interp(s, np.arange(len(P)), R))


def bone_at(s):
    return "head" if s > 8.4 else BONES[min(int(s), len(BONES) - 1)]


# --- THE SAILS: the Mega's largest new mass, a pair along the back of the rearing neck, spread in a V ---------------
# Five blue ribs a side, rooted along the back from behind the head down to the coil, raking up and back and out
# some 35 degrees; a pale yellow skin between them, its trailing edge scalloped between the ribs' tips.
RIB_S = [8.3, 7.6, 6.9, 6.2]
RIB_L = [0.48, 0.52, 0.46, 0.32]
RIB_UP = [0.62, 0.48, 0.32, 0.16]
UPW = np.array([0, 0, 1.0])


def ribs(s_side):
    roots, dirs = [], []
    for s, up in zip(RIB_S, RIB_UP):
        c, t, bel, r = at(s)
        back = -bel
        side = np.cross(t, back); side /= np.linalg.norm(side)
        if side[0] < 0:
            side = -side
        roots.append(c + back * r * 0.62 + side * s_side * r * 0.4)
        d = back * 0.75 + UPW * up + side * s_side * 0.85
        dirs.append(d / np.linalg.norm(d))
    return np.array(roots), np.array(dirs)


for sd, nm in ((1, "R"), (-1, "L")):
    roots, dirs = ribs(sd)
    for j in range(len(RIB_S) - 1):
        def skin(u, v, j=j):
            # u across one bay from rib j to rib j+1, v from the root to the scalloped edge
            uu = 0.02 + 0.96 * u
            root = roots[j] * (1 - uu)[..., None] + roots[j + 1] * uu[..., None]
            d = dirs[j] * (1 - uu)[..., None] + dirs[j + 1] * uu[..., None]
            d /= np.linalg.norm(d, axis=-1, keepdims=True)
            L = RIB_L[j] * (1 - uu) + RIB_L[j + 1] * uu
            L = L * (1 - 0.3 * np.sin(math.pi * uu) ** 1.5) * 0.97
            return root + d * (0.01 + v * L)[..., None]
        k.add(f"sail{j}{nm}", outward(sheet(skin, 12, 10, 0.012)), YELLOW, bone_at(RIB_S[j]), fuse=False)
    for j, (r0, d) in enumerate(zip(roots, dirs)):
        L = RIB_L[j]
        k.add(f"rib{j}{nm}", outward(blade([r0 - d * 0.01, r0 + d * L * 0.5, r0 + d * (L + 0.035)],
                                           [0.03, 0.022, 0.006], np.cross(d, UPW) + 1e-6, 0.75)),
              BLUE, bone_at(RIB_S[j]), fuse=False)

# --- small yellow fins on the lower back, blue spines along the coil's flanks ---------------------------------------
DORSAL = [(-0.06, -0.03), (-0.05, 0.03), (-0.035, 0.13), (-0.005, 0.055), (0.03, 0.12), (0.05, 0.045), (0.095, 0.085),
          (0.08, 0.0), (0.07, -0.03)]
for n, (s, size) in enumerate([(4.3, 0.95), (2.7, 0.9), (1.4, 0.7)]):
    c, t, bel, r = at(s)
    back = -bel
    k.add(f"dorsal{n}", flat(np.array(DORSAL) * size, c + back * r * 0.72, -t, back, 0.03,
                             taper=lambda b: np.clip(1 - 0.7 * b / 0.12, 0.3, 1)), YELLOW, bone_at(s), fuse=False)

for n, s in enumerate([1.9, 3.1, 4.6]):
    c, t, bel, r = at(s)
    back = -bel
    side = np.cross(t, back); side /= np.linalg.norm(side)
    for sd, nm in ((1, "R"), (-1, "L")):
        d = side * sd * 0.8 + back * 0.35 - t * 0.45
        k.add(f"flank{n}{nm}", spine(c + side * sd * r * 0.8, d, 0.075 + 0.015 * n, 0.024, up=back), BLUE,
              bone_at(s), fuse=False)

# --- the tail's fan: blue at the root, yellow and saw-toothed at its edge ---------------------------------------------
TD = unit(*(FIN - BODY))
FAN_O = TIP - TD * 0.02
FAN = [(-0.03, -0.035), (0.05, -0.07), (0.13, -0.095), (0.13, -0.055), (0.16, -0.035), (0.155, 0.0), (0.185, 0.03),
       (0.165, 0.07), (0.19, 0.11), (0.155, 0.135), (0.16, 0.19), (0.07, 0.1), (-0.03, 0.04)]
k.add("fan", flat(FAN, FAN_O, TD, (0, 0, 1), 0.016), BLUE, "tailFin", fuse=False)
EDGE = [(0.085, -0.075), (0.13, -0.095), (0.13, -0.055), (0.16, -0.035), (0.155, 0.0), (0.185, 0.03), (0.165, 0.07),
        (0.19, 0.11), (0.155, 0.135), (0.16, 0.19), (0.1, 0.13), (0.11, 0.06), (0.1, -0.02)]
k.add("fanedge", flat(EDGE, FAN_O, TD, (0, 0, 1), 0.022), YELLOW, "tailFin", fuse=False)

# --- the head: as gyarados.py's, grown about its joint ---------------------------------------------------------------
J, HS = np.array([0.0, -0.1, 0.86]), 1.2


def hp(p):
    return J + (np.array(p, float) - J) * HS


def hs(shape):
    V, F = shape
    return J + (V - J) * HS, F


k.add("skull", hs(ell((0, -0.12, 0.95), (0.125, 0.15, 0.105), rot=(-8, 0, 0))), BLUE, "head")
k.add("snout", hs(ell((0, -0.27, 0.93), (0.095, 0.13, 0.07), rot=(6, 0, 0))), BLUE, "head")
k.add("throat", hs(ell((0, -0.11, 0.8), (0.1, 0.11, 0.09))), BLUE, "head")
k.add("lowjaw", hs(ell((0, -0.235, 0.762), (0.088, 0.135, 0.05), rot=(34, 0, 0))), BLUE, "jaw")
k.add("brow", hs(ell((0.068, -0.25, 0.995), (0.042, 0.065, 0.024), rot=(-15, 0, -25))), BLUE, "head", mirror=True)

MC, MR, MROT = hp((0.0, -0.32, 0.828)), np.array([0.074, 0.17, 0.068]) * HS, (16, 0, 0)
MM = np.array(Euler([math.radians(a) for a in MROT]).to_matrix())


def rim(Q):
    q = (Q - MC) @ MM / MR
    d = (np.linalg.norm(q, axis=1) - 1) * 0.07
    return inside(d - 0.024, 0.004) * (Q[:, 1] < -0.2)


k.paint(rim, RIM)
k.add("maw", ell(MC, MR, rot=MROT), MOUTH, "head", carve=True)

for x, top, bot, bone in ((0.05, (-0.352, 0.885), (-0.358, 0.82), "head"), (0.05, (-0.33, 0.752), (-0.335, 0.808), "jaw")):
    a = hp((x, top[0], top[1])); b = hp((x, bot[0], bot[1]))
    k.add(f"fang{bone}", blade([a, (a + b) / 2, b], [0.018, 0.012, 0.002], (0, 1, 0), 0.6), TOOTH, bone, fuse=False,
          mirror=True, surface="shell")

# The horn: long, dark, swept up and back off the brow; two shorter beside it, up and out
k.add("horn", hull([hp((0.03, -0.2, 1.0)), hp((-0.03, -0.2, 1.0)), hp((0.0, -0.25, 1.01)), hp((0.0, -0.1, 1.04)),
                    hp((0.0, -0.17, 1.08)), hp((0.0, 0.14, 1.2))]), HORN, "head", fuse=False, surface="shell")
k.add("hornside", hull([hp((0.04, -0.17, 1.0)), hp((0.07, -0.15, 1.0)), hp((0.05, -0.12, 1.02)),
                        hp((0.055, -0.19, 1.03)), hp((0.15, -0.02, 1.1))]), HORN, "head", fuse=False, mirror=True,
      surface="shell")

# The yellow fins at the sides of the head: big, zigzagging back and out from behind the mouth
SIDE = [(-0.01, -0.05), (0.07, -0.07), (0.15, -0.05), (0.11, -0.012), (0.21, 0.02), (0.15, 0.04), (0.25, 0.09),
        (0.12, 0.075), (0.15, 0.13), (0.04, 0.075), (-0.01, 0.05)]
k.add("sidefin", flat(np.array(SIDE) * 1.4, hp((0.1, -0.15, 0.88)), (1, 0.7, 0.15), (0, -0.2, 1), 0.02), YELLOW, "head",
      fuse=False, mirror=True)

# Blue spines round the cheeks and the jaw, pointing back
for n, (p, d, L) in enumerate([((0.09, -0.2, 0.84), (0.7, 0.7, -0.15), 0.08), ((0.08, -0.14, 0.77), (0.6, 0.75, -0.4), 0.08),
                               ((0.1, -0.08, 0.96), (0.7, 0.8, 0.3), 0.07)]):
    k.add(f"cheek{n}", spine(hp(p), d, L * HS, 0.022), BLUE, "jaw" if n == 1 else "head", fuse=False, mirror=True)

# The whiskers: white, from the corners of the upper lip out and back, trailing down past the neck
k.add("whisker", tube([hp((0.075, -0.33, 0.87)), hp((0.13, -0.33, 0.83)), hp((0.19, -0.26, 0.76)),
                       hp((0.23, -0.12, 0.7)), hp((0.25, 0.0, 0.64)), hp((0.255, 0.07, 0.61))],
                      [0.015, 0.014, 0.014, 0.013, 0.012, 0.011]),
      WHISKER, "head", fuse=False, mirror=True)

# --- the eyes: as gyarados.py's, red with a dark pupil under the brow ------------------------------------------------
INK, REDE, LID = srgb("#1A1418"), srgb("#D82A36"), srgb(BLUE)
BA, BB = 0.42, 0.42


def cut(th):
    s = math.sin(th) - BB * math.cos(th)
    return min(1.0, BA / s) if s > 1e-6 else 1.0


def eye(u, v):
    c = np.tile(REDE, (len(u), 1))
    c[((u + 0.08) / 0.36) ** 2 + ((v + 0.05) / 0.46) ** 2 < 1] = INK
    return c


de = unit(0.52, -0.8, 0.22)
EC = hp((0.07, -0.27, 0.962))


def eyes(state):
    look = eye_look(state, eye, LID, INK, line=0.2)
    k.decal(EC, de, (0.034 * HS, 0.032 * HS), look, eps=0.006, rings=20, segs=56, outline=cut)
    m = np.array([-1, 1, 1.0])
    k.decal(EC * m, de * m, (0.034 * HS, 0.032 * HS), lambda u, v: look(-u, v), eps=0.006, rings=20, segs=56,
            outline=lambda th: cut(math.pi - th))


k.build(voxel=0.0075, smooth=4)
k.eye_states(eyes)
k.export(out)
