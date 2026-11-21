# Metagross's Mega Evolution sculpted on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/metagrossmega.py -- <the export's Metagross-Mega.glb> <out.glb>
# From the two pictures: Metagross hovering, its four legs raised round it as arms in an X. Largest first: four great
# fists, each a faceted steel cylinder some two thirds of the body's width across and longer than it is wide, with a
# dark cuff at its root and three big white talons out of its end, the front pair reaching down and out, the hind pair
# up and out; the body between them a compact faceted ball of steel (Metagross's saucer drawn in), its front a face
# with a broad GOLD X across it reaching past its edges, a red eye in a black socket either side of the X's crossing,
# a big white cone under the face pointing down and forward and one at each side; each fist joined to the body by a
# thin dark rod with a ball at each end.
# Grown from metagross.py's line (beldum's gem, claw, eye frame and eye; metang's rod and column): the same steel,
# white claws, black sockets and red eyes, every part a faceted hull.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, unit, args, hull, lathe, outward, sibling

mg = sibling("metagross")
m = mg.m
b = mg.b
STEEL, SHADE, DEEP, JOINT, CLAW = b.STEEL, b.SHADE, b.DEEP, b.JOINT, b.CLAW
GOLD, GOLD_DARK = "#E9B949", "#C08C2C"

C = np.array([0.0, 0.0, 0.44])            # the body's middle
BODY = [  # along -Y (forward) from C: (distance, radius across, radius up, sides, turn)
    (-0.17, 0.085, 0.075, 8, 22.5), (-0.1, 0.155, 0.14, 10, 0), (0.03, 0.172, 0.152, 10, 0),
    (0.12, 0.14, 0.122, 10, 0), (0.168, 0.08, 0.07, 8, 22.5)]

_BODY = {}


def on_body(x, z, lift=0.0):
    """The point of the body's front at (x, z) and its facet's normal."""
    from mathutils import Vector
    from mathutils.bvhtree import BVHTree
    if "tree" not in _BODY:
        V, F = b.gem(C, (0, -1, 0), BODY, up=(0, 0, 1))
        _BODY["tree"] = BVHTree.FromPolygons(V.tolist(), F)
    hit = (None,)
    f = 1.0
    while hit[0] is None:
        hit = _BODY["tree"].ray_cast(Vector((x * f, -1.0, C[2] + (z - C[2]) * f)), Vector((0, 1.0, 0)))
        f *= 0.98
    p, n = np.array(hit[0]), np.array(hit[1])
    if n[1] > 0:
        n = -n
    return p + n * lift, n


def x_bar(k, name, sign):
    """One bar of the gold X: hulls laid over the face along its diagonal, running on past the body's outline."""
    S = np.linspace(-1, 1, 9)
    stations = []
    for s in S:
        x, z = 0.215 * s, C[2] - 0.005 + 0.19 * s * sign
        ins = min(abs(s), 0.72) * np.sign(s)          # past the face's edge the bar runs straight on
        p, n = on_body(0.215 * ins, C[2] - 0.005 + 0.19 * ins * sign)
        p = p + np.array([x - 0.215 * ins, 0.012 * (abs(s) - abs(ins)) / 0.3, z - (C[2] - 0.005 + 0.19 * ins * sign)])
        along = unit(0.215, 0, 0.19 * sign)
        w = np.cross(n, along); w /= np.linalg.norm(w)
        hw = 0.03 + 0.012 * abs(s) ** 2
        stations.append([p + w * sw * hw + n * h for sw in (1, -1) for h in (-0.012, 0.02)])
    for i in range(len(S) - 1):
        k.add(f"{name}{i}", hull(stations[i] + stations[i + 1]), GOLD, "head", fuse=False, surface="metal")


EYE_X, EYE_Z = 0.098, C[2] - 0.012

# the fists, for +X: (where its rod leaves the body, the fist's root, its direction, its length, its radius)
FISTS = {
    "leg": ((0.13, -0.06, 0.36), np.array((0.23, -0.1, 0.29)), unit(0.62, -0.25, -0.74), 0.22, 0.1),
    "hind": ((0.13, 0.06, 0.52), np.array((0.23, 0.1, 0.58)), unit(0.6, 0.25, 0.76), 0.22, 0.1),
}


def fist(k, name, at, d, L, r, bone):
    """A great fist: a faceted steel drum along d, a dark cuff at its root, three white talons out of its end."""
    k.add(name + "drum", b.gem(at, d, [(0.035, r * 0.82, r * 0.82, 8, 22.5), (0.06, r, r, 8, 22.5),
                                       (L * 0.8, r * 1.04, r * 1.04, 8, 22.5), (L, r * 0.88, r * 0.88, 8, 22.5)]),
          STEEL, bone, fuse=False, mirror=True, surface="metal")
    k.add(name + "cuff", b.gem(at, d, [(-0.01, r * 0.7, r * 0.7, 8, 22.5), (0.045, r * 0.86, r * 0.86, 8, 22.5)]),
          SHADE, bone, fuse=False, mirror=True, surface="metal")
    # the band round the drum's middle, darker
    k.add(name + "band", b.gem(at, d, [(L * 0.42, r * 1.06, r * 1.06, 8, 22.5), (L * 0.52, r * 1.06, r * 1.06, 8, 22.5)]),
          SHADE, bone, fuse=False, mirror=True, surface="metal")
    a1 = np.cross(d, (0, 0, 1.0)); a1 /= np.linalg.norm(a1)
    a2 = np.cross(d, a1)
    end = at + d * L
    for i, th in enumerate((math.radians(30), math.radians(150), math.radians(270))):
        radial = math.cos(th) * a1 + math.sin(th) * a2
        base = end - d * 0.03 + radial * r * 0.5
        k.add(f"{name}talon{i}", b.claw(base, d + radial * 0.28, 0.155, 0.052), CLAW, bone, fuse=False, mirror=True,
              surface="metal")


def metagross_mega(src, out):
    k = Kit(src, coat="metal")
    k.add("body", b.gem(C, (0, -1, 0), BODY, up=(0, 0, 1)), STEEL, "body", fuse=False, surface="metal")
    # its underside darker
    k.add("under", b.gem(C + (0, 0.02, -0.05), (0, 0, -1), [(0.0, 0.16, 0.15, 10, 0), (0.105, 0.09, 0.08, 10, 0)]),
          DEEP, "body", fuse=False, surface="metal")

    x_bar(k, "xup", 1)
    x_bar(k, "xdown", -1)

    # the white cones: one under the face pointing down and forward, one at each side
    k.add("chin", b.claw(C + (0, -0.13, -0.08), (0, -0.55, -0.85), 0.16, 0.045), CLAW, "head", fuse=False,
          surface="metal")
    k.add("side", b.claw(C + (0.15, 0.02, 0.0), (1, 0.15, -0.1), 0.12, 0.038), CLAW, "body", fuse=False, mirror=True,
          surface="metal")

    # the fists on their rods
    for name, (root, at, d, L, r) in FISTS.items():
        bone = name + "{s}"
        m.rod(k, name + "rod", root, at - d * 0.01, 0.02, bone)
        fist(k, name, at, d, L, r, bone)

    # the black sockets the eyes sit in (thin plates fused just proud of the face)
    for side, nm in ((1, "socketR"), (-1, "socketL")):
        p, n = on_body(side * EYE_X, EYE_Z, -0.002)
        b.eye_frame(k, nm, p, n, 0.05, "head", lobe=0.06)

    k.build(voxel=0.0035, smooth=3)
    V = np.array([v.co[:] for v in k.ob.data.vertices])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    print("KIT: board: shows %.3f in a square of %.3f" % (np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l))))

    def lay(state):
        p, n = on_body(EYE_X, EYE_Z, 0.004)
        b.lay_eye(k, state, p, n, (0.033, 0.033), lid=b.STEEL, mirror=True)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    metagross_mega(*args())
