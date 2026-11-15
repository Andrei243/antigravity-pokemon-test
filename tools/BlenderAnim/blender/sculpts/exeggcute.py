# Exeggcute sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/exeggcute.py -- <the export's Exeggcute.glb> <out.glb>
#
# The design, from the references: six pale pink eggs huddled tight, each standing upright, a little narrower at the
# top; one in the middle a little larger and raised, the others round it: one in front, one on each side, two behind.
# Each has its own small face: white almond eyes edged in dark, a black pupil at the inner corner, the top edge a
# thick dark line sloping down toward the middle (angry); the middle one smug with a smirk, the front one with a small
# open mouth, the side ones frowning, the one behind on the right sleepy with half-shut lids. The one behind on the
# left is broken open at its top: a jagged rim, the dark inside of the shell, a yellow yolk. Thin dark zigzag cracks
# down a few of the others.
# Exeggutor's script copies this one's colours, its almond eye and its way of laying a face on an egg.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, inside, unit, eye_look, polygon, hull, args

src, out = args()
k = Kit(src, coat="shell")

# --- the line's colours -------------------------------------------------------------------------------------------------
PINK, INKH, CRACKH, MOUTHH = "#F2CED4", "#2A1E24", "#6A4450", "#5C2430"
YOLK, HOLLOW = "#F4C22E", "#4A2E34"
PINK_, INK, CRACK, MOUTH = srgb(PINK), srgb(INKH), srgb(CRACKH), srgb(MOUTHH)
WHITE = np.array([1.0, 1.0, 1.0])

# --- the frame: the eggs, each (bone, centre, radii, the way its face looks in degrees round from the front, its mood)
R = np.array([0.079, 0.079, 0.1])
EGGS = [
    ("body", (0.0, -0.152, 0.1), R, 0, "open"),          # in front, a small open mouth
    ("egg0", (-0.116, -0.07, 0.1), R, -24, "frown"),      # on the left as one looks at it
    ("egg2", (0.118, -0.064, 0.1), R, 26, "frown"),       # on the right
    ("egg3", (-0.072, 0.078, 0.1), R, -82, "frown"),      # behind on the left, broken open at the top
    ("egg4", (0.078, 0.084, 0.104), R, 100, "sleepy"),     # behind on the right, sleepy
    ("egg5", (0.0, -0.022, 0.2), R * np.array([1.07, 1.05, 1.06]), -4, "smug"),   # the middle one, raised
]
BROKEN = 3                     # which egg is broken open
BREAK_Z = 0.62                 # where its top is broken off, in its own half height


def egg_shape(V):
    """An egg as it stands: fuller below its middle, narrower toward its top, a little flatter underneath."""
    V = V.copy()
    s = 1 + 0.05 * np.clip(-V[:, 2], 0, 1) - 0.13 * np.clip(V[:, 2], 0, 1) ** 1.7
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.where(V[:, 2] < 0, np.maximum(V[:, 2], -0.95), V[:, 2])
    return V


def broken_shape(V):
    """The broken egg: the same egg with its top cut off flat a little below where the rim's teeth stand."""
    V = egg_shape(V)
    V[:, 2] = np.minimum(V[:, 2], BREAK_Z)
    return V


def on_egg(c, r, az, el, lift=0.0):
    """A point of an egg at azimuth az (0 the front, -Y; + toward +X) and elevation el, in degrees, and its outward
    normal (on the plain ellipsoid; the patches find the surface themselves)."""
    a, e = math.radians(az), math.radians(el)
    d = np.array([math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)])
    s = 1 / math.sqrt(((d / r) ** 2).sum())
    p = np.array(c) + d * s
    n = (p - c) / np.array(r) ** 2
    n /= np.linalg.norm(n)
    return p + n * lift, n


# --- the eggs -----------------------------------------------------------------------------------------------------------
for i, (bone, c, r, look, mood) in enumerate(EGGS):
    k.add(f"egg{i}", ell(c, r, deform=broken_shape if i == BROKEN else egg_shape), PINK, bone)

# How the eggs lie against each other (printed for the frame's tuning: below 1 they are pressed together)
for i in range(len(EGGS)):
    for j in range(i + 1, len(EGGS)):
        ci, ri = np.array(EGGS[i][1]), EGGS[i][2]
        cj, rj = np.array(EGGS[j][1]), EGGS[j][2]
        P, _ = ell(ci, ri, deform=egg_shape, sub=3)
        m = float(np.sqrt((((P - cj) / rj) ** 2).sum(1)).min())
        if m < 0.97:
            print(f"EGGS: {EGGS[i][0]} presses into {EGGS[j][0]} ({m:.2f})")

# The broken one: the dark inside of the shell on its flat top, a yolk standing up in it, a jagged rim of shell round it
bc, br = np.array(EGGS[BROKEN][1]), EGGS[BROKEN][2]
top = bc[2] + br[2] * BREAK_Z
rim_r = br[0] * 0.76          # the egg's radius at the break, about
k.paint(lambda P: inside(top - 0.004 - P[:, 2], 0.002) * inside(np.hypot(P[:, 0] - bc[0], P[:, 1] - bc[1]) - rim_r * 0.97, 0.002),
        HOLLOW, on=[f"egg{BROKEN}"])
k.add("yolk", ell((bc[0] + 0.004, bc[1] - 0.006, top + 0.002), (0.036, 0.036, 0.02)), YOLK, "egg3")
# the rim's teeth: flat-faced shards standing up round the break, higher behind, a gap where the yolk shows in front
TEETH = 11
for t in range(TEETH):
    a0 = 2 * math.pi * t / TEETH
    a1 = 2 * math.pi * (t + 1) / TEETH
    am = (a0 + a1) / 2
    h = (0.024 if t % 2 else 0.014) + 0.012 * (1 + math.cos(am - math.pi * 0.5)) / 2     # taller toward the back (+Y)
    ro, ri = rim_r * 1.01, rim_r * 0.82
    pts = []
    for a in (a0, a1):
        for rr in (ro, ri):
            pts.append((bc[0] + rr * math.cos(a), bc[1] + rr * math.sin(a), top - 0.01))
    tip = (bc[0] + (ro + ri) / 2 * math.cos(am + 0.12), bc[1] + (ro + ri) / 2 * math.sin(am + 0.12), top + h)
    pts.append(tip)
    pts.append((tip[0] * 0.5 + bc[0] * 0.5 + (ro * 0.99) * math.cos(am) * 0.5, tip[1] * 0.5 + bc[1] * 0.5 + (ro * 0.99) * math.sin(am) * 0.5,
                top + h * 0.45))
    k.add(f"shard{t}", hull(pts), PINK, "egg3", fuse=False, surface="shell")

# --- the cracks: thin dark zigzags, each a chain of straight strokes laid as patches with crisp edges ------------------
def stroke(c, r, a, b, width=0.0042):
    """One straight stroke of a crack from (az, el) a to b on an egg: a thin patch along the surface."""
    pa, _ = on_egg(c, r, *a)
    pb, _ = on_egg(c, r, *b)
    pm, nm = on_egg(c, r, (a[0] + b[0]) / 2, (a[1] + b[1]) / 2)
    d = pb - pa
    d = d - nm * (d @ nm)
    L = np.linalg.norm(d) / 2 + width * 0.4
    t = width / 2 / L
    k.decal(pm, nm, (L, L), CRACKH, up=d / np.linalg.norm(d), eps=0.0032,
            outline=polygon([(-t, -1), (t, -1), (t, 1), (-t, 1)]), rings=6, segs=40)


def crack(i, pts):
    c, r = EGGS[i][1], EGGS[i][2]
    for a, b in zip(pts, pts[1:]):
        stroke(c, r, a, b)


crack(1, [(-30, 78), (-40, 62), (-30, 50), (-44, 36)])          # the left one, down from its top
crack(2, [(-10, 80), (2, 64), (-8, 52), (4, 40)])               # the right one, on its inner side
crack(4, [(150, 74), (158, 58), (146, 46), (156, 32)])              # the sleepy one, on its outer side
crack(0, [(-20, 72), (-10, 60), (-22, 50)])                     # the front one, a short one near the top

# --- the faces ----------------------------------------------------------------------------------------------------------
EYE_HALF = (0.0205, 0.0155)
EYE_AZ, EYE_EL, MOUTH_EL = 25, 12, -20

ANGRY = [(-1.0, 0.0), (-0.3, 0.3), (1.0, 0.78), (0.92, -0.2), (0.35, -0.8), (-0.45, -0.66)]
SLEEPY = [(-1.0, 0.05), (-0.45, 0.48), (0.45, 0.5), (1.0, 0.12), (0.5, -0.55), (-0.5, -0.6)]


def outline_of(pts):
    f = polygon(pts)
    cache = {}
    def r(th):
        key = round(th, 6)
        if key not in cache:
            cache[key] = f(th)
        return cache[key]
    return r


ANGRY_O, SLEEPY_O = outline_of(ANGRY), outline_of(SLEEPY)


def rim_share(o, u, v):
    """How far out a point is in a patch whose outline is o: 1 at the outline."""
    th = np.arctan2(v, u)
    return np.hypot(u, v) / np.array([o(t) for t in th])


def angry(u, v):
    """White, a dark rim, a thick dark top edge sloping down to the inner corner, a black pupil under it inside."""
    c = np.tile(WHITE, (len(u), 1))
    c[rim_share(ANGRY_O, u, v) > 0.8] = INK
    c[v > 0.25 + 0.3 * u + 0.16] = INK
    c[((u + 0.28) / 0.34) ** 2 + ((v - 0.02) / 0.36) ** 2 < 1] = INK
    return c


def smug(u, v):
    """The angry eye half shut: the lid's dark line lower and flat, the pupil looking aside."""
    c = np.tile(WHITE, (len(u), 1))
    c[rim_share(ANGRY_O, u, v) > 0.8] = INK
    c[v > 0.08 + 0.18 * u] = PINK_
    c[np.abs(v - (0.08 + 0.18 * u)) < 0.17] = INK
    c[(((u + 0.42) / 0.32) ** 2 + ((v + 0.12) / 0.34) ** 2 < 1) & (v < 0.08 + 0.18 * u)] = INK
    return c


def sleepy(u, v):
    """Lids drooping over the top half, a heavy lid line, the pupil low."""
    c = np.tile(WHITE, (len(u), 1))
    c[rim_share(SLEEPY_O, u, v) > 0.8] = INK
    c[v > 0.02] = PINK_
    c[np.abs(v - 0.02) < 0.17] = INK
    c[(((u - 0.0) / 0.34) ** 2 + ((v + 0.2) / 0.32) ** 2 < 1) & (v < 0.02)] = INK
    return c


EYES = {"open": (angry, ANGRY_O), "frown": (angry, ANGRY_O), "smug": (smug, ANGRY_O), "sleepy": (sleepy, SLEEPY_O)}


def mouth_of(mood):
    """The mood's mouth: a colour function on the egg's pink, and its patch's half size."""
    if mood == "open":
        def f(u, v):
            col = np.tile(PINK_, (len(u), 1))
            col[(u / 0.72) ** 2 + ((v + 0.05) / 0.82) ** 2 < 1] = MOUTH
            return col
        return f, (0.013, 0.011)
    if mood == "smug":
        def f(u, v):
            col = np.tile(PINK_, (len(u), 1))
            curve = -0.15 + 0.5 * (u + 0.2) ** 2 * (u > -0.2) - 0.1 * u
            col[(np.abs(v - curve) < 0.2) & (np.abs(u) < 0.9)] = INK
            return col
        return f, (0.024, 0.012)
    def f(u, v):
        col = np.tile(PINK_, (len(u), 1))
        curve = 0.35 - 0.62 * u * u          # a frown, its corners turned down
        col[(np.abs(v - curve) < 0.2) & (np.abs(u) < 0.85)] = INK
        return col
    return f, (0.015, 0.01)


for i, (bone, c, r, look, mood) in enumerate(EGGS):
    f, half = mouth_of(mood)
    p, n = on_egg(c, r, look, MOUTH_EL)
    k.decal(p, n, half, f, eps=0.0034, rings=24, segs=64)

k.build(voxel=0.0045, smooth=4, band=0.006)


def flip(f):
    return lambda u, v: f(-u, v)


def flip_outline(o):
    return lambda th: o(math.pi - th)


def eyes(state):
    for i, (bone, c, r, look, mood) in enumerate(EGGS):
        open_look, o = EYES[mood]
        f = eye_look(state, open_look, PINK_, INK, line=0.2)
        for s in (1, -1):
            p, n = on_egg(c, r, look + s * EYE_AZ, EYE_EL)
            # u runs toward +X on both: outward is +u on the right eye and -u on the left
            k.decal(p, n, EYE_HALF, f if s > 0 else flip(f), eps=0.004,
                    outline=o if s > 0 else flip_outline(o), rings=20, segs=56)


k.eye_states(eyes)
k.export(out)
