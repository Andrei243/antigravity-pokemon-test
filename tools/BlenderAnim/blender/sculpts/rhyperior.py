# Rhyperior sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rhyperior.py -- <the export's Rhyperior.glb> <out.glb>
# Grown from rhydon.py (and so rhyhorn.py): the same plates of rock made the same way, the same drill of a horn with
# its spiral groove, the same white fangs and claws and red scowling eyes. From the references: far heavier, its hide
# grey-brown, a huge round trunk with the small head sunk between broad shoulders; armour of hard orange plates on
# the head, over the shoulders and upper back, in a belt right round the waist, on each knee and down each forearm;
# a big pale drill on the nose and a smaller pale horn above it; a wide grin of white fangs over a pale lower jaw;
# great forearms ending in three thick pale fingers round a hole in the palm; short thick legs, three white claws a
# foot; a thick tail ending in a dark lumpy club of rock lying on the ground.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import bmesh
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, boxy, lumpy, eye_look, args

src, out = args()
k = Kit(src, coat="shell")

# --- the line's colours (rhyhorn.py's), and Rhyperior's own -----------------------------------------------------
GREY, GREYDARK, HORN, CLAW, HOLE, MOUTHLINE = "#9AA6B6", "#727E90", "#C2CAD6", "#EEF0F2", "#262830", "#30343E"
IRIS, PUPIL, SCLERA, INK = "#D8302C", "#2A0A0A", "#F4F0EC", "#22262E"
HIDE, ORANGE, PALE, GROOVE, CLUB, GRIN, JAW = "#7E6E6E", "#E8762C", "#D6D8DE", "#9A9CA6", "#5A5254", "#33282A", "#C9C6CC"


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


def armour(name, c, n, up, w, h, thick, bone, color=ORANGE, tone=1.0, sink=0.03, cut=0.3):
    """
    A plate of armour on the body (rock_plate's way, flat and set close): an octagon w by h (its corners cut by cut)
    in the surface's plane, its top a flat face with bevelled edges standing thick off the surface, its root sunk.
    """
    c, n = np.array(c, float), unit(*n)
    u = np.array(up, float); u = u - n * (u @ n); u /= np.linalg.norm(u)
    s = np.cross(u, n)
    oct_ = [(-1 + cut, -1), (1 - cut, -1), (1, -1 + cut), (1, 1 - cut), (1 - cut, 1), (-1 + cut, 1), (-1, 1 - cut),
            (-1, -1 + cut)]
    pts = []
    for a, b in oct_:
        p = c + s * a * w / 2 + u * b * h / 2
        pts.append(p + n * thick * 0.6)
        pts.append(c + (p - c) * 0.88 + n * thick)
        pts.append(p - n * sink)
    k.add(name, hull(np.array(pts)), shade(color, tone), bone, fuse=False, surface="shell")
    ROCKS.append(name)


def fang(name, base, up, length, width, bone):
    """A small white fang, a cone from base along up."""
    base, up = np.array(base, float), unit(*up)
    k.add(name, tube([base - up * 0.006, base + up * length * 0.5, base + up * length],
                     [width, width * 0.6, 0.002], seg=10, per=4), CLAW, bone, fuse=False, surface="shell")


def claws(tag, foot, toward, bone, n=3, spread=0.034, size=0.022, down=-0.35, color=CLAW):
    """Three white claws along a foot's front edge, short stout cones turned forward and down."""
    foot = np.array(foot, float)
    for j in range(n):
        dx = (j - (n - 1) / 2) * spread
        b = foot + np.array([dx, 0, 0])
        d = unit(dx * 1.2, toward, down)
        k.add(f"claw{tag}{j}", tube([b - d * 0.01, b + d * size * 0.8, b + d * size * 1.5],
                                    [size * 0.62, size * 0.4, 0.002], seg=10, per=4), color, bone, fuse=False,
              surface="shell")


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
    p = V[F[0]]
    nrm = np.cross(p[1] - p[0], p[2] - p[0])
    if nrm @ (p.mean(0) - c - a * (a @ (p.mean(0) - c))) < 0:
        F = [f[::-1] for f in F]
    return V, F


def drill(base, tip, r0, bone, grooves=4, color=PALE, groove=GROOVE, name="horn"):
    """
    The horn as a drill (rhydon.py's): a cone, and round it thin bands slanting across its axis, spaced up its length,
    which read as a spiral groove.
    """
    base, tip = np.array(base, float), np.array(tip, float)
    ax = tip - base; L = np.linalg.norm(ax); a = ax / L
    k.add(name, tube([base - a * L * 0.12, base + a * L * 0.45, tip], [r0, r0 * 0.6, 0.004], seg=24, per=8), color,
          bone, fuse=False, surface="shell")
    side = unit(*np.cross(a, (1.0, 0, 0)))
    for g in range(grooves):
        t = 0.08 + 0.7 * g / grooves
        r = np.interp(t, [-0.12, 0.45, 1.0], [r0, r0 * 0.6, 0.004]) + 0.002
        c = base + a * L * t
        tilt = unit(*(a + side * 0.45))
        k.add(f"{name}groove{g}", band(c, tilt, r, 0.0035, 0.004), groove, bone, fuse=False, surface="shell")


# --- the frame: the skeleton's joints and the masses on them -----------------------------------------------------
HEAD = np.array([0.0, -0.1, 0.66])                      # the head bone
ARM = np.array([0.22, -0.03, 0.6])

# The trunk: a huge barrel, widest at the waist, the shoulders broad and high, the head sunk between them
def barrel(V):
    V = V.copy()
    low = np.clip(-V[:, 2], 0, 1)
    V[:, :2] *= (1 + 0.1 * low)[:, None]
    return V
TRUNK_C, TRUNK_R = np.array([0.0, 0.02, 0.44]), np.array([0.27, 0.23, 0.26])
k.add("trunk", ell(TRUNK_C, TRUNK_R, deform=barrel), HIDE, "body")
k.add("hips", ell((0, 0.03, 0.28), (0.26, 0.21, 0.14)), HIDE, "body")
k.add("shoulder", ell((0.19, -0.01, 0.6), (0.13, 0.14, 0.12)), HIDE, "body", mirror=True)

# The legs: thighs fused into the hips, short thick shins, broad feet with three white claws
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"thigh{side}", ell(np.array((0.17, -0.02, 0.22)) * m, (0.12, 0.14, 0.13)), HIDE, f"leg{side}")
    k.add(f"shin{side}", tube(np.array([(0.18, -0.03, 0.15), (0.185, -0.04, 0.09), (0.19, -0.045, 0.055)]) * m,
                              [0.09, 0.085, 0.082]), HIDE, f"leg{side}")
    k.add(f"foot{side}", ell(np.array((0.19, -0.07, 0.038)) * m, (0.095, 0.115, 0.042)), HIDE, f"leg{side}")
    claws("foot" + side, np.array((0.19, -0.17, 0.032)) * m, -1, f"leg{side}", spread=0.04, size=0.026)

# The tail: thick, lying on the ground behind, ending in a dark lumpy club of rock
k.add("tail", tube([(0, 0.15, 0.32), (0, 0.33, 0.19), (0, 0.47, 0.14)], [0.13, 0.09, 0.075]), HIDE, "tail")
k.add("club", ell((0, 0.57, 0.135), (0.13, 0.13, 0.12), deform=lumpy(3, 3, 0.14)), CLUB, "tail")

# The head: small for the body, sunk between the shoulders, a broad brow over a short snout
k.add("cranium", ell((0, -0.12, 0.74), (0.13, 0.13, 0.105), deform=boxy(2.5)), HIDE, "head")
def wedge(V):
    V = boxy(2.6)(V)
    V[:, 0] *= 1 + 0.15 * V[:, 1]
    return V
k.add("snout", ell((0, -0.21, 0.7), (0.105, 0.09, 0.075), deform=wedge), HIDE, "head")
k.add("jaw", ell((0, -0.18, 0.64), (0.1, 0.08, 0.05), deform=boxy(2.4)), HIDE, "head")
k.add("brow", ell((0.065, -0.22, 0.79), (0.055, 0.04, 0.022), rot=(-10, 0, -12), deform=boxy(3)), HIDE, "head",
      mirror=True)
# the grin: a wide shallow hollow across the face, dark within, curving up at its ends, over a pale lower jaw
def grin(V):
    V = V.copy()
    V[:, 2] += 0.5 * V[:, 0] ** 2
    return V
k.add("mouth", ell((0, -0.282, 0.668), (0.085, 0.035, 0.014), deform=grin), GRIN, "head", carve=True)
k.paint(lambda P: inside(P[:, 2] - (0.656 + 0.03 * (P[:, 0] / 0.09) ** 2), 0.003) * inside(P[:, 1] + 0.2, 0.01)
        * inside(0.6 - P[:, 2], 0.004), JAW, on=["jaw", "snout"])
for j, x in enumerate((0.022, 0.06)):
    fang(f"toothR{j}", (x, -0.275, 0.676 + 0.25 * x * x), (0, -0.1, -1), 0.028, 0.012, "head")
    fang(f"toothL{j}", (-x, -0.275, 0.676 + 0.25 * x * x), (0, -0.1, -1), 0.028, 0.012, "head")

# the two horns: the big drill on the nose and a smaller pale horn above it
drill((0, -0.27, 0.75), (0, -0.43, 0.84), 0.062, "head", grooves=3)
k.add("horn2", tube([(0, -0.15, 0.82), (0, -0.15, 0.88), (0, -0.14, 0.94)], [0.036, 0.022, 0.004], seg=20, per=6),
      PALE, "head", fuse=False, surface="shell")

# The arms: thick upper arms, the forearms great and wide, a little forward and down; three thick pale fingers
# round the palm, a dark hole in its middle facing the front
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    SH, EL, WR = np.array((0.25, -0.03, 0.6)), np.array((0.34, -0.08, 0.53)), np.array((0.37, -0.22, 0.5))
    k.add(f"arm{side}", tube(np.array([SH, EL]) * m, [0.095, 0.09]), HIDE, f"arm{side}")
    k.add(f"forearm{side}", tube(np.array([EL, (EL + WR) / 2, WR]) * m, [0.095, 0.11, 0.115]), HIDE, f"arm{side}")
    HC = np.array((0.375, -0.27, 0.5))
    k.add(f"hand{side}", ell(HC * m, (0.1, 0.06, 0.1), deform=boxy(2.4)), HIDE, f"arm{side}")
    for j, (dx, dz) in enumerate(((-0.06, 0.055), (0.06, 0.055), (0.0, -0.075))):
        b = HC + np.array((dx, -0.03, dz))
        d = unit(dx * 0.6, -1, dz * 0.4)
        k.add(f"finger{side}{j}", tube(np.array([b - d * 0.01, b + d * 0.04, b + d * 0.075]) * m,
                                       [0.034, 0.03, 0.012], seg=16, per=4), PALE, f"arm{side}", fuse=False,
              surface="shell")

# --- the orange armour ---------------------------------------------------------------------------------------------
def trunk_point(a, z):
    """The point of the trunk at an angle a round it (0 the front, + to the model's right) and height z, and its
    outward normal."""
    t = np.clip((z - TRUNK_C[2]) / TRUNK_R[2], -0.99, 0.99)
    rr = math.sqrt(1 - t * t) * (1 + 0.1 * max(-t, 0))
    p = TRUNK_C + np.array([math.sin(a) * TRUNK_R[0] * rr, -math.cos(a) * TRUNK_R[1] * rr, z - TRUNK_C[2]])
    q = (p - TRUNK_C) / (TRUNK_R ** 2)
    return p, unit(*q)
# the belt right round the waist
for j in range(10):
    a = math.radians(-162 + 36 * j)
    p, n = trunk_point(a, 0.4)
    armour(f"belt{j}", p, n, (0, 0, 1), 0.15, 0.13, 0.03, "body", tone=0.96 + 0.05 * (j % 2))
# over the shoulders and upper back
for side, s in (("R", 1), ("L", -1)):
    for j, (a, z, w, h) in enumerate(((60, 0.66, 0.13, 0.12), (105, 0.64, 0.12, 0.11), (140, 0.6, 0.12, 0.11))):
        p, n = trunk_point(math.radians(a * s), z)
        armour(f"back{side}{j}", p, n, (0, 0, 1), w, h, 0.028, "body", tone=0.95 + 0.04 * j)
p, n = trunk_point(math.radians(180), 0.62)
armour("backmid", p, n, (0, 0, 1), 0.12, 0.12, 0.028, "body")
p, n = trunk_point(math.radians(180), 0.3)
armour("lowback", p, n, (0, 0, 1), 0.12, 0.09, 0.025, "body", tone=0.96)
# on the head, over the crown behind the small horn
# (a flat plate on the round crown stood off it as a box: the crown's armour is the head's own top, painted orange
# to a crisp edge, with a dark groove across it that parts it into two plates)
def crown(P):
    q = P - np.array([0, -0.12, 0.74])
    return inside(0.8 - P[:, 2] - 0.25 * np.clip(-(q[:, 1] + 0.05), 0, 1), 0.003) * inside(np.abs(q[:, 0]) - 0.15, 0.004)
k.paint(crown, ORANGE, on=["cranium"])
k.paint(lambda P: crown(P) * inside(np.abs(P[:, 1] + 0.06) - 0.006, 0.003), shade(ORANGE, 0.6), on=["cranium"])
# on each knee and down the outside of each forearm
for side, s in (("R", 1), ("L", -1)):
    armour(f"knee{side}", (0.18 * s, -0.15, 0.24), unit(0.3 * s, -1, 0.2), (0, 0, 1), 0.1, 0.09, 0.025, f"leg{side}")
    armour(f"fore{side}", (0.44 * s, -0.15, 0.54), unit(1 * s, 0.05, 0.45), (0, -1, -0.2), 0.11, 0.17, 0.022,
           f"arm{side}", sink=0.04)

# the holes in the palms, dark patches on the hands' fronts
for s in (1, -1):
    k.decal((0.375 * s, -0.33, 0.5), (0, -1, 0), (0.032, 0.032), HOLE, eps=0.004, rings=10, segs=36)


# --- the eyes: the line's, red with a dark pupil, under the lid sloping down to the snout (scowling) --------------
CI, CP, CS, CK, LID = srgb(IRIS), srgb(PUPIL), srgb(SCLERA), srgb(INK), srgb(HIDE)
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
EYE_C, EYE_N, EYE_H = np.array([0.085, -0.225, 0.755]), unit(0.5, -0.82, 0.2), (0.034, 0.028)
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


finish(voxel=0.008)
k.eye_states(eyes)
k.export(out)
