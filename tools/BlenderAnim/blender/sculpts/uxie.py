# Uxie sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/uxie.py -- <the export's Uxie.glb> <out.glb>
#
# The first of the lake trio, written so that Mesprit and Azelf are grown from it: the body, the arms, the thighs, the
# two tails with their fins, the gems, the face's mask and the mouth are the same functions on the same frame in all
# three; only the head's cap, its colour and the eyes differ.
#
# From the references: a small floating fairy, pale grey-blue; a slim pear of a body with a thin short neck, thin arms
# hanging with pointed hands, two plump thighs drawn up with a pointed flap rising off each; two long thin tails out of
# the body's bottom, each ending in a flat fin of three pointed lobes over a round middle with a red gem in it, two
# small teeth either side of the stalk under it; a large head (some 0.4 of the height) whose top, back and sides are a
# great yellow cap in rounded lobes like a brain, coming down at the cheeks in two curls that frame a pale face, with a
# point down between the eyes on which sits a red gem; the eyes always shut, two calm curved lines; a tiny mouth.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

# --- the trio's colours ----------------------------------------------------------------------------------------------
SKIN = "#B6D3EE"           # the pale grey-blue body (between the art's grey and the 3D model's paler blue)
GEM = "#E0263E"
INKH = "#2A2432"
CAP = "#F3C63E"            # Uxie's yellow

# --- the trio's frame (bones: body 0, 0, 0.44; arms +-0.055, -0.01, 0.49; tails +-0.025, 0.01, 0.35; head 0, 0, 0.54)
BODY_C = np.array([0.0, 0.004, 0.425])        # a slim pear, fullest low down
BODY_R = np.array([0.064, 0.056, 0.104])
THIGH_C = np.array([0.046, -0.014, 0.352])     # plump thighs drawn up at the body's bottom
THIGH_R = np.array([0.04, 0.046, 0.043])
NECK = [(0.0, 0.0, 0.5), (0.0, -0.003, 0.535), (0.0, -0.005, 0.575)]
HEAD_C = np.array([0.0, -0.005, 0.655])       # the pale skull under the cap; the face is its front
HEAD_R = np.array([0.104, 0.097, 0.1])
ARM = [(0.05, -0.006, 0.49), (0.082, -0.026, 0.458), (0.104, -0.046, 0.416), (0.113, -0.056, 0.39)]
ARM_R = [0.019, 0.016, 0.011, 0.004]
# The tails: down out of the body's bottom, round in a loop under it and up at each side to the fin
TAIL = [(0.02, 0.012, 0.35), (0.032, 0.03, 0.275), (0.065, 0.058, 0.205), (0.125, 0.07, 0.185),
        (0.178, 0.062, 0.206), (0.198, 0.052, 0.252)]
TAIL_R = [0.026, 0.024, 0.021, 0.019, 0.018, 0.017]
FIN_TURN = 30              # the fin's face turned this far from straight ahead toward its own side
FIN = 1.7                  # the fin's size: the tails' whole point, about half as tall as the head
STALK, BULB = 0.088 * FIN, 0.105 * FIN  # the stalk's length over the tail's end, the fin's round body's middle over it
FIN_GEM = (0.027, 0.017, 0.044)         # the fin's gem's half sizes: across, through, up (a quarter of the fin's height)


def pear(V):
    """Fullest low down, narrowing upward into the neck."""
    V = V.copy()
    s = 1 + 0.08 * np.clip(-V[:, 2], 0, 1) - 0.38 * np.clip(V[:, 2], 0, 1) ** 1.5
    V[:, 0] *= s; V[:, 1] *= s
    return V


def lens(V):
    """A pointed oval: a gem's shape."""
    V = V.copy()
    w = 0.35 + 0.65 * (1 - np.abs(V[:, 2])) ** 0.8
    V[:, 0] *= w; V[:, 1] *= w
    return V


def on_head(ang, h, lift=0.0):
    """The point of the skull at an angle round its upright axis (radians, 0 the front, + toward +X) and a height over
    its middle, and the outward normal there."""
    c = math.sqrt(max(1 - (h / HEAD_R[2]) ** 2, 0.0))
    p = HEAD_C + np.array([HEAD_R[0] * math.sin(ang) * c, -HEAD_R[1] * math.cos(ang) * c, h])
    n = (p - HEAD_C) / HEAD_R ** 2
    n /= np.linalg.norm(n)
    return p + n * lift, n


# --- the body ------------------------------------------------------------------------------------------------------
def add_body(k):
    k.add("body", ell(BODY_C, BODY_R, deform=pear), SKIN, "body")
    k.add("thigh", ell(THIGH_C, THIGH_R, rot=(-15, 0, -10)), SKIN, "body", mirror=True)
    # The pointed flap rising off each thigh, up, out and a little forward, flattened across
    k.add("flap", blade([(0.05, -0.04, 0.37), (0.062, -0.058, 0.4), (0.07, -0.066, 0.425)], [0.02, 0.011, 0.004],
                        (1, 0, 0), 0.55), SKIN, "body", mirror=True)
    k.add("necklow", tube(NECK[:2], [0.03, 0.024]), SKIN, "body")
    k.add("neckup", tube(NECK[1:], [0.024, 0.028]), SKIN, "head")
    # Thin arms hanging a little forward, ending in a point
    k.add("arm", tube(ARM, ARM_R), SKIN, "arm{s}", mirror=True)


# --- the tails and their fins: loose, so the fins keep their edges; each fin a flat plate facing the front and out ----
def slab(poly, base, a, b, n, thick):
    """A convex polygon of (along a, along b) points about base, extruded thick along n; faces wound outward."""
    P = np.array([base + x * a + y * b for x, y in poly])
    m = len(P)
    top, bot = P + n * thick / 2, P - n * thick / 2
    ct, cb = top.mean(axis=0), bot.mean(axis=0)
    V = np.vstack([top, bot, [ct, cb]])
    F = []
    for i in range(m):
        j = (i + 1) % m
        F.append([2 * m, i, j])
        F.append([2 * m + 1, m + j, m + i])
        F.append([i, m + i, m + j, j])
    mid = V.mean(axis=0)
    out_ = []
    for f in F:
        p = V[f]
        nn = np.cross(p[1] - p[0], p[2] - p[0])
        out_.append(f if np.dot(nn, p.mean(axis=0) - mid) >= 0 else f[::-1])
    return V, out_


def petal(length, width, angle, at=(0.0, 0.0), k_=24):
    """A pointed lobe from at, length long, leaning angle degrees from upright (+ outward along a)."""
    t = np.linspace(0, 1, k_)
    w = width * np.sin(np.pi * t) ** 0.7 * (1 - 0.3 * t)
    right = [(w[i], length * t[i]) for i in range(k_)]
    left = [(-w[i], length * t[i]) for i in range(k_ - 2, 0, -1)]
    pts = right + left
    ca, sa = math.cos(math.radians(angle)), math.sin(math.radians(angle))
    return [(at[0] + x * ca + y * sa, at[1] - x * sa + y * ca) for x, y in pts]


def add_tails(k):
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        m = np.array([s, 1, 1.0])
        end = np.array(TAIL[-1]) * m
        up = unit(0.08 * s, 0, 1)
        t = math.radians(FIN_TURN)
        a = np.array([math.cos(t) * s, math.sin(t), 0.0])          # across the fin: out and back
        a = a - up * np.dot(a, up); a /= np.linalg.norm(a)
        n = np.cross(a, up)
        pts = [np.array(p) * m for p in TAIL] + [end + up * STALK * 0.5, end + up * STALK]
        k.add(f"tail{sd}", tube(pts, TAIL_R + [0.0155, 0.015]), SKIN, f"tail{sd}", fuse=False)
        mid = end + up * BULB
        f = FIN
        # The fin's round body, its three pointed lobes over it, and two teeth either side of the stalk
        disc = [(0.034 * f * math.cos(th) * (1 - 0.15 * math.sin(th)), 0.036 * f * math.sin(th))
                for th in np.linspace(0, 2 * math.pi, 48, endpoint=False)]
        k.add(f"fin{sd}", slab(disc, mid, a, up, n, 0.02), SKIN, f"tail{sd}", fuse=False)
        k.add(f"finlobe{sd}0", slab(petal(0.088 * f, 0.024 * f, 0, (0, 0.012 * f)), mid, a, up, n, 0.017), SKIN,
              f"tail{sd}", fuse=False)
        for side in (1, -1):
            k.add(f"finlobe{sd}{side}", slab(petal(0.07 * f, 0.021 * f, 44 * side, (0.012 * f * side, 0.004 * f)),
                                             mid, a, up, n, 0.0165), SKIN, f"tail{sd}", fuse=False)
        for j, hgt in enumerate((0.03 * f, 0.052 * f)):
            for side in (1, -1):
                tooth = [(0, -0.007 * f), (0.026 * f * side, 0.002 * f), (0, 0.009 * f)]
                k.add(f"fintooth{sd}{j}{side}", slab(tooth, end + up * hgt, a, up, n, 0.014), SKIN, f"tail{sd}",
                      fuse=False)
        # The gem through the fin's middle, showing on both faces, with a white glint on each face
        frame = np.stack([a, -n, up], axis=1).T          # right-handed, or its faces are wound inside out
        V, F = ell((0, 0, 0), FIN_GEM, deform=lens, sub=3)
        k.add(f"fingem{sd}", (V @ frame + mid, F), GEM, f"tail{sd}", fuse=False, glow=GEM)
        for face in (1, -1):
            g = mid + a * (-0.3 * FIN_GEM[0]) + up * (0.3 * FIN_GEM[2]) + (-n) * face * FIN_GEM[1] * 0.78
            Vg, Fg = ell((0, 0, 0), (0.0065, 0.004, 0.01), sub=2)
            k.add(f"finglint{sd}{face}", (Vg @ frame + g, Fg), "#FFFFFF", f"tail{sd}", fuse=False)


# --- the gem on the forehead: a loose pointed oval standing out of the cap's point ------------------------------------
def add_gem(k, h=0.052, half=(0.022, 0.037), proud=0.004):
    p, n = on_head(0.0, h)
    V, F = ell((0, 0, 0), (half[0], 0.012, half[1]), deform=lens, sub=3)
    up = unit(*(np.array([0, 0, 1.0]) - n * n[2]))
    side = np.cross(up, n)
    frame = np.stack([side, -n, up], axis=1).T
    V = V @ frame + p + n * proud
    k.add("gem", (V, F), GEM, "head", fuse=False, glow=GEM)
    # A white glint high on the gem's face toward its left, so it reads as a gem and not as red paint
    g = p + n * (proud + 0.0095) - side * 0.3 * half[0] + up * 0.3 * half[1]
    Vg, Fg = ell((0, 0, 0), (0.0055, 0.0035, 0.0085), sub=2)
    k.add("gemglint", (Vg @ frame + g, Fg), "#FFFFFF", "head", fuse=False)


# --- the face: the skull's front painted pale under the cap's edge, by angle round the head's upright axis ------------
def face_mask(edge_angles, edge_heights, reach=1.3, edge=0.0025):
    """Skin where a point of the head lies under the cap's edge: the edge's height over the skull's middle is read
    from a table by the angle round its upright axis; reach limits it to the head (not locks hanging at its sides)."""
    def mask(P):
        d = P - HEAD_C
        ang = np.abs(np.arctan2(d[:, 0], -d[:, 1]))
        e = np.interp(ang, edge_angles, edge_heights)
        near = (((d / (HEAD_R * reach)) ** 2).sum(1) < 1).astype(float)
        return inside(d[:, 2] - e, edge) * near
    return mask


INK = srgb(INKH)
CS = srgb(SKIN)


def add_face(k, outline, h=-0.005, half=(0.1, 0.07)):
    """The face's crisp top and sides: a pale patch over the front whose outline is the cap's edge (a paint's edge
    along the creases by the curls came out ragged); the paint under it only needs to fill below."""
    p, n = on_head(0.0, h)
    k.decal(p, (0, -1, 0.0), half, SKIN, eps=0.0025, outline=polygon(outline), rings=24, segs=120)


def mouth_line(u, v):
    c = np.tile(CS, (len(u), 1))
    c[(np.abs(v - 0.25 * (u * u - 0.4)) < 0.42) & (np.abs(u) < 0.85)] = INK
    return c


def add_mouth(k, h=-0.056, half=(0.011, 0.005), look=mouth_line):
    p, n = on_head(0.0, h)
    k.decal(p, n, half, look, eps=0.0035, rings=10, segs=48)


# --- Uxie's head: a great yellow cap in rounded lobes over the top, back and sides, down in curls at the cheeks --------
def add_cap(k):
    k.add("head", ell(HEAD_C, HEAD_R), CAP, "head")
    k.add("capmass", ell((0.0, 0.03, 0.675), (0.13, 0.112, 0.1)), CAP, "head")
    # Two broad smooth halves like a brain's, a clear parting down the middle from front to back
    k.add("caphalf", ell((0.062, 0.022, 0.712), (0.092, 0.122, 0.102), rot=(0, -12, 0)), CAP, "head", mirror=True)
    # A clear round knob of a curl standing out at each cheek
    k.add("capcurl", ell((0.124, -0.03, 0.64), (0.048, 0.048, 0.05)), CAP, "head", mirror=True)


# The cap's edge over the face: a point down between the eyes, arches over them, down the cheeks inside the curls
UX_EDGE_A = [0.0, 0.12, 0.3, 0.55, 0.8, 1.0, 1.25, 1.6, 3.2]
UX_EDGE_H = [0.005, 0.012, 0.02, 0.018, -0.005, -0.04, -0.07, -0.085, -0.085]
# The patch's outline in its own measure (u across the face, v up; half sizes 0.1 by 0.07): the point down between
# the eyes, an arch over each, down the cheeks inside the curls, and a bottom well inside the painted chin
UX_FACE = [(0.0, 0.3), (0.18, 0.42), (0.38, 0.6), (0.55, 0.58), (0.72, 0.42), (0.86, 0.12), (0.95, -0.3),
           (0.9, -0.85), (0.0, -0.9), (-0.9, -0.85), (-0.95, -0.3), (-0.86, 0.12), (-0.72, 0.42), (-0.55, 0.58),
           (-0.38, 0.6), (-0.18, 0.42)]


# --- Uxie's eyes: always shut, two calm lines arched like closed lids ------------------------------------------------
CI = srgb("#3A3040")


def slit(u, v):
    c = np.tile(CS, (len(u), 1))
    c[(np.abs(u) < 0.88) & (np.abs(v - (0.32 * (1 - u * u) - 0.12)) < 0.26)] = CI
    return c


def sleepy(state):
    """eye_look's states from the slit; Fierce is the same line tilted down toward the nose (eye_look's would draw a
    lid line across it)."""
    if state != "Fierce":
        return eye_look(state, slit, CS, CI, line=0.26)

    def look(u, v):
        c = np.tile(CS, (len(u), 1))
        c[(np.abs(u) < 0.88) & (np.abs(v - (0.4 * u - 0.02)) < 0.26)] = CI
        return c
    return look


EYE_ANG, EYE_H, EYE_HALF = 0.47, 0.004, (0.032, 0.026)


def eyes(state):
    p, n = on_head(EYE_ANG, EYE_H)
    k.decal(p, n, EYE_HALF, sleepy(state), eps=0.004, rings=20, segs=56, mirror=True)


add_body(k)
add_cap(k)
add_tails(k)
add_gem(k, h=0.058)
k.paint(face_mask(UX_EDGE_A, UX_EDGE_H), SKIN)
add_face(k, UX_FACE)
add_mouth(k)
k.build(voxel=0.0055, smooth=4, band=0.005)
k.eye_states(eyes)
k.export(out)
