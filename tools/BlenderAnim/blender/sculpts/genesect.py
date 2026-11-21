# Genesect sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/genesect.py -- <the export's Genesect.glb> <out.glb>
#
# From the references: a purple insect of war standing upright, angular and smooth like a machine. The head is a long
# pointed wedge held forward at shoulder height (no saucer), its snout tip pink, a white grin of teeth along each
# side and a great red compound eye behind it with two pale lenses. Light lavender ball joints at the neck, the
# shoulders and the hips; the thorax banded, a pale V plate low on the front. On the back a purple housing carries a
# great pale cannon pointing forward over the head, a dark bore at its mouth and the drive's slot (yellow-orange) on
# its top at the back, dark gaps under it. Thin angular arms: an upper arm to the elbow, then a long flat blade of a
# forearm down and forward to a white claw. Digitigrade legs: block thighs angled out, a pointed knee, shins flaring
# to a white pointed toe and a heel spur. Limbs stout enough to read.
# The frame is in named constants and functions (the drives change DRIVE): build under __main__.
# Bones: body (0, 0, 0.36); head (0, -0.03, 0.48); arms (+-0.08, -0.03, 0.46); legs (+-0.06, 0, 0.26). The export
# stands 0.72 tall.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, lathe, ring_band, outward, srgb, inside, unit, eye_look, polygon, boxy, args)

PURPLE = "#A274CC"
PURPLE_D = "#7E54A8"
JOINT = "#CFB6E6"
PALE = "#ECE0F4"
CLAW = "#F4EEF8"
GAP = "#2C3140"
DRIVE = "#F2A81E"
SNOUT = "#F08A92"
TEETH = "#F6F4F6"
EYE_RED, EYE_RIM, EYE_LENS = "#E2404E", "#8E1A2C", "#F7A8AE"

H = np.array([0.0, -0.11, 0.488])        # the head's centre
HR = np.array([0.078, 0.165, 0.072])
SHOULDER = np.array([0.085, -0.03, 0.455])
ELBOW = np.array([0.165, -0.065, 0.385])
HAND = np.array([0.205, -0.15, 0.225])
HIP = np.array([0.062, 0.0, 0.265])
KNEE = np.array([0.12, -0.055, 0.155])
ANKLE = np.array([0.13, 0.02, 0.035])


def box_along(a, b, w0, t0, w1, t1, up=(0, 0, 1)):
    """A flat-faced limb from a to b: a rectangle w0 by t0 at a growing to w1 by t1 at b (a hull)."""
    a, b = np.array(a, float), np.array(b, float)
    d = b - a; d /= np.linalg.norm(d)
    s = np.cross(d, up); s /= np.linalg.norm(s)
    f = np.cross(s, d)
    pts = []
    for c, w, t in ((a, w0, t0), (b, w1, t1)):
        for sx in (1, -1):
            for sy in (1, -1):
                pts.append(c + s * sx * w / 2 + f * sy * t / 2)
    return hull(pts)


def point_along(a, d, length, w, t, up=(0, 0, 1)):
    """A pointed claw: a kite-section root at a narrowing to a point length along d (a hull)."""
    a = np.array(a, float); d = np.array(d, float); d /= np.linalg.norm(d)
    s = np.cross(d, up); s /= np.linalg.norm(s)
    f = np.cross(s, d)
    return hull([a + s * w / 2, a - s * w / 2, a + f * t / 2, a - f * t / 2, a - d * 0.004, a + d * length])


# --- the body -------------------------------------------------------------------------------------------------------------
def add_body(k):
    k.add("thorax", ell((0, 0.012, 0.405), (0.078, 0.066, 0.078), deform=boxy(3)), PURPLE, "body")
    k.add("waist", ell((0, 0.012, 0.325), (0.052, 0.046, 0.05)), PURPLE, "body")
    k.add("waistring", ring_band((0, 0.012, 0.328), (0, 0, 1), 0.05, 0.022, thick=0.008), JOINT, "body", fuse=False,
          surface="metal")
    k.add("pelvis", ell((0, 0.004, 0.272), (0.07, 0.058, 0.05), deform=boxy(3)), PURPLE, "body")
    k.add("neck", tube([(0, -0.02, 0.43), (0, -0.05, 0.47)], [0.034, 0.032]), PURPLE, "head")
    k.add("neckring", ring_band((0, -0.034, 0.448), unit(0, -0.6, 0.8), 0.033, 0.02, thick=0.008), JOINT, "head",
          fuse=False, surface="metal")
    # Bands across the thorax
    k.paint(lambda P: sum(inside(np.abs(P[:, 2] - z) - 0.004, 0.0025) for z in (0.375, 0.41, 0.445))
            * (P[:, 1] < 0.03), PURPLE_D, on=["thorax"])
    # The pale V plate low on the front (pelvis)
    k.decal((0, -0.054, 0.272), (0, -1, 0), (0.05, 0.038), PALE, eps=0.003,
            outline=polygon([(-1, 0.9), (1, 0.9), (0, -1)]), rings=8, segs=40)


def add_head(k):
    def wedge(V):
        V = boxy(3)(V)
        f = 1 - 0.62 * np.clip(-V[:, 1], 0, 1)
        V[:, 0] *= f; V[:, 2] *= 1 - 0.45 * np.clip(-V[:, 1], 0, 1)
        V[:, 2] = np.where(V[:, 2] > 0, V[:, 2] * 0.85, V[:, 2])
        return V
    k.add("head", ell(H, HR, rot=(-8, 0, 0), deform=wedge), PURPLE, "head")
    k.add("crown", ell(H + (0, 0.08, 0.01), (0.074, 0.07, 0.066), deform=boxy(2.6)), PURPLE, "head")
    # The pink tip of the snout
    tip = H[1] - HR[1] * 0.98
    k.paint(lambda P: inside(P[:, 1] - (tip + 0.011), 0.002) * (np.abs(P[:, 0]) < 0.06) * (P[:, 2] > 0.38),
            SNOUT, on=["head"])


def add_teeth(k):
    """A white grin along each side of the head, one clean strip with a dark edge."""
    def look(u, v):
        c = np.tile(srgb(TEETH), (len(u), 1))
        c[np.abs(u) > 0.7] = srgb(GAP)
        return c
    for sx in (1, -1):
        k.decal((sx * 0.052, -0.17, 0.462), unit(sx, -0.3, -0.15), (0.009, 0.05), look, up=unit(0, -1, -0.12),
                eps=0.003, rings=6, segs=40)


def add_arms(k):
    k.add("shoulder", ell(SHOULDER, (0.032, 0.032, 0.032), sub=3), JOINT, "arm{s}", fuse=False, mirror=True,
          surface="metal")
    k.add("upper", box_along(SHOULDER + (0.01, 0, 0), ELBOW, 0.036, 0.034, 0.03, 0.03), PURPLE, "arm{s}",
          fuse=False, mirror=True, surface="metal")
    k.add("elbow", ell(ELBOW, (0.022, 0.022, 0.022), sub=3), JOINT, "arm{s}", fuse=False, mirror=True, surface="metal")
    # The forearm: a long flat blade, broad at the elbow, its face turned out and forward
    k.add("fore", box_along(ELBOW, HAND, 0.05, 0.03, 0.03, 0.024, up=unit(1, -0.6, 0)), PURPLE, "arm{s}",
          fuse=False, mirror=True, surface="metal")
    k.add("claw", point_along(HAND + (0, 0, 0.004), HAND - ELBOW, 0.05, 0.03, 0.024, up=unit(1, -0.6, 0)), CLAW,
          "arm{s}", fuse=False, mirror=True, surface="metal")


def add_legs(k):
    k.add("hip", ell(HIP + (0.012, 0, 0), (0.036, 0.036, 0.036), sub=3), JOINT, "leg{s}", fuse=False, mirror=True,
          surface="metal")
    k.add("thigh", box_along(HIP + (0.02, 0, 0), KNEE, 0.066, 0.05, 0.05, 0.044, up=(0, -1, 0.3)), PURPLE, "leg{s}",
          fuse=False, mirror=True, surface="metal")
    k.add("kneeball", ell(KNEE, (0.03, 0.03, 0.03), sub=3), JOINT, "leg{s}", fuse=False, mirror=True, surface="metal")
    k.add("knee", point_along(KNEE + (0, -0.01, 0.005), (0.1, -1, 0.45), 0.05, 0.036, 0.032), PURPLE, "leg{s}",
          fuse=False, mirror=True, surface="metal")
    k.add("shin", box_along(KNEE, ANKLE, 0.04, 0.038, 0.062, 0.05, up=(0, -1, 0.1)), PURPLE, "leg{s}",
          fuse=False, mirror=True, surface="metal")
    k.add("toe", point_along(ANKLE + (0, -0.01, -0.012), (0.05, -1, -0.15), 0.07, 0.044, 0.03), CLAW, "leg{s}",
          fuse=False, mirror=True, surface="metal")
    k.add("heel", point_along(ANKLE + (0, 0.02, -0.01), (0, 1, -0.1), 0.04, 0.032, 0.026), CLAW, "leg{s}",
          fuse=False, mirror=True, surface="metal")


def add_cannon(k, drive=DRIVE):
    """The housing on the back, the cannon pointing forward over the head and the drive's slot on top."""
    house = [(sx * 0.05, y, z) for sx in (1, -1) for y, z in ((-0.03, 0.6), (-0.03, 0.655), (0.13, 0.665),
                                                             (0.17, 0.62), (0.12, 0.57), (0.02, 0.575))]
    k.add("housing", hull(house), PURPLE, "body", fuse=False, surface="metal")
    k.add("under", hull([(sx * 0.044, y, z) for sx in (1, -1) for y, z in ((0.03, 0.45), (0.1, 0.47), (0.02, 0.585), (0.11, 0.585),
                                                                          (0.0, 0.6))]),
          GAP, "body", fuse=False, surface="metal")
    d = unit(0, -1, 0.06)
    c = np.array([0, 0.02, 0.632])
    k.add("barrel", lathe(c, d, [(0, 0), (0.046, 0.0), (0.048, 0.05), (0.04, 0.24), (0.042, 0.27), (0.024, 0.27),
                                 (0.024, 0.235), (0, 0.235)], seg=40), PALE, "body", fuse=False, surface="metal")
    k.add("bore", lathe(c, d, [(0, 0.2), (0.0235, 0.2), (0.0235, 0.262), (0, 0.262)], seg=32), GAP, "body",
          fuse=False, surface="metal")
    k.add("drive", hull([(sx * 0.028, y, z) for sx in (1, -1) for y, z in ((0.08, 0.655), (0.08, 0.705), (0.15, 0.705),
                                                                          (0.15, 0.655))]),
          drive, "body", fuse=False, surface="metal")


# --- the eyes: great red compound eyes with two pale lenses ---------------------------------------------------------------
def eye_open(u, v):
    c = np.tile(srgb(EYE_RIM), (len(u), 1))
    c[(u / 0.86) ** 2 + (v / 0.82) ** 2 < 1] = srgb(EYE_RED)
    for cu in (-0.38, 0.32):
        r = ((u - cu) / 0.3) ** 2 + ((v + 0.02) / 0.42) ** 2
        c[r < 1] = srgb(EYE_LENS)
        c[r < 0.38] = srgb(EYE_RED)
    return c


def eyes(k, state):
    f = eye_look(state, eye_open, srgb(EYE_RIM), srgb(GAP), line=0.16)
    for side in (1, -1):
        col = f if side == 1 else (lambda a, b, g=f: g(-a, b))
        k.decal((side * 0.068, -0.085, 0.5), unit(side * 0.85, -0.5, 0.15), (0.04, 0.031), col,
                up=unit(0, 0.2, 1), eps=0.003, rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="metal")
    add_body(k)
    add_head(k)
    add_teeth(k)
    add_arms(k)
    add_legs(k)
    add_cannon(k)
    k.build(voxel=0.006, band=0.005)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)
