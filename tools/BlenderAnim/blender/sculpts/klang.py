# Klang sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# klink.py (its gear, plate, core, face and colours):
#   blender --background --factory-startup --python sculpts/klang.py -- <the export's Klang.glb> <out.glb>
#
# From the references: a great darker grey gear of ten teeth behind; on its front a pale gear of seven teeth with a
# large teal ball in its middle (no face); round that pale gear a thin black circle drawn on the great gear's face,
# with a black square on it at the upper left, a black dot at the right and a white chevron at the lower left (the
# great gear's face). At the upper left, in front, a small pale gear with Klink's face (eye, white plus, mouth) and
# its teal ball.
#
# Written to be grown from (klinklang.py imports it with sibling); the build is under the main guard.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, args, slab, sibling

kl = sibling("klink")
MARK_T = 0.004          # the great gear's marks: plates this thick, standing out of its face


def flat(c, fr, outline, lift):
    """A thin plate lying on a gear's face: outline (a, b) in the face's plane about c, lift out along its normal."""
    ex, ez, n = fr
    return slab(outline, np.array(c, float) + n * lift, ex, ez, MARK_T)


def arc_outline(r0, r1, a0, a1, k=40):
    """A band between two radii from a0 to a1 degrees (a0 < a1), as one outline."""
    t = np.radians(np.linspace(a0, a1, k))
    outer = [(r1 * math.cos(x), r1 * math.sin(x)) for x in t]
    inner = [(r0 * math.cos(x), r0 * math.sin(x)) for x in t[::-1]]
    return outer + inner


def shift(pts, p):
    return [(a + p[0], b + p[1]) for a, b in pts]


def turn(pts, deg):
    t = math.radians(deg)
    c, s = math.cos(t), math.sin(t)
    return [(a * c - b * s, a * s + b * c) for a, b in pts]


def polar(rad, deg):
    t = math.radians(deg)
    return (rad * math.cos(t), rad * math.sin(t))


def great_face(k, bone, c, fr, thick, line_r, arc=(0, 360), square_at=None, dot_at=None, chevron_at=None, size=0.013):
    """The great gear's face: a thin black line round its middle (an arc, or a whole circle in two halves), a black
    square, a black dot and a white chevron on it, each a thin loose plate."""
    lift = thick / 2 + MARK_T / 2 - 0.0005
    w = 0.0045
    a0, a1 = arc
    if a1 - a0 >= 359:
        spans = [(a0, a0 + 180), (a0 + 180, a0 + 360)]
    else:
        spans = [(a0, a1)]
    for i, (s0, s1) in enumerate(spans):
        k.add(f"line{i}", flat(c, fr, arc_outline(line_r - w, line_r + w, s0, s1), lift), kl.INK, bone, fuse=False,
              surface="metal")
    if square_at is not None:
        sq = turn([(-size, -size), (size, -size), (size, size), (-size, size)], square_at + 15)
        k.add("square", flat(c, fr, shift(sq, polar(line_r, square_at)), lift + 0.0004), kl.INK, bone, fuse=False,
              surface="metal")
    if dot_at is not None:
        d = [polar(size * 1.1, a) for a in range(0, 360, 15)]
        k.add("dot", flat(c, fr, shift(d, polar(line_r, dot_at)), lift + 0.0004), kl.INK, bone, fuse=False,
              surface="metal")
    if chevron_at is not None:
        L, s = 2.4 * size, 0.95 * size       # a chevron opening up and to the right, its corner at the line
        ch = [(0, 0), (L, 0), (L, s), (s, s), (s, L), (0, L)]
        ch = shift(ch, (-0.3 * size, -0.3 * size))
        k.add("chevron", flat(c, fr, shift(turn(ch, 10), polar(line_r + 0.008, chevron_at)), lift + 0.0004), kl.WHITE,
              bone, fuse=False, surface="metal")


def great_gear(k, bone, c, r, fr, thick, hub_c, hub_r, hub_thick, teeth=10, hub_teeth=7, turn_=0.0, hub_turn=0.0):
    """The great gear and the pale gear standing on its front with its big teal ball."""
    kl.gear(k, "great", bone, c, r, teeth, fr, thick, colour=kl.STEEL, turn=turn_)
    kl.gear(k, "hub", bone, hub_c, hub_r, hub_teeth, fr, hub_thick, colour=kl.PALE, turn=hub_turn)
    kl.core(k, "hubcore", bone, hub_c, hub_r, fr, hub_thick, share=0.43)


# --- Klang ---------------------------------------------------------------------------------------------------------
FR = kl.FR
SMALL_R, SMALL_T = 0.106, 0.032
SMALL = np.array([-0.15, -0.013, 0.401])           # the head bone's gear, in front
GREAT_R, GREAT_T = 0.18, 0.042
GREAT = np.array([0.045, 0.030, 0.287])
HUB_R, HUB_T = 0.104, 0.030
HUB = np.array([0.04, -0.0085, 0.289])
FACE = (12, 120, -10, 25, 225, 30)                  # as klink's: teeth's turn, eye, plus, plus's roll, mouth, roll


def klang(src, out):
    k = Kit(src, coat="metal")
    turn_, e, p, pr, m, mr = FACE
    kl.faced_gear(k, "small", "head", SMALL, SMALL_R, 7, FR, SMALL_T, turn_, p, pr, m, mr)
    great_gear(k, "body", GREAT, GREAT_R, FR, GREAT_T, HUB, HUB_R, HUB_T, turn_=9, hub_turn=20)
    great_face(k, "body", GREAT, FR, GREAT_T, 0.123, square_at=112, dot_at=-4, chevron_at=226)
    k.build(voxel=0.0035, smooth=3, wband=0.01)
    k.eye_states(lambda state: kl.eye(k, state, SMALL, SMALL_R, FR, SMALL_T, e))
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    klang(src, out)
