# Pidgeot-Mega sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pidgeotmega.py -- <the export's Pidgeot-Mega.glb> <out.glb>
#
# Grown from pidgeot.py (sibling: its frame, and through it pidgeotto.py's and pidgey.py's body, face, beak, black
# wedge, almond eye, tail, thighs and legs). From the two pictures, what the Mega changes, largest mass first: the
# wings grown great, cream, their long primaries parted like fingers and tipped in blue, a bright orange band along
# the leading edge and over the shoulder; the crest a big sheaf of yellow plumes swept up and back, red-pink quills
# rising behind it, one long pink streamer trailing back from the crown and one hanging forward past the beak; the
# tail long feathers of red-pink tipped in blue; the back, crown and brow orange where Pidgeot's are brown.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, sibling, slab, srgb, unit, tube, args

pg = sibling("pidgeot")
o, p = pg.o, pg.p
ORANGE, CREAM, GOLD = "#E8893C", "#F6E6B2", "#F6D650"
PINK, PINKTIP, BLUE = "#E2466A", "#C9304E", "#1E98D8"
SHIFT = np.array([0, -0.02, 0.008])                  # the Mega's head joint against Pidgeot's


def frame():
    F = pg.frame()
    F.H = F.H + SHIFT
    F.BEAK = tuple(np.array(F.BEAK[0]) + SHIFT for _ in (0,)) + (F.BEAK[1],)
    F.BEAKLOW = tuple(np.array(F.BEAKLOW[0]) + SHIFT for _ in (0,)) + (F.BEAKLOW[1],)
    F.NECK = (tuple(np.array(F.NECK[0]) + SHIFT * 0.5), F.NECK[1])
    F.TAILBASE = F.TAILBASE + np.array([0, 0, 0.014])
    F.SHOULDER = np.array([0.155, -0.02, 0.48])
    F.EYE = (F.EYE[0] * 1.25, F.EYE[1] * 1.25)
    F.HR = F.HR * 1.06                              # a tenth larger for the face at the game's size, less the crest
    return F


def crest(k, F):
    H = F.H
    # the crown painted gold from the forehead back
    k.paint(lambda P: p.inside(H[2] + 0.05 + 0.25 * (P[:, 1] - H[1]) - P[:, 2], 0.003) * (P[:, 1] < H[1] + 0.08)
            * (P[:, 2] > H[2]), GOLD, on=["head", "neck"])
    # red-pink quills rising behind the gold
    for x, t, w in [(0.04, (0.08, 0.24, 0.3), 0.06), (-0.04, (-0.08, 0.24, 0.3), 0.06), (0.0, (0.0, 0.3, 0.36), 0.065)]:
        k.add("plume", p.spike(H + (x * 0.5, -0.02, 0.07), H + t, w, 0.03, side=(1, 0, 0), at=0.3), PINK, "plume",
              fuse=False)
    # the long streamer trailing back from the crown, lifting at its end
    k.add("streamer", tube([H + (0.0, 0.02, 0.1), H + (0.02, 0.25, 0.2), H + (0.04, 0.5, 0.22), H + (0.05, 0.66, 0.26),
                            H + (0.05, 0.73, 0.34)], [0.022, 0.02, 0.018, 0.016, 0.014]), PINK, "plume", fuse=False)
    # the one hanging forward past the beak
    k.add("forelock", tube([H + (0.0, -0.08, 0.12), H + (0.02, -0.22, 0.15), H + (0.035, -0.34, 0.1),
                            H + (0.035, -0.4, 0.0), H + (0.025, -0.37, -0.08)], [0.02, 0.019, 0.017, 0.016, 0.015]),
          PINK, "plume", fuse=False)
    # the gold sheaf swept up and back over them
    for x, z0, t, w in [(0.0, 0.08, (0.0, 0.34, 0.22), 0.11), (0.05, 0.065, (0.09, 0.3, 0.17), 0.09),
                        (-0.05, 0.065, (-0.09, 0.3, 0.17), 0.09), (0.0, 0.1, (0.0, 0.18, 0.28), 0.08)]:
        k.add("plume", p.spike(H + (x * 0.6, -0.085, z0), H + t, w, 0.034, side=(1, 0, 0), at=0.35), GOLD, "plume",
              fuse=False)


ELEV, SWEEP = 57, 18
PLATE = [(0.0, -0.02), (0.14, -0.04), (0.3, -0.045), (0.44, -0.03), (0.47, 0.03), (0.42, 0.12), (0.3, 0.2),
         (0.16, 0.23), (0.05, 0.21), (0.0, 0.16)]
BAND = [(0.0, -0.026), (0.14, -0.046), (0.3, -0.05), (0.44, -0.035), (0.46, 0.01), (0.34, 0.04), (0.18, 0.07),
        (0.06, 0.1), (0.0, 0.12)]
# the parted feathers: (root along, root across, angle from the span toward the chord, length, width)
FEATHERS = [(0.4, 0.0, 2, 0.4, 0.09), (0.38, 0.06, 12, 0.39, 0.09), (0.35, 0.11, 22, 0.37, 0.088),
            (0.3, 0.16, 33, 0.34, 0.086), (0.23, 0.2, 45, 0.3, 0.084)]


def feather_outline(L, w):
    return [(0.0, -w / 2), (L * 0.82, -w / 2), (L, -w * 0.15), (L, w * 0.15), (L * 0.82, w / 2), (0.0, w / 2)]


def wings(k, F):
    e, s = math.radians(ELEV), math.radians(SWEEP)
    d = np.array([math.cos(e) * math.cos(s), math.cos(e) * math.sin(s), math.sin(e)])
    c0 = np.array([0.35, 0.3, -0.9])
    c = c0 - d * (d @ c0); c /= np.linalg.norm(c)
    root = F.SHOULDER - d * 0.04
    k.add("wing", slab(PLATE, root, d, c, 0.024), CREAM, "wing{s}", fuse=False, mirror=True)
    k.add("wingband", slab(BAND, root, d, c, 0.032), ORANGE, "wing{s}", fuse=False, mirror=True)
    for i, (a, b, ang, L, w) in enumerate(FEATHERS):
        r = math.radians(ang)
        fd = d * math.cos(r) + c * math.sin(r)
        fc = -d * math.sin(r) + c * math.cos(r)
        o0 = root + d * a + c * b
        k.add(f"feather{i}", slab(feather_outline(L, w), o0, fd, fc, 0.02), CREAM, "wing{s}", fuse=False, mirror=True)
        k.add(f"feathertip{i}", slab(feather_outline(L * 0.3, w * 1.04), o0 + fd * L * 0.7, fd, fc, 0.026), BLUE,
              "wing{s}", fuse=False, mirror=True)
    k.add("shoulder", p.ell(F.SHOULDER + np.array([-0.02, 0.0, -0.01]), (0.065, 0.075, 0.065)), ORANGE, "wing{s}",
          mirror=True)


def tail(k, F):
    """Long tail feathers, red-pink tipped in blue: Pidgeot's fan made longer, each tip a thicker blue plate."""
    p.tail(k, F, [(0, 0.34, 0.08, PINK, None), (12, 0.31, 0.074, PINKTIP, None), (-12, 0.31, 0.074, PINKTIP, None),
                  (24, 0.28, 0.07, PINK, None), (-24, 0.28, 0.07, PINK, None)], thick=0.022)
    base = F.TAILBASE
    pt = math.radians(-10)
    for i, (yaw, ln, w) in enumerate([(0, 0.34, 0.08), (12, 0.31, 0.074), (-12, 0.31, 0.074), (24, 0.28, 0.07),
                                      (-24, 0.28, 0.07)]):
        y = math.radians(yaw)
        dd = np.array([math.sin(y) * math.cos(pt), math.cos(y) * math.cos(pt), math.sin(pt)])
        side = np.array([math.cos(y), -math.sin(y), 0])
        rr = math.radians(1.5 * yaw)
        side = side * math.cos(rr) - np.array([0, 0, 1.0]) * math.sin(rr)
        bb = base + np.array([math.sin(y) * 0.02, 0, -0.012 * abs(yaw) / 20])
        half = [(a * ln, h * w / 2 * 1.03) for a, h in p.FEATHER if a >= 0.6]
        outline = [(0.68 * ln, half[0][1])] + half[1:] + [(a, -h) for a, h in half[::-1][1:-1]] + [(0.68 * ln, -half[0][1])]
        k.add(f"tailtip{i}", slab(outline, bb, dd, side, 0.03), BLUE, "tail", fuse=False)


def eyes(k, F):
    LID = srgb(CREAM)
    CB = srgb(ORANGE)
    E = p.eye_centre(F)
    m = np.array([-1, 1, 1.0])
    def lay(state):
        base = p.eye_look(state, p.eye_open, LID, p.INK, line=0.17)
        def look(u, v):
            col = base(u, v)
            col[v > 0.32 + 0.38 * u] = CB
            return col
        k.decal(E, F.EYEDIR, F.EYE, look, eps=0.006, outline=p.polygon(p.ALMOND), rings=20, segs=56)
        k.decal(E * m, F.EYEDIR * m, F.EYE, lambda u, v: look(-u, v), eps=0.006,
                outline=p.polygon([(-a, b) for a, b in p.ALMOND]), rings=20, segs=56)
    return lay


def pidgeotmega(src, out):
    F = frame()
    k = Kit(src, coat="fur")
    p.body(k, F, back=ORANGE)
    p.face(k, F, cream=CREAM)
    p.beak(k, F)
    crest(k, F)
    wings(k, F)
    tail(k, F)
    o.thighs(k, F, colour=CREAM, size=(0.058, 0.066, 0.072))
    p.legs(k, F)
    k.build(voxel=F.VOXEL)
    k.eye_states(eyes(k, F))
    k.export(out)


if __name__ == "__main__":
    pidgeotmega(*args())
