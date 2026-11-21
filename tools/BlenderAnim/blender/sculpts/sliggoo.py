# Sliggoo sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sliggoo.py -- <the export's Sliggoo.glb> <out.glb>
#
# Goomy grown (sibling("goomy")): its colours, its wavy purple line, its rounded horns, its green spots.
# From the references (drawn from the front three-quarter): a pale lavender slug whose neck rises from a purple
# puddle of slime that spreads in lobes round its foot, with a short flipper-like nub out to each front side; behind
# the neck a round pale shell-like hump with a spiral drawn on its sides; a round head on the neck, its lower jaw a
# little forward; a wide open smile with a pink tongue; big green oval eyes with a dark rim and no pupil; three small
# green spots down each cheek; two short pointed horns on the head's top front and two long feelers from behind them,
# rising, arching out and drooping back, each ending in a round knob.
# Skeleton: body (0, 0, 0.12), head (0, -0.03, 0.24), ears (+-0.02, -0.04, 0.32). The export is 0.39 tall.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, outward, srgb, inside, unit, eye_look, args, sibling

g = sibling("goomy")
PALE, PURPLE, LINE, INK = g.PALE, g.PURPLE, g.LINE, g.INK
SPIRAL, MOUTH, TONGUE = "#B6A2D6", "#4A2440", "#E8788E"
EYE, EYE_RIM, EYE_LIGHT = "#6CCB62", "#24402A", "#B8EAA8"

HEAD, HEADR = np.array([0.0, -0.052, 0.292]), np.array([0.08, 0.074, 0.068])
SHELL, SHELLR = np.array([0.0, 0.055, 0.115]), np.array([0.062, 0.068, 0.066])


def spiral_look(turns=2.2, width=0.085):
    """A spiral line drawn out from the middle of a round patch, on the patch's own colour."""
    P, S = srgb(PALE), srgb(SPIRAL)
    def look(u, v):
        c = np.tile(P, (len(u), 1))
        rr = np.sqrt(u * u + v * v)
        th = (np.arctan2(v, u) / (2 * math.pi)) % 1
        f = (rr * turns - th) % 1
        d = np.minimum(f, 1 - f) / turns
        c[(d < width / turns * 1.0) & (rr < 0.92) & (rr > 0.05)] = S
        return c
    return look


def mouth_look(top=0.15, curl=0.5, bottom=-0.75):
    """A wide open smile: its corners curl up; a pink tongue in its lower part."""
    P, M, T = srgb(PALE), srgb(MOUTH), srgb(TONGUE)
    def look(u, v):
        c = np.tile(P, (len(u), 1))
        hi = top + curl * u * u
        lo = bottom + 0.8 * u * u
        m = (v < hi) & (v > lo) & (np.abs(u) < 0.96)
        c[m] = M
        c[m & (v < lo + 0.45) & (((u / 0.6) ** 2 + ((v - lo) / 0.55) ** 2) < 1)] = T
        return c
    return look


def green_eye(u, v):
    """A green oval with a dark rim and a pale glint up and out; no pupil."""
    c = np.tile(srgb(EYE), (len(u), 1))
    c[u * u + v * v > 0.8 ** 2] = srgb(EYE_RIM)
    c[((u - 0.2) / 0.28) ** 2 + ((v - 0.35) / 0.26) ** 2 < 1] = srgb(EYE_LIGHT)
    return c


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")

    # The puddle, the flipper nubs, the shell-like hump behind, the neck rising in front of it
    g.puddle(k, "puddle", (0.0, 0.02, 0.028), 0.118, 0.115, 0.038, PURPLE, "body", lobes=6, amp=0.09)
    k.add("trail", ell((-0.03, 0.12, 0.015), (0.06, 0.06, 0.018), rot=(0, 0, 25)), PURPLE, "body")
    k.add("nub", tube([(0.07, -0.04, 0.07), (0.105, -0.065, 0.058), (0.128, -0.08, 0.048)], [0.026, 0.02, 0.016]),
          PURPLE, "body", mirror=True)
    k.add("shell", ell(SHELL, SHELLR), PALE, "body")
    k.add("neck", tube([(0.0, -0.015, 0.06), (0.0, -0.03, 0.14), (0.0, -0.04, 0.2), (0.0, -0.045, 0.25)],
                       [0.056, 0.042, 0.037, 0.042]), PALE, "head")
    k.add("head", ell(HEAD, HEADR), PALE, "head")
    k.add("jaw", ell(HEAD + (0, -0.022, -0.028), (0.064, 0.06, 0.044)), PALE, "head")

    # The purple below a wave, low at the front, a dark line along it
    k.paint(g.wave(0.078, 0.014), PURPLE)
    k.paint(lambda P: inside(P[:, 2] - 0.055, 0.002), PURPLE)
    k.paint(lambda P: g.wave(0.078, 0.014, line=0.0022)(P) * (P[:, 2] > 0.055), LINE)

    # The spiral on each side of the hump
    for s in (1, -1):
        p, n = g.on(SHELL, SHELLR, (s, 0.05, 0.12))
        k.decal(p, n, (0.05, 0.05), spiral_look(), eps=0.002, rings=40, segs=160, up=(0, 0, 1))

    # The short horns on the top front, the long feelers from behind them
    for s in (1, -1):
        m = np.array([s, 1, 1.0]); nm = "R" if s > 0 else "L"
        k.add(f"horn{nm}", g.feeler(np.array([(0.03, -0.072, 0.34), (0.038, -0.076, 0.366), (0.044, -0.078, 0.388)])
                                    * m, [0.013, 0.01, 0.0065], bulb=1.0), PALE, "head", fuse=False)
        k.add(f"feeler{nm}", g.feeler(np.array([(0.022, -0.03, 0.34), (0.045, -0.01, 0.395), (0.085, 0.03, 0.415),
                                                (0.13, 0.075, 0.395), (0.15, 0.1, 0.36)]) * m,
                                      [0.015, 0.0135, 0.0125, 0.012, 0.012], bulb=1.3), PALE, f"ear{nm}", fuse=False)

    # Three green spots down each cheek
    for s in (1, -1):
        for d, h in (((0.93, -0.25, -0.05), 0.012), ((0.9, -0.05, -0.38), 0.011), ((0.92, -0.32, -0.42), 0.009)):
            p, n = g.on(HEAD, HEADR, (d[0] * s, d[1], d[2]))
            g.spot(k, p, n, (h, h * 1.15), eps=0.0015)

    # The wide open smile with its tongue
    p, n = g.on(HEAD + (0, -0.022, -0.028), (0.064, 0.06, 0.044), (0, -0.85, -0.1))
    k.decal(p, unit(0, -1, 0.15), (0.047, 0.027), mouth_look(), eps=0.002, rings=24, segs=72)

    k.build(voxel=0.0036, smooth=4, band=0.005, wband=0.04)

    CP = srgb(PALE)

    def eyes(state):
        for s in (1, -1):
            p, n = g.on(HEAD, HEADR, (0.46 * s, -0.8, 0.3))
            k.decal(p, n, (0.018, 0.026), g.mirrored(eye_look(state, green_eye, CP, srgb(INK), line=0.18), s),
                    eps=0.002, rings=20, segs=56)

    k.eye_states(eyes)
    k.export(out)
