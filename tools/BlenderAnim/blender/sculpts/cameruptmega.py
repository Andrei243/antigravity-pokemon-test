# Camerupt-Mega sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cameruptmega.py -- <the export's Camerupt-Mega.glb> <out.glb>
# Grown from camerupt.py (its frame, colours and helpers through sibling; its build copied, since its function takes
# no arguments that reach what changes) and numel.py's eyes.
#
# From the references: the two volcanoes become one great volcano over the rear half of the back, the design's largest
# mass: a pale grey cone of flat faces rising some 0.45 of the height over the back, its crater brimming with orange
# lava that runs down its sides in red streams; round its foot the back is a shell of big black boulders bedded in
# glowing orange, ringed by smaller ones. The coat hangs as a long shaggy skirt to just over the dark hooves; the blue
# rings are gone. The head keeps the grey muzzle with its zigzag, with a bigger crown of tufts, a black zigzag mark on
# the brow, a smug look (the lid lower than Camerupt's, the pupil low) and a wide grin.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, srgb, inside, unit, polygon, tri, sibling, args

cm = sibling("camerupt")
nm = cm.nm
on_ell, normal_on, oriented, spike = nm.on_ell, nm.normal_on, nm.oriented, nm.spike

RED, MUZZLE, HOOF, NOSTRIL = cm.RED, cm.MUZZLE, cm.HOOF, cm.NOSTRIL
CONE, LAVA, MAGMA, GLOW, STONE, STONE2, MARK = ("#D2CCC5", "#F7A51E", "#E4492A", "#F07A26", "#45434A", "#5A575C",
                                               "#2A2226")
EDGE = cm.EDGE
BODY, H, HR, MUZ, MUZR, EYE_D, EYE_HALF = cm.BODY, cm.H, cm.HR, cm.MUZ, cm.MUZR, cm.EYE_D, cm.EYE_HALF
body_top = cm.body_top
VY = 0.09                     # the volcano's middle along the back
VBX, VBY, VT = 0.135, 0.19, 0.06   # its radii at the foot (across, along) and at the crater
VH = 0.34                     # its height over the back


def volcano(k, rs):
    zb = body_top(0, VY) - 0.05
    pts = []
    n = 11
    for j in range(n):
        a = 2 * math.pi * (j + 0.25 * rs.rand()) / n
        w = 0.93 + 0.12 * rs.rand()
        rt = VT * (0.92 + 0.14 * rs.rand())
        for t, f in ((0.0, 1.0), (0.3, 0.62), (0.62, 0.36), (1.0, 0.0)):
            # a concave slope: wide foot, the sides steepening toward the crater
            rx = rt + (VBX * w - rt) * f
            ry = rt + (VBY * w - rt) * f
            aa = a + 0.12 * t
            pts.append((rx * math.cos(aa), VY + ry * math.sin(aa), zb + VH * t))
    k.add("cone", hull(pts), CONE, "body", fuse=False, surface="shell")
    top = zb + VH
    prof = [(0, top - 0.01), (VT * 1.1, top - 0.012), (VT * 1.16, top + 0.006), (VT * 0.9, top + 0.03),
            (0, top + 0.024)]
    k.add("crater", lathe((0, VY, 0), (0, 0, 1), prof, seg=36), LAVA, "body", fuse=False, surface="skin")
    for j in range(6):
        a = 2 * math.pi * (j + 0.5 * rs.rand()) / 6
        c = np.array([VT * 1.05 * math.cos(a), VY + VT * 1.05 * math.sin(a), top - 0.006])
        k.add(f"drip{j}", ell(c, (0.032, 0.032, 0.024), sub=3), LAVA, "body", fuse=False, surface="skin")
    # three streams running down from the rim, wavy, laid on the cone's faces
    for j, a in enumerate((-2.0, -0.55, 1.2)):
        ln = 0.7 + 0.25 * rs.rand()
        P = []
        for t in np.linspace(0, ln, 7):
            f = t
            aa = a + 0.18 * math.sin(t * 6 + j)
            rx, ry = VT + (VBX - VT) * f, VT + (VBY - VT) * f
            P.append((rx * 0.95 * math.cos(aa), VY + ry * 0.95 * math.sin(aa), top - VH * t))
        k.add(f"stream{j}", tube(P, [0.026, 0.022, 0.024, 0.02, 0.022, 0.018, 0.012], seg=10, per=4), MAGMA, "body",
              fuse=False, surface="skin")


def boulders(k, rs):
    """The shell round the volcano's foot: big black stones bedded in orange, a ring of smaller ones at its rim."""
    def stone(name, c, s, col):
        P = rs.normal(size=(24, 3)); P /= np.linalg.norm(P, axis=1, keepdims=True)
        P = np.array(c) + P * np.array([s, s, s * 0.85]) * (0.88 + 0.12 * rs.rand(24, 1))
        k.add(name, hull(P), col, "body", fuse=False, surface="shell")
    BIG = ((0.0, -0.14, 0.085), (0.12, -0.06, 0.08), (-0.12, -0.06, 0.08), (0.13, 0.1, 0.078),
           (-0.13, 0.1, 0.078), (0.1, 0.25, 0.072), (-0.1, 0.25, 0.072), (0.0, 0.31, 0.068))
    for j, (x, y, s) in enumerate(BIG):
        stone(f"big{j}", (x, y, body_top(x, y) + s * 0.3), s, STONE)
    c, r = BODY
    for j in range(26):
        a = 2 * math.pi * (j + 0.5) / 26
        x = 0.158 * math.cos(a)
        y = VY + 0.26 * math.sin(a)
        if y > c[1] + r[1] * 0.98:
            y = c[1] + r[1] * 0.95
        z = body_top(x * 0.95, y) - 0.012
        stone(f"ring{j}", (x, y, z), 0.04 + 0.01 * rs.rand(), STONE2 if j % 2 else STONE)


def cameruptmega(src, out):
    k = Kit(src, coat="fur")
    rs = np.random.RandomState(11)

    # The body as camerupt.py's, the coat a long skirt
    k.add("body", ell(*BODY), RED, "body")
    k.add("chest", ell((0, -0.17, 0.23), (0.15, 0.12, 0.16)), RED, "body")
    k.add("rump", ell((0, 0.22, 0.24), (0.15, 0.11, 0.16)), RED, "body")
    # the back between the stones glowing orange
    c, r = BODY
    def bed(P):
        q = (P[:, 0] / 0.16) ** 2 + ((P[:, 1] - VY) / 0.27) ** 2
        return inside(q - 1, 0.03) * (P[:, 2] > 0.33)
    k.paint(bed, GLOW)

    # The head: camerupt.py's, its crown of tufts larger
    k.add("head", ell(H, HR), RED, "head")
    k.add("muzzle", ell(MUZ, MUZR), MUZZLE, "head")
    def face(P):
        zz = 0.245 + 0.03 * tri(P[:, 0] / 0.07) - 0.15 * np.abs(P[:, 0])
        return inside(P[:, 2] - zz, EDGE) * inside(P[:, 1] + 0.355, EDGE)
    k.paint(face, MUZZLE)
    k.paint(lambda P: inside(np.linalg.norm((P - MUZ) / MUZR, axis=1) - 1.02, EDGE), MUZZLE, on=["muzzle"])
    for s, nmE in ((1, "R"), (-1, "L")):
        d = unit(1.0 * s, 0.25, -0.12)
        b = np.array([0, 0, 1.0]) - d * d[2]; b /= np.linalg.norm(b)
        a = np.cross(b, d)
        base = np.array([0.095 * s, -0.29, 0.28])
        taper = lambda V: V * np.stack([np.ones(len(V)), 1 - 0.8 * ((V[:, 0] + 1) / 2) ** 1.4, 1 - 0.5 * ((V[:, 0] + 1) / 2)], 1)
        k.add(f"ear{nmE}", oriented(base + d * 0.07, [d, b, a], (0.08, 0.045, 0.022), deform=taper), RED, "head")
    top = np.array([0, -0.3, 0.31])
    TUFTS = (((0, 0.2, 1), 0.19), ((0.55, 0.1, 0.85), 0.16), ((-0.55, 0.1, 0.85), 0.16), ((0.3, -0.5, 0.85), 0.13),
             ((-0.3, -0.5, 0.85), 0.13), ((0.85, 0.3, 0.45), 0.14), ((-0.85, 0.3, 0.45), 0.14), ((0, 0.75, 0.6), 0.15),
             ((0.45, 0.6, 0.65), 0.13), ((-0.45, 0.6, 0.65), 0.13))
    for j, (d, L) in enumerate(TUFTS):
        k.add(f"tuft{j}", spike(top, d, L, 0.05, sink=0.035), RED, "head", fuse=False, surface="fur")
    k.add("crownmass", ell((0, -0.29, 0.31), (0.085, 0.075, 0.05)), RED, "head")

    # The legs short and thick, hidden under the skirt but for the dark hooves
    for name, y, bone in (("leg", -0.11, "leg{s}"), ("hind", 0.15, "hind{s}")):
        k.add(name + "top", ell((0.11, y, 0.14), (0.075, 0.085, 0.09)), RED, bone, mirror=True)
        k.add(name, tube([(0.112, y, 0.13), (0.116, y - 0.004, 0.07)], [0.062, 0.058]), RED, bone, mirror=True)
        k.add(name + "hoof", ell((0.117, y - 0.006, 0.026), (0.058, 0.064, 0.027)), HOOF, bone, mirror=True,
              surface="shell")
        for j in range(5):
            a = math.radians(-60 + 30 * j)
            d = unit(math.cos(a), math.sin(a), -0.9)
            base = np.array([0.116 + 0.055 * math.cos(a), y - 0.004 + 0.06 * math.sin(a), 0.085])
            k.add(f"{name}fringe{j}", spike(base, d, 0.045, 0.026, sink=0.015), RED, bone, fuse=False, mirror=True,
                  surface="fur")

    # The long shaggy skirt round the body, two rows, hanging to just over the hooves
    for row, (zr, ln, ws, n) in enumerate(((0.2, 0.13, 0.042, 30), (0.13, 0.085, 0.034, 26))):
        for j in range(n):
            a = 2 * math.pi * (j + 0.5 * row + 0.5) / n
            x, y = r[0] * (1.0 - 0.06 * row) * math.cos(a), c[1] + (r[1] + 0.03) * (0.98 - 0.05 * row) * math.sin(a)
            if y < -0.2 and abs(x) < 0.12:
                continue
            d = unit(0.35 * math.cos(a), 0.25 * math.sin(a), -1.0)
            k.add(f"skirt{row}_{j}", spike((x, y, zr), d, ln * (0.9 + 0.2 * rs.rand()), ws, sink=0.02), RED, "body",
                  fuse=False, surface="fur")

    k.add("tail", hull([(0.03, 0.29, 0.29), (-0.03, 0.29, 0.29), (0, 0.29, 0.32), (0, 0.27, 0.25),
                        (0.015, 0.34, 0.22), (-0.015, 0.34, 0.22), (0, 0.4, 0.18)]), RED, "body", fuse=False,
          surface="fur")

    volcano(k, rs)
    boulders(k, rs)

    # Two nostrils, the grin, the black zigzag on the brow
    ND = unit(0.28, -0.9, 0.1)
    k.decal(on_ell(MUZ, MUZR, ND), normal_on(MUZ, MUZR, ND), (0.012, 0.009), NOSTRIL, eps=0.004, rings=6, segs=24,
            mirror=True)
    CM, CK, CW = srgb(MUZZLE), srgb(NOSTRIL), srgb("#F4F2EE")
    def grin(u, v):
        col = np.tile(CM, (len(u), 1))
        curve = -0.35 + 0.55 * u * u
        band = np.abs(v - curve) < 0.12 + 0.12 * (np.abs(u) > 0.55)
        col[band & (np.abs(u) < 0.92)] = CK
        col[(v > curve) & (v < curve + 0.22) & (np.abs(u) > 0.5) & (np.abs(u) < 0.85)] = CW
        return col
    GD = unit(0.0, -0.75, -0.66)
    k.decal(on_ell(MUZ, MUZR, GD), normal_on(MUZ, MUZR, GD), (0.075, 0.04), grin, eps=0.004, rings=16, segs=64)
    BD = unit(0.0, -0.62, 0.78)
    zig = polygon([(-1.0, -0.25), (-1.0, 0.1), (-0.6, 0.6), (-0.3, 0.35), (0.0, 0.62), (0.3, 0.35), (0.6, 0.6), (1.0, 0.1), (1.0, -0.25),
                   (0.0, -0.45)])
    k.decal(on_ell(H, HR, BD), normal_on(H, HR, BD), (0.05, 0.028), MARK, outline=zig, eps=0.004, rings=12,
            segs=96)

    k.build(voxel=0.0072, smooth=3, band=0.005)

    centre = on_ell(H, HR, EYE_D)
    n = normal_on(H, HR, EYE_D)
    k.eye_states(lambda st: nm.lay_eyes(k, st, centre, n, (EYE_HALF[0] * 1.08, EYE_HALF[1] * 1.08), RED, line=0.2,
                                        slope=-0.25, pr=(0.26, 0.3), rim=False))
    k.export(out)


if __name__ == "__main__":
    cameruptmega(*args())
