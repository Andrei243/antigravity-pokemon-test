# Lickilicky sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Lickitung's script (lickitung.py): the same pink, the same head with its snout and jaw, the same black eyes with a
# white glint, the same lighter flat tongue out of a mouth carved under the snout, the same banded belly painted round
# the body's axis and the same white nails.
#   blender --background --factory-startup --python sculpts/lickilicky.py -- <the export's Lickilicky.glb> <out.glb>
#
# The design, from the references: a great pink ball of a body; the head a dome sunk into its top with no neck, a
# broad upper lip over a nearly shut mouth, and a thick curl on the crown; tiny black eyes with a glint, far apart; a
# broad flat tongue out of the mouth, lying forward, down and off to the model's left; a white bib under the chin from
# shoulder to shoulder, its hem zigzag; three yellow bands across the lower belly curving up at the sides; short
# stubby arms carried forward, a white claw on each; short legs with a white nail on each foot; a short thick tail low
# behind, swung out to +X, its end blunt and turned up.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, boxy, tri, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

PINK, CREAM, TONGUE, MOUTH, INKH, WHITE = "#EE98AC", "#F6E6AE", "#F4C2D2", "#6E2236", "#16141A", "#F8F6F2"
YELLOW, BIB = "#F2D24A", "#FAF8F4"
B = np.array([0, 0.0, 0.37])              # the body's middle
BR = np.array([0.28, 0.25, 0.31])
H = np.array([0, -0.05, 0.68])            # the head's middle
HR = np.array([0.15, 0.12, 0.1])


def hit(shape, o, d):
    """Where a ray from o along d first meets a part's own mesh: places on a part before the body is fused."""
    V, F = shape
    h = BVHTree.FromPolygons(V.tolist(), F).ray_cast(Vector(o), Vector(unit(*d)))
    return np.array(h[0])


def round_body(P, c=B, r=BR):
    """(the arc round the body's upright axis, z): the coordinates the belly's paint is measured in."""
    return np.arctan2(P[:, 0], -(P[:, 1] - c[1])) * r[0], P[:, 2]


def ball(V):
    """The great body: a ball, a little squared and settled on its seat."""
    V = boxy(2.15)(V)
    s = 1 + 0.04 * np.clip(-V[:, 2], 0, 1) - 0.06 * np.clip(V[:, 2], 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    return V


def head_shape(V):
    """Lickitung's round head, a little flattened over the crown."""
    V = boxy(2.2)(V)
    V[:, 2] = np.where(V[:, 2] > 0, V[:, 2] * (1 - 0.08 * V[:, 2]), V[:, 2])
    return V


def head(c, r, snout, snout_r, jaw, jaw_r, bone="head"):
    """The head, the snout overhanging the mouth and the lower jaw under it, all one fused mass with the body."""
    k.add("head", ell(c, r, deform=head_shape), PINK, bone)
    k.add("snout", ell(snout, snout_r), PINK, bone)
    k.add("jaw", ell(jaw, jaw_r), PINK, bone)


def mouth(c, r):
    """The mouth, carved under the snout's overhang and lined dark."""
    k.add("mouth", ell(c, r, deform=boxy(2.6)), MOUTH, "head", carve=True)


def tongue(pts, radii, axis, flat=0.55):
    """The tongue: a loose broad flat piece out of the mouth, a lighter pink, hung from the head."""
    k.add("tongue", blade(pts, radii, axis, flat), TONGUE, "head", fuse=False, surface="skin")


def bands(stripes, colour, front_y, edge=0.004, cb=B, cr=BR):
    """Bands across the belly curving up at the sides ((z, bend, half length in arc, half width) each), lens-ended,
    painted round the body's axis with a crisp edge and no `on`."""
    for z0, bend, L, w in stripes:
        def band(P, z0=z0, bend=bend, L=L, w=w):
            s, z = round_body(P, cb, cr)
            return inside(np.hypot(s / L, (z - (z0 + bend * s * s)) / w) - 1, edge) * (P[:, 1] < front_y)
        k.paint(band, colour)


def nail(at, r, bone, mirror=True):
    k.add("nail", ell(at, r, deform=boxy(2.4)), WHITE, bone, fuse=False, mirror=mirror, surface="shell")


# --- the great body and the head sunk into its top, with no neck --------------------------------------------------
BODY = ell(B, BR, deform=ball)
k.add("body", BODY, PINK, "body")
k.add("nape", ell((0, -0.02, 0.6), (0.17, 0.15, 0.1)), PINK, "head")
head(H, HR, (0, -0.14, 0.665), (0.14, 0.07, 0.048), (0, -0.115, 0.6), (0.13, 0.08, 0.045))
mouth((0, -0.188, 0.628), (0.045, 0.045, 0.018))

# --- the curl on the crown: a thick tube rising and coiling over to the model's +X side, its face to the front -----
CC = np.array([0.04, 0.0, 0.81])
CURL_X = np.array([math.cos(math.radians(35)), math.sin(math.radians(35)), 0])   # the coil's plane turned 35 degrees
pts = []
for i in range(9):
    t = i / 8
    a = math.pi * 1.15 - t * 1.75 * math.pi      # from the left, over the top, round and in
    r = 0.055 - 0.03 * t
    pts.append(CC + r * (math.cos(a) * CURL_X + np.array([0, 0, math.sin(a)])))
pts = [np.array([-0.02, -0.01, 0.73])] + pts
k.add("curl", tube(pts, [0.062] + [0.05 - 0.022 * i / 8 for i in range(9)]), PINK, "head")

# --- arms: short and stubby, carried forward, a white claw on each ------------------------------------------------
SH, HAND = np.array([0.22, -0.04, 0.47]), np.array([0.29, -0.17, 0.42])
k.add("arm", tube([SH, (0.265, -0.11, 0.455), HAND], [0.075, 0.068, 0.062]), PINK, "arm{s}", mirror=True)
k.add("hand", ell(HAND + (0.008, -0.012, 0), (0.06, 0.056, 0.055)), PINK, "arm{s}", mirror=True)
nail(HAND + (0.03, -0.045, -0.045), (0.018, 0.016, 0.013), "arm{s}")

# --- legs: short and thick, fused into the ball's bottom, a white nail on each foot -------------------------------
k.add("thigh", ell((0.12, -0.03, 0.13), (0.105, 0.1, 0.1)), PINK, "leg{s}", mirror=True)
k.add("foot", ell((0.125, -0.08, 0.034), (0.08, 0.095, 0.036)), PINK, "leg{s}", mirror=True)
nail((0.125, -0.172, 0.028), (0.036, 0.02, 0.016), "leg{s}")

# --- the tail: short and thick, low behind, swung out to +X, its end blunt and turned up ---------------------------
TAIL = [(0.0, 0.2, 0.2), (0.06, 0.27, 0.13), (0.12, 0.31, 0.1), (0.16, 0.33, 0.12), (0.17, 0.335, 0.16)]
k.add("tail", blade(TAIL, [0.105, 0.095, 0.085, 0.08, 0.065], (0.82, -0.57, 0), 0.85), PINK, "tail")

# --- the tongue: broad and flat, out of the mouth, lying forward, down and off to the model's left ----------------
tongue([(0, -0.15, 0.63), (-0.02, -0.235, 0.615), (-0.065, -0.29, 0.56), (-0.12, -0.31, 0.49),
        (-0.16, -0.3, 0.44)],
       [0.045, 0.075, 0.085, 0.085, 0.072], unit(0.25, -0.55, 0.8), 0.42)

# --- the white bib under the chin, shoulder to shoulder, its hem zigzag -------------------------------------------
def bib(P):
    s, z = round_body(P)
    hem = 0.5 - 0.05 * (tri(s / 0.11 + 0.5) + 1) / 2 - 0.03 * inside(np.abs(s) - 0.06, 0.03)
    return (inside(hem - z, 0.0035) * inside(np.abs(s) - 0.22, 0.0035) * inside(z - 0.575, 0.0035)
            * (P[:, 1] < -0.06))
k.paint(bib, BIB)

# --- three yellow bands across the lower belly, each longer and wider than the one above ---------------------------
bands([(0.38, 0.9, 0.1, 0.02), (0.29, 0.9, 0.13, 0.024), (0.195, 0.9, 0.155, 0.028)], YELLOW, front_y=-0.08,
      edge=0.0035)

# --- the eyes: tiny, black with a white glint, far apart over the snout -------------------------------------------
INK, LID = srgb(INKH), srgb(PINK)
def eye(u, v):
    c = np.tile(INK, (len(u), 1))
    c[((u - 0.2) / 0.36) ** 2 + ((v - 0.3) / 0.36) ** 2 < 1] = (1, 1, 1)
    return c
de = unit(0.5, -0.76, 0.42)
E = H + np.array([0, -0.01, 0.025]) + de * 0.1
def eyes(state):
    k.decal(E, de, (0.016, 0.019), eye_look(state, eye, LID, INK, line=0.22), eps=0.004, rings=20, segs=56,
            mirror=True)

k.build(voxel=0.007, smooth=6)
k.eye_states(eyes)
k.export(out)
