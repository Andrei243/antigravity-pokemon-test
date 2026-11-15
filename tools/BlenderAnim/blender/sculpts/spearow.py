# Spearow sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/spearow.py -- <the export's Spearow.glb> <out.glb>
# A small scruffy bird: a big brown head with a shock of ragged feathers sweeping back off the crown and cheeks, the
# brown coming down over the cream breast in points, a short hooked pink beak, a fierce eye under a brow, rose-red
# wings folded at the sides with pale tips, a short fan of brown tail feathers and long pink legs with white claws.
# Fearow (fearow.py) grows from its functions.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, hull, srgb, inside, tri, unit, eye_look, outward, args

BROWN, DARK, CREAM, RED, PALE = "#A86A3E", "#7A4A2C", "#F1DEB8", "#D85F68", "#EFA2A8"
BEAK, LEG, CLAW = "#ECB4AA", "#E0A196", "#F6F4F0"
WHITE, PUPIL, INK = "#FAFAF6", "#16100C", "#2A1A12"


# --- the line's own parts, shared with Fearow -------------------------------------------------------------------------
def spike(base, tip, w, t, up=(0, 0, 1)):
    """A solid feather point: a flat-faced kite from a base w wide and t thick to its tip (a loose piece)."""
    base = np.array(base, float); tip = np.array(tip, float)
    d = unit(*(tip - base))
    a = np.cross(d, up)
    if np.linalg.norm(a) < 1e-3:
        a = np.cross(d, (0, 1.0, 0))
    a /= np.linalg.norm(a)
    b = np.cross(a, d)
    mid = base + (tip - base) * 0.35
    return hull([base - d * t, base + a * w * 0.6, base - a * w * 0.6, mid + a * w, mid - a * w, base + b * t, base - b * t,
                 mid + b * t * 0.8, mid - b * t * 0.8, tip])


def feather(k, name, base, tip, w, t, color, bone, up=(0, 0, 1), mirror=False):
    k.add(name, spike(base, tip, w, t, up), color, bone, fuse=False, mirror=mirror)


def hooked_beak(k, pts, radii, color, bone="head"):
    k.add("beak", tube(pts, radii, seg=20), color, bone, fuse=False, surface="shell")


def foot(k, side, top, base, length, leg, claw, r=0.016, bone_fmt="leg{s}"):
    """A long bare leg, three toes forward and one back, white claws (laid by hand a side)."""
    s, n = side
    x = np.array([s, 1, 1.0])
    top = np.array(top, float) * x; base = np.array(base, float) * x
    bone = bone_fmt.format(s=n)
    k.add(f"leg{n}", tube([top, (top + base) / 2 + (0, -0.004, 0), base + (0, 0, 0.01)], [r * 1.15, r, r * 0.95]), leg, bone,
          fuse=False, surface="scales")
    for j, ang in enumerate((-30, 0, 30, 180)):
        rr = math.radians(ang)
        d = np.array([math.sin(rr) * s, -math.cos(rr), 0])
        tip = base + d * (length if ang != 180 else length * 0.55) + (0, 0, -0.008)
        k.add(f"toe{n}{j}", tube([base, (base + tip) / 2 + (0, 0, 0.004), tip], [r * 0.95, r * 0.8, r * 0.7]), leg, bone,
              fuse=False, surface="scales")
        k.add(f"claw{n}{j}", tube([tip, tip + (d + (0, 0, -0.6)) * length * 0.35], [r * 0.6, r * 0.1]), claw, bone,
              fuse=False, surface="shell")


def eye_outline(top, side, n=72):
    """The eye's outline in the patch's u, v (u outward): its unit circle cut along the top by the brow's line."""
    pts = []
    for th in np.linspace(0, 2 * math.pi, n, endpoint=False):
        u, v = math.cos(th), math.sin(th)
        if v > top(u):
            lo, hi = 0.0, 1.0
            for _ in range(30):
                t = (lo + hi) / 2
                if t * v > top(t * u):
                    hi = t
                else:
                    lo = t
            u, v = lo * u, lo * v
        pts.append((side * u, v))
    pts = np.array(pts)
    a = np.arctan2(pts[:, 1], pts[:, 0]); d = np.hypot(pts[:, 0], pts[:, 1]); o = np.argsort(a)
    return lambda th: float(np.interp((th + math.pi) % (2 * math.pi) - math.pi, a[o], d[o], period=2 * math.pi))


def lay_eyes(k, state, E, de, half, open_look, top, fierce_top, lid, ink):
    """Both eyes laid by hand, cut along the top by a brow slanting down toward the beak; u outward on both."""
    for side in (1, -1):
        m = np.array([side, 1, 1.0])
        if state in ("Open", "Fierce"):
            t, look = (top if state == "Open" else fierce_top), open_look
        else:
            t, look = (lambda u: top(u) - 0.04), eye_look(state, open_look, lid, ink, line=0.16)
        col = look if side > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
        k.decal(E * m, de * m, half, col, eps=0.006, outline=eye_outline(t, side), rings=20, segs=56)


def bird_eye(white, pupil):
    def look(u, v):
        c = np.tile(white, (len(u), 1))
        c[((u + 0.22) / 0.52) ** 2 + ((v + 0.12) / 0.62) ** 2 < 1] = pupil
        c[((u + 0.12) / 0.14) ** 2 + ((v - 0.06) / 0.14) ** 2 < 1] = white
        return c
    return look


# --- Spearow ---------------------------------------------------------------------------------------------------------
H = np.array([0, -0.08, 0.455])           # the head's centre
HR = np.array([0.122, 0.118, 0.118])


def spearow(src, out):
    k = Kit(src, coat="fur")

    k.add("body", ell((0, 0.02, 0.27), (0.13, 0.135, 0.14)), BROWN, "body")
    k.add("head", ell(H, HR), BROWN, "head")
    k.add("nape", ell((0, -0.02, 0.38), (0.11, 0.1, 0.08)), BROWN, "head")
    k.add("thigh", ell((0.06, 0.0, 0.17), (0.045, 0.05, 0.05)), CREAM, "leg{s}", mirror=True)
    # The cream breast and belly, the brown of the head coming down over its top in ragged points
    def breast(P):
        th = np.arctan2(P[:, 0], -(P[:, 1] - 0.03))
        line = 0.335 + 0.03 * tri(th * 9 / (2 * math.pi))
        return inside(P[:, 2] - line, 0.003) * inside(np.abs(th) - 1.75, 0.02)
    k.paint(breast, CREAM, on=["body", "thigh"])

    # The shock of ragged feathers: points off the crown, the back of the head and the cheeks, sweeping back
    # long and thick, so the head's outline is spiky from every side and wider than the body
    C = H
    MANE = 1.6
    for i, (d, L, w) in enumerate([((0.0, 0.4, 1.0), 0.12, 0.05), ((0.0, 0.85, 0.65), 0.15, 0.055),
                                   ((0.0, 1.0, 0.1), 0.14, 0.055), ((0.0, 0.9, -0.35), 0.1, 0.05),
                                   ((0.35, 0.6, 0.75), 0.12, 0.05), ((-0.35, 0.6, 0.75), 0.12, 0.05),
                                   ((0.5, 0.8, 0.35), 0.12, 0.05), ((-0.5, 0.8, 0.35), 0.12, 0.05),
                                   ((0.45, 0.85, -0.2), 0.1, 0.045), ((-0.45, 0.85, -0.2), 0.1, 0.045),
                                   ((0.0, -0.05, 1.0), 0.07, 0.045), ((0.15, 0.1, 1.0), 0.08, 0.045)]):
        dd = unit(*d)
        base = C + dd * np.array([0.1, 0.1, 0.1])
        feather(k, f"crown{i}", base, base + dd * L * MANE, w * 1.4, 0.024, BROWN, "head")
    # the ruff at the cheeks and throat, standing out sideways and back
    for i, (d, L) in enumerate([((1, 0.45, 0.1), 0.08), ((1, 0.35, -0.3), 0.085), ((0.8, 0.2, -0.65), 0.075),
                                ((0.4, -0.1, -0.95), 0.06)]):
        dd = unit(*d)
        base = H + dd * np.array([0.1, 0.1, 0.1])
        feather(k, f"ruff{i}", base, base + dd * L * MANE, 0.055, 0.02, BROWN, "head", mirror=True)

    # The short hooked beak, and its lower half under the open mouth
    hooked_beak(k, [(0, -0.17, 0.445), (0, -0.215, 0.44), (0, -0.25, 0.425), (0, -0.262, 0.4), (0, -0.255, 0.383)],
                [0.042, 0.032, 0.021, 0.011, 0.003], BEAK)
    k.add("jaw", ell((0, -0.205, 0.398), (0.026, 0.035, 0.012), rot=(12, 0, 0)), BEAK, "head", fuse=False, surface="shell")

    # Wings folded at the sides, rose-red, pale feather tips at their backs
    # as large as the body: grown by WS about the shoulder
    WS, SH = 1.3, np.array([0.108, -0.05, 0.35])
    WP = [SH + (np.array(p) - SH) * WS for p in [(0.108, -0.05, 0.35), (0.13, 0.03, 0.315), (0.127, 0.12, 0.26),
                                                 (0.1, 0.19, 0.21)]]
    k.add("wing", outward(blade(WP, [0.06 * WS, 0.078 * WS, 0.064 * WS, 0.03 * WS], (1, 0, 0), 0.38)), RED, "wing{s}",
          fuse=False, mirror=True)
    # the pale ends of the long feathers, fanned off the wing's back end, clear of it
    for i, (z, L) in enumerate([(0.25, 0.08), (0.225, 0.09), (0.2, 0.08)]):
        b = SH + (np.array([0.112, 0.17, z]) - SH) * WS
        feather(k, f"wingtip{i}", b, b + unit(-0.2, 0.75, -0.5 - 0.15 * i) * L * WS, 0.05, 0.022, PALE, "wing{s}",
                up=(1, 0, 0), mirror=True)

    # A short fan of brown tail feathers, the middle one dark
    for i, a in enumerate((-0.45, 0, 0.45)):
        b = np.array([a * 0.05, 0.12, 0.25])
        d = unit(a * 0.6, 1, -0.3)
        feather(k, f"tail{i}", b, b + d * 0.19, 0.06, 0.013, BROWN if i != 1 else DARK, "tail", up=(0, 0, 1))

    # Long pink legs, white claws
    for side in ((1, "R"), (-1, "L")):
        foot(k, side, (0.06, -0.01, 0.17), (0.065, -0.03, 0.02), 0.055, LEG, CLAW)

    # The eyes: white with a big black pupil toward the beak, cut by a brow slanting down to it
    look = bird_eye(srgb(WHITE), srgb(PUPIL))
    de = unit(0.55, -0.8, 0.18)
    E = H + de / np.linalg.norm(de / HR)
    def eyes(state):
        lay_eyes(k, state, E, de, (0.031, 0.031), look, lambda u: 0.14 + 0.6 * u, lambda u: -0.1 + 0.65 * u,
                 srgb(BROWN), srgb(INK))

    k.build(voxel=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    spearow(*args())
