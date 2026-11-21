# Swadloon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/swadloon.py -- <the export's Swadloon.glb> <out.glb>
# Grown from sewaddle.py (its greens, its yellow, its ink and white).
#
# The design, from the references: Sewaddle wrapped up in its leaf: a round bell of a cloak, light green, a little
# broader low down; the yellow face peeping out of its upper front, the cloak's top coming down to a point between the
# eyes and dark green showing inside the cloak's edge at the face's upper corners; grumpy half-shut eyes (white, a
# black pupil low, a heavy grey lid straight across) and a small frown; the cloak's front flaps meeting below the face
# in a seam; a yellow base showing at the bottom; two leaves standing up from the top like ears (one taller), each
# with its midrib; dark green lobed leaves spread flat on the ground round the foot, two each side.
# Skeleton: body (0, 0, 0.14), head (0, -0.04, 0.18), ears (+-0.03, 0, 0.25).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, leaf, slab, tube, srgb, inside, smoothstep, unit, eye_look, polygon, args, outward,
                       sibling)

sw = sibling("sewaddle")
CLOAK, LIGHT, DARK, INNER = "#8CCB42", "#9DD64C", "#3E8E3C", "#2F7432"
YELLOW, BASE, INK, WHITE = sw.YELLOW, "#D9CC66", sw.INK, sw.WHITE
LIDGREY, SEAM = "#4A4A50", "#6AA436"

BC, BR = np.array([0.0, 0.0, 0.136]), np.array([0.128, 0.12, 0.142])     # the cloak
FC, FR = np.array([0.0, -0.07, 0.15]), np.array([0.1, 0.07, 0.085])     # the flattened front the face lies on
FACE_C, FACE_H = np.array([0.0, -0.14, 0.152]), (0.088, 0.074)          # the face patch (centre on the front, halves)


def bell(V):
    """Broader low down, rounded over the top, flat underneath."""
    V = V.copy()
    s = 1 + 0.07 * np.clip(-V[:, 2], 0, 1) - 0.1 * np.clip(V[:, 2], 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.maximum(V[:, 2], -0.93)
    return V


def add_cloak(k):
    k.add("cloak", ell(BC, BR, deform=bell), CLOAK, "body", surface="leaf")
    k.add("front", ell(FC, FR), CLOAK, "head", surface="leaf")

    # the yellow base showing at the bottom, under the cloak's hem
    k.paint(lambda P: inside(P[:, 2] - 0.026 - 0.008 * np.cos(np.arctan2(P[:, 0], -P[:, 1]) * 2), 0.003), BASE)
    # the seam where the front flaps meet below the face, curving a little to one side, and the top leaf's midrib
    def seams(P):
        x0 = 0.012 * np.sin((0.075 - P[:, 2]) * 30)
        low = inside(np.abs(P[:, 0] - x0) - 0.0035, 0.0025) * inside(P[:, 2] - 0.077, 0.003) \
            * inside(0.03 - P[:, 2], 0.003)
        top = inside(np.abs(P[:, 0]) - 0.0035, 0.0025) * inside(0.212 - P[:, 2], 0.003)
        return low * (P[:, 1] < -0.02)
    k.paint(seams, SEAM)


def add_ears(k):
    for s, side, tilt, length in ((1, "R", 0.42, 0.13), (-1, "L", 0.36, 0.15)):
        root = np.array([0.026 * s, 0.004, 0.255])
        d = unit(tilt * s, 0.06, 1.0)
        k.add(f"ear{side}", outward(leaf(root, d, length, 0.042, thick=0.02)), LIGHT, f"ear{side}", fuse=False,
              surface="leaf")
        # the midrib, a darker line up its front face
        a, b = root + d * 0.02 + (0, -0.009, 0), root + d * length * 0.8 + (0, -0.006, 0)
        k.add(f"earrib{side}", tube([a, (a + b) / 2 + (0, -0.002, 0), b], [0.0045, 0.004, 0.0025]), SEAM,
              f"ear{side}", fuse=False, surface="leaf")


def ground_leaf(n=60):
    """A leaf of three round lobes at its far end, narrowing to its root in the cloak."""
    th = np.linspace(0, 2 * math.pi, n, endpoint=False)
    r = 1 + 0.17 * np.cos(6 * th) * np.clip(np.cos(th), 0, 1) ** 0.7 - 0.25 * np.clip(-np.cos(th), 0, 1) ** 2
    return np.stack([0.06 + 0.066 * r * np.cos(th), 0.044 * r * np.sin(th)], 1)


GROUND_LEAF = ground_leaf()


def add_ground_leaves(k):
    for s, side in ((1, "R"), (-1, "L")):
        for j, az in enumerate((-28, 30)):          # one forward of the side, one back
            a = math.radians(az)
            out = np.array([s * math.cos(a), math.sin(a), 0.0])
            ex = out + np.array([0, 0, 0.1])         # rising a little outward, so the cameras see its face
            ez = np.cross(np.array([0, 0, 1.0]), out)
            o = np.array([0, 0, 0.01]) + out * 0.065
            k.add(f"ground{j}{side}", slab(GROUND_LEAF, o, ex, ez, 0.01), DARK, "body", fuse=False, surface="leaf")


FACE = [(1.0, 0.1), (0.95, 0.55), (0.72, 0.9), (0.36, 0.98), (0.0, 0.32), (-0.36, 0.98), (-0.72, 0.9),
        (-0.95, 0.55), (-1.0, 0.1), (-0.9, -0.45), (-0.55, -0.85), (0.0, -1.0), (0.55, -0.85), (0.9, -0.45)]
INSIDE = [(1.06, 0.0), (1.07, 0.6), (0.88, 1.12), (0.42, 1.16), (0.0, 0.32), (-0.42, 1.16), (-0.88, 1.12),
          (-1.07, 0.6), (-1.06, 0.0), (-0.9, -0.45), (-0.55, -0.85), (0.0, -1.0), (0.55, -0.85), (0.9, -0.45)]


def add_face(k):
    k.decal(FACE_C, (0, -1, 0), FACE_H, srgb(INNER), eps=0.003, rings=20, segs=96, outline=polygon(INSIDE))
    k.decal(FACE_C, (0, -1, 0), FACE_H, srgb(YELLOW), eps=0.005, rings=20, segs=96, outline=polygon(FACE))
    # a small frown
    CY, CI = srgb(YELLOW), srgb("#2A2420")
    def frown(u, v):
        c = np.tile(CY, (len(u), 1))
        c[(np.abs(v - (-0.35 + 0.6 * (1 - u * u))) < 0.24) & (np.abs(u) < 0.92)] = CI
        return c
    k.decal(FACE_C + np.array([0, 0.004, -0.044]), unit(0, -1, -0.1), (0.02, 0.011), frown, eps=0.007, rings=16,
            segs=48)


EYE_HALF = (0.024, 0.019)
EYE_SHAPE = [(1.0, 0.0), (0.92, 0.32), (0.6, 0.42), (0.0, 0.44), (-0.6, 0.42), (-0.92, 0.32), (-1.0, 0.0),
             (-0.88, -0.5), (-0.5, -0.86), (0.0, -0.98), (0.5, -0.86), (0.88, -0.5)]


def eye_open(u, v, white=srgb(WHITE), ink=srgb(INK), lid=srgb(LIDGREY)):
    c = np.tile(white, (len(u), 1))
    c[((u + 0.06) / 0.38) ** 2 + ((v + 0.12) / 0.5) ** 2 < 1] = ink
    c[v > 0.02] = lid
    c[(u / 1.0) ** 2 + ((v + 0.06) / 0.98) ** 2 > 0.78] = ink
    return c


def eyes(k, state):
    look = eye_look(state, eye_open, srgb(YELLOW), srgb(INK), line=0.17)
    for s in (1, -1):
        p = np.array([0.05 * s, -0.132, 0.147])
        n = unit(0.34 * s, -1, 0.04)
        col = look if s > 0 else (lambda a, b, f=look: f(-a, b))
        shape = EYE_SHAPE if s > 0 else [(-a, b) for a, b in EYE_SHAPE]
        k.decal(p, n, EYE_HALF, col, eps=0.009, rings=20, segs=56, outline=polygon(shape))


def swadloon(src, out):
    k = Kit(src, coat="leaf")
    add_cloak(k)
    add_ears(k)
    add_ground_leaves(k)
    add_face(k)
    k.build(voxel=0.005, smooth=3)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    swadloon(*args())
