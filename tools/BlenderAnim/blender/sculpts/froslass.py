# Froslass sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/froslass.py -- <the export's Froslass.glb> <out.glb>
# Grown from snorunt.py: Snorunt's pale blue in its eyes, now as irises in yellow eyes, and the dark face of the line
# turned purple; its cloak become a white kimono.
#
# The design, from the references: a slim floating ghost of ice. A white head wider than tall, a hood whose sides
# flare out into two points, pale blue beneath them; two pale blue ice crystals standing on its crown; a purple face
# like a mask: a patch round each eye drawn out to a point up and outward, joined below the eyes and running down to a
# pointed chin, white between the eyes over the brow; heavy-lidded yellow eyes with pale blue irises and dark slit
# pupils. Long white sleeves that are its arms hang from under the hood's sides, flaring into wide pale blue cuffs
# with a white hexagon on each, small white three-fingered hands at their inner edges. A slim white body, a red-orange
# sash round the waist with a fin of it standing out behind to one side, the body below it swelling a little and
# tapering to a point: no legs.
# The skeleton: body (0, 0, 0.446), tail (0, 0.01, 0.306), arms (+-0.08, -0.02, 0.526), head (0, 0, 0.606).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import bmesh
import numpy as np
from mathutils.kdtree import KDTree
from sculptkit import Kit, ell, tube, blade, leaf, srgb, inside, smoothstep, unit, tri, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

WHITE, SHADE, ICEB, DEEPB, SASH, SASHD = "#F4F4FA", "#E6E8F0", "#9ED6F0", "#74BCE4", "#E4502C", "#B8381E"
PURPLE, PURPLED, CRYS = "#7A3E9C", "#4A1E64", "#A6DCF4"
W_, B_, D_ = srgb(WHITE), srgb(ICEB), srgb(DEEPB)

# Loose pieces coloured vertex by vertex (as mismagius.py): added in one colour, recoloured after the build
graded = []
def gadd(name, shape, colours, bone, surface=None):
    V, F = shape
    k.add(name, (V, F), WHITE, bone, fuse=False, surface=surface)
    graded.append((np.array(V, float), np.array(colours, float)))

def recolour(ob):
    V = np.vstack([g[0] for g in graded]); C = np.vstack([g[1] for g in graded])
    kd = KDTree(len(V))
    for i, v in enumerate(V.tolist()):
        kd.insert(v, i)
    kd.balance()
    me = ob.data
    n = len(me.vertices)
    co = np.empty(n * 3, np.float32); me.vertices.foreach_get("co", co); co = co.reshape(n, 3)
    vc = {}
    for i, p in enumerate(co.tolist()):
        _, j, d = kd.find(p)
        if d < 1e-6:
            vc[i] = C[j]
    loops = np.empty(len(me.loops), np.int32); me.loops.foreach_get("vertex_index", loops)
    attr = me.color_attributes["Color"]
    rgba = np.empty(len(me.loops) * 4, np.float32); attr.data.foreach_get("color", rgba); rgba = rgba.reshape(-1, 4)
    for li, vi in enumerate(loops.tolist()):
        if vi in vc:
            rgba[li, :3] = vc[vi]
    attr.data.foreach_set("color", rgba.ravel())


def hull(V):
    bm = bmesh.new()
    for v in V.tolist():
        bm.verts.new(v)
    h = bmesh.ops.convex_hull(bm, input=bm.verts[:])
    bmesh.ops.delete(bm, geom=list({g for g in h["geom_interior"] + h["geom_unused"] if isinstance(g, bmesh.types.BMVert)}),
                     context="VERTS")
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(2), verts=bm.verts[:], edges=bm.edges[:])
    bm.verts.index_update()
    out_ = (np.array([v.co[:] for v in bm.verts]), [[v.index for v in f.verts] for f in bm.faces])
    bm.free()
    return out_


FLAT = []               # loose pieces drawn with flat faces (the crystals)

# ---- the head: a white dome wider than tall, a flattened face in front, a chin, the hood's flaring points ----------
HC = np.array([0.0, 0.0, 0.69])
k.add("head", ell(HC, (0.135, 0.118, 0.118)), WHITE, "head")
k.add("face", ell((0, -0.05, 0.655), (0.105, 0.075, 0.1)), WHITE, "head")
k.add("chin", ell((0, -0.05, 0.575), (0.05, 0.045, 0.045)), WHITE, "head")
for s_ in (1, -1):
    k.add("flare" + ("R" if s_ > 0 else "L"),
          blade([(0.08 * s_, 0.01, 0.69), (0.15 * s_, 0.015, 0.715), (0.205 * s_, 0.02, 0.75)], [0.07, 0.04, 0.005],
                (0, 1, 0), 0.5), WHITE, "head")

# Two ice crystals on the crown: hexagonal, a little apart and leaning out, their tops cut in facets
for s_ in (1, -1):
    base = np.array([0.055 * s_, 0.005, 0.785])
    d = unit(0.35 * s_, 0.05, 1)
    a1 = np.cross(d, (0, 1.0, 0)); a1 /= np.linalg.norm(a1); a2 = np.cross(d, a1)
    pts = []
    for h, r in ((0.0, 0.026), (0.042, 0.031), (0.068, 0.012)):
        for j in range(6):
            t = 2 * math.pi * j / 6 + 0.3
            pts.append(base + d * h + r * (math.cos(t) * a1 + math.sin(t) * a2))
    pts.append(base + d * 0.074)
    V, F = hull(np.array(pts))
    k.add("crystal" + ("R" if s_ > 0 else "L"), (V, F), CRYS, "head", fuse=False, surface="shell")
    FLAT.append(V)

# ---- the body: a slim white torso, the sash round its waist, the skirt below swelling and tapering to a point -------
k.add("torso", tube([(0, 0.0, 0.58), (0, 0.004, 0.5), (0, 0.008, 0.43), (0, 0.01, 0.37)],
                    [0.036, 0.046, 0.06, 0.072], seg=28), WHITE, "body")
k.add("waist", ell((0, 0.01, 0.345), (0.074, 0.066, 0.03)), SASH, "body")
k.add("skirt", tube([(0, 0.01, 0.34), (0, 0.015, 0.27), (0, 0.02, 0.2), (0, 0.025, 0.14), (0, 0.03, 0.1)],
                    [0.072, 0.088, 0.078, 0.045, 0.01], seg=28), WHITE, "tail")
# The sash's fin, standing out behind to the model's right (+X) and up a little
fd = unit(0.75, 0.6, 0.35)
fn = np.cross(fd, (0, 0, 1.0)); fn /= np.linalg.norm(fn)        # the fin stands upright, broad seen from the front
f0 = np.array([0.03, 0.05, 0.345])
k.add("fin", blade([f0, f0 + fd * 0.06 + np.array([0, 0, 0.012]), f0 + fd * 0.13 + np.array([0, 0, 0.012]), f0 + fd * 0.19],
                   [0.03, 0.055, 0.045, 0.004], fn, 0.22), SASH, "body", fuse=False, surface="cloth")
k.add("finroot", ell((0.035, 0.055, 0.345), (0.03, 0.03, 0.025)), SASH, "body")

# ---- the sleeves: white tubes from under the hood's sides down past the arms' joints, flaring into wide cuffs ------
for s_, side in ((1, "R"), (-1, "L")):
    bone = "arm" + side
    k.add("sleeve" + side, tube([(0.09 * s_, 0.0, 0.62), (0.098 * s_, -0.015, 0.526), (0.1 * s_, -0.02, 0.43),
                                 (0.11 * s_, -0.035, 0.35)], [0.034, 0.027, 0.026, 0.03], seg=20), WHITE, bone,
          fuse=False, surface="cloth")
    # The cuff: a flattened bell, narrow at the top and wide and rounded below, white above and pale blue below
    def bell(V):
        V = V.copy()
        t = (1 - V[:, 2]) / 2                                  # 0 at the top, 1 at the bottom
        V[:, 0] *= 0.45 + 0.65 * np.sqrt(np.clip(t, 0, 1))
        return V
    cc = np.array([0.125 * s_, -0.045, 0.29])
    V, F = ell(cc, (0.062, 0.03, 0.12), rot=(0, -6 * s_, 0), deform=bell, sub=4)
    t = np.clip((0.41 - V[:, 2]) / 0.24, 0, 1)
    m = smoothstep(0.14, 0.3, t)
    col = W_[None] * (1 - m[:, None]) + B_[None] * m[:, None]
    deep = smoothstep(0.82, 1.0, t)
    col = col * (1 - deep[:, None]) + D_[None] * deep[:, None]
    gadd("cuff" + side, (V, F), col, bone, surface="cloth")
    # The white hexagon on the cuff's front, a pale blue middle in it
    front = np.array([0.128 * s_, -0.045 - 0.031, 0.26])
    for r, c_, lift in ((0.022, WHITE, 0.0), (0.011, ICEB, 0.003)):
        pts = []
        for h in (-0.004, 0.004 + lift):
            for j in range(6):
                a = 2 * math.pi * j / 6
                pts.append(front + np.array([r * math.cos(a), h, r * math.sin(a)]))
        V, F = hull(np.array(pts))
        k.add(("hex" if r > 0.015 else "hexmid") + side, (V, F), c_, bone, fuse=False, surface="cloth")
    # A small white hand at the cuff's inner lower edge, three short fingers pointing down and in
    hp = np.array([0.085 * s_, -0.07, 0.2])
    k.add("palm" + side, ell(hp, (0.022, 0.016, 0.024)), WHITE, bone, fuse=False, surface="skin")
    for j, (dx, dz) in enumerate(((-0.013, -0.004), (-0.002, -0.012), (0.01, -0.008))):
        p0 = hp + np.array([dx * s_, -0.004, dz])
        p1 = p0 + np.array([(dx * 0.6 - 0.006) * s_, -0.004, -0.024])
        k.add(f"finger{j}" + side, tube([p0, p1], [0.0065, 0.0055], seg=10, per=4), WHITE, bone, fuse=False,
              surface="skin")


# ---- the colours painted on the fused body ---------------------------------------------------------------------------
def arc(P):
    """Coordinates round the head's upright axis: (angle from the front toward +X times the radius, height)."""
    return np.arctan2(P[:, 0], -(P[:, 1] - 0.0)) * 0.12, P[:, 2]

EYE_Z, EYE_S = 0.672, 0.046        # the eyes' height and how far each is from the middle (arc length)
def face_mask(P):
    s, z = arc(P)
    front = smoothstep(-0.0, -0.03, P[:, 1])
    a = np.abs(s)
    # round each eye, drawn out to a point up and outward
    d1 = np.sqrt(((a - EYE_S) / 0.04) ** 2 + ((z - EYE_Z + 0.004) / 0.042) ** 2) - 1
    t = np.clip(((a - EYE_S) * 0.6 + (z - EYE_Z) * 0.8) / 0.075, 0, 1)
    ax, az = EYE_S + 0.6 * 0.075 * t, EYE_Z + 0.8 * 0.075 * t
    d2 = np.sqrt((a - ax) ** 2 + (z - az) ** 2) - (0.03 * (1 - t) + 0.006)
    # below the eyes, joined, narrowing down to the chin's point
    w = 0.088 * np.clip((z - 0.535) / (0.655 - 0.535), 0, 1) ** 0.85
    d3 = np.maximum(a - w, z - 0.66)
    d = np.minimum(np.minimum(d1 * 0.04, d2), d3)
    return inside(d, 0.004) * front
k.paint(face_mask, PURPLE)
# The pale blue under the hood's flaring points
k.paint(lambda P: inside(P[:, 2] - (0.682 + 0.55 * (np.abs(P[:, 0]) - 0.08)), 0.003)
        * smoothstep(0.1, 0.12, np.abs(P[:, 0])), ICEB, on=["flare"])
# The sash round the waist
k.paint(lambda P: inside(np.abs(P[:, 2] - 0.345) - 0.024, 0.0025), SASH)

body = k.build(voxel=0.0045, smooth=3)
recolour(body)

# The crystals' faces drawn flat, found by their points
me = body.data
kd = KDTree(sum(len(v) for v in FLAT))
i = 0
for Vh in FLAT:
    for p in Vh.tolist():
        kd.insert(p, i); i += 1
kd.balance()
co = np.empty(len(me.vertices) * 3, np.float32); me.vertices.foreach_get("co", co); co = co.reshape(-1, 3)
onflat = np.array([kd.find(p)[2] < 1e-6 for p in co.tolist()])
me.polygons.foreach_set("use_smooth", [not all(onflat[v] for v in p.vertices) for p in me.polygons])

# ---- the eyes: yellow, heavy-lidded, a pale blue iris toward the middle with a dark slit pupil; the lid purple ------
YEL, IRIS, PUP, LID, INK = srgb("#F2D03A"), srgb("#7CC8EE"), srgb("#1A1A2C"), srgb(PURPLE), srgb(PURPLED)
def eye(u, v):
    c = np.tile(YEL, (len(u), 1))
    c[((u + 0.28) / 0.42) ** 2 + ((v + 0.12) / 0.6) ** 2 < 1] = IRIS
    c[((u + 0.28) / 0.17) ** 2 + ((v + 0.12) / 0.48) ** 2 < 1] = PUP
    c[u * u + v * v > 0.86] = INK
    lid = 0.24 + 0.08 * u
    c[v > lid + 0.12] = LID
    c[np.abs(v - lid) <= 0.15] = INK
    return c
def eyes(state):
    for s_ in (1, -1):
        a = EYE_S / 0.12 * s_
        n = unit(math.sin(a), -math.cos(a), 0.12)
        p = np.array([0.12 * math.sin(a), -0.12 * math.cos(a), EYE_Z])
        look = eye_look(state, eye, LID, INK)
        # a patch laid by hand has u toward +X: on the left eye that is inward, so its u is turned round
        k.decal(p, n, (0.03, 0.028), (lambda u, v, look=look, f=s_: look(u * f, v)), eps=0.004, rings=20, segs=56)

k.eye_states(eyes)
k.export(out)
