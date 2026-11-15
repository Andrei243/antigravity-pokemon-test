# Ledyba sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ledyba.py -- <the export's Ledyba.glb> <out.glb>
#
# From the references: a round ladybird, an egg of a body leaning a little back, red-orange all round its back and
# sides, a cream belly down its front split by a fine seam; black spots on its back. A broad red head on the egg's
# top front, a cream muzzle round its lower half; two great eyes bulging on its brow, white with a heavy black rim and
# a large black pupil with a glint; two thick black antennae rising from the crown, tapering, bent outward. Six short
# blue-purple arms from the sides where the cream meets the red, each ending in a pale round pad with a disc pressed
# on its face. The art holds one arm high; here all six are carried forward and a little out.
#
# Written to be grown from (ledian.py imports it): the colours, the eye, the antenna, the arm and its pad, the spots.
# Skeleton: body (0, 0, 0.226), head (0, -0.08, 0.246), arms (+-0.12, -0.02, 0.266 / 0.176 / 0.086).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, srgb, inside, unit, eye_look, polygon, args

RED, CREAM, SEAM, BLACK = "#E5482C", "#F2E0A2", "#B99A62", "#24212C"
LIMB, PAD, DISC = "#7378C8", "#DCE2DA", "#F2F5EE"
WHITE, PUPIL = "#F8F8F6", "#151418"
E = 0.004                                       # a crisp paint's edge


# --- the line's pieces ---------------------------------------------------------------------------------------------
def antenna(k, base, d, length, r0, r1, bone="head", color=BLACK):
    """A thick black antenna from base along d, bending outward toward its tip, tapering. Mirrored."""
    base, d = np.array(base, float), unit(*d)
    out = np.array([1.0, 0, 0])
    pts = [base - d * 0.02, base + d * length * 0.4, base + d * length * 0.75 + out * length * 0.08,
           base + d * length + out * length * 0.2]
    k.add("antenna", tube(pts, [r0, r0 * 0.9, (r0 + r1) / 2, r1]), color, bone, fuse=False, mirror=True)


def arm(k, i, root, hand, r, color=LIMB):
    """An arm from the body's side to a hand, both written on +X and mirrored, on bone arm{s}i."""
    root, hand = np.array(root, float), np.array(hand, float)
    mid = (root + hand) / 2 + np.array([0, 0, 0.006])
    k.add(f"arm{i}", tube([root, mid, hand], [r, r * 0.95, r]), color, f"arm{{s}}{i}", fuse=False, mirror=True)


def pad(k, i, c, d, r, ball=PAD, disc=DISC):
    """A round pad at a hand: a pale ball with a flat disc pressed on its face, turned along d. Mirrored."""
    c, d = np.array(c, float), unit(*d)
    k.add(f"pad{i}", ell(c, (r, r, r)), ball, f"arm{{s}}{i}", fuse=False, mirror=True)
    k.add(f"disc{i}", lathe(c + d * r * 0.55, d, [(0, 0), (r * 0.66, 0.0), (r * 0.68, r * 0.32), (r * 0.5, r * 0.5),
                                                  (0, r * 0.53)]), disc, f"arm{{s}}{i}", fuse=False, mirror=True)


def spot(k, c, n, half, color=BLACK, eps=0.004):
    """A round black spot on the shell, cast along -n."""
    k.decal(c, n, half, color, up=(0, 0, 1) if abs(unit(*n)[2]) < 0.8 else (0, 1, 0), eps=eps, rings=12, segs=40)


def round_eye(white=WHITE, rim=BLACK, pupil=PUPIL, pupil_r=0.5, rim_w=0.2, look=(-0.06, -0.02)):
    """The line's eye: a white disc in a heavy black rim, a large black pupil, a glint up and outward."""
    CW, CR, CP = srgb(white), srgb(rim), srgb(pupil)
    def eye(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(CW, (len(u), 1))
        c[r > 1 - rim_w] = CR
        c[(u - look[0]) ** 2 + (v - look[1]) ** 2 < pupil_r ** 2] = CP
        c[(u - look[0] - 0.17) ** 2 + (v - look[1] - 0.2) ** 2 < 0.16 ** 2] = CW
        return c
    return eye


def strip(k, side, z0, z1, w, color, n=5, B=None, BR=None):
    """A fine line down the egg's middle, front (side -1) or back (+1), from z0 to z1: short patches each cast along
    the surface's own normal, so none slips off the curve."""
    B = np.array([0.0, 0.03, 0.205]) if B is None else np.array(B, float)
    BR = np.array([0.19, 0.175, 0.205]) if BR is None else np.array(BR, float)
    zs = np.linspace(z0, z1, n + 1)
    for a, b in zip(zs[:-1], zs[1:]):
        z = (a + b) / 2
        t = np.clip((z - B[2]) / BR[2], -0.97, 0.97)
        y = B[1] + side * BR[1] * math.sqrt(1 - t * t)
        nrm = unit(0, (y - B[1]) / BR[1] ** 2, (z - B[2]) / BR[2] ** 2)
        k.decal((0, y, z), nrm, (w, (b - a) * 0.62), color, up=(0, 0, 1), eps=0.004, rings=3, segs=24,
                outline=polygon([(-1, 1), (1, 1), (1, -1), (-1, -1)]))


# --- the frame -----------------------------------------------------------------------------------------------------
B, BR = np.array([0.0, 0.03, 0.205]), np.array([0.19, 0.175, 0.205])     # the body's egg
H, HR = np.array([0.0, -0.075, 0.375]), np.array([0.145, 0.12, 0.098])    # the head
EYE = np.array([0.062, -0.14, 0.43])                                      # an eye's bulge (on +X)
ARMS = [  # (bone index, root on the body's side, hand)
    (0, (0.15, -0.07, 0.29), (0.19, -0.16, 0.305)),
    (1, (0.17, -0.06, 0.19), (0.215, -0.15, 0.185)),
    (2, (0.15, -0.06, 0.09), (0.19, -0.145, 0.06)),
]
PAD_R = 0.043


def ledyba(src, out):
    k = Kit(src, coat="shell")

    # ---- the body: an egg leaning a little back, red all round, a cream belly down its front
    def egg(V):
        V = V.copy()
        s = 1 + 0.05 * np.clip(-V[:, 2], 0, 1) - 0.08 * np.clip(V[:, 2], 0, 1) ** 2
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell(B, BR, rot=(-8, 0, 0), deform=egg), RED, "body")
    def belly(P):
        x, z = P[:, 0], P[:, 2]
        e = (x / 0.135) ** 2 + ((z - 0.17) / 0.185) ** 2
        return inside(np.sqrt(e) - 1, E) * (P[:, 1] < -0.03)
    k.paint(belly, CREAM, on=["body"])
    # the seam down the belly
    strip(k, -1, 0.03, 0.31, 0.0045, SEAM)

    # ---- the head: broad, a cream muzzle round its lower front, the eyes bulging on its brow
    k.add("head", ell(H, HR, deform=lambda V: V * np.array([1, 1, 1.0]) + np.array([0, 0, 0.0])), RED, "head")
    k.add("muzzle", ell(H + np.array([0, -0.035, -0.035]), (0.13, 0.095, 0.07)), CREAM, "head")
    k.add("neck", ell((0, -0.05, 0.31), (0.11, 0.09, 0.06)), CREAM, "head")
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"bulge{side}", ell(EYE * np.array([s, 1, 1]), (0.054, 0.045, 0.052)), RED, "head")
    # the muzzle's crisp edge: cream below a line round the head's front, red above it
    def muzzle(P):
        z0 = H[2] - 0.005 + 0.03 * np.abs(P[:, 0]) / 0.14
        return inside(P[:, 2] - z0, E) * (P[:, 1] < H[1] + 0.03)
    k.paint(muzzle, CREAM, on=["head", "muzzle", "neck"])
    k.paint(lambda P: inside(-(P[:, 2] - (H[2] - 0.005 + 0.03 * np.abs(P[:, 0]) / 0.14)), E) * (P[:, 1] < H[1] + 0.03),
            RED, on=["head", "muzzle", "bulge"])

    # ---- the spots on the back: a black seam where the shell parts, two big spots a side
    strip(k, 1, 0.06, 0.37, 0.008, BLACK)
    for s in (1, -1):
        for (x, y, z, h) in ((0.1, 0.16, 0.3, 0.052), (0.15, 0.1, 0.13, 0.046)):
            n = unit(x * s, y, (z - B[2]) * 0.8)
            spot(k, (x * s, y, z), n, (h, h))

    # ---- six short arms, each to a pale pad
    for i, root, hand in ARMS:
        arm(k, i, root, hand, 0.017)
        d = unit(*(np.array(hand) - np.array(root)))
        pad(k, i, np.array(hand) + d * PAD_R * 0.6, d + np.array([0, -0.3, 0]), PAD_R)

    # ---- the antennae: thick, black, rising from the crown and bending outward
    antenna(k, (0.045, -0.04, 0.445), (0.25, 0.15, 1), 0.12, 0.024, 0.009)

    k.build(voxel=0.006, smooth=4)

    # ---- the eyes: great round ones on the bulges, looking forward
    EN = unit(0.3, -0.85, 0.42)
    eye = round_eye()
    def eyes(state):
        k.decal(EYE + EN * 0.04, EN, (0.05, 0.05), eye_look(state, eye, srgb(RED), srgb(BLACK), line=0.17), eps=0.004,
                rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    ledyba(*args())
