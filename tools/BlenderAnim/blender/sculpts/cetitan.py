# Cetitan sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cetitan.py -- <the export's Cetitan.glb> <out.glb>
#
# Grown from cetoddle.py: its eye's way, its fluke and horn. From the references: a great white whale, the body one huge
# ball that is also its head (the largest mass by far), standing on two short thick legs under its middle, each with
# two grey claws. Five horns of pale ice: a great one on the midline of the face pointing forward and a little up, a
# pair on the upper face above the eyes pointing up and forward, a pair on the upper back. A grey patch round each upper
# horn's root over the face's sides; small fierce eyes in pink rings at the patch's outer edge, heavy-lidded. The ball
# white above and grey below, the grey crossed by pale seams, with large round white spots. Two long flat flippers from
# the ball's sides at mid-height, pink near their ends; a tail rising behind to a fluke of two lobes, each with a pink
# stripe. The art spreads the flippers wide; here they are held out, down and a little forward, so the width stays
# within 1.15 of the height. Mapping: the legs are on the skeleton's hind{s} bones, brought forward under the ball;
# the flippers are on its leg{s} bones; nothing stands under the front.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, unit, args, sibling

cd = sibling("cetoddle")
WHITE, GREY, SEAM, SPOT, PINK, INK = "#F7F6FA", "#ACAABA", "#DCDAE4", "#F4F3F8", cd.PINK, cd.INK
ICE, CLAW = "#DCE0EA", "#A8A6B2"

B, BR = np.array([0.0, 0.0, 0.29]), np.array([0.24, 0.245, 0.23])
FACE_C, FACE_R = np.array([0.0, -0.07, 0.3]), np.array([0.215, 0.185, 0.2])


def eye_open(u, v):
    """A small heavy-lidded eye: white, a black pupil toward the front, the lid's dark line slanting down inward."""
    c = np.tile(np.array((1.0, 1.0, 1.0)), (len(u), 1))
    c[((u + 0.22) / 0.34) ** 2 + ((v + 0.05) / 0.46) ** 2 < 1] = srgb(INK)
    c[((u + 0.12) / 0.12) ** 2 + ((v - 0.16) / 0.13) ** 2 < 1] = (1.0, 1.0, 1.0)
    edge = 0.42 + 0.28 * u
    c[v > edge] = srgb(PINK)
    c[np.abs(v - edge) < 0.16] = srgb(INK)
    c[(u * u + v * v > 0.8) & (v <= edge)] = srgb(INK)
    return c


def cetitan(src, out):
    k = Kit(src, coat="fur")
    k.add("body", ell(B, BR, sub=5), WHITE, "body")
    k.add("face", ell(FACE_C, FACE_R, sub=5), WHITE, "head")
    # Two short thick legs under the ball's middle, so it balances on them, each with two grey claws; the belly low
    LX, LY = 0.12, -0.01
    k.add("leg", tube([(LX - 0.01, LY, 0.16), (LX, LY - 0.005, 0.085)], [0.072, 0.066], seg=28), WHITE, "hind{s}",
          mirror=True)
    k.add("legfoot", ell((LX, LY - 0.015, 0.04), (0.07, 0.08, 0.04)), WHITE, "hind{s}", mirror=True)
    for dx in (-0.024, 0.024):
        x0, y0 = LX + dx, LY - 0.015 - 0.065
        k.add(f"claw{'a' if dx < 0 else 'b'}", hull([(x0 - 0.017, y0 + 0.03, 0.0), (x0 + 0.017, y0 + 0.03, 0.0),
                                                     (x0 - 0.014, y0 + 0.03, 0.03), (x0 + 0.014, y0 + 0.03, 0.03),
                                                     (x0, y0 - 0.022, 0.0), (x0, y0 - 0.01, 0.012)]),
              CLAW, "hind{s}", fuse=False, mirror=True, surface="shell")
    k.paint(lambda P: inside(P[:, 2] - 0.06, 0.02) * 0.6, GREY, on=["leg"])
    # The flippers: long flat blades (loose, so they stay thin) from the ball's sides at mid-height, held out, down and a
    # little forward, their broad faces turned to the front and the sides; a pink band across each near its end, on
    # both faces (a thicker plate of the band's outline)
    FL, FW = 0.28, 0.095
    fd = unit(0.45, -0.32, -0.83)
    face = unit(0.6, -0.8, 0.05)
    across = np.cross(face, fd); across /= np.linalg.norm(across)
    root = np.array([0.205, -0.03, 0.3])
    blade_ = [(0.0, -FW * 0.42), (FL * 0.35, -FW * 0.5), (FL * 0.75, -FW * 0.4), (FL, 0.0), (FL * 0.75, FW * 0.42),
              (FL * 0.35, FW * 0.5), (0.0, FW * 0.42)]
    k.add("flipper", slab(blade_, root, fd, across, 0.02), WHITE, "leg{s}", fuse=False, mirror=True)
    band = [(FL * 0.68, -FW * 0.44), (FL * 0.8, -FW * 0.36), (FL * 0.8, FW * 0.36), (FL * 0.68, FW * 0.44)]
    k.add("flipperband", slab(band, root, fd, across, 0.026), PINK, "leg{s}", fuse=False, mirror=True)
    tip = [(FL * 0.9, -FW * 0.2), (FL * 0.995, -FW * 0.02), (FL * 0.995, FW * 0.02), (FL * 0.9, FW * 0.2)]
    k.add("flippertip", slab(tip, root, fd, across, 0.026), PINK, "leg{s}", fuse=False, mirror=True)
    k.add("shoulder", ell((0.2, -0.03, 0.29), (0.05, 0.06, 0.06)), WHITE, "leg{s}", mirror=True)
    # The tail rising behind, and its fluke of two upright lobes, each with a pink stripe
    k.add("tailstem", tube([(0, 0.18, 0.31), (0, 0.26, 0.33), (0, 0.31, 0.35)], [0.11, 0.065, 0.042], seg=28), WHITE,
          "tail")
    root = np.array([0, 0.31, 0.35])
    cd.fluke(k, root, (0.74, 0.52, 0.42), (0.12, 0.052, 0.018), "tail", spot=False)
    for sx in (1, -1):
        d = unit(0.74 * sx, 0.52, 0.42)
        c = root + d * 0.12 * 0.9
        def stripe(P, c=c, d=d):
            q = P - c
            a = q @ d
            off = np.linalg.norm(q - np.outer(a, d), axis=1)
            return inside(off - 0.022, 0.004) * inside(np.abs(a + 0.01) - 0.085, 0.004)
        k.paint(stripe, PINK, on=[f"fluke{'RL'[sx < 0]}"])
    # The lower half of the ball grey, crossed by pale seams, with white spots
    def lower(P):
        return inside(P[:, 2] - (0.29 - 0.03 * np.clip(P[:, 1] / 0.2, -1, 1)), 0.004)
    k.paint(lower, GREY, on=["body", "face"])
    def seams(P):
        q = P - B
        ang = np.degrees(np.arctan2(q[:, 0], -q[:, 1]))
        r = np.hypot(q[:, 0], q[:, 1])
        m = np.zeros(len(P))
        for a0 in (-150, -105, -60, -20, 20, 60, 105, 150):
            m = np.maximum(m, inside(np.abs(np.radians(ang - a0)) * r - 0.0035, 0.002))
        m = np.maximum(m, inside(np.abs(P[:, 2] - 0.165) - 0.0035, 0.002))
        return m * lower(P) * (P[:, 2] > 0.09) * inside(P[:, 2] - 0.275, 0.002)
    k.paint(seams, SEAM, on=["body", "face"])
    for d, half in (((0.55, -0.72, -0.3), (0.056, 0.05)), ((0.97, 0.12, -0.22), (0.048, 0.045)),
                    ((0.68, 0.55, -0.42), (0.042, 0.04)), ((0.0, -0.94, -0.3), (0.048, 0.042))):
        d = unit(*d)
        k.decal(cd.at_surface(B, BR, d), d, half, SPOT, eps=0.0025, rings=12, segs=44, mirror=d[0] != 0)
    # The grey patch round each upper horn's root, over the face's sides
    SH = unit(0.36, -0.7, 0.62)
    for sx in (1, -1):
        hc = cd.at_surface(FACE_C, FACE_R, SH * (sx, 1, 1))
        k.paint(lambda P, hc=hc: inside(np.linalg.norm((P - hc) / (0.085, 0.085, 0.1), axis=1) - 1, 0.04), GREY,
                on=["face", "body"])
    # The horns: the great one forward on the midline, the pair above the eyes, the pair on the back
    FH = unit(0, -0.95, 0.2)
    cd.horn(k, "hornfront", cd.at_surface(FACE_C, FACE_R, FH), unit(0, -1, 0.62), 0.17, 0.052, "head", color=ICE)
    cd.horn(k, "hornside", cd.at_surface(FACE_C, FACE_R, SH), unit(0.3, -0.5, 0.82), 0.12, 0.04, "head", color=ICE,
            mirror=True)
    BH = unit(0.45, 0.42, 0.79)
    cd.horn(k, "spike", cd.at_surface(B, BR, BH), unit(0.42, 0.4, 0.82), 0.11, 0.038, "body", color=ICE, mirror=True)
    # The pink ring round each eye (in every state)
    ED = unit(0.72, -0.6, 0.26)
    E = [(cd.at_surface(FACE_C, FACE_R, ED * (sx, 1, 1)), ED * (sx, 1, 1), sx) for sx in (1, -1)]
    for at, d, sx in E:
        k.decal(at, d, (0.046, 0.04), PINK, eps=0.0018, rings=14, segs=48)
    k.build(voxel=0.006, smooth=4, band=0.008, wband=0.05)
    k.eye_states(lambda st: cd.eyes(k, E, (0.031, 0.026), st, lid=PINK, open_look=eye_open, eps=0.0032))
    k.export(out)


if __name__ == "__main__":
    cetitan(*args())
