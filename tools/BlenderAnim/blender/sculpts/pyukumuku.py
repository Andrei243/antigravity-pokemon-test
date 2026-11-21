# Pyukumuku sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pyukumuku.py -- <the export's Pyukumuku.glb> <out.glb>
#
# From the references: a sea cucumber like a long black bean lying on its flat belly, its front end (-Y) blunt and a
# little lower, its back end rounder and higher. Ten rounded pink spikes in pairs along its back and sides: a pair
# leaning forward at the front, a tall pair on the top just behind it, a small pair low on the sides in the middle, a
# tall pair on the top behind the middle and a pair leaning back at the rear. A white puff of fluff out of its back end.
# On the blunt front a white star of a mouth, low; its eyes small pink ovals on the front's corners beside it. (The
# pictures overrule the task's text: the body is a smooth bean, not a spiny one; only the ten pink spikes are spiky.)
#
# Reference ratios (3D picture): length / body height 2.2; the tallest spike above the back / body height 0.45; the
# fluff across / body height 0.4; the mouth across / body height 0.3. Bones: root, body (one body).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, outward, polygon, lumpy

BLACK, PINK, WHITE, FLUFF = "#3A3A3E", "#E8548E", "#F4F4F2", "#E8E8E6"

BODY_C, BODY_R = np.array([0.0, 0.012, 0.0655]), np.array([0.094, 0.165, 0.074])
FRONT_C, FRONT_R = np.array([0.0, -0.088, 0.061]), np.array([0.09, 0.082, 0.069])


def sole(V):
    V = V.copy()
    V[:, 2] = np.maximum(V[:, 2], -0.86)
    return V


def body(k):
    k.add("body", ell(BODY_C, BODY_R, deform=sole), BLACK, "body")
    k.add("front", ell(FRONT_C, FRONT_R, deform=sole), BLACK, "body")


def on_body(y, phi):
    """A point of the body's ellipsoid at length y and angle phi from the top (degrees, toward +X), and its normal."""
    c, r = BODY_C, BODY_R
    q = max(0.05, 1 - ((y - c[1]) / r[1]) ** 2)
    a, b = r[0] * math.sqrt(q), r[2] * math.sqrt(q)
    p = math.radians(phi)
    pt = np.array([a * math.sin(p), y, c[2] + b * math.cos(p)])
    n = (pt - c) / r ** 2
    return pt, n / np.linalg.norm(n)


SPIKES = [  # y, angle from the top, length, root radius, lean along the body (- forward)
    (-0.12, 58, 0.05, 0.018, -0.75),
    (-0.06, 26, 0.074, 0.021, -0.2),
    (-0.005, 74, 0.042, 0.016, 0.0),
    (0.05, 30, 0.07, 0.02, 0.15),
    (0.115, 52, 0.052, 0.018, 0.7),
]


def spikes(k, specs=SPIKES, color=PINK):
    for i, (y, phi, L, r, lean) in enumerate(specs):
        root, n = on_body(y, phi)
        d = unit(*(n + np.array([0, lean, 0.25])))
        pts = [root - d * 0.012, root + d * L * 0.45, root + d * L * 0.85]
        k.add(f"spike{i}", outward(tube(pts, [r * 1.15, r * 0.8, r * 0.34], seg=16, per=6)), color, "body", fuse=False,
              surface="shell", mirror=True)


def fluff(k, at=(0.0, 0.178, 0.07), color=FLUFF):
    at = np.array(at, float)
    for i, (o, r) in enumerate((((0, 0.006, 0.004), 0.027), ((0.02, 0.012, -0.012), 0.02), ((-0.02, 0.012, -0.012), 0.02),
                                ((0.016, 0.014, 0.018), 0.019), ((-0.016, 0.014, 0.018), 0.019),
                                ((0.0, 0.028, 0.0), 0.02))):
        k.add(f"fluff{i}", outward(ell(at + o, (r, r * 0.95, r), deform=lumpy(5, 3, 0.08), sub=3)), color, "body",
              fuse=False, surface="skin")


def asterisk(arms=6, width=0.26):
    pts = []
    for i in range(arms):
        a = 2 * math.pi * i / arms + math.pi / 2
        for da, rr in ((-0.13, 1.0), (0.13, 1.0), (math.pi / arms, width)):
            pts.append((rr * math.cos(a + da), rr * math.sin(a + da)))
    return polygon(pts)


def mouth(k, at=(0.0, -0.17, 0.046), half=(0.022, 0.022), color=WHITE):
    k.decal(at, unit(0, -1, 0.12), half, color, eps=0.0022, outline=asterisk(), rings=8, segs=144)


EYE_C, EYE_N, EYE_HALF = np.array([0.05, -0.15, 0.067]), unit(0.7, -0.72, 0.12), (0.0105, 0.0125)
CB, CPK, CHI = srgb(BLACK), srgb(PINK), srgb("#F7A6C6")


def open_eye(u, v):
    c = np.tile(CPK, (len(u), 1))
    c[((u + 0.25) ** 2 + (v - 0.35) ** 2) < 0.07] = CHI
    return c


def eyes(k):
    def lay(state):
        k.decal(EYE_C, EYE_N, EYE_HALF, eye_look(state, open_eye, CB, CPK, line=0.2), eps=0.0022, rings=20, segs=56,
                mirror=True)
    return lay


def pyukumuku(src, out_path):
    k = Kit(src, coat="scales")
    body(k)
    spikes(k)
    fluff(k)
    mouth(k)
    k.build(voxel=0.0028, smooth=3, band=0.004, wband=0.03)
    k.eye_states(eyes(k))
    k.export(out_path)


if __name__ == "__main__":
    src, out = args()
    pyukumuku(src, out)
