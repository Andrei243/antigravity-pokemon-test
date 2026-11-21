# Amoonguss sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/amoonguss.py -- <the export's Amoonguss.glb> <out.glb>
# Grown from foongus.py (sibling("foongus")): its cap and Poke Ball paint, its button, its nose and its colours' way.
#
# From the references: a broad cap like a Poke Ball laid on its side, mostly pink with a pale grey quarter at the front
# and the -X side, a brown band between them, a brown rim, grey-blue gills beneath; a few pink spots ringed darker on
# the pink. Under it a tall pale stem, a pear fullest low with a brown band round its foot, its top shaded blue-grey
# under the gills; a face just under the cap's rim: sleepy half-shut eyes, white below a heavy lid with a dark pupil
# low in each, and a big round pink nose with a dark dot under them. Two thin stalks of arms out and down to two great
# Poke Balls held as shields at its sides, pink above, white below, a brown band and a ringed button on the outer face.
# The skeleton: body (0, 0, 0.17), head (0, 0, 0.3), armR/armL (+-0.08, 0, 0.18).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, sibling

f = sibling("foongus")
PINK, GREY, BROWN, GILL, GILLINE = "#DC5C86", "#D7D9D0", f.BROWN, "#A3AABA", "#868DA0"
STEM, SHADE, SPOT, WHITE, INK = "#E0E3DA", "#B9C0CB", "#A9385E", "#F2F1EC", f.INK

CAP = np.array([0.0, 0.0, 0.29])
CAPR = np.array([0.19, 0.185, 0.155])
BODY = np.array([0.0, 0.0, 0.142])
BODYR = np.array([0.126, 0.116, 0.15])
UPPER = np.array([0.0, -0.004, 0.226])
UPPERR = np.array([0.078, 0.074, 0.075])
VOXEL = 0.0042


def pear(V):
    V = V.copy()
    z = V[:, 2]
    s = 1 + 0.04 * np.clip(-z, 0, 1) - 0.52 * np.clip(z + 0.15, 0, 1) ** 1.3
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.where(z < 0, z * 0.92, z)
    return V


def eye_open(u, v):
    """Sleepy: a heavy lid of the stem's colour over the top, a dark lid line, white below with a dark pupil low."""
    c = np.tile(srgb(WHITE), (len(u), 1))
    c[u * u + v * v > 0.84 ** 2] = srgb(INK)
    c[(u + 0.05) ** 2 + (v + 0.38) ** 2 < 0.34 ** 2] = srgb(INK)
    c[v > 0.18] = srgb(STEM)
    c[np.abs(v - 0.1) < 0.14] = srgb(INK)
    return c


def shield(k, s):
    """A great Poke Ball held at the side: a lens facing out and forward, pink above, white below."""
    side = "R" if s > 0 else "L"
    S = np.array([0.2 * s, -0.04, 0.115])
    A = unit(s, -0.85, 0.08)
    e1 = np.cross((0, 0, 1.0), A); e1 /= np.linalg.norm(e1)
    e2 = np.cross(A, e1)
    M = np.stack([e1, e2, A], axis=1)
    V, F = ell((0, 0, 0), (0.07, 0.07, 0.043))
    k.add(f"shield{side}", (V @ M.T + S, F), WHITE, f"arm{side}")
    def upper(P):
        q = P - S
        near = inside(np.linalg.norm(q, axis=1) - 0.08, 0.004)
        return inside(0.0075 - q @ e2, 0.0012) * near
    def band(P):
        q = P - S
        near = inside(np.linalg.norm(q, axis=1) - 0.08, 0.004)
        return inside(np.abs(q @ e2) - 0.0075, 0.0012) * near
    k.paint(upper, PINK, on=[f"shield{side}"])
    k.paint(band, BROWN, on=[f"shield{side}"])
    f.button(k, S + A * 0.043, A, 0.026, 0.017, up=tuple(e2))
    # the stalk of an arm, out and down from the shoulder into the shield's inner face
    k.add(f"stalk{side}", tube([(0.07 * s, -0.005, 0.185), (0.12 * s, -0.015, 0.16), (0.16 * s, -0.022, 0.13)],
                               [0.016, 0.014, 0.014]), STEM, f"arm{side}")


def spot(k, d):
    """A pink spot ringed darker on the cap."""
    p, n = f.on_ell(CAP, CAPR, d)
    CS, CP = srgb(SPOT), srgb(PINK)
    def look(u, v):
        c = np.tile(CS, (len(u), 1))
        c[u * u + v * v < 0.62 ** 2] = CP
        return c
    up = (0, 1, 0) if abs(n[2]) > 0.85 else (0, 0, 1)
    k.decal(p, n, (0.024, 0.015), look, up=up, eps=0.003, rings=12, segs=48)


def amoonguss(src, out):
    k = Kit(src, coat="fur")

    # ---- the stem: a tall pear fullest low, its upper part going up into the cap; a brown band round its foot
    k.add("body", ell(BODY, BODYR, deform=pear), STEM, "body")
    k.add("upper", ell(UPPER, UPPERR), STEM, "head")
    k.add("neck", tube([(0, 0, 0.24), (0, 0, 0.29)], [0.068, 0.064]), STEM, "head")
    k.paint(lambda P: inside(P[:, 2] - 0.024, 0.0015), BROWN, on=["body"])
    k.paint(lambda P: inside(0.264 - P[:, 2], 0.006) * (np.abs(P[:, 0]) < 0.1), SHADE, on=["neck", "upper"])

    # ---- the cap: pink with a pale grey quarter at the front and -X, gills beneath
    f.cap(k, CAP, CAPR)
    f.ball_paint(k, CAP, CAPR, (-0.72, -0.6, 0.35), 0.045, GREY, PINK, BROWN, GILL, 0.0085, 0.016)
    def gills(P):
        q = P - CAP
        th = np.arctan2(q[:, 1], q[:, 0])
        r = np.sqrt((q[:, 0] / CAPR[0]) ** 2 + (q[:, 1] / CAPR[1]) ** 2)
        return inside(np.abs(np.sin(14 * th)) - 0.16, 0.02) * inside(q[:, 2] + 0.006, 0.002) \
            * inside(r - 0.84, 0.01) * inside(0.42 - r, 0.02)
    k.paint(gills, GILLINE, on=["cap"])
    for d in ((0.5, -0.35, 0.78), (0.78, 0.25, 0.55), (-0.15, 0.55, 0.8), (0.1, 0.85, 0.45)):
        spot(k, d)

    # ---- the shields and their stalks
    for s in (1, -1):
        shield(k, s)

    # ---- the nose: big, round, pink, just under the eyes
    np_, nn = f.on_ell(UPPER, UPPERR, (0, -1, -0.34))
    f.nose(k, np_, nn, 0.024, "head")

    k.build(voxel=VOXEL, smooth=4, band=0.004, wband=0.05)

    # ---- the eyes: sleepy, heavy-lidded, just under the cap's rim
    ep, en = f.on_ell(UPPER, UPPERR, (0.5, -0.84, 0.17))
    CS, CK = srgb(STEM), srgb(INK)
    def eyes(state):
        k.decal(ep, en, (0.021, 0.022), eye_look(state, eye_open, CS, CK, line=0.16), eps=0.003, rings=20, segs=56,
                mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    amoonguss(*args())
