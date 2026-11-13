# Roserade sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/roserade.py -- <the export's Roserade.glb> <out.glb>
#
# Grown from Roselia's script: the same greens and pale limbs, the same way of painting a hood and laying a mouth.
# From the references: a compact, almost chibi figure. A big green head (with its bloom about two fifths of the whole
# height, the face as wide as the hips) under a great white bloom of hair, one smooth mass set like a cap over the top
# of the head and swept back, a few short points off its top on the -X side and a swept lock at each side, the larger
# out to +X; a dark green mask across sly eyes (a yellow eye under a dark lid line, a red iris), a small smirk. A short
# pear of a torso: a yellow ring at the neck, a darker green vest ending in a V on the chest, pale belly, round pale
# hips; short legs, thick at the hip and tapering to yellow points. Short thin pale arms with a jagged leaf cuff at
# each wrist, each holding a bouquet of three big round roses about as big as the face: red in the hand on -X, raised
# by the cheek, blue on +X, low by the hip. A dark green leaf of a cape behind, from the collar to about the knees,
# wider than the hips and ending in a toothed point.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, leaf, sheet, srgb, inside, smoothstep, tri, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="leaf")

GREEN, PALE, CAPE, MASK = "#47B046", "#C6E59C", "#24703A", "#1C4824"
WHITE, YELLOW, INKH = "#F6F6F0", "#F0D24A", "#24301C"
H = np.array([0.0, -0.01, 0.71])         # the head's centre
HEADR = np.array([0.14, 0.125, 0.115])
E_ = 0.0055                              # a crisp paint's edge, under a voxel or so


def torus(c, R, r, n=48, m=14):
    """A ring about the upright axis."""
    a = np.linspace(0, 2 * math.pi, n, endpoint=False)
    b = np.linspace(0, 2 * math.pi, m, endpoint=False)
    A, Bb = np.meshgrid(a, b, indexing="ij")
    V = np.stack([(R + r * np.cos(Bb)) * np.cos(A), (R + r * np.cos(Bb)) * np.sin(A), r * np.sin(Bb)], -1)
    V = V.reshape(-1, 3) + np.array(c)
    F = []
    for i in range(n):
        for j in range(m):
            F.append([i * m + j, ((i + 1) % n) * m + j, ((i + 1) % n) * m + (j + 1) % m, i * m + (j + 1) % m])
    return V, F


# --- legs and body: short legs, thick at the hip, under a short pear of a torso -------------------------------
k.add("leg", tube([(0.055, 0.0, 0.3), (0.054, -0.006, 0.21), (0.046, -0.014, 0.13), (0.038, -0.02, 0.07),
                   (0.034, -0.022, 0.05)], [0.068, 0.056, 0.036, 0.02, 0.014]), PALE, "leg{s}", mirror=True)
# the feet: yellow points
k.add("foot", tube([(0.034, -0.022, 0.07), (0.033, -0.028, 0.03), (0.032, -0.036, 0.003)], [0.016, 0.011, 0.002]),
      YELLOW, "leg{s}", fuse=False, mirror=True)
k.add("hips", ell((0, 0.004, 0.31), (0.126, 0.1, 0.1)), PALE, "body")
def pear(V):
    V = V.copy()
    t = np.clip(V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.45 * t ** 1.5; V[:, 1] *= 1 - 0.4 * t ** 1.5
    return V
k.add("torso", ell((0, 0.0, 0.45), (0.088, 0.078, 0.135), deform=pear), GREEN, "body")
k.add("neck", tube([(0, 0.0, 0.55), (0, 0.0, 0.64)], [0.034, 0.034]), GREEN, "head")

# the vest: green over the chest ending in a V at the front; the belly, hips and legs pale
def belly(P):
    front = np.clip(-P[:, 1] / 0.03, 0, 1)
    edge = 0.485 - 0.085 * np.clip(1 - np.abs(P[:, 0]) / 0.075, 0, 1) * front
    return inside(P[:, 2] - edge, E_) * (P[:, 2] < 0.56)
k.paint(belly, PALE)

# the yellow ring at the neck
k.add("collar", torus((0, 0.0, 0.575), 0.04, 0.013), YELLOW, "body", fuse=False, surface="leaf")

# --- the cape: one dark green leaf down the back from the collar to about the knees, wider than the hips so it shows
# past them from the front, its edges curling in toward the body and its end a toothed point
def cape(inset, lift):
    def at(u, v):
        v = 0.015 + 0.97 * v
        c = 2 * u - 1
        z = 0.57 - 0.4 * v
        w = inset * (0.035 + 0.17 * np.sin(np.pi * np.clip(v, 0, 1) ** 1.1)) * (1 - v) ** 0.3
        w = w * (1 + 0.14 * tri(v * 7) * smoothstep(0.5, 0.7, v))      # teeth along the lower edges
        x = c * w + 0.05 * v ** 3                                       # the point swept a little to +X
        yb = 0.055 + 0.11 * np.sin(np.pi * np.clip(v / 1.1, 0, 1) ** 0.8)
        y = yb + lift - 1.6 * (c * w) ** 2
        return np.stack([x, y, z], -1)
    return at
k.add("cape", sheet(cape(1.0, 0.0), 16, 34, 0.012), CAPE, "cape", fuse=False)
k.add("capein", sheet(cape(0.9, -0.007), 16, 34, 0.004), "#3A8E46", "cape", fuse=False)

# --- the arms, leafy cuffs and bouquets: short thin pale arms -------------------------------------------------
ARMS = {
    -1: [(-0.07, 0.0, 0.545), (-0.13, -0.035, 0.53), (-0.175, -0.07, 0.575)],    # raised, the red bouquet by the cheek
    1: [(0.07, 0.0, 0.545), (0.125, -0.02, 0.47), (0.165, -0.05, 0.415)],         # low, the blue one by the hip
}
for s, pts in ARMS.items():
    side = "R" if s > 0 else "L"
    k.add(f"arm{side}", tube(pts, [0.02, 0.016, 0.015]), PALE, f"arm{side}", fuse=False)
    hand = np.array(pts[-1])
    k.add(f"hand{side}", ell(hand, (0.019, 0.019, 0.019)), PALE, f"arm{side}", fuse=False)
    # the jagged leaf cuff round the wrist: leaves radiating about the forearm's line
    ax = unit(*(hand - np.array(pts[1])))
    a1 = np.cross(ax, (0, 0, 1.0)); a1 /= np.linalg.norm(a1)
    a2 = np.cross(ax, a1)
    for i in range(6):
        th = i * math.pi / 3 + 0.3
        d = 0.75 * (math.cos(th) * a1 + math.sin(th) * a2) + 0.55 * ax
        k.add(f"cuff{side}{i}", leaf(hand - ax * 0.008, d, 0.05, 0.02, 0.008), "#B4DC8C", f"arm{side}", fuse=False)


def rose(name, c, axis, R, col, dark, bone, turn):
    """A tidy rose: a round ball wrapped in a few shells of petal lying close on it, each starting a little further
    from the face than the one inside it so their rims spiral, and a thin dark spiral drawn on the face."""
    z = unit(*axis)
    x = np.cross(z, (0, 0, 1.0)); x /= np.linalg.norm(x)
    y = np.cross(z, x)
    c = np.array(c, float)
    k.add(f"{name}ball", ell(c, (0.97 * R, 0.97 * R, 0.97 * R), sub=4), col, bone, fuse=False)
    for j in range(4):
        a0 = turn + j * 1.75
        rj = R * (1.0 + 0.03 * j)
        p0 = 0.55 + 0.2 * j
        shade = col * (0.94 if j % 2 else 1.0)
        def at(u, v, a0=a0, rj=rj, p0=p0):
            cu = 2 * u - 1
            a = a0 + 2.5 * u
            ph = p0 + 0.45 * cu * cu + (2.55 - p0 - 0.45 * cu * cu) * v
            lip = 1 + 0.05 * np.clip(1 - v / 0.25, 0, 1)            # the rim turned out a little
            d = (np.sin(ph) * np.cos(a))[..., None] * x + (np.sin(ph) * np.sin(a))[..., None] * y \
                + np.cos(ph)[..., None] * z
            return c + d * (rj * lip)[..., None]
        k.add(f"{name}pet{j}", sheet(at, 22, 14, 0.007), shade, bone, fuse=False)
    t = np.linspace(0, 3.2 * math.pi, 48)
    rr = 0.12 + 0.6 * t / t[-1]
    pts = [c + R * 1.012 * (q * math.cos(a + turn) * x + q * math.sin(a + turn) * y + math.sqrt(1 - q * q) * z)
           for q, a in zip(rr, t)]
    k.add(f"{name}curl", tube(pts, [0.0032] * len(pts), seg=8, per=2), dark, bone, fuse=False)


def bouquet(name, hand, s, lift, col, dark, bone):
    """Three big round roses in a tight trefoil before the hand, facing forward and a little out."""
    axis = unit(0.3 * s, -1, 0.1)
    xx = np.cross(axis, (0, 0, 1.0)); xx /= np.linalg.norm(xx)
    yy = np.cross(axis, xx)
    mid = np.array(hand) + np.array((0.055 * s, -0.05, lift))
    for i, a in enumerate((90, 210, 330)):
        p = mid + 0.07 * (math.cos(math.radians(a)) * yy + math.sin(math.radians(a)) * xx)
        rose(f"{name}{i}", p, axis, 0.062, col, dark, bone, 1.3 * i)

bouquet("red", ARMS[-1][-1], -1, 0.04, srgb("#E2505E"), srgb("#A82A3C"), "armL")
bouquet("blue", ARMS[1][-1], 1, -0.035, srgb("#3B7FE2"), srgb("#1E4EA8"), "armR")

# --- the head: big, round, drawn down a little to a soft chin -------------------------------------------------
def chin(V):
    V = V.copy()
    t = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.2 * t ** 1.5
    V[:, 2] -= 0.08 * t ** 2 * np.clip(-V[:, 1], 0, 1)
    return V
k.add("head", ell(H, HEADR, deform=chin), GREEN, "head")

# --- the white bloom of hair: one smooth mass fused to the head, a cap over the top swept back, a short point or two
# off its top on the -X side, a broad lock swept out and back to +X and a smaller one at the -X temple. Every piece
# thick and round, so the remesh keeps its edges smooth.
def bl(name, shape):
    k.add(name, shape, WHITE, "head", surface="leaf")
def capshape(V):
    V = V.copy()
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.8, V[:, 2])        # a little flatter underneath, a round dome
    s = np.clip(V[:, 0], 0, 1) * np.clip(0.55 + 0.6 * V[:, 1], 0, 1)
    V[:, 0] += 0.6 * s ** 1.5                                      # one mass swept out to +X and back
    V[:, 1] += 0.25 * s ** 1.5 + 0.18 * np.clip(V[:, 1], 0, 1) ** 2
    V[:, 2] += 0.04 * s ** 2
    return V
bl("bloom", ell((0.02, 0.035, 0.82), (0.172, 0.168, 0.135), deform=capshape))
bl("bloomBack", ell((0.03, 0.1, 0.79), (0.15, 0.11, 0.1)))
# a roll along the brow, so the bloom comes down to the hairline in one mass with no ledge over the face
roll = []
for th in np.linspace(-1.95, 1.95, 15):
    hl = 0.042 - 0.03 * float(smoothstep(0.6, 1.5, abs(th))) - 0.05 * float(smoothstep(1.7, 2.7, abs(th)))
    roll.append((0.136 * math.sin(th), -0.01 - 0.121 * math.cos(th), H[2] + hl + 0.026))
bl("bloomRoll", tube(roll, [0.03] * len(roll)))
bl("lockR", leaf((0.17, 0.1, 0.85), (1, 0.55, -0.12), 0.2, 0.1, 0.065))
bl("lockR2", leaf((0.11, 0.0, 0.775), (1, 0.25, -0.45), 0.12, 0.05, 0.045))
bl("lockL", leaf((-0.13, 0.0, 0.78), (-0.5, 0.2, -0.85), 0.13, 0.06, 0.05))
for i, (base, tip, r) in enumerate([
        ((-0.08, 0.02, 0.9), (-0.14, 0.0, 0.99), 0.045),
        ((-0.035, 0.03, 0.93), (-0.06, 0.02, 1.02), 0.045),
        ((0.02, 0.04, 0.93), (0.035, 0.05, 0.99), 0.035)]):
    mid = np.array(base) * 0.45 + np.array(tip) * 0.55
    bl(f"bloomPoint{i}", blade([base, mid, tip], [r, r * 0.6, 0.01], (0, 1, 0), 0.75))


def hairline(P):
    """Above it the bloom: just over the eyes in front, lower at the sides, lowest behind."""
    q = P - H
    th = np.abs(np.arctan2(q[:, 0], -q[:, 1]))
    return q[:, 2] - (0.042 - 0.03 * smoothstep(0.6, 1.5, th) - 0.05 * smoothstep(1.7, 2.7, th))
def hair(P):
    q = P - H
    far = np.linalg.norm(q / HEADR, axis=1) > 1.12
    above = inside(-hairline(P), 0.006) * (np.linalg.norm(q, axis=1) < 0.2)
    return np.maximum(above, far * (P[:, 2] > 0.72)) * (P[:, 2] > 0.66)
def skin(P):
    """The head's own green back under the hairline, where the fused bloom's white spread onto it."""
    q = P - H
    return inside(hairline(P), 0.005) * (np.linalg.norm(q, axis=1) < 0.15) * (P[:, 2] > 0.6)
k.paint(skin, GREEN)

# the dark green mask across the eyes, in coordinates round the head's upright axis (angle times radius, height)
def mask(P):
    q = P - H
    ang = np.arctan2(q[:, 0], -q[:, 1])
    au = np.abs(ang) * 0.13
    zr = q[:, 2]
    top = 0.2                      # up under the bloom, whose painted hairline is the crisp edge
    low = -0.022 - 0.022 * np.sin(np.clip(au / 0.115, 0, 1) * math.pi * 0.85) + 0.04 * smoothstep(0.1, 0.15, au)
    front = (q[:, 1] < 0.0) & (np.linalg.norm(q, axis=1) < 0.16)
    m = inside(zr - top, E_) * inside(low - zr, E_) * inside(au - 0.15, E_)
    return m * front
k.paint(mask, MASK)
k.paint(hair, WHITE)

# the smirk, low on the face, lifting at the +X corner
INK = srgb(INKH)
def smirk(u, v):
    c = np.tile(srgb(GREEN), (len(u), 1))
    line = -0.15 + 0.35 * u * u + 0.25 * u
    c[(np.abs(v - line) < 0.2) & (np.abs(u) < 0.88)] = INK
    return c
mdir = unit(0, -1, -0.62)
k.decal(H + mdir * 0.112, mdir, (0.028, 0.014), smirk, eps=0.004, rings=8, segs=36)

# --- the eyes: sly yellow almonds slanting up outward, a dark lid line along the top, a red iris and a dark pupil.
# u runs outward, v up.
HE = 0.043
SL = 0.32
def eye_top(u):
    return 0.28 + SL * u
def eye_bottom(u):
    return eye_top(u) - 0.85 * (1 - u * u) ** 0.75
ALMOND = [(u, eye_top(u)) for u in np.linspace(1, -1, 9)] + [(u, eye_bottom(u)) for u in np.linspace(-0.92, 0.92, 13)]
SCL, LIDR, IRIS, IRISRIM, DARK, LID = (srgb("#F4D64A"), srgb("#2A140E"), srgb("#D8303C"), srgb("#8A1424"),
                                       srgb("#1A0E10"), srgb(MASK))
RI = 0.46
UC = -0.05
VC = eye_top(UC) - (RI - 0.12) * math.sqrt(1 + SL * SL)
def lid_cut(state, u):
    return np.minimum(eye_top(u), 0.02 + 0.6 * u) if state == "Fierce" else eye_top(u)
def eye_open(state):
    def look(u, v):
        c = np.tile(SCL, (len(u), 1))
        cut = lid_cut(state, u)
        c[v > cut - 0.16] = LIDR
        c[v > cut] = LID
        return c
    return look
def iris_look(state):
    def look(a, b):
        u, v = UC + a * RI, VC + b * RI
        r = np.sqrt(a * a + b * b)
        c = np.tile(IRIS, (len(a), 1))
        c[r > 0.8] = IRISRIM
        c[r < 0.42] = DARK
        c[((a + 0.25) / 0.18) ** 2 + ((b - 0.3) / 0.18) ** 2 < 1] = (1, 1, 1)
        c[v > lid_cut(state, u) - 0.16] = LIDR
        c[v > lid_cut(state, u)] = LID
        c[(v < eye_bottom(u) + 0.02) | (np.abs(u) > 0.95)] = SCL
        c[v < eye_bottom(u)] = LID
        return c
    return look
de = unit(0.45, -0.89, 0.08)
EC = np.array([0.06, -0.122, 0.72])
def frame(n):
    n = np.array(n, float); n /= np.linalg.norm(n)
    right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
    return right, np.cross(n, right)
def eyes(state):
    for side in (1, -1):
        m = np.array([side, 1, 1.0])
        c, n = EC * m, de * m
        right, up = frame(n)
        flip = (lambda f: f) if side == 1 else (lambda f: (lambda u, v: f(-u, v)))
        outline = polygon([(side * u, v) for u, v in (ALMOND if side == 1 else ALMOND[::-1])])
        if state in ("Open", "Fierce"):
            k.decal(c, n, (HE, HE), flip(eye_open(state)), eps=0.006, outline=outline, rings=20, segs=56)
            ic = c + right * side * UC * HE + up * VC * HE
            k.decal(ic, n, (RI * HE, RI * HE), flip(iris_look(state)), eps=0.008, rings=12, segs=40)
        else:
            # closed: the yellow lid's line on the dark mask
            k.decal(c, n, (HE, HE), flip(eye_look(state, eye_open("Open"), LID, SCL, line=0.16)), eps=0.006,
                    outline=outline, rings=20, segs=56)

k.build(voxel=0.0065, smooth=4)
k.eye_states(eyes)
k.export(out)
