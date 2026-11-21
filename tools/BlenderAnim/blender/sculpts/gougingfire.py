# Gouging Fire sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gougingfire.py -- <the export's Gouging Fire.glb> <out.glb>
#
# Entei's ancient kin, built anew on raikou.py's plates and eyes (through sibling("raikou")). From the references: a
# heavy brown beast crouched low on four thick shaggy legs set wide, its belly near the ground, black shackles banded
# grey round each leg, pale blue-grey paws with long grey claws striped red. A fluffy golden ruff round the neck and
# shoulders. The head is carried low in front: a red plate over the face in a V to the nose, its corners swept out,
# grey tusks hooking down from under it, a cream jaw hanging open on a pink mouth, blue eyes under the plate's edge.
# The design's largest mass is the crest over the head: a striped column of gold, green and red rising up and back
# from the brow, and a fan of curved golden blades flaring out to both sides and up from it, like a headdress. Grey
# blades rise from the back; a green zigzag runs round the trunk; a cream cloud of smoke trails behind as the tail,
# curling up and out to +X.
# Reference ratios (3D picture, toe to crest 1.0): the head with its crest 0.82 tall and 0.55 wide (the largest
# mass), the face plate 0.3 tall, the belly 0.12 off the ground (built 0.14), the forelegs 0.15 across, the length
# 1.08 of the height.
# Bones: body (0, 0.02, 0.4); head (0, -0.22, 0.5); legs (+-0.13, -0.18, 0.32); hind legs (+-0.13, 0.18, 0.32);
# tail (0, 0.28, 0.46).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, lathe, outward, srgb, inside, tri, unit, lumpy, args, sibling)

r = sibling("raikou")

BROWN = "#9A5E30"
DARK = "#6A3E22"
RUFF = "#E2B850"
GOLD = "#F2C62E"
GOLD_DARK = "#D09A26"
RED = "#D8302E"
GREEN = "#46AE50"
SPIKE = "#A8B2C2"
PAW = "#B0BACC"
TUSK = "#C8CED8"
CREAM = "#F0DCA8"
MOUTH = "#C84A5A"
SMOKE = "#E2DEC4"
SHACKLE = "#24252A"
EYE, IRIS, PUPIL = "#F6F4EE", "#5A5ED6", "#14102A"

H = np.array([0.0, -0.37, 0.43])
HR = np.array([0.132, 0.13, 0.112])
EYE_DIR = unit(0.66, -0.66, 0.2)
EYE_R = 0.029
FX, FY = 0.2, -0.29          # where the fore paws stand, wide
BX, BY = 0.19, 0.24


def kite(base, d, length, width, flat=0.55, up=(0, 0, 1.0), mid=0.45):
    b = np.array(base, float)
    d = unit(*d)
    a = np.cross(d, up)
    if np.linalg.norm(a) < 1e-3:
        a = np.cross(d, (1.0, 0, 0))
    a /= np.linalg.norm(a)
    n = np.cross(a, d)
    w = width
    m = b + d * length * mid
    pts = [b + a * w, b - a * w, b + n * w * flat, b - n * w * flat,
           m + a * w * 0.62, m - a * w * 0.62, m + n * w * flat * 0.6, m - n * w * flat * 0.6, b + d * length]
    return hull(pts)


def curved_blade(k, name, path, width, thick, face, color, bone):
    """A curved flat-faced blade along a path: a flat hull between each pair of sections, width tapering to a point."""
    path = [np.array(p, float) for p in path]
    m = len(path)
    secs = []
    for i, p in enumerate(path):
        d = path[min(i + 1, m - 1)] - path[max(i - 1, 0)]; d /= np.linalg.norm(d)
        f = np.array(face, float) - d * (np.array(face, float) @ d); f /= np.linalg.norm(f)
        a = np.cross(f, d)
        w = width * (1 - i / (m - 1)) ** 0.8 + 0.004
        t = thick * (1 - 0.6 * i / (m - 1))
        secs.append([p + a * w, p - a * w * 0.5, p + f * t / 2, p - f * t / 2])
    for i in range(m - 1):
        k.add(f"{name}{i}", hull(secs[i] + secs[i + 1]), color, bone, fuse=False, surface="shell")


def add_trunk(k):
    k.add("chest", ell((0, -0.13, 0.33), (0.18, 0.15, 0.15)), BROWN, "body")
    k.add("middle", ell((0, 0.04, 0.31), (0.17, 0.18, 0.14)), BROWN, "body")
    k.add("rump", ell((0, 0.2, 0.33), (0.165, 0.12, 0.14)), BROWN, "body")
    # A green zigzag band round the trunk behind the ruff
    def band(P):
        y = 0.02 + 0.025 * tri(P[:, 2] / 0.06)
        return inside(np.abs(P[:, 1] - y) - 0.018, 0.003) * (P[:, 2] > 0.3)
    k.paint(band, GREEN, on=["middle", "chest", "rump"])


def add_ruff(k):
    """The fluffy golden ruff round the neck and shoulders, ragged at its hem."""
    k.add("ruff", ell((0, -0.2, 0.42), (0.23, 0.14, 0.16), deform=lumpy(6, 3, 0.06)), RUFF, "body")
    k.add("ruffnape", ell((0, -0.25, 0.53), (0.15, 0.1, 0.09), deform=lumpy(5, 3, 0.05)), RUFF, "head")
    for j, ang in enumerate(np.radians([-150, -120, -90, -60, -30, 0])):
        for s in (1, -1):
            if j == 5 and s < 0:
                continue
            x = math.cos(ang) * 0.22 * s
            z = 0.42 + math.sin(ang) * 0.15
            d = unit(math.cos(ang) * s, -0.2, math.sin(ang) - 0.3)
            k.add(f"rufftuft{j}{'R' if s > 0 else 'L'}", kite((x, -0.2, z), d, 0.09, 0.045, flat=0.7), RUFF, "body",
                  fuse=False, surface="fur")


def add_legs(k):
    for name, bone, top, paw, mass in (("fore", "leg{s}", (0.13, -0.18, 0.32), (FX, FY), (0.1, 0.11, 0.14)),
                                       ("hind", "hind{s}", (0.13, 0.18, 0.32), (BX, BY), (0.105, 0.13, 0.15))):
        x0, y0, z0 = top
        px, py = paw
        k.add(name + "mass", ell((x0 + 0.01, y0 + 0.01, z0 - 0.04), mass), BROWN, bone, mirror=True)
        k.add(name, tube([(x0, y0, z0), ((x0 + px) / 2 + 0.01, (y0 + py) / 2, 0.17), (px, py, 0.06)],
                         [0.08, 0.068, 0.06]), BROWN if name == "fore" else DARK, bone, mirror=True)
        k.add(name + "fur", ell((px, py, 0.085), (0.075, 0.08, 0.035), deform=lumpy(6, 2, 0.12)),
              BROWN if name == "fore" else DARK, bone, mirror=True)
        k.add(name + "paw", ell((px, py - 0.02, 0.035), (0.07, 0.08, 0.036)), PAW, bone, mirror=True)
        k.add(name + "shackle", lathe((px - 0.01 * (1 if name == "fore" else -1) * 0, (y0 + py) / 2 * 0.3 + py * 0.7,
                                       0.135), unit(-0.15, 0.1 if name == "fore" else -0.1, 1),
                                      [(0.055, -0.02), (0.074, -0.02), (0.074, 0.02), (0.055, 0.02),
                                       (0.055, -0.02)], seg=48), SHACKLE, bone, fuse=False, mirror=True,
              surface="metal")
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        for bone, py in ((f"leg{sd}", FY), (f"hind{sd}", BY)):
            px = (FX if bone.startswith("leg") else BX) * s
            for j, dx in enumerate((-0.04, 0.0, 0.04)):
                b = np.array((px + dx * s, py - 0.085, 0.04))
                d = unit(dx * s * 4, -1, -0.45)
                k.add(f"claw{bone}{j}", kite(b, d, 0.06, 0.017, flat=0.9), TUSK, bone, fuse=False, surface="shell")
                k.add(f"clawred{bone}{j}", kite(b + d * 0.03 + (0, 0, 0.004), d, 0.035, 0.012, flat=0.95), RED, bone,
                      fuse=False, surface="shell")


def add_head(k):
    k.add("head", ell(H, HR), BROWN, "head")
    k.add("muzzle", ell(H + (0, -0.1, -0.03), (0.08, 0.08, 0.06)), CREAM, "head")
    k.add("neck", tube([(0, -0.18, 0.44), (0, -0.25, 0.45), (0, -0.31, 0.44)], [0.12, 0.11, 0.1]), BROWN, "head")
    # The jaw dropped open, a pink mouth set back inside
    k.add("jaw", ell(H + (0, -0.1, -0.11), (0.065, 0.075, 0.03), rot=(-14, 0, 0)), CREAM, "head")
    k.add("mouth", ell(H + (0, -0.12, -0.075), (0.052, 0.055, 0.03)), MOUTH, "head")
    # The red plate over the face: a V down to the nose, its upper corners swept out over the brows
    def keep(D):
        return (D[:, 1] < -0.25) & (D[:, 2] > -0.45) & (D[:, 2] < 0.75) & (np.abs(D[:, 0]) < (D[:, 2] + 0.5) * 0.75)
    k.add("plate", r.plate(H + (0, -0.02, 0.0), HR * (1.0, 1.08, 1.0), keep, lift=0.022, under=0.008), RED, "head",
          fuse=False, surface="shell")
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        # the plate's corners flared out to the sides
        k.add(f"platewing{sd}", kite(H + (0.08 * s, -0.08, 0.05), (s, -0.4, 0.15), 0.12, 0.04, flat=0.4,
                                     up=(0, 1.0, 0)), RED, "head", fuse=False, surface="shell")
        # grey tusks hooking down beside the jaw
        curved_blade(k, f"tusk{sd}", [H + (0.05 * s, -0.13, -0.02), H + (0.07 * s, -0.17, -0.09),
                                      H + (0.065 * s, -0.17, -0.17), H + (0.045 * s, -0.14, -0.21)],
                     0.036, 0.034, (s, 0, 0), TUSK, "head")


def add_crest(k):
    """The headdress: a striped column rising up and back from the brow, golden blades fanning out on both sides."""
    base = H + (0, -0.06, 0.07)
    up = unit(0, 0.55, 1)
    # The column: gold, with a green and a red stripe on its front
    L = 0.42
    def column(w, th, front, lift=0.0):
        pts = []
        for t, ww in ((0, w), (0.6, w * 0.85), (1.0, w * 0.35)):
            c = base + up * L * t + np.array((0, -lift, 0))
            for sx in (1, -1):
                pts.append(c + np.array((sx * ww, 0, 0)) + np.array((0, -front, 0)))
                pts.append(c + np.array((sx * ww, 0, 0)) + np.array((0, th, 0)))
        return hull(pts)
    k.add("column", column(0.115, 0.06, 0.03), GOLD, "head", fuse=False, surface="shell")
    k.add("colgreen", column(0.08, 0.02, 0.036, 0.0), GREEN, "head", fuse=False, surface="shell")
    k.add("colred", column(0.04, 0.02, 0.042, 0.0), RED, "head", fuse=False, surface="shell")
    # The fan: curved gold blades from the column's sides, flaring out and curling up, faces turned forward
    fan = [(0.25, 18, 0.3), (0.45, 42, 0.33), (0.68, 68, 0.3), (0.88, 95, 0.24)]
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        for j, (t, ang, ln) in enumerate(fan):
            a = math.radians(ang)
            root = base + up * L * t + np.array((0.08 * s, 0.01, 0))
            d = np.array((math.sin(a) * s, 0.4, math.cos(a)))
            d /= np.linalg.norm(d)
            curl = np.array((0, 0.45, 1.0))
            path = [root, root + d * ln * 0.45, root + d * ln * 0.8 + curl * ln * 0.12,
                    root + d * ln * 0.95 + curl * ln * 0.35]
            curved_blade(k, f"fan{sd}{j}", path, 0.075, 0.04, (0, -1, 0.15), GOLD if j % 2 == 0 else GOLD_DARK,
                         "head")
    # Two points rising from the column's top
    top = base + up * L
    for s in (1, -1):
        curved_blade(k, f"crown{'R' if s > 0 else 'L'}", [top + (0.02 * s, 0, -0.04), top + (0.05 * s, 0.03, 0.06),
                                                         top + (0.05 * s, 0.09, 0.14)], 0.035, 0.03, (0, -1, 0.3),
                     GOLD, "head")


def add_spikes(k):
    """Grey blades rising up and back from the back."""
    for j, (x, y, d, ln) in enumerate(((0.0, -0.02, (0, 0.35, 1), 0.2), (0.07, 0.04, (0.35, 0.45, 1), 0.17),
                                       (-0.07, 0.04, (-0.35, 0.45, 1), 0.17), (0.0, 0.1, (0, 0.6, 1), 0.17),
                                       (0.06, 0.15, (0.3, 0.7, 0.8), 0.13), (-0.06, 0.15, (-0.3, 0.7, 0.8), 0.13))):
        k.add(f"spike{j}", kite((x, y, 0.42), d, ln, 0.045, flat=0.5, up=(1.0, 0, 0)), SPIKE, "body", fuse=False,
              surface="shell")


def add_smoke(k):
    """The cream cloud of smoke trailing behind as the tail, curling up and out to +X."""
    for i, (c, rr) in enumerate((((0.0, 0.3, 0.42), (0.11, 0.09, 0.09)), ((0.05, 0.4, 0.45), (0.1, 0.08, 0.085)),
                                 ((0.12, 0.47, 0.5), (0.08, 0.07, 0.075)), ((0.19, 0.5, 0.57), (0.06, 0.055, 0.06)),
                                 ((0.22, 0.47, 0.64), (0.04, 0.04, 0.045)))):
        k.add(f"smoke{i}", ell(c, rr, deform=lumpy(5, 3, 0.08)), SMOKE, "tail")


SHUT_LINE = "#2A1A14"


def eye_top(u):
    return 0.2 + 0.5 * u


def eyes(k, state):
    p, n = r.on_ell(H, HR, EYE_DIR)
    r.lay_eyes(k, state, p, n, EYE_R, r.eye_open(EYE, IRIS, PUPIL, iris_r=(0.66, 0.82)), BROWN, SHUT_LINE, eye_top)


if __name__ == "__main__":
    src, dst = args()
    k = Kit(src, coat="fur")
    add_trunk(k)
    add_legs(k)
    add_head(k)
    add_ruff(k)
    add_smoke(k)
    add_spikes(k)
    add_crest(k)
    k.build(voxel=0.008, band=0.004)
    k.eye_states(lambda s: eyes(k, s))
    k.export(dst)
