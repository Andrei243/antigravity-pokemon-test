# Purugly sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/purugly.py -- <the export's Purugly.glb> <out.glb>
# Glameow grown fat: a grey-blue tabby cat on four short legs, its body a great round barrel heavier below, white in
# front with a grey wedge come down from the collar, white tabby stripes on its flanks and legs, white paws. A wide
# head with a white lower face and a smug scowl: narrow yellow eyes under heavy purple lids sloping down to the nose, a
# small dark nose, a frown. Huge ears flaring out sideways, grey at the base with pink inside, their tops lilac and
# split into four fingers of fur. Black whiskers that run out straight and end in zigzags. Its twin tail: one prong
# belted round its waist, the other rising behind its left shoulder in coils, a white puff of fur at its root (the
# puff Glameow's tail ends in).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

GREY, WHITE, PINK, LILAC, LID, INK = "#8494B6", "#F4F5F9", "#EE9CBA", "#B48AD8", "#9A6CC4", "#22222E"
YELLOW, NOSE = "#F4D244", "#3A3446"
H = np.array([0, -0.08, 0.55])           # the head's centre
HR = np.array([0.148, 0.105, 0.112])


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


# --- the body: a barrel, heaviest low down, the chest narrower above it --------------------------------------------
k.add("belly", ell((0, 0.02, 0.215), (0.255, 0.23, 0.17)), GREY, "body")
k.add("chest", ell((0, -0.01, 0.355), (0.19, 0.17, 0.12)), GREY, "body")
k.add("neck", ell((0, -0.05, 0.46), (0.13, 0.11, 0.07)), GREY, "head")

# The head: wide, flat in front; pointed cheek fur jutting out at the sides; a small white muzzle
k.add("head", ell(H, HR), GREY, "head")
k.add("muzzle", ell((0, -0.158, 0.497), (0.055, 0.04, 0.034)), GREY, "head")
for i, (a, b, r) in enumerate([((0.12, -0.1, 0.52), (0.2, -0.1, 0.5), 0.035), ((0.11, -0.09, 0.485), (0.18, -0.08, 0.455),
                                                                              0.03)]):
    k.add(f"cheek{i}", blade([a, b], [r, 0.004], (0, 1, 0), 0.5), GREY, "head", mirror=True)

# Short thick legs, set wide, white paws
k.add("leg", tube([(0.13, -0.1, 0.14), (0.165, -0.125, 0.07), (0.18, -0.14, 0.058)], [0.06, 0.056, 0.05]), GREY,
      "leg{s}", mirror=True)
k.add("paw", ell((0.185, -0.158, 0.026), (0.056, 0.062, 0.028)), GREY, "leg{s}", mirror=True)
k.add("hind", tube([(0.13, 0.14, 0.14), (0.165, 0.16, 0.07), (0.18, 0.17, 0.058)], [0.062, 0.058, 0.052]), GREY,
      "hind{s}", mirror=True)
k.add("hpaw", ell((0.185, 0.155, 0.026), (0.056, 0.062, 0.028)), GREY, "hind{s}", mirror=True)
k.paint(lambda P: inside(P[:, 2] - 0.036, 0.003) * (np.abs(P[:, 0]) > 0.1), WHITE, on=["paw", "hpaw", "leg", "hind"])

# --- the ears: huge, flaring out sideways; a broad lilac top split into four fingers ---------------------------------
EB = np.array([0.085, -0.085, 0.615])           # the root, at the ear's joint
ED = unit(0.76, 0.02, 0.65)                     # out and up
EN = unit(0.0, -1, 0.0)                         # what the ear's face looks along
ES = np.cross(ED, EN); ES /= np.linalg.norm(ES)  # across the ear
EL = 0.2
k.add("ear", blade([EB - ED * 0.02, EB + ED * EL * 0.5, EB + ED * EL], [0.075, 0.085, 0.072], EN, 0.42), GREY,
      "ear{s}", mirror=True)
for i, (a, length) in enumerate(((-0.75, 0.11), (-0.25, 0.14), (0.25, 0.145), (0.75, 0.12))):
    base = EB + ED * EL * 0.82 + ES * a * 0.055
    d = unit(*(ED + ES * a * 0.35))
    k.add(f"earfinger{i}", blade([base, base + d * length * 0.55, base + d * length], [0.032, 0.026, 0.006], EN, 0.4),
          LILAC, "ear{s}", mirror=True)
# the ear's upper half lilac, in a soft line across it
def ear_top(P):
    Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
    return inside(EL * 0.52 - (Q - EB) @ ED, 0.006) * (P[:, 2] > 0.6)
k.paint(ear_top, LILAC, on=["ear"])

# --- the tail: from the root above the rump one prong belts the waist round the -X side, ending sunk in the flank
# before the front; the other rises behind the +X shoulder in loops (+X is on the viewer's right in the turntable's
# front view, where the pictures have the loops); a white puff at the root -------------------------------------------
BELT = [(0.0, 0.175, 0.335), (-0.1, 0.17, 0.345), (-0.175, 0.11, 0.355), (-0.2, 0.03, 0.36), (-0.19, -0.02, 0.362)]
k.add("belt", tube(BELT, [0.03, 0.03, 0.029, 0.025, 0.014], seg=14), GREY, "body", fuse=False)
COIL = [(0.02, 0.17, 0.34), (0.12, 0.17, 0.37), (0.19, 0.13, 0.4), (0.22, 0.1, 0.43)]
for j in range(1, 19):
    th = j * (2 * math.pi / 9)
    COIL.append((0.225 + 0.015 * j / 18, 0.1 + 0.045 * math.sin(th), 0.475 - 0.045 * math.cos(th) + 0.07 * j / 18))
k.add("coil", tube(COIL, list(np.linspace(0.03, 0.012, len(COIL))), seg=14), GREY, "body", fuse=False)
T = np.array((0.12, 0.17, 0.34))
g = math.pi * (3 - math.sqrt(5))
for i in range(11):
    z = 1 - 2 * (i + 0.5) / 11
    rr = math.sqrt(1 - z * z)
    d = np.array([rr * math.cos(g * i), rr * math.sin(g * i) * 0.8, z * 0.8])
    k.add(f"puff{i}", ell(T + d * 0.045, (0.035, 0.032, 0.032)), WHITE, "body")
k.add("puff", ell(T, (0.05, 0.04, 0.045)), WHITE, "body")

# --- markings --------------------------------------------------------------------------------------------------------
# The white front: a great oval from the collar to the belly's bottom
def front_white(P):
    x, y, z = P[:, 0], P[:, 1], P[:, 2]
    oval = inside(np.hypot(x / 0.205, (z - 0.24) / 0.205) - 1, 0.004)
    return oval * (y < -0.02)
k.paint(front_white, WHITE, on=["belly", "chest"])
# the grey wedge come down from the collar: a point each side and one in the middle
WEDGE = [(-0.115, 0.45), (0.115, 0.45), (0.105, 0.39), (0.135, 0.36), (0.05, 0.37), (0.0, 0.325), (-0.05, 0.37),
         (-0.135, 0.36), (-0.105, 0.39)]
k.paint(lambda P: poly_mask(P[:, 0], P[:, 2], WEDGE, 0.003) * (P[:, 1] < -0.02), GREY, on=["belly", "chest"])
# white tabby stripes slanting across the flanks and the legs' outer sides
def stripes(P):
    x, y, z = np.abs(P[:, 0]), P[:, 1], P[:, 2]
    s = (z + 0.45 * y) / 0.11
    band = inside(np.abs(s - np.round(s)) - 0.1, 0.004)
    flank = (x > 0.17) * (z > 0.04) * (z < 0.27) * (y < 0.1)
    away = 1 - inside(np.hypot(P[:, 0] / 0.205, (z - 0.24) / 0.205) - 1.05, 0.004) * (y < 0)
    return band * flank * away
k.paint(stripes, WHITE, on=["belly", "leg", "hind"])

# The white lower face, round the muzzle and out under the eyes, the grey coming down to a point between them
R = 0.15
def round_head(P):
    return np.arctan2(P[:, 0], -(P[:, 1] - H[1])) * R, P[:, 2]
FACE = mirrored([(0.0, 0.512), (0.025, 0.527), (0.06, 0.533), (0.1, 0.536), (0.15, 0.532), (0.2, 0.5),
                 (0.175, 0.458), (0.11, 0.44), (0.0, 0.434)])
FRONT = lambda P: (P[:, 1] < H[1] + 0.01) * (P[:, 2] > 0.425)
k.paint(lambda P: poly_mask(*round_head(P), FACE, edge=0.0025) * FRONT(P), WHITE, on=["head", "muzzle", "cheek"])

# The ears' pink inside, low on the face of each
k.decal(EB + ED * 0.075 - EN * 0.02, EN, (0.04, 0.066), PINK, up=ED, eps=0.004, rings=12, segs=48, mirror=True,
        outline=polygon([(-1, -0.75), (1, -0.75), (0.15, 1)]))

# A small dark nose, a frown under it
k.decal((0, -0.195, 0.515), unit(0, -1, 0.2), (0.012, 0.009), NOSE, eps=0.003,
        outline=polygon([(-1, 0.7), (1, 0.7), (0, -1)]), rings=6, segs=24)
CW, CI = srgb(WHITE), srgb(INK)
def mouth(u, v):
    c = np.tile(CW, (len(u), 1))
    c[(np.abs(v - (0.3 - 0.9 * np.abs(u))) < 0.2) & (np.abs(u) < 0.85)] = CI
    return c
k.decal((0, -0.193, 0.483), unit(0, -1, -0.1), (0.034, 0.02), mouth, eps=0.003, rings=16, segs=72)

# Black whiskers: thin lines from the cheeks straight out, each ending in one small tight zigzag near its tip, about
# half a head-width past the cheek; each stroke its own straight piece
def stroke(name, a, b, r=0.0026):
    a, b = np.array(a, float), np.array(b, float)
    k.add(name, tube([a, (a + b) / 2, b], [r, r, r], seg=6, per=2), INK, "head", fuse=False, mirror=True, surface="hair")
for w, (start, bend, zig) in enumerate([((0.13, -0.135, 0.505), (0.225, -0.14, 0.512),
                                         [(0.237, 0.53), (0.25, 0.497), (0.263, 0.528)]),
                                        ((0.13, -0.135, 0.488), (0.22, -0.14, 0.472),
                                         [(0.232, 0.454), (0.245, 0.484), (0.258, 0.452)])]):
    stroke(f"whisker{w}a", start, bend)
    prev = np.array(bend)
    for j, (x, z) in enumerate(zig):
        nxt = np.array((x, -0.14, z))
        stroke(f"whisker{w}z{j}", prev, nxt)
        prev = nxt

k.build(voxel=0.008)

# --- the eyes: narrow and smug, yellow under a heavy purple lid whose dark edge slopes down toward the nose ---------
CL, CY, CP, CA = srgb(LID), srgb(YELLOW), srgb("#16141E"), srgb("#D8A82A")
def lid(u):
    return -0.1 + 0.42 * u - 0.1 * u * u          # low at the nose, rising outward: the scowl
def eye(u, v):
    c = np.tile(CL, (len(u), 1))
    below = v < lid(u)
    c[below] = CY
    c[below & (v < -0.55)] = CA
    c[below & (np.hypot((u - 0.05) / 0.12, (v + 0.2) / 0.5) < 1)] = CP
    c[np.abs(v - lid(u) - 0.02) < 0.13] = CI
    c[(u * u + v * v > 0.8) & (v < lid(u))] = CI
    return c
de = unit(0.5, -0.86, 0.05)
E = np.array([0.08, -0.155, 0.545])
def eyes(state):
    k.decal(E, de, (0.046, 0.03), eye_look(state, eye, CL, CI, line=0.12), eps=0.004, rings=20, segs=56, mirror=True,
            up=unit(-0.18, 0, 1))


k.eye_states(eyes)
k.export(out)
