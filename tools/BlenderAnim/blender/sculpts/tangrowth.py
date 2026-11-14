# Tangrowth sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tangrowth.py -- <the export's Tangrowth.glb> <out.glb>
#
# From the references: Tangela grown. A great heap of darker blue vines, taller than wide and broadest low down, the
# vines lying over it like a shaggy coat and hanging from its lower edge all round as a ragged fringe of rounded ends;
# in the upper front the same dark hollow with two small white eyes and dark pupils, framed by curled tendrils; two
# long arms of three vines wound together, each ending in three stubby fingers with red tips; short dark legs under
# the fringe with red soles at the front.
#
# Grown from tangela.py: the same ball with its dimpled hollow (`ball`, `hollow_paint`), the same loose ropes of vine
# (`vine`, `arcs`, `curl`) and the same eyes (`eye_open`, `lay_eyes`); what is new is the coat's lie (arcs turned to
# run downward), the fringe (`fringe`), the curled tendrils (`tendril`) and the arms (`arm`).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="leaf")

# The drawing's blue, greyer and darker than Tangela's (the 3D model's is a brighter cyan)
VINE, VINE2, CORE, HOLLOW, RED, WHITE, PUPIL, LINE, LEG = ("#4678B2", "#5083BC", "#2C5486", "#2C2F3A", "#E0405A",
                                                           "#F7F7F4", "#101014", "#8A96AE", "#2C2D35")

C = np.array([0.0, 0.0, 0.5])             # the heap's middle, on the body bone's line
R = np.array([0.295, 0.255, 0.32])         # its radii: taller than wide
THICK = 0.03                              # a vine's radius: fat, some 10% of the heap's width across
HOLLOW_AZ, HOLLOW_EL, HOLLOW_HW, HOLLOW_HH = 0.0, math.radians(25), math.radians(19), math.radians(9)
EYE_X, EYE_Z, EYE_HALF = 0.038, 0.64, (0.024, 0.027)


def sph(az, el, lift=0.0, c=C, r=R):
    """A point over the heap at an angle round from the front (radians, + toward +X) and up, lifted by a share."""
    d = np.array([math.sin(az) * math.cos(el), -math.cos(az) * math.cos(el), math.sin(el)])
    return c + d * r * (1 + lift)


def ball(name, c, r, color, bone, hollow, depth=0.13, pear=0.0):
    """The heap under the vines, a shallow dimple pressed into its front where the hollow is; pear widens it low."""
    az0, el0, hw, hh = hollow
    def dimple(V):
        V = V.copy()
        az = np.arctan2(V[:, 0], -V[:, 1])
        el = np.arcsin(np.clip(V[:, 2], -1, 1))
        q = ((az - az0) / (hw * 1.05)) ** 2 + ((el - el0) / (hh * 1.1)) ** 2
        V = V * (1 - depth * np.exp(-q ** 2))[:, None]
        V[:, :2] *= (1 - pear * V[:, 2])[:, None]
        return V
    k.add(name, ell(c, r, deform=dimple, sub=5), color, bone)


def hollow_paint(c, r, hollow, color, on, reach=1.2, drop=0.05):
    """The hollow's dark, painted over the dimple with a ragged edge that the vines round it hide."""
    az0, el0, hw, hh = hollow
    def mask(P):
        d = (P - c) / r
        az = np.arctan2(d[:, 0], -d[:, 1])
        el = np.arcsin(np.clip(d[:, 2] / np.maximum(np.linalg.norm(d, axis=1), 1e-6), -1, 1))
        a = np.arctan2(el - el0, az - az0)
        rr = reach + 0.04 * np.sin(5 * a) + 0.03 * np.sin(3 * a + 1)
        q = np.sqrt(((az - az0) / hw) ** 2 + ((el - el0 + drop) / (hh * 1.15)) ** 2) - rr
        return inside(q, 0.05) * (d[:, 1] < 0)
    k.paint(mask, color, on=on)


def vine(name, pts, radius, color, bone, seg=8, per=4, radii=None):
    """One rope of vine, loose so that it keeps its own round edge against the heap and its fellows."""
    pts = np.array(pts, float)
    rad = radii if radii is not None else [radius] * len(pts)
    k.add(name, tube(pts, rad, seg=seg, per=per), color, bone, fuse=False)


def in_hollow(d, hollow, margin=1.18):
    """Whether a direction from the heap's middle looks into the hollow (kept clear of the random vines)."""
    az0, el0, hw, hh = hollow
    az = math.atan2(d[0], -d[1]); el = math.asin(max(-1, min(1, d[2])))
    return ((az - az0) / (hw * margin)) ** 2 + ((el - el0) / (hh * margin)) ** 2 < 1


def arcs(prefix, count, seed, c, r, radius, colours, bone_of, hollow, span=(0.45, 0.8), lift=(0.06, 0.2),
         lowest=-0.72, hoops=0.18, turn=(0.0, math.pi), highest=1.0):
    """
    Vines wound over the heap: arcs round its surface, spread by the golden angle, each rising off it toward its
    middle, a share of them in hoops; turn limits their run (pi/2: up and down, as a coat lies). None crosses the hollow.
    """
    rs = np.random.RandomState(seed)
    made = 0
    for i in range(count):
        y = 1 - 2 * (i + 0.5) / count
        ring = math.sqrt(max(0.0, 1 - y * y))
        a = i * 2.39996 + rs.rand() * 0.6
        n = np.array([math.sin(a) * ring, -math.cos(a) * ring, y])
        if y < lowest or y > highest:
            continue
        t = np.cross(n, (0, 0, 1.0) if abs(n[2]) < 0.95 else (1.0, 0, 0)); t /= np.linalg.norm(t)
        tw = turn[0] + (turn[1] - turn[0]) * rs.rand()
        u = t * math.cos(tw) + np.cross(n, t) * math.sin(tw)
        sp = span[0] + (span[1] - span[0]) * rs.rand()
        lf = lift[0] + (lift[1] - lift[0]) * rs.rand()
        if rs.rand() < hoops:
            lf *= 2.0
        pts, bad = [], False
        # the hollow is small here: checked finely, or a vine strides over it between its points
        for s in np.linspace(-1, 1, 25):
            d = n * math.cos(s * sp) + u * math.sin(s * sp); d /= np.linalg.norm(d)
            if in_hollow(d, hollow, 1.3):
                bad = True
        for s in np.linspace(-1, 1, 7):
            d = n * math.cos(s * sp) + u * math.sin(s * sp); d /= np.linalg.norm(d)
            if in_hollow(d, hollow) or d[2] < lowest - 0.05:
                bad = True; break
            pts.append(c + d * r * (1 + radius / r.min() * 0.6 + lf * math.cos(s * math.pi / 2) ** 1.5))
        if bad:
            continue
        mid = pts[3]
        vine(f"{prefix}{made}", pts, radius * (0.92 + 0.16 * rs.rand()), colours[made % len(colours)], bone_of(mid))
        made += 1
    return made


def curl(name, start, out_dir, length, turns, radius, color, bone, up=(0, 0, 1.0), shrink=0.55, n=10):
    """A vine's free end: leaves the heap at start, runs out along out_dir and rolls up in a curl at its tip."""
    out_dir = unit(*out_dir); up = np.array(up, float)
    side = np.cross(out_dir, up); side /= np.linalg.norm(side)
    roll = np.cross(side, out_dir)
    pts = []
    for j in range(n):
        t = j / (n - 1)
        ang = turns * 2 * math.pi * t ** 1.6
        rad = length * (1 - shrink * t) * 0.35
        p = (np.array(start, float) + out_dir * length * 0.6 * t
             + (out_dir * math.sin(ang) + roll * (1 - math.cos(ang))) * rad * min(1, t * 3))
        pts.append(p)
    vine(name, pts, radius, color, bone)


def tendril(name, az, el, size, turns, sense, radius, color, bone, start_lift=0.05, end_lift=0.16):
    """A curl lying on the heap round a point (the tendrils framing the face): a spiral inward, rising as it rolls."""
    pts = []
    for j in range(12):
        t = j / 11
        a = sense * turns * 2 * math.pi * t
        rr = size * (1 - 0.72 * t)
        pts.append(sph(az + rr * math.cos(a) / max(math.cos(el), 0.3), el + rr * math.sin(a),
                       start_lift + (end_lift - start_lift) * t))
    vine(name, pts, radius, color, bone)


def fringe(prefix, count, seed, el, bottom, radius, colours, skip_front=0.0):
    """The ragged skirt: vine ends hanging from the heap's lower edge all round, out and down, their tips rounded in."""
    rs = np.random.RandomState(seed)
    for i in range(count):
        az = 2 * math.pi * (i + 0.3 * rs.rand()) / count
        top = sph(az, el + 0.12 * (rs.rand() - 0.5), 0.05)
        outward = np.array([math.sin(az), -math.cos(az), 0.0])
        low = bottom + 0.05 * rs.rand()
        hook = 0.02 * (rs.rand() - 0.5)
        pts = [top,
               top + outward * 0.03 + (0, 0, -(top[2] - low) * 0.45),
               np.array([top[0], top[1], low + 0.025]) + outward * 0.035,
               np.array([top[0], top[1], low]) + outward * (0.02 + hook)]
        vine(f"{prefix}{i}", pts, radius * (0.95 + 0.15 * rs.rand()), colours[i % len(colours)], "body",
             radii=[radius, radius * 1.05, radius * 1.05, radius * 1.1])


def arm(side, path, r0, r1, gap0, gap1, tip, color, tipcolor, row):
    """
    A thick arm: three fat vines side by side along a path (not wound), in a row across it that turns with it, each
    thickening toward the hand (r0 to r1) and spreading a little (gap0 to gap1), and running on past the path's end
    into a stubby finger whose last stretch, tip long, is red. A core fills the grooves between them.
    """
    path = np.array(path, float)
    bone = f"arm{side}"
    n = len(path)
    end_dir = unit(*(path[-1] - path[-2]))
    rows = []
    for i, p in enumerate(path):
        nxt = path[min(n - 1, i + 1)] - path[max(0, i - 1)]
        along = nxt / np.linalg.norm(nxt)
        r = np.array(row, float) - along * np.dot(row, along); r /= np.linalg.norm(r)
        rows.append(r)
    for j in (-1, 0, 1):
        pts, radii = [], []
        for i, p in enumerate(path):
            t = i / (n - 1)
            pts.append(p + rows[i] * j * (gap0 + (gap1 - gap0) * t))
            radii.append(r0 + (r1 - r0) * t)
        splay = rows[-1] * j * gap1 * 0.35
        f0 = pts[-1] + end_dir * tip * 0.3 + splay * 0.4
        f1 = f0 + end_dir * tip + splay * 0.6
        rf = r1 * (1.0 - 0.06 * abs(j))
        k.add(f"arm{side}{j + 1}", tube(pts + [f0], radii + [rf], seg=12, per=5), color, bone, fuse=False)
        k.add(f"finger{side}{j + 1}", tube([f0 - end_dir * 0.006, (f0 + f1) / 2, f1], [rf * 1.03] * 3, seg=12, per=4),
              tipcolor, bone, fuse=False)
    k.add(f"armcore{side}", tube(path, [r0 * 1.15 + (r1 - r0) * 1.15 * i / (n - 1) for i in range(n)], seg=12, per=5),
          CORE, bone, fuse=False)


def hanging(prefix, count, seed, top_el, bottom, radius, colours, face_az=0.0, face_el=None, lift=0.07):
    """
    The coat's lower half: fat vines hanging side by side all round from under the curls to the ground, bulging a
    little out over the heap, their ends rounded at uneven lengths: the fringe. None crosses another.
    """
    rs = np.random.RandomState(seed)
    for i in range(count):
        az = 2 * math.pi * (i + 0.5) / count + 0.04 * (rs.rand() - 0.5)
        front = abs(math.atan2(math.sin(az), math.cos(az))) < face_az
        el0 = face_el if (front and face_el is not None) else top_el + math.radians(6) * (rs.rand() - 0.5)
        out_ = np.array([math.sin(az), -math.cos(az), 0.0])
        p0 = sph(az, el0, lift * 0.5)
        p1 = sph(az, el0 - math.radians(18), lift)
        p2 = sph(az, math.radians(-26), lift * 1.1)
        low = bottom + 0.06 * rs.rand()
        p3 = np.array([p2[0], p2[1], (p2[2] + low) / 2]) + out_ * 0.012
        p4 = np.array([p2[0], p2[1], low]) + out_ * 0.004
        pts = [p0, p1, p2, p3, p4] if p1[2] > p2[2] + 0.02 else [p0, p2, p3, p4]
        vine(f"{prefix}{i}", pts, radius * (0.95 + 0.1 * rs.rand()), colours[i % len(colours)], "body", seg=10)


def eye_open(u, v, pupil=(0.0, 0.0), size=0.36):
    """A plain white eye; its pupil is a patch of its own."""
    return np.tile(srgb(WHITE), (len(u), 1))


def lay_eyes(state, xs, z, half, pupil_at, pupil_half, lid, ink, normal_of, surface_y=-0.24):
    """The two eyes on the hollow's floor, cast from the front: white with a solid dark pupil (Open and Fierce)."""
    for s in (1, -1):
        c = np.array([xs * s, surface_y, z])
        n = normal_of(s)
        look = eye_look(state, eye_open, lid, ink, line=0.16)
        col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
        k.decal(c, n, half, col, eps=0.005, rings=20, segs=56)
        if state in ("Open", "Fierce"):
            pc = c + np.array([pupil_at[0] * s * half[0], 0, pupil_at[1] * half[1] - (0.005 if state == "Fierce" else 0)])
            k.decal(pc, n, (pupil_half, pupil_half * 1.08), PUPIL, eps=0.008, rings=8, segs=32)


# ---- the heap and its hollow ----------------------------------------------------------------------------------------
HOLLOW_SPEC = (HOLLOW_AZ, HOLLOW_EL, HOLLOW_HW, HOLLOW_HH)
ball("core", C, R, CORE, "body", HOLLOW_SPEC, pear=0.1)
hollow_paint(C, R, HOLLOW_SPEC, HOLLOW, on=["core"], reach=1.35, drop=0.03)

# ---- the coat: few fat vines, wound over the upper heap and rolled into big puffy curls -------------------------------
def bone_of(p):
    return "head" if p[2] > 0.72 else "body"
arcs("vine", 62, 465, C, R, THICK, (VINE, VINE2), bone_of, HOLLOW_SPEC, span=(0.35, 0.6), lift=(0.04, 0.1),
     lowest=0.05, hoops=0.15)
D = math.radians
for i, (a, e, sz, sense) in enumerate(((-60, 48, 12, 1), (60, 50, 12, -1), (180, 52, 12, 1), (120, 40, 12, -1),
                                       (-120, 42, 12, 1), (150, 15, 11, 1), (-150, 18, 11, -1), (90, 22, 11, 1),
                                       (-90, 20, 11, -1), (0, 72, 11, 1))):
    tendril(f"puff{i}", D(a), D(e), D(sz), 1.0, sense, THICK, (VINE, VINE2)[i % 2], "head" if e > 40 else "body",
            start_lift=0.06, end_lift=0.14)

# ---- the face's frame: a vine over the hollow, a vine under it, and big curls round it ---------------------------------
vine("brow", [sph(D(a), D(e), l) for a, e, l in
              ((-36, 24, 0.05), (-26, 38, 0.1), (-10, 42, 0.12), (8, 41, 0.12), (24, 38, 0.1), (36, 26, 0.05))],
     THICK, VINE2, "head")
vine("chin", [sph(D(a), D(e), l) for a, e, l in
              ((-34, 26, 0.05), (-26, 11, 0.1), (-9, 7, 0.12), (9, 7, 0.12), (26, 11, 0.1), (34, 26, 0.05))],
     THICK, VINE, "body")
for s, side in ((1, "R"), (-1, "L")):
    tendril(f"curlhi{side}", D(33 * s), D(42), D(13), 1.1, s, THICK, VINE2, "head")
    tendril(f"curllo{side}", D(37 * s), D(8), D(13), 1.1, -s, THICK, VINE, "body")
    tendril(f"curlside{side}", D(54 * s), D(28), D(12), 1.0, s, THICK, VINE2, "body")
tendril("curltop", D(-4), D(56), D(13), 1.1, 1, THICK, VINE, "head")

# ---- the lower coat: fat vines hanging side by side all round into a fringe of rounded ends ---------------------------
hanging("hang", 30, 4650, D(-2), 0.12, THICK, (VINE, VINE2), face_az=D(30), face_el=D(-6))
# a band of puffy curls round the middle, where the hanging vines begin (the face's own curls in the front)
for i, a in enumerate(range(-150, 181, 30)):
    if abs(a) < 50:
        continue
    tendril(f"band{i}", D(a + 8 * (i % 2)), D(6 + 14 * (i % 2)), D(12), 1.0, (1, -1)[i % 2], THICK,
            (VINE2, VINE)[i % 2], "body", start_lift=0.06, end_lift=0.14)

# ---- the arms: thick, three fat vines side by side, down and a little forward, clear of the coat; large red tips ----
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    path = [np.array(p) * m for p in ((0.25, -0.02, 0.58), (0.36, -0.06, 0.55), (0.42, -0.12, 0.45),
                                      (0.44, -0.17, 0.33), (0.44, -0.19, 0.24))]
    arm(side, path, 0.032, 0.042, 0.045, 0.06, 0.1, VINE, RED, row=(0.4 * s, 0.92, 0.0))

# ---- the legs: short and dark under the fringe, red soles at the front ------------------------------------------------
for s, side in ((1, "R"), (-1, "L")):
    k.add(f"leg{side}", tube([(0.11 * s, 0.0, 0.2), (0.115 * s, -0.005, 0.1), (0.12 * s, -0.01, 0.05)],
                             [0.06, 0.058, 0.055]), LEG, f"leg{side}", fuse=False, surface="fur")
    k.add(f"foot{side}", ell((0.12 * s, -0.03, 0.04), (0.07, 0.085, 0.04), sub=4), LEG, f"leg{side}", fuse=False,
          surface="fur")
    k.add(f"sole{side}", ell((0.12 * s, -0.085, 0.032), (0.062, 0.04, 0.032), sub=4), RED, f"leg{side}", fuse=False,
          surface="fur")

k.build(voxel=0.0085, smooth=3)

# ---- the eyes: round whites in the hollow, a solid dark pupil a little up and in -----------------------------------------
def normal_of(s):
    return unit(0.1 * s, -1.0, 0.25)
def eyes(state):
    lay_eyes(state, EYE_X, EYE_Z, EYE_HALF, (-0.1, 0.12), 0.0095, srgb(HOLLOW), srgb(LINE), normal_of)

k.eye_states(eyes)
k.export(out)
