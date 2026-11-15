# Hitmonlee sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/hitmonlee.py -- <the export's Hitmonlee.glb> <out.glb>
# From the references: a body that is all head, a brown dome narrowing to a waist, no neck and no mouth; two dark
# almond eyes slanting fiercely on its upper front, a white glint in each; short brown shoulders, forearms of cream
# rings stacked like a spring and brown three-fingered fists; long legs of the same rings, thicker at the top, down to
# brown ankles and great feet with three white claws and a yellow pad on the outer heel.
import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, outward, polygon, srgb, smoothstep, unit, eye_look, args

SKIN, RING, CLAW, PAD, INK = "#B48C78", "#E6D8A2", "#F2F0EA", "#EEC83C", "#1A1416"


def segment(k, name, a, b, r, bone):
    """One ring of a spring: a short drum with rounded shoulders from a to b, so each meets the next in a crisp groove."""
    a, b = np.array(a, float), np.array(b, float)
    d = b - a; L = np.linalg.norm(d)
    k.add(name, lathe(a, d, [(0, 0), (r * 0.74, 0), (r * 0.95, L * 0.18), (r, L * 0.5), (r * 0.95, L * 0.82),
                             (r * 0.74, L), (0, L)], seg=28), RING, bone, fuse=False, surface="shell")


def spring(k, tag, pts, r0, r1, step, bone):
    """Rings along a polyline, each step long, their radius going from r0 to r1."""
    pts = np.array(pts, float)
    seg_len = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    total = seg_len.sum()
    n = max(2, int(round(total / step)))
    def at(t):
        s = t * total
        for i, l in enumerate(seg_len):
            if s <= l or i == len(seg_len) - 1:
                return pts[i] + (pts[i + 1] - pts[i]) * min(s / l, 1)
            s -= l
    for i in range(n):
        t0, t1 = i / n, (i + 1) / n
        a, b = at(t0), at(t1)
        b = b + (b - a) * 0.06                    # a hair of overlap, so no gap shows between two rings
        segment(k, f"{tag}{i}", a, b, r0 + (r1 - r0) * (i + 0.5) / n, bone)


def hitmonlee(src, out):
    k = Kit(src, coat="fur")

    # The body that is a head: a tall dome narrowing to a waist
    def pear(V):
        V = V.copy()
        w = 0.6 + 0.4 * smoothstep(-1.0, 0.2, V[:, 2])
        V[:, 0] *= w; V[:, 1] *= w
        V[:, 1] += 0.06 * np.clip(V[:, 2], 0, 1)          # the crown a little back
        return V
    k.add("body", ell((0, 0.0, 0.67), (0.155, 0.14, 0.235), deform=pear), SKIN, "body")
    k.add("hips", ell((0, 0.005, 0.47), (0.082, 0.072, 0.045)), SKIN, "body")

    # The arms: a brown shoulder, a forearm of rings carried forward and down, a brown fist before the body
    SH = np.array([0.13, -0.02, 0.585])
    k.add("shoulder", ell(SH, (0.045, 0.044, 0.044)), SKIN, "arm{s}", mirror=True)
    k.add("upper", tube([SH, (0.165, -0.05, 0.56)], [0.04, 0.034]), SKIN, "arm{s}", mirror=True)
    FIST = np.array([0.175, -0.205, 0.46])
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        spring(k, f"armring{side}", np.array([(0.16, -0.05, 0.56), (0.19, -0.11, 0.51), (0.185, -0.165, 0.475)]) * m,
               0.03, 0.027, 0.022, f"arm{side}")
    k.add("fist", ell(FIST, (0.046, 0.046, 0.043)), SKIN, "arm{s}", mirror=True)
    for i, (x, z) in enumerate(((0.152, 0.478), (0.175, 0.47), (0.198, 0.46))):
        k.add(f"finger{i}", ell((x, -0.245, z - 0.012), (0.016, 0.02, 0.026)), SKIN, "arm{s}", mirror=True)
    k.add("thumb", ell((0.14, -0.225, 0.44), (0.017, 0.022, 0.016)), SKIN, "arm{s}", mirror=True)

    # The legs: rings thicker at the top, down to brown ankles and great feet with three white claws
    k.add("hipball", ell((0.065, 0.0, 0.445), (0.052, 0.05, 0.045)), SKIN, "leg{s}", mirror=True)
    AN = np.array([0.095, -0.015, 0.1])
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        spring(k, f"legring{side}", np.array([(0.066, 0.0, 0.43), (0.082, -0.01, 0.27), AN + (0, 0, 0.02)]) * m,
               0.046, 0.037, 0.034, f"leg{side}")
    k.add("ankle", ell(AN, (0.043, 0.043, 0.04)), SKIN, "leg{s}", mirror=True)
    k.add("foot", ell((0.1, -0.065, 0.036), (0.062, 0.098, 0.037)), SKIN, "leg{s}", mirror=True)
    k.add("heel", ell((0.1, 0.0, 0.04), (0.05, 0.04, 0.04)), SKIN, "leg{s}", mirror=True)
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        for j, dx in enumerate((-0.035, 0.0, 0.035)):
            b = np.array((0.1 + dx, -0.15, 0.03))
            k.add(f"claw{side}{j}", hull(np.array([b + (-0.016, 0.02, -0.02), b + (0.016, 0.02, -0.02),
                                                   b + (-0.013, 0.02, 0.02), b + (0.013, 0.02, 0.02),
                                                   b + (0.0, -0.04, -0.004)]) * m), CLAW, f"leg{side}", fuse=False,
                  surface="shell")
    # the yellow pad on the outer heel
    k.decal((0.15, 0.0, 0.045), unit(1, 0.35, 0.1), (0.022, 0.018), PAD, eps=0.003, rings=8, segs=36, mirror=True)

    # The eyes: dark almonds slanting down to the inner corner, a white glint, high on the dome's front
    CS, CK = srgb(SKIN), srgb(INK)
    OUT = [(-1.0, -0.42), (-0.55, -0.05), (0.0, 0.3), (0.55, 0.55), (0.95, 0.45), (1.0, 0.05),
           (0.6, -0.45), (0.0, -0.62), (-0.55, -0.62)]
    def open_eye(u, v):
        c = np.tile(CK, (len(u), 1))
        c[((u - 0.32) / 0.24) ** 2 + ((v - 0.12) / 0.26) ** 2 < 1] = (1, 1, 1)
        return c
    E = np.array([0.062, -0.12, 0.735])
    def lay(state):
        look = eye_look(state, open_eye, CS, CK, line=0.18)
        for s in (1, -1):
            n = unit(0.42 * s, -0.9, 0.12)
            shape = polygon([(u * s, v) for u, v in OUT])
            col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
            k.decal(E * np.array([s, 1, 1]), n, (0.048, 0.036), col, up=(0, 0.1, 1), eps=0.005, outline=shape,
                    rings=20, segs=56)

    k.build(voxel=0.0075, smooth=4)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    hitmonlee(*args())
