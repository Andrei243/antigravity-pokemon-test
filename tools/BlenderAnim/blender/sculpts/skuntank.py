# Skuntank sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/skuntank.py -- <the export's Skuntank.glb> <out.glb>
# Stunky grown: a stout purple skunk on short legs, its head low and forward. A huge bushy tail rises from the rump,
# its spikes standing up behind, and arches forward over the whole back so that its cream tip lies over the head like
# a forelock; the tail's underside is a cream band running from that tip back along it and down its rear. A cream
# muzzle and puffed cream cheeks, a fang, small sour eyes ringed orange under a flat purple brow, an orange nose,
# small pointed ears lined orange, dark whiskers; the legs and the underside cream in ragged fur below, grey-blue paws
# with white claws.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

PURPLE, CREAM, ORANGE, BROWN, INK = "#5E3A72", "#F2E2B0", "#E8781E", "#B8521A", "#1C1220"
PAW, CLAW, DARK = "#9C9CB8", "#F4F2F0", "#3E2650"
H = np.array([0, -0.37, 0.37])           # the head's centre
HR = np.array([0.135, 0.12, 0.13])


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


# --- the body: long and stout, low on short legs ---------------------------------------------------------------------
k.add("body", ell((0, 0.03, 0.355), (0.17, 0.27, 0.13)), PURPLE, "body")
k.add("chest", ell((0, -0.16, 0.35), (0.16, 0.13, 0.13)), PURPLE, "body")
k.add("rump", ell((0, 0.21, 0.36), (0.16, 0.12, 0.13)), PURPLE, "body")
k.add("neck", ell((0, -0.26, 0.355), (0.12, 0.1, 0.11)), PURPLE, "head")
# The back rises into the tail's underside: one mass, no daylight between
k.add("saddle", ell((0, 0.08, 0.52), (0.13, 0.25, 0.09)), PURPLE, "body")

# The head: low and forward; a cream muzzle standing forward with puffed cream cheeks either side
k.add("head", ell(H, HR), PURPLE, "head")
k.add("muzzle", ell((0, -0.475, 0.3), (0.075, 0.07, 0.065)), CREAM, "head")
k.add("cheek", ell((0.062, -0.45, 0.285), (0.07, 0.065, 0.065)), CREAM, "head", mirror=True)
k.add("chin", ell((0, -0.44, 0.255), (0.07, 0.06, 0.045)), CREAM, "head")

# Legs, short and stout, the front ones slanting forward a little, the hind ones back; grey-blue paws
k.add("leg", tube([(0.094, -0.121, 0.2), (0.102, -0.15, 0.11), (0.105, -0.17, 0.045)], [0.054, 0.046, 0.042]),
      PURPLE, "leg{s}", mirror=True)
k.add("paw", ell((0.106, -0.188, 0.026), (0.046, 0.058, 0.027)), PAW, "leg{s}", mirror=True)
k.add("thigh", ell((0.1, 0.17, 0.25), (0.07, 0.09, 0.095)), PURPLE, "hind{s}", mirror=True)
k.add("hind", tube([(0.094, 0.162, 0.2), (0.103, 0.19, 0.11), (0.105, 0.205, 0.045)], [0.052, 0.045, 0.042]),
      PURPLE, "hind{s}", mirror=True)
k.add("hpaw", ell((0.106, 0.19, 0.026), (0.046, 0.058, 0.027)), PAW, "hind{s}", mirror=True)
for bone, y in (("leg{s}", -0.188), ("hind{s}", 0.19)):
    for i, a in enumerate((-1, 0, 1)):
        base = np.array((0.106 + 0.024 * a, y - 0.045, 0.02))
        k.add(f"claw{bone[0]}{i}", tube([base, base + (0.006 * a, -0.03, -0.012)], [0.009, 0.002]), CLAW, bone,
              fuse=False, mirror=True, surface="shell")

# --- the tail: from the rump up, then forward over the whole back to a cream tip lying over the head ------------------
# The rising plume behind is the tail bone's; the stretch lying along the back and the tip go with the body, so that a
# turn of the tail swings the plume and never the forelock into the head
PATH = np.array([(0, 0.27, 0.38), (0, 0.36, 0.5), (0, 0.34, 0.62), (0, 0.22, 0.69), (0, 0.05, 0.71),
                 (0, -0.12, 0.705), (0, -0.27, 0.665), (0, -0.39, 0.615), (0, -0.48, 0.575)])
RAD = [0.09, 0.13, 0.15, 0.16, 0.155, 0.14, 0.12, 0.1, 0.06]
SPLIT = 3
k.add("tail", tube(PATH[:SPLIT + 1], RAD[:SPLIT + 1], seg=24), PURPLE, "tail")
k.add("tailfront", tube(PATH[SPLIT:], RAD[SPLIT:], seg=24), PURPLE, "body")
# The tip a broad, blunt cream forelock laid forward and a little down over the brow, just clear of the crown
k.add("lock", ell((0, -0.485, 0.575), (0.105, 0.085, 0.05), rot=(-22, 0, 0)), CREAM, "body")


def dense(pts, n=320):
    P = []
    for i in range(len(pts) - 1):
        for t in np.linspace(0, 1, n // (len(pts) - 1), endpoint=False):
            P.append(pts[i] + (pts[i + 1] - pts[i]) * t)
    P.append(pts[-1])
    return np.array(P)


LINE = dense(PATH)
ARCH = np.array([0, 0.03, 0.45])          # the arch's middle: outward is away from it


def frame_at(t):
    i = int(round(t * (len(LINE) - 1)))
    a, b = LINE[max(i - 3, 0)], LINE[min(i + 3, len(LINE) - 1)]
    d = (b - a) / np.linalg.norm(b - a)
    o = LINE[i] - ARCH; o[0] = 0
    o = o - d * (o @ d)
    return LINE[i], d, o / np.linalg.norm(o)


def radius_at(t):
    return float(np.interp(t, np.linspace(0, 1, len(RAD)), RAD))


# Spikes of fur: tall ones standing up behind from the rising part, then along the top sweeping back, and a row a side
# (share along the tail, length, how far it sweeps back toward the rump; the rear ones stand up)
SPIKES = [(0.04, 0.17, 0.15), (0.11, 0.2, 0.35), (0.19, 0.18, 0.6), (0.28, 0.16, 1.0), (0.37, 0.15, 1.15),
          (0.46, 0.13, 1.25), (0.55, 0.11, 1.3), (0.63, 0.08, 1.3)]
for i, (t, length, sweep) in enumerate(SPIKES):
    p, d, o = frame_at(t)
    r = radius_at(t)
    base = p + o * r * 0.5
    tip = p + o * (r + length) - d * sweep * length
    mid = (base + tip) / 2 + o * length * 0.18
    bone = "tail" if t < 0.33 else "body"
    k.add(f"tailspike{i}", blade([base, mid, tip], [r * 0.6, r * 0.3, 0.005], (1, 0, 0), 0.45),
          PURPLE, bone)
    side = np.array([r * 0.6, 0, 0])
    k.add(f"tailside{i}", blade([base + side * 0.8 - o * r * 0.2, mid + side * 1.2 - o * length * 0.15,
                                 tip + side * 1.35 - o * length * 0.35 + d * length * 0.15],
                                [r * 0.48, r * 0.25, 0.005], (0.4, 0, 0.1), 0.45), PURPLE, bone, mirror=True)


def tail_place(P):
    """Each point's share t along the tail and its offset along the outward normal there."""
    d2 = ((P[:, None, :] - LINE[None, ::4]) ** 2).sum(2)
    j = d2.argmin(1) * 4
    t = j / (len(LINE) - 1)
    near = LINE[j]
    o = near - ARCH; o[:, 0] = 0
    nxt = LINE[np.minimum(j + 4, len(LINE) - 1)]; prv = LINE[np.maximum(j - 4, 0)]
    tg = nxt - prv; tg /= np.linalg.norm(tg, axis=1, keepdims=True)
    o = o - tg * (o * tg).sum(1, keepdims=True)
    o /= np.linalg.norm(o, axis=1, keepdims=True)
    return t, ((P - near) * o).sum(1), np.interp(t, np.linspace(0, 1, len(RAD)), RAD)


# The inner half of the arch cream, a ragged edge, the whole front of the tail cream from above the brow
def tail_cream(P):
    t, off, r = tail_place(P)
    edge = off - r * (0.05 + 0.18 * tri(t * 9))
    return np.maximum(inside(edge, 0.004), inside(0.8 - t, 0.006))
k.paint(tail_cream, CREAM, on=["tail", "lock"])

# --- fur below: the legs cream below the knee and the underside cream, each in a ragged edge -------------------
def low(P):
    jag = 0.022 * tri(P[:, 1] / 0.05 + 0.3 * P[:, 0] / 0.05)
    return inside(P[:, 2] - (0.2 - 0.035 * smoothstep(-0.12, -0.2, P[:, 1]) + jag), 0.003)
k.paint(low, CREAM, on=["leg", "hind", "thigh", "body", "chest", "rump"])
# the legs' ragged cuffs higher on the front of each leg, as in the drawing
k.paint(lambda P: inside(P[:, 2] - (0.15 + 0.02 * tri(P[:, 0] / 0.035)), 0.003) * (P[:, 2] > 0.03), CREAM,
        on=["leg", "hind"])

# Small pointed ears at the back of the crown, poking up beside the cream lock, lined orange. They go with the head:
# the ear joints stand 0.15 behind them, where the old sculpt had its head
EA = [(0.075, -0.34, 0.46), (0.095, -0.325, 0.505), (0.115, -0.31, 0.545)]
k.add("ear", blade(EA, [0.034, 0.022, 0.004], (0, 1, 0), 0.5), PURPLE, "head", mirror=True)
k.decal((0.093, -0.35, 0.505), unit(0.15, -1, 0.1), (0.012, 0.026), ORANGE, up=unit(0.3, 0, 1), eps=0.003,
        rings=10, segs=40, mirror=True, outline=lambda th: 1 - 0.45 * max(0.0, math.sin(th)) ** 1.5)

# Ragged purple fur at the back of the cheeks
for i, (a, b) in enumerate((((0.11, -0.37, 0.3), (0.18, -0.33, 0.28)), ((0.11, -0.34, 0.35), (0.175, -0.3, 0.355)))):
    k.add(f"cheekfur{i}", blade([a, b], [0.03, 0.004], (0, 1, 0.3), 0.5), PURPLE, "head", mirror=True)

# An orange nose at the top of the cream muzzle, rounded
k.add("nose", ell((0, -0.538, 0.335), (0.02, 0.014, 0.016)), ORANGE, "head", fuse=False, surface="skin")

# Three dark whiskers out of each cheek
for i, (a, b) in enumerate((((0.115, -0.415, 0.33), (0.2, -0.4, 0.355)), ((0.12, -0.41, 0.31), (0.21, -0.39, 0.305)),
                            ((0.115, -0.405, 0.29), (0.195, -0.385, 0.26)))):
    a, b = np.array(a), np.array(b)
    k.add(f"whisker{i}", tube([a, (a + b) / 2, b], [0.0045, 0.0035, 0.002], seg=8, per=6), DARK, "head",
          fuse=False, mirror=True, surface="hair")

# The mouth: a wide sour frown across the cream under the nose, with a fang standing up at one corner
CC, CI = srgb(CREAM), srgb(INK)
def mouth(u, v):
    c = np.tile(CC, (len(u), 1))
    c[(np.abs(v - (-0.25 + 0.45 * (1 - u * u))) < 0.2) & (np.abs(u) < 0.9)] = CI
    return c
k.decal((0, -0.535, 0.27), unit(0, -1, -0.15), (0.05, 0.032), mouth, eps=0.003, rings=24, segs=96)
k.add("fang", tube([(0.036, -0.528, 0.265), (0.038, -0.533, 0.287)], [0.008, 0.002]), CLAW, "head", fuse=False,
      surface="shell")


k.build(voxel=0.009)

# The eyes: Stunky's make, smaller and sourer. No rim of ink: an orange ring, white within, a small black pupil;
# each patch cut straight along its top by the purple brow, low and slanting down to the nose.
HE = 0.03
def eye_top(u):
    return 0.05 + 0.35 * u
de = unit(0.42, -0.9, 0.1)
E = np.array([0.058, -0.478, 0.385])
def frame(n):
    n = np.array(n, float); n /= np.linalg.norm(n)
    right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
    return right, np.cross(n, right)
WHITE, PUPIL = np.array([1.0, 1.0, 1.0]), srgb("#141018")
CO, CB, CI, CP = srgb(ORANGE), srgb(BROWN), srgb(INK), srgb(PURPLE)
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
OUT = clipped(1.25, 1.0)
def eye(u, v):
    c = np.tile(WHITE, (len(u), 1))
    r = np.sqrt((u / 1.25) ** 2 + v ** 2)
    c[(r > 0.55) | (v > eye_top(u) - 0.2)] = CO
    c[(r > 0.55) & (r < 0.63)] = CB
    return c
def eyes(state):
    look = eye_look(state, eye, CO, CI, line=0.13)
    for side in (1, -1):
        n = de * np.array([side, 1, 1.0])
        outline = polygon([(side * a, b) for a, b in OUT])
        col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
        k.decal(E * np.array([side, 1, 1.0]), n, (HE, HE), col, eps=0.005, outline=outline, rings=20, segs=56)
        if state in ("Open", "Fierce"):
            right, up = frame(n)
            c = E * np.array([side, 1, 1.0]) + right * side * (-0.1 * HE) + up * (-0.2 * HE)
            k.decal(c, n, (0.0058, 0.0072), PUPIL, eps=0.008, rings=6, segs=24)

k.eye_states(eyes)
k.export(out)
