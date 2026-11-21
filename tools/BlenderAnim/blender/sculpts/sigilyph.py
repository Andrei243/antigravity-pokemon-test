# Sigilyph sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sigilyph.py -- <the export's Sigilyph.glb> <out.glb>
# A painted totem of a bird: a round body banded from the top down (a gold collar under the head, a white band, a
# black band, green, a black zigzag band round the middle with a white-ringed blue eye-mark on each side of the front,
# green again, black beneath), two small white spikes low at the sides; a black head on top, a bulb with one large eye
# (a white ring, a light blue iris, a dark pupil) drawn up into a tall black point; at each side a black comb of a hand
# (a spine with three prongs out); two fans of five long gold feathers raised in a V behind it, each banded blue, red
# and blue to a blue point; a gold tail hanging below that parts into four prongs banded the same way.
# (The pictures overrule the brief: no yellow comb of teeth; the gold is a collar; the third eye is on the head, the
# other two are marks on the body; the arms are black combs.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, eye_look, args

BLACK, GREEN, GOLD, WHITE = "#1F1F25", "#4E9E3C", "#F0B238", "#F2F2EE"
FBLUE, RED, EYEBLUE, PUPIL = "#78C0EA", "#D8303E", "#86CDEB", "#14243A"

C = np.array([0.0, 0.0, 0.395])                   # the body's ball, over the body's joint
R = np.array([0.143, 0.136, 0.132])
HB = np.array([0.0, -0.006, 0.588])               # the head's bulb, over the head's joint (0, 0, 0.485)
HBR = np.array([0.056, 0.05, 0.064])
VOXEL = 0.006


def banded(k, name, pts, radii, bands, bone):
    """A long piece in coloured lengths: bands is [(start, end, colour)] in fractions of the run along pts (a polyline),
    each length a loose tube of its own, so the edges between colours stay crisp."""
    pts = np.array(pts, float); radii = np.array(radii, float)
    seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    cum = np.concatenate([[0], np.cumsum(seg)]) / seg.sum()
    def at(t):
        return np.array([np.interp(t, cum, pts[:, i]) for i in range(3)]), float(np.interp(t, cum, radii))
    for j, (a, b, col) in enumerate(bands):
        ts = sorted({a, b} | {float(c) for c in cum if a < c < b})
        P, Rr = zip(*[at(t) for t in ts])
        if len(P) == 2:
            P = [P[0], (P[0] + P[1]) / 2, P[1]]; Rr = [Rr[0], (Rr[0] + Rr[1]) / 2, Rr[1]]
        k.add(f"{name}{j}", tube(P, Rr, seg=14, per=4), col, bone, fuse=False)


TIPBANDS = [(0.0, 0.56, GOLD), (0.55, 0.68, FBLUE), (0.675, 0.8, RED), (0.795, 1.0, FBLUE)]


def sigilyph(src, out):
    k = Kit(src, coat="fur")
    k.add("ball", ell(C, R), BLACK, "body")
    # the head: a black neck into a bulb with the eye, drawn up into a tall point leaning a little back
    k.add("neck", tube([(0, 0.0, 0.5), (0, -0.003, 0.56)], [0.04, 0.042]), BLACK, "head")
    k.add("bulb", ell(HB, HBR), BLACK, "head")
    k.add("point", tube([HB + (0, 0.004, 0.035), (0, 0.004, 0.69), (0, 0.012, 0.755), (0, 0.026, 0.795)],
                        [0.038, 0.025, 0.018, 0.012]), BLACK, "head")

    # the body's bands, from the top down, by height on the ball
    def on_ball(P):
        return inside(np.linalg.norm((P - C) / R, axis=1) - 1.12, 0.01)
    def band(lo, hi):
        def m(P):
            h = (P[:, 2] - C[2]) / R[2]
            return inside(lo - h, 0.03) * inside(h - hi, 0.03) * on_ball(P)
        return m
    k.paint(band(0.72, 2.0), GOLD)
    k.paint(band(0.88, 2.0), BLACK)
    k.paint(band(0.56, 0.72), WHITE)
    k.paint(band(-0.6, 0.36), GREEN)
    def zigzag(P):
        h = (P[:, 2] - C[2]) / R[2]
        az = np.arctan2(P[:, 0], -P[:, 1]) / (2 * math.pi)
        mid = -0.04 + 0.11 * (2 * np.abs(2 * (az * 7 - np.floor(az * 7 + 0.5))) - 1)
        return inside(np.abs(h - mid) - 0.15, 0.03) * on_ball(P)
    k.paint(zigzag, BLACK)

    # the eye-marks on the body, a little to each side of the front in the zigzag band
    CW, CB = srgb(WHITE), srgb(EYEBLUE)
    def mark(u, v):
        c = np.tile(CW, (len(u), 1))
        c[u * u + v * v < 0.6 ** 2] = CB
        return c
    for s in (1, -1):
        d = unit(0.62 * s, -0.78, -0.03)
        k.decal(C + d / np.linalg.norm(d / R), d, (0.03, 0.03), mark, rings=14, segs=48, eps=0.004)

    # two small white spikes low at the sides
    for s, side in ((1, "R"), (-1, "L")):
        d = unit(0.62 * s, -0.3, -0.72)
        base = C + d / np.linalg.norm(d / R) * 0.94
        a, b = np.cross(d, (0, 0, 1.0)), None
        a /= np.linalg.norm(a); b = np.cross(d, a)
        pts = [base + (a * x + b * y) * 0.016 for x, y in ((1, 0), (0, 1), (-1, 0), (0, -1))] + [base + d * 0.055]
        k.add(f"spike{side}", hull(pts), WHITE, "body", fuse=False, surface="shell")

    # the hands: black combs at the sides, a stub out of the ball, an upright spine and three prongs out from it
    for s, side in ((1, "R"), (-1, "L")):
        def box(x0, x1, z0, z1, y=-0.012, t=0.017):
            return hull([(x * s, y + dy, z) for x in (x0, x1) for z in (z0, z1) for dy in (-t, t)])
        k.add(f"stub{side}", box(0.12, 0.185, 0.385, 0.415), BLACK, "body", fuse=False)
        k.add(f"spine{side}", box(0.165, 0.198, 0.33, 0.465), BLACK, "body", fuse=False)
        for i, z in enumerate((0.345, 0.397, 0.449)):
            L = (0.272, 0.262, 0.252)[i]
            k.add(f"prong{side}{i}", hull([(x * s, -0.012 + dy, zz) for x, zz0, zz1 in
                                           ((0.19, z - 0.016, z + 0.016), (L, z - 0.012 + (0.018 if i == 2 else 0),
                                                                            z + 0.012 + (0.022 if i == 2 else 0)))
                                           for zz in (zz0, zz1) for dy in (-0.015, 0.015)]), BLACK, "body",
                  fuse=False)

    # the wings: two fans of five long gold feathers raised in a V, banded blue, red and blue to a blue point
    for s, side in ((1, "R"), (-1, "L")):
        root = np.array((0.055 * s, 0.07, 0.5))
        for i, (ang, L) in enumerate(((71, 0.56), (56, 0.49), (42, 0.42), (28, 0.35), (15, 0.28))):
            a = math.radians(ang)
            d = unit(math.cos(a) * s, 0.3, math.sin(a))
            pts = [root, root + d * L * 0.5 + (0, 0, 0.012), root + d * L]
            banded(k, f"feather{side}{i}", pts, [0.021, 0.019, 0.004], TIPBANDS, f"wing{side}")

    # the tail: a gold stem out of the ball's bottom parting into four prongs that hang down, banded at their ends
    k.add("stem", tube([(0, 0.0, 0.29), (0, 0.0, 0.215)], [0.026, 0.024]), GOLD, "tail", fuse=False)
    for i, x in enumerate((-0.075, -0.026, 0.026, 0.075)):
        pts = [(x * 0.15, 0, 0.222), (x * 0.75, 0, 0.205), (x, 0, 0.17), (x, 0, 0.13), (x, 0, 0.006)]
        banded(k, f"prong{i}", pts, [0.019, 0.018, 0.017, 0.016, 0.004],
               [(0.0, 0.57, GOLD), (0.565, 0.7, FBLUE), (0.695, 0.81, RED), (0.805, 1.0, FBLUE)], "tail")

    k.build(voxel=VOXEL, smooth=4, band=0.006)

    # the head's eye: a white ring, a light blue iris, a dark pupil with a glint; on a black head the shut states
    # draw a light line
    LID, INK = srgb(BLACK), srgb("#D2D2D6")
    CP, CG = srgb(PUPIL), srgb("#FFFFFF")
    def eye(u, v):
        c = np.tile(CW, (len(u), 1))
        r = u * u + v * v
        c[r < 0.66 ** 2] = CB
        c[r < 0.3 ** 2] = CP
        c[((u + 0.12) ** 2 + (v - 0.14) ** 2) < 0.1 ** 2] = CG
        return c
    ED = unit(0, -1, 0.0)
    E = HB + ED / np.linalg.norm(ED / HBR)
    def eyes(state):
        k.decal(E, ED, (0.039, 0.039), eye_look(state, eye, LID, INK, line=0.16), rings=20, segs=56, eps=0.004)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    sigilyph(*args())
