# Liliacon (Carpathia No. 10140) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/liliacon.py -- <the export's Liliacon.glb> <out.glb>
# From its picture: a large dark brown bat crouched on all fours, walking on its folded wings: two enormous tall
# pointed ears standing up and a little out like twin speaker horns (the Sound type's part, seen first), their insides
# violet with ridges in rings of light and dark; a round head with a short muzzle, a leaf-shaped nose standing up on
# it, the mouth open on four small fangs, small black eyes; a shaggy ruff of fur over the neck and chest; a deep furry
# body on short crouched hind legs with clawed feet; the long arms out and down to clawed hands on the ground, two long
# fingers rising behind from each elbow, leathery membranes brown outside and violet inside; a small furry tail.
# The rings of sound from its mouth in the picture are a drawn effect and left out. Liliacel's line: its eye, its
# palette's browns and its violet, and its helpers (sibling("liliacel")).
# Not a flier by type (Dark/Sound) and standing on the ground in its picture: built standing (lowest 0), where the
# export's generated body hovers.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, outward, srgb, inside, smoothstep, unit, eye_look, polygon, args, \
    sibling

L = sibling("liliacel")
N = sibling("noibat")

FUR, FURDARK, FURLIGHT = "#4B3A33", "#3A2D28", "#5E4A40"
SKIN, MEMBRANE = "#54413A", "#4A3932"
VIOLET, VIOLETDARK, VIOLETLIGHT, EARCORE = "#7C4CB8", "#5E3794", "#A57BE0", "#3E2A52"
NOSE, MOUTH, TOOTH = "#6E5550", "#6E2834", "#F2EEE6"
CLAW = "#2A221F"

HC, HR = np.array([0.0, -0.13, 0.6]), np.array([0.135, 0.122, 0.118])
VOXEL = 0.0068


def membrane(k, name, pts, hint, inner_back, bone):
    """A wing's skin: a brown plate with a violet plate let into one face (back or front), its brown rim showing."""
    shape, n = L.plate(pts, 0.022, hint=hint)
    k.add(name, shape, MEMBRANE, bone, fuse=False, mirror=True, surface="leather")
    P = np.array(pts, float); c = P.mean(axis=0)
    inner = c + (P - c) * 0.86
    sgn = -1 if inner_back else 1
    inner = inner + n * sgn * (0.011 - 0.006 + 0.003)
    shape2, _ = L.plate(inner, 0.012, hint=hint)
    k.add(name + "v", shape2, VIOLET, bone, fuse=False, mirror=True, surface="leather")


def liliacon(src, out):
    k = Kit(src, coat="fur")

    # The body: deep and furry, leaning a little forward; the chest under the ruff
    k.add("barrel", ell((0, 0.05, 0.35), (0.158, 0.155, 0.17), rot=(28, 0, 0)), FUR, "body")
    k.add("chest", ell((0, -0.075, 0.47), (0.15, 0.13, 0.13)), FUR, "body")
    k.add("neck", ell((0, -0.1, 0.54), (0.11, 0.1, 0.09)), FUR, "head")

    # The head: round, a short muzzle, the jaw dropped open, the nose-leaf standing up on the muzzle
    k.add("head", ell(HC, HR), FUR, "head")
    k.add("muzzle", ell((0, -0.24, 0.582), (0.066, 0.06, 0.05)), FURLIGHT, "head")
    k.add("jaw", ell((0, -0.225, 0.525), (0.052, 0.055, 0.028), rot=(-12, 0, 0)), FURLIGHT, "head")
    k.add("mouth", ell((0, -0.235, 0.548), (0.044, 0.05, 0.022)), MOUTH, "head")
    k.add("noseleaf", hull([(0.032, -0.293, 0.595), (-0.032, -0.293, 0.595), (0, -0.283, 0.58),
                            (0, -0.285, 0.68), (0.012, -0.275, 0.63), (-0.012, -0.275, 0.63), (0, -0.267, 0.6)]),
          NOSE, "head", fuse=False)
    for s in (1, -1):
        k.add(f"fang{'R' if s > 0 else 'L'}", L.claw((0.028 * s, -0.275, 0.565), (0, -0.15, -1), 0.03, 0.011),
              TOOTH, "head", fuse=False, surface="shell")
        k.add(f"lfang{'R' if s > 0 else 'L'}", L.claw((0.024 * s, -0.263, 0.528), (0, -0.2, 1), 0.022, 0.009),
              TOOTH, "head", fuse=False, surface="shell")
        k.decal((0.017 * s, -0.297, 0.58), unit(0.2 * s, -1, 0.05), (0.01, 0.008), L.NOSTRIL, eps=0.003,
                rings=10, segs=36)

    # The ears: tall pointed dishes standing up and a little out, their faces forward, violet with ridged rings
    tear = N.tear_outline(0.5)
    EAR_ROOT = np.array([0.08, -0.11, 0.665])
    EAR_D = unit(0.5, 0.06, 1.0)
    EAR = (0.152, 0.092, 0.028)
    N.ear_dish(k, "ear", "head", EAR_ROOT + EAR_D * (EAR[0] * 0.9), EAR_D, unit(0.36, -1.0, 0.05), EAR, FUR,
               [(0.85, VIOLET, tear, 0.0025), (0.7, VIOLETLIGHT, tear, 0.004), (0.55, VIOLETDARK, tear, 0.0055),
                (0.4, VIOLETLIGHT, tear, 0.007), (0.24, EARCORE, N.tear_outline(0.4), 0.0085)],
               shape=N.teardrop(0.55), front=0.4)

    # The ruff: thick tapered locks of fur round the neck and over the chest, tips lifting clear
    for i, a in enumerate(range(-150, 151, 23)):
        t = math.radians(a)
        dx, dy = math.sin(t), -math.cos(t)
        b = (0.115 * dx, -0.09 + 0.1 * dy, 0.51)
        col = FURLIGHT if i % 2 else FUR
        k.add(f"ruff{i}", L.claw(b, (dx * 0.8, dy * 0.8, -1.0), 0.085, 0.03, up=(0, 0, 1)), col, "body",
              fuse=False)
    for i, a in enumerate((-50, -20, 10, 40)):
        t = math.radians(a)
        b = (0.09 * math.sin(t), -0.16 - 0.03 * math.cos(t), 0.45)
        k.add(f"bib{i}", L.claw(b, (math.sin(t) * 0.5, -0.6, -1.0), 0.07, 0.026, up=(0, 0, 1)), FUR, "body",
              fuse=False)
    # Fur on the crown and the back of the head, between the ears
    for i, (x, y, z, d) in enumerate([(0.0, -0.085, 0.71, (0, 0.6, 1)), (0.05, -0.035, 0.68, (0.5, 0.8, 0.6)),
                                      (-0.05, -0.035, 0.68, (-0.5, 0.8, 0.6)), (0.0, -0.005, 0.64, (0, 1, 0.3))]):
        k.add(f"crown{i}", L.claw((x, y, z), d, 0.05, 0.022, up=(0, 0, 1)), FUR, "head", fuse=False)

    # The hind legs: short and crouched, thighs fused into the flanks, clawed feet
    k.add("thigh", ell((0.11, 0.06, 0.2), (0.075, 0.09, 0.095)), FUR, "leg{s}", mirror=True)
    k.add("shin", tube([(0.12, 0.05, 0.14), (0.13, 0.01, 0.06)], [0.042, 0.036]), FUR, "leg{s}", mirror=True)
    k.add("foot", ell((0.13, -0.035, 0.022), (0.045, 0.06, 0.022)), SKIN, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.025, 0.0, 0.025)):
        k.add(f"toe{j}", L.claw((0.13 + dx, -0.085, 0.02), (dx * 10, -1, -0.4), 0.03, 0.011, up=(0, 0, 1)), CLAW,
              "leg{s}", fuse=False, mirror=True, surface="shell")

    # The wings walked on: the arm up and out to a high elbow, the forearm down to a clawed hand on the ground,
    # two long fingers rising back from the elbow, and the skin between
    SH, EL, WR, HD = (np.array(p) for p in [(0.13, -0.04, 0.47), (0.31, 0.08, 0.52), (0.3, -0.15, 0.13),
                                            (0.285, -0.2, 0.035)])
    T1, T2 = np.array([0.405, 0.33, 0.62]), np.array([0.345, 0.41, 0.47])
    HIP, FLANK = np.array([0.16, 0.19, 0.24]), np.array([0.14, -0.04, 0.29])
    k.add("upperarm", outward(tube([SH, EL], [0.045, 0.034])), SKIN, "wing{s}", fuse=False, mirror=True,
          surface="leather")
    k.add("forearm", outward(tube([EL, WR, HD + (0, 0, 0.02)], [0.034, 0.028, 0.026])), SKIN, "wing{s}",
          fuse=False, mirror=True, surface="leather")
    k.add("hand", outward(ell(HD, (0.04, 0.05, 0.026), sub=3)), SKIN, "wing{s}", fuse=False, mirror=True,
          surface="leather")
    for j, dx in enumerate((-0.022, 0.0, 0.022)):
        k.add(f"hclaw{j}", L.claw(HD + (dx, -0.04, -0.008), (dx * 10, -1, -0.45), 0.032, 0.011, up=(0, 0, 1)),
              CLAW, "wing{s}", fuse=False, mirror=True, surface="shell")
    for j, T in enumerate((T1, T2)):
        k.add(f"finger{j}", outward(tube([EL, (EL + T) / 2 + (0.0, 0.0, 0.03), T], [0.026, 0.02, 0.015])), SKIN,
              "wing{s}", fuse=False, mirror=True, surface="leather")
        d = T - EL
        k.add(f"ftip{j}", L.claw(T, (d[0], d[1], d[2]), 0.035, 0.013), CLAW, "wing{s}", fuse=False, mirror=True,
              surface="shell")
    # Outer skin between the fingers and down to the hip: brown outside, violet on its inner (back) face
    membrane(k, "skinA", [EL, T1, T2], hint=(1, -0.5, 0.2), inner_back=True, bone="wing{s}")
    membrane(k, "skinB", [EL, T2, HIP], hint=(1, -0.4, 0.2), inner_back=True, bone="wing{s}")
    # Inner skin between the forearm and the flank: violet to the front
    membrane(k, "skinC", [EL, WR, FLANK], hint=(0.3, -1, 0.1), inner_back=False, bone="wing{s}")
    membrane(k, "skinD", [EL, FLANK, SH], hint=(0.3, -1, 0.1), inner_back=False, bone="wing{s}")

    # The tail: short and furry, out back and a little to +X
    k.add("tail", tube([(0, 0.15, 0.3), (0.02, 0.26, 0.25), (0.06, 0.33, 0.24)], [0.045, 0.035, 0.022]), FUR,
          "tail")
    for i, d in enumerate([(0.5, 1, 0.2), (0.7, 0.7, -0.3), (0.2, 1, -0.4)]):
        k.add(f"tailtuft{i}", L.claw((0.05, 0.31, 0.24), d, 0.05, 0.018, up=(0, 0, 1)), FURLIGHT, "tail",
              fuse=False)

    k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.05)

    LID, INK = srgb(FUR), srgb(L.EYEINK)
    ED = unit(0.44, -0.86, 0.22)
    E = L.on_ell(HC, HR, ED)
    def eyes(state):
        k.decal(E, ED, (0.023, 0.023), eye_look(state, L.bat_eye(), LID, INK, line=0.17), rings=20, segs=56,
                eps=0.004, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    liliacon(*args())
