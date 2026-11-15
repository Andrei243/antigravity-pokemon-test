# Charmeleon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/charmeleon.py -- <the export's Charmeleon.glb> <out.glb>
# Charmander grown taller and red: the head smaller against the body, its snout long and its mouth shut on a dark line,
# a horn of the skull swept up and back off the back of the head, narrow fierce eyes cut by a brow that slopes down to
# the snout (white, a dark blue iris, a black pupil). The cream oval down the belly and now along the tail's underside,
# longer arms hanging forward with three white claws, heavy thighs over short shins, three white claws a foot, the
# tail curling up on the +X side to a larger flame. Built by Charmander's own functions (claws, flame, eyes).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, polygon, eye_look, args, sibling

cm = sibling("charmander")
RED, CREAM = "#E24F2E", cm.CREAM
INK, WHITE = cm.INK, cm.WHITE
BLUE = srgb("#2E4FA6")


def tail_underside(pts, below=0.012, start=0.1):
    """The cream under a tail: points under its centre line (z by y along it), behind the hips."""
    pts = np.array(pts, float)
    def mask(P):
        zc = np.interp(P[:, 1], pts[:, 1], pts[:, 2])
        return inside(P[:, 2] - zc + below, 0.005) * smoothstep(start, start + 0.03, P[:, 1])
    return mask


def brow_cut(BA, BB):
    """An eye's outline cut straight along the brow v = BA + BB u (u outward): its reach at each angle."""
    def cut(th):
        s = math.sin(th) - BB * math.cos(th)
        return min(1.0, BA / s) if s > 1e-6 else 1.0
    return cut


def fierce_eye(iris, cut, rim=0.76, iris_c=(-0.22, -0.12), iris_r=0.5, pupil_r=0.26):
    """An almond eye under a brow: a black rim along the cut outline, white, the iris and pupil toward the nose."""
    def eye(u, v):
        reach = np.array([cut(t) for t in np.arctan2(v, u)])
        rho = np.sqrt(u * u + v * v) / reach
        c = np.tile(INK, (len(u), 1))
        c[rho < rim] = WHITE
        d = np.sqrt((u - iris_c[0]) ** 2 + (v - iris_c[1]) ** 2)
        c[(d < iris_r) & (rho < rim)] = iris
        c[(d < pupil_r) & (rho < rim)] = INK
        return c
    return eye


def eyes_under_brow(k, E, de, half, look_open, cut, lid):
    """Lays the two eyes for a state: the right as it is, the left with its picture and outline turned over."""
    m = np.array([-1, 1, 1.0])
    def eyes(state):
        look = eye_look(state, look_open, lid, INK, line=0.17)
        k.decal(E, de, half, look, eps=0.006, rings=20, segs=56, outline=cut)
        k.decal(E * m, de * m, half, lambda u, v: look(-u, v), eps=0.006, rings=20, segs=56,
                outline=lambda th: cut(math.pi - th))
    return eyes


def charmeleon(src, out):
    k = Kit(src, coat="scales")

    # The head: a skull carried forward over a long snout, the jaw under it, the horn swept up and back
    H, HR = np.array([0.0, -0.02, 0.725]), np.array([0.105, 0.105, 0.1])
    k.add("head", ell(H, HR), RED, "head")
    k.add("snout", ell((0, -0.115, 0.69), (0.068, 0.11, 0.058), rot=(-6, 0, 0)), RED, "head")
    k.add("jaw", ell((0, -0.07, 0.645), (0.08, 0.1, 0.05), rot=(10, 0, 0)), RED, "head")
    k.add("brow", ell((0, -0.07, 0.755), (0.085, 0.07, 0.04), rot=(12, 0, 0)), RED, "head")
    k.add("horn", blade([(0, 0.0, 0.77), (0, 0.075, 0.82), (0, 0.14, 0.855), (0, 0.19, 0.872)],
                        [0.085, 0.058, 0.032, 0.012], (1, 0, 0), 0.62), RED, "head")
    k.add("neck", tube([(0, 0.0, 0.53), (0, -0.015, 0.6), (0, -0.03, 0.66)], [0.07, 0.066, 0.064]), RED, "head")

    # The body: taller than Charmander's, stocky, leaning a little forward over wide hips
    k.add("torso", ell((0, -0.01, 0.43), (0.11, 0.1, 0.165), rot=(10, 0, 0)), RED, "body")
    k.add("hips", ell((0, 0.01, 0.3), (0.15, 0.12, 0.12)), RED, "body")
    k.decal((0, -0.1, 0.4), (0, -1, 0), (0.078, 0.15), CREAM, eps=0.003, rings=16, segs=56)

    # The arms: the upper arm down along the side, the forearm forward, big hands of three white claws held in front
    # of the belly's edge at the hips' height
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"arm{side}", tube(np.array([(0.09, -0.02, 0.5), (0.165, -0.04, 0.43), (0.155, -0.125, 0.375)]) * m,
                                 [0.05, 0.042, 0.038]), RED, f"arm{side}")
        HC = np.array((0.15, -0.16, 0.36))
        k.add(f"hand{side}", ell(HC * m, (0.042, 0.04, 0.036)), RED, f"arm{side}")
        for j, (dx, dz) in enumerate([(0.026, 0.004), (0.0, -0.006), (-0.026, 0.004)]):
            b = HC + np.array((dx, -0.03, -0.012 + dz))
            k.add(f"claw{side}{j}", cm.claw(b * m, np.array((dx * 1.5, -0.8, -1)) * m, 0.055, 0.028, 0.022,
                                            up=(0, 1, 0)), cm.CLAW, f"arm{side}", fuse=False, surface="shell")

    # The legs: heavy thighs as wide as the head fused into the hips, the knee bent forward, the shin back down to a
    # foot with three white claws
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.112, -0.03, 0.25)) * m, (0.1, 0.12, 0.12), rot=(-25, 0, 0)), RED,
              f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.115, -0.075, 0.17), (0.115, -0.03, 0.11), (0.116, 0.0, 0.06)]) * m,
                                  [0.064, 0.058, 0.052]), RED, f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.116, -0.045, 0.031)) * m, (0.062, 0.095, 0.033)), RED, f"leg{side}")
        for j, dx in enumerate((-0.03, 0.0, 0.03)):
            b = np.array((0.116 + dx, -0.125, 0.021))
            k.add(f"toe{side}{j}", cm.claw(b * m, np.array((dx * 0.6, -1, -0.35)) * m, 0.036, 0.024, 0.019), cm.CLAW,
                  f"leg{side}", fuse=False, surface="shell")

    # The tail: thick at the hips, back low and curling up on the +X side to its flame, cream beneath
    TAIL = [(0, 0.08, 0.3), (0.01, 0.21, 0.165), (0.06, 0.34, 0.105), (0.14, 0.45, 0.13), (0.2, 0.5, 0.215)]
    k.add("tail", tube(TAIL, [0.1, 0.075, 0.052, 0.038, 0.03]), RED, "tail")
    k.paint(tail_underside(TAIL, start=0.16), CREAM)
    cm.flame(k, (0.2, 0.5, 0.225), 1.15)

    # The mouth shut: a dark line along each side of the snout, curving up a little at its back
    for s in (1, -1):
        k.decal((0.05 * s, -0.14, 0.668), unit(0.85 * s, -0.4, -0.2), (0.05, 0.005), "#5A1A16", up=(0, 0, 1),
                eps=0.004, rings=4, segs=32)
    # The nostrils: two small dark dots on top of the snout's tip
    k.decal((0.02, -0.21, 0.725), unit(0.2, -0.6, 0.8), (0.008, 0.006), cm.NOSTRIL, eps=0.004, rings=4, segs=16,
            mirror=True)

    # The eyes: narrow, fierce, the brow cutting them straight and sloping down toward the snout
    cut = brow_cut(0.32, 0.42)
    de = unit(0.66, -0.7, 0.24)
    E = cm.on_ell(H, HR, de)
    eyes = eyes_under_brow(k, E, de, (0.03, 0.036), fierce_eye(BLUE, cut), cut, srgb(RED))

    k.build(voxel=0.007, smooth=4)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    charmeleon(*args())
