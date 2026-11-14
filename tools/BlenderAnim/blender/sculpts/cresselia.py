# Cresselia sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cresselia.py -- <the export's Cresselia.glb> <out.glb>
#
# From the references: a swan of the moon floating with no legs. A smooth teardrop of a body, pale blue above and pale
# gold beneath, drawn out behind into a long flat pointed tail (blue on top, gold under); a slender neck rising in an S
# from the body's front, gold down its front, to a small head whose face is gold and runs forward into a long sharp
# beak. Behind the head stands the great gold crescent, its back curving round behind the skull, its upper horn rising
# over the head and bending forward, its lower horn coming down under the chin; a magenta cap fills the crescent's
# hollow over the back of the head, and the small eye, pink with a dark upper lid, sits under it. On the chest two
# small arms folded forward: a magenta bud at each shoulder with a gold petal under it. The wings are rings of pink
# light: a great loop on each side sweeping up and back, a smaller loop dipping forward under the right arm and one
# dipping back under the left (so the side view shows the pictures' three); each a broad band, open in the middle,
# its rim a deeper pink and its middle pale. The bands' planes are tilted so their faces show from the game's front
# three-quarter and from behind.
# Bones: root 0; body (0, 0, 0.262); head (0, -0.163, 0.604); wingL (-0.07, 0.02, 0.272), wingR (0.07, 0.02, 0.272).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, sheet, srgb, inside, smoothstep, unit, eye_look, polygon, args,
                       outward, hull)

src, out = args()
k = Kit(src, coat="fur")

BLUE, GOLD, MAGENTA = "#96BEEA", "#F4D58E", "#C25F96"
DEEP, PALE = "#F09AC0", "#FBD3E6"
INKH = "#3A1630"

# --- the frame ---------------------------------------------------------------------------------------------------------
BODY_C = np.array([0.0, 0.0, 0.245])
BODY_R = np.array([0.112, 0.175, 0.112])
HEAD_C = np.array([0.0, -0.19, 0.63])
HEAD_R = np.array([0.062, 0.074, 0.074])
NECK = [(0.0, -0.06, 0.29), (0.0, -0.105, 0.37), (0.0, -0.098, 0.45), (0.0, -0.125, 0.53), (0.0, -0.17, 0.6)]
NECK_R = [0.092, 0.054, 0.036, 0.033, 0.04]
# the crescent's middle line in y, z (from the lower horn jutting forward under the chin, round behind the head, to
# the upper horn hooked forward over the face)
CRESCENT = [(-0.235, 0.46), (-0.185, 0.52), (-0.125, 0.6), (-0.096, 0.71), (-0.12, 0.82), (-0.195, 0.9),
            (-0.29, 0.925)]
CRES_RAD = [0.006, 0.02, 0.038, 0.048, 0.04, 0.024, 0.005]        # across the band, in its own plane
CRES_X = [0.012, 0.024, 0.038, 0.044, 0.038, 0.024, 0.01]        # its half thickness from side to side
# The head and everything on it grown together about the skull's middle; the crescent a little less
HEAD_GROW, CRES_GROW = 1.2, 1.12
HEAD_R = HEAD_R * HEAD_GROW
_hc = np.array([HEAD_C[1], HEAD_C[2]])
CRESCENT = [tuple(_hc + (np.array(p) - _hc) * CRES_GROW) for p in CRESCENT]
CRES_RAD = [r * 1.1 for r in CRES_RAD]
CRES_X = [r * 1.1 for r in CRES_X]


def on_head(*p):
    """A point given as it was on the smaller head, moved out with the grown head."""
    return tuple(HEAD_C + (np.array(p, float) - HEAD_C) * HEAD_GROW)


def spline(pts, per=10):
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


def on_ell(c, r, d):
    d = unit(*d)
    s = 1 / math.sqrt(((d / r) ** 2).sum())
    p = np.array(c) + d * s
    n = (p - c) / np.array(r) ** 2
    return p, n / np.linalg.norm(n)


def teardrop(V):
    """The body: full and round in front, drawn out and narrowing behind (+y)."""
    V = V.copy()
    t = np.clip(V[:, 1], 0, 1)
    s = 1 - 0.32 * t ** 1.4
    V[:, 0] *= s; V[:, 2] *= s
    V[:, 2] -= 0.12 * (1 - V[:, 1] ** 2) * (V[:, 2] < 0)   # a fuller belly
    return V


# --- the body, the tail and the neck ---------------------------------------------------------------------------------
k.add("body", ell(BODY_C, BODY_R, deform=teardrop, sub=5), BLUE, "body")
TAIL = [(0.0, 0.05, 0.245), (0.0, 0.18, 0.24), (0.0, 0.32, 0.225), (0.0, 0.47, 0.205)]
k.add("tail", blade(TAIL, [0.1, 0.066, 0.032, 0.004], (0, 0, 1), 0.55), BLUE, "body")
NC, NS = spline(NECK, per=8)
NR = np.interp(NS, np.arange(len(NECK)), NECK_R)
k.add("neck", tube(NC, NR, seg=20, per=3), BLUE, "head")

# The underside gold: under a line running from high on the chest down under the body and back along the tail
# (the lower third of the body's sides, low along the flank from the chest's front to the tail's underside)
ZB_Y = [-0.3, -0.16, -0.06, 0.05, 0.16, 0.3, 0.5]
ZB_Z = [0.19, 0.165, 0.148, 0.148, 0.195, 0.212, 0.205]
k.paint(lambda Q: inside(Q[:, 2] - np.interp(Q[:, 1], ZB_Y, ZB_Z), 0.003), GOLD)
# the chest between the arms
k.paint(lambda Q: inside(Q[:, 1] + 0.095, 0.003) * inside(np.abs(Q[:, 0]) - 0.05, 0.003) * inside(Q[:, 2] - 0.33, 0.003),
        GOLD)
# the gold down the neck's front edge: a quarter of its girth, joining the chest's
FN, FS = spline(NECK, per=40)


def neck_front(Q):
    j = np.concatenate([((Q[a:a + 4000, None, :] - FN[None]) ** 2).sum(2).argmin(1) for a in range(0, len(Q), 4000)])
    r = Q - FN[j]
    c = -r[:, 1] / (np.linalg.norm(r, axis=1) + 1e-9)          # 1 straight forward from the neck's line
    near = inside(np.linalg.norm(r, axis=1) - 0.11, 0.003)
    return (inside(0.83 - c, 0.025) * near * inside(Q[:, 2] - 0.6, 0.003) * inside(0.3 - Q[:, 2], 0.003))


k.paint(neck_front, GOLD)

# --- the head: a small gold skull, a long beak, the magenta cap in the crescent's hollow -----------------------------
k.add("head", ell(HEAD_C, HEAD_R), GOLD, "head")
k.add("beak", tube([on_head(0, -0.215, 0.628), on_head(0, -0.27, 0.612), on_head(0, -0.33, 0.59),
                    on_head(0, -0.375, 0.574)], [0.036, 0.023, 0.012, 0.0035]), GOLD, "head")
# The cap: the top and back of the skull inside the crescent, sunk so only a low dome of magenta stands out
k.add("cap", outward(ell(HEAD_C + np.array([0, 0.042, 0.028]), HEAD_R * np.array([0.95, 0.84, 0.88]))), MAGENTA,
      "head", fuse=False)

# The crescent: a chain of ellipsoids laid along its line, each turned with it, fused into the back of the head
CC, CS = spline(CRESCENT, per=14)
CR = np.interp(CS, np.arange(len(CRESCENT)), CRES_RAD)
CX = np.interp(CS, np.arange(len(CRESCENT)), CRES_X)
T = np.gradient(CC, axis=0)
for i in range(0, len(CC)):
    ang = math.degrees(math.atan2(T[i, 1], T[i, 0]))          # the line's turn in the y, z plane
    step = np.linalg.norm(T[i]) * 1.6
    k.add(f"cres{i:02d}", ell((0, CC[i, 0], CC[i, 1]), (CX[i], max(step, CR[i] * 0.6), CR[i]), rot=(ang, 0, 0), sub=3),
          GOLD, "head")

# --- the arms: a magenta bud at each shoulder with a gold petal folded under it ---------------------------------------
k.add("petal", outward(ell((0.092, -0.128, 0.238), (0.03, 0.046, 0.036), rot=(-25, 0, 8))), GOLD, "body",
      fuse=False, mirror=True)
k.add("bud", outward(ell((0.098, -0.112, 0.27), (0.034, 0.034, 0.032), rot=(-20, 0, 0))), MAGENTA, "body",
      fuse=False, mirror=True)


# --- the wings: loops of pink light ------------------------------------------------------------------------------------
def loop(root, d, across, L, W, h0, h1, thick, share=1.0, cup=0.0, n=110):
    """A ring of a wing: a teardrop loop from root out along d and back, in the plane of d and across, its band
    h0 wide at the root and h0 + h1 at the far end; share narrows it (the pale middle); cup bows it out of its plane."""
    d = unit(*d)
    b = np.array(across, float); b = b - d * (b @ d); b /= np.linalg.norm(b)
    nrm = np.cross(d, b)
    root = np.array(root, float)
    eps = 0.04

    def C2(phi):
        a = L * (1 - np.cos(phi)) / 2
        bb = W * np.sin(phi) * (0.55 + 0.45 * (1 - np.cos(phi)) / 2)
        return a, bb

    def at(u, v):
        phi = eps + (2 * math.pi - 2 * eps) * u
        a, bb = C2(phi)
        a2, b2 = C2(phi + 1e-4)
        ta, tb = (a2 - a), (b2 - bb)
        tl = np.hypot(ta, tb) + 1e-12
        na, nb = tb / tl, -ta / tl
        flip = np.sign(na * (a - L / 2) + nb * bb)
        flip[flip == 0] = 1
        na, nb = na * flip, nb * flip
        far = ((1 - np.cos(phi)) / 2) ** 1.3
        h = (h0 + h1 * far * (1 + 0.35 * np.sin(phi))) * share
        A = a + (2 * v - 1) * h * na
        B = bb + (2 * v - 1) * h * nb
        bow = cup * (A / L) ** 2 * L
        return (root[None, None, :] + A[..., None] * d[None, None, :] + B[..., None] * b[None, None, :]
                + bow[..., None] * nrm[None, None, :])

    return outward(sheet(at, n, 3, thick))


def join(*shapes):
    V, F, off = [], [], 0
    for sV, sF in shapes:
        V.append(sV); F.extend([[i + off for i in f] for f in sF]); off += len(sV)
    return np.vstack(V), F


def wing_pair(s, root, d, across, L, W, h0, h1, cup):
    m = np.array([s, 1, 1.0])
    r, dd, aa = np.array(root) * m, np.array(d) * m, np.array(across) * m
    rim = loop(r, dd, aa, L, W, h0, h1, 0.014, cup=cup * s)
    mid = loop(r + np.array([0, 0, 0.0005]), dd, aa, L, W, h0, h1, 0.024, share=0.5, cup=cup * s)
    return rim, mid


# The great loops, up and back from the shoulders, and the smaller ones dipping out and down from the flanks: in the
# pictures' three-quarter views the lower pair splays to either side of the body (one before it, one behind)
UP = dict(root=(0.045, 0.05, 0.3), d=(0.33, 0.66, 0.68), across=(1, -0.2, 0), L=0.46, W=0.15, h0=0.008, h1=0.05,
          cup=0.06)
DOWN = dict(root=(0.05, 0.03, 0.21), d=(0.68, 0.22, -0.6), across=(1, 0, 1), L=0.24, W=0.095, h0=0.007,
            h1=0.034, cup=0.05)
for s, bone in ((1, "wingR"), (-1, "wingL")):
    a = wing_pair(s, **UP)
    b = wing_pair(s, **DOWN)
    k.add(f"wingrim{bone}", outward(join(a[0], b[0])), DEEP, bone, fuse=False, surface="skin")
    k.add(f"wingmid{bone}", outward(join(a[1], b[1])), PALE, bone, fuse=False, surface="skin")

# --- the mouth's corner: a small blue mark under the eye at the beak's root -------------------------------------------
mp, mn = on_ell(HEAD_C, HEAD_R, (0.62, -0.62, -0.42))
k.decal(mp, mn, (0.013, 0.008), BLUE, eps=0.003, up=(0, 0.3, 1), mirror=True,
        outline=polygon([(-1, 0.6), (1, 0.9), (0.2, -1)]), rings=10, segs=48)

k.build(voxel=0.0068, smooth=4, band=0.006)

# --- the eyes: small pink almonds with a dark upper lid ----------------------------------------------------------------
LID, INK = srgb(GOLD), srgb(INKH)
IRIS, DEEPI, WHITE = srgb("#E0559A"), srgb("#8E2060"), srgb("#FFFFFF")
ALMOND = polygon([(-1, 0.1), (-0.55, 0.62), (0.25, 0.85), (1, 0.45), (0.8, -0.25), (0.1, -0.7), (-0.6, -0.45)])


def eye(u, v):
    c = np.tile(IRIS, (len(u), 1))
    c[((u + 0.05) / 0.36) ** 2 + ((v - 0.0) / 0.5) ** 2 < 1] = DEEPI
    c[((u + 0.18) / 0.2) ** 2 + ((v - 0.25) / 0.22) ** 2 < 1] = WHITE
    c[v > 0.5 - 0.15 * u] = INK
    return c


EP, EN = on_ell(HEAD_C, HEAD_R, (0.6, -0.72, 0.2))


def eyes(state):
    k.decal(EP, EN, (0.036, 0.03), eye_look(state, eye, LID, INK, line=0.2), eps=0.005, up=(0, 0.25, 1),
            outline=ALMOND, rings=20, segs=56, mirror=True)


k.eye_states(eyes)
k.export(out)
