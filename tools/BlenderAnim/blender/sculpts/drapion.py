# Drapion sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/drapion.py -- <the export's Drapion.glb> <out.glb>
#
# Skorupi grown (skorupi.py: the same stacked lens plates, white hooks and crisp ink strokes). From the references: a
# big purple scorpion ogre. A wide head with a huge open mouth, a white tusk hooking down at each corner of it, small
# white eyes with a black pupil under a scowl, a pointed teal-blue mark rising over the outer corner of each eye, a dark
# seam peaking on the brow. A body of stacked lavender and purple bands rising from between the legs to the head. Two
# long segmented arms arching up and out from the shoulders, a dark purple cone on each segment, ending in a dark
# round claw with a teal spot and two white hooks curling inward. A dark segmented tail arching up behind, white
# barbs on its sides, ending in a dark ball with a teal spot and two white hooks. Four lavender legs bent at a raised
# knee with a white spike there, ending in white points.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, boxy, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="shell")

LAV, PURPLE, DARK, PLUM = "#A886CC", "#7C52A2", "#563070", "#46265C"
WHITE, INK, TEAL, MOUTH = "#F2F2F6", "#16142A", "#2EB4D8", "#2E1438"

# ---- the head: wide and a little boxy, its front flattened where the face is
H = np.array([0.0, -0.2, 0.43])
k.add("head", ell(H, (0.15, 0.12, 0.115), deform=boxy(2.4)), LAV, "head")
k.add("face", ell((0, -0.26, 0.42), (0.128, 0.06, 0.095)), LAV, "head")
k.add("jaw", ell((0, -0.22, 0.35), (0.11, 0.08, 0.05)), LAV, "head")
# the mouth: a wide dark opening carved into the lower face
k.add("mouth", ell((0, -0.315, 0.385), (0.075, 0.05, 0.03)), MOUTH, "head", carve=True)

# ---- the body: a dark core, the bands are loose plates stacked round it
k.add("core", tube([(0, 0.1, 0.2), (0, 0.0, 0.25), (0, -0.1, 0.32)], [0.075, 0.072, 0.07]), DARK, "body")
k.add("neck", ell((0, -0.14, 0.36), (0.08, 0.07, 0.06)), DARK, "head")

AX0, AX1 = np.array((0, 0.11, 0.17)), np.array((0, -0.1, 0.33))
d = AX1 - AX0; L = np.linalg.norm(d); d /= L
tilt = math.degrees(math.atan2(d[1], d[2]))
def lens(V):
    V = V.copy()
    V[:, 2] *= 1 - 0.35 * (V[:, 0] ** 2 + V[:, 1] ** 2)
    return V
for i in range(5):
    t = i / 4
    p = AX0 + d * L * t
    r = 0.135 - 0.025 * t
    k.add(f"band{i}", ell(p, (r, r * 0.95, 0.034), rot=(-tilt, 0, 0), deform=lens, sub=3),
          LAV if i % 2 == 0 else PURPLE, "body", fuse=False)

# ---- the legs: lavender, bent at a raised knee with a white spike, ending in white points
for i, (bone, y, dy) in enumerate((("leg{s}", -0.08, -0.05), ("hind{s}", 0.17, 0.05))):
    hip = np.array((0.11, y, 0.19))
    knee = np.array((0.2, y + dy * 0.6, 0.25))
    ankle = np.array((0.25, y + dy, 0.09))
    tip = np.array((0.265, y + dy * 1.15, 0.004))
    k.add(f"leg{i}", tube([hip, (0.14, y + dy * 0.3, 0.245), knee, ankle], [0.044, 0.042, 0.038, 0.028], seg=14),
          LAV, bone, fuse=False, mirror=True)
    k.add(f"knee{i}", ell(knee, (0.042, 0.042, 0.042), sub=3), PURPLE, bone, fuse=False, mirror=True)
    k.add(f"foot{i}", blade([ankle + (0, 0, 0.01), (ankle + tip) / 2, tip], [0.03, 0.019, 0.002], (0, 1, 0), 0.8),
          WHITE, bone, fuse=False, mirror=True)
    k.add(f"kspike{i}", blade([knee + (0.015, 0, 0.015), knee + (0.045, dy * 0.3, 0.05), knee + (0.06, dy * 0.4, 0.085)],
                              [0.018, 0.01, 0.002], (0, 1, 0), 0.6), WHITE, bone, fuse=False, mirror=True)

# ---- the arms: segments arching up and out from the shoulder, a dark cone on each, a round claw at the end
ARM = np.array([(0.1, 0.0, 0.36), (0.16, 0.0, 0.45), (0.23, -0.03, 0.49), (0.29, -0.08, 0.45),
                (0.31, -0.13, 0.37), (0.31, -0.155, 0.335)])
def along(C, t):
    n = len(C) - 1
    i = min(int(t * n), n - 1); f = t * n - i
    p0 = C[max(i - 1, 0)]; p1 = C[i]; p2 = C[i + 1]; p3 = C[min(i + 2, n)]
    return 0.5 * (2 * p1 + (-p0 + p2) * f + (2 * p0 - 5 * p1 + 4 * p2 - p3) * f * f + (-p0 + 3 * p1 - 3 * p2 + p3) * f ** 3)
def tangent(C, t):
    a = along(C, min(t + 0.02, 1)) - along(C, max(t - 0.02, 0))
    return a / np.linalg.norm(a)
def across(p, tdir, r, h):
    """An ellipsoid with radius r across the line tdir and half-thickness h along it."""
    z = np.array(tdir, float)
    x = np.cross((0, 1.0, 0), z) if abs(z[1]) < 0.9 else np.cross((1.0, 0, 0), z)
    x /= np.linalg.norm(x); y = np.cross(z, x)
    V, F = ell((0, 0, 0), (r, r, h), deform=lens, sub=3)
    return V @ np.stack([x, y, z], axis=1).T + p, F

for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    C = ARM * m
    k.add(f"armcore{side}", tube(C, [0.036, 0.035, 0.034, 0.033, 0.03, 0.03], seg=14), DARK, f"arm{side}", fuse=False)
    for i in range(5):
        t = 0.05 + 0.18 * i
        p = along(C, t); td = tangent(C, t)
        k.add(f"seg{i}{side}", across(p, td, 0.056 - 0.004 * i, 0.034), LAV if i % 2 == 0 else PURPLE,
              f"arm{side}", fuse=False)
        if i == 4:
            continue
        # the cone stands on the outer, upper side of the segment
        up = np.array([0.45 * s, 0.1, 1.0]); up -= td * np.dot(up, td); up /= np.linalg.norm(up)
        base = p + up * 0.035
        k.add(f"cone{i}{side}", tube([base, base + up * 0.045 - td * 0.008, base + up * 0.08 - td * 0.02],
                                      [0.03, 0.017, 0.002], seg=12), DARK, f"arm{side}", fuse=False)
    BALL = np.array((0.31, -0.17, 0.31)) * m
    k.add(f"claw{side}", ell(BALL, (0.05, 0.05, 0.052), sub=3), PLUM, f"arm{side}", fuse=False)
    # the teal spot on the outer front of the claw: a thin disc standing proud
    sd = unit(0.75 * s, -0.66, 0.0)
    k.add(f"spot{side}", across(BALL + sd * 0.046, sd, 0.022, 0.008), TEAL, f"arm{side}", fuse=False)
    # two white hooks out of the claw's front, the upper curling down, the lower up: a pincer opening forward
    for j, zz in ((0, 1), (1, -1)):
        b0 = BALL + np.array((0.0, -0.035, 0.03 * zz))
        pts = np.array([b0, b0 + (0.012 * s, -0.045, 0.03 * zz), b0 + (-0.0 * s, -0.085, 0.02 * zz),
                        b0 + (-0.03 * s, -0.1, -0.005 * zz)])
        k.add(f"hook{j}{side}", blade(pts, [0.02, 0.016, 0.009, 0.002], (0, 0, 1), 0.6), WHITE, f"arm{side}",
              fuse=False)

# ---- the tail: dark plates arching up behind, white barbs on their sides, a dark ball with two white hooks
TAIL = np.array([(0, 0.25, 0.25), (0.015, 0.33, 0.28), (0.045, 0.39, 0.34), (0.08, 0.42, 0.41), (0.1, 0.4, 0.46)])
k.add("tailcore", tube(TAIL, [0.04, 0.038, 0.036, 0.034, 0.03], seg=14), DARK, "tail", fuse=False)
for i in range(5):
    t = 0.04 + 0.22 * i
    p = along(TAIL, t); td = tangent(TAIL, t)
    k.add(f"tplate{i}", across(p, td, 0.06 - 0.004 * i, 0.033), DARK if i % 2 == 0 else PLUM, "tail", fuse=False)
    if i == 1:
        for s in (1, -1):
            b0 = p + np.array((0.05 * s, 0, 0))
            k.add(f"barb{i}{'R' if s > 0 else 'L'}",
                  blade([b0, b0 + (0.035 * s, -0.01, 0.02), b0 + (0.05 * s, -0.025, 0.05)], [0.016, 0.009, 0.002],
                        (0, 1, 0), 0.6), WHITE, "tail", fuse=False)
TB = along(TAIL, 1.0) + np.array((0, -0.025, 0.005))
k.add("tailball", ell(TB, (0.05, 0.05, 0.05), sub=3), PLUM, "tail", fuse=False)
k.add("tailspot", across(TB + np.array((0, -0.047, 0.0)), (0, -1, 0), 0.02, 0.008), TEAL, "tail", fuse=False)
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"sting{side}", blade(TB + np.array([(0.035, 0.0, 0.025), (0.065, -0.01, 0.055),
                                               (0.07, -0.03, 0.09), (0.05, -0.05, 0.11)]) * m,
                                [0.019, 0.014, 0.008, 0.002], (0, 1, 0), 0.6), WHITE, "tail", fuse=False)

# ---- the tusks: a big white hook down out of each corner of the mouth (Skorupi's mandibles grown)
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    base = np.array((0.078, -0.3, 0.385))
    k.add(f"tusk{side}", blade(np.array([base, base + (0.014, -0.025, -0.025), base + (0.008, -0.035, -0.06),
                                         base + (-0.014, -0.032, -0.08)]) * m,
                               [0.022, 0.018, 0.011, 0.002], (0, 1, 0.3), 0.55), WHITE, "head", fuse=False)
# three white fangs hanging from the upper rim of the mouth
for i, x in enumerate((-0.035, 0.0, 0.035)):
    b0 = np.array((x, -0.318, 0.405))
    k.add(f"fang{i}", blade([b0, b0 + (0, -0.006, -0.018), b0 + (0, -0.008, -0.03)], [0.012, 0.007, 0.001],
                            (0, 1, 0), 0.6), WHITE, "head", fuse=False)

# ---- ink strokes on the face (Skorupi's way)
CL, CI = srgb(LAV), srgb(INK)
def strokes(centre, n, pts, w, col=INK):
    n = np.array(n, float); n /= np.linalg.norm(n)
    r0 = np.cross((0, 0, 1.0), n); r0 /= np.linalg.norm(r0)
    u0 = np.cross(n, r0)
    for a, b in zip(pts[:-1], pts[1:]):
        a, b = np.array(a), np.array(b)
        dd = b - a
        LL = np.linalg.norm(dd) / 2 + w * 0.6
        dirv = (dd[0] * r0 + dd[1] * u0) / np.linalg.norm(dd)
        mid = np.array(centre) + r0 * (a[0] + b[0]) / 2 + u0 * (a[1] + b[1]) / 2
        k.decal(mid, n, (LL, w), col, up=np.cross(n, dirv), eps=0.004, rings=6, segs=32)

# the seam on the brow: a dark line rising to a peak
strokes((0, -0.27, 0.515), unit(0, -0.7, 0.7), [(-0.03, -0.014), (0, 0.008), (0.03, -0.014)], 0.0048)

# the teal-blue marks: a pointed blade rising over the outer corner of each eye, its point up and outward

for s in (1, -1):
    tip = [(0.0, -0.7), (0.55, -0.55), (0.95, 1.0), (-0.35, -0.2)]
    pts = [(s * a, b) for a, b in tip]
    if s < 0:
        pts = pts[::-1]
    k.decal((0.09 * s, -0.27, 0.5), unit(0.4 * s, -0.85, 0.3), (0.034, 0.036), TEAL, eps=0.005,
            outline=polygon(pts), rings=12, segs=48)

k.build(voxel=0.0065, smooth=3)


# ---- the eyes: small, white, a black pupil toward the nose, the top cut by a scowl sloping down to the nose
def scowl(u):
    return 0.35 + 0.45 * u                    # u outward: lower at the inner corner
CW = srgb(WHITE)
# the eye's outline: the ellipse below the scowl's line, so the patch's own edge is the crisp top line
EYE = []
for th in np.linspace(0, 2 * math.pi, 96, endpoint=False):
    u, v = math.cos(th), math.sin(th)
    if v > scowl(u):
        # along the ray to where it meets the line v = 0.35 + 0.45 u
        t = 0.35 / (v - 0.45 * u)
        u, v = u * t, v * t
    EYE.append((u, v))
EYER = polygon(EYE)
EYEL = polygon([(-a, b) for a, b in EYE[::-1]])
def eye(u, v):
    reach = np.array([EYER(t) for t in np.arctan2(v, u)])
    rho = np.sqrt(u * u + v * v) / reach
    c = np.tile(CW, (len(u), 1))
    c[rho > 0.74] = CI
    return c
E = np.array([0.052, -0.29, 0.462])
de = unit(0.35, -0.9, 0.18)
def eyes(state):
    look = eye_look(state, eye, CL, CI, line=0.16)
    for s in (1, -1):
        mm = np.array([s, 1, 1.0])
        col = look if s == 1 else (lambda a, b, f=look: f(-a, b))
        k.decal(E * mm, de * mm, (0.032, 0.03), col, eps=0.005, rings=20, segs=56, outline=EYER if s == 1 else EYEL)
        if state in ("Open", "Fierce"):
            k.decal(E * mm + np.array([-0.006 * s, -0.002, -0.006]), de * mm, (0.0105, 0.012), "#0C0C14", eps=0.008,
                    rings=6, segs=24)

k.eye_states(eyes)
k.export(out)
