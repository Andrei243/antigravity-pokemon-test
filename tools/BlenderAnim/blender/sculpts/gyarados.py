# Gyarados sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gyarados.py -- <the export's Gyarados.glb> <out.glb>
# Magikarp grown into a blue sea dragon: a long body coiled on the ground and rearing up, its underside cream plates
# in segments, cream ovals along its blue flanks, white spiky fins along the back, a great head with a gaping pink
# maw rimmed in cream and four white fangs, a three-pointed dark blue crest on the brow, white fins spread at the
# sides of the head, two long blue barbels hooked at their ends (Magikarp's barbels; a male's are blue), furious red
# eyes under heavy brows, and a fan at the tail's end, blue at its root and white and saw-toothed at its edge.
# The export's chain: tailFin (0.22, 0.44, 0.12), tail and body (0.04..0.06, 0.3..0.32, 0.14), seg1 to seg6 coiling
# round on the ground and up, head (0, -0.1, 0.86) on top facing forward, jaw beside it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

BLUE, CREAM, PLATE, WHITE, NAVY = "#3C9EDC", "#F2E2A0", "#CDB574", "#EAF0F6", "#2A5BA8"
MOUTH, RIM, TOOTH = "#E2808A", "#F4E49C", "#FAFAFA"


# --- flat plates (as magikarp.py's) --------------------------------------------------------------------------------
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
    return np.vstack([front, back, np.array(rim)]), F


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


# --- the body: one smooth line from the tail's tip through every joint to the head, a tube a span on its bone --------
FIN = np.array([0.22, 0.44, 0.12]); BODY = np.array([0.04, 0.3, 0.14])
TIP = FIN + (FIN - BODY) * 0.22
P = np.array([TIP, FIN, BODY, (-0.18, 0.14, 0.15), (-0.14, -0.12, 0.18), (0.1, -0.17, 0.26), (0.16, -0.05, 0.44),
              (0.05, 0.03, 0.62), (-0.01, -0.03, 0.77), (0.0, -0.1, 0.86)])
R = np.array([0.036, 0.05, 0.085, 0.105, 0.115, 0.12, 0.116, 0.106, 0.098, 0.094])
BONES = ["tailFin", "tail", "body", "seg1", "seg2", "seg3", "seg4", "seg5", "seg6"]

CRV, SEG = spline(P, per=14)
RAD = np.interp(SEG, np.arange(len(P)), R)
for i, bone in enumerate(BONES):
    sel = (SEG >= i - 0.15) & (SEG <= i + 1.15)
    k.add(f"seg{i}", tube(CRV[sel][::2], RAD[sel][::2], seg=20, per=5), BLUE, bone)

# The belly's way at each point of the line: down where the body lies on the ground, forward where it rears up
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


SPAN = 8.25                      # the paint stops under the head


def belly(Q):
    j, d = facing(Q)
    th = 0.2 - 0.3 * smoothstep(0.3, 0.52, FCRV[j, 2])      # narrow under the coil on the ground, wide up the front
    return inside(th - d, 0.012) * (FSEG[j] < SPAN) * (FSEG[j] > 0.6)


PER = 0.078


def plates(Q):
    j, d = facing(Q)
    f = (CUM[j] / PER) % 1.0
    th = 0.2 - 0.3 * smoothstep(0.3, 0.52, FCRV[j, 2])
    return inside((np.abs(f - 0.5) - 0.44) * -PER, 0.003) * smoothstep(th + 0.02, th + 0.1, d) * \
        (FSEG[j] < SPAN - 0.2) * (FSEG[j] > 1.0)


SPER = 0.16


def spots(Q):
    j, d = facing(Q)
    f = (CUM[j] / SPER) % 1.0
    u = (f - 0.5) * SPER / 0.042
    th = 0.2 - 0.3 * smoothstep(0.3, 0.52, FCRV[j, 2])
    v = (d - th + 0.34) / 0.15
    return inside(np.sqrt(u * u + v * v) - 1, 0.08) * (FSEG[j] < SPAN - 0.4) * (FSEG[j] > 1.2)


k.paint(belly, CREAM, on=["seg"])
k.paint(plates, PLATE, on=["seg"])
k.paint(spots, CREAM, on=["seg"])


def at(s):
    i = int(np.argmin(np.abs(FSEG - s)))
    return FCRV[i], TAN[i], BEL[i], float(np.interp(s, np.arange(len(P)), R))


# --- white spiky fins along the back, leaning toward the tail ---------------------------------------------------------
DORSAL = [(-0.06, -0.03), (-0.05, 0.03), (-0.035, 0.13), (-0.005, 0.055), (0.03, 0.12), (0.05, 0.045), (0.095, 0.085),
          (0.08, 0.0), (0.07, -0.03)]
for n, (s, size) in enumerate([(8.55, 1.15), (6.9, 1.05), (5.0, 1.15), (2.7, 0.85)]):
    c, t, bel, r = at(s)
    back = -bel
    k.add(f"dorsal{n}", flat(np.array(DORSAL) * size, c + back * r * 0.72, -t, back, 0.03,
                             taper=lambda b: np.clip(1 - 0.7 * b / 0.12, 0.3, 1)), WHITE,
          "head" if s > 8.4 else BONES[int(s)], fuse=False)

# --- the tail's fan: blue at the root, white and saw-toothed at its edge -------------------------------------------
TD = unit(*(FIN - BODY))
FAN_O = TIP - TD * 0.02
FAN = [(-0.03, -0.035), (0.05, -0.07), (0.13, -0.095), (0.13, -0.055), (0.16, -0.035), (0.155, 0.0), (0.185, 0.03),
       (0.165, 0.07), (0.19, 0.11), (0.155, 0.135), (0.16, 0.19), (0.07, 0.1), (-0.03, 0.04)]
k.add("fan", flat(FAN, FAN_O, TD, (0, 0, 1), 0.016), BLUE, "tailFin", fuse=False)
EDGE = [(0.085, -0.075), (0.13, -0.095), (0.13, -0.055), (0.16, -0.035), (0.155, 0.0), (0.185, 0.03), (0.165, 0.07),
        (0.19, 0.11), (0.155, 0.135), (0.16, 0.19), (0.1, 0.13), (0.11, 0.06), (0.1, -0.02)]
k.add("fanedge", flat(EDGE, FAN_O, TD, (0, 0, 1), 0.022), WHITE, "tailFin", fuse=False)

# --- the head: a great skull over a snout, the lower jaw dropped open on its own bone -------------------------------
# Laid out at one size and grown about the head's joint, everything on it together (the pictures' head is huge)
J, HS = np.array([0.0, -0.1, 0.86]), 1.18


def hp(p):
    return J + (np.array(p, float) - J) * HS


def hs(shape):
    V, F = shape
    return J + (V - J) * HS, F


k.add("skull", hs(ell((0, -0.12, 0.95), (0.125, 0.15, 0.105), rot=(-8, 0, 0))), BLUE, "head")
k.add("snout", hs(ell((0, -0.27, 0.93), (0.095, 0.13, 0.07), rot=(6, 0, 0))), BLUE, "head")
k.add("throat", hs(ell((0, -0.11, 0.8), (0.1, 0.11, 0.09))), BLUE, "head")
k.add("lowjaw", hs(ell((0, -0.235, 0.762), (0.088, 0.135, 0.05), rot=(34, 0, 0))), BLUE, "jaw")
# heavy brows over the eyes
k.add("brow", hs(ell((0.068, -0.25, 0.995), (0.042, 0.065, 0.024), rot=(-15, 0, -25))), BLUE, "head", mirror=True)

# The maw, carved between the jaws and opening forward; a cream rim round it
MC, MR, MROT = hp((0.0, -0.32, 0.828)), np.array([0.074, 0.17, 0.068]) * HS, (16, 0, 0)
MM = np.array(Euler([math.radians(a) for a in MROT]).to_matrix())


def rim(Q):
    q = (Q - MC) @ MM / MR
    d = (np.linalg.norm(q, axis=1) - 1) * 0.07
    return inside(d - 0.024, 0.004) * (Q[:, 1] < -0.2)


k.paint(rim, RIM)
k.add("maw", ell(MC, MR, rot=MROT), MOUTH, "head", carve=True)

# Four fangs: two from the upper lip down, two from the lower up, each seated just inside the rim
for x, top, bot, bone in ((0.05, (-0.352, 0.885), (-0.358, 0.82), "head"), (0.05, (-0.33, 0.752), (-0.335, 0.808), "jaw")):
    a = hp((x, top[0], top[1])); b = hp((x, bot[0], bot[1]))
    k.add(f"fang{bone}", blade([a, (a + b) / 2, b], [0.017, 0.011, 0.002], (0, 1, 0), 0.6), TOOTH, bone, fuse=False,
          mirror=True, surface="shell")

# The crest: three dark blue points, the middle tall and leaning forward, the outer two up and out
k.add("crest", hs(blade([(0, -0.17, 1.02), (0, -0.21, 1.09), (0, -0.25, 1.15)], [0.034, 0.022, 0.003], (1, 0, 0), 0.55)),
      NAVY, "head", fuse=False, surface="shell")
k.add("crestside", hs(blade([(0.04, -0.16, 1.02), (0.085, -0.2, 1.07), (0.125, -0.24, 1.1)], [0.026, 0.017, 0.003],
                         (0.6, 0, 1), 0.55)), NAVY, "head", fuse=False, mirror=True, surface="shell")

# The white fins at the sides of the head, spread back and out from behind the mouth
SIDE = [(-0.01, -0.05), (0.06, -0.075), (0.13, -0.06), (0.09, -0.015), (0.16, 0.02), (0.1, 0.035), (0.13, 0.095),
        (0.05, 0.065), (-0.01, 0.05)]
k.add("sidefin", flat(np.array(SIDE) * 1.35, hp((0.1, -0.15, 0.87)), (0.75, 1, 0.05), (0, -0.15, 1), 0.018), WHITE, "head", fuse=False,
      mirror=True)

# The barbels: blue, from the corners of the upper lip, out and down, hooked at their ends
k.add("barbel", hs(tube([(0.07, -0.33, 0.87), (0.11, -0.36, 0.82), (0.14, -0.365, 0.73), (0.14, -0.34, 0.65),
                      (0.125, -0.315, 0.615), (0.11, -0.325, 0.64)], [0.012, 0.011, 0.011, 0.01, 0.009, 0.008])),
      BLUE, "head", fuse=False, mirror=True)

# --- the eyes: red with a dark pupil, cut across the top by the brow sloping down toward the snout ------------------
INK, RED, LID = srgb("#1A1418"), srgb("#D82A36"), srgb(BLUE)
BA, BB = 0.42, 0.42                                  # the brow's line: v = BA + BB u (u toward the back)


def cut(th):
    s = math.sin(th) - BB * math.cos(th)
    return min(1.0, BA / s) if s > 1e-6 else 1.0


def eye(u, v):
    c = np.tile(RED, (len(u), 1))
    c[((u + 0.08) / 0.36) ** 2 + ((v + 0.05) / 0.46) ** 2 < 1] = INK
    return c


de = unit(0.52, -0.8, 0.22)
EC = hp((0.07, -0.27, 0.962))


def eyes(state):
    look = eye_look(state, eye, LID, INK, line=0.2)
    k.decal(EC, de, (0.032 * HS, 0.03 * HS), look, eps=0.006, rings=20, segs=56, outline=cut)
    m = np.array([-1, 1, 1.0])
    k.decal(EC * m, de * m, (0.032 * HS, 0.03 * HS), lambda u, v: look(-u, v), eps=0.006, rings=20, segs=56,
            outline=lambda th: cut(math.pi - th))


k.build(voxel=0.0075, smooth=4)
k.eye_states(eyes)
k.export(out)
