# Salazzle sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/salazzle.py -- <the export's Salazzle.glb> <out.glb>
# Salandit grown tall (built by salandit.py's functions: the line's grey, its almond lavender eye cut by the lid with
# a slit, its long splayed fingers, its limbs and its stripe along a tube). From the references: a dark grey lizard
# crouched upright, chest out and waist pinched, a long neck curving forward to a head with a long beak of a snout
# hooked down at its tip and a point at the back of the crown; a slanted lavender eye under a heavy lid, a dark slit.
# A deep violet belly from under the chest down over the fronts of great crouched thighs; pink flames on the chest
# either side of the middle and on the flanks; two pairs of long feelers streaming back from the shoulders and the
# small of the back; arms bent forward and down, long fingers spread before the belly; short thick shins, feet of long
# splayed toes; a long tail sweeping back and round on the ground to the +X side, its end pink.
# The pictures overrule the task's text: the marks are flames of pink on a grey body with a violet belly, and the body
# is a crouch on great thighs, not a slender upright figure.
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

H = np.array([0.0, -0.075, 0.585])           # the cranium
HR = np.array([0.057, 0.066, 0.053])
NECK = [(0, 0.0, 0.43), (0, -0.012, 0.48), (0, -0.035, 0.53), (0, -0.055, 0.565)]
TAIL = [(0, 0.06, 0.22), (0.015, 0.14, 0.13), (0.05, 0.2, 0.05), (0.12, 0.22, 0.03), (0.19, 0.17, 0.03),
        (0.215, 0.09, 0.03), (0.185, 0.035, 0.032)]
TAIL_R = [0.05, 0.038, 0.03, 0.025, 0.021, 0.017, 0.013]


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

    # The trunk: chest out, waist pinched, broad hips sitting low between the thighs
    k.add("chest", ell((0, -0.012, 0.405), (0.066, 0.056, 0.068), rot=(-10, 0, 0)), GREY, "body")
    k.add("waist", ell((0, 0.004, 0.33), (0.052, 0.047, 0.06)), GREY, "body")
    k.add("hips", ell((0, 0.02, 0.245), (0.076, 0.064, 0.058)), GREY, "body")
    k.add("neck", tube(NECK, [0.046, 0.04, 0.037, 0.038]), GREY, "head")

    # The head: a cranium with a point at the back of the crown, a long beak of a snout hooked down at its tip
    k.add("head", ell(H, HR), GREY, "head")
    k.add("crown", tube([H + (0, 0.02, 0.02), H + (0, 0.06, 0.018), H + (0, 0.085, 0.006)], [0.026, 0.016, 0.009]),
          GREY, "head")
    k.add("snout", tube([H + (0, -0.03, -0.012), H + (0, -0.085, -0.016), H + (0, -0.13, -0.024),
                         H + (0, -0.15, -0.042)], [0.03, 0.022, 0.016, 0.012]), GREY, "head")
    k.add("jaw", ell(H + (0, -0.035, -0.028), (0.036, 0.06, 0.022)), GREY, "head")
    k.decal(H + (0.008, -0.134, -0.012), unit(0.3, -0.6, 0.75), (0.0045, 0.0035), INK, eps=0.002, rings=4, segs=16,
            mirror=True)

    # The thighs: great masses crouched forward and out from the hips, the knees high, short thick shins
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        d = unit(0.55, -0.75, -0.05)
        k.add(f"thigh{side}", ell((0.078 * s, -0.045, 0.22), (0.062, 0.1, 0.066),
                                  rot=(0, 0, 24 * s)), GREY, f"leg{side}")
        S.limb(k, f"shin{side}", [(0.11, -0.125, 0.2), (0.12, -0.135, 0.11), (0.125, -0.14, 0.03)],
               [0.05, 0.04, 0.03], f"leg{side}", GREY, s)
        k.add(f"sole{side}", ell(np.array((0.127, -0.148, 0.016)) * m, (0.028, 0.028, 0.014)), GREY, f"leg{side}")
        S.fingers(k, f"toe{side}", (0.127, -0.155, 0.016), [(0.85, -0.5, 0), (0.4, -1, 0), (0.0, -1, 0),
                                                          (-0.4, -0.9, 0), (1, 0.15, 0)],
                  0.068, 0.0095, f"leg{side}", GREY, s)

    # The arms: from the shoulder out and back to the elbow, the forearm forward and down, long fingers spread before
    # the belly
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        S.limb(k, f"arm{side}", [(0.05, -0.01, 0.43), (0.088, 0.005, 0.38), (0.075, -0.07, 0.335)],
               [0.022, 0.018, 0.015], f"arm{side}", GREY, s)
        k.add(f"hand{side}", ell(np.array((0.07, -0.085, 0.33)) * m, (0.018, 0.017, 0.013)), GREY, f"arm{side}")
        S.fingers(k, f"finger{side}", (0.068, -0.094, 0.328), [(0.55, -0.7, -0.45), (0.15, -0.85, -0.5),
                                                              (-0.2, -0.8, -0.55), (-0.5, -0.6, -0.5)],
                  0.06, 0.0092, f"arm{side}", GREY, s)

    # The feelers: two pairs streaming back and out, from the shoulders and the small of the back
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"feelerA{side}", tube(np.array([(0.032, 0.035, 0.44), (0.07, 0.09, 0.45), (0.1, 0.15, 0.44),
                                                (0.12, 0.2, 0.415)]) * m, [0.02, 0.015, 0.012, 0.0105]), GREY, "body")
        k.add(f"feelerB{side}", tube(np.array([(0.036, 0.045, 0.33), (0.075, 0.1, 0.32), (0.11, 0.16, 0.3),
                                                (0.13, 0.21, 0.275)]) * m, [0.02, 0.015, 0.012, 0.0105]), GREY, "body")

    # The tail: back and down to the ground, sweeping round to the +X side
    k.add("tail", tube(TAIL, TAIL_R), GREY, "tail")

    # The violet belly: from under the chest down over the fronts and tops of the thighs
    def belly(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        band = inside(z - 0.37, 0.004) * inside(0.165 - z, 0.004)
        trunk = inside(np.abs(x) - 0.04, 0.003) * inside(y + 0.01, 0.003)
        thigh = (np.abs(x) > 0.035) * inside(y + 0.01, 0.004)
        return band * np.maximum(trunk, thigh)
    k.paint(belly, VIOLET, on=["chest", "waist", "hips", "thigh"])

    # The tail's end, pink all round
    k.paint(S.stripe_mask(TAIL, TAIL_R, 9.0, t0=0.6, all_round=True), PINK, on=["tail"])

    # The flames: two on the chest either side of the middle, one on each flank where grey meets violet
    for s in (1, -1):
        k.decal((0.032 * s, -0.07, 0.418), unit(0.45 * s, -1, 0.15), (0.022, 0.034), PINK, eps=0.0022, rings=14,
                segs=64, outline=flame())
        k.decal((0.056 * s, -0.012, 0.29), unit(1 * s, -0.35, 0.05), (0.018, 0.028), PINK, eps=0.0022, rings=14,
                segs=64, outline=flame())

    # The eyes: slanted almonds on the sides of the cranium under a heavy lid, a dark slit
    de = unit(0.8, -0.42, 0.42)
    E = S.on_ell(H, HR, de) + np.array([0, -0.004, 0.0])
    cut = S.lid_cut(0.2, 0.3, 0.05)
    lay = S.lay_eyes(k, E, de, (0.03, 0.026), cut, GREY, INK, slit=(0.13, 0.6), slit_at=(-0.1, -0.08))

    k.build(voxel=VOXEL, smooth=4, band=0.008, wband=0.04)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    salazzle(*args())
