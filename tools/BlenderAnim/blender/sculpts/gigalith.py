# Gigalith sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gigalith.py -- <the export's Gigalith.glb> <out.glb>
# Grown from roggenrola.py and boldore.py (sibling): the same faceted stone, ridge, hexagonal sockets and crystals.
#
# The design, from the references: a great figure of dark blue stone sitting up on four legs, its body a pear, narrow
# at the shoulders and broad at the belly, which comes down nearly to the ground between the legs. The head forward
# on the body: down its middle the line's ridge, ending over a dark notch of a mouth; on each side of it a yellow
# hexagonal socket with a round red eye and a dark pupil. Over the head a crest of red crystals, the tallest in the
# middle, more on the shoulders, on the back and on each leg. Each leg a broad pyramid of stone, its foot spread on the
# ground with small crystals at its edges; between the front legs and the body pale grey round joints.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, unit, args, hull, sibling

r = sibling("roggenrola")
b = sibling("boldore")

HEAD_C, HEAD_R = np.array([0.0, -0.118, 0.425]), (0.102, 0.09, 0.094)
EYE_N, EYE_D = unit(0.36, -1, 0.06), 0.86
EYE_HALF = (0.036, 0.029)
NOSE = [(-0.07, 0.52), (-0.155, 0.49), (-0.2, 0.44), (-0.212, 0.39), (-0.205, 0.36)]
MOUTH = "#141A33"


def leg(k, g, s, y0, y1, apex, bone, crystals):
    """A broad pyramid of stone: a ragged footprint from x 0.05 to 0.3 (by side s) and y0 to y1, a middle ring and
    a small top; its crystals (base, direction, length, radius) put on its bone."""
    rs = np.random.RandomState(70 + int(s > 0) + int(10 * y0))
    P = []
    xs, ys = (0.05, 0.3), (y0, y1)
    cx, cy = (xs[0] + xs[1]) / 2, (y0 + y1) / 2
    for i in range(9):
        th = 2 * math.pi * i / 9
        j = 0.88 + 0.12 * rs.rand()
        P.append((s * (cx + (xs[1] - xs[0]) / 2 * j * math.cos(th)), cy + (y1 - y0) / 2 * j * math.sin(th), 0.0))
    for i in range(6):
        th = 2 * math.pi * i / 6 + 0.5
        P.append((s * (cx * 0.92 + 0.075 * math.cos(th)), cy + 0.06 * math.sin(th), apex[2] * 0.55))
    for i in range(3):
        th = 2 * math.pi * i / 3
        P.append((s * apex[0] + 0.02 * math.cos(th), apex[1] + 0.018 * math.sin(th), apex[2]))
    k.add("leg" + bone, hull(P), r.NAVY, bone, fuse=False, surface="shell")
    for base, d, ln, rad in crystals:
        g.put(r.crystal(np.array(base) * (s, 1, 1), np.array(d) * (s, 1, 1), ln * 1.2, rad * 1.35, turn=20, flat=0.75),
              r.RED, bone)


def gigalith(src, out):
    k = Kit(src, coat="shell")
    g = r.Glints()
    # the body: a pear of two stones, the chest narrow, the belly broad and low between the legs
    k.add("chest", r.stone((0, -0.03, 0.37), (0.112, 0.105, 0.11), r.ball_planes(12, jitter=0.04, turn=(10, 20, 5))),
          r.NAVY, "body", fuse=False, surface="shell")
    k.add("belly", r.stone((0, 0.02, 0.205), (0.142, 0.13, 0.15),
                           r.ball_planes(13, jitter=0.04, turn=(30, 5, 12)) + [((0, 0, -1.0), 0.88)]),
          r.NAVY, "body", fuse=False, surface="shell")
    # the head, with a facet each side of its ridge for the sockets
    eyes = [EYE_N * np.array([s, 1, 1]) for s in (1, -1)]
    planes = r.ball_planes(14, jitter=0.03, turn=(4, 11, 26))
    planes = [(n, d) for n, d in planes if min(np.degrees(np.arccos(np.clip(n @ e, -1, 1))) for e in eyes) > 30]
    planes += [(e, EYE_D) for e in eyes]
    k.add("head", r.stone(HEAD_C, HEAD_R, planes), r.NAVY, "head", fuse=False, surface="shell")
    b.ridge(k, NOSE, half=0.022, rise=0.016, centre=HEAD_C)
    # the mouth: a dark notch pointing down under the ridge's end
    k.add("mouth", hull([(0.03, -0.17, 0.372), (-0.03, -0.17, 0.372), (0.0, -0.188, 0.33), (0.0, -0.16, 0.335),
                         (0.026, -0.194, 0.364), (-0.026, -0.194, 0.364)]), MOUTH, "head", fuse=False,
          surface="shell")
    sc, sn = r.plane_world(HEAD_C, HEAD_R, eyes[0], EYE_D)
    front = r.hex_plate(k, "plateR", sc, sn, EYE_HALF, "head", turn=0)
    r.hex_plate(k, "plateL", sc * (-1, 1, 1), sn * (-1, 1, 1), EYE_HALF, "head", turn=0)
    # the crest over the head: the tallest in the middle, flattened to show its faces to the front and back
    for base, d, ln, rad in (((-0.012, -0.155, 0.47), (0, 0.12, 1), 0.2, 0.04),
                             ((0.052, -0.11, 0.47), (0.35, 0.18, 1), 0.16, 0.034),
                             ((-0.064, -0.1, 0.46), (-0.55, 0.08, 1), 0.1, 0.023),
                             ((0.01, -0.03, 0.46), (0.1, 0.6, 1), 0.1, 0.022),
                             ((-0.07, -0.18, 0.42), (-0.35, -0.45, -1), 0.065, 0.017)):
        g.put(r.crystal(base, d, ln, rad, flat=0.65, turn=30), r.RED, "head")
    # the shoulders' and the back's crystals
    for s in (1, -1):
        for base, d, ln, rad in (((0.09, -0.04, 0.41), (0.6, 0.0, 1), 0.12, 0.026),
                                 ((0.06, 0.08, 0.42), (0.3, 0.7, 0.8), 0.1, 0.023),
                                 ((0.12, 0.08, 0.3), (0.8, 0.5, 0.6), 0.08, 0.02)):
            g.put(r.crystal(np.array(base) * (s, 1, 1), np.array(d) * (s, 1, 1), ln * 1.15, rad * 1.3, turn=20,
                            flat=0.75), r.RED, "body")
    # the legs, the grey joints between the front legs and the body
    for s, side in ((1, "R"), (-1, "L")):
        leg(k, g, s, -0.225, -0.005, (0.15, -0.115, 0.255), "leg" + side,
            [((0.19, -0.12, 0.17), (0.6, 0, 1), 0.12, 0.026), ((0.27, -0.08, 0.09), (1, 0, 0.8), 0.1, 0.023),
             ((0.25, -0.19, 0.03), (0.6, -0.6, 0.2), 0.06, 0.016), ((0.12, -0.21, 0.025), (0.1, -1, 0.2), 0.05, 0.015)])
        leg(k, g, s, 0.02, 0.205, (0.15, 0.11, 0.21), "hind" + side,
            [((0.21, 0.12, 0.15), (0.6, 0.4, 1), 0.1, 0.023), ((0.27, 0.16, 0.03), (0.6, 0.6, 0.2), 0.06, 0.016)])
        k.add("joint" + side, ell((s * 0.098, -0.125, 0.165), (0.034, 0.03, 0.028), sub=3), r.GREY, "leg" + side,
              fuse=False, surface="shell")
    g.flush(k)
    k.build(voxel=0.003, smooth=2)
    def lay(state):
        k.decal(front, sn, EYE_HALF, r.hex_eye(state, 0, iris=(0.52, 0.225)), up=(0, 0, 1), eps=0.003, rings=20,
                segs=56, outline=r.polygon(r.hex_pts(0)), mirror=True)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    gigalith(src, out)
