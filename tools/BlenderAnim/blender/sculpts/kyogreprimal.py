# Primal Kyogre sculpted on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/kyogreprimal.py -- <the export's Kyogre-Primal.glb> <out.glb>
#
# Grown from kyogre.py, whose frame is copied here (its functions read its colours as constants and lay the crown's
# ovals and the fins' red pattern in white and red, which the Primal changes). From the references: the same great
# whale, its blue deeper and richer; every red line of Kyogre's turned pale cyan (the lip, the lines on the sides and
# back, the dorsal fin's edge); the white ovals on the crown turned pale yellow, the large one ringed in cyan; the
# jaw a pale blue-white. Its largest change: the pectoral fins grown and flared, narrow at the root and broad as a
# paddle at the end, each with a cyan rim along its sides, a large yellow disc ringed in cyan near its middle, a
# pink-red H by the fingers and four white fingers. The tail's two fins split into long finger-like fronds, three a
# side, splayed in a fan, each with a yellow stripe down it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, ring_band, srgb, inside, unit, eye_look, polygon, args

BLUE, JAW, LINE, INKC = "#2C47B6", "#D9E9F6", "#8FDDF4", "#14161E"
YELLOW, PINK, WHITE = "#F6E08E", "#E2477A", "#EEF2F6"

H, HR = np.array([0.0, -0.27, 0.205]), np.array([0.15, 0.17, 0.135])     # the head's mass


def on_ell(c, r, d):
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / np.array(r)) ** 2).sum())
    return np.array(c, float) + d * t


def seg_dist(P2, a, b):
    a, b = np.array(a, float), np.array(b, float)
    ab = b - a
    t = np.clip(((P2 - a) @ ab) / (ab @ ab), 0, 1)
    return np.linalg.norm(P2 - (a + t[:, None] * ab), axis=1)


def poly_dist(P2, pts):
    return np.min([seg_dist(P2, pts[i], pts[i + 1]) for i in range(len(pts) - 1)], axis=0)


def mouth_z(y):
    return 0.148 + 0.03 * np.clip((y + 0.43) / 0.2, 0, 1) ** 1.3


def body(k):
    k.add("head", ell(H, HR), BLUE, "head")
    k.add("snout", ell((0, -0.355, 0.19), (0.12, 0.1, 0.105)), BLUE, "head")
    k.add("jaw", ell((0, -0.32, 0.145), (0.118, 0.115, 0.06)), JAW, "head")
    V, F = tube([(0, -0.2, 0.215), (0, -0.06, 0.218), (0, 0.08, 0.212), (0, 0.2, 0.2)],
                [0.155, 0.152, 0.13, 0.095], seg=40)
    V = V.copy(); V[:, 0] *= 1.08
    k.add("trunk", (V, F), BLUE, "body")
    k.add("tailroot", tube([(0, 0.16, 0.203), (0, 0.29, 0.192), (0, 0.38, 0.188), (0, 0.43, 0.188)],
                           [0.1, 0.07, 0.045, 0.034]), BLUE, "tail")

    def lip(P):
        d = P[:, 2] - mouth_z(P[:, 1])
        return inside(np.abs(d) - 0.007, 0.002) * (P[:, 1] < -0.2) * (P[:, 2] < 0.2)

    def under(P):
        return inside(P[:, 2] - mouth_z(P[:, 1]), 0.002) * (P[:, 1] < -0.205)

    def over(P):
        return inside(mouth_z(P[:, 1]) - P[:, 2], 0.002) * (P[:, 1] < -0.18) * (P[:, 1] > -0.5)
    k.paint(over, BLUE)
    k.paint(under, JAW)
    k.paint(lip, LINE)

    # The cyan lines: round the gills down to the fin's root, along the back toward the tail, and over the crown
    # from the ring round the large oval back to the dorsal fin
    side_line = [(-0.02, 0.345), (0.03, 0.3), (0.035, 0.24), (0.0, 0.2), (-0.055, 0.175)]
    back_line = [(0.1, 0.33), (0.2, 0.29), (0.3, 0.245), (0.4, 0.22)]
    crown_line = [(-0.27, 0.325), (-0.18, 0.345), (-0.07, 0.355), (-0.02, 0.35)]

    def lines(P):
        P2 = P[:, 1:3]
        m = inside(poly_dist(P2, side_line) - 0.0065, 0.002) * (np.abs(P[:, 0]) > 0.06)
        m = np.maximum(m, inside(poly_dist(P2, back_line) - 0.0065, 0.002) * (np.abs(P[:, 0]) > 0.03))
        m = np.maximum(m, inside(poly_dist(P2, crown_line) - 0.006, 0.002)
                       * inside(np.abs(np.abs(P[:, 0]) - 0.055) - 0.006, 0.002))
        return m
    k.paint(lines, LINE)

    # The crown's ovals: the large one yellow in a cyan ring, a smaller yellow one behind
    for c, n, half, ring in (((0.105, -0.315, 0.29), (0.55, -0.35, 0.75), (0.062, 0.034), True),
                             ((0.12, -0.19, 0.31), (0.5, -0.1, 0.85), (0.036, 0.022), False)):
        cc = np.array(c); nn = unit(*n)
        if ring:
            k.decal(cc, nn, (half[0] + 0.012, half[1] + 0.012), LINE, up=unit(0, -1, 0.35), eps=0.004, rings=12,
                    segs=48, mirror=True)
        k.decal(cc, nn, half, YELLOW, up=unit(0, -1, 0.35), eps=0.0055 if ring else 0.004, rings=12, segs=48,
                mirror=True)
    for j, y in enumerate((0.055, 0.085, 0.115)):
        k.decal((0.16, y, 0.245 - 0.008 * j), unit(1, 0, 0.25), (0.009, 0.03), JAW, eps=0.004, rings=6, segs=40,
                outline=polygon([(-1, -1), (1, -1), (1, 1), (-1, 1)]), mirror=True)


FIN_T = 0.032
# narrow at the root, flared broad as a paddle at the end
FIN = [(0.0, -0.075), (0.12, -0.085), (0.24, -0.13), (0.33, -0.175), (0.375, -0.13), (0.39, 0.0), (0.385, 0.13),
       (0.35, 0.2), (0.25, 0.155), (0.12, 0.1), (0.0, 0.085)]


def fin_frame():
    o = np.array([0.125, -0.06, 0.18])
    ex = unit(0.9, 0.08, -0.42)
    ez = np.array([0.0, 1.0, 0.0])
    ez = ez - ex * (ez @ ex); ez /= np.linalg.norm(ez)
    n = np.cross(ex, ez)
    if n[2] < 0:
        n = -n
    return o, ex, ez, n


def fins(k):
    o, ex, ez, n = fin_frame()
    o2 = o - n * 0.004
    k.add("fin", slab(FIN, o2, ex, ez, FIN_T), BLUE, "fin{s}", fuse=False, mirror=True, surface="scales")
    # four white fingers across the broad end
    for j, b in enumerate((-0.125, -0.042, 0.042, 0.125)):
        a0 = 0.37 if abs(b) < 0.08 else 0.355
        base = o2 + ex * a0 + ez * b
        tip = base + ex * 0.065 + ez * 0.012 * np.sign(b)
        w, t = 0.026, 0.028
        pts = [base + ez * w + n * t / 2, base - ez * w + n * t / 2, base + ez * w - n * t / 2, base - ez * w - n * t / 2,
               tip + ez * w * 0.85 + n * t * 0.4, tip - ez * w * 0.85 + n * t * 0.4, tip + ez * w * 0.85 - n * t * 0.4,
               tip - ez * w * 0.85 - n * t * 0.4]
        k.add(f"finger{j}", hull(pts), WHITE, "fin{s}", fuse=False, mirror=True, surface="shell")
    # the pattern: pieces thicker than the fin, so each shows on both faces
    W, TT = 0.013, FIN_T + 0.008
    rc = np.array([0.19, 0.01])
    disc = [(rc[0] + 0.056 * math.cos(t), rc[1] + 0.056 * math.sin(t)) for t in np.linspace(0, 2 * math.pi, 32,
                                                                                               endpoint=False)]
    k.add("findisc", slab(disc, o2, ex, ez, TT - 0.002), YELLOW, "fin{s}", fuse=False, mirror=True, surface="scales")
    k.add("finring", ring_band(o2 + ex * rc[0] + ez * rc[1], n, 0.064, TT, thick=W), LINE, "fin{s}", fuse=False,
          mirror=True, surface="scales")

    def strip(name, a, b, col, w=W):
        a, b = np.array(a, float), np.array(b, float)
        d = (b - a) / np.linalg.norm(b - a); s = np.array([-d[1], d[0]]) * w / 2
        pts = [a + s, b + s, b - s, a - s]
        k.add(name, slab(pts, o2, ex, ez, TT), col, "fin{s}", fuse=False, mirror=True, surface="scales")
    # the cyan rim inset along both long sides, out to the fingers
    strip("rimA0", (0.02, -0.06), (0.12, -0.07), LINE)
    strip("rimA1", (0.12, -0.07), (0.235, -0.112), LINE)
    strip("rimA2", (0.235, -0.112), (0.32, -0.152), LINE)
    strip("rimB0", (0.02, 0.07), (0.12, 0.085), LINE)
    strip("rimB1", (0.12, 0.085), (0.245, 0.138), LINE)
    strip("rimB2", (0.245, 0.138), (0.33, 0.176), LINE)
    # the pink H by the fingers
    strip("h0", (0.28, -0.036), (0.345, -0.036), PINK, 0.016)
    strip("h1", (0.28, 0.036), (0.345, 0.036), PINK, 0.016)
    strip("h2", (0.3125, -0.036), (0.3125, 0.036), PINK, 0.014)


DORSAL = [(-0.08, -0.03), (0.09, -0.03), (0.08, 0.02), (0.13, 0.125), (0.0, 0.04)]


def dorsal(k):
    o = np.array([0.0, -0.02, 0.345])
    ex, ez = np.array([0.0, 1.0, 0.0]), unit(0.55, 0.0, 0.84)
    k.add("dorsal", slab(DORSAL, o, ex, ez, 0.034), BLUE, "body", fuse=False, surface="scales")
    a0, b = np.array(DORSAL[0]), np.array(DORSAL[3])
    d = (b - a0) / np.linalg.norm(b - a0); s = np.array([d[1], -d[0]]) * 0.013
    k.add("dorsaledge", slab([a0, b, b + s, a0 + s], o, ex, ez, 0.04), LINE, "body", fuse=False, surface="scales")


# a long finger-like frond, tapering to a rounded point
FROND = [(0.0, -0.022), (0.1, -0.026), (0.24, -0.024), (0.33, -0.014), (0.36, 0.0), (0.33, 0.014), (0.24, 0.024),
         (0.1, 0.026), (0.0, 0.022)]
STRIPE = [(0.07, -0.007), (0.28, -0.006), (0.28, 0.006), (0.07, 0.007)]


def fronds(k):
    o = np.array([0.0, 0.425, 0.19])
    # three a side in a fan: (sideways, rise, bank, length)
    for j, (dx, dz, bank, size) in enumerate(((0.22, 0.42, 42, 1.0), (0.42, 0.18, 38, 1.0), (0.62, -0.05, 34, 0.9))):
        for s in (1, -1):
            d = unit(dx * s, 1.0, dz)
            lat = np.cross(d, (0, 0, 1.0)); lat /= np.linalg.norm(lat)
            up = np.cross(lat, d)
            a = math.radians(bank) * s
            ez = lat * math.cos(a) + up * math.sin(a)
            nm = f"{j}{'R' if s > 0 else 'L'}"
            k.add("frond" + nm, slab(np.array(FROND) * size, o + d * 0.01, d, ez, 0.026), BLUE, "tail", fuse=False,
                  surface="scales")
            k.add("stripe" + nm, slab(np.array(STRIPE) * size, o + d * 0.01, d, ez, 0.032), YELLOW, "tail",
                  fuse=False, surface="scales")


CB, CK, CL, CY = srgb(BLUE), srgb(INKC), srgb(LINE), srgb(YELLOW)


def eye_open(u, v):
    r = np.sqrt(u * u + v * v)
    c = np.tile(CL, (len(u), 1))
    c[r < 0.72] = CY
    c[((u + 0.05) / 0.3) ** 2 + (v / 0.34) ** 2 < 1] = CK
    return c


DE = unit(0.66, -0.72, -0.1)


def eyes(k, state):
    look = eye_look(state, eye_open, CB, CK, line=0.2)
    E = on_ell(H, HR, DE)
    k.decal(E, DE, (0.026, 0.024), look, eps=0.004, rings=20, segs=56, mirror=True)


def kyogreprimal(src, out):
    k = Kit(src, coat="scales")
    body(k)
    fins(k)
    dorsal(k)
    fronds(k)
    k.build(voxel=0.0065, smooth=4, band=0.004)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    kyogreprimal(*args())
