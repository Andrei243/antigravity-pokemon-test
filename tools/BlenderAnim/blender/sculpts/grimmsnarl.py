# Grimmsnarl sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Impidimp's and Morgrem's scripts (impidimp.py, morgrem.py, imported with sibling): the same cupped ears with their
# lining, the same fangs and claws, the same eyes (a white patch with its pupil as a patch of its own), the same navy
# of the line's hair, the horn of a nose and the open grin.
#   blender --background --factory-startup --python sculpts/grimmsnarl.py -- <the export's Grimmsnarl.glb> <out.glb>
#
# From the references: a hulking ogre whose muscles are its dark purple-grey hair wound round its body. The largest
# masses are the arms (great shoulders and forearms of hair ending in four long hooked locks for fingers, carried
# forward and down here, held out and raised in the art) and the thighs (a bell of hair round each, its hem in
# uneven points over short dark shins and green clawed feet); then the broad chest; then the hood of hair round a small
# green face. The face: a long green horn of a nose rising between two small angry white eyes with red pupils, a wide
# open red mouth with white fangs above and below, two tall pointed green ears lined red standing up from the hood.
# Locks of the hood fall over the chest's sides; a green stripe runs down the chest from a red diamond under the chin
# to the crotch, widening at the belly; locks of hair rise curling off the shoulders; small green slashes show on the
# upper arms. The pictures overrule the old C# sculpt (green claws on the hands: the fingers are hair) and the task's
# "green-black ogre" (the hair is purple-grey, the skin green only on the face, the stripe and the feet).
#
# Reference ratios (3D picture): hood height / total 0.26 (built 0.30), face width / hood width 0.5, total width /
# height 1.08 in the art's pose (built 0.85 with the arms forward and down), shoulder-and-arm mass / chest width 0.6
# each side (built 0.6), skirt width / chest width 1.05.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, outward, srgb, inside, unit, polygon, tri, args, sibling)

imp = sibling("impidimp")
on_head, ear, fang, claw = imp.on_head, imp.ear, imp.fang, imp.claw

HAIR, DEEP, GREEN, RED = "#545074", "#403C5E", "#2FD08A", "#E0304E"
LINING, MOUTH, TEETH = "#E8344E", "#C8243E", imp.FANG
WHITE, PUPIL = imp.WHITE, "#D01C30"
LIDLINE = "#0E3A26"

F = np.array([0.0, -0.195, 0.765])              # the green face's centre and radii (small, a pointed chin)
FR = np.array([0.08, 0.062, 0.095])
HOOD = np.array([0.0, -0.075, 0.8])
HOODR = np.array([0.155, 0.14, 0.155])


def chinned(V):
    """A face narrowing to a pointed chin."""
    V = V.copy()
    t = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.45 * t
    return V


def lock(k, name, pts, radii, flat, colour, bone, tip=None):
    """A lock of hair: a tube flattened across flat, ending in a loose pointed hull along its last leg."""
    pts = np.array(pts, float)
    k.add(name, blade(pts, radii, flat, 0.6), colour, bone)
    if tip is not None:
        d = pts[-1] - pts[-2]
        k.add(name + "tip", fang(pts[-1] - unit(*d) * radii[-1] * 0.6, d, tip, radii[-1] * 1.9, radii[-1] * 1.2,
                                 toward=flat), colour, bone, fuse=False, surface="hair")


def grimmsnarl(src, out_path):
    k = Kit(src, coat="hair")

    # --- the thighs: a bell of hair round each, its hem in uneven points; short dark shins, green clawed feet --
    def bell(V):
        V = V.copy()
        s = 1 + 0.18 * np.clip(-V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array([0.135, -0.005, 0.265]) * m, (0.14, 0.135, 0.175), deform=bell), HAIR,
              f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.15, 0.0, 0.2), (0.155, -0.01, 0.06)]) * m, [0.06, 0.05]), DEEP,
              f"leg{side}")
        rs = np.random.RandomState(7 if s > 0 else 11)
        for j, a in enumerate(np.radians(np.arange(0, 360, 51.4) + 12)):
            o = np.array([math.sin(a), -math.cos(a), 0.0])
            root = np.array([0.135 * s, -0.005, 0.13]) + o * np.array([0.11, 0.105, 0])
            length = 0.035 + 0.075 * rs.rand()
            k.add(f"hem{side}{j}", fang(root, o * 0.35 + np.array([0, 0, -1.0]), length, 0.1, 0.036, toward=o),
                  HAIR, f"leg{side}", fuse=False, surface="hair")
        FT = np.array([0.16, -0.05, 0.026])
        k.add(f"foot{side}", ell(FT * m, (0.058, 0.08, 0.028)), GREEN, f"leg{side}", surface="skin")
        k.add(f"ankle{side}", ell(np.array([0.157, -0.01, 0.06]) * m, (0.05, 0.05, 0.04)), GREEN, f"leg{side}",
              surface="skin")
        for j, dx in enumerate((-0.035, 0.0, 0.035)):
            b = FT + np.array([dx, -0.07, -0.004])
            k.add(f"toe{side}{j}", claw(b * m, np.array([dx * 1.2, -1, -0.25]) * m, 0.06, 0.034, 0.026), GREEN,
                  f"leg{side}", fuse=False, surface="shell")
        k.add(f"heel{side}", claw((FT + (0.0, 0.07, 0.0)) * m, np.array([0.3 * s, 1, -0.2]), 0.045, 0.03, 0.024),
              GREEN, f"leg{side}", fuse=False, surface="shell")

    # a crisp edge between the green feet and the dark shins
    k.paint(lambda P: inside(P[:, 2] - 0.078, 0.003), GREEN, on=["shin", "ankle", "foot"])
    k.paint(lambda P: inside(0.078 - P[:, 2], 0.003), DEEP, on=["shin", "ankle"])

    # --- the trunk: broad chest out, a pinched waist, hips under it ------------------------------------------
    k.add("chest", ell((0, -0.02, 0.6), (0.22, 0.155, 0.155)), HAIR, "body")
    k.add("waist", ell((0, 0.0, 0.47), (0.16, 0.125, 0.11)), HAIR, "body")
    k.add("hips", ell((0, 0.005, 0.38), (0.2, 0.14, 0.1)), HAIR, "body")

    # the green stripe down the middle, from the red diamond under the chin to the crotch, widest at the belly
    def stripe(P):
        z = P[:, 2]
        w = 0.022 + 0.032 * np.clip(1 - np.abs(z - 0.46) / 0.14, 0, 1) - 0.016 * np.clip((0.38 - z) / 0.06, 0, 1)
        w = w + 0.006 * np.sin(z * 90)
        return (inside(np.abs(P[:, 0]) - w, 0.003) * inside(z - 0.69, 0.003) * inside(0.33 - z, 0.003)
                * (P[:, 1] < -0.04))
    k.paint(stripe, GREEN, on=["chest", "waist", "hips"])
    def diamond(P):
        return (inside(np.abs(P[:, 0]) / 0.03 + np.abs(P[:, 2] - 0.665) / 0.045 - 1, 0.003) * (P[:, 1] < -0.06))
    k.paint(diamond, RED, on=["chest"])

    # --- the arms: great masses of hair carried forward and down, elbows bent; four hooked locks for fingers ---
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        SH, EL, WR = np.array([0.25, 0.0, 0.64]), np.array([0.34, -0.1, 0.5]), np.array([0.3, -0.28, 0.46])
        k.add(f"shoulder{side}", ell(SH * m, (0.13, 0.12, 0.12)), HAIR, f"arm{side}")
        k.add(f"upper{side}", tube(np.array([SH, (SH + EL) / 2 + (0.02, 0, 0), EL]) * m, [0.115, 0.11, 0.095]), HAIR,
              f"arm{side}")
        k.add(f"fore{side}", tube(np.array([EL, (EL + WR) / 2 + (0.015, -0.01, 0.01), WR]) * m, [0.095, 0.1, 0.075]),
              HAIR, f"arm{side}")
        HD = WR + np.array([0.0, -0.05, -0.02])
        k.add(f"hand{side}", ell(HD * m, (0.075, 0.07, 0.06)), HAIR, f"arm{side}")
        for j, dx in enumerate((-0.05, -0.017, 0.017, 0.05)):
            a = HD + np.array([dx, -0.04, -0.02])
            b = a + np.array([dx * 0.4, -0.03, -0.045])
            c_ = b + np.array([dx * 0.2, 0.0, -0.05])
            lock(k, f"finger{side}{j}", np.array([a, b, c_]) * m, [0.03, 0.026, 0.02], (0, 1, 0), HAIR, f"arm{side}",
                 tip=0.05)
        # a green slash on the upper arm's outside, and one on the forearm
        for j, (c, n) in enumerate([((0.36, -0.02, 0.58), (1, -0.25, 0.25)), ((0.42, -0.18, 0.49), (1, -0.2, 0.1))]):
            k.decal(np.array(c) * m, np.array(n) * m, (0.05, 0.013), srgb(GREEN), up=unit(0.2 * s, 0, 1),
                    eps=0.004, rings=6, segs=36, outline=polygon([(-1, 0.0), (-0.3, 0.8), (1, 0.1), (0.3, -0.8)]))
        # locks of hair rising and curling off the shoulder, laid back and out
        for j, (r, t) in enumerate([((0.2, 0.06, 0.72), (0.27, 0.12, 0.86)), ((0.29, 0.07, 0.68), (0.4, 0.13, 0.78))]):
            r, t = np.array(r), np.array(t)
            mid = (r + t) / 2 + np.array([0.03, 0.0, -0.01])
            lock(k, f"wisp{side}{j}", np.array([r, mid, t]) * m, [0.05, 0.036, 0.024], (0, 1, 0.3), HAIR, "body",
                 tip=0.06)

    # --- the hood of hair round the face, locks falling over the chest's sides ---------------------------------
    k.add("hood", ell(HOOD, HOODR), HAIR, "head")
    k.add("hoodback", ell(HOOD + (0, 0.07, -0.09), (0.15, 0.12, 0.15)), HAIR, "head")
    k.add("neck", tube([(0, 0.0, 0.62), (0, -0.04, 0.72)], [0.1, 0.1]), HAIR, "head")
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        lock(k, f"curtain{side}", np.array([(0.1, -0.15, 0.8), (0.12, -0.17, 0.7), (0.105, -0.17, 0.6),
                                            (0.07, -0.17, 0.52)]) * m, [0.045, 0.046, 0.036, 0.02], (0, 1, 0), HAIR,
             "head")
    k.add("face", ell(F, FR, deform=chinned), GREEN, "head", surface="skin")

    # a crisp edge where the face comes out of the hood (the parts' colours blend over a band there)
    def in_face(P):
        q = (P - F) / FR
        q[:, 0] /= 1 - 0.45 * np.clip(-q[:, 2], 0, 1)
        return np.linalg.norm(q, axis=1)
    k.paint(lambda P: inside(1.04 - in_face(P), 0.004), HAIR, on=["face", "hood", "curtain"])
    k.paint(lambda P: inside(in_face(P) - 1.04, 0.004), GREEN, on=["face", "hood", "curtain"])

    # the horn of a nose, green, rising between the eyes
    NB = F + np.array([0, -0.045, 0.0])
    NS = 1.25                                    # the horn grown with the face
    k.add("nose", hull([NB + NS * np.array((0.022, 0.01, -0.025)), NB + NS * np.array((-0.022, 0.01, -0.025)), NB + NS * np.array((0, 0.012, 0.03)),
                        NB + NS * np.array((0.016, -0.02, 0.0)), NB + NS * np.array((-0.016, -0.02, 0.0)), NB + NS * np.array((0, -0.035, -0.012)),
                        NB + NS * np.array((0.008, -0.05, 0.06)), NB + NS * np.array((-0.008, -0.05, 0.06)), NB + NS * np.array((0, -0.06, 0.15))]),
          GREEN, "head", fuse=False, surface="skin")

    # the ears: tall green points lined red, standing up from the hood
    for s, side in ((1, "R"), (-1, "L")):
        base, nrm = on_head(s * 0.85, 0.62, HOOD, HOODR)
        ear(k, f"ear{side}", base - nrm * 0.03, (s * 0.32, 0.04, 0.95), (s * 0.3, -0.95, 0.05), 0.17, 0.05, 0.017,
            GREEN, LINING, "head", cup=0.45, curl=0.05, surface="skin")

    # the mouth: wide open, red, a few big fangs above and below
    p, n = on_head(0.0, -0.44, F, FR)
    k.decal(p, unit(0, -1, -0.3), (0.054, 0.04), srgb(MOUTH), eps=0.003, rings=12, segs=72,
            outline=polygon([(-1, 0.6), (-0.5, 0.75), (0, 0.65), (0.5, 0.75), (1, 0.6), (0.7, -0.4),
                             (0.3, -0.85), (0, -0.95), (-0.3, -0.85), (-0.7, -0.4)]))
    for j, (a, e, dz) in enumerate([(-0.45, -0.2, -1), (0.0, -0.17, -1), (0.45, -0.2, -1), (-0.28, -0.72, 1),
                                    (0.28, -0.72, 1)]):
        fp, fn = on_head(a, e, F, FR)
        k.add(f"tooth{j}", fang(fp + fn * 0.001, (0, -0.5, dz), 0.042, 0.026, 0.016, toward=fn), TEETH, "head",
              fuse=False, surface="shell")

    k.build(voxel=0.0085, smooth=4, band=0.008, wband=0.07)

    # --- the eyes: small, slanting down to the nose, white with a red pupil; the lid the face's green ----------
    almond = [(1.0, 0.3), (0.5, 0.8), (-0.2, 0.45), (-1.0, -0.15), (-0.6, -0.7), (0.3, -0.85), (0.95, -0.35)]
    lay = imp.lay_eyes(k, F, FR, 0.55, 0.12, (0.028, 0.025), WHITE, PUPIL, GREEN, LIDLINE, pupil_r=0.42,
                       pupil_dv=-0.12, outline=almond)
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    grimmsnarl(*args())
