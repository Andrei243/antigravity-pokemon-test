# Vikavolt sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/vikavolt.py -- <the export's Vikavolt.glb> <out.glb>
#
# From the references: a stag beetle flying like a railgun, hovering. Its largest mass is its two great jaws: long
# flat bars of dark teal reaching forward and a little down from under the face, their inner halves yellow, three
# white teeth along each inner edge, and at each root a teal flange sweeping out sideways, edged yellow. The head is
# a faceted teal helmet with a point over the brow, and under it an orange face with two angry yellow eyes, a dark
# pupil low toward the middle of each. Behind the head two tall angular teal wing cases stand up and back in a V; a
# grey abdomen with dark seams and a yellow stripe lies behind; two broad pale wings spread wide to the sides, raised
# in a shallow V; three pairs of short black legs with white claws hang under the thorax.
#
# Grown from grubbin.py (its on_ell, its jaws of a teal sleeve, its way of making eyes as patches on a fused face).
# Skeleton: body (0, 0, 0.104), head (0, -0.06, 0.104), wing{s} (+-0.03, 0.04, 0.144).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, unit, eye_look, polygon, outward, sibling, args

g = sibling("grubbin")

TEAL, TEAL_L, YELLOW, ORANGE = "#1C6680", "#2B8AA6", "#F4E040", "#E8762E"
GREY, GREY_D, WING, INK, WHITE = "#B6BCC6", "#7A808C", "#D4DCE6", "#141418", "#F4F6F8"

FC, FR = np.array([0.0, -0.075, 0.093]), np.array([0.033, 0.028, 0.029])    # the orange face
VOXEL = 0.0018

JAW_ROOT = np.array([0.024, -0.098, 0.074])
JAW_D = unit(0, -0.95, -0.28)
JAW_L = 0.215


def frame(side):
    """The jaw's frame on one side: along it, outward (x on the +X side), and the face's normal."""
    d = JAW_D
    out = np.array([side, 0.0, 0.0])
    n = np.cross(d, out) * side          # up and a little forward on both sides
    return d, out, n / np.linalg.norm(n)


def jaws(k):
    d, o, n = frame(1)
    # the teal bar with its flange, then the yellow inner strip thicker than it (shows on both faces)
    bar = [(-0.01, -0.012), (0.03, -0.014), (JAW_L - 0.01, -0.01), (JAW_L, 0.0), (JAW_L - 0.012, 0.022),
           (0.078, 0.03), (0.03, 0.086), (-0.004, 0.034), (-0.012, 0.02)]
    k.add("jaw", slab(bar, JAW_ROOT, d, o, 0.014), TEAL, "head", fuse=False, mirror=True, surface="shell")
    strip = [(0.03, -0.016), (JAW_L - 0.012, -0.013), (JAW_L + 0.002, -0.002), (JAW_L - 0.02, 0.006),
             (0.03, 0.004)]
    k.add("jawstrip", slab(strip, JAW_ROOT, d, o, 0.02), YELLOW, "head", fuse=False, mirror=True, surface="shell")
    edge = [(0.07, 0.027), (0.082, 0.031), (0.034, 0.092), (0.026, 0.087)]
    k.add("jawedge", slab(edge, JAW_ROOT, d, o, 0.019), YELLOW, "head", fuse=False, mirror=True, surface="shell")
    # three white teeth along the inner edge, pointing in and forward
    for i, a in enumerate((0.09, 0.13, 0.17)):
        p = lambda aa, bb, hh: JAW_ROOT + aa * d + bb * o + hh * n
        k.add(f"tooth{i}", hull([p(a - 0.012, -0.008, 0.007), p(a + 0.006, -0.008, 0.007),
                                 p(a - 0.012, -0.008, -0.007), p(a + 0.006, -0.008, -0.007),
                                 p(a + 0.006, -0.028, 0.0)]), WHITE, "head", fuse=False, mirror=True,
              surface="shell")


def helmet(k):
    """The faceted teal helmet over the face, its point over the brow."""
    pts = [(0, -0.103, 0.104), (0.036, -0.094, 0.106), (0.043, -0.06, 0.098), (0.036, -0.026, 0.112),
           (0, -0.02, 0.122), (0, -0.07, 0.148), (0.012, -0.09, 0.132), (0.034, -0.05, 0.13),
           (0.03, -0.088, 0.096), (0, -0.098, 0.098)]
    P = [p for p in pts] + [(-x, y, z) for x, y, z in pts if x]
    k.add("helmet", hull(P), TEAL, "head", fuse=False, surface="shell")


def elytra(k):
    """The two tall wing cases standing up and back in a V: an angular blade each."""
    for s, side in ((1, "R"), (-1, "L")):
        base = np.array([0.022 * s, -0.006, 0.124])
        d = unit(0.45 * s, 0.52, 0.72)
        w = np.cross(d, (0, 0, 1.0)); w /= np.linalg.norm(w)
        t = np.cross(w, d); t /= np.linalg.norm(t)
        L = 0.12
        P = [base + w * 0.018 + t * 0.008, base - w * 0.018 + t * 0.008, base + w * 0.018 - t * 0.01,
             base - w * 0.018 - t * 0.01,
             base + d * L * 0.45 + w * 0.034 + t * 0.013, base + d * L * 0.45 - w * 0.03 + t * 0.006,
             base + d * L * 0.45 + w * 0.02 - t * 0.008, base + d * L * 0.45 - w * 0.018 - t * 0.008,
             base + d * L]
        k.add(f"elytron{side}", hull(P), TEAL, f"wing{side}", fuse=False, surface="shell")


def wings(k):
    """The broad pale wings, spread wide and raised in a shallow V."""
    out = unit(0.88, 0.22, 0.42)
    back = unit(0.0, 0.55, 0.7); back = back - out * (back @ out); back /= np.linalg.norm(back)
    shape = [(0.0, -0.012), (0.05, -0.034), (0.11, -0.03), (0.152, -0.008), (0.16, 0.018), (0.125, 0.046),
             (0.065, 0.054), (0.0, 0.02)]
    k.add("wing", slab(shape, (0.026, 0.036, 0.132), out, back, 0.005), WING, "wing{s}", fuse=False, mirror=True,
          surface="shell")
    vein = [(0.0, -0.016), (0.05, -0.038), (0.11, -0.034), (0.152, -0.012), (0.152, -0.006), (0.11, -0.027),
            (0.05, -0.03), (0.0, -0.008)]
    k.add("wingvein", slab(vein, (0.026, 0.036, 0.132), out, back, 0.007), GREY_D, "wing{s}", fuse=False,
          mirror=True, surface="shell")


def legs(k):
    for i, y in enumerate((-0.04, -0.012, 0.016)):
        hip = np.array([0.024, y, 0.094])
        knee = np.array([0.05, y - 0.004, 0.088])
        foot = np.array([0.056, y - 0.002, 0.064])
        k.add(f"leg{i}", outward(tube([hip, knee, foot], [0.0062, 0.006, 0.0055], seg=12, per=6)), INK, "body",
              fuse=False, mirror=True, surface="shell")
        k.add(f"claw{i}", hull([foot + (0.006, 0.006, 0.004), foot + (-0.006, 0.006, 0.004),
                                foot + (0.006, -0.006, 0.004), foot + (-0.006, -0.006, 0.004),
                                foot + (0.003, -0.004, -0.022)]), WHITE, "body", fuse=False, mirror=True,
              surface="shell")


def vikavolt(src, out_path):
    k = Kit(src, coat="shell")
    # --- the fused body: thorax, abdomen, the face -----------------------------------------------------------
    k.add("thorax", ell((0, -0.02, 0.104), (0.036, 0.034, 0.032)), TEAL, "body")
    k.add("abdomen", ell((0, 0.06, 0.11), (0.042, 0.07, 0.036), rot=(-8, 0, 0)), GREY, "body")
    k.add("face", ell(FC, FR), ORANGE, "head")
    # the abdomen's seams and its yellow stripe
    def seams(P):
        m = np.zeros(len(P))
        for y in (0.03, 0.075, 0.105):
            m = np.maximum(m, inside(np.abs(P[:, 1] - y) - 0.0018, 0.001))
        return m
    k.paint(seams, GREY_D, on=["abdomen"])
    k.paint(lambda P: inside(np.abs(P[:, 1] - 0.052) - 0.006, 0.001) * inside(0.118 - P[:, 2], 0.002), YELLOW,
            on=["abdomen"])

    helmet(k)
    jaws(k)
    elytra(k)
    wings(k)
    legs(k)
    k.build(voxel=VOXEL, smooth=3, band=0.003, wband=0.02)

    # --- the eyes: angry yellow wedges under the helmet's brow, a dark pupil low toward the middle ---------------
    # u outward on the +X eye; the -X eye is laid by hand with its outline and colours turned round
    pts = [(-1, -0.15), (-0.55, -0.85), (0.6, -0.85), (1, -0.25), (0.95, 0.9), (-1, 0.05)]
    def eye_open(u, v):
        c = np.tile(srgb(YELLOW), (len(u), 1))
        c[((u + 0.32) / 0.22) ** 2 + ((v + 0.22) / 0.4) ** 2 < 1] = srgb(INK)
        return c
    def lay(state):
        look = eye_look(state, eye_open, srgb(ORANGE), srgb(INK), line=0.2)
        p, n = g.on_ell(FC, FR, (0.52, -0.82, 0.12))
        m = np.array([-1, 1, 1.0])
        k.decal(p, n, (0.0145, 0.0098), look, eps=0.0012, rings=20, segs=56, outline=polygon(pts))
        k.decal(p * m, n * m, (0.0145, 0.0098), lambda u, v: look(-u, v), eps=0.0012, rings=20, segs=56,
                outline=polygon([(-a, b) for a, b in pts]))
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    vikavolt(*args())
