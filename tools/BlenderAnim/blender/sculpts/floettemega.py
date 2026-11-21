# Mega Floette sculpted on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/floettemega.py -- <the export's Floette-Mega.glb> <out.glb>
#
# Grown from floette.py (copied: its function takes nothing that reaches the parts) and through it from Flabébé's
# script (sibling("flabébé"): its wing, its eye, its face's patches). The Mega is of the Eternal Flower Floette.
# From the pictures: the little sprite hangs under a flower grown to dwarf it, which is its largest mass by far: three
# great dark loops (two long ones out to the sides and a shorter one rising behind), each ending in a dark point and
# rimmed with blue along its outer edge, meeting at a red heart; red flares out from the heart along the loops' inner
# edges, and small blue spikes stand round the heart. A red braided cord runs from the heart down to the sprite's
# hand and hangs on below it. The sprite is Floette's, its teal turned blue (the strands from its crown and the heart
# leaf of its tail) and its yellow puffs red. The game's own sculptor builds two loops in an upright plane; the
# pictures show three, the flower tilted back some 40 degrees from upright, which shows its face to the front camera
# and its back to the camera behind.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, slab, hull, srgb, unit, args, outward, sibling

fb = sibling("flabébé")

WHITE_ = fb.WHITE_
BLUE, BLUE_DARK = "#3E6AD0", "#2A50B0"
PUFF, CORD = "#D8323E", "#D42A36"
LOOP, RIM, HEART_RED = "#2C2C36", "#3C78D8", "#D8283A"
DRESS = "#E6E8EE"
EYE_GREY = srgb("#2E2E3A")

DZ = 0.011                               # the Mega's joints stand 0.011 over Floette's


def Z(x, y, z):
    return np.array([x, y, z + DZ])


HC = Z(0.0, -0.004, 0.264)
HR = np.array([0.064, 0.056, 0.058])
EYE_D = (0.39, -0.9, -0.13)
EYE_HALF = (0.017, 0.023)
HAND = Z(0.05, -0.036, 0.236)

# --- the flower's frame: its middle, its face (tilted back some 41 degrees from upright), and its two axes in plane --
HUB = np.array([0.0, 0.03, 0.5])
N = unit(0, -0.75, 0.66)
A = np.array([1.0, 0, 0])                # across, toward +X
U = np.cross(N, A); U = U / np.linalg.norm(U)   # in the plane, up and back
if U[2] < 0:
    U = -U


def P(s, t, h=0.0):
    """A point of the flower: s across, t up its face, h out of it toward the front."""
    return HUB + A * s + U * t + N * h


def loop_path(length, height, tip_t=0.0, n=28, lean=0.0):
    """A teardrop round from the heart out to a far end and back: (s, t) points, the far end at s = length."""
    pts = []
    for i in range(n + 1):
        ph = math.pi * 2 * i / n                  # 0 at the heart's top edge, pi at the far end
        s = length * 0.5 * (1 - math.cos(ph))
        w = math.sin(ph) * height * (0.55 + 0.45 * (1 - math.cos(ph)) / 2)
        t = w + tip_t * s / length + lean * s
        pts.append((s, t))
    return pts


def add_loop(k, name, path, frame, width, thick, rim_from=0.22, rim_to=0.78):
    """A dark band round a loop of the flower, a blue rim along its outer edge (a thinner band set out from it)."""
    W = [frame(s, t) for s, t in path]
    k.add(name, outward(blade(W, [width] * len(W), N, thick / (2 * width))), LOOP, "stem", fuse=False, surface="leaf")
    m = len(path)
    c = np.mean(np.array(path), axis=0)
    rim = []
    for i, (s, t) in enumerate(path):
        f = i / (m - 1)
        if rim_from <= f <= rim_to:
            d = np.array([s - c[0], t - c[1]]); d = d / (np.linalg.norm(d) + 1e-9)
            rim.append(frame(s + d[0] * width * 0.75, t + d[1] * width * 0.75))
    k.add(name + "rim", outward(blade(rim, [width * 0.72] * len(rim), N, thick * 0.7 / (2 * width * 0.72))), RIM,
          "stem", fuse=False, surface="leaf")


def add_flower(k):
    th = 0.026                                  # the loops' thickness: a fifth of their band's width over 0.03 floor
    side = loop_path(0.33, 0.085, tip_t=-0.02)
    for sx in (1, -1):
        fr = lambda s, t, sx=sx: P(sx * (s + 0.025), t)
        add_loop(k, f"loop{'R' if sx > 0 else 'L'}", side, fr, 0.031, th)
        # the dark point beyond its far end, pointing out and a little down
        e = P(sx * 0.34, -0.02)
        k.add(f"tip{'R' if sx > 0 else 'L'}", hull([e + A * sx * 0.14 + U * -0.03, e + U * 0.03, e - U * 0.03,
                                                    e + N * 0.013, e - N * 0.013, e + A * sx * 0.05 + U * 0.02]),
              LOOP, "stem", fuse=False, surface="leaf")
        # red flaring from the heart along the loop's inner base
        red = [(0.0, 0.045), (0.08, 0.04), (0.2, -0.035), (0.07, -0.05), (0.0, -0.035)]
        k.add(f"flare{'R' if sx > 0 else 'L'}", slab([(sx * a, b) for a, b in red], HUB, A, U, th * 0.75), HEART_RED,
              "stem", fuse=False, surface="leaf")
    # the loop rising behind, shorter, its point at the top
    top = loop_path(0.2, 0.065)
    frt = lambda s, t: P(-t, s + 0.03)
    add_loop(k, "loopT", top, frt, 0.028, th)
    e = P(0.0, 0.235)
    k.add("tipT", hull([e + U * 0.12 - A * 0.015, e + A * 0.028, e - A * 0.028, e + N * 0.012, e - N * 0.012]), LOOP,
          "stem", fuse=False, surface="leaf")
    k.add("flareT", slab([(-0.04, 0.0), (0.04, 0.0), (0.02, 0.1), (0.0, 0.15), (-0.02, 0.1)], HUB, A, U, th * 0.75),
          HEART_RED, "stem", fuse=False, surface="leaf")
    # the red heart, and small blue spikes standing round it
    V, F = ell((0, 0, 0), (0.06, 0.055, 0.03), sub=3)
    k.add("heart", outward((V @ np.stack([A, U, N], axis=1).T + HUB, F)), HEART_RED, "stem", fuse=False, surface="leaf")
    for i, (s, t, ds, dt) in enumerate(((0.045, 0.035, 0.6, 0.8), (-0.045, 0.035, -0.6, 0.8), (0.0, 0.05, 0.0, 1.0))):
        b = P(s, t, 0.01)
        d = A * ds + U * dt + N * 0.5
        d = d / np.linalg.norm(d)
        a1 = np.cross(d, N); a1 = a1 / np.linalg.norm(a1)
        k.add(f"spike{i}", hull([b + d * 0.06, b + a1 * 0.014, b - a1 * 0.014, b + N * 0.012, b - N * 0.01]), BLUE,
              "stem", fuse=False, surface="leaf")


def braid(k, name, pts, r, bead, bone):
    """A red braided cord: a core and beads along it, crossed as a braid's twists."""
    pts = np.array(pts, float)
    k.add(name, outward(tube(pts, [r] * len(pts))), CORD, bone, fuse=False, surface="cloth")
    seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    cum = np.concatenate([[0], np.cumsum(seg)])
    n = int(cum[-1] / (bead * 1.7))
    for i in range(1, n):
        s = i * cum[-1] / n
        p = np.array([np.interp(s, cum, pts[:, j]) for j in range(3)])
        j = min(np.searchsorted(cum, s), len(pts) - 1)
        d = pts[j] - pts[j - 1]; d = d / np.linalg.norm(d)
        V, F = ell((0, 0, 0), (bead, bead * 0.8, bead * 1.15), sub=2)
        a, b = np.cross(d, (0.3, 0.2, 0.9)), None
        a = a / np.linalg.norm(a); b = np.cross(d, a)
        tw = 0.5 if i % 2 else -0.5
        M = np.stack([a * math.cos(tw) + b * math.sin(tw), -a * math.sin(tw) + b * math.cos(tw), d], axis=1)
        k.add(f"{name}{i}", outward((V @ M.T + p, F)), CORD, bone, fuse=False, surface="cloth")


def strands(k, c, r, colour, bone, size=1.0, dirs=((0.2, -0.42, 0.88), (0.36, -0.66, 0.66), (0.5, -0.72, 0.46), (0.6, -0.7, 0.34))):
    """Two strands from the crown curling down over the brows, lying on the head (Floette's, blue)."""
    for sx in (1, -1):
        pts = []
        for d in dirs:
            p, n = fb.on_ell(c, r, (d[0] * sx, d[1], d[2]))
            pts.append(p + n * 0.0035 * size)
        k.add("strand" + ("R" if sx > 0 else "L"),
              outward(blade(pts, [0.0065 * size, 0.009 * size, 0.0095 * size, 0.008 * size], (0, -0.7, 0.7), 0.5)),
              colour, bone, fuse=False, surface="leaf")


def open_mouth(k, c, r, d, half, face):
    p, nrm = fb.on_ell(c, r, d)
    f = srgb(face)
    mr, tg = srgb("#8E2430"), srgb("#F08A9A")
    def look(u, v):
        col = np.tile(f, (len(u), 1))
        inner = (v < 0.45) & (u * u + ((v - 0.45) / 1.35) ** 2 < 0.82)
        col[inner] = mr
        col[inner & (v < -0.35) & (np.abs(u) < 0.55)] = tg
        return col
    k.decal(p, nrm, half, look, eps=0.0015, rings=12, segs=48)


def floettemega(src, out):
    k = Kit(src, coat="leaf")
    # --- the sprite: Floette's, blue and red where it was teal and yellow ---
    k.add("chest", ell(Z(0, -0.002, 0.2), (0.027, 0.023, 0.024)), WHITE_, "body")
    k.add("dress", ell(Z(0, 0.0, 0.17), (0.029, 0.026, 0.032)), DRESS, "body")
    k.add("tail", tube([Z(0, 0.0, 0.152), Z(0.004, -0.006, 0.118), Z(0.01, -0.012, 0.09)], [0.022, 0.014, 0.0095]),
          BLUE, "legR")
    k.add("heartleaf", slab(fb.wing_outline(0.05, 0.024, notch=0.13), Z(0.012, -0.014, 0.036), (0, 0, 1.0),
                            (1.0, 0, 0), 0.012), BLUE_DARK, "legR", fuse=False, surface="leaf")
    # tiny arms: the right one up to the cord, the left forward and down
    k.add("armR", tube([Z(0.022, -0.002, 0.204), Z(0.038, -0.024, 0.214), HAND + (-0.004, 0.004, -0.006)],
                       [0.0095, 0.009, 0.0095]), WHITE_, "armR")
    k.add("handR", ell(HAND + (-0.002, 0.002, -0.002), (0.012, 0.011, 0.012)), WHITE_, "armR")
    k.add("armL", tube([Z(-0.022, -0.002, 0.204), Z(-0.03, -0.026, 0.192), Z(-0.022, -0.04, 0.186)],
                       [0.0095, 0.009, 0.0095]), WHITE_, "armL")
    k.add("handL", ell(Z(-0.02, -0.043, 0.185), (0.0115, 0.011, 0.011)), WHITE_, "armL")
    k.add("neck", ell(Z(0, -0.003, 0.214), (0.022, 0.02, 0.016)), WHITE_, "head")
    k.add("head", ell(HC, HR), WHITE_, "head")
    for sx in (1, -1):
        k.add("puff" + ("R" if sx > 0 else "L"), ell(Z(0.058 * sx, 0.004, 0.272), (0.019, 0.021, 0.022), sub=3), PUFF,
              "head")
    fb.wing(k, "ear", Z(0.054, 0.006, 0.27), (1.0, 0.12, -0.82), 0.12, 0.031, 0.012, WHITE_, "head", notch=0.13)
    strands(k, HC, HR, BLUE, "head")
    open_mouth(k, HC, HR, (0, -1, -0.5), (0.0135, 0.0095), WHITE_)
    # --- the cord from the hand up to the heart, and its loose end hanging beside the sprite ---
    braid(k, "cord", [HAND + (0.0, 0.0, 0.0), HAND + (0.01, 0.01, 0.07), P(0.02, -0.12, -0.02), P(0.0, -0.04, -0.01)],
          0.0062, 0.0095, "stem")
    braid(k, "cordend", [HAND + (0.004, -0.004, -0.004), HAND + (0.03, -0.004, -0.05), HAND + (0.034, 0.0, -0.11),
                         HAND + (0.02, 0.004, -0.16)], 0.006, 0.009, "stem")
    # --- the flower ---
    add_flower(k)
    k.build(voxel=0.0036, smooth=4, band=0.004, wband=0.03)

    def eyes(state):
        fb.lay_eyes(k, HC, HR, EYE_D, EYE_HALF, fb.black_eye(EYE_GREY), WHITE_, state)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    floettemega(*args())
