# Audino's Mega Evolution sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/audinomega.py -- <the export's Audino-Mega.glb> <out.glb>
# Audino grown (audino.py's body, head, arms, legs, tail, smile and eye copied here, since its build takes no
# arguments). From the references: the pink coat turned white and grown into a long coat whose two broad flaps hang at
# the sides nearly to the ground, flaring out, their rounded ends pink; cream down the front and the round lower belly;
# the head white above, cream below the eyes; the ears grown tall, standing up and out like wings, white with pink
# tips and a cream frill of three feathers along each outer edge; the feelers cream cords curling into a big spiral
# beside each cheek; a white puff of a ruff under the chin with a cream knot under it; big rose-red eyes with a white
# glint; white sleeves, cream hands; short lilac legs with white tufts at the ankles; a big white puff of a tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, slab, srgb, inside, unit, eye_look, args, tri, lumpy, outward

WHITE, CREAM, PINK, LILAC, ROSE, ROSE_DARK, INK = "#FBF8F6", "#F4E6AE", "#F2A2B6", "#BFB2D0", "#E2486E", "#7E1C38", "#4A2E2A"
FRILL = "#F1DE9A"
HEAD_C, HEAD_R = np.array([0.0, -0.015, 0.38]), np.array([0.11, 0.096, 0.09])
BODY_C, BODY_R = np.array([0.0, 0.0, 0.2]), np.array([0.112, 0.098, 0.135])


def wider_below(V):
    V = V.copy()
    s = 1 + 0.12 * np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= s; V[:, 1] *= s
    return V


def build(src, path):
    k = Kit(src, coat="fur")
    k.add("body", ell(BODY_C, BODY_R, deform=wider_below), WHITE, "body")
    k.add("neck", ell((0, -0.01, 0.31), (0.08, 0.075, 0.05)), WHITE, "head")
    k.add("head", ell(HEAD_C, HEAD_R), WHITE, "head")
    # the coat: a cape over the back and two broad flaps at the sides hanging nearly to the ground, flaring out
    k.add("cape", ell((0, 0.05, 0.2), (0.125, 0.075, 0.15), deform=wider_below), WHITE, "body")
    k.add("flap", ell((0.112, 0.02, 0.15), (0.058, 0.085, 0.135), rot=(0, -16, 0)), WHITE, "body", mirror=True)
    # the ears: tall, standing up and out like wings, broad faces to the front
    k.add("bump", ell((0.062, 0.0, 0.445), (0.05, 0.05, 0.04)), WHITE, "ear{s}", mirror=True)
    k.add("ear", blade([(0.07, 0.0, 0.44), (0.1, 0.0, 0.5), (0.135, 0.0, 0.56), (0.165, 0.0, 0.605)],
                       [0.045, 0.066, 0.062, 0.03], (0, 1, 0), 0.34), WHITE, "ear{s}", mirror=True)
    # arms: white sleeves, forward and a little out; cream hands
    k.add("arm", tube([(0.085, -0.02, 0.28), (0.14, -0.06, 0.245), (0.16, -0.1, 0.21)], [0.042, 0.038, 0.035]),
          WHITE, "arm{s}", mirror=True)
    k.add("hand", ell((0.165, -0.12, 0.2), (0.032, 0.03, 0.029)), CREAM, "arm{s}", mirror=True)
    # short lilac legs, white feet
    k.add("leg", tube([(0.055, 0.0, 0.1), (0.06, -0.01, 0.035)], [0.047, 0.044]), LILAC, "leg{s}", mirror=True)
    k.add("foot", ell((0.06, -0.035, 0.021), (0.046, 0.06, 0.023)), WHITE, "leg{s}", mirror=True)
    # the big white puff of a tail, low behind and out to its right
    k.add("tail", ell((0.04, 0.14, 0.16), (0.08, 0.075, 0.085), deform=lumpy(5, 3, 0.08)), WHITE, "tail")

    # the white tufts at the ankles: three short feathers swept back off each leg
    for s, side in ((1, "R"), (-1, "L")):
        for i, (a, ln) in enumerate(((-35, 0.05), (0, 0.06), (35, 0.05))):
            r = math.radians(a)
            d = unit(s * (0.75 + 0.25 * math.sin(r)), 0.55 + 0.25 * math.cos(r), 0.25 + 0.3 * math.sin(r))
            root = np.array((0.09 * s, -0.005, 0.06))
            k.add(f"tuft{side}{i}", outward(blade([root, root + d * ln * 0.6, root + d * ln], [0.017, 0.014, 0.004],
                                                  (0, 0, 1), 0.5)), WHITE, f"leg{side}", fuse=False)

    # the ears' cream frills: three feathers off each outer edge, faces to the front
    for s, side in ((1, "R"), (-1, "L")):
        roots = [(0.142, 0.004, 0.48), (0.158, 0.004, 0.535), (0.165, 0.004, 0.585)]
        for i, (ang, ln, w) in enumerate(((64, 0.085, 0.034), (40, 0.098, 0.036), (16, 0.088, 0.032))):
            a = math.radians(ang)
            d = np.array((math.sin(a) * s, 0.0, math.cos(a)))
            root = np.array(roots[i]) * np.array((s, 1, 1))
            outline = [(-w, 0.0), (w, 0.0), (w * 1.05, ln * 0.5), (w * 0.72, ln * 0.84), (0.0, ln), (-w * 0.72, ln * 0.84), (-w * 1.05, ln * 0.5)]
            ex = np.cross(d, (0, 1.0, 0)); ex /= np.linalg.norm(ex)
            k.add(f"frill{side}{i}", slab(outline, root, ex, d, 0.018), FRILL, f"ear{side}", fuse=False)

    # the feelers: cream cords out of the ears' roots, down past the cheeks, curling into a big spiral
    for s, side in ((1, "R"), (-1, "L")):
        c = np.array([0.155 * s, -0.05, 0.315])
        pts = [(0.1 * s, -0.02, 0.425), (0.128 * s, -0.035, 0.4), (0.15 * s, -0.045, 0.37)]
        rad = [0.013, 0.012, 0.012]
        for t in np.linspace(0, 1, 18)[1:]:
            a = math.pi * 0.5 - t * 2 * math.pi * 1.15
            r = 0.04 * (1 - 0.6 * t)
            pts.append(c + np.array([s * r * math.cos(a), 0, r * math.sin(a)]))
            rad.append(0.012 - 0.002 * t)
        k.add(f"feeler{side}", outward(tube(pts, rad)), CREAM, f"ear{side}", fuse=False)

    # the ruff under the chin: a white puff with a cream knot under it
    k.add("ruff", outward(ell((0, -0.098, 0.27), (0.058, 0.045, 0.05), deform=lumpy(4, 2, 0.05))), WHITE, "body",
          fuse=False)
    k.add("ruffcurl", outward(tube([(0.0, -0.13, 0.285), (0.015, -0.14, 0.31), (0.005, -0.13, 0.33)],
                                   [0.016, 0.012, 0.008])), WHITE, "body", fuse=False)
    k.add("knot", outward(ell((0, -0.118, 0.215), (0.024, 0.02, 0.02))), CREAM, "body", fuse=False)

    # --- cream: the front of the body down to the round lower belly; the face below the eyes ---------------------
    def front(Q):
        x, y, z = Q[:, 0], Q[:, 1], Q[:, 2]
        phi = np.arctan2(x, -y)
        lim = np.clip(0.55 + 1.6 * (0.24 - z), 0.55, 0.95)
        return inside(np.abs(phi) - lim, 0.02) * (z < 0.31) * inside(0.04 - z, 0.003) * (y < 0.02)
    k.paint(front, CREAM, on=["body", "neck"])
    def face(Q):
        x, y, z = Q[:, 0], Q[:, 1], Q[:, 2]
        phi = np.arctan2(x, -y)
        return inside(z - (0.37 + 0.45 * np.abs(x)), 0.003) * inside(np.abs(phi) - 1.6, 0.03)
    k.paint(face, CREAM, on=["head", "neck"])
    # the coat's flaps pink at their rounded ends; the ears' tips pink
    k.paint(lambda Q: inside(Q[:, 2] - 0.085, 0.008) * (np.abs(Q[:, 0]) > 0.07), PINK, on=["flap"])
    k.paint(lambda Q: inside(0.575 - Q[:, 2], 0.006), PINK, on=["ear"])

    # the small smile
    Cc, M = srgb(CREAM), srgb(INK)
    def smile(u, v):
        c = np.tile(Cc, (len(u), 1))
        c[(np.abs(v - (-0.35 + 0.8 * u * u)) < 0.22) & (np.abs(u) < 0.85)] = M
        return c
    k.decal((0, -0.104, 0.35), unit(0, -1, 0.15), (0.026, 0.013), smile, eps=0.004, rings=14, segs=64)

    # --- the eyes: big upright rose-red ovals, a dark rim, a white glint; lid white above the face's line ---------
    EC = np.array([0.048, -0.093, 0.392])
    de = unit(0.35, -0.93, 0.1)
    half = (0.024, 0.033)
    Ro, Rd, Wh = srgb(ROSE), srgb(ROSE_DARK), srgb(WHITE)
    def eye(u, v):
        c = np.tile(Ro, (len(u), 1))
        c[v < -0.3] = Ro * 0.7 + srgb("#FF8AA8") * 0.3
        c[u * u + v * v > 0.8 ** 2] = Rd
        return c
    def eyes(state):
        look = eye_look(state, eye, Cc, Rd, line=0.15)
        def two_lids(u, v):
            c = look(u, v)
            lid = np.all(np.isclose(c, Cc), axis=1) & (v > 0.15 + 0.33 * u)
            c[lid] = Wh
            return c
        k.decal(EC, de, half, two_lids, eps=0.005, rings=20, segs=56, mirror=True)
        if state in ("Open", "Fierce"):
            n = de; right = np.cross((0, 0, 1), n); right /= np.linalg.norm(right); upv = np.cross(n, right)
            v0 = 0.35 if state == "Open" else -0.05
            k.decal(EC + right * (-0.18) * half[0] + upv * v0 * half[1], n, (half[0] * 0.32, half[1] * 0.26),
                    "#FFFFFF", eps=0.0068, rings=8, segs=40, mirror=True)

    k.build(voxel=0.0055, smooth=4, band=0.006)
    k.eye_states(eyes)
    k.export(path)


if __name__ == "__main__":
    build(*args())
