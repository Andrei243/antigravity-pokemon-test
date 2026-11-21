# Kilowattrel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/kilowattrel.py -- <the export's Kilowattrel.glb> <out.glb>
# Wattrel grown (its colours, its eyes, its beak, its tail and its jagged edge, imported from wattrel.py): from the
# references, a frigatebird in the air; a great yellow throat pouch swelling under a small charcoal head, the design's
# largest mass on the body, the charcoal of the back meeting it in a jagged edge down the neck; a slate belly behind
# it; a long dark red hooked beak; a small pale eye with a yellow streak over it; long pointed wings raised in a V,
# yellow along their inner front and charcoal behind with a jagged edge between, shortened so that the model fills its
# cell; a long forked charcoal tail; dark red feet tucked back under the tail.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, unit, args, sibling

w = sibling("wattrel")
DARK, YELLOW = w.DARK, w.YELLOW
RED, SLATE, PALE = "#962A33", "#3C435C", "#E6EEEC"


def frame():
    F = w.frame()
    F.H = np.array([0, -0.165, 0.224])
    F.HR = np.array([0.045, 0.054, 0.046])
    F.NECK = ((0, -0.122, 0.2), (0.044, 0.05, 0.046))
    F.BODY = ((0, 0.02, 0.182), (0.058, 0.13, 0.055), (-4, 0, 0))
    F.POUCH = ((0, -0.072, 0.152), (0.07, 0.088, 0.074))
    F.SH = np.array([0.048, -0.03, 0.208])
    F.BEAK = (np.array([0, -0.204, 0.217]), unit(0, -1, -0.3), 0.062, 0.016)
    F.EYEDIR = unit(0.66, -0.66, 0.36)
    F.EYE = (0.0135, 0.0135)
    F.TAILBASE = np.array([0, 0.13, 0.182])
    F.VOXEL = 0.0034
    return F


def body(k, F):
    k.add("body", ell(*F.BODY[:2], rot=F.BODY[2]), DARK, "body")
    k.add("pouch", ell(*F.POUCH), YELLOW, "body")
    k.add("neck", ell(*F.NECK), DARK, "head")
    k.add("head", ell(F.H, F.HR), DARK, "head")
    for s, b in ((1, "wingR"), (-1, "wingL")):
        k.add("shoulder", ell(F.SH * np.array([s, 1, 1]) + np.array([-0.012 * s, 0.005, -0.004]), (0.024, 0.04, 0.02)),
              DARK, b)


def coat(k, F):
    """The charcoal of the back meeting the pouch in a jagged edge down the neck, the slate belly behind it."""
    edge = lambda P: 0.205 - 0.42 * (P[:, 1] + 0.12) - w.jag(P[:, 1], 0.026, 0.02)
    k.paint(lambda P: inside(edge(P) - P[:, 2], 0.0025) * inside(P[:, 1] + 0.135, 0.004), DARK)
    # the pouch's own front stays yellow up to the chin
    k.paint(lambda P: inside(P[:, 1] + 0.12, 0.004) * inside(P[:, 2] - 0.205, 0.004)
            * inside(np.abs(P[:, 0]) - 0.03, 0.004), YELLOW, on=["pouch", "neck"])
    k.paint(lambda P: inside(0.005 - P[:, 1], 0.006) * inside(P[:, 2] - 0.168, 0.004), SLATE, on=["body"])
    # a yellow streak over each eye, on the hood
    de = unit(0.62, -0.62, 0.48)
    k.decal(w.on_ell(F.H, F.HR, de), de, (0.016, 0.006), YELLOW, up=(0.3, 0.7, 0.6), eps=0.002, mirror=True)


# --- the wings, the largest mass of all, shortened ------------------------------------------------------------------
ELEV, SWEEP, L = 30, 14, 0.25
BLACK = [(0.0, -0.024), (0.08, -0.03), (0.16, -0.025), (0.21, -0.012), (0.25, 0.022),
         (0.205, 0.04), (0.195, 0.07), (0.155, 0.055), (0.14, 0.085), (0.1, 0.066), (0.085, 0.096),
         (0.045, 0.072), (0.03, 0.094), (0.0, 0.07)]
GOLD = [(0.0, -0.021), (0.08, -0.027), (0.16, -0.022), (0.205, -0.01), (0.228, 0.006),
        (0.19, 0.012), (0.175, 0.032), (0.145, 0.02), (0.125, 0.046), (0.09, 0.03), (0.07, 0.056),
        (0.035, 0.04), (0.015, 0.058), (0.0, 0.05)]


def wing_axes():
    e, s = math.radians(ELEV), math.radians(SWEEP)
    d = np.array([math.cos(e) * math.cos(s), math.cos(e) * math.sin(s), math.sin(e)])
    c0 = np.array([0, 1.0, -0.8])
    c = c0 - d * (d @ c0); c /= np.linalg.norm(c)
    return d, c


def wings(k, F):
    d, c = wing_axes()
    root = F.SH - d * 0.012
    B = lambda o: [(a, b * 1.6 if b > 0 else b * 1.3) for a, b in o]
    k.add("wing", slab(B(BLACK), root, d, c, 0.011), DARK, "wing{s}", fuse=False, mirror=True)
    k.add("wingyellow", slab(B(GOLD), root - c * 0.001, d, c, 0.016), YELLOW, "wing{s}", fuse=False, mirror=True)


def legs(k, F):
    """Dark red legs tucked back under the tail, the webbed feet trailing."""
    for s, side in ((1, "R"), (-1, "L")):
        top = np.array([0.03 * s, 0.05, 0.14])
        ank = np.array([0.034 * s, 0.098, 0.108])
        k.add(f"leg{side}", tube([top, (top + ank) / 2, ank], [0.009, 0.0085, 0.008]), RED, f"leg{side}",
              fuse=False, surface="scales")
        pts = []
        for p in (ank + np.array([0, -0.004, 0]), ank + np.array([0.018 * s, 0.04, -0.012]),
                  ank + np.array([0.0, 0.046, -0.014]), ank + np.array([-0.014 * s, 0.04, -0.01])):
            pts += [p + np.array([0, 0, 0.004]), p - np.array([0, 0, 0.004])]
        k.add(f"foot{side}", hull(pts), RED, f"leg{side}", fuse=False, surface="scales")


def kilowattrel(src, out_path):
    F = frame()
    k = Kit(src, coat="fur")
    body(k, F)
    coat(k, F)
    wings(k, F)
    w.beak(k, F, colour=RED, hook=0.42)
    w.tail(k, F, [(9, -8, 0.17, 0.032, 38, DARK), (-9, -8, 0.17, 0.032, -38, DARK), (0, -14, 0.11, 0.04, 0, DARK)])
    legs(k, F)
    k.build(voxel=F.VOXEL, band=0.004, wband=0.03)
    k.eye_states(w.eyes(k, F, w.round_eye(white=PALE, pr=0, rim_from=0.8), pupil=(0.5, (-0.06, 0.0), None, 0)))
    k.export(out_path)


if __name__ == "__main__":
    kilowattrel(*args())
