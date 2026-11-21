# Kartana sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/kartana.py -- <the export's Kartana.glb> <out.glb>
#
# The design, from the references: a swordsman of folded white paper, all flat facets and hard edges. Its head is a
# diamond as tall as its torso and legs together are wide, its upper edges folded over in orange (a chevron), its lower
# point orange, and in its middle a pale hollow with the eye: a yellow folded star edged in dark grey. Two white horn
# blades cross over the head's top. Under the head an orange knot at the waist, a small white torso, white hips folded
# orange on their inner faces, and two long pointed legs standing apart like an upturned V. Its arms are blades: the
# left a great crescent sweeping up past the head, the right a long straight knife held out and a little down. Behind
# each shoulder a fan of three gold strips rising outward.
# The C# sculptor grew the crescent from the head; both pictures grow it from the left shoulder: it is the left arm.
# The brief expected orange in the folds: it is there, with the gold of the fans.
# Skeleton: root, body (0.48), head (0.54), armL/armR (+-0.03, 0, 0.52), legL/legR (+-0.025, 0, 0.43).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, unit, boxy, eye_look, args, hull

# --- colours ----------------------------------------------------------------------------------------------------------
WHITE, SHADE, ORANGE, RED = "#F4F2EE", "#D9D7D4", "#EE6A1E", "#D4441A"
GOLD, GOLD_PALE, YELLOW, INK = "#F2BE1C", "#F8DC56", "#F6D21E", "#3A3B42"

# --- the frame --------------------------------------------------------------------------------------------------------
HEAD_C, HEAD_W, HEAD_H, HEAD_D = np.array([0.0, 0.0, 0.615]), 0.098, 0.135, 0.032
EYE_C, EYE_HALF = np.array([0.0, -HEAD_D - 0.006, 0.615]), 0.034
THICK = 0.03                       # a blade's thickness at its ridge (the floor at 0.93 tall)


def plate(outline, origin, ex, ez, thick, edge=0.006):
    """A convex blade from an outline in the plane of ex and ez: its rim thin, a ridge down its middle thick."""
    ex, ez = np.array(ex, float), np.array(ez, float)
    n = np.cross(ex, ez); n /= np.linalg.norm(n)
    o = np.array(origin, float)
    p = np.array(outline, float)
    mid = p.mean(axis=0)
    pts = []
    for a, b in p:
        q = o + ex * a + ez * b
        pts += [q + n * edge / 2, q - n * edge / 2]
    for t in np.linspace(0.15, 0.85, 3):
        a, b = mid + (p[0] - mid) * 0 + (p - mid).mean(axis=0) * 0
    q = o + ex * mid[0] + ez * mid[1]
    pts += [q + n * thick / 2, q - n * thick / 2]
    return hull(pts)


def strip(pts2d, widths, origin, ex, ez, thick, edge=0.006):
    """A curved blade as a chain of convex hulls: a middle line in the plane of ex and ez with a width at each point."""
    ex, ez = np.array(ex, float), np.array(ez, float)
    n = np.cross(ex, ez); n /= np.linalg.norm(n)
    o = np.array(origin, float)
    P = np.array(pts2d, float)
    T = np.gradient(P, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True)
    N = np.stack([-T[:, 1], T[:, 0]], axis=1)
    shapes = []
    for i in range(len(P) - 1):
        pts = []
        for j in (i, i + 1):
            w = widths[j] / 2
            for side in (-1, 1):
                a, b = P[j] + N[j] * w * side
                q = o + ex * a + ez * b
                pts += [q + n * edge / 2, q - n * edge / 2]
            q = o + ex * P[j][0] + ez * P[j][1]
            th = max(thick * min(1.0, widths[j] / 0.05), edge)
            pts += [q + n * th / 2, q - n * th / 2]
        shapes.append(hull(pts))
    return shapes


def spline(pts, n):
    """A Catmull-Rom curve through 2D points, n samples."""
    p = np.array(pts, float)
    ext = np.vstack([2 * p[0] - p[1], p, 2 * p[-1] - p[-2]])
    out = []
    per = max(2, n // (len(p) - 1))
    for i in range(len(p) - 1):
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        for j in range(per):
            t = j / per
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    out.append(p[-1])
    return np.array(out)


def head(k):
    c = HEAD_C
    pts = []
    for y in (-HEAD_D, HEAD_D):
        pts += [(0, y, c[2] + HEAD_H), (0, y, c[2] - HEAD_H), (HEAD_W, y, c[2]), (-HEAD_W, y, c[2])]
    k.add("head", hull(pts), WHITE, "head", fuse=False)
    # the orange folds: a chevron over the two upper edges and the lower point, each a thin plate over the front
    f = -HEAD_D - 0.004
    def on_front(u, v, lift=0.0):
        return (u * HEAD_W, f - lift, c[2] + v * HEAD_H)
    for s in (1, -1):
        band = [on_front(0, 1), on_front(s * 1, 0), on_front(s * 0.62, 0), on_front(0, 0.38)]
        k.add(f"fold{'R' if s > 0 else 'L'}", hull([p for q in band for p in (q, (q[0], q[1] + 0.006, q[2]))]),
              ORANGE, "head", fuse=False)
        back = [(x, HEAD_D + 0.004, z) for x, _, z in band]
        k.add(f"foldb{'R' if s > 0 else 'L'}", hull([p for q in back for p in (q, (q[0], q[1] - 0.006, q[2]))]),
              ORANGE, "head", fuse=False)
    tip = [on_front(0, -1), on_front(0.33, -0.67), on_front(0, -0.5), on_front(-0.33, -0.67)]
    k.add("chin", hull([p for q in tip for p in (q, (q[0], q[1] + 0.006, q[2]))]), RED, "head", fuse=False)
    # the pale hollow with the eye: a fused plate just proud of the front (the eye's patch needs it)
    k.add("face", ell((0, -HEAD_D - 0.002, c[2] - 0.005), (0.058, 0.0045, 0.058), rot=(0, 45, 0), deform=boxy(5), sub=4),
          SHADE, "head")
    # two horn blades crossing over the top
    for s in (1, -1):
        base = np.array([-s * 0.022, -0.004, c[2] + HEAD_H * 0.55])
        tipp = base + np.array([s * 0.085, -0.012, 0.15])
        d = (tipp - base) / np.linalg.norm(tipp - base)
        side = np.cross(d, (0, -1.0, 0)); side /= np.linalg.norm(side)
        pts = [base + side * 0.016 * p + np.array([0, 0.012 * q, 0]) for p in (-1, 1) for q in (-1, 1)] + [tipp]
        k.add(f"horn{'R' if s > 0 else 'L'}", hull(pts), WHITE, "head", fuse=False)


def body(k):
    # the waist's orange knot and the gold bands running out from it
    k.add("knot", hull([(x, y, z) for x in (-0.026, 0.026) for y in (-0.03, 0.022) for z in (0.448, 0.5)]), ORANGE,
          "body", fuse=False)
    k.add("knotfold", hull([(x, y, z) for x in (-0.012, 0.012) for y in (-0.036, 0.0) for z in (0.43, 0.5)]), RED,
          "body", fuse=False)
    # a small white torso tapering into the hips
    k.add("torso", hull([(x * s, y, z) for s in (1, -1) for x, z in ((0.05, 0.47), (0.03, 0.39)) for y in (-0.025, 0.025)]),
          WHITE, "body", fuse=False)
    # the hips, folded orange underneath
    k.add("hips", hull([(x * s, y, z) for s in (1, -1) for x, z in ((0.07, 0.4), (0.1, 0.36), (0.02, 0.33))
                        for y in (-0.032, 0.032)]), WHITE, "body", fuse=False)


def legs(k):
    for s, side in ((1, "R"), (-1, "L")):
        bone = f"leg{side}"
        A, Bp = np.array([0.012 * s, -0.036, 0.37]), np.array([0.095 * s, -0.036, 0.37])
        Ab, Bb = A + (0, 0.072, 0), Bp + (0, 0.072, 0)
        tip = np.array([0.172 * s, 0.0, 0.0])
        knee = np.array([0.105 * s, -0.026, 0.19])
        k.add(f"leg{side}", hull([A, Bp, Ab, Bb, tip, knee]), WHITE, bone, fuse=False)
        # the orange fold on its inner front, a little proud of the face
        nrm = np.cross(Bp - A, tip - A) * s
        nrm = nrm / np.linalg.norm(nrm)
        if nrm[1] > 0:
            nrm = -nrm
        tri = [A + nrm * 0.004, (A + Bp) / 2 + nrm * 0.004, A + (tip - A) * 0.42 + nrm * 0.004]
        k.add(f"legfold{side}", hull([p for q in tri for p in (q, q - nrm * 0.006)]), ORANGE, bone, fuse=False)


def arms(k):
    front = (1, 0, 0), (0, 0, 1)
    # the left arm: a great crescent sweeping up from the shoulder past the head, in a plane turned a little to the front
    P = spline([(-0.04, 0.47), (-0.15, 0.52), (-0.23, 0.63), (-0.245, 0.76), (-0.2, 0.87), (-0.12, 0.95)], 22)
    s = np.linspace(0, 1, len(P))
    W = 0.04 + 0.075 * np.sin(np.pi * np.clip(s * 1.15, 0, 1)) ** 0.8
    W[-1] = 0.006
    W = np.maximum(W * (1 - s ** 6), 0.006)
    for i, sh in enumerate(strip(P, W, (0, 0.012, 0), unit(1, 0.12, 0), (0, 0, 1), THICK)):
        k.add(f"crescent{i}", sh, WHITE if i % 2 == 0 else "#ECEAE6", "armL", fuse=False)
    # the right arm: a long straight knife held out and a little down and forward
    root = np.array([0.04, -0.01, 0.475])
    d = unit(0.92, -0.18, -0.2)
    up = np.cross(d, (0, -1.0, 0)); up = -up / np.linalg.norm(up)
    L = 0.42
    out2 = [(0.0, -0.024), (0.0, 0.024), (L * 0.8, 0.036), (L, 0.0), (L * 0.84, -0.03)]
    k.add("knife", plate(out2, root, d, up, THICK, edge=0.008), WHITE, "armR", fuse=False)
    # a gold band along its root and a row of white points on it
    band = [(0.04, -0.012), (0.04, 0.012), (0.2, 0.014), (0.2, -0.014)]
    k.add("knifeband", plate(band, root + np.cross(d, up) * -0.0, d, up, THICK + 0.008, edge=0.02), GOLD, "armR",
          fuse=False)
    # fans of three gold strips behind each shoulder, rising outward, spread so that they never alias into stripes
    for sgn, side in ((1, "R"), (-1, "L")):
        d = unit(sgn * 0.78, 0.15, 0.63)
        perp = unit(sgn * 0.63, 0.0, -0.78)
        for j, L in enumerate((0.3, 0.27, 0.24)):
            base = np.array([sgn * 0.05, 0.03 + 0.008 * j, 0.5]) + perp * (0.046 * j - 0.02)
            out2 = [(0.0, -0.014), (0.0, 0.014), (L, 0.014), (L - 0.02, -0.014)]
            k.add(f"fan{j}{side}", plate(out2, base, d, perp * -sgn, 0.022, edge=0.016),
                  GOLD if j % 2 == 0 else GOLD_PALE, f"arm{side}", fuse=False)


def eye_open():
    cy, ck, cg = srgb(YELLOW), srgb(INK), srgb(SHADE)
    def look(u, v):
        # a four-pointed folded star, its left point longest, edged in dark grey
        a = np.abs(u * np.where(u < 0, 0.8, 1.15)) ** 0.55 + np.abs(v * 1.1) ** 0.55
        c = np.tile(cg, (len(u), 1))
        c[a < 1.0] = ck
        c[a < 0.78] = cy
        c[(a < 0.78) & (v > 0.05) & (u > -0.1)] = srgb("#F9E87A")
        return c
    return look


def kartana(src, out_path):
    k = Kit(src, coat="fur")
    head(k)
    body(k)
    legs(k)
    arms(k)
    k.build(voxel=0.0035, smooth=3, band=0.004)

    def lay(state):
        look = eye_look(state, eye_open(), srgb(SHADE), srgb(INK), line=0.18)
        k.decal(EYE_C, (0, -1, 0), (EYE_HALF, EYE_HALF), look, eps=0.002, rings=24, segs=64)
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    kartana(*args())
