# Exeggutor sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/exeggutor.py -- <the export's Exeggutor.glb> <out.glb>
#
# The design, from the references: a palm tree that walks. A short pear-shaped brown trunk ringed with darker grooves,
# sitting low on two short thick legs whose round feet face forward, each with a pale cream sole and three white claws
# over it. On top three big round pale yellow heads, the eggs grown into coconuts: the one on the left laughing with a
# wide open mouth, a pink tongue and two small fangs, the one on the right smiling, the third peeping out behind on the
# left; all with sleepy half-shut eyes. Out of the top between them a crown of long narrow green leaves fans out all
# round, rising and arching, their tips falling.
# Grown from exeggcute.py: its almond eye, its lid and ink, the way a face is laid on an egg (on_egg, the left eye's
# patch flipped by hand).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, srgb, inside, unit, eye_look, polygon, hull, ring_band, outward, args

src, out = args()
k = Kit(src, coat="leaf")

# --- the line's colours (the eyes' ink and white as Exeggcute's) ----------------------------------------------------------
BARK, GROOVE, CREAM, HEADH = "#A9835C", "#6A4C33", "#F1E6B4", "#EFE29A"
LEAF1, LEAF2, CROWN, CLAW = "#4C9C3A", "#3E8A33", "#3D7F30", "#F4F2EC"
INKH, MOUTHH, TONGUEH = "#2A1E24", "#8C2A36", "#EE8C92"
INK, HEAD_, MOUTH, TONGUE = srgb(INKH), srgb(HEADH), srgb(MOUTHH), srgb(TONGUEH)
WHITE = np.array([1.0, 1.0, 1.0])

# --- the frame -----------------------------------------------------------------------------------------------------------
TRUNK_C, TRUNK_R = np.array([0.0, 0.01, 0.255]), np.array([0.168, 0.158, 0.215])
GROOVES = [0.2, 0.27, 0.34, 0.405]
HEADS = [  # centre, radius, the way its face looks (degrees round from the front), its mood
    ((-0.122, -0.07, 0.565), 0.116, -28, "laugh"),
    ((0.124, -0.058, 0.585), 0.112, 26, "smile"),
    ((-0.172, 0.085, 0.64), 0.1, -78, "glum"),
]
CROWN_C = np.array([0.0, 0.03, 0.68])
HIP, FOOT_C, FOOT_R = np.array([0.11, -0.03, 0.15]), np.array([0.175, -0.14, 0.062]), np.array([0.074, 0.064, 0.064])


def pear(V):
    """The trunk: fuller below its middle, narrowing a little toward its top."""
    V = V.copy()
    s = 1 + 0.08 * np.clip(-V[:, 2], 0, 1) - 0.12 * np.clip(V[:, 2], 0, 1) ** 1.5
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.maximum(V[:, 2], -0.92)
    return V


def on_egg(c, r, az, el, lift=0.0):
    """A point of an ellipsoid at azimuth az (0 the front, -Y; + toward +X) and elevation el, and its normal."""
    a, e = math.radians(az), math.radians(el)
    d = np.array([math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)])
    r = np.array(r, float) * np.ones(3)
    s = 1 / math.sqrt(((d / r) ** 2).sum())
    p = np.array(c) + d * s
    n = (p - c) / r ** 2
    n /= np.linalg.norm(n)
    return p + n * lift, n


# --- the trunk, ringed --------------------------------------------------------------------------------------------------
k.add("trunk", ell(TRUNK_C, TRUNK_R, deform=pear), BARK, "body", surface="skin")
k.add("tail", ell((0.0, 0.135, 0.17), (0.05, 0.05, 0.045)), BARK, "tail", surface="skin")


def trunk_radius(z):
    t = (z - TRUNK_C[2]) / TRUNK_R[2]
    s = 1 + 0.08 * max(-t, 0) - 0.12 * max(t, 0) ** 1.5
    return TRUNK_R[0] * s * math.sqrt(max(0.0, 1 - t * t))


for i, z in enumerate(GROOVES):
    k.add(f"groove{i}", ring_band((0, TRUNK_C[1], z), (0, 0, 1), trunk_radius(z) * 0.965 + 0.002, 0.011, thick=0.007),
          GROOVE, "body", fuse=False, surface="skin")

# --- the legs: short and thick, sitting forward; round feet facing forward with cream soles and three white claws ----
k.add("thigh", tube([HIP, (0.15, -0.09, 0.1), FOOT_C + (0, 0.03, 0.01)], [0.075, 0.068, 0.06]), BARK, "leg{s}",
      mirror=True, surface="skin")
k.add("foot", ell(FOOT_C, FOOT_R, rot=(-20, 0, 12)), BARK, "leg{s}", mirror=True, surface="skin")
SOLE_N = unit(0.2, -1, 0.35)


def sole(P):
    """The foot's front face, a round cream pad with a crisp edge."""
    m = np.zeros(len(P))
    for s in (1, -1):
        c = FOOT_C * np.array([s, 1, 1])
        n = SOLE_N * np.array([s, 1, 1])
        d = P - c
        m = np.maximum(m, inside(0.042 - d @ n, 0.002) * inside(np.linalg.norm(d, axis=1) - 0.1, 0.002))
    return m


k.paint(sole, CREAM)
for j, dx in enumerate((-0.036, 0.0, 0.036)):
    b = FOOT_C + np.array([dx, -0.012, 0.05])
    d = unit(dx * 5, -0.6, 1)
    side = np.array([1.0, 0, 0]) * 0.015
    fwd = unit(0, -1, 0.1) * 0.015
    pts = [b - side, b + side, b - fwd * 0.6, b + fwd, b + d * 0.055]
    k.add(f"claw{j}", hull(pts), CLAW, "leg{s}", fuse=False, mirror=True, surface="shell")

# --- the heads: three big round coconuts on top, fused into the trunk's top ------------------------------------------
for i, (c, r, look, mood) in enumerate(HEADS):
    k.add(f"head{i}", ell(c, (r, r * 0.96, r * 0.97)), HEADH, "head", surface="shell")
k.add("neck", ell((0.0, -0.01, 0.5), (0.12, 0.11, 0.07)), BARK, "head", surface="skin")
k.add("crown", ell(CROWN_C, (0.075, 0.07, 0.06)), CROWN, "leaves")
# the heads keep their own yellow where the bark and the crown's green press against them
k.paint(lambda P: np.ones(len(P)), HEADH, on=["head"])


# --- the leaves: long narrow thick plates from the crown, rising and arching out, their faces turned to the front ----
def leaf_plate(name, a, reach, rise, width, color):
    """A leaf from the crown toward azimuth a (0 the front): its midline arching up and out, its tip falling; its
    width tilted up from the level so its face shows from the front and behind."""
    ar = math.radians(a)
    d = np.array([math.sin(ar), -math.cos(ar), 0.0])
    tilt = math.radians(18 + 42 * abs(math.sin(ar)))
    w0 = np.cross((0, 0, 1.0), d)
    w = w0 * math.cos(tilt) + np.array([0, 0, 1.0]) * math.sin(tilt)
    root = CROWN_C + d * 0.03 + np.array([0, 0, 0.02])

    def at(u, v):
        uu = 0.015 + 0.97 * u
        h = rise * (2.4 * uu - 2.1 * uu * uu)
        mid = root[None, None] + d[None, None] * (reach * uu)[..., None] + np.array([0, 0, 1.0])[None, None] * h[..., None]
        wide = width * (np.clip(uu / 0.18, 0, 1) ** 0.6) * (1 - uu) ** 0.85 + 0.003
        s = (v - 0.5) * 2 * wide
        return mid + s[..., None] * w[None, None]

    k.add(name, outward(sheet(at, 40, 7, 0.017)), color, "leaves", fuse=False, surface="leaf")


LEAVES = [  # azimuth, reach, rise, width
    (-24, 0.24, 0.34, 0.052), (24, 0.24, 0.35, 0.052), (-60, 0.36, 0.28, 0.058), (60, 0.36, 0.29, 0.058),
    (-98, 0.4, 0.22, 0.058), (98, 0.4, 0.22, 0.058), (-138, 0.38, 0.25, 0.056), (138, 0.38, 0.26, 0.056),
    (180, 0.32, 0.3, 0.054), (-162, 0.3, 0.32, 0.052), (162, 0.3, 0.31, 0.052), (0, 0.16, 0.38, 0.048),
]
for i, (a, reach, rise, width) in enumerate(LEAVES):
    leaf_plate(f"leaf{i}", a, reach, rise, width, LEAF1 if i % 2 else LEAF2)


# --- the faces: Exeggcute's way, grown ---------------------------------------------------------------------------------
SLEEPY = [(-1.0, 0.05), (-0.45, 0.48), (0.45, 0.5), (1.0, 0.12), (0.5, -0.55), (-0.5, -0.6)]
ANGRY = [(-1.0, 0.0), (-0.3, 0.3), (1.0, 0.78), (0.92, -0.2), (0.35, -0.8), (-0.45, -0.66)]


def outline_of(pts):
    f = polygon(pts)
    cache = {}
    def r(th):
        key = round(th, 6)
        if key not in cache:
            cache[key] = f(th)
        return cache[key]
    return r


SLEEPY_O, ANGRY_O = outline_of(SLEEPY), outline_of(ANGRY)


def rim_share(o, u, v):
    th = np.arctan2(v, u)
    return np.hypot(u, v) / np.array([o(t) for t in th])


def sleepy(u, v):
    """Lids drooping over the top half, a heavy lid line, the pupil low (Exeggcute's, on the yellow)."""
    c = np.tile(WHITE, (len(u), 1))
    c[rim_share(SLEEPY_O, u, v) > 0.8] = INK
    c[v > 0.06 - 0.12 * u] = HEAD_
    c[np.abs(v - (0.06 - 0.12 * u)) < 0.17] = INK
    c[(((u + 0.05) / 0.36) ** 2 + ((v + 0.2) / 0.34) ** 2 < 1) & (v < 0.06 - 0.12 * u)] = INK
    return c


def glum(u, v):
    c = np.tile(WHITE, (len(u), 1))
    c[rim_share(ANGRY_O, u, v) > 0.8] = INK
    c[v > 0.08 + 0.18 * u] = HEAD_
    c[np.abs(v - (0.08 + 0.18 * u)) < 0.17] = INK
    c[(((u + 0.3) / 0.34) ** 2 + ((v + 0.15) / 0.34) ** 2 < 1) & (v < 0.08 + 0.18 * u)] = INK
    return c


EYES = {"laugh": (sleepy, SLEEPY_O), "smile": (sleepy, SLEEPY_O), "glum": (glum, ANGRY_O)}


def d_shape(th):
    """Flat across the top, round below: an open mouth."""
    s = math.sin(th)
    return min(1.0, 0.25 / s) if s > 0 else 1.0


def laugh(u, v):
    """A wide open laugh: dark red, a pink tongue low in it, two small white fangs hanging from the top's corners,
    edged in ink."""
    c = np.tile(MOUTH, (len(u), 1))
    c[(u / 0.62) ** 2 + ((v + 0.72) / 0.5) ** 2 < 1] = TONGUE
    for x in (-0.5, 0.5):
        c[(np.abs(u - x) < 0.17 * (v - 0.0) / 0.25) & (v > 0.0) & (v < 0.25)] = WHITE
    c[np.hypot(u, v) > 0.88] = INK
    return c


def smile(u, v):
    """A closed smile, a pink crescent edged in ink."""
    c = np.tile(TONGUE, (len(u), 1))
    c[np.hypot(u, v) > 0.8] = INK
    c[v > 0.2] = INK
    return c


def glum_mouth(u, v):
    c = np.tile(HEAD_, (len(u), 1))
    curve = 0.35 - 0.62 * u * u
    c[(np.abs(v - curve) < 0.22) & (np.abs(u) < 0.85)] = INK
    return c


for i, (c, r, look, mood) in enumerate(HEADS):
    if mood == "laugh":
        p, n = on_egg(c, r, look, -26)
        k.decal(p, n, (0.046, 0.04), laugh, eps=0.0034, outline=d_shape, rings=24, segs=64)
    elif mood == "smile":
        p, n = on_egg(c, r, look, -24)
        k.decal(p, n, (0.04, 0.026), smile, eps=0.0034, outline=lambda th: min(1.0, 0.24 / math.sin(th)) if math.sin(th) > 0 else 1.0,
                rings=24, segs=64)
    else:
        p, n = on_egg(c, r, look, -24)
        k.decal(p, n, (0.022, 0.014), glum_mouth, eps=0.0034, rings=24, segs=64)

k.build(voxel=0.0065, smooth=4, band=0.006)


def flip(f):
    return lambda u, v: f(-u, v)


def flip_outline(o):
    return lambda th: o(math.pi - th)


def eyes(state):
    for i, (c, r, look, mood) in enumerate(HEADS):
        open_look, o = EYES[mood]
        f = eye_look(state, open_look, HEAD_, INK, line=0.2)
        half = (r * 0.3, r * 0.22)
        for s in (1, -1):
            p, n = on_egg(c, r, look + s * 25, 14)
            k.decal(p, n, half, f if s > 0 else flip(f), eps=0.004,
                    outline=o if s > 0 else flip_outline(o), rings=20, segs=56)


k.eye_states(eyes)
k.export(out)
