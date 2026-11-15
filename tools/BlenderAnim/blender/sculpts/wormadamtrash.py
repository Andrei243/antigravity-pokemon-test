# Wormadam (Trash Cloak) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/wormadamtrash.py -- <the export's Wormadam-Trash.glb> <out.glb>
#
# Grown from Wormadam's script (wormadam.py), whose head, stalk, knot, spear, nose and eyes are kept as they are made
# there, moved down onto this form's own head joint (0.552 where the Plant Cloak's is 0.584), and from burmytrash.py's
# pink insulation. The design, from the references: the same grey bagworm peeking out of a puff of pink insulation,
# soft and rounded, its underside creased, gathered in over a narrower bag below that is tied off in a small tassel;
# a magenta-pink nose; two small magenta bows on the crown where the Plant Cloak has its little leaves; and two long
# magenta ribbons streaming out from behind the head like a scarf, the -X end lifted, the +X end hanging (on armL and
# armR), each split into three tails at its end.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, unit, srgb, inside, smoothstep, eye_look, args

# --- the cloak: everything a form changes -------------------------------------------------------------------------
CLOAK = dict(
    coat="fur",
    puff="#F4A8BA", puff2="#EE9DB1",               # the insulation (burmytrash.py's pinks, the puff a shade lighter)
    bag="#E98CA3", crease="#C9758D",
    ribbon="#E2407F", nose="#DE4A88", bow="#E64F8A",
)

src, out = args()
k = Kit(src, coat=CLOAK["coat"])
GREY, GREY2 = "#5D5F67", "#686A73"            # Burmy's head and horn
rs = np.random.RandomState(4132)
DZ = 0.552 - 0.584                            # this form's head joint against the Plant Cloak's

# --- the head (wormadam.py's, moved down by DZ) --------------------------------------------------------------------
H = np.array([0.0, -0.06, 0.6 + DZ])
HR = np.array([0.105, 0.09, 0.085])
k.add("head", ell(H, HR), GREY, "head", surface="shell")

def up(p):
    return (p[0], p[1], p[2] + DZ)

k.add("stalk", tube([up((0, -0.045, 0.66)), up((0, -0.035, 0.72)), up((0, -0.03, 0.78))], [0.03, 0.017, 0.016]),
      GREY, "head", surface="shell")
ring = []
for i in range(25):
    t = 2 * math.pi * i / 24
    p = np.array([0.024 * math.cos(t), 0.024 * math.sin(t), 0.0])
    tilt = math.radians(32)
    p = np.array([p[0] * math.cos(tilt), p[1], p[0] * math.sin(tilt)])
    ring.append(p + np.array(up((0, -0.03, 0.8))))
k.add("knot0", tube(ring, [0.0105] * 25, seg=12), GREY2, "head", surface="shell")
k.add("knot1", ell(up((0, -0.03, 0.8)), (0.02, 0.02, 0.026)), GREY, "head", surface="shell")
k.add("neck2", tube([up((0, -0.03, 0.78)), up((0, -0.03, 0.83))], [0.016, 0.017]), GREY, "head", surface="shell")
k.add("spear", tube([up((0, -0.03, 0.825)), up((0, -0.029, 0.87)), up((0, -0.027, 0.915)), up((0, -0.026, 0.94))],
                    [0.018, 0.018, 0.009, 0.001], seg=20), GREY, "head", fuse=False, surface="shell")

def cone(V):
    V = V.copy()
    t = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.8 * t; V[:, 1] *= 1 - 0.8 * t
    return V
k.add("nose", ell(up((0, -0.128, 0.505)), (0.03, 0.022, 0.066), rot=(-18, 0, 0), deform=cone), CLOAK["nose"],
      "head", fuse=False, surface="cloth")

# Two small bows on the crown: rounded lobes faced to the front, leaning out
def lobe(n=14):
    return [(0.5 * math.sin(t) * (1 - 0.35 * math.cos(t)), 0.5 - 0.5 * math.cos(t))
            for t in np.linspace(0, 2 * math.pi, n, endpoint=False)]
for s in (1, -1):
    base = np.array(up((0.07 * s, -0.035, 0.665)))
    ez = unit(0.55 * s, 0.05, 1.0)
    ex = unit(1.0 * s, 0.0, -0.55 * s * s)
    pts = [(a * 0.055, b * 0.06) for a, b in lobe()]
    k.add(f"ear{'R' if s > 0 else 'L'}", slab(pts, base, ex, ez, 0.018), CLOAK["bow"], "head", fuse=False,
          surface="cloth")

# --- the puff: a rounded wad of insulation round the head's chin and back -----------------------------------------
PC = np.array([0.0, 0.015, 0.455])
PR = np.array([0.19, 0.145, 0.095])
def under_chin(V):
    """The puff's top front falls back a little, so the head and its nose stand before it."""
    V = V.copy()
    front = np.clip(-V[:, 1], 0, 1) * (1 - np.clip(np.abs(V[:, 0]) / 0.7, 0, 1) ** 2)
    V[:, 1] *= 1 - 0.45 * smoothstep(0.2, 0.8, V[:, 2]) * front
    return V
k.add("puff", ell(PC, PR, deform=under_chin), CLOAK["puff"], "body")
k.add("hood", ell((0, 0.05, 0.54 + DZ), (0.15, 0.11, 0.085)), CLOAK["puff"], "body")
# Soft lumps round its rim, and a few over its top behind the head
n = 0
for row, (z, rr, count, size) in enumerate(((0.425, 0.175, 12, 0.068), (0.49, 0.14, 9, 0.06))):
    for j in range(count):
        a = 2 * math.pi * (j + 0.5 * row) / count + rs.uniform(-0.08, 0.08)
        a = math.atan2(math.sin(a), math.cos(a))
        if row == 1 and abs(a) < 1.3:
            continue                                   # the head is there
        p = np.array([rr * math.sin(a), PC[1] - rr * 0.78 * math.cos(a), z + rs.uniform(-0.008, 0.008)])
        if row == 0 and abs(a) < 0.5:
            p[2] -= 0.02                               # under the chin
        sz = size * rs.uniform(0.9, 1.1)
        k.add(f"lump{n}", ell(p, (sz, sz, sz * 0.8)), CLOAK["puff2"] if n % 4 == 0 else CLOAK["puff"], "body")
        n += 1

# --- the bag below, gathered in under the puff and tied off -------------------------------------------------------
PROFILE = [(0.15, 0.045), (0.19, 0.08), (0.25, 0.11), (0.31, 0.128), (0.36, 0.13), (0.4, 0.115)]
ZS = np.linspace(PROFILE[0][0], PROFILE[-1][0], 32)
RS = np.interp(ZS, [p[0] for p in PROFILE], [p[1] for p in PROFILE])
for _ in range(4):
    RS[1:-1] = 0.25 * RS[:-2] + 0.5 * RS[1:-1] + 0.25 * RS[2:]
V, F = tube([(0, 0.01, z) for z in ZS], RS, seg=40, per=2)
V[:, 1] = 0.01 + (V[:, 1] - 0.01) * 0.85
k.add("bag", (V, F), CLOAK["bag"], "body")
k.add("tie", ell((0, 0.01, 0.145), (0.042, 0.04, 0.03)), CLOAK["bag"], "body")
# The tassel: five short lobes flaring down out of the tie
for i in range(5):
    a = 2 * math.pi * (i + 0.5) / 5
    o = np.array([math.sin(a), -math.cos(a), 0.0])
    k.add(f"tassel{i}", ell((0.026 * o[0], 0.01 + 0.026 * o[1], 0.118), (0.018, 0.011, 0.032),
                            rot=(-28 * o[1], 28 * o[0], math.degrees(a))), CLOAK["bag"], "body", fuse=False,
          surface="cloth")

# Creases: fine dark lines down the puff's underside, where it gathers in over the bag
def creases(P):
    a = np.arctan2(P[:, 0], -(P[:, 1] - PC[1]))
    lines = np.abs(a * 22 / (2 * math.pi) - np.round(a * 22 / (2 * math.pi)))
    band = inside(P[:, 2] - 0.41, 0.004) * inside(0.37 - P[:, 2], 0.004)
    reach = inside(0.13 - np.hypot(P[:, 0], P[:, 1] - PC[1]), 0.004)
    return inside(lines - 0.07, 0.02) * band * (1 - reach)
k.paint(creases, CLOAK["crease"])

# --- the ribbons: long flat bands streaming out from behind the head, split in three at their ends ----------------
def ribbon(centre, width, notch=0.05):
    """An outline of a band along a 2D centre line (a list of points), as wide as width(t), its end cut in three."""
    C = np.array(centre, float)
    n = len(C)
    T = np.gradient(C, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True)
    N = np.stack([-T[:, 1], T[:, 0]], axis=1)
    W = np.array([width(i / (n - 1)) for i in range(n)])
    left = C + N * W[:, None] / 2
    right = C - N * W[:, None] / 2
    end, t, nn = C[-1], T[-1], N[-1]
    w = W[-1] / 2
    cut = [end + nn * w * 0.5 - t * notch, end + t * 0.01, end - nn * w * 0.5 - t * notch]
    return list(left) + [cut[0], cut[1], cut[2]] + list(right[::-1])

def smooth_line(pts, m=24):
    pts = np.array(pts, float)
    t = np.linspace(0, 1, len(pts))
    u = np.linspace(0, 1, m)
    from_ = np.stack([np.interp(u, t, pts[:, 0]), np.interp(u, t, pts[:, 1])], axis=1)
    for _ in range(4):
        from_[1:-1] = 0.25 * from_[:-2] + 0.5 * from_[1:-1] + 0.25 * from_[2:]
    return from_

for s in (1, -1):
    side = "R" if s > 0 else "L"
    root = np.array((0.07 * s, 0.035, 0.55))
    ex = unit(1.0 * s, 0.5, 0.0)                       # out to the side and some 27 degrees back
    ez = np.array([0, 0, 1.0])
    if s < 0:                                          # -X: lifted, then streaming out
        line = smooth_line([(0, 0), (0.08, 0.05), (0.17, 0.12), (0.25, 0.14), (0.31, 0.12)])
    else:                                              # +X: hanging down and out
        line = smooth_line([(0, 0), (0.09, -0.02), (0.18, -0.08), (0.24, -0.16), (0.27, -0.22)])
    k.add(f"ribbon{side}", slab(ribbon(line, lambda t: 0.06 + 0.02 * t), root, ex, ez, 0.018), CLOAK["ribbon"],
          f"arm{side}", fuse=False, surface="cloth")

# --- the eyes: wormadam.py's, white with an upright black pupil and a dark rim ---------------------------------------
EYEW, PUPIL, INK = srgb("#F6F6F2"), srgb("#141416"), srgb("#141416")
RIM = srgb("#26262C")
def eye(u, v):
    c = np.tile(EYEW, (len(u), 1))
    c[(u / 0.36) ** 2 + (v / 0.58) ** 2 < 1] = PUPIL
    c[u * u + v * v > 0.69] = RIM
    return c
LID = srgb(GREY)

def on_head(d):
    d = np.array(d, float); d /= np.linalg.norm(d)
    p = H + d / math.sqrt(((d / HR) ** 2).sum())
    nrm = (p - H) / HR ** 2
    return p, nrm / np.linalg.norm(nrm)

EP, EN = on_head((0.62, -0.72, -0.42))
def eyes(state):
    k.decal(EP, EN, (0.025, 0.034), eye_look(state, eye, LID, INK), eps=0.004, rings=20, segs=56, mirror=True)

k.build(voxel=0.0065, smooth=7, band=0.007)
k.eye_states(eyes)
k.export(out)
