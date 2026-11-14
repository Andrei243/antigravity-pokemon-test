# Rhydon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rhydon.py -- <the export's Rhydon.glb> <out.glb>
# Grown from rhyhorn.py (its colours, its plates of rock, its horn, fangs, claws and red scowling eyes): Rhyhorn
# stood up. From the references: a heavy grey biped with a big round belly of cream crossed by dark ribs, short thick
# legs with the thighs one with the body, three white claws a foot; a rhino's head carried forward on a short neck,
# the mouth wide open and pink with a white fang down at each front corner of the upper jaw; on the nose a long tan
# drill of a horn ringed by a spiral groove; two pointed ears up and back on the head, and behind them a jagged frill
# of rock; small red eyes under a scowling lid; thick arms a little forward and down, three white claws a hand; small
# plates of rock down the back; a thick tail lying on the ground behind, ringed by dark grooves.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import bmesh
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="shell")

# --- the line's colours (rhyhorn.py's), and Rhydon's own --------------------------------------------------------
GREY, GREYDARK, HORN, CLAW, HOLE, MOUTHLINE = "#9AA6B6", "#727E90", "#C2CAD6", "#EEF0F2", "#262830", "#30343E"
IRIS, PUPIL, SCLERA, INK = "#D8302C", "#2A0A0A", "#F4F0EC", "#22262E"
CREAM, RIB, DRILL, GROOVE, MOUTH = "#DCCDB0", "#9A8C78", "#CDBFA6", "#5E5446", "#D46E7C"


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


def claws(tag, foot, toward, bone, n=3, spread=0.034, size=0.022, down=-0.35):
    """Three white claws along a foot's front edge, short stout cones turned forward and down."""
    foot = np.array(foot, float)
    for j in range(n):
        dx = (j - (n - 1) / 2) * spread
        b = foot + np.array([dx, 0, 0])
        d = unit(dx * 1.2, toward, down)
        k.add(f"claw{tag}{j}", tube([b - d * 0.01, b + d * size * 0.8, b + d * size * 1.5],
                                    [size * 0.62, size * 0.4, 0.002], seg=10, per=4), CLAW, bone, fuse=False,
              surface="shell")


def drill(base, tip, r0, bone, grooves=4):
    """
    The horn as a drill: a tan cone, and round it thin dark bands slanting across its axis, spaced up its length,
    which read as a spiral groove (each a short loose band standing a hair off the cone).
    """
    base, tip = np.array(base, float), np.array(tip, float)
    ax = tip - base; L = np.linalg.norm(ax); a = ax / L
    k.add("horn", tube([base - a * L * 0.12, base + a * L * 0.45, tip], [r0, r0 * 0.6, 0.004], seg=24, per=8), DRILL,
          bone, fuse=False, surface="shell")
    side = unit(*np.cross(a, (1.0, 0, 0)))
    for g in range(grooves):
        t = 0.08 + 0.7 * g / grooves
        r = np.interp(t, [-0.12, 0.45, 1.0], [r0, r0 * 0.6, 0.004]) + 0.002
        c = base + a * L * t
        tilt = unit(*(a + side * 0.45))                 # slanting across the axis: a spiral's turn
        k.add(f"groove{g}", band(c, tilt, r, 0.0035, 0.004), GROOVE, bone, fuse=False, surface="shell")


def band(c, a, r, half, wall, seg=32):
    """A thin open ring about c, its axis a: a short tube with no ends, its wall thick, its faces wound outward."""
    a = unit(*a)
    e1 = unit(*np.cross(a, (0, 0, 1.0) if abs(a[2]) < 0.9 else (1.0, 0, 0)))
    e2 = np.cross(a, e1)
    ang = np.linspace(0, 2 * math.pi, seg, endpoint=False)
    rad = np.cos(ang)[:, None] * e1 + np.sin(ang)[:, None] * e2
    rings = [c - a * half + rad * r, c + a * half + rad * r, c + a * half + rad * (r - wall), c - a * half + rad * (r - wall)]
    V = np.vstack(rings)
    F = []
    for i in range(4):
        j = (i + 1) % 4
        for s in range(seg):
            t = (s + 1) % seg
            F.append([i * seg + s, i * seg + t, j * seg + t, j * seg + s])
    # wind outward: the first face's normal must point away from the axis
    p = V[F[0]]
    nrm = np.cross(p[1] - p[0], p[2] - p[0])
    if nrm @ (p.mean(0) - c - a * (a @ (p.mean(0) - c))) < 0:
        F = [f[::-1] for f in F]
    return V, F


# --- the frame: the skeleton's joints and the masses on them -----------------------------------------------------
HEAD = np.array([0.0, -0.05, 0.62])                     # the head bone
ARM = np.array([0.15, -0.03, 0.52])
LEG = np.array([0.11, 0.0, 0.24])

# The body: a big round belly over wide hips, the thighs one with it
def pear(V):
    V = V.copy()
    low = np.clip(-V[:, 2], 0, 1)
    V[:, :2] *= (1 + 0.14 * low)[:, None]
    return V
TORSO_C, TORSO_R = np.array([0.0, 0.0, 0.43]), np.array([0.2, 0.18, 0.21])
k.add("torso", ell(TORSO_C, TORSO_R, deform=pear), GREY, "body")
k.add("chest", ell((0, -0.01, 0.56), (0.17, 0.14, 0.1)), GREY, "body")
k.add("hips", ell((0, 0.03, 0.29), (0.2, 0.16, 0.12)), GREY, "body")

# The legs: thighs fused into the hips, short thick shins, broad feet with three white claws
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"thigh{side}", ell(np.array((0.135, -0.005, 0.24)) * m, (0.105, 0.12, 0.12)), GREY, f"leg{side}")
    k.add(f"shin{side}", tube(np.array([(0.145, -0.01, 0.17), (0.15, -0.02, 0.1), (0.15, -0.025, 0.055)]) * m,
                              [0.08, 0.072, 0.07]), GREY, f"leg{side}")
    k.add(f"foot{side}", ell(np.array((0.152, -0.05, 0.035)) * m, (0.082, 0.105, 0.04)), GREY, f"leg{side}")
    claws("foot" + side, np.array((0.152, -0.14, 0.03)) * m, -1, f"leg{side}", spread=0.036, size=0.024)

# The tail: thick at the hips and lying on the ground behind, tapering, its tip a little raised
TAIL = [(0, 0.12, 0.32), (0, 0.27, 0.2), (0, 0.42, 0.11), (0, 0.55, 0.1), (0, 0.63, 0.14)]
k.add("tail", tube(TAIL, [0.115, 0.09, 0.065, 0.042, 0.016]), GREY, "tail")
# dark grooves ringing it, a little apart
k.paint(lambda P: sum(inside(np.abs(P[:, 1] - y) - 0.005, 0.0025) for y in (0.24, 0.32, 0.4, 0.48))
        * (P[:, 1] > 0.18), GREYDARK, on=["tail"])

# The neck, short and thick, carrying the head forward
k.add("neck", tube([(0, 0.0, 0.56), (0, -0.04, 0.63), (0, -0.07, 0.67)], [0.11, 0.095, 0.085]), GREY, "head")

# The head: a rhino's, a broad crown over a long upper jaw, the lower jaw dropped open under it
k.add("cranium", ell((0, -0.07, 0.72), (0.122, 0.122, 0.102), deform=boxy(2.6)), GREY, "head")
def wedge(V):
    V = boxy(2.6)(V)
    V[:, 0] *= 1 + 0.18 * V[:, 1]                        # narrower toward the front
    return V
k.add("snout", ell((0, -0.205, 0.69), (0.098, 0.125, 0.064), rot=(-8, 0, 0), deform=wedge), GREY, "head")
k.add("lowjaw", ell((0, -0.165, 0.588), (0.092, 0.11, 0.042), rot=(14, 0, 0), deform=wedge), GREY, "head")
k.add("cheek", ell((0, -0.1, 0.63), (0.1, 0.07, 0.07)), GREY, "head")
# the mouth: wide open between the jaws, pink within
def gape(V):
    V = V.copy()
    V[:, 2] += 0.3 * V[:, 1] ** 2
    return V
k.add("mouth", ell((0, -0.225, 0.64), (0.07, 0.11, 0.035), rot=(-6, 0, 0), deform=gape), MOUTH, "head", carve=True)
fang("fangR", (0.045, -0.27, 0.665), (0, -0.1, -1), 0.032, 0.012, "head")
fang("fangL", (-0.045, -0.27, 0.665), (0, -0.1, -1), 0.032, 0.012, "head")

# the brow over each eye, slanting down toward the snout (Rhyhorn's)
k.add("brow", ell((0.072, -0.16, 0.765), (0.036, 0.07, 0.018), rot=(-20, 0, -8), deform=boxy(3)), GREY, "head",
      mirror=True)
# two pointed ears, up, out and back: flat points as Rhyhorn's head's corners are
EAR0, EAR1 = np.array([0.075, -0.03, 0.78]), np.array([0.13, 0.03, 0.89])
k.add("ear", blade([EAR0, (EAR0 + EAR1) / 2 + (0.008, 0, 0), EAR1], [0.05, 0.035, 0.008], (0, 1, 0), 0.5), GREY,
      "head", mirror=True)
# the jagged frill of rock behind them: points out and back from the back of the head
FRILL = [((0.09, 0.02, 0.72), (0.75, 0.6, 0.15), 0.08), ((0.08, 0.05, 0.66), (0.7, 0.7, -0.25), 0.065),
         ((0.04, 0.05, 0.78), (0.35, 0.8, 0.5), 0.07)]
for j, (b, d, l) in enumerate(FRILL):
    b = np.array(b); d = unit(*d)
    k.add(f"frill{j}", blade([b - d * 0.02, b + d * l * 0.5, b + d * l], [0.03, 0.02, 0.004], (0, 0, 1), 0.5), GREY,
          "head", mirror=True)

# the drill on the nose
drill((0, -0.29, 0.72), (0, -0.46, 0.83), 0.056, "head")

# The arms: thick, out from the shoulders, a little forward and down; a hand of three white claws
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    SH, EL, WR = ARM + (0.0, 0, 0.01), np.array((0.25, -0.07, 0.47)), np.array((0.3, -0.15, 0.46))
    k.add(f"arm{side}", tube(np.array([SH, EL, WR]) * m, [0.085, 0.072, 0.064]), GREY, f"arm{side}")
    HC = np.array((0.31, -0.19, 0.46))
    k.add(f"hand{side}", ell(HC * m, (0.06, 0.06, 0.06)), GREY, f"arm{side}")
    claws("hand" + side, np.array((0.315, -0.235, 0.47)) * m, -1, f"arm{side}", spread=0.032, size=0.022, down=0.1)

# Small plates of rock down the back (Rhyhorn's), their points running back
def torso_normal(p):
    q = (np.array(p) - TORSO_C) / TORSO_R ** 2
    return unit(*q)
for j, (c, d, l, w) in enumerate([((0, 0.15, 0.58), (0, 0.7, 0.7), 0.1, 0.08), ((0, 0.19, 0.46), (0, 0.6, 0.2), 0.1, 0.08),
                                  ((0, 0.18, 0.34), (0, 0.8, -0.3), 0.09, 0.07)]):
    rock_plate(f"back{j}", c, torso_normal(c), d, l, w, 0.035, 0.02, "body", tone=0.95 + 0.04 * (j % 2))

# The cream belly and its dark ribs across it, painted round the body's front
def belly(P):
    a = np.abs(np.arctan2(P[:, 0], -(P[:, 1] - 0.02)))
    top = 0.58 - 0.06 * (a / 1.0) ** 2
    return inside(a - 1.05, 0.01) * inside(P[:, 2] - top, 0.004) * inside(0.24 - P[:, 2], 0.004) * (P[:, 1] < 0.08)
def ribs(P):
    return belly(P) * sum(inside(np.abs(P[:, 2] - z) - 0.0045, 0.0025) for z in (0.31, 0.38, 0.45, 0.52))
k.paint(belly, CREAM, on=["torso", "chest", "hips"])
k.paint(ribs, RIB, on=["torso", "chest", "hips"])


# --- the eyes: Rhyhorn's, red with a dark pupil, under the lid sloping down to the snout (scowling) ---------------
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
EYE_C, EYE_N, EYE_H = np.array([0.08, -0.19, 0.735]), unit(0.5, -0.82, 0.22), (0.034, 0.028)
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


finish(voxel=0.0075)
k.eye_states(eyes)
k.export(out)
