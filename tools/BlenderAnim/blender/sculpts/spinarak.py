# Spinarak sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/spinarak.py -- <the export's Spinarak.glb> <out.glb>
#
# From the references: a round green spider, a big domed abdomen behind and a smaller round green head in front.
# On the abdomen's top a face drawn in dark teal, read from the front: a straight line of a mouth nearest the head,
# two dark eyes behind it and a dark band across the back. A white horn rising from the head's crown, leaning
# forward; two red mandibles hanging under the face and hooking inward; two round eyes, white in a heavy black rim
# with a black pupil. Six thick legs, three a side, knees raised, banded dark teal and yellow, each ending in a
# yellow point.
#
# Written to be grown from (ariados.py imports it): the leg, the horn, the eye and the abdomen's marks are functions.
# Skeleton: body (0, 0, 0.15), head (0, -0.1, 0.14), legs (+-0.1, -0.06 / 0.02 / 0.1, 0.15) on leg, mid, hind.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, srgb, unit, eye_look, args

GREEN, MARK, TEAL, YELLOW = "#A6D46C", "#1D4E58", "#1F5A68", "#F2D34C"
HORN, MANDIBLE, WHITE, INK = "#EEEEF0", "#E8505E", "#F8F8F6", "#141418"


# --- the line's pieces ---------------------------------------------------------------------------------------------
def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) in direction d from its centre, and its outward normal."""
    c, r, d = np.array(c, float), np.array(r, float), unit(*d)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def leg(k, name, bone, hip, knee, tip, r, upper, bands, point):
    """A banded leg written on +X and mirrored: hip to a raised knee in one colour, then from the knee to the tip in
    bands, each a tube of its own meeting the next at a flat crisp edge ([(share of the way, colour)...]), the last
    a cone to the point."""
    hip, knee, tip = np.array(hip, float), np.array(knee, float), np.array(tip, float)
    k.add(f"{name}up", tube([hip, (hip + knee) / 2 + np.array([0, 0, 0.01]), knee], [r, r, r * 0.95]), upper, bone,
          fuse=False, mirror=True)
    t0, rr = 0.0, r * 0.88
    for i, (t1, col) in enumerate(bands):
        a, b = knee + (tip - knee) * t0, knee + (tip - knee) * t1
        r1 = r * (0.95 - 0.2 * t1)
        k.add(f"{name}b{i}", tube([a, (a + b) / 2, b], [rr, (rr + r1) / 2, r1]), col, bone, fuse=False, mirror=True)
        t0, rr = t1, r1 * 0.93
    a = knee + (tip - knee) * t0
    d = tip - a
    k.add(f"{name}pt", lathe(a, d, [(0, -rr * 0.6), (rr, 0.0), (rr * 0.82, np.linalg.norm(d) * 0.4),
                                    (0, np.linalg.norm(d))]), point, bone, fuse=False, mirror=True)


def horn(k, base, d, length, r, color=HORN, bone="head"):
    """A white horn: a cone from inside the crown along d."""
    k.add("horn", lathe(np.array(base, float), unit(*d), [(0, -0.01), (r, 0.0), (r * 0.72, length * 0.45),
                                                         (0, length)]), color, bone, fuse=False, surface="shell")


def ringed_eye(white=WHITE, rim=INK, pupil=INK, rim_w=0.3, pupil_r=0.33):
    """The line's eye: white in a heavy black rim, a round black pupil in the middle."""
    CW, CR, CP = srgb(white), srgb(rim), srgb(pupil)
    def eye(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(CW, (len(u), 1))
        c[r > 1 - rim_w] = CR
        c[(u + 0.04) ** 2 + (v + 0.02) ** 2 < pupil_r ** 2] = CP
        return c
    return eye


# --- the frame -----------------------------------------------------------------------------------------------------
AB, ABR = np.array([0.0, 0.05, 0.158]), np.array([0.15, 0.162, 0.135])      # the abdomen
HD, HDR = np.array([0.0, -0.13, 0.13]), np.array([0.1, 0.082, 0.085])       # the head
LEGS = [  # bone, hip, knee, tip (on +X)
    ("leg{s}", (0.09, -0.06, 0.15), (0.17, -0.12, 0.2), (0.2, -0.21, 0.004)),
    ("mid{s}", (0.11, 0.02, 0.15), (0.2, 0.01, 0.21), (0.265, 0.0, 0.004)),
    ("hind{s}", (0.09, 0.1, 0.15), (0.17, 0.16, 0.2), (0.2, 0.23, 0.004)),
]


def spinarak(src, out):
    k = Kit(src, coat="shell")

    # ---- the abdomen and the head, both green and round
    k.add("abdomen", ell(AB, ABR), GREEN, "body")
    k.add("head", ell(HD, HDR), GREEN, "head")
    k.add("waist", ell((0, -0.06, 0.13), (0.085, 0.06, 0.07)), GREEN, "body")

    # ---- the face on the abdomen: a mouth line, two eyes behind it, a band across the back
    p, n = on_ell(AB, ABR, (0, -0.2, 1))
    k.decal(p, n, (0.085, 0.016), MARK, up=(0, 1, 0), eps=0.004, rings=8, segs=48,
            outline=lambda th: 1.0 / max(abs(math.cos(th)) ** 6 + abs(math.sin(th)) ** 6, 1e-6) ** (1 / 6))
    for s in (1, -1):
        p, n = on_ell(AB, ABR, (0.42 * s, 0.38, 1))
        k.decal(p, n, (0.034, 0.022), MARK, up=(0, 1, 0), eps=0.004, rings=10, segs=40)
    p, n = on_ell(AB, ABR, (0, 1, 0.8))
    k.decal(p, n, (0.12, 0.02), MARK, up=(0, 0, 1), eps=0.004, rings=8, segs=48,
            outline=lambda th: 1.0 / max(abs(math.cos(th)) ** 6 + abs(math.sin(th)) ** 6, 1e-6) ** (1 / 6))

    # ---- the horn on the crown, the mandibles under the face
    horn(k, (0, -0.15, 0.198), (0, -0.35, 1), 0.075, 0.022)
    k.add("mandible", tube([(0.045, -0.185, 0.075), (0.053, -0.215, 0.05), (0.046, -0.224, 0.03),
                            (0.032, -0.217, 0.022)], [0.016, 0.015, 0.011, 0.006]), MANDIBLE, "head", fuse=False,
          mirror=True)

    # ---- six banded legs
    for bone, hip, knee, tip in LEGS:
        leg(k, bone[:3], bone, hip, knee, tip, 0.022, TEAL, [(0.32, YELLOW), (0.64, TEAL)], YELLOW)

    k.build(voxel=0.005, smooth=4)

    # ---- the eyes: wide apart on the head's front
    eye = ringed_eye()
    def eyes(state):
        p, n = on_ell(HD, HDR, (0.5, -0.85, 0.2))
        k.decal(p, n, (0.034, 0.034), eye_look(state, eye, srgb(GREEN), srgb(INK), line=0.17), eps=0.004, rings=20,
                segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    spinarak(*args())
