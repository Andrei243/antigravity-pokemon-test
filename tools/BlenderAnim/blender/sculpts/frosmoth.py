# Frosmoth sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/frosmoth.py -- <the export's Frosmoth.glb> <out.glb>
#
# Grown from snom.py (sibling("snom")): the same white, the same pale ice blue, a face of the same smooth white,
# its puff of a mouth grown into a ruff of rolls.
# From the references: a moth of ice floating. Its body is one big round white head-and-thorax with the face high on
# its front, a ruff of three soft white rolls stacked under the face like a scarf; on its crown two big feathery
# white antennae like leaves, rising in a V and curling out at the top. Its eyes are almond-shaped, slanting up
# outward, dark blue with a navy rim and a green glint, with thin yellow-green lines like frost running out from their
# corners, and a small blue diamond on the brow between them. Two great wings, the largest mass: each forewing reaches
# up and out from the shoulder, narrow at the root and broad and blunt at its tip; under it a hindwing hangs down and
# out to a long point. The wings are white with pale blue snowflakes on them. They are a mirror pair raised in a V
# and swept back, so both of the game's cameras see their faces.
# Skeleton: body (0, 0, 0.204), wing{s} (+-0.04, -0.01, 0.234), head (0, -0.08, 0.244).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, slab, srgb, unit, eye_look, polygon, args, sibling

sn = sibling("snom")

WHITE, ROLL, ICE = sn.WHITE, "#E2E5F4", "#A9BBE6"
WING, WING_EDGE = "#EEF1FA", "#C4CFEE"
NAVY, IRIS, GLINT, FROST, GEM = "#1C2462", "#3A4CC0", "#8ED47E", "#C2E496", "#3448B8"

HC = np.array([0.0, -0.055, 0.252])          # the head-and-thorax's middle
HR = np.array([0.096, 0.088, 0.09])
VOXEL = 0.004


def on(az, el, c=HC, r=HR):
    return sn.on(c, r, az, el)


def body(k):
    k.add("head", ell(HC, HR), WHITE, "head")
    k.add("thorax", ell((0, 0.012, 0.228), (0.064, 0.06, 0.062)), WHITE, "body")
    # the ruff: three soft rolls stacked under the face, each a little smaller
    for i, (y, z, rx, ry, rz) in enumerate(((-0.104, 0.186, 0.08, 0.042, 0.032), (-0.094, 0.157, 0.068, 0.04, 0.029),
                                            (-0.078, 0.133, 0.05, 0.034, 0.025))):
        k.add(f"roll{i}", ell((0, y, z), (rx, ry, rz)), ROLL, "body")


def antennae(k):
    """Two feathery leaves rising from the crown in a V, curling out at the top; a vein down each."""
    for sx in (1, -1):
        root, _ = on(sx * 28, 62)
        root = root - np.array([0, 0, 0.01])
        d = unit(0.4, 0.12, 1.0)
        across = unit(1.0, 0.0, -0.4)
        L, W = 0.17, 0.058
        t = np.linspace(0.0, 1.0, 22)
        w = W * np.sin(np.pi * np.clip(t, 0, 1) ** 0.8) * (1 - 0.15 * t) + 0.004
        curl = 0.085 * t ** 2.2                      # bends outward toward its top
        upper = np.stack([t * L, curl + w], axis=1)
        lower = np.stack([t * L, curl - w * 0.8], axis=1)[::-1]
        outline = np.vstack([upper, lower])
        vein = np.vstack([np.stack([t[1:-3] * L, curl[1:-3] + 0.006], axis=1),
                          np.stack([t[1:-3] * L, curl[1:-3] - 0.006], axis=1)[::-1]])
        if sx < 0:
            dd, aa = d * np.array([-1, 1, 1]), across * np.array([-1, 1, 1])
        else:
            dd, aa = d, across
        k.add("antenna" + ("R" if sx > 0 else "L"), slab(outline, root, dd, aa, 0.012), WHITE, "head", fuse=False,
              surface="leaf")
        k.add("vein" + ("R" if sx > 0 else "L"), slab(vein, root, dd, aa, 0.016), WING_EDGE, "head", fuse=False,
              surface="leaf")


def flake(centre, size, turn=0.0):
    """A snowflake's six arms as rhombi (outlines in the wing's own a, b)."""
    out = []
    for i in range(6):
        a = turn + i * math.pi / 3
        d = np.array([math.cos(a), math.sin(a)])
        s = np.array([-d[1], d[0]])
        c = np.array(centre)
        out.append([c + d * size * 0.12, c + d * size * 0.55 + s * size * 0.17, c + d * size, c + d * size * 0.55
                    - s * size * 0.17])
    return out


FORE = [(0.0, 0.025), (0.08, 0.05), (0.18, 0.08), (0.28, 0.11), (0.36, 0.125), (0.42, 0.11), (0.45, 0.06),
        (0.445, -0.005), (0.41, -0.06), (0.34, -0.085), (0.23, -0.07), (0.13, -0.05), (0.05, -0.035), (0.0, -0.02)]
# the hindwing along its own length, down and out
HIND = [(0.0, 0.02), (0.06, 0.035), (0.13, 0.03), (0.19, 0.01), (0.24, -0.02), (0.27, -0.04), (0.21, -0.045),
        (0.15, -0.04), (0.07, -0.03), (0.0, -0.02)]


def wings(k):
    root = np.array([0.03, 0.01, 0.236])
    # the forewing: raised in a V of 28 degrees, swept back
    ex, ez = unit(1.0, 0.36, 0.45), unit(0.0, 0.25, 1.0)
    k.add("fore", slab(np.array(FORE) * (0.8, 1.0), root, ex, ez, 0.009), WING, "wing{s}", fuse=False, mirror=True, surface="leaf")
    for j, (c, s, tn) in enumerate((((0.275, 0.02), 0.07, 0.3), ((0.13, 0.0), 0.04, 0.0))):
        for i, rh in enumerate(flake(c, s, tn)):
            k.add(f"foreflake{j}{i}", slab(rh, root, ex, ez, 0.014), ICE, "wing{s}", fuse=False, mirror=True,
                  surface="leaf")
    # the hindwing: hanging down and out under it, swept back too
    hroot = root + np.array([-0.005, 0.01, -0.02])
    hx, hz = unit(1.0, 0.45, -0.85), unit(0.0, 0.3, 1.0)
    k.add("hind", slab(HIND, hroot, hx, hz, 0.009), WING, "wing{s}", fuse=False, mirror=True, surface="leaf")
    for i, rh in enumerate(flake((0.16, -0.005), 0.038, 0.5)):
        k.add(f"hindflake{i}", slab(rh, hroot, hx, hz, 0.014), ICE, "wing{s}", fuse=False, mirror=True,
              surface="leaf")


# --- the face -------------------------------------------------------------------------------------------------------
EYE_AZ, EYE_EL, EYE_HALF = 23, 14, (0.031, 0.02)
EYE_OUT = [(1.0, 0.62), (0.4, 0.72), (-0.4, 0.42), (-1.0, -0.05), (-0.5, -0.7), (0.35, -0.72), (0.9, -0.15)]
EYE_O = polygon(EYE_OUT)
EYE_OL = polygon([(-a, b) for a, b in EYE_OUT])


def rim_share(u, v):
    th = np.arctan2(v, u)
    return np.hypot(u, v) / np.array([EYE_O(t) for t in th])


def eye_open(u, v):
    c = np.tile(srgb(IRIS), (len(u), 1))
    c[rim_share(u, v) > 0.78] = srgb(NAVY)
    c[((u + 0.05) / 0.3) ** 2 + ((v + 0.25) / 0.3) ** 2 < 1] = srgb(GLINT)
    c[((u - 0.2) / 0.2) ** 2 + ((v - 0.3) / 0.2) ** 2 < 1] = srgb("#FFFFFF")
    return c


def stroke(k, a, b, width, colour):
    """A straight thin mark from (az, el) a to b on the head."""
    pa, _ = on(*a)
    pb, _ = on(*b)
    pm, nm = on((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)
    d = pb - pa
    d = d - nm * (d @ nm)
    L = np.linalg.norm(d) / 2
    t = width / 2 / L
    k.decal(pm, nm, (L, L), colour, up=d / np.linalg.norm(d), eps=0.0026,
            outline=polygon([(-t, -1), (t * 1.2, -0.3), (t, 0.7), (0, 1), (-t, 0.7)]), rings=8, segs=48)


def face_marks(k):
    for sx in (1, -1):
        # frost running out of the eye's corners: up and out, out, and up from the inner corner
        stroke(k, (sx * 37, 22), (sx * 52, 42), 0.011, FROST)
        stroke(k, (sx * 40, 13), (sx * 60, 9), 0.011, FROST)
        stroke(k, (sx * 6, 23), (sx * 10, 40), 0.01, FROST)
    p, n = on(0, 34)
    k.decal(p, n, (0.009, 0.014), GEM, eps=0.0022, outline=polygon([(1, 0), (0, 1), (-1, 0), (0, -1)]),
            rings=8, segs=48)


def lay(k, state):
    look = eye_look(state, eye_open, srgb(WHITE), srgb(NAVY), line=0.18)
    for sx in (1, -1):
        p, n = on(sx * EYE_AZ, EYE_EL)
        k.decal(p, n, EYE_HALF, look if sx > 0 else (lambda u, v, g=look: g(-u, v)), eps=0.0018,
                outline=EYE_O if sx > 0 else EYE_OL, rings=20, segs=56)


def frosmoth(src, out):
    k = Kit(src, coat="fur")
    body(k)
    antennae(k)
    wings(k)
    face_marks(k)
    k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.05)
    k.eye_states(lambda state: lay(k, state))
    k.export(out)


if __name__ == "__main__":
    frosmoth(*args())
