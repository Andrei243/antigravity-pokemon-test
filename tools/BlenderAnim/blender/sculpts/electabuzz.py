# Electabuzz sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/electabuzz.py -- <the export's Electabuzz.glb> <out.glb>
#
# Elekid grown (elekid.py's frame: its colours, its loose dark rings for stripes, its bolt of convex patches, its
# scowling eye laid one side at a time, its white claws): a yellow biped with a round pot belly crossed by a great
# dark bolt from the upper right to the lower left; a round head with a muzzle standing forward and a wide open pink
# mouth with two white fangs at its corners; a dark chevron on the brow over scowling eyes; two short pointed ears
# close together on top and two short feelers with round yellow tips on the sides (the plug's prongs grown apart);
# thick arms with dark bands, a jag of fur at each elbow and big round fists; thick legs banded dark over three
# white claws; a thin striped tail curling out behind. The belly is yellow like the rest (the pictures show no
# cream), and the back is crossed by dark stripes.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, smoothstep, unit, boxy, eye_look, polygon, args

# --- the line's colours (Elekid's) ----------------------------------------------------------------------------------
YELLOW, INK, CLAW = "#F6D232", "#33363C", "#F2F2F0"
WHITE, PUPIL, MOUTH = "#FAFAF6", "#1E1E24", "#D2606C"
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
    """Elekid's angry eye: white, a solid pupil toward the inner corner, a heavy dark line along the top edge."""
    def eye(u, v):
        c = np.tile(CW, (len(u), 1))
        if iris is not None:
            c[((u - pupil_at[0]) / iris_r[0]) ** 2 + ((v - pupil_at[1]) / iris_r[1]) ** 2 < 1] = iris
        c[((u - pupil_at[0]) / pupil_r[0]) ** 2 + ((v - pupil_at[1]) / pupil_r[1]) ** 2 < 1] = CP
        c[v > scowl_top(u) - brow] = CI
        return c
    return eye


def claws(k, F, D, r, bone, n=3, length=0.045, base_r=0.014):
    """Elekid's white claws: n pointed tubes along D from a rounded end of radius r at F, the middle one forward."""
    D = unit(*D)
    side = np.cross(D, (0, 0, 1.0))
    if np.linalg.norm(side) < 1e-3:
        side = np.array([1.0, 0, 0])
    side /= np.linalg.norm(side)
    upv = np.cross(side, D)
    spots = [(0.0, 0.55), (0.55, -0.35), (-0.55, -0.35)] if n == 3 else [(a, -0.2) for a in np.linspace(-0.6, 0.6, n)]
    for i, (a, b) in enumerate(spots):
        o = (side * a + upv * b) * r * 0.62
        base = F + o + D * r * 0.55
        k.add(f"claw{i}", tube([base, base + D * length * 0.6 + o * 0.15, base + D * length],
                               [base_r, base_r * 0.68, 0.002]), CLAW, bone, fuse=False, mirror=True, surface="shell")


def along(pts, t):
    """A point and the way on at a share t of a polyline's length."""
    pts = np.array(pts, float)
    seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    s = np.concatenate([[0], np.cumsum(seg)]) / seg.sum()
    i = min(np.searchsorted(s, t, side="right") - 1, len(seg) - 1)
    f = (t - s[i]) / (s[i + 1] - s[i])
    return pts[i] + (pts[i + 1] - pts[i]) * f, unit(*(pts[i + 1] - pts[i]))


def striped_limb(k, name, pts, radii, bands, bone, mirror=True, width=0.03, lift=0.004):
    """A yellow tube through pts with Elekid's loose dark rings at shares of its length (an arm, a shin, a tail)."""
    k.add(name, tube(pts, radii), YELLOW, bone, mirror=mirror)
    for i, t in enumerate(bands):
        c, d = along(pts, t)
        r = np.interp(t, np.linspace(0, 1, len(radii)), radii)
        k.add(f"{name}band{i}", ring_band(c, d, lambda v, r=r: np.full_like(v, r + lift), width), INK, bone,
              fuse=False, mirror=mirror)


# --- Electabuzz's own frame -----------------------------------------------------------------------------------------
BELLY_C, BELLY_R = np.array([0, 0.0, 0.42]), np.array([0.168, 0.15, 0.175])
H, HR = np.array([0, -0.04, 0.668]), np.array([0.112, 0.104, 0.098])
SHOULDER = np.array([0.135, -0.02, 0.53])
ELBOW = SHOULDER + unit(0.72, -0.3, -0.62) * 0.13
WRIST = ELBOW + unit(0.42, -0.8, -0.2) * 0.12
FIST_R = 0.058
HIP = np.array([0.09, 0.0, 0.26])


def on_head(d, lift=0.0):
    d = unit(*d)
    s = 1 / math.sqrt(((d / HR) ** 2).sum())
    return H + d * (s + lift)


src, out = args()
k = Kit(src, coat="fur")

# The pot belly and a chest rising into the head, no neck to speak of
k.add("belly", ell(BELLY_C, BELLY_R), YELLOW, "body")
k.add("chest", ell((0, 0.0, 0.54), (0.14, 0.11, 0.08)), YELLOW, "body")
k.add("head", ell(H, HR), YELLOW, "head")
# the muzzle standing forward, the wide open mouth carved under it
k.add("muzzle", ell((0, -0.118, 0.628), (0.078, 0.06, 0.054), deform=boxy(2.3)), YELLOW, "head")
k.add("jaw", ell((0, -0.1, 0.585), (0.07, 0.055, 0.03)), YELLOW, "head")
k.add("mouth", ell((0, -0.168, 0.603), (0.052, 0.045, 0.026), rot=(-12, 0, 0)), MOUTH, "head", carve=True)
for i, s in enumerate((1, -1)):
    b = np.array([0.04 * s, -0.162, 0.628])
    k.add(f"fang{i}", tube([b, b + (0.002 * s, -0.004, -0.014), b + (0.004 * s, -0.006, -0.026)], [0.009, 0.006, 0.0015]),
          CLAW, "head", fuse=False, surface="shell")

# The ears: two short pointed spikes close together on top, leaning out a little
k.add("ear", blade([(0.026, -0.03, 0.74), (0.034, -0.026, 0.79), (0.044, -0.02, 0.84)], [0.023, 0.016, 0.006],
                   (0, 1, 0), 0.72), YELLOW, "head", mirror=True)
# The feelers: short stalks out of the sides of the crown, round yellow tips
k.add("feeler", tube([(0.07, -0.04, 0.73), (0.1, -0.05, 0.765), (0.122, -0.052, 0.785)], [0.011, 0.009, 0.009]),
      YELLOW, "head", mirror=True)
k.add("feelertip", ell((0.128, -0.053, 0.792), (0.022, 0.022, 0.022)), YELLOW, "head", mirror=True)

# The arms: thick, down and forward, dark bands, a jag of fur at the elbow and a big round fist
striped_limb(k, "arm", [SHOULDER, (SHOULDER + ELBOW) / 2, ELBOW, (ELBOW + WRIST) / 2, WRIST],
             [0.05, 0.05, 0.046, 0.048, 0.05], (0.3, 0.62, 0.84), "arm{s}")
k.add("fist", ell(WRIST + unit(*(WRIST - ELBOW)) * 0.03, (FIST_R, FIST_R * 0.95, FIST_R * 0.92), deform=boxy(2.5)),
      YELLOW, "arm{s}", mirror=True)
for i, (dx, dz, ln) in enumerate([(0.0, 0.03, 0.06), (0.025, -0.0, 0.05)]):
    b = ELBOW + np.array([dx, 0.025, dz])
    k.add(f"tuft{i}", blade([b, b + unit(0.5, 0.6, 0.6) * ln * 0.6, b + unit(0.5, 0.7, 0.5) * ln],
                            [0.02, 0.012, 0.002], (0.6, -0.4, 0.2), 0.5), YELLOW, "arm{s}", mirror=True)

# The legs: thick thighs under the belly, banded shins, feet with three white claws
k.add("thigh", ell((0.1, 0.0, 0.24), (0.078, 0.082, 0.095)), YELLOW, "leg{s}", mirror=True)
striped_limb(k, "shin", [(0.104, 0.0, 0.2), (0.108, -0.005, 0.12), (0.11, -0.01, 0.05)], [0.056, 0.05, 0.044],
             (0.42, 0.78), "leg{s}")
k.add("foot", ell((0.11, -0.045, 0.03), (0.056, 0.082, 0.032)), YELLOW, "leg{s}", mirror=True)
claws(k, np.array([0.11, -0.1, 0.03]), (0, -1, -0.15), 0.05, "leg{s}", length=0.04, base_r=0.012)

# The tail: thin, striped, from low behind back and curling up and out to the model's +X side
TAIL = [(0, 0.12, 0.32), (0.03, 0.22, 0.25), (0.1, 0.3, 0.22), (0.17, 0.33, 0.27), (0.2, 0.32, 0.34)]
striped_limb(k, "tail", TAIL, [0.036, 0.03, 0.025, 0.021, 0.018], (0.25, 0.45, 0.65, 0.85), "tail", mirror=False,
             width=0.026)

# The great bolt across the belly, from the upper right (+X) to a point at the lower left: three convex pieces cast
# straight from the front
FRONT = (0, -1, -0.05)
for piece in ([(0.165, 0.535), (0.175, 0.49), (0.02, 0.425), (-0.03, 0.47)],
              [(-0.04, 0.475), (0.075, 0.47), (0.085, 0.42), (-0.03, 0.425)],
              [(0.02, 0.45), (0.085, 0.425), (-0.14, 0.3)]):
    convex_patch(k, piece, (0, -0.15, 0.0), FRONT, INK, segs=96)

# Stripes across the back: lenses cast from behind, narrower than the back, crisp
LENS = polygon([(-1, 0), (-0.8, 0.45), (-0.4, 0.82), (0, 1), (0.4, 0.82), (0.8, 0.45), (1, 0),
                (0.8, -0.45), (0.4, -0.82), (0, -1), (-0.4, -0.82), (-0.8, -0.45)])
for zc, hw in ((0.53, 0.11), (0.46, 0.13), (0.39, 0.13)):
    k.decal((0, 0.15, zc), unit(0, 1, 0.08), (hw, 0.017), INK, eps=0.003, outline=LENS, rings=10, segs=64)

# The chevron on the brow: two dark bars meeting in a point between the eyes
NB = unit(0, -1, 0.4)
for s in (1, -1):
    pts = [(0.0, 0.0), (0.0, 0.028), (0.072 * s, 0.078), (0.082 * s, 0.052)]
    convex_patch(k, pts, (0, -0.105, 0.674), NB, INK, segs=96)

# The eyes: Elekid's scowl, a little larger against the head
de = unit(0.42, -0.9, 0.0)
E = on_head(de) + np.array([0, 0, -0.004])
def eyes(state):
    pair_of_eyes(k, E, de, (0.033, 0.026), eye_look(state, scowl_eye(), CY, CI))

k.build(voxel=0.0065)
k.eye_states(eyes)
k.export(out)
