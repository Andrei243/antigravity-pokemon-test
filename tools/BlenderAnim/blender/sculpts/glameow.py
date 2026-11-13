# Glameow sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/glameow.py -- <the export's Glameow.glb> <out.glb>
# A slender grey-blue cat standing on four thin legs (the skeleton's pose; the art sits): a wide head narrowing to a
# white muzzle, tall crescent ears lined pink with white tips, haughty half-shut eyes under pink lids, spiky tufts on
# the chest and haunches, white toes, and a long thin tail wound up behind it like a spring to a white puff.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

GREY, WHITE, PINK, LID, INK = "#8494B6", "#F4F5F9", "#EE9CBA", "#E888B0", "#22222E"
YELLOW, BLUE, BLUED = "#F4D244", "#3E92E4", "#1E5CAE"
H = np.array([0, -0.165, 0.42])           # the head's centre
HR = np.array([0.118, 0.096, 0.094])


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


def mirrored(half):
    return half + [(-a, z) for a, z in half[::-1][1:-1]]


def along(P, pts, n=200):
    """Nearest place on a polyline (|x| taken, so a mirrored part's both sides): its share of the length and the offset."""
    pts = np.array(pts, float)
    seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    cum = np.concatenate([[0], np.cumsum(seg)]); L = cum[-1]
    s = np.linspace(0, L, n)
    line = np.stack([np.interp(s, cum, pts[:, i]) for i in range(3)], axis=1)
    Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
    j = np.concatenate([((Q[a:a + 4000, None, :] - line[None]) ** 2).sum(2).argmin(1) for a in range(0, len(Q), 4000)])
    return s[j] / L, Q - line[j]


# The head: wide across the temples, narrowing to the chin; a small white muzzle and a slim neck
def wedge(V):
    V = V.copy()
    V[:, 0] *= 1 + 0.12 * V[:, 2] - 0.1 * np.clip(-V[:, 2], 0, 1)
    return V
k.add("head", ell(H, HR, deform=wedge), GREY, "head")
k.add("muzzle", ell((0, -0.232, 0.38), (0.05, 0.04, 0.036)), GREY, "head")
k.add("neck", tube([(0, -0.08, 0.3), (0, -0.12, 0.34), (0, -0.15, 0.37)], [0.047, 0.042, 0.045]), GREY, "head")

# A slender body, a deep chest tapering to a narrow waist and a round rump, one smooth line
k.add("chest", ell((0, -0.055, 0.285), (0.068, 0.09, 0.078)), GREY, "body")
k.add("waist", ell((0, 0.05, 0.28), (0.06, 0.11, 0.066)), GREY, "body")
k.add("rump", ell((0, 0.115, 0.275), (0.066, 0.07, 0.07)), GREY, "body")

# Thin legs; the hind ones with full thighs; white toes on every paw
k.add("leg", tube([(0.045, -0.085, 0.27), (0.052, -0.095, 0.15), (0.055, -0.1, 0.04)], [0.03, 0.022, 0.019]),
      GREY, "leg{s}", mirror=True)
k.add("paw", ell((0.055, -0.112, 0.02), (0.025, 0.034, 0.02)), GREY, "leg{s}", mirror=True)
k.add("thigh", ell((0.05, 0.13, 0.245), (0.034, 0.058, 0.068)), GREY, "hind{s}", mirror=True)
k.add("hind", tube([(0.056, 0.15, 0.22), (0.06, 0.16, 0.12), (0.06, 0.13, 0.04)], [0.028, 0.02, 0.019]),
      GREY, "hind{s}", mirror=True)
k.add("hpaw", ell((0.06, 0.115, 0.02), (0.025, 0.034, 0.02)), GREY, "hind{s}", mirror=True)
def toes(P):
    front = (P[:, 1] < -0.105) * (P[:, 1] > -0.16)
    back = (P[:, 1] < 0.105) * (P[:, 1] > 0.07)
    return inside(P[:, 2] - 0.03, 0.0025) * (front + back) * inside(np.abs(np.abs(P[:, 0]) - 0.057) - 0.035, 0.003)
k.paint(toes, WHITE)

# Spiky tufts: a ruff on the chest pointing up and out, and on the haunches pointing back
for i, (a, b, r) in enumerate([((0.0, -0.11, 0.27), (0.0, -0.16, 0.33), 0.03),
                               ((0.03, -0.11, 0.26), (0.065, -0.15, 0.31), 0.028),
                               ((0.04, -0.1, 0.235), (0.08, -0.13, 0.255), 0.024)]):
    k.add(f"ruff{i}", blade([a, b], [r, 0.004], (0, 0.6, -1) if i == 0 else (1, 0.5, -0.6), 0.7), GREY, "body",
          mirror=i > 0)
for i, (a, b, r) in enumerate([((0.06, 0.13, 0.27), (0.1, 0.2, 0.305), 0.026),
                               ((0.065, 0.13, 0.225), (0.105, 0.19, 0.23), 0.024)]):
    k.add(f"tuft{i}", blade([a, b], [r, 0.004], (0, 0, 1), 0.65), GREY, "hind{s}", mirror=True)
# A long back from the shoulders to the rump, so the line of the body runs smooth
k.add("back", ell((0, 0.03, 0.3), (0.05, 0.16, 0.05)), GREY, "body")

# Tall crescent ears from the top corners of the head, flaring out sideways, lined pink, their points white
EAR = [(0.06, -0.15, 0.47), (0.095, -0.152, 0.52), (0.14, -0.148, 0.56), (0.195, -0.14, 0.585)]
k.add("ear", blade(EAR, [0.05, 0.044, 0.028, 0.004], (0, 1, 0), 0.35), GREY, "ear{s}", mirror=True)
def lining(P):
    t, off = along(P, EAR)
    half = 0.032 * np.sin(np.pi * np.clip((t - 0.02) / 0.82, 0, 1)) ** 0.6
    side = off[:, 0] + 0.008 * (1 - t)
    return (inside(np.abs(side) - half, 0.003) * inside(off[:, 1] + 0.004, 0.003) * inside(t - 0.8, 0.01)
            * (P[:, 2] > 0.465))
def tip(P):
    t, off = along(P, EAR)
    return inside(0.8 - t, 0.006) * (np.linalg.norm(off, axis=1) < 0.05) * (P[:, 2] > 0.5)
k.paint(tip, WHITE, on=["ear"])
k.paint(lining, PINK, on=["ear"])

# The white of the lower face: round the muzzle and up between the eyes, out under them over the cheeks
R = 0.118
def round_head(P):
    return np.arctan2(P[:, 0], -(P[:, 1] - H[1])) * R, P[:, 2]
FACE = mirrored([(0.0, 0.418), (0.024, 0.414), (0.05, 0.395), (0.08, 0.388), (0.105, 0.37), (0.1, 0.345),
                 (0.07, 0.33), (0.035, 0.322), (0.0, 0.32)])
FRONT = lambda P: (P[:, 1] < H[1] - 0.01) * (P[:, 2] > 0.3)
k.paint(lambda P: poly_mask(*round_head(P), FACE, edge=0.0025) * FRONT(P), WHITE)

# Long dark whiskers, three a side, fanning out from the muzzle's sides
for i, (end, mid) in enumerate([((0.22, -0.2, 0.41), (0.13, -0.235, 0.4)),
                                ((0.23, -0.21, 0.38), (0.13, -0.24, 0.382)),
                                ((0.21, -0.22, 0.35), (0.12, -0.245, 0.365))]):
    k.add(f"whisker{i}", tube([(0.04, -0.258, 0.38 - 0.008 * i), mid, end], [0.0028, 0.0024, 0.0016], seg=6, per=6),
          INK, "head", fuse=False, mirror=True, surface="hair")

# The long tail: back and up from the rump, then wound twice round like a spring behind the body, out to the side,
# and up to a white puff of fur
C = np.array([-0.05, 0.33])
pts = [(0, 0.16, 0.29), (-0.01, 0.23, 0.28), (-0.03, 0.27, 0.29)]
for j in range(1, 17):
    th = -math.pi / 2 + j * (2 * math.pi / 8)
    z = 0.3 + 0.17 * j / 16
    pts.append((C[0] + 0.095 * math.cos(th), C[1] + 0.075 * math.sin(th), z))
pts += [(-0.06, 0.27, 0.5), (-0.07, 0.28, 0.53)]
rad = np.concatenate([[0.022, 0.019], np.linspace(0.016, 0.013, len(pts) - 2)])
k.add("tail", tube(pts, rad, seg=12), GREY, "tail")
T = np.array((-0.075, 0.29, 0.56))
k.add("puff", ell(T, (0.045, 0.04, 0.038)), WHITE, "tail")
g = math.pi * (3 - math.sqrt(5))
for i in range(9):
    z = 1 - 2 * (i + 0.5) / 9
    rr = math.sqrt(1 - z * z)
    d = np.array([rr * math.cos(g * i), rr * math.sin(g * i) * 0.9, z * 0.8])
    k.add(f"puff{i}", ell(T + d * 0.034, (0.027, 0.026, 0.025)), WHITE, "tail")

# A small mauve nose and a little cat's mouth on the white
k.decal((0, -0.272, 0.395), unit(0, -1, 0.15), (0.009, 0.0065), "#B86A88", eps=0.003,
        outline=polygon([(-1, 0.7), (1, 0.7), (0, -1)]), rings=6, segs=24)
CW, CI = srgb(WHITE), srgb(INK)
def mouth(u, v):
    c = np.tile(CW, (len(u), 1))
    c[(np.abs(v - (-0.25 + 1.8 * (np.abs(u) - 0.45) ** 2)) < 0.15) & (np.abs(u) < 0.85)] = CI
    c[(np.abs(u) < 0.08) & (v > -0.05)] = CI
    return c
k.decal((0, -0.27, 0.375), unit(0, -1, -0.15), (0.02, 0.014), mouth, eps=0.003, rings=10, segs=48)

# The eyes, half shut and haughty: a pink lid over the top half with a dark line along its edge sloping a little
# down toward the nose; below it yellow with a blue iris looking inward, a dark pupil and a glint
CL, CY, CB, CBD, CP = srgb(LID), srgb(YELLOW), srgb(BLUE), srgb(BLUED), srgb("#14142A")
def lid(u):
    return -0.12 + 0.2 * (1 - u * u) + 0.06 * u          # arched and low: calm, half shut
def eye(u, v):
    c = np.tile(CL, (len(u), 1))
    below = v < lid(u)
    c[below] = CY
    ir = np.hypot((u - 0.12) / 0.42, (v + 0.3) / 0.62)
    c[below & (ir < 1)] = CBD
    c[below & (ir < 0.8)] = CB
    c[below & (np.hypot((u - 0.12) / 0.13, (v + 0.3) / 0.3) < 1)] = CP
    c[below & (np.hypot((u - 0.28) / 0.1, (v + 0.15) / 0.12) < 1)] = (1, 1, 1)
    c[np.abs(v - lid(u) - 0.03) < 0.085] = CI
    c[(u * u + v * v > 0.84) & (v < lid(u))] = CI                            # a thin rim below
    return c
de = unit(0.45, -0.88, 0.1)
E = H + de * 0.1 + np.array([0, 0, 0.002])
def eyes(state):
    k.decal(E, de, (0.034, 0.025), eye_look(state, eye, CL, CI), eps=0.004, rings=20, segs=56, mirror=True,
            up=unit(-0.15, 0, 1))


k.build(voxel=0.0055)
k.eye_states(eyes)
k.export(out)
