# Morgrem sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Impidimp's script (impidimp.py, imported as sibling("impidimp")): the same cupped ears lined red, the same fangs,
# the same eyes (a white patch with its pupil as a patch of its own), the same navy of the hair and the tail.
#   blender --background --factory-startup --python sculpts/morgrem.py -- <the export's Morgrem.glb> <out.glb>
#
# From the references: a lanky magenta imp; a narrow face with a long pointed nose standing forward and up like a horn
# between two slanting eyes (white, a small dark pupil); a wide open grin under it, dark inside, a white fang at each
# corner; big cupped ears up and out, lined pink-red; long dark navy hair from the crown falling back and down the
# sides behind the face to the shoulders; a thin magenta torso whose lower half is green, the green rising to a point
# on the belly; thin green legs on green feet with three long toes; long thin magenta arms (one raised in the art,
# both carried forward and down here) ending in three long hooked claws; a long dark tail ending in an anchor's
# hooks. The pictures overrule the task's "pink-and-black goblin with a long black fringe of hair": the body is
# magenta and green, the hair falls behind the face, not over it.
#
# Reference ratios (3D picture): head with hair height / total 0.28 (built 0.31), ear length / head height 0.6,
# legs showing / total 0.4 (built 0.36, thighs fused into the hips), arm thickness / total 0.042 (built 0.043).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, slab, outward, srgb, inside, unit, polygon, args, sibling)

imp = sibling("impidimp")
on_head, ear, fang, claw = imp.on_head, imp.ear, imp.fang, imp.claw

MAGENTA, HAIR, GREEN, LINING = "#D8346E", imp.FRINGE, "#3CC07E", "#F0506A"
WHITE, PUPIL, MOUTH, FANG = imp.WHITE, imp.PUPIL, "#2A2050", imp.FANG
LIDLINE = "#F8B8CC"

H = np.array([0.0, -0.03, 0.5])               # the face's centre and radii (narrow, a little long)
HR = np.array([0.068, 0.072, 0.082])


def morgrem(src, out_path):
    k = Kit(src, coat="fur")

    # --- the head: a narrow face, a long horn of a nose ----------------------------------------------------------
    k.add("head", ell(H, HR), MAGENTA, "head")
    k.add("jaw", ell(H + (0, -0.018, -0.045), (0.056, 0.06, 0.05)), MAGENTA, "head")
    k.add("neck", tube([(0, 0.0, 0.36), (0, -0.012, 0.44)], [0.026, 0.028]), MAGENTA, "head")
    np_, nn = on_head(0.0, 0.08, H, HR)
    NB = np_ - nn * 0.02
    k.add("nose", hull([NB + (0.024, 0.01, -0.02), NB + (-0.024, 0.01, -0.02), NB + (0, 0.012, 0.035),
                        NB + (0.018, -0.02, 0.0), NB + (-0.018, -0.02, 0.0), NB + (0, -0.04, -0.01),
                        NB + (0.008, -0.06, 0.04), NB + (-0.008, -0.06, 0.04), NB + (0, -0.09, 0.09)]),
          MAGENTA, "head", fuse=False, surface="skin")

    # --- the hair: a navy cap over the crown, falling back and down the sides behind the face to the shoulders ---
    k.add("haircap", ell(H + (0, 0.022, 0.026), HR * (1.09, 1.0, 0.98)), HAIR, "head")
    k.add("hairback", ell(H + (0, 0.05, -0.03), (0.072, 0.05, 0.088)), HAIR, "head")
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        pts = np.array([(0.058, 0.012, 0.53), (0.075, 0.018, 0.46), (0.082, 0.028, 0.4), (0.078, 0.04, 0.345)]) * m
        k.add(f"lock{side}", blade(pts, [0.034, 0.03, 0.022, 0.012], (s * 0.6, 0.8, 0), 0.55), HAIR, "head")
    k.add("lockback", blade([(0, 0.07, 0.48), (0, 0.085, 0.42), (0, 0.075, 0.36)], [0.05, 0.04, 0.016], (0, 1, 0), 0.5),
          HAIR, "head")
    # the hair stops at the brow: the face's front is skin
    def face(P):
        a, e = imp.head_angles(P, H, HR)
        return (inside(np.abs(a) - 0.95, 0.006) * inside(e - (0.4 - 0.25 * a * a), 0.005) * inside(P[:, 1] - H[1] + 0.01, 0.006)
                * inside(np.abs(P[:, 2] - H[2]) - 0.12, 0.01))

    def crown(P):
        # all of the head's top and back is hair first (the cap's and the head's colours blend where their
        # surfaces nearly meet), then the face is painted back over it with a crisp edge
        a, e = imp.head_angles(P, H, HR)
        r = np.linalg.norm((P - H) / HR, axis=1)
        return inside(r - 1.35, 0.01) * inside(-0.3 - e, 0.01)
    k.paint(crown, HAIR, on=["haircap", "head", "hair"])
    k.paint(face, MAGENTA, on=["haircap", "head", "jaw"])

    # --- the body: a thin torso, magenta over green ---------------------------------------------------------------
    k.add("chest", ell((0, 0.0, 0.335), (0.05, 0.038, 0.06)), MAGENTA, "body")
    k.add("waist", ell((0, 0.004, 0.28), (0.042, 0.034, 0.045)), MAGENTA, "body")
    k.add("hips", ell((0, 0.006, 0.23), (0.05, 0.038, 0.04)), GREEN, "body")
    def green(P):
        line = 0.245 + 0.05 * np.clip(1 - np.abs(P[:, 0]) / 0.03, 0, 1) * (P[:, 1] < 0)
        return inside(P[:, 2] - line, 0.004) * inside(np.hypot(P[:, 0], P[:, 1]) - 0.07, 0.005)
    k.paint(green, GREEN, on=["waist", "hips", "chest"])

    # --- the legs: thin and green, the knees a little bent, long three-toed feet ----------------------------------
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", tube(np.array([(0.035, 0.005, 0.235), (0.052, -0.022, 0.125)]) * m, [0.034, 0.025]),
              GREEN, f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.052, -0.022, 0.125), (0.06, 0.004, 0.04)]) * m, [0.024, 0.02]),
              GREEN, f"leg{side}")
        FT = np.array([0.062, -0.022, 0.015])
        k.add(f"foot{side}", ell(FT * m, (0.026, 0.036, 0.015)), GREEN, f"leg{side}")
        for j, dx in enumerate((-0.022, 0.0, 0.022)):
            a = FT + np.array([dx * 0.5, -0.025, 0.0])
            b = a + np.array([dx, -0.04, -0.006])
            k.add(f"toe{side}{j}", tube(np.array([a, b]) * m, [0.0105, 0.0095]), GREEN, f"leg{side}")
            k.add(f"toeclaw{side}{j}", claw((b + np.array([dx * 0.1, -0.006, 0])) * m, np.array([dx, -1, -0.15]) * m,
                                            0.022, 0.018, 0.012), GREEN, f"leg{side}", fuse=False, surface="shell")

    # --- the arms: long and thin, forward and down, elbows bent; three long hooked claws ------------------------
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        SH, EL, WR = np.array([0.048, 0.0, 0.372]), np.array([0.092, -0.03, 0.3]), np.array([0.096, -0.095, 0.235])
        k.add(f"upper{side}", tube(np.array([SH, EL]) * m, [0.0155, 0.014]), MAGENTA, f"arm{side}")
        k.add(f"fore{side}", tube(np.array([EL, WR]) * m, [0.014, 0.0135]), MAGENTA, f"arm{side}")
        PALM = WR + np.array([0.002, -0.014, -0.01])
        k.add(f"palm{side}", ell(PALM * m, (0.02, 0.017, 0.016)), MAGENTA, f"arm{side}")
        for j, dx in enumerate((-0.016, 0.0, 0.016)):
            a = PALM + np.array([dx, -0.012, -0.008])
            b = a + np.array([dx * 0.6, -0.022, -0.03])
            c_ = b + np.array([dx * 0.3, -0.004, -0.028])
            k.add(f"finger{side}{j}", tube(np.array([a, b, c_]) * m, [0.0105, 0.01, 0.0095]), MAGENTA, f"arm{side}")
            k.add(f"fclaw{side}{j}", claw((c_ + np.array([0, 0.004, -0.004])) * m, np.array([dx * 0.4, 0.45, -1]) * m,
                                          0.026, 0.018, 0.013, up=(0, -1, 0)), MAGENTA, f"arm{side}", fuse=False,
                  surface="shell")

    # --- the tail: long and dark, out behind to +X, ending in an anchor's hooks ------------------------------------
    TP = [(0.0, 0.03, 0.225), (0.035, 0.085, 0.17), (0.09, 0.14, 0.12), (0.155, 0.18, 0.1)]
    k.add("tail", tube(TP, [0.013, 0.012, 0.011, 0.0105]), HAIR, "tail")
    ex = unit(0.62, 0.52, -0.05)
    tip = np.array(TP[-1]) - ex * 0.006
    anchor = [(0, -0.008), (0.032, -0.008), (0.024, -0.026), (-0.008, -0.034), (0.03, -0.044), (0.054, -0.014),
              (0.088, 0.0), (0.054, 0.014), (0.03, 0.044), (-0.008, 0.034), (0.024, 0.026), (0.032, 0.008), (0, 0.008)]
    k.add("anchor", slab(anchor, tip, ex, (0, 0, 1), 0.02), HAIR, "tail", fuse=False, surface="skin")

    # --- the ears: big cups up and out from the head's upper sides, lined pink-red, their tips curled ----------
    for s, side in ((1, "R"), (-1, "L")):
        base, nrm = on_head(s * 1.25, 0.32, H, HR)
        ear(k, f"ear{side}", base - nrm * 0.02, (s * 0.78, 0.08, 0.62), (s * 0.4, -0.92, 0.05), 0.15, 0.058, 0.013,
            MAGENTA, LINING, "head", cup=0.5, curl=0.07)

    # --- the mouth: a wide open grin under the nose, dark inside, a white fang at each corner --------------------
    p, n = on_head(0.0, -0.42, H, HR)
    k.decal(p, unit(0, -1, -0.25), (0.05, 0.026), srgb(MOUTH), eps=0.0025, rings=12, segs=72,
            outline=polygon([(-1, 0.85), (-0.5, 0.5), (0, 0.42), (0.5, 0.5), (1, 0.85), (0.72, -0.1),
                             (0.38, -0.65), (0, -0.85), (-0.38, -0.65), (-0.72, -0.1)]))
    for s, side in ((1, "R"), (-1, "L")):
        fp, fn = on_head(s * 0.5, -0.34, H, HR)
        k.add(f"fang{side}", fang(fp + fn * 0.001, (s * 0.08, -0.45, -1), 0.04, 0.02, 0.012, toward=fn), FANG, "head",
              fuse=False, surface="shell")

    k.build(voxel=0.0055, smooth=4, band=0.006, wband=0.04)

    # --- the eyes: almonds slanting down to the nose, a small dark pupil; the lid the face's magenta ------------
    almond = [(1.0, 0.25), (0.6, 0.75), (0.0, 0.6), (-0.7, 0.15), (-1.0, -0.35), (-0.4, -0.75), (0.4, -0.85),
              (0.95, -0.4)]
    lay = imp.lay_eyes(k, H, HR, 0.47, 0.1, (0.026, 0.026), WHITE, PUPIL, MAGENTA, LIDLINE, pupil_r=0.34,
                       pupil_dv=-0.1, outline=almond)
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    morgrem(*args())
