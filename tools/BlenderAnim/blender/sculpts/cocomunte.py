# Cocomunte sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cocomunte.py -- <the export's Cocomunte.glb> <out.glb>
# From the picture (10093-Cocomunte.jpg, the project's own): a capercaillie cock in display, standing on the ground:
# dark slate plumage, an iridescent green breast thrust out, a shaggy beard of dark feathers under the chin, the
# head raised with a pale hooked beak open mid-song (a Sound type: its voice is its instrument, so the gape and the
# raised head are seen first), a bright red brow over each eye with flame-like points swept back; the wings drooped
# down its sides, their primaries trailing to the ground; a great fanned tail standing up behind as a half wheel,
# dark navy feathers each with a white mark, a lighter blue-grey ring of coverts at its root edged in white; dark
# feathered legs and grey scaly feet. The picture's rings of song in the air are dropped (a drawn effect).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, slab, outward, srgb, inside, smoothstep, unit, eye_look, args,
                       sibling)

T = sibling("torchic")         # spike, foot, on_ell
C = sibling("cocosel")         # eye_open, lay_eyes, wing (a folded wing as a fused blade)

SLATE = "#424D60"
DARK = "#262D3A"
WING = "#56637A"
WINGDK = "#2E3646"
FAN = "#1D2436"
FAN2 = "#28324A"
COVERT = "#4D5D82"
MARK = "#DCE2EC"
GREEN = "#1A7848"
GREENHI = "#2E9E5E"
RED = "#E2402E"
BEAK = "#CFCDBA"
MOUTH = "#6A1E24"
FOOT = "#5A5B63"
CLAW = "#3A3A40"
INK = srgb("#141217")
SCLERA = srgb("#E9E4D8")
IRIS = srgb("#5A2E1E")

H = np.array([0.0, -0.105, 0.645])          # the head raised, a tenth and more over the picture's share
HR = np.array([0.072, 0.075, 0.071])
DE = unit(0.78, -0.5, 0.2)
EYE_HALF = (0.022, 0.024)


def framed_ell(c, r, ex, n, d, sub=3):
    """An ellipsoid whose axes are ex, n, d (radii r in that order)."""
    V, F = ell((0, 0, 0), r, sub=sub)
    M = np.stack([ex, n, d], axis=1)
    return outward((V @ M.T + np.array(c, float), F))


def fan_feather(k, name, base, d, L, W, thick, colour, mark, mark_at=0.72, mark_r=(0.017, 0.024)):
    """A broad tail feather with a rounded end, its face to the front and the back, a white mark through both faces."""
    d = unit(*d)
    ex = np.cross(d, (0, 1.0, 0)); ex /= np.linalg.norm(ex)
    n = np.cross(ex, d)
    out = [(-0.014, 0), (-W * 0.75, 0.42 * L), (-W, 0.78 * L), (-0.72 * W, 0.94 * L), (0, L),
           (0.72 * W, 0.94 * L), (W, 0.78 * L), (W * 0.75, 0.42 * L), (0.014, 0)]
    k.add(name, slab(out, base, ex, d, thick), colour, "tail", fuse=False, surface="fur")
    if mark is not None:
        c = np.array(base, float) + d * L * mark_at
        k.add(name + "mark", framed_ell(c, (mark_r[0], thick * 0.5 + 0.004, mark_r[1]), ex, n, d), mark, "tail",
              fuse=False, surface="fur")


def cocomunte(src, out_path):
    k = Kit(src, coat="fur")
    # --- the body, chest thrown out and up; a thick neck into the raised head ------------------------------------
    k.add("body", ell((0, 0.025, 0.345), (0.128, 0.165, 0.145), rot=(-20, 0, 0)), SLATE, "body")
    k.add("belly", ell((0, 0.035, 0.27), (0.108, 0.13, 0.09)), SLATE, "body")
    k.add("chest", ell((0, -0.1, 0.44), (0.112, 0.102, 0.13)), SLATE, "body")
    k.add("neck", ell((0, -0.092, 0.56), (0.078, 0.078, 0.088)), SLATE, "head")
    k.add("head", ell(H, HR), SLATE, "head")
    # the green breast, brighter in its middle (crisp paint on the chest's front, limited all round)
    def breast(P, c=(0, -0.19, 0.445), r=(0.112, 0.11, 0.13)):
        d = np.linalg.norm((P - np.array(c)) / np.array(r), axis=1)
        return smoothstep(1.0, 0.82, d) * inside(P[:, 1] + 0.09, 0.01)
    k.paint(breast, GREEN, on=["chest", "body"])
    k.paint(lambda P: breast(P, (0.015, -0.2, 0.455), (0.075, 0.09, 0.09)) * 0.6, GREENHI, on=["chest"])
    # a darker back
    k.paint(lambda P: smoothstep(0.0, 0.08, P[:, 1]) * inside(0.38 - P[:, 2], 0.02) * inside(P[:, 2] - 0.56, 0.01),
            DARK, on=["body"])

    # --- legs: dark feathered thighs and shanks, grey scaly feet with dark claws ---------------------------------
    k.add("thigh", ell((0.062, 0.02, 0.215), (0.058, 0.064, 0.085)), DARK, "leg{s}", mirror=True)
    k.add("shank", tube([(0.063, 0.012, 0.21), (0.066, 0.004, 0.12), (0.069, -0.004, 0.045)], [0.032, 0.027, 0.022]),
          DARK, "leg{s}", mirror=True)
    T.foot(k, (0.07, -0.008, 0.032), 0.066, 0.0115, FOOT, claws=CLAW, claw_len=0.02, surface="scales")

    # --- the wings drooped down the sides, their primaries trailing to the ground ---------------------------------
    k.add("wing", blade([(0.112, -0.07, 0.47), (0.138, -0.005, 0.405), (0.15, 0.055, 0.3), (0.146, 0.095, 0.2)],
                        [0.05, 0.075, 0.066, 0.034], (1, 0, 0), 0.38), WING, "wing{s}", mirror=True)
    k.paint(lambda P: inside(np.abs(np.sin((P[:, 2] + 0.6 * P[:, 1]) * 55)) - 0.35, 0.08) *
            inside(0.42 - P[:, 2], 0.01) * inside(np.abs(P[:, 0]) - 0.17, 0.01) * inside(0.13 - np.abs(P[:, 0]), 0.01),
            WINGDK, on=["wing"])
    for i in range(5):
        d = unit(0.28 + 0.05 * i, 0.3 + 0.13 * i, -1.0)
        base = (0.15 - 0.004 * i, 0.03 + 0.024 * i, 0.235 - 0.006 * i)
        L = 0.19 - 0.012 * i
        k.add(f"primary{i}", T.feather(base, d, L, 0.036, thick=0.022, across=(0, 1, 0), root=0.6),
              WING if i % 2 == 0 else WINGDK, "wing{s}", fuse=False, mirror=True, surface="fur")

    # --- the tail fan: a half wheel of broad dark feathers standing up behind, leaned back, a ring of coverts ----
    k.add("tailroot", ell((0, 0.155, 0.405), (0.085, 0.06, 0.075)), DARK, "tail")
    B = np.array([0.0, 0.172, 0.4])
    n = 13
    for i in range(n):
        a = math.radians(-96 + 192 * i / (n - 1))
        d = (math.sin(a), 0.27 * math.cos(a) + 0.06, math.cos(a))
        L = 0.3 - 0.035 * abs(math.sin(a)) ** 2
        base = B + np.array([0, 0.012 - 0.026 * (i % 2), 0])
        fan_feather(k, f"fan{i}", base, d, L, 0.041, 0.024, FAN if i % 2 == 0 else FAN2, MARK)
    for i in range(11):
        a = math.radians(-90 + 180 * i / 10)
        d = (math.sin(a), 0.2 * math.cos(a) + 0.03, math.cos(a))
        base = B + np.array([0, -0.055 - 0.022 * (i % 2), 0.005])
        fan_feather(k, f"covert{i}", base, d, 0.17, 0.034, 0.022, COVERT, MARK, mark_at=0.86, mark_r=(0.02, 0.016))

    # --- the shaggy beard under the chin ------------------------------------------------------------------------
    for i, x in enumerate((-0.032, -0.012, 0.012, 0.032)):
        k.add(f"beard{i}", T.spike((x, -0.15, 0.6), (x * 2, -0.3, -1), 0.06 - 0.008 * abs(x) / 0.032, 0.02, 0.012,
                                   across=(1, 0, 0)), SLATE, "head", fuse=False, surface="fur")
    for i, x in enumerate((-0.05, 0.05)):
        k.add(f"ruff{i}", T.spike((x, -0.118, 0.59), (x * 6, -0.2, -1), 0.05, 0.02, 0.012, across=(0, 1, 0)),
              DARK, "head", fuse=False, surface="fur")

    # --- the beak, open mid-song: a hooked upper bill, a lower bill, the mouth dark between ------------------------
    k.add("mouth", ell((0, -0.168, 0.65), (0.024, 0.024, 0.02)), MOUTH, "head", fuse=False, surface="skin")
    k.add("upperbill", hull([(0.028, -0.152, 0.668), (-0.028, -0.152, 0.668), (0, -0.15, 0.7), (0, -0.16, 0.66),
                             (0.012, -0.21, 0.694), (-0.012, -0.21, 0.694), (0, -0.208, 0.708),
                             (0, -0.248, 0.692), (0, -0.244, 0.678)]), BEAK, "head", fuse=False, surface="shell")
    k.add("lowerbill", hull([(0.024, -0.15, 0.64), (-0.024, -0.15, 0.64), (0, -0.152, 0.622), (0, -0.16, 0.652),
                             (0.01, -0.2, 0.638), (-0.01, -0.2, 0.638), (0, -0.218, 0.628), (0, -0.2, 0.624)]),
          BEAK, "head", fuse=False, surface="shell")

    # --- the red brow over each eye: painted on the head, flame points swept back -------------------------------
    E = T.on_ell(H, HR, DE)
    BC = E + np.array([-0.004, 0.006, 0.025])
    def brow(P):
        q = P.copy(); q[:, 0] = np.abs(q[:, 0])
        d = np.linalg.norm((q - BC) / np.array([0.03, 0.036, 0.015]), axis=1)
        return inside(d - 1, 0.04)
    k.paint(brow, RED, on=["head"])
    for j, (dy, dz, ln) in enumerate(((0.0, 0.0, 0.036), (0.022, -0.006, 0.03), (-0.018, 0.004, 0.026))):
        b = BC + np.array([-0.006, dy + 0.012, dz])
        k.add(f"browtip{j}", T.spike(b, unit(0.3, 0.85, 0.42 - 0.12 * j), ln, 0.012, 0.011), RED, "head",
              fuse=False, mirror=True, surface="skin")

    k.build(voxel=0.006, smooth=4, band=0.006, wband=0.05)
    V = np.array([v.co[:] for v in k.ob.data.vertices])
    h = V[:, 2].max(); w = np.ptp(V[:, 0]); l = np.ptp(V[:, 1]); sq = 1.3 * max(h, 0.85 * max(w, l))
    print(f"KIT: board height {h:.3f} width {w:.3f} length {l:.3f}: sum {0.853 * w + 0.522 * l:.3f} of square"
          f" {sq:.3f} ({(0.853 * w + 0.522 * l) / sq:.2f})")
    look = C.eye_open(sclera=SCLERA, iris=IRIS, iris_r=0.66, pupil_r=0.34, iris_c=(-0.1, -0.04), rim=0.84)
    k.eye_states(lambda s: C.lay_eyes(k, s, E, DE, EYE_HALF, look, srgb(SLATE)))
    k.export(out_path)


if __name__ == "__main__":
    cocomunte(*args())
