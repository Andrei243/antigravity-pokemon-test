# Zapdos sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/zapdos.py -- <the export's Zapdos.glb> <out.glb>
# A yellow bird all of spikes, built on articuno.py's frame (its wing axes, legs and eyes cut along a brow): a burst of
# bold spikes round the head, a long thin orange beak pointing down, fierce eyes in black, great jagged wings like
# lightning raised in a V and swept back with black spikes under them, a ruff of spikes on the chest, grey-brown fluffy
# thighs over orange legs with white claws, and a tail of spikes pointing down, black ones between.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, unit, lumpy, sibling, args

a = sibling("articuno")
sp = a.sp

YELLOW, YELLOW2, BLACK = "#F6D03A", "#EFC22C", "#26222A"
ORANGE, BEAK, THIGH, CLAW = "#EE8A2C", "#EC922E", "#9A8C7C", "#F6F4F0"
WHITE, PUPIL = "#FBFBF8", "#16120E"

H = np.array([0, -0.085, 0.63])          # the head's centre, before the head's joint (0, -0.05, 0.586)
HR = np.array([0.074, 0.08, 0.074])


def star(angles, lengths, widths, root=0.11, start=-70, end=100, inner=0.07):
    """A starburst of points about the wing's root in (along the span, across the chord): a jagged wing."""
    pts = [(inner * math.cos(math.radians(start)), inner * math.sin(math.radians(start)))]
    for ang, L, w in zip(angles, lengths, widths):
        for t, r in ((ang - w, root), (ang, L), (ang + w, root)):
            pts.append((r * math.cos(math.radians(t)), r * math.sin(math.radians(t))))
    pts.append((inner * math.cos(math.radians(end)), inner * math.sin(math.radians(end))))
    pts.append((-0.03, 0.0))
    return pts


ELEV, SWEEP = 36, 24
WANG = [-30, -3, 25, 53, 80]
WLEN = [0.3, 0.41, 0.35, 0.28, 0.2]
WWID = [13, 14, 13, 12, 11]


def zapdos(src, out):
    k = Kit(src, coat="fur")

    # The trunk, mostly hidden behind its spikes; the head over it, forward
    k.add("body", ell((0, 0.01, 0.48), (0.11, 0.105, 0.13), rot=(-10, 0, 0)), YELLOW, "body")
    k.add("chest", ell((0, -0.05, 0.52), (0.09, 0.08, 0.1)), YELLOW, "body")
    k.add("neck", tube([(0, -0.04, 0.55), H + (0, 0.02, -0.03)], [0.07, 0.062]), YELLOW, "head")
    k.add("head", ell(H, HR), YELLOW, "head")

    # The burst of bold spikes round the head, rooted behind the eyes, laid in the face's plane and leaning back
    for i, (d, L, w) in enumerate([((0, 0.25, 1), 0.17, 0.062), ((0.6, 0.3, 0.85), 0.15, 0.058),
                                   ((-0.6, 0.3, 0.85), 0.15, 0.058), ((1, 0.35, 0.3), 0.14, 0.056),
                                   ((-1, 0.35, 0.3), 0.14, 0.056), ((0.85, 0.3, -0.4), 0.11, 0.05),
                                   ((-0.85, 0.3, -0.4), 0.11, 0.05)]):
        dd = unit(*d)
        base = H + dd * HR * 0.55 + (0, 0.025, 0)
        k.add(f"crest{i}", sp.spike(base, base + dd * (L + 0.04), w, 0.02, up=(0, -1, 0)), YELLOW, "head", fuse=False)

    # A ruff of spikes on the chest, pointing down and out over the thighs
    for i, (x, d, L) in enumerate([(0.04, (0.55, -0.35, -0.75), 0.13), (-0.04, (-0.55, -0.35, -0.75), 0.13),
                                   (0.0, (0.0, -0.45, -0.9), 0.11), (0.07, (0.95, -0.1, -0.2), 0.1),
                                   (-0.07, (-0.95, -0.1, -0.2), 0.1)]):
        dd = unit(*d)
        base = np.array([x, -0.08, 0.52])
        k.add(f"ruff{i}", sp.spike(base, base + dd * L, 0.055, 0.018, up=(0, -1, 0)), YELLOW2, "body", fuse=False)

    # The long thin orange beak, pointing down and forward
    B = H + (0, -0.068, -0.012)
    D = unit(0, -0.62, -0.78)
    k.add("beak", tube([B, B + D * 0.06, B + D * 0.13, B + D * 0.18], [0.021, 0.014, 0.008, 0.002], seg=20), BEAK,
          "head", fuse=False, surface="shell")

    # The jagged wings: a yellow starburst plate, and black spikes behind it peeking out under each yellow one
    ex, ez = a.wing_axes(ELEV, SWEEP, (0.25, 0.45, -0.85))
    n = np.cross(ex, ez)
    if n[1] < 0:
        n = -n                                  # n toward the back: the black lies behind the yellow
    root = np.array([0.08, 0.03, 0.55])
    k.add("wing", slab(star(WANG, WLEN, WWID, root=0.13), root, ex, ez, 0.032), YELLOW, "wing{s}", fuse=False, mirror=True)
    k.add("wingblack", slab(star([w + 15 for w in WANG[:3]], [l * 0.8 for l in WLEN[:3]], [8] * 3, root=0.1,
                                 start=-20, end=75), root + n * 0.022, ex, ez, 0.02), BLACK, "wing{s}", fuse=False,
          mirror=True)
    k.add("shoulder", ell(root + ex * 0.02, (0.045, 0.05, 0.045)), YELLOW, "wing{s}", mirror=True)

    # The tail: spikes pointing down and back, black ones behind and between
    TB = np.array([0, 0.11, 0.42])
    for i, (d, L) in enumerate([((0, 0.45, -1), 0.2), ((0.42, 0.4, -0.9), 0.17), ((-0.42, 0.4, -0.9), 0.17),
                                ((0.8, 0.3, -0.55), 0.13), ((-0.8, 0.3, -0.55), 0.13)]):
        dd = unit(*d)
        k.add(f"tail{i}", sp.spike(TB, TB + dd * L, 0.06, 0.02, up=(0, -1, 0)), YELLOW, "tail", fuse=False)
    for i, (d, L) in enumerate([((0.22, 0.6, -1), 0.15), ((-0.22, 0.6, -1), 0.15)]):
        dd = unit(*d)
        b = TB + (0, 0.025, 0)
        k.add(f"tailblack{i}", sp.spike(b, b + dd * L, 0.035, 0.016, up=(0, -1, 0)), BLACK, "tail", fuse=False)
    k.add("rump", ell(TB + (0, -0.02, 0.01), (0.06, 0.05, 0.05)), YELLOW, "tail")

    # Grey-brown fluffy thighs over orange legs, small feet with white claws
    k.add("thigh", ell((0.068, -0.04, 0.4), (0.052, 0.055, 0.055), deform=lumpy(5, 3, 0.12)), THIGH, "leg{s}",
          mirror=True)
    for side in ((1, "R"), (-1, "L")):
        sp.foot(k, side, (0.075, -0.05, 0.38), (0.09, -0.1, 0.3), 0.048, ORANGE, CLAW, r=0.013)

    # The eyes: a bold black patch round each, white eyes cut by a fierce brow over it
    de = unit(0.5, -0.83, 0.18)
    E = a.eye_at(H, HR, de)
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        k.decal(E * m + (0, -0.003, 0), de * m, (0.045, 0.034), BLACK, up=unit(-0.35 * s, 0, 1), eps=0.004, rings=8,
                segs=40)

    def look(u, v):
        c = np.tile(srgb(WHITE), (len(u), 1))
        c[((u + 0.2) / 0.42) ** 2 + ((v + 0.15) / 0.52) ** 2 < 1] = srgb(PUPIL)
        c[((u + 0.08) / 0.12) ** 2 + ((v - 0.02) / 0.12) ** 2 < 1] = srgb(WHITE)
        return c
    def eyes(state):
        sp.lay_eyes(k, state, E, de, (0.03, 0.025), look, lambda u: 0.25 + 0.45 * u, lambda u: -0.05 + 0.6 * u,
                    srgb(BLACK), srgb(YELLOW))

    k.build(voxel=0.0068)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    zapdos(*args())
