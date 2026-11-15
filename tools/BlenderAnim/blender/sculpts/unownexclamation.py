# Unown's exclamation mark, built by unown.py's letter() with an eye of its own (sculptkit.py):
#   blender --background --factory-startup --python sculpts/unownexclamation.py -- <the export's Unown-Exclamation.glb> <out.glb>
#
# The design, from the 3D picture (201_Unown-Exclamation_3d.png): the eye's ring is the mark's dot, at the bottom, and
# out of its top a long straight stroke rises, leaning a little to +X and narrowing to its rounded end. There is no
# dot apart from the body. The eye is half shut, a sleepy look: a heavy dark lid covers the top of the white down to a
# straight edge a little over the middle, with a black line along that edge, and a small pupil just under it, a little
# to the viewer's left. The question mark's script (unownquestion.py) takes its eye from here.
#
# The eye is four patches, each with its own crisp outline (a line in a colour function steps at the patch's rings):
# the white cut by the lid's edge, the lid over the rest, the pupil cut by the edge, and the edge's black band over
# them. Shut, Squeeze and Smile are the whole disc in the lid's colour with eye_look's line; Fierce slants the lid.
#
# Measured on the picture: the ring's outside 235 px across with its middle at (258, 348); its bottom (465 px) is set
# at the export's lowest point (0.079), which gives the scale. The white is 0.72 of the ring's outer radius.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import sibling, args, Kit, srgb, eye_look, polygon
u = sibling("unown")

EX, EZ = 0.0, 0.206                     # the body bone: the eye's middle
CX, CY = 258, 348                       # the eye's middle on the picture
K = (EZ - 0.079) / (465 - CY)           # model units a pixel


def P(px, py):
    return (round(EX + (px - CX) * K, 4), round(EZ + (CY - py) * K, 4))


R = 117 * K
WHITE = 0.72 * R

UNOWN_EXCLAMATION = {
    "eye": {"at": (EX, EZ), "white": round(WHITE, 4), "ring": round(R - WHITE - u.GAP, 4)},
    "strokes": [
        ("line", [P(270, 232), P(287, 140), P(305, 52)], (0.050, 0.040)),     # the stroke up from the dot
    ],
}

# --- the sleepy eye ----------------------------------------------------------------------------------------------
LID = "#3B3F49"             # the lid: a little lighter than the body, so the line along its edge reads
CL, CP = srgb(LID), srgb(u.PUPIL)
INK = 0.05                  # half the black band's thickness along the lid's edge, in the white's radius
PUPIL_R = 0.19              # the pupil's radius, in the white's radius: small, as the picture's
PUPIL_U = -0.14             # where it looks: a little to the viewer's left, just under the lid


def edge(state):
    """The lid's lower edge as v = a + b u (u to the viewer's right, v up, the white's radius 1): level a little over
    the middle when open, slanted down to the viewer's right in Fierce."""
    return (0.12, 0.0) if state != "Fierce" else (0.08, -0.34)


def circle(n=160, c=(0.0, 0.0), r=1.0):
    th = np.linspace(0, 2 * math.pi, n, endpoint=False)
    return [(c[0] + r * math.cos(t), c[1] + r * math.sin(t)) for t in th]


def clip(poly, a, b, below):
    """A convex polygon cut by the line v = a + b u, keeping what is below it (or above)."""
    def side(p):
        d = p[1] - (a + b * p[0])
        return -d if below else d
    out = []
    for i, p in enumerate(poly):
        q = poly[(i + 1) % len(poly)]
        sp, sq = side(p), side(q)
        if sp >= 0:
            out.append(p)
        if (sp >= 0) != (sq >= 0):
            t = sp / (sp - sq)
            out.append((p[0] + (q[0] - p[0]) * t, p[1] + (q[1] - p[1]) * t))
    return out


def patch(k, at, r, pts, color, eps, rings=20, segs=56):
    """A decal of a convex region given in the white's units (unit circle about the eye's middle at `at`, radius r):
    centred on the region's box, its outline in the box's own units."""
    Q = np.array(pts, float)
    lo, hi = Q.min(0), Q.max(0)
    c, h = (lo + hi) / 2, np.maximum((hi - lo) / 2, 1e-4)
    loc = (Q - c) / h
    x, z = at
    k.decal((x + c[0] * r, -0.01, z + c[1] * r), (0, -1, 0), (h[0] * r, h[1] * r), color, eps=eps, rings=rings,
            segs=segs, outline=polygon([tuple(p) for p in loc]))


def sleepy(k, state, desc):
    """The mark's eye in one of the five states."""
    e = desc["eye"]
    at, r = e["at"], e["white"]
    disc = circle()
    if state == "Shut":
        # the lid down over the whole eye and its black edge, straight, low across it: crisp as the open lid's edge
        # (eye_look's curved line steps at the patch's rings on an eye this large)
        patch(k, at, r, disc, LID, 0.003, rings=24, segs=112)
        patch(k, at, r, clip(clip(disc, -0.25 + 1.6 * INK, 0.0, True), -0.25 - 1.6 * INK, 0.0, False), u.PUPIL, 0.0048,
              rings=6, segs=112)
        return
    if state not in ("Open", "Fierce"):
        x, z = at
        k.decal((x, -0.01, z), (0, -1, 0), (r, r), eye_look(state, u.eye_white, CL, CP, line=0.1), eps=0.003,
                rings=40, segs=112)
        return
    a, b = edge(state)
    patch(k, at, r, clip(disc, a, b, True), u.WHITE, 0.003, rings=30, segs=112)            # the white under the lid
    patch(k, at, r, clip(disc, a, b, False), LID, 0.003, rings=24, segs=112)               # the lid
    pv = a + b * PUPIL_U - INK - PUPIL_R * 0.55
    patch(k, at, r, clip(circle(72, (PUPIL_U, pv), PUPIL_R), a, b, True), u.PUPIL, 0.0042, rings=8, segs=72)
    band = clip(clip(disc, a + INK, b, True), a - INK, b, False)                            # the lid's black edge
    patch(k, at, r, band, u.PUPIL, 0.0048, rings=6, segs=112)


def mark(src, out, desc, more=None):
    k = Kit(src, coat="fur")
    u.letter(k, desc)
    if more:
        more(k)
    k.build(voxel=0.0035, smooth=3)
    k.eye_states(lambda state: sleepy(k, state, desc))
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    mark(src, out, UNOWN_EXCLAMATION)
