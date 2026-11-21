# Audino sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/audino.py -- <the export's Audino.glb> <out.glb>
# From the references: a soft upright pear of a body, pink over the back, the sides and the arms like a coat whose
# front edges and hem are scalloped, cream down the whole front, the lower body and the legs; a round head, pink on top
# with a bump over each ear's root, the pink coming down to a point between the eyes and the face below cream; big
# upright blue eyes with a dark rim and a white glint; a small smile; great floppy ears out to the sides and drooping,
# pink above and cream below with a wavy edge; a cream feeler hanging from under each ear, curled at its end; short
# pink arms held forward and a little out, cream hands; short cream legs and feet; a white puff of a tail behind.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args, tri, lumpy

PINK, CREAM, WHITE, BLUE, NAVY, INK = "#F2A2B6", "#F5E3A6", "#FBF7F0", "#2E6CC8", "#1C2858", "#4A2E2A"
HEAD_C, HEAD_R = np.array([0.0, -0.01, 0.375]), np.array([0.102, 0.09, 0.082])
BODY_C, BODY_R = np.array([0.0, 0.0, 0.2]), np.array([0.112, 0.098, 0.135])


def wider_below(V):
    V = V.copy()
    s = 1 + 0.12 * np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= s; V[:, 1] *= s
    return V


def build(src, out):
    k = Kit(src, coat="fur")
    k.add("body", ell(BODY_C, BODY_R, deform=wider_below), PINK, "body")
    k.add("neck", ell((0, -0.01, 0.31), (0.08, 0.075, 0.05)), PINK, "head")
    k.add("head", ell(HEAD_C, HEAD_R), PINK, "head")
    # the bumps over the ears' roots
    k.add("bump", ell((0.065, 0.0, 0.435), (0.058, 0.055, 0.042)), PINK, "ear{s}", mirror=True)
    # the great ears, out to the sides and drooping, their broad faces to the front
    k.add("ear", blade([(0.075, 0.0, 0.42), (0.13, -0.004, 0.416), (0.18, -0.008, 0.388), (0.208, -0.012, 0.35)],
                       [0.04, 0.05, 0.045, 0.02], (0, 1, 0.25), 0.34), PINK, "ear{s}", mirror=True)
    # arms: short, forward and a little out; cream hands
    k.add("arm", tube([(0.085, -0.02, 0.28), (0.145, -0.06, 0.245), (0.175, -0.1, 0.205)], [0.04, 0.036, 0.033]),
          PINK, "arm{s}", mirror=True)
    k.add("hand", ell((0.185, -0.118, 0.195), (0.034, 0.032, 0.031)), CREAM, "arm{s}", mirror=True)
    # short cream legs and feet
    k.add("leg", tube([(0.055, 0.0, 0.1), (0.06, -0.01, 0.035)], [0.047, 0.044]), CREAM, "leg{s}", mirror=True)
    k.add("foot", ell((0.06, -0.035, 0.021), (0.046, 0.06, 0.023)), CREAM, "leg{s}", mirror=True)
    # the white puff of a tail, low behind and out to its right
    k.add("tail", ell((0.035, 0.12, 0.15), (0.064, 0.058, 0.07), deform=lumpy(5, 3, 0.08)), WHITE, "tail")

    # the feelers: cream cords out of the ears' undersides, hanging and curling outward at their ends
    for s, side in ((1, "R"), (-1, "L")):
        c = np.array([0.158 * s, -0.035, 0.29])
        pts = [(0.122 * s, -0.012, 0.385), (0.14 * s, -0.022, 0.35), (0.152 * s, -0.03, 0.322)]
        rad = [0.017, 0.016, 0.016]
        for t in np.linspace(0, 1, 14)[1:]:
            a = math.pi * 0.5 - t * 2 * math.pi * 1.0
            r = 0.03 * (1 - 0.55 * t)
            pts.append(c + np.array([s * r * math.cos(a) - 0.0 * s, 0, r * math.sin(a)]) + np.array([-0.0 * s, 0, 0]))
            rad.append(0.016 - 0.004 * t)
        k.add(f"feeler{side}", tube(pts, rad), CREAM, f"ear{side}", fuse=False)

    # --- cream: the front of the body (the coat's edges scalloped), the hem, the face below the eyes, the ears' undersides
    def front(Q):
        x, y, z = Q[:, 0], Q[:, 1], Q[:, 2]
        phi = np.arctan2(x, -y)
        lim = np.clip(0.72 + 1.6 * (0.3 - z), 0.72, 1.12) + 0.07 * tri(z / 0.09)
        hem = 0.112 + 0.012 * tri(phi * 6 / math.pi)
        return np.maximum(inside(np.abs(phi) - lim, 0.02) * (z < 0.34), inside(z - hem, 0.003))
    k.paint(front, CREAM, on=["body", "neck"])
    def face(Q):
        x, y, z = Q[:, 0], Q[:, 1], Q[:, 2]
        phi = np.arctan2(x, -y)
        return inside(z - (0.368 + 0.5 * np.abs(x)), 0.003) * inside(np.abs(phi) - 1.7, 0.03)
    k.paint(face, CREAM, on=["head", "neck"])
    def under(Q):
        a = np.abs(Q[:, 0])
        line = 0.44 - 0.55 * (a - 0.075) + 0.007 * tri(a / 0.038)
        return inside(Q[:, 2] - line, 0.003) * (a > 0.11)
    k.paint(under, CREAM, on=["ear"])

    # the small smile
    Cc, M = srgb(CREAM), srgb(INK)
    def smile(u, v):
        c = np.tile(Cc, (len(u), 1))
        c[(np.abs(v - (-0.35 + 0.8 * u * u)) < 0.22) & (np.abs(u) < 0.85)] = M
        return c
    k.decal((0, -0.09, 0.345), unit(0, -1, 0.15), (0.026, 0.013), smile, eps=0.004, rings=14, segs=64)

    # --- the eyes: big upright blue ovals, a dark rim, a white glint; lid pink above the face's line --------------
    EC = np.array([0.046, -0.086, 0.385])
    de = unit(0.35, -0.93, 0.1)
    half = (0.021, 0.031)
    Bl, Nv, P = srgb(BLUE), srgb(NAVY), srgb(PINK)
    def eye(u, v):
        c = np.tile(Bl, (len(u), 1))
        c[v < -0.3] = Bl * 0.75 + srgb("#5AA0F0") * 0.25
        c[u * u + v * v > 0.8 ** 2] = Nv
        return c
    def eyes(state):
        look = eye_look(state, eye, Cc, Nv, line=0.15)
        def two_lids(u, v):
            c = look(u, v)
            lid = np.all(np.isclose(c, Cc), axis=1) & (v > 0.15 + 0.33 * u)
            c[lid] = P
            return c
        k.decal(EC, de, half, two_lids, eps=0.005, rings=20, segs=56, mirror=True)
        if state in ("Open", "Fierce"):
            n = de; right = np.cross((0, 0, 1), n); right /= np.linalg.norm(right); upv = np.cross(n, right)
            v0 = 0.35 if state == "Open" else -0.05
            k.decal(EC + right * (-0.18) * half[0] + upv * v0 * half[1], n, (half[0] * 0.3, half[1] * 0.26),
                    "#FFFFFF", eps=0.0068, rings=8, segs=40, mirror=True)

    k.build(voxel=0.0055, smooth=4, band=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    build(*args())
