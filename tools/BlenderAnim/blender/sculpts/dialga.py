# Dialga sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/dialga.py -- <the export's Dialga.glb> <out.glb>
#
# From the references: a great blue quadruped, lean and long-legged; a long thick neck rising steeply from the chest;
# a small boxy head under a long steel crest that sweeps up and back from the brow, a steel face with red scowling
# eyes, a steel horn back from each side of the head and a point down from each jaw, the mouth open with two fangs;
# a broad steel breastplate over the chest and shoulders with a point rising at each shoulder and a blue diamond in
# its middle; a fan of steel blades rising up and back off the back; steel spikes down the back of the neck; long
# legs with steel cuffs low on the shins and three long steel claws a foot; a LONG tapering tail (the pictures show
# one as long as the body, not a short one); pale cyan lines up the neck, along the flanks and down the legs and tail.
# The export's joints: body (0, 0.04, 0.46), tail (0, 0.32, 0.44), head (0, -0.3, 0.93), legs (+-0.14, -0.15, 0.4),
# hinds (+-0.14, 0.22, 0.4).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import bmesh
import numpy as np
from sculptkit import Kit, ell, tube, sheet, srgb, inside, smoothstep, unit, boxy, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="metal")

BLUE, STEEL, STEEL2, CYAN, GEM = "#22437A", "#B4BFD0", "#96A2B8", "#62D6F2", "#3070E8"
PLATE, PLATE2 = "#A2ADC0", "#B8C2D2"           # the breastplate's facets, a medium silver-grey
EYE_RED, PUPIL, INK, MOUTH, FANG = "#E0303A", "#1A0A0E", "#1C2230", "#B8404E", "#F2F4F8"


def shade(h, f):
    c = np.clip(np.array([int(h[i:i + 2], 16) for i in (1, 3, 5)]) * f, 0, 255).astype(int)
    return "#%02X%02X%02X" % tuple(c)


def hull(V):
    """Points made into a solid: their convex hull, its coplanar faces merged (rhyperior.py's)."""
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


FLAT = []               # the loose pieces drawn flat-faced (hard metal)


def spike(name, base, d, wdir, length, width, thick, bone, color=STEEL, mid=0.55, midw=0.8):
    """A hard steel blade: a flat convex piece from base along d, width along wdir, a point at its end."""
    base, d = np.array(base, float), unit(*d)
    w = np.array(wdir, float); w = w - d * (w @ d); w /= np.linalg.norm(w)
    n = np.cross(w, d)
    # a kite in section: thin edges either side, a ridge down the middle on both faces, so each blade shows a lit
    # face and a shaded one and a clean outline even small
    pts = []
    for t, ww in ((0.0, 1.0), (mid, midw)):
        c = base + d * length * t
        for a in (-1, 1):
            pts.append(c + w * a * width / 2 * ww)
            pts.append(c + n * a * thick / 2 * (1 - 0.35 * t))
    pts.append(base + d * length)
    k.add(name, hull(pts), color, bone, fuse=False, surface="metal")
    FLAT.append(name)


def band(c, a, r, half, wall, seg=36):
    """A thin open ring about c, its axis a (rhyperior.py's), wound outward."""
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


# --- the masses: a long lean trunk, a chest under the breastplate, a rump --------------------------------------------
ELLS = []               # the fused ellipsoids, which the breastplate is laid over


def mass(name, c, r, bone, color=BLUE, **kw):
    k.add(name, ell(c, r, **kw), color, bone)
    ELLS.append((np.array(c, float), np.array(r, float)))


mass("body", (0, 0.04, 0.47), (0.165, 0.33, 0.145), "body")
mass("chest", (0, -0.17, 0.485), (0.16, 0.14, 0.16), "body")
mass("rump", (0, 0.24, 0.475), (0.16, 0.15, 0.15), "body")
mass("neckroot", (0, -0.2, 0.6), (0.15, 0.15, 0.14), "body")

# --- the legs: broad shoulders and thighs fused over long legs, steel cuffs low, three long steel claws -------------
LX = 0.145
LEGS = (("fore", -0.15, "leg{s}", 0.0), ("hind", 0.22, "hind{s}", 1.0))
FOOT = {}
for name, y, bone, hind in LEGS:
    sg = 1 if hind else -1                                   # the feet splayed forward in front, back behind
    top = (LX - 0.005, y + 0.01 * hind, 0.445)
    k.add(name + "top", ell(top, (0.102 + 0.014 * hind, 0.128 + 0.025 * hind, 0.145 + 0.01 * hind)), BLUE, bone,
          mirror=True)
    knee = (LX + 0.022, y + sg * 0.045, 0.27)
    ankle = (LX + 0.04, y + sg * 0.06, 0.16)
    k.add(name, tube([(LX, y, 0.41), knee, ankle], [0.085 + 0.008 * hind, 0.064, 0.052]), BLUE, bone, mirror=True)
    foot = (LX + 0.05, y + sg * 0.065, 0.06)
    k.add(name + "foot", tube([ankle, foot], [0.052, 0.047]), BLUE, bone, mirror=True)
    FOOT[name] = (np.array(ankle), np.array(foot), y)
    # the cuff round the shin, low, with a spur back from it
    k.add(name + "cuff", band(np.array(ankle), (0.15, 0.15 * sg, 1), 0.06, 0.024, 0.016), STEEL, bone,
          fuse=False, mirror=True, surface="metal")
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        spike(f"{name}spur{side}", np.array((LX + 0.045, y + sg * 0.06 + 0.04, 0.17)) * m, (0, 1, 1.1), (1, 0, 0),
              0.08, 0.055, 0.036, bone.format(s=side))
        # three long claws, from the foot down to the ground, splayed forward and out
        for j, dx in enumerate((-1, 0, 1)):
            b = np.array((LX + 0.05 + dx * 0.03, y + sg * 0.065 - 0.04, 0.105)) * m
            d = unit(s * dx * 0.22, -0.45, -1)
            spike(f"{name}claw{side}{j}", b, d, (s, 0.2 * dx, 0), 0.12, 0.046, 0.04, bone.format(s=side),
                  color=STEEL if j != 1 else shade(STEEL, 1.04), mid=0.5, midw=0.75)

# --- the tail: long, tapering, a little out to +X at its end -----------------------------------------------------------
TAIL = np.array([(0, 0.3, 0.47), (0, 0.45, 0.43), (0.015, 0.62, 0.37), (0.04, 0.78, 0.31), (0.07, 0.92, 0.285)])
TAIL_R = np.array([0.11, 0.08, 0.055, 0.032, 0.008])
k.add("tail", tube(TAIL, TAIL_R), BLUE, "tail")

# --- the neck: thick at its root, rising steeply to the head ---------------------------------------------------------
# an S carried forward: out of the chest leaning forward, the upper part drawn back a little under the head
NECK = np.array([(0, -0.15, 0.53), (0, -0.25, 0.64), (0, -0.315, 0.76), (0, -0.315, 0.86), (0, -0.295, 0.94)])
NECK_R = np.array([0.15, 0.13, 0.115, 0.105, 0.1])
k.add("necklo", tube(NECK[:3], NECK_R[:3]), BLUE, "body")
k.add("neckhi", tube(NECK[1:], NECK_R[1:]), BLUE, "head")
# steel spikes down the back of the neck, pointing back and up
for i, (t, L) in enumerate(((0.75, 0.12), (0.4, 0.14))):
    z = 0.56 + 0.38 * t
    cy = np.interp(z, NECK[:, 2], NECK[:, 1]); r = np.interp(z, NECK[:, 2], NECK_R)
    spike(f"neckspike{i}", (0, cy + r * 0.7, z), (0, 1, 0.75), (1, 0, 0), L, 0.07, 0.055, "head" if z > 0.7 else "body",
          color=shade(STEEL, 1.0 - 0.02 * i))

# --- the head: a boxy skull, a snout forward, a short jaw; the face steel ------------------------------------------
J, HS = np.array([0, -0.3, 0.93]), 1.52         # the head is made at a first size and grown about its joint
HEAD_FIRST = len(k.parts)
H = np.array([0, -0.335, 0.985])
HR = np.array([0.07, 0.085, 0.07])
k.add("cranium", ell(H, HR, deform=boxy(2.6)), STEEL, "head")
# a long angular snout, squared and tapering to its tip
def wedge(V):
    V = boxy(3.0)(V)
    t = np.clip(-V[:, 1], 0, 1)
    V[:, 0] *= 1 - 0.3 * t
    V[:, 2] *= 1 - 0.2 * t
    return V
k.add("snout", ell(H + (0, -0.105, -0.025), (0.05, 0.1, 0.045), deform=wedge), STEEL, "head")
k.add("jaw", ell(H + (0, -0.085, -0.064), (0.045, 0.085, 0.026), rot=(6, 0, 0), deform=boxy(2.4)), STEEL, "head")
# the top of the head blue from the crest's root back to the neck, a blue band up the brow's middle
k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.024, 0.003) * inside(-(P[:, 2] - (1.0 + 0.25 * (P[:, 1] - H[1] + 0.07))), 0.003)
        * inside(P[:, 1] - (H[1] + 0.06), 0.003) * inside(-(P[:, 1] - (H[1] - 0.13)), 0.003), BLUE, on=["cranium", "snout"])
k.paint(lambda P: inside(-(P[:, 1] - (H[1] + 0.05)), 0.004), BLUE, on=["cranium"])
# the open mouth across the snout's tip, two fangs hanging into it
k.add("mouth", ell(H + (0, -0.18, -0.054), (0.034, 0.045, 0.011), rot=(-10, 0, 0)), MOUTH, "head", carve=True)
for s in (1, -1):
    b = H + np.array((s * 0.022, -0.185, -0.043))
    k.add("fang" + ("R" if s > 0 else "L"), tube([b + (0, 0.004, 0.006), b + (0, -0.002, -0.008), b + (0, -0.004, -0.02)],
                                                  [0.009, 0.006, 0.0015], seg=10, per=4), FANG, "head", fuse=False,
          surface="metal")

# the crest: two long steel blades from the brow sweeping up and back, a little apart, so it is no pole from the front
for s, side in ((1, "R"), (-1, "L")):
    # the crest: one long broad blade (two close together) from the brow sweeping back nearly level, far past the
    # head; a shorter point above it
    if s > 0:
        spike("crest", H + (0, -0.055, 0.055), (0, 1, 0.17), (0, -0.17, 1), 0.33, 0.075, 0.026, "head",
              mid=0.65, midw=0.8)
        spike("crest2", H + (0, -0.01, 0.075), (0, 1, 0.55), (0, -0.55, 1), 0.13, 0.045, 0.024, "head",
              mid=0.5, midw=0.8)
    # a brow over each eye, sloping down toward the snout
    spike(f"brow{side}", H + (s * 0.035, -0.088, 0.03), (s * 0.55, 1, 0.22), (0, -0.22, 1), 0.085, 0.024, 0.014, "head",
          mid=0.5, midw=0.9)
    # a steel horn back from each side of the head, a point down and back from each jaw
    spike(f"horn{side}", H + (s * 0.06, 0.0, 0.025), (s * 0.45, 0.75, 0.45), (0, 0, 1), 0.11, 0.035, 0.02, "head")
    spike(f"jawpoint{side}", H + (s * 0.05, -0.01, -0.06), (s * 0.3, 0.45, -0.8), (0, 1, 0), 0.085, 0.032, 0.02, "head")
    # the cheek's plate flaring back under the eye
    spike(f"cheek{side}", H + (s * 0.055, -0.07, -0.025), (s * 0.35, 0.9, -0.05), (0, 0, 1), 0.1, 0.04, 0.018, "head",
          color=STEEL2)


for p in k.parts[HEAD_FIRST:]:
    p["V"] = J + (p["V"] - J) * HS
H0 = H
H = J + (H - J) * HS


# --- the breastplate: a steel shield over the chest and shoulders, a point up at each shoulder, a blue diamond -------
PC = np.array([0, -0.12, 0.48])


def push_out(Q, scale=1.07):
    Q = Q.copy()
    for _ in range(3):
        for c, r in ELLS:
            d = (Q - c) / r
            s = np.linalg.norm(d, axis=-1)
            m = s < scale
            Q[m] = c + (Q[m] - c) * (scale / s[m])[..., None]
    return Q


def exit_t(o, d):
    """How far a ray from o along d runs before it leaves the last of the chest's masses (none: 0)."""
    best = np.zeros(o.shape[:-1])
    for c, r in PLATE_ELLS:
        p = (o - c) / r; q = d / r
        A = (q * q).sum(-1); B = 2 * (p * q).sum(-1); C = (p * p).sum(-1) - 1
        disc = B * B - 4 * A * C
        t = np.where(disc > 0, (-B + np.sqrt(np.maximum(disc, 0))) / (2 * A), 0)
        best = np.maximum(best, t)
    return best


PLATE_ELLS = None


def plate_at(u, v):
    a = np.radians(-84 + 168 * (0.008 + 0.984 * u))           # 0 the front, + to the model's right
    aa = np.abs(np.degrees(a))
    top = 0.6 + 0.05 * (aa / 84) + 0.15 * np.exp(-((aa - 68) / 8) ** 2)
    bot = 0.355 + 0.08 * (aa / 84) ** 1.5 - 0.08 * np.exp(-(aa / 16) ** 2)
    z = bot + (top - bot) * (0.01 + 0.98 * v)
    d = np.stack([np.sin(a), -np.cos(a), np.zeros_like(a)], axis=-1)
    o = np.stack([np.zeros_like(z), np.full_like(z, PC[1]), z], axis=-1)
    t = exit_t(o, d)
    # the shell smoothed round the chest along its angle, so it is one clean curve, not the masses' bumps
    ts = t.copy()
    for _ in range(6):
        ts = 0.25 * np.roll(ts, 1, 0) + 0.5 * ts + 0.25 * np.roll(ts, -1, 0)
        ts[0], ts[-1] = t[0], t[-1]
    t = np.maximum(t, ts) + 0.016
    return o + d * t[..., None]


PLATE_ELLS = list(ELLS)
for s in (1, -1):
    PLATE_ELLS.append((np.array([s * (LX - 0.005), -0.155, 0.445]), np.array([0.092, 0.115, 0.135])))
def shell_point(a, z, lift=0.02):
    """A point of the plate's outer face: out from the chest's middle at an angle a (degrees, 0 the front) and height z,
    just outside every mass there."""
    d = np.array([math.sin(math.radians(a)), -math.cos(math.radians(a)), 0])
    o = np.array([0, PC[1], z])
    return o + d * (exit_t(o[None], d[None])[0] + lift), d


def panel(name, corners, thick=0.026, color=None):
    """A flat-faced steel panel: its corners (a, z) on the shell, given a thickness inward."""
    pts = []
    for a, z in corners:
        p, d = shell_point(a, z)
        pts += [p, p - d * thick]
    k.add(name, hull(pts), color or PLATE, "body", fuse=False, surface="metal")
    FLAT.append(name)


# the front: a pentagon pointed below, the gem's ground; then two facets a side turning back to the shoulders
panel("platemid", [(-21, 0.63), (21, 0.63), (21, 0.42), (0, 0.33), (-21, 0.42)])
for s, side in ((1, "R"), (-1, "L")):
    panel(f"plateA{side}", [(s * 20, 0.63), (s * 50, 0.66), (s * 50, 0.44), (s * 20, 0.42)], color=PLATE2)
    panel(f"plateB{side}", [(s * 49, 0.66), (s * 80, 0.66), (s * 80, 0.47), (s * 49, 0.44)])
    # the long sharp point thrust out at each shoulder
    p, d = shell_point(s * 74, 0.62, 0.0)
    spike(f"shoulder{side}", p - d * 0.03, (s * 1, 0.25, 0.42), (0, 0, 1), 0.24, 0.11, 0.03, "body", color=PLATE,
          mid=0.35, midw=0.7)
front, _ = shell_point(0, 0.5, 0.02)
gc = front + np.array([0, -0.012, 0])
k.add("gem", hull([gc + (0.06, 0, 0), gc - (0.06, 0, 0), gc + (0, 0, 0.08), gc - (0, 0, 0.08),
                   gc + (0, -0.04, 0), gc + (0, 0.02, 0)]), GEM, "body", fuse=False, surface="metal", glow=GEM)
FLAT.append("gem")

# --- the fan on the back: steel blades rising up and back, two rows tilted out in a V --------------------------------
# five long broad blades fanning from one root over the hips, each a close pair tilted out in a V so the fan reads
# from the front three-quarter and from behind
FAN_ROOT = np.array([0, 0.12, 0.6])
FAN = ((78, 0.26), (62, 0.36), (46, 0.42), (31, 0.44), (16, 0.42))
TILT = math.radians(38)
for i, (e, L) in enumerate(FAN):
    # one solid blade each, turned a little alternately about its length so neighbours catch the light apart
    e = math.radians(e)
    s = (-1, 1, 0, -1, 1)[i]
    d = unit(s * 0.3 * math.sin(e), math.cos(e), math.sin(e))       # splayed a little apart, seen from behind
    up = (s * math.sin(TILT) + (0.35 if s == 0 else 0), 0, math.cos(TILT))
    wdir = np.cross(np.cross(up, (0, 1.0, 0)), d)
    spike(f"fan{i}", FAN_ROOT + (0, 0.0, -0.02), d, wdir, L, 0.09, 0.042, "body",
          color=shade(STEEL, 1.0 - 0.04 * (i % 2)), mid=0.55, midw=0.85)

# --- the pale cyan lines: crisp strips of patches up the neck, along the flanks, down the legs and tail -------------
RECT = polygon([(-1, -1), (1, -1), (1, 1), (-1, 1)])


def stripe(pts, nrms, w=0.026):
    pts = np.array(pts, float)
    for i in range(len(pts) - 1):
        a, b = pts[i], pts[i + 1]
        n = unit(*(np.array(nrms[i], float) + np.array(nrms[i + 1], float)))
        d = b - a
        L = np.linalg.norm(d)
        k.decal((a + b) / 2, n, (w / 2, L / 2 + w * 0.35), CYAN, up=d, eps=0.005, outline=RECT, rings=3, segs=32,
                mirror=True)


# up the neck's side toward its front
zs = np.linspace(0.69, 0.93, 6)
phi = math.radians(62)
pts, ns = [], []
for z in zs:
    cy = np.interp(z, NECK[:, 2], NECK[:, 1]); r = np.interp(z, NECK[:, 2], NECK_R)
    dd = np.array([math.sin(phi), -math.cos(phi), 0])
    pts.append(np.array([0, cy, z]) + dd * r); ns.append(dd)
stripe(pts, ns)
# along the flank and down the outside of the hind leg
FLANK = [(-0.06, 0.53), (0.03, 0.545), (0.11, 0.52), (0.18, 0.46), (0.215, 0.37), (0.235, 0.28), (0.245, 0.2)]
stripe([(0.16, y, z) for y, z in FLANK], [(1, 0, 0.35)] * 3 + [(1, 0.1, 0)] * 4)
# down the front of the foreleg's outside
stripe([(0.17 + 0.25 * (0.4 - z), -0.2 - 0.3 * (0.4 - z), z) for z in (0.37, 0.31, 0.25, 0.195)],
       [unit(0.75, -0.65, 0)] * 4)
# along the tail's side
tpts = [TAIL[i] + (TAIL_R[i] * 0.9, 0, TAIL_R[i] * 0.3) for i in range(4)]
stripe(tpts, [(1, 0, 0.3)] * 4, w=0.024)

# --- the build: the hard pieces flat-faced ----------------------------------------------------------------------------
loose = [(p["name"], len(p["F"])) for p in k.parts if not p["fuse"] and not p["carve"]]
body = k.build(voxel=0.0078, smooth=3, band=0.005)
me = body.data
flags = np.ones(len(me.polygons), bool)
at = len(k.surfaces[k.name][1])
for nm, count in loose:
    if nm in FLAT:
        flags[at:at + count] = False
    at += count
me.polygons.foreach_set("use_smooth", flags.tolist())

# --- the eyes: red, a dark pupil, a dark rim, the top edge sloping down to the snout (a scowl) -----------------------
CR, CP, CK, CW, LID = srgb(EYE_RED), srgb(PUPIL), srgb(INK), srgb("#FFFFFF"), srgb(STEEL)
OUTLINE = [(-1, -0.05), (-0.5, -0.62), (0.3, -0.78), (0.85, -0.45), (1, 0.2), (0.75, 0.62), (0.1, 0.42), (-0.6, 0.15)]
EYE_OUT = polygon(OUTLINE)


def eye(u, v):
    c = np.tile(CR, (len(u), 1))
    c[((u + 0.05) / 0.22) ** 2 + ((v + 0.05) / 0.42) ** 2 < 1] = CP
    c[((u - 0.2) / 0.13) ** 2 + ((v - 0.18) / 0.13) ** 2 < 1] = CW
    frac = np.array([math.hypot(a, b) / EYE_OUT(math.atan2(b, a)) for a, b in zip(u, v)])
    c[frac > 0.82] = CK
    return c


EYE_DIR = unit(0.72, -0.62, 0.18)
E = H + (np.array([0.0, -0.035, 0.008]) + EYE_DIR * 0.072) * HS


def eyes(state):
    k.decal(E, EYE_DIR, (0.032 * HS, 0.026 * HS), eye_look(state, eye, LID, CK, line=0.18), eps=0.005, rings=20, segs=56,
            outline=EYE_OUT, mirror=True)


k.eye_states(eyes)
k.export(out)
