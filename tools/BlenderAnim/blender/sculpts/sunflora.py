# Sunflora sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sunflora.py -- <the export's Sunflora.glb> <out.glb>
#
# Grown from Sunkern's script (sibling("sunkern")): the line's yellow and greens, its leaf, its ink. From the
# references: a sunflower for a head, a pale cream face (two curved seams down it, what is left of the seed's stripes)
# ringed by two rows of yellow petals, rounded with a pointed tip, the front row between the back row's; happy eyes
# shut in arcs curving up, a big open mouth flat along its top and round below, pink inside; a short green stem of a
# neck down to a green body like a drop, wider below; two leaves for arms held out to the sides, forward and a little
# down, their tips lifting; short legs and long flat feet turned out.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, unit, eye_look, inside, args, sibling

sk = sibling("sunkern")

FACE, SEAM = "#F6E8A2", "#C8AE60"
PETAL_OUT, PETAL_IN = "#F2B21E", "#FAC838"
GREEN = "#7CC468"                          # the body: a lighter leaf green than the leaves
CALYX = sk.LEAF
MOUTH_RIM, MOUTH_IN, TONGUE = srgb("#7A2030"), srgb("#E06C82"), srgb("#F4A6B2")
INK = sk.INK

# --- the frame ----------------------------------------------------------------------------------------------------
FC = np.array([0.0, -0.01, 0.43])        # the face's middle
FR = np.array([0.105, 0.066, 0.105])
BODY = np.array([0.0, 0.0, 0.152])
BODY_R = np.array([0.074, 0.066, 0.088])


def on_ell(c, r, d):
    d = unit(*d)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def drop(V):
    """The unit sphere made the body: narrowing toward its top."""
    V = V.copy()
    s = 1 - 0.42 * np.clip(V[:, 2], 0, 1) ** 1.3
    V[:, 0] *= s; V[:, 1] *= s
    return V


def petal_outline(L, w):
    return [(0.0, -0.6 * w), (0.18 * L, -0.92 * w), (0.42 * L, -w), (0.66 * L, -0.86 * w), (0.84 * L, -0.55 * w),
            (L, 0.0), (0.84 * L, 0.55 * w), (0.66 * L, 0.86 * w), (0.42 * L, w), (0.18 * L, 0.92 * w), (0.0, 0.6 * w)]


def petal_ring(k, name, n, phase, root, L, w, y, cup, colour, thick):
    for i in range(n):
        a = math.radians(phase + 360 * i / n)
        radial = np.array([math.sin(a), 0.0, math.cos(a)])
        tangent = np.array([math.cos(a), 0.0, -math.sin(a)])
        c = math.radians(cup)
        d = radial * math.cos(c) + np.array([0, -1.0, 0]) * math.sin(c)
        origin = np.array([FC[0], y, FC[2]]) + radial * root
        k.add(f"{name}{i}", slab(petal_outline(L, w), origin, d, tangent, thick), colour, "head", fuse=False,
              surface="leaf")


def flower(k):
    k.add("face", ell(FC, FR), FACE, "head")
    # the green back of the flower, where the stem joins
    k.add("calyx", ell((0, 0.044, 0.43), (0.095, 0.028, 0.095)), CALYX, "head", fuse=False, surface="leaf")
    petal_ring(k, "petalBack", 10, 0, 0.074, 0.106, 0.045, 0.014, 12, PETAL_OUT, 0.024)
    petal_ring(k, "petalFront", 10, 18, 0.07, 0.09, 0.04, -0.01, 22, PETAL_IN, 0.024)
    # the seams down the face, ( and ), on its front alone
    def seam(P):
        t = (P[:, 2] - FC[2]) / FR[2]
        x = np.sign(P[:, 0]) * (0.074 - 0.03 * t * t)
        front = inside(P[:, 1] + 0.012, 0.002)
        return inside(np.abs(P[:, 0] - x) - 0.0032, 0.0015) * front * inside(np.abs(t) - 0.88, 0.02)
    k.paint(seam, SEAM)


def body(k):
    k.add("body", ell(BODY, BODY_R, deform=drop), GREEN, "body")
    k.add("neck", tube([(0, 0.006, 0.21), (0, 0.016, 0.3), (0, 0.03, 0.37)], [0.03, 0.026, 0.026]), GREEN, "head")
    k.add("leg", tube([(0.03, 0.0, 0.1), (0.044, -0.004, 0.05), (0.056, -0.012, 0.022)], [0.02, 0.018, 0.017]),
          GREEN, "leg{s}", mirror=True)
    k.add("foot", ell((0.088, -0.03, 0.014), (0.054, 0.022, 0.014), rot=(0, 0, -32)), GREEN, "leg{s}", mirror=True)
    # the leaves for arms: out to the sides, forward and a little down, their tips lifting
    for s, nm in ((1, "R"), (-1, "L")):
        sk.add_leaf(k, f"arm{nm}", (0.052 * s, -0.004, 0.236), (s, -0.28, -0.1), (0, -0.9, 0.6), 0.2, 0.05,
                    f"arm{nm}", droop=-0.14, fold=0.2, thick=0.02)


def mouth(k):
    p, n = on_ell(FC, FR, (0, -1, -0.32))
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(MOUTH_IN, (len(u), 1))
        c[(u / 0.62) ** 2 + ((v + 0.68) / 0.46) ** 2 < 1] = TONGUE
        c[v > 0.3] = MOUTH_RIM
        return c
    def shape(th):
        s = math.sin(th)
        return min(1.0, 0.36 / s) if s > 0 else 1.0
    k.decal(p, n, (0.046, 0.044), look, eps=0.004, outline=shape, rings=16, segs=56)


def happy(u, v):
    """Always shut, happily: an arc curving up on the face."""
    c = np.tile(srgb(FACE), (len(u), 1))
    c[(np.abs(v - (-0.42 + 0.8 * (1 - u * u))) < 0.2) & (np.abs(u) < 0.86)] = INK
    return c


def happy_fierce(u, v):
    """Fierce on a shut eye: a straight line tilted down toward the inner corner."""
    c = np.tile(srgb(FACE), (len(u), 1))
    c[(np.abs(v - (0.0 + 0.45 * u)) < 0.18) & (np.abs(u) < 0.86)] = INK
    return c


def sunflora(src, out):
    k = Kit(src, coat="leaf")
    flower(k)
    body(k)
    mouth(k)
    k.build(voxel=0.005, smooth=4, band=0.004)

    p, n = on_ell(FC, FR, (0.4, -0.95, 0.3))
    def eyes(state):
        look = happy_fierce if state == "Fierce" else eye_look(state, happy, srgb(FACE), INK, line=0.17)
        k.decal(p, n, (0.029, 0.019), look, eps=0.005, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    sunflora(*args())
