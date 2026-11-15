# Kyogre sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/kyogre.py -- <the export's Kyogre.glb> <out.glb>
#
# From the references: a great deep-blue whale floating level, a bulbous rounded head with a white lower jaw under a
# red lip line, small yellow eyes ringed in red low on the head's sides, two white ovals on each side of the crown
# (a large one in front, a smaller one behind), a small swept dorsal fin edged in red, three white gill bars on each
# side under it framed by a red line, red lines along the back; two huge flat pectoral fins, rounded rectangles each
# with four white fingers at its outer end and a red pattern on it (a ring with a three-spoked mark in it and lines
# running back to the root); a tapering tail ending in four long trailing fins.
#
# The fins droop outward some 30 degrees so that each of the game's two cameras sees one fin's face; the dorsal fin
# leans to +X; the trailing fins are banked so their faces show from behind.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, ring_band, srgb, inside, smoothstep, unit, eye_look, polygon, args

BLUE, WHITE, RED, INKC = "#2457B4", "#EEF2F6", "#E3263A", "#14161E"
YELLOW = "#F6D02A"

H, HR = np.array([0.0, -0.27, 0.205]), np.array([0.15, 0.17, 0.135])     # the head's mass


def on_ell(c, r, d):
    d = unit(*d)
    t = 1.0 / math.sqrt(((d / np.array(r)) ** 2).sum())
    return np.array(c, float) + d * t


def seg_dist(P2, a, b):
    """Distance of 2D points from the segment a..b."""
    a, b = np.array(a, float), np.array(b, float)
    ab = b - a
    t = np.clip(((P2 - a) @ ab) / (ab @ ab), 0, 1)
    return np.linalg.norm(P2 - (a + t[:, None] * ab), axis=1)


def poly_dist(P2, pts):
    return np.min([seg_dist(P2, pts[i], pts[i + 1]) for i in range(len(pts) - 1)], axis=0)


def mouth_z(y):
    """The mouth's line on the head's side: low at the nose, rising a little toward its corner under the eye."""
    return 0.148 + 0.03 * np.clip((y + 0.43) / 0.2, 0, 1) ** 1.3


def body(k):
    # The head: a broad rounded dome, wider than tall
    k.add("head", ell(H, HR), BLUE, "head")
    k.add("snout", ell((0, -0.355, 0.19), (0.12, 0.1, 0.105)), BLUE, "head")
    # The jaw: white, under the head's front, a little flattened
    k.add("jaw", ell((0, -0.32, 0.145), (0.118, 0.115, 0.06)), WHITE, "head")
    # The trunk: a thick body tapering to the tail
    V, F = tube([(0, -0.2, 0.215), (0, -0.06, 0.218), (0, 0.08, 0.212), (0, 0.2, 0.2)],
                [0.155, 0.152, 0.13, 0.095], seg=40)
    V = V.copy(); V[:, 0] *= 1.08
    k.add("trunk", (V, F), BLUE, "body")
    k.add("tailroot", tube([(0, 0.16, 0.203), (0, 0.29, 0.192), (0, 0.38, 0.188), (0, 0.43, 0.188)],
                           [0.1, 0.07, 0.045, 0.034]), BLUE, "tail")

    # The white jaw's edge is crisp: below the mouth line white, above it blue, with a red lip between
    def lip(P):
        d = P[:, 2] - mouth_z(P[:, 1])
        return inside(np.abs(d) - 0.007, 0.002) * (P[:, 1] < -0.2) * (P[:, 2] < 0.2)

    def under(P):
        return inside(P[:, 2] - mouth_z(P[:, 1]), 0.002) * (P[:, 1] < -0.205)

    def over(P):
        return inside(mouth_z(P[:, 1]) - P[:, 2], 0.002) * (P[:, 1] < -0.18) * (P[:, 1] > -0.5)
    k.paint(over, BLUE)
    k.paint(under, WHITE)
    k.paint(lip, RED)

    # Red lines on the sides: one from the dorsal fin's root down round the gills to the fin's root, one along the
    # back from the dorsal fin toward the tail
    side_line = [(-0.02, 0.345), (0.03, 0.3), (0.035, 0.24), (0.0, 0.2), (-0.055, 0.175)]
    back_line = [(0.1, 0.33), (0.2, 0.29), (0.3, 0.245), (0.4, 0.22)]

    def lines(P):
        P2 = P[:, 1:3]
        m = inside(poly_dist(P2, side_line) - 0.0065, 0.002) * (np.abs(P[:, 0]) > 0.06)
        m = np.maximum(m, inside(poly_dist(P2, back_line) - 0.0065, 0.002) * (np.abs(P[:, 0]) > 0.03))
        return m
    k.paint(lines, RED)

    # The white ovals on the crown's sides, the large one in front and a smaller one behind
    for c, n, half in (((0.105, -0.315, 0.29), (0.55, -0.35, 0.75), (0.062, 0.034)),
                       ((0.12, -0.19, 0.31), (0.5, -0.1, 0.85), (0.036, 0.022))):
        cc = np.array(c); nn = unit(*n)
        k.decal(cc, nn, half, WHITE, up=unit(0, -1, 0.35), eps=0.004, rings=12, segs=48, mirror=True)
    # Three white gill bars on each side, behind the red line
    for j, y in enumerate((0.055, 0.085, 0.115)):
        k.decal((0.16, y, 0.245 - 0.008 * j), unit(1, 0, 0.25), (0.009, 0.03), WHITE, eps=0.004, rings=6, segs=40,
                outline=polygon([(-1, -1), (1, -1), (1, 1), (-1, 1)]), mirror=True)


FIN_T = 0.032
FIN = [(0.0, -0.11), (0.12, -0.14), (0.24, -0.142), (0.31, -0.115), (0.335, -0.04), (0.335, 0.07), (0.315, 0.155),
       (0.25, 0.195), (0.12, 0.2), (0.0, 0.15)]


def fin_frame():
    o = np.array([0.125, -0.06, 0.18])
    ex = unit(0.9, 0.08, -0.42)                # outward, a little back, drooping 25 degrees
    ez = np.array([0.0, 1.0, 0.0])             # back along the body
    ez = ez - ex * (ez @ ex); ez /= np.linalg.norm(ez)
    n = np.cross(ex, ez)
    if n[2] < 0:
        n = -n
    return o, ex, ez, n


def fins(k):
    o, ex, ez, n = fin_frame()
    o2 = o - n * 0.004
    k.add("fin", slab(FIN, o2, ex, ez, FIN_T), BLUE, "fin{s}", fuse=False, mirror=True, surface="scales")
    # The four white fingers at the outer end, pointing outward and a little back
    for j, b in enumerate((-0.095, -0.025, 0.045, 0.115)):
        a0 = 0.315 if abs(b) < 0.08 else 0.3
        base = o2 + ex * a0 + ez * b
        tip = base + ex * 0.06 + ez * 0.012
        w, t = 0.022, 0.026
        pts = [base + ez * w + n * t / 2, base - ez * w + n * t / 2, base + ez * w - n * t / 2, base - ez * w - n * t / 2,
               tip + ez * w * 0.85 + n * t * 0.4, tip - ez * w * 0.85 + n * t * 0.4, tip + ez * w * 0.85 - n * t * 0.4,
               tip - ez * w * 0.85 - n * t * 0.4]
        k.add(f"finger{j}", hull(pts), WHITE, "fin{s}", fuse=False, mirror=True, surface="shell")
    # The red pattern: a ring on the outer half with a three-spoked mark in it, two lines back to the root
    W, TT = 0.012, FIN_T + 0.008
    rc = np.array([0.215, 0.03])
    k.add("finring", ring_band(o2 + ex * rc[0] + ez * rc[1], n, 0.074, TT, thick=W), RED, "fin{s}", fuse=False,
          mirror=True, surface="scales")

    def strip(name, a, b):
        a, b = np.array(a, float), np.array(b, float)
        d = (b - a) / np.linalg.norm(b - a); s = np.array([-d[1], d[0]]) * W / 2
        pts = [a + s, b + s, b - s, a - s]
        k.add(name, slab(pts, o2, ex, ez, TT), RED, "fin{s}", fuse=False, mirror=True, surface="scales")
    strip("spoke0", rc, rc + (0.0, -0.07))
    strip("spoke1", rc, rc + (0.06, 0.036))
    strip("spoke2", rc, rc + (-0.06, 0.036))
    strip("root0", (0.143, 0.0), (0.02, -0.06))
    strip("root1", (0.16, 0.075), (0.02, 0.12))
    strip("edge", (0.289, 0.03), (0.325, 0.03))


DORSAL = [(-0.08, -0.03), (0.09, -0.03), (0.08, 0.02), (0.13, 0.125), (0.0, 0.04)]


def dorsal(k):
    o = np.array([0.0, -0.02, 0.345])
    ex, ez = np.array([0.0, 1.0, 0.0]), unit(0.55, 0.0, 0.84)          # leaning some 33 degrees to +X
    k.add("dorsal", slab(DORSAL, o, ex, ez, 0.034), BLUE, "body", fuse=False, surface="scales")
    # The red edge along its leading edge, a strip a little thicker than the fin
    a, b = np.array(DORSAL[4]), np.array(DORSAL[3])
    a0 = np.array(DORSAL[0])
    pts = [a0 + (0.0, 0.006), b + (0.0, 0.008), b + (-0.01, -0.012), a0 + (0.025, -0.012)]
    # leading edge from the front root to the tip, a band some 0.014 wide
    d = (b - a0) / np.linalg.norm(b - a0); s = np.array([d[1], -d[0]]) * 0.013
    k.add("dorsaledge", slab([a0, b, b + s, a0 + s], o, ex, ez, 0.04), RED, "body", fuse=False, surface="scales")


FROND = [(0.0, -0.03), (0.09, -0.045), (0.2, -0.04), (0.26, -0.02), (0.31, 0.0), (0.23, 0.018), (0.17, 0.04),
         (0.09, 0.035), (0.0, 0.03)]


def fronds(k):
    o = np.array([0.0, 0.425, 0.19])
    for j, (dx, dz, bank, size) in enumerate(((0.32, 0.32, 40, 1.0), (0.55, -0.05, 35, 0.9))):
        for s in (1, -1):
            d = unit(dx * s, 1.0, dz)
            lat = np.cross(d, (0, 0, 1.0)); lat /= np.linalg.norm(lat)
            up = np.cross(lat, d)
            a = math.radians(bank) * s
            ez = lat * math.cos(a) + up * math.sin(a)
            k.add(f"frond{j}{'R' if s > 0 else 'L'}", slab(np.array(FROND) * size, o + d * 0.01, d, ez, 0.026),
                  BLUE, "tail", fuse=False, surface="scales")


CB, CK, CW, CR, CY = srgb(BLUE), srgb(INKC), srgb(WHITE), srgb(RED), srgb(YELLOW)


def eye_open(u, v):
    r = np.sqrt(u * u + v * v)
    c = np.tile(CR, (len(u), 1))
    c[r < 0.72] = CY
    c[((u + 0.05) / 0.3) ** 2 + (v / 0.34) ** 2 < 1] = CK
    return c


DE = unit(0.66, -0.72, -0.1)


def eyes(k, state):
    look = eye_look(state, eye_open, CB, CK, line=0.2)
    E = on_ell(H, HR, DE)
    k.decal(E, DE, (0.026, 0.024), look, eps=0.004, rings=20, segs=56, mirror=True)


def kyogre(src, out):
    k = Kit(src, coat="scales")
    body(k)
    fins(k)
    dorsal(k)
    fronds(k)
    k.build(voxel=0.0065, smooth=4, band=0.004)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    kyogre(*args())
