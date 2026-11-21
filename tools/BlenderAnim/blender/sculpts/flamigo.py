# Flamigo sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/flamigo.py -- <the export's Flamigo.glb> <out.glb>
# From the references: a pink flamingo standing on one pale leg, the other bent with its foot tucked forward across the
# standing leg; a big round pink body with deeper pink wings folded on its sides, scalloped at their backs; a long pink
# neck that leaves the body's front in a knot (one fused mass), rises up the back, arches over and hangs the head
# forward; a white patch on each side of the face round a big yellow eye with a black pupil and rim; a thick beak white
# at its root, bent down and black over its tip. The legs are thicker than the pictures' wires so that they read.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, unit, eye_look, args, sibling

r = sibling("rookidee")
on_ell = r.on_ell

PINK, DEEP, WHITE, BLACK, LEG = "#EC6E8E", "#DE6585", "#F6F4F4", "#26232A", "#DCD8E0"
YELLOW, PUPIL = "#F2C83A", "#17151A"


def frame():
    F = SimpleNamespace()
    F.H = np.array([0, -0.15, 0.935])
    F.HR = np.array([0.056, 0.074, 0.062])
    F.KNOT = np.array([0, -0.125, 0.585])
    F.EYEDIR = unit(0.74, -0.42, 0.34)
    F.EYE = (0.028, 0.028)
    F.VOXEL = 0.0075
    return F


def body(k, F):
    k.add("body", ell((0, 0.03, 0.575), (0.13, 0.158, 0.135), rot=(6, 0, 0)), PINK, "body")
    k.add("wing", ell((0.1, 0.055, 0.59), (0.055, 0.13, 0.1), rot=(-10, 0, 0)), DEEP, "wing{s}", mirror=True)
    for j, (dy, dz) in enumerate(((0.0, 0.03), (0.02, -0.02), (-0.015, -0.06))):
        k.add(f"scallop{j}", ell((0.1, 0.17 + dy, 0.6 + dz), (0.05, 0.045, 0.034), rot=(0, 0, 0)), DEEP, "wing{s}",
              mirror=True)
    k.add("tail", ell((0, 0.2, 0.6), (0.05, 0.05, 0.03), rot=(-15, 0, 0)), DEEP, "tail")


def neck(k, F):
    """Out of the body's front in a knot, up the back, over the top and down to the head."""
    k.add("neck", tube([F.KNOT, (0, -0.05, 0.665), (0, 0.04, 0.765), (0, 0.055, 0.88), (0, 0.005, 0.965),
                        (0, -0.085, 0.965), (0, -0.13, 0.945)],
                       [0.04, 0.036, 0.035, 0.035, 0.035, 0.036, 0.038]), PINK, "head")
    k.add("knot", ell(F.KNOT + np.array([0, -0.01, 0]), (0.062, 0.05, 0.056)), PINK, "head")
    # the loop round the knot
    n = unit(0.6, -0.45, 0.66)
    a = unit(*np.cross(n, (0, 0, 1)))
    b = np.cross(n, a)
    pts = [F.KNOT + np.array([0, -0.012, 0]) + 0.05 * (math.cos(t) * a + math.sin(t) * b)
           for t in np.linspace(0, 2 * math.pi, 13)]
    k.add("loop", tube(pts, [0.026] * 13), PINK, "head")
    k.add("head", ell(F.H, F.HR), PINK, "head")


def face(k, F):
    """The white patch round each eye (in every state)."""
    E = on_ell(F.H, F.HR, F.EYEDIR)
    k.decal(E + np.array([0, -0.008, -0.004]), F.EYEDIR, (0.04, 0.034), WHITE, eps=0.002, rings=16, segs=48,
            mirror=True)


def beak(k, F):
    pts = [(0, -0.2, 0.94), (0, -0.245, 0.93), (0, -0.272, 0.9), (0, -0.276, 0.86)]
    k.add("beak", blade(pts, [0.032, 0.026, 0.02, 0.012], (1, 0, 0), 0.78), WHITE, "head", fuse=False,
          surface="shell")
    tip = [(0, -0.256, 0.922), (0, -0.276, 0.898), (0, -0.281, 0.862), (0, -0.276, 0.835)]
    k.add("beaktip", blade(tip, [0.024, 0.023, 0.018, 0.01], (1, 0, 0), 0.8), BLACK, "head", fuse=False,
          surface="shell")


def foot(k, ank, fwd, bone, t=0.007):
    side = unit(*np.cross(fwd, (0, 0, 1)))
    pts = []
    for p in (ank - fwd * 0.012, ank + side * 0.012, ank - side * 0.012,
              ank + fwd * 0.06 + side * 0.03, ank + fwd * 0.066, ank + fwd * 0.06 - side * 0.03):
        pts += [p + np.array([0, 0, t / 2]), p - np.array([0, 0, t / 2])]
    pts.append(ank + np.array([0, 0, 0.012]))
    k.add("foot", hull(pts), LEG, bone, fuse=False, surface="scales")


def legs(k, F):
    # the standing leg
    k.add("legR", tube([(0.025, 0.0, 0.52), (0.027, 0.006, 0.4), (0.029, 0.012, 0.275), (0.03, 0.004, 0.15),
                        (0.03, 0.0, 0.03)], [0.018, 0.017, 0.02, 0.016, 0.015]), LEG, "legR", fuse=False,
          surface="scales")
    foot(k, np.array([0.03, 0.0, 0.0045]), unit(0.1, -1, 0), "legR")
    # the tucked leg: down to the heel, forward across the standing leg, the foot hanging
    k.add("legL", tube([(-0.025, 0.0, 0.52), (-0.03, 0.008, 0.4), (-0.03, 0.012, 0.31), (-0.012, -0.05, 0.325),
                        (0.0, -0.115, 0.34)], [0.018, 0.017, 0.02, 0.016, 0.015]), LEG, "legL", fuse=False,
          surface="scales")
    ank = np.array([0.0, -0.118, 0.34])
    k.add("tuckedfoot", hull([ank + np.array(p) for p in ((0.012, 0.0, 0.006), (-0.012, 0.0, 0.006),
                                                           (0.016, -0.012, -0.05), (-0.016, -0.012, -0.05),
                                                           (0.0, -0.02, -0.056), (0.0, 0.008, -0.04))]),
          LEG, "legL", fuse=False, surface="scales")


def eye_open(u, v):
    c = np.tile(srgb(YELLOW), (len(u), 1))
    q = np.hypot(u, v)
    c[q > 0.8] = srgb(PUPIL)
    c[np.hypot(u + 0.06, v) < 0.36] = srgb(PUPIL)
    c[np.hypot(u + 0.2, v - 0.18) < 0.12] = srgb("#FFFFFF")
    return c


def eyes(k, F):
    E = on_ell(F.H, F.HR, F.EYEDIR)
    def lay(state):
        k.decal(E, F.EYEDIR, F.EYE, eye_look(state, eye_open, srgb(WHITE), srgb(PUPIL), line=0.16), eps=0.004,
                rings=20, segs=56, mirror=True)
    return lay


def flamigo(src, out_path):
    F = frame()
    k = Kit(src, coat="fur")
    body(k, F)
    neck(k, F)
    face(k, F)
    beak(k, F)
    legs(k, F)
    k.build(voxel=F.VOXEL, band=0.006, wband=0.05)
    k.eye_states(eyes(k, F))
    k.export(out_path)


if __name__ == "__main__":
    flamigo(*args())
