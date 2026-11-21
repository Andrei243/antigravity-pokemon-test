# Tapu Bulu sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tapubulu.py -- <the export's Tapu Bulu.glb> <out.glb>
#
# The guardians' frame is tapukoko.py's (sibling). From the references, Tapu Bulu: a big round black body that is
# also its head, small teal eyes peeping from under the brim of its shell, a white mask of bars across the face (a
# bar slanting out under each eye, a broad bar for a mouth) and a silver ring through the nose. Its shell is a wide
# red hat over its head: a tall box in the middle and a wing out to each side sloping down to a turned-down end, a
# white band with a black zigzag along the wings' edges; two black horns tipped yellow stand on the box. Red pads on
# its shoulders, black arms bending down and forward to black fists with yellow rings at the wrists. Below, a short
# black tail with a yellow knot ends in a big yellow bell with a black zigzag round it and a dark clapper.
# The skeleton: root, body (0, 0, 0.392), head (0, -0.02, 0.452), armR/L (+-0.04, -0.02, 0.442).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, sheet, slab, hull, lathe, ring_band, outward, srgb, unit, tri, polygon, args,
                       sibling)

T = sibling("tapukoko")

BLACK, RED, RED_D, YELLOW = "#2A2830", "#D8383A", "#A42A2E", "#E8C230"
WHITE, SILVER, TEAL, INK = "#F6F4F0", "#C8CCD6", "#58CCC8", "#16141A"

BULU = dict(
    body=BLACK,
    torso=[((0, -0.004, 0.398), (0.075, 0.065, 0.088)), ((0, 0.002, 0.33), (0.048, 0.044, 0.05)),
           ((0, -0.01, 0.462), (0.067, 0.059, 0.056))],
    neck=((0, -0.01, 0.44), (0, -0.012, 0.468)), neck_r=0.04,
    head_c=(0, -0.012, 0.47), head_r=(0.064, 0.058, 0.05),
    arm=((0.045, -0.02, 0.442), (0.104, -0.035, 0.375), (0.084, -0.088, 0.312)), arm_r=0.018, hand_r=0.025,
    eye_c=(0.028, -0.064, 0.474), eye_n=unit(0.4, -0.91, 0.08), eye_half=(0.0145, 0.013),
)
FACE_C, FACE_R = np.array([0.0, -0.01, 0.462]), np.array([0.067, 0.059, 0.056])


def on_ell(c, r, d):
    d = np.array(d, float); d /= np.linalg.norm(d)
    p = d / math.sqrt(((d / r) ** 2).sum())
    n = p / r ** 2
    return c + p, n / np.linalg.norm(n)


def box(x0, x1, y0, y1, z0, z1):
    """A block whose bottom and top are z0(x) and z1(x) (numbers or functions of x)."""
    zb = z0 if callable(z0) else (lambda x: z0)
    zt = z1 if callable(z1) else (lambda x: z1)
    return hull([(x, y, z(x)) for x in (x0, x1) for y in (y0, y1) for z in (zb, zt)])


# --- the hat ----------------------------------------------------------------------------------------------------------
HAT_Y = (-0.095, 0.08)
BOX_X, BOX_Z = 0.074, (0.488, 0.585)
WING_IN, WING_OUT = 0.06, 0.232


def wing_bottom(x):
    return 0.5 - (abs(x) - WING_IN) / (WING_OUT - WING_IN) * 0.046


def wing_top(x):
    return 0.548 - (abs(x) - WING_IN) / (WING_OUT - WING_IN) * 0.05


def zigzag(k, name, s, y, bone):
    """The white band and the black zigzag along a wing's lower edge, on its face at y (front or back)."""
    xs = np.linspace(0.078, 0.214, 2)
    lo = [(x * s, wing_bottom(x) + 0.003) for x in xs]
    hi = [(x * s, wing_bottom(x) + 0.028) for x in xs[::-1]]
    out = 1 if y > 0 else -1
    k.add(f"{name}band", slab(lo + hi, (0, y + out * 0.0018, 0), (1, 0, 0), (0, 0, 1), 0.003), WHITE, bone,
          fuse=False)
    n = 7
    pts = []
    for i in range(2 * n + 1):
        x = 0.08 + (0.212 - 0.08) * i / (2 * n)
        h = 0.003 + (0.025 if i % 2 == 0 else 0.004)
        pts.append((x * s, wing_bottom(x) + h))
    pts += [(0.212 * s, wing_bottom(0.212) + 0.0015), (0.08 * s, wing_bottom(0.08) + 0.0015)]
    k.add(f"{name}zig", slab(pts, (0, y + out * 0.0036, 0), (1, 0, 0), (0, 0, 1), 0.003), INK, bone, fuse=False)


def hat(k):
    k.add("hatbox", box(-BOX_X, BOX_X, HAT_Y[0], HAT_Y[1], BOX_Z[0], lambda x: BOX_Z[1]), RED, "head", fuse=False)
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"wing{side}", box(WING_IN * s, WING_OUT * s, HAT_Y[0] + 0.004, HAT_Y[1] - 0.004, wing_bottom, wing_top),
              RED, "head", fuse=False)
        k.add(f"lip{side}", box((WING_OUT - 0.022) * s, (WING_OUT + 0.004) * s, HAT_Y[0] + 0.006, HAT_Y[1] - 0.006,
                                0.428, wing_bottom(WING_OUT) + 0.008), RED_D, "head", fuse=False)
        zigzag(k, f"front{side}", s, HAT_Y[0] + 0.004, "head")
        zigzag(k, f"back{side}", s, HAT_Y[1] - 0.004, "head")
        # the horns: black, tipped yellow, out a little and up
        b0 = np.array([0.042 * s, -0.004, 0.575])
        tip = np.array([0.062 * s, 0.004, 0.722])
        k.add(f"horn{side}", T.spike(b0, tip, 0.027, n=7), BLACK, "head", fuse=False)
        d = (tip - b0) / np.linalg.norm(tip - b0)
        k.add(f"horntip{side}", T.spike(b0 + (tip - b0) * 0.55, tip + d * 0.004, 0.0136, n=7), YELLOW, "head",
              fuse=False)
    # darker lines across the box's front, the grain of its wood
    for z in (0.512, 0.536, 0.56):
        k.add(f"grain{int(z * 1000)}", slab([(-0.066, z - 0.0025), (0.066, z - 0.0025), (0.066, z + 0.0025),
                                             (-0.066, z + 0.0025)], (0, HAT_Y[0] - 0.0012, 0), (1, 0, 0), (0, 0, 1),
                                            0.002), RED_D, "head", fuse=False)


# --- the bell -------------------------------------------------------------------------------------------------------
BELL_C = np.array([0.0, 0.012, 0.0])
BELL = [(0.0, 0.206), (0.017, 0.203), (0.028, 0.19), (0.034, 0.16), (0.041, 0.12), (0.05, 0.096), (0.055, 0.083),
        (0.051, 0.074), (0.0, 0.074)]


def bell_radius(z):
    rs = [r for r, h in BELL[1:-1]][::-1]
    zs = [h for r, h in BELL[1:-1]][::-1]
    return np.interp(z, zs, rs)


def bell(k):
    k.add("tail", tube([(0, 0.004, 0.32), (0, 0.016, 0.262), (0, 0.012, 0.2)], [0.014, 0.012, 0.012]), BLACK,
          "body")
    k.add("knot", ring_band((0, 0.015, 0.25), (0, 0.15, 1), 0.0135, 0.012, thick=0.007), YELLOW, "body", fuse=False,
          surface="metal")
    k.add("bell", lathe(BELL_C, (0, 0, 1), BELL, seg=48), YELLOW, "body", fuse=False, surface="metal")
    def at(u, v):
        th = 2 * math.pi * u + math.pi / 2
        top = 0.1 + 0.026 * (0.12 + 0.88 * (0.5 + 0.5 * tri(7 * u)))
        z = 0.098 + (top - 0.098) * v
        r = bell_radius(z) + 0.0025
        return np.stack([BELL_C[0] + r * np.cos(th), BELL_C[1] + r * np.sin(th), z], axis=-1)
    k.add("bellzig", outward(sheet(at, 140, 3, 0.003)), INK, "body", fuse=False)
    k.add("clapper", T.spike((0, 0.012, 0.08), (0, 0.012, 0.044), 0.013, n=6), INK, "body", fuse=False)


# --- the face -------------------------------------------------------------------------------------------------------
def face(k):
    for s in (1, -1):
        p, n = on_ell(FACE_C, FACE_R, (0.55 * s, -0.83, -0.32))
        bar = [(-1, -0.35), (1, -1), (1, 0.15), (-1, 1)] if s > 0 else [(1, -0.35), (-1, -1), (-1, 0.15), (1, 1)]
        k.decal(p, n, (0.026, 0.012), WHITE, outline=polygon(bar), eps=0.0025, rings=10, segs=48)
    p, n = on_ell(np.array([0, -0.004, 0.398]), np.array([0.075, 0.065, 0.088]), (0, -1, 0.06))
    k.decal(p, n, (0.034, 0.011), WHITE, eps=0.0025, rings=10, segs=60,
            outline=polygon([(-1, -0.6), (-0.85, 1), (0.85, 1), (1, -0.6), (0.7, -1), (-0.7, -1)]))
    k.add("ring", ring_band((0, -0.07, 0.447), (0, 1, 0), 0.0125, 0.008, thick=0.0075), SILVER, "head", fuse=False,
          surface="metal")


def bulu_extras(k, F):
    S, E, H = (np.array(p, float) for p in F["arm"])
    k.add("pad", ell((0.072, -0.012, 0.442), (0.036, 0.036, 0.031)), RED, "arm{s}", mirror=True)
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"cuff{side}", ring_band((E + (H - E) * 0.66) * m, (H - E) * m, 0.019, 0.018, thick=0.008), YELLOW,
              f"arm{side}", fuse=False, surface="metal")
    face(k)
    hat(k)
    bell(k)


def bulu_eyes(k, F):
    IRIS, PUP = srgb(TEAL), srgb("#12262A")

    def open_eye(u, v):
        c = np.tile(srgb(BLACK), (len(u), 1))
        r = (u / 0.96) ** 2 + (v / 0.96) ** 2
        c[r < 1] = IRIS
        c[((u + 0.04) / 0.42) ** 2 + ((v + 0.06) / 0.48) ** 2 < 1] = PUP
        c[((u + 0.28) / 0.16) ** 2 + ((v - 0.26) / 0.16) ** 2 < 1] = (1, 1, 1)
        return c
    return T.eye_lay(k, F, open_eye, BLACK, "#9AA2AC", line=0.18)


if __name__ == "__main__":
    src, out = args()
    T.guardian(src, out, BULU, bulu_extras, bulu_eyes, voxel=0.0045)
