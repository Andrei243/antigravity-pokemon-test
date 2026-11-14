# Lickitung sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lickitung.py -- <the export's Lickitung.glb> <out.glb>
#
# The design, from the references: a pink round-bellied biped, wider low down, its head on a short thick neck; a
# rounded snout overhanging a wide open mouth, dark inside, out of which hangs a long, broad, flat tongue a lighter
# pink, down the front and off to the model's left, its end curling up and out; small black eyes with a white glint
# high on the head over the snout; the belly cream, crossed by two pink stripes that curve up at the sides; a cream
# ring on the outside of each thigh; short thick arms held out and a little forward, a white claw under each hand;
# thick legs fused into the body, each foot with a white nail; a big thick tail rising off the back and running down
# to a point on the ground behind, swung out to the model's +X side.
#
# Written to be grown from (lickilicky.py): the frame is in named constants and small functions (the head and its
# snout, the mouth, the tongue, the eye, the bands of the belly, the nails).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

PINK, CREAM, TONGUE, MOUTH, INKH, WHITE = "#EE98AC", "#F6E6AE", "#F4C2D2", "#6E2236", "#16141A", "#F8F6F2"
B = np.array([0, 0.0, 0.29])              # the body's middle
BR = np.array([0.185, 0.165, 0.235])
H = np.array([0, -0.05, 0.6])             # the head's middle
HR = np.array([0.112, 0.105, 0.095])


def hit(shape, o, d):
    """Where a ray from o along d first meets a part's own mesh: places on a part before the body is fused."""
    V, F = shape
    h = BVHTree.FromPolygons(V.tolist(), F).ray_cast(Vector(o), Vector(unit(*d)))
    return np.array(h[0])


def round_body(P, c=B, r=BR):
    """(the arc round the body's upright axis, z): the coordinates the belly's paint is measured in."""
    return np.arctan2(P[:, 0], -(P[:, 1] - c[1])) * r[0], P[:, 2]


def pear(V):
    """The body: fuller low down, narrower under the neck."""
    V = V.copy()
    s = 1 + 0.1 * np.clip(-V[:, 2], 0, 1) ** 0.8 - 0.14 * np.clip(V[:, 2], 0, 1) ** 1.4
    V[:, 0] *= s; V[:, 1] *= s
    return V


def head_shape(V):
    """A round head, a little flattened over the crown."""
    V = boxy(2.2)(V)
    V[:, 2] = np.where(V[:, 2] > 0, V[:, 2] * (1 - 0.08 * V[:, 2]), V[:, 2])
    return V


def head(c, r, snout, snout_r, jaw, jaw_r, bone="head"):
    """The head, the snout overhanging the mouth and the lower jaw under it, all one fused mass with the body."""
    k.add("head", ell(c, r, deform=head_shape), PINK, bone)
    k.add("snout", ell(snout, snout_r), PINK, bone)
    k.add("jaw", ell(jaw, jaw_r), PINK, bone)


def mouth(c, r):
    """The open mouth, carved under the snout's overhang and lined dark."""
    k.add("mouth", ell(c, r, deform=boxy(2.6)), MOUTH, "head", carve=True)


def tongue(pts, radii, axis, flat=0.55):
    """The tongue: a loose broad flat piece out of the mouth, a lighter pink, hung from the head."""
    k.add("tongue", blade(pts, radii, axis, flat), TONGUE, "head", fuse=False, surface="skin")


def bands(centre, half, stripes, ground, mark, front_y, edge=0.004, cb=B, cr=BR):
    """
    The belly: an oval of `mark` (centre z, half sizes in arc and z), crossed by stripes of `ground` that curve up at
    the sides ((z, bend, half width) each), painted round the body's axis with a crisp edge and no `on`.
    """
    def oval(P):
        s, z = round_body(P, cb, cr)
        return inside(np.hypot(s / half[0], (z - centre) / half[1]) - 1, edge * 2) * (P[:, 1] < front_y)
    k.paint(oval, mark)
    for z0, bend, w in stripes:
        def stripe(P, z0=z0, bend=bend, w=w):
            s, z = round_body(P, cb, cr)
            return inside(np.abs(z - (z0 + bend * s * s)) - w, edge) * oval(P)
        k.paint(stripe, ground)


def nail(at, r, bone, mirror=True):
    k.add("nail", ell(at, r, deform=boxy(2.4)), WHITE, bone, fuse=False, mirror=mirror, surface="shell")


# --- the body, the neck and the head -------------------------------------------------------------------------------
BODY = ell(B, BR, deform=pear)
k.add("body", BODY, PINK, "body")
k.add("neck", ell((0, -0.05, 0.5), (0.12, 0.115, 0.1)), PINK, "head")
head(H, HR, (0, -0.122, 0.602), (0.09, 0.076, 0.05), (0, -0.09, 0.512), (0.092, 0.078, 0.045))
mouth((0, -0.166, 0.548), (0.08, 0.07, 0.032))

# --- arms: short and thick, held out and a little forward and down, a white claw under each hand ------------------
SH, HAND = np.array([0.15, -0.04, 0.42]), np.array([0.245, -0.135, 0.36])
k.add("arm", tube([SH, (0.2, -0.09, 0.4), HAND], [0.06, 0.055, 0.05]), PINK, "arm{s}", mirror=True)
k.add("hand", ell(HAND + (0.012, -0.008, 0), (0.05, 0.046, 0.044)), PINK, "arm{s}", mirror=True)
nail(HAND + (0.03, -0.035, -0.04), (0.016, 0.014, 0.012), "arm{s}")

# --- legs: thick thighs fused into the body, short shins, broad feet with a white nail ----------------------------
THIGH = ell((0.11, -0.02, 0.15), (0.105, 0.1, 0.11))
k.add("thigh", THIGH, PINK, "leg{s}", mirror=True)
k.add("shin", tube([(0.11, -0.03, 0.12), (0.115, -0.05, 0.075)], [0.075, 0.07]), PINK, "leg{s}", mirror=True)
k.add("foot", ell((0.115, -0.07, 0.032), (0.075, 0.09, 0.034)), PINK, "leg{s}", mirror=True)
nail((0.115, -0.158, 0.026), (0.034, 0.02, 0.016), "leg{s}")

# --- the tail: thick, rising off the back, running down to a point on the ground behind and out to +X ------------
TAIL = [(0.0, 0.11, 0.33), (0.05, 0.2, 0.3), (0.1, 0.265, 0.19), (0.135, 0.305, 0.08), (0.15, 0.325, 0.04)]
k.add("tail", blade(TAIL, [0.13, 0.125, 0.1, 0.066, 0.04], (0.82, -0.57, 0), 0.85), PINK, "tail")

# --- the tongue: out of the mouth, down the front and off to the model's left, its end curling up and out --------
# Broad and flat, some 45% of the head's width at the mouth and a little wider at its rounded end, hanging to the
# viewer's left of the belly down to about the knee's ring, its tip curled a little; a darker line down its middle
TONGUE_PTS = [(0, -0.11, 0.548), (-0.01, -0.2, 0.53), (-0.04, -0.24, 0.45), (-0.075, -0.25, 0.35),
              (-0.1, -0.24, 0.25), (-0.12, -0.225, 0.17), (-0.15, -0.21, 0.14), (-0.175, -0.2, 0.16)]
tongue(TONGUE_PTS, [0.035, 0.048, 0.05, 0.052, 0.054, 0.057, 0.052, 0.04], (0.15, 1, 0.1), flat=0.5)
GROOVE = np.array(TONGUE_PTS[2:6]) + np.array([0, -0.024, 0.0])
k.add("groove", tube(GROOVE, [0.0045, 0.005, 0.005, 0.004]), "#D486A0", "head", fuse=False, surface="skin")

# --- the belly: cream, crossed by two pink stripes that curve up at the sides --------------------------------------
bands(0.305, (0.15, 0.225), [(0.37, 0.9, 0.019), (0.23, 0.9, 0.019)], PINK, CREAM, front_y=-0.04)

# --- the cream ring on the outside of each thigh: two stacked patches ----------------------------------------------
dk = unit(0.62, -0.78, 0.1)
KC = hit(THIGH, np.array([0.11, -0.02, 0.15]) + dk * 0.3, -dk)
k.decal(KC, dk, (0.034, 0.034), CREAM, eps=0.004, rings=10, segs=48, mirror=True)
k.decal(KC, dk, (0.019, 0.019), PINK, eps=0.007, rings=8, segs=48, mirror=True)

# --- the eyes: small, black with a white glint, high on the head over the snout -----------------------------------
INK, LID = srgb(INKH), srgb(PINK)
def eye(u, v):
    c = np.tile(INK, (len(u), 1))
    c[((u - 0.2) / 0.36) ** 2 + ((v - 0.3) / 0.36) ** 2 < 1] = (1, 1, 1)
    return c
de = unit(0.42, -0.78, 0.46)
E = H + np.array([0, -0.02, 0.03]) + de * 0.1
def eyes(state):
    k.decal(E, de, (0.021, 0.025), eye_look(state, eye, LID, INK, line=0.22), eps=0.004, rings=20, segs=56,
            mirror=True)

k.build(voxel=0.0065, smooth=6)
k.eye_states(eyes)
k.export(out)
