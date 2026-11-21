# Sirfetch'd sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sirfetchd.py -- "<the export's Sirfetch'd.glb>" <out.glb>
# Farfetch'd grown into a knight (farfetchd.py's way of making the bill, the crest and the eyes under a dark mark):
# a plump WHITE duck with a broad yellow bill, a crest of three white feathers, thick black brows slanting down to the
# middle over stern half-lidded eyes, short orange legs and yellow webbed feet. Its left wing (the model's -X, the
# viewer's left) holds a great leek upright beside it as a lance: a white bulb with tan roots at the ground, a white
# stalk wrapped at the grip by the leek's own white leaves, then pale green into a long green blade cut to a slanted
# point. Its right wing holds a shield of dark green leek leaves hanging down in layers, a pale green spike rising
# from its middle. Both are thick loose pieces held forward of the body.
# The skeleton: body (0, 0, 0.25), arms (+-0.07, -0.02, 0.3), head (0, -0.01, 0.35), tail (0, 0.07, 0.23),
# legs (+-0.04, 0, 0.13).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, slab, leaf, srgb, inside, smoothstep, unit, eye_look, polygon, outward,
                       args)

WHITE, SHADE, CREAMWHITE = "#F4F3EF", "#DCDBD6", "#ECEBE4"
BILL, LOWBILL, LEGS, FEET, BLACK = "#F4B634", "#DE9424", "#E8963A", "#F2BC3A", "#1E1C1E"
STALK, PALEGREEN, GREEN, TIP, ROOT = "#F1F0E6", "#C9E08E", "#8CC23E", "#6FA82E", "#C9A77A"
SHIELD_DARK, SHIELD_MID, SHIELD_LIGHT, SPIKE = "#1F5A2A", "#2C7436", "#3D8C3C", "#A9D257"
H = np.array([0, -0.035, 0.465])          # the head's centre
HR = np.array([0.106, 0.1, 0.102])


def sirfetchd(src, out):
    k = Kit(src, coat="fur")

    # --- the body: a plump white duck, fuller below, a breast standing forward --------------------------------------
    def pear(V):
        V = V.copy()
        s = 1 + 0.12 * np.clip(-V[:, 2], 0, 1) - 0.1 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell((0, 0.01, 0.245), (0.122, 0.128, 0.135), rot=(-8, 0, 0), deform=pear), WHITE, "body")
    k.add("breast", ell((0, -0.045, 0.28), (0.104, 0.105, 0.115)), WHITE, "body")
    k.add("neck", ell((0, -0.025, 0.37), (0.085, 0.08, 0.07)), WHITE, "head")
    k.add("head", ell(H, HR), WHITE, "head")
    k.add("tail", blade([(0, 0.11, 0.2), (0, 0.18, 0.235), (0, 0.235, 0.28)], [0.055, 0.035, 0.006], (0, 0, 1), 0.5),
          WHITE, "tail")
    # a soft grey under the belly, so the white body has a bottom
    k.paint(lambda P: inside(P[:, 2] - 0.15, 0.03) * inside(0.08 - P[:, 2], 0.01), SHADE, on=["body"])

    # --- the wings: the left down and forward to the leek's grip, the right to the shield ---------------------------
    GRIP = np.array([-0.175, -0.105, 0.265])
    k.add("wingL", tube([(-0.095, 0.0, 0.33), (-0.14, -0.04, 0.3), (-0.165, -0.085, 0.272)], [0.055, 0.05, 0.045]),
          WHITE, "armL")
    k.add("handL", ell(GRIP + np.array([0.005, -0.012, 0.0]), (0.05, 0.048, 0.055)), WHITE, "armL")
    HOLD = np.array([0.185, -0.07, 0.265])
    k.add("wingR", tube([(0.095, 0.0, 0.33), (0.145, -0.03, 0.3), (0.175, -0.06, 0.272)], [0.055, 0.05, 0.045]),
          WHITE, "armR")
    k.add("handR", ell(HOLD, (0.048, 0.046, 0.052)), WHITE, "armR")

    # --- the bill: broad and flat, widest at its round tip, a lower bill under it -----------------------------------
    def bill(V):
        V = V.copy()
        t = np.clip(-V[:, 1], -1, 1)
        V[:, 0] *= 0.62 + 0.45 * smoothstep(-0.9, 0.4, t)
        V[:, 2] += 0.5 * V[:, 0] ** 2
        return V
    BC = H + np.array([0, -0.115, -0.03])
    k.add("bill", outward(ell(BC, (0.066, 0.085, 0.022), rot=(12, 0, 0), deform=bill)), BILL, "head", fuse=False,
          surface="shell")
    k.add("lowbill", outward(ell(BC + np.array([0, 0.012, -0.02]), (0.05, 0.07, 0.017), rot=(14, 0, 0), deform=bill)),
          LOWBILL, "head", fuse=False, surface="shell")

    # --- the crest: three thick white feathers rising from the crown and laid back ----------------------------------
    for i, (x, lean, length, r) in enumerate(((-0.026, -0.4, 0.075, 0.022), (0.0, 0.0, 0.095, 0.026),
                                               (0.026, 0.4, 0.075, 0.022))):
        b0 = H + np.array([x * 0.7, 0.01, 0.07])
        d = unit(lean * 0.4, 0.42, 1.0)
        k.add(f"crest{i}", outward(blade([b0, b0 + d * length * 0.55 + np.array([0, 0.012, 0]), b0 + d * length],
                                         [r, r * 0.8, 0.006], (0, 1, 0), 0.6)), WHITE, "head", fuse=False)

    # --- the leek, a lance through the left hand -------------------------------------------------------------------
    AX = unit(0.03, -0.05, 1.0)
    def at(z):
        return GRIP + AX * (z - GRIP[2]) / AX[2]
    R = 0.039
    k.add("bulb", outward(ell(at(0.055), (0.046, 0.046, 0.05))), STALK, "armL", fuse=False, surface="leaf")
    for i, (dx, dy) in enumerate(((0.03, 0.0), (-0.028, 0.01), (0.0, -0.03), (0.01, 0.03))):
        b0 = at(0.03) + np.array([dx * 0.5, dy * 0.5, 0])
        tip = at(0.0) + np.array([dx * 1.4, dy * 1.4, 0.012])
        k.add(f"root{i}", outward(tube([b0, (b0 + tip) / 2 + np.array([0, 0, -0.01]), tip], [0.014, 0.012, 0.009])),
              ROOT, "armL", fuse=False, surface="leaf")
    k.add("stalk", outward(tube([at(0.08), at(0.25), at(0.44)], [R, R, R])), STALK, "armL", fuse=False,
          surface="leaf")
    k.add("pale", outward(tube([at(0.43), at(0.5), at(0.57)], [R + 0.0005, R + 0.0005, R + 0.0005])), PALEGREEN,
          "armL", fuse=False, surface="leaf")
    k.add("blade", outward(tube([at(0.56), at(0.7), at(0.84)], [R + 0.001, R, R - 0.002])), GREEN, "armL",
          fuse=False, surface="leaf")
    # the slanted cut at the top: a wedge rising to a point at the back of the leek
    top = at(0.84)
    pts = []
    for a in np.linspace(0, 2 * math.pi, 14, endpoint=False):
        c, s = math.cos(a), math.sin(a)
        p = top + np.array([c * (R - 0.002), s * (R - 0.002), 0])
        pts.append(p)
        pts.append(p + np.array([0, 0, 0.012 + 0.05 * (1 + s) / 2]))
    from sculptkit import hull
    k.add("cut", hull(pts, merge=0), TIP, "armL", fuse=False, surface="leaf")
    # the leek's own white leaves wrapped round the grip and hanging, a guard over the hand
    for i, ang in enumerate((-110, -70, -30, 10, 60, 160)):
        a = math.radians(ang)
        d = unit(math.cos(a) * 0.5, math.sin(a) * 0.5 - 0.15, -1.0)
        base = at(0.33) + np.array([math.cos(a) * 0.032, math.sin(a) * 0.032, 0])
        k.add(f"guard{i}", outward(leaf(base, d, 0.17, 0.042, thick=0.026)), CREAMWHITE, "armL", fuse=False,
              surface="leaf")

    # --- the shield: dark green leek leaves hanging down in layers, held forward and turned to the front three-quarter
    n = unit(0.45, -1.0, 0.0)                     # the shield's face, toward the viewer
    ez = np.array([0, 0, 1.0])
    ex = np.cross(ez, n); ex /= np.linalg.norm(ex)   # across it
    S0 = HOLD + n * 0.06 + np.array([0.0, 0, 0.0])
    def leaflet(root, tip, w):
        root, tip = np.array(root), np.array(tip)
        d = tip - root; L = np.linalg.norm(d); d /= L
        s = np.array([-d[1], d[0]])
        return [tuple(root + s * w * 0.5), tuple(root + d * L * 0.45 + s * w), tuple(tip),
                tuple(root + d * L * 0.45 - s * w), tuple(root - s * w * 0.5)]
    layers = [
        (SHIELD_DARK, [((0.0, 0.06), (-0.12, -0.12), 0.05), ((0.0, 0.06), (0.13, -0.1), 0.05),
                       ((0.0, 0.0), (-0.07, -0.21), 0.05), ((0.0, 0.0), (0.08, -0.21), 0.05),
                       ((0.0, 0.1), (-0.1, 0.0), 0.04), ((0.0, 0.1), (0.11, 0.01), 0.04)]),
        (SHIELD_MID, [((0.0, 0.04), (-0.1, -0.16), 0.045), ((0.0, 0.04), (0.1, -0.15), 0.045),
                      ((0.0, 0.0), (0.0, -0.23), 0.045), ((0.0, 0.1), (-0.07, 0.04), 0.035),
                      ((0.0, 0.1), (0.08, 0.05), 0.035)]),
        (SHIELD_LIGHT, [((0.0, 0.06), (-0.06, -0.13), 0.03), ((0.0, 0.06), (0.07, -0.12), 0.03),
                        ((0.0, 0.12), (-0.04, 0.06), 0.025), ((0.0, 0.12), (0.045, 0.06), 0.025)]),
        (SPIKE, [((0.0, 0.12), (0.0, -0.16), 0.042)]),          # the flat pale leaf down the middle
    ]
    for li, (col, leaves) in enumerate(layers):
        org = S0 + n * (0.016 * li)
        for j, (r0, t0, w) in enumerate(leaves):
            k.add(f"shield{li}_{j}", slab(leaflet(r0, t0, w), org, ex, ez, 0.022), col, "armR", fuse=False,
                  surface="leaf")
    # the pale green spike up the middle, in front of the leaves
    sp = S0 + n * 0.02 + ex * 0.005
    k.add("spike", outward(blade([sp + ez * 0.09, sp + ez * 0.17, sp + ez * 0.24, sp + ez * 0.29],
                                 [0.022, 0.019, 0.012, 0.004], n, 0.6)), SPIKE, "armR", fuse=False, surface="leaf")

    # --- legs and webbed feet ---------------------------------------------------------------------------------------
    k.add("leg", outward(tube([(0.05, 0.0, 0.15), (0.055, -0.01, 0.08), (0.058, -0.02, 0.028)],
                              [0.019, 0.017, 0.016])), LEGS, "leg{s}", fuse=False, mirror=True, surface="scales")
    def toes(V):
        ph = np.arctan2(V[:, 0], -V[:, 1])
        front = np.clip(-V[:, 1], 0, 1)
        s = 1 + 0.2 * np.cos(3 * ph) * front
        V = V.copy(); V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("foot", outward(ell((0.064, -0.065, 0.016), (0.05, 0.072, 0.016), rot=(0, 0, -10), deform=toes)), FEET,
          "leg{s}", fuse=False, mirror=True, surface="scales")

    # --- thick black brows slanting down to the middle, in every state ---------------------------------------------
    de = unit(0.46, -0.85, 0.2)
    E = H + de / np.linalg.norm(de / HR)
    brow = polygon([(-1, -0.35), (1, 0.05), (1, 0.8), (-1, 0.4)])
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        k.decal((E + np.array([-0.004, -0.006, 0.02])) * m, de * m, (0.038, 0.024), BLACK, eps=0.008,
                outline=(brow if s == 1 else (lambda th, o=brow: o(math.pi - th))), rings=10, segs=72)

    k.build(voxel=0.0055, smooth=7)

    # --- the eyes: a pale half-lidded almond, a dark grey iris toward the middle; the brow cuts its top -------------
    EW, IR, LID, INK = srgb("#E9E9EC"), srgb("#3E3C44"), srgb(WHITE), srgb(BLACK)
    def eye(u, v):
        c = np.tile(EW, (len(u), 1))
        c[v < -0.72 + 0.25 * u * u] = INK                 # the lower lid's dark line
        return c
    def eyes(state):
        for s in (1, -1):
            m = np.array([s, 1, 1.0])
            look = eye_look(state, eye, LID, INK, line=0.16)
            col = look if s == 1 else (lambda u, v, f=look: f(-u, v))
            k.decal((E + np.array([0, -0.002, -0.004])) * m, de * m, (0.03, 0.022), col, eps=0.005, rings=20, segs=56)
            if state in ("Open", "Fierce"):
                # the dark grey iris toward the middle, a patch of its own so its edge is clean
                k.decal((E + np.array([-0.006, -0.004, -0.005])) * m, de * m, (0.015, 0.016), IR, eps=0.0065,
                        rings=8, segs=40)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    sirfetchd(*args())
