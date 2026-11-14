# Abomasnow sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/abomasnow.py -- <the export's Abomasnow.glb> <out.glb>
# Snover grown: the same snow white, teal of fir boughs and pale eyes in a dark rim. A huge pear of a body under white
# fur, the head one with it; two spiky crowns of snow for brows over pale lilac eyes that scowl; a long moustache of
# flat white strips hanging from under the eyes, an open pink mouth below it, a second fringe of strips round the
# chest; thick white arms held out and bent up to great teal hands like fir boughs, snow round their wrists; short legs
# in teal boughs pointing up from the feet, teal toes; a spiky teal tail low behind and teal boughs up the back.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, smoothstep, tri, unit, eye_look, boxy, args

src, out = args()
k = Kit(src, coat="fur")

SNOW, STRIP, TEAL, MOUTH = "#F2F5F9", "#E0E8F1", "#3A8C87", "#D2626A"

# --- The body: a pear, widest low, the head one with it -----------------------------------------------------------
k.add("low", ell((0, 0.01, 0.27), (0.265, 0.225, 0.235)), SNOW, "body")
k.add("mid", ell((0, 0.0, 0.46), (0.225, 0.19, 0.2)), SNOW, "body")
k.add("head", ell((0, -0.015, 0.655), (0.185, 0.16, 0.15)), SNOW, "head")
k.add("face", ell((0, -0.06, 0.655), (0.155, 0.125, 0.12)), SNOW, "head")

# --- Legs: short and thick, teal boughs pointing up from the feet, teal toes --------------------------------------
for s in (1, -1):
    sd = "R" if s > 0 else "L"
    k.add(f"leg{sd}", ell((0.12 * s, -0.02, 0.11), (0.085, 0.085, 0.1)), SNOW, f"leg{sd}")
    k.add(f"foot{sd}", ell((0.13 * s, -0.05, 0.035), (0.08, 0.095, 0.035)), TEAL, f"leg{sd}")
    for j, a in enumerate((-30, 0, 30)):
        r_ = math.radians(a + 12 * s)
        base = np.array([0.13 * s + math.sin(r_) * 0.04, -0.09, 0.03])
        tip = base + np.array([math.sin(r_) * 0.06, -math.cos(r_) * 0.06, -0.012])
        k.add(f"toe{sd}{j}", blade([base, (base + tip) / 2, tip], [0.03, 0.022, 0.004], (0, 0, 1), 0.5), TEAL,
              f"leg{sd}", fuse=False, surface="leaf")
def around_leg(P, s):
    return np.arctan2(P[:, 0] - 0.12 * s, -(P[:, 1] + 0.02)) / (2 * math.pi)
for s in (1, -1):
    sd = "R" if s > 0 else "L"
    k.paint(lambda P, s=s: inside(P[:, 2] - (0.115 + 0.055 * tri(around_leg(P, s) * 4 + 0.25)), 0.003)
            * (P[:, 0] * s > 0), TEAL, on=[f"leg{sd}", f"foot{sd}", "low"])

# --- Arms: out from the shoulder and a little down, the forearm forward, great teal hands at the shoulder's height -
SH = np.array([0.2, 0.0, 0.6])
k.add("arm", tube([SH - (0.06, 0, 0.0), SH, (0.29, -0.02, 0.555), (0.335, -0.07, 0.53), (0.36, -0.12, 0.54)],
                  [0.075, 0.072, 0.068, 0.066, 0.066]), SNOW, "arm{s}", mirror=True)
HAND = np.array([0.37, -0.14, 0.605])
k.add("hand", ell(HAND, (0.088, 0.075, 0.1)), TEAL, "arm{s}", mirror=True)
# the bough's points: up and out from the hand's top, flat to the front
for i, (dx, dz, ln, r) in enumerate([(-0.05, 0.9, 0.1, 0.04), (0.0, 1.0, 0.115, 0.045), (0.05, 0.85, 0.1, 0.04),
                                     (0.08, 0.55, 0.075, 0.035)]):
    base = HAND + (dx * 0.8, 0, 0.055)
    d = unit(dx * 4 + (0.3 if i == 3 else 0.05), 0, dz)
    k.add(f"bough{i}_", blade([base, base + d * ln * 0.55, base + d * ln], [r, r * 0.75, 0.006], (0, 1, 0), 0.6),
          TEAL, "arm{s}", mirror=True, surface="leaf")
# a thumb inward
k.add("thumb", blade([HAND + (-0.06, -0.01, -0.02), HAND + (-0.1, -0.015, 0.02), HAND + (-0.12, -0.02, 0.07)],
                     [0.035, 0.026, 0.006], (0, 1, 0), 0.6), TEAL, "arm{s}", mirror=True, surface="leaf")
# the snow over the wrist, its jagged edge onto the hand
k.paint(lambda P: inside(P[:, 2] - (HAND[2] - 0.055 + 0.022 * tri(np.arctan2(P[:, 1] - HAND[1], np.abs(P[:, 0])
                                                                                - HAND[0]) * 1.6)), 0.0025)
        * (np.abs(P[:, 0]) > 0.27), SNOW, on=["arm", "hand", "thumb"])

# --- Teal boughs up the back and a spiky tail low behind ------------------------------------------------------
for i, (x, y, z, d, ln, r) in enumerate([(0.1, 0.14, 0.64, (0.45, 0.7, 0.75), 0.17, 0.05),
                                          (0.16, 0.13, 0.54, (0.75, 0.75, 0.35), 0.15, 0.045),
                                          (0.05, 0.17, 0.5, (0.3, 0.9, 0.4), 0.14, 0.045)]):
    base = np.array([x, y, z]); dd = unit(*d)
    k.add(f"back{i}_", blade([base, base + dd * ln * 0.55, base + dd * ln], [r, r * 0.75, 0.006], (0, 0, 1), 0.55),
          TEAL, "body", mirror=True, surface="leaf", fuse=False)
TB = np.array([0.0, 0.2, 0.2])
for i, tip in enumerate(((0.1, 0.36, 0.27), (-0.07, 0.35, 0.29), (0.02, 0.38, 0.16), (0.12, 0.3, 0.13),
                         (0.0, 0.3, 0.36))):
    tip = np.array(tip)
    k.add(f"tail{i}", blade([TB, TB + (tip - TB) * 0.5, tip], [0.05, 0.035, 0.005], (0, 0, 1), 0.6), TEAL, "body", fuse=False,
          surface="leaf")

# --- The face: the mouth (a patch, below the moustache), strips, brows ----------------------------------------------
k.decal((0, -0.18, 0.585), (0, -1, 0.1), (0.052, 0.05), MOUTH, eps=0.004)

# the moustache: two heavy tufts of fur, one hanging from each side of the mouth like a walrus's, wide at the top and
# ending in four points; a rounded pillow of a plate, loose so its points keep their edges, bent round the face
def pillow(outline, yback, thick, rim=0.008):
    """A plate whose outline (x, z) is star-shaped about its mean, rounded from rim at the edge to thick in the
    middle; yback(x, z) is where its back lies, the front standing forward of it (-Y)."""
    pts = np.array(outline, float); mid = pts.mean(axis=0); m = len(pts)
    rings = [1.0, 0.82, 0.6, 0.34]
    V, F = [], []
    def put(xz, t):
        V.append((xz[0], yback(xz[0], xz[1]) - t, xz[1]))
        return len(V) - 1
    front, back = [], []
    for s in rings:
        t = rim + (thick - rim) * math.sqrt(max(0.0, 1 - s * s))
        front.append([put(mid + (p - mid) * s, t) for p in pts])
        back.append([put(mid + (p - mid) * s, 0.0) for p in pts])
    tf = put(mid, thick); tb = put(mid, 0.0)
    for ring in (front, back):
        for r in range(len(rings) - 1):
            for i in range(m):
                j = (i + 1) % m
                q = [ring[r][i], ring[r][j], ring[r + 1][j], ring[r + 1][i]]
                F.append(q if ring is front else q[::-1])
    for i in range(m):
        j = (i + 1) % m
        F.append([front[-1][i], front[-1][j], tf])
        F.append([back[-1][j], back[-1][i], tb])
        F.append([front[0][j], front[0][i], back[0][i], back[0][j]])
    V = np.array(V); F = [list(f) for f in F]
    # faces turn outward: the front's normal must point to -Y
    a, b, c = V[F[0][0]], V[F[0][1]], V[F[0][2]]
    if np.cross(b - a, c - a)[1] > 0:
        F = [f[::-1] for f in F]
    return V, F
TUFT = [(0.004, 0.666), (0.06, 0.674), (0.12, 0.668), (0.168, 0.644), (0.182, 0.59), (0.17, 0.495),
        (0.152, 0.55), (0.134, 0.468), (0.117, 0.537), (0.098, 0.476), (0.082, 0.542), (0.062, 0.498),
        (0.052, 0.58), (0.03, 0.632)]
def tuft_back(x, z):
    return -0.19 + 2.0 * x * x - 0.15 * (0.66 - z)
for s in (1, -1):
    V, F = pillow([(x * s, z) for x, z in TUFT], lambda x, z: tuft_back(x, z), 0.034)
    k.add(f"must{'R' if s > 0 else 'L'}", (V, F), STRIP, "head", fuse=False)

# the chest's fur: one shaggy apron round the front and sides, its edge in a few broad points
def apron(u, v):
    a = np.radians(-118 + 236 * u)
    zb = 0.465 - 0.075 * (1 - np.abs(2 * ((u * 6.5 + 0.5) % 1) - 1)) ** 1.2
    z = 0.565 + (zb - 0.565) * v
    rx = 0.225 * np.sqrt(np.clip(1 - ((z - 0.46) / 0.205) ** 2, 0.25, 1))
    ry = 0.19 * np.sqrt(np.clip(1 - ((z - 0.46) / 0.205) ** 2, 0.25, 1))
    f = 0.96 + 0.13 * v
    return np.stack([rx * f * np.sin(a), -ry * f * np.cos(a), z], -1)
k.add("fringe", sheet(apron, 130, 10, 0.016), STRIP, "body", fuse=False)

# the brows: spiky crowns of snow over each eye, the points up and out
for s in (1, -1):
    sd = "R" if s > 0 else "L"
    B0 = np.array([0.078 * s, -0.135, 0.768])
    k.add(f"brow{sd}", ell(B0, (0.07, 0.045, 0.038), rot=(0, -12 * s, 0)), SNOW, "head", fuse=False)
    for j, (dx, dz, ln) in enumerate([(-0.3, 1.0, 0.085), (0.25, 1.0, 0.11), (0.8, 0.75, 0.1), (1.0, 0.15, 0.085)]):
        base = B0 + np.array([(0.025 * j - 0.03) * s, 0, 0.012])
        d = unit(dx * s, -0.15, dz)
        k.add(f"browpt{sd}{j}", blade([base, base + d * ln * 0.55, base + d * ln], [0.032, 0.022, 0.003], (0, 1, 0),
                                       0.45), SNOW, "head", fuse=False)

# --- The eyes: pale lilac in a dark rim, a small dark pupil, the top cut sloping down to the nose (a scowl) --------
RIM, IRIS, PUP, LID = srgb("#35283F"), srgb("#CDB8E2"), srgb("#3A2A4A"), srgb(SNOW)
def eye(u, v):
    c = np.tile(RIM, (len(u), 1))
    top = 0.4 + 0.35 * u
    c[(u ** 2 + v ** 2 < 0.78 ** 2) & (v < top - 0.2)] = IRIS
    c[((u + 0.05) / 0.25) ** 2 + ((v + 0.14) / 0.27) ** 2 < 1] = PUP
    c[v > top] = LID
    return c
de = unit(0.38, -0.92, 0.08)
def eyes(state):
    k.decal((0.074, -0.166, 0.706), de, (0.047, 0.043), eye_look(state, eye, LID, RIM, line=0.2), eps=0.005, rings=20,
            segs=56, mirror=True)

k.build(voxel=0.0075)
k.eye_states(eyes)
k.export(out)
