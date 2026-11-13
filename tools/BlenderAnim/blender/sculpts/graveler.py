# Graveler sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/graveler.py -- <the export's Graveler.glb> <out.glb>
# Geodude grown: the same grey stone, the same small scowling eyes, now a boulder on two stubby legs with four arms,
# its face set inside a ring of squared rock teeth like a cog, its mouth gaping pink.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import bmesh
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, boxy, facets, eye_look, args

src, out = args()
k = Kit(src, coat="shell")

GREY, DARK, CRACK, UNDER = "#ACAAA0", "#8E8C84", "#6A6862", "#8C8E94"
C = np.array([0.0, 0.0, 0.4])            # the boulder's middle, on the body bone
R = np.array([0.27, 0.235, 0.25])        # its radii


def shade(h, f):
    """A grey a little lighter or darker (f above or below 1), still written as on screen."""
    c = np.clip(np.array([int(h[i:i + 2], 16) for i in (1, 3, 5)]) * f, 0, 255).astype(int)
    return "#%02X%02X%02X" % tuple(c)


def stone(n, seed, depth, jitter=0.2):
    """Cut rock: n planes spread round the sphere, each at a depth of its own (onix.py)."""
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
    """A stone's points made into the stone itself: their convex hull, its coplanar faces merged (onix.py)."""
    V, _ = shape
    bm = bmesh.new()
    for v in V.tolist():
        bm.verts.new(v)
    hull = bmesh.ops.convex_hull(bm, input=bm.verts[:])
    bmesh.ops.delete(bm, geom=list({g for g in hull["geom_interior"] + hull["geom_unused"] if isinstance(g, bmesh.types.BMVert)}), context="VERTS")
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(3), verts=bm.verts[:], edges=bm.edges[:])
    bm.verts.index_update()
    out_ = (np.array([v.co[:] for v in bm.verts]), [[v.index for v in f.verts] for f in bm.faces])
    bm.free()
    return out_


ROCKS = []              # the names of the loose rocks, drawn flat-faced


def boulder(name, at, r, seed, bone, tone=1.0, mirror=False, planes=9, rot=(0, 0, 0)):
    k.add(name, rock(ell(at, r, rot=rot, deform=stone(planes, seed, (0.68, 0.82)), sub=3)), shade(GREY, tone), bone,
          fuse=False, mirror=mirror, surface="shell")
    ROCKS.append(name)


def on_body(d, lift=0.0):
    """The point of the boulder's ellipsoid along direction d from its middle, lifted along d."""
    d = unit(*d)
    t = 1 / np.sqrt(((d / R) ** 2).sum())
    return C + d * (t + lift)


# The boulder: round, a little wider than tall, faceted on its back and sides, smoother where the face is
k.add("body", ell(C, R, deform=facets(16, 1.0, 75, keep_front=0.5, depth=(0.82, 0.94))), GREY, "body")

# The ring of squared rock teeth round the face, like a cog: fused, so the brow at its lower edge takes no loose
# piece over the eyes. Each tooth is a short block standing proud of the stone along its own direction.
F = unit(0, -1, 0.32)                     # the middle of the face, seen from the boulder's middle
A = unit(1, 0, 0); Bv = np.cross(F, A)    # the face's own right and up
for i, ang in enumerate(np.linspace(-35, 215, 9)):
    a = math.radians(ang)
    d = unit(*(F * math.cos(math.radians(43)) + math.sin(math.radians(43)) * (A * math.cos(a) + Bv * math.sin(a))))
    p = on_body(d, 0.004)
    # a block turned to the surface: its long side along the ring, its depth along d
    along = unit(*(-A * math.sin(a) + Bv * math.cos(a)))
    side = np.cross(d, along)
    M = np.stack([along, side, d], axis=1)
    V, Fc = ell((0, 0, 0), (0.046, 0.036, 0.048), deform=boxy(6.0), sub=4)
    k.add(f"tooth{i}", (V @ M.T + p, Fc), GREY, "body")

# A heavy brow over each eye, dipping to a notch in the middle: the scowl (the ring's lower edge)
k.add("brow", ell((0.075, -0.222, 0.482), (0.085, 0.04, 0.03), rot=(-14, -16, 9), deform=facets(10, 0.5, 7)), GREY,
      "body", mirror=True)
# The cheeks under the eyes and round the mouth
k.add("cheek", ell((0.105, -0.2, 0.37), (0.07, 0.045, 0.06), deform=facets(8, 0.5, 11)), GREY, "body", mirror=True)

# Rocks all over its back, top and sides: loose cut stones standing off the boulder
rs = np.random.RandomState(7)
dirs = [(0.3, 0.6, 0.75), (-0.35, 0.55, 0.75), (0.0, 0.9, 0.4), (0.7, 0.55, 0.3), (-0.7, 0.5, 0.35),
        (0.55, 0.8, -0.1), (-0.5, 0.85, -0.15), (0.0, 0.95, -0.3), (0.9, 0.2, -0.2), (-0.9, 0.25, -0.15),
        (0.15, 0.4, 0.9), (-0.2, 0.25, 0.95), (0.85, 0.05, 0.4), (-0.85, 0.0, 0.45), (0.3, 0.9, 0.2), (-0.3, 0.9, 0.05)]
for i, d in enumerate(dirs):
    r = 0.05 + 0.025 * rs.rand()
    boulder(f"lump{i}", on_body(d, -0.012), (r, r * 0.85, r * 0.8), 20 + i, "body", tone=0.92 + 0.12 * rs.rand(),
            rot=tuple((rs.rand(3) * 90 - 45).tolist()))

# The upper arms: craggy stone reaching out and down to great open hands of three thick fingers
SH = np.array([0.2, -0.04, 0.48]); EL = np.array([0.38, -0.08, 0.45]); WR = np.array([0.5, -0.13, 0.34])
k.add("arm", tube([SH, EL, WR], [0.055, 0.052, 0.046]), GREY, "arm{s}", mirror=True)
for j, (t, r) in enumerate([(0.3, 0.06), (0.62, 0.065), (0.85, 0.058)]):
    p = SH + (EL - SH) * min(t * 2, 1) if t < 0.5 else EL + (WR - EL) * (t - 0.5) * 2
    boulder(f"armrock{j}", p + np.array([0.0, 0.004, 0.012]), (r, r * 0.9, r * 0.85), 50 + j, "arm{s}", mirror=True,
            tone=0.95 + 0.04 * j, rot=(15 * j, 30 * j, 10))
HC = np.array([0.53, -0.16, 0.29])
k.add("palm", ell(HC, (0.07, 0.06, 0.055), rot=(0, 20, 0), deform=boxy(3.0)), GREY, "arm{s}", mirror=True)
# three thick blocky fingers spread out and down, each a fused tube with a cut stone for its tip
for j, (dx, dy, dz) in enumerate([(0.09, -0.06, -0.08), (0.03, -0.1, -0.11), (-0.06, -0.09, -0.06)]):
    tip = HC + np.array([dx, dy, dz])
    k.add(f"finger{j}", tube([HC + np.array([dx, dy, dz]) * 0.3, tip], [0.03, 0.026]), GREY, "arm{s}", mirror=True)
    boulder(f"tip{j}", tip, (0.03, 0.03, 0.03), 60 + j, "arm{s}", mirror=True, planes=8)

# The lower arms: short, curling round to the front of the belly, two small fists meeting under the mouth
LS = np.array([0.2, -0.1, 0.3]); LE = np.array([0.18, -0.22, 0.25]); LF = np.array([0.08, -0.26, 0.26])
k.add("lowarm", tube([LS, LE, LF + np.array([0.02, 0.02, 0])], [0.045, 0.042, 0.038]), GREY, "lowArm{s}", mirror=True)
boulder("lowrock", LE + np.array([0.01, -0.01, 0.01]), (0.045, 0.04, 0.04), 70, "lowArm{s}", mirror=True)
boulder("lowfist", LF, (0.05, 0.042, 0.045), 72, "lowArm{s}", mirror=True, planes=10, rot=(0, 0, 20))

# Short stubby legs under the boulder, on flat blocky feet
k.add("leg", tube([(0.12, 0.0, 0.2), (0.13, -0.01, 0.1), (0.13, -0.02, 0.08)], [0.085, 0.08, 0.072]), GREY,
      "leg{s}", mirror=True)
k.add("foot", ell((0.135, -0.04, 0.05), (0.09, 0.1, 0.045), deform=boxy(3.0)), GREY, "leg{s}", mirror=True)
for j, x in enumerate((0.1, 0.17)):
    boulder(f"toe{j}", (x, -0.12, 0.045), (0.028, 0.03, 0.026), 80 + j, "leg{s}", mirror=True, planes=12)

# The underside darker, bluish, where the boulder curves in under itself
k.paint(lambda P: smoothstep(0.27, 0.19, P[:, 2]) * inside(np.linalg.norm(P[:, :2] - [0, 0], axis=1) - 0.3, 0.01),
        UNDER, on=["body", "cheek"])

# Cracks in the stone, as on Geodude: thin dark lines over the top, sides and back
def cracks(P):
    m = np.zeros(len(P))
    Q = P - C; Q /= np.linalg.norm(Q, axis=1, keepdims=True)
    lines = [((0.15, 0.05, 0.6), (0.2, 0.12, 0.55), (0.24, 0.18, 0.47)),
             ((-0.16, 0.06, 0.6), (-0.22, 0.12, 0.52)),
             ((0.05, 0.2, 0.6), (0.1, 0.25, 0.48), (0.12, 0.24, 0.36)),
             ((-0.08, 0.22, 0.58), (-0.04, 0.26, 0.42)),
             ((0.25, -0.05, 0.3), (0.26, 0.05, 0.24)),
             ((-0.25, -0.03, 0.33), (-0.26, 0.08, 0.27))]
    for pts in lines:
        pts = np.array(pts) - C
        pts /= np.linalg.norm(pts, axis=1, keepdims=True)
        for a, b in zip(pts[:-1], pts[1:]):
            ab = b - a
            t = np.clip(((Q - a) @ ab) / (ab @ ab), 0, 1)
            d = np.linalg.norm(Q - (a + t[:, None] * ab), axis=1) * 0.25
            m = np.maximum(m, inside(d - 0.004, 0.003))
    return m
k.paint(cracks, CRACK, on=["body"])
# The shadow under the brow, darker
k.paint(lambda P: inside(np.abs(P[:, 2] - 0.455) - 0.012, 0.01) * smoothstep(-0.17, -0.21, P[:, 1]) *
        inside(np.abs(P[:, 0]) - 0.16), DARK, on=["body", "brow"])

# The mouth: a wide hollow gaping open under the eyes, pink within, its corners drawn down a little
def gape(V):
    V = V.copy()
    V[:, 2] -= 0.25 * V[:, 0] ** 2
    return V
k.add("mouth", ell((0, -0.245, 0.36), (0.085, 0.06, 0.03), rot=(-12, 0, 0), deform=gape), "#E07A86", "body",
      carve=True)

# Small angry eyes under the brow: Geodude's, white, a dark iris to the inside, the lid slanting down to the middle
SCLERA, IRIS, PUPIL, LID, INK = srgb("#F4F2EC"), srgb("#4A3426"), srgb("#120E0C"), srgb(GREY), srgb("#2A2824")
def eye(u, v):
    c = np.tile(SCLERA, (len(u), 1))
    c[((u + 0.2) / 0.42) ** 2 + ((v + 0.05) / 0.85) ** 2 < 1] = IRIS
    c[((u + 0.22) / 0.24) ** 2 + ((v + 0.05) / 0.5) ** 2 < 1] = PUPIL
    c[((u + 0.05) / 0.13) ** 2 + ((v - 0.22) / 0.16) ** 2 < 1] = (1, 1, 1)
    c[u * u + v * v > 0.84] = INK
    edge = 0.25 + 0.45 * u
    c[v > edge + 0.12] = LID
    c[np.abs(v - edge) <= 0.12] = INK
    return c
de = unit(0.4, -0.91, 0.08)
E = np.array([0.072, -0.215, 0.438])
def eyes(state):
    k.decal(E, de, (0.036, 0.026), eye_look(state, eye, LID, INK), eps=0.006, rings=20, segs=56, mirror=True)

# The rocks' faces are drawn flat: the loose pieces come after the fused body in its faces, in the order they were
# added, and this build lays no patch
loose = [(p["name"], len(p["F"])) for p in k.parts if not p["fuse"] and not p["carve"]]
body = k.build(voxel=0.008, smooth=1)
me = body.data
smooth = np.ones(len(me.polygons), bool)
end = len(me.polygons)
for name, count in reversed(loose):
    if any(name.startswith(r) for r in ROCKS):
        smooth[end - count:end] = False
    end -= count
me.polygons.foreach_set("use_smooth", smooth.tolist())
k.eye_states(eyes)
k.export(out)
