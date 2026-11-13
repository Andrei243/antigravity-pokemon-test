# Onix sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/onix.py -- <the export's Onix.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import bmesh
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, boxy, facets, eye_look, args

src, out = args()
k = Kit(src, coat="shell")

GREY, CRACK, DARKMOUTH = "#ABABB4", "#6E6E78", "#1E1B20"


def shade(h, f):
    """A grey a little lighter or darker (f above or below 1), still written as on screen."""
    c = np.clip(np.array([int(h[i:i + 2], 16) for i in (1, 3, 5)]) * f, 0, 255).astype(int)
    return "#%02X%02X%02X" % tuple(c)


def stone(n, seed, depth, jitter=0.2):
    """
    Cut rock: n planes spread evenly round the sphere (a Fibonacci spiral turned and jittered by the seed), each at a
    depth of its own, and every point pulled in onto the nearest plane. facets' planes fall at random and leave gaps
    that it rounds off; spread planes close all round, so the stone is all flat faces and hard corners.
    """
    rs = np.random.RandomState(seed)
    i = np.arange(n) + 0.5
    ph = np.arccos(1 - 2 * i / n); th = math.pi * (1 + 5 ** 0.5) * i
    N = np.stack([np.cos(th) * np.sin(ph), np.sin(th) * np.sin(ph), np.cos(ph)], 1) + rs.normal(size=(n, 3)) * jitter
    N /= np.linalg.norm(N, axis=1, keepdims=True)
    q, _ = np.linalg.qr(rs.normal(size=(3, 3)))
    N = N @ q
    d = depth[0] + (depth[1] - depth[0]) * rs.rand(n)
    def f(V):
        return V / np.maximum((V @ N.T / d[None]).max(1), 0.3)[:, None]
    return f


def rock(shape):
    """
    A stone's points made into the stone itself: their convex hull with the faces that lie in one plane merged, so
    each face is one flat polygon with straight edges (an icosphere pulled onto the planes zigzags along every edge).
    """
    V, _ = shape
    bm = bmesh.new()
    for v in V.tolist():
        bm.verts.new(v)
    hull = bmesh.ops.convex_hull(bm, input=bm.verts[:])
    bmesh.ops.delete(bm, geom=list({g for g in hull["geom_interior"] + hull["geom_unused"] if isinstance(g, bmesh.types.BMVert)}), context="VERTS")
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(3), verts=bm.verts[:], edges=bm.edges[:])
    bm.verts.index_update()
    out = (np.array([v.co[:] for v in bm.verts]), [[v.index for v in f.verts] for f in bm.faces])
    bm.free()
    out[0][:, 2] += max(0.0, -out[0][:, 2].min())      # a stone of the tail rests on the ground, never under it
    return out


# The chain of boulders, each a stone of its own on its bone's joint, growing from the tail to the great stones of
# the neck. Loose, so each keeps its bone rigidly and its planes flat; they overlap one another a little.
JOINTS = [("body", (0.3, -0.08, 0.05), 0.058), ("seg1", (0.33, -0.01, 0.06), 0.07),
          ("seg2", (0.355, 0.125, 0.085), 0.08), ("seg3", (0.325, 0.235, 0.14), 0.087),
          ("seg4", (0.275, 0.295, 0.25), 0.097), ("seg5", (0.225, 0.29, 0.4), 0.115),
          ("seg6", (0.175, 0.235, 0.56), 0.13), ("seg7", (0.12, 0.155, 0.705), 0.135),
          ("seg8", (0.06, 0.065, 0.81), 0.125)]
ROCKS = []              # the loose pieces drawn flat-faced, in the order they are added
for i, (bone, at, r) in enumerate(JOINTS):
    tone = shade(GREY, 0.9 + 0.012 * i + (0.03 if i % 2 else 0.0))
    r = r * 1.18
    k.add(f"boulder{i}", rock(ell(at, (r * 1.04, r * 0.96, r), rot=((i * 37) % 50 - 25, (i * 53) % 70 - 35, (i * 29) % 40 - 20),
                                  deform=stone(10, 11 + 7 * i, (0.68, 0.8)), sub=4)),
          tone, bone, fuse=False, surface="shell")
    ROCKS.append(f"boulder{i}")

# The tail: pebbles curling away from the smallest boulder, smaller and smaller
for i, (at, r) in enumerate([((0.265, -0.135, 0.04), 0.038), ((0.225, -0.175, 0.03), 0.028),
                             ((0.185, -0.19, 0.022), 0.02), ((0.152, -0.183, 0.016), 0.014)]):
    r = r * 1.15
    k.add(f"pebble{i}", rock(ell(at, (r, r * 0.9, r * 0.85), rot=(20 * i, 30 * i, 10 * i),
                                 deform=stone(8, 40 + 9 * i, (0.68, 0.8)), sub=3)),
          shade(GREY, 0.86), "tail", fuse=False, surface="shell")
    ROCKS.append(f"pebble{i}")

# The head, fused so that its eyes and mouth lie on it: a long block cut flat on top and on the sides, a squared
# snout jutting forward and down, a heavy lower jaw under an open mouth, a brow ridge over each eye
HC = np.array([0.0, -0.17, 0.875])
# Planes of the head's block in the unit sphere's points, each (its normal, how far in): a flat top, flat sides
# leaning in, a brow sloping down to the snout, cheeks cut off at the front corners, a flat chin and a back
HEAD_PLANES = [((0, 0, 1), 0.82), ((1, 0, 0.2), 0.86), ((-1, 0, 0.2), 0.86), ((0, -0.8, 1), 0.86),
               ((0.8, -0.8, 0.2), 0.92), ((-0.8, -0.8, 0.2), 0.92), ((0.7, 0.6, 0.6), 0.86), ((-0.7, 0.6, 0.6), 0.86),
               ((0, 0, -1), 0.84), ((0, 1, 0), 0.9), ((0.6, 0, -1), 0.86), ((-0.6, 0, -1), 0.86), ((0, -1, 0), 0.92)]
HN = np.array([unit(*n) for n, _ in HEAD_PLANES]); HD = np.array([d for _, d in HEAD_PLANES])
def block(V):
    V = boxy(3.0)(V)
    return V / np.maximum((V @ HN.T / HD[None]).max(1), 0.3)[:, None]
k.add("skull", ell(HC, (0.13, 0.17, 0.11), rot=(-6, 0, 0), deform=block), GREY, "head")
def wedge(V):
    """Narrower toward the front (the unit sphere's -Y): the snout comes to a blunt wedge."""
    V = boxy(4.0)(V)
    V[:, 0] *= 1 + 0.2 * V[:, 1]
    return V
k.add("snout", ell((0, -0.315, 0.84), (0.09, 0.09, 0.062), rot=(-12, 0, 0), deform=wedge), GREY, "head")
k.add("jaw", ell((0, -0.285, 0.768), (0.08, 0.1, 0.04), rot=(6, 0, 0), deform=boxy(4.0)), GREY, "head")
k.add("brow", ell((0.058, -0.27, 0.932), (0.054, 0.05, 0.024), rot=(-18, -14, 10), deform=boxy(4.0)), GREY, "head",
      mirror=True)
# The ridge running down the middle of the face, from the horn's foot to the nose
k.add("ridge", tube([(0, -0.14, 0.97), (0, -0.25, 0.945), (0, -0.36, 0.87)], [0.03, 0.026, 0.022]), GREY, "head")
# The horn on top: a tall thin blade rising and leaning a little back, flat side on
k.add("horn", blade([(0, -0.14, 0.94), (0, -0.115, 1.04), (0, -0.07, 1.16)], [0.034, 0.022, 0.004], (1, 0, 0), 0.6),
      GREY, "head", fuse=False, surface="shell")

# The mouth, cut into the head between the snout and the jaw: a wide slot across the front, its corners turned up
# and running back along the sides, dark inside
def grin(V):
    V = V.copy()
    V[:, 2] += 1.2 * V[:, 0] ** 2           # in the unit sphere's points: the corners rise
    return V
k.add("mouth", ell((0, -0.36, 0.796), (0.088, 0.075, 0.02), rot=(4, 0, 0), deform=grin), DARKMOUTH, "head", carve=True)

# The crease of the brow and the shadow under it, darker
k.paint(lambda P: inside(np.abs(P[:, 2] - 0.9) - 0.01, 0.006) * smoothstep(-0.24, -0.28, P[:, 1]) *
        inside(np.abs(P[:, 0]) - 0.11) * smoothstep(0.02, 0.04, np.abs(P[:, 0])), CRACK, on=["skull", "brow", "snout"])

# Small fierce eyes under the brow: white, a black pupil to the inside, the lid slanting down toward the middle
SCLERA, PUPIL, LID, INK = srgb("#F4F2EE"), srgb("#141216"), srgb(GREY), srgb("#2A2A30")
def eye(u, v):
    c = np.tile(SCLERA, (len(u), 1))
    c[((u + 0.3) / 0.3) ** 2 + ((v + 0.1) / 0.6) ** 2 < 1] = PUPIL
    c[u * u + v * v > 0.8] = INK
    edge = 0.3 + 0.4 * u
    c[v > edge + 0.12] = LID
    c[np.abs(v - edge) <= 0.12] = INK
    return c
de = unit(0.5, -0.85, 0.05)
def eyes(state):
    k.decal((0.06, -0.272, 0.888), de, (0.037, 0.023), eye_look(state, eye, LID, INK), eps=0.006, rings=20, segs=56,
            mirror=True)

# The rocks' faces are drawn flat, so each plane reads as cut stone with a hard edge: the loose pieces come after
# the fused head in the body's faces, in the order they were added, and this build lays no patch
loose = [(p["name"], len(p["F"])) for p in k.parts if not p["fuse"] and not p["carve"]]
body = k.build(voxel=0.006, smooth=1)
me = body.data
smooth = np.ones(len(me.polygons), bool)
end = len(me.polygons)
for name, count in reversed(loose):
    if name in ROCKS:
        smooth[end - count:end] = False
    end -= count
me.polygons.foreach_set("use_smooth", smooth.tolist())
k.eye_states(eyes)
k.export(out)
