# Chi-Yu sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/chiyu.py -- <the export's Chi-Yu.glb> <out.glb>
#
# From the references: a little goldfish of ruin hanging in the air, a round body of orange shading pale yellow
# underneath, a red stripe over its crown ending in a red diamond on the brow, red crescents on its sides; two big
# glossy black eyes bulging at the front, each sitting in a thick ring of green jade (a comma-shaped bead, a knob on
# its outer side); fins of flame, dark red with orange hearts and wavy edges: a fan of four lobes streaming back from
# the tail (two long ones rising, two short ones hanging), a dorsal flame curling back, a little flame at each side
# under the eyes and one under the belly.
# The skeleton: body (0, 0, 0.115), wingR/L (+-0.07, -0.01, 0.095) for the side fins, tail (0, 0.09, 0.115).
# A fish seen from the front three-quarter and from behind: every flame is turned so that its face shows to both.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, slab, lathe, outward, srgb, inside, unit, polygon, eye_look, args

ORANGE, PALE, RED, FLAME, HEART = "#F49A46", "#FCCB7C", "#C8321E", "#A82418", "#F06A26"
JADE, JADE2, BLACK, GLINT = "#3C8A6E", "#5FAA8C", "#141418", "#F4F4F4"

BC = np.array([0.0, 0.0, 0.118])
BR = np.array([0.08, 0.084, 0.078])


def flame_outline(L, w, bend=0.35, waves=2.5):
    """A tongue of flame along b from its root (b 0) to its tip (b L), swaying by bend, its edges wavy."""
    t = np.linspace(0, 1, 22)
    c = bend * L * t ** 2
    half = w * (1 - t) ** 0.75 * (1 + 0.28 * np.sin(waves * math.pi * t)) + 0.0015
    right = [(c[i] + half[i], t[i] * L) for i in range(len(t))]
    left = [(c[i] - half[i] * (1 - 0.35 * math.sin(waves * math.pi * t[i] + 1)), t[i] * L) for i in range(len(t) - 1)]
    return right + left[::-1]


def flame(k, name, root, d, L, w, bone, bend=0.35, thick=0.0075):
    """A flat flame (dark red with an orange heart showing on both faces), its face turned to front and back."""
    d = unit(*d)
    ex = np.cross((0, 1.0, 0), d)
    if np.linalg.norm(ex) < 1e-3:
        ex = np.array([1.0, 0, 0])
    ex /= np.linalg.norm(ex)
    root = np.array(root, float)
    k.add(name, slab(flame_outline(L, w, bend), root, ex, d, thick), FLAME, bone, fuse=False, surface="scales",
          glow=FLAME)
    k.add(name + "heart", slab(flame_outline(L * 0.6, w * 0.55, bend * 0.8, 2.0), root + d * L * 0.04, ex, d,
                               thick * 1.6), HEART, bone, fuse=False, surface="scales", glow=HEART)


def body(k):
    k.add("body", ell(BC, BR), ORANGE, "body")
    k.add("tailstalk", ell(BC + (0, 0.06, 0.004), (0.045, 0.04, 0.042)), ORANGE, "tail")
    for s in (1, -1):
        k.add("bulge" + ("R" if s > 0 else "L"), ell(BC + (0.048 * s, -0.058, 0.004), (0.032, 0.03, 0.032)),
              ORANGE, "body")
    # pale underneath
    k.paint(lambda P: inside(P[:, 2] - (BC[2] - 0.025), 0.02), PALE)
    # the red stripe over the crown
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.007, 0.002) * inside(BC[2] + 0.045 - P[:, 2], 0.003)
            * inside(P[:, 1] - 0.045, 0.004) * inside(-0.03 - P[:, 1], 0.004), RED)
    # red crescents on its sides
    for s in (1, -1):
        def cres(P, s=s):
            yz = np.stack([P[:, 1], P[:, 2]], 1)
            d1 = np.linalg.norm(yz - (0.022, BC[2] - 0.005), axis=1)
            d2 = np.linalg.norm(yz - (0.034, BC[2] + 0.005), axis=1)
            return inside(d1 - 0.03, 0.002) * inside(0.026 - d2, 0.002) * inside(0.04 - P[:, 0] * s, 0.004)
        k.paint(cres, RED)
    # the diamond on the brow
    n = unit(0, -0.55, 0.83)
    c = BC + np.array([0, -0.043, 0.067])
    k.decal(c, n, (0.016, 0.022), RED, eps=0.002, outline=polygon([(0, 1), (1, 0), (0, -1), (-1, 0)]), rings=6, segs=40)
    k.decal(c, n, (0.007, 0.01), ORANGE, eps=0.0035, outline=polygon([(0, 1), (1, 0), (0, -1), (-1, 0)]), rings=4,
            segs=40)


def rings(k):
    """A ring of jade round each eye, comma-shaped: a knob on its outer lower side."""
    for s in (1, -1):
        n = unit(0.55 * s, -1, 0.05)
        c = BC + np.array([0.048 * s, -0.058, 0.004]) + n * 0.024
        R, r = 0.035, 0.0115
        prof = [(R + r * math.cos(t), r * math.sin(t) * 1.25) for t in np.linspace(0, 2 * math.pi, 25)]
        k.add("ring" + ("R" if s > 0 else "L"), lathe(c, n, prof, seg=44), JADE, "body", fuse=False, surface="shell")
        side = np.cross(n, (0, 0, 1.0)); side /= np.linalg.norm(side)
        if side[0] * s < 0:
            side = -side
        knob = c + side * R * 0.75 + np.array([0, 0, -R * 0.75])
        k.add("knob" + ("R" if s > 0 else "L"), outward(ell(knob, (0.014, 0.012, 0.014))), JADE2, "body", fuse=False,
              surface="shell")


def fins(k):
    T = BC + np.array([0, 0.085, 0.004])
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        flame(k, "upper" + sd, T, (0.36 * s, 0.75, 0.58), 0.2, 0.04, "tail", bend=0.3 * s)
        flame(k, "lower" + sd, T + (0, 0, -0.01), (0.42 * s, 0.8, -0.42), 0.12, 0.032, "tail", bend=-0.3 * s)
        flame(k, "side" + sd, BC + (0.07 * s, -0.012, -0.03), (0.6 * s, 0.3, -0.72), 0.07, 0.02,
              "wing" + sd, bend=0.3 * s)
    flame(k, "dorsal", BC + (0, 0.02, 0.07), (0.3, 0.6, 0.75), 0.125, 0.036, "body", bend=0.25)
    flame(k, "belly", BC + (0, 0.035, -0.07), (0.0, 0.55, -0.85), 0.06, 0.02, "body", bend=0.3)


CB, CG, CO = srgb(BLACK), srgb(GLINT), srgb(ORANGE)


def eye_open(u, v):
    c = np.tile(CB, (len(u), 1))
    c[np.hypot(u + 0.3, v - 0.35) < 0.22] = CG
    c[np.hypot(u - 0.25, v + 0.35) < 0.09] = CG
    return c


def eyes(k, state):
    look = eye_look(state, eye_open, CO, CB, line=0.15)
    for s in (1, -1):
        n = unit(0.55 * s, -1, 0.05)
        c = BC + np.array([0.048 * s, -0.058, 0.004]) + n * 0.03
        f = look if s > 0 else (lambda u, v, look=look: look(-u, v))
        k.decal(c, n, (0.03, 0.03), f, eps=0.0018, rings=20, segs=56)


def chiyu(src, out):
    k = Kit(src, coat="scales")
    body(k)
    rings(k)
    fins(k)
    k.build(voxel=0.0028, smooth=4, band=0.005, wband=0.03)
    k.eye_states(lambda st: eyes(k, st))
    k.export(out)


if __name__ == "__main__":
    chiyu(*args())
