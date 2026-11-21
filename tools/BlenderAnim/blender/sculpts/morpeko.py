# Morpeko (Full Belly Mode) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/morpeko.py -- <the export's Morpeko.glb> <out.glb>
#
# The design, from the references: a little hamster round as a bean, its head and body one mass, pale yellow in the
# middle. Like a hood split down its middle, the -X side is ochre brown and the +X side dark grey, from the crown down
# over the sides to below the arms, each edge on the front a zigzag, so the yellow is a V narrow at the crown and wide
# at the belly. Two leaf-shaped ears standing up and a little out, each with a small second lobe out to the side at its
# root: brown on -X, grey on +X. Black oval eyes set wide, each with a white glint; round pink cheeks; a tiny dark nose;
# a wide open mouth, dark red with a pink tongue and one white tooth at the top. The -X arm, brown, hangs at its side;
# the +X arm, grey, holds a pale green seed (the art holds it up beside the head, carried here in front of the chest).
# Pale yellow oval feet; a thin brown tail behind.
# The pictures agree with the brief (the face is cream-yellow, the same as the belly).
# Skeleton: root, body (0, 0, 0.14), head (0, 0, 0.23), armL/armR (+-0.06, -0.03, 0.15), tail (0, 0.04, 0.08),
# legL/legR (+-0.035, -0.01, 0.04).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, eye_look, args, outward, inside, smoothstep, tri

# --- colours ----------------------------------------------------------------------------------------------------------
YELLOW, BROWN, GREY = "#F2E393", "#B98A52", "#505057"
CHEEK, MOUTH, TONGUE, TOOTH, INK = "#F1A1A8", "#9E2E36", "#EE8890", "#FFFFFF", "#18161A"
SEED, SEED_D = "#CFE3AE", "#8DA866"

# --- the frame --------------------------------------------------------------------------------------------------------
C, R = np.array([0.0, 0.0, 0.142]), np.array([0.096, 0.082, 0.13])


def bean(V):
    V = V.copy()
    V[:, :2] *= (1 + 0.05 * np.clip(-V[:, 2], 0, 1) - 0.06 * np.clip(V[:, 2], 0, 1) ** 2)[:, None]
    V[:, 2] = np.where(V[:, 2] < -0.85, -0.85 + (V[:, 2] + 0.85) * 0.6, V[:, 2])
    return V


def on_body(x, z, front=True):
    """A point on the bean's front at x, z and the outward normal there (the bean taken as its ellipsoid)."""
    d = 1 - (x / R[0]) ** 2 - ((z - C[2]) / R[2]) ** 2
    y = C[1] - R[1] * math.sqrt(max(d, 0.0)) * (1 if front else -1)
    p = np.array([x, y, z])
    g = (p - C) / R ** 2
    return p, g / np.linalg.norm(g)


def hood(s):
    """The -X (s = -1, brown) or +X (s = 1, grey) half of the split hood: zigzag edges down the front."""
    def mask(P):
        x, y, z = P[:, 0] * s, P[:, 1], P[:, 2]
        edge = (0.016 + 0.07 * np.clip((0.27 - z) / 0.17, 0, 1.2) + 0.026 * np.exp(-((z - 0.206) / 0.026) ** 2)
                + 0.011 * tri(z / 0.042 + (0.25 if s > 0 else 0)))
        front = smoothstep(-0.02, 0.02, -y)
        xb = edge * front + (-0.002) * (1 - front)      # behind: split down the middle
        low = 0.085 + 0.012 * tri(np.arctan2(y, x) * 1.4)
        return inside(xb - x, 0.0015) * inside(low - z, 0.002)
    return mask


def ears(k):
    def leaf(V):
        V = V.copy()
        t = (V[:, 2] + 1) / 2
        V[:, 0] *= 1.25 - 0.95 * t ** 1.6
        return V
    for s, side, col in ((-1, "L", BROWN), (1, "R", GREY)):
        k.add(f"ear{side}", ell((s * 0.062, 0.006, 0.3), (0.031, 0.013, 0.05), rot=(0, -14 * s, 0), deform=leaf),
              col, "head")
        k.add(f"earlobe{side}", ell((s * 0.088, 0.006, 0.262), (0.022, 0.011, 0.014), rot=(0, 30 * s, 0)), col, "head")


def body(k):
    k.add("body", ell(C, R, deform=bean), YELLOW, "body")
    k.add("top", ell(C + (0, 0.0, 0.07), (0.083, 0.072, 0.07)), YELLOW, "head")
    k.paint(hood(-1), BROWN, on=["body", "top", "ear"])
    k.paint(hood(1), GREY, on=["body", "top", "ear"])
    ears(k)
    # feet
    k.add("foot", ell((0.045, -0.034, 0.016), (0.027, 0.036, 0.015)), YELLOW, "leg{s}", mirror=True)
    # -X: the brown arm hanging at its side; +X: the grey arm holding the seed before the chest
    k.add("armL", tube([(-0.074, -0.03, 0.15), (-0.093, -0.048, 0.112), (-0.088, -0.062, 0.088)],
                       [0.02, 0.018, 0.017]), BROWN, "armL")
    k.add("armR", tube([(0.074, -0.03, 0.148), (0.096, -0.066, 0.122), (0.079, -0.096, 0.112)],
                       [0.02, 0.018, 0.018]), GREY, "armR")
    k.add("seed", ell((0.072, -0.112, 0.124), (0.021, 0.02, 0.021), sub=3), SEED, "armR", fuse=False)
    k.add("seedband", ell((0.072, -0.112, 0.124), (0.0215, 0.0205, 0.006), rot=(0, 0, 0), sub=3), SEED_D, "armR",
          fuse=False)
    # a thin brown tail behind
    k.add("tail", outward(tube([(0.0, 0.07, 0.07), (0.03, 0.105, 0.06), (0.05, 0.12, 0.085)], [0.011, 0.01, 0.009],
                               seg=10)), BROWN, "tail", fuse=False)


def face(k):
    for s in (1, -1):
        p, n = on_body(s * 0.071, 0.168)
        k.decal(p, n, (0.021, 0.019), CHEEK, eps=0.0016, rings=10, segs=40)
    p, n = on_body(0.0, 0.204)
    k.decal(p, n, (0.0035, 0.0028), INK, eps=0.0016, rings=5, segs=20)
    CM, CT, CW = srgb(MOUTH), srgb(TONGUE), srgb(TOOTH)
    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[(u / 0.66) ** 2 + ((v + 0.85) / 0.6) ** 2 < 1] = CT
        c[(np.abs(u) < 0.16) & (v > 0.12)] = CW
        return c
    def shape(th):
        s_ = math.sin(th)
        return min(1.0, 0.35 / s_) if s_ > 0 else 1.0
    p, n = on_body(0.0, 0.168)
    k.decal(p, unit(0, -1, -0.12), (0.03, 0.022), mouth, eps=0.0016, outline=shape, rings=22, segs=72)


def eyes(k, state):
    CI, CY = srgb(INK), srgb(YELLOW)
    def eye(u, v):
        c = np.tile(CI, (len(u), 1))
        c[((u + 0.05) / 0.38) ** 2 + ((v - 0.34) / 0.32) ** 2 < 1] = (1, 1, 1)
        return c
    p, n = on_body(0.047, 0.208)
    k.decal(p, n, (0.015, 0.019), eye_look(state, eye, CY, CI), eps=0.0016, rings=20, segs=56, mirror=True)


def morpeko(src, dest):
    k = Kit(src, coat="fur")
    body(k)
    face(k)
    k.build(voxel=0.0034, band=0.004, wband=0.03)
    k.eye_states(lambda state: eyes(k, state))
    k.export(dest)


if __name__ == "__main__":
    morpeko(*args())
