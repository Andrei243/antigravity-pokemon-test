# Tangela sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tangela.py -- <the export's Tangela.glb> <out.glb>
#
# From the references: a ball of thick blue vines wound all over it, rising off it in loops and ends; in the front a
# dark hollow among the vines out of which two large round white eyes with dark pupils look, framed by a vine over it
# and a moustache of curled vines under it; two big red feet, their soles to the front, under the tangle. No arms.
#
# Written to be grown from (Tangrowth): the ball with its hollow (`ball`, `hollow_paint`), the vines (`vine`, `arcs`,
# `curl`), the eyes (`eye_open`, `lay_eyes`) are functions of the frame's named constants.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="leaf")

VINE, VINE2, CORE, HOLLOW, RED, WHITE, PUPIL, LINE = ("#3C8CDA", "#4A9AE6", "#24589E", "#121520", "#E04C54",
                                                      "#F7F7F4", "#101014", "#8094B4")

C = np.array([0.0, 0.0, 0.255])           # the ball's middle, on the body bone
R = np.array([0.205, 0.19, 0.2])          # its radii
THICK = 0.0165                            # a vine's radius: some 8% of the ball's width across
# The hollow, in angles about the ball's middle: round the upright axis from the front (+ toward +X) and up
HOLLOW_AZ, HOLLOW_EL, HOLLOW_HW, HOLLOW_HH = 0.0, math.radians(9), math.radians(31), math.radians(18)
EYE_X, EYE_Z, EYE_HALF = 0.05, 0.29, (0.031, 0.035)


def sph(az, el, lift=0.0, c=C, r=R):
    """A point over the ball at an angle round from the front (radians, + toward +X) and up, lifted by a share."""
    d = np.array([math.sin(az) * math.cos(el), -math.cos(az) * math.cos(el), math.sin(el)])
    return c + d * r * (1 + lift)


def ball(name, c, r, color, bone, hollow, depth=0.13):
    """The ball under the vines, a shallow dimple pressed into its front where the hollow is."""
    az0, el0, hw, hh = hollow
    def dimple(V):
        V = V.copy()
        az = np.arctan2(V[:, 0], -V[:, 1])
        el = np.arcsin(np.clip(V[:, 2], -1, 1))
        q = ((az - az0) / (hw * 1.05)) ** 2 + ((el - el0) / (hh * 1.1)) ** 2
        return V * (1 - depth * np.exp(-q ** 2))[:, None]
    k.add(name, ell(c, r, deform=dimple, sub=5), color, bone)


def hollow_paint(c, r, hollow, color, on):
    """The hollow's dark, painted over the dimple with a ragged edge that the vines round it hide."""
    az0, el0, hw, hh = hollow
    def mask(P):
        d = (P - c) / r
        az = np.arctan2(d[:, 0], -d[:, 1])
        el = np.arcsin(np.clip(d[:, 2] / np.maximum(np.linalg.norm(d, axis=1), 1e-6), -1, 1))
        a = np.arctan2(el - el0, az - az0)
        rr = 1.2 + 0.04 * np.sin(5 * a) + 0.03 * np.sin(3 * a + 1)
        # reaching a little lower than the dimple, down to the moustache that hides its edge
        q = np.sqrt(((az - az0) / hw) ** 2 + ((el - el0 + 0.05) / (hh * 1.15)) ** 2) - rr
        return inside(q, 0.05) * (d[:, 1] < 0)
    k.paint(mask, color, on=on)


def vine(name, pts, radius, color, bone, seg=10, per=5):
    """One rope of vine, loose so that it keeps its own round edge against the ball and its fellows."""
    pts = np.array(pts, float)
    rad = [radius] * len(pts)
    k.add(name, tube(pts, rad, seg=seg, per=per), color, bone, fuse=False)


def in_hollow(d, hollow, margin=1.18):
    """Whether a direction from the ball's middle looks into the hollow (kept clear of the random vines)."""
    az0, el0, hw, hh = hollow
    az = math.atan2(d[0], -d[1]); el = math.asin(max(-1, min(1, d[2])))
    return ((az - az0) / (hw * margin)) ** 2 + ((el - el0) / (hh * margin)) ** 2 < 1


def arcs(prefix, count, seed, c, r, radius, colours, bone_of, hollow, span=(0.45, 0.8), lift=(0.06, 0.2),
         lowest=-0.72, hoops=0.18):
    """
    Vines wound over the ball: arcs round its surface, spread by the golden angle, each rising off it toward its middle,
    a share of them in tall hoops. None crosses the hollow, none grows under the feet.
    """
    rs = np.random.RandomState(seed)
    made = 0
    # count over the whole ball, the ones under the lowest line and in the hollow left out
    for i in range(count):
        y = 1 - 2 * (i + 0.5) / count
        ring = math.sqrt(max(0.0, 1 - y * y))
        a = i * 2.39996 + rs.rand() * 0.6
        n = np.array([math.sin(a) * ring, -math.cos(a) * ring, y])
        if y < lowest:
            continue
        t = np.cross(n, (0, 0, 1.0) if abs(n[2]) < 0.95 else (1.0, 0, 0)); t /= np.linalg.norm(t)
        turn = rs.rand() * math.pi
        u = t * math.cos(turn) + np.cross(n, t) * math.sin(turn)
        sp = span[0] + (span[1] - span[0]) * rs.rand()
        lf = lift[0] + (lift[1] - lift[0]) * rs.rand()
        if rs.rand() < hoops:
            lf *= 2.0
        pts, bad = [], False
        for s in np.linspace(-1, 1, 7):
            d = n * math.cos(s * sp) + u * math.sin(s * sp); d /= np.linalg.norm(d)
            if in_hollow(d, hollow) or d[2] < lowest - 0.05:
                bad = True; break
            pts.append(c + d * r * (1 + radius / r.min() * 0.6 + lf * math.cos(s * math.pi / 2) ** 1.5))
        if bad:
            continue
        mid = pts[3]
        vine(f"{prefix}{made}", pts, radius * (0.92 + 0.16 * rs.rand()), colours[made % len(colours)], bone_of(mid))
        made += 1
    return made


def curl(name, start, out_dir, length, turns, radius, color, bone, up=(0, 0, 1.0), shrink=0.55, n=10):
    """A vine's free end: leaves the ball at start, runs out along out_dir and rolls up in a curl at its tip."""
    out_dir = unit(*out_dir); up = np.array(up, float)
    side = np.cross(out_dir, up); side /= np.linalg.norm(side)
    roll = np.cross(side, out_dir)
    pts = []
    for j in range(n):
        t = j / (n - 1)
        ang = turns * 2 * math.pi * t ** 1.6
        rad = length * (1 - shrink * t) * 0.35
        p = (np.array(start, float) + out_dir * length * 0.6 * t
             + (out_dir * math.sin(ang) + roll * (1 - math.cos(ang))) * rad * min(1, t * 3))
        pts.append(p)
    vine(name, pts, radius, color, bone)


def eye_open(u, v, pupil=(0.0, 0.0), size=0.36):
    """A plain white eye; its pupil is a patch of its own."""
    return np.tile(srgb(WHITE), (len(u), 1))


def lay_eyes(state, xs, z, half, pupil_at, pupil_half, lid, ink, normal_of, surface_y=-0.3):
    """The two eyes on the hollow's floor, cast from the front: white with a solid dark pupil (Open and Fierce)."""
    for s in (1, -1):
        c = np.array([xs * s, surface_y, z])
        n = normal_of(s)
        look = eye_look(state, eye_open, lid, ink, line=0.16)
        col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
        k.decal(c, n, half, col, eps=0.005, rings=20, segs=56)
        if state in ("Open", "Fierce"):
            pc = c + np.array([pupil_at[0] * s * half[0], 0, pupil_at[1] * half[1] - (0.006 if state == "Fierce" else 0)])
            k.decal(pc, n, (pupil_half, pupil_half * 1.08), PUPIL, eps=0.008, rings=8, segs=32)


# ---- the ball and its hollow --------------------------------------------------------------------------------------
HOLLOW_SPEC = (HOLLOW_AZ, HOLLOW_EL, HOLLOW_HW, HOLLOW_HH)
ball("core", C, R, CORE, "body", HOLLOW_SPEC)
hollow_paint(C, R, HOLLOW_SPEC, HOLLOW, on=["core"])

# ---- the vines wound all over it ------------------------------------------------------------------------------------
def bone_of(p):
    if p[2] > C[2] + 0.11:
        return "head"
    return "body"
arcs("vine", 112, 114, C, R, THICK, (VINE, VINE2), bone_of, HOLLOW_SPEC)

# The frame of the hollow: a vine drooping over its top, one down each side, and the moustache of curls under it
D = math.radians
vine("brow", [sph(D(a), D(e), l) for a, e, l in
              ((-50, 6, 0.06), (-42, 24, 0.1), (-24, 33, 0.13), (0, 29, 0.14), (22, 34, 0.13), (40, 26, 0.1),
               (48, 10, 0.06))],
     THICK * 1.05, VINE2, "head")
vine("brow2", [sph(D(a), D(e), l) for a, e, l in
               ((-20, 46, 0.03), (-8, 36, 0.12), (12, 38, 0.14), (34, 40, 0.08), (44, 50, 0.03))],
     THICK, VINE, "head")
for s in (1, -1):
    vine(f"cheek{'R' if s > 0 else 'L'}", [sph(D(a * s), D(e), l) for a, e, l in
                                            ((40, 36, 0.03), (45, 20, 0.1), (40, 6, 0.13), (43, -8, 0.12), (52, -20, 0.06))],
         THICK, VINE if s > 0 else VINE2, "body")
    # the moustache: from the middle under the hollow out to the side, rolled down and back in a loop
    vine(f"tache{'R' if s > 0 else 'L'}", [sph(D(a * s), D(e), l) for a, e, l in
                                           ((2, -13, 0.1), (12, -10, 0.14), (24, -12, 0.15), (32, -20, 0.14),
                                            (26, -29, 0.13), (15, -27, 0.13), (12, -19, 0.12), (20, -16, 0.12))],
         THICK * 1.05, VINE2 if s > 0 else VINE, "body")
    vine(f"tache2{'R' if s > 0 else 'L'}", [sph(D(a * s), D(e), l) for a, e, l in
                                            ((3, -36, 0.03), (8, -28, 0.1), (4, -21, 0.17), (-3, -24, 0.12),
                                             (-6, -33, 0.04))],
         THICK, VINE, "body")

# Free ends: curls off the top and loose ends out at the sides (on the arm bones, where the skeleton has its arms)
curl("endtop", sph(D(-30), D(55), 0.06), (-0.5, -0.3, 1.0), 0.13, 0.75, THICK, VINE2, "head", up=(1, 0, 0))
curl("endtop2", sph(D(150), D(48), 0.06), (0.3, 0.6, 1.0), 0.12, 0.7, THICK, VINE, "head", up=(1, 0, 0))
curl("endR", sph(D(70), D(14), 0.08), (1.0, -0.25, -0.15), 0.16, 0.8, THICK, VINE, "armR", up=(0, -1, 0))
curl("endL", sph(D(-80), D(4), 0.08), (-1.0, -0.15, -0.35), 0.1, 0.7, THICK, VINE2, "armL", up=(0, 1, 0))
curl("endR2", sph(D(100), D(-22), 0.06), (0.9, 0.3, -0.5), 0.1, 0.6, THICK, VINE2, "armR", up=(0, 1, 0))
curl("endbackL", sph(D(-140), D(10), 0.06), (-0.6, 0.8, 0.0), 0.11, 0.7, THICK, VINE, "body")
curl("endlow", sph(D(0), D(-50), 0.04), (0, -0.3, -1.0), 0.07, 0.3, THICK * 0.9, VINE, "body", up=(1, 0, 0))

# ---- the feet: big red ovals, their soles turned to the front and a little out ----------------------------------
for s, side in ((1, "R"), (-1, "L")):
    k.add(f"foot{side}", ell((0.122 * s, -0.095, 0.072), (0.06, 0.05, 0.072), rot=(-14, 0, -24 * s), sub=4),
          RED, f"leg{side}", fuse=False, surface="fur")

k.build(voxel=0.006, smooth=3)

# ---- the eyes: large round whites in the hollow, a solid dark pupil a little up and in -------------------------------
def normal_of(s):
    return unit(0.12 * s, -1.0, 0.06)
def eyes(state):
    lay_eyes(state, EYE_X, EYE_Z, EYE_HALF, (-0.12, 0.1), 0.011, srgb(HOLLOW), srgb(LINE), normal_of)

k.eye_states(eyes)
k.export(out)
