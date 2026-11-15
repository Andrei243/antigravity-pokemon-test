# Blaziken sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/blaziken.py -- <the export's Blaziken.glb> <out.glb>
# Combusken grown into a tall red fighter (built by torchic.py's functions: its spikes, claws, ruffs and eyes): a red
# head with a short pointed beak, open a little (pale within), two red horns rising from the brow in a V, sharp eyes
# (a yellow white, a blue iris, a black pupil in a black rim, the top cut straight and sloping to the beak); a long
# cream mane from the crown falling down the back to the hips; a ruff of cream points over the shoulders and chest, a
# yellow point down the breast under it; red arms bent forward with three grey bands at each forearm and grey hands of
# three thick fingers; a red waist with red plumes behind; long thick red legs fused into the hips, from the shin down
# yellow boots of fur jagged at the top and flared at the ground, white claws at their fronts.
# The pictures show no flames at the wrists (only grey bands): they are left out.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, ring_band, srgb, inside, unit, args, sibling

T = sibling("torchic")
RED, CREAM, YELLOW, GREY, CLAW, INK = "#DA3A2C", "#F3E6BE", "#F8C83A", "#8F8A92", "#F4F1EC", T.INK
SCLERA, IRIS = srgb("#F6D43A"), srgb("#3A6CD2")
VOXEL = 0.0075

H = np.array([0.0, -0.045, 0.85])
HR = np.array([0.084, 0.088, 0.088])
DE = unit(0.6, -0.77, 0.2)
EYE_HALF = (0.034, 0.03)
EYE_SHAPE = ([(u, 0.45 + 0.3 * u) for u in np.linspace(1.0, -0.95, 9)]
             + [(u, -0.7 * math.sqrt(1 - u * u) + 0.25 * u) for u in np.linspace(-0.9, 0.95, 13)])


def blaziken(src, out):
    k = Kit(src, coat="fur")

    # The trunk: red hips and waist, broad shoulders under a ruff of cream points, a cream neck
    k.add("pelvis", ell((0, 0.01, 0.5), (0.115, 0.09, 0.088)), RED, "body")
    k.add("torso", ell((0, 0.0, 0.625), (0.098, 0.078, 0.12)), RED, "body")
    k.add("shoulders", ell((0, 0.005, 0.715), (0.122, 0.085, 0.062)), RED, "body")
    k.add("ruff", T.ruff((0, -0.006, 0.73), (0.125, 0.094, 0.05), 10, 1.3, 0.2), CREAM, "body")
    k.add("neck", ell((0, -0.018, 0.775), (0.056, 0.055, 0.06)), CREAM, "head")
    # the yellow point down the breast
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.05 * np.clip((P[:, 2] - 0.55) / 0.13, 0, 1), 0.003)
            * inside(P[:, 1] + 0.03) * inside(0.55 - P[:, 2], 0.003) * inside(P[:, 2] - 0.7), YELLOW)

    # The head: red, a short pointed beak open a little, two horns in a V
    k.add("head", ell(H, HR), RED, "head")
    k.add("beak", ell((0, -0.135, 0.828), (0.034, 0.046, 0.02), rot=(12, 0, 0), deform=T.pointed), RED, "head",
          fuse=False)
    k.add("jaw", ell((0, -0.118, 0.8), (0.022, 0.032, 0.012), rot=(18, 0, 0), deform=T.pointed), "#E2B4A6", "head",
          fuse=False, surface="skin")
    k.add("horn", T.spike((0.03, -0.078, 0.905), (0.32, 0.1, 0.94), 0.15, 0.021, 0.018), RED, "head", fuse=False,
          mirror=True)

    # The mane: from the crown down the back to the hips, a little apart from the back
    k.add("mane", tube([(0, -0.01, 0.925), (0, 0.08, 0.905), (0, 0.15, 0.8), (0, 0.19, 0.7)],
                       [0.068, 0.085, 0.09, 0.085]), CREAM, "head")
    k.add("manelow", tube([(0, 0.19, 0.72), (0, 0.205, 0.6), (0, 0.195, 0.48)], [0.084, 0.072, 0.035]), CREAM, "body")
    k.add("lock", tube([(0.045, 0.0, 0.9), (0.085, 0.09, 0.845), (0.11, 0.15, 0.7), (0.11, 0.165, 0.54)],
                       [0.048, 0.058, 0.052, 0.026]), CREAM, "head", mirror=True)

    # Arms: red, bent forward, three grey bands at the forearm, grey hands of three thick fingers
    sh, el, wr = np.array([0.09, -0.01, 0.72]), np.array([0.168, 0.015, 0.6]), np.array([0.2, -0.115, 0.56])
    k.add("arm", tube([sh, el, wr], [0.046, 0.041, 0.037]), RED, "arm{s}", mirror=True)
    along = (wr - el) / np.linalg.norm(wr - el)
    for i, t in enumerate((0.5, 0.68, 0.86)):
        r = 0.041 + (0.037 - 0.041) * t
        k.add(f"band{i}", ring_band(el + (wr - el) * t, along, r + 0.003, 0.022, thick=0.008), GREY, "arm{s}",
              fuse=False, mirror=True)
    palm = wr + along * 0.035
    k.add("hand", ell(palm, (0.036, 0.04, 0.034)), GREY, "arm{s}", mirror=True)
    for i, dx in enumerate((-0.022, 0.0, 0.022)):
        d = along + np.array([dx * 6, 0, -0.35])
        d /= np.linalg.norm(d)
        b = palm + along * 0.02 + np.array([dx, 0, -0.01])
        k.add(f"finger{i}", tube([b, b + d * 0.035, b + d * 0.06 + np.array([0, 0, -0.012])], [0.018, 0.017, 0.015]),
              GREY, "arm{s}", mirror=True)

    # Red plumes behind the waist
    k.add("tail", T.spike((0, 0.06, 0.5), (0, 0.85, -0.42), 0.13, 0.036, 0.022), RED, "body", fuse=False)
    k.add("tailside", T.spike((0.04, 0.05, 0.5), (0.55, 0.75, -0.35), 0.09, 0.028, 0.02), RED, "body", fuse=False,
          mirror=True)

    # Legs: thick red thighs fused into the hips, red shins, yellow boots of fur from the shin down
    k.add("thigh", tube([(0.08, 0.01, 0.5), (0.1, -0.03, 0.38), (0.11, -0.04, 0.3)], [0.076, 0.066, 0.056]), RED,
          "leg{s}", mirror=True)
    k.add("shin", tube([(0.11, -0.04, 0.3), (0.116, -0.01, 0.2)], [0.054, 0.052]), RED, "leg{s}", mirror=True)
    k.add("boot", tube([(0.116, -0.01, 0.235), (0.118, -0.008, 0.14), (0.118, -0.012, 0.085)], [0.056, 0.07, 0.082]),
          YELLOW, "leg{s}", mirror=True)
    k.add("bootcuff", T.ruff((0.116, -0.008, 0.245), (0.068, 0.068, 0.03), 7, 2.4, 0.12, up=True), YELLOW, "leg{s}",
          mirror=True)
    k.add("boothem", T.ruff((0.118, -0.012, 0.064), (0.082, 0.088, 0.032), 8, 0.7, 0.4), YELLOW, "leg{s}", mirror=True)
    for i, dx in enumerate((-0.03, 0.0, 0.03)):
        k.add(f"claw{i}", T.claw((0.118 + dx, -0.075, 0.03), (dx * 5, -1, -0.15), 0.045, 0.015), CLAW, "leg{s}",
              fuse=False, mirror=True, surface="shell")

    k.build(voxel=VOXEL, band=0.007)

    E = T.on_ell(H, HR, DE)
    look = T.eye_open(SCLERA, iris=IRIS, iris_c=(-0.18, -0.08), iris_r=0.52, pupil=INK, pupil_c=(-0.18, -0.08),
                      pupil_r=(0.24, 0.3), glint=(-0.32, 0.12, 0.13))
    k.eye_states(lambda state: T.lay_eyes(k, state, E, DE, EYE_HALF, look, srgb(RED), INK, outline=EYE_SHAPE,
                                          rim=INK, inset=0.78))
    k.export(out)


if __name__ == "__main__":
    blaziken(*args())
