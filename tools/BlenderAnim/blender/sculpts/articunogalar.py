# Galarian Articuno sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/articunogalar.py -- <the export's Articuno-Galar.glb> <out.glb>
# Articuno's kin on articuno.py's frame (its wing axes, fan of feathers, ribbon, legs' way and eyes cut along a brow): a
# slender lilac bird hovering upright, its face a black mask with narrow glowing cyan eyes, a dark violet hood swept up
# and back off the crown with a curl over the forehead, a black feather curling out and down beside each cheek, a small
# dark hooked beak, the black running down the front of the neck, great lilac wings raised from the shoulders and
# hanging round the body like a cloak, their long feathers tipped white, a ruff of white-tipped feathers over the
# breast, long black legs under lilac thighs with the toes hanging together, and one long violet ribbon of a tail that
# loops out to the right and falls away below the feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, hull, srgb, inside, smoothstep, unit, eye_look, sibling, args

a = sibling("articuno")
sp = a.sp

LILAC, LILAC2, WHITE = "#BBA6E0", "#AC96D8", "#F4F0FA"
HOOD, TAIL, STRIPE = "#5A47A6", "#7A64C8", "#4D3C98"
BLACK, BEAK, CLAW = "#2A2532", "#3A3046", "#5E5468"
CYAN, GLINT, LIDINK = "#46D8F2", "#E4FCFF", "#8C7CB0"

H = np.array([0, -0.074, 0.842])          # the head's centre, over the head's joint (0, -0.04, 0.787)
HR = np.array([0.082, 0.088, 0.083])      # a tenth larger than Articuno's: the face must read small

ELEV, SWEEP = 50, 4
SPAN = 0.27
ARM = [(0.0, -0.03), (SPAN * 0.5, -0.036), (SPAN - 0.01, -0.026), (SPAN + 0.02, 0.01), (SPAN * 0.75, 0.07),
       (0.12, 0.085), (0.0, 0.06)]
# (along the span, angle off the chord toward the span, length, width): long feathers hanging from the raised arm
FEATHERS = [(a0, ang, L, 0.062) for a0, ang, L in zip(np.linspace(0.03, SPAN, 8), np.linspace(-50, -22, 8),
                                                      np.linspace(0.24, 0.36, 8))]
TIP = 0.34          # the share of each feather's length that is white


def wing(k, root, axes, thick=0.024):
    """articuno.py's fan of feathers, each with its end white: a plate of the tip thicker than the feather, so the white
    shows on both faces."""
    ex, ez = axes
    n = np.cross(ex, ez)
    k.add("wingarm", slab(ARM, root, ex, ez, thick + 0.006), LILAC, "wing{s}", fuse=False, mirror=True)
    for j, (a0, ang, L, w) in enumerate(FEATHERS):
        off = root + n * (0.004 if j % 2 else -0.004)
        k.add(f"feather{j}", slab(a.feather_outline(a0, 0.02, ang, L, w), off, ex, ez, thick),
              (LILAC, LILAC2)[j % 2], "wing{s}", fuse=False, mirror=True)
        t = math.radians(ang)
        d = np.array([math.sin(t), math.cos(t)])
        r0 = np.array([a0, 0.02]) + d * L * (1 - TIP)
        k.add(f"tip{j}", slab(a.feather_outline(r0[0], r0[1], ang, L * TIP, w * 0.98), off, ex, ez, thick + 0.01),
              WHITE, "wing{s}", fuse=False, mirror=True)
    k.add("shoulder", ell(root + ex * 0.025, (0.04, 0.046, 0.04)), LILAC, "wing{s}", mirror=True)


def tipped(base, tip, w, t, frac=0.55, up=(0, -1, 0)):
    """sp.spike, and a white hull over its end from frac along it: a lilac feather tipped white."""
    base = np.array(base, float); tip = np.array(tip, float)
    d = unit(*(tip - base))
    q = np.cross(d, up); q /= np.linalg.norm(q)
    b = np.cross(q, d)
    mid = base + (tip - base) * frac
    k_ = 1 - (frac - 0.35) / 0.65
    end = hull([mid + q * w * k_ * 1.08, mid - q * w * k_ * 1.08, mid + b * t * 0.8 * k_ * 1.25,
                mid - b * t * 0.8 * k_ * 1.25, tip + d * 0.004])
    return sp.spike(base, tip, w, t, up=up), end


def galar(src, out):
    k = Kit(src, coat="fur")

    # A slender upright trunk, a short thick neck to the head
    k.add("body", ell((0, 0.0, 0.62), (0.108, 0.112, 0.152), rot=(-6, 0, 0)), LILAC, "body")
    k.add("breast", ell((0, -0.038, 0.62), (0.095, 0.082, 0.13)), LILAC, "body")
    k.add("neck", tube([(0, -0.015, 0.7), (0, -0.04, 0.77), H + (0, 0.012, -0.035)], [0.068, 0.06, 0.058]), LILAC,
          "head")
    k.add("head", ell(H, HR), BLACK, "head")
    # the black of the face runs down the front of the neck to the breast
    k.paint(lambda P: inside(np.abs(P[:, 0]) - (0.034 + 0.18 * np.clip(P[:, 2] - 0.7, 0, 1)), 0.005)
            * inside(P[:, 1] + 0.035 - 0.25 * np.clip(0.8 - P[:, 2], 0, 1), 0.005) * (P[:, 2] > 0.645), BLACK,
            on=["neck", "breast", "body"])

    # The hood: a dark violet cap swept up and back off the crown, its front over the brow, a curl on the forehead
    k.add("hood", ell(H + (0, 0.04, 0.045), (0.086, 0.11, 0.055), rot=(-30, 0, 0)), HOOD, "head")
    k.add("hoodtip", tube([H + (0, 0.04, 0.075), H + (0, 0.11, 0.1), H + (0, 0.17, 0.1), H + (0, 0.21, 0.078)],
                          [0.05, 0.04, 0.03, 0.018]), HOOD, "head")
    # the face's black kept below a crisp line over the eyes, the hood's violet above it
    hz = lambda P: H[2] + 0.042 - 0.45 * (P[:, 1] - (H[1] - 0.06)) + 2.0 * P[:, 0] ** 2
    k.paint(lambda P: inside(hz(P) - P[:, 2], 0.0035) * (P[:, 2] > H[2] - 0.02) * (P[:, 1] < H[1] + 0.2)
            * inside(np.abs(P[:, 0]) - 0.1, 0.002), HOOD, on=["head", "hood"])
    k.paint(lambda P: inside(P[:, 2] - hz(P), 0.0035) * (P[:, 2] > H[2] - 0.12) * inside(np.abs(P[:, 0]) - 0.1, 0.002),
            BLACK, on=["head", "hood"])
    curl = [H + (0, -0.06, 0.05), H + (0, -0.09, 0.055), H + (0, -0.1, 0.035), H + (0, -0.085, 0.025)]
    k.add("curl", tube(curl, [0.018, 0.017, 0.015, 0.013], seg=20), HOOD, "head", fuse=False)

    # A black feather curling out and down beside each cheek, under the hood
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        c = H + np.array([0.058, -0.02, -0.012])
        pts = [c * m, (c + (0.04, 0.01, -0.012)) * m, (c + (0.07, 0.03, -0.045)) * m, (c + (0.068, 0.03, -0.075)) * m]
        k.add(f"brow{'R' if s > 0 else 'L'}", tube(pts, [0.02, 0.018, 0.015, 0.011], seg=20), BLACK, "head",
              fuse=False)

    # A small dark beak, hooked down
    B = H + (0, -0.07, -0.012)
    k.add("beak", tube([B, B + (0, -0.036, -0.008), B + (0, -0.058, -0.028)], [0.021, 0.013, 0.004], seg=20), BEAK,
          "head", fuse=False, surface="shell")

    # A ruff of lilac feathers over the breast, tipped white, under the black of the throat
    for i, ang in enumerate(np.linspace(-62, 62, 7)):
        t = math.radians(ang)
        base = np.array([math.sin(t) * 0.09, -0.08 * math.cos(t) - 0.008, 0.7 - 0.012 * abs(ang) / 60])
        tip = base + np.array([math.sin(t) * 0.045, -0.045 * math.cos(t), -0.15 + 0.02 * abs(ang) / 60])
        f, w_ = tipped(base, tip, 0.072, 0.02, up=(math.sin(t), -math.cos(t), 0))
        k.add(f"ruff{i}", f, LILAC2 if i % 2 else LILAC, "body", fuse=False)
        k.add(f"rufftip{i}", w_, WHITE, "body", fuse=False)

    # The great wings: raised from the shoulders and hanging round the body like a cloak, long feathers tipped white
    wing(k, np.array([0.08, 0.025, 0.725]), a.wing_axes(ELEV, SWEEP, (0.2, -0.05, -1.0)))

    # One long ribbon of a tail: back from the rump, out to the right (+X) in a loop and falling away below the feet;
    # a deeper stripe down its middle, thicker so it shows on both faces
    path = [(0, 0.07, 0.55), (0.06, 0.18, 0.5), (0.18, 0.25, 0.44), (0.3, 0.22, 0.37), (0.35, 0.1, 0.33),
            (0.32, -0.01, 0.29), (0.22, -0.03, 0.25), (0.14, 0.04, 0.2), (0.1, 0.15, 0.14), (0.06, 0.24, 0.08),
            (-0.02, 0.29, 0.04)]
    width = lambda u: (0.11 - 0.03 * u) * (1 - 0.55 * smoothstep(0.88, 1.0, u))
    twist = lambda u: 0.55 * u
    k.add("tail", a.ribbon(path, width, 0.02, twist=twist, nu=120), TAIL, "tail", fuse=False)
    k.add("tailstripe", a.ribbon(path, lambda u: 0.03 - 0.008 * u, 0.028, twist=twist, u0=0.06, nu=120), STRIPE,
          "tail", fuse=False)
    k.add("rump", ell((0, 0.07, 0.55), (0.06, 0.05, 0.05)), LILAC, "tail")

    # Long black legs under lilac thighs, the toes hanging together with dark claws
    k.add("thigh", ell((0.052, -0.012, 0.495), (0.05, 0.054, 0.062)), LILAC, "leg{s}", mirror=True)
    for s, n in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        top, ank = np.array([0.054, -0.01, 0.47]) * m, np.array([0.058, 0.0, 0.3]) * m
        k.add(f"shin{n}", tube([top, (top + ank) / 2 + (0, -0.006, 0), ank], [0.026, 0.022, 0.02]), BLACK,
              f"leg{n}", fuse=False, surface="scales")
        for j, (dx, dy) in enumerate(((-0.4, -0.55), (0.0, -0.65), (0.4, -0.55), (0.0, 0.7))):
            d = unit(dx * s, dy, -1.0 if dy < 0 else -0.6)
            L = 0.07 if dy < 0 else 0.04
            tip = ank + d * L
            k.add(f"toe{n}{j}", tube([ank, (ank + tip) / 2, tip], [0.016, 0.014, 0.012]), BLACK, f"leg{n}",
                  fuse=False, surface="scales")
            k.add(f"claw{n}{j}", tube([tip, tip + unit(*(d + (0, 0.5 if dy < 0 else -0.5, -0.4))) * 0.025],
                                      [0.009, 0.002]), CLAW, f"leg{n}", fuse=False, surface="shell")

    # Eyes: narrow glowing cyan slits across the black mask, slanting down toward the beak
    de = unit(0.55, -0.8, 0.14)
    E = a.eye_at(H, HR, de)
    def look(u, v):
        c = np.tile(srgb(CYAN), (len(u), 1))
        c[((u + 0.05) / 0.5) ** 2 + ((v + 0.05) / 0.32) ** 2 < 1] = srgb(GLINT)
        return c
    def eyes(state):
        sp.lay_eyes(k, state, E, de, (0.036, 0.02), look, lambda u: 0.45 + 0.25 * u, lambda u: 0.0 + 0.55 * u,
                    srgb(BLACK), srgb(LIDINK))

    k.build(voxel=0.0068, band=0.006)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    galar(*args())
