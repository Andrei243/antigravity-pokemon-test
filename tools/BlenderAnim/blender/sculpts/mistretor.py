# Mistretor (No. 10088, Carpathia, Ground/Fighting), sculpted from nothing on the skeleton and clips of the game's export:
#   blender --background --factory-startup --python sculpts/mistretor.py -- <the export's Mistretor.glb> <out.glb>
# Grown from mistretel.py (sibling("mistretel")): the same snout with its flat disc and nostrils, the same pointed ear
# with its lining, the same leg and cloven hoof, the same stripes (faded), the same dark eye with its glint (here cut by
# a glowering brow). From the picture (10088-Mistretor.jpg) and the brief (CP-dex-2b, 88): a huge wild boar, dark
# grizzled grey-brown; the shoulders hunched high over a lowered head, the back falling to the rump; a ridge of stiff
# black bristles standing up and raked back along the spine from the crown to the middle of the back; two great cream
# tusks sweeping out of the jaw and curving up; a dark muzzle round the snout; small fierce eyes under a heavy brow;
# shaggy dark cheeks; faint pale stripes over the back behind the shoulders; thick bent legs set wide, caked to the
# knee in dried mud, dark hooves; a curled tail ending in a black tuft.
#
# Reference ratios (picture): the crest 0.2 of the whole height (built 0.19); the head 0.45 of the body's height tall
# (built 0.47); a tusk's span 0.36 of the head's width each side (built 0.35); legs showing under the belly 0.27 of
# the body's height (built 0.25).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, lumpy, outward, srgb, inside, unit, polygon, args, sibling)

ms = sibling("mistretel")
lc = sibling("lechonk")

COAT, COAT2, BRISTLE, MUZZLE = "#4E4A42", "#5E584C", "#1E1C1A", "#2C2824"
STRIPE, DISC, LINING, TUSK = "#ADA488", "#6A4C46", "#4E3A34", "#EEE4C8"
HOOF, MUD, MUD2, NOSTRIL = "#2A2420", "#7C6A4C", "#6A5A40", "#1A1412"


def lock(k, name, root, d, length, width, thick, bone, color, side=None):
    """A stiff bristle lock: a solid kite-sectioned spike from root along d."""
    root, d = np.array(root, float), unit(*d)
    a = np.cross(d, (0, 0, 1.0)) if side is None else np.array(side, float)
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a = a - d * (a @ d); a /= np.linalg.norm(a)
    b = np.cross(d, a)
    k.add(name, hull([root + a * width, root - a * width, root + b * thick, root - b * thick,
                      root + d * length * 0.45 + a * width * 0.62, root + d * length * 0.45 - a * width * 0.62,
                      root + d * length * 0.45 + b * thick * 0.6, root - b * thick * 0.6 + d * length * 0.45,
                      root + d * length]), color, bone, fuse=False, surface="fur")


def mistretor(src, out):
    k = Kit(src, coat="fur")

    # --- the body: shoulders hunched high, the back falling to a round rump ----------------------------------------
    BC, BR = np.array([0, 0.04, 0.4]), np.array([0.255, 0.31, 0.24])
    k.add("barrel", ell(BC, BR), COAT, "body")
    k.add("shoulders", ell((0, -0.14, 0.45), (0.262, 0.25, 0.25)), COAT, "body")
    k.add("rump", ell((0, 0.22, 0.405), (0.232, 0.18, 0.218)), COAT, "body")
    k.add("neck", ell((0, -0.3, 0.42), (0.2, 0.16, 0.2)), COAT, "body")
    # grizzle: a lighter grey over the flanks
    k.paint(lambda P: inside(np.hypot((P[:, 1] - 0.04) / 0.26, (P[:, 2] - 0.36) / 0.12) - 1, 0.06)
            * inside(0.16 - np.abs(P[:, 0]), 0.04), COAT2, on=["barrel", "rump", "shoulders"])
    ms.stripes(k, BC, BR, STRIPE, (0.32, -0.32, 0.72, -0.72), 0.075, -0.06, 0.36, 0.44, wave=0.03, edge=0.025)

    # --- legs: thick, bent, set wide; mud caked to the knee -------------------------------------------------------
    ms.leg(k, "fore", (0.16, -0.22, 0.31), (0.11, 0.12, 0.16),
           [(0.17, -0.23, 0.28), (0.185, -0.26, 0.15), (0.19, -0.245, 0.06)], [0.085, 0.07, 0.064], "leg{s}", COAT,
           hoof=HOOF)
    ms.leg(k, "hind", (0.16, 0.22, 0.32), (0.12, 0.14, 0.17),
           [(0.17, 0.24, 0.28), (0.18, 0.2, 0.16), (0.188, 0.23, 0.06)], [0.088, 0.072, 0.065], "hind{s}", COAT,
           hoof=HOOF)
    k.paint(lambda P: inside(P[:, 2] - (0.17 + 0.022 * np.sin(P[:, 1] * 60 + P[:, 0] * 45)
                                        + 0.012 * np.sin(P[:, 1] * 140)), 0.004)
            * (np.abs(P[:, 0]) > 0.1), MUD, on=["fore", "hind"])
    for nm, bone, (x, y) in (("fore", "leg{s}", (0.188, -0.25)), ("hind", "hind{s}", (0.186, 0.225))):
        for j, (a, z, r) in enumerate(((-1.1, 0.12, 0.03), (0.4, 0.15, 0.026), (2.2, 0.1, 0.028))):
            c = (x + math.cos(a) * 0.066, y + math.sin(a) * 0.066, z)
            k.add(f"clod{nm}{j}", ell(c, (r, r, r * 0.75), deform=lumpy(3, 2, 0.25), sub=3), MUD2, bone,
                  fuse=False, mirror=True, surface="fur")

    # --- the head: lowered and forward, heavy brow, a dark muzzle round the snout -----------------------------------
    H, HR = np.array([0, -0.44, 0.39]), np.array([0.17, 0.165, 0.158])
    k.add("head", ell(H, HR), COAT, "head")
    k.add("brow", ell((0, -0.5, 0.47), (0.135, 0.09, 0.06)), COAT, "head")
    k.add("jowl", ell((0, -0.43, 0.31), (0.17, 0.12, 0.1)), COAT, "head")
    front, d = ms.snout(k, (0, -0.55, 0.345), (0, -0.62, 0.26), [0.1, 0.092, 0.082], (0.078, 0, 0.02), COAT, DISC,
                        NOSTRIL)
    k.paint(lambda P: inside(np.hypot(P[:, 0] / 0.11, (P[:, 1] + 0.61) / 0.1, ) - 1, 0.012)
            * inside(P[:, 2] - 0.36, 0.01) * inside(-0.54 - P[:, 1] + 0 * P[:, 2], 0.02) + 0 * P[:, 0],
            MUZZLE, on=["snout", "head", "jowl"])
    # the mouth, a hard line back along the jaw
    for s in (1, -1):
        md = unit(0.9 * s, -0.4, -0.3)
        k.decal(np.array([0.085 * s, -0.57, 0.25]), md, (0.04, 0.007), NOSTRIL, eps=0.002, rings=3, segs=28,
                up=(0, -0.3, 1))
    # shaggy dark cheeks
    for s in (1, -1):
        for j, (y, z) in enumerate(((-0.4, 0.33), (-0.36, 0.4), (-0.32, 0.3), (-0.45, 0.28))):
            lock(k, f"cheek{j}{'R' if s > 0 else 'L'}", (0.15 * s, y, z), (0.6 * s, 0.6, -0.45), 0.1, 0.034, 0.016,
                 "head", COAT if j % 2 else COAT2)

    # --- the tusks: out of the jaw, sweeping out and curving up, clear below the eyes --------------------------------
    for s in (1, -1):
        pts = [(0.06 * s, -0.58, 0.27), (0.14 * s, -0.63, 0.245), (0.205 * s, -0.66, 0.28), (0.225 * s, -0.66, 0.33),
               (0.205 * s, -0.63, 0.37)]
        k.add(f"tusk{'R' if s > 0 else 'L'}", outward(tube(pts, [0.03, 0.027, 0.021, 0.014, 0.006], seg=16, per=6)),
              TUSK, "head", fuse=False, surface="shell")

    # --- ears: pointed, standing up and a little back ------------------------------------------------------------------
    ms.ear(k, (0.105, -0.37, 0.5), (0.45, 0.2, 1), unit(0.3, -1, 0.2), 0.14, 0.055, COAT, LINING, 0.026)

    # --- the crest: stiff black bristles along the spine, raked back ------------------------------------------------
    # a fused black ridge along the spine carries the locks, so the crest is one mass
    def ridge_top(t):
        return float(np.interp(t, [0, 0.25, 0.6, 1.0], [0.55, 0.7, 0.72, 0.665]))
    k.add("ridgehead", ell((0, -0.42, 0.53), (0.06, 0.08, 0.05)), BRISTLE, "head")
    k.add("ridge", tube([(0, -0.36, ridge_top(0.05) - 0.03), (0, -0.2, ridge_top(0.32) - 0.035),
                         (0, -0.02, ridge_top(0.65) - 0.04), (0, 0.13, ridge_top(0.95) - 0.05)],
                        [0.06, 0.07, 0.06, 0.035]), BRISTLE, "body")
    n = 17
    for i in range(n):
        t = i / (n - 1)
        y = -0.43 + 0.58 * t
        z = ridge_top(t) - 0.01
        h = 0.12 + 0.1 * math.sin(math.pi * min(t * 1.3, 1.0)) * (1 - 0.25 * t)
        lean = 0.3 if i % 2 else 0.1                   # turned toward +X, so the front camera sees their faces
        lock(k, f"bristle{i}", (0, y, z), (lean, 0.8, 1.0), h, 0.06, 0.026, "body" if y > -0.3 else "head",
             BRISTLE, side=(math.cos(0.6), -math.sin(0.6), 0))
        if 0.1 < t < 0.85 and i % 2 == 0:              # a lower row each side, spread out, so the mane is broad
            for s in (1, -1):
                lock(k, f"bristleside{i}{'R' if s > 0 else 'L'}", (0.05 * s, y + 0.02, z - 0.035),
                     (0.6 * s, 0.75, 0.7), h * 0.6, 0.05, 0.022, "body" if y > -0.3 else "head", BRISTLE)

    # --- the tail: curled, a black tuft at its end -------------------------------------------------------------------
    end = lc.curl_tail(k, (0.0, 0.39, 0.5), 0.05, (0.5, 0.86, 0.0), 0.02, COAT)
    for j, dd in enumerate(((0.4, 0.6, -0.2), (0.6, 0.3, 0.3), (0.1, 0.7, 0.4))):
        lock(k, f"tailtuft{j}", end, dd, 0.08, 0.03, 0.016, "tail", BRISTLE)

    de = unit(0.66, -0.66, 0.36)
    E = ms.on_ell(H, HR, de)
    k.build(voxel=0.0088, smooth=4, band=0.008, wband=0.05)
    cut = polygon([(-2.0, -2.0), (2.0, -2.0), (2.0, 0.28 + 0.3 * 2), (-2.0, 0.28 - 0.3 * 2)])
    outline = lambda th: min(1.0, cut(th))
    k.eye_states(ms.eyes_pair(k, E, de, (0.034, 0.032), ms.boar_eye(), srgb(COAT), line=0.17, outline=outline))
    k.export(out)


if __name__ == "__main__":
    mistretor(*args())
