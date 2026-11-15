# Combusken sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/combusken.py -- <the export's Combusken.glb> <out.glb>
# Torchic grown into a fighting chicken (built by torchic.py's functions: its plumes, points of down, claws, feet,
# ruffs and eyes): a tall yellow head on a short thick neck, a small orange beak, angry eyes (an orange iris and a
# black pupil in a black rim, the top cut straight and sloping down to the beak); a crest of three orange plumes laid
# up and back; a yellow chest whose points of down hang over a round orange lower body, a tongue of orange running up
# its middle; great round orange thighs with a grey knee at each side, short grey shins, grey feet of three toes
# forward and one back with white claws; long thick yellow arms held forward and a little down, a tuft of down at
# each wrist and three white claws from its end; two orange tail plumes behind.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, tri, unit, args, sibling

T = sibling("torchic")
ORANGE, YELLOW, INK = T.ORANGE, "#F9DC6E", T.INK
GREY, CLAW, EYE = "#9A949C", "#F4F1EC", srgb("#F2862E")
VOXEL = 0.006

H = np.array([0.0, -0.03, 0.648])            # the head's centre
HR = np.array([0.1, 0.103, 0.116])
DE = unit(0.62, -0.76, 0.14)
EYE_HALF = (0.038, 0.038)
# The eye's shape, u outward: its top a straight brow sloping down to the beak, its bottom round
EYE_SHAPE = ([(u, 0.36 + 0.42 * u) for u in np.linspace(0.95, -0.95, 9)]
             + [(u, -0.88 * math.sqrt(1 - u * u)) for u in np.linspace(-0.9, 0.9, 13)])


def combusken(src, out):
    k = Kit(src, coat="fur")

    # The body: an orange lower body over great round thighs, a yellow chest, a short thick neck
    k.add("hips", ell((0, 0.012, 0.33), (0.112, 0.096, 0.095)), ORANGE, "body")
    k.add("thigh", ell((0.092, 0.0, 0.2), (0.096, 0.1, 0.128)), ORANGE, "leg{s}", mirror=True)
    k.add("chest", ell((0, 0.0, 0.45), (0.106, 0.088, 0.105)), YELLOW, "body")
    k.add("neck", ell((0, -0.014, 0.548), (0.088, 0.082, 0.08)), YELLOW, "head")
    k.add("head", ell(H, HR), YELLOW, "head")

    # The yellow chest's points of down over the orange, an orange tongue up its middle
    def edge(P):
        ph = np.arctan2(P[:, 0], -P[:, 1])
        return 0.385 + 0.03 * tri(ph * 8 / (2 * math.pi)) + 0.07 * np.exp(-(ph / 0.28) ** 2)
    torso = lambda P: inside(np.abs(P[:, 0]) - 0.13, 0.004) * inside(P[:, 2] - 0.52, 0.004)
    k.paint(lambda P: inside(P[:, 2] - edge(P), 0.0025) * inside(0.08 - P[:, 2], 0.004) * inside(np.abs(P[:, 0]) - 0.21) * inside(-P[:, 1] - 0.15),
            ORANGE)
    k.paint(lambda P: inside(edge(P) - P[:, 2], 0.0025) * torso(P), YELLOW)

    # Two orange plumes for a tail
    for i, (d, length) in enumerate([((0.0, 0.85, -0.45), 0.09), ((0.35, 0.8, -0.3), 0.075)]):
        k.add(f"tail{i}", T.spike((0.0, 0.07, 0.33), d, length, 0.028, 0.02), ORANGE, "body", fuse=False)

    # Legs: grey shins out of the thighs, a grey knee at each thigh's side, grey feet with white claws
    k.add("shin", tube([(0.1, -0.006, 0.11), (0.102, -0.012, 0.045)], [0.037, 0.035]),
          GREY, "leg{s}", mirror=True)
    k.add("knee", ell((0.19, 0.0, 0.2), (0.016, 0.04, 0.04)), GREY, "leg{s}", fuse=False, mirror=True)
    T.foot(k, (0.102, -0.014, 0.03), 0.078, 0.02, GREY, claws=CLAW, claw_len=0.035, surface="shell")

    # Long thick yellow arms forward and a little down, a tuft of down at the wrist, three white claws
    sh, el, wr = np.array([0.07, 0.0, 0.5]), np.array([0.125, -0.085, 0.455]), np.array([0.15, -0.205, 0.41])
    k.add("arm", tube([sh, el, wr], [0.05, 0.045, 0.041]), YELLOW, "arm{s}", mirror=True)
    along = (wr - el) / np.linalg.norm(wr - el)
    for i, d in enumerate([(0.2, 0.6, 0.75), (0.85, 0.45, 0.2), (0.3, 0.55, -0.75), (-0.5, 0.6, 0.55)]):
        k.add(f"cuff{i}", T.spike(wr - along * 0.02, d, 0.095, 0.022, 0.019), YELLOW, "arm{s}", fuse=False,
              mirror=True)
    for i, dx in enumerate((-0.016, 0.0, 0.016)):
        base = wr + along * 0.032 + np.array([dx, 0, -0.004])
        k.add(f"claw{i}", T.claw(base, along + np.array([dx * 8, 0, -0.1]), 0.075, 0.015), CLAW, "arm{s}",
              fuse=False, mirror=True, surface="shell")

    # A small orange beak
    k.add("beak", ell((0, -0.136, 0.615), (0.037, 0.04, 0.02), rot=(10, 0, 0), deform=T.pointed), "#F07A3A",
          "head", fuse=False, surface="shell")

    # The crest: three orange plumes up and back
    for i, (base, d, length, width) in enumerate([
            ((-0.008, 0.02, 0.755), (-0.25, 0.35, 0.9), 0.14, 0.034),
            ((0.008, 0.03, 0.75), (0.45, 0.6, 0.66), 0.15, 0.036),
            ((0.0, -0.01, 0.762), (0.08, -0.4, 0.9), 0.06, 0.022)]):
        k.add(f"plume{i}", T.feather(base, d, length, width, thick=0.024), ORANGE, "head", fuse=False)

    k.build(voxel=VOXEL)

    E = T.on_ell(H, HR, DE)
    look = T.eye_open(EYE, pupil=INK, pupil_c=(-0.05, -0.08), pupil_r=(0.3, 0.42), glint=(-0.25, 0.12, 0.17))
    k.eye_states(lambda state: T.lay_eyes(k, state, E, DE, EYE_HALF, look, srgb(YELLOW), INK, outline=EYE_SHAPE,
                                          rim=INK, inset=0.78))
    k.export(out)


if __name__ == "__main__":
    combusken(*args())
