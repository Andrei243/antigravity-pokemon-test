# Muk sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/muk.py -- <the export's Muk.glb> <out.glb>
# Grown from grimer.py (sibling("grimer")): its colours, puddle, drips, hands, dark bands and two-cutter mouth kept.
#
# From the references: a bigger heap, longer than it is tall, of several sagging lumps on a wide puddle, with ooze
# sagging in folds round its sides; a hood of a head leaning far forward over an enormous mouth that takes the whole front of
# it, dark within with a pale floor, a thick strand of slime hanging across it near one corner; small white eyes with
# black pupils on top of the hood, their tops slanting down to the middle in a scowl; two long thick arms reaching
# forward with soft stubby hands; dark wavy bands over the heap, the hood and the arms. The drawing's muted grey-purple
# is a little darker than Grimer's; the 3D model's is the same.
# The skeleton: body (0, 0, 0.2), armR/armL (+-0.162, -0.067, 0.324).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, polygon, sibling, args

g = sibling("grimer")
PURPLE, STRIPE, MOUTH, FLOOR = "#9381B4", "#5A4878", g.MOUTH, g.FLOOR
g.PURPLE = PURPLE                      # the line's makes colour by the module's own name

HOOD = np.array([0.0, -0.075, 0.44])
HOODR = np.array([0.175, 0.17, 0.105])
EYE = np.array([0.08, -0.2, 0.5])           # the +X eye's place on the hood
MOUTHC = np.array([0.0, -0.215, 0.345])
MOUTHR = (0.122, 0.075, 0.098)
FLOORC = np.array([0.0, -0.195, 0.3])
FLOORR = (0.092, 0.05, 0.036)
SHOULDER = (0.162, -0.067, 0.324)
ARM = [SHOULDER, (0.235, -0.16, 0.27), (0.29, -0.26, 0.21), (0.31, -0.32, 0.175)]
HAND = np.array([0.315, -0.355, 0.15])

# The eye: a white almond, its top slanting down toward the middle, a black pupil toward the inner corner
TOP = lambda u: 0.12 + 0.3 * u
ALMOND = [(1.0, TOP(1.0)), (0.5, TOP(0.5)), (0.0, TOP(0.0)), (-0.5, TOP(-0.5)), (-1.0, TOP(-1.0)), (-0.9, -0.42),
          (-0.5, -0.72), (0.0, -0.8), (0.5, -0.66), (0.9, -0.3)]


def eye_open(u, v):
    c = np.tile(srgb(g.WHITE), (len(u), 1))
    c[(u + 0.12) ** 2 / 0.3 ** 2 + (v + 0.12) ** 2 / 0.4 ** 2 < 1] = srgb(g.PUPIL)
    c[v > TOP(u) - 0.17] = srgb(g.INK)
    return c


def mouth_box(P):
    q = ((P[:, 0] / 0.16) ** 2 + ((P[:, 2] - MOUTHC[2]) / 0.13) ** 2)
    return 1 - inside(q - 1, 0.1) * (P[:, 1] < -0.1)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")

    # The heap: a wide puddle, a long low mass of sagging lumps, the upper mass and the hood leaning forward
    g.puddle(k, "puddle", (0, 0.04, 0.03), 0.37, 0.34, 0.045, "body", n=11, amp=0.08)
    k.add("mound", ell((0, 0.05, 0.12), (0.29, 0.27, 0.12)), PURPLE, "body")
    k.add("torso", ell((0, 0.02, 0.255), (0.215, 0.2, 0.15)), PURPLE, "body")
    k.add("upper", ell((0, -0.03, 0.36), (0.18, 0.17, 0.11)), PURPLE, "body")
    k.add("hood", ell(HOOD, HOODR, rot=(-12, 0, 0)), PURPLE, "body")
    k.add("brow", ell((0, -0.18, 0.478), (0.13, 0.06, 0.05)), PURPLE, "body")
    for i, (x, y, z, r) in enumerate([(0.22, -0.08, 0.13, 0.08), (-0.23, -0.06, 0.12, 0.08), (0.2, 0.17, 0.14, 0.085),
                                      (-0.18, 0.2, 0.15, 0.08), (0.0, 0.26, 0.13, 0.09), (0.17, 0.05, 0.27, 0.07),
                                      (-0.18, 0.06, 0.27, 0.07), (0.0, 0.17, 0.33, 0.08), (0.1, -0.17, 0.14, 0.07),
                                      (-0.12, -0.17, 0.13, 0.065)]):
        k.add(f"lump{i}", ell((x, y, z), (r, r, r * 0.8)), PURPLE, "body")
    # Sagging folds round the flanks: long soft rolls lying along the heap, their ends drooping toward the puddle
    def sag(V):
        V = V.copy()
        V[:, 2] -= 0.55 * V[:, 0] ** 2
        return V
    for i, (a, z, r, L) in enumerate(((20, 0.2, 0.205, 0.14), (95, 0.22, 0.2, 0.15), (160, 0.19, 0.215, 0.13))):
        t = math.radians(a)
        e = np.array([math.cos(t), math.sin(t), 0.0])
        k.add(f"fold{i}", ell(e * r + (0, 0.04, z), (L, 0.05, 0.042), rot=(0, 0, a + 90), deform=sag), PURPLE, "body")

    # The arms: long and thick, reaching forward and down, a soft hand of four stubby fingers, ooze hanging under
    for s in (1, -1):
        b = "armR" if s > 0 else "armL"
        g.arm(k, s, ARM, [0.07, 0.062, 0.056, 0.055], b)
        g.hand(k, s, HAND, 0.06, b)
        g.drip(k, f"armdrip{'R' if s > 0 else 'L'}", np.array([0.265, -0.21, 0.2]) * (s, 1, 1), 0.06, 0.02, b)

    # The mouth: the hood's whole front carved out, its pale floor a second cutter in its lower part
    k.add("mouth", ell(MOUTHC, MOUTHR, rot=(-10, 0, 0)), MOUTH, "body", carve=True)
    k.add("floor", ell(FLOORC, FLOORR), FLOOR, "body", carve=True)
    # A thick strand of slime across the mouth near its +X corner
    k.add("strand", tube([(0.098, -0.225, 0.43), (0.103, -0.238, 0.37), (0.098, -0.232, 0.31)], [0.018, 0.013, 0.018],
                         seg=14), PURPLE, "body", fuse=False)

    body = ["puddle", "mound", "torso", "upper", "hood", "lump", "fold"]
    k.paint(g.bands([0.07, 0.15, 0.23, 0.31, 0.39], 0.016, 0.009, keep=mouth_box), STRIPE, on=body)
    k.paint(lambda P: inside(np.abs(P[:, 2] - (0.49 + 0.06 * (P[:, 1] + 0.05) / 0.12)) - 0.008, 0.003)
            * (P[:, 1] > -0.1), STRIPE, on=["hood"])
    for s in (1, -1):
        k.paint(g.arm_bands(s, SHOULDER, HAND, (0.3, 0.55, 0.8), 0.009, 0.09), STRIPE, on=["arm", "hand"])

    k.build(voxel=0.007, smooth=3)

    CL, CI = srgb(PURPLE), srgb(g.INK)

    def eyes(state):
        for s in (1, -1):
            n = unit(0.38 * s, -0.78, 0.5)
            look = eye_look(state, eye_open, CL, CI, line=0.16)
            lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
            outline = polygon([(s * u, v) for u, v in (ALMOND if s > 0 else ALMOND[::-1])])
            k.decal(EYE * (s, 1, 1), n, (0.047, 0.039), lk, eps=0.004, outline=outline, rings=20, segs=56)

    k.eye_states(eyes)
    k.export(out)
