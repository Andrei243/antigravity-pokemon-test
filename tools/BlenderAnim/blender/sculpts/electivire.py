# Electivire sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/electivire.py -- <the export's Electivire.glb> <out.glb>
#
# Electabuzz grown (its frame and Elekid's: the line's yellow and dark, loose dark rings for the bands, convex
# patches for the marks, the scowling eye laid one side at a time, white claws): far heavier, a great round dome of a
# head sunk in a shaggy ruff of yellow fur round the shoulders; a dark band across the brow with a dark dot over it;
# red eyes under it; a small yellow snout over a wide grin of white teeth; the two feelers with round yellow tips on
# top of the dome; a barrel of a body with a dark stripe down its middle and jagged dark marks either side; huge
# arms, tufted at the shoulder, the forearms with two broad dark bands, dark hands with thick fingers; short thick
# legs banded dark with a tuft at the ankle over dark feet with white claws; two long dark cables out of the back,
# curling up and out to each side, red at their ends.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, smoothstep, unit, boxy, eye_look, polygon, args

# --- the line's colours (Elekid's), and Electivire's red ------------------------------------------------------------
YELLOW, INK, CLAW = "#F6D232", "#33363C", "#F2F2F0"
WHITE, PUPIL, MOUTH = "#FAFAF6", "#1E1E24", "#3A2228"
RED, IRIS = "#E8452C", "#E0471E"
CY, CI, CW, CP, CM = srgb(YELLOW), srgb(INK), srgb(WHITE), srgb(PUPIL), srgb(MOUTH)


def ring_band(c, axis, radius, length, thick=0.006, nu=73):
    """Elekid's loose ring round a limb (a stripe that keeps its edges), its seam behind."""
    a = unit(*axis)
    ref = np.array([0, 0, 1.0]) if abs(a[2]) < 0.9 else np.array([0, 1.0, 0])
    e1 = np.cross(a, ref); e1 /= np.linalg.norm(e1)
    e2 = np.cross(a, e1)
    back = np.array([0, 1.0, 0]) - a * a[1]
    phase = math.atan2(np.dot(back, e2), np.dot(back, e1)) if np.linalg.norm(back) > 1e-6 else 0.0
    def at(u, v):
        th = phase + 2 * math.pi * u
        r = radius(v)
        off = (v - 0.5) * length
        return (np.asarray(c)[None, None, :] + a[None, None, :] * off[..., None]
                + r[..., None] * (np.cos(th)[..., None] * e1[None, None, :] + np.sin(th)[..., None] * e2[None, None, :]))
    return sheet(at, nu, 3, thick)


def body_ring(z0, width, rx, ry, cy=0.0, lift=0.004, thick=0.006):
    """Elekid's loose band round a body whose cross-section at height z is an ellipse rx(z), ry(z) about (0, cy)."""
    def at(u, v):
        th = 2 * math.pi * u + math.pi / 2
        z = z0 + (v - 0.5) * width
        return np.stack([np.cos(th) * (rx(z) + lift), cy + np.sin(th) * (ry(z) + lift), z], -1)
    return sheet(at, 97, 3, thick)


def ell_radius(c, r):
    def rx(z):
        return r[0] * np.sqrt(np.clip(1 - ((z - c[2]) / r[2]) ** 2, 0.02, 1))
    def ry(z):
        return r[1] * np.sqrt(np.clip(1 - ((z - c[2]) / r[2]) ** 2, 0.02, 1))
    return rx, ry


def convex_patch(k, pts, centre, normal, color, up=(0, 0, 1), eps=0.004, segs=96, rings=12):
    """Elekid's crisp mark of corners on the fused surface: pts in metres in the patch's own plane."""
    pts = np.array(pts, float)
    mid = pts.mean(axis=0)
    rel = pts - mid
    hx, hy = np.abs(rel[:, 0]).max(), np.abs(rel[:, 1]).max()
    n = unit(*normal)
    right = np.cross(np.array(up, float), n); right /= np.linalg.norm(right)
    upv = np.cross(n, right)
    c = np.array(centre, float) + right * mid[0] + upv * mid[1]
    k.decal(c, n, (hx, hy), color, up=up, eps=eps, outline=polygon(rel / np.array([hx, hy])), rings=rings, segs=segs)


SCOWL = [(-1.0, 0.02), (-0.4, 0.42), (0.3, 0.76), (1.0, 0.98), (0.98, 0.3), (0.72, -0.4), (0.22, -0.72),
         (-0.38, -0.68), (-0.82, -0.36)]


def scowl_outline(flip=False):
    return polygon([(-u, v) for u, v in SCOWL] if flip else SCOWL)


def pair_of_eyes(k, E, de, half, look, eps=0.005):
    """Elekid's two eyes laid one at a time, +u outward on both."""
    k.decal(E, de, half, look, eps=eps, outline=scowl_outline(), rings=20, segs=56)
    m = np.array([-1, 1, 1.0])
    k.decal(np.array(E) * m, np.array(de) * m, half, lambda u, v: look(-u, v), eps=eps,
            outline=scowl_outline(flip=True), rings=20, segs=56)


def scowl_top(u):
    return np.interp(u, [-1.0, -0.4, 0.3, 1.0], [0.02, 0.42, 0.76, 0.98])


def scowl_eye(pupil_at=(-0.26, -0.1), pupil_r=(0.4, 0.48), brow=0.2, iris=None, iris_r=None):
    """Elekid's angry eye: white, a solid pupil toward the inner corner (here in a red iris), a heavy top line."""
    def eye(u, v):
        c = np.tile(CW, (len(u), 1))
        if iris is not None:
            c[((u - pupil_at[0]) / iris_r[0]) ** 2 + ((v - pupil_at[1]) / iris_r[1]) ** 2 < 1] = iris
        c[((u - pupil_at[0]) / pupil_r[0]) ** 2 + ((v - pupil_at[1]) / pupil_r[1]) ** 2 < 1] = CP
        c[v > scowl_top(u) - brow] = CI
        return c
    return eye


def claws(k, F, D, r, bone, n=3, length=0.045, base_r=0.014):
    """Elekid's white claws: n pointed tubes along D from a rounded end of radius r at F."""
    D = unit(*D)
    side = np.cross(D, (0, 0, 1.0))
    if np.linalg.norm(side) < 1e-3:
        side = np.array([1.0, 0, 0])
    side /= np.linalg.norm(side)
    upv = np.cross(side, D)
    spots = [(0.0, 0.55), (0.55, -0.35), (-0.55, -0.35)] if n == 3 else [(a, -0.2) for a in np.linspace(-0.7, 0.7, n)]
    for i, (a, b) in enumerate(spots):
        o = (side * a + upv * b) * r * 0.62
        base = F + o + D * r * 0.55
        k.add(f"claw{i}", tube([base, base + D * length * 0.6 + o * 0.15, base + D * length],
                               [base_r, base_r * 0.68, 0.002]), CLAW, bone, fuse=False, mirror=True, surface="shell")


def along(pts, t):
    pts = np.array(pts, float)
    seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    s = np.concatenate([[0], np.cumsum(seg)]) / seg.sum()
    i = min(np.searchsorted(s, t, side="right") - 1, len(seg) - 1)
    f = (t - s[i]) / (s[i + 1] - s[i])
    return pts[i] + (pts[i + 1] - pts[i]) * f, unit(*(pts[i + 1] - pts[i]))


def striped_limb(k, name, pts, radii, bands, bone, mirror=True, width=0.03, lift=0.004):
    """Electabuzz's yellow tube with Elekid's loose dark rings at shares of its length."""
    k.add(name, tube(pts, radii), YELLOW, bone, mirror=mirror)
    for i, t in enumerate(bands):
        c, d = along(pts, t)
        r = np.interp(t, np.linspace(0, 1, len(radii)), radii)
        k.add(f"{name}band{i}", ring_band(c, d, lambda v, r=r: np.full_like(v, r + lift), width), INK, bone,
              fuse=False, mirror=mirror)


def shag(k, name, centre, radii, n, length, bone, z_drop=0.6, mirror=False, start=0.0, span=2 * math.pi, r0=0.034):
    """A shaggy ring of fur: n chunky jags round an ellipse (rx, ry) at centre, pointing out and down."""
    for i in range(n):
        a = start + span * (i + 0.5) / n
        d = np.array([math.cos(a), math.sin(a), 0.0])
        root = np.array(centre) + d * np.array([radii[0], radii[1], 0]) * 0.86
        ln = length * (0.85 + 0.3 * ((i * 7) % 5) / 4)
        tip = root + unit(d[0], d[1], -z_drop) * ln
        k.add(f"{name}{i}", blade([root, (root + tip) / 2, tip], [r0, r0 * 0.6, 0.004], (0, 0, 1), 0.55), YELLOW,
              bone, mirror=mirror)


# --- Electivire's own frame -----------------------------------------------------------------------------------------
BODY_C, BODY_R = np.array([0, 0.0, 0.43]), np.array([0.2, 0.16, 0.2])
H, HR = np.array([0, -0.06, 0.7]), np.array([0.16, 0.15, 0.15])
SHOULDER = np.array([0.24, -0.01, 0.56])
ELBOW = np.array([0.33, -0.05, 0.43])
WRIST = np.array([0.37, -0.11, 0.29])
HAND = np.array([0.385, -0.13, 0.24])
HIP = np.array([0.11, 0.0, 0.22])
BAND_Z = 0.765


def on_head(d, lift=0.0):
    d = unit(*d)
    s = 1 / math.sqrt(((d / HR) ** 2).sum())
    return H + d * (s + lift)


src, out = args()
k = Kit(src, coat="fur")

# The barrel of a body and the dome of a head sunk into it, the ruff between
k.add("body", ell(BODY_C, BODY_R), YELLOW, "body")
k.add("hips", ell((0, 0.01, 0.31), (0.19, 0.15, 0.1)), YELLOW, "body")
k.add("collar", ell((0, -0.02, 0.6), (0.21, 0.17, 0.08)), YELLOW, "body")
k.add("head", ell(H, HR), YELLOW, "head")
k.add("snout", ell((0, -0.2, 0.675), (0.04, 0.034, 0.026), rot=(-15, 0, 0)), YELLOW, "head")
shag(k, "ruff", (0, -0.02, 0.62), (0.22, 0.18), 18, 0.085, "body", z_drop=0.55)

# The feelers on top of the dome, round yellow tips
k.add("feeler", tube([(0.05, -0.07, 0.83), (0.065, -0.08, 0.88), (0.075, -0.085, 0.91)], [0.013, 0.011, 0.011]),
      YELLOW, "head", mirror=True)
k.add("feelertip", ell((0.078, -0.086, 0.922), (0.03, 0.03, 0.03)), YELLOW, "head", mirror=True)

# The arms: a tufted shoulder, a thick upper arm, a wide forearm with two broad dark bands, a dark hand
k.add("shoulder", ell(SHOULDER, (0.095, 0.09, 0.09)), YELLOW, "arm{s}", mirror=True)
for i, (dx, dy) in enumerate([(-0.02, 0.02), (0.03, -0.01), (0.07, 0.03)]):
    b = SHOULDER + np.array([dx, dy, 0.06])
    k.add(f"shtuft{i}", blade([b, b + (0.01, 0.01, 0.04), b + (0.02 + 0.01 * i, 0.02, 0.075)], [0.028, 0.016, 0.003],
                              (0, 1, 0), 0.55), YELLOW, "arm{s}", mirror=True)
striped_limb(k, "arm", [SHOULDER, ELBOW, (ELBOW + WRIST) / 2, WRIST], [0.075, 0.074, 0.082, 0.072], (0.55, 0.85),
             "arm{s}", width=0.05)
k.add("hand", ell(HAND, (0.05, 0.045, 0.045), deform=boxy(2.4)), INK, "arm{s}", mirror=True, surface="skin")
for j, (dx, dy) in enumerate([(-0.03, -0.02), (-0.005, -0.03), (0.02, -0.025), (0.04, -0.005)]):
    b = HAND + np.array([dx, dy, -0.02])
    k.add(f"finger{j}", tube([b, b + (dx * 0.4, dy * 0.6 - 0.01, -0.04), b + (dx * 0.6, dy * 0.6 - 0.025, -0.075)],
                             [0.02, 0.019, 0.017]), INK, "arm{s}", mirror=True, surface="skin")
k.add("thumb", tube([HAND + (-0.04, -0.02, 0.0), HAND + (-0.07, -0.05, -0.02), HAND + (-0.08, -0.07, -0.05)],
                    [0.02, 0.018, 0.016]), INK, "arm{s}", mirror=True, surface="skin")

# The legs: short thick thighs, a dark band, a tuft at the ankle, dark feet with white claws
k.add("thigh", ell((0.12, 0.0, 0.22), (0.095, 0.095, 0.1)), YELLOW, "leg{s}", mirror=True)
striped_limb(k, "shin", [(0.125, 0.0, 0.18), (0.13, -0.005, 0.1)], [0.075, 0.066], (0.45,), "leg{s}", width=0.04)
shag(k, "ankle", (0.13, -0.01, 0.085), (0.06, 0.06), 8, 0.045, "leg{s}", z_drop=0.9, mirror=True, r0=0.022)
k.add("foot", ell((0.13, -0.04, 0.035), (0.07, 0.09, 0.036)), INK, "leg{s}", mirror=True, surface="skin")
claws(k, np.array([0.13, -0.1, 0.03]), (0, -1, -0.15), 0.06, "leg{s}", n=4, length=0.045, base_r=0.013)

# The two cables out of the back, up and out to each side so both show from the front and from behind, red tips
CABLE = [(0.06, 0.12, 0.52), (0.13, 0.2, 0.63), (0.25, 0.23, 0.7), (0.36, 0.19, 0.72), (0.42, 0.14, 0.79)]
k.add("cable", tube(CABLE, [0.03, 0.026, 0.024, 0.023, 0.022]), INK, "cable{s}", fuse=False, mirror=True,
      surface="skin")
tip_d = unit(*(np.array(CABLE[-1]) - np.array(CABLE[-2])))
k.add("cabletip", tube([np.array(CABLE[-1]) - tip_d * 0.005, np.array(CABLE[-1]) + tip_d * 0.04],
                       [0.026, 0.024]), RED, "cable{s}", fuse=False, mirror=True, surface="skin")
k.add("cableroot", ell((0.06, 0.14, 0.52), (0.042, 0.03, 0.042)), INK, "body", fuse=False, mirror=True,
      surface="skin")

# The marks on the body: a dark stripe down the middle, jagged dark teeth either side, stripes across the back
FRONT = (0, -1, -0.02)
convex_patch(k, [(-0.02, 0.56), (0.02, 0.56), (0.022, 0.3), (-0.022, 0.3)], (0, -0.2, 0.0), FRONT, INK)
for s in (1, -1):
    for zc, ln in ((0.5, 0.075), (0.43, 0.085), (0.36, 0.07)):
        pts = [(0.075 * s, zc + 0.03), (0.075 * s, zc - 0.03), ((0.075 + ln) * s, zc - 0.045)]
        convex_patch(k, pts, (0, -0.2, 0.0), unit(0.35 * s, -1, 0), INK, segs=96)
LENS = polygon([(-1, 0), (-0.8, 0.45), (-0.4, 0.82), (0, 1), (0.4, 0.82), (0.8, 0.45), (1, 0),
                (0.8, -0.45), (0.4, -0.82), (0, -1), (-0.4, -0.82), (-0.8, -0.45)])
for zc, hw in ((0.45, 0.15), (0.36, 0.15)):
    k.decal((0, 0.17, zc), unit(0, 1, 0.05), (hw, 0.024), INK, eps=0.003, outline=LENS, rings=10, segs=64)

# The head's band across the brow, round the dome, and the dot over it
rx, ry = ell_radius(H, HR)
k.add("browband", body_ring(BAND_Z, 0.04, rx, ry, cy=H[1], lift=0.003), INK, "head", fuse=False)
dd = unit(0, -1, 1.15)
k.decal(on_head(dd), dd, (0.02, 0.02), INK, eps=0.004, rings=10, segs=48)

# The grin: a wide band of white teeth in a dark rim, a line between upper and lower
def grin(u, v):
    c = np.tile(CI, (len(u), 1))
    teeth = (np.abs(u) < 0.9) & (np.abs(v) < 0.62)
    c[teeth] = CW
    c[teeth & (np.abs(v - 0.0) < 0.1)] = CI
    return c
GRIN = polygon([(-1, 0.3), (-0.6, 0.85), (0, 0.9), (0.6, 0.85), (1, 0.3), (0.9, -0.55), (0.4, -0.95), (-0.4, -0.95),
                (-0.9, -0.55)])
dm = unit(0, -1, -0.35)
k.decal(on_head(dm) + np.array([0, 0, 0.004]), dm, (0.07, 0.024), grin, eps=0.004, outline=GRIN, rings=16, segs=72)

# The eyes: Elekid's scowl with a red iris, just under the band
de = unit(0.45, -0.89, 0.12)
E = on_head(de)
def eyes(state):
    pair_of_eyes(k, E, de, (0.034, 0.026),
                 eye_look(state, scowl_eye(iris=srgb(IRIS), iris_r=(0.56, 0.62), pupil_r=(0.26, 0.32)), CY, CI))

k.build(voxel=0.008, band=0.004)          # the dark hands keep their edge at the wrist
k.eye_states(eyes)
k.export(out)
