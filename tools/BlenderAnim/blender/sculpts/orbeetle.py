# Orbeetle sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/orbeetle.py -- <the export's Orbeetle.glb> <out.glb>
#
# Blipbug grown (sibling("blipbug"): its spike, its way of laying a patch on an ellipsoid). From the references, which
# overrule the task where they differ: a great red dome, the largest mass, half the height and as wide as the whole,
# spotted with big round navy-violet spots (not black), dark under its rim; it hovers. Under its front a dark brown
# head (not blue), a white mask over its face, two big cyan eyes with a dark ring inside them, slanted fiercely; a dark
# pointed horn rising from the brow up before the dome; two long gold feelers swept out and up from beside the eyes
# like a moustache, short gold strokes from the same roots meeting over the horn's root; a small red mouth. A small
# dark body under the head with a red belly; two thin dark arms bent out and down at the sides, three claws on each
# hand (the front pair of legs); two dark angular pods hanging under it, a red band on each and a curved gold claw at
# its end (the hind pair).
# Skeleton: body (0, 0, 0.429), legs front (+-0.04, -0.03, 0.299), back (+-0.04, 0.03, 0.299). The export stands
# 0.065 to 0.609.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, eye_look, polygon, args, sibling

bb = sibling("blipbug")
RED, UNDER, SPOT = "#E2352C", "#5A2A2C", "#4A4396"
DARK, MASK, CYAN, RING, GOLD, MOUTH = "#4B3E40", "#EEEEEC", "#5EE2EA", "#1E3E52", "#F6C334", "#D9302C"

DC = np.array([0.0, 0.03, 0.43])          # the dome's middle (its rim)
DR = np.array([0.255, 0.245, 0.26])       # its radii above the rim
DROP = 0.07                               # how far its underside sags below the rim
HC = np.array([0.0, -0.19, 0.365])        # the head
HR = np.array([0.097, 0.084, 0.086])


def dome(k):
    def shape(V):
        V = V.copy()
        V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * DROP / DR[2], V[:, 2])
        return V
    k.add("dome", ell(DC, DR, deform=shape, sub=5), RED, "body")
    # dark under the rim
    k.paint(lambda P: inside(P[:, 2] - (DC[2] - 0.004), 0.003) * inside(-(P[:, 1] - (HC[1] + 0.07)), 0.003)
            * inside(np.abs(P[:, 0]) - 0.3, 0.003), UNDER)


SPOTS = [  # (azimuth from the front, elevation, half size)
    (0, 52, 0.06), (52, 22, 0.075), (-52, 22, 0.075), (118, 42, 0.08), (-118, 42, 0.08), (180, 72, 0.07),
    (180, 18, 0.07), (90, 75, 0.05)]


def spots(k):
    for az, el, h in SPOTS:
        a, e = math.radians(az), math.radians(el)
        d = (math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e))
        p, n = bb.on_ell(d, DC, DR)
        k.decal(p, n, (h, h), SPOT, up=(0, 1, 0) if el > 60 else (0, 0, 1), eps=0.0015, rings=14, segs=48)


def head(k):
    k.add("head", ell(HC, HR), DARK, "body")
    k.add("thorax", ell((0, -0.1, 0.29), (0.055, 0.055, 0.05)), DARK, "body")
    k.paint(lambda P: inside(P[:, 2] - 0.275, 0.003) * inside(-(P[:, 1] + 0.03), 0.003), RED, on=["thorax"])
    # the horn up the brow before the dome
    r = HC + np.array([0, -0.05, 0.055])
    k.add("horn", hull([r + (-0.032, 0, -0.01), r + (0.032, 0, -0.01), r + (0, 0.035, -0.01), r + (0, -0.012, -0.02),
                        r + (0, -0.03, 0.135), r + (0, -0.012, 0.135)]), DARK, "body", fuse=False)
    # the gold feelers, and short strokes from their roots meeting over the horn's root
    for s in (1, -1):
        root = np.array([0.07 * s, -0.245, 0.35])
        bb.spike(k, "feeler" + ("R" if s > 0 else "L"), root, (0.84 * s, 0.25, 0.48), 0.21, 0.026, "body", GOLD, t=0.018)
        bb.spike(k, "stroke" + ("R" if s > 0 else "L"), root, (-0.6 * s, -0.12, 0.8), 0.085, 0.02, "body", GOLD, t=0.014)
    # the white mask, its top cut down to the horn, and a small red mouth
    p, n = bb.on_ell((0, -1, 0.05), HC, HR)
    k.decal(p, n, (0.074, 0.05), srgb(MASK), eps=0.002, rings=16, segs=64,
            outline=polygon([(-1.1, -0.85), (1.1, -0.85), (1.1, 0.75), (0.3, 1.0), (0, 0.55), (-0.3, 1.0), (-1.1, 0.75)]))
    p, n = bb.on_ell((0, -1, -0.62), HC, HR)
    k.decal(p, n, (0.016, 0.014), srgb(MOUTH), eps=0.004, rings=8, segs=36,
            outline=polygon([(-1, 0.8), (1, 0.8), (0, -1)]))


def arm(k, s):
    """A thin dark arm bent out and down at the side, three claws on its hand (the front legs)."""
    side = "R" if s > 0 else "L"
    sh, el, hand = np.array([0.05 * s, -0.08, 0.305]), np.array([0.17 * s, -0.12, 0.33]), np.array([0.2 * s, -0.14, 0.24])
    k.add("arm" + side, tube([sh, el, hand], [0.019, 0.019, 0.017]), DARK, f"leg{side}f", fuse=False)
    k.add("hand" + side, ell(hand, (0.024, 0.024, 0.024)), DARK, f"leg{side}f", fuse=False)
    for d in ((0.5 * s, -0.4, -1), (-0.2 * s, -0.6, -1), (0.1 * s, 0.4, -1)):
        bb.spike(k, f"claw{side}{d[1]}", hand + np.array([0, 0, -0.01]), d, 0.05, 0.017, f"leg{side}f", DARK)


def pod(k, s):
    """A dark angular pod hanging under the body, a red band low on it, a curved gold claw at its end (hind legs)."""
    side = "R" if s > 0 else "L"
    bone = f"leg{side}b"
    top, mid, low = np.array([0.04 * s, -0.08, 0.29]), np.array([0.09 * s, -0.1, 0.235]), np.array([0.095 * s, -0.1, 0.13])
    k.add("strut" + side, tube([top, mid], [0.018, 0.017]), DARK, bone, fuse=False)
    w = 0.042
    k.add("pod" + side, hull([mid + (0, 0, 0.02), mid + (w * s, 0, -0.02), mid + (-w * 0.7 * s, 0, -0.02),
                              mid + (0, -w, -0.02), mid + (0, w * 0.8, -0.02),
                              low + (w * 0.9 * s, 0, 0.02), low + (-w * 0.6 * s, 0, 0.02), low + (0, -w * 0.9, 0.02),
                              low + (0, w * 0.7, 0.02), low + (0.004 * s, -0.004, -0.004)]), DARK, bone, fuse=False)
    b = low + np.array([0.0, 0.0, 0.03])
    k.add("band" + side, hull([b + (w * 0.95 * s, 0, 0.022), b + (-w * 0.66 * s, 0, 0.022), b + (0, -w * 0.96, 0.022),
                               b + (0, w * 0.76, 0.022), b + (w * 0.9 * s, 0, -0.018), b + (-w * 0.62 * s, 0, -0.018),
                               b + (0, -w * 0.92, -0.018), b + (0, w * 0.72, -0.018)]), RED, bone, fuse=False)
    c0, c1, c2 = low + (0, 0, 0.012), low + (-0.014 * s, -0.014, -0.05), low + (-0.045 * s, -0.026, -0.085)
    k.add("clawa" + side, hull([c0 + (0.03 * s, 0, 0), c0 + (-0.025 * s, 0, 0), c0 + (0, -0.028, 0), c0 + (0, 0.022, 0),
                                c1 + (0.016 * s, 0, 0), c1 + (-0.013 * s, 0, 0), c1 + (0, -0.014, 0), c1 + (0, 0.012, 0)]),
          GOLD, bone, fuse=False)
    k.add("clawb" + side, hull([c1 + (0.016 * s, 0, 0.004), c1 + (-0.013 * s, 0, 0.004), c1 + (0, -0.014, 0.004),
                                c1 + (0, 0.012, 0.004), c2]), GOLD, bone, fuse=False)


EYE_D = (0.44, -0.88, 0.12)
EYE_HALF = (0.032, 0.027)
FIERCE = polygon([(-3, -3), (3, -3), (3, 0.9), (-3, 0.0)])


def cut(th):
    return min(1.0, FIERCE(th))


def eye_open(u, v, cyan=srgb(CYAN), ring=srgb(RING)):
    r = np.sqrt((u + 0.08) ** 2 + (v + 0.05) ** 2)
    c = np.tile(cyan, (len(u), 1))
    c[(r > 0.3) & (r < 0.5)] = ring
    c[r < 0.12] = ring
    c[np.sqrt(u * u + v * v) > 0.84] = ring
    return c


def eyes(k, state):
    p, n = bb.on_ell(EYE_D, HC, HR)
    look = eye_look(state, eye_open, srgb(MASK), srgb(RING), line=0.17)
    k.decal(p, n, EYE_HALF, look, eps=0.004, rings=20, segs=56, outline=cut, mirror=True)


def orbeetle(src, dst):
    k = Kit(src, coat="shell")
    dome(k)
    spots(k)
    head(k)
    for s in (1, -1):
        arm(k, s)
        pod(k, s)
    k.build(voxel=0.006, smooth=4, band=0.004, wband=0.05)
    k.eye_states(lambda s: eyes(k, s))
    k.export(dst)


if __name__ == "__main__":
    orbeetle(*args())
