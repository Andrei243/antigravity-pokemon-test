# Roserade sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/roserade.py -- <the export's Roserade.glb> <out.glb>
#
# Grown from Roselia's script: the same greens and pale limbs, the same roses of loose cupped petals (now three to a
# bouquet, closed tighter, as round as the drawing's), the same way of painting a hood and laying a mouth. From the
# references: a slim green figure, its chest green and its belly, hips and legs pale; legs tapering to yellow points;
# a yellow ring at the neck; a dark green cape of leaf hanging from the shoulders with a pointed hem; a green face
# with a dark green mask round sly yellow eyes (a red lid line over a red iris); a small smirk; a great white bloom of
# petals for hair, swept out to the +X side, points rising off its top; leafy pale hands, a bouquet of red roses in
# the hand on -X and of blue in the hand on +X.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, leaf, sheet, srgb, inside, smoothstep, tri, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="leaf")

GREEN, PALE, CAPE, MASK = "#4FB04A", "#C6E59C", "#2E8040", "#245C30"
WHITE, YELLOW, INKH = "#F6F6F0", "#F0D24A", "#24301C"
H = np.array([0.0, -0.004, 0.70])        # the head's centre
E_ = 0.0035                              # a crisp paint's edge, under a voxel or so


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


# --- legs and body -------------------------------------------------------------------------------------------
k.add("leg", tube([(0.042, 0.0, 0.37), (0.052, -0.008, 0.25), (0.054, -0.018, 0.15), (0.05, -0.024, 0.075)],
                  [0.046, 0.034, 0.022, 0.013]), PALE, "leg{s}", mirror=True)
# the feet: yellow points
k.add("foot", tube([(0.05, -0.024, 0.09), (0.05, -0.034, 0.04), (0.05, -0.048, 0.004)], [0.014, 0.01, 0.002]),
      YELLOW, "leg{s}", fuse=False, mirror=True)
k.add("hips", ell((0, 0.0, 0.38), (0.078, 0.062, 0.06)), PALE, "body")
def slim(V):
    V = V.copy()
    t = np.clip(V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.3 * t ** 2; V[:, 1] *= 1 - 0.3 * t ** 2
    return V
k.add("torso", ell((0, 0.0, 0.48), (0.074, 0.062, 0.13), deform=slim), GREEN, "body")
# the pale belly: the lower body pale, rising to a point up the front
def belly(P):
    edge = 0.43 + 0.07 * np.clip(1 - np.abs(P[:, 0]) / 0.045, 0, 1) * (P[:, 1] < -0.02)
    return inside(P[:, 2] - edge, E_) * (P[:, 2] < 0.6)
k.paint(belly, PALE)
k.add("neck", tube([(0, 0.0, 0.58), (0, 0.0, 0.66)], [0.03, 0.03]), GREEN, "head")

# the yellow ring at the neck
k.add("collar", torus((0, 0.0, 0.602), 0.034, 0.011), YELLOW, "body", fuse=False, surface="leaf")

# --- the cape: dark green, from behind the shoulders down past the hips, its hem in points ------------------------
def cape(u, v):
    u = 0.015 + 0.97 * u
    s = 2 * u - 1
    w = 0.05 + 0.16 * v
    L = 0.25 + 0.07 * (1 - np.abs(tri(s * 1.25 + 0.5)))   # five broad points at the hem
    x = s * w
    # its top hugs the back of the neck under the ring, wrapping toward the shoulders; then it bellies out over the
    # back and hangs, its sides drawn forward
    y = 0.034 + 0.11 * v - 0.04 * v * v - 0.012 * s * s - 0.05 * s * s * v
    z = 0.592 - L * v
    return np.stack([x, y, z], -1)
k.add("cape", sheet(cape, 30, 20, 0.012), CAPE, "cape", fuse=False)

# --- the arms, leafy hands and bouquets ----------------------------------------------------------------------
for s in (1, -1):
    side = "R" if s > 0 else "L"
    sh = np.array((0.06 * s, 0.0, 0.565))
    hand = np.array((0.17 * s, -0.075, 0.49 + (0.01 if s < 0 else -0.01)))
    k.add(f"arm{side}", tube([sh, (0.12 * s, -0.02, 0.52), hand], [0.021, 0.017, 0.016]), GREEN, f"arm{side}",
          fuse=False)
    k.add(f"hand{side}", ell(hand, (0.02, 0.018, 0.02)), PALE, f"arm{side}", fuse=False)
    # three pale leaf fingers cupping the bouquet's foot
    for i, d in enumerate(((0.3 * s, -0.6, 0.75), (0.9 * s, -0.6, 0.15), (0.45 * s, -0.6, -0.65))):
        k.add(f"leafhand{side}{i}", leaf(hand, d, 0.055, 0.02, 0.008), PALE, f"arm{side}", fuse=False)


def rose(name, c, axis, R, col, dark, bone):
    """Roselia's rose closed tighter: loose cupped petals in rings round a dark heart, its face along axis."""
    z = unit(*axis)
    x = np.cross(z, (0, 0, 1.0)); x /= np.linalg.norm(x)
    y = np.cross(z, x)
    c = np.array(c, float)
    k.add(f"{name}heart", ell(c + z * 0.2 * R, (0.34 * R, 0.34 * R, 0.36 * R), sub=3), dark, bone, fuse=False)
    k.add(f"{name}back", ell(c - z * 0.2 * R, (0.8 * R, 0.8 * R, 0.75 * R), sub=3), col * 0.85, bone, fuse=False)
    for ring, (n, rho, h, w, hh, tilt, off, colr) in enumerate([
            (3, 0.28, 0.24, 0.32, 0.4, 14, 0.3, dark),
            (5, 0.56, 0.08, 0.44, 0.5, 30, 0.1, col),
            (6, 0.82, -0.12, 0.46, 0.52, 48, 0.55, col)]):
        for i in range(n):
            a = 2 * math.pi * (i + off) / n
            r = math.cos(a) * x + math.sin(a) * y
            up = math.cos(math.radians(tilt)) * z + math.sin(math.radians(tilt)) * r
            rad = math.cos(math.radians(tilt)) * r - math.sin(math.radians(tilt)) * z
            tan = np.cross(up, rad)
            def cup(V, i=i):
                V = V.copy()
                V[:, 1] -= 0.9 * V[:, 0] ** 2
                V[:, 0] *= 1 + 0.08 * np.sin(7 * V[:, 2] + i)
                return V
            V, F = ell((0, 0, 0), (w * R, 0.11 * R, hh * R), deform=cup, sub=2)
            V = V @ np.stack([tan, rad, up], axis=1).T + c + r * rho * R + z * h * R
            k.add(f"{name}p{ring}{i}", (V, F), colr, bone, fuse=False)


def bouquet(name, hand, s, col, dark, bone):
    """Three roses in a little triangle before the hand, facing forward and out."""
    axis = unit(0.35 * s, -1, 0.15)
    xx = np.cross(axis, (0, 0, 1.0)); xx /= np.linalg.norm(xx)
    yy = np.cross(axis, xx)
    mid = np.array(hand) + np.array((0.03 * s, -0.055, 0.03))
    for i, a in enumerate((90, 210, 330)):
        p = mid + 0.05 * (math.cos(math.radians(a)) * yy + math.sin(math.radians(a)) * xx)
        rose(f"{name}{i}", p, axis, 0.048, col, dark, bone)

bouquet("red", (-0.17, -0.075, 0.5), -1, srgb("#E2505E"), srgb("#A82A3C"), "armL")
bouquet("blue", (0.17, -0.075, 0.48), 1, srgb("#3B7FE2"), srgb("#1E4EA8"), "armR")

# --- the head ----------------------------------------------------------------------------------------------
def chin(V):
    V = V.copy()
    t = np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.25 * t ** 1.5
    V[:, 2] -= 0.12 * t ** 2 * np.clip(-V[:, 1], 0, 1)          # drawn down a little to a soft point in front
    return V
k.add("head", ell(H, (0.078, 0.074, 0.075), deform=chin), GREEN, "head")

# the white hair's line round the head: over the brow in front, lower at the sides and back
def hair(P):
    q = P - H
    th = np.abs(np.arctan2(q[:, 0], -q[:, 1]))
    edge = 0.042 - 0.05 * smoothstep(0.9, 2.2, th) + 0.2 * (th < 1.2)    # in front the mask runs up under the bloom
    near = np.linalg.norm(q, axis=1) < 0.11
    return inside(edge - q[:, 2], E_) * near * (P[:, 2] > 0.64)

# the dark green mask round the eyes, in coordinates round the head's upright axis (angle times radius, height)
def mask(P):
    q = P - H
    ang = np.arctan2(q[:, 0], -q[:, 1])
    au = np.abs(ang) * 0.076
    zr = q[:, 2]
    top = 0.09                      # up under the bloom, whose rim is the crisp edge
    low = -0.016 - 0.012 * np.sin(np.clip(au / 0.06, 0, 1) * math.pi * 0.85) + 0.02 * smoothstep(0.045, 0.072, au)
    front = (q[:, 1] < 0.0) & (np.linalg.norm(q, axis=1) < 0.11)
    m = inside(zr - top, E_) * inside(low - zr, E_) * inside(au - 0.075, E_)
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
mdir = unit(0, -1, -0.55)
k.decal(H + mdir * 0.078, mdir, (0.02, 0.011), smirk, eps=0.004, rings=8, segs=36)

# --- the white bloom of hair: loose petals over the crown, swept out to the +X side, points off its top -------
def flare(V):
    """The bloom's lower rim flared out and turned up, so it stands proud of the brow instead of grazing it."""
    V = V.copy()
    t = np.clip(-V[:, 2] - 0.2, 0, 1)
    V[:, 0] *= 1 + 0.25 * t; V[:, 1] *= 1 + 0.25 * t
    V[:, 2] += 0.35 * t ** 2
    return V
k.add("bloom", ell((0, 0.012, 0.8), (0.1, 0.098, 0.077), deform=flare), WHITE, "head", fuse=False, surface="leaf")
k.add("bloomB", ell((0, 0.06, 0.75), (0.085, 0.06, 0.07)), WHITE, "head", fuse=False, surface="leaf")
# the side lobes, the +X one larger and swept out and down
k.add("lobeR", ell((0.1, 0.03, 0.775), (0.075, 0.06, 0.05), rot=(0, 25, 0)), WHITE, "head", fuse=False,
      surface="leaf")
k.add("lobeR2", ell((0.16, 0.04, 0.74), (0.045, 0.04, 0.032), rot=(0, 40, 0)), WHITE, "head", fuse=False,
      surface="leaf")
k.add("lobeL", ell((-0.085, 0.03, 0.79), (0.06, 0.055, 0.045), rot=(0, -20, 0)), WHITE, "head", fuse=False,
      surface="leaf")
# petals' points: off the top, and off the +X lobe's tip
# broad soft petal tips, flat to the front, a few off the top and two off the +X lobe
for i, (base, tip, r) in enumerate([
        ((-0.03, 0.03, 0.83), (-0.06, 0.05, 0.915), 0.045),
        ((0.02, 0.035, 0.835), (0.035, 0.06, 0.925), 0.045),
        ((-0.07, 0.04, 0.82), (-0.13, 0.06, 0.875), 0.035),
        ((0.17, 0.05, 0.75), (0.235, 0.07, 0.725), 0.03),
        ((0.15, 0.05, 0.725), (0.2, 0.08, 0.675), 0.026),
        ((0.0, 0.1, 0.78), (0.0, 0.16, 0.76), 0.035)]):
    k.add(f"spike{i}", blade([base, (np.array(base) * 0.4 + np.array(tip) * 0.6), tip], [r, r * 0.75, 0.004],
                             (0, 1, 0) if i < 3 else (0, 0, 1), 0.35), WHITE, "head", fuse=False, surface="leaf")

# --- the eyes: sly yellow almonds, a heavy red lid along the top sloping down to the nose, a red iris and a dark
# pupil. u runs outward, v up.
HE = 0.026
SL = 0.32
def eye_top(u):
    return 0.28 + SL * u
def eye_bottom(u):
    return eye_top(u) - 0.85 * (1 - u * u) ** 0.75
ALMOND = [(u, eye_top(u)) for u in np.linspace(1, -1, 9)] + [(u, eye_bottom(u)) for u in np.linspace(-0.92, 0.92, 13)]
SCL, LIDR, IRIS, IRISRIM, DARK, LID = (srgb("#F4D64A"), srgb("#B42030"), srgb("#D8303C"), srgb("#8A1424"),
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
        c[v > cut - 0.2] = LIDR
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
        c[v > lid_cut(state, u) - 0.2] = LIDR
        c[v > lid_cut(state, u)] = LID
        c[(v < eye_bottom(u) + 0.02) | (np.abs(u) > 0.95)] = SCL
        c[v < eye_bottom(u)] = LID
        return c
    return look
de = unit(0.48, -0.87, 0.06)
EC = np.array([0.036, -0.068, 0.706])
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
            # closed: the red lid's line on the dark mask
            k.decal(c, n, (HE, HE), flip(eye_look(state, eye_open("Open"), LID, LIDR, line=0.16)), eps=0.006,
                    outline=outline, rings=20, segs=56)

k.build(voxel=0.006, smooth=4)
k.eye_states(eyes)
k.export(out)
