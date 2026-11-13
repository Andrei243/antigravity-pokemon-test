# Stunky sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/stunky.py -- <the export's Stunky.glb> <out.glb>
# A purple skunk on all fours with a big round head: a cream cap over the crown that comes down to a point between
# the eyes and runs on down the back as a stripe into a great spiky tail raised over the back; white eyes ringed
# orange and cut slanting by the cream brow, an orange beak of a nose, round purple jowls, cream whiskers, small
# pointed cream ears lined orange, a cream tuft at the nape, a cream underside and white claws.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

PURPLE, CREAM, ORANGE, BROWN, INK, CLAW = "#5E3A72", "#F2E2B0", "#E8781E", "#B8521A", "#1C1220", "#F4F2F0"
H = np.array([0, -0.19, 0.262])          # the head's centre
HR = np.array([0.135, 0.118, 0.112])


def in_poly(x, y, pts):
    pts = np.array(pts, float)
    res = np.zeros(len(x), bool)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        res ^= ((y1 > y) != (y2 > y)) & (x < (x2 - x1) * (y - y1) / (y2 - y1 + 1e-12) + x1)
    return res


def poly_mask(x, y, pts, edge=0.003):
    pts = np.array(pts, float)
    d = np.full(len(x), 9.0)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        ex, ey = x2 - x1, y2 - y1
        t = np.clip(((x - x1) * ex + (y - y1) * ey) / (ex * ex + ey * ey + 1e-12), 0, 1)
        d = np.minimum(d, np.hypot(x - x1 - t * ex, y - y1 - t * ey))
    return inside(np.where(in_poly(x, y, pts), -d, d), edge)


# --- the body ---------------------------------------------------------------------------------------------------
k.add("head", ell(H, HR), PURPLE, "head")
# Round jowls under the eyes, meeting in a crease down the middle
k.add("jowl", ell((0.052, -0.265, 0.205), (0.072, 0.06, 0.062)), PURPLE, "head", mirror=True)
k.add("neck", ell((0, -0.1, 0.24), (0.1, 0.09, 0.09)), PURPLE, "body")
k.add("body", ell((0, 0.04, 0.205), (0.118, 0.17, 0.1)), PURPLE, "body")

# Legs, short and stout, with paws
k.add("leg", tube([(0.072, -0.09, 0.18), (0.078, -0.1, 0.04)], [0.042, 0.038]), PURPLE, "leg{s}", mirror=True)
k.add("paw", ell((0.078, -0.115, 0.026), (0.042, 0.05, 0.026)), PURPLE, "leg{s}", mirror=True)
k.add("thigh", ell((0.072, 0.12, 0.17), (0.055, 0.07, 0.07)), PURPLE, "hind{s}", mirror=True)
k.add("hind", tube([(0.075, 0.13, 0.15), (0.078, 0.125, 0.04)], [0.04, 0.037]), PURPLE, "hind{s}", mirror=True)
k.add("hpaw", ell((0.078, 0.105, 0.026), (0.042, 0.05, 0.026)), PURPLE, "hind{s}", mirror=True)
# Three white claws on every paw
for bone, y in (("leg{s}", -0.115), ("hind{s}", 0.105)):
    for i, a in enumerate((-1, 0, 1)):
        base = np.array((0.078 + 0.02 * a, y - 0.04, 0.018))
        k.add(f"claw{bone[0]}{i}", tube([base, base + (0.005 * a, -0.025, -0.01)], [0.008, 0.002]), CLAW, bone,
              fuse=False, mirror=True, surface="shell")

# --- the tail: raised from the rump, up and back over the back, its fur in points along its back and tip --------------
PATH = np.array([(0, 0.16, 0.25), (0, 0.25, 0.32), (0, 0.32, 0.42), (0, 0.35, 0.51), (0, 0.33, 0.58)])
RAD = [0.07, 0.11, 0.13, 0.12, 0.08]
k.add("tail", blade(PATH, RAD, (1, 0, 0), 0.55), PURPLE, "tail")
def dense(pts, n=200):
    P = []
    for i in range(len(pts) - 1):
        for t in np.linspace(0, 1, n // (len(pts) - 1), endpoint=False):
            P.append(pts[i] + (pts[i + 1] - pts[i]) * t)
    P.append(pts[-1])
    return np.array(P)
LINE = dense(PATH)
def frame_at(t):
    """Point, tangent and the outward (back) normal of the tail's line at t in 0..1."""
    i = int(round(t * (len(LINE) - 1)))
    a, b = LINE[max(i - 3, 0)], LINE[min(i + 3, len(LINE) - 1)]
    d = (b - a) / np.linalg.norm(b - a)
    out_ = np.array([0, d[2], -d[1]])          # the tangent turned in its plane, toward the back
    if out_[1] < 0 and t < 0.8:
        out_ = -out_
    return LINE[i], d, out_ / np.linalg.norm(out_)
def radius_at(t):
    return float(np.interp(t, np.linspace(0, 1, len(RAD)), RAD))
# Spikes along the back of the tail, sweeping toward the tip, and a fan of them round the tip
for i, t in enumerate((0.2, 0.34, 0.47, 0.6, 0.72, 0.83)):
    p, d, o = frame_at(t)
    r = radius_at(t)
    base = p + o * r * 0.5
    tip = p + o * (r + 0.06 + 0.012 * i) + d * 0.06
    k.add(f"tailspike{i}", blade([base, (base + tip) / 2 + o * 0.01, tip], [r * 0.8, r * 0.45, 0.005], (1, 0, 0),
                                 0.45), PURPLE, "tail")
    if i in (1, 3, 5):
        side = np.array([0.045, 0, 0])
        k.add(f"tailside{i}", blade([base + side * 0.5, (base + tip) / 2 + side * 1.1, tip + side * 1.5 - o * 0.02],
                                    [r * 0.6, r * 0.35, 0.005], (0.3, 0, 0.1), 0.5), PURPLE, "tail", mirror=True)
p, d, o = frame_at(1.0)
for i, (a, b) in enumerate(((0.0, 1.0), (0.5, 0.6), (-0.5, 0.6))):
    base = p - d * 0.02
    tip = p + d * 0.05 + o * 0.06 + np.array([0.05 * a, 0, 0]) * b
    k.add(f"tailtip{i}", blade([base, tip], [0.05, 0.005], (1, 0, 0) if a == 0 else (0, 1, 0), 0.5), PURPLE, "tail")

# The tail's front, toward the head, cream: the stripe widening up the tail
def tail_front(P):
    d2 = ((P[:, None, :] - LINE[None, ::4]) ** 2).sum(2)
    j = d2.argmin(1) * 4
    t = j / (len(LINE) - 1)
    near = LINE[j]
    nxt = LINE[np.minimum(j + 4, len(LINE) - 1)]; prv = LINE[np.maximum(j - 4, 0)]
    tg = nxt - prv; tg /= np.linalg.norm(tg, axis=1, keepdims=True)
    back = np.stack([np.zeros(len(P)), tg[:, 2], -tg[:, 1]], 1)
    back[back[:, 1] < 0] *= -1
    off = ((P - near) * back).sum(1)
    r = np.interp(t, np.linspace(0, 1, len(RAD)), RAD)
    return inside(off + 0.05 * r, 0.004)
k.paint(tail_front, CREAM, on=["tail"])

# --- the head's fur ---------------------------------------------------------------------------------------------
# Small pointed ears at the crown's corners, cream, an orange lining in front
EA = [(0.07, -0.17, 0.33), (0.085, -0.17, 0.39), (0.1, -0.165, 0.445)]
k.add("ear", blade(EA, [0.042, 0.028, 0.004], (0, 1, 0), 0.5), CREAM, "ear{s}", mirror=True)
# A cream tuft rising at the nape, curving back
k.add("tuft", blade([(0.0, -0.13, 0.33), (0.004, -0.115, 0.385), (0.012, -0.09, 0.42), (0.022, -0.06, 0.435)],
                    [0.032, 0.022, 0.012, 0.004], (1, 0, 0), 0.5), CREAM, "head")

# The eyes' make, in an eye's own units (u outward, v up, one unit HE): white ringed orange, the top cut straight by
# the cream brow and slanting down toward the nose: the sly look
HE = 0.04
def eye_top(u):
    return 0.2 + 0.45 * u
de = unit(0.4, -0.9, 0.12)
E = np.array([0.058, -0.302, 0.287])
def frame(n):
    n = np.array(n, float); n /= np.linalg.norm(n)
    right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
    return right, np.cross(n, right)
ERI, EUP = frame(de)
R = 0.135
def round_head(P):
    P = np.atleast_2d(P)
    return np.arctan2(P[:, 0], -(P[:, 1] - H[1])) * R, P[:, 2]
def on_head(u, v):
    o = E + de * 0.35 + ERI * u * HE + EUP * v * HE
    oc, dd = (o - H) / HR, -de / HR
    a, b, c = dd @ dd, 2 * oc @ dd, oc @ oc - 1
    return o + dd * HR * (-b - math.sqrt(max(b * b - 4 * a * c, 0))) / (2 * a)

# The cream cap: above a line along each eye's cut (a little under it, so the eye's own edge is the line), down to a
# point between the eyes, out over the temples and up behind the ears; the back of the head purple but for the stripe
brow = []
for u in np.linspace(-1.2, 1.15, 30):
    s, z = round_head(on_head(u, eye_top(u) - 0.18))
    if s[0] > 0.003:
        brow.append((float(s[0]), float(z[0])))
(s0, z0), (s1, z1) = brow[0], brow[1]
point = z0 - (z1 - z0) / (s1 - s0) * s0
CAP_HALF = [(0.0, point)] + brow + [(0.15, brow[-1][1] + 0.02), (0.2, 0.36), (0.26, 0.5), (0.0, 0.5)]
CAP = CAP_HALF + [(-a, z) for a, z in CAP_HALF[::-1][1:-1]]
def cap(P):
    s, z = round_head(P)
    return poly_mask(s, z, CAP, edge=0.0025) * (P[:, 1] < H[1] + 0.06)
k.paint(cap, CREAM, on=["head", "ear", "tuft", "neck"])

# The stripe down the back, from the nape to the tail, widening as it goes
def stripe(P):
    w = 0.03 + 0.05 * np.clip((P[:, 1] + 0.12) / 0.3, 0, 1)
    return inside(np.abs(P[:, 0]) - w, 0.003) * (P[:, 2] > 0.25) * (P[:, 1] > -0.16) * (P[:, 1] < 0.17)
k.paint(stripe, CREAM, on=["neck", "body", "head", "tail"])
# The cream underside between the legs
k.paint(lambda P: inside(P[:, 2] - 0.118, 0.004) * (P[:, 1] > -0.06) * (P[:, 1] < 0.16), CREAM, on=["body"])

# The ears' orange linings, crisp
en = unit(0, -1, 0.05); eu = unit(0.25, 0, 1)
k.decal((0.086, -0.19, 0.39), en, (0.015, 0.032), BROWN, up=eu, eps=0.003, rings=10, segs=40, mirror=True,
        outline=lambda th: 1 - 0.45 * max(0.0, math.sin(th)) ** 1.5)

# The orange beak of a nose at the point of the cream, pointing down
def beak(V):
    V = V.copy()
    t = np.clip((V[:, 2] + 1) / 2, 0, 1)
    V[:, 0] *= 0.2 + 0.85 * t
    V[:, 1] *= 0.5 + 0.6 * t
    return V
k.add("nose", ell((0, -0.315, 0.252), (0.02, 0.016, 0.026), rot=(-15, 0, 0), deform=beak), ORANGE, "head",
      fuse=False, surface="skin")
# The mouth: a short line down from under the beak to where the jowls meet
CP, CI = srgb(PURPLE), srgb(INK)
def mouth(u, v):
    c = np.tile(CP, (len(u), 1))
    c[(np.abs(u) < 0.12) & (v > -0.6)] = CI
    c[(np.abs(v + 0.6 - 0.5 * u * u) < 0.12) & (np.abs(u) < 0.55)] = CI
    return c
k.decal((0, -0.325, 0.19), unit(0, -1, 0.0), (0.03, 0.045), mouth, eps=0.003, rings=14, segs=56)

# Three cream whiskers out of each jowl
for i, (a, b) in enumerate((((0.1, -0.27, 0.225), (0.22, -0.31, 0.25)), ((0.105, -0.27, 0.205), (0.235, -0.295, 0.2)),
                            ((0.1, -0.27, 0.185), (0.21, -0.29, 0.15)))):
    a, b = np.array(a), np.array(b)
    k.add(f"whisker{i}", blade([a, (a + b) / 2, b], [0.014, 0.01, 0.003], (0, 0, 1), 0.5), CREAM, "head", fuse=False,
          mirror=True, surface="hair")

k.build(voxel=0.006)

# The eyes: no rim of ink; an orange ring, white within, a small black pupil, each patch cut straight along its top
# where the cream comes down over it. Laid by hand a side, u toward +X: the left eye's outline and colour flipped.
WHITE, RING, PUPIL = np.array([1.0, 1.0, 1.0]), srgb(ORANGE), srgb("#141018")
def clipped(rx, ry, cx=0.0, cy=0.0, n=96):
    pts = []
    for th in np.linspace(0, 2 * math.pi, n, endpoint=False):
        du, dv = rx * math.cos(th), ry * math.sin(th)
        t = 1.0
        if cy + dv > eye_top(cx + du):
            lo, hi = 0.0, 1.0
            for _ in range(40):
                t = (lo + hi) / 2
                if cy + t * dv > eye_top(cx + t * du):
                    hi = t
                else:
                    lo = t
            t = lo
        pts.append((t * math.cos(th), t * math.sin(th)))
    return pts
OUT = clipped(1.0, 0.92)
def eye(u, v):
    c = np.tile(WHITE, (len(u), 1))
    r = np.sqrt(u * u + (v / 0.92) ** 2)
    c[(r > 0.66) | (v > eye_top(u) - 0.16)] = RING
    c[(r > 0.66) & (r < 0.74)] = srgb(BROWN)
    return c
CO = srgb(ORANGE)
def eyes(state):
    look = eye_look(state, eye, CO, CI, line=0.13)
    for side in (1, -1):
        n = de * np.array([side, 1, 1.0])
        outline = polygon([(side * a, b) for a, b in OUT])
        col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
        k.decal(E * np.array([side, 1, 1.0]), n, (HE, HE), col, eps=0.005, outline=outline, rings=20, segs=56)
        if state in ("Open", "Fierce"):
            right, up = frame(n)
            c = E * np.array([side, 1, 1.0]) + right * side * (-0.12 * HE) + up * (-0.12 * HE)
            k.decal(c, n, (0.0075, 0.0095), PUPIL, eps=0.008, rings=6, segs=24)

k.eye_states(eyes)
k.export(out)
