# Scyther sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/scyther.py -- <the export's Scyther.glb> <out.glb>
# Written to be grown from (scizor.py keeps its frame: the constants and small functions below, the calls at the end).
#
# From the references: a green mantis standing upright and leaning forward. A reptile's head with a muzzle forward, a
# long crest rising up and back from its crown and a short point out of each side behind the eyes (three points);
# narrow fierce eyes, white with a large black pupil, cut by the brow. A short cream neck under the chin; a round green
# thorax with round shoulders; a green abdomen of rounded segments below it, its lower end out behind. Each forearm
# ends in a great curved scythe: a silver-white crescent with a green back along its outer edge. Two pairs of pale
# cream wings on the back. Thin cream stalks from the abdomen to the legs, and big green boots, a spike rising from
# the top of each, three white claws in front and one behind.
# Skeleton: body (0, 0, 0.42), head (0, -0.04, 0.58), arms (+-0.07, -0.04, 0.54), wings (+-0.04, 0.05, 0.52),
# legs (+-0.07, 0.03, 0.3).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="shell")

GREEN, DEEP, CREAM, CLAW = "#6CC24E", "#3E8A32", "#EEE2AE", "#F4F4F0"
STEEL, WING, WINGEDGE, INK, WHITE = "#DDE2EA", "#F3EACB", "#CDB884", "#15171C", "#F8F8F6"

# --- the frame ------------------------------------------------------------------------------------------------------
TH_C, TH_R = np.array([0.0, -0.025, 0.505]), (0.078, 0.066, 0.076)       # the thorax
AB_C, AB_R, AB_TILT = np.array([0.0, 0.02, 0.37]), (0.068, 0.068, 0.088), 22   # the abdomen, its lower end back
NECK = [(0, -0.03, 0.55), (0, -0.05, 0.63)]
HJ, HS = np.array([0.0, -0.04, 0.58]), 1.35   # the head's joint, and how much the head is grown about it


def hs(p):
    """A point of the head drawn at its first size, grown about the head's joint."""
    return HJ + (np.array(p, float) - HJ) * HS


H, HR = hs([0.0, -0.078, 0.678]), np.array([0.07, 0.076, 0.066]) * HS     # the head ball
MUZ_C, MUZ_R = hs([0.0, -0.145, 0.65]), np.array([0.042, 0.052, 0.034]) * HS   # the muzzle
SHOULDER = np.array([0.088, -0.035, 0.535])
ELBOW = np.array([0.14, -0.085, 0.47])
WRIST = np.array([0.165, -0.15, 0.48])
KNEE = np.array([0.115, 0.0, 0.235])
ANKLE = np.array([0.13, -0.025, 0.07])
FOOT = np.array([0.13, -0.045, 0.038])


def ab_axis():
    a = math.radians(AB_TILT)
    return np.array([0.0, -math.sin(a), math.cos(a)])


# --- flat plates (yanmega.py's) -----------------------------------------------------------------------------------------
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


# --- the wings: pale plates in a tan rim, raised in a V and swept back ------------------------------------------------
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
    wing("wingU", (0.03, 0.055, 0.535), 0.25, 0.048, -22, 32, 62)
    wing("wingD", (0.03, 0.06, 0.51), 0.2, 0.04, -50, 14, 55)


# --- the body ---------------------------------------------------------------------------------------------------------
def add_body(colour=GREEN, neck=CREAM):
    k.add("thorax", ell(TH_C, TH_R), colour, "body")
    k.add("abdomen", ell(AB_C, AB_R, rot=(AB_TILT, 0, 0)), colour, "body")
    k.add("neck", tube(NECK, [0.031, 0.029]), neck, "head")


def ab_segments(colour, at=(0.035, -0.02), width=0.0035):
    """Rings round the abdomen across its own axis."""
    ax = ab_axis()
    def m(P):
        a = (P - AB_C) @ ax
        r = np.linalg.norm((P - AB_C) - np.outer(a, ax), axis=1)
        ring = np.zeros(len(P))
        for t in at:
            ring = np.maximum(ring, inside(np.abs(a - t) - width, 0.002))
        return ring * inside(r - 0.08, 0.004)
    k.paint(m, colour, on=["abdomen"])


# --- the legs: a thin cream stalk to a big green boot, a spike on its top, claws -------------------------------------
def add_legs(stalk=CREAM, boot=GREEN, claw=CLAW):
    k.add("stalk", tube([(0.045, 0.03, 0.33), (0.085, 0.02, 0.28), KNEE + (0, 0.004, 0.006)], [0.022, 0.02, 0.022]),
          stalk, "leg{s}", mirror=True)
    # a club: narrow at the knee, swelling round toward the foot
    k.add("boot", tube([KNEE, KNEE * 0.55 + ANKLE * 0.45 + (0, -0.006, 0.0), ANKLE], [0.034, 0.056, 0.064]), boot,
          "leg{s}", mirror=True)
    k.add("calf", ell(KNEE * 0.4 + ANKLE * 0.6 + (0.004, 0.01, 0), (0.062, 0.066, 0.075)), boot, "leg{s}",
          mirror=True)
    k.add("foot", ell(FOOT, (0.062, 0.08, 0.042)), boot, "leg{s}", mirror=True)
    b0 = KNEE + (0.018, 0.012, 0.012)
    k.add("kneespike", blade([b0, b0 + (0.015, 0.006, 0.05), b0 + (0.03, 0.012, 0.1)], [0.03, 0.017, 0.002],
                             (0, 1, 0), 0.5), boot, "leg{s}", fuse=False, mirror=True)
    for j, dx in enumerate((-0.03, 0.0, 0.03)):
        c0 = FOOT + (dx, -0.06, -0.006)
        k.add(f"claw{j}", blade([c0, c0 + (dx * 0.3, -0.03, -0.006), c0 + (dx * 0.5, -0.052, -0.03)],
                                [0.016, 0.011, 0.0015], (0, 0, 1), 0.6), claw, "leg{s}", fuse=False, mirror=True)
    c0 = FOOT + (0.0, 0.065, -0.008)
    k.add("heel", blade([c0, c0 + (0, 0.028, -0.006), c0 + (0, 0.045, -0.028)], [0.014, 0.01, 0.0015], (0, 0, 1), 0.6),
          claw, "leg{s}", fuse=False, mirror=True)


# --- the arms: a round shoulder, an upper arm down and forward, a forearm forward to the wrist -----------------------
def add_arms(colour=GREEN):
    k.add("shoulder", ell(SHOULDER, (0.045, 0.045, 0.045)), colour, "arm{s}", mirror=True)
    k.add("upper", tube([SHOULDER, (SHOULDER + ELBOW) / 2 + (0.006, 0, 0), ELBOW], [0.03, 0.026, 0.026]), colour,
          "arm{s}", mirror=True)
    k.add("fore", tube([ELBOW, (ELBOW + WRIST) / 2, WRIST], [0.026, 0.027, 0.03]), colour, "arm{s}", mirror=True)


# --- the scythes: a flat crescent from the wrist, rising over and down in front, its plane turned out ----------------
SC_E1 = unit(0.82, -0.57, 0)                # forward in the blade's plane, turned out so its face shows
SC_E2 = np.array([0.0, 0.0, 1.0])
SC_R, SC_A0, SC_A1 = 0.14, 145.0, -55.0    # the arc's radius, its start (at the wrist) and its end in degrees


def plate(origin, e1, e2, curve, wf, wb, thick, nu=48, nv=6):
    """absol.py's flat strip: curve(u) its middle line in the plane of e1, e2; wf to the convex side, wb inward."""
    e1, e2 = np.array(e1, float), np.array(e2, float)
    n = np.cross(e1, e2); n /= np.linalg.norm(n)
    origin = np.array(origin, float)

    def at(u, v):
        u = 0.015 + 0.97 * u
        ab = curve(u)
        c = origin + ab[0][..., None] * e1 + ab[1][..., None] * e2
        ab2 = curve(u + 1e-3)
        t = (ab2[0] - ab[0])[..., None] * e1 + (ab2[1] - ab[1])[..., None] * e2
        t /= np.linalg.norm(t, axis=-1, keepdims=True)
        p = np.cross(n, t)
        return c + p * (-wb(u) + v * (wf(u) + wb(u)))[..., None]
    return sheet(at, nu, nv, thick)


def arc_centre():
    a = math.radians(SC_A0)
    return WRIST - SC_R * (math.cos(a) * SC_E1 + math.sin(a) * SC_E2)


def arc(u):
    th = np.radians(SC_A0 + (SC_A1 - SC_A0) * u)
    return np.array([SC_R * np.cos(th), SC_R * np.sin(th)])


def add_scythes(back=GREEN, edge=STEEL):
    C = arc_centre()
    inner = lambda u: 0.003 + 0.105 * np.sin(np.pi * np.clip(0.12 + 0.88 * u, 0, 1)) ** 0.8   # smooth: no knots on the edge
    k.add("scythe", plate(C, SC_E1, SC_E2, arc, lambda u: 0.006 + 0 * u, inner, 0.012), edge, "arm{s}",
          fuse=False, mirror=True, surface="metal")
    spine_o = lambda u: np.interp(u, [0, 0.15, 0.7, 1], [0.016, 0.012, 0.009, 0.0015])
    spine_i = lambda u: np.interp(u, [0, 0.15, 0.7, 1], [0.01, 0.004, 0.003, 0.0005])
    k.add("spine", plate(C, SC_E1, SC_E2, arc, spine_o, spine_i, 0.0145), back, "arm{s}", fuse=False, mirror=True)


# --- the head: a ball, a muzzle forward, the crest and two side points -------------------------------------------------
def add_head(colour=GREEN):
    k.add("head", ell(H, HR), colour, "head")
    k.add("muzzle", ell(MUZ_C, MUZ_R), colour, "head")
    k.add("jaw", ell(H + np.array([0, -0.03, -0.03]) * HS, np.array([0.045, 0.05, 0.03]) * HS), colour, "head")


def add_crest(colour=GREEN):
    # a short broad point from the brow up and back, no longer than the head is tall, with width so the front sees
    # a blade, not a hair
    k.add("crest", blade([hs(p) for p in [(0, -0.078, 0.722), (0, -0.05, 0.752), (0, -0.015, 0.772), (0, 0.018, 0.784)]],
                         np.array([0.03, 0.036, 0.024, 0.002]) * HS, (1, 0, 0), 0.45), colour, "head", fuse=False)
    k.add("cheekpoint", blade([hs(p) for p in [(0.05, -0.05, 0.69), (0.085, -0.03, 0.698), (0.115, -0.005, 0.704)]],
                              np.array([0.024, 0.014, 0.002]) * HS, (0, 0, 1), 0.5), colour, "head", fuse=False,
          mirror=True)


def eye_frame(side, d=(0.62, -0.74, 0.26)):
    de = unit(side * d[0], d[1], d[2])
    p = H + de / math.sqrt(((de / HR) ** 2).sum())
    n = (p - H) / HR ** 2
    return p, n / np.linalg.norm(n)


def add_mouth(skin=GREEN, ink=INK):
    CS, CI = srgb(skin), srgb(ink)
    def mouth(u, v):
        c = np.tile(CS, (len(u), 1))
        c[(np.abs(v - (0.1 - 0.25 * u * u)) < 0.3) & (np.abs(u) < 0.9)] = CI
        return c
    k.decal(MUZ_C + np.array([0, -0.03, -0.022]) * HS, unit(0, -0.8, -0.6), (0.028 * HS, 0.01 * HS), mouth, eps=0.003, rings=10, segs=48)


# --- the eyes: narrow almonds, the brow cutting their tops down toward the muzzle -------------------------------------
EYE_HALF = (0.032 * HS, 0.021 * HS)
EYE_SHAPE = [(-1.0, -0.15), (-0.55, 0.2), (0.0, 0.55), (0.55, 0.85), (1.0, 0.75), (0.95, 0.1), (0.55, -0.55),
             (0.0, -0.7), (-0.6, -0.5)]


def eye_open(sclera, pupil):
    CW, CK = srgb(sclera), srgb(pupil)
    def eye(u, v):
        c = np.tile(CW, (len(u), 1))
        c[((u + 0.12) / 0.48) ** 2 + ((v + 0.02) / 0.78) ** 2 < 1] = CK
        c[((u + 0.0) / 0.16) ** 2 + ((v - 0.28) / 0.2) ** 2 < 1] = (1, 1, 1)
        return c
    return eye


def add_eyes(state, lid=GREEN, sclera=WHITE, pupil=INK):
    look = eye_look(state, eye_open(sclera, pupil), srgb(lid), srgb(INK), line=0.17)
    for side in (1, -1):
        c, n = eye_frame(side)
        col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
        shape = EYE_SHAPE if side == 1 else [(-a, b) for a, b in EYE_SHAPE]
        k.decal(c, n, EYE_HALF, col, eps=0.003, rings=20, segs=56, outline=polygon(shape))


add_body()
ab_segments(DEEP)
add_legs()
add_arms()
add_head()
add_crest()
add_wings()
add_scythes()
add_mouth()
k.build(voxel=0.0055)
k.eye_states(add_eyes)
k.export(out)
