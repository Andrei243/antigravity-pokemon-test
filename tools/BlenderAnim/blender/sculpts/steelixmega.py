# Mega Steelix sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/steelixmega.py -- <the export's Steelix-Mega.glb> <out.glb>
# Copied from steelix.py (which builds as it is read) and changed as the two pictures change it: the same chain of
# cut-steel drums arching up to a fused head, now with the head the Mega's largest mass, a fifth larger, its lower jaw
# a great scoop whose front hangs in a comb of four square pillars and whose sides sweep back and up behind the skull
# in a jagged crescent; jagged ridges along the top of the head; blue eyes; a black hexagon ringed in blue on each side
# of every drum, the drums behind the head larger; long crystals of pale pink, blue and lilac jutting from every other
# drum and from the tail in place of the steel blades; a few pale shards of steel hanging over the head.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import bmesh
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, smoothstep, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="metal")

STEEL, SEAM, GUM, TOOTH = "#A6B4D0", "#6C7894", "#262A36", "#EEEEE8"
HEXDARK, HEXBLUE, SHARD = "#1E222E", "#2E9BE2", "#DDE5F2"
CRYSTALS = ["#F6CCE4", "#C6E6FA", "#DCD2FA"]


def shade(h, f):
    c = np.clip(np.array([int(h[i:i + 2], 16) for i in (1, 3, 5)]) * f, 0, 255).astype(int)
    return "#%02X%02X%02X" % tuple(c)


def hull(V, lift=True):
    """As steelix.py's: points made into a solid, their convex hull with the faces in one plane merged."""
    bm = bmesh.new()
    for v in np.asarray(V).tolist():
        bm.verts.new(v)
    h = bmesh.ops.convex_hull(bm, input=bm.verts[:])
    bmesh.ops.delete(bm, geom=list({g for g in h["geom_interior"] + h["geom_unused"] if isinstance(g, bmesh.types.BMVert)}), context="VERTS")
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(3), verts=bm.verts[:], edges=bm.edges[:])
    bm.verts.index_update()
    res = (np.array([v.co[:] for v in bm.verts]), [[v.index for v in f.verts] for f in bm.faces])
    bm.free()
    if lift:
        res[0][:, 2] += max(0.0, -res[0][:, 2].min())
    return res


def frame(t):
    t = t / np.linalg.norm(t)
    side = np.array([1.0, 0, 0]) - t * t[0]
    if np.linalg.norm(side) < 1e-3:
        side = np.cross(t, (0, 0, 1.0))
    side /= np.linalg.norm(side)
    return side, np.cross(t, side)


def drum(at, t, r, length, turn):
    t = np.asarray(t, float); t /= np.linalg.norm(t)
    a, b = frame(t)
    pts = []
    for z, rr in ((-length / 2 - 0.18 * r, 0.72), (-length / 2, 1.0), (length / 2, 1.0), (length / 2 + 0.18 * r, 0.72)):
        for j in range(8):
            th = turn + j * math.pi / 4
            pts.append(at + t * z + (a * math.cos(th) + b * math.sin(th)) * r * rr)
    return hull(pts)


def spike(base, tip, r, flat, axis, sides=6):
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


FLAT = []


def loose(name, shape, color, bone, surface=None):
    k.add(name, shape, color, bone, fuse=False, surface=surface)
    FLAT.append(name)


def hexagon(rad):
    return [(rad * math.cos(j * math.pi / 3 + math.pi / 6), rad * math.sin(j * math.pi / 3 + math.pi / 6)) for j in range(6)]


# --- the chain: as steelix.py's, a drum a joint; the drums behind the head larger -----------------------------------
JOINTS = [("body", (0.3, -0.08, 0.05), 0.064), ("seg1", (0.33, -0.01, 0.06), 0.078),
          ("seg2", (0.355, 0.125, 0.085), 0.09), ("seg3", (0.325, 0.235, 0.14), 0.1),
          ("seg4", (0.275, 0.295, 0.25), 0.11), ("seg5", (0.225, 0.29, 0.4), 0.125),
          ("seg6", (0.175, 0.235, 0.56), 0.14), ("seg7", (0.12, 0.155, 0.705), 0.16),
          ("seg8", (0.06, 0.065, 0.81), 0.17)]
P = [np.array(at) for _, at, _ in JOINTS] + [np.array((0.0, -0.07, 0.9))]
for i, (bone, at, r) in enumerate(JOINTS):
    at = np.array(at)
    t = P[i + 1] - (P[i - 1] if i else at + (at - P[i + 1]))
    length = np.linalg.norm(P[i + 1] - at) * 0.78
    R = r * 1.1
    loose(f"drum{i}", drum(at, t, R, length, 0.2 * i), shade(STEEL, 0.93 + (0.05 if i % 2 else 0.0)), bone)
    # A black hexagon ringed in blue on each side of the drum, standing just proud of it
    tt = t / np.linalg.norm(t)
    side, up = frame(tt)
    for s, nm in ((1, "R"), (-1, "L")):
        c = at + side * s * R * 0.97
        n = side * s
        hr = R * 0.5
        loose(f"hexa{i}{nm}", slab(hexagon(hr), c, tt, up, 0.03), HEXDARK, bone)
        loose(f"hexb{i}{nm}", slab(hexagon(hr * 0.72), c + n * 0.006, tt, up, 0.03), HEXBLUE, bone)
        loose(f"hexc{i}{nm}", slab(hexagon(hr * 0.5), c + n * 0.011, tt, up, 0.03), HEXDARK, bone)

# Crystals: long, pale, hexagonal, a pair from every other drum, jutting out, up and back
for n, (i, length) in enumerate(((2, 0.2), (4, 0.28), (6, 0.32), (8, 0.26))):
    bone, at, r = JOINTS[i]
    at = np.array(at)
    t = P[i + 1] - P[i - 1]; t /= np.linalg.norm(t)
    side, up = frame(t)
    if up[2] < 0:
        up = -up
    for s, nm in ((1, "R"), (-1, "L")):
        d = unit(*(side * s + up * 0.55 - t * 0.35))
        base = at + d * r * 0.7
        loose(f"crystal{i}{nm}", spike(base, base + d * (length + r * 0.6), r * 0.55, 0.5, t),
              CRYSTALS[(n + (s > 0)) % 3], bone, surface="shell")

# The tail: small drums curling away, a long point, and two crystals off its end
TAIL = [((0.265, -0.13, 0.045), 0.045), ((0.225, -0.17, 0.035), 0.036), ((0.185, -0.188, 0.028), 0.028)]
prev = np.array(JOINTS[0][1])
for i, (at, r) in enumerate(TAIL):
    at = np.array(at)
    nxt = np.array(TAIL[i + 1][0]) if i + 1 < len(TAIL) else at + (at - prev)
    loose(f"tail{i}", drum(at, nxt - prev, r, np.linalg.norm(nxt - at) * 0.9, 0.4 * i), shade(STEEL, 0.9), "tail")
    prev = at
loose("tailtip", spike((0.17, -0.19, 0.028), (0.05, -0.17, 0.03), 0.026, 0.8, (0, 0, 1)), shade(STEEL, 0.95), "tail")
loose("tailcrys0", spike((0.24, -0.15, 0.06), (0.22, -0.22, 0.24), 0.03, 0.55, (1, 0, 0)), CRYSTALS[0], "tail", "shell")
loose("tailcrys1", spike((0.28, -0.12, 0.06), (0.4, -0.2, 0.19), 0.026, 0.55, (0, 1, 0)), CRYSTALS[1], "tail", "shell")

# --- the head: steelix.py's, grown about its joint a fifth larger ---------------------------------------------------
J, HS = np.array([0.0, -0.04, 0.86]), 1.22


def hp(p):
    return J + (np.array(p, float) - J) * HS


def hs(shape):
    V, F = shape
    return J + (np.asarray(V) - J) * HS, F


HC = np.array([0.0, -0.21, 0.955])
HEAD_PLANES = [((0, 0, 1), 0.84), ((1, 0, 0.15), 0.88), ((-1, 0, 0.15), 0.88), ((0, -0.7, 1), 0.86),
               ((0.8, -0.8, 0.1), 0.94), ((-0.8, -0.8, 0.1), 0.94), ((0.7, 0.6, 0.6), 0.86), ((-0.7, 0.6, 0.6), 0.86),
               ((0, 0, -1), 0.86), ((0, 1, 0), 0.9), ((0, -1, 0), 0.94)]
HN = np.array([unit(*n) for n, _ in HEAD_PLANES]); HD = np.array([d for _, d in HEAD_PLANES])
def block(V):
    V = boxy(3.0)(V)
    V[:, 0] *= 1 + 0.16 * V[:, 1]
    return V / np.maximum((V @ HN.T / HD[None]).max(1), 0.3)[:, None]
k.add("skull", hs(ell(HC, (0.165, 0.26, 0.11), deform=block)), STEEL, "head")
k.add("brow", hs(ell((0.075, -0.25, 1.0), (0.07, 0.07, 0.028), rot=(-10, -16, 8), deform=boxy(4.0))), STEEL, "head",
      mirror=True)
k.add("crest", hs(ell((0, -0.09, 1.005), (0.11, 0.12, 0.045), rot=(10, 0, 0), deform=boxy(3.0))), STEEL, "head")

# Jagged ridges along the top of the head: a row of laid-back teeth either side of the middle
for s, nm in ((1, "R"), (-1, "L")):
    for j, y in enumerate((-0.33, -0.24, -0.15, -0.06)):
        x = 0.055 * s
        z = 1.035 if y > -0.12 else 1.03
        pts = [hp((x - 0.016, y - 0.035, z - 0.03)), hp((x + 0.016, y - 0.035, z - 0.03)),
               hp((x - 0.016, y + 0.04, z - 0.03)), hp((x + 0.016, y + 0.04, z - 0.03)),
               hp((x, y + 0.045, z + 0.035 + 0.006 * j))]
        loose(f"ridge{j}{nm}", hull(pts, lift=False), shade(STEEL, 1.05), "head")

# The lower jaw: the shovel of steelix.py's, larger, on the jaw bone
JAW = [(0.16, -0.06, 0.85), (-0.16, -0.06, 0.85), (0.12, -0.42, 0.85), (-0.12, -0.42, 0.85), (0.05, -0.53, 0.845),
       (-0.05, -0.53, 0.845), (0.0, -0.56, 0.84),
       (0.14, -0.03, 0.77), (-0.14, -0.03, 0.77), (0.165, -0.2, 0.74), (-0.165, -0.2, 0.74), (0.11, -0.4, 0.76),
       (-0.11, -0.4, 0.76), (0.03, -0.53, 0.815), (-0.03, -0.53, 0.815), (0.0, -0.3, 0.715)]
loose("jaw", hull([hp(p) for p in JAW], lift=False), shade(STEEL, 1.02), "jaw")
loose("gum", hs(ell((0, -0.22, 0.857), (0.13, 0.2, 0.014))), GUM, "head")

# The comb: four square pillars hanging from the jaw's front
for j, x in enumerate((-0.09, -0.03, 0.03, 0.09)):
    y = -0.44 + 0.04 * abs(x) / 0.09
    L = 0.13 if abs(x) < 0.05 else 0.11
    pts = []
    for dx in (-0.024, 0.024):
        for dy in (-0.022, 0.022):
            pts += [hp((x + dx, y + dy, 0.77)), hp((x + dx * 0.85, y + dy * 0.85, 0.77 - L))]
    loose(f"pillar{j}", hull(pts, lift=False), shade(STEEL, 0.98), "jaw")

# The crescent: the jaw's sides sweeping back past the skull and up behind it, jagged on their outer edge
ARC = [(0.165, -0.14, 0.77), (0.2, -0.02, 0.8), (0.21, 0.09, 0.88), (0.18, 0.16, 0.99), (0.11, 0.17, 1.09)]
WID = [0.15, 0.17, 0.16, 0.13, 0.07]
for s, nm in ((1, "R"), (-1, "L")):
    A = [np.array((x * s, y, z)) for x, y, z in ARC]
    for j in range(len(A) - 1):
        pts = []
        for q, w_ in ((A[j], WID[j]), (A[j + 1], WID[j + 1])):
            out_ = np.array((s, 0.0, 0.0))
            tdir = unit(*(A[j + 1] - A[j]))
            inn = np.cross(tdir, out_); inn /= np.linalg.norm(inn)
            for a_ in (-1, 1):
                for b_ in (-1, 1):
                    pts.append(hp(q + out_ * a_ * 0.03 + inn * b_ * w_ * 0.5))
        loose(f"arc{j}{nm}", hull(pts, lift=False), shade(STEEL, 0.97), "jaw")
        # a jagged tooth on the outer edge of each piece
        m = (A[j] + A[j + 1]) / 2
        tdir = unit(*(A[j + 1] - A[j]))
        inn = np.cross(tdir, np.array((s, 0.0, 0.0))); inn /= np.linalg.norm(inn)
        o = -inn if (inn[1] < 0 and j < 2) or inn[2] < 0 else inn
        pts = [hp(m + tdir * 0.03 + np.array((0.018 * s, 0, 0))), hp(m + tdir * 0.03 - np.array((0.018 * s, 0, 0))),
               hp(m - tdir * 0.03 + np.array((0.018 * s, 0, 0))), hp(m - tdir * 0.03 - np.array((0.018 * s, 0, 0))),
               hp(m - o * (WID[j] * 0.5 + 0.045) + tdir * 0.02)]
        loose(f"arct{j}{nm}", hull(pts, lift=False), shade(STEEL, 0.97), "jaw")

TEETH = [(0.02, -0.43, 0), (0.06, -0.422, 6), (0.097, -0.403, 20), (0.124, -0.37, 40), (0.137, -0.325, 62), (0.142, -0.275, 80)]
for i, (x, y, turn) in enumerate(TEETH):
    for s, nm in ((1, "R"), (-1, "L")):
        k.add(f"tooth{i}{nm}", hs(ell((x * s, y, 0.852), (0.02, 0.014, 0.022), rot=(0, 0, turn * s), deform=boxy(6))),
              TOOTH, "head", fuse=False, surface="shell")

# A few pale shards of steel hanging over the head
for j, (c, sz) in enumerate([((0.27, -0.05, 1.12), 0.045), ((-0.25, -0.2, 1.1), 0.04), ((0.05, 0.14, 1.2), 0.04)]):
    rs = np.random.RandomState(40 + j)
    D = rs.normal(size=(7, 3)); D /= np.linalg.norm(D, axis=1, keepdims=True)
    loose(f"shard{j}", hull(np.array(c) + D * sz * np.array((1.3, 0.8, 0.5)), lift=False), SHARD, "head")

k.paint(lambda P: inside(np.abs(P[:, 2] - hp((0, 0, 0.968))[2]) - 0.009, 0.005) *
        smoothstep(hp((0, -0.18, 0))[1], hp((0, -0.22, 0))[1], P[:, 1]) * smoothstep(0.035, 0.07, np.abs(P[:, 0])),
        SEAM, on=["skull", "brow"])

# Small fierce eyes under the brow, as steelix.py's, the iris blue
SCLERA, IRIS, PUPIL, LID, INK = srgb("#F2F2EE"), srgb("#2A8FE0"), srgb("#141216"), srgb(STEEL), srgb("#1C1E26")
def eye(u, v):
    c = np.tile(SCLERA, (len(u), 1))
    c[((u + 0.35) / 0.45) ** 2 + ((v + 0.1) / 0.78) ** 2 < 1] = IRIS
    c[((u + 0.4) / 0.2) ** 2 + ((v + 0.1) / 0.42) ** 2 < 1] = PUPIL
    c[u * u + v * v > 0.78] = INK
    edge = 0.25 + 0.45 * u
    c[v > edge + 0.12] = LID
    c[np.abs(v - edge) <= 0.12] = INK
    return c
de = unit(0.62, -0.76, 0.05)
def eyes(state):
    k.decal(hp((0.112, -0.262, 0.952)), de, (0.046, 0.03), eye_look(state, eye, LID, INK), eps=0.006, rings=20,
            segs=56, mirror=True)

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
