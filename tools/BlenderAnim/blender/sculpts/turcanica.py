# Turcanica sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/turcanica.py -- <the export's Turcanica.glb> <out.glb>
#
# Grown from Lanita's script (sibling("lanita")): the same cream wool and hanging locks, the same black face, nose and
# eye, the same legs and hooves, the same bronze bell. From its picture (10024-Turcanica.jpg) and brief (CP-dex-1-
# plateau, 24): a tall mountain ewe of the turcana breed; its wool hangs in long wavy ringlets from the spine to the
# ground, flaring out over the ground at the foot like a shawl, the black legs showing in front; a thick ruff of wool
# round the neck and chest and a woolly topknot on the crown; the face long and black, the muzzle carried forward and
# down, a grey nose and a quiet smile; calm half-lidded eyes with a brown iris; black ears straight out sideways;
# great dark spiralled horns from the crown curling back, down and forward round the ears, their tips flicked out;
# three red embroidered ribbons laid across the back and hanging down both flanks to tassels; a red embroidered
# collar at the throat with the bigger bronze bell (a red band round its waist) and two tassels beside it.
# Reference ratios (the picture): the face 0.18 of the height long; the horns' span 0.7 of the height (built 0.58, the
# tips curled in so the model is not drawn small); the ringlets' flare at the foot 0.65 of the height across; the
# legs showing in front 0.2 of the height; the bell 0.1 of the height.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, sheet, ring_band, srgb, inside, unit, args, outward, sibling

la = sibling("lanita")
WOOL, WOOL_SHADE, FACE, LEG, HOOF = la.WOOL, la.WOOL_SHADE, la.FACE, la.LEG, la.HOOF
TIP = "#FBF4DE"                     # the ringlets' ends, warmer and lighter: the fleece's soft glow at its edges
HORN, HORN_DARK = "#4C4748", "#3A3637"
RED, RED_DARK, STRIPE = "#A8323A", "#7A222A", "#2A1E22"


def frame():
    """Turcanica's frame (joints: body (0, 0.02, 0.5), head (0, -0.27, 0.6), ears (+-0.06, -0.35, 0.75), legs
    (+-0.11, -0.18 / 0.2, 0.32))."""
    F = la.frame()
    F.BODY, F.BODY_R = np.array([0.0, 0.04, 0.52]), np.array([0.185, 0.28, 0.165])
    F.RUFF, F.RUFF_R = np.array([0.0, -0.215, 0.6]), np.array([0.14, 0.12, 0.155])
    F.CAP, F.CAP_R = np.array([0.0, -0.305, 0.8]), np.array([0.082, 0.075, 0.055])
    F.FACE, F.FACE_R = np.array([0.0, -0.345, 0.742]), np.array([0.07, 0.078, 0.078])
    F.SNOUT, F.SNOUT_R = np.array([0.0, -0.405, 0.672]), np.array([0.05, 0.058, 0.05])
    F.LEG_X, F.LEG_Y, F.HIP, F.LEG_R = 0.105, (-0.18, 0.2), 0.36, 0.036
    F.EYE_D = unit(0.6, -0.62, 0.36)
    F.EYE_HALF = (0.026, 0.024)
    F.EYE_LID = 0.44
    F.BELL_H = 0.09
    F.VOXEL = 0.0088
    return F


def front_clear(a):
    """The locks leave the chest and the front legs open."""
    return math.sin(a) < -0.9


def horn(k, F):
    """A great ram's horn from the crown: up and back, round down behind the ear and forward under it, the tip flicked
    out. Authored on +X, mirrored."""
    centre = np.array([0.0, -0.24, 0.76])
    pts, n = [], 18
    for i in range(n):
        t = i / (n - 1)
        a = -1.15 + t * 0.86 * 2 * math.pi
        rr = 0.088 - 0.036 * t
        x = 0.05 + 0.16 * t + 0.07 * t ** 3
        pts.append(centre + np.array([x, rr * math.sin(a), rr * math.cos(a)]))
    radii = list(np.linspace(0.036, 0.011, n))
    k.add("horn", outward(tube(pts, radii, seg=18, per=4)), HORN, "head", fuse=False, mirror=True, surface="shell")
    # ridges across it, a few darker bands
    P = np.array(pts)
    for j, i in enumerate(()):          # ridges read as a pipe's joints at the game's size: none
        d = P[i + 1] - P[i - 1]
        k.add(f"hornridge{j}", ring_band(P[i], d, radii[i] + 0.0015, 0.012, thick=0.004), HORN_DARK, "head",
              fuse=False, mirror=True, surface="shell")


def tassel(k, name, top, bone="body", length=0.075, colour=RED, knot=STRIPE):
    """A tassel hanging from top: a knot and a fringe widening down."""
    top = np.array(top, float)
    k.add(name + "knot", outward(ell(top, (0.017, 0.017, 0.015), sub=3)), knot, bone, fuse=False, surface="cloth")
    ring = []
    for j in range(8):
        a = 2 * math.pi * j / 8
        ring.append(top + np.array([0.012 * math.cos(a), 0.012 * math.sin(a), -0.008]))
        ring.append(top + np.array([0.024 * math.cos(a), 0.024 * math.sin(a), -length]))
    k.add(name, hull(ring), colour, bone, fuse=False, surface="cloth")


def ribbon(k, F, name, y, tilt, ends_at, width=0.062):
    """A red embroidered ribbon over the back from flank to flank, hanging down each side to a tassel, a dark stripe
    of its pattern along its middle. y: where it crosses the spine; tilt: how far back each hanging end runs."""
    c, R = F.BODY, F.BODY_R
    k_ = math.sqrt(max(0.1, 1 - ((y - c[1]) / R[1]) ** 2))
    rx, rz = R[0] * k_ + 0.03, R[2] * k_ + 0.03
    side_z = c[2]
    # the path: a half ellipse over the back, then straight down each flank
    P, N = [], []
    for s in np.linspace(-1, 1, 41):
        ph = s * math.pi / 2
        P.append([math.sin(ph) * rx, y, side_z + math.cos(ph) * rz]); N.append([math.sin(ph), 0, math.cos(ph)])
    for side in (-1, 1):
        drop = []
        for t in np.linspace(0.05, 1, 14):
            z = side_z - (side_z - ends_at) * t
            drop.append(([side * (rx + 0.022 * t), y + tilt * t, z], [side, 0, 0]))
        if side < 0:
            P = [p for p, _ in drop[::-1]] + P; N = [n for _, n in drop[::-1]] + N
        else:
            P += [p for p, _ in drop]; N += [n for _, n in drop]
    P, N = np.array(P), np.array(N, float)
    L = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(P, axis=0), axis=1))]); L /= L[-1]
    def along(u, lift, w):
        def at(uu, vv):
            p = np.stack([np.interp(uu, L, P[:, j]) for j in range(3)], -1)
            nrm = np.stack([np.interp(uu, L, N[:, j]) for j in range(3)], -1)
            nrm /= np.linalg.norm(nrm, axis=-1, keepdims=True)
            across = np.cross(nrm, np.array([1.0, 0, 0]))
            bad = np.linalg.norm(across, axis=-1) < 0.3
            across[bad] = np.array([0, 1.0, 0])
            across /= np.linalg.norm(across, axis=-1, keepdims=True)
            across *= np.sign(across[..., 1:2] + 1e-9)
            return p + nrm * lift + across * ((vv - 0.5) * w)[..., None]
        return at
    k.add(name, outward(sheet(along(None, 0.0, width), 90, 3, 0.008)), RED, "body", fuse=False, surface="cloth")
    k.add(name + "stripe", outward(sheet(along(None, 0.006, width * 0.42), 90, 3, 0.006)), STRIPE, "body", fuse=False,
          surface="cloth")
    for side, end in ((-1, P[0]), (1, P[-1])):
        tassel(k, f"{name}tassel{'L' if side < 0 else 'R'}", end + np.array([0, 0, -0.004]))


def turcanica(src, out_path):
    F = frame()
    k = Kit(src, coat="fur")
    # --- the body of wool, ringlets from the spine to the ground, flaring out at the foot ------------------------
    la.wool(k, "wool", F.BODY, F.BODY_R, "body", col=WOOL, n=30, puff=0.06, sink=0.036, seed=5,
            skip=lambda d: d[2] < -0.1)
    tips = []
    la.hanging_locks(k, F, n=34, top=0.5, bottom=0.03, r=0.036, seed=3, flare=0.075, skip=front_clear, segs=9,
                     wave=0.016, ends=tips)
    la.hanging_locks(k, F, name="upper", n=26, top=0.63, bottom=0.22, r=0.032, seed=8, flare=0.03, skip=front_clear,
                     segs=7, wave=0.014)
    k.add("tail", ell((0.0, 0.33, 0.5), (0.06, 0.05, 0.07)), WOOL, "body")
    k.paint(lambda P: inside(P[:, 2] - 0.075, 0.025), TIP, on=["lock"])
    k.paint(lambda P: inside(P[:, 2] - 0.34, 0.05) * inside(0.08 - P[:, 2], 0.03) * 0.4, WOOL_SHADE, on=["lock", "wool"])

    # --- the ruff round neck and chest, the topknot on the crown ---------------------------------------------------
    la.wool(k, "ruff", F.RUFF, F.RUFF_R, "head", col=WOOL, n=26, puff=0.05, sink=0.026, seed=13,
            skip=lambda d: d[1] < -0.5 and d[2] > 0.2)
    for i, x in enumerate((-0.09, -0.045, 0.0, 0.045, 0.09)):
        k.add(f"chestlock{i}", tube([(x, -0.3, 0.6), (x * 1.1, -0.33, 0.48), (x * 1.15, -0.31, 0.3)],
                                    [0.04, 0.034, 0.024]), WOOL if i % 2 else WOOL_SHADE, "head")
    k.add("chest", ell((0.0, -0.24, 0.38), (0.12, 0.09, 0.13)), WOOL, "body")
    la.wool(k, "cap", F.CAP, F.CAP_R, "head", col=WOOL, n=14, puff=0.034, sink=0.016, seed=17,
            skip=lambda d: d[1] < -0.55 and d[2] < 0.3)

    # --- the long black face, the muzzle forward and down -------------------------------------------------------
    k.add("face", ell(F.FACE, F.FACE_R), FACE, "head")
    k.add("snout", ell(F.SNOUT, F.SNOUT_R), FACE, "head")
    k.add("bridge", tube([F.FACE + np.array([0, -0.03, 0.01]), F.SNOUT + np.array([0, -0.01, 0.01])], [0.058, 0.046]),
          FACE, "head")
    k.paint(lambda P: inside(np.linalg.norm((P - F.FACE) / (F.FACE_R * 1.03), axis=1) - 1, 0.002)
            * inside(P[:, 1] + 0.33, 0.004), FACE)
    la.nose(k, F, half=(0.028, 0.03))

    # --- ears straight out sideways, the great horns over them ------------------------------------------------
    la.ears(k, F, (0.118, -0.34, 0.75), (0.06, 0.018, 0.026), (0, 10, -12))
    horn(k, F)

    # --- the embroidered ribbons over the back --------------------------------------------------------------------
    ribbon(k, F, "ribbon0", -0.12, 0.07, 0.24)
    ribbon(k, F, "ribbon1", 0.04, 0.08, 0.2)
    ribbon(k, F, "ribbon2", 0.2, 0.07, 0.24)

    # --- the red collar at the throat, the bell and its two tassels -----------------------------------------------
    axis = unit(0, -0.45, 0.89)
    cc = np.array([0, -0.27, 0.6])
    k.add("collar", ring_band(cc, axis, 0.128, 0.032, thick=0.012), RED, "head", fuse=False, surface="cloth")
    k.add("collarstripe", ring_band(cc, axis, 0.135, 0.012, thick=0.006), STRIPE, "head", fuse=False, surface="cloth")
    front = cc + unit(0, -0.89, -0.45) * 0.13
    la.bell(k, front + np.array([0, -0.01, -0.006]), (0, -0.2, -1), F.BELL_H, bone="head", band=RED)
    for s in (1, -1):
        tassel(k, "neck" + ("R" if s > 0 else "L"), front + np.array([0.062 * s, 0.01, -0.01]), bone="head",
               length=0.085)

    # --- the legs ----------------------------------------------------------------------------------------------
    la.legs(k, F)

    k.build(voxel=F.VOXEL, smooth=4, band=0.005, wband=0.05)
    k.eye_states(la.eyes(k, F, F.FACE, F.FACE_R, F.EYE_D, F.EYE_HALF, F.EYE_LID))
    k.export(out_path)


if __name__ == "__main__":
    turcanica(*args())
