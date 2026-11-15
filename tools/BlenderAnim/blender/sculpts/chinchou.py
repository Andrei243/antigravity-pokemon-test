# Chinchou sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/chinchou.py -- <the export's Chinchou.glb> <out.glb>
#
# The design, from the references: a round blue fish a little wider than it is tall; two great yellow discs for eyes
# filling most of its front, each ringed in dark and crossed by a bold black plus; a small red mouth low between
# them; a pale, glassy fin on each side swept out and back; two little blue fins under it and a nub of a tail behind;
# two blue feelers rising from the top of the head and arching out and down, turning yellow near their ends, each
# hung with a large glowing yellow drop, narrow at the top and round below.
#
# Written to be grown from (Lanturn keeps the line's blue, its glowing lights and its feelers): the feeler, the drop
# and the colours are the functions and constants below.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, lathe, outward, srgb, unit, eye_look, polygon, args

BLUE, FIN, YELLOW, LIGHT, INK, MOUTH = "#5288D8", "#D4E2F2", "#F2D64E", "#FFE45A", "#20222E", "#D8505E"


# --- the line's lights: a feeler and its glowing drop ----------------------------------------------------------------
def feeler(k, name, pts, radius, bone, colour=BLUE, tip=YELLOW, tip_from=0.7, mirror=False):
    """A loose round feeler through pts, its last stretch (from tip_from of the way) yellow, a little thicker."""
    # The curve made dense once (the tube's own spline), so the blue and the yellow follow the same line
    p = np.array(pts, float)
    ext = np.vstack([2 * p[0] - p[1], p, 2 * p[-1] - p[-2]])
    line = []
    for i in range(len(p) - 1):
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        for t in np.linspace(0, 1, 8, endpoint=False):
            line.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                               + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    line.append(p[-1])
    line = np.array(line)
    k.add(name, outward(tube(line, [radius] * len(line), seg=14, per=1)), colour, bone, fuse=False, mirror=mirror,
          surface="scales")
    if tip is None:
        return
    # The yellow end: the same line from tip_from of its length on, a hair thicker so it covers the blue
    s = np.cumsum(np.r_[0, np.linalg.norm(np.diff(line, axis=0), axis=1)])
    i = int(np.searchsorted(s / s[-1], tip_from))
    rest = line[min(i, len(line) - 2):]
    k.add(name + "tip", outward(tube(rest, [radius * 1.18] * len(rest), seg=14, per=1)), tip, bone, fuse=False,
          mirror=mirror, surface="scales")


def drop(k, name, top, length, width, bone, colour=LIGHT, mirror=False, d=(0, 0, -1)):
    """A glowing drop hung from top along d: narrow where it hangs, round below."""
    prof = [(0.0, 0.0), (0.22, 0.06), (0.5, 0.2), (0.78, 0.4), (0.96, 0.6), (1.0, 0.72), (0.92, 0.85), (0.68, 0.95),
            (0.36, 0.995), (0.0, 1.0)]
    k.add(name, lathe(top, d, [(r * width, s * length) for r, s in prof], seg=36), colour, bone, fuse=False,
          mirror=mirror, glow=colour, surface="skin")


def orb(k, name, c, r, bone, colour=LIGHT):
    """A round glowing light."""
    k.add(name, outward(ell(c, (r, r, r), sub=4)), colour, bone, fuse=False, glow=colour, surface="skin")


# --- Chinchou ----------------------------------------------------------------------------------------------------------
C = np.array([0.0, 0.0, 0.175])         # the body's middle, near the body bone
RB = (0.165, 0.15, 0.13)                # its radii: across, front to back, up
EYE_D = unit(0.46, -1.0, 0.06)          # where each eye's middle looks (the +X one)
EYE_HALF = 0.06
RIM = 0.84                              # the dark ring's inner edge, a share of the eye's radius
PLUS = polygon([(0.25, 0.25), (0.78, 0.25), (0.78, -0.25), (0.25, -0.25), (0.25, -0.78), (-0.25, -0.78),
                (-0.25, -0.25), (-0.78, -0.25), (-0.78, 0.25), (-0.25, 0.25), (-0.25, 0.78), (0.25, 0.78)])
FEELER = [(0.03, 0.0, 0.28), (0.055, 0.0, 0.35), (0.12, 0.01, 0.395), (0.19, 0.02, 0.385), (0.225, 0.02, 0.33),
          (0.232, 0.02, 0.3)]


def body(k):
    k.add("body", ell(C, RB), BLUE, "body")
    # Two little fins under it, a nub of a tail behind
    k.add("foot", ell((0.048, -0.035, 0.062), (0.03, 0.034, 0.034), rot=(-15, 0, 0)), BLUE, "body", mirror=True)
    k.add("tailnub", ell((0.03, 0.135, 0.085), (0.026, 0.04, 0.03), rot=(30, 0, 25)), BLUE, "body")
    # The pale fins, swept out and back, their faces to the front
    wing = [(0.0, -0.016), (0.05, -0.032), (0.11, -0.028), (0.142, -0.006), (0.136, 0.016), (0.1, 0.03),
            (0.05, 0.032), (0.0, 0.02)]
    k.add("fin", slab(wing, (0.13, -0.015, 0.125), unit(1, 0.25, -0.18), (0, 0, 1), 0.013), FIN, "body", fuse=False,
          mirror=True, surface="scales")
    # The feelers and their drops
    feeler(k, "feeler", FEELER, 0.0115, "lure{s}", mirror=True)
    drop(k, "drop", FEELER[-1], 0.15, 0.05, "lure{s}", mirror=True)
    # The small red mouth, low between the eyes
    def mouth(u, v):
        c = np.tile(srgb(MOUTH), (len(u), 1))
        c[u * u + v * v > 0.72] = srgb("#8A2A36")
        return c
    k.decal(C + unit(0, -1, -0.5) * 0.14, unit(0, -1, -0.55), (0.011, 0.013), mouth, eps=0.004, rings=8, segs=36)


CY, CK, CB = srgb(YELLOW), srgb(INK), srgb(BLUE)


def eye_open(u, v):
    c = np.tile(CY, (len(u), 1))
    c[u * u + v * v > RIM * RIM] = CK
    return c


def eye_state(state):
    """The yellow disc is the design's mark, so it is its own lid: shut, it stays yellow with its ring."""
    base = eye_look(state, eye_open, CY, CK, line=0.12)
    def f(u, v):
        c = base(u, v)
        c[u * u + v * v > RIM * RIM] = CK
        return c
    return f


def plus_state(state):
    def f(u, v):
        c = np.tile(CK, (len(u), 1))
        if state == "Fierce":
            c[0.8 * v > 0.2 + 0.4 * u + 0.12] = CY      # the lid of the eye's own frame, the plus being 0.8 of it
        return c
    return f


def eyes(k, state, c=C, d=EYE_D, half=EYE_HALF):
    for s in (1, -1):
        n = d * np.array([s, 1, 1])
        p = c + n * 0.15
        look = eye_state(state)
        k.decal(p, n, (half, half), look if s > 0 else (lambda u, v, f=look: f(-u, v)), eps=0.004, rings=20, segs=56)
        if state in ("Open", "Fierce"):
            pl = plus_state(state)
            k.decal(p, n, (half * 0.8, half * 0.8), pl if s > 0 else (lambda u, v, f=pl: f(-u, v)), eps=0.006,
                    outline=PLUS, rings=12, segs=120)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")
    body(k)
    k.build(voxel=0.005, smooth=4)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
