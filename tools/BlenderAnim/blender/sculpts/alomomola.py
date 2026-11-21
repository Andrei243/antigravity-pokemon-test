# Alomomola sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/alomomola.py -- <the export's Alomomola.glb> <out.glb>
#
# From the references: a big pink sunfish swimming nose up, its body a plump oval disc flat from side to side, its
# long axis leaning back from the nose some 25 degrees; a small face at the nose: a slight snout with a small curling
# mouth line under its tip, and on each face a small gold eye with a black pupil under a thick navy lid that flicks
# back; a darker pink flipper lying flat on each face behind the eye, its back edge scalloped like a heart's lobes; a
# darker pink dorsal fin of two round lobes rising up and back from the top, a long tail fin of two lobes hanging down
# under the front of the belly and a small fin at the bottom of the back, every fin's lobes tipped pale.
# The game never sees a model side-on, so the disc's plane is turned 45 degrees about the upright (as luvdisc.py):
# its nose edge to the front and the model's -X, one face toward the front three-quarter camera and the other toward
# the camera behind. The joints: body (0, 0, 0.395), head (0, 0, 0.565), tail (0, 0, 0.215), finR/L (+-0.11, 0, 0.375).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, inside, unit, eye_look, polygon, args

PINK, FIN, TIP, FLIP, NAVY, GOLD, INK, MOUTH = ("#EFA2B6", "#E3869F", "#F8CDD7", "#D86E8E", "#1F3466", "#E9B531",
                                                "#15151C", "#7A3550")

# --- the disc's frame: a from the nose edge back across the disc, b up, n out of its +X face --------------------------
TURN = math.radians(45)
A = np.array([math.sin(TURN), math.cos(TURN), 0.0])
Z = np.array([0.0, 0.0, 1.0])
N = np.cross(A, Z)
O = np.array([0.0, 0.0, 0.395])
TILT = 25.0                                    # the long axis leans back from the nose
BODY_R = np.array([0.15, 0.215, 0.1])


def at(a, b, n=0.0):
    return O + a * A + b * Z + n * N


def local(shape):
    V, F = shape
    return O + V[:, :1] * A + V[:, 1:2] * Z + V[:, 2:] * N, F


def to_local(P):
    d = P - O
    return d @ A, d @ Z, d @ N


def lobe(c, r, angle, n=0.0):
    """A flat lobe in the disc's plane, its long axis angle degrees from straight up (toward -a positive)."""
    return local(ell((c[0], c[1], n), r, rot=(0, 0, angle)))


def surf(a, b, s):
    """A point of the body's face at (a, b) on the side s (+1 the +N face) and its outward normal."""
    t = math.radians(TILT)
    ca, sa = math.cos(t), math.sin(t)
    ex, ey = a * ca + b * sa, -a * sa + b * ca
    q = (ex / BODY_R[0]) ** 2 + (ey / BODY_R[1]) ** 2
    n = BODY_R[2] * math.sqrt(max(1 - q, 0.02))
    gx, gy = ex / BODY_R[0] ** 2, ey / BODY_R[1] ** 2
    ga, gb = gx * ca - gy * sa, gx * sa + gy * ca
    nrm = ga * A + gb * Z + (n / BODY_R[2] ** 2) * N * s
    return at(a, b, n * s), nrm / np.linalg.norm(nrm)


def tip_mask(c, r, angle, frac):
    """The far end of a lobe beyond frac of its long radius: its pale tip."""
    t = math.radians(angle)
    def m(P):
        a, b, n = to_local(P)
        x, y = a - c[0], b - c[1]
        ex, ey = x * math.cos(t) + y * math.sin(t), -x * math.sin(t) + y * math.cos(t)
        near = ((ex / r[0]) ** 2 + (ey / r[1]) ** 2 + (n / r[2]) ** 2) < 1.6
        return near * inside(frac - ey / r[1], 0.03)
    return m


# The fins' lobes: (centre (a, b), radii (a, b, n), angle)
DORSAL = [((0.06, 0.235), (0.058, 0.09, 0.032), -34.0), ((0.12, 0.2), (0.055, 0.082, 0.032), -56.0)]
TAIL = [((-0.07, -0.255), (0.054, 0.11, 0.034), -8.0), ((-0.012, -0.262), (0.052, 0.104, 0.034), 6.0)]
ANAL = [((0.115, -0.18), (0.042, 0.045, 0.03), 40.0)]


def body(k):
    k.add("body", local(ell((0, 0, 0), BODY_R, rot=(0, 0, TILT), sub=5)), PINK, "body")
    # A plumper back, and the snout at the nose
    k.add("back", lobe((0.05, -0.03), (0.12, 0.15, 0.09), TILT), PINK, "body")
    k.add("snout", lobe((-0.078, 0.172), (0.04, 0.03, 0.058), TILT), PINK, "head")
    # The fins' roots, then their lobes
    k.add("dorsalroot", lobe((0.05, 0.17), (0.07, 0.05, 0.04), -40), FIN, "head")
    for i, (c, r, ang) in enumerate(DORSAL):
        k.add(f"dorsal{i}", lobe(c, r, ang), FIN, "head")
    k.add("tailroot", lobe((-0.035, -0.17), (0.06, 0.05, 0.045), 0), FIN, "tail")
    for i, (c, r, ang) in enumerate(TAIL):
        k.add(f"tailfin{i}", lobe(c, r, ang), FIN, "tail")
    for i, (c, r, ang) in enumerate(ANAL):
        k.add(f"anal{i}", lobe(c, r, ang), FIN, "tail")
    for c, r, ang in DORSAL:
        k.paint(tip_mask(c, r, ang, 0.55), TIP, on=["dorsal"])
    for c, r, ang in TAIL:
        k.paint(tip_mask((c[0], c[1]), r, ang + 180, 0.55), TIP, on=["tailfin"])
    for c, r, ang in ANAL:
        k.paint(tip_mask(c, r, ang + 180, 0.45), TIP, on=["anal"])
    # A low bump under each flipper, on the fin bones
    for s, side in ((1, "R"), (-1, "L")):
        p, _ = surf(0.075, 0.025, s)
        n = float((p - O) @ N)
        k.add("flipbump" + side, lobe((0.075, 0.025), (0.045, 0.045, 0.016), 0.0, n=n - 0.012 * s), PINK, "fin" + side)


# The flipper on each face: a heart's outline, its point toward the nose, its two lobes behind (u back across the disc)
HEART = [(-1.0, -0.15), (-0.35, 0.45), (0.2, 0.9), (0.65, 0.92), (0.95, 0.5), (0.72, 0.08), (0.98, -0.32),
         (0.72, -0.78), (0.25, -0.8), (-0.35, -0.55)]


def flippers(k):
    for s in (1, -1):
        p, nrm = surf(0.075, 0.025, s)
        pts = HEART if s > 0 else [(-u, v) for u, v in HEART]
        k.decal(p, nrm, (0.058, 0.052), FLIP, outline=polygon(pts), eps=0.004, rings=16, segs=90)


def mouths(k):
    CPK, CM = srgb(PINK), srgb(MOUTH)
    def look(u, v):
        c = np.tile(CPK, (len(u), 1))
        line = 0.5 * u * u - 0.25 + 0.18 * u
        c[(np.abs(v - line) < 0.24) & (np.abs(u) < 0.86)] = CM
        return c
    for s in (1, -1):
        p, nrm = surf(-0.09, 0.158, s)
        k.decal(p, nrm, (0.024, 0.013), look if s > 0 else (lambda u, v: look(-u, v)), eps=0.004, rings=12, segs=48)


# --- the eyes: gold with a dark rim and a black pupil, a thick navy lid over the top flicking back --------------------
EYE_AB, EYE_HALF = (-0.028, 0.124), (0.034, 0.032)
CG, CN, CK, CP = srgb(GOLD), srgb(NAVY), srgb(INK), srgb(PINK)


def lid_mask(u, v):
    r2 = u * u + v * v
    return ((v > 0.28 - 0.18 * u) & (r2 < 1.0)) | ((u > 0.35) & (v > 0.05 + 0.3 * (u - 0.35)) & (r2 < 1.0))


def eye_open(u, v):
    c = np.tile(CP, (len(u), 1))
    d = u * u + (v + 0.12) ** 2
    c[d < 0.7 ** 2] = CK
    c[d < 0.58 ** 2] = CG
    return c


def eye_state(state):
    look = eye_look(state, eye_open, CP, CK, line=0.15)
    def f(u, v):
        c = look(u, v)
        c[lid_mask(u, v)] = CN
        return c
    return f


def eyes(k, state):
    for s in (1, -1):
        p, nrm = surf(EYE_AB[0], EYE_AB[1], s)
        f = eye_state(state)
        k.decal(p, nrm, EYE_HALF, f if s > 0 else (lambda u, v, f=f: f(-u, v)), eps=0.004, rings=20, segs=56)
        if state in ("Open", "Fierce"):
            q, _ = surf(EYE_AB[0], EYE_AB[1] - 0.004, s)
            k.decal(q, nrm, (0.0075, 0.012), INK, eps=0.0055, rings=8, segs=32)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")
    body(k)
    flippers(k)
    mouths(k)
    k.build(voxel=0.0055, smooth=4, band=0.008)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
