# Amaura sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/amaura.py -- <the export's Amaura.glb> <out.glb>
#
# From the references: a small pale sky-blue sauropod. A round pear of a body, low to the ground, on four short thick
# legs with one small white nail at the front of each foot; a white belly under it; a short tail rising back to a
# point; a long neck rising from the chest in a gentle S to a round head with a little snout, a small smile and very
# large eyes (a dark blue iris, a white glint, a sliver of white at the back). Two fan-shaped frills on the top of the
# head, a mirror pair spread up and out in a V, each with three rounded lobes at its end, yellow at the root and pink
# at the lobes. A faceted diamond of ice on each flank, white above and blue below.
# Reference ratios (art): the head 0.21 of the height (built a tenth larger), the frills 0.25 of the height above the
# head, the body 0.28 of the height and 0.55 of the length, the neck some 0.07 of the height across.
# The export's joints: body (0, 0.02, 0.12), tail (0, 0.092, 0.12), head (0, -0.08, 0.34),
# legs (+-0.04, -0.05, 0.1), hinds (+-0.04, 0.07, 0.1).
#
# Aurorus grows from this script's frame (sibling("amaura")): its eyes, its crystals and its nails.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, eye_look, args

BLUE, BELLY = "#9ED4F2", "#F2F6FA"
YELLOW, PINK = "#F8E68C", "#F6A6B6"
ICE, ICE_TOP = "#6EC2EE", "#EEF8FF"
NAIL = "#F4F6F8"
WHITE, INK = "#FFFFFF", "#1A2440"
IRIS_TOP, IRIS_LOW, PUPIL = "#1C3480", "#4C86DC", "#0E1838"

H = np.array([0.0, -0.088, 0.336])          # the head's middle
HR = np.array([0.057, 0.06, 0.05])


# --- shared with Aurorus ------------------------------------------------------------------------------------------------
def crystal(k, name, c, n, size, bone, low=ICE, top=ICE_TOP, sink=0.4, rise=0.55):
    """A diamond of ice on a flank: a flat faceted rhombus, its upper facets pale and its lower ones blue."""
    c, n = np.array(c, float), unit(*n)
    up = np.array([0, 0, 1.0]) - n * n[2]; up /= np.linalg.norm(up)
    along = np.cross(up, n)
    base = c - n * size * sink
    apex = c + n * size * rise
    t, b = base + up * size, base - up * size
    f, r = base + along * size * 0.7, base - along * size * 0.7
    mid = base + n * size * 0.05
    k.add(name + "top", hull([t, f, r, apex, mid]), top, bone, fuse=False, surface="shell")
    k.add(name + "low", hull([b, f, r, apex, mid]), low, bone, fuse=False, surface="shell")


def nail(k, name, c, d, size, bone, color=NAIL, mirror=True):
    """A small white nail at the front of a foot, pointing forward and down."""
    c, d = np.array(c, float), unit(*d)
    side = np.array([1.0, 0, 0])
    up = np.cross(d, side); up /= np.linalg.norm(up)
    pts = [c + side * size * 0.6, c - side * size * 0.6, c + up * size * 0.45, c - up * size * 0.3,
           c - d * size * 0.5, c + d * size * 0.9 - up * size * 0.15]
    k.add(name, hull(pts), color, bone, fuse=False, mirror=mirror, surface="shell")


def eye_open(lid, white=WHITE, ink=INK, iris_top=IRIS_TOP, iris_low=IRIS_LOW, pupil=PUPIL, iris_w=0.66, iris_h=0.82,
             iris_u=-0.14, rim=0.84):
    """The line's eye: a dark rim, a sliver of white behind, a tall iris dark above and light below, a pupil, a glint."""
    cl, cw, ck = srgb(lid), srgb(white), srgb(ink)
    ct, cb, cp = srgb(iris_top), srgb(iris_low), srgb(pupil)

    def look(u, v):
        c = np.tile(cl, (len(u), 1))
        r = u * u + v * v
        c[r < 1.0] = ck
        c[r < rim * rim] = cw
        iris = ((u - iris_u) / iris_w) ** 2 + (v / iris_h) ** 2 < 1
        t = np.clip((v[iris] + iris_h) / (2 * iris_h), 0, 1)[:, None]
        c[iris] = cb * (1 - t) + ct * t
        c[((u - iris_u) / (iris_w * 0.48)) ** 2 + ((v - 0.08) / (iris_h * 0.5)) ** 2 < 1] = cp
        c[((u - iris_u + 0.2) / 0.24) ** 2 + ((v - 0.34) / 0.26) ** 2 < 1] = cw
        c[((u - iris_u - 0.2) / 0.11) ** 2 + ((v + 0.42) / 0.1) ** 2 < 1] = cw
        return c
    return look


# --- Amaura ---------------------------------------------------------------------------------------------------------------
def frill(k, side):
    """One frill on the top of the head: a fan rising up and out, three rounded lobes at its end, fused."""
    s = 1 if side == "R" else -1
    root = H + np.array([0.026 * s, 0.012, 0.036])
    d = unit(0.62 * s, 0.1, 0.78)                                  # up and out in a V
    # the fan's plane: across it runs fore and aft, turned so its face looks a little forward (to the cameras)
    across = unit(0, 1, 0) - d * d[1]; across /= np.linalg.norm(across)
    n = np.cross(d, across); n /= np.linalg.norm(n)
    turn = math.radians(-55 * s)
    across2 = across * math.cos(turn) + n * math.sin(turn)
    n2 = np.cross(d, across2); n2 /= np.linalg.norm(n2)
    TH = 0.0085

    def flat(c, a, b, axis_a, axis_b):
        """A flat ellipsoid in the frill's plane: a along axis_a, b along axis_b, TH across."""
        V, F = ell((0, 0, 0), (a, b, TH), sub=4)
        M = np.stack([axis_a, axis_b, n2], axis=1)
        return V @ M.T + c, F
    k.add("frillstalk" + side, flat(root + d * 0.02, 0.026, 0.016, d, across2), YELLOW, "head")
    # a broad fan widening from the root, as the pictures draw it, not a stalk
    for i, a in enumerate((-28, 0, 28)):
        ar = math.radians(a)
        dl = d * math.cos(ar) + across2 * math.sin(ar)
        k.add(f"frillfan{i}{side}", flat(root + dl * 0.05, 0.045, 0.02, dl, np.cross(n2, dl)), YELLOW, "head")
    for i, a in enumerate((-44, 0, 44)):
        ar = math.radians(a)
        dl = d * math.cos(ar) + across2 * math.sin(ar)
        c = root + d * 0.075 + dl * 0.036
        k.add(f"frilllobe{i}{side}", flat(c, 0.028, 0.021, dl, np.cross(n2, dl)), PINK, "head")
    return root, d


def amaura(src, out):
    k = Kit(src, coat="scales")

    # --- the body: a round pear, low over the ground -------------------------------------------------------------------
    k.add("body", ell((0, 0.012, 0.108), (0.074, 0.105, 0.07)), BLUE, "body")
    k.add("bodychest", ell((0, -0.06, 0.122), (0.058, 0.06, 0.062)), BLUE, "body")
    k.add("bodyrump", ell((0, 0.065, 0.104), (0.07, 0.065, 0.066)), BLUE, "body")

    # --- the legs: short thick stumps, the shoulders and thighs fused into the flanks -------------------------------------
    for name, y, bone, top in (("fore", -0.05, "leg{s}", (0.034, 0.04, 0.04)),
                               ("hind", 0.07, "hind{s}", (0.038, 0.044, 0.044))):
        x = 0.046
        k.add(name + "top", ell((x - 0.012, y, 0.09), top), BLUE, bone, mirror=True)
        k.add(name, tube([(x, y, 0.08), (x + 0.002, y - 0.002, 0.04), (x + 0.003, y - 0.004, 0.022)],
                         [0.029, 0.026, 0.026]), BLUE, bone, mirror=True)
        k.add(name + "foot", ell((x + 0.003, y - 0.008, 0.016), (0.03, 0.035, 0.016)), BLUE, bone, mirror=True)
        nail(k, name + "nail", (x + 0.003, y - 0.04, 0.009), (0, -1, -0.35), 0.014, bone)

    # --- the tail: short, rising back to a point -----------------------------------------------------------------------
    k.add("tail", tube([(0, 0.1, 0.118), (0, 0.15, 0.14), (0, 0.185, 0.168), (0, 0.205, 0.194)],
                       [0.045, 0.03, 0.017, 0.0085]), BLUE, "tail")

    # --- the neck: rising from the chest in a gentle S to the head ------------------------------------------------------
    k.add("neck", tube([(0, -0.07, 0.15), (0, -0.078, 0.2), (0, -0.074, 0.25), (0, -0.078, 0.29), (0, -0.085, 0.32)],
                       [0.042, 0.027, 0.02, 0.02, 0.023]), BLUE, "head")

    # --- the head: round, a little snout, the face flattened a little for the great eyes ---------------------------------
    k.add("head", ell(H, HR), BLUE, "head")
    k.add("snout", ell(H + (0, -0.045, -0.018), (0.04, 0.035, 0.03)), BLUE, "head")
    k.add("face", ell(H + (0, -0.028, 0.0), (0.054, 0.04, 0.045)), BLUE, "head")

    # --- the frills ------------------------------------------------------------------------------------------------------
    root, d = frill(k, "R")
    frill(k, "L")
    k.paint(lambda P: smoothstep(0.5, 0.8, ((P - root) @ d) / 0.12) * (P[:, 0] > 0), PINK, on=["frill"])
    rl, dl = root * (-1, 1, 1), d * (-1, 1, 1)
    k.paint(lambda P: smoothstep(0.5, 0.8, ((P - rl) @ dl) / 0.12) * (P[:, 0] < 0), PINK, on=["frill"])
    k.paint(lambda P: (1 - smoothstep(0.38, 0.62, ((P - root) @ d) / 0.12)) * (P[:, 0] > 0), YELLOW, on=["frill"])
    k.paint(lambda P: (1 - smoothstep(0.38, 0.62, ((P - rl) @ dl) / 0.12)) * (P[:, 0] < 0), YELLOW, on=["frill"])

    # --- the white belly ----------------------------------------------------------------------------------------------------
    k.paint(lambda P: inside(P[:, 2] - 0.072, 0.003) * inside(np.abs(P[:, 0]) - 0.056, 0.004)
            * inside(np.abs(P[:, 1] - 0.01) - 0.085, 0.006), BELLY, on=["body"])

    # --- a diamond of ice on each flank --------------------------------------------------------------------------------------
    for s, side in ((1, "R"), (-1, "L")):
        crystal(k, "ice" + side, (0.068 * s, 0.035, 0.14), (s, 0.15, 0.35), 0.027, "body", sink=0.3, rise=0.3)

    # --- the smile: a small curve low on the snout ----------------------------------------------------------------------------
    cb, ck = srgb(BLUE), srgb(INK)

    def smile(u, v):
        c = np.tile(cb, (len(u), 1))
        c[(np.abs(v - (0.55 * u * u - 0.25)) < 0.2) & (np.abs(u) < 0.8)] = ck
        return c
    k.decal(H + (0, -0.078, -0.03), unit(0, -0.8, -0.5), (0.016, 0.009), smile, eps=0.0025, rings=12, segs=48)

    k.build(voxel=0.0042, smooth=6, band=0.005, wband=0.03)

    # --- the eyes: very large, a dark blue iris dark above and light below, a white glint ---------------------------------
    de = unit(0.6, -0.76, 0.16)
    E = H + (0, -0.018, 0.004) + de * 0.047
    open_eye = eye_open(BLUE)

    def eyes(state):
        k.decal(E, de, (0.023, 0.029), eye_look(state, open_eye, srgb(BLUE), srgb(INK), line=0.15), eps=0.0025,
                rings=20, segs=56, mirror=True)

    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    amaura(*args())
