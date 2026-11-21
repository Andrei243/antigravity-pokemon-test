# Galarian Slowpoke sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py),
# grown from slowpoke.py: its head (pink, the cream bill, the fangs, the nostrils, the round eyes with their tiny
# pupils, the ears with their curls), its claws and its colours.
#   blender --background --factory-startup --python sculpts/slowpokegalar.py -- <the export's Slowpoke-Galar.glb> <out.glb>
#
# Written to be grown from (Slowbro-Galar, Slowking-Galar: sibling("slowpokegalar")). From the references: Slowpoke
# lying long and flat on its belly, chin on the ground; its mouth SHUT under the cream bill, two small white fangs
# hanging from the bill's lower edge; the whole top of its head a bright yellow cap down to the tops of its eyes, with
# a few darker wrinkles over the brow, the ears yellow with their curls; the body a long low loaf; the tail NOT raised
# but lying flat along the ground behind, tapering to a point, its last third yellow.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, args, sibling

sp = sibling("slowpoke")
PINK, CREAM = sp.PINK, sp.CREAM
YELLOW, WRINKLE, CURL_Y = "#F6DE36", "#C8A22A", "#D2AE2A"

HC = np.array([0, -0.165, 0.2])        # the head's middle, a little lower than Slowpoke's: the chin on the ground
HS = 1.25


def cap_edge(y_off):
    """The height over the head's middle where the yellow cap ends, by how far forward (-) or back a point is."""
    return 0.053 - 0.33 * y_off


def yellow_cap(k, HC, s, col=None, wrinkle=None):
    """The yellow cap over the head down to the eyes' tops, the ears with it, and two darker wrinkles over the brow."""
    R = sp.HEAD_R * s
    def mask(P):
        Q = P - HC
        near = inside(np.linalg.norm(Q / (R * np.array([1.35, 1.3, 1.45])), axis=1) - 1, 0.01)
        behind = inside(Q[:, 1] - 0.065 * s / 1.25, 0.004)          # ends just behind the ears, not on the hump
        return near * behind * inside(cap_edge(Q[:, 1] / s * 1.25) * s / 1.25 - Q[:, 2], 0.0025)
    k.paint(mask, col or YELLOW)
    for lift in (0.022, 0.042):
        def line(P, lift=lift):
            Q = P - HC
            d = Q[:, 2] - (cap_edge(Q[:, 1] / s * 1.25) * s / 1.25 + lift * s / 1.25)
            front = inside(Q[:, 1] + 0.07 * s / 1.25, 0.004) * inside(np.abs(Q[:, 0]) - 0.085 * s / 1.25, 0.006)
            return inside(np.abs(d) - 0.0042 * s / 1.25, 0.0015) * front * inside(np.linalg.norm(Q / (R * 1.3), axis=1) - 1, 0.01)
        k.paint(line, wrinkle or WRINKLE)


def ear_curls(k, HC, s, col=None, curl_col=None):
    """The ears' curls again, in the cap's colours (slow_head's own are pink)."""
    for sx in (1, -1):
        ec = HC + sp.EAR_AT * np.array([sx, 1, 1]) * s
        n = unit(0.45 * sx, -1, 0.25)
        def curl(u, v, sx=sx):
            uu = u * sx
            r = np.sqrt(uu * uu + v * v)
            th = (np.arctan2(v, uu) + math.pi) / (2 * math.pi)
            c = np.tile(srgb(col or YELLOW), (len(u), 1))
            line = (np.abs(r - (0.18 + 0.55 * th)) < 0.11) & (r < 0.86)
            c[line] = srgb(curl_col or CURL_Y)
            return c
        k.decal(ec + n * 0.03 * s, n, (0.022 * s, 0.022 * s), curl, eps=0.003, rings=12, segs=40)


def galar_head(k, HC, s, bone="head", col=None, curl_col=None):
    """slow_head with the mouth shut (the carve taken out) and the ears' curls in yellow."""
    spec = sp.slow_head(k, HC, s, bone)
    k.parts = [p for p in k.parts if not (p["name"] == "mouth" and p["carve"])]
    k.decals = k.decals[:-2]                     # slow_head's pink curls
    ear_curls(k, HC, s, col, curl_col)
    return spec


def slowpokegalar(src, out):
    k = Kit(src, coat="scales")
    spec = galar_head(k, HC, HS)
    yellow_cap(k, HC, HS)
    # The body: a long low loaf, highest just behind the head, sloping down to the tail
    k.add("body", ell((0, 0.1, 0.135), (0.175, 0.25, 0.135)), PINK, "body")
    k.add("hump", ell((0, 0.01, 0.17), (0.17, 0.15, 0.15)), PINK, "body")
    k.add("neck", ell((0, -0.07, 0.15), (0.155, 0.12, 0.135)), PINK, "body")
    # Front legs tucked forward under the chin, short and thick
    k.add("leg", tube([(0.11, -0.08, 0.1), (0.13, -0.18, 0.045), (0.135, -0.245, 0.032)], [0.05, 0.046, 0.04]),
          PINK, "leg{s}", mirror=True)
    k.add("paw", ell((0.135, -0.265, 0.03), (0.048, 0.05, 0.03)), PINK, "leg{s}", mirror=True)
    sp.claws(k, "claw", (0.135, -0.31, 0.022), (0, -1, 0), 2, "leg{s}")
    # Hind legs: stubs folded at the rump's sides
    k.add("hind", tube([(0.11, 0.24, 0.1), (0.14, 0.24, 0.05), (0.15, 0.21, 0.03)], [0.055, 0.048, 0.04]),
          PINK, "hind{s}", mirror=True)
    k.add("hpaw", ell((0.15, 0.195, 0.028), (0.044, 0.05, 0.028)), PINK, "hind{s}", mirror=True)
    sp.claws(k, "hclaw", (0.152, 0.148, 0.02), (0, -1, 0), 2, "hind{s}")
    # The tail: flat along the ground behind, out a little to the +X side, tapering to a point
    TAIL = [(0.0, 0.3, 0.11), (0.03, 0.42, 0.07), (0.08, 0.53, 0.05), (0.14, 0.62, 0.045), (0.2, 0.69, 0.05),
            (0.245, 0.735, 0.058)]
    k.add("tail", tube(TAIL, [0.085, 0.066, 0.05, 0.036, 0.024, 0.014]), PINK, "tail")
    # its last third yellow, the line crossing the tail
    a, b = np.array(TAIL[2]), np.array(TAIL[3])
    d = unit(*(b - a))
    k.paint(lambda P: inside(((P - (a + 0.6 * (b - a))) @ d) * -1, 0.004) * (P[:, 1] > 0.4), YELLOW, on=["tail"])
    k.build(voxel=0.006, smooth=3, band=0.006)
    k.eye_states(lambda st: sp.eyes(k, spec, st))
    k.export(out)


if __name__ == "__main__":
    slowpokegalar(*args())
