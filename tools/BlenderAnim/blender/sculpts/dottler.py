# Dottler sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dottler.py -- <the export's Dottler.glb> <out.glb>
#
# Blipbug grown (sibling("blipbug"): its navy, its spike and its prongs). From the references: a great dome of a
# shell cut in triangular facets like the top of an icosahedron, a corner pointing to the front at the rim, golden
# yellow, a round navy dot over every corner (the top, the five of the upper ring, the five of the rim); the task's
# "orange triangles" are not in the pictures, whose facets are all yellow. Under its front rim the orange body peers
# out with two eyes close together, each light blue with a deeper blue middle and a glint in a thick orange-red rim;
# four little navy feet under it and a navy two-pronged spike out of each side, out and back. The thin dark lines the
# pictures draw along the facets' edges are left out: at the game's size they would be wire, and the flat shading of
# each facet shows the edges.
# Skeleton: body (0, 0, 0.15), legs (+-0.1, -0.06, 0.05), hinds (+-0.1, 0.06, 0.05). The export stands 0.30 tall.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, srgb, inside, unit, eye_look, args, sibling

bb = sibling("blipbug")
NAVY = bb.NAVY
SHELL, DOT = "#EDB93A", "#3D3F7E"
BODY, RIM, BLUE, DEEP, GLINT = "#EA8A3A", "#D8502A", "#86D8F2", "#2C86C4", "#FFFFFF"

DC = np.array([0.0, 0.01, 0.128])       # the dome's middle
DR = 0.158                                # its radius (to a corner)
WIDE = 1.2                               # wider than it is tall
TILT = -16                                # degrees about X: the front rim raised over the face
DOT_T = 0.24                              # how far along each edge a corner's dot reaches


def ico():
    """An icosahedron's twelve corners on the unit sphere: the top, the upper ring, the lower ring (a corner in
    front), the bottom; and its edges."""
    z, r = 1 / math.sqrt(5), 2 / math.sqrt(5)
    P = [(0, 0, 1)]
    P += [(r * math.sin(math.radians(36 + 72 * i)), -r * math.cos(math.radians(36 + 72 * i)), z) for i in range(5)]
    P += [(r * math.sin(math.radians(72 * i)), -r * math.cos(math.radians(72 * i)), -z) for i in range(5)]
    P += [(0, 0, -1)]
    P = np.array(P)
    d = np.linalg.norm(P[:, None] - P[None], axis=2)
    edge = d[0, 1]
    E = [(i, j) for i in range(12) for j in range(i + 1, 12) if abs(d[i, j] - edge) < 1e-6]
    return P, E


def place(P, c=DC, r=DR, wide=WIDE, tilt=TILT):
    """The unit dome's points into the world: widened, tilted, moved onto its middle."""
    P = np.array(P, float) * np.array([wide, wide, 1.0]) * r
    a = math.radians(tilt)
    y = P[:, 1] * math.cos(a) - P[:, 2] * math.sin(a)
    z = P[:, 1] * math.sin(a) + P[:, 2] * math.cos(a)
    return np.stack([P[:, 0], y, z], 1) + c


def dome(k, shell=SHELL, dot=DOT, bone="body", **kw):
    """The faceted dome (every corner but the bottom one) and a navy dot over each of its corners."""
    P, E = ico()
    keep = list(range(11))
    k.add("dome", hull(place(P[keep], **kw)), shell, bone, fuse=False)
    for i in keep:
        nb = [j for a, b in E for j in ((b,) if a == i else (a,) if b == i else ()) if j in keep]
        pts = [P[i]] + [P[i] + DOT_T * (P[j] - P[i]) for j in nb]
        k.add(f"dot{i}", hull(place(np.array(pts) * 1.012, **kw)), dot, bone, fuse=False)


EYE_D = (0.27, -1.0, -0.2)
EYE_HALF = (0.028, 0.031)
BC = np.array([0.0, -0.01, 0.072])      # the body's middle
BR = np.array([0.112, 0.11, 0.066])


def goggle(u, v, rim=srgb(RIM), blue=srgb(BLUE), deep=srgb(DEEP), glint=srgb(GLINT)):
    """The eye: a thick orange-red rim round light blue, a deeper blue upright middle, a glint."""
    r = np.sqrt(u * u + v * v)
    c = np.tile(blue, (len(u), 1))
    c[(u / 0.4) ** 2 + (v / 0.58) ** 2 < 1] = deep
    c[(u - 0.12) ** 2 + (v - 0.2) ** 2 < 0.2 ** 2] = glint
    c[r > 0.72] = rim
    return c


def keep_rim(look, rim=srgb(RIM)):
    """Whatever the eye does, its rim stays."""
    def f(u, v):
        c = look(u, v)
        c[np.sqrt(u * u + v * v) > 0.72] = rim
        return c
    return f


def body(k):
    k.add("body", ell(BC, BR), BODY, "body")
    for i, (bone, y) in enumerate((("leg{s}", -0.062), ("hind{s}", 0.062))):
        k.add(f"foot{i}", ell((0.092, y, 0.017), (0.026, 0.032, 0.018)), NAVY, bone, mirror=True)
    bb.prongs(k, "side", (0.1, 0.04, 0.05), (1, 0.75, 0.3), (1, 1.1, -0.05), 0.1, 0.032, "body", mirror=True)


def eyes(k, state):
    p, n = bb.on_ell(EYE_D, BC, BR)
    look = keep_rim(eye_look(state, goggle, srgb(BODY), srgb("#3A1E18"), line=0.16))
    k.decal(p, n, EYE_HALF, look, eps=0.002, rings=20, segs=56, mirror=True)


def dottler(src, dst):
    k = Kit(src, coat="shell")
    body(k)
    dome(k)
    k.build(voxel=0.004, smooth=4, band=0.006, wband=0.03)
    k.eye_states(lambda s: eyes(k, s))
    k.export(dst)


if __name__ == "__main__":
    dottler(*args())
