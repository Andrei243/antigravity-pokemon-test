# Basculin (red-striped) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/basculin.py -- <the export's Basculin.glb> <out.glb>
#
# From the references: a fierce green bass, its head a big round dome; a charcoal cap over the top of the dome and down
# the back to the tail, a thin red stripe along the cap's whole edge, the cap's front edge rising over the forehead so
# the face is green; a jutting lower jaw (an underbite) with a tooth notched up into the upper lip; a big round eye
# under the stripe, white with a black rim, a large red iris and a small black pupil; three dark spots low on the flank
# behind the fin; spiky white fins shaded grey at the root: a big pectoral out of each side behind the eye, splayed
# back and out with its face to the front, a spiky crest on the back and a spiky fin under the belly (both turned 40
# degrees to +X so the game's cameras see a face), and a green tail fin that hooks down and back with the cap's dark
# along its top. The joints: body (0, 0, 0.233), head (0, -0.1, 0.183), tail (0, 0.2, 0.233).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, hull, boxy, srgb, inside, unit, eye_look, polygon, args

GREEN, CAP, RED, FIN, FINROOT, WHITE, INK, IRIS, SPOT = ("#5FA43F", "#353B42", "#E2443A", "#F2F3F1", "#AEB4BC",
                                                        "#F7F7F3", "#16161C", "#E0362E", "#37464C")

DOME_C, DOME_R = np.array([0.0, -0.03, 0.25]), np.array([0.106, 0.16, 0.117])
REAR_C, REAR_R = np.array([0.0, 0.075, 0.236]), np.array([0.088, 0.112, 0.1])
STRIPE = 0.012


def cap_edge(y):
    """The height of the cap's edge along the body: rising steeply over the forehead, falling gently to the tail."""
    y = np.asarray(y, float)
    back = 0.302 - (y + 0.09) * 0.13
    front = 0.302 + (-0.09 - y) * 1.6
    return np.where(y < -0.09, front, back)


def on(c, r, d):
    """The point of an ellipsoid along a direction of its unit sphere, and its outward normal."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    n = d / r
    return c + d * r, n / np.linalg.norm(n)


def body(k):
    k.add("dome", ell(DOME_C, DOME_R, sub=5), GREEN, "body")
    k.add("rear", ell(REAR_C, REAR_R, sub=5), GREEN, "body")
    # The crease between the dome and the rear filled, so the back is one mass
    k.add("waist", ell((0.0, 0.03, 0.245), (0.098, 0.1, 0.112), sub=5), GREEN, "body")
    # The tail's stalk, flatter from side to side than it is deep
    V, F = tube([(0, 0.14, 0.234), (0, 0.2, 0.228), (0, 0.24, 0.222)], [0.07, 0.048, 0.034])
    V[:, 0] *= 0.72
    k.add("stalk", (V, F), GREEN, "tail")
    # The jutting lower jaw, a rounded box ahead of the dome
    k.add("jaw", ell((0.0, -0.13, 0.172), (0.09, 0.105, 0.056), deform=boxy(2.8), sub=5), GREEN, "head")
    k.add("chin", ell((0.0, -0.06, 0.17), (0.085, 0.09, 0.06), sub=4), GREEN, "head")
    # The cap and its stripe: crisp edges by height along the body
    k.paint(lambda P: inside((cap_edge(P[:, 1]) - STRIPE) - P[:, 2], 0.0022) * inside(P[:, 2] - cap_edge(P[:, 1]), 0.0022), RED)
    k.paint(lambda P: inside(cap_edge(P[:, 1]) - P[:, 2], 0.0022), CAP)


def spots(k):
    for y, z, s_ in ((0.055, 0.205, 0.014), (0.1, 0.188, 0.013), (0.145, 0.176, 0.012)):
        dy = (y - REAR_C[1]) / REAR_R[1]; dz = (z - REAR_C[2]) / REAR_R[2]
        dx = math.sqrt(max(1 - dy * dy - dz * dz, 0.05))
        for s in (1, -1):
            p, n = on(REAR_C, REAR_R, (dx * s, dy, dz))
            k.decal(p, n, (s_, s_ * 1.15), SPOT, eps=0.004, rings=10, segs=36)


def fangs(k):
    for s in (1, -1):
        root = np.array([0.074 * s, -0.115, 0.214])
        pts = [root + (0.012 * s, -0.012, 0), root + (0.012 * s, 0.012, 0), root + (-0.006 * s, 0.0, 0.0),
               root + (0.004 * s, -0.004, 0.026), root + (0.008 * s, 0.0, 0.03)]
        k.add(f"fang{'R' if s > 0 else 'L'}", hull(pts), WHITE, "head", fuse=False, surface="shell")


def fin(k, name, outline, root_outline, origin, ex, ez, bone, thick=0.022):
    k.add(name, slab(outline, origin, ex, ez, thick), FIN, bone, fuse=False)
    k.add(name + "root", slab(root_outline, origin, ex, ez, thick * 1.35), FINROOT, bone, fuse=False)


# The pectoral: a long main point with spikes along both edges (a along the fin, b across it)
PEC = [(0.0, 0.03), (0.055, 0.042), (0.075, 0.08), (0.1, 0.048), (0.135, 0.07), (0.148, 0.036), (0.215, 0.01),
       (0.16, -0.016), (0.172, -0.048), (0.122, -0.03), (0.108, -0.064), (0.07, -0.032), (0.0, -0.026)]
PEC_ROOT = [(0.0, 0.028), (0.045, 0.034), (0.06, 0.0), (0.045, -0.026), (0.0, -0.024)]
# The crest on the back (a back along the fin's base, b up)
CREST = [(-0.065, -0.025), (-0.08, 0.03), (-0.045, 0.036), (-0.045, 0.074), (-0.012, 0.058), (0.0, 0.098),
         (0.016, 0.062), (0.046, 0.082), (0.044, 0.044), (0.082, 0.05), (0.064, 0.016), (0.088, -0.002),
         (0.062, -0.025)]
CREST_ROOT = [(-0.06, -0.025), (-0.058, 0.02), (0.052, 0.02), (0.06, -0.025)]
# The fin under the belly (b down from its root)
BELLY = [(-0.045, 0.015), (-0.035, -0.035), (-0.012, -0.026), (0.0, -0.075), (0.02, -0.042), (0.042, -0.066),
         (0.05, -0.024), (0.074, -0.03), (0.052, 0.015)]
BELLY_ROOT = [(-0.04, 0.015), (-0.03, -0.012), (0.045, -0.012), (0.05, 0.015)]
# The tail fin hooking down and back, and the cap's dark along its top
TAIL = [(-0.03, 0.035), (0.025, 0.048), (0.062, 0.024), (0.08, -0.03), (0.085, -0.1), (0.055, -0.066),
        (0.02, -0.038), (-0.03, -0.03)]
TAIL_TOP = [(-0.03, 0.035), (0.025, 0.048), (0.062, 0.024), (0.06, 0.009), (0.024, 0.03), (-0.03, 0.022)]


def fins(k):
    for s in (1, -1):
        o = np.array([0.075 * s, 0.0, 0.232])
        fin(k, f"pec{'R' if s > 0 else 'L'}", PEC, PEC_ROOT, o, (0.76 * s, 0.64, -0.06), (0.0, 0.0, 1.0), "body")
    yaw = math.radians(40)
    ex = (math.sin(yaw), math.cos(yaw), 0.0)
    fin(k, "crest", CREST, CREST_ROOT, np.array([0.0, 0.03, 0.335]), ex, (0, 0, 1), "body")
    fin(k, "belly", BELLY, BELLY_ROOT, np.array([0.0, 0.1, 0.155]), ex, (0, 0, 1), "body")
    tex = unit(0.72, 0.7, 0.0)
    o = np.array([0.0, 0.228, 0.224])
    k.add("tailfin", slab(TAIL, o, tex, (0, 0, 1), 0.02), GREEN, "tail", fuse=False)
    k.add("tailtop", slab(TAIL_TOP, o, tex, (0, 0, 1), 0.026), CAP, "tail", fuse=False)


# --- the eyes: white with a black rim, a large red iris, a small black pupil; under the stripe ------------------------
EYE_Y, EYE_Z, EYE_HALF = -0.1, 0.282, (0.037, 0.037)
EYE_N = unit(0.78, -0.56, 0.24)
CW, CK, CI, CG, CC, CR = srgb(WHITE), srgb(INK), srgb(IRIS), srgb(GREEN), srgb(CAP), srgb(RED)


def eye_point(s):
    dy = (EYE_Y - DOME_C[1]) / DOME_R[1]; dz = (EYE_Z - DOME_C[2]) / DOME_R[2]
    x = DOME_R[0] * math.sqrt(1 - dy * dy - dz * dz)
    return np.array([x * s, EYE_Y, EYE_Z])


def eye_open(u, v):
    c = np.tile(CW, (len(u), 1))
    c[((u + 0.06) / 0.6) ** 2 + (v / 0.6) ** 2 < 1] = CI
    c[u * u + v * v > 0.84 ** 2] = CK
    return c


def lid_colours(look):
    """The shut states draw the lid in the cap's colours where the cap and its stripe run over the eye."""
    edge = float(cap_edge(EYE_Y))
    def f(u, v):
        c = look(u, v)
        z = EYE_Z + v * EYE_HALF[1] * 0.97
        lid = np.all(np.isclose(c, CG), axis=1)
        c[lid & (z > edge - STRIPE)] = CR
        c[lid & (z > edge)] = CC
        return c
    return f


def eyes(k, state):
    for s in (1, -1):
        n = EYE_N * np.array([s, 1, 1])
        look = eye_look(state, eye_open, CG, CK, line=0.16)
        if state != "Open":
            look = lid_colours(look)
        k.decal(eye_point(s), n, EYE_HALF, look if s > 0 else (lambda u, v, f=look: f(-u, v)), eps=0.004,
                rings=20, segs=56)
        if state in ("Open", "Fierce"):
            k.decal(eye_point(s) + np.array([-0.002 * s, -0.004, -0.002]), n, (0.0085, 0.012), INK, eps=0.0055,
                    rings=8, segs=32)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="scales")
    body(k)
    spots(k)
    fangs(k)
    fins(k)
    k.build(voxel=0.0045, smooth=4, band=0.006)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
