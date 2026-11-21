# Boldore sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/boldore.py -- <the export's Boldore.glb> <out.glb>
# Grown from roggenrola.py (sibling): the same dark blue faceted stone, the same hexagonal sockets and crystals.
#
# The design, from the references: a dome of dark blue stone, wider than it is tall, cut in flat facets, its underside
# darker. Down its middle a ridge of stone like a nose, rising in a point over the dome's top and running down the
# face. Behind the ridge's lower part the yellow hexagonal socket, lying on its side, a dark heart showing on each side
# of the ridge (built as a pair, one each side, on facets turned a little out). Out at the sides two great blocks of
# stone, each standing on an orange crystal pointing down and out; under the middle a third crystal, darker, short of
# the ground. Orange crystals jut up and out from the dome's shoulders, two each side, and three hang from under the
# face like teeth.
# Its skeleton has four legs; the design has three: the blocks are on the front legs' bones, the hind ones carry
# nothing.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, unit, args, hull, sibling

r = sibling("roggenrola")

DOME_C, DOME_R = np.array([0.0, 0.004, 0.2]), (0.127, 0.106, 0.116)
EYE_N, EYE_D = unit(0.3, -1, -0.02), 0.84             # each socket's facet (right side; mirrored)
EYE_HALF = (0.042, 0.034)
RIDGE = [(0.024, 0.34), (-0.035, 0.316), (-0.085, 0.27), (-0.112, 0.21), (-0.118, 0.158), (-0.11, 0.135)]


def ridge(k, path=RIDGE, half=0.021, rise=0.014, bone="head", centre=DOME_C, color=r.NAVY):
    """The ridge down the middle: an arc of points over the dome's top and down its face, half wide, standing rise
    proud along its own outward direction, the top a single point; their hull."""
    pts = []
    for i, (y, z) in enumerate(path):
        o = unit(0, y - centre[1], z - centre[2])
        p = np.array([0.0, y, z])
        w = half * (0.55 if i == 0 else 1.0)
        pts += [p + (sx * w, 0, 0) - o * 0.03 for sx in (1, -1)]
        pts += [p + (sx * w * 0.7, 0, 0) + o * rise * 0.6 for sx in (1, -1)]
        pts.append(p + o * rise)
    k.add("ridge", hull(pts), color, bone, fuse=False, surface="shell")


def on_dome(d, sink=0.86, c=DOME_C, radii=DOME_R):
    d = unit(*d)
    return np.array(c) + d * np.array(radii) * sink


def boldore(src, out):
    k = Kit(src, coat="shell")
    g = r.Glints()
    # the dome, with a facet each side of the ridge for the sockets and a flat underside
    eyes = [EYE_N * np.array([s, 1, 1]) for s in (1, -1)]
    planes = r.ball_planes(8, pent=1.0, hexa=0.985, jitter=0.03, turn=(6, 17, 31))
    planes = [(n, d) for n, d in planes if min(np.degrees(np.arccos(np.clip(n @ e, -1, 1))) for e in eyes) > 30]
    planes += [(e, EYE_D) for e in eyes] + [((0, 0, -1.0), 0.78)]
    k.add("dome", r.stone(DOME_C, DOME_R, planes), r.NAVY, "body", fuse=False, surface="shell")
    # its darker underside
    k.add("under", r.chunk((0, 0.01, 0.11), (0.1, 0.082, 0.032), 21, n=12), r.NAVYDK, "body", fuse=False,
          surface="shell")
    ridge(k)
    # the sockets' plates
    sockets = []
    for s, e in zip((1, -1), eyes):
        sc, sn = r.plane_world(DOME_C, DOME_R, e, EYE_D)
        sockets.append((r.hex_plate(k, "plate" + "RL"[s < 0], sc, sn, EYE_HALF, "body", turn=0), sn))
    # the side blocks, each on an orange crystal pointing down and out
    for s, side in ((1, "R"), (-1, "L")):
        k.add("block" + side, r.chunk((s * 0.13, -0.012, 0.122), (0.05, 0.058, 0.07), 40 + (s > 0), n=18),
              r.NAVY, "leg" + side, fuse=False, surface="shell")
        g.put(r.crystal((s * 0.142, -0.02, 0.066), (s * 0.3, -0.05, -1), 0.07, 0.021, turn=15), r.ORANGE,
              "leg" + side)
    # the third, under the middle, darker and short of the ground
    g.put(r.crystal((0, 0.0, 0.112), (0, 0, -1), 0.068, 0.027, turn=30), r.REDBROWN, "body")
    # crystals up and out from the dome's shoulders, two each side, and two on its back
    for s in (1, -1):
        for d, up, ln in (((0.42, -0.1, 0.85), (0.5, -0.1, 1.0), 0.066), ((0.86, -0.05, 0.42), (1.0, 0.0, 0.75), 0.07),
                          ((0.35, 0.75, 0.55), (0.3, 0.8, 0.7), 0.06)):
            dd = np.array(d) * (s, 1, 1)
            g.put(r.crystal(on_dome(dd), np.array(up) * (s, 1, 1), ln, 0.019, turn=20), r.ORANGE, "body")
    # three hanging from under the face like teeth
    for x in (-0.052, 0.0, 0.052):
        g.put(r.crystal((x, -0.088 + abs(x) * 0.35, 0.138), (x * 2, -0.3, -1), 0.052, 0.017, turn=30), r.ORANGE,
              "body")
    g.flush(k)
    k.build(voxel=0.0028, smooth=2)
    def lay(state):
        c, n = sockets[0]
        k.decal(c, n, EYE_HALF, r.hex_eye(state, 0, core=0.475), up=(0, 0, 1), eps=0.003, rings=20, segs=56,
                outline=r.polygon(r.hex_pts(0)), mirror=True)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    boldore(src, out)
