# Steelix sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/steelix.py -- <the export's Steelix.glb> <out.glb>
# Grown from Onix's script: the same chain of loose flat-faced pieces a joint and a fused head with eyes under a
# brow, now cut steel: each piece an eight-sided drum along the chain, blades of hexagonal steel jutting out at the
# sides of every other one, a pointed tail, and a massive head whose lower jaw juts forward like a shovel under a row
# of square teeth.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import bmesh
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="metal")

STEEL, SEAM, GUM, TOOTH = "#A2B1CD", "#6C7894", "#262A36", "#EEEEE8"


def shade(h, f):
    """A colour a little lighter or darker (f above or below 1), still written as on screen."""
    c = np.clip(np.array([int(h[i:i + 2], 16) for i in (1, 3, 5)]) * f, 0, 255).astype(int)
    return "#%02X%02X%02X" % tuple(c)


def hull(V, lift=True):
    """Points made into a solid: their convex hull with the faces lying in one plane merged, so each is flat."""
    bm = bmesh.new()
    for v in np.asarray(V).tolist():
        bm.verts.new(v)
    h = bmesh.ops.convex_hull(bm, input=bm.verts[:])
    bmesh.ops.delete(bm, geom=list({g for g in h["geom_interior"] + h["geom_unused"] if isinstance(g, bmesh.types.BMVert)}), context="VERTS")
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(3), verts=bm.verts[:], edges=bm.edges[:])
    bm.verts.index_update()
    out = (np.array([v.co[:] for v in bm.verts]), [[v.index for v in f.verts] for f in bm.faces])
    bm.free()
    if lift:
        out[0][:, 2] += max(0.0, -out[0][:, 2].min())       # nothing of the tail goes under the ground
    return out


def frame(t):
    """Two directions square to t: the first as near the world's x as can be (the serpent's side), the second up."""
    t = t / np.linalg.norm(t)
    side = np.array([1.0, 0, 0]) - t * t[0]
    if np.linalg.norm(side) < 1e-3:
        side = np.cross(t, (0, 0, 1.0))
    side /= np.linalg.norm(side)
    return side, np.cross(t, side)


def drum(at, t, r, length, turn):
    """An eight-sided drum of steel along t, its ends bevelled: the corners of a cut-steel segment."""
    t = np.asarray(t, float); t /= np.linalg.norm(t)
    a, b = frame(t)
    pts = []
    for z, rr in ((-length / 2 - 0.18 * r, 0.72), (-length / 2, 1.0), (length / 2, 1.0), (length / 2 + 0.18 * r, 0.72)):
        for j in range(8):
            th = turn + j * math.pi / 4
            pts.append(at + t * z + (a * math.cos(th) + b * math.sin(th)) * r * rr)
    return hull(pts)


def spike(base, tip, r, flat, axis, sides=6):
    """A hexagonal steel spike from base to tip, squashed along axis by flat (1 round): a blade or a tail's point."""
    base, tip = np.asarray(base, float), np.asarray(tip, float)
    t = tip - base; L = np.linalg.norm(t); t /= L
    ax = np.asarray(axis, float); ax = ax - t * np.dot(ax, t); ax /= np.linalg.norm(ax)
    bx = np.cross(t, ax)
    pts = [tip]
    for s, rr in ((-0.25 * r / L, 0.75), (0.0, 1.0), (0.35, 0.82)):
        for j in range(sides):
            th = (j + 0.5) * 2 * math.pi / sides
            pts.append(base + t * L * s + (ax * math.cos(th) * flat + bx * math.sin(th)) * r * rr)
    return hull(pts, lift=False)


FLAT = []           # loose pieces drawn flat-faced


def loose(name, shape, color, bone):
    k.add(name, shape, color, bone, fuse=False)
    FLAT.append(name)


# The chain: a drum a joint, growing from the tail to the neck, each along the way the chain runs there
JOINTS = [("body", (0.3, -0.08, 0.05), 0.064), ("seg1", (0.33, -0.01, 0.06), 0.078),
          ("seg2", (0.355, 0.125, 0.085), 0.09), ("seg3", (0.325, 0.235, 0.14), 0.1),
          ("seg4", (0.275, 0.295, 0.25), 0.11), ("seg5", (0.225, 0.29, 0.4), 0.125),
          ("seg6", (0.175, 0.235, 0.56), 0.135), ("seg7", (0.12, 0.155, 0.705), 0.13),
          ("seg8", (0.06, 0.065, 0.81), 0.115)]
P = [np.array(at) for _, at, _ in JOINTS] + [np.array((0.0, -0.07, 0.9))]
for i, (bone, at, r) in enumerate(JOINTS):
    at = np.array(at)
    t = P[i + 1] - (P[i - 1] if i else at + (at - P[i + 1]))
    length = np.linalg.norm(P[i + 1] - at) * 0.78
    loose(f"drum{i}", drum(at, t, r * 1.1, length, 0.2 * i), shade(STEEL, 0.93 + (0.05 if i % 2 else 0.0)), bone)

# Blades: a pair at the sides of every other drum, longest at the neck, jutting out and a little up
for i, length in ((3, 0.17), (5, 0.24), (7, 0.27)):
    bone, at, r = JOINTS[i]
    at = np.array(at)
    t = P[i + 1] - P[i - 1]; t /= np.linalg.norm(t)
    side, up = frame(t)
    for s, nm in ((1, "R"), (-1, "L")):
        d = unit(*(side * s + up * 0.45 + t * 0.15))
        base = at + d * r * 0.6
        loose(f"blade{i}{nm}", spike(base, base + d * (length + r * 0.6), r * 0.5, 0.5, t), shade(STEEL, 1.04), bone)

# The tail: small drums curling away from the smallest, ending in a long point, a spur on its back
TAIL = [((0.265, -0.13, 0.045), 0.045), ((0.225, -0.17, 0.035), 0.036), ((0.185, -0.188, 0.028), 0.028)]
prev = np.array(JOINTS[0][1])
for i, (at, r) in enumerate(TAIL):
    at = np.array(at)
    nxt = np.array(TAIL[i + 1][0]) if i + 1 < len(TAIL) else at + (at - prev)
    loose(f"tail{i}", drum(at, nxt - prev, r, np.linalg.norm(nxt - at) * 0.9, 0.4 * i), shade(STEEL, 0.9), "tail")
    prev = at
loose("tailtip", spike((0.17, -0.19, 0.028), (0.05, -0.17, 0.03), 0.026, 0.8, (0, 0, 1)), shade(STEEL, 0.95), "tail")
loose("tailspur", spike((0.24, -0.15, 0.06), (0.25, -0.16, 0.16), 0.022, 0.6, (1, 0, 0)), shade(STEEL, 1.02), "tail")

# The head, fused so that its eyes lie on it: a long block cut flat on top and on the sides, narrowing to a blunt
# front, a heavy brow over each eye
HC = np.array([0.0, -0.21, 0.955])
HEAD_PLANES = [((0, 0, 1), 0.84), ((1, 0, 0.15), 0.88), ((-1, 0, 0.15), 0.88), ((0, -0.7, 1), 0.86),
               ((0.8, -0.8, 0.1), 0.94), ((-0.8, -0.8, 0.1), 0.94), ((0.7, 0.6, 0.6), 0.86), ((-0.7, 0.6, 0.6), 0.86),
               ((0, 0, -1), 0.86), ((0, 1, 0), 0.9), ((0, -1, 0), 0.94)]
HN = np.array([unit(*n) for n, _ in HEAD_PLANES]); HD = np.array([d for _, d in HEAD_PLANES])
def block(V):
    V = boxy(3.0)(V)
    V[:, 0] *= 1 + 0.16 * V[:, 1]
    return V / np.maximum((V @ HN.T / HD[None]).max(1), 0.3)[:, None]
k.add("skull", ell(HC, (0.165, 0.26, 0.11), deform=block), STEEL, "head")
k.add("brow", ell((0.075, -0.25, 1.0), (0.07, 0.07, 0.028), rot=(-10, -16, 8), deform=boxy(4.0)), STEEL, "head",
      mirror=True)
# The back of the head rising into a blunt crest
k.add("crest", ell((0, -0.09, 1.005), (0.11, 0.12, 0.045), rot=(10, 0, 0), deform=boxy(3.0)), STEEL, "head")

# The lower jaw: a great shovel of steel on the jaw bone, flat on top, jutting past the snout and coming to a blunt
# point, its underside swelling down
JAW = [(0.16, -0.06, 0.85), (-0.16, -0.06, 0.85), (0.12, -0.42, 0.85), (-0.12, -0.42, 0.85), (0.05, -0.53, 0.845),
       (-0.05, -0.53, 0.845), (0.0, -0.56, 0.84),
       (0.14, -0.03, 0.77), (-0.14, -0.03, 0.77), (0.165, -0.2, 0.74), (-0.165, -0.2, 0.74), (0.11, -0.4, 0.76),
       (-0.11, -0.4, 0.76), (0.03, -0.53, 0.815), (-0.03, -0.53, 0.815), (0.0, -0.3, 0.715)]
loose("jaw", hull(JAW, lift=False), shade(STEEL, 1.02), "jaw")
# The mouth's line: a dark filling between the jaw and the head, showing where their edges part
k.add("gum", ell((0, -0.22, 0.857), (0.13, 0.2, 0.014)), GUM, "head", fuse=False)

# The teeth: square, white, in a row round the front of the upper jaw, between it and the shovel
TEETH = [(0.02, -0.43, 0), (0.06, -0.422, 6), (0.097, -0.403, 20), (0.124, -0.37, 40), (0.137, -0.325, 62), (0.142, -0.275, 80)]
for i, (x, y, turn) in enumerate(TEETH):
    for s, nm in ((1, "R"), (-1, "L")):
        k.add(f"tooth{i}{nm}", ell((x * s, y, 0.852), (0.019, 0.013, 0.02), rot=(0, 0, turn * s), deform=boxy(6)),
              TOOTH, "head", fuse=False, surface="shell")

# The crease under the brow, darker
k.paint(lambda P: inside(np.abs(P[:, 2] - 0.968) - 0.008, 0.005) * smoothstep(-0.18, -0.22, P[:, 1]) *
        smoothstep(0.03, 0.06, np.abs(P[:, 0])), SEAM, on=["skull", "brow"])

# Small fierce eyes under the brow: a white wedge with its point toward the back, a red iris to the front with a
# black pupil, ringed in black, the lid slanting down toward the snout
SCLERA, IRIS, PUPIL, LID, INK = srgb("#F2F2EE"), srgb("#D4262E"), srgb("#141216"), srgb(STEEL), srgb("#1C1E26")
def eye(u, v):
    c = np.tile(SCLERA, (len(u), 1))
    c[((u + 0.35) / 0.42) ** 2 + ((v + 0.1) / 0.75) ** 2 < 1] = IRIS
    c[((u + 0.4) / 0.18) ** 2 + ((v + 0.1) / 0.4) ** 2 < 1] = PUPIL
    c[u * u + v * v > 0.78] = INK
    edge = 0.25 + 0.45 * u
    c[v > edge + 0.12] = LID
    c[np.abs(v - edge) <= 0.12] = INK
    return c
de = unit(0.62, -0.76, 0.05)
def eyes(state):
    k.decal((0.112, -0.262, 0.952), de, (0.04, 0.026), eye_look(state, eye, LID, INK), eps=0.006, rings=20, segs=56,
            mirror=True)

# The steel's faces are drawn flat, so each plane reads as cut metal with a hard edge: the loose pieces come after the
# fused head in the body's faces, in the order they were added, and this build lays no patch
loose_parts = [(p["name"], len(p["F"])) for p in k.parts if not p["fuse"] and not p["carve"]]
body = k.build(voxel=0.006, smooth=1)
me = body.data
smooth = np.ones(len(me.polygons), bool)
end = len(me.polygons)
for name, count in reversed(loose_parts):
    if name in FLAT:
        smooth[end - count:end] = False
    end -= count
me.polygons.foreach_set("use_smooth", smooth.tolist())
k.eye_states(eyes)
k.export(out)
