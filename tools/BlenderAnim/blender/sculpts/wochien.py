# Wo-Chien sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/wochien.py -- <the export's Wo-Chien.glb> <out.glb>
#
# From the references: a snail of ruin. A broad low mound of dark moss, ragged and tufted at its foot, a green leaf
# lying on its back; at its front a tall cone of a head heaped with leaves in shingled rows, green and tan by turns,
# the lowest the largest, hanging over the mound like a skirt; at its top a crown of a tan bow-shaped leaf and green
# leaves, under it two narrow slanting orange eyes with a dark streak of a pupil, looking out of a dark shadow; two
# pale grey-green vines rising from the crown like antlers, each with a branch out to the side and a hooked top, small
# green leaves at their ends. On its back, leaning back, a cone of wooden tablets in a spiral of three tiers, the gaps
# between them dark, a last tall tablet at the top with a dark mark on it.
# The skeleton: body (0, 0.02, 0.16), head (-0.03, -0.12, 0.22), earL (-0.08, -0.09, 0.47), earR (0.02, -0.09, 0.47).
# The head's joints stand 0.03 to the model's left, so the head and its antlers are laid about x = -0.03 by hand (a
# patch or part mirrored by the kit would mirror about x = 0).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, lathe, outward, srgb, inside, unit, polygon, eye_look, args

MOSS, MOSS2, MOSSDK = "#3B4334", "#4C5641", "#2A3026"
CORE = "#6E7444"
GREEN, GREEN2, TAN, TAN2 = "#6F9A50", "#5E8644", "#CBAA6C", "#B8955A"
VINE = "#A9B5A2"
WOOD, WOOD2, WOODDK, GAP = "#9A7656", "#86654A", "#5A4030", "#3A2C22"
SOCKET, EYE_O, EYE_Y, SLIT = "#2A2220", "#F08C1E", "#FFC860", "#5A1A10"

HX = -0.03                     # the head's middle line
HY = -0.13


def radius_at(z):
    """The head cone's core radius at a height."""
    return float(np.interp(z, [0.08, 0.2, 0.32, 0.42, 0.47], [0.165, 0.14, 0.112, 0.09, 0.075]))


def on_cone(phi, z, lift=0.0):
    """A point on the head cone (phi 0 at the front, + toward +X) and its outward horizontal normal."""
    r = radius_at(z) + lift
    n = np.array([math.sin(phi), -math.cos(phi), 0.0])
    return np.array([HX, HY + 0.0, z]) + n * r, n


def leaf_outline(L, w):
    return [(0, 0), (w * 0.45, L * 0.1), (w, L * 0.32), (w * 0.7, L * 0.48), (w * 0.88, L * 0.62), (w * 0.38, L * 0.84),
            (0, L), (-w * 0.38, L * 0.84), (-w * 0.88, L * 0.62), (-w * 0.7, L * 0.48), (-w, L * 0.32),
            (-w * 0.45, L * 0.1)]


def leaf(k, name, root, ex, d, L, w, colour, bone, thick=0.014):
    """A loose leaf plate from its root along d, its face across ex."""
    ex = np.array(ex, float); d = np.array(d, float)
    k.add(name, slab(leaf_outline(L, w), root, ex, d, thick), colour, bone, fuse=False, surface="leaf")


def mound(k):
    def flat_bottom(V):
        V = V.copy()
        V[:, 2] = np.maximum(V[:, 2], -0.24)
        return V
    k.add("mound", ell((0, 0.05, 0.05), (0.255, 0.3, 0.21), deform=flat_bottom), MOSS, "body")
    k.add("hump", ell((0.01, 0.15, 0.16), (0.19, 0.17, 0.13)), MOSS, "body")
    # The ragged foot: tufts all round its lower edge, out and down
    for i in range(28):
        a = 2 * math.pi * i / 28 + 0.11
        n = np.array([math.sin(a), -math.cos(a), 0.0])
        rx, ry = 0.25, 0.295
        root = np.array([n[0] * rx * 0.93, 0.05 + n[1] * ry * 0.93, 0.05])
        tip = root + n * (0.06 + 0.02 * (i % 3 == 0)) + np.array([0, 0, -0.045])
        k.add(f"tuft{i}", tube([root, (root + tip) / 2, tip], [0.032, 0.022, 0.01], seg=12, per=4),
              MOSSDK if i % 2 else MOSS, "body")
    # Lighter patches of moss on its back
    for c, r in [((0.12, 0.08, 0.2), 0.05), ((-0.13, 0.02, 0.19), 0.045), ((-0.04, 0.22, 0.2), 0.05)]:
        c = np.array(c)
        k.paint(lambda P, c=c, r=r: inside(np.linalg.norm(P - c, axis=1) - r, 0.012), MOSS2, on=["mound", "hump"])
    # A green leaf lying on the mound's side, toward the shell
    root = np.array([0.2, 0.02, 0.16])
    leaf(k, "moundleaf", root, unit(0.3, -0.95, 0.0), unit(0.55, 0.15, -0.8), 0.09, 0.045, GREEN, "body", thick=0.012)


def head(k):
    # The core cone, olive, and its dome
    zs = [0.16, 0.24, 0.32, 0.42, 0.47]
    k.add("cone", tube([(HX, HY, z) for z in zs], [radius_at(z) for z in zs], seg=24), CORE, "head")
    k.add("dome", ell((HX, HY - 0.005, 0.47), (0.085, 0.08, 0.06)), CORE, "head")
    k.add("brow", ell((HX, HY - 0.045, 0.455), (0.08, 0.04, 0.035)), CORE, "head")
    # The dark shadow the eyes look out of
    k.paint(lambda P: inside(np.hypot((P[:, 0] - HX) / 0.09, (P[:, 2] - 0.445) / 0.035) - 1, 0.01)
            * inside(P[:, 1] - (HY - 0.03), 0.004), SOCKET, on=["dome", "brow", "cone"])
    # Rows of shingled leaves, green and tan by turns, the lowest the largest
    # (height, length, width, count round the whole head, half-width of the gap left at the front for the eyes)
    rows = [(0.475, 0.08, 0.04, 9, 0.75), (0.42, 0.09, 0.045, 10, 0.0), (0.355, 0.105, 0.052, 11, 0.0),
            (0.285, 0.12, 0.06, 12, 0.0), (0.215, 0.135, 0.066, 13, 0.0), (0.145, 0.145, 0.072, 14, 0.0)]
    for r, (z, L, w, n, gap) in enumerate(rows):
        for i in range(n):
            phi = -math.pi + 2 * math.pi * (i + 0.5 + 0.5 * (r % 2)) / n
            phi = (phi + math.pi) % (2 * math.pi) - math.pi
            if abs(phi) < gap:
                continue
            root, nrm = on_cone(phi, z, 0.006)
            ex = np.cross((0, 0, 1.0), nrm)
            d = unit(*(np.array([0, 0, -1.0]) * 0.86 + nrm * 0.5))
            col = (GREEN if (i + r) % 2 else TAN) if r % 2 == 0 else (TAN2 if (i + r) % 2 else GREEN2)
            leaf(k, f"leaf{r}_{i}", root, ex, d, L * (0.9 + 0.2 * ((i * 7) % 3) / 2), w, col, "head")
    # The crown: a tan bow of a leaf at the top front, green leaves fanned up behind it and over each eye
    top = np.array([HX, HY - 0.06, 0.505])
    for s in (1, -1):
        leaf(k, f"bow{'R' if s > 0 else 'L'}", top, unit(0.2, -1, 0.1) * np.array([1, 1, 1]),
             unit(0.85 * s, -0.15, 0.55), 0.055, 0.03, TAN, "head", thick=0.016)
        leaf(k, f"crest{'R' if s > 0 else 'L'}", np.array([HX + 0.04 * s, HY - 0.035, 0.49]), unit(1, -0.3 * s, 0),
             unit(0.55 * s, -0.25, 0.8), 0.06, 0.03, GREEN, "head", thick=0.016)
    leaf(k, "crestM", np.array([HX, HY - 0.01, 0.51]), unit(1, 0, 0), unit(0, -0.35, 0.94), 0.055, 0.03, GREEN2,
         "head", thick=0.016)


def antlers(k):
    for s, bone in ((1, "earR"), (-1, "earL")):
        b = np.array([HX + 0.048 * s, HY + 0.035, 0.47])
        stem = [b, b + (0.012 * s, 0.002, 0.07), b + (0.026 * s, 0.006, 0.13), b + (0.05 * s, 0.01, 0.175),
                b + (0.088 * s, 0.012, 0.19), b + (0.115 * s, 0.012, 0.168), b + (0.118 * s, 0.01, 0.14)]
        k.add(f"antler{'R' if s > 0 else 'L'}", outward(tube(stem, [0.015, 0.0145, 0.0135, 0.0125, 0.012, 0.0115, 0.011],
                                                          seg=14, per=6)), VINE, bone, fuse=False, surface="shell")
        j = b + (0.016 * s, 0.003, 0.09)
        branch = [j, j + (0.035 * s, 0.0, 0.025), j + (0.07 * s, -0.004, 0.02), j + (0.085 * s, -0.006, -0.008)]
        k.add(f"branch{'R' if s > 0 else 'L'}", outward(tube(branch, [0.012, 0.0115, 0.011, 0.0105], seg=14, per=6)),
              VINE, bone, fuse=False, surface="shell")
        # small green leaves at the ends
        leaf(k, f"tipleaf{'R' if s > 0 else 'L'}", stem[-1] + (0.0, 0, -0.005), unit(0, 1, 0),
             unit(0.3 * s, -0.4, -0.85), 0.04, 0.02, GREEN, bone, thick=0.012)
        leaf(k, f"brleaf{'R' if s > 0 else 'L'}", branch[-1] + (0.0, 0, -0.004), unit(0.2, 1, 0),
             unit(0.5 * s, -0.6, -0.6), 0.038, 0.019, GREEN, bone, thick=0.012)


def shell(k):
    """The cone of wooden tablets, leaning back, in a spiral of three tiers over a dark core."""
    foot = np.array([0.025, 0.15, 0.13])
    top = np.array([0.04, 0.3, 0.5])
    ax = unit(*(top - foot))
    L = np.linalg.norm(top - foot)
    a1 = np.cross(ax, (0, 0, 1.0)); a1 /= np.linalg.norm(a1)
    a2 = np.cross(ax, a1)
    def P(t, phi, r):
        return foot + ax * L * t + r * (math.cos(phi) * a1 + math.sin(phi) * a2)
    def rad(t):
        return 0.168 - 0.1 * t
    # the dark core showing in the gaps
    k.add("shellcore", lathe(foot, ax, [(0, -0.02), (0.15, -0.02), (0.15, L * 0.3), (0.105, L * 0.62), (0.075, L * 0.84),
                                         (0.05, L * 0.86), (0, L * 0.87)], seg=32), GAP, "body", fuse=False,
          surface="shell")
    tiers = [(0.0, 0.3, 13), (0.34, 0.6, 12), (0.64, 0.84, 10)]
    for ti, (t0, t1, n) in enumerate(tiers):
        for i in range(n):
            f0 = 2 * math.pi * i / n + 0.025
            f1 = 2 * math.pi * (i + 1) / n - 0.025
            rise = 0.05 * i / n          # the spiral: each tablet a little higher round the tier
            pts = []
            for t in (t0 + rise, t1 + rise):
                ro = rad(t) + 0.012
                ri = rad(t) - 0.03
                for f in (f0, (f0 + f1) / 2, f1):
                    pts.append(P(t, f, ro + (0.006 if f == (f0 + f1) / 2 else 0)))
                    pts.append(P(t, f, ri))
            k.add(f"tablet{ti}_{i}", hull(pts), WOOD if (i + ti) % 2 else WOOD2, "body", fuse=False, surface="shell")
    # a wooden lid over the top tier
    k.add("lid", lathe(P(0.9, 0, 0), ax, [(0, -0.012), (rad(0.86) + 0.004, -0.012), (rad(0.86) - 0.004, 0.006),
                                                    (0, 0.012)], seg=32), WOOD2, "body", fuse=False, surface="shell")
    # the last tall tablet at the top, and its mark
    t0, t1 = 0.86, 1.06
    f0, f1 = -1.15, -0.2
    pts = []
    for t in (t0, t1):
        for f in (f0, (f0 + f1) / 2, f1):
            pts.append(P(t, f, 0.075))
            pts.append(P(t, f, 0.045))
    k.add("toptablet", hull(pts), WOOD, "body", fuse=False, surface="shell")
    mid = P(0.97, (f0 + f1) / 2, 0.079)
    n = unit(*(mid - (foot + ax * L * 0.97)))
    side = np.cross(ax, n)
    for j, (a, b) in enumerate([((-0.012, 0.03), (0.01, 0.0)), ((0.01, 0.0), (-0.008, -0.03))]):
        p0 = mid + side * a[0] + ax * a[1]
        p1 = mid + side * b[0] + ax * b[1]
        q = [p0 + n * 0.0, p1 + n * 0.0, p0 + n * 0.006 + side * 0.006, p1 + n * 0.006 + side * 0.006,
             p0 - n * 0.006, p1 - n * 0.006]
        k.add(f"mark{j}", hull(q), WOODDK, "body", fuse=False, surface="shell")
    # a few tablets lying round the mound's lower back, the spiral's start
    for i in range(6):
        a = 1.25 + 0.33 * i
        c = np.array([0.24 * math.sin(a), 0.05 + 0.29 * -math.cos(a), 0.07])
        n = unit(math.sin(a), -math.cos(a) * 0.29 / 0.24, 0.35)
        t = unit(math.cos(a), math.sin(a), 0)
        up = np.cross(n, t)
        pts = [c + t * dx + up * dz + n * dn for dx in (-0.033, 0.033) for dz in (-0.045, 0.045) for dn in (-0.012, 0.016)]
        k.add(f"lowtablet{i}", hull(pts), WOOD2 if i % 2 else WOOD, "body", fuse=False, surface="shell")


# --- the eyes: narrow orange almonds slanting up outward, a dark streak of a pupil, in the dark shadow --------------
CO, CY, CS, CK = srgb(EYE_O), srgb(EYE_Y), srgb(SLIT), srgb(SOCKET)
OUT = [(-1, -0.3), (-0.6, 0.15), (0.1, 0.55), (0.75, 0.85), (1.0, 0.55), (0.6, -0.2), (-0.1, -0.6), (-0.7, -0.62)]


def eye_open(u, v):
    c = np.tile(CO, (len(u), 1))
    c[(v > 0.15 + 0.45 * u) & (u < 0.3)] = CY
    c[np.abs(v - (0.05 + 0.35 * u)) < 0.12] = CS
    return c


def eyes(k, state):
    look = eye_look(state, eye_open, CK, CO, line=0.16)
    for s in (1, -1):
        n = unit(0.5 * s, -1.0, 0.15)
        c = np.array([HX + 0.038 * s, HY - 0.085, 0.447])
        outline = polygon([(x * s, y) for x, y in OUT]) if s > 0 else polygon([(-x, y) for x, y in OUT])
        f = look if s > 0 else (lambda u, v, look=look: look(-u, v))
        # u runs toward +X on both patches: the left one's colour function and outline are flipped
        k.decal(c, n, (0.032, 0.021), f, eps=0.003, outline=polygon([(x if s > 0 else -x, y) for x, y in OUT]),
                rings=20, segs=56)


def wochien(src, out):
    k = Kit(src, coat="leaf")
    mound(k)
    head(k)
    antlers(k)
    shell(k)
    k.build(voxel=0.006, smooth=4, band=0.008)
    k.eye_states(lambda st: eyes(k, st))
    k.export(out)


if __name__ == "__main__":
    wochien(*args())
