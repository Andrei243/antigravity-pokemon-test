# Nevastuica (Carpathia No. 10052) sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py), grown from Nevastel (sibling("nevastel")): the same weasel at its own scale (its joints are
# Nevastel's times 1.45 exactly), with the bride's veil and wreath laid over it and the +X paw raised to the chin.
#   blender --background --factory-startup --python sculpts/nevastuica.py -- <the export's Nevastuica.glb> <out.glb>
#
# The design, from the project's picture (docs/art/reference/gemini/carpathia/10052-Nevastuica.jpg): Nevastel's body,
# brown back, white throat and belly, dark paws and feet, the S of a tail with its dark brush; over the crown a wreath
# of small wildflowers (red, yellow, blue, white) among green leaves, and from it a cream bridal veil falling behind
# the head over the shoulders, flaring, its hem scalloped and embroidered with a band of red and a line of black thread.
# One forepaw is raised shyly to the chin. The picture's paw raised is the far one; it is the +X paw here, which the
# game's front camera sees.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import ell, tube, sheet, outward, unit, args, sibling

nv = sibling("nevastel")

K = 1.45
VEIL, RED, THREAD, STEM, LEAF = "#F3EBD7", "#C3392F", "#2B2121", "#4F8A3A", "#3E7A32"
FLOWERS = ["#D8403A", "#F2C93A", "#4F86D6", "#F6F2E8", "#D8403A", "#4F86D6", "#F2C93A", "#E86C8A", "#F6F2E8"]

RING_Z, RING_R, RING_Y, TILT = 0.042, 0.054, -0.004, math.radians(10)     # the wreath, in Nevastel's units from H


def frame(n):
    n = unit(*n)
    a = np.cross(n, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a)
    return a, np.cross(n, a), n


def ring_point(H, a, lift=0.0, out=0.0):
    """A point of the wreath's ring at angle a from the front (+ toward +X), tilted down at the front."""
    r = (RING_R + out) * K
    y0, x = -math.cos(a) * r, math.sin(a) * r
    y = y0 * math.cos(TILT)
    z = y0 * math.sin(TILT)                  # the front (y0 below nought) lower
    return H + np.array([x, y + RING_Y * K, z + (RING_Z + lift) * K])


def flower(k, name, at, n, r, colour, bone):
    a, b, n = frame(n)
    def petals(V):
        V = V.copy()
        th = np.arctan2(V[:, 1], V[:, 0])
        s = 1 + 0.32 * np.cos(5 * th)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    V, F = ell((0, 0, 0), (r, r, r * 0.45), deform=petals, sub=3)
    V = V @ np.stack([a, b, n], axis=0) + np.array(at)
    k.add(name, outward((V, F)), colour, bone, fuse=False, surface="leaf")


def bride(k, K_, H, HR, HEAD):
    # The wreath: a green ring round the crown, leaves along it, flowers over its front half
    ring = [ring_point(H, a) for a in np.linspace(0, 2 * math.pi, 25)]
    k.add("wreath", tube(ring, [0.0085 * K] * len(ring), seg=12), STEM, "head", fuse=False, surface="leaf")
    for i in range(14):
        a = 2 * math.pi * (i + 0.5) / 14
        p = ring_point(H, a, lift=0.004, out=0.004)
        o = unit(*(p - H - np.array([0, 0, RING_Z * K])))
        flower(k, f"leaf{i}", p, unit(*(o + np.array([0, 0, 0.8]))), 0.011 * K, LEAF, "head")
    for i, c in enumerate(FLOWERS):
        a = math.radians(-112 + 224 * i / (len(FLOWERS) - 1))
        p = ring_point(H, a, lift=0.008, out=0.006)
        o = unit(*(p - H - np.array([0, 0, RING_Z * K])))
        flower(k, f"flower{i}", p, unit(*(o * 0.8 + np.array([0, 0, 0.9]))), (0.015 + 0.002 * (i % 2)) * K, c, "head")

    # The veil: from the wreath's back, behind and beside the head, down over the shoulders and flaring
    # its top edge is the wreath's ring behind the ears; it falls soft in a few folds, out behind more than aside
    L = 0.255 * K
    span = math.radians(84)
    def at(u, v):
        a = (u - 0.5) * 2 * span                 # 0 straight behind (+Y)
        y0 = math.cos(TILT) * RING_R * K
        ztop = H[2] + RING_Z * K + y0 * math.sin(TILT) - 0.002 * K
        fold = 0.008 * K * np.sin(5.5 * a * 2 + 0.6) * np.clip(v * 1.6, 0, 1)
        r = (RING_R + 0.008) * K + 0.07 * K * np.sqrt(v) + fold
        scallop = 0.05 * np.abs(np.sin(math.pi * 15 * u)) * v ** 8
        z = ztop - L * v * (1 + scallop)
        y = H[1] + RING_Y * K + np.cos(a) * r + 0.045 * K * v
        x = np.sin(a) * r * (1 - 0.18 * v)
        return np.stack([x, y, z], axis=-1)
    # the veil laid over the crown inside the wreath, so the back shows cloth over the head and no open bucket
    k.add("veilcap", ell(H + np.array([0, 0.014, 0.033]) * K, np.array([0.057, 0.058, 0.035]) * K), VEIL, "head",
          fuse=False, surface="cloth")
    k.add("veil", outward(sheet(at, 61, 22, 0.006 * K)), VEIL, "head", fuse=False, surface="cloth")
    def strip(v0, v1):
        return lambda u, w: at(u, v0 + (v1 - v0) * w)
    k.add("veilred", outward(sheet(strip(0.76, 0.86), 61, 3, 0.011 * K)), RED, "head", fuse=False, surface="cloth")
    k.add("veilthread", outward(sheet(strip(0.885, 0.955), 61, 3, 0.011 * K)), THREAD, "head", fuse=False,
          surface="cloth")


if __name__ == "__main__":
    src, out_path = args()
    nv.weasel(src, out_path, K=K, extras=bride, raise_right=True)
