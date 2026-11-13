# Golem sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/golem.py -- <the export's Golem.glb> <out.glb>
# Graveler grown into a walking boulder: a great round shell of flat grey plates parted by dark grooves, and out of it
# a tan reptile's head with a red eye under a scowling brow and a gaping pink mouth, two short arms raised with white
# claws, and two thick legs on clawed feet. Its plates are cut stone like Graveler's rocks, drawn flat-faced.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import bmesh
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

PLATE, GROOVE, TAN, TANDARK, CLAW, MOUTH = "#8C9088", "#4C504A", "#CDAA92", "#B08E7A", "#F2F2F0", "#E0788A"
C = np.array([0.0, 0.06, 0.54])          # the shell's middle, on the body bone
R = 0.36                                 # its radius


def shade(h, f):
    """A colour a little lighter or darker (f above or below 1), still written as on screen."""
    c = np.clip(np.array([int(h[i:i + 2], 16) for i in (1, 3, 5)]) * f, 0, 255).astype(int)
    return "#%02X%02X%02X" % tuple(c)


def hull(V):
    """Points made into a solid: their convex hull, its coplanar faces merged (onix.py's rock)."""
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

# The shell: a dark sphere under the plates, which shows only in the grooves between them
k.add("shell", ell(C, (R, R * 0.97, R)), GROOVE, "body", surface="shell")

# The plates: a cell of a spherical Voronoi of points spread over the sphere, each cut back from its neighbours by a
# groove and stood as a flat slab with bevelled edges, its top a plane tangent to the sphere
N = 46
i = np.arange(N) + 0.5
ph = np.arccos(1 - 2 * i / N); th = math.pi * (1 + 5 ** 0.5) * i
P = np.stack([np.cos(th) * np.sin(ph), np.sin(th) * np.sin(ph), np.cos(ph)], 1)
rs = np.random.RandomState(76)
P += rs.normal(size=P.shape) * 0.06
P /= np.linalg.norm(P, axis=1, keepdims=True)
HEADDIR = unit(0, -1, -0.32)
ARMDIR = [unit(1, -0.5, 0.05), unit(-1, -0.5, 0.05)]
GAP = 0.011
for n_, c in enumerate(P):
    if c @ HEADDIR > math.cos(math.radians(17)) or c[2] < -0.9:
        continue                        # the mouth of the shell the head comes out of, and the underside
    t1 = np.cross(c, (0, 0, 1.0))
    if np.linalg.norm(t1) < 1e-3:
        t1 = np.cross(c, (1.0, 0, 0))
    t1 /= np.linalg.norm(t1); t2 = np.cross(c, t1)
    poly = [np.array(p) * 0.3 for p in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
    for m in P:
        if m is c or np.allclose(m, c):
            continue
        dmn = m - c
        a, b = t1 @ dmn, t2 @ dmn
        lim = R * (1 - c @ m) - GAP * math.hypot(a, b)       # the bisector, moved in by the groove's half
        # clip the polygon by a x + b y <= lim
        new = []
        for j in range(len(poly)):
            p, q = poly[j], poly[(j + 1) % len(poly)]
            fp, fq = a * p[0] + b * p[1] - lim, a * q[0] + b * q[1] - lim
            if fp <= 0:
                new.append(p)
            if fp * fq < 0:
                new.append(p + (q - p) * (fp / (fp - fq)))
        poly = new
        if len(poly) < 3:
            break
    if len(poly) < 3:
        continue
    poly = np.array(poly)
    top = [C + c * (R + 0.026) + t1 * x * 0.86 + t2 * y * 0.86 for x, y in poly]
    mid = [C + c * (R + 0.012) + t1 * x + t2 * y for x, y in poly]
    low = []
    for x, y in poly:
        p = c * R + t1 * x + t2 * y
        low.append(C + p / np.linalg.norm(p) * (R - 0.03))
    tone = 0.9 + 0.16 * rs.rand()
    k.add(f"plate{n_}", hull(np.vstack([top, mid, low])), shade(PLATE, tone), "body", fuse=False, surface="shell")
    ROCKS.append(f"plate{n_}")

# The head: out of the front of the shell low down, a long tan reptile's head on a short neck, a brow over each eye,
# the upper jaw a squared snout, the lower jaw on its own bone under a gaping mouth
k.add("neck", tube([(0, -0.18, 0.43), (0, -0.33, 0.44)], [0.085, 0.08]), TAN, "head")
k.add("skull", ell((0, -0.38, 0.47), (0.092, 0.1, 0.075), deform=boxy(2.4)), TAN, "head")
def snoutf(V):
    V = boxy(2.8)(V)
    V[:, 0] *= 1 + 0.22 * V[:, 1]                # narrower toward the front
    return V
k.add("snout", ell((0, -0.5, 0.455), (0.066, 0.1, 0.04), rot=(-6, 0, 0), deform=snoutf), TAN, "head")
k.add("brow", ell((0.045, -0.43, 0.515), (0.045, 0.045, 0.022), rot=(-10, 0, -18), deform=boxy(3.0)), TAN, "head",
      mirror=True)
k.add("lowjaw", ell((0, -0.465, 0.375), (0.062, 0.09, 0.03), rot=(18, 0, 0), deform=boxy(2.6)), TAN, "jaw")
k.add("chin", ell((0, -0.39, 0.395), (0.07, 0.06, 0.04)), TAN, "jaw")

# The mouth: wide open between the jaws, pink within, its corners running back under the eyes
def gape(V):
    V = V.copy()
    V[:, 2] += 0.35 * V[:, 1] ** 2
    return V
k.add("mouth", ell((0, -0.5, 0.415), (0.056, 0.095, 0.03), rot=(-10, 0, 0), deform=gape), MOUTH, "head", carve=True)
# two fangs hanging from the upper jaw and two rising from the lower, near the front
def fangf(V):
    V = V.copy()
    t = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.75 * t; V[:, 1] *= 1 - 0.6 * t
    return V
for s in (1, -1):
    k.add(f"fang{s}", ell((0.036 * s, -0.56, 0.43), (0.009, 0.008, 0.017), deform=fangf), CLAW, "head", fuse=False)
    k.add(f"tusk{s}", ell((0.034 * s, -0.53, 0.397), (0.008, 0.007, 0.014), rot=(180, 0, 0), deform=fangf), CLAW,
          "jaw", fuse=False)

# The arms: short and thick, out of the shell's sides, raised with the hands open over the shoulders
SH = np.array([0.27, -0.12, 0.52]); EL = np.array([0.42, -0.16, 0.55]); WR = np.array([0.47, -0.18, 0.7])
k.add("arm", tube([SH, EL, WR], [0.07, 0.065, 0.055]), TAN, "arm{s}", mirror=True)
HC = np.array([0.48, -0.19, 0.75])
k.add("palm", ell(HC, (0.055, 0.04, 0.055), rot=(0, -15, 0)), TAN, "arm{s}", mirror=True)
# three white claws curling up and out, and a thumb's claw inward
for j, (a, b) in enumerate([((0.03, -0.01, 0.04), (0.07, -0.02, 0.1)), ((-0.0, -0.02, 0.05), (0.01, -0.04, 0.12)),
                             ((-0.03, -0.01, 0.04), (-0.05, -0.02, 0.1)), ((-0.045, -0.02, -0.005), (-0.1, -0.04, 0.02))]):
    p0 = HC + np.array(a); p1 = HC + np.array(b)
    k.add(f"finger{j}", tube([HC + np.array(a) * 0.4, p0], [0.022, 0.02]), TAN, "arm{s}", mirror=True)
    k.add(f"claw{j}", tube([p0 - (p1 - p0) * 0.15, p0 + (p1 - p0) * 0.5, p1], [0.02, 0.013, 0.003], seg=8, per=4),
          CLAW, "arm{s}", fuse=False, mirror=True, surface="shell")

# The legs: thick, splayed out from under the shell, on wide flat feet with four white claws
HP = np.array([0.17, -0.06, 0.26]); KN = np.array([0.25, -0.12, 0.16]); AN = np.array([0.26, -0.15, 0.08])
k.add("thigh", tube([HP, KN, AN], [0.13, 0.12, 0.09]), TAN, "leg{s}", mirror=True)
k.add("foot", ell((0.265, -0.19, 0.045), (0.1, 0.11, 0.047), deform=boxy(2.6)), TAN, "leg{s}", mirror=True)
for j, (dx, dy) in enumerate([(-0.065, -0.07), (-0.022, -0.095), (0.022, -0.095), (0.065, -0.07)]):
    b0 = np.array([0.265 + dx, -0.19 + dy, 0.03])
    d = unit(dx * 0.8, dy, 0)
    k.add(f"toe{j}", tube([b0 - d * 0.015, b0 + d * 0.03 + [0, 0, -0.008], b0 + d * 0.055 + [0, 0, -0.022]],
                          [0.016, 0.011, 0.003], seg=8, per=4), CLAW, "leg{s}", fuse=False, mirror=True, surface="shell")

# A darker tan under the jaw and on the soles
k.paint(lambda P: smoothstep(0.405, 0.375, P[:, 2]) * smoothstep(-0.36, -0.42, P[:, 1]), TANDARK, on=["lowjaw", "chin"])

# The eyes: small and fierce under the brow, set on the sides of the head behind the snout, a red iris in white with
# a dark pupil to the front, the lid of tan slanting down to the snout (Graveler's way of making them)
SCLERA, IRIS, PUPIL, LID, INK = srgb("#F6F2EC"), srgb("#D8302C"), srgb("#3A0E0E"), srgb(TAN), srgb("#3A2A24")
def eye(u, v):
    c = np.tile(SCLERA, (len(u), 1))
    c[((u + 0.25) / 0.45) ** 2 + ((v + 0.08) / 0.8) ** 2 < 1] = IRIS
    c[((u + 0.3) / 0.18) ** 2 + ((v + 0.1) / 0.4) ** 2 < 1] = PUPIL
    c[((u + 0.08) / 0.12) ** 2 + ((v - 0.2) / 0.14) ** 2 < 1] = (1, 1, 1)
    c[u * u + v * v > 0.84] = INK
    edge = 0.3 + 0.4 * u
    c[v > edge + 0.12] = LID
    c[np.abs(v - edge) <= 0.12] = INK
    return c
de = unit(0.72, -0.62, 0.3)
E = np.array([0.064, -0.43, 0.49])
def eyes(state):
    k.decal(E, de, (0.033, 0.025), eye_look(state, eye, LID, INK), up=(0, 0.25, 1), eps=0.005, rings=20, segs=56,
            mirror=True)

# The plates and claws drawn flat-faced: the loose pieces come after the fused body in its faces, in order
loose = [(p["name"], len(p["F"])) for p in k.parts if not p["fuse"] and not p["carve"]]
body = k.build(voxel=0.008, smooth=2)
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
