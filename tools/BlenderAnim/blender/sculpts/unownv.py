# Unown's letter V, built by unown.py's letter() and eye() (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unownv.py -- <the export's Unown-V.glb> <out.glb>
#
# The design, from the 3D picture (201_Unown-V_3d.png): the eye's ring at the bottom, and over it a closed loop of a
# thick stroke, a rounded triangle with its point down on the ring's top and its two upper corners wide apart (the
# right one further out), its sides bowed a little out; from the loop's upper-right corner a short stick up and out to
# +X. The loop is a smooth closed sweep (a description's lines would show their joints on so round a shape).
#
# Measured on the picture: the ring's outside 235 px across with its middle at (202, 337); its bottom (452 px) is set
# at the export's lowest point (0.073), which gives the scale. The white is 0.72 of the ring's outer radius.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import sibling, args, Kit
u = sibling("unown")

EX, EZ = 0.0, 0.2                       # the body bone: the eye's middle
CX, CY = 202, 337                       # the eye's middle on the picture
K = (EZ - 0.073) / (452 - CY)           # model units a pixel


def P(px, py):
    return (round(EX + (px - CX) * K, 4), round(EZ + (CY - py) * K, 4))


def spline(pts, n=10, closed=False):
    """Catmull-Rom through the points, n samples a span."""
    p = [np.array(q, float) for q in pts]
    if closed:
        p = [p[-1]] + p + [p[0], p[1]]
    else:
        p = [2 * p[0] - p[1]] + p + [2 * p[-1] - p[-2]]
    out = []
    for i in range(1, len(p) - 2):
        p0, p1, p2, p3 = p[i - 1], p[i], p[i + 1], p[i + 2]
        for t in np.linspace(0, 1, n, endpoint=False):
            out.append(0.5 * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    if not closed:
        out.append(p[-2])
    return out


R = 118 * K                             # the ring's outer radius
WHITE = 0.72 * R
LOOP = [P(165, 215), P(130, 150), P(118, 82), P(152, 58), P(240, 56), P(330, 58), P(370, 82), P(322, 152),
        P(255, 220), P(210, 222)]       # the loop's centre line, round from its bottom on the ring's top
LOOP_W = 0.044

UNOWN_V = {
    "eye": {"at": (EX, EZ), "white": round(WHITE, 4), "ring": round(R - WHITE - u.GAP, 4)},
    "strokes": [
        ("line", [P(366, 84), P(410, 52)], (0.042, 0.036)),            # the stick out of the loop's corner
    ],
}

if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    u.letter(k, UNOWN_V)
    k.add("loop", u.sweep(spline(LOOP, 8, closed=True), LOOP_W, closed=True), u.BODY, u.BONE)
    k.build(voxel=0.0035, smooth=3)
    k.eye_states(lambda state: u.eye(k, state, UNOWN_V))
    k.export(out)
