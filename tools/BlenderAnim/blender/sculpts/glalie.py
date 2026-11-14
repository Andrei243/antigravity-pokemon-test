# Glalie sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/glalie.py -- <the export's Glalie.glb> <out.glb>
# Grown from snorunt.py: the same pale blue eyes on the same dark face colour, the same white grin of flat teeth with
# grey lines between them, now set in a head of rock clad in ice.
#
# The design, from the references: a floating round head of black rock held in a LATTICE of thick white ice bars,
# the black showing through large angular gaps all round it (a rhombus on the brow between the horns, big three- and
# four-sided gaps on the cheeks, sides, back and underneath, long gaps under the horns); two faceted black horns at the
# upper sides, pointing out and up and a little back, their roots in gaps of the ice; the bars make a heavy angry brow
# from each horn's root down to a bar between the eyes, which runs on down to the grin; pale blue eyes with black
# pupils in black sockets under the brow; a wide grin of flat white teeth below, framed by ice.
# Built inverted: a black ball, and the ice as round bars laid along great arcs over it. The bars are the edges of a
# Voronoi pattern on the ball, so every gap is a clean polygon and each face gap sits round a seed placed by hand.
# The skeleton is one bone, the body, at z 0.293: everything is on it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import bmesh
import numpy as np
from mathutils.kdtree import KDTree
from sculptkit import Kit, ell, tube, srgb, unit, polygon, eye_look, args

src, out = args()
k = Kit(src, coat="shell")

ICE, ROCK, HORN, WHITE, LINE, BLUE = "#ECEFF7", "#2C2E36", "#34363F", "#F6F6F2", "#8E939C", "#4CB8EC"
C = np.array([0.0, 0.0, 0.30])        # the ball's middle
R = 0.22                              # the ice's outer radius
RB = 0.205                            # the rock ball under it
BAR, BARC = 0.028, RB - 0.014         # a bar's radius and the radius its axis runs at (it stands ~0.014 proud)


def dirv(az, el):
    """A direction from the ball's middle: az degrees round from the front toward +X, el degrees up."""
    a, e = math.radians(az), math.radians(el)
    return np.array([math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)])


def surface(n):
    """Where a direction meets the rock between the bars."""
    return C + n * RB


# --- the rock ball --------------------------------------------------------------------------------------------------
k.add("ball", ell(C, (RB, RB, RB), sub=5), ROCK, "body")

# --- the ice: the edges of the Voronoi cells round these seeds (az, el), each cell a gap of black ---------------------
SEEDS = [(0, 37),                                  # the rhombus on the brow
         (27, 6), (-27, 6),                        # the eye sockets
         (0, -31),                                 # the grin
         (57, 47), (-57, 47),                      # the long gaps under the horns
         (0, 76),                                  # the crown
         (80, -8), (-80, -8),                      # the cheeks
         (56, -66), (-56, -66),                    # low beside the grin
         (0, -86),                                 # underneath
         (100, 18), (-98, 22),                     # the sides
         (104, -34), (-100, -30),
         (128, 52), (-132, 48),                    # the back
         (144, -4), (-140, 2),
         (180, 30), (178, -28),
         (140, -58), (-136, -62),
         (-178, 70)]
P = np.array([dirv(a, e) for a, e in SEEDS])
bm = bmesh.new()
for p in P.tolist():
    bm.verts.new(p)
bmesh.ops.convex_hull(bm, input=bm.verts[:])
bmesh.ops.triangulate(bm, faces=bm.faces[:])
bm.faces.index_update()
corner = {}
for f in bm.faces:
    a, b, c = [np.array(v.co) for v in f.verts]
    nn = np.cross(b - a, c - a); nn /= np.linalg.norm(nn)
    if np.dot(nn, a) < 0:
        nn = -nn
    corner[f.index] = nn
arcs = [(corner[e.link_faces[0].index], corner[e.link_faces[1].index]) for e in bm.edges if len(e.link_faces) == 2]
bm.free()

IV, IF, off = [], [], 0
for a, b in arcs:
    ang = math.acos(float(np.clip(np.dot(a, b), -1, 1)))
    if ang < 1e-3:
        continue
    m = max(2, int(ang / math.radians(8)) + 1)
    pts = []
    for t in np.linspace(0, 1, m + 1):
        d = (math.sin((1 - t) * ang) * a + math.sin(t * ang) * b) / math.sin(ang)
        pts.append(C + d * BARC)
    mid = (a + b) / np.linalg.norm(a + b)
    # The face's bars heavier: the brow over the eyes and the bar between them
    r = BAR * (1.15 if mid[1] < -0.55 and mid[2] > -0.05 else 1.0)
    V, F = tube(pts, [r] * len(pts), seg=12, per=4)
    IV.append(V); IF.extend([[i + off for i in f] for f in F]); off += len(V)
k.add("ice", (np.vstack(IV), IF), ICE, "body")


# --- the horns: faceted black cones at the upper sides, their roots in the gaps under them ----------------------------
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


def horn(root, tip, r0, sides=7, flat=0.75):
    """A cone of a few flat faces from a root to a point, a little flattened front to back."""
    root, tip = np.array(root, float), np.array(tip, float)
    d = tip - root; d /= np.linalg.norm(d)
    s1 = np.cross(d, (0, 1.0, 0)); s1 /= np.linalg.norm(s1)
    s2 = np.cross(d, s1)
    pts = [tip]
    for ring, rr in ((0.0, r0), (0.25, r0 * 0.8)):
        base = root + (tip - root) * ring
        for j in range(sides):
            a = 2 * math.pi * (j + 0.5 * ring * 4) / sides
            pts.append(base + rr * (math.cos(a) * s1 + flat * math.sin(a) * s2))
    return hull(np.array(pts))


FLAT = []               # the loose pieces drawn with flat faces
for s_ in (1, -1):
    root = C + R * 0.8 * dirv(54 * s_, 44)
    tip = root + np.array([0.16 * s_, 0.035, 0.14])
    name = "hornR" if s_ > 0 else "hornL"
    V, F = horn(root, tip, 0.047)
    k.add(name, (V, F), HORN, "body", fuse=False, surface="shell")
    FLAT.append(V)

# --- the grin: Snorunt's, grown wide: a band of flat white teeth with dark lines between them along the top of the
# grin's gap, the dark of the mouth showing in a crescent under them ----------------------------------------------------
DK = srgb(ROCK)          # (painted on the fused body after its build, below)
body = k.build(voxel=0.004, smooth=6, band=0.003)

# The horns' faces drawn flat, so they read as cut rock: found by their points
me = body.data
kd = KDTree(sum(len(v) for v in FLAT))
i = 0
for Vh in FLAT:
    for p in Vh.tolist():
        kd.insert(p, i); i += 1
kd.balance()
co = np.empty(len(me.vertices) * 3, np.float32); me.vertices.foreach_get("co", co); co = co.reshape(-1, 3)
onflat = np.array([kd.find(p)[2] < 1e-6 for p in co.tolist()])
smooth = np.array([not all(onflat[v] for v in p.vertices) for p in me.polygons])
me.polygons.foreach_set("use_smooth", smooth.tolist())

# The bars' edge crisp: the fused body's points coloured again by how far they stand off the rock ball (the kit's
# blend by nearest part frays along the voxels), ice from a millimetre and a half up
from sculptkit import smoothstep
nf = len(k.surfaces[k.name][0])          # the fused body's points come first in the mesh
rad = np.linalg.norm(co[:nf] - C[None], axis=1)
m = smoothstep(RB + 0.002, RB + 0.008, rad)        # soft over a voxel or so, or the voxels' corners show as a comb
# The grin painted on the rock of its gap, so the bars round it frame it crisply: a band of flat white teeth under
# the top bar with dark lines between them, the dark of the mouth in a crescent under them
dn = (co[:nf] - C[None]) / rad[:, None]
az = np.degrees(np.arctan2(dn[:, 0], -dn[:, 1])); el = np.degrees(np.arcsin(np.clip(dn[:, 2], -1, 1)))
mouth = np.argmax(dn @ P.T, axis=1) == SEEDS.index((0, -31))        # the points of the grin's own gap
teeth = mouth & (el > -32 + 0.005 * az * az)
gaps = np.zeros(nf, bool)
for x in (-36, -24, -12, 0, 12, 24, 36):
    gaps |= np.abs(az - x) < 1.4
rock = np.tile(srgb(ROCK), (nf, 1))
rock[teeth] = srgb(WHITE)
rock[teeth & gaps] = srgb(LINE)
col = rock * (1 - m[:, None]) + srgb(ICE)[None] * m[:, None]
loops = np.empty(len(me.loops), np.int32); me.loops.foreach_get("vertex_index", loops)
attr = me.color_attributes["Color"]
rgba = np.empty(len(me.loops) * 4, np.float32); attr.data.foreach_get("color", rgba); rgba = rgba.reshape(-1, 4)
sel = loops < nf
rgba[sel, :3] = col[loops[sel]]
attr.data.foreach_set("color", rgba.ravel())

# --- the eyes: Snorunt's pale blue, grown into angry eyes with a black pupil, in the black of their gaps; the brow bar
# above cuts their top on a slope down toward the middle; on the black the closed states draw a pale line -----------
BL, PUP, PALE = srgb(BLUE), srgb("#141820"), srgb("#BCE6F8")
def brow(u):
    return 0.20 + 0.42 * u            # the eye's top edge: low toward the middle (u -1), high at the outer side
def eye(u, v):
    c = np.tile(DK, (len(u), 1))
    blue = ((u / 0.82) ** 2 + ((v + 0.05) / 0.72) ** 2 < 1) & (v < brow(u))
    c[blue] = BL
    c[blue & (((u + 0.18) / 0.26) ** 2 + ((v + 0.08) / 0.36) ** 2 < 1)] = PUP
    c[blue & (((u - 0.25) / 0.14) ** 2 + ((v - 0.02) / 0.15) ** 2 < 1)] = (1, 1, 1)
    return c
def look(state, flip):
    inner = eye_look(state, eye, DK, PALE, line=0.18)
    def f(u, v):
        return inner(-u if flip else u, v)
    return f
def eyes(state):
    for side in (1, -1):
        de = dirv(26 * side, 4)
        k.decal(surface(de), de, (0.056, 0.05), look(state, side < 0), eps=0.003, rings=20, segs=56)

k.eye_states(eyes)
k.export(out)
