# Pincurchin sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pincurchin.py -- <the export's Pincurchin.glb> <out.glb>
#
# The design, from the references: a dark purple sea urchin, its body a low dome wider than it is tall, bristling
# with long straight purple spines tipped greyish tan (not yellow: the pictures overrule the brief): the longest
# straight up from its crown, the rest fanned out over its top, sides and back, a few short ones under its rim like
# feet. Low on its front a pair of bulging yellow lips side by side (two rounded lobes, like a peanut); beside them,
# a little higher and further out, two small round yellow eyes with a black downward triangle for a pupil.
# Skeleton: body (0, 0, 0.1), one bone.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, lathe, srgb, unit, eye_look, args

PURPLE, TIP, YELLOW, INK = "#4A3C5C", "#A8988A", "#F2BE2C", "#141218"
C = np.array([0.0, 0.0, 0.1])
R = np.array([0.122, 0.106, 0.092])
VOXEL = 0.0028


def dome(V):
    """Flatter underneath, its front a little flattened where the face is."""
    V = V.copy()
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.85, V[:, 2])
    V[:, 1] = np.where(V[:, 1] < 0, V[:, 1] * (1 - 0.08 * np.clip(-V[:, 2], 0, 1)), V[:, 1])
    return V


def on(az, el):
    """The dome's surface (as an ellipsoid) at azimuth az from the front toward +X and elevation el, and its way."""
    a, e = math.radians(az), math.radians(el)
    d = np.array([math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)])
    k = 1 / math.sqrt(((d / R) ** 2).sum())
    p = C + d * k
    n = (p - C) / R ** 2
    return p, n / np.linalg.norm(n)


# (azimuth, elevation, length, base radius, a tilt up toward the sky)
SPINES = [
    (0, 90, 0.15, 0.027, 0.0),
    (-30, 52, 0.14, 0.026, 0.0), (32, 55, 0.13, 0.026, 0.0),
    (-92, 38, 0.16, 0.027, 0.0), (90, 40, 0.16, 0.027, 0.0),
    (-82, 2, 0.15, 0.025, 0.0), (84, 0, 0.15, 0.025, 0.0),
    (-76, -24, 0.11, 0.022, 0.0), (77, -22, 0.11, 0.022, 0.0),
    (-150, 45, 0.14, 0.026, 0.0), (150, 42, 0.14, 0.026, 0.0), (180, 62, 0.13, 0.025, 0.0),
    (-128, 2, 0.14, 0.025, 0.0), (128, 4, 0.14, 0.025, 0.0), (180, 8, 0.12, 0.024, 0.0),
]
# short ones under the rim, like feet
FEET = [(30, -45), (-30, -45), (100, -50), (-100, -50), (170, -48), (65, -52), (-65, -52)]


def spine(k, name, p, d, length, radius):
    """A long cone: purple, its last third a tan tip set over it a little wider, so the colour change is crisp."""
    root = p - d * radius * 0.8
    k.add(name, lathe(root, d, [(0, -radius * 0.2), (radius, 0), (radius * 0.75, length * 0.35),
                                (radius * 0.42, length * 0.66), (radius * 0.3, length * 0.76), (0, length * 0.8)],
                      seg=12), PURPLE, "body", fuse=False, surface="shell")
    k.add(name + "tip", lathe(root, d, [(0, length * 0.6), (radius * 0.52, length * 0.62),
                                        (radius * 0.4, length * 0.72), (radius * 0.18, length * 0.88), (0, length)],
                              seg=12), TIP, "body", fuse=False, surface="shell")


EYE_AZ, EYE_EL = 50, -20


def eye_open(u, v):
    c = np.tile(srgb(YELLOW), (len(u), 1))
    c[np.hypot(u, v) > 0.84] = srgb(INK)
    tri = (v < 0.32) & (v > -0.5) & (np.abs(u) < (v + 0.5) * 0.62)
    c[tri] = srgb(INK)
    return c


def pincurchin(src, out):
    k = Kit(src, coat="shell")
    k.add("body", ell(C, R, deform=dome, sub=5), PURPLE, "body")
    # the lips: two yellow bulges side by side, low on the front
    for sx in (1, -1):
        k.add(f"lip{sx}", ell((0.027 * sx, -0.093, 0.043), (0.029, 0.024, 0.025)), YELLOW, "body")
    for i, (az, el, ln, rd, tilt) in enumerate(SPINES):
        p, n = on(az, el)
        spine(k, f"spine{i}", p, unit(*(n + np.array([0, 0, tilt]))), ln, rd)
    for i, (az, el) in enumerate(FEET):
        p, n = on(az, el)
        spine(k, f"foot{i}", p, unit(*(n + np.array([0, 0, -0.6]))), 0.03, 0.014)
    k.build(voxel=VOXEL, smooth=3, band=0.003, wband=0.05)

    def lay(state):
        look = eye_look(state, eye_open, srgb(PURPLE), srgb(YELLOW), line=0.18)
        for sx in (1, -1):
            p, n = on(sx * EYE_AZ, EYE_EL)
            k.decal(p, n, (0.024, 0.024), look if sx > 0 else (lambda u, v, g=look: g(-u, v)), eps=0.0018,
                    rings=20, segs=56)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    pincurchin(*args())
