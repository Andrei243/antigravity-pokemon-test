# Archeops sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Archen's script (sibling("archen"): its colours, eyes, fangs, wings, tail fan and feet):
#   blender --background --factory-startup --python sculpts/archeops.py -- <the export's Archeops.glb> <out.glb>
#
# From the references: a feathered bird-lizard in flight. A long head carried forward on a red S of a neck: a teal
# cap with dark bands from the hooked beak's tip back over the crown, the face and jaw red, the mouth open and pink
# with white fangs; a round white eye with a black pupil in a bold black ring. A shaggy yellow body of points; yellow
# arms raised in a V, shaggy, with a red hand and three white claws at each wrist and great blue flight feathers fanned
# out from them; a skirt of teal-green points round the hips with blue feathers hanging behind; red legs tucked up
# with white claws; a long red tail curling down to a star of blue feathers.
# The task's text spoke of yellow-blue-green wings: the pictures show the wings yellow and blue, the green round the
# hips; the pictures are followed.
# Reference ratios (3D picture): head length / total 0.25, head height / total 0.14 (made a fifth larger: it is small
# against the wings), wing span / height 0.9, the tail's star 0.05 of the height off the lowest point.
# The export's joints: body (0, 0, 0.535), wings (+-0.06, 0, 0.595), tail (0, 0.07, 0.485), head (0, -0.13, 0.725),
# jaw (0, -0.18, 0.745), legs (+-0.04, 0, 0.475); the export spans 0.096 to 0.881.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, args, sibling

ar = sibling("archen")
tc = ar.tc
RED, BLUE, YELLOW, WHITE, INK, CLAW = ar.RED, ar.BLUE, ar.YELLOW, ar.WHITE, ar.INK, ar.CLAW
TEAL, BAND, GREEN, MOUTH = "#2BA89C", "#1E7C78", "#2A9C78", "#E77C88"

C = np.array([0.0, -0.165, 0.735])            # the skull's centre, a fifth over the measure
CR = np.array([0.072, 0.08, 0.066])
DE = unit(0.8, -0.55, -0.05)
EYE_HALF = (0.03, 0.03)
VOXEL = 0.0066


def cap(P):
    """The teal cap: the skull's and the beak's tops, its edge sloping down to the beak's tip."""
    edge = C[2] + 0.034 + 0.15 * (P[:, 1] - C[1])
    return inside(edge - P[:, 2], 0.0025) * (P[:, 1] < C[1] + 0.07)


def archeops(src, out):
    k = Kit(src, coat="fur")

    # The head: a long skull and an upper jaw hooked down at the tip, the lower jaw dropped open, pink between
    k.add("skull", ell(C, CR), RED, "head")
    k.add("upper", ell((0, -0.27, 0.738), (0.05, 0.1, 0.04), rot=(-8, 0, 0)), RED, "head")
    k.add("hook", ell((0, -0.355, 0.716), (0.03, 0.03, 0.03)), RED, "head")
    k.add("lower", ell((0, -0.255, 0.682), (0.042, 0.085, 0.022), rot=(16, 0, 0)), RED, "jaw")
    k.add("gape", ell((0, -0.24, 0.708), (0.036, 0.07, 0.02), rot=(6, 0, 0)), MOUTH, "head")
    k.paint(cap, TEAL, on=["skull", "upper", "hook"])
    # dark bands across the cap
    k.paint(lambda P: cap(P) * inside(np.abs(((P[:, 1] + 0.36) % 0.045) - 0.0225) - 0.006, 0.002)
            * (P[:, 1] > -0.34) * (P[:, 1] < C[1] + 0.05), BAND, on=["skull", "upper"])
    for s in (1, -1):
        k.add(f"fang{'R' if s > 0 else 'L'}", ar.fang((0.03 * s, -0.28, 0.705), 0.03, 0.011), WHITE, "head",
              fuse=False, surface="shell")
        k.add(f"jawfang{'R' if s > 0 else 'L'}", ar.fang((0.026 * s, -0.255, 0.69), 0.022, 0.009, d=(0, 0, 1)),
              WHITE, "jaw", fuse=False, surface="shell")

    # The neck: a red S of fused balls from the chest up and forward to the skull
    for i, (c, r) in enumerate((((0, -0.04, 0.6), 0.058), ((0, -0.07, 0.64), 0.05), ((0, -0.1, 0.68), 0.046),
                                ((0, -0.12, 0.715), 0.046))):
        k.add(f"neck{i}", ell(c, (r, r, r)), RED, "head")

    # The body: shaggy yellow, points of fur all round
    k.add("body", ell((0, 0.0, 0.54), (0.1, 0.095, 0.115)), YELLOW, "body")
    for i, (b, d) in enumerate((((0.08, 0.0, 0.6), (1, 0.2, 0.5)), ((-0.08, 0.0, 0.6), (-1, 0.2, 0.5)),
                                ((0.09, 0.02, 0.52), (1, 0.3, -0.1)), ((-0.09, 0.02, 0.52), (-1, 0.3, -0.1)),
                                ((0.05, 0.07, 0.6), (0.4, 1, 0.6)), ((-0.05, 0.07, 0.6), (-0.4, 1, 0.6)),
                                ((0.0, 0.08, 0.53), (0, 1, 0.1)), ((0.0, 0.06, 0.64), (0, 0.6, 1)),
                                ((0.05, -0.07, 0.5), (0.5, -1, -0.2)), ((-0.05, -0.07, 0.5), (-0.5, -1, -0.2)))):
        k.add(f"fur{i}", tc.spike(b, d, 0.07, 0.03, 0.022), YELLOW, "body", fuse=False)

    # The skirt of teal-green points round the hips, blue feathers hanging behind it
    k.add("skirt", tc.ruff((0, 0.01, 0.455), (0.1, 0.095, 0.035), 10, 0.9, 0.3), GREEN, "body")
    for i, a in enumerate(np.radians([-60, -25, 25, 60, 120, 155, 205, 240])):
        d = unit(math.sin(a), -math.cos(a), -0.6)
        k.add(f"skirtpoint{i}", tc.spike((0.085 * math.sin(a), 0.01 - 0.085 * math.cos(a), 0.45), d, 0.065, 0.03,
                                         0.022), GREEN, "body", fuse=False)
    for i, x in enumerate((-0.07, -0.025, 0.025, 0.07)):
        d = unit(x * 4, 0.6, -1)
        k.add(f"hipfeather{i}", tc.feather((x, 0.07, 0.45), d, 0.15, 0.04, thick=0.022), BLUE, "body", fuse=False)

    # The wings: yellow arms raised in a V, a red hand with white claws at each wrist, great blue feathers fanned out
    wrist = ar.wing(k, (0.07, 0.0, 0.6), (0.5, 0.12, 0.85), 0.21, 0.03, (1.0, 0.3, -0.55),
                    [(BLUE, ar.zig(0.15, 1.05, -0.05, 0.95, 4, 0.2), 0.006),
                     (YELLOW, ar.zig(-0.05, 1.0, -0.12, 0.38, 5, 0.12), -0.006)],
                    (0.05, 0.013), thick=0.02)
    k.add("hand", ell(wrist, (0.036, 0.034, 0.036)), RED, "wing{s}", mirror=True)

    # The legs: red, tucked up and forward, white claws
    k.add("thigh", ell((0.055, -0.02, 0.47), (0.045, 0.05, 0.045)), RED, "leg{s}", mirror=True)
    k.add("shin", tube([(0.06, -0.05, 0.46), (0.07, -0.11, 0.44), (0.072, -0.13, 0.41)], [0.03, 0.026, 0.024]), RED,
          "leg{s}", mirror=True)
    k.add("foot", ell((0.072, -0.14, 0.395), (0.03, 0.035, 0.022)), RED, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.02, 0.0, 0.02)):
        k.add(f"toeclaw{j}", tc.spike((0.072 + dx, -0.16, 0.392), (dx * 3, -0.6, -1), 0.04, 0.011, 0.009), CLAW,
              "leg{s}", fuse=False, mirror=True, surface="shell")

    # The tail: long and red, curling down and out to +X, a star of blue feathers at its end
    TAIL = [(0, 0.06, 0.47), (0.02, 0.15, 0.41), (0.06, 0.18, 0.31), (0.08, 0.13, 0.21), (0.07, 0.07, 0.16)]
    k.add("tail", tube(TAIL, [0.04, 0.034, 0.028, 0.022, 0.018]), RED, "tail")
    e = np.array(TAIL[-1])
    n = unit(0.35, -0.75, 0.2)
    e1 = np.cross(n, (0, 0, 1.0)); e1 /= np.linalg.norm(e1); e2 = np.cross(n, e1)
    for j, th in enumerate(np.radians([-90, -18, 54, 126, 198])):
        d = math.cos(th) * e1 + math.sin(th) * e2
        side = -math.sin(th) * e1 + math.cos(th) * e2
        k.add(f"star{j}", ar.kite(e + d * 0.008, d, side, 0.075, 0.024, thick=0.018), BLUE, "tail", fuse=False)

    k.build(voxel=VOXEL, smooth=3, band=0.006)

    E = tc.on_ell(C, CR, DE)
    k.eye_states(lambda state: tc.lay_eyes(k, state, E, DE, EYE_HALF, ar.eye_open(), srgb(RED), srgb(INK),
                                           eps=0.004))
    k.export(out)


if __name__ == "__main__":
    archeops(*args())
