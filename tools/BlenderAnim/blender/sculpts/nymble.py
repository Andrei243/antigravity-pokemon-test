# Nymble sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/nymble.py -- <the export's Nymble.glb> <out.glb>
#
# The design, from the references: a small grasshopper. Its first mass is a great round slate-blue head in front, as
# wide as the model is half tall, a white lip across its lower front and two white teardrop marks on its upper sides;
# on each side of the head an eye that is a yellow four-lobed flower with a small black four-lobed pupil. Behind the
# head a pale grey-white abdomen runs back, ringed with segment lines. Over the body rise its folded hind legs: two
# great slate-blue lobes, flat side to side, leaning back and splayed a little apart, a white ring on the outer face of
# each near its top. It stands on four thick slate pegs tipped white, the front pair under the head, the back pair at
# the abdomen's end, each back peg reached by a thin white strut from the body (the folded leg's shin).
# The task's text spoke of a dark face: the pictures show the eyes as yellow flowers on the head's sides and a white
# lip, nothing dark but the pupils.
#
# Written to be grown from (lokix.py imports it): the colours, on_ell, the hind leg's lobe with its ring and the white
# tipped peg are functions and constants; the build is under __main__.
# Skeleton: body (0, 0, 0.17), ear{s} (+-0.035, 0.03, 0.22) (the folded hind legs), leg{s} (+-0.05, -0.04, 0.13),
# hind{s} (+-0.05, 0.05, 0.13). No head bone: the head is the body's.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args
from mathutils import Euler

SLATE, SLATE_D, PALE, PALE_D = "#5F7398", "#4A5A7C", "#DCDFE6", "#A9AFBF"
YELLOW, INK, WHITE = "#F2C02E", "#141418", "#F4F4F6"

HC, HR = np.array([0.0, -0.06, 0.192]), np.array([0.088, 0.084, 0.086])      # the head
VOXEL = 0.0034


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) in direction d from its centre, and its outward normal."""
    c, r, d = np.array(c, float), np.array(r, float), unit(*d)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def turn(rot):
    return np.array(Euler([math.radians(a) for a in rot]).to_matrix())


def lobe(k, name, c, r, rot, bone, colour=SLATE, ring=WHITE, ring_at=(0.0, 0.55), ring_half=0.022, mirror=True):
    """A folded hind leg: a flat lobe (radii r, thin across x) turned by rot about its centre c, fused, with a white
    ring on its outer face (two stacked patches, the inner one the lobe's colour). ring_at is the ring's place on the
    face as shares of the lobe's y and z radii. Returns nothing; the patches go in before build()."""
    c = np.array(c, float); r = np.array(r, float)
    k.add(name, ell(c, r, rot=rot, sub=5), colour, bone, mirror=mirror)
    m = turn(rot)
    local = np.array([r[0], ring_at[0] * r[1], ring_at[1] * r[2]])
    # the point on the lobe's outer face at that place (an ellipsoid's surface through it)
    q = local / np.sqrt(((local / r) ** 2).sum())
    nl = q / r ** 2; nl /= np.linalg.norm(nl)
    p, n = c + m @ q, m @ nl
    up = m @ np.array([0, 0, 1.0])
    sides = (1, -1) if mirror else (1,)
    for s in sides:
        mm = np.array([s, 1, 1.0])
        k.decal(p * mm, n * mm, (ring_half, ring_half * 1.25), ring, up=up * mm, eps=0.0014, rings=14, segs=56)
        k.decal(p * mm, n * mm, (ring_half * 0.55, ring_half * 0.75), colour, up=up * mm, eps=0.0026, rings=10,
                segs=48)


def peg(k, name, top, foot, radii, bone, colour=SLATE, tip=WHITE, tip_h=0.018, mirror=True):
    """A thick tapering peg of a leg, its last tip_h white."""
    k.add(name, tube([top, (np.array(top) + np.array(foot)) / 2, foot], radii), colour, bone, mirror=mirror)
    k.paint(lambda P, h=tip_h: inside(P[:, 2] - h, 0.0015), tip, on=[name])


def flower(th, lobes=4, depth=0.42, turn_=math.pi / 4):
    """A four-lobed outline, its lobes on the diagonals (an X)."""
    return 1 - depth * (1 - abs(math.cos(lobes / 2 * (th - turn_)))) ** 1.5


# --- Nymble ----------------------------------------------------------------------------------------------------------
EYE_D, EYE_HALF = (0.86, -0.5, 0.06), (0.042, 0.042)


def lay_eyes(k, state):
    """The yellow flowers on the head's sides; in the Open and Fierce states a black four-lobed pupil over each."""
    plain = lambda u, v: np.tile(srgb(YELLOW), (len(u), 1))
    look = eye_look(state, plain, srgb(SLATE), srgb(INK), line=0.17)
    for s in (1, -1):
        p, n = on_ell(HC, HR, (EYE_D[0] * s, EYE_D[1], EYE_D[2]))
        col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
        k.decal(p, n, EYE_HALF, col, eps=0.0014, outline=flower, rings=20, segs=72)
        if state in ("Open", "Fierce"):
            q = p + np.array([0, 0, -0.004])
            k.decal(q, n, (0.016, 0.016), INK, eps=0.0026, outline=lambda th: flower(th, depth=0.55), rings=10,
                    segs=64)


def nymble(src, out):
    k = Kit(src, coat="shell")
    # ---- the head, its white lip and the white marks on its upper sides
    k.add("head", ell(HC, HR, sub=5), SLATE, "body")
    def lip(P):
        q = (P - (HC + np.array([0, -0.03, -0.05]))) / np.array([0.07, 0.07, 0.04])
        return inside((q ** 2).sum(1) - 1, 0.012) * (P[:, 1] < HC[1] - 0.035)
    k.paint(lip, WHITE, on=["head"])
    p, n = on_ell(HC, HR, (0.55, -0.55, 0.68))
    k.decal(p, n, (0.016, 0.026), WHITE, up=(-0.3, 0.2, 1), eps=0.0014, rings=10, segs=48, mirror=True)
    # ---- the abdomen behind, ringed with segment lines
    k.add("abdomen", tube([(0, -0.01, 0.175), (0, 0.08, 0.165), (0, 0.17, 0.158)], [0.06, 0.054, 0.038]), PALE,
          "body", surface="skin")
    k.add("waist", ell((0, 0.01, 0.17), (0.058, 0.05, 0.058)), PALE, "body", surface="skin")
    def segments(P):
        m = np.zeros(len(P))
        for y in (0.045, 0.085, 0.125):
            m = np.maximum(m, inside(np.abs(P[:, 1] - y) - 0.0016, 0.0008))
        return m
    k.paint(segments, PALE_D, on=["abdomen"])
    # ---- the folded hind legs rising over the body, leaning back and splayed apart
    lobe(k, "lobe", (0.05, 0.075, 0.255), (0.026, 0.064, 0.082), (-38, 16, 0), "ear{s}")
    # ---- four thick pegs, the back pair reached by thin white struts
    peg(k, "front", (0.048, -0.06, 0.13), (0.062, -0.085, 0.01), [0.024, 0.019, 0.013], "leg{s}")
    peg(k, "back", (0.075, 0.165, 0.11), (0.08, 0.18, 0.01), [0.019, 0.017, 0.012], "hind{s}")
    k.add("strut", tube([(0.03, 0.06, 0.15), (0.058, 0.115, 0.135), (0.075, 0.165, 0.115)], [0.0085, 0.008, 0.009]),
          WHITE, "hind{s}", mirror=True)
    k.build(voxel=VOXEL, smooth=3, band=0.004, wband=0.02)
    k.eye_states(lambda state: lay_eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    nymble(*args())
