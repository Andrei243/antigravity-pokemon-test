# Centiskorch sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/centiskorch.py -- <the export's Centiskorch.glb> <out.glb>
#
# Sizzlipede grown (sizzlipede.py's line and frame, its paints by arc length, its eyes, legs and fangs): from the
# references, a long red centipede whose front half rears up in a thick column and whose rear lies on the ground in a
# curve, the tail's end lifting. The rear's back is dark maroon plates over red sides, the column red all round, thin
# dark seams between all the segments. Down the front of the column a great disc on each segment: orange-red with a
# yellow ring and a yellow middle. The head bends forward over the column like a hood; yellow eyes cut by an angry brow,
# orange fangs under the snout; a moustache of flame sweeping out and down from each side of the snout and curling up
# at its end, and two flames rising from the brow and curling out. Two pairs of flames spreading from the tail's end.
# Legs: many short dark maroon claws down both sides, along the rear and up the column.
# (The task's text said legs at the thickness floor and a glowing belly of rings: the pictures show filled discs, not
# rings, and claws thick enough to read; the flames are the design's and are kept within the model's height.)
# Skeleton: body and tail (0.22, 0.3, 0.05), seg1 (0.217, 0.181, 0.05), seg2 (0.157, 0.08, 0.047), seg3 (0.05, 0.02,
# 0.05), seg4 (-0.006, -0.04, 0.086), seg5 (-0.019, -0.067, 0.172), seg6 (0, -0.06, 0.3), seg7 (0.001, -0.043, 0.424),
# seg8 (0, -0.05, 0.529), head (0, -0.08, 0.6).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, unit, outward, args, sibling

sz = sibling("sizzlipede")
RED, BACK, SEAM, BACKSEAM, INK = sz.RED, sz.BACK, sz.SEAM, sz.BACKSEAM, sz.INK
LEG, DISC, YELLOW, ORANGE = "#6E2C2A", "#EE5A2A", "#F8C83A", sz.ORANGE
FLAME, HEART = "#FF7A22", "#FFD23A"

LINE = [(0.245, 0.45, 0.125), (0.238, 0.38, 0.078), (0.222, 0.3, 0.066), (0.214, 0.18, 0.07), (0.16, 0.08, 0.074),
        (0.065, 0.015, 0.08), (0.005, -0.035, 0.115), (-0.01, -0.058, 0.19), (0.0, -0.058, 0.3), (0.0, -0.048, 0.415),
        (0.0, -0.052, 0.52), (0.0, -0.082, 0.592)]
RADS = [0.048, 0.062, 0.07, 0.074, 0.078, 0.084, 0.092, 0.1, 0.102, 0.1, 0.095, 0.085]
BONES = ["tail", "tail", "body", "seg1", "seg2", "seg3", "seg4", "seg5", "seg6", "seg7", "seg8"]
HC, HR = np.array([0.0, -0.14, 0.632]), np.array([0.124, 0.134, 0.078])


def bone_of(s):
    i = int(np.clip(math.floor(s), 0, len(BONES) - 1))
    return BONES[i]


def head(k):
    def hood(V):
        V = V.copy()
        V[:, 2] -= 0.25 * np.clip(-V[:, 1], 0, 1) ** 2      # the snout bent down
        V[:, 0] *= 1 - 0.15 * np.clip(-V[:, 1], 0, 1)
        return V
    k.add("head", ell(HC, HR, deform=hood), RED, "head")
    k.add("nape", ell((0, -0.07, 0.61), (0.088, 0.08, 0.065)), RED, "head")


def discs(k, line):
    """The discs down the column's front: orange-red with a yellow ring and a yellow middle."""
    D, Y = srgb(DISC), srgb(YELLOW)
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(D, (len(u), 1))
        c[(r > 0.5) & (r < 0.7)] = Y
        c[r < 0.28] = Y
        return c
    for s in (6.55, 7.5, 8.45, 9.4):
        c, t, b, sd, r = line.at(s)
        k.decal(c + b * r * 0.95, b, (r * 0.64, r * 0.64), look, up=t, eps=0.003, rings=18, segs=56)


def fangs(k):
    for sg, nm in ((1, "R"), (-1, "L")):
        m = np.array([sg, 1, 1.0])
        P = np.array([(0.03, -0.24, 0.588), (0.055, -0.258, 0.552), (0.07, -0.252, 0.518)]) * m
        k.add(f"fang{nm}", outward(tube(P, [0.017, 0.013, 0.005], seg=12, per=6)), ORANGE, "head", fuse=False,
              surface="shell")


def joined(shapes):
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(v); F.extend([[i + off for i in q] for q in f]); off += len(v)
    return np.vstack(V), F


def tongue(pts, r0, flat_axis, heart=True):
    """A flame along pts tapering to a point, flattened across flat_axis; and its yellow heart, thinner, inside it
    standing out of both faces."""
    pts = np.array(pts, float)
    # resampled along its points, its width waving as it goes, so the edge is a flame's and not a horn's
    tt = np.linspace(0, len(pts) - 1, 3 * len(pts) - 2)
    pts = np.array([np.interp(tt, np.arange(len(pts)), pts[:, j]) for j in range(3)]).T
    n = len(pts)
    ph = float(pts[0].sum()) * 37.0
    radii = [r0 * max(1 - 0.92 * i / (n - 1), 0.08) ** 0.75 * (1 + 0.28 * math.sin(2.6 * i + ph)) for i in range(n)]
    outer = outward(blade(pts, radii, flat_axis, 0.5))
    if not heart:
        return outer, None
    m = max(2, int(n * 0.6))
    inner = outward(blade(pts[:m], [r * 0.5 for r in radii[:m]], flat_axis, 1.2))
    return outer, inner


def flames(k):
    groups = {}
    def put(bone, pts, r0, axis):
        o, h = tongue(np.array(pts, float), r0, axis)
        groups.setdefault((bone, "o"), []).append(o)
        if h is not None:
            groups.setdefault((bone, "h"), []).append(h)
    for sg in (1, -1):
        m = np.array([sg, 1, 1.0])
        # the moustache: out and down from the snout's side, curling up at its end
        put("head", np.array([(0.07, -0.215, 0.6), (0.14, -0.25, 0.575), (0.21, -0.25, 0.52), (0.265, -0.23, 0.51),
                              (0.29, -0.21, 0.55)]) * m, 0.032, (0, 1, 0))
        # the brow's flames: up and out, curling outward
        put("head", np.array([(0.05, -0.15, 0.69), (0.1, -0.18, 0.76), (0.16, -0.17, 0.81), (0.2, -0.15, 0.83)]) * m,
            0.03, (0, 1, 0))
    # the tail's two pairs
    tip = np.array(LINE[0])
    for d, L in (((0.55, 0.35, 0.75), 0.11), ((-0.45, 0.4, 0.8), 0.1), ((0.75, 0.55, 0.1), 0.1),
                 ((-0.35, 0.85, 0.2), 0.09)):
        d = unit(*d)
        side = np.cross(d, (0, 0, 1.0)); side /= max(np.linalg.norm(side), 1e-6)
        curl = unit(*(d + np.array((0, 0, 0.6))))
        pts = [tip + d * 0.01, tip + d * L * 0.45, tip + d * L * 0.8 + curl * 0.02, tip + d * L + curl * 0.045]
        put("tail", pts, 0.026, np.cross(d, side))
    for (bone, kind), shapes in groups.items():
        if kind == "o":
            k.add(f"flame{bone}", joined(shapes), FLAME, bone, fuse=False, glow=FLAME)
        else:
            k.add(f"heart{bone}", joined(shapes), HEART, bone, fuse=False, glow=HEART)


def claw(k, name, base, d, length, w, bone):
    """A short dark claw curling down at its end: two hulls, a root and a hooked tip."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    mid = base + d * length * 0.6
    mid[2] = max(mid[2], 0.012)
    tip = mid + unit(*(d + np.array((0, 0, -1.2)))) * length * 0.5
    tip[2] = max(tip[2], 0.004)
    sz.spike(k, name + "a", base, mid + d * w * 0.4, w, bone, LEG, along=(0, 0, 1.0))
    sz.spike(k, name + "b", mid - d * w * 0.3, tip, w * 0.8, bone, LEG, along=(0, 0, 1.0))


def legs(k, line):
    for i, s in enumerate(np.arange(0.9, 10.2, 0.62)):
        c, t, b, sd, r = line.at(s)
        upright = abs(t[2]) > 0.6
        for sg, nm in ((1, "R"), (-1, "L")):
            if upright:
                base = c + sd * sg * r * 0.8 + b * r * 0.2
                d = sd * sg * 0.85 + b * 0.35 - t * 0.2
            else:
                base = c + sd * sg * r * 0.72 + b * r * 0.5
                d = sd * sg * 0.75 + b * 0.5 - t * 0.2
            claw(k, f"leg{i}{nm}", base, d, 0.068, 0.0135, bone_of(s))


def centiskorch(src, out_path):
    k = Kit(src, coat="shell")
    line = sz.Line(LINE, RADS, rise=(0.11, 0.2))
    line.tubes(k, BONES, RED)
    head(k)
    k.paint(sz.top(line, -1, 5.7, width=-0.15), BACK, on=["seg"])
    k.paint(lambda Q: sz.seams(line, 0.5, 5.7, 0.09, half=0.003, start=0.02, edge=0.0012)(Q)
            * sz.top(line, -1, 5.7, width=-0.1)(Q), BACKSEAM, on=["seg"])
    k.paint(sz.seams(line, 5.7, 10.6, 0.11, half=0.003, start=0.055, edge=0.0012), SEAM, on=["seg"])
    discs(k, line)
    legs(k, line)
    fangs(k)
    flames(k)
    k.build(voxel=0.006, smooth=3, band=0.006, wband=0.05)
    k.eye_states(sz.eyes(k, HC, HR, (0.52, -0.76, 0.38), (0.039, 0.033), RED, eps=0.003))
    k.export(out_path)


if __name__ == "__main__":
    centiskorch(*args())
