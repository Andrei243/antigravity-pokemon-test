# Magmortar sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Magmar's script (magmar.py), which was grown from Magby's: the same red and yellow, the same eye with its solid
# pupil under a heavy lid, the same white cone claws and red feet, flame marks painted the same way round the body,
# and Ponyta's ragged tongues of flame gathered a bone and a colour to a piece.
#   blender --background --factory-startup --python sculpts/magmortar.py -- <the export's Magmortar.glb> <out.glb>
#
# The design, from the references: a great round body, red with wavy yellow bands of flame round it; pink thighs,
# dark grey shins, red feet with white claws; a small red head sunk between two great red shoulder puffs, each
# crowned with flames; a crest of flame on the head, half yellow and half red; a pink muzzle of thick lips under
# narrow eyes; a wide dark grey collar; arms that are cannons, a dark grey sleeve and a yellow barrel flaring to a
# rim of five petals round a dark bore; a pink tail ending in a flame.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, tri, boxy, args

src, out = args()
k = Kit(src, coat="fur")

# The line's colours, Magby's unchanged, Magmar's flames, and Magmortar's own pink and grey
RED, YELLOW, BAND, CLAW, INK, MOUTH = "#EC4A44", "#F8D650", "#2B2528", "#F4F1E8", "#1C1416", "#A8384A"
ORANGE, DEEP, FLAMEY = "#FF7A22", "#E8442A", "#FFD23A"
PINK, GREY, PETAL, BORE, LIP = "#F2A8BA", "#4C4548", "#F2BE2E", "#221C1C", "#B85A72"

B = np.array([0, 0.0, 0.43])             # the body's middle
BR = np.array([0.215, 0.185, 0.2])
H = np.array([0, -0.085, 0.665])         # the head's middle, sunk forward between the shoulders
HR = np.array([0.078, 0.074, 0.074])


# --- Magby's helpers ---------------------------------------------------------------------------------------------
def volume(V, F):
    V = np.asarray(V, float)
    s = 0.0
    for f in F:
        for i in range(1, len(f) - 1):
            s += np.dot(V[f[0]], np.cross(V[f[i]], V[f[i + 1]]))
    return s / 6


def outward(name, shape):
    if volume(*shape) <= 0:
        print("KIT: !! piece", name, "is wound inside out")
    return shape


def plate(c, n, r, t):
    a_, b_ = (r, r) if np.isscalar(r) else r
    V, F = ell((0, 0, 0), (a_, b_, t), sub=3)
    n = unit(*n)
    a = np.cross(n, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a)
    b = np.cross(n, a)
    return V @ np.stack([a, b, n], axis=1).T + np.array(c, float), F


def ring(c, R, r, seg=40):
    a = np.linspace(0, 2 * math.pi, seg + 2) + math.pi / 2
    pts = [(c[0] + R * math.cos(t), c[1] + R * math.sin(t), c[2]) for t in a]
    return tube(pts, [r] * len(pts), seg=12, per=2)


def cone(base, d, length, r0, r1=0.0015):
    d = unit(*d)
    b = np.array(base, float)
    return tube([b, b + d * length * 0.5, b + d * length], [r0, r0 * 0.6, r1], seg=12, per=4)


def in_poly(x, y, pts):
    pts = np.array(pts, float)
    res = np.zeros(len(x), bool)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        res ^= ((y1 > y) != (y2 > y)) & (x < (x2 - x1) * (y - y1) / (y2 - y1 + 1e-12) + x1)
    return res


def poly_mask(x, y, pts, edge=0.0035):
    pts = np.array(pts, float)
    d = np.full(len(x), 9.0)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        ex, ey = x2 - x1, y2 - y1
        t = np.clip(((x - x1) * ex + (y - y1) * ey) / (ex * ex + ey * ey + 1e-12), 0, 1)
        d = np.minimum(d, np.hypot(x - x1 - t * ex, y - y1 - t * ey))
    return inside(np.where(in_poly(x, y, pts), -d, d), edge)


def round_about(P, c, r):
    return np.arctan2(P[:, 0] - c[0], -(P[:, 1] - c[1])) * r, P[:, 2]


def axes(d):
    """Two directions square to d and to each other: a frame round an axis, right-handed (a, b, d)."""
    d = unit(*d)
    a = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a)
    return a, np.cross(d, a)


def lathe(c, d, profile, seg=40):
    """
    A closed solid of turning: profile is (radius, distance along d) from the axis round to the axis again; the
    piece is wound outward whichever way the profile runs.
    """
    a, b = axes(d)
    d = unit(*d)
    c = np.array(c, float)
    V, F = [], []
    ang = np.linspace(0, 2 * math.pi, seg, endpoint=False)
    rows = []
    for r, s in profile:
        if r < 1e-9:
            rows.append([len(V)]); V.append(c + d * s)
        else:
            rows.append(list(range(len(V), len(V) + seg)))
            V.extend(c + d * s + r * (math.cos(t) * a + math.sin(t) * b) for t in ang)
    for r0, r1 in zip(rows, rows[1:]):
        for j in range(seg):
            if len(r0) == 1:
                F.append([r0[0], r1[(j + 1) % seg], r1[j]])
            elif len(r1) == 1:
                F.append([r0[j], r0[(j + 1) % seg], r1[0]])
            else:
                F.append([r0[j], r0[(j + 1) % seg], r1[(j + 1) % seg], r1[j]])
    V = np.array(V)
    if volume(V, F) < 0:
        F = [f[::-1] for f in F]
    return V, F


# --- Ponyta's flames, as Magmar has them ----------------------------------------------------------------------------
def fframe(d, side):
    d = np.array(d, float); d /= np.linalg.norm(d)
    s = np.array(side, float); s -= d * np.dot(s, d); s /= np.linalg.norm(s)
    return np.stack([s, np.cross(d, s), d], axis=1)
count = [0]
def tongue(base, d, length, width, side=(0, 1, 0), flick=0.35, thick=0.38, rag=0.24, waves=1.6, root=0.5, sub=None):
    count[0] += 1
    ph = 2.3 * count[0]
    def shape(V):
        V = V.copy()
        t = np.clip((V[:, 2] + 1) / 2, 0, 1)
        w = (1 - t ** 1.1) ** 1.15 * (root + (1.55 - root) * np.sqrt(t))
        s = np.where(V[:, 0] >= 0, 1.0, -1.0)
        edge = 1 + rag * np.sin(2 * math.pi * waves * t + ph + s * 2.1) * smoothstep(0.1, 0.35, t)
        V[:, 0] *= w * edge; V[:, 1] *= w
        V[:, 0] += flick * (2.1 * t ** 2.4 - 0.55 * np.sin(math.pi * t))
        return V
    V, F = ell((0, 0, 0), (width, width * thick, length / 2), deform=shape, sub=sub or (4 if length > 0.13 else 3))
    return V @ fframe(d, side).T + np.array(base, float) + fframe(d, side)[:, 2] * length / 2, F
FULL = 1.35
groups = {}
def gather(shape, color, bone, glow):
    groups.setdefault((bone, color, glow), []).append(shape)
def flame(base, d, length, width, bone, side=(0, 1, 0), flick=0.35, heart=True, color=ORANGE, glow="#FF7A2A",
          lick=True, thick=0.38):
    width = width * FULL
    b = np.array(base, float); dd = np.array(d, float) / np.linalg.norm(d)
    gather(tongue(b, dd, length, width, side, flick, thick=thick), color, bone, glow)
    if lick and length >= 0.09:
        su = fframe(dd, side)[:, 0]
        dl = dd - 0.75 * su; dl /= np.linalg.norm(dl)
        gather(tongue(b + dd * length * 0.32 - su * width * 0.35, dl, length * 0.42, width * 0.42, -su, flick * 0.8,
                      thick=thick, sub=3), color, bone, glow)
    if heart:
        ny = fframe(dd, side)[:, 1]
        for sg in (1, -1):
            gather(tongue(b + dd * 0.004 + ny * sg * width * thick * 0.6, dd, length * 0.6, width * 0.56, side,
                          flick * 0.5, thick=0.3, rag=0.16, root=0.2, sub=3), FLAMEY, bone, "#FFD860")
def back(base, d, length, width, bone, side, flick=0.45):
    flame(base, d, length, width, bone, side, flick, heart=False, color=DEEP, glow="#FF5A22")
def gather_all():
    for i, ((bone, color, glow), shapes) in enumerate(groups.items()):
        V, F, off = [], [], 0
        for sv, sf in shapes:
            V.append(sv); F.extend([[j + off for j in f] for f in sf]); off += len(sv)
        k.add(f"flame{i}", (np.vstack(V), F), color, bone, fuse=False, glow=glow)


# --- the great round body, red with wavy yellow bands of flame round it ---------------------------------------------
def ball(V):
    V = V.copy()
    s = 1 + 0.05 * np.clip(-V[:, 2], 0, 1) - 0.06 * np.clip(V[:, 2], 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("body", ell(B, BR, deform=ball), RED, "body")
k.add("neck", ell((0, -0.05, 0.605), (0.08, 0.072, 0.045)), RED, "head")
k.add("collar", outward("collar", ring((0, -0.05, 0.61), 0.078, 0.022)), GREY, "body", fuse=False,
      surface="leather")


def bands(P):
    """Two bands of flame round the body, each edge a wave in arc length round its axis."""
    s = np.arctan2(P[:, 0] - B[0], -(P[:, 1] - B[1])) * BR[0]
    z = P[:, 2]
    def tongues(x, ph):
        """Flame tongues of uneven sizes: rounded humps drawn to points, with a smaller wave over them."""
        return np.maximum(np.sin(x + ph), 0) ** 1.6 + 0.4 * np.maximum(np.sin(2.3 * x + 2 * ph), 0) ** 1.8
    up1 = 0.6
    lo1 = 0.565 - 0.075 * tongues(s / 0.05, 0.4)                        # the upper band's tongues licking down
    lo2 = 0.345 + 0.012 * np.sin(s / 0.05)
    up2 = 0.37 + 0.085 * tongues(s / 0.055, 1.7)                       # the lower band's tongues licking up
    e = 0.0035
    return np.maximum(inside(z - up1, e) * inside(lo1 - z, e), inside(z - up2, e) * inside(lo2 - z, e)) * \
        (np.abs(P[:, 0]) < 0.24)
k.paint(lambda P: bands(P) * (P[:, 2] > 0.3) * (P[:, 2] < 0.6) * (np.hypot(P[:, 0], P[:, 1] - B[1]) > 0.1), YELLOW)

# Two great red shoulder puffs either side of the head, each crowned with flames
PUFF = np.array([0.185, 0.01, 0.6])
k.add("puff", ell(PUFF, (0.105, 0.1, 0.095)), RED, "body", mirror=True)
for s in (1, -1):
    top = PUFF * (s, 1, 1) + (0.01 * s, 0.01, 0.08)
    back(top + (0, 0.01, -0.01), (0.25 * s, 0.25, 1), 0.17, 0.06, "body", (s, 0.4, 0), flick=0.4)
    flame(top, (0.35 * s, 0.1, 1), 0.15, 0.05, "body", side=(s, 0.4, 0), flick=0.4, color=DEEP, glow="#FF5A22")
    flame(top + (0.04 * s, 0.0, -0.02), (0.8 * s, 0.2, 0.6), 0.11, 0.04, "body", side=(s, 0.4, 0), flick=0.35)

# --- legs: pink thighs, dark grey shins, Magmar's red feet with three white claws ----------------------------------
THIGH = np.array([0.135, -0.04, 0.25])
k.add("thigh", ell(THIGH, (0.1, 0.105, 0.095)), PINK, "leg{s}", mirror=True)
k.add("shin", tube([(0.13, -0.03, 0.18), (0.135, -0.035, 0.1), (0.137, -0.04, 0.05)], [0.052, 0.046, 0.044]), GREY,
      "leg{s}", mirror=True)
FOOT = np.array([0.14, -0.07, 0.034])
k.add("foot", ell(FOOT, (0.066, 0.095, 0.034)), RED, "leg{s}", mirror=True)
for i, a in enumerate((-30, 0, 30)):
    d = unit(math.sin(math.radians(a)), -math.cos(math.radians(a)), -0.12)
    k.add(f"toe{i}", cone(FOOT + d * 0.07 + (0, 0, -0.006), d, 0.048, 0.016), CLAW, "leg{s}", fuse=False,
          mirror=True, surface="shell")
# the colours meet crisply where thigh, shin and body join
k.paint(lambda P: inside(P[:, 2] - 0.172, 0.0035) * (P[:, 2] > 0.08) * (np.abs(P[:, 0]) > 0.07), GREY,
        on=["shin", "thigh"])

# --- the cannons: a dark grey sleeve out of the shoulder, a yellow barrel flaring to five petals round a dark bore --
CB = np.array([0.2, -0.07, 0.53])        # the barrel's root, inside the shoulder puff
CD = unit(0.22, -1, -0.3)                # forward, a little out and down, so that both show from the front
CL = 0.26                                 # the barrel's length
R0, R1, RI, DEPTH = 0.06, 0.075, 0.05, 0.03
k.add("sleeve", outward("sleeve", lathe(CB, CD, [(0, 0.0), (0.068, 0.0), (0.072, 0.03), (0.072, 0.075),
                                                 (0.066, 0.1), (0, 0.1)])), GREY, "arm{s}", fuse=False,
      mirror=True, surface="metal")
k.add("barrel", outward("barrel", lathe(CB, CD, [(0, 0.08), (R0, 0.08), (R0, 0.15), (R1, CL - 0.01), (R1, CL),
                                                 (RI, CL), (RI, CL - DEPTH), (0, CL - DEPTH)])), YELLOW,
      "arm{s}", fuse=False, mirror=True, surface="metal")
k.add("bore", outward("bore", plate(CB + CD * (CL - DEPTH + 0.003), CD, RI * 0.99, 0.004)), BORE, "arm{s}",
      fuse=False, mirror=True, surface="metal")
ca, cb = axes(CD)
for i in range(5):
    t = 2 * math.pi * (i + 0.25) / 5
    o = ca * math.cos(t) + cb * math.sin(t)
    tg = np.cross(CD, o)                                  # along the rim
    dd = unit(*(CD * 0.9 + o * 0.45))                     # flaring forward and out from the barrel's end
    nn = np.cross(tg, dd)                                 # the petal's face: (tg, dd, nn) right-handed
    def squared(V):
        V = boxy(3.5)(V); t = np.clip((V[:, 1] + 1) / 2, 0, 1)
        V[:, 0] *= 0.85 + 0.2 * t                         # a little wider at its free end, a squared flap
        V[:, 2] -= 0.9 * V[:, 0] ** 2                     # curved round the barrel's axis
        return V
    PV, PF = ell((0, 0, 0), (0.042, 0.056, 0.016), deform=squared, sub=3)
    PV = PV @ np.stack([tg, dd, nn], axis=1).T + CB + CD * (CL - 0.06) + o * (R1 + 0.002) + dd * 0.045
    k.add(f"petal{i}", outward("petal", (PV, PF)), PETAL,
          "arm{s}", fuse=False, mirror=True, surface="metal")

# --- the tail: pink, thick, back and out to +X, ending in a flame ---------------------------------------------------
TAIL = [(0.0, 0.14, 0.32), (0.04, 0.26, 0.24), (0.11, 0.35, 0.22), (0.16, 0.4, 0.28)]
k.add("tail", tube(TAIL, [0.075, 0.06, 0.045, 0.03]), PINK, "tail")
TIP = np.array(TAIL[-1])
back(TIP + (0, 0.01, 0.0), (0.45, 0.35, 1), 0.2, 0.065, "tail", (0, 1, 0), flick=0.5)
for d, L, w in [((0.35, 0.2, 1), 0.17, 0.055), ((0.7, 0.4, 0.6), 0.13, 0.045)]:
    flame(TIP, d, L, w, "tail", side=(0.5, 1, 0), flick=0.45, color=DEEP, glow="#FF5A22")

# --- the head: small and sunk between the shoulders, a pink muzzle, a crest of flame half yellow, half red ---------
k.add("head", ell(H, HR), RED, "head")
k.add("brow", ell(H + (0, -0.02, 0.035), (0.07, 0.06, 0.045)), RED, "head")
MUZ = np.array([0, -0.14, 0.632])
k.add("muzzle", ell(MUZ, (0.055, 0.035, 0.04)), PINK, "head")
# the face pink below the eyes, crisp under them
k.paint(lambda P: inside(P[:, 2] - (0.655 + 0.12 * np.abs(P[:, 0])), 0.0035) * (P[:, 1] < H[1] - 0.02)
        * (np.linalg.norm(P - H, axis=1) < 0.13) * (P[:, 2] > 0.6), PINK)
# the crest: a red tongue and a yellow one beside it, swept back, with volume so it reads from the front
CREST = H + (0, 0.01, 0.06)
for s, col, glow, L in ((1, DEEP, "#FF5A22", 0.17), (-1, FLAMEY, "#FFD860", 0.15)):
    gather(tongue(CREST + (0.006 * s, 0, 0), unit(0.03 * s, 0.5, 1), L, 0.032 * FULL, side=(0, 1, 0), flick=0.45,
                  thick=0.9), col, "head", glow)

# The thick lips: a dark line across the pink muzzle, wavering
CL_, CLIP = srgb(LIP), srgb(PINK)
def mouth(u, v):
    c = np.tile(CLIP, (len(u), 1))
    c[np.abs(v - 0.25 * np.cos(u * math.pi * 1.2)) < 0.42] = CL_
    return c
k.decal(MUZ + (0, -0.035, -0.004), (0, -1, 0.05), (0.04, 0.008), mouth, eps=0.003, rings=6, segs=60)

# --- the eyes: Magmar's, narrower and fiercer under the red brow --------------------------------------------------
HE = (0.023, 0.022)
def eye_top(u):
    return 0.1 + 0.32 * u                 # sloping down toward the nose: a glare
EYE = [(u, eye_top(u)) for u in np.linspace(1, -1, 9)] + \
      [(0.98 * math.cos(t), 0.98 * math.sin(t)) for t in np.linspace(math.pi * 1.06, math.pi * 1.94, 15)]
de = unit(0.5, -0.85, 0.12)
E = H + de * HR * 1.02 + (0, 0, 0.004)
WHITE, CI, LID = np.array([1.0, 1.0, 1.0]), srgb(INK), srgb(RED)
PUPIL = (-0.15, -0.35, 0.48, 0.55)
def eye_open(u, v, pupil=PUPIL, top=eye_top):
    c = np.tile(WHITE, (len(u), 1))
    pu, pv, pa, pb = pupil
    c[((u - pu) / pa) ** 2 + ((v - pv) / pb) ** 2 < 1] = CI
    c[v > top(u) - 0.26] = CI
    c[np.hypot(u, v) > 0.84] = CI
    return c
def eyes(state):
    for side in (1, -1):
        m = np.array([side, 1, 1.0])
        c, n = E * m, de * m
        flip = (lambda f: f) if side == 1 else (lambda f: (lambda u, v: f(-u, v)))
        out_ = polygon([(side * u, v) for u, v in (EYE if side == 1 else EYE[::-1])])
        k.decal(c, n, HE, flip(eye_look(state, eye_open, LID, CI)), eps=0.006, outline=out_, rings=20, segs=56)

gather_all()
k.build(voxel=0.0075, band=0.006)
k.eye_states(eyes)
k.export(out)
