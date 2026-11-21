# Zweilous sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/zweilous.py -- <the export's Zweilous.glb> <out.glb>
# Grown from deino.py (its heads, mantle, colours, by import). From the references: a squat blue dragon on four short
# splayed legs with white claws, a dark red band under its belly; a great shaggy mantle spotted pink over its shoulders,
# small ragged grey-brown wings spread from it; two necks, each with a big head under a bowl of hair with two horns
# in a V, the eyes hidden by the locks: the left head roaring (jaw dropped, pink inside, white fangs), the right with
# its mouth shut on a zigzag line. A short blue tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, unit, args, sibling

d = sibling("deino")
BLUE, MOP, SPOT, TOOTH = d.BLUE, d.MOP, d.SPOT, d.TOOTH
BELLY = "#8E1E44"
CLAW = "#EEEDEA"


def zweilous(src, out):
    k = Kit(src, coat="fur")

    # the body: broad, squat, the belly low; a dark red band under it
    k.add("body", ell((0, 0.02, 0.165), (0.125, 0.15, 0.095)), BLUE, "body")
    k.add("chest", ell((0, -0.08, 0.18), (0.11, 0.08, 0.085)), BLUE, "body")
    k.paint(lambda P: inside(P[:, 2] - 0.112, 0.003) * inside(np.abs(P[:, 0]) - 0.085, 0.003)
            * inside(np.abs(P[:, 1] - 0.0) - 0.15, 0.004), BELLY, on=["body", "chest"])
    # four short splayed legs: thick thighs fused into the flanks, a stub of shin, a broad foot with three claws
    for y, bone, big in ((-0.09, "leg{s}", 1.0), (0.09, "hind{s}", 1.1)):
        k.add(f"thigh{bone[0]}", ell((0.1, y, 0.13), (0.055 * big, 0.06 * big, 0.065)), BLUE, bone, mirror=True)
        k.add(f"shin{bone[0]}", tube([(0.125, y - 0.005, 0.1), (0.148, y - 0.012, 0.045)], [0.045, 0.04]), BLUE,
              bone, mirror=True)
        k.add(f"foot{bone[0]}", ell((0.152, y - 0.026, 0.022), (0.048, 0.058, 0.024)), BLUE, bone, mirror=True)
        for j, dx in enumerate((-0.026, 0.0, 0.026)):
            b = np.array((0.152 + dx, y - 0.074, 0.016))
            k.add(f"claw{bone[0]}{j}", d.spike(b, unit(dx * 4, -1, -0.15), 0.028, 0.02, (0, 0, 1)), CLAW, bone,
                  fuse=False, mirror=True, surface="shell")
    # the tail: short and thick, back and down, with a small spike on it
    k.add("tail", tube([(0, 0.14, 0.19), (0, 0.21, 0.16), (0, 0.27, 0.12)], [0.05, 0.034, 0.014]), BLUE, "tail")
    k.add("tailspike", d.spike((0, 0.2, 0.2), unit(0, 0.8, 1), 0.035, 0.024, (1, 0, 0)), BLUE, "tail", fuse=False,
          surface="scales")

    # the mantle over the shoulders, spotted pink
    d.mantle(k, (0, -0.04, 0.265), (0.12, 0.125, 0.07), seed=6, tufts=80,
             spots=[((0.85, -0.25, 0.25), 0.02, (1, 1, 0.7)), ((-0.8, -0.35, 0.2), 0.018, (1, 1, 0.7)),
                    ((0.55, 0.55, 0.5), 0.017, (1, 1, 0.7)), ((-0.5, 0.6, 0.55), 0.016, (1, 1, 0.7)),
                    ((0.25, -0.9, 0.05), 0.014, (1, 1, 0.8)), ((-0.15, 0.1, 1.0), 0.015, (1, 1, 0.8))])

    # small ragged wings spread out of the mantle: a mirror pair raised in a V and swept back
    out_ = unit(1.0, 0.45, 0.55)
    up_ = np.cross(out_, (0, 1.0, 0)); up_ /= np.linalg.norm(up_)
    up_ = -up_ if up_[2] < 0 else up_
    WING = [(0.0, -0.035), (0.07, -0.042), (0.15, -0.03), (0.12, -0.012), (0.19, 0.0), (0.135, 0.016),
            (0.17, 0.042), (0.1, 0.036), (0.07, 0.055), (0.0, 0.035)]
    k.add("wing", slab(WING, (0.09, 0.03, 0.3), out_, up_, 0.018), MOP, "wing{s}", fuse=False, mirror=True,
          surface="fur")

    # two necks rising out of the mantle, each with a head
    for s, tag in ((1, "R"), (-1, "L")):
        k.add(f"neck{tag}", tube([(0.035 * s, -0.06, 0.28), (0.07 * s, -0.1, 0.35), (0.098 * s, -0.125, 0.395)],
                                 [0.042, 0.038, 0.045]), MOP, f"head{tag}")
    heads = [
        d.Head(k, "L", "headL", (-0.105, -0.14, 0.43), unit(-0.3, -1, -0.12), 0.088,
               horns=[(-0.45, 0.4, 0.75), (0.35, 0.4, 0.7)], gape=1.2, fangs=True),
        d.Head(k, "R", "headR", (0.105, -0.14, 0.43), unit(0.3, -1, -0.12), 0.088,
               horns=[(0.45, 0.4, 0.75), (-0.35, 0.4, 0.7)], gape=0.0),
    ]
    heads[1].mouth_line()

    k.build(voxel=0.006, smooth=4, band=0.005)
    k.eye_states(lambda state: [h.lay(state) for h in heads])
    k.export(out)


if __name__ == "__main__":
    zweilous(*args())
