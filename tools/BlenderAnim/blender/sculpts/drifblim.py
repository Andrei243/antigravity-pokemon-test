# Drifblim sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/drifblim.py -- <the export's Drifblim.glb> <out.glb>
#
# Grown from Drifloon's script: the same fur coat, the same fused cloud with its curl, solid patches for the eyes and
# the face's yellow mark, thin loose pieces for what hangs.
# From the references: a great round balloon of dark purple, its panels parted by darker seams running from the top
# down; its lower third pale lavender, the pale band's top edge in round scallops that dip at each seam; a yellow X
# for a mouth in the middle of the face; two small red eyes with a darker rim, set wide and higher than the X; the
# white cloud on top, over to +X and back, its thin white curl rising from its -X end; a red frill of paper points
# under the balloon; four long flat ribbon arms from the pale underside, the front two hanging down and out, the back
# two spread wide, each pale lavender above and yellow below, ending in a ragged edge of three points.
# The skeleton: body (0, 0, 0.713), armR/armL (+-0.1, -0.1, 0.493), backArmR/backArmL (+-0.1, 0.1, 0.493).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

PURPLE, SEAM, PALE, PALESEAM, CLOUD, SHADE = "#6E5C9E", "#4A3C74", "#CBBEE3", "#A797C6", "#F7F8FB", "#CDD6E6"
YELLOW, RED, REDDARK, RIBBON = "#F6CC34", "#E6475F", "#B42C48", "#D3C7EA"
C = np.array([0.0, 0.0, 0.71])
R = np.array([0.245, 0.238, 0.25])
BOTTOM = C[2] - R[2]

# --- the balloon ---------------------------------------------------------------------------------------------------
k.add("balloon", ell(C, R), PURPLE, "body")

def ang(P):
    return np.arctan2(P[:, 0] - C[0], -(P[:, 1] - C[1]))          # 0 at the front, + toward +X
def hrad(P):
    return np.sqrt((P[:, 0] - C[0]) ** 2 + (P[:, 1] - C[1]) ** 2)

SEAMS = math.radians(45)
def seamdist(P):
    """How far a point is from the nearest seam, along the surface round the upright axis."""
    a = ang(P) - math.radians(22.5)
    a = (a + SEAMS / 2) % SEAMS - SEAMS / 2
    return np.abs(a) * hrad(P)

def palebelow(P):
    """The pale band's scalloped top: highest between seams, dipping at each."""
    a = ang(P) - math.radians(22.5)
    t = ((a % SEAMS) / SEAMS) * 2 - 1                                  # -1..1 across a panel
    h = BOTTOM + 0.125 + 0.05 * np.sqrt(np.clip(1 - t * t, 0, 1))
    return h - P[:, 2]

E = 0.0035
k.paint(lambda P: inside(-palebelow(P), E), PALE, on=["balloon"])
# the seams: dark on the purple, a soft lavender on the pale band, from under the cloud down to the knot
k.paint(lambda P: inside(seamdist(P) - 0.0045, E) * inside(palebelow(P) + 0.004, E) * (P[:, 2] < C[2] + 0.22),
        SEAM, on=["balloon"])
k.paint(lambda P: inside(seamdist(P) - 0.0045, E) * inside(-palebelow(P), E) * (hrad(P) > 0.04),
        PALESEAM, on=["balloon"])

# --- the cloud: fused puffs on the top, over to +X and a little back, the curl from its -X end ----------------------
CC = np.array([0.075, 0.045, 0.94])
PUFFS = [(-0.1, 0.0, -0.01, 0.055), (-0.055, -0.015, 0.035, 0.06), (0.0, -0.01, 0.05, 0.062),
         (0.055, 0.0, 0.03, 0.056), (0.095, 0.005, -0.02, 0.046), (0.045, -0.03, -0.035, 0.052),
         (-0.03, -0.035, -0.035, 0.054), (-0.1, 0.015, -0.05, 0.042), (0.0, 0.045, 0.005, 0.064),
         (-0.06, 0.04, 0.0, 0.054), (0.06, 0.04, -0.015, 0.052), (0.0, 0.0, 0.0, 0.07)]
for i, (dx, dy, dz, r) in enumerate(PUFFS):
    k.add(f"cloud{i}_", ell(CC + np.array([dx, dy, dz]), (r, r * 0.9, r * 0.85)), CLOUD, "body")
k.paint(lambda P: smoothstep(CC[2] - 0.0, CC[2] - 0.06, P[:, 2]) * 0.8, SHADE, on=["cloud"])
k.add("curl", blade([(0.005, 0.05, 0.97), (-0.035, 0.05, 1.005), (-0.06, 0.045, 1.04), (-0.07, 0.04, 1.065)],
                    [0.02, 0.015, 0.008, 0.002], (0, 1, 0), 0.45), CLOUD, "body", fuse=False)

# --- the red frill under the balloon -------------------------------------------------------------------------------
KB = np.array([0.0, 0.0, BOTTOM + 0.012])
k.add("knot", ell(KB, (0.045, 0.045, 0.03)), PALE, "body")
for i in range(8):
    a = math.radians(22.5 + 45 * i)
    d = np.array([math.sin(a), -math.cos(a), 0])
    p0 = KB + d * 0.02 + (0, 0, -0.006)
    p1 = KB + d * 0.065 + (0, 0, -0.06)
    k.add(f"frill{i}", blade([p0, (p0 + p1) / 2, p1], [0.026, 0.018, 0.002], d, 0.35),
          RED if i % 2 == 0 else REDDARK, "body", fuse=False)

# --- the four ribbon arms ------------------------------------------------------------------------------------------
def ribbon(path, w0, w1, face, start, end, thick, flare=1.0):
    """A flat ribbon along a Bezier path (3 points), its face toward face, from v start(u) to v end(u)."""
    p0, p1, p2 = [np.array(p, float) for p in path]
    fn = np.array(face, float); fn /= np.linalg.norm(fn)
    def at(u, v):
        v0, v1 = start(u), end(u)
        v = v0 + (v1 - v0) * (0.015 + 0.97 * v)
        P = ((1 - v) ** 2)[..., None] * p0 + (2 * (1 - v) * v)[..., None] * p1 + (v * v)[..., None] * p2
        T = (2 * (1 - v))[..., None] * (p1 - p0) + (2 * v)[..., None] * (p2 - p1)
        T /= np.linalg.norm(T, axis=-1, keepdims=True)
        S = np.cross(T, fn); S /= np.linalg.norm(S, axis=-1, keepdims=True)
        w = (w0 + (w1 - w0) * v ** 0.8) * flare
        return P + S * ((2 * u - 1) * w)[..., None]
    return sheet(at, 9, 40, thick)

ARMS = [("arm", -0.1, (0.16, -0.18, 0.38), (0.25, -0.24, 0.22), (0.2, -1, 0.2)),
        ("backArm", 0.1, (0.26, 0.14, 0.42), (0.43, 0.19, 0.29), (0.4, -1, 0.3))]
for bone, y, mid, end, face in ARMS:
    for s, sd in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1])
        path = [np.array((0.1, y, 0.5)) * m, np.array(mid) * m, np.array(end) * m]
        fc = np.array(face) * m
        name = f"{bone}{sd}"
        k.add(f"ribbon{sd}{bone}", ribbon(path, 0.026, 0.046, fc, lambda u: 0 * u, lambda u: 0 * u + 0.66, 0.012),
              RIBBON, name, fuse=False)
        # the yellow tip: begins in a point up into the pale, ends in three ragged points
        k.add(f"tip{sd}{bone}", ribbon(path, 0.026, 0.046, fc, lambda u: 0.52 + 0.13 * np.abs(2 * u - 1),
                                       lambda u: 1 - 0.1 * np.abs(np.sin(2 * np.pi * u)), 0.018, 1.04),
              YELLOW, name, fuse=False)

# --- the X: a crisp yellow patch in the middle of the face -----------------------------------------------------
def x_outline(w=0.3):
    """An X of four square-ended arms of half-width w; each inner corner sits on the line between two arms."""
    pts = []
    for q in range(4):
        a = math.pi / 4 + q * math.pi / 2
        d = np.array([math.cos(a), math.sin(a)]); s = np.array([-d[1], d[0]])
        corner = np.array([math.cos(a - math.pi / 4), math.sin(a - math.pi / 4)]) * w * math.sqrt(2)
        pts += [corner, d - s * w, d + s * w]
    return polygon(np.array(pts))
dx = unit(0.0, -1, -0.02)
k.decal(C + dx * R, dx, (0.068, 0.068), YELLOW, eps=0.004, outline=x_outline(0.3), rings=16, segs=120)

k.build(voxel=0.006, smooth=3)

# --- the eyes: small red ovals in a darker rim, set wide and above the X ------------------------------------------
CE, CR, CL, CLINE = srgb(RED), srgb(REDDARK), srgb(PURPLE), srgb("#D3C6EA")
def eye(u, v):
    c = np.tile(CE, (len(u), 1))
    c[u * u + v * v > 0.62] = CR
    return c
def eyes(state):
    for s in (1, -1):
        dn = unit(s * 0.62, -0.75, 0.3)
        look = eye_look(state, eye, CL, CLINE, line=0.2)
        lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
        k.decal(C + dn * R, dn, (0.019, 0.026), lk, eps=0.004, rings=20, segs=56)
k.eye_states(eyes)
k.export(out)
