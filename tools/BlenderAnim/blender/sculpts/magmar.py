# Magmar sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Magby's script (magby.py): the same red and yellow, the same black band round the neck, the same tall eye with its
# solid pupil and a lid cut straight across it, the same white cone claws, flame marks painted the same way round the
# body. Its flames are Ponyta's way (ponyta.py): ragged tongues with a yellow heart on both faces, gathered a bone and
# a colour to a piece.
#   blender --background --factory-startup --python sculpts/magmar.py -- <the export's Magmar.glb> <out.glb>
#
# The design, from the references: a yellow trunk and great yellow thighs, the back red, red flame marks licking over
# the front (a tongue down the chest from the neck, tongues rising over the belly); a small red head whose brow is two
# great lumps, each crowned with a flame; heavy-lidded eyes under them; a yellow duck's bill open on a pink mouth; a
# black band round the neck and one round each ankle; red arms with ridged forearms, white spikes and claws; red
# shins and feet, three white claws each; red points along the back; a long tail, yellow at its root and red along it,
# ending in a flame.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, tri, args

src, out = args()
k = Kit(src, coat="fur")

# The line's colours, Magby's unchanged, and the flames'
RED, YELLOW, BAND, CLAW, INK, MOUTH = "#EC4A44", "#F8D650", "#2B2528", "#F4F1E8", "#1C1416", "#A8384A"
SPIKE, PINK = "#C9322E", "#E98A8E"
ORANGE, DEEP, FLAMEY = "#FF7A22", "#E8442A", "#FFD23A"

B = np.array([0, 0.0, 0.43])             # the trunk's middle
BR = np.array([0.155, 0.13, 0.165])
H = np.array([0, -0.05, 0.69])           # the face's middle
HR = np.array([0.096, 0.092, 0.092])


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


# --- Ponyta's flames ---------------------------------------------------------------------------------------------
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


# --- the trunk: yellow, red behind ----------------------------------------------------------------------------------
def barrel(V):
    V = V.copy()
    s = 1 + 0.08 * np.clip(-V[:, 2], 0, 1) - 0.12 * np.clip(V[:, 2], 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("trunk", ell(B, BR, deform=barrel), YELLOW, "body")
k.add("shoulder", ell((0.125, -0.005, 0.525), (0.075, 0.07, 0.07)), YELLOW, "body", mirror=True)
k.add("neck", ell((0, -0.025, 0.59), (0.065, 0.06, 0.045)), RED, "head")
k.add("collar", outward("collar", ring((0, -0.025, 0.595), 0.062, 0.012)), BAND, "body", fuse=False,
      surface="leather")
# Great yellow thighs fused into the trunk's foot
THIGH = np.array([0.11, -0.025, 0.27])
k.add("thigh", ell(THIGH, (0.1, 0.105, 0.1)), YELLOW, "leg{s}", mirror=True)

# The back red, its edge a row of tongues licking forward round the sides
def back_red(P):
    """In arc length round the trunk's axis, so that the edge is as crisp as the marks'."""
    a = np.abs(np.arctan2(P[:, 0] - B[0], -(P[:, 1] - B[1]))) * BR[0]
    edge = BR[0] * (2.0 - 0.36 * (0.5 + 0.5 * tri((P[:, 2] - 0.3) / 0.075)))
    return inside(edge - a, 0.0035) * (P[:, 2] > 0.36)
k.paint(back_red, RED)
# A red tongue down the chest from the neck, and red flames rising over the belly
CHEST = [(-0.052, 0.62), (0.052, 0.62), (0.045, 0.565), (0.06, 0.52), (0.03, 0.5), (0.016, 0.455), (0.002, 0.49),
         (-0.028, 0.47), (-0.032, 0.51), (-0.058, 0.545)]
BELLY = [(-0.075, 0.30), (0.08, 0.30), (0.088, 0.35), (0.07, 0.395), (0.052, 0.36), (0.038, 0.4), (0.015, 0.435),
         (0.004, 0.39), (-0.022, 0.415), (-0.04, 0.37), (-0.058, 0.385), (-0.082, 0.34)]
FRONT = lambda P: (P[:, 1] < B[1] - 0.02) & (np.abs(P[:, 0]) < 0.13)
k.paint(lambda P: poly_mask(*round_about(P, B, BR[0]), CHEST) * FRONT(P), RED)
k.paint(lambda P: poly_mask(*round_about(P, B, BR[0]), BELLY) * FRONT(P) * (P[:, 2] > 0.29), RED)

# Red points along the back, in pairs, swept back
for i, (c, d, L, r) in enumerate([((0.07, 0.1, 0.53), (0.5, 1, 0.35), 0.11, 0.035),
                                  ((0.09, 0.11, 0.42), (0.5, 1, 0.1), 0.1, 0.032),
                                  ((0.05, 0.12, 0.33), (0.3, 1, -0.1), 0.07, 0.025)]):
    c = np.array(c); d = unit(*d)
    k.add(f"spike{i}", blade([c, c + d * L * 0.5, c + d * L], [r, r * 0.55, 0.003], (0, 0, 1), 0.45), SPIKE, "body",
          fuse=False, mirror=True)

# --- legs: red shins under the thighs, a black band at each ankle, red feet with three white claws -----------------
k.add("shin", tube([(0.115, -0.02, 0.2), (0.12, -0.025, 0.12), (0.122, -0.03, 0.05)], [0.05, 0.044, 0.042]), RED,
      "leg{s}", mirror=True)
k.add("ankle", outward("ankle", ring((0.121, -0.027, 0.1), 0.044, 0.012)), BAND, "leg{s}", fuse=False, mirror=True,
      surface="leather")
FOOT = np.array([0.125, -0.065, 0.034])
k.add("foot", ell(FOOT, (0.066, 0.095, 0.034)), RED, "leg{s}", mirror=True)
for i, a in enumerate((-30, 0, 30)):
    d = unit(math.sin(math.radians(a)), -math.cos(math.radians(a)), -0.12)
    k.add(f"toe{i}", cone(FOOT + d * 0.07 + (0, 0, -0.006), d, 0.048, 0.016), CLAW, "leg{s}", fuse=False,
          mirror=True, surface="shell")

# --- arms forward and a little down: a red upper arm, a forearm of ridged scales, white spikes and claws ----------
ARM = [(0.15, -0.025, 0.53), (0.19, -0.085, 0.49), (0.21, -0.155, 0.45), (0.218, -0.205, 0.425)]
AD = unit(*(np.array(ARM[-1]) - np.array(ARM[1])))
k.add("arm", tube(ARM, [0.045, 0.042, 0.048, 0.045]), RED, "arm{s}", mirror=True)
# The forearm's scales: two rings of loose points laid back toward the elbow, a jagged sleeve
fa = np.array(ARM[1]); fb = np.array(ARM[-1])
s1 = np.cross(AD, (0, 0, 1.0)); s1 /= np.linalg.norm(s1); s2 = np.cross(s1, AD)
for j, t in enumerate((0.3, 0.75)):
    c = fa + (fb - fa) * t
    for i in range(6):
        a = 2 * math.pi * (i + 0.5 * j) / 6
        o = s1 * math.cos(a) + s2 * math.sin(a)
        d = unit(*(-AD * 0.9 + o * 0.55))
        base = c + o * 0.034
        k.add(f"scale{j}{i}", blade([base, base + d * 0.03, base + d * 0.055], [0.022, 0.014, 0.002],
                                     o, 0.45), SPIKE, "arm{s}", fuse=False, mirror=True)
HAND = np.array(ARM[-1]) + AD * 0.03
for i, d in enumerate([(0.2, -1, 0.35), (0.55, -1, 0.05), (0.35, -1, -0.35), (0.0, -1, -0.15)]):
    k.add(f"claw{i}", cone(HAND + unit(*d) * 0.03, d, 0.045, 0.012), CLAW, "arm{s}", fuse=False, mirror=True,
          surface="shell")
for i, t in enumerate((0.35, 0.7)):
    c = np.array(ARM[1]) + (np.array(ARM[-1]) - np.array(ARM[1])) * t + (0.01, 0.01, 0.04)
    k.add(f"aspike{i}", cone(c, (0.3, 0.4, 1), 0.045, 0.012), CLAW, "arm{s}", fuse=False, mirror=True,
          surface="shell")

# --- the tail: yellow at its root, red along it, back and out to +X, ending in a flame ------------------------------
TAIL = [(0.0, 0.1, 0.32), (0.02, 0.21, 0.2), (0.07, 0.3, 0.14), (0.14, 0.37, 0.15), (0.18, 0.41, 0.23)]
k.add("tail", tube(TAIL, [0.07, 0.055, 0.042, 0.032, 0.022]), RED, "tail")
k.paint(lambda P: inside(P[:, 1] - 0.24, 0.004) * (P[:, 1] > 0.11) * (P[:, 2] < 0.32), YELLOW, on=["tail"])
TIP = np.array(TAIL[-1])
back(TIP + (0, 0.01, 0.0), (0.45, 0.35, 1), 0.2, 0.06, "tail", (0, 1, 0), flick=0.5)
for d, L, w in [((0.35, 0.2, 1), 0.2, 0.055), ((0.7, 0.4, 0.6), 0.14, 0.045), ((0.1, 0.6, 0.8), 0.14, 0.045)]:
    flame(TIP, d, L, w, "tail", side=(0.5, 1, 0), flick=0.45)

# --- the head: two great lumps on the brow, each crowned with a flame -----------------------------------------------
k.add("face", ell(H, HR), RED, "head")
LUMP = np.array([0.055, -0.062, 0.778])
k.add("lump", ell(LUMP, (0.068, 0.07, 0.066)), RED, "head", mirror=True)
k.add("crown", ell((0, 0.0, 0.755), (0.08, 0.07, 0.058)), RED, "head")
for s in (1, -1):
    top = LUMP * (s, 1, 1) + (0.008 * s, 0.01, 0.05)
    flame(top, (0.35 * s, 0.25, 1), 0.13, 0.045, "head", side=(s, 0.4, 0), flick=0.35)
    flame(top + (0.012 * s, 0.01, -0.01), (0.75 * s, 0.35, 0.7), 0.08, 0.03, "head", side=(s, 0.4, 0), flick=0.3,
          heart=False, lick=False)

# The bill: Magby's cone grown into a duck's bill, flattened, open on a pink mouth
BILL0 = np.array([0, -0.07, 0.665])
BILLD = unit(0, -1, -0.32)
BILLL = 0.11
k.add("bill", blade([BILL0, BILL0 + BILLD * BILLL * 0.55, BILL0 + BILLD * BILLL], [0.058, 0.052, 0.04],
                    (0, -0.3, 1), 0.5), YELLOW, "head", fuse=False, surface="skin")
JAW0 = np.array([0, -0.065, 0.625])
JAWD = unit(0, -1, -0.75)
k.add("jaw", blade([JAW0, JAW0 + JAWD * 0.038, JAW0 + JAWD * 0.075], [0.045, 0.04, 0.029], (0, -0.7, 1), 0.5),
      YELLOW, "head", fuse=False, surface="skin")
k.add("mouth", ell((0, -0.112, 0.625), (0.038, 0.044, 0.024), rot=(25, 0, 0)), PINK, "head", fuse=False,
      surface="skin")

# --- the eyes: Magby's tall white and solid pupil, the heavy lid cut lower and level under the brow ----------------
HE = (0.025, 0.028)
def eye_top(u):
    return 0.2 - 0.1 * u                  # a heavy lid, a little lower at the outer corner: a bored glare
EYE = [(u, eye_top(u)) for u in np.linspace(1, -1, 9)] + \
      [(0.98 * math.cos(t), 0.98 * math.sin(t)) for t in np.linspace(math.pi * 1.06, math.pi * 1.94, 15)]
de = unit(0.48, -0.86, 0.08)
E = H + de * HR * 1.02
WHITE, CI, LID = np.array([1.0, 1.0, 1.0]), srgb(INK), srgb(RED)
PUPIL = (-0.2, -0.3, 0.5, 0.58)
def eye_open(u, v, pupil=PUPIL, top=eye_top):
    c = np.tile(WHITE, (len(u), 1))
    pu, pv, pa, pb = pupil
    c[((u - pu) / pa) ** 2 + ((v - pv) / pb) ** 2 < 1] = CI
    c[v > top(u) - 0.24] = CI
    c[np.hypot(u, v) > 0.84] = CI
    return c
def eyes(state):
    for side in (1, -1):
        m = np.array([side, 1, 1.0])
        c, n = E * m, de * m
        flip = (lambda f: f) if side == 1 else (lambda f: (lambda u, v: f(-u, v)))
        out_ = polygon([(side * u, v) for u, v in (EYE if side == 1 else EYE[::-1])])
        k.decal(c, n, HE, flip(eye_look(state, eye_open, LID, CI)), eps=0.006, outline=out_, rings=20, segs=56)

# The whole head (face, brow lumps, crown, bill, its flames and the eyes) a fifth larger about its bone, as measured
# against the references; the neck, which is on the head's bone too, stays
HB, HS = np.array([0, -0.03, 0.58]), 1.2
for p in k.parts:
    if p["bone"] == "head" and not p["name"].startswith("neck"):
        p["V"] = HB + (p["V"] - HB) * HS
for (bone, _, _), shapes in groups.items():
    if bone == "head":
        shapes[:] = [(HB + (V - HB) * HS, F) for V, F in shapes]
E = HB + (E - HB) * HS
HE = (HE[0] * HS, HE[1] * HS)

gather_all()
k.build(voxel=0.0075, band=0.006)
k.eye_states(eyes)
k.export(out)
