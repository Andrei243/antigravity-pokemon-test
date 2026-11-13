# Meditite sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/meditite.py -- <the export's Meditite.glb> <out.glb>
# It sits cross-legged in the air, hands held out at its sides, as the game's skeleton does: a great white onion of a
# head with a point on top, a seam down its front and a round knob curled like a shell at each side, its rim coming
# down over the big eyes in a frown; a small open beak of a mouth and pink cheeks; a blue body, white puffed shorts,
# thin blue arms with white cuffs, blue crossed legs and white feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

WHITE, LINE, BLUE, PINK, MOUTH, INK = "#E6E7EE", "#9A9CAE", "#36A6E6", "#F2A2B2", "#C2587C", "#1A1A22"


def near_line(P, pts, n=240):
    """The distance of points to a polyline."""
    pts = np.array(pts, float)
    seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    cum = np.concatenate([[0], np.cumsum(seg)])
    s = np.linspace(0, cum[-1], n)
    line = np.stack([np.interp(s, cum, pts[:, i]) for i in range(3)], axis=1)
    return np.concatenate([np.sqrt(((P[a:a + 4000, None, :] - line[None]) ** 2).sum(2).min(1))
                           for a in range(0, len(P), 4000)])


# The head: a wide bulb, its point drawn up out of the top like an onion's; the face a little set back under its rim
HB = np.array([0, 0.01, 0.535])
k.add("bulb", ell(HB, (0.152, 0.136, 0.125)), WHITE, "head")
k.add("point", tube([(0, 0.012, 0.58), (0, 0.016, 0.65), (0, 0.022, 0.73)], [0.125, 0.07, 0.008]), WHITE, "head")
k.add("face", ell((0, -0.02, 0.445), (0.118, 0.1, 0.085)), WHITE, "head")
# The round knobs at the sides, a little behind the middle
KN = np.array([0.168, 0.02, 0.52])
k.add("knob", ell(KN, (0.06, 0.066, 0.066)), WHITE, "head", mirror=True)

# The seam from the point down the front to the rim between the eyes, a little to its right
def front_y(x, z):
    """The front of the bulb or of the face beneath it, whichever stands further forward."""
    ys = []
    for c, r in ((HB, (0.152, 0.136, 0.125)), ((0, -0.02, 0.445), (0.118, 0.1, 0.085))):
        q = 1 - (x / r[0]) ** 2 - ((z - c[2]) / r[2]) ** 2
        if q > 0:
            ys.append(c[1] - r[1] * math.sqrt(q))
    rp = float(np.interp(z, [0.58, 0.65, 0.73], [0.125, 0.07, 0.008]))       # the point drawn up out of the top
    if rp > abs(x):
        ys.append(0.016 - math.sqrt(rp * rp - x * x))
    return min(ys)
SEAM = [(x, front_y(x, z), z) for x, z in [(0.0, 0.7), (0.006, 0.665), (0.012, 0.63), (0.024, 0.59), (0.028, 0.55), (0.024, 0.51),
                                           (0.012, 0.47), (0.004, 0.43)]]
def seam(P):
    Q = P * np.array([1, 0, 1.0])                 # measured across the front, whatever the depth
    S = np.array(SEAM) * np.array([1, 0, 1.0])
    return inside(near_line(Q, S) - 0.004, 0.0022) * (P[:, 1] < -0.04) * (P[:, 2] > 0.44)
k.paint(seam, LINE)
# The bulb's rim over the face: down from the sides to a point between the eyes, in a frown
def rim(x):
    ax = np.abs(x)
    return np.where(ax < 0.1, 0.416 + 0.64 * ax, 0.48 + 0.25 * (ax - 0.1))
k.paint(lambda P: inside(np.abs(P[:, 2] - rim(P[:, 0]) - 0.002) - 0.0045, 0.0025) * (P[:, 1] < -0.05)
        * (np.abs(P[:, 0]) < 0.135), LINE)

# A short blue body, white puffed shorts
k.add("torso", ell((0, 0.0, 0.325), (0.064, 0.056, 0.065)), BLUE, "body")
k.add("shorts", ell((0, 0.0, 0.24), (0.088, 0.074, 0.054)), WHITE, "body")

# Thin blue arms held out to the sides, a white cuff at each wrist, hands open with the fingers curled up
k.add("arm", tube([(0.055, 0.0, 0.335), (0.13, -0.015, 0.31), (0.195, -0.05, 0.27)], [0.019, 0.017, 0.016]), BLUE,
      "arm{s}", mirror=True)
k.add("cuff", tube([(0.19, -0.046, 0.274), (0.212, -0.06, 0.26)], [0.022, 0.022]), BLUE, "arm{s}", mirror=True)
WR = np.array([0.2, -0.053, 0.267]); WD = unit(0.065, -0.035, -0.04)
def cuff(P):
    Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
    return inside(np.abs((Q - WR) @ WD) - 0.011, 0.002) * (np.linalg.norm(Q - WR, axis=1) < 0.04)
k.paint(cuff, WHITE)
k.add("hand", ell((0.232, -0.072, 0.252), (0.032, 0.03, 0.02)), BLUE, "arm{s}", mirror=True)
for i, (bx, by) in enumerate([(0.25, -0.1), (0.265, -0.085), (0.27, -0.065)]):
    k.add(f"finger{i}", tube([(bx, by, 0.25), (bx + 0.012, by - 0.004, 0.265), (bx + 0.008, by - 0.002, 0.288)],
                             [0.011, 0.01, 0.008]), BLUE, "arm{s}", mirror=True)
k.add("thumb", tube([(0.22, -0.098, 0.255), (0.218, -0.115, 0.27), (0.23, -0.118, 0.285)], [0.011, 0.01, 0.008]),
      BLUE, "arm{s}", mirror=True)

# Crossed legs: thighs out to the knees, shins back in to the middle, the white feet together in front, one above
# the other
for side, s, fz in (("R", 1, 0.095), ("L", -1, 0.13)):
    leg = "leg" + side
    k.add("thigh" + side, tube([(0.05 * s, -0.02, 0.2), (0.12 * s, -0.06, 0.13), (0.16 * s, -0.09, 0.1)],
                               [0.045, 0.042, 0.04]), BLUE, leg)
    k.add("shin" + side, tube([(0.16 * s, -0.09, 0.1), (0.1 * s, -0.14, fz), (0.035 * s, -0.16, fz)],
                              [0.039, 0.035, 0.032]), BLUE, leg)
    k.add("foot" + side, ell((0.02 * s, -0.17, fz + 0.005), (0.042, 0.042, 0.038)), WHITE, leg, fuse=False)

# The knobs' curls: a line winding in to the middle on each outer face
CW, CL = srgb(WHITE), srgb(LINE)
def curl(u, v):
    c = np.tile(CW, (len(u), 1))
    r = np.hypot(u, v)
    a = (np.arctan2(v, -u) + math.pi) / (2 * math.pi)          # 0..1 round, opening toward the front
    want = 0.15 + 0.55 * a
    c[(np.abs(r - want) < 0.075) & (a > 0.12)] = CL
    return c
k.decal(KN + 0.06 * unit(1, -0.5, 0), unit(1, -0.5, 0), (0.048, 0.048), curl, up=(0, 0, 1), eps=0.003, rings=16, segs=64,
        mirror=True)

# Pink cheeks and a small open beak of a mouth, carved into the face
k.decal((0.088, -0.1, 0.405), unit(0.55, -1, -0.1), (0.022, 0.012), PINK, eps=0.003, rings=8, segs=32, mirror=True)
def beak(V):
    V = V.copy()
    t = np.clip((V[:, 2] + 1) / 2, 0, 1)
    V[:, 0] *= 0.35 + 0.75 * t                 # wide on top, narrowing below: a triangle
    return V
k.add("mouth", ell((0, -0.112, 0.392), (0.03, 0.018, 0.02), rot=(-15, 0, 0), deform=beak), MOUTH, "head",
      carve=True)

# The eyes: big, white with a large black pupil and a glint, their tops cut off by the bulb's rim sloping down to the
# nose, so that it frowns
CI, CP = srgb(INK), srgb("#202028")
def top(u):
    return 0.28 + 0.52 * u                     # the bulb's rim over the eye, low toward the nose
def eye(u, v):
    c = np.tile(CW, (len(u), 1))
    e = (u / 0.98) ** 2 + (v / 0.98) ** 2
    under = v < top(u)
    c[under & (e > 0.8)] = CI
    pu = np.hypot((u + 0.02) / 0.56, (v + 0.2) / 0.6)
    c[under & (pu < 1)] = CP
    c[under & (np.hypot((u - 0.16) / 0.17, (v + 0.05) / 0.17) < 1)] = (1, 1, 1)
    c[(np.abs(v - top(u)) < 0.12) & (e < 1.0)] = CI
    return c
de = unit(0.32, -0.94, 0.05)
E = np.array([0.056, -0.11, 0.443])
def eyes(state):
    k.decal(E, de, (0.034, 0.042), eye_look(state, eye, CW, CI), eps=0.004, rings=20, segs=56, mirror=True)


k.build(voxel=0.006)
k.eye_states(eyes)
k.export(out)
