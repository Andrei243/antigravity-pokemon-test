# Yanmega sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/yanmega.py -- <the export's Yanmega.glb> <out.glb>
#
# Grown from yanma.py: the same wings (a pale plate in a grey rim with coloured plates inside it, raised in a V and
# swept apart), the same eyes as patches laid by `eye_frame` on the head ball, the same tail as a tube through `TAIL`
# with its rings painted by `segments`. From the references: a big dark green dragonfly. A round green head with great
# pink-red visors of eyes over both sides of its front, a dark seam round each and across its middle; a green snout
# between them low at the front, two white fangs curving down under it. A broad rounded green thorax with three black
# spikes along its top leaning back, pink spots low on its sides; six black legs, each with a small spur. Four long
# clear wings, the tip of each a pink oval. A long thick green tail rising behind, ringed into segments, a pink spot on
# each segment's top, two small pale wings near its end, which splits into a black hooked spike and a green finger.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="shell")

GREEN, GREENDARK, PINK, PINKDARK, BLACK = "#56853A", "#2E4A22", "#EC5470", "#8E1E34", "#2C2C32"
WING, WINGEDGE, SMALLWING, WHITE, INK = "#E8EEF6", "#AEBACA", "#D6DBE2", "#F6F4F0", "#16141E"

# --- flat plates (beautifly.py's, as yanma.py) -------------------------------------------------------------------------
def area(p):
    return 0.5 * np.sum(p[:, 0] * np.roll(p[:, 1], -1) - np.roll(p[:, 0], -1) * p[:, 1])


def earclip(p):
    idx = list(range(len(p)))
    tris = []
    def cross(o, a, b):
        return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])
    guard = 0
    while len(idx) > 3 and guard < 100000:
        guard += 1
        m = len(idx)
        cut = False
        for i in range(m):
            a, b, c = idx[i - 1], idx[i], idx[(i + 1) % m]
            if cross(p[a], p[b], p[c]) <= 1e-12:
                continue
            ok = True
            for j in idx:
                if j in (a, b, c):
                    continue
                q = p[j]
                if cross(p[a], p[b], q) >= 0 and cross(p[b], p[c], q) >= 0 and cross(p[c], p[a], q) >= 0:
                    ok = False
                    break
            if ok:
                tris.append((a, b, c)); idx.pop(i); cut = True
                break
        if not cut:
            tris.append((idx[-1], idx[0], idx[1])); idx.pop(0)
    tris.append(tuple(idx))
    return tris


def flat(outline, origin, ex, ez, thick):
    """A flat plate of a polygon (a, b in the plane of ex, ez about origin) with sharp edges that shade flat."""
    ex, ez = unit(*ex), unit(*ez)
    nrm = np.cross(ex, ez); nrm /= np.linalg.norm(nrm)
    p = np.array(outline, float)
    if area(p) < 0:
        p = p[::-1]
    W = np.array(origin, float) + p[:, :1] * ex + p[:, 1:] * ez
    m = len(p)
    front, back = W + nrm * thick / 2, W - nrm * thick / 2
    F = []
    for a, b, c in earclip(p):
        F.append([a, b, c]); F.append([m + c, m + b, m + a])
    rim = []
    for i in range(m):
        j = (i + 1) % m
        rim.extend([front[i], back[i], back[j], front[j]])
        F.append([2 * m + 4 * i + q for q in range(4)])
    return np.vstack([front, back, np.array(rim)]), F


# --- the wings (yanma.py's) ----------------------------------------------------------------------------------------------
def wing_width(t, W):
    return W * np.sqrt(np.clip(1 - ((t - 0.5) / 0.5) ** 2, 0, 1)) * (0.55 + 0.45 * t)


def wing_outline(L, W, t0=0.0, t1=1.0, inset=1.0, n=28):
    t = np.linspace(t0, t1, n)
    a = 0.004 + t * L
    w = np.maximum(wing_width(t, W) * inset, 0.0008)
    return np.vstack([np.stack([a, w], 1), np.stack([a, -w], 1)[::-1]])


def wing_axes(s, sweep, lift, twist):
    sw, li = math.radians(sweep), math.radians(lift)
    ex = np.array([s * math.cos(li) * math.cos(sw), -math.cos(li) * math.sin(sw), math.sin(li)])
    ez = np.array([0.0, 1.0, math.tan(math.radians(twist))])
    ez = ez - ex * np.dot(ez, ex)
    return ex, ez / np.linalg.norm(ez)


def wing(name, root, bone, L, W, sweep, lift, twist, bands, band_colour, rim=WINGEDGE, fill=WING, thick=0.004,
         anchor=(0, 0, 0)):
    """A wing on both sides of an anchor (the body's midline, or a point of the tail): only the root's offset is
    mirrored."""
    for s, side in ((1, "R"), (-1, "L")):
        o = np.array(anchor, float) + np.array(root, float) * (s, 1, 1)
        ex, ez = wing_axes(s, sweep, lift, twist)
        b = bone.format(s=side)
        k.add(f"{name}{side}", flat(wing_outline(L, W), o, ex, ez, thick), rim, b, fuse=False)
        k.add(f"{name}in{side}", flat(wing_outline(L, W, 0.04, 0.985, 0.82), o, ex, ez, thick * 1.5), fill, b,
              fuse=False)
        for i, (t0, t1) in enumerate(bands):
            k.add(f"{name}band{i}{side}", flat(wing_outline(L, W, t0, t1, 0.9, n=16), o, ex, ez, thick * 2.2),
                  band_colour, b, fuse=False)


# Larger and broader than Yanma's, the whole tip a pink oval
L_WING, W_WING = 0.235, 0.05
wing("wingF", (0.05, -0.06, 0.35), "wing{s}", L_WING, W_WING, 24, 34, 38, [(0.64, 0.985)], PINK)
wing("wingH", (0.05, -0.004, 0.35), "wing2{s}", L_WING * 0.95, W_WING * 0.95, -26, 26, -38, [(0.64, 0.985)], PINK)

# --- the body: a broad rounded green thorax --------------------------------------------------------------------------
k.add("thorax", ell((0, -0.03, 0.3), (0.072, 0.1, 0.066)), GREEN, "body")
k.add("waist", ell((0, 0.06, 0.305), (0.04, 0.045, 0.04)), GREEN, "tail")
k.add("wingroot", ell((0.042, -0.03, 0.34), (0.018, 0.06, 0.014)), GREEN, "body", mirror=True)
# pink spots low on the thorax's sides
SPOTS = [np.array((0.064, -0.055, 0.285)), np.array((0.062, 0.0, 0.28))]
def side_spots(P):
    m = np.zeros(len(P))
    for c in SPOTS:
        for s in (1, -1):
            m = np.maximum(m, inside(np.linalg.norm((P - c * (s, 1, 1)) / (1, 1.25, 1), axis=1) - 0.017, 0.0015))
    return m
k.paint(side_spots, PINK, on=["thorax"])

# three black spikes along its top, leaning back
for i, (y, h) in enumerate(((-0.075, 0.072), (-0.03, 0.084), (0.015, 0.072))):
    base = np.array((0, y, 0.35))
    tip = base + np.array((0, 0.045, h))
    k.add(f"spike{i}", blade([base, base * 0.5 + tip * 0.5 + (0, -0.006, 0), tip], [0.02, 0.011, 0.0015],
                             (1, 0, 0), 0.75), BLACK, "body", fuse=False)

# --- the tail: long and thick, rising behind and out to +X, ringed into segments, a pink spot atop each ------------
TAIL = np.array([(0, 0.06, 0.305), (0.004, 0.15, 0.322), (0.018, 0.23, 0.36), (0.042, 0.29, 0.412),
                 (0.066, 0.322, 0.47)])
TAIL_R = [0.04, 0.034, 0.031, 0.028, 0.025]
k.add("tail", tube(TAIL, TAIL_R), GREEN, "tail")
TL = float(np.linalg.norm(np.diff(TAIL, axis=0), axis=1).sum())


def along(P, pts):
    """For points P: how far along a polyline (in its own length) their nearest point lies, and their distance."""
    best_d = np.full(len(P), 9.0); best_s = np.zeros(len(P)); run = 0.0
    for a, b in zip(pts[:-1], pts[1:]):
        e = b - a; ln = np.linalg.norm(e)
        t = np.clip(((P - a) @ e) / (ln * ln), 0, 1)
        d = np.linalg.norm(P - (a + t[:, None] * e), axis=1)
        better = d < best_d
        best_d[better] = d[better]; best_s[better] = run + t[better] * ln
        run += ln
    return best_s, best_d


def point_at(pts, s):
    """The polyline's point and direction s along it."""
    run = 0.0
    for a, b in zip(pts[:-1], pts[1:]):
        ln = np.linalg.norm(b - a)
        if s <= run + ln:
            return a + (b - a) * (s - run) / ln, (b - a) / ln
        run += ln
    return pts[-1], (pts[-1] - pts[-2]) / np.linalg.norm(pts[-1] - pts[-2])


def segments(pts, first, step, count, width, reach, colour):
    def m(P):
        s, d = along(P, pts)
        ring = np.zeros(len(P))
        for i in range(count):
            ring = np.maximum(ring, inside(np.abs(s - (first + i * step)) - width, 0.0018))
        return ring * inside(d - reach, 0.002)
    k.paint(m, colour, on=["tail"])


RING0, RINGSTEP, RINGS = 0.07, 0.06, 5
segments(TAIL, RING0, RINGSTEP, RINGS, 0.003, 0.06, GREENDARK)


# a pink oval atop each segment, between the rings
def top_spots(P):
    m = np.zeros(len(P))
    for i in range(RINGS):
        s = RING0 + (i - 0.5) * RINGSTEP
        c, d = point_at(TAIL, s)
        up = np.array((0, 0, 1.0)) - d * d[2]; up /= np.linalg.norm(up)
        r = np.interp(s / TL,np.linspace(0, 1, len(TAIL_R)), TAIL_R)
        q = P - (c + up * r)
        a = q @ d
        m = np.maximum(m, inside(np.sqrt((a / 0.017) ** 2 + ((q - np.outer(a, d)) ** 2).sum(1) / 0.021 ** 2) - 1, 0.08))
    return m
k.paint(top_spots, PINK, on=["tail"])

# its end: a green finger rising on and a black hooked spike beside it
END, DIR = TAIL[-1], unit(*(TAIL[-1] - TAIL[-2]))
SIDE = np.cross(DIR, (0, 0, 1.0)); SIDE /= np.linalg.norm(SIDE)
k.add("tailtip", tube([END, END + DIR * 0.03 + (0, 0, 0.012), END + DIR * 0.055 + (0, 0, 0.03)],
                      [0.024, 0.017, 0.006]), GREEN, "tail")
hb = END - SIDE * 0.012 - DIR * 0.004
k.add("hook", blade([hb, hb + DIR * 0.03 + (0, 0, 0.035) - SIDE * 0.006, hb + DIR * 0.012 + (0, 0, 0.07) - SIDE * 0.01],
                    [0.016, 0.009, 0.0012], DIR, 0.6), BLACK, "tail", fuse=False)
# two small pale wings out to the tail's sides near its end
wing("tailwing", (0.02, 0.0, 0.0), "tail", 0.085, 0.022, -10, 18, 30, [], SMALLWING, rim=WINGEDGE, fill=SMALLWING,
     anchor=TAIL[-2] + (TAIL[-1] - TAIL[-2]) * 0.3)

# --- the legs: six black legs bent at the knee, a spur at each knee, a hooked foot -------------------------------------
LEGS = [("legF", "leg{s}", [(0.03, -0.09, 0.248), (0.09, -0.12, 0.214), (0.092, -0.145, 0.15)]),
        ("legM", "hind{s}", [(0.034, -0.03, 0.244), (0.098, -0.03, 0.21), (0.1, -0.02, 0.145)]),
        ("legB", "hind{s}", [(0.028, 0.02, 0.25), (0.088, 0.05, 0.214), (0.088, 0.075, 0.15)])]


def tuck(pts, k_=0.7, fold=0.022):
    """A leg some 30% shorter and more folded: each length scaled, the foot drawn in toward the body."""
    h, kn, f = (np.array(p, float) for p in pts)
    kn2 = h + (kn - h) * k_
    f2 = kn2 + (f - kn) * k_ + np.array((-fold, 0, 0.004))
    return [tuple(h), tuple(kn2), tuple(f2)]


LEGS = [(n, b, tuck(p)) for n, b, p in LEGS]
for name, bone, pts in LEGS:
    k.add(name, tube(pts, [0.012, 0.01, 0.0075]), BLACK, bone, fuse=False, mirror=True)
    knee = np.array(pts[1])
    k.add(name + "spur", blade([knee, knee + (0.022, -0.004, 0.006), knee + (0.036, -0.006, 0.004)],
                                [0.006, 0.004, 0.001], (0, 0, 1), 0.6), BLACK, bone, fuse=False, mirror=True)
    f = np.array(pts[-1])
    k.add(name + "foot", blade([f + (0, 0.006, 0.006), f + (0.006, -0.012, -0.006), f + (0.012, -0.024, -0.004)],
                                [0.009, 0.006, 0.0015], (0, 0, 1), 0.6), BLACK, bone, fuse=False, mirror=True)

# --- the head: a green ball with the visors' bulges, a green snout, white fangs ------------------------------------------
H = np.array([0.0, -0.17, 0.29])
HR = np.array([0.084, 0.07, 0.068])
k.add("head", ell(H, HR), GREEN, "head")
VISOR_D = (0.7, -0.66, 0.02)


def eye_frame(side, H, HR, d=VISOR_D):
    """Where an eye sits on a head ball, and the way it faces (yanma.py's)."""
    de = unit(side * d[0], d[1], d[2])
    p = H + de / math.sqrt(((de / HR) ** 2).sum())
    n = (p - H) / HR ** 2
    return p, n / np.linalg.norm(n)


# the visors bulge out of the ball, flatter than it, so their great patches lie without wrapping
for side, nm in ((1, "R"), (-1, "L")):
    c, n = eye_frame(side, H, HR)
    turn = math.degrees(math.atan2(n[0], -n[1]))
    k.add(f"visor{nm}", ell(c - n * 0.012, (0.05, 0.024, 0.052), rot=(0, 0, turn)), GREEN, "head")
k.add("snout", ell((0, -0.228, 0.262), (0.034, 0.03, 0.034)), GREEN, "head")
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"fang{side}", tube([np.array((0.024, -0.238, 0.244)) * m, np.array((0.028, -0.252, 0.226)) * m,
                               np.array((0.024, -0.256, 0.206)) * m], [0.0075, 0.0055, 0.001]),
          WHITE, "head", fuse=False)

k.build(voxel=0.0042, smooth=4, band=0.006)


# --- the eyes: great pink visors with a dark seam round and across them, a glint up toward the nose -------------------
CP, CD, CK, CW, CG = srgb(PINK), srgb(PINKDARK), srgb(INK), np.array([1.0, 1.0, 1.0]), srgb(GREEN)
def eye(u, v):
    c = np.tile(CP, (len(u), 1))
    c[np.abs(v + 0.12) < 0.11] = CD                              # the seam across the middle
    c[((u + 0.3) / 0.2) ** 2 + ((v - 0.4) / 0.16) ** 2 < 1] = CW               # the glint
    c[u * u + v * v > 0.7] = CD                                               # the seam round it
    return c


EYE_HALF = (0.046, 0.05)
def eyes(state):
    look = eye_look(state, eye, CP, CD, line=0.12)          # a shut visor stays pink, the line dark
    for side in (1, -1):
        c, n = eye_frame(side, H, HR)
        c = c + n * 0.012
        col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
        k.decal(c, n, EYE_HALF, col, eps=0.003, rings=20, segs=56)


k.eye_states(eyes)
k.export(out)
