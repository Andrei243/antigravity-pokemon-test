# Victini sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/victini.py -- <the export's Victini.glb> <out.glb>
#
# From the references: a small cream creature some 0.6 tall with the ears, its head as wide again as its pear of a
# body. Two great ears rise from the top of the head in a V, each a long flat cone: the half facing into the V is
# orange, and the two orange halves run down the forehead to meet in a point between the eyes; the outer half is
# cream with a black triangle near its base. Big round blue eyes with a black pupil and a large white glint, a small
# open mouth (dark red, a pink tongue, two little fangs). Short cream arms held out and a little forward, ending in
# orange hands making the V sign; short legs on small orange feet; a thin V mark on the chest; and from the back two
# little cream wings of three feathers each, raised behind the arms.
# The joints: body (0, 0, 0.2), head (0, -0.01, 0.24), earR/L (+-0.03, -0.02, 0.35), armR/L (+-0.05, -0.01, 0.2),
# wingR/L (+-0.03, 0.05, 0.2), legR/L (+-0.04, 0, 0.1).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, slab, outward, srgb, inside, unit, polygon, eye_look, args

CREAM, WING, ORANGE, BLACK = "#F7E9B6", "#F6E2A2", "#EF6A3A", "#1C1414"
BLUE, DEEP, MOUTH, TONGUE, LINE = "#3A9BEA", "#1F5FB8", "#7A2030", "#E46A7C", "#C47A4E"

HEAD_C, HEAD_R = np.array([0.0, -0.022, 0.3]), np.array([0.118, 0.1, 0.098])
APEX = np.array([0.0, 0.298])                 # where the ears' orange halves meet, between the eyes (x, z)
TIP = np.array([0.235, 0.6])                   # the right ear's tip (x, z)
EAR_Y = (0.0, 0.035)                           # the ear's middle at its base and tip (y)
EYE_N = unit(0.43, -0.89, 0.06)


def ear_frame(s):
    """The right (s=1) or left ear's line in the front view: its start, its direction, and the side facing into the V."""
    d = (TIP - APEX) / np.linalg.norm(TIP - APEX)
    d = np.array([d[0] * s, d[1]])
    inner = np.array([-d[1], d[0]]) if s == 1 else np.array([d[1], -d[0]])
    if inner[1] < 0:
        inner = -inner
    return APEX, d, inner


def victini(src, out):
    k = Kit(src, coat="fur")

    # --- the body: a small pear, short legs on orange feet --------------------------------------------------------
    def pear(V):
        V = V.copy()
        s = 1 + 0.14 * np.clip(-V[:, 2], 0, 1) - 0.22 * np.clip(V[:, 2], 0, 1) ** 1.3
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell((0.0, -0.004, 0.14), (0.072, 0.064, 0.082), deform=pear), CREAM, "body")
    k.add("neck", ell((0.0, -0.012, 0.21), (0.045, 0.042, 0.035)), CREAM, "head")
    k.add("thigh", ell((0.044, -0.012, 0.072), (0.04, 0.043, 0.046)), CREAM, "leg{s}", mirror=True)
    k.add("leg", tube([(0.046, -0.014, 0.06), (0.048, -0.018, 0.026)], [0.026, 0.022]), CREAM, "leg{s}", mirror=True)
    k.add("foot", ell((0.05, -0.034, 0.016), (0.024, 0.034, 0.017)), ORANGE, "leg{s}", fuse=False, mirror=True)

    # --- the arms: short, out and a little forward, orange hands in the V sign ----------------------------------
    HAND = np.array([0.108, -0.062, 0.18])
    k.add("arm", tube([(0.045, -0.01, 0.2), (0.078, -0.036, 0.19), HAND - np.array([0.008, -0.006, 0.0])],
                      [0.021, 0.019, 0.017]), CREAM, "arm{s}", mirror=True)
    k.add("hand", ell(HAND, (0.019, 0.017, 0.018)), ORANGE, "arm{s}", fuse=False, mirror=True)
    for i, d in enumerate((unit(0.25, -0.15, 1), unit(0.75, -0.2, 0.8))):
        k.add(f"finger{i}", tube([HAND + d * 0.008, HAND + d * 0.034], [0.0105, 0.009]), ORANGE, "arm{s}", fuse=False,
              mirror=True)
    k.add("thumb", tube([HAND, HAND + unit(-0.3, -0.7, 0.3) * 0.022], [0.009, 0.008]), ORANGE, "arm{s}", fuse=False,
          mirror=True)

    # --- the wings: three little cream feathers each, raised and spread behind the arms --------------------------
    for i, (d, L) in enumerate(((unit(0.95, 0.45, 0.42), 0.1), (unit(1.0, 0.5, 0.08), 0.092),
                                (unit(0.9, 0.55, -0.25), 0.078))):
        w = 0.021
        outl = [(0.0, -w), (0.55 * L, -w), (0.9 * L, -0.5 * w), (L, 0.0), (0.9 * L, 0.5 * w), (0.55 * L, w), (0.0, w)]
        k.add(f"wing{i}", slab(outl, (0.036, 0.052, 0.19), d, unit(0, 0.25, 1), 0.016), WING, "wing{s}", fuse=False,
              mirror=True)

    # --- the head: big and round, the ears rising in a V -------------------------------------------------------
    k.add("head", ell(HEAD_C, HEAD_R), CREAM, "head")
    k.add("cheek", ell((0.07, -0.05, 0.262), (0.05, 0.05, 0.045)), CREAM, "head", mirror=True)
    for s, side in ((1, "R"), (-1, "L")):
        A, d, _ = ear_frame(s)
        B = A + d * 0.075
        T = A + d * np.linalg.norm(TIP - APEX)
        P = lambda p, y: np.array([p[0], y, p[1]])
        pts = [P(B, EAR_Y[0]), P(B + (T - B) * 0.33, 0.008), P(B + (T - B) * 0.7, 0.022), P(T, EAR_Y[1])]
        d3 = pts[-1] - pts[0]; d3 /= np.linalg.norm(d3)
        flat = np.array([0, 1.0, 0]) - d3 * d3[1]
        k.add(f"ear{side}", blade(pts, [0.06, 0.05, 0.028, 0.006], flat, 0.4), CREAM, f"ear{side}")

    def orange(P):
        m = np.zeros(len(P))
        for s in (1, -1):
            A, d, inner = ear_frame(s)
            q = np.stack([P[:, 0], P[:, 2]], 1) - A
            t, din = q @ d, q @ inner
            w = 0.08 * np.clip(t / 0.075, 0, 1)
            lo = -0.012 * np.clip((t - 0.08) / 0.05, 0, 1)
            band = inside(-t, 0.002) * inside(lo - din, 0.0025) * inside(din - w, 0.0025)
            where = np.maximum(inside(P[:, 1] + 0.045, 0.004), inside(0.375 - P[:, 2], 0.004))
            m = np.maximum(m, band * where)
        return m
    k.paint(orange, ORANGE, on=["ear", "head"])

    # The black triangle on each ear's outer half, near its base
    A, d, inner = ear_frame(1)
    c2 = A + d * 0.2 - inner * 0.03
    d3 = unit(d[0], 0.06, d[1])
    TRI = polygon([(0.0, 1.0), (0.85, -0.9), (-0.85, -0.9)])
    k.decal((c2[0], -0.018, c2[1]), unit(0, -1, 0.12), (0.015, 0.05), BLACK, up=d3, outline=TRI, eps=0.003, rings=10,
            segs=60, mirror=True)

    # The chest's thin V
    CC, CL = srgb(CREAM), srgb(LINE)

    def chest(u, v):
        c = np.tile(CC, (len(u), 1))
        c[(np.abs(v - (-0.8 + 1.9 * np.abs(u))) < 0.24) & (v < 0.85)] = CL
        return c
    k.decal((0.0, -0.066, 0.17), unit(0, -1, 0.15), (0.024, 0.028), chest, eps=0.003, rings=12, segs=60)

    # The mouth: small and open, two fangs at the top, a pink tongue
    CM, CT, CW = srgb(MOUTH), srgb(TONGUE), np.array([1.0, 1.0, 1.0])
    MOUTH_O = polygon([(-1.0, 0.5), (1.0, 0.5)] + [(math.cos(a), 0.5 + 1.4 * math.sin(a))
                                                   for a in np.linspace(-0.15, -math.pi + 0.15, 11)])

    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[((u / 0.6) ** 2 + ((v + 0.75) / 0.4) ** 2) < 1] = CT
        for x in (-0.48, 0.48):
            c[(v > 0.05) & (np.abs(u - x) < 0.24 * (v - 0.05) / 0.45)] = CW
        return c
    k.decal((0.0, -0.118, 0.252), unit(0, -1, -0.12), (0.025, 0.016), mouth, outline=MOUTH_O, eps=0.003, rings=12,
            segs=60)

    # --- the eyes: big, round and blue, a black pupil and a large white glint ---------------------------------
    CB, CD, CK = srgb(BLUE), srgb(DEEP), srgb(BLACK)

    def eye(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(CB, (len(u), 1))
        c[r > 0.62] = CD
        c[r > 0.86] = CK
        c[np.sqrt((u + 0.06) ** 2 + (v - 0.02) ** 2) < 0.36] = CK
        c[((u + 0.24) / 0.3) ** 2 + ((v - 0.3) / 0.3) ** 2 < 1] = CW
        c[((u - 0.32) / 0.12) ** 2 + ((v + 0.38) / 0.12) ** 2 < 1] = CW
        return c

    def eyes(state):
        look = eye_look(state, eye, srgb(CREAM), CK, line=0.16)
        k.decal(HEAD_C + np.array([0.0, 0.0, 0.002]) + EYE_N * HEAD_R * 0.99, EYE_N, (0.032, 0.034), look, eps=0.003,
                rings=20, segs=56, mirror=True)

    k.build(voxel=0.005, smooth=4, band=0.004)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    victini(*args())
