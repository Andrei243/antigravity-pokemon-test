# Roaring Moon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/roaringmoon.py -- <the export's Roaring Moon.glb> <out.glb>
#
# A Salamence of old (salamence.py's frame: its crests and claws as flat-faced hulls, its wings as thick slabs, its
# angry eye under a brow through sibling("bagon")). From the references: a hovering dragon under a great crescent of
# red wings, the two wings rising from its shoulders, out and up and curving in over it until their tips nearly meet,
# the outer edge of each a row of rounded feathers tipped teal; a small teal body leaning forward, its chest and belly
# red with yellow chevrons; a thick teal neck carrying the head forward; a white mane swept back over the skull and a
# crown of jagged teal crests round it; small angry red eyes; the jaws open on white fangs; thick teal arms reaching
# forward and down to hands of three long pale claws; short legs hanging tucked under it; a red tail hanging down and
# curling, tipped teal; a ragged black fringe spreading behind and below the body, yellow where it meets the wings.
# The pictures and the game's own sculpt (a hovering bird) overrule the task's "on four legs": it hovers, the arms
# forward, the legs tucked. The skeleton has no leg bones: the legs are on `body`.
# Bones: body (0, 0, 0.456), wing{s} (0.08, 0.01, 0.516), arm{s} (0.08, -0.08, 0.506), head (0, -0.08, 0.566),
# tail (0, 0.08, 0.396).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, outward, srgb, inside, unit, polygon, lumpy, args, sibling

bg = sibling("bagon")

TEAL, RED, DARKRED, YELLOW, BLACK = "#3FA8D8", "#D2203E", "#A8182E", "#F2CE30", "#1E1C22"
FEATHER, MANE, CLAW, MOUTH, FANG = "#6FE2EE", "#EEF0F4", "#B8F0F4", "#6A1622", "#F6F6F2"
EYE_RED = srgb("#E8202C")

HC = np.array([0.0, -0.14, 0.62])            # the skull's middle
HR = np.array([0.064, 0.078, 0.058])


def spike(base, d, length, width, thick, up):
    """A crest, claw or spike: a flat-faced blade from base along d, wide across `up` (salamence.py's)."""
    base, d = np.array(base, float), unit(*d)
    up = np.array(up, float); up = up - d * (up @ d); up /= np.linalg.norm(up)
    side = np.cross(d, up)
    b = base - d * 0.015
    return hull([b + side * thick / 2, b - side * thick / 2, b + up * width / 2, b - up * width / 2,
                 base + d * length * 0.5 + side * thick * 0.3, base + d * length * 0.5 - side * thick * 0.3,
                 base + d * length * 0.5 + up * width * 0.3, base + d * length * 0.5 - up * width * 0.3,
                 base + d * length])


# --- the crescent: two wings, out from the shoulders and up, curving in over the head --------------------------------
C = np.array([0.0, 0.66])                    # the crescent's middle, (x, z) in the wings' plane
RI = 0.21                                    # the inner edge's radius
T0, T1 = -50.0, 82.0                         # the wing from its root to its tip (degrees from straight out)
LOBES, LOBE = 10, 0.085
ORIGIN, EX, EZ = np.array([0.0, -0.13, 0.0]), unit(1, 0.12, 0), unit(0, 0.3, 1)


def core(t):
    """The wing's breadth beyond the inner edge at angle t (degrees)."""
    a = np.clip((t - T0) / 40, 0, 1)
    b = np.clip((T1 - t) / 28, 0, 1)
    return 0.09 + 0.1 * a * b ** 0.6 - 0.09 * (1 - b)


def polar(t, r):
    t = math.radians(t)
    return (C[0] + r * math.cos(t), C[1] + r * math.sin(t))


def wing_outline():
    pts = [polar(t, RI) for t in np.linspace(T0, T1, 40)]
    for t in np.linspace(T1, T0, 260):
        frac = (T1 - t) / (T1 - T0) * LOBES
        bump = abs(math.sin(math.pi * frac)) ** 0.8 * (frac > 0.6)
        pts.append(polar(t, RI + core(t) + LOBE * bump))
    return pts


def wings(k):
    k.add("wing", slab(wing_outline(), ORIGIN, EX, EZ, 0.024), RED, "wing{s}", fuse=False, mirror=True, surface="fur")
    # the teal tips, thicker than the wing so they show on both faces
    for i in range(1, LOBES):
        t = T1 - (i + 0.5) * (T1 - T0) / LOBES
        r = RI + core(t) + LOBE
        a = math.radians(t)
        rad, tan = np.array([math.cos(a), math.sin(a)]), np.array([-math.sin(a), math.cos(a)])
        mid = np.array(C) + rad * (r - 0.04)
        th = np.linspace(0, 2 * math.pi, 20, endpoint=False)
        oval = [tuple(mid + rad * 0.042 * math.cos(x) + tan * 0.019 * math.sin(x)) for x in th]
        k.add(f"feather{i}", slab(oval, ORIGIN, EX, EZ, 0.032), FEATHER, "wing{s}", fuse=False, mirror=True,
              surface="fur")
    # a darker band along the inner edge
    inner = [polar(t, RI - 0.004) for t in np.linspace(T0 + 3, T1 - 4, 30)] + \
            [polar(t, RI + 0.03) for t in np.linspace(T1 - 4, T0 + 3, 30)]
    k.add("wingband", slab(inner, ORIGIN, EX, EZ, 0.03), DARKRED, "wing{s}", fuse=False, mirror=True, surface="fur")


def fringe(k):
    """The ragged black fringe behind and below the body, yellow where it meets the wings."""
    xs = np.linspace(0.28, -0.28, 13)
    low = []
    for i, x in enumerate(xs):
        depth = 0.33 - 0.13 * (1 - (x / 0.3) ** 2) if i % 2 == 0 else 0.43 - 0.03 * (1 - (x / 0.3) ** 2)
        low.append((x, depth))
    outline = [(-0.09, 0.5), (0.09, 0.5), (0.27, 0.45)] + low[1:-1] + [(-0.27, 0.45)]
    k.add("fringe", slab(outline, (0, 0.06, 0.0), (1, 0.0, 0), (0, 0.2, 1), 0.022), BLACK, "body", fuse=False,
          surface="fur")
    tri = [(0.07, 0.51), (0.16, 0.53), (0.11, 0.47)]
    k.add("yellow", slab(tri, (0, 0.045, 0.0), (1, 0.0, 0), (0, 0.2, 1), 0.026), YELLOW, "body", fuse=False,
          mirror=True, surface="fur")


def body(k):
    k.add("torso", ell((0, -0.01, 0.47), (0.085, 0.078, 0.11), rot=(-22, 0, 0)), TEAL, "body")
    k.add("hips", ell((0, 0.03, 0.385), (0.07, 0.068, 0.065)), TEAL, "body")
    k.add("neck", tube([(0, -0.05, 0.54), (0, -0.09, 0.58), (0, -0.12, 0.6)], [0.058, 0.05, 0.046]), TEAL, "head")
    # the red chest and belly
    def chest(P):
        return inside(P[:, 1] + 0.035 + 0.2 * (P[:, 2] - 0.45), 0.004) * inside(np.abs(P[:, 0]) - 0.058, 0.004) * \
            inside(P[:, 2] - 0.56, 0.004) * inside(0.34 - P[:, 2], 0.004)
    k.paint(chest, RED, on=["torso", "hips"])
    # yellow chevrons on the chest, each its own patch
    for j, z in enumerate((0.5, 0.465, 0.43)):
        y = -0.09 + 0.16 * (0.5 - z) - 0.005
        k.decal((0, y, z), unit(0, -1, 0.25), (0.035 - 0.004 * j, 0.008), YELLOW, eps=0.003, rings=6, segs=40,
                outline=polygon([(-1, 1), (0, 0.3), (1, 1), (1, 0.4), (0, -0.7), (-1, 0.4)]))


def head(k):
    k.add("skull", ell(HC, HR), TEAL, "head")
    k.add("snout", ell(HC + (0, -0.075, 0.0), (0.042, 0.06, 0.03), rot=(-8, 0, 0)), TEAL, "head")
    k.add("jaw", ell(HC + (0, -0.06, -0.05), (0.038, 0.055, 0.018), rot=(16, 0, 0)), TEAL, "head")
    k.add("mouth", ell(HC + (0, -0.065, -0.025), (0.034, 0.05, 0.02), rot=(4, 0, 0)), MOUTH, "head")
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"fang{side}", bg.fang(HC + (0.022 * s, -0.115, -0.012), unit(0, -0.2, -1), 0.03, 0.02), FANG, "head",
              fuse=False, surface="shell")
    # the white mane swept back over the skull
    k.add("mane", ell(HC + (0, 0.03, 0.045), (0.06, 0.085, 0.035), rot=(-12, 0, 0), deform=lumpy(5, 2, 0.12)), MANE,
          "head")
    for j, (x, d, ln) in enumerate(((0.0, (0, 0.8, 0.45), 0.12), (0.035, (0.4, 0.8, 0.35), 0.1))):
        b = HC + (x, 0.06, 0.05)
        k.add(f"manespike{j}", spike(b, d, ln, 0.05, 0.025, (0, -0.5, 1)), MANE, "head", fuse=False,
              mirror=x > 0, surface="fur")
    # the jagged teal crests round the head, out and back
    for j, (o, d, ln, w) in enumerate((((0.055, 0.0, 0.03), (0.75, 0.45, 0.5), 0.13, 0.05),
                                       ((0.06, 0.02, -0.015), (0.85, 0.45, -0.05), 0.11, 0.045),
                                       ((0.045, -0.02, -0.045), (0.6, 0.3, -0.7), 0.09, 0.04))):
        k.add(f"crest{j}", spike(HC + np.array(o), d, ln, w, 0.026, (0, 0, 1)), TEAL, "head", fuse=False,
              mirror=True, surface="scales")


def arms(k):
    SH, EL, HA = np.array((0.08, -0.07, 0.52)), np.array((0.13, -0.13, 0.45)), np.array((0.095, -0.215, 0.395))
    k.add("arm", tube([SH, EL, HA], [0.042, 0.036, 0.032]), TEAL, "arm{s}", mirror=True)
    k.add("hand", ell(HA + (0, -0.01, -0.01), (0.034, 0.034, 0.03)), TEAL, "arm{s}", mirror=True)
    for j, dx in enumerate((-0.02, 0.0, 0.02)):
        k.add(f"claw{j}", spike(HA + (dx, -0.03, -0.015), (dx * 2, -0.55, -1), 0.08, 0.026, 0.02, (0, 1, 0)), CLAW,
              "arm{s}", fuse=False, mirror=True, surface="shell")


def legs(k):
    k.add("thigh", ell((0.065, 0.02, 0.36), (0.045, 0.05, 0.055)), TEAL, "body", mirror=True)
    k.add("shin", tube([(0.07, 0.0, 0.33), (0.08, -0.02, 0.27)], [0.032, 0.027]), TEAL, "body", mirror=True)
    for j, dx in enumerate((-0.015, 0.015)):
        k.add(f"toe{j}", spike((0.08 + dx, -0.035, 0.265), (dx * 2, -0.6, -1), 0.05, 0.02, 0.016, (0, 1, 0)), CLAW,
              "body", fuse=False, mirror=True, surface="shell")


TAIL = [(0, 0.06, 0.37), (0.0, 0.07, 0.3), (0.02, 0.04, 0.22), (0.05, -0.01, 0.17), (0.08, -0.04, 0.165)]


def tail(k):
    k.add("tail", tube(TAIL, [0.04, 0.034, 0.028, 0.022, 0.016]), RED, "tail")
    k.paint(lambda P: inside(P[:, 1] + 0.005, 0.004) * inside(P[:, 2] - 0.2, 0.004) * (P[:, 0] > 0.035), FEATHER,
            on=["tail"])


def eyes(k):
    de = unit(0.62, -0.72, 0.3)
    E = bg.on_ell(HC, HR, de)
    cut = bg.brow_cut(0.24, 0.5)
    look = bg.angry_eye(cut, iris=EYE_RED, pupil_c=(-0.2, -0.12), pupil_r=0.26, iris_r=0.55)
    return bg.eyes_under_brow(k, E, de, (0.026, 0.024), look, cut, srgb(TEAL))


def roaringmoon(src, out):
    k = Kit(src, coat="scales")
    body(k)
    head(k)
    arms(k)
    legs(k)
    tail(k)
    wings(k)
    fringe(k)
    lay = eyes(k)
    k.build(voxel=0.0055, smooth=4, band=0.006, wband=0.04)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    roaringmoon(*args())
