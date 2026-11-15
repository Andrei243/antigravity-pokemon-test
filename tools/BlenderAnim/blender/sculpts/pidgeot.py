# Pidgeot sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pidgeot.py -- <the export's Pidgeot.glb> <out.glb>
# Pidgeotto grown (built by pidgey.py's and pidgeotto.py's own functions, on Pidgeotto's frame a fifth larger, as the
# skeleton is): the same brown crown and back over a broad cream breast, the same almond eye under a slanting brow and
# the black wedge behind it; the crest now a long sheaf of yellow feathers laid back over the nape and down the back,
# red streamers under and beside it, the longest trailing past it; great wings spread, raised in a V and swept back,
# cream with a brown band along the leading edge and the shoulder; a broad fan of red tail feathers; pink legs.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, sibling, slab, srgb, unit, args

o = sibling("pidgeotto")
p = o.p
GOLD, RED, REDTAIL = "#F4CF5A", "#E2475E", "#DE3A44"
S = 1.2                                              # Pidgeot's joints are Pidgeotto's a fifth larger


def frame():
    F = o.frame()
    for name in ("H", "TAILBASE"):
        setattr(F, name, getattr(F, name) * S)
    F.HR = F.HR * S
    c, r, rot = F.BODY
    F.BODY = (tuple(np.array(c) * S), tuple(np.array(r) * S * np.array([1.0, 1.0, 1.0])), rot)
    F.BREAST = tuple(tuple(np.array(v) * S) for v in F.BREAST)
    F.NECK = tuple(tuple(np.array(v) * S) for v in F.NECK)
    F.LEGX, F.LEGTOP, F.ANKLE, F.LEGY = F.LEGX * S, F.LEGTOP * S, F.ANKLE * S, F.LEGY * S
    F.LEGR = (F.LEGR[0] * S, F.LEGR[1] * S)
    F.TOE, F.HEEL, F.TOER = F.TOE * S, F.HEEL * S, F.TOER * S
    F.BEAK = tuple(tuple(np.array(v) * S) for v in F.BEAK)
    F.BEAKLOW = tuple(tuple(np.array(v) * S) for v in F.BEAKLOW)
    F.EYE = (F.EYE[0] * S, F.EYE[1] * S)
    F.R = F.R * S
    F.VOXEL = 0.0068
    F.CREAMLOW = [(1.55, 0.44), (1.55, 0.34), (1.8, 0.18), (3.2, 0.07), (3.2, -0.2)]
    F.SHOULDER = np.array([0.15, -0.025, 0.47])      # the wing's joint (0.156, -0.024, 0.468)
    return F


def crest(k, F):
    """
    A long sheaf of yellow feathers from the forehead laid back over the crown and the nape and on down the back, red
    streamers under and beside it, the longest trailing out past its end.
    """
    H = F.H
    k.paint(lambda P: p.inside(H[2] + 0.05 + 0.25 * (P[:, 1] - H[1]) - P[:, 2], 0.003) * (P[:, 1] < H[1] + 0.08)
            * (P[:, 2] > H[2]), GOLD, on=["head", "neck"])
    # red under the gold: two streamers along the sides of the crown, the longest out past the gold
    for s in (1, -1):
        k.add("plume", p.spike(H + (0.05 * s, -0.04, 0.045), H + (0.085 * s, 0.4, 0.035), 0.05, 0.028,
                               side=(1, 0, 0), at=0.3), RED, "plume", fuse=False)
    k.add("plume", p.spike(H + (0.02, 0.05, 0.05), H + (0.05, 0.55, 0.02), 0.04, 0.024, side=(1, 0, 0), at=0.25),
          RED, "plume", fuse=False)
    # the gold sheaf over them
    for x, z0, t, w in [(0.0, 0.07, (0.0, 0.42, 0.075), 0.09), (0.04, 0.058, (0.07, 0.36, 0.05), 0.075),
                        (-0.04, 0.058, (-0.07, 0.36, 0.05), 0.075)]:
        k.add("plume", p.spike(H + (x * 0.6, -0.075, z0), H + t, w, 0.03, side=(1, 0, 0), at=0.35), GOLD, "plume",
              fuse=False)


# The spread wing: its span raised 56 degrees and swept 16 back from the shoulder, its feathers hanging back and down,
# so the front three-quarter sees its cream underside and the camera behind its upper face
ELEV, SWEEP = 56, 16
SPAN = [(0.0, -0.02), (0.12, -0.035), (0.26, -0.03), (0.38, -0.01), (0.46, 0.03)]
TIPS = [(0.44, 0.11), (0.37, 0.18), (0.3, 0.22), (0.23, 0.23), (0.15, 0.22), (0.07, 0.19)]


def wing_axes():
    e, s = math.radians(ELEV), math.radians(SWEEP)
    d = np.array([math.cos(e) * math.cos(s), math.cos(e) * math.sin(s), math.sin(e)])
    c0 = np.array([0.35, 0.3, -0.9])
    c = c0 - d * (d @ c0); c /= np.linalg.norm(c)
    return d, c


def wing_outline():
    out_ = list(SPAN)
    for i, (a, b) in enumerate(TIPS):
        out_.append((a, b))
        nxt = TIPS[i + 1] if i + 1 < len(TIPS) else (0.0, 0.16)
        out_.append(((a + nxt[0]) / 2 + 0.01, (b + nxt[1]) / 2 - 0.045))     # the notch between two feathers
    out_.append((0.0, 0.16))
    return out_


def wings_spread(k, F, cream=p.CREAM, brown=p.BROWN):
    d, c = wing_axes()
    root = F.SHOULDER - d * 0.04
    k.add("wing", slab(wing_outline(), root, d, c, 0.022), cream, "wing{s}", fuse=False, mirror=True)
    # the brown band along the leading edge and over the shoulder, thicker so it shows on both faces
    band = [(0.0, -0.025), (0.12, -0.04), (0.26, -0.035), (0.38, -0.015), (0.43, 0.01), (0.34, 0.035),
            (0.2, 0.06), (0.08, 0.09), (0.0, 0.1)]
    k.add("wingband", slab(band, root, d, c, 0.03), brown, "wing{s}", fuse=False, mirror=True)
    # the shoulder fused into the body
    k.add("shoulder", p.ell(F.SHOULDER + np.array([-0.02, 0.0, -0.01]), (0.06, 0.07, 0.06)), brown, "wing{s}",
          mirror=True)


def pidgeot(src, out):
    F = frame()
    k = Kit(src, coat="fur")
    p.body(k, F)
    p.face(k, F)
    p.beak(k, F)
    crest(k, F)
    wings_spread(k, F)
    p.tail(k, F, [(0, 0.25, 0.085, REDTAIL, None), (10, 0.235, 0.08, RED, None), (-10, 0.235, 0.08, RED, None),
                  (20, 0.22, 0.078, REDTAIL, None), (-20, 0.22, 0.078, REDTAIL, None)], thick=0.022)
    o.thighs(k, F, size=(0.058, 0.066, 0.072))
    p.legs(k, F)
    k.build(voxel=F.VOXEL)
    k.eye_states(o.eyes(k, F))
    k.export(out)


if __name__ == "__main__":
    pidgeot(*args())
