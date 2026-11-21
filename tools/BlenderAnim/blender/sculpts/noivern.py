# Noivern sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/noivern.py -- <the export's Noivern.glb> <out.glb>
# Grown from Noibat's script (sibling("noibat")): its ear dishes, its eye and its jags of fur, grown into a wyvern.
# From the references: a charcoal wyvern crouched on short clawed legs. Its head is carried forward under two great
# round loudspeaker dishes set side by side on its brow, facing forward, teal in bold black rings, a small black point
# over each; a red crest runs down between them into a pointed red snout; small yellow eyes under the dishes at the
# snout's sides. A white shaggy ruff round its neck and chest over a purple belly; great wings standing up from the
# shoulders like sails, charcoal with a broad purple trailing strip down their outer edges, a teal panel low against
# the body, two black spines standing up off each, three red claws hanging at each wing's foot; a black tail out
# behind to the +X side ending in a fan of spikes; red claws on the feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, outward, srgb, inside, smoothstep, unit, eye_look, polygon, args, \
    sibling

nb = sibling("noibat")

BLACK, PURPLE, TEAL, WHITE, RED = "#3B3338", "#8E4FC0", "#2FC2B4", "#ECEAE6", "#D42A34"
RING, CLAWRED = "#1C181C", "#C41E2C"
EYE, EYEINK = nb.EYE, nb.EYEINK

HC = np.array([0.0, -0.045, 0.452])            # the skull, carried forward of its joint (0, -0.02, 0.399)
HR = np.array([0.066, 0.06, 0.056])
DISH = np.array([0.054, -0.082, 0.49])        # the +X dish's middle on the brow
DISH_R = (0.05, 0.05, 0.018)
VOXEL = 0.0052

# the wing, in its own plane from the shoulder: a outward, b up
W_ORIGIN = np.array((0.075, 0.022, 0.35))
W_EX, W_EZ = unit(1, 0.32, 0), unit(0.16, 0.16, 1)
W_OUT = [(0.0, 0.035), (0.06, 0.14), (0.118, 0.225), (0.15, 0.13), (0.172, 0.0), (0.17, -0.12), (0.158, -0.245),
         (0.125, -0.2), (0.085, -0.14), (0.045, -0.085), (0.0, -0.05)]
W_PURPLE = [(0.118, 0.225), (0.15, 0.13), (0.172, 0.0), (0.17, -0.12), (0.158, -0.245), (0.122, -0.198),
            (0.112, -0.06), (0.108, 0.08), (0.104, 0.19)]
W_TEAL = [(0.0, 0.01), (0.035, -0.02), (0.07, -0.118), (0.045, -0.085), (0.0, -0.05)]


def wframe():
    ex = W_EX; ez = W_EZ - ex * (W_EZ @ ex); ez /= np.linalg.norm(ez)
    return ex, ez, np.cross(ex, ez)


def wp(a, b, lift=0.0):
    ex, ez, n = wframe()
    return W_ORIGIN + a * ex + b * ez + n * lift


def wings(k):
    ex, ez, n = wframe()
    nb.wing_plate(k, "wing", "wing{s}", W_ORIGIN, ex, ez, W_OUT, 0.017, BLACK)
    nb.wing_plate(k, "wpurple", "wing{s}", W_ORIGIN, ex, ez, W_PURPLE, 0.023, PURPLE)
    nb.wing_plate(k, "wteal", "wing{s}", W_ORIGIN, ex, ez, W_TEAL, 0.023, TEAL)
    # the arm along the leading edge, up to the wing's top
    k.add("arm", outward(tube([wp(-0.03, 0.0), wp(0.02, 0.06), wp(0.065, 0.145), wp(0.112, 0.215)],
                              [0.03, 0.024, 0.019, 0.014])), BLACK, "wing{s}", fuse=False, mirror=True)
    # two black spines standing up off the wing
    for j, (a, b, L) in enumerate(((0.07, 0.11, 0.075), (0.095, 0.03, 0.065))):
        k.add(f"spine{j}", nb.claw(wp(a, b), ez * 0.98 + ex * 0.2, L, 0.013, up=n), BLACK, "wing{s}", fuse=False,
              mirror=True, surface="shell")
    # the hand at the wing's foot and its three red claws hanging down
    foot = wp(0.152, -0.232)
    k.add("hand", outward(ell(foot, (0.02, 0.016, 0.022), sub=3)), BLACK, "wing{s}", fuse=False, mirror=True)
    for j, da in enumerate((-0.6, 0.0, 0.6)):
        d = -ez + ex * 0.35 * da - n * 0.25
        k.add(f"wclaw{j}", nb.claw(foot + ex * 0.012 * da - ez * 0.01, d, 0.048, 0.0105, up=n), CLAWRED, "wing{s}",
              fuse=False, mirror=True, surface="shell")


def noivern(src, out):
    k = Kit(src, coat="fur")

    # the body: a chest leaning forward, a purple belly, the thighs fused in as haunches
    k.add("chest", ell((0, -0.02, 0.33), (0.074, 0.066, 0.085), rot=(-18, 0, 0)), BLACK, "body")
    k.add("belly", ell((0, -0.004, 0.258), (0.07, 0.062, 0.066)), BLACK, "body")
    k.add("neck", tube([(0, -0.02, 0.36), (0, -0.035, 0.41)], [0.045, 0.042]), BLACK, "head")
    k.add("thigh", ell((0.07, 0.004, 0.196), (0.046, 0.058, 0.062), rot=(15, 0, -12)), BLACK, "leg{s}", mirror=True)
    k.add("shin", tube([(0.1, -0.04, 0.142), (0.118, 0.012, 0.07), (0.122, 0.005, 0.035)], [0.03, 0.022, 0.019]),
          BLACK, "leg{s}", mirror=True)
    k.add("foot", ell((0.124, -0.03, 0.02), (0.028, 0.044, 0.018)), BLACK, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.017, 0.0, 0.017)):
        k.add(f"toe{j}", nb.claw((0.124 + dx, -0.068, 0.018), (dx * 12, -1, -0.45), 0.032, 0.0095),
              CLAWRED, "leg{s}", fuse=False, mirror=True, surface="shell")
    k.add("heel", nb.claw((0.122, 0.012, 0.022), (0, 1, -0.5), 0.026, 0.009), CLAWRED, "leg{s}", fuse=False,
          mirror=True, surface="shell")

    # the tail: back and out to the +X side, a fan of spikes at its end
    TAIL = [(0, 0.05, 0.215), (0.03, 0.16, 0.175), (0.09, 0.25, 0.13), (0.16, 0.31, 0.1)]
    k.add("tail", tube(TAIL, [0.038, 0.026, 0.019, 0.013]), BLACK, "tail")
    e = np.array(TAIL[-1]); d = unit(*(e - np.array(TAIL[-2])))
    sideways = unit(*np.cross(d, (0, 0, 1)))
    for j, (a, b) in enumerate(((0.0, 0.0), (0.7, 0.55), (-0.7, 0.55), (0.0, 1.0))):
        dd = d + sideways * a * 0.8 + np.array((0, 0, 1.0)) * b * 0.7
        base = e - d * 0.02 * (j > 0)
        k.add(f"tailspike{j}", nb.claw(base, dd, 0.05 if j == 0 else 0.04, 0.012), BLACK, "tail", fuse=False,
              surface="shell")
    for j, t in enumerate((0.45, 0.7)):
        p = np.array(TAIL[1]) * (1 - t) + np.array(TAIL[2]) * t
        k.add(f"tailback{j}", nb.claw(p + (0, 0, 0.014), (0.2, 0.4, 1), 0.028, 0.009), BLACK, "tail", fuse=False,
              surface="shell")

    # the ruff: a white shag round the neck and over the chest, its jags standing out
    k.add("ruff", ell((0, -0.038, 0.356), (0.078, 0.058, 0.052)), WHITE, "body")
    k.add("ruffchest", ell((0, -0.06, 0.32), (0.05, 0.034, 0.05)), WHITE, "body")
    places = []
    for a, z, up in ((-80, 0.355, -0.1), (-45, 0.365, 0.05), (-10, 0.375, 0.4), (25, 0.37, 0.45), (60, 0.36, 0.3),
                     (100, 0.355, 0.2), (-100, 0.33, -0.4), (-70, 0.31, -0.6)):
        t = math.radians(a)
        dx, dy = math.cos(t), math.sin(t)
        places.append(((0.07 * dx, -0.038 + 0.052 * dy, z), (dx, dy, up), 0.04, 0.016))
    nb.tufts(k, "ruffjag", WHITE, "body", places)
    k.add("ruffjagF", nb.claw((0, -0.088, 0.31), (0, -0.4, -1), 0.04, 0.016), WHITE, "body", fuse=False)

    wings(k)

    # the head: a black skull carried forward, the jaw under a pointed red snout
    k.add("skull", ell(HC, HR), BLACK, "head")
    k.add("jaw", ell((0, -0.1, 0.405), (0.036, 0.05, 0.022), rot=(-12, 0, 0)), BLACK, "head")
    k.add("snout", tube([(0, -0.086, 0.43), (0, -0.125, 0.418), (0, -0.158, 0.398)], [0.024, 0.017, 0.006]), RED,
          "head")
    k.add("crest", tube([(0, -0.11, 0.462), (0, -0.136, 0.436), (0, -0.162, 0.405)], [0.017, 0.016, 0.008]),
          RED, "head")
    # the two dishes on the brow, facing forward, teal in bold black rings
    nb.ear_dish(k, "dish", "ear{s}", DISH, unit(1, 0, 0.35), unit(0.32, -1, 0.3), DISH_R, RING,
                [(0.88, TEAL, None, 0.003), (0.64, RING, None, 0.005), (0.47, TEAL, None, 0.007),
                 (0.2, RING, None, 0.009)], front=0.4)
    # a small black point over each dish
    k.add("earpoint", nb.claw((0.088, -0.058, 0.512), (0.55, 0.45, 1), 0.05, 0.016), BLACK, "ear{s}", fuse=False,
          mirror=True, surface="shell")

    # the purple belly painted crisp on the front
    k.paint(lambda P: inside(P[:, 1] + 0.006, 0.003) * inside(P[:, 2] - 0.315, 0.004) * inside(0.2 - P[:, 2], 0.004)
            * inside(np.abs(P[:, 0]) - 0.066, 0.004), PURPLE)

    k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.05)

    # the eyes: small, yellow, at the snout's sides under the dishes
    LID, INK = srgb(BLACK), srgb(EYEINK)          # a pale line for the closed states on the black face
    ED = unit(0.76, -0.62, -0.12)
    E = np.array([0.056, -0.084, 0.43])
    ALMOND = polygon([(-1, 0.0), (-0.5, 0.62), (0.5, 0.62), (1, 0.0), (0.5, -0.58), (-0.5, -0.58)])
    def eyes(state):
        k.decal(E, ED, (0.022, 0.016), eye_look(state, nb.slit_eye(rim=0.86, slit=0.3), LID,
                                                 INK if state in ("Open", "Fierce") else srgb("#A0949C"), line=0.2),
                rings=20, segs=56, eps=0.004, outline=ALMOND, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    noivern(*args())
