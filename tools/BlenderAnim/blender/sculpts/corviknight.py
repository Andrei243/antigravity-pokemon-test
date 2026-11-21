# Corviknight sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/corviknight.py -- <the export's Corviknight.glb> <out.glb>
# Corvisquire grown (its crest of spikes, beak, tail, feet and way of making the eyes, imported from corvisquire.py and
# rookidee.py): from the references, a great raven standing tall in indigo steel; its wings folded down its sides as a
# cape, the design's largest mass, each with a hard plate standing up over the shoulder, a pale sheen band across it and
# long feather plates trailing to the ground behind; a helmet of a head with a crown of short spikes laid back and a
# hard brow; a black visor band round small red eyes; a great dark grey hooked beak; a ruff of plates at the throat;
# armoured thighs over dark grey legs, three long toes forward and one back with indigo talons; a long tail behind.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, slab, hull, srgb, inside, tri, unit, args, sibling

cv = sibling("corvisquire")
r = cv.r
spike, on_ell = r.spike, r.on_ell

NAVY, DEEP, SHEEN, VISOR = "#3C3B70", "#2B2A52", "#5C5EA6", "#141319"
BEAKC, LEGC, TALON = "#4C5062", "#4A4A56", "#4E4C92"
RED, GLINT, LINE = "#E2243A", "#FFFFFF", "#8E90B0"


def frame():
    F = SimpleNamespace()
    F.H = np.array([0, -0.075, 0.685])               # the head's centre, before its joint (0, -0.03, 0.6)
    F.HR = np.array([0.106, 0.12, 0.1])
    F.NECK = ((0, -0.04, 0.6), (0.105, 0.105, 0.1))
    F.BODY = ((0, 0.02, 0.435), (0.15, 0.165, 0.215), (-8, 0, 0))
    F.CHEST = ((0, -0.06, 0.5), (0.13, 0.11, 0.14))
    F.WING = ([(0.13, -0.04, 0.6), (0.165, 0.04, 0.48), (0.15, 0.15, 0.32), (0.1, 0.25, 0.2)],
              [0.07, 0.1, 0.08, 0.035], 0.5)
    F.LEGX, F.LEGTOP, F.ANKLE, F.LEGY = 0.075, 0.28, 0.034, 0.0
    F.LEGR = (0.027, 0.023)
    F.TOE, F.HEEL, F.TOER = 0.095, 0.05, 0.0145
    F.EYEDIR = unit(0.58, -0.76, 0.16)
    F.EYE = (0.03, 0.022)
    F.TAILBASE = np.array([0, 0.14, 0.3])
    F.VOXEL = 0.0075
    return F


def body(k, F):
    k.add("neck", ell(*F.NECK), NAVY, "head")
    c, rr, rot = F.BODY
    k.add("body", ell(c, rr, rot=rot), NAVY, "body")
    k.add("chest", ell(*F.CHEST), NAVY, "body")
    k.add("head", ell(F.H, F.HR), NAVY, "head")
    for s, b in ((1, "legR"), (-1, "legL")):
        k.add("thigh", ell((0.07 * s, 0.01, 0.285), (0.06, 0.07, 0.06)), NAVY, b)
    pts, radii, f = F.WING
    k.add("wing", blade(pts, radii, (1, 0, 0), f), NAVY, "wing{s}", mirror=True)


def coat(k, F):
    """A pale sheen band across each folded wing and across the chest; the black visor band round the eyes."""
    k.paint(lambda P: inside(np.abs(P[:, 2] - (0.47 - 0.35 * P[:, 1])) - 0.012, 0.003)
            * inside(0.1 - np.abs(P[:, 0]), 0.004), SHEEN, on=["wing"])
    H = F.H
    k.paint(lambda P: inside(np.abs(P[:, 2] - (H[2] + 0.01 + 0.3 * (P[:, 1] - H[1] + 0.03))) - 0.022, 0.002)
            * inside(P[:, 1] - (H[1] + 0.045), 0.004) * inside(0.035 - np.abs(P[:, 0]), 0.004), VISOR, on=["head"])


def helmet(k, F):
    """The crown of short spikes laid back, and a hard brow over the visor."""
    top = F.H + np.array([0, 0, F.HR[2]])
    for b, t, w, th in (((0, -0.06, -0.012), (0, 0.0, 0.065), 0.06, 0.028),
                        ((0, -0.005, -0.006), (0, 0.065, 0.05), 0.056, 0.026),
                        ((0, 0.045, -0.024), (0, 0.12, 0.012), 0.05, 0.024)):
        k.add("crest", spike(top + b, top + t, w, th, side=(1, 0, 0)), DEEP, "head", fuse=False)
    for s in (1, -1):
        k.add("crestside", spike(top + (0.05 * s, -0.02, -0.025), top + (0.09 * s, 0.05, 0.035), 0.05, 0.024,
                                 side=(0.3 * s, 0, 1)), DEEP, "head", fuse=False)
    H, R = F.H, F.HR
    y0, z0 = H[1] - R[1] * 0.66, H[2] + R[2] * 0.5
    k.add("brow", hull([(0.0, y0 - 0.035, z0 + 0.004), (0.06, y0 - 0.012, z0 + 0.0), (-0.06, y0 - 0.012, z0 + 0.0),
                        (0.085, y0 + 0.045, z0 + 0.012), (-0.085, y0 + 0.045, z0 + 0.012),
                        (0.0, y0 + 0.04, z0 + 0.045), (0.0, y0 - 0.02, z0 + 0.026),
                        (0.05, y0 + 0.03, z0 - 0.02), (-0.05, y0 + 0.03, z0 - 0.02)]), NAVY, "head", fuse=False)


def beak(k, F):
    pg = sibling("pidgey")
    H = F.H
    k.add("beak", ell(H + (0, -0.135, -0.012), (0.042, 0.082, 0.04), rot=(10, 0, 0), deform=pg.hooked), BEAKC,
          "head", fuse=False, surface="shell")
    k.add("beaklow", ell(H + (0, -0.118, -0.045), (0.03, 0.058, 0.014), rot=(-6, 0, 0), deform=pg.pointed), BEAKC,
          "head", fuse=False, surface="shell")


def armour(k, F):
    """The shoulder plates standing up, the trailing feather plates of the folded wings, the throat's ruff and the
    plates over the thighs: hard pieces in the body's colours."""
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        k.add("pauldron", hull([p * m for p in ((0.09, -0.08, 0.57), (0.18, -0.06, 0.57), (0.18, 0.06, 0.57),
                                                (0.09, 0.05, 0.59), (0.1, -0.07, 0.635), (0.17, -0.05, 0.635),
                                                (0.17, 0.04, 0.625), (0.1, 0.04, 0.64))]), NAVY, f"wing{'R' if s > 0 else 'L'}",
              fuse=False, surface="metal")
        for j, (b, t, w) in enumerate((((0.15, 0.1, 0.36), (0.13, 0.3, 0.09), 0.1),
                                       ((0.12, 0.13, 0.33), (0.07, 0.33, 0.1), 0.09),
                                       ((0.16, 0.04, 0.42), (0.17, 0.2, 0.2), 0.11))):
            k.add("primary", spike(np.array(b) * m, np.array(t) * m, w, 0.03, side=(s * 0.8, 0, 0.6)),
                  DEEP if j != 2 else NAVY, f"wing{'R' if s > 0 else 'L'}", fuse=False, surface="metal")
        for j, (b, t) in enumerate((((0.07, -0.05, 0.36), (0.1, -0.07, 0.25)), ((0.1, 0.03, 0.36), (0.13, 0.03, 0.25)))):
            k.add("thighplate", spike(np.array(b) * m, np.array(t) * m, 0.08, 0.03, side=(0, 1, 0)), DEEP,
                  f"leg{'R' if s > 0 else 'L'}", fuse=False, surface="metal")
    # the ruff at the throat: plates pointing down over the chest
    for j, (x, y, z, ln) in enumerate(((0.0, -0.155, 0.6, 0.1), (0.06, -0.14, 0.59, 0.09), (-0.06, -0.14, 0.59, 0.09),
                                       (0.035, -0.18, 0.52, 0.08), (-0.035, -0.18, 0.52, 0.08))):
        b = np.array([x, y, z])
        k.add("ruff", spike(b, b + unit(x * 2, -0.35, -1) * ln, 0.06, 0.024, side=(1, 0, 0)), NAVY, "body",
              fuse=False, surface="metal")


def corviknight(src, out_path):
    F = frame()
    k = Kit(src, coat="metal")
    body(k, F)
    coat(k, F)
    helmet(k, F)
    beak(k, F)
    armour(k, F)
    r.tail(k, F, [(12, 0.3, 0.09, NAVY), (0, 0.27, 0.08, DEEP), (26, 0.26, 0.08, DEEP)], pitch=-38, thick=0.02,
           lean=30)
    r.legs(k, F, foot=LEGC, claw=TALON, clawlen=3.2)
    k.build(voxel=F.VOXEL, band=0.006, wband=0.06)
    k.eye_states(r.eyes(k, F, r.iris_eye(VISOR, RED, GLINT, at=(-0.1, -0.05), r=0.55),
                        lambda u: 0.35 + 0.4 * u, lambda u: 0.0 + 0.55 * u, lid=VISOR, ink=LINE))
    k.export(out_path)


if __name__ == "__main__":
    corviknight(*args())
