# Darmanitan (standard mode) sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py), grown from darumaka.py (imported as sibling("darumaka")): its red, tan and yellow, its round
# black-rimmed eye, its grin and its way of placing marks on the ball.
#   blender --background --factory-startup --python sculpts/darmanitan.py -- <the export's Darmanitan.glb> <out.glb>
#
# The design, from the references: a great red ape whose head and body are one ball, held up between long thick arms
# that come down to the ground on big tan fists, a zigzag of fur where the red meets the tan; a tan mask round the
# eyes, its lower edge pointed under a small nose; round white eyes with a bold dark rim and a black pupil; a huge
# white grin, a zigzag line of teeth across it; over each eye a small yellow curl (Darumaka's brow) from which a long
# flame rises up and out, yellow at its root, orange, then red; a band of tan ovals round the lower ball; short hind
# legs with tan three-toed feet under it; a small tuft of a tail behind. The skeleton is a quadruped's: the arms are on
# the leg bones, the hind legs on the hind bones.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, unit, tri, polygon, outward, eye_look, args, sibling

d = sibling("darumaka")
RED, TAN, YELLOW, INK, WHITE = d.RED, d.TAN, d.YELLOW, d.INK, d.WHITE
FLAME, FLAME_MID = "#E4522E", "#F59A35"

C = np.array([0.0, -0.02, 0.34])            # the ball's middle (the body's bone is at 0.32, the head's at 0.38)
R = np.array([0.2, 0.19, 0.2])


def arms(k):
    """Long thick arms down to the ground on big tan fists, a zigzag of fur where the red meets the tan."""
    SH, EL, WR = np.array([0.135, -0.05, 0.41]), np.array([0.235, -0.12, 0.255]), np.array([0.25, -0.17, 0.125])
    k.add("arm", tube([SH, EL, WR], [0.064, 0.056, 0.062]), RED, "leg{s}", mirror=True)
    FIST = np.array([0.255, -0.185, 0.068])
    k.add("fist", ell(FIST, (0.078, 0.08, 0.068)), TAN, "leg{s}", mirror=True)

    def cuff(P):
        ax = np.abs(P[:, 0]) - WR[0]
        ay = P[:, 1] - WR[1]
        near = inside(np.hypot(ax, ay) - 0.1, 0.004) * inside(P[:, 2] - 0.2, 0.004)
        a = np.arctan2(ay, ax)
        edge = 0.122 + 0.024 * tri(a * 8 / (2 * math.pi))
        return near * inside(P[:, 2] - edge, 0.0025)
    k.paint(cuff, TAN)
    k.paint(lambda P: inside(np.hypot(np.abs(P[:, 0]) - WR[0], P[:, 1] - WR[1]) - 0.1, 0.004)
            * inside(0.2 - P[:, 2], 0.004) * inside(P[:, 2] - 0.4, 0.004), RED)


def hind_legs(k):
    k.add("haunch", tube([(0.095, 0.075, 0.22), (0.11, 0.07, 0.13), (0.12, 0.055, 0.065)], [0.075, 0.066, 0.056]), RED, "hind{s}", mirror=True)
    FC = np.array([0.122, 0.05, 0.03])
    k.add("foot", ell(FC, (0.052, 0.062, 0.032)), TAN, "hind{s}", mirror=True)
    for i, a in enumerate((-30, 0, 30)):
        dd = unit(math.sin(math.radians(a)), -math.cos(math.radians(a)), 0)
        k.add(f"toe{i}", tube([FC + dd * 0.04, FC + dd * 0.066], [0.019, 0.017]), TAN, "hind{s}", mirror=True)
    k.paint(lambda P: inside(P[:, 2] - 0.058, 0.0025) * inside(np.abs(np.abs(P[:, 0]) - 0.12) - 0.08, 0.004)
            * inside(-P[:, 1] - 0.03, 0.004) * inside(P[:, 1] - 0.14, 0.004), TAN)


def flame_outline(L, w):
    """A flame's outline along its length a: wide at the root, ragged sides tapering to a point."""
    return [(0.0, -w), (0.3 * L, -0.92 * w), (0.36 * L, -1.12 * w), (0.55 * L, -0.66 * w), (0.62 * L, -0.8 * w),
            (0.82 * L, -0.32 * w), (L, 0.0), (0.7 * L, 0.42 * w), (0.6 * L, 0.78 * w), (0.46 * L, 0.66 * w),
            (0.34 * L, 1.06 * w), (0.18 * L, 0.98 * w), (0.0, w)]


def flames(k):
    """Over each eye Darumaka's curl, small, and from it a long flame up and out, leaning back."""
    for side, nm in ((1, "R"), (-1, "L")):
        root = np.array([side * 0.058, -0.112, 0.5])
        dirn = unit(side * 0.52, 0.3, 1.0)
        across = unit(*np.cross((0, -1.0, 0), dirn))
        if side < 0:
            across = -across                # the twin keeps its frame right-handed
        for name, col, L, w, t, back in (("flame", FLAME, 0.25, 0.04, 0.034, 0.0),
                                         ("flamemid", FLAME_MID, 0.15, 0.03, 0.044, 0.004),
                                         ("flameheart", YELLOW, 0.075, 0.024, 0.054, 0.006)):
            k.add(f"{name}{nm}", slab(flame_outline(L, w), root + dirn * back, dirn, across, t), col, "head",
                  fuse=False, surface="fur")
        d.brow(k, f"curl{nm}", (side * 0.072, -0.122, 0.497), 0.024, 0.017, side)


def face(k):
    MASK = [(-0.138, 0.44), (-0.122, 0.478), (-0.075, 0.49), (-0.03, 0.47), (0.0, 0.475), (0.03, 0.47),
            (0.075, 0.49), (0.122, 0.478), (0.138, 0.44), (0.122, 0.398), (0.075, 0.378), (0.032, 0.384),
            (0.0, 0.358), (-0.032, 0.384), (-0.075, 0.378), (-0.122, 0.398)]
    k.paint(lambda P: d.poly_mask(*d.round_about(P, C, R[0]), MASK) * (P[:, 1] < C[1] - 0.05), TAN)
    p, n = d.on_ball(C, R, 0.0, 0.392)
    k.add("nose", ell(p + n * 0.002, (0.022, 0.018, 0.016)), TAN, "head")

    # the great grin: white, a bold dark rim, a zigzag line of teeth across it
    p, n = d.on_ball(C, R, 0.0, 0.305)
    out = polygon(d.GRIN)
    half = (0.105, 0.05)
    k.decal(p, n, (half[0] * 1.08, half[1] * 1.2), INK, eps=0.004, outline=out, rings=16, segs=96)
    def look(u, v):
        zig = 0.12 + 0.2 * tri(u * 2.6 + 0.25)
        ink = inside(np.abs(v - zig) - 0.075, 0.02)
        return srgb(WHITE)[None] * (1 - ink[:, None]) + srgb(INK)[None] * ink[:, None]
    k.decal(p, n, half, look, eps=0.006, outline=out, rings=24, segs=120)

    # a band of tan ovals round the lower ball
    d.ovals(k, C, R, [i * 0.3 for i in range(-2, 3)], 0.218, (0.024, 0.019), TAN)


def tail(k):
    k.add("tail", tube([(0, 0.15, 0.3), (0.01, 0.195, 0.315), (0.02, 0.215, 0.34)], [0.04, 0.028, 0.017]), RED, "body")


def darmanitan(src, out):
    k = Kit(src, coat="fur")
    k.add("ball", ell(C, R), RED, "body")
    k.add("crown", ell(C + (0, -0.01, 0.06), R * (0.95, 0.94, 0.72)), RED, "head")
    arms(k)
    hind_legs(k)
    tail(k)
    flames(k)
    face(k)
    k.build(voxel=0.006, smooth=3, band=0.006)
    look = lambda u, v: d.eye_open(u, v, pupil=(-0.06, 0.0, 0.36), rim=0.74)
    k.eye_states(lambda s: d.round_eyes(k, C, R, 0.4, 0.432, (0.04, 0.041), s, look=look))
    k.export(out)


if __name__ == "__main__":
    darmanitan(*args())
