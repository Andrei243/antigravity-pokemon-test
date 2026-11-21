# Cetitan sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cetitan.py -- <the export's Cetitan.glb> <out.glb>
#
# Grown from cetoddle.py: its eye's way, its flipper with the pink pad, its fluke, horn and leg. From the references: a
# great white whale on four short thick legs, the body one huge ball that is also its head (the largest mass by far).
# Five horns of pale ice: a great one on the midline of the face pointing forward and a little up, a pair on the upper
# face above the eyes pointing up and forward, a pair on the upper back. A grey patch round each upper horn's root over
# the face's sides; small fierce eyes in pink rings at the patch's outer edge, heavy-lidded. The lower half of the ball
# grey, crossed by pale seams, with round white spots. A broad flipper springs from each front shoulder, out and down,
# its end pink; grey two-toed claws on the feet; a tail rising behind to a fluke of two upright lobes, each with a pink
# stripe. The art spreads the flippers wide; here they are carried out, a little back and down, so the width stays near the
# height. Mapping: the front legs carry the flippers (on the same bones: each flipper springs from above its leg, as the
# pictures show), the hind legs are tucked under the rear of the ball, where the pictures hide them.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, args, sibling

cd = sibling("cetoddle")
WHITE, GREY, SEAM, SPOT, PINK, INK = "#F7F6FA", "#B4B2C0", "#DCDAE4", "#F4F3F8", cd.PINK, cd.INK
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
    # Four short thick legs, the belly low over them; grey claws on the feet
    for name, x, y, bone in (("leg", 0.165, -0.125, "leg{s}"), ("hind", 0.125, 0.09, "hind{s}")):
        k.add(name, tube([(x - 0.01, y, 0.17), (x, y - 0.01, 0.095)], [0.088, 0.08], seg=28), WHITE, bone, mirror=True)
        k.add(name + "foot", ell((x, y - 0.02, 0.045), (0.085, 0.092, 0.045)), WHITE, bone, mirror=True)
        for dx in (-0.025, 0.025):
            x0, y0 = x + dx, y - 0.02 - 0.075
            k.add(f"{name}claw{'a' if dx < 0 else 'b'}", hull([(x0 - 0.017, y0 + 0.03, 0.0), (x0 + 0.017, y0 + 0.03, 0.0),
                                                               (x0 - 0.014, y0 + 0.03, 0.03), (x0 + 0.014, y0 + 0.03, 0.03),
                                                               (x0, y0 - 0.022, 0.0), (x0, y0 - 0.01, 0.012)]),
                  CLAW, bone, fuse=False, mirror=True, surface="shell")
    k.paint(lambda P: inside(P[:, 2] - 0.07, 0.02) * 0.6, GREY, on=["leg", "hind"])
    # The flippers from the front shoulders: flat, broad at the root and tapering, out, a little back and down, pink ends
    def taper(V):
        V = V.copy(); V[:, 0] *= 1.0 - 0.6 * np.clip(V[:, 1], 0, 1) ** 1.3; return V
    fd = unit(0.82, 0.12, -0.42)
    fc = np.array([0.2, -0.11, 0.26]) + fd * 0.095 * 0.9
    k.add("flipper", cd.flat(fc, fd, (0.095, 0.062, 0.016), up=(0, -0.35, 1.0), roll=-18, deform=taper), WHITE,
          "leg{s}", mirror=True)
    tipc = fc + fd * 0.06
    for sx in (1, -1):
        t = tipc * np.array([sx, 1, 1])
        k.paint(lambda P, t=t: inside(np.linalg.norm(P - t, axis=1) - 0.035, 0.003), PINK, on=["flipper"])
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
        return inside(P[:, 2] - (0.27 - 0.03 * np.clip(P[:, 1] / 0.2, -1, 1)), 0.004)
    k.paint(lower, GREY, on=["body", "face"])
    def seams(P):
        q = P - B
        ang = np.degrees(np.arctan2(q[:, 0], -q[:, 1]))
        r = np.hypot(q[:, 0], q[:, 1])
        m = np.zeros(len(P))
        for a0 in (-150, -105, -60, -20, 20, 60, 105, 150):
            m = np.maximum(m, inside(np.abs(np.radians(ang - a0)) * r - 0.0035, 0.002))
        m = np.maximum(m, inside(np.abs(P[:, 2] - 0.165) - 0.0035, 0.002))
        return m * lower(P) * (P[:, 2] > 0.09) * inside(P[:, 2] - 0.258, 0.002)
    k.paint(seams, SEAM, on=["body", "face"])
    for d, half in (((0.55, -0.72, -0.34), (0.04, 0.036)), ((0.97, 0.1, -0.18), (0.034, 0.032)),
                    ((0.7, 0.55, -0.4), (0.03, 0.028)), ((0.0, -0.94, -0.35), (0.034, 0.03))):
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
