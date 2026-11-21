# Mega Dragalge sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dragalgemega.py -- <the export's Dragalge-Mega.glb> <out.glb>
#
# From the references (10299): Dragalge under a great cape. The largest masses, in order: a cape of red-pink kelp
# thrown over the head and streaming back and down to the model's left in long tongues, purple at their ends; a
# long purple ribbon of a fin hanging down on the other side; the body, now deep purple, an S with a red chest, the
# back brown and set with branching brown twigs; the head under the cape's front, brown over its top, the long
# snout and the small red eye; two olive stalks rising from the crown, each holding up a purple-red orb, curling
# olive tendrils round them; thin brown branching arms and leaves as before.
#
# The cape is laid in the plane the game's cameras look across (its face toward the back, tilted up), as the
# export's arch is, so both cameras see it broad; the picture's cape is thrown to one side, so is this one.
#
# Grown from dragalge.py (sibling): its head, snout, eye, twig, back spikes, arms and leaf plates, moved up by
# what the form's bones moved up (0.007).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, slab, hull, sibling, sheet, outward

dg = sibling("dragalge")
sk = dg.s

PURPLE, LINE, CHEST = "#8C4CC2", "#5E2E8E", "#C23A6E"
CAPE, CAPE_END, RIBBON = "#E0546A", "#7E3CB4", "#7A40C0"
OLIVE, ORB, ORB_HI = "#8E9A42", "#8A2C78", "#C870B8"
UP = np.array([0, 0, 0.007])

# the base's frame moved up with the form's bones, and its lilac made the Mega's purple
dg.HEAD_C = dg.HEAD_C + UP
dg.SNOUT = [tuple(np.array(p) + UP) for p in dg.SNOUT]
dg.SPINE = [tuple(np.array(p) + UP) for p in dg.SPINE]
dg.LILAC = PURPLE
HEAD_C, HEAD_R = dg.HEAD_C, dg.HEAD_R


def curve(pts, u):
    """A point and tangent at u (0..1) along a polyline smoothed by Catmull-Rom (as tube does)."""
    pts = np.array(pts, float)
    n = len(pts) - 1
    ext = np.vstack([2 * pts[0] - pts[1], pts, 2 * pts[-1] - pts[-2]])
    x = np.clip(u, 0, 1) * n
    i = np.minimum(x.astype(int), n - 1)
    t = (x - i)[..., None]
    p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
    P = 0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3)
    T = 0.5 * ((-p0 + p2) + 2 * (2 * p0 - 5 * p1 + 4 * p2 - p3) * t + 3 * (-p0 + 3 * p1 - 3 * p2 + p3) * t * t)
    return P, T / np.linalg.norm(T, axis=-1, keepdims=True)


def cloth(path, face, w0, w1, tongues=3, cut=0.3, ripple=0.018, u0=0.0, wide=1.0):
    """A broad sheet along a path, its face toward `face`, widening from w0 to w1, its far edge cut into tongues."""
    N = unit(*face)
    def at(u, v):
        L = 1 - cut * np.abs(np.sin(math.pi * tongues * v)) ** 0.7
        uu = L * (u0 + (1 - u0) * u)
        P, T = curve(path, uu)
        W = np.cross(N, T); W /= np.linalg.norm(W, axis=-1, keepdims=True)
        w = (w0 + (w1 - w0) * uu ** 0.8) * wide
        return P + W * ((v - 0.5) * w)[..., None] + N * (ripple * np.sin(2 * math.pi * 2 * v) * uu)[..., None]
    return at


CAPE_PATH = [(0.0, -0.1, 0.6), (-0.07, 0.0, 0.7), (-0.19, 0.08, 0.69), (-0.3, 0.12, 0.57), (-0.37, 0.13, 0.41),
             (-0.39, 0.12, 0.25)]
CAPE_FACE = (-0.15, 1.0, 0.45)
RIBBON_PATH = [(0.03, -0.02, 0.6), (0.12, 0.07, 0.58), (0.2, 0.12, 0.47), (0.22, 0.14, 0.32), (0.19, 0.14, 0.17)]
RIBBON_FACE = (0.25, 1.0, 0.2)


def cape(k):
    f = cloth(CAPE_PATH, CAPE_FACE, 0.09, 0.27)
    k.add("cape", outward(sheet(f, 46, 34, 0.018)), CAPE, "capeL", fuse=False, surface="leaf")
    # the tongues' purple ends: a thicker sheet over the far fifth, so the purple shows on both faces
    g = cloth(CAPE_PATH, CAPE_FACE, 0.09, 0.27, u0=0.8, wide=1.012)
    k.add("capeend", outward(sheet(g, 14, 34, 0.026)), CAPE_END, "capeL", fuse=False, surface="leaf")
    # the hood of the cape over the head's top, fused, so the cape grows out of the head
    k.add("hood", ell(HEAD_C + (0.0, 0.01, 0.05), (0.06, 0.07, 0.035)), CAPE, "head")
    r = cloth(RIBBON_PATH, RIBBON_FACE, 0.05, 0.1, tongues=1, cut=0.25, ripple=0.01)
    k.add("ribbon", outward(sheet(r, 36, 14, 0.02)), RIBBON, "capeR", fuse=False, surface="leaf")


def antlers(k):
    top = HEAD_C + (0, 0.0, HEAD_R[2] * 0.9 + 0.035)
    for i, sx in enumerate((1, -1)):
        a = top + np.array([0.03 * sx, 0.0, 0.0])
        stalk = [a, a + (0.008 * sx, -0.004, 0.05), a + (0.014 * sx, -0.006, 0.1)]
        k.add(f"stalk{i}", tube(stalk, [0.014, 0.012, 0.011], seg=12, per=6), OLIVE, "head")
        orb = np.array(stalk[-1]) + (0, 0, 0.018)
        k.add(f"orb{i}", ell(orb, (0.022, 0.022, 0.026)), ORB, "head", fuse=False)
        k.add(f"orbhi{i}", ell(orb + (0.006 * sx, -0.012, 0.009), (0.007, 0.006, 0.008)), ORB_HI, "head", fuse=False)
        # olive tendrils curling out and back down round the orb
        for j, (dx, dy) in enumerate(((1, 0.2), (-0.6, 0.5))):
            b = np.array(stalk[1])
            o = np.array([dx * sx, dy, 0.0])
            pts = [b, b + o * 0.03 + (0, 0, 0.04), b + o * 0.06 + (0, 0, 0.075), b + o * 0.085 + (0, 0, 0.07),
                   b + o * 0.08 + (0, 0, 0.045)]
            k.add(f"tendril{i}{j}", tube(pts, [0.012, 0.011, 0.01, 0.009, 0.008], seg=12, per=6), OLIVE, "head")


def chest(k):
    """The chest red down its front: the forward side of the body's S between the arms and the hip."""
    sp = dg.SPINE
    def m(P):
        z = P[:, 2]
        front = inside(P[:, 1] - (sk.spine_y(z, sp) - 0.025), 0.006)
        return front * inside(z - 0.47, 0.01) * inside(0.29 - z, 0.01)
    k.paint(m, CHEST)


def twigs(k):
    """Branching brown twigs off the body's sides, as the picture's seaweed sprigs."""
    for i, (z, sx, out_) in enumerate(((0.42, 1, 0.08), (0.34, -1, 0.09), (0.27, 1, 0.08), (0.2, -1, 0.07))):
        y = sk.spine_y(z, dg.SPINE)
        a = np.array([0.03 * sx, y, z])
        pts = [a, a + (out_ * 0.5 * sx, 0.01, 0.02), a + (out_ * sx, 0.02, 0.05)]
        k.add(f"sprig{i}", tube(pts, [0.013, 0.011, 0.009], seg=10, per=5), dg.BROWN, "body")
        dg.plate(k, f"sprigleaf{i}", pts[-1], (0.6 * sx, 0.2, 0.7), (0, 1, 0), dg.lobed(0.06, 0.022, lobes=2), 0.02,
                 dg.BROWN, "body")


def dragalge_mega(src, out):
    k = Kit(src, coat="scales")
    dg.head(k)
    cape(k)
    antlers(k)
    dg.twig(k)
    k.add("neck", tube(dg.SPINE, dg.SPINE_R, seg=18, per=6), PURPLE, "body")
    # (the base's brown back is gone: the pictures' Mega is purple all round under its twigs)
    chest(k)
    sk.belly_lines(k, rows=(0.3, 0.34, 0.38, 0.42), half=(0.04, 0.005), color=LINE, spine=dg.SPINE,
                   radii=dg.SPINE_R)
    dg.back_spikes(k)
    dg.arms(k)
    dg.fins(k)
    twigs(k)
    k.build(voxel=0.0062, smooth=3, band=0.006, wband=0.05)
    k.eye_states(sk.eyes(k, look=dg.dragalge_eye, centre=HEAD_C + dg.EYE_N * HEAD_R * 0.96 + (0, 0, 0.004),
                         n=dg.EYE_N, half=(0.026, 0.024), top=dg.TOP, lid=srgb(PURPLE), ink=dg.CK))
    k.export(out)


if __name__ == "__main__":
    dragalge_mega(*args())
