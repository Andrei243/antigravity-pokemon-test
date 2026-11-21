# Salazzle sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/salazzle.py -- <the export's Salazzle.glb> <out.glb>
# Salandit grown tall (built by salandit.py's functions: the line's grey, its almond lavender eye cut by the lid with
# a slit, its long splayed fingers, its limbs and its stripe along a tube). From the references: a dark grey lizard in
# a low crouch, the trunk leaning forward over great thighs that lie nearly level beside the hips, short shins sloping
# back to long feet of splayed toes; chest out and waist pinched; a thick neck curving forward in an S to a large head
# with a long flat beak hooked down at its tip and a point at the back of the crown; a slanted lavender eye under a
# heavy lid, a dark slit. A deep violet belly from under the chest down between the thighs and over their fronts;
# large pink flames on the chest either side of the middle and on the flanks; two pairs of long feelers streaming back
# from the shoulders and the small of the back; arms bent forward and down, long fingers spread before the belly; a
# tail thick at its root sweeping back and round on the ground to the +X side, its end pink.
# The pictures overrule the task's text: the marks are flames of pink on a grey body with a violet belly, and the body
# is a low crouch on great thighs, not a slender upright figure.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, polygon, args, sibling

S = sibling("salandit")
GREY = "#58585F"
VIOLET = "#4C3E8E"
PINK = "#F0428E"
INK = S.INK
VOXEL = 0.0058

H = np.array([0.0, -0.115, 0.56])            # the cranium
HR = np.array([0.074, 0.084, 0.068])
NECK = [(0, -0.045, 0.36), (0, -0.075, 0.415), (0, -0.07, 0.46), (0, -0.09, 0.505)]
TAIL = [(0, 0.08, 0.14), (0.015, 0.16, 0.08), (0.05, 0.225, 0.042), (0.12, 0.245, 0.036), (0.19, 0.195, 0.036),
        (0.218, 0.115, 0.036), (0.188, 0.05, 0.036)]
TAIL_R = [0.066, 0.05, 0.038, 0.031, 0.026, 0.021, 0.017]


def flame(tongues=3):
    """A flame's outline in u, v: a round foot and tongues licking up, the middle one tallest (star-shaped about its
    middle)."""
    pts = []
    for i in range(64):
        th = 2 * math.pi * i / 64
        c, s = math.cos(th), math.sin(th)
        if s < 0:
            r = 0.7
        else:
            a = th / math.pi                                   # 0 at the right, 1 at the left
            lick = abs(math.cos(a * math.pi * tongues))
            r = 0.62 + 0.38 * lick ** 3 * (1.0 if abs(a - 0.5) < 0.2 else 0.75)
        pts.append((r * c, r * s * 1.1 - 0.1))
    return polygon(pts)


def salazzle(src, out):
    k = Kit(src, coat="scales")

    # The trunk: hips low between the thighs, the waist pinched, the chest out and leaning forward over them
    k.add("chest", ell((0, -0.045, 0.32), (0.066, 0.058, 0.066), rot=(20, 0, 0)), GREY, "body")
    k.add("waist", ell((0, -0.005, 0.245), (0.054, 0.05, 0.06), rot=(12, 0, 0)), GREY, "body")
    k.add("hips", ell((0, 0.03, 0.165), (0.078, 0.07, 0.06)), GREY, "body")
    k.add("neck", tube(NECK, [0.052, 0.046, 0.043, 0.045]), GREY, "head")

    # The head: a large cranium with a point at the back of the crown, a long flat beak hooked down at its tip
    k.add("head", ell(H, HR), GREY, "head")
    k.add("crown", tube([H + (0, 0.03, 0.03), H + (0, 0.08, 0.025), H + (0, 0.112, 0.008)], [0.034, 0.021, 0.012]),
          GREY, "head")

    def flat_taper(V):
        V = V.copy()
        t = np.clip(-V[:, 1], 0, 1)
        V[:, 0] *= 1 - 0.35 * t
        return V
    k.add("snout", ell(H + (0, -0.11, -0.024), (0.042, 0.105, 0.024), deform=flat_taper), GREY, "head")
    k.add("hook", ell(H + (0, -0.2, -0.038), (0.026, 0.022, 0.017)), GREY, "head")
    k.add("jaw", ell(H + (0, -0.06, -0.038), (0.052, 0.085, 0.028)), GREY, "head")
    k.decal(H + (0.012, -0.175, -0.002), unit(0.3, -0.3, 0.9), (0.0055, 0.0042), INK, eps=0.002, rings=4, segs=16,
            mirror=True)

    # The thighs: great masses lying nearly level beside the hips, the knees forward, no higher than the hips; short
    # shins sloping back to long feet of splayed toes
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell((0.082 * s, -0.04, 0.15), (0.064, 0.105, 0.062), rot=(-6, 0, 18 * s)), GREY,
              f"leg{side}")
        S.limb(k, f"shin{side}", [(0.105, -0.125, 0.13), (0.112, -0.11, 0.075), (0.118, -0.095, 0.03)],
               [0.05, 0.04, 0.03], f"leg{side}", GREY, s)
        k.add(f"sole{side}", ell(np.array((0.12, -0.11, 0.016)) * m, (0.03, 0.04, 0.014)), GREY, f"leg{side}")
        S.fingers(k, f"toe{side}", (0.12, -0.135, 0.016), [(0.85, -0.5, 0), (0.4, -1, 0), (0.0, -1, 0),
                                                         (-0.4, -0.9, 0), (1, 0.15, 0)],
                  0.08, 0.0095, f"leg{side}", GREY, s)

    # The arms: from the shoulder out and back to the elbow, the forearm forward and down, long fingers spread before
    # the belly
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        S.limb(k, f"arm{side}", [(0.05, -0.04, 0.36), (0.09, -0.03, 0.3), (0.072, -0.11, 0.26)],
               [0.024, 0.019, 0.016], f"arm{side}", GREY, s)
        k.add(f"hand{side}", ell(np.array((0.067, -0.125, 0.255)) * m, (0.018, 0.017, 0.013)), GREY, f"arm{side}")
        S.fingers(k, f"finger{side}", (0.065, -0.134, 0.252), [(0.55, -0.7, -0.45), (0.15, -0.85, -0.5),
                                                              (-0.2, -0.8, -0.55), (-0.5, -0.6, -0.5)],
                  0.06, 0.0092, f"arm{side}", GREY, s)

    # The feelers: two pairs streaming back and out, from the shoulders and the small of the back
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"feelerA{side}", tube(np.array([(0.03, 0.0, 0.36), (0.07, 0.06, 0.375), (0.1, 0.12, 0.37),
                                                (0.12, 0.17, 0.345)]) * m, [0.02, 0.015, 0.012, 0.0105]), GREY, "body")
        k.add(f"feelerB{side}", tube(np.array([(0.036, 0.035, 0.25), (0.075, 0.09, 0.25), (0.11, 0.15, 0.235),
                                                (0.13, 0.2, 0.21)]) * m, [0.02, 0.015, 0.012, 0.0105]), GREY, "body")

    # The tail: thick at its root, back and down to the ground, sweeping round to the +X side
    k.add("tail", tube(TAIL, TAIL_R), GREY, "tail")

    # The violet belly: from under the chest down between the thighs and over their fronts and tops
    def belly(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        band = inside(z - 0.3, 0.004) * inside(0.09 - z, 0.004)
        trunk = inside(np.abs(x) - 0.046, 0.003) * inside(y + 0.0, 0.003)
        thigh = (np.abs(x) > 0.035) * inside(y + 0.02, 0.004) * inside(0.12 - z, 0.004)
        return band * np.maximum(trunk, thigh)
    k.paint(belly, VIOLET, on=["chest", "waist", "hips", "thigh"])

    # The tail's end, pink all round
    k.paint(S.stripe_mask(TAIL, TAIL_R, 9.0, t0=0.45, all_round=True), PINK, on=["tail"])

    # The flames: two large ones on the chest either side of the middle, one on each flank where grey meets violet
    for s in (1, -1):
        k.decal((0.034 * s, -0.095, 0.34), unit(0.45 * s, -1, 0.25), (0.03, 0.046), PINK, eps=0.0022, rings=14,
                segs=64, outline=flame())
        k.decal((0.058 * s, -0.025, 0.215), unit(1 * s, -0.3, 0.25), (0.024, 0.036), PINK, eps=0.0022, rings=14,
                segs=64, outline=flame())

    # The eyes: slanted almonds on the sides of the cranium under a heavy lid, a dark slit
    de = unit(0.8, -0.42, 0.42)
    E = S.on_ell(H, HR, de) + np.array([0, -0.004, 0.0])
    cut = S.lid_cut(0.2, 0.3, 0.05)
    lay = S.lay_eyes(k, E, de, (0.038, 0.033), cut, GREY, INK, slit=(0.13, 0.6), slit_at=(-0.1, -0.08))

    k.build(voxel=VOXEL, smooth=4, band=0.008, wband=0.04)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    salazzle(*args())
