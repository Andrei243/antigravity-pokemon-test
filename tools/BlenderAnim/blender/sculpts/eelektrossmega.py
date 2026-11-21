# Eelektross's Mega Evolution sculpted on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/eelektrossmega.py -- <the export's Eelektross-Mega.glb> <out.glb>
#
# From the references (they overrule the game's sculptor, which stands two thin tubes upright at the sides): the eel
# straightened out and lying long, its head forward and a little up, the body running back and down and the tail a
# thin whip curling up at the end with a small dark fin; on each side of it, and as long as most of it, a great white
# tube of light lying level and pointing forward, twice the eel's thickness, its front a flat face in a broad red rim
# (pink inside) with a pale yellow cross on white; from each tube's back pale ghostly streamers flow back and up,
# waving, their tips curled. A small white fin with yellow streaks stands on each tube's top near the front; an orange
# flower of a mark and a row of yellow dots on each tube's outer flank. The eel keeps Eelektross's sucker of a mouth
# (red rim, white fangs) on its head's front, its red eye in yellow, and gains a jagged yellow pattern of lightning
# along the flank behind the eye, a row of yellow dots further back, and a long cream fin streaming back and up from
# the neck. The pictures show no arms: the tubes are on the arm bones and float beside the body.
# Masses, largest first (3D picture, against the eel's body diameter): the tubes (diameter 2.1x, length 4.4x), the
# streamers (back and up past the tail), the eel (length 7x its diameter), the head (1.3x), the neck fin.
# Grown from eelektross.py (by sibling): its colours, the sucker of a mouth with its fangs, the eye.
# The skeleton: body (0, 0, 0.287), head (0, -0.08, 0.447), armL/R (+-0.07, -0.05, 0.347), tail (-0.04, 0.42, 0.247).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, lathe, axes, srgb, inside, unit, eye_look, args, outward, sibling, tri

ex = sibling("eelektross")
ek, dr = ex.ek, ex.dr
NAVY, CREAM, CREAM2, RED, YELLOW, WHITE, INK = ex.NAVY, ex.CREAM, ex.CREAM2, ex.RED, ex.YELLOW, ex.WHITE, ex.INK
GHOST, GHOST2, RIM, PINK, CROSS, FACE, ORANGE = ("#E4F2F1", "#D2E9EA", "#D64E50", "#F29A94", "#F2DE5E", "#F8F6E6",
                                                 "#F0A23A")

# The eel's line, the neck first: forward and up at the head, back and down, then the tail whip curling up
LINE = [(0.0, -0.12, 0.44), (0.0, -0.04, 0.405), (0.0, 0.06, 0.36), (0.0, 0.17, 0.32), (-0.01, 0.28, 0.295),
        (-0.025, 0.38, 0.29), (-0.035, 0.47, 0.31), (-0.04, 0.54, 0.36), (-0.035, 0.575, 0.43), (-0.02, 0.56, 0.49)]
RADS = [0.07, 0.066, 0.062, 0.056, 0.048, 0.04, 0.032, 0.026, 0.022, 0.019]
BONES = ["head", "body", "body", "body", "tail", "tail", "tail", "tail", "tail"]

HC = np.array([0.0, -0.17, 0.455])          # the head
HR = np.array([0.08, 0.1, 0.078])

TX, TZ = 0.2, 0.27                           # the tubes' axes (x, z), front and back along y, radius
TF, TB, TR = -0.21, 0.25, 0.108


def on(c, r, d):
    d = unit(*d)
    return c + d * r, d


def joined(shapes):
    V, F, o = [], [], 0
    for v, f in shapes:
        V.append(v); F += [[i + o for i in q] for q in f]; o += len(v)
    return np.vstack(V), F


def tubes(k):
    """The two great tubes of light: fused white drums (the marks are patches on them), a loose red rim and a pink
    ring inside it on the flat front, a cross on a white face; streamers and a fin on each."""
    L = TB - TF
    for s, side in ((1, "R"), (-1, "L")):
        c = np.array([s * TX, TF, TZ])
        prof = [(0, 0), (TR * 0.97, 0), (TR, 0.012), (TR, L * 0.78), (TR * 0.93, L * 0.9), (TR * 0.7, L * 0.97),
                (0.0, L)]
        k.add("drum" + side, lathe(c, (0, 1, 0), prof, seg=48), GHOST, "arm" + side, surface="skin")
        # the rim: red outside, pink inside, standing a little proud of the face
        k.add("rim" + side, lathe(c, (0, 1, 0), [(TR * 0.8, -0.012), (TR + 0.007, -0.012), (TR + 0.009, 0.0),
                                                  (TR + 0.006, 0.045), (TR * 0.8, 0.045), (TR * 0.8, -0.012)],
                                  seg=56), RIM, "arm" + side, fuse=False, surface="skin")
        k.add("ring" + side, lathe(c, (0, 1, 0), [(TR * 0.62, -0.006), (TR * 0.81, -0.006), (TR * 0.81, 0.02),
                                                   (TR * 0.62, 0.02), (TR * 0.62, -0.006)], seg=56),
              PINK, "arm" + side, fuse=False, surface="skin")
        # the face: white with a pale yellow cross
        F_, X_ = srgb(FACE), srgb(CROSS)

        def face(u, v, F_=F_, X_=X_):
            col = np.tile(F_, (len(u), 1))
            arm = ((np.abs(u) < 0.2) | (np.abs(v) < 0.2)) & (u * u + v * v < 0.88 ** 2)
            col[arm] = X_
            return col
        k.decal(c + (0, 0.001, 0), (0, -1, 0), (TR * 0.64, TR * 0.64), face, eps=0.003, rings=16, segs=64)

        # the flower on the outer flank and a row of yellow dots behind it
        fc, fn = np.array([s * (TX + TR), TF + 0.2, TZ + 0.01]), unit(s, 0, 0.12)
        O, Y = srgb(ORANGE), srgb(YELLOW)

        def flower(u, v, O=O, Y=Y):
            col = np.tile(O, (len(u), 1))
            col[u * u + v * v < 0.45 ** 2] = Y
            return col
        k.decal(fc, fn, (0.034, 0.034), flower, eps=0.003, outline=lambda th: 0.82 + 0.18 * math.cos(5 * th),
                rings=12, segs=60)
        for i in range(3):
            a = math.radians(30 + 14 * i)
            p = np.array([s * (TX + TR * math.cos(a)), TF + 0.27 + 0.045 * i, TZ + TR * math.sin(a)])
            k.decal(p, unit(s * math.cos(a), 0, math.sin(a)), (0.012, 0.012), YELLOW, eps=0.003, rings=8, segs=36)
        # two yellow streaks along the top at the front
        for dx in (-0.03, 0.03):
            p = np.array([s * TX + dx, TF + 0.09, TZ + TR])
            k.decal(p, (0, 0, 1.0), (0.008, 0.05), YELLOW, up=(0, 1.0, 0), eps=0.003, rings=8, segs=40)

        # a small white fin on the top near the front, swept back, yellow streaks on both faces
        fo = np.array([s * (TX + 0.03), TF + 0.13, TZ + TR - 0.01])
        ax, az = unit(s * 0.45, 0.65, 0.6), unit(s * 0.5, -0.6, 0.6)
        fin = [(0.0, -0.02), (0.08, 0.0), (0.12, 0.02), (0.07, 0.03), (0.0, 0.03)]
        k.add("fin" + side, slab(fin, fo, ax, az, 0.02), GHOST, "arm" + side, fuse=False, surface="skin")
        k.add("finstreak" + side, slab([(0.015, 0.0), (0.085, 0.012), (0.015, 0.012)], fo + az * 0.004, ax, az,
                                        0.026), YELLOW, "arm" + side, fuse=False, surface="skin")

        # the streamers: ghostly, waving back and up from the tube's back, curled at their ends (one glowing piece)
        paths = [([(0.0, 0.0, 0.07), (0.02, 0.09, 0.13), (-0.015, 0.18, 0.2), (0.02, 0.26, 0.27), (0.0, 0.3, 0.33),
                   (-0.025, 0.29, 0.36)], [0.05, 0.042, 0.035, 0.028, 0.022, 0.02]),
                 ([(0.05, 0.0, 0.0), (0.09, 0.1, 0.04), (0.065, 0.19, 0.09), (0.1, 0.27, 0.14), (0.095, 0.31, 0.19),
                   (0.07, 0.3, 0.21)], [0.046, 0.04, 0.032, 0.026, 0.021, 0.019]),
                 ([(-0.05, 0.0, -0.02), (-0.055, 0.1, 0.0), (-0.025, 0.18, 0.04), (-0.055, 0.26, 0.08),
                   (-0.045, 0.3, 0.12), (-0.025, 0.29, 0.14)], [0.044, 0.038, 0.03, 0.025, 0.02, 0.019])]
        base = np.array([s * TX, TB - 0.05, TZ])
        # the veil: the tube's back flowing up into the streamers (fused, so it is the tube's own surface)
        k.add("veil" + side, ell(base + np.array([0.0, 0.04, 0.07]), (0.085, 0.09, 0.075), rot=(-30, 0, 0)), GHOST,
              "arm" + side, surface="skin")
        shapes = []
        for pts, rr in paths:
            P = [base + np.array([s * x, y, z]) for x, y, z in pts]
            shapes.append(outward(tube(P, rr, seg=14, per=6)))
        k.add("streamers" + side, joined(shapes), GHOST2, "arm" + side, fuse=False, glow=GHOST2, surface="skin")


def eel(k):
    line = dr.Line(LINE, RADS, rise=(0.13, 0.2))
    line.tubes(k, BONES, NAVY)

    # the head: a big end carried forward, the sucker on its front
    k.add("skull", ell(HC, HR, rot=(-10, 0, 0)), NAVY, "head")
    k.add("neck", tube([LINE[0], HC + (0, 0.04, -0.005)], [0.072, 0.078]), NAVY, "head")

    # the jagged yellow pattern of lightning along the flank behind the eye, and the eye's yellow patch
    def bolt(P):
        j, s_, d = line.place(P)
        T = line.T[j]
        r = P - line.C[j]
        r = r - T * (r * T).sum(1, keepdims=True)
        r /= np.linalg.norm(r, axis=1, keepdims=True) + 1e-9
        side = np.abs(r[:, 0])
        up = r[:, 2]
        arc = line.cum[j]
        reach = 0.12 + 0.28 * np.clip(1 - arc / 0.32, 0, 1)
        jag = 0.2 + 0.14 * tri(arc / 0.05)
        return (inside(np.abs(up - 0.25) - jag * reach / 0.4, 0.002) * (side > 0.4) * (arc < 0.34)
                * (P[:, 1] > -0.12))
    k.paint(bolt, YELLOW, on=["seg"])
    for s in (1, -1):
        c, n = on(HC, HR, (s * 0.7, -0.35, 0.5))
        k.decal(c, n, (0.036, 0.034), YELLOW, eps=0.003, rings=12, segs=48)
        c2, n2 = on(HC + (0, 0.04, 0.0), HR, (s * 0.85, 0.15, 0.45))
        k.decal(c2, n2, (0.03, 0.022), YELLOW, eps=0.003, rings=10, segs=44)

    # the row of yellow dots along the flank further back
    for s in (1, -1):
        for sv in (2.6, 3.0, 3.4, 3.8, 4.2, 4.6):
            p, t, rad = line.at(sv)
            n = unit(s, 0.0, 0.1)
            k.decal(p + n * rad, n, (0.011, 0.011), YELLOW, eps=0.003, rings=8, segs=36)

    # the mouth: the sucker of the base, on the head's front, fangs round its rim
    mc, md = on(HC, HR, (0, -0.85, -0.35))
    ex.sucker(k, "mouth", mc + md * 0.002, md, 0.044, 0.07, 0.014, "head", ex.MAW, 4, 0.028, 0.011,
              roll=math.pi / 4)

    # the long cream fin streaming back and up from the neck, turned to +X so both cameras see its face
    ek.paddle(k, "necfin", (0.0, -0.07, 0.49), (0.1, 0.9, 0.38), (0, -0.4, 1.0), 0.24, 0.09, [CREAM, CREAM2],
              "body", thick=0.018, turn=40, ribs=4)
    # a small dark spiky fin where the body narrows into the whip, and the whip's end fin
    p, t, rad = line.at(5.0)
    spikes = [(0.0, -0.02), (0.05, -0.03), (0.07, -0.05), (0.08, -0.01), (0.11, -0.015), (0.09, 0.02), (0.0, 0.02)]
    k.add("backfin", slab(spikes, p + np.array([0.0, 0.0, rad * 0.8]), unit(0.4, 0.5, 0.75), unit(0.8, -0.3, -0.4),
                          0.02), ex.GREY, "tail", fuse=False, surface="scales")
    p, t, rad = line.at(len(LINE) - 1.0)
    tip = [(0.0, -0.025), (0.06, -0.03), (0.09, -0.005), (0.07, 0.02), (0.0, 0.02)]
    k.add("tailfin", slab(tip, p - t * 0.01, unit(0.3, -0.4, 0.85), unit(0.9, 0.3, 0.1), 0.02), NAVY, "tail",
          fuse=False, surface="scales")
    return line


def eelektross_mega(src, out):
    k = Kit(src, coat="scales")
    eel(k)
    tubes(k)
    k.build(voxel=0.006, smooth=5, band=0.005)

    R, K, W, Y = srgb(ex.IRIS), srgb(INK), srgb("#FFFFFF"), srgb(YELLOW)

    def eye(u, v):
        c = np.tile(R, (len(u), 1))
        r2 = u * u + v * v
        c[r2 > 0.84 ** 2] = K
        c[(u + 0.04) ** 2 + v * v < 0.38 ** 2] = K
        c[(u - 0.3) ** 2 + (v - 0.3) ** 2 < 0.17 ** 2] = W
        return c

    def eyes(state):
        look = eye_look(state, eye, Y, K, line=0.17)
        for s in (1, -1):
            lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
            c, n = on(HC, HR, (s * 0.7, -0.35, 0.5))
            k.decal(c, n, (0.026, 0.026), lk, eps=0.0045, rings=20, segs=56)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    eelektross_mega(*args())
