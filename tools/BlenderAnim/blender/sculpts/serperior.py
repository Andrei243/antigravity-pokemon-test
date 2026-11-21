# Serperior sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/serperior.py -- <the export's Serperior.glb> <out.glb>
# The end of Snivy's line (its yellow, its lidded eye and its flat leaves from snivy.py): a long regal serpent with no
# limbs, laid as dratini.py's line (a tube a stretch on its own bone, the pale underside painted by the angle round the
# line). From the references: dark green above, pale mint below; the head long and pointed, pale mint on top, white on
# the snout and the jaw and down the front of the neck; red almond eyes under a heavy lid; a dark green curl mark on
# each cheek; two long yellow blades sweeping back and up from the back of the head; a yellow leaf standing up on each
# side of the throat like a collar; a green vine on each shoulder sweeping back to a curl; yellow hooked marks curling on
# the tops of the coils and two yellow bands round the body; the tail a thin vine with three small three-pointed leaves
# and a curl at its end.
# (The pictures hold the body up in a great S; the game's skeleton lays it round on the ground in two coils with the neck
# rising from their middle, which decides.)
# The export's chain: body and tail at (0.24, 0.34, 0.08) behind on the +X side, seg1 (0.218, -0.1, 0.06) round the
# outer coil to seg6, seg7 to seg10 the inner coil over it, seg11 (-0.081, -0.088, 0.14), seg12 (0, -0.07, 0.38) up the
# neck, head (0, -0.06, 0.5).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, smoothstep, unit, eye_look, args, outward, sibling

D = sibling("dratini")          # Line, belly
SN = sibling("snivy")           # the line's yellow and leaf
T = SN.T                        # leaf_blade, brow_cut

DARK = "#2C8A40"
MINT = "#AEE0B4"
WHITE = "#F4F6EE"
YELLOW = SN.YELLOW
LEAF = "#2A7E3C"
RED, INK = "#E03A2A", srgb("#1A1214")
VOXEL = 0.0058

LINE = [(0.175, 0.345, 0.27), (0.215, 0.39, 0.2), (0.24, 0.38, 0.125), (0.245, 0.3, 0.078), (0.248, 0.13, 0.068),
        (0.218, -0.1, 0.065), (0.073, -0.216, 0.068), (-0.102, -0.19, 0.072), (-0.197, -0.053, 0.076),
        (-0.163, 0.101, 0.08), (-0.036, 0.176, 0.086), (0.097, 0.137, 0.1), (0.155, 0.021, 0.112),
        (0.112, -0.091, 0.124), (0.01, -0.132, 0.134), (-0.075, -0.09, 0.155), (-0.075, -0.065, 0.225),
        (-0.04, -0.065, 0.305), (0.0, -0.07, 0.38), (0.0, -0.076, 0.44), (0.0, -0.085, 0.49)]
RADS = [0.015, 0.017, 0.024, 0.036, 0.048, 0.056, 0.06, 0.062, 0.062, 0.062, 0.061, 0.06, 0.058, 0.057, 0.056,
        0.056, 0.055, 0.052, 0.05, 0.048, 0.047]
BONES = ["tail", "tail", "tail", "body", "body", "seg1", "seg2", "seg3", "seg4", "seg5", "seg6", "seg7", "seg8", "seg9",
         "seg10", "seg11", "seg11", "seg11", "seg12", "seg12"]
HC = np.array([0.0, -0.11, 0.53])           # the head's middle
HR = np.array([0.058, 0.09, 0.052])          # a long head, a third larger than the pictures' share of the height


def head(k):
    """A long head tapering to a pointed snout, a little up, the jaw full under it."""
    def wedge(V):
        V = V.copy()
        t = np.clip(-V[:, 1], 0, 1)                 # toward the snout
        V[:, 0] *= 1 - 0.55 * t ** 1.2
        V[:, 2] *= 1 - 0.45 * t ** 1.2
        V[:, 2] += 0.1 * (1 - V[:, 1] ** 2) * (V[:, 2] > 0)
        return V
    k.add("skull", ell(HC, HR, rot=(-8, 0, 0), deform=wedge), MINT, "head")
    k.add("jaw", ell(HC + (0, -0.03, -0.022), (0.048, 0.07, 0.034), rot=(-4, 0, 0)), WHITE, "head")
    k.add("nape", tube([LINE[-2], LINE[-1], HC + (0, 0.035, -0.015)], [0.048, 0.047, 0.05]), DARK, "head")


def white_face(P):
    """White under a line from the snout's tip back under the eye, and down the front of the neck."""
    rel = P - HC
    line = -0.004 + 0.12 * (rel[:, 1] + 0.02)        # rising toward the snout's tip (y negative is forward)
    face = inside(rel[:, 2] - np.minimum(line, 0.012), 0.003) * inside(np.linalg.norm(rel, axis=1) - 0.13, 0.004)
    a = np.arctan2(np.abs(P[:, 0]), -(P[:, 1] + 0.07))
    neck = inside(a - 0.85, 0.06) * inside(0.39 - P[:, 2], 0.02) * (P[:, 2] < 0.52)
    return np.maximum(face, neck)


def cheek_curl(k):
    """A dark green curl on each cheek behind the eye: a small hook, laid as a patch over the mint."""
    dark, mint = srgb(DARK), srgb(MINT)

    def look(u, v):
        c = np.tile(mint, (len(u), 1))
        r = np.hypot(u + 0.2, v)
        ang = np.arctan2(v, u + 0.2)
        ring = (np.abs(r - 0.5) < 0.2) & (ang > -2.2) & (ang < 1.6)
        tail = (np.abs(v - (-0.5 + 0.25 * (u - 0.3))) < 0.18) & (u > 0.0) & (u < 0.95)
        c[ring | tail] = dark
        return c
    for s in (1, -1):
        de = unit(0.95 * s, 0.25, 0.1)
        c = HC + np.array([0.052 * s, 0.035, 0.0])
        k.decal(c, de, (0.024, 0.02), (lambda u, v: look(u, v)) if s > 0 else (lambda u, v: look(-u, v)),
                eps=0.003, rings=14, segs=48)


def blades(k):
    """Two long yellow blades sweeping back and up from the back of the head, faces turned out."""
    for s, side in ((1, "R"), (-1, "L")):
        base = HC + np.array([0.03 * s, 0.03, 0.035])
        d = unit(0.32 * s, 0.9, 0.33)
        k.add(f"blade{side}", T.leaf_blade(base, d, (s, -0.1, 0.6), 0.2, 0.026, 0.03, widest=0.3), YELLOW, "head",
              fuse=False, surface="leaf")


def collar(k):
    """A yellow leaf standing up on each side of the throat, flaring out, like a collar."""
    for s, side in ((1, "R"), (-1, "L")):
        base = np.array([0.036 * s, -0.06, 0.4])
        d = unit(0.42 * s, -0.25, 0.87)
        k.add(f"collar{side}", T.leaf_blade(base, d, (s, -0.6, -0.1), 0.1, 0.03, 0.03, widest=0.45), YELLOW, "seg12",
              fuse=False, surface="leaf")


def vines(k):
    """A green vine on each shoulder sweeping back and out and ending in a curl."""
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        root = np.array([0.026, 0.0, 0.345])
        run = [root, root + (0.014, 0.05, -0.04), root + (0.024, 0.1, -0.08), root + (0.03, 0.15, -0.1)]
        end = run[-1]
        curl = [end + np.array([0.0, 0.03 * math.sin(th), 0.03 * (1 - math.cos(th))]) * (1 - 0.1 * th)
                for th in np.linspace(0.4, 1.6 * math.pi, 7)]
        pts = np.array(run + curl) * m
        n = len(pts)
        k.add(f"vine{side}", outward(tube(pts, list(np.linspace(0.017, 0.013, n)))), DARK, "seg12", fuse=False,
              surface="leaf")


def hooks(k, line):
    """Yellow hooked marks on the tops and outsides of the coils: a crescent opening down, each end curling."""
    yel, dark = srgb(YELLOW), srgb(DARK)

    def look(u, v):
        c = np.tile(dark, (len(u), 1))
        r = np.hypot(u, v - 0.15)
        ang = np.arctan2(v - 0.15, u)
        arc = (np.abs(r - 0.62) < 0.2) & (ang > -0.3) & (ang < math.pi + 0.3)
        tips = (np.hypot(np.abs(u) - 0.72, v + 0.45) < 0.24) & (np.hypot(np.abs(u) - 0.5, v + 0.45) > 0.12)
        c[arc | tips] = yel
        return c
    for s, up in ((6.4, 0.9), (7.6, 0.9), (8.8, 0.9), (11.6, 1.2), (13.0, 1.2)):
        c, t, r = line.at(s)
        out = np.array([c[0], c[1], 0.0]); out /= np.linalg.norm(out) + 1e-9
        n = out + np.array([0, 0, up])
        n = n - t * (n @ t); n /= np.linalg.norm(n)
        k.decal(c + n * r, n, (0.042, 0.034), look, eps=0.004, rings=16, segs=56, up=t)


def bands(line, at, half=0.011):
    """Yellow bands round the body at arc lengths (the line's parameters), on the dark top and the sides."""
    def mask(Q):
        j, s, d = line.place(Q)
        par = line.S[j]
        m = np.zeros(len(Q))
        for a in at:
            c, _, _ = line.at(a)
            s0 = line.cum[int(np.argmin(np.abs(line.S - a)))]
            m = np.maximum(m, inside(np.abs(s - s0) - half, 0.003))
        return m * inside(d - 0.3, 0.05)
    return mask


def tail_leaves(k, line):
    """Three small three-pointed leaves along the tail's vine and a curl at its tip."""
    out = [(-0.006, 0.0), (0.006, 0.0), (0.03, 0.02), (0.05, 0.022), (0.036, 0.034), (0.048, 0.055), (0.024, 0.048),
           (0.0, 0.07), (-0.024, 0.048), (-0.048, 0.055), (-0.036, 0.034), (-0.05, 0.022), (-0.03, 0.02)]
    for j, (s, d) in enumerate([(0.6, (0.9, 0.2, 0.3)), (1.3, (-0.4, 0.8, 0.3)), (2.0, (0.9, -0.2, 0.2))]):
        c, t, r = line.at(s)
        d = unit(*d)
        d = d - t * (d @ t); d /= np.linalg.norm(d)
        ex = t
        k.add(f"tailleaf{j}", slab(out, c + d * r * 0.6, ex, d, 0.016), LEAF, "tail", fuse=False, surface="leaf")
    c, t, r = line.at(0.0)
    side = np.cross(t, (0, 0, 1.0)); side /= np.linalg.norm(side)
    curl = [c + (t * math.sin(th) + side * (1 - math.cos(th))) * 0.028 * (1 - 0.12 * th) for th in np.linspace(0, 1.7 * math.pi, 8)]
    k.add("tailcurl", outward(tube(curl, list(np.linspace(0.015, 0.013, 8)))), DARK, "tail", fuse=False, surface="leaf")


def lay_eyes(k):
    """Red almond eyes under a heavy lid, a small dark pupil, a white glint; laid by hand so +u runs back on both."""
    R, W = srgb(RED), srgb("#FFFFFF")
    top, slope = 0.3, 0.2
    cut = T.brow_cut(top, slope)

    def eye(u, v):
        c = np.tile(R, (len(u), 1))
        c[((u + 0.12) / 0.2) ** 2 + ((v + 0.1) / 0.4) ** 2 < 1] = INK
        c[((u + 0.42) / 0.14) ** 2 + ((v - 0.0) / 0.16) ** 2 < 1] = W
        c[np.hypot(u, v) > 0.84] = INK
        c[v > top + slope * u - 0.17] = INK
        return c
    lid = srgb(MINT)
    de = unit(0.78, -0.48, 0.32)
    E = HC + np.array([0.047, -0.03, 0.022])

    def fierce(u, v):
        c = eye(u, v)
        edge = top - 0.3 - 0.45 * u
        c[v > edge] = lid
        c[np.abs(v - edge) < 0.15] = INK
        return c

    def lay(state):
        fn = fierce if state == "Fierce" else eye_look(state, eye, lid, INK, line=0.17)
        m = np.array([-1, 1, 1.0])
        k.decal(E, de, (0.026, 0.022), fn, eps=0.0045, rings=20, segs=56, outline=cut)
        k.decal(E * m, de * m, (0.026, 0.022), lambda u, v: fn(-u, v), eps=0.0045, rings=20, segs=56,
                outline=lambda th: cut(math.pi - th))
    return lay


def serperior(src, out):
    k = Kit(src, coat="scales")
    line = D.Line(LINE, RADS, rise=(0.18, 0.28))
    line.tubes(k, BONES, DARK)
    head(k)
    blades(k)
    collar(k)
    vines(k)
    tail_leaves(k, line)

    k.paint(D.belly(line, 2.5, 20.0, ground=-0.35, risen=-0.2, rise=(0.18, 0.28), edge=0.07), MINT, on=["seg", "nape"])
    k.paint(bands(line, [9.6, 10.2]), YELLOW, on=["seg"])
    k.paint(white_face, WHITE, on=["skull", "jaw", "nape", "seg"])
    hooks(k, line)
    cheek_curl(k)

    k.build(voxel=VOXEL, smooth=4, band=0.005)
    k.eye_states(lay_eyes(k))
    k.export(out)


if __name__ == "__main__":
    serperior(*args())
