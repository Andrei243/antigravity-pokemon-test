# Maractus sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/maractus.py -- <the export's Maractus.glb> <out.glb>
#
# The design, from the references: a green cactus dancer. A big round green head with one yellow thorn on its crown,
# round yellow eyes ringed in orange with no pupil, a small zigzag mouth; a ruff of dark teal leaves at the neck; a
# green pear of a body on one stalk (no legs), dark teal below with a ragged edge and a star of dark teal points
# spread on the ground for a foot. Two pairs of limbs: fat green cactus pads rising up and out from the head, each
# with yellow thorns and a big pink flower with a yellow heart at its end; and short arms forward and down from under
# the head, each ending in a green spiked ball (its maraca) in a cuff of dark teal leaves. The pictures show no
# flower crown and no pupils, and they put the flowers on the pads of the head, not on the arms; the pads are made a
# mirror pair here, where the art turns them each another way.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, lathe, srgb, inside, unit, tri, axes, eye_look, args

GREEN, DARK, THORN, PINK, HEART = "#86C74A", "#2E8A76", "#F4CB3C", "#F27CAB", "#F6D648"
EYE, EYE_RING, MOUTH = "#F6D23E", "#D0841E", "#24523A"

HEAD = np.array([0.0, -0.01, 0.405])
HR = np.array([0.108, 0.098, 0.1])
BODY = np.array([0.0, 0.0, 0.17])
BR = np.array([0.083, 0.078, 0.13])


def on_ball(c, r, az, z):
    c, r = np.array(c, float), np.array(r, float)
    s = math.sqrt(max(1 - ((z - c[2]) / r[2]) ** 2, 0.0))
    p = np.array([c[0] + r[0] * s * math.sin(az), c[1] - r[1] * s * math.cos(az), z])
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def thorn(k, name, root, d, length, radius, bone, colour=THORN, mirror=False, sides=6):
    """A solid thorn: a cone of flat faces from a base sunk a little behind root out along d."""
    d = unit(*d)
    a, b = axes(d)
    root = np.array(root, float)
    base = root - d * radius * 0.9
    pts = [base + radius * (math.cos(t) * a + math.sin(t) * b) for t in np.linspace(0, 2 * math.pi, sides, endpoint=False)]
    pts.append(root + d * length)
    k.add(name, hull(pts), colour, bone, fuse=False, mirror=mirror, surface="shell")


def leaf_point(k, name, root, d, length, width, thick, bone, mirror=False, up=(0, 0, 1.0)):
    """A dark teal pointed leaf: a flat-faced solid from a wide base at root out along d to its point."""
    d = unit(*d)
    side = np.cross(d, up)
    if np.linalg.norm(side) < 1e-3:
        side = np.cross(d, (0, 1.0, 0))
    side = side / np.linalg.norm(side)
    face = np.cross(side, d)
    root = np.array(root, float)
    pts = [root + side * width, root - side * width, root + face * thick, root - face * thick,
           root + d * length * 0.45 + side * width * 0.55, root + d * length * 0.45 - side * width * 0.55,
           root + d * length]
    k.add(name, hull(pts), DARK, bone, fuse=False, mirror=mirror, surface="leaf")


def ruff(k, name, centre, radius, n, length, width, droop, bone, phase=0.0, axis=(0, 0, 1.0)):
    """A ring of dark teal leaves pointing out (and down by droop) round an axis through centre."""
    a, b = axes(axis)
    ax = unit(*axis)
    for i in range(n):
        t = phase + 2 * math.pi * i / n
        o = math.cos(t) * a + math.sin(t) * b
        leaf_point(k, f"{name}{i}", np.array(centre) + o * radius, o - ax * droop, length, width, 0.016, bone,
                   up=ax)


def flower(k, name, centre, normal, side, bone):
    """A big pink flower of six rounded petals round a yellow heart, its face turned along normal."""
    n = unit(*normal)
    a, b = axes(n)
    c = np.array(centre, float)
    L, w = 0.078, 0.037
    outline = [(0.0, -0.4 * w), (0.2 * L, -0.85 * w), (0.5 * L, -w), (0.78 * L, -0.85 * w), (0.95 * L, -0.45 * w),
               (L, 0.0), (0.95 * L, 0.45 * w), (0.78 * L, 0.85 * w), (0.5 * L, w), (0.2 * L, 0.85 * w), (0.0, 0.4 * w)]
    for i in range(6):
        t = 2 * math.pi * i / 6 + 0.3
        r = math.cos(t) * a + math.sin(t) * b
        tang = np.cross(n, r)
        cup = r * math.cos(0.18) + n * math.sin(0.18)        # a shallow cup
        lift = 0.004 * (i % 2)                                # neighbours a hair apart, or they flicker
        k.add(f"{name}petal{i}", slab(outline, c + n * lift, cup, tang, 0.02), PINK, bone, fuse=False, surface="leaf")
    k.add(f"{name}heart", lathe(c + n * 0.006, n, [(0.0, -0.008), (0.024, -0.006), (0.026, 0.006), (0.016, 0.014),
                                                   (0.0, 0.016)], seg=32), HEART, bone, fuse=False, surface="leaf")


def body(k):
    def pear(V):
        V = V.copy()
        s = 1 + 0.12 * np.clip(-V[:, 2], 0, 1) - 0.22 * np.clip(V[:, 2], 0, 1) ** 1.4
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("stalk", ell(BODY, BR, deform=pear), GREEN, "body")
    k.add("hip", ell((0, 0.0, 0.1), (0.085, 0.08, 0.075)), GREEN, "root")
    for s, nm in ((1, "R"), (-1, "L")):
        k.add(f"leg{nm}", ell((s * 0.035, -0.005, 0.12), (0.055, 0.06, 0.07)), GREEN, f"leg{nm}")
    k.add("neck", ell((0, -0.005, 0.305), (0.05, 0.05, 0.04)), GREEN, "body")
    k.add("head", ell(HEAD, HR), GREEN, "head")
    # dark teal below a ragged edge
    def low(P):
        a = np.arctan2(P[:, 0], -P[:, 1])
        edge = 0.105 + 0.028 * tri(a * 5 / (2 * math.pi) + 0.25)
        return inside(P[:, 2] - edge, 0.0025)
    k.paint(low, DARK)
    # the star of points spread on the ground
    for i, az in enumerate((45, 120, 180, -120, -45)):
        o = np.array([math.sin(math.radians(az)), -math.cos(math.radians(az)), 0.0])
        bone = "legR" if o[0] > 0.2 else "legL" if o[0] < -0.2 else "root"
        leaf_point(k, f"foot{i}", (0, 0, 0.022) + o * 0.06, o + (0, 0, -0.05), 0.105, 0.04, 0.02, bone)
    ruff(k, "ruff", (0, -0.005, 0.315), 0.045, 7, 0.06, 0.028, 0.55, "body", phase=0.2)


def pads(k):
    """Fat cactus pads up and out from the head, thorns along them, a flower at each end."""
    for side, nm in ((1, "R"), (-1, "L")):
        base = np.array([side * 0.06, 0.0, 0.46])
        d = unit(side * 0.62, 0.05, 1.0)
        tip = base + d * 0.12
        k.add(f"pad{nm}", tube([base, base + d * 0.06, tip], [0.05, 0.056, 0.048]), GREEN, "head")
        for j, (t, ang) in enumerate(((0.45, 0.6), (0.7, -0.9), (0.95, 2.4))):
            a, b = axes(d)
            o = math.cos(ang) * a + math.sin(ang) * b
            thorn(k, f"padthorn{nm}{j}", base + d * 0.12 * t + o * 0.05, o + d * 0.3, 0.036, 0.013, "head")
        fn = unit(side * 0.55, -0.62, 0.55)
        flower(k, f"flower{nm}", tip + d * 0.03 + fn * 0.01, fn, side, "head")
    # the thorn on the crown
    thorn(k, "crown", HEAD + (-0.03, -0.015, HR[2] - 0.004), (-0.25, -0.2, 1), 0.052, 0.017, "head")


def arms(k):
    """Short arms forward and down from under the head, each a spiked green ball in a cuff of dark teal leaves."""
    SH, WR = np.array([0.06, -0.01, 0.305]), np.array([0.12, -0.085, 0.24])
    HAND = np.array([0.14, -0.11, 0.224])
    k.add("arm", tube([SH, (SH + WR) / 2 + (0, 0, 0.004), WR], [0.042, 0.04, 0.038]), GREEN, "arm{s}", mirror=True)
    k.add("ball", ell(HAND, (0.044, 0.044, 0.043)), GREEN, "arm{s}", mirror=True)
    d = unit(*(HAND - SH))
    a, b = axes(d)
    for i in range(5):
        t = 2 * math.pi * i / 5 + 0.3
        o = math.cos(t) * a + math.sin(t) * b
        leaf_point(k, f"cuff{i}", WR - d * 0.004 + o * 0.032, o - d * 0.45, 0.05, 0.022, 0.014, "arm{s}",
                   mirror=True, up=tuple(d))
    for i, o in enumerate([(0.3, -0.6, 0.75), (0.9, -0.3, -0.1), (0.1, -0.95, -0.35)]):
        o = unit(*o)
        thorn(k, f"ballthorn{i}", HAND + o * 0.042, o, 0.034, 0.012, "arm{s}", mirror=True)


def mouth(k):
    p, n = on_ball(HEAD, HR, 0.0, 0.358)
    def look(u, v):
        zig = 0.45 * tri(u * 1.0 + 0.5)
        ink = inside(np.abs(v - zig) - 0.24, 0.05) * inside(np.abs(u) - 0.9, 0.05)
        return srgb(GREEN)[None] * (1 - ink[:, None]) + srgb(MOUTH)[None] * ink[:, None]
    k.decal(p, n, (0.024, 0.014), look, eps=0.0035, rings=16, segs=72)


def eye_open(u, v):
    r = np.hypot(u, v)
    ring = np.maximum(1 - inside(r - 0.78, 0.02), inside(np.abs(r - 0.47) - 0.1, 0.02))
    return srgb(EYE)[None] * (1 - ring[:, None]) + srgb(EYE_RING)[None] * ring[:, None]


def eyes(k, state):
    p, n = on_ball(HEAD, HR, 0.47, 0.41)
    k.decal(p, n, (0.033, 0.035), eye_look(state, eye_open, srgb(EYE), srgb(EYE_RING), line=0.18), eps=0.005,
            rings=20, segs=56, mirror=True)


def maractus(src, out):
    k = Kit(src, coat="leaf")
    body(k)
    pads(k)
    arms(k)
    mouth(k)
    k.build(voxel=0.005, smooth=3, band=0.006)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    maractus(*args())
