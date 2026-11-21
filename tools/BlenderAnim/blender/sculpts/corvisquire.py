# Corvisquire sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/corvisquire.py -- <the export's Corvisquire.glb> <out.glb>
# Rookidee grown (its spikes, beak cone, tail, feet and way of making the eyes, imported from rookidee.py): from the
# references, an upright crow on long lavender-grey legs with black talons; a dark head with a ragged crest of spikes
# swept back; a pale lavender mask round the eye and the base of the hooked lavender beak, a blue streak over it running
# back; a fierce red eye in a narrow white cut by the brow; a dark shaggy bib over the neck and chest ending in points
# over a pale blue belly; a blue back; wings raised in a V, blue over their inner part with long dark fingered feathers;
# a long blue tail hanging down behind.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, tri, unit, args, sibling

r = sibling("rookidee")
spike, on_ell = r.spike, r.on_ell

DARK, BLUE, WINGBLUE, BELLY = "#2E2C30", "#3A68B8", "#3F72CC", "#86A4CC"
PALE, BEAKC, LEGC, TALON = "#B4B4D2", "#9A9CBC", "#9A9CBE", "#1E1C22"
WHITE, RED, GLINT, INK = "#F6F6F2", "#D8202E", "#FFFFFF", "#26242A"


def frame():
    F = SimpleNamespace()
    F.H = np.array([0, -0.1, 0.372])                 # the head's centre, before its joint (0, -0.07, 0.34)
    F.HR = np.array([0.073, 0.08, 0.069])
    F.NECK = ((0, -0.062, 0.33), (0.064, 0.062, 0.064))
    F.BODY = ((0, 0.01, 0.255), (0.098, 0.112, 0.128), (-16, 0, 0))
    F.CHEST = ((0, -0.045, 0.29), (0.086, 0.074, 0.09))
    F.SH = np.array([0.062, -0.02, 0.318])           # the wings' joint (0.06, -0.02, 0.31)
    F.LEGX, F.LEGTOP, F.ANKLE, F.LEGY = 0.043, 0.19, 0.02, 0.0
    F.LEGR = (0.019, 0.016)
    F.TOE, F.HEEL, F.TOER = 0.06, 0.034, 0.0092
    F.EYEDIR = unit(0.6, -0.74, 0.2)
    F.EYE = (0.027, 0.021)
    F.TAILBASE = np.array([0, 0.1, 0.22])
    F.BIB = 0.255                                     # the bib's lower edge at the front
    F.VOXEL = 0.005
    return F


def body(k, F):
    k.add("neck", ell(*F.NECK), DARK, "head")
    c, rr, rot = F.BODY
    k.add("body", ell(c, rr, rot=rot), BELLY, "body")
    k.add("chest", ell(*F.CHEST), DARK, "body")
    k.add("head", ell(F.H, F.HR), DARK, "head")
    for s, b in ((1, "legR"), (-1, "legL")):
        k.add("thigh", ell((0.05 * s, 0.0, 0.17), (0.046, 0.052, 0.056)), BELLY, b)


def coat(k, F):
    """The blue back, the dark bib ending in points over the pale blue belly, the pale mask and the blue streak."""
    # the back and the body's sides behind the bib: blue
    k.paint(lambda P: inside(-0.01 - P[:, 1] + 0.3 * (P[:, 2] - 0.3), 0.006) * inside(P[:, 2] - 0.36, 0.004)
            * inside(0.17 - P[:, 2], 0.004), BLUE, on=["body", "chest", "neck"])
    # the bib: dark down the front to a ragged edge of points
    edge = lambda P: F.BIB + 0.02 * tri(P[:, 0] / 0.045 + 0.5) - 0.12 * np.clip(P[:, 1] + 0.02, -0.05, 0.1)
    k.paint(lambda P: inside(edge(P) - P[:, 2], 0.002) * inside(P[:, 1] - 0.02, 0.006)
            * inside(0.36 - P[:, 2], 0.01), DARK, on=["body", "chest"])
    # the pale mask round the eye and the beak's base
    H = F.H
    for s in (1, -1):
        c = H + np.array([0.042 * s, -0.052, 0.0])
        k.paint(lambda P, c=c: inside(np.linalg.norm((P - c) / np.array([0.05, 0.062, 0.036]), axis=1) - 1, 0.06),
                PALE, on=["head"])
    # the blue streak over the mask, from the beak's top back over the eye
    k.paint(lambda P: inside(np.abs(P[:, 2] - (H[2] + 0.03 - 0.25 * (P[:, 1] - H[1] + 0.06))) - 0.0075, 0.002)
            * inside(np.abs(P[:, 0]) - 0.07, 0.004) * inside(0.03 - np.abs(P[:, 0]), 0.004)
            * inside(P[:, 1] - (H[1] + 0.045), 0.004), BLUE, on=["head"])


def crest(k, F, colour=DARK):
    top = F.H + np.array([0, 0, F.HR[2]])
    for b, t, w, th in (((0, -0.05, -0.006), (0, 0.01, 0.07), 0.048, 0.02),
                        ((0, -0.008, -0.004), (0, 0.06, 0.05), 0.044, 0.018),
                        ((0, 0.03, -0.02), (0, 0.1, 0.012), 0.04, 0.017)):
        k.add("crest", spike(top + b, top + t, w, th, side=(1, 0, 0)), colour, "head", fuse=False)
    for s in (1, -1):
        k.add("crestside", spike(top + (0.04 * s, -0.02, -0.02), top + (0.075 * s, 0.05, 0.04), 0.04, 0.017,
                                 side=(0.3 * s, 0, 1)), colour, "head", fuse=False)
        k.add("crestback", spike(top + (0.05 * s, 0.03, -0.04), top + (0.085 * s, 0.1, -0.01), 0.036, 0.016,
                                 side=(0.3 * s, 0, 1)), colour, "head", fuse=False)


def ruff(k, F, colour=DARK):
    """The bib's ragged sides: dark feather points hanging down and out over the shoulders and chest."""
    for s in (1, -1):
        for j, (b, t) in enumerate((((0.06, -0.03, 0.3), (0.1, -0.02, 0.24)),
                                    ((0.05, -0.06, 0.28), (0.075, -0.07, 0.22)))):
            b = np.array(b) * (s, 1, 1); t = np.array(t) * (s, 1, 1)
            k.add("ruffpt", spike(b, t, 0.04, 0.018, side=(0, 1, 0)), colour, "body", fuse=False)


def beak(k, F):
    pg = sibling("pidgey")
    H = F.H
    k.add("beak", ell(H + (0, -0.09, -0.006), (0.024, 0.052, 0.022), rot=(10, 0, 0), deform=pg.hooked), BEAKC,
          "head", fuse=False, surface="shell")
    k.add("beaklow", ell(H + (0, -0.078, -0.026), (0.017, 0.036, 0.009), rot=(-6, 0, 0), deform=pg.pointed), BEAKC,
          "head", fuse=False, surface="shell")


def wings(k, F, scale=0.64):
    """Raised in a V as a mirror pair: the dark of the whole wing, the blue over its inner part thicker than it."""
    tf = sibling("talonflame")
    tf.ELEV, tf.SWEEP = 42, 24
    d, c = tf.wing_axes()
    root = F.SH - d * 0.02
    t = 0.011
    sc = lambda pts: [(a * scale, b * scale) for a, b in pts]
    k.add("wing", slab(sc(tf.wing_outline()), root, d, c, t), DARK, "wing{s}", fuse=False, mirror=True)
    blue = [(0.0, -0.022), (0.12, -0.032), (0.25, -0.027), (0.33, -0.012), (0.3, 0.012), (0.265, 0.02),
            (0.24, 0.05), (0.2, 0.034), (0.17, 0.08), (0.13, 0.058), (0.09, 0.1), (0.05, 0.08), (0.012, 0.106),
            (0.0, 0.1)]
    k.add("wingblue", slab(sc(blue), root, d, c, t * 1.6), WINGBLUE, "wing{s}", fuse=False, mirror=True)
    k.add("shoulder", ell(F.SH + (0.0, 0.02, -0.01), (0.034, 0.05, 0.038)), BLUE, "wing{s}", mirror=True)


def corvisquire(src, out_path):
    F = frame()
    k = Kit(src, coat="fur")
    body(k, F)
    coat(k, F)
    crest(k, F)
    ruff(k, F)
    beak(k, F)
    wings(k, F)
    r.tail(k, F, [(14, 0.23, 0.056, BLUE), (2, 0.2, 0.05, WINGBLUE), (28, 0.19, 0.05, WINGBLUE)], pitch=-38,
           thick=0.012, lean=30)
    r.legs(k, F, foot=LEGC, claw=TALON, clawlen=2.8)
    k.build(voxel=F.VOXEL, band=0.005, wband=0.04)
    k.eye_states(r.eyes(k, F, r.iris_eye(WHITE, RED, GLINT, at=(-0.18, -0.12), r=0.52),
                        lambda u: 0.3 + 0.45 * u, lambda u: -0.05 + 0.6 * u, lid=PALE, ink=INK))
    k.export(out_path)


if __name__ == "__main__":
    corvisquire(*args())
