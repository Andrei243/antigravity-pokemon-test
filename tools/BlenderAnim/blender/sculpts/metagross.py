# Metagross sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/metagross.py -- <the export's Metagross.glb> <out.glb>
#
# Grown from Metang's (and so Beldum's) script: the same steel, darker underside, white claws, round cuffs and red
# eyes on black plates, every part a faceted hull. From the references: the body is the mass, a wide heavy saucer of
# steel plates (Metang's grown), its front a great round faceted face with a silver X across it, a red eye with a dark
# pupil in a black socket either side between the X's arms; a row of small white teeth under its front rim. Four thick
# legs leave the saucer's upper corners: an upper limb out to a knee that rises only a little over the face's top, then
# a lower leg bending down and OUT to a foot set wide, standing on a round cuff with small white claws, so the stance
# reads as a crouch.
#
# The game's board frames a wide model with feet at its four corners by 1.3 x max(height, 0.85 x max(width, length))
# against the 0.853 x width + 0.522 x length it shows: the span is measured on the built mesh and printed on the KIT
# line "board". The feet are spread sideways more than lengthwise for it; the whole stance is drawn at SCALE.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, unit, args, hull, outward, sibling

m = sibling("metang")
b = m.b
STEEL, SHADE, DEEP, JOINT, CLAW = b.STEEL, b.SHADE, b.DEEP, b.JOINT, b.CLAW
SILVER = "#B9BEC7"
SCALE = 0.86                               # the whole model, about the ground under its middle

BC = np.array([0.0, 0.03, 0.30])           # the saucer's middle
TOP = [(-0.04, 0.26, 0.28), (0.04, 0.26, 0.28), (0.1, 0.22, 0.24), (0.15, 0.13, 0.143)]
UNDER = [(-0.15, 0.117, 0.13), (-0.1, 0.22, 0.24), (-0.033, 0.252, 0.272)]
FC = np.array([0.0, -0.13, 0.33])          # the face's middle (near the head bone)
FACE = [(-0.078, 0.24, 0.225, 10, 0), (0.039, 0.247, 0.231, 10, 0), (0.104, 0.21, 0.196, 10, 0),
        (0.146, 0.126, 0.12, 10, 0)]
FR = np.array([0.247, 0.146, 0.231])       # the ellipsoid the face's facets are cut from, for laying things on it

_FACE = {}


def on_face(x, z, lift=0.0, round_=False):
    """The point of the face's front at (x, z), found on its facets themselves, and the facet's normal there (or the
    ellipsoid's, round_, for a piece laid across several facets)."""
    from mathutils import Vector
    from mathutils.bvhtree import BVHTree
    if "tree" not in _FACE:
        V, F = b.gem(FC, (0, -1, 0), FACE, up=(0, 0, 1))
        _FACE["tree"] = BVHTree.FromPolygons(V.tolist(), F)
    hit = (None,)
    f = 1.0
    while hit[0] is None:          # past the face's outline: the nearest point inside it
        hit = _FACE["tree"].ray_cast(Vector((x * f, -1.0, FC[2] + (z - FC[2]) * f)), Vector((0, 1.0, 0)))
        f *= 0.98
    p, n = np.array(hit[0]), np.array(hit[1])
    if n[1] > 0:
        n = -n
    if round_:
        n = (p - FC) / FR ** 2
        n /= np.linalg.norm(n)
    return p + n * lift, n


def x_bar(k, name, sign):
    """One bar of the X: hulls laid along the face's diagonal, flat on top, wider at its squared ends."""
    S = np.linspace(-1, 1, 7)
    along = unit(0.21, 0, 0.19 * sign)
    stations = []
    for s in S:
        x, z = 0.21 * s, FC[2] - 0.017 + 0.19 * s * sign
        p, n = on_face(x, z, round_=True)
        w = np.cross(n, along); w /= np.linalg.norm(w)
        hw = 0.042 + 0.018 * abs(s) ** 3
        stations.append([p + w * sw * hw + n * h for sw in (1, -1) for h in (-0.015, 0.018)])
    for i in range(len(S) - 1):
        k.add(f"{name}{i}", hull(stations[i] + stations[i + 1]), SILVER, "head", fuse=False, surface="metal")


EYE_X, EYE_Z = 0.117, FC[2] + 0.031


# the legs, for +X: (the saucer's corner, the knee, stations of the lower leg from the knee down, the foot, the claws'
# facing). The knee rises a little over the face's top (0.56) and the lower leg leans out to a foot set wide.
LEGS = {
    "leg": ((0.19, -0.11, 0.42), (0.33, -0.17, 0.585),
            [((0.33, -0.175, 0.55), 0.074, 0.078), ((0.39, -0.2, 0.35), 0.09, 0.094), ((0.45, -0.225, 0.14), 0.1, 0.104)],
            (0.455, -0.225), 30.0),
    "hind": ((0.19, 0.15, 0.42), (0.33, 0.2, 0.575),
             [((0.33, 0.2, 0.54), 0.072, 0.076), ((0.39, 0.22, 0.34), 0.088, 0.092), ((0.45, 0.24, 0.14), 0.1, 0.104)],
             (0.455, 0.24), 150.0),
}


def beam(a, c, r0, r1):
    """A faceted limb from a to c."""
    a, c = np.array(a, float), np.array(c, float)
    L = np.linalg.norm(c - a)
    return b.gem(a, c - a, [(0, r0, r0, 8, 22.5), (L, r1, r1, 8, 22.5)])


def legs(k):
    for name, (corner, knee, st, foot, facing) in LEGS.items():
        bone = name + "{s}"
        # the upper limb from the saucer's corner to the knee, a dark joint where it leaves the saucer, the knee a block
        k.add(name + "upper", beam(corner, knee, 0.058, 0.06), STEEL, bone, fuse=False, mirror=True, surface="metal")
        k.add(name + "joint", outward(ell(corner, (0.062, 0.062, 0.05), sub=3)), SHADE, bone, fuse=False, mirror=True,
              surface="metal")
        kc = np.array(knee)
        k.add(name + "knee", hull([kc + (dx, dy, dz) for dx in (-0.06, 0.06) for dy in (-0.06, 0.06) for dz in (-0.05, 0.03)]
                                  + [kc + (0.0, 0.0, 0.05)]), STEEL, bone, fuse=False, mirror=True, surface="metal")
        # the lower leg, leaning out from the knee to the foot, widening
        k.add(name, m.column(st, n=8, turn=22.5), STEEL, bone, fuse=False, mirror=True, surface="metal")
        m.cuff_and_claws(k, name + "foot", (foot[0], foot[1], 0), 0.106, bone, 0.055, 0.075, claws=4, spread=70.0,
                         length=0.06, cr=0.021, facing=facing)


def metagross(src, out):
    k = Kit(src, coat="metal")
    k.add("top", m.column([(BC + (0, 0, t), ra, rb) for t, ra, rb in TOP], n=10, turn=0), STEEL, "body", fuse=False,
          surface="metal")
    k.add("under", m.column([(BC + (0, 0, t), ra, rb) for t, ra, rb in UNDER], n=10, turn=0), DEEP, "body",
          fuse=False, surface="metal")
    k.add("face", b.gem(FC, (0, -1, 0), FACE, up=(0, 0, 1)), STEEL, "head", fuse=False, surface="metal")
    # the row of small teeth under the front rim
    for i, a in enumerate(np.linspace(-56, 56, 5)):
        t = math.radians(a)
        base = BC + np.array([0.2 * math.sin(t), -0.24 * math.cos(t), -0.12])
        k.add(f"tooth{i}", b.claw(base, (0.1 * math.sin(t), -0.15, -1.0), 0.036, 0.018), CLAW, "head", fuse=False,
              surface="metal")
    x_bar(k, "xup", 1)
    x_bar(k, "xdown", -1)
    legs(k)
    # the black sockets the eyes sit in (thin plates fused just proud of the face)
    for side, nm in ((1, "socketR"), (-1, "socketL")):
        p, n = on_face(side * EYE_X, EYE_Z, -0.002)
        b.eye_frame(k, nm, p, n, 0.058, "head", lobe=0.06)
    # the whole stance at SCALE
    for p in k.parts:
        p["V"] = p["V"] * SCALE
    k.build(voxel=0.0035, smooth=3)
    V = np.array([v.co[:] for v in k.ob.data.vertices])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    print("KIT: board: shows %.3f in a square of %.3f" % (np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l))))

    def lay(state):
        p, n = on_face(EYE_X, EYE_Z, 0.004)
        b.lay_eye(k, state, p * SCALE, n, (0.038 * SCALE, 0.038 * SCALE), lid=b.STEEL, mirror=True)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    metagross(src, out)
