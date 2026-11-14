# Snorlax sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Munchlax's script (munchlax.py): the same teal and cream, the face's cream mask painted round the head's axis, pointed
# ears out of the crown, cream feet with a round brown pad and three white claws over it.
#   blender --background --factory-startup --python sculpts/snorlax.py -- <the export's Snorlax.glb> <out.glb>
#
# The design, from the references: an enormous round body, the head small on top of it and sunk into its shoulders;
# a great cream belly over most of the front; the face cream but for the teal coming down in a point between the eyes;
# small pointed ears on the crown's corners; eyes always shut, two short dark slits; a thin mouth with two small fangs
# standing up from the lower lip; thick arms from high on the shoulders, hanging down and out, three white claws at each
# hand; big cream feet out in front, soles to the world, each with a round brown pad and three white claws over it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

TEAL, CREAM, PAD, INK, WHITE = "#2F7590", "#EFE6CC", "#84664F", "#2A2422", "#FFFFFF"
B = np.array([0, 0.02, 0.38])             # the body's middle
BR = np.array([0.4, 0.34, 0.37])
H = np.array([0, -0.06, 0.78])            # the head's middle
HR = np.array([0.24, 0.2, 0.166])


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


def round_body(P):
    return np.arctan2(P[:, 0], -(P[:, 1] - B[1])) * BR[0], P[:, 2]


def head_shape(V):
    """Munchlax's head grown: squared a little, fullest at the cheeks, rounder over the crown."""
    V = boxy(2.4)(V)
    s = 1 + 0.06 * np.clip(-V[:, 2], 0, 1) - 0.1 * np.clip(V[:, 2], 0, 1) ** 2
    V[:, 0] *= s
    return V


def round_bottom(V):
    """The great body: a ball, a little squared and settled on its seat."""
    V = boxy(2.2)(V)
    s = 1 + 0.05 * np.clip(-V[:, 2], 0, 1) - 0.08 * np.clip(V[:, 2], 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    return V


# The great body and the small head sunk into its shoulders
BODY = ell(B, BR, deform=round_bottom)
k.add("body", BODY, TEAL, "body")
HEAD = ell(H, HR, deform=head_shape)
k.add("head", HEAD, TEAL, "head")

# Small pointed ears on the crown's corners, leaning a little out
EB = np.array([0.14, 0.0, 0.875])
k.add("ear", blade([EB, (0.162, -0.004, 0.948), (0.186, -0.008, 0.996), (0.192, -0.01, 1.009)],
                   [0.074, 0.047, 0.016, 0.005], (0, 1, 0), 0.6), TEAL, "ear{s}", mirror=True)

# Thick arms from high on the shoulders, hanging down and out, three white claws at each hand
# Thick sleeves from high on the shoulders, carried down and forward of the belly's widest line, so that both show
# from the front three-quarter; the clawed hands clear of the body
HA = np.array([0.45, -0.25, 0.33])
k.add("shoulder", ell((0.25, 0.0, 0.58), (0.14, 0.13, 0.11)), TEAL, "body", mirror=True)
k.add("arm", tube([(0.28, 0.0, 0.52), (0.39, -0.1, 0.47), (0.44, -0.19, 0.39), HA], [0.125, 0.118, 0.105, 0.085]),
      TEAL, "arm{s}", mirror=True)
AD = unit(*(HA - np.array([0.44, -0.19, 0.39])))
for i, dx in enumerate((-0.045, 0.0, 0.045)):
    d = unit(0.15 + dx * 4, -0.55, -0.8)
    base = HA + AD * 0.06 + np.array([dx, 0, 0])
    k.add(f"claw{i}", tube([base, base + d * 0.05], [0.017, 0.002]), WHITE, "arm{s}", fuse=False, mirror=True,
          surface="shell")

# Short thick legs and big cream feet out in front, the soles to the world
k.add("leg", tube([(0.17, -0.06, 0.18), (0.22, -0.2, 0.15)], [0.12, 0.11]), TEAL, "leg{s}", mirror=True)
FC = np.array([0.255, -0.3, 0.15])
FOOT = ell(FC, (0.135, 0.085, 0.15), rot=(0, 0, -14))
k.add("foot", FOOT, CREAM, "leg{s}", mirror=True)
SOLE = unit(0.24, -1, 0.0)
k.decal(FC + SOLE * 0.1 + (0, 0, -0.025), SOLE, (0.072, 0.08), PAD, eps=0.004, rings=10, segs=48, mirror=True)
for side in (1, -1):
    for i, dx in enumerate((-0.06, 0.0, 0.06)):
        top = hit(FOOT, (FC[0] + dx, -0.345 + 0.25 * dx, 1), (0, 0, -1))
        base = np.array([side * top[0], top[1] + 0.004, top[2] - 0.012])
        d = unit(side * dx * 4, -0.55, 0.85)
        k.add(f"toe{i}{'R' if side > 0 else 'L'}", tube([base, base + d * 0.055], [0.019, 0.003]), WHITE,
              "legR" if side > 0 else "legL", fuse=False, surface="shell")

# The great cream belly over most of the front, round
BELLY = lambda P: inside(np.hypot(round_body(P)[0] / 0.31, (P[:, 2] - 0.37) / 0.33) - 1, 0.012) * (P[:, 1] < B[1])
k.paint(BELLY, CREAM, on=["body"])

# The face's cream mask: from the chin up the cheeks nearly to the ears, its top coming down in a point between the
# eyes, where the teal of the brow reaches down
FACE0 = mirrored([(0.0, 0.8), (0.05, 0.828), (0.1, 0.852), (0.15, 0.868), (0.185, 0.862), (0.215, 0.83),
                 (0.235, 0.76), (0.24, 0.66), (0.22, 0.55), (0.0, 0.55)])
FACE = [(s * 1.12, H[2] + (z - 0.765) * 1.12) for s, z in FACE0]   # drawn for a head 1.12 times smaller
FRONT = lambda P: P[:, 1] < H[1] + 0.06
k.paint(lambda P: poly_mask(*round_head(P), FACE, edge=0.003) * FRONT(P), CREAM, on=["head"])

# A thin mouth, a little wavy, and two small fangs standing up from its lower lip
CI, CC = srgb(INK), srgb(CREAM)
def mouth(u, v):
    c = np.tile(CC, (len(u), 1))
    c[np.abs(v - 0.25 * np.cos(u * math.pi * 1.5)) < 0.55] = CI
    return c
dm = unit(0, -1, 0.12)
MC = hit(HEAD, (0, -1, 0.727), (0, 1, 0))
k.decal(MC, dm, (0.07, 0.009), mouth, eps=0.003, rings=6, segs=60)
for x in (-0.047, 0.047):
    p = hit(HEAD, (x, -1, 0.72), (0, 1, 0))
    base = p + (0, -0.002, 0.0)
    k.add(f"fang{x > 0}", tube([base, base + (0, -0.001, 0.027)], [0.0105, 0.0015]), WHITE, "head", fuse=False,
          surface="shell")

# Eyes always shut: in the open state two short dark slits arched a little; the other states only change the line's
# curve or slant, never opening the eye
def slit(u, v):
    c = np.tile(CC, (len(u), 1))
    c[(np.abs(u) < 0.9) & (np.abs(v - (0.34 * (1 - u * u) - 0.16)) < 0.2)] = CI
    return c
de = unit(0.36, -0.92, 0.1)
E = hit(HEAD, H + de * 0.4 + (0, 0, 0.0), -de)
def sleepy(state):
    """eye_look's states, but Fierce is the same slit tilted down toward the nose (eye_look's would draw a lid line
    across the slit)."""
    if state != "Fierce":
        return eye_look(state, slit, CC, CI, line=0.2)
    def look(u, v):
        c = np.tile(CC, (len(u), 1))
        c[(np.abs(u) < 0.9) & (np.abs(v - (0.42 * u - 0.05)) < 0.2)] = CI
        return c
    return look
def eyes(state):
    k.decal(E, de, (0.05, 0.029), sleepy(state), eps=0.004, rings=20, segs=56,
            mirror=True)

k.build(voxel=0.009)
k.eye_states(eyes)
k.export(out)
