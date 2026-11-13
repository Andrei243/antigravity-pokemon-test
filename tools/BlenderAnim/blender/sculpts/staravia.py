# Staravia sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/staravia.py -- <the export's Staravia.glb> <out.glb>
# Starly grown: the same dark head and grey-brown body, white face and chest, orange beak with a dark tip, but taller,
# with a single crest that rises from the back of the head and curls forward at its tip, and a fan of a tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

BODY, DARK, WHITE, ORANGE, TIP, PATCH = "#8A7F82", "#4A4648", "#F4F4F2", "#F08A2C", "#3A3638", "#8F8488"
TAILW = "#DCE0E4"
H = np.array([0, -0.1, 0.63])          # the head's centre


def in_poly(x, y, pts):
    """Whether points (x, y) lie inside a polygon, by counting crossings."""
    pts = np.array(pts, float)
    res = np.zeros(len(x), bool)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        c = ((y1 > y) != (y2 > y)) & (x < (x2 - x1) * (y - y1) / (y2 - y1 + 1e-12) + x1)
        res ^= c
    return res


def poly_mask(x, y, pts, edge=0.0035):
    """A polygon painted with a soft edge: the signed distance to its outline (negative inside), eased."""
    pts = np.array(pts, float)
    d = np.full(len(x), 9.0)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        ex, ey = x2 - x1, y2 - y1
        t = np.clip(((x - x1) * ex + (y - y1) * ey) / (ex * ex + ey * ey + 1e-12), 0, 1)
        d = np.minimum(d, np.hypot(x - x1 - t * ex, y - y1 - t * ey))
    return inside(np.where(in_poly(x, y, pts), -d, d), edge)


def mirrored(half):
    """A polygon symmetric about a = 0 from its right half (from the top middle round to the bottom middle)."""
    return half + [(-a, z) for a, z in half[::-1][1:-1]]


# A tall egg of a body flowing into the head with no neck to speak of
k.add("body", ell((0, 0.03, 0.32), (0.21, 0.21, 0.22)), BODY, "body")
k.add("neck", ell((0, -0.055, 0.5), (0.14, 0.14, 0.11)), DARK, "head")
k.add("head", ell(H, (0.13, 0.135, 0.135)), DARK, "head")
# The crest: one broad blade rising from the back of the head and curling forward at its tip
k.add("crest", blade([(0, -0.04, 0.7), (0, 0.0, 0.8), (0, 0.0, 0.89), (0, -0.04, 0.95), (0, -0.1, 0.955),
                      (0, -0.125, 0.92), (0, -0.105, 0.89)],
                     [0.075, 0.058, 0.042, 0.032, 0.024, 0.016, 0.006], (1, 0, 0), 0.5), DARK, "head")
# Wings folded to the sides, their tips sweeping back and down
k.add("wing", ell((0.16, 0.08, 0.35), (0.055, 0.17, 0.15), rot=(-22, -12, 0)), DARK, "wing{s}", mirror=True)
k.add("wingtip", ell((0.14, 0.22, 0.25), (0.045, 0.1, 0.055), rot=(-35, -8, 0)), DARK, "wing{s}", mirror=True)

# The dark of the back and shoulders comes down over the body to a zigzag edge; below it the grey-brown
def upper(P):
    th = np.arctan2(P[:, 0], -P[:, 1])
    return inside(0.43 + 0.03 * tri(th * 7 / (2 * math.pi)) - P[:, 2], 0.004)
k.paint(upper, DARK, on=["body"])

# The face: the white mask under the eyes, round the beak's base, ending in points at the sides and below.
# Measured round the head's upright axis: a (radians from the front), z
AX = np.array([0, -0.04])
MASK = mirrored([(0.0, 0.61), (0.16, 0.67), (0.3, 0.715), (0.46, 0.705), (0.6, 0.63), (0.8, 0.56), (0.55, 0.555),
                 (0.44, 0.47), (0.26, 0.53), (0.0, 0.45)])
def face(P):
    a = np.arctan2(P[:, 0], -(P[:, 1] - AX[1]))
    R = 0.15
    return poly_mask(a * R, P[:, 2], [(p * R, z) for p, z in MASK]) * (P[:, 1] < -0.02) * (P[:, 2] > 0.42)
k.paint(face, WHITE)
# The white chest under a dark collar whose lower edge dips in points
CHEST = mirrored([(0.0, 0.405), (0.18, 0.375), (0.34, 0.415), (0.5, 0.385), (0.72, 0.34), (0.7, 0.27), (0.5, 0.2),
                  (0.25, 0.17), (0.0, 0.165)])
def chest(P):
    a = np.arctan2(P[:, 0], -(P[:, 1] - 0.02))
    R = 0.21
    return poly_mask(a * R, P[:, 2], [(p * R, z) for p, z in CHEST]) * (P[:, 1] < 0.0)
k.paint(chest, WHITE)
# The pale grey spot on the forehead, between the crest and the beak
k.decal(H + unit(0, -0.62, 0.78) * 0.135, unit(0, -0.62, 0.78), (0.06, 0.045), PATCH, eps=0.003, rings=10, segs=40)

# The beak: a short orange wedge and a long dark point, a little drooping
def pointed(V):
    t = np.clip(-V[:, 1], 0, 1)
    V = V.copy(); V[:, 0] *= 1 - 0.75 * t; V[:, 2] *= 1 - 0.7 * t
    return V
k.add("beak", ell((0, -0.24, 0.6), (0.05, 0.075, 0.042), rot=(6, 0, 0), deform=pointed), ORANGE, "head", fuse=False,
      surface="shell")
k.add("beaktip", tube([(0, -0.275, 0.596), (0, -0.32, 0.588), (0, -0.365, 0.576)], [0.022, 0.014, 0.004]), TIP, "head",
      fuse=False, surface="shell")

# The tail: a fan of feathers rising behind, pale in the middle and dark at its sides
# Each feather an upright blade (thin across), stacked from the pale one on top down to the dark ones under it,
# splayed a little to the sides so that the fan reads from the front too
for i, (ang, side, length, col) in enumerate([(42, 0.0, 0.31, TAILW), (32, 0.035, 0.29, DARK), (32, -0.035, 0.29, DARK),
                                              (20, 0.05, 0.25, DARK), (20, -0.05, 0.25, DARK), (8, 0.0, 0.2, DARK)]):
    r = math.radians(ang)
    d = np.array([side * 2.5, math.cos(r), math.sin(r)]); d /= np.linalg.norm(d)
    base = np.array([side * 0.5, 0.17, 0.32])
    tip = base + d * length
    k.add(f"tail{i}", blade([base, base + d * length * 0.55, tip], [0.07 if i == 0 else 0.055, 0.065 if i == 0 else 0.05, 0.008], (1, 0, 0), 0.3), col, "tail",
          fuse=False)

# Orange legs with dark feathers at the top, three toes forward and one back, dark claws
CLAW = "#2E2A2C"
for s, side in ((1, "R"), (-1, "L")):
    x = 0.075 * s
    k.add(f"leg{side}", tube([(x, -0.01, 0.2), (x * 1.05, -0.02, 0.1), (x * 1.08, -0.03, 0.035)], [0.022, 0.019, 0.018]),
          ORANGE, f"leg{side}", fuse=False, surface="scales")
    for j, ang in enumerate((-60, -20, 20, 60, 140, 200)):
        rr = math.radians(ang)
        d = np.array([math.sin(rr), -math.cos(rr), 0])
        b0 = np.array([x * 1.03, -0.015, 0.155]) + d * 0.012
        t0 = b0 + d * 0.03 + np.array([0, 0, -0.045])
        k.add(f"fluff{side}{j}", blade([b0, t0], [0.016, 0.003], np.cross(d, (0, 0, 1.0)), 0.4), DARK, f"leg{side}",
              fuse=False)
    base = np.array([x * 1.08, -0.035, 0.025])
    for j, ang in enumerate((-32, 0, 32, 180)):
        rr = math.radians(ang)
        d = np.array([math.sin(rr), -math.cos(rr), 0])
        tip = base + d * (0.075 if ang != 180 else 0.045) + np.array([0, 0, -0.012])
        k.add(f"toe{side}{j}", tube([base, (base + tip) / 2 + np.array([0, 0, 0.004]), tip], [0.015, 0.013, 0.011]),
              ORANGE, f"leg{side}", fuse=False, surface="scales")
        k.add(f"claw{side}{j}", tube([tip, tip + (d + np.array([0, 0, -0.5])) * 0.024], [0.009, 0.0015]), CLAW,
              f"leg{side}", fuse=False, surface="shell")

# The eyes: black with a white glint, in the white mask's top, a dark brow slanting down over them toward the beak
BLACK, LID, CD = srgb("#141214"), srgb(WHITE), srgb(DARK)
def eye(u, v):
    c = np.tile(LID, (len(u), 1))
    c[((u + 0.05) / 0.62) ** 2 + ((v + 0.05) / 0.72) ** 2 < 1] = BLACK
    c[((u - 0.12) / 0.2) ** 2 + ((v - 0.25) / 0.2) ** 2 < 1] = (1, 1, 1)
    return c
def browed(look):
    def f(u, v):
        c = look(u, v)
        c[v > 0.4 + 0.45 * u] = CD
        return c
    return f
de = unit(0.46, -0.87, 0.14)
def eyes(state):
    k.decal(H + de * 0.135, de, (0.034, 0.038), browed(eye_look(state, eye, LID, BLACK)), eps=0.007, rings=20, segs=56,
            mirror=True)

k.build(voxel=0.0065)
k.eye_states(eyes)
k.export(out)
