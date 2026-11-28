# Lanita sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lanita.py -- <the export's Lanita.glb> <out.glb>
#
# The design, from its picture (docs/art/reference/gemini/carpathia/10023-Lanita.jpg) and brief (CP-dex-1-plateau,
# 23): a small mountain lamb that is mostly a barrel of long, hanging, wavy cream wool, its belly low; a mop of the
# same wool over its crown with a fringe hanging over the brow, cutting the top of its eyes; the face black, a short
# round muzzle, a small grey nose and a little smile; black ears hanging out sideways under the mop, a tiny dark
# curled horn above and behind each; a dark collar under the chin with a bronze bell hanging from it; four short
# thick black legs with dark hooves under the wool.
# Reference ratios (the picture): the wool 0.83 of the height tall, its length about the height; the head with its
# mop 0.42 of the height tall and 0.75 of the body's width; the black face 0.19 of the height; the legs showing 0.17
# of the height and 0.09 of it thick; the bell 0.12 of the height.
#
# Written to be grown from (turcanica.py imports it with sibling("lanita")): the frame is a namespace, the legs, the
# hanging locks, the bell, the ears, the nose and the eyes are functions of it; the build is under __main__.
import os, sys, math
from types import SimpleNamespace
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, ring_band, srgb, inside, unit, eye_look, args, outward, polygon, \
    sibling

mp = sibling("mareep")
wool = mp.wool
curve = mp.curve

# --- the line's colours --------------------------------------------------------------------------------------------
WOOL, WOOL_SHADE = "#EEE5CD", "#D9CAA6"
FACE = "#2C2A2D"
LEG, HOOF = "#2E2B2E", "#5A524C"
HORN = "#4A4547"
NOSE, MOUTH = "#55504F", "#77706C"
COLLAR = "#4A382E"
BRONZE, BRONZE_DARK, BELL_MOUTH = "#B07E4C", "#7A5634", "#2E2218"
WHITE, IRIS, PUPIL = srgb("#F4EFE6"), srgb("#6E4C34"), srgb("#161214")
GLINT = srgb("#FFFFFF")
SHUT_LINE = srgb("#A49C96")           # a light line on the black face for the closed states


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r, unturned) along the direction d from its centre, and its normal."""
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def frame():
    """Lanita's frame (joints: body (0, 0.01, 0.28), head (0, -0.15, 0.32), ears (+-0.07, -0.2, 0.34), legs
    (+-0.075, -0.1 / 0.1, 0.17))."""
    F = SimpleNamespace()
    F.BODY, F.BODY_R = np.array([0.0, 0.025, 0.27]), np.array([0.158, 0.205, 0.15])
    F.MOP, F.MOP_R = np.array([0.0, -0.165, 0.39]), np.array([0.118, 0.11, 0.1])
    F.FACE, F.FACE_R = np.array([0.0, -0.232, 0.345]), np.array([0.07, 0.066, 0.068])
    F.SNOUT, F.SNOUT_R = np.array([0.0, -0.282, 0.318]), np.array([0.05, 0.042, 0.042])
    F.LEG_X, F.LEG_Y, F.HIP, F.LEG_R = 0.075, (-0.1, 0.1), 0.17, 0.026
    F.EYE_D = unit(0.52, -0.78, 0.3)
    F.EYE_HALF = (0.022, 0.02)
    F.EYE_LID = 0.36                   # the fringe's cut over the eye, in v
    F.LOCKS, F.LOCK_TOP, F.LOCK_BOTTOM, F.LOCK_R = 26, 0.25, 0.1, 0.03
    F.BELL_H = 0.052
    F.VOXEL = 0.0055
    return F


# --- the parts -------------------------------------------------------------------------------------------------------
def hanging_locks(k, F, name="lock", bone="body", n=None, top=None, bottom=None, r=None, seed=3, flare=0.0,
                  skip=None, colours=(WOOL, WOOL, WOOL_SHADE), segs=5, wave=0.012, ends=None):
    """Long wavy locks hanging from round the body's middle to near the ground, fused into a skirt of wool. flare
    swings their ends out; skip(angle) leaves a place clear (the chest under a face)."""
    n = n or F.LOCKS; top = F.LOCK_TOP if top is None else top
    bottom = F.LOCK_BOTTOM if bottom is None else bottom; r = r or F.LOCK_R
    rs = np.random.RandomState(seed)
    c, R = F.BODY, F.BODY_R
    for i in range(n):
        a = 2 * math.pi * (i + rs.uniform(-0.2, 0.2)) / n
        if skip is not None and skip(a):
            continue
        dx, dy = math.cos(a), math.sin(a)
        k_ = math.sqrt(max(0.05, 1 - ((top - c[2]) / R[2]) ** 2))
        root = c + np.array([dx * R[0] * k_ * 0.96, dy * R[1] * k_ * 0.96, top - c[2]])
        out = np.array([dx, dy, 0.0])
        end_z = bottom + rs.uniform(-0.006, 0.01)
        pts = []
        for t in np.linspace(0, 1, segs):
            z = root[2] + (end_z - root[2]) * t
            wave_ = wave * math.sin(t * 2.6 * math.pi + i) + flare * t ** 1.6
            pts.append(root + out * (0.012 * t + wave_) + np.array([0, 0, z - root[2]]))
        rr = r * rs.uniform(0.9, 1.1)
        prof = np.interp(np.linspace(0, 1, segs), np.linspace(0, 1, 5), [1, 1.02, 0.96, 0.86, 0.7]) * rr
        k.add(f"{name}{i}", tube(pts, list(prof)), colours[i % len(colours)], bone)
        if ends is not None:
            ends.append((pts[-1], out))


def legs(k, F, color=LEG, hoof=HOOF):
    """Four short thick black legs under the wool, a darker cloven hoof at each foot."""
    r = F.LEG_R
    for y, bone in ((F.LEG_Y[0], "leg{s}"), (F.LEG_Y[1], "hind{s}")):
        x = F.LEG_X
        k.add("leg" + bone[:4], tube([(x, y, F.HIP), (x + 0.002, y - 0.003, r * 1.2)], [r * 1.05, r]), color, bone,
              mirror=True)
        hh = r * 0.75
        k.add("hoof" + bone[:4], ell((x + 0.002, y - 0.006, hh * 0.55), (r * 1.08, r * 1.18, hh * 0.62)), hoof, bone,
              mirror=True)
        def band(P, x=x, y=y):
            return inside(P[:, 2] - hh, 0.0015) * inside(np.abs(np.abs(P[:, 0]) - x) - r * 1.5, 0.002) \
                * inside(np.abs(P[:, 1] - y) - r * 1.6, 0.002)
        k.paint(band, hoof)


def ears(k, F, c, r, rot, bone="ear{s}"):
    """Black ears hanging out to the sides (authored on +X)."""
    k.add("ear", ell(c, r, rot=rot), FACE, bone, mirror=True)


def curl_horn(k, F, centre, r0, r1, turns, thick0, thick1, start, out, name="horn", bone="head", colour=HORN):
    """A small curled horn: a spiral round centre in the plane square to x, its radius r0 to r1, moving out along x
    by out; start is the angle it leaves the head at (0 up, pi/2 back). Authored on +X, mirrored."""
    pts = []
    n = 14
    for i in range(n):
        t = i / (n - 1)
        a = start + t * turns * 2 * math.pi
        rr = r0 + (r1 - r0) * t
        pts.append(np.array(centre) + np.array([out * t, rr * math.sin(a), rr * math.cos(a)]))
    radii = list(np.linspace(thick0, thick1, n))
    k.add(name, outward(tube(pts, radii, seg=14, per=4)), colour, bone, fuse=False, mirror=True, surface="shell")


def bell(k, top, d, h, bone="body", name="bell", band=None):
    """A bronze sheep bell hanging from top along d, h tall: a lip, a dark mouth under it and a clapper. band: a
    colour for an embroidered band round its waist."""
    d = unit(*d)
    prof = [(0.0, 0.0), (0.3 * h, 0.0), (0.4 * h, 0.1 * h), (0.43 * h, 0.4 * h), (0.52 * h, 0.72 * h),
            (0.66 * h, 0.93 * h), (0.7 * h, 1.0 * h), (0.6 * h, 1.0 * h), (0.5 * h, 0.88 * h), (0.0, 0.84 * h)]
    k.add(name, lathe(top, d, prof, seg=36), BRONZE, bone, fuse=False, surface="metal")
    k.add(name + "mouth", lathe(np.array(top) + d * 0.86 * h, d,
                                [(0.0, 0.0), (0.5 * h, 0.0), (0.5 * h, 0.03 * h), (0.0, 0.03 * h)], seg=36),
          BELL_MOUTH, bone, fuse=False, surface="metal")
    k.add(name + "clapper", outward(ell(np.array(top) + d * 0.98 * h, (0.16 * h, 0.16 * h, 0.16 * h), sub=3)),
          BRONZE_DARK, bone, fuse=False, surface="metal")
    k.add(name + "loop", outward(ell(np.array(top) - d * 0.02 * h, (0.2 * h, 0.12 * h, 0.2 * h), sub=3)), BRONZE_DARK,
          bone, fuse=False, surface="metal")
    if band is not None:
        k.add(name + "band", ring_band(np.array(top) + d * 0.6 * h, d, lambda v: 0.47 * h + 0.06 * h * v + 0.004 * h,
                                       0.18 * h, thick=0.05 * h), band, bone, fuse=False, surface="cloth")


def nose(k, F, nose_c=NOSE, mouth_c=MOUTH, face=FACE, half=(0.024, 0.026), lift=(0, 0, -0.004)):
    """The grey nose and the little smile under it, one patch on the muzzle's front."""
    NP, NN = on_ell(F.SNOUT, F.SNOUT_R, (0, -1, 0.2))
    N_, M_, F_ = srgb(nose_c), srgb(mouth_c), srgb(face)
    def look(u, v):
        col = np.tile(F_, (len(u), 1))
        col[((u / 0.6) ** 2 + ((v - 0.38) / 0.36) ** 2 < 1) & (v > 0.38 - 0.62 + 0.6 * np.abs(u))] = N_
        col[(np.abs(v + 0.05) < 0.1) & (np.abs(u) < 0.06)] = M_
        smile = np.abs(v - (-0.45 + 0.55 * u * u)) < 0.11
        col[smile & (np.abs(u) < 0.62)] = M_
        return col
    k.decal(NP + np.array(lift), NN, half, look, eps=0.0018, rings=18, segs=56)


def eye_open(u, v, white=WHITE, iris=IRIS, pupil=PUPIL):
    """A cream eye with a large brown iris, a black pupil and a glint, its top cut by the patch's own outline."""
    col = np.tile(white, (len(u), 1))
    col[np.hypot(u + 0.1, (v + 0.08) / 1.05) < 0.62] = iris
    col[np.hypot(u + 0.1, v + 0.08) < 0.36] = pupil
    col[np.hypot(u + 0.28, v - 0.05) < 0.14] = GLINT
    return col


def eyes(k, F, centre, radii, d, half, lid, lid_colour=FACE, line_colour=SHUT_LINE, look=eye_open):
    """The eyes on the black face, their tops cut flat by the fringe (or a brow) at v = lid."""
    E, N = on_ell(centre, radii, d)
    # a gentle arch, high in the middle (a flat cut sloping to the nose scowls)
    cut = polygon([(-1.3, lid - 0.12), (-0.4, lid + 0.04), (0.4, lid + 0.04), (1.3, lid - 0.12), (1.3, -1.3),
                   (-1.3, -1.3)])
    def lay(state):
        k.decal(E, N, half, eye_look(state, look, srgb(lid_colour), line_colour, line=0.16), eps=0.0018, rings=20,
                segs=56, mirror=True, outline=lambda th: min(1.0, cut(th)))
    return lay


# --- Lanita -----------------------------------------------------------------------------------------------------------
def lanita(src, out_path):
    F = frame()
    k = Kit(src, coat="fur")
    # --- the barrel of wool, its top puffed, long locks hanging round it nearly to the ground ---------------------
    wool(k, "wool", F.BODY, F.BODY_R, "body", col=WOOL, n=30, puff=0.05, sink=0.03, seed=7,
         skip=lambda d: d[2] < -0.2)
    hanging_locks(k, F)
    k.add("tail", ell((0.0, 0.235, 0.29), (0.045, 0.04, 0.05)), WOOL, "body")
    k.paint(lambda P: inside(P[:, 2] - 0.15, 0.03) * 0.55, WOOL_SHADE, on=["wool", "lock"])

    # --- the mop of wool on the crown, joined to the body; the fringe over the brow -------------------------------
    wool(k, "mop", F.MOP, F.MOP_R, "head", col=WOOL, n=18, puff=0.04, sink=0.022, seed=11,
         skip=lambda d: d[1] < -0.45 and d[2] < 0.45)
    for i, x in enumerate((-0.072, -0.044, -0.015, 0.015, 0.044, 0.072)):
        z0 = 0.455 - 3.0 * x * x
        end = (x * 1.05, -0.282 + 0.06 * abs(x), 0.392 + 0.15 * abs(x))
        k.add(f"fringe{i}", tube([(x * 0.9, -0.2, z0), (x, -0.255, z0 - 0.02), end],
                                 [0.024, 0.022, 0.014]), WOOL if i % 3 else WOOL_SHADE, "head")
    # the wool round the cheeks and under the chin, framing the face
    for s in (1, -1):
        k.add("cheek", ell((0.072 * s, -0.215, 0.305), (0.04, 0.05, 0.055)), WOOL, "head")

    # --- the black face, the short muzzle -----------------------------------------------------------------------
    k.add("face", ell(F.FACE, F.FACE_R), FACE, "head")
    k.add("snout", ell(F.SNOUT, F.SNOUT_R), FACE, "head")
    k.paint(lambda P: inside(np.linalg.norm((P - F.FACE) / (F.FACE_R * 1.02), axis=1) - 1, 0.002)
            * inside(P[:, 1] + 0.21, 0.004), FACE)
    nose(k, F)

    # --- the ears hanging out to the sides, the tiny curled horns above and behind them -------------------------
    ears(k, F, (0.112, -0.215, 0.338), (0.046, 0.016, 0.022), (0, 12, -28))
    curl_horn(k, F, (0.104, -0.18, 0.384), 0.027, 0.012, 1.15, 0.0145, 0.009, -0.6, 0.03)

    # --- the collar under the chin and the bronze bell ----------------------------------------------------------
    axis = unit(0, -0.55, 0.84)
    k.add("collar", ring_band((0, -0.17, 0.29), axis, 0.088, 0.018, thick=0.01), COLLAR, "body", fuse=False,
          surface="leather")
    front = np.array([0, -0.17, 0.29]) + unit(0, -0.84, -0.55) * 0.088
    bell(k, front + np.array([0, -0.006, -0.004]), (0, -0.22, -1), F.BELL_H)

    # --- the legs ----------------------------------------------------------------------------------------------
    legs(k, F)

    k.build(voxel=F.VOXEL, smooth=4, band=0.004, wband=0.035)
    k.eye_states(eyes(k, F, F.FACE, F.FACE_R, F.EYE_D, F.EYE_HALF, F.EYE_LID))
    k.export(out_path)


if __name__ == "__main__":
    lanita(*args())
