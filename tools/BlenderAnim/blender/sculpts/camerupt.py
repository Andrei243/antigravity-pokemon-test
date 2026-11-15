# Camerupt sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/camerupt.py -- <the export's Camerupt.glb> <out.glb>
# Grown from numel.py (sibling("numel")): its helpers and its way of making the half-shut eyes kept.
#
# From the references: a long, deep, red-orange camel on short legs hidden under a shaggy fringe, grey hooves under
# it; its big head carried low in front of the chest, a broad grey muzzle whose top edge is a zigzag of fur, two dark
# nostrils; heavy-lidded grumpy eyes (white, the pupil low, the lid falling to the nose), with no ring; ears out to the
# sides; a crown of five pointed tufts on the head. On its back two grey volcanoes, flat-topped stubby cones of flat
# faces, each standing in a ring of grey boulders; spiky fur along the back's edges; three blue rings on each flank, on
# the rear half; a pointed tail hanging down behind.
# Reference ratios (3D picture): length nose to tail 1.29 of the height, the volcanoes 0.27 of the height, the belly's
# fringe 0.05 of the height off the ground, head with its tuft 0.7 of the height, its width 0.37 of the length.
# The export's joints: body (0, 0, 0.24), head (0, -0.18, 0.24), legs (+-0.11, -0.11, 0.12), hind (+-0.11, 0.13, 0.12).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, srgb, inside, unit, polygon, tri, sibling, args, outward

nm = sibling("numel")
on_ell, normal_on, oriented, spike = nm.on_ell, nm.normal_on, nm.oriented, nm.spike

RED, MUZZLE, ROCK, ROCK2 = "#E06A3E", "#BEB8B2", "#A9A39B", "#8E8882"
CONE, CRATER, BLUE, HOOF, NOSTRIL = "#A8A29A", "#77716C", "#5AB6EA", "#6A6670", "#2A2224"
EDGE = 0.0035

BODY = ((0, 0.04, 0.25), (0.165, 0.27, 0.175))
H = np.array([0.0, -0.31, 0.215])
HR = np.array([0.118, 0.11, 0.118])
MUZ = np.array([0.0, -0.395, 0.155])
MUZR = np.array([0.092, 0.08, 0.075])
EYE_D = unit(0.55, -0.72, 0.38)
EYE_HALF = (0.032, 0.03)
CONES = (-0.04, 0.17)


def body_top(x, y):
    """The body's top surface at (x, y)."""
    c, r = BODY
    q = 1 - (x / r[0]) ** 2 - ((y - c[1]) / r[1]) ** 2
    return c[2] + r[2] * math.sqrt(max(q, 0.0))


def volcano(k, y, i, rs):
    """A stubby flat-topped cone of flat faces, a ring of boulders round its foot."""
    zb = body_top(0, y) - 0.03
    pts = []
    n = 7
    for j in range(n):
        a = 2 * math.pi * (j + 0.3 * rs.rand()) / n
        rb, rt = 0.092 * (0.92 + 0.12 * rs.rand()), 0.07 * (0.92 + 0.12 * rs.rand())
        pts.append((rb * math.cos(a), y + rb * math.sin(a), zb))
        pts.append((rt * math.cos(a + 0.15), y + rt * math.sin(a + 0.15), zb + 0.15))
    k.add(f"cone{i}", hull(pts), CONE, "body", fuse=False, surface="shell")
    for j in range(10):
        a = 2 * math.pi * (j + 0.4 * rs.rand()) / 10
        rr = 0.098 + 0.012 * rs.rand()
        x, yy = rr * math.cos(a), y + rr * math.sin(a)
        if abs(x) > 0.13:
            x = 0.13 * np.sign(x)
        c = np.array([x, yy, body_top(x, yy) + 0.006])
        s = 0.034 + 0.012 * rs.rand()
        P = rs.normal(size=(22, 3)); P /= np.linalg.norm(P, axis=1, keepdims=True)
        P = c + P * np.array([s, s, s * 0.85]) * (0.9 + 0.1 * rs.rand(22, 1))
        k.add(f"rock{i}{j}", hull(P), ROCK if (i + j) % 2 else ROCK2, "body", fuse=False, surface="shell")


def camerupt(src, out):
    k = Kit(src, coat="fur")
    rs = np.random.RandomState(7)

    # The body: a long deep barrel, a chest that carries the head, a rump
    k.add("body", ell(*BODY), RED, "body")
    k.add("chest", ell((0, -0.17, 0.23), (0.15, 0.12, 0.16)), RED, "body")
    k.add("rump", ell((0, 0.22, 0.24), (0.15, 0.11, 0.16)), RED, "body")

    # The head: low in front of the chest, the grey muzzle under its front
    k.add("head", ell(H, HR), RED, "head")
    k.add("muzzle", ell(MUZ, MUZR), MUZZLE, "head")
    # the grey rises over the face in a zigzag of fur
    def face(P):
        zz = 0.245 + 0.03 * tri(P[:, 0] / 0.07) - 0.15 * np.abs(P[:, 0])
        return inside(P[:, 2] - zz, EDGE) * inside(P[:, 1] + 0.355, EDGE)
    k.paint(face, MUZZLE)
    k.paint(lambda P: inside(np.linalg.norm((P - MUZ) / MUZR, axis=1) - 1.02, EDGE), MUZZLE, on=["muzzle"])
    # ears out to the sides, a little down and back, flat to the front
    for s, nmE in ((1, "R"), (-1, "L")):
        d = unit(1.0 * s, 0.25, -0.12)
        b = np.array([0, 0, 1.0]) - d * d[2]; b /= np.linalg.norm(b)
        a = np.cross(b, d)
        base = np.array([0.095 * s, -0.29, 0.28])
        taper = lambda V: V * np.stack([np.ones(len(V)), 1 - 0.8 * ((V[:, 0] + 1) / 2) ** 1.4, 1 - 0.5 * ((V[:, 0] + 1) / 2)], 1)
        k.add(f"ear{nmE}", oriented(base + d * 0.07, [d, b, a], (0.08, 0.045, 0.022), deform=taper), RED, "head")
    # the crown of tufts on the head
    top = np.array([0, -0.3, 0.315])
    for j, (d, L) in enumerate((((0, 0.2, 1), 0.14), ((0.6, 0.15, 0.8), 0.11), ((-0.6, 0.15, 0.8), 0.11),
                                ((0.3, -0.5, 0.8), 0.095), ((-0.3, -0.5, 0.8), 0.095))):
        k.add(f"tuft{j}", spike(top, d, L, 0.045, sink=0.03), RED, "head", fuse=False, surface="fur")

    # The legs: short and thick under the fringe, grey hooves
    for name, y, bone in (("leg", -0.11, "leg{s}"), ("hind", 0.15, "hind{s}")):
        k.add(name + "top", ell((0.11, y, 0.14), (0.075, 0.085, 0.09)), RED, bone, mirror=True)
        k.add(name, tube([(0.112, y, 0.13), (0.116, y - 0.004, 0.07)], [0.062, 0.058]), RED, bone, mirror=True)
        k.add(name + "hoof", ell((0.117, y - 0.006, 0.026), (0.058, 0.064, 0.027)), HOOF, bone, mirror=True,
              surface="shell")
        for j in range(5):
            a = math.radians(-60 + 30 * j)
            d = unit(math.cos(a), math.sin(a), -0.9)
            base = np.array([0.116 + 0.055 * math.cos(a), y - 0.004 + 0.06 * math.sin(a), 0.085])
            k.add(f"{name}fringe{j}", spike(base, d, 0.05, 0.026, sink=0.015), RED, bone, fuse=False, mirror=True,
                  surface="fur")

    # The shaggy fringe round the bottom of the body, and along the back's edges
    c, r = BODY
    for j in range(26):
        a = 2 * math.pi * (j + 0.5) / 26
        x, y = r[0] * 0.92 * math.cos(a), c[1] + (r[1] + 0.02) * 0.95 * math.sin(a)
        if y < -0.2 and abs(x) < 0.12:
            continue                                        # under the head
        d = unit(0.45 * math.cos(a), 0.3 * math.sin(a), -1.0)
        k.add(f"skirt{j}", spike((x, y, 0.135), d, 0.075, 0.036, sink=0.02), RED, "body", fuse=False, surface="fur")
    for j, y in enumerate(np.linspace(-0.2, 0.29, 9)):
        for s in (1, -1):
            x = 0.118 * s
            d = unit(0.75 * s, 0.15, 0.8)
            k.add(f"mane{j}{'R' if s > 0 else 'L'}", spike((x, y, body_top(x, y) - 0.01), d, 0.055, 0.03, sink=0.02),
                  RED, "body", fuse=False, surface="fur")

    # The tail: a point hanging down behind
    k.add("tail", hull([(0.03, 0.29, 0.29), (-0.03, 0.29, 0.29), (0, 0.29, 0.32), (0, 0.27, 0.25),
                        (0.015, 0.34, 0.2), (-0.015, 0.34, 0.2), (0, 0.39, 0.11)]), RED, "body", fuse=False,
          surface="fur")

    # The volcanoes and their boulders
    for i, y in enumerate(CONES):
        volcano(k, y, i, rs)

    # The blue rings on the flanks' rear half: one patch each, a blue band round the coat's own colour (the band's
    # edges between the patch's rings)
    def ring(u, v):
        q = np.hypot(u, v)
        col = np.tile(srgb(RED), (len(u), 1))
        col[(q > 0.604) & (q < 0.98)] = srgb(BLUE)
        return col
    for y in (-0.05, 0.06, 0.17):
        dd = np.array([0.95, (y - c[1]) / r[1] * 1.1, 0.28])
        cc = on_ell(c, r, dd)
        nn = normal_on(c, r, dd)
        k.decal(cc, nn, (0.032, 0.062), ring, eps=0.004, rings=24, segs=64, mirror=True)

    # Two nostrils on the muzzle's front
    ND = unit(0.28, -0.9, 0.1)
    k.decal(on_ell(MUZ, MUZR, ND), normal_on(MUZ, MUZR, ND), (0.012, 0.009), NOSTRIL, eps=0.004, rings=6, segs=24,
            mirror=True)

    k.build(voxel=0.0072, smooth=3, band=0.005)

    centre = on_ell(H, HR, EYE_D)
    n = normal_on(H, HR, EYE_D)
    k.eye_states(lambda st: nm.lay_eyes(k, st, centre, n, EYE_HALF, RED, line=0.06, slope=0.1,
                                        pr=(0.28, 0.32), rim=False))
    k.export(out)


if __name__ == "__main__":
    camerupt(*args())
