# Mega Infernape (a Mega of our own, plan 06 · R20b) on the skeleton and clips of the game's own export of the form:
#   blender --background --factory-startup --python sculpts/infernapemega.py -- <Infernape-Mega.glb> <out.glb>
# No game has it, so the game's own sculpt (PokemonModels.Ours.Megas.cs, InfernapeMega) is the design, grown here from
# Infernape's Blender sculpt (infernape.py, copied: it builds as it is read), laid in Infernape's own frame and stretched
# to the form's (6% taller on its legs, the form's joints): its orange fur turned red and its white fur charcoal, red
# flashes down the dark chest, a tan face under the red mask so the eyes read; its crown of fire grown into a mane of
# thick tongues streaming back from the brow and down the spine, a flame burning at the tail's tip; its gold guards
# grown into great plates of armour on the shoulders and over the knees; a ring of flame round each wrist in place of
# the pale wisp. Everything else is Infernape's: the blue hands and feet, the ears, the almond eyes under the mask.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, tri, unit, boxy, eye_look, args, leaf, polygon, sibling
from sculptkit import ring_band

cq = sibling("cyndaquil")

# The form's stretch of Infernape's frame (Blender's axes: across, depth, height)
K = np.array([1.0, 1.0, 1.06])


class Stretched(Kit):
    """A kit that lays everything it is given in Infernape's frame, stretched to the form's."""

    def add(self, name, shape, color, bone, **kw):
        V, F = shape
        super().add(name, (V * K, F), color, bone, **kw)

    def paint(self, mask, color, on=None):
        super().paint(lambda P, m0=mask: m0(P / K), color, on)

    def decal(self, center, normal, half, color, **kw):
        n = np.array(normal, float) / K
        super().decal(np.array(center, float) * K, n / np.linalg.norm(n), half, color, **kw)


src, out = args()
k = Stretched(src, coat="fur")

# Infernape's orange is the Mega's red, its white the Mega's charcoal; the tan of the face is new
ORANGE, WHITE, GOLD, GOLDLINE, BLUE, RED = "#CE462A", "#42383E", "#F2C640", "#C2901E", "#2C5FB4", "#E23A2C"
TAN, FLASH = "#E2B28C", "#E8502E"
H = np.array([0, -0.03, 1.02])


def plate(c, n, r, t):
    """A flat round plate (a guard): radius r across, half thickness t along n, its middle at c."""
    V, F = ell((0, 0, 0), (r, r, t))
    n = unit(*n)
    a = np.cross(n, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a)
    b = np.cross(n, a)
    return V @ np.stack([a, b, n], axis=1).T + np.array(c, float), F

# The body: a broad chest of white fur, ragged where it meets the orange waist and back
k.add("chest", ell((0, 0.0, 0.69), (0.17, 0.13, 0.2)), ORANGE, "body")
k.add("hips", ell((0, 0.01, 0.5), (0.135, 0.11, 0.1)), ORANGE, "body")
def skirt(V):
    V = V.copy()
    ph = np.arctan2(V[:, 0], -V[:, 1])
    low = np.clip(-V[:, 2], 0, 1)
    s = 1 + 0.35 * low * (0.5 + 0.5 * tri(ph * 11 / (2 * math.pi)))
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] -= 0.8 * low * (0.5 + 0.5 * tri(ph * 11 / (2 * math.pi)))
    return V
k.add("fringe", ell((0, -0.015, 0.565), (0.165, 0.125, 0.04), deform=skirt), WHITE, "body")
def chest(P):
    edge = 0.585 + 0.025 * tri(P[:, 0] * 14)
    front = smoothstep(0.06, 0.0, P[:, 1])
    return inside(edge - P[:, 2], 0.004) * front
k.paint(chest, WHITE, on=["chest"])
k.add("neck", ell((0, -0.01, 0.86), (0.08, 0.07, 0.06)), ORANGE, "head")

# The head: a little wider than tall, a monkey's snout standing forward of the face, big round ears blue within,
# white fur swept back from the cheeks in points
k.add("head", ell(H, (0.18, 0.16, 0.15)), ORANGE, "head")
k.add("muzzle", ell((0, -0.165, 0.962), (0.075, 0.07, 0.052)), TAN, "head")
for base, d, length, width, thick in [((0.11, -0.09, 0.95), (1, 0.45, -0.3), 0.2, 0.05, 0.036),
                                      ((0.125, -0.08, 1.0), (1, 0.5, 0.05), 0.2, 0.048, 0.034),
                                      ((0.13, -0.05, 1.045), (1, 0.7, 0.35), 0.15, 0.04, 0.03)]:
    k.add("tuft", leaf(base, d, length, width, thick), WHITE, "head", mirror=True)
# the head's sides white where the tufts grow from, so the cheek fur runs into them
k.paint(lambda P: inside(0.105 - np.abs(P[:, 0]), 0.004) * inside(P[:, 2] - 1.075, 0.004)
        * inside(P[:, 1] - 0.02, 0.004) * inside(0.9 - P[:, 2], 0.004), WHITE, on=["head"])
def pointed(V):
    """An ear rounded below and drawn up to a blunt point."""
    V = V.copy()
    t = np.clip(V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.5 * t ** 1.5
    V[:, 2] += 0.25 * t ** 2
    return V
k.add("ear", ell((0.205, 0.015, 1.09), (0.066, 0.022, 0.075), rot=(0, 25, 22), deform=pointed), ORANGE, "head",
      mirror=True)
k.decal((0.212, -0.008, 1.095), unit(0.37, -1, 0), (0.03, 0.045), BLUE, up=(0.42, 0, 1), eps=0.003, rings=10,
        segs=36, mirror=True, outline=lambda th: 1.0 - 0.25 * max(0.0, math.sin(th)))

# The Mega's mane: Infernape's crown of fire grown into thick tongues streaming back from the brow, out at the sides
# and down the nape, one standing at the brow; tongues down the spine; a flame at the tail's tip
f = cq.Flames()
def tongue(root, d, length, width, bone, flick=0.25):
    d = unit(*d)
    side = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(side) < 1e-3:
        side = np.array([1.0, 0, 0])
    f.flame(np.array(root, float), d, length, width, bone, side / np.linalg.norm(side), flick=flick)
def lick(root, d, length, width, bone, flick=0.2):
    """A small tongue of fire with no heart (the wrists', the spine's): a third of a flame's vertices."""
    d = unit(*d)
    side = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(side) < 1e-3:
        side = np.array([1.0, 0, 0])
    f.groups.setdefault((bone, cq.FLAME), []).append(
        f.tongue(np.array(root, float), d, length, width, side / np.linalg.norm(side), flick))
tongue((0, -0.05, 1.14), (0, 0.5, 1), 0.24, 0.065, "flame")
for x, y, z, up, L in [(0.0, 0.0, 1.16, 0.55, 0.54), (-0.075, 0.01, 1.14, 0.45, 0.48), (0.075, 0.01, 1.14, 0.45, 0.48),
                       (-0.13, 0.04, 1.08, 0.2, 0.42), (0.13, 0.04, 1.08, 0.2, 0.42), (0.0, 0.08, 1.1, 0.1, 0.5),
                       (-0.07, 0.1, 1.02, -0.05, 0.4), (0.07, 0.1, 1.02, -0.05, 0.4)]:
    tongue((x, y, z), (x * 1.5, 1, up), L, 0.075, "flame")
for i in range(5):
    lick((0, 0.07, 0.925 - 0.094 * i), (0, 1, -0.15), 0.24 - 0.025 * i, 0.055 - 0.004 * i, "body", flick=0.15)
tongue((0.0, 0.41, 0.92), (0.2, 0.15, 1), 0.28, 0.065, "tail")

# Arms: golden shoulders, orange arms, golden bracers with a pale flame of fur, blue hands
# The guards: flat golden plates, each with its swirl (centre, facing, radius, half thickness, bone)
GUARDS = [((0.262, -0.016, 0.818), (0.85, -0.35, 0.45), 0.074, 0.024, "arm{s}"),     # shoulder
          ((0.366, -0.05, 0.445), (1, -0.55, 0.05), 0.042, 0.018, "arm{s}"),        # wrist's outer face
          ((0.165, -0.075, 0.3), (0.15, -1, 0.1), 0.072, 0.026, "leg{s}"),         # knee (the Mega's plate, larger)
          ((0.088, -0.108, 0.755), (0.4, -1, 0.25), 0.036, 0.014, "body")]          # chest
for i, (c, n, r, t, bone) in enumerate(GUARDS):
    k.add(f"guard{i}", plate(c, n, r, t), GOLD, bone, mirror=True, surface="metal")
k.add("arm", tube([(0.22, 0, 0.78), (0.285, 0, 0.62), (0.325, -0.02, 0.45)], [0.05, 0.044, 0.042]), ORANGE, "arm{s}",
      mirror=True)
k.add("bracer", tube([(0.317, -0.016, 0.485), (0.332, -0.025, 0.415)], [0.052, 0.054]), GOLD, "arm{s}", mirror=True,
      surface="metal")
# The Mega's great plates of gold armour on the shoulders, a darker lip under each
k.add("pauldron", ell((0.255, 0.0, 0.84), (0.15, 0.13, 0.095), rot=(0, 20, 0)), GOLD, "arm{s}", mirror=True,
      surface="metal")
k.add("pauldronlip", ell((0.31, 0.0, 0.79), (0.09, 0.11, 0.04), rot=(0, 35, 0)), GOLDLINE, "arm{s}", mirror=True,
      surface="metal")
# A ring of flame round each wrist, where Infernape has its pale wisp: a glowing band round the bracer and short
# tongues standing out from it
for s, bone in ((1, "armR"), (-1, "armL")):
    a, b = np.array([0.317 * s, -0.016, 0.485]), np.array([0.332 * s, -0.025, 0.415])
    ax = b - a
    k.add(f"wristfire{bone}", ring_band((a + b) / 2, ax, 0.064, 0.05, thick=0.014), "#F26028", bone, fuse=False,
          glow="#FF6A2A")
    for j in range(5):
        t = j * 2 * math.pi / 5 + 0.3
        o = np.array([math.cos(t), math.sin(t), 0.0])
        o = o - ax * np.dot(o, ax) / np.dot(ax, ax)
        o /= np.linalg.norm(o)
        lick((a + b) / 2 + o * 0.06, o * 0.8 + np.array([0, 0, 1.0]), 0.1, 0.03, bone, flick=0.2)
k.add("hand", ell((0.342, -0.035, 0.36), (0.055, 0.048, 0.052)), BLUE, "arm{s}", mirror=True)
for i, (x, y) in enumerate([(0.312, -0.068), (0.338, -0.075), (0.364, -0.066), (0.385, -0.045)]):
    k.add(f"finger{i}", tube([(x, y, 0.34), (x + 0.007 * (i - 1.5), y - 0.012, 0.29), (x + 0.01 * (i - 1.5), y - 0.006,
                              0.225)], [0.017, 0.015, 0.012]), BLUE, "arm{s}", mirror=True)
k.add("thumb", tube([(0.302, -0.05, 0.375), (0.285, -0.09, 0.325)], [0.016, 0.012]), BLUE, "arm{s}", mirror=True)

# Legs: orange thighs, golden knees, white shins of ragged fur, blue feet
k.add("leg", tube([(0.12, 0.02, 0.48), (0.16, -0.02, 0.3), (0.165, 0.0, 0.1)], [0.07, 0.055, 0.05]), ORANGE, "leg{s}",
      mirror=True)
k.paint(lambda P: inside(P[:, 2] - (0.235 + 0.02 * tri(np.arctan2(P[:, 0] - 0.16 * np.sign(P[:, 0]), -P[:, 1]) * 1.6)),
                         0.004), WHITE, on=["leg"])
def toes(V):
    ph = np.arctan2(V[:, 0], -V[:, 1])
    s = 1 + 0.16 * np.cos(4 * ph) * np.clip(-V[:, 1], 0, 1)
    V = V.copy(); V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("foot", ell((0.17, -0.06, 0.04), (0.07, 0.12, 0.04), deform=toes), BLUE, "leg{s}", mirror=True)

# The long tail, curling up behind
k.add("tail", tube([(0, 0.1, 0.48), (0, 0.26, 0.43), (0, 0.4, 0.5), (0.02, 0.48, 0.68), (0.03, 0.46, 0.86),
                    (0.0, 0.4, 0.93)], [0.036, 0.032, 0.03, 0.028, 0.026, 0.022]), ORANGE, "tail")

# The golden swirls on shoulders, bracers and knees
CG, CL = srgb(GOLD), srgb(GOLDLINE)
def swirl(u, v):
    r = np.sqrt(u * u + v * v)
    turn = (r * 1.6 - np.arctan2(v, u) / (2 * math.pi)) % 1
    c = np.tile(CG, (len(u), 1))
    c[(turn < 0.16) & (r > 0.08) & (r < 0.8)] = CL
    c[r > 0.9] = CL
    return c
for c, n, r, t, bone in GUARDS:
    n = unit(*n)
    k.decal(np.array(c) + n * t, n, (r * 0.92, r * 0.92), swirl, eps=0.004, rings=16, segs=48, mirror=True)

# The face: a thick red mask slanting down to a point over the nose, edged dark, a golden crest rising from it
# into white fur; orange below it, white fur only out at the cheeks
CO, CW, CR, CGd, CB, CRD = srgb(ORANGE), srgb(WHITE), srgb(RED), srgb(GOLD), srgb(BLUE), srgb("#6E1210")
CT = srgb(TAN)
FC, FS = 1.04, 0.15                          # the face patch's middle height and half size
def mask_low(au):
    return -0.12 + 0.45 * au                 # the mask's lower edge, a point low between the eyes
def face(u, v):
    au = np.abs(u)
    c = np.tile(CO, (len(u), 1))
    c[(u / 0.78) ** 2 + ((v + 0.3) / 0.75) ** 2 < 1] = CT       # the Mega's tan face under the mask
    low = mask_low(au)
    top = low + 0.33 + 0.12 * au
    c[(au > 0.7 - 0.12 * np.clip(-v, 0, 1)) & (v < low)] = CW
    c[v > top] = CW
    # the golden crest rising from the mask's middle in tongues of flame
    t0 = 0.21
    h = np.clip(v - t0, 0, 1)
    w = 0.08 + 0.55 * h - 0.09 * np.abs(np.sin(h * 9))      # widening upward in two tongues of flame
    c[(v > top - 0.03) & (au < w)] = CGd
    band = (v >= low) & (v <= top)
    c[band] = CR
    c[band & ((v - low < 0.04) | (top - v < 0.04))] = CRD
    c[band & (au > 0.74) & (v - low > (top - low) * 0.55 * (1 - (au - 0.74) / 0.2))] = CB
    return c
k.decal((0, -0.17, FC), (0, -1, 0), (FS, FS), face, eps=0.003, rings=64, segs=220)

# The snout: two small nostrils and a thin smirk low on it
NOSE = srgb("#4A2016")
k.decal((0.017, -0.224, 0.99), unit(0.2, -1, 0.7), (0.007, 0.0045), NOSE, eps=0.005, rings=4, segs=20, mirror=True)
def smirk(u, v):
    c = np.tile(CT, (len(u), 1))
    line = -0.15 + 0.25 * u * u + 0.22 * u + 0.2 * np.clip(u - 0.55, 0, 1)
    c[(np.abs(v - line) < 0.17) & (np.abs(u) < 0.9)] = NOSE
    return c
k.decal((0, -0.222, 0.94), unit(0, -1, -0.35), (0.04, 0.02), smirk, eps=0.005, rings=8, segs=40)

# The eyes: small slanted almonds tucked under the mask, which cuts their tops straight; a yellow white, a blue iris
# cut by the lid, a dark pupil. In each eye's own frame u runs outward and v up.
HE = 0.035                                  # an eye's half size
SL = 0.42                                    # the slope of its top, down toward the nose
def eye_top(u):
    return 0.25 + SL * u
def eye_bottom(u):
    return eye_top(u) - 0.78 * (1 - u * u) ** 0.8
ALMOND = [(u, eye_top(u)) for u in np.linspace(1, -1, 9)] + [(u, eye_bottom(u)) for u in np.linspace(-0.92, 0.92, 13)]
de = unit(0.38, -0.92, 0.06)
E = np.array([0.064, -0.176, 1.042])
DARK, LID = srgb("#1E1410"), srgb("#5A1610")
SCLERA, IRIS, IRISRIM, LOWER = srgb("#F6E7A2"), srgb("#3C7DD4"), srgb("#1F3E86"), srgb("#C9864A")
RI = 0.42                                    # the iris's radius in the eye's units
UC = -0.12
VC = eye_top(UC) - (RI - 0.07) * math.sqrt(1 + SL * SL)
def lid_cut(state, u):
    """Where the lid comes down to: the eye's own top, or lower toward the nose when fierce."""
    return np.minimum(eye_top(u), 0.02 + 0.62 * u) if state == "Fierce" else eye_top(u)
def eye_open(state):
    def look(u, v):
        c = np.tile(SCLERA, (len(u), 1))
        cut = lid_cut(state, u)
        c[v > cut - 0.17] = LID
        c[v > cut] = srgb(RED)
        c[(v < eye_bottom(u) + 0.08)] = LOWER
        return c
    return look
def iris_look(state):
    def look(a, b):
        u, v = UC + a * RI, VC + b * RI
        r = np.sqrt(a * a + b * b)
        c = np.tile(IRIS, (len(a), 1))
        c[r > 0.78] = IRISRIM
        c[r < 0.5] = DARK
        c[((a + 0.25) / 0.2) ** 2 + ((b - 0.3) / 0.2) ** 2 < 1] = (1, 1, 1)
        c[v > lid_cut(state, u) - 0.17] = LID
        c[v > lid_cut(state, u)] = srgb(RED) if state == "Fierce" else LID
        c[(v < eye_bottom(u) + 0.08) | (np.abs(u) > 0.95)] = LOWER
        c[v < eye_bottom(u)] = CT
        return c
    return look
def frame(n):
    n = np.array(n, float); n /= np.linalg.norm(n)
    right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
    return right, np.cross(n, right)
def eyes(state):
    for side in (1, -1):
        m = np.array([side, 1, 1.0])
        c, n = E * m, de * m
        right, up = frame(n)
        flip = (lambda f: f) if side == 1 else (lambda f: (lambda u, v: f(-u, v)))
        outline = polygon([(side * u, v) for u, v in (ALMOND if side == 1 else ALMOND[::-1])])
        if state in ("Open", "Fierce"):
            k.decal(c, n, (HE, HE), flip(eye_open(state)), eps=0.008, outline=outline, rings=16, segs=56)
            ic = c + right * side * UC * HE + up * VC * HE
            k.decal(ic, n, (RI * HE, RI * HE), flip(iris_look(state)), eps=0.010, rings=12, segs=40)
        else:
            k.decal(c, n, (HE, HE), flip(eye_look(state, eye_open("Open"), CT, DARK)), eps=0.008, outline=outline,
                    rings=16, segs=56)

# The Mega's red flashes down the dark of the chest, slanting out and down, three a side
def flashes(P):
    m = np.zeros(len(P))
    for z in (0.58, 0.645, 0.71):
        for s in (1, -1):
            x, zz = P[:, 0] - 0.085 * s, P[:, 2] - z
            c, sn = math.cos(math.radians(25)), math.sin(math.radians(25)) * s
            u, w = x * c + zz * sn, -x * sn + zz * c
            m = np.maximum(m, inside((u / 0.06) ** 2 + (w / 0.013) ** 2 - 1, 0.25))
    return m * inside(P[:, 1] + 0.06, 0.01)
k.paint(flashes, FLASH, on=["chest"])
f.add(k)

k.build(voxel=0.0095)
k.eye_states(eyes)
k.export(out)
