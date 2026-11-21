# Celesteela sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/celesteela.py -- <the export's Celesteela.glb> <out.glb>
#
# The design, from the references: a tall pointed bell, a grey-teal cloak open down its front over a dark teal inside,
# a white jointed spine running up the opening to a little grey-green drum of a head with two small glowing cyan eyes
# and a cyan mark under them, four thin spikes standing out from the drum in an X, and over it a white spire ringed
# like a stalk of bamboo. The bell's foot is wrapped in layered aprons (grey with folds on the left, light green on the
# right, yellow-green at the hem) with a white panel down the middle, and dark green thrusters under the hem. Beside
# it, floating, two great bamboo shafts, the design's largest masses after the bell: grey-green, ringed white, their
# tops cut on the slant (the cut face gold), their lower ends a cuff of teal flaps and a hand of five dark green
# thrusters rimmed with cyan, splayed like fingers round a gold palm.
# The brief expected the shafts hanging at her sides: in both pictures they rise from the hands up and in beside the
# spire (top near the head, hand out and down), as built here; the hands are drawn in from the art's width so the model
# stays narrower than it is tall.
# Skeleton: root, body (0.45), head (0.62), armL/armR at (+-0.04, 0, 0.6).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, boxy, eye_look, args, hull, lathe, sheet, ring_band, outward, axes

# --- colours ----------------------------------------------------------------------------------------------------------
CLOAK, CLOAK_IN, RIM = "#5F9C8C", "#164E4C", "#3CC4AE"
SPINE, HEAD, EYE, EYE_HI = "#E2ECE8", "#7DA394", "#56E8DC", "#D8FFFA"
SHAFT, NODE, GOLD, GOLD_RIM = "#8DAE9F", "#E6EFEA", "#E9D26E", "#2A5B4E"
THRUST, THRUST_IN = "#1C3A2C", "#3E6B55"
APRON_GREY, APRON_GREEN, APRON_HEM, PANEL = "#9CB2A4", "#B6E0A2", "#D8E696", "#EEF2EC"

# --- the frame --------------------------------------------------------------------------------------------------------
BELL = [(0.0, 0.15), (0.2, 0.15), (0.21, 0.2), (0.195, 0.32), (0.16, 0.44), (0.115, 0.55), (0.075, 0.65),
        (0.046, 0.73), (0.0, 0.76)]                  # (radius, height) of the bell's dark inside
HEAD_C, HEAD_R = np.array([0.0, 0.0, 0.775]), np.array([0.05, 0.048, 0.042])
EYE_X, EYE_Z, EYE_HALF = 0.019, 0.786, 0.0115
SHAFT_B, SHAFT_T, SHAFT_R = np.array([0.29, -0.03, 0.37]), np.array([0.135, 0.02, 0.925]), 0.056


def bell_r(z):
    """The bell's radius at height z."""
    r, h = zip(*BELL[1:-1])
    return float(np.interp(z, h, r))


def bell(k):
    k.add("bell", lathe((0, 0, 0), (0, 0, 1), BELL, seg=64), CLOAK_IN, "body", fuse=False)

    # the cloak: a sheet round the bell, open in front, the opening narrowing to the head
    def at(u, v):
        z = 0.2 + v * 0.555
        half = np.radians(14 + 34 * (1 - v) ** 1.2)        # the opening's half angle
        th = half + u * (2 * math.pi - 2 * half)
        r = np.array([bell_r(zz) for zz in z.ravel()]).reshape(z.shape) + 0.014
        return np.stack([r * np.sin(th), -r * np.cos(th), z], axis=-1)
    k.add("cloak", outward(sheet(at, 64, 24, 0.012)), CLOAK, "body", fuse=False)

    # its cyan edge down each side of the opening: a cord standing a little proud
    for s in (1, -1):
        pts = []
        for v in np.linspace(0.0, 1.0, 9):
            z = 0.2 + v * 0.555
            half = math.radians(14 + 34 * (1 - v) ** 1.2) + 0.03
            r = bell_r(z) + 0.02
            pts.append((s * r * math.sin(half), -r * math.cos(half), z))
        k.add(f"rim{'R' if s > 0 else 'L'}", outward(tube(pts, [0.011] * len(pts), seg=10)), RIM, "body", fuse=False)


def aprons(k):
    """Layered aprons over the bell's foot: grey on the left, light green on the right, yellow-green at the hem, and a
    white panel down the middle."""
    def wrap(a0, a1, z0, z1, grow, folds=0.0):
        def at(u, v):
            th = np.radians(a0 + u * (a1 - a0))
            z = z1 - v * (z1 - z0)
            r = np.array([bell_r(zz) for zz in z.ravel()]).reshape(z.shape) + grow + 0.02 * v
            r = r + folds * np.abs(np.sin(th * 9))
            return np.stack([r * np.sin(th), -r * np.cos(th), z], axis=-1)
        return outward(sheet(at, 40, 12, 0.01))
    k.add("panel", wrap(-22, 22, 0.13, 0.5, 0.012), PANEL, "body", fuse=False)
    k.add("apronhem", wrap(-75, 75, 0.1, 0.24, 0.05), APRON_HEM, "body", fuse=False)
    k.add("aprongreen", wrap(8, 70, 0.17, 0.4, 0.036), APRON_GREEN, "body", fuse=False)
    k.add("aprongrey", wrap(-72, -6, 0.2, 0.47, 0.042, folds=0.006), APRON_GREY, "body", fuse=False)
    # the panel's lattice: a few darker diagonal laths, one piece
    # thrusters under the hem
    for i in range(5):
        a = 2 * math.pi * i / 5 + 0.3
        c = np.array([0.085 * math.cos(a), 0.085 * math.sin(a) - 0.01, 0.16])
        k.add(f"under{i}", thruster(c, (0, 0, -1.0), 0.15, 0.027), THRUST, "body", fuse=False)
        k.add(f"underrim{i}", ring_band(c + (0, 0, -0.14), (0, 0, 1), 0.029, 0.014, thick=0.006), RIM, "body", fuse=False)


def thruster(c, d, length, r):
    """A dark nozzle from c along d with its bore open at the far end."""
    prof = [(0.0, 0.0), (r * 0.9, 0.0), (r, 0.02), (r, length), (r * 0.6, length), (r * 0.6, length - 0.02),
            (0.0, length - 0.02)]
    return lathe(c, d, prof, seg=20)


def head(k):
    k.add("head", ell(HEAD_C, HEAD_R, deform=boxy(3.2), sub=4), HEAD, "head")
    # the spire: white, ringed like bamboo, to a fine point
    prof = [(0.0, 0.0), (0.03, 0.0), (0.03, 0.05), (0.034, 0.055), (0.026, 0.06), (0.024, 0.1), (0.028, 0.105),
            (0.02, 0.11), (0.017, 0.15), (0.0, 0.225)]
    k.add("spire", lathe(HEAD_C + (0, 0, 0.03), (0, 0, 1), prof, seg=24), SPINE, "head", fuse=False)
    # four spikes standing out from the drum in an X
    for sx in (1, -1):
        for sz in (1, -1):
            d = unit(sx, 0.15, 0.55 * sz)
            base = HEAD_C + np.array([sx * 0.04, 0.0, 0.012 * sz])
            a, b = axes(d)
            pts = [base + a * 0.008 * p + b * 0.008 * q for p in (-1, 1) for q in (-1, 1)] + [base + d * 0.085]
            k.add(f"spike{sx}{sz}", hull(pts), SPINE, "head", fuse=False)
    # the spine up the opening: a white cord with joints, a bulb under the head
    zs = np.linspace(0.42, 0.735, 8)
    pts = [(0, -bell_r(z) - 0.006, z) for z in zs]
    k.add("spine", outward(tube(pts, [0.015] * len(pts), seg=12)), SPINE, "body", fuse=False)
    for z, r in ((0.5, 0.021), (0.58, 0.02), (0.655, 0.022), (0.715, 0.026)):
        k.add(f"joint{z}", ell((0, -bell_r(z) - 0.006, z), (r, r * 0.8, r * 0.8)), SPINE, "body", fuse=False)


def arms(k):
    for s, side in ((1, "R"), (-1, "L")):
        bone = f"arm{side}"
        m = np.array([s, 1, 1])
        B, T = SHAFT_B * m, SHAFT_T * m
        d = (T - B) / np.linalg.norm(T - B)
        L = float(np.linalg.norm(T - B))
        prof = [(0.0, 0.0), (SHAFT_R * 0.92, 0.0), (SHAFT_R, 0.02), (SHAFT_R * 0.95, L), (0.0, L)]
        k.add(f"shaft{side}", lathe(B, d, prof, seg=36), SHAFT, bone, fuse=False, surface="metal")
        for t in (0.2, 0.42, 0.63, 0.82):
            k.add(f"node{int(t * 100)}{side}", ring_band(B + d * L * t, d, SHAFT_R * (1 - 0.05 * t) + 0.003, 0.02,
                                                          thick=0.008), NODE, bone, fuse=False)
        # the slanted cut at the top: a gold face looking out and forward, rimmed dark, and the shaft's lip behind it
        out_dir = unit(s * 0.75, -0.45, 0.5)
        k.add(f"cut{side}", ell(T + out_dir * 0.006, (SHAFT_R * 0.92, SHAFT_R * 1.2, 0.007),
                                rot=_rot_to(out_dir)), GOLD, bone, fuse=False)
        k.add(f"cutrim{side}", ell(T, (SHAFT_R * 1.02, SHAFT_R * 1.3, 0.009), rot=_rot_to(out_dir)), GOLD_RIM, bone,
              fuse=False)
        a, b = axes(d)
        lip = [T - out_dir * SHAFT_R * 0.9 + a * SHAFT_R * 0.6 * p + b * SHAFT_R * 0.6 * q for p in (-1, 1) for q in (-1, 1)]
        k.add(f"lip{side}", hull(lip + [T - out_dir * 0.03 + d * 0.075]), SHAFT, bone, fuse=False)
        # a short spike on the shaft's outer side, below its middle
        base = B + d * L * 0.52 + np.array([s * SHAFT_R * 0.9, 0, 0])
        sd = unit(s * 0.35, 0.0, 1)
        a2, b2 = axes(sd)
        k.add(f"stub{side}", hull([base + a2 * 0.008 * p + b2 * 0.008 * q for p in (-1, 1) for q in (-1, 1)]
                                  + [base + sd * 0.09]), NODE, bone, fuse=False)
        # a strut from the bell to the shaft, mostly hidden behind the cloak
        k.add(f"strut{side}", outward(tube([(0.08 * s, 0.04, 0.6), (0.16 * s, 0.04, 0.6), B + d * L * 0.42],
                                           [0.02, 0.02, 0.022], seg=12)), CLOAK_IN, bone, fuse=False)
        # the cuff: teal flaps round the shaft's lower end, pointing down and out
        for i in range(6):
            ang = 2 * math.pi * i / 6
            rd = a * math.cos(ang) + b * math.sin(ang)
            root = B + d * 0.035 + rd * SHAFT_R * 0.8
            tip = root + rd * 0.05 - d * 0.06
            w = np.cross(rd, d)
            k.add(f"flap{i}{side}", hull([root + w * 0.02, root - w * 0.02, root + rd * 0.012 + w * 0.016,
                                          root + rd * 0.012 - w * 0.016, tip]), RIM, bone, fuse=False)
        # the hand: five thrusters splayed like fingers round a gold palm
        k.add(f"palm{side}", ell(B - d * 0.012, (SHAFT_R * 0.8, SHAFT_R * 0.8, 0.012), rot=_rot_to(-d)), GOLD, bone,
              fuse=False)
        for i in range(5):
            ang = 2 * math.pi * i / 5 + 0.4
            rd = a * math.cos(ang) + b * math.sin(ang)
            root = B + rd * 0.034 + d * 0.01
            fd = unit(*(-d * 1.0 + rd * 0.55 + np.array([0.25 * s, 0, 0])))
            k.add(f"finger{i}{side}", thruster(root, fd, 0.13, 0.022), THRUST, bone, fuse=False)
            k.add(f"fingerrim{i}{side}", ring_band(root + fd * 0.12, fd, 0.024, 0.014, thick=0.006), RIM, bone,
                  fuse=False)


def _rot_to(n):
    """Euler degrees turning +Z onto n."""
    n = np.array(n, float) / np.linalg.norm(n)
    x = math.degrees(math.atan2(-n[1], n[2]))
    y = math.degrees(math.asin(np.clip(n[0], -1, 1)))
    return (x, y, 0)


def eyes_open():
    ce, ch, cd = srgb(EYE), srgb(EYE_HI), srgb("#1E6E68")
    def look(u, v):
        r = np.hypot(u, v)
        c = np.tile(ce, (len(u), 1))
        c[r > 0.82] = cd
        c[(u + 0.25) ** 2 + (v - 0.3) ** 2 < 0.3 ** 2] = ch
        return c
    return look


def celesteela(src, out_path):
    k = Kit(src, coat="metal")
    bell(k)
    aprons(k)
    head(k)
    arms(k)
    # the cyan mark under the eyes, in every state
    k.decal((0, -HEAD_R[1], 0.757), (0, -1, 0), (0.012, 0.006), EYE, eps=0.002, rings=10, segs=40)
    k.build(voxel=0.004, smooth=3, band=0.004)

    def lay(state):
        look = eye_look(state, eyes_open(), srgb(HEAD), srgb("#173C36"), line=0.18)
        k.decal((EYE_X, -HEAD_R[1] * 0.96, EYE_Z), unit(0.25, -1, 0.05), (EYE_HALF, EYE_HALF * 1.1), look, eps=0.002,
                rings=20, segs=56)
        k.decal((-EYE_X, -HEAD_R[1] * 0.96, EYE_Z), unit(-0.25, -1, 0.05), (EYE_HALF, EYE_HALF * 1.1),
                lambda u, v: look(-u, v), eps=0.002, rings=20, segs=56)
    k.eye_states(lay)
    k.export(out_path)


if __name__ == "__main__":
    celesteela(*args())
