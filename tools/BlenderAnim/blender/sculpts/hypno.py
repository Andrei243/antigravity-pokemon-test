# Hypno sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Drowzee's script (drowzee.py, imported as sibling("drowzee")): the same yellow, the same tired half-shut eyes, the
# same big hands and the same way of laying a patch and painting a crisp line.
#   blender --background --factory-startup --python sculpts/hypno.py -- <the export's Hypno.glb> <out.glb>
#
# The design, from the references: taller and leaner than Drowzee and yellow all over (no brown); an egg of a head
# with a long pointed nose drooping from the middle of the face, tired half-shut eyes either side of its root, a small
# mouth under it; tall pointed ears, brown inside; a thick lumpy white ruff round the neck; a slim chest and waist over
# thick bent legs set wide, three toes a foot; arms bent at the elbow and fists, the +X fist raised with a pendulum
# hanging from it on a dark string, a silver ring with a hole in its middle turned to the front.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, lathe, srgb, inside, unit, lumpy, args, sibling

d = sibling("drowzee")
YELLOW, EAR, RUFF = d.YELLOW, "#9A6A44", "#F5F3EC"
STRING, SILVER = "#3A3434", "#C9CED6"


def hypno(src, out):
    k = Kit(src, coat="fur")
    H = np.array([0, -0.03, 0.8])            # the head's middle
    HR = np.array([0.132, 0.125, 0.152])

    # A short thick chest and waist, broad hips over the legs
    k.add("waist", ell((0, 0.0, 0.5), (0.145, 0.115, 0.14)), YELLOW, "body")
    k.add("chest", ell((0, -0.005, 0.6), (0.175, 0.13, 0.09)), YELLOW, "body")
    k.add("hips", ell((0, 0.005, 0.4), (0.165, 0.125, 0.095)), YELLOW, "body")

    # The ruff: a thick lumpy collar of white fur round the neck
    k.add("ruff", ell((0, -0.012, 0.665), (0.205, 0.175, 0.088), deform=lumpy(11, 2, 0.15)), RUFF, "body")

    # The head: an egg, narrower at the crown; the long pointed nose drooping from the face's middle
    def egg(V):
        V = V.copy()
        s = 1 - 0.12 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    HEAD = ell(H, HR, deform=egg)
    k.add("head", HEAD, YELLOW, "head")
    k.add("nose", tube([(0, -0.09, 0.795), (0, -0.175, 0.77), (0, -0.237, 0.726), (0, -0.268, 0.682)],
                       [0.062, 0.045, 0.025, 0.008]), YELLOW, "head")

    # Tall pointed ears, a little out, brown inside
    EB, ET = np.array([0.075, 0.005, 0.9]), np.array([0.165, 0.018, 1.06])
    k.add("ear", blade([EB, EB + (ET - EB) * 0.55, ET], [0.066, 0.043, 0.005], (0, 1, 0), 0.4), YELLOW, "ear{s}",
          mirror=True)
    ED = unit(*(ET - EB)); EL = np.linalg.norm(ET - EB)
    def lining(P):
        Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
        r = Q - EB
        t = (r @ ED) / EL
        w = np.linalg.norm(r - np.outer(r @ ED, ED) - np.outer(r[:, 1], (0, 1, 0)), axis=1)
        half = 0.066 + (0.005 - 0.066) * np.clip(t, 0, 1)
        return (inside(w - 0.62 * half, 0.003) * inside(0.25 - t, 0.004) * inside(t - 0.92, 0.004)
                * inside(P[:, 1] - 0.0, 0.002))
    # (t above 0.25 and below 0.92: inside(0.25 - t) is 1 where t > 0.25)
    k.paint(lining, EAR)

    # Thick arms bent at the elbow, big fists; the +X fist raised with the pendulum, the -X carried forward and down
    FR, FL = np.array([0.29, -0.14, 0.73]), np.array([-0.28, -0.175, 0.5])
    k.add("armR", tube([(0.14, -0.005, 0.625), (0.265, -0.05, 0.6), (0.285, -0.115, 0.69)], [0.068, 0.058, 0.05]),
          YELLOW, "armR")
    k.add("fistR", ell(FR, (0.068, 0.062, 0.066)), YELLOW, "armR")
    k.add("armL", tube([(-0.14, -0.005, 0.625), (-0.255, -0.065, 0.575), (-0.275, -0.145, 0.515)],
                       [0.068, 0.058, 0.05]), YELLOW, "armL")
    k.add("fistL", ell(FL, (0.068, 0.062, 0.066)), YELLOW, "armL")
    for s, F, bone in ((1, FR, "armR"), (-1, FL, "armL")):
        for i in range(3):                     # the curled fingers' knuckles over the fist's front
            k.add(f"knuckle{i}{bone[-1]}", ell(F + np.array([0, -0.05, 0.04 - i * 0.034]), (0.053, 0.028, 0.017)),
                  YELLOW, bone)

    # The pendulum: a thick dark string from the fist's underside, a silver ring with its hole to the front, held out
    top = FR + np.array([0.012, -0.05, -0.05])
    RC = np.array([0.315, -0.215, 0.53])
    k.add("string", tube([top, (top + RC) / 2 + np.array([0, -0.004, 0]), RC + np.array([0, 0, 0.038])],
                         [0.0075, 0.007, 0.007], seg=10), STRING, "armR", fuse=False)
    k.add("ring", lathe(RC, (0, -1, 0), [(0.014, -0.009), (0.039, -0.009), (0.042, 0.0), (0.039, 0.009),
                                         (0.014, 0.009), (0.014, -0.009)], seg=48),
          SILVER, "armR", fuse=False, surface="metal")

    # Heavy bent legs, the knees out so the stance is wide and low; big feet with three toes
    for s, bone in ((1, "legR"), (-1, "legL")):
        m = np.array([s, 1, 1.0])
        k.add("thigh" + bone[-1], tube([np.array(p) * m for p in [(0.08, 0.0, 0.4), (0.2, -0.06, 0.225)]],
                                       [0.135, 0.1]), YELLOW, bone)
        k.add("shin" + bone[-1], tube([np.array(p) * m for p in [(0.2, -0.06, 0.225), (0.18, -0.025, 0.07)]],
                                      [0.088, 0.066]), YELLOW, bone)
        d.foot(k, np.array([0.185, -0.07, 0.038]) * m, (0.078, 0.112, 0.042), bone, YELLOW, toes=3,
               name="foot" + bone[-1])

    # No mouth: the nose hides it in the 3D reference, and a patch laid behind the nose read as a streak from the
    # game's front three-quarter

    # Drowzee's tired eyes, either side of the nose's root
    de = unit(0.55, -0.8, 0.22)
    E = d.hit(HEAD, H + np.array([0.063, 0, 0.038]) + de * 0.4, -de)
    lay = d.tired_eyes(k, E, de, (0.038, 0.024), srgb(YELLOW))

    k.build(voxel=0.0072, band=0.004)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    hypno(*args())
