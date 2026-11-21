# Throh sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/throh.py -- <the export's Throh.glb> <out.glb>
# From the references: a squat block of a judoka in a white gi, its red face one tall egg set into the gi's top front
# with no neck, the gi's body as high behind it; a dark grey collar wraps the face like a hood and runs down in a V to
# a black belt knotted in front, two ends hanging; on the face a dark T, a brow bar sloping to the middle and a bar
# down the nose, small white eyes under the bar; thick white sleeves, great red hands with four fat fingers and a
# thumb, clawed; a short flared jacket hem, short thick trouser legs with dark grey knee patches, round red feet.
# Its helpers (frame_ell, hand, collar, belt) take everything as arguments: sawk.py builds with them.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, sheet, slab, outward, polygon, srgb, inside, smoothstep, unit, boxy,
                       eye_look, args)

GI, COLLAR, BELT, RED, PATCH = "#EEEDE6", "#55555C", "#3A3A40", "#D63A40", "#5A5A60"
INK, IRIS, WHITE = "#16161C", "#4A4A52", "#F6F5F0"


def frame_ell(c, r, ex, ey, ez, deform=None, sub=4):
    """An ellipsoid of radii r along the axes ex, ey, ez (right-handed is not needed: ell is closed)."""
    V, F = ell((0, 0, 0), r, deform=deform, sub=sub)
    M = np.stack([unit(*ex), unit(*ey), unit(*ez)], axis=1)
    return outward((V @ M.T + np.array(c, float), F))


def hand(k, name, bone, wrist, fdir, ndir, colour, size=1.0, fist=False, surface="skin"):
    """
    A great hand at a wrist: a palm along fdir (toward the fingers) with its inner face to ndir, four fat fingers and a
    thumb on the side toward the body's middle. Open, the fingers spread and curl a little toward ndir (a claw); a
    fist rolls them under. Every piece loose, so the red keeps its edges against the sleeve.
    """
    f, n = unit(*fdir), unit(*ndir)
    n = unit(*(n - f * (n @ f)))
    w = np.cross(f, n)
    s = size
    wrist = np.array(wrist, float)
    inner = -w if w[0] * np.sign(wrist[0]) > 0 else w            # toward the body's middle
    c = wrist + f * 0.042 * s
    k.add(f"{name}wrist", outward(tube([wrist - f * 0.03 * s, c], [0.03 * s, 0.034 * s])), colour, bone, fuse=False,
          surface=surface)
    k.add(f"{name}palm", frame_ell(c, (0.058 * s, 0.05 * s, 0.034 * s), w, f, n), colour, bone, fuse=False,
          surface=surface)
    for i, o in enumerate((-1.5, -0.5, 0.5, 1.5)):
        b = c + f * 0.038 * s + w * o * 0.026 * s
        if fist:
            pts = [b, b + f * 0.03 * s + n * 0.012 * s, b + f * 0.022 * s + n * 0.042 * s]
            r = [0.022 * s, 0.022 * s, 0.02 * s]
        else:
            spread = w * o * 0.012 * s
            pts = [b, b + f * 0.032 * s + n * 0.006 * s + spread, b + f * 0.054 * s + n * 0.026 * s + spread * 1.5]
            r = [0.022 * s, 0.021 * s, 0.019 * s]
        k.add(f"{name}finger{i}", outward(tube(pts, r)), colour, bone, fuse=False, surface=surface)
    tb = c + inner * 0.042 * s - f * 0.005 * s
    td = unit(*(0.55 * f + 0.8 * inner + 0.35 * n))
    tp = [tb, tb + td * 0.035 * s, tb + td * 0.055 * s + n * 0.018 * s]
    k.add(f"{name}thumb", outward(tube(tp, [0.023 * s, 0.022 * s, 0.019 * s])), colour, bone, fuse=False,
          surface=surface)


def collar(k, outline, front_y, bone, colour, r=0.024, n=60):
    """
    The dark collar: one loose roll from the belt up round the face like a hood and down again to the belt, its path
    the face's outline (outline(a) gives x, z for an angle a from the top, nought, round to either side) lying at the
    depth front_y(x, z) gives, its two ends meeting in a V under the face.
    """
    pts = []
    for t in np.linspace(-1, 1, n):
        x, z = outline(t * math.pi)
        pts.append((x, front_y(x, z), z))
    return pts


def belt(k, z, rx, ry, yc, colour, height=0.036, thick=0.012, bone="body"):
    """A loose band round the waist, its seam behind."""
    def at(u, v):
        a = 2 * math.pi * u
        return np.stack([np.sin(a) * rx, -np.cos(a) * ry + yc, z + (v - 0.5) * height + 0 * u], -1)
    k.add("belt", outward(sheet(at, 97, 3, thick)), colour, bone, fuse=False, surface="cloth")


def knot(k, at, colour, size=1.0, tails=(0.09, 0.1), bone="body"):
    """The belt's knot in front and its two ends hanging, splayed a little: solid plates, not thin ones."""
    at = np.array(at, float)
    s = size
    k.add("knot", frame_ell(at, (0.03 * s, 0.022 * s, 0.024 * s), (1, 0, 0), (0, 1, 0), (0, 0, 1)), colour, bone,
          fuse=False, surface="cloth")
    for side, ln in ((1, tails[0]), (-1, tails[1])):
        top = at + np.array([0.008 * side * s, 0.004 * s, -0.01 * s])
        down = unit(0.22 * side, -0.22, -1)
        ex = unit(*np.cross(down, (0, 1, 0)))
        outline = [(-0.016 * s, 0), (0.016 * s, 0), (0.019 * s, -ln * s), (-0.014 * s, -ln * s * 0.94)]
        k.add(f"tail{'R' if side > 0 else 'L'}", slab(outline, top, ex, -down, 0.016 * s), colour, bone, fuse=False,
              surface="cloth")


def throh(src, out):
    k = Kit(src, coat="cloth")

    # The gi's body: a broad rounded block, its top a little lower than the hood, a short flared hem under the belt
    k.add("trunk", ell((0, 0.02, 0.385), (0.192, 0.148, 0.19), deform=boxy(2.6)), GI, "body", surface="cloth")
    k.add("hem", ell((0, 0.02, 0.225), (0.208, 0.162, 0.07)), GI, "body", surface="cloth")
    # the hood behind and over the face's top
    k.add("hood", ell((0, -0.045, 0.5), (0.13, 0.105, 0.1)), GI, "head", surface="cloth")

    # The face: one tall red egg set into the gi's front, narrowing to a point at the bottom
    FC, FR = np.array([0.0, -0.105, 0.452]), (0.094, 0.076, 0.13)
    def narrow(z):                          # the face's width at a height of the unit sphere
        return 0.58 + 0.42 * smoothstep(-1.0, 0.25, z)
    def egg(V):
        V = V.copy(); V[:, 0] *= narrow(V[:, 2]); return V
    k.add("face", ell(FC, FR, deform=egg), RED, "head", surface="skin")

    # The collar: round the face's outline where it leaves the gi, then down in a V to the belt
    def outline(a):
        if abs(a) <= math.pi * 0.8:
            zz = math.cos(a) * 0.985
            return math.sin(a) * FR[0] * 1.0 * narrow(zz), FC[2] + FR[2] * zz
        t = (abs(a) - math.pi * 0.8) / (math.pi * 0.2)          # the last stretch to the belt, crossing under the tip
        zz = math.cos(math.pi * 0.8) * 0.985
        x0, z0 = math.sin(math.pi * 0.8) * FR[0] * narrow(zz), FC[2] + FR[2] * zz
        sgn = 1 if a > 0 else -1
        return sgn * (x0 + (0.016 - x0) * t), z0 + (0.27 - z0) * t
    def front_y(x, z):
        return -0.126 if z < 0.55 else -0.126 + 0.5 * (z - 0.55)
    path = collar(k, outline, front_y, "body", COLLAR)
    k.add("collar", outward(tube(path, [0.024] * len(path), seg=14, per=3)), COLLAR, "body", fuse=False,
          surface="cloth")

    # The belt and its knot, the ends hanging over the hem
    belt(k, 0.27, 0.198, 0.158, 0.02, BELT)
    knot(k, (0.0, -0.148, 0.268), BELT)

    # Short thick trouser legs, fused into the hem, knees bent a little; round red feet
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", tube(np.array([(0.085, 0.02, 0.22), (0.112, -0.005, 0.13), (0.118, 0.0, 0.065)]) * m,
                                   [0.074, 0.068, 0.06]), GI, f"leg{side}", surface="cloth")
        k.add(f"foot{side}", frame_ell((0.122 * s, -0.035, 0.036), (0.062, 0.084, 0.038), (1, 0, 0), (0, 1, 0),
                                       (0, 0, 1)), RED, f"leg{side}", fuse=False, surface="skin")
    # the knee patches: dark rounded squares on the front of each shin
    KNEE = polygon([(-1, -0.8), (-0.8, -1), (0.8, -1), (1, -0.8), (1, 0.8), (0.8, 1), (-0.8, 1), (-1, 0.8)])
    k.decal((0.116, -0.064, 0.112), unit(0.25, -1, 0.05), (0.038, 0.034), PATCH, eps=0.003, outline=KNEE, rings=10,
            segs=40, mirror=True)

    # The sleeves: thick white tubes from the shoulders, forward and a little down; the model's left hand higher
    ARMS = (("R", 1, (0.15, 0.0, 0.44), (0.232, -0.06, 0.37), (0.235, -0.15, 0.33)),
            ("L", -1, (0.15, 0.0, 0.44), (0.235, -0.07, 0.4), (0.24, -0.175, 0.41)))
    for side, s, sh, el, wr in ARMS:
        m = np.array([s, 1, 1.0])
        SH, EL, WR = np.array(sh) * m, np.array(el) * m, np.array(wr) * m
        k.add(f"sleeve{side}", tube([SH, EL, WR], [0.074, 0.068, 0.064]), GI, f"arm{side}", surface="cloth")
    # the hands: the right low, reaching forward, palm down; the left at the chest, palm forward, fingers up
    hand(k, "handR", "armR", (0.235, -0.195, 0.33), (0.0, -1, -0.12), (0, -0.15, -1), RED, size=1.1)
    hand(k, "handL", "armL", (-0.23, -0.215, 0.415), (0.05, -0.6, 1), (0, -1, -0.3), RED, size=1.1)

    # The T on the face: a brow bar sloping to the middle, a bar down the nose (in every state: before the build)
    T = polygon([(-0.22, -0.92), (0.22, -0.92), (0.18, 0.0), (1.0, 0.22), (1.0, 0.52), (0.0, 0.32), (-1.0, 0.52),
                 (-1.0, 0.22), (-0.18, 0.0)])
    TC = np.array([0.0, -0.177, 0.482])
    k.decal(TC, unit(0, -1, 0.22), (0.074, 0.074), INK, eps=0.007, outline=T, rings=16, segs=96)

    # The eyes: small and white with a dark pupil, set close under the bar, their tops cut by its slope
    CR, CW, CI, CK = srgb(RED), srgb(WHITE), srgb(IRIS), srgb(INK)
    def open_eye(u, v):
        c = np.tile(CW, (len(u), 1))
        c[((u + 0.12) / 0.5) ** 2 + ((v + 0.12) / 0.6) ** 2 < 1] = CK
        c[((u - 0.06) / 0.18) ** 2 + ((v - 0.12) / 0.18) ** 2 < 1] = (1, 1, 1)
        return c
    # the outline: an ellipse whose top is cut by the bar's slope, lower toward the nose
    EYE_OUT = [(math.cos(a), min(math.sin(a), 0.5 + 0.3 * math.cos(a))) for a in np.linspace(0, 2 * math.pi, 40, endpoint=False)]
    E = np.array([0.045, -0.172, 0.464])
    def lay(state):
        look = eye_look(state, open_eye, CR, CK, line=0.18)
        for s in (1, -1):
            shape = polygon([(u * s, v) for u, v in EYE_OUT])
            col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
            k.decal(E * np.array([s, 1, 1]), unit(0.42 * s, -0.9, 0.12), (0.025, 0.022), col, up=(0, 0.1, 1),
                    eps=0.004, outline=shape, rings=20, segs=56)

    k.build(voxel=0.0052, smooth=4, band=0.006)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    throh(*args())
