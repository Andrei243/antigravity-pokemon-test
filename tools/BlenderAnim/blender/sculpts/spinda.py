# Spinda sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/spinda.py -- <the export's Spinda.glb> <out.glb>
#
# From the references: a cream teddy-like panda, the head nearly as wide as it is tall and some two fifths of the
# height, wider than the body; two long rounded ears in a V, thick paddles with a red tip and a black spiral on their
# front; spiral eyes, black on cream (they are the open state); a red spot over the forehead on its -X side and one
# round the +X eye; a small black nose and a wide open pink smile; red paddle arms carried forward and down; a cream
# body with a red band round the hips; short cream legs and feet. (The spirals are black, as the pictures have them.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, boxy, eye_look, args, sibling

pw = sibling("poliwag")
on_ell = sibling("shroomish").on_ell

CREAM, RED, INK, PINK, LIP = "#F2E4C8", "#E65A5E", "#1A1414", "#EE8E9C", "#8A3442"
H = np.array([0.0, -0.015, 0.36])        # the head's middle (its bone is at 0, -0.01, 0.28)
HR = np.array([0.158, 0.13, 0.134])
EAR_LEN, EAR_W, EAR_T = 0.205, 0.062, 0.031
VOXEL = 0.0055


def ear_frame(s, tilt):
    """An ear's base and its directions: along it, out of its front face, across it (s the side, tilt from up)."""
    t = math.radians(tilt)
    base = np.array([0.062 * s, 0.0, 0.44])
    d = np.array([math.sin(t) * s, 0.06, math.cos(t)])
    d /= np.linalg.norm(d)
    f = np.array([0.0, -1.0, 0.0]); f -= d * (f @ d); f /= np.linalg.norm(f)
    return base, d, f


def spinda(src, out):
    k = Kit(src, coat="fur")

    # ---- the head: wide and round, a flattened face part for the patches
    k.add("head", ell(H, HR, deform=boxy(2.2)), CREAM, "head")
    k.add("face", ell(H + (0, -0.06, -0.02), (0.13, 0.08, 0.1)), CREAM, "head")

    # ---- the ears: thick rounded paddles in a V, a red tip
    EARS = {}
    for s, side, tilt in ((1, "R", 30), (-1, "L", 16)):
        base, d, f = ear_frame(s, tilt)
        c = base + d * EAR_LEN * 0.48
        rot = (math.degrees(math.asin(-d[1])), tilt * s, 0)
        k.add(f"ear{side}", ell(c, (EAR_W, EAR_T, EAR_LEN * 0.5), rot=rot), CREAM, f"ear{side}")
        EARS[s] = (base, d, f)
        k.paint(lambda P, b=base, d=d: inside(EAR_LEN * 0.74 - (P - b) @ d, 0.003), RED, on=[f"ear{side}"])

    # ---- the body: a round pear under the head, a red band round the hips; short legs, cream feet
    def pear(V):
        V = V.copy()
        s = 1 + 0.08 * np.clip(-V[:, 2], 0, 1) - 0.12 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell((0, 0.0, 0.16), (0.1, 0.09, 0.112), deform=pear), CREAM, "body")
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.058, 0.0, 0.09)) * m, (0.05, 0.055, 0.058)), CREAM, f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.062, -0.018, 0.028)) * m, (0.044, 0.058, 0.03)), CREAM, f"leg{side}")
    k.paint(lambda P: inside(P[:, 2] - (0.138 + 0.006 * np.cos(np.arctan2(P[:, 0], -P[:, 1]) * 3)), 0.003)
            * inside(0.058 - P[:, 2], 0.003), RED, on=["body", "thigh"])

    # ---- the arms: red paddles, forward and a little down
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        pts = np.array([(0.065, -0.012, 0.24), (0.105, -0.06, 0.205), (0.12, -0.1, 0.17)]) * m
        k.add(f"arm{side}", blade(pts, [0.036, 0.032, 0.03], (0, 0, 1), 0.62), RED, f"arm{side}")

    # ---- the spots: the forehead's on the -X side, and a round one taking in the +X eye
    FS = on_ell(H, HR, (-0.42, -0.62, 0.66))[0]
    k.paint(lambda P: inside(np.linalg.norm(P - FS, axis=1) - 0.06, 0.003) * (P[:, 2] > 0.3), RED, on=["head", "face"])
    ER, en_r = on_ell(H + (0, -0.06, -0.02), (0.13, 0.08, 0.1), (0.6, -0.8, 0.12))
    SR = ER + np.array([0.012, 0.0, 0.008])
    k.paint(lambda P: inside(np.linalg.norm(P - SR, axis=1) - 0.066, 0.003) * (P[:, 2] > 0.25), RED,
            on=["head", "face"])

    # ---- the ears' spirals, black on the cream of their fronts
    for s in (1, -1):
        base, d, f = EARS[s]
        c = base + d * EAR_LEN * 0.42
        k.decal(c, f, (0.046, 0.046), pw.spiral_look(1.5, 0.22, s, start=0.08, end=0.86, ground=CREAM, ink=INK),
                up=tuple(d), eps=0.004, rings=40, segs=140)

    # ---- the nose and the wide pink smile
    NC, nn = on_ell(H + (0, -0.06, -0.02), (0.13, 0.08, 0.1), (0, -1, -0.22))
    k.decal(NC, nn, (0.015, 0.011), INK, eps=0.004, rings=8, segs=32)
    MC, mn = on_ell(H + (0, -0.06, -0.02), (0.13, 0.08, 0.1), (0, -1, -0.62))
    CP, CL = srgb(PINK), srgb(LIP)
    def smile(u, v):
        c = np.tile(CP, (len(u), 1))
        r = np.sqrt(u * u + v * v)
        c[(r > 0.78) | (v > 0.32)] = CL
        return c
    def lens(th):
        s = math.sin(th)
        return min(1.0, 0.5 / s) if s > 0 else 1.0
    k.decal(MC, mn, (0.054, 0.025), smile, eps=0.004, outline=lens, rings=12, segs=56)

    k.build(voxel=VOXEL, smooth=4, band=0.004)

    # ---- the spiral eyes: each on its own ground (the +X one in its red spot), laid by hand
    CC, CR, CK = srgb(CREAM), srgb(RED), srgb(INK)
    EL, en_l = on_ell(H + (0, -0.06, -0.02), (0.13, 0.08, 0.1), (-0.6, -0.8, 0.12))
    right = pw.spiral_look(1.6, 0.22, 1, start=0.08, end=0.88, ground=RED, ink=INK)
    left = pw.spiral_look(1.6, 0.22, 1, start=0.08, end=0.88, ground=CREAM, ink=INK)
    def lay(state):
        fine = state == "Open"
        rr, ss = (40, 140) if fine else (20, 56)
        k.decal(ER, en_r, (0.046, 0.046), eye_look(state, right, CR, CK, line=0.2), eps=0.004, rings=rr, segs=ss)
        look_l = eye_look(state, left, CC, CK, line=0.2)
        k.decal(EL, en_l, (0.046, 0.046), lambda u, v, f=look_l: f(-u, v), eps=0.004, rings=rr, segs=ss)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    spinda(*args())
