# Kecleon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/kecleon.py -- <the export's Kecleon.glb> <out.glb>
#
# From the references: a light green chameleon standing upright. A big casqued head with a blunt snout whose long
# mouth is a pair of yellow lips; a great round eye on a turret on each side, ringed in yellow, a black upright pupil
# with a glint; a yellow stripe up each side of the casque from the snout to the crest; a jagged crest over the crown,
# green at its front and yellow behind. A pear-shaped body, the belly widest, a red-pink zigzag band round it; small
# green spikes on the upper back; arms held forward with three cream claws; short thick legs with cream claws and a
# yellow two-lobed mark on each thigh; a tail coiled in a flat spiral behind, turned some 35 degrees out to +X.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, polygon, tri, eye_look, args

GREEN, YELLOW, RED, CLAW, LIPLINE = "#A0D872", "#F1D27A", "#CF4C78", "#F4DE98", "#A88A3E"
INK, WHITE = srgb("#141A12"), np.array([1.0, 1.0, 1.0])

H = np.array([0.0, -0.015, 0.545])               # the head (the casque) and its radii
HR = np.array([0.124, 0.138, 0.138])


def claw(base, d, length, width, thick, up=(0, 0, 1)):
    d = unit(*d)
    a = np.cross(d, np.array(up, float)); a /= np.linalg.norm(a)
    b = np.cross(a, d)
    base = np.array(base, float)
    mid = base + d * length * 0.45
    pts = [base + a * width / 2, base - a * width / 2, base + b * thick / 2, base - b * thick / 2 - d * 0.004,
           mid + a * width * 0.28, mid - a * width * 0.28, mid + b * thick * 0.28, mid - b * thick * 0.28,
           base + d * length]
    return hull(pts)


def spike(base, d, length, along, width, thick):
    """A crest's point: a flat-faced solid from a root `width` long along `along` and `thick` across to its tip."""
    d, a = unit(*d), unit(*along)
    t = unit(*np.cross(a, d))
    base = np.array(base, float)
    pts = [base + a * width / 2, base - a * width / 2, base + t * thick / 2, base - t * thick / 2,
           base + d * length * 0.5 + a * width * 0.12 + t * thick * 0.2,
           base + d * length * 0.5 + a * width * 0.12 - t * thick * 0.2, base + d * length]
    return hull(pts)


def kecleon(src, out):
    k = Kit(src, coat="scales")

    # --- the head: a casque, a blunt snout forward and down, the eye turrets on its sides --------------------------
    k.add("head", ell(H, HR), GREEN, "head")
    k.add("back", ell(H + (0, 0.04, 0.03), (0.1, 0.11, 0.12)), GREEN, "head")
    k.add("snout", ell((0, -0.12, 0.49), (0.08, 0.085, 0.06)), GREEN, "head")
    k.add("chin", ell((0, -0.06, 0.455), (0.09, 0.08, 0.045)), GREEN, "head")
    ET = {}
    for s, side in ((1, "R"), (-1, "L")):
        c = np.array([s * 0.094, -0.045, 0.555])
        k.add(f"turret{side}", ell(c, (0.058, 0.058, 0.058)), GREEN, "head")
        ET[s] = c

    # the lips: a broad yellow band round the snout's front, the mouth a dark line along its middle
    def lips(P, half=0.019):
        q = P - np.array([0, -0.06, 0.0])
        a = np.arctan2(q[:, 0], -q[:, 1])
        line = 0.478 - 0.02 * a * a
        return inside(np.abs(P[:, 2] - line) - half, 0.003) * inside(np.abs(a) - 1.15, 0.03) * (P[:, 1] < -0.04)
    k.paint(lambda P: lips(P), YELLOW, on=["snout", "chin", "head"])
    k.paint(lambda P: lips(P, 0.0028), LIPLINE, on=["snout", "chin", "head"])

    # the stripe up each side of the casque, from the snout's top to the crest, in front of the eye
    def stripe(P):
        out = np.zeros(len(P))
        for s in (1, -1):
            a0 = np.array([s * 0.045, -0.15, 0.53])
            a1 = np.array([s * 0.035, -0.02, 0.675])
            d = a1 - a0; L = np.linalg.norm(d); d = d / L
            t = np.clip((P - a0) @ d, 0, L)
            near = a0 + np.outer(t, d)
            dist = np.linalg.norm((P - near)[:, 1:], axis=1)
            out = np.maximum(out, inside(dist - 0.011, 0.003) * (s * P[:, 0] > 0.008) * (P[:, 2] > 0.505))
        return out
    k.paint(stripe, YELLOW, on=["head", "snout", "back"])

    # the crest over the crown: green points at its front, yellow behind, laid back along the head
    CREST = [(-0.6, 0.06, GREEN), (-0.38, 0.075, GREEN), (-0.15, 0.1, YELLOW), (0.08, 0.11, YELLOW),
             (0.3, 0.105, YELLOW), (0.52, 0.09, YELLOW), (0.74, 0.075, YELLOW), (0.96, 0.06, YELLOW)]
    for j, (a, L, col) in enumerate(CREST):
        th = a                                   # angle from straight up toward the back (+Y), radians
        d = np.array([0, math.sin(th), math.cos(th)])
        base = H + (0, 0.02, 0.0) + d * np.array([1, 0.138, 0.138]) * 0.9
        tip_d = unit(0, math.sin(th + 0.55), math.cos(th + 0.55))
        along = unit(0, math.cos(th), -math.sin(th))
        k.add(f"crest{j}", spike(base, tip_d, L, along, 0.075, 0.042), col, "head", fuse=False,
              surface=None)

    # --- the body: a pear, the belly widest, the red zigzag band round it ---------------------------------------
    k.add("neck", ell((0, -0.005, 0.43), (0.072, 0.07, 0.06)), GREEN, "body")
    k.add("torso", ell((0, 0.0, 0.36), (0.085, 0.08, 0.085)), GREEN, "body")
    k.add("belly", ell((0, 0.005, 0.27), (0.112, 0.105, 0.105)), GREEN, "body")
    k.add("hips", ell((0, 0.01, 0.2), (0.1, 0.09, 0.07)), GREEN, "body")

    def trunk(P):
        return inside(np.hypot(P[:, 0] / 0.13, P[:, 1] / 0.125) - 1, 0.004) * (np.abs(P[:, 0]) < 0.125)
    def band(P):
        a = np.arctan2(P[:, 0], -P[:, 1])
        top = 0.335 + 0.018 * tri(a * 7 / (2 * math.pi))
        bot = 0.262 + 0.016 * tri(a * 7 / (2 * math.pi) + 0.5)
        return inside(P[:, 2] - top, 0.003) * inside(bot - P[:, 2], 0.003) * trunk(P)
    k.paint(band, RED, on=["belly", "torso"])
    # small green spikes on the upper back
    for j, (x, z, L) in enumerate([(0.0, 0.42, 0.05), (0.03, 0.39, 0.042), (-0.03, 0.385, 0.04)]):
        b = np.array([x, 0.07, z])
        k.add(f"backspike{j}", spike(b, unit(x * 4, 1, 0.5), L, (0, -0.5, 1), 0.04, 0.03), GREEN, "body", fuse=False)

    # --- the arms: forward and a little down, three cream claws ---------------------------------------------------
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"arm{side}", tube(np.array([(0.065, -0.02, 0.38), (0.11, -0.07, 0.345), (0.115, -0.15, 0.32)]) * m,
                                 [0.032, 0.028, 0.026]), GREEN, f"arm{side}")
        HC = np.array([0.115, -0.17, 0.315])
        k.add(f"hand{side}", ell(HC * m, (0.03, 0.028, 0.022)), GREEN, f"arm{side}")
        for j, d in enumerate([(0.45, -1, 0.1), (0.0, -1, -0.1), (-0.45, -1, -0.05)]):
            d = np.array(d)
            b = HC + unit(*d) * 0.02
            k.add(f"finger{side}{j}", claw(b * m, d * m, 0.03, 0.03, 0.022), CLAW, f"arm{side}", fuse=False,
                  surface="shell")

    # --- the legs: thick thighs fused into the hips, short shins, broad feet with cream claws -----------------------
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array([0.08, 0.0, 0.17]) * m, (0.065, 0.075, 0.075)), GREEN, f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.085, -0.005, 0.12), (0.085, -0.015, 0.05)]) * m, [0.045, 0.042]), GREEN,
              f"leg{side}")
        F = np.array([0.085, -0.04, 0.025])
        k.add(f"foot{side}", ell(F * m, (0.045, 0.065, 0.026)), GREEN, f"leg{side}")
        for j, dx in enumerate((-0.025, 0.0, 0.025)):
            b = np.array([0.085 + dx, -0.095, 0.02])
            k.add(f"toe{side}{j}", claw(b * m, np.array([dx * 1.5, -1, -0.25]) * m, 0.03, 0.03, 0.022), CLAW,
                  f"leg{side}", fuse=False, surface="shell")
    # the yellow mark on each thigh's outer side: a point below, two lobes above with a notch between
    MARK = [(0.0, -1.0), (0.55, -0.35), (0.85, 0.35), (0.62, 0.85), (0.28, 0.55), (0.0, 0.62), (-0.28, 0.55),
            (-0.62, 0.85), (-0.85, 0.35), (-0.55, -0.35)]
    k.decal((0.14, 0.02, 0.19), (1, 0.1, 0), (0.037, 0.043), YELLOW, eps=0.004, outline=polygon(MARK), rings=12,
            segs=56, mirror=True)

    # --- the tail: back along the ground and coiled in a flat spiral, its face turned out to +X ------------------
    D = unit(math.sin(math.radians(35)), math.cos(math.radians(35)), 0)
    Z = np.array([0, 0, 1.0])
    C = np.array([0, 0.07, 0.22]) + D * 0.2 + Z * 0.075
    pts, rad = [np.array([0, 0.07, 0.22]), np.array([0.025, 0.13, 0.18]) ], [0.06, 0.05]
    turns = 1.45
    for i in range(1, 60):
        f = i / 59
        th = -math.pi / 2 + 2 * math.pi * turns * f
        r = 0.115 * (1 - f) + 0.012 * f
        pts.append(C + r * (math.cos(th) * D + math.sin(th) * Z))
        rad.append(0.04 * (1 - f) + 0.022 * f)
    pts = [pts[0], pts[1]] + pts[3:]
    rad = [rad[0], rad[1]] + rad[3:]
    k.add("tail", tube(pts, rad, seg=20, per=4), GREEN, "tail")
    k.add("coil", ell(C + D * 0.004, (0.034, 0.034, 0.034)), GREEN, "tail")    # the coil's heart closed

    # --- the eyes: a great round eye on each turret, ringed in yellow, a black upright pupil, a glint -------------
    CG, CY = srgb("#B4E486"), srgb(YELLOW)
    def eye(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(CG, (len(u), 1))
        c[r > 0.68] = CY
        c[((u + 0.05) / 0.24) ** 2 + (v / 0.44) ** 2 < 1] = INK
        return c
    de = unit(0.66, -0.74, 0.12)
    E = ET[1] + de * 0.058
    half = (0.056, 0.056)
    right = np.cross((0, 0, 1.0), de); right /= np.linalg.norm(right)
    upv = np.cross(de, right)

    def ringed(f):
        def g(u, v):
            c = f(u, v)
            c[np.sqrt(u * u + v * v) > 0.68] = CY
            return c
        return g

    def eyes(state):
        k.decal(E, de, half, ringed(eye_look(state, eye, CG, INK, line=0.14)), eps=0.006, rings=20, segs=56, mirror=True)
        if state == "Open":
            g = E + right * (-0.1 * half[0]) + upv * (0.2 * half[1])
            k.decal(g, de, (0.008, 0.009), "#FFFFFF", eps=0.011, rings=5, segs=20, mirror=True)

    k.build(voxel=0.0065, smooth=4, band=0.008)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    kecleon(*args())
