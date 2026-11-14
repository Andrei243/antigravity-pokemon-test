# Skorupi sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/skorupi.py -- <the export's Skorupi.glb> <out.glb>
#
# From the references: a small periwinkle scorpion, low to the ground. A broad domed head that is most of its front,
# a dark seam rising to a peak on its brow; two tall slanted eyes, teal in a dark rim, their tops leaning outward
# (a V), a small black pupil low and toward the nose; a pale scalloped lip and two big white hooked mandibles at the
# lower corners of the face. Its arms are big round balls at its shoulders, higher than the head, each with one white
# horn curving out of its outer side. A dark navy body under and behind the head, four thin pale pointed legs. The
# tail rises behind it in stacked lens-shaped plates, light and dark by turns, to a big dark ball over its back with
# two white horns curving up off its sides.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="shell")

PALE, LILAC, NAVY, PLATE = "#A6B4F0", "#8D9AE2", "#45437E", "#8E9AE4"
WHITE, INK, TEAL = "#F4F4F6", "#16182E", "#1E96D6"

# ---- the head: broad and domed, its front a little flattened where the face is
H = np.array([0.0, -0.085, 0.165])
HR = np.array([0.105, 0.085, 0.08])
k.add("head", ell(H, HR), PALE, "head")
k.add("face", ell((0, -0.12, 0.16), (0.092, 0.06, 0.07)), PALE, "head")

# ---- the body: dark navy, under and behind the head
k.add("body", ell((0, 0.03, 0.125), (0.095, 0.1, 0.07)), NAVY, "body")
k.add("belly", ell((0, -0.05, 0.1), (0.08, 0.06, 0.05)), NAVY, "body")
k.add("rump", ell((0, 0.1, 0.16), (0.06, 0.06, 0.055)), NAVY, "tail")
# the head's underside dark too: the navy of the body comes up under the jaw
k.paint(lambda P: inside(P[:, 2] - (0.098 + 0.02 * np.clip(P[:, 1] + 0.12, 0, 1)), 0.004) * (P[:, 1] > -0.2), NAVY)

# ---- the legs: four thin pale legs bent at a raised knee, ending in points
for i, (bone, y) in enumerate((("leg{s}", -0.05), ("hind{s}", 0.06))):
    hip = np.array((0.07, y, 0.09))
    knee = np.array((0.125, y - 0.02, 0.1))
    tip = np.array((0.15, y - 0.03, 0.003))
    k.add(f"leg{i}", tube([hip, knee, (0.145, y - 0.028, 0.045), tip], [0.022, 0.019, 0.014, 0.003]), PALE, bone,
          fuse=False, mirror=True)

# ---- the arms: a short dark stalk to a big round ball at the shoulder, each with one white horn out of its outer side
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    BALL = np.array((0.14, -0.06, 0.232))
    k.add(f"stalk{side}", tube(np.array([(0.08, -0.04, 0.165), (0.11, -0.05, 0.2)]) * m, [0.028, 0.025]), NAVY,
          f"arm{side}", fuse=False)
    k.add(f"ball{side}", ell(BALL * m, (0.058, 0.055, 0.056)), LILAC, f"arm{side}", fuse=False)
    k.add(f"horn{side}", blade(np.array([BALL + (0.035, -0.03, 0.0), BALL + (0.075, -0.045, 0.002),
                                         BALL + (0.105, -0.05, 0.016), BALL + (0.122, -0.05, 0.038)]) * m,
                               [0.022, 0.016, 0.009, 0.002], (0, 0, 1), 0.65), WHITE, f"arm{side}", fuse=False,
          surface="shell")

# ---- the mandibles: two big white hooks at the lower corners of the face, curving down and in
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    base = np.array((0.068, -0.135, 0.13))
    k.add(f"mandible{side}", blade(np.array([base, base + (0.008, -0.022, -0.018), base + (-0.004, -0.032, -0.038),
                                             base + (-0.024, -0.03, -0.048)]) * m,
                                   [0.019, 0.016, 0.01, 0.002], (0, 1, 0.3), 0.55), WHITE, "head", fuse=False,
          surface="shell")

# ---- the tail: lens-shaped plates stacked up behind the body, light and dark by turns, to a big dark ball
CURVE = np.array([(0, 0.1, 0.17), (0, 0.165, 0.215), (0, 0.195, 0.29), (0, 0.19, 0.37), (0, 0.15, 0.43)])
def along(t):
    n = len(CURVE) - 1
    i = min(int(t * n), n - 1); f = t * n - i
    p0 = CURVE[max(i - 1, 0)]; p1 = CURVE[i]; p2 = CURVE[i + 1]; p3 = CURVE[min(i + 2, n)]
    return 0.5 * (2 * p1 + (-p0 + p2) * f + (2 * p0 - 5 * p1 + 4 * p2 - p3) * f * f + (-p0 + 3 * p1 - 3 * p2 + p3) * f ** 3)
def lens(V):
    V = V.copy()
    V[:, 2] *= 1 - 0.55 * np.abs(V[:, 0]) ** 1.5            # thinner toward its pointed sides
    return V
NP = 5
for i in range(NP):
    t = 0.04 + 0.8 * i / (NP - 1)
    p = along(t)
    d = along(min(t + 0.02, 1)) - along(max(t - 0.02, 0)); d /= np.linalg.norm(d)
    tilt = math.degrees(math.atan2(d[1], d[2]))          # the plate lies across the tail's line
    col = PLATE if i % 2 == 0 else NAVY
    k.add(f"plate{i}", ell(p, (0.072 - 0.003 * i, 0.056, 0.042), rot=(-tilt, 0, 0), deform=lens), col, "tail",
          fuse=False)
TB = along(1.0) + np.array((0, -0.01, 0.015))
k.add("tailball", ell(TB, (0.066, 0.064, 0.064)), NAVY, "tail", fuse=False)
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"sting{side}", blade(np.array([TB + (0.05, 0.0, 0.025), TB + (0.085, 0.0, 0.05), TB + (0.098, 0.0, 0.09),
                                          TB + (0.085, 0.0, 0.122)]) * m,
                                [0.022, 0.017, 0.01, 0.002], (0, 1, 0), 0.6), WHITE, "tail", fuse=False,
          surface="shell")

# ---- the lip: a pale scalloped edge across the lower face, a dark line over it
CP, CI = srgb(PALE), srgb(INK)
def strokes(centre, n, pts, w):
    """A line through pts (right, up offsets in the plane facing n) as one crisp ellipse a stroke."""
    n = np.array(n, float); n /= np.linalg.norm(n)
    r0 = np.cross((0, 0, 1.0), n); r0 /= np.linalg.norm(r0)
    u0 = np.cross(n, r0)
    for a, b in zip(pts[:-1], pts[1:]):
        a, b = np.array(a), np.array(b)
        d = b - a
        L = np.linalg.norm(d) / 2 + w * 0.6
        dirv = (d[0] * r0 + d[1] * u0) / np.linalg.norm(d)
        mid = np.array(centre) + r0 * (a[0] + b[0]) / 2 + u0 * (a[1] + b[1]) / 2
        k.decal(mid, n, (L, w), INK, up=np.cross(n, dirv), eps=0.004, rings=6, segs=32)

# ---- the lip: a dark zigzag across the lower face
strokes((0, -0.165, 0.128), unit(0, -1, -0.2),
        [(-0.048, 0.004), (-0.024, -0.008), (0, 0.004), (0.024, -0.008), (0.048, 0.004)], 0.0042)
# ---- the seam on the brow: a dark line rising to a peak
strokes((0, -0.125, 0.232), unit(0, -0.55, 1), [(-0.026, -0.012), (0, 0.006), (0.026, -0.012)], 0.0038)

k.build(voxel=0.0045, smooth=3)


# ---- the eyes: tall slanted lozenges, their tops leaning outward, teal in a dark rim, a small black pupil low and
# toward the nose. Laid by hand a side, u toward +X on both: the left eye's outline and colour are flipped.
EYE_D = unit(0.62, 0.78, 0)[:2]                           # the eye's long axis in (u, v): its top outward
EYE_N = np.array([EYE_D[1], -EYE_D[0]])
WMAX = 0.45
def halfwidth(s):
    return WMAX * np.clip(1 - s * s, 0, 1) ** 0.55
LOZ = []
for t in np.linspace(-1, 1, 25):
    LOZ.append(tuple(t * EYE_D + halfwidth(t) * EYE_N))
for t in np.linspace(1, -1, 25)[1:-1]:
    LOZ.append(tuple(t * EYE_D - halfwidth(t) * EYE_N))
CT = srgb(TEAL)
LOZR = polygon(LOZ)
def eye(u, v):
    # the rim by the share of the outline's reach at each angle, so it follows the patch's own crisp edge
    reach = np.array([LOZR(t) for t in np.arctan2(v, u)])
    rho = np.sqrt(u * u + v * v) / reach
    c = np.tile(CI, (len(u), 1))
    c[rho < 0.7] = CT
    return c
HE = np.array([0.05, 0.058])
E = np.array([0.047, -0.165, 0.185])
de = unit(0.3, -0.94, 0.15)
def frame(n):
    right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
    return right, np.cross(n, right)
def eyes(state):
    look = eye_look(state, eye, CP, CI, line=0.2)
    for side in (1, -1):
        mm = np.array([side, 1, 1.0])
        n = de * mm
        outline = polygon([(side * a, b) for a, b in (LOZ if side == 1 else LOZ[::-1])])
        col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
        if state in ("Shut", "Smile", "Squeeze"):
            # the closed eye is crisp strokes on the face (u outward on both sides): across a narrow lozenge the
            # patch's own lines came out ragged
            pts = {"Shut": [(-0.03, 0.006), (-0.012, -0.006), (0.012, -0.006), (0.03, 0.006)],
                   "Smile": [(-0.03, -0.006), (-0.012, 0.006), (0.012, 0.006), (0.03, -0.006)],
                   "Squeeze": [(0.028, 0.016), (-0.022, 0.0), (0.028, -0.016)]}[state]
            strokes(E * mm, n, [(side * a, b) for a, b in pts], 0.0045)
            continue
        k.decal(E * mm, n, tuple(HE), col, eps=0.005, outline=outline, rings=20, segs=56)
        if state in ("Open", "Fierce"):
            right, up = frame(n)
            c = E * mm + right * side * (-0.12 * HE[0]) + up * (-0.22 * HE[1])
            k.decal(c, n, (0.0075, 0.012), "#0C0C14", eps=0.008, rings=6, segs=24)

k.eye_states(eyes)
k.export(out)
