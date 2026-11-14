# Heatran sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/heatran.py -- <the export's Heatran.glb> <out.glb>
# From the references: a wide, low quadruped like a toad or a crab; a dark red-brown dome of a body with bright orange
# molten blotches and long grey steel studs lying on it; the head a great grey steel helmet sunk into the front with no
# neck, a round boss on its brow and a blunt horn out of each side; fierce orange eyes with dark pupils under the
# helmet's edge in a grey face, and under them a ring of long rounded grey plates hanging like teeth over a dark grey
# ribbed belly; four thick brown legs splayed out and forward, each with a grey band set with big blunt studs and a
# broad brown bell of a foot ringed with grey points at its foot.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, srgb, inside, unit, boxy, eye_look, args, outward, hull, ring_band, polygon, slab)

src, out = args()
k = Kit(src, coat="metal")

HIDE, BLOTCH, MOLTEN = "#7E3A22", "#EE8424", "#F8BE32"
STEEL, FACE, STUD = "#A2A8B2", "#AEB4BC", "#AAB0BA"
BELLY, RIB = "#4A4A52", "#33333A"
IRIS, PUPIL, INK = "#EE6A1C", "#2A1810", "#2A2428"

X = np.array([1.0, 0, 0])


def oriented(c, frame, r, deform=None, sub=3):
    """An ellipsoid about c whose radii lie along the three directions of frame."""
    V, F = ell((0, 0, 0), r, deform=deform, sub=sub)
    return outward((V @ np.stack(frame, axis=0) + np.array(c), F))


def frame_on(n, along):
    """A frame (t, b, n) on a surface of normal n, t as near along as the surface lets it."""
    n = np.array(n, float); n /= np.linalg.norm(n)
    t = np.array(along, float); t = t - n * (t @ n); t /= np.linalg.norm(t)
    return t, np.cross(n, t), n


# --- the body: a brown dome, higher at the back, and the dark ribbed belly under the head ---------------------------
BC, BR = np.array([0.0, 0.09, 0.37]), np.array([0.215, 0.25, 0.225])
k.add("body", ell(BC, BR), HIDE, "body")
k.add("hips", ell((0, 0.14, 0.3), (0.2, 0.17, 0.15)), HIDE, "body")
k.add("belly", ell((0, -0.1, 0.27), (0.175, 0.16, 0.115)), BELLY, "body", surface="shell")


def on_body(d):
    """The point of the body's dome along direction d from its middle, and the surface's normal there."""
    u = unit(*d)
    p = BC + BR * u
    n = u / BR; n /= np.linalg.norm(n)
    return p, n


# Ribs across the belly: dark lines curving up at the sides
def ribs(P):
    x, y, z = P[:, 0], P[:, 1], P[:, 2]
    m = np.zeros(len(P))
    for z0 in (0.205, 0.245):
        m = np.maximum(m, inside(np.abs(z - (z0 + 1.6 * x * x)) - 0.0045, 0.002))
    return m * (y < -0.12)


k.paint(ribs, RIB, on=["belly"])

# --- the head: a grey helmet sunk into the front, its face, a boss on the brow, a horn out of each side -------------
HC, HR = np.array([0.0, -0.205, 0.455]), np.array([0.15, 0.135, 0.115])
k.add("helmet", ell(HC, HR, deform=boxy(2.3)), STEEL, "head")
k.add("face", ell((0, -0.275, 0.37), (0.13, 0.075, 0.08)), FACE, "head")
k.add("jowl", ell((0, -0.24, 0.31), (0.13, 0.09, 0.06)), FACE, "head")
# The brow: the helmet's front edge standing out over the eyes, low toward the middle
for s in (1, -1):
    k.add(f"brow{'R' if s > 0 else 'L'}", tube([(0.115 * s, -0.31, 0.425), (0.06 * s, -0.345, 0.415),
                                                  (0.0, -0.352, 0.398)], [0.024, 0.024, 0.022]), STEEL, "head")
# The boss: a round disc proud of the dome's front, tilted up
BN = unit(0, -0.78, 0.62)
BP = HC + np.array([0, -0.11, 0.075])
k.add("boss", oriented(BP + BN * 0.012, frame_on(BN, X), (0.058, 0.058, 0.024), sub=4), STEEL, "head", fuse=False,
      surface="metal")
k.add("bossrim", oriented(BP, frame_on(BN, X), (0.066, 0.066, 0.02), sub=4), STEEL, "head")
# The horns: blunt cones out of the helmet's sides, a little back and up
k.add("horn", tube([(0.12, -0.21, 0.455), (0.19, -0.2, 0.46), (0.245, -0.19, 0.468)], [0.05, 0.034, 0.016]),
      STEEL, "head", mirror=True)

# The teeth: long rounded grey plates hanging in a ring round the lower face, over the belly
for i, a in enumerate(np.linspace(-62, 62, 7)):
    t = math.radians(a)
    rx, ry = 0.135, 0.11
    c = np.array([rx * math.sin(t), -0.215 - ry * math.cos(t), 0.29 - 0.008 * abs(math.sin(t))])
    out_ = unit(math.sin(t), -math.cos(t), 0.0)
    down = unit(*(out_ * 0.22 + np.array([0, 0, -1.0])))
    side = np.cross(down, out_)
    k.add(f"tooth{i}", oriented(c, [side, out_, down], (0.024, 0.018, 0.048)), STUD, "head", fuse=False,
          surface="metal")

# --- the legs: thick brown limbs splayed out and forward, a grey band of studs, a bell of a foot --------------------
LEGS = [("leg", -1.0), ("hind", 1.0)]
for name, fy in LEGS:
    hip = np.array([0.13, 0.12 * fy, 0.28])
    # Bowed out at the elbow, the foot brought in under the body's edge and further forward or back (the game's cell)
    elbow = np.array([0.225, 0.235 * fy, 0.33])
    ankle = np.array([0.205, 0.345 * fy, 0.13])
    foot = np.array([0.2, 0.36 * fy, 0.0])
    bone = name + "{s}"
    k.add(name + "upper", tube([hip, elbow], [0.095, 0.088]), HIDE, bone, mirror=True)
    k.add(name + "lower", tube([elbow, ankle], [0.088, 0.078]), HIDE, bone, mirror=True)

    def bell(V):
        V = V.copy()
        V[:, 2] = np.maximum(V[:, 2], -0.72)
        w = 0.62 + 0.38 * (1 - V[:, 2]) / 2
        V[:, 0] *= w; V[:, 1] *= w
        return V
    k.add(name + "foot", ell(foot + np.array([0, 0, 0.1]), (0.112, 0.112, 0.13), deform=bell), HIDE, bone, mirror=True)

    # The band round the lower leg, its studs, and a knob on the elbow
    d = ankle - elbow; L = np.linalg.norm(d); d /= L
    mid = elbow + d * L * 0.42
    k.add(name + "band", ring_band(mid, d, 0.088, 0.055, thick=0.012), STEEL, bone, fuse=False, mirror=True,
          surface="metal")
    a = np.cross(d, (0, 0, 1.0)); a /= np.linalg.norm(a)
    if a[0] < 0:
        a = -a
    b = np.cross(d, a)
    if b[2] < 0:
        b = -b
    for j, q in enumerate((-80, -50, 50, 120)):
        r = math.radians(q)
        n = a * math.cos(r) + b * math.sin(r)
        k.add(f"{name}stud{j}", oriented(mid + n * 0.1, frame_on(n, d), (0.03, 0.024, 0.034)), STUD, bone,
              fuse=False, mirror=True, surface="metal")
    ue = elbow + np.array([0.02, 0.0, 0.082])
    k.add(name + "knob", oriented(ue, frame_on(unit(0.3, 0, 1), unit(0, fy, 0)), (0.04, 0.03, 0.026)), STUD, bone,
          fuse=False, mirror=True, surface="metal")
    # The cuff where the bell begins, and grey points round the bell's foot
    k.add(name + "cuff", ring_band(foot + np.array([0, 0, 0.115]), (0, 0, 1), 0.092, 0.03, thick=0.012), STEEL, bone,
          fuse=False, mirror=True, surface="metal")
    for j, q in enumerate((-80, -50, 50, 140)):
        r = math.radians(q)
        dirn = unit(math.cos(r), fy * math.sin(r), 0.0)
        root = foot + dirn * 0.1 + np.array([0, 0, 0.025])
        tip = foot + dirn * 0.155 + np.array([0, 0, 0.012])
        k.add(f"{name}claw{j}", outward(tube([root, tip], [0.026, 0.01], seg=14, per=6)), STEEL, bone, fuse=False,
              mirror=True, surface="metal")

    # Large molten patches, loose orange domes with a yellow heart (a patch here once came out in ink): one on the
    # lower leg's outer face toward the camera that sees it (front for the forelegs, back for the hind legs) and one
    # on the foot's upper face
    def molten(at, n, along, r, seed):
        # Flat plates with a ragged outline laid on the surface (hard-edged, so they read as molten rock, not buttons)
        t, b, n = frame_on(n, along)
        g = np.random.RandomState(seed)
        th = np.linspace(0, 2 * math.pi, 8, endpoint=False) + g.rand(8) * 0.35
        rr = 0.72 + 0.28 * g.rand(8)
        ring = np.stack([rr * np.cos(th), rr * np.sin(th)], axis=1)
        k.add(f"{name}molten{len(k.parts)}", slab(ring * np.array([r, r * 0.82]), at - n * 0.001, b, t, 0.012),
              BLOTCH, bone, fuse=False, mirror=True, surface="metal")
        k.add(f"{name}heart{len(k.parts)}", slab(ring * np.array([r * 0.5, r * 0.42]), at + n * 0.004, b, t, 0.012),
              MOLTEN, bone, fuse=False, mirror=True, surface="metal")
    sp = elbow + d * L * 0.78
    ns = unit(0.72, 0.62 * fy, 0.1); ns = ns - d * (ns @ d); ns /= np.linalg.norm(ns)
    molten(sp + ns * 0.08, ns, d, 0.04, 11 if fy < 0 else 12)
    rd = unit(0.85, 0.53 * fy, 0.0)
    nf = unit(*(rd + np.array([0, 0, 0.6])))
    molten(foot + rd * 0.096 + np.array([0, 0, 0.075]), nf, (0, 0, 1), 0.04, 13 if fy < 0 else 14)


# --- the studs on the back: long grey ovals lying on the dome ---------------------------------------------------------
STUDS = [((0.5, -0.05, 0.86), 30), ((-0.45, 0.2, 0.87), -40), ((0.15, 0.5, 0.85), 80),
         ((0.8, 0.4, 0.45), 10), ((-0.75, 0.5, 0.45), -20), ((-0.1, 0.85, 0.5), 90), ((0.5, 0.75, 0.35), 60)]
for i, (d, turn) in enumerate(STUDS):
    p, n = on_body(d)
    along = unit(math.cos(math.radians(turn)), math.sin(math.radians(turn)), 0.0)
    k.add(f"stud{i}", oriented(p + n * 0.008, frame_on(n, along), (0.052, 0.03, 0.022)), STUD, "body", fuse=False,
          surface="metal")


# --- the molten blotches: orange patches with a yellow heart, ragged outlines ---------------------------------------
def blotch_look(u, v):
    c = np.tile(srgb(BLOTCH), (len(u), 1))
    c[u * u + v * v < 0.22] = srgb(MOLTEN)
    return c


rs = np.random.RandomState(485)
def ragged(n=7):
    th = np.linspace(0, 2 * math.pi, n, endpoint=False) + rs.rand(n) * 0.4
    r = 0.7 + 0.3 * rs.rand(n)
    return polygon(np.stack([r * np.cos(th), r * np.sin(th)], axis=1))


BLOTCHES = [((0.32, -0.25, 0.9), 0.045), ((-0.4, 0.1, 0.9), 0.05), ((0.6, 0.3, 0.72), 0.04), ((-0.15, 0.55, 0.8), 0.045),
            ((0.25, 0.85, 0.45), 0.04), ((-0.7, 0.55, 0.42), 0.04), ((0.85, 0.35, 0.2), 0.035),
            ((0.0, 0.95, 0.15), 0.04)]
for d, r in BLOTCHES:
    p, n = on_body(d)
    k.decal(p, n, (r, r * 0.8), blotch_look, up=(0, 0, 1) if abs(n[2]) < 0.9 else (0, 1, 0), outline=ragged(),
            rings=8, segs=28)
# On the upper legs' tops, cast from up and out so that the rays pass outside the horns (the lower legs and feet carry
# loose molten domes, above: a patch on the lower leg's outer face came out in ink on one side)
for name, fy in LEGS:
    for s in (1, -1):
        nu_ = unit(0.6 * s, 0.0, 0.8)
        k.decal(np.array([0.2 * s, 0.18 * fy, 0.3]) + nu_ * 0.09, nu_, (0.036, 0.03), blotch_look, up=(0, fy, 0),
                outline=ragged(), rings=8, segs=28)


# --- the eyes: fierce orange eyes with a dark pupil, their top edge cut by the brow sloping to the middle -----------
CI, CP, CK, CF = srgb(IRIS), srgb(PUPIL), srgb(INK), srgb(FACE)
TOP = lambda u: 0.42 + 0.38 * u                         # the brow's cut, low toward the inner corner (u -1)


def eye(u, v):
    # The patch's own outline is the eye (an oval cut by the brow), so its edges are crisp: an ink rim, the iris, a pupil
    c = np.tile(CK, (len(u), 1))
    shape = (u / 0.98) ** 2 + ((v + 0.05) / 0.92) ** 2
    core = (shape < 0.6) & (v < TOP(u) - 0.24)
    c[core] = CI
    c[((u + 0.18) / 0.34) ** 2 + ((v + 0.1) / 0.46) ** 2 < 1] = CP
    return c


# The eye's outline: the oval below the brow's line (star-shaped about the middle, since TOP(0) is above it)
_t = np.linspace(0, 2 * math.pi, 96, endpoint=False)
_u, _v = 0.98 * np.cos(_t), -0.05 + 0.92 * np.sin(_t)
_v = np.minimum(_v, TOP(_u))
OUT_R = polygon(np.stack([_u, _v], axis=1))
OUT_L = polygon(np.stack([-_u, _v], axis=1))

ED = unit(0.38, -1, 0.08)
EC = np.array([0.058, -0.345, 0.385])
M = np.array([-1.0, 1, 1])


def lay(state):
    # The open eye is already fierce under its brow: Fierce is the same eye. Two patches by hand, since a mirrored
    # patch's outline is not mirrored: +u runs toward +X on both, so the left one's look is turned round
    look = eye if state in ("Open", "Fierce") else eye_look(state, eye, CF, CK, line=0.16)
    k.decal(EC, ED, (0.04, 0.034), look, eps=0.004, rings=20, segs=56, outline=OUT_R)
    k.decal(EC * M, ED * M, (0.04, 0.034), lambda u, v: look(-u, v), eps=0.004, rings=20, segs=56, outline=OUT_L)


k.build(voxel=0.0075, smooth=3)
k.eye_states(lay)
k.export(out)
