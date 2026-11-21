# Tynamo sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tynamo.py -- <the export's Tynamo.glb> <out.glb>
#
# From the references: a small pale fish like a tadpole, white with a cold blue cast; a thick round body, blunt and
# round in front, tapering behind to a point; a red four-pointed star on the front of the snout; a big black oval eye
# with a white glint on each side, well forward; a grey line round the body where the head ends; a yellow zigzag of
# light along each side from that line back to the tail's point; and round the rear of the body a pale blue fin
# fold, a leaf in the body's midline plane of which the white body is the thick core: a low ridge over and under the
# body behind the head's line, tallest two thirds of the way back, running past the tail's point into a wavy tip. The
# game looks from the front and back three-quarters, never straight on, so the leaf shows its faces to both.
# The skeleton is tiny (the export 0.114 tall, 0.37 long): head (0, -0.1, 0.06), body (0, 0, 0.05), tail (0.01, 0.13,
# 0.05). Every absolute size here is for that height.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, unit, eye_look, polygon, args, outward

WHITE, MEMBRANE, RED, YELLOW, GREY, INK = "#E8EFF3", "#C4D6E6", "#D9464C", "#F2D23A", "#9AAAB8", "#141418"

# The body's line, front to back: a blunt round head, a thick body, a point behind
HEAD = np.array([0.0, -0.104, 0.066])
HR = np.array([0.047, 0.056, 0.046])
LINE = [(0.0, -0.1, 0.066), (0.0, -0.06, 0.062), (0.0, 0.0, 0.058), (0.004, 0.06, 0.054), (0.008, 0.11, 0.051),
        (0.01, 0.15, 0.05)]
RADS = [0.044, 0.045, 0.046, 0.034, 0.018, 0.005]
SEAM = -0.048          # the grey line where the head ends


def centre_z(y):
    ys = [p[1] for p in LINE]; zs = [p[2] for p in LINE]
    return np.interp(y, ys, zs)


def zigzag_mask(s):
    """The yellow zigzag on the side s (+1 or -1): a stroke in (y, z) round the body's line."""
    pts = np.array([(-0.043, -0.013), (-0.032, 0.011), (-0.02, -0.006), (-0.006, 0.012), (0.008, -0.006),
                    (0.022, 0.009), (0.036, -0.005), (0.052, 0.008), (0.072, -0.003), (0.098, 0.003), (0.125, 0.0)])
    half = 0.0034

    def mask(P):
        y = P[:, 1]; z = P[:, 2] - centre_z(P[:, 1])
        Q = np.stack([y, z], 1)
        best = np.full(len(P), 9.0)
        for a, b in zip(pts[:-1], pts[1:]):
            e = b - a
            t = np.clip(((Q - a) @ e) / (e @ e), 0, 1)
            best = np.minimum(best, np.linalg.norm(Q - (a + t[:, None] * e), axis=1))
        return inside(best - half, 0.0008) * (P[:, 0] * s > 0.012)
    return mask


def membrane():
    """The fin fold: a solid leaf in the body's midline plane, a low ridge over and under the body just behind the
    head's line, tallest some two thirds of the way back and running past the tail's point into a wavy tip. The game
    looks from the front and back three-quarters, so its faces show to both."""
    top = [(-0.046, 0.104), (-0.02, 0.11), (0.02, 0.113), (0.07, 0.114), (0.11, 0.111), (0.145, 0.101),
           (0.17, 0.09), (0.185, 0.091), (0.205, 0.075), (0.222, 0.066), (0.235, 0.05), (0.25, 0.034)]
    bottom = [(0.232, 0.024), (0.212, 0.022), (0.195, 0.014), (0.172, 0.013), (0.14, 0.007), (0.1, 0.004),
              (0.06, 0.006), (0.02, 0.01), (-0.015, 0.014), (-0.046, 0.019)]
    return slab(top + bottom, (0.0, 0.0, 0.0), (0, 1.0, 0), (0, 0, 1.0), 0.0045)


def tynamo(src, out):
    k = Kit(src, coat="shell")

    # --- the body: a thick tube on its three bones, the head a round ball in front -----------------------------------
    k.add("head", ell(HEAD, HR), WHITE, "head")
    k.add("front", tube(LINE[:3], RADS[:3], seg=28), WHITE, "head")
    k.add("body", tube(LINE[1:5], RADS[1:5], seg=28), WHITE, "body")
    k.add("tail", tube(LINE[3:], [RADS[3], RADS[4], 0.0045], seg=28), WHITE, "tail")

    # the grey line round the body where the head ends
    k.paint(lambda P: inside(np.abs(P[:, 1] - SEAM) - 0.0016, 0.0007), GREY)
    # the yellow zigzags
    for s in (1, -1):
        k.paint(zigzag_mask(s), YELLOW)

    # --- the fin fold, a leaf round the body --------------------------------------------------------------------------------
    k.add("membrane", membrane(), MEMBRANE, "tail", fuse=False, surface="shell")

    # --- the red four-pointed star on the snout ---------------------------------------------------------------------
    star = []
    for i in range(16):
        a = math.pi / 2 + i * 2 * math.pi / 16
        r = 1.0 if i % 4 == 0 else (0.42 if i % 2 else 0.3)
        star.append((r * math.cos(a), r * math.sin(a)))
    sn = unit(0, -0.93, 0.36)
    k.decal(HEAD + sn * HR, sn, (0.024, 0.024), RED, eps=0.0007, outline=polygon(star), rings=14, segs=96)

    k.build(voxel=0.0019, smooth=7, band=0.002, wband=0.02)

    # --- the eyes: big black ovals with a white glint, forward on the head's sides -------------------------------------
    W, K, L = srgb("#FFFFFF"), srgb(INK), srgb(WHITE)

    def eye(u, v):
        c = np.tile(K, (len(u), 1))
        c[((u - 0.22) / 0.3) ** 2 + ((v - 0.34) / 0.3) ** 2 < 1] = W
        return c

    def eyes(state):
        look = eye_look(state, eye, L, K, line=0.17)
        for s in (1, -1):
            lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
            d = unit(s * 0.74, -0.56, 0.3)
            k.decal(HEAD + d * HR, d, (0.0165, 0.0205), lk, eps=0.0007, rings=20, segs=56)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    tynamo(*args())
