# Houndstone sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/houndstone.py -- <the export's Houndstone.glb> <out.glb>
#
# Greavard grown (greavard.py: its ragged teeth, its hanging hair, its colours). From the references: a long, low
# ghost dog whose body is a great dome of shaggy white fur, lilac over the top, hanging in long locks nearly to the
# ground; a skull of grey bone thrust forward and low from under a lilac ruff at the neck, the upper jaw narrow with
# two dark triangular sockets and a dark nose hole, the lower jaw great and lumpy, the mouth wide open and dark, both
# jaws ragged with teeth; a tall grey gravestone standing up from the back of the head, its head wider than its shaft,
# three wavy lines cut in its face; legs of bare bone with knobbed joints and clawed toes under the coat; a tail of
# bone segments curving up behind.
# The pictures overrule the brief: the head is a bare skull, not shaggy; the stone is a tall tablet, not rounded;
# the coat is white with lilac only over its top.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, tri, unit, eye_look, polygon, lumpy, args, sibling

M = sibling("maschiff")
G = sibling("greavard")

WHITE, LILAC, BONE, BONESHADE = "#EEF0F6", "#A9A2DC", "#C9CAD0", "#9C9DA8"
STONE, CUT, HOLLOW = "#9FA0AB", "#55566A", "#24222C"

# --- the frame (joints: body (0, 0.05, 0.32), head (0, -0.15, 0.37), legs (+-0.1, -0.13, 0.24),
#     hinds (+-0.1, 0.2, 0.24), tail (0, 0.26, 0.36)) ---------------------------------------------------------------------
CC = np.array([0.0, 0.08, 0.37])          # the coat's dome
CR = np.array([0.175, 0.245, 0.19])
UC = np.array([0.0, -0.315, 0.36])        # the upper jaw
UR = np.array([0.082, 0.14, 0.058])
JC = np.array([0.0, -0.31, 0.175])        # the great lower jaw
JR = np.array([0.112, 0.15, 0.078])


def bone_leg(k, x, y, top, knee, foot, r, bone):
    """A leg of bare bone: a shaft to a knobbed joint, a shaft to the foot, three clawed toes forward."""
    top, knee, foot = (np.array((x, y, top)), np.array((x, *knee)), np.array((x, *foot)))
    k.add(bone[:4] + "upper", tube([top, (top + knee) / 2, knee], [r * 1.25, r * 1.05, r * 1.1]), BONE,
          bone, mirror=True)
    k.add(bone[:4] + "knee", ell(knee, (r * 1.45, r * 1.45, r * 1.45)), BONE, bone, mirror=True)
    k.add(bone[:4] + "lower", tube([knee, (knee + foot) / 2, foot], [r * 1.05, r * 0.95, r * 1.05]), BONE,
          bone, mirror=True)
    k.add(bone[:4] + "heel", ell(foot, (r * 1.5, r * 1.6, r * 1.15)), BONE, bone, mirror=True)
    for j, dx in enumerate((-0.022, 0.0, 0.022)):
        a = foot + np.array((dx, -0.012, -foot[2] + 0.016))
        k.add(f"{bone[:4]}toe{j}", G.fang(a, (dx * 2.5, -1, -0.12), 0.06, 0.013, 0.01), BONE, bone, fuse=False,
              mirror=True, surface="shell")


def gravestone(k, base, lean=0.2):
    """The tall tablet standing up from the back of the head, its head wider than its shaft, its corners cut, three
    wavy lines raised dark on its face."""
    base = np.array(base, float)
    up = unit(0, lean, 1)
    ex = np.array((1.0, 0, 0))
    outline = [(-0.05, 0.0), (0.05, 0.0), (0.05, 0.17), (0.078, 0.192), (0.078, 0.29), (0.056, 0.315),
               (-0.056, 0.315), (-0.078, 0.29), (-0.078, 0.192), (-0.05, 0.17)]
    k.add("stone", slab(outline, base, ex, up, 0.064), STONE, "head", fuse=False, surface="shell")
    face = unit(0, -1, lean)                            # the stone's front
    for i, h in enumerate((0.27, 0.23, 0.19)):
        pts = [base + up * h + ex * (-0.042 + 0.084 * t) + up * (0.008 * math.sin(t * 3 * math.pi))
               + face * 0.033 for t in np.linspace(0, 1, 9)]
        k.add(f"cut{i}", tube(pts, [0.0085] * 9), CUT, "head", fuse=False, surface="shell")


def houndstone(src, out):
    k = Kit(src, coat="fur")

    # --- the coat: a great dome of white fur, lilac over its top, hanging in long locks ------------------------
    k.add("coat", ell(CC, CR), WHITE, "body")
    k.add("withers", ell((0, -0.1, 0.4), (0.15, 0.1, 0.14)), WHITE, "body")
    k.paint(lambda P: inside((0.47 + 0.016 * tri(np.arctan2(P[:, 0], P[:, 1] - 0.08) * 2.5)
                              + 0.01 * tri(P[:, 1] * 9)) - P[:, 2], 0.004), LILAC, on=["coat", "withers"])
    G.hanging_hair(k, "lock", (0, 0.08, 0), CR * (1.0, 1.0, 1), 0.33, 22, 0.27, WHITE, "body", start=0.45,
                   end=2 * math.pi - 0.45, width=0.042)
    G.hanging_hair(k, "lockin", (0, 0.08, 0), CR * (0.86, 0.9, 1), 0.3, 18, 0.23, WHITE, "body", start=0.6,
                   end=2 * math.pi - 0.6, width=0.04)
    # the lilac ruff round the neck, its locks hanging in front
    k.add("ruff", ell((0, -0.19, 0.45), (0.135, 0.085, 0.07), deform=lumpy(5, 2, 0.08)), LILAC, "head")
    for i, x in enumerate((-0.1, -0.05, 0.0, 0.05, 0.1)):
        M.tuft(k, f"ruffl{i}", (x, -0.24 + abs(x) * 0.4, 0.41), (x * 1.2, -0.3, -1), 0.08, 0.034, LILAC, "head",
               flat=(0, 1, 0), f=0.45)
    for i, x in enumerate((-0.13, 0.13)):
        M.tuft(k, f"bib{i}", (x, -0.16, 0.36), (x * 1.5, -0.3, -1), 0.2, 0.04, WHITE, "body", flat=(0, 1, 0), f=0.42)

    # --- legs of bare bone under the coat ---------------------------------------------------------------------
    bone_leg(k, 0.1, -0.13, 0.26, (-0.17, 0.13), (-0.19, 0.03), 0.024, "leg{s}")
    bone_leg(k, 0.1, 0.2, 0.26, (0.25, 0.14), (0.22, 0.03), 0.024, "hind{s}")

    # --- the tail: bone segments curving up behind, a little out to +X ----------------------------------------
    path = [(0.0, 0.29, 0.43), (0.012, 0.34, 0.47), (0.024, 0.38, 0.52), (0.034, 0.41, 0.57), (0.04, 0.425, 0.62),
            (0.044, 0.43, 0.665)]
    for i, p in enumerate(path):
        r = 0.027 - 0.0025 * i
        k.add(f"tailseg{i}", ell(p, (r, r, r * 1.2)), BONE, "tail")
    k.add("tailcore", tube(path, [0.017, 0.016, 0.015, 0.013, 0.012, 0.01]), BONE, "tail")

    # --- the skull: thrust forward and low, the mouth wide open --------------------------------------------------
    k.add("skull", ell((0, -0.24, 0.39), (0.09, 0.09, 0.07)), BONE, "head")
    k.add("upper", ell(UC, UR, rot=(-12, 0, 0)), BONE, "head")
    k.add("jaw", ell(JC, JR, deform=lumpy(3, 2, 0.07)), BONE, "head")
    k.add("jawback", ell((0, -0.22, 0.22), (0.1, 0.08, 0.09)), BONE, "head")
    k.add("mouth", ell((0, -0.29, 0.27), (0.075, 0.13, 0.065)), HOLLOW, "head")
    # the lower jaw darker underneath
    k.paint(lambda P: inside(P[:, 2] - (0.14 + 0.01 * tri(P[:, 1] * 14)), 0.005) * (P[:, 1] < -0.2), BONESHADE,
            on=["jaw"])
    G.ragged_lip(k, "uppertooth", UC + (0, -0.006, 0), UR * (0.9, 1.0, 1), UC[2] - UR[2] * 0.55, True, 9, 0.036, 0.015,
                 BONE, "head", span=1.15)
    G.ragged_lip(k, "lowertooth", JC, JR * (0.82, 0.92, 1), JC[2] + JR[2] * 0.55, False, 9, 0.04, 0.017, BONE, "head",
                 span=1.15)

    gravestone(k, (0.0, -0.18, 0.44))

    # the dark nose hole on the upper jaw's front
    nose_at = M.on_ell(UC, UR, 0.0, UC[2] + 0.025) + (0, 0.02, 0)
    k.decal(nose_at, unit(0, -0.7, 0.75), (0.022, 0.02), HOLLOW, eps=0.0025,
            outline=polygon([(0, -1), (1, 0.7), (-1, 0.7)]), rings=10, segs=40)

    k.build(voxel=0.0066, smooth=3, band=0.006, wband=0.06)

    # --- the eyes: dark triangular sockets on the upper jaw, toward its back -----------------------------------
    E = M.on_ell(UC, UR, 0.046, UC[2] + 0.03) + (0, 0.035, 0.01)
    n = unit(0.55, -0.6, 0.6)
    hollow, bone = srgb(HOLLOW), srgb(BONE)
    def socket(u, v):
        return np.tile(hollow, (len(u), 1))
    def lay(state):
        f = eye_look(state, socket, bone, hollow, line=0.18)
        for s in (1, -1):
            col = f if s == 1 else (lambda a, b, g=f: g(-a, b))
            ol = polygon([(s * a, b) for a, b in ((-0.95, 0.75), (0.95, 0.85), (0.15, -1.0))]) \
                if state in ("Open", "Fierce") else None
            k.decal(E * np.array([s, 1, 1.0]), n * np.array([s, 1, 1.0]), (0.028, 0.022), col, eps=0.0025,
                    outline=ol, rings=20, segs=56)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    houndstone(*args())
