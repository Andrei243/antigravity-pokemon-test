# Farfetch'd-Galar sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/farfetchdgalar.py -- "<the export's Farfetch'd-Galar.glb>" <out.glb>
# Kin of farfetchd.py (copied from it: the same plump duck, bill, crest, raised right wing and webbed feet) with
# sirfetchd.py's thick brows over the eyes. From the pictures: a dark grey-brown duck, the head all dark; a pale
# cream-grey breast and belly whose top edge is cut in a zigzag under the throat; a broad yellow bill turned down;
# heavy black brows slanting down to the middle so that only a sliver of white eye shows under them, a small dark
# pupil low in it; a fan of five dark spiky feathers standing on the crown; the right wing raised in a fist at its
# side; orange legs and yellow webbed feet. Its left wing (-X) holds a great leek over its shoulder like a club: the
# thick white stalk rising high in front with a pale tan cut end, a pale green band at the grip, and broad dark green
# leaves trailing back and down behind it, one curling up at its end. The leek is as long as the bird is tall here
# (the pictures draw it some twice as long), held steep so that it fits the game's cell.
# The skeleton: body (0, 0, 0.3), wings (+-0.12, -0.02, 0.38), head (0, -0.02, 0.44), tail (0, 0.12, 0.22),
# legs (+-0.055, 0, 0.15).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, slab, lathe, srgb, inside, smoothstep, unit, eye_look, polygon, outward,
                       tri, args)

BROWN, DARK, CREAM, PALE = "#5E4C46", "#4A3B36", "#CFC9BE", "#7A665E"
BILL, LOWBILL, LEGS, FEET, BLACK = "#F4B82C", "#DE9420", "#E8963A", "#F2B83A", "#1C1818"
STALK, CUT, CUTRIM, PALEGREEN = "#EEEDE6", "#CDA88C", "#B6A496", "#BFDC86"
GREEN, LEAF, LEAFDARK = "#6FB040", "#3F8A34", "#2C6A2A"
H = np.array([0, -0.035, 0.55])           # the head's centre
HR = np.array([0.112, 0.106, 0.108])


def farfetchd_galar(src, out):
    k = Kit(src, coat="fur")

    # --- the body: a plump duck, fuller below and in the breast, leaning a little forward ----------------------------
    def pear(V):
        V = V.copy()
        s = 1 + 0.1 * np.clip(-V[:, 2], 0, 1) - 0.08 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell((0, 0.01, 0.255), (0.145, 0.16, 0.15), rot=(-10, 0, 0), deform=pear), BROWN, "body")
    k.add("breast", ell((0, -0.04, 0.29), (0.13, 0.125, 0.14)), BROWN, "body")
    k.add("neck", ell((0, -0.03, 0.43), (0.08, 0.078, 0.075)), BROWN, "head")
    k.add("head", ell(H, HR), BROWN, "head")
    k.add("tail", blade([(0, 0.11, 0.21), (0, 0.18, 0.24), (0, 0.245, 0.29)], [0.055, 0.035, 0.006], (0, 0, 1), 0.5),
          BROWN, "tail")
    # the right wing's root on the shoulder, under its raised plate
    k.add("shoulder", ell((0.12, 0.01, 0.38), (0.05, 0.06, 0.06)), BROWN, "wingR")

    # The pale breast and belly round the body's upright axis, its top cut in a zigzag under the throat
    def belly(P):
        th = np.arctan2(P[:, 0], -(P[:, 1] - 0.03))
        top = 0.42 - 0.03 * (1 + tri(th * 3.2 / math.pi)) / 2 - 0.06 * smoothstep(0.5, 1.0, np.abs(th))
        return inside(np.abs(th) - 0.95, 0.004) * inside(P[:, 2] - top, 0.003) * inside(0.07 - P[:, 2], 0.004)
    k.paint(belly, CREAM, on=["body", "breast", "neck"])

    # --- the bill: broad and flat, widest at its round tip, turned down; a lower bill under it ------------------------
    def bill(V):
        V = V.copy()
        t = np.clip(-V[:, 1], -1, 1)
        V[:, 0] *= 0.62 + 0.45 * smoothstep(-0.9, 0.4, t)
        V[:, 2] += 0.5 * V[:, 0] ** 2
        return V
    k.add("bill", outward(ell((0, -0.205, 0.51), (0.066, 0.095, 0.024), rot=(20, 0, 0), deform=bill)), BILL, "head",
          fuse=False, surface="shell")
    k.add("lowbill", outward(ell((0, -0.19, 0.49), (0.05, 0.078, 0.017), rot=(22, 0, 0), deform=bill)), LOWBILL,
          "head", fuse=False, surface="shell")

    # --- the crown's fan: five thick spiky feathers standing up, splayed ---------------------------------------------
    for i, (x, lean, length, r) in enumerate(((-0.05, -0.75, 0.07, 0.019), (-0.025, -0.35, 0.085, 0.021),
                                              (0.0, 0.0, 0.095, 0.023), (0.025, 0.35, 0.085, 0.021),
                                              (0.05, 0.75, 0.07, 0.019))):
        b0 = np.array([x * 0.6, -0.005, 0.635])
        d = unit(lean * 0.55, 0.25, 1.0)
        k.add(f"crest{i}", outward(blade([b0, b0 + d * length * 0.6 + np.array([0, 0.008, 0]), b0 + d * length],
                                         [r, r * 0.85, 0.005], (0, 1, 0), 0.62)), DARK, "head", fuse=False)

    # --- the raised right wing, a fist of feathers at its side --------------------------------------------------------
    d = unit(0.22, 0.75, 0.62)
    n = unit(1.0, -0.25, -0.1)
    ez = np.cross(n, d); ez /= np.linalg.norm(ez)
    ORG = np.array([0.13, -0.01, 0.36])
    WING = [(0.0, -0.04), (0.09, -0.075), (0.17, -0.085), (0.215, -0.07), (0.205, -0.045), (0.235, -0.035),
            (0.22, -0.01), (0.245, 0.005), (0.22, 0.025), (0.235, 0.045), (0.2, 0.06), (0.14, 0.065), (0.06, 0.055),
            (0.0, 0.04)]
    k.add("wing", slab(WING, ORG, d, ez, 0.034), BROWN, "wingR", fuse=False)
    TIPS = [(0.15, -0.09), (0.17, -0.09), (0.215, -0.072), (0.205, -0.045), (0.235, -0.035), (0.22, -0.01),
            (0.245, 0.005), (0.22, 0.025), (0.235, 0.045), (0.2, 0.062), (0.14, 0.07), (0.13, 0.07)]
    k.add("wingtip", slab(TIPS, ORG, d, ez, 0.04), DARK, "wingR", fuse=False)

    # --- the leek over the left shoulder, a club: high in front, its leaves trailing back and down -------------------
    TOP = np.array([-0.205, -0.21, 0.71])
    AX = unit(0.12, 0.62, -0.78)                    # down the leek from its cut end
    def at(t):
        return TOP + AX * t
    R = 0.05
    k.add("stalk", lathe(TOP, AX, [(0, 0.0), (R * 0.9, 0.0), (R, 0.012), (R * 1.03, 0.14), (R, 0.3), (R * 0.95, 0.34),
                                   (0, 0.34)], seg=40), STALK, "wingL", fuse=False, surface="leaf")
    k.add("cut", lathe(TOP, AX, [(0, -0.004), (R * 0.84, -0.004), (R * 0.84, 0.003), (0, 0.003)], seg=40), CUT,
          "wingL", fuse=False, surface="leaf")
    k.add("cutrim", lathe(TOP, AX, [(R * 0.82, -0.002), (R * 0.93, -0.002), (R * 0.95, 0.006), (R * 0.82, 0.006)],
                          seg=40), CUTRIM, "wingL", fuse=False, surface="leaf")
    k.add("palegreen", lathe(TOP, AX, [(0, 0.33), (R * 0.97, 0.33), (R * 0.92, 0.39), (R * 0.84, 0.44), (0, 0.44)],
                             seg=40), PALEGREEN, "wingL", fuse=False, surface="leaf")
    # broad leaves from the green band, thick solid blades: one straight back and down, one out and curling up at
    # its end, one under them
    side = np.cross(AX, (0, 0, 1.0)); side /= np.linalg.norm(side)
    up = np.cross(side, AX)
    def leafy(name, t0, way, length, r0, col, curl=0.0, flat=0.36):
        b = at(t0)
        w = unit(*way)
        pts = [b, b + w * length * 0.35, b + w * length * 0.7 + up * curl * 0.4, b + w * length + up * curl]
        k.add(name, outward(blade(pts, [r0, r0 * 1.05, r0 * 0.75, r0 * 0.12], unit(1, 0, 0.35), flat)), col,
              "wingL", fuse=False, surface="leaf")
    leafy("leafa", 0.4, AX + np.array([-0.3, 0.05, 0.08]), 0.42, 0.058, LEAF, curl=0.0)
    leafy("leafb", 0.41, AX + np.array([-0.55, 0.1, 0.2]), 0.38, 0.052, GREEN, curl=0.1)
    leafy("leafc", 0.39, AX + np.array([-0.15, -0.05, -0.1]), 0.34, 0.05, LEAFDARK, curl=-0.02)

    # the left wing up over the shoulder to the grip, a fist of feathers round the stalk
    GRIP = at(0.22)
    k.add("arm", tube([(-0.12, -0.0, 0.39), (-0.155, -0.03, 0.44), GRIP + np.array([0.012, 0.025, -0.02])],
                      [0.055, 0.05, 0.046]), BROWN, "wingL")
    k.add("hand", ell(GRIP + np.array([0.01, 0.01, -0.005]), (0.055, 0.05, 0.058)), BROWN, "wingL")
    for i, t in enumerate((0.16, 0.2, 0.24)):
        c = at(t)
        pts = [c + side * 0.052, c - up * 0.052 + side * 0.03, c - up * 0.054 - side * 0.012]
        k.add(f"finger{i}", outward(tube(pts, [0.019, 0.018, 0.015])), BROWN, "wingL", fuse=False)

    # --- legs and webbed feet ---------------------------------------------------------------------------------------
    k.add("leg", outward(tube([(0.055, 0.0, 0.15), (0.06, -0.01, 0.08), (0.062, -0.02, 0.025)], [0.022, 0.019, 0.018])),
          LEGS, "leg{s}", fuse=False, mirror=True, surface="scales")
    def toes(V):
        ph = np.arctan2(V[:, 0], -V[:, 1])
        front = np.clip(-V[:, 1], 0, 1)
        s = 1 + 0.2 * np.cos(3 * ph) * front
        V = V.copy(); V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("foot", outward(ell((0.066, -0.06, 0.016), (0.05, 0.074, 0.016), rot=(0, 0, -8), deform=toes)), FEET,
          "leg{s}", fuse=False, mirror=True, surface="scales")

    # --- thick black brows slanting down to the middle, in every state (sirfetchd.py's) ------------------------------
    de = unit(0.46, -0.85, 0.2)
    E = H + de / np.linalg.norm(de / HR)
    brow = polygon([(-1, -0.45), (1, -0.05), (1, 0.85), (-1, 0.45)])
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        k.decal((E + np.array([-0.006, -0.007, 0.014])) * m, de * m, (0.046, 0.028), BLACK, eps=0.008,
                outline=(brow if s == 1 else (lambda th, o=brow: o(math.pi - th))), rings=10, segs=72)

    k.build(voxel=0.0062, smooth=7)

    # --- the eyes: a sliver of white under the brow, a small dark pupil low toward the middle -------------------------
    EW, PU, LID, INK = srgb("#F2F0EA"), srgb("#1E1A1A"), srgb(BROWN), srgb(BLACK)
    def eye(u, v):
        c = np.tile(EW, (len(u), 1))
        c[v < -0.72 + 0.25 * u * u] = INK
        return c
    def eyes(state):
        for s in (1, -1):
            m = np.array([s, 1, 1.0])
            look = eye_look(state, eye, LID, INK, line=0.18)
            col = look if s == 1 else (lambda u, v, f=look: f(-u, v))
            k.decal((E + np.array([0, -0.002, -0.008])) * m, de * m, (0.029, 0.017), col, eps=0.005, rings=20, segs=56)
            if state in ("Open", "Fierce"):
                k.decal((E + np.array([-0.007, -0.004, -0.01])) * m, de * m, (0.012, 0.013), PU, eps=0.0065,
                        rings=8, segs=40)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    farfetchd_galar(*args())
