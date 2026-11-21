# Frogadier sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/frogadier.py -- <the export's Frogadier.glb> <out.glb>
#
# Froakie grown (froakie.py: the same blues, the loose foam bubbles, three digits with round tips, the eyes laid on the
# head's front corners). From the references: a lean frog crouched to spring, leaning forward with its head carried low
# in front. The head long and flat, dark blue above and pale blue below the mouth, a dark blue crest sweeping back off
# the crown in a point; the eyes yellow, heavy-lidded (the dark lid cuts their top flat) with a black bar of a pupil; a
# white spot on each side of the snout's tip. A scarf of foam bubbles round the neck, trailing back over one shoulder.
# A pale blue body with a dark blue stripe down the chest and over the back. Long pale arms reaching down to the
# ground, dark blue hands with three fingers tipped white; thick pale thighs folded high, short shins, dark blue feet
# with three toes tipped white.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, polygon, srgb, inside, unit, eye_look, args, sibling

fk = sibling("froakie")
PALE, DARK, WHITE, GREY, YELLOW, INK = ("#6CBDEE", "#2F5FB8", fk.WHITE, fk.GREY, "#F2D84A", fk.INK)


def frogadier(src, out):
    k = Kit(src, coat="scales")
    V = 0.0034

    # --- the trunk: leaning forward over the hips --------------------------------------------------------------------
    k.add("body", ell((0, -0.004, 0.165), (0.046, 0.048, 0.072), rot=(-22, 0, 0)), PALE, "body")
    k.add("hips", ell((0, 0.02, 0.105), (0.05, 0.05, 0.045)), PALE, "body")
    k.add("neck", ell((0, -0.03, 0.228), (0.036, 0.036, 0.03)), PALE, "body")
    # the dark stripe down the chest and the dark back
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.016, 0.0025) * inside(P[:, 1] + 0.02, 0.004)
            * inside(P[:, 2] - 0.235, 0.004) * inside(0.11 - P[:, 2], 0.004), DARK, on=["body", "neck"])
    # the dark back, narrowing to a point over the hips
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.03 * np.clip((P[:, 2] - 0.09) / 0.08, 0, 1), 0.003)
            * inside(0.03 - P[:, 1], 0.004) * inside(P[:, 2] - 0.24, 0.004), DARK, on=["body", "hips"])

    # --- the head: long, broad and flat, carried low in front ---------------------------------------------------------
    HC = np.array([0.0, -0.062, 0.272])
    k.add("head", ell(HC, (0.066, 0.08, 0.047)), DARK, "head")
    k.add("snout", ell((0, -0.112, 0.264), (0.05, 0.044, 0.034)), DARK, "head")
    k.add("jaw", ell((0, -0.075, 0.249), (0.06, 0.068, 0.03)), PALE, "head")
    # the line of the mouth: dark above, pale below, by place round the head
    k.paint(lambda P: inside(P[:, 2] - (0.252 + 0.06 * np.clip(P[:, 1] + 0.13, 0, 1)), 0.0015)
            * inside(np.sqrt((P[:, 0] / 0.072) ** 2 + ((P[:, 1] + 0.075) / 0.1) ** 2) - 1, 0.002), PALE,
            on=["head", "snout", "jaw"])
    # the crest: a dark point sweeping back off the crown, laid back
    k.add("crest", hull([(-0.014, -0.07, 0.31), (0.014, -0.07, 0.31), (0.0, -0.08, 0.316),
                         (-0.01, -0.01, 0.314), (0.01, -0.01, 0.314), (0.0, -0.03, 0.328),
                         (0.0, 0.085, 0.338)]), DARK, "head", fuse=False)
    k.add("crest2", hull([(-0.009, -0.04, 0.312), (0.009, -0.04, 0.312), (0.0, -0.03, 0.322),
                          (0.0, 0.02, 0.322), (0.0, 0.035, 0.334)]), DARK, "head", fuse=False)
    # a white spot on each side of the snout's tip
    k.decal((0.016, -0.146, 0.274), unit(0.35, -1, 0.4), (0.0055, 0.0045), WHITE, eps=0.0014, rings=8, segs=40,
            mirror=True)

    # --- the scarf of foam: round the neck, trailing back over the +X shoulder ---------------------------------------
    fk.bubble_ring(k, "scarf", (0, -0.02, 0.222), 0.046, [-150, -120, -90, -60, -30, 0, 30, 60, 90, 120, 150, 180],
                   [0.02, 0.024, 0.026, 0.024, 0.022, 0.024, 0.024, 0.026, 0.026, 0.024, 0.02, 0.021], "body",
                   zs=[-0.006, 0.0, 0.006, 0.01, 0.012, 0.012, 0.012, 0.01, 0.006, 0.0, -0.006, -0.008],
                   cols=[WHITE, GREY, WHITE, WHITE, GREY, WHITE, WHITE, GREY, WHITE, WHITE, GREY, WHITE])
    for i, (p, r) in enumerate([((0.06, 0.03, 0.236), 0.022), ((0.08, 0.055, 0.232), 0.018),
                                ((0.097, 0.077, 0.226), 0.014)]):
        fk.bubble(k, f"trail{i}", p, r, WHITE if i != 1 else GREY, "body")

    # --- arms: long and pale, reaching forward and down to the ground; dark hands, white-tipped fingers -------------
    SH, EL, WR = np.array((0.046, -0.025, 0.212)), np.array((0.066, -0.07, 0.15)), np.array((0.05, -0.1, 0.05))
    k.add("upper", tube([SH, (0.058, -0.045, 0.185), EL], [0.0145, 0.013, 0.012]), PALE, "arm{s}", mirror=True)
    k.add("fore", tube([EL, (0.06, -0.088, 0.1), WR], [0.012, 0.0115, 0.0115]), PALE, "arm{s}", mirror=True)
    k.add("hand", ell((0.05, -0.106, 0.03), (0.016, 0.017, 0.016)), DARK, "arm{s}", mirror=True)
    fk.digits(k, "finger", (0.05, -0.115, 0.016), (0.1, -0.9, -0.5), (0.022, 0, 0), 0.016, 0.0055, DARK,
              "arm{s}", tip=WHITE, tip_r=0.0075)

    # --- legs: thick thighs folded high beside the hips, short shins, dark feet ---------------------------------------
    k.add("thigh", tube([(0.03, 0.025, 0.105), (0.068, 0.0, 0.092), (0.088, -0.035, 0.075)], [0.042, 0.036, 0.026]),
          PALE, "leg{s}", mirror=True)
    k.add("shin", tube([(0.088, -0.035, 0.075), (0.088, -0.008, 0.045), (0.08, 0.018, 0.02)], [0.022, 0.018, 0.015]),
          PALE, "leg{s}", mirror=True)
    k.add("foot", ell((0.083, -0.008, 0.01), (0.022, 0.034, 0.01)), DARK, "leg{s}", mirror=True)
    fk.digits(k, "toe", (0.086, -0.036, 0.008), (0.3, -1.0, 0.0), (0.034, 0, 0), 0.02, 0.006, DARK, "leg{s}",
              tip=WHITE, tip_r=0.0078)

    k.build(voxel=V, smooth=4, band=0.005, wband=0.025)

    # --- the eyes: yellow, heavy-lidded, a black bar of a pupil -------------------------------------------------------
    CY, CK, CD = srgb(YELLOW), srgb(INK), srgb(DARK)
    # the heavy lid cuts the eye's top straight across: the patch's own outline, so the line is crisp
    cut = polygon([(-1.3, -1.3), (1.3, -1.3), (1.3, 0.22), (-1.3, 0.22)])
    def lidded(th):
        return min(1.0, cut(th))
    def eye(u, v):
        return np.tile(CY, (len(u), 1))
    E = np.array([0.05, -0.102, 0.288])
    de = unit(0.62, -0.75, 0.25)
    def eyes(state):
        k.decal(E, de, (0.026, 0.027), eye_look(state, eye, CD, CK, line=0.13), eps=0.0016, rings=20, segs=56,
                mirror=True, outline=lidded)
        if state in ("Open", "Fierce"):
            k.decal(E + np.array([-0.0015, -0.0005, -0.0035]), de, (0.013, 0.0068), CK, eps=0.0026, rings=8,
                    segs=44, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    frogadier(*args())
