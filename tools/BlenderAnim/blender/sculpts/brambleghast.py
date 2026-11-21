# Brambleghast sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/brambleghast.py -- <the export's Brambleghast.glb> <out.glb>
#
# From the references: Bramblin grown into a great wreath of thorny branches standing on its edge, facing the front:
# two strands round each side, pale tan, thorns all round its outside, the strands' ends at the top left open and
# tipped purple, with two more purple-tipped points reaching in from the top. Inside it grows a brown thorny tree from
# the wreath's foot: a trunk up the middle to a thorn at its top, a pair of boughs out to the wreath's sides, and two
# thorny arches each holding a dark diamond eye with a red slit (Bramblin's eye, larger). Darker branches cross behind.
#
# Grown from bramblin.py (its thorn, its diamond eye and its slit).
# Skeleton: body (0, 0, 0.552), head (0, 0, 0.552): the wreath on body, the tree and the eyes on head.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, sibling, args

bl = sibling("bramblin")

TAN, BROWN, BACK, PURPLE = "#EAD58E", "#C0703E", "#BCA26E", "#A98AD6"

C = np.array([0.0, 0.0, 0.52])           # the wreath's middle
RR = 0.385                               # its radius
TR = 0.034                               # a strand's radius
VOXEL = 0.0078
EYE_AT = np.array([0.15, -0.075, 0.56])
EYE_HALF = (0.05, 0.085)
EYE_LEAN = 16.0


def ring(phi, f=1.0, y=0.0):
    """A point of the wreath at phi degrees round from the top toward +X, f of its radius out, y behind."""
    a = math.radians(phi)
    return C + np.array([math.sin(a) * RR * f, y, math.cos(a) * RR * f])


def strand(k, name, phi0, phi1, f, ph, colour=TAN, bone="body", r=TR, tip=PURPLE, tip_len=0.08, curl=0.0):
    """A strand of the wreath from phi0 round to phi1, waving front to back, its end a loose point."""
    n = 14
    pts = []
    for i in range(n):
        t = i / (n - 1)
        phi = phi0 + (phi1 - phi0) * t
        pts.append(ring(phi, f + 0.03 * math.sin(5 * t + ph) - curl * t ** 3,
                        0.035 * math.sin(3.3 * t + 2 * ph)))
    pts = np.array(pts)
    k.add(name, tube(pts, np.linspace(r, r * 0.8, n), seg=16, per=4), colour, bone)
    d = pts[-1] - pts[-2]
    bl.spike(k, name + "tip", pts[-1] - unit(*d) * r * 1.3, d, tip_len + r * 0.8, r * 0.74, srgb(tip), bone)
    return pts


def branch(k, name, pts, r0, r1, colour, bone, tip=None, tip_len=0.07):
    pts = np.array(pts, float)
    k.add(name, tube(pts, np.linspace(r0, r1, len(pts)), seg=14, per=6), colour, bone)
    if tip is not None:
        d = pts[-1] - pts[-2]
        bl.spike(k, name + "tip", pts[-1] - unit(*d) * r1 * 0.5, d, tip_len, r1 * 0.95, srgb(tip), bone)


def brambleghast(src, out):
    k = Kit(src, coat="shell")
    # ---- the wreath: two strands round each side, open at the top, their ends tipped purple
    s1 = strand(k, "wrL", 178, 352, 1.0, 0.0, tip_len=0.1, curl=0.18)
    s2 = strand(k, "wrR", 186, 20, 1.0, 1.4, tip_len=0.1, curl=0.12)
    s3 = strand(k, "wrL2", 150, 320, 0.9, 2.2, tip_len=0.09, curl=0.22)
    s4 = strand(k, "wrR2", 214, 48, 0.92, 0.6, tip_len=0.09, curl=0.25)
    # thorns standing out of the strands themselves: outward on the outer two, inward on the inner two
    i = 0
    for pts, idx, sign in ((s1, (2, 4, 6, 8, 10, 12), 1), (s2, (1, 3, 5, 7, 9, 11), 1),
                           (s3, (5, 9), -1), (s4, (4, 8), -1)):
        for j in idx:
            p = pts[j]
            n = unit(*(p - C)) * sign + np.array([0, 0.22 * math.sin(i * 1.9), 0.12 * math.cos(i * 1.3)])
            bl.spike(k, f"thorn{i}", p, n, 0.085 + TR * 0.6, 0.022, srgb(TAN))
            i += 1
    # ---- the tree within: trunk, boughs to the sides, arches holding the eyes
    foot = ring(180, 0.95, -0.01)
    top = C + np.array([0.02, -0.03, 0.3])
    branch(k, "trunk", [foot, C + np.array([-0.02, -0.03, -0.2]), C + np.array([0.01, -0.04, 0.0]),
                        C + np.array([0.025, -0.035, 0.18]), top], 0.045, 0.026, BROWN, "head", tip=BROWN,
           tip_len=0.08)
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        root = C + np.array([0.0, -0.04, -0.12])
        branch(k, "bough" + sd, [root, root + np.array([0.12 * s, -0.01, 0.02]), root + np.array([0.24 * s, 0.0, 0.07]),
                                 ring(110 * s, 0.93, -0.0)], 0.032, 0.026, BROWN, "head")
        branch(k, "low" + sd, [C + np.array([0, -0.03, -0.26]), C + np.array([0.12 * s, -0.02, -0.28]),
                               ring(150 * s, 0.93)], 0.03, 0.026, BROWN, "head")
        # the arch over the eye, open beneath, and the twig holding it from the trunk
        e = EYE_AT * np.array([s, 1, 1])
        arch = [e + np.array([math.sin(math.radians(a)) * 0.1 * s, 0.01, math.cos(math.radians(a)) * 0.115])
                for a in (-112, -80, -40, 0, 40, 80, 112)]
        branch(k, "arch" + sd, arch, 0.021, 0.019, BROWN, "head")
        branch(k, "hold" + sd, [C + np.array([0.01 * s, -0.04, -0.06]), e + np.array([-0.06 * s, 0.01, -0.1]),
                                arch[0]], 0.022, 0.02, BROWN, "head")
        for j, a in enumerate((-95, -25, 35, 100)):
            ar = math.radians(a)
            d = np.array([math.sin(ar) * s, 0, math.cos(ar)])
            bl.spike(k, f"archthorn{sd}{j}", e + np.array([math.sin(ar) * 0.1 * s, 0.01, math.cos(ar) * 0.115]),
                     d, 0.055, 0.016, srgb(BROWN), "head")
        bl.eye_part(k, "eye" + sd, e, EYE_HALF, -EYE_LEAN * s)
    # thorns on the trunk and the boughs
    for j, (p, d) in enumerate([(C + np.array([0.02, -0.06, 0.12]), (1, -0.2, 0.4)),
                                (C + np.array([0.0, -0.06, -0.05]), (-1, -0.2, 0.3)),
                                (C + np.array([0.16, -0.05, -0.07]), (0.2, -0.1, 1)),
                                (C + np.array([-0.16, -0.05, -0.07]), (-0.2, -0.1, 1)),
                                (C + np.array([0.13, -0.04, -0.26]), (0.1, -0.2, -1)),
                                (C + np.array([-0.13, -0.04, -0.26]), (-0.1, -0.2, -1))]):
        bl.spike(k, f"treethorn{j}", p, d, 0.06, 0.017, srgb(BROWN), "head")
    # the wreath browner at its foot, where the tree grows from it
    k.paint(lambda P: inside(P[:, 2] - (C[2] - RR * 0.82), 0.05), BROWN, on=["wr"])
    k.build(voxel=VOXEL, smooth=3, band=0.006, wband=0.06)

    def eyes(state):
        for s in (1, -1):
            c = EYE_AT * np.array([s, 1, 1]) + np.array([0, -EYE_HALF[0] * 0.55, 0])
            bl.lay_eye(k, state, c, (0, -1, 0), (EYE_HALF[0] * 0.86, EYE_HALF[1] * 0.9), -EYE_LEAN * s, s)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    brambleghast(*args())
