# Karrablast sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/karrablast.py -- <the export's Karrablast.glb> <out.glb>
#
# From the references: one blue teardrop of shell, with no head of its own: its top draws out into a stalk that ends in
# a round knob with a notch cut into its top (a crescent opening upward). A dark grey face on the front of the egg's
# upper half, a pale blue lens of a mark at its top; angry yellow eyes slanting down to the middle, a black upright
# pupil each; a big round open mouth, dark red round a pink inside, with two small white fangs in its top. A yellow
# belly below the mouth, its plates parted by curved lines. Short dark grey stubs of arms pointing out and down and
# dark stubs of feet under the egg.
# The export's joints: body (0, 0, 0.15), head (0, 0, 0.24), arms (+-0.07, 0, 0.15), legs (+-0.04, 0, 0.06). About
# 0.35 tall.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, outward, srgb, inside, smoothstep, unit, polygon, eye_look, args

BLUE, DARK, STUB = "#2F78C8", "#3A3C45", "#44464F"
YELLOW, LINE, EYE, INK = "#F2C24A", "#B98A30", "#F4AE2C", "#15161B"
LENS, MOUTH, PINK, FANG = "#8FD4F2", "#5C1C28", "#E67884", "#F4F2EE"
SHUTLINE = "#9A9CA8"           # the closed eyes' line, light on the dark face

C = np.array([0, -0.004, 0.135])          # the egg's middle
R = np.array([0.104, 0.094, 0.128])       # and its radii


def teardrop(V):
    """The egg narrowing toward its top, where the stalk grows out of it."""
    V = V.copy()
    t = np.clip(V[:, 2], 0, 1)
    s = 1 - 0.38 * t ** 1.6
    V[:, 0] *= s; V[:, 1] *= s
    return V


def on_egg(d, c=C, r=R, lift=0.0):
    """The point of the (unshaped) egg in a direction: decal centres lie on it."""
    d = unit(*d)
    return c + d * (1 / math.sqrt(((d / r) ** 2).sum()) + lift)


# --- the eyes: angular, slanting down to the middle; an upright black pupil toward the inner corner ------------------
# Laid by hand, one a side: u runs toward +X on both, so the left eye's outline and colours are mirrored here
EYE_PTS = [(-1.0, -0.05), (-0.55, -0.62), (0.2, -0.9), (0.82, -0.62), (1.0, 0.1), (0.78, 0.95), (0.2, 0.62),
           (-0.45, 0.3)]


def eye_outline(s):
    return polygon([(u * s, v) for u, v in EYE_PTS])


def eye_open(u, v, eye=EYE):
    """u outward on both eyes."""
    c = np.tile(srgb(eye), (len(u), 1))
    c[((u + 0.18) / 0.24) ** 2 + ((v + 0.05) / 0.52) ** 2 < 1] = srgb(INK)
    return c


def eye_state(state, s, lid=DARK, ink=SHUTLINE, open_look=eye_open):
    look = eye_look(state, open_look, srgb(lid), srgb(ink), line=0.16)
    return lambda u, v: look(u * s, v)             # the patch's u runs to +X: outward on the right eye only


def belly(k, c, r, top, bottom, width, lines, on=None):
    """The yellow plates on the egg's front: a rounded shield from bottom to top over width radians either side, its
    top edge rising in the middle, parted by curved lines that dip at the middle."""
    def az(P):
        return np.arctan2(P[:, 0] - c[0], -(P[:, 1] - c[1]))
    def plate(P):
        a = az(P)
        edge = top - 0.035 * (a / width) ** 2
        return inside(np.abs(a) - width, 0.02) * inside(P[:, 2] - edge, 0.0022) * inside(bottom - P[:, 2], 0.004) \
            * (P[:, 1] < c[1] + 0.02)
    k.paint(plate, YELLOW, on=on)
    for z in lines:
        def line(P, z=z):
            a = az(P)
            return inside(np.abs(P[:, 2] - (z + 0.016 * (a / width) ** 2)) - 0.0022, 0.0016) * plate(P) \
                * inside(np.abs(a) - width * 0.92, 0.02)
        k.paint(line, LINE, on=on)


def karrablast(src, out):
    k = Kit(src, coat="shell")

    # --- the egg and its stalk with the notched knob -----------------------------------------------------------------
    k.add("egg", ell(C, R, deform=teardrop), BLUE, "body")
    k.add("stalk", tube([(0, 0.0, 0.22), (0, 0.004, 0.27), (0, 0.009, 0.305)], [0.05, 0.03, 0.021]), BLUE, "head")
    KN = np.array([0, 0.01, 0.318])
    arc = [KN + 0.027 * np.array([math.cos(math.radians(a)), 0, math.sin(math.radians(a))])
           for a in (38, 0, -45, -90, -135, -180, -218)]
    k.add("knob", tube(arc, [0.0095, 0.0115, 0.013, 0.014, 0.013, 0.0115, 0.0095]), BLUE, "head")

    # --- arms: short dark stubs pointing out and down; feet: dark stubs under the egg -------------------------------
    k.add("arm", outward(tube([(0.08, -0.004, 0.152), (0.118, -0.014, 0.138), (0.146, -0.022, 0.124)],
                              [0.03, 0.024, 0.014])), STUB, "arm{s}", fuse=False, mirror=True)
    k.add("foot", ell((0.046, -0.012, 0.018), (0.028, 0.034, 0.022)), STUB, "leg{s}", mirror=True)

    # --- the belly's plates ------------------------------------------------------------------------------------------
    belly(k, C, R, top=0.148, bottom=0.03, width=1.05, lines=(0.118, 0.088, 0.058), on=["egg"])

    # --- the face: the dark mask, the pale lens, the open mouth with its fangs ---------------------------------------
    df = unit(0, -1, 0.5)
    k.decal(on_egg(df), df, (0.066, 0.062), DARK, eps=0.002, rings=16, segs=60)
    dl = unit(0, -0.74, 1.0)
    k.decal(on_egg(dl, lift=-0.01), dl, (0.026, 0.012), LENS, eps=0.0032, rings=8, segs=40)
    CM, CP = srgb(MOUTH), srgb(PINK)

    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[(u / 0.74) ** 2 + ((v + 0.12) / 0.74) ** 2 < 1] = CP
        return c
    dm = unit(0, -1, 0.16)
    MC = on_egg(dm)
    k.decal(MC, dm, (0.041, 0.033), mouth, eps=0.0034, rings=29, segs=84)
    for s in (1, -1):
        b = MC + np.array([0.021 * s, 0.0, 0.023])
        k.add(f"fang{'R' if s > 0 else 'L'}", hull([b + (-0.006, -0.004, 0), b + (0.006, -0.004, 0),
                                                    b + (0, 0.004, 0), b + (0.0015 * s, -0.003, -0.016)]),
              FANG, "head", fuse=False)

    k.build(voxel=0.004, band=0.006)

    # --- the angry eyes --------------------------------------------------------------------------------------------
    def eyes(state):
        for s in (1, -1):
            de = unit(0.38 * s, -0.86, 0.6)
            k.decal(on_egg(de), de, (0.027, 0.02), eye_state(state, s), eps=0.0042, outline=eye_outline(s),
                    rings=27, segs=64)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    karrablast(*args())
