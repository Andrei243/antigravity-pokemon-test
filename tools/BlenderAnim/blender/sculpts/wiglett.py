# Wiglett sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/wiglett.py -- <the export's Wiglett.glb> <out.glb>
#
# Written to be grown from (Wugtrio is three of these eels: sibling("wiglett")). From the references: a thick white
# garden eel standing up out of a low round patch of grey-green sand (darker stripes and dark specks on it), rising
# straight and then arching forward so that its rounded end, the head, looks to the front; on the head two small dark
# navy upright oval eyes with a white glint, set close high on the end, and under them at the tip a big red-pink nose
# like pursed lips, as wide as the eel. The drawing's grey lower trunk is shade: the 3D model is white to the sand.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, outward, args

WHITE, NOSE, NAVY = "#F4F2EE", "#EC5466", "#22285A"
SAND, SAND_DARK, SPECK = "#93B8A2", "#7CA48E", "#4E6A5C"

# --- Wiglett's frame (export: 0.54 tall; the head bone at 0, -0.02, 0.42) -----------------------------------------
PATH = [(0, 0.05, 0.05), (0, 0.062, 0.16), (0, 0.058, 0.28), (0, 0.036, 0.39), (0, -0.002, 0.458), (0, -0.05, 0.47),
        (0, -0.095, 0.454)]
RADII = [0.05, 0.048, 0.047, 0.048, 0.051, 0.054, 0.054]
EYE_UP, EYE_SIDE = 0.62, 0.36           # the eyes' place on the head's cap, along its up and across
EYE_HALF = (0.0105, 0.0165)             # an eye's half width and height, for a head of radius 0.054
NOSE_DOWN = 0.28                        # the nose's place under the face's middle, a share of the radius
NOSE_R = (0.84, 0.46, 0.44)             # its half sizes (across, forward, up), shares of the radius
VOXEL = 0.0052


def frame(f):
    """A face's frame from the way it looks: f, its up (toward the sky) and its side (toward +X when f is -Y)."""
    f = unit(*f)
    u = np.array([0, 0, 1.0]) - f * f[2]
    if np.linalg.norm(u) < 1e-3:
        u = np.array([0, 1.0, 0])
    u /= np.linalg.norm(u)
    s = np.cross(u, f)
    return f, u, s


def eel(k, tag, pts, radii, color, low, high, split):
    """One eel's trunk: a round tube through pts, its points up to split on bone low, the rest on bone high (they
    overlap by a point, so the two fuse into one trunk)."""
    k.add(f"eel{tag}lo", tube(pts[:split + 1], radii[:split + 1]), color, low)
    k.add(f"eel{tag}hi", tube(pts[split:], radii[split:]), color, high)


def head(k, tag, end, look, R, bone, nose_color, nose_r=NOSE_R, nose_down=NOSE_DOWN, eye_up=EYE_UP,
         eye_side=EYE_SIDE, eye_half=EYE_HALF):
    """The face on an eel's rounded end (a cap of radius R about end), looking along look: a loose nose at the tip,
    and where the eyes go, for eyes()."""
    f, u, s = frame(look)
    end = np.array(end, float)
    nd = unit(*(f - u * nose_down))
    nc = end + nd * R * 0.9
    V, F = ell((0, 0, 0), (nose_r[0] * R, nose_r[1] * R, nose_r[2] * R), sub=4)
    V = V[:, :1] * s + V[:, 1:2] * f + V[:, 2:3] * u + nc
    k.add(f"nose{tag}", outward((V, F)), nose_color, bone, fuse=False, surface="shell")
    spots = []
    scale = R / 0.054
    for side in (1, -1):
        d = unit(*(f + u * eye_up + s * eye_side * side))
        spots.append((end + d * R, d, side))
    return dict(spots=spots, half=(eye_half[0] * scale, eye_half[1] * scale))


def dark_eye(color):
    """An upright dark oval, a white glint high on it (u outward, v up)."""
    K = srgb(color)
    def eye(u, v):
        c = np.tile(K, (len(u), 1))
        c[((u - 0.05) / 0.42) ** 2 + ((v - 0.42) / 0.28) ** 2 < 1] = np.ones(3)
        return c
    return eye


def eyes(k, specs, state, lid, color, ink=None):
    look = eye_look(state, dark_eye(color), srgb(lid), srgb(ink or color), line=0.18)
    for sp in specs:
        for at, n, side in sp["spots"]:
            col = look if side > 0 else (lambda u, v, look=look: look(-u, v))
            k.decal(at, n, sp["half"], col, eps=0.004, rings=20, segs=56)


def sand(k, rx=0.13, ry=0.12, h=0.032, keep_off=(0.0, 0.05, 0.09)):
    """The low round patch of sand: a flattened dome, darker stripes across it and dark specks."""
    def flat(V):
        V = V.copy(); V[:, 2] = np.maximum(V[:, 2], -0.2); return V
    k.add("sand", ell((0, 0.01, 0.006), (rx, ry, h), deform=flat), SAND, "root", surface="shell")
    k.paint(lambda P: inside(np.abs(np.sin((P[:, 0] * 0.6 + P[:, 1]) * 34)) - 0.45, 0.04) * (P[:, 2] < 0.04)
            * inside(np.hypot(P[:, 0], P[:, 1] - 0.05) - 0.07, 0.003).__rsub__(1), SAND_DARK, on=["sand"])
    rs = np.random.RandomState(9)
    for i in range(9):
        a = 2 * math.pi * (i + rs.uniform(-0.3, 0.3)) / 9
        rr = rs.uniform(0.6, 0.85)
        p = np.array([math.cos(a) * rx * rr, 0.01 + math.sin(a) * ry * rr, 0.0])
        if math.hypot(p[0] - keep_off[0], p[1] - keep_off[1]) < keep_off[2]:
            continue                        # a speck under the trunk would land on it as a streak
        q = (p - np.array([0, 0.01, 0.006])) / np.array([rx, ry, h])
        p[2] = 0.006 + h * math.sqrt(max(1 - q[0] ** 2 - q[1] ** 2, 0.04))
        n = unit(q[0] / rx, q[1] / ry, max(math.sqrt(max(1 - q[0] ** 2 - q[1] ** 2, 0)), 0.2) / h)
        k.decal(p, n, (0.0045, 0.0045), SPECK, eps=0.002, rings=4, segs=16)


def wiglett(src, out):
    k = Kit(src, coat="scales")
    sand(k)
    eel(k, "", PATH, RADII, WHITE, "body", "head", 3)
    look = unit(*(np.array(PATH[-1]) - np.array(PATH[-2])))
    look = unit(*(look + np.array([0, 0, 0.15])))            # the face a little up from the tube's end
    spec = head(k, "", PATH[-1], look, RADII[-1], "head", NOSE)
    k.build(voxel=VOXEL, band=0.004)
    k.eye_states(lambda st: eyes(k, [spec], st, WHITE, NAVY))
    k.export(out)


if __name__ == "__main__":
    wiglett(*args())
