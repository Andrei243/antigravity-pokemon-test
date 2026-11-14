# Phione sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Manaphy's script (sculpts/manaphy.py), whose frame it keeps:
#   blender --background --factory-startup --python sculpts/phione.py -- <the export's Phione.glb> <out.glb>
#
# The design, from the references: a small sea sprite of a paler blue than Manaphy, almost all head; the head a drop
# drawn up at the top into a round flat bulb, from whose back ONE long flat ribbon of a feeler arches over and trails
# down and away, ending in two rounded lobes; large eyes, a black pupil in a broad blue ring with a white glint; two
# blue dots on the brow, each joined to its eye by a fine dark line; a small smile; a tiny body under the head that
# narrows to a point, a red oval gem on its chest; long paddle arms. No yellow marks, no ring of dots.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

BLUE, RING, INK, WHITE = "#62C4EE", "#2C84D0", "#141A28", "#FFFFFF"
PUPIL, MOUTH = "#18202E", "#1E3450"
GEM, GEMGLOW = "#E2404A", "#E03A44"
H = np.array([0.0, 0.006, 0.238])          # the head's middle
HR = np.array([0.142, 0.128, 0.138])
BODY = np.array([0.0, 0.004, 0.085])
BR = np.array([0.05, 0.046, 0.068])
CAP = np.array([0.0, 0.014, 0.4])         # the bulb on the head's top
CR = np.array([0.064, 0.06, 0.036])


def onion(V):
    """The upper half drawn in and up to a narrow top, where the bulb sits."""
    V = V.copy()
    up = np.clip(V[:, 2], 0, 1)
    s = 1 - 0.36 * up ** 2
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = V[:, 2] * (1 + 0.14 * up ** 4)
    return V


def on_head(d, lift=0.0):
    """The point of the head's (undeformed) surface along direction d from its middle."""
    d = unit(*d)
    s = 1 / math.sqrt(((d / HR) ** 2).sum())
    return H + d * (s + lift)


# The head, the bulb on its top, and a tiny body under it narrowing to a point
k.add("head", ell(H, HR, deform=onion, sub=5), BLUE, "head")
k.add("cap", ell(CAP, CR, sub=4), BLUE, "head")
def drop(V):
    V = V.copy()
    t = np.clip(-V[:, 2], 0, 1)
    s = 1 - 0.55 * t ** 1.5
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("body", ell(BODY, BR, deform=drop), BLUE, "body")
k.add("neck", ell((0, 0.004, 0.14), (0.064, 0.056, 0.036)), BLUE, "body")

# Two little stubs at the point, close together, so that the foot still reads as one point
k.add("foot", ell((0.018, -0.004, 0.024), (0.017, 0.019, 0.022)), BLUE, "leg{s}", mirror=True)

# Arms: long flat paddles, wider toward the end, carried down and a little forward, their faces to the front
SH = np.array([0.05, -0.01, 0.15])
AD = unit(0.78, -0.3, -0.56)
AL = 0.088
def paddle(V):
    V = V.copy()
    t = (V[:, 0] + 1) / 2
    V[:, 2] *= 0.5 + 0.62 * t
    return V
yaw = math.degrees(math.atan2(AD[1], AD[0]))
droop = math.degrees(math.asin(-AD[2]))
k.add("arm", ell(SH + AD * (AL - 0.004), (AL, 0.012, 0.04), rot=(0, droop, yaw), deform=paddle), BLUE, "arm{s}",
      mirror=True)

# The feeler: a root out of the bulb's back, then a flat ribbon in one upright plane that runs back and some 35
# degrees out to its right (+X), so its face shows to the game's cameras; it arches over and trails down to two lobes
HD = unit(0.57, 0.82, 0)                  # the ribbon's plane: across it HD, up z
FO = CAP + np.array([0.0, 0.03, 0.022])   # where it leaves the bulb
k.add("feelerroot", tube([CAP + (0, 0.01, 0.0), FO, FO + HD * 0.02 + (0, 0, 0.022)], [0.03, 0.022, 0.016]),
      BLUE, "head")
# Its centre line in (along HD, up) from FO, with each point's half width
FL = np.array([(0.0, 0.0), (0.02, 0.022), (0.05, 0.036), (0.09, 0.034), (0.13, 0.016), (0.18, -0.02),
               (0.23, -0.07), (0.275, -0.125), (0.31, -0.18)])
FW = np.array([0.012, 0.014, 0.017, 0.021, 0.026, 0.031, 0.036, 0.041, 0.045])


def ribbon():
    """The ribbon's outline: one edge out to the tip, two round lobes with a notch between, the other edge back."""
    # Points along the centre line, finely, with their normals in the plane
    t = np.linspace(0, 1, 60)
    i = np.arange(len(FL))
    x = np.interp(t * (len(FL) - 1), i, FL[:, 0]); y = np.interp(t * (len(FL) - 1), i, FL[:, 1])
    # a little smoothing of the polyline
    for _ in range(3):
        x[1:-1] = (x[:-2] + 2 * x[1:-1] + x[2:]) / 4; y[1:-1] = (y[:-2] + 2 * y[1:-1] + y[2:]) / 4
    w = np.interp(t * (len(FW) - 1), i, FW)
    tx, ty = np.gradient(x), np.gradient(y)
    L = np.hypot(tx, ty); tx /= L; ty /= L
    nx, ny = -ty, tx
    left = np.stack([x + nx * w, y + ny * w], 1)
    right = np.stack([x - nx * w, y - ny * w], 1)
    # The tip: two lobes, each a half disc, the notch between them on the centre line
    end = np.array([x[-1], y[-1]]); T = np.array([tx[-1], ty[-1]]); N = np.array([nx[-1], ny[-1]])
    we = w[-1]
    tip = []
    for side in (1, -1):
        c = end + N * side * we * 0.5 + T * 0.004
        a0 = math.atan2(*(N * side)[::-1])
        a1 = math.atan2(*(-N * side)[::-1])
        # the first lobe from its outer edge over the far end to the notch, the second from the notch to its edge
        for s in (np.linspace(0, 1, 9) if side == 1 else np.linspace(1, 0, 9)):
            ang = a0 - side * s * math.pi
            r = we * 0.55
            tip.append(c + r * np.array([math.cos(ang), math.sin(ang)]))
        if side == 1:
            tip.append(end + T * 0.0)            # the notch
    return np.vstack([left, np.array(tip), right[::-1]])

k.add("feeler", slab(ribbon(), FO, HD, (0, 0, 1.0), 0.011), BLUE, "head", fuse=False)

# The red gem on the chest, loose so its edge stays crisp
k.add("gem", ell((0.0, -0.043, 0.078), (0.012, 0.008, 0.019), rot=(-8, 0, 0), sub=3), GEM, "body", fuse=False,
      glow=GEMGLOW)

# The eyes' places, and over them, nearer the middle, two blue dots joined to the eyes by fine dark lines
de = unit(0.58, -0.78, -0.24)
E = on_head(de)
EH = (0.031, 0.037)
DOT = unit(0.3, -0.9, 0.2)
k.decal(on_head(DOT), DOT, (0.022, 0.022), RING, eps=0.004, rings=10, segs=40, mirror=True)
EYETOP = on_head(unit(0.52, -0.8, -0.04))
STALKS = [(on_head(DOT), EYETOP)]


def stalks(P):
    Q = np.stack([np.abs(P[:, 0]), P[:, 1], P[:, 2]], axis=1)
    best = np.full(len(P), 9.0)
    for a, b in STALKS:
        ab = b - a
        t = np.clip(((Q - a) @ ab) / (ab @ ab), 0, 1)
        best = np.minimum(best, np.linalg.norm(Q - (a + t[:, None] * ab), axis=1))
    return inside(best - 0.0034, 0.0014) * (P[:, 1] < -0.02)
k.paint(stalks, INK, on=["head"])

# A small smile, a little to one side as in the pictures
CB, CMO = srgb(BLUE), srgb(MOUTH)
def smile(u, v):
    c = np.tile(CB, (len(u), 1))
    c[(np.abs(v - (-0.35 + 0.85 * u * u)) < 0.3) & (np.abs(u) < 0.9)] = CMO
    return c
dm = unit(0.04, -1, -0.5)
k.decal(on_head(dm), dm, (0.024, 0.014), smile, eps=0.003, rings=8, segs=40)

k.build(voxel=0.0045)

# Large eyes: a black pupil with a glint in a broad blue ring; the ring stays whatever the eye does
CR_, CI, CW, CP = srgb(RING), srgb(INK), srgb(WHITE), srgb(PUPIL)
def eye(u, v):
    c = np.tile(CP, (len(u), 1))
    c[((u - 0.18) / 0.24) ** 2 + ((v - 0.3) / 0.26) ** 2 < 1] = CW
    return c
def ringed(state):
    look = eye_look(state, eye, CB, CI, line=0.18)
    def f(u, v):
        c = look(u, v)
        r = np.sqrt(u * u + v * v)
        c[r > 0.62] = CR_
        return c
    return f
def eyes(state):
    k.decal(E, de, EH, ringed(state), eps=0.004, rings=20, segs=56, mirror=True)

k.eye_states(eyes)
k.export(out)
