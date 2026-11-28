# Nameton (Carpathia No. 10104, the great snowdrift) sculpted from nothing on the skeleton and clips of the game's own
# export of it (sculptkit.py), grown from Nametel (sibling("nametel")): its snow and shade, its clumps of packed snow,
# its pink blush, its twig brown gone dark in the claws.
#   blender --background --factory-startup --python sculpts/nameton.py -- <the export's Nameton.glb> <out.glb>
#
# From its picture (docs/art/reference/gemini/carpathia/10104-Nameton.jpg) and its brief (CP-dex-2b-mountains-more.md,
# 104): a hulking golem, a great heap of packed snow piled over its head, shoulders and back (the largest mass, wider
# than the body), with grey rock spurs breaking through it, two large on +X and one on -X; under the snow a grey rocky
# body of plates banded with brown earth; heavy arms of rock ending in great drifts of snow as fists, four long dark
# claws like old wood under each; short thick rocky legs set apart, snow on the knees, dark toes. The face is the snow
# under a jutting brow of snow: small pale icy eyes glowering in shadowed sockets, a pink blush, a wide open dark mouth,
# icicles hanging from the brow and a row of longer ones from the chin. The frost breath, the falling stones and the
# snow crumbs are left out.
# Bones: root (0, 0, 0), body (0, 0, 0.505), armR/L (+-0.27, 0, 0.625), head (0, -0.12, 0.665), legR/L (+-0.13, 0, 0.305).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, outward, srgb, inside, smoothstep, unit, eye_look, polygon, facets, \
    tri, args, sibling

nt = sibling("nametel")

SNOW, SHADE, DEEPSHADE, CHEEK = nt.SNOW, nt.SHADE, nt.DEEPSHADE, nt.CHEEK
FACE = "#E2EBF6"
ROCK, ROCKDARK, PLATE, EARTH = "#80868D", "#646A72", "#8F959C", "#6B5A4A"
CLAW, CLAWPALE = "#4A3A2E", "#6A5646"
ICE, SOCKET, EYEICE, PUPIL = "#B6DDF1", "#5E6E86", "#C8ECFA", "#1E2E4C"
MOUTH, FROST = "#2C3A54", "#7E98B8"

FC, FR = np.array([0.0, -0.2, 0.56]), np.array([0.15, 0.12, 0.12])       # the snow face
CAP_C, CAP_R = np.array([0.0, 0.0, 0.77]), np.array([0.36, 0.28, 0.19])  # the drift over head and shoulders
VOXEL = 0.0092


def body(k):
    k.add("torso", ell((0, 0.03, 0.45), (0.27, 0.22, 0.24), deform=facets(16, 0.45, 10104, depth=(0.88, 1.0))),
          ROCK, "body", surface="shell")
    k.add("belly", ell((0, -0.03, 0.3), (0.24, 0.19, 0.12), deform=facets(10, 0.4, 3, depth=(0.9, 1.0))), ROCK,
          "body", surface="shell")
    # the brown earth banding the rock, ragged, across the belly and round the back
    def earth(P):
        th = np.arctan2(P[:, 0], -P[:, 1])
        top = 0.34 + 0.018 * tri(th * 4 / math.pi)
        bot = 0.255 + 0.015 * tri(th * 3 / math.pi + 0.3)
        return inside(P[:, 2] - top, 0.004) * inside(bot - P[:, 2], 0.004)
    k.paint(earth, EARTH, on=["torso", "belly"])
    # the rock darker toward its foot
    k.paint(lambda P: smoothstep(0.26, 0.18, P[:, 2]), ROCKDARK, on=["belly", "torso"])
    # plates of rock on the chest and belly, loose and flat, each tilted to its own face
    for j, (c, w, h, az) in enumerate((((0.07, -0.235, 0.43), 0.09, 0.075, 12), ((-0.07, -0.235, 0.42), 0.08, 0.07, -14),
                                       ((0.0, -0.225, 0.3), 0.1, 0.05, 0), ((0.17, -0.17, 0.39), 0.07, 0.07, 40),
                                       ((-0.17, -0.17, 0.38), 0.065, 0.065, -40), ((0.12, 0.2, 0.42), 0.09, 0.08, 150),
                                       ((-0.12, 0.2, 0.4), 0.08, 0.08, -150), ((0.0, 0.24, 0.3), 0.09, 0.05, 180))):
        a = math.radians(az)
        n = np.array([math.sin(a), -math.cos(a), 0.05])
        ex = np.array([math.cos(a), math.sin(a), 0.0])
        c = np.array(c)
        pts = []
        for (u, v, d) in ((-1, -1, 0), (1, -1, 0), (1.1, 0.9, 0), (-0.9, 1, 0), (0.1, 0.1, 0.014), (-0.2, -0.3, 0.01)):
            pts.append(c + ex * u * w + np.array([0, 0, 1.0]) * v * h + n * (d - 0.012))
        pts += [p - n * 0.03 for p in pts[:4]]
        k.add(f"plate{j}", hull(pts), PLATE if j % 2 == 0 else ROCK, "body", fuse=False, surface="shell")


def legs(k):
    for s, nm in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add("thigh" + nm, ell(np.array((0.16, 0.0, 0.27)) * m, (0.13, 0.13, 0.13),
                                deform=facets(9, 0.35, 20 + s, depth=(0.9, 1.0))), ROCK, "leg" + nm, surface="shell")
        k.add("shin" + nm, tube([np.array(p) * m for p in [(0.17, -0.01, 0.22), (0.18, -0.03, 0.09)]], [0.105, 0.1]),
              ROCK, "leg" + nm, surface="shell")
        k.add("foot" + nm, ell(np.array((0.18, -0.06, 0.05)) * m, (0.115, 0.135, 0.05)), ROCKDARK, "leg" + nm,
              surface="shell")
        # packed snow on the knee
        k.add("knee" + nm, ell(np.array((0.175, -0.095, 0.185)) * m, (0.085, 0.055, 0.05)), SNOW, "leg" + nm,
              surface="fur")
        k.add("ankle" + nm, ell(np.array((0.18, -0.07, 0.085)) * m, (0.1, 0.06, 0.035)), SNOW, "leg" + nm,
              surface="fur")
        # three dark toes at the front of the foot
        for t in (-1, 0, 1):
            b0 = np.array((0.18 + 0.055 * t * s, -0.165, 0.035)) * np.array([s if False else 1, 1, 1])
            b0 = np.array(((0.18 + 0.055 * t) * s, -0.165, 0.035))
            k.add(f"toe{nm}{t + 1}", outward(ell(b0 + (0, -0.02, 0), (0.03, 0.042, 0.03), sub=3)), CLAW, "leg" + nm,
                  fuse=False, surface="shell")


def drift(k):
    """The great drift: one mass over head, shoulders and back, clumped all over its top and back, the brow of snow
    jutting over the face, rock spurs breaking through."""
    k.add("cap", ell(CAP_C, CAP_R), SNOW, "body", surface="fur")
    k.add("capback", ell((0, 0.12, 0.7), (0.3, 0.17, 0.17)), SNOW, "body", surface="fur")
    for s, nm in ((1, "R"), (-1, "L")):
        k.add("shouldersnow" + nm, ell(np.array((0.27, -0.02, 0.66)) * np.array([s, 1, 1]), (0.14, 0.17, 0.12)), SNOW,
              "body", surface="fur")
    nt.clumps(k, "clump", CAP_C + (0, 0.02, 0.0), CAP_R * np.array([1.0, 0.95, 1.0]), 52, 0.075, "body",
              keep_clear=lambda az, el: abs(az) < 55 and el < 30, el_range=(-25, 80), seed=10104, sink=0.6)
    # the shade in the drift's underside
    k.paint(lambda P: smoothstep(0.66, 0.58, P[:, 2]) * (P[:, 1] > -0.12), SHADE, on=["cap", "capback", "shoulder",
                                                                                      "clump"])
    # the jutting brow over the face, clumped
    k.add("brow", ell((0, -0.225, 0.685), (0.17, 0.11, 0.06)), SNOW, "head", surface="fur")
    for s in (1, -1):
        k.add("browlump" + ("R" if s > 0 else "L"), ell((0.1 * s, -0.25, 0.7), (0.075, 0.07, 0.05)), SNOW, "head",
              surface="fur")
    # rock spurs breaking through: two large on +X, one on -X, a small one on the back
    for j, (base, d, size, hgt) in enumerate((((0.2, 0.08, 0.84), (0.65, 0.3, 0.7), 0.095, 0.24),
                                              ((0.31, -0.02, 0.74), (0.9, 0.0, 0.45), 0.075, 0.2),
                                              ((-0.24, 0.06, 0.82), (-0.65, 0.25, 0.7), 0.075, 0.2),
                                              ((0.04, 0.22, 0.74), (0.15, 0.85, 0.5), 0.07, 0.19))):
        base, d = np.array(base), unit(*d)
        a = unit(*np.cross(d, (0, 1.0, 0.2)))
        b = unit(*np.cross(d, a))
        pts = [base - d * 0.04 + (a * math.cos(t) + b * math.sin(t)) * size * (1.0 + 0.25 * math.sin(3 * t + j))
               for t in np.linspace(0, 2 * math.pi, 6, endpoint=False)]
        # a broken, blocky top: a slanted face of three corners, not a point
        pts += [base + d * hgt * 0.75 + a * size * 0.55, base + d * hgt + b * size * 0.35,
                base + d * hgt * 0.85 - a * size * 0.4 - b * size * 0.2]
        k.add(f"spur{j}", hull(pts), ROCK if j != 1 else ROCKDARK, "body", fuse=False, surface="shell")


def arms(k):
    for s, nm in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add("upper" + nm, tube([np.array(p) * m for p in [(0.26, 0.0, 0.63), (0.34, -0.03, 0.52),
                                                            (0.37, -0.05, 0.44)]], [0.095, 0.09, 0.09]),
              ROCK, "arm" + nm, surface="shell")
        fc, fr = np.array((0.38, -0.06, 0.35)) * m, np.array((0.125, 0.135, 0.165))
        k.add("fist" + nm, ell(fc, fr), SNOW, "arm" + nm, surface="fur")
        nt.clumps(k, "fistclump" + nm, fc, fr, 18, 0.055, "arm" + nm, el_range=(-30, 75), seed=7 + s, sink=0.6)
        k.paint(lambda P, fc=fc: smoothstep(fc[2] - 0.06, fc[2] - 0.15, P[:, 2])
                * inside(np.hypot(P[:, 0] - fc[0], P[:, 1] - fc[1]) - 0.2, 0.01), SHADE, on=["fist"])
        # four long dark claws like old wood out of the drift's foot, curving down and forward
        for t in (-1.5, -0.5, 0.5, 1.5):
            x0 = (0.38 + 0.05 * t) * s
            pts = [np.array((x0, -0.1 + 0.012 * abs(t), 0.24)), np.array((x0 + 0.008 * t * s, -0.16, 0.15)),
                   np.array((x0 + 0.014 * t * s, -0.19, 0.075))]
            k.add(f"claw{nm}{int(t * 2 + 3)}", outward(blade(pts, [0.03, 0.022, 0.006], (1, 0, 0), 0.62)),
                  CLAW if abs(t) > 1 else CLAWPALE, "arm" + nm, fuse=False, surface="shell")


def face(k):
    k.add("face", ell(FC, FR), FACE, "head", surface="fur")
    k.add("beard", ell(FC + (0, -0.02, -0.075), (0.13, 0.1, 0.07)), FACE, "head", surface="fur")
    # shadowed sockets round the eyes
    def socket(P):
        q = P - FC
        d = np.hypot((np.abs(q[:, 0]) - 0.055) / 0.05, (q[:, 2] - 0.058) / 0.034)
        return inside(d - 1, 0.1) * (q[:, 1] < -0.06)
    k.paint(socket, SOCKET, on=["face", "brow"])
    # icicles: short ones from the brow's edge, longer ones from the chin
    def icicle(name, root, length, w):
        root = np.array(root)
        pts = [root + (dx, dy, 0.012) for dx, dy in ((-w, -w * 0.6), (w, -w * 0.6), (w * 0.8, w * 0.7), (-w * 0.8, w * 0.7))]
        pts += [root + (0, -0.004, -length * 0.55) + (w * 0.25, 0, 0), root + (0.002, -0.006, -length)]
        k.add(name, hull(pts), ICE, "head", fuse=False, surface="shell")
    for j, t in enumerate((-2.6, -3.2, 2.6, 3.2)):
        icicle(f"browice{j}", (0.05 * t, -0.29 + 0.04 * (abs(t) - 2.6), 0.645), 0.05 + 0.03 * (3.2 - abs(t)), 0.016)
    for j, t in enumerate((-3, -2, -1, 0, 1, 2, 3)):
        icicle(f"chinice{j}", (0.035 * t, -0.29 + 0.01 * abs(t), 0.46 + 0.004 * abs(t)),
               0.13 - 0.016 * abs(t) + 0.02 * (j % 2), 0.017)
    # the blush
    for s in (1, -1):
        p, n = nt.on_ell(FC, FR, 46 * s, 12)
        k.decal(p, n, (0.026, 0.016), CHEEK, eps=0.002, rings=10, segs=40)
    # the wide open mouth, dark, frost on its lower lip
    Mo, Fr = srgb(MOUTH), srgb(FROST)
    def mouth(u, v):
        c = np.tile(Mo, (len(u), 1))
        c[v < -0.45 + 0.25 * u * u] = Fr
        return c
    out_ = polygon([(-1.0, 0.45), (-0.5, 0.75), (0.0, 0.62), (0.5, 0.75), (1.0, 0.45), (0.85, -0.4), (0.4, -0.9),
                    (-0.4, -0.9), (-0.85, -0.4)])
    p, n = nt.on_ell(FC, FR, 0, -6)
    k.decal(p, unit(0, -1, -0.05), (0.075, 0.045), mouth, eps=0.003, outline=out_, rings=14, segs=72)


def open_eye(u, v):
    c = np.tile(srgb(EYEICE), (len(u), 1))
    c[((u + 0.08) / 0.36) ** 2 + ((v + 0.12) / 0.42) ** 2 < 1] = srgb(PUPIL)
    c[((u - 0.05) / 0.14) ** 2 + ((v - 0.05) / 0.14) ** 2 < 1] = srgb("#FFFFFF")
    edge = 0.28 + 0.42 * u                       # the brow's line, down toward the nose (u outward)
    c[v > edge - 0.17] = srgb(PUPIL)
    c[v > edge] = srgb(SOCKET)
    return c


def lay_eyes(k):
    Sk, Ik = srgb(SOCKET), srgb(PUPIL)
    def lay(state):
        if state == "Fierce":
            def look(u, v):
                c = open_eye(u, v)
                c[v > -0.1 + 0.7 * u - 0.17] = Ik
                c[v > -0.1 + 0.7 * u] = Sk
                return c
        else:
            look = eye_look(state, open_eye, Sk, srgb(EYEICE), line=0.2)
        for s in (1, -1):
            p, _ = nt.on_ell(FC, FR, 21 * s, 26)
            col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
            k.decal(p + np.array([0, 0.0005 * s, 0]), unit(0.34 * s, -0.94, 0.05), (0.03, 0.024), col, eps=0.0022,
                    rings=20, segs=56)
    return lay


def nameton(src, out):
    k = Kit(src, coat="fur")
    body(k)
    legs(k)
    drift(k)
    arms(k)
    face(k)
    k.build(voxel=VOXEL, smooth=4, band=0.008, wband=0.07)
    nt.board(k.ob)
    k.eye_states(lay_eyes(k))
    k.export(out)


if __name__ == "__main__":
    nameton(*args())
