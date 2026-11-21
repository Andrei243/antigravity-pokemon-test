# Haxorus sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/haxorus.py -- <the export's Haxorus.glb> <out.glb>
# Fraxure grown up (built on axew.py's and fraxure.py's frame through sibling): from the references a tall dragon in
# yellow-gold armour over black. A long helmeted head carried forward on a short neck, its lower face and snout
# black, a red eye in a black band under the helmet's edge, a point at the helmet's back. Out of each side of the
# mouth's corners a great tusk grows out and forward into an axe's crescent, its horns rising over the helmet and
# dropping under the chin, red with black at its root, the two a mirror pair round the face like brackets. Plates of gold on the shoulders and chest, a ridge of plates down the back, the
# front black from the chest to the crotch, narrow at the waist. Gold arms with bands, carried forward and down,
# red claws. Great gold haunches fused into the hips over short black shins and black feet with red claws. A thick
# gold tail with dark bands rising behind, its end black.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, polygon, eye_look, sibling, args

ax = sibling("axew")
fx = sibling("fraxure")
spike, on_ell, eye_pair = ax.spike, ax.on_ell, ax.eye_pair

GOLD = "#CDB838"
BLACK = "#3A3836"
RED = fx.RED
TUSKRED = "#C8303A"
INK = ax.INK


def crescent(k, name, root, s, color_root, color_blade, bone, a=0.075, b=0.19, stub=0.05):
    """An axe-blade tusk held in the mouth like a double-bitted axe head: a black haft from root (the mouth's corner)
    out and forward to the middle of a crescent whose convex edge faces out and forward, its upper horn rising to a
    tip over the helmet, its lower horn dropping to a tip under the chin, both tips curling back toward the face.
    The crescent lies in a plane turned out from straight ahead (a mirror pair by s), so neither camera sees it
    edge-on; it is broad across the arc and thick across the plane."""
    root = np.array(root, float)
    z = np.array([0, 0, 1.0])
    h = unit(0.85 * s, -0.53, 0.0)                 # out and forward
    n = np.cross(h, z)                             # across the plane
    out = np.array([s, 0, 0.0])
    M = root + h * stub + z * 0.02                 # the crescent's middle, where the haft meets it
    C0 = M - h * a
    def at(phi):
        return C0 + a * math.cos(phi) * h + b * math.sin(phi) * z + out * 0.03 * max(math.sin(phi), 0)
    def normal(phi):
        return unit(*(math.cos(phi) / a * h + math.sin(phi) / b * z))
    def width(phi):
        return 0.085 * (1 - (abs(phi) / math.radians(112)) ** 2) ** 0.55 + 0.006
    # the haft, black, from inside the jaw to the crescent's inner edge
    k.add(f"{name}haft", hull([root - h * 0.012 + n * 0.018, root - h * 0.012 - n * 0.018,
                               root + z * 0.03, root - z * 0.03,
                               M + n * 0.016, M - n * 0.016, M + z * 0.045, M - z * 0.045]),
          color_root, bone, fuse=False, surface="shell")
    phis = np.radians(np.linspace(-72, 108, 16))
    for i in range(len(phis) - 1):
        P = []
        for phi in (phis[i], phis[i + 1]):
            c, r, w = at(phi), normal(phi), width(phi)
            th = 0.034 * (0.45 + 0.55 * w / 0.091)
            # the blade stands out of the arc: its inner edge on it, its outer edge out from it
            P += [c - r * 0.006, c + r * w * 0.95, c + r * w * 0.45 + n * th / 2, c + r * w * 0.45 - n * th / 2]
        col = color_root if abs(phis[i] + phis[i + 1]) / 2 < math.radians(12) else color_blade
        k.add(f"{name}{i}", hull(P), col, bone, fuse=False, surface="shell")


def haxorus(src, out):
    k = Kit(src, coat="shell")

    # The trunk: a deep chest of gold plates over a narrow black waist, broad hips
    k.add("chest", ell((0, -0.02, 0.515), (0.11, 0.095, 0.095), rot=(-10, 0, 0)), GOLD, "body")
    k.add("waist", ell((0, 0.0, 0.42), (0.078, 0.075, 0.09)), GOLD, "body")
    k.add("hips", ell((0, 0.01, 0.33), (0.1, 0.09, 0.075)), GOLD, "body")
    k.add("neck", tube([(0, -0.01, 0.55), (0, -0.045, 0.6), (0, -0.07, 0.64)], [0.058, 0.05, 0.048]), GOLD, "head")
    # the black front: from between the chest plates down to the crotch, narrow at the waist
    def front(P):
        a = np.abs(np.arctan2(P[:, 0], -(P[:, 1] + 0.01)))
        reach = np.interp(P[:, 2], [0.27, 0.32, 0.42, 0.5, 0.56, 0.62], [0.9, 0.75, 0.42, 0.6, 0.3, 0.2])
        return inside(a - reach, 0.004) * inside(P[:, 2] - 0.6, 0.004) * inside(0.27 - P[:, 2], 0.004)
    k.paint(front, BLACK, on=["chest", "waist", "hips", "neck"])
    # a dark line across the chest's plates
    k.paint(lambda P: inside(np.abs(P[:, 2] - (0.475 + 0.25 * np.abs(P[:, 0]))) - 0.004, 0.002)
            * inside(-0.03 - P[:, 1], 0.003) * (np.abs(P[:, 0]) > 0.04), BLACK, on=["chest"])

    # Plates on the shoulders and a ridge down the back
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"shoulder{side}", ell(np.array((0.105, 0.0, 0.545)) * m, (0.06, 0.068, 0.055)), GOLD, f"arm{side}")
        k.add(f"plate{side}", hull(np.array([(0.075, 0.03, 0.59), (0.14, 0.02, 0.585), (0.16, 0.03, 0.54),
                                            (0.11, 0.07, 0.6), (0.09, -0.04, 0.585), (0.13, 0.06, 0.55),
                                            (0.1, 0.04, 0.635)]) * m), GOLD, f"arm{side}", fuse=False)
    for j, (y, z, l) in enumerate(((0.07, 0.6, 0.07), (0.1, 0.52, 0.06), (0.105, 0.44, 0.045))):
        k.add(f"ridge{j}", spike((0, y - 0.01, z), unit(0, 0.75, 0.75), l, 0.06, up=(1, 0, 0), flat=0.3), GOLD, "body",
              fuse=False)

    # The arms: gold, forward and down, a dark band at the elbow, red claws
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"arm{side}", tube(np.array([(0.115, -0.01, 0.53), (0.155, -0.05, 0.46), (0.16, -0.12, 0.41),
                                          (0.158, -0.155, 0.395)]) * m, [0.04, 0.034, 0.03, 0.027]), GOLD, f"arm{side}")
        k.add(f"hand{side}", ell(np.array((0.158, -0.165, 0.39)) * m, (0.03, 0.03, 0.026)), GOLD, f"arm{side}")
        for j, (dx, dz) in enumerate(((-0.018, 0.004), (0.002, -0.006), (0.02, 0.004))):
            b = np.array((0.158 + dx, -0.183, 0.385 + dz))
            k.add(f"claw{side}{j}", spike(b * m, unit(dx * 8 * s, -0.6, -1), 0.045, 0.024, up=(0, 1, 0), flat=0.5),
                  TUSKRED, f"arm{side}", fuse=False)
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.462) - 0.006, 0.002) * (np.abs(P[:, 0]) > 0.125) * (P[:, 1] < -0.02),
            BLACK, on=["arm"])

    # The legs: great gold haunches fused into the hips down to just above the ankle, short black shins, black feet
    # with red claws
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.1, -0.015, 0.25)) * m, (0.085, 0.115, 0.125), rot=(-12, 0, 0)), GOLD,
              f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.115, 0.04, 0.15), (0.12, 0.05, 0.09), (0.122, 0.035, 0.04)]) * m,
                                  [0.05, 0.042, 0.04]), BLACK, f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.122, -0.02, 0.026)) * m, (0.05, 0.085, 0.027)), BLACK, f"leg{side}")
        for j, dx in enumerate((-0.03, 0.0, 0.03)):
            b = np.array((0.122 + dx, -0.1, 0.024))
            k.add(f"toe{side}{j}", spike(b * m, unit(dx * 5 * s, -1, -0.2), 0.05, 0.03, up=(0, 0, 1), flat=0.5),
                  TUSKRED, f"leg{side}", fuse=False)
        b = np.array((0.122, 0.055, 0.03))
        k.add(f"heel{side}", spike(b * m, unit(0, 1, -0.35), 0.04, 0.026, up=(0, 0, 1), flat=0.5), TUSKRED,
              f"leg{side}", fuse=False)
    # the haunch's lower edge black where the shin runs up into it
    k.paint(lambda P: inside(P[:, 2] - 0.155, 0.004), BLACK, on=["thigh"])

    # The tail: thick, out back and rising, dark bands across its top, its end black
    tp = [(0, 0.06, 0.32), (0, 0.18, 0.25), (0, 0.3, 0.28), (0, 0.38, 0.38), (0, 0.42, 0.5), (0, 0.435, 0.57)]
    k.add("tail", tube(tp, [0.075, 0.06, 0.045, 0.032, 0.018, 0.006]), GOLD, "tail")
    k.paint(lambda P: inside(0.42 - P[:, 2], 0.004) * (P[:, 1] > 0.3), BLACK, on=["tail"])
    k.paint(lambda P: sum(inside(np.abs(P[:, 1] - y) - 0.005, 0.002) for y in (0.17, 0.24, 0.31))
            * inside(P[:, 2] - 0.42, 0.003), BLACK, on=["tail"])

    # The head: a long gold helmet carried forward, the snout and the lower face black, a point at the back
    # Laid out at the size first measured, then the whole head grown about its joint by G (the face must read)
    J, G = np.array([0.0, -0.05, 0.62]), 1.45
    def hp(p):
        return J + (np.array(p, float) - J) * G
    first = len(k.parts)
    H, HR = np.array([0.0, -0.085, 0.69]), np.array([0.066, 0.092, 0.068])
    k.add("head", ell(H, HR, rot=(-12, 0, 0)), GOLD, "head")
    k.add("snout", tube([(0, -0.12, 0.665), (0, -0.175, 0.64), (0, -0.205, 0.61)], [0.04, 0.03, 0.014]), BLACK, "head")
    k.add("jaw", ell((0, -0.105, 0.632), (0.04, 0.06, 0.024), rot=(-15, 0, 0)), BLACK, "head")
    k.add("helmpoint", tube([(0, -0.02, 0.72), (0, 0.03, 0.745), (0, 0.065, 0.76)], [0.04, 0.022, 0.006]), GOLD, "head")
    k.parts[-1]["V"][:, 0] *= 0.6
    for p in k.parts[first:]:
        p["V"] = J + (p["V"] - J) * G
    # the black of the face: under a line from the snout's top back to the helmet's lower back
    def face(P):
        q = (J + (P - J) / G) - H
        line = -0.016 - 0.32 * q[:, 1]
        return inside(q[:, 2] - line, 0.003) * inside(q[:, 1] - 0.06, 0.004)
    k.paint(face, BLACK, on=["head", "snout", "jaw", "neck"])

    # The tusks: an axe-blade crescent held at each corner of the mouth, beside and in front of the snout
    for s, side in ((1, "R"), (-1, "L")):
        crescent(k, f"tusk{side}", hp((0.03 * s, -0.17, 0.632)), s, BLACK, TUSKRED, "head")

    # The eyes: red, bold, in a black band under the helmet's edge
    RED_E = srgb("#E8342C")
    def eye(u, v):
        c = np.tile(RED_E, (len(u), 1))
        c[((u + 0.08) / 0.3) ** 2 + (v / 0.72) ** 2 < 1] = INK
        c[(u - 0.3) ** 2 + (v - 0.35) ** 2 < 0.035] = np.array([1.0, 1.0, 1.0])
        return c
    almond = polygon([(-1, -0.2), (-0.55, -0.85), (0.4, -0.95), (1, -0.3), (0.85, 0.5), (0.1, 0.95), (-0.7, 0.55)])
    de = unit(0.62, -0.74, 0.22)
    E = hp(on_ell(H, HR, de) + np.array([0.0, 0.0, -0.004]))
    k.decal(E, de, (0.045, 0.03), BLACK, eps=0.003, rings=8, segs=40, mirror=True)
    eyes = eye_pair(k, E, de, (0.03, 0.022), eye, srgb(BLACK), outline=almond, eps=0.0055, ink=srgb("#9A9AA2"))

    k.build(voxel=0.0065, smooth=4, band=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    haxorus(*args())
