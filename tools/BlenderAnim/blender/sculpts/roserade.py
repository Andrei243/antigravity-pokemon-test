# Roserade sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/roserade.py -- <the export's Roserade.glb> <out.glb>
#
# Grown from Roselia's script: the same greens and pale limbs, the same way of painting a hood and laying a mouth.
# From the references: a slim green figure, its chest green and its belly, hips and legs pale; legs tapering to yellow
# points; a yellow ring at the neck; a cape of two long leaves hanging from the back of the neck and parting like a
# tailcoat's tails to the knees; a green face with a dark green mask round sly yellow eyes (a red lid line over a red
# iris); a small smirk; a great white bloom for hair, one smooth mass fused to the head (a crisp painted hairline, no
# seam), broad petals swept out to the +X side and back, a few points off its top; leafy pale hands, each holding a
# bouquet of three round roses (a ball in shells of petal, a spiral on its face), red in the hand on -X, blue on +X.
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
    return inside(P[:, 2] - edge, 0.006) * (P[:, 2] < 0.6)
k.paint(belly, PALE)
k.add("neck", tube([(0, 0.0, 0.58), (0, 0.0, 0.66)], [0.03, 0.03]), GREEN, "head")

# the yellow ring at the neck
k.add("collar", torus((0, 0.0, 0.602), 0.034, 0.011), YELLOW, "body", fuse=False, surface="leaf")

# --- the cape: two long leaves hanging from the back of the neck, curving out and parting like a tailcoat's tails,
# to about the knees; dark green outside, a lighter green underside facing the body
def tail(s, inset, lift):
    def at(u, v):
        v = 0.985 * v
        c = 2 * u - 1
        x0 = s * (0.022 + 0.085 * v + 0.045 * v * v)
        y0 = 0.046 + 0.085 * v - 0.025 * v * v
        z0 = 0.588 - 0.33 * v
        r = np.stack([x0, y0], -1); r = r / np.linalg.norm(r, axis=-1, keepdims=True)
        across = np.stack([-r[..., 1], r[..., 0]], -1)
        w = inset * (0.014 * (1 - v) + 0.062 * np.sin(np.pi * v ** 0.75))
        bow = 0.022 * (1 - c * c) * np.sin(np.pi * v ** 0.8)        # the leaf's middle stands out, its edges in
        out = bow + lift - 0.004 * c * c
        x = x0 + across[..., 0] * c * w + r[..., 0] * out
        y = y0 + across[..., 1] * c * w + r[..., 1] * out
        z = z0 + 0.012 * c * s * v                                   # a little twist down the length
        return np.stack([x, y, z], -1)
    return at
for s, side in ((1, "R"), (-1, "L")):
    k.add(f"cape{side}", sheet(tail(s, 1.0, 0.0), 14, 34, 0.011), CAPE, "cape", fuse=False)
    k.add(f"capein{side}", sheet(tail(s, 0.86, -0.006), 14, 34, 0.004), "#3E9A48", "cape", fuse=False)

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


def rose(name, c, axis, R, col, dark, bone, turn):
    """A tidy rose: a round ball wrapped in a few shells of petal lying close on it, each starting a little further
    from the face than the one inside it so their rims spiral, and a thin dark spiral drawn on the face."""
    z = unit(*axis)
    x = np.cross(z, (0, 0, 1.0)); x /= np.linalg.norm(x)
    y = np.cross(z, x)
    c = np.array(c, float)
    k.add(f"{name}ball", ell(c, (0.97 * R, 0.97 * R, 0.97 * R), sub=3), col, bone, fuse=False)
    for j in range(4):
        a0 = turn + j * 1.75
        rj = R * (1.0 + 0.03 * j)
        p0 = 0.55 + 0.2 * j
        shade = col * (0.94 if j % 2 else 1.0)
        def at(u, v, a0=a0, rj=rj, p0=p0):
            cu = 2 * u - 1
            a = a0 + 2.5 * u
            ph = p0 + 0.45 * cu * cu + (2.55 - p0 - 0.45 * cu * cu) * v
            lip = 1 + 0.07 * np.clip(1 - v / 0.25, 0, 1)            # the rim turned out a little
            d = (np.sin(ph) * np.cos(a))[..., None] * x + (np.sin(ph) * np.sin(a))[..., None] * y \
                + np.cos(ph)[..., None] * z
            return c + d * (rj * lip)[..., None]
        k.add(f"{name}pet{j}", sheet(at, 18, 12, 0.005), shade, bone, fuse=False)
    t = np.linspace(0, 3.2 * math.pi, 40)
    rr = 0.12 + 0.6 * t / t[-1]
    pts = [c + R * 1.012 * (q * math.cos(a + turn) * x + q * math.sin(a + turn) * y + math.sqrt(1 - q * q) * z)
           for q, a in zip(rr, t)]
    k.add(f"{name}curl", tube(pts, [0.0022] * len(pts), seg=8, per=2), dark, bone, fuse=False)


def bouquet(name, hand, s, col, dark, bone):
    """Three round roses in a tight triangle before the hand, facing forward and out."""
    axis = unit(0.35 * s, -1, 0.15)
    xx = np.cross(axis, (0, 0, 1.0)); xx /= np.linalg.norm(xx)
    yy = np.cross(axis, xx)
    mid = np.array(hand) + np.array((0.03 * s, -0.05, 0.03))
    for i, a in enumerate((90, 210, 330)):
        p = mid + 0.04 * (math.cos(math.radians(a)) * yy + math.sin(math.radians(a)) * xx)
        rose(f"{name}{i}", p, axis, 0.032, col, dark, bone, 1.3 * i)

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

# --- the white bloom of hair: one smooth mass fused to the head, a rounded cap with broad petals sweeping back and
# out to the +X side, a few points rising off its top (all fused, so it joins the head with no seam)
def bl(name, shape):
    k.add(name, shape, WHITE, "head", surface="leaf")
bl("bloom", ell((0.005, 0.012, 0.77), (0.104, 0.1, 0.078)))
bl("bloomTop", ell((-0.01, 0.018, 0.815), (0.09, 0.085, 0.068)))
bl("bloomBack", ell((0.0, 0.055, 0.75), (0.088, 0.065, 0.072)))
for i, (base, d, L, w) in enumerate([
        ((0.05, 0.025, 0.79), (1, 0.55, -0.3), 0.2, 0.075),      # the big sweep out to +X and back
        ((0.065, -0.01, 0.76), (1, -0.1, -0.7), 0.12, 0.05),     # its lower petal, by the +X cheek
        ((0.02, 0.065, 0.79), (0.3, 1, -0.5), 0.14, 0.07),       # back
        ((-0.055, 0.035, 0.79), (-1, 0.45, -0.3), 0.11, 0.055)]):  # the smaller -X side
    bl(f"bloomPetal{i}", leaf(base, d, L, w, 0.034))
for i, (base, tip, r) in enumerate([
        ((-0.04, 0.02, 0.84), (-0.085, 0.01, 0.935), 0.04),
        ((0.0, 0.025, 0.845), (-0.01, 0.02, 0.94), 0.036),
        ((0.04, 0.03, 0.83), (0.07, 0.03, 0.9), 0.03)]):
    mid = np.array(base) * 0.45 + np.array(tip) * 0.55
    bl(f"bloomPoint{i}", blade([base, mid, tip], [r, r * 0.55, 0.006], (0, 1, 0), 0.62))

HEADR = np.array([0.078, 0.074, 0.075])
def hairline(P):
    """Above it the bloom: over the brow in front, lower at the sides, lowest behind."""
    q = P - H
    th = np.abs(np.arctan2(q[:, 0], -q[:, 1]))
    return q[:, 2] - (0.031 - 0.02 * smoothstep(0.6, 1.5, th) - 0.032 * smoothstep(1.7, 2.7, th))
def hair(P):
    q = P - H
    far = np.linalg.norm(q / HEADR, axis=1) > 1.18
    above = inside(-hairline(P), 0.006) * (np.linalg.norm(q, axis=1) < 0.13)
    return np.maximum(above, far * (P[:, 2] > 0.665)) * (P[:, 2] > 0.6)
def skin(P):
    """The head's own green back under the hairline, where the fused bloom's white spread onto it."""
    q = P - H
    return inside(hairline(P), 0.006) * (np.linalg.norm(q, axis=1) < 0.105) * (P[:, 2] > 0.63)
k.paint(skin, GREEN)

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
