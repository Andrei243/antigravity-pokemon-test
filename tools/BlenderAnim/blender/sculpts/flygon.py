# Flygon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/flygon.py -- <the export's Flygon.glb> <out.glb>
# Grown from vibrava.py: the same diamond wings (a rim plate with a thicker plate of the membrane inset in it, raised
# in a V by wing_axes), the same tail as a tube through TAIL with its bands painted by its length, the same bulging
# eyes as fused balls carrying a patch. From the references: a light green desert dragon held in the air. A round
# pear of a body, a darker green patch on each shoulder; a slim neck (made shorter and thicker for the game) to a small
# head with a short snout, two great red goggles over its eyes (a dark lens, a white glint), two long antennae laid
# back from the brow, dark green above. Two broad diamond wings of pale green in a thick red rim. Small arms held
# forward with white claws, legs tucked with dark green thighs. A long tail curling down and back, banded dark
# green, ending in a fan of three red-rimmed diamonds.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, slab, srgb, inside, smoothstep, unit, eye_look, sibling, args

v = sibling("vibrava")
GREEN, DARK, RED, REDDARK, PALE, WHITE, INK = \
    "#A2D47E", "#3F9A3C", "#E0475A", "#8E1E2C", "#C2E496", "#F6F6F2", "#1A1214"

TAIL = np.array([(0, 0.055, 0.3), (0, 0.11, 0.225), (0.006, 0.16, 0.15), (0.03, 0.24, 0.095), (0.06, 0.33, 0.085),
                 (0.085, 0.4, 0.115), (0.1, 0.45, 0.16)])
TAIL_R = [0.05, 0.042, 0.035, 0.028, 0.022, 0.018, 0.014]

H = np.array([0.0, -0.075, 0.6])
HR = np.array([0.055, 0.064, 0.05])
EYE_C = np.array([0.05, -0.082, 0.614])
EYE_R = 0.036
EYE_D = unit(0.8, -0.52, 0.18)


def flygon(k):
    # --- body: a round pear, chest over belly, a darker green patch on each shoulder -------------------------------
    k.add("belly", ell((0, 0.01, 0.345), (0.088, 0.085, 0.09)), GREEN, "body")
    k.add("chest", ell((0, -0.012, 0.425), (0.068, 0.064, 0.07)), GREEN, "body")
    k.add("neck", tube([(0, -0.02, 0.45), (0, -0.04, 0.51), (0, -0.06, 0.57)], [0.04, 0.032, 0.03]), GREEN, "head")
    def shoulders(P):
        m = np.zeros(len(P))
        for s in (1, -1):
            q = (P - (0.055 * s, 0.035, 0.405)) / (0.042, 0.05, 0.05)
            m = np.maximum(m, inside(np.sqrt((q ** 2).sum(1)) - 1, 0.05))
        return m
    k.paint(shoulders, DARK, on=["belly", "chest"])

    # --- head: small, a short snout, the goggles as bulging red balls -----------------------------------------------
    k.add("head", ell(H, HR), GREEN, "head")
    k.add("snout", ell((0, -0.125, 0.588), (0.03, 0.04, 0.026)), GREEN, "head")
    k.add("goggle", ell(EYE_C, (EYE_R, EYE_R, EYE_R)), RED, "head", mirror=True)
    # the antennae: two long flat blades laid back from the brow, dark green
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        pts = np.array([(0.012, -0.1, 0.634), (0.02, -0.055, 0.66), (0.032, 0.03, 0.672), (0.046, 0.13, 0.66),
                        (0.056, 0.21, 0.636)]) * m
        k.add(f"antenna{side}", blade(pts, [0.016, 0.015, 0.013, 0.01, 0.003], (0, 0, 1), 0.55), DARK, "head",
              fuse=False, surface="scales")

    # --- wings: two broad diamonds, pale green in a thick red rim, raised in a V --------------------------------------
    v.diamond_wing(k, "wing", (0.045, 0.03, 0.455), "wing{s}", 0.36, 0.12, 6, 32, 24, rim=RED, fill=PALE,
                   thick=0.009, at=0.42, f=0.74)

    # --- arms: small, held forward and down, three white claws -------------------------------------------------------
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        a = np.array([(0.05, -0.03, 0.435), (0.07, -0.075, 0.415), (0.075, -0.105, 0.395)])
        k.add(f"arm{side}", tube(a * m, [0.022, 0.019, 0.018]), GREEN, f"arm{side}")
        hn = a[-1] + (0, -0.012, -0.004)
        for j, d in enumerate([(0.4, -1, 0.3), (0.0, -1, -0.1), (-0.4, -1, 0.2)]):
            d = unit(*d)
            k.add(f"claw{side}{j}", blade(np.array([hn, hn + d * 0.014, hn + d * 0.026]) * m, [0.009, 0.007, 0.002],
                                          (0, 0, 1), 0.8), WHITE, f"arm{side}", fuse=False, surface="scales")

    # --- legs: tucked under, dark green thighs fused into the belly, short shins and feet -------------------------
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.068, 0.0, 0.31)) * m, (0.042, 0.056, 0.058), rot=(25, 0, 0)), DARK,
              f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.07, -0.01, 0.29), (0.078, -0.035, 0.245), (0.076, -0.045, 0.215)]) * m,
                                  [0.03, 0.025, 0.022]), GREEN, f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.076, -0.065, 0.205)) * m, (0.026, 0.036, 0.018)), GREEN, f"leg{side}")

    # --- tail: long, curling down and back, banded dark green, a fan of three diamonds at its end ----------------------
    k.add("tail", tube(TAIL, TAIL_R), GREEN, "tail")
    def bands(P):
        s, d = v.along(P, TAIL)
        m = np.zeros(len(P))
        for x in (0.2, 0.27, 0.34, 0.41, 0.48):
            m = np.maximum(m, inside(np.abs(s - x) - 0.016, 0.0015))
        return m * inside(d - 0.06, 0.002)
    k.paint(bands, DARK, on=["tail"])
    tip, back = TAIL[-1], unit(*(TAIL[-1] - TAIL[-2]))
    turn = unit(0.6, 0.0, 0.0) - back * np.dot(unit(0.6, 0, 0), back)       # the fan's plane: back and out to +X
    side = unit(*np.cross(back, turn))
    for j, a in enumerate((-40, 0, 40)):
        ex = unit(*(back * math.cos(math.radians(a)) + side * math.sin(math.radians(a))))
        ez = np.cross(ex, unit(*np.cross(back, side)))
        out = v.diamond(0.085, 0.03, 0.5)
        o = tip + ex * 0.006
        k.add(f"fan{j}", slab(out, o, ex, ez, 0.009), RED, "tail", fuse=False, surface="scales")
        k.add(f"fanin{j}", slab(v.inset(out, 0.6), o, ex, ez, 0.014), PALE, "tail", fuse=False, surface="scales")


# --- eyes: the goggles' red domes, a dark lens low in each, a white glint, a darker rim ---------------------------------
CR, CD, CK, CW = srgb(RED), srgb(REDDARK), srgb(INK), np.array([1.0, 1.0, 1.0])


def eye(u, v_):
    c = np.tile(CR, (len(u), 1))
    r = np.sqrt(u * u + v_ * v_)
    c[r > 0.86] = CD
    c[((u + 0.15) / 0.36) ** 2 + ((v_ + 0.12) / 0.42) ** 2 < 1] = CK
    c[((u + 0.0) / 0.13) ** 2 + ((v_ - 0.3) / 0.14) ** 2 < 1] = CW
    return c


def lay_eyes(k, state):
    look = eye_look(state, eye, CR, CD, line=0.15)
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        c = EYE_C * m + EYE_D * m * EYE_R
        col = look if s == 1 else (lambda a, b, f=look: f(-a, b))
        k.decal(c, EYE_D * m, (0.035, 0.035), col, eps=0.003, rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")
    flygon(k)
    k.build(voxel=0.0045, smooth=4, band=0.006)
    k.eye_states(lambda st: lay_eyes(k, st))
    k.export(out)
