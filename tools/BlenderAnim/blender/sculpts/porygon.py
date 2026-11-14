# Porygon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/porygon.py -- <the export's Porygon.glb> <out.glb>
#
# The design, from the references: a bird-like figure of flat faces and hard edges, like an early computer model. A
# long pink head of facets (an octagonal block bevelled all round, narrowing to its front), a cyan wedge of a beak
# under its front pointing forward and down, a large white hexagonal eye with a small square black pupil on the
# facet that turns the head's side toward its front; a pink faceted trunk, cyan tapered blocks for front legs under
# the chest, big cyan boxes for hind legs and a tall thin cyan pyramid of a tail standing up behind.
#
# Every piece is a loose convex hull whose faces each keep their own points (so they stay flat in the game); the
# only fused parts are two thin pink plates set just proud of the eye facets, to carry the eye patches.
#
# The line's frame (Porygon2 and Porygon-Z copy it): PINK and BLUE, the eye's colour function (eye_open) and the way
# an eye is laid (eye()).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import bmesh
import numpy as np
from sculptkit import Kit, ell, srgb, unit, eye_look, polygon, boxy, args

PINK, BLUE = "#EA5379", "#66C6E6"
WHITE, INK, PUPIL = "#F7F7F8", "#2A2430", "#16141A"


# --- hard-edged pieces ------------------------------------------------------------------------------------------------
def hull(points):
    """A convex hull with its coplanar faces merged, each face its own points (flat in the game), wound outward."""
    bm = bmesh.new()
    for p in np.array(points, float).tolist():
        bm.verts.new(p)
    h = bmesh.ops.convex_hull(bm, input=bm.verts[:])
    bmesh.ops.delete(bm, geom=list({g for g in h["geom_interior"] + h["geom_unused"] if isinstance(g, bmesh.types.BMVert)}),
                     context="VERTS")
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(1), verts=bm.verts[:], edges=bm.edges[:])
    faces = [[np.array(v.co[:]) for v in f.verts] for f in bm.faces]
    bm.free()
    mid = np.mean([p for f in faces for p in f], axis=0)
    V, F = [], []
    for f in faces:
        c = np.mean(f, axis=0)
        n = np.zeros(3)
        for a, b in zip(f, f[1:] + f[:1]):           # Newell's normal of the face as listed
            n += np.cross(a - c, b - c)
        if np.dot(n, c - mid) < 0:
            f = f[::-1]
        o = len(V)
        V.extend(p.tolist() for p in f)
        F.append(list(range(o, o + len(f))))
    return np.array(V), F


def section(y, w, zb, zt, bev, x0=0.0):
    """An octagonal cross-section across the body at y: half width w, bottom zb, top zt, its corners bevelled."""
    b = min(bev, w * 0.6)
    pts = []
    for s in (1, -1):
        pts += [(x0 + s * w, y, zb + bev), (x0 + s * w, y, zt - bev), (x0 + s * (w - b), y, zt), (x0 + s * (w - b), y, zb)]
    return pts


def mirrored(points):
    """The points and their twins across the middle: a hull over both is the whole piece."""
    P = np.array(points, float)
    return np.vstack([P, P * np.array([-1, 1, 1])])


# --- the eye: white hexagon, a square black pupil of its own -----------------------------------------------------------
EYE_HALF = 0.048                                   # the eye's half width
HEX = [(1.0, 0.0), (0.5, 0.86), (-0.5, 0.86), (-1.0, 0.0), (-0.5, -0.86), (0.5, -0.86)]
PUPIL_HALF = 0.27                                   # the pupil square's half side, a share of the eye's half width
CW, CI, CP = srgb(WHITE), srgb(INK), srgb(PUPIL)


def eye_open(u, v):
    return np.tile(CW, (len(u), 1))


def eye(k, state, at, n, up, lid=PINK):
    """An eye on a flat plate: a hexagon of white (or a state of it), and in Open and Fierce a square pupil above it."""
    k.decal(at, n, (EYE_HALF, EYE_HALF), eye_look(state, eye_open, srgb(lid), CI, line=0.16), up=up, eps=0.0025,
            outline=polygon(HEX), rings=20, segs=56, mirror=True)
    if state in ("Open", "Fierce"):
        dv = 0.0 if state == "Open" else -0.22
        p = PUPIL_HALF
        k.decal(np.array(at) + np.array(up) * dv * EYE_HALF, n, (EYE_HALF, EYE_HALF), PUPIL, up=up, eps=0.0042,
                outline=polygon([(p, p), (-p, p), (-p, -p), (p, -p)]), rings=8, segs=40, mirror=True)


def plate(c, n, up, half, thick=0.0075, proud=0.0011):
    """A thin flat plate on the plane through c with normal n, its front standing proud of that plane: the fused ground
    a patch is cast on where the piece under it is loose."""
    n = unit(*n); up = np.array(up, float); right = np.cross(up, n); right /= np.linalg.norm(right); up = np.cross(n, right)
    hu, hv = half if isinstance(half, tuple) else (half, half)
    V, F = ell((0, 0, 0), (hu, hv, thick), deform=boxy(5))
    M = np.stack([right, up, n], axis=1)
    return V @ M.T + np.array(c, float) + n * (proud - thick), F


# --- Porygon -------------------------------------------------------------------------------------------------------------
# The reference's measures: head and beak some 0.6 of the height, the head's bottom low over the chest, the trunk low
# with only a short stub of each leg showing under it.
HC = np.array([0.0, -0.11, 0.35])                   # the head's middle (its joint is at 0, -0.06, 0.32)
BEV = 0.026
HEAD = [(-0.17, 0.04, -0.10, 0.10),                 # (y, half width, bottom, top) about HC, front to back
        (-0.07, 0.11, -0.125, 0.125),               # the brow: the top slopes from here down to the back
        (0.06, 0.11, -0.125, 0.0698),               # (on the line from the brow's top to the back's, so the top is one facet)
        (0.13, 0.07, -0.08, 0.04)]
TOP_PINK = "#F49CB1"                                # the pale sloping top facet of the art


def top_facet():
    """A thin pale slab over the head's sloping top, from the brow to the back, just proud of it."""
    tops = [(HC[1] + y, w - min(BEV, w * 0.6), HC[2] + zt) for y, w, zb, zt in HEAD[1:]]
    a, b = np.array([0, tops[0][0], tops[0][2]]), np.array([0, tops[-1][0], tops[-1][2]])
    d = unit(*(b - a)); n = unit(0, -d[2], d[1])    # the top facet's normal, up and a little back
    if n[2] < 0:
        n = -n
    pts = []
    for y, w, z in tops:
        for s in (1, -1):
            p = np.array([s * (w - 0.002), y, z])
            pts += [p + n * 0.0016, p - n * 0.006]
    return hull(pts)


def head_points():
    pts = []
    for y, w, zb, zt in HEAD:
        pts += section(HC[1] + y, w, HC[2] + zb, HC[2] + zt, BEV)
    return pts


def eye_place():
    """The middle, normal and up of the eye on the head's +X facet that runs from its front section to the next."""
    (y0, w0, b0, t0), (y1, w1, b1, t1) = HEAD[0], HEAD[1]
    a = np.array([w0, HC[1] + y0, 0.0]); b = np.array([w1, HC[1] + y1, 0.0])
    t = 0.62
    m = a + (b - a) * t
    d = unit(*(b - a))
    n = unit(d[1], -d[0], 0)                        # outward: +X and toward the front
    return np.array([m[0], m[1], HC[2] + 0.035]), n, (0, 0, 1)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")

    k.add("head", hull(head_points()), PINK, "head", fuse=False)
    k.add("headtop", top_facet(), TOP_PINK, "head", fuse=False)
    # The beak: half the head, a long faceted wedge thrust forward out of the head's lower front, as deep at its root
    # as the head's front is tall, tapering to a blunt end a little lower; its top kept under the eyes
    beak = []
    for y, w, zb, zt, bev in ((-0.19, 0.098, 0.215, 0.338, 0.024), (-0.34, 0.072, 0.19, 0.29, 0.02),
                              (-0.48, 0.036, 0.168, 0.228, 0.012)):
        beak += section(y, w, zb, zt, bev)
    k.add("beak", hull(beak), BLUE, "head", fuse=False)
    # The trunk: bevelled sections from chest to rump, low and deep
    body = []
    for y, w, zb, zt in ((-0.06, 0.08, 0.10, 0.27), (0.04, 0.112, 0.075, 0.30), (0.15, 0.102, 0.085, 0.29),
                         (0.225, 0.066, 0.115, 0.255)):
        body += section(y, w, zb, zt, BEV)
    k.add("trunk", hull(body), PINK, "body", fuse=False)
    # The front legs: cyan wedges, wide under the chest and tapering to a short foot
    k.add("fore", hull([(0.022, -0.12, 0.15), (0.108, -0.12, 0.15), (0.022, 0.03, 0.14), (0.108, 0.03, 0.14),
                        (0.042, -0.09, 0.0), (0.098, -0.09, 0.0), (0.042, -0.03, 0.0), (0.098, -0.03, 0.0)]),
          BLUE, "leg{s}", fuse=False, mirror=True)
    # The hind legs: big cyan boxes, leaning a little back
    hind = []
    for dx in (0.05, 0.14):
        for y, z in ((0.10, 0.19), (0.215, 0.19), (0.115, 0.0), (0.235, 0.0)):
            hind.append((dx, y, z))
    k.add("hind", hull(hind), BLUE, "body", fuse=False, mirror=True)
    # The tail: a tall tapering four-sided block rising behind the body, its foot a third as wide as it is tall
    k.add("tail", hull([(0.04, 0.09, 0.26), (-0.04, 0.09, 0.26), (0.04, 0.2, 0.26), (-0.04, 0.2, 0.26),
                        (0.01, 0.215, 0.5), (-0.01, 0.215, 0.5), (0.01, 0.245, 0.5), (-0.01, 0.245, 0.5)]), BLUE,
          "tail", fuse=False)

    # The plates the eyes lie on
    at, n, up = eye_place()
    k.add("plate", plate(at, n, up, (EYE_HALF * 1.12, EYE_HALF * 0.97)), PINK, "head", mirror=True)

    ob = k.build(voxel=0.0028, smooth=2)
    # The plates are flat like the facets they lie on: shaded flat, they take the facet's own light, not a square of
    # their own (the hulls keep their own points a face, so flat shading changes nothing of theirs)
    ob.data.polygons.foreach_set("use_smooth", [False] * len(ob.data.polygons))
    k.eye_states(lambda state: eye(k, state, at, n, up))
    k.export(out)
