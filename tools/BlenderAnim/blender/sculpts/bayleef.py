# Bayleef sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/bayleef.py -- <the export's Bayleef.glb> <out.glb>
#
# The design, from the references: Chikorita grown into a pale yellow long-necked quadruped: a deep barrel of a body
# on four thick column legs with white claws, a short pointed tail rising behind; a long neck and a small round head
# with a full snout, big red eyes with a white glint and a dark rim, a small smile; round its crown a rolled leaf
# like a band, and out of it a great sickle-shaped leaf rising and sweeping back, a notch with a curl cut in its back
# edge; round the root of the neck a collar of eight rolled green leaves standing out, scored across with darker lines.
#
# Grown from chikorita.py (sibling): the same trunk, legs, claws, way of making eyes and smile, the same necklace's
# way of finding the surface; Bayleef's sizes, colours, neck, tail, crown leaf and collar.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, ring_band, srgb, unit, args, outward, sibling

c = sibling("chikorita")

CREAM = "#F0DA7E"          # the coat: between the drawing's cream and the model's yellow
LEAF, RIB = "#86B45C", "#5A8A3E"
ROLL, ROLLINE = "#7FAE56", "#557F3A"
CLAW = c.CLAW
RED, PUPIL, WHITE, INK = c.RED, c.PUPIL, c.WHITE, c.INK
SMILE = "#7A5A2A"

H = np.array([0, -0.205, 0.565])                        # the head's middle
HR = np.array([0.09, 0.102, 0.082])
SNOUT = (H + (0, -0.064, -0.022), (0.069, 0.064, 0.058))
NECK = [(0, -0.07, 0.33), (0, -0.13, 0.43), (0, -0.175, 0.51), (0, -0.19, 0.55)]
NECK_R = [0.085, 0.058, 0.05, 0.05]
TRUNK = [((0, -0.06, 0.275), (0.122, 0.12, 0.13)),          # chest
         ((0, 0.05, 0.27), (0.13, 0.15, 0.13)),            # middle
         ((0, 0.16, 0.255), (0.12, 0.1, 0.12)),            # rump
         ((0, 0.04, 0.18), (0.112, 0.17, 0.1))]            # belly, low
LEGS = dict(x=0.088, front=-0.08, hind=0.145, top=0.2, ankle=0.05, r=0.052, shoulder=(0.064, 0.075, 0.1),
            thigh=(0.07, 0.085, 0.105), foot=(0.054, 0.062, 0.03), toe=0.012, claws=3, spread=0.024, claw=0.02,
            haunch_at=(0.014, 0.0, 0.03))
EYE_D = unit(0.66, -0.7, 0.18)
EYE_HALF = (0.03, 0.036)


def crown_leaf(k, H, HR, col, rib, scale=1.0):
    """The rolled band round the crown and the sickle leaf rising out of it, its plane turned back and out to +X."""
    top = c.hit([(H, HR)], H, (0, -0.25, 1))
    k.add("band", ring_band(H + (0, 0.0, HR[2] * 0.42), unit(0, -0.45, 1), HR[0] * 0.9, 0.04, thick=0.02),
          col, "leaf", fuse=False, surface="leaf")
    # the band's curled front end over the brow
    k.add("curl", outward(tube([top + (0, -0.03, -0.01), top + (0, -0.05, 0.012), top + (0, -0.035, 0.03)],
                               [0.016, 0.014, 0.011], seg=14, per=4)), col, "leaf", fuse=False, surface="leaf")
    h = unit(0.57, 0.82, 0)
    ez = unit(*(np.array([0, 0, 1.0]) * 0.85 + h * 0.55))
    s = 0.74 * scale
    outline = [(-0.03, 0.0), (-0.06, 0.08), (-0.05, 0.16), (0.0, 0.24), (0.08, 0.3), (0.18, 0.33),
               (0.17, 0.27), (0.14, 0.22), (0.095, 0.21), (0.078, 0.18), (0.1, 0.15), (0.09, 0.1),
               (0.06, 0.04), (0.03, 0.0)]
    outline = [(a * s, b * s) for a, b in outline]
    base = top + (0, 0.0, -0.01)
    k.add("sickle", slab(outline, base, h, ez, 0.026), col, "leaf", fuse=False, surface="leaf")
    # (a midrib was tried: at this size it read as a second blade)


def rolled(k, name, at, d, length, r, col, line, bone, bands=(-0.22, 0.32), face=(0, -0.55, 0.85)):
    """A rolled leaf from at along d: a thick oval plate, its faces turned toward `face` (so the collar is seen as a
    ruff from the front), scored across with darker veins on both faces."""
    at, d = np.array(at, float), unit(*d)
    mid = at + d * length / 2
    n = np.array(face, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    a = np.cross(n, d)
    w, th = r * 1.4, r * 0.62
    V, F = ell((0, 0, 0), (w, th, length / 2), sub=3)
    V = V @ np.stack([a, n, d], axis=1).T + mid
    k.add(name, outward((V, F)), col, bone, fuse=False, surface="leaf")
    for j, t in enumerate(bands):
        q = math.sqrt(max(1 - t * t, 0.05))
        for sg in (1, -1):
            Vv, Fv = ell((0, 0, 0), (w * q * 0.84, 0.006, 0.0105), sub=2)
            Vv = Vv @ np.stack([a, n, d], axis=1).T + mid + d * t * length / 2 + n * sg * th * q * 0.97
            k.add(f"{name}vein{j}{'a' if sg > 0 else 'b'}", outward((Vv, Fv)), line, bone, fuse=False, surface="leaf")


def collar(k, ells, centre, count, tilt, length, r, col, line, bone="body", lift=0.45, start=0.5):
    centre = np.array(centre, float)
    for i in range(count):
        ar = 2 * math.pi * (i + start) / count
        d = np.array([math.sin(ar), -math.cos(ar), 0.0])
        o = np.array([centre[0], centre[1], centre[2] - tilt * math.cos(ar)])
        p = c.hit(ells, o, d)
        out = unit(d[0], d[1], lift - 0.35 * max(-math.cos(ar), 0))
        rolled(k, f"roll{i}", p - out * length * 0.15, out, length, r, col, line, bone)


def bayleef(src, out):
    k = Kit(src, coat="fur")
    c.trunk(k, TRUNK, CREAM)
    c.legs(k, LEGS, CREAM, CLAW)
    k.add("neck", tube(NECK, NECK_R), CREAM, "head")
    k.add("head", ell(H, HR), CREAM, "head")
    k.add("snout", ell(*SNOUT), CREAM, "head")
    # The tail: short and pointed, rising back
    k.add("tail", tube([(0, 0.2, 0.27), (0, 0.29, 0.31), (0, 0.37, 0.37)], [0.07, 0.04, 0.012]), CREAM, "tail")

    crown_leaf(k, H, HR, LEAF, RIB)
    ells = TRUNK + [((0, -0.1, 0.36), (0.085, 0.085, 0.08))]
    collar(k, ells, (0, -0.1, 0.37), 8, 0.03, 0.16, 0.034, ROLL, ROLLINE)

    k.decal(c.hit([SNOUT], SNOUT[0], (0, -1, -0.45)), unit(0, -1, -0.35), (0.024, 0.014), c.smile(CREAM, SMILE, width=0.32),
            eps=0.003, rings=16, segs=72)

    k.build(voxel=0.0068, band=0.008)
    centre = c.hit([(H, HR)], H, EYE_D)
    look = c.eye_colours(WHITE, RED, PUPIL, INK)
    k.eye_states(lambda state: c.lay_eyes(k, state, centre, EYE_D, EYE_HALF, look, CREAM, INK))
    k.export(out)


if __name__ == "__main__":
    bayleef(*args())
