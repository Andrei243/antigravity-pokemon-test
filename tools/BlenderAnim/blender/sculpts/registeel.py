# Registeel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/registeel.py -- <the export's Registeel.glb> <out.glb>
#
# The design, from the references: a golem of smooth pale grey steel. Its body is its head: a great ball, a smaller
# ball on each shoulder, a broad flat ring round its waist under the ball and two short round legs under the ring.
# Down its front a dark grey band outlined in black, narrow over the top, widest at the middle and rounded at its foot
# by the waist; on the band the face, seven red dots in a ring of six round a middle one (two above, three across,
# two below). Its arms are dark grey, thin at the shoulder and widening down to the hands (hanging down here, carried a
# little forward), three red triangles on the back of each hand and three thick pale grey fingers.
# The brief expected black fingers and black bands on the arms: the pictures show pale grey fingers and plain dark
# arms with red marks on the hands. The eyes' five states are laid on the dots: open they shine, shut they dim with a
# dark line across (regigigas.py's gems).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, inside, eye_look, args, lathe, polygon, outward

# --- colours ----------------------------------------------------------------------------------------------------------
STEEL, STEELPALE, BAND, INK, ARM = "#C2C4CA", "#D6D8DC", "#4C4D53", "#18181B", "#55565D"
RED, REDRIM = "#EC3C44", "#8E1A20"
E_ = 0.003                    # a crisp paint's edge

# --- the frame --------------------------------------------------------------------------------------------------------
BALL_C, BALL_R = np.array([0.0, 0.0, 0.585]), np.array([0.29, 0.275, 0.295])
FACE_Z = 0.645                # the middle dot
DOT_DX, DOT_DZ, DOT_HALF = 0.096, 0.066, 0.024
RING = [(0, 0), (-1, 0), (1, 0), (-0.5, 1), (0.5, 1), (-0.5, -1), (0.5, -1)]
SHOULDER = np.array([0.31, 0.01, 0.66])
HAND = np.array([0.375, -0.105, 0.245])


def band_half(z):
    """The dark band's half width up the ball (a projection on x), nought where it ends."""
    return np.interp(z, [0.315, 0.335, 0.39, 0.5, 0.7, 0.785, 0.83, 0.858],
                     [0.0, 0.08, 0.13, 0.163, 0.163, 0.125, 0.07, 0.025], left=0.0, right=0.0)


def ball_point(x, z):
    """A point of the ball's front at width x and height z, and its outward normal."""
    lx, lz = x / BALL_R[0], (z - BALL_C[2]) / BALL_R[2]
    ly = -math.sqrt(max(1 - lx * lx - lz * lz, 0.01))
    p = BALL_C + np.array([lx, ly, lz]) * BALL_R
    n = unit(lx / BALL_R[0], ly / BALL_R[1], lz / BALL_R[2])
    return p, n


def body(k):
    k.add("ball", ell(BALL_C, BALL_R), STEEL, "body")
    for s, side in ((1, "R"), (-1, "L")):
        k.add(f"shoulder{side}", ell(SHOULDER * (s, 1, 1), (0.095, 0.092, 0.09)), STEEL, f"arm{side}")
    # the waist's ring: a broad flat disc with a rounded rim
    prof = [(0, 0.225), (0.24, 0.225), (0.268, 0.24), (0.278, 0.265), (0.27, 0.29), (0.245, 0.305), (0, 0.312)]
    k.add("waist", lathe((0, 0, 0), (0, 0, 1), prof, seg=64), STEELPALE, "body", fuse=False, surface="metal")
    for s, side in ((1, "R"), (-1, "L")):
        # a short round leg with a flat foot: a cylinder with rounded edges
        def cyl(V):
            n = ((V[:, 0] ** 2 + V[:, 1] ** 2) ** 3 + V[:, 2] ** 6) ** (1 / 6)
            return V / n[:, None]
        k.add(f"leg{side}", ell((0.115 * s, 0.0, 0.135), (0.078, 0.078, 0.135), deform=cyl), STEELPALE, f"leg{side}")




BAND_Z, BAND_HU, BAND_HV, BAND_INK = 0.585, 0.17, 0.275, 0.013


def band(k):
    """
    The dark band down the front, outlined in black: one patch cast from the front, its outline the band's own (painted,
    its edge hatched at the voxels where the ball turns away).
    """
    zs = np.linspace(0.315, 0.858, 48)
    right = [(band_half(z) / BAND_HU, (z - BAND_Z) / BAND_HV) for z in zs]
    left = [(-u, v) for u, v in right[::-1]]
    outline = polygon(right + left)
    ink, dark = srgb(INK), srgb(BAND)
    def look(u, v):
        x, z = u * BAND_HU, BAND_Z + v * BAND_HV
        inner = (np.abs(x) < band_half(z) - BAND_INK) & (z > 0.33) & (z < 0.858 - BAND_INK)
        c = np.tile(ink, (len(u), 1))
        c[inner] = dark
        return c
    k.decal((0, -0.2, BAND_Z), (0, -1.0, 0), (BAND_HU, BAND_HV), look, eps=0.002, outline=outline, rings=40, segs=160)


def arms(k):
    for s, side in ((1, "R"), (-1, "L")):
        bone = f"arm{side}"
        m = np.array([s, 1, 1])
        a0, a1, a2 = np.array([0.33, 0.0, 0.6]) * m, np.array([0.355, -0.05, 0.43]) * m, HAND * m
        k.add(f"arm{side}", tube([a0, a1, a2], [0.036, 0.042, 0.056]), ARM, bone)
        k.add(f"hand{side}", ell(HAND * m + (0, 0, -0.01), (0.066, 0.052, 0.058), rot=(0, -10 * s, 0)), ARM, bone)
        # three thick pale fingers, side by side across the hand's width, curling a little in
        for j, dx in enumerate((-0.04, 0.0, 0.04)):
            root = HAND * m + np.array([(dx + 0.005) * s, -0.012 - 0.01 * j, -0.035])
            mid = root + np.array([dx * 0.25 * s, -0.01, -0.05])
            tip = mid + np.array([-0.012 * s, 0.0, -0.035])
            k.add(f"finger{j}{side}", outward(tube([root, mid, tip], [0.025, 0.024, 0.022])), STEELPALE, bone,
                  fuse=False, surface="metal")


TRI = polygon([(0.0, 1.0), (-0.95, -0.75), (0.95, -0.75)])


def hand_marks(k):
    """Three red triangles in a row on the back of each hand, pointing up the arm."""
    for s in (1, -1):
        n = unit(0.55 * s, -0.75, 0.25)
        for j, dx in enumerate((-0.034, 0.0, 0.034)):
            c = HAND * np.array([s, 1, 1]) + np.array([dx * s * 0.8, -0.045 + 0.02 * dx * s, 0.015 + 0.002 * j])
            k.decal(c, n, (0.012, 0.016), RED, eps=0.003, outline=TRI, rings=10, segs=90)


# --- the dots, which are the eyes -------------------------------------------------------------------------------------
def dot_look():
    c0, rim, hi = srgb(RED), srgb(REDRIM), np.array([1.0, 0.85, 0.85])
    def look(u, v):
        c = np.tile(c0, (len(u), 1))
        c[u * u + v * v > 0.82 ** 2] = rim
        c[(u + 0.25) ** 2 + (v - 0.3) ** 2 < 0.22 ** 2] = hi
        return c
    return look


def dots(k, state):
    c0 = srgb(RED)
    look = eye_look(state, dot_look(), c0 * 0.4, c0 * 0.12, line=0.2)
    for cx, cz in RING:
        p, n = ball_point(cx * DOT_DX, FACE_Z + cz * DOT_DZ)
        k.decal(p, n, (DOT_HALF, DOT_HALF), look, eps=0.006, rings=20, segs=56)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="metal")
    body(k)
    band(k)
    arms(k)
    hand_marks(k)
    k.build(voxel=0.008, smooth=6, band=0.004)
    k.eye_states(lambda state: dots(k, state))
    k.export(out)
