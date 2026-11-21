# Iron Jugulis sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ironjugulis.py -- <the export's Iron Jugulis.glb> <out.glb>
# Its worked example is Hydreigon (hydreigon.py), whose shapes it does not share: built anew, with deino.py's frame for
# a head (imported). From the references: a robot Hydreigon that hovers. Three heads like steel bird skulls, blue, each
# with a hooked upper beak over a short lower one and a dark gap between: the main head on a black jointed neck, a
# dark socket with a magenta eye on each side, framed behind by a great fan of magenta spikes (the design's largest
# mass); an arm head at the end of each black jointed arm, eyeless, a ring of smaller magenta spikes round its wrist. A
# black mantle over the shoulders, ragged in points below; a dark ribbed abdomen with a magenta stripe down each side
# of its front, blue at its foot; two blue legs hanging with black claws; six long black jointed rods of wings, three a
# side, raised and swept back. (The task's "wings like plates" are rods in the pictures, built thick here; the bands
# are two magenta stripes, not rings.)
# Reference ratios (3D picture): the crown's span / height 0.4; the main head / height 0.17 (built a third larger);
# the abdomen's width / height 0.26; a wing rod's reach / height 0.45.
# Bones: body (0, 0, 0.49); head (0, -0.03, 0.61); arms (+-0.09, -0.02, 0.54); wings (+-0.05, 0.07, 0.58);
# tail (0, 0.03, 0.39); legs (+-0.045, -0.02, 0.41).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, srgb, inside, unit, eye_look, outward, args, sibling)

dn = sibling("deino")

BLUE, BLUE_L, DARK, BLACK = "#4F7FC4", "#6E9AD6", "#34363F", "#1E1E25"
MAGENTA, RIB = "#E2369C", "#24252C"
SOCKET, PUPIL = srgb("#241C2A"), srgb("#F0306E")

SKULL_C, SKULL_R = (0.0, 0.08, 0.12), (0.8, 0.95, 0.78)


def plate_spike(base, d, length, w, t, n):
    """A flat pointed plate from base along d, w wide, t thick across n (a hull)."""
    base = np.array(base, float); d = unit(*d)
    n = np.array(n, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    s = np.cross(d, n)
    m = base + d * length * 0.45
    return hull([base + s * w / 2 + n * t / 2, base + s * w / 2 - n * t / 2, base - s * w / 2 + n * t / 2,
                 base - s * w / 2 - n * t / 2, m + s * w * 0.36 + n * t * 0.4, m + s * w * 0.36 - n * t * 0.4,
                 m - s * w * 0.36 + n * t * 0.4, m - s * w * 0.36 - n * t * 0.4, base + d * length])


def rod(a, b, w, t):
    """A black flat-faced rod from a to b (a hull)."""
    a, b = np.array(a, float), np.array(b, float)
    d = b - a; d /= np.linalg.norm(d)
    u = np.cross(d, (0, -1.0, 0.2)); u /= np.linalg.norm(u)
    v = np.cross(u, d)
    return hull([c + u * x + v * y for c in (a, b) for x in (-w / 2, w / 2) for y in (-t / 2, t / 2)])


class Head:
    """A steel bird skull at c looking along look, r its size: a hooked upper beak, a short lower one, a dark gap."""

    def __init__(self, k, tag, bone, c, look, r):
        self.k, self.r, self.c = k, r, np.array(c, float)
        self.M = dn.frame(look)
        M, C = self.M, self.c
        put = lambda shape: ((shape[0] * r) @ M.T + C, shape[1])
        self.at = lambda p: C + M @ (np.array(p, float) * r)
        k.add(f"skull{tag}", put(ell(SKULL_C, SKULL_R)), BLUE, bone)
        k.add(f"upper{tag}", put(blade([(0, -0.45, 0.2), (0, -1.05, 0.05), (0, -1.38, -0.3), (0, -1.3, -0.6)],
                                       [0.46, 0.34, 0.2, 0.1], (1, 0, 0), 0.8)), BLUE, bone)
        k.add(f"lower{tag}", put(blade([(0, -0.35, -0.42), (0, -0.82, -0.6), (0, -1.05, -0.66)],
                                       [0.36, 0.22, 0.12], (1, 0, 0), 0.8)), BLUE, bone)
        k.add(f"gap{tag}", outward(put(ell((0, -0.72, -0.4), (0.22, 0.42, 0.1)))), DARK, bone, fuse=False, surface="metal")
        self.bone, self.tag = bone, tag

    def crown(self, spikes, colour=MAGENTA):
        """A fan of flat spikes round the back of the head: (angle from up in degrees, length, width) in r."""
        k, M, r = self.k, self.M, self.r
        for j, (ang, length, width) in enumerate(spikes):
            th = math.radians(ang)
            base = self.at((0.62 * math.sin(th), 0.42, 0.12 + 0.62 * math.cos(th)))
            d = M @ unit(math.sin(th), 0.38, math.cos(th))
            k.add(f"crown{self.tag}{j}", plate_spike(base, d, length * r, width * r, 0.032, M @ unit(0, 1, 0)),
                  colour, self.bone, fuse=False, surface="metal")

    def eyes(self, state):
        k, M, r = self.k, self.M, self.r
        def open_look(u, v):
            c = np.tile(SOCKET, (len(u), 1))
            c[((u + 0.12) / 0.42) ** 2 + (v / 0.5) ** 2 < 1] = PUPIL
            c[((u + 0.25) / 0.14) ** 2 + ((v - 0.22) / 0.14) ** 2 < 1] = srgb("#FFD0E4")
            return c
        look = eye_look(state, open_look, srgb(BLUE), SOCKET, line=0.2)
        for s in (1, -1):
            p = dn.on_ell(SKULL_C, SKULL_R, (0.62 * s, -0.62, 0.22))
            nl = (p - np.array(SKULL_C)) / np.array(SKULL_R) ** 2
            n = M @ unit(*nl)
            up = M[:, 2]
            right = np.cross(up, n); right /= np.linalg.norm(right)
            f = -1.0 if np.dot(right, -M[:, 1]) > 0 else 1.0           # +u away from the beak
            k.decal(self.at(p), n, (0.36 * r, 0.29 * r), (lambda u, v, lk=look, f=f: lk(f * u, v)), up=up, eps=0.004,
                    rings=20, segs=56)


def add_body(k):
    # the black mantle over the shoulders, ragged in points below
    k.add("mantle", ell((0, -0.005, 0.535), (0.12, 0.105, 0.065)), BLACK, "body")
    for j, a in enumerate(np.radians([-140, -100, -60, -25, 25, 60, 100, 140, 180])):
        b = np.array([0.105 * math.sin(a), -0.005 - 0.09 * math.cos(a), 0.51])
        k.add(f"rag{j}", plate_spike(b, unit(0.4 * math.sin(a), -0.4 * math.cos(a), -1), 0.085, 0.065, 0.03,
                                     (math.sin(a), -math.cos(a), 0)), BLACK, "body", fuse=False, surface="metal")
    # the dark ribbed abdomen, a magenta stripe down each side of its front, blue at its foot
    k.add("abdomen", ell((0, 0.015, 0.385), (0.1, 0.088, 0.15)), DARK, "tail")
    def ribs(P):
        a = np.arctan2(P[:, 0], -(P[:, 1] - 0.015))
        return inside(np.abs(np.mod(a + 0.2, 0.4) - 0.2) - 0.035, 0.01) * (P[:, 2] > 0.27)
    k.paint(ribs, RIB, on=["abdomen"])
    def stripes(P):
        a = np.abs(np.arctan2(P[:, 0], -(P[:, 1] - 0.015)))
        return inside(np.abs(a - 0.26) - 0.09, 0.006) * (P[:, 2] > 0.27)
    k.paint(stripes, MAGENTA, on=["abdomen"])
    k.paint(lambda P: inside(P[:, 2] - 0.275, 0.003), BLUE, on=["abdomen"])
    # two blue legs hanging, black claws
    k.add("thigh", tube([(0.05, -0.03, 0.4), (0.08, -0.07, 0.33), (0.085, -0.05, 0.27)], [0.05, 0.047, 0.04]),
          BLUE, "leg{s}", mirror=True)
    k.add("shin", tube([(0.085, -0.05, 0.27), (0.088, -0.02, 0.22), (0.088, -0.035, 0.19)], [0.038, 0.034, 0.03]),
          BLUE, "leg{s}", mirror=True)
    for j, (dx, dy) in enumerate(((0.012, -1.0), (-0.012, -1.0), (0.0, 0.7))):
        k.add(f"claw{j}", plate_spike((0.088 + dx, -0.035, 0.175), (dx * 10, dy * 0.6, -1), 0.05, 0.03, 0.026,
                                      (1, 0, 0)), BLACK, "leg{s}", fuse=False, mirror=True, surface="metal")


def add_wings(k):
    root = np.array((0.055, 0.075, 0.585))
    for j, (d1, l1, d2, l2) in enumerate((((0.3, 0.45, 1.0), 0.17, (0.75, 0.55, 0.55), 0.2),
                                          ((0.55, 0.6, 0.6), 0.16, (0.85, 0.5, 0.15), 0.2),
                                          ((0.65, 0.6, 0.25), 0.14, (0.9, 0.42, -0.25), 0.18))):
        a = root + np.array((0, 0.01 * j, -0.02 * j))
        b = a + unit(*d1) * l1
        c = b + unit(*d2) * l2
        k.add(f"wing{j}a", rod(a, b, 0.042, 0.032), BLACK, "wing{s}", fuse=False, mirror=True, surface="metal")
        k.add(f"wing{j}b", rod(b, c, 0.036, 0.03), BLACK, "wing{s}", fuse=False, mirror=True, surface="metal")
        k.add(f"wingjoint{j}", outward(ell(b, (0.026, 0.026, 0.026), sub=2)), DARK, "wing{s}", fuse=False,
              mirror=True, surface="metal")


def add_arms(k):
    heads = []
    for s, side in ((1, "R"), (-1, "L")):
        q = np.array([s, 1, 1.0])
        sh, el, wr = np.array((0.09, -0.02, 0.535)) * q, np.array((0.16, -0.09, 0.5)) * q, np.array((0.19, -0.16, 0.44)) * q
        k.add(f"shoulder{side}", outward(ell(sh, (0.04, 0.04, 0.04), sub=3)), DARK, f"arm{side}", fuse=False,
              surface="metal")
        k.add(f"armup{side}", rod(sh, el, 0.04, 0.034), BLACK, f"arm{side}", fuse=False, surface="metal")
        k.add(f"elbow{side}", outward(ell(el, (0.027, 0.027, 0.027), sub=3)), DARK, f"arm{side}", fuse=False,
              surface="metal")
        k.add(f"armlow{side}", rod(el, wr, 0.038, 0.032), BLACK, f"arm{side}", fuse=False, surface="metal")
        h = Head(k, side, f"arm{side}", (0.2 * s, -0.22, 0.42), unit(0.2 * s, -1, -0.3), 0.075)
        # the ring of magenta spikes round the wrist, behind the head
        for j in range(9):
            th = 2 * math.pi * j / 9
            radial = h.M @ unit(math.sin(th), 0, math.cos(th))
            base = h.at((0, 0.75, 0.1)) + radial * 0.035
            d = radial * 0.9 + h.M[:, 1] * 0.55
            k.add(f"collar{side}{j}", plate_spike(base, d, 0.07, 0.042, 0.028, h.M[:, 1]), MAGENTA, f"arm{side}",
                  fuse=False, surface="metal")
        heads.append(h)
    return heads


def iron_jugulis(src, out):
    k = Kit(src, coat="metal")
    add_body(k)
    add_wings(k)
    add_arms(k)
    k.add("neck", tube([(0, -0.02, 0.55), (0, -0.07, 0.6), (0, -0.11, 0.635)], [0.045, 0.04, 0.042]), BLACK, "head")
    main = Head(k, "M", "head", (0, -0.14, 0.665), unit(0, -1, -0.25), 0.1)
    main.crown([(a, 1.6 if abs(a) < 70 else 1.3, 0.72) for a in (-120, -90, -60, -30, 0, 30, 60, 90, 120)])
    k.build(voxel=0.0065, smooth=4, band=0.005, wband=0.05)
    k.eye_states(main.eyes)
    k.export(out)


if __name__ == "__main__":
    iron_jugulis(*args())
