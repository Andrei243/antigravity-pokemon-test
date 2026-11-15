# Teddiursa sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/teddiursa.py -- <the export's Teddiursa.glb> <out.glb>
# Written to be grown from: ursaring.py imports its frame (sibling("teddiursa")): the colours' way, the round ears with
# their dark lining, the bear's eye, the paws and feet with their claws, the face's paint round the head's axis.
#
# The design, from the references: a small orange-brown bear cub, the head nearly as wide as the body and some two
# fifths of its height; round ears on the crown's corners, a dark oval inside each; a pale cream crescent moon on the
# forehead, open to the upper right (+X); the lower face yellow-cream from cheek to cheek, the eyes on its top edge,
# solid black with a white glint, a small black nose between them, a small mouth and two little white fangs at its
# corners; a round pear of a body, short thick arms carried forward with three cream claws on each paw; short legs,
# broad feet with three cream claws; a round little tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, boxy, eye_look, args

ORANGE, MUZZLE, MOON = "#DE8A4E", "#F2D27E", "#F6E4B0"
LINING, CLAW, INK, MOUTH, WHITE = "#2E1C18", "#F1EBD6", "#17110F", "#5A2A24", "#FFFFFF"


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


def round_about(P, c, r):
    """(the arc round an upright axis through c, at radius r; z): coordinates for paint round a head or a body."""
    return np.arctan2(P[:, 0] - c[0], -(P[:, 1] - c[1])) * r, P[:, 2]


def claw(k, name, base, d, length, width, bone, color=CLAW):
    """A claw: a flat-faced wedge from base along d, its back a little thicker than its sides are wide."""
    base, d = np.array(base, float), unit(*d)
    a = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a)
    b = np.cross(a, d)
    k.add(name, hull([base + a * width + b * width * 0.8, base - a * width + b * width * 0.8,
                      base + a * width - b * width * 0.7, base - a * width - b * width * 0.7,
                      base - d * width, base + d * length - b * width * 0.25]), color, bone, fuse=False,
          surface="shell")


def paw(k, name, c, r, d, bone, color, claws=3, length=0.03, width=0.008, spread=0.6, claw_color=CLAW):
    """A round fused paw at c, its claws fanned along d over its front."""
    c, d = np.array(c, float), unit(*d)
    k.add(name, ell(c, r), color, bone)
    a = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a)
    for i in range(claws):
        o = (i - (claws - 1) / 2) * spread
        base = c + d * min(r) * 0.82 + a * o * min(r)
        claw(k, f"{name}claw{i}", base, d + a * o * 0.25, length, width, bone, claw_color)


def round_ear(k, c, r, n, bone, color, lining, half):
    """A round ear, a flattened ball fused into the crown, a dark oval laid on its front (mirrored)."""
    c, n = np.array(c, float), unit(*n)
    k.add("ear", ell(c, r), color, bone, mirror=True)
    front = hit(ell(c, r), c + n * 0.3, -n)
    k.decal(front, n, half, lining, eps=0.003, rings=10, segs=48, mirror=True)


def bear_eye(ink, glint=(1, 1, 1), at=(0.28, 0.32), size=0.3):
    """A solid black eye with one white glint high on its outer side (u outward, v up)."""
    ink, glint = np.array(ink, float), np.array(glint, float)
    def look(u, v):
        c = np.tile(ink, (len(u), 1))
        c[((u - at[0]) / size) ** 2 + ((v - at[1]) / size) ** 2 < 1] = glint
        return c
    return look


def eyes(k, E, de, half, lid, ink, open_look, up=(0, 0, 1), fierce=None):
    """The lay function for eye_states: eye_look's states from the open eye, laid mirrored."""
    def lay(state):
        look = fierce if (state == "Fierce" and fierce is not None) else eye_look(state, open_look, lid, ink, line=0.2)
        k.decal(E, de, half, look, eps=0.004, rings=20, segs=56, mirror=True, up=up)
    return lay


def teddiursa(src, out):
    k = Kit(src, coat="fur")
    H = np.array([0, -0.025, 0.375])              # the head's middle (its bone is at 0, -0.02, 0.3)
    HR = np.array([0.138, 0.12, 0.122])
    B = np.array([0, 0.005, 0.172])               # the body's middle

    # The body: a round pear, fullest low; the thighs fused into its bottom
    def pear(V):
        V = V.copy()
        s = 1 + 0.1 * np.clip(-V[:, 2], 0, 1) - 0.12 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell(B, (0.118, 0.104, 0.128), deform=pear), ORANGE, "body")
    k.add("tail", ell((0, 0.108, 0.1), (0.036, 0.034, 0.034)), ORANGE, "body")

    # The head: wide and round, a little squared, a full lower face standing forward for the muzzle
    def head_shape(V):
        V = boxy(2.3)(V)
        V[:, 0] *= 1 + 0.05 * np.clip(-V[:, 2], 0, 1)
        return V
    HEAD = ell(H, HR, deform=head_shape)
    k.add("head", HEAD, ORANGE, "head")
    k.add("muzzle", ell(H + (0, -0.07, -0.055), (0.098, 0.062, 0.058)), ORANGE, "head")

    # Round ears on the crown's corners, a dark oval inside each
    round_ear(k, (0.112, 0.012, 0.488), (0.06, 0.03, 0.058), (0.12, -1, 0.05), "ear{s}", ORANGE, LINING,
              (0.033, 0.035))

    # Short thick arms carried forward and a little down; round paws, three cream claws each
    k.add("arm", tube([(0.075, -0.01, 0.255), (0.118, -0.05, 0.215), (0.132, -0.095, 0.182)], [0.052, 0.048, 0.044]),
          ORANGE, "arm{s}", mirror=True)
    for s, bone in ((1, "armR"), (-1, "armL")):
        paw(k, "paw" + bone[-1], (0.135 * s, -0.112, 0.172), (0.044, 0.041, 0.041), (0.1 * s, -1, -0.35), bone,
            ORANGE, length=0.026, width=0.0085, spread=0.55)

    # Short legs, the thighs fused into the body; broad feet with three cream claws
    for s, bone in ((1, "legR"), (-1, "legL")):
        m = np.array([s, 1, 1.0])
        k.add("thigh" + bone[-1], ell(np.array((0.066, -0.005, 0.1)) * m, (0.062, 0.066, 0.068)), ORANGE, bone)
        k.add("shin" + bone[-1], tube([np.array(p) * m for p in [(0.07, -0.01, 0.075), (0.074, -0.02, 0.035)]],
                                      [0.046, 0.042]), ORANGE, bone)
        k.add("foot" + bone[-1], ell(np.array((0.075, -0.035, 0.022)) * m, (0.046, 0.062, 0.024)), ORANGE, bone)
        for i, dx in enumerate((-0.022, 0.0, 0.022)):
            claw(k, f"toe{bone[-1]}{i}", np.array((0.075 + dx, -0.088, 0.018)) * m, (0.25 * s * np.sign(dx), -1, 0.05),
                 0.022, 0.0075, bone)

    # The lower face yellow-cream from cheek to cheek, its top line under the eyes' tops, dipping between them
    FRONT = lambda P: P[:, 1] < H[1] + 0.03
    z0 = H[2]
    FACE = mirrored([(0.0, z0 + 0.012), (0.035, z0 + 0.02), (0.065, z0 + 0.034), (0.095, z0 + 0.028),
                     (0.122, z0 - 0.005), (0.136, z0 - 0.05), (0.13, z0 - 0.1), (0.1, z0 - 0.14), (0.0, z0 - 0.15)])
    k.paint(lambda P: poly_mask(*round_about(P, H, HR[0]), FACE, edge=0.0025) * FRONT(P), MUZZLE, on=["head", "muzzle"])

    # The crescent moon on the forehead: a disc less a disc pushed to the upper right (+X)
    zc, R = z0 + 0.073, 0.05
    def moon(P):
        s, z = round_about(P, H, HR[0])
        whole = inside(np.hypot(s, z - zc) - R, 0.0022)
        bite = inside(np.hypot(s - 0.025, z - zc - 0.015) - R * 0.66, 0.0022)
        return whole * (1 - bite) * FRONT(P)
    k.paint(moon, MOON)

    # A small black nose between the eyes, a little mouth under it with two white fangs at its corners
    nn = unit(0, -1, 0.15)
    NC = hit(ell(H + (0, -0.07, -0.055), (0.098, 0.062, 0.058)), (0, -1, z0 - 0.022), (0, 1, 0))
    k.decal(NC, nn, (0.021, 0.014), srgb(INK), eps=0.004, rings=10, segs=48)
    CM, CY = srgb(MOUTH), srgb(MUZZLE)
    def mouth(u, v):
        c = np.tile(CY, (len(u), 1))
        c[np.abs(v + 0.6 * (1 - u * u) - 0.25) < 0.42] = CM
        return c
    MC = hit(ell(H + (0, -0.07, -0.055), (0.098, 0.062, 0.058)), (0, -1, z0 - 0.075), (0, 1, 0))
    k.decal(MC, unit(0, -1, -0.25), (0.036, 0.016), mouth, eps=0.004, rings=8, segs=56)
    for s in (1, -1):
        p = hit(ell(H + (0, -0.07, -0.055), (0.098, 0.062, 0.058)), (0.032 * s, -1, z0 - 0.088), (0, 1, 0))
        claw(k, f"fang{'R' if s > 0 else 'L'}", p + (0, 0.003, 0), (0.15 * s, -0.25, 1), 0.026, 0.0095, "head", WHITE)

    # Eyes on the cream's top edge, solid black with a white glint
    de = unit(0.38, -0.9, 0.1)
    E = hit(HEAD, (0.062, -1, z0 + 0.004), (0, 1, 0))
    lay = eyes(k, E, de, (0.025, 0.028), srgb(MUZZLE), srgb(INK), bear_eye(srgb(INK)))

    k.build(voxel=0.005, band=0.005)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    teddiursa(*args())
