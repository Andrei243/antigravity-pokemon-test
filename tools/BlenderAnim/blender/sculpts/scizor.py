# Scizor sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/scizor.py -- <the export's Scizor.glb> <out.glb>
# Grown from scyther.py: its frame (the named joints, the thorax, the abdomen and its tilt, the head ball and muzzle,
# the eyes as narrow almond patches laid by `eye_frame`, the wings by `wing`), the same lean; what the pictures change.
#
# From the references: the same mantis in red metal, leaner. A head with three blunt points standing up from its
# crown (the middle one tallest), yellow eyes with a black pupil cut by the brow; a dark grey V from under the eyes
# down the chin and the neck to a point on the chest. A red thorax, a red abdomen with a dark chevron band across
# its top. Thin red arms, each ending in a great round PINCER: two red jaws, the upper the larger, dark between them,
# a yellow ring with a dark middle like an eye on each outer side. One pair of short thin grey wings. Thin red thighs
# and long lean shins tapering to a white claw at the toe, a small spur behind each knee.
# Skeleton: body (0, 0, 0.42), head (0, -0.04, 0.6), arms (+-0.08, -0.03, 0.56), wings (+-0.04, 0.05, 0.54),
# legs (+-0.07, 0.02, 0.3).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="metal")

RED, DARK, CLAW, YELLOW = "#D9323C", "#3C3C46", "#F4F4F0", "#F4C52C"
WING, WINGEDGE, INK, WHITE = "#DCE2EA", "#A4ACBA", "#15171C", "#F8F8F6"

# --- the frame (scyther.py's, on Scizor's joints, leaner) -----------------------------------------------------------------
TH_C, TH_R = np.array([0.0, -0.025, 0.52]), (0.07, 0.062, 0.074)
AB_C, AB_R, AB_TILT = np.array([0.0, 0.02, 0.375]), (0.064, 0.066, 0.086), 22
NECK = [(0, -0.03, 0.57), (0, -0.05, 0.645)]
HJ, HS = np.array([0.0, -0.04, 0.6]), 1.2     # the head's joint, and how much the head is grown about it (scyther.py)


def hs(p):
    """A point of the head drawn at its first size, grown about the head's joint."""
    return HJ + (np.array(p, float) - HJ) * HS


H, HR = hs([0.0, -0.07, 0.69]), np.array([0.064, 0.07, 0.064]) * HS
MUZ_C, MUZ_R = hs([0.0, -0.118, 0.656]), np.array([0.034, 0.04, 0.036]) * HS   # a short pointed chin, no snout
SHOULDER = np.array([0.095, -0.03, 0.555])
ELBOW = np.array([0.14, -0.07, 0.48])
WRIST = np.array([0.175, -0.15, 0.5])
KNEE = np.array([0.11, -0.01, 0.2])
ANKLE = np.array([0.12, -0.03, 0.045])


def ab_axis():
    a = math.radians(AB_TILT)
    return np.array([0.0, -math.sin(a), math.cos(a)])


# --- flat plates (yanmega.py's, as scyther.py) ---------------------------------------------------------------------------
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
            ok = True
            for j in idx:
                if j in (a, b, c):
                    continue
                q = p[j]
                if cross(p[a], p[b], q) >= 0 and cross(p[b], p[c], q) >= 0 and cross(p[c], p[a], q) >= 0:
                    ok = False
                    break
            if ok:
                tris.append((a, b, c)); idx.pop(i); cut = True
                break
        if not cut:
            tris.append((idx[-1], idx[0], idx[1])); idx.pop(0)
    tris.append(tuple(idx))
    return tris


def flat(outline, origin, ex, ez, thick):
    """A flat plate of a polygon (a, b in the plane of ex, ez about origin), wound outward whichever way ex points."""
    ex, ez = unit(*ex), unit(*ez)
    nrm = np.cross(ex, ez); nrm /= np.linalg.norm(nrm)
    p = np.array(outline, float)
    if area(p) < 0:
        p = p[::-1]
    W = np.array(origin, float) + p[:, :1] * ex + p[:, 1:] * ez
    m = len(p)
    front, back = W + nrm * thick / 2, W - nrm * thick / 2
    F = []
    for a, b, c in earclip(p):
        F.append([a, b, c]); F.append([m + c, m + b, m + a])
    rim = []
    for i in range(m):
        j = (i + 1) % m
        rim.extend([front[i], back[i], back[j], front[j]])
        F.append([2 * m + 4 * i + q for q in range(4)])
    return np.vstack([front, back, np.array(rim)]), F


# --- the wings: scyther.py's, one pair, short and grey ---------------------------------------------------------------
def wing_width(t, W):
    return W * np.sqrt(np.clip(1 - ((t - 0.42) / 0.58) ** 2, 0, 1)) * (1.0 - 0.55 * t)


def wing_outline(L, W, t0=0.0, t1=1.0, inset=1.0, n=28):
    t = np.linspace(t0, t1, n)
    a = 0.004 + t * L
    w = np.maximum(wing_width(t, W) * inset, 0.0008)
    return np.vstack([np.stack([a, w], 1), np.stack([a, -w], 1)[::-1]])


def wing_axes(s, sweep, lift, twist):
    sw, li = math.radians(sweep), math.radians(lift)
    ex = np.array([s * math.cos(li) * math.cos(sw), -math.cos(li) * math.sin(sw), math.sin(li)])
    ez = np.array([0.0, 1.0, math.tan(math.radians(twist))])
    ez = ez - ex * np.dot(ez, ex)
    return ex, ez / np.linalg.norm(ez)


def wing(name, root, L, W, sweep, lift, twist, rim=WINGEDGE, fill=WING, thick=0.004):
    for s, side in ((1, "R"), (-1, "L")):
        o = np.array(root, float) * (s, 1, 1)
        ex, ez = wing_axes(s, sweep, lift, twist)
        b = f"wing{side}"
        k.add(f"{name}{side}", flat(wing_outline(L, W), o, ex, ez, thick), rim, b, fuse=False)
        k.add(f"{name}in{side}", flat(wing_outline(L, W, 0.05, 0.97, 0.78), o, ex, ez, thick * 1.6), fill, b,
              fuse=False)


def add_wings():
    wing("wing", (0.03, 0.055, 0.55), 0.17, 0.042, -35, 26, 60)


# --- the body ---------------------------------------------------------------------------------------------------------
def add_body(colour=RED, neck=DARK):
    k.add("thorax", ell(TH_C, TH_R), colour, "body")
    k.add("abdomen", ell(AB_C, AB_R, rot=(AB_TILT, 0, 0)), colour, "body")
    k.add("neck", tube(NECK, [0.027, 0.026]), neck, "head")


def ab_chevron(colour):
    """A dark band across the abdomen's top, dipping to a point in front (the references' V)."""
    ax = ab_axis()
    def m(P):
        a = (P - AB_C) @ ax
        dip = 0.03 * np.clip(1 - np.abs(P[:, 0]) / 0.06, 0, 1) * (P[:, 1] < AB_C[1])
        top = AB_R[2] * 0.82 - dip
        return inside(np.abs(a - (top - 0.026)) - 0.024, 0.003)
    k.paint(m, colour, on=["abdomen"])


def chest_v(colour):
    """The dark V from the neck down the chest's front to a point."""
    def m(P):
        z = P[:, 2]
        half = 0.05 * np.clip((z - 0.46) / 0.1, 0, 1)
        return inside(np.abs(P[:, 0]) - half, 0.003) * (P[:, 1] < TH_C[1] - 0.02) * inside(z - 0.6, 0.004)
    k.paint(m, colour, on=["thorax", "neck"])


# --- the legs: a thin red thigh, a long lean shin tapering to a white claw, a spur behind the knee -------------------
def add_legs(colour=RED, claw=CLAW):
    k.add("thigh", tube([(0.045, 0.025, 0.33), (0.085, 0.01, 0.27), KNEE], [0.026, 0.022, 0.024]), colour,
          "leg{s}", mirror=True)
    mid = KNEE * 0.6 + ANKLE * 0.4 + (0, 0.008, 0)
    k.add("shin", tube([KNEE, mid, ANKLE + (0, 0, 0.01)], [0.03, 0.046, 0.028]), colour, "leg{s}", mirror=True)
    k.add("toe", ell(ANKLE + (0, -0.03, -0.012), (0.03, 0.05, 0.026)), colour, "leg{s}", mirror=True)
    c0 = ANKLE + (0, -0.07, -0.016)
    k.add("claw", blade([c0, c0 + (0, -0.025, -0.008), c0 + (0, -0.04, -0.028)], [0.017, 0.011, 0.0015], (0, 0, 1),
                        0.6), claw, "leg{s}", fuse=False, mirror=True)
    b0 = KNEE + (0.005, 0.02, -0.01)
    k.add("spur", blade([b0, b0 + (0.004, 0.03, -0.01), b0 + (0.006, 0.05, -0.03)], [0.016, 0.01, 0.0015],
                        (1, 0, 0), 0.6), colour, "leg{s}", fuse=False, mirror=True)


# --- the arms: scyther.py's, thinner ------------------------------------------------------------------------------------
def add_arms(colour=RED):
    k.add("shoulder", ell(SHOULDER, (0.04, 0.04, 0.04)), colour, "arm{s}", mirror=True)
    k.add("upper", tube([SHOULDER, (SHOULDER + ELBOW) / 2 + (0.006, 0, 0), ELBOW], [0.024, 0.02, 0.021]), colour,
          "arm{s}", mirror=True)
    k.add("fore", tube([ELBOW, (ELBOW + WRIST) / 2, WRIST], [0.021, 0.022, 0.03]), colour, "arm{s}", mirror=True)


# --- the pincers: two red jaws hinged at the wrist, open forward, dark inside, an eye on each outer side ---------------
PIN_R = 0.074                   # the upper jaw's radius across
PIN_TURN = 12                   # the jaws' opening turned this far outward from straight ahead


def turned(V, c, rot):
    m = np.array(Euler([math.radians(a) for a in rot]).to_matrix())
    return (V - c) @ m.T + c


def disc(c, n, r, thick):
    """A flat round loose disc at c, its face along n."""
    n = np.array(n, float); n /= np.linalg.norm(n)
    a = np.cross(n, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.cross(n, (1.0, 0, 0))
    a /= np.linalg.norm(a); b = np.cross(n, a)
    V, F = ell((0, 0, 0), (r, r, thick), sub=3)
    return V @ np.stack([a, b, n], axis=1).T + np.array(c, float), F


def add_pincers(colour=RED, inner=DARK, ring=YELLOW, mark=INK):
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        bone = f"arm{side}"
        hinge = WRIST * m
        turn = (0, 0, s * PIN_TURN)
        # the upper jaw: a long dome from the hinge forward, its underside flat, its front raised a little
        def dome(V):
            V = V.copy()
            V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.5, V[:, 2])
            return V
        uc = hinge + (0, -0.072, 0.036)
        UR = np.array([PIN_R, 0.096, 0.07])
        V, F = ell(uc, UR, rot=(-16, 0, 0), deform=dome)
        k.add(f"jawU{side}", (turned(V, hinge, turn), F), colour, bone, fuse=False)
        # the lower jaw: smaller, its top flat, its front dropped
        def bowl(V):
            V = V.copy()
            V[:, 2] = np.where(V[:, 2] > 0, V[:, 2] * 0.35, V[:, 2])
            return V
        lc = hinge + (0, -0.052, -0.018)
        V, F = ell(lc, (PIN_R * 0.8, 0.074, 0.034), rot=(20, 0, 0), deform=bowl)
        k.add(f"jawL{side}", (turned(V, hinge, turn), F), colour, bone, fuse=False)
        # dark between the jaws, filling the back so the mouth opens at the front only
        V, F = ell(hinge + (0, -0.05, 0.01), (PIN_R * 0.78, 0.068, 0.03))
        k.add(f"maw{side}", (turned(V, hinge, turn), F), inner, bone, fuse=False)
        # the eye on the upper jaw's outer side, toward the front three-quarter
        d = unit(s * 0.85, -0.35, 0.4)
        p = uc + d / math.sqrt(((d / UR) ** 2).sum())
        nrm = (p - uc) / UR ** 2; nrm /= np.linalg.norm(nrm)
        p = turned(turned(np.array([p]), uc, (-16, 0, 0)), hinge, turn)[0]
        nrm = turned(turned(np.array([nrm]), np.zeros(3), (-16, 0, 0)), np.zeros(3), turn)[0]
        k.add(f"pinring{side}", disc(p + nrm * 0.002, nrm, 0.024, 0.005), ring, bone, fuse=False)
        k.add(f"pinmark{side}", disc(p + nrm * 0.005, nrm, 0.012, 0.005), mark, bone, fuse=False)


# --- the head: scyther.py's ball and a short chin, three blunt points standing up ---------------------------------------
def add_head(colour=RED):
    k.add("head", ell(H, HR), colour, "head")
    k.add("muzzle", ell(MUZ_C, MUZ_R), colour, "head")
    k.add("jaw", ell(H + np.array([0, -0.03, -0.035]) * HS, np.array([0.04, 0.045, 0.03]) * HS), colour, "head")


def add_crest(colour=RED):
    # three short broad blunt points, each about half the head's height: one in the middle, one to each side
    k.add("crest", blade([hs(p) for p in [(0, -0.062, 0.735), (0, -0.056, 0.775), (0, -0.05, 0.808)]],
                         np.array([0.036, 0.028, 0.015]) * HS, (1, 0, 0), 0.55), colour, "head", fuse=False)
    k.add("sidepoint", blade([hs(p) for p in [(0.035, -0.066, 0.725), (0.055, -0.062, 0.758), (0.074, -0.058, 0.786)]],
                             np.array([0.03, 0.023, 0.013]) * HS, unit(0.25, 1, 0), 0.55), colour, "head", fuse=False,
          mirror=True)


def eye_frame(side, d=(0.6, -0.76, 0.24)):
    de = unit(side * d[0], d[1], d[2])
    p = H + de / math.sqrt(((de / HR) ** 2).sum())
    n = (p - H) / HR ** 2
    return p, n / np.linalg.norm(n)


def add_face_v(colour=DARK):
    """The dark V from under the eyes down the chin: a patch pointed below, on the face's front."""
    k.decal(MUZ_C + np.array([0, -0.03, 0.0]) * HS, unit(0, -1, -0.1), (0.034 * HS, 0.05 * HS), colour, eps=0.003, rings=14, segs=48,
            outline=polygon([(-1.0, 0.75), (-0.3, 0.55), (0.0, 0.3), (0.3, 0.55), (1.0, 0.75), (0.0, -1.0)]))


# --- the eyes: scyther.py's almonds, yellow ------------------------------------------------------------------------------
EYE_HALF = (0.031 * HS, 0.021 * HS)
EYE_SHAPE = [(-1.0, -0.15), (-0.55, 0.2), (0.0, 0.55), (0.55, 0.85), (1.0, 0.75), (0.95, 0.1), (0.55, -0.55),
             (0.0, -0.7), (-0.6, -0.5)]


def eye_open(sclera, pupil):
    CW, CK = srgb(sclera), srgb(pupil)
    def eye(u, v):
        c = np.tile(CW, (len(u), 1))
        c[((u + 0.12) / 0.4) ** 2 + ((v + 0.02) / 0.74) ** 2 < 1] = CK
        c[((u + 0.0) / 0.15) ** 2 + ((v - 0.28) / 0.19) ** 2 < 1] = (1, 1, 1)
        return c
    return eye


def add_eyes(state, lid=RED, sclera=YELLOW, pupil=INK):
    look = eye_look(state, eye_open(sclera, pupil), srgb(lid), srgb(INK), line=0.17)
    for side in (1, -1):
        c, n = eye_frame(side)
        col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
        shape = EYE_SHAPE if side == 1 else [(-a, b) for a, b in EYE_SHAPE]
        k.decal(c, n, EYE_HALF, col, eps=0.003, rings=20, segs=56, outline=polygon(shape))


add_body()
ab_chevron(DARK)
chest_v(DARK)
add_legs()
add_arms()
add_head()
add_crest()
add_wings()
add_pincers()
add_face_v()
k.build(voxel=0.0055)
k.eye_states(add_eyes)
k.export(out)
