# Larvesta sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/larvesta.py -- <the export's Larvesta.glb> <out.glb>
#
# Written to be grown from (volcarona.py imports it with sibling("larvesta")): the line's white fluff, red horn,
# blue eye and the way they are made are functions here; the calls that build Larvesta are under __main__.
#
# From the references: a great round ball of white fluff that is the head and the front of the body, its upper front
# a smooth white brow like a helmet coming down over a black face band; blue eyes at the top of the black, cut by the
# brow's edge; the fluff jagged below and at the back where it meets the body. Five red horns like a sun's rays
# springing from the fluff's rim behind the face: one up from the crown, two out at its upper sides, two out low at
# its lower sides, each wavy like a flame. Behind the ball a tan abdomen of a few ribbed segments, sloping down to a
# blunt end; six short dark pointed legs under the ball. (The pictures add the tan abdomen and the six legs to the
# task's text.) Bones: root, body (0, 0, 0.14), head (0, -0.04, 0.16), leg{s}, mid{s}, hind{s} at x 0.06, z 0.06.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, outward, srgb, inside, smoothstep, tri, unit, eye_look, args,
                       polygon)

WHITE, FACE, HORN, TAN, RIB, LEG = "#F2F1EE", "#25242A", "#D8413A", "#C99A5E", "#8C6638", "#3A3A44"
IRIS, PUPIL, GLINT = "#2E9BE6", "#0E3E76", "#FFFFFF"

C = np.array([0.0, -0.03, 0.15])             # the ball of fluff's middle
CR = np.array([0.128, 0.12, 0.128])
FC = np.array([0.0, -0.088, 0.112])          # the black face band's middle
FR = np.array([0.094, 0.068, 0.052])


def fluff(teeth=8, rows=3.0, amp=0.34, keep_front=True):
    """A deform of the unit sphere: tufts of fluff jagged round the lower half and the back, the brow left smooth."""
    def f(V):
        th = np.arctan2(V[:, 0], -V[:, 1])
        z = V[:, 2]
        a = np.clip(tri(th * teeth / (2 * math.pi)), 0, 1)
        b = np.clip(tri(z * rows + 0.25), 0, 1)
        amt = smoothstep(0.45, -0.05, z) + smoothstep(0.1, 0.7, V[:, 1]) * smoothstep(0.9, 0.3, z)
        amt = np.clip(amt, 0, 1)
        if keep_front:
            amt = amt * (1 - smoothstep(0.55, 0.8, -V[:, 1]) * smoothstep(-0.65, -0.35, z))
        return V * (1 + amp * (a * b) ** 1.2 * amt)[:, None]
    return f


MASK_TOP, MASK_LOW, MASK_WIDE = 0.168, 0.074, 1.05


def mask(P):
    """The black face band: round the ball's front, its top edge just over the eyes rising a little at the sides."""
    th = np.arctan2(P[:, 0], -(P[:, 1] - C[1]))
    front = -(P[:, 1] - C[1]) > 0.05
    top = MASK_TOP + 0.022 * np.abs(th) - 0.014 * np.cos(th * 3) * (np.abs(th) < 0.5)
    # a band with rounded ends: its height shrinks toward the sides as a rounded box does
    mid, half = (top + MASK_LOW) / 2, (top - MASK_LOW) / 2
    shape = (np.abs(th) / MASK_WIDE) ** 4 + (np.abs(P[:, 2] - mid) / half) ** 4
    return inside(shape - 1, 0.03) * front


def ball(k):
    k.add("ball", ell(C, CR, deform=fluff(), sub=5), WHITE, "head")
    # the smooth brow standing proud over the face band, so the black lies in its shade
    k.add("brow", ell(C + np.array([0, -0.035, 0.055]), (0.114, 0.1, 0.08)), WHITE, "head")
    k.paint(mask, FACE)


def abdomen(k):
    k.add("abdomen", ell((0, 0.12, 0.095), (0.082, 0.1, 0.078), rot=(-14, 0, 0)), TAN, "body")
    k.add("abdomenend", ell((0, 0.19, 0.07), (0.058, 0.06, 0.056), rot=(-20, 0, 0)), TAN, "body")

    def ribs(P):
        m = np.zeros(len(P))
        for y in (0.085, 0.13, 0.172, 0.208):
            m = np.maximum(m, inside(np.abs(P[:, 1] - y - 0.25 * (P[:, 2] - 0.09)) - 0.0028, 0.0018))
        return m
    k.paint(ribs, RIB, on=["abdomen"])


def legs(k):
    for bone, y, r in (("leg{s}", -0.085, 0.021), ("mid{s}", -0.01, 0.019), ("hind{s}", 0.065, 0.017)):
        k.add(bone.split("{")[0], outward(tube([(0.062, y, 0.045), (0.072, y - 0.006, 0.02), (0.076, y - 0.012, 0.006)],
                                               [r, r * 0.75, r * 0.3])),
              LEG, bone, fuse=False, mirror=True, surface="shell")


def flame(k, name, root, tip, wig, r0, bone, color=HORN, flat=0.45, axis=(0, 1, 0), mirror=False):
    """A horn like a ray of flame: a flattened tube waving across its length, wide at its root, to a point."""
    root, tip = np.array(root, float), np.array(tip, float)
    d = tip - root
    n = np.cross(d, axis); n /= np.linalg.norm(n)
    ts = np.linspace(0, 1, 6)
    pts = [root + d * t + n * wig * math.sin(t * 1.8 * math.pi) * t for t in ts]
    radii = [r0 * f for f in (1.0, 0.95, 0.82, 0.62, 0.38, 0.1)]
    k.add(name, outward(blade(pts, radii, axis, flat)), color, bone, fuse=False, mirror=mirror, surface="shell")


def horns(k):
    flame(k, "horntop", C + (0, 0.05, 0.1), (0.0, 0.1, 0.405), 0.022, 0.024, "head")
    flame(k, "hornup", C + (0.085, 0.05, 0.07), (0.19, 0.12, 0.31), 0.02, 0.022, "head", mirror=True)
    flame(k, "hornlow", C + (0.1, 0.05, -0.03), (0.205, 0.13, 0.075), -0.018, 0.02, "head", mirror=True)


# --- the eyes: blue, a dark pupil, a glint; their top cut by the brow, sloping down to the nose
CI, CP, CG, CF = srgb(IRIS), srgb(PUPIL), srgb(GLINT), srgb(FACE)
LINE = srgb("#8A8A96")                       # the closed eye's line, light on the black face


def eye_open(u, v):
    c = np.tile(CI, (len(u), 1))
    c[((u + 0.05) / 0.36) ** 2 + ((v + 0.05) / 0.62) ** 2 < 1] = CP
    c[((u - 0.2) / 0.22) ** 2 + ((v - 0.28) / 0.24) ** 2 < 1] = CG
    return c


_CUT = polygon([(-2, -2), (2, -2), (2, 0.62), (-2, 0.12)])


def EYE_CUT(th):
    """An oval eye with its top cut straight by the brow, lower toward the nose."""
    return min(1.0, _CUT(th))


def eye_frame(side, x=0.05, z=0.146):
    """On the ball's front, just under the mask's top edge, looking forward and a little out."""
    return np.array([side * x, -0.14, z]), unit(side * 0.38, -1, 0.06)


EYE_HALF = (0.02, 0.026)


def eyes(k, frame=eye_frame, half=EYE_HALF, cut=EYE_CUT, lid=CF, line=LINE):
    def lay(state):
        if state == "Fierce":
            def look(u, v):
                c = eye_open(u, v)
                c[v > 0.05 - 0.45 * u] = lid
                return c
        else:
            look = eye_look(state, eye_open, lid, line if state != "Open" else CP, line=0.16)
        for side in (1, -1):
            c, n = frame(side)
            col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
            out = cut if side == 1 else (lambda th, f=cut: f(math.pi - th))
            k.decal(c, n, half, col, eps=0.002, rings=20, segs=56, outline=out)
    return lay


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    ball(k)
    abdomen(k)
    legs(k)
    horns(k)
    k.build(voxel=0.0045, smooth=3, band=0.006)
    k.eye_states(eyes(k))
    k.export(out)
