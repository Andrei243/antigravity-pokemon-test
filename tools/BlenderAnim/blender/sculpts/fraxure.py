# Fraxure sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/fraxure.py -- <the export's Fraxure.glb> <out.glb>
# Axew grown up (built on axew.py's frame through sibling): from the references a grey dragon in a green jacket of
# armour. The green hood covers the top and back of the big head, with a point on the crown and a second at the
# nape, and runs down over the shoulders (round pauldrons) and the upper body to a point on the front of the belly.
# The face is grey: a long snout forward and down, the jaws open on a pink mouth, a black band from the snout back
# along each side holding a red eye. A long flat tusk out of each cheek, out and forward: grey, a black band, a red
# tip, the pair a V opening forward. Green arms, carried forward and down with the elbows bent (the art flings them wide), red three-clawed hands. The lower body,
# thick legs and thick tail grey; red spots on the lower belly, red claws on the feet, the tail's end red with a red
# fin on its top.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, polygon, eye_look, sibling, args

ax = sibling("axew")
spike, on_ell, eye_pair = ax.spike, ax.on_ell, ax.eye_pair

GREY, DARKGREY = "#7E7E86", "#5E5E66"
GREEN = "#5E9A45"
RED = "#D8473C"
BLACK = "#202024"
PINK, PINKDEEP = "#E89AA2", "#C46470"
INK = ax.INK


def tusk_part(root, d, up, t0, t1, w0, w1, flat):
    """A length of a flat tusk from t0 to t1 along d: a kite section w wide across up and flat * w thick along it."""
    root, d = np.array(root, float), unit(*d)
    up = np.array(up, float); up = up - d * (up @ d); up /= np.linalg.norm(up)
    side = np.cross(d, up)
    P = []
    for t, w in ((t0, w0), (t1, w1)):
        c = root + d * t
        if w < 1e-6:
            P.append(c)
            continue
        P += [c + side * w / 2, c - side * w / 2, c + up * w * flat, c - up * w * flat]
    return hull(P)


def fraxure(src, out):
    k = Kit(src, coat="scales")

    # The trunk: grey, low and broad at the hips; the green jacket over the upper body
    k.add("torso", ell((0, 0.0, 0.215), (0.098, 0.086, 0.1)), GREY, "body")
    k.add("chest", ell((0, -0.008, 0.29), (0.096, 0.08, 0.062)), GREEN, "body")
    def jacket(P):
        a = np.abs(np.arctan2(P[:, 0], -P[:, 1]))
        line = 0.248 - 0.085 * np.clip(1 - a / 0.55, 0, 1) - 0.012 * (a > 1.6)
        return inside(line - P[:, 2], 0.003)
    k.paint(jacket, GREEN, on=["torso", "chest"])
    k.paint(lambda P: inside(P[:, 2] - 0.25, 0.003) * (1 - jacket(P)), GREY, on=["torso"])
    # Red spots on the lower belly
    for j, (x, z, w, h, rot) in enumerate([(0.03, 0.195, 0.017, 0.012, 0.4), (-0.028, 0.168, 0.014, 0.01, -0.5),
                                           (0.01, 0.138, 0.019, 0.011, 0.2), (0.04, 0.14, 0.01, 0.009, 0.0)]):
        n = unit(x * 4, -1, 0.0)
        k.decal((x, -0.095, z), n, (w, h), RED, eps=0.003, rings=6, segs=24,
                outline=polygon([(-1, -0.6), (0.2, -1), (1, -0.2), (0.6, 1), (-0.5, 0.8)]))

    # The legs: thick thighs fused into the hips in a wide stance, short thick shins, broad feet, red claws
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.085, -0.005, 0.14)) * m, (0.066, 0.072, 0.08)), GREY, f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.1, -0.01, 0.085), (0.112, -0.012, 0.035)]) * m, [0.046, 0.043]), GREY,
              f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.113, -0.03, 0.021)) * m, (0.048, 0.062, 0.022)), GREY, f"leg{side}")
        for j, dx in enumerate((-0.026, 0.0, 0.026)):
            b = np.array((0.113 + dx, -0.082, 0.02))
            k.add(f"toe{side}{j}", spike(b * m, unit(dx * 6 * s, -1, -0.15), 0.032, 0.024, up=(0, 0, 1), flat=0.5),
                  RED, f"leg{side}", fuse=False, surface="shell")
        b = np.array((0.113, 0.022, 0.018))
        k.add(f"heel{side}", spike(b * m, unit(0, 1, -0.1), 0.026, 0.022, up=(0, 0, 1), flat=0.5), RED, f"leg{side}",
              fuse=False, surface="shell")

    # The arms: round green pauldrons, the arms forward and down, red hands of three claws
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"pauldron{side}", ell(np.array((0.095, -0.01, 0.3)) * m, (0.048, 0.05, 0.045)), GREEN, f"arm{side}")
        # elbows bent, the hands in front of the belly's sides, inside the body's width
        k.add(f"arm{side}", tube(np.array([(0.1, -0.02, 0.29), (0.128, -0.05, 0.24), (0.1, -0.105, 0.215)]) * m,
                                 [0.031, 0.027, 0.023]), GREEN, f"arm{side}")
        k.add(f"hand{side}", ell(np.array((0.094, -0.125, 0.208)) * m, (0.024, 0.024, 0.022)), RED, f"arm{side}")
        for j, (dx, dz) in enumerate(((-0.016, 0.004), (0.002, -0.004), (0.018, 0.004))):
            b = np.array((0.094 + dx, -0.142, 0.204 + dz))
            k.add(f"claw{side}{j}", spike(b * m, unit(dx * 8 * s, -1, -0.6), 0.03, 0.02, up=(0, 0, 1), flat=0.5),
                  RED, f"arm{side}", fuse=False, surface="shell")
    k.paint(lambda P: inside(-0.108 - P[:, 1], 0.002), GREEN, on=["arm"])
    k.paint(lambda P: inside(P[:, 1] + 0.108, 0.002), RED, on=["arm", "hand"])

    # The tail: thick, back and down, its end turning up, red; a red fin on its top near the end
    k.add("tail", tube([(0, 0.06, 0.18), (0, 0.15, 0.12), (0, 0.24, 0.11), (0, 0.31, 0.15), (0, 0.335, 0.19)],
                       [0.058, 0.044, 0.032, 0.02, 0.006]), GREY, "tail")
    k.paint(lambda P: inside(0.29 - P[:, 1], 0.003) * (P[:, 1] < 0.4), RED, on=["tail"])
    k.add("tailfin", hull([(0, 0.215, 0.13), (0.012, 0.25, 0.135), (-0.012, 0.25, 0.135), (0, 0.285, 0.14),
                           (0, 0.29, 0.205), (0.004, 0.27, 0.18), (-0.004, 0.27, 0.18)]), RED, "tail", fuse=False,
          surface="shell")

    # The head: a green hood over a grey face, the snout long, forward and down, the jaws open
    # Laid out at the size first measured, then the whole head grown about its joint by G (the face must read)
    J, G = np.array([0.0, -0.03, 0.34]), 1.15
    def hp(p):
        return J + (np.array(p, float) - J) * G
    first = len(k.parts)
    H, HR = np.array([0.0, -0.035, 0.4]), np.array([0.075, 0.075, 0.068])
    k.add("head", ell(H, HR), GREEN, "head")
    k.add("neck", ell((0, -0.02, 0.33), (0.065, 0.06, 0.05)), GREEN, "head")
    k.add("snout", ell((0, -0.14, 0.372), (0.05, 0.1, 0.034), rot=(-14, 0, 0)), GREY, "head")
    k.add("cheek", ell((0, -0.06, 0.365), (0.062, 0.05, 0.04)), GREY, "head")
    k.add("jaw", ell((0, -0.118, 0.312), (0.046, 0.085, 0.022), rot=(22, 0, 0)), GREY, "head")
    k.add("mouth", ell((0, -0.1, 0.338), (0.038, 0.072, 0.022), rot=(5, 0, 0)), PINK, "head")
    # the crown's point and the nape's, laid back
    k.add("crownpoint", tube([(0, -0.03, 0.44), (0, 0.0, 0.48), (0, 0.025, 0.505)], [0.035, 0.018, 0.004]), GREEN,
          "head")
    k.add("napepoint", tube([(0, 0.035, 0.39), (0, 0.075, 0.42), (0, 0.098, 0.445)], [0.033, 0.017, 0.004]), GREEN,
          "head")
    for p in k.parts[-2:]:
        p["V"][:, 0] = p["V"][:, 0] * 0.6
    for p in k.parts[first:]:
        p["V"] = J + (p["V"] - J) * G
    # the hood's front edge over the eyes, the grey face under it
    def face(P):
        q = (J + (P - J) / G) - H
        return inside(q[:, 2] - (-0.002 + 0.5 * np.minimum(q[:, 1] + 0.04, 0)), 0.003) * inside(q[:, 1] + 0.0, 0.003)
    k.paint(face, GREY, on=["head", "cheek", "snout"])
    k.paint(lambda P: inside(np.linalg.norm((P - hp((0, -0.11, 0.33))) / (0.041, 0.092, 0.029), axis=1) - 1, 0.003),
            PINKDEEP, on=["mouth"])

    # The black band: from the snout's top back along each side through the eye to the tusk
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        n = unit(0.75 * s, -0.6, 0.25)
        k.decal(hp(np.array((0.05, -0.085, 0.392)) * m), n, (0.055, 0.018), BLACK, eps=0.003, rings=8, segs=48,
                up=(0, 0.2, 1), outline=polygon([(-1, -0.3), (-0.4, -1), (1, -0.6), (1, 0.7), (-0.6, 1)] if s > 0 else
                                               [(1, -0.3), (0.4, -1), (-1, -0.6), (-1, 0.7), (0.6, 1)]))

    # The tusks: out of each cheek, forward in a V and a little down, flat, rolled to show their faces; grey, a black
    # band, a red tip
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        R = hp(np.array((0.058, -0.06, 0.375)) * m)
        d = unit(0.85 * s, -1.0, -0.14)
        up = (0.75 * s, 0, 1)
        k.add(f"tusk{side}", tusk_part(R, d, up, -0.01, 0.115, 0.056, 0.042, 0.26), GREY, "head", fuse=False,
              surface="metal")
        k.add(f"tuskband{side}", tusk_part(R, d, up, 0.113, 0.136, 0.043, 0.039, 0.27), BLACK, "head", fuse=False,
              surface="metal")
        k.add(f"tusktip{side}", tusk_part(R, d, up, 0.134, 0.185, 0.04, 0.0, 0.27), RED, "head", fuse=False,
              surface="metal")

    # The eyes: red, a black pupil, a glint, almond-shaped, in the black band
    RED_E = srgb("#E23A30")
    def eye(u, v):
        c = np.tile(RED_E, (len(u), 1))
        c[((u + 0.05) / 0.3) ** 2 + (v / 0.75) ** 2 < 1] = INK
        c[(u - 0.3) ** 2 + (v - 0.35) ** 2 < 0.03] = np.array([1.0, 1.0, 1.0])
        return c
    almond = polygon([(-1, -0.2), (-0.55, -0.85), (0.4, -0.95), (1, -0.3), (0.85, 0.5), (0.1, 0.95), (-0.7, 0.55)])
    de = unit(0.7, -0.66, 0.22)
    E = hp(np.array((0.052, -0.088, 0.395)))
    eyes = eye_pair(k, E, de, (0.024, 0.019), eye, srgb(BLACK), outline=almond, eps=0.0055, ink=srgb("#9A9AA2"))

    k.build(voxel=0.005, smooth=4, band=0.005)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    fraxure(*args())
