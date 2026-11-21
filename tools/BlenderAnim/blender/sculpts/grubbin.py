# Grubbin sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/grubbin.py -- <the export's Grubbin.glb> <out.glb>
#
# From the references: a grub lying low along the ground. A pale blue-white body of three rolls, the first a great
# hood that rises over the back of the head, the others smaller toward the tail, small round nubs of feet under them
# and yellow ovals low on their sides. The head in front is brown, under an orange cap edged by a yellow band that
# overhangs the face like a brim. Big upright oval eyes low on the sides of the face, white in a black rim with a
# black upright pupil. Two great jaws reach forward from under the cap's front corners: a yellow sleeve at the root,
# an orange point beyond it.
#
# Written to be grown from (charjabug.py takes its colours, on_ell and jaw): the colours and the frame are constants,
# the build is under __main__.
# Skeleton: body (0, 0.12, 0.035), seg1 (0, 0.07, 0.038), seg2 (0, 0.02, 0.042), seg3 (0, -0.03, 0.048),
# head (0, -0.06, 0.048).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, outward, args

WHITE, WHITE_D, BROWN, ORANGE, YELLOW = "#E4E9F2", "#C9D1E0", "#7A4A2C", "#E8702E", "#F6D27A"
INK, EYE_W, FOOT = "#141418", "#FFFFFF", "#D6DCE8"

HC = np.array([0.0, -0.072, 0.054])          # the head's middle
HR = np.array([0.049, 0.052, 0.05])          # its radii
CAP_C = HC + np.array([0.0, 0.006, 0.006])   # the cap over it, its lower half drawn in to a brim
CAP_R = np.array([0.055, 0.058, 0.05])
RIM = 0.05                                  # the brim's height at the cap's middle
BAND = 0.017                                 # the yellow band's height over the brim
VOXEL = 0.0016

# The rolls of the body, front to back: bone, centre, radii
ROLLS = [("seg3", (0, -0.016, 0.06), (0.054, 0.048, 0.06)),
         ("seg2", (0, 0.04, 0.046), (0.05, 0.034, 0.046)),
         ("seg1", (0, 0.08, 0.033), (0.042, 0.028, 0.033))]


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) in direction d from its centre, and its outward normal."""
    c, r, d = np.array(c, float), np.array(r, float), unit(*d)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def jaw(k, name, root, tip, radius, bone, sleeve=0.55, base=YELLOW, point=ORANGE, surface="shell"):
    """A jaw from root to tip: a round sleeve of base colour over the root's share, a cone of point colour beyond it
    reaching out of the sleeve (a little thinner, so the sleeve's rim shows)."""
    root, tip = np.array(root, float), np.array(tip, float)
    mid = root + (tip - root) * sleeve
    bend = np.array([0, 0, radius * 0.25])
    k.add(f"{name}sleeve", outward(tube([root, (root + mid) / 2 + bend * 0.5, mid],
                                       [radius, radius * 1.02, radius * 0.97], seg=20, per=6)),
          base, bone, fuse=False, surface=surface)
    start = root + (tip - root) * (sleeve - 0.08)
    k.add(f"{name}point", outward(tube([start, (start + tip) / 2 + bend, tip],
                                       [radius * 0.86, radius * 0.55, radius * 0.08], seg=20, per=8)),
          point, bone, fuse=False, surface=surface)


def eye_open(u, v):
    """White in a black rim, a black upright pupil a little toward the front (u runs to the back), a white glint."""
    r = np.sqrt(u * u + v * v)
    c = np.tile(srgb(EYE_W), (len(u), 1))
    c[r > 0.76] = srgb(INK)
    c[((u + 0.12) / 0.34) ** 2 + ((v + 0.02) / 0.58) ** 2 < 1] = srgb(INK)
    c[((u + 0.2) / 0.11) ** 2 + ((v - 0.24) / 0.16) ** 2 < 1] = srgb(EYE_W)
    return c


EYE_D, EYE_HALF = (0.78, -0.5, -0.3), (0.016, 0.022)


def grubbin(src, out_path):
    k = Kit(src, coat="shell")

    # --- the head: brown, under a cap whose lower half is drawn in to a brim over the face -------------------------
    k.add("head", ell(HC, HR), BROWN, "head")
    def brim(V):
        V = V.copy()
        V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.22, V[:, 2])
        return V
    k.add("cap", ell(CAP_C, CAP_R, deform=brim), YELLOW, "head")
    # orange over the band, the band's top a little lower at the front
    k.paint(lambda P: inside(RIM + BAND - P[:, 2], 0.0012), ORANGE, on=["cap"])

    # --- the body: a great hood over the back of the head, then smaller rolls to the tail --------------------------
    for i, (bone, c, r) in enumerate(ROLLS):
        k.add(f"roll{i}", ell(c, r), WHITE, bone)
    # small round nubs of feet under the rolls
    for i, (bone, y) in enumerate((("seg3", -0.03), ("seg3", 0.0), ("seg2", 0.04), ("seg1", 0.078))):
        k.add(f"foot{i}", ell((0.028, y, 0.007), (0.008, 0.009, 0.006), sub=3), WHITE, bone, mirror=True)
    # a soft shade down the seams between the rolls
    def seams(P):
        m = np.zeros(len(P))
        for y in (0.022, 0.064):
            m = np.maximum(m, inside(np.abs(P[:, 1] - y) - 0.0025, 0.0015))
        return m
    k.paint(seams, WHITE_D, on=["roll"])

    # --- the jaws, from under the cap's front corners -------------------------------------------------------------
    for s, side in ((1, "R"), (-1, "L")):
        jaw(k, f"jaw{side}", (0.021 * s, -0.104, 0.027), (0.019 * s, -0.215, 0.04), 0.0135, "head")

    # --- the yellow ovals low on the rolls' sides (in every state) -------------------------------------------------
    for (bone, c, r), d, half in ((ROLLS[0], (1, 0.25, -0.35), (0.012, 0.0095)),
                                  (ROLLS[1], (1, 0.1, -0.3), (0.011, 0.0085))):
        p, n = on_ell(c, r, d)
        k.decal(p, n, half, YELLOW, eps=0.0012, rings=10, segs=36, mirror=True)

    k.build(voxel=VOXEL, smooth=3, band=0.003, wband=0.02)

    def lay(state):
        p, n = on_ell(HC, HR, EYE_D)
        k.decal(p, n, EYE_HALF, eye_look(state, eye_open, srgb(BROWN), srgb(INK), line=0.16), up=(0, 0, 1),
                eps=0.0012, rings=20, segs=56, mirror=True)
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    grubbin(*args())
