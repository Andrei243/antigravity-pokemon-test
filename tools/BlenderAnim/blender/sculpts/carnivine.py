# Carnivine sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/carnivine.py -- <the export's Carnivine.glb> <out.glb>
#
# From the references: a floating flytrap. A big round head, wider than tall, that is one great mouth: a light green
# dome above with darker green spots on its top and a ring of short spines round its crown, the mouth open all round
# its front and sides, red inside, a row of long pale green fangs hanging from the upper lip and short ones rising from
# the lower; a pale yellow-green lower lip narrowing under the head. Two round white eyes with a black dot just over
# the upper lip. A golden yellow round collar under the head with a line round it. From the collar two long dark green
# leaf arms swept out and up, each ending in three pointed lobes, a darker vein down each. Below, instead of legs, a
# tangle of green and red vines twisting down, their tips curling.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="leaf")

DOME, TOP, SPOT, LIP, RED = "#A6D466", "#BCE07C", "#62A84A", "#D2E68A", "#C83A56"
FANG, GOLD, GOLDLINE, LEAF, VEIN = "#E2F2AE", "#E8B238", "#A8781E", "#3E9A3C", "#2A6C2C"
VGREEN, VRED, INK = "#94C658", "#D84466", "#141A12"


def flat(outline, origin, ex, ez, thick):
    """A flat plate of a polygon (a, b in the plane of ex, ez about origin), thick about its middle, sharp edged."""
    ex, ez = unit(*ex), unit(*ez)
    nrm = np.cross(ex, ez); nrm /= np.linalg.norm(nrm)
    p = np.array(outline, float)
    if 0.5 * np.sum(p[:, 0] * np.roll(p[:, 1], -1) - np.roll(p[:, 0], -1) * p[:, 1]) < 0:
        p = p[::-1]
    W = np.array(origin, float) + p[:, :1] * ex + p[:, 1:] * ez
    m = len(p)
    front, back = W + nrm * thick / 2, W - nrm * thick / 2
    idx, tris = list(range(m)), []
    def cross(o, a, b):
        return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])
    while len(idx) > 3:
        for i in range(len(idx)):
            a, b, c = idx[i - 1], idx[i], idx[(i + 1) % len(idx)]
            if cross(p[a], p[b], p[c]) <= 1e-12:
                continue
            if any(cross(p[a], p[b], p[j]) >= 0 and cross(p[b], p[c], p[j]) >= 0 and cross(p[c], p[a], p[j]) >= 0
                   for j in idx if j not in (a, b, c)):
                continue
            tris.append((a, b, c)); idx.pop(i)
            break
        else:
            break
    tris.append(tuple(idx))
    F = []
    for a, b, c in tris:
        F.append([a, b, c]); F.append([m + c, m + b, m + a])
    rim = []
    for i in range(m):
        j = (i + 1) % m
        rim.extend([front[i], back[i], back[j], front[j]])
        F.append([2 * m + 4 * i, 2 * m + 4 * i + 1, 2 * m + 4 * i + 2, 2 * m + 4 * i + 3])
    return np.vstack([front, back, np.array(rim)]), F


# ---- the head: a wide dome over the mouth, a smaller lower lip under it narrowing to the collar
H = np.array([0.0, 0.0, 0.6])
HR = np.array([0.195, 0.165, 0.125])
def dome(V):
    V = V.copy()
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.55, V[:, 2])         # the underside flatter: a lens under a dome
    return V
k.add("head", ell(H, HR, deform=dome), DOME, "body")
LOW = np.array([0.0, -0.005, 0.505])
LOWR = np.array([0.16, 0.135, 0.06])
k.add("lowlip", ell(LOW, LOWR), LIP, "body")
k.add("chin", ell((0, 0.0, 0.46), (0.085, 0.075, 0.05)), LIP, "body")
# the top of the dome a little lighter
k.paint(lambda P: smoothstep(0.66, 0.7, P[:, 2]) * (np.linalg.norm(P[:, :2], axis=1) < 0.2), TOP, on=["head"])

# ---- the collar: a round golden ball under the head with a line round its middle
k.add("collar", ell((0, 0.0, 0.415), (0.062, 0.058, 0.046)), GOLD, "body")
k.paint(lambda P: inside(np.abs(P[:, 2] - 0.413) - 0.0035, 0.002) * (P[:, 2] < 0.44), GOLDLINE, on=["collar"])

# ---- the mouth: one open band carved round the front and sides between the dome and the lower lip, red inside
k.add("mouth", ell((0, -0.09, 0.548), (0.205, 0.15, 0.03)), RED, "body", carve=True)


def rim_at(a, z, scale):
    """A point on the dome's outline at angle a round the head (0 the front, + to +X), at height z."""
    return np.array((HR[0] * scale * math.sin(a), -HR[1] * scale * math.cos(a), z))


# ---- the fangs: long and pale, hanging from the upper lip over the opening; short ones rising from the lower lip
for i, a in enumerate(np.radians([-66, -44, -22, 0, 22, 44, 66])):
    p = rim_at(a, 0.575, 0.9)
    out_ = unit(math.sin(a), -math.cos(a), 0) * 0.012
    L = 0.075 - 0.012 * abs(a)
    k.add(f"fang{i}", blade([p, p + out_ + (0, 0, -L * 0.5), p + out_ * 1.6 + (0, 0, -L)], [0.014, 0.01, 0.002],
                            (math.sin(a), -math.cos(a), 0), 0.55), FANG, "body", fuse=False, surface="shell")
for i, a in enumerate(np.radians([-55, -33, -11, 11, 33, 55])):
    p = rim_at(a, 0.522, 0.78)
    out_ = unit(math.sin(a), -math.cos(a), 0) * 0.01
    k.add(f"tooth{i}", blade([p, p + out_ + (0, 0, 0.016), p + out_ * 1.4 + (0, 0, 0.032)], [0.01, 0.007, 0.002],
                             (math.sin(a), -math.cos(a), 0), 0.55), FANG, "body", fuse=False, surface="shell")

# ---- the spines round the crown: short, pointing out and up
for i in range(9):
    a = 2 * math.pi * i / 9 + math.pi / 9
    d = np.array((math.sin(a), -math.cos(a), 0.0))
    base = H + d * np.array((HR[0], HR[1], 0)) * 0.8 + (0, 0, 0.065)
    tip = base + d * 0.05 + (0, 0, 0.04)
    k.add(f"spine{i}", blade([base - d * 0.01, (base + tip) / 2, tip], [0.022, 0.012, 0.002], (0, 0, 1), 0.6),
          TOP, "body", fuse=False)

# ---- the spots: darker green patches over the top of the dome
for j, (a_deg, el, sz) in enumerate([(0, 0.95, 0.03), (-40, 0.62, 0.026), (40, 0.62, 0.026), (-95, 0.55, 0.022),
                                     (95, 0.55, 0.022), (160, 0.75, 0.026), (-150, 0.75, 0.024), (180, 0.35, 0.02)]):
    a = math.radians(a_deg)
    d = np.array((HR[0] * math.sin(a) * math.cos(el), -HR[1] * math.cos(a) * math.cos(el), HR[2] * math.sin(el)))
    n = unit(d[0] / HR[0] ** 2, d[1] / HR[1] ** 2, d[2] / HR[2] ** 2)
    rs = np.random.RandomState(j)
    shape = [(math.cos(t) * (1 + 0.25 * rs.rand()), math.sin(t) * (0.7 + 0.25 * rs.rand()))
             for t in np.linspace(0, 2 * math.pi, 7, endpoint=False)]
    k.decal(H + d, n, (sz, sz), SPOT, eps=0.004, outline=polygon(shape), rings=8, segs=36,
            up=unit(math.sin(a + 1.0), -math.cos(a + 1.0), 0.3))

# ---- the leaf arms: a short stem from the collar, then a long flat leaf swept out and up ending in three lobes
LEAFP = [(0.0, -0.014), (0.1, -0.03), (0.2, -0.042), (0.27, -0.035), (0.335, 0.0),      # the main tip, outward
         (0.27, 0.035), (0.305, 0.088),                                                   # the second lobe
         (0.235, 0.068), (0.24, 0.135),                                                   # the third, up
         (0.19, 0.068), (0.1, 0.04), (0.0, 0.014)]
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    root = np.array((0.11, -0.012, 0.425))
    k.add(f"stem{side}", tube(np.array([(0.04, 0.0, 0.42), (0.075, -0.006, 0.42), root + (0.015, 0, 0.003)]) * m,
                              [0.016, 0.015, 0.013]), LEAF, f"arm{side}", fuse=False)
    ex = np.array((1.0, -0.12, 0.55)) * m
    ez = np.array((-0.55, -0.1, 1.0)) * m
    V, F = flat(LEAFP, root * m, ex, ez, 0.012)
    k.add(f"leaf{side}", (V, F if s > 0 else [f[::-1] for f in F]), LEAF, f"arm{side}", fuse=False)
    # the veins: a midrib to the main tip and one into each lobe, a little proud of the front face
    exu, ezu = unit(*ex), unit(*ez)
    nrm = np.cross(exu, ezu) * s
    if nrm[1] > 0:
        nrm = -nrm
    def at(a, b):
        return root * m + exu * a + ezu * b + nrm * 0.007
    for name, pts, r in (("rib", [(0.01, 0.0), (0.16, 0.005), (0.31, -0.003)], 0.0055),
                         ("vein1", [(0.21, 0.008), (0.29, 0.07)], 0.004),
                         ("vein2", [(0.17, 0.01), (0.232, 0.115)], 0.004)):
        k.add(f"{name}{side}", tube([at(a, b) for a, b in pts], [r] * (len(pts) - 1) + [0.0015], seg=8, per=6),
              VEIN, f"arm{side}", fuse=False)

# ---- the vines: green and red strands twisting round one another down from under the collar, the tips curling
for j, (ph, col, r0, length, curl) in enumerate([(0.0, VGREEN, 0.017, 0.22, 1), (2.1, VGREEN, 0.016, 0.2, -1),
                                                 (4.2, VGREEN, 0.015, 0.23, 1), (1.0, VRED, 0.011, 0.19, -1),
                                                 (3.2, VRED, 0.011, 0.21, 1)]):
    pts, radii = [], []
    for t in np.linspace(0, 1, 9):
        rr = 0.016 + 0.03 * t
        a = ph + 2.8 * t
        z = 0.39 - length * t
        pts.append((rr * math.cos(a), rr * math.sin(a) * 0.8, z))
        radii.append(r0 * (1 - 0.55 * t))
    # the tip curls out and up
    last = np.array(pts[-1]); d = unit(last[0], last[1], 0)
    pts.append(tuple(last + d * 0.025 + (0, 0, -0.012)))
    pts.append(tuple(last + d * 0.045 + (0, 0, 0.004)))
    radii += [r0 * 0.35, 0.002]
    k.add(f"vine{j}", tube(pts, radii, seg=12, per=6), col, "vines", fuse=False)

k.build(voxel=0.006, smooth=3)


# ---- the eyes: round, white in a black rim, a black dot in the middle, just over the upper lip
CW, CI, CL = srgb("#FAFAF6"), srgb(INK), srgb(DOME)
def eye(u, v):
    r = np.sqrt(u * u + v * v)
    c = np.tile(CW, (len(u), 1))
    c[r > 0.8] = CI
    return c
EA = math.radians(24)
EZ = 0.615
def eyes(state):
    look = eye_look(state, eye, CL, CI, line=0.15)
    for side in (1, -1):
        a = EA * side
        sc = math.sqrt(max(1 - ((EZ - H[2]) / HR[2]) ** 2, 0.1))
        c = rim_at(a, EZ, sc)
        n = unit(c[0] / HR[0] ** 2, c[1] / HR[1] ** 2, (EZ - H[2]) / HR[2] ** 2)
        col = look if side == 1 else (lambda u, v, f=look: f(-u, v))
        k.decal(c, n, (0.03, 0.03), col, eps=0.005, rings=20, segs=56)
        if state in ("Open", "Fierce"):
            k.decal(c, n, (0.0085, 0.0085), INK, eps=0.008, rings=6, segs=24)

k.eye_states(eyes)
k.export(out)
