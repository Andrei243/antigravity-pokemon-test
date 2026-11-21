# Mega Swampert sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/swampertmega.py -- <the export's Swampert-Mega.glb> <out.glb>
# Swampert (swampert.py: the line's blue, its slate fins, orange eyes, pale front and rounded orange pads, grown by
# mudkip.py's and marshtomp.py's functions) grown into its Mega, as the two pictures draw it: the largest change is the
# arms, now vast round forearms the size of the head ending in great dark slate fists of four thick fingers, resting
# on the ground before it, round red-orange pads on the forearms and the upper arms; a hunched body whose shoulders and
# back rise in a great hump high over the head, the head carried low and forward between the arms, its mouth shut over
# a pale jaw; the two head fins grown into long dark crests sweeping from the brow up and back over the hump; the gills
# grown into long red-orange spikes swept back and up from the cheeks; short thick hind legs with a pad on each thigh;
# a tall pointed dark tail fin rising behind.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, blade, srgb, inside, unit, eye_look, sibling, outward, axes, args

SW = sibling("swampert")
mk, mt = SW.mk, SW.mt
BLUE, SLATE, PALE, INK = SW.BLUE, SW.SLATE, SW.PALE, SW.INK
FIST = "#3C4A5C"
PAD = "#EE5A3E"
SPIKE = "#F0503A"
JAW = "#B9C4D0"
VOXEL = 0.0072

H = np.array([0, -0.3, 0.27])           # the head, low and forward between the arms
HR = np.array([0.16, 0.14, 0.1])
EYE_D = unit(0.36, -0.8, 0.48)
EYE_HALF = (0.024, 0.024)
ARM_X = 0.185                           # the forearms' middle across


def pad(k, name, c, n, w, l, ldir=(0, 0, 1.0), bone="body", mirror=True):
    """A round red-orange pad standing on the surface at c, its face along n: a loose flattened ball."""
    n = unit(*n)
    ld = np.array(ldir, float); ld = ld - n * (ld @ n); ld /= np.linalg.norm(ld)
    wd = np.cross(n, ld)
    k.add(name, outward(mk.plate(np.array(c, float), wd, n, ld, w, 0.02, l, sub=3)), PAD, bone, fuse=False,
          mirror=mirror, surface="shell")


def spike(base, d, length, width, thick, across):
    """A long point as one flat-faced solid: a kite at its root, full at a third, to its tip along d."""
    d = unit(*d)
    ex = np.array(across, float); ex = ex - d * (ex @ d); ex /= np.linalg.norm(ex)
    nn = np.cross(d, ex)
    b = np.array(base, float)
    m = b + d * length * 0.3
    pts = [b + ex * width, b - ex * width, b + nn * thick, b - nn * thick,
           m + ex * width * 0.9, m - ex * width * 0.9, m + nn * thick * 0.85, m - nn * thick * 0.85, b + d * length]
    return hull(pts)


TAILFIN = [(-0.07, 0.0), (0.16, -0.02), (0.2, 0.06), (0.17, 0.16), (0.1, 0.27), (0.03, 0.36), (-0.03, 0.22),
           (-0.07, 0.08)]


def swampertmega(src, out):
    k = Kit(src, coat="scales")

    # The body: a great hunched mass, the shoulders and back rising high over the head, a pale chest and belly
    def hump(V):
        V = V.copy()
        front = np.clip(-V[:, 1], 0, 1)
        V[:, 2] = np.where(V[:, 2] > 0, V[:, 2] * (1 + 0.25 * front), V[:, 2])
        return V
    k.add("trunk", ell((0, 0.02, 0.34), (0.2, 0.25, 0.2), deform=hump), BLUE, "body")
    k.add("hump", ell((0, -0.07, 0.5), (0.19, 0.18, 0.2)), BLUE, "body")
    k.add("belly", ell((0, -0.06, 0.2), (0.16, 0.2, 0.11)), BLUE, "body")
    k.add("chest", ell((0, -0.2, 0.25), (0.15, 0.11, 0.12)), BLUE, "body")
    k.add("neck", ell((0, -0.24, 0.3), (0.12, 0.1, 0.09)), BLUE, "head")
    k.add("shoulder", ell((0.165, -0.13, 0.42), (0.125, 0.13, 0.13)), BLUE, "leg{s}", mirror=True)

    # The head: wide and flat, low and forward, its mouth shut over a pale jaw
    def flat(V):
        V = V.copy()
        V[:, 0] *= 1 + 0.1 * np.clip(-V[:, 2], 0, 1)
        V[:, 2] = np.where(V[:, 2] > 0, V[:, 2] * 0.85, V[:, 2])
        return V
    k.add("head", ell(H, HR, deform=flat), BLUE, "head")
    k.add("muzzle", ell((0, H[1] - 0.06, H[2] - 0.025), (0.11, 0.08, 0.062)), BLUE, "head")
    k.add("jaw", ell((0, H[1] - 0.035, H[2] - 0.06), (0.115, 0.085, 0.04)), BLUE, "head")

    # The arms: vast round forearms ending in dark fists of four thick fingers on the ground
    sh, el = np.array([0.18, -0.15, 0.42]), np.array([ARM_X + 0.02, -0.25, 0.3])
    k.add("upperarm", tube([sh, el], [0.09, 0.095]), BLUE, "leg{s}", mirror=True)
    fc = np.array([ARM_X, -0.33, 0.215])
    k.add("forearm", ell(fc, (0.125, 0.13, 0.15), rot=(-15, 0, 0)), BLUE, "leg{s}", mirror=True)
    fist = np.array([ARM_X - 0.005, -0.385, 0.075])
    k.add("fist", ell(fist, (0.092, 0.075, 0.072)), FIST, "leg{s}", mirror=True)
    for i, z in enumerate((0.115, 0.075, 0.035)):
        k.add(f"finger{i}", ell(fist + (0, -0.05, z - 0.075), (0.078, 0.042, 0.026)), FIST, "leg{s}", mirror=True)
    k.add("thumb", ell(fist + (-0.06, -0.035, 0.03), (0.03, 0.04, 0.03)), FIST, "leg{s}", mirror=True)
    pad(k, "armpad0", fc + (0.124, 0.0, 0.02), (1, -0.15, 0.1), 0.055, 0.06, bone="leg{s}")
    pad(k, "armpad1", fc + (0.084, -0.084, -0.066), (0.6, -0.7, -0.2), 0.04, 0.04, bone="leg{s}")
    pad(k, "armpad2", fc + (0.05, -0.105, 0.066), (0.35, -0.9, 0.3), 0.035, 0.038, bone="leg{s}")
    pad(k, "shoulderpad", (0.255, -0.11, 0.49), (0.75, -0.1, 0.65), 0.05, 0.055, bone="leg{s}")

    # The hind legs: short thick haunches, broad feet of three toes, a pad on each thigh
    k.add("thigh", ell((0.16, 0.15, 0.2), (0.12, 0.14, 0.13)), BLUE, "hind{s}", mirror=True)
    k.add("shin", tube([(0.2, 0.18, 0.13), (0.215, 0.19, 0.08)], [0.07, 0.062]), BLUE, "hind{s}", mirror=True)
    k.add("foot", ell((0.225, 0.15, 0.029), (0.07, 0.1, 0.027), rot=(0, 0, -15)), BLUE, "hind{s}", mirror=True)
    for i, a in enumerate((-0.5, 0.0, 0.5)):
        d = np.array([math.sin(a - 0.25), -math.cos(a - 0.25), 0])
        k.add(f"toe{i}", tube([(0.225, 0.13, 0.024) + d * 0.04, (0.225, 0.13, 0.022) + d * 0.09], [0.024, 0.021]),
              BLUE, "hind{s}", mirror=True)
    pad(k, "legpad", (0.278, 0.15, 0.21), (1, 0.05, 0.15), 0.055, 0.06, ldir=(0, 1, -0.1), bone="hind{s}")

    # The crests: long dark fins from the brow up and back over the hump, each tilted out a little
    path = [(0.062, -0.355, 0.37), (0.07, -0.28, 0.5), (0.078, -0.16, 0.64), (0.085, -0.02, 0.7), (0.09, 0.12, 0.64)]
    k.add("crest", blade(path, [0.04, 0.08, 0.095, 0.08, 0.02], (1, 0, -0.55), 0.3), SLATE, "head", fuse=False,
          mirror=True, surface="skin")

    # The gills: long red-orange spikes swept back and up from each cheek
    gc = np.array([0.165, H[1] + 0.01, H[2] + 0.0])
    for j, (d, ln, w) in enumerate([((0.4, 0.3, 0.86), 0.34, 0.034), ((0.75, 0.3, 0.55), 0.25, 0.028),
                                    ((0.85, 0.0, 0.3), 0.17, 0.024)]):
        k.add(f"gill{j}", spike(gc, d, ln, w * 1.7, 0.024, (0, 0, 1) if j < 2 else (0, 0.3, 1)), SPIKE, "head", fuse=False,
              mirror=True, surface="skin")

    # The tail fin: tall and pointed, run back and out to +X so its face shows to both cameras
    out_ = unit(0.57, 0.82, 0)
    k.add("tailroot", tube([(0, 0.24, 0.3), (0, 0.24, 0.3) + out_ * 0.1], [0.07, 0.045]), BLUE, "tail")
    k.add("tailfin", slab(TAILFIN, (0.02, 0.26, 0.33), out_, (0, 0, 1.0), 0.042), SLATE, "tail", fuse=False,
          surface="skin")

    # The pale chest and belly; the pale jaw under a shut mouth
    def pale(P):
        zline = np.minimum(0.29 - 0.55 * (P[:, 1] + 0.08), 0.29)
        return (inside(P[:, 2] - zline, 0.004) * inside(np.abs(P[:, 0]) - 0.11, 0.01) * inside(P[:, 1] - 0.2, 0.01)
                * inside(P[:, 1] + 0.27, 0.01))
    k.paint(pale, PALE, on=["trunk", "belly", "chest", "neck"])
    mk.chin(k, H, H[2] - 0.045, 0.02, 0.9, JAW, H[1] + 0.0, on=["jaw", "muzzle", "head"])

    k.build(voxel=VOXEL, band=0.006)
    lid = srgb(BLUE)
    k.eye_states(lambda state: k.decal(mk.on_ell(H, HR, EYE_D), EYE_D, EYE_HALF,
                                       eye_look(state, mt.eye_open, lid, srgb(INK), line=0.2), eps=0.004, rings=20,
                                       segs=56, mirror=True))
    k.export(out)


if __name__ == "__main__":
    swampertmega(*args())
