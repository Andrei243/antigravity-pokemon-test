# Mienshao sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mienshao.py -- <the export's Mienshao.glb> <out.glb>
# Grown from mienfoo.py (its frame_ell, on_head, nose and forked tips, the same pink nose and way of making a face).
# From the references: a tall pale grey-white ermine: a small head on a long thick neck, a long pointed muzzle with a
# pink nose, short pointed ears, a purple patch over the top of the head, heavy half-shut eyes under purple lids with
# a red iris below; a long whisker at each side of the muzzle ending in a yellow fork; long white arms whose fur hangs
# from the forearms as broad whips ending in purple points (held forward and down, not crossed as in the art); a slim
# torso with a purple band at the waist; purple haunches and legs like loose trousers, purple feet with white claws;
# a long thin white tail out behind and to one side, its tip a purple fan of three points.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, slab, hull, outward, polygon, srgb, inside, smoothstep, unit,
                       eye_look, args, sibling)

mf = sibling("mienfoo")

WHITE, PURPLE, YELLOW, INK = "#E8E4EA", "#A994D8", "#F2C66A", "#26161E"
IRIS, CLAW = "#B0203C", "#F8F8F8"

HC, HR = np.array([0.0, -0.028, 0.542]), np.array([0.064, 0.066, 0.056])


def mienshao(src, out):
    k = Kit(src, coat="fur")

    # The head: small and smooth, a long pointed muzzle, short pointed ears out to the sides and back
    k.add("head", ell(HC, HR), WHITE, "head")
    def taper(V):
        V = V.copy(); t = np.clip((1 - V[:, 1]) / 2, 0, 1)              # 0 at the back, 1 at the tip (-Y)
        g = 1 - 0.55 * t ** 1.4
        V[:, 0] *= g; V[:, 2] *= g
        return V
    k.add("muzzle", ell((0, -0.088, 0.516), (0.035, 0.055, 0.03), rot=(4, 0, 0), deform=taper), WHITE, "head")
    k.add("ear", blade([(0.038, 0.0, 0.565), (0.058, 0.012, 0.6), (0.078, 0.022, 0.628)], [0.026, 0.017, 0.003],
                       (0.3, 1, 0), 0.6), WHITE, "ear{s}", mirror=True)
    # the purple patch over the top of the head, from above the eyes back between the ears
    k.paint(lambda P: inside(0.567 - P[:, 2] - 0.01 * smoothstep(0.0, 0.05, -P[:, 1]), 0.003)
            * (np.linalg.norm((P - HC) / (HR * 1.35), axis=1) < 1) * inside(np.abs(P[:, 0]) - 0.045, 0.003),
            PURPLE, on=["head"])

    # The whiskers: long, out and back from the muzzle's sides, a little up, a yellow fork at each end
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        W = np.array([(0.022, -0.098, 0.514), (0.065, -0.07, 0.54), (0.105, -0.01, 0.585)]) * m
        k.add(f"whisker{side}", outward(tube(W, [0.011, 0.01, 0.01])), WHITE, "head", fuse=False)
        d = unit(*(W[2] - W[1]))
        up = np.array([0, 0, 1.0])
        for i, a in enumerate((-0.6, 0.6)):
            g = unit(*(d + up * a))
            k.add(f"fork{side}{i}", outward(blade([W[2] - d * 0.012, W[2] + g * 0.025, W[2] + g * 0.042],
                                                  [0.012, 0.01, 0.003], np.cross(d, up), 0.65)),
                  YELLOW, "head", fuse=False)

    # A long thick neck, a slim chest and waist, a purple band at the waist
    k.add("neck", tube([(0, 0.0, 0.38), (0, -0.01, 0.44), (0, -0.022, 0.5)], [0.04, 0.034, 0.034]), WHITE, "body")
    k.add("chest", ell((0, 0.0, 0.372), (0.062, 0.048, 0.054)), WHITE, "body")
    k.add("waist", ell((0, 0.004, 0.315), (0.054, 0.045, 0.055)), WHITE, "body")
    k.paint(lambda P: inside(P[:, 2] - 0.35, 0.0025) * inside(0.322 - P[:, 2], 0.0025), PURPLE,
            on=["waist", "chest"])

    # Purple haunches and legs like loose trousers, crouched a little; purple feet with white claws
    k.add("hips", ell((0, 0.01, 0.262), (0.072, 0.058, 0.05)), WHITE, "body")
    k.paint(lambda P: inside(P[:, 2] - 0.228 - 0.04 * np.clip(np.abs(P[:, 0]) / 0.06, 0, 1), 0.003), PURPLE,
            on=["hips", "thigh"])
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", tube(np.array([(0.045, 0.005, 0.245), (0.078, -0.04, 0.15), (0.072, -0.005, 0.055)]) * m,
                                   [0.056, 0.044, 0.032]), PURPLE, f"leg{side}")
        k.add(f"foot{side}", mf.frame_ell((0.074 * s, -0.03, 0.018), (0.032, 0.046, 0.019), (1, 0, 0), (0, 1, 0),
                                          (0, 0, 1)), PURPLE, f"leg{side}", fuse=False)
        for i, o in enumerate((-1, 0, 1)):
            b = np.array([0.074 * s + o * 0.017, -0.066, 0.014])
            k.add(f"claw{side}{i}", hull([b + (-0.007, 0.01, -0.006), b + (0.007, 0.01, -0.006), b + (-0.006, 0.01, 0.008),
                                          b + (0.006, 0.01, 0.008), b + (0.0, -0.022, -0.008)]), CLAW, f"leg{side}",
                  fuse=False)

    # The arms: long, white, held forward and down; from each forearm a broad whip of fur hangs to the knee
    SH, EL, WR = np.array([0.052, -0.01, 0.44]), np.array([0.105, -0.045, 0.39]), np.array([0.1, -0.13, 0.385])
    k.add("upper", tube([SH, EL], [0.03, 0.028]), WHITE, "arm{s}", mirror=True)
    k.add("fore", tube([EL, WR], [0.028, 0.025]), WHITE, "arm{s}", mirror=True)
    k.add("hand", ell(WR + np.array([0, -0.012, 0]), (0.024, 0.026, 0.022)), WHITE, "arm{s}", mirror=True)
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        n = unit(0.65 * s, -0.76, 0)                                  # its flat face turned out and forward
        top = np.array([(0.104, -0.085, 0.385), (0.13, -0.08, 0.31), (0.148, -0.066, 0.235)]) * m
        k.add(f"whip{side}", outward(blade(top, [0.024, 0.03, 0.028], n, 0.42)), WHITE, f"arm{side}", fuse=False)
        low = np.array([(0.146, -0.068, 0.25), (0.154, -0.058, 0.2), (0.156, -0.052, 0.16)]) * m
        k.add(f"whiptip{side}", outward(blade(low, [0.031, 0.032, 0.026], n, 0.45)), PURPLE, f"arm{side}",
              fuse=False)
        end = low[-1]
        across = unit(*np.cross(n, (0, 0, 1)))
        for i, o in enumerate((-1, 0, 1)):
            g = unit(*(np.array([0, 0, -1.0]) + across * 0.35 * o))
            pts = [end + across * 0.016 * o + np.array([0, 0, 0.01]), end + across * 0.018 * o + g * 0.03,
                   end + across * 0.02 * o + g * 0.055]
            k.add(f"whippt{side}{i}", outward(blade(pts, [0.016, 0.013, 0.003], n, 0.5)), PURPLE, f"arm{side}",
                  fuse=False)

    # The tail: long and thin, back and out to the +X side, rising; a purple fan of three points at its tip
    TP = np.array([(0.0, 0.04, 0.29), (0.03, 0.12, 0.25), (0.1, 0.2, 0.25), (0.17, 0.25, 0.3), (0.2, 0.27, 0.35)])
    k.add("tail", tube(TP, [0.02, 0.017, 0.015, 0.014, 0.014]), WHITE, "tail")
    d = unit(*(TP[-1] - TP[-2]))
    side = unit(*np.cross(d, (0, 0, 1)))
    for i, a in enumerate((-0.7, 0.0, 0.7)):
        g = unit(*(d + side * a))
        k.add(f"tailtip{i}", blade([TP[-1] - d * 0.02, TP[-1] + g * 0.03, TP[-1] + g * 0.06],
                                   [0.018, 0.015, 0.003], np.cross(side, d), 0.6), PURPLE, "tail")
    k.add("tailend", ell(TP[-1], (0.019, 0.019, 0.019)), PURPLE, "tail")

    # The pink nose (in every state)
    mf.nose(k, (0, -0.142, 0.518), size=0.9)

    # The eyes: heavy, half shut: an almond of purple lid over a red iris on white
    CP, CW, CI, CK = srgb(PURPLE), srgb("#FBF6F2"), srgb(IRIS), srgb(INK)
    def open_eye(u, v):
        c = np.tile(CW, (len(u), 1))
        c[((u + 0.1) / 0.5) ** 2 + ((v + 0.35) / 0.6) ** 2 < 1] = CI
        c[((u + 0.1) / 0.24) ** 2 + ((v + 0.4) / 0.3) ** 2 < 1] = CK
        lid = 0.05 - 0.1 * u
        c[v > lid] = CP
        c[np.abs(v - lid) < 0.14] = CK
        return c
    def fierce(u, v):
        c = open_eye(u, v)
        lid = -0.05 + 0.35 * u
        c[v > lid] = CP
        c[np.abs(v - lid) < 0.14] = CK
        return c
    ALMOND = polygon([(-1, 0.0), (-0.6, -0.6), (0.2, -0.85), (0.85, -0.45), (1, 0.15), (0.55, 0.75), (-0.2, 0.85),
                      (-0.75, 0.5)])
    E = mf.on_head(0.036, 0.54, HC, HR)
    def lay(state):
        look = fierce if state == "Fierce" else eye_look(state, open_eye, CP, CK, line=0.16)
        for s in (1, -1):
            shape = polygon([(u * s, v) for u, v in [(-1, 0.0), (-0.6, -0.6), (0.2, -0.85), (0.85, -0.45), (1, 0.15),
                                                     (0.55, 0.75), (-0.2, 0.85), (-0.75, 0.5)]])
            col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
            k.decal(E * np.array([s, 1, 1]), unit(0.5 * s, -0.85, 0.1), (0.024, 0.018), col, up=(0, 0.1, 1),
                    eps=0.004, outline=shape, rings=20, segs=56)

    k.build(voxel=0.0045, smooth=4, band=0.006)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    mienshao(*args())
