# Munchlax sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/munchlax.py -- <the export's Munchlax.glb> <out.glb>
#
# The design, from the references: a dark teal body like a pear in a shaggy coat, ragged at its hem; a big wide head
# with two tall pointed ears; the lower face cream, wrapped round the jaw from cheek to cheek; a wide open grin along
# the cream's top line, pink inside, with two small white teeth standing up from its lower lip; round white eyes with a
# small black pupil, high and apart; a round cream patch on the chest under the chin; short stubby arms out to the
# sides ending in ragged points of fur; cream feet showing under the hem, a brown round pad on each and three white
# claws over it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

TEAL, CREAM, PAD, INK, WHITE = "#3B7F8C", "#F2E2A0", "#7A5232", "#18181E", "#FFFFFF"
MOUTH = "#D2606E"
H = np.array([0, -0.005, 0.49])           # the head's middle
HR = np.array([0.158, 0.14, 0.16])


def hit(shape, o, d):
    """Where a ray from o along d first meets a part's own mesh: places on a part before the body is fused."""
    V, F = shape
    h = BVHTree.FromPolygons(V.tolist(), F).ray_cast(Vector(o), Vector(unit(*d)))
    return np.array(h[0])


def in_poly(x, y, pts):
    pts = np.array(pts, float)
    res = np.zeros(len(x), bool)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        res ^= ((y1 > y) != (y2 > y)) & (x < (x2 - x1) * (y - y1) / (y2 - y1 + 1e-12) + x1)
    return res


def poly_mask(x, y, pts, edge=0.003):
    """A polygon painted with a soft edge: the signed distance to its outline (negative inside), eased."""
    pts = np.array(pts, float)
    d = np.full(len(x), 9.0)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        ex, ey = x2 - x1, y2 - y1
        t = np.clip(((x - x1) * ex + (y - y1) * ey) / (ex * ex + ey * ey + 1e-12), 0, 1)
        d = np.minimum(d, np.hypot(x - x1 - t * ex, y - y1 - t * ey))
    return inside(np.where(in_poly(x, y, pts), -d, d), edge)


def mirrored(half):
    return half + [(-a, z) for a, z in half[::-1][1:-1]]


def round_head(P):
    """(the arc round the head's upright axis, z): the coordinates the face's paint is measured in."""
    return np.arctan2(P[:, 0], -(P[:, 1] - H[1])) * HR[0], P[:, 2]


def head_shape(V):
    """A wide head, squared a little, fullest at the cheeks and rounder over the crown."""
    V = boxy(2.4)(V)
    s = 1 + 0.06 * np.clip(-V[:, 2], 0, 1) - 0.1 * np.clip(V[:, 2], 0, 1) ** 2
    V[:, 0] *= s
    return V


def pear(V):
    """The body: narrower at the top under the head, full below."""
    s = 1 - 0.2 * np.clip(V[:, 2], 0, 1) + 0.04 * np.clip(-V[:, 2], 0, 1)
    V = V.copy(); V[:, 0] *= s; V[:, 1] *= s
    return V


# The body like a pear, its hem ragged in points of fur that splay out and down
BODY = ell((0, 0.005, 0.2), (0.185, 0.16, 0.168), deform=pear)
k.add("body", BODY, TEAL, "body")
for i in range(18):
    a = 2 * math.pi * (i + 0.5) / 18
    d = np.array([math.sin(a), -math.cos(a), 0])
    if a < 0.8 or a > 2 * math.pi - 0.8:
        continue              # none over the feet, so they show clean under the coat's front
    reach = 0.05 + 0.012 * (i % 3)
    root = np.array([0, 0.005, 0.11]) + d * np.array([0.16, 0.135, 0])
    tip = np.array([0, 0.005, 0.03 + 0.012 * (i % 2)]) + d * np.array([0.16 + reach, 0.135 + reach, 0])
    k.add(f"hem{i}", tube([root, (root + tip) / 2 + (0, 0, 0.005), tip], [0.034, 0.02, 0.004]), TEAL, "body")

# The head, wide, its lower front a little fuller for the jaw
HEAD = ell(H, HR, deform=head_shape)
k.add("head", HEAD, TEAL, "head")

# Two tall pointed ears on the crown, wide at the root, leaning a little out
EB = np.array([0.07, 0.008, 0.59])
k.add("ear", blade([EB, (0.073, 0.004, 0.655), (0.072, 0.0, 0.715), (0.07, -0.002, 0.742)],
                   [0.062, 0.042, 0.016, 0.005], (0, 1, 0), 0.62), TEAL, "ear{s}", mirror=True)

# Short stubby arms out to the sides and a little down, ending in ragged points of fur
SH, TIP = np.array([0.15, -0.02, 0.28]), np.array([0.23, -0.045, 0.22])
k.add("arm", tube([SH - (0.04, 0, 0), SH, TIP], [0.052, 0.048, 0.034]), TEAL, "arm{s}", mirror=True)
for i, d in enumerate([unit(1, -0.25, -0.15), unit(0.75, -0.3, -0.7), unit(0.25, -0.25, -1)]):
    base = TIP + d * 0.012
    k.add(f"claw{i}", tube([base, base + d * 0.042], [0.016, 0.003]), TEAL, "arm{s}", mirror=True)

# Short legs under the coat and cream feet with the toe turned up, so the pad shows to the front
k.add("leg", tube([(0.07, -0.01, 0.1), (0.076, -0.05, 0.05)], [0.05, 0.046]), TEAL, "leg{s}", mirror=True)
FC = np.array([0.082, -0.105, 0.048])
FOOT = ell(FC, (0.058, 0.064, 0.047), rot=(-28, 0, 0))
k.add("foot", FOOT, CREAM, "leg{s}", mirror=True)
SOLE = unit(0.1, -0.95, -0.12)

# The cream of the lower face: everything under a line through the mouth's top, rising a little to the cheeks,
# wrapped round to the sides; under the chin it gives way to the body's teal
FACE = mirrored([(0.0, 0.43), (0.06, 0.432), (0.12, 0.443), (0.17, 0.458), (0.2, 0.463), (0.225, 0.44),
                 (0.235, 0.38), (0.22, 0.25), (0.0, 0.25)])
FRONT = lambda P: P[:, 1] < H[1] + 0.07
def face(P):
    s, z = round_head(P)
    return poly_mask(s, z, FACE, edge=0.0028) * FRONT(P)
k.paint(face, CREAM, on=["head"])

# The open grin: flat along the cream's top line, deep in the middle, pink inside
MOUTH_SHAPE = mirrored([(0.0, 0.434), (0.04, 0.434), (0.052, 0.431), (0.054, 0.42), (0.047, 0.405), (0.034, 0.393),
                        (0.017, 0.387), (0.0, 0.386)])
k.paint(lambda P: poly_mask(*round_head(P), MOUTH_SHAPE, edge=0.0025) * FRONT(P), MOUTH)

# The chest's round cream patch under the chin
CC = hit(BODY, (0, -1, 0.255), (0, 1, 0))
k.paint(lambda P: inside(np.hypot(P[:, 0] / 1.0, (P[:, 2] - CC[2]) / 1.2) - 0.054, 0.0028) * (P[:, 1] < -0.05),
        CREAM, on=["body"])

# Two small white teeth hanging from the mouth's top line
for x in (-0.03, 0.03):
    p = hit(HEAD, (x, -1, 0.426), (0, 1, 0))
    k.add(f"tooth{x > 0}", ell(p + (0, -0.001, 0.0), (0.009, 0.005, 0.01), deform=boxy(2.6)), WHITE, "head",
          fuse=False, surface="shell")

# Three white claws over each foot's pad, and the pad itself
for side in (1, -1):
    for i, dx in enumerate((-0.028, 0.0, 0.028)):
        x = side * (FC[0] + dx)
        top = hit(FOOT, (FC[0] + dx, -0.135, 1), (0, 0, -1))
        base = np.array([x, top[1] + 0.006, top[2] - 0.008])
        d = unit(side * dx * 6, -0.7, 0.75)
        k.add(f"toe{i}{'R' if side > 0 else 'L'}", tube([base, base + d * 0.036], [0.013, 0.002]), WHITE,
              "legR" if side > 0 else "legL", fuse=False, surface="shell")
k.decal(FC + SOLE * 0.06 + (0, 0, -0.004), SOLE, (0.03, 0.03), PAD, eps=0.003, rings=8, segs=40, mirror=True)

# Round white eyes, high and apart, a small black pupil in each and a dark rim
CI, CW, CT = srgb(INK), srgb(WHITE), srgb(TEAL)
def eye(u, v):
    c = np.tile(CW, (len(u), 1))
    r = np.hypot(u, v)
    c[r > 0.92] = CI                      # a thin edge: at 0.83 it was a heavy black ring at the game's size
    c[np.hypot(u + 0.02, v + 0.03) < 0.36] = CI
    return c
de = unit(0.48, -0.84, 0.28)
E = hit(HEAD, H + de * 0.4, -de)
def eyes(state):
    k.decal(E, de, (0.036, 0.045), eye_look(state, eye, CT, CI, line=0.18), eps=0.004, rings=20, segs=56,
            mirror=True)

k.build(voxel=0.006)
k.eye_states(eyes)
k.export(out)
