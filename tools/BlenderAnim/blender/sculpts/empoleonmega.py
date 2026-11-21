# Mega Empoleon (a Mega of our own, plan 06 · R20b) on the skeleton and clips of the game's own export of the form:
#   blender --background --factory-startup --python sculpts/empoleonmega.py -- <Empoleon-Mega.glb> <out.glb>
# No game has it, so the game's own sculpt (PokemonModels.Ours.Megas.cs, EmpoleonMega) is the design, grown here from
# Empoleon's Blender sculpt (empoleon.py, copied: it builds as it is read): its golden trident risen into a tall steel
# crown of three prongs on a gold band over the brow, the middle prong tallest; the blades of its flippers grown long
# and their blue rims steel; a cape of deep navy plumage falling from its shoulders to the ground behind it, its hem
# feathered into points; and gold epaulettes with a fringe where the cape hangs from. Everything else is Empoleon's:
# the heavy navy body, the blue collar, the white shirt with its lace hem and lapels, the golden feet, the fierce eyes.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, tri, unit, boxy, eye_look, polygon, args, hull, outward
import sculptkit

src, out = args()
k = Kit(src, coat="scales")

NAVY, BLUE, WHITE, GOLD = "#2B3550", "#3A8FE0", "#F4F6F8", "#F3C838"
STEEL, STEEL2, CAPE, CAPE2 = "#8EA4C6", "#5E7398", "#22305F", "#1A2650"      # the Mega's
H = np.array([0, -0.02, 1.1])           # the head's centre
HR = 0.155


def blade(pts, radii, axis, f):
    """A tube squashed along an axis about its own line: a blade, its middle where the points are."""
    V, F = tube(pts, radii)
    a = np.array(axis, float); a /= np.linalg.norm(a)
    p = np.array(pts, float)
    line = np.vstack([p[i] + (p[i + 1] - p[i]) * t for i in range(len(p) - 1) for t in np.linspace(0, 1, 40)])
    near = line[np.argmin(((V[:, None, :] - line[None]) ** 2).sum(2), axis=1)]
    d = (V - near) @ a
    return V - np.outer(d * (1 - f), a), F


def sheet(at, nu, nv, thick):
    """A thick sheet: at(u, v) gives the middle surface's points for u, v in 0..1 (arrays nu x nv), closed round."""
    u, v = np.meshgrid(np.linspace(0, 1, nu), np.linspace(0, 1, nv), indexing="ij")
    P = at(u, v)
    du = np.gradient(P, axis=0); dv = np.gradient(P, axis=1)
    N = np.cross(du, dv); N /= np.linalg.norm(N, axis=2, keepdims=True)
    A, B = (P + N * thick / 2).reshape(-1, 3), (P - N * thick / 2).reshape(-1, 3)
    V = np.vstack([A, B]); o = len(A)
    def i(a, b):
        return a * nv + b
    F = []
    for a in range(nu - 1):
        for b in range(nv - 1):
            F.append([i(a, b), i(a + 1, b), i(a + 1, b + 1), i(a, b + 1)])
            F.append([o + i(a, b), o + i(a, b + 1), o + i(a + 1, b + 1), o + i(a + 1, b)])
    for a in range(nu - 1):            # the edges along u
        for b in (0, nv - 1):
            q = [i(a, b), i(a + 1, b), o + i(a + 1, b), o + i(a, b)]
            F.append(q if b else q[::-1])
    for b in range(nv - 1):            # and along v
        for a in (0, nu - 1):
            q = [i(a, b), i(a, b + 1), o + i(a, b + 1), o + i(a, b)]
            F.append(q[::-1] if a else q)
    return V, F


def slab(outline, origin, ex, ez, thick_rim, thick_mid=None):
    """
    A flat plate with corners, left loose so no remesh rounds them: outline points (a, b) in the plane of ex, ez about
    origin, star-shaped about their mean; thick_rim at the edge, thick_mid at the middle (a low ridge, a blade's face).
    """
    ex, ez = unit(*ex), unit(*ez)
    n = np.cross(ex, ez); n /= np.linalg.norm(n)
    pts = np.array(outline, float)
    mid = pts.mean(axis=0)
    tm = thick_rim if thick_mid is None else thick_mid
    o = np.array(origin, float)
    def at(p, t):
        return o + ex * p[0] + ez * p[1] + n * t
    m = len(pts)
    V = [at(mid, tm / 2), at(mid, -tm / 2)]
    V += [at(p, thick_rim / 2) for p in pts] + [at(p, -thick_rim / 2) for p in pts]
    F = []
    for i in range(m):
        j = (i + 1) % m
        F.append([0, 2 + i, 2 + j])                      # the front fan
        F.append([1, 2 + m + j, 2 + m + i])              # the back fan
        F.append([2 + i, 2 + m + i, 2 + m + j, 2 + j])   # the rim
    # Faces must turn outward: the fan's turn decides
    a, b = pts[0] - mid, pts[1] - mid
    if a[0] * b[1] - a[1] * b[0] < 0:
        F = [f[::-1] for f in F]
    return np.array(V), F


# A tall, heavy body, wide at the hips, the small head sunk into its collar
def pear(V):
    V = V.copy()
    s = 1 + 0.08 * np.clip(-V[:, 2], 0, 1) - 0.16 * np.clip(V[:, 2], 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("body", ell((0, 0.02, 0.56), (0.28, 0.25, 0.48), deform=pear), NAVY, "body")
k.add("head", ell(H, (0.155, 0.15, 0.16)), NAVY, "head")
k.add("tail", ell((0, 0.25, 0.22), (0.09, 0.035, 0.12), rot=(30, 0, 0)), NAVY, "body")

# Short legs with a ragged fringe of feathers over the feet
k.add("leg", tube([(0.13, -0.01, 0.22), (0.14, -0.03, 0.07)], [0.08, 0.065]), NAVY, "leg{s}", mirror=True)
for i, a in enumerate((-80, -30, 20, 70, 130)):
    r = math.radians(a)
    d = np.array([math.sin(r), -math.cos(r), 0])
    base = np.array([0.14, -0.03, 0.11])
    k.add(f"fringe{i}", tube([base + d * 0.04, base + d * 0.085 + [0, 0, -0.06]], [0.03, 0.006]), NAVY, "leg{s}",
          mirror=True)

# Golden feet: a pad and three clawed toes
k.add("pad", ell((0.14, -0.05, 0.03), (0.075, 0.075, 0.03)), GOLD, "leg{s}", fuse=False, mirror=True, surface="metal")
for i, a in enumerate((-38, 0, 38)):
    r = math.radians(a)
    tip = (0.14 + 0.14 * math.sin(r), -0.05 - 0.14 * math.cos(r), 0.012)
    k.add(f"toe{i}", tube([(0.14, -0.05, 0.03), tip], [0.034, 0.007]), GOLD, "leg{s}", fuse=False, mirror=True,
          surface="metal")

# The collar: a stiff blue funnel open at the front, rising to a tall point on each side of the face
def collar(u, v):
    phi = np.radians(-138 + 276 * u)                 # 0 at the back
    a = np.abs(phi) / np.radians(138)
    z0 = 0.88 + 0.2 * a ** 6                         # the front edges slope up to a point
    z = z0 + v * (0.12 + 0.24 * a ** 3 - 0.2 * a ** 6)
    r = 0.165 + 0.05 * v + 0.07 * a
    return np.stack([r * np.sin(phi), 0.02 + r * np.cos(phi), z], axis=-1)
k.add("collar", sheet(collar, 64, 12, 0.024), BLUE, "body")

# The flippers: flat steel blades with corners, left loose so nothing rounds them: blue all round the edge with three
# points (one rising at the shoulder, one out from the outer edge, the blade's own tip below), dark over the face
WO = np.array([0.27, 0.0, 0.82])                     # the blade's root, beside the wing's joint
EX, EZ = (0.95, 0.3, 0.0), (0.0, 0.0, 1.0)            # outward along the blade's face, and up it; the face looks ahead
EDGE = [(-0.03, 0.04), (0.05, 0.075), (0.125, 0.2), (0.155, 0.05), (0.185, -0.16), (0.3, -0.215), (0.185, -0.275),
        (0.115, -0.53), (0.02, -0.41), (-0.045, -0.15)]
FACE = [(-0.025, 0.035), (0.05, 0.06), (0.105, 0.105), (0.125, 0.01), (0.145, -0.17), (0.14, -0.27), (0.096, -0.44),
        (0.02, -0.375), (-0.04, -0.15)]
def held(pts, turn=math.radians(12), grow=1.1):
    """The blade a little larger and turned so that its tip swings out from the body."""
    cs, sn = math.cos(turn), math.sin(turn)
    return [(grow * (a * cs - b * sn), grow * (a * sn + b * cs) * LONG) for a, b in pts]
LONG = 1.3                                           # the Mega's blades, grown long down their length
k.add("wing", slab(held(EDGE), WO, EX, EZ, 0.018), STEEL, "wing{s}", fuse=False, mirror=True, surface="metal")
k.add("wingface", slab(held(FACE), WO, EX, EZ, 0.026, 0.05), NAVY, "wing{s}", fuse=False, mirror=True,
      surface="metal")
# Three golden claws lying down the lower face
ex_, ez_ = unit(*EX), unit(*EZ)
nf = np.cross(ex_, ez_)
for i, (a, b) in enumerate(held(((0.055, -0.27), (0.085, -0.3), (0.06, -0.335)))):
    c = WO + ex_ * a + ez_ * b + nf * 0.022
    k.add(f"claw{i}", tube([c, c + ex_ * 0.006 - ez_ * 0.05 + nf * 0.006], [0.015, 0.003]), GOLD, "wing{s}",
          fuse=False, mirror=True, surface="metal")

# The front, every edge a patch so it stays crisp: a white shirt from the collar down to a scalloped hem of lace
# with a dark hole in each scallop, its collar's points turned out; a blue lapel down each side of it past the hem
SC = np.array([0.0, -0.2, 0.7])
def hem():
    xs = np.linspace(0.21, -0.21, 57)
    return [(x, 0.47 - 0.026 * abs(math.sin(math.pi * (x - 0.21) / 0.06))) for x in xs]
SHIRT = [(0.0, 0.965), (0.06, 0.96), (0.165, 0.89), (0.12, 0.82), (0.15, 0.68), (0.2, 0.55)] + hem() + \
        [(-0.2, 0.55), (-0.15, 0.68), (-0.12, 0.82), (-0.165, 0.89), (-0.06, 0.96)]
k.decal(SC, (0, -1, 0), (1, 1), WHITE, eps=0.003, rings=40, segs=240,
        outline=polygon([(x - SC[0], z - SC[2]) for x, z in SHIRT]))
for x in (-0.18, -0.12, -0.06, 0.0, 0.06, 0.12, 0.18):
    k.decal((x, -0.2, 0.478), (0, -1, 0), (0.0125, 0.0125), NAVY, eps=0.005, rings=4, segs=24)
LAPEL = [[(0.1, 0.885), (0.168, 0.945), (0.218, 0.7), (0.168, 0.655)],
         [(0.163, 0.672), (0.217, 0.715), (0.25, 0.5), (0.242, 0.38), (0.2, 0.28), (0.193, 0.45)]]
for side in (1, -1):
    for piece in LAPEL:
        pts = [(x * side, z) for x, z in piece]
        mid = np.mean(pts, axis=0)
        k.decal((mid[0], -0.2, mid[1]), (0, -1, 0), (1, 1), BLUE, eps=0.006, rings=12, segs=120,
                outline=polygon([(x - mid[0], z - mid[1]) for x, z in pts]))
    # the shirt's collar point, turned out over the lapel
    pts = [(x * side, z) for x, z in ((0.05, 0.958), (0.178, 0.885), (0.118, 0.812), (0.062, 0.86))]
    mid = np.mean(pts, axis=0)
    k.decal((mid[0], -0.2, mid[1]), (0, -1, 0), (1, 1), WHITE, eps=0.008, rings=8, segs=96,
            outline=polygon([(x - mid[0], z - mid[1]) for x, z in pts]))

# The face: blue under the crown and round the eyes
dm = unit(0, -0.95, 0.1)
k.decal(H + dm * HR, dm, (0.13, 0.08), BLUE, eps=0.003, rings=12, segs=48,
        outline=lambda th: 1 - 0.25 * max(0.0, -math.sin(th)) ** 2)

# The golden beak
k.add("beak", tube([(0, -0.16, 1.08), (0, -0.21, 1.06), (0, -0.235, 1.02)], [0.04, 0.028, 0.006]), GOLD, "head",
      fuse=False, surface="metal")
# The trident's shaft up the brow, gold, to the Mega's crown: a band of gold round the brow and three prongs of steel
# standing on it, the middle one tallest, the outer two leaning out; a short gold spike between each pair
k.add("shaft", blade([(0, -0.17, 1.07), (0, -0.168, 1.16), (0, -0.15, 1.22)],
                     [0.032, 0.03, 0.028], (0, 1, 0.3), 0.55), GOLD, "head", fuse=False, surface="metal")
def ring(c, rx, ry, h, tilt, n=28):
    """A band round the head: a hull of two tilted ellipses of points (convex, so the hull is the band)."""
    pts = []
    for j in range(n):
        t = 2 * math.pi * j / n
        for dz in (-h / 2, h / 2):
            x, y = rx * math.sin(t), -ry * math.cos(t)
            pts.append(np.array(c) + (x, y, dz + tilt * (-y)))
    return hull(pts)
k.add("band", ring((0, -0.015, 1.21), 0.13, 0.132, 0.05, 0.35), GOLD, "head", fuse=False, surface="metal")
def prong(base, tip, w, d):
    """A steel prong: a kite-sectioned hull from a base w across and d deep to a point."""
    base, tip = np.array(base, float), np.array(tip, float)
    a = unit(*(tip - base))
    side = np.cross(a, (0, 1.0, 0)); side /= np.linalg.norm(side)
    fwd = np.cross(side, a)
    mid = base + (tip - base) * 0.3
    return hull([base + side * w, base - side * w, base + fwd * d, base - fwd * d,
                 mid + side * w * 1.15, mid - side * w * 1.15, mid + fwd * d * 0.8, mid - fwd * d * 0.8, tip])
k.add("prongc", prong((0, -0.1, 1.2), (0, -0.07, 1.6), 0.05, 0.032), STEEL2, "head", fuse=False, surface="metal")
for s, sd in ((1, "R"), (-1, "L")):
    k.add(f"prong{sd}", prong((0.085 * s, -0.08, 1.2), (0.19 * s, -0.04, 1.49), 0.042, 0.028), STEEL2, "head",
          fuse=False, surface="metal")
    k.add(f"spike{sd}", prong((0.045 * s, -0.12, 1.21), (0.07 * s, -0.11, 1.33), 0.024, 0.018), GOLD, "head",
          fuse=False, surface="metal")
# and its golden tail down the back of the head to the nape, over the collar
k.add("nape", blade([(0, 0.06, 1.24), (0, 0.15, 1.15), (0, 0.18, 1.06), (0, 0.19, 0.99)], [0.03, 0.06, 0.05, 0.008],
                    (0, 1, -0.2), 0.45), GOLD, "head", fuse=False, surface="metal")

# The Mega's cape: deep navy plumage hanging from the shoulders behind the body to the ground, widening as it falls,
# its hem cut into feathered points; a darker lining a little inside it, so its inner face shows from the sides
TEETH = 9
def cape_at(spread, rr, drop):
    def at(u, v):
        phi = np.radians(-spread + 2 * spread * u)               # 0 straight behind
        t = (u * TEETH) % 1.0
        point = 1 - np.abs(2 * t - 1)                          # 1 at a feather's tip, 0 between two
        bottom = 0.1 - 0.075 * point - drop
        z = 0.88 - v * (0.88 - bottom)
        r = rr[0] + (rr[1] - rr[0]) * v ** 0.8 + 0.012 * np.sin(phi * 14) * v
        return np.stack([r * np.sin(phi), 0.03 + r * np.cos(phi), z], axis=-1)
    return at
k.add("cape", sculptkit.outward(sheet(cape_at(68, (0.215, 0.37), 0.0), 91, 18, 0.024)), CAPE, "body", fuse=False,
      surface="cloth")
k.add("capelining", sculptkit.outward(sheet(cape_at(64, (0.2, 0.348), -0.03), 85, 16, 0.016)), CAPE2, "body",
      fuse=False, surface="cloth")
# Gold epaulettes where the cape hangs from: a low round dome on each shoulder over a wider brim of darker gold, the
# brim's edge cut into a fringe (tassels of their own read as fingers at the game's size)
def fringed(V):
    V = V.copy()
    th = np.arctan2(V[:, 1], V[:, 0])
    V[:, 2] -= 0.55 * np.clip(-V[:, 2], 0, 1) * (0.5 + 0.5 * tri(th * 14 / (2 * math.pi)))
    return V
for s, sd in ((1, "R"), (-1, "L")):
    EC = np.array([0.225 * s, 0.04, 0.9])
    k.add(f"epaulette{sd}", ell(EC + (0, 0, 0.012), (0.075, 0.085, 0.032), rot=(0, 20 * s, 0)), GOLD, "body",
          fuse=False, surface="metal")
    k.add(f"brim{sd}", ell(EC, (0.095, 0.1, 0.028), rot=(0, 20 * s, 0), deform=fringed), "#D2A426", "body",
          fuse=False, surface="metal")

# Small fierce eyes
LID, INK = srgb(BLUE), srgb("#141A30")
SCLERA, IRIS = srgb("#E4F4FF"), srgb("#2C6FC0")
def eye(u, v):
    c = np.tile(SCLERA, (len(u), 1))
    c[((u + 0.25) / 0.5) ** 2 + ((v + 0.1) / 0.75) ** 2 < 1] = IRIS
    c[((u + 0.27) / 0.28) ** 2 + ((v + 0.12) / 0.45) ** 2 < 1] = INK
    c[((u + 0.05) / 0.15) ** 2 + ((v - 0.25) / 0.15) ** 2 < 1] = (1, 1, 1)
    c[u * u + v * v > 0.8] = INK
    edge = 0.4 + 0.4 * u
    c[v > edge + 0.12] = LID
    c[np.abs(v - edge) <= 0.12] = INK
    return c
de = unit(0.5, -0.85, 0.12)
def eyes(state):
    k.decal(H + de * HR, de, (0.034, 0.036), eye_look(state, eye, LID, INK), eps=0.007, rings=20, segs=56, mirror=True)

k.build(voxel=0.012)
k.eye_states(eyes)
k.export(out)
