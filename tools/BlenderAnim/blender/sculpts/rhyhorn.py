# Rhyhorn sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rhyhorn.py -- <the export's Rhyhorn.glb> <out.glb>
# The first of its line (rhydon.py and rhyperior.py are grown from this file): a low grey rhinoceros of rock. From the
# references: a huge wedge of a head, low at the snout and rising back to a broad top whose two corners flare up as
# points with a dark hole in each; a ridge up the forehead's middle; a short stout cone of a horn on the nose; small red
# eyes on the head's sides under a heavy brow, scowling; a long mouth line along the jaw with a white fang turned up at
# each corner; behind the head a tall plate of rock across the back, notched into two peaks; angular plates of rock
# over the back and flanks, their trailing edges lifted; four stumpy legs with three white claws a foot; a short tail.
# The plates are hard loose pieces (convex hulls drawn flat-faced), as Graveler's and Golem's rocks are.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import bmesh
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="shell")

# --- the line's colours -------------------------------------------------------------------------------------------
GREY, GREYDARK, HORN, CLAW, HOLE, MOUTHLINE = "#9AA6B6", "#727E90", "#C2CAD6", "#EEF0F2", "#262830", "#30343E"
IRIS, PUPIL, SCLERA, INK = "#D8302C", "#2A0A0A", "#F4F0EC", "#22262E"


def shade(h, f):
    """A colour a little lighter or darker (f above or below 1), still written as on screen."""
    c = np.clip(np.array([int(h[i:i + 2], 16) for i in (1, 3, 5)]) * f, 0, 255).astype(int)
    return "#%02X%02X%02X" % tuple(c)


def hull(V):
    """Points made into a solid: their convex hull, its coplanar faces merged (golem.py's plates)."""
    bm = bmesh.new()
    for v in np.asarray(V).tolist():
        bm.verts.new(v)
    h = bmesh.ops.convex_hull(bm, input=bm.verts[:])
    bmesh.ops.delete(bm, geom=list({g for g in h["geom_interior"] + h["geom_unused"] if isinstance(g, bmesh.types.BMVert)}), context="VERTS")
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(3), verts=bm.verts[:], edges=bm.edges[:])
    bm.verts.index_update()
    o = (np.array([v.co[:] for v in bm.verts]), [[v.index for v in f.verts] for f in bm.faces])
    bm.free()
    return o


ROCKS = []              # the loose pieces drawn flat-faced


def rock_plate(name, c, n, d, length, width, lift, thick, bone, color=GREY, tone=1.0, sink=0.03):
    """
    A plate of rock lying on the body: its middle c on the surface, n the surface's outward normal, d the way its
    point runs (back, out). A pointed pentagon in the surface's plane, its trailing edge and point lifted off the body
    by lift, thick at the root, sunk into the body by sink: an angular shingle with hard edges.
    """
    c, n, d = np.array(c, float), unit(*n), np.array(d, float)
    d = d - n * (d @ n); d /= np.linalg.norm(d)
    s = np.cross(n, d)
    outline = [(-0.5, -0.42), (-0.5, 0.42), (0.12, 0.5), (0.5, 0.0), (0.12, -0.5)]
    pts = []
    for a, b in outline:
        t = a + 0.5                                     # 0 at the leading edge, 1 at the point
        p = c + d * a * length + s * b * width
        pts.append(p + n * (thick + lift * t))
        pts.append(p - n * sink)
    k.add(name, hull(np.array(pts)), shade(color, tone), bone, fuse=False, surface="shell")
    ROCKS.append(name)


def fang(name, base, up, length, width, bone):
    """A small white fang, a cone from base along up."""
    base, up = np.array(base, float), unit(*up)
    k.add(name, tube([base - up * 0.006, base + up * length * 0.5, base + up * length],
                     [width, width * 0.6, 0.002], seg=10, per=4), CLAW, bone, fuse=False, surface="shell")


def claws(tag, foot, toward, bone, n=3, spread=0.034, size=0.022):
    """Three white claws along a foot's front edge, short stout cones turned forward and down."""
    foot = np.array(foot, float)
    for j in range(n):
        dx = (j - (n - 1) / 2) * spread
        b = foot + np.array([dx, 0, 0])
        d = unit(dx * 1.2, toward, -0.35)
        k.add(f"claw{tag}{j}", tube([b - d * 0.01, b + d * size * 0.8, b + d * size * 1.5],
                                    [size * 0.62, size * 0.4, 0.002], seg=10, per=4), CLAW, bone, fuse=False,
              surface="shell")


# --- the frame: the skeleton's joints and the masses on them -----------------------------------------------------
BODY_C, BODY_R = np.array([0.0, 0.07, 0.225]), np.array([0.2, 0.25, 0.175])
HEAD = np.array([0.0, -0.18, 0.27])                     # the head bone
LEGS = [("leg", -0.1, "leg{s}"), ("hind", 0.16, "hind{s}")]
LEG_X = 0.13

# The body: a deep barrel, its belly low over the short legs
def barrel(V):
    V = V.copy()
    low = np.clip(-V[:, 2], 0, 1)
    V[:, :2] *= (1 + 0.12 * low)[:, None]
    return V
k.add("body", ell(BODY_C, BODY_R, deform=barrel), GREY, "body")
# a heavy belly slung low between the legs, so that only their stumps show
k.add("belly", ell((0, 0.04, 0.14), (0.17, 0.25, 0.1), deform=boxy(2.4)), GREY, "body")
k.add("tail", tube([(0, 0.28, 0.25), (0, 0.36, 0.22), (0, 0.42, 0.17)], [0.06, 0.04, 0.012]), GREY, "tail")

# The legs: stumpy columns on round feet, three white claws a foot
for name, y, bone in LEGS:
    k.add(name, tube([(LEG_X, y, 0.2), (LEG_X + 0.01, y - 0.005, 0.1), (LEG_X + 0.015, y - 0.01, 0.05)],
                     [0.085, 0.082, 0.08]), GREY, bone, mirror=True)
    k.add(name + "foot", ell((LEG_X + 0.015, y - 0.025, 0.037), (0.084, 0.088, 0.04)), GREY, bone, mirror=True)
    for s, side in ((1, "R"), (-1, "L")):
        claws(name + side, (s * (LEG_X + 0.01), y - 0.1, 0.03), -1, bone.format(s=side))

# --- the head: a great wedge, low at the snout and rising to a broad top --------------------------------------------
def shield(V):
    """The head's back half: a wide, tall, flat-sided block, wider at the top than at the bottom."""
    V = boxy(3.2)(V)
    V[:, 0] *= 0.86 + 0.16 * V[:, 2]
    return V
k.add("cranium", ell((0, -0.2, 0.325), (0.175, 0.115, 0.145), deform=shield), GREY, "head")
def wedge(V):
    """The face: narrower toward the snout and lower toward it, its sides flat planes."""
    V = boxy(3.0)(V)
    V[:, 0] *= 1 + 0.2 * V[:, 1]
    V[:, 2] -= 0.25 * np.clip(-V[:, 1], 0, 1) * np.clip(V[:, 2] + 0.2, 0, 1.2)
    return V
k.add("face", ell((0, -0.32, 0.235), (0.145, 0.15, 0.125), deform=wedge), GREY, "head")
k.add("snout", ell((0, -0.43, 0.155), (0.088, 0.075, 0.072), deform=boxy(2.4)), GREY, "head")
k.add("jaw", ell((0, -0.33, 0.135), (0.11, 0.11, 0.055), deform=boxy(2.4)), GREY, "head")
# the ridge up the forehead's middle, from the horn to the top
k.add("ridge", tube([(0, -0.42, 0.24), (0, -0.34, 0.33), (0, -0.25, 0.45)], [0.03, 0.03, 0.028]), GREY, "head")
# the brow over each eye: the edge of the face's side, a long flat ledge slanting down toward the snout
k.add("brow", ell((0.1, -0.33, 0.285), (0.045, 0.1, 0.024), rot=(-24, 0, -6), deform=boxy(3)), GREY, "head",
      mirror=True)
# the top's two corners flaring up and out as flat points, each with a dark hole in its front face
EAR0, EAR1 = np.array([0.13, -0.2, 0.41]), np.array([0.215, -0.18, 0.515])
k.add("ear", blade([EAR0, (EAR0 + EAR1) / 2 + (0.012, 0, 0), EAR1], [0.065, 0.045, 0.012], (0, 1, 0), 0.55), GREY,
      "head", mirror=True)

# the horn on the nose: a short stout cone, a little forward
HB, HT = np.array([0, -0.43, 0.22]), np.array([0, -0.48, 0.35])
k.add("horn", tube([HB - (HT - HB) * 0.15, HB + (HT - HB) * 0.45, HT], [0.062, 0.038, 0.005], seg=20, per=6), HORN,
      "head", fuse=False, surface="shell")

# a fang turned up at each corner of the mouth
fang("fangR", (0.078, -0.395, 0.12), (0.15, -0.3, 1), 0.03, 0.012, "head")
fang("fangL", (-0.078, -0.395, 0.12), (-0.15, -0.3, 1), 0.03, 0.012, "head")

# the mouth: a dark line along the jaw from the snout back toward the cheeks, curving up at its ends
def mouth(P):
    y = P[:, 1]
    zline = 0.122 + 0.06 * np.clip((y + 0.4) / 0.12, 0, 1) ** 2
    return inside(np.abs(P[:, 2] - zline) - 0.005, 0.003) * inside(y + 0.27, 0.004) * inside(-0.5 - y, 0.004)
k.paint(mouth, MOUTHLINE, on=["snout", "jaw", "face"])

# --- the plates of rock ------------------------------------------------------------------------------------------
# The tall plate across the back behind the head, its face to the front, notched into two peaks
CREST = [(-0.09, 0.0), (0.09, 0.0), (0.095, 0.2), (0.06, 0.245), (0.03, 0.175), (-0.005, 0.165), (-0.035, 0.275),
         (-0.075, 0.245), (-0.095, 0.14)]
def crest(outline, base, lean, thick, name, bone, color=GREY):
    """A plate standing across the body (its face to the front), from a polygon (x, height), leaning back by lean."""
    base = np.array(base, float)
    up = unit(0, math.sin(math.radians(lean)), math.cos(math.radians(lean)))
    nrm = unit(0, -up[2], up[1])                        # its front face
    # a hull of a non-convex outline loses its notch: build it from convex slices between neighbouring corners
    o = np.array(outline, float)
    mid = o.mean(axis=0)
    for i in range(len(o)):
        a, b = o[i], o[(i + 1) % len(o)]
        tri_ = [mid, a, b]
        pts = []
        for x, h in tri_:
            p = base + np.array([x, 0, 0]) + up * h
            pts += [p + nrm * thick / 2, p - nrm * thick / 2]
        nm = f"{name}{i}"
        k.add(nm, hull(np.array(pts)), color, bone, fuse=False, surface="shell")
        ROCKS.append(nm)
crest(CREST, (0, -0.07, 0.33), 18, 0.05, "crest", "body")

# Plates over the back and flanks, their points running back and out
PLATES = [  # (centre on the body, its point's way, length, width, lift)
    ((0.15, 0.0, 0.34), (0.45, 0.75, 0.45), 0.17, 0.13, 0.05),
    ((0.19, 0.17, 0.27), (0.45, 0.85, 0.1), 0.17, 0.13, 0.045),
    ((0.08, 0.2, 0.37), (0.25, 0.95, 0.2), 0.15, 0.12, 0.04),
]
def body_normal(p):
    q = (np.array(p) - BODY_C) / BODY_R ** 2
    return unit(*q)
for j, (c, d, l, w, lift) in enumerate(PLATES):
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        cc = np.array(c) * m
        rock_plate(f"plate{side}{j}", cc, body_normal(cc), np.array(d) * m, l, w, lift, 0.02, "body",
                   tone=0.94 + 0.05 * ((j * 7 + (s > 0)) % 3))
rock_plate("plateTail", (0, 0.29, 0.33), body_normal((0, 0.29, 0.33)), (0, 1, -0.2), 0.12, 0.1, 0.025, 0.022, "body")

# The dark holes in the head's two points: small slanting slits on their fronts, near the tips
for s in (1, -1):
    e = (EAR0 * 0.4 + EAR1 * 0.6) * np.array([s, 1, 1.0])
    k.decal(e, unit(-0.45 * s, -1, 0.15), (0.009, 0.016), HOLE, up=(0.5 * s, 0, 1), eps=0.004, rings=8, segs=32)


# --- the eyes: small, red with a dark pupil, under the brow sloping down to the snout (scowling) --------------------
CI, CP, CS, CK, LID = srgb(IRIS), srgb(PUPIL), srgb(SCLERA), srgb(INK), srgb(GREY)
def eye(u, v):
    c = np.tile(CI, (len(u), 1))
    c[(u > 0.42) & (u * u + v * v > 0.5 ** 2)] = CS                 # a sliver of white at the back corner alone
    c[((u + 0.15) / 0.3) ** 2 + ((v + 0.08) / 0.4) ** 2 < 1] = CP
    c[((u + 0.02) / 0.14) ** 2 + ((v - 0.12) / 0.14) ** 2 < 1] = (1, 1, 1)
    c[u * u + v * v > 0.8 ** 2] = CK
    edge = 0.35 + 0.35 * u                               # the lid slopes down toward the snout (u inward is -)
    c[v > edge + 0.13] = LID
    c[np.abs(v - edge) <= 0.13] = CK
    return c
EYE_C, EYE_N, EYE_H = np.array([0.112, -0.37, 0.235]), unit(0.68, -0.68, 0.15), (0.034, 0.028)
def eyes(state):
    k.decal(EYE_C, EYE_N, EYE_H, eye_look(state, eye, LID, CK, line=0.16), up=(0, 0.2, 1), eps=0.005, rings=20,
            segs=56, mirror=True)


def finish(voxel=0.007, smooth=3):
    """
    Builds the body with the plates drawn flat-faced. The mesh's faces are the fused surface's, then the loose pieces'
    in the order they were added, then the patches'.
    """
    loose = [(p["name"], len(p["F"])) for p in k.parts if not p["fuse"] and not p["carve"]]
    body = k.build(voxel=voxel, smooth=smooth)
    me = body.data
    flags = np.ones(len(me.polygons), bool)
    at = len(k.surfaces[k.name][1])
    for name, count in loose:
        if name in ROCKS:
            flags[at:at + count] = False
        at += count
    me.polygons.foreach_set("use_smooth", flags.tolist())
    return body


finish()
k.eye_states(eyes)
k.export(out)
