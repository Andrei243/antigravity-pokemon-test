# Yanma sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/yanma.py -- <the export's Yanma.glb> <out.glb>
#
# The design, from the references: a red dragonfly with a great lime-green round head. A red stripe runs over the top of
# the head from the back to a point on the forehead, two red horns standing up out of it in a V; black oval eyes with a
# white glint on the front of the green head, wide apart; a red two-lobed mouth under them with a dark parting. A short
# red thorax; six thin blue-grey legs bent at the knee, hanging under it; four long clear wings, pale with a grey edge,
# two orange bands across each near its tip; a long thin red tail ringed with dark segment lines, ending in a red bar
# of two paddles across it with a knob in the middle and two small white spikes on the knob.
#
# Written to be grown from (Yanmega keeps its frame): the wings are `wing` (a pale plate in a grey rim, bands as
# thicker plates inside it so they show front and back), the eyes `eye_frame` and `eyes`, the tail `TAIL` with its
# rings painted by `segments`.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="shell")

RED, REDDARK, LIME, LEG = "#E23A3E", "#8E1E2A", "#A6D838", "#5E7A9E"
WING, WINGEDGE, BAND, WHITE, INK = "#E8EEF6", "#AEBACA", "#F48A2C", "#F6F4F0", "#16141E"

# --- flat plates (beautifly.py's) ------------------------------------------------------------------------------------
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


# --- the wings ---------------------------------------------------------------------------------------------------------
def wing_width(t, W):
    """A dragonfly wing's half width along it (0 at the root, 1 at the tip): an ellipse, narrower toward the root."""
    return W * np.sqrt(np.clip(1 - ((t - 0.5) / 0.5) ** 2, 0, 1)) * (0.55 + 0.45 * t)


def wing_outline(L, W, t0=0.0, t1=1.0, inset=1.0, n=28):
    """The wing's outline between t0 and t1 along it, its width scaled by inset (a panel inside the wing)."""
    t = np.linspace(t0, t1, n)
    a = 0.004 + t * L
    w = np.maximum(wing_width(t, W) * inset, 0.0008)
    return np.vstack([np.stack([a, w], 1), np.stack([a, -w], 1)[::-1]])


def wing_axes(s, sweep, lift, twist):
    """The plane of a wing on side s: outward raised by lift and swept by sweep (degrees, + forward), its width along
    the body tilted by twist so that its face shows to the cameras at the model's own height."""
    sw, li = math.radians(sweep), math.radians(lift)
    ex = np.array([s * math.cos(li) * math.cos(sw), -math.cos(li) * math.sin(sw), math.sin(li)])
    ez = np.array([0.0, 1.0, math.tan(math.radians(twist))])
    ez = ez - ex * np.dot(ez, ex)
    return ex, ez / np.linalg.norm(ez)


def wing(name, root, bone, L, W, sweep, lift, twist, bands, band_colour, rim=WINGEDGE, fill=WING, thick=0.004):
    """A wing on both sides: a grey rim plate, a pale plate inside it, coloured bands as thicker plates across it."""
    for s, side in ((1, "R"), (-1, "L")):
        o = np.array(root, float) * (s, 1, 1)
        ex, ez = wing_axes(s, sweep, lift, twist)
        b = bone.format(s=side)
        k.add(f"{name}{side}", flat(wing_outline(L, W), o, ex, ez, thick), rim, b, fuse=False)
        k.add(f"{name}in{side}", flat(wing_outline(L, W, 0.04, 0.985, 0.82), o, ex, ez, thick * 1.5), fill, b,
              fuse=False)
        for i, (t0, t1) in enumerate(bands):
            k.add(f"{name}band{i}{side}", flat(wing_outline(L, W, t0, t1, 0.9, n=10), o, ex, ez, thick * 2.2),
                  band_colour, b, fuse=False)


L_WING, W_WING = 0.165, 0.032
wing("wingF", (0.03, -0.05, 0.24), "wing{s}", L_WING, W_WING, 22, 30, 38, [(0.6, 0.7), (0.8, 0.9)], BAND)
wing("wingH", (0.03, -0.002, 0.24), "wing2{s}", L_WING * 0.95, W_WING * 0.95, -24, 26, -38, [(0.6, 0.7), (0.8, 0.9)],
     BAND)

# --- the body: a short red thorax, the head forward of it ----------------------------------------------------------
k.add("thorax", ell((0, -0.025, 0.205), (0.034, 0.055, 0.035)), RED, "body")
k.add("waist", ell((0, 0.03, 0.208), (0.02, 0.025, 0.02)), RED, "tail")
k.add("wingroot", ell((0.024, -0.024, 0.232), (0.012, 0.04, 0.01)), RED, "body", mirror=True)

# --- the tail: long and thin, rising a little behind and out to +X, ringed into segments ---------------------------
TAIL = np.array([(0, 0.03, 0.209), (0.004, 0.09, 0.222), (0.016, 0.15, 0.244), (0.036, 0.2, 0.272),
                 (0.06, 0.236, 0.298)])
TAIL_R = [0.017, 0.0145, 0.013, 0.012, 0.0115]
k.add("tail", tube(TAIL, TAIL_R), RED, "tail")


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


def segments(pts, first, step, count, width, reach, colour):
    """Rings of paint across a tail, `count` of them `step` apart from `first` along it."""
    def m(P):
        s, d = along(P, pts)
        ring = np.zeros(len(P))
        for i in range(count):
            ring = np.maximum(ring, inside(np.abs(s - (first + i * step)) - width, 0.0018))
        return ring * inside(d - reach, 0.002)
    k.paint(m, colour, on=["tail"])


segments(TAIL, 0.05, 0.045, 5, 0.0022, 0.03, REDDARK)

# the tail's end: a bar of two paddles across it, a knob in the middle with two small white spikes
END = TAIL[-1]
DIR = unit(*(TAIL[-1] - TAIL[-2]))
SIDE = np.cross(DIR, (0, 0, 1.0)); SIDE /= np.linalg.norm(SIDE)
k.add("knob", ell(END + DIR * 0.006, (0.017, 0.017, 0.016)), RED, "tail", fuse=False)
for s, side in ((1, "R"), (-1, "L")):
    d = SIDE * s
    pts = [END + d * 0.008, END + d * 0.03, END + d * 0.05, END + d * 0.066]
    k.add(f"paddle{side}", blade(pts, [0.008, 0.009, 0.016, 0.011], (0, 0, 1), 0.55), RED, "tail", fuse=False)
    base = END + DIR * 0.006 + d * 0.008 + np.array([0, 0, 0.012])
    tip = base + unit(*(d * 0.3 + DIR * 0.6 + np.array([0, 0, 0.9]))) * 0.022
    k.add(f"spike{side}", tube([base, (base + tip) / 2, tip], [0.005, 0.0035, 0.0008]), WHITE, "tail", fuse=False)

# --- the legs: six thin blue-grey legs bent at the knee, a small foot each ---------------------------------------------
# short and tucked close under the thorax, as the art holds them: each about a third of the head's height
LEGS = [("legF", "leg{s}", [(0.018, -0.058, 0.182), (0.042, -0.076, 0.164), (0.03, -0.092, 0.142)]),
        ("legM", "hind{s}", [(0.02, -0.022, 0.178), (0.048, -0.026, 0.16), (0.036, -0.016, 0.138)]),
        ("legB", "hind{s}", [(0.016, 0.01, 0.182), (0.044, 0.028, 0.163), (0.034, 0.044, 0.142)])]
for name, bone, pts in LEGS:
    k.add(name, tube(pts, [0.0075, 0.0062, 0.0048]), LEG, bone, fuse=False, mirror=True)
    f = np.array(pts[-1])
    k.add(name + "foot", blade([f + (0, 0.004, 0.004), f + (0.004, -0.008, -0.004), f + (0.008, -0.016, -0.004)],
                                [0.006, 0.004, 0.001], (0, 0, 1), 0.6), LEG, bone, fuse=False, mirror=True)

# --- the head: a great lime ball, the red stripe over its top, red horns, a red mouth ---------------------------------
H = np.array([0.0, -0.135, 0.226])
HR = np.array([0.066, 0.062, 0.062])
k.add("head", ell(H, HR), LIME, "head")
# the mouth: two red lobes under the eyes, standing forward of the ball
k.add("mouth", ell((0.012, -0.184, 0.186), (0.017, 0.014, 0.016)), RED, "head", mirror=True)


def stripe(P):
    q = P - H
    phi = np.degrees(np.arctan2(-q[:, 1], q[:, 2]))           # 0 at the top, 90 at the front, -90 at the back
    w = 0.019 * np.clip((52 - phi) / 56, 0, 1) ** 0.8
    near = np.linalg.norm(q / HR, axis=1) < 1.2
    return inside(np.abs(q[:, 0]) - w, 0.0012) * inside(phi - 52, 0.5) * (phi > -150) * near * (q[:, 2] > -0.02)
k.paint(stripe, RED, on=["head"])
# the mouth's dark parting between its lobes
k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.0022, 0.0012) * inside(P[:, 1] + 0.186, 0.003)
        * inside(np.abs(P[:, 2] - 0.186) - 0.012, 0.002), REDDARK, on=["mouth"])

# the horns: red cones standing up and out of the stripe in a V
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"horn{side}", blade([np.array((0.012, -0.152, 0.27)) * m, np.array((0.022, -0.158, 0.302)) * m,
                                np.array((0.036, -0.162, 0.336)) * m],
                               [0.013, 0.008, 0.0012], (0, 1, 0), 0.7), RED, "head", fuse=False)

k.build(voxel=0.0032, smooth=4, band=0.006)


# --- the eyes: black upright ovals with a white glint, wide apart on the front of the green head -----------------------
CK, CW, CL, CB = srgb(INK), np.array([1.0, 1.0, 1.0]), srgb(LIME), srgb("#2A3A66")
def eye(u, v):
    c = np.tile(CK, (len(u), 1))
    c[((u + 0.0) / 0.55) ** 2 + ((v + 0.55) / 0.35) ** 2 < 1] = CB          # a dark blue sheen low in the eye
    c[((u + 0.18) / 0.32) ** 2 + ((v - 0.3) / 0.32) ** 2 < 1] = CW         # the glint, up and toward the nose
    return c


def eye_frame(side, H, HR, d=(0.6, -0.78, -0.06)):
    """Where an eye sits on a head ball, and the way it faces."""
    de = unit(side * d[0], d[1], d[2])
    p = H + de / math.sqrt(((de / HR) ** 2).sum())
    n = (p - H) / HR ** 2
    return p, n / np.linalg.norm(n)


EYE_HALF = (0.0125, 0.0185)
def eyes(state):
    look = eye_look(state, eye, CL, CK, line=0.16)
    for side in (1, -1):
        c, n = eye_frame(side, H, HR)
        col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
        k.decal(c, n, EYE_HALF, col, eps=0.003, rings=20, segs=56)


k.eye_states(eyes)
k.export(out)
